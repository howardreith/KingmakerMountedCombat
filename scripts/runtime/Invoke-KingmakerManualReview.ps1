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
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedCurrentQualificationSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
    [Parameter(Mandatory = $true)][string]$PriorSaveTransactionStatePath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedPriorSaveTransactionRunId,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveTransactionStateSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveMetadataDigest,
    [Parameter(Mandatory = $true)][string]$ProtectedSaveContinuityAuthorityPath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedProtectedSaveContinuityEpochId,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedSaveContinuityAuthoritySha256,
    [string]$ExpectedProtectedAutoSaveName,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedAutoSaveSha256,
    [string]$ExpectedProtectedQuickSaveName,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedQuickSaveSha256,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedSavePinSetSha256,
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
    ExpectedCurrentQualificationSha256 = $ExpectedCurrentQualificationSha256
    ExpectedSupersededWorkingSha256 = $ExpectedSupersededWorkingSha256
    PriorSaveTransactionStatePath = $PriorSaveTransactionStatePath
    ExpectedPriorSaveTransactionRunId = $ExpectedPriorSaveTransactionRunId
    ExpectedPriorSaveTransactionStateSha256 = $ExpectedPriorSaveTransactionStateSha256
    ExpectedPriorSaveMetadataDigest = $ExpectedPriorSaveMetadataDigest
    ProtectedSaveContinuityAuthorityPath = $ProtectedSaveContinuityAuthorityPath
    ExpectedProtectedSaveContinuityEpochId = $ExpectedProtectedSaveContinuityEpochId
    ExpectedProtectedSaveContinuityAuthoritySha256 = $ExpectedProtectedSaveContinuityAuthoritySha256
    SteamPath = $SteamPath
}
foreach ($pinName in @(
    'ExpectedProtectedAutoSaveName','ExpectedProtectedAutoSaveSha256',
    'ExpectedProtectedQuickSaveName','ExpectedProtectedQuickSaveSha256',
    'ExpectedProtectedSavePinSetSha256'
)) {
    if ($PSBoundParameters.ContainsKey($pinName)) { $invoke[$pinName] = $PSBoundParameters[$pinName] }
}
if ($requestedWhatIf) { $invoke['WhatIf'] = $true }
elseif ($PSBoundParameters.ContainsKey('Confirm')) { $invoke['Confirm'] = [bool]$PSBoundParameters['Confirm'] }

& (Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeScenario.ps1') @invoke
