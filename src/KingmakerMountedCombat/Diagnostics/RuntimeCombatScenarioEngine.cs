using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        private readonly MountedCombatController combat;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly Stopwatch rowClock = new Stopwatch();
        private readonly string evidencePath;
        private readonly string dllSha256;
        private readonly string dllMvid;

        private DiagnosticCombatTargetService targetService;
        private MountedCombatRuleProbe ruleProbe;
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
        private float targetDistanceAtClick;
        private Vector3 riderPositionAtClick;
        private Vector3 mountPositionAtClick;
        private Vector3 targetPositionAtClick;
        private float riderDisplacementAtOutcome;
        private float mountDisplacementAtOutcome;
        private float targetDisplacementAtOutcome;
        private bool clickAccepted;
        private MountedPairAttackOutcome outcome;
        private bool targetRemoved;
        private bool targetEntityRemoved;
        private bool targetRuntimeGroupRemoved;
        private bool targetRuntimeFactionRemoved;
        private bool targetDurabilityLeaseReleased;
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
        private DiagnosticNativeCombatJoinReadinessSnapshot nativeJoinReadiness;
        private DiagnosticTurnBasedDispatchReadinessSnapshot turnBasedReadiness;
        private NativeModeTransitionProbe turnBasedModeProbe;
        private bool turnBasedModeEnabledAtMount;
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

        public RuntimeCombatScenarioEngine(
            RuntimeRequest request,
            GameMountedRelationshipService relationship,
            MountedCombatController combat,
            DiagnosticSettings settings,
            IModLogger logger)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
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
                string.Equals(scenario, MammothPrimaryHitTurnBased, StringComparison.Ordinal);
        }

        private bool IsTurnBasedRow =>
            string.Equals(currentRow, RiderHitTurnBased, StringComparison.Ordinal) ||
            string.Equals(currentRow, MammothPrimaryHitTurnBased, StringComparison.Ordinal);

        private bool IsMissRow => string.Equals(currentRow, RiderMissRealTime, StringComparison.Ordinal);

        private bool IsMammothPrimaryRow =>
            string.Equals(currentRow, MammothPrimaryHitRealTime, StringComparison.Ordinal) ||
            string.Equals(currentRow, MammothPrimaryHitTurnBased, StringComparison.Ordinal);

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
                if (rowClock.Elapsed.TotalSeconds > RowTimeoutSeconds && step != CombatEngineStep.AwaitCleanupFrame)
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
                    case CombatEngineStep.AwaitOutcome:
                        ObserveOutcome();
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
                RestorePause();
                RestoreSettings();
                rowClock.Stop();
                disposed = true;
            }
        }

        private void BeginRow()
        {
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

            if (IsTurnBasedRow)
            {
                turnBasedModeProbe = new NativeModeTransitionProbe();
                assertions.Check(!turnBasedModeProbe.OriginalValue && turnBasedModeProbe.TemporaryValue,
                    "Turn-based combat row leased an exact false-to-true native mode transition.");
                if (assertions.FailureCount != 0)
                {
                    BeginCleanup();
                    return;
                }
                turnBasedModeRestored = false;
                turnBasedPersistedSettingUnchanged = false;
                turnBasedModeProbe.DispatchTemporaryValue();
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
            target = targetService.Spawn(rider, mount, spawnPoint, request.RunId, true, IsMammothPrimaryRow);
            targetId = target.UniqueId;
            targetProvisioning = CombatTargetProvisioningEvidence.From(targetService, target);
            assertions.Check(target != null && target.IsInState && target.View != null &&
                    target.IsEnemy(rider) && rider.IsEnemy(target) &&
                    AttackActor != null && AttackActor.IsEnemy(target) && AttackActor.CanAttack(target),
                "Runtime-only hostile Mammoth target passed exact creation gates.");

            var rangeProbe = new MountedPairSingleAttack(target, rider, mount, !IsMammothPrimaryRow);
            rangeProbe.Init(AttackActor);
            pairApproachRadius = rangeProbe.PairApproachRadius;
            float finalDistance;
            assertions.Check(MountedCombatSpatialPolicy.TryCalculateDiagnosticTargetDistance(
                    pairApproachRadius,
                    out finalDistance),
                "Mounted rider pair approach radius admits the bounded diagnostic placement.");
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
            }

            var handsEquipment = game.HandsEquipmentController;
            var actionActor = AttackActor;
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

            assertions.Check(IsTurnBasedRow
                    ? CombatController.IsInTurnBasedCombat()
                    : !CombatController.IsInTurnBasedCombat(),
                "Combat mode remained exact at dispatch.");
            assertions.Check(entryReadiness.AllPassed,
                "Native memory, combat entry, initiative preparation, and Default-mode time remained exact at dispatch.");
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
                "Diagnostic target was retained at the exact current actor-specific near-boundary placement before dispatch.");
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
            assertions.Check(targetDistanceAtClick <= pairApproachRadius + MountedCombatSpatialPolicy.RangeTolerance,
                "Target was inside the exact Mammoth-origin rider melee radius at dispatch.");
            assertions.Check(MountedCombatSpatialPolicy.IsBoundedDiagnosticTargetDistance(
                    pairApproachRadius,
                    targetDistanceAtClick),
                "Diagnostic target retained the exact near-boundary mounted-range placement.");

            ruleProbe = new MountedCombatRuleProbe();
            ruleProbe.Arm(rider, mount, AttackActor, target, IsMissRow ? 1 : 20);
            assertions.Check(targetService != null && targetService.BeginExpectedAttackDispatch(target),
                "Target incoming-rule observation marked the exact expected pair-action dispatch boundary.");
            assertions.Check(combat.ArmedAction == AttackAction || combat.Arm(AttackAction),
                AttackAction + " armed through the combat controller on the exact action actor turn.");
            if (assertions.FailureCount != 0)
            {
                BeginCleanup();
                return;
            }

            clickAccepted = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
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

        private bool RetainDiagnosticTargetPlacementAtDispatch()
        {
            if (mount == null || mount.View == null || target == null || target.View == null ||
                !target.IsInState)
            {
                return false;
            }

            var observedDistance = HorizontalDistance(mount.Position, target.Position);
            if (!MountedCombatSpatialPolicy.RequiresDiagnosticTargetPlacementRefresh(
                    pairApproachRadius,
                    observedDistance))
            {
                return true;
            }

            float requiredDistance;
            if (!MountedCombatSpatialPolicy.TryCalculateDiagnosticTargetDistance(
                    pairApproachRadius,
                    out requiredDistance))
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
            return MountedCombatSpatialPolicy.IsBoundedDiagnosticTargetDistance(
                pairApproachRadius,
                HorizontalDistance(mount.Position, target.Position));
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
                "Stationary in-range attack required no delegated movement or repath.");
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
            assertions.Check(Math.Abs(mountMoveAfter - mountMoveBefore) <= 0.01f &&
                    Math.Abs(riderMoveAfter - riderMoveBefore) <= 0.01f,
                "Stationary mounted action charged neither rider nor Mammoth Move action.");
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
            assertions.Check(riderDisplacementAtOutcome <= 0.05f &&
                    mountDisplacementAtOutcome <= 0.05f &&
                    targetDisplacementAtOutcome <= 0.05f,
                "Mounted pair and target attack remained stationary at the authoritative Mammoth origin.");

            poseProfileAtOutcome = relationship.Runtime.PoseProfileId;
            poseHealthyAtOutcome = relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied;

            BeginCleanup();
        }

        private void BeginCleanup()
        {
            if (step == CombatEngineStep.AwaitCleanupFrame || completed)
            {
                return;
            }

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

            assertions.Check(targetRemoved && targetEntityRemoved &&
                    targetRuntimeGroupRemoved && targetRuntimeFactionRemoved && targetSleeplessLeaseReleased &&
                    targetNonPairPartyAiLeaseRestored,
                "Runtime-only combat target, sleepless lease, non-pair party AI lease, project group, and runtime faction were removed with zero residue.");
            assertions.Check(combatCleared,
                "Pair, target, and party left combat before final evidence.");
            assertions.Check(pauseRestored,
                "The exact pre-row pause state was restored after the combat lease.");
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
                SchemaVersion = IsTurnBasedRow ? 23 : 22,
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
                ClickAccepted = clickAccepted,
                PairApproachRadius = pairApproachRadius,
                TargetDistanceAtClick = targetDistanceAtClick,
                RiderPositionAtClick = PositionEvidence.From(riderPositionAtClick),
                MountPositionAtClick = PositionEvidence.From(mountPositionAtClick),
                TargetPositionAtClick = PositionEvidence.From(targetPositionAtClick),
                CombatEntry = CombatEntryEvidence.From(entryReadiness, nativeJoinReadiness, combatMemoryRemoved),
                Dispatch = CombatDispatchEvidence.From(
                    originalPause,
                    unpausedForRealTime,
                    pausedAtClick,
                    dispatchReadiness,
                    pauseRestored),
                TurnBased = IsTurnBasedRow
                    ? TurnBasedCombatEvidence.Capture(
                        turnBasedModeProbe,
                        turnBasedModeEnabledAtMount,
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
                        turnBasedPersistedSettingUnchanged)
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
        }

        private void CaptureTargetCleanupState()
        {
            targetEntityRemoved = targetService != null && targetService.TargetEntityRemoved;
            targetRuntimeGroupRemoved = targetService != null && targetService.RuntimeGroupRemoved;
            targetRuntimeFactionRemoved = targetService != null && targetService.RuntimeFactionRemoved;
            targetDurabilityLeaseReleased = targetService != null && targetService.TargetDurabilityLeaseReleased;
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
            if (!IsTurnBasedRow || turnBasedModeProbe == null)
            {
                return;
            }

            if (turnBasedModeProbe.TemporaryDeliveryAttempted &&
                !turnBasedModeProbe.RestoreDeliveryCompleted)
            {
                turnBasedModeProbe.DispatchRestoreAndRestoreRawCache();
            }
            turnBasedModeProbe.Dispose();
            turnBasedPersistedSettingUnchanged = !turnBasedModeProbe.TemporaryDeliveryAttempted ||
                turnBasedModeProbe.PersistedValueUnchanged;
            turnBasedModeRestored = !CombatController.IsInTurnBasedCombat() &&
                (!turnBasedModeProbe.TemporaryDeliveryAttempted ||
                 turnBasedModeProbe.RestoreDeliveryCompleted) &&
                turnBasedPersistedSettingUnchanged;
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
            AwaitOutcome,
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
            public CombatResourceEvidence Resources { get; set; }
            public CombatCommandEvidence Command { get; set; }
            public CombatRuleEvidence Rules { get; set; }
            public CombatMovementEvidence Movement { get; set; }
            public CombatPoseEvidence Pose { get; set; }
            public CombatCleanupEvidence Cleanup { get; set; }
            public IReadOnlyList<string> Selection { get; set; }
            public int AssertionPassCount { get; set; }
            public int AssertionFailCount { get; set; }
            public IReadOnlyList<string> Errors { get; set; }
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
            public float GameDeltaTime { get; set; }
            public bool MemoryRemovedAtCleanup { get; set; }
            public NativeCombatJoinEvidence NativeJoin { get; set; }

            public static CombatEntryEvidence From(
                DiagnosticCombatEntryReadinessSnapshot readiness,
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

            public static TurnBasedCombatEvidence Capture(
                NativeModeTransitionProbe probe,
                bool enabledAtMount,
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
                bool persistedValueUnchanged)
            {
                return new TurnBasedCombatEvidence
                {
                    Requested = true,
                    OriginalEnabled = probe != null && probe.OriginalValue,
                    TemporaryEnabled = probe != null && probe.TemporaryValue,
                    OriginalRawCacheHadValue = probe != null && probe.OriginalRawCacheHadValue,
                    EnabledAtMount = enabledAtMount,
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
                    RestoreDeliveryCompleted = probe != null && probe.RestoreDeliveryCompleted,
                    ModeRestored = modeRestored,
                    PersistedValueUnchanged = persistedValueUnchanged
                };
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
