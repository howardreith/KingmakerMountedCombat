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
$beforeMods=Get-KmcDirectoryManifest $liveMods
$beforeSaves=Get-KmcSaveMetadataInventory $saveRoot
$WhatIfPreference=$requestedWhatIf
$target=if($legacyMode){$liveMods}else{"$liveMods plus the exact guarded Working-save transaction"}
if(-not$PSCmdlet.ShouldProcess($target,"recover exact KMC transaction $($raw.runId)")){
    $WhatIfPreference=$false
    if((Get-KmcDirectoryManifest $liveMods).digest-cne$beforeMods.digest-or
        (Get-KmcSaveMetadataInventory $saveRoot).digest-cne$beforeSaves.digest){
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
