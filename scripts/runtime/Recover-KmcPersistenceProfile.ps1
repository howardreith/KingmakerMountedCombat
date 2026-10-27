[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$SnapshotSha256,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$CurrentParamsSha256,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$CurrentPrefsSha256,
    # b210 c3: after a native crash the transaction is recovered first (Recover-KingmakerRuntimeTransaction.ps1 closes the lock);
    # this mode then restores the startup settings under a fresh profile-recovery lock, bound to the completed transaction.
    [switch]$AfterTransactionRecovery
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# -WhatIf is a dry run of this whole script. Its read-only checks must not inherit the preference (final B1 c4: it reached
# Get-FileHash's path resolution and the dry run could not run), so it is taken here and every mutation stays behind the gate below.
$dryRun=[bool]$WhatIfPreference;$WhatIfPreference=$false
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'PersistenceProfileProtection.ps1')
$lab=Get-KmcLabRoot;$stateRoot=Join-Path $lab 'runtime-state';$backups=Join-Path $lab 'runtime-backups'
Assert-KmcNoGameProcesses
if(-not(Wait-KmcStableNoKingmakerProcess -ExpectedProcessId 0)){throw 'Game process absence is not stable.'}
$snapshotPath=Assert-KmcChildPath (Join-Path $backups ('profile-'+$RunId+'/snapshot.json')) $backups 'profile snapshot'
Assert-KmcRecoveryLeafNoLinks $snapshotPath 'profile snapshot'
if((Get-KmcSha256 $snapshotPath)-cne$SnapshotSha256){throw 'Profile snapshot pin differs.'}
$snapshot=Read-KmcPersistenceProfileSnapshot $snapshotPath
# An interrupted post-transaction recovery leaves only its own stale profile-recovery lock; it is adopted and re-verified.
$staleProfileLock=$AfterTransactionRecovery-and(Test-Path -LiteralPath (Join-Path $stateRoot 'active-transaction.lock') -PathType Leaf)
if($AfterTransactionRecovery){
    $state=Assert-KmcCompletedTransactionRecovery -StateRoot $stateRoot -EvidenceRoot (Join-Path $lab 'runtime-evidence') -RunId $RunId -Snapshot $snapshot -ProfileRecoveryLock:$staleProfileLock
    $runToken=[string]$state.token
}else{
    $raw=Read-KmcJson (Join-Path $stateRoot 'active-transaction.lock')
    $state=Read-KmcRunTransactionState -StatePath (Get-KmcRunTransactionStatePath $stateRoot $RunId)
    if($raw.runId-cne$RunId-or$snapshot.runId-cne$RunId-or$raw.token-cne$snapshot.token-or
        $state.token-cne$raw.token-or$state.phase-cne'restored'){throw 'Profile recovery has no exact otherwise-restored transaction.'}
    $runToken=[string]$raw.token
}
if((Get-KmcDirectoryManifest $state.liveModsRoot).digest-cne$state.restoredModsDigest-or
    (Get-KmcSaveMetadataInventory $state.saveRoot).digest-cne$state.restoredSaveInventoryDigest){
    throw 'Current Mods or saves differ from the completed transaction; refusing stale recovery.'
}
$prefs=Get-KmcPersistencePlayerPrefs
if((Get-KmcSha256 $snapshot.paramsPath)-cne$CurrentParamsSha256-or(Get-KmcTextSha256 $prefs)-cne$CurrentPrefsSha256){
    throw 'Current settings differ from the exact reviewed recovery pins.'
}
$currentProfile=Get-KmcQualificationTreeInventory -Root $snapshot.profile -Scope save-root -ExcludeRelativeRoots @('Saved Games','output_log.txt')
# A new zero-length native achievement-cache leaf is admitted only by the settled-empty rule (PersistenceProfileProtection.ps1).
[void]@(Get-KmcPersistenceProfileRecoveryDelta $snapshot $currentProfile -EmptyNewLeaf Admit)
$admittedBefore=@(Get-KmcPersistenceProfileCacheDelta $snapshot.inventory $currentProfile -EmptyNewLeaf Admit|Where-Object change -CEQ 'created-empty')
Assert-KmcObservedPreparationTimeoutRecord $RunId
[void]@(Get-KmcPersistencePreferenceChanges -BeforeJson $snapshot.playerPrefsJson -AfterJson $prefs -ObservedResetRunId $RunId)
$original=Join-Path $backups ('profile-'+$RunId+'/Params.xml')
# A removal-observer run (the genuine no-DLL load) is named by its own request.
$requestPath=Join-Path $lab ('runtime-evidence/'+$RunId+'/runtime-request.json')
$removalObserver=(Test-Path -LiteralPath $requestPath -PathType Leaf)-and[string](Read-KmcJson $requestPath).scenario-ceq'persistence-p07-load'-and[string](Read-KmcJson $requestPath).persistenceCase-ceq'removal-no-dll'
$baseGameOnly=$false;$withObserver=$false
$modsStatePath=Get-KmcTransactionStatePath $stateRoot $RunId
if(Test-Path -LiteralPath $modsStatePath -PathType Leaf){
    $modsState=Read-KmcJson $modsStatePath
    if([int]$modsState.schemaVersion-eq4){
        $intake=Read-KmcJson (Join-Path $lab 'environment-intake.json')
        Assert-KmcBaseGameSettingsAuthority -State $modsState -RunId $RunId -Token $runToken -GameRoot ([string]$intake.requestedLayout.kingmakerInstallDir)
        $baseGameOnly=$true;$withObserver=[string]$modsState.stagingMode-ceq'base-game-only-kmc-observer'
    }
}
if($removalObserver){Assert-KmcNativeUmmStartupDelta -Before ([IO.File]::ReadAllText($original)) -After ([IO.File]::ReadAllText($snapshot.paramsPath)) -RemovalObserver -StagedModIds (Get-KmcRunStagedModIds $RunId)}
else{Assert-KmcNativeUmmStartupDelta -Before ([IO.File]::ReadAllText($original)) -After ([IO.File]::ReadAllText($snapshot.paramsPath)) -BaseGameOnly:$baseGameOnly -WithObserver:$withObserver}
if($dryRun-or-not$PSCmdlet.ShouldProcess($RunId,'restore exact observed startup settings drift and release otherwise-restored runtime lock')){
    Write-Host 'PROFILE RECOVERY WHATIF PASS; no file, registry or lock mutation.'
    return
}
$lock=$null
try{
    if($AfterTransactionRecovery){
        if($staleProfileLock){$lock=Adopt-KmcStaleRuntimeLock $stateRoot -ExpectedPurpose 'profile-recovery'}
        else{$lock=Open-KmcRuntimeLock -StateRoot $stateRoot -RunId $RunId -Purpose 'profile-recovery'}
        if($lock.RunId-cne$RunId){throw 'The adopted profile-recovery lock belongs to another run.'}
        # Every admission check again, now under the held lock.
        $held=Assert-KmcCompletedTransactionRecovery -StateRoot $stateRoot -EvidenceRoot (Join-Path $lab 'runtime-evidence') -RunId $RunId -Snapshot $snapshot -HeldLock $lock
        if([string]$held.token-cne$runToken-or(Get-KmcDirectoryManifest $held.liveModsRoot).digest-cne$held.restoredModsDigest-or
            (Get-KmcSaveMetadataInventory $held.saveRoot).digest-cne$held.restoredSaveInventoryDigest){
            throw 'The completed transaction or its Mods/saves changed before profile restoration.'
        }
    }
    else{$lock=Adopt-KmcStaleRuntimeLock $stateRoot}
    Restore-KmcPersistenceStartupSettings -Lock $lock -RecoveredTransactionToken $(if($AfterTransactionRecovery){$runToken}else{''}) -Snapshot $snapshot -BackupRoot $backups -ExpectedCurrentParamsSha256 $CurrentParamsSha256 -ExpectedCurrentPrefsSha256 $CurrentPrefsSha256 -RemovalObserver:$removalObserver -BaseGameOnly:$baseGameOnly -Confirm:$false
    # Restore defers a zero-length native cache leaf; the settled-empty rule is the authority again after the mutation, and
    # the admitted leaf is recorded, so the PASS below never rests on a deferred check.
    $accepted=@(Assert-KmcPersistenceProfileUnchanged $snapshot -EmptyNewLeaf Admit)
    $acceptedEmpty=@($accepted|Where-Object change -CEQ 'created-empty')
    if((ConvertTo-Json -InputObject @($acceptedEmpty|ForEach-Object path) -Compress)-cne(ConvertTo-Json -InputObject @($admittedBefore|ForEach-Object path) -Compress)){throw 'The admitted empty native cache leaf changed during recovery.'}
    Close-KmcRuntimeLock $lock;$lock=$null
    Write-KmcJsonCreateNewDurable -Path (Join-Path $lab ('runtime-evidence/'+$RunId+'/profile-recovery.json')) -Value ([ordered]@{
        runId=$RunId;status='PASS';mode=$(if($AfterTransactionRecovery){'after-transaction-recovery'}else{'stale-lock'});recoveredAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
        snapshotSha256=$SnapshotSha256;paramsSha256=$snapshot.paramsSha256
        prefsSha256=Get-KmcTextSha256 $snapshot.playerPrefsJson;nativeScenarioStatus='FAIL'
        protectedSavesUnchanged=$true;foreignModsUnchanged=$true;gameAbsent=$true;lockReleased=$true
        acceptedNativeCacheChanges=@($accepted|ForEach-Object{[ordered]@{path=[string]$_.path;change=[string]$_.change;length=[long]$_.length;beforeSha256=[string]$_.beforeSha256;afterSha256=[string]$_.afterSha256}})
    })
    Write-Host 'PROFILE RECOVERY PASS; exact actual intake restored, game absent, lock released.'
}finally{if($null-ne$lock){Abandon-KmcRuntimeLock $lock}}
