[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param()

# One observed failed P07 launch: the combined Mods/save transaction was restored,
# but UMM appended KMC and advanced its update-check timestamp. Restore only its
# pinned pre-run settings after proving that these are the entire settings delta.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'PersistenceProfileProtection.ps1')

$runId='20261019-c7b-p07-head7348-b32s2-removal'
$snapshotHash='5764a09e100caba3951f78e24d1ab89a85243d375410654f40fcb7afe4d0aa39'
$beforeParamsHash='1bdebe8e9f97f5b58a3cf0de0cec48f84764e52e54b61e4df74a2cb96e6d8696'
$currentParamsHash='4fffb80b3f242933cd618cf3451f7a995777e015e3270a934e2e17e3dc8da021'
$beforePrefsHash='ac5553ebbc0a2a106e1c5c792357fe359fb5e7c5fbebf7385d6cf1e79062786f'
$currentPrefsHash='e60582989e98df8687736ce4e5841dd00cd138e79cea4de25646049f73f47aca'
$newUpdateCheck='2026-10-19T05:31:20.3193157-04:00'
$kmcEntry='<Mod Id="KingmakerMountedCombat" Enabled="true"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod>'
$lab=Get-KmcLabRoot
$stateRoot=Join-Path $lab 'runtime-state'
$backupRoot=Join-Path $lab 'runtime-backups'
$evidenceRoot=Join-Path $lab ('runtime-evidence/'+$runId)
$receipt=Join-Path $evidenceRoot 'profile-recovery-exact-p07.json'
if(Test-Path -LiteralPath $receipt){throw 'Exact P07 profile recovery already has a receipt.'}
Assert-KmcNoGameProcesses
if(-not(Wait-KmcStableNoKingmakerProcess -ExpectedProcessId 0)){throw 'No-game interval is unstable.'}

$lockPath=Join-Path $stateRoot 'active-transaction.lock'
$raw=Read-KmcJson $lockPath
$state=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath $stateRoot $runId)
$result=Read-KmcJson (Join-Path $evidenceRoot 'runtime-result.json')
$orchestration=Read-KmcJson (Join-Path $evidenceRoot 'orchestration.json')
$snapshotPath=Assert-KmcChildPath (Join-Path $backupRoot ('profile-'+$runId+'/snapshot.json')) $backupRoot 'exact P07 snapshot'
Assert-KmcRecoveryLeafNoLinks $snapshotPath 'exact P07 snapshot'
if((Get-KmcSha256 $snapshotPath)-cne$snapshotHash){throw 'P07 profile snapshot changed.'}
$snapshot=Read-KmcPersistenceProfileSnapshot $snapshotPath
if($raw.runId-cne$runId-or$raw.token-cne$snapshot.token-or$state.token-cne$raw.token-or
   $state.phase-cne'restored'-or$state.modsRestored-ne$true-or$state.saveProtectionPassed-ne$true-or
   $state.workingRestored-ne$true-or$state.baselineImmutable-ne$true-or
   $state.saveWriteAllowlistPassed-ne$true-or$result.status-cne'FAIL'-or
   $orchestration.stage-cne'restoration-blocked'){
    throw 'P07 has no exact otherwise-restored failed transaction.'
}
if((Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$state.restoredModsDigest-or
   (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$state.restoredSaveInventoryDigest){
    throw 'Current Mods or saves differ from P07 restored transaction.'
}

$intake=Read-KmcJson (Join-Path $lab 'environment-intake.json')
$paramsPath=[IO.Path]::GetFullPath((Join-Path $intake.requestedLayout.kingmakerInstallDir 'Kingmaker_Data/Managed/UnityModManager/Params.xml'))
if([IO.Path]::GetFullPath($snapshot.paramsPath)-cne$paramsPath-or
   [IO.Path]::GetFullPath($snapshot.profile)-cne[IO.Path]::GetFullPath((Split-Path $intake.requestedLayout.kingmakerSaveRoot))){
    throw 'P07 profile paths differ from exact current intake.'
}
$saved=Assert-KmcChildPath (Join-Path $backupRoot ('profile-'+$runId+'/Params.xml')) $backupRoot 'captured UMM parameters'
Assert-KmcRecoveryLeafNoLinks $saved 'captured UMM parameters'
Assert-KmcRecoveryLeafNoLinks $paramsPath 'current UMM parameters'
if($snapshot.paramsSha256-cne$beforeParamsHash-or(Get-KmcSha256 $saved)-cne$beforeParamsHash-or
   (Get-KmcSha256 $paramsPath)-cne$currentParamsHash){throw 'P07 UMM parameter pins differ.'}
[xml]$before=[IO.File]::ReadAllText($saved)
[xml]$current=[IO.File]::ReadAllText($paramsPath)
$added=@($current.SelectNodes("//Mod[@Id='KingmakerMountedCombat']"))
$prior=@($before.SelectNodes("//Mod[@Id='KingmakerMountedCombat']"))
$beforeTime=@($before.SelectNodes('//LastUpdateCheck'))
$currentTime=@($current.SelectNodes('//LastUpdateCheck'))
if($prior.Count-ne0-or$added.Count-ne1-or$added[0].OuterXml-cne$kmcEntry-or
   $beforeTime.Count-ne1-or$currentTime.Count-ne1-or$currentTime[0].InnerText-cne$newUpdateCheck){
    throw 'P07 UMM change is not the exact observed KMC append and update-check value.'
}
[void]$added[0].ParentNode.RemoveChild($added[0])
$currentTime[0].InnerText=$beforeTime[0].InnerText
if($current.OuterXml-cne$before.OuterXml){throw 'P07 UMM parameters contain another change.'}

$profile=Get-KmcQualificationTreeInventory -Root $snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
if(@(Get-KmcPersistenceProfileRecoveryDelta $snapshot $profile).Count-ne0-or
   (Get-KmcPersistenceProfileDigest $profile)-cne$snapshot.profileDigest){
    throw 'P07 profile/cache differs from its exact snapshot.'
}
$prefs=Get-KmcPersistencePlayerPrefs
if((Get-KmcTextSha256 $snapshot.playerPrefsJson)-cne$beforePrefsHash-or
   (Get-KmcTextSha256 $prefs)-cne$currentPrefsHash){throw 'P07 PlayerPrefs pins differ.'}
$changes=@(Get-KmcPersistencePreferenceChanges -BeforeJson $snapshot.playerPrefsJson -AfterJson $prefs -ObservedResetRunId $runId)
if(($changes.name|Sort-Object)-join'|' -cne
   'KingdomDifficulty_h4200925179|unity.player_session_count_h922449978|unity.player_sessionid_h1351336811'){
    throw 'P07 PlayerPrefs changed outside the exact three native startup keys.'
}
if(-not$PSCmdlet.ShouldProcess($runId,'restore pinned UMM parameters and three native PlayerPrefs changes; release otherwise-restored lock')){
    Write-Host 'P07 PROFILE RECOVERY WHATIF PASS; exact two-field UMM delta and three native preference keys; no mutation.'
    return
}

$lock=$null
try{
    $lock=Adopt-KmcStaleRuntimeLock $stateRoot
    [void](Assert-KmcRuntimeLockOwner $lock)
    if($lock.RunId-cne$runId-or$lock.Token-cne$snapshot.token){
        throw 'Adopted lock is not the exact failed P07 transaction.'
    }
    $state=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath $stateRoot $runId) -Lock $lock
    if($state.token-cne$lock.Token-or$state.phase-cne'restored'-or
       $state.modsRestored-ne$true-or$state.saveProtectionPassed-ne$true-or
       $state.workingRestored-ne$true-or$state.baselineImmutable-ne$true-or
       $state.saveWriteAllowlistPassed-ne$true){
        throw 'Adopted P07 transaction lost its otherwise-restored authority.'
    }
    Assert-KmcNoGameProcesses
    Assert-KmcRecoveryLeafNoLinks $saved 'captured UMM parameters under recovery lock'
    Assert-KmcRecoveryLeafNoLinks $paramsPath 'current UMM parameters under recovery lock'
    $capturedParams=[IO.File]::ReadAllBytes($saved)
    $hasher=[Security.Cryptography.SHA256]::Create()
    try{$capturedHash=([BitConverter]::ToString($hasher.ComputeHash($capturedParams))).Replace('-','').ToLowerInvariant()}
    finally{$hasher.Dispose()}
    if($capturedHash-cne$beforeParamsHash){throw 'Captured P07 UMM parameters changed after review.'}
    if((Get-KmcSha256 $paramsPath)-cne$currentParamsHash-or
       (Get-KmcTextSha256 (Get-KmcPersistencePlayerPrefs))-cne$currentPrefsHash-or
       (Get-KmcPersistenceProfileDigest (Get-KmcQualificationTreeInventory -Root $snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')))-cne$snapshot.profileDigest-or
       (Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$state.restoredModsDigest-or
       (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$state.restoredSaveInventoryDigest){
        throw 'P07 external state changed after exact recovery review.'
    }
    [IO.File]::WriteAllBytes($paramsPath,$capturedParams)
    [IO.File]::SetLastWriteTimeUtc($paramsPath,[IO.File]::GetLastWriteTimeUtc($saved))
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Owlcat Games\Pathfinder Kingmaker',$true)
    try{
        if($null-eq$key){throw 'Actual Kingmaker PlayerPrefs key disappeared.'}
        foreach($change in $changes){
            $key.SetValue($change.name,[Convert]::FromBase64String($change.before),[Microsoft.Win32.RegistryValueKind]::Binary)
        }
        $key.Flush()
    }finally{if($null-ne$key){$key.Dispose()}}
    [void](Assert-KmcPersistenceProfileUnchanged $snapshot)
    if((Get-KmcPersistenceProfileDigest (Get-KmcQualificationTreeInventory -Root $snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')))-cne$snapshot.profileDigest-or
       (Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$state.restoredModsDigest-or
       (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$state.restoredSaveInventoryDigest){
        throw 'P07 Mods or saves changed during profile restoration.'
    }
    Close-KmcRuntimeLock $lock
    $lock=$null
    Write-KmcJsonCreateNewDurable -Path $receipt -Value ([ordered]@{
        schemaVersion=1;kind='exact-p07-profile-recovery';runId=$runId;status='PASS'
        snapshotSha256=$snapshotHash;restoredParamsSha256=$beforeParamsHash
        restoredPrefsSha256=$beforePrefsHash;modsDigest=$state.restoredModsDigest
        saveInventoryDigest=$state.restoredSaveInventoryDigest;gameAbsent=$true
        originalScenarioStatus='FAIL';lockReleased=$true;recoveredAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
    })
    Write-Host 'P07 PROFILE RECOVERY PASS; original parameters and PlayerPrefs restored; game absent; lock released.'
}finally{if($null-ne$lock){Abandon-KmcRuntimeLock $lock}}
