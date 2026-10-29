[CmdletBinding()]
param()
# Offline orchestration checks for scripts/Test-Tiers.ps1 and scripts/WorkflowBudget.ps1 inside a
# private temporary boundary: every child is a tracing stub, the component runner is a stub, the
# budget record lives in the boundary and the clock is a file. Nothing is built, no game is
# launched and no lab state is touched.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$boundary = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) ('kmc-tier-selection-' + [Guid]::NewGuid().ToString('N'))))
$previous = @{}
foreach ($name in @('KMC_WORKFLOW_TEST_TRACE','KMC_WORKFLOW_BUDGET_ROOT','KMC_WORKFLOW_BUDGET_ID','KMC_WORKFLOW_BUDGET_ENTRY','KMC_WORKFLOW_TEST_CLOCK_FILE','KMC_WORKFLOW_TEST_DEAD_PIDS','KMC_WORKFLOW_TEST_ADVANCE_TO','KMC_WORKFLOW_NATIVE_BATCH')) {
    $previous[$name] = [Environment]::GetEnvironmentVariable($name)
    [Environment]::SetEnvironmentVariable($name, $null)
}
New-Item -ItemType Directory -Path (Join-Path $boundary 'scripts') -Force | Out-Null
$junction = $null
function Set-Clock([string]$Value) { Set-Content -LiteralPath $env:KMC_WORKFLOW_TEST_CLOCK_FILE -Value $Value -Encoding ASCII }
function Read-Calls([string]$Trace) { if (Test-Path -LiteralPath $Trace) { @(Get-Content -LiteralPath $Trace | ForEach-Object { $_ | ConvertFrom-Json }) } else { @() } }
function Read-Budget([string]$Root, [string]$Id) { $path = Join-Path $Root ($Id + '.json'); if (Test-Path -LiteralPath $path) { Get-Content -Raw -LiteralPath $path | ConvertFrom-Json } else { $null } }
function Use-BudgetRoot([string]$Name) { $root = Join-Path $boundary $Name; New-Item -ItemType Directory -Path $root -Force | Out-Null; $env:KMC_WORKFLOW_BUDGET_ROOT = $root; return $root }
function Write-Record([string]$Root, [hashtable[]]$Entries, [hashtable]$StepHistory = @{}) {
    @{ schemaVersion=1; budgetId='development'; allowanceMinutes=90; blockCeilingMinutes=30; createdUtc='2026-10-29T12:00:00Z'; stepHistory=$StepHistory; entries=$Entries } |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $Root 'development.json') -Encoding UTF8
}
try {
    $scripts = Join-Path $boundary 'scripts'
    $trace = Join-Path $boundary 'calls.jsonl'
    $clock = Join-Path $boundary 'clock.txt'
    $env:KMC_WORKFLOW_TEST_TRACE = $trace
    $env:KMC_WORKFLOW_TEST_CLOCK_FILE = $clock
    $budgetRoot = Use-BudgetRoot 'budgets'
    Set-Clock '2026-10-29T12:00:00Z'
    foreach ($name in @('Test-Tiers.ps1','Test.ps1','WorkflowBudget.ps1','Reconcile-WorkflowBudget.ps1','workflow-impact-map.json')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $scripts }
    # Discover the actual umbrella's children rather than a claimed plan.
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $scripts 'Test.ps1'), [ref]$tokens, [ref]$errors)
    if ($errors.Count -ne 0) { throw 'Umbrella parse failed.' }
    $children = @($ast.FindAll({ param($node) $node -is [Management.Automation.Language.StringConstantExpressionAst] -and $node.Value -like '*.ps1' }, $true) | ForEach-Object { $_.Value })
    $children += @('Validate-Source.ps1','Test-SelectedFixture.ps1','chunk7\Test-SelectedNested.ps1','Test-AdvanceClock.ps1','Test-Marker.ps1')
    foreach ($name in @($children | Select-Object -Unique)) {
        $path = Join-Path $scripts $name
        New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
        @'
[CmdletBinding()]
param([string]$Configuration='Release', [string]$Target='Both', [Parameter(ValueFromRemainingArguments=$true)][object[]]$Rest)
@{ script=[IO.Path]::GetFileName($PSCommandPath); configuration=$Configuration; target=$Target; entry=$env:KMC_WORKFLOW_BUDGET_ENTRY } | ConvertTo-Json -Compress | Add-Content -LiteralPath $env:KMC_WORKFLOW_TEST_TRACE
if ([IO.Path]::GetFileName($PSCommandPath) -eq 'Test-AdvanceClock.ps1') { Set-Content -LiteralPath $env:KMC_WORKFLOW_TEST_CLOCK_FILE -Value $env:KMC_WORKFLOW_TEST_ADVANCE_TO -Encoding ASCII }
$global:LASTEXITCODE=0
'@ | Set-Content -LiteralPath $path -Encoding ASCII
    }
    # A stub elsewhere, reachable only through a link or an escape.
    $outside = Join-Path $boundary 'outside'
    New-Item -ItemType Directory -Path $outside -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $scripts 'Test-SelectedFixture.ps1') -Destination (Join-Path $outside 'Test-Outside.ps1')
    $junction = Join-Path $scripts 'linked'
    & cmd.exe /c mklink /J "$junction" "$outside" | Out-Null
    if (-not (Test-Path -LiteralPath (Join-Path $junction 'Test-Outside.ps1'))) { throw 'Link fixture could not be created.' }
    $runner = Join-Path $boundary 'bin/Tests/Debug/KingmakerMountedCombat.Tests.exe'
    New-Item -ItemType Directory -Path (Split-Path -Parent $runner) -Force | Out-Null
    Add-Type -OutputAssembly $runner -OutputType ConsoleApplication -TypeDefinition @'
