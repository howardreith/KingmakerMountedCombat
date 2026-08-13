[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$RequestPath,
    [string]$PackageManifestPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

$request = Read-KmcJson $RequestPath
$required = @('schemaVersion', 'runId', 'scenario', 'branch', 'commit', 'productVersion', 'dllSha256', 'dllMvid', 'transactionToken', 'evidenceRoot', 'saveAccessAllowed', 'saveName')
$actual = @($request.PSObject.Properties.Name | Sort-Object)
if (($actual -join "`n") -cne (($required | Sort-Object) -join "`n")) {
    throw "Runtime request property set is not exact: $($actual -join ', ')"
}

$scenarios = @(
    'mod-load-smoke', 'export-mounted-contracts', 'export-candidate-mount-rigs', 'observe-mount-diagnostic-availability',
    'mounted-pair-create-and-clear', 'mounted-pair-double-mount-rejected', 'mounted-pair-invalid-pair-rejected',
    'mounted-pair-cleanup-idempotent', 'mounted-pair-death-cleanup', 'mounted-pair-combat-start-cleanup',
    'mounted-pair-area-unload-cleanup', 'mounted-pair-mod-disable-cleanup', 'mounted-pair-open-ground',
    'mounted-pair-stop-start', 'mounted-pair-turns-and-corners', 'mounted-pair-doorway', 'mounted-pair-selection',
    'mounted-pair-party-formation', 'mounted-pair-pause-unpause', 'mounted-pair-destination-cancel',
    'mounted-pair-turn-based-entry-cleanup', 'mounted-pair-realtime-entry-cleanup', 'mounted-pair-save-safety',
    'mounted-pair-load-safety', 'mounted-pair-area-transition-safety'
)

if ([int]$request.schemaVersion -ne 1) { throw 'Runtime request schemaVersion must be 1.' }
if ([string]$request.runId -notmatch '^[A-Za-z0-9._-]{1,120}$') { throw 'Runtime request runId is invalid.' }
if ([string]$request.scenario -cne 'mod-load-smoke') { throw 'Only mod-load-smoke is implemented by the no-save runtime host.' }
if ([string]$request.branch -notmatch '^codex/mounted-combat-[A-Za-z0-9._/-]+$') { throw 'Runtime request branch is outside the KMC prefix.' }
if ([string]$request.commit -notmatch '^[0-9a-f]{40}$') { throw 'Runtime request commit must be a full lowercase Git SHA.' }
if ([string]$request.productVersion -cne '0.0.1-feasibility') { throw 'Runtime request product version is not exact.' }
if ([string]$request.dllSha256 -notmatch '^[0-9a-f]{64}$') { throw 'Runtime request DLL SHA-256 is invalid.' }
if ([string]$request.transactionToken -notmatch '^[0-9a-f]{64}$') { throw 'Runtime request transaction token is invalid.' }
$parsedMvid = [Guid]::Empty
if (-not [Guid]::TryParse([string]$request.dllMvid, [ref]$parsedMvid)) { throw 'Runtime request DLL MVID is invalid.' }

$evidenceRoot = [IO.Path]::GetFullPath([string]$request.evidenceRoot)
$permittedEvidence = [IO.Path]::GetFullPath((Join-Path (Get-KmcLabRoot) 'runtime-evidence')).TrimEnd('\')
if (-not $evidenceRoot.StartsWith($permittedEvidence + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Runtime request evidenceRoot escaped the KMC runtime-evidence root.'
}

if ([bool]$request.saveAccessAllowed) {
    throw 'Save-backed runtime is not authorized without the exact disposable fixture pair.'
}
else {
    if ($null -ne $request.saveName -and -not [string]::IsNullOrEmpty([string]$request.saveName)) {
        throw 'No-save request contains a save name.'
    }
}

if (-not [string]::IsNullOrWhiteSpace($PackageManifestPath)) {
    $manifest = Read-KmcJson $PackageManifestPath
    if ([string]$manifest.commit -cne [string]$request.commit -or
        [string]$manifest.branch -cne [string]$request.branch -or
        [string]$manifest.version -cne [string]$request.productVersion -or
        [string]$manifest.dllSha256 -cne [string]$request.dllSha256 -or
        [string]$manifest.dllMvid -cne [string]$request.dllMvid -or
        $manifest.worktreeClean -ne $true -or $manifest.qualificationEligible -ne $true -or [int]$manifest.schemaVersion -ne 2) {
        throw 'Runtime request does not match the clean package manifest.'
    }
}

Write-Host 'TOTAL PASS=12 FAIL=0'
