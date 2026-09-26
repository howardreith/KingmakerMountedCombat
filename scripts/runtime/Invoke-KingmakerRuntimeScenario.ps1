[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [ValidateSet(
        'mod-load-smoke','export-mounted-contracts','export-candidate-mount-rigs','observe-mount-diagnostic-availability','horse-native-asset-audit','horse-companion-blueprint-registration','horse-companion-unmounted-suite','horse-mounted-alpha-suite','horse-native-controls-ux-suite',
        'chunk6a-combat-mount-rt', 'chunk6a-combat-mount-tb', 'chunk6a-mount-preamble', 'chunk6a-mount-approach', 'chunk4-rider-incapacitation-tb', 'chunk4-rider-death-tb', 'chunk4-mount-death-tb', 'chunk4-targeting-rider-rt', 'chunk4-targeting-mount-rt', 'chunk4-ground-arrival-rt', 'chunk4-horse-strike-comparison-rt', 'chunk4-targeting-area-unmounted-rt', 'chunk4-obstruction-ranged-rt', 'chunk4-ranged-native-control-rt', 'chunk4-interrupt-melee-rt', 'chunk4-interrupt-ranged-rt', 'chunk4-inspection-rt', 'chunk4-session-rt', 'chunk4-session-tb', 'chunk4-sustained-melee-rt', 'chunk4-sustained-ranged-rt', 'chunk4-sustained-tb', 'chunk4-charge-safety-rt', 'chunk4-charge-safety-tb', 'actor-allocation-rider-first-tb', 'actor-allocation-mount-first-tb', 'actor-allocation-rider-first-unmounted-tb', 'actor-allocation-mount-first-unmounted-tb', 'ordinary-attack-controls-tb', 'unmounted-attack-controls-rt', 'phase3h-combat-loop-rt', 'phase3h-combat-loop-tb', 'phase3g-native-controls-rt', 'phase3g-native-controls-tb', 'phase3d-unified-combat-rt-suite','phase3d-unified-combat-tb-suite','phase3d-horse-presentation-suite',
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
    [ValidateRange(360,900)][int]$TimeoutSeconds=360,
    [switch]$SaveAccessAllowed,
    [string]$PackagePath,
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$PersistenceSourceRunId,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPersistenceSourceSha256,
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPersistenceAlternateSha256,
    # The foreign-header P06 derivative is campaign B's own manual archive from a
    # completed campaign-b run; the alternate hash above then names that archive.
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$PersistenceForeignSourceRunId,
    # The genuine no-DLL cleanup-save observation: the removal observer package
    # (a separate minimal UMM mod) is staged in place of KMC for that one run.
    [string]$ObserverPackagePath,
    [ValidateSet('partial-movement','rider-spent','between-partner-orders','exhausted','explicit-end','step','conversion','round-effect','reaction','condition','condition-preparing','suspended','manual','quick','auto','manual-renamed','alternating','queued','unmounted-spent','mounted-spent','unmounted-attack','mounted-attack','unmounted-projectile','mounted-projectile','unmounted-approach','mounted-approach','unmounted-casting','mounted-casting','legacy','schema1','future','malformed','profile','campaign','missing-rider','missing-mount','mismatched-profile','policy','combat-missing','combat-ai','timeout','cancel-wait','locked-replace','serialization-cancel','serialization-cancel-output','disable-reenable','campaign-b','prepare-removal','disable-during-load','absent-kmc','removal-no-dll','rider-death','mount-death','rider-size-change','area-reload','area-cross-entry','area-cross-exit','area-cross-entry-auto','area-cross-exit-auto','failed-area-load','foreign-header-campaign')][string]$PersistenceCase,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$PersistenceAreaEnterPoint,
    [ValidatePattern('^[0-9a-f]{32}$')][string]$PersistenceAreaTargetArea,
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
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'PersistenceProfileProtection.ps1')
. (Join-Path $PSScriptRoot 'PersistenceSaveFixtures.ps1')
. (Join-Path $PSScriptRoot 'PersistenceValidationFixtures.ps1')
if($PSBoundParameters.ContainsKey('PersistenceCase') -and $Scenario -cnotin @('persistence-p07-save','persistence-p07-load','persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load','persistence-p04-save','persistence-p04-load','persistence-p05-save','persistence-p05-load','persistence-p06-load')) { throw 'PersistenceCase is restricted to the exact combat scenarios.' }
if($Scenario -cin @('persistence-p05-save','persistence-p05-load')){
    $slotCases=if($Scenario-ceq'persistence-p05-load'){@('manual','quick','auto','manual-renamed','alternating','queued')}else{@('manual','quick','auto','alternating','queued')}
    if($PersistenceCase-cnotin $slotCases){throw 'P05 requires its exact native slot category.'}
}elseif($PersistenceCase-cin @('manual','quick','auto','manual-renamed','alternating','queued')){throw 'P05 slot category cannot run under another scenario.'}
if($Scenario -cin @('persistence-p04-save','persistence-p04-load')){
    if($PersistenceCase-cnotin @('unmounted-spent','mounted-spent','unmounted-attack','mounted-attack','unmounted-projectile','mounted-projectile','unmounted-approach','mounted-approach','unmounted-casting','mounted-casting')){throw 'P04 requires its exact native RT checkpoint.'}
}elseif($PersistenceCase-cin @('unmounted-spent','mounted-spent','unmounted-attack','mounted-attack','unmounted-projectile','mounted-projectile','unmounted-approach','mounted-approach','unmounted-casting','mounted-casting')){throw 'P04 checkpoint cannot run under another scenario.'}
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
    if($PersistenceCase-cnotin @('timeout','cancel-wait','locked-replace','serialization-cancel','serialization-cancel-output','disable-reenable','campaign-b','prepare-removal','disable-during-load','absent-kmc','removal-no-dll','rider-death','mount-death','rider-size-change','area-reload','area-cross-entry','area-cross-exit','area-cross-entry-auto','area-cross-exit-auto')){throw 'P07 requires its exact owned recovery case.'}
    if($PersistenceCase-cin @('prepare-removal','disable-during-load')-and$Scenario-cne'persistence-p07-save'){throw 'A removal or disable-during-load case is save-only.'}
    if($PersistenceCase-ceq'absent-kmc'-and$Scenario-cne'persistence-p07-load'){throw 'The integration-absent case is cold-load only.'}
    if($PersistenceCase-ceq'removal-no-dll'-and$Scenario-cne'persistence-p07-load'){throw 'The no-DLL removal case is cold-load only.'}
}elseif($PersistenceCase-cin @('timeout','cancel-wait','locked-replace','serialization-cancel','serialization-cancel-output','disable-reenable','campaign-b','prepare-removal','disable-during-load','absent-kmc','removal-no-dll','rider-death','mount-death','rider-size-change','area-reload','area-cross-entry','area-cross-exit','area-cross-entry-auto','area-cross-exit-auto')){throw 'Recovery faults require the exact P07 scenario.'}
if($PersistenceCase-cin @('area-cross-entry','area-cross-exit','area-cross-entry-auto','area-cross-exit-auto')){
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
$isObserver=$Scenario-ceq'persistence-p07-load'-and$PersistenceCase-ceq'removal-no-dll'
if($isObserver-and[string]::IsNullOrWhiteSpace($ObserverPackagePath)){throw 'The no-DLL removal case requires the removal-observer package.'}
if(-not$isObserver-and-not[string]::IsNullOrWhiteSpace($ObserverPackagePath)){throw 'Only the no-DLL removal case takes the removal-observer package.'}
$requestedWhatIf=[bool]$WhatIfPreference
$WhatIfPreference=$false
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
    $beforeRoots=@(
        (Get-KmcDirectoryManifest $runtimeState),(Get-KmcDirectoryManifest $runtimeBackups),
        (Get-KmcDirectoryManifest $runtimeStaging),(Get-KmcDirectoryManifest $runtimeEvidence),
        $beforeMods
    )
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
    $afterRoots=@(
        (Get-KmcDirectoryManifest $runtimeState),(Get-KmcDirectoryManifest $runtimeBackups),
        (Get-KmcDirectoryManifest $runtimeStaging),(Get-KmcDirectoryManifest $runtimeEvidence),
        (Get-KmcDirectoryManifest $liveMods)
    )
    # A purity failure must name the tree and the exact entries that differ. The
    # comparison itself is unchanged: every byte of every root is still rehashed
    # and any difference still fails closed.
    for($index=0;$index-lt$afterRoots.Count;$index++){
        if($afterRoots[$index].digest-cne$beforeRoots[$index].digest){
            $purityBefore=$beforeRoots[$index]; $purityAfter=$afterRoots[$index]
            $purityBeforeEntries=@{}; foreach($entry in $purityBefore.entries){$purityBeforeEntries[($entry.kind+'|'+$entry.path)]=([string]$entry.length+'|'+[string]$entry.sha256)}
            $purityAfterEntries=@{}; foreach($entry in $purityAfter.entries){$purityAfterEntries[($entry.kind+'|'+$entry.path)]=([string]$entry.length+'|'+[string]$entry.sha256)}
            $purityDetails=New-Object 'System.Collections.Generic.List[string]'
            foreach($key in $purityBeforeEntries.Keys){
                if(-not$purityAfterEntries.ContainsKey($key)){$purityDetails.Add('removed '+$key)}
                elseif($purityAfterEntries[$key]-cne$purityBeforeEntries[$key]){$purityDetails.Add('changed '+$key+' : '+$purityBeforeEntries[$key]+' -> '+$purityAfterEntries[$key])}
                if($purityDetails.Count-ge20){break}
            }
            foreach($key in $purityAfterEntries.Keys){
                if(-not$purityBeforeEntries.ContainsKey($key)){$purityDetails.Add('added '+$key)}
                if($purityDetails.Count-ge40){break}
            }
            throw ('WhatIf purity failed: an external tree changed: '+$purityAfter.root+
                ' (files '+$purityBefore.fileCount+'->'+$purityAfter.fileCount+
                ', directories '+$purityBefore.directoryCount+'->'+$purityAfter.directoryCount+
                ', bytes '+$purityBefore.totalBytes+'->'+$purityAfter.totalBytes+')'+
                $(if($purityDetails.Count-eq0){''}else{[Environment]::NewLine+($purityDetails-join[Environment]::NewLine)}))
        }
    }
    if((Get-KmcSaveMetadataInventory $saveRoot).digest-cne$beforeSaves.digest){throw 'WhatIf purity failed: save metadata changed.'}
    Assert-KmcNoGameProcesses
    if($BootstrapOfflineCloudEvidence){[void](Assert-KmcSteamSafety $SteamPath -AllowMissingCurrentSessionCloudState)}
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
    if($Scenario -cin @('persistence-isolation','persistence-p07-save','persistence-p07-load','persistence-p01-save','persistence-p01-load','persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load','persistence-p04-save','persistence-p04-load','persistence-p05-save','persistence-p05-load','persistence-p06-load')){
        $profileSnapshot=New-KmcPersistenceProfileSnapshot -Lock $lock -SaveRoot $saveRoot -GameRoot ([string]$intake.requestedLayout.kingmakerInstallDir) -BackupRoot $runtimeBackups
    }
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
    if($PSBoundParameters.ContainsKey('PersistenceCase')) { $request['persistenceCase']=$PersistenceCase }
    if($PersistenceCase-cin @('area-cross-entry','area-cross-exit','area-cross-entry-auto','area-cross-exit-auto')){
        $request['persistenceAreaTarget']=[ordered]@{
            enterPoint=$PersistenceAreaEnterPoint;area=$PersistenceAreaTargetArea
            autoSaveMode=if($PersistenceCase-cin @('area-cross-entry','area-cross-entry-auto')){'AfterEntry'}else{'BeforeExit'}
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
        if($Scenario -cin @('persistence-isolation','persistence-p07-save','persistence-p07-load','persistence-p01-save','persistence-p01-load','persistence-p02-save','persistence-p02-load','persistence-p03-save','persistence-p03-load','persistence-p04-save','persistence-p04-load','persistence-p05-save','persistence-p05-load','persistence-p06-load')){
            $profileRoot=Assert-KmcChildPath (Join-Path $runtimeStaging ('persistence-'+$actualRunId)) $runtimeStaging 'owned persistence profile'
            if(Test-Path -LiteralPath $profileRoot){throw 'Persistence profile already exists; refusing ambiguous ownership.'}
            [void][IO.Directory]::CreateDirectory($profileRoot)
            $isolatedSaves=Join-Path $profileRoot 'Saved Games'
            [void][IO.Directory]::CreateDirectory($isolatedSaves)
            [void][IO.Directory]::CreateDirectory((Join-Path $profileRoot 'Areas'))
            $copySource=$lockedWorkingPath
            $copyDescriptor=$fixturePayload.working
            if($Scenario -cin @('persistence-p07-load','persistence-p01-load','persistence-p02-load','persistence-p03-load','persistence-p04-load','persistence-p05-load','persistence-p06-load')){
                $sourceCase=if($Scenario-ceq'persistence-p07-load'){$PersistenceCase}elseif($Scenario-ceq'persistence-p05-load'){if($PersistenceCase-ceq'manual-renamed'){'manual'}else{$PersistenceCase}}elseif($Scenario-ceq'persistence-p04-load'-or($Scenario-ceq'persistence-p03-load'-and$PersistenceCase-cin @('condition','condition-preparing','suspended'))){$PersistenceCase}else{$null}
                $source=if($Scenario-ceq'persistence-p06-load'){
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
            [IO.File]::SetLastWriteTimeUtc($copiedFixture,([IO.File]::GetLastWriteTimeUtc($copySource)))
            if((Get-KmcSha256 $copiedFixture)-cne[string]$copyDescriptor.sha256){
                throw 'Copied persistence fixture bytes differ from the admitted Working fixture.'
            }
            Write-KmcJsonAtomic (Join-Path $profileRoot 'owner.json') ([ordered]@{
                runId=$actualRunId;transactionToken=[string]$lock.Token;scenario=$Scenario;persistenceCase=$PersistenceCase
                sourceSha256=[string]$copyDescriptor.sha256;saveRoot=$isolatedSaves
            })
        }

    }
    [void](Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $liveMods -PackagePath $(if($isObserver){$ObserverPackagePath}else{$PackagePath}) -StateRoot $runtimeState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging -StagingMode $(if($isObserver){'live-clone-minus-kmc-plus-observer'}else{'live-clone-plus-kmc-overlay'}))
    Write-KmcJsonAtomic $requestPath $request
    & (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeRequest.ps1') -RequestPath $requestPath -PackageManifestPath $packageManifestPath
    if($isObserver){
        # What the observer process is bound to: the owned profile, the live Mods
        # root to prove empty of KMC, the prepared cleanup archive, and KMC's own
        # registered blueprint and Harmony identities to look for and not find.
        $observerRequest=[ordered]@{
            schemaVersion=1;runId=$actualRunId;transactionToken=[string]$lock.Token;evidenceRoot=$evidenceRoot
            profileRoot=$profileRoot;modsRoot=$liveMods;kmcModId='KingmakerMountedCombat';observerModId='KmcRemovalObserver'
            kmcHarmonyIds=@('KingmakerMountedCombat.Feasibility','KingmakerMountedCombat.PersistenceIsolation','KingmakerMountedCombat.Diagnostics.QueuedMountWindow','KingmakerMountedCombat.Diagnostics.ActorAllocation','KingmakerMountedCombat.Diagnostics.OrdinaryAttackTrace')
            archive=$request.persistenceLoad
            kmcBlueprintGuids=@('4016c7db400ab721ff125aef9e65e202','7db7c50677e39f09feef56f3831fc723','98e651899e6278d938de77af1d69bd32','6874a165bf8bda3531ee4e2abc10c899')
            mammothBlueprintGuid='e7aa96d15a45238438ae4cfb476f6bb9'
            candidate=[ordered]@{commit=[string]$manifest.commit;productVersion=[string]$manifest.version;dllSha256=[string]$manifest.dllSha256;dllMvid=[string]$manifest.dllMvid}
            observer=[ordered]@{version=[string]$observerManifest.version;packageSha256=[string]$observerManifest.packageSha256;dllSha256=[string]$observerManifest.dllSha256;dllMvid=[string]$observerManifest.dllMvid}
            timeoutSeconds=300
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
        $compareRealtime=$Scenario-ceq'persistence-p04-load'-and(
            $PersistenceCase.EndsWith('-projectile',[StringComparison]::Ordinal)-or
            $PersistenceCase.EndsWith('-approach',[StringComparison]::Ordinal)-or
            $PersistenceCase.EndsWith('-casting',[StringComparison]::Ordinal))
        $compareCondition=$Scenario-ceq'persistence-p03-load'-and$PersistenceCase-cin @('condition','condition-preparing','suspended')
        if($gamePassed-and($compareRealtime-or$compareCondition)){
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
            Restore-KmcPersistenceStartupSettings -Lock $lock -Snapshot $profileSnapshot -BackupRoot $runtimeBackups -ExpectedCurrentParamsSha256 (Get-KmcSha256 $profileSnapshot.paramsPath) -ExpectedCurrentPrefsSha256 (Get-KmcTextSha256 (Get-KmcPersistencePlayerPrefs)) -RemovalObserver:$isObserver -Confirm:$false
            # Admitted native achievement-cache churn is an expected external
            # change, not restored bytes, so it is recorded rather than implied
            # away by a bare "profile unchanged" result.
            $profileCacheChanges=@(Assert-KmcPersistenceProfileUnchanged $profileSnapshot)
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
    if($null-ne$lock){
        try{
            if($modsRestored-and$saveProtection-and$processExited){Close-KmcRuntimeLock $lock}else{Abandon-KmcRuntimeLock $lock}
        }catch{$errors.Add('Runtime lock finalization failed: '+$_.Exception.Message)}
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
