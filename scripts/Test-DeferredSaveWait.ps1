<#
Focused regression for the 2026-11-22 post-combat save and door incident.

Splices the production deferred-save wait (MountedDeferredSave.cs), the native
loading-queue adapter (NativeDeferredSave.cs) and the command hold
(MountedPatchController.cs) verbatim into tests/DeferredSaveWait/DeferredSaveWaitTemplate.cs,
compiles them with the production domain sources and runs the cases. Game
objects and the readiness evaluation are fakes: this is component/integration
evidence of the wait's lifecycle, never native gameplay evidence.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')

function Get-ActualMember([string]$Text, [string]$Signature) {
    $a = $Text.IndexOf($Signature)
    if ($a -lt 0) { throw ('Missing exact production member ' + $Signature) }
    if ($Text.IndexOf($Signature, $a + 1) -ge 0) { throw ('Ambiguous production member ' + $Signature) }
    $o = $Text.IndexOf('{', $a); $semi = $Text.IndexOf(';', $a); $arrow = $Text.IndexOf('=>', $a)
    if ($arrow -ge 0 -and $arrow -lt $o -and $semi -lt $o) { return $Text.Substring($a, $semi + 1 - $a) }
    $d = 1; $e = $o + 1
    while ($d -gt 0 -and $e -lt $Text.Length) { if ($Text[$e] -eq '{') { $d++ }; if ($Text[$e] -eq '}') { $d-- }; $e++ }
    if ($d -ne 0) { throw ('Unbalanced production member ' + $Signature) }
    $Text.Substring($a, $e - $a)
}

$deferSource = Join-Path $repo 'src/KingmakerMountedCombat/Integration/MountedDeferredSave.cs'
$nativeSource = Join-Path $repo 'src/KingmakerMountedCombat/Integration/NativeDeferredSave.cs'
$patchSource = Join-Path $repo 'src/KingmakerMountedCombat/Integration/MountedPatchController.cs'
$templateSource = Join-Path $repo 'tests/DeferredSaveWait/DeferredSaveWaitTemplate.cs'
$domain = @('DeferredSaveEnumerator.cs', 'SaveWaitObservation.cs') | ForEach-Object { Join-Path $repo ('src/KingmakerMountedCombat/Domain/' + $_) }

