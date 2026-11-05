[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param([switch]$ContractOnly)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$dry=[bool]$WhatIfPreference;$WhatIfPreference=$false
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'PersistenceProfileProtection.ps1')

# One failed run only. Do not widen the normal startup preference policy.
function Get-F1CreatorPreferenceRestoration([string]$Before,[string]$Observed,[string]$Current) {
 if((Get-KmcTextSha256 $Before)-cne'6941565f2f648df82894ed842385cbc13363bbf85cf46bea70bb64a1cad17787'-or
    (Get-KmcTextSha256 $Observed)-cne'd5806e80fa830427804ea0ef33a52cbb4fc76c360565b392d7ab8e5ce76ad0e0'){throw 'Original preference evidence differs.'}
 $prior=ConvertFrom-Json $Before;$seen=ConvertFrom-Json $Observed;$now=ConvertFrom-Json $Current
 $names=@('AutoLevelup_h3494002937','CriticalHits_h3063476040','DamageToParty_h3737204571',
 'DeadCompanionsRiseAfterCombat_h2349052283','DeathDoor_h1819350543','EnemyDifficulty_h2919976620',
 'EternalKingdom_h3591390253','KingdomDifficulty_h4200925179','OnlyInitiatorReceiveSkillCheckExperience_h3577921032',
 'RemoveNegativeEffectsOnRest_h3051215733','StartGameTutorialMesseges_h973415547','StatsAdjustments_h841702840',
 'unity.player_session_count_h922449978','unity.player_sessionid_h1351336811')
 foreach($items in @($prior,$seen,$now)) {
  $unique=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach($item in $items){if(-not$unique.Add([string]$item.name)){throw 'Duplicate preference.'}}
 }
 foreach($item in $now){if(@($prior|Where-Object name -CEQ $item.name).Count-ne1){throw 'Unexpected preference addition.'}}
 foreach($old in $prior){
  $s=@($seen|Where-Object name -CEQ $old.name);$n=@($now|Where-Object name -CEQ $old.name)
  $oldText=$old|ConvertTo-Json -Compress
  $seenText=if($s.Count){$s[0]|ConvertTo-Json -Compress}else{''}
  $nowText=if($n.Count){$n[0]|ConvertTo-Json -Compress}else{''}
  if($nowText-cne$oldText-and$nowText-cne$seenText){throw 'Unobserved preference value or deletion.'}
  if($oldText-cne$seenText){
   if($old.name-cnotin$names-or$old.kind-cne'Binary'-or($s.Count-and$s[0].kind-cne'Binary')){throw 'Unreviewed preference delta.'}
   if($n.Count-eq0-and$old.name-cnotin@('OnlyInitiatorReceiveSkillCheckExperience_h3577921032','StartGameTutorialMesseges_h973415547')){throw 'Unreviewed missing preference.'}
   if($nowText-cne$oldText){$old}
  }
 }
}
if($ContractOnly){return}
$lab=Get-KmcLabRoot;$run='20261105-finalize-f1c-owner-creator-01';$stateRoot=Join-Path $lab 'runtime-state'
$evidence=Join-Path $lab ('runtime-evidence/'+$run);$backup=Join-Path $lab ('runtime-backups/profile-'+$run)
$snapshotPath=Join-Path $backup 'snapshot.json';$saved=Join-Path $backup 'Params.xml'
$receipt=Join-Path $evidence 'reviewed-preferences-restoration.json'
function Assert-F1Recovery($HeldLock=$null) {
 Assert-KmcNoGameProcesses
 foreach($path in @($snapshotPath,$saved,(Join-Path $evidence 'preferences-after-exit.json'))){Assert-KmcRecoveryLeafNoLinks $path 'exact F1 recovery input'}
 if((Get-KmcSha256 $snapshotPath)-cne'90588536e15c3116194a4b82272cc706c12f7c23e97d47dba38f0bd0b50b27c5'-or
    (Get-KmcSha256 $saved)-cne'ca5d402248723c3578c4a35a21d69b749d7459a4a74c6f406e16f0d2fa9386c4'-or
    (Get-KmcSha256 (Join-Path $evidence 'runtime-result.json'))-cne'e765aa6339fe0c8ed4a2a4fc407281b25bf7c48c984330a536244e1fd5df17c0'){throw 'Original recovery input changed.'}
 $script:snapshot=Read-KmcPersistenceProfileSnapshot $snapshotPath
 Assert-KmcRecoveryLeafNoLinks $snapshot.paramsPath 'current exact UMM parameters'
 $script:state=Read-KmcRunTransactionState (Get-KmcRunTransactionStatePath $stateRoot $run)
 $raw=if($null-ne$HeldLock){[void](Assert-KmcRuntimeLockOwner $HeldLock);[pscustomobject]@{runId=$HeldLock.RunId;token=$HeldLock.Token}}else{Read-KmcJson (Join-Path $stateRoot 'active-transaction.lock')}
 if($raw.runId-cne$run-or$raw.token-cne'02cdfbe10c95284bf1463bbf4d296dd381db2202758c285c864dbe2747d413c5'-or
    $snapshot.token-cne$raw.token-or$state.token-cne$raw.token-or$state.phase-cne'restored'-or
    -not$state.modsRestored-or-not$state.saveProtectionPassed-or-not$state.baselineImmutable-or-not$state.workingRestored-or
    -not$state.saveWriteAllowlistPassed-or@($state.restorationErrors).Count-ne0){throw 'Otherwise-restored exact transaction absent.'}
 $intake=Read-KmcJson (Join-Path $lab 'environment-intake.json')
 if([IO.Path]::GetFullPath($snapshot.paramsPath)-cne[IO.Path]::GetFullPath((Join-Path $intake.requestedLayout.kingmakerInstallDir 'Kingmaker_Data/Managed/UnityModManager/Params.xml'))-or
    [IO.Path]::GetFullPath($snapshot.profile)-cne[IO.Path]::GetFullPath((Split-Path $intake.requestedLayout.kingmakerSaveRoot))){throw 'Recovery path differs from intake.'}
 if((Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$state.modsDigestBefore-or
    (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$state.saveInventoryDigestBefore){throw 'Mods or saves changed.'}
 $suite=Read-KmcQualificationSuiteSnapshot -Path $state.qualificationSuiteSnapshotPath -StateRoot $stateRoot -ExpectedSuiteId $state.qualificationSuiteId -ExpectedSha256 $state.qualificationSuiteSnapshotSha256
 [void](Assert-KmcQualificationSuiteExternalState -Snapshot $suite -SaveRoot $state.saveRoot -ModsRoot $state.liveModsRoot)
 [void]@(Get-KmcPersistenceProfileRecoveryDelta $snapshot (Get-KmcQualificationTreeInventory -Root $snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')) -EmptyNewLeaf Admit)
 $script:observed=[IO.File]::ReadAllText((Join-Path $evidence 'preferences-after-exit.json'))
 $script:prefs=Get-KmcPersistencePlayerPrefs
 $script:changes=@(Get-F1CreatorPreferenceRestoration $snapshot.playerPrefsJson $observed $prefs)
 $params=Get-KmcSha256 $snapshot.paramsPath
 if($params-cnotin@($snapshot.paramsSha256,'0907de11cf267751128889e4c8e0fc110c467e5ec9b83ea64fe141e629e14324')){throw 'UMM parameters changed.'}
 $mods=Read-KmcJson (Get-KmcTransactionStatePath $stateRoot $run)
 Assert-KmcBaseGameSettingsAuthority -State $mods -RunId $run -Token $snapshot.token -GameRoot $intake.requestedLayout.kingmakerInstallDir
 Assert-KmcNativeUmmStartupDelta -Before ([IO.File]::ReadAllText($saved)) -After ([IO.File]::ReadAllText($snapshot.paramsPath)) -BaseGameOnly -StagedModIds (Get-KmcRunRetainedDependencyIds $run)
}
if(Test-Path $receipt){throw 'Append-only recovery receipt already exists.'}
Assert-F1Recovery
if($dry-or-not$PSCmdlet.ShouldProcess($run,'restore only pinned creation/session preferences and captured UMM settings, preserving native FAIL')){Write-Host 'PASS exact F1 preference recovery WhatIf; no mutation.';return}
$lock=$null
try {
 $lock=Adopt-KmcStaleRuntimeLock $stateRoot
 Assert-F1Recovery -HeldLock $lock
 [void](Assert-KmcRuntimeLockOwner $lock)
 if((Get-KmcPersistencePlayerPrefs)-cne$prefs){throw 'Preference race before restoration.'}
 $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Owlcat Games\Pathfinder Kingmaker',$true)
 try{foreach($change in $changes){$key.SetValue($change.name,[Convert]::FromBase64String($change.value),[Microsoft.Win32.RegistryValueKind]::Binary)};$key.Flush()}finally{$key.Dispose()}
 [IO.File]::WriteAllBytes($snapshot.paramsPath,[IO.File]::ReadAllBytes($saved))
 [IO.File]::SetLastWriteTimeUtc($snapshot.paramsPath,[IO.File]::GetLastWriteTimeUtc($saved))
 $accepted=@(Assert-KmcPersistenceProfileUnchanged $snapshot -EmptyNewLeaf Admit)
 if((Get-KmcPersistencePlayerPrefs)-cne$snapshot.playerPrefsJson-or(Get-KmcSha256 $snapshot.paramsPath)-cne$snapshot.paramsSha256){throw 'Restored settings differ.'}
 Write-KmcJsonCreateNewDurable -Path $receipt -Value ([ordered]@{runId=$run;nativeVerdict='FAIL';recoveredAtUtc=[DateTimeOffset]::UtcNow.ToString('o');restoredPreferenceCount=$changes.Count;paramsRestoredExactly=$true;preferencesRestoredExactly=$true;acceptedNativeCacheChanges=$accepted;profileBytesRestoredExactly=($accepted.Count-eq0);modsRestored=$true;protectedSavesRestored=$true;originalRuntimeResultSha256='e765aa6339fe0c8ed4a2a4fc407281b25bf7c48c984330a536244e1fd5df17c0'})
 Close-KmcRuntimeLock $lock;$lock=$null
 Write-Host 'PASS exact F1 settings restored; original FAIL retained; lock closed.'
}finally{if($null-ne$lock){Abandon-KmcRuntimeLock $lock}}
