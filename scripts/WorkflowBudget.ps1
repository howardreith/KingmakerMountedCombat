# One shared, cumulative validation budget for a sprint (docs/VALIDATION-POLICY.md;
# Kingmaker_Mounted_Combat_Playtest_Completion_Plan.md section 6.2). Dot-source this
# file; it defines functions only and changes nothing on load.
#
# The record is one small JSON file per budget id under the lab's runtime-state (or,
# without a lab, under the repository's ignored analysis-cache). Every launch boundary
# that can spend validation time -- Test-Tiers.ps1 and the native launcher -- asks for
# admission before it validates, builds, locks or launches; asks again before every
# further child step; keeps a bounded heartbeat while it runs; and completes its entry
# when it ends. A nested invocation (a wrapper's child) inherits its parent's entry only
# after that entry is verified live, and then counts in the parent. A new shell, restart,
# worktree, commit or agent does not replenish the allowance: the record is keyed by
# budget id, not by process. Record updates run under an exclusive lock file.
#
# Rules enforced here (ceilings, not estimates):
#   allowance        90 min cumulative per budget id (policy default, created once)
#   block ceiling    30 min per entry (an ordinary validation block)
#   native batch     at most 5 distinct scenario/case selections per batch id, and at
#                    most 2 failed attempts of the same selection before a changed experiment
#   unknown spend    an interrupted entry with no measured time and no estimate blocks
#                    admission until it is reconciled explicitly
# A refusal happens before work; it never interrupts, cancels or skips the completion or
# restoration of work already running.
Set-StrictMode -Version Latest

$script:KmcWorkflowAllowanceMinutes = 90
$script:KmcWorkflowBlockCeilingMinutes = 30
$script:KmcWorkflowBatchScenarioLimit = 5
$script:KmcWorkflowEquivalentFailureLimit = 2

function Get-KmcWorkflowClock {
    # Test seam: a file whose content is a UTC timestamp replaces the wall clock, so
    # exhaustion, interruption and propagation are tested without waiting real minutes.
    $clockFile = $env:KMC_WORKFLOW_TEST_CLOCK_FILE
    if (-not [string]::IsNullOrWhiteSpace($clockFile) -and (Test-Path -LiteralPath $clockFile -PathType Leaf)) {
        return [DateTime]::Parse((Get-Content -Raw -LiteralPath $clockFile).Trim(), [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal)
    }
    return [DateTime]::UtcNow
}

function ConvertTo-KmcWorkflowUtc([string]$Value) {
    return [DateTime]::Parse($Value, [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal)
}

function Get-KmcWorkflowBudgetRoot([string]$RepoRoot) {
    $override = $env:KMC_WORKFLOW_BUDGET_ROOT
    if (-not [string]::IsNullOrWhiteSpace($override)) { return [IO.Path]::GetFullPath($override) }
    $labRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot '..\..'))
    if (Test-Path -LiteralPath (Join-Path $labRoot 'environment-intake.json') -PathType Leaf) {
        return Join-Path $labRoot 'runtime-state\workflow-budgets'
    }
    return Join-Path $RepoRoot 'analysis-cache\workflow-budgets'
}

