[CmdletBinding()]
param(
    [ValidateSet('Fast','Candidate','Full')][string]$Tier = 'Fast',
    # Additional focused tests for the work in hand (directly affected evidence validators,
    # contract tests and the focused regression for the current defect). A script name under
    # scripts\, or a repository-relative path inside the scripts tree (chunk7\Test-X.ps1,
    # scripts\runtime\Test-Y.ps1). Paths are canonicalized; traversal, links and anything
    # outside the tree are refused; equivalent selections run once.
    [string[]]$Focused = @(),
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$SkipBuild,
    # Current KMC assembly checks default to Kingmaker. Wrath or Both must be selected explicitly.
    [ValidateSet('Both','Kingmaker','Wrath')][string]$AssemblyTarget='Kingmaker',
    # Retained for a clear refusal; historical replay takes its own parameters.
    [string]$LabRoot = '',
    # Resolve and print the selection, prerequisites, artifact identity, impact map and the
    # remaining shared allowance, then stop: no build, no test, no lock, no game, no budget entry.
    [switch]$Plan,
    # Shared cumulative validation budget (scripts/WorkflowBudget.ps1). Defaults to
    # KMC_WORKFLOW_BUDGET_ID or 'development'; the record lives under the lab's runtime-state.
    [string]$BudgetId = '',
    # Full is explicit milestone/release qualification (docs/VALIDATION-POLICY.md 3, 7) and
    # must be selected by naming its task; ordinary development uses Fast or Candidate.
    [ValidatePattern('^$|^[A-Za-z0-9][A-Za-z0-9._ -]{0,119}$')][string]$QualificationTask = ''
)
# Three explicit verification tiers for development (owner workflow amendment, 2026-10-02;
# budget and selection enforcement of the playtest completion plan, Sprint 1). Every product,
# safety, restoration and final-acceptance requirement is kept; the tiers only decide how much
# of the existing suite runs at each point in the loop.
#
#   FAST       during implementation: build, the component/domain and directly affected
#              source/contract tests, the directly affected evidence validators (-Focused) and the
#              focused regression for the current defect (-Focused).
#   CANDIDATE  once, before freezing a native candidate: FAST plus the runtime safety/harness core
#              (which includes the package validator), the assembly contracts, the cross-feature
#              affected regression readers and protocol checks supplied through -Focused.
#   FULL       at a milestone, at final qualification, or when a shared foundation change affects
#              the whole repository: the complete umbrella (scripts/Test.ps1), selected explicitly.
#
# Budget: admission once before any work, then again before EVERY further step (cumulative
# allowance, 30-minute block ceiling, known oversized steps refused rather than started). A
# running step always settles; a refusal only stops the steps that have not started.
#
# docs/VALIDATION-POLICY.md also governs package/suite/purity/native work:
# those are explicit frozen-candidate/release operations, not checkpoint gates.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scriptsRoot = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
. (Join-Path $PSScriptRoot 'WorkflowBudget.ps1')
$started = [DateTime]::UtcNow
$results = @()

