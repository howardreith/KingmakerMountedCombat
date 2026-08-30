[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [ValidateSet(
        'mod-load-smoke','export-mounted-contracts','export-candidate-mount-rigs','observe-mount-diagnostic-availability','horse-native-asset-audit','horse-companion-blueprint-registration','horse-companion-unmounted-suite','horse-mounted-alpha-suite','horse-native-controls-ux-suite',
        'phase3d-unified-combat-rt-suite','phase3d-unified-combat-tb-suite','phase3d-horse-presentation-suite',
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
        'mounted-pair-load-safety','mounted-pair-area-transition-safety','fixture-intake','lifecycle-suite','combat-lifecycle-suite',
        'native-save-clean-dismount','native-area-clean-dismount','native-mode-transition-cleanup',
        'presentation-residue-and-uninstall-safety','pose-idle','pose-walk-run','pose-turn-stop',
        'pose-doorway-formation','pose-equipment-variants','ui-selection-portrait-actionbar',
        'camera-follow-and-command-routing','movement-suite','boundary-suite','presentation-suite',
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
$beforeRoots=@(
    (Get-KmcDirectoryManifest $runtimeState),(Get-KmcDirectoryManifest $runtimeBackups),
    (Get-KmcDirectoryManifest $runtimeStaging),(Get-KmcDirectoryManifest $runtimeEvidence),
    (Get-KmcDirectoryManifest $liveMods)
)
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
    for($index=0;$index-lt$afterRoots.Count;$index++){
        if($afterRoots[$index].digest-cne$beforeRoots[$index].digest){throw 'WhatIf purity failed: an external tree changed.'}
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
$finalResultPath=Join-Path $evidenceRoot 'runtime-result.json'
$manualReadyPath=Join-Path $evidenceRoot 'manual-review-ready.json'
$manualFailurePath=Join-Path $evidenceRoot 'manual-review-failure.json'
$manualResultPath=Join-Path $evidenceRoot 'manual-review-result.json'
if($isManualReview){$finalResultPath=$manualResultPath}
$orchestrationPath=Join-Path $evidenceRoot 'orchestration.json'
$lock=$null
$request=$null
$combinedStatePath=$null
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
    $combinedStatePath=New-KmcRunTransactionState -Lock $lock -Mode $(if($isSaveBacked){'save-backed-v3-suite'}else{'no-save-v1'}) `
        -LiveModsRoot $liveMods -SaveRoot $saveRoot -StateRoot $runtimeState -ModsBefore $beforeRoots[4] -SavesBefore $beforeSaves `
        -QualificationSuiteSnapshotPath $QualificationSuiteSnapshotPath -QualificationSuiteId $ExpectedQualificationSuiteId `
        -QualificationSuiteSnapshotSha256 $ExpectedQualificationSuiteSnapshotSha256
    if($isSaveBacked){
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        Assert-KmcSaveMetadataInventoriesEqual `
            -Before $beforeSaves `
            -After (Get-KmcSaveMetadataInventory $saveRoot) `
            -Description 'runtime immediate pre-save-transaction metadata'
        [void](Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $lockedPair -SaveRoot $saveRoot -StateRoot $runtimeState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging -Scenario $Scenario)
    }
    [void](Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $liveMods -PackagePath $PackagePath -StateRoot $runtimeState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging)
    Write-KmcJsonAtomic $requestPath $request
    & (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeRequest.ps1') -RequestPath $requestPath -PackageManifestPath $packageManifestPath
    $orchestration=[ordered]@{
        schemaVersion=2;runId=$actualRunId;scenario=$Scenario;status='IN PROGRESS';stage='transactions-staged';
        startedAtUtc=$startedAt.ToString('o');steamSafety=$steamSafety;combinedTransactionState=$combinedStatePath;
        protectedSaveDigestBefore=$beforeSaves.digest;liveModsDigestBefore=$beforeRoots[4].digest;saveBacked=$isSaveBacked
    }
    Write-KmcJsonAtomic $orchestrationPath $orchestration
    Assert-KmcNoGameProcesses
    $requestHash=Get-KmcSha256 $requestPath
    $arguments=@('-applaunch','640820','-kmcRuntimeRequest',('"'+$requestPath+'"'),'-kmcRuntimeToken',[string]$lock.Token,'-kmcRuntimeRequestSha256',$requestHash)
    [void](Start-Process -FilePath $SteamPath -ArgumentList $arguments -PassThru)
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
        while(-not(Test-Path -LiteralPath $gameResultPath -PathType Leaf)){
            $process.Refresh()
            if($process.HasExited){$processExited=$true;throw 'Kingmaker exited before committing its atomic game result.'}
            $all=@(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
            if($all.Count-ne1-or$all[0].Id-ne$process.Id){throw 'Kingmaker process attribution changed during the run.'}
            if(@(Get-KmcSuspiciousWindows).Count-ne0){throw 'Unexpected Steam/account UI appeared during the run.'}
            if([DateTimeOffset]::UtcNow-ge$deadline){throw 'Runtime game result timed out; Kingmaker is intentionally left running and restoration is blocked.'}
            Start-Sleep -Milliseconds 250
        }
        $candidateHash=Get-KmcSha256 $gameResultPath
        $gameResultHash=$candidateHash
        & (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeGameResult.ps1') -GameResultPath $gameResultPath -RequestPath $requestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $process.Id -NotBeforeUtc $startedAt -VerifyLiveWorkingIdentity -ExpectedLiveWorkingPath $lockedWorkingPath
        $validatedGameResult=Read-KmcJson $gameResultPath
        $gamePassed=[string]$validatedGameResult.status -ceq 'PASS'
        if(-not$gamePassed){$errors.Add('Game reported FAIL: '+(@($validatedGameResult.errors) -join '; '))}
    }
}
catch{
    $errors.Add($_.Exception.Message)
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
            $modsRestored=(Get-KmcDirectoryManifest $liveMods).digest-ceq$beforeRoots[4].digest
            $currentSaves=Get-KmcSaveMetadataInventory $saveRoot
            $restoredSaveInventoryDigest=[string]$currentSaves.digest
            $saveProtection=$currentSaves.digest-ceq$beforeSaves.digest
            $baselineImmutable=$saveProtection;$workingRestored=$saveProtection;$saveWriteAllowlistPassed=$saveProtection
            if(-not$modsRestored-or-not$saveProtection){$errors.Add('External state differs after a run that created no combined transaction state.')}
        }catch{$errors.Add('Unmutated external-state verification failed: '+$_.Exception.Message)}
    }else{$errors.Add('Kingmaker process state is ambiguous; external-state restoration was intentionally not attempted.')}
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
