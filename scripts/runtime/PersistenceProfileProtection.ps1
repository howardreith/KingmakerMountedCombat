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

function Assert-KmcPersistenceProfileUnchanged {
    param($Snapshot)
    Assert-KmcNoGameProcesses
    $after=Get-KmcQualificationTreeInventory -Root $Snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
    if((Get-KmcPersistenceProfileDigest $after)-cne$Snapshot.profileDigest){
        throw 'Native profile/cache bytes changed during the owned persistence process; exact intake backup retained, no automatic stale overwrite performed.'
    }
    if((Get-KmcSha256 $Snapshot.paramsPath)-cne$Snapshot.paramsSha256){throw 'UMM parameters changed during the owned persistence process.'}
    if((Get-KmcPersistencePlayerPrefs)-cne$Snapshot.playerPrefsJson){throw 'Native PlayerPrefs changed during the owned persistence process.'}
    return $true
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
    if((Get-KmcPersistenceProfileDigest $profile)-cne$Snapshot.profileDigest){throw 'Profile/cache bytes changed outside the startup settings seam.'}
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
