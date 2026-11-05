[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')] param()
$ErrorActionPreference='Stop'
$requestedWhatIf=[bool]$WhatIfPreference
$WhatIfPreference=$false
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$lab=Get-KmcLabRoot
$stateRoot=Join-Path $lab 'runtime-state'
$run='20261104-finalize-f1c-creator-scroll'
$token='6afaf1c419f8840d8b1f6737c5aa9e5386d93aa9e9a425ce35ce00bab7862596'
$combinedPath=Get-KmcRunTransactionStatePath $stateRoot $run
$modsPath=Get-KmcTransactionStatePath $stateRoot $run
$evidence=Join-Path $lab ('runtime-evidence\'+$run)
$receiptPath=Join-Path $evidence 'reviewed-symbol-restoration.json'
$originalPath=Join-Path $evidence 'combined-before-symbol-review.json'
function Assert-ReviewedState {
 Assert-KmcNoGameProcesses
 if((Get-KmcSha256 $combinedPath)-cne'e731c0c08bb672c05a8fa2994aa8ab3d906a872380cb2d329846ef7024caba82'-or
    (Get-KmcSha256 $modsPath)-cne'3bca2b6a00ae43b2e5bdacf32713ff27922fbcf846ad1fbaf53e6a60925b2ff9'){throw 'Original restoration records changed.'}
 $script:combined=Read-KmcRunTransactionState -StatePath $combinedPath
 $script:mods=Read-KmcJson $modsPath
 if($combined.runId-cne$run-or$combined.token-cne$token-or$combined.phase-cne'restoration-attempted'-or
    $combined.saveProtectionPassed-ne$true-or$combined.baselineImmutable-ne$true-or$combined.workingRestored-ne$true-or
    $combined.saveWriteAllowlistPassed-ne$true-or$mods.phase-cne'restored'-or$mods.token-cne$token){throw 'Exact restored boundary is absent.'}
 $originalFinding=@($mods.runtimeUnexpectedWrites)
 if($originalFinding.Count-ne1-or$originalFinding[0].path-cne'KingmakerLevelUpSelectionScrollFix/KingmakerLevelUpSelectionScrollFix.dll.44607.cache.pdb'-or
    $originalFinding[0].runtimeSha256-cne'b3b3342d7008304590d8735a8367db2b0ca4a1ab9f8a3979e9515b6bd4bb29e2'){throw 'Unexpected write is not the reviewed symbol copy.'}
 $staging=Join-Path $lab 'runtime-staging'
 $script:quarantine=Assert-KmcChildPath $mods.stagedAfter $staging 'reviewed quarantine'
 if($quarantine-cne(Join-Path $staging ($run+'\Mods-staged-after'))){throw 'Wrong quarantine path.'}
 $manifest=Get-KmcDirectoryManifest $quarantine
 if($manifest.digest-cne$mods.stagedAfterDigest){throw 'Quarantined runtime bytes changed.'}
 $staged=Read-KmcModsStagedManifestSidecar -State $mods -StagingRoot $staging -RunId $run
 $script:classified=Get-KmcModsRuntimeWriteClassification -StagedEntries $staged -RuntimeManifest $manifest -RetainedDependencies $mods.retainedDependencies
 if(@($classified.unexpected).Count-ne0){throw 'New reader still finds unexpected runtime writes.'}
 if((Get-KmcDirectoryManifest $combined.liveModsRoot).digest-cne$combined.modsDigestBefore-or
    (Get-KmcSaveMetadataInventory $combined.saveRoot).digest-cne$combined.saveInventoryDigestBefore){throw 'Live Mods/saves are not restored.'}
 $snapshot=Read-KmcQualificationSuiteSnapshot -Path $combined.qualificationSuiteSnapshotPath -StateRoot $stateRoot -ExpectedSuiteId $combined.qualificationSuiteId -ExpectedSha256 $combined.qualificationSuiteSnapshotSha256
 [void](Assert-KmcQualificationSuiteExternalState -Snapshot $snapshot -SaveRoot $combined.saveRoot -ModsRoot $combined.liveModsRoot)
}
Assert-ReviewedState
$raw=Read-KmcJson (Join-Path $stateRoot 'active-transaction.lock')
if($raw.runId-cne$run-or$raw.token-cne$token){throw 'Stale lock belongs to another transaction.'}
if((Test-Path $receiptPath)-or(Test-Path $originalPath)){throw 'Append-only review evidence already exists.'}
$WhatIfPreference=$requestedWhatIf
if(-not$PSCmdlet.ShouldProcess($run,'record verified copied-symbol review and close only this restored stale transaction')){
 Write-Host 'PASS reviewed symbol recovery WhatIf; no files, lock, saves or Mods changed.'
 return
}
$WhatIfPreference=$false
$lock=$null
try {
 $lock=Adopt-KmcStaleRuntimeLock $stateRoot
 if($lock.RunId-cne$run-or$lock.Token-cne$token){throw 'Adopted lock differs.'}
 Assert-ReviewedState
 [IO.File]::Copy($combinedPath,$originalPath,$false)
 $receipt=[ordered]@{schemaVersion=1;kind='exact-copied-loader-symbol-restoration-review';runId=$run;reviewedAtUtc=[DateTimeOffset]::UtcNow.ToString('o');originalScenarioStatus='FAIL';originalModsRestored=$false;originalCombinedSha256=Get-KmcSha256 $originalPath;originalModsStateSha256=Get-KmcSha256 $modsPath;quarantine=$quarantine;quarantineDigest=$mods.stagedAfterDigest;newReaderUnexpectedCount=@($classified.unexpected).Count;currentModsDigest=$combined.modsDigestBefore;currentSaveDigest=$combined.saveInventoryDigestBefore;source='byte-exact staged and runtime PDB plus byte-exact source and cached DLL; original records/findings retained'}
 Write-KmcJsonCreateNewDurable -Path $receiptPath -Value $receipt
 $combined.phase='restored'
 $combined.modsRestored=$true
 $combined.restorationErrors=@()
 $combined | Add-Member restoredAtUtc ([DateTimeOffset]::UtcNow.ToString('o')) -Force
 Write-KmcJsonAtomic $combinedPath $combined
 Close-KmcRuntimeLock $lock
 $lock=$null
 Write-Host 'PASS current restoration reviewed and lock closed; original native FAIL and quarantine retained.'
} finally {if($null-ne$lock){Abandon-KmcRuntimeLock $lock}}
