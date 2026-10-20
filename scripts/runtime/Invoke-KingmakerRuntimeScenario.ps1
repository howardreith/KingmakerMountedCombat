[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [ValidateSet(
        'chunk7-stock-mammoth-fixture-preparation','chunk7-small-fixture-preparation','chunk7-assisted-cold-inventory','chunk7-assisted-fixture-preparation','chunk7-owned-two-pair-campaign','chunk7-cumulative-acquisition','chunk7-acquired-manual-derivative','chunk7-manual-derived-acquisition','chunk7-bootstrap-manual-seed','chunk7-bootstrap-cold-inventory','chunk7-native-main-bootstrap','chunk7-native-progression','chunk7-small-profile-calibration','chunk7-ordinary-field-admission','chunk7-ordinary-acquisition','chunk7-two-pair-exploration','chunk7-persistence-save','chunk7-persistence-load','chunk7-two-pair-combat-rider-first','chunk7-two-pair-combat-mount-first','chunk7-two-pair-realtime-core','chunk7-two-pair-realtime-casting','chunk7-two-pair-realtime-charge','chunk7-two-pair-native-life-cleanup',
        'mod-load-smoke','export-mounted-contracts','export-candidate-mount-rigs','observe-mount-diagnostic-availability','horse-native-asset-audit','horse-companion-blueprint-registration','horse-companion-unmounted-suite','horse-mounted-alpha-suite','horse-native-controls-ux-suite',
        'chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb',
        'chunk6d-staged-rt','chunk6d-staged-tb','chunk6e-reaction-rt','chunk6e-reaction-tb',
        'chunk6b-charge-rt','chunk6b-charge-core-rt','chunk6b-charge-interruption-rt','chunk6b-charge-lifecycle-rt', 'chunk6b-charge-tb', 'chunk6b-charge-path-rt', 'chunk6b-charge-path-tb', 'chunk6a-combat-mount-rt', 'chunk6a-combat-mount-tb', 'chunk6a-mount-preamble', 'chunk6a-mammoth-mount-rt','chunk6a-mammoth-mount-tb','chunk6a-paused-queue','chunk6a-refused-policy-disabled','chunk6a-refused-foreign-companion','chunk6a-refused-wrong-creature-target','chunk6a-refused-mount-selected','chunk6a-refused-multiple-selection','chunk6a-refused-foreign-selection','chunk6a-mount-approach','chunk6a-geometry-change','chunk6a-obstruction','chunk6a-stop-approach','chunk6a-combat-end-approach','chunk6a-disable-approach','chunk6a-command-replacement','chunk6a-repeated-mount-request','chunk6a-ownership-change','chunk6a-size-form-change','chunk6a-lost-direct-control','chunk6a-rider-incapacitated','chunk6a-mount-incapacitated','chunk6a-hotbar-approach','chunk6a-allocation-rider-first-tb','chunk6a-allocation-mount-first-tb','chunk6a-mount-spent-move-tb','chunk6a-mount-spent-standard-tb','chunk6a-mount-spent-all-tb','chunk6a-rider-without-move-tb','chunk6a-rider-other-action-tb','chunk6a-unrelated-candidate-between-tb','chunk6a-dismount-after-rider-expenditure-tb','chunk6a-dismount-after-mount-expenditure-tb','chunk6a-dismount-immediately-after-mount-tb','chunk6a-dismount-feature-disabled-rt','chunk6a-dismount-policy-disabled-rt','chunk6a-auto-use-mount-rt','chunk6a-auto-use-dismount-rt', 'chunk6a-adoption-compensation-rt', 'chunk6a-adoption-compensation-tb', 'chunk6a-left-area', 'chunk6a-view-agent-lost', 'chunk6a-loading-cutscene', 'chunk6a-generation-change', 'chunk6a-injected-exception', 'chunk6a-rider-death-approach', 'chunk6a-mount-death-approach', 'chunk6a-turn-end-approach-tb', 'chunk6a-mode-exit-tb', 'chunk4-rider-incapacitation-tb', 'chunk4-rider-death-tb', 'chunk4-mount-death-tb', 'chunk4-targeting-rider-rt', 'chunk4-targeting-mount-rt', 'chunk4-ground-arrival-rt', 'chunk4-horse-strike-comparison-rt', 'chunk4-targeting-area-unmounted-rt', 'chunk4-obstruction-ranged-rt', 'chunk4-ranged-native-control-rt', 'chunk4-interrupt-melee-rt', 'chunk4-interrupt-ranged-rt', 'chunk4-inspection-rt', 'chunk4-session-rt', 'chunk4-session-tb', 'chunk4-sustained-melee-rt', 'chunk4-sustained-ranged-rt', 'chunk4-sustained-tb', 'chunk4-charge-safety-rt', 'chunk4-charge-safety-tb', 'actor-allocation-rider-first-tb', 'actor-allocation-mount-first-tb', 'actor-allocation-rider-first-unmounted-tb', 'actor-allocation-mount-first-unmounted-tb', 'ordinary-attack-controls-tb', 'unmounted-attack-controls-rt', 'phase3h-combat-loop-rt', 'phase3h-combat-loop-tb', 'phase3g-native-controls-rt', 'phase3g-native-controls-tb', 'phase3d-unified-combat-rt-suite','phase3d-unified-combat-tb-suite','phase3d-horse-presentation-suite',
        'player-action-availability','mount-dismount-user-flow',
        'mounted-pair-create-and-clear','mounted-pair-double-mount-rejected','mounted-pair-invalid-pair-rejected',
        'mounted-pair-cleanup-idempotent','mounted-pair-death-cleanup','mounted-pair-combat-start-cleanup',
        'mounted-pair-area-unload-cleanup','mounted-pair-mod-disable-cleanup',
        'mounted-pair-combat-start-retained','mounted-pair-combat-end-retained',
        'mounted-pair-rider-death-cleanup','mounted-pair-mount-death-cleanup',
        'mounted-pair-rider-incapacitated-cleanup','mounted-pair-mount-incapacitated-cleanup',
        'mounted-pair-rider-native-incapacitated-cleanup','mounted-pair-mount-native-incapacitated-cleanup',
        'mounted-pair-companion-removal-cleanup','mounted-pair-view-destroyed-cleanup','mounted-pair-exception-cleanup',
        'mounted-pair-open-ground',
        'mounted-pair-stop-start','mounted-pair-turns-and-corners','mounted-pair-doorway','mounted-distance-door-interaction','mounted-pair-selection',
        'mounted-pair-party-formation','mounted-pair-pause-unpause','mounted-pair-destination-cancel',
        'mounted-pair-turn-based-entry-cleanup','mounted-pair-realtime-entry-cleanup','mounted-pair-save-safety',
        'mounted-pair-load-safety','mounted-pair-area-transition-safety','fixture-intake','persistence-isolation','persistence-p07-save','persistence-p07-load','persistence-p01-save','persistence-p01-load','persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load','persistence-p04-save','persistence-p04-load','persistence-p05-save','persistence-p05-load','persistence-p06-load','lifecycle-suite','combat-lifecycle-suite',
        'native-save-clean-dismount','native-area-clean-dismount','native-mode-transition-cleanup',
        'presentation-residue-and-uninstall-safety','pose-idle','pose-walk-run','pose-turn-stop',
        'pose-doorway-formation','pose-equipment-variants','ui-selection-portrait-actionbar',
        'camera-follow-and-command-routing','chunk4-traversal-core','chunk4-traversal-slope','chunk4-area-cleanup','movement-suite','boundary-suite','presentation-suite',
        'mounted-rider-melee-hit-rt','mounted-rider-melee-hit-tb','mounted-rider-melee-miss-rt',
        'mounted-mammoth-primary-hit-rt','mounted-mammoth-primary-hit-tb',
        'mounted-rider-melee-move-to-attack-rt','mounted-rider-melee-move-to-attack-tb',
        'mounted-rider-melee-command-cancel-rt','mounted-rider-melee-command-cancel-tb',
        'mounted-rider-melee-command-interrupt-rt','mounted-rider-melee-command-interrupt-tb',
        'mounted-rider-melee-combat-end-rt','mounted-rider-melee-combat-end-tb',
        'mounted-rider-melee-human-play-path-rt','mounted-rider-melee-human-play-path-tb',
        'combat-core-control-suite',
        'manual-visual-review'
    )][string]$Scenario='mod-load-smoke',
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
    [ValidateRange(360,1200)][int]$TimeoutSeconds=360,
    [switch]$SaveAccessAllowed,
    # Explicit private compatibility selection. Other modes keep the actual live
    # mod set. This mode stages KMC alone while retaining installed UMM/Harmony.
    [switch]$BaseGameOnly,
    [switch]$DevelopmentScopedAudit,
    [switch]$ObserveWorkingDeserialization,
    [ValidateSet('authored-main-campaign')][string]$NativeBootstrapCase,
    [string]$OrdinarySelectionPath,
    [string]$OrdinaryFieldSelectionPath,
    [string]$BootstrapInventorySelectionPath,
    [string]$AssistedColdInventorySelectionPath,
    [string]$ManualSeedSelectionPath,
    [string]$AcquiredManualSelectionPath,
    [string]$AssistedFixtureSelectionPath,
    [string]$OwnedPairsSelectionPath,
    [string]$ManualDerivedSelectionPath,
    [string]$CumulativeAcquisitionSelectionPath,
    [string]$SmallCalibrationSelectionPath,
    [string]$SmallFixtureSelectionPath,
    [string]$StockMammothFixtureSelectionPath,
    [string]$ProgressionSelectionPath,
    [string]$PackagePath,
    [string]$FrozenSourceRoot,
    [string]$FrozenSourceFreezePath,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedFrozenSourceFreezeSha256,
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$PersistenceSourceRunId,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPersistenceSourceSha256,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPersistenceAlternateSha256,
    # The foreign-header P06 derivative is campaign B's own manual archive from a
    # completed campaign-b run; the alternate hash above then names that archive.
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$PersistenceForeignSourceRunId,
    # The genuine no-DLL cleanup-save observation: the removal observer package
    # (a separate minimal UMM mod) is staged in place of KMC for that one run.
    [string]$ObserverPackagePath,
    [ValidateSet('multi-exploration-manual','multi-exploration-quick','multi-exploration-auto','multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual','legacy-mounted-pending-tb-manual','pending-schema3-control-manual','multi-area-autosave','legacy-schema1-resave-manual','multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual','multi-nonselected-mount-unsettled-manual','multi-owned-worker-cancel-manual','owned-schema1-manual','owned-schema1-resave-manual','owned-schema1-current-manual','owned-schema2-pending-current-manual','multi-malformed-refusal-manual','multi-future-refusal-manual','owner-seed-manticore-fixture-manual','manticore-fixture-cold-manual','partial-movement','rider-spent','between-partner-orders','exhausted','explicit-end','combat-mount-tb','casting-items','step','conversion','round-effect','reaction','condition','condition-preparing','suspended','manual','quick','auto','manual-renamed','alternating','queued','unmounted-spent','mounted-spent','unmounted-attack','mounted-attack','unmounted-projectile','mounted-projectile','unmounted-approach','mounted-approach','unmounted-casting','mounted-casting','mounted-casting-items','combat-mount-rt','combat-dismount-rt','mounted-charge-pending','mounted-charge-settled','mounted-charge-cancelled','mounted-charge-failed','mounted-charge-drained','legacy','schema1','future','malformed','profile','campaign','missing-rider','missing-mount','mismatched-profile','policy','combat-missing','combat-ai','timeout','cancel-wait','locked-replace','serialization-cancel','serialization-cancel-output','disable-reenable','campaign-b','prepare-removal','disable-during-load','mounted-charge-area','mounted-charge-session','mounted-charge-disable','mounted-charge-removal','absent-kmc','removal-no-dll','mounted-charge-removal-no-dll','rider-death','mount-death','rider-size-change','area-reload','area-cross-entry','area-cross-exit','area-cross-entry-auto','area-cross-exit-auto','failed-area-load','foreign-header-campaign','combat-mount-routes','combat-mount-unsettled-save','combat-mount-area-reload','pending-mount-area')][string]$PersistenceCase,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$PersistenceAreaEnterPoint,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$PersistenceAreaTargetArea,
    [ValidatePattern('^[0-9a-f]{32}$')][string[]]$PersistenceReplayAreaEnterPoints,
    [ValidatePattern('^[0-9a-f]{32}$')][string[]]$PersistenceReplayAreaAreas,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPackageSha256,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPackageManifestSha256,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedDllSha256,
    [ValidatePattern('^[A-Za-z0-9._/-]{1,200}$')][string]$ExpectedBranch,
    [ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedCommit,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedCurrentQualificationSha256,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
    [string]$PriorSaveTransactionStatePath,
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedPriorSaveTransactionRunId,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveTransactionStateSha256,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveMetadataDigest,
    [string]$ProtectedSaveContinuityAuthorityPath,
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedProtectedSaveContinuityEpochId,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedSaveContinuityAuthoritySha256,
    [string]$ExpectedProtectedAutoSaveName,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedAutoSaveSha256,
    [string]$ExpectedProtectedQuickSaveName,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedQuickSaveSha256,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedSavePinSetSha256,
    [string]$QualificationSuiteSnapshotPath,
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedQualificationSuiteId,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedQualificationSuiteSnapshotSha256,
    [switch]$BootstrapOfflineCloudEvidence,
    [string]$SteamPath='C:\Program Files (x86)\Steam\steam.exe'
)

$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if($PSBoundParameters.ContainsKey('ObserveWorkingDeserialization') -and
    (-not$ObserveWorkingDeserialization -or $Scenario -cne 'export-mounted-contracts' -or -not$BaseGameOnly -or -not$SaveAccessAllowed)) {
    throw 'Working deserialization observation requires its exact true opt-in, export-mounted-contracts, qualified save access and explicit base-only staging.'
}
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'PersistenceProfileProtection.ps1')
. (Join-Path $PSScriptRoot 'PersistenceProfileAreasIsolation.ps1')
. (Join-Path $PSScriptRoot 'PersistenceSaveFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7PersistenceSaveFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7OrdinaryAcquisitionFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7OrdinaryFieldFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7BootstrapInventoryFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7AssistedColdInventoryFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7ManualSeedFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7ManualDerivedFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7CumulativeAcquisitionFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7AcquiredManualFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7AssistedFixtureFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7AssistedPreparationInput.ps1')
. (Join-Path $PSScriptRoot 'Chunk7OwnedPairsFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7OwnedSchema1Fixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7LoadRefusalFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7SmallCalibrationFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7SmallFixtureFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7StockMammothFixtureFixtures.ps1')
. (Join-Path $PSScriptRoot 'Chunk7SmallFixtureColdSource.ps1')
. (Join-Path $PSScriptRoot 'Chunk7ProgressionFixtures.ps1')
# Stale level-up autosave families are refused here, before any selection intake, lock,
# transaction, Mods swap or launch (docs/CHUNK7-STALE-LEVELUP-AUTOSAVE-FAMILIES.md).
Assert-KmcChunk7FamilyReady $Scenario $null
if(Test-KmcChunk7NativeBootstrapScenario $Scenario){
    if(-not$BaseGameOnly-or-not$PSBoundParameters.ContainsKey('NativeBootstrapCase')-or$NativeBootstrapCase-cne'authored-main-campaign'){
        throw 'Native main bootstrap requires explicit authored-main-campaign selection and base-only staging.'
    }
    foreach($name in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){
        if($PSBoundParameters.ContainsKey($name)){throw 'Native bootstrap cannot select a saved-game load, persistence case or area route.'}
    }
}elseif($PSBoundParameters.ContainsKey('NativeBootstrapCase')){throw 'Native bootstrap selection is forbidden under another scenario.'}
. (Join-Path $PSScriptRoot 'PersistenceValidationFixtures.ps1')
$assistedFixtureInput=$null
if(Test-KmcChunk7AssistedFixtureScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($AssistedFixtureSelectionPath)){throw 'Assisted setup requires explicit base-only staging and the exact owner-authorized closed selection.'}
    foreach($name in @('NativeBootstrapCase','PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){
        if($PSBoundParameters.ContainsKey($name)){throw 'Assisted setup permits only its authored new campaign and cumulative native Auto/Manual writes.'}
    }
    $assistedFixtureInput=Read-KmcChunk7AssistedPreparationSelectionFile $AssistedFixtureSelectionPath $(if($RunId){$RunId}else{'unassigned-assisted-fixture'})
}elseif($PSBoundParameters.ContainsKey('AssistedFixtureSelectionPath')){throw 'Assisted fixture selection is forbidden under another scenario.'}
$ownedPairsInput=$null;$ownedPairsSource=$null
if(Test-KmcChunk7OwnedPairsScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($OwnedPairsSelectionPath)){throw 'Inherited two-pair campaign requires explicit base-only staging and a qualified two-owner native Manual selection.'}
    foreach($name in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId')){
        if($PSBoundParameters.ContainsKey($name)){throw 'Inherited pairs select only their closed source and checkpoint cases.'}
    }
    if(($PSBoundParameters.ContainsKey('PersistenceAreaEnterPoint')-or$PSBoundParameters.ContainsKey('PersistenceAreaTargetArea'))-and$PersistenceCase-cne'multi-area-autosave'){throw 'Only the exact owned area autosave may carry its original/destination pins.'}
    if(($PSBoundParameters.ContainsKey('PersistenceReplayAreaEnterPoints')-or$PSBoundParameters.ContainsKey('PersistenceReplayAreaAreas'))-and$PersistenceCase-cne'multi-exploration-three-area-transfers-manual'){throw 'Only the exact owned repeated-area cold case may carry route pins.'}
    $ownedPairsInput=Read-KmcChunk7OwnedPairsSelectionFile $OwnedPairsSelectionPath $(if($RunId){$RunId}else{'unassigned-owned-pairs'}) $PersistenceCase
}elseif($PSBoundParameters.ContainsKey('OwnedPairsSelectionPath')){throw 'Inherited pair selection is forbidden under another scenario.'}
$smallFixtureInput=$null;$smallFixtureSource=$null
Assert-KmcChunk7SmallFixtureLaunchArguments $PSBoundParameters $Scenario ([bool]$BaseGameOnly) $TimeoutSeconds $SmallFixtureSelectionPath
if(Test-KmcChunk7SmallFixtureScenario $Scenario){
    $smallFixtureInput=Read-KmcChunk7SmallFixtureSelectionFile $SmallFixtureSelectionPath $(if($RunId){$RunId}else{'unassigned-f09-fixture'})
}
$stockMammothFixtureInput=$null;$stockMammothFixtureSource=$null
Assert-KmcChunk7StockMammothFixtureLaunchArguments $PSBoundParameters $Scenario ([bool]$BaseGameOnly) $TimeoutSeconds $StockMammothFixtureSelectionPath
if(Test-KmcChunk7StockMammothFixtureScenario $Scenario){
    $stockMammothFixtureInput=Read-KmcChunk7StockMammothFixtureSelectionFile $StockMammothFixtureSelectionPath $(if($RunId){$RunId}else{'unassigned-stock-mammoth-fixture'})
}
$acquiredManualInput=$null;$acquiredManualSource=$null
if(Test-KmcChunk7AcquiredManualScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($AcquiredManualSelectionPath)){throw 'Acquired Manual acquisition requires explicit base-only staging and exact successful Manual lineage plus observed readiness pins.'}
    foreach($name in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){
        if($PSBoundParameters.ContainsKey($name)){throw 'Acquired Manual acquisition selects only its closed one-load one-terminal-write source file.'}
    }
    $acquiredManualInput=Read-KmcChunk7AcquiredManualSelectionFile $AcquiredManualSelectionPath $(if($RunId){$RunId}else{'unassigned-manual-derived'})
}elseif($PSBoundParameters.ContainsKey('AcquiredManualSelectionPath')){throw 'Acquired Manual selection is forbidden under another scenario.'}
$manualDerivedInput=$null;$manualDerivedSource=$null
if(Test-KmcChunk7ManualDerivedScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($ManualDerivedSelectionPath)){throw 'Manual-derived acquisition requires explicit base-only staging and exact successful Manual lineage plus observed readiness pins.'}
    foreach($name in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){
        if($PSBoundParameters.ContainsKey($name)){throw 'Manual-derived acquisition selects only its closed one-load one-terminal-write source file.'}
    }
    $manualDerivedInput=Read-KmcChunk7ManualDerivedSelectionFile $ManualDerivedSelectionPath $(if($RunId){$RunId}else{'unassigned-manual-derived'})
}elseif($PSBoundParameters.ContainsKey('ManualDerivedSelectionPath')){throw 'Manual-derived selection is forbidden under another scenario.'}
$cumulativeAcquisitionInput=$null;$cumulativeAcquisitionSource=$null
if(Test-KmcChunk7CumulativeScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($CumulativeAcquisitionSelectionPath)){throw 'Manual-derived acquisition requires explicit base-only staging and exact successful Manual lineage plus observed readiness pins.'}
    foreach($name in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){
        if($PSBoundParameters.ContainsKey($name)){throw 'Manual-derived acquisition selects only its closed one-load one-terminal-write source file.'}
    }
    $cumulativeAcquisitionInput=Read-KmcChunk7CumulativeSelectionFile $CumulativeAcquisitionSelectionPath $(if($RunId){$RunId}else{'unassigned-cumulative'})
}elseif($PSBoundParameters.ContainsKey('CumulativeAcquisitionSelectionPath')){throw 'Manual-derived selection is forbidden under another scenario.'}
$manualSeedInput=$null;$manualSeedSource=$null
if(Test-KmcChunk7ManualSeedScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($ManualSeedSelectionPath)){throw 'Manual seed requires explicit base-only staging and exact completed bootstrap plus cold-inventory source pins.'}
    foreach($name in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){
        if($PSBoundParameters.ContainsKey($name)){throw 'Manual seed selects only its closed one-load one-write source file.'}
    }
    $manualSeedInput=Read-KmcChunk7ManualSeedSelectionFile $ManualSeedSelectionPath $(if($RunId){$RunId}else{'unassigned-manual-seed'})
}elseif($PSBoundParameters.ContainsKey('ManualSeedSelectionPath')){throw 'Manual seed selection is forbidden under another scenario.'}
$assistedColdInput=$null;$assistedColdSource=$null
if(Test-KmcChunk7AssistedColdInventoryScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($AssistedColdInventorySelectionPath)){throw 'Assisted cold inventory requires base-only staging and exact completed assisted final Manual source pins.'}
    foreach($name in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){
        if($PSBoundParameters.ContainsKey($name)){throw 'Assisted cold inventory selects only its closed one-load zero-write Manual source file.'}
    }
    $assistedColdInput=Read-KmcChunk7AssistedColdInventorySelectionFile $AssistedColdInventorySelectionPath $(if($RunId){$RunId}else{'unassigned-assisted-cold'})
}elseif($PSBoundParameters.ContainsKey('AssistedColdInventorySelectionPath')){throw 'Assisted cold selection is forbidden under another scenario.'}
$bootstrapInventoryInput=$null;$bootstrapInventorySource=$null
if(Test-KmcChunk7BootstrapInventoryScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($BootstrapInventorySelectionPath)){throw 'Bootstrap cold inventory requires explicit base-only staging and exact completed bootstrap source pins.'}
    foreach($name in @('PersistenceSourceRunId','ExpectedPersistenceSourceSha256','ExpectedPersistenceAlternateSha256','PersistenceForeignSourceRunId','PersistenceCase','PersistenceAreaEnterPoint','PersistenceAreaTargetArea','PersistenceReplayAreaEnterPoints','PersistenceReplayAreaAreas')){
        if($PSBoundParameters.ContainsKey($name)){throw 'Bootstrap cold inventory selects only its closed one-load source file.'}
    }
    $bootstrapInventoryInput=Read-KmcChunk7BootstrapInventorySelectionFile $BootstrapInventorySelectionPath $(if($RunId){$RunId}else{'unassigned-cold-inventory'})
}elseif($PSBoundParameters.ContainsKey('BootstrapInventorySelectionPath')){throw 'Bootstrap inventory selection is forbidden under another scenario.'}
$smallSelectionInput=$null;$smallSource=$null
if(Test-KmcChunk7SmallScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($SmallCalibrationSelectionPath)){throw 'Small calibration requires explicit base-only staging and exact compiled declaration/source pins.'}
    if($PSBoundParameters.ContainsKey('PersistenceSourceRunId')-or$PSBoundParameters.ContainsKey('ExpectedPersistenceSourceSha256')){throw 'Small calibration source is selected only by its closed selection file.'}
    $smallSelectionInput=Read-KmcChunk7SmallSelectionFile $SmallCalibrationSelectionPath $(if($RunId){$RunId}else{'unassigned-small-run'})
}elseif($PSBoundParameters.ContainsKey('SmallCalibrationSelectionPath')){throw 'Small calibration selection is forbidden under another scenario.'}
$progressionSelectionInput=$null;$progressionSource=$null
if(Test-KmcChunk7ProgressionScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($ProgressionSelectionPath)){throw 'Native progression requires exact source/inventory pins and explicit base-only staging.'}
    if($PSBoundParameters.ContainsKey('PersistenceSourceRunId')-or$PSBoundParameters.ContainsKey('ExpectedPersistenceSourceSha256')){throw 'Progression source is selected only through its closed selection file.'}
    $progressionSelectionInput=Read-KmcChunk7ProgressionSelectionFile $ProgressionSelectionPath $(if($RunId){$RunId}else{'unassigned-progression-run'})
    # Progression readiness depends on the selected case; still before any lock or launch.
    Assert-KmcChunk7FamilyReady $Scenario ([string]$progressionSelectionInput.selection.case)
}elseif($PSBoundParameters.ContainsKey('ProgressionSelectionPath')){throw 'Progression selection is forbidden under another scenario.'}
$fieldSelectionInput=$null;$fieldSource=$null
if(Test-KmcChunk7FieldScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($OrdinaryFieldSelectionPath)){throw 'Field admission requires explicit base-only staging and exact paid acquisition source pins.'}
    if($PSBoundParameters.ContainsKey('PersistenceSourceRunId')-or$PSBoundParameters.ContainsKey('ExpectedPersistenceSourceSha256')){throw 'Field source is selected only by its closed selection file.'}
    $fieldSelectionInput=Read-KmcChunk7FieldSelectionFile $OrdinaryFieldSelectionPath $(if($RunId){$RunId}else{'unassigned-field-run'})
}elseif($PSBoundParameters.ContainsKey('OrdinaryFieldSelectionPath')){throw 'Field selection is forbidden under another scenario.'}
$ordinarySelectionInput=$null;$ordinaryInventorySource=$null
if(Test-KmcChunk7OrdinaryScenario $Scenario){
    if(-not$BaseGameOnly-or[string]::IsNullOrEmpty($OrdinarySelectionPath)){throw 'Ordinary acquisition requires explicit base-only isolation and a caller-pinned native inventory selection file.'}
    if(-not$PSBoundParameters.ContainsKey('TimeoutSeconds')){$TimeoutSeconds=900}
    elseif($TimeoutSeconds-ne900){throw 'Ordinary acquisition requires its declared 900-second outer native choice/worker/cleanup budget.'}
    $ordinarySelectionInput=Read-KmcChunk7OrdinarySelectionFile $OrdinarySelectionPath $(if($RunId){$RunId}else{'unassigned-run'})
}elseif($PSBoundParameters.ContainsKey('OrdinarySelectionPath')){throw 'Ordinary selection cannot be supplied to another native scenario.'}
if($PSBoundParameters.ContainsKey('PersistenceCase') -and $Scenario -cnotin @('chunk7-owned-two-pair-campaign','chunk7-persistence-save','chunk7-persistence-load','persistence-p07-save','persistence-p07-load','persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load','persistence-p04-save','persistence-p04-load','persistence-p05-save','persistence-p05-load','persistence-p06-load')) { throw 'PersistenceCase is restricted to the exact combat scenarios.' }
if(((Test-KmcChunk7CombatScenario $Scenario)-or(Test-KmcChunk7RealtimeScenario $Scenario)-or(Test-KmcChunk7NativeLifeScenario $Scenario))-and-not$BaseGameOnly){throw 'Chunk 7 combat requires explicit base-only staging.'}
if((Test-KmcChunk7OwnedPairsScenario $Scenario)-or(Test-KmcChunk7PersistenceScenario $Scenario)-or(Test-KmcChunk7CombatScenario $Scenario)-or(Test-KmcChunk7RealtimeScenario $Scenario)-or(Test-KmcChunk7NativeLifeScenario $Scenario)){
    if(-not$PSBoundParameters.ContainsKey('TimeoutSeconds')){$TimeoutSeconds=600}
    elseif($TimeoutSeconds-lt600){throw 'Selected Chunk 7 native scenarios require the declared 600-second outer worker/cleanup budget.'}
}
# The owner-seed kinds (exploration and the owner-seed-derived Manual producer) add their bounded
# native SETUP phase (ordinary level-ups) before the measured steps.
if((Test-KmcChunk7OwnedPairsScenario $Scenario)-and((Test-KmcChunk7OwnedPairsOwnerSeed ([pscustomobject]@{ownedPairs=$ownedPairsInput.selection}))-or(Test-KmcChunk7OwnedPairsSeedManual ([pscustomobject]@{ownedPairs=$ownedPairsInput.selection})))){
    if(-not$PSBoundParameters.ContainsKey('TimeoutSeconds')){$TimeoutSeconds=900}
    elseif($TimeoutSeconds-ne900){throw 'Owner-seed two-pair exploration requires its declared 900-second outer setup/worker/cleanup budget.'}
}
if(Test-KmcChunk7RepeatedWorldCase $PersistenceCase){
    if($Scenario-cne'chunk7-persistence-load'-and-not((Test-KmcChunk7OwnedPairsScenario $Scenario)-and$ownedPairsInput.selection.case-cin@('persistence-load','owner-seed-persistence-load'))){throw 'Repeated world cases are cold-load only.'}
    if(-not$PSBoundParameters.ContainsKey('TimeoutSeconds')){$TimeoutSeconds=900}
    elseif($TimeoutSeconds-ne900){throw 'Repeated worlds require exactly the declared 900-second outer admission/cleanup budget.'}
}
if($PersistenceCase-ceq'multi-exploration-three-area-transfers-manual'){
    if(@($PersistenceReplayAreaEnterPoints).Count-ne3-or@($PersistenceReplayAreaAreas).Count-ne3){throw 'Repeated area transfer requires exactly three caller-pinned enter points and destination areas.'}
}elseif($PSBoundParameters.ContainsKey('PersistenceReplayAreaEnterPoints')-or$PSBoundParameters.ContainsKey('PersistenceReplayAreaAreas')){throw 'Only the repeated-area case accepts native route pins.'}
if($PersistenceCase-ceq'legacy-schema1-resave-manual'-and$Scenario-cne'chunk7-persistence-load'){throw 'Schema-1 native resave is load-only.'}
if((Test-KmcOwnedSchema1Case $PersistenceCase)-and$Scenario-cne'chunk7-owned-two-pair-campaign'){throw 'Owned schema-1 cases belong only to the closed inherited family.'}
if($PersistenceCase-ceq'owner-seed-manticore-fixture-manual'-and$Scenario-cne'chunk7-owned-two-pair-campaign'){throw 'Manticore fixture preparation belongs only to its source-only inherited checkpoint.'}
if((Test-KmcChunk7OwnerSeedCurrentCheckpoint $PersistenceCase)-and$Scenario-cne'chunk7-owned-two-pair-campaign'){throw 'Current-format migration consumers belong only to the owner-seed checkpoint family.'}
if(Test-KmcChunk7PersistenceScenario $Scenario){
    if(-not$BaseGameOnly-or$PersistenceCase-cnotin(Get-KmcChunk7PersistenceCases)){throw 'Chunk 7 persistence requires explicit base-only staging and one declared native case.'}
    if($Scenario-ceq'chunk7-persistence-load'-and([string]::IsNullOrEmpty($PersistenceSourceRunId)-or[string]::IsNullOrEmpty($ExpectedPersistenceSourceSha256))){throw 'Chunk 7 cold load requires its exact completed source run and archive hash.'}
}elseif($PersistenceCase-cin(Get-KmcChunk7PersistenceCases)-and-not(Test-KmcChunk7OwnedPairsScenario $Scenario)){throw 'Chunk 7 persistence cases cannot run under a historical scenario.'}
if($Scenario -cin @('persistence-p05-save','persistence-p05-load')){
    $slotCases=if($Scenario-ceq'persistence-p05-load'){@('manual','quick','auto','manual-renamed','alternating','queued')}else{@('manual','quick','auto','alternating','queued')}
    if($PersistenceCase-cnotin $slotCases){throw 'P05 requires its exact native slot category.'}
}elseif($PersistenceCase-cin @('manual','quick','auto','manual-renamed','alternating','queued')){throw 'P05 slot category cannot run under another scenario.'}
if($Scenario -cin @('persistence-p04-save','persistence-p04-load')){
    if($PersistenceCase-cnotin @('unmounted-spent','mounted-spent','unmounted-attack','mounted-attack','unmounted-projectile','mounted-projectile','unmounted-approach','mounted-approach','unmounted-casting','mounted-casting','mounted-casting-items','combat-mount-rt','combat-dismount-rt','mounted-charge-pending','mounted-charge-settled','mounted-charge-cancelled','mounted-charge-failed','mounted-charge-drained','combat-mount-routes','combat-mount-unsettled-save','combat-mount-area-reload','pending-mount-area')){throw 'P04 requires its exact native RT checkpoint.'}
}elseif($PersistenceCase-cin @('unmounted-spent','mounted-spent','unmounted-attack','mounted-attack','unmounted-projectile','mounted-projectile','unmounted-approach','mounted-approach','unmounted-casting','mounted-casting','mounted-casting-items','combat-mount-rt','combat-dismount-rt','mounted-charge-pending','mounted-charge-settled','mounted-charge-cancelled','mounted-charge-failed','mounted-charge-drained','combat-mount-routes','combat-mount-unsettled-save','combat-mount-area-reload','pending-mount-area')){throw 'P04 checkpoint cannot run under another scenario.'}
if($Scenario -cin @('persistence-p03-save','persistence-p03-load')){
    if($PersistenceCase-cnotin @('step','conversion','round-effect','reaction','condition','condition-preparing','suspended')){throw 'P03 requires its exact step/conversion/round-effect checkpoint.'}
}elseif($PersistenceCase-cin @('step','conversion','round-effect','reaction','condition','condition-preparing','suspended')){throw 'P03 checkpoint cannot run under another scenario.'}
if($Scenario-ceq'persistence-p05-load'-and$PersistenceCase-ceq'alternating'){
    if([string]::IsNullOrEmpty($ExpectedPersistenceAlternateSha256)-or$ExpectedPersistenceAlternateSha256-ceq$ExpectedPersistenceSourceSha256){throw 'Alternating cold loads require two distinct exact archive hashes.'}
}elseif($Scenario-ceq'persistence-p06-load'-and$PersistenceCase-ceq'foreign-header-campaign'){
    if([string]::IsNullOrEmpty($ExpectedPersistenceAlternateSha256)-or$ExpectedPersistenceAlternateSha256-ceq$ExpectedPersistenceSourceSha256-or[string]::IsNullOrEmpty($PersistenceForeignSourceRunId)){throw 'A foreign-header validation load requires campaign B own archive from its completed campaign-b run.'}
}elseif(-not[string]::IsNullOrEmpty($ExpectedPersistenceAlternateSha256)){throw 'Only alternating P05 cold loads and the foreign-header P06 load may select a second archive.'}
if(-not[string]::IsNullOrEmpty($PersistenceForeignSourceRunId)-and-not($Scenario-ceq'persistence-p06-load'-and$PersistenceCase-ceq'foreign-header-campaign')){throw 'Only the foreign-header P06 load takes campaign B own archive.'}
if($Scenario -cin @('persistence-p07-save','persistence-p07-load')){
    if($PersistenceCase-cnotin @('timeout','cancel-wait','locked-replace','serialization-cancel','serialization-cancel-output','disable-reenable','campaign-b','prepare-removal','disable-during-load','mounted-charge-area','mounted-charge-session','mounted-charge-disable','mounted-charge-removal','absent-kmc','removal-no-dll','mounted-charge-removal-no-dll','rider-death','mount-death','rider-size-change','area-reload','area-cross-entry','area-cross-exit','area-cross-entry-auto','area-cross-exit-auto')){throw 'P07 requires its exact owned recovery case.'}
    if($PersistenceCase-cin @('prepare-removal','disable-during-load','mounted-charge-area','mounted-charge-session','mounted-charge-disable','mounted-charge-removal')-and$Scenario-cne'persistence-p07-save'){throw 'A removal or disable-during-load case is save-only.'}
    if($PersistenceCase-ceq'absent-kmc'-and$Scenario-cne'persistence-p07-load'){throw 'The integration-absent case is cold-load only.'}
    if($PersistenceCase-cin @('removal-no-dll','mounted-charge-removal-no-dll')-and$Scenario-cne'persistence-p07-load'){throw 'The no-DLL removal case is cold-load only.'}
}elseif($PersistenceCase-cin @('timeout','cancel-wait','locked-replace','serialization-cancel','serialization-cancel-output','disable-reenable','campaign-b','prepare-removal','disable-during-load','mounted-charge-area','mounted-charge-session','mounted-charge-disable','mounted-charge-removal','absent-kmc','removal-no-dll','mounted-charge-removal-no-dll','rider-death','mount-death','rider-size-change','area-reload','area-cross-entry','area-cross-exit','area-cross-entry-auto','area-cross-exit-auto')){throw 'Recovery faults require the exact P07 scenario.'}
if($PersistenceCase-ceq'multi-area-autosave'-or$PersistenceCase-cin @('area-cross-entry','area-cross-exit','area-cross-entry-auto','area-cross-exit-auto')){
    if($PersistenceCase-cin @('area-cross-entry-auto','area-cross-exit-auto')-and$Scenario-cne'persistence-p07-load'){throw 'A transition autosave case is cold-load only.'}
    if([string]::IsNullOrEmpty($PersistenceAreaEnterPoint)-or[string]::IsNullOrEmpty($PersistenceAreaTargetArea)){
        throw 'A cross-area transfer requires its declared native enter point and destination area.'
    }
}elseif(-not[string]::IsNullOrEmpty($PersistenceAreaEnterPoint)-or-not[string]::IsNullOrEmpty($PersistenceAreaTargetArea)){
    throw 'Only an exact cross-area transfer may declare a native destination.'
}
if($Scenario-ceq'persistence-p06-load'){
    if($PersistenceCase-cnotin @('legacy','schema1','future','malformed','profile','campaign','missing-rider','missing-mount','mismatched-profile','policy','combat-missing','combat-ai','failed-area-load','foreign-header-campaign')){throw 'P06 requires its exact validation variant.'}
}elseif($PersistenceCase-cin @('legacy','schema1','future','malformed','profile','campaign','missing-rider','missing-mount','mismatched-profile','policy','combat-missing','combat-ai','failed-area-load','foreign-header-campaign')){throw 'Validation variants require the exact P06 scenario.'}
$isObserver=$Scenario-ceq'persistence-p07-load'-and$PersistenceCase-cin @('removal-no-dll','mounted-charge-removal-no-dll')
if($isObserver-and[string]::IsNullOrWhiteSpace($ObserverPackagePath)){throw 'The no-DLL removal case requires the removal-observer package.'}
if(-not$isObserver-and-not[string]::IsNullOrWhiteSpace($ObserverPackagePath)){throw 'Only the no-DLL removal case takes the removal-observer package.'}
$requestedWhatIf=[bool]$WhatIfPreference
$WhatIfPreference=$false
$KmcFrozenProductContext=$null
$frozenParameters=@('FrozenSourceRoot','FrozenSourceFreezePath','ExpectedFrozenSourceFreezeSha256')
$boundFrozen=@($frozenParameters | Where-Object {$PSBoundParameters.ContainsKey($_)})
if($boundFrozen.Count -ne 0) {
    if($boundFrozen.Count -ne 3) { throw 'Frozen product execution requires the complete source and original-freeze pin set.' }
    $KmcFrozenProductContext=Get-KmcFrozenProductSource -RepositoryRoot (Get-KmcRepositoryRoot) -SourceRoot $FrozenSourceRoot -FreezePath $FrozenSourceFreezePath -ExpectedFreezeSha256 $ExpectedFrozenSourceFreezeSha256
}
$repoRoot=Get-KmcRepositoryRoot
$labRoot=Get-KmcLabRoot
$intake=Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json')|ConvertFrom-Json
$fingerprintPath=Join-Path $repoRoot 'planning\ENVIRONMENT-FINGERPRINT.json'
$fingerprint=Read-KmcJson $fingerprintPath
$runtimeState=Join-Path $labRoot 'runtime-state'
$runtimeBackups=Join-Path $labRoot 'runtime-backups'
$runtimeStaging=Join-Path $labRoot 'runtime-staging'
$runtimeEvidence=Join-Path $labRoot 'runtime-evidence'
$liveMods=[string]$intake.requestedLayout.kingmakerModsRoot
$saveRoot=[string]$intake.requestedLayout.kingmakerSaveRoot
$gameExecutable=Join-Path ([string]$intake.requestedLayout.kingmakerInstallDir) 'Kingmaker.exe'
$expectedGameExecutableHash=[string](@($fingerprint.kingmaker.files|Where-Object role -eq 'executable')[0].sha256)
$isSaveBacked=[string]$Scenario -cne 'mod-load-smoke'
$isManualReview=[string]$Scenario -ceq 'manual-visual-review'
# Development-scoped WhatIf is opt-in, WhatIf-only and never a release/milestone proof.
if($DevelopmentScopedAudit){
    if(-not$requestedWhatIf){throw 'The development-scoped audit is valid only together with -WhatIf; native runs always keep every guard.'}
    if($isManualReview){throw 'The development-scoped audit is refused for the manual-review scenario.'}
    if([string]::IsNullOrEmpty($RunId)){throw 'The development-scoped audit requires an explicit -RunId so its run-owned paths are defined.'}
}
if($BaseGameOnly-and($isObserver-or$isManualReview)){throw 'Base-game compatibility is a separate automated selection, not a removal-observer or human-review mode.'}
$baseGameDependencies=$null
if($BaseGameOnly){$baseGameDependencies=Get-KmcBaseGameDependencies -GameRoot ([string]$intake.requestedLayout.kingmakerInstallDir) -Fingerprint $fingerprint}
if($BootstrapOfflineCloudEvidence -and $isSaveBacked){
    throw '-BootstrapOfflineCloudEvidence is restricted to the no-save mod-load-smoke scenario.'
}
$legacyContinuityPinNames=@(
    'ExpectedCurrentQualificationSha256','ExpectedSupersededWorkingSha256','PriorSaveTransactionStatePath',
    'ExpectedPriorSaveTransactionRunId','ExpectedPriorSaveTransactionStateSha256','ExpectedPriorSaveMetadataDigest',
    'ProtectedSaveContinuityAuthorityPath','ExpectedProtectedSaveContinuityEpochId',
    'ExpectedProtectedSaveContinuityAuthoritySha256','ExpectedProtectedAutoSaveName','ExpectedProtectedAutoSaveSha256',
    'ExpectedProtectedQuickSaveName','ExpectedProtectedQuickSaveSha256','ExpectedProtectedSavePinSetSha256'
)
$boundLegacyContinuityPinNames=@($legacyContinuityPinNames|Where-Object{$PSBoundParameters.ContainsKey($_)})
$suitePinNames=@('QualificationSuiteSnapshotPath','ExpectedQualificationSuiteId','ExpectedQualificationSuiteSnapshotSha256')
$boundSuitePinNames=@($suitePinNames|Where-Object{$PSBoundParameters.ContainsKey($_)})
if($isSaveBacked -and ($boundSuitePinNames.Count-ne3 -or $boundLegacyContinuityPinNames.Count-ne0)){
    throw 'A save-backed runtime scenario requires exactly one complete qualification-suite snapshot pin set and rejects historical whole-directory admission pins.'
}
if(-not$isSaveBacked -and ($boundSuitePinNames.Count-ne0 -or $boundLegacyContinuityPinNames.Count-ne0)){
    throw 'A no-save runtime scenario rejects qualification-suite and historical save-continuity pins.'
}
$artifactPinNames=@('ExpectedPackageSha256','ExpectedPackageManifestSha256','ExpectedDllSha256','ExpectedBranch','ExpectedCommit')
$boundArtifactPinNames=@($artifactPinNames|Where-Object{$PSBoundParameters.ContainsKey($_)})
[void](Assert-KmcManualReviewArtifactPinCombination `
    -IsManualReview $isManualReview -BoundArtifactPinNames $boundArtifactPinNames `
    -ExpectedPackageSha256 $ExpectedPackageSha256 `
    -ExpectedPackageManifestSha256 $ExpectedPackageManifestSha256 `
    -ExpectedDllSha256 $ExpectedDllSha256 `
    -ExpectedBranch $ExpectedBranch -ExpectedCommit $ExpectedCommit)

if($isSaveBacked -and -not $SaveAccessAllowed){
    throw 'A save-backed Phase 1 scenario requires the explicit -SaveAccessAllowed operator gate; it authorizes only the exact qualified Working fixture.'
}
if(-not $isSaveBacked -and $SaveAccessAllowed){
    throw 'The schema-v1 mod-load-smoke scenario is an exact no-save run and rejects -SaveAccessAllowed.'
}
if($isSaveBacked -and @(Get-KmcSaveBackedRuntimeScenarios|Where-Object { $_ -ceq $Scenario }).Count -ne 1){
    throw 'The requested scenario is outside the save-backed mission allowlist.'
}
if([string]::IsNullOrWhiteSpace($PackagePath)){
    $version=Read-KmcJson (Join-Path $repoRoot 'version.json')
    $PackagePath=Join-Path (Join-Path $labRoot 'artifacts') ("KingmakerMountedCombat-{0}-diagnostic.zip"-f $version.productVersion)
}
$PackagePath=[IO.Path]::GetFullPath($PackagePath)
$packageManifestPath=$PackagePath+'.manifest.json'
& (Join-Path $repoRoot 'scripts\Validate-Source.ps1')
& (Join-Path $repoRoot 'scripts\Validate-Package.ps1') -PackagePath $PackagePath
$manifest=Assert-KmcPackageManifest $PackagePath $packageManifestPath
$observerManifest=$null
if($isObserver){
    # The observer is built from the exact candidate commit; the candidate DLL
    # itself is what this run keeps out of the process.
    $ObserverPackagePath=[IO.Path]::GetFullPath($ObserverPackagePath)
    & (Join-Path $repoRoot 'scripts\Validate-ObserverPackage.ps1') -PackagePath $ObserverPackagePath
    $observerManifest=Assert-KmcPackageManifest $ObserverPackagePath ($ObserverPackagePath+'.manifest.json') -Generator 'scripts/Package-Observer.ps1'
    if([string]$observerManifest.commit-cne[string]$manifest.commit-or[string]$observerManifest.branch-cne[string]$manifest.branch){throw 'The removal-observer package was not built from the candidate commit.'}
}
if($isManualReview -and (
    (Get-KmcSha256 $PackagePath)-cne$ExpectedPackageSha256 -or
    (Get-KmcSha256 $packageManifestPath)-cne$ExpectedPackageManifestSha256 -or
    [string]$manifest.dllSha256-cne$ExpectedDllSha256 -or
    [string]$manifest.branch-cne$ExpectedBranch -or
    [string]$manifest.commit-cne$ExpectedCommit)){
    throw 'Manual-review package, manifest, DLL, branch, or commit differs from its explicit caller pin.'
}
Assert-KmcNoGameProcesses
if(Test-Path -LiteralPath (Join-Path $runtimeState 'active-transaction.lock')){throw 'A stale or active KMC runtime transaction sentinel exists.'}

# Save-backed preflight opens only the two exact canonical KMC fixture headers.
# Every foreign save is treated as opaque bytes: name/type/length/time/raw hash
# are compared to the admitted suite snapshot before ShouldProcess.
$qualificationPath=Assert-KmcChildPath (Join-Path $runtimeState 'fixture-qualification.json') $runtimeState 'fixture qualification'
$preflightContinuity=$null
$preflightPair=$null
$fixturePayload=$null
if($isSaveBacked){
    $preflightContinuity=Assert-KmcQualificationSuiteContinuity `
        -SnapshotPath $QualificationSuiteSnapshotPath -StateRoot $runtimeState -SaveRoot $saveRoot -ModsRoot $liveMods `
        -QualificationPath $qualificationPath -PackagePath $PackagePath -PackageManifest $manifest `
        -ExpectedSuiteId $ExpectedQualificationSuiteId -ExpectedSnapshotSha256 $ExpectedQualificationSuiteSnapshotSha256
    $preflightPair=$preflightContinuity.pair
    $fixturePayload=New-KmcRuntimeFixturePayload $preflightPair -ReadOnly:$isManualReview
}
if(Test-KmcChunk7OrdinaryScenario $Scenario){
    $ordinaryInventorySource=Get-KmcChunk7OrdinaryInventorySource $ordinarySelectionInput.selection $fixturePayload $manifest
}
if(Test-KmcChunk7SmallScenario $Scenario){$smallSource=if(Test-KmcChunk7SmallManualCase $smallSelectionInput.selection){Get-KmcChunk7SmallManualSource $smallSelectionInput.selection.manualSource $fixturePayload $manifest -Assisted:(Test-KmcChunk7SmallAssistedCase $smallSelectionInput.selection)}else{Get-KmcChunk7FieldSource $smallSelectionInput.selection.source $fixturePayload $manifest}}
if(Test-KmcChunk7ProgressionScenario $Scenario){$progressionSource=Get-KmcChunk7ProgressionSource $progressionSelectionInput.selection $fixturePayload $manifest}
if(Test-KmcChunk7FieldScenario $Scenario){$fieldSource=Get-KmcChunk7FieldSource $fieldSelectionInput.selection $fixturePayload $manifest}
$ownedAreaTarget=if((Test-KmcChunk7OwnedPairsScenario $Scenario)-and$PersistenceCase-ceq'multi-area-autosave'){[pscustomobject]@{enterPoint=$PersistenceAreaEnterPoint;area=$PersistenceAreaTargetArea;autoSaveMode='AfterEntry'}}else{$null}
if(Test-KmcChunk7OwnedPairsScenario $Scenario){$ownedPairsSource=Get-KmcChunk7OwnedPairsSource $ownedPairsInput.selection $PersistenceCase $fixturePayload $manifest $ownedAreaTarget}
if(Test-KmcChunk7SmallFixtureScenario $Scenario){$smallFixtureSource=Get-KmcChunk7SmallFixtureSource $smallFixtureInput.selection $fixturePayload $manifest}
if(Test-KmcChunk7StockMammothFixtureScenario $Scenario){$stockMammothFixtureSource=Get-KmcChunk7StockMammothFixtureSource $stockMammothFixtureInput.selection $fixturePayload $manifest}
if(Test-KmcChunk7AcquiredManualScenario $Scenario){$acquiredManualSource=Get-KmcChunk7AcquiredManualSource $acquiredManualInput.selection $fixturePayload $manifest}
if(Test-KmcChunk7ManualDerivedScenario $Scenario){$manualDerivedSource=Get-KmcChunk7ManualDerivedSource $manualDerivedInput.selection $fixturePayload $manifest}
if(Test-KmcChunk7CumulativeScenario $Scenario){$cumulativeAcquisitionSource=Get-KmcChunk7CumulativeSource $cumulativeAcquisitionInput.selection $fixturePayload $manifest}
if(Test-KmcChunk7ManualSeedScenario $Scenario){$manualSeedSource=Get-KmcChunk7ManualSeedSource $manualSeedInput.selection $fixturePayload $manifest}
if(Test-KmcChunk7BootstrapInventoryScenario $Scenario){$bootstrapInventorySource=Get-KmcChunk7BootstrapInventorySource $bootstrapInventoryInput.selection $fixturePayload $manifest}
if(Test-KmcChunk7AssistedColdInventoryScenario $Scenario){$assistedColdSource=Get-KmcChunk7AssistedColdInventorySource $assistedColdInput.selection $fixturePayload $manifest}
$beforeMods=Get-KmcDirectoryManifest $liveMods
$beforeSaves=Get-KmcSaveMetadataInventory $saveRoot
if($isSaveBacked){
    Assert-KmcSaveMetadataInventoriesEqual `
        -Before $preflightContinuity.saveMetadata `
        -After $beforeSaves `
        -Description 'runtime preflight fixture-continuity save metadata'
}
$WhatIfPreference=$requestedWhatIf
$action=if($isManualReview){'open guarded read-only KMC manual visual review against Working fixture only'}elseif($isSaveBacked){"run guarded KMC $Scenario against Working fixture only"}else{'run guarded KMC mod-load-smoke'}
if(-not $PSCmdlet.ShouldProcess('Steam App 640820, exact live Kingmaker Mods, and guarded KMC save policy',$action)){
    $WhatIfPreference=$false
    # Historical archives are compared only by this purity check. Live runs
    # still snapshot every current Mods byte and retain all save/restore guards.
    $scopedArchiveBefore=$null
    if($DevelopmentScopedAudit){
        # Exposed surfaces keep whole-tree content identity; archives are run-scoped.
        Write-Host 'WhatIf audit scope: development-run-scoped (runtime-state and live Mods content identity; archive roots run-owned paths and top-level names only).'
        $beforeRoots=@((Get-KmcDirectoryManifest $runtimeState),$beforeMods)
        $scopedArchiveBefore=Get-KmcWhatIfRunScopedArchiveState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging -EvidenceRoot $runtimeEvidence -RunId $RunId
    }else{
    $beforeRoots=@(
        (Get-KmcDirectoryManifest $runtimeState),(Get-KmcDirectoryManifest $runtimeBackups),
        (Get-KmcDirectoryManifest $runtimeStaging),(Get-KmcDirectoryManifest $runtimeEvidence),
        $beforeMods
    )
    }
    if($isSaveBacked){
        $whatIfContinuity=Assert-KmcQualificationSuiteContinuity `
            -SnapshotPath $QualificationSuiteSnapshotPath -StateRoot $runtimeState -SaveRoot $saveRoot -ModsRoot $liveMods `
            -QualificationPath $qualificationPath -PackagePath $PackagePath -PackageManifest $manifest `
            -ExpectedSuiteId $ExpectedQualificationSuiteId -ExpectedSnapshotSha256 $ExpectedQualificationSuiteSnapshotSha256
        if((New-KmcRuntimeFixturePayload $whatIfContinuity.pair -ReadOnly:$isManualReview|ConvertTo-Json -Depth 10 -Compress)-cne
            ($fixturePayload|ConvertTo-Json -Depth 10 -Compress)){
            throw 'KMC fixture identity changed during runtime WhatIf continuity validation.'
        }
        Assert-KmcSaveMetadataInventoriesEqual `
            -Before $beforeSaves `
            -After $whatIfContinuity.saveMetadata `
            -Description 'runtime WhatIf fixture-continuity save metadata'
    }
    if($DevelopmentScopedAudit){
        $afterRoots=@((Get-KmcDirectoryManifest $runtimeState),(Get-KmcDirectoryManifest $liveMods))
        Assert-KmcWhatIfRunScopedArchiveUnchanged $scopedArchiveBefore (Get-KmcWhatIfRunScopedArchiveState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging -EvidenceRoot $runtimeEvidence -RunId $RunId)
    }else{
    $afterRoots=@(
        (Get-KmcDirectoryManifest $runtimeState),(Get-KmcDirectoryManifest $runtimeBackups),
        (Get-KmcDirectoryManifest $runtimeStaging),(Get-KmcDirectoryManifest $runtimeEvidence),
        (Get-KmcDirectoryManifest $liveMods)
    )
    }
    # Unchanged whole-tree comparison and failure detail (shared so it is testable).
    Assert-KmcWhatIfRootsUnchanged -Before $beforeRoots -After $afterRoots
    if((Get-KmcSaveMetadataInventory $saveRoot).digest-cne$beforeSaves.digest){throw 'WhatIf purity failed: save metadata changed.'}
    Assert-KmcNoGameProcesses
    if($BootstrapOfflineCloudEvidence){[void](Assert-KmcSteamSafety $SteamPath -AllowMissingCurrentSessionCloudState)}
    if($BaseGameOnly){Assert-KmcBaseGameDependencyReceipt $baseGameDependencies -VerifyFiles;Write-Host ('Base-game-only KMC staging selected; unchanged installed loader dependency digest: '+$baseGameDependencies.dependencyDigest)}
    if($DevelopmentScopedAudit){
        # Deliberately not the full-proof sentence: no reader may accept it as whole-lab equality.
        Write-Host ('Runtime WhatIf development-scoped check PASS (scope: development-run-scoped, run '+$RunId+'; NOT a full whole-lab purity proof): runtime-state and the live Mods tree were compared by whole-tree content identity and save metadata was compared; historical archives under runtime-backups, runtime-staging and runtime-evidence were checked only for run-owned paths and new top-level entries, not rehashed.')
        return
    }
    Write-Host 'Runtime WhatIf purity PASS; exact fixture descriptors were validated when required, and no evidence, lock, transaction, Mods, process, game, or save mutation occurred.'
    return
}
$WhatIfPreference=$false
$ConfirmPreference='None'
$steamSafety=Assert-KmcSteamSafety $SteamPath -AllowMissingCurrentSessionCloudState:$BootstrapOfflineCloudEvidence
$actualRunId=if([string]::IsNullOrWhiteSpace($RunId)){[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')+'-'+$Scenario}else{$RunId}
$evidenceRoot=Assert-KmcChildPath (Join-Path $runtimeEvidence $actualRunId) $runtimeEvidence 'runtime evidence directory'
if(Test-Path -LiteralPath $evidenceRoot){throw "Runtime evidence ID already exists: $actualRunId"}
$startedAt=[DateTimeOffset]::UtcNow
$requestPath=Join-Path $evidenceRoot 'runtime-request.json'
$gameResultPath=Join-Path $evidenceRoot 'runtime-game-result.json'
$observerRequestPath=Join-Path $evidenceRoot 'observer-request.json'
$observerResultPath=Join-Path $evidenceRoot 'observer-result.json'
$finalResultPath=Join-Path $evidenceRoot 'runtime-result.json'
$manualReadyPath=Join-Path $evidenceRoot 'manual-review-ready.json'
$manualFailurePath=Join-Path $evidenceRoot 'manual-review-failure.json'
$manualResultPath=Join-Path $evidenceRoot 'manual-review-result.json'
if($isManualReview){$finalResultPath=$manualResultPath}
$orchestrationPath=Join-Path $evidenceRoot 'orchestration.json'
$lock=$null
$request=$null
$combinedStatePath=$null
$profileSnapshot=$null
$areasIsolationSelected=Test-KmcPersistenceAreasIsolationSelection -Scenario $Scenario -PersistenceCase $PersistenceCase -ObserveWorkingDeserialization ([bool]$ObserveWorkingDeserialization)
$modsTransactionStatePath=$null
$baseGameStaged=$false
$baseGameDependenciesUnchanged=$false
$baseGameNativeInventoryObserved=$false
$baseGameNativeInventorySha256=$null
$profileCacheChanges=@()
$process=$null
$launchIssued=$false
$processExited=$false
$modsRestored=$false
$saveProtection=$false
$baselineImmutable=$false
$workingRestored=$false
$saveWriteAllowlistPassed=$false
$restoredSaveInventoryDigest='0'*64
$gamePassed=$false
$validatedGameResult=$null
$gameResultHash=$null
$final=$null
$lockedWorkingPath=$null
$manualReady=$null
$manualReadyHash=$null
$manualReviewReady=$false
$manualFailureObserved=$false
$errors=New-Object 'System.Collections.Generic.List[string]'
New-Item -ItemType Directory -Path $evidenceRoot|Out-Null
try{
    $lock=Open-KmcRuntimeLock $runtimeState $actualRunId
    $request=[ordered]@{
        schemaVersion=$(if($isSaveBacked){2}else{1})
        runId=$actualRunId
        scenario=$Scenario
        branch=[string]$manifest.branch
        commit=[string]$manifest.commit
        productVersion=[string]$manifest.version
        dllSha256=[string]$manifest.dllSha256
        dllMvid=[string]$manifest.dllMvid
        transactionToken=[string]$lock.Token
        evidenceRoot=$evidenceRoot
    }
    if($ObserveWorkingDeserialization){$request['observeWorkingDeserialization']=$true}
    if(Test-KmcChunk7OrdinaryScenario $Scenario){$request['ordinaryAcquisition']=$ordinarySelectionInput.selection}
    if(Test-KmcChunk7SmallScenario $Scenario){$request['smallCalibration']=$smallSelectionInput.selection}
    if(Test-KmcChunk7SmallFixtureScenario $Scenario){$request['smallFixture']=$smallFixtureInput.selection}
    if(Test-KmcChunk7StockMammothFixtureScenario $Scenario){$request['stockMammothFixture']=$stockMammothFixtureInput.selection}
    if(Test-KmcChunk7FieldScenario $Scenario){$request['ordinaryField']=$fieldSelectionInput.selection}
    if(Test-KmcChunk7OwnedPairsScenario $Scenario){$request['ownedPairs']=$ownedPairsInput.selection}
    if(Test-KmcChunk7AssistedFixtureScenario $Scenario){$request['assistedFixture']=$assistedFixtureInput.selection}
    if(Test-KmcChunk7AcquiredManualScenario $Scenario){$request['acquiredManual']=$acquiredManualInput.selection}
    if(Test-KmcChunk7ManualDerivedScenario $Scenario){$request['manualDerived']=$manualDerivedInput.selection}
    if(Test-KmcChunk7CumulativeScenario $Scenario){$request['cumulativeAcquisition']=$cumulativeAcquisitionInput.selection}
    if(Test-KmcChunk7ManualSeedScenario $Scenario){$request['manualSeed']=$manualSeedInput.selection}
    if(Test-KmcChunk7BootstrapInventoryScenario $Scenario){$request['bootstrapInventory']=$bootstrapInventoryInput.selection}
    if(Test-KmcChunk7AssistedColdInventoryScenario $Scenario){$request['assistedColdInventory']=$assistedColdInput.selection}
    if(Test-KmcChunk7ProgressionScenario $Scenario){$request['progression']=$progressionSelectionInput.selection}
    if(Test-KmcChunk7NativeBootstrapScenario $Scenario){$request['nativeBootstrap']=[ordered]@{case=$NativeBootstrapCase;baseGameOnly=[bool]$BaseGameOnly;expectedNativeLoadCount=0;expectedNativeWriteCount=1;nativeWriteLeaf='Auto_1.zks'}}
    if($PSBoundParameters.ContainsKey('PersistenceCase')) { $request['persistenceCase']=$PersistenceCase }
    if($PersistenceCase-ceq'multi-exploration-three-area-transfers-manual'){
        $request['persistenceReplayTargets']=@(for($leg=0;$leg-lt3;$leg++){[ordered]@{enterPoint=$PersistenceReplayAreaEnterPoints[$leg];area=$PersistenceReplayAreaAreas[$leg];autoSaveMode='None'}})
    }
    if($PersistenceCase-ceq'multi-area-autosave'-or$PersistenceCase-cin @('area-cross-entry','area-cross-exit','area-cross-entry-auto','area-cross-exit-auto')){
        $request['persistenceAreaTarget']=[pscustomobject][ordered]@{
            enterPoint=$PersistenceAreaEnterPoint;area=$PersistenceAreaTargetArea
            autoSaveMode=if($PersistenceCase-ceq'multi-area-autosave'-or$PersistenceCase-cin @('area-cross-entry','area-cross-entry-auto')){'AfterEntry'}else{'BeforeExit'}
        }
    }
    if($isSaveBacked){
        $request['fixture']=$fixturePayload
        $request['qualificationSuite']=[ordered]@{suiteId=$ExpectedQualificationSuiteId;snapshotSha256=$ExpectedQualificationSuiteSnapshotSha256}
    }else{$request['saveAccessAllowed']=$false;$request['saveName']=$null}

    if($isSaveBacked){
        # Recovery can restore an interrupted transaction, but never confers
        # runtime admission. Re-prove the caller-pinned prior-to-current Working-
        # only transition under this lock before any durable run-state mutation.
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        $lockedContinuity=Assert-KmcQualificationSuiteContinuity `
            -SnapshotPath $QualificationSuiteSnapshotPath -StateRoot $runtimeState -SaveRoot $saveRoot -ModsRoot $liveMods `
            -QualificationPath $qualificationPath -PackagePath $PackagePath -PackageManifest $manifest `
            -ExpectedSuiteId $ExpectedQualificationSuiteId -ExpectedSnapshotSha256 $ExpectedQualificationSuiteSnapshotSha256
        $lockedPair=$lockedContinuity.pair
        $lockedWorkingPath=[IO.Path]::GetFullPath([string]$lockedPair.working.path)
        $lockedPayload=New-KmcRuntimeFixturePayload $lockedPair -ReadOnly:$isManualReview
        if(($lockedPayload|ConvertTo-Json -Depth 10 -Compress)-cne($fixturePayload|ConvertTo-Json -Depth 10 -Compress)){
            throw 'KMC fixture identity changed between preflight and locked transaction entry.'
        }
        Assert-KmcSaveMetadataInventoriesEqual `
            -Before $beforeSaves `
            -After $lockedContinuity.saveMetadata `
            -Description 'runtime locked fixture-continuity save metadata'
    }
    if(Test-KmcChunk7OrdinaryScenario $Scenario){
        $currentSelection=Read-KmcChunk7OrdinarySelectionFile $ordinarySelectionInput.path $actualRunId
        if($currentSelection.sha256-cne$ordinarySelectionInput.sha256){throw 'Explicit ordinary native selection changed before locked admission.'}
        $currentInventory=Get-KmcChunk7OrdinaryInventorySource $request.ordinaryAcquisition $fixturePayload $manifest
        if($currentInventory.sha256-cne$ordinaryInventorySource.sha256-or$currentInventory.path-cne$ordinaryInventorySource.path){throw 'Ordinary native source changed before locked admission.'}
        $ordinaryInventorySource=$currentInventory
    }
    if(Test-KmcChunk7SmallScenario $Scenario){
        $currentSmallSelection=Read-KmcChunk7SmallSelectionFile $smallSelectionInput.path $actualRunId
        if($currentSmallSelection.sha256-cne$smallSelectionInput.sha256){throw 'Small calibration selection changed before locked admission.'}
        $currentSmallSource=if(Test-KmcChunk7SmallManualCase $request.smallCalibration){Get-KmcChunk7SmallManualSource $request.smallCalibration.manualSource $fixturePayload $manifest -Assisted:(Test-KmcChunk7SmallAssistedCase $request.smallCalibration)}else{Get-KmcChunk7FieldSource $request.smallCalibration.source $fixturePayload $manifest}
        if((ConvertTo-KmcChunk7Canonical $currentSmallSource)-cne(ConvertTo-KmcChunk7Canonical $smallSource)){throw 'Small calibration source changed before locked admission.'}
        $smallSource=$currentSmallSource
    }
    if(Test-KmcChunk7AssistedFixtureScenario $Scenario){
        $currentAssistedInput=Read-KmcChunk7AssistedPreparationSelectionFile $assistedFixtureInput.path $actualRunId
        if($currentAssistedInput.sha256-cne$assistedFixtureInput.sha256){throw 'Assisted fixture selection changed before locked admission.'}
        Assert-KmcChunk7AssistedFixtureSelection $request
    }
    if(Test-KmcChunk7OwnedPairsScenario $Scenario){
        $currentOwnedInput=Read-KmcChunk7OwnedPairsSelectionFile $ownedPairsInput.path $actualRunId $PersistenceCase
        if($currentOwnedInput.sha256-cne$ownedPairsInput.sha256){throw 'Inherited two-owner selection changed before locked admission.'}
        $currentOwnedSource=Get-KmcChunk7OwnedPairsSource $request.ownedPairs $PersistenceCase $fixturePayload $manifest $(if($PersistenceCase-ceq'multi-area-autosave'){$request.persistenceAreaTarget}else{$null})
        if((ConvertTo-KmcChunk7Canonical $currentOwnedSource)-cne(ConvertTo-KmcChunk7Canonical $ownedPairsSource)){throw 'Inherited source lineage changed before locked admission.'}
        $ownedPairsSource=$currentOwnedSource
    }
    if(Test-KmcChunk7SmallFixtureScenario $Scenario){
        $currentSmallFixtureInput=Read-KmcChunk7SmallFixtureSelectionFile $smallFixtureInput.path $actualRunId
        if($currentSmallFixtureInput.sha256-cne$smallFixtureInput.sha256){throw 'F09 exact selection changed before locked admission.'}
        $currentSmallFixtureSource=Get-KmcChunk7SmallFixtureSource $request.smallFixture $fixturePayload $manifest
        if((ConvertTo-KmcChunk7Canonical $currentSmallFixtureSource)-cne(ConvertTo-KmcChunk7Canonical $smallFixtureSource)){throw 'F09 seed or historical provenance changed before locked admission.'}
        $smallFixtureSource=$currentSmallFixtureSource
    }
    if(Test-KmcChunk7StockMammothFixtureScenario $Scenario){
        $currentStockInput=Read-KmcChunk7StockMammothFixtureSelectionFile $stockMammothFixtureInput.path $actualRunId
        if($currentStockInput.sha256-cne$stockMammothFixtureInput.sha256){throw 'Stock Mammoth exact selection changed before locked admission.'}
        $currentStockSource=Get-KmcChunk7StockMammothFixtureSource $request.stockMammothFixture $fixturePayload $manifest
        if((ConvertTo-KmcChunk7Canonical $currentStockSource)-cne(ConvertTo-KmcChunk7Canonical $stockMammothFixtureSource)){throw 'Stock Mammoth seed or provenance changed before locked admission.'}
        $stockMammothFixtureSource=$currentStockSource
    }
    if(Test-KmcChunk7AcquiredManualScenario $Scenario){
        $currentAcquiredInput=Read-KmcChunk7AcquiredManualSelectionFile $acquiredManualInput.path $actualRunId
        if($currentAcquiredInput.sha256-cne$acquiredManualInput.sha256){throw 'Acquired Manual selection changed before locked admission.'}
        $currentAcquiredSource=Get-KmcChunk7AcquiredManualSource $request.acquiredManual $fixturePayload $manifest
        if((ConvertTo-KmcChunk7Canonical $currentAcquiredSource)-cne(ConvertTo-KmcChunk7Canonical $acquiredManualSource)){throw 'Acquired Manual source changed before locked admission.'}
        $acquiredManualSource=$currentAcquiredSource
    }
    if(Test-KmcChunk7ManualDerivedScenario $Scenario){
        $currentDerivedInput=Read-KmcChunk7ManualDerivedSelectionFile $manualDerivedInput.path $actualRunId
        if($currentDerivedInput.sha256-cne$manualDerivedInput.sha256){throw 'Manual-derived selection changed before locked admission.'}
        $currentDerivedSource=Get-KmcChunk7ManualDerivedSource $request.manualDerived $fixturePayload $manifest
        if((ConvertTo-KmcChunk7Canonical $currentDerivedSource)-cne(ConvertTo-KmcChunk7Canonical $manualDerivedSource)){throw 'Manual-derived source changed before locked admission.'}
        $manualDerivedSource=$currentDerivedSource
    }
    if(Test-KmcChunk7CumulativeScenario $Scenario){
        $currentCumulativeInput=Read-KmcChunk7CumulativeSelectionFile $cumulativeAcquisitionInput.path $actualRunId
        if($currentCumulativeInput.sha256-cne$cumulativeAcquisitionInput.sha256){throw 'Manual-derived selection changed before locked admission.'}
        $currentCumulativeSource=Get-KmcChunk7CumulativeSource $request.cumulativeAcquisition $fixturePayload $manifest
        if((ConvertTo-KmcChunk7Canonical $currentCumulativeSource)-cne(ConvertTo-KmcChunk7Canonical $cumulativeAcquisitionSource)){throw 'Manual-derived source changed before locked admission.'}
        $cumulativeAcquisitionSource=$currentCumulativeSource
    }
    if(Test-KmcChunk7ManualSeedScenario $Scenario){
        $currentManualInput=Read-KmcChunk7ManualSeedSelectionFile $manualSeedInput.path $actualRunId
        if($currentManualInput.sha256-cne$manualSeedInput.sha256){throw 'Manual seed selection changed before locked admission.'}
        $currentManualSource=Get-KmcChunk7ManualSeedSource $request.manualSeed $fixturePayload $manifest
        if((ConvertTo-KmcChunk7Canonical $currentManualSource)-cne(ConvertTo-KmcChunk7Canonical $manualSeedSource)){throw 'Manual seed source changed before locked admission.'}
        $manualSeedSource=$currentManualSource
    }
    if(Test-KmcChunk7AssistedColdInventoryScenario $Scenario){
        $currentAssistedColdInput=Read-KmcChunk7AssistedColdInventorySelectionFile $assistedColdInput.path $actualRunId
        if($currentAssistedColdInput.sha256-cne$assistedColdInput.sha256){throw 'Assisted cold selection changed before locked admission.'}
        $currentAssistedColdSource=Get-KmcChunk7AssistedColdInventorySource $request.assistedColdInventory $fixturePayload $manifest
        if((ConvertTo-KmcChunk7Canonical $currentAssistedColdSource)-cne(ConvertTo-KmcChunk7Canonical $assistedColdSource)){throw 'Assisted cold source changed before locked admission.'}
        $assistedColdSource=$currentAssistedColdSource
    }
    if(Test-KmcChunk7BootstrapInventoryScenario $Scenario){
        $currentBootstrapInput=Read-KmcChunk7BootstrapInventorySelectionFile $bootstrapInventoryInput.path $actualRunId
        if($currentBootstrapInput.sha256-cne$bootstrapInventoryInput.sha256){throw 'Bootstrap inventory selection changed before locked admission.'}
        $currentBootstrapSource=Get-KmcChunk7BootstrapInventorySource $request.bootstrapInventory $fixturePayload $manifest
        if((ConvertTo-KmcChunk7Canonical $currentBootstrapSource)-cne(ConvertTo-KmcChunk7Canonical $bootstrapInventorySource)){throw 'Bootstrap inventory source changed before locked admission.'}
        $bootstrapInventorySource=$currentBootstrapSource
    }
    if(Test-KmcChunk7FieldScenario $Scenario){
        $currentFieldSelection=Read-KmcChunk7FieldSelectionFile $fieldSelectionInput.path $actualRunId
        if($currentFieldSelection.sha256-cne$fieldSelectionInput.sha256){throw 'Field selection changed before locked admission.'}
        $currentFieldSource=Get-KmcChunk7FieldSource $request.ordinaryField $fixturePayload $manifest
        if((ConvertTo-KmcChunk7Canonical $currentFieldSource)-cne(ConvertTo-KmcChunk7Canonical $fieldSource)){throw 'Field source changed before locked admission.'}
        $fieldSource=$currentFieldSource
    }
    if(Test-KmcChunk7ProgressionScenario $Scenario){
        $currentProgressionSelection=Read-KmcChunk7ProgressionSelectionFile $progressionSelectionInput.path $actualRunId
        if($currentProgressionSelection.sha256-cne$progressionSelectionInput.sha256){throw 'Progression selection changed before locked admission.'}
        $currentProgressionSource=Get-KmcChunk7ProgressionSource $request.progression $fixturePayload $manifest
        if((ConvertTo-KmcChunk7Canonical $currentProgressionSource)-cne(ConvertTo-KmcChunk7Canonical $progressionSource)){throw 'Progression source/inventory changed before locked admission.'}
        $progressionSource=$currentProgressionSource
    }
    # Snapshot only after the under-lock fixture and save-metadata proof.
    if($BaseGameOnly-or$Scenario -cin @('chunk7-stock-mammoth-fixture-preparation','chunk7-small-fixture-preparation','chunk7-assisted-cold-inventory','chunk7-assisted-fixture-preparation','chunk7-owned-two-pair-campaign','chunk7-cumulative-acquisition','chunk7-acquired-manual-derivative','chunk7-manual-derived-acquisition','chunk7-bootstrap-manual-seed','chunk7-bootstrap-cold-inventory','chunk7-native-main-bootstrap','chunk7-native-progression','chunk7-small-profile-calibration','chunk7-ordinary-field-admission','chunk7-ordinary-acquisition','chunk7-persistence-save','chunk7-persistence-load','persistence-isolation','persistence-p07-save','persistence-p07-load','persistence-p01-save','persistence-p01-load','persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load','persistence-p04-save','persistence-p04-load','persistence-p05-save','persistence-p05-load','persistence-p06-load')){
        $profileSnapshot=New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $saveRoot -GameRoot ([string]$intake.requestedLayout.kingmakerInstallDir) -BackupRoot $runtimeBackups
        if($areasIsolationSelected){
            [void](Enter-KmcPersistenceAreasIsolation -Lock $lock -Snapshot $profileSnapshot -BackupRoot $runtimeBackups)
        }
    }
    # The no-save mode must bind NO suite argument, not an empty one: the
    # receiving parameters are ValidatePattern-guarded, so an explicitly passed
    # empty string is rejected before the function's own completeness check
    # ('exactly three for a suite mode, exactly none otherwise') can run. The
    # save-backed binding is unchanged and still always supplies all three.
    $suiteBinding=@{}
    if($isSaveBacked){
        $suiteBinding['QualificationSuiteSnapshotPath']=$QualificationSuiteSnapshotPath
        $suiteBinding['QualificationSuiteId']=$ExpectedQualificationSuiteId
        $suiteBinding['QualificationSuiteSnapshotSha256']=$ExpectedQualificationSuiteSnapshotSha256
    }
    $combinedStatePath=New-KmcRunTransactionState -Lock $lock -Mode $(if($isSaveBacked){'save-backed-v3-suite'}else{'no-save-v1'}) `
        -LiveModsRoot $liveMods -SaveRoot $saveRoot -StateRoot $runtimeState -ModsBefore $beforeMods -SavesBefore $beforeSaves `
        @suiteBinding
    if($isSaveBacked){
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        Assert-KmcSaveMetadataInventoriesEqual `
            -Before $beforeSaves `
            -After (Get-KmcSaveMetadataInventory $saveRoot) `
            -Description 'runtime immediate pre-save-transaction metadata'
        [void](Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $lockedPair -SaveRoot $saveRoot -StateRoot $runtimeState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging -Scenario $Scenario)
        if($Scenario -cin @('chunk7-stock-mammoth-fixture-preparation','chunk7-small-fixture-preparation','chunk7-assisted-cold-inventory','chunk7-assisted-fixture-preparation','chunk7-owned-two-pair-campaign','chunk7-cumulative-acquisition','chunk7-acquired-manual-derivative','chunk7-manual-derived-acquisition','chunk7-bootstrap-manual-seed','chunk7-bootstrap-cold-inventory','chunk7-native-main-bootstrap','chunk7-native-progression','chunk7-small-profile-calibration','chunk7-ordinary-field-admission','chunk7-ordinary-acquisition','chunk7-persistence-save','chunk7-persistence-load','persistence-isolation','persistence-p07-save','persistence-p07-load','persistence-p01-save','persistence-p01-load','persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load','persistence-p04-save','persistence-p04-load','persistence-p05-save','persistence-p05-load','persistence-p06-load')){
            $profileRoot=Assert-KmcChildPath (Join-Path $runtimeStaging ('persistence-'+$actualRunId)) $runtimeStaging 'owned persistence profile'
            if(Test-Path -LiteralPath $profileRoot){throw 'Persistence profile already exists; refusing ambiguous ownership.'}
            [void][IO.Directory]::CreateDirectory($profileRoot)
            $isolatedSaves=Join-Path $profileRoot 'Saved Games'
            [void][IO.Directory]::CreateDirectory($isolatedSaves)
            [void][IO.Directory]::CreateDirectory((Join-Path $profileRoot 'Areas'))
            $copySource=$lockedWorkingPath
            $copyDescriptor=$fixturePayload.working
            if(Test-KmcChunk7OwnedPairsScenario $Scenario){
                $copySource=$ownedPairsSource.path;$copyDescriptor=$ownedPairsSource.descriptor
                if($PersistenceCase-ceq'owned-schema1-resave-manual'){
                    # Mutation is confined to a fresh project-owned derivative,
                    # only after ShouldProcess and current lock qualification.
                    $ownedSchema1Derivative=New-KmcOwnedSchema1Input $ownedPairsSource $request.ownedPairs $fixturePayload $manifest $lock
                    $copySource=$ownedSchema1Derivative.path;$copyDescriptor=$ownedSchema1Derivative.descriptor
                    $request['schema1DerivationSha256']=$ownedSchema1Derivative.receiptSha256
                }elseif(Test-KmcOwnedSchema2ColdRequest ([pscustomobject]$request)){
                    $ownedSchema2Derivative=New-KmcOwnedSchema2Input $ownedPairsSource $request.ownedPairs $fixturePayload $manifest $lock
                    $copySource=$ownedSchema2Derivative.path;$copyDescriptor=$ownedSchema2Derivative.descriptor
                    $request['schema2DerivationSha256']=$ownedSchema2Derivative.receiptSha256
                }
                $request['persistenceLoad']=$copyDescriptor
                if(Test-KmcChunk7LoadRefusal ([pscustomobject]$request)){
                    $ownedLoadRefusalDerivative=New-KmcChunk7LoadRefusalInput $ownedPairsSource $request.ownedPairs $PersistenceCase $fixturePayload $manifest $lock
                    $request['persistenceAlternate']=$ownedLoadRefusalDerivative.descriptor
                    $request['loadRefusalDerivationSha256']=$ownedLoadRefusalDerivative.receiptSha256
                }
            }elseif(Test-KmcChunk7SmallScenario $Scenario){
                $copySource=$smallSource.path;$copyDescriptor=$smallSource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif(Test-KmcChunk7ProgressionScenario $Scenario){
                $copySource=$progressionSource.path;$copyDescriptor=$progressionSource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif(Test-KmcChunk7SmallFixtureScenario $Scenario){
                $copySource=$smallFixtureSource.path;$copyDescriptor=$smallFixtureSource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif(Test-KmcChunk7StockMammothFixtureScenario $Scenario){
                $copySource=$stockMammothFixtureSource.path;$copyDescriptor=$stockMammothFixtureSource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif(Test-KmcChunk7AcquiredManualScenario $Scenario){
                $copySource=$acquiredManualSource.path;$copyDescriptor=$acquiredManualSource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif(Test-KmcChunk7ManualDerivedScenario $Scenario){
                $copySource=$manualDerivedSource.path;$copyDescriptor=$manualDerivedSource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif(Test-KmcChunk7CumulativeScenario $Scenario){
                $copySource=$cumulativeAcquisitionSource.path;$copyDescriptor=$cumulativeAcquisitionSource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif(Test-KmcChunk7ManualSeedScenario $Scenario){
                $copySource=$manualSeedSource.path;$copyDescriptor=$manualSeedSource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif(Test-KmcChunk7AssistedColdInventoryScenario $Scenario){
                $copySource=$assistedColdSource.path;$copyDescriptor=$assistedColdSource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif(Test-KmcChunk7BootstrapInventoryScenario $Scenario){
                $copySource=$bootstrapInventorySource.path;$copyDescriptor=$bootstrapInventorySource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif(Test-KmcChunk7FieldScenario $Scenario){
                $copySource=$fieldSource.path;$copyDescriptor=$fieldSource.descriptor
                $request['persistenceLoad']=$copyDescriptor
            }elseif($Scenario -cin @('chunk7-persistence-load','persistence-p07-load','persistence-p01-load','persistence-p02-load','persistence-p03-load','persistence-p04-load','persistence-p05-load','persistence-p06-load')){
                $sourceCase=if($Scenario-ceq'persistence-p07-load'){$PersistenceCase}elseif($Scenario-ceq'persistence-p05-load'){if($PersistenceCase-ceq'manual-renamed'){'manual'}else{$PersistenceCase}}elseif($Scenario-ceq'persistence-p04-load'-or($Scenario-ceq'persistence-p03-load'-and$PersistenceCase-cin @('condition','condition-preparing','suspended'))){$PersistenceCase}else{$null}
                $source=if($Scenario-ceq'chunk7-persistence-load'-and$PersistenceCase-ceq'legacy-schema1-resave-manual'){
                    New-KmcChunk7Schema1Input $PersistenceSourceRunId $ExpectedPersistenceSourceSha256 $fixturePayload
                }elseif($Scenario-ceq'chunk7-persistence-load'){
                    Get-KmcChunk7PersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceSourceSha256 -Fixture $fixturePayload -Case $PersistenceCase -ExpectedDllSha256 $request.dllSha256 -ExpectedCommit $request.commit -AreaTarget $(if($PersistenceCase-ceq'multi-area-autosave'){$request.persistenceAreaTarget}else{$null})
                }elseif($Scenario-ceq'persistence-p06-load'){
                    Get-KmcPersistenceValidationSource $PersistenceSourceRunId $ExpectedPersistenceSourceSha256 $fixturePayload -Case $PersistenceCase
                }elseif($null-eq$sourceCase){Get-KmcPersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceSourceSha256 -Fixture $fixturePayload}
                elseif($sourceCase-cin @('area-cross-entry-auto','area-cross-exit-auto')){
                    # The transition autosave, selected by its producing run's
                    # own case and by the area that mode actually committed in:
                    # the destination for AfterEntry, the departure for BeforeExit.
                    $producing=if($sourceCase-ceq'area-cross-entry-auto'){'area-cross-entry'}else{'area-cross-exit'}
                    $committed=if($sourceCase-ceq'area-cross-entry-auto'){$PersistenceAreaTargetArea}else{[string]$fixturePayload.working.area}
                    Get-KmcPersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceSourceSha256 -Fixture $fixturePayload -NativeCase $producing -ArtifactRole transition-auto -ExpectedArea $committed
                }
                elseif($sourceCase-cin @('area-cross-entry','area-cross-exit')){Get-KmcPersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceSourceSha256 -Fixture $fixturePayload -NativeCase $sourceCase -ExpectedArea $PersistenceAreaTargetArea}
                elseif($sourceCase-ceq'mounted-charge-removal-no-dll'){
                    Get-KmcPersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceSourceSha256 -Fixture $fixturePayload -NativeCase mounted-charge-removal -ArtifactRole cleanup-manual
                }
                elseif($sourceCase-cin @('absent-kmc','removal-no-dll')){
                    # The cleanup archive a prepare-removal run wrote, under its
                    # own name, is what the integration-absent process opens.
                    Get-KmcPersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceSourceSha256 -Fixture $fixturePayload -NativeCase prepare-removal -ArtifactRole cleanup-manual
                }
                elseif($sourceCase-ceq'rider-size-change'){
                    # The no-pair archive an eligibility run wrote, under its own name.
                    Get-KmcPersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceSourceSha256 -Fixture $fixturePayload -NativeCase $sourceCase -ArtifactRole eligibility-manual
                }
                elseif($sourceCase-ceq'campaign-b'){
                    # Campaign B's own manual archive, under B's engine-minted identity.
                    Get-KmcPersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceSourceSha256 -Fixture $fixturePayload -NativeCase campaign-b -ArtifactRole campaign-b-manual
                }
                elseif($sourceCase-cin @('rider-death','mount-death')){
                    # The no-pair archive a death run wrote, under its own name.
                    Get-KmcPersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceSourceSha256 -Fixture $fixturePayload -NativeCase $sourceCase -ArtifactRole death-manual
                }
                else{Get-KmcPersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceSourceSha256 -Fixture $fixturePayload -NativeCase $sourceCase}
                $copySource=$source.path;$copyDescriptor=$source.descriptor
                # Copy the admitted immutable archive under one exact new leaf.
                # Only the destination descriptor changes; no archive/header rewrite.
                if($Scenario-ceq'persistence-p05-load'-and$PersistenceCase-ceq'manual-renamed'){
                    $copyDescriptor.fileName='Manual_811_KMC_RENAMED.zks'
                }
                if($Scenario-ceq'persistence-p06-load'){$copyDescriptor.fileName='Manual_300_KMC_P01.zks'}
                $request['persistenceLoad']=$copyDescriptor
                if($Scenario-ceq'chunk7-persistence-load'-and$PersistenceCase-ceq'legacy-schema1-resave-manual'){
                    $receiptPath=Join-Path $profileRoot 'validation-copy.json'
                    Copy-Item -LiteralPath $source.receiptPath -Destination $receiptPath
                    if((Get-KmcSha256 $receiptPath)-cne$source.receiptSha256){throw 'Schema-1 derivative provenance changed during staging.'}
                    $request['schema1DerivationSha256']=$source.receiptSha256
                }
                if($Scenario-ceq'persistence-p06-load'){
                    # Only the post-disposal failure case derives from the native
                    # area member; every other variant stays metadata-only.
                    $variant=if($PersistenceCase-ceq'failed-area-load'){
                        New-KmcPersistenceFailedAreaLoadCopy $PersistenceSourceRunId $ExpectedPersistenceSourceSha256 $fixturePayload
                    }else{
                        New-KmcPersistenceValidationCopy $PersistenceSourceRunId $ExpectedPersistenceSourceSha256 $fixturePayload -Case $PersistenceCase -ForeignSourceRunId $PersistenceForeignSourceRunId -ForeignSha256 $ExpectedPersistenceAlternateSha256
                    }
                    $secondPath=Join-Path $isolatedSaves $variant.descriptor.fileName
                    Copy-Item -LiteralPath $variant.path -Destination $secondPath
                    [IO.File]::SetLastWriteTimeUtc($secondPath,[IO.File]::GetLastWriteTimeUtc($variant.path))
                    if((Get-KmcSha256 $secondPath)-cne$variant.descriptor.sha256){throw 'Owned validation copy changed during intake.'}
                    $request['persistenceAlternate']=$variant.descriptor
                    Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $variant.path) 'owner.json') -Destination (Join-Path $profileRoot 'validation-copy.json')
                }
                if($Scenario-ceq'persistence-p05-load'-and$PersistenceCase-ceq'alternating'){
                    $alternate=Get-KmcPersistenceSource -SourceRunId $PersistenceSourceRunId -ExpectedSha256 $ExpectedPersistenceAlternateSha256 -Fixture $fixturePayload -NativeCase alternating -Alternate
                    $secondPath=Join-Path $isolatedSaves $alternate.descriptor.fileName
                    Copy-Item -LiteralPath $alternate.path -Destination $secondPath
                    [IO.File]::SetLastWriteTimeUtc($secondPath,[IO.File]::GetLastWriteTimeUtc($alternate.path))
                    if((Get-KmcSha256 $secondPath)-cne$alternate.descriptor.sha256){throw 'Second exact owned archive changed during copy.'}
                    $request['persistenceAlternate']=$alternate.descriptor
                }
            }elseif(-not [string]::IsNullOrEmpty($PersistenceSourceRunId)-or-not [string]::IsNullOrEmpty($ExpectedPersistenceSourceSha256)){
                throw 'Only the dedicated cold-load scenario may select an owned archive.'
            }
            $copiedFixture=Join-Path $isolatedSaves ([string]$copyDescriptor.fileName)
            Assert-KmcNotReparsePoint $copySource 'persistence fixture source'
            Assert-KmcNotHardLink $copySource 'persistence fixture source'
            Copy-Item -LiteralPath $copySource -Destination $copiedFixture
            # Copy-Item carries a read-only source attribute (the owner-seed master is
            # read-only) and SetLastWriteTimeUtc is then denied; the fresh run-owned copy
            # is an ordinary file. Only the copy's attribute changes; its bytes are
            # hash-verified below and the source is never modified.
            $copiedInfo=Get-Item -LiteralPath $copiedFixture
            if($copiedInfo.IsReadOnly){$copiedInfo.IsReadOnly=$false}
            [IO.File]::SetLastWriteTimeUtc($copiedFixture,([IO.File]::GetLastWriteTimeUtc($copySource)))
            if((Get-KmcSha256 $copiedFixture)-cne[string]$copyDescriptor.sha256){
                throw 'Copied persistence fixture bytes differ from the admitted Working fixture.'
            }
            Write-KmcJsonAtomic (Join-Path $profileRoot 'owner.json') ([ordered]@{
                runId=$actualRunId;transactionToken=[string]$lock.Token;scenario=$Scenario;persistenceCase=$PersistenceCase
                sourceSha256=[string]$copyDescriptor.sha256;saveRoot=$isolatedSaves
            })
            if(Test-KmcChunk7SmallScenario $Scenario){if(Test-KmcChunk7SmallManualCase $request.smallCalibration){Copy-KmcChunk7SmallManualSourceArtifact $smallSource $profileRoot $lock}else{Copy-KmcChunk7FieldSourceArtifact $smallSource $profileRoot $lock}}
            if(Test-KmcChunk7ProgressionScenario $Scenario){Copy-KmcChunk7ProgressionInputs $progressionSource $profileRoot $lock}
            if(Test-KmcChunk7FieldScenario $Scenario){Copy-KmcChunk7FieldSourceArtifact $fieldSource $profileRoot $lock}
            if(Test-KmcChunk7OwnedPairsScenario $Scenario){
                if($PersistenceCase-ceq'owned-schema1-resave-manual'){Copy-KmcOwnedSchema1Inputs $ownedPairsSource $ownedSchema1Derivative $profileRoot $lock $request}
                elseif(Test-KmcOwnedSchema2ColdRequest ([pscustomobject]$request)){Copy-KmcOwnedSchema2Inputs $ownedPairsSource $ownedSchema2Derivative $profileRoot $lock $request}
                else{Copy-KmcChunk7OwnedPairsSourceArtifact $ownedPairsSource $profileRoot $lock}
                if(Test-KmcChunk7OwnerSeedCurrentCheckpoint $PersistenceCase){Copy-KmcChunk7OwnerSeedCurrentParent $ownedPairsSource $profileRoot $lock ([pscustomobject]$request)}
                if(Test-KmcChunk7LoadRefusal ([pscustomobject]$request)){Copy-KmcChunk7LoadRefusalInputs $ownedLoadRefusalDerivative $profileRoot $lock $request}
            }
            if(Test-KmcChunk7SmallFixtureScenario $Scenario){[void](Copy-KmcChunk7SmallFixtureSource $smallFixtureSource $profileRoot $lock $request.smallFixture $fixturePayload $manifest)}
            if(Test-KmcChunk7StockMammothFixtureScenario $Scenario){[void](Copy-KmcChunk7StockMammothFixtureSource $stockMammothFixtureSource $profileRoot $lock $request.stockMammothFixture $fixturePayload $manifest)}
            if(Test-KmcChunk7AcquiredManualScenario $Scenario){Copy-KmcChunk7AcquiredManualSourceArtifact $acquiredManualSource $profileRoot $lock}
            if(Test-KmcChunk7ManualDerivedScenario $Scenario){Copy-KmcChunk7ManualDerivedSourceArtifact $manualDerivedSource $profileRoot $lock}
            if(Test-KmcChunk7CumulativeScenario $Scenario){if(Test-KmcAssistedCumulativeMode $request.cumulativeAcquisition){Copy-KmcAssistedCumulativeSourceArtifacts $cumulativeAcquisitionSource $profileRoot $lock $request.cumulativeAcquisition $fixturePayload $manifest}else{Copy-KmcChunk7CumulativeSourceArtifact $cumulativeAcquisitionSource $profileRoot $lock}}
            if(Test-KmcChunk7ManualSeedScenario $Scenario){Copy-KmcChunk7ManualSeedSourceArtifacts $manualSeedSource $profileRoot $lock}
            if(Test-KmcChunk7BootstrapInventoryScenario $Scenario){Copy-KmcChunk7BootstrapInventorySourceArtifact $bootstrapInventorySource $profileRoot $lock}
            if(Test-KmcChunk7AssistedColdInventoryScenario $Scenario){[void](Copy-KmcChunk7AssistedColdInventorySourceArtifact $assistedColdSource $profileRoot $lock $request.assistedColdInventory $fixturePayload $manifest)}
            if(Test-KmcChunk7OrdinaryScenario $Scenario){
                [void](Copy-KmcChunk7OrdinaryInventory $ordinaryInventorySource $profileRoot $lock)
            }
        }

    }
    $stageArguments=@{}
    if($BaseGameOnly){
        Assert-KmcBaseGameDependencyReceipt $baseGameDependencies -VerifyFiles
        $stageArguments['BaseGameDependencies']=$baseGameDependencies
    }
    $modsTransactionStatePath=Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $liveMods -PackagePath $(if($isObserver){$ObserverPackagePath}else{$PackagePath}) -StateRoot $runtimeState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging -StagingMode $(if($BaseGameOnly){'base-game-only-kmc'}elseif($isObserver){'live-clone-minus-kmc-plus-observer'}else{'live-clone-plus-kmc-overlay'}) @stageArguments
    if($BaseGameOnly){
        $baseGameStaged=$true
        Write-KmcJsonAtomic (Join-Path $evidenceRoot 'base-game-isolation-admission.json') ([ordered]@{schemaVersion=1;runId=$actualRunId;
            transactionToken=[string]$lock.Token;stagingMode='base-game-only-kmc';dependencies=$baseGameDependencies;
            transactionStatePath=$modsTransactionStatePath;stagedStateSha256=(Get-KmcSha256 $modsTransactionStatePath);
            nativeInventoryObserved=$false;nativeCompatibilityQualified=$false})
    }
    Write-KmcJsonAtomic $requestPath $request
    & (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeRequest.ps1') -RequestPath $requestPath -PackageManifestPath $packageManifestPath
    # The serialized file is the validated request shape; re-read it so every later reader receives the
    # same object tree the validators checked instead of the ordered dictionary used to build it.
    $request=Read-KmcJson $requestPath
    if($isObserver){
        # What the observer process is bound to: the owned profile, the live Mods
        # root to prove empty of KMC, the prepared cleanup archive, and KMC's own
        # registered blueprint and Harmony identities to look for and not find.
        $observerRequest=[ordered]@{
            schemaVersion=1;runId=$actualRunId;transactionToken=[string]$lock.Token;evidenceRoot=$evidenceRoot
            profileRoot=$profileRoot;modsRoot=$liveMods;kmcModId='KingmakerMountedCombat';observerModId='KmcRemovalObserver'
            kmcHarmonyIds=@('KingmakerMountedCombat.Feasibility','KingmakerMountedCombat.PersistenceIsolation','KingmakerMountedCombat.Diagnostics.QueuedMountWindow','KingmakerMountedCombat.Diagnostics.ActorAllocation','KingmakerMountedCombat.Diagnostics.OrdinaryAttackTrace')
            archive=$request.persistenceLoad
            kmcBlueprintGuids=@('4016c7db400ab721ff125aef9e65e202','7db7c50677e39f09feef56f3831fc723','98e651899e6278d938de77af1d69bd32','6874a165bf8bda3531ee4e2abc10c899','f053faad986631688defa003cd7bda0e','3af2b81f4d72bbb30501fa730fcdf36e','27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e','d79eaec224a7a832e738eb81baef9d49')
            mammothBlueprintGuid='e7aa96d15a45238438ae4cfb476f6bb9'
            candidate=[ordered]@{commit=[string]$manifest.commit;productVersion=[string]$manifest.version;dllSha256=[string]$manifest.dllSha256;dllMvid=[string]$manifest.dllMvid}
            observer=[ordered]@{version=[string]$observerManifest.version;packageSha256=[string]$observerManifest.packageSha256;dllSha256=[string]$observerManifest.dllSha256;dllMvid=[string]$observerManifest.dllMvid}
            timeoutSeconds=300
        }
        if($PersistenceCase-ceq'mounted-charge-removal-no-dll'){
            $observerRequest.schemaVersion=2
            $observerRequest.chargeSource=Get-KmcChargeRemovalSourceProof $PersistenceSourceRunId $ExpectedPersistenceSourceSha256 $request.commit $request.dllSha256
        }
        Write-KmcJsonAtomic $observerRequestPath $observerRequest
    }
    $orchestration=[ordered]@{
        schemaVersion=2;runId=$actualRunId;scenario=$Scenario;status='IN PROGRESS';stage='transactions-staged';
        startedAtUtc=$startedAt.ToString('o');steamSafety=$steamSafety;combinedTransactionState=$combinedStatePath;
        protectedSaveDigestBefore=$beforeSaves.digest;liveModsDigestBefore=$beforeMods.digest;saveBacked=$isSaveBacked
    }
    Write-KmcJsonAtomic $orchestrationPath $orchestration
    Assert-KmcNoGameProcesses
    $requestHash=Get-KmcSha256 $requestPath
    $arguments=if($isObserver){
        # No KMC arguments at all: even a stray KMC assembly would stay inert.
        @('-applaunch','640820','-kmcObserverRequest',('"'+$observerRequestPath+'"'),'-kmcObserverToken',[string]$lock.Token,'-kmcObserverRequestSha256',(Get-KmcSha256 $observerRequestPath))
    }else{@('-applaunch','640820','-kmcRuntimeRequest',('"'+$requestPath+'"'),'-kmcRuntimeToken',[string]$lock.Token,'-kmcRuntimeRequestSha256',$requestHash)}
    [void](Start-Process -FilePath $SteamPath -ArgumentList $arguments -WindowStyle Hidden -PassThru)
    $launchIssued=$true
    $launchDeadline=[DateTimeOffset]::UtcNow.AddSeconds(60)
    while([DateTimeOffset]::UtcNow-lt$launchDeadline-and$null-eq$process){
        if(@(Get-KmcSuspiciousWindows).Count-ne0){throw 'Unexpected Steam/account UI appeared during launch.'}
        $new=@(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
        if($new.Count-gt1){throw 'More than one Kingmaker process appeared.'}
        if($new.Count-eq1){$process=$new[0];break}
        Start-Sleep -Milliseconds 250
    }
    if($null-eq$process){throw 'Steam launch was issued but no uniquely attributable Kingmaker process appeared; restoration is intentionally blocked pending recovery.'}
    $capturedProcessPath=$null
    $capturedProcessStartedAtUtc=$null
    while([DateTimeOffset]::UtcNow-lt$launchDeadline-and[string]::IsNullOrWhiteSpace($capturedProcessPath)){
        $process.Refresh()
        if($process.HasExited){throw 'Captured Kingmaker process exited before its identity metadata became available.'}
        try{
            $candidateProcessPath=[string]$process.Path
            $candidateProcessStartedAtUtc=$process.StartTime.ToUniversalTime()
            if(-not[string]::IsNullOrWhiteSpace($candidateProcessPath)){
                $capturedProcessPath=$candidateProcessPath
                $capturedProcessStartedAtUtc=$candidateProcessStartedAtUtc
                break
            }
        }catch{}
        Start-Sleep -Milliseconds 100
    }
    if([string]::IsNullOrWhiteSpace($capturedProcessPath)-or$null-eq$capturedProcessStartedAtUtc){
        throw 'Captured Kingmaker process identity metadata did not become available within the existing launch deadline.'
    }
    if(-not[string]::Equals($capturedProcessPath,[IO.Path]::GetFullPath($gameExecutable),[StringComparison]::OrdinalIgnoreCase)-or
        (Get-KmcSha256 $capturedProcessPath)-cne$expectedGameExecutableHash-or
        $capturedProcessStartedAtUtc-lt$startedAt.UtcDateTime.AddSeconds(-5)){
        throw 'Captured Kingmaker process identity/path/hash/start time is unexpected.'
    }
    $orchestration.stage=if($isManualReview){'waiting-for-manual-review-ready'}else{'waiting-for-game-result'}
    $orchestration['kingmakerProcessId']=$process.Id
    Write-KmcJsonAtomic $orchestrationPath $orchestration
    $deadline=[DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    if($isManualReview){
        while(-not(Test-Path -LiteralPath $manualReadyPath -PathType Leaf)){
            if(Test-Path -LiteralPath $manualFailurePath -PathType Leaf){
                $failure=Read-KmcJson $manualFailurePath
                throw 'Kingmaker rejected the manual review before READY: '+[string]$failure.reason
            }
            $process.Refresh()
            if($process.HasExited){$processExited=$true;throw 'Kingmaker exited before committing manual-review READY evidence.'}
            $all=@(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
            if($all.Count-ne1-or$all[0].Id-ne$process.Id){throw 'Kingmaker process attribution changed before manual-review READY.'}
            if(@(Get-KmcSuspiciousWindows).Count-ne0){throw 'Unexpected Steam/account UI appeared before manual-review READY.'}
            if([DateTimeOffset]::UtcNow-ge$deadline){throw 'Manual-review READY timed out; Kingmaker is intentionally left running and restoration is blocked.'}
            Start-Sleep -Milliseconds 250
        }
        & (Join-Path $repoRoot 'scripts\runtime\Test-KmcManualReviewReady.ps1') `
            -ReadyPath $manualReadyPath -RequestPath $requestPath -PackageManifestPath $packageManifestPath `
            -ExpectedProcessId $process.Id -NotBeforeUtc $startedAt
        $manualReady=Read-KmcJson $manualReadyPath
        $manualReadyHash=Get-KmcSha256 $manualReadyPath
        $manualReviewReady=$true
        $gamePassed=$true
        $orchestration.stage='manual-review-ready'
        $orchestration|Add-Member manualReviewReadySha256 $manualReadyHash -Force
        Write-KmcJsonAtomic $orchestrationPath $orchestration
        Write-Host 'KMC MANUAL VISUAL REVIEW READY.'
        Write-Host 'Review only presentation, selection, UI, camera, movement, and mount/dismount behavior. Do not save, load, enter combat, change area, or interrupt this launcher.'
        Write-Host 'Exit Kingmaker normally when review is complete; this launcher will then restore Working and Mods exactly.'
        $suspiciousReported=$false
        $attributionReported=$false
        while(-not$processExited){
            if(-not$manualFailureObserved-and(Test-Path -LiteralPath $manualFailurePath -PathType Leaf)){
                $failure=Read-KmcJson $manualFailurePath
                Assert-KmcExactProperties $failure @('schemaVersion','evidenceKind','runId','scenario','status','transactionToken','failedAtUtc','processId','reason') 'manual review failure evidence'
                if([int]$failure.schemaVersion-ne1-or[string]$failure.evidenceKind-cne'manual-visual-review-failure'-or
                    [string]$failure.runId-cne$actualRunId-or[string]$failure.scenario-cne$Scenario-or
                    [string]$failure.status-cne'FAIL'-or[string]$failure.transactionToken-cne[string]$lock.Token-or
                    [int]$failure.processId-ne$process.Id-or[string]::IsNullOrWhiteSpace([string]$failure.reason)){
                    $errors.Add('Manual review failure evidence identity is invalid.')
                }else{$errors.Add('Manual review failed closed: '+[string]$failure.reason)}
                $manualFailureObserved=$true
                $gamePassed=$false
            }
            $process.Refresh()
            if($process.HasExited){$processExited=$true;break}
            $all=@(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
            if(-not$attributionReported-and($all.Count-ne1-or$all[0].Id-ne$process.Id)){
                $errors.Add('Kingmaker process attribution changed during manual review; no automated action was taken while a game process remained open.')
                $attributionReported=$true
                $gamePassed=$false
                Write-Warning 'Kingmaker process attribution changed. Close every Kingmaker process normally so guarded restoration can proceed.'
            }
            if(-not$suspiciousReported-and@(Get-KmcSuspiciousWindows).Count-ne0){
                $errors.Add('Unexpected Steam/account UI appeared during manual review; no further automated action was taken while Kingmaker remained open.')
                $suspiciousReported=$true
                $gamePassed=$false
                Write-Warning 'Unexpected Steam/account UI observed. Close Kingmaker normally so guarded restoration can proceed.'
            }
            Start-Sleep -Milliseconds 250
        }
    }
    else{
        $awaitedResultPath=if($isObserver){$observerResultPath}else{$gameResultPath}
        while(-not(Test-Path -LiteralPath $awaitedResultPath -PathType Leaf)){
            $process.Refresh()
            if($process.HasExited){$processExited=$true;throw 'Kingmaker exited before committing its atomic game result.'}
            $all=@(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
            if($all.Count-ne1-or$all[0].Id-ne$process.Id){throw 'Kingmaker process attribution changed during the run.'}
            if(@(Get-KmcSuspiciousWindows).Count-ne0){throw 'Unexpected Steam/account UI appeared during the run.'}
            if([DateTimeOffset]::UtcNow-ge$deadline){throw 'Runtime game result timed out; Kingmaker is intentionally left running and restoration is blocked.'}
            Start-Sleep -Milliseconds 250
        }
        if($isObserver){
            # The observer's own result is validated against the request, the
            # observer binding and the archive bytes on disk, then composed into
            # the run's game-result record; KMC wrote nothing in this process.
            & (Join-Path $repoRoot 'scripts\runtime\Test-KmcObserverResult.ps1') -ObserverResultPath $observerResultPath -RequestPath $requestPath -ObserverRequestPath $observerRequestPath -GameResultPath $gameResultPath -ExpectedProcessId $process.Id -NotBeforeUtc $startedAt -SourceArchivePath $copySource
            $gameResultHash=Get-KmcSha256 $gameResultPath
        }else{
            $candidateHash=Get-KmcSha256 $gameResultPath
            $gameResultHash=$candidateHash
            & (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeGameResult.ps1') -GameResultPath $gameResultPath -RequestPath $requestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $process.Id -NotBeforeUtc $startedAt -VerifyLiveWorkingIdentity -ExpectedLiveWorkingPath $lockedWorkingPath
        }
        $validatedGameResult=Read-KmcJson $gameResultPath
        $gamePassed=[string]$validatedGameResult.status -ceq 'PASS'
        if($BaseGameOnly){
            $inventoryManifestPath=Join-Path $evidenceRoot 'runtime-artifacts.json'
            if($isSaveBacked-and(Get-KmcSha256 $inventoryManifestPath)-cne$validatedGameResult.evidenceManifestSha256){throw 'Base-game inventory manifest is not the native result manifest.'}
            $inventoryManifest=Read-KmcJson $inventoryManifestPath
            $inventory=Read-KmcChunk7ManifestArtifact $request $inventoryManifest 'runtime-loader-inventory.json' 'runtime-loader-inventory'
            if($inventory.observations.processId-ne$process.Id){throw 'Loader inventory came from a different native process.'}
            Assert-KmcBaseGameNativeInventory $request $inventory $baseGameDependencies
            $baseGameNativeInventoryObserved=$true
            $baseGameNativeInventorySha256=Get-KmcSha256 (Join-Path $evidenceRoot 'runtime-loader-inventory.json')
        }
        if($gamePassed-and(Test-KmcChunk7SmallScenario $Scenario)){
            if(Test-KmcChunk7SmallManualCase $request.smallCalibration){
                $verifiedSmallSource=Get-KmcChunk7SmallManualSource $request.smallCalibration.manualSource $fixturePayload $manifest -Assisted:(Test-KmcChunk7SmallAssistedCase $request.smallCalibration)
                if((ConvertTo-KmcChunk7Canonical $verifiedSmallSource)-cne(ConvertTo-KmcChunk7Canonical $smallSource)-or
                   (Get-KmcSha256 (Join-Path $isolatedSaves $request.persistenceLoad.fileName))-cne$request.smallCalibration.manualSource.sourceArchiveSha256){throw 'Read-only Small Manual source changed during the native process.'}
            }else{
                $verifiedSmallSource=Get-KmcChunk7FieldSource $request.smallCalibration.source $fixturePayload $manifest
                if((ConvertTo-KmcChunk7Canonical $verifiedSmallSource)-cne(ConvertTo-KmcChunk7Canonical $smallSource)-or
                   (Get-KmcSha256 (Join-Path $isolatedSaves 'Auto_1.zks'))-cne$request.smallCalibration.source.sourceArchiveSha256){throw 'Read-only Small calibration source changed during the native process.'}
            }
            $sourceArtifact=Read-KmcJson $smallSource.artifactPath
            $smallArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-small-calibration-evidence.json')
            if($sourceArtifact.observations.processId-eq$smallArtifact.observations.processId){throw 'Small calibration reused the acquisition process rather than cold loading.'}
            if((Test-KmcChunk7SmallManualCase $request.smallCalibration)-and(ConvertTo-KmcChunk7Canonical $sourceArtifact.observations)-cne(ConvertTo-KmcChunk7Canonical $smallArtifact.observations.source.acquiredManual)){throw 'Small Manual runtime source witness differs from the completely qualified artifact.'}
        }
        if(Test-KmcChunk7AssistedFixtureScenario $Scenario){
            $verifiedAssistedInput=Read-KmcChunk7AssistedPreparationSelectionFile $assistedFixtureInput.path $actualRunId
            if($verifiedAssistedInput.sha256-cne$assistedFixtureInput.sha256){throw 'Assisted fixture selection changed during the native process.'}
            if($gamePassed){
                $assistedArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-assisted-fixture-evidence.json')
                Assert-KmcChunk7AssistedFixtureOutput $request $assistedArtifact
                Assert-KmcChunk7AssistedFixtureArchive $request $assistedArtifact
            }
        }
        if($gamePassed-and(Test-KmcChunk7NativeBootstrapScenario $Scenario)){
            $bootstrapArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-native-bootstrap-evidence.json')
            Assert-KmcChunk7NativeBootstrapOutput $request $bootstrapArtifact
        }
        if($gamePassed-and(Test-KmcChunk7ProgressionScenario $Scenario)){
            $verifiedProgressionSource=Get-KmcChunk7ProgressionSource $request.progression $fixturePayload $manifest
            if((ConvertTo-KmcChunk7Canonical $verifiedProgressionSource)-cne(ConvertTo-KmcChunk7Canonical $progressionSource)-or
               (Get-KmcSha256 (Join-Path $isolatedSaves (Get-KmcChunk7ProgressionInputLeaf $request)))-cne$request.progression.sourceArchiveSha256){throw 'Native progression source/inventory changed during its cold process.'}
            $progressionArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-progression-evidence.json')
            if($progressionArtifact.observations.processId-eq$progressionSource.sourceProcessId){throw 'Progression did not cold-load its declared native source.'}
            Assert-KmcChunk7ProgressionNativeArchive $request $progressionArtifact
        }
        if($gamePassed-and(Test-KmcChunk7OwnedPairsScenario $Scenario)){
            $verifiedOwnedSource=Get-KmcChunk7OwnedPairsSource $request.ownedPairs $PersistenceCase $fixturePayload $manifest $(if($PersistenceCase-ceq'multi-area-autosave'){$request.persistenceAreaTarget}else{$null})
            if((ConvertTo-KmcChunk7Canonical $verifiedOwnedSource)-cne(ConvertTo-KmcChunk7Canonical $ownedPairsSource)){throw 'Inherited two-owner source lineage changed during native execution.'}
            $seedManualRun=Test-KmcChunk7OwnedPairsSeedManual ([pscustomobject]$request)
            if((Test-KmcChunk7OwnedPairsOwnerSeed ([pscustomobject]$request))-or$seedManualRun){
                # Owner seed (exploration or Manual producer): the placed record is the honest seed-source record and the seed copy is never written.
                $seedRecord=Read-KmcJson (Join-Path $profileRoot 'owned-two-pair-source.json')
                if($seedRecord.kind-cne'kmc-owner-seed-source'-or$seedRecord.seedId-cne$request.ownedPairs.seedId-or$seedRecord.registrySha256-cne$verifiedOwnedSource.registrySha256-or
                    $seedRecord.producerAncestry-ne$false-or$seedRecord.nativeAuthorityGranted-ne$false-or
                    (Get-KmcSha256 (Join-Path $isolatedSaves $request.persistenceLoad.fileName))-cne$request.ownedPairs.seedSha256){throw 'Owner-seed source record or immutable native copy changed during execution.'}
            }elseif((Get-KmcSha256 (Join-Path $profileRoot 'owned-two-pair-source.json'))-cne$request.ownedPairs.sourceArtifactSha256-or
                (Get-KmcSha256 (Join-Path $isolatedSaves $request.persistenceLoad.fileName))-cne$request.persistenceLoad.sha256){throw 'Inherited source receipt or immutable native input changed during execution.'}
            if($request.ownedPairs.case-cin@('persistence-load','owner-seed-checkpoint-load')-and$PersistenceCase-ceq'multi-area-autosave'){
                $ownedColdArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-persistence-evidence.json')
                Assert-KmcChunk7OwnedAreaColdSource $verifiedOwnedSource $request $ownedColdArtifact
            }
            if(Test-KmcChunk7OwnedPairsSeedDerivedLoad ([pscustomobject]$request)){
                # Separate-process cold proof against the exact pinned producer receipt.
                $derivedColdArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-persistence-evidence.json')
                if((Get-KmcSha256 $verifiedOwnedSource.artifactPath)-cne$request.ownedPairs.sourceArtifactSha256){throw 'Owner-seed-derived producer receipt changed during the cold process.'}
                if(Test-KmcChunk7ManticoreManualCold ([pscustomobject]$request)){
                    Assert-KmcChunk7ManticoreManualColdSource ([pscustomobject]$request) (Read-KmcJson $verifiedOwnedSource.artifactPath) $derivedColdArtifact
                }else{
                    Assert-KmcChunk7OwnerSeedDerivedColdSource ([pscustomobject]$request) (Read-KmcJson $verifiedOwnedSource.artifactPath) $derivedColdArtifact
                }
            }
            if(Test-KmcChunk7OwnedPairsSeedCheckpointLoad ([pscustomobject]$request)){
                # Separate-process cold proof against the checkpoint producer's own recorded read-back.
                $checkpointColdArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-persistence-evidence.json')
                if((Get-KmcSha256 $verifiedOwnedSource.checkpointArtifactPath)-cne$request.ownedPairs.checkpointSourceArtifactSha256){throw 'Owner-seed checkpoint producer receipt changed during the cold process.'}
                Assert-KmcChunk7OwnerSeedCheckpointColdSource ([pscustomobject]$request) (Read-KmcJson $verifiedOwnedSource.checkpointArtifactPath) $checkpointColdArtifact
            }
            $nativeLeaves=@(Get-ChildItem -LiteralPath $isolatedSaves -Filter '*.zks' -File|ForEach-Object Name)
            $writes=Get-KmcChunk7OwnedWriteCount $request
            if(Test-KmcChunk7OwnedPairsOwnerSeed ([pscustomobject]$request)){
                # Owner seed: zero writes (the native game has no level-up autosave), so only the seed copy remains: no Auto_1 or any other entry.
                $allLeaves=@(Get-ChildItem -LiteralPath $isolatedSaves -Force|ForEach-Object Name)
                Assert-KmcChunk7OwnerSeedFinalSaveRoot ([pscustomobject]$request) $allLeaves
            }elseif($writes-eq0){Assert-KmcChunk7OwnedFinalSaveRoot ([pscustomobject]$request) @(Get-ChildItem -LiteralPath $isolatedSaves -Force|ForEach-Object Name) $null}
            if($writes-gt0-and-not(Test-KmcChunk7OwnedPairsOwnerSeed ([pscustomobject]$request))){
                # The owner-seed Manual producer's receipt is its world evidence.
                $ownedArtifact=Read-KmcJson (Join-Path $evidenceRoot $(if($seedManualRun){'chunk7-world-evidence.json'}else{'chunk7-persistence-evidence.json'}))
                if($PersistenceCase-ceq'owned-schema1-resave-manual'){Assert-KmcOwnedSchema1ColdSource $request $verifiedOwnedSource $ownedArtifact}
                if(Test-KmcOwnedSchema2ColdRequest ([pscustomobject]$request)){Assert-KmcOwnedSchema2ColdSource $request $verifiedOwnedSource $ownedArtifact}
                Assert-KmcChunk7OwnedOutputEvidence ([pscustomobject]$request) $ownedArtifact.observations
                Assert-KmcChunk7OwnedFinalSaveRoot ([pscustomobject]$request) @(Get-ChildItem -LiteralPath $isolatedSaves -Force|ForEach-Object Name) $ownedArtifact.observations
                $outputLeaf=Get-KmcChunk7OwnedOutputLeaf ([pscustomobject]$request) $ownedArtifact.observations
                if($outputLeaf-cnotin$nativeLeaves-or(Get-KmcSha256 (Join-Path $isolatedSaves $outputLeaf))-cne$ownedArtifact.observations.nativeWrite.sha256){throw 'Inherited fresh native output differs from its observed descriptor/hash.'}
                if($seedManualRun){Assert-KmcChunk7OwnerSeedManualFinalSaveRoot ([pscustomobject]$request) @(Get-ChildItem -LiteralPath $isolatedSaves -Force|ForEach-Object Name) $outputLeaf}
            }
        }
        if($gamePassed-and(Test-KmcChunk7SmallFixtureScenario $Scenario)){
            $verifiedSmallFixtureSource=Get-KmcChunk7SmallFixtureSource $request.smallFixture $fixturePayload $manifest
            if((ConvertTo-KmcChunk7Canonical $verifiedSmallFixtureSource)-cne(ConvertTo-KmcChunk7Canonical $smallFixtureSource)){throw 'F09 immutable seed/provenance changed during the native process.'}
            $smallFixtureArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-small-fixture-evidence.json')
            Assert-KmcChunk7SmallFixtureOutput $request $smallFixtureArtifact
        }
        if($gamePassed-and(Test-KmcChunk7StockMammothFixtureScenario $Scenario)){
            $verifiedStockSource=Get-KmcChunk7StockMammothFixtureSource $request.stockMammothFixture $fixturePayload $manifest
            if((ConvertTo-KmcChunk7Canonical $verifiedStockSource)-cne(ConvertTo-KmcChunk7Canonical $stockMammothFixtureSource)){throw 'Stock Mammoth immutable seed/provenance changed during the native process.'}
            $stockArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-stock-mammoth-fixture-evidence.json')
            Assert-KmcChunk7StockMammothFixtureOutput $request $stockArtifact
        }
        if($gamePassed-and(Test-KmcChunk7AcquiredManualScenario $Scenario)){
            $verifiedAcquiredSource=Get-KmcChunk7AcquiredManualSource $request.acquiredManual $fixturePayload $manifest
            if((ConvertTo-KmcChunk7Canonical $verifiedAcquiredSource)-cne(ConvertTo-KmcChunk7Canonical $acquiredManualSource)){throw 'Acquired Manual source lineage changed during the native process.'}
            $sourceArtifact=Read-KmcJson $verifiedAcquiredSource.artifactPath
            $acquiredArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-acquired-manual-evidence.json')
            Assert-KmcChunk7AcquiredManualEvidence $request $acquiredArtifact $sourceArtifact 'PASS'
            Assert-KmcChunk7AcquiredManualOutput $request $acquiredArtifact
        }
        if($gamePassed-and(Test-KmcChunk7ManualDerivedScenario $Scenario)){
            $verifiedDerivedSource=Get-KmcChunk7ManualDerivedSource $request.manualDerived $fixturePayload $manifest
            if((ConvertTo-KmcChunk7Canonical $verifiedDerivedSource)-cne(ConvertTo-KmcChunk7Canonical $manualDerivedSource)){throw 'Manual-derived source lineage changed during the native process.'}
            $sourceArtifact=Read-KmcJson $verifiedDerivedSource.artifactPath
            $derivedArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-manual-derived-evidence.json')
            Assert-KmcChunk7ManualDerivedEvidence $request $derivedArtifact $sourceArtifact 'PASS'
            Assert-KmcChunk7ManualDerivedOutput $request $derivedArtifact
        }
        if($gamePassed-and(Test-KmcChunk7CumulativeScenario $Scenario)){
            $verifiedCumulativeSource=Get-KmcChunk7CumulativeSource $request.cumulativeAcquisition $fixturePayload $manifest
            if((ConvertTo-KmcChunk7Canonical $verifiedCumulativeSource)-cne(ConvertTo-KmcChunk7Canonical $cumulativeAcquisitionSource)){throw 'Manual-derived source lineage changed during the native process.'}
            $sourceArtifact=Read-KmcJson $verifiedCumulativeSource.artifactPath
            $cumulativeArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-cumulative-acquisition-evidence.json')
            $assistedColdArtifact=$null;if(Test-KmcAssistedCumulativeMode $request.cumulativeAcquisition){$assistedColdArtifact=(Read-KmcAssistedConnectionJson $verifiedCumulativeSource.coldArtifactPath $verifiedCumulativeSource.coldArtifactSha256).value}
            Assert-KmcChunk7CumulativeEvidence $request $cumulativeArtifact $sourceArtifact 'PASS' $assistedColdArtifact
            Assert-KmcChunk7CumulativeOutput $request $cumulativeArtifact
        }
        if($gamePassed-and(Test-KmcChunk7ManualSeedScenario $Scenario)){
            $verifiedManualSource=Get-KmcChunk7ManualSeedSource $request.manualSeed $fixturePayload $manifest
            if((ConvertTo-KmcChunk7Canonical $verifiedManualSource)-cne(ConvertTo-KmcChunk7Canonical $manualSeedSource)){throw 'Manual seed bootstrap/cold source changed during the native process.'}
            $sourceArtifact=Read-KmcJson $manualSeedSource.artifactPath;$coldArtifact=Read-KmcJson $manualSeedSource.inventoryPath
            $seedArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-manual-seed-evidence.json')
            Assert-KmcChunk7ManualSeedEvidence $request $seedArtifact $sourceArtifact $coldArtifact 'PASS'
            Assert-KmcChunk7ManualSeedOutput $request $seedArtifact
        }
        if($gamePassed-and(Test-KmcChunk7AssistedColdInventoryScenario $Scenario)){
            $verifiedAssistedColdSource=Get-KmcChunk7AssistedColdInventorySource $request.assistedColdInventory $fixturePayload $manifest
            if((ConvertTo-KmcChunk7Canonical $verifiedAssistedColdSource)-cne(ConvertTo-KmcChunk7Canonical $assistedColdSource)-or
               (Get-KmcSha256 (Join-Path $isolatedSaves $request.persistenceLoad.fileName))-cne$(if(Test-KmcChunk7OwnerSeedSelection $request.assistedColdInventory){$request.assistedColdInventory.seedSha256}else{$request.assistedColdInventory.sourceArchiveSha256})){throw 'Read-only assisted source changed during the cold process.'}
            $sourceArtifact=Read-KmcAssistedColdSourceArtifact $request
            $coldArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-assisted-cold-inventory-evidence.json')
            # An owner seed has no producer process to compare; its record is checked by the evidence reader.
            if(-not(Test-KmcChunk7OwnerSeedSelection $request.assistedColdInventory)-and$sourceArtifact.observations.processId-eq$coldArtifact.observations.processId){throw 'Assisted cold inventory reused the preparation process.'}
            Assert-KmcChunk7AssistedColdInventoryEvidence $request $coldArtifact $sourceArtifact 'PASS'
        }
        if($gamePassed-and(Test-KmcChunk7BootstrapInventoryScenario $Scenario)){
            $verifiedBootstrapSource=Get-KmcChunk7BootstrapInventorySource $request.bootstrapInventory $fixturePayload $manifest
            if((ConvertTo-KmcChunk7Canonical $verifiedBootstrapSource)-cne(ConvertTo-KmcChunk7Canonical $bootstrapInventorySource)-or
               (Get-KmcSha256 (Join-Path $isolatedSaves 'Auto_1.zks'))-cne$request.bootstrapInventory.sourceArchiveSha256){throw 'Read-only bootstrap source changed during the cold process.'}
            $sourceArtifact=Read-KmcJson $bootstrapInventorySource.artifactPath
            $coldArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-bootstrap-inventory-evidence.json')
            if($sourceArtifact.observations.processId-eq$coldArtifact.observations.processId){throw 'Bootstrap inventory reused the authored creation process rather than cold loading.'}
            Assert-KmcChunk7BootstrapInventoryEvidence $request $coldArtifact $sourceArtifact 'PASS'
        }
        if($gamePassed-and(Test-KmcChunk7FieldScenario $Scenario)){
            $verifiedFieldSource=Get-KmcChunk7FieldSource $request.ordinaryField $fixturePayload $manifest
            if((ConvertTo-KmcChunk7Canonical $verifiedFieldSource)-cne(ConvertTo-KmcChunk7Canonical $fieldSource)-or
               (Get-KmcSha256 (Join-Path $isolatedSaves 'Auto_1.zks'))-cne$request.ordinaryField.sourceArchiveSha256){throw 'Read-only field source changed during the cold process.'}
            $sourceArtifact=Read-KmcJson $fieldSource.artifactPath
            $fieldArtifact=Read-KmcJson (Join-Path $evidenceRoot 'chunk7-ordinary-field-evidence.json')
            if($sourceArtifact.observations.processId-eq$fieldArtifact.observations.processId){throw 'Field bridge reused the acquisition process rather than cold loading.'}
        }
        if($gamePassed-and(Test-KmcChunk7OrdinaryScenario $Scenario)){
            $ordinaryArtifact=Read-KmcChunk7ManifestArtifact $request (Read-KmcJson (Join-Path $evidenceRoot 'runtime-artifacts.json')) 'chunk7-ordinary-acquisition-evidence.json' 'chunk7-ordinary-acquisition-evidence'
            Assert-KmcChunk7OrdinaryNativeArchive $request $ordinaryArtifact
        }
        if($gamePassed-and$Scenario-ceq'chunk7-persistence-load'){
            $coldArtifact=Read-KmcChunk7ManifestArtifact $request (Read-KmcJson (Join-Path $evidenceRoot 'runtime-artifacts.json')) 'chunk7-persistence-evidence.json' 'chunk7-persistence-evidence'
            Assert-KmcChunk7PersistenceColdSource -SourceRunId $PersistenceSourceRunId -Request $request -ColdArtifact $coldArtifact
        }
        $compareRealtime=$Scenario-ceq'persistence-p04-load'-and(
            (Test-KmcChargePersistenceCase $PersistenceCase)-or
            $PersistenceCase.EndsWith('-projectile',[StringComparison]::Ordinal)-or
            $PersistenceCase.EndsWith('-approach',[StringComparison]::Ordinal)-or
            $PersistenceCase.EndsWith('-casting',[StringComparison]::Ordinal))
        $compareCondition=$Scenario-ceq'persistence-p03-load'-and$PersistenceCase-cin @('condition','condition-preparing','suspended')
        if($gamePassed-and($compareRealtime-or$compareCondition-or($Scenario-cin@('persistence-p02-load','persistence-p04-load')-and$PersistenceCase-cin@('casting-items','mounted-casting-items')))){
            Assert-KmcRealtimeColdSource -SourceRunId $PersistenceSourceRunId -Request $request
        }
        if(-not$gamePassed){$errors.Add('Game reported FAIL: '+(@($validatedGameResult.errors) -join '; '))}
    }
}
catch{
    $errors.Add($_.Exception.Message)
    Write-KmcJsonAtomic (Join-Path $evidenceRoot 'launcher-failure.json') ([ordered]@{
        runId=$actualRunId;scenario=$Scenario;failedAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
        launchIssued=$launchIssued;errors=@($errors|ForEach-Object{[string]$_})
    })
    if(Test-Path -LiteralPath $orchestrationPath -PathType Leaf){
        try{
            $caughtOrchestration=Read-KmcJson $orchestrationPath
            $caughtOrchestration.stage='launcher-error-waiting-for-process-exit'
            $caughtOrchestration|Add-Member launcherErrorAtUtc ([DateTimeOffset]::UtcNow.ToString('o')) -Force
            $caughtOrchestration|Add-Member launcherErrors @($errors|ForEach-Object{[string]$_}) -Force
            Write-KmcJsonAtomic $orchestrationPath $caughtOrchestration
        }catch{$errors.Add('Durable launcher-error observation failed: '+$_.Exception.Message)}
    }
}
finally{
    if($null-ne$process){
        try{
            $exitDeadline=[DateTimeOffset]::UtcNow.AddSeconds(30)
            do{
                $process.Refresh()
                if($process.HasExited){$processExited=$true;break}
                if(@(Get-KmcSuspiciousWindows).Count-ne0){$errors.Add('Unexpected Steam/account UI appeared during exit wait.');break}
                Start-Sleep -Milliseconds 250
            }while([DateTimeOffset]::UtcNow-lt$exitDeadline)
            if(-not$processExited){$errors.Add('Kingmaker did not exit within the bounded grace period; restoration remains blocked.')}
        }catch{$errors.Add('Process exit verification failed: '+$_.Exception.Message)}
    }elseif(-not$launchIssued){$processExited=$true}
    else{$errors.Add('Launch was issued without a captured process; late-launch ambiguity blocks restoration.')}
    if($processExited){
        try{
            $expectedExitedProcessId=if($null-eq$process){0}else{$process.Id}
            if(-not(Wait-KmcStableNoKingmakerProcess -ExpectedProcessId $expectedExitedProcessId)){
                $processExited=$false
                $errors.Add('Kingmaker did not reach a stable no-process state after attributed exit.')
            }
        }catch{$processExited=$false;$errors.Add('Stable post-exit verification failed: '+$_.Exception.Message)}
    }
    if($null-ne$combinedStatePath-and(Test-Path -LiteralPath $combinedStatePath)-and$processExited){
        try{
            $restoration=Restore-KmcRuntimeTransactions -Lock $lock -CombinedStatePath $combinedStatePath -StateRoot $runtimeState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging
            $modsRestored=[bool]$restoration.modsRestored
            $saveProtection=[bool]$restoration.saveProtectionPassed
            $baselineImmutable=[bool]$restoration.baselineImmutable
            $workingRestored=[bool]$restoration.workingRestored
            $saveWriteAllowlistPassed=[bool]$restoration.saveWriteAllowlistPassed
            $restoredSaveInventoryDigest=[string]$restoration.restoredSaveInventoryDigest
            foreach($restorationError in @($restoration.errors)){$errors.Add([string]$restorationError)}
            if($isSaveBacked){
                try{
                    [void](Assert-KmcQualificationSuiteContinuity `
                        -SnapshotPath $QualificationSuiteSnapshotPath -StateRoot $runtimeState -SaveRoot $saveRoot -ModsRoot $liveMods `
                        -QualificationPath $qualificationPath -PackagePath $PackagePath -PackageManifest $manifest `
                        -ExpectedSuiteId $ExpectedQualificationSuiteId -ExpectedSnapshotSha256 $ExpectedQualificationSuiteSnapshotSha256)
                }catch{
                    $modsRestored=$false;$saveProtection=$false;$baselineImmutable=$false
                    $workingRestored=$false;$saveWriteAllowlistPassed=$false
                    $errors.Add('Qualification-suite post-restoration audit failed: '+$_.Exception.Message)
                }
            }
        }catch{$errors.Add('Combined external-state restoration failed: '+$_.Exception.Message)}
    }elseif($processExited){
        try{
            $modsRestored=(Get-KmcDirectoryManifest $liveMods).digest-ceq$beforeMods.digest
            $currentSaves=Get-KmcSaveMetadataInventory $saveRoot
            $restoredSaveInventoryDigest=[string]$currentSaves.digest
            $saveProtection=$currentSaves.digest-ceq$beforeSaves.digest
            $baselineImmutable=$saveProtection;$workingRestored=$saveProtection;$saveWriteAllowlistPassed=$saveProtection
            if(-not$modsRestored-or-not$saveProtection){$errors.Add('External state differs after a run that created no combined transaction state.')}
        }catch{$errors.Add('Unmutated external-state verification failed: '+$_.Exception.Message)}
    }else{$errors.Add('Kingmaker process state is ambiguous; external-state restoration was intentionally not attempted.')}
    if($processExited-and$null-ne$profileSnapshot){
        try{
            if($areasIsolationSelected){
                $areasRestoration=Restore-KmcPersistenceAreasIsolation -Lock $lock -Snapshot $profileSnapshot -BackupRoot $runtimeBackups
                if($null-ne$areasRestoration){
                    try{
                        Write-KmcJsonAtomic (Join-Path $evidenceRoot 'profile-areas-isolation.json') ([ordered]@{
                            schemaVersion=1;runId=$actualRunId;profileRoot=[string]$areasRestoration.profile
                            originalDigest=[string]$areasRestoration.originalDigest
                            generatedDigest=[string]$areasRestoration.generatedDigest
                            generatedQuarantine=$areasRestoration.generatedQuarantine
                            originalAreasRestoredExactly=[bool]$areasRestoration.restored
                        })
                    }catch{$errors.Add('Areas restoration receipt failed: '+$_.Exception.Message);$saveProtection=$false}
                }
            }
            Restore-KmcPersistenceStartupSettings -Lock $lock -Snapshot $profileSnapshot -BackupRoot $runtimeBackups -ExpectedCurrentParamsSha256 (Get-KmcSha256 $profileSnapshot.paramsPath) -ExpectedCurrentPrefsSha256 (Get-KmcTextSha256 (Get-KmcPersistencePlayerPrefs)) -RemovalObserver:$isObserver -BaseGameOnly:$baseGameStaged -Confirm:$false
            # Admitted native achievement-cache churn is an expected external
            # change, not restored bytes, so it is recorded rather than implied
            # away by a bare "profile unchanged" result.
            $profileCacheChanges=@(Wait-KmcPersistenceProfileUnchanged $profileSnapshot)
        }
        catch{$errors.Add($_.Exception.Message);$saveProtection=$false}
        try{
            Write-KmcJsonAtomic (Join-Path $evidenceRoot 'profile-cache-changes.json') ([ordered]@{
                schemaVersion=1;runId=$actualRunId
                profileRoot=[string]$profileSnapshot.profile
                snapshotProfileDigest=[string]$profileSnapshot.profileDigest
                policy='native-achievement-cache-settled-size'
                acceptedNativeCacheChanges=@($profileCacheChanges|ForEach-Object{[ordered]@{
                    path=[string]$_.path;change=[string]$_.change;length=[long]$_.length
                    beforeSha256=[string]$_.beforeSha256;afterSha256=[string]$_.afterSha256}})
                profileBytesRestoredExactly=($profileCacheChanges.Count-eq0)
            })
        }catch{$errors.Add('Profile cache change receipt failed: '+$_.Exception.Message)}
    }
    try{if($processExited){[void](Assert-KmcSteamSafety $SteamPath)}}catch{$errors.Add('Steam postflight safety failed: '+$_.Exception.Message)}
    if($BaseGameOnly-and$processExited){
        try{Assert-KmcBaseGameDependencyReceipt $baseGameDependencies -VerifyFiles;$baseGameDependenciesUnchanged=$true}
        catch{$errors.Add('Base-game loader dependency verification failed: '+$_.Exception.Message)}
    }
    if($null-ne$lock){
        try{
            if($modsRestored-and$saveProtection-and$processExited){Close-KmcRuntimeLock $lock}else{Abandon-KmcRuntimeLock $lock}
        }catch{$errors.Add('Runtime lock finalization failed: '+$_.Exception.Message)}
    }
    if($BaseGameOnly){
        try{Write-KmcJsonAtomic (Join-Path $evidenceRoot 'base-game-isolation-result.json') ([ordered]@{
            schemaVersion=1;runId=$actualRunId;stagingMode='base-game-only-kmc';dependencyDigest=[string]$baseGameDependencies.dependencyDigest;
            stageVerified=$baseGameStaged;dependenciesUnchanged=$baseGameDependenciesUnchanged;processExited=$processExited;
            modsRestored=$modsRestored;saveProtectionPassed=$saveProtection;gameReportedPass=$gamePassed;
            transactionStatePath=$modsTransactionStatePath;transactionStateSha256=$(if($null-ne$modsTransactionStatePath-and(Test-Path -LiteralPath $modsTransactionStatePath)){Get-KmcSha256 $modsTransactionStatePath}else{$null});
            nativeInventoryObserved=$baseGameNativeInventoryObserved;runtimeLoaderInventorySha256=$baseGameNativeInventorySha256;
            nativeCompatibilityQualified=($gamePassed-and$baseGameNativeInventoryObserved-and$baseGameDependenciesUnchanged-and$modsRestored-and$saveProtection-and$processExited-and$errors.Count-eq0);
            limitation='Qualification is limited to this exact selected scenario and candidate. Loaded UMM entries and assembly identities are observed; broader gameplay is not inferred.'})}
        catch{$errors.Add('Base-game isolation receipt failed: '+$_.Exception.Message)}
    }
    $errorArray=@($errors|ForEach-Object{[string]$_})
    if($null-ne$request){
        if($isManualReview){
            $status=if($manualReviewReady-and$gamePassed-and$processExited-and$modsRestored-and$saveProtection-and
                $baselineImmutable-and$workingRestored-and$saveWriteAllowlistPassed-and$errorArray.Count-eq0){'PASS'}else{'FAIL'}
            $final=[ordered]@{
                schemaVersion=1;evidenceKind='manual-visual-review-session';runId=$actualRunId;scenario=$Scenario;
                status=$status;branch=[string]$manifest.branch;commit=[string]$manifest.commit;
                productVersion=[string]$manifest.version;dllSha256=[string]$manifest.dllSha256;
                dllMvid=[string]$manifest.dllMvid;transactionToken=[string]$lock.Token;
                startedAtUtc=$startedAt.ToString('o');completedAtUtc=[DateTimeOffset]::UtcNow.ToString('o');
                reviewReady=$manualReviewReady;readyAtUtc=$(if($null-eq$manualReady){$null}else{[string]$manualReady.readyAtUtc});
                readyEvidenceSha256=$manualReadyHash;visualAcceptance='PENDING';processExited=$processExited;
                modsRestored=$modsRestored;saveProtectionPassed=$saveProtection;baselineImmutable=$baselineImmutable;
                workingRestored=$workingRestored;saveWriteAllowlistPassed=$saveWriteAllowlistPassed;
                restoredSaveInventoryDigest=$restoredSaveInventoryDigest;errors=$errorArray
            }
        }
        elseif($isSaveBacked){
            $final=New-KmcRuntimeResultV2 -Request $request -ValidatedGameResult $validatedGameResult -StartedAtUtc $startedAt -ModsRestored $modsRestored -BaselineImmutable $baselineImmutable -WorkingRestored $workingRestored -SaveWriteAllowlistPassed $saveWriteAllowlistPassed -RestoredSaveInventoryDigest $restoredSaveInventoryDigest -GameResultSha256 $gameResultHash -Errors $errorArray
        }
        else{
            $status=if($gamePassed-and$modsRestored-and$saveProtection-and$errorArray.Count-eq0){'PASS'}else{'FAIL'}
            $final=[ordered]@{
                schemaVersion=1;runId=$actualRunId;scenario=$Scenario;status=$status;branch=[string]$manifest.branch;
                commit=[string]$manifest.commit;productVersion=[string]$manifest.version;dllSha256=[string]$manifest.dllSha256;
                dllMvid=[string]$manifest.dllMvid;transactionToken=[string]$lock.Token;startedAtUtc=$startedAt.ToString('o');
                completedAtUtc=[DateTimeOffset]::UtcNow.ToString('o');modsRestored=$modsRestored;saveProtectionPassed=$saveProtection;
                gameResultSha256=$gameResultHash;errors=$errorArray
            }
        }
        try{Write-KmcJsonAtomic $finalResultPath $final}catch{Write-Error ('Final runtime evidence write failed: '+$_.Exception.Message)}
    }
    if(Test-Path -LiteralPath $orchestrationPath){
        try{
            $orchestration=Read-KmcJson $orchestrationPath
            $orchestration.status=if($null-ne$final){[string]$final.status}else{'FAIL'}
            $orchestration.stage=if($modsRestored-and$saveProtection){'restored'}else{'restoration-blocked'}
            $orchestration|Add-Member completedAtUtc ([DateTimeOffset]::UtcNow.ToString('o')) -Force
            Write-KmcJsonAtomic $orchestrationPath $orchestration
        }catch{Write-Error ('Orchestration evidence update failed: '+$_.Exception.Message)}
    }
}
if(-not(Test-Path -LiteralPath $requestPath -PathType Leaf)-or-not(Test-Path -LiteralPath $finalResultPath -PathType Leaf)){
    throw "Runtime scenario failed before complete request/result evidence was written. Evidence: $evidenceRoot"
}
if($isManualReview){
    & (Join-Path $repoRoot 'scripts\runtime\Test-KmcManualReviewResult.ps1') -ResultPath $finalResultPath -RequestPath $requestPath
    if((Read-KmcJson $finalResultPath).status-cne'PASS'){throw "Manual review launcher failed its safety/restoration contract. Evidence: $finalResultPath"}
    Write-Host "Manual review session safely restored; visual acceptance remains PENDING: $finalResultPath"
}
else{
    & (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeResult.ps1') -ResultPath $finalResultPath -RequestPath $requestPath
    if((Read-KmcJson $finalResultPath).status-cne'PASS'){throw "Runtime scenario failed. Evidence: $finalResultPath"}
    Write-Host "Runtime scenario PASS: $finalResultPath"
}
