Set-StrictMode -Version Latest

# These are the selected routes which can touch the live profile's Areas cache.
# A new route must be reviewed here before it receives this write authority.
function Test-KmcPersistenceAreasIsolationSelection {
    param([string]$Scenario,[string]$PersistenceCase,[bool]$ObserveWorkingDeserialization)
    if($Scenario-ceq'export-mounted-contracts'){return $ObserveWorkingDeserialization}
    if($ObserveWorkingDeserialization){return $false}
    if($Scenario-ceq'persistence-p07-load'){return $true}
    if($Scenario-ceq'persistence-p07-save'){
        return $PersistenceCase-cin @('area-reload','mounted-charge-area','area-cross-entry','area-cross-exit')
    }
    return $Scenario-ceq'chunk7-owned-two-pair-campaign'-and
        $PersistenceCase-cin @('multi-area-autosave','multi-exploration-three-area-transfers-manual','owner-seed-manticore-fixture-manual','manticore-fixture-cold-manual')
}

function Get-KmcPersistenceAreasIsolationPaths {
    param($Lock,$Snapshot,[string]$BackupRoot)
    [void](Assert-KmcRuntimeLockOwner $Lock)
    if($null-eq$Snapshot-or$Lock.RunId-cne$Snapshot.runId-or$Lock.Token-cne$Snapshot.token){
        throw 'Areas isolation has no matching profile snapshot and owned transaction.'
    }
    $intake=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'environment-intake.json')
    $expected=[IO.Path]::GetFullPath((Split-Path ([string]$intake.requestedLayout.kingmakerSaveRoot))).TrimEnd('\')
    if([IO.Path]::GetFullPath([string]$Snapshot.profile).TrimEnd('\')-cne$expected){
        throw 'Areas isolation profile differs from the exact current lab intake.'
    }
    $backup=Assert-KmcChildPath (Join-Path $BackupRoot ('profile-'+$Lock.RunId)) $BackupRoot 'owned profile backup'
    if(-not(Test-Path -LiteralPath $backup -PathType Container)){throw 'Owned profile backup is missing.'}
    $snapshotPath=Join-Path $backup 'snapshot.json'
    Assert-KmcRecoveryLeafNoLinks $snapshotPath 'Areas source snapshot'
    $saved=Read-KmcPersistenceProfileSnapshot $snapshotPath
    if($saved.runId-cne$Snapshot.runId-or$saved.token-cne$Snapshot.token-or
        $saved.profile-cne$Snapshot.profile-or$saved.profileDigest-cne$Snapshot.profileDigest){
        throw 'Durable profile snapshot differs from the active Areas transaction.'
    }
    return [pscustomobject]@{
        live=Join-Path $expected 'Areas';copy=Join-Path $backup 'profile/Areas'
        original=Join-Path $backup 'Areas-original';generated=Join-Path $backup 'Areas-generated'
        state=Join-Path $backup 'Areas-isolation.json';snapshotSha256=Get-KmcSha256 $snapshotPath
    }
}

function Assert-KmcPersistenceProfileOutsideAreasUnchanged {
    param($Snapshot)
    Assert-KmcNoGameProcesses
    $current=Get-KmcQualificationTreeInventory -Root $Snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt','Areas')
    $prior=[pscustomobject]@{entries=@($Snapshot.inventory.entries|Where-Object{
        -not([string]::Equals([string]$_.path,'Areas',[StringComparison]::OrdinalIgnoreCase)-or
            ([string]$_.path).StartsWith('Areas/',[StringComparison]::OrdinalIgnoreCase))})}
    [void](Get-KmcPersistenceProfileCacheDelta $prior $current)
    [void](Get-KmcPersistenceProfileAnalyticsDelta $prior $current)
    if((Get-KmcPersistenceProfileIdentityDigest $current $prior)-cne
        (Get-KmcPersistenceProfileIdentityDigest $prior $current)){
        throw 'Profile outside the isolated Areas tree changed; Areas restoration retains custody.'
    }
}

function Enter-KmcPersistenceAreasIsolation {
    param($Lock,$Snapshot,[string]$BackupRoot)
    Assert-KmcNoGameProcesses
    $paths=Get-KmcPersistenceAreasIsolationPaths $Lock $Snapshot $BackupRoot
    foreach($path in @($paths.state,$paths.original,$paths.generated)){
        if(Test-Path -LiteralPath $path){throw 'Areas isolation destination already exists; refusing ambiguous custody.'}
    }
    foreach($path in @($paths.live,$paths.copy)){
        Assert-KmcDirectoryTreeCloneable $path 'owned Areas source'
    }
    [void](Assert-KmcPersistenceProfileUnchanged $Snapshot)
    $before=Get-KmcDirectoryManifest $paths.live
    Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $paths.copy) 'Areas intake/backup'
    $state=[ordered]@{schemaVersion=1;runId=[string]$Lock.RunId;token=[string]$Lock.Token
        profile=[string]$Snapshot.profile;profileDigest=[string]$Snapshot.profileDigest
        snapshotSha256=[string]$paths.snapshotSha256;originalDigest=[string]$before.digest
        stage='prepared';generatedDigest=$null}
    Write-KmcJsonCreateNewDurable -Path $paths.state -Value $state
    [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
    Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $paths.live) 'Areas immediately before isolation'
    [IO.Directory]::Move($paths.live,$paths.original)
    $state.stage='original-held';Write-KmcJsonAtomic $paths.state $state
    [void](Copy-KmcDirectoryTreeExact -SourceRoot $paths.copy -DestinationRoot $paths.live)
    Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $paths.live) 'disposable Areas copy'
    $state.stage='isolated-ready';Write-KmcJsonAtomic $paths.state $state
    [void](Assert-KmcPersistenceProfileUnchanged $Snapshot)
    return $paths.state
}

