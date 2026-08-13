[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameResultPath,
    [Parameter(Mandatory = $true)][string]$RequestPath,
    [Parameter(Mandatory = $true)][string]$FingerprintPath,
    [Parameter(Mandatory = $true)][int]$ExpectedProcessId,
    [Parameter(Mandatory = $true)][DateTimeOffset]$NotBeforeUtc
)

$ErrorActionPreference='Stop'; Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$game=Read-KmcJson $GameResultPath; $request=Read-KmcJson $RequestPath; $fingerprint=Read-KmcJson $FingerprintPath
$required=@('schemaVersion','runId','scenario','status','branch','commit','productVersion','dllSha256','dllMvid','transactionToken','startedAtUtc','completedAtUtc','loadedModId','gameVersion','gameAssemblySha256','gameAssemblyMvid','ummVersion','ummSha256','harmony12Version','harmony12Sha256','relationshipState','movementExperimentEnabled','processId','currentGameMode','loadedAreaPresent','saveRequestCount','loadRequestCount','frameCount','elapsedSeconds','errors')
Assert-KmcExactProperties $game $required 'runtime game result'
if ([int]$game.schemaVersion -ne 1) { throw 'Runtime game-result schemaVersion must be 1.' }
foreach($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')) { if([string]$game.$name -cne [string]$request.$name){throw "Game result identity mismatch: $name"} }
$started=[DateTimeOffset]::MinValue; $completed=[DateTimeOffset]::MinValue
if(-not [DateTimeOffset]::TryParse([string]$game.startedAtUtc,[ref]$started)-or -not [DateTimeOffset]::TryParse([string]$game.completedAtUtc,[ref]$completed)-or $started -lt $NotBeforeUtc.AddSeconds(-5)-or $completed -lt $started-or $completed -gt [DateTimeOffset]::UtcNow.AddMinutes(1)){throw 'Runtime game-result timestamps are invalid or outside the run.'}
$gameAuthority=@($fingerprint.kingmaker.files|Where-Object role -eq 'gameplayAssembly')[0]
$umm=@($fingerprint.kingmaker.files|Where-Object role -eq 'umm')[0]; $harmony=@($fingerprint.kingmaker.files|Where-Object role -eq 'harmony')[0]
if([string]$game.status -cne 'PASS'-or [string]$game.loadedModId -cne 'KingmakerMountedCombat'-or
   [string]$game.gameVersion -cne [string]$fingerprint.kingmaker.displayVersion-or
   [string]$game.gameAssemblySha256 -cne [string]$gameAuthority.sha256-or [string]$game.gameAssemblyMvid -cne [string]$gameAuthority.mvid-or
   [string]$game.ummVersion -cne '0.28.2.0'-or [string]$game.ummSha256 -cne [string]$umm.sha256-or
   [string]$game.harmony12Version -cne '1.2.0.1'-or [string]$game.harmony12Sha256 -cne [string]$harmony.sha256){throw 'Runtime game-result platform identity is not exact.'}
if([int]$game.processId -ne $ExpectedProcessId-or [string]$game.relationshipState -cne 'Unmounted'-or $game.movementExperimentEnabled -ne $false-or
   $game.loadedAreaPresent -ne $false-or [int]$game.saveRequestCount -ne 0-or [int]$game.loadRequestCount -ne 0-or
   [int]$game.frameCount -lt 10-or [double]$game.elapsedSeconds -lt 1.0-or [string]::IsNullOrWhiteSpace([string]$game.currentGameMode)-or
   $null -eq $game.errors-or $game.errors -is [string]-or @($game.errors).Count -ne 0){throw 'Runtime game-result safety state is not an unmounted no-save smoke PASS.'}
Write-Host 'TOTAL PASS=24 FAIL=0'
