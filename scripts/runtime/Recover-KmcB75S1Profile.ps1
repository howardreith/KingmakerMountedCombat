[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param()

# Exact recovery of the failed batch75 producer. The game completed its owned
# save, but wrote one native achievement-cache name containing a dot. Keep the
# one observed cache leaf as an explicitly recorded external change; restore
# only this run's startup settings and release its otherwise-restored lock.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'PersistenceProfileProtection.ps1')

$runId='20261020-c7b-world89-b75s1-small-mounted-save'
$snapshotSha='77c22cd3dbdf8eae40b8244be83e982fa8306c7b79019aa6f817d26136f0d32f'
$paramsBefore='343997c80ec0d3d9796438beb4c68d0bdd4b3495d99d1816cd8f4b40d6df534b'
$paramsAfter='3ef54abd54572382ccc716e3577788fc08f57ff12cad540cbf583e91a5d3c84c'
$prefsBefore='ae26ad89a144794e03d8af5033c0c4b5103594ff587b2d0c194d49765641e824'
$prefsAfter='d2a57ff67cf9f34d11e35e38a6fd0ccb2a4f263f6323027bb52f5d85d674e73c'
$modsBefore='b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda'
$savesBefore='df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab'
$nativeAfter='b46bddd972c27cc84ae409110f1588f94472c76aefcf1e922e5a4150a709921c'
$resultSha='7eaf58179ee1cdf4c10b59d6ae0ec25bbb5239f9d59ffa6a88552610d6676d06'
$gameSha='213565a7eedeac09e5bdafdd26537b0dc7ebffbb8e3db1d030cd10342d1208f2'

function Convert-ExactUtf16Path([string]$Hex) {
    return -join @($Hex.Split(',') | ForEach-Object { [char][Convert]::ToInt32($_,16) })
}

$expected=@(
    [pscustomobject]@{path=(Convert-ExactUtf16Path '0061,0063,0068,0069,0065,0076,0065,006d,0065,006e,0074,0073,002e,0064,0061,0074,fffd,002e,fffd,fffd,fffd,fffd');kind='created';before=$null;after=$nativeAfter}
)

function Assert-ExactProfileDelta($Snapshot) {
    $current=Get-KmcQualificationTreeInventory -Root $Snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
    $before=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    $after=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    $allowed=[Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach($entry in $Snapshot.inventory.entries){$before.Add([string]$entry.path,$entry)}
    foreach($entry in $current.entries){$after.Add([string]$entry.path,$entry)}
    foreach($spec in $expected){$allowed.Add([string]$spec.path,$spec)}
    foreach($path in $before.Keys){
        if(-not$after.ContainsKey($path)){throw 'An intake profile entry disappeared.'}
        $prior=$before[$path];$now=$after[$path]
        if($prior.kind-cne$now.kind-or[long]$prior.length-ne[long]$now.length-or
           ($prior.sha256-cne$now.sha256-and-not$allowed.ContainsKey($path))){
            throw 'A profile entry changed outside the exact batch75 cache set.'
        }
    }
    foreach($path in $after.Keys){
        if(-not$before.ContainsKey($path)-and-not$allowed.ContainsKey($path)){
            throw 'A profile entry appeared outside the exact batch75 cache set.'
        }
    }
    foreach($spec in $expected){
        $path=[string]$spec.path;$now=$after[$path]
        if($null-eq$now-or$now.kind-cne'file'-or[long]$now.length-ne12288-or$now.sha256-cne$spec.after){
            throw 'An exact batch75 cache leaf changed.'
        }
        if($before.ContainsKey($path)){throw 'New cache leaf existed at intake.'}
        $leaf=Assert-KmcChildPath (Join-Path $Snapshot.profile $path) $Snapshot.profile 'exact batch75 native cache leaf'
        Assert-KmcRecoveryLeafNoLinks $leaf 'exact batch75 native cache leaf'
    }
    return $current
}

function Get-ExactStartupDelta($Snapshot) {
    $params=Get-KmcSha256 $Snapshot.paramsPath
    $prefs=Get-KmcPersistencePlayerPrefs
    $prefsHash=Get-KmcTextSha256 $prefs
    if($params-cnotin@($paramsBefore,$paramsAfter)){
        throw 'UMM parameters differ from the exact observed or restored state.'
    }
    $changes=@(Get-KmcPersistencePreferenceChanges -BeforeJson $Snapshot.playerPrefsJson -AfterJson $prefs -ObservedResetRunId $runId)
    $known=@{
        'unity.player_session_count_h922449978'='MjAwMQA='
        'unity.player_sessionid_h1351336811'='NzU4Mzc5MDg1NTg4NzQ3NDk2MwA='
    }
    foreach($change in $changes){
        if(-not$known.ContainsKey([string]$change.name)-or$change.after-cne$known[[string]$change.name]){
            throw 'PlayerPrefs changed outside exact batch75 native session keys.'
        }
    }
    # Either session key may already have been restored if a previous guarded
    # attempt stopped between Registry writes. The exact key/value comparison
    # above still rejects any unrelated or unobserved preference change.
    if($changes.Count-eq0-and$prefsHash-cne$prefsBefore){throw 'Restored PlayerPrefs digest differs.'}
    if($changes.Count-eq2-and$prefsHash-cne$prefsAfter){throw 'Observed PlayerPrefs digest differs.'}
    if($params-ceq$paramsAfter){
        Assert-KmcNativeUmmStartupDelta -Before ([IO.File]::ReadAllText($saved)) -After ([IO.File]::ReadAllText($Snapshot.paramsPath)) -BaseGameOnly
    }
    return [pscustomobject]@{params=$params;prefs=$prefsHash;changes=$changes}
}

$sid=[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
if($sid-cne'S-1-5-21-1108991106-2476425705-1721024090-1001'){
    throw 'Exact profile recovery requires the original Howie identity.'
}
$lab=Get-KmcLabRoot;$stateRoot=Join-Path $lab 'runtime-state';$backups=Join-Path $lab 'runtime-backups'
$evidence=Join-Path $lab ('runtime-evidence/'+$runId)
$snapshotPath=Assert-KmcChildPath (Join-Path $backups ('profile-'+$runId+'/snapshot.json')) $backups 'exact batch75 snapshot'
$saved=Assert-KmcChildPath (Join-Path $backups ('profile-'+$runId+'/Params.xml')) $backups 'exact batch75 UMM parameters'
$receipt=Join-Path $evidence 'profile-recovery-exact-b75s1.json'
if(Test-Path -LiteralPath $receipt){throw 'Exact batch75 recovery already has a receipt.'}
Assert-KmcNoGameProcesses
if(-not(Wait-KmcStableNoKingmakerProcess -ExpectedProcessId 0)){throw 'No-game interval is unstable.'}
Assert-KmcRecoveryLeafNoLinks $snapshotPath 'exact batch75 snapshot'
Assert-KmcRecoveryLeafNoLinks $saved 'exact batch75 UMM parameters'
if((Get-KmcSha256 $snapshotPath)-cne$snapshotSha-or(Get-KmcSha256 $saved)-cne$paramsBefore){
    throw 'Exact batch75 intake backups changed.'
}
$snapshot=Read-KmcPersistenceProfileSnapshot $snapshotPath
$lockRaw=Read-KmcJson (Join-Path $stateRoot 'active-transaction.lock')
$state=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath $stateRoot $runId)
$result=Read-KmcJson (Join-Path $evidence 'runtime-result.json')
$game=Read-KmcJson (Join-Path $evidence 'runtime-game-result.json')
if((Get-KmcSha256 (Join-Path $evidence 'runtime-result.json'))-cne$resultSha-or
   (Get-KmcSha256 (Join-Path $evidence 'runtime-game-result.json'))-cne$gameSha){
    throw 'Original batch75 result evidence changed.'
}
if($snapshot.runId-cne$runId-or$lockRaw.runId-cne$runId-or$lockRaw.token-cne$snapshot.token-or
   $state.runId-cne$runId-or$state.token-cne$snapshot.token-or$state.phase-cne'restored'-or
   $state.modsRestored-ne$true-or$state.saveProtectionPassed-ne$true-or
   $state.baselineImmutable-ne$true-or$state.workingRestored-ne$true-or$state.saveWriteAllowlistPassed-ne$true-or
   $state.restoredModsDigest-cne$modsBefore-or$state.restoredSaveInventoryDigest-cne$savesBefore-or
   $result.runId-cne$runId-or$result.scenario-cne'chunk7-owned-two-pair-campaign'-or$result.status-cne'FAIL'-or
   $game.runId-cne$runId-or$game.scenario-cne'chunk7-owned-two-pair-campaign'-or$game.status-cne'PASS'){
    throw 'Exact batch75 failed run lacks otherwise-restored transaction authority.'
}
if((Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$modsBefore-or
   (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$savesBefore){
    throw 'Current Mods or saves differ from exact batch75 intake.'
}
if((Get-KmcTextSha256 $snapshot.playerPrefsJson)-cne$prefsBefore-or$snapshot.paramsSha256-cne$paramsBefore){
    throw 'Exact batch75 settings snapshot changed.'
}
$modsState=Read-KmcJson (Get-KmcTransactionStatePath $stateRoot $runId)
$intake=Read-KmcJson (Join-Path $lab 'environment-intake.json')
Assert-KmcBaseGameSettingsAuthority -State $modsState -RunId $runId -Token $snapshot.token -GameRoot ([string]$intake.requestedLayout.kingmakerInstallDir)
[void](Assert-ExactProfileDelta $snapshot)
$startup=Get-ExactStartupDelta $snapshot
if(-not$PSCmdlet.ShouldProcess($runId,'restore exact startup settings; retain one pinned native cache addition; release the otherwise-restored run lock')){
    Write-Host 'B75S1 PROFILE RECOVERY WHATIF PASS; no mutation.'
    return
}
$lock=$null
try{
    $lock=Adopt-KmcStaleRuntimeLock $stateRoot
    if($lock.RunId-cne$runId-or$lock.Token-cne$snapshot.token){throw 'Adopted lock differs from exact batch75 run.'}
    [void](Assert-KmcRuntimeLockOwner $lock)
    $state=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath $stateRoot $runId) -Lock $lock
    if($state.runId-cne$runId-or$state.token-cne$snapshot.token-or$state.phase-cne'restored'-or
       $state.modsRestored-ne$true-or$state.saveProtectionPassed-ne$true-or
       $state.baselineImmutable-ne$true-or$state.workingRestored-ne$true-or$state.saveWriteAllowlistPassed-ne$true-or
       $state.restoredModsDigest-cne$modsBefore-or$state.restoredSaveInventoryDigest-cne$savesBefore){
        throw 'Adopted transaction lost exact batch75 restoration authority.'
    }
    Assert-KmcNoGameProcesses
    if((Get-KmcSha256 $snapshotPath)-cne$snapshotSha-or
       (Get-KmcSha256 (Join-Path $evidence 'runtime-result.json'))-cne$resultSha-or
       (Get-KmcSha256 (Join-Path $evidence 'runtime-game-result.json'))-cne$gameSha){
        throw 'Batch75 snapshot or result changed after lock adoption.'
    }
    [void](Assert-ExactProfileDelta $snapshot)
    $startup=Get-ExactStartupDelta $snapshot
    Assert-KmcRecoveryLeafNoLinks $saved 'exact batch75 UMM parameters'
    Assert-KmcRecoveryLeafNoLinks $snapshot.paramsPath 'current batch75 UMM parameters'
    $capturedParams=[IO.File]::ReadAllBytes($saved)
    $hasher=[Security.Cryptography.SHA256]::Create()
    try{$capturedHash=([BitConverter]::ToString($hasher.ComputeHash($capturedParams))).Replace('-','').ToLowerInvariant()}
    finally{$hasher.Dispose()}
    if($capturedHash-cne$paramsBefore){throw 'Captured UMM bytes changed after review.'}
    if((Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$modsBefore-or
       (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$savesBefore){
        throw 'Mods or saves changed before batch75 recovery.'
    }
    if($startup.params-cne$paramsBefore){
        [IO.File]::WriteAllBytes($snapshot.paramsPath,$capturedParams)
        [IO.File]::SetLastWriteTimeUtc($snapshot.paramsPath,[IO.File]::GetLastWriteTimeUtc($saved))
    }
    if($startup.changes.Count-ne0){
        $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Owlcat Games\Pathfinder Kingmaker',$true)
        try{
            if($null-eq$key){throw 'Kingmaker PlayerPrefs key disappeared.'}
            foreach($change in $startup.changes){
                $key.SetValue($change.name,[Convert]::FromBase64String($change.before),[Microsoft.Win32.RegistryValueKind]::Binary)
            }
            $key.Flush()
        }finally{if($null-ne$key){$key.Dispose()}}
    }
    [void](Assert-ExactProfileDelta $snapshot)
    $settled=Get-ExactStartupDelta $snapshot
    if($settled.params-cne$paramsBefore-or$settled.prefs-cne$prefsBefore-or
       (Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$modsBefore-or
       (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$savesBefore){
        throw 'Batch75 profile, startup settings, Mods or saves changed during recovery.'
    }
    Close-KmcRuntimeLock $lock;$lock=$null
    Write-KmcJsonCreateNewDurable -Path $receipt -Value ([ordered]@{
        runId=$runId;status='PASS';recoveredAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
        snapshotSha256=$snapshotSha;nativeScenarioStatus='FAIL';gameScenarioStatus='PASS'
        acceptedNativeCacheChanges=@($expected|ForEach-Object{[ordered]@{pathUtf16=([string]::Join(',',@([char[]]$_.path|ForEach-Object{([int][char]$_).ToString('x4')})));change=$_.kind;beforeSha256=$_.before;afterSha256=$_.after;length=12288}})
        profileBytesRestoredExactly=$false;startupSettingsRestoredExactly=$true
        modsRestoredExactly=$true;protectedSavesRestoredExactly=$true;gameAbsent=$true;lockReleased=$true
    })
    Write-Host 'B75S1 PROFILE RECOVERY PASS; one pinned native cache retained and startup settings restored.'
}finally{if($null-ne$lock){Abandon-KmcRuntimeLock $lock}}
