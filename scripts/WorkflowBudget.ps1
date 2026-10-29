# One shared, cumulative validation budget for a sprint (docs/VALIDATION-POLICY.md;
# Kingmaker_Mounted_Combat_Playtest_Completion_Plan.md section 6.2). Dot-source this
# file; it defines functions only and changes nothing on load.
#
# The record is one small JSON file per budget id under the lab's runtime-state
# (or, without a lab, under the repository's ignored analysis-cache). Every launch
# boundary that can spend validation time -- Test-Tiers.ps1 and the native launcher --
# asks for admission before it builds, locks or launches, records its entry, keeps a
# heartbeat while it runs and completes the entry when it ends. A nested invocation
# (a wrapper's child) inherits its parent's entry through KMC_WORKFLOW_BUDGET_ENTRY
# and is not counted twice. A new shell, restart, worktree, commit or agent does not
# replenish the allowance: the record is keyed by budget id, not by process.
#
# Refusal happens before any work; it never interrupts, cancels or skips the
# completion/restoration of work already running.
Set-StrictMode -Version Latest

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

function Read-KmcWorkflowBudget([string]$RepoRoot, [string]$BudgetId, [int]$DefaultAllowanceMinutes = 90) {
    $root = Get-KmcWorkflowBudgetRoot $RepoRoot
    $path = Join-Path $root ($BudgetId + '.json')
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        $record = Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
        if ([int]$record.schemaVersion -ne 1 -or [string]$record.budgetId -cne $BudgetId) { throw "Workflow budget record is not the requested budget: $path" }
    } else {
        # A new record is only ever created at the policy default; changing an
        # allowance is an explicit edit of the record, not a command-line switch.
        $record = [pscustomobject]@{ schemaVersion = 1; budgetId = $BudgetId; allowanceMinutes = $DefaultAllowanceMinutes
            createdUtc = (Get-KmcWorkflowClock).ToString('o'); entries = @(); stepHistory = @{} }
    }
    $entries = @(); if ($null -ne $record.entries) { $entries = @($record.entries) }
    $record | Add-Member -NotePropertyName entries -NotePropertyValue $entries -Force
    if ($null -eq $record.stepHistory) { $record | Add-Member -NotePropertyName stepHistory -NotePropertyValue ([pscustomobject]@{}) -Force }
    return [pscustomobject]@{ Path = $path; Record = $record }
}

