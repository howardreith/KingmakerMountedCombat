Set-StrictMode -Version Latest

# Read-only protection for native caches/settings outside the already guarded
# Saves/Mods trees. Never restores stale data over a possibly newer human profile.
function Get-KmcPersistencePlayerPrefs {
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Owlcat Games\Pathfinder Kingmaker',$false)
    try {
        if($null-eq$key){return 'null'}
        $items=@(foreach($name in @($key.GetValueNames()|Sort-Object)){
            $value=$key.GetValue($name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
            if($value-is[byte[]]){$value=[Convert]::ToBase64String($value)}
            [ordered]@{name=$name;kind=[string]$key.GetValueKind($name);value=$value}
        })
        return ConvertTo-Json -InputObject $items -Depth 8 -Compress
    } finally {if($null-ne$key){$key.Dispose()}}
}

function Get-KmcPersistenceProfileDigest {
    param($Inventory)
    # Current profiles may contain culturally equivalent, ordinally distinct
    # filenames. Content identity must not depend on their enumeration order.
    [string[]]$rows=@($Inventory.entries|ForEach-Object {
        '{0}|{1}|{2}|{3}' -f $_.kind,$_.path,$_.length,$_.sha256
    })
    [Array]::Sort($rows,[StringComparer]::Ordinal)
    return Get-KmcTextSha256 ($rows -join "`n")
}

function New-KmcPersistenceProfileSnapshot {
    param($Lock,[string]$SaveRoot,[string]$GameRoot,[string]$BackupRoot)
    [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
    $profile=[IO.Path]::GetFullPath((Split-Path $SaveRoot)).TrimEnd('\')
    $params=Join-Path $GameRoot 'Kingmaker_Data/Managed/UnityModManager/Params.xml'
    Assert-KmcNotReparsePoint $params 'UMM parameters';Assert-KmcNotHardLink $params 'UMM parameters'
    # The ordinary game log is expected output, retained separately as evidence.
    # Human saves have their own exact suite inventory and Working transaction.
    $before=Get-KmcQualificationTreeInventory -Root $profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
    $prefs=Get-KmcPersistencePlayerPrefs
    $paramsHash=Get-KmcSha256 $params
    $target=Assert-KmcChildPath (Join-Path $BackupRoot ('profile-'+$Lock.RunId)) $BackupRoot 'persistence profile backup'
    if(Test-Path -LiteralPath $target){throw 'Persistence profile backup already exists.'}
    [void][IO.Directory]::CreateDirectory($target)
    $copy=Join-Path $target 'profile'
    [void][IO.Directory]::CreateDirectory($copy)
    foreach($entry in @($before.entries|Where-Object kind -ceq 'directory'|Sort-Object path)){
        $dest=Assert-KmcChildPath (Join-Path $copy $entry.path) $copy 'profile backup directory'
        [void][IO.Directory]::CreateDirectory($dest)
    }
    foreach($entry in @($before.entries|Where-Object kind -ceq 'file')){
        $source=Assert-KmcChildPath (Join-Path $profile $entry.path) $profile 'profile source file'
        $dest=Assert-KmcChildPath (Join-Path $copy $entry.path) $copy 'profile backup file'
        [IO.File]::Copy($source,$dest,$false)
        [IO.File]::SetLastWriteTimeUtc($dest,[DateTime]::new([long]$entry.lastWriteTimeUtcTicks,[DateTimeKind]::Utc))
    }
    [IO.File]::Copy($params,(Join-Path $target 'Params.xml'),$false)
    $cloned=Get-KmcQualificationTreeInventory -Root $copy -Scope save-root
    if((Get-KmcPersistenceProfileDigest $cloned)-cne(Get-KmcPersistenceProfileDigest $before)){throw 'Profile backup bytes differ from intake.'}
    $record=[pscustomobject][ordered]@{
        runId=[string]$Lock.RunId;token=[string]$Lock.Token;profile=$profile
        inventory=$before;profileDigest=Get-KmcPersistenceProfileDigest $before;paramsPath=$params;paramsSha256=$paramsHash;playerPrefsJson=$prefs
    }
    Write-KmcJsonCreateNewDurable -Path (Join-Path $target 'snapshot.json') -Value $record
    [void](Assert-KmcPersistenceProfileUnchanged $record)
    return $record
}

# The installed game keeps its own achievement cache directly in the profile
# root. Bounded read-only characterisation, retained in lab analysis-cache/
# chunk5-persistence/achievement-cache-contract64.txt: 58 leaves, 57 at exactly
# 12288 bytes and one pre-existing empty leaf written 2026-09-08, suffixes of
# 0 to 11 characters containing no dot and no separator, and only two unrelated
# files in that root. The settled native size is therefore 12288.
#
# A name alone cannot exclude a deliberately crafted lookalike, so the name rule
# never admits anything by itself. Admission is a BEFORE/AFTER transition: an
# existing leaf may have its bytes rewritten but must keep its exact prior
# length, a new leaf must arrive at the settled native size, and nothing may
# shrink, grow or disappear. A pre-existing empty leaf is not permission to
# truncate a populated one. Accepted churn is returned so the caller records it
# as an expected external change rather than claiming the profile is untouched.
$script:KmcNativeAchievementCacheSettledSize = 12288

function Test-KmcNativeAchievementCacheName {
    param($Entry)
    return $Entry.kind-ceq'file'-and$Entry.path-cmatch'^achievements\.dat[^/\\.]{0,11}$'
}

# Unity's own analytics spool. The engine writes a new timestamped batch
# directory of small event files when the game exits, and deletes a batch once
# it has been dispatched, so a clean run that touched nothing of ours can leave
# the profile with entries it did not have, or without entries it did. Restoring
# the earlier bytes over either is explicitly not allowed: the spool is the
# engine's own and its newer state is not ours to revert.
#
# Like the achievement cache above, the name rule never admits anything by
# itself. It is only ever applied to entries present on exactly ONE side of a
# comparison, which is what creation and dispatch look like. An analytics entry
# present on BOTH sides stays in the identity digest, so rewriting one in place
# is still a real profile change and still fails, as is anything outside the
# exact batch path shape. Admitted differences are returned so the caller records
# their exact paths, lengths and hashes as expected external changes rather than
# claiming the profile was untouched.
function Test-KmcNativeAnalyticsArchivedEventName {
    param($Entry)
    $batch='^Unity/[0-9a-f-]{36}/Analytics/ArchivedEvents/[0-9]{1,20}\.[0-9a-f]{1,16}$'
    if($Entry.kind-ceq'directory'){return $Entry.path-cmatch$batch-and[long]$Entry.length-eq0}
    return $Entry.kind-ceq'file'-and$Entry.path-cmatch($batch.TrimEnd('$')+'/[a-z]$')
}

function Get-KmcPersistenceProfileAnalyticsDelta {
    param($Before,$After)
    $prior=@{};foreach($entry in $Before.entries){$prior[[string]$entry.path]=$entry}
    $current=@{};foreach($entry in $After.entries){$current[[string]$entry.path]=$entry}
    $changes=@()
    foreach($entry in $After.entries){
        if(-not(Test-KmcNativeAnalyticsArchivedEventName $entry)){continue}
        if($prior.ContainsKey([string]$entry.path)){continue}
        $changes+=[pscustomobject]@{path=[string]$entry.path;change='created';kind=[string]$entry.kind
            length=[long]$entry.length;afterSha256=[string]$entry.sha256}
    }
    foreach($entry in $Before.entries){
        if(-not(Test-KmcNativeAnalyticsArchivedEventName $entry)){continue}
        if($current.ContainsKey([string]$entry.path)){continue}
        $changes+=[pscustomobject]@{path=[string]$entry.path;change='dispatched';kind=[string]$entry.kind
            length=[long]$entry.length;afterSha256=[string]$entry.sha256}
    }
    return $changes
}

function Get-KmcPersistenceProfileIdentityDigest {
    param($Inventory,$Baseline)
    # With a baseline, analytics entries absent from it are left out, because
    # the engine both writes and dispatches spool batches on its own. Compare
    # two inventories by passing each as the other's baseline: then a batch that
    # exists on only one side drops out of both digests, while an analytics
    # entry present on both sides still counts and a rewrite of one still shows.
    $added=@{}
    if($null-ne$Baseline){
        $prior=@{};foreach($entry in $Baseline.entries){$prior[[string]$entry.path]=$true}
        foreach($entry in $Inventory.entries){
            if((Test-KmcNativeAnalyticsArchivedEventName $entry)-and-not$prior.ContainsKey([string]$entry.path)){
                $added[[string]$entry.path]=$true
            }
        }
    }
    return Get-KmcPersistenceProfileDigest ([pscustomobject]@{
        entries=@($Inventory.entries|Where-Object{
            -not(Test-KmcNativeAchievementCacheName $_)-and-not$added.ContainsKey([string]$_.path)})})
}

function Get-KmcPersistenceProfileCacheDelta {
    param($Before,$After)
    $priorByPath=@{};foreach($entry in $Before.entries){if(Test-KmcNativeAchievementCacheName $entry){$priorByPath[$entry.path]=$entry}}
    $currentByPath=@{};foreach($entry in $After.entries){if(Test-KmcNativeAchievementCacheName $entry){$currentByPath[$entry.path]=$entry}}
    $changes=@()
    foreach($path in @($priorByPath.Keys)){
        if(-not$currentByPath.ContainsKey($path)){
            throw ('A native achievement cache leaf disappeared during the owned persistence process: '+$path)
        }
        $prior=$priorByPath[$path];$current=$currentByPath[$path]
        if([long]$current.length-ne[long]$prior.length){
            throw ('A native achievement cache leaf changed length during the owned persistence process: '+
                $path+' '+$prior.length+' to '+$current.length)
        }
        if($current.sha256-cne$prior.sha256){
            $changes+=[pscustomobject]@{path=$path;change='rewritten';length=[long]$current.length
                beforeSha256=[string]$prior.sha256;afterSha256=[string]$current.sha256}
        }
    }
    foreach($path in @($currentByPath.Keys)){
        if($priorByPath.ContainsKey($path)){continue}
        $current=$currentByPath[$path]
        if([long]$current.length-ne[long]$script:KmcNativeAchievementCacheSettledSize){
            throw ('A new profile leaf used the achievement cache name without its settled native size: '+
                $path+' '+$current.length)
        }
        $changes+=[pscustomobject]@{path=$path;change='created';length=[long]$current.length
            beforeSha256=$null;afterSha256=[string]$current.sha256}
    }
    return $changes
}

function Assert-KmcPersistenceProfileUnchanged {
    param($Snapshot)
    Assert-KmcNoGameProcesses
    $after=Get-KmcQualificationTreeInventory -Root $Snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
    $cacheChanges=Get-KmcPersistenceProfileCacheDelta $Snapshot.inventory $after
    $analyticsChanges=Get-KmcPersistenceProfileAnalyticsDelta $Snapshot.inventory $after
    if((Get-KmcPersistenceProfileIdentityDigest $after $Snapshot.inventory)-cne
        (Get-KmcPersistenceProfileIdentityDigest $Snapshot.inventory $after)){
        throw 'Native profile/cache bytes changed during the owned persistence process; exact intake backup retained, no automatic stale overwrite performed.'
    }
    if((Get-KmcSha256 $Snapshot.paramsPath)-cne$Snapshot.paramsSha256){throw 'UMM parameters changed during the owned persistence process.'}
    if((Get-KmcPersistencePlayerPrefs)-cne$Snapshot.playerPrefsJson){throw 'Native PlayerPrefs changed during the owned persistence process.'}
    # Admitted native cache churn and appended analytics batches, for the caller
    # to record as expected external changes with their exact hashes. An empty
    # result means the profile really is byte-equal.
    return @($cacheChanges)+@($analyticsChanges)
}

# One observed owned LoadGameException reset changed this exact boolean.
# Normal runs retain the original three-key startup contract.
function Test-KmcObservedPreparationResetPreference {
    param([string]$RunId,$Before,$After)
    return $RunId-ceq'20260921-chunk5-P03-condition-preparing-save-A'-and
        $Before.name-ceq'EternalKingdom_h3591390253'-and$After.name-ceq$Before.name-and
        $Before.kind-ceq'Binary'-and$After.kind-ceq'Binary'-and
        $Before.value-ceq'RmFsc2UA'-and$After.value-ceq'VHJ1ZQA='
}

function Assert-KmcObservedPreparationTimeoutRecord {
    param([string]$RunId)
    if($RunId-cne'20260921-chunk5-P03-condition-preparing-save-A'){return}
    $path=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$RunId+'/runtime-game-result.json')
    Assert-KmcRecoveryLeafNoLinks $path 'owned preparation failure evidence'
    if((Get-KmcSha256 $path)-cne'ed6919f6ee3ec26fa2d9eea2db91c2247722ec47fab32948460a65b9fc03157c'){
        throw 'Exact attributed preparation failure evidence differs.'
    }
}

function Get-KmcPersistencePreferenceChanges {
    param([string]$BeforeJson,[string]$AfterJson,[string]$ObservedResetRunId)
    $before=ConvertFrom-Json -InputObject $BeforeJson
    $after=ConvertFrom-Json -InputObject $AfterJson
    if(@($before).Count-ne@($after).Count){throw 'PlayerPrefs key set changed; automatic restoration refused.'}
    foreach($old in $before){
        $matches=@($after|Where-Object {$_.name-ceq$old.name})
        if($matches.Count-ne1){throw 'PlayerPrefs key identity is ambiguous.'}
        $new=$matches[0]
        if(($old|ConvertTo-Json -Depth 8 -Compress)-ceq($new|ConvertTo-Json -Depth 8 -Compress)){continue}
        $ownedReset=Test-KmcObservedPreparationResetPreference $ObservedResetRunId $old $new
        if(($old.name-cnotin@('KingdomDifficulty_h4200925179','unity.player_session_count_h922449978','unity.player_sessionid_h1351336811')-and
            -not$ownedReset)-or$old.kind-cne'Binary'-or$new.kind-cne'Binary'){
            throw 'PlayerPrefs delta is outside the observed native startup changes.'
        }
        [void][Convert]::FromBase64String([string]$old.value)
        [void][Convert]::FromBase64String([string]$new.value)
        [pscustomobject]@{name=[string]$old.name;before=[string]$old.value;after=[string]$new.value}
    }
}

function Assert-KmcNativeUmmStartupDelta {
    param([string]$Before,[string]$After)
    [xml]$original=$Before;[xml]$current=$After
    # Unchanged parameters are the ordinary case now and the safest possible
    # outcome, so they pass on their own. This assertion was written when UMM
    # was still appending the SkipIntro entry on startup; that entry has since
    # become part of the settled installation, present on both sides, and
    # requiring the append made a byte-identical file fail.
    if($current.OuterXml-ceq$original.OuterXml){return}
    # The one historical startup transition is still accepted, and nothing else.
    $old=@($original.SelectNodes("//Mod[@Id='SkipIntro']"))
    $added=@($current.SelectNodes("//Mod[@Id='SkipIntro']"))
    if($old.Count-ne0-or$added.Count-ne1-or$added[0].OuterXml-cne
        '<Mod Id="SkipIntro" Enabled="true"><Hotkey><keyCode>None</keyCode><modifiers>0</modifiers></Hotkey></Mod>'){
        throw 'UMM parameter delta is outside the observed native startup append.'
    }
    [void]$added[0].ParentNode.RemoveChild($added[0])
    if($current.OuterXml-cne$original.OuterXml){throw 'UMM parameters include an unrelated change.'}
}

function Restore-KmcPersistenceStartupSettings {
    [CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
    param($Lock,$Snapshot,[string]$BackupRoot,[string]$ExpectedCurrentParamsSha256,[string]$ExpectedCurrentPrefsSha256)
    [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
    if($Lock.RunId-cne$Snapshot.runId-or$Lock.Token-cne$Snapshot.token){throw 'Profile restoration has no matching owned transaction.'}
    $intake=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'environment-intake.json')
    $expectedParams=[IO.Path]::GetFullPath((Join-Path $intake.requestedLayout.kingmakerInstallDir 'Kingmaker_Data/Managed/UnityModManager/Params.xml'))
    if([IO.Path]::GetFullPath($Snapshot.paramsPath)-cne$expectedParams-or
        [IO.Path]::GetFullPath($Snapshot.profile)-cne[IO.Path]::GetFullPath((Split-Path $intake.requestedLayout.kingmakerSaveRoot))){
        throw 'Profile restoration paths differ from exact current lab intake.'
    }
    $saved=Assert-KmcChildPath (Join-Path $BackupRoot ('profile-'+$Lock.RunId+'/Params.xml')) $BackupRoot 'captured UMM parameters'
    Assert-KmcRecoveryLeafNoLinks $saved 'captured UMM parameters'
    Assert-KmcRecoveryLeafNoLinks $expectedParams 'current UMM parameters'
    if((Get-KmcSha256 $saved)-cne$Snapshot.paramsSha256){throw 'Captured UMM parameters changed.'}
    $paramsHash=Get-KmcSha256 $expectedParams
    $prefs=Get-KmcPersistencePlayerPrefs
    if($paramsHash-cne$ExpectedCurrentParamsSha256-or(Get-KmcTextSha256 $prefs)-cne$ExpectedCurrentPrefsSha256){
        throw 'Current settings changed after the restoration review; refusing a stale overwrite.'
    }
    $profile=Get-KmcQualificationTreeInventory -Root $Snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
    $createdAnalytics=@(Get-KmcPersistenceProfileRecoveryDelta $Snapshot $profile)
    Assert-KmcObservedPreparationTimeoutRecord $Snapshot.runId
    $changes=@(Get-KmcPersistencePreferenceChanges -BeforeJson $Snapshot.playerPrefsJson -AfterJson $prefs -ObservedResetRunId $Snapshot.runId)
    $restoreParams=$paramsHash-cne$Snapshot.paramsSha256
    if($restoreParams){
        Assert-KmcNativeUmmStartupDelta -Before ([IO.File]::ReadAllText($saved)) -After ([IO.File]::ReadAllText($expectedParams))
    }
    if(-not$PSCmdlet.ShouldProcess('exact Kingmaker UMM parameters and attributed native preference deltas',
        'restore verified actual intake after the attributed process exit')){return}
    [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
    if((Get-KmcSha256 $expectedParams)-cne$paramsHash-or(Get-KmcPersistencePlayerPrefs)-cne$prefs){
        throw 'Settings changed immediately before restoration.'
    }
    if($createdAnalytics.Count-ne0){
        $directory=@($createdAnalytics|Where-Object kind -CEQ 'directory')
        if($directory.Count-ne1){throw 'Observed cache recovery lacks its exact directory.'}
        $source=Assert-KmcChildPath (Join-Path $Snapshot.profile $directory[0].path) $Snapshot.profile 'exact new owned analytics cache'
        $destination=Assert-KmcChildPath (Join-Path $BackupRoot ('profile-'+$Lock.RunId+'/created-analytics-178998321600004.7fa040cf')) $BackupRoot 'owned analytics evidence'
        Assert-KmcDirectoryTreeCloneable $source 'owned analytics cache'
        if(Test-Path -LiteralPath $destination){throw 'Analytics quarantine already exists; refusing an ambiguous move.'}
        $now=Get-KmcQualificationTreeInventory -Root $Snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
        [void]@(Get-KmcPersistenceProfileRecoveryDelta $Snapshot $now)
        # Both resolved absolute roots and the exact four contents were checked.
        # Same-volume rename keeps evidence intact; no recursive delete is used.
        [IO.Directory]::Move($source,$destination)
        $verified=Get-KmcQualificationTreeInventory -Root $Snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
        if((Get-KmcPersistenceProfileDigest $verified)-cne$Snapshot.profileDigest){throw 'Profile differs after exact cache quarantine.'}
    }
    if($restoreParams){
        [IO.File]::WriteAllBytes($expectedParams,[IO.File]::ReadAllBytes($saved))
        [IO.File]::SetLastWriteTimeUtc($expectedParams,[IO.File]::GetLastWriteTimeUtc($saved))
    }
    if($changes.Count-ne0){
        $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Owlcat Games\Pathfinder Kingmaker',$true)
        try{
            if($null-eq$key){throw 'Exact PlayerPrefs key disappeared.'}
            foreach($change in $changes){$key.SetValue($change.name,[Convert]::FromBase64String($change.before),[Microsoft.Win32.RegistryValueKind]::Binary)}
            $key.Flush()
        }finally{if($null-ne$key){$key.Dispose()}}
    }
    [void](Assert-KmcPersistenceProfileUnchanged $Snapshot)
}

# Exact one-run cache delta: native Unity analytics archived four new files on
# exit. No preexisting profile entry changed. Keep the archive as lab evidence.
function Test-KmcObservedValidationAnalyticsEntry {
    param([string]$RunId,$Entry)
    if($RunId-cne'20260921-chunk5-P06-future-A'){return $false}
    $root='Unity/2b02a6f4-4611-4ce0-b230-f9998567c3af/Analytics/ArchivedEvents/178998321600004.7fa040cf'
    if($Entry.path-ceq$root){return $Entry.kind-ceq'directory'-and$Entry.length-eq0-and$null-eq$Entry.sha256}
    $pins=@{
        c=@(1,'6b86b273ff34fce19d6b804eff5a3f5747ada4eaa22f1d49c01e52ddb7875b4b')
        e=@(1367,'865b2b8da5e0dfb27245ba38f50debd5d221a2e117d32fdaa69455b07105eead')
        g=@(1,'d4735e3a265e16eee03f59718b9b5d03019c07d8b6c51f90da3a666eec13ab35')
        s=@(366,'fcb8d4ca679b2110deb0e304797f489539370adb40e611762cf5db1967e872e0')
    }
    foreach($leaf in $pins.Keys){
        if($Entry.path-ceq($root+'/'+$leaf)){
            return $Entry.kind-ceq'file'-and$Entry.length-eq$pins[$leaf][0]-and$Entry.sha256-ceq$pins[$leaf][1]
        }
    }
    return $false
}

function Get-KmcPersistenceProfileRecoveryDelta {
    param($Snapshot,$Current)
    [void](Get-KmcPersistenceProfileCacheDelta $Snapshot.inventory $Current)
    [void](Get-KmcPersistenceProfileAnalyticsDelta $Snapshot.inventory $Current)
    if((Get-KmcPersistenceProfileIdentityDigest $Current $Snapshot.inventory)-ceq
        (Get-KmcPersistenceProfileIdentityDigest $Snapshot.inventory $Current)){return @()}
    $run=$Snapshot.runId
    if($run-cne'20260921-chunk5-P06-future-A'-or$Snapshot.token-cne'e8a5fd891fd0d5f75c750242c1f4e19ed985708e94fdfe09fc33057ab9513337'){
        throw 'Profile/cache bytes changed outside the exact owned recovery seam.'
    }
    $evidence=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$run+'/runtime-game-result.json')
    Assert-KmcRecoveryLeafNoLinks $evidence 'exact owned P06 native outcome'
    if((Get-KmcSha256 $evidence)-cne'6fb1fed240ea6dba377616b8d205f097f321a03678f055985f8ab0167ca3c215'){
        throw 'Owned P06 native outcome changed before cache recovery.'
    }
    $retained=[pscustomobject]@{entries=@($Current.entries|Where-Object{-not(Test-KmcObservedValidationAnalyticsEntry $run $_)})}
    $created=@($Current.entries|Where-Object{Test-KmcObservedValidationAnalyticsEntry $run $_})
    if($created.Count-ne5-or(Get-KmcPersistenceProfileDigest $retained)-cne$Snapshot.profileDigest-or
        @($Snapshot.inventory.entries|Where-Object{Test-KmcObservedValidationAnalyticsEntry $run $_}).Count-ne0){
        throw 'Cache recovery would change preexisting or unobserved profile entries.'
    }
    return $created
}

function Read-KmcPersistenceProfileSnapshot {
    param([string]$Path)
    # Durable snapshots are UTF-8 without a BOM; Windows PowerShell's default
    # Get-Content encoding otherwise corrupts non-ASCII native cache filenames.
    return [IO.File]::ReadAllText($Path,[Text.Encoding]::UTF8)|ConvertFrom-Json
}