# Can settle an interrupted stage or an interrupted post-game restoration.
# If the game is still present, or either tree has changed under our hand,
# ownership remains in the durable state and no original bytes are overwritten.
function Restore-KmcPersistenceAreasIsolation {
    param($Lock,$Snapshot,[string]$BackupRoot)
    [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
    $paths=Get-KmcPersistenceAreasIsolationPaths $Lock $Snapshot $BackupRoot
    if(-not(Test-Path -LiteralPath $paths.state -PathType Leaf)){return $null}
    Assert-KmcRecoveryLeafNoLinks $paths.state 'Areas isolation state'
    $state=Read-KmcJson $paths.state
    if([long]$state.schemaVersion-ne1-or$state.runId-cne$Lock.RunId-or$state.token-cne$Lock.Token-or
        $state.profile-cne$Snapshot.profile-or$state.profileDigest-cne$Snapshot.profileDigest-or
        $state.snapshotSha256-cne$paths.snapshotSha256-or$state.originalDigest-cnotmatch'^[a-f0-9]{64}$'-or
        $state.stage-cnotin @('prepared','original-held','isolated-ready','quarantined','restored')){
        throw 'Areas isolation state is not the exact active run.'
    }
    $hasOriginal=Test-Path -LiteralPath $paths.original -PathType Container
    $hasLive=Test-Path -LiteralPath $paths.live -PathType Container
    $hasGenerated=Test-Path -LiteralPath $paths.generated -PathType Container
    foreach($path in @($paths.original,$paths.live,$paths.generated)){
        if(Test-Path -LiteralPath $path){Assert-KmcDirectoryTreeCloneable $path 'owned Areas settlement tree'}
    }
    if($hasGenerated-and$null-ne$state.generatedDigest-and
        (Get-KmcDirectoryManifest $paths.generated).digest-cne$state.generatedDigest){
        throw 'Quarantined Areas changed; restoration refused.'
    }
    Assert-KmcPersistenceProfileOutsideAreasUnchanged $Snapshot
    if($hasOriginal){
        $held=Get-KmcDirectoryManifest $paths.original
        if($held.digest-cne$state.originalDigest){throw 'Held original Areas changed; restoration refused.'}
        if($hasLive-and$hasGenerated){throw 'Both live and quarantined Areas exist; restoration ownership is ambiguous.'}
        if($hasLive){
            $generated=Get-KmcDirectoryManifest $paths.live
            [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
            if((Get-KmcDirectoryManifest $paths.live).digest-cne$generated.digest){
                throw 'Disposable Areas changed during quarantine preflight.'
            }
            [IO.Directory]::Move($paths.live,$paths.generated)
            $state.generatedDigest=[string]$generated.digest
            $state.stage='quarantined';Write-KmcJsonAtomic $paths.state $state
            $hasGenerated=$true
        }elseif($hasGenerated){
            $generated=Get-KmcDirectoryManifest $paths.generated
            if($null-ne$state.generatedDigest-and$state.generatedDigest-cne$generated.digest){
                throw 'Quarantined Areas changed; restoration refused.'
            }
            $state.generatedDigest=[string]$generated.digest
            $state.stage='quarantined';Write-KmcJsonAtomic $paths.state $state
        }
        if(Test-Path -LiteralPath $paths.live){throw 'Live Areas reappeared before original restoration.'}
        [void](Assert-KmcRuntimeLockOwner $Lock);Assert-KmcNoGameProcesses
        if((Get-KmcDirectoryManifest $paths.original).digest-cne$state.originalDigest){
            throw 'Held original Areas changed immediately before restoration.'
        }
        [IO.Directory]::Move($paths.original,$paths.live)
    }elseif(-not$hasLive-or
        ($hasGenerated-and$state.stage-cnotin @('quarantined','restored'))-or
        (-not$hasGenerated-and$state.stage-cne'prepared'-and$state.stage-cne'restored')){
        throw 'Areas settlement lacks an unambiguous original tree.'
    }
    if((Get-KmcDirectoryManifest $paths.live).digest-cne$state.originalDigest){
        throw 'Restored live Areas differs from the exact intake bytes.'
    }
    $state.stage='restored';Write-KmcJsonAtomic $paths.state $state
    return [pscustomobject]@{runId=$Lock.RunId;profile=$Snapshot.profile;originalDigest=$state.originalDigest
        generatedDigest=$state.generatedDigest;generatedQuarantine=$(if(Test-Path -LiteralPath $paths.generated){$paths.generated}else{$null});restored=$true}
}