function Save-KmcWorkflowBudget($Budget) {
    $directory = Split-Path -Parent $Budget.Path
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $temporary = $Budget.Path + '.tmp'
    ($Budget.Record | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $temporary -Encoding UTF8
    Move-Item -LiteralPath $temporary -Destination $Budget.Path -Force
}

function Test-KmcWorkflowProcessAlive([int]$ProcessId, [string]$StartedUtc) {
    if ($ProcessId -le 0) { return $false }
    try { $process = Get-Process -Id $ProcessId -ErrorAction Stop } catch { return $false }
    # Process ids are reused: a process that started after the entry is not the entry's.
    # (Not decidable under the test clock, whose entries carry fictional start times.)
    if (-not [string]::IsNullOrWhiteSpace($env:KMC_WORKFLOW_TEST_CLOCK_FILE)) { return $true }
    try { return $process.StartTime.ToUniversalTime() -le ([DateTime]::Parse($StartedUtc, [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AdjustToUniversal).AddMinutes(1)) } catch { return $true }
}

function Get-KmcWorkflowEntrySeconds($Entry, [DateTime]$Now) {
    $started = [DateTime]::Parse([string]$Entry.startedUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AdjustToUniversal)
    switch ([string]$Entry.status) {
        'running' { return [Math]::Max(0, ($Now - $started).TotalSeconds) }
        default {
            if ($null -ne $Entry.seconds) { return [double]$Entry.seconds }
            return 0
        }
    }
}

# Reconciles the record against reality before any admission: a running entry whose
# process is gone was interrupted (a killed shell, a crash, a restart) and keeps the
# time it is known to have spent, from its start to its last heartbeat. Nothing is
# ever reset by this; an interrupted entry is reported, not forgiven.
function Update-KmcWorkflowBudgetReconciliation($Budget, [DateTime]$Now) {
    $reconciled = 0
    foreach ($entry in $Budget.Record.entries) {
        if ([string]$entry.status -ne 'running') { continue }
        $alive = Test-KmcWorkflowProcessAlive ([int]$entry.processId) ([string]$entry.startedUtc)
        $testDead = -not [string]::IsNullOrWhiteSpace($env:KMC_WORKFLOW_TEST_DEAD_PIDS) -and (($env:KMC_WORKFLOW_TEST_DEAD_PIDS -split ',') -contains [string]$entry.processId)
        if ($alive -and -not $testDead) { continue }
        $started = [DateTime]::Parse([string]$entry.startedUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AdjustToUniversal)
        $last = if ([string]::IsNullOrWhiteSpace([string]$entry.heartbeatUtc)) { $started } else {
            [DateTime]::Parse([string]$entry.heartbeatUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AdjustToUniversal) }
        $entry | Add-Member -NotePropertyName status -NotePropertyValue 'interrupted' -Force
        $entry | Add-Member -NotePropertyName endedUtc -NotePropertyValue $last.ToString('o') -Force
        $entry | Add-Member -NotePropertyName seconds -NotePropertyValue ([Math]::Round([Math]::Max(0, ($last - $started).TotalSeconds), 1)) -Force
        $entry | Add-Member -NotePropertyName reconciledUtc -NotePropertyValue $Now.ToString('o') -Force
        $reconciled++
    }
    return $reconciled
}

function Get-KmcWorkflowBudgetSummary($Budget, [DateTime]$Now) {
    $spent = 0.0; $running = 0; $interrupted = 0
    foreach ($entry in $Budget.Record.entries) {
        $spent += Get-KmcWorkflowEntrySeconds $entry $Now
        if ([string]$entry.status -eq 'running') { $running++ }
        if ([string]$entry.status -eq 'interrupted') { $interrupted++ }
    }
    $allowance = [double]$Budget.Record.allowanceMinutes * 60
    return [pscustomobject]@{ BudgetId = [string]$Budget.Record.budgetId; AllowanceMinutes = [double]$Budget.Record.allowanceMinutes
        SpentMinutes = [Math]::Round($spent / 60, 2); RemainingMinutes = [Math]::Round([Math]::Max(0, $allowance - $spent) / 60, 2)
        Exhausted = ($spent -ge $allowance); RunningEntries = $running; InterruptedEntries = $interrupted; Entries = @($Budget.Record.entries).Count }
}

function Format-KmcWorkflowBudgetSummary($Summary) {
    return ('budget ' + $Summary.BudgetId + ': spent ' + $Summary.SpentMinutes + ' of ' + $Summary.AllowanceMinutes + ' min; remaining ' +
        $Summary.RemainingMinutes + ' min; entries ' + $Summary.Entries + ' (running ' + $Summary.RunningEntries + ', interrupted ' + $Summary.InterruptedEntries + ')')
}

# Admission. Returns the entry id to pass to Update-/Complete-KmcWorkflowEntry, or the
# inherited parent entry (nested) which the caller must neither heartbeat nor complete.
function Request-KmcWorkflowAdmission {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [string]$BudgetId,
        [Parameter(Mandatory = $true)][ValidateSet('tier', 'native')][string]$Kind,
        [Parameter(Mandatory = $true)][string]$Selection,
        # Known or estimated cost in seconds; $null when unknown. Reported, never invented.
        [Nullable[int]]$EstimatedSeconds = $null
    )
    $parent = $env:KMC_WORKFLOW_BUDGET_ENTRY
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        # A child inherits its parent's budget through the entry id; an explicit different
        # budget inside a parent's entry is a contradiction, not a second allowance.
        $parentId = ($parent -split ':')[0]
        if (-not [string]::IsNullOrWhiteSpace($BudgetId) -and $BudgetId -cne $parentId) { throw "Nested workflow invocation belongs to another budget: $parent (this invocation: $BudgetId)." }
        Write-Host ('Workflow budget: nested under entry ' + $parent + '; this invocation is counted in its parent.')
        return [pscustomobject]@{ EntryId = $parent; Nested = $true; BudgetId = $parentId }
    }
    $id = Get-KmcWorkflowBudgetId $BudgetId
    $now = Get-KmcWorkflowClock
    $budget = Read-KmcWorkflowBudget $RepoRoot $id
    $reconciled = Update-KmcWorkflowBudgetReconciliation $budget $now
    $summary = Get-KmcWorkflowBudgetSummary $budget $now
    if ($reconciled -gt 0) { Write-Host ('Workflow budget: reconciled ' + $reconciled + ' interrupted entr' + $(if ($reconciled -eq 1) { 'y' } else { 'ies' }) + ' from a previous shell.') }
    if ($summary.Exhausted) {
        Save-KmcWorkflowBudget $budget
        throw ('Workflow budget exhausted: ' + (Format-KmcWorkflowBudgetSummary $summary) + '. No new validation is admitted; settle running work, keep the patch, ' +
            'and report the precise remaining check. Additional execution needs a specifically selected remaining check and allowance (a new explicit budget id), not a retry.')
    }
    if ($null -ne $EstimatedSeconds -and ($EstimatedSeconds / 60.0) -gt $summary.RemainingMinutes) {
        Write-Warning ('Workflow budget: the estimated cost (' + [Math]::Round($EstimatedSeconds / 60.0, 1) + ' min) exceeds the remaining allowance (' + $summary.RemainingMinutes + ' min); the ceiling applies when it is reached.')
    }
    $entryId = $id + ':' + $now.ToString('yyyyMMddTHHmmssfffZ') + ':' + $PID + ':' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $entry = [pscustomobject]@{ entryId = $entryId; kind = $Kind; selection = $Selection; startedUtc = $now.ToString('o'); heartbeatUtc = $now.ToString('o')
        processId = $PID; status = 'running'; estimatedSeconds = $EstimatedSeconds; endedUtc = $null; seconds = $null }
    $budget.Record.entries = @($budget.Record.entries) + @($entry)
    Save-KmcWorkflowBudget $budget
    $env:KMC_WORKFLOW_BUDGET_ENTRY = $entryId
    Write-Host ('Workflow budget: admitted ' + $Kind + ' "' + $Selection + '" as ' + $entryId + '; ' + (Format-KmcWorkflowBudgetSummary $summary))
    return [pscustomobject]@{ EntryId = $entryId; Nested = $false; BudgetId = $id }
}

