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
