[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param()

# Exact one-run recovery for a timeout whose combined Mods/save transaction was
# restored before its separate native-startup profile snapshot was considered.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'PersistenceProfileProtection.ps1')
$requestedWhatIf=[bool]$WhatIfPreference;$WhatIfPreference=$false
$lab=Get-KmcLabRoot;$stateRoot=Join-Path $lab 'runtime-state';$backupRoot=Join-Path $lab 'runtime-backups'
$runId='20261020-c7b-world94-b81s3-small-native-dismiss'
$expectedSnapshot='b607214bb3adfbeeedae16046c92ed432754ef72b4a9346d0028c0df84b9137e'
$expectedParamsNow='3ef54abd54572382ccc716e3577788fc08f57ff12cad540cbf583e91a5d3c84c'
$expectedPrefsNow='0d6c7116cd7654b11c66ed06c14e04f2f5d50eb072b73900e6f5f7c32d5907b6'
$expectedOriginalResult='7ff9569ecb7e299fa114c20751c9520efb355cb1676c6973e13786773aaf9a0c'
$expectedMods='b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda'
$expectedSaves='df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab'
$receiptPath=Join-Path $lab ('runtime-evidence/'+$runId+'/profile-recovery-after-combined.json')
if(Test-Path -LiteralPath $receiptPath){throw 'Exact profile recovery already has a receipt.'}
Assert-KmcNoGameProcesses
if(-not(Wait-KmcStableNoKingmakerProcess -ExpectedProcessId 0)){throw 'No-game interval is unstable.'}
if(Test-Path -LiteralPath (Join-Path $stateRoot 'active-transaction.lock')){throw 'Another runtime transaction owns the installation.'}
$state=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath $stateRoot $runId)
$snapshotPath=Assert-KmcChildPath (Join-Path $backupRoot ('profile-'+$runId+'/snapshot.json')) $backupRoot 'exact timed-out profile snapshot'
Assert-KmcRecoveryLeafNoLinks $snapshotPath 'exact timed-out profile snapshot'
if((Get-KmcSha256 $snapshotPath)-cne$expectedSnapshot){throw 'Profile snapshot differs from the exact captured bytes.'}
$snapshot=Read-KmcPersistenceProfileSnapshot $snapshotPath
$resultPath=Join-Path $lab ('runtime-evidence/'+$runId+'/runtime-result.json')
$resultPath=Assert-KmcChildPath $resultPath (Join-Path $lab 'runtime-evidence') 'exact timed-out result'
Assert-KmcRecoveryLeafNoLinks $resultPath 'exact timed-out result'
if((Get-KmcSha256 $resultPath)-cne$expectedOriginalResult){throw 'Original timed-out result differs from its reviewed bytes.'}
$result=Read-KmcJson $resultPath
$orchestration=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$runId+'/orchestration.json'))
if($state.runId-cne$runId-or$state.token-cne$snapshot.token-or$state.phase-cne'restored'-or
   $state.modsRestored-ne$true-or$state.saveProtectionPassed-ne$true-or$state.workingRestored-ne$true-or
   $state.baselineImmutable-ne$true-or$state.saveWriteAllowlistPassed-ne$true-or
   $state.restoredModsDigest-cne$expectedMods-or$state.restoredSaveInventoryDigest-cne$expectedSaves-or
   $result.runId-cne$runId-or$result.status-cne'FAIL'-or$orchestration.stage-cne'restoration-blocked'){
 throw 'Exact timed-out transaction did not reach verified combined restoration.'
}
if((Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$expectedMods-or
   (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$expectedSaves){
 throw 'Current Mods or protected saves differ from the restored transaction.'
}
$intake=Read-KmcJson (Join-Path $lab 'environment-intake.json')
$paramsPath=[IO.Path]::GetFullPath((Join-Path $intake.requestedLayout.kingmakerInstallDir 'Kingmaker_Data/Managed/UnityModManager/Params.xml'))
if($snapshot.runId-cne$runId-or[IO.Path]::GetFullPath($snapshot.paramsPath)-cne$paramsPath-or
   [IO.Path]::GetFullPath($snapshot.profile)-cne[IO.Path]::GetFullPath((Split-Path $intake.requestedLayout.kingmakerSaveRoot))){
 throw 'Captured profile paths differ from current exact intake.'
}
$saved=Assert-KmcChildPath (Join-Path $backupRoot ('profile-'+$runId+'/Params.xml')) $backupRoot 'captured UMM parameters'
Assert-KmcRecoveryLeafNoLinks $saved 'captured UMM parameters'
Assert-KmcRecoveryLeafNoLinks $paramsPath 'current UMM parameters'
if((Get-KmcSha256 $saved)-cne$snapshot.paramsSha256-or
   (Get-KmcSha256 $paramsPath)-cne$expectedParamsNow){throw 'Captured/current UMM parameter bytes differ from reviewed pins.'}
$prefs=Get-KmcPersistencePlayerPrefs
if((Get-KmcTextSha256 $prefs)-cne$expectedPrefsNow){throw 'Current PlayerPrefs differ from reviewed pin.'}
$profile=Get-KmcQualificationTreeInventory -Root $snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
$profileDelta=@(Get-KmcPersistenceProfileRecoveryDelta $snapshot $profile)
if($profileDelta.Count-ne0-or(Get-KmcPersistenceProfileDigest $profile)-cne$snapshot.profileDigest){
 throw 'Unexpected profile/cache delta cannot be restored by this exact case.'
}
$modsState=Read-KmcJson (Get-KmcTransactionStatePath $stateRoot $runId)
Assert-KmcBaseGameSettingsAuthority -State $modsState -RunId $runId -Token $snapshot.token -GameRoot ([string]$intake.requestedLayout.kingmakerInstallDir)
Assert-KmcNativeUmmStartupDelta -Before ([IO.File]::ReadAllText($saved)) -After ([IO.File]::ReadAllText($paramsPath)) -BaseGameOnly
$changes=@(Get-KmcPersistencePreferenceChanges -BeforeJson $snapshot.playerPrefsJson -AfterJson $prefs -ObservedResetRunId $runId)
if($changes.Count-ne2-or(@($changes.name|Sort-Object)-join'|')-cne'unity.player_session_count_h922449978|unity.player_sessionid_h1351336811'){
 throw 'PlayerPrefs drift is not the exact two admitted native session keys.'
}
$WhatIfPreference=$requestedWhatIf
if(-not$PSCmdlet.ShouldProcess($runId,'restore pinned native startup parameters/preferences after combined timeout recovery')){
 Write-Host 'POST-COMBINED PROFILE RECOVERY WHATIF PASS; no file, registry or lock mutation.'
 return
}
$WhatIfPreference=$false
$lock=$null
try{
 $lock=Open-KmcRuntimeLock -StateRoot $stateRoot -RunId ($runId+'-profile-recovery')
 [void](Assert-KmcRuntimeLockOwner $lock);Assert-KmcNoGameProcesses
 Assert-KmcRecoveryLeafNoLinks $snapshotPath 'exact timed-out profile snapshot under recovery lock'
 Assert-KmcRecoveryLeafNoLinks $resultPath 'exact timed-out result under recovery lock'
 if((Get-KmcSha256 $snapshotPath)-cne$expectedSnapshot-or
    (Get-KmcSha256 $resultPath)-cne$expectedOriginalResult){
  throw 'Exact failed-run snapshot or result changed under recovery lock.'
 }
 $underState=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath $stateRoot $runId)
 if($underState.runId-cne$runId-or$underState.token-cne$snapshot.token-or$underState.phase-cne'restored'-or
    $underState.modsRestored-ne$true-or$underState.saveProtectionPassed-ne$true-or
    $underState.workingRestored-ne$true-or$underState.baselineImmutable-ne$true-or
    $underState.saveWriteAllowlistPassed-ne$true-or
    $underState.restoredModsDigest-cne$expectedMods-or
    $underState.restoredSaveInventoryDigest-cne$expectedSaves){
  throw 'Exact failed-run restoration authority changed under recovery lock.'
 }
 $underModsState=Read-KmcJson (Get-KmcTransactionStatePath $stateRoot $runId)
 Assert-KmcBaseGameSettingsAuthority -State $underModsState -RunId $runId -Token $snapshot.token -GameRoot ([string]$intake.requestedLayout.kingmakerInstallDir)
 Assert-KmcRecoveryLeafNoLinks $saved 'captured UMM parameters under recovery lock'
 Assert-KmcRecoveryLeafNoLinks $paramsPath 'current UMM parameters under recovery lock'
 $capturedParams=[IO.File]::ReadAllBytes($saved)
 $hasher=[Security.Cryptography.SHA256]::Create()
 try{$capturedHash=([BitConverter]::ToString($hasher.ComputeHash($capturedParams))).Replace('-','').ToLowerInvariant()}
 finally{$hasher.Dispose()}
 if($capturedHash-cne$snapshot.paramsSha256){
  throw 'Captured UMM parameter bytes changed after review.'
 }
 if((Get-KmcSha256 $paramsPath)-cne$expectedParamsNow-or
    (Get-KmcTextSha256 (Get-KmcPersistencePlayerPrefs))-cne$expectedPrefsNow-or
    (Get-KmcPersistenceProfileDigest (Get-KmcQualificationTreeInventory -Root $snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')))-cne$snapshot.profileDigest-or
    (Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$expectedMods-or
    (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$expectedSaves){
  throw 'External state changed after exact profile recovery review.'
 }
 [IO.File]::WriteAllBytes($paramsPath,$capturedParams)
 [IO.File]::SetLastWriteTimeUtc($paramsPath,[IO.File]::GetLastWriteTimeUtc($saved))
 $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Owlcat Games\Pathfinder Kingmaker',$true)
 try{
  if($null-eq$key){throw 'Exact Kingmaker PlayerPrefs key disappeared.'}
  foreach($change in $changes){
   $key.SetValue($change.name,[Convert]::FromBase64String($change.before),[Microsoft.Win32.RegistryValueKind]::Binary)
  }
  $key.Flush()
 }finally{if($null-ne$key){$key.Dispose()}}
 [void](Assert-KmcPersistenceProfileUnchanged $snapshot)
 if((Get-KmcPersistenceProfileDigest (Get-KmcQualificationTreeInventory -Root $snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')))-cne$snapshot.profileDigest-or
    (Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$expectedMods-or
    (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$expectedSaves){
  throw 'Mods or saves changed during exact profile restoration.'
 }
 $receipt=[ordered]@{
  schemaVersion=1;kind='exact-post-combined-profile-recovery';runId=$runId;status='PASS'
  snapshotSha256=$expectedSnapshot;restoredParamsSha256=$snapshot.paramsSha256
  restoredPrefsSha256=(Get-KmcTextSha256 $snapshot.playerPrefsJson)
  modsDigest=$expectedMods;saveInventoryDigest=$expectedSaves;gameAbsent=$true
  originalScenarioStatus='FAIL';recoveredAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
 }
 Close-KmcRuntimeLock $lock;$lock=$null
 Write-KmcJsonCreateNewDurable -Path $receiptPath -Value $receipt
 Write-Host 'PASS exact post-combined profile recovery; original native scenario remains FAIL.'
}finally{if($null-ne$lock){Abandon-KmcRuntimeLock $lock}}
