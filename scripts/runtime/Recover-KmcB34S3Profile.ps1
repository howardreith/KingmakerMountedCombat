[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param()

# Exact recovery for the base-only Working load that left seven newly written
# Areas files outside the save root. Preserve those files in run-owned quarantine.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'PersistenceProfileProtection.ps1')

$runId='20261019-c7b-p07-howie-base-fixture-b34s3'
$snapshotSha='37ff7aa3d9fa0a18957d7ce080369671002d518fcf003b3c7c7fb5a742419c14'
$paramsBefore='1bdebe8e9f97f5b58a3cf0de0cec48f84764e52e54b61e4df74a2cb96e6d8696'
$paramsNow='3ecde22b7fb1b499e9ca42b8a9a71c2bbf7a62640ea3d4c56899164abad05bce'
$prefsBefore='ac5553ebbc0a2a106e1c5c792357fe359fb5e7c5fbebf7385d6cf1e79062786f'
$prefsNow='f296ffb4972e8b9a1f50503c6c996f073ff7a5b5a65db1964218dc23689aaa39'
$afterPreferenceHashes=@{
    KingdomDifficulty_h4200925179='e79e418e48623569d75e2a7b09ae88ed9b77b126a445b9ff9dc6989a08efa079'
    'unity.player_session_count_h922449978'='f9f44fb80c170fcb6e04715ee5671e93cd215dc9b2ca017f9d73a3e59e7cc95d'
    'unity.player_sessionid_h1351336811'='06514a506e536c955849cfdd109d2067c231b80cb4a2c59786704a6d2816e148'
}
$modsBefore='b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda'
$savesBefore='df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab'
$expectedFiles=@(
    [pscustomobject]@{path='Areas/9d1278a2f599b2a4daab53abdfe88d2e.fog.png';length=24236;sha='346220d8a2a00d49b7c512f2e0141d0006ef89c59fffdad4b6b6c46dfe239201'},
    [pscustomobject]@{path='Areas/9d1278a2f599b2a4daab53abdfe88d2e.json';length=1637207;sha='60ca01e4b228b940486f1052b9e3da0cda6f236a5e89875d4660f3ff4711e5fa'},
    [pscustomobject]@{path='Areas/c49315fe499f0e5468af6f19242499a2.json';length=165317;sha='3de248f09f5b5df9b43eae0c84a4e4c11d480a22fad319cddc3e263e92db7ace'},
    [pscustomobject]@{path='Areas/header.png';length=478661;sha='e5e5ab8eb7432aef263c0f3dc17ba3cd25393cd4fbcf0ab45e2e8dbbc72fcc1d'},
    [pscustomobject]@{path='Areas/history';length=80431;sha='5e4e6e1fe7a2105a229d2be65810beeb53f764b9f9d5f3c6de4db0a3f21cbf20'},
    [pscustomobject]@{path='Areas/party.json';length=320028;sha='243efdc008619f53c2c5f9feb917a62dc3f07feb805e699ace25d3148db36615'},
    [pscustomobject]@{path='Areas/player.json';length=102996;sha='a890635ebcea08c26de30a288e7b4e2332df047be838903ab93793ec8f1e588c'}
)

function Assert-ExactQuarantineShape([string]$Quarantine){
    $profileBackup=Split-Path -Parent $Quarantine
    Assert-KmcNotReparsePoint $profileBackup 'exact profile backup parent'
    if(-not(Test-Path -LiteralPath $Quarantine)){return}
    if(-not(Test-Path -LiteralPath $Quarantine -PathType Container)){throw 'Areas quarantine is not a directory.'}
    Assert-KmcNotReparsePoint $Quarantine 'exact Areas quarantine'
    $children=@(Get-ChildItem -LiteralPath $Quarantine -Force)
    if($children.Count-gt1-or($children.Count-eq1-and($children[0].Name-cne'Areas'-or-not$children[0].PSIsContainer))){
        throw 'Areas quarantine contains an unexpected entry.'
    }
    if($children.Count-eq0){return}
    $areas=Join-Path $Quarantine 'Areas'
    Assert-KmcNotReparsePoint $areas 'exact Areas quarantine child'
    $allowed=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($spec in $expectedFiles){[void]$allowed.Add((Split-Path $spec.path -Leaf))}
    foreach($item in @(Get-ChildItem -LiteralPath $areas -Force)){
        if($item.PSIsContainer-or-not$allowed.Contains($item.Name)){throw 'Areas quarantine contains an unexpected leaf.'}
        Assert-KmcRecoveryLeafNoLinks $item.FullName 'exact Areas quarantine leaf'
    }
}

function Get-ExactStartupState($Snapshot,[string]$Saved,[string]$Params,[bool]$AllAreasHeld){
    Assert-KmcRecoveryLeafNoLinks $Saved 'exact UMM snapshot'
    Assert-KmcRecoveryLeafNoLinks $Params 'current UMM parameters'
    $paramsHash=Get-KmcSha256 $Params
    if($paramsHash-cnotin@($paramsBefore,$paramsNow)){throw 'UMM parameters are neither observed nor restored bytes.'}
    $prefs=Get-KmcPersistencePlayerPrefs
    $prefsHash=Get-KmcTextSha256 $prefs
    $changes=@(Get-KmcPersistencePreferenceChanges -BeforeJson $Snapshot.playerPrefsJson -AfterJson $prefs -ObservedResetRunId $runId)
    $names=@($changes|ForEach-Object{$_.name}|Sort-Object)
    foreach($change in $changes){
        if(-not$afterPreferenceHashes.ContainsKey($change.name)){throw 'PlayerPrefs changed outside three pinned keys.'}
        $hasher=[Security.Cryptography.SHA256]::Create()
        try{$hash=([BitConverter]::ToString($hasher.ComputeHash([Convert]::FromBase64String($change.after)))).Replace('-','').ToLowerInvariant()}
        finally{$hasher.Dispose()}
        if($hash-cne$afterPreferenceHashes[$change.name]){throw 'Observed PlayerPrefs value changed.'}
    }
    if($changes.Count-eq3){
        if(($names-join'|')-cne'KingdomDifficulty_h4200925179|unity.player_session_count_h922449978|unity.player_sessionid_h1351336811'-or$prefsHash-cne$prefsNow){throw 'Initial three-key PlayerPrefs state differs.'}
    }elseif($changes.Count-eq0){
        if($prefsHash-cne$prefsBefore){throw 'Restored PlayerPrefs state differs.'}
    }elseif(-not$AllAreasHeld){throw 'Partly restored PlayerPrefs require all seven Areas files in quarantine.'}
    if(-not$AllAreasHeld-and($paramsHash-cne$paramsNow-or$prefsHash-cne$prefsNow)){
        throw 'Settings changed before Areas quarantine completed.'
    }
    if($paramsHash-cne$paramsBefore){
        Assert-KmcNativeUmmStartupDelta -Before ([IO.File]::ReadAllText($Saved)) -After ([IO.File]::ReadAllText($Params)) -BaseGameOnly
    }
    return [pscustomobject]@{paramsHash=$paramsHash;prefsHash=$prefsHash;changes=$changes}
}

function Assert-ExactAreasDelta($Snapshot,[string]$Quarantine){
    Assert-ExactQuarantineShape $Quarantine
    $now=Get-KmcQualificationTreeInventory -Root $Snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
    $before=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($entry in $Snapshot.inventory.entries){if($before.ContainsKey($entry.path)){throw 'Snapshot repeats a profile path.'};$before[$entry.path]=$entry}
    $current=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($entry in $now.entries){if($current.ContainsKey($entry.path)){throw 'Current profile repeats a path.'};$current[$entry.path]=$entry}
    foreach($path in $before.Keys){
        if(-not$current.ContainsKey($path)){throw "Preexisting profile path disappeared: $path"}
        $a=$before[$path];$b=$current[$path]
        if($a.kind-cne$b.kind-or$a.length-ne$b.length-or$a.sha256-cne$b.sha256){throw "Preexisting profile path changed: $path"}
    }
    $expected=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($spec in $expectedFiles){$expected[$spec.path]=$spec}
    foreach($path in $current.Keys){if(-not$before.ContainsKey($path)-and-not$expected.ContainsKey($path)){throw "Unexpected profile addition: $path"}}
    foreach($spec in $expectedFiles){
        $relative=$spec.path.Replace('/','\')
        $live=Assert-KmcChildPath (Join-Path $Snapshot.profile $relative) $Snapshot.profile 'exact newly written Areas file'
        $held=Assert-KmcChildPath (Join-Path $Quarantine $relative) $Quarantine 'exact quarantined Areas file'
        $liveExists=Test-Path -LiteralPath $live -PathType Leaf
        $heldExists=Test-Path -LiteralPath $held -PathType Leaf
        if($liveExists-eq$heldExists){throw "Areas file is duplicated or missing: $relative"}
        $path=if($liveExists){$live}else{$held}
        Assert-KmcRecoveryLeafNoLinks $path 'exact Areas file'
        $item=Get-Item -LiteralPath $path
        if($item.Length-ne$spec.length-or(Get-KmcSha256 $path)-cne$spec.sha){throw "Areas file differs from observed bytes: $relative"}
        if($liveExists){
            if(-not$current.ContainsKey($spec.path)-or$current[$spec.path].sha256-cne$spec.sha){throw "Live Areas inventory differs: $relative"}
        }elseif($current.ContainsKey($spec.path)){throw "Quarantined Areas file still appears live: $relative"}
    }
    return $now
}

$sid=[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
if($sid-cne'S-1-5-21-1108991106-2476425705-1721024090-1001'){throw 'Recovery requires the actual Howie profile and HKCU identity.'}
$lab=Get-KmcLabRoot;$stateRoot=Join-Path $lab 'runtime-state';$backupRoot=Join-Path $lab 'runtime-backups'
$evidenceRoot=Join-Path $lab ('runtime-evidence/'+$runId)
$snapshotPath=Assert-KmcChildPath (Join-Path $backupRoot ('profile-'+$runId+'/snapshot.json')) $backupRoot 'exact run profile snapshot'
$quarantine=Assert-KmcChildPath (Join-Path $backupRoot ('profile-'+$runId+'/Areas-after')) $backupRoot 'exact Areas quarantine'
$receipt=Join-Path $evidenceRoot 'profile-recovery-exact-b34s3.json'
if(Test-Path -LiteralPath $receipt){throw 'Exact recovery already has a receipt.'}
Assert-KmcNoGameProcesses
if(-not(Wait-KmcStableNoKingmakerProcess -ExpectedProcessId 0)){throw 'No-game interval is unstable.'}
Assert-KmcRecoveryLeafNoLinks $snapshotPath 'exact profile snapshot'
if((Get-KmcSha256 $snapshotPath)-cne$snapshotSha){throw 'Profile snapshot differs from this run.'}
$snapshot=Read-KmcPersistenceProfileSnapshot $snapshotPath
$lockRaw=Read-KmcJson (Join-Path $stateRoot 'active-transaction.lock')
$state=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath $stateRoot $runId)
$result=Read-KmcJson (Join-Path $evidenceRoot 'runtime-result.json')
$orchestration=Read-KmcJson (Join-Path $evidenceRoot 'orchestration.json')
if($lockRaw.runId-cne$runId-or$lockRaw.token-cne$snapshot.token-or$state.token-cne$snapshot.token-or
   $state.phase-cne'restored'-or$state.modsRestored-ne$true-or$state.saveProtectionPassed-ne$true-or
   $state.baselineImmutable-ne$true-or$state.workingRestored-ne$true-or$state.saveWriteAllowlistPassed-ne$true-or
   $state.restoredModsDigest-cne$modsBefore-or$state.restoredSaveInventoryDigest-cne$savesBefore-or
   $result.status-cne'FAIL'-or$orchestration.stage-cne'restoration-blocked'){
    throw 'Exact failed run has no otherwise-restored transaction.'
}
$intake=Read-KmcJson (Join-Path $lab 'environment-intake.json')
$params=[IO.Path]::GetFullPath((Join-Path $intake.requestedLayout.kingmakerInstallDir 'Kingmaker_Data/Managed/UnityModManager/Params.xml'))
$saved=Assert-KmcChildPath (Join-Path $backupRoot ('profile-'+$runId+'/Params.xml')) $backupRoot 'exact UMM snapshot'
if([IO.Path]::GetFullPath($snapshot.paramsPath)-cne$params-or
   [IO.Path]::GetFullPath($snapshot.profile)-cne[IO.Path]::GetFullPath((Split-Path $intake.requestedLayout.kingmakerSaveRoot))){throw 'Profile paths differ from the actual run.'}
Assert-KmcRecoveryLeafNoLinks $saved 'exact UMM snapshot'
Assert-KmcRecoveryLeafNoLinks $params 'current UMM parameters'
if($snapshot.paramsSha256-cne$paramsBefore-or(Get-KmcSha256 $saved)-cne$paramsBefore-or
   (Get-KmcTextSha256 $snapshot.playerPrefsJson)-cne$prefsBefore-or
   (Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$modsBefore-or
   (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$savesBefore){throw 'Actual settings, Mods or saves differ from exact pins.'}
$modsState=Read-KmcJson (Get-KmcTransactionStatePath $stateRoot $runId)
Assert-KmcBaseGameSettingsAuthority -State $modsState -RunId $runId -Token $snapshot.token -GameRoot ([string]$intake.requestedLayout.kingmakerInstallDir)
[void](Assert-ExactAreasDelta $snapshot $quarantine)
$allHeld=@($expectedFiles|Where-Object {-not(Test-Path -LiteralPath (Join-Path $snapshot.profile ($_.path.Replace('/','\'))) -PathType Leaf)}).Count-eq7
$startup=Get-ExactStartupState $snapshot $saved $params $allHeld
if(-not$PSCmdlet.ShouldProcess($runId,'quarantine seven exact native Areas writes, restore this run profile/settings, release lock')){
    Write-Host 'B34S3 PROFILE RECOVERY WHATIF PASS; seven pinned Areas files and three startup preferences; no mutation.'
    return
}
$lock=$null
try{
    $lock=Adopt-KmcStaleRuntimeLock $stateRoot
    if($lock.RunId-cne$runId-or$lock.Token-cne$snapshot.token){throw 'Adopted lock differs from exact failed run.'}
    [void](Assert-KmcRuntimeLockOwner $lock)
    $state=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath $stateRoot $runId) -Lock $lock
    if($state.phase-cne'restored'-or$state.modsRestored-ne$true-or$state.saveProtectionPassed-ne$true-or
       $state.baselineImmutable-ne$true-or$state.workingRestored-ne$true-or$state.saveWriteAllowlistPassed-ne$true-or
       $state.restoredModsDigest-cne$modsBefore-or$state.restoredSaveInventoryDigest-cne$savesBefore){
        throw 'Adopted transaction lost its restored authority.'
    }
    Assert-KmcNoGameProcesses
    [void](Assert-ExactAreasDelta $snapshot $quarantine)
    $allHeld=@($expectedFiles|Where-Object {-not(Test-Path -LiteralPath (Join-Path $snapshot.profile ($_.path.Replace('/','\'))) -PathType Leaf)}).Count-eq7
    $startup=Get-ExactStartupState $snapshot $saved $params $allHeld
    if((Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$modsBefore-or
       (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$savesBefore){throw 'Mods or saves changed after recovery review.'}
    $capturedParams=[IO.File]::ReadAllBytes($saved)
    $hasher=[Security.Cryptography.SHA256]::Create()
    try{$capturedHash=([BitConverter]::ToString($hasher.ComputeHash($capturedParams))).Replace('-','').ToLowerInvariant()}
    finally{$hasher.Dispose()}
    if($capturedHash-cne$paramsBefore){throw 'Captured UMM bytes changed after review.'}
    if(-not(Test-Path -LiteralPath $quarantine -PathType Container)){[void][IO.Directory]::CreateDirectory($quarantine)}
    $areasAfter=Join-Path $quarantine 'Areas'
    if(-not(Test-Path -LiteralPath $areasAfter -PathType Container)){[void][IO.Directory]::CreateDirectory($areasAfter)}
    Assert-ExactQuarantineShape $quarantine
    foreach($spec in $expectedFiles){
        $relative=$spec.path.Replace('/','\')
        $live=Assert-KmcChildPath (Join-Path $snapshot.profile $relative) $snapshot.profile 'exact live Areas file'
        $held=Assert-KmcChildPath (Join-Path $quarantine $relative) $quarantine 'exact Areas quarantine file'
        if(Test-Path -LiteralPath $live -PathType Leaf){
            if(Test-Path -LiteralPath $held){throw 'Exact Areas quarantine target already exists.'}
            [IO.File]::Move($live,$held)
        }
    }
    [void](Assert-ExactAreasDelta $snapshot $quarantine)
    $clean=Get-KmcQualificationTreeInventory -Root $snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
    if((Get-KmcPersistenceProfileDigest $clean)-cne$snapshot.profileDigest){throw 'Profile differs after exact Areas quarantine.'}
    $startup=Get-ExactStartupState $snapshot $saved $params $true
    if($startup.paramsHash-cne$paramsBefore){
        [IO.File]::WriteAllBytes($params,$capturedParams)
        [IO.File]::SetLastWriteTimeUtc($params,[IO.File]::GetLastWriteTimeUtc($saved))
    }
    if($startup.changes.Count-ne0){
        $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Owlcat Games\Pathfinder Kingmaker',$true)
        try{
            if($null-eq$key){throw 'Actual Kingmaker PlayerPrefs key disappeared.'}
            foreach($change in $startup.changes){
                $key.SetValue($change.name,[Convert]::FromBase64String($change.before),[Microsoft.Win32.RegistryValueKind]::Binary)
            }
            $key.Flush()
        }finally{if($null-ne$key){$key.Dispose()}}
    }
    [void](Assert-KmcPersistenceProfileUnchanged $snapshot)
    if((Get-KmcPersistenceProfileDigest (Get-KmcQualificationTreeInventory -Root $snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')))-cne$snapshot.profileDigest-or
       (Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$modsBefore-or
       (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$savesBefore){throw 'Profile, Mods or saves changed during exact recovery.'}
    Close-KmcRuntimeLock $lock;$lock=$null
    Write-KmcJsonCreateNewDurable -Path $receipt -Value ([ordered]@{
        schemaVersion=1;kind='exact-b34s3-profile-recovery';runId=$runId;status='PASS'
        snapshotSha256=$snapshotSha;restoredParamsSha256=$paramsBefore;restoredPrefsSha256=$prefsBefore
        quarantine=$quarantine;quarantinedFileCount=7;modsDigest=$modsBefore;saveInventoryDigest=$savesBefore
        originalScenarioStatus='FAIL';gameAbsent=$true;lockReleased=$true;recoveredAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
    })
    Write-Host 'B34S3 PROFILE RECOVERY PASS; exact Areas writes quarantined, settings restored, lock released.'
}finally{if($null-ne$lock){Abandon-KmcRuntimeLock $lock}}
