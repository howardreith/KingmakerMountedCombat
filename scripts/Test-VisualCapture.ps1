[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$coordinatorPath = Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\MovementScreenshotCaptureCoordinator.cs'
$statePath = Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\MovementScreenshotCaptureState.cs'
$enginePath = Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeMovementScenarioEngine.cs'
$poseAdapterPath = Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedRiderPoseAdapter.cs'
$manualReviewPath = Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeManualReviewSession.cs'
$horseEnginePath = Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\HorseCompanionUnmountedScenarioEngine.cs'
$compositionPath = Join-Path $repoRoot 'src\KingmakerMountedCombat\CompositionRoot.cs'
$projectPath = Join-Path $repoRoot 'src\KingmakerMountedCombat\KingmakerMountedCombat.csproj'
$coordinator = [IO.File]::ReadAllText($coordinatorPath)
$state = [IO.File]::ReadAllText($statePath)
$engine = [IO.File]::ReadAllText($enginePath)
$poseAdapter = [IO.File]::ReadAllText($poseAdapterPath)
$manualReview = [IO.File]::ReadAllText($manualReviewPath)
$horseEngine = [IO.File]::ReadAllText($horseEnginePath)
$composition = [IO.File]::ReadAllText($compositionPath)
$project = [IO.File]::ReadAllText($projectPath)
$passes = 0
$failures = New-Object 'System.Collections.Generic.List[string]'

function Assert-VisualCapture {
    param([bool]$Condition, [string]$Message)
    if ($Condition) {
        $script:passes++
        Write-Host "PASS $Message"
    }
    else {
        $script:failures.Add($Message)
        Write-Host "FAIL $Message"
    }
}

Assert-VisualCapture ($project.Contains('Diagnostics\MovementScreenshotCaptureCoordinator.cs') -and
    $project.Contains('Diagnostics\MovementScreenshotCaptureState.cs') -and
    $project.Contains('Diagnostics\PresentationOverlayEvidence.cs')) 'production project includes the isolated screenshot and overlay evidence state machines'
Assert-VisualCapture ($coordinator.Contains('UnityModManager.UI.Instance') -and
    $coordinator.Contains('instance.Opened') -and
    $coordinator.Contains('instance.ToggleWindow(opened)')) 'capture uses the exact public UMM UI.Instance, Opened, and ToggleWindow(Boolean) surface'
Assert-VisualCapture (-not $coordinator.Contains('.Params') -and
    -not $coordinator.Contains('SaveSettings') -and
    -not $coordinator.Contains('ConfigPath')) 'UMM overlay lease is in-memory and does not touch settings persistence'
Assert-VisualCapture ([regex]::Matches($coordinator, 'yield return new WaitForEndOfFrame\(\);').Count -eq 1 -and
    -not $coordinator.Contains('yield return null;')) 'capture coroutine has one exact bounded WaitForEndOfFrame yield'
Assert-VisualCapture ($coordinator.Contains('private const int MaximumCaptureFrameDelay = 2;') -and
    $coordinator.Contains('currentFrame > activeStartedFrame + MaximumCaptureFrameDelay') -and
    $coordinator.Contains('exceeded its fixed two-frame completion bound')) 'capture coroutine has a fixed frame-progress cancellation bound'
Assert-VisualCapture ($coordinator.Contains('activeRunner.StopCoroutine(activeCoroutine)') -and
    $coordinator.Contains('activeLease?.Dispose()') -and
    $coordinator.Contains('Screenshot capture was cancelled')) 'cancellation stops in-flight work, restores the UI lease, and reports failed evidence'
Assert-VisualCapture ($state.Contains('waiting.Count + (active == null ? 0 : 1)') -and
    $state.Contains('Only the exact in-flight screenshot request may be completed.')) 'queued and in-flight screenshots remain pending until exact completion'
Assert-VisualCapture ($state.Contains('UMM overlay UI.Instance is missing at screenshot capture.') -and
    $state.Contains('UMM overlay remained open at screenshot capture.')) 'capture fails closed when the UMM overlay is missing or open'
Assert-VisualCapture ($state.Contains('overlay.Opened != originallyOpened') -and
    $state.Contains('UMM overlay did not return to its exact captured open state.')) 'overlay lease verifies restoration to the exact captured state'
Assert-VisualCapture ($engine.Contains('screenshotCapture.Pump(frameNumber);') -and
    $engine.Contains('screenshotCapture.PendingCount') -and
    $engine.Contains('assertions.Fail(error);')) 'movement rows wait for asynchronous captures and turn capture errors into row failures'
Assert-VisualCapture (-not $engine.Contains('Screenshot.CapturePNG') -and
    -not $engine.Contains('pendingScreenshots') -and
    -not $engine.Contains('FlushReadyScreenshots')) 'movement engine cannot capture from Update or remove work before render-boundary completion'
$captureAllowlistStart = $engine.IndexOf('private static readonly HashSet<string> CaptureMilestones', [StringComparison]::Ordinal)
$captureAllowlistEnd = $engine.IndexOf('};', $captureAllowlistStart, [StringComparison]::Ordinal)
$captureAllowlist = if ($captureAllowlistStart -ge 0 -and $captureAllowlistEnd -gt $captureAllowlistStart) {
    $engine.Substring($captureAllowlistStart, $captureAllowlistEnd - $captureAllowlistStart)
}
else {
    ''
}
Assert-VisualCapture ($captureAllowlist.Contains('"pose-stop-motion"')) 'stop-motion is an explicitly allowlisted presentation capture milestone'
$stopCaptureDecisionIndex = $engine.IndexOf('var stopCaptureDecision = stopEarlyCaptureBoundary.Observe(true, navigationMovingCaptureTaken);', [StringComparison]::Ordinal)
$stopCaptureMilestoneIndex = $engine.IndexOf('CaptureMilestone(navigationMilestone);', $stopCaptureDecisionIndex, [StringComparison]::Ordinal)
$stopCaptureWaitIndex = $engine.IndexOf('if (stopCaptureDecision != StopEarlyCaptureDecision.Stop)', $stopCaptureDecisionIndex, [StringComparison]::Ordinal)
$stopCaptureReturnIndex = $engine.IndexOf('return false;', $stopCaptureWaitIndex, [StringComparison]::Ordinal)
$authoritativeStopIndex = $engine.IndexOf('RequireSelectionManager().Stop();', $stopCaptureDecisionIndex, [StringComparison]::Ordinal)
Assert-VisualCapture ($engine.Contains('stopEarlyCaptureBoundary.Reset();') -and
    $stopCaptureDecisionIndex -ge 0 -and
    $stopCaptureMilestoneIndex -gt $stopCaptureDecisionIndex -and
    $stopCaptureWaitIndex -gt $stopCaptureMilestoneIndex -and
    $stopCaptureReturnIndex -gt $stopCaptureWaitIndex -and
    $authoritativeStopIndex -gt $stopCaptureReturnIndex) 'stop-early rows queue a missing moving milestone and retain a render boundary before authoritative stop'
$uiOverlaySnapshotIndex = $engine.IndexOf('rowOverlayEvidence = new PresentationOverlayEvidence(', [StringComparison]::Ordinal)
$uiOverlayCleanupIndex = $engine.IndexOf('BeginCleanup(CleanupTrigger.Manual);', $uiOverlaySnapshotIndex, [StringComparison]::Ordinal)
$uiOverlayPublishIndex = $engine.IndexOf('uiOverlayLabel = rowOverlayEvidence.Label,', $uiOverlayCleanupIndex, [StringComparison]::Ordinal)
Assert-VisualCapture ($uiOverlaySnapshotIndex -ge 0 -and
    $uiOverlayCleanupIndex -gt $uiOverlaySnapshotIndex -and
    $uiOverlayPublishIndex -gt $uiOverlayCleanupIndex -and
    $engine.Contains('uiOverlayEnabled = rowOverlayEvidence.Enabled,') -and
    $engine.Contains('uiOverlayVisible = rowOverlayEvidence.Visible,') -and
    $engine.Contains('uiOverlayButtonActivationCount = rowOverlayEvidence.ButtonActivationCount,') -and
    -not $engine.Contains('uiOverlayLabel = playerAction.LastOverlayLabel,') -and
    -not $engine.Contains('uiOverlayEnabled = playerAction.LastOverlayEnabled,') -and
    -not $engine.Contains('uiOverlayVisible = playerAction.LastOverlayVisible,')) 'UI row snapshots mounted overlay evidence before cleanup and never republishes mutable post-cleanup live state'
$poseConfigureStart = $poseAdapter.IndexOf('public void Configure(', [StringComparison]::Ordinal)
$poseConfigureEnd = $poseAdapter.IndexOf('internal static bool TryValidateSupportedSurface(', $poseConfigureStart, [StringComparison]::Ordinal)
$poseConfigure = if ($poseConfigureStart -ge 0 -and $poseConfigureEnd -gt $poseConfigureStart) {
    $poseAdapter.Substring($poseConfigureStart, $poseConfigureEnd - $poseConfigureStart)
}
else {
    ''
}
$poseAcquireIndex = $poseConfigure.IndexOf('baselineLease.Acquire(', [StringComparison]::Ordinal)
$posePrimeIndex = $poseConfigure.IndexOf('PrimeTimedPosePath();', [StringComparison]::Ordinal)
$poseEvidenceResetIndex = $poseConfigure.IndexOf('PoseApplicationFrameCount = 0;', [StringComparison]::Ordinal)
$poseConfiguredIndex = $poseConfigure.IndexOf('configured = true;', [StringComparison]::Ordinal)
$posePrimeMethodStart = $poseAdapter.IndexOf('private void PrimeTimedPosePath()', [StringComparison]::Ordinal)
$posePrimeMethodEnd = $poseAdapter.IndexOf('internal static bool TryValidateSupportedSurface(', $posePrimeMethodStart, [StringComparison]::Ordinal)
$posePrimeMethod = if ($posePrimeMethodStart -ge 0 -and $posePrimeMethodEnd -gt $posePrimeMethodStart) {
    $poseAdapter.Substring($posePrimeMethodStart, $posePrimeMethodEnd - $posePrimeMethodStart)
}
else {
    ''
}
Assert-VisualCapture ($poseAcquireIndex -ge 0 -and
    $posePrimeIndex -gt $poseAcquireIndex -and
    $poseEvidenceResetIndex -gt $posePrimeIndex -and
    $poseConfiguredIndex -gt $poseEvidenceResetIndex -and
    $posePrimeMethod.Contains('var started = Stopwatch.GetTimestamp();') -and
    $posePrimeMethod.Contains('baselineLease.PrimeFrame(ApplyPose);') -and
    $posePrimeMethod.Contains('TicksToMicroseconds(Stopwatch.GetTimestamp() - started)')) 'pose cold path is reversibly primed before per-frame evidence counters and configuration become active'
Assert-VisualCapture ($manualReview.Contains('if (!ValidateReadOnlyBoundary())') -and
    $manualReview.Contains('saveAuthorization.AuthorizedWriteCount != 0') -and
    $manualReview.Contains('relationship.MountAutomationPair()') -and
    $manualReview.Contains('runtime.PoseFrameApplied') -and
    $manualReview.Contains('VisualAcceptance = "PENDING"') -and
    $manualReview.Contains('relationship.Dismount(CleanupTrigger.ProcessTeardown)') -and
    $manualReview.Contains('ManualReviewBoundaryDecision.BeginProcessTeardown') -and
    $manualReview.Contains('ManualReviewFixtureBoundary.Invalid') -and
    $composition.Contains('runtimeAutomation != null && !runtimeAutomation.IsManualReview')) 'manual review establishes exact mounted pose/UI state without writes or unbounded telemetry and retains process-teardown cleanup'
$horseDismountIndex = $horseEngine.IndexOf('private void AwaitMountedDismount()', [StringComparison]::Ordinal)
$horseReplacementIndex = $horseEngine.IndexOf('BeginMountedLifecycleTargetReplacement();', $horseDismountIndex, [StringComparison]::Ordinal)
$horseReplacementMethodIndex = $horseEngine.IndexOf('private void BeginMountedLifecycleTargetReplacement()', $horseReplacementIndex, [StringComparison]::Ordinal)
$horseOldTargetDestroyIndex = $horseEngine.IndexOf('targetService.DestroyAndVerify()', $horseReplacementMethodIndex, [StringComparison]::Ordinal)
$horseFreshTargetIndex = $horseEngine.IndexOf('request.RunId + "-lifecycle"', $horseOldTargetDestroyIndex, [StringComparison]::Ordinal)
$horseLifecycleReadyIndex = $horseEngine.IndexOf('private void AwaitLifecycleCombatEntry()', $horseFreshTargetIndex, [StringComparison]::Ordinal)
$horseDeathProbeIndex = $horseEngine.IndexOf('BeginDeathProbe();', $horseLifecycleReadyIndex, [StringComparison]::Ordinal)
Assert-VisualCapture ($horseDismountIndex -ge 0 -and
    $horseReplacementIndex -gt $horseDismountIndex -and
    $horseReplacementMethodIndex -gt $horseReplacementIndex -and
    $horseOldTargetDestroyIndex -gt $horseReplacementMethodIndex -and
    $horseFreshTargetIndex -gt $horseOldTargetDestroyIndex -and
    $horseLifecycleReadyIndex -gt $horseFreshTargetIndex -and
    $horseDeathProbeIndex -gt $horseLifecycleReadyIndex -and
    $horseEngine.Contains('The fresh exact hostile lifecycle attacker lost admission before its stock attack.')) 'mounted Horse lifecycle retires the spent combat target, admits one fresh hostile stock attacker, and fails closed on lease loss'
$cameraResolveIndex = $coordinator.IndexOf('var camera = Game.GetCamera();', [StringComparison]::Ordinal)
$cameraGuardIndex = $coordinator.IndexOf('if (!camera)', $cameraResolveIndex, [StringComparison]::Ordinal)
$captureIndex = $coordinator.IndexOf('Screenshot.CapturePNG(camera);', [StringComparison]::Ordinal)
$captureValidationIndex = $coordinator.LastIndexOf('activeLease.VerifyCaptureReady();', $captureIndex, [StringComparison]::Ordinal)
$captureRestorationIndex = $coordinator.IndexOf('activeLease?.Dispose();', $captureIndex, [StringComparison]::Ordinal)
Assert-VisualCapture ([regex]::Matches($coordinator, 'Screenshot\.CapturePNG\(').Count -eq 1 -and
    $cameraResolveIndex -ge 0 -and $cameraResolveIndex -lt $cameraGuardIndex -and
    $cameraGuardIndex -lt $captureIndex -and
    $coordinator.Contains('Kingmaker gameplay camera is missing at screenshot capture.') -and
    $captureValidationIndex -ge 0 -and $captureValidationIndex -lt $captureIndex -and
    $captureRestorationIndex -gt $captureIndex) 'single screenshot call resolves and guards the gameplay camera and is bracketed by overlay validation and exact-state restoration'

Assert-VisualCapture ($poseAdapter.Contains('private readonly AnimatedSaddlePosition animatedSaddle') -and
    $poseAdapter.Contains('saddleSource.IsChildOf(saddleMountRoot)') -and
    $poseAdapter.Contains('saddleMountRoot.InverseTransformPoint(saddleSource.position)') -and
    $poseAdapter.Contains('pelvis.position = LastAnimatedSeatPosition;') -and
    $poseAdapter.Contains('animatedSaddle.Release();') -and
    -not $poseAdapter.Contains('EntityData.Position =') -and
    -not $poseAdapter.Contains('saddleSource.rotation')) 'Horse animated seating leases visual pelvis position with exact source health and no mechanics or bone-quaternion inheritance'

$motion = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\HorseMotionEvidenceRecorder.cs'))
$automation = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeAutomationHost.cs'))
Assert-VisualCapture ($composition.Contains('runtimeAutomation.RequiresLegacyDiagnosticOverlay') -and
    -not $composition.Contains('settings.EnableDiagnosticOverlay || runtimeAutomation != null') -and
    $automation.Contains('request.Scenario != Phase3dHorseScenarioTranche.RealTimeScenario') -and
    $automation.Contains('request.Scenario != Phase3dHorseScenarioTranche.PresentationScenario') -and
    $automation.Contains('request.Scenario != "mounted-mammoth-primary-hit-rt"') -and
    $automation.Contains('request.Scenario != "combat-lifecycle-suite"')) 'exact Phase 3F runtime scenarios honor overlay-off configuration while historical overlay fixtures remain explicit'
$tranche = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\Phase3dHorseScenarioTranche.cs'))
Assert-VisualCapture ($tranche.Contains('if (IsPhase3fNativeControlScope)') -and
    $tranche -match '(?s)if \(IsPhase3fNativeControlScope\)\s*\{[^}]*BeginRtCombatDismount\(\);\s*return;\s*\}\s*BeginRtToTbTransition\(\);' -and
    $tranche.Contains('["schemaVersion"] = IsOrdinaryAttackControls ? 1 : IsPhase3hLoop ? 9 : IsPhase3gControls ? 8 : IsPhase3fNativeControlScope ? 7 : 6,')) 'C0 RT fixture bypasses all legacy shared-turn exercises before any mode transition'
Assert-VisualCapture ($motion.Contains('MaximumFrames = 160') -and
    $motion.Contains('MaximumFramesPerPhase = 24') -and
    $motion.Contains('phase.Contains("-horse-") ? MaximumFramesPerPhase : 8') -and
    $motion.Contains('clock.ElapsedMilliseconds + 80') -and
    $motion.Contains('new MovementScreenshotCaptureCoordinator(Commit, Fail, logger)') -and
    $motion.Contains('FileMode.CreateNew') -and
    $motion.Contains('algorithm.ComputeHash(bytes)') -and
    $motion.Contains('relationship.CapturePresentationObservation(false)') -and
    $tranche.Contains('motionEvidence.Dispose();') -and
    $tranche.Contains('observations["phase3fMotionFrames"] = motionEvidence.Snapshot();') -and
    -not $motion.Contains('SelectedUnits') -and -not $motion.Contains('Commands.') -and
    -not $motion.Contains('IsPaused =')) 'Horse motion evidence is bounded, hashed, after rendering, disposed and independent of gameplay driving'
Assert-VisualCapture ($poseAdapter.Contains('internal double? ObserveCurrentAnimatedSeatResidual()') -and
    $poseAdapter.Contains('Vector3.Distance(pelvis.position, saddleMountRoot.TransformPoint(ToUnity(seat)))')) 'render-time seat observation measures current cached source and pelvis without rewriting either'

Write-Host "TOTAL PASS=$passes FAIL=$($failures.Count)"
if ($failures.Count -ne 0) {
    exit 1
}
