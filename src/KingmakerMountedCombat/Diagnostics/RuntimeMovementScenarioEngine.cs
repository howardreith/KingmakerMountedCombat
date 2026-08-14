using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Frame-driven, save-independent-in-process executor for the eight Phase 1
    /// movement rows. The containing harness owns fixture and Mods restoration;
    /// this type never invokes a save API and changes only movement, selection,
    /// pause, and the explicitly scoped mounted relationship.
    /// </summary>
    internal sealed class RuntimeMovementScenarioEngine : IDisposable
    {
        // The host has a 300-second monotonic deadline and the launcher allows
        // another bounded exit/restoration window. Eight rows can legitimately
        // include several 12-second path legs, so keep a useful cleanup margin
        // without pre-empting the ordinary suite envelope.
        private const double SuiteTimeoutSeconds = 250.0d;
        private const double RowTimeoutSeconds = 42.0d;
        private const double PathProbeTimeoutSeconds = 4.0d;
        private const double MovementTimeoutSeconds = 12.0d;
        private const double StableWindowSeconds = 0.75d;
        private const double PauseObservationSeconds = 1.0d;
        private const float MinimumRadialDistance = 5.0f;
        private const float MaximumRadialDistance = 11.0f;
        private const float EndpointTolerance = 1.5f;
        private const float ReachTolerance = 1.25f;
        private const float StationaryTolerance = 0.15f;
        private const double MaximumPostCorrectionRotationResidualDegrees = 0.10d;
        private const int MaximumOscillations = 2;
        private const int MaximumUnexpectedRepaths = 2;

        private static readonly string[] SuiteRows =
        {
            // Door selection is bounded to nearby geometry. Run its matched
            // unmounted control before any earlier row can move the fixture away
            // from the authored doorway.
            "mounted-pair-doorway",
            "mounted-pair-open-ground",
            "mounted-pair-stop-start",
            "mounted-pair-turns-and-corners",
            "mounted-pair-selection",
            "mounted-pair-party-formation",
            "mounted-pair-pause-unpause",
            "mounted-pair-destination-cancel"
        };

        private static readonly HashSet<string> CaptureMilestones = new HashSet<string>(StringComparer.Ordinal)
        {
            "mounted-idle",
            "moving",
            "stopped",
            "restarted",
            "corner",
            "door-control",
            "door-mounted",
            "selection",
            "formation",
            "paused",
            "cancelled",
            "dismounted"
        };

        private static readonly JsonSerializerSettings EvidenceJsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        private readonly RuntimeRequest request;
        private readonly GameMountedRelationshipService relationship;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly string evidenceRoot;
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly List<ScreenshotEvidence> screenshots = new List<ScreenshotEvidence>();
        private readonly List<string> captureErrors = new List<string>();
        private readonly Dictionary<string, int> captureCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly MovementScreenshotCaptureCoordinator screenshotCapture;
        private readonly List<UnitEntityData> touchedUnits = new List<UnitEntityData>();
        private readonly Stopwatch suiteClock = new Stopwatch();
        private readonly Stopwatch rowClock = new Stopwatch();
        private readonly Stopwatch phaseClock = new Stopwatch();

        private IReadOnlyList<string> selectedRows;
        private AssertionRecorder assertions;
        private SelectionSnapshot selectionSnapshot;
        private PairSnapshot pairSnapshot;
        private NonPairSnapshot nonPairSnapshot;
        private CleanupStateEvidence cleanupBefore;
        private CleanupStateEvidence cleanupAfter;
        private StreamWriter evidenceWriter;
        private UnitEntityData rider;
        private UnitEntityData mount;
        private UnitEntityData nonPairUnit;
        private string currentRow;
        private int rowIndex;
        private int rowPhase;
        private int frameNumber;
        private int cleanupFrame;
        private long evidenceSequence;
        private EngineStep step;
        private bool originalUnsafeMovementSetting;
        private bool settingLeaseOwned;
        private bool originalPause;
        private bool pauseLeaseOwned;
        private bool started;
        private bool completed;
        private bool disposed;
        private bool fatalResidue;
        private bool abortAfterVerifiedCleanup;
        private CleanupTrigger pendingCleanupTrigger;
        private string cleanupResult;
        private bool cleanupAttemptSucceeded;
        private bool cleanupResidual;

        private readonly List<Vector3> probeCandidates = new List<Vector3>();
        private readonly List<string> probeRejections = new List<string>();
        private int probeIndex;
        private int probeGeneration;
        private bool probePending;
        private bool probeCallbackReady;
        private bool probeCallbackAccepted;
        private string probeCallbackReason;
        private Vector3 probeRequested;
        private Vector3 probeEndpoint;
        private double probePathLength;
        private Func<Vector3, bool> probeDirectionFilter;
        private bool probeDoorStrict;

        private NavigationMode navigationMode;
        private NavigationStage navigationStage;
        private Vector3 navigationDestination;
        private Vector3 navigationStart;
        private Vector3 navigationStablePosition;
        private object navigationCommand;
        private object navigationPath;
        private double navigationStartedAt;
        private double navigationLastProgressAt;
        private double navigationStableStartedAt;
        private double navigationBestDistance;
        private double navigationPreviousDistance;
        private double navigationMovedDistance;
        private double navigationMaximumStationaryDrift;
        private double navigationMaximumStuckSeconds;
        private int navigationAwayFrameRun;
        private bool navigationWasApproaching;
        private int navigationOscillations;
        private int navigationRepaths;
        private int navigationCommandReplacements;
        private int navigationSelectionLosses;
        private bool navigationMovingCaptureTaken;
        private bool navigationPauseRequested;
        private double navigationPauseStartedAt;
        private Vector3 navigationPausePosition;
        private double navigationPauseDrift;
        private object nonPairCommand;
        private Vector3 nonPairStart;
        private Vector3 nonPairTarget;
        private double nonPairMovedDistance;
        private double nonPairBestTargetDistance;
        private double nonPairFinalTargetDistance;
        private double mountFinalTargetDistance;
        private double minimumPairNonPairSeparation;
        private double requiredPairNonPairSeparation;
        private Dictionary<UnitEntityData, object> uninvolvedCommands;

        private double rowMaximumPreCorrectionResidual;
        private double rowMaximumInitialConfigurationResidual;
        private double rowMaximumUpdatePreCorrectionResidual;
        private double rowMaximumLateUpdatePreCorrectionResidual;
        private double rowMaximumUpdatePreCorrectionRotation;
        private double rowMaximumLateUpdatePreCorrectionRotation;
        private double rowMaximumPostCorrectionResidual;
        private double rowMaximumPostCorrectionRotation;
        private double rowMaximumRawCurrentPositionResidual;
        private double rowMaximumUpdateRawCurrentPositionResidual;
        private double rowMaximumLateUpdateRawCurrentPositionResidual;
        private double rowMaximumViewCurrentPositionResidual;
        private double rowMaximumEntityRawCurrentPositionResidual;
        private double rowMaximumEntityPreviousAuthoritativePositionResidual;
        private double rowMaximumEntityPhaseAdjustedPositionResidual;
        private double rowMaximumAuthoritativePositionDelta;
        private double rowMaximumEntityRawPositionLagExcess;
        private long rowPositionPhaseLagObservedCount;
        private long rowPositionPhaseLagPermittedCount;
        private long rowPositionPhaseLagSameFrameUpdateReferenceCount;
        private long rowPositionPhaseLagEligibleReferenceCount;
        private long rowPositionPhaseLagViolationCount;
        private long rowPositionPhaseLagRecoveryRequiredCount;
        private long rowPositionPhaseLagRecoveryUpdateCount;
        private long rowPositionPhaseLagRecoverySatisfiedCount;
        private long rowPositionPhaseLagRecoveryRequiredEffectiveCount;
        private long rowPositionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount;
        private long rowPositionPhaseLagRecoverySatisfiedEffectiveCount;
        private long rowPositionPhaseLagRecoveryViolationCount;
        private long rowStationaryPositionCorrectionViolationCount;
        private long rowOutstandingPositionPhaseLagRecoveryCount;
        private long rowMaximumConsecutiveUnrecoveredPositionPhaseLagCount;
        private double rowMaximumViewCurrentYawResidual;
        private double rowMaximumFullViewCurrentRotationResidual;
        private double rowMaximumMountEntityRootYawResidual;
        private double rowMaximumEntityRawCurrentYawResidual;
        private double rowMaximumEntityPreviousAuthoritativeYawResidual;
        private double rowMaximumEntityPhaseAdjustedYawResidual;
        private double rowMaximumAuthoritativeYawDelta;
        private double rowMaximumEntityRawLagExcess;
        private long rowPhaseLagObservedCount;
        private long rowPhaseLagPermittedCount;
        private long rowPhaseLagSameFrameUpdateReferenceCount;
        private long rowPhaseLagEligibleReferenceCount;
        private long rowPhaseLagViolationCount;
        private long rowPhaseLagRecoveryRequiredCount;
        private long rowPhaseLagRecoveryUpdateCount;
        private long rowPhaseLagRecoverySatisfiedCount;
        private long rowPhaseLagRecoveryRequiredEffectiveCount;
        private long rowPhaseLagRecoveryUpdateOrBoundaryEffectiveCount;
        private long rowPhaseLagRecoverySatisfiedEffectiveCount;
        private long rowPhaseLagRecoveryViolationCount;
        private long rowStationaryYawCorrectionViolationCount;
        private long rowOutstandingPhaseLagRecoveryCount;
        private long rowMaximumConsecutiveUnrecoveredPhaseLagCount;
        private long rowStationaryBoundaryClosureAttemptCount;
        private long rowStationaryBoundaryClosureSucceededCount;
        private long rowStationaryBoundaryClosureFailedCount;
        private long rowYawPhaseLagStationaryBoundaryClosureCount;
        private long rowPositionPhaseLagStationaryBoundaryClosureCount;
        private bool finalSynchronizationSnapshotCaptured;
        private long finalSynchronizationSnapshotFrame;
        private long finalSynchronizationAgentFrame;
        private long finalSynchronizationSampleCount;
        private long finalSynchronizationOutstandingRecoveryCount;
        private long finalSynchronizationOutstandingPositionRecoveryCount;
        private bool finalSynchronizationQualificationPassed;
        private double finalSynchronizationBoundaryPositionResidual;
        private double finalSynchronizationBoundaryFullViewRotationResidual;
        private double finalSynchronizationBoundaryViewYawResidual;
        private double finalSynchronizationBoundaryEntityCurrentYawResidual;
        private double finalSynchronizationBoundaryMountEntityRootYawResidual;
        private double finalSynchronizationBoundaryViewPositionResidual;
        private double finalSynchronizationBoundaryEntityPositionResidual;
        private double finalSynchronizationBoundaryAuthoritativePositionAdvance;
        private double finalSynchronizationBoundaryAuthoritativeYawAdvance;
        private bool finalSynchronizationBoundaryMovementCommandAbsent;
        private bool finalSynchronizationBoundaryWantsToMove;
        private bool finalSynchronizationBoundaryIsReallyMoving;
        private bool finalSynchronizationBoundaryClosureAttempted;
        private bool finalSynchronizationBoundaryClosureSucceeded;
        private string finalSynchronizationBoundaryClosureReason;
        private long finalSynchronizationBoundaryYawPendingBefore;
        private long finalSynchronizationBoundaryPositionPendingBefore;
        private long finalSynchronizationBoundaryYawClosedCount;
        private long finalSynchronizationBoundaryPositionClosedCount;
        private long finalSynchronizationBoundaryYawPendingAfter;
        private long finalSynchronizationBoundaryPositionPendingAfter;
        private bool cleanupMovementStoppedBeforeFinalSynchronization;
        private double rowMaximumStationaryDrift;
        private double rowMaximumStuckSeconds;
        private int rowOscillations;
        private int rowUnexpectedRepaths;
        private int rowCommandReplacements;
        private int rowSelectionLosses;
        private int rowWaypointCount;
        private int rowEndpointQualifiedWaypointCount;
        private double rowMaximumCompletedLegFinalTargetDistance;
        private double rowMaximumCompletedLegBestTargetDistance;
        private double rowMaximumTurnDegrees;
        private int rowNonPairInterferenceCount;
        private long rowSynchronizationObservationCount;
        private long rowUpdateSampleCount;
        private long rowLateUpdateSampleCount;
        private long rowUpdateCorrectionCount;
        private long rowLateUpdateCorrectionCount;
        private bool rowSynchronizationFailureRecorded;
        private bool rowUnmountedDoorControlPassed;
        private bool rowDoorApproachSkipped;
        private int rowStopCommandIssuedCount;
        private bool rowRestartCompleted;
        private bool rowSelectionMountNormalized;
        private bool rowSelectionSwitchedAway;
        private bool rowSelectionSwitchedBack;
        private bool rowFormationSelectionNormalized;
        private bool rowPauseEntered;
        private double rowPauseObservationSeconds;
        private double rowPauseMaximumDrift;
        private bool rowPauseExited;
        private bool rowDestinationCancelCommandAbsent;
        private bool rowDestinationCancelRelationshipPreserved;
        private Vector3 previousLegDirection;
        private StandardDoor selectedDoor;
        private Vector3 doorNearPoint;
        private Vector3 doorFarPoint;

        public RuntimeMovementScenarioEngine(
            RuntimeRequest request,
            GameMountedRelationshipService relationship,
            DiagnosticSettings settings,
            IModLogger logger,
            string evidenceRoot)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrWhiteSpace(evidenceRoot))
            {
                throw new ArgumentException("Movement evidence root is required.", nameof(evidenceRoot));
            }
            this.evidenceRoot = Path.GetFullPath(evidenceRoot);
            screenshotCapture = new MovementScreenshotCaptureCoordinator(CommitScreenshot, RecordScreenshotFailure, logger);
        }

        public bool IsCompleted => completed;

        public IReadOnlyList<RuntimeSubscenarioResult> Results => results;

        public IReadOnlyList<string> Errors => errors;

        internal string CurrentRow => currentRow;

        internal static bool SupportsScenario(string scenario)
        {
            return SelectRows(scenario) != null;
        }

        public void Start()
        {
            ThrowIfDisposed();
            if (started)
            {
                throw new InvalidOperationException("Movement scenario engine has already started.");
            }

            selectedRows = SelectRows(request.Scenario);
            started = true;
            if (selectedRows == null)
            {
                errors.Add("Scenario is not movement-suite or an exact Phase 1 movement row: " + request.Scenario + ".");
                completed = true;
                return;
            }

            if (!Directory.Exists(evidenceRoot))
            {
                throw new DirectoryNotFoundException("Harness-owned evidence root does not exist: " + evidenceRoot);
            }

            var evidencePath = Path.Combine(evidenceRoot, "movement-scenario-evidence.jsonl");
            evidenceWriter = new StreamWriter(new FileStream(evidencePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new System.Text.UTF8Encoding(false));
            originalUnsafeMovementSetting = settings.EnableUnsafeMovementExperiment;
            settings.EnableUnsafeMovementExperiment = true;
            settingLeaseOwned = true;
            suiteClock.Start();
            step = EngineStep.BeginRow;
            logger.Info("Movement runtime engine started for " + request.Scenario + ".");
        }

        public void Update()
        {
            ThrowIfDisposed();
            if (!started)
            {
                throw new InvalidOperationException("Movement scenario engine must be started before Update.");
            }
            if (completed)
            {
                return;
            }

            frameNumber++;
            try
            {
                screenshotCapture.Pump(frameNumber);
                if (suiteClock.Elapsed.TotalSeconds > SuiteTimeoutSeconds && !abortAfterVerifiedCleanup)
                {
                    const string timeoutMessage = "Movement suite exceeded its bounded monotonic deadline.";
                    if (currentRow == null)
                    {
                        CompleteRemainingAsNotRun(timeoutMessage);
                        Complete();
                        return;
                    }

                    FailCurrent(timeoutMessage);
                    abortAfterVerifiedCleanup = true;
                    if (step == EngineStep.BeginRow || step == EngineStep.ExecuteRow)
                    {
                        BeginCleanup(CleanupTrigger.Exception);
                        return;
                    }
                }
                if (currentRow != null && rowClock.Elapsed.TotalSeconds > RowTimeoutSeconds && step == EngineStep.ExecuteRow)
                {
                    FailCurrent("Movement row exceeded its " + RowTimeoutSeconds.ToString("0", CultureInfo.InvariantCulture) + " second monotonic deadline.");
                    BeginCleanup(CleanupTrigger.Exception);
                    return;
                }

                switch (step)
                {
                    case EngineStep.BeginRow:
                        BeginRow();
                        break;
                    case EngineStep.ExecuteRow:
                        AdvanceCurrentRow();
                        break;
                    case EngineStep.AwaitPreCleanupCaptures:
                        ContinueCleanupAfterCaptures();
                        break;
                    case EngineStep.AwaitCleanupFrame:
                        VerifyCleanupAndFinishRow();
                        break;
                    case EngineStep.AwaitFinalCaptures:
                        FinishRowAfterCaptures();
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected movement engine step: " + step + ".");
                }
            }
            catch (Exception exception)
            {
                logger.Exception("Movement runtime row threw", exception);
                FailCurrent(exception.GetType().Name + ": " + exception.Message);
                BeginCleanup(CleanupTrigger.Exception);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            try
            {
                screenshotCapture.Dispose();
                StopTouchedMovement();
                BestEffortDismount(CleanupTrigger.ProcessTeardown);
                RestorePause();
                RestoreSelection();
            }
            catch (Exception exception)
            {
                errors.Add("Movement-engine dispose cleanup threw " + exception.GetType().Name + ": " + exception.Message);
                logger.Exception("Movement runtime dispose cleanup threw", exception);
            }
            finally
            {
                CloseEvidenceWriter();
                RestoreSettings();
                suiteClock.Stop();
                rowClock.Stop();
                phaseClock.Stop();
                disposed = true;
            }
        }

        private void BeginRow()
        {
            if (rowIndex >= selectedRows.Count)
            {
                Complete();
                return;
            }

            currentRow = selectedRows[rowIndex];
            assertions = new AssertionRecorder();
            selectionSnapshot = SelectionSnapshot.Capture();
            originalPause = Game.Instance != null && Game.Instance.IsPaused;
            pauseLeaseOwned = true;
            pairSnapshot = null;
            nonPairSnapshot = null;
            cleanupBefore = null;
            cleanupAfter = null;
            cleanupResult = null;
            cleanupAttemptSucceeded = false;
            cleanupResidual = false;
            rider = null;
            mount = null;
            nonPairUnit = null;
            touchedUnits.Clear();
            screenshots.Clear();
            captureErrors.Clear();
            captureCounts.Clear();
            if (screenshotCapture.PendingCount != 0)
            {
                throw new InvalidOperationException("A prior movement row retained pending screenshot work.");
            }
            ResetRowMetrics();
            ResetNavigationMetrics();
            ResetPathProbe();
            navigationStage = NavigationStage.None;
            rowPhase = 0;
            rowClock.Restart();
            phaseClock.Restart();

            assertions.Check(Game.Instance != null && !Game.Instance.Player.IsInCombat,
                "Party remained out of combat.",
                "Movement row cannot run while the party is in combat.");
            assertions.Check(relationship.State == RelationshipState.Unmounted,
                "Relationship began Unmounted.",
                "Relationship began in " + relationship.State + " rather than Unmounted.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup(CleanupTrigger.Exception);
                return;
            }

            string pairError;
            if (!relationship.TryResolveAutomationPair(out rider, out mount, out pairError))
            {
                assertions.Fail("Exact automation pair did not resolve: " + (pairError ?? "unknown error"));
                BeginCleanup(CleanupTrigger.Exception);
                return;
            }

            pairSnapshot = PairSnapshot.Capture(rider, mount);
            assertions.Check(pairSnapshot != null,
                "Exact rider/mount stock state was captured.",
                "Exact rider/mount stock state could not be captured.");
            if (pairSnapshot == null)
            {
                BeginCleanup(CleanupTrigger.Exception);
                return;
            }

            if (!string.Equals(currentRow, "mounted-pair-doorway", StringComparison.Ordinal))
            {
                if (!MountPair())
                {
                    BeginCleanup(CleanupTrigger.Exception);
                    return;
                }
                SelectOnly(rider);
                CaptureMilestone("mounted-idle");
            }

            step = EngineStep.ExecuteRow;
            logger.Info("Movement runtime row started: " + currentRow + ".");
        }

        private void AdvanceCurrentRow()
        {
            if (relationship.State == RelationshipState.Mounted)
            {
                relationship.ValidateActivePair();
                if (relationship.State != RelationshipState.Mounted)
                {
                    FailCurrent("Mounted relationship invalidated during movement; last result: " + relationship.LastResult);
                    BeginCleanup(CleanupTrigger.Exception);
                    return;
                }
                ObserveSynchronization();
            }

            if (string.Equals(currentRow, "mounted-pair-open-ground", StringComparison.Ordinal))
            {
                AdvanceOpenGround();
            }
            else if (string.Equals(currentRow, "mounted-pair-stop-start", StringComparison.Ordinal))
            {
                AdvanceStopStart();
            }
            else if (string.Equals(currentRow, "mounted-pair-turns-and-corners", StringComparison.Ordinal))
            {
                AdvanceTurnsAndCorners();
            }
            else if (string.Equals(currentRow, "mounted-pair-doorway", StringComparison.Ordinal))
            {
                AdvanceDoorway();
            }
            else if (string.Equals(currentRow, "mounted-pair-selection", StringComparison.Ordinal))
            {
                AdvanceSelection();
            }
            else if (string.Equals(currentRow, "mounted-pair-party-formation", StringComparison.Ordinal))
            {
                AdvanceFormation();
            }
            else if (string.Equals(currentRow, "mounted-pair-pause-unpause", StringComparison.Ordinal))
            {
                AdvancePauseUnpause();
            }
            else if (string.Equals(currentRow, "mounted-pair-destination-cancel", StringComparison.Ordinal))
            {
                AdvanceDestinationCancel();
            }
            else
            {
                FailCurrent("Unsupported movement row reached execution: " + currentRow + ".");
                BeginCleanup(CleanupTrigger.Exception);
            }
        }

        private void AdvanceOpenGround()
        {
            if (rowPhase == 0)
            {
                BeginRadialNavigation(NavigationMode.Normal, null, "moving");
                rowPhase = 1;
                return;
            }
            if (rowPhase == 1 && PollNavigation())
            {
                CaptureMilestone("stopped");
                AssertRowMovementQuality();
                BeginCleanup(CleanupTrigger.Manual);
            }
        }

        private void AdvanceStopStart()
        {
            if (rowPhase == 0)
            {
                BeginRadialNavigation(NavigationMode.StopEarly, null, "moving");
                rowPhase = 1;
                return;
            }
            if (rowPhase == 1 && PollNavigation())
            {
                assertions.Check(navigationMovedDistance >= 0.75d,
                    "Mount moved before the routed stop command.",
                    "Mount did not make measurable progress before the routed stop command.");
                CaptureMilestone("stopped");
                var firstDirection = PlanarDirection(navigationStart, navigationDestination);
                BeginRadialNavigation(NavigationMode.Normal, candidate => Vector3.Dot(firstDirection, PlanarDirection(mount.Position, candidate)) < 0.35f, "restarted");
                rowPhase = 2;
                return;
            }
            if (rowPhase == 2 && PollNavigation())
            {
                rowRestartCompleted = navigationMovedDistance >= 1.0d;
                assertions.Check(rowRestartCompleted,
                    "Mount restarted and progressed after stop.",
                    "Mount did not restart after stop.");
                AssertRowMovementQuality();
                BeginCleanup(CleanupTrigger.Manual);
            }
        }

        private void AdvanceTurnsAndCorners()
        {
            if (rowPhase == 0)
            {
                BeginRadialNavigation(NavigationMode.Normal, null, "moving");
                rowPhase = 1;
                return;
            }
            if (rowPhase == 1 && PollNavigation())
            {
                previousLegDirection = PlanarDirection(navigationStart, navigationDestination);
                BeginRadialNavigation(NavigationMode.Normal, candidate =>
                {
                    var direction = PlanarDirection(mount.Position, candidate);
                    var dot = Vector3.Dot(previousLegDirection, direction);
                    return dot > -0.35f && dot < 0.35f;
                }, "corner");
                rowPhase = 2;
                return;
            }
            if (rowPhase == 2 && PollNavigation())
            {
                var direction = PlanarDirection(navigationStart, navigationDestination);
                rowMaximumTurnDegrees = Math.Max(rowMaximumTurnDegrees, Vector3.Angle(previousLegDirection, direction));
                previousLegDirection = direction;
                BeginRadialNavigation(NavigationMode.Normal, candidate => Vector3.Dot(previousLegDirection, PlanarDirection(mount.Position, candidate)) < -0.55f, "corner");
                rowPhase = 3;
                return;
            }
            if (rowPhase == 3 && PollNavigation())
            {
                var direction = PlanarDirection(navigationStart, navigationDestination);
                rowMaximumTurnDegrees = Math.Max(rowMaximumTurnDegrees, Vector3.Angle(previousLegDirection, direction));
                assertions.Check(rowMaximumTurnDegrees >= 75.0d,
                    "Mounted route completed a substantial turn/reversal.",
                    "Maximum measured turn was only " + rowMaximumTurnDegrees.ToString("0.0", CultureInfo.InvariantCulture) + " degrees.");
                AssertRowMovementQuality();
                BeginCleanup(CleanupTrigger.Manual);
            }
        }

        private void AdvanceDoorway()
        {
            if (rowPhase == 0)
            {
                DoorCandidate candidate;
                string reason;
                if (!TrySelectOpenDoorCandidate(out candidate, out reason))
                {
                    assertions.Fail(reason);
                    BeginCleanup(CleanupTrigger.Manual);
                    return;
                }
                selectedDoor = candidate.Door;
                doorNearPoint = candidate.Near;
                doorFarPoint = candidate.Far;
                SelectOnly(mount);
                rowDoorApproachSkipped = PlanarDistance(mount.Position, doorNearPoint) < MinimumRadialDistance - 1.0f;
                if (rowDoorApproachSkipped)
                {
                    BeginExactNavigation(NavigationMode.UnmountedControl, doorFarPoint, true, "door-control");
                    rowPhase = 2;
                }
                else
                {
                    BeginExactNavigation(NavigationMode.UnmountedControl, doorNearPoint, false, "door-control");
                    rowPhase = 1;
                }
                return;
            }
            if (rowPhase == 1 && PollNavigation())
            {
                BeginExactNavigation(NavigationMode.UnmountedControl, doorFarPoint, true, "door-control");
                rowPhase = 2;
                return;
            }
            if (rowPhase == 2 && PollNavigation())
            {
                rowUnmountedDoorControlPassed = true;
                assertions.Check(selectedDoor != null && selectedDoor.isActiveAndEnabled && selectedDoor.IsOpen,
                    "Unmounted Mammoth control traversed an unchanged active open StandardDoor.",
                    "Door state changed during the unmounted Mammoth control.");
                if (!MountPair())
                {
                    BeginCleanup(CleanupTrigger.Exception);
                    return;
                }
                SelectOnly(rider);
                CaptureMilestone("door-mounted");
                BeginExactNavigation(NavigationMode.Normal, doorNearPoint, true, "door-mounted");
                rowPhase = 3;
                return;
            }
            if (rowPhase == 3 && PollNavigation())
            {
                assertions.Check(rowUnmountedDoorControlPassed,
                    "Doorway comparison retained its exact unmounted control.",
                    "Mounted doorway run lacked a successful unmounted Mammoth control.");
                assertions.Check(selectedDoor != null && selectedDoor.isActiveAndEnabled && selectedDoor.IsOpen,
                    "Mounted pair traversed the same unchanged active open StandardDoor.",
                    "Selected StandardDoor was no longer active and open after mounted traversal.");
                AssertRowMovementQuality();
                BeginCleanup(CleanupTrigger.Manual);
            }
        }

        private void AdvanceSelection()
        {
            if (rowPhase == 0)
            {
                var selection = RequireSelectionManager();
                selection.SelectUnit(mount.View, true, false, false);
                rowSelectionMountNormalized = selection.SelectedUnits.Count == 1 && selection.SelectedUnits[0] == rider && !selection.SelectedUnits.Contains(mount);
                assertions.Check(rowSelectionMountNormalized,
                    "Selecting the mounted Mammoth normalized to the rider.",
                    "Mounted mount-to-rider selection normalization failed.");
                nonPairUnit = FindIdleNonPairControllable();
                assertions.Check(nonPairUnit != null,
                    "A directly controllable non-pair unit was available for selection switching.",
                    "No directly controllable non-pair unit was available for the required away/back selection switch.");
                if (nonPairUnit == null)
                {
                    BeginCleanup(CleanupTrigger.Manual);
                    return;
                }
                nonPairSnapshot = NonPairSnapshot.Capture(nonPairUnit);
                assertions.Check(nonPairSnapshot != null,
                    "Idle non-pair movement snapshot was captured before selection switching.",
                    "Idle non-pair movement snapshot could not be captured.");
                selection.SelectUnit(nonPairUnit.View, true, false, false);
                rowSelectionSwitchedAway = selection.SelectedUnits.Count == 1 && selection.SelectedUnits[0] == nonPairUnit;
                assertions.Check(rowSelectionSwitchedAway,
                    "Selection switched away from the mounted pair.",
                    "Selection did not switch to the non-pair unit.");
                selection.SelectUnit(rider.View, true, false, false);
                rowSelectionSwitchedBack = selection.SelectedUnits.Count == 1 && selection.SelectedUnits[0] == rider;
                assertions.Check(rowSelectionSwitchedBack,
                    "Selection switched back to the rider.",
                    "Selection did not switch back to the rider.");
                CaptureMilestone("selection");
                BeginRadialNavigation(NavigationMode.Normal, null, "moving");
                rowPhase = 1;
                return;
            }
            if (rowPhase == 1 && PollNavigation())
            {
                var selection = RequireSelectionManager();
                assertions.Check(selection.SelectedUnits.Contains(rider) && !selection.SelectedUnits.Contains(mount),
                    "Rider selection remained stable through routed movement.",
                    "Rider selection was lost or replaced by the mount during movement.");
                AssertRowMovementQuality();
                BeginCleanup(CleanupTrigger.Manual);
            }
        }

        private void AdvanceFormation()
        {
            if (rowPhase == 0)
            {
                nonPairUnit = FindIdleNonPairControllable();
                assertions.Check(nonPairUnit != null,
                    "A directly controllable non-pair formation member was available.",
                    "No directly controllable non-pair unit was available for formation qualification.");
                if (nonPairUnit == null)
                {
                    BeginCleanup(CleanupTrigger.Manual);
                    return;
                }
                nonPairSnapshot = NonPairSnapshot.Capture(nonPairUnit);
                assertions.Check(nonPairSnapshot != null,
                    "Idle non-pair movement snapshot was captured before formation movement.",
                    "Idle non-pair movement snapshot could not be captured.");
                requiredPairNonPairSeparation = Math.Max(0.10d, mount.Corpulence + nonPairUnit.Corpulence);

                RequireSelectionManager().MultiSelect(new[] { rider.View, nonPairUnit.View }, false);
                rowFormationSelectionNormalized = RequireSelectionManager().SelectedUnits.Contains(rider) && RequireSelectionManager().SelectedUnits.Contains(nonPairUnit) && !RequireSelectionManager().SelectedUnits.Contains(mount);
                assertions.Check(rowFormationSelectionNormalized,
                    "Formation selection contains rider and one non-pair unit, not the mount.",
                    "Formation selection was not normalized to rider plus one non-pair unit.");
                uninvolvedCommands = CaptureUninvolvedMoveCommands(nonPairUnit);
                CaptureMilestone("formation");
                BeginRadialNavigation(NavigationMode.Formation, null, "formation");
                rowPhase = 1;
                return;
            }
            if (rowPhase == 1 && PollNavigation())
            {
                assertions.Check(nonPairMovedDistance >= 1.0d,
                    "Selected non-pair formation member made measurable progress.",
                    "Selected non-pair formation member did not make measurable progress.");
                assertions.Check(rowNonPairInterferenceCount == 0,
                    "No unselected non-pair movement command was changed.",
                    "Observed " + rowNonPairInterferenceCount + " unselected non-pair command interference event(s).");
                assertions.Check(mountFinalTargetDistance <= ReachTolerance && navigationBestDistance <= ReachTolerance,
                    "Authoritative mount finished within the calibrated formation target tolerance.",
                    "Authoritative mount formation target distances were final=" + mountFinalTargetDistance.ToString("0.000", CultureInfo.InvariantCulture) +
                    ", best=" + navigationBestDistance.ToString("0.000", CultureInfo.InvariantCulture) + ".");
                assertions.Check(nonPairFinalTargetDistance <= ReachTolerance && nonPairBestTargetDistance <= ReachTolerance,
                    "Selected non-pair member finished within the calibrated formation target tolerance.",
                    "Non-pair formation target distances were final=" + nonPairFinalTargetDistance.ToString("0.000", CultureInfo.InvariantCulture) +
                    ", best=" + nonPairBestTargetDistance.ToString("0.000", CultureInfo.InvariantCulture) + ".");
                assertions.Check(minimumPairNonPairSeparation >= requiredPairNonPairSeparation,
                    "Formation members retained non-overlap clearance derived from both units' corpulence.",
                    "Minimum pair/non-pair separation was " + minimumPairNonPairSeparation.ToString("0.000", CultureInfo.InvariantCulture) +
                    " but combined corpulence requires " + requiredPairNonPairSeparation.ToString("0.000", CultureInfo.InvariantCulture) + ".");
                AssertRowMovementQuality();
                BeginCleanup(CleanupTrigger.Manual);
            }
        }

        private void AdvancePauseUnpause()
        {
            if (rowPhase == 0)
            {
                BeginRadialNavigation(NavigationMode.PauseResume, null, "moving");
                rowPhase = 1;
                return;
            }
            if (rowPhase == 1 && PollNavigation())
            {
                assertions.Check(rowPauseMaximumDrift <= StationaryTolerance,
                    "Mounted pair remained stationary during the real-clock pause window.",
                    "Mounted pair drifted " + rowPauseMaximumDrift.ToString("0.000", CultureInfo.InvariantCulture) + " world units while paused.");
                rowPauseExited = !Game.Instance.IsPaused;
                assertions.Check(rowPauseExited,
                    "Game returned to unpaused state and the mounted move completed.",
                    "Game remained paused after the pause/unpause movement exercise.");
                AssertRowMovementQuality();
                BeginCleanup(CleanupTrigger.Manual);
            }
        }

        private void AdvanceDestinationCancel()
        {
            if (rowPhase == 0)
            {
                BeginRadialNavigation(NavigationMode.StopEarly, null, "moving");
                rowPhase = 1;
                return;
            }
            if (rowPhase == 1 && PollNavigation())
            {
                CaptureMilestone("cancelled");
                rowDestinationCancelCommandAbsent = mount.Commands.Move == null && !mount.View.AgentASP.WantsToMove;
                assertions.Check(rowDestinationCancelCommandAbsent,
                    "SelectionManager.Stop cancelled the authoritative mount destination.",
                    "Authoritative mount retained a movement command or destination after cancellation.");
                assertions.Check(navigationMaximumStationaryDrift <= StationaryTolerance,
                    "Cancelled pair remained stationary.",
                    "Cancelled pair drifted " + navigationMaximumStationaryDrift.ToString("0.000", CultureInfo.InvariantCulture) + " world units.");
                rowDestinationCancelRelationshipPreserved = relationship.State == RelationshipState.Mounted;
                assertions.Check(rowDestinationCancelRelationshipPreserved,
                    "Destination cancellation preserved the mounted relationship.",
                    "Destination cancellation changed relationship state to " + relationship.State + ".");
                AssertRowMovementQuality();
                BeginCleanup(CleanupTrigger.Manual);
            }
        }

        private bool MountPair()
        {
            var result = relationship.MountAutomationPair();
            assertions.Check(result.Succeeded,
                "Exact automation pair mounted.",
                "Exact automation pair mount failed: " + FormatTransitionErrors(result));
            assertions.Check(relationship.State == RelationshipState.Mounted,
                "Relationship entered Mounted.",
                "Relationship state after mount was " + relationship.State + ".");
            if (!result.Succeeded || relationship.State != RelationshipState.Mounted)
            {
                return false;
            }

            var runtime = relationship.Runtime;
            assertions.Check(pairSnapshot.RiderView.AgentASP == pairSnapshot.RiderStockAgent && !pairSnapshot.RiderStockAgent.enabled,
                "Rider exact stock agent is disabled while mounted.",
                "Rider stock movement authority was not disabled.");
            assertions.Check(pairSnapshot.RiderStockAgent.AvoidanceDisabled,
                "Rider avoidance is disabled under the mounted lease.",
                "Rider avoidance lease was not acquired.");
            assertions.Check(pairSnapshot.RiderView.AgentOverride == runtime.MovementAgent && runtime.MovementAgent != null,
                "Rider owns only the scoped synchronization override.",
                "Rider synchronization override is missing or replaced.");
            assertions.Check(runtime.PresentationAttachmentLeaseActive && runtime.RiderParentMatchesAttachment,
                "Rider view owns one scoped root-projected position attachment lease.",
                "Rider view position attachment lease is missing or has the wrong parent.");
            assertions.Check(pairSnapshot.MountView.AgentASP == pairSnapshot.MountStockAgent && pairSnapshot.MountStockAgent.enabled && pairSnapshot.MountView.AgentOverride == null,
                "Mammoth stock agent is the sole authoritative mover.",
                "Mammoth stock movement authority is unavailable or overridden.");
            return true;
        }

        private void BeginRadialNavigation(NavigationMode mode, Func<Vector3, bool> directionFilter, string movingMilestone)
        {
            var candidates = BuildRadialCandidates(mount.Position, mount.Orientation);
            BeginNavigation(mode, candidates, directionFilter, false, movingMilestone);
        }

        private void BeginExactNavigation(NavigationMode mode, Vector3 destination, bool strictDoorPath, string movingMilestone)
        {
            BeginNavigation(mode, new[] { destination }, null, strictDoorPath, movingMilestone);
        }

        private void BeginNavigation(NavigationMode mode, IEnumerable<Vector3> candidates, Func<Vector3, bool> directionFilter, bool strictDoorPath, string movingMilestone)
        {
            if (navigationStage != NavigationStage.None && navigationStage != NavigationStage.Complete)
            {
                throw new InvalidOperationException("A navigation leg is already active.");
            }
            ResetNavigationMetrics();
            navigationMode = mode;
            navigationStage = NavigationStage.Searching;
            probeCandidates.Clear();
            probeCandidates.AddRange(candidates.Where(candidate => directionFilter == null || directionFilter(candidate)));
            probeDirectionFilter = directionFilter;
            probeDoorStrict = strictDoorPath;
            probeIndex = 0;
            probeGeneration++;
            probePending = false;
            probeCallbackReady = false;
            probeRejections.Clear();
            phaseClock.Restart();
            navigationMovingCaptureTaken = false;
            navigationMilestone = movingMilestone;
            if (probeCandidates.Count == 0)
            {
                FailCurrent("No deterministic navigation candidate satisfied the requested direction constraint.");
                navigationStage = NavigationStage.Failed;
            }
        }

        private string navigationMilestone;

        private bool PollNavigation()
        {
            if (navigationStage == NavigationStage.Failed)
            {
                BeginCleanup(CleanupTrigger.Exception);
                return false;
            }

            if (navigationStage == NavigationStage.Searching)
            {
                if (!PollPathProbe())
                {
                    return false;
                }
                navigationDestination = probeEndpoint;
                IssueSelectedMovementCommand();
                if (navigationStage == NavigationStage.Failed)
                {
                    BeginCleanup(CleanupTrigger.Exception);
                    return false;
                }
                return false;
            }

            if (navigationStage == NavigationStage.Moving || navigationStage == NavigationStage.Pausing || navigationStage == NavigationStage.Stabilizing)
            {
                ObserveNavigation();
            }

            if (navigationStage == NavigationStage.Moving)
            {
                if (suiteClock.Elapsed.TotalSeconds - navigationStartedAt > MovementTimeoutSeconds)
                {
                    FailCurrent("Authoritative mount movement exceeded its " + MovementTimeoutSeconds.ToString("0", CultureInfo.InvariantCulture) + " second monotonic deadline.");
                    navigationStage = NavigationStage.Failed;
                    BeginCleanup(CleanupTrigger.Exception);
                    return false;
                }

                if (navigationMode == NavigationMode.StopEarly && navigationMovedDistance >= 0.75d)
                {
                    RequireSelectionManager().Stop();
                    rowStopCommandIssuedCount++;
                    navigationStablePosition = mount.Position;
                    navigationStableStartedAt = suiteClock.Elapsed.TotalSeconds;
                    navigationStage = NavigationStage.Stabilizing;
                    return false;
                }

                if (navigationMode == NavigationMode.PauseResume && !navigationPauseRequested && navigationMovedDistance >= 0.75d)
                {
                    navigationPauseRequested = true;
                    Game.Instance.IsPaused = true;
                    rowPauseEntered = Game.Instance.IsPaused;
                    navigationPauseStartedAt = suiteClock.Elapsed.TotalSeconds;
                    navigationPausePosition = mount.Position;
                    navigationStage = NavigationStage.Pausing;
                    CaptureMilestone("paused");
                    return false;
                }

                if (HasReachedNavigationDestination())
                {
                    RequireSelectionManager().Stop();
                    navigationStablePosition = mount.Position;
                    navigationStableStartedAt = suiteClock.Elapsed.TotalSeconds;
                    navigationStage = NavigationStage.Stabilizing;
                }
                return false;
            }

            if (navigationStage == NavigationStage.Pausing)
            {
                navigationPauseDrift = Math.Max(navigationPauseDrift, PlanarDistance(navigationPausePosition, mount.Position));
                rowPauseMaximumDrift = Math.Max(rowPauseMaximumDrift, navigationPauseDrift);
                if (!Game.Instance.IsPaused)
                {
                    if (suiteClock.Elapsed.TotalSeconds - navigationPauseStartedAt > 2.0d)
                    {
                        FailCurrent("Game did not enter Pause within the bounded real-clock window.");
                        navigationStage = NavigationStage.Failed;
                        BeginCleanup(CleanupTrigger.Exception);
                    }
                    return false;
                }
                if (suiteClock.Elapsed.TotalSeconds - navigationPauseStartedAt < PauseObservationSeconds)
                {
                    return false;
                }
                rowPauseObservationSeconds = suiteClock.Elapsed.TotalSeconds - navigationPauseStartedAt;
                Game.Instance.IsPaused = false;
                rowPauseExited = !Game.Instance.IsPaused;
                navigationStage = NavigationStage.Moving;
                return false;
            }

            if (navigationStage == NavigationStage.Stabilizing)
            {
                navigationMaximumStationaryDrift = Math.Max(navigationMaximumStationaryDrift, PlanarDistance(navigationStablePosition, mount.Position));
                if (suiteClock.Elapsed.TotalSeconds - navigationStableStartedAt < StableWindowSeconds)
                {
                    return false;
                }
                CompleteNavigationLeg();
                return true;
            }

            return navigationStage == NavigationStage.Complete;
        }

        private bool PollPathProbe()
        {
            if (phaseClock.Elapsed.TotalSeconds > PathProbeTimeoutSeconds)
            {
                FailCurrent("Path probe exceeded its " + PathProbeTimeoutSeconds.ToString("0", CultureInfo.InvariantCulture) + " second monotonic deadline. Rejections: " + FormatProbeRejections());
                navigationStage = NavigationStage.Failed;
                return false;
            }

            if (probeCallbackReady)
            {
                probeCallbackReady = false;
                probePending = false;
                if (probeCallbackAccepted)
                {
                    WriteEvidence(new
                    {
                        kind = "path-probe",
                        requested = PositionEvidence.From(probeRequested),
                        endpoint = PositionEvidence.From(probeEndpoint),
                        pathLength = probePathLength,
                        accepted = true,
                        strictDoor = probeDoorStrict
                    });
                    return true;
                }
                probeRejections.Add(probeCallbackReason ?? "path callback rejected without a reason");
            }

            if (probePending)
            {
                return false;
            }

            while (probeIndex < probeCandidates.Count)
            {
                var candidate = probeCandidates[probeIndex++];
                if (probeDirectionFilter != null && !probeDirectionFilter(candidate))
                {
                    continue;
                }
                var agent = mount?.View?.AgentASP;
                if (agent == null || !agent.enabled)
                {
                    FailCurrent("Mammoth stock movement agent became unavailable during path probing.");
                    navigationStage = NavigationStage.Failed;
                    return false;
                }
                probeRequested = candidate;
                probePending = true;
                var generation = probeGeneration;
                var returned = agent.FindPath(candidate, path => OnPathProbeCompleted(generation, candidate, path));
                if (returned == null)
                {
                    probePending = false;
                    probeRejections.Add("candidate " + FormatPosition(candidate) + " could not start because the stock agent had a pending path");
                    continue;
                }
                return false;
            }

            FailCurrent("No deterministic Mammoth navigation candidate passed bounded path validation. Rejections: " + FormatProbeRejections());
            navigationStage = NavigationStage.Failed;
            return false;
        }

        private void OnPathProbeCompleted(int generation, Vector3 requested, Pathfinding.Path path)
        {
            if (generation != probeGeneration || completed || disposed)
            {
                return;
            }

            probeCallbackAccepted = false;
            probePathLength = 0.0d;
            if (path == null)
            {
                probeCallbackReason = "candidate " + FormatPosition(requested) + " returned a null path";
            }
            else if (path.error)
            {
                probeCallbackReason = "candidate " + FormatPosition(requested) + " returned path error: " + (path.errorLog ?? "unspecified") ;
            }
            else if (path.vectorPath == null || path.vectorPath.Count < 2)
            {
                probeCallbackReason = "candidate " + FormatPosition(requested) + " returned fewer than two path points";
            }
            else
            {
                for (var index = 1; index < path.vectorPath.Count; index++)
                {
                    probePathLength += PlanarDistance(path.vectorPath[index - 1], path.vectorPath[index]);
                }
                var endpoint = path.vectorPath[path.vectorPath.Count - 1];
                var direct = PlanarDistance(mount.Position, endpoint);
                var endpointError = PlanarDistance(requested, endpoint);
                var maximumDetour = probeDoorStrict ? direct * 2.5d + 3.0d : direct * 4.0d + 4.0d;
                if (!probeDoorStrict && direct < MinimumRadialDistance - 1.0f)
                {
                    probeCallbackReason = "candidate " + FormatPosition(requested) + " was too short (" + direct.ToString("0.00", CultureInfo.InvariantCulture) + ")";
                }
                else if (direct > MaximumRadialDistance * 3.0f)
                {
                    probeCallbackReason = "candidate " + FormatPosition(requested) + " endpoint was an outlier (" + direct.ToString("0.00", CultureInfo.InvariantCulture) + ")";
                }
                else if (endpointError > EndpointTolerance)
                {
                    probeCallbackReason = "candidate " + FormatPosition(requested) + " endpoint error was " + endpointError.ToString("0.00", CultureInfo.InvariantCulture);
                }
                else if (probePathLength > maximumDetour)
                {
                    probeCallbackReason = "candidate " + FormatPosition(requested) + " path detour was " + probePathLength.ToString("0.00", CultureInfo.InvariantCulture);
                }
                else if (probeDoorStrict && selectedDoor != null && !PathCrossesSelectedDoor(path.vectorPath))
                {
                    probeCallbackReason = "candidate " + FormatPosition(requested) + " path did not cross the selected open StandardDoor plane within the bounded aperture proxy";
                }
                else
                {
                    probeEndpoint = endpoint;
                    probeCallbackAccepted = true;
                    probeCallbackReason = null;
                }
            }
            probeCallbackReady = true;
        }

        private void IssueSelectedMovementCommand()
        {
            var mounted = relationship.State == RelationshipState.Mounted;
            var riderMoveBefore = rider.Commands.Move;
            var mountMoveBefore = mount.Commands.Move;
            if (navigationMode == NavigationMode.Formation)
            {
                nonPairStart = nonPairUnit.Position;
            }

            ClickGroundHandler.MoveSelectedUnitsToPoint(navigationDestination, false);
            TrackTouched(mount);
            ObserveUninvolvedCommands();
            navigationCommand = mount.Commands.Move;
            if (mount.Commands.Move != null)
            {
                navigationDestination = mount.Commands.Move.Target;
            }
            navigationStart = mount.Position;
            navigationBestDistance = PlanarDistance(mount.Position, navigationDestination);
            navigationPreviousDistance = navigationBestDistance;
            navigationLastProgressAt = suiteClock.Elapsed.TotalSeconds;
            navigationStartedAt = suiteClock.Elapsed.TotalSeconds;
            navigationPath = mount.View.AgentASP.Path;

            if (mounted)
            {
                assertions.Check(rider.Commands.Move == null && riderMoveBefore == null,
                    "Rider received no ordinary UnitMoveTo command.",
                    "Rider received or retained an ordinary movement command.");
                assertions.Check(navigationCommand != null && !ReferenceEquals(navigationCommand, mountMoveBefore),
                    "Stock click routing issued a new UnitMoveTo to the Mammoth.",
                    "Stock click routing did not issue a new Mammoth UnitMoveTo.");
                AssertLiveMovementAuthority();
            }
            else
            {
                assertions.Check(relationship.State == RelationshipState.Unmounted,
                    "Door control remained unmounted.",
                    "Door control unexpectedly changed relationship state.");
                assertions.Check(navigationCommand != null && !ReferenceEquals(navigationCommand, mountMoveBefore),
                    "Stock click issued the unmounted Mammoth control command.",
                    "Unmounted Mammoth control command was not issued.");
                assertions.Check(rider.Commands.Move == riderMoveBefore,
                    "Unmounted Mammoth control did not command the rider.",
                    "Unmounted Mammoth control unexpectedly changed the rider command.");
            }

            if (navigationMode == NavigationMode.Formation)
            {
                TrackTouched(nonPairUnit);
                nonPairCommand = nonPairUnit.Commands.Move;
                assertions.Check(nonPairCommand != null,
                    "Selected non-pair member received its stock formation command.",
                    "Selected non-pair member did not receive a formation movement command.");
                if (nonPairUnit.Commands.Move != null)
                {
                    nonPairTarget = nonPairUnit.Commands.Move.Target;
                    nonPairBestTargetDistance = PlanarDistance(nonPairUnit.Position, nonPairTarget);
                }
            }

            navigationStage = navigationCommand == null ? NavigationStage.Failed : NavigationStage.Moving;
            if (navigationStage == NavigationStage.Failed)
            {
                FailCurrent("Movement command was absent immediately after stock click routing.");
            }
        }

        private void ObserveNavigation()
        {
            if (mount == null || mount.View == null || mount.View.AgentASP == null)
            {
                FailCurrent("Mammoth view or stock movement agent disappeared during navigation.");
                navigationStage = NavigationStage.Failed;
                return;
            }

            var position = mount.Position;
            var distance = PlanarDistance(position, navigationDestination);
            navigationMovedDistance = Math.Max(navigationMovedDistance, PlanarDistance(navigationStart, position));
            if (distance + 0.10d < navigationBestDistance)
            {
                navigationBestDistance = distance;
                navigationLastProgressAt = suiteClock.Elapsed.TotalSeconds;
            }
            if (distance + 0.05d < navigationPreviousDistance)
            {
                navigationWasApproaching = true;
                navigationAwayFrameRun = 0;
            }
            else if (distance > navigationPreviousDistance + 0.12d)
            {
                navigationAwayFrameRun++;
                if (navigationWasApproaching && navigationAwayFrameRun == 3)
                {
                    navigationOscillations++;
                    navigationWasApproaching = false;
                }
            }
            else
            {
                navigationAwayFrameRun = 0;
            }
            navigationPreviousDistance = distance;

            var currentPath = mount.View.AgentASP.Path;
            if (currentPath != null && navigationPath != null && !ReferenceEquals(currentPath, navigationPath))
            {
                navigationRepaths++;
            }
            if (currentPath != null)
            {
                navigationPath = currentPath;
            }

            var currentCommand = mount.Commands.Move;
            if (currentCommand != null && navigationCommand != null && !ReferenceEquals(currentCommand, navigationCommand))
            {
                navigationCommandReplacements++;
                navigationCommand = currentCommand;
            }

            if ((mount.View.AgentASP.WantsToMove || mount.View.AgentASP.IsReallyMoving) && suiteClock.Elapsed.TotalSeconds - navigationLastProgressAt > 2.0d)
            {
                navigationMaximumStuckSeconds = Math.Max(navigationMaximumStuckSeconds, suiteClock.Elapsed.TotalSeconds - navigationLastProgressAt);
            }

            ObserveExpectedSelection();
            ObserveUninvolvedCommands();
            if (navigationMode == NavigationMode.Formation && nonPairUnit != null)
            {
                nonPairMovedDistance = Math.Max(nonPairMovedDistance, PlanarDistance(nonPairStart, nonPairUnit.Position));
                nonPairBestTargetDistance = Math.Min(nonPairBestTargetDistance, PlanarDistance(nonPairUnit.Position, nonPairTarget));
                minimumPairNonPairSeparation = Math.Min(minimumPairNonPairSeparation, PlanarDistance(mount.Position, nonPairUnit.Position));
            }

            if (!navigationMovingCaptureTaken && mount.View.AgentASP.IsReallyMoving)
            {
                navigationMovingCaptureTaken = true;
                CaptureMilestone(navigationMilestone);
            }
        }

        private bool HasReachedNavigationDestination()
        {
            if (navigationMode == NavigationMode.Formation)
            {
                if (nonPairUnit == null)
                {
                    return false;
                }
                mountFinalTargetDistance = PlanarDistance(mount.Position, navigationDestination);
                nonPairFinalTargetDistance = PlanarDistance(nonPairUnit.Position, nonPairTarget);
                var mountReached = mountFinalTargetDistance <= ReachTolerance && navigationBestDistance <= ReachTolerance;
                var otherReached = nonPairFinalTargetDistance <= ReachTolerance && nonPairBestTargetDistance <= ReachTolerance;
                return mountReached && otherReached && navigationMovedDistance >= 1.0d && nonPairMovedDistance >= 1.0d;
            }
            return PlanarDistance(mount.Position, navigationDestination) <= ReachTolerance ||
                (mount.Commands.Move == null && navigationBestDistance <= EndpointTolerance);
        }

        private void CompleteNavigationLeg()
        {
            if (navigationMode == NavigationMode.PauseResume && Game.Instance.IsPaused)
            {
                Game.Instance.IsPaused = false;
            }
            rowOscillations += navigationOscillations;
            rowUnexpectedRepaths += navigationRepaths;
            rowCommandReplacements += navigationCommandReplacements;
            rowSelectionLosses += navigationSelectionLosses;
            rowMaximumStationaryDrift = Math.Max(rowMaximumStationaryDrift, navigationMaximumStationaryDrift);
            rowMaximumStuckSeconds = Math.Max(rowMaximumStuckSeconds, navigationMaximumStuckSeconds);
            rowWaypointCount++;
            mountFinalTargetDistance = PlanarDistance(mount.Position, navigationDestination);
            if (navigationMode == NavigationMode.Formation && nonPairUnit != null)
            {
                nonPairFinalTargetDistance = PlanarDistance(nonPairUnit.Position, nonPairTarget);
            }
            if (navigationMode != NavigationMode.StopEarly)
            {
                rowEndpointQualifiedWaypointCount++;
                rowMaximumCompletedLegFinalTargetDistance = Math.Max(
                    rowMaximumCompletedLegFinalTargetDistance,
                    mountFinalTargetDistance);
                rowMaximumCompletedLegBestTargetDistance = Math.Max(
                    rowMaximumCompletedLegBestTargetDistance,
                    navigationBestDistance);
                assertions.Check(
                    mountFinalTargetDistance <= ReachTolerance && navigationBestDistance <= ReachTolerance,
                    "Movement leg finished within the calibrated final and best target-distance tolerance.",
                    "Movement leg target distances exceeded the calibrated tolerance: final=" +
                    mountFinalTargetDistance.ToString("0.000", CultureInfo.InvariantCulture) +
                    ", best=" + navigationBestDistance.ToString("0.000", CultureInfo.InvariantCulture) + ".");
            }
            if (previousLegDirection.sqrMagnitude > 0.01f)
            {
                rowMaximumTurnDegrees = Math.Max(rowMaximumTurnDegrees, Vector3.Angle(previousLegDirection, PlanarDirection(navigationStart, navigationDestination)));
            }
            assertions.Check(navigationMovedDistance >= (navigationMode == NavigationMode.StopEarly ? 0.75d : 1.0d),
                "Movement leg made measurable progress.",
                "Movement leg made only " + navigationMovedDistance.ToString("0.00", CultureInfo.InvariantCulture) + " world units of progress.");
            assertions.Check(navigationOscillations <= MaximumOscillations,
                "Movement leg remained within the oscillation bound.",
                "Movement leg observed " + navigationOscillations + " oscillations.");
            assertions.Check(navigationRepaths <= MaximumUnexpectedRepaths,
                "Movement leg remained within the unexpected-repath bound.",
                "Movement leg observed " + navigationRepaths + " unexpected path replacements.");
            assertions.Check(navigationCommandReplacements == 0,
                "Movement command was not unexpectedly replaced.",
                "Movement command was unexpectedly replaced " + navigationCommandReplacements + " time(s).");
            assertions.Check(navigationMaximumStuckSeconds <= 3.0d,
                "Movement leg remained within the stuck-duration bound.",
                "Movement leg was stuck for at least " + navigationMaximumStuckSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " seconds.");
            assertions.Check(navigationMaximumStationaryDrift <= StationaryTolerance,
                "Post-stop drift remained within the calibrated bound.",
                "Post-stop drift reached " + navigationMaximumStationaryDrift.ToString("0.000", CultureInfo.InvariantCulture) + " world units.");
            navigationStage = NavigationStage.Complete;
        }

        private void ObserveSynchronization()
        {
            var agent = relationship.Runtime.MovementAgent;
            if (agent == null)
            {
                return;
            }
            rowSynchronizationObservationCount++;
            rowMaximumPreCorrectionResidual = Math.Max(rowMaximumPreCorrectionResidual, agent.MaximumPreCorrectionPositionResidualWorldUnits);
            rowMaximumInitialConfigurationResidual = Math.Max(rowMaximumInitialConfigurationResidual,
                agent.MaximumInitialConfigurationPreCorrectionPositionResidualWorldUnits);
            rowMaximumUpdatePreCorrectionResidual = Math.Max(rowMaximumUpdatePreCorrectionResidual, agent.MaximumUpdatePreCorrectionPositionResidualWorldUnits);
            rowMaximumLateUpdatePreCorrectionResidual = Math.Max(rowMaximumLateUpdatePreCorrectionResidual, agent.MaximumLateUpdatePreCorrectionPositionResidualWorldUnits);
            rowMaximumUpdatePreCorrectionRotation = Math.Max(rowMaximumUpdatePreCorrectionRotation, agent.MaximumUpdatePreCorrectionRotationResidualDegrees);
            rowMaximumLateUpdatePreCorrectionRotation = Math.Max(rowMaximumLateUpdatePreCorrectionRotation, agent.MaximumLateUpdatePreCorrectionRotationResidualDegrees);
            rowMaximumPostCorrectionResidual = Math.Max(rowMaximumPostCorrectionResidual, agent.MaximumPostCorrectionPositionResidualWorldUnits);
            rowMaximumPostCorrectionRotation = Math.Max(rowMaximumPostCorrectionRotation, agent.MaximumPostCorrectionRotationResidualDegrees);
            rowMaximumRawCurrentPositionResidual = Math.Max(rowMaximumRawCurrentPositionResidual, agent.MaximumPreCorrectionRawCurrentPositionResidualWorldUnits);
            rowMaximumUpdateRawCurrentPositionResidual = Math.Max(rowMaximumUpdateRawCurrentPositionResidual, agent.MaximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits);
            rowMaximumLateUpdateRawCurrentPositionResidual = Math.Max(rowMaximumLateUpdateRawCurrentPositionResidual, agent.MaximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits);
            rowMaximumViewCurrentPositionResidual = Math.Max(rowMaximumViewCurrentPositionResidual, agent.MaximumCalibratedViewCurrentPositionResidualWorldUnits);
            rowMaximumEntityRawCurrentPositionResidual = Math.Max(rowMaximumEntityRawCurrentPositionResidual, agent.MaximumCalibratedEntityRawCurrentPositionResidualWorldUnits);
            rowMaximumEntityPreviousAuthoritativePositionResidual = Math.Max(rowMaximumEntityPreviousAuthoritativePositionResidual, agent.MaximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits);
            rowMaximumEntityPhaseAdjustedPositionResidual = Math.Max(rowMaximumEntityPhaseAdjustedPositionResidual, agent.MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits);
            rowMaximumAuthoritativePositionDelta = Math.Max(rowMaximumAuthoritativePositionDelta, agent.MaximumAuthoritativePositionDeltaWorldUnits);
            rowMaximumEntityRawPositionLagExcess = Math.Max(rowMaximumEntityRawPositionLagExcess, agent.MaximumEntityRawPositionLagExcessWorldUnits);
            rowPositionPhaseLagObservedCount = agent.PositionPhaseLagObservedCount;
            rowPositionPhaseLagPermittedCount = agent.PositionPhaseLagPermittedCount;
            rowPositionPhaseLagSameFrameUpdateReferenceCount = agent.PositionPhaseLagSameFrameUpdateReferenceCount;
            rowPositionPhaseLagEligibleReferenceCount = agent.PositionPhaseLagEligibleReferenceCount;
            rowPositionPhaseLagViolationCount = agent.PositionPhaseLagViolationCount;
            rowPositionPhaseLagRecoveryRequiredCount = agent.PositionPhaseLagRecoveryRequiredCount;
            rowPositionPhaseLagRecoveryUpdateCount = agent.PositionPhaseLagRecoveryUpdateCount;
            rowPositionPhaseLagRecoverySatisfiedCount = agent.PositionPhaseLagRecoverySatisfiedCount;
            rowPositionPhaseLagRecoveryRequiredEffectiveCount = agent.EffectivePositionPhaseLagRecoveryRequiredCount;
            rowPositionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount = agent.EffectivePositionPhaseLagRecoveryUpdateOrBoundaryCount;
            rowPositionPhaseLagRecoverySatisfiedEffectiveCount = agent.EffectivePositionPhaseLagRecoverySatisfiedCount;
            rowPositionPhaseLagRecoveryViolationCount = agent.PositionPhaseLagRecoveryViolationCount;
            rowStationaryPositionCorrectionViolationCount = agent.StationaryPositionCorrectionViolationCount;
            rowOutstandingPositionPhaseLagRecoveryCount = agent.OutstandingPositionPhaseLagRecoveryCount;
            rowMaximumConsecutiveUnrecoveredPositionPhaseLagCount = agent.MaximumConsecutiveUnrecoveredPositionPhaseLagCount;
            rowMaximumViewCurrentYawResidual = Math.Max(rowMaximumViewCurrentYawResidual, agent.MaximumCalibratedViewCurrentYawResidualDegrees);
            rowMaximumFullViewCurrentRotationResidual = Math.Max(
                rowMaximumFullViewCurrentRotationResidual,
                agent.MaximumCalibratedFullViewCurrentRotationResidualDegrees);
            rowMaximumMountEntityRootYawResidual = Math.Max(rowMaximumMountEntityRootYawResidual, agent.MaximumCalibratedMountEntityRootYawResidualDegrees);
            rowMaximumEntityRawCurrentYawResidual = Math.Max(rowMaximumEntityRawCurrentYawResidual, agent.MaximumCalibratedEntityRawCurrentYawResidualDegrees);
            rowMaximumEntityPreviousAuthoritativeYawResidual = Math.Max(rowMaximumEntityPreviousAuthoritativeYawResidual, agent.MaximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees);
            rowMaximumEntityPhaseAdjustedYawResidual = Math.Max(rowMaximumEntityPhaseAdjustedYawResidual, agent.MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees);
            rowMaximumAuthoritativeYawDelta = Math.Max(rowMaximumAuthoritativeYawDelta, agent.MaximumAuthoritativeYawDeltaDegrees);
            rowMaximumEntityRawLagExcess = Math.Max(rowMaximumEntityRawLagExcess, agent.MaximumEntityRawLagExcessDegrees);
            rowPhaseLagObservedCount = agent.PhaseLagObservedCount;
            rowPhaseLagPermittedCount = agent.PhaseLagPermittedCount;
            rowPhaseLagSameFrameUpdateReferenceCount = agent.PhaseLagSameFrameUpdateReferenceCount;
            rowPhaseLagEligibleReferenceCount = agent.PhaseLagEligibleReferenceCount;
            rowPhaseLagViolationCount = agent.PhaseLagViolationCount;
            rowPhaseLagRecoveryRequiredCount = agent.PhaseLagRecoveryRequiredCount;
            rowPhaseLagRecoveryUpdateCount = agent.PhaseLagRecoveryUpdateCount;
            rowPhaseLagRecoverySatisfiedCount = agent.PhaseLagRecoverySatisfiedCount;
            rowPhaseLagRecoveryRequiredEffectiveCount = agent.EffectivePhaseLagRecoveryRequiredCount;
            rowPhaseLagRecoveryUpdateOrBoundaryEffectiveCount = agent.EffectivePhaseLagRecoveryUpdateOrBoundaryCount;
            rowPhaseLagRecoverySatisfiedEffectiveCount = agent.EffectivePhaseLagRecoverySatisfiedCount;
            rowPhaseLagRecoveryViolationCount = agent.PhaseLagRecoveryViolationCount;
            rowStationaryYawCorrectionViolationCount = agent.StationaryYawCorrectionViolationCount;
            rowOutstandingPhaseLagRecoveryCount = agent.OutstandingPhaseLagRecoveryCount;
            rowMaximumConsecutiveUnrecoveredPhaseLagCount = agent.MaximumConsecutiveUnrecoveredPhaseLagCount;
            rowStationaryBoundaryClosureAttemptCount = agent.StationaryBoundaryClosureAttemptCount;
            rowStationaryBoundaryClosureSucceededCount = agent.StationaryBoundaryClosureSucceededCount;
            rowStationaryBoundaryClosureFailedCount = agent.StationaryBoundaryClosureFailedCount;
            rowYawPhaseLagStationaryBoundaryClosureCount = agent.YawPhaseLagStationaryBoundaryClosureCount;
            rowPositionPhaseLagStationaryBoundaryClosureCount = agent.PositionPhaseLagStationaryBoundaryClosureCount;
            rowUpdateSampleCount = agent.UpdateSampleCount;
            rowLateUpdateSampleCount = agent.LateUpdateSampleCount;
            rowUpdateCorrectionCount = agent.UpdateCorrectionCount;
            rowLateUpdateCorrectionCount = agent.LateUpdateCorrectionCount;
            var qualification = agent.QualifySynchronization(
                rowSynchronizationObservationCount,
                settings.MaximumAnchorResidualWorldUnits,
                MaximumPostCorrectionRotationResidualDegrees);
            if (!rowSynchronizationFailureRecorded && (!qualification.PhaseOrderPositionSafetyPassed || !qualification.PhaseOrderYawSafetyPassed ||
                !qualification.PostCorrectionPositionPassed || !qualification.PostCorrectionRotationPassed))
            {
                rowSynchronizationFailureRecorded = true;
                FailCurrent("Calibrated synchronization residual gate failed: Update=" +
                    agent.MaximumUpdatePreCorrectionPositionResidualWorldUnits.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", LateUpdate=" + agent.MaximumLateUpdatePreCorrectionPositionResidualWorldUnits.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", raw Update/LateUpdate position=" + agent.MaximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits.ToString("0.000000", CultureInfo.InvariantCulture) + "/" +
                    agent.MaximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", view-current position=" + agent.MaximumCalibratedViewCurrentPositionResidualWorldUnits.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", entity-raw-current position=" + agent.MaximumCalibratedEntityRawCurrentPositionResidualWorldUnits.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", entity-phase-adjusted position=" + agent.MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", position raw-lag excess=" + agent.MaximumEntityRawPositionLagExcessWorldUnits.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", position lag observed/permitted/violations=" + agent.PositionPhaseLagObservedCount + "/" + agent.PositionPhaseLagPermittedCount + "/" + agent.PositionPhaseLagViolationCount +
                    ", position recovery raw required/satisfied/violations=" + agent.PositionPhaseLagRecoveryRequiredCount + "/" + agent.PositionPhaseLagRecoverySatisfiedCount + "/" + agent.PositionPhaseLagRecoveryViolationCount +
                    ", Update rotation=" + agent.MaximumUpdatePreCorrectionRotationResidualDegrees.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", LateUpdate rotation=" + agent.MaximumLateUpdatePreCorrectionRotationResidualDegrees.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", view-current yaw=" + agent.MaximumCalibratedViewCurrentYawResidualDegrees.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", full-view-current rotation=" + agent.MaximumCalibratedFullViewCurrentRotationResidualDegrees.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", mount-entity/root yaw=" + agent.MaximumCalibratedMountEntityRootYawResidualDegrees.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", entity-raw-current yaw=" + agent.MaximumCalibratedEntityRawCurrentYawResidualDegrees.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", entity-phase-adjusted yaw=" + agent.MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", raw-lag excess=" + agent.MaximumEntityRawLagExcessDegrees.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", phase-lag observed/permitted/violations=" + agent.PhaseLagObservedCount + "/" + agent.PhaseLagPermittedCount + "/" + agent.PhaseLagViolationCount +
                    ", same-frame/eligible references=" + agent.PhaseLagSameFrameUpdateReferenceCount + "/" + agent.PhaseLagEligibleReferenceCount +
                    ", recovery required/satisfied/violations=" + agent.PhaseLagRecoveryRequiredCount + "/" + agent.PhaseLagRecoverySatisfiedCount + "/" + agent.PhaseLagRecoveryViolationCount +
                    ", stationary-yaw violations=" + agent.StationaryYawCorrectionViolationCount +
                    ", post-position=" + agent.MaximumPostCorrectionPositionResidualWorldUnits.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ", post-rotation=" + agent.MaximumPostCorrectionRotationResidualDegrees.ToString("0.000000", CultureInfo.InvariantCulture) + ".");
                navigationStage = NavigationStage.Failed;
            }
        }

        private void AssertLiveMovementAuthority()
        {
            var runtime = relationship.Runtime;
            assertions.Check(rider.View.AgentASP == pairSnapshot.RiderStockAgent && !rider.View.AgentASP.enabled && rider.View.AgentASP.AvoidanceDisabled,
                "Rider stock pathing and avoidance remain suppressed.",
                "Rider stock pathing or avoidance suppression changed during movement.");
            assertions.Check(rider.View.AgentOverride == runtime.MovementAgent && runtime.MovementAgent != null,
                "Rider synchronization override remains scoped and active.",
                "Rider synchronization override changed during movement.");
            assertions.Check(runtime.PresentationAttachmentLeaseActive && runtime.RiderParentMatchesAttachment,
                "Rider root-projected position attachment remains scoped and active.",
                "Rider position attachment lease changed during movement.");
            assertions.Check(mount.View.AgentASP == pairSnapshot.MountStockAgent && mount.View.AgentASP.enabled && mount.View.AgentOverride == null,
                "Mammoth stock agent remains authoritative.",
                "Mammoth movement authority changed during movement.");
        }

        private void AssertRowMovementQuality()
        {
            var agent = relationship.Runtime.MovementAgent;
            var qualification = agent == null
                ? null
                : agent.QualifySynchronization(
                    rowSynchronizationObservationCount,
                    settings.MaximumAnchorResidualWorldUnits,
                    MaximumPostCorrectionRotationResidualDegrees);
            assertions.Check(rowWaypointCount > 0,
                "At least one validated navigation leg completed.",
                "No validated navigation leg completed.");
            assertions.Check(qualification != null && qualification.PhaseOrderPositionSafetyPassed &&
                rowMaximumUpdatePreCorrectionResidual <= settings.MaximumAnchorResidualWorldUnits &&
                rowMaximumLateUpdatePreCorrectionResidual <= settings.MaximumAnchorResidualWorldUnits &&
                rowMaximumViewCurrentPositionResidual <= settings.MaximumAnchorResidualWorldUnits &&
                rowMaximumEntityPhaseAdjustedPositionResidual <= settings.MaximumAnchorResidualWorldUnits &&
                rowMaximumEntityRawPositionLagExcess <= MovementPositionPhaseTracker.RawLagArithmeticCoherenceEpsilonWorldUnits &&
                rowPositionPhaseLagViolationCount == 0L &&
                rowPositionPhaseLagRecoveryViolationCount == 0L &&
                rowStationaryPositionCorrectionViolationCount == 0L &&
                rowMaximumConsecutiveUnrecoveredPositionPhaseLagCount <= 1L &&
                rowOutstandingPositionPhaseLagRecoveryCount <= 1L &&
                rowPositionPhaseLagObservedCount == rowPositionPhaseLagPermittedCount &&
                rowPositionPhaseLagPermittedCount == rowPositionPhaseLagSameFrameUpdateReferenceCount &&
                rowPositionPhaseLagPermittedCount == rowPositionPhaseLagEligibleReferenceCount &&
                rowPositionPhaseLagRecoveryRequiredCount == rowPositionPhaseLagRecoveryUpdateCount &&
                rowPositionPhaseLagRecoveryUpdateCount == rowPositionPhaseLagRecoverySatisfiedCount &&
                rowPositionPhaseLagPermittedCount == rowPositionPhaseLagRecoverySatisfiedCount + rowOutstandingPositionPhaseLagRecoveryCount,
                "Visible rider position remained current; logical entity position was current or the immediately previous same-frame anchor with bounded raw lag and at most one pending recovery.",
                "Phase-aware position gate failed: effective Update/LateUpdate=" + rowMaximumUpdatePreCorrectionResidual.ToString("0.000000", CultureInfo.InvariantCulture) + "/" +
                rowMaximumLateUpdatePreCorrectionResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", raw Update/LateUpdate=" + rowMaximumUpdateRawCurrentPositionResidual.ToString("0.000000", CultureInfo.InvariantCulture) + "/" +
                rowMaximumLateUpdateRawCurrentPositionResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", view-current=" + rowMaximumViewCurrentPositionResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", entity-raw-current=" + rowMaximumEntityRawCurrentPositionResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", entity-previous=" + rowMaximumEntityPreviousAuthoritativePositionResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", entity-adjusted=" + rowMaximumEntityPhaseAdjustedPositionResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", authority-delta=" + rowMaximumAuthoritativePositionDelta.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", raw-lag-excess=" + rowMaximumEntityRawPositionLagExcess.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", lag observed/permitted/violations=" + rowPositionPhaseLagObservedCount + "/" + rowPositionPhaseLagPermittedCount + "/" + rowPositionPhaseLagViolationCount +
                ", recovery raw required/update/satisfied/violations=" + rowPositionPhaseLagRecoveryRequiredCount + "/" + rowPositionPhaseLagRecoveryUpdateCount + "/" + rowPositionPhaseLagRecoverySatisfiedCount + "/" + rowPositionPhaseLagRecoveryViolationCount +
                ", stationary violations=" + rowStationaryPositionCorrectionViolationCount +
                ", outstanding/max-consecutive=" + rowOutstandingPositionPhaseLagRecoveryCount + "/" + rowMaximumConsecutiveUnrecoveredPositionPhaseLagCount + ".");
            assertions.Check(qualification != null && qualification.PhaseOrderYawSafetyPassed &&
                rowMaximumUpdatePreCorrectionRotation <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumLateUpdatePreCorrectionRotation <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumViewCurrentYawResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumFullViewCurrentRotationResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumMountEntityRootYawResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumEntityPhaseAdjustedYawResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumEntityRawLagExcess <= MovementYawPhaseTracker.RawLagArithmeticCoherenceEpsilonDegrees &&
                rowPhaseLagViolationCount == 0L &&
                rowPhaseLagRecoveryViolationCount == 0L &&
                rowStationaryYawCorrectionViolationCount == 0L &&
                rowMaximumConsecutiveUnrecoveredPhaseLagCount <= 1L &&
                rowOutstandingPhaseLagRecoveryCount <= 1L &&
                rowPhaseLagObservedCount == rowPhaseLagPermittedCount &&
                rowPhaseLagPermittedCount == rowPhaseLagSameFrameUpdateReferenceCount &&
                rowPhaseLagPermittedCount == rowPhaseLagEligibleReferenceCount &&
                rowPhaseLagRecoveryRequiredCount == rowPhaseLagRecoveryUpdateCount &&
                rowPhaseLagRecoveryUpdateCount == rowPhaseLagRecoverySatisfiedCount &&
                rowPhaseLagPermittedCount == rowPhaseLagRecoverySatisfiedCount + rowOutstandingPhaseLagRecoveryCount,
                "Visible rider yaw remained current; logical entity yaw was current or the immediately previous same-frame authority value with bounded raw lag and at most one pending recovery.",
                "Phase-aware yaw gate failed: adjusted Update=" + rowMaximumUpdatePreCorrectionRotation.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", adjusted LateUpdate=" + rowMaximumLateUpdatePreCorrectionRotation.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", view-current=" + rowMaximumViewCurrentYawResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", full-view-current=" + rowMaximumFullViewCurrentRotationResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", mount-entity/root=" + rowMaximumMountEntityRootYawResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", entity-raw-current=" + rowMaximumEntityRawCurrentYawResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", entity-previous=" + rowMaximumEntityPreviousAuthoritativeYawResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", entity-adjusted=" + rowMaximumEntityPhaseAdjustedYawResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", authority-delta=" + rowMaximumAuthoritativeYawDelta.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", raw-lag-excess=" + rowMaximumEntityRawLagExcess.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", lag observed/permitted/violations=" + rowPhaseLagObservedCount + "/" + rowPhaseLagPermittedCount + "/" + rowPhaseLagViolationCount +
                ", same-frame/eligible references=" + rowPhaseLagSameFrameUpdateReferenceCount + "/" + rowPhaseLagEligibleReferenceCount +
                ", recovery required/updates/satisfied/violations=" + rowPhaseLagRecoveryRequiredCount + "/" + rowPhaseLagRecoveryUpdateCount + "/" + rowPhaseLagRecoverySatisfiedCount + "/" + rowPhaseLagRecoveryViolationCount +
                ", stationary violations=" + rowStationaryYawCorrectionViolationCount +
                ", outstanding/max-consecutive=" + rowOutstandingPhaseLagRecoveryCount + "/" + rowMaximumConsecutiveUnrecoveredPhaseLagCount + ".");
            assertions.Check(qualification != null && qualification.PostCorrectionPositionPassed && rowMaximumPostCorrectionResidual <= settings.MaximumAnchorResidualWorldUnits,
                "Maximum post-correction anchor residual remained within the configured threshold.",
                "Maximum post-correction anchor residual was " + rowMaximumPostCorrectionResidual.ToString("0.000000", CultureInfo.InvariantCulture) + ".");
            assertions.Check(qualification != null && qualification.PostCorrectionRotationPassed && rowMaximumPostCorrectionRotation <= MaximumPostCorrectionRotationResidualDegrees,
                "Maximum post-correction rotation residual remained within 0.10 degrees.",
                "Maximum post-correction rotation residual was " + rowMaximumPostCorrectionRotation.ToString("0.000000", CultureInfo.InvariantCulture) + " degrees.");
            assertions.Check(agent != null && agent.UpdateSampleCount > 0 && agent.LateUpdateSampleCount > 0,
                "Synchronization was sampled in both Update and LateUpdate phases.",
                "Synchronization did not produce both Update and LateUpdate samples.");
            assertions.Check(qualification != null && qualification.CorrectionCadencePassed,
                "Synchronization samples and corrections remained within the bounded once-per-phase callback cadence.",
                "Synchronization cadence exceeded its bound: observations=" + rowSynchronizationObservationCount +
                ", Update samples/corrections=" + (agent == null ? "missing" : agent.UpdateSampleCount + "/" + agent.UpdateCorrectionCount) +
                ", LateUpdate samples/corrections=" + (agent == null ? "missing" : agent.LateUpdateSampleCount + "/" + agent.LateUpdateCorrectionCount) + ".");
            assertions.Check(rowOscillations <= MaximumOscillations * Math.Max(1, rowWaypointCount),
                "Row oscillation count remained bounded.",
                "Row oscillation count was " + rowOscillations + ".");
            assertions.Check(rowUnexpectedRepaths <= MaximumUnexpectedRepaths * Math.Max(1, rowWaypointCount),
                "Row unexpected-repath count remained bounded.",
                "Row unexpected-repath count was " + rowUnexpectedRepaths + ".");
            assertions.Check(rowCommandReplacements == 0,
                "No routed Mammoth command was unexpectedly replaced.",
                "Observed " + rowCommandReplacements + " routed-command replacements.");
            assertions.Check(rowSelectionLosses == 0,
                "No unrequested selection loss occurred.",
                "Observed " + rowSelectionLosses + " unrequested selection-loss frame(s).");
            AssertLiveMovementAuthority();
        }

        private void BeginCleanup(CleanupTrigger trigger)
        {
            if (step == EngineStep.AwaitPreCleanupCaptures || step == EngineStep.AwaitCleanupFrame || step == EngineStep.AwaitFinalCaptures)
            {
                return;
            }
            pendingCleanupTrigger = trigger;
            cleanupBefore = CleanupStateEvidence.Capture(trigger, relationship, rider, mount, Game.Instance);
            step = EngineStep.AwaitPreCleanupCaptures;
            if (screenshotCapture.PendingCount == 0)
            {
                ContinueCleanupAfterCaptures();
            }
        }

        private void ContinueCleanupAfterCaptures()
        {
            if (screenshotCapture.PendingCount != 0)
            {
                return;
            }
            if (!cleanupMovementStoppedBeforeFinalSynchronization)
            {
                probeGeneration++;
                probePending = false;
                navigationStage = NavigationStage.None;
                StopTouchedMovement();
                cleanupMovementStoppedBeforeFinalSynchronization = true;
            }
            if (!FreezeFinalSynchronizationAtCleanupBoundary())
            {
                return;
            }
            cleanupBefore = CleanupStateEvidence.Capture(pendingCleanupTrigger, relationship, rider, mount, Game.Instance);
            var clean = BestEffortDismount(pendingCleanupTrigger);
            RestorePause();
            RestoreSelection();
            cleanupAfter = CleanupStateEvidence.Capture(pendingCleanupTrigger, relationship, rider, mount, Game.Instance);
            cleanupResidual = !clean || cleanupAfter.HasMountedResidual;
            cleanupFrame = frameNumber;
            step = EngineStep.AwaitCleanupFrame;
            if (!clean)
            {
                fatalResidue = true;
            }
        }

        private bool FreezeFinalSynchronizationAtCleanupBoundary()
        {
            if (relationship.State != RelationshipState.Mounted)
            {
                return true;
            }

            var agent = relationship.Runtime.MovementAgent;
            if (agent == null)
            {
                finalSynchronizationSnapshotCaptured = true;
                finalSynchronizationSnapshotFrame = frameNumber;
                finalSynchronizationAgentFrame = 0L;
                finalSynchronizationSampleCount = 0L;
                finalSynchronizationOutstandingRecoveryCount = 0L;
                finalSynchronizationOutstandingPositionRecoveryCount = 0L;
                finalSynchronizationQualificationPassed = false;
                assertions.Fail("Final pre-dismount synchronization snapshot could not resolve the mounted RiderMovementAgent.");
                return true;
            }

            ObserveSynchronization();
            MovementSynchronizationBoundarySnapshot boundary;
            try
            {
                boundary = agent.CaptureBoundarySnapshot();
            }
            catch (Exception exception)
            {
                finalSynchronizationSnapshotCaptured = true;
                finalSynchronizationSnapshotFrame = frameNumber;
                finalSynchronizationAgentFrame = agent.LatestYawObservation == null ? 0L : agent.LatestYawObservation.Frame;
                finalSynchronizationSampleCount = agent.SampleCount;
                finalSynchronizationOutstandingRecoveryCount = agent.OutstandingPhaseLagRecoveryCount;
                finalSynchronizationOutstandingPositionRecoveryCount = agent.OutstandingPositionPhaseLagRecoveryCount;
                finalSynchronizationQualificationPassed = false;
                assertions.Fail("Final pre-dismount synchronization boundary capture threw " + exception.GetType().Name + ": " + exception.Message);
                return true;
            }

            var closure = agent.ClosePendingRecoveryAtStationaryBoundary(
                boundary,
                settings.MaximumAnchorResidualWorldUnits,
                MaximumPostCorrectionRotationResidualDegrees);
            rowOutstandingPhaseLagRecoveryCount = agent.OutstandingPhaseLagRecoveryCount;
            rowOutstandingPositionPhaseLagRecoveryCount = agent.OutstandingPositionPhaseLagRecoveryCount;
            rowPhaseLagRecoveryRequiredEffectiveCount = agent.EffectivePhaseLagRecoveryRequiredCount;
            rowPhaseLagRecoveryUpdateOrBoundaryEffectiveCount = agent.EffectivePhaseLagRecoveryUpdateOrBoundaryCount;
            rowPhaseLagRecoverySatisfiedEffectiveCount = agent.EffectivePhaseLagRecoverySatisfiedCount;
            rowPositionPhaseLagRecoveryRequiredEffectiveCount = agent.EffectivePositionPhaseLagRecoveryRequiredCount;
            rowPositionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount = agent.EffectivePositionPhaseLagRecoveryUpdateOrBoundaryCount;
            rowPositionPhaseLagRecoverySatisfiedEffectiveCount = agent.EffectivePositionPhaseLagRecoverySatisfiedCount;
            rowStationaryBoundaryClosureAttemptCount = agent.StationaryBoundaryClosureAttemptCount;
            rowStationaryBoundaryClosureSucceededCount = agent.StationaryBoundaryClosureSucceededCount;
            rowStationaryBoundaryClosureFailedCount = agent.StationaryBoundaryClosureFailedCount;
            rowYawPhaseLagStationaryBoundaryClosureCount = agent.YawPhaseLagStationaryBoundaryClosureCount;
            rowPositionPhaseLagStationaryBoundaryClosureCount = agent.PositionPhaseLagStationaryBoundaryClosureCount;
            var qualification = agent.QualifySynchronization(
                rowSynchronizationObservationCount,
                settings.MaximumAnchorResidualWorldUnits,
                MaximumPostCorrectionRotationResidualDegrees);

            finalSynchronizationSnapshotCaptured = true;
            finalSynchronizationSnapshotFrame = frameNumber;
            finalSynchronizationAgentFrame = agent.LatestYawObservation == null ? 0L : agent.LatestYawObservation.Frame;
            finalSynchronizationSampleCount = agent.SampleCount;
            finalSynchronizationOutstandingRecoveryCount = agent.OutstandingPhaseLagRecoveryCount;
            finalSynchronizationOutstandingPositionRecoveryCount = agent.OutstandingPositionPhaseLagRecoveryCount;
            finalSynchronizationBoundaryPositionResidual = boundary.PositionResidualWorldUnits;
            finalSynchronizationBoundaryViewPositionResidual = boundary.ViewCurrentPositionResidualWorldUnits;
            finalSynchronizationBoundaryEntityPositionResidual = boundary.EntityCurrentPositionResidualWorldUnits;
            finalSynchronizationBoundaryFullViewRotationResidual = boundary.FullViewCurrentRotationResidualDegrees;
            finalSynchronizationBoundaryViewYawResidual = boundary.ViewCurrentYawResidualDegrees;
            finalSynchronizationBoundaryEntityCurrentYawResidual = boundary.EntityCurrentYawResidualDegrees;
            finalSynchronizationBoundaryMountEntityRootYawResidual = boundary.MountEntityRootYawResidualDegrees;
            finalSynchronizationBoundaryAuthoritativePositionAdvance = boundary.AuthoritativePositionAdvanceWorldUnits;
            finalSynchronizationBoundaryAuthoritativeYawAdvance = boundary.AuthoritativeYawAdvanceDegrees;
            finalSynchronizationBoundaryMovementCommandAbsent = boundary.MovementCommandAbsent;
            finalSynchronizationBoundaryWantsToMove = boundary.WantsToMove;
            finalSynchronizationBoundaryIsReallyMoving = boundary.IsReallyMoving;
            finalSynchronizationBoundaryClosureAttempted = closure.Attempted;
            finalSynchronizationBoundaryClosureSucceeded = closure.Succeeded;
            finalSynchronizationBoundaryClosureReason = closure.Reason;
            finalSynchronizationBoundaryYawPendingBefore = closure.YawPendingBefore;
            finalSynchronizationBoundaryPositionPendingBefore = closure.PositionPendingBefore;
            finalSynchronizationBoundaryYawClosedCount = closure.YawClosedCount;
            finalSynchronizationBoundaryPositionClosedCount = closure.PositionClosedCount;
            finalSynchronizationBoundaryYawPendingAfter = closure.YawPendingAfter;
            finalSynchronizationBoundaryPositionPendingAfter = closure.PositionPendingAfter;
            finalSynchronizationQualificationPassed = closure.Succeeded && IsStrictSynchronizationQualificationPassed(agent, qualification) &&
                finalSynchronizationBoundaryPositionResidual <= settings.MaximumAnchorResidualWorldUnits &&
                finalSynchronizationBoundaryFullViewRotationResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                finalSynchronizationBoundaryViewYawResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                finalSynchronizationBoundaryEntityCurrentYawResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                finalSynchronizationBoundaryMountEntityRootYawResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                finalSynchronizationBoundaryMovementCommandAbsent &&
                !finalSynchronizationBoundaryWantsToMove &&
                !finalSynchronizationBoundaryIsReallyMoving &&
                finalSynchronizationBoundaryAuthoritativePositionAdvance <= MovementPositionPhaseTracker.StationaryAuthorityEpsilonWorldUnits &&
                finalSynchronizationBoundaryAuthoritativeYawAdvance <= MovementYawPhaseTracker.StationaryAuthorityEpsilonDegrees;
            assertions.Check(
                finalSynchronizationQualificationPassed,
                "Final synchronization snapshot was frozen at the stopped stationary boundary; any single pending position/yaw lag was closed separately before immediate dismount.",
                "Final pre-dismount synchronization snapshot failed: samples=" + finalSynchronizationSampleCount +
                ", agent-frame=" + finalSynchronizationAgentFrame +
                ", full-view-current=" + rowMaximumFullViewCurrentRotationResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", boundary position=" + finalSynchronizationBoundaryPositionResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                " (view/entity=" + finalSynchronizationBoundaryViewPositionResidual.ToString("0.000000", CultureInfo.InvariantCulture) + "/" +
                finalSynchronizationBoundaryEntityPositionResidual.ToString("0.000000", CultureInfo.InvariantCulture) + ")" +
                ", boundary full-view/current-entity=" + finalSynchronizationBoundaryFullViewRotationResidual.ToString("0.000000", CultureInfo.InvariantCulture) + "/" +
                finalSynchronizationBoundaryEntityCurrentYawResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", adjusted entity=" + rowMaximumEntityPhaseAdjustedYawResidual.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", raw-lag excess=" + rowMaximumEntityRawLagExcess.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", authority position/yaw advance=" + finalSynchronizationBoundaryAuthoritativePositionAdvance.ToString("0.000000", CultureInfo.InvariantCulture) + "/" +
                finalSynchronizationBoundaryAuthoritativeYawAdvance.ToString("0.000000", CultureInfo.InvariantCulture) +
                ", stopped command/wants/really=" + finalSynchronizationBoundaryMovementCommandAbsent + "/" + finalSynchronizationBoundaryWantsToMove + "/" + finalSynchronizationBoundaryIsReallyMoving +
                ", closure=" + finalSynchronizationBoundaryClosureAttempted + "/" + finalSynchronizationBoundaryClosureSucceeded + "/" + finalSynchronizationBoundaryClosureReason +
                ", yaw pending/closed/after=" + finalSynchronizationBoundaryYawPendingBefore + "/" + finalSynchronizationBoundaryYawClosedCount + "/" + finalSynchronizationBoundaryYawPendingAfter +
                ", position pending/closed/after=" + finalSynchronizationBoundaryPositionPendingBefore + "/" + finalSynchronizationBoundaryPositionClosedCount + "/" + finalSynchronizationBoundaryPositionPendingAfter +
                ", outstanding yaw/position=" + finalSynchronizationOutstandingRecoveryCount + "/" + finalSynchronizationOutstandingPositionRecoveryCount + ".");
            return true;
        }

        private bool IsStrictSynchronizationQualificationPassed(
            RiderMovementAgent agent,
            MovementSynchronizationQualification qualification)
        {
            return agent != null && qualification != null &&
                qualification.PreCorrectionPositionPassed &&
                qualification.PreCorrectionRotationPassed &&
                qualification.PostCorrectionPositionPassed &&
                qualification.PostCorrectionRotationPassed &&
                qualification.CorrectionCadencePassed &&
                agent.UpdateSampleCount > 0L && agent.LateUpdateSampleCount > 0L &&
                rowMaximumUpdatePreCorrectionResidual <= settings.MaximumAnchorResidualWorldUnits &&
                rowMaximumLateUpdatePreCorrectionResidual <= settings.MaximumAnchorResidualWorldUnits &&
                rowMaximumViewCurrentPositionResidual <= settings.MaximumAnchorResidualWorldUnits &&
                rowMaximumEntityPhaseAdjustedPositionResidual <= settings.MaximumAnchorResidualWorldUnits &&
                rowMaximumEntityRawPositionLagExcess <= MovementPositionPhaseTracker.RawLagArithmeticCoherenceEpsilonWorldUnits &&
                rowPositionPhaseLagViolationCount == 0L &&
                rowPositionPhaseLagRecoveryViolationCount == 0L &&
                rowStationaryPositionCorrectionViolationCount == 0L &&
                rowMaximumConsecutiveUnrecoveredPositionPhaseLagCount <= 1L &&
                rowOutstandingPositionPhaseLagRecoveryCount == 0L &&
                rowPositionPhaseLagObservedCount == rowPositionPhaseLagPermittedCount &&
                rowPositionPhaseLagPermittedCount == rowPositionPhaseLagSameFrameUpdateReferenceCount &&
                rowPositionPhaseLagPermittedCount == rowPositionPhaseLagEligibleReferenceCount &&
                rowPositionPhaseLagRecoveryRequiredCount == rowPositionPhaseLagRecoveryUpdateCount &&
                rowPositionPhaseLagRecoveryUpdateCount == rowPositionPhaseLagRecoverySatisfiedCount &&
                rowPositionPhaseLagPermittedCount == agent.EffectivePositionPhaseLagRecoveryRequiredCount &&
                rowPositionPhaseLagPermittedCount == agent.EffectivePositionPhaseLagRecoveryUpdateOrBoundaryCount &&
                rowPositionPhaseLagPermittedCount == agent.EffectivePositionPhaseLagRecoverySatisfiedCount &&
                rowMaximumUpdatePreCorrectionRotation <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumLateUpdatePreCorrectionRotation <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumViewCurrentYawResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumFullViewCurrentRotationResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumMountEntityRootYawResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumEntityPhaseAdjustedYawResidual <= MaximumPostCorrectionRotationResidualDegrees &&
                rowMaximumEntityRawLagExcess <= MovementYawPhaseTracker.RawLagArithmeticCoherenceEpsilonDegrees &&
                rowPhaseLagViolationCount == 0L &&
                rowPhaseLagRecoveryViolationCount == 0L &&
                rowStationaryYawCorrectionViolationCount == 0L &&
                rowMaximumConsecutiveUnrecoveredPhaseLagCount <= 1L &&
                rowOutstandingPhaseLagRecoveryCount == 0L &&
                rowPhaseLagObservedCount == rowPhaseLagPermittedCount &&
                rowPhaseLagPermittedCount == rowPhaseLagSameFrameUpdateReferenceCount &&
                rowPhaseLagPermittedCount == rowPhaseLagEligibleReferenceCount &&
                rowPhaseLagRecoveryRequiredCount == rowPhaseLagRecoveryUpdateCount &&
                rowPhaseLagRecoveryUpdateCount == rowPhaseLagRecoverySatisfiedCount &&
                rowPhaseLagPermittedCount == agent.EffectivePhaseLagRecoveryRequiredCount &&
                rowPhaseLagPermittedCount == agent.EffectivePhaseLagRecoveryUpdateOrBoundaryCount &&
                rowPhaseLagPermittedCount == agent.EffectivePhaseLagRecoverySatisfiedCount &&
                agent.StationaryBoundaryClosureAttemptCount == agent.StationaryBoundaryClosureSucceededCount + agent.StationaryBoundaryClosureFailedCount &&
                agent.StationaryBoundaryClosureFailedCount == 0L &&
                agent.StationaryBoundaryClosureSucceededCount <= 1L &&
                agent.YawPhaseLagStationaryBoundaryClosureCount <= 1L &&
                agent.PositionPhaseLagStationaryBoundaryClosureCount <= 1L;
        }

        private bool BestEffortDismount(CleanupTrigger trigger)
        {
            if (relationship.State == RelationshipState.Unmounted)
            {
                cleanupAttemptSucceeded = true;
                cleanupResult = "Already Unmounted; idempotent cleanup required no transition.";
                return true;
            }
            if (relationship.State == RelationshipState.Disposed)
            {
                RecordCleanupFailure("Cleanup could not run because the relationship owner was already Disposed.");
                return false;
            }
            try
            {
                var result = relationship.Dismount(trigger);
                cleanupAttemptSucceeded = result.Succeeded;
                cleanupResult = FormatTransitionErrors(result);
                if (!result.Succeeded || result.MovementAuthorityResidual || result.PresentationResidual)
                {
                    RecordCleanupFailure("Cleanup retained mounted movement or presentation residue: " + FormatTransitionErrors(result));
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                RecordCleanupFailure("Cleanup threw " + exception.GetType().Name + ": " + exception.Message);
                cleanupAttemptSucceeded = false;
                cleanupResult = exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Movement cleanup threw", exception);
                return false;
            }
        }

        private void VerifyCleanupAndFinishRow()
        {
            if (frameNumber <= cleanupFrame)
            {
                return;
            }
            try
            {
                var failuresBeforeCleanupVerification = assertions.FailureCount;
                var clean = relationship.State == RelationshipState.Unmounted && relationship.Rider == null && relationship.Mount == null && relationship.Runtime.MovementAgent == null;
                assertions.Check(clean,
                    "Post-cleanup frame retained no mounted relationship references.",
                    "Post-cleanup frame retained relationship or movement-override residue.");
                if (pairSnapshot != null)
                {
                    assertions.Check(pairSnapshot.RiderStateRestored(),
                        "Rider stock movement, avoidance, view, and override state were restored exactly.",
                        "Rider retained post-cleanup movement, avoidance, view, or override residue, or its Unity view/agent was destroyed.");
                    assertions.Check(pairSnapshot.MountStateRestored(),
                        "Mammoth stock movement, avoidance, view, and override state were restored exactly.",
                        "Mammoth retained post-cleanup movement, avoidance, view, or override residue, or its Unity view/agent was destroyed.");
                    assertions.Check(pairSnapshot.RiderOverrideComponentCountRestored(),
                        "Owned RiderMovementAgent component count returned to its exact prior value.",
                        "A RiderMovementAgent component remained/disappeared, or its Unity view was destroyed after cleanup.");
                    assertions.Check(pairSnapshot.RiderAttachmentStateRestored() &&
                        relationship.Runtime.PresentationAttachmentRestoreVerified &&
                        !relationship.Runtime.PresentationAttachmentLeaseActive && !relationship.Runtime.HasPresentationAttachmentResidue,
                        "Rider attachment restored its exact parent, sibling index, and local scale; the lease verified captured world pose before nav-safe dismount placement.",
                        "Rider attachment retained parent/carrier residue, lost its Unity view, or did not verify the captured transform state.");
                }
                assertions.Check(PauseMatchesSnapshot(),
                    "Pause state was restored.",
                    "Pause state was not restored after movement cleanup.");
                assertions.Check(SelectionMatchesSnapshot(),
                    "Selection state was restored.",
                    "Selection state was not restored after movement cleanup.");
                if (nonPairSnapshot != null)
                {
                    assertions.Check(nonPairSnapshot.MatchesRestoredStoppedState(),
                        "Chosen idle non-pair unit returned to its captured movement lease state and remained stopped.",
                        "Chosen non-pair unit retained movement, a changed agent/avoidance/override state, or a destroyed Unity view after cleanup.");
                }
                AssertTouchedMovementStopped();
                if (assertions.FailureCount != failuresBeforeCleanupVerification)
                {
                    fatalResidue = true;
                    cleanupResidual = true;
                }
                CaptureMilestone("dismounted");
                if (!clean)
                {
                    fatalResidue = true;
                }
                if (screenshotCapture.PendingCount != 0)
                {
                    step = EngineStep.AwaitFinalCaptures;
                    return;
                }
                FinishRowAfterCaptures();
            }
            catch (Exception exception)
            {
                // A destroyed Unity object can throw MissingReferenceException
                // during a post-cleanup observation. That is failed cleanup
                // evidence, not permission to re-enter BeginCleanup forever.
                FailCurrent("Post-cleanup verification threw " + exception.GetType().Name + ": " + exception.Message);
                cleanupResidual = true;
                fatalResidue = true;
                screenshotCapture.CancelPending("Screenshot capture was cancelled after post-cleanup verification threw.");
                FinishCurrentRow();
                CompleteRemainingAsNotRun("Further movement was suppressed because post-cleanup verification could not prove restoration.");
                Complete();
            }
        }

        private void FinishRowAfterCaptures()
        {
            if (screenshotCapture.PendingCount != 0)
            {
                return;
            }
            FinishCurrentRow();
            if (abortAfterVerifiedCleanup)
            {
                CompleteRemainingAsNotRun("Further movement was suppressed after the suite deadline and verified cleanup.");
                Complete();
                return;
            }
            if (fatalResidue)
            {
                CompleteRemainingAsNotRun("Further movement was suppressed because cleanup residue was observed.");
                Complete();
            }
        }

        private void FinishCurrentRow()
        {
            var result = new RuntimeSubscenarioResult
            {
                Name = currentRow,
                Status = assertions.FailureCount == 0 ? "PASS" : "FAIL",
                AssertionPassCount = assertions.PassCount,
                AssertionFailCount = assertions.FailureCount,
                Errors = assertions.Errors
            };
            results.Add(result);
            foreach (var error in assertions.Errors)
            {
                errors.Add(currentRow + ": " + error);
            }

            WriteEvidence(new
            {
                kind = "movement-row-result",
                status = result.Status,
                assertionPassCount = result.AssertionPassCount,
                assertionFailCount = result.AssertionFailCount,
                maximumPreCorrectionResidualWorldUnits = rowMaximumPreCorrectionResidual,
                maximumInitialConfigurationResidualWorldUnits = rowMaximumInitialConfigurationResidual,
                maximumUpdatePreCorrectionResidualWorldUnits = rowMaximumUpdatePreCorrectionResidual,
                maximumLateUpdatePreCorrectionResidualWorldUnits = rowMaximumLateUpdatePreCorrectionResidual,
                maximumUpdatePreCorrectionRotationResidualDegrees = rowMaximumUpdatePreCorrectionRotation,
                maximumLateUpdatePreCorrectionRotationResidualDegrees = rowMaximumLateUpdatePreCorrectionRotation,
                maximumPostCorrectionResidualWorldUnits = rowMaximumPostCorrectionResidual,
                maximumPostCorrectionRotationResidualDegrees = rowMaximumPostCorrectionRotation,
                maximumRawCurrentPositionResidualWorldUnits = rowMaximumRawCurrentPositionResidual,
                maximumUpdateRawCurrentPositionResidualWorldUnits = rowMaximumUpdateRawCurrentPositionResidual,
                maximumLateUpdateRawCurrentPositionResidualWorldUnits = rowMaximumLateUpdateRawCurrentPositionResidual,
                maximumViewCurrentPositionResidualWorldUnits = rowMaximumViewCurrentPositionResidual,
                maximumEntityRawCurrentPositionResidualWorldUnits = rowMaximumEntityRawCurrentPositionResidual,
                maximumEntityPreviousAuthoritativePositionResidualWorldUnits = rowMaximumEntityPreviousAuthoritativePositionResidual,
                maximumEntityPhaseAdjustedPositionResidualWorldUnits = rowMaximumEntityPhaseAdjustedPositionResidual,
                maximumAuthoritativePositionDeltaWorldUnits = rowMaximumAuthoritativePositionDelta,
                maximumEntityRawPositionLagExcessWorldUnits = rowMaximumEntityRawPositionLagExcess,
                entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits = MovementPositionPhaseTracker.RawLagArithmeticCoherenceEpsilonWorldUnits,
                positionPhaseLagObservedCount = rowPositionPhaseLagObservedCount,
                positionPhaseLagPermittedCount = rowPositionPhaseLagPermittedCount,
                positionPhaseLagSameFrameUpdateReferenceCount = rowPositionPhaseLagSameFrameUpdateReferenceCount,
                positionPhaseLagEligibleReferenceCount = rowPositionPhaseLagEligibleReferenceCount,
                positionPhaseLagViolationCount = rowPositionPhaseLagViolationCount,
                positionPhaseLagRecoveryRequiredRawCount = rowPositionPhaseLagRecoveryRequiredCount,
                positionPhaseLagRecoveryUpdateRawCount = rowPositionPhaseLagRecoveryUpdateCount,
                positionPhaseLagRecoverySatisfiedRawCount = rowPositionPhaseLagRecoverySatisfiedCount,
                positionPhaseLagRecoveryRequiredEffectiveCount = rowPositionPhaseLagRecoveryRequiredEffectiveCount,
                positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount = rowPositionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount,
                positionPhaseLagRecoverySatisfiedEffectiveCount = rowPositionPhaseLagRecoverySatisfiedEffectiveCount,
                positionPhaseLagRecoveryViolationCount = rowPositionPhaseLagRecoveryViolationCount,
                stationaryPositionCorrectionViolationCount = rowStationaryPositionCorrectionViolationCount,
                outstandingPositionPhaseLagRecoveryCount = rowOutstandingPositionPhaseLagRecoveryCount,
                maximumConsecutiveUnrecoveredPositionPhaseLagCount = rowMaximumConsecutiveUnrecoveredPositionPhaseLagCount,
                maximumViewCurrentYawResidualDegrees = rowMaximumViewCurrentYawResidual,
                maximumFullViewCurrentRotationResidualDegrees = rowMaximumFullViewCurrentRotationResidual,
                maximumMountEntityRootYawResidualDegrees = rowMaximumMountEntityRootYawResidual,
                maximumEntityRawCurrentYawResidualDegrees = rowMaximumEntityRawCurrentYawResidual,
                maximumEntityPreviousAuthoritativeYawResidualDegrees = rowMaximumEntityPreviousAuthoritativeYawResidual,
                maximumEntityPhaseAdjustedYawResidualDegrees = rowMaximumEntityPhaseAdjustedYawResidual,
                maximumAuthoritativeYawDeltaDegrees = rowMaximumAuthoritativeYawDelta,
                maximumEntityRawLagExcessDegrees = rowMaximumEntityRawLagExcess,
                entityRawLagArithmeticCoherenceEpsilonDegrees = MovementYawPhaseTracker.RawLagArithmeticCoherenceEpsilonDegrees,
                phaseLagObservedCount = rowPhaseLagObservedCount,
                phaseLagPermittedCount = rowPhaseLagPermittedCount,
                phaseLagSameFrameUpdateReferenceCount = rowPhaseLagSameFrameUpdateReferenceCount,
                phaseLagEligibleReferenceCount = rowPhaseLagEligibleReferenceCount,
                phaseLagViolationCount = rowPhaseLagViolationCount,
                phaseLagRecoveryRequiredCount = rowPhaseLagRecoveryRequiredCount,
                phaseLagRecoveryUpdateCount = rowPhaseLagRecoveryUpdateCount,
                phaseLagRecoverySatisfiedCount = rowPhaseLagRecoverySatisfiedCount,
                phaseLagRecoveryRequiredRawCount = rowPhaseLagRecoveryRequiredCount,
                phaseLagRecoveryUpdateRawCount = rowPhaseLagRecoveryUpdateCount,
                phaseLagRecoverySatisfiedRawCount = rowPhaseLagRecoverySatisfiedCount,
                phaseLagRecoveryRequiredEffectiveCount = rowPhaseLagRecoveryRequiredEffectiveCount,
                phaseLagRecoveryUpdateOrBoundaryEffectiveCount = rowPhaseLagRecoveryUpdateOrBoundaryEffectiveCount,
                phaseLagRecoverySatisfiedEffectiveCount = rowPhaseLagRecoverySatisfiedEffectiveCount,
                phaseLagRecoveryViolationCount = rowPhaseLagRecoveryViolationCount,
                stationaryYawCorrectionViolationCount = rowStationaryYawCorrectionViolationCount,
                outstandingPhaseLagRecoveryCount = rowOutstandingPhaseLagRecoveryCount,
                maximumConsecutiveUnrecoveredPhaseLagCount = rowMaximumConsecutiveUnrecoveredPhaseLagCount,
                finalSynchronizationSnapshotCaptured,
                finalSynchronizationSnapshotStage = finalSynchronizationSnapshotCaptured ? "pre-dismount-after-captures" : "not-captured",
                finalSynchronizationSnapshotFrame,
                finalSynchronizationAgentFrame,
                finalSynchronizationSampleCount,
                finalSynchronizationOutstandingRecoveryCount,
                finalSynchronizationOutstandingPositionRecoveryCount,
                finalSynchronizationQualificationPassed,
                finalSynchronizationMovementStoppedBeforeSnapshot = cleanupMovementStoppedBeforeFinalSynchronization,
                finalSynchronizationBoundaryPositionResidualWorldUnits = finalSynchronizationBoundaryPositionResidual,
                finalSynchronizationBoundaryViewPositionResidualWorldUnits = finalSynchronizationBoundaryViewPositionResidual,
                finalSynchronizationBoundaryEntityPositionResidualWorldUnits = finalSynchronizationBoundaryEntityPositionResidual,
                finalSynchronizationBoundaryFullViewRotationResidualDegrees = finalSynchronizationBoundaryFullViewRotationResidual,
                finalSynchronizationBoundaryViewYawResidualDegrees = finalSynchronizationBoundaryViewYawResidual,
                finalSynchronizationBoundaryEntityCurrentYawResidualDegrees = finalSynchronizationBoundaryEntityCurrentYawResidual,
                finalSynchronizationBoundaryMountEntityRootYawResidualDegrees = finalSynchronizationBoundaryMountEntityRootYawResidual,
                finalSynchronizationBoundaryAuthoritativePositionAdvanceWorldUnits = finalSynchronizationBoundaryAuthoritativePositionAdvance,
                finalSynchronizationBoundaryAuthoritativeYawAdvanceDegrees = finalSynchronizationBoundaryAuthoritativeYawAdvance,
                finalSynchronizationBoundaryMovementCommandAbsent,
                finalSynchronizationBoundaryWantsToMove,
                finalSynchronizationBoundaryIsReallyMoving,
                finalSynchronizationBoundaryClosureAttempted,
                finalSynchronizationBoundaryClosureSucceeded,
                finalSynchronizationBoundaryClosureReason,
                finalSynchronizationBoundaryYawPendingBefore,
                finalSynchronizationBoundaryPositionPendingBefore,
                finalSynchronizationBoundaryYawClosedCount,
                finalSynchronizationBoundaryPositionClosedCount,
                finalSynchronizationBoundaryYawPendingAfter,
                finalSynchronizationBoundaryPositionPendingAfter,
                stationaryBoundaryClosureAttemptCount = rowStationaryBoundaryClosureAttemptCount,
                stationaryBoundaryClosureSucceededCount = rowStationaryBoundaryClosureSucceededCount,
                stationaryBoundaryClosureFailedCount = rowStationaryBoundaryClosureFailedCount,
                yawPhaseLagStationaryBoundaryClosureCount = rowYawPhaseLagStationaryBoundaryClosureCount,
                positionPhaseLagStationaryBoundaryClosureCount = rowPositionPhaseLagStationaryBoundaryClosureCount,
                synchronizationObservationCount = rowSynchronizationObservationCount,
                updateSynchronizationSampleCount = rowUpdateSampleCount,
                lateUpdateSynchronizationSampleCount = rowLateUpdateSampleCount,
                updateSynchronizationCorrectionCount = rowUpdateCorrectionCount,
                lateUpdateSynchronizationCorrectionCount = rowLateUpdateCorrectionCount,
                maximumStationaryDriftWorldUnits = rowMaximumStationaryDrift,
                maximumStuckSeconds = rowMaximumStuckSeconds,
                oscillationCount = rowOscillations,
                unexpectedRepathCount = rowUnexpectedRepaths,
                commandReplacementCount = rowCommandReplacements,
                selectionLossCount = rowSelectionLosses,
                waypointCount = rowWaypointCount,
                endpointQualifiedWaypointCount = rowEndpointQualifiedWaypointCount,
                maximumCompletedLegFinalTargetDistanceWorldUnits = rowMaximumCompletedLegFinalTargetDistance,
                maximumCompletedLegBestTargetDistanceWorldUnits = rowMaximumCompletedLegBestTargetDistance,
                maximumTurnDegrees = rowMaximumTurnDegrees,
                nonPairInterferenceCount = rowNonPairInterferenceCount,
                nonPairUnitId = nonPairUnit == null ? null : nonPairUnit.UniqueId,
                mountFinalTargetDistanceWorldUnits = mountFinalTargetDistance,
                nonPairBestTargetDistanceWorldUnits = nonPairBestTargetDistance,
                nonPairFinalTargetDistanceWorldUnits = nonPairFinalTargetDistance,
                minimumPairNonPairSeparationWorldUnits = minimumPairNonPairSeparation,
                requiredPairNonPairSeparationWorldUnits = requiredPairNonPairSeparation,
                unmountedDoorControlPassed = rowUnmountedDoorControlPassed,
                doorApproachSkipped = rowDoorApproachSkipped,
                stopCommandIssuedCount = rowStopCommandIssuedCount,
                restartCompleted = rowRestartCompleted,
                selectionMountNormalized = rowSelectionMountNormalized,
                selectionSwitchedAway = rowSelectionSwitchedAway,
                selectionSwitchedBack = rowSelectionSwitchedBack,
                formationSelectionNormalized = rowFormationSelectionNormalized,
                pauseEntered = rowPauseEntered,
                pauseObservationSeconds = rowPauseObservationSeconds,
                pauseMaximumDriftWorldUnits = rowPauseMaximumDrift,
                pauseExited = rowPauseExited,
                destinationCancelCommandAbsent = rowDestinationCancelCommandAbsent,
                destinationCancelRelationshipPreserved = rowDestinationCancelRelationshipPreserved,
                cleanupTrigger = pendingCleanupTrigger.ToString(),
                cleanupSucceeded = cleanupAttemptSucceeded,
                cleanupResult,
                cleanupResidual,
                cleanupBefore,
                cleanupAfter,
                selectionCoverage = "SelectionManager.SelectedUnits and scoped mount-to-rider normalization only; active portrait and camera-follow state are not asserted by this row.",
                formationCoverage = "Stock group-command recipients, final/best target distance, corpulence clearance, and uninvolved command identity only; authored formation-slot persistence is not asserted.",
                door = selectedDoor == null ? null : BuildHierarchyName(selectedDoor.transform),
                doorNear = PositionEvidence.From(doorNearPoint),
                doorFar = PositionEvidence.From(doorFarPoint),
                screenshots = screenshots.ToArray(),
                screenshotCaptureErrors = captureErrors.ToArray(),
                errors = assertions.Errors
            });

            if (string.Equals(result.Status, "PASS", StringComparison.Ordinal))
            {
                logger.Info("Movement runtime row PASS: " + currentRow + " (" + assertions.PassCount + " assertions).");
            }
            else
            {
                logger.Warning("Movement runtime row FAIL: " + currentRow + " (" + assertions.FailureCount + " failed assertions).");
            }

            rowIndex++;
            currentRow = null;
            assertions = null;
            pairSnapshot = null;
            selectionSnapshot = null;
            rider = null;
            mount = null;
            nonPairUnit = null;
            touchedUnits.Clear();
            rowClock.Reset();
            phaseClock.Reset();
            step = EngineStep.BeginRow;
        }

        private void CompleteRemainingAsNotRun(string reason)
        {
            while (rowIndex < selectedRows.Count)
            {
                var row = selectedRows[rowIndex++];
                results.Add(new RuntimeSubscenarioResult
                {
                    Name = row,
                    Status = "FAIL",
                    AssertionPassCount = 0,
                    AssertionFailCount = 1,
                    Errors = new[] { reason }
                });
                errors.Add(row + ": " + reason);
            }
        }

        private void Complete()
        {
            StopTouchedMovement();
            BestEffortDismount(CleanupTrigger.Exception);
            RestorePause();
            RestoreSelection();
            CloseEvidenceWriter();
            RestoreSettings();
            suiteClock.Stop();
            rowClock.Stop();
            phaseClock.Stop();
            completed = true;
            logger.Info("Movement runtime engine completed with " + results.Count + " row result(s).");
        }

        private void CloseEvidenceWriter()
        {
            if (evidenceWriter == null) { return; }
            try
            {
                evidenceWriter.Flush();
                evidenceWriter.Dispose();
            }
            catch (Exception exception)
            {
                errors.Add("Movement evidence finalization threw " + exception.GetType().Name + ": " + exception.Message);
                logger.Exception("Movement evidence finalization threw", exception);
            }
            finally
            {
                evidenceWriter = null;
            }
        }

        private void AssertExpectedSelection(bool condition)
        {
            if (!condition)
            {
                navigationSelectionLosses++;
            }
        }

        private void ObserveExpectedSelection()
        {
            var selected = RequireSelectionManager().SelectedUnits;
            if (navigationMode == NavigationMode.UnmountedControl)
            {
                AssertExpectedSelection(selected.Count == 1 && selected[0] == mount);
            }
            else if (navigationMode == NavigationMode.Formation)
            {
                AssertExpectedSelection(selected.Contains(rider) && selected.Contains(nonPairUnit) && !selected.Contains(mount));
            }
            else
            {
                AssertExpectedSelection(selected.Contains(rider) && !selected.Contains(mount));
            }
        }

        private void ObserveUninvolvedCommands()
        {
            if (uninvolvedCommands == null)
            {
                return;
            }
            var changed = new List<UnitEntityData>();
            foreach (var pair in uninvolvedCommands)
            {
                if (!ReferenceEquals(pair.Key.Commands.Move, pair.Value))
                {
                    rowNonPairInterferenceCount++;
                    TrackTouched(pair.Key);
                    changed.Add(pair.Key);
                }
            }
            foreach (var unit in changed)
            {
                uninvolvedCommands[unit] = unit.Commands.Move;
            }
        }

        private Dictionary<UnitEntityData, object> CaptureUninvolvedMoveCommands(UnitEntityData selectedNonPair)
        {
            var captured = new Dictionary<UnitEntityData, object>();
            var controllable = Game.Instance?.Player?.ControllableCharacters;
            if (controllable == null)
            {
                return captured;
            }
            foreach (var unit in controllable)
            {
                if (unit != null && unit != rider && unit != mount && unit != selectedNonPair && unit.Commands.Move == null)
                {
                    captured[unit] = unit.Commands.Move;
                }
            }
            return captured;
        }

        private UnitEntityData FindIdleNonPairControllable()
        {
            var controllable = Game.Instance?.Player?.ControllableCharacters;
            if (controllable == null)
            {
                return null;
            }
            foreach (var unit in controllable)
            {
                var agent = unit?.View?.AgentASP;
                if (unit != null && unit != rider && unit != mount && unit.IsInGame && unit.IsDirectlyControllable &&
                    unit.Commands != null && unit.Commands.Move == null && agent != null && agent.enabled &&
                    !agent.WantsToMove && !agent.IsReallyMoving)
                {
                    return unit;
                }
            }
            return null;
        }

        private bool TrySelectOpenDoorCandidate(out DoorCandidate selected, out string reason)
        {
            selected = null;
            reason = null;
            var doors = UnityEngine.Object.FindObjectsOfType<StandardDoor>()
                .Where(door => door != null && door.isActiveAndEnabled && door.gameObject.activeInHierarchy)
                .OrderBy(door => PlanarDistance(mount.Position, door.transform.position))
                .ThenBy(door => BuildHierarchyName(door.transform), StringComparer.Ordinal)
                .ToArray();
            var openCount = 0;
            foreach (var door in doors)
            {
                bool isOpen;
                try
                {
                    isOpen = door.IsOpen;
                }
                catch
                {
                    continue;
                }
                if (!isOpen)
                {
                    continue;
                }
                openCount++;
                var centerDistance = PlanarDistance(mount.Position, door.transform.position);
                if (centerDistance > 28.0d)
                {
                    continue;
                }
                var clearance = Math.Max(3.5f, mount.Corpulence * 2.0f + 1.0f);
                var doorToMount = PlanarDirection(door.transform.position, mount.Position);
                var axes = new[] { PlanarNormalized(door.transform.forward), PlanarNormalized(door.transform.right) }
                    .OrderByDescending(axis => Math.Abs(Vector3.Dot(axis, doorToMount)))
                    .ToArray();
                foreach (var axis in axes)
                {
                    if (axis.sqrMagnitude < 0.5f)
                    {
                        continue;
                    }
                    var first = door.transform.position - axis * clearance;
                    var second = door.transform.position + axis * clearance;
                    if (PlanarDistance(mount.Position, second) < PlanarDistance(mount.Position, first))
                    {
                        var temporary = first;
                        first = second;
                        second = temporary;
                    }
                    var directCrossing = PlanarDistance(first, second);
                    if (directCrossing < MinimumRadialDistance || directCrossing > MaximumRadialDistance * 2.0f)
                    {
                        continue;
                    }
                    selected = new DoorCandidate(door, first, second);
                    return true;
                }
            }
            reason = openCount == 0
                ? "Doorway row found no active open StandardDoor; no door was mutated and no mounted inference was made."
                : "Doorway row found " + openCount + " active open StandardDoor object(s), but none had a bounded nearby geometry candidate for the Mammoth control.";
            return false;
        }

        private IReadOnlyList<Vector3> BuildRadialCandidates(Vector3 origin, float orientation)
        {
            var forward = Quaternion.Euler(0f, orientation, 0f) * Vector3.forward;
            forward = PlanarNormalized(forward);
            var right = new Vector3(forward.z, 0f, -forward.x);
            var directions = new[]
            {
                forward,
                PlanarNormalized(forward + right),
                right,
                PlanarNormalized(-forward + right),
                -forward,
                PlanarNormalized(-forward - right),
                -right,
                PlanarNormalized(forward - right)
            };
            var candidates = new List<Vector3>();
            foreach (var distance in new[] { MaximumRadialDistance, 8.0f, MinimumRadialDistance })
            {
                foreach (var direction in directions)
                {
                    candidates.Add(origin + direction * distance);
                }
            }
            return candidates;
        }

        private bool PathCrossesSelectedDoor(IList<Vector3> points)
        {
            if (selectedDoor == null || points == null || points.Count < 2)
            {
                return false;
            }

            var center = selectedDoor.transform.position;
            var normal = PlanarDirection(doorNearPoint, doorFarPoint);
            if (normal.sqrMagnitude < 0.5f)
            {
                return false;
            }

            var minimumSide = double.MaxValue;
            var maximumSide = double.MinValue;
            var minimumSegmentDistance = double.MaxValue;
            for (var index = 0; index < points.Count; index++)
            {
                var relative = points[index] - center;
                var side = relative.x * normal.x + relative.z * normal.z;
                minimumSide = Math.Min(minimumSide, side);
                maximumSide = Math.Max(maximumSide, side);
                if (index > 0)
                {
                    minimumSegmentDistance = Math.Min(minimumSegmentDistance, SegmentPlanarDistance(center, points[index - 1], points[index]));
                }
            }

            var apertureProxy = Math.Max(2.5d, mount.Corpulence + 1.0d);
            return minimumSide < -0.25d && maximumSide > 0.25d && minimumSegmentDistance <= apertureProxy;
        }

        private static double SegmentPlanarDistance(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            var dx = (double)segmentEnd.x - segmentStart.x;
            var dz = (double)segmentEnd.z - segmentStart.z;
            var lengthSquared = dx * dx + dz * dz;
            if (lengthSquared <= 0.000001d)
            {
                return PlanarDistance(point, segmentStart);
            }
            var px = (double)point.x - segmentStart.x;
            var pz = (double)point.z - segmentStart.z;
            var amount = Math.Max(0.0d, Math.Min(1.0d, (px * dx + pz * dz) / lengthSquared));
            var closest = new Vector3((float)(segmentStart.x + dx * amount), point.y, (float)(segmentStart.z + dz * amount));
            return PlanarDistance(point, closest);
        }

        private void SelectOnly(UnitEntityData unit)
        {
            if (unit == null || unit.View == null)
            {
                throw new InvalidOperationException("Cannot select a missing unit/view.");
            }
            RequireSelectionManager().SelectUnit(unit.View, true, false, false);
        }

        private SelectionManager RequireSelectionManager()
        {
            var selection = SelectionManager.Instance;
            if (selection == null)
            {
                throw new InvalidOperationException("Kingmaker SelectionManager is unavailable.");
            }
            return selection;
        }

        private void StopTouchedMovement()
        {
            foreach (var unit in touchedUnits)
            {
                try
                {
                    unit?.Commands?.InterruptMove();
                    unit?.View?.StopMoving();
                }
                catch (Exception exception)
                {
                    var message = "Best-effort touched-unit stop failed for " + (unit == null ? "<missing>" : unit.UniqueId) +
                        ": " + exception.GetType().Name + ": " + exception.Message;
                    if (assertions != null) { assertions.Fail(message); }
                    else { errors.Add(message); }
                    logger.Warning("Best-effort touched-unit stop failed: " + exception.GetType().Name + ": " + exception.Message);
                }
            }
        }

        private void AssertTouchedMovementStopped()
        {
            foreach (var unit in touchedUnits)
            {
                try
                {
                    if (unit == null || unit.View == null || unit.View.AgentASP == null || unit.Commands == null)
                    {
                        FailCurrent("A test-issued movement recipient disappeared before stop verification.");
                        continue;
                    }
                    assertions.Check(unit.Commands.Move == null && !unit.View.AgentASP.WantsToMove && !unit.View.AgentASP.IsReallyMoving,
                        "Test-issued movement was stopped for " + unit.UniqueId + ".",
                        "Test-issued movement or destination remained active for " + unit.UniqueId + ".");
                }
                catch (Exception exception)
                {
                    FailCurrent("Touched-unit stop verification threw " + exception.GetType().Name + ": " + exception.Message);
                }
            }
        }

        private void TrackTouched(UnitEntityData unit)
        {
            if (unit != null && !touchedUnits.Contains(unit))
            {
                touchedUnits.Add(unit);
            }
        }

        private void RestorePause()
        {
            if (!pauseLeaseOwned || Game.Instance == null)
            {
                return;
            }
            if (Game.Instance.IsPaused != originalPause)
            {
                Game.Instance.IsPaused = originalPause;
            }
            pauseLeaseOwned = false;
        }

        private bool PauseMatchesSnapshot()
        {
            return Game.Instance != null && Game.Instance.IsPaused == originalPause;
        }

        private void RestoreSelection()
        {
            if (selectionSnapshot == null)
            {
                return;
            }
            selectionSnapshot.Restore();
        }

        private bool SelectionMatchesSnapshot()
        {
            return selectionSnapshot == null || selectionSnapshot.MatchesCurrent();
        }

        private void RestoreSettings()
        {
            if (!settingLeaseOwned)
            {
                return;
            }
            settings.EnableUnsafeMovementExperiment = originalUnsafeMovementSetting;
            settingLeaseOwned = false;
        }

        private void CaptureMilestone(string milestone)
        {
            if (!CaptureMilestones.Contains(milestone) || currentRow == null)
            {
                return;
            }
            screenshotCapture.Enqueue(currentRow, milestone, frameNumber + 1);
        }

        private void CommitScreenshot(MovementScreenshotCaptureRequest request, byte[] bytes)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException("Screenshot bytes are required.", nameof(bytes));
            }
            if (!string.Equals(currentRow, request.Row, StringComparison.Ordinal) || !CaptureMilestones.Contains(request.Milestone))
            {
                throw new InvalidOperationException("Screenshot completion does not belong to the active allowlisted movement row and milestone.");
            }

            int captureCount;
            captureCounts.TryGetValue(request.Milestone, out captureCount);
            captureCount++;
            captureCounts[request.Milestone] = captureCount;
            var fileName = GetRowToken(request.Row) + "-" + request.Milestone + "-" + captureCount.ToString("00", CultureInfo.InvariantCulture) + ".png";
            var visualsRoot = Path.Combine(evidenceRoot, "movement-visuals");
            Directory.CreateDirectory(visualsRoot);
            var path = Path.Combine(visualsRoot, fileName);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
            screenshots.Add(new ScreenshotEvidence
            {
                Milestone = request.Milestone,
                RelativePath = "movement-visuals/" + fileName,
                Length = bytes.LongLength,
                Sha256 = ComputeSha256(bytes)
            });
        }

        private void RecordScreenshotFailure(MovementScreenshotCaptureRequest request, string reason)
        {
            var row = request == null ? currentRow : request.Row;
            var milestone = request == null ? "unknown" : request.Milestone;
            var error = "Screenshot evidence failed for " + (row ?? "unknown-row") + "/" + milestone + ": " + (reason ?? "unknown failure");
            captureErrors.Add(error);
            if (assertions != null && string.Equals(currentRow, row, StringComparison.Ordinal))
            {
                assertions.Fail(error);
            }
            else
            {
                errors.Add(error);
            }
            logger.Warning(error);
        }

        private void WriteEvidence(object value)
        {
            if (evidenceWriter == null)
            {
                return;
            }
            var serializer = JsonSerializer.Create(EvidenceJsonSettings);
            var payload = JObject.FromObject(value, serializer);
            var record = new JObject
            {
                { "schemaVersion", 1 },
                { "runId", request.RunId },
                { "scenario", request.Scenario },
                { "row", currentRow },
                { "branch", request.Branch },
                { "commit", request.Commit },
                { "productVersion", request.ProductVersion },
                { "dllSha256", request.DllSha256 },
                { "dllMvid", request.DllMvid },
                { "sequence", evidenceSequence++ },
                { "utcTimestamp", DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture) }
            };
            foreach (var property in payload.Properties())
            {
                if (record.Property(property.Name) != null)
                {
                    throw new InvalidOperationException("Movement evidence payload duplicated an owned identity field: " + property.Name + ".");
                }
                record.Add(property.Name, property.Value);
            }
            evidenceWriter.WriteLine(record.ToString(Formatting.None));
            evidenceWriter.Flush();
        }

        private void ResetRowMetrics()
        {
            rowMaximumPreCorrectionResidual = 0.0d;
            rowMaximumInitialConfigurationResidual = 0.0d;
            rowMaximumUpdatePreCorrectionResidual = 0.0d;
            rowMaximumLateUpdatePreCorrectionResidual = 0.0d;
            rowMaximumUpdatePreCorrectionRotation = 0.0d;
            rowMaximumLateUpdatePreCorrectionRotation = 0.0d;
            rowMaximumPostCorrectionResidual = 0.0d;
            rowMaximumPostCorrectionRotation = 0.0d;
            rowMaximumRawCurrentPositionResidual = 0.0d;
            rowMaximumUpdateRawCurrentPositionResidual = 0.0d;
            rowMaximumLateUpdateRawCurrentPositionResidual = 0.0d;
            rowMaximumViewCurrentPositionResidual = 0.0d;
            rowMaximumEntityRawCurrentPositionResidual = 0.0d;
            rowMaximumEntityPreviousAuthoritativePositionResidual = 0.0d;
            rowMaximumEntityPhaseAdjustedPositionResidual = 0.0d;
            rowMaximumAuthoritativePositionDelta = 0.0d;
            rowMaximumEntityRawPositionLagExcess = 0.0d;
            rowPositionPhaseLagObservedCount = 0L;
            rowPositionPhaseLagPermittedCount = 0L;
            rowPositionPhaseLagSameFrameUpdateReferenceCount = 0L;
            rowPositionPhaseLagEligibleReferenceCount = 0L;
            rowPositionPhaseLagViolationCount = 0L;
            rowPositionPhaseLagRecoveryRequiredCount = 0L;
            rowPositionPhaseLagRecoveryUpdateCount = 0L;
            rowPositionPhaseLagRecoverySatisfiedCount = 0L;
            rowPositionPhaseLagRecoveryRequiredEffectiveCount = 0L;
            rowPositionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount = 0L;
            rowPositionPhaseLagRecoverySatisfiedEffectiveCount = 0L;
            rowPositionPhaseLagRecoveryViolationCount = 0L;
            rowStationaryPositionCorrectionViolationCount = 0L;
            rowOutstandingPositionPhaseLagRecoveryCount = 0L;
            rowMaximumConsecutiveUnrecoveredPositionPhaseLagCount = 0L;
            rowMaximumViewCurrentYawResidual = 0.0d;
            rowMaximumFullViewCurrentRotationResidual = 0.0d;
            rowMaximumMountEntityRootYawResidual = 0.0d;
            rowMaximumEntityRawCurrentYawResidual = 0.0d;
            rowMaximumEntityPreviousAuthoritativeYawResidual = 0.0d;
            rowMaximumEntityPhaseAdjustedYawResidual = 0.0d;
            rowMaximumAuthoritativeYawDelta = 0.0d;
            rowMaximumEntityRawLagExcess = 0.0d;
            rowPhaseLagObservedCount = 0L;
            rowPhaseLagPermittedCount = 0L;
            rowPhaseLagSameFrameUpdateReferenceCount = 0L;
            rowPhaseLagEligibleReferenceCount = 0L;
            rowPhaseLagViolationCount = 0L;
            rowPhaseLagRecoveryRequiredCount = 0L;
            rowPhaseLagRecoveryUpdateCount = 0L;
            rowPhaseLagRecoverySatisfiedCount = 0L;
            rowPhaseLagRecoveryRequiredEffectiveCount = 0L;
            rowPhaseLagRecoveryUpdateOrBoundaryEffectiveCount = 0L;
            rowPhaseLagRecoverySatisfiedEffectiveCount = 0L;
            rowPhaseLagRecoveryViolationCount = 0L;
            rowStationaryYawCorrectionViolationCount = 0L;
            rowOutstandingPhaseLagRecoveryCount = 0L;
            rowMaximumConsecutiveUnrecoveredPhaseLagCount = 0L;
            rowStationaryBoundaryClosureAttemptCount = 0L;
            rowStationaryBoundaryClosureSucceededCount = 0L;
            rowStationaryBoundaryClosureFailedCount = 0L;
            rowYawPhaseLagStationaryBoundaryClosureCount = 0L;
            rowPositionPhaseLagStationaryBoundaryClosureCount = 0L;
            finalSynchronizationSnapshotCaptured = false;
            finalSynchronizationSnapshotFrame = 0L;
            finalSynchronizationAgentFrame = 0L;
            finalSynchronizationSampleCount = 0L;
            finalSynchronizationOutstandingRecoveryCount = 0L;
            finalSynchronizationOutstandingPositionRecoveryCount = 0L;
            finalSynchronizationQualificationPassed = false;
            finalSynchronizationBoundaryPositionResidual = 0.0d;
            finalSynchronizationBoundaryFullViewRotationResidual = 0.0d;
            finalSynchronizationBoundaryViewYawResidual = 0.0d;
            finalSynchronizationBoundaryEntityCurrentYawResidual = 0.0d;
            finalSynchronizationBoundaryMountEntityRootYawResidual = 0.0d;
            finalSynchronizationBoundaryViewPositionResidual = 0.0d;
            finalSynchronizationBoundaryEntityPositionResidual = 0.0d;
            finalSynchronizationBoundaryAuthoritativePositionAdvance = 0.0d;
            finalSynchronizationBoundaryAuthoritativeYawAdvance = 0.0d;
            finalSynchronizationBoundaryMovementCommandAbsent = false;
            finalSynchronizationBoundaryWantsToMove = false;
            finalSynchronizationBoundaryIsReallyMoving = false;
            finalSynchronizationBoundaryClosureAttempted = false;
            finalSynchronizationBoundaryClosureSucceeded = false;
            finalSynchronizationBoundaryClosureReason = "not-captured";
            finalSynchronizationBoundaryYawPendingBefore = 0L;
            finalSynchronizationBoundaryPositionPendingBefore = 0L;
            finalSynchronizationBoundaryYawClosedCount = 0L;
            finalSynchronizationBoundaryPositionClosedCount = 0L;
            finalSynchronizationBoundaryYawPendingAfter = 0L;
            finalSynchronizationBoundaryPositionPendingAfter = 0L;
            cleanupMovementStoppedBeforeFinalSynchronization = false;
            rowMaximumStationaryDrift = 0.0d;
            rowMaximumStuckSeconds = 0.0d;
            rowOscillations = 0;
            rowUnexpectedRepaths = 0;
            rowCommandReplacements = 0;
            rowSelectionLosses = 0;
            rowWaypointCount = 0;
            rowEndpointQualifiedWaypointCount = 0;
            rowMaximumCompletedLegFinalTargetDistance = 0.0d;
            rowMaximumCompletedLegBestTargetDistance = 0.0d;
            rowMaximumTurnDegrees = 0.0d;
            rowNonPairInterferenceCount = 0;
            rowSynchronizationObservationCount = 0L;
            rowUpdateSampleCount = 0L;
            rowLateUpdateSampleCount = 0L;
            rowUpdateCorrectionCount = 0L;
            rowLateUpdateCorrectionCount = 0L;
            rowSynchronizationFailureRecorded = false;
            rowUnmountedDoorControlPassed = false;
            rowDoorApproachSkipped = false;
            rowStopCommandIssuedCount = 0;
            rowRestartCompleted = false;
            rowSelectionMountNormalized = false;
            rowSelectionSwitchedAway = false;
            rowSelectionSwitchedBack = false;
            rowFormationSelectionNormalized = false;
            rowPauseEntered = false;
            rowPauseObservationSeconds = 0.0d;
            rowPauseMaximumDrift = 0.0d;
            rowPauseExited = false;
            rowDestinationCancelCommandAbsent = false;
            rowDestinationCancelRelationshipPreserved = false;
            previousLegDirection = Vector3.zero;
            selectedDoor = null;
            doorNearPoint = Vector3.zero;
            doorFarPoint = Vector3.zero;
            uninvolvedCommands = null;
            requiredPairNonPairSeparation = 0.0d;
        }

        private void ResetNavigationMetrics()
        {
            navigationDestination = Vector3.zero;
            navigationStart = Vector3.zero;
            navigationStablePosition = Vector3.zero;
            navigationCommand = null;
            navigationPath = null;
            navigationStartedAt = 0.0d;
            navigationLastProgressAt = 0.0d;
            navigationStableStartedAt = 0.0d;
            navigationBestDistance = double.MaxValue;
            navigationPreviousDistance = double.MaxValue;
            navigationMovedDistance = 0.0d;
            navigationMaximumStationaryDrift = 0.0d;
            navigationMaximumStuckSeconds = 0.0d;
            navigationAwayFrameRun = 0;
            navigationWasApproaching = false;
            navigationOscillations = 0;
            navigationRepaths = 0;
            navigationCommandReplacements = 0;
            navigationSelectionLosses = 0;
            navigationPauseRequested = false;
            navigationPauseStartedAt = 0.0d;
            navigationPausePosition = Vector3.zero;
            navigationPauseDrift = 0.0d;
            nonPairCommand = null;
            nonPairStart = Vector3.zero;
            nonPairTarget = Vector3.zero;
            nonPairMovedDistance = 0.0d;
            nonPairBestTargetDistance = double.MaxValue;
            nonPairFinalTargetDistance = double.MaxValue;
            mountFinalTargetDistance = double.MaxValue;
            minimumPairNonPairSeparation = double.MaxValue;
        }

        private void ResetPathProbe()
        {
            probeCandidates.Clear();
            probeRejections.Clear();
            probeIndex = 0;
            probeGeneration++;
            probePending = false;
            probeCallbackReady = false;
            probeCallbackAccepted = false;
            probeCallbackReason = null;
            probeRequested = Vector3.zero;
            probeEndpoint = Vector3.zero;
            probePathLength = 0.0d;
            probeDirectionFilter = null;
            probeDoorStrict = false;
        }

        private void FailCurrent(string message)
        {
            if (assertions == null)
            {
                assertions = new AssertionRecorder();
            }
            assertions.Fail(message);
        }

        private void RecordCleanupFailure(string message)
        {
            if (assertions != null && currentRow != null)
            {
                assertions.Fail(message);
            }
            else
            {
                errors.Add(message);
            }
        }

        private static IReadOnlyList<string> SelectRows(string scenario)
        {
            if (string.Equals(scenario, "movement-suite", StringComparison.Ordinal))
            {
                return SuiteRows;
            }
            foreach (var row in SuiteRows)
            {
                if (string.Equals(row, scenario, StringComparison.Ordinal))
                {
                    return new[] { row };
                }
            }
            return null;
        }

        private static string FormatTransitionErrors(TransitionResult result)
        {
            if (result == null)
            {
                return "transition result was null";
            }
            return result.Errors == null || result.Errors.Count == 0
                ? "state=" + result.State
                : string.Join(" | ", result.Errors);
        }

        private string FormatProbeRejections()
        {
            return probeRejections.Count == 0 ? "none recorded" : string.Join("; ", probeRejections.Take(8).ToArray());
        }

        private static double PlanarDistance(Vector3 first, Vector3 second)
        {
            var x = (double)first.x - second.x;
            var z = (double)first.z - second.z;
            return Math.Sqrt(x * x + z * z);
        }

        private static Vector3 PlanarDirection(Vector3 from, Vector3 to)
        {
            return PlanarNormalized(new Vector3(to.x - from.x, 0f, to.z - from.z));
        }

        private static Vector3 PlanarNormalized(Vector3 vector)
        {
            vector.y = 0f;
            return vector.sqrMagnitude <= 0.0001f ? Vector3.zero : vector.normalized;
        }

        private static string FormatPosition(Vector3 position)
        {
            return "(" + position.x.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                position.y.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                position.z.ToString("0.00", CultureInfo.InvariantCulture) + ")";
        }

        private static string BuildHierarchyName(Transform transform)
        {
            if (transform == null)
            {
                return "<missing>";
            }
            var names = new List<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                names.Add(current.name ?? "<unnamed>");
            }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        private static string GetRowToken(string row)
        {
            foreach (var allowed in SuiteRows)
            {
                if (string.Equals(row, allowed, StringComparison.Ordinal))
                {
                    return row.Substring("mounted-pair-".Length);
                }
            }
            throw new InvalidOperationException("Screenshot row is outside the fixed movement allowlist.");
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RuntimeMovementScenarioEngine));
            }
        }

        private enum EngineStep
        {
            BeginRow,
            ExecuteRow,
            AwaitPreCleanupCaptures,
            AwaitCleanupFrame,
            AwaitFinalCaptures
        }

        private enum NavigationMode
        {
            Normal,
            StopEarly,
            PauseResume,
            Formation,
            UnmountedControl
        }

        private enum NavigationStage
        {
            None,
            Searching,
            Moving,
            Pausing,
            Stabilizing,
            Complete,
            Failed
        }

        private sealed class CleanupStateEvidence
        {
            public string Trigger { get; set; }
            public string RelationshipState { get; set; }
            public bool HasMountedResidual { get; set; }
            public bool? RiderStockAgentEnabled { get; set; }
            public bool? MountStockAgentEnabled { get; set; }
            public bool? RiderAvoidanceDisabled { get; set; }
            public bool? MountAvoidanceDisabled { get; set; }
            public bool RiderOverridePresent { get; set; }
            public bool MountOverridePresent { get; set; }
            public bool RiderSelected { get; set; }
            public bool MountSelected { get; set; }
            public string[] SelectedUnitIds { get; set; }
            public bool? Paused { get; set; }
            public bool? RiderForbidRotation { get; set; }
            public bool AttachmentLeaseActive { get; set; }
            public bool AttachmentRestoreVerified { get; set; }
            public bool AttachmentResidue { get; set; }
            public bool RiderParentMatchesAttachment { get; set; }
            public string RiderParent { get; set; }
            public string AttachmentParent { get; set; }
            public string SourceAnchor { get; set; }
            public string AttachmentRiskState { get; set; }

            public static CleanupStateEvidence Capture(
                CleanupTrigger trigger,
                GameMountedRelationshipService relationship,
                UnitEntityData rider,
                UnitEntityData mount,
                Game game)
            {
                var selected = SelectionManager.Instance?.SelectedUnits;
                return new CleanupStateEvidence
                {
                    Trigger = trigger.ToString(),
                    RelationshipState = relationship.State.ToString(),
                    HasMountedResidual = relationship.State != KingmakerMountedCombat.Domain.RelationshipState.Unmounted || relationship.Rider != null ||
                        relationship.Mount != null || relationship.Runtime.MovementAgent != null || relationship.Runtime.HasPresentationAttachmentResidue,
                    RiderStockAgentEnabled = rider?.View?.AgentASP?.enabled,
                    MountStockAgentEnabled = mount?.View?.AgentASP?.enabled,
                    RiderAvoidanceDisabled = rider?.View?.AgentASP?.AvoidanceDisabled,
                    MountAvoidanceDisabled = mount?.View?.AgentASP?.AvoidanceDisabled,
                    RiderOverridePresent = rider?.View?.AgentOverride != null,
                    MountOverridePresent = mount?.View?.AgentOverride != null,
                    RiderSelected = selected != null && rider != null && selected.Contains(rider),
                    MountSelected = selected != null && mount != null && selected.Contains(mount),
                    SelectedUnitIds = selected == null ? new string[0] : selected.Where(unit => unit != null).Select(unit => unit.UniqueId).ToArray(),
                    Paused = game == null ? (bool?)null : game.IsPaused,
                    RiderForbidRotation = rider?.View == null ? (bool?)null : rider.View.ForbidRotation,
                    AttachmentLeaseActive = relationship.Runtime.PresentationAttachmentLeaseActive,
                    AttachmentRestoreVerified = relationship.Runtime.PresentationAttachmentRestoreVerified,
                    AttachmentResidue = relationship.Runtime.HasPresentationAttachmentResidue,
                    RiderParentMatchesAttachment = relationship.Runtime.RiderParentMatchesAttachment,
                    RiderParent = rider?.View?.transform.parent == null ? null : BuildHierarchyName(rider.View.transform.parent),
                    AttachmentParent = relationship.Runtime.PresentationAttachmentParentName,
                    SourceAnchor = relationship.Runtime.PresentationSourceAnchorName,
                    AttachmentRiskState = relationship.Runtime.PresentationAttachmentRiskState
                };
            }
        }

        private sealed class NonPairSnapshot
        {
            private UnitEntityData Unit { get; set; }
            private UnitMovementAgent Agent { get; set; }
            private bool AgentEnabled { get; set; }
            private bool AvoidanceDisabled { get; set; }
            private object Override { get; set; }

            public static NonPairSnapshot Capture(UnitEntityData unit)
            {
                if (unit?.View?.AgentASP == null || unit.Commands == null)
                {
                    return null;
                }
                return new NonPairSnapshot
                {
                    Unit = unit,
                    Agent = unit.View.AgentASP,
                    AgentEnabled = unit.View.AgentASP.enabled,
                    AvoidanceDisabled = unit.View.AgentASP.AvoidanceDisabled,
                    Override = unit.View.AgentOverride
                };
            }

            public bool MatchesRestoredStoppedState()
            {
                try
                {
                    return Unit != null && Unit.View != null && Unit.Commands != null &&
                        ReferenceEquals(Unit.View.AgentASP, Agent) && Agent != null &&
                        Agent.enabled == AgentEnabled && Agent.AvoidanceDisabled == AvoidanceDisabled &&
                        ReferenceEquals(Unit.View.AgentOverride, Override) && Unit.Commands.Move == null &&
                        !Agent.WantsToMove && !Agent.IsReallyMoving;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private sealed class PairSnapshot
        {
            public UnitEntityData Rider { get; private set; }
            public UnitEntityData Mount { get; private set; }
            public UnitEntityView RiderView { get; private set; }
            public UnitEntityView MountView { get; private set; }
            public UnitMovementAgent RiderStockAgent { get; private set; }
            public UnitMovementAgent MountStockAgent { get; private set; }
            public bool RiderAgentWasEnabled { get; private set; }
            public bool MountAgentWasEnabled { get; private set; }
            public bool RiderAvoidanceWasDisabled { get; private set; }
            public bool MountAvoidanceWasDisabled { get; private set; }
            public object RiderOverride { get; private set; }
            public object MountOverride { get; private set; }
            public int RiderOverrideComponentCount { get; private set; }
            public Transform RiderParent { get; private set; }
            public int RiderSiblingIndex { get; private set; }
            public Vector3 RiderLocalScale { get; private set; }
            public bool RiderForbidRotationWasEnabled { get; private set; }

            public static PairSnapshot Capture(UnitEntityData rider, UnitEntityData mount)
            {
                if (rider == null || mount == null || rider.View == null || mount.View == null ||
                    rider.View.AgentASP == null || mount.View.AgentASP == null)
                {
                    return null;
                }
                return new PairSnapshot
                {
                    Rider = rider,
                    Mount = mount,
                    RiderView = rider.View,
                    MountView = mount.View,
                    RiderStockAgent = rider.View.AgentASP,
                    MountStockAgent = mount.View.AgentASP,
                    RiderAgentWasEnabled = rider.View.AgentASP.enabled,
                    MountAgentWasEnabled = mount.View.AgentASP.enabled,
                    RiderAvoidanceWasDisabled = rider.View.AgentASP.AvoidanceDisabled,
                    MountAvoidanceWasDisabled = mount.View.AgentASP.AvoidanceDisabled,
                    RiderOverride = rider.View.AgentOverride,
                    MountOverride = mount.View.AgentOverride,
                    RiderOverrideComponentCount = rider.View.GetComponents<RiderMovementAgent>().Length,
                    RiderParent = rider.View.transform.parent,
                    RiderSiblingIndex = rider.View.transform.GetSiblingIndex(),
                    RiderLocalScale = rider.View.transform.localScale,
                    RiderForbidRotationWasEnabled = rider.View.ForbidRotation
                };
            }

            public bool RiderStateRestored()
            {
                try
                {
                    return Rider != null && RiderView != null && RiderStockAgent != null &&
                        Rider.View == RiderView && RiderView.AgentASP == RiderStockAgent &&
                        RiderStockAgent.enabled == RiderAgentWasEnabled &&
                        RiderStockAgent.AvoidanceDisabled == RiderAvoidanceWasDisabled &&
                        ReferenceEquals(RiderView.AgentOverride, RiderOverride) &&
                        RiderView.ForbidRotation == RiderForbidRotationWasEnabled;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public bool MountStateRestored()
            {
                try
                {
                    return Mount != null && MountView != null && MountStockAgent != null &&
                        Mount.View == MountView && MountView.AgentASP == MountStockAgent &&
                        MountStockAgent.enabled == MountAgentWasEnabled &&
                        MountStockAgent.AvoidanceDisabled == MountAvoidanceWasDisabled &&
                        ReferenceEquals(MountView.AgentOverride, MountOverride);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public bool RiderOverrideComponentCountRestored()
            {
                try
                {
                    return RiderView != null && RiderView.GetComponents<RiderMovementAgent>().Length == RiderOverrideComponentCount;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public bool RiderAttachmentStateRestored()
            {
                try
                {
                    return RiderView != null && RiderView.transform.parent == RiderParent &&
                        RiderView.transform.GetSiblingIndex() == RiderSiblingIndex &&
                        RiderView.transform.localScale == RiderLocalScale;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private sealed class SelectionSnapshot
        {
            private readonly List<UnitEntityData> units;

            private SelectionSnapshot(List<UnitEntityData> units)
            {
                this.units = units;
            }

            public static SelectionSnapshot Capture()
            {
                var manager = SelectionManager.Instance;
                return new SelectionSnapshot(manager == null ? new List<UnitEntityData>() : new List<UnitEntityData>(manager.SelectedUnits));
            }

            public void Restore()
            {
                var manager = SelectionManager.Instance;
                if (manager == null)
                {
                    return;
                }
                manager.MultiSelect(units.Where(unit => unit != null && unit.IsInGame && unit.IsDirectlyControllable && unit.View != null).Select(unit => unit.View).ToArray(), false);
            }

            public bool MatchesCurrent()
            {
                var manager = SelectionManager.Instance;
                if (manager == null)
                {
                    return false;
                }
                var expected = units.Where(unit => unit != null && unit.IsInGame && unit.IsDirectlyControllable && unit.View != null).ToArray();
                return manager.SelectedUnits.Count == expected.Length && expected.All(manager.SelectedUnits.Contains);
            }
        }

        private sealed class DoorCandidate
        {
            public DoorCandidate(StandardDoor door, Vector3 near, Vector3 far)
            {
                Door = door;
                Near = near;
                Far = far;
            }

            public StandardDoor Door { get; }
            public Vector3 Near { get; }
            public Vector3 Far { get; }
        }

        private sealed class AssertionRecorder
        {
            private readonly List<string> errors = new List<string>();

            public int PassCount { get; private set; }
            public int FailureCount { get; private set; }
            public IReadOnlyList<string> Errors => errors;

            public void Check(bool condition, string success, string failure)
            {
                if (condition)
                {
                    PassCount++;
                }
                else
                {
                    Fail(failure);
                }
            }

            public void Fail(string message)
            {
                FailureCount++;
                errors.Add(message);
            }
        }

        private sealed class ScreenshotEvidence
        {
            public string Milestone { get; set; }
            public string RelativePath { get; set; }
            public long Length { get; set; }
            public string Sha256 { get; set; }
        }

        private sealed class PositionEvidence
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public static PositionEvidence From(Vector3 value)
            {
                return new PositionEvidence { X = value.x, Y = value.y, Z = value.z };
            }
        }
    }
}
