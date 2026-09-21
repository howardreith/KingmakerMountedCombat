[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$SnapshotSha256,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$CurrentParamsSha256,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$CurrentPrefsSha256
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'PersistenceProfileProtection.ps1')
$lab=Get-KmcLabRoot;$stateRoot=Join-Path $lab 'runtime-state';$backups=Join-Path $lab 'runtime-backups'
Assert-KmcNoGameProcesses
if(-not(Wait-KmcStableNoKingmakerProcess -ExpectedProcessId 0)){throw 'Game process absence is not stable.'}
$snapshotPath=Assert-KmcChildPath (Join-Path $backups ('profile-'+$RunId+'/snapshot.json')) $backups 'profile snapshot'
Assert-KmcRecoveryLeafNoLinks $snapshotPath 'profile snapshot'
if((Get-KmcSha256 $snapshotPath)-cne$SnapshotSha256){throw 'Profile snapshot pin differs.'}
$snapshot=Read-KmcJson $snapshotPath
$raw=Read-KmcJson (Join-Path $stateRoot 'active-transaction.lock')
$state=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath $stateRoot $RunId)
if($raw.runId-cne$RunId-or$snapshot.runId-cne$RunId-or$raw.token-cne$snapshot.token-or
    $state.token-cne$raw.token-or$state.phase-cne'restored'){throw 'Profile recovery has no exact otherwise-restored transaction.'}
if((Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$state.restoredModsDigest-or
    (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$state.restoredSaveInventoryDigest){
    throw 'Current Mods or saves differ from the completed transaction; refusing stale recovery.'
}
$prefs=Get-KmcPersistencePlayerPrefs
if((Get-KmcSha256 $snapshot.paramsPath)-cne$CurrentParamsSha256-or(Get-KmcTextSha256 $prefs)-cne$CurrentPrefsSha256){
    throw 'Current settings differ from the exact reviewed recovery pins.'
}
Assert-KmcObservedPreparationTimeoutRecord $RunId
[void]@(Get-KmcPersistencePreferenceChanges -BeforeJson $snapshot.playerPrefsJson -AfterJson $prefs -ObservedResetRunId $RunId)
$original=Join-Path $backups ('profile-'+$RunId+'/Params.xml')
Assert-KmcNativeUmmStartupDelta -Before ([IO.File]::ReadAllText($original)) -After ([IO.File]::ReadAllText($snapshot.paramsPath))
if(-not$PSCmdlet.ShouldProcess($RunId,'restore exact observed startup settings drift and release otherwise-restored runtime lock')){
    Write-Host 'PROFILE RECOVERY WHATIF PASS; no file, registry or lock mutation.'
    return
}
$lock=$null
try{
    $lock=Adopt-KmcStaleRuntimeLock $stateRoot
    Restore-KmcPersistenceStartupSettings -Lock $lock -Snapshot $snapshot -BackupRoot $backups -ExpectedCurrentParamsSha256 $CurrentParamsSha256 -ExpectedCurrentPrefsSha256 $CurrentPrefsSha256 -Confirm:$false
    Close-KmcRuntimeLock $lock;$lock=$null
    Write-KmcJsonCreateNewDurable -Path (Join-Path $lab ('runtime-evidence/'+$RunId+'/profile-recovery.json')) -Value ([ordered]@{
        runId=$RunId;status='PASS';recoveredAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
        snapshotSha256=$SnapshotSha256;paramsSha256=$snapshot.paramsSha256
        prefsSha256=Get-KmcTextSha256 $snapshot.playerPrefsJson;nativeScenarioStatus='FAIL'
        protectedSavesUnchanged=$true;foreignModsUnchanged=$true;gameAbsent=$true;lockReleased=$true
    })
    Write-Host 'PROFILE RECOVERY PASS; exact actual intake restored, game absent, lock released.'
}finally{if($null-ne$lock){Abandon-KmcRuntimeLock $lock}}
