[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ReadyPath,
    [Parameter(Mandatory = $true)][string]$RequestPath,
    [Parameter(Mandatory = $true)][string]$PackageManifestPath,
    [Parameter(Mandatory = $true)][int]$ExpectedProcessId,
    [Parameter(Mandatory = $true)][DateTimeOffset]$NotBeforeUtc
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

$request = Read-KmcJson $RequestPath
$readyRoot = [IO.Path]::GetFullPath([string]$request.evidenceRoot).TrimEnd('\')
$fullReady = Assert-KmcChildPath $ReadyPath $readyRoot 'manual review READY evidence'
if ([IO.Path]::GetFileName($fullReady) -cne 'manual-review-ready.json' -or -not (Test-Path -LiteralPath $fullReady -PathType Leaf)) {
    throw 'Manual review READY evidence path or leaf is not exact.'
}
Assert-KmcNotReparsePoint $fullReady 'manual review READY evidence'
Assert-KmcNotHardLink $fullReady 'manual review READY evidence'
$raw = [IO.File]::ReadAllText($fullReady, (New-Object Text.UTF8Encoding($false, $true)))
Assert-KmcJsonObjectMembersUnique $raw 'manual review READY evidence'
$ready = $raw | ConvertFrom-Json
$properties = @(
    'schemaVersion','evidenceKind','runId','scenario','status','branch','commit','productVersion','dllSha256','dllMvid',
    'transactionToken','readyAtUtc','loadedModId','gameVersion','processId','currentGameMode','loadedAreaGuid',
    'fixtureIdentityVerified','workingInternalName','workingFileName','saveWriteMode','loadRequestCount','saveRequestCount',
    'authorizedLoadCount','authorizedWriteCount','unauthorizedLoadCount','unauthorizedWriteCount','relationshipState',
    'movementExperimentEnabled','riderId','mountId','mountBlueprintGuid','selectedUnitIds','actionLabel','actionVisible',
    'actionEnabled','poseProfileId','poseHealthy','poseFrameApplied','poseBoneCount','poseComponentCount','visualAcceptance'
)
Assert-KmcExactProperties $ready $properties 'manual review READY evidence'

if ([int]$ready.schemaVersion -ne 1 -or [string]$ready.evidenceKind -cne 'manual-visual-review-ready' -or
    [string]$ready.status -cne 'READY' -or [string]$ready.scenario -cne 'manual-visual-review' -or
    [string]$request.scenario -cne 'manual-visual-review') {
    throw 'Manual review READY schema, kind, status, or scenario is invalid.'
}
foreach ($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')) {
    if ([string]$ready.$name -cne [string]$request.$name) { throw "Manual review READY $name differs from its request." }
}
$manifest = Read-KmcJson $PackageManifestPath
foreach ($name in @('branch','commit')) {
    if ([string]$ready.$name -cne [string]$manifest.$name) { throw "Manual review READY $name differs from the package manifest." }
}
if ([string]$ready.productVersion -cne [string]$manifest.version -or
    [string]$ready.dllSha256 -cne [string]$manifest.dllSha256 -or
    [string]$ready.dllMvid -cne [string]$manifest.dllMvid) {
    throw 'Manual review READY build identity differs from the package manifest.'
}
$readyAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$ready.readyAtUtc, [ref]$readyAt) -or $readyAt -lt $NotBeforeUtc) {
    throw 'Manual review READY timestamp predates the guarded launch.'
}
if ([int]$ready.processId -ne $ExpectedProcessId -or [string]$ready.loadedModId -cne 'KingmakerMountedCombat' -or
    [string]$ready.gameVersion -cne '2.1.7b' -or [string]$ready.currentGameMode -cne 'Default' -or
    [string]$ready.loadedAreaGuid -cne [string]$request.fixture.working.area -or $ready.fixtureIdentityVerified -ne $true) {
    throw 'Manual review READY process, mod, game, mode, area, or fixture identity is invalid.'
}
if ([string]$ready.workingInternalName -cne 'KMC_AUTOMATION_WORKING' -or
    [string]$ready.workingFileName -cne [string]$request.fixture.working.fileName -or
    [string]$ready.saveWriteMode -cne 'read-only' -or [string]$request.fixture.writeAuthorization.mode -cne 'read-only' -or
    $null -ne $request.fixture.writeAuthorization.allowedInternalName -or
    $null -ne $request.fixture.writeAuthorization.allowedFileName) {
    throw 'Manual review READY is not bound to exact read-only Working access.'
}
if ([int]$ready.loadRequestCount -ne 1 -or [int]$ready.saveRequestCount -ne 0 -or
    [int]$ready.authorizedLoadCount -ne 1 -or [int]$ready.authorizedWriteCount -ne 0 -or
    [int]$ready.unauthorizedLoadCount -ne 0 -or [int]$ready.unauthorizedWriteCount -ne 0) {
    throw 'Manual review READY save/load counters are not exact load-once/write-never state.'
}
if ([string]$ready.relationshipState -cne 'Mounted' -or $ready.movementExperimentEnabled -ne $true -or
    [string]::IsNullOrWhiteSpace([string]$ready.riderId) -or [string]::IsNullOrWhiteSpace([string]$ready.mountId) -or
    [string]$ready.riderId -ceq [string]$ready.mountId -or
    [string]$ready.mountBlueprintGuid -cne 'e7aa96d15a45238438ae4cfb476f6bb9') {
    throw 'Manual review READY mounted pair identity is invalid.'
}
if ($ready.selectedUnitIds -isnot [Array] -or @($ready.selectedUnitIds).Count -ne 1 -or
    [string]$ready.selectedUnitIds[0] -cne [string]$ready.riderId -or
    [string]$ready.actionLabel -cne 'Dismount' -or $ready.actionVisible -ne $true -or $ready.actionEnabled -ne $true) {
    throw 'Manual review READY selection or player-action state is invalid.'
}
if ([string]$ready.poseProfileId -cne 'medium-humanoid-mammoth-v1' -or $ready.poseHealthy -ne $true -or
    $ready.poseFrameApplied -ne $true -or [int]$ready.poseBoneCount -ne 7 -or [int]$ready.poseComponentCount -ne 1 -or
    [string]$ready.visualAcceptance -cne 'PENDING') {
    throw 'Manual review READY pose state or acceptance boundary is invalid.'
}

Write-Host 'TOTAL PASS=40 FAIL=0'
