[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId=('fixture-recovery-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')),
    [Parameter(Mandatory=$true)][string]$ProtectedSaveContinuityAuthorityPath,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedSaveContinuityAuthoritySha256
)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$labRoot=Get-KmcLabRoot;$stateRoot=Join-Path $labRoot 'runtime-state';$backupRoot=Join-Path $labRoot 'runtime-backups'
$intake=Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json')|ConvertFrom-Json
$authorityPath=Assert-KmcChildPath $ProtectedSaveContinuityAuthorityPath (Join-Path $stateRoot 'protected-save-authorities') 'fixture-recovery historical authority'
Assert-KmcRecoveryLeafNoLinks $authorityPath 'fixture-recovery historical authority'
if((Get-KmcSha256 $authorityPath)-cne$ExpectedProtectedSaveContinuityAuthoritySha256){throw 'Fixture-recovery authority SHA-256 differs.'}
$arguments=@{RunId=$RunId;SaveRoot=[string]$intake.requestedLayout.kingmakerSaveRoot;StateRoot=$stateRoot;BackupRoot=$backupRoot;QualificationPath=(Join-Path $stateRoot 'fixture-qualification.json');HistoricalAuthority=(Read-KmcJson $authorityPath)}
if($WhatIfPreference){$arguments['WhatIf']=$true}
Invoke-KmcQualifiedWorkingFixtureRecovery @arguments|ConvertTo-Json -Compress
