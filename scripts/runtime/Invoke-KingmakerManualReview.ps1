[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
    [ValidateRange(360,900)][int]$ReadyTimeoutSeconds = 900,
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPackageSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPackageManifestSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedDllSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._/-]{1,200}$')][string]$ExpectedBranch,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedCommit,
    [Parameter(Mandatory = $true)][string]$QualificationSuiteSnapshotPath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedQualificationSuiteId,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedQualificationSuiteSnapshotSha256,
    [string]$SteamPath = 'C:\Program Files (x86)\Steam\steam.exe'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false
$actualRunId = if ([string]::IsNullOrWhiteSpace($RunId)) {
    [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ') + '-manual-visual-review'
}
else { $RunId }

$invoke = @{
    Scenario = 'manual-visual-review'
    RunId = $actualRunId
    TimeoutSeconds = $ReadyTimeoutSeconds
    SaveAccessAllowed = $true
    PackagePath = $PackagePath
    ExpectedPackageSha256 = $ExpectedPackageSha256
    ExpectedPackageManifestSha256 = $ExpectedPackageManifestSha256
    ExpectedDllSha256 = $ExpectedDllSha256
    ExpectedBranch = $ExpectedBranch
    ExpectedCommit = $ExpectedCommit
    QualificationSuiteSnapshotPath = $QualificationSuiteSnapshotPath
    ExpectedQualificationSuiteId = $ExpectedQualificationSuiteId
    ExpectedQualificationSuiteSnapshotSha256 = $ExpectedQualificationSuiteSnapshotSha256
    SteamPath = $SteamPath
}
if ($requestedWhatIf) { $invoke['WhatIf'] = $true }
elseif ($PSBoundParameters.ContainsKey('Confirm')) { $invoke['Confirm'] = [bool]$PSBoundParameters['Confirm'] }

& (Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeScenario.ps1') @invoke