# ---- Selection validation: everything here is decided before any work -------------------------
if (-not [string]::IsNullOrWhiteSpace($LabRoot)) {
    throw '-LabRoot is not consumed by this selector. Invoke the intended historical replay explicitly with its supported parameters.'
}
if ($Tier -eq 'Fast' -and $PSBoundParameters.ContainsKey('AssemblyTarget')) {
    throw '-AssemblyTarget applies to Candidate or Full, which run assembly contracts.'
}
if ($Tier -eq 'Full' -and [string]::IsNullOrWhiteSpace($QualificationTask)) {
    throw 'Full is explicit milestone/release qualification and is not selected by default: name the selected task with -QualificationTask (docs/VALIDATION-POLICY.md 3, 7). Ordinary development uses Fast or Candidate with -Focused.'
}
if ($Tier -ne 'Full' -and -not [string]::IsNullOrWhiteSpace($QualificationTask)) {
    throw '-QualificationTask selects the Full umbrella only.'
}
$requestedFocused = @($Focused | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
if ($PSBoundParameters.ContainsKey('Focused') -and $requestedFocused.Count -eq 0) {
    throw '-Focused requires at least one real test script; omit it to run the tier defaults.'
}
if ($Tier -eq 'Full' -and $requestedFocused.Count -gt 0) {
    throw 'Full runs the complete umbrella and does not accept -Focused; use Fast or Candidate for selected checks.'
}
function Resolve-FocusedScript([string]$Selected) {
    if ($Selected -match '[\x00-\x1f]' -or $Selected.IndexOfAny([IO.Path]::GetInvalidPathChars()) -ge 0) { throw "Focused test script is invalid: $Selected" }
    $relative = $Selected -replace '/', '\'
    if (@($relative -split '\\') -contains '..') { throw "Focused test script uses traversal: $Selected" }
    if (-not [IO.Path]::IsPathRooted($relative)) { $relative = $relative -replace '^(\.\\)?scripts\\', '' }
    $candidate = if ([IO.Path]::IsPathRooted($relative)) { $relative } else { Join-Path $scriptsRoot $relative }
    $full = [IO.Path]::GetFullPath($candidate)
    if (-not $full.StartsWith($scriptsRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Focused test script is outside the scripts tree: $Selected" }
    if ([IO.Path]::GetFileName($full) -notmatch '^Test-[A-Za-z0-9-]+\.ps1$') { throw "Focused test script name is invalid: $Selected" }
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "Focused test script is missing: $Selected" }
    # No link below scripts\ may resolve the selection elsewhere.
    $probe = $full
    while ($probe.Length -gt $scriptsRoot.Length) {
        if (([IO.File]::GetAttributes($probe) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Focused test script path contains a link: $Selected" }
        $probe = [IO.Path]::GetDirectoryName($probe)
    }
    return [pscustomobject]@{ Path = $full; Name = $full.Substring($scriptsRoot.Length + 1) }
}
$focusedScripts = @()
foreach ($selected in $requestedFocused) {
    $resolved = Resolve-FocusedScript $selected
    if (@($focusedScripts | Where-Object { $_.Path -ieq $resolved.Path }).Count -eq 0) { $focusedScripts += $resolved }
}
$tierBuiltIns = @('Validate-Source.ps1')
$candidateBuiltIns = @('Test-Harness.ps1','Test-PairedActivationContracts.ps1','Test-RuntimeArtifactManifestContract.ps1','Test-AssemblyContracts.ps1')
$focusedScripts = @($focusedScripts | Where-Object {
    $_.Name -notin $tierBuiltIns -and -not ($Tier -eq 'Candidate' -and $_.Name -in $candidateBuiltIns) })

# ---- Resolved plan ----------------------------------------------------------------------------
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = ''
if (Test-Path -LiteralPath $vswhere -PathType Leaf) {
    $msbuild = [string](@(& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe') | Select-Object -First 1)
}
$components = Join-Path $repoRoot ('bin/Tests/' + $Configuration + '/KingmakerMountedCombat.Tests.exe')
$productDll = Join-Path $repoRoot ('bin/' + $Configuration + '/KingmakerMountedCombat.dll')
$steps = New-Object 'System.Collections.Generic.List[object]'
function Add-Step([string]$Name, [string]$Detail, [string]$Prerequisite) { $steps.Add([pscustomobject]@{ Name = $Name; Detail = $Detail; Prerequisite = $Prerequisite }) }
if ($Tier -eq 'Full') {
    Add-Step 'FULL umbrella (scripts/Test.ps1)' ('Test.ps1 -AssemblyTarget ' + $AssemblyTarget + $(if ($SkipBuild) { ' -SkipBuild' } else { '' }) + '; task: ' + $QualificationTask) $(if (Test-Path -LiteralPath (Join-Path $scriptsRoot 'Test.ps1')) { 'present' } else { 'MISSING Test.ps1' })
} else {
    if (-not $SkipBuild) { Add-Step 'build' ('MSBuild KingmakerMountedCombat.sln /p:Configuration=' + $Configuration) $(if ($msbuild) { $msbuild } else { 'MISSING MSBuild' }) }
    Add-Step 'FAST components' $components $(if ((Test-Path -LiteralPath $components) -or -not $SkipBuild) { 'present or built by this run' } else { 'MISSING compiled component runner' })
    foreach ($testScript in $tierBuiltIns) { Add-Step ('FAST ' + $testScript) (Join-Path $scriptsRoot $testScript) $(if (Test-Path -LiteralPath (Join-Path $scriptsRoot $testScript)) { 'present' } else { 'MISSING' }) }
    foreach ($testScript in $focusedScripts) { Add-Step ('FOCUSED ' + $testScript.Name) $testScript.Path 'present' }
    if ($Tier -eq 'Candidate') {
        foreach ($testScript in @('Test-Harness.ps1','Test-PairedActivationContracts.ps1','Test-RuntimeArtifactManifestContract.ps1')) {
            Add-Step ('CANDIDATE ' + $testScript) (Join-Path $scriptsRoot $testScript) $(if (Test-Path -LiteralPath (Join-Path $scriptsRoot $testScript)) { 'present' } else { 'MISSING' })
        }
        Add-Step ('CANDIDATE assembly ' + $AssemblyTarget) ('Test-AssemblyContracts.ps1 -Target ' + $AssemblyTarget) $(if (Test-Path -LiteralPath (Join-Path $scriptsRoot 'Test-AssemblyContracts.ps1')) { 'present' } else { 'MISSING' })
    }
}
$budgetIdResolved = Get-KmcWorkflowBudgetId $BudgetId
$budgetView = Read-KmcWorkflowBudget $repoRoot $budgetIdResolved
$knownSeconds = 0.0; $unknownSteps = 0
$stepEstimates = @{}
foreach ($step in $steps) {
    $last = Get-KmcWorkflowStepHistorySeconds $budgetView $step.Name
    if ($null -eq $last) { $unknownSteps++ } else { $knownSeconds += $last; $stepEstimates[$step.Name] = [int][Math]::Ceiling($last) }
}
$estimate = if ($unknownSteps -eq 0 -and $steps.Count -gt 0) { [int][Math]::Ceiling($knownSeconds) } else { $null }
# What admission is told: the complete estimate when every step is known, otherwise the known
# part as a lower bound (a selection whose known steps alone exceed the allowance never starts).
$admissionEstimate = if ($knownSeconds -gt 0) { [int][Math]::Ceiling($knownSeconds) } else { $null }

if ($Plan) {
    $now = Get-KmcWorkflowClock
    [void](Update-KmcWorkflowBudgetReconciliation $budgetView $now)
    $summary = Get-KmcWorkflowBudgetSummary $budgetView $now
    Write-Host ('PLAN tier=' + $Tier + ' configuration=' + $Configuration + ' build=' + $(if ($Tier -eq 'Full') { 'inside umbrella' } elseif ($SkipBuild) { 'no' } else { 'yes' }) +
        ' assemblyTarget=' + $(if ($Tier -eq 'Fast') { 'n/a' } else { $AssemblyTarget }) + ' selected=' + $steps.Count)
    Write-Host ('  ' + (Format-KmcWorkflowBudgetSummary $summary))
    $index = 0
    foreach ($step in $steps) {
        $index++
        $last = Get-KmcWorkflowStepHistorySeconds $budgetView $step.Name
        Write-Host ('  ' + $index + '. ' + $step.Name + ' [' + $step.Prerequisite + '] last=' + $(if ($null -eq $last) { 'unknown' } else { [string]$last + 's' }) + ' :: ' + $step.Detail)
    }
    Write-Host ('  estimate=' + $(if ($null -eq $estimate) { 'unknown (' + $unknownSteps + ' step(s) without a recorded duration)' } else { [string]$estimate + 's from recorded durations' }))
    if (Test-Path -LiteralPath $productDll -PathType Leaf) {
        $mvid = 'unknown'
        try { $mvid = [Reflection.Assembly]::ReflectionOnlyLoadFrom($productDll).ManifestModule.ModuleVersionId.ToString() } catch { }
        Write-Host ('  artifact: ' + $productDll + ' sha256=' + (Get-FileHash -LiteralPath $productDll -Algorithm SHA256).Hash.ToLowerInvariant() + ' mvid=' + $mvid + $(if ($SkipBuild) { ' (reused)' } else { ' (replaced by the build step)' }))
    } else { Write-Host ('  artifact: ' + $productDll + ' not built') }
    # Impact map: which reviewed families the working tree touches and what they depend on.
    $mapPath = Join-Path $scriptsRoot 'workflow-impact-map.json'
    if (Test-Path -LiteralPath $mapPath -PathType Leaf) {
        $changed = @()
        try { $changed = @(& git -C $repoRoot status --porcelain --untracked-files=all 2>$null | ForEach-Object { $_.Substring(3).Trim() -replace '^.* -> ', '' }) } catch { $changed = $null }
        if ($null -eq $changed) { Write-Host '  impact: unknown (git status unavailable)' }
        else {
            $map = Get-Content -Raw -LiteralPath $mapPath | ConvertFrom-Json
            $touched = @($map.families | Where-Object { $family = $_; @($changed | Where-Object { $file = $_; @($family.paths | Where-Object { $file -like $_ }).Count -gt 0 }).Count -gt 0 })
            if ($touched.Count -eq 0) { Write-Host ('  impact: no mapped family touched by ' + $changed.Count + ' changed path(s); unmapped changes need their own focused selection') }
            foreach ($family in $touched) {
                Write-Host ('  impact ' + $family.id + ' (' + $family.title + '):')
                Write-Host ('    focused tests: ' + ($family.focusedTests -join ', '))
                Write-Host ('    native routes: ' + ($family.nativeRoutes -join ' | '))
                Write-Host ('    neighbors: ' + ($family.neighbors -join ' | '))
                Write-Host ('    safety checks: ' + ($family.safetyChecks -join ' | '))
            }
        }
    } else { Write-Host '  impact: map missing (scripts/workflow-impact-map.json)' }
    Write-Host '  plan only: nothing was built, run, locked, launched or recorded in the budget.'
    return
}

# ---- Admission, then execution ----------------------------------------------------------------
$selectionText = $Tier + ': ' + (($steps | ForEach-Object { $_.Name }) -join ', ')
$admission = Request-KmcWorkflowAdmission -RepoRoot $repoRoot -BudgetId $BudgetId -Kind tier -Selection $selectionText -EstimatedSeconds $admissionEstimate
$invokedTests = @{}
$refusal = $null
function Invoke-Tiered([string]$Name, [scriptblock]$Body) {
    # Admission again before this step: cumulative allowance, block ceiling and the step's own
    # known cost. A refusal here stops only the steps that have not started.
    $known = if ($stepEstimates.ContainsKey($Name)) { [int]$stepEstimates[$Name] } else { $null }
    try { Assert-KmcWorkflowStepAdmission -RepoRoot $repoRoot -Admission $admission -StepName $Name -EstimatedSeconds $known }
    catch {
        $script:refusal = $_.Exception.Message
        $script:results += [pscustomobject]@{ step = $Name; pass = $false; seconds = 0; detail = 'REFUSED before start: ' + $script:refusal }
        Write-Host ('REFUSED ' + $Name + ': ' + $script:refusal)
        throw ('Workflow budget refused step: ' + $Name)
    }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $ok = $true; $detail = ''
    try { & $Body } catch { $ok = $false; $detail = $_.Exception.Message }
    $sw.Stop()
    $script:results += [pscustomobject]@{ step = $Name; pass = $ok; seconds = [Math]::Round($sw.Elapsed.TotalSeconds, 1); detail = $detail }
    Write-Host (('{0,-4} {1} ({2}s){3}' -f ($(if ($ok) { 'PASS' } else { 'FAIL' })), $Name, [Math]::Round($sw.Elapsed.TotalSeconds, 1), $(if ($ok) { '' } else { ': ' + $detail })))
    try { Update-KmcWorkflowEntry -RepoRoot $repoRoot -Admission $admission -StepName $Name -StepSeconds $sw.Elapsed.TotalSeconds } catch { Write-Warning ('Workflow budget heartbeat failed: ' + $_.Exception.Message) }
    if (-not $ok) { throw ('Tier step failed: ' + $Name) }
}
# Children run as separate processes whose output is relayed when they end; while they run
# the entry's heartbeat is refreshed every 15 seconds, so an interrupted long step is charged
# to within that bound instead of to its start.
function Invoke-KmcTierChild([string]$FilePath, [string[]]$Arguments) {
    $stdout = [IO.Path]::GetTempFileName(); $stderr = [IO.Path]::GetTempFileName()
    try {
        $quoted = @($Arguments | ForEach-Object { if ($_ -match '[\s"]') { '"' + ($_ -replace '"', '\"') + '"' } else { $_ } })
        $start = @{ FilePath = $FilePath; NoNewWindow = $true; PassThru = $true; RedirectStandardOutput = $stdout; RedirectStandardError = $stderr }
        if ($quoted.Count -gt 0) { $start.ArgumentList = $quoted }
        $process = Start-Process @start
        # Windows PowerShell only reports ExitCode for a process whose handle was taken while it ran.
        $null = $process.Handle
        while (-not $process.WaitForExit(15000)) {
            try { Update-KmcWorkflowEntry -RepoRoot $repoRoot -Admission $admission } catch { }
        }
        $process.WaitForExit()
        foreach ($file in @($stdout, $stderr)) { if (Test-Path -LiteralPath $file) { Get-Content -LiteralPath $file | ForEach-Object { Write-Host ('    ' + $_) } } }
        return $process.ExitCode
    }
    finally { foreach ($file in @($stdout, $stderr)) { Remove-Item -LiteralPath $file -Force -ErrorAction SilentlyContinue } }
}
function Invoke-TestScript([string]$Name, [string[]]$Extra = @(), [string]$Path = '') {
    if ($invokedTests.ContainsKey($Name)) { return }
    if ([string]::IsNullOrWhiteSpace($Path)) { $Path = Join-Path $scriptsRoot $Name }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw ('Test script is missing: ' + $Name) }
    $takesConfiguration = (Get-Command $Path).Parameters.ContainsKey('Configuration')
    $arguments = @('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',$Path)
    if ($takesConfiguration) { $arguments += @('-Configuration',$Configuration) }
    if ($Extra.Count -gt 0) { $arguments += $Extra }
    $exit = Invoke-KmcTierChild 'powershell.exe' $arguments
    if ($exit -ne 0) { throw ($Name + ' exited ' + $exit) }
    $invokedTests[$Name] = $true
}
$outcome = 'failed'
try {
    if ($Tier -eq 'Full') {
        $fullArguments = @('-AssemblyTarget', $AssemblyTarget)
        if ($SkipBuild) { $fullArguments += '-SkipBuild' }
        Invoke-Tiered 'FULL umbrella (scripts/Test.ps1)' { Invoke-TestScript 'Test.ps1' $fullArguments }
    } else {
        if (-not $SkipBuild) {
            Invoke-Tiered 'build' {
                if ([string]::IsNullOrWhiteSpace($msbuild)) { throw 'MSBuild not found.' }
                $exit = Invoke-KmcTierChild $msbuild @((Join-Path $repoRoot 'KingmakerMountedCombat.sln'), '/t:Build', "/p:Configuration=$Configuration", '/m:1', '/v:minimal', '/nologo')
                if ($exit -ne 0) { throw 'Build failed.' }
            }
        }
        Invoke-Tiered 'FAST components' {
            if(-not(Test-Path -LiteralPath $components)){throw 'Compiled component runner missing.'}
            $componentExit = Invoke-KmcTierChild $components @()
            if($componentExit-ne0){throw ('Component runner exited '+$componentExit)}
        }
        foreach ($testScript in $tierBuiltIns) { Invoke-Tiered ('FAST ' + $testScript) { Invoke-TestScript $testScript } }
        foreach ($testScript in $focusedScripts) { Invoke-Tiered ('FOCUSED ' + $testScript.Name) { Invoke-TestScript $testScript.Name @() $testScript.Path } }
        if ($Tier -eq 'Candidate') {
            foreach ($testScript in @('Test-Harness.ps1','Test-PairedActivationContracts.ps1','Test-RuntimeArtifactManifestContract.ps1')) { Invoke-Tiered ('CANDIDATE ' + $testScript) { Invoke-TestScript $testScript } }
            Invoke-Tiered ('CANDIDATE assembly '+$AssemblyTarget) { Invoke-TestScript 'Test-AssemblyContracts.ps1' @('-Target',$AssemblyTarget) }
            # Affected registration/readers/ledger checks are explicit -Focused inputs.
            # Historical archives and immutable replays remain under FULL; LabRoot
            # alone never adds unrelated native-evidence interpretation.
        }
    }
    $outcome = 'completed'
}
finally {
    try { Complete-KmcWorkflowEntry -RepoRoot $repoRoot -Admission $admission -Status $outcome } catch { Write-Warning ('Workflow budget completion failed: ' + $_.Exception.Message) }
}
$elapsed = [Math]::Round(([DateTime]::UtcNow - $started).TotalMinutes, 2)
Write-Host ('TIER ' + $Tier.ToUpperInvariant() + ' PASS=' + @($results | Where-Object pass).Count + ' FAIL=' + @($results | Where-Object { -not $_.pass }).Count + ' (' + $elapsed + ' min; offline verification only, no native qualification)')
