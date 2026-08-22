using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Runs exact, save-backed combat probes against a runtime-only hostile target. The
    /// first qualified rows are intentionally narrow: one stationary rider melee hit in
    /// real time or one exact native rider turn, entered through the real ClickUnitHandler
    /// Harmony seam.
    /// </summary>
    internal sealed class RuntimeCombatScenarioEngine : IDisposable
    {
        internal const string EvidenceFileName = "combat-scenario-evidence.jsonl";
        private const string RiderHitRealTime = "mounted-rider-melee-hit-rt";
        private const string RiderHitTurnBased = "mounted-rider-melee-hit-tb";
        private const string RiderMissRealTime = "mounted-rider-melee-miss-rt";
        private const string MammothPrimaryHitRealTime = "mounted-mammoth-primary-hit-rt";
        private const string MammothPrimaryHitTurnBased = "mounted-mammoth-primary-hit-tb";
        private const string RiderMoveToAttackRealTime = "mounted-rider-melee-move-to-attack-rt";
        private const string RiderMoveToAttackTurnBased = "mounted-rider-melee-move-to-attack-tb";
        private const string RiderCommandCancelRealTime = "mounted-rider-melee-command-cancel-rt";
        private const string RiderCommandCancelTurnBased = "mounted-rider-melee-command-cancel-tb";
        private const string RiderCommandInterruptRealTime = "mounted-rider-melee-command-interrupt-rt";
        private const string RiderCommandInterruptTurnBased = "mounted-rider-melee-command-interrupt-tb";
        private const string RiderCombatEndRealTime = "mounted-rider-melee-combat-end-rt";
        private const string RiderCombatEndTurnBased = "mounted-rider-melee-combat-end-tb";
        private const string RiderHumanPlayRealTime = "mounted-rider-melee-human-play-path-rt";
        private const string RiderHumanPlayTurnBased = "mounted-rider-melee-human-play-path-tb";
        private const double RowTimeoutSeconds = 30.0d;
        private const double CleanupTimeoutSeconds = 10.0d;
        private const float SpawnDistance = 6.0f;

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
        private readonly MountedPlayerActionController playerAction;
        private readonly MountedCombatController combat;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly Stopwatch rowClock = new Stopwatch();
        private readonly string evidencePath;
        private readonly string dllSha256;
        private readonly string dllMvid;
        private readonly DiagnosticCombatInitiativeObservation initiativeTickObservation =
            new DiagnosticCombatInitiativeObservation();

        private DiagnosticCombatTargetService targetService;
        private MountedCombatRuleProbe ruleProbe;
        private CombatReachEvidence reachEvidence;
        private AssertionRecorder assertions;
        private UnitEntityData rider;
        private UnitEntityData mount;
        private UnitEntityData target;
        private string targetId;
        private string currentRow;
        private CombatEngineStep step;
        private bool originalUnsafeExperimentSetting;
        private bool settingLeaseOwned;
        private bool started;
        private bool completed;
        private bool disposed;
        private int frameNumber;
        private int cleanupFrame;
        private float riderStandardBefore;
        private float mountStandardBefore;
        private float riderMoveBefore;
        private float mountMoveBefore;
        private float riderStandardAfter;
        private float mountStandardAfter;
        private float riderMoveAfter;
        private float mountMoveAfter;
        private float pairApproachRadius;
        private float requestedTargetDistance;
        private float targetDistanceAtClick;
        private Vector3 riderPositionAtClick;
        private Vector3 mountPositionAtClick;
        private Vector3 targetPositionAtClick;
        private float riderDisplacementAtOutcome;
        private float mountDisplacementAtOutcome;
        private float targetDisplacementAtOutcome;
        private bool clickAccepted;
        private bool humanPlayArmedThroughPlayerAction;
        private bool humanPlayOverlayGuardExercised;
        private bool humanPlayPropagatedWorldClickSuppressed;
        private bool humanPlayArmedActionRetainedAfterOverlayClick;
        private bool directClickedUnitView;
        private string admissionFeedback;
        private string[] admissionRejectionCodes = new string[0];
        private MountedPairAttackOutcome outcome;
        private bool targetRemoved;
        private bool targetEntityRemoved;
        private bool targetRuntimeGroupRemoved;
        private bool targetRuntimeFactionRemoved;
        private bool targetDurabilityLeaseReleased;
        private bool targetBrainLeaseReleased;
        private bool targetSleeplessLeaseReleased;
        private bool targetNonPairPartyAiLeaseRestored;
        private bool combatMemoryRemoved;
        private CombatTargetProvisioningEvidence targetProvisioning;
        private bool relationshipClean;
        private bool combatCleared;
        private bool riderAgentInitiallyEnabled;
        private bool mountAgentInitiallyEnabled;
        private bool riderAvoidanceInitiallyDisabled;
        private bool mountAvoidanceInitiallyDisabled;
        private bool rowEvidenceWritten;
        private string poseProfileAtOutcome;
        private bool poseHealthyAtOutcome;
        private bool originalPause;
        private bool pauseLeaseOwned;
        private bool unpausedForRealTime;
        private bool pausedAtClick;
        private bool pauseRestored = true;
        private double cleanupStartedAtSeconds;
        private DiagnosticCombatDispatchReadinessSnapshot dispatchReadiness;
        private DiagnosticCombatEntryReadinessSnapshot entryReadiness;
        private DiagnosticCombatActionActorReadinessSnapshot actionActorReadiness;
        private DiagnosticNativeCombatJoinReadinessSnapshot nativeJoinReadiness;
        private DiagnosticTurnBasedDispatchReadinessSnapshot turnBasedReadiness;
        private NativeModeTransitionProbe realTimeBaselineModeProbe;
        private NativeModeTransitionProbe turnBasedModeProbe;
        private CombatCameraFollowerSnapshot cameraFollowerSnapshot;
        private bool cameraFollowerLeaseOwned;
        private bool cameraFollowerRestored = true;
        private bool turnBasedOriginalEnabled;
        private bool turnBasedTemporaryEnabled;
        private bool turnBasedOriginalRawCacheHadValue;
        private bool turnBasedRestoreDeliveryCompleted;
        private double modeRestoreStartedAtSeconds;
        private bool nativeRealtimePauseObserved;
        private bool realtimeUnpauseRequested;
        private bool turnBasedModeEnabledAtMount;
        private bool pairMountedBeforeTurnBasedEnable;
        private bool pairRetainedAfterTurnBasedEnable;
        private bool pairRetainedAfterRealtimeRestore;
        private string presentationAfterTurnBasedEnable;
        private string presentationAfterRealtimeRestore = "<not-observed>";
        private bool turnBasedControllerInitialized;
        private bool turnRosterContainsRider;
        private bool turnRosterContainsMount;
        private bool turnRosterContainsTarget;
        private bool nativeActionActorTurnStarted;
        private bool nativeActionActorTurnActingObservedAfterDispatch;
        private string currentTurnUnitIdAtDispatch;
        private bool currentTurnActingAtDispatch;
        private int roundNumberAtDispatch = -1;
        private string currentTurnUnitIdAtOutcome;
        private bool currentTurnActingAtOutcome;
        private bool turnBasedModeRestored = true;
        private bool turnBasedPersistedSettingUnchanged = true;
        private bool groundMovementStarted;
        private bool groundMovementCompleted;
        private Vector3 groundMovementDestination;
        private Vector3 riderPositionBeforeGroundMovement;
        private Vector3 mountPositionBeforeGroundMovement;
        private Vector3 targetPositionBeforeGroundMovement;
        private float riderGroundMovementDisplacement;
        private float mountGroundMovementDisplacement;
        private float targetGroundMovementDisplacement;
        private bool groundMovementPairRetained;
        private bool groundMovementSelectionRetained;
        private bool groundMovementPoseHealthy;
        private int movementToAttackObservationCount;
        private bool selectionRetainedDuringApproach = true;
        private bool uiCoherentDuringApproach = true;
        private bool terminationDelivered;
        private bool terminationRepeated;
        private bool wrapperPresentBeforeTermination;
        private bool delegatedMovePresentBeforeTermination;
        private bool riderQueueEmptyBeforeTermination;
        private bool mountQueueEmptyBeforeTermination;
        private bool childAbsentBeforeTermination;
        private float pairDistanceAtTermination;
        private float riderDisplacementAtTermination;
        private float mountDisplacementAtTermination;
        private float targetDisplacementAtTermination;
        private bool wrapperAbsentAfterTermination;
        private bool delegatedMoveAbsentAfterTermination;
        private bool riderQueueEmptyAfterTermination;
        private bool mountQueueEmptyAfterTermination;
        private bool mountAgentStoppedAfterTermination;
        private bool relationshipPreservedAfterTermination;
        private bool selectionRetainedAfterTermination;
        private bool uiCoherentAfterTermination;
        private int terminationLifecycleDeliveryCount;
        private bool terminationLifecycleDeliveriesExact;
        private MountedPairAttackCommand terminationWrapper;
        private Kingmaker.UnitLogic.Commands.UnitMoveTo terminationMove;

        public RuntimeCombatScenarioEngine(
            RuntimeRequest request,
            GameMountedRelationshipService relationship,
            MountedPlayerActionController playerAction,
            MountedCombatController combat,
            MountedLifecycleSubscriber lifecycle,
            DiagnosticSettings settings,
            IModLogger logger)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.playerAction = playerAction ?? throw new ArgumentNullException(nameof(playerAction));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            evidencePath = Path.Combine(request.EvidenceRoot, EvidenceFileName);
            var assembly = typeof(Main).Assembly;
            dllSha256 = ComputeSha256(assembly.Location);
            dllMvid = assembly.ManifestModule.ModuleVersionId.ToString();
        }

        public bool IsCompleted => completed;

        public IReadOnlyList<RuntimeSubscenarioResult> Results => results;

        public IReadOnlyList<string> Errors => errors;

        internal static bool SupportsScenario(string scenario)
        {
            return string.Equals(scenario, RiderHitRealTime, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderHitTurnBased, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderMissRealTime, StringComparison.Ordinal) ||
                string.Equals(scenario, MammothPrimaryHitRealTime, StringComparison.Ordinal) ||
                string.Equals(scenario, MammothPrimaryHitTurnBased, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderMoveToAttackRealTime, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderMoveToAttackTurnBased, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderCommandCancelRealTime, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderCommandCancelTurnBased, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderCommandInterruptRealTime, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderCommandInterruptTurnBased, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderCombatEndRealTime, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderCombatEndTurnBased, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderHumanPlayRealTime, StringComparison.Ordinal) ||
                string.Equals(scenario, RiderHumanPlayTurnBased, StringComparison.Ordinal);
        }

        private bool IsTurnBasedRow =>
                string.Equals(currentRow, RiderHitTurnBased, StringComparison.Ordinal) ||
                string.Equals(currentRow, MammothPrimaryHitTurnBased, StringComparison.Ordinal) ||
                string.Equals(currentRow, RiderMoveToAttackTurnBased, StringComparison.Ordinal) ||
                string.Equals(currentRow, RiderCommandCancelTurnBased, StringComparison.Ordinal) ||
                string.Equals(currentRow, RiderCommandInterruptTurnBased, StringComparison.Ordinal) ||
                string.Equals(currentRow, RiderCombatEndTurnBased, StringComparison.Ordinal) ||
                string.Equals(currentRow, RiderHumanPlayTurnBased, StringComparison.Ordinal);

        private bool IsHumanPlayRow =>
            string.Equals(currentRow, RiderHumanPlayRealTime, StringComparison.Ordinal) ||
            string.Equals(currentRow, RiderHumanPlayTurnBased, StringComparison.Ordinal);

        private bool IsMountedBeforeModeTransitionRow =>
            string.Equals(currentRow, RiderHumanPlayTurnBased, StringComparison.Ordinal);

        private bool IsMissRow => string.Equals(currentRow, RiderMissRealTime, StringComparison.Ordinal);

        private bool IsReachQualificationRow =>
            string.Equals(currentRow, RiderHitRealTime, StringComparison.Ordinal) ||
            string.Equals(currentRow, RiderHitTurnBased, StringComparison.Ordinal) ||
            IsHumanPlayRow ||
            string.Equals(currentRow, MammothPrimaryHitRealTime, StringComparison.Ordinal) ||
            string.Equals(currentRow, MammothPrimaryHitTurnBased, StringComparison.Ordinal);

        private bool IsMammothPrimaryRow =>
            string.Equals(currentRow, MammothPrimaryHitRealTime, StringComparison.Ordinal) ||
            string.Equals(currentRow, MammothPrimaryHitTurnBased, StringComparison.Ordinal);

        private bool IsMovementToAttackRow =>
            string.Equals(currentRow, RiderMoveToAttackRealTime, StringComparison.Ordinal) ||
            string.Equals(currentRow, RiderMoveToAttackTurnBased, StringComparison.Ordinal);

        private bool IsCommandCancellationRow =>
            string.Equals(currentRow, RiderCommandCancelRealTime, StringComparison.Ordinal) ||
            string.Equals(currentRow, RiderCommandCancelTurnBased, StringComparison.Ordinal);

        private bool IsCommandInterruptionRow =>
            string.Equals(currentRow, RiderCommandInterruptRealTime, StringComparison.Ordinal) ||
            string.Equals(currentRow, RiderCommandInterruptTurnBased, StringComparison.Ordinal);

        private bool IsCombatEndTerminationRow =>
            string.Equals(currentRow, RiderCombatEndRealTime, StringComparison.Ordinal) ||
            string.Equals(currentRow, RiderCombatEndTurnBased, StringComparison.Ordinal);

        private bool IsCommandTerminationRow =>
            IsCommandCancellationRow || IsCommandInterruptionRow || IsCombatEndTerminationRow;

        private bool IsApproachRow => IsMovementToAttackRow || IsCommandTerminationRow;

        private MountedCombatActionKind AttackAction => IsMammothPrimaryRow
            ? MountedCombatActionKind.MountPrimaryNatural
            : MountedCombatActionKind.RiderMelee;

        private UnitEntityData AttackActor => IsMammothPrimaryRow ? mount : rider;

        private string ExpectedActorRole => IsMammothPrimaryRow ? "mount" : "rider";

        public void Start()
        {
            ThrowIfDisposed();
            if (started)
            {
                throw new InvalidOperationException("Combat scenario engine has already started.");
            }
            if (!SupportsScenario(request.Scenario))
            {
                throw new InvalidOperationException("Scenario is outside the exact combat runtime allowlist.");
            }

            started = true;
            currentRow = request.Scenario;
            assertions = new AssertionRecorder();
            originalUnsafeExperimentSetting = settings.EnableUnsafeMovementExperiment;
            settings.EnableUnsafeMovementExperiment = true;
            settingLeaseOwned = true;
            rowClock.Start();
            step = CombatEngineStep.BeginRow;
            logger.Info("Combat runtime engine started for " + currentRow + ".");
        }

        public void Update()
        {
            ThrowIfDisposed();
            if (!started)
            {
                throw new InvalidOperationException("Combat scenario engine must be started before Update.");
            }
            if (completed)
            {
                return;
            }

            frameNumber++;
            try
            {
                targetService?.ObserveTargetLifeState();
                if (rowClock.Elapsed.TotalSeconds > RowTimeoutSeconds &&
                    step != CombatEngineStep.AwaitTurnBasedRealtimeRestore &&
                    step != CombatEngineStep.AwaitCleanupFrame)
                {
                    assertions.Fail("Combat row exceeded its " + RowTimeoutSeconds + " second monotonic deadline at " + step +
                        ". " + DescribeDeadlineReadiness() + ".");
                    BeginCleanup();
                    return;
                }

                switch (step)
                {
                    case CombatEngineStep.BeginRow:
                        BeginRow();
                        break;
                    case CombatEngineStep.AwaitTurnBasedMode:
                        AwaitTurnBasedModeAndMount();
                        break;
                    case CombatEngineStep.AwaitMountedFrame:
                        EnterCombat();
                        break;
                    case CombatEngineStep.AwaitCombatFrame:
                        IssueAttackWhenReady();
                        break;
                    case CombatEngineStep.AwaitGroundMovement:
                        ObserveGroundMovement();
                        break;
                    case CombatEngineStep.AwaitOutcome:
                        ObserveOutcome();
                        break;
                    case CombatEngineStep.AwaitTurnBasedRealtimeRestore:
                        AwaitTurnBasedRealtimeRestore();
                        break;
                    case CombatEngineStep.AwaitCleanupFrame:
                        VerifyCleanupAndComplete();
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected combat engine step: " + step + ".");
                }
            }
            catch (Exception exception)
            {
                logger.Exception("Combat runtime row threw", exception);
                assertions.Fail(exception.GetType().Name + ": " + exception.Message);
                BeginCleanup();
            }
        }

        internal void ObserveCombatCooldownTick(
            UnitEntityData unit,
            float prefixInitiative,
            float postfixInitiative,
            float gameDeltaTime,
            bool prepared,
            bool inCombat,
            bool awake)
        {
            if (completed || step != CombatEngineStep.AwaitCombatFrame || unit == null || unit != AttackActor)
            {
                return;
            }

            initiativeTickObservation.Observe(
                prefixInitiative,
                postfixInitiative,
                gameDeltaTime,
                prepared,
                inCombat,
                awake);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (started && !completed)
            {
                errors.Add("Combat engine was disposed before its exact row completed.");
            }

            try
            {
                BestEffortCleanup();
                if (!rowEvidenceWritten && assertions != null)
                {
                    WriteRowEvidence();
                }
            }
            finally
            {
                ruleProbe?.Dispose();
                ruleProbe = null;
                targetService?.Dispose();
                targetService = null;
                try { turnBasedModeProbe?.Dispose(); }
                catch (Exception exception) { errors.Add("Turn-based mode probe disposal failed: " + exception.Message); }
                turnBasedModeProbe = null;
                try { realTimeBaselineModeProbe?.Dispose(); }
                catch (Exception exception) { errors.Add("Real-time baseline mode probe disposal failed: " + exception.Message); }
                realTimeBaselineModeProbe = null;
                RestorePause();
                RestoreSettings();
                rowClock.Stop();
                disposed = true;
            }
        }

        private void BeginRow()
        {
            initiativeTickObservation.Reset();
            movementToAttackObservationCount = 0;
            selectionRetainedDuringApproach = true;
            uiCoherentDuringApproach = true;
            assertions.Check(relationship.State == RelationshipState.Unmounted,
                "Relationship began Unmounted.");
            assertions.Check(!CombatController.IsInTurnBasedCombat(),
                "Combat row began from the exact real-time baseline.");
            assertions.Check(Game.Instance != null && Game.Instance.CurrentlyLoadedArea != null &&
                    Game.Instance.CurrentMode == Kingmaker.GameModes.GameModeType.Default,
                "Loaded Working fixture began in exact Default gameplay mode.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            originalPause = Game.Instance.IsPaused;
            pauseLeaseOwned = true;
            pauseRestored = false;

            realTimeBaselineModeProbe = new NativeModeTransitionProbe(false);
            assertions.Check(!realTimeBaselineModeProbe.TemporaryValue,
                "Combat row leased the exact native real-time setting baseline.");
            realTimeBaselineModeProbe.DispatchTemporaryValueIfRequired();
            assertions.Check(realTimeBaselineModeProbe.TemporaryValueIsCurrent &&
                    !CombatController.IsInTurnBasedCombat(),
                "Combat row established real-time mode before mounting or combat entry.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            if (IsTurnBasedRow && !IsMountedBeforeModeTransitionRow)
            {
                turnBasedModeProbe = new NativeModeTransitionProbe(true);
                CaptureTurnBasedModeLeaseIdentity();
                assertions.Check(!turnBasedModeProbe.OriginalValue && turnBasedModeProbe.TemporaryValue,
                    "Turn-based combat row leased an exact false-to-true native mode transition.");
                if (assertions.FailureCount != 0)
                {
                    BeginCleanup();
                    return;
                }
                turnBasedModeRestored = false;
                turnBasedPersistedSettingUnchanged = false;
                turnBasedModeProbe.DispatchTemporaryValueIfRequired();
                step = CombatEngineStep.AwaitTurnBasedMode;
                return;
            }

            ResolveAndMountPair();
        }

        private void AwaitTurnBasedModeAndMount()
        {
            if (turnBasedModeProbe == null || !turnBasedModeProbe.TemporaryValueIsCurrent ||
                Game.Instance?.TurnBasedCombatController == null)
            {
                return;
            }
            if (IsMountedBeforeModeTransitionRow)
            {
                pairRetainedAfterTurnBasedEnable = relationship.State == RelationshipState.Mounted &&
                    relationship.Rider == rider && relationship.Mount == mount &&
                    relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied;
                assertions.Check(pairRetainedAfterTurnBasedEnable,
                    "The exact mounted pair and accepted Mammoth presentation survived RT-to-TB transition.");
                EnterCombat();
                return;
            }

            turnBasedModeEnabledAtMount = true;
            ResolveAndMountPair();
        }

        private void ResolveAndMountPair()
        {
            string resolutionError;
            assertions.Check(relationship.TryResolveAutomationPair(out rider, out mount, out resolutionError),
                "Exact Medium-humanoid/Mammoth automation pair resolved: " + (resolutionError ?? "unknown error") + ".");
            if (rider == null || mount == null || rider.View?.AgentASP == null || mount.View?.AgentASP == null)
            {
                assertions.Fail("Resolved combat pair lacks exact views and stock agents.");
                BeginCleanup();
                return;
            }

            riderAgentInitiallyEnabled = rider.View.AgentASP.enabled;
            mountAgentInitiallyEnabled = mount.View.AgentASP.enabled;
            riderAvoidanceInitiallyDisabled = rider.View.AgentASP.AvoidanceDisabled;
            mountAvoidanceInitiallyDisabled = mount.View.AgentASP.AvoidanceDisabled;
            assertions.Check(riderAgentInitiallyEnabled && mountAgentInitiallyEnabled &&
                    !riderAvoidanceInitiallyDisabled && !mountAvoidanceInitiallyDisabled,
                "Pair began with exact stock movement authority and avoidance.");
            assertions.Check(string.Equals(mount.Blueprint.AssetGuid, KingmakerMountedPairRuntime.MammothBlueprintGuid, StringComparison.Ordinal),
                "Resolved mount is the exact supported Mammoth profile.");
            assertions.Check(SelectionManager.Instance != null,
                "Native SelectionManager is available.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, false, false);
            if (IsMountedBeforeModeTransitionRow)
            {
                string cameraError;
                cameraFollowerSnapshot = CombatCameraFollowerSnapshot.TryCapture(Game.Instance, out cameraError);
                assertions.Check(cameraFollowerSnapshot != null,
                    "Exact native camera-follower state was captured for bounded human-play transition restoration: " +
                    (cameraError ?? "available") + ".");
                if (cameraFollowerSnapshot != null)
                {
                    cameraFollowerLeaseOwned = true;
                    cameraFollowerRestored = false;
                    assertions.Check(Game.Instance.CameraController.Follower.Follow(rider),
                        "Native camera follower accepted the exact selected rider before RT-to-TB transition.");
                }
                if (assertions.FailureCount != 0)
                {
                    BeginCleanup();
                    return;
                }
            }
            var mounted = relationship.MountAutomationPair();
            assertions.Check(mounted.Succeeded && relationship.State == RelationshipState.Mounted,
                "Exact automation pair mounted for combat: " + FormatTransitionErrors(mounted) + ".");
            if (!mounted.Succeeded || relationship.State != RelationshipState.Mounted)
            {
                BeginCleanup();
                return;
            }
            step = CombatEngineStep.AwaitMountedFrame;
        }

        private void EnterCombat()
        {
            if (IsMountedBeforeModeTransitionRow && turnBasedModeProbe == null)
            {
                pairMountedBeforeTurnBasedEnable = relationship.State == RelationshipState.Mounted &&
                    relationship.Rider == rider && relationship.Mount == mount;
                assertions.Check(pairMountedBeforeTurnBasedEnable,
                    "The exact pair mounted in real time before the native turn-based transition.");
                turnBasedModeProbe = new NativeModeTransitionProbe(true);
                CaptureTurnBasedModeLeaseIdentity();
                assertions.Check(!turnBasedModeProbe.OriginalValue && turnBasedModeProbe.TemporaryValue,
                    "Human-play TB row leased an exact false-to-true native mode transition after mounting.");
                if (assertions.FailureCount != 0)
                {
                    BeginCleanup();
                    return;
                }
                turnBasedModeRestored = false;
                turnBasedPersistedSettingUnchanged = false;
                turnBasedModeProbe.DispatchTemporaryValueIfRequired();
                step = CombatEngineStep.AwaitTurnBasedMode;
                return;
            }

            assertions.Check(relationship.State == RelationshipState.Mounted &&
                    relationship.Runtime.PoseConfigured && relationship.Runtime.PoseHealthy &&
                    relationship.Runtime.PoseFrameApplied,
                "Accepted Mammoth-specific pose remained healthy on the mounted combat frame.");
            assertions.Check(!rider.View.AgentASP.enabled && mount.View.AgentASP.enabled &&
                    rider.View.AgentASP.AvoidanceDisabled && !mount.View.AgentASP.AvoidanceDisabled,
                "Mammoth is the sole stock pathfinding authority while mounted.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            targetService = new DiagnosticCombatTargetService(logger);
            var spawnPoint = FindWalkablePoint(mount.Position, SpawnDistance, 0.4f);
            target = targetService.Spawn(
                rider,
                mount,
                spawnPoint,
                request.RunId,
                true,
                IsMammothPrimaryRow || IsApproachRow || IsMountedBeforeModeTransitionRow);
            targetId = target.UniqueId;
            targetProvisioning = CombatTargetProvisioningEvidence.From(targetService, target);
            assertions.Check(target != null && target.IsInState && target.View != null &&
                    target.IsEnemy(rider) && rider.IsEnemy(target) &&
                    AttackActor != null && AttackActor.IsEnemy(target) && AttackActor.CanAttack(target),
                "Runtime-only hostile Mammoth target passed exact creation gates.");

            if (IsReachQualificationRow)
            {
                CaptureInitialReachEvidence();
            }

            var rangeProbe = new MountedPairSingleAttack(target, rider, mount, !IsMammothPrimaryRow);
            rangeProbe.Init(AttackActor);
            pairApproachRadius = rangeProbe.PairApproachRadius;
            float finalDistance;
            var placementCalculated = IsApproachRow
                ? MountedCombatSpatialPolicy.TryCalculateDiagnosticApproachTargetDistance(
                    pairApproachRadius,
                    out finalDistance)
                : MountedCombatSpatialPolicy.TryCalculateDiagnosticTargetDistance(
                    pairApproachRadius,
                    out finalDistance);
            requestedTargetDistance = finalDistance;
            assertions.Check(placementCalculated,
                IsApproachRow
                    ? "Mounted rider pair approach radius admits a bounded out-of-range diagnostic approach."
                    : "Mounted rider pair approach radius admits the bounded diagnostic placement.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            var attackPoint = FindWalkablePoint(
                mount.Position,
                finalDistance,
                MountedCombatSpatialPolicy.DiagnosticPlacementTolerance);
            target.Translocate(attackPoint, null);
            target.View.ForcePlaceAboveGround();
            target.Commands.InterruptAll();
            target.HoldState = true;

            assertions.Check(targetService.PrepareForPlayerClick(target),
                "Runtime-only target was made exactly visible before native combat memory provisioning.");
            assertions.Check(targetService.QueueBidirectionalCombatMemory(rider, target),
                "Exact rider/target pair was queued through native bidirectional combat memory.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }
            step = CombatEngineStep.AwaitCombatFrame;
        }

        private void IssueAttackWhenReady()
        {
            var game = Game.Instance;
            if (game == null)
            {
                return;
            }

            if (game.IsPaused)
            {
                game.IsPaused = false;
            }
            var gameUnpaused = !game.IsPaused;
            unpausedForRealTime = !IsTurnBasedRow && gameUnpaused;
            var combatMemoryLeaseHealthy = targetService != null &&
                targetService.RefreshBidirectionalCombatMemoryLease();
            var riderState = rider?.Descriptor?.State;
            var mountState = mount?.Descriptor?.State;
            var targetState = target?.Descriptor?.State;
            var playerGroup = rider?.Group;
            var targetGroup = target?.Group;
            nativeJoinReadiness = new DiagnosticNativeCombatJoinReadinessSnapshot(
                rider != null && rider.IsInGame,
                mount != null && mount.IsInGame,
                target != null && target.IsInGame,
                riderState != null && riderState.IsConscious,
                mountState != null && mountState.IsConscious,
                targetState != null && targetState.IsConscious,
                riderState != null && (bool)riderState.IsIgnoredByCombat,
                mountState != null && (bool)mountState.IsIgnoredByCombat,
                targetState != null && (bool)targetState.IsIgnoredByCombat,
                playerGroup != null && playerGroup.Any(unit => unit == rider),
                playerGroup != null && playerGroup.Any(unit => unit == mount),
                targetGroup != null && targetGroup.Any(unit => unit == target),
                MemoryEnemiesContain(rider, target),
                MemoryEnemiesContain(target, rider),
                rider != null && !rider.IsInFogOfWar,
                target != null && !target.IsInFogOfWar,
                riderState != null && !((bool)riderState.IsInStealth && rider.Stealth != null && rider.Stealth.InAmbush),
                targetState != null && !((bool)targetState.IsInStealth && target.Stealth != null && target.Stealth.InAmbush));
            entryReadiness = new DiagnosticCombatEntryReadinessSnapshot(
                combatMemoryLeaseHealthy,
                targetService != null && targetService.PlayerGroupMemoryContainsTarget,
                targetService != null && targetService.TargetGroupMemoryContainsRider,
                rider != null && rider.IsInCombat,
                mount != null && mount.IsInCombat,
                target != null && target.IsInCombat,
                game.Player != null && game.Player.IsInCombat,
                rider?.CombatState != null && rider.CombatState.Prepared,
                rider != null && game.State?.AwakeUnits != null && game.State.AwakeUnits.Contains(rider),
                target != null && game.State?.AwakeUnits != null && game.State.AwakeUnits.Contains(target),
                game.CurrentMode == GameModeType.Default,
                rider?.CombatState == null ? float.MaxValue : rider.CombatState.Cooldown.Initiative,
                game.TimeController == null ? 0f : game.TimeController.GameDeltaTime);
            if (!nativeJoinReadiness.AllPassed || !entryReadiness.AllPassed)
            {
                return;
            }

            if (IsTurnBasedRow)
            {
                var turnController = game.TurnBasedCombatController;
                turnBasedControllerInitialized = turnController != null && turnController.Initialized;
                turnRosterContainsRider = ContainsTurnRosterUnit(turnController, rider);
                turnRosterContainsMount = ContainsTurnRosterUnit(turnController, mount);
                turnRosterContainsTarget = ContainsTurnRosterUnit(turnController, target);
                if (!turnBasedControllerInitialized || !turnRosterContainsRider ||
                    !turnRosterContainsMount || !turnRosterContainsTarget)
                {
                    turnBasedReadiness = CaptureTurnBasedReadiness(turnController);
                    return;
                }
                if (!nativeActionActorTurnStarted)
                {
                    if (IsMammothPrimaryRow && combat.ArmedAction != AttackAction)
                    {
                        assertions.Check(combat.Arm(AttackAction),
                            "Mammoth primary was armed before its exact native turn began.");
                        if (assertions.FailureCount != 0)
                        {
                            BeginCleanup();
                            return;
                        }
                    }
                    turnController.StartTurn(AttackActor);
                    nativeActionActorTurnStarted = true;
                    return;
                }

                turnBasedReadiness = CaptureTurnBasedReadiness(turnController);
                if (!turnBasedReadiness.AllPassed)
                {
                    return;
                }
                if (IsMountedBeforeModeTransitionRow && presentationAfterTurnBasedEnable == null)
                {
                    presentationAfterTurnBasedEnable = relationship.CapturePresentationObservation();
                    assertions.Check(IsRiderUiOwnershipCoherent(presentationAfterTurnBasedEnable, false),
                        "The rider view, selection, portrait, action bar, and camera remained coherent after RT-to-TB transition.");
                }
            }

            var handsEquipment = game.HandsEquipmentController;
            var actionActor = AttackActor;
            actionActorReadiness = new DiagnosticCombatActionActorReadinessSnapshot(
                IsTurnBasedRow,
                AttackActor?.UniqueId,
                actionActor?.UniqueId,
                actionActor?.CombatState != null && actionActor.CombatState.Prepared,
                actionActor?.CombatState != null && actionActor.CombatState.CanActInCombat,
                actionActor?.CombatState == null
                    ? float.MaxValue
                    : actionActor.CombatState.Cooldown.Initiative);
            if (!actionActorReadiness.AllPassed)
            {
                return;
            }
            dispatchReadiness = new DiagnosticCombatDispatchReadinessSnapshot(
                gameUnpaused,
                actionActor.CombatState.CanActInCombat,
                !actionActor.AreHandsBusyWithAnimation,
                handsEquipment != null,
                handsEquipment != null && !handsEquipment.IsUpdateScheduledFor(actionActor));
            if (!dispatchReadiness.AllPassed)
            {
                return;
            }

            if (IsMountedBeforeModeTransitionRow && !groundMovementCompleted)
            {
                BeginGroundMovement();
                return;
            }

            assertions.Check(IsTurnBasedRow
                    ? CombatController.IsInTurnBasedCombat()
                    : !CombatController.IsInTurnBasedCombat(),
                "Combat mode remained exact at dispatch.");
            assertions.Check(entryReadiness.AllPassed,
                "Native memory, combat entry, rider preparation, and Default-mode time remained exact at dispatch.");
            assertions.Check(actionActorReadiness.AllPassed,
                "The exact action actor retained native preparation, initiative, and action eligibility at dispatch.");
            assertions.Check(nativeJoinReadiness.AllPassed,
                "Every exact native UnitCombatJoinController eligibility gate remained healthy at dispatch.");
            assertions.Check(dispatchReadiness.AllPassed,
                "Combat dispatch waited for unpaused initiative, hands, and equipment readiness.");
            if (IsTurnBasedRow)
            {
                assertions.Check(turnBasedReadiness != null && turnBasedReadiness.AllPassed,
                    "Turn-based dispatch retained the exact initialized roster and native action-actor turn.");
            }
            assertions.Check(relationship.State == RelationshipState.Mounted,
                "Native combat entry retained the mounted relationship.");
            assertions.Check(target.IsInState && target.Descriptor.State.IsConscious && !target.Descriptor.State.IsFinallyDead,
                "Diagnostic target remained live at dispatch.");

            assertions.Check(RetainDiagnosticTargetPlacementAtDispatch(),
                IsApproachRow
                    ? "Diagnostic target was retained at the exact actor-specific out-of-range approach placement before dispatch."
                    : "Diagnostic target was retained at the exact current actor-specific near-boundary placement before dispatch.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            var targetPreparedForClick = targetService != null && targetService.PrepareForPlayerClick(target);
            var actionWeapon = IsMammothPrimaryRow
                ? NativeSingleAttackWeaponResolver.Resolve(mount)?.Weapon
                : rider.GetFirstWeapon();
            var clickSafety = new DiagnosticCombatClickSafetySnapshot(
                targetPreparedForClick && targetService.Target == target,
                targetService != null && targetService.TargetFogOfWarCleared,
                targetService != null && targetService.TargetViewVisible,
                targetService != null && targetService.TargetVisibleForPlayer,
                targetService != null && targetService.TargetCommandsEmptyAtClick,
                targetService != null && targetService.TargetAgentEnabledAtClick,
                targetService != null && targetService.TargetAgentStoppedAtClick,
                targetService != null && targetService.TargetBrainSuppressedAtClick,
                target.View != null && target.View.gameObject.GetComponent<UnitEntityView>() == target.View,
                actionActor != null && actionActor.CanAttack(target),
                actionWeapon?.Blueprint != null && !actionWeapon.Blueprint.IsRanged &&
                    (!IsMammothPrimaryRow || actionWeapon.Blueprint.IsNatural));
            assertions.Check(clickSafety.AllPassed,
                "Diagnostic target passed exact player-click gates: " + clickSafety.FailureSummary + ".");

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var selected = SelectionManager.Instance.SelectedUnits;
            assertions.Check(selected != null && selected.Count == 1 && selected[0] == rider,
                "Exactly the rider owned player selection at dispatch.");
            assertions.Check(combat.CanShowCombatActions,
                "Mounted combat actions were available only for the exact selected pair in combat.");
            assertions.Check(actionActor.HasStandardAction(),
                "The exact action actor owned an available Standard action before dispatch.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            riderStandardBefore = rider.CombatState.Cooldown.StandardAction;
            mountStandardBefore = mount.CombatState.Cooldown.StandardAction;
            riderMoveBefore = rider.CombatState.Cooldown.MoveAction;
            mountMoveBefore = mount.CombatState.Cooldown.MoveAction;
            riderPositionAtClick = rider.Position;
            mountPositionAtClick = mount.Position;
            targetPositionAtClick = target.Position;
            pausedAtClick = Game.Instance.IsPaused;
            targetDistanceAtClick = HorizontalDistance(mountPositionAtClick, targetPositionAtClick);
            if (IsReachQualificationRow)
            {
                CaptureDispatchReachEvidence();
            }
            if (IsApproachRow)
            {
                assertions.Check(targetDistanceAtClick > pairApproachRadius + MountedCombatSpatialPolicy.RangeTolerance,
                    "Target began outside the exact Mammoth-origin rider melee radius at dispatch.");
                assertions.Check(MountedCombatSpatialPolicy.IsBoundedDiagnosticApproachTargetDistance(
                        pairApproachRadius,
                        targetDistanceAtClick),
                    "Diagnostic target retained the exact bounded movement-to-attack placement.");
            }
            else
            {
                assertions.Check(targetDistanceAtClick <= pairApproachRadius + MountedCombatSpatialPolicy.RangeTolerance,
                    "Target was inside the exact Mammoth-origin rider melee radius at dispatch.");
                assertions.Check(MountedCombatSpatialPolicy.IsBoundedDiagnosticTargetDistance(
                        pairApproachRadius,
                        targetDistanceAtClick),
                    "Diagnostic target retained the exact near-boundary mounted-range placement.");
            }

            if (IsHumanPlayRow && !humanPlayOverlayGuardExercised)
            {
                humanPlayArmedThroughPlayerAction = playerAction.ArmCombatActionFromOverlay(AttackAction);
                var propagatedClickAccepted = new ClickUnitHandler().OnClick(
                    target.View.gameObject,
                    target.Position,
                    0,
                    false,
                    false);
                humanPlayPropagatedWorldClickSuppressed = !propagatedClickAccepted;
                humanPlayArmedActionRetainedAfterOverlayClick =
                    combat.ArmedAction == AttackAction && !combat.HasActiveCommand;
                humanPlayOverlayGuardExercised = true;
                assertions.Check(humanPlayArmedThroughPlayerAction &&
                        humanPlayPropagatedWorldClickSuppressed &&
                        humanPlayArmedActionRetainedAfterOverlayClick,
                    "The exact overlay activation suppressed one propagated world click while retaining the armed rider action.");
                if (assertions.FailureCount != 0)
                {
                    BeginCleanup();
                }
                return;
            }

            ruleProbe = new MountedCombatRuleProbe();
            ruleProbe.Arm(rider, mount, AttackActor, target,
                IsCommandTerminationRow ? (int?)null : IsMissRow ? 1 : 20);
            assertions.Check(targetService != null && targetService.BeginExpectedAttackDispatch(target),
                "Target incoming-rule observation marked the exact expected pair-action dispatch boundary.");
            var attackArmed = IsHumanPlayRow
                ? humanPlayArmedThroughPlayerAction && combat.ArmedAction == AttackAction
                : combat.ArmedAction == AttackAction || combat.Arm(AttackAction);
            assertions.Check(attackArmed,
                AttackAction + " armed through the combat controller on the exact action actor turn.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            directClickedUnitView = target.View.gameObject.GetComponent<UnitEntityView>() == target.View;
            clickAccepted = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            admissionFeedback = combat.LastFeedback;
            admissionRejectionCodes = combat.LastRejectionCodes.Select(code => code.ToString()).ToArray();
            assertions.Check(clickAccepted &&
                    combat.ArmedAction == MountedCombatActionKind.None &&
                    combat.HasActiveCommand,
                "Native ClickUnitHandler/Harmony path consumed the exact enemy click. Feedback=" +
                combat.LastFeedback + "; armed=" + combat.ArmedAction +
                "; activeCommand=" + combat.HasActiveCommand + ".");
            if (!clickAccepted || combat.ArmedAction != MountedCombatActionKind.None || !combat.HasActiveCommand)
            {
                BeginCleanup();
                return;
            }
            step = CombatEngineStep.AwaitOutcome;
        }

        private void BeginGroundMovement()
        {
            if (groundMovementStarted)
            {
                step = CombatEngineStep.AwaitGroundMovement;
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var selected = SelectionManager.Instance.SelectedUnits;
            assertions.Check(selected != null && selected.Count == 1 && selected[0] == rider,
                "Exactly the rider owned selection before the ordinary turn-based ground click.");
            assertions.Check(!combat.HasActiveCommand && !combat.HasActiveGroundMovement,
                "Mounted command routing was idle before the ordinary turn-based ground click.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            riderPositionBeforeGroundMovement = rider.Position;
            mountPositionBeforeGroundMovement = mount.Position;
            targetPositionBeforeGroundMovement = target.Position;
            groundMovementDestination = FindWalkablePoint(mount.Position, 2.0f, 0.4f);
            groundMovementStarted = true;
            ClickGroundHandler.MoveSelectedUnitsToPoint(groundMovementDestination, false);
            assertions.Check(combat.HasActiveGroundMovement,
                "Ordinary ClickGroundHandler input admitted one exact Mammoth Move-slot command on the rider turn. Feedback=" +
                combat.LastFeedback + ".");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }
            step = CombatEngineStep.AwaitGroundMovement;
        }

        private void ObserveGroundMovement()
        {
            if (targetService == null || !targetService.RefreshBidirectionalCombatMemoryLease())
            {
                assertions.Fail("Exact bidirectional combat-memory lease was lost during rider-turn ground movement.");
                BeginCleanup();
                return;
            }

            groundMovementPairRetained = relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == mount;
            var selected = SelectionManager.Instance?.SelectedUnits;
            groundMovementSelectionRetained = selected != null && selected.Count == 1 && selected[0] == rider;
            groundMovementPoseHealthy = relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied;
            if (combat.HasActiveGroundMovement)
            {
                return;
            }
            if (string.IsNullOrEmpty(combat.LastGroundMoveResult))
            {
                return;
            }

            riderGroundMovementDisplacement = HorizontalDistance(riderPositionBeforeGroundMovement, rider.Position);
            mountGroundMovementDisplacement = HorizontalDistance(mountPositionBeforeGroundMovement, mount.Position);
            targetGroundMovementDisplacement = HorizontalDistance(targetPositionBeforeGroundMovement, target.Position);
            assertions.Check(string.Equals(combat.LastGroundMoveResult, "Success", StringComparison.Ordinal) &&
                    combat.LastGroundMoveDriveCount > 0 && combat.LastGroundMoveUsedRiderTurnAdapter,
                "Rider-turn ground movement completed successfully through the bounded Mammoth-command adapter.");
            assertions.Check(string.Equals(combat.LastGroundMoveExecutorId, mount.UniqueId, StringComparison.Ordinal) &&
                    combat.LastGroundMoveSlotRestored,
                "Ground movement retained exact Mammoth execution and restored its exact Move slot and queue.");
            assertions.Check(combat.LastGroundMoveRiderMoveAfter > combat.LastGroundMoveRiderMoveBefore &&
                    Math.Abs(combat.LastGroundMoveMountMoveAfter - combat.LastGroundMoveMountMoveBefore) <= 0.01f,
                "Turn-based ground movement charged only the rider Move ledger.");
            assertions.Check(riderGroundMovementDisplacement >= 0.75f &&
                    mountGroundMovementDisplacement >= 0.75f &&
                    targetGroundMovementDisplacement <= MountedCombatSpatialPolicy.RangeTolerance,
                "The exact mounted pair moved measurably while the diagnostic target remained stationary.");
            assertions.Check(groundMovementPairRetained && groundMovementSelectionRetained && groundMovementPoseHealthy,
                "Ground movement retained the exact pair, rider selection, and accepted Mammoth presentation.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            groundMovementCompleted = true;
            step = CombatEngineStep.AwaitCombatFrame;
        }

        private bool RetainDiagnosticTargetPlacementAtDispatch()
        {
            if (mount == null || mount.View == null || target == null || target.View == null ||
                !target.IsInState)
            {
                return false;
            }

            var observedDistance = HorizontalDistance(mount.Position, target.Position);
            var requiresRefresh = IsApproachRow
                ? MountedCombatSpatialPolicy.RequiresDiagnosticApproachPlacementRefresh(
                    pairApproachRadius,
                    observedDistance)
                : MountedCombatSpatialPolicy.RequiresDiagnosticTargetPlacementRefresh(
                    pairApproachRadius,
                    observedDistance);
            if (!requiresRefresh)
            {
                return true;
            }

            float requiredDistance;
            var placementCalculated = IsApproachRow
                ? MountedCombatSpatialPolicy.TryCalculateDiagnosticApproachTargetDistance(
                    pairApproachRadius,
                    out requiredDistance)
                : MountedCombatSpatialPolicy.TryCalculateDiagnosticTargetDistance(
                    pairApproachRadius,
                    out requiredDistance);
            if (!placementCalculated)
            {
                return false;
            }

            var refreshedPoint = FindWalkablePoint(
                mount.Position,
                requiredDistance,
                MountedCombatSpatialPolicy.DiagnosticPlacementTolerance);
            target.Translocate(refreshedPoint, null);
            target.View.ForcePlaceAboveGround();
            target.Commands.InterruptAll();
            target.HoldState = true;
            var retainedDistance = HorizontalDistance(mount.Position, target.Position);
            requestedTargetDistance = requiredDistance;
            return IsApproachRow
                ? MountedCombatSpatialPolicy.IsBoundedDiagnosticApproachTargetDistance(
                    pairApproachRadius,
                    retainedDistance)
                : MountedCombatSpatialPolicy.IsBoundedDiagnosticTargetDistance(
                    pairApproachRadius,
                    retainedDistance);
        }

        private void ObserveOutcome()
        {
            if (targetService == null || !targetService.RefreshBidirectionalCombatMemoryLease())
            {
                assertions.Fail("Exact bidirectional combat-memory lease was lost before native attack completion.");
                BeginCleanup();
                return;
            }
            if (IsTurnBasedRow && !nativeActionActorTurnActingObservedAfterDispatch)
            {
                var turnController = Game.Instance?.TurnBasedCombatController;
                var currentTurn = turnController?.CurrentTurn;
                if (currentTurn?.Unit != AttackActor)
                {
                    assertions.Fail("The exact native action-actor turn changed after mounted attack dispatch.");
                    BeginCleanup();
                    return;
                }
                if (!currentTurn.IsActing)
                {
                    if (combat.LastOutcome != null)
                    {
                        assertions.Fail("The mounted attack completed without an observed native Acting action-actor turn.");
                        BeginCleanup();
                    }
                    return;
                }

                nativeActionActorTurnActingObservedAfterDispatch = true;
                currentTurnUnitIdAtDispatch = currentTurn.Unit.UniqueId;
                currentTurnActingAtDispatch = true;
                roundNumberAtDispatch = turnController.RoundNumber;
            }
            if (IsApproachRow && combat.HasActiveCommand)
            {
                ObserveMovementToAttackRuntime();
            }
            if (IsCommandTerminationRow && !terminationDelivered)
            {
                TryDeliverCommandTermination();
            }
            if (combat.LastOutcome == null)
            {
                return;
            }

            outcome = combat.LastOutcome;
            riderStandardAfter = rider.CombatState.Cooldown.StandardAction;
            mountStandardAfter = mount.CombatState.Cooldown.StandardAction;
            riderMoveAfter = rider.CombatState.Cooldown.MoveAction;
            mountMoveAfter = mount.CombatState.Cooldown.MoveAction;
            riderDisplacementAtOutcome = HorizontalDistance(riderPositionAtClick, rider.Position);
            mountDisplacementAtOutcome = HorizontalDistance(mountPositionAtClick, mount.Position);
            targetDisplacementAtOutcome = HorizontalDistance(targetPositionAtClick, target.Position);
            assertions.Check(targetService.CaptureCurrentLife(target),
                "Diagnostic target life was captured at the exact completed attack outcome.");
            assertions.Check(targetService.TargetBrainSuppressedAtOutcome &&
                    !targetService.TargetBrainLeaseViolationObserved,
                "Diagnostic target native brain remained continuously suppressed through attack outcome.");

            if (IsCommandTerminationRow)
            {
                if (!CaptureCommandTerminationAfterState())
                {
                    return;
                }
                ValidateCommandTerminationOutcome();
                return;
            }

            assertions.Check(outcome.Action == AttackAction,
                "Terminal command retained exact mounted action identity.");
            assertions.Check(string.Equals(outcome.ActorId, AttackActor.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(outcome.TargetId, targetId, StringComparison.Ordinal),
                "Terminal command retained exact action actor and target identity.");
            assertions.Check(string.Equals(outcome.Result, "Success", StringComparison.Ordinal),
                "Native single attack completed successfully.");
            assertions.Check(outcome.ChildAttackStartCount == 1 && outcome.NativeAttackRuleObserved,
                "Exactly one native child attack started and exposed its native attack rule.");
            var expectedActionWeapon = IsMammothPrimaryRow
                ? NativeSingleAttackWeaponResolver.Resolve(mount)?.Weapon
                : rider.GetFirstWeapon();
            assertions.Check(expectedActionWeapon?.Blueprint != null &&
                    string.Equals(outcome.AttackWeaponBlueprintId, expectedActionWeapon.Blueprint.AssetGuid, StringComparison.Ordinal) &&
                    !outcome.AttackWeaponIsRanged &&
                    (IsMammothPrimaryRow
                        ? outcome.AttackWeaponIsNatural && string.Equals(outcome.AttackWeaponSlot, "PrimaryHand", StringComparison.Ordinal)
                        : string.Equals(outcome.AttackWeaponSlot, "EquippedMelee", StringComparison.Ordinal)),
                "Native rule execution retained the exact selected actor weapon and natural-attack identity.");
            assertions.Check(outcome.RepathCount == 0,
                IsMovementToAttackRow
                    ? "Stationary diagnostic target required one stable approach path and no repath."
                    : "Stationary in-range attack required no delegated movement or repath.");
            assertions.Check(outcome.PairRangeSatisfiedAtStart &&
                    Math.Abs(outcome.PairApproachRadiusAtStart - pairApproachRadius) <= 0.0001f &&
                    outcome.PairDistanceAtStart <= outcome.PairApproachRadiusAtStart + MountedCombatSpatialPolicy.RangeTolerance &&
                    outcome.NativeExecutorDistanceAtStart <= outcome.NativeAdmissionRadiusAtStart + 0.0001f &&
                    outcome.NativeAdmissionRadiusAtStart >= outcome.PairApproachRadiusAtStart &&
                    outcome.NativeAdmissionRadiusAtStart - outcome.PairApproachRadiusAtStart <=
                        MountedCombatSpatialPolicy.MaximumNativeExecutorRadiusAdjustment + 0.0001f,
                IsMammothPrimaryRow
                    ? "Mammoth-origin range exactly matched native Mammoth attack admission."
                    : "Mammoth-origin range exclusively gated the bounded native rider-executor admission bridge.");
            assertions.Check(outcome.ActionStandardCharged &&
                    string.Equals(outcome.CommandOwnerId, AttackActor.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(outcome.ResourceOwnerId, AttackActor.UniqueId, StringComparison.Ordinal),
                "The exact action actor owned and paid for the pair Standard wrapper.");
            if (IsMammothPrimaryRow)
            {
                assertions.Check(!outcome.RiderStandardCharged && mountStandardAfter > mountStandardBefore &&
                        Math.Abs(riderStandardAfter - riderStandardBefore) <= 0.01f,
                    "Mammoth primary charged only the Mammoth Standard action and left the rider Standard unchanged.");
            }
            else
            {
                assertions.Check(outcome.RiderStandardCharged && riderStandardAfter > riderStandardBefore &&
                        Math.Abs(mountStandardAfter - mountStandardBefore) <= 0.01f,
                    "Rider melee charged only the rider Standard action and left the Mammoth Standard unchanged.");
            }
            if (IsTurnBasedRow && (IsMovementToAttackRow || (IsHumanPlayRow && groundMovementCompleted)))
            {
                assertions.Check(riderMoveAfter > riderMoveBefore &&
                        Math.Abs(mountMoveAfter - mountMoveBefore) <= 0.01f,
                    IsHumanPlayRow
                        ? "Turn-based player-path ground movement remained charged only to the rider Move ledger through native Standard completion."
                        : "Turn-based Mammoth movement charged only the current rider Move ledger.");
            }
            else
            {
                assertions.Check(Math.Abs(mountMoveAfter - mountMoveBefore) <= 0.01f &&
                        Math.Abs(riderMoveAfter - riderMoveBefore) <= 0.01f,
                    IsMovementToAttackRow
                        ? "Real-time ignored-cooldown approach changed neither rider nor Mammoth Move ledger."
                        : "Stationary mounted action charged neither rider nor Mammoth Move action.");
            }
            assertions.Check(ruleProbe.AttackRuleCount == 1 && ruleProbe.AttackRollCount == 1 &&
                    (IsMissRow ? ruleProbe.DamageRuleCount == 0 : ruleProbe.DamageRuleCount <= 1) &&
                    ruleProbe.UnexpectedPairAttackCount == 0,
                IsMissRow
                    ? "Rulebook observed exactly one rider attack/roll, zero damage events, and no pair duplicate."
                    : "Rulebook observed exactly one expected pair-actor attack/roll, at most one damage event, and no pair duplicate.");
            assertions.Check(IsMissRow
                    ? ruleProbe.ForcedD20 == 1 && ruleProbe.ForcedD20Count >= 1 &&
                        ruleProbe.LastAttackHit == false &&
                        IsNativeAcMissReason(ruleProbe.LastAttackResult) &&
                        ruleProbe.TotalDamage == 0
                    : ruleProbe.ForcedD20 == 20 && ruleProbe.ForcedD20Count >= 1 &&
                        ruleProbe.LastAttackHit == true &&
                        (string.Equals(ruleProbe.LastAttackResult, "Hit", StringComparison.Ordinal) ||
                         string.Equals(ruleProbe.LastAttackResult, "CriticalHit", StringComparison.Ordinal)),
                IsMissRow
                    ? "Deterministic natural 1 produced native IsHit=false, an exact AC-selected miss reason, and zero damage."
                    : "Deterministic natural 20 produced native IsHit=true and a hit or critical-hit result.");
            assertions.Check(string.Equals(ruleProbe.LastInitiatorId, AttackActor.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(ruleProbe.LastTargetId, targetId, StringComparison.Ordinal),
                "Rulebook identities remained the exact action actor and diagnostic target.");
            if (IsTurnBasedRow)
            {
                var currentTurn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
                currentTurnUnitIdAtOutcome = currentTurn?.Unit?.UniqueId;
                currentTurnActingAtOutcome = currentTurn != null && currentTurn.IsActing;
                assertions.Check(IsMammothPrimaryRow
                        ? !currentTurnActingAtOutcome ||
                            !string.Equals(currentTurnUnitIdAtOutcome, mount.UniqueId, StringComparison.Ordinal)
                        : string.Equals(currentTurnUnitIdAtOutcome, rider.UniqueId, StringComparison.Ordinal) &&
                            currentTurnActingAtOutcome,
                    IsMammothPrimaryRow
                        ? "The exact native Mammoth turn ended after its bounded stationary action."
                        : "The exact native rider turn remained active through the stationary attack outcome.");
            }
            assertions.Check(relationship.State == RelationshipState.Mounted &&
                    relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied,
                "Mounted relationship and accepted pose remained healthy after the attack.");
            if (IsMovementToAttackRow)
            {
                var approach = new MountedCombatApproachSnapshot(
                    outcome.ApproachRequiredAtStart,
                    outcome.DelegatedMoveStartCount,
                    outcome.DelegatedMoveTickCount,
                    outcome.DelegatedMoveExecutorIsExactMount &&
                        string.Equals(outcome.DelegatedMoveExecutorId, mount.UniqueId, StringComparison.Ordinal),
                    outcome.WrapperCommandRetainedThroughoutApproach,
                    outcome.DelegatedMoveNeverQueuedOnMount,
                    outcome.DelegatedMoveOwnedByMountMoveSlot,
                    outcome.MountMoveSlotUnreplacedThroughoutApproach,
                    outcome.MountQueueEmptyThroughoutApproach,
                    outcome.DelegatedMoveFinishedSuccessfully,
                    outcome.MountMoveSlotRestoredAfterApproach,
                    outcome.DelegatedMoveDrivenByStockController,
                    outcome.DelegatedMoveDrivenByRiderTurnAdapter,
                    IsTurnBasedRow,
                    outcome.DelegatedMoveProgressObservationCount,
                    outcome.RiderStockAgentSuppressedThroughoutApproach,
                    outcome.MountStockAgentAuthoritativeThroughoutApproach,
                    outcome.PoseHealthyThroughoutApproach,
                    movementToAttackObservationCount,
                    selectionRetainedDuringApproach,
                    uiCoherentDuringApproach,
                    pairApproachRadius,
                    outcome.InitialPairDistance,
                    outcome.PairDistanceAtAttackStart,
                    outcome.RiderDisplacementAtAttackStart,
                    outcome.MountDisplacementAtAttackStart,
                    outcome.TargetDisplacementAtAttackStart,
                    outcome.RepathCount);
                assertions.Check(approach.AllPassed,
                    "Movement-to-attack retained exact command, mover, selection, UI, pose, and range invariants: " +
                    approach.FailureSummary + ".");
                assertions.Check(riderDisplacementAtOutcome >= MountedCombatSpatialPolicy.MinimumDiagnosticApproachDisplacement &&
                        mountDisplacementAtOutcome >= MountedCombatSpatialPolicy.MinimumDiagnosticApproachDisplacement &&
                        targetDisplacementAtOutcome <= MountedCombatSpatialPolicy.RangeTolerance,
                    "The synchronized pair moved to attack while the exact target remained stationary.");
            }
            else
            {
                assertions.Check(riderDisplacementAtOutcome <= 0.05f &&
                        mountDisplacementAtOutcome <= 0.05f &&
                        targetDisplacementAtOutcome <= 0.05f,
                    "Mounted pair and target attack remained stationary at the authoritative Mammoth origin.");
            }

            poseProfileAtOutcome = relationship.Runtime.PoseProfileId;
            poseHealthyAtOutcome = relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied;

            BeginCleanup();
        }

        private void ObserveMovementToAttackRuntime()
        {
            movementToAttackObservationCount++;
            var selected = SelectionManager.Instance?.SelectedUnits;
            selectionRetainedDuringApproach &= selected != null && selected.Count == 1 && selected[0] == rider;
            uiCoherentDuringApproach &= combat.CanShowCombatActions &&
                relationship.State == RelationshipState.Mounted &&
                relationship.Runtime.PoseConfigured;
        }

        private void TryDeliverCommandTermination()
        {
            var riderCommands = rider?.Commands;
            var mountCommands = mount?.Commands;
            var wrapper = riderCommands?.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard)
                as MountedPairAttackCommand;
            var move = mountCommands?.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move)
                as Kingmaker.UnitLogic.Commands.UnitMoveTo;
            if (wrapper == null || move == null)
            {
                return;
            }

            var riderDisplacement = HorizontalDistance(riderPositionAtClick, rider.Position);
            var mountDisplacement = HorizontalDistance(mountPositionAtClick, mount.Position);
            var currentPairDistance = HorizontalDistance(mount.Position, target.Position);
            if (riderDisplacement < 0.75f || mountDisplacement < 0.75f ||
                currentPairDistance <= pairApproachRadius + MountedCombatSpatialPolicy.RangeTolerance)
            {
                return;
            }

            terminationWrapper = wrapper;
            terminationMove = move;
            wrapperPresentBeforeTermination = riderCommands.Contains(wrapper) && riderCommands.Standard == wrapper;
            delegatedMovePresentBeforeTermination = mountCommands.Contains(move) &&
                mountCommands.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move) == move;
            riderQueueEmptyBeforeTermination = riderCommands.Queue.Count == 0;
            mountQueueEmptyBeforeTermination = mountCommands.Queue.Count == 0;
            childAbsentBeforeTermination = wrapper.ChildAttack != null && !wrapper.ChildAttack.IsStarted;
            pairDistanceAtTermination = currentPairDistance;
            riderDisplacementAtTermination = riderDisplacement;
            mountDisplacementAtTermination = mountDisplacement;
            targetDisplacementAtTermination = HorizontalDistance(targetPositionAtClick, target.Position);

            if (IsCommandCancellationRow)
            {
                var selection = SelectionManager.Instance;
                if (selection == null)
                {
                    throw new InvalidOperationException("SelectionManager was unavailable for the exact Stop cancellation boundary.");
                }
                selection.Stop();
                terminationDelivered = true;
                selection.Stop();
                terminationRepeated = true;
            }
            else if (IsCommandInterruptionRow)
            {
                riderCommands.InterruptAll();
                terminationDelivered = true;
                riderCommands.InterruptAll();
                terminationRepeated = true;
            }
            else
            {
                var before = lifecycle.SnapshotNativeDeliveries();
                var lastSequence = before.Count == 0 ? 0L : before.Max(item => item.Sequence);
                lifecycle.HandlePartyCombatStateChanged(false);
                terminationDelivered = true;
                lifecycle.HandlePartyCombatStateChanged(false);
                terminationRepeated = true;
                var deliveries = lifecycle.SnapshotNativeDeliveries()
                    .Where(item => item.Sequence > lastSequence)
                    .ToArray();
                terminationLifecycleDeliveryCount = deliveries.Length;
                terminationLifecycleDeliveriesExact = deliveries.Length == 2 && deliveries.All(item =>
                    item.Boundary == NativeLifecycleBoundary.CombatEnded &&
                    string.Equals(item.Source, "IPartyCombatHandler.HandlePartyCombatStateChanged(false)", StringComparison.Ordinal) &&
                    item.StateBefore == RelationshipState.Mounted &&
                    item.StateAfter == RelationshipState.Mounted &&
                    !item.CleanupTrigger.HasValue &&
                    !item.CleanupAttempted &&
                    item.CleanupSucceeded);
            }
        }

        private bool CaptureCommandTerminationAfterState()
        {
            var riderCommands = rider?.Commands;
            var mountCommands = mount?.Commands;
            if (riderCommands == null || mountCommands == null || terminationWrapper == null || terminationMove == null)
            {
                return false;
            }

            wrapperAbsentAfterTermination =
                riderCommands.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard) == null &&
                !riderCommands.Contains(terminationWrapper) && !riderCommands.Queue.Contains(terminationWrapper);
            delegatedMoveAbsentAfterTermination =
                mountCommands.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move) == null &&
                !mountCommands.Contains(terminationMove) && !mountCommands.Queue.Contains(terminationMove);
            riderQueueEmptyAfterTermination = riderCommands.Queue.Count == 0;
            mountQueueEmptyAfterTermination = mountCommands.Queue.Count == 0;
            var agent = mount.View?.AgentASP;
            mountAgentStoppedAfterTermination = agent != null && !agent.WantsToMove && !agent.IsReallyMoving &&
                agent.Speed == 0f && agent.Velocity.sqrMagnitude == 0f;
            relationshipPreservedAfterTermination = relationship.State == RelationshipState.Mounted;
            var selected = SelectionManager.Instance?.SelectedUnits;
            selectionRetainedAfterTermination = selected != null && selected.Count == 1 && selected[0] == rider;
            uiCoherentAfterTermination = combat.CanShowCombatActions && relationship.Runtime.PoseConfigured &&
                relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied;
            return wrapperAbsentAfterTermination && delegatedMoveAbsentAfterTermination &&
                riderQueueEmptyAfterTermination && mountQueueEmptyAfterTermination && mountAgentStoppedAfterTermination;
        }

        private void ValidateCommandTerminationOutcome()
        {
            assertions.Check(terminationDelivered && terminationRepeated,
                "The exact command termination boundary was delivered and repeated idempotently.");
            assertions.Check(wrapperPresentBeforeTermination && delegatedMovePresentBeforeTermination &&
                    riderQueueEmptyBeforeTermination && mountQueueEmptyBeforeTermination &&
                    childAbsentBeforeTermination,
                "The exact rider wrapper and Mammoth Move slot existed without a started child or queued command before termination.");
            assertions.Check(pairDistanceAtTermination > pairApproachRadius + MountedCombatSpatialPolicy.RangeTolerance &&
                    riderDisplacementAtTermination >= 0.75f && mountDisplacementAtTermination >= 0.75f &&
                    targetDisplacementAtTermination <= MountedCombatSpatialPolicy.RangeTolerance,
                "Termination occurred after measurable Mammoth-owned movement, outside attack range, with a stationary target.");
            assertions.Check(wrapperAbsentAfterTermination && delegatedMoveAbsentAfterTermination &&
                    riderQueueEmptyAfterTermination && mountQueueEmptyAfterTermination &&
                    mountAgentStoppedAfterTermination && !combat.HasActiveCommand,
                "Termination removed both owned command representations, emptied both queues, and stopped the Mammoth agent.");
            assertions.Check(relationshipPreservedAfterTermination && selectionRetainedAfterTermination &&
                    uiCoherentAfterTermination,
                "Termination preserved the mounted relationship, accepted pose, rider selection, and combat UI.");
            if (IsCombatEndTerminationRow)
            {
                assertions.Check(terminationLifecycleDeliveryCount == 2 && terminationLifecycleDeliveriesExact,
                    "Repeated combat-end handler delivery produced exactly two mounted-state non-cleanup ledger observations.");
            }
            assertions.Check(outcome.Action == MountedCombatActionKind.RiderMelee &&
                    string.Equals(outcome.ActorId, rider.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(outcome.CommandOwnerId, rider.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(outcome.ResourceOwnerId, rider.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(outcome.TargetId, targetId, StringComparison.Ordinal),
                "Interrupted terminal evidence retained exact rider command/resource ownership and target identity.");
            assertions.Check(string.Equals(outcome.Result, "Interrupt", StringComparison.Ordinal) &&
                    string.Equals(outcome.TerminalReason, "Interrupt", StringComparison.Ordinal) &&
                    outcome.ChildAttackStartCount == 0 && !outcome.NativeAttackRuleObserved && outcome.RepathCount == 0,
                "The command terminated as Interrupt before any child attack or repath.");
            var riderWeapon = rider.GetFirstWeapon();
            assertions.Check(riderWeapon?.Blueprint != null &&
                    string.Equals(outcome.AttackWeaponBlueprintId, riderWeapon.Blueprint.AssetGuid, StringComparison.Ordinal) &&
                    !outcome.AttackWeaponIsRanged &&
                    string.Equals(outcome.AttackWeaponSlot, "EquippedMelee", StringComparison.Ordinal),
                "Interrupted command retained the exact planned rider melee weapon identity.");
            assertions.Check(!outcome.ActionStandardCharged && !outcome.RiderStandardCharged &&
                    Math.Abs(riderStandardAfter - riderStandardBefore) <= 0.01f &&
                    Math.Abs(mountStandardAfter - mountStandardBefore) <= 0.01f,
                "Pre-child termination remained Standard-cost-free for both rider and Mammoth.");
            assertions.Check(Math.Abs(mountMoveAfter - mountMoveBefore) <= 0.01f &&
                    (IsTurnBasedRow
                        ? riderMoveAfter > riderMoveBefore
                        : Math.Abs(riderMoveAfter - riderMoveBefore) <= 0.01f),
                IsTurnBasedRow
                    ? "Turn-based terminated movement charged only actual rider-owned Move expenditure."
                    : "Real-time terminated movement changed neither Move ledger.");
            assertions.Check(ruleProbe.ForcedD20Count == 0 && ruleProbe.AttackRuleCount == 0 &&
                    ruleProbe.AttackRollCount == 0 && ruleProbe.DamageRuleCount == 0 &&
                    ruleProbe.UnexpectedPairAttackCount == 0 && ruleProbe.TotalDamage == 0,
                "No attack, roll, damage, opportunity, or duplicate rule chain occurred before termination.");
            if (IsTurnBasedRow)
            {
                var currentTurn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
                currentTurnUnitIdAtOutcome = currentTurn?.Unit?.UniqueId;
                currentTurnActingAtOutcome = currentTurn != null && currentTurn.IsActing;
                assertions.Check(string.Equals(currentTurnUnitIdAtOutcome, rider.UniqueId, StringComparison.Ordinal) &&
                        currentTurnActingAtOutcome,
                    "The exact rider turn remained active after bounded command termination.");
            }
            assertions.Check(targetService.LifeTransitionCount == 0 &&
                    targetService.LastObservedLife?.LifeState == "Conscious",
                "The diagnostic target remained conscious with zero life transition.");

            poseProfileAtOutcome = relationship.Runtime.PoseProfileId;
            poseHealthyAtOutcome = relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied;
            BeginCleanup();
        }

        private void BeginCleanup()
        {
            if (step == CombatEngineStep.AwaitTurnBasedRealtimeRestore ||
                step == CombatEngineStep.AwaitCleanupFrame || completed)
            {
                return;
            }

            try
            {
                if (IsMountedBeforeModeTransitionRow && turnBasedModeProbe != null &&
                    turnBasedModeProbe.TemporaryDeliveryAttempted && !turnBasedModeProbe.RestoreDeliveryCompleted)
                {
                    RestoreTurnBasedTransitionLease();
                    modeRestoreStartedAtSeconds = rowClock.Elapsed.TotalSeconds;
                    step = CombatEngineStep.AwaitTurnBasedRealtimeRestore;
                    return;
                }
                BeginRelationshipCleanup();
            }
            catch (Exception exception)
            {
                assertions.Fail("Combat cleanup threw " + exception.GetType().Name + ": " + exception.Message);
                logger.Exception("Combat runtime cleanup", exception);
                BeginRelationshipCleanup();
            }
        }

        private void AwaitTurnBasedRealtimeRestore()
        {
            var game = Game.Instance;
            if (!CombatController.IsInTurnBasedCombat() && game != null &&
                game.CurrentMode == GameModeType.Pause && game.IsPaused && !originalPause)
            {
                nativeRealtimePauseObserved = true;
                game.IsPaused = false;
                realtimeUnpauseRequested = true;
            }

            if (CombatController.IsInTurnBasedCombat() || game == null ||
                game.CurrentMode != GameModeType.Default)
            {
                if (rowClock.Elapsed.TotalSeconds - modeRestoreStartedAtSeconds < CleanupTimeoutSeconds)
                {
                    return;
                }
                assertions.Fail("Native TB-to-RT transition did not reach exact Default real-time mode within its " +
                    CleanupTimeoutSeconds + " second cleanup deadline. State: " +
                    DescribeTurnBasedRestoreState() + ".");
                BeginRelationshipCleanup();
                return;
            }

            turnBasedModeRestored = turnBasedRestoreDeliveryCompleted &&
                turnBasedPersistedSettingUnchanged;
            if (relationship.NativeTurnBasedExitUiLeaseRestoreAttemptCount == 0)
            {
                if (rowClock.Elapsed.TotalSeconds - modeRestoreStartedAtSeconds < CleanupTimeoutSeconds)
                {
                    return;
                }
                assertions.Fail("Exact post-native-exit rider UI lease restoration did not complete within its " +
                    CleanupTimeoutSeconds + " second cleanup deadline. State: " +
                    DescribeTurnBasedRestoreState() + ".");
                BeginRelationshipCleanup();
                return;
            }
            assertions.Check(nativeRealtimePauseObserved && realtimeUnpauseRequested,
                "Native TB disable entered its exact combat Pause boundary before a stock unpause resumed Default real time.");
            assertions.Check(
                relationship.NativeTurnBasedExitAiLeaseReassertionArmedCount == 1 &&
                relationship.NativeTurnBasedExitAiLeaseReassertionAttemptCount == 1 &&
                relationship.NativeTurnBasedExitAiLeaseReassertionMutationCount == 1 &&
                relationship.NativeTurnBasedExitAiLeaseReassertionSuccessCount == 1 &&
                string.Equals(relationship.NativeTurnBasedExitAiLeaseReassertionResult, "reasserted", StringComparison.Ordinal),
                "Exact native TB shutdown AI reset was isolated by one successful reassertion of the already-owned Mammoth AI-disable lease.");
            assertions.Check(
                relationship.NativeTurnBasedExitUiLeaseRestoreArmedCount == 1 &&
                relationship.NativeTurnBasedExitUiLeaseRestoreAttemptCount == 1 &&
                relationship.NativeTurnBasedExitUiLeaseRestoreMutationCount == 1 &&
                relationship.NativeTurnBasedExitUiLeaseRestoreSuccessCount == 1 &&
                string.Equals(relationship.NativeTurnBasedExitUiLeaseRestoreResult, "reselected-rider", StringComparison.Ordinal),
                "Exact native TB shutdown UI reset was isolated by one rider-selection/UI-principal restoration.");
            pairRetainedAfterRealtimeRestore = turnBasedModeRestored &&
                relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == mount &&
                relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied;
            assertions.Check(pairRetainedAfterRealtimeRestore,
                "The exact mounted pair and accepted Mammoth presentation survived TB-to-RT restoration.");
            presentationAfterRealtimeRestore = relationship.CapturePresentationObservation();
            assertions.Check(IsRiderUiOwnershipCoherent(presentationAfterRealtimeRestore, false),
                "The rider view, selection, portrait, action bar, and camera remained coherent after TB-to-RT restoration.");
            BeginRelationshipCleanup();
        }

        private string DescribeTurnBasedRestoreState()
        {
            var game = Game.Instance;
            var controller = game?.TurnBasedCombatController;
            return "settingCurrent=" + (turnBasedModeProbe == null
                    ? "<probe-null>"
                    : turnBasedModeProbe.CurrentValue.ToString()) +
                ";rawCache=" + (turnBasedModeProbe?.CurrentRawCacheValue.HasValue == true
                    ? turnBasedModeProbe.CurrentRawCacheValue.Value.ToString()
                    : "<null>") +
                ";combatPredicate=" + CombatController.IsInTurnBasedCombat() +
                ";controllerInitialized=" + (controller?.Initialized.ToString() ?? "<controller-null>") +
                ";playerInCombat=" + (game?.Player?.IsInCombat.ToString() ?? "<player-null>") +
                ";currentMode=" + (game?.CurrentMode.ToString() ?? "<game-null>") +
                ";nativeRealtimePauseObserved=" + nativeRealtimePauseObserved +
                ";realtimeUnpauseRequested=" + realtimeUnpauseRequested +
                ";restoreDeliveryCompleted=" + turnBasedRestoreDeliveryCompleted +
                ";persistedValueUnchanged=" + turnBasedPersistedSettingUnchanged +
                ";uiLeaseRestoreResult=" + relationship.NativeTurnBasedExitUiLeaseRestoreResult;
        }

        private void BeginRelationshipCleanup()
        {
            try
            {
                combat.Cancel("runtime combat row cleanup");
                var cleanup = relationship.Dismount(CleanupTrigger.Manual);
                relationshipClean = cleanup.Succeeded && !cleanup.MovementAuthorityResidual &&
                    !cleanup.PresentationResidual && relationship.State == RelationshipState.Unmounted;
                assertions.Check(relationshipClean,
                    "Relationship cleanup restored Unmounted state without movement or presentation residue.");

                TryLeaveCombat(target);
                TryLeaveCombat(mount);
                TryLeaveCombat(rider);
                if (targetService != null)
                {
                    targetRemoved = targetService.DestroyAndVerify();
                    CaptureTargetCleanupState();
                }
                else
                {
                    targetRemoved = true;
                    targetEntityRemoved = true;
                    targetRuntimeGroupRemoved = true;
                    targetRuntimeFactionRemoved = true;
                    targetDurabilityLeaseReleased = true;
                    targetBrainLeaseReleased = true;
                    targetSleeplessLeaseReleased = true;
                    targetNonPairPartyAiLeaseRestored = true;
                }
            }
            catch (Exception exception)
            {
                assertions.Fail("Combat cleanup threw " + exception.GetType().Name + ": " + exception.Message);
                logger.Exception("Combat runtime cleanup", exception);
            }
            cleanupStartedAtSeconds = rowClock.Elapsed.TotalSeconds;
            cleanupFrame = frameNumber;
            step = CombatEngineStep.AwaitCleanupFrame;
        }

        private void VerifyCleanupAndComplete()
        {
            if (frameNumber <= cleanupFrame)
            {
                return;
            }

            if (targetService != null && !targetRemoved)
            {
                targetRemoved = targetService.DestroyAndVerify();
                CaptureTargetCleanupState();
            }

            combatCleared = (rider == null || !rider.IsInCombat) &&
                (mount == null || !mount.IsInCombat) &&
                (target == null || !target.IsInState || !target.IsInCombat) &&
                !(Game.Instance?.Player?.IsInCombat ?? false);
            if ((!combatCleared || !targetRemoved) &&
                rowClock.Elapsed.TotalSeconds - cleanupStartedAtSeconds < CleanupTimeoutSeconds)
            {
                return;
            }

            RestoreTurnBasedMode();
            if (IsTurnBasedRow)
            {
                assertions.Check(turnBasedModeRestored && turnBasedPersistedSettingUnchanged,
                    "Turn-based mode, raw cache, and persisted setting were restored exactly after cleanup.");
            }
            RestorePause();
            RestoreCameraFollower();

            assertions.Check(targetRemoved && targetEntityRemoved &&
                    targetRuntimeGroupRemoved && targetRuntimeFactionRemoved && targetDurabilityLeaseReleased &&
                    targetBrainLeaseReleased && targetSleeplessLeaseReleased &&
                    targetNonPairPartyAiLeaseRestored,
                "Runtime-only combat target, durability, brain, sleepless, and non-pair party AI leases, project group, and runtime faction were removed with zero residue.");
            assertions.Check(combatCleared,
                "Pair, target, and party left combat before final evidence.");
            assertions.Check(pauseRestored,
                "The exact pre-row pause state was restored after the combat lease.");
            if (IsMountedBeforeModeTransitionRow)
            {
                assertions.Check(cameraFollowerRestored,
                    "The exact pre-row native camera-follower state was restored after transition qualification.");
            }
            assertions.Check(relationship.State == RelationshipState.Unmounted &&
                    relationship.Rider == null && relationship.Mount == null &&
                    relationship.Runtime.MovementAgent == null &&
                    !relationship.Runtime.HasPresentationAttachmentResidue,
                "Final combat row retained no relationship, movement, or presentation residue.");
            if (rider?.View?.AgentASP != null && mount?.View?.AgentASP != null)
            {
                assertions.Check(rider.View.AgentASP.enabled == riderAgentInitiallyEnabled &&
                        mount.View.AgentASP.enabled == mountAgentInitiallyEnabled &&
                        rider.View.AgentASP.AvoidanceDisabled == riderAvoidanceInitiallyDisabled &&
                        mount.View.AgentASP.AvoidanceDisabled == mountAvoidanceInitiallyDisabled,
                    "Pair restored the exact pre-row stock agent and avoidance state.");
            }

            WriteRowEvidence();
            var status = assertions.FailureCount == 0 ? "PASS" : "FAIL";
            results.Add(new RuntimeSubscenarioResult
            {
                Name = currentRow,
                Status = status,
                AssertionPassCount = assertions.PassCount,
                AssertionFailCount = assertions.FailureCount,
                Errors = assertions.Errors
            });
            if (assertions.FailureCount != 0)
            {
                errors.AddRange(assertions.Errors);
            }
            RestoreSettings();
            completed = true;
            rowClock.Stop();
            logger.Info("Combat runtime engine completed " + currentRow + " with " + status + ".");
        }

        private void WriteRowEvidence()
        {
            if (rowEvidenceWritten)
            {
                return;
            }

            var selected = SelectionManager.Instance?.SelectedUnits;
            var record = new CombatEvidenceRecord
            {
                SchemaVersion = IsHumanPlayRow
                    ? (IsTurnBasedRow ? 47 : 44)
                    : IsCommandTerminationRow
                    ? IsCombatEndTerminationRow
                        ? (IsTurnBasedRow ? 41 : 40)
                        : (IsTurnBasedRow ? 39 : 38)
                    : IsMovementToAttackRow
                        ? (IsTurnBasedRow ? 35 : 34)
                        : IsReachQualificationRow
                            ? (IsTurnBasedRow ? 43 : 42)
                            : (IsTurnBasedRow ? 27 : 26),
                ArtifactKind = "combat-scenario-evidence",
                RunId = request.RunId,
                Scenario = request.Scenario,
                Row = currentRow,
                RowIndex = 0,
                Sequence = 0,
                Frame = frameNumber,
                UtcTimestamp = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Branch = request.Branch,
                Commit = request.Commit,
                ProductVersion = request.ProductVersion,
                DllSha256 = dllSha256,
                DllMvid = dllMvid,
                Status = assertions.FailureCount == 0 ? "PASS" : "FAIL",
                Mode = IsTurnBasedRow ? "turn-based" : "real-time",
                Action = AttackAction.ToString(),
                ExpectedActor = ExpectedActorRole,
                RiderId = rider?.UniqueId,
                MountId = mount?.UniqueId,
                TargetId = targetId,
                TargetProvisioning = targetProvisioning ?? new CombatTargetProvisioningEvidence(),
                TargetLife = CombatTargetLifeEvidence.From(targetService),
                TargetIncomingRules = CombatTargetIncomingRulesEvidence.From(targetService),
                NonPairPartyAiLease = CombatNonPairPartyAiLeaseEvidence.From(targetService),
                TargetBrainLease = CombatTargetBrainLeaseEvidence.From(targetService),
                Reach = reachEvidence,
                ClickAccepted = clickAccepted,
                Admission = IsHumanPlayRow
                    ? new CombatAdmissionEvidence
                    {
                        ArmedThroughPlayerFacingCombatController = humanPlayArmedThroughPlayerAction,
                        OverlayActivationWorldClickSuppressed = humanPlayPropagatedWorldClickSuppressed,
                        ArmedActionRetainedAfterOverlayClick = humanPlayArmedActionRetainedAfterOverlayClick,
                        DirectClickedUnitView = directClickedUnitView,
                        Feedback = admissionFeedback,
                        RejectionCodes = admissionRejectionCodes
                    }
                    : null,
                PairApproachRadius = pairApproachRadius,
                TargetDistanceAtClick = targetDistanceAtClick,
                RiderPositionAtClick = PositionEvidence.From(riderPositionAtClick),
                MountPositionAtClick = PositionEvidence.From(mountPositionAtClick),
                TargetPositionAtClick = PositionEvidence.From(targetPositionAtClick),
                CombatEntry = CombatEntryEvidence.From(
                    entryReadiness,
                    actionActorReadiness,
                    nativeJoinReadiness,
                    combatMemoryRemoved),
                Dispatch = CombatDispatchEvidence.From(
                    originalPause,
                    unpausedForRealTime,
                    pausedAtClick,
                    dispatchReadiness,
                    pauseRestored),
                TurnBased = IsTurnBasedRow
                    ? TurnBasedCombatEvidence.Capture(
                        turnBasedOriginalEnabled,
                        turnBasedTemporaryEnabled,
                        turnBasedOriginalRawCacheHadValue,
                        turnBasedRestoreDeliveryCompleted,
                        turnBasedModeEnabledAtMount,
                        pairMountedBeforeTurnBasedEnable,
                        pairRetainedAfterTurnBasedEnable,
                        pairRetainedAfterRealtimeRestore,
                        presentationAfterTurnBasedEnable,
                        presentationAfterRealtimeRestore,
                        turnBasedControllerInitialized,
                        turnRosterContainsRider,
                        turnRosterContainsMount,
                        turnRosterContainsTarget,
                        ExpectedActorRole,
                        nativeActionActorTurnStarted,
                        currentTurnUnitIdAtDispatch,
                        currentTurnActingAtDispatch,
                        roundNumberAtDispatch,
                        currentTurnUnitIdAtOutcome,
                        currentTurnActingAtOutcome,
                        IsMammothPrimaryRow && (!currentTurnActingAtOutcome ||
                            !string.Equals(currentTurnUnitIdAtOutcome, mount?.UniqueId, StringComparison.Ordinal)),
                        turnBasedModeRestored,
                        turnBasedPersistedSettingUnchanged,
                        relationship.NativeTurnBasedExitAiLeaseReassertionArmedCount,
                        relationship.NativeTurnBasedExitAiLeaseReassertionAttemptCount,
                        relationship.NativeTurnBasedExitAiLeaseReassertionMutationCount,
                        relationship.NativeTurnBasedExitAiLeaseReassertionSuccessCount,
                        relationship.NativeTurnBasedExitAiLeaseReassertionResult,
                        relationship.NativeTurnBasedExitUiLeaseRestoreArmedCount,
                        relationship.NativeTurnBasedExitUiLeaseRestoreAttemptCount,
                        relationship.NativeTurnBasedExitUiLeaseRestoreMutationCount,
                        relationship.NativeTurnBasedExitUiLeaseRestoreSuccessCount,
                        relationship.NativeTurnBasedExitUiLeaseRestoreResult)
                    : null,
                GroundMovement = IsMountedBeforeModeTransitionRow
                    ? new CombatGroundMovementEvidence
                    {
                        Requested = groundMovementStarted,
                        Destination = PositionEvidence.From(groundMovementDestination),
                        Result = combat.LastGroundMoveResult,
                        DriveCount = combat.LastGroundMoveDriveCount,
                        ExecutorId = combat.LastGroundMoveExecutorId,
                        ExecutorIsExactMount = mount != null && string.Equals(
                            combat.LastGroundMoveExecutorId,
                            mount.UniqueId,
                            StringComparison.Ordinal),
                        UsedRiderTurnAdapter = combat.LastGroundMoveUsedRiderTurnAdapter,
                        SlotRestored = combat.LastGroundMoveSlotRestored,
                        RiderMoveBefore = combat.LastGroundMoveRiderMoveBefore,
                        RiderMoveAfter = combat.LastGroundMoveRiderMoveAfter,
                        MountMoveBefore = combat.LastGroundMoveMountMoveBefore,
                        MountMoveAfter = combat.LastGroundMoveMountMoveAfter,
                        RiderDisplacement = riderGroundMovementDisplacement,
                        MountDisplacement = mountGroundMovementDisplacement,
                        TargetDisplacement = targetGroundMovementDisplacement,
                        PairRetained = groundMovementPairRetained,
                        SelectionRetained = groundMovementSelectionRetained,
                        PoseHealthy = groundMovementPoseHealthy
                    }
                    : null,
                Resources = new CombatResourceEvidence
                {
                    RiderStandardBefore = riderStandardBefore,
                    RiderStandardAfter = riderStandardAfter,
                    RiderMoveBefore = riderMoveBefore,
                    RiderMoveAfter = riderMoveAfter,
                    MountStandardBefore = mountStandardBefore,
                    MountStandardAfter = mountStandardAfter,
                    MountMoveBefore = mountMoveBefore,
                    MountMoveAfter = mountMoveAfter
                },
                Command = CombatCommandEvidence.From(outcome),
                Rules = CombatRuleEvidence.From(ruleProbe),
                Movement = new CombatMovementEvidence
                {
                    AuthoritativeMover = "mount",
                    RepathCount = outcome?.RepathCount ?? 0,
                    RiderDisplacementAtOutcome = riderDisplacementAtOutcome,
                    MountDisplacementAtOutcome = mountDisplacementAtOutcome,
                    TargetDisplacementAtOutcome = targetDisplacementAtOutcome,
                    RiderStockAgentEnabledAtEnd = rider?.View?.AgentASP == null ? (bool?)null : rider.View.AgentASP.enabled,
                    MountStockAgentEnabledAtEnd = mount?.View?.AgentASP == null ? (bool?)null : mount.View.AgentASP.enabled,
                    RiderAvoidanceDisabledAtEnd = rider?.View?.AgentASP == null ? (bool?)null : rider.View.AgentASP.AvoidanceDisabled,
                    MountAvoidanceDisabledAtEnd = mount?.View?.AgentASP == null ? (bool?)null : mount.View.AgentASP.AvoidanceDisabled
                },
                MovementToAttack = IsApproachRow
                    ? CombatMovementToAttackEvidence.From(
                        outcome,
                        requestedTargetDistance,
                        movementToAttackObservationCount,
                        selectionRetainedDuringApproach,
                        uiCoherentDuringApproach)
                    : null,
                CommandTermination = IsCommandTerminationRow
                    ? new CombatCommandTerminationEvidence
                    {
                        Kind = IsCommandCancellationRow
                            ? "player-stop"
                            : IsCommandInterruptionRow
                                ? "native-wrapper-interrupt"
                                : "party-combat-end",
                        Trigger = IsCommandCancellationRow
                            ? "SelectionManagerBase.Stop"
                            : IsCommandInterruptionRow
                                ? "UnitCommands.InterruptAll"
                                : "IPartyCombatHandler.HandlePartyCombatStateChanged(false)",
                        Delivered = terminationDelivered,
                        RepeatedIdempotently = terminationRepeated,
                        WrapperPresentBefore = wrapperPresentBeforeTermination,
                        DelegatedMovePresentBefore = delegatedMovePresentBeforeTermination,
                        RiderQueueEmptyBefore = riderQueueEmptyBeforeTermination,
                        MountQueueEmptyBefore = mountQueueEmptyBeforeTermination,
                        ChildAttackNotStartedBefore = childAbsentBeforeTermination,
                        PairDistanceAtTrigger = pairDistanceAtTermination,
                        RiderDisplacementAtTrigger = riderDisplacementAtTermination,
                        MountDisplacementAtTrigger = mountDisplacementAtTermination,
                        TargetDisplacementAtTrigger = targetDisplacementAtTermination,
                        WrapperAbsentAfter = wrapperAbsentAfterTermination,
                        DelegatedMoveAbsentAfter = delegatedMoveAbsentAfterTermination,
                        RiderQueueEmptyAfter = riderQueueEmptyAfterTermination,
                        MountQueueEmptyAfter = mountQueueEmptyAfterTermination,
                        MountAgentStoppedAfter = mountAgentStoppedAfterTermination,
                        ActiveCommandClearedAfter = !combat.HasActiveCommand,
                        RelationshipPreservedAfter = relationshipPreservedAfterTermination,
                        SelectionRetainedAfter = selectionRetainedAfterTermination,
                        UiCoherentAfter = uiCoherentAfterTermination,
                        LifecycleDeliveryCount = IsCombatEndTerminationRow
                            ? (int?)terminationLifecycleDeliveryCount
                            : null,
                        LifecycleDeliveriesExact = IsCombatEndTerminationRow
                            ? (bool?)terminationLifecycleDeliveriesExact
                            : null
                    }
                    : null,
                Pose = new CombatPoseEvidence
                {
                    ProfileId = poseProfileAtOutcome,
                    HealthyAtOutcome = poseHealthyAtOutcome,
                    ConfiguredAtEnd = relationship.Runtime.PoseConfigured,
                    AttachmentLeaseAtEnd = relationship.Runtime.PresentationAttachmentLeaseActive,
                    ResidueAtEnd = relationship.Runtime.HasPresentationAttachmentResidue
                },
                Cleanup = new CombatCleanupEvidence
                {
                    TargetRemoved = targetRemoved,
                    TargetEntityRemoved = targetEntityRemoved,
                    RuntimeGroupRemoved = targetRuntimeGroupRemoved,
                    RuntimeFactionRemoved = targetRuntimeFactionRemoved,
                    DurabilityLeaseReleased = targetDurabilityLeaseReleased,
                    BrainLeaseReleased = targetBrainLeaseReleased,
                    SleeplessLeaseReleased = targetSleeplessLeaseReleased,
                    NonPairPartyAiLeaseRestored = targetNonPairPartyAiLeaseRestored,
                    RelationshipClean = relationshipClean,
                    CombatCleared = combatCleared,
                    RelationshipState = relationship.State.ToString(),
                    ResidualState = relationship.Rider != null || relationship.Mount != null ||
                        relationship.Runtime.MovementAgent != null || relationship.Runtime.HasPresentationAttachmentResidue,
                    PresentationResidual = relationship.Runtime.HasPresentationAttachmentResidue
                },
                Selection = selected == null
                    ? new string[0]
                    : System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(
                        System.Linq.Enumerable.Where(selected, unit => unit != null),
                        unit => unit.UniqueId)),
                AssertionPassCount = assertions.PassCount,
                AssertionFailCount = assertions.FailureCount,
                Errors = assertions.Errors
            };

            var json = JsonConvert.SerializeObject(record, EvidenceJsonSettings);
            using (var stream = new FileStream(evidencePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            {
                writer.WriteLine(json);
                writer.Flush();
                stream.Flush(true);
            }
            rowEvidenceWritten = true;
        }

        private Vector3 FindWalkablePoint(
            Vector3 origin,
            float requestedDistance,
            float distanceTolerance)
        {
            if (global::AstarPath.active == null)
            {
                throw new InvalidOperationException("Active native navigation graph is unavailable.");
            }

            var baseDirection = mount?.View == null ? Vector3.forward : mount.View.transform.forward;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude < 0.01f)
            {
                baseDirection = Vector3.forward;
            }
            baseDirection.Normalize();

            for (var index = 0; index < 16; index++)
            {
                var direction = Quaternion.Euler(0f, index * 22.5f, 0f) * baseDirection;
                var candidate = origin + (direction * requestedDistance);
                var nearest = global::AstarPath.active.GetNearest(candidate);
                if (nearest.node == null || !nearest.node.Walkable)
                {
                    continue;
                }
                var point = nearest.clampedPosition;
                var originDistance = HorizontalDistance(origin, point);
                if (originDistance > MountedCombatSpatialPolicy.RangeTolerance &&
                    Math.Abs(originDistance - requestedDistance) <= distanceTolerance)
                {
                    return point;
                }
            }

            throw new InvalidOperationException("No bounded walkable diagnostic target point satisfied the exact distance contract.");
        }

        private void BestEffortCleanup()
        {
            try { combat.Cancel("combat engine disposal"); }
            catch (Exception exception) { errors.Add("Combat cancellation during disposal failed: " + exception.Message); }
            try
            {
                if (relationship.State != RelationshipState.Unmounted && relationship.State != RelationshipState.Disposed)
                {
                    var cleanup = relationship.Dismount(CleanupTrigger.ProcessTeardown);
                    if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                    {
                        errors.Add("Combat engine disposal retained mounted residue.");
                    }
                }
            }
            catch (Exception exception) { errors.Add("Combat relationship disposal cleanup failed: " + exception.Message); }
            TryLeaveCombat(target);
            TryLeaveCombat(mount);
            TryLeaveCombat(rider);
            try
            {
                if (targetService != null && !targetService.DestroyAndVerify())
                {
                    errors.Add("Combat engine disposal retained diagnostic target residue.");
                }
            }
            catch (Exception exception) { errors.Add("Combat target disposal cleanup failed: " + exception.Message); }
            try { RestoreTurnBasedMode(); }
            catch (Exception exception) { errors.Add("Turn-based mode restoration during disposal failed: " + exception.Message); }
            RestorePause();
            try { RestoreCameraFollower(); }
            catch (Exception exception) { errors.Add("Camera-follower restoration during disposal failed: " + exception.Message); }
        }

        private void CaptureTargetCleanupState()
        {
            targetEntityRemoved = targetService != null && targetService.TargetEntityRemoved;
            targetRuntimeGroupRemoved = targetService != null && targetService.RuntimeGroupRemoved;
            targetRuntimeFactionRemoved = targetService != null && targetService.RuntimeFactionRemoved;
            targetDurabilityLeaseReleased = targetService != null && targetService.TargetDurabilityLeaseReleased;
            targetBrainLeaseReleased = targetService != null && targetService.TargetBrainLeaseReleased;
            targetSleeplessLeaseReleased = targetService != null && targetService.TargetSleeplessLeaseReleased;
            targetNonPairPartyAiLeaseRestored = targetService != null && targetService.NonPairPartyAiLeaseRestored;
            combatMemoryRemoved = targetService != null && targetService.CombatMemoryRemoved;
        }

        private DiagnosticTurnBasedDispatchReadinessSnapshot CaptureTurnBasedReadiness(
            TurnBased.Controllers.CombatController controller)
        {
            var currentTurn = controller?.CurrentTurn;
            return new DiagnosticTurnBasedDispatchReadinessSnapshot(
                CombatController.IsInTurnBasedCombat(),
                controller != null && controller.Initialized,
                turnRosterContainsRider,
                turnRosterContainsMount,
                turnRosterContainsTarget,
                nativeActionActorTurnStarted,
                currentTurn?.Unit == AttackActor,
                MountedPairTurnPolicy.CanIssueAction(
                    true,
                    currentTurn?.Unit == AttackActor,
                    currentTurn != null &&
                        currentTurn.Status == TurnBased.Controllers.TurnController.TurnStatus.Preparing,
                    currentTurn != null && currentTurn.IsActing));
        }

        private static bool MemoryEnemiesContain(UnitEntityData observer, UnitEntityData expectedEnemy)
        {
            var enemies = observer?.Group?.Memory?.Enemies;
            if (enemies == null || expectedEnemy == null)
            {
                return false;
            }
            for (var index = 0; index < enemies.Count; index++)
            {
                if (enemies[index]?.Unit == expectedEnemy)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ContainsTurnRosterUnit(
            TurnBased.Controllers.CombatController controller,
            UnitEntityData expected)
        {
            if (controller == null || expected == null)
            {
                return false;
            }
            foreach (var unit in controller.SortedUnits)
            {
                if (unit == expected)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsRiderUiOwnershipCoherent(string observation, bool expectedCameraOn)
        {
            if (rider == null || string.IsNullOrWhiteSpace(observation))
            {
                return false;
            }

            var riderId = rider.UniqueId;
            return observation.IndexOf("uiOwnershipObservationError=", StringComparison.Ordinal) < 0 &&
                observation.IndexOf("riderViewActiveInHierarchy=True", StringComparison.Ordinal) >= 0 &&
                observation.IndexOf("riderSelected=True", StringComparison.Ordinal) >= 0 &&
                observation.IndexOf("actionBarOwner=" + riderId, StringComparison.Ordinal) >= 0 &&
                observation.IndexOf("actionBarActive=True", StringComparison.Ordinal) >= 0 &&
                observation.IndexOf("portraitActiveOwnerCount=1", StringComparison.Ordinal) >= 0 &&
                observation.IndexOf("portraitActive=True", StringComparison.Ordinal) >= 0 &&
                observation.IndexOf("portraitSelected=True", StringComparison.Ordinal) >= 0 &&
                observation.IndexOf("cameraOn=" + expectedCameraOn, StringComparison.Ordinal) >= 0 &&
                observation.IndexOf("cameraOwner=" + riderId, StringComparison.Ordinal) >= 0;
        }

        private static void TryLeaveCombat(UnitEntityData unit)
        {
            if (unit != null && unit.IsInState && unit.IsInCombat)
            {
                unit.LeaveCombat();
            }
        }

        private void RestoreSettings()
        {
            if (settingLeaseOwned)
            {
                settings.EnableUnsafeMovementExperiment = originalUnsafeExperimentSetting;
                settingLeaseOwned = false;
            }
        }

        private void RestoreTurnBasedMode()
        {
            RestoreTurnBasedTransitionLease();
            var turnBasedPersistedUnchanged = turnBasedPersistedSettingUnchanged;
            if (!IsMountedBeforeModeTransitionRow && IsTurnBasedRow)
            {
                turnBasedModeRestored = !CombatController.IsInTurnBasedCombat() &&
                    turnBasedRestoreDeliveryCompleted && turnBasedPersistedUnchanged;
            }

            if (turnBasedModeProbe != null)
            {
                turnBasedModeProbe.Dispose();
                turnBasedModeProbe = null;
            }

            var realTimePersistedUnchanged = true;
            if (realTimeBaselineModeProbe != null)
            {
                if (realTimeBaselineModeProbe.TemporaryDeliveryAttempted &&
                    !realTimeBaselineModeProbe.RestoreDeliveryCompleted)
                {
                    realTimeBaselineModeProbe.DispatchRestoreAndRestoreRawCache();
                }
                realTimeBaselineModeProbe.Dispose();
                realTimePersistedUnchanged = !realTimeBaselineModeProbe.TemporaryDeliveryAttempted ||
                    realTimeBaselineModeProbe.PersistedValueUnchanged;
                realTimeBaselineModeProbe = null;
            }

            turnBasedPersistedSettingUnchanged = turnBasedPersistedUnchanged &&
                realTimePersistedUnchanged;
            turnBasedModeRestored = turnBasedModeRestored && turnBasedPersistedSettingUnchanged;
        }

        private void CaptureTurnBasedModeLeaseIdentity()
        {
            turnBasedOriginalEnabled = turnBasedModeProbe != null && turnBasedModeProbe.OriginalValue;
            turnBasedTemporaryEnabled = turnBasedModeProbe != null && turnBasedModeProbe.TemporaryValue;
            turnBasedOriginalRawCacheHadValue = turnBasedModeProbe != null &&
                turnBasedModeProbe.OriginalRawCacheHadValue;
        }

        private void RestoreTurnBasedTransitionLease()
        {
            if (turnBasedModeProbe == null)
            {
                return;
            }
            if (turnBasedModeProbe.TemporaryDeliveryAttempted &&
                !turnBasedModeProbe.RestoreDeliveryCompleted)
            {
                turnBasedModeProbe.DispatchRestoreAndRestoreRawCache();
            }
            turnBasedRestoreDeliveryCompleted = !turnBasedModeProbe.TemporaryDeliveryAttempted ||
                turnBasedModeProbe.RestoreDeliveryCompleted;
            turnBasedPersistedSettingUnchanged = !turnBasedModeProbe.TemporaryDeliveryAttempted ||
                turnBasedModeProbe.PersistedValueUnchanged;
        }

        private void RestoreCameraFollower()
        {
            if (!cameraFollowerLeaseOwned)
            {
                return;
            }
            cameraFollowerSnapshot.Restore();
            cameraFollowerRestored = cameraFollowerSnapshot.IsCurrent;
            if (cameraFollowerRestored)
            {
                cameraFollowerLeaseOwned = false;
            }
        }

        private void RestorePause()
        {
            if (!pauseLeaseOwned)
            {
                return;
            }
            if (Game.Instance == null)
            {
                pauseRestored = false;
                return;
            }
            if (Game.Instance.IsPaused != originalPause)
            {
                Game.Instance.IsPaused = originalPause;
            }
            pauseRestored = Game.Instance.IsPaused == originalPause;
            if (pauseRestored)
            {
                pauseLeaseOwned = false;
            }
        }

        private string DescribeDeadlineReadiness()
        {
            if (step == CombatEngineStep.AwaitTurnBasedMode)
            {
                return "Turn-based setting current=" +
                    (turnBasedModeProbe != null && turnBasedModeProbe.TemporaryValueIsCurrent) +
                    ";combat predicate=" + CombatController.IsInTurnBasedCombat() +
                    ";controllerAvailable=" + (Game.Instance?.TurnBasedCombatController != null) +
                    ";temporaryDelivery=" + (turnBasedModeProbe != null && turnBasedModeProbe.TemporaryDeliveryAttempted);
            }
            if (step == CombatEngineStep.AwaitCombatFrame)
            {
                return "Combat entry readiness=" + (entryReadiness?.FailureSummary ?? "not-observed") +
                    ";actionActorReadiness=" + (actionActorReadiness?.FailureSummary ?? "not-observed") +
                    ";actionActorInitiative=" + (actionActorReadiness == null
                        ? "not-observed"
                        : actionActorReadiness.ActorInitiative.ToString("R", CultureInfo.InvariantCulture)) +
                    ";nativeJoinReadiness=" + (nativeJoinReadiness?.FailureSummary ?? "not-observed") +
                    ";riderInitiative=" + (entryReadiness == null
                        ? "not-observed"
                        : entryReadiness.RiderInitiative.ToString("R", CultureInfo.InvariantCulture)) +
                    ";gameDeltaTime=" + (entryReadiness == null
                        ? "not-observed"
                        : entryReadiness.GameDeltaTime.ToString("R", CultureInfo.InvariantCulture)) +
                    ";targetAwake=" + (target != null && target.IsAwake) +
                    ";targetInFog=" + (target != null && target.IsInFogOfWar) +
                    ";targetFactionPeaceful=" + (target?.Faction != null && target.Faction.Peaceful) +
                    ";targetLife=" + (target?.Descriptor?.State == null
                        ? "unavailable"
                        : target.Descriptor.State.LifeState.ToString()) +
                    ";targetDamage=" + (target == null ? "unavailable" : target.Damage.ToString(CultureInfo.InvariantCulture)) +
                    ";targetHitPoints=" + (target?.Stats == null
                        ? "unavailable"
                        : ((int)target.Stats.HitPoints).ToString(CultureInfo.InvariantCulture)) +
                    ";incomingAttackRules=" + (targetService?.IncomingAttackRuleCount ?? 0) +
                    ";preDispatchIncomingAttackRules=" + (targetService?.PreDispatchIncomingAttackRuleCount ?? 0) +
                    ";firstIncomingAttackInitiator=" +
                        (targetService?.FirstIncomingAttack?.InitiatorId ?? "none") +
                    ";firstIncomingAttackGroup=" +
                        (targetService?.FirstIncomingAttack?.InitiatorGroupId ?? "none") +
                    ";firstIncomingAttackSharesRiderGroup=" +
                        (targetService?.FirstIncomingAttack?.InitiatorSharesRiderGroup ?? false) +
                    ";firstIncomingAttackDirectlyControllable=" +
                        (targetService?.FirstIncomingAttack?.InitiatorDirectlyControllable ?? false) +
                    ";firstIncomingAttackEffectiveAiEnabled=" +
                        (targetService?.FirstIncomingAttack?.InitiatorEffectiveAiEnabled ?? false) +
                    ";firstIncomingAttackRawAiEnabled=" +
                        (targetService?.FirstIncomingAttack?.InitiatorRawAiEnabled ?? false) +
                    ";incomingDamageRules=" + (targetService?.IncomingDamageRuleCount ?? 0) +
                    ";preDispatchIncomingDamageRules=" + (targetService?.PreDispatchIncomingDamageRuleCount ?? 0) +
                    ";firstIncomingDamageInitiator=" +
                        (targetService?.FirstIncomingDamage?.InitiatorId ?? "none") +
                    ";firstIncomingDamage=" +
                        (targetService?.FirstIncomingDamage == null
                            ? "none"
                            : targetService.FirstIncomingDamage.Damage.ToString(CultureInfo.InvariantCulture)) +
                    ";turnBasedReadiness=" + (turnBasedReadiness?.FailureSummary ??
                        (IsTurnBasedRow ? "not-observed" : "not-requested")) +
                    ";turnStatus=" + (Game.Instance?.TurnBasedCombatController?.CurrentTurn == null
                        ? "none"
                        : Game.Instance.TurnBasedCombatController.CurrentTurn.Status.ToString()) +
                    ";dispatchReadiness=" + (dispatchReadiness?.FailureSummary ?? "not-observed") +
                    ";initiativeTickObservation=" + initiativeTickObservation.Describe() +
                    ";gamePaused=" + (Game.Instance != null && Game.Instance.IsPaused);
            }
            return "Command readiness: " + combat.DescribeActiveCommandReadiness();
        }

        private static string FormatTransitionErrors(TransitionResult transition)
        {
            return transition == null || transition.Errors == null
                ? "no transition detail"
                : string.Join("; ", transition.Errors);
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            var dx = first.x - second.x;
            var dz = first.z - second.z;
            return Mathf.Sqrt((dx * dx) + (dz * dz));
        }

        private static string ComputeSha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(RuntimeCombatScenarioEngine));
            }
        }

        private enum CombatEngineStep
        {
            BeginRow,
            AwaitTurnBasedMode,
            AwaitMountedFrame,
            AwaitCombatFrame,
            AwaitGroundMovement,
            AwaitOutcome,
            AwaitTurnBasedRealtimeRestore,
            AwaitCleanupFrame
        }

        private sealed class AssertionRecorder
        {
            private readonly List<string> errors = new List<string>();

            public int PassCount { get; private set; }

            public int FailureCount { get; private set; }

            public IReadOnlyList<string> Errors => errors;

            public void Check(bool condition, string message)
            {
                if (condition)
                {
                    PassCount++;
                }
                else
                {
                    Fail(message);
                }
            }

            public void Fail(string message)
            {
                FailureCount++;
                errors.Add(message);
            }
        }

        private sealed class CombatEvidenceRecord
        {
            public int SchemaVersion { get; set; }
            public string ArtifactKind { get; set; }
            public string RunId { get; set; }
            public string Scenario { get; set; }
            public string Row { get; set; }
            public int RowIndex { get; set; }
            public long Sequence { get; set; }
            public int Frame { get; set; }
            public string UtcTimestamp { get; set; }
            public string Branch { get; set; }
            public string Commit { get; set; }
            public string ProductVersion { get; set; }
            public string DllSha256 { get; set; }
            public string DllMvid { get; set; }
            public string Status { get; set; }
            public string Mode { get; set; }
            public string Action { get; set; }
            public string ExpectedActor { get; set; }
            public string RiderId { get; set; }
            public string MountId { get; set; }
            public string TargetId { get; set; }
            public CombatTargetProvisioningEvidence TargetProvisioning { get; set; }
            public CombatTargetLifeEvidence TargetLife { get; set; }
            public CombatTargetIncomingRulesEvidence TargetIncomingRules { get; set; }
            public CombatNonPairPartyAiLeaseEvidence NonPairPartyAiLease { get; set; }
            public CombatTargetBrainLeaseEvidence TargetBrainLease { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public CombatReachEvidence Reach { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public CombatAdmissionEvidence Admission { get; set; }
            public bool ClickAccepted { get; set; }
            public float PairApproachRadius { get; set; }
            public float TargetDistanceAtClick { get; set; }
            public PositionEvidence RiderPositionAtClick { get; set; }
            public PositionEvidence MountPositionAtClick { get; set; }
            public PositionEvidence TargetPositionAtClick { get; set; }
            public CombatEntryEvidence CombatEntry { get; set; }
            public CombatDispatchEvidence Dispatch { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public TurnBasedCombatEvidence TurnBased { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public CombatGroundMovementEvidence GroundMovement { get; set; }
            public CombatResourceEvidence Resources { get; set; }
            public CombatCommandEvidence Command { get; set; }
            public CombatRuleEvidence Rules { get; set; }
            public CombatMovementEvidence Movement { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public CombatMovementToAttackEvidence MovementToAttack { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public CombatCommandTerminationEvidence CommandTermination { get; set; }
            public CombatPoseEvidence Pose { get; set; }
            public CombatCleanupEvidence Cleanup { get; set; }
            public IReadOnlyList<string> Selection { get; set; }
            public int AssertionPassCount { get; set; }
            public int AssertionFailCount { get; set; }
            public IReadOnlyList<string> Errors { get; set; }
        }

        private sealed class CombatAdmissionEvidence
        {
            public bool ArmedThroughPlayerFacingCombatController { get; set; }
            public bool OverlayActivationWorldClickSuppressed { get; set; }
            public bool ArmedActionRetainedAfterOverlayClick { get; set; }
            public bool DirectClickedUnitView { get; set; }
            public string Feedback { get; set; }
            public string[] RejectionCodes { get; set; }
        }

        private sealed class CombatGroundMovementEvidence
        {
            public bool Requested { get; set; }
            public PositionEvidence Destination { get; set; }
            public string Result { get; set; }
            public int DriveCount { get; set; }
            public string ExecutorId { get; set; }
            public bool ExecutorIsExactMount { get; set; }
            public bool UsedRiderTurnAdapter { get; set; }
            public bool SlotRestored { get; set; }
            public float RiderMoveBefore { get; set; }
            public float RiderMoveAfter { get; set; }
            public float MountMoveBefore { get; set; }
            public float MountMoveAfter { get; set; }
            public float RiderDisplacement { get; set; }
            public float MountDisplacement { get; set; }
            public float TargetDisplacement { get; set; }
            public bool PairRetained { get; set; }
            public bool SelectionRetained { get; set; }
            public bool PoseHealthy { get; set; }
        }

        private void CaptureInitialReachEvidence()
        {
            var riderWeapon = rider?.GetFirstWeapon();
            var mountPrimary = NativeSingleAttackWeaponResolver.Resolve(mount);
            var riderWeaponBlueprint = riderWeapon?.Blueprint;
            var mountWeaponBlueprint = mountPrimary?.Weapon?.Blueprint;
            if (rider?.View == null || mount?.View == null || target?.View == null ||
                riderWeaponBlueprint == null || riderWeaponBlueprint.IsRanged ||
                mountPrimary?.Kind != NativeSingleAttackSlotKind.PrimaryHand ||
                mountWeaponBlueprint == null || !mountWeaponBlueprint.IsNatural || mountWeaponBlueprint.IsRanged)
            {
                assertions.Fail("Exact rider and Mammoth melee reach inputs were unavailable.");
                return;
            }

            var riderProbe = new MountedPairSingleAttack(target, rider, mount, true);
            riderProbe.Init(rider);
            var mountProbe = new MountedPairSingleAttack(target, rider, mount, false);
            mountProbe.Init(mount);
            var riderWeaponRange = riderProbe.PairApproachRadius - mount.View.Corpulence - target.View.Corpulence;
            var mountWeaponRange = mountProbe.PairApproachRadius - mount.View.Corpulence - target.View.Corpulence;
            var riderStoppingRadius = MountedCombatSpatialPolicy.CalculateStoppingRadius(
                mount.View.Corpulence,
                target.View.Corpulence,
                riderWeaponRange);
            var mountStoppingRadius = MountedCombatSpatialPolicy.CalculateStoppingRadius(
                mount.View.Corpulence,
                target.View.Corpulence,
                mountWeaponRange);
            var initialDistance = HorizontalDistance(mount.Position, target.Position);

            reachEvidence = new CombatReachEvidence
            {
                RiderWeaponBlueprintId = riderWeaponBlueprint.AssetGuid,
                MountWeaponBlueprintId = mountWeaponBlueprint.AssetGuid,
                RiderWeaponRange = riderWeaponRange,
                MountWeaponRange = mountWeaponRange,
                MountCorpulence = mount.View.Corpulence,
                TargetCorpulence = target.View.Corpulence,
                RiderStoppingRadius = riderStoppingRadius,
                MountStoppingRadius = mountStoppingRadius,
                InitialDistance = initialDistance,
                RiderOutsideAtInitial = !riderProbe.IsPairEnoughClose,
                MountOutsideAtInitial = !mountProbe.IsPairEnoughClose,
                RiderProbeRadiusAtInitial = riderProbe.PairApproachRadius,
                MountProbeRadiusAtInitial = mountProbe.PairApproachRadius
            };

            assertions.Check(Math.Abs(riderProbe.PairApproachRadius - riderStoppingRadius) <= 0.0001f &&
                    Math.Abs(mountProbe.PairApproachRadius - mountStoppingRadius) <= 0.0001f,
                "Rider and Mammoth probes retained their independent exact stopping-radius contracts.");
            assertions.Check(reachEvidence.RiderOutsideAtInitial && reachEvidence.MountOutsideAtInitial &&
                    initialDistance > riderStoppingRadius + MountedCombatSpatialPolicy.RangeTolerance &&
                    initialDistance > mountStoppingRadius + MountedCombatSpatialPolicy.RangeTolerance,
                "The initial target position was independently outside both mounted melee radii.");
        }

        private void CaptureDispatchReachEvidence()
        {
            if (reachEvidence == null || rider?.View == null || mount?.View == null || target?.View == null)
            {
                assertions.Fail("Mounted reach evidence was unavailable at dispatch.");
                return;
            }

            var riderWeapon = rider.GetFirstWeapon();
            var mountPrimary = NativeSingleAttackWeaponResolver.Resolve(mount);
            var riderProbe = new MountedPairSingleAttack(target, rider, mount, true);
            riderProbe.Init(rider);
            var mountProbe = new MountedPairSingleAttack(target, rider, mount, false);
            mountProbe.Init(mount);
            var riderWeaponRange = riderProbe.PairApproachRadius - mount.View.Corpulence - target.View.Corpulence;
            var mountWeaponRange = mountProbe.PairApproachRadius - mount.View.Corpulence - target.View.Corpulence;
            reachEvidence.DispatchDistance = HorizontalDistance(mount.Position, target.Position);
            reachEvidence.RiderWithinAtDispatch = riderProbe.IsPairEnoughClose;
            reachEvidence.MountWithinAtDispatch = mountProbe.IsPairEnoughClose;
            reachEvidence.RiderCanAttackTarget = rider.CanAttack(target);
            reachEvidence.MountCanAttackTarget = mount.CanAttack(target);
            reachEvidence.TargetCanAttackRider = target.CanAttack(rider);
            reachEvidence.TargetCanAttackMount = target.CanAttack(mount);
            reachEvidence.InputsUnchangedAtDispatch =
                riderWeapon?.Blueprint != null && mountPrimary?.Weapon?.Blueprint != null &&
                string.Equals(riderWeapon.Blueprint.AssetGuid, reachEvidence.RiderWeaponBlueprintId, StringComparison.Ordinal) &&
                string.Equals(mountPrimary.Weapon.Blueprint.AssetGuid, reachEvidence.MountWeaponBlueprintId, StringComparison.Ordinal) &&
                Math.Abs(riderWeaponRange - reachEvidence.RiderWeaponRange) <= 0.0001f &&
                Math.Abs(mountWeaponRange - reachEvidence.MountWeaponRange) <= 0.0001f &&
                Math.Abs(mount.View.Corpulence - reachEvidence.MountCorpulence) <= 0.0001f &&
                Math.Abs(target.View.Corpulence - reachEvidence.TargetCorpulence) <= 0.0001f;
            reachEvidence.ActionRadiusMatches = Math.Abs(
                pairApproachRadius - (IsMammothPrimaryRow
                    ? reachEvidence.MountStoppingRadius
                    : reachEvidence.RiderStoppingRadius)) <= 0.0001f;

            assertions.Check(reachEvidence.InputsUnchangedAtDispatch && reachEvidence.ActionRadiusMatches,
                "Mounted reach retained exact corpulence, weapon, and actor-specific radius inputs without mutation.");
            assertions.Check(reachEvidence.RiderCanAttackTarget && reachEvidence.MountCanAttackTarget &&
                    reachEvidence.TargetCanAttackRider && reachEvidence.TargetCanAttackMount,
                "Rider and Mammoth remained independently targetable in both hostile directions.");
            assertions.Check(IsMammothPrimaryRow
                    ? reachEvidence.MountWithinAtDispatch
                    : reachEvidence.RiderWithinAtDispatch,
                "The exact action actor reached its independent mounted melee boundary before dispatch.");
            assertions.Check(reachEvidence.RiderWithinAtDispatch == MountedCombatSpatialPolicy.IsWithinRange(
                    new MountedCombatPoint(mount.Position.x, mount.Position.z),
                    new MountedCombatPoint(target.Position.x, target.Position.z),
                    reachEvidence.RiderStoppingRadius) &&
                    reachEvidence.MountWithinAtDispatch == MountedCombatSpatialPolicy.IsWithinRange(
                    new MountedCombatPoint(mount.Position.x, mount.Position.z),
                    new MountedCombatPoint(target.Position.x, target.Position.z),
                    reachEvidence.MountStoppingRadius),
                "Native rider/Mammoth admission agreed with the independent Mammoth-origin spatial policy.");
        }

        private sealed class CombatReachEvidence
        {
            public string RiderWeaponBlueprintId { get; set; }
            public string MountWeaponBlueprintId { get; set; }
            public float RiderWeaponRange { get; set; }
            public float MountWeaponRange { get; set; }
            public float MountCorpulence { get; set; }
            public float TargetCorpulence { get; set; }
            public float RiderStoppingRadius { get; set; }
            public float MountStoppingRadius { get; set; }
            public float InitialDistance { get; set; }
            public float RiderProbeRadiusAtInitial { get; set; }
            public float MountProbeRadiusAtInitial { get; set; }
            public bool RiderOutsideAtInitial { get; set; }
            public bool MountOutsideAtInitial { get; set; }
            public float DispatchDistance { get; set; }
            public bool RiderWithinAtDispatch { get; set; }
            public bool MountWithinAtDispatch { get; set; }
            public bool RiderCanAttackTarget { get; set; }
            public bool MountCanAttackTarget { get; set; }
            public bool TargetCanAttackRider { get; set; }
            public bool TargetCanAttackMount { get; set; }
            public bool InputsUnchangedAtDispatch { get; set; }
            public bool ActionRadiusMatches { get; set; }
        }

        private sealed class CombatEntryEvidence
        {
            public bool MemoryQueued { get; set; }
            public bool PlayerGroupMemoryContainsTarget { get; set; }
            public bool TargetGroupMemoryContainsRider { get; set; }
            public bool RiderInCombat { get; set; }
            public bool MountInCombat { get; set; }
            public bool TargetInCombat { get; set; }
            public bool PlayerInCombat { get; set; }
            public bool RiderPrepared { get; set; }
            public bool RiderAwake { get; set; }
            public bool TargetAwake { get; set; }
            public bool DefaultGameMode { get; set; }
            public float RiderInitiative { get; set; }
            public string ActionActorId { get; set; }
            public bool ActionActorPrepared { get; set; }
            public bool ActionActorCanActInCombat { get; set; }
            public float ActionActorInitiative { get; set; }
            public float GameDeltaTime { get; set; }
            public bool MemoryRemovedAtCleanup { get; set; }
            public NativeCombatJoinEvidence NativeJoin { get; set; }

            public static CombatEntryEvidence From(
                DiagnosticCombatEntryReadinessSnapshot readiness,
                DiagnosticCombatActionActorReadinessSnapshot actionActorReadiness,
                DiagnosticNativeCombatJoinReadinessSnapshot nativeJoin,
                bool memoryRemovedAtCleanup)
            {
                return new CombatEntryEvidence
                {
                    MemoryQueued = readiness?.MemoryQueued ?? false,
                    PlayerGroupMemoryContainsTarget = readiness?.PlayerGroupMemoryContainsTarget ?? false,
                    TargetGroupMemoryContainsRider = readiness?.TargetGroupMemoryContainsRider ?? false,
                    RiderInCombat = readiness?.RiderInCombat ?? false,
                    MountInCombat = readiness?.MountInCombat ?? false,
                    TargetInCombat = readiness?.TargetInCombat ?? false,
                    PlayerInCombat = readiness?.PlayerInCombat ?? false,
                    RiderPrepared = readiness?.RiderPrepared ?? false,
                    RiderAwake = readiness?.RiderAwake ?? false,
                    TargetAwake = readiness?.TargetAwake ?? false,
                    DefaultGameMode = readiness?.DefaultGameMode ?? false,
                    RiderInitiative = readiness?.RiderInitiative ?? float.MaxValue,
                    ActionActorId = actionActorReadiness?.ActorId,
                    ActionActorPrepared = actionActorReadiness?.ActorPrepared ?? false,
                    ActionActorCanActInCombat = actionActorReadiness?.ActorCanActInCombat ?? false,
                    ActionActorInitiative = actionActorReadiness?.ActorInitiative ?? float.MaxValue,
                    GameDeltaTime = readiness?.GameDeltaTime ?? 0f,
                    MemoryRemovedAtCleanup = memoryRemovedAtCleanup,
                    NativeJoin = NativeCombatJoinEvidence.From(nativeJoin)
                };
            }
        }

        private sealed class NativeCombatJoinEvidence
        {
            public bool RiderInGame { get; set; }
            public bool MountInGame { get; set; }
            public bool TargetInGame { get; set; }
            public bool RiderConscious { get; set; }
            public bool MountConscious { get; set; }
            public bool TargetConscious { get; set; }
            public bool RiderIgnoredByCombat { get; set; }
            public bool MountIgnoredByCombat { get; set; }
            public bool TargetIgnoredByCombat { get; set; }
            public bool PlayerGroupContainsRider { get; set; }
            public bool PlayerGroupContainsMount { get; set; }
            public bool TargetGroupContainsTarget { get; set; }
            public bool PlayerGroupEnemiesContainsTarget { get; set; }
            public bool TargetGroupEnemiesContainsRider { get; set; }
            public bool RiderNotInFogOfWar { get; set; }
            public bool TargetNotInFogOfWar { get; set; }
            public bool RiderNotInStealthAmbush { get; set; }
            public bool TargetNotInStealthAmbush { get; set; }

            public static NativeCombatJoinEvidence From(DiagnosticNativeCombatJoinReadinessSnapshot value)
            {
                return new NativeCombatJoinEvidence
                {
                    RiderInGame = value?.RiderInGame ?? false,
                    MountInGame = value?.MountInGame ?? false,
                    TargetInGame = value?.TargetInGame ?? false,
                    RiderConscious = value?.RiderConscious ?? false,
                    MountConscious = value?.MountConscious ?? false,
                    TargetConscious = value?.TargetConscious ?? false,
                    RiderIgnoredByCombat = value != null && value.RiderIgnoredByCombat,
                    MountIgnoredByCombat = value != null && value.MountIgnoredByCombat,
                    TargetIgnoredByCombat = value != null && value.TargetIgnoredByCombat,
                    PlayerGroupContainsRider = value?.PlayerGroupContainsRider ?? false,
                    PlayerGroupContainsMount = value?.PlayerGroupContainsMount ?? false,
                    TargetGroupContainsTarget = value?.TargetGroupContainsTarget ?? false,
                    PlayerGroupEnemiesContainsTarget = value?.PlayerGroupEnemiesContainsTarget ?? false,
                    TargetGroupEnemiesContainsRider = value?.TargetGroupEnemiesContainsRider ?? false,
                    RiderNotInFogOfWar = value?.RiderNotInFogOfWar ?? false,
                    TargetNotInFogOfWar = value?.TargetNotInFogOfWar ?? false,
                    RiderNotInStealthAmbush = value?.RiderNotInStealthAmbush ?? false,
                    TargetNotInStealthAmbush = value?.TargetNotInStealthAmbush ?? false
                };
            }
        }

        private sealed class CombatDispatchEvidence
        {
            public bool OriginalPaused { get; set; }
            public bool UnpausedForRealTime { get; set; }
            public bool PausedAtClick { get; set; }
            public bool ActionActorCanActInCombat { get; set; }
            public bool ActionActorHandsBusy { get; set; }
            public bool EquipmentControllerAvailable { get; set; }
            public bool EquipmentUpdateScheduled { get; set; }
            public bool PauseRestored { get; set; }

            public static CombatDispatchEvidence From(
                bool originalPaused,
                bool unpaused,
                bool pausedAtClick,
                DiagnosticCombatDispatchReadinessSnapshot readiness,
                bool pauseRestored)
            {
                return new CombatDispatchEvidence
                {
                    OriginalPaused = originalPaused,
                    UnpausedForRealTime = unpaused,
                    PausedAtClick = pausedAtClick,
                    ActionActorCanActInCombat = readiness?.ActionActorCanActInCombat ?? false,
                    ActionActorHandsBusy = readiness?.ActionActorHandsBusy ?? false,
                    EquipmentControllerAvailable = readiness?.EquipmentControllerAvailable ?? false,
                    EquipmentUpdateScheduled = readiness?.EquipmentUpdateScheduled ?? false,
                    PauseRestored = pauseRestored
                };
            }
        }

        private sealed class CombatResourceEvidence
        {
            public float RiderStandardBefore { get; set; }
            public float RiderStandardAfter { get; set; }
            public float RiderMoveBefore { get; set; }
            public float RiderMoveAfter { get; set; }
            public float MountStandardBefore { get; set; }
            public float MountStandardAfter { get; set; }
            public float MountMoveBefore { get; set; }
            public float MountMoveAfter { get; set; }
        }

        private sealed class TurnBasedCombatEvidence
        {
            public bool Requested { get; set; }
            public bool OriginalEnabled { get; set; }
            public bool TemporaryEnabled { get; set; }
            public bool OriginalRawCacheHadValue { get; set; }
            public bool EnabledAtMount { get; set; }
            public bool PairMountedBeforeEnable { get; set; }
            public bool PairRetainedAfterEnable { get; set; }
            public bool PairRetainedAfterRealtimeRestore { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string PresentationAfterEnable { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string PresentationAfterRealtimeRestore { get; set; }
            public bool ControllerInitialized { get; set; }
            public bool RosterContainsRider { get; set; }
            public bool RosterContainsMount { get; set; }
            public bool RosterContainsTarget { get; set; }
            public string ExpectedTurnActor { get; set; }
            public bool NativeActionActorTurnStarted { get; set; }
            public string CurrentTurnUnitIdAtDispatch { get; set; }
            public bool CurrentTurnActingAtDispatch { get; set; }
            public int RoundNumberAtDispatch { get; set; }
            public string CurrentTurnUnitIdAtOutcome { get; set; }
            public bool CurrentTurnActingAtOutcome { get; set; }
            public bool ActionActorTurnEndedAfterCommand { get; set; }
            public bool RestoreDeliveryCompleted { get; set; }
            public bool ModeRestored { get; set; }
            public bool PersistedValueUnchanged { get; set; }
            public int MountAiLeaseReassertionArmedCount { get; set; }
            public int MountAiLeaseReassertionAttemptCount { get; set; }
            public int MountAiLeaseReassertionMutationCount { get; set; }
            public int MountAiLeaseReassertionSuccessCount { get; set; }
            public string MountAiLeaseReassertionResult { get; set; }
            public int RiderUiLeaseRestoreArmedCount { get; set; }
            public int RiderUiLeaseRestoreAttemptCount { get; set; }
            public int RiderUiLeaseRestoreMutationCount { get; set; }
            public int RiderUiLeaseRestoreSuccessCount { get; set; }
            public string RiderUiLeaseRestoreResult { get; set; }

            public static TurnBasedCombatEvidence Capture(
                bool originalEnabled,
                bool temporaryEnabled,
                bool originalRawCacheHadValue,
                bool restoreDeliveryCompleted,
                bool enabledAtMount,
                bool pairMountedBeforeEnable,
                bool pairRetainedAfterEnable,
                bool pairRetainedAfterRealtimeRestore,
                string presentationAfterEnable,
                string presentationAfterRealtimeRestore,
                bool controllerInitialized,
                bool rosterContainsRider,
                bool rosterContainsMount,
                bool rosterContainsTarget,
                string expectedTurnActor,
                bool nativeActionActorTurnStarted,
                string currentTurnUnitIdAtDispatch,
                bool currentTurnActingAtDispatch,
                int roundNumberAtDispatch,
                string currentTurnUnitIdAtOutcome,
                bool currentTurnActingAtOutcome,
                bool actionActorTurnEndedAfterCommand,
                bool modeRestored,
                bool persistedValueUnchanged,
                int mountAiLeaseReassertionArmedCount,
                int mountAiLeaseReassertionAttemptCount,
                int mountAiLeaseReassertionMutationCount,
                int mountAiLeaseReassertionSuccessCount,
                string mountAiLeaseReassertionResult,
                int riderUiLeaseRestoreArmedCount,
                int riderUiLeaseRestoreAttemptCount,
                int riderUiLeaseRestoreMutationCount,
                int riderUiLeaseRestoreSuccessCount,
                string riderUiLeaseRestoreResult)
            {
                return new TurnBasedCombatEvidence
                {
                    Requested = true,
                    OriginalEnabled = originalEnabled,
                    TemporaryEnabled = temporaryEnabled,
                    OriginalRawCacheHadValue = originalRawCacheHadValue,
                    EnabledAtMount = enabledAtMount,
                    PairMountedBeforeEnable = pairMountedBeforeEnable,
                    PairRetainedAfterEnable = pairRetainedAfterEnable,
                    PairRetainedAfterRealtimeRestore = pairRetainedAfterRealtimeRestore,
                    PresentationAfterEnable = presentationAfterEnable,
                    PresentationAfterRealtimeRestore = presentationAfterRealtimeRestore,
                    ControllerInitialized = controllerInitialized,
                    RosterContainsRider = rosterContainsRider,
                    RosterContainsMount = rosterContainsMount,
                    RosterContainsTarget = rosterContainsTarget,
                    ExpectedTurnActor = expectedTurnActor,
                    NativeActionActorTurnStarted = nativeActionActorTurnStarted,
                    CurrentTurnUnitIdAtDispatch = currentTurnUnitIdAtDispatch,
                    CurrentTurnActingAtDispatch = currentTurnActingAtDispatch,
                    RoundNumberAtDispatch = roundNumberAtDispatch,
                    CurrentTurnUnitIdAtOutcome = currentTurnUnitIdAtOutcome,
                    CurrentTurnActingAtOutcome = currentTurnActingAtOutcome,
                    ActionActorTurnEndedAfterCommand = actionActorTurnEndedAfterCommand,
                    RestoreDeliveryCompleted = restoreDeliveryCompleted,
                    ModeRestored = modeRestored,
                    PersistedValueUnchanged = persistedValueUnchanged,
                    MountAiLeaseReassertionArmedCount = mountAiLeaseReassertionArmedCount,
                    MountAiLeaseReassertionAttemptCount = mountAiLeaseReassertionAttemptCount,
                    MountAiLeaseReassertionMutationCount = mountAiLeaseReassertionMutationCount,
                    MountAiLeaseReassertionSuccessCount = mountAiLeaseReassertionSuccessCount,
                    MountAiLeaseReassertionResult = mountAiLeaseReassertionResult,
                    RiderUiLeaseRestoreArmedCount = riderUiLeaseRestoreArmedCount,
                    RiderUiLeaseRestoreAttemptCount = riderUiLeaseRestoreAttemptCount,
                    RiderUiLeaseRestoreMutationCount = riderUiLeaseRestoreMutationCount,
                    RiderUiLeaseRestoreSuccessCount = riderUiLeaseRestoreSuccessCount,
                    RiderUiLeaseRestoreResult = riderUiLeaseRestoreResult
                };
            }
        }

        private sealed class CombatCameraFollowerSnapshot
        {
            private readonly object follower;
            private readonly FieldInfo isOnField;
            private readonly FieldInfo unitField;
            private readonly bool isOn;
            private readonly UnitEntityData unit;

            private CombatCameraFollowerSnapshot(
                object follower,
                FieldInfo isOnField,
                FieldInfo unitField,
                bool isOn,
                UnitEntityData unit)
            {
                this.follower = follower;
                this.isOnField = isOnField;
                this.unitField = unitField;
                this.isOn = isOn;
                this.unit = unit;
            }

            public bool IsCurrent => (bool)isOnField.GetValue(follower) == isOn &&
                unitField.GetValue(follower) == unit;

            public static CombatCameraFollowerSnapshot TryCapture(Game game, out string error)
            {
                error = null;
                try
                {
                    var exactFollower = game?.CameraController?.Follower;
                    if (exactFollower == null)
                    {
                        error = "Game.CameraController.Follower is unavailable.";
                        return null;
                    }
                    var type = exactFollower.GetType();
                    var exactIsOn = type.GetField("m_IsOn", BindingFlags.Instance | BindingFlags.NonPublic);
                    var exactUnit = type.GetField("m_Unit", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (exactIsOn == null || exactIsOn.FieldType != typeof(bool) ||
                        exactUnit == null || exactUnit.FieldType != typeof(UnitEntityData))
                    {
                        error = "Exact native camera-follower fields did not match their pinned contract.";
                        return null;
                    }
                    return new CombatCameraFollowerSnapshot(
                        exactFollower,
                        exactIsOn,
                        exactUnit,
                        (bool)exactIsOn.GetValue(exactFollower),
                        exactUnit.GetValue(exactFollower) as UnitEntityData);
                }
                catch (Exception exception)
                {
                    error = exception.GetType().Name + ": " + exception.Message;
                    return null;
                }
            }

            public void Restore()
            {
                unitField.SetValue(follower, unit);
                isOnField.SetValue(follower, isOn);
                if (!IsCurrent)
                {
                    throw new InvalidOperationException("Native camera-follower fields did not restore to their captured values.");
                }
            }
        }

        private sealed class CombatCommandEvidence
        {
            public string Action { get; set; }
            public string ActorId { get; set; }
            public string CommandOwnerId { get; set; }
            public string ResourceOwnerId { get; set; }
            public string TargetId { get; set; }
            public string Result { get; set; }
            public int ChildAttackStartCount { get; set; }
            public int RepathCount { get; set; }
            public bool RiderStandardCharged { get; set; }
            public bool ActionStandardCharged { get; set; }
            public bool NativeAttackRuleObserved { get; set; }
            public string AttackWeaponBlueprintId { get; set; }
            public bool AttackWeaponIsNatural { get; set; }
            public bool AttackWeaponIsRanged { get; set; }
            public string AttackWeaponSlot { get; set; }
            public string TerminalReason { get; set; }
            public bool PairRangeSatisfiedAtStart { get; set; }
            public float PairDistanceAtStart { get; set; }
            public float PairApproachRadiusAtStart { get; set; }
            public float NativeExecutorDistanceAtStart { get; set; }
            public float NativeAdmissionRadiusAtStart { get; set; }
            public bool NativeAdmissionAdjusted { get; set; }

            public static CombatCommandEvidence From(MountedPairAttackOutcome value)
            {
                return value == null ? null : new CombatCommandEvidence
                {
                    Action = value.Action.ToString(),
                    ActorId = value.ActorId,
                    CommandOwnerId = value.CommandOwnerId,
                    ResourceOwnerId = value.ResourceOwnerId,
                    TargetId = value.TargetId,
                    Result = value.Result,
                    ChildAttackStartCount = value.ChildAttackStartCount,
                    RepathCount = value.RepathCount,
                    RiderStandardCharged = value.RiderStandardCharged,
                    ActionStandardCharged = value.ActionStandardCharged,
                    NativeAttackRuleObserved = value.NativeAttackRuleObserved,
                    AttackWeaponBlueprintId = value.AttackWeaponBlueprintId,
                    AttackWeaponIsNatural = value.AttackWeaponIsNatural,
                    AttackWeaponIsRanged = value.AttackWeaponIsRanged,
                    AttackWeaponSlot = value.AttackWeaponSlot,
                    TerminalReason = value.TerminalReason,
                    PairRangeSatisfiedAtStart = value.PairRangeSatisfiedAtStart,
                    PairDistanceAtStart = value.PairDistanceAtStart,
                    PairApproachRadiusAtStart = value.PairApproachRadiusAtStart,
                    NativeExecutorDistanceAtStart = value.NativeExecutorDistanceAtStart,
                    NativeAdmissionRadiusAtStart = value.NativeAdmissionRadiusAtStart,
                    NativeAdmissionAdjusted = value.NativeAdmissionAdjusted
                };
            }
        }

        private static bool IsNativeAcMissReason(string result)
        {
            return string.Equals(result, "Miss", StringComparison.Ordinal) ||
                string.Equals(result, "DodgeAC", StringComparison.Ordinal) ||
                string.Equals(result, "ArmorAC", StringComparison.Ordinal) ||
                string.Equals(result, "ShieldAC", StringComparison.Ordinal);
        }

        private sealed class CombatRuleEvidence
        {
            public int? ForcedD20 { get; set; }
            public int ForcedD20Count { get; set; }
            public int AttackRuleCount { get; set; }
            public int AttackRollCount { get; set; }
            public int DamageRuleCount { get; set; }
            public int UnexpectedPairAttackCount { get; set; }
            public int TotalDamage { get; set; }
            public string LastInitiatorId { get; set; }
            public string LastTargetId { get; set; }
            public string LastAttackResult { get; set; }
            public bool? LastAttackHit { get; set; }

            public static CombatRuleEvidence From(MountedCombatRuleProbe value)
            {
                return value == null ? null : new CombatRuleEvidence
                {
                    ForcedD20 = value.ForcedD20,
                    ForcedD20Count = value.ForcedD20Count,
                    AttackRuleCount = value.AttackRuleCount,
                    AttackRollCount = value.AttackRollCount,
                    DamageRuleCount = value.DamageRuleCount,
                    UnexpectedPairAttackCount = value.UnexpectedPairAttackCount,
                    TotalDamage = value.TotalDamage,
                    LastInitiatorId = value.LastInitiatorId,
                    LastTargetId = value.LastTargetId,
                    LastAttackResult = value.LastAttackResult,
                    LastAttackHit = value.LastAttackHit
                };
            }
        }

        private sealed class CombatMovementEvidence
        {
            public string AuthoritativeMover { get; set; }
            public int RepathCount { get; set; }
            public float RiderDisplacementAtOutcome { get; set; }
            public float MountDisplacementAtOutcome { get; set; }
            public float TargetDisplacementAtOutcome { get; set; }
            public bool? RiderStockAgentEnabledAtEnd { get; set; }
            public bool? MountStockAgentEnabledAtEnd { get; set; }
            public bool? RiderAvoidanceDisabledAtEnd { get; set; }
            public bool? MountAvoidanceDisabledAtEnd { get; set; }
        }

        private sealed class CombatMovementToAttackEvidence
        {
            public float RequestedTargetDistance { get; set; }
            public bool ApproachRequiredAtStart { get; set; }
            public int DelegatedMoveStartCount { get; set; }
            public int DelegatedMoveTickCount { get; set; }
            public string DelegatedMoveExecutorId { get; set; }
            public bool DelegatedMoveExecutorIsExactMount { get; set; }
            public bool WrapperCommandRetainedThroughoutApproach { get; set; }
            public bool DelegatedMoveNeverQueuedOnMount { get; set; }
            public bool DelegatedMoveOwnedByMountMoveSlot { get; set; }
            public bool MountMoveSlotUnreplacedThroughoutApproach { get; set; }
            public bool MountQueueEmptyThroughoutApproach { get; set; }
            public bool DelegatedMoveFinishedSuccessfully { get; set; }
            public bool MountMoveSlotRestoredAfterApproach { get; set; }
            public bool DelegatedMoveDrivenByStockController { get; set; }
            public bool DelegatedMoveDrivenByRiderTurnAdapter { get; set; }
            public int DelegatedMoveProgressObservationCount { get; set; }
            public bool RiderStockAgentSuppressedThroughoutApproach { get; set; }
            public bool MountStockAgentAuthoritativeThroughoutApproach { get; set; }
            public bool PoseHealthyThroughoutApproach { get; set; }
            public int CommandObservationCount { get; set; }
            public int RuntimeObservationCount { get; set; }
            public bool SelectionRetainedDuringApproach { get; set; }
            public bool UiCoherentDuringApproach { get; set; }
            public float InitialPairDistance { get; set; }
            public float PairDistanceAtAttackStart { get; set; }
            public float RiderDisplacementAtAttackStart { get; set; }
            public float MountDisplacementAtAttackStart { get; set; }
            public float TargetDisplacementAtAttackStart { get; set; }

            public static CombatMovementToAttackEvidence From(
                MountedPairAttackOutcome value,
                float requestedTargetDistance,
                int runtimeObservationCount,
                bool selectionRetained,
                bool uiCoherent)
            {
                return new CombatMovementToAttackEvidence
                {
                    RequestedTargetDistance = requestedTargetDistance,
                    ApproachRequiredAtStart = value != null && value.ApproachRequiredAtStart,
                    DelegatedMoveStartCount = value?.DelegatedMoveStartCount ?? 0,
                    DelegatedMoveTickCount = value?.DelegatedMoveTickCount ?? 0,
                    DelegatedMoveExecutorId = value?.DelegatedMoveExecutorId,
                    DelegatedMoveExecutorIsExactMount = value != null && value.DelegatedMoveExecutorIsExactMount,
                    WrapperCommandRetainedThroughoutApproach = value != null && value.WrapperCommandRetainedThroughoutApproach,
                    DelegatedMoveNeverQueuedOnMount = value != null && value.DelegatedMoveNeverQueuedOnMount,
                    DelegatedMoveOwnedByMountMoveSlot = value != null && value.DelegatedMoveOwnedByMountMoveSlot,
                    MountMoveSlotUnreplacedThroughoutApproach = value != null && value.MountMoveSlotUnreplacedThroughoutApproach,
                    MountQueueEmptyThroughoutApproach = value != null && value.MountQueueEmptyThroughoutApproach,
                    DelegatedMoveFinishedSuccessfully = value != null && value.DelegatedMoveFinishedSuccessfully,
                    MountMoveSlotRestoredAfterApproach = value != null && value.MountMoveSlotRestoredAfterApproach,
                    DelegatedMoveDrivenByStockController = value != null && value.DelegatedMoveDrivenByStockController,
                    DelegatedMoveDrivenByRiderTurnAdapter = value != null && value.DelegatedMoveDrivenByRiderTurnAdapter,
                    DelegatedMoveProgressObservationCount = value?.DelegatedMoveProgressObservationCount ?? 0,
                    RiderStockAgentSuppressedThroughoutApproach = value != null && value.RiderStockAgentSuppressedThroughoutApproach,
                    MountStockAgentAuthoritativeThroughoutApproach = value != null && value.MountStockAgentAuthoritativeThroughoutApproach,
                    PoseHealthyThroughoutApproach = value != null && value.PoseHealthyThroughoutApproach,
                    CommandObservationCount = value?.ApproachObservationCount ?? 0,
                    RuntimeObservationCount = runtimeObservationCount,
                    SelectionRetainedDuringApproach = selectionRetained,
                    UiCoherentDuringApproach = uiCoherent,
                    InitialPairDistance = value?.InitialPairDistance ?? 0f,
                    PairDistanceAtAttackStart = value?.PairDistanceAtAttackStart ?? 0f,
                    RiderDisplacementAtAttackStart = value?.RiderDisplacementAtAttackStart ?? 0f,
                    MountDisplacementAtAttackStart = value?.MountDisplacementAtAttackStart ?? 0f,
                    TargetDisplacementAtAttackStart = value?.TargetDisplacementAtAttackStart ?? 0f
                };
            }
        }

        private sealed class CombatCommandTerminationEvidence
        {
            public string Kind { get; set; }
            public string Trigger { get; set; }
            public bool Delivered { get; set; }
            public bool RepeatedIdempotently { get; set; }
            public bool WrapperPresentBefore { get; set; }
            public bool DelegatedMovePresentBefore { get; set; }
            public bool RiderQueueEmptyBefore { get; set; }
            public bool MountQueueEmptyBefore { get; set; }
            public bool ChildAttackNotStartedBefore { get; set; }
            public float PairDistanceAtTrigger { get; set; }
            public float RiderDisplacementAtTrigger { get; set; }
            public float MountDisplacementAtTrigger { get; set; }
            public float TargetDisplacementAtTrigger { get; set; }
            public bool WrapperAbsentAfter { get; set; }
            public bool DelegatedMoveAbsentAfter { get; set; }
            public bool RiderQueueEmptyAfter { get; set; }
            public bool MountQueueEmptyAfter { get; set; }
            public bool MountAgentStoppedAfter { get; set; }
            public bool ActiveCommandClearedAfter { get; set; }
            public bool RelationshipPreservedAfter { get; set; }
            public bool SelectionRetainedAfter { get; set; }
            public bool UiCoherentAfter { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? LifecycleDeliveryCount { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool? LifecycleDeliveriesExact { get; set; }
        }

        private sealed class CombatTargetProvisioningEvidence
        {
            public string TargetBlueprintId { get; set; }
            public string RuntimeGroupId { get; set; }
            public string BlueprintEmptyHandWeaponBlueprintId { get; set; }
            public string TargetNativeSingleAttackWeaponBlueprintId { get; set; }
            public string TargetNativeSingleAttackSlot { get; set; }
            public int TargetPrimaryMainAttacks { get; set; }
            public int TargetSecondaryMainAttacks { get; set; }
            public int AdditionalLimbCountBefore { get; set; }
            public int AdditionalLimbCountAfter { get; set; }
            public bool NoWeaponProvisioningMutation { get; set; }
            public bool TargetPrimaryHandHasItem { get; set; }
            public bool TargetWeaponUsesEmptyHandFallback { get; set; }
            public bool TargetNativeSingleAttackWeaponIsNatural { get; set; }
            public bool TargetNativeSingleAttackWeaponIsMelee { get; set; }
            public bool NoLoot { get; set; }
            public bool RawAiDisabled { get; set; }
            public bool SleeplessBefore { get; set; }
            public bool SleeplessLeaseAcquired { get; set; }
            public int TemporaryHitPointsBefore { get; set; }
            public int TemporaryHitPointsAfterProvisioning { get; set; }
            public int DurabilityLeaseAmount { get; set; }
            public bool DurabilityLeaseAcquired { get; set; }
            public bool BidirectionalHostility { get; set; }
            public bool NoExperienceReward { get; set; }

            public static CombatTargetProvisioningEvidence From(
                DiagnosticCombatTargetService service,
                UnitEntityData target)
            {
                return new CombatTargetProvisioningEvidence
                {
                    TargetBlueprintId = target?.Blueprint?.AssetGuid,
                    RuntimeGroupId = service?.CreatedRuntimeGroupId,
                    BlueprintEmptyHandWeaponBlueprintId = service?.BlueprintEmptyHandWeaponBlueprintId,
                    TargetNativeSingleAttackWeaponBlueprintId = service?.TargetNativeSingleAttackWeaponBlueprintId,
                    TargetNativeSingleAttackSlot = service?.TargetNativeSingleAttackSlot,
                    TargetPrimaryMainAttacks = service?.TargetPrimaryMainAttacks ?? 0,
                    TargetSecondaryMainAttacks = service?.TargetSecondaryMainAttacks ?? 0,
                    AdditionalLimbCountBefore = service?.AdditionalLimbCountBefore ?? 0,
                    AdditionalLimbCountAfter = service?.AdditionalLimbCountAfter ?? 0,
                    NoWeaponProvisioningMutation = service != null && service.NoWeaponProvisioningMutation,
                    TargetPrimaryHandHasItem = service != null && service.TargetPrimaryHandHasItem,
                    TargetWeaponUsesEmptyHandFallback = service != null && service.TargetWeaponUsesEmptyHandFallback,
                    TargetNativeSingleAttackWeaponIsNatural = service != null && service.TargetNativeSingleAttackWeaponIsNatural,
                    TargetNativeSingleAttackWeaponIsMelee = service != null && service.TargetNativeSingleAttackWeaponIsMelee,
                    NoLoot = service != null && service.TargetHasNoLoot,
                    RawAiDisabled = service != null && service.RawAiBackingDisabled,
                    SleeplessBefore = service != null && service.TargetSleeplessBefore,
                    SleeplessLeaseAcquired = service != null && service.TargetSleeplessLeaseAcquired,
                    TemporaryHitPointsBefore = service?.TargetTemporaryHitPointsBefore ?? 0,
                    TemporaryHitPointsAfterProvisioning = service?.TargetTemporaryHitPointsAfterProvisioning ?? 0,
                    DurabilityLeaseAmount = service?.TargetDurabilityLeaseAmount ?? 0,
                    DurabilityLeaseAcquired = service != null && service.TargetDurabilityLeaseAcquired,
                    BidirectionalHostility = service != null && service.BidirectionalHostilityVerified,
                    NoExperienceReward = service != null && service.NoExperienceReward
                };
            }
        }

        private sealed class CombatTargetBrainLeaseEvidence
        {
            public bool BrainActiveBefore { get; set; }
            public bool LeaseAcquired { get; set; }
            public bool EffectiveAiEnabledDuring { get; set; }
            public int ValidationCount { get; set; }
            public bool ViolationObserved { get; set; }
            public bool SuppressedAtClick { get; set; }
            public bool SuppressedAtOutcome { get; set; }
            public bool BrainActiveAfterRelease { get; set; }
            public bool LeaseReleased { get; set; }

            public static CombatTargetBrainLeaseEvidence From(DiagnosticCombatTargetService service)
            {
                return new CombatTargetBrainLeaseEvidence
                {
                    BrainActiveBefore = service != null && service.TargetBrainActiveBefore,
                    LeaseAcquired = service != null && service.TargetBrainLeaseAcquired,
                    EffectiveAiEnabledDuring = service != null && service.TargetEffectiveAiEnabledDuringBrainLease,
                    ValidationCount = service?.TargetBrainLeaseValidationCount ?? 0,
                    ViolationObserved = service != null && service.TargetBrainLeaseViolationObserved,
                    SuppressedAtClick = service != null && service.TargetBrainSuppressedAtClick,
                    SuppressedAtOutcome = service != null && service.TargetBrainSuppressedAtOutcome,
                    BrainActiveAfterRelease = service != null && service.TargetBrainActiveAfterRelease,
                    LeaseReleased = service != null && service.TargetBrainLeaseReleased
                };
            }
        }

        private sealed class CombatTargetLifeEvidence
        {
            public CombatTargetLifeSnapshotEvidence ImmediatelyAfterCreation { get; set; }
            public CombatTargetLifeSnapshotEvidence AtActivation { get; set; }
            public CombatTargetLifeSnapshotEvidence LastObserved { get; set; }
            public int TransitionCount { get; set; }
            public CombatTargetLifeTransitionEvidence FirstTransition { get; set; }

            public static CombatTargetLifeEvidence From(DiagnosticCombatTargetService service)
            {
                return new CombatTargetLifeEvidence
                {
                    ImmediatelyAfterCreation = CombatTargetLifeSnapshotEvidence.From(
                        service?.LifeImmediatelyAfterCreation),
                    AtActivation = CombatTargetLifeSnapshotEvidence.From(service?.LifeAtActivation),
                    LastObserved = CombatTargetLifeSnapshotEvidence.From(service?.LastObservedLife),
                    TransitionCount = service?.LifeTransitionCount ?? 0,
                    FirstTransition = CombatTargetLifeTransitionEvidence.From(service?.FirstLifeTransition)
                };
            }
        }

        private sealed class CombatTargetLifeSnapshotEvidence
        {
            public bool Observed { get; set; }
            public string LifeState { get; set; }
            public bool Conscious { get; set; }
            public bool Dead { get; set; }
            public bool FinallyDead { get; set; }
            public int Damage { get; set; }
            public int NonLethalDamage { get; set; }
            public int HitPoints { get; set; }
            public int Constitution { get; set; }
            public bool ForceKill { get; set; }
            public bool MarkedForDeath { get; set; }

            public static CombatTargetLifeSnapshotEvidence From(DiagnosticTargetLifeSnapshot value)
            {
                return new CombatTargetLifeSnapshotEvidence
                {
                    Observed = value != null,
                    LifeState = value?.LifeState,
                    Conscious = value != null && value.Conscious,
                    Dead = value != null && value.Dead,
                    FinallyDead = value != null && value.FinallyDead,
                    Damage = value?.Damage ?? 0,
                    NonLethalDamage = value?.NonLethalDamage ?? 0,
                    HitPoints = value?.HitPoints ?? 0,
                    Constitution = value?.Constitution ?? 0,
                    ForceKill = value != null && value.ForceKill,
                    MarkedForDeath = value != null && value.MarkedForDeath
                };
            }
        }

        private sealed class CombatTargetLifeTransitionEvidence
        {
            public bool Observed { get; set; }
            public string PreviousLifeState { get; set; }
            public string CurrentLifeState { get; set; }
            public CombatTargetLifeSnapshotEvidence Snapshot { get; set; }

            public static CombatTargetLifeTransitionEvidence From(DiagnosticTargetLifeTransition value)
            {
                return new CombatTargetLifeTransitionEvidence
                {
                    Observed = value != null,
                    PreviousLifeState = value?.PreviousLifeState,
                    CurrentLifeState = value?.CurrentLifeState,
                    Snapshot = CombatTargetLifeSnapshotEvidence.From(value?.Snapshot)
                };
            }
        }

        private sealed class CombatTargetIncomingRulesEvidence
        {
            public bool DispatchMarkerSet { get; set; }
            public int AttackRuleCount { get; set; }
            public int DamageRuleCount { get; set; }
            public int PreDispatchAttackRuleCount { get; set; }
            public int PreDispatchDamageRuleCount { get; set; }
            public CombatTargetIncomingAttackEvidence FirstAttack { get; set; }
            public CombatTargetIncomingDamageEvidence FirstDamage { get; set; }

            public static CombatTargetIncomingRulesEvidence From(DiagnosticCombatTargetService service)
            {
                return new CombatTargetIncomingRulesEvidence
                {
                    DispatchMarkerSet = service != null && service.ExpectedAttackDispatchStarted,
                    AttackRuleCount = service?.IncomingAttackRuleCount ?? 0,
                    DamageRuleCount = service?.IncomingDamageRuleCount ?? 0,
                    PreDispatchAttackRuleCount = service?.PreDispatchIncomingAttackRuleCount ?? 0,
                    PreDispatchDamageRuleCount = service?.PreDispatchIncomingDamageRuleCount ?? 0,
                    FirstAttack = CombatTargetIncomingAttackEvidence.From(service?.FirstIncomingAttack),
                    FirstDamage = CombatTargetIncomingDamageEvidence.From(service?.FirstIncomingDamage)
                };
            }
        }

        private sealed class CombatNonPairPartyAiLeaseEvidence
        {
            public bool Acquired { get; set; }
            public string GroupId { get; set; }
            public bool GroupIsPlayerParty { get; set; }
            public bool RiderSharesGroup { get; set; }
            public bool MountSharesGroup { get; set; }
            public int MemberCount { get; set; }
            public bool ActiveValidationPassed { get; set; }
            public bool Restored { get; set; }
            public string LastError { get; set; }
            public IReadOnlyList<CombatNonPairPartyAiMemberEvidence> Members { get; set; }

            public static CombatNonPairPartyAiLeaseEvidence From(DiagnosticCombatTargetService service)
            {
                var adapter = service?.NonPairPartyAiLease;
                var members = new List<CombatNonPairPartyAiMemberEvidence>();
                if (adapter != null)
                {
                    foreach (var state in adapter.Members)
                    {
                        members.Add(CombatNonPairPartyAiMemberEvidence.From(state));
                    }
                }
                return new CombatNonPairPartyAiLeaseEvidence
                {
                    Acquired = adapter != null && adapter.Acquired,
                    GroupId = adapter?.GroupId,
                    GroupIsPlayerParty = adapter != null && adapter.GroupIsPlayerParty,
                    RiderSharesGroup = adapter != null && adapter.RiderSharesGroup,
                    MountSharesGroup = adapter != null && adapter.MountSharesGroup,
                    MemberCount = members.Count,
                    ActiveValidationPassed = adapter != null && adapter.ActiveValidationPassed,
                    Restored = adapter == null || adapter.Restored,
                    LastError = adapter?.LastError,
                    Members = members
                };
            }
        }

        private sealed class CombatNonPairPartyAiMemberEvidence
        {
            public string UnitId { get; set; }
            public string BlueprintId { get; set; }
            public bool DirectlyControllable { get; set; }
            public bool InState { get; set; }
            public bool CommandsEmptyBefore { get; set; }
            public bool RawAiBefore { get; set; }
            public bool EffectiveAiBefore { get; set; }
            public bool CommandsEmptyDuring { get; set; }
            public bool RawAiDuring { get; set; }
            public bool EffectiveAiDuring { get; set; }
            public bool CommandsEmptyAfter { get; set; }
            public bool RawAiAfter { get; set; }
            public bool EffectiveAiAfter { get; set; }

            public static CombatNonPairPartyAiMemberEvidence From(
                ScopedDiagnosticAiLease<UnitEntityData>.State state)
            {
                return new CombatNonPairPartyAiMemberEvidence
                {
                    UnitId = state?.UnitId,
                    BlueprintId = state?.Unit?.Blueprint?.AssetGuid,
                    DirectlyControllable = state?.Unit != null && state.Unit.IsDirectlyControllable,
                    InState = state?.Unit != null && state.Unit.IsInState,
                    CommandsEmptyBefore = state != null && state.CommandsEmptyBefore,
                    RawAiBefore = state != null && state.RawAiBefore,
                    EffectiveAiBefore = state != null && state.EffectiveAiBefore,
                    CommandsEmptyDuring = state != null && state.CommandsEmptyDuring,
                    RawAiDuring = state != null && state.RawAiDuring,
                    EffectiveAiDuring = state != null && state.EffectiveAiDuring,
                    CommandsEmptyAfter = state != null && state.CommandsEmptyAfter,
                    RawAiAfter = state != null && state.RawAiAfter,
                    EffectiveAiAfter = state != null && state.EffectiveAiAfter
                };
            }
        }

        private sealed class CombatTargetIncomingAttackEvidence
        {
            public bool Observed { get; set; }
            public bool BeforeExpectedDispatch { get; set; }
            public string InitiatorId { get; set; }
            public string InitiatorBlueprintId { get; set; }
            public bool InitiatorIsPlayerFaction { get; set; }
            public bool InitiatorIsPlayersEnemy { get; set; }
            public string InitiatorGroupId { get; set; }
            public bool InitiatorGroupIsPlayerParty { get; set; }
            public bool InitiatorSharesRiderGroup { get; set; }
            public bool InitiatorSharesMountGroup { get; set; }
            public bool InitiatorDirectlyControllable { get; set; }
            public bool InitiatorEffectiveAiEnabled { get; set; }
            public bool InitiatorRawAiEnabled { get; set; }
            public bool InitiatorCommandsEmpty { get; set; }
            public string WeaponBlueprintId { get; set; }
            public bool IsAttackOfOpportunity { get; set; }
            public bool IsCharge { get; set; }

            public static CombatTargetIncomingAttackEvidence From(DiagnosticIncomingAttackSnapshot value)
            {
                return new CombatTargetIncomingAttackEvidence
                {
                    Observed = value != null,
                    BeforeExpectedDispatch = value != null && value.BeforeExpectedDispatch,
                    InitiatorId = value?.InitiatorId,
                    InitiatorBlueprintId = value?.InitiatorBlueprintId,
                    InitiatorIsPlayerFaction = value != null && value.InitiatorIsPlayerFaction,
                    InitiatorIsPlayersEnemy = value != null && value.InitiatorIsPlayersEnemy,
                    InitiatorGroupId = value?.InitiatorGroupId,
                    InitiatorGroupIsPlayerParty = value != null && value.InitiatorGroupIsPlayerParty,
                    InitiatorSharesRiderGroup = value != null && value.InitiatorSharesRiderGroup,
                    InitiatorSharesMountGroup = value != null && value.InitiatorSharesMountGroup,
                    InitiatorDirectlyControllable = value != null && value.InitiatorDirectlyControllable,
                    InitiatorEffectiveAiEnabled = value != null && value.InitiatorEffectiveAiEnabled,
                    InitiatorRawAiEnabled = value != null && value.InitiatorRawAiEnabled,
                    InitiatorCommandsEmpty = value != null && value.InitiatorCommandsEmpty,
                    WeaponBlueprintId = value?.WeaponBlueprintId,
                    IsAttackOfOpportunity = value != null && value.IsAttackOfOpportunity,
                    IsCharge = value != null && value.IsCharge
                };
            }
        }

        private sealed class CombatTargetIncomingDamageEvidence
        {
            public bool Observed { get; set; }
            public bool BeforeExpectedDispatch { get; set; }
            public string InitiatorId { get; set; }
            public string InitiatorBlueprintId { get; set; }
            public bool InitiatorIsPlayerFaction { get; set; }
            public bool InitiatorIsPlayersEnemy { get; set; }
            public int Damage { get; set; }
            public bool IsFake { get; set; }
            public bool IsDot { get; set; }
            public bool AttackRollPresent { get; set; }
            public string WeaponBlueprintId { get; set; }
            public string SourceAbilityBlueprintId { get; set; }
            public string SourceAreaBlueprintId { get; set; }

            public static CombatTargetIncomingDamageEvidence From(DiagnosticIncomingDamageSnapshot value)
            {
                return new CombatTargetIncomingDamageEvidence
                {
                    Observed = value != null,
                    BeforeExpectedDispatch = value != null && value.BeforeExpectedDispatch,
                    InitiatorId = value?.InitiatorId,
                    InitiatorBlueprintId = value?.InitiatorBlueprintId,
                    InitiatorIsPlayerFaction = value != null && value.InitiatorIsPlayerFaction,
                    InitiatorIsPlayersEnemy = value != null && value.InitiatorIsPlayersEnemy,
                    Damage = value?.Damage ?? 0,
                    IsFake = value != null && value.IsFake,
                    IsDot = value != null && value.IsDot,
                    AttackRollPresent = value != null && value.AttackRollPresent,
                    WeaponBlueprintId = value?.WeaponBlueprintId,
                    SourceAbilityBlueprintId = value?.SourceAbilityBlueprintId,
                    SourceAreaBlueprintId = value?.SourceAreaBlueprintId
                };
            }
        }

        private sealed class CombatPoseEvidence
        {
            public string ProfileId { get; set; }
            public bool HealthyAtOutcome { get; set; }
            public bool ConfiguredAtEnd { get; set; }
            public bool AttachmentLeaseAtEnd { get; set; }
            public bool ResidueAtEnd { get; set; }
        }

        private sealed class CombatCleanupEvidence
        {
            public bool TargetRemoved { get; set; }
            public bool TargetEntityRemoved { get; set; }
            public bool RuntimeGroupRemoved { get; set; }
            public bool RuntimeFactionRemoved { get; set; }
            public bool DurabilityLeaseReleased { get; set; }
            public bool BrainLeaseReleased { get; set; }
            public bool SleeplessLeaseReleased { get; set; }
            public bool NonPairPartyAiLeaseRestored { get; set; }
            public bool RelationshipClean { get; set; }
            public bool CombatCleared { get; set; }
            public string RelationshipState { get; set; }
            public bool ResidualState { get; set; }
            public bool PresentationResidual { get; set; }
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