using System;
using System.IO;
public static class TierFixture {
  public static int Main() {
    File.AppendAllText(Environment.GetEnvironmentVariable("KMC_WORKFLOW_TEST_TRACE"), "{\"script\":\"components\",\"entry\":\"" + Environment.GetEnvironmentVariable("KMC_WORKFLOW_BUDGET_ENTRY") + "\"}" + Environment.NewLine);
    return 0;
  }
}
'@
    $selector = Join-Path $scripts 'Test-Tiers.ps1'
    $reconcile = Join-Path $scripts 'Reconcile-WorkflowBudget.ps1'
    $passed = 0
    function Count-Entries { $record = Read-Budget $env:KMC_WORKFLOW_BUDGET_ROOT 'development'; if ($null -eq $record) { 0 } else { @($record.entries).Count } }
    function Assert-Refused([hashtable]$Arguments, [string]$Expected) {
        $entriesBefore = Count-Entries
        $refused = $null
        try { & $selector @Arguments } catch { $refused = $_.Exception.Message }
        if ($null -eq $refused -or (Test-Path -LiteralPath $trace)) { throw ('Unsupported selection was not refused before work: ' + ($Arguments | ConvertTo-Json -Compress)) }
        if ($refused -notlike ('*' + $Expected + '*')) { throw ('Refusal gave the wrong reason for ' + ($Arguments | ConvertTo-Json -Compress) + ': ' + $refused) }
        if ((Count-Entries) -ne $entriesBefore) { throw 'A refused selection recorded a budget entry.' }
    }
    function Invoke-Selector([hashtable]$Arguments) {
        $failure = $null
        $output = @(try { & $selector @Arguments *>&1 | ForEach-Object { [string]$_ } } catch { $failure = $_.Exception.Message })
        return [pscustomobject]@{ Output = ($output -join "`n"); Failure = $failure }
    }
    foreach ($case in @(
        @{ Arguments = @{ Tier='Full'; Focused='Test-SelectedFixture.ps1'; QualificationTask='selection test' }; Expected = 'does not accept -Focused' },
        @{ Arguments = @{ Tier='Full' }; Expected = '-QualificationTask' },
        @{ Arguments = @{ Tier='Fast'; QualificationTask='selection test' }; Expected = 'selects the Full umbrella only' },
        @{ Arguments = @{ Tier='Fast'; Focused='Test-NotPresent.ps1' }; Expected = 'missing' },
        @{ Arguments = @{ Tier='Fast'; Focused=' , , ' }; Expected = 'requires at least one' },
        @{ Arguments = @{ Tier='Fast'; AssemblyTarget='Kingmaker' }; Expected = '-AssemblyTarget applies' },
        @{ Arguments = @{ Tier='Candidate'; LabRoot=$boundary }; Expected = '-LabRoot' },
        @{ Arguments = @{ Tier='Fast'; Focused='chunk7\..\..\Build-Local.ps1' }; Expected = 'traversal' },
        @{ Arguments = @{ Tier='Fast'; Focused='..\scripts\Test-SelectedFixture.ps1' }; Expected = 'traversal' },
        @{ Arguments = @{ Tier='Fast'; Focused=(Join-Path $outside 'Test-Outside.ps1') }; Expected = 'outside the scripts tree' },
        @{ Arguments = @{ Tier='Fast'; Focused='C:\Windows\System32\Test-Outside.ps1' }; Expected = 'outside the scripts tree' },
        @{ Arguments = @{ Tier='Fast'; Focused='linked\Test-Outside.ps1' }; Expected = 'contains a link' },
        @{ Arguments = @{ Tier='Fast'; Focused='Validate-Source.ps1' }; Expected = 'name is invalid' },
        @{ Arguments = @{ Tier='Fast'; Focused='chunk7' }; Expected = 'name is invalid' }
    )) { Assert-Refused $case.Arguments $case.Expected; $passed++ }

    # Plan only: resolved steps, prerequisites, allowance; nothing runs and nothing is recorded.
    $plan = (Invoke-Selector @{ Plan=$true; Tier='Candidate'; SkipBuild=$true; Configuration='Debug'; Focused='chunk7/Test-SelectedNested.ps1,Test-SelectedFixture.ps1' }).Output
    if ($plan -notmatch 'PLAN tier=Candidate' -or $plan -notmatch 'assemblyTarget=Kingmaker' -or $plan -notmatch 'FOCUSED chunk7\\Test-SelectedNested\.ps1' -or
        $plan -notmatch 'FOCUSED Test-SelectedFixture\.ps1' -or $plan -notmatch 'CANDIDATE assembly Kingmaker' -or $plan -notmatch 'last=unknown' -or
        $plan -notmatch 'estimate=unknown' -or $plan -notmatch 'spent 0 of 90 min' -or $plan -notmatch 'plan only') { throw ('Plan output is incomplete: ' + $plan) }
    if ((Test-Path -LiteralPath $trace) -or (Count-Entries) -ne 0) { throw 'Plan-only invocation ran work or recorded a budget entry.' }
    $passed++

    # Nested repository-relative paths in every accepted spelling run once; Kingmaker is the default target.
    & $selector -Tier Candidate -SkipBuild -Configuration Debug -Focused 'chunk7/Test-SelectedNested.ps1,scripts\chunk7\Test-SelectedNested.ps1,.\scripts\chunk7\Test-SelectedNested.ps1,Test-SelectedFixture.ps1,Test-Harness.ps1,Test-AssemblyContracts.ps1,Test-SelectedFixture.ps1'
    $calls = Read-Calls $trace
    foreach ($group in @($calls | Group-Object script)) { if ($group.Count -ne 1) { throw "Repeated child: $($group.Name)" } }
    $assembly = @($calls | Where-Object script -eq 'Test-AssemblyContracts.ps1')
    if ($assembly.Count -ne 1 -or $assembly[0].target -cne 'Kingmaker' -or $assembly[0].configuration -cne 'Debug' -or
        @($calls | Where-Object script -eq 'components').Count -ne 1 -or
        @($calls | Where-Object script -eq 'Build-Local.ps1').Count -ne 0 -or
        @($calls | Where-Object script -eq 'Test-SelectedFixture.ps1').Count -ne 1 -or
        @($calls | Where-Object script -eq 'Test-SelectedNested.ps1').Count -ne 1) { throw 'Candidate propagation/deduplication failed.' }
    $budget = Read-Budget $budgetRoot 'development'
    if ($null -eq $budget -or @($budget.entries).Count -ne 1 -or [string]$budget.entries[0].status -cne 'completed' -or [string]$budget.entries[0].kind -cne 'tier' -or
        $null -eq $budget.stepHistory.'CANDIDATE assembly Kingmaker') { throw 'The tier run was not recorded in the shared budget with its step durations.' }
    $completedEntryId = [string]$budget.entries[0].entryId
    if (@($calls | Where-Object { [string]$_.entry -cne $completedEntryId }).Count -ne 0) { throw 'Children did not inherit the budget entry.' }
    if (-not [string]::IsNullOrWhiteSpace($env:KMC_WORKFLOW_BUDGET_ENTRY)) { throw 'The selector did not restore the caller''s entry context.' }
    $passed++
    Remove-Item -LiteralPath $trace

    # Recorded durations make the next plan's estimate known.
    $plan2 = (Invoke-Selector @{ Plan=$true; Tier='Candidate'; SkipBuild=$true; Configuration='Debug' }).Output
    if ($plan2 -notmatch 'estimate=\d+s from recorded durations' -or $plan2 -match 'last=unknown') { throw ('Second plan did not use recorded durations: ' + $plan2) }
    $passed++

    # Explicit Wrath/Both selection still works; Full forwards target/configuration/SkipBuild only when its task is named.
    & $selector -Tier Full -SkipBuild -Configuration Debug -AssemblyTarget Both -QualificationTask 'selection test'
    $calls = Read-Calls $trace
    $assembly = @($calls | Where-Object script -eq 'Test-AssemblyContracts.ps1')
    if ($assembly.Count -ne 1 -or $assembly[0].target -cne 'Both' -or
        @($calls | Where-Object script -eq 'Build-Local.ps1').Count -ne 0 -or
        @($calls | Where-Object script -eq 'components').Count -ne 1) { throw 'Full did not forward target/configuration/SkipBuild through the actual umbrella.' }
    $budget = Read-Budget $budgetRoot 'development'
    if (@($budget.entries).Count -ne 2 -or [string]$budget.entries[1].selection -notlike 'Full:*') { throw 'The Full run was not recorded.' }
    $passed++
    Remove-Item -LiteralPath $trace

    # Inheritance is verified, never assumed. A live running parent (this process, admitted
    # through the library) is inherited; a completed, missing or dead parent is stale context
    # and the invocation is accounted on its own.
    . (Join-Path $scripts 'WorkflowBudget.ps1')
    $parent = Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind tier -Selection 'parent wrapper'
    $live = Invoke-Selector @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' }
    if ($live.Failure -or $live.Output -notmatch 'nested under live entry' -or (Count-Entries) -ne 3) { throw ('A live parent was not inherited: ' + $live.Output + $live.Failure) }
    if (@(Read-Calls $trace | Where-Object { [string]$_.entry -cne $parent.EntryId }).Count -ne 0) { throw 'A nested invocation did not propagate the live parent entry.' }
    if ($env:KMC_WORKFLOW_BUDGET_ENTRY -cne $parent.EntryId) { throw 'The nested invocation did not restore the parent context.' }
    Complete-KmcWorkflowEntry -RepoRoot $boundary -Admission $parent -Status completed
    Remove-Item -LiteralPath $trace
    foreach ($stale in @(@{ Entry = $completedEntryId; Expected = 'completed' }, @{ Entry = 'development:x:1:ffffffff'; Expected = 'missing' })) {
        $env:KMC_WORKFLOW_BUDGET_ENTRY = $stale.Entry
        $before = Count-Entries
        $result = Invoke-Selector @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' }
        [Environment]::SetEnvironmentVariable('KMC_WORKFLOW_BUDGET_ENTRY', $null)
        if ($result.Failure -or $result.Output -notmatch ('is ' + $stale.Expected + ', not a live parent') -or (Count-Entries) -ne $before + 1) { throw ('A ' + $stale.Expected + ' inherited entry was not treated as stale: ' + $result.Output + $result.Failure) }
        Remove-Item -LiteralPath $trace
    }
    $passed++

    # A step after the one that exhausts the allowance never starts: the exhausting step settles,
    # the following marker stub is refused before it runs, and the entry is accounted.
    $env:KMC_WORKFLOW_TEST_ADVANCE_TO = '2026-10-29T13:35:00Z'
    $exhaust = Invoke-Selector @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-AdvanceClock.ps1,Test-Marker.ps1' }
    $calls = Read-Calls $trace
    if (-not $exhaust.Failure -or $exhaust.Failure -notmatch 'refused step' -or $exhaust.Output -notmatch 'REFUSED FOCUSED Test-Marker\.ps1: Workflow budget exhausted' -or
        @($calls | Where-Object script -eq 'Test-AdvanceClock.ps1').Count -ne 1 -or @($calls | Where-Object script -eq 'Test-Marker.ps1').Count -ne 0) { throw ('The step after exhaustion was started: ' + $exhaust.Output + $exhaust.Failure) }
    $budget = Read-Budget $budgetRoot 'development'
    $last = $budget.entries[-1]
    if ([string]$last.status -cne 'failed' -or [double]$last.seconds -lt 90 * 60) { throw 'The exhausting run was not accounted.' }
    Remove-Item -LiteralPath $trace
    Assert-Refused @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' } 'Workflow budget exhausted'
    $planAfter = (Invoke-Selector @{ Plan=$true; Tier='Fast'; SkipBuild=$true; Configuration='Debug' }).Output
    if ($planAfter -notmatch 'remaining 0 min') { throw ('Plan did not report the exhausted allowance: ' + $planAfter) }
    # A new shell, commit or wrapper does not replenish it; only an explicitly selected new budget admits more work.
    Assert-Refused @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1'; BudgetId='development' } 'Workflow budget exhausted'
    & $selector -Tier Fast -SkipBuild -Configuration Debug -Focused 'Test-SelectedFixture.ps1' -BudgetId 'sprint1-selected-check' | Out-Null
    if (@((Read-Budget $budgetRoot 'sprint1-selected-check').entries).Count -ne 1) { throw 'An explicitly selected budget did not admit the check.' }
    $passed++
    Remove-Item -LiteralPath $trace

    # The ordinary-block ceiling: a block that has run 31 minutes (well inside the allowance)
    # does not start its next step either.
    $blockRoot = Use-BudgetRoot 'budgets-block'
    Set-Clock '2026-10-29T12:00:00Z'
    $env:KMC_WORKFLOW_TEST_ADVANCE_TO = '2026-10-29T12:31:00Z'
    $block = Invoke-Selector @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-AdvanceClock.ps1,Test-Marker.ps1' }
    $calls = Read-Calls $trace
    if (-not $block.Failure -or $block.Output -notmatch 'REFUSED FOCUSED Test-Marker\.ps1: Workflow block ceiling reached' -or
        @($calls | Where-Object script -eq 'Test-Marker.ps1').Count -ne 0) { throw ('The block ceiling did not stop the next step: ' + $block.Output + $block.Failure) }
    $budget = Read-Budget $blockRoot 'development'
    if ([string]$budget.entries[0].status -cne 'failed' -or [double]$budget.entries[0].seconds -ne 31 * 60) { throw 'The block-ceiling run was not accounted.' }
    Remove-Item -LiteralPath $trace
    # 59 minutes remain, so a further selection is admitted on its own fresh block.
    & $selector -Tier Fast -SkipBuild -Configuration Debug -Focused 'Test-SelectedFixture.ps1' | Out-Null
    if ((Count-Entries) -ne 2) { throw 'A fresh block was not admitted after a block-ceiling refusal.' }
    $passed++
    Remove-Item -LiteralPath $trace

    # Known oversized work is refused, not warned about: a recorded 95-minute component step
    # against a fresh 90-minute allowance never starts; a selection whose known 40-minute step
    # exceeds the 10 minutes left is refused at admission before any step; and a step whose
    # known 15 minutes exceed what is left of the 30-minute block is refused at its own boundary.
    $sizeRoot = Use-BudgetRoot 'budgets-size'
    Set-Clock '2026-10-29T12:00:00Z'
    Write-Record $sizeRoot @() @{ 'FAST components' = @{ seconds = 95 * 60; observedUtc = '2026-10-29T11:00:00Z' } }
    Assert-Refused @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' } 'refuses oversized work'
    Write-Record $sizeRoot @(@{ entryId='development:a:0:00000001'; kind='tier'; selection='earlier'; startedUtc='2026-10-29T10:00:00Z'; heartbeatUtc='2026-10-29T11:20:00Z'; stepStartedUtc=$null; processId=0; status='completed'; estimatedSeconds=$null; endedUtc='2026-10-29T11:20:00Z'; seconds=80*60; launch=$false; batchId=$null; batchKey=$null }) `
        @{ 'FOCUSED Test-Marker.ps1' = @{ seconds = 40 * 60; observedUtc = '2026-10-29T11:00:00Z' } }
    Assert-Refused @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1,Test-Marker.ps1' } 'refuses oversized work'
    $blockSizeRoot = Use-BudgetRoot 'budgets-block-size'
    Write-Record $blockSizeRoot @() @{ 'FOCUSED Test-Marker.ps1' = @{ seconds = 15 * 60; observedUtc = '2026-10-29T11:00:00Z' } }
    $env:KMC_WORKFLOW_TEST_ADVANCE_TO = '2026-10-29T12:20:00Z'
    $oversized = Invoke-Selector @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-AdvanceClock.ps1,Test-Marker.ps1' }
    $calls = Read-Calls $trace
    if (-not $oversized.Failure -or $oversized.Output -notmatch 'REFUSED FOCUSED Test-Marker\.ps1: Workflow block ceiling refuses oversized work' -or
        @($calls | Where-Object script -eq 'Test-AdvanceClock.ps1').Count -ne 1 -or @($calls | Where-Object script -eq 'Test-Marker.ps1').Count -ne 0) { throw ('An oversized step was started inside a block: ' + $oversized.Output + $oversized.Failure) }
    $passed++
    Remove-Item -LiteralPath $trace

    # Interrupted records: a running entry whose process is gone is charged to its last measured
    # point before admission; an unmeasured interval with no estimate blocks admission until it
    # is reconciled explicitly, and an estimate is charged when nothing was measured.
    $interruptedRoot = Use-BudgetRoot 'budgets-interrupted'
    Set-Clock '2026-10-29T14:00:00Z'
    Write-Record $interruptedRoot @(@{ entryId='development:a:999999:00000002'; kind='native'; selection='persistence-p05-save/quick'; startedUtc='2026-10-29T12:00:00Z'; heartbeatUtc='2026-10-29T12:40:00Z'; stepStartedUtc=$null; processId=999999; status='running'; estimatedSeconds=$null; endedUtc=$null; seconds=$null; launch=$true; batchId='development:batch1'; batchKey='persistence-p05-save/quick' })
    $output = (Invoke-Selector @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' }).Output
    $budget = Read-Budget $interruptedRoot 'development'
    if ($output -notmatch 'reconciled 1 interrupted entry' -or [string]$budget.entries[0].status -cne 'interrupted' -or [double]$budget.entries[0].seconds -ne 2400 -or
        @($budget.entries).Count -ne 2 -or $output -notmatch 'spent 40 of 90 min') { throw ('Interrupted entry was not reconciled before admission: ' + $output) }
    Remove-Item -LiteralPath $trace
    Write-Record $interruptedRoot @(@{ entryId='development:b:999999:00000003'; kind='tier'; selection='unmeasured'; startedUtc='2026-10-29T12:00:00Z'; heartbeatUtc='2026-10-29T12:00:00Z'; stepStartedUtc=$null; processId=999999; status='running'; estimatedSeconds=$null; endedUtc=$null; seconds=$null; launch=$false; batchId=$null; batchKey=$null })
    Assert-Refused @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' } 'unknown spend'
    & $reconcile -EntryId 'development:b:999999:00000003' -Seconds 600 -Reason 'transcript shows ten minutes' | Out-Null
    & $selector -Tier Fast -SkipBuild -Configuration Debug -Focused 'Test-SelectedFixture.ps1' | Out-Null
    $budget = Read-Budget $interruptedRoot 'development'
    if ([string]$budget.entries[0].status -cne 'interrupted' -or [double]$budget.entries[0].seconds -ne 600 -or @($budget.entries).Count -ne 2) { throw 'Explicit reconciliation did not admit the next check.' }
    Remove-Item -LiteralPath $trace
    Write-Record $interruptedRoot @(@{ entryId='development:c:999999:00000004'; kind='native'; selection='x'; startedUtc='2026-10-29T12:00:00Z'; heartbeatUtc='2026-10-29T12:00:00Z'; stepStartedUtc=$null; processId=999999; status='running'; estimatedSeconds=360; endedUtc=$null; seconds=$null; launch=$true; batchId='development:batch1'; batchKey='x' })
    $output = (Invoke-Selector @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' }).Output
    $budget = Read-Budget $interruptedRoot 'development'
    if ([double]$budget.entries[0].seconds -ne 360 -or [string]$budget.entries[0].reconciliation -cne 'charged-estimate-unmeasured') { throw 'An unmeasured interrupted estimate was not charged.' }
    # Imported work (run before the record existed) counts like any other spend.
    & $reconcile -ImportKind tier -ImportSelection 'pre-record Fast run' -ImportSeconds 120 -ImportStartedUtc '2026-10-29T11:00:00Z' -Reason 'transcript' | Out-Null
    $budget = Read-Budget $interruptedRoot 'development'
    if (@($budget.entries | Where-Object { $null -ne $_.PSObject.Properties['reconciliation'] -and [string]$_.reconciliation -like 'imported:*' }).Count -ne 1 -or ($budget.entries | ForEach-Object { [double]$_.seconds } | Measure-Object -Sum).Sum -lt 480) { throw 'Imported work was not accounted.' }
    $passed++
    Remove-Item -LiteralPath $trace

    # The native batch rule at admission: five distinct scenarios per batch, two equivalent
    # failures per scenario, decided before any lock.
    $batchRoot = Use-BudgetRoot 'budgets-batch'
    Set-Clock '2026-10-29T15:00:00Z'
    foreach ($key in @('a/1','b/1','c/1','d/1','e/1')) {
        $admission = Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection ($key + '/launch/r') -EstimatedSeconds 60 -Launch -BatchKey $key
        Complete-KmcWorkflowEntry -RepoRoot $boundary -Admission $admission -Status completed
    }
    $sixth = $null
    try { [void](Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection 'f/1/launch/r' -EstimatedSeconds 60 -Launch -BatchKey 'f/1') } catch { $sixth = $_.Exception.Message }
    if ($sixth -notlike '*already names 5 scenarios*') { throw ('A sixth scenario was admitted into the batch: ' + $sixth) }
    $dry = Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection 'f/1/whatif/r' -BatchKey 'f/1'
    Complete-KmcWorkflowEntry -RepoRoot $boundary -Admission $dry -Status completed
    foreach ($attempt in 1..2) { $failed = Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection ('a/1/launch/r' + $attempt) -EstimatedSeconds 60 -Launch -BatchKey 'a/1'; Complete-KmcWorkflowEntry -RepoRoot $boundary -Admission $failed -Status failed }
    $third = $null
    try { [void](Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection 'a/1/launch/r3' -EstimatedSeconds 60 -Launch -BatchKey 'a/1') } catch { $third = $_.Exception.Message }
    if ($third -notlike '*third equivalent attempt*') { throw ('A third equivalent failure was admitted: ' + $third) }
    $env:KMC_WORKFLOW_NATIVE_BATCH = 'development:batch2'
    $fresh = Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection 'f/1/launch/r' -EstimatedSeconds 60 -Launch -BatchKey 'f/1'
    Complete-KmcWorkflowEntry -RepoRoot $boundary -Admission $fresh -Status completed
    [Environment]::SetEnvironmentVariable('KMC_WORKFLOW_NATIVE_BATCH', $null)
    $passed++

    # A launcher-shaped early failure (admitted, then a terminating error before its own
    # try/finally) still completes its entry and restores the shell's context, so the next
    # invocation in the same shell is metered on its own rather than riding a stale entry.
    $trapRoot = Use-BudgetRoot 'budgets-trap'
    Set-Clock '2026-10-29T16:00:00Z'
    $early = Join-Path $boundary 'early-failure.ps1'
    @'
param([string]$Boundary)
$ErrorActionPreference = 'Stop'
. (Join-Path $Boundary 'scripts\WorkflowBudget.ps1')
$workflowAdmission = Request-KmcWorkflowAdmission -RepoRoot $Boundary -Kind native -Selection 'early/case/launch/r1' -EstimatedSeconds 60 -Launch -BatchKey 'early/case'
trap { try { Complete-KmcWorkflowEntry -RepoRoot $Boundary -Admission $workflowAdmission -Status failed } catch { }; break }
throw 'Runtime request branch is outside the KMC prefix.'
'@ | Set-Content -LiteralPath $early -Encoding ASCII
    $env:KMC_WORKFLOW_BUDGET_ENTRY = $null
    $earlyFailure = $null
    try { & $early -Boundary $boundary *>&1 | Out-Null } catch { $earlyFailure = $_.Exception.Message }
    $budget = Read-Budget $trapRoot 'development'
    if ($earlyFailure -notlike '*KMC prefix*' -or @($budget.entries).Count -ne 1 -or [string]$budget.entries[0].status -cne 'failed' -or -not [string]::IsNullOrWhiteSpace($env:KMC_WORKFLOW_BUDGET_ENTRY)) { throw ('An early launcher failure left the budget entry running or the context installed: ' + $earlyFailure) }
    & $selector -Tier Fast -SkipBuild -Configuration Debug -Focused 'Test-SelectedFixture.ps1' | Out-Null
    $budget = Read-Budget $trapRoot 'development'
    if (@($budget.entries).Count -ne 2 -or [string]$budget.entries[1].status -cne 'completed') { throw 'The invocation after an early failure was not metered on its own.' }
    $passed++
    Remove-Item -LiteralPath $trace

    # The admission function itself refuses before a caller takes any lock: the native launcher
    # calls it before validation, Steam checks, evidence directories, locks, staging or launch.
    $lockSentinel = Join-Path $boundary 'active-transaction.lock'
    $env:KMC_WORKFLOW_BUDGET_ROOT = $budgetRoot
    $refusedBeforeLock = $false
    try { [void](Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection 'persistence-p05-save/quick/launch/r' -EstimatedSeconds 360 -Launch -BatchKey 'persistence-p05-save/quick'); New-Item -ItemType File -Path $lockSentinel | Out-Null }
    catch { $refusedBeforeLock = $_.Exception.Message -like '*Workflow budget exhausted*' }
    if (-not $refusedBeforeLock -or (Test-Path -LiteralPath $lockSentinel)) { throw 'An exhausted budget did not refuse before the lock step.' }
    $passed++

    # Nested native attempts still count: under a live parent, each real launch shares the
    # parent's timing but is admitted against the batch's five selections, limited to two
    # failures per selection, and records its own outcome under the parent. A nested dry run
    # records no attempt. Time is counted once, in the parent, whose entry the child never completes.
    $nestedRoot = Use-BudgetRoot 'budgets-nested'
    Set-Clock '2026-10-29T18:00:00Z'
    $liveParent = Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind tier -Selection 'parent wrapper with native children'
    $tick = [DateTime]::Parse('2026-10-29T18:00:00Z', [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal)
    foreach ($key in @('a/1','b/1','c/1','d/1','e/1')) {
        $child = Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection ($key + '/launch/r1') -EstimatedSeconds 60 -Launch -BatchKey $key
        if (-not $child.Nested -or $child.EntryId -cne $liveParent.EntryId -or [string]::IsNullOrWhiteSpace([string]$child.AttemptEntryId) -or $env:KMC_WORKFLOW_BUDGET_ENTRY -cne $liveParent.EntryId) { throw 'A nested native launch was not recorded as an attempt under its live parent.' }
        $tick = $tick.AddSeconds(120); Set-Clock $tick.ToString('o')
        Complete-KmcWorkflowEntry -RepoRoot $boundary -Admission $child -Status completed
        if ($env:KMC_WORKFLOW_BUDGET_ENTRY -cne $liveParent.EntryId) { throw 'Completing a nested attempt did not restore the parent context.' }
    }
    $sixthNested = $null
    try { [void](Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection 'f/1/launch/r1' -EstimatedSeconds 60 -Launch -BatchKey 'f/1') } catch { $sixthNested = $_.Exception.Message }
    if ($sixthNested -notlike '*already names 5 scenarios*') { throw ('A sixth nested scenario was admitted into the batch: ' + $sixthNested) }
    $nestedDry = Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection 'f/1/whatif/r1' -BatchKey 'f/1'
    if (-not $nestedDry.Nested -or -not [string]::IsNullOrWhiteSpace([string]$nestedDry.AttemptEntryId)) { throw 'A nested dry run was recorded as a native attempt.' }
    Complete-KmcWorkflowEntry -RepoRoot $boundary -Admission $nestedDry -Status completed
    foreach ($attempt in 1..2) { $failedChild = Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection ('a/1/launch/r' + ($attempt + 1)) -EstimatedSeconds 60 -Launch -BatchKey 'a/1'; Complete-KmcWorkflowEntry -RepoRoot $boundary -Admission $failedChild -Status failed }
    $thirdNested = $null
    try { [void](Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection 'a/1/launch/r4' -EstimatedSeconds 60 -Launch -BatchKey 'a/1') } catch { $thirdNested = $_.Exception.Message }
    if ($thirdNested -notlike '*third equivalent attempt*') { throw ('A third equivalent nested failure was admitted: ' + $thirdNested) }
    $budget = Read-Budget $nestedRoot 'development'
    $attempts = @($budget.entries | Where-Object { $null -ne $_.PSObject.Properties['parentEntryId'] -and [string]$_.parentEntryId -ceq $liveParent.EntryId })
    $parentEntry = @($budget.entries | Where-Object { [string]$_.entryId -ceq $liveParent.EntryId })[0]
    if ($attempts.Count -ne 7 -or @($attempts | Where-Object { [string]$_.status -ceq 'completed' }).Count -ne 5 -or @($attempts | Where-Object { [string]$_.status -ceq 'failed' }).Count -ne 2 -or
        @($attempts | Where-Object { [double]$_.seconds -ne 0 -or -not $_.launch -or [string]$_.batchId -cne 'development:batch1' }).Count -ne 0 -or [string]$parentEntry.status -cne 'running' -or
        @($budget.entries).Count -ne 8) { throw 'Nested attempts were not recorded exactly once each under the running parent with no spend of their own.' }
    $planNested = (Invoke-Selector @{ Plan=$true; Tier='Fast'; SkipBuild=$true; Configuration='Debug' }).Output
    if ($planNested -notmatch 'spent 10 of 90 min') { throw ('Nested attempts changed the single-counted parent time: ' + $planNested) }
    Complete-KmcWorkflowEntry -RepoRoot $boundary -Admission $liveParent -Status completed
    $budget = Read-Budget $nestedRoot 'development'
    if ([string](@($budget.entries | Where-Object { [string]$_.entryId -ceq $liveParent.EntryId })[0].status) -cne 'completed' -or [double](@($budget.entries | Where-Object { [string]$_.entryId -ceq $liveParent.EntryId })[0].seconds) -ne 600) { throw 'The parent entry was not completed with its own single-counted time.' }
    # A dead parent with a still-"running" attempt: the parent is charged, the attempt is a failed
    # attempt counted in its parent, and the next admission sees exactly the parent's spend.
    $deadRoot = Use-BudgetRoot 'budgets-nested-dead'
    Set-Clock '2026-10-29T19:00:00Z'
    Write-Record $deadRoot @(
        @{ entryId='development:p:999999:00000005'; kind='tier'; selection='dead parent'; startedUtc='2026-10-29T18:00:00Z'; heartbeatUtc='2026-10-29T18:10:00Z'; stepStartedUtc=$null; processId=999999; status='running'; estimatedSeconds=$null; endedUtc=$null; seconds=$null; launch=$false; batchId=$null; batchKey=$null; parentEntryId=$null; suspendedUtc=$null; excludedSeconds=0 },
        @{ entryId='development:p:999999:00000005:attempt:1'; kind='native'; selection='x/1/launch/r1'; startedUtc='2026-10-29T18:02:00Z'; heartbeatUtc='2026-10-29T18:02:00Z'; stepStartedUtc=$null; processId=999999; status='running'; estimatedSeconds=60; endedUtc=$null; seconds=$null; launch=$true; batchId='development:batch1'; batchKey='x/1'; parentEntryId='development:p:999999:00000005' })
    $deadOutput = (Invoke-Selector @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' }).Output
    $budget = Read-Budget $deadRoot 'development'
    if ($deadOutput -notmatch 'reconciled 2 interrupted entries' -or $deadOutput -notmatch 'spent 10 of 90 min' -or [string]$budget.entries[1].reconciliation -cne 'counted-in-parent' -or [double]$budget.entries[1].seconds -ne 0 -or [double]$budget.entries[0].seconds -ne 600) { throw ('A dead parent and its attempt were not reconciled as one spend: ' + $deadOutput) }
    Remove-Item -LiteralPath $trace
    $passed++

    # Recheck before the game: a launcher-shaped stub admits, runs a preflight that advances the
    # clock, re-admits before its lock and again immediately before its launch marker. Exhaustion
    # during preflight leaves no lock and no launch; exhaustion after the lock (during staging)
    # leaves no launch, and the stub's own finally path restores (removes) the lock it owned.
    $launcherStub = Join-Path $boundary 'launcher-shaped.ps1'
    @'
param([string]$Boundary, [string]$AdvanceAt, [string]$AdvanceTo, [string]$Markers)
$ErrorActionPreference = 'Stop'
. (Join-Path $Boundary 'scripts\WorkflowBudget.ps1')
$admission = Request-KmcWorkflowAdmission -RepoRoot $Boundary -Kind native -Selection 'stub/case/launch/r1' -EstimatedSeconds 60 -Launch -BatchKey 'stub/case'
trap { try { Complete-KmcWorkflowEntry -RepoRoot $Boundary -Admission $admission -Status failed } catch { }; break }
function Advance([string]$Phase) { if ($AdvanceAt -ceq $Phase) { Set-Content -LiteralPath $env:KMC_WORKFLOW_TEST_CLOCK_FILE -Value $AdvanceTo -Encoding ASCII } }
Advance 'preflight'
Update-KmcWorkflowEntry -RepoRoot $Boundary -Admission $admission
Assert-KmcWorkflowStepAdmission -RepoRoot $Boundary -Admission $admission -StepName 'transaction lock stub/case'
$lock = Join-Path $Markers 'lock'
New-Item -ItemType File -Path $lock | Out-Null
try {
    Advance 'staging'
    Assert-KmcWorkflowStepAdmission -RepoRoot $Boundary -Admission $admission -StepName 'game launch stub/case' -EstimatedSeconds 60
    New-Item -ItemType File -Path (Join-Path $Markers 'launch') | Out-Null
}
finally {
    Suspend-KmcWorkflowEntry -RepoRoot $Boundary -Admission $admission
    Remove-Item -LiteralPath $lock -Force
    New-Item -ItemType File -Path (Join-Path $Markers 'restored') | Out-Null
    Resume-KmcWorkflowEntry -RepoRoot $Boundary -Admission $admission
}
Complete-KmcWorkflowEntry -RepoRoot $Boundary -Admission $admission -Status completed
'@ | Set-Content -LiteralPath $launcherStub -Encoding ASCII
    foreach ($phase in @('preflight', 'staging')) {
        $recheckRoot = Use-BudgetRoot ('budgets-recheck-' + $phase)
        Set-Clock '2026-10-29T20:00:00Z'
        $markers = Join-Path $boundary ('markers-' + $phase); New-Item -ItemType Directory -Path $markers -Force | Out-Null
        $env:KMC_WORKFLOW_BUDGET_ENTRY = $null
        $recheckFailure = $null
        try { & $launcherStub -Boundary $boundary -AdvanceAt $phase -AdvanceTo '2026-10-29T21:35:00Z' -Markers $markers *>&1 | Out-Null } catch { $recheckFailure = $_.Exception.Message }
        $budget = Read-Budget $recheckRoot 'development'
        if ($recheckFailure -notlike '*Workflow budget exhausted*' -or (Test-Path -LiteralPath (Join-Path $markers 'launch')) -or (Test-Path -LiteralPath (Join-Path $markers 'lock')) -or
            [string]$budget.entries[0].status -cne 'failed' -or -not [string]::IsNullOrWhiteSpace($env:KMC_WORKFLOW_BUDGET_ENTRY)) { throw ('Exhaustion during ' + $phase + ' did not stop the launch: ' + $recheckFailure) }
        if ($phase -eq 'staging' -and -not (Test-Path -LiteralPath (Join-Path $markers 'restored'))) { throw 'A refusal after the lock did not let the finally path restore the owned transaction.' }
        if ($phase -eq 'preflight' -and (Test-Path -LiteralPath (Join-Path $markers 'restored'))) { throw 'A refusal before the lock entered the transaction.' }
    }
    # The real launcher keeps that order: lock re-admission before the lock, launch re-admission
    # before Start-Process, suspension before restoration, resumption before the final reader,
    # and completion only after the reader's verdict.
    $launcherText = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1')
    function Find-Launcher([string]$Text, [string]$Needle, [switch]$Last) { $index = if ($Last) { $Text.LastIndexOf($Needle, [StringComparison]::Ordinal) } else { $Text.IndexOf($Needle, [StringComparison]::Ordinal) }; if ($index -lt 0) { throw ('Launcher text lacks: ' + $Needle) }; return $index }
    $lockAdmission = Find-Launcher $launcherText "Assert-KmcWorkflowStepAdmission -RepoRoot `$repoRoot -Admission `$workflowAdmission -StepName ('transaction lock '"
    $lockOpen = Find-Launcher $launcherText '$lock=Open-KmcRuntimeLock $runtimeState $actualRunId'
    $launchAdmission = Find-Launcher $launcherText "Assert-KmcWorkflowStepAdmission -RepoRoot `$repoRoot -Admission `$workflowAdmission -StepName ('game launch '"
    $launchStart = Find-Launcher $launcherText 'Start-Process -FilePath $SteamPath -ArgumentList $arguments'
    $suspend = Find-Launcher $launcherText 'Suspend-KmcWorkflowEntry -RepoRoot $repoRoot -Admission $workflowAdmission'
    $resume = Find-Launcher $launcherText 'Resume-KmcWorkflowEntry -RepoRoot $repoRoot -Admission $workflowAdmission'
    $reader = Find-Launcher $launcherText "scripts\runtime\Test-RuntimeResult.ps1"
    $completion = Find-Launcher $launcherText 'Complete-KmcWorkflowEntry -RepoRoot $repoRoot -Admission $workflowAdmission -Status completed' -Last
    $earlyCompletion = $launcherText.IndexOf('Complete-KmcWorkflowEntry', $suspend, $reader - $suspend, [StringComparison]::Ordinal)
    if (-not ($lockAdmission -lt $lockOpen -and $lockOpen -lt $launchAdmission -and $launchAdmission -lt $launchStart -and $launchStart -lt $suspend -and $suspend -lt $resume -and $resume -lt $reader -and $reader -lt $completion) -or $earlyCompletion -ge 0) {
        throw 'The native launcher does not re-admit before its lock and launch, suspend for restoration, and complete only after its final reader.'
    }
    $passed++

    # Final failures and reader time: the entry stays pending through restoration (suspended,
    # excluded) and the reader (metered); the recorded terminal result is the actual final verdict.
    $terminalRoot = Use-BudgetRoot 'budgets-terminal'
    $start = [DateTime]::Parse('2026-10-29T22:00:00Z', [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal)
    $at = $start
    foreach ($variant in @('reader-fail', 'restoration-fail', 'success', 'completed-while-suspended')) {
        Set-Clock $at.ToString('o')
        $env:KMC_WORKFLOW_BUDGET_ENTRY = 'development:x:1:ffffffff'
        $run = Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection ('terminal/' + $variant + '/launch/r1') -EstimatedSeconds 60 -Launch -BatchKey ('terminal/' + $(if ($variant -like '*fail*') { 'failing' } else { $variant }))
        $at = $at.AddSeconds(60); Set-Clock $at.ToString('o')             # the game ran 60 s
        Suspend-KmcWorkflowEntry -RepoRoot $boundary -Admission $run
        $at = $at.AddSeconds(20); Set-Clock $at.ToString('o')             # restoration took 20 s (excluded)
        if ($variant -ne 'completed-while-suspended') {
            Resume-KmcWorkflowEntry -RepoRoot $boundary -Admission $run
            $at = $at.AddSeconds(5); Set-Clock $at.ToString('o')          # the reader took 5 s (metered)
        }
        Complete-KmcWorkflowEntry -RepoRoot $boundary -Admission $run -Status $(if ($variant -like '*fail*') { 'failed' } else { 'completed' })
        if ($env:KMC_WORKFLOW_BUDGET_ENTRY -cne 'development:x:1:ffffffff') { throw 'Completion did not restore the previous context.' }
        $budget = Read-Budget $terminalRoot 'development'
        $entry = @($budget.entries | Where-Object { [string]$_.entryId -ceq $run.EntryId })[0]
        $expectedSeconds = if ($variant -eq 'completed-while-suspended') { 60 } else { 65 }
        if ([string]$entry.status -cne $(if ($variant -like '*fail*') { 'failed' } else { 'completed' }) -or [double]$entry.seconds -ne $expectedSeconds -or [double]$entry.excludedSeconds -ne 20 -or -not [string]::IsNullOrWhiteSpace([string]$entry.suspendedUtc)) {
            throw ('Terminal accounting differs for ' + $variant + ': status=' + $entry.status + ' seconds=' + $entry.seconds + ' excluded=' + $entry.excludedSeconds)
        }
        $at = $at.AddSeconds(5)
    }
    [Environment]::SetEnvironmentVariable('KMC_WORKFLOW_BUDGET_ENTRY', $null)
    # Two late failures of one selection are two failed attempts: the third is refused.
    $thirdTerminal = $null
    try { [void](Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection 'terminal/third/launch/r1' -EstimatedSeconds 60 -Launch -BatchKey 'terminal/failing') } catch { $thirdTerminal = $_.Exception.Message }
    if ($thirdTerminal -notlike '*third equivalent attempt*') { throw ('A late failure did not count toward the repeated-failure limit: ' + $thirdTerminal) }
    # A suspended entry whose process dies is charged only up to its suspension.
    Write-Record $terminalRoot @(@{ entryId='development:s:999999:00000006'; kind='native'; selection='suspended/dead/launch/r1'; startedUtc='2026-10-29T22:00:00Z'; heartbeatUtc='2026-10-29T22:03:00Z'; stepStartedUtc=$null; processId=999999; status='running'; estimatedSeconds=$null; endedUtc=$null; seconds=$null; launch=$true; batchId='development:batch1'; batchKey='suspended/dead'; parentEntryId=$null; suspendedUtc='2026-10-29T22:01:00Z'; excludedSeconds=30 })
    Set-Clock '2026-10-29T23:00:00Z'
    $suspendedOutput = (Invoke-Selector @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' }).Output
    $budget = Read-Budget $terminalRoot 'development'
    if ([string]$budget.entries[0].status -cne 'interrupted' -or [double]$budget.entries[0].seconds -ne 30 -or $suspendedOutput -notmatch 'spent 0.5 of 90 min') { throw ('A dead suspended entry was charged past its suspension: ' + $suspendedOutput) }
    Remove-Item -LiteralPath $trace
    $passed++
    Write-Host "Tier selection: PASS=$passed FAIL=0; offline orchestration only (stub children, boundary budget, file clock)."
}
finally {
    foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name]) }
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $boundary.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe cleanup path.' }
    if ($null -ne $junction -and (Test-Path -LiteralPath $junction)) { & cmd.exe /c rmdir "$junction" | Out-Null }
    Remove-Item -LiteralPath $boundary -Recurse -Force
}
