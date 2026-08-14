[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param()

$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf=[bool]$WhatIfPreference
$WhatIfPreference=$false
$repoRoot=Get-KmcRepositoryRoot
$labRoot=Get-KmcLabRoot
$intake=Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json')|ConvertFrom-Json
$stateRoot=Join-Path $labRoot 'runtime-state'
$backupRoot=Join-Path $labRoot 'runtime-backups'
$stagingRoot=Join-Path $labRoot 'runtime-staging'
$liveMods=[IO.Path]::GetFullPath([string]$intake.requestedLayout.kingmakerModsRoot).TrimEnd('\')
$saveRoot=[IO.Path]::GetFullPath([string]$intake.requestedLayout.kingmakerSaveRoot).TrimEnd('\')
Assert-KmcNoGameProcesses
if(-not(Wait-KmcStableNoKingmakerProcess -ExpectedProcessId 0)){
    throw 'Recovery requires a stable interval with no Kingmaker process.'
}
$lockPath=Join-Path $stateRoot 'active-transaction.lock'
if(-not(Test-Path -LiteralPath $lockPath -PathType Leaf)){throw 'No stale KMC runtime transaction exists.'}
$raw=Read-KmcJson $lockPath
Assert-KmcExactProperties $raw @('schemaVersion','runId','token','ownerProcessId','createdAtUtc') 'stale runtime lock'
if([int]$raw.schemaVersion-ne1-or[string]$raw.runId-cnotmatch'^[A-Za-z0-9._-]{1,120}$'-or[string]$raw.token-cnotmatch'^[0-9a-f]{64}$'){
    throw 'Stale KMC runtime lock payload is invalid.'
}
$combinedStatePath=Get-KmcRunTransactionStatePath $stateRoot ([string]$raw.runId)
$legacyModsStatePath=Get-KmcTransactionStatePath $stateRoot ([string]$raw.runId)
$combined=$null
$legacyMode=$false
if(Test-Path -LiteralPath $combinedStatePath -PathType Leaf){
    $combined=Read-KmcRunTransactionState -StatePath $combinedStatePath
    if(-not [string]::Equals([string]$combined.liveModsRoot,$liveMods,[StringComparison]::OrdinalIgnoreCase)-or
        -not [string]::Equals([string]$combined.saveRoot,$saveRoot,[StringComparison]::OrdinalIgnoreCase)){
        throw 'Combined recovery state does not target the exact configured Kingmaker Mods and save roots.'
    }
}
elseif(Test-Path -LiteralPath $legacyModsStatePath -PathType Leaf){
    $legacyMode=$true
}
else{throw 'Stale lock has no matching combined or legacy durable transaction state; automatic recovery is unsafe.'}
$liveModsExistedBefore=Test-Path -LiteralPath $liveMods -PathType Container
$beforeMods=$null
if($liveModsExistedBefore){
    $beforeMods=Get-KmcDirectoryManifest $liveMods
}
elseif($legacyMode){
    throw 'Legacy recovery requires the live Mods root to exist.'
}
else{
    # A process can stop after the original tree is durably moved but before the
    # staged clone is activated. Prove that exact crash window from the owned
    # state and intact backup instead of requiring a live tree that should not
    # exist in this phase.
    $modsStatePath=Get-KmcTransactionStatePath $stateRoot ([string]$raw.runId)
    if(-not(Test-Path -LiteralPath $modsStatePath -PathType Leaf)){
        throw 'Missing live Mods root has no matching owned Mods transaction state.'
    }
    $modsState=Read-KmcJson $modsStatePath
    if([int]$modsState.schemaVersion-cnotin@(2,3)-or
        [string]$modsState.runId-cne[string]$raw.runId-or
        [string]$modsState.token-cne[string]$raw.token-or
        [string]$modsState.phase-cne'original-moved'-or
        -not[string]::Equals([IO.Path]::GetFullPath([string]$modsState.liveModsRoot).TrimEnd('\'),$liveMods,[StringComparison]::OrdinalIgnoreCase)){
        throw 'Missing live Mods root is not the exact owned original-moved recovery state.'
    }
    $recordedBackup=Assert-KmcChildPath ([string]$modsState.originalBackup) $backupRoot 'recorded transaction backup'
    if(-not(Test-Path -LiteralPath $recordedBackup -PathType Container)){
        throw 'Missing live Mods root has no intact recorded original backup.'
    }
    Assert-KmcManifestMatchesState (Get-KmcDirectoryManifest $recordedBackup) $modsState 'before'
    $recordedReady=Assert-KmcChildPath ([string]$modsState.stagedReady) $stagingRoot 'recorded staged Mods tree'
    $recordedAfter=Assert-KmcChildPath ([string]$modsState.stagedAfter) $stagingRoot 'recorded staged-after quarantine'
    if(-not(Test-Path -LiteralPath $recordedReady -PathType Container)-or(Test-Path -LiteralPath $recordedAfter)){
        throw 'Missing live Mods root does not have the exact unactivated staged-tree shape.'
    }
    Assert-KmcManifestMatchesState (Get-KmcDirectoryManifest $recordedReady) $modsState 'staged'
}
$beforeSaves=Get-KmcSaveMetadataInventory $saveRoot
$WhatIfPreference=$requestedWhatIf
$target=if($legacyMode){$liveMods}else{"$liveMods plus the exact guarded Working-save transaction"}
if(-not$PSCmdlet.ShouldProcess($target,"recover exact KMC transaction $($raw.runId)")){
    $WhatIfPreference=$false
    $modsChanged=if($liveModsExistedBefore){
        -not(Test-Path -LiteralPath $liveMods -PathType Container)-or
        (Get-KmcDirectoryManifest $liveMods).digest-cne$beforeMods.digest
    }else{Test-Path -LiteralPath $liveMods}
    if($modsChanged-or(Get-KmcSaveMetadataInventory $saveRoot).digest-cne$beforeSaves.digest){
        throw 'Recovery WhatIf purity failed.'
    }
    Write-Host 'Recovery WhatIf purity PASS; durable state was validated and no external state changed.'
    return
}
$WhatIfPreference=$false
$lock=$null
try{
    $lock=Adopt-KmcStaleRuntimeLock $stateRoot
    if($legacyMode){
        $restored=Restore-KmcModsTransaction -Lock $lock -StatePath $legacyModsStatePath -LiveModsRoot $liveMods -BackupRoot $backupRoot -StagingRoot $stagingRoot
        if((Get-KmcSaveMetadataInventory $saveRoot).digest-cne$beforeSaves.digest){
            throw 'Protected save metadata changed during legacy no-save recovery.'
        }
        Close-KmcRuntimeLock $lock
        $lock=$null
        Write-Host "PASS recovered legacy no-save transaction $($raw.runId); live Mods digest=$($restored.digest)"
        return
    }

    $restoration=Restore-KmcRuntimeTransactions -Lock $lock -CombinedStatePath $combinedStatePath -StateRoot $stateRoot -BackupRoot $backupRoot -StagingRoot $stagingRoot
    if(-not$restoration.modsRestored-or-not$restoration.saveProtectionPassed-or@($restoration.errors).Count-ne0){
        throw "Combined recovery remains incomplete: $(@($restoration.errors) -join '; ')"
    }
    Close-KmcRuntimeLock $lock
    $lock=$null
    Write-Host "PASS recovered transaction $($raw.runId); Mods=$($restoration.restoredModsDigest); saves=$($restoration.restoredSaveInventoryDigest); baseline immutable; Working restored; allowlist passed."
}
finally{
    if($null-ne$lock){Abandon-KmcRuntimeLock $lock}
}