$probe = Join-Path (Get-KmcLabRoot) ('analysis-cache/deferred-save-wait/' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($probe)
# .NET hashing: the tier runner's child host does not autoload Get-FileHash.
function Get-Sha256([string]$Path) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash([IO.File]::ReadAllBytes($Path))) -replace '-', '').ToLowerInvariant() }
    finally { $algorithm.Dispose() }
}
$receipt = [ordered]@{ template = Get-Sha256 $templateSource }
foreach ($source in @($deferSource, $nativeSource, $patchSource) + $domain) { $receipt[[IO.Path]::GetFileName($source)] = Get-Sha256 $source }
$receipt['actualMembers'] = $true; $receipt['nativeEvidence'] = $false
$receipt | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $probe 'source-receipt.json')
$vs = Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$compiler = @(& $vs -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/Roslyn/csc.exe') | Select-Object -First 1
if (-not $compiler) { throw 'Roslyn csc.exe was not found through vswhere.' }

$production = @{
    defer = [IO.File]::ReadAllText($deferSource)
    native = [IO.File]::ReadAllText($nativeSource)
    patch = [IO.File]::ReadAllText($patchSource)
}

# Builds the adapter from (possibly mutated) production text, runs it, and returns case -> PASS/FAIL.
function Invoke-Harness([hashtable]$Text, [string]$Label, [switch]$Echo) {
    $nativeMethods = (Get-ActualMember $Text.native 'internal static bool Waiting(LoadingProcess owner)') + [Environment]::NewLine +
        (Get-ActualMember $Text.native 'internal static void StartScreen(') + [Environment]::NewLine +
        (Get-ActualMember $Text.native 'internal static bool MoveNext(IEnumerator iterator, LoadingProcess owner)')
    $patchMethods = (Get-ActualMember $Text.patch 'internal static bool SaveCommandStartPrefix(') + [Environment]::NewLine +
        (Get-ActualMember $Text.patch 'internal static void DeferredSaveLoadingPostfix(')
    $adapter = [IO.File]::ReadAllText($templateSource).
        Replace('__READINESS_TYPE__', (Get-ActualMember $Text.defer 'internal struct SaveReadiness')).
        Replace('__DEFER_METHOD__', (Get-ActualMember $Text.defer 'private IEnumerator<object> DeferNativeSave(')).
        Replace('__OBSERVE_METHOD__', (Get-ActualMember $Text.defer 'private void ObserveSaveWait(')).
        Replace('__END_METHOD__', (Get-ActualMember $Text.defer 'private void ReportSaveWaitEnd(')).
        Replace('__OUTCOME_PROPERTY__', (Get-ActualMember $Text.defer 'internal SaveWaitOutcome? LastSaveWaitOutcome')).
        Replace('__NATIVE_METHODS__', $nativeMethods).
        Replace('__PATCH_METHODS__', $patchMethods)
    if ($adapter -match '__[A-Z_]+__') { throw 'An unspliced production placeholder remains in the deferred-save harness.' }
    $directory = Join-Path $probe $Label
    [void][IO.Directory]::CreateDirectory($directory)
    $adapterPath = Join-Path $directory 'DeferredSaveWait.cs'
    [IO.File]::WriteAllText($adapterPath, $adapter)
    $exe = Join-Path $directory 'DeferredSaveWait.exe'
    $compile = & $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649,0169,0414 "/out:$exe" $adapterPath @domain 2>&1
    if ($LASTEXITCODE -ne 0) { $compile | Out-Host; throw "Deferred-save wait harness compilation failed ($Label)." }
    $output = @(& $exe)
    if ($Echo) { $output | Out-Host }
    $verdicts = [ordered]@{}
    foreach ($line in $output) { if ($line -match '^(PASS|FAIL) (.+?)( :: .*)?$') { $verdicts[$Matches[2]] = $Matches[1] } }
    return $verdicts
}

$baseline = Invoke-Harness $production 'unmutated' -Echo
if (@($baseline.Values | Where-Object { $_ -ne 'PASS' }).Count -ne 0 -or $baseline.Count -lt 16) {
    throw 'Deferred-save wait regression failed.'
}

# One production line per mutation, in the generated copy only. Each must fail every named case.
$mutations = @(
    @{ id = 'D1-generic-world-text'; file = 'defer'; old = 'throw new InvalidOperationException(SaveWaitWorldPolicy.FailureMessage(state, mode.ToString(), observation?.WaitingFor));'
       new = 'throw new InvalidOperationException("Save was not written: the native world changed while its effects were finishing.");'
       cases = @('a menu opened during the wait refuses before serialization and names the menu and the blocker', 'a loading screen during the wait is named') }
    @{ id = 'D2-fault-reported-as-cancellation'; file = 'defer'; old = 'else if (operation.Failure != null)'; new = 'else if (operation.Failure == operation.Failure && false)'
       cases = @('a begin-wait notification that throws is Faulted, not Cancelled', 'a loading screen that fails to start after the wait settles is Faulted, never ReadyToSerialize',
           'a readiness fault refuses before serialization and is reported as Faulted') }
    @{ id = 'D3-blocker-not-observed'; file = 'defer'; old = 'ObserveSaveWait(observation, readiness, now);'; new = ';'
       cases = @('a menu opened during the wait refuses before serialization and names the menu and the blocker', 'each blocker change is reported once and a settled wait serializes with its pause restored') }
    @{ id = 'D4-end-not-reported'; file = 'defer'; old = '                    ReportSaveWaitEnd(observation, operation, refused, refusedReason, clock());'; new = '                    ;'
       cases = @('queued requests during the menu each retire with their own named refusal and nothing holds afterwards', 'native cancellation while waiting reports Cancelled and releases the fence, pause and hold') }
    @{ id = 'D5-observer-not-contained'; file = 'defer'; old = 'catch { /* An unloggable observation leaves the save decision untouched. */ }'; new = 'catch { throw; }'
       cases = @('a failing logger changes no refusal, outcome, fence or pause') }
    @{ id = 'D6-hold-ignores-wait'; file = 'patch'; old = '!NativeDeferredSave.Waiting(LoadingProcess.Instance)) &&'; new = 'true) &&'
       cases = @('a menu opened during the wait refuses before serialization and names the menu and the blocker') }
    @{ id = 'D7-queue-time-counted-as-waiting'; file = 'defer'; old = 'new SaveWaitObservation(request, label, wrappedAt, now)'; new = 'new SaveWaitObservation(request, label, wrappedAt, wrappedAt)'
       cases = @('a request queued 20 s then waiting 30.01 s times out with active-wait times and its blocker named') }
    @{ id = 'D8-condition-not-observed'; file = 'defer'; old = '() => ConditionOfReadiness(readiness)'; new = '() => string.Empty'
       cases = @('the same blocking owner changing its refusing condition is reported and the end carries the latest snapshot') }
)
$killed = 0
foreach ($mutation in $mutations) {
    $text = @{ defer = $production.defer; native = $production.native; patch = $production.patch }
    $source = $text[$mutation.file]
    $at = $source.IndexOf($mutation.old)
    if ($at -lt 0 -or $source.IndexOf($mutation.old, $at + 1) -ge 0) { throw ('Mutation target is not unique: ' + $mutation.id) }
    $text[$mutation.file] = $source.Substring(0, $at) + $mutation.new + $source.Substring($at + $mutation.old.Length)
    $verdicts = Invoke-Harness $text $mutation.id
    $survivors = @($mutation.cases | Where-Object { $verdicts[$_] -ne 'FAIL' })
    if ($survivors.Count -ne 0) { throw ('Mutation ' + $mutation.id + ' survived: ' + ($survivors -join '; ')) }
    $killed++
    Write-Output ('KILLED ' + $mutation.id + ' (' + $mutation.cases.Count + ' named case(s) failed)')
}
Write-Output ('DEFERRED SAVE WAIT MUTATIONS KILLED=' + $killed + ' OF ' + $mutations.Count + '; generated copies only, worktree unchanged')

# Review of 0b4b8fa4: the production readiness evaluation against the v0.2.14 predicate it replaced.
$baselineCommit = '6151903116d418d5c96d4862a3fb9a68c843b377'
$baselineDefer = (& git -C $repo show ($baselineCommit + ':src/KingmakerMountedCombat/Integration/MountedDeferredSave.cs')) -join "`n"
if ($LASTEXITCODE -ne 0 -or -not $baselineDefer) { throw 'Baseline 61519031 MountedDeferredSave.cs is unavailable.' }
$baselineMember = (Get-ActualMember $baselineDefer 'internal bool SaveEffectsReady()').Replace('internal bool SaveEffectsReady()', 'internal bool BaselineSaveEffectsReady()')
$equivalenceTemplate = Join-Path $repo 'tests/DeferredSaveWait/SaveReadinessEquivalenceTemplate.cs'
$equivalence = [IO.File]::ReadAllText($equivalenceTemplate).
    Replace('__READINESS_TYPE__', (Get-ActualMember $production.defer 'internal struct SaveReadiness')).
    Replace('__READY_METHOD__', (Get-ActualMember $production.defer 'internal bool SaveEffectsReady()')).
    Replace('__EVALUATE_METHOD__', (Get-ActualMember $production.defer 'internal SaveReadiness EvaluateSaveEffects()')).
    Replace('__BASELINE_METHOD__', $baselineMember)
if ($equivalence -match '__[A-Z_]+__') { throw 'An unspliced placeholder remains in the readiness equivalence harness.' }
$equivalenceDirectory = Join-Path $probe 'equivalence'
[void][IO.Directory]::CreateDirectory($equivalenceDirectory)
$equivalencePath = Join-Path $equivalenceDirectory 'SaveReadinessEquivalence.cs'
[IO.File]::WriteAllText($equivalencePath, $equivalence)
$equivalenceExe = Join-Path $equivalenceDirectory 'SaveReadinessEquivalence.exe'
$compile = & $compiler /nologo /target:exe /langversion:7.3 /nowarn:0649,0169,0414 "/out:$equivalenceExe" $equivalencePath @domain 2>&1
if ($LASTEXITCODE -ne 0) { $compile | Out-Host; throw 'Readiness equivalence harness compilation failed.' }
& $equivalenceExe | Write-Output
if ($LASTEXITCODE -ne 0) { throw 'Readiness equivalence against the baseline failed.' }
