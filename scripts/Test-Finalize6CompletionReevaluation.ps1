[CmdletBinding()] param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
    [string]$EvidenceRoot='C:\Dev\KingmakerMountedCombatLab\runtime-evidence')
# Read-only re-evaluation of one finalize6-falls-completion-tb run under the corrected readers: the run's own game result through
# the real game-result reader with -RequirePass (subscenario registration included), then every manifest artifact reader the
# final result reader dispatches, with PASS asked of each. Nothing is written; the run's recorded verdict is never changed.
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$root=Join-Path $EvidenceRoot $RunId
$requestPath=Join-Path $root 'runtime-request.json'; $gamePath=Join-Path $root 'runtime-game-result.json'
$request=Read-KmcJson $requestPath; $game=Read-KmcJson $gamePath; $result=Read-KmcJson (Join-Path $root 'runtime-result.json')
if([string]$request.scenario -cne 'finalize6-falls-completion-tb'){ throw 'The run is not the F6 completion variant.' }
$hashesBefore=@{}; foreach($f in Get-ChildItem -LiteralPath $root -File){ $hashesBefore[$f.Name]=Get-KmcSha256 $f.FullName }
& (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $gamePath -RequestPath $requestPath -FingerprintPath (Join-Path $repo 'planning\ENVIRONMENT-FINGERPRINT.json') `
    -ExpectedProcessId ([int]$game.processId) -NotBeforeUtc ([DateTimeOffset]::Parse([string]$result.startedAtUtc)) -RequirePass | Out-Null
$manifest=Read-KmcJson (Join-Path $root 'runtime-artifacts.json')
Assert-KmcNativeMammothArtifact -Request $request -Manifest $manifest -Status 'PASS'
Assert-KmcPhase3dHorseScenarioEvidence -Request $request -Manifest $manifest -Status 'PASS' -SubscenarioResults $game.subscenarioResults
foreach($f in Get-ChildItem -LiteralPath $root -File){ if($hashesBefore[$f.Name]-cne(Get-KmcSha256 $f.FullName)){ throw ('Evidence changed during re-evaluation: '+$f.Name) } }
Write-Host ('PASS re-evaluation of '+$RunId+' under the corrected readers (recorded verdict '+[string]$result.status+' unchanged)')