function Get-KmcWorkflowBudgetId([string]$Requested) {
    if (-not [string]::IsNullOrWhiteSpace($Requested)) { $id = $Requested }
    elseif (-not [string]::IsNullOrWhiteSpace($env:KMC_WORKFLOW_BUDGET_ID)) { $id = $env:KMC_WORKFLOW_BUDGET_ID }
    else { $id = 'development' }
    if ($id -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$') { throw "Workflow budget id is invalid: $id" }
    return $id
}

function Read-KmcWorkflowBudget([string]$RepoRoot, [string]$BudgetId) {
    $root = Get-KmcWorkflowBudgetRoot $RepoRoot
    $path = Join-Path $root ($BudgetId + '.json')
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $record = Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
        if ([int]$record.schemaVersion -ne 1 -or [string]$record.budgetId -cne $BudgetId) { throw "Workflow budget record is not the requested budget: $path" }
    } else {
        # A new record is only ever created at the policy default; changing an
        # allowance is an explicit edit of the record, not a command-line switch.
        $record = [pscustomobject]@{ schemaVersion = 1; budgetId = $BudgetId; allowanceMinutes = $script:KmcWorkflowAllowanceMinutes
            blockCeilingMinutes = $script:KmcWorkflowBlockCeilingMinutes; createdUtc = (Get-KmcWorkflowClock).ToString('o'); entries = @(); stepHistory = @{} }
    }
    $entries = @(); if ($null -ne $record.entries) { $entries = @($record.entries) }
    $record | Add-Member -NotePropertyName entries -NotePropertyValue $entries -Force
    if ($null -eq $record.PSObject.Properties['stepHistory'] -or $null -eq $record.stepHistory) { $record | Add-Member -NotePropertyName stepHistory -NotePropertyValue ([pscustomobject]@{}) -Force }
    if ($null -eq $record.PSObject.Properties['blockCeilingMinutes']) { $record | Add-Member -NotePropertyName blockCeilingMinutes -NotePropertyValue $script:KmcWorkflowBlockCeilingMinutes -Force }
    return [pscustomobject]@{ Path = $path; Record = $record }
}

