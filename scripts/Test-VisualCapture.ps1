[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$coordinatorPath = Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\MovementScreenshotCaptureCoordinator.cs'
$statePath = Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\MovementScreenshotCaptureState.cs'
$enginePath = Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeMovementScenarioEngine.cs'
$projectPath = Join-Path $repoRoot 'src\KingmakerMountedCombat\KingmakerMountedCombat.csproj'
$coordinator = [IO.File]::ReadAllText($coordinatorPath)
$state = [IO.File]::ReadAllText($statePath)
$engine = [IO.File]::ReadAllText($enginePath)
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
    $project.Contains('Diagnostics\MovementScreenshotCaptureState.cs')) 'production project includes the isolated screenshot coordinator and state machine'
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

Write-Host "TOTAL PASS=$passes FAIL=$($failures.Count)"
if ($failures.Count -ne 0) {
    exit 1
}
