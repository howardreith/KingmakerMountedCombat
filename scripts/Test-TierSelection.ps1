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
foreach ($name in @('KMC_WORKFLOW_TEST_TRACE','KMC_WORKFLOW_BUDGET_ROOT','KMC_WORKFLOW_BUDGET_ID','KMC_WORKFLOW_BUDGET_ENTRY','KMC_WORKFLOW_TEST_CLOCK_FILE','KMC_WORKFLOW_TEST_DEAD_PIDS')) {
    $previous[$name] = [Environment]::GetEnvironmentVariable($name)
    [Environment]::SetEnvironmentVariable($name, $null)
}
New-Item -ItemType Directory -Path (Join-Path $boundary 'scripts') -Force | Out-Null
$junction = $null
function Set-Clock([string]$Value) { Set-Content -LiteralPath $env:KMC_WORKFLOW_TEST_CLOCK_FILE -Value $Value -Encoding ASCII }
function Read-Calls([string]$Trace) { if (Test-Path -LiteralPath $Trace) { @(Get-Content -LiteralPath $Trace | ForEach-Object { $_ | ConvertFrom-Json }) } else { @() } }
function Read-Budget([string]$Root, [string]$Id) { $path = Join-Path $Root ($Id + '.json'); if (Test-Path -LiteralPath $path) { Get-Content -Raw -LiteralPath $path | ConvertFrom-Json } else { $null } }
try {
    $scripts = Join-Path $boundary 'scripts'
    $trace = Join-Path $boundary 'calls.jsonl'
    $budgetRoot = Join-Path $boundary 'budgets'
    $clock = Join-Path $boundary 'clock.txt'
    $env:KMC_WORKFLOW_TEST_TRACE = $trace
    $env:KMC_WORKFLOW_BUDGET_ROOT = $budgetRoot
    $env:KMC_WORKFLOW_TEST_CLOCK_FILE = $clock
    Set-Clock '2026-10-29T12:00:00Z'
    foreach ($name in @('Test-Tiers.ps1','Test.ps1','WorkflowBudget.ps1','workflow-impact-map.json')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $scripts }
    # Discover the actual umbrella's children rather than a claimed plan.
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $scripts 'Test.ps1'), [ref]$tokens, [ref]$errors)
    if ($errors.Count -ne 0) { throw 'Umbrella parse failed.' }
    $children = @($ast.FindAll({ param($node) $node -is [Management.Automation.Language.StringConstantExpressionAst] -and $node.Value -like '*.ps1' }, $true) | ForEach-Object { $_.Value })
    $children += @('Validate-Source.ps1','Test-SelectedFixture.ps1','chunk7\Test-SelectedNested.ps1','Test-AdvanceClock.ps1')
    foreach ($name in @($children | Select-Object -Unique)) {
        $path = Join-Path $scripts $name
        New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
        @'
[CmdletBinding()]
param([string]$Configuration='Release', [string]$Target='Both', [Parameter(ValueFromRemainingArguments=$true)][object[]]$Rest)
@{ script=[IO.Path]::GetFileName($PSCommandPath); configuration=$Configuration; target=$Target; entry=$env:KMC_WORKFLOW_BUDGET_ENTRY } | ConvertTo-Json -Compress | Add-Content -LiteralPath $env:KMC_WORKFLOW_TEST_TRACE
if ([IO.Path]::GetFileName($PSCommandPath) -eq 'Test-AdvanceClock.ps1') { Set-Content -LiteralPath $env:KMC_WORKFLOW_TEST_CLOCK_FILE -Value '2026-10-29T13:35:00Z' -Encoding ASCII }
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
    $plan = @(& $selector -Plan -Tier Candidate -SkipBuild -Configuration Debug -Focused 'chunk7/Test-SelectedNested.ps1,Test-SelectedFixture.ps1' *>&1 | ForEach-Object { [string]$_ })
    $planText = $plan -join "`n"
    if ($planText -notmatch 'PLAN tier=Candidate' -or $planText -notmatch 'assemblyTarget=Kingmaker' -or $planText -notmatch 'FOCUSED chunk7\\Test-SelectedNested\.ps1' -or
        $planText -notmatch 'FOCUSED Test-SelectedFixture\.ps1' -or $planText -notmatch 'CANDIDATE assembly Kingmaker' -or $planText -notmatch 'last=unknown' -or
        $planText -notmatch 'estimate=unknown' -or $planText -notmatch 'spent 0 of 90 min' -or $planText -notmatch 'plan only') { throw ('Plan output is incomplete: ' + $planText) }
    if ((Test-Path -LiteralPath $trace) -or ($null -ne (Read-Budget $budgetRoot 'development') -and @((Read-Budget $budgetRoot 'development').entries).Count -ne 0)) { throw 'Plan-only invocation ran work or recorded a budget entry.' }
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
    $entryId = [string]$budget.entries[0].entryId
    if (@($calls | Where-Object { [string]$_.entry -cne $entryId }).Count -ne 0) { throw 'Children did not inherit the budget entry.' }
    $passed++
    Remove-Item -LiteralPath $trace

    # Recorded durations make the next plan's estimate known.
    $plan2 = (@(& $selector -Plan -Tier Candidate -SkipBuild -Configuration Debug *>&1 | ForEach-Object { [string]$_ })) -join "`n"
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

    # A nested invocation (a wrapper's child) is counted in its parent, never twice.
    $env:KMC_WORKFLOW_BUDGET_ENTRY = $entryId
    $env:KMC_WORKFLOW_BUDGET_ID = 'development'
    & $selector -Tier Fast -SkipBuild -Configuration Debug -Focused 'Test-SelectedFixture.ps1' | Out-Null
    [Environment]::SetEnvironmentVariable('KMC_WORKFLOW_BUDGET_ENTRY', $null); [Environment]::SetEnvironmentVariable('KMC_WORKFLOW_BUDGET_ID', $null)
    $budget = Read-Budget $budgetRoot 'development'
    if (@($budget.entries).Count -ne 2) { throw 'A nested invocation created its own budget entry.' }
    $calls = Read-Calls $trace
    if (@($calls | Where-Object { [string]$_.entry -cne $entryId }).Count -ne 0) { throw 'A nested invocation did not propagate the parent entry.' }
    $passed++
    Remove-Item -LiteralPath $trace

    # Exhaustion: a child advances the controlled clock past the allowance during a run; the run
    # completes (running work is never interrupted), and the NEXT selection is refused before work.
    & $selector -Tier Fast -SkipBuild -Configuration Debug -Focused 'Test-AdvanceClock.ps1' | Out-Null
    $budget = Read-Budget $budgetRoot 'development'
    $spent = ($budget.entries | ForEach-Object { [double]$_.seconds } | Measure-Object -Sum).Sum
    if (@($budget.entries).Count -ne 3 -or [string]$budget.entries[2].status -cne 'completed' -or $spent -lt 90 * 60) { throw ('Exhausting run was not completed and accounted: ' + ($budget | ConvertTo-Json -Depth 6 -Compress)) }
    Remove-Item -LiteralPath $trace
    Assert-Refused @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' } 'Workflow budget exhausted'
    $planAfter = (@(& $selector -Plan -Tier Fast -SkipBuild -Configuration Debug *>&1 | ForEach-Object { [string]$_ })) -join "`n"
    if ($planAfter -notmatch 'remaining 0 min') { throw ('Plan did not report the exhausted allowance: ' + $planAfter) }
    # A new shell, commit or wrapper does not replenish it; only an explicitly selected new budget admits more work.
    Assert-Refused @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1'; BudgetId='development' } 'Workflow budget exhausted'
    & $selector -Tier Fast -SkipBuild -Configuration Debug -Focused 'Test-SelectedFixture.ps1' -BudgetId 'sprint1-selected-check' | Out-Null
    if (@((Read-Budget $budgetRoot 'sprint1-selected-check').entries).Count -ne 1) { throw 'An explicitly selected budget did not admit the check.' }
    $passed++
    Remove-Item -LiteralPath $trace

    # Interrupted records: a running entry whose process is gone is reconciled to its last heartbeat
    # before admission, and an interrupted spend that reaches the allowance refuses new work.
    $interruptedRoot = Join-Path $boundary 'budgets-interrupted'
    New-Item -ItemType Directory -Path $interruptedRoot -Force | Out-Null
    $env:KMC_WORKFLOW_BUDGET_ROOT = $interruptedRoot
    Set-Clock '2026-10-29T14:00:00Z'
    @{ schemaVersion=1; budgetId='development'; allowanceMinutes=90; createdUtc='2026-10-29T12:00:00Z'; stepHistory=@{}; entries=@(
        @{ entryId='development:a:999999'; kind='native'; selection='persistence-p05-save/quick'; startedUtc='2026-10-29T12:00:00Z'; heartbeatUtc='2026-10-29T12:40:00Z'; processId=999999; status='running'; estimatedSeconds=$null; endedUtc=$null; seconds=$null }
    ) } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $interruptedRoot 'development.json') -Encoding UTF8
    $output = (@(& $selector -Tier Fast -SkipBuild -Configuration Debug -Focused 'Test-SelectedFixture.ps1' *>&1 | ForEach-Object { [string]$_ })) -join "`n"
    $budget = Read-Budget $interruptedRoot 'development'
    if ($output -notmatch 'reconciled 1 interrupted entry' -or [string]$budget.entries[0].status -cne 'interrupted' -or [double]$budget.entries[0].seconds -ne 2400 -or
        @($budget.entries).Count -ne 2 -or $output -notmatch 'spent 40 of 90 min') { throw ('Interrupted entry was not reconciled before admission: ' + $output) }
    Remove-Item -LiteralPath $trace
    @{ schemaVersion=1; budgetId='development'; allowanceMinutes=90; createdUtc='2026-10-29T12:00:00Z'; stepHistory=@{}; entries=@(
        @{ entryId='development:b:999999'; kind='native'; selection='persistence-p05-save/quick'; startedUtc='2026-10-29T12:00:00Z'; heartbeatUtc='2026-10-29T13:31:00Z'; processId=999999; status='running'; estimatedSeconds=$null; endedUtc=$null; seconds=$null }
    ) } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $interruptedRoot 'development.json') -Encoding UTF8
    Assert-Refused @{ Tier='Fast'; SkipBuild=$true; Configuration='Debug'; Focused='Test-SelectedFixture.ps1' } 'Workflow budget exhausted'
    $passed++

    # The admission function itself refuses before a caller takes any lock: the native launcher calls
    # it before Steam checks, evidence directories, locks, staging or launch.
    . (Join-Path $scripts 'WorkflowBudget.ps1')
    $lockSentinel = Join-Path $boundary 'active-transaction.lock'
    $refusedBeforeLock = $false
    try { [void](Request-KmcWorkflowAdmission -RepoRoot $boundary -Kind native -Selection 'persistence-p05-save/quick' -EstimatedSeconds 360); New-Item -ItemType File -Path $lockSentinel | Out-Null }
    catch { $refusedBeforeLock = $_.Exception.Message -like '*Workflow budget exhausted*' }
    if (-not $refusedBeforeLock -or (Test-Path -LiteralPath $lockSentinel)) { throw 'An exhausted budget did not refuse before the lock step.' }
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