function Save-KmcWorkflowBudget($Budget) {
    $directory = Split-Path -Parent $Budget.Path
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $temporary = $Budget.Path + '.' + $PID + '.' + [Guid]::NewGuid().ToString('N').Substring(0, 8) + '.tmp'
    ($Budget.Record | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $temporary -Encoding UTF8
    Move-Item -LiteralPath $temporary -Destination $Budget.Path -Force
}

# Every read-modify-write of a record runs here, under an exclusive lock file, so two
# shells (a wrapper and a detached launcher, or two tool hosts) never lose an update.
function Update-KmcWorkflowBudgetRecord([string]$RepoRoot, [string]$BudgetId, [scriptblock]$Action) {
    $root = Get-KmcWorkflowBudgetRoot $RepoRoot
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    $lockPath = Join-Path $root ($BudgetId + '.lock')
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    $stream = $null
    while ($null -eq $stream) {
        try { $stream = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
        catch [IO.IOException] {
            if ([DateTime]::UtcNow -gt $deadline) { throw "Workflow budget record is locked by another writer: $lockPath" }
            Start-Sleep -Milliseconds 100
        }
    }
    try {
        $budget = Read-KmcWorkflowBudget $RepoRoot $BudgetId
        # Saved even when the action refuses: a reconciliation made on the way to a refusal
        # is a fact about the record, not part of the refused work.
        try { return & $Action $budget }
        finally { Save-KmcWorkflowBudget $budget }
    }
    finally { $stream.Dispose() }
}

function Test-KmcWorkflowProcessAlive([int]$ProcessId, [string]$StartedUtc) {
    if ($ProcessId -le 0) { return $false }
    if (-not [string]::IsNullOrWhiteSpace($env:KMC_WORKFLOW_TEST_DEAD_PIDS) -and (($env:KMC_WORKFLOW_TEST_DEAD_PIDS -split ',') -contains [string]$ProcessId)) { return $false }
    try { $process = Get-Process -Id $ProcessId -ErrorAction Stop } catch { return $false }
    # Process ids are reused: a process that started after the entry is not the entry's.
    # (Not decidable under the test clock, whose entries carry fictional start times.)
    if (-not [string]::IsNullOrWhiteSpace($env:KMC_WORKFLOW_TEST_CLOCK_FILE)) { return $true }
    try { return $process.StartTime.ToUniversalTime() -le (ConvertTo-KmcWorkflowUtc $StartedUtc).AddMinutes(1) } catch { return $true }
}

function Get-KmcWorkflowEntrySeconds($Entry, [DateTime]$Now) {
    $started = ConvertTo-KmcWorkflowUtc ([string]$Entry.startedUtc)
    switch ([string]$Entry.status) {
        'running' { return [Math]::Max(0, ($Now - $started).TotalSeconds) }
        default {
            if ($null -ne $Entry.PSObject.Properties['seconds'] -and $null -ne $Entry.seconds) { return [double]$Entry.seconds }
            return 0
        }
    }
}

# Reconciles the record against reality before any admission: a running entry whose
# process is gone was interrupted (a killed shell, a crash, a restart). It is charged
# from its start to its last heartbeat or its last step start, whichever is later; an
# interval that was never measured is charged at the entry's estimate when one was
# recorded, and otherwise the entry becomes 'unreconciled' and blocks admission until
# Set-KmcWorkflowEntryReconciliation records what it actually cost. Nothing is ever
# reset or forgiven here.
function Update-KmcWorkflowBudgetReconciliation($Budget, [DateTime]$Now) {
    $reconciled = 0
    foreach ($entry in $Budget.Record.entries) {
        if ([string]$entry.status -ne 'running') { continue }
        if (Test-KmcWorkflowProcessAlive ([int]$entry.processId) ([string]$entry.startedUtc)) { continue }
        $started = ConvertTo-KmcWorkflowUtc ([string]$entry.startedUtc)
        $last = $started
        foreach ($name in @('heartbeatUtc', 'stepStartedUtc')) {
            if ($null -ne $entry.PSObject.Properties[$name] -and -not [string]::IsNullOrWhiteSpace([string]$entry.$name)) {
                $candidate = ConvertTo-KmcWorkflowUtc ([string]$entry.$name)
                if ($candidate -gt $last) { $last = $candidate }
            }
        }
        $measured = [Math]::Round([Math]::Max(0, ($last - $started).TotalSeconds), 1)
        $estimate = if ($null -ne $entry.PSObject.Properties['estimatedSeconds'] -and $null -ne $entry.estimatedSeconds) { [double]$entry.estimatedSeconds } else { $null }
        if ($measured -gt 0) {
            $charged = if ($null -ne $estimate -and $estimate -gt $measured) { $estimate } else { $measured }
            $entry | Add-Member -NotePropertyName status -NotePropertyValue 'interrupted' -Force
            $entry | Add-Member -NotePropertyName seconds -NotePropertyValue $charged -Force
            $entry | Add-Member -NotePropertyName reconciliation -NotePropertyValue $(if ($charged -eq $measured) { 'measured-to-last-heartbeat' } else { 'charged-estimate-over-partial-measurement' }) -Force
        } elseif ($null -ne $estimate) {
            $entry | Add-Member -NotePropertyName status -NotePropertyValue 'interrupted' -Force
            $entry | Add-Member -NotePropertyName seconds -NotePropertyValue $estimate -Force
            $entry | Add-Member -NotePropertyName reconciliation -NotePropertyValue 'charged-estimate-unmeasured' -Force
        } else {
            $entry | Add-Member -NotePropertyName status -NotePropertyValue 'unreconciled' -Force
            $entry | Add-Member -NotePropertyName seconds -NotePropertyValue $null -Force
            $entry | Add-Member -NotePropertyName reconciliation -NotePropertyValue 'unknown-spend-requires-explicit-reconciliation' -Force
        }
        $entry | Add-Member -NotePropertyName endedUtc -NotePropertyValue $last.ToString('o') -Force
        $entry | Add-Member -NotePropertyName reconciledUtc -NotePropertyValue $Now.ToString('o') -Force
        $reconciled++
    }
    return $reconciled
}

function Get-KmcWorkflowBudgetSummary($Budget, [DateTime]$Now) {
    $spent = 0.0; $running = 0; $interrupted = 0; $unreconciled = 0
    foreach ($entry in $Budget.Record.entries) {
        $spent += Get-KmcWorkflowEntrySeconds $entry $Now
        switch ([string]$entry.status) { 'running' { $running++ } 'interrupted' { $interrupted++ } 'unreconciled' { $unreconciled++ } }
    }
    $allowance = [double]$Budget.Record.allowanceMinutes * 60
    return [pscustomobject]@{ BudgetId = [string]$Budget.Record.budgetId; AllowanceMinutes = [double]$Budget.Record.allowanceMinutes
        SpentMinutes = [Math]::Round($spent / 60, 2); RemainingMinutes = [Math]::Round([Math]::Max(0, $allowance - $spent) / 60, 2)
        RemainingSeconds = [Math]::Max(0, $allowance - $spent); Exhausted = ($spent -ge $allowance)
        RunningEntries = $running; InterruptedEntries = $interrupted; UnreconciledEntries = $unreconciled; Entries = @($Budget.Record.entries).Count }
}

function Format-KmcWorkflowBudgetSummary($Summary) {
    return ('budget ' + $Summary.BudgetId + ': spent ' + $Summary.SpentMinutes + ' of ' + $Summary.AllowanceMinutes + ' min; remaining ' +
        $Summary.RemainingMinutes + ' min; entries ' + $Summary.Entries + ' (running ' + $Summary.RunningEntries + ', interrupted ' +
        $Summary.InterruptedEntries + ', unreconciled ' + $Summary.UnreconciledEntries + ')')
}

function Find-KmcWorkflowEntry($Budget, [string]$EntryId) {
    foreach ($entry in $Budget.Record.entries) { if ([string]$entry.entryId -ceq $EntryId) { return $entry } }
    return $null
}

# The checks every admission and every further step share. Throws with the precise gap.
function Assert-KmcWorkflowAllowance($Budget, [DateTime]$Now, [string]$What, [Nullable[int]]$EstimatedSeconds, $Entry) {
    $summary = Get-KmcWorkflowBudgetSummary $Budget $Now
    if ($summary.UnreconciledEntries -gt 0) {
        throw ('Workflow budget blocked: ' + (Format-KmcWorkflowBudgetSummary $summary) + '. An interrupted entry has unknown spend; record it with ' +
            'scripts\Reconcile-WorkflowBudget.ps1 before admitting ' + $What + '.')
    }
    if ($summary.Exhausted) {
        throw ('Workflow budget exhausted: ' + (Format-KmcWorkflowBudgetSummary $summary) + '. ' + $What + ' is not admitted; settle running work, keep the patch, ' +
            'and report the precise remaining check. Additional execution needs a specifically selected remaining check and allowance (a new explicit budget id), not a retry.')
    }
    if ($null -ne $EstimatedSeconds -and $EstimatedSeconds -gt $summary.RemainingSeconds) {
        throw ('Workflow budget refuses oversized work: ' + $What + ' is known to take about ' + [Math]::Round($EstimatedSeconds / 60.0, 1) + ' min and only ' +
            $summary.RemainingMinutes + ' min remain (' + (Format-KmcWorkflowBudgetSummary $summary) + '). Select a smaller check or a new explicit budget id.')
    }
    if ($null -ne $Entry) {
        $ceiling = [double]$Budget.Record.blockCeilingMinutes * 60
        $elapsed = ($Now - (ConvertTo-KmcWorkflowUtc ([string]$Entry.startedUtc))).TotalSeconds
        if ($elapsed -ge $ceiling) {
            throw ('Workflow block ceiling reached: this validation block has run ' + [Math]::Round($elapsed / 60, 1) + ' min of its ' + $Budget.Record.blockCeilingMinutes +
                ' min ceiling; ' + $What + ' is not started. Running work settles; report the remaining steps as NOT RUN.')
        }
        if ($null -ne $EstimatedSeconds -and $EstimatedSeconds -gt ($ceiling - $elapsed)) {
            throw ('Workflow block ceiling refuses oversized work: ' + $What + ' is known to take about ' + [Math]::Round($EstimatedSeconds / 60.0, 1) + ' min but only ' +
                [Math]::Round(($ceiling - $elapsed) / 60, 1) + ' min of this block remain. Select a smaller check or a new block.')
        }
    }
    return $summary
}

function Get-KmcWorkflowBatchId([string]$BudgetId) {
    $batch = $env:KMC_WORKFLOW_NATIVE_BATCH
    if ([string]::IsNullOrWhiteSpace($batch)) { return $BudgetId + ':batch1' }
    if ($batch -notmatch '^[A-Za-z0-9][A-Za-z0-9._:-]{0,79}$') { throw "Workflow native batch id is invalid: $batch" }
    return $batch
}

# The bounded native batch: at most five distinct scenario/case selections per batch, and
# at most two failed attempts of the same selection before a changed experiment.
function Assert-KmcWorkflowNativeBatch($Budget, [string]$BatchId, [string]$BatchKey) {
    $launches = @($Budget.Record.entries | Where-Object { [string]$_.kind -eq 'native' -and $null -ne $_.PSObject.Properties['launch'] -and $_.launch -and
        $null -ne $_.PSObject.Properties['batchId'] -and [string]$_.batchId -ceq $BatchId })
    $keys = @($launches | ForEach-Object { [string]$_.batchKey } | Select-Object -Unique)
    if ($keys -cnotcontains $BatchKey -and $keys.Count -ge $script:KmcWorkflowBatchScenarioLimit) {
        throw ('Workflow native batch ' + $BatchId + ' already names ' + $keys.Count + ' scenarios (' + ($keys -join ', ') + '); "' + $BatchKey +
            '" is not admitted. A further scenario needs a new explicitly selected batch (KMC_WORKFLOW_NATIVE_BATCH).')
    }
    $failures = @($launches | Where-Object { [string]$_.batchKey -ceq $BatchKey -and [string]$_.status -in @('failed', 'interrupted', 'unreconciled') }).Count
    if ($failures -ge $script:KmcWorkflowEquivalentFailureLimit) {
        throw ('Workflow native batch refuses a third equivalent attempt: "' + $BatchKey + '" has failed ' + $failures + ' times in batch ' + $BatchId +
            '. A new discriminating observation or a materially changed experiment (a different selection or batch) is required before another launch.')
    }
}

# Admission. Returns the entry to pass to Assert-KmcWorkflowStepAdmission, Update- and
# Complete-KmcWorkflowEntry. A verified live parent entry is inherited (Nested = true);
# a stale inherited entry is reported and this invocation is accounted on its own.
function Request-KmcWorkflowAdmission {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [string]$BudgetId,
        [Parameter(Mandatory = $true)][ValidateSet('tier', 'native')][string]$Kind,
        [Parameter(Mandatory = $true)][string]$Selection,
        # Known or estimated cost in seconds; $null when unknown. Reported, never invented.
        [Nullable[int]]$EstimatedSeconds = $null,
        # A real native launch (not a dry run): subject to the five-scenario batch rule.
        [switch]$Launch,
        [string]$BatchKey = ''
    )
    $previousEntry = $env:KMC_WORKFLOW_BUDGET_ENTRY
    $parent = $previousEntry
    $now = Get-KmcWorkflowClock
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        # A child inherits its parent's budget through the entry id, but only a verified
        # live parent: the entry must exist, still be running, belong to a live process
        # and have allowance left. Anything else is stale context, not ownership.
        $parentId = ($parent -split ':')[0]
        if (-not [string]::IsNullOrWhiteSpace($BudgetId) -and $BudgetId -cne $parentId) { throw "Nested workflow invocation belongs to another budget: $parent (this invocation: $BudgetId)." }
        $verdict = Update-KmcWorkflowBudgetRecord $RepoRoot $parentId {
            param($budget)
            [void](Update-KmcWorkflowBudgetReconciliation $budget $now)
            $entry = Find-KmcWorkflowEntry $budget $parent
            if ($null -eq $entry) { return 'missing' }
            if ([string]$entry.status -ne 'running') { return [string]$entry.status }
            if (-not (Test-KmcWorkflowProcessAlive ([int]$entry.processId) ([string]$entry.startedUtc))) { return 'dead' }
            [void](Assert-KmcWorkflowAllowance $budget $now ('nested ' + $Kind + ' "' + $Selection + '"') $EstimatedSeconds $entry)
            $entry | Add-Member -NotePropertyName heartbeatUtc -NotePropertyValue $now.ToString('o') -Force
            return 'live'
        }
        if ($verdict -eq 'live') {
            Write-Host ('Workflow budget: nested under live entry ' + $parent + '; this invocation is counted in its parent.')
            return [pscustomobject]@{ EntryId = $parent; Nested = $true; BudgetId = $parentId; PreviousEntry = $previousEntry }
        }
        Write-Warning ('Workflow budget: inherited entry ' + $parent + ' is ' + $verdict + ', not a live parent; this invocation is accounted on its own.')
        Remove-Item Env:KMC_WORKFLOW_BUDGET_ENTRY -ErrorAction SilentlyContinue
        if ([string]::IsNullOrWhiteSpace($BudgetId)) { $BudgetId = $parentId }
    }
    $id = Get-KmcWorkflowBudgetId $BudgetId
    $batchId = if ($Kind -eq 'native' -and $Launch) { Get-KmcWorkflowBatchId $id } else { $null }
    $entryId = $id + ':' + $now.ToString('yyyyMMddTHHmmssfffZ') + ':' + $PID + ':' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $summary = Update-KmcWorkflowBudgetRecord $RepoRoot $id {
        param($budget)
        $reconciled = Update-KmcWorkflowBudgetReconciliation $budget $now
        if ($reconciled -gt 0) { Write-Host ('Workflow budget: reconciled ' + $reconciled + ' interrupted entr' + $(if ($reconciled -eq 1) { 'y' } else { 'ies' }) + ' from a previous shell.') }
        $admitted = Assert-KmcWorkflowAllowance $budget $now ($Kind + ' "' + $Selection + '"') $EstimatedSeconds $null
        if ($null -ne $batchId) { Assert-KmcWorkflowNativeBatch $budget $batchId $BatchKey }
        $entry = [pscustomobject]@{ entryId = $entryId; kind = $Kind; selection = $Selection; startedUtc = $now.ToString('o'); heartbeatUtc = $now.ToString('o')
            stepStartedUtc = $null; processId = $PID; status = 'running'; estimatedSeconds = $EstimatedSeconds; endedUtc = $null; seconds = $null
            launch = [bool]$Launch; batchId = $batchId; batchKey = $(if ($null -ne $batchId) { $BatchKey } else { $null }) }
        $budget.Record.entries = @($budget.Record.entries) + @($entry)
        return $admitted
    }
    $env:KMC_WORKFLOW_BUDGET_ENTRY = $entryId
    Write-Host ('Workflow budget: admitted ' + $Kind + ' "' + $Selection + '" as ' + $entryId + '; ' + (Format-KmcWorkflowBudgetSummary $summary))
    return [pscustomobject]@{ EntryId = $entryId; Nested = $false; BudgetId = $id; PreviousEntry = $previousEntry }
}