function Find-KmcWorkflowEntry($Budget, [string]$EntryId) {
    foreach ($entry in $Budget.Record.entries) { if ([string]$entry.entryId -ceq $EntryId) { return $entry } }
    return $null
}

function Update-KmcWorkflowEntry {
    # Heartbeat, plus the measured duration of one step for later planning.
    param([Parameter(Mandatory = $true)][string]$RepoRoot, $Admission, [string]$StepName, [Nullable[double]]$StepSeconds = $null)
    if ($null -eq $Admission -or $Admission.Nested) { return }
    $now = Get-KmcWorkflowClock
    $budget = Read-KmcWorkflowBudget $RepoRoot $Admission.BudgetId
    $entry = Find-KmcWorkflowEntry $budget $Admission.EntryId
    if ($null -ne $entry) { $entry | Add-Member -NotePropertyName heartbeatUtc -NotePropertyValue $now.ToString('o') -Force }
    if (-not [string]::IsNullOrWhiteSpace($StepName) -and $null -ne $StepSeconds) {
        $budget.Record.stepHistory | Add-Member -NotePropertyName $StepName -NotePropertyValue ([pscustomobject]@{ seconds = [Math]::Round($StepSeconds, 1); observedUtc = $now.ToString('o') }) -Force
    }
    Save-KmcWorkflowBudget $budget
}

function Complete-KmcWorkflowEntry {
    param([Parameter(Mandatory = $true)][string]$RepoRoot, $Admission, [ValidateSet('completed', 'failed')][string]$Status = 'completed')
    if ($null -eq $Admission -or $Admission.Nested) { return }
    $now = Get-KmcWorkflowClock
    $budget = Read-KmcWorkflowBudget $RepoRoot $Admission.BudgetId
    $entry = Find-KmcWorkflowEntry $budget $Admission.EntryId
    if ($null -ne $entry -and [string]$entry.status -eq 'running') {
        $started = [DateTime]::Parse([string]$entry.startedUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AdjustToUniversal)
        $entry | Add-Member -NotePropertyName status -NotePropertyValue $Status -Force
        $entry | Add-Member -NotePropertyName endedUtc -NotePropertyValue $now.ToString('o') -Force
        $entry | Add-Member -NotePropertyName seconds -NotePropertyValue ([Math]::Round([Math]::Max(0, ($now - $started).TotalSeconds), 1)) -Force
        $entry | Add-Member -NotePropertyName heartbeatUtc -NotePropertyValue $now.ToString('o') -Force
    }
    Save-KmcWorkflowBudget $budget
    if ($env:KMC_WORKFLOW_BUDGET_ENTRY -ceq $Admission.EntryId) { Remove-Item Env:KMC_WORKFLOW_BUDGET_ENTRY -ErrorAction SilentlyContinue }
    Write-Host ('Workflow budget: ' + $Status + ' ' + $Admission.EntryId + '; ' + (Format-KmcWorkflowBudgetSummary (Get-KmcWorkflowBudgetSummary $budget $now)))
}

function Get-KmcWorkflowStepHistorySeconds($Budget, [string]$StepName) {
    $history = $Budget.Record.stepHistory
    if ($null -eq $history) { return $null }
    $property = $history.PSObject.Properties[$StepName]
    if ($null -eq $property -or $null -eq $property.Value) { return $null }
    return [double]$property.Value.seconds
}