# Before every further child step of an admitted invocation: the cumulative allowance and
# the block ceiling are checked again, and known oversized work is refused rather than
# started. Records the step start as the entry's latest measured point.
function Assert-KmcWorkflowStepAdmission {
    param([Parameter(Mandatory = $true)][string]$RepoRoot, $Admission, [Parameter(Mandatory = $true)][string]$StepName, [Nullable[int]]$EstimatedSeconds = $null)
    if ($null -eq $Admission) { return }
    $now = Get-KmcWorkflowClock
    [void](Update-KmcWorkflowBudgetRecord $RepoRoot $Admission.BudgetId {
        param($budget)
        [void](Update-KmcWorkflowBudgetReconciliation $budget $now)
        $entry = Find-KmcWorkflowEntry $budget $Admission.EntryId
        [void](Assert-KmcWorkflowAllowance $budget $now ('step "' + $StepName + '"') $EstimatedSeconds $entry)
        if ($null -ne $entry) {
            $entry | Add-Member -NotePropertyName stepStartedUtc -NotePropertyValue $now.ToString('o') -Force
            $entry | Add-Member -NotePropertyName heartbeatUtc -NotePropertyValue $now.ToString('o') -Force
        }
    })
}

function Update-KmcWorkflowEntry {
    # Heartbeat, plus the measured duration of one step for later planning. Nested
    # invocations heartbeat their parent's entry; they never complete it.
    param([Parameter(Mandatory = $true)][string]$RepoRoot, $Admission, [string]$StepName, [Nullable[double]]$StepSeconds = $null)
    if ($null -eq $Admission) { return }
    $now = Get-KmcWorkflowClock
    [void](Update-KmcWorkflowBudgetRecord $RepoRoot $Admission.BudgetId {
        param($budget)
        $entry = Find-KmcWorkflowEntry $budget $Admission.EntryId
        if ($null -ne $entry) { $entry | Add-Member -NotePropertyName heartbeatUtc -NotePropertyValue $now.ToString('o') -Force }
        if (-not [string]::IsNullOrWhiteSpace($StepName) -and $null -ne $StepSeconds) {
            $budget.Record.stepHistory | Add-Member -NotePropertyName $StepName -NotePropertyValue ([pscustomobject]@{ seconds = [Math]::Round($StepSeconds, 1); observedUtc = $now.ToString('o') }) -Force
        }
    })
}

function Complete-KmcWorkflowEntry {
    param([Parameter(Mandatory = $true)][string]$RepoRoot, $Admission, [ValidateSet('completed', 'failed')][string]$Status = 'completed')
    if ($null -eq $Admission) { return }
    # The caller's previous context is restored whether this entry was owned or inherited.
    if ($null -ne $Admission.PSObject.Properties['PreviousEntry']) {
        if ([string]::IsNullOrWhiteSpace([string]$Admission.PreviousEntry)) { Remove-Item Env:KMC_WORKFLOW_BUDGET_ENTRY -ErrorAction SilentlyContinue }
        else { $env:KMC_WORKFLOW_BUDGET_ENTRY = [string]$Admission.PreviousEntry }
    }
    if ($Admission.Nested) { return }
    $now = Get-KmcWorkflowClock
    $outcome = Update-KmcWorkflowBudgetRecord $RepoRoot $Admission.BudgetId {
        param($budget)
        $entry = Find-KmcWorkflowEntry $budget $Admission.EntryId
        $changed = $false
        if ($null -ne $entry -and [string]$entry.status -eq 'running') {
            $started = ConvertTo-KmcWorkflowUtc ([string]$entry.startedUtc)
            $entry | Add-Member -NotePropertyName status -NotePropertyValue $Status -Force
            $entry | Add-Member -NotePropertyName endedUtc -NotePropertyValue $now.ToString('o') -Force
            $entry | Add-Member -NotePropertyName seconds -NotePropertyValue ([Math]::Round([Math]::Max(0, ($now - $started).TotalSeconds), 1)) -Force
            $entry | Add-Member -NotePropertyName heartbeatUtc -NotePropertyValue $now.ToString('o') -Force
            $changed = $true
        }
        return [pscustomobject]@{ Summary = (Get-KmcWorkflowBudgetSummary $budget $now); Changed = $changed }
    }
    # Completion is idempotent: a second call (a trap after the finally block) changes nothing and says nothing.
    if ($outcome.Changed) { Write-Host ('Workflow budget: ' + $Status + ' ' + $Admission.EntryId + '; ' + (Format-KmcWorkflowBudgetSummary $outcome.Summary)) }
}

# Explicit reconciliation of an entry whose spend the record could not measure, and the
# import of work that ran before the record existed. Both append facts; nothing is reset.
function Set-KmcWorkflowEntryReconciliation {
    param([Parameter(Mandatory = $true)][string]$RepoRoot, [Parameter(Mandatory = $true)][string]$BudgetId, [Parameter(Mandatory = $true)][string]$EntryId,
        [Parameter(Mandatory = $true)][ValidateRange(0, 86400)][double]$Seconds, [Parameter(Mandatory = $true)][string]$Reason)
    $now = Get-KmcWorkflowClock
    return Update-KmcWorkflowBudgetRecord $RepoRoot $BudgetId {
        param($budget)
        [void](Update-KmcWorkflowBudgetReconciliation $budget $now)
        $entry = Find-KmcWorkflowEntry $budget $EntryId
        if ($null -eq $entry) { throw "Workflow budget entry not found: $EntryId" }
        if ([string]$entry.status -cnotin @('unreconciled', 'interrupted')) { throw ('Only interrupted or unreconciled entries are reconciled; ' + $EntryId + ' is ' + $entry.status + '.') }
        $entry | Add-Member -NotePropertyName status -NotePropertyValue 'interrupted' -Force
        $entry | Add-Member -NotePropertyName seconds -NotePropertyValue ([Math]::Round($Seconds, 1)) -Force
        $entry | Add-Member -NotePropertyName reconciliation -NotePropertyValue ('explicit: ' + $Reason) -Force
        $entry | Add-Member -NotePropertyName reconciledUtc -NotePropertyValue $now.ToString('o') -Force
        return Get-KmcWorkflowBudgetSummary $budget $now
    }
}

function Add-KmcWorkflowImportedEntry {
    param([Parameter(Mandatory = $true)][string]$RepoRoot, [Parameter(Mandatory = $true)][string]$BudgetId, [Parameter(Mandatory = $true)][ValidateSet('tier', 'native', 'other')][string]$Kind,
        [Parameter(Mandatory = $true)][string]$Selection, [Parameter(Mandatory = $true)][ValidateRange(1, 86400)][double]$Seconds,
        [Parameter(Mandatory = $true)][string]$StartedUtc, [Parameter(Mandatory = $true)][string]$Reason)
    $now = Get-KmcWorkflowClock
    $started = ConvertTo-KmcWorkflowUtc $StartedUtc
    return Update-KmcWorkflowBudgetRecord $RepoRoot $BudgetId {
        param($budget)
        $entry = [pscustomobject]@{ entryId = $BudgetId + ':imported:' + $started.ToString('yyyyMMddTHHmmssfffZ') + ':' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
            kind = $Kind; selection = $Selection; startedUtc = $started.ToString('o'); heartbeatUtc = $started.AddSeconds($Seconds).ToString('o'); stepStartedUtc = $null
            processId = 0; status = 'completed'; estimatedSeconds = $null; endedUtc = $started.AddSeconds($Seconds).ToString('o'); seconds = [Math]::Round($Seconds, 1)
            launch = $false; batchId = $null; batchKey = $null; reconciliation = ('imported: ' + $Reason); reconciledUtc = $now.ToString('o') }
        $budget.Record.entries = @($budget.Record.entries) + @($entry)
        return Get-KmcWorkflowBudgetSummary $budget $now
    }
}

function Get-KmcWorkflowStepHistorySeconds($Budget, [string]$StepName) {
    $history = $Budget.Record.stepHistory
    if ($null -eq $history) { return $null }
    $property = $history.PSObject.Properties[$StepName]
    if ($null -eq $property -or $null -eq $property.Value) { return $null }
    return [double]$property.Value.seconds
}
