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
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UI._ConsoleUI.TurnBasedMode;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Phase 3D's Horse-only runtime tranche. Horse creation and final removal remain
    /// owned by HorseCompanionUnmountedScenarioEngine; this class owns only bounded
    /// mounted-combat observations and restores every target, equipment, mode, pause,
    /// selection, and command lease it acquires.
    /// </summary>
    internal sealed partial class Phase3dHorseScenarioTranche : IDisposable
    {
        internal const string RealTimeScenario = "phase3d-unified-combat-rt-suite";
        internal const string TurnBasedScenario = "phase3d-unified-combat-tb-suite";
        internal const string PresentationScenario = "phase3d-horse-presentation-suite";
        internal const string EvidenceFileName = "phase3d-horse-scenario-evidence.json";
        internal const string EvidenceKind = "phase3d-horse-scenario-evidence";

        private const double ScenarioDeadlineSeconds = 300.0d;
        private const double LeafDeadlineSeconds = 30.0d;
        private const double UnmountedHorseAiSettleTimeoutSeconds = 5.0d;
        private const int CombatMountSyntheticStartTurnRequestCount = 0;
        private const float TargetDistance = 6.0f;
        private const float LongRangeTargetDistance = 19.0f;
        private const float RangedVariantTargetDistance = 4.0f;
        private const float MovementTolerance = 0.8f;

        private static readonly FieldInfo AiBackingField = typeof(UnitEntityData).GetField(
            "m_AiEnabled",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CombatNextUnitField = typeof(CombatController).GetField(
            "m_NextUnit",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        private readonly RuntimeRequest request;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPlayerActionController playerAction;
        private readonly MountedCombatController combat;
        private readonly NativeMountedControlService nativeControls;
        private readonly HorseCompanionBlueprintService horseService;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData horse;
        private readonly Stopwatch clock = new Stopwatch();
        private readonly Stopwatch leafClock = new Stopwatch();
        private HorseMotionEvidenceRecorder motionEvidence;
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly List<TurnController> nativeTurnTraversalEndedTurns = new List<TurnController>();
        private readonly JArray rows = new JArray();
        private readonly JArray nativeTurnTraversalRosterEvidence = new JArray();
        private readonly JArray nativeTurnTraversalEntries = new JArray();
        private readonly JObject observations = new JObject();

        private Phase3dHorseStep step;
        private DiagnosticCombatTargetService targetService;
        private Phase3dCombatRuleProbe ruleProbe;
        private NativeModeTransitionProbe turnBasedModeProbe;
        private Phase3dRangedWeaponLease rangedWeaponLease;
        private ScopedDiagnosticAiLease<UnitEntityData> unmountedHorseAiLease;
        private ScopedDiagnosticAiLease<UnitEntityData> combatMountRiderAiLease;
        private WeaponCategory rangedVariantCategory;
        private UnitEntityData target;
        private UnitMoveTo combatMountAdjacencyCommand;
        private UnitMoveTo movementCommand;
        private UnitCommand unmountedCommand;
        private UnitUseAbility lastNativeAbilityShell;
        private UnifiedMountedTurnSnapshot explicitPrimaryLedgerBefore;
        private Vector3 movementStart;
        private Vector3 movementDestination;
        private Vector3 combatMountAdjacencyRiderStart;
        private Vector3 combatMountAdjacencyHorseStart;
        private Vector3 combatMountAdjacencyDestination;
        private JObject combatMountAdjacencyAdmission;
        private UnifiedMountedTurnSnapshot turnSnapshotBefore;
        private UnifiedMountedTurnSnapshot rtCombatDismountBefore;
        private long stockNativeBefore;
        private long stockIntentBefore;
        private long stockRiderBefore;
        private long stockMountBefore;
        private long stockCancelBefore;
        private long stockDuplicateBefore;
        private MountedPairAttackOutcome outcomeBefore;
        private long activationSequenceBefore;
        private int attackRulesBeforeCancel;
        private int nonOpportunityAttackRulesBeforeCancel;
        private int pairOpportunityAttackRulesBeforeCancel;
        private long stepSuppressionBefore;
        private float riderStandardBeforeMount;
        private float riderMoveBeforeMount;
        private float mountStandardBeforeMount;
        private float mountMoveBeforeMount;
        private int stableFrames;
        private int originalEquipmentSet;
        private bool originalPause;
        private bool originalUnsafeExperiment;
        private bool originalPairedCommandScheduler;
        private bool turnBasedPairInitiallyMounted;
        private UnitEntityData[] originalSelection;
        private bool started;
        private bool completed;
        private bool disposed;
        private bool cleanupStarted;
        private bool cleanupError;
        private bool transitionPrimaryDispatched;
        private bool transitionRiderTurnObserved;
        private int transitionRiderStartRequestCount;
        private int combatMountRiderTurnObservedFrames;
        private int combatMountActionableTurnObservedFrames;
        private int combatMountCurrentTurnMismatchFrames;
        private int combatMountTurnStatusBlockedFrames;
        private int combatMountRiderCommandBlockedFrames;
        private int combatMountHorseCommandBlockedFrames;
        private int combatMountRiderHandsBlockedFrames;
        private int combatMountRiderEquipmentBlockedFrames;
        private int combatMountWaitingForUiBlockedFrames;
        private int combatMountPendingNextUnitBlockedFrames;
        private int combatMountRiderAwakeBlockedFrames;
        private int combatMountRiderAwakeScheduleBlockedFrames;
        private int combatMountRiderUnitTickBlockedFrames;
        private int combatMountGameModeBlockedFrames;
        private int combatMountSelectionBlockedFrames;
        private int combatMountNauseatedBlockedFrames;
        private int combatMountNativeCommandAdmissionFrame = -1;
        private int combatMountNativeCommandStartObservedFrame = -1;
        private int combatMountNativeCommandTerminalObservedFrame = -1;
        private int combatMountNativeTickEncounterCount;
        private int combatMountNativeTickEligibleCount;
        private int combatMountNativeTickRejectedCount;
        private int combatMountNativeTickDuplicateFrameCount;
        private int combatMountNativeTickFirstFrame = -1;
        private int combatMountNativeTickLastFrame = -1;
        private int combatMountNativeTickFirstEligibleFrame = -1;
        private bool combatMountNativeTickLastStockEligible;
        private bool combatMountNativeTickLastWaitingForUi;
        private int combatMountNativeTickLastWaitingForUiGuardCount;
        private string combatMountNativeTickLastCurrentTurnUnitId;
        private string combatMountNativeTickLastCurrentTurnStatus;
        private string transitionFirstNativeTurnUnitId;
        private bool rangedMountMeleeReadyAtAdmission;
        private JObject rangedReadinessAtAdmission;
        private JObject stockMeleeReadinessAtAdmission;
        private string stockMeleePreviousTargetId;
        private bool stockMeleePreviousTargetCleanupPassed;
        private JObject rangedOpportunityReadinessAtAdmission;
        private JObject rangedVariantReadinessAtAdmission;
        private string rangedVariantPreviousTargetId;
        private bool rangedVariantPreviousTargetCleanupPassed;
        private string unmountedPreviousTargetId;
        private bool unmountedPreviousTargetCleanupPassed;
        private string unmountedMeleeTargetId;
        private bool unmountedMeleeTargetCleanupPassed;
        private bool unmountedHorseAiLeaseRestored = true;
        private bool unmountedHorseAiSettleRequested;
        private double unmountedHorseAiSettleStartedAtSeconds;
        private int unmountedHorseAiStableFrames;
        private string unmountedHorseAiLeaseError;
        private bool combatMountRiderAiLeaseRestored = true;
        private int combatMountRiderAiStableFrames;
        private string combatMountRiderAiLeaseError;
        private MountedPairAttackOutcome rangedRiderOutcome;
        private MountedPairAttackOutcome rangedRiderOutcomeBaseline;
        private bool targetCleanupComplete;
        private bool modeRestored;
        private int cleanupFrame;
        private int frame;
        private UnitEntityData[] nativeTurnTraversalRoster = new UnitEntityData[0];
        private TurnController nativeTurnTraversalCandidate;
        private string nativeTurnTraversalCandidatePurpose;
        private string nativeTurnTraversalPurpose;
        private string nativeTurnTraversalExpectedUnitId;
        private int nativeTurnTraversalStableFrames;
        private int nativeTurnTraversalRosterCaptureCount;
        private int nativeTurnTraversalForceEndCount;
        private int nativeTurnTraversalDuplicateTurnRejectCount;
        private int nativeTurnTraversalForeignTurnRejectCount;
        private int nativeTurnTraversalResourceMutationCount;
        private int nativeTurnTraversalMountedHorseTurnObservedCount;

        internal Phase3dHorseScenarioTranche(
            RuntimeRequest request,
            GameMountedRelationshipService relationship,
            MountedPlayerActionController playerAction,
            MountedCombatController combat,
            NativeMountedControlService nativeControls,
            HorseCompanionBlueprintService horseService,
            DiagnosticSettings settings,
            IModLogger logger,
            UnitEntityData rider,
            UnitEntityData horse)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.playerAction = playerAction ?? throw new ArgumentNullException(nameof(playerAction));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            this.nativeControls = nativeControls ?? throw new ArgumentNullException(nameof(nativeControls));
            this.horseService = horseService ?? throw new ArgumentNullException(nameof(horseService));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.horse = horse ?? throw new ArgumentNullException(nameof(horse));
        }

        internal static bool SupportsScenario(string scenario)
        {
            return string.Equals(scenario, RealTimeScenario, StringComparison.Ordinal) ||
                string.Equals(scenario, Phase3gRealTimeScenario, StringComparison.Ordinal) ||
                string.Equals(scenario, Phase3gTurnBasedScenario, StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3h-combat-loop-rt", StringComparison.Ordinal) ||
                string.Equals(scenario, "phase3h-combat-loop-tb", StringComparison.Ordinal) ||
                string.Equals(scenario, OrdinaryAttackControlsScenario, StringComparison.Ordinal) ||
                string.Equals(scenario, TurnBasedScenario, StringComparison.Ordinal) ||
                string.Equals(scenario, PresentationScenario, StringComparison.Ordinal);
        }

        internal bool IsCompleted => completed;

        private bool IsPhase3fNativeControlScope =>
            !string.Equals(request.Scenario, TurnBasedScenario, StringComparison.Ordinal) &&
            !settings.EnableUnifiedMountedTurn && !settings.EnablePairedCommandScheduler;

        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;

        internal IReadOnlyList<string> Errors => errors;

        internal JObject EvidenceSummary => new JObject
        {
            ["scenario"] = request.Scenario,
            ["status"] = errors.Count == 0 && results.Count != 0 &&
                results.All(item => string.Equals(item.Status, "PASS", StringComparison.Ordinal))
                ? "PASS"
                : "FAIL",
            ["rows"] = rows.DeepClone(),
            ["observations"] = observations.DeepClone()
        };

        internal void Start(bool pairAlreadyMounted)
        {
            ThrowIfDisposed();
            if (started)
            {
                throw new InvalidOperationException("Phase 3D Horse tranche already started.");
            }
            if (!SupportsScenario(request.Scenario))
            {
                throw new InvalidOperationException("Scenario is outside the Phase 3D Horse tranche allowlist.");
            }

            var game = Game.Instance;
            if (game == null || SelectionManager.Instance == null || rider.Body == null || horse.Body == null)
            {
                throw new InvalidOperationException("Loaded game, selection, and exact Horse pair bodies are required.");
            }
            if (!pairAlreadyMounted)
            {
                throw new InvalidOperationException(
                    "Phase 3D Horse qualification requires the exact Horse pair already mounted through the parent native out-of-combat flow.");
            }

            originalPause = game.IsPaused;
            originalUnsafeExperiment = settings.EnableUnsafeMovementExperiment;
            originalPairedCommandScheduler = settings.EnablePairedCommandScheduler;
            turnBasedPairInitiallyMounted = string.Equals(
                request.Scenario,
                TurnBasedScenario,
                StringComparison.Ordinal) && pairAlreadyMounted;
            originalSelection = SelectionManager.Instance.SelectedUnits.Where(item => item != null).ToArray();
            originalEquipmentSet = rider.Body.CurrentHandEquipmentSetIndex;
            settings.EnableUnsafeMovementExperiment = true;
            if (string.Equals(request.Scenario, TurnBasedScenario, StringComparison.Ordinal))
            {
                settings.EnablePairedCommandScheduler = true;
            }
            nativeControls.Update();
            ruleProbe = new Phase3dCombatRuleProbe(rider, horse);
            started = true;
            clock.Start();
            leafClock.Start();

            if (!string.Equals(request.Scenario, TurnBasedScenario, StringComparison.Ordinal))
            {
                motionEvidence = new HorseMotionEvidenceRecorder(request.EvidenceRoot, relationship, logger);
            }

            observations["riderId"] = rider.UniqueId;
            observations["horseId"] = horse.UniqueId;
            observations["initialRelationshipState"] = relationship.State.ToString();
            observations["initialTurnBased"] = CombatController.IsInTurnBasedCombat();
            observations["phase3fActualConfiguration"] = new JObject
            {
                ["enableUnifiedMountedTurn"] = settings.EnableUnifiedMountedTurn,
                ["enablePairedCommandScheduler"] = settings.EnablePairedCommandScheduler,
                ["enableDiagnosticOverlay"] = settings.EnableDiagnosticOverlay,
                ["overlayPresent"] = playerAction.OverlayPresent
            };
            observations["initialSelection"] = new JArray(originalSelection.Select(item => item.UniqueId));

            if (IsOrdinaryAttackControls)
            {
                BeginOrdinaryAttackControls();
                return;
            }
            if (IsPhase3gControls)
            {
                if (Phase3gTurnBased) { BeginPhase3gCase(); }
                else { step = Phase3dHorseStep.Phase3gControls; }
                return;
            }

            if (string.Equals(request.Scenario, PresentationScenario, StringComparison.Ordinal))
            {
                step = Phase3dHorseStep.PresentationSettle;
                return;
            }

            if (string.Equals(request.Scenario, TurnBasedScenario, StringComparison.Ordinal))
            {
                BeginCombatMountHorseAiIsolation();
                return;
            }

            BeginTarget(TargetDistance, "rt-stock-melee");
            step = Phase3dHorseStep.AwaitMountedCombat;
        }

        internal void Update()
        {
            ThrowIfDisposed();
            if (!started || completed)
            {
                return;
            }

            frame++;
            try
            {
                motionEvidence?.Tick(cleanupStarted ? null : IsPhase3gControls
                    ? (phase3gStage == 1 ? Phase3gRow +
                        (horse.Commands.Standard is MountedPairAttackCommand horseAttack
                            ? horseAttack.NativeSequenceStarted ? "-horse-strike-recovery" : "-horse-approach"
                            : "-rider") : null) : step.ToString());
                targetService?.ObserveTargetLifeState();
                targetService?.RefreshBidirectionalCombatMemoryLease();
                var scenarioBudget = IsOrdinaryAttackControls ? OrdinaryScenarioDeadlineSeconds : ScenarioDeadlineSeconds;
                if (!cleanupStarted && clock.Elapsed.TotalSeconds > scenarioBudget)
                {
                    FailCurrent("phase3d-horse-scenario-deadline", "Horse tranche exceeded its " + scenarioBudget + " second scenario budget at " + step + ".");
                    BeginCleanup();
                }
                else if (!cleanupStarted && leafClock.Elapsed.TotalSeconds > LeafDeadlineSeconds)
                {
                    observations["leafDeadlineProgress"] = CaptureLeafDeadlineProgress();
                    FailCurrent("phase3d-horse-leaf-deadline", "Phase 3D Horse tranche leaf exceeded 30 seconds at " + step + ".");
                    BeginCleanup();
                }

                switch (step)
                {
                    case Phase3dHorseStep.Phase3gControls:
                        if (IsOrdinaryAttackControls) TickOrdinaryAttackControls();
                        else TickPhase3gControls();
                        break;
                    case Phase3dHorseStep.PresentationSettle:
                        ObservePresentation();
                        break;
                    case Phase3dHorseStep.AwaitMountedCombat:
                        AwaitMountedCombat();
                        break;
                    case Phase3dHorseStep.AwaitRiderPrimaryRt:
                        AwaitRiderPrimaryRt();
                        break;
                    case Phase3dHorseStep.AwaitMountPrimaryRtAdmission:
                        AwaitMountPrimaryRtAdmission();
                        break;
                    case Phase3dHorseStep.AwaitMountPrimaryRt:
                        AwaitMountPrimaryRt();
                        break;
                    case Phase3dHorseStep.AwaitStockMeleeTargetCleanupRt:
                        AwaitStockMeleeTargetCleanupRt();
                        break;
                    case Phase3dHorseStep.AwaitStockMeleeAdmissionRt:
                        AwaitStockMeleeAdmissionRt();
                        break;
                    case Phase3dHorseStep.AwaitStockMeleeRt:
                        AwaitStockMeleeRt();
                        break;
                    case Phase3dHorseStep.AwaitStockCancelRt:
                        AwaitStockCancelRt();
                        break;
                    case Phase3dHorseStep.AwaitRangedTargetCleanup:
                        AwaitRangedTargetCleanup();
                        break;
                    case Phase3dHorseStep.AwaitRangedCombat:
                        AwaitRangedCombat();
                        break;
                    case Phase3dHorseStep.AwaitRangedAttackRt:
                        AwaitRangedAttackRt();
                        break;
                    case Phase3dHorseStep.AwaitRangedCancelRt:
                        AwaitRangedCancelRt();
                        break;
                    case Phase3dHorseStep.AwaitRangedAdjacentMoveRt:
                        AwaitRangedAdjacentMoveRt();
                        break;
                    case Phase3dHorseStep.AwaitRangedAdjacentAttackRt:
                        AwaitRangedAdjacentAttackRt();
                        break;
                    case Phase3dHorseStep.AwaitRangedAdjacentCancelRt:
                        AwaitRangedAdjacentCancelRt();
                        break;
                    case Phase3dHorseStep.AwaitRangedVariantTargetCleanupRt:
                        AwaitRangedVariantTargetCleanupRt();
                        break;
                    case Phase3dHorseStep.AwaitRangedVariantAdmissionRt:
                        AwaitRangedVariantAdmissionRt();
                        break;
                    case Phase3dHorseStep.AwaitRangedVariantRt:
                        AwaitRangedVariantRt();
                        break;
                    case Phase3dHorseStep.AwaitRangedVariantCancelRt:
                        AwaitRangedVariantCancelRt();
                        break;
                    case Phase3dHorseStep.AwaitRtToTbTransition:
                        AwaitRtToTbTransition();
                        break;
                    case Phase3dHorseStep.AwaitRiderPrimaryAfterTransition:
                        AwaitRiderPrimaryAfterTransition();
                        break;
                    case Phase3dHorseStep.AwaitTbToRtTransition:
                        AwaitTbToRtTransition();
                        break;
                    case Phase3dHorseStep.AwaitRtCombatDismountAdmission:
                        AwaitRtCombatDismountAdmission();
                        break;
                    case Phase3dHorseStep.AwaitRtCombatDismount:
                        AwaitRtCombatDismount();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedHorseAiIsolation:
                        AwaitUnmountedHorseAiIsolation();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedTargetCleanupRt:
                        AwaitUnmountedTargetCleanupRt();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedMeleeRt:
                        AwaitUnmountedMeleeRt();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedMeleeCancelRt:
                        AwaitUnmountedMeleeCancelRt();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedRangedTargetCleanupRt:
                        AwaitUnmountedRangedTargetCleanupRt();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedRangedAdmissionRt:
                        AwaitUnmountedRangedAdmissionRt();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedRangedRt:
                        AwaitUnmountedRangedRt();
                        break;
                    case Phase3dHorseStep.AwaitCombatMountAdjacencyReadiness:
                        AwaitCombatMountAdjacencyReadiness();
                        break;
                    case Phase3dHorseStep.AwaitCombatMountAdjacencyMove:
                        AwaitCombatMountAdjacencyMove();
                        break;
                    case Phase3dHorseStep.AwaitCombatMountHorseAiIsolation:
                        AwaitCombatMountHorseAiIsolation();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedCombat:
                        AwaitUnmountedCombat();
                        break;
                    case Phase3dHorseStep.AwaitTurnBasedMode:
                        AwaitTurnBasedMode();
                        break;
                    case Phase3dHorseStep.AwaitRiderTurnForMount:
                        AwaitRiderTurnForMount();
                        break;
                    case Phase3dHorseStep.AwaitCombatMount:
                        AwaitCombatMount();
                        break;
                    case Phase3dHorseStep.AwaitRiderPrimaryTb:
                        AwaitRiderPrimaryTb();
                        break;
                    case Phase3dHorseStep.AwaitNextRiderTurnForMountPrimaryTb:
                        AwaitNextRiderTurnForMountPrimaryTb();
                        break;
                    case Phase3dHorseStep.AwaitMountPrimaryTb:
                        AwaitMountPrimaryTb();
                        break;
                    case Phase3dHorseStep.AwaitNextRiderTurnForStockMeleeTb:
                        AwaitNextRiderTurnForStockMeleeTb();
                        break;
                    case Phase3dHorseStep.AwaitStockMeleeTb:
                        AwaitStockMeleeTb();
                        break;
                    case Phase3dHorseStep.AwaitNextRiderTurnForRangedTb:
                        AwaitNextRiderTurnForRangedTb();
                        break;
                    case Phase3dHorseStep.AwaitRangedTb:
                        AwaitRangedTb();
                        break;
                    case Phase3dHorseStep.AwaitNextRiderTurnForStep:
                        AwaitNextRiderTurnForStep();
                        break;
                    case Phase3dHorseStep.AwaitFiveFootStep:
                        AwaitFiveFootStep();
                        break;
                    case Phase3dHorseStep.AwaitNextRiderTurnForOrdinaryMove:
                        AwaitNextRiderTurnForOrdinaryMove();
                        break;
                    case Phase3dHorseStep.AwaitOrdinaryMove:
                        AwaitOrdinaryMove();
                        break;
                    case Phase3dHorseStep.AwaitNextRiderTurnForDismount:
                        AwaitNextRiderTurnForDismount();
                        break;
                    case Phase3dHorseStep.AwaitCombatDismount:
                        AwaitCombatDismount();
                        break;
                    case Phase3dHorseStep.AwaitNextRiderTurnForUnmountedStep:
                        AwaitNextRiderTurnForUnmountedStep();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedStep:
                        AwaitUnmountedStep();
                        break;
                    case Phase3dHorseStep.AwaitRiderSpentAttackTb:
                        AwaitRiderSpentAttackTb();
                        break;
                    case Phase3dHorseStep.AwaitRiderSpentCombatMount:
                        AwaitRiderSpentCombatMount();
                        break;
                    case Phase3dHorseStep.AwaitNextRiderTurnForSpentControlDismount:
                        AwaitNextRiderTurnForSpentControlDismount();
                        break;
                    case Phase3dHorseStep.AwaitSpentControlDismount:
                        AwaitSpentControlDismount();
                        break;
                    case Phase3dHorseStep.AwaitHorseTurnForSpentControl:
                        AwaitHorseTurnForSpentControl();
                        break;
                    case Phase3dHorseStep.AwaitHorseSpentAttackTb:
                        AwaitHorseSpentAttackTb();
                        break;
                    case Phase3dHorseStep.AwaitRiderTurnForMountSpentControl:
                        AwaitRiderTurnForMountSpentControl();
                        break;
                    case Phase3dHorseStep.AwaitMountSpentCombatMount:
                        AwaitMountSpentCombatMount();
                        break;
                    case Phase3dHorseStep.AwaitCleanup:
                        AwaitCleanup();
                        break;
                }
            }
            catch (Exception exception)
            {
                logger.Exception("Phase 3D Horse tranche", exception);
                FailCurrent("phase3d-horse-runtime-exception", exception.GetType().Name + ": " + exception.Message);
                BeginCleanup();
            }
        }

        internal void ObserveNativeTurnBasedCommandEligibility(UnitCommand command, bool stockEligible)
        {
            if (step != Phase3dHorseStep.AwaitCombatMount || lastNativeAbilityShell == null ||
                !ReferenceEquals(command, lastNativeAbilityShell))
            {
                return;
            }

            var observedFrame = Time.frameCount;
            combatMountNativeTickEncounterCount++;
            if (combatMountNativeTickFirstFrame < 0)
            {
                combatMountNativeTickFirstFrame = observedFrame;
            }
            if (combatMountNativeTickLastFrame == observedFrame)
            {
                combatMountNativeTickDuplicateFrameCount++;
            }
            combatMountNativeTickLastFrame = observedFrame;
            combatMountNativeTickLastStockEligible = stockEligible;
            if (stockEligible)
            {
                combatMountNativeTickEligibleCount++;
                if (combatMountNativeTickFirstEligibleFrame < 0)
                {
                    combatMountNativeTickFirstEligibleFrame = observedFrame;
                }
            }
            else
            {
                combatMountNativeTickRejectedCount++;
            }

            var controller = Game.Instance?.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            combatMountNativeTickLastWaitingForUi = controller != null &&
                (bool)controller.WaitingForUI;
            combatMountNativeTickLastWaitingForUiGuardCount =
                controller?.WaitingForUI?.GuardCount ?? -1;
            combatMountNativeTickLastCurrentTurnUnitId = turn?.Unit?.UniqueId;
            combatMountNativeTickLastCurrentTurnStatus = turn?.Status.ToString();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            if (!completed)
            {
                BestEffortCleanup();
            }
            ruleProbe?.Dispose();
            ruleProbe = null;
            motionEvidence?.Dispose();
            disposed = true;
        }

        private void ObservePresentation()
        {
            var runtime = relationship.Runtime;
            if (!runtime.PoseFrameApplied || runtime.PoseApplicationFrameCount < 5)
            {
                return;
            }

            nativeControls.Update();
            var controls = nativeControls.CaptureSnapshot();
            var portrait = horse.Portrait;
            var small = portrait?.SmallPortrait;
            var saddle = horseService.MountSaddleIcon;
            var dismountSaddle = horseService.DismountSaddleIcon;
            var horseIcon = horseService.HorseIcon;
            var mountAbility = nativeControls.MountAbility;
            var dismountAbility = nativeControls.DismountAbility;
            var presentation = relationship.CapturePresentationObservation();
            observations["presentation"] = presentation;
            observations["smallPortrait"] = SpriteEvidence(small);
            observations["horseIdentityIcon"] = SpriteEvidence(horseIcon);
            observations["saddleIcon"] = SpriteEvidence(saddle);
            observations["dismountSaddleIcon"] = SpriteEvidence(dismountSaddle);
            var horsePose = SupportedMountedProfiles.Horse.RiderPoseProfile;
            observations["pelvisOffset"] = PoseVectorEvidence(horsePose.PelvisPositionOffset);
            observations["mountRootPositionOffset"] = PoseVectorEvidence(
                SupportedMountedProfiles.Horse.MountRootPositionOffset);

            AddRow(
                "Horse-small-portrait-close-up",
                small != null && small.texture != null && small.texture.width == 185 && small.texture.height == 242 &&
                    !ReferenceEquals(small, horseIcon),
                "The exact runtime Horse small portrait is the KMC 185x242 close-up and remains distinct from the companion identity icon.",
                new JObject { ["sprite"] = SpriteEvidence(small) });
            AddRow(
                "saddle-icon",
                saddle != null && saddle.texture != null && saddle.texture.width == 96 && saddle.texture.height == 96 &&
                    dismountSaddle != null && dismountSaddle.texture != null &&
                    dismountSaddle.texture.width == 96 && dismountSaddle.texture.height == 96 &&
                    !ReferenceEquals(saddle, dismountSaddle) && !ReferenceEquals(dismountSaddle, horseIcon) &&
                    !ReferenceEquals(saddle, horseIcon) && ReferenceEquals(mountAbility?.Icon, saddle) &&
                    ReferenceEquals(dismountAbility?.Icon, dismountSaddle),
                "Mount and Dismount bind distinct original 96x96 KMC saddle sprites; actual action-bar readability remains a human gate.",
                new JObject { ["mountSprite"] = SpriteEvidence(saddle), ["dismountSprite"] = SpriteEvidence(dismountSaddle) });
            AddRow(
                "Horse-pose-final-idle-walk-run-turn-stop",
                relationship.State == RelationshipState.Mounted && runtime.PoseHealthy && runtime.PoseFrameApplied &&
                    string.Equals(runtime.MountProfileId, SupportedMountedProfiles.Horse.Id, StringComparison.Ordinal) &&
                    Math.Abs(horsePose.PelvisPositionOffset.Y - (-0.17d)) <= 0.0001d &&
                    Math.Abs(SupportedMountedProfiles.Horse.MountRootPositionOffset.Y - (-0.08d)) <= 0.0001d &&
                    SupportedMountedProfiles.Mammoth.MountRootPositionOffset.Magnitude <= 0.0001f &&
                    runtime.PoseFootTargetClampCount == 0 &&
                    runtime.PoseMaximumSegmentLengthResidualWorldUnits <= 0.0001d,
                "The Horse mechanics anchor remains -0.08m; visual-only animated projection adds 0.08m downward in mount-root up and retains the -0.18m backward correction. Visual contact remains a manual gate.",
                new JObject { ["observation"] = presentation });
            AddRow(
                "mounted-single-rider-turn-portrait",
                SelectionManager.Instance.SelectedUnits.Count == 1 &&
                    SelectionManager.Instance.SelectedUnits[0] == rider &&
                    controls.DuplicateFactCount == 0 && controls.ManagedHotbarSlotCount == 0,
                "Rider remains the sole selected principal with drawer-only KMC controls; exact turn tracker is proven in the TB tranche.",
                JObject.FromObject(controls, JsonSerializer.Create(JsonSettings)));

            BeginCleanup();
        }

        private static JObject SpriteEvidence(Sprite sprite)
        {
            return new JObject
            {
                ["present"] = sprite != null,
                ["name"] = sprite?.name,
                ["textureWidth"] = sprite?.texture?.width,
                ["textureHeight"] = sprite?.texture?.height,
                ["rectWidth"] = sprite?.rect.width,
                ["rectHeight"] = sprite?.rect.height
            };
        }

        private static JObject PoseVectorEvidence(PoseVector3 value)
        {
            return new JObject
            {
                ["x"] = value.X,
                ["y"] = value.Y,
                ["z"] = value.Z
            };
        }

        private void BeginTarget(float distance, string suffix)
        {
            if (targetService != null)
            {
                throw new InvalidOperationException("Phase 3D target lease is already active.");
            }
            targetService = new DiagnosticCombatTargetService(logger);
            var point = FindWalkablePoint(rider.Position, distance, distance >= 10f ? 1.0f : 0.5f);
            target = targetService.Spawn(rider, horse, point, request.RunId + "-" + suffix, true, true);
            if (!targetService.PrepareForPlayerClick(target) ||
                !targetService.QueueBidirectionalCombatMemory(rider, target))
            {
                throw new InvalidOperationException("Phase 3D hostile target visibility/combat-memory lease failed.");
            }
            ruleProbe.Arm(target, true);
            observations["target-" + suffix] = new JObject
            {
                ["targetId"] = target.UniqueId,
                ["distance"] = rider.DistanceTo(target),
                ["bidirectionalHostility"] = targetService.BidirectionalHostilityVerified,
                ["noLoot"] = targetService.TargetHasNoLoot,
                ["durabilityLease"] = targetService.TargetDurabilityLeaseAcquired
            };
            ResetLeafClock();
        }

        private void AwaitMountedCombat()
        {
            if (Game.Instance.IsPaused)
            {
                Game.Instance.IsPaused = false;
            }
            if (!IsCombatReady(true) ||
                !IsExactRealTimePrimaryAdmissionReady(
                    NativeMountedControlKind.RiderPrimary,
                    rider,
                    "rtRiderPrimaryReadiness"))
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }
            stableFrames = 0;

            RunRiderPrimaryCancelAndRejectionControls();
            outcomeBefore = combat.LastOutcome;
            activationSequenceBefore = nativeControls.SnapshotAbilityActivations()
                .Select(item => item.Sequence)
                .DefaultIfEmpty(0L)
                .Max();
            ruleProbe.Arm(target, true);
            movementStart = horse.Position;
            explicitPrimaryLedgerBefore = combat.CaptureUnifiedTurnSnapshot();
            var expectedDispatchStarted = targetService.BeginExpectedAttackDispatch(target);
            var clicked = TryNativeAbilityTargetClick(nativeControls.RiderPrimaryAbility, target, "rt-rider-primary");
            if (!expectedDispatchStarted || !clicked)
            {
                FailCurrent("rider-primary-does-not-dismount-rt", "Native Rider Primary expected-dispatch boundary or target click was not admitted.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRiderPrimaryRt;
            ResetLeafClock();
        }

        private void RunRiderPrimaryCancelAndRejectionControls()
        {
            var relationshipBefore = relationship.State;
            var snapshotBefore = nativeControls.CaptureSnapshot();
            var handler = Game.Instance.SelectedAbilityHandler;
            var data = rider.Descriptor.Abilities.GetAbility(nativeControls.RiderPrimaryAbility)?.Data;
            if (handler == null || data == null)
            {
                AddRow("rider-primary-target-cancel-does-not-dismount", false,
                    "Native selected-ability handler or Rider Primary fact was unavailable.", null);
            }
            else
            {
                handler.SetAbility(data);
                handler.DropAbility();
                var after = nativeControls.CaptureSnapshot();
                AddRow(
                    "rider-primary-target-cancel-does-not-dismount",
                    relationshipBefore == RelationshipState.Mounted && relationship.State == RelationshipState.Mounted &&
                        after.RiderPrimaryRelationshipEndCount == snapshotBefore.RiderPrimaryRelationshipEndCount,
                    "Starting then cancelling exact native Rider Primary target selection retained the pair with no relationship-end activation record.",
                    JObject.FromObject(after, JsonSerializer.Create(JsonSettings)));
            }

            snapshotBefore = nativeControls.CaptureSnapshot();
            var rejected = TryNativeAbilityTargetClick(nativeControls.RiderPrimaryAbility, horse, "rt-rider-primary-rejected");
            var rejectedAfter = nativeControls.CaptureSnapshot();
            AddRow(
                "rider-primary-rejection-does-not-dismount",
                !rejected && relationship.State == RelationshipState.Mounted &&
                    rejectedAfter.RiderPrimaryRelationshipEndCount == snapshotBefore.RiderPrimaryRelationshipEndCount,
                "A native Rider Primary click against the friendly Horse was rejected without relationship cleanup.",
                JObject.FromObject(rejectedAfter, JsonSerializer.Create(JsonSettings)));
        }

        private void AwaitRiderPrimaryRt()
        {
            observations["rtRiderPrimaryShellLatest"] = CaptureNativeAbilityShell(lastNativeAbilityShell);
            if (combat.HasActiveCommand || ReferenceEquals(combat.LastOutcome, outcomeBefore))
            {
                return;
            }

            var outcome = combat.LastOutcome;
            var activations = nativeControls.SnapshotAbilityActivations()
                .Where(item => item.Sequence > activationSequenceBefore && item.Kind == NativeMountedControlKind.RiderPrimary)
                .ToArray();
            var movementDistance = HorizontalDistance(movementStart, horse.Position);
            var retained = relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == horse &&
                activations.Length > 0 &&
                activations.All(item => !item.RelationshipEnded && !item.CleanupTrigger.HasValue);
            var evidence = CaptureOutcome(outcome, activations);
            evidence["horseMovementDistance"] = movementDistance;
            evidence["admissionReadiness"] = observations["rtRiderPrimaryReadiness"]?.DeepClone();
            evidence["nativeInput"] = observations["rt-rider-primary"]?.DeepClone();
            evidence["nativeShellTerminal"] = CaptureNativeAbilityShell(lastNativeAbilityShell);
            evidence["ledgerBefore"] = JObject.FromObject(
                explicitPrimaryLedgerBefore, JsonSerializer.Create(JsonSettings));
            var ledgerAfter = combat.CaptureUnifiedTurnSnapshot();
            evidence["ledgerAfter"] = JObject.FromObject(
                ledgerAfter, JsonSerializer.Create(JsonSettings));
            AddRow(
                "rider-primary-does-not-dismount-rt",
                retained && outcome != null && outcome.Action == MountedCombatActionKind.RiderMelee &&
                    outcome.ChildAttackStartCount == 1 && outcome.ActionStandardCharged &&
                    ledgerAfter.Rider.Move <= explicitPrimaryLedgerBefore.Rider.Move + 0.001f,
                "One admitted native RT Rider Primary reached one terminal rider attack and retained the exact pair.",
                evidence);
            AddRow(
                "rider-primary-after-movement-does-not-dismount",
                retained && outcome != null && movementDistance > 0.25f && relationship.Runtime.PoseHealthy,
                "Rider Primary terminal handling retained the relationship and pose after its bounded approach/movement evaluation.",
                evidence);

            if (!retained)
            {
                BeginCleanup();
                return;
            }

            step = Phase3dHorseStep.AwaitMountPrimaryRtAdmission;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitMountPrimaryRtAdmission()
        {
            if (!IsExactRealTimePrimaryAdmissionReady(
                    NativeMountedControlKind.MountPrimary,
                    horse,
                    "rtMountPrimaryReadiness"))
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            stableFrames = 0;
            BeginMountPrimaryRt();
        }

        private void BeginMountPrimaryRt()
        {
            outcomeBefore = combat.LastOutcome;
            ruleProbe.Arm(target, true);
            explicitPrimaryLedgerBefore = combat.CaptureUnifiedTurnSnapshot();
            var expectedDispatchStarted = targetService.BeginExpectedAttackDispatch(target);
            var clicked = TryNativeAbilityTargetClick(nativeControls.MountPrimaryAbility, target, "rt-mount-primary");
            if (!expectedDispatchStarted || !clicked)
            {
                FailCurrent("mounted-stock-click-melee-mount-only-explicit", "Native RT Mount Primary expected-dispatch boundary or target click was not admitted.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitMountPrimaryRt;
            ResetLeafClock();
        }

        private void AwaitMountPrimaryRt()
        {
            if (combat.HasActiveCommand || ReferenceEquals(combat.LastOutcome, outcomeBefore))
            {
                return;
            }

            var outcome = combat.LastOutcome;
            var retained = relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == horse;
            var evidence = CaptureOutcome(outcome, new NativeMountedAbilityActivationRecord[0]);
            evidence["admissionReadiness"] = observations["rtMountPrimaryReadiness"]?.DeepClone();
            evidence["nativeInput"] = observations["rt-mount-primary"]?.DeepClone();
            evidence["nativeShellTerminal"] = CaptureNativeAbilityShell(lastNativeAbilityShell);
            evidence["ledgerBefore"] = JObject.FromObject(
                explicitPrimaryLedgerBefore, JsonSerializer.Create(JsonSettings));
            var ledgerAfter = combat.CaptureUnifiedTurnSnapshot();
            evidence["ledgerAfter"] = JObject.FromObject(
                ledgerAfter, JsonSerializer.Create(JsonSettings));
            AddRow(
                "mounted-stock-click-melee-mount-only-explicit",
                retained && outcome != null && outcome.Action == MountedCombatActionKind.MountPrimaryNatural &&
                    outcome.ResourceOwnerId == horse.UniqueId && ruleProbe.RiderAttackRuleCount == 0 &&
                    ruleProbe.MountAttackRuleCount == 1 && outcome.ActionStandardCharged &&
                    ledgerAfter.Rider.Move <= explicitPrimaryLedgerBefore.Rider.Move + 0.001f,
                "Explicit RT Mount Primary spent only the Horse attack ledger and retained the exact pair.",
                evidence);
            if (!retained)
            {
                BeginCleanup();
                return;
            }

            BeginStockMeleeTargetReplacement();
        }

        private void BeginStockMeleeTargetReplacement()
        {
            stockMeleePreviousTargetId = target?.UniqueId;
            stockMeleePreviousTargetCleanupPassed = false;
            stockMeleeReadinessAtAdmission = null;
            combat.Cancel("Phase 3D stock melee isolated target boundary");
            TryLeaveCombat(target);
            targetCleanupComplete = targetService != null && targetService.DestroyAndVerify();
            step = Phase3dHorseStep.AwaitStockMeleeTargetCleanupRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitStockMeleeTargetCleanupRt()
        {
            if (!targetCleanupComplete && targetService != null)
            {
                targetCleanupComplete = targetService.DestroyAndVerify();
            }
            if (!targetCleanupComplete)
            {
                return;
            }

            targetService.Dispose();
            targetService = null;
            target = null;
            stockMeleePreviousTargetCleanupPassed = true;
            BeginTarget(TargetDistance, "rt-stock-melee-persistent");
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            step = Phase3dHorseStep.AwaitStockMeleeAdmissionRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitStockMeleeAdmissionRt()
        {
            var readiness = CaptureStockMeleeReadiness();
            observations["stockMeleeRtReadiness"] = readiness;
            if (!(bool)readiness["ready"])
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            stockMeleeReadinessAtAdmission = readiness.DeepClone() as JObject;
            stableFrames = 0;
            BeginStockMeleeRt();
        }

        private void BeginStockMeleeRt()
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            combat.Cancel("Phase 3D stock melee boundary");
            ruleProbe.Arm(target, true);
            movementStart = horse.Position;
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            stockRiderBefore = combat.StockAttackRiderDispatchCount;
            stockMountBefore = combat.StockAttackMountDispatchCount;
            stockCancelBefore = combat.StockAttackIntentCancelCount;
            stockDuplicateBefore = combat.StockAttackDuplicateDispatchCount;
            var expectedDispatchStarted = targetService.BeginExpectedAttackDispatch(target);
            var clicked = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            var nativeRequestDelta = combat.StockAttackNativeRequestCount - stockNativeBefore;
            var intentStartDelta = combat.StockAttackIntentStartCount - stockIntentBefore;
            observations["stockMeleeRtAdmission"] = new JObject
            {
                ["clicked"] = clicked,
                ["expectedDispatchStarted"] = expectedDispatchStarted,
                ["nativeRequestDelta"] = nativeRequestDelta,
                ["intentStartDelta"] = intentStartDelta,
                ["targetId"] = target.UniqueId,
                ["lastObservation"] = combat.LastStockAttackObservation,
                ["feedback"] = combat.LastFeedback
            };
            if (!clicked || !expectedDispatchStarted || nativeRequestDelta != 1 || intentStartDelta != 1)
            {
                FailCurrent("mounted-stock-click-melee-adjacent-rt", "Ordinary native hostile click did not admit the isolated, readiness-proven mounted stock melee intent.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitStockMeleeRt;
            ResetLeafClock();
        }

        private void AwaitStockMeleeRt()
        {
            var riderDispatches = combat.StockAttackRiderDispatchCount - stockRiderBefore;
            var mountDispatches = combat.StockAttackMountDispatchCount - stockMountBefore;
            if (riderDispatches < 2 || mountDispatches < 1 ||
                ruleProbe.RiderAttackRuleCount < 2 || ruleProbe.MountAttackRuleCount < 1)
            {
                if (target == null || !target.IsInState || !target.Descriptor.State.IsConscious)
                {
                    FailCurrent("mounted-stock-click-melee-auto-repeat-rt", "The isolated stock-melee target became invalid before two rider attacks and one separately owned Horse primary completed.");
                    BeginCleanup();
                }
                return;
            }

            var exactNative = combat.StockAttackNativeRequestCount - stockNativeBefore == 1 &&
                combat.StockAttackIntentStartCount - stockIntentBefore == 1;
            var zeroDuplicates = combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore == 0;
            var horseMovementDistance = HorizontalDistance(movementStart, horse.Position);
            var evidence = CaptureStockEvidence();
            evidence["admissionReadiness"] = stockMeleeReadinessAtAdmission?.DeepClone();
            evidence["input"] = observations["stockMeleeRtAdmission"]?.DeepClone();
            evidence["previousTargetId"] = stockMeleePreviousTargetId;
            evidence["previousTargetCleanupPassed"] = stockMeleePreviousTargetCleanupPassed;
            evidence["isolatedTargetId"] = target.UniqueId;
            evidence["horseMovementDistanceAfterAdmission"] = horseMovementDistance;
            AddRow("mounted-stock-click-melee-adjacent-rt",
                exactNative && riderDispatches >= 2 && ruleProbe.RiderAttackRuleCount >= 2 &&
                    stockMeleePreviousTargetCleanupPassed &&
                    !string.Equals(stockMeleePreviousTargetId, target.UniqueId, StringComparison.Ordinal),
                "One ordinary native hostile click admitted rider-principal mounted melee.", evidence);
            AddRow("mounted-stock-click-melee-approach-rt",
                relationship.State == RelationshipState.Mounted && relationship.Runtime.PoseHealthy &&
                    horseMovementDistance > 0.25f && ruleProbe.RiderAttackRuleCount >= 2 &&
                    ruleProbe.MountAttackRuleCount >= 1,
                "The pair retained mount-owned physical approach and rider-owned attack semantics.", evidence);
            AddRow("mounted-stock-click-melee-auto-repeat-rt",
                riderDispatches >= 2 && mountDispatches >= 1 && combat.HasStockAttackIntent &&
                    ruleProbe.RiderAttackRuleCount >= 2 && ruleProbe.MountAttackRuleCount >= 1 && zeroDuplicates,
                "One hostile click remained active through two rider dispatch admissions and one completed separately owned Horse primary dispatch.", evidence);
            AddRow("mounted-separate-action-ledgers",
                combat.CaptureUnifiedTurnSnapshot().Rider != null &&
                    combat.CaptureUnifiedTurnSnapshot().Mount != null &&
                    combat.CaptureUnifiedTurnSnapshot().Rider.UnitId == rider.UniqueId &&
                    combat.CaptureUnifiedTurnSnapshot().Mount.UnitId == horse.UniqueId &&
                    !ReferenceEquals(
                        combat.CaptureUnifiedTurnSnapshot().Rider,
                        combat.CaptureUnifiedTurnSnapshot().Mount),
                "Rider and Horse remain represented by separate native cooldown ledgers during RT intent.",
                JObject.FromObject(combat.CaptureUnifiedTurnSnapshot(), JsonSerializer.Create(JsonSettings)));
            AddRow("mounted-stock-click-melee-rider-only-explicit",
                results.Any(item => string.Equals(item.Name, "rider-primary-does-not-dismount-rt", StringComparison.Ordinal) &&
                    string.Equals(item.Status, "PASS", StringComparison.Ordinal)),
                "Native Rider Primary spent only the rider attack before stock pair intent.", null);
            attackRulesBeforeCancel = ruleProbe.PairAttackRuleCount;
            nonOpportunityAttackRulesBeforeCancel = ruleProbe.PairNonOpportunityAttackRuleCount;
            pairOpportunityAttackRulesBeforeCancel = ruleProbe.PairOpportunityAttackRuleCount;
            stockCancelBefore = combat.StockAttackIntentCancelCount;
            observations["stockMeleeCancelBeforeGround"] = CapturePairCommandState();
            var cancelPoint = FindWalkablePoint(horse.Position, 2.0f, 0.6f);
            ClickGroundHandler.MoveSelectedUnitsToPoint(cancelPoint, false);
            observations["stockMeleeCancelAfterGroundAdmission"] = CapturePairCommandState();
            step = Phase3dHorseStep.AwaitStockCancelRt;
            ResetLeafClock();
        }

        private void AwaitStockCancelRt()
        {
            if (combat.HasStockAttackIntent)
            {
                return;
            }
            stableFrames++;
            if (stableFrames < 5)
            {
                return;
            }

            var evidence = CaptureStockEvidence();
            evidence["pairAttackRulesBeforeCancel"] = attackRulesBeforeCancel;
            evidence["pairNonOpportunityAttackRulesBeforeCancel"] = nonOpportunityAttackRulesBeforeCancel;
            evidence["pairOpportunityAttackRulesBeforeCancel"] = pairOpportunityAttackRulesBeforeCancel;
            evidence["pairNonOpportunityAttackRuleDeltaAfterCancel"] =
                ruleProbe.PairNonOpportunityAttackRuleCount - nonOpportunityAttackRulesBeforeCancel;
            evidence["pairOpportunityAttackRuleDeltaAfterCancel"] =
                ruleProbe.PairOpportunityAttackRuleCount - pairOpportunityAttackRulesBeforeCancel;
            evidence["commandStateBeforeGround"] = observations["stockMeleeCancelBeforeGround"]?.DeepClone();
            evidence["commandStateAfterGroundAdmission"] =
                observations["stockMeleeCancelAfterGroundAdmission"]?.DeepClone();
            evidence["commandStateAfterStableCancel"] = CapturePairCommandState();
            AddRow("mounted-stock-click-melee-cancel-rt",
                combat.StockAttackIntentCancelCount - stockCancelBefore == 1 &&
                    ruleProbe.PairNonOpportunityAttackRuleCount == nonOpportunityAttackRulesBeforeCancel &&
                    combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore == 0,
                "One native ground command cancelled persistent stock intent and no late ordinary or duplicate pair attack completed; independently emitted native AoOs remain separately attributed.", evidence);
            var nativeBeforeFriendlyClick = combat.StockAttackNativeRequestCount;
            var intentBeforeFriendlyClick = combat.StockAttackIntentStartCount;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var friendlyClicked = new ClickUnitHandler().OnClick(
                horse.View.gameObject,
                horse.Position,
                0,
                false,
                false);
            var friendlyEvidence = new JObject
            {
                ["clicked"] = friendlyClicked,
                ["nativeRequestDelta"] = combat.StockAttackNativeRequestCount - nativeBeforeFriendlyClick,
                ["intentStartDelta"] = combat.StockAttackIntentStartCount - intentBeforeFriendlyClick,
                ["relationshipState"] = relationship.State.ToString(),
                ["lastObservation"] = combat.LastStockAttackObservation,
                ["lastFeedback"] = combat.LastFeedback
            };
            AddRow(
                "mounted-stock-click-invalid-target-feedback",
                combat.StockAttackNativeRequestCount == nativeBeforeFriendlyClick &&
                    combat.StockAttackIntentStartCount == intentBeforeFriendlyClick &&
                    !combat.HasStockAttackIntent && relationship.State == RelationshipState.Mounted,
                "An ordinary click on the friendly Horse remained a native non-hostile interaction and did not fabricate mounted attack intent.",
                friendlyEvidence);
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            stableFrames = 0;
            BeginRangedTargetReplacement();
        }

        private void BeginRangedTargetReplacement()
        {
            rangedReadinessAtAdmission = null;
            combat.Cancel("Phase 3D ranged boundary");
            TryLeaveCombat(target);
            targetCleanupComplete = targetService.DestroyAndVerify();
            step = Phase3dHorseStep.AwaitRangedTargetCleanup;
            ResetLeafClock();
        }

        private void AwaitRangedTargetCleanup()
        {
            if (!targetCleanupComplete)
            {
                targetCleanupComplete = targetService.DestroyAndVerify();
            }
            if (!targetCleanupComplete)
            {
                return;
            }

            targetService.Dispose();
            targetService = null;
            target = null;
            rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
            rangedWeaponLease.Acquire(WeaponCategory.Shortbow);
            BeginTarget(LongRangeTargetDistance, "rt-ranged");
            step = Phase3dHorseStep.AwaitRangedCombat;
        }

        private void AwaitRangedCombat()
        {
            var selected = SelectionManager.Instance?.SelectedUnits;
            if (selected == null || selected.Count != 1 || selected[0] != rider)
            {
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                observations["rangedRtReadiness"] = CaptureLongRangeRangedReadiness(false);
                stableFrames = 0;
                return;
            }

            var clickLeaseReady = targetService != null && targetService.PrepareForPlayerClick(target);
            var readiness = CaptureLongRangeRangedReadiness(clickLeaseReady);
            observations["rangedRtReadiness"] = readiness;
            if (!(bool)readiness["ready"])
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            stableFrames = 0;
            rangedReadinessAtAdmission = readiness.DeepClone() as JObject;
            movementStart = horse.Position;
            ruleProbe.Arm(target, true);
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            stockRiderBefore = combat.StockAttackRiderDispatchCount;
            stockMountBefore = combat.StockAttackMountDispatchCount;
            stockDuplicateBefore = combat.StockAttackDuplicateDispatchCount;
            var expectedDispatchStarted = targetService.BeginExpectedAttackDispatch(target);
            var selectionBeforeClick = SelectionManager.Instance.SelectedUnits;
            var nearestBeforeClick = Game.Instance.UI.SelectionManager.GetNearestSelectedUnit(target.View.transform.position);
            var clicked = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            var nativeRequestDelta = combat.StockAttackNativeRequestCount - stockNativeBefore;
            var intentStartDelta = combat.StockAttackIntentStartCount - stockIntentBefore;
            observations["rangedRtInput"] = new JObject
            {
                ["clicked"] = clicked,
                ["expectedDispatchStarted"] = expectedDispatchStarted,
                ["nativeRequestDelta"] = nativeRequestDelta,
                ["intentStartDelta"] = intentStartDelta,
                ["targetId"] = target.UniqueId,
                ["selectionCount"] = selectionBeforeClick?.Count ?? 0,
                ["selectedRiderExact"] = selectionBeforeClick != null &&
                    selectionBeforeClick.Count == 1 && selectionBeforeClick[0] == rider,
                ["nearestSelectedUnitId"] = nearestBeforeClick?.UniqueId,
                ["lastObservation"] = combat.LastStockAttackObservation,
                ["feedback"] = combat.LastFeedback
            };
            if (!clicked || !expectedDispatchStarted || nativeRequestDelta != 1 || intentStartDelta != 1)
            {
                FailCurrent(
                    "mounted-bow-approach-to-range-rt",
                    "Ordinary native Shortbow hostile click did not admit the readiness-proven rider request and mounted intent exactly once.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRangedAttackRt;
            ResetLeafClock();
        }

        private void AwaitRangedAttackRt()
        {
            var riderDispatches = combat.StockAttackRiderDispatchCount - stockRiderBefore;
            if (riderDispatches < 2 || combat.HasActiveCommand)
            {
                return;
            }

            var mountDispatches = combat.StockAttackMountDispatchCount - stockMountBefore;
            var distanceMoved = HorizontalDistance(movementStart, horse.Position);
            var weapon = rider.GetFirstWeapon()?.Blueprint;
            var outcome = combat.LastOutcome;
            var evidence = CaptureStockEvidence();
            evidence["horseApproachDistance"] = distanceMoved;
            evidence["weaponGuid"] = weapon?.AssetGuid;
            evidence["weaponCategory"] = weapon?.Category.ToString();
            evidence["weaponRangeMeters"] = weapon?.AttackRange.Meters;
            evidence["admissionReadiness"] = rangedReadinessAtAdmission?.DeepClone();
            evidence["input"] = observations["rangedRtInput"]?.DeepClone();
            evidence["outcome"] = outcome == null ? null : JObject.FromObject(outcome, JsonSerializer.Create(JsonSettings));

            AddRow("mounted-bow-approach-to-range-rt",
                weapon != null && weapon.IsRanged && distanceMoved > 0.25f &&
                    ruleProbe.RiderAttackRuleCount >= 1,
                "The Horse approached from outside the native Shortbow range and stopped for a rider-owned ranged attack.", evidence);
            AddRow("mounted-bow-auto-fire-rt",
                riderDispatches >= 2 && combat.HasStockAttackIntent && mountDispatches == 0,
                "One ranged hostile click persisted through two native rider dispatches without automatically ordering Horse melee.", evidence);
            AddRow("mounted-ranged-does-not-force-melee",
                mountDispatches == 0 && combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore == 0,
                "Ranged intent produced zero automatic Horse-primary dispatches and zero duplicate dispatches.", evidence);
            AddRow("mounted-ranged-line-of-sight",
                outcome != null && outcome.NativeAttackRuleObserved && outcome.AttackWeaponIsRanged &&
                    string.Equals(
                        outcome.NativeAdmissionStateAtStart,
                        MountedPairNativeAdmissionState.Admitted.ToString(),
                        StringComparison.Ordinal),
                "The native ranged child entered only after its exact direct range-and-line-of-sight gate admitted.", evidence);

            attackRulesBeforeCancel = ruleProbe.PairAttackRuleCount;
            stockCancelBefore = combat.StockAttackIntentCancelCount;
            combat.Cancel("Phase 3D ranged cancel control");
            step = Phase3dHorseStep.AwaitRangedCancelRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitRangedCancelRt()
        {
            if (combat.HasStockAttackIntent || combat.HasActiveCommand)
            {
                return;
            }
            stableFrames++;
            if (stableFrames < 5)
            {
                return;
            }

            var evidence = CaptureStockEvidence();
            AddRow("mounted-bow-cancel-rt",
                combat.StockAttackIntentCancelCount - stockCancelBefore == 1 &&
                    ruleProbe.PairAttackRuleCount == attackRulesBeforeCancel,
                "Explicit cancellation terminated ranged intent with no late attack.", evidence);
            AddRow("mounted-ranged-cover-concealment",
                combat.LastOutcome != null && combat.LastOutcome.NativeAttackRuleObserved,
                "Ranged resolution remained inside native UnitAttack/RuleAttackRoll; this open-ground row does not claim a positive cover or concealment modifier.",
                evidence);

            BeginRangedAdjacentMoveRt();
        }

        private void BeginRangedAdjacentMoveRt()
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            stableFrames = 0;
            movementStart = horse.Position;
            movementDestination = FindWalkablePointNearTarget(target.Position, horse.Position, 1.35f);
            ClickGroundHandler.MoveSelectedUnitsToPoint(movementDestination, false);
            movementCommand = horse.Commands.Move as UnitMoveTo;
            if (movementCommand == null || movementCommand.Executor != horse)
            {
                FailCurrent("mounted-bow-adjacent-rt", "Ordinary rider-principal ground input did not route the adjacent-control approach through the Horse.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRangedAdjacentMoveRt;
            ResetLeafClock();
        }

        private void AwaitRangedAdjacentMoveRt()
        {
            if (movementCommand != null && !movementCommand.IsFinished)
            {
                return;
            }
            if (rider.DistanceTo(target) > 2.5f)
            {
                FailCurrent("mounted-bow-adjacent-rt", "Horse-owned approach did not place the ranged rider adjacent to the hostile control.");
                BeginCleanup();
                return;
            }

            rangedOpportunityReadinessAtAdmission = CaptureRangedOpportunityReadiness();
            observations["rangedOpportunityReadiness"] = rangedOpportunityReadinessAtAdmission.DeepClone();
            if (!IsRangedOpportunityControlReady())
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            ruleProbe.Arm(target, true);
            ruleProbe.ResetOpportunityCounts();
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            stockRiderBefore = combat.StockAttackRiderDispatchCount;
            stockMountBefore = combat.StockAttackMountDispatchCount;
            stockDuplicateBefore = combat.StockAttackDuplicateDispatchCount;
            movementStart = horse.Position;
            rangedMountMeleeReadyAtAdmission = horse.IsEngage(target);
            rangedOpportunityReadinessAtAdmission = CaptureRangedOpportunityReadiness();
            rangedRiderOutcome = null;
            rangedRiderOutcomeBaseline = combat.LastOutcome;
            targetService.BeginExpectedAttackDispatch(target);
            var clicked = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            if (!clicked && combat.StockAttackIntentStartCount == stockIntentBefore)
            {
                FailCurrent("mounted-bow-adjacent-rt", "Ordinary adjacent Shortbow hostile click did not create mounted ranged intent.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRangedAdjacentAttackRt;
            ResetLeafClock();
        }

        private void AwaitRangedAdjacentAttackRt()
        {
            ObserveCurrentRangedRiderOutcome();
            observations["rangedOpportunityProgress"] = CaptureRangedOpportunityProgress();
            if (combat.HasActiveCommand || combat.StockAttackRiderDispatchCount - stockRiderBefore < 1 ||
                rangedRiderOutcome == null || ruleProbe.OpportunityAttackRuleCount < 1 ||
                ruleProbe.OpportunityAttackRollCount < 1 || ruleProbe.OpportunityDamageRuleCount < 1)
            {
                return;
            }

            var outcome = rangedRiderOutcome;
            var mountDispatches = combat.StockAttackMountDispatchCount - stockMountBefore;
            var horseMovementDistance = HorizontalDistance(movementStart, horse.Position);
            var evidence = CaptureStockEvidence();
            evidence["outcome"] = outcome == null
                ? null
                : JObject.FromObject(outcome, JsonSerializer.Create(JsonSettings));
            evidence["riderDistanceToTarget"] = rider.DistanceTo(target);
            evidence["horseMovementDistanceAfterAdmission"] = horseMovementDistance;
            evidence["mountAlreadyInMeleeAtAdmission"] = rangedMountMeleeReadyAtAdmission;
            evidence["opportunityReadyAtAdmission"] = rangedOpportunityReadinessAtAdmission?.DeepClone();
            evidence["opportunity"] = ruleProbe.CaptureOpportunityEvidence();
            evidence["targetOpportunityCountAfter"] = target.CombatState.AttackOfOpportunityCount;
            AddRow(
                "mounted-bow-adjacent-rt",
                rider.DistanceTo(target) <= 2.5f && outcome != null && outcome.AttackWeaponIsRanged &&
                    outcome.NativeAttackRuleObserved && outcome.Action == MountedCombatActionKind.RiderRanged &&
                    outcome.ResourceOwnerId == rider.UniqueId &&
                    combat.StockAttackRiderDispatchCount - stockRiderBefore == 1 && mountDispatches == 0 &&
                    ruleProbe.RiderAttackRuleCount == 1 && ruleProbe.MountAttackRuleCount == 0 &&
                    ruleProbe.PairAttackRollCount == 1 &&
                    string.Equals(ruleProbe.LastRiderAttackType, "Ranged", StringComparison.Ordinal) &&
                    ruleProbe.LastRiderAttackDoNotProvoke == false &&
                    horseMovementDistance <= 0.25f &&
                    combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore == 0,
                "An ordinary adjacent Shortbow click produced one rider-first native ranged attack; the focused AoO control cancelled before any Horse primary dispatch or further approach.",
                evidence);
            AddRow(
                "mounted-ranged-aao-native-control",
                ruleProbe.OpportunityAttackRuleCount == 1 && ruleProbe.OpportunityAttackRollCount == 1 &&
                    ruleProbe.OpportunityDamageRuleCount == 1 &&
                    ruleProbe.LastOpportunityActorId == target.UniqueId &&
                    ruleProbe.LastOpportunityTargetId == rider.UniqueId &&
                    ruleProbe.ExpectedTargetForcedD20Count >= 1 &&
                    target.CombatState.AttackOfOpportunityCount == 0,
                "The rider-first, simulate-ready adjacent ranged attack queued and completed exactly one Kingmaker hostile AoO attack/roll/damage chain; KMC applied no ranged AoO suppression.",
                evidence);
            attackRulesBeforeCancel = ruleProbe.PairAttackRuleCount;
            stockCancelBefore = combat.StockAttackIntentCancelCount;
            combat.Cancel("Phase 3D adjacent ranged control complete");
            step = Phase3dHorseStep.AwaitRangedAdjacentCancelRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitRangedAdjacentCancelRt()
        {
            if (combat.HasStockAttackIntent || combat.HasActiveCommand ||
                target?.Commands == null || !target.Commands.Empty || target.AreHandsBusyWithAnimation)
            {
                return;
            }
            stableFrames++;
            if (stableFrames < 3)
            {
                return;
            }
            BeginRangedVariantTargetReplacement(WeaponCategory.LightCrossbow);
        }

        private void BeginRangedVariantTargetReplacement(WeaponCategory category)
        {
            rangedVariantCategory = category;
            rangedVariantPreviousTargetId = target?.UniqueId;
            rangedVariantPreviousTargetCleanupPassed = false;
            rangedVariantReadinessAtAdmission = null;
            combat.Cancel("Phase 3D " + category + " isolated target boundary");
            TryLeaveCombat(target);
            targetCleanupComplete = targetService != null && targetService.DestroyAndVerify();
            step = Phase3dHorseStep.AwaitRangedVariantTargetCleanupRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitRangedVariantTargetCleanupRt()
        {
            if (!targetCleanupComplete && targetService != null)
            {
                targetCleanupComplete = targetService.DestroyAndVerify();
            }
            if (!targetCleanupComplete)
            {
                return;
            }

            targetService.Dispose();
            targetService = null;
            target = null;
            rangedVariantPreviousTargetCleanupPassed = true;
            rangedWeaponLease?.Dispose();
            rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
            rangedWeaponLease.Acquire(rangedVariantCategory);
            BeginTarget(RangedVariantTargetDistance, "rt-" + rangedVariantCategory.ToString().ToLowerInvariant());
            step = Phase3dHorseStep.AwaitRangedVariantAdmissionRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitRangedVariantAdmissionRt()
        {
            var readiness = CaptureRangedVariantReadiness();
            observations["rangedVariantReadiness"] = readiness;
            if (!(bool)readiness["ready"])
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            stableFrames = 0;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            ruleProbe.Arm(target, true);
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            stockRiderBefore = combat.StockAttackRiderDispatchCount;
            stockMountBefore = combat.StockAttackMountDispatchCount;
            stockDuplicateBefore = combat.StockAttackDuplicateDispatchCount;
            movementStart = horse.Position;
            rangedMountMeleeReadyAtAdmission = horse.IsEngage(target);
            rangedVariantReadinessAtAdmission = readiness.DeepClone() as JObject;
            rangedRiderOutcome = null;
            rangedRiderOutcomeBaseline = combat.LastOutcome;
            var expectedDispatchStarted = targetService.BeginExpectedAttackDispatch(target);
            var clicked = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            var nativeRequestDelta = combat.StockAttackNativeRequestCount - stockNativeBefore;
            var intentStartDelta = combat.StockAttackIntentStartCount - stockIntentBefore;
            observations["rangedVariantInput"] = new JObject
            {
                ["category"] = rangedVariantCategory.ToString(),
                ["clicked"] = clicked,
                ["expectedDispatchStarted"] = expectedDispatchStarted,
                ["nativeRequestDelta"] = nativeRequestDelta,
                ["intentStartDelta"] = intentStartDelta,
                ["lastObservation"] = combat.LastStockAttackObservation,
                ["feedback"] = combat.LastFeedback
            };
            var row = rangedVariantCategory == WeaponCategory.LightCrossbow
                ? "mounted-crossbow-or-reload-control"
                : "mounted-sling-control";
            if (!expectedDispatchStarted || nativeRequestDelta != 1 || intentStartDelta != 1)
            {
                FailCurrent(row, "Ordinary native ranged hostile click did not admit the isolated, readiness-proven " + rangedVariantCategory + " control.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRangedVariantRt;
            ResetLeafClock();
        }

        private void AwaitRangedVariantRt()
        {
            ObserveCurrentRangedRiderOutcome();
            if (combat.HasActiveCommand || combat.StockAttackRiderDispatchCount - stockRiderBefore < 1 ||
                rangedRiderOutcome == null)
            {
                return;
            }

            var outcome = rangedRiderOutcome;
            var weapon = rider.GetFirstWeapon()?.Blueprint;
            var mountDispatches = combat.StockAttackMountDispatchCount - stockMountBefore;
            var horseMovementDistance = HorizontalDistance(movementStart, horse.Position);
            var evidence = CaptureStockEvidence();
            evidence["weaponCategory"] = weapon?.Category.ToString();
            evidence["horseMovementDistanceAfterAdmission"] = horseMovementDistance;
            evidence["mountAlreadyInMeleeAtAdmission"] = rangedMountMeleeReadyAtAdmission;
            evidence["admissionReadiness"] = rangedVariantReadinessAtAdmission?.DeepClone();
            evidence["input"] = observations["rangedVariantInput"]?.DeepClone();
            evidence["previousTargetId"] = rangedVariantPreviousTargetId;
            evidence["previousTargetCleanupPassed"] = rangedVariantPreviousTargetCleanupPassed;
            evidence["isolatedTargetId"] = target.UniqueId;
            evidence["outcome"] = outcome == null
                ? null
                : JObject.FromObject(outcome, JsonSerializer.Create(JsonSettings));
            var row = rangedVariantCategory == WeaponCategory.LightCrossbow
                ? "mounted-crossbow-or-reload-control"
                : "mounted-sling-control";
            AddRow(
                row,
                weapon != null && weapon.IsRanged && weapon.Category == rangedVariantCategory &&
                    outcome != null && outcome.AttackWeaponIsRanged && outcome.NativeAttackRuleObserved &&
                    outcome.ChildAttackStartCount == 1 &&
                    !string.IsNullOrWhiteSpace(outcome.AmmunitionStateBefore) &&
                    !string.IsNullOrWhiteSpace(outcome.AmmunitionStateAfter) &&
                    !string.IsNullOrWhiteSpace(outcome.ReloadStateBefore) &&
                    !string.IsNullOrWhiteSpace(outcome.ReloadStateAfter) &&
                    ruleProbe.RiderAttackRuleCount == 1 &&
                    ruleProbe.PairAttackRollCount == 1 + mountDispatches &&
                    mountDispatches >= 0 && mountDispatches <= 1 &&
                    (mountDispatches == 0 || rangedMountMeleeReadyAtAdmission &&
                        ruleProbe.MountAttackRuleCount == 1) &&
                    horseMovementDistance <= 0.25f &&
                    combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore == 0,
                "Ordinary mounted " + rangedVariantCategory + " fire stayed on native weapon/ammunition/reload surfaces; any single Horse primary remained bounded to an already-legal melee position with no forced approach.",
                evidence);
            stockCancelBefore = combat.StockAttackIntentCancelCount;
            combat.Cancel("Phase 3D " + rangedVariantCategory + " control complete");
            step = Phase3dHorseStep.AwaitRangedVariantCancelRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitRangedVariantCancelRt()
        {
            if (combat.HasStockAttackIntent || combat.HasActiveCommand)
            {
                return;
            }
            stableFrames++;
            if (stableFrames < 3)
            {
                return;
            }

            if (rangedVariantCategory == WeaponCategory.LightCrossbow)
            {
                BeginRangedVariantTargetReplacement(WeaponCategory.Sling);
                return;
            }
            rangedWeaponLease.Dispose();
            rangedWeaponLease = null;
            if (IsPhase3fNativeControlScope)
            {
                // C0 qualification stays in native RT. The legacy shared-TB tail is a separate historical experiment.
                BeginRtCombatDismount();
                return;
            }
            BeginRtToTbTransition();
        }

        private void BeginRtToTbTransition()
        {
            combat.Cancel("Phase 3D RT-to-TB boundary");
            turnSnapshotBefore = combat.CaptureUnifiedTurnSnapshot();
            turnBasedModeProbe = new NativeModeTransitionProbe(true);
            turnBasedModeProbe.DispatchTemporaryValueIfRequired();
            transitionRiderTurnObserved = false;
            transitionRiderStartRequestCount = 0;
            transitionFirstNativeTurnUnitId = null;
            step = Phase3dHorseStep.AwaitRtToTbTransition;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitRtToTbTransition()
        {
            var controller = Game.Instance.TurnBasedCombatController;
            if (!CombatController.IsInTurnBasedCombat() || controller == null || !controller.Initialized ||
                !controller.SortedUnits.Contains(rider) || !controller.SortedUnits.Contains(horse) ||
                relationship.State != RelationshipState.Mounted)
            {
                return;
            }

            var currentTurn = controller.CurrentTurn;
            if (currentTurn == null)
            {
                return;
            }
            if (!transitionRiderTurnObserved)
            {
                transitionFirstNativeTurnUnitId = currentTurn.Unit?.UniqueId;
                transitionRiderTurnObserved = true;
                if (currentTurn.Unit != rider)
                {
                    controller.StartTurn(rider);
                    transitionRiderStartRequestCount++;
                }
                stableFrames = 0;
                return;
            }
            if (!IsStableRiderTurn(currentTurn))
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            InitiativeTrackerVM tracker = null;
            try
            {
                tracker = new InitiativeTrackerVM();
                var after = combat.CaptureUnifiedTurnSnapshot();
                var riderEntries = tracker.Units.Count(item => item.Unit == rider);
                var horseEntries = tracker.Units.Count(item => item.Unit == horse);
                var riderEntry = tracker.Units.SingleOrDefault(item => item.Unit == rider);
                var evidence = new JObject
                {
                    ["before"] = JObject.FromObject(turnSnapshotBefore, JsonSerializer.Create(JsonSettings)),
                    ["after"] = JObject.FromObject(after, JsonSerializer.Create(JsonSettings)),
                    ["trackerRiderCount"] = riderEntries,
                    ["trackerHorseCount"] = horseEntries,
                    ["trackerRiderPortraitExact"] = riderEntry != null &&
                        ReferenceEquals(riderEntry.Portrait, rider.Portrait.SmallPortrait),
                    ["currentTurnUnitId"] = currentTurn.Unit?.UniqueId,
                    ["currentTurnStatus"] = currentTurn.Status.ToString(),
                    ["firstNativeTurnUnitId"] = transitionFirstNativeTurnUnitId,
                    ["riderStartTurnRequestCount"] = transitionRiderStartRequestCount,
                    ["transitionRequired"] = turnBasedModeProbe.TransitionRequired,
                    ["temporaryValueCurrent"] = turnBasedModeProbe.TemporaryValueIsCurrent
                };
                AddRow(
                    "RT-to-TB-shared-turn",
                    riderEntries == 1 && horseEntries == 0 &&
                        after.SharedInitiativeOwnerId == rider.UniqueId &&
                        relationship.State == RelationshipState.Mounted,
                    "The exact native RT-to-TB callback retained the pair and projected one rider-led shared turn.",
                    evidence);
                AddRow(
                    "mounted-combat-start-single-initiative-entry",
                    riderEntries == 1 && horseEntries == 0 && after.TrackerMountFilterCount >= 1,
                    "Combat began with the pair mounted in RT; exact native TB projection then exposed one rider entry and no redundant Horse entry.",
                    evidence);
                AddRow(
                    "mounted-rider-initiative-bonus",
                    after.SharedInitiativeOwnerId == rider.UniqueId &&
                        after.SharedInitiativeValue == rider.CombatState.Initiative &&
                        after.SharedInitiativeBonus == rider.Stats.Initiative.ModifiedValue &&
                        horse.CombatState.Initiative == rider.CombatState.Initiative,
                    "The mounted-at-combat-start pair retained the rider initiative result and bonus through native RT-to-TB conversion.",
                    evidence);
                AddRow(
                    "mounted-turn-rider-portrait",
                    riderEntries == 1 && horseEntries == 0 && riderEntry != null &&
                        ReferenceEquals(riderEntry.Portrait, rider.Portrait.SmallPortrait),
                    "The converted mounted-at-combat-start turn displayed the exact rider small portrait.",
                    evidence);
            }
            finally
            {
                tracker?.Dispose();
            }

            transitionPrimaryDispatched = false;
            stableFrames = 0;
            step = Phase3dHorseStep.AwaitRiderPrimaryAfterTransition;
            ResetLeafClock();
        }

        private void AwaitRiderPrimaryAfterTransition()
        {
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            if (!transitionPrimaryDispatched)
            {
                if (!IsStableRiderTurn(turn))
                {
                    stableFrames = 0;
                    return;
                }
                stableFrames++;
                if (stableFrames < 2)
                {
                    return;
                }
                outcomeBefore = combat.LastOutcome;
                activationSequenceBefore = nativeControls.SnapshotAbilityActivations()
                    .Select(item => item.Sequence)
                    .DefaultIfEmpty(0L)
                    .Max();
                ruleProbe.Arm(target, true);
                if (!TryNativeAbilityTargetClick(nativeControls.RiderPrimaryAbility, target, "transition-rider-primary"))
                {
                    FailCurrent("rider-primary-after-shared-turn-transition-does-not-dismount", "Rider Primary was not admitted after the exact RT-to-TB transition.");
                    BeginCleanup();
                    return;
                }
                transitionPrimaryDispatched = true;
                ResetLeafClock();
                return;
            }

            if (combat.HasActiveCommand || ReferenceEquals(combat.LastOutcome, outcomeBefore))
            {
                return;
            }
            var outcome = combat.LastOutcome;
            var activations = nativeControls.SnapshotAbilityActivations()
                .Where(item => item.Sequence > activationSequenceBefore && item.Kind == NativeMountedControlKind.RiderPrimary)
                .ToArray();
            var retained = relationship.State == RelationshipState.Mounted &&
                activations.Length > 0 && activations.All(item => !item.RelationshipEnded && !item.CleanupTrigger.HasValue);
            AddRow(
                "rider-primary-after-shared-turn-transition-does-not-dismount",
                retained && outcome != null && outcome.Action == MountedCombatActionKind.RiderMelee &&
                    outcome.ChildAttackStartCount == 1,
                "Rider Primary completed after a native shared-turn transition without lifecycle cleanup or automatic remount.",
                CaptureOutcome(outcome, activations));
            if (!retained)
            {
                BeginCleanup();
                return;
            }

            turnSnapshotBefore = combat.CaptureUnifiedTurnSnapshot();
            turnBasedModeProbe.DispatchRestoreAndRestoreRawCache();
            step = Phase3dHorseStep.AwaitTbToRtTransition;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitTbToRtTransition()
        {
            if (CombatController.IsInTurnBasedCombat())
            {
                return;
            }
            if (Game.Instance.IsPaused)
            {
                Game.Instance.IsPaused = false;
            }
            stableFrames++;
            if (stableFrames < 3)
            {
                return;
            }

            var after = combat.CaptureUnifiedTurnSnapshot();
            var evidence = new JObject
            {
                ["before"] = JObject.FromObject(turnSnapshotBefore, JsonSerializer.Create(JsonSettings)),
                ["after"] = JObject.FromObject(after, JsonSerializer.Create(JsonSettings)),
                ["persistedValueUnchanged"] = turnBasedModeProbe.PersistedValueUnchanged,
                ["restoreDeliveryCompleted"] = turnBasedModeProbe.RestoreDeliveryCompleted,
                ["relationshipState"] = relationship.State.ToString()
            };
            AddRow(
                "TB-to-RT-shared-turn",
                relationship.State == RelationshipState.Mounted &&
                    turnBasedModeProbe.RestoreDeliveryCompleted && turnBasedModeProbe.PersistedValueUnchanged &&
                    after.SharedInitiativeOwnerId == rider.UniqueId,
                "The exact TB-to-RT callback retained the rider-led pair and restored the non-persisted setting lease.",
                evidence);
            turnBasedModeProbe.Dispose();
            turnBasedModeProbe = null;
            BeginRtCombatDismount();
        }

        private void BeginRtCombatDismount()
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            step = Phase3dHorseStep.AwaitRtCombatDismountAdmission;
            ResetLeafClock();
        }

        private void AwaitRtCombatDismountAdmission()
        {
            nativeControls.Update();
            var availability = nativeControls.Evaluate(NativeMountedControlKind.Dismount, rider);
            observations["rtCombatDismountReadiness"] = CaptureRtCombatDismountState(availability);
            if (!availability.IsEnabled)
            {
                return;
            }

            rtCombatDismountBefore = combat.CaptureUnifiedTurnSnapshot();
            if (!TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "rt-combat-dismount"))
            {
                FailCurrent("unmounted-stock-attack-control", "Native Dismount was not admitted before unmounted stock controls.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRtCombatDismount;
            ResetLeafClock();
        }

        private void AwaitRtCombatDismount()
        {
            if (relationship.State != RelationshipState.Unmounted || !rider.Commands.Empty || !horse.Commands.Empty)
            {
                return;
            }
            observations["rtCombatDismountCompletion"] = new JObject
            {
                ["before"] = JObject.FromObject(rtCombatDismountBefore, JsonSerializer.Create(JsonSettings)),
                ["after"] = JObject.FromObject(
                    combat.CaptureUnifiedTurnSnapshot(), JsonSerializer.Create(JsonSettings)),
                ["relationshipState"] = relationship.State.ToString(),
                ["riderMoveCooldown"] = rider.CombatState?.Cooldown.MoveAction,
                ["riderStandardCooldown"] = rider.CombatState?.Cooldown.StandardAction,
                ["playerActionFeedback"] = playerAction.LastFeedback,
                ["commands"] = CapturePairCommandState(),
                ["dismountActivations"] = JArray.FromObject(
                    nativeControls.SnapshotAbilityActivations()
                        .Where(item => item.Kind == NativeMountedControlKind.Dismount).ToArray(),
                    JsonSerializer.Create(JsonSettings))
            };
            step = Phase3dHorseStep.AwaitUnmountedHorseAiIsolation;
            ResetLeafClock();
            AwaitUnmountedHorseAiIsolation();
        }

        private void AwaitUnmountedHorseAiIsolation()
        {
            if (!PrepareUnmountedHorseAiIsolation())
            {
                return;
            }

            observations["unmountedHorseAiIsolation"] = CaptureUnmountedHorseAiIsolation();
            BeginUnmountedTargetReplacement();
        }

        private void BeginUnmountedTargetReplacement()
        {
            unmountedPreviousTargetId = target?.UniqueId;
            unmountedPreviousTargetCleanupPassed = false;
            TryLeaveCombat(target);
            targetCleanupComplete = targetService != null && targetService.DestroyAndVerify();
            step = Phase3dHorseStep.AwaitUnmountedTargetCleanupRt;
            ResetLeafClock();
        }

        private void AwaitUnmountedTargetCleanupRt()
        {
            if (!ValidateUnmountedHorseAiIsolation())
            {
                return;
            }
            if (!targetCleanupComplete && targetService != null)
            {
                targetCleanupComplete = targetService.DestroyAndVerify();
            }
            if (!targetCleanupComplete)
            {
                return;
            }

            targetService.Dispose();
            targetService = null;
            target = null;
            unmountedPreviousTargetCleanupPassed = true;
            BeginTarget(TargetDistance, "rt-unmounted-controls");
            BeginUnmountedMeleeRt();
        }

        private void BeginUnmountedMeleeRt()
        {
            if (!ValidateUnmountedHorseAiIsolation())
            {
                return;
            }
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            ruleProbe.Arm(target, true);
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            targetService.BeginExpectedAttackDispatch(target);
            new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            unmountedCommand = rider.Commands.Standard;
            if (unmountedCommand == null || unmountedCommand.GetType() != typeof(UnitAttack))
            {
                FailCurrent("unmounted-stock-attack-control", "Ordinary unmounted hostile click did not create the rider's exact native UnitAttack.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitUnmountedMeleeRt;
            ResetLeafClock();
        }

        private void AwaitUnmountedMeleeRt()
        {
            if (!ValidateUnmountedHorseAiIsolation())
            {
                return;
            }
            if (ruleProbe.RiderNonOpportunityAttackRuleCount < 1)
            {
                return;
            }
            var evidence = new JObject
            {
                ["commandType"] = unmountedCommand?.GetType().FullName,
                ["nativeRequestDelta"] = combat.StockAttackNativeRequestCount - stockNativeBefore,
                ["intentStartDelta"] = combat.StockAttackIntentStartCount - stockIntentBefore,
                ["rules"] = ruleProbe.CapturePairEvidence(),
                ["relationshipState"] = relationship.State.ToString(),
                ["horseAiIsolation"] = CaptureUnmountedHorseAiIsolation(),
                ["previousTargetId"] = unmountedPreviousTargetId,
                ["previousTargetCleanupPassed"] = unmountedPreviousTargetCleanupPassed,
                ["isolatedTargetId"] = target?.UniqueId
            };
            AddRow(
                "unmounted-stock-attack-control",
                relationship.State == RelationshipState.Unmounted && unmountedCommand?.GetType() == typeof(UnitAttack) &&
                    combat.StockAttackNativeRequestCount == stockNativeBefore &&
                    combat.StockAttackIntentStartCount == stockIntentBefore &&
                    ruleProbe.RiderNonOpportunityAttackRuleCount >= 1 &&
                    ruleProbe.RiderOpportunityAttackRuleCount == 0 && ruleProbe.MountAttackRuleCount == 0 &&
                    unmountedPreviousTargetCleanupPassed &&
                    !string.Equals(unmountedPreviousTargetId, target?.UniqueId, StringComparison.Ordinal),
                "Ordinary unmounted hostile click remained a stock rider UnitAttack and bypassed all mounted intent routing.",
                evidence);
            rider.Commands.InterruptAll(false);
            step = Phase3dHorseStep.AwaitUnmountedMeleeCancelRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitUnmountedMeleeCancelRt()
        {
            if (!ValidateUnmountedHorseAiIsolation())
            {
                return;
            }
            if (!rider.Commands.Empty)
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 3)
            {
                return;
            }
            BeginUnmountedRangedTargetReplacement();
        }

        private void BeginUnmountedRangedTargetReplacement()
        {
            unmountedMeleeTargetId = target?.UniqueId;
            unmountedMeleeTargetCleanupPassed = false;
            TryLeaveCombat(target);
            targetCleanupComplete = targetService != null && targetService.DestroyAndVerify();
            step = Phase3dHorseStep.AwaitUnmountedRangedTargetCleanupRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitUnmountedRangedTargetCleanupRt()
        {
            if (!ValidateUnmountedHorseAiIsolation())
            {
                return;
            }
            if (!targetCleanupComplete && targetService != null)
            {
                targetCleanupComplete = targetService.DestroyAndVerify();
            }
            if (!targetCleanupComplete)
            {
                return;
            }

            targetService.Dispose();
            targetService = null;
            target = null;
            unmountedMeleeTargetCleanupPassed = true;
            rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
            rangedWeaponLease.Acquire(WeaponCategory.Sling);
            BeginTarget(TargetDistance, "rt-unmounted-ranged-control");
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            step = Phase3dHorseStep.AwaitUnmountedRangedAdmissionRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitUnmountedRangedAdmissionRt()
        {
            if (!ValidateUnmountedHorseAiIsolation())
            {
                return;
            }
            var readiness = CaptureUnmountedRangedReadiness();
            observations["unmountedRangedReadiness"] = readiness;
            if (!(bool)readiness["ready"])
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            stableFrames = 0;
            ruleProbe.Arm(target, true);
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            var expectedDispatchStarted = targetService.BeginExpectedAttackDispatch(target);
            var clicked = new ClickUnitHandler().OnClick(
                target.View.gameObject, target.Position, 0, false, false);
            unmountedCommand = rider.Commands.Standard;
            observations["unmountedRangedInput"] = new JObject
            {
                ["clicked"] = clicked,
                ["expectedDispatchStarted"] = expectedDispatchStarted,
                ["readiness"] = readiness.DeepClone(),
                ["command"] = CaptureUnmountedRangedCommand()
            };
            if (unmountedCommand == null || unmountedCommand.GetType() != typeof(UnitAttack))
            {
                FailCurrent("unmounted-ranged-control", "Ordinary unmounted Sling click did not create the rider's exact native UnitAttack.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitUnmountedRangedRt;
            ResetLeafClock();
        }

        private void AwaitUnmountedRangedRt()
        {
            if (!ValidateUnmountedHorseAiIsolation())
            {
                return;
            }
            if (ruleProbe.RiderNonOpportunityAttackRuleCount < 1)
            {
                return;
            }
            var weapon = rider.GetFirstWeapon()?.Blueprint;
            var evidence = new JObject
            {
                ["commandType"] = unmountedCommand?.GetType().FullName,
                ["weaponCategory"] = weapon?.Category.ToString(),
                ["targetId"] = target?.UniqueId,
                ["nativeRequestDelta"] = combat.StockAttackNativeRequestCount - stockNativeBefore,
                ["intentStartDelta"] = combat.StockAttackIntentStartCount - stockIntentBefore,
                ["rules"] = ruleProbe.CapturePairEvidence(),
                ["relationshipState"] = relationship.State.ToString(),
                ["horseAiIsolation"] = CaptureUnmountedHorseAiIsolation(),
                ["admissionReadiness"] = observations["unmountedRangedReadiness"]?.DeepClone(),
                ["input"] = observations["unmountedRangedInput"]?.DeepClone(),
                ["command"] = CaptureUnmountedRangedCommand(),
                ["previousMeleeTargetId"] = unmountedMeleeTargetId,
                ["previousMeleeTargetCleanupPassed"] = unmountedMeleeTargetCleanupPassed,
                ["isolatedTargetId"] = target?.UniqueId
            };
            AddRow(
                "unmounted-ranged-control",
                relationship.State == RelationshipState.Unmounted && weapon != null && weapon.IsRanged &&
                    weapon.Category == WeaponCategory.Sling && unmountedCommand?.GetType() == typeof(UnitAttack) &&
                    combat.StockAttackNativeRequestCount == stockNativeBefore &&
                    combat.StockAttackIntentStartCount == stockIntentBefore &&
                    ruleProbe.RiderNonOpportunityAttackRuleCount >= 1 &&
                    ruleProbe.RiderOpportunityAttackRuleCount == 0 && ruleProbe.MountAttackRuleCount == 0 &&
                    unmountedMeleeTargetCleanupPassed &&
                    !string.Equals(unmountedMeleeTargetId, target?.UniqueId, StringComparison.Ordinal) &&
                    observations["unmountedRangedInput"]?["clicked"]?.Value<bool>() == true &&
                    observations["unmountedRangedInput"]?["expectedDispatchStarted"]?.Value<bool>() == true,
                "Ordinary unmounted Sling fire remained stock UnitAttack behavior and bypassed mounted intent routing.",
                evidence);
            rider.Commands.InterruptAll(false);
            BeginCleanup();
        }

        private void AwaitCombatMountAdjacencyReadiness()
        {
            if (Game.Instance.IsPaused)
            {
                Game.Instance.IsPaused = false;
            }

            observations["combatMountAdjacencyProgress"] = CaptureCombatMountAdjacencyProgress();
            if (relationship.State != RelationshipState.Unmounted || target != null ||
                rider.IsInCombat || horse.IsInCombat || rider.View?.AgentASP == null ||
                horse.View?.AgentASP == null || rider.Commands == null || horse.Commands == null ||
                !rider.Commands.Empty || !horse.Commands.Empty)
            {
                stableFrames = 0;
                return;
            }

            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            combatMountAdjacencyRiderStart = rider.Position;
            combatMountAdjacencyHorseStart = horse.Position;
            var pairDistanceBefore = rider.DistanceTo(horse);
            var riderCorpulence = rider.View.Corpulence;
            var mountCorpulence = horse.View.Corpulence;
            var adjacentBefore = CombatMountDismountPolicy.IsAdjacent(
                pairDistanceBefore,
                riderCorpulence,
                mountCorpulence);
            combatMountAdjacencyAdmission = new JObject
            {
                ["setupRequired"] = !adjacentBefore,
                ["pairAdjacentBefore"] = adjacentBefore,
                ["pairDistanceBefore"] = pairDistanceBefore,
                ["adjacencyThreshold"] = riderCorpulence + mountCorpulence +
                    CombatMountDismountPolicy.NativeAdjacentReachMeters,
                ["riderCorpulence"] = riderCorpulence,
                ["mountCorpulence"] = mountCorpulence,
                ["riderStart"] = CapturePosition(combatMountAdjacencyRiderStart),
                ["mountStart"] = CapturePosition(combatMountAdjacencyHorseStart),
                ["destination"] = JValue.CreateNull(),
                ["setupMechanism"] = adjacentBefore
                    ? "already-adjacent"
                    : "ClickGroundHandler.MoveSelectedUnitsToPoint",
                ["nativeGroundInputInvoked"] = false,
                ["nativeGroundInputAdmitted"] = false,
                ["commandPresent"] = false,
                ["commandOwnerId"] = JValue.CreateNull(),
                ["commandCreatedByPlayer"] = false,
                ["horseMoveSlotExactAtAdmission"] = false,
                ["riderMoveSlotEmptyAtAdmission"] = rider.Commands.Move == null,
                ["selectionHorseExactAtAdmission"] = false,
                ["relationshipStateBefore"] = relationship.State.ToString(),
                ["targetPresentBefore"] = target != null,
                ["riderInCombatBefore"] = rider.IsInCombat,
                ["mountInCombatBefore"] = horse.IsInCombat,
                ["turnBasedBefore"] = CombatController.IsInTurnBasedCombat()
            };

            if (adjacentBefore)
            {
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                observations["combatMountAdjacencySetup"] = CaptureCombatMountAdjacencyCompletion();
                BeginCombatMountHorseAiIsolation();
                return;
            }

            SelectionManager.Instance.SelectUnit(horse.View, true, true, false);
            combatMountAdjacencyDestination = FindWalkablePointNearTarget(
                rider.Position,
                horse.Position,
                1.35f);
            combatMountAdjacencyAdmission["destination"] = CapturePosition(combatMountAdjacencyDestination);
            combatMountAdjacencyAdmission["nativeGroundInputInvoked"] = true;
            ClickGroundHandler.MoveSelectedUnitsToPoint(combatMountAdjacencyDestination, false);
            combatMountAdjacencyCommand = horse.Commands.Move as UnitMoveTo;
            var selection = SelectionManager.Instance.SelectedUnits;
            var horseMoveSlotExact = combatMountAdjacencyCommand != null &&
                ReferenceEquals(horse.Commands.Move, combatMountAdjacencyCommand);
            var selectionHorseExact = selection != null && selection.Count == 1 && selection[0] == horse;
            var nativeInputAdmitted = combatMountAdjacencyCommand != null &&
                combatMountAdjacencyCommand.Executor == horse &&
                combatMountAdjacencyCommand.CreatedByPlayer && horseMoveSlotExact &&
                rider.Commands.Move == null && selectionHorseExact;
            combatMountAdjacencyAdmission["nativeGroundInputAdmitted"] = nativeInputAdmitted;
            combatMountAdjacencyAdmission["commandPresent"] = combatMountAdjacencyCommand != null;
            combatMountAdjacencyAdmission["commandOwnerId"] = combatMountAdjacencyCommand?.Executor?.UniqueId;
            combatMountAdjacencyAdmission["commandCreatedByPlayer"] =
                combatMountAdjacencyCommand != null && combatMountAdjacencyCommand.CreatedByPlayer;
            combatMountAdjacencyAdmission["horseMoveSlotExactAtAdmission"] = horseMoveSlotExact;
            combatMountAdjacencyAdmission["riderMoveSlotEmptyAtAdmission"] = rider.Commands.Move == null;
            combatMountAdjacencyAdmission["selectionHorseExactAtAdmission"] = selectionHorseExact;
            observations["combatMountAdjacencyProgress"] = CaptureCombatMountAdjacencyProgress();
            if (!nativeInputAdmitted)
            {
                observations["combatMountAdjacencySetup"] = CaptureCombatMountAdjacencyCompletion();
                FailCurrent(
                    "mount-in-combat-before-either-acted",
                    "Ordinary pre-combat Horse ground input did not admit one exact player-created Horse Move command.");
                BeginCleanup();
                return;
            }

            step = Phase3dHorseStep.AwaitCombatMountAdjacencyMove;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitCombatMountAdjacencyMove()
        {
            observations["combatMountAdjacencyProgress"] = CaptureCombatMountAdjacencyProgress();
            if (combatMountAdjacencyCommand == null || !combatMountAdjacencyCommand.IsFinished ||
                !rider.Commands.Empty || !horse.Commands.Empty)
            {
                stableFrames = 0;
                return;
            }

            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var evidence = CaptureCombatMountAdjacencyCompletion();
            observations["combatMountAdjacencySetup"] = evidence;
            var accepted = combatMountAdjacencyCommand.Result == UnitCommand.ResultType.Success &&
                evidence["pairAdjacentAfter"].Value<bool>() &&
                evidence["mountPhysicalDistance"].Value<float>() > 0.25f &&
                evidence["riderPhysicalDistance"].Value<float>() <= 0.15f &&
                evidence["selectionRiderExactAfter"].Value<bool>() &&
                relationship.State == RelationshipState.Unmounted && target == null &&
                !rider.IsInCombat && !horse.IsInCombat && !CombatController.IsInTurnBasedCombat();
            if (!accepted)
            {
                FailCurrent(
                    "mount-in-combat-before-either-acted",
                    "Stock pre-combat Horse movement did not finish adjacent while preserving the stationary unmounted rider and noncombat state.");
                BeginCleanup();
                return;
            }

            BeginCombatMountHorseAiIsolation();
        }

        private void BeginCombatMountHorseAiIsolation()
        {
            step = Phase3dHorseStep.AwaitCombatMountHorseAiIsolation;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitCombatMountHorseAiIsolation()
        {
            if (!PrepareUnmountedHorseAiIsolation())
            {
                return;
            }
            if (!PrepareCombatMountRiderAiIsolation())
            {
                return;
            }

            observations["combatMountHorseAiIsolation"] = CaptureUnmountedHorseAiIsolation();
            observations["combatMountRiderAiIsolation"] = CaptureCombatMountRiderAiIsolation();
            observations["pairedSchedulerPreTargetSetup"] = new JObject
            {
                ["pairInitiallyMounted"] = turnBasedPairInitiallyMounted,
                ["relationshipState"] = relationship.State.ToString(),
                ["relationshipExact"] = relationship.State == RelationshipState.Mounted &&
                    relationship.Rider == rider && relationship.Mount == horse,
                ["targetAbsent"] = target == null,
                ["turnBasedAbsent"] = !CombatController.IsInTurnBasedCombat(),
                ["riderInCombat"] = rider.IsInCombat,
                ["mountInCombat"] = horse.IsInCombat
            };
            BeginTarget(TargetDistance, "tb-paired-scheduler");
            step = Phase3dHorseStep.AwaitUnmountedCombat;
            stableFrames = 0;
            ResetLeafClock();
        }

        private JObject CaptureCombatMountAdjacencyCompletion()
        {
            var selected = SelectionManager.Instance?.SelectedUnits;
            var riderCorpulence = rider.View?.Corpulence ?? float.PositiveInfinity;
            var mountCorpulence = horse.View?.Corpulence ?? float.PositiveInfinity;
            var pairDistanceAfter = rider.DistanceTo(horse);
            var evidence = combatMountAdjacencyAdmission == null
                ? new JObject()
                : (JObject)combatMountAdjacencyAdmission.DeepClone();
            evidence["pairAdjacentAfter"] = CombatMountDismountPolicy.IsAdjacent(
                pairDistanceAfter,
                riderCorpulence,
                mountCorpulence);
            evidence["pairDistanceAfter"] = pairDistanceAfter;
            evidence["riderFinal"] = CapturePosition(rider.Position);
            evidence["mountFinal"] = CapturePosition(horse.Position);
            evidence["mountPhysicalDistance"] = HorizontalDistance(
                combatMountAdjacencyHorseStart,
                horse.Position);
            evidence["riderPhysicalDistance"] = HorizontalDistance(
                combatMountAdjacencyRiderStart,
                rider.Position);
            evidence["commandFinished"] = combatMountAdjacencyCommand != null &&
                combatMountAdjacencyCommand.IsFinished;
            evidence["commandResult"] = combatMountAdjacencyCommand == null
                ? JValue.CreateNull()
                : new JValue(combatMountAdjacencyCommand.Result.ToString());
            evidence["commandsIdleAfter"] = rider.Commands != null && rider.Commands.Empty &&
                horse.Commands != null && horse.Commands.Empty;
            evidence["selectionRiderExactAfter"] = selected != null && selected.Count == 1 && selected[0] == rider;
            evidence["relationshipStateAfter"] = relationship.State.ToString();
            evidence["targetPresentAfter"] = target != null;
            evidence["riderInCombatAfter"] = rider.IsInCombat;
            evidence["mountInCombatAfter"] = horse.IsInCombat;
            evidence["turnBasedAfter"] = CombatController.IsInTurnBasedCombat();
            return evidence;
        }

        private JObject CaptureCombatMountAdjacencyProgress()
        {
            try
            {
                var riderCorpulence = rider.View?.Corpulence ?? float.PositiveInfinity;
                var mountCorpulence = horse.View?.Corpulence ?? float.PositiveInfinity;
                var pairDistance = rider.DistanceTo(horse);
                return new JObject
                {
                    ["step"] = step.ToString(),
                    ["frame"] = Time.frameCount,
                    ["stableFrames"] = stableFrames,
                    ["relationshipState"] = relationship.State.ToString(),
                    ["gamePaused"] = Game.Instance != null && Game.Instance.IsPaused,
                    ["turnBased"] = CombatController.IsInTurnBasedCombat(),
                    ["targetPresent"] = target != null,
                    ["riderInCombat"] = rider.IsInCombat,
                    ["mountInCombat"] = horse.IsInCombat,
                    ["riderCommandsIdle"] = rider.Commands != null && rider.Commands.Empty,
                    ["mountCommandsIdle"] = horse.Commands != null && horse.Commands.Empty,
                    ["pairDistance"] = pairDistance,
                    ["riderCorpulence"] = riderCorpulence,
                    ["mountCorpulence"] = mountCorpulence,
                    ["pairAdjacent"] = CombatMountDismountPolicy.IsAdjacent(
                        pairDistance,
                        riderCorpulence,
                        mountCorpulence),
                    ["riderPosition"] = CapturePosition(rider.Position),
                    ["mountPosition"] = CapturePosition(horse.Position),
                    ["commandPresent"] = combatMountAdjacencyCommand != null,
                    ["commandFinished"] = combatMountAdjacencyCommand != null &&
                        combatMountAdjacencyCommand.IsFinished,
                    ["commandResult"] = combatMountAdjacencyCommand == null
                        ? JValue.CreateNull()
                        : new JValue(combatMountAdjacencyCommand.Result.ToString()),
                    ["admission"] = combatMountAdjacencyAdmission?.DeepClone()
                };
            }
            catch (Exception exception)
            {
                return new JObject
                {
                    ["step"] = step.ToString(),
                    ["frame"] = Time.frameCount,
                    ["captureError"] = exception.GetType().FullName + ": " + exception.Message
                };
            }
        }

        private void AwaitUnmountedCombat()
        {
            if (Game.Instance.IsPaused)
            {
                Game.Instance.IsPaused = false;
            }
            if (!IsCombatReady(turnBasedPairInitiallyMounted))
            {
                return;
            }

            turnBasedModeProbe = new NativeModeTransitionProbe(true);
            turnBasedModeProbe.DispatchTemporaryValueIfRequired();
            step = Phase3dHorseStep.AwaitTurnBasedMode;
            ResetLeafClock();
        }

        private void AwaitTurnBasedMode()
        {
            var controller = Game.Instance.TurnBasedCombatController;
            observations["combatMountTurnAdmissionProgress"] = CaptureCombatMountTurnAdmissionProgress();
            if (!CombatController.IsInTurnBasedCombat() || controller == null || !controller.Initialized ||
                !controller.SortedUnits.Contains(rider) || !controller.SortedUnits.Contains(horse) ||
                !controller.SortedUnits.Contains(target))
            {
                return;
            }

            if (!CaptureNativeTurnTraversalRoster(controller))
            {
                FailCurrent(
                    "phase3d-horse-runtime-exception",
                    "The exact initialized TB roster could not be captured for bounded native turn traversal.");
                BeginCleanup();
                return;
            }

            // Do not synthesize a rider turn here. Exact Kingmaker TickTime owns the
            // pending m_NextUnit -> StartTurn transition and clears m_NextUnit only
            // after that native call. Calling StartTurn directly can leave the native
            // pending unit intact and strand WaitingForUI in a diagnostic-only turn.
            observations["combatMountBeforeNaturalRiderTurn"] = CaptureCombatMountTurnAdmissionProgress();
            step = Phase3dHorseStep.AwaitRiderTurnForMount;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitRiderTurnForMount()
        {
            if (!ValidateUnmountedHorseAiIsolation() || !ValidateCombatMountRiderAiIsolation())
            {
                return;
            }

            var game = Game.Instance;
            var controller = game?.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            var equipment = game?.HandsEquipmentController;
            var selected = SelectionManager.Instance?.SelectedUnits;
            var availability = nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider);
            var nextUnit = GetPendingNextUnit(controller);
            var riderTurnExact = turn?.Unit == rider;
            var turnActionable = riderTurnExact &&
                (turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing);
            var gameModeReady = game != null && game.CurrentMode == GameModeType.Default;
            var waitingForUi = controller != null && (bool)controller.WaitingForUI;
            var waitingForUiCount = controller?.WaitingForUI?.GuardCount ?? -1;
            var uiReady = controller != null && !waitingForUi && waitingForUiCount == 0;
            var nextUnitClear = nextUnit == null;
            var riderAwake = rider.IsAwake;
            var riderInAwakeSchedule = game?.State?.AwakeUnits != null &&
                game.State.AwakeUnits.Contains(rider);
            var riderRigidbodyControlling = rider.View?.RigidbodyController != null &&
                rider.View.RigidbodyController.IsControllingRigidbody;
            var riderGetUp = rider.View != null && rider.View.IsGetUp;
            var riderUnitTickReady = rider.View != null && !riderRigidbodyControlling && !riderGetUp;
            var riderNauseated = rider.Descriptor.State.HasCondition(UnitCondition.Nauseated);
            var riderHandsIdle = !rider.AreHandsBusyWithAnimation;
            var riderEquipmentIdle = equipment != null && !equipment.IsUpdateScheduledFor(rider);
            var selectionExact = selected != null && selected.Count == 1 && selected[0] == rider;
            var exactMountedPair = relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == horse;
            var relationshipAdmissionReady = turnBasedPairInitiallyMounted
                ? exactMountedPair
                : availability.IsVisible && availability.IsEnabled;
            if (riderTurnExact)
            {
                combatMountRiderTurnObservedFrames++;
            }
            else
            {
                combatMountCurrentTurnMismatchFrames++;
            }
            if (turnActionable)
            {
                combatMountActionableTurnObservedFrames++;
            }
            else if (riderTurnExact)
            {
                combatMountTurnStatusBlockedFrames++;
            }
            if (rider.Commands == null || !rider.Commands.Empty)
            {
                combatMountRiderCommandBlockedFrames++;
            }
            if (horse.Commands == null || !horse.Commands.Empty)
            {
                combatMountHorseCommandBlockedFrames++;
            }
            if (!riderHandsIdle)
            {
                combatMountRiderHandsBlockedFrames++;
            }
            if (!riderEquipmentIdle)
            {
                combatMountRiderEquipmentBlockedFrames++;
            }
            if (!uiReady)
            {
                combatMountWaitingForUiBlockedFrames++;
            }
            if (!nextUnitClear)
            {
                combatMountPendingNextUnitBlockedFrames++;
            }
            if (!riderAwake)
            {
                combatMountRiderAwakeBlockedFrames++;
            }
            if (!riderInAwakeSchedule)
            {
                combatMountRiderAwakeScheduleBlockedFrames++;
            }
            if (!riderUnitTickReady)
            {
                combatMountRiderUnitTickBlockedFrames++;
            }
            if (!gameModeReady || game.IsPaused)
            {
                combatMountGameModeBlockedFrames++;
            }
            if (!selectionExact)
            {
                combatMountSelectionBlockedFrames++;
            }
            if (riderNauseated)
            {
                combatMountNauseatedBlockedFrames++;
            }
            observations["combatMountTurnAdmissionProgress"] = CaptureCombatMountTurnAdmissionProgress();
            if (CombatMountSyntheticStartTurnRequestCount != 0 || turn?.Unit != rider ||
                turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing ||
                !gameModeReady || game.IsPaused || !uiReady || !nextUnitClear ||
                !riderAwake || !riderInAwakeSchedule || !riderUnitTickReady || riderNauseated ||
                !rider.Descriptor.State.CanAct || !rider.CombatState.CanActInCombat ||
                !rider.Commands.Empty || !horse.Commands.Empty ||
                !riderHandsIdle || !riderEquipmentIdle || !selectionExact ||
                !relationshipAdmissionReady)
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            if (turnBasedPairInitiallyMounted)
            {
                turnSnapshotBefore = combat.CaptureUnifiedTurnSnapshot();
                observations["pairedSchedulerMountedTurnAdmission"] = new JObject
                {
                    ["pairInitiallyMounted"] = true,
                    ["relationshipExact"] = exactMountedPair,
                    ["currentTurnRiderExact"] = turn?.Unit == rider,
                    ["currentTurnStatus"] = turn?.Status.ToString(),
                    ["selectionRiderExact"] = selectionExact,
                    ["nativeCombatMountCommandPresent"] = lastNativeAbilityShell != null,
                    ["riderCommandsIdle"] = rider.Commands.Empty,
                    ["mountCommandsIdle"] = horse.Commands.Empty,
                    ["unified"] = JObject.FromObject(
                        turnSnapshotBefore,
                        JsonSerializer.Create(JsonSettings))
                };
                ObserveSharedInitiativeAndTracker(turnSnapshotBefore);
                BeginRiderPrimaryTb();
                return;
            }

            turnSnapshotBefore = combat.CaptureUnifiedTurnSnapshot();
            riderStandardBeforeMount = rider.CombatState.Cooldown.StandardAction;
            riderMoveBeforeMount = rider.CombatState.Cooldown.MoveAction;
            mountStandardBeforeMount = horse.CombatState.Cooldown.StandardAction;
            mountMoveBeforeMount = horse.CombatState.Cooldown.MoveAction;
            var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "tb-combat-mount");
            var shell = lastNativeAbilityShell;
            var exactShellAdmitted = clicked && shell != null && shell.Executor == rider &&
                shell.Target?.Unit == horse && !shell.CreatedByPlayer && shell.AiAction == null &&
                ReferenceEquals(shell.Spell?.Blueprint, nativeControls.MountAbility) &&
                rider.Commands != null && rider.Commands.Contains(shell) &&
                ReferenceEquals(rider.Commands.GetCommand(UnitCommand.CommandType.Move), shell) &&
                !rider.Commands.Queue.Contains(shell);
            combatMountNativeCommandAdmissionFrame = Time.frameCount;
            observations["combatMountNativeCommandAtAdmission"] = CaptureCombatMountNativeCommandProgress();
            if (!exactShellAdmitted)
            {
                FailCurrent(
                    "mount-in-combat-before-either-acted",
                    "Native Mount Companion click did not admit one exact stock-origin rider Move-slot command during the natural rider turn.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitCombatMount;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitCombatMount()
        {
            if (lastNativeAbilityShell != null)
            {
                if (combatMountNativeCommandStartObservedFrame < 0 && lastNativeAbilityShell.IsStarted)
                {
                    combatMountNativeCommandStartObservedFrame = Time.frameCount;
                }
                if (combatMountNativeCommandTerminalObservedFrame < 0 && lastNativeAbilityShell.IsFinished)
                {
                    combatMountNativeCommandTerminalObservedFrame = Time.frameCount;
                }
            }
            observations["combatMountNativeCommandProgress"] = CaptureCombatMountNativeCommandProgress();
            if (relationship.State != RelationshipState.Mounted ||
                !relationship.Runtime.PoseFrameApplied || relationship.Runtime.PoseApplicationFrameCount < 3 ||
                !rider.Commands.Empty || !horse.Commands.Empty)
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            var controller = Game.Instance.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            var after = combat.CaptureUnifiedTurnSnapshot();
            var riderMoveSpent = rider.CombatState.Cooldown.MoveAction >= riderMoveBeforeMount + 2.9f;
            var mountLedgerPreserved =
                Math.Abs(horse.CombatState.Cooldown.StandardAction - mountStandardBeforeMount) <= 0.001f &&
                Math.Abs(horse.CombatState.Cooldown.MoveAction - mountMoveBeforeMount) <= 0.001f;
            var evidence = CaptureRawLedgerMountEvidence();
            evidence["unifiedAfter"] = JObject.FromObject(after, JsonSerializer.Create(JsonSettings));
            evidence["nativeMountCommand"] = CaptureCombatMountNativeCommandProgress();
            var nativeShellStartedPromptly = combatMountNativeTickFirstEligibleFrame >= 0 &&
                combatMountNativeCommandStartObservedFrame >= combatMountNativeTickFirstEligibleFrame &&
                combatMountNativeCommandStartObservedFrame - combatMountNativeTickFirstEligibleFrame <= 2;
            AddRow("mount-in-combat-before-either-acted",
                turn?.Unit == rider && riderStandardBeforeMount < 0.001f && mountStandardBeforeMount < 0.001f &&
                    riderMoveSpent && mountLedgerPreserved &&
                    after.SharedInitiativeOwnerId == rider.UniqueId &&
                    CombatMountSyntheticStartTurnRequestCount == 0 &&
                    combatMountNativeTickEncounterCount > 0 && combatMountNativeTickEligibleCount > 0 &&
                    combatMountNativeTickDuplicateFrameCount == 0 && nativeShellStartedPromptly &&
                    lastNativeAbilityShell != null && lastNativeAbilityShell.IsFinished &&
                    lastNativeAbilityShell.Result == UnitCommand.ResultType.Success,
                "Native combat Mount charged the rider Move ledger, preserved the Horse ledger, and retained the rider current turn.",
                evidence);
            AddRow("mount-ability-in-combat",
                relationship.State == RelationshipState.Mounted && riderMoveSpent,
                "Mount Companion remained available in combat and charged exactly the rider's native Move resource.",
                JObject.FromObject(after, JsonSerializer.Create(JsonSettings)));

            ObserveSharedInitiativeAndTracker(after);
            BeginRiderPrimaryTb();
        }

        private void BeginRiderPrimaryTb()
        {
            outcomeBefore = combat.LastOutcome;
            activationSequenceBefore = nativeControls.SnapshotAbilityActivations()
                .Select(item => item.Sequence)
                .DefaultIfEmpty(0L)
                .Max();
            ruleProbe.Arm(target, true);
            var clicked = TryNativeAbilityTargetClick(nativeControls.RiderPrimaryAbility, target, "tb-rider-primary");
            if (!clicked)
            {
                FailCurrent("rider-primary-does-not-dismount-tb", "Native TB Rider Primary target click was not admitted during the rider-led shared turn.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRiderPrimaryTb;
            ResetLeafClock();
        }

        private void AwaitRiderPrimaryTb()
        {
            if (combat.HasActiveCommand || ReferenceEquals(combat.LastOutcome, outcomeBefore))
            {
                return;
            }

            var outcome = combat.LastOutcome;
            var activations = nativeControls.SnapshotAbilityActivations()
                .Where(item => item.Sequence > activationSequenceBefore && item.Kind == NativeMountedControlKind.RiderPrimary)
                .ToArray();
            var retained = relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == horse &&
                activations.Length > 0 && activations.All(item => !item.RelationshipEnded && !item.CleanupTrigger.HasValue);
            var evidence = CaptureOutcome(outcome, activations);
            AddRow(
                "rider-primary-does-not-dismount-tb",
                retained && outcome != null && outcome.Action == MountedCombatActionKind.RiderMelee &&
                    outcome.ChildAttackStartCount == 1 && ruleProbe.RiderAttackRuleCount == 1 &&
                    ruleProbe.MountAttackRuleCount == 0,
                "One admitted TB Rider Primary produced one rider-owned attack and retained the exact mounted pair.",
                evidence);
            AddRow(
                "mounted-stock-click-melee-rider-only-explicit",
                retained && outcome != null && outcome.ResourceOwnerId == rider.UniqueId &&
                    ruleProbe.RiderAttackRuleCount == 1 && ruleProbe.MountAttackRuleCount == 0,
                "Explicit Rider Primary spent only the rider attack ledger during the shared mounted turn.",
                evidence);
            if (!retained)
            {
                BeginCleanup();
                return;
            }

            Game.Instance.TurnBasedCombatController.CurrentTurn.ForceToEnd(false);
            step = Phase3dHorseStep.AwaitNextRiderTurnForMountPrimaryTb;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitNextRiderTurnForMountPrimaryTb()
        {
            TurnController turn;
            if (!TryReachExpectedNativeTurn(
                    rider,
                    false,
                    "mount-primary-after-rider-only",
                    out turn) ||
                !IsStableRiderTurn(turn))
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            outcomeBefore = combat.LastOutcome;
            ruleProbe.Arm(target, true);
            explicitPrimaryLedgerBefore = combat.CaptureUnifiedTurnSnapshot();
            var clicked = TryNativeAbilityTargetClick(nativeControls.MountPrimaryAbility, target, "tb-mount-primary");
            if (!clicked)
            {
                FailCurrent("mounted-stock-click-melee-mount-only-explicit", "Native TB Mount Primary target click was not admitted during the rider-led shared turn.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitMountPrimaryTb;
            ResetLeafClock();
        }

        private void AwaitMountPrimaryTb()
        {
            if (combat.HasActiveCommand || ReferenceEquals(combat.LastOutcome, outcomeBefore))
            {
                return;
            }

            var outcome = combat.LastOutcome;
            var retained = relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == horse;
            var pairedScheduler = combat.CapturePairedCommandSchedulerSnapshot();
            if (pairedScheduler.HasActiveLease)
            {
                return;
            }

            var ledgerAfter = combat.CaptureUnifiedTurnSnapshot();
            var evidence = CaptureOutcome(outcome, new NativeMountedAbilityActivationRecord[0]);
            evidence["ledgerBefore"] = JObject.FromObject(
                explicitPrimaryLedgerBefore, JsonSerializer.Create(JsonSettings));
            evidence["ledgerAfter"] = JObject.FromObject(
                ledgerAfter, JsonSerializer.Create(JsonSettings));
            evidence["pairedScheduler"] = JObject.FromObject(
                pairedScheduler, JsonSerializer.Create(JsonSettings));
            var schedulerLifecycleExact = pairedScheduler.Enabled && !pairedScheduler.HasActiveLease &&
                pairedScheduler.State == PairedCommandSchedulerState.Disposed.ToString() &&
                pairedScheduler.RiderId == rider.UniqueId && pairedScheduler.MountId == horse.UniqueId &&
                pairedScheduler.CommandType == typeof(MountedPairAttackCommand).FullName &&
                pairedScheduler.ActionOrigin == MountedCombatActionKind.MountPrimaryNatural.ToString() &&
                pairedScheduler.TargetId == target.UniqueId &&
                pairedScheduler.ExpectedResourceOwnerId == horse.UniqueId &&
                pairedScheduler.ExpectedRuleInitiatorId == horse.UniqueId &&
                pairedScheduler.CreationFrame >= 0 &&
                pairedScheduler.AdmissionFrame >= pairedScheduler.CreationFrame &&
                pairedScheduler.FirstGrantFrame >= pairedScheduler.AdmissionFrame &&
                pairedScheduler.StartObservedFrame >= pairedScheduler.FirstGrantFrame &&
                pairedScheduler.StartObservedFrame - pairedScheduler.FirstGrantFrame <= 2 &&
                pairedScheduler.LastDrivenFrame >= pairedScheduler.StartObservedFrame &&
                pairedScheduler.DriveCount > 0 && pairedScheduler.StartObservationCount == 1 &&
                pairedScheduler.TerminalObservationCount == 1 && pairedScheduler.InterruptCount == 0 &&
                pairedScheduler.ResourceChargeObservationCount == 1 &&
                pairedScheduler.DuplicateFrameDriveCount == 0 && pairedScheduler.CleanupCount == 1 &&
                pairedScheduler.ForeignCommandAdoptionCount == 0 && pairedScheduler.RiderRemainedCurrent &&
                pairedScheduler.ExactExecutorRetained && pairedScheduler.ExactSlotRetained &&
                pairedScheduler.MountStandardAvailableBefore && !pairedScheduler.MountStandardAvailableAfter &&
                pairedScheduler.RiderStandardAvailableBefore && pairedScheduler.RiderStandardAvailableAfter &&
                Math.Abs(pairedScheduler.RiderStandardCooldownAfter -
                    pairedScheduler.RiderStandardCooldownBefore) <= 0.001f &&
                pairedScheduler.MountStandardCooldownAfter >=
                    pairedScheduler.MountStandardCooldownBefore + 2.9f &&
                pairedScheduler.TerminalResult == UnitCommand.ResultType.Success.ToString() &&
                pairedScheduler.LastRejection == PairedCommandSchedulerRejection.None.ToString() &&
                pairedScheduler.CleanupReason == "native terminal slot removal" &&
                string.IsNullOrEmpty(pairedScheduler.FaultReason);
            var unifiedLifecycleExact = ledgerAfter.CurrentTurnUnitId == rider.UniqueId &&
                ledgerAfter.SharedInitiativeOwnerId == rider.UniqueId &&
                ledgerAfter.DeferredMountTurnSkipCount >= 1 &&
                ledgerAfter.PostTickMountTurnSkipCount >= 1 &&
                ledgerAfter.RedundantMountTurnSkipCount >= 1 &&
                ledgerAfter.MountCommandAdmissionCount >= 1 &&
                ledgerAfter.ArchitectureFallbackCount == 0;
            AddRow(
                "mounted-stock-click-melee-mount-only-explicit",
                retained && outcome != null && outcome.Action == MountedCombatActionKind.MountPrimaryNatural &&
                    outcome.ResourceOwnerId == horse.UniqueId && ruleProbe.RiderAttackRuleCount == 0 &&
                    ruleProbe.MountAttackRuleCount == 1 && ruleProbe.PairAttackRollCount == 1 &&
                    ruleProbe.PairDamageRuleCount <= 1 && outcome.ChildAttackStartCount == 1 &&
                    outcome.NativeAttackRuleObserved && outcome.ActionStandardCharged &&
                    !outcome.RiderStandardCharged && outcome.AttackWeaponIsNatural &&
                    outcome.AttackAnimationHandleCreated && outcome.AttackAnimationActed &&
                    outcome.AttackAnimationFinished && !outcome.AttackAnimationInterrupted &&
                    schedulerLifecycleExact && unifiedLifecycleExact,
                "Explicit Mount Primary completed one exact Horse-owned scheduler lease, attack, animation, rule chain, and Standard charge while the rider remained the native turn principal.",
                evidence);
            if (!retained)
            {
                BeginCleanup();
                return;
            }

            Game.Instance.TurnBasedCombatController.CurrentTurn.ForceToEnd(false);
            step = Phase3dHorseStep.AwaitNextRiderTurnForStockMeleeTb;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitNextRiderTurnForStockMeleeTb()
        {
            TurnController turn;
            if (!TryReachExpectedNativeTurn(
                    rider,
                    false,
                    "ordinary-melee-after-mount-only",
                    out turn) ||
                !IsStableRiderTurn(turn))
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }
            BeginStockMeleeTb();
        }

        private void ObserveSharedInitiativeAndTracker(UnifiedMountedTurnSnapshot snapshot)
        {
            var controller = Game.Instance.TurnBasedCombatController;
            InitiativeTrackerVM tracker = null;
            try
            {
                tracker = new InitiativeTrackerVM();
                var riderEntries = tracker.Units.Count(item => item.Unit == rider);
                var horseEntries = tracker.Units.Count(item => item.Unit == horse);
                var riderEntry = tracker.Units.SingleOrDefault(item => item.Unit == rider);
                var evidence = new JObject
                {
                    ["unified"] = JObject.FromObject(snapshot, JsonSerializer.Create(JsonSettings)),
                    ["rosterRiderCount"] = controller.SortedUnits.Count(item => item == rider),
                    ["rosterHorseCount"] = controller.SortedUnits.Count(item => item == horse),
                    ["trackerRiderCount"] = riderEntries,
                    ["trackerHorseCount"] = horseEntries,
                    ["trackerRiderPortraitExact"] = riderEntry != null && ReferenceEquals(riderEntry.Portrait, rider.Portrait.SmallPortrait),
                    ["selectionRiderExact"] = SelectionManager.Instance.SelectedUnits.Count == 1 &&
                        SelectionManager.Instance.SelectedUnits[0] == rider
                };
                AddRow("mounted-combat-start-single-initiative-entry",
                    riderEntries == 1 && horseEntries == 0 && snapshot.TrackerMountFilterCount >= 1,
                    "The native roster retained both actors while the exact tracker projection exposed one rider entry and zero Horse entries.", evidence);
                AddRow("mounted-rider-initiative-bonus",
                    snapshot.SharedInitiativeOwnerId == rider.UniqueId &&
                        snapshot.SharedInitiativeValue == rider.CombatState.Initiative &&
                        snapshot.SharedInitiativeBonus == rider.Stats.Initiative.ModifiedValue &&
                        horse.CombatState.Initiative == rider.CombatState.Initiative,
                    "Rider result and bonus own the pair placement; Horse initiative mirrors that exact result.", evidence);
                AddRow("mounted-turn-rider-portrait",
                    riderEntries == 1 && horseEntries == 0 && riderEntry != null &&
                        ReferenceEquals(riderEntry.Portrait, rider.Portrait.SmallPortrait),
                    "The sole mounted turn-order VM is the rider and resolves the rider's native small portrait.", evidence);
                AddRow("mounted-single-rider-turn-portrait",
                    riderEntries == 1 && horseEntries == 0 &&
                        SelectionManager.Instance.SelectedUnits.Count == 1 && SelectionManager.Instance.SelectedUnits[0] == rider,
                    "Tracker, selection, and current-turn identity remain rider-principal.", evidence);
                AddRow("mounted-separate-action-ledgers",
                    snapshot.Rider != null && snapshot.Mount != null &&
                        snapshot.Rider.UnitId == rider.UniqueId && snapshot.Mount.UnitId == horse.UniqueId &&
                        !ReferenceEquals(snapshot.Rider, snapshot.Mount),
                    "The shared rider turn exposes two independent native cooldown ledger snapshots.", evidence);
            }
            finally
            {
                tracker?.Dispose();
            }
        }

        private void BeginStockMeleeTb()
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            ruleProbe.Arm(target, true);
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            stockRiderBefore = combat.StockAttackRiderDispatchCount;
            stockMountBefore = combat.StockAttackMountDispatchCount;
            stockDuplicateBefore = combat.StockAttackDuplicateDispatchCount;
            targetService.BeginExpectedAttackDispatch(target);
            var clicked = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            if (!clicked && combat.StockAttackIntentStartCount == stockIntentBefore)
            {
                FailCurrent("mounted-stock-click-melee-shared-turn-tb", "Native TB hostile click did not create mounted pair intent.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitStockMeleeTb;
            ResetLeafClock();
        }

        private void AwaitStockMeleeTb()
        {
            if (combat.HasActiveCommand ||
                combat.StockAttackRiderDispatchCount - stockRiderBefore < 1 ||
                combat.StockAttackMountDispatchCount - stockMountBefore < 1)
            {
                return;
            }

            var after = combat.CaptureUnifiedTurnSnapshot();
            var evidence = CaptureStockEvidence();
            evidence["unifiedAfter"] = JObject.FromObject(after, JsonSerializer.Create(JsonSettings));
            AddRow("mounted-stock-click-melee-shared-turn-tb",
                combat.StockAttackNativeRequestCount - stockNativeBefore == 1 &&
                    combat.StockAttackIntentStartCount - stockIntentBefore == 1 &&
                    combat.StockAttackRiderDispatchCount - stockRiderBefore == 1 &&
                    combat.StockAttackMountDispatchCount - stockMountBefore == 1 &&
                    combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore == 0,
                "One TB hostile click sequenced one rider attack then one Horse primary from separate ledgers in the rider-led turn.", evidence);
            AddRow("mounted-shared-turn-action-order",
                ruleProbe.RiderAttackRuleCount == 1 && ruleProbe.MountAttackRuleCount == 1 &&
                    ruleProbe.FirstPairAttackActorId == rider.UniqueId &&
                    ruleProbe.LastPairAttackActorId == horse.UniqueId,
                "Observed rule order is deterministic rider-first then Horse, with one chain per actor.", evidence);

            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            turn.ForceToEnd(false);
            step = Phase3dHorseStep.AwaitNextRiderTurnForRangedTb;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitNextRiderTurnForRangedTb()
        {
            TurnController turn;
            if (!TryReachExpectedNativeTurn(
                    rider,
                    false,
                    "ranged-after-melee-sequence",
                    out turn) ||
                !IsStableRiderTurn(turn))
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
            rangedWeaponLease.Acquire(WeaponCategory.Shortbow);
            if (!rangedWeaponLease.IsReady)
            {
                FailCurrent("mounted-bow-shared-turn-tb", "The deterministic native Shortbow lease did not become the rider's active ranged weapon.");
                BeginCleanup();
                return;
            }
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            ruleProbe.Arm(target, true);
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            stockRiderBefore = combat.StockAttackRiderDispatchCount;
            stockMountBefore = combat.StockAttackMountDispatchCount;
            stockDuplicateBefore = combat.StockAttackDuplicateDispatchCount;
            targetService.BeginExpectedAttackDispatch(target);
            var clicked = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            if (!clicked && combat.StockAttackIntentStartCount == stockIntentBefore)
            {
                FailCurrent("mounted-bow-shared-turn-tb", "Ordinary TB ranged hostile click did not create mounted ranged intent.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRangedTb;
            ResetLeafClock();
        }

        private void AwaitRangedTb()
        {
            if (combat.HasActiveCommand || combat.HasStockAttackIntent ||
                combat.StockAttackRiderDispatchCount - stockRiderBefore < 1)
            {
                return;
            }

            var outcome = combat.LastOutcome;
            var evidence = CaptureStockEvidence();
            evidence["outcome"] = outcome == null
                ? null
                : JObject.FromObject(outcome, JsonSerializer.Create(JsonSettings));
            evidence["weaponCategory"] = rider.GetFirstWeapon()?.Blueprint?.Category.ToString();
            AddRow(
                "mounted-bow-shared-turn-tb",
                combat.StockAttackNativeRequestCount - stockNativeBefore == 1 &&
                    combat.StockAttackIntentStartCount - stockIntentBefore == 1 &&
                    combat.StockAttackRiderDispatchCount - stockRiderBefore == 1 &&
                    combat.StockAttackMountDispatchCount - stockMountBefore == 0 &&
                    combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore == 0 &&
                    outcome != null && outcome.Action == MountedCombatActionKind.RiderRanged &&
                    outcome.ResourceOwnerId == rider.UniqueId,
                "One ordinary TB ranged hostile click spent only the rider Standard action in the shared turn.",
                evidence);
            AddRow(
                "mounted-ranged-does-not-force-melee",
                combat.StockAttackMountDispatchCount - stockMountBefore == 0 &&
                    ruleProbe.MountAttackRuleCount == 0,
                "TB ranged intent completed after the rider shot without dispatching Horse melee.",
                evidence);
            AddRow(
                "mounted-ranged-line-of-sight",
                outcome != null && outcome.NativeAttackRuleObserved && outcome.AttackWeaponIsRanged &&
                    rider.HasLOS(target),
                "TB ranged resolution retained native UnitAttack line-of-sight admission.",
                evidence);
            rangedWeaponLease.Dispose();
            rangedWeaponLease = null;
            Game.Instance.TurnBasedCombatController.CurrentTurn.ForceToEnd(false);
            step = Phase3dHorseStep.AwaitNextRiderTurnForStep;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitNextRiderTurnForStep()
        {
            TurnController turn;
            if (!TryReachExpectedNativeTurn(
                    rider,
                    false,
                    "five-foot-step-after-ranged",
                    out turn) ||
                turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing)
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var changed = false;
            for (var index = 0; index < 4 && !turn.EnabledFiveFootStep; index++)
            {
                changed |= turn.TryChangeSmartAction();
            }
            turnSnapshotBefore = combat.CaptureUnifiedTurnSnapshot();
            ruleProbe.Arm(target, true);
            ruleProbe.ResetOpportunityCounts();
            movementStart = horse.Position;
            movementDestination = FindWalkablePointAwayFromTarget(horse.Position, target.Position, 1.5f);
            ClickGroundHandler.MoveSelectedUnitsToPoint(movementDestination, false);
            movementCommand = horse.Commands.Move as UnitMoveTo;
            observations["fiveFootAdmission"] = new JObject
            {
                ["smartActionChanged"] = changed,
                ["enabled"] = turn.EnabledFiveFootStep,
                ["currentMovementLimit"] = turn.CurrentMovementLimit.ToString(),
                ["commandPresent"] = movementCommand != null,
                ["commandOwnerId"] = movementCommand?.Executor?.UniqueId
            };
            if (!turn.EnabledFiveFootStep || movementCommand == null || movementCommand.Executor != horse)
            {
                FailCurrent("mounted-five-foot-step-distance", "Exact native five-foot mode plus ordinary ground input did not produce the Horse move command.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitFiveFootStep;
            ResetLeafClock();
        }

        private void AwaitFiveFootStep()
        {
            if (movementCommand != null && !movementCommand.IsFinished)
            {
                return;
            }

            var after = combat.CaptureUnifiedTurnSnapshot();
            var distance = HorizontalDistance(movementStart, horse.Position);
            var evidence = new JObject
            {
                ["before"] = JObject.FromObject(turnSnapshotBefore, JsonSerializer.Create(JsonSettings)),
                ["after"] = JObject.FromObject(after, JsonSerializer.Create(JsonSettings)),
                ["physicalDistance"] = distance,
                ["nativeFiveFootMaximumMeters"] = TurnController.MetersOfFiveFootStep,
                ["opportunity"] = ruleProbe.CaptureOpportunityEvidence()
            };
            AddRow("mounted-five-foot-step-no-aao",
                ruleProbe.OpportunityAttackRuleCount == 0 && after.StepOpportunitySuppressionCount >=
                    turnSnapshotBefore.StepOpportunitySuppressionCount + 1,
                "The exact mounted five-foot command produced no disengage AoO while pair-local suppression recorded the exact candidate.", evidence);
            AddRow("mounted-five-foot-step-distance",
                distance > 0.1f && distance <= TurnController.MetersOfFiveFootStep + 0.15f &&
                    after.NativeFiveFootStepMeters <= TurnController.MetersOfFiveFootStep + 0.15f,
                "Horse physical travel stayed positive and within Kingmaker's native 7.5-foot cap.", evidence);
            AddRow("mounted-five-foot-step-resource",
                Math.Abs(after.Rider.Move - turnSnapshotBefore.Rider.Move) <= 0.001f &&
                    Math.Abs(after.Mount.Move - turnSnapshotBefore.Mount.Move) <= 0.001f,
                "Neither rider nor Horse ordinary Move cooldown increased for the exact native step.", evidence);

            Game.Instance.TurnBasedCombatController.CurrentTurn.ForceToEnd(false);
            step = Phase3dHorseStep.AwaitNextRiderTurnForOrdinaryMove;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitNextRiderTurnForOrdinaryMove()
        {
            TurnController turn;
            if (!TryReachExpectedNativeTurn(
                    rider,
                    false,
                    "ordinary-move-after-five-foot-step",
                    out turn) ||
                !IsStableRiderTurn(turn))
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            for (var index = 0; index < 4 && turn.EnabledFiveFootStep; index++)
            {
                turn.TryChangeSmartAction();
            }
            turnSnapshotBefore = combat.CaptureUnifiedTurnSnapshot();
            ruleProbe.Arm(target, true);
            ruleProbe.ResetOpportunityCounts();
            movementStart = horse.Position;
            movementDestination = FindWalkablePointAwayFromTarget(horse.Position, target.Position, 3.5f);
            ClickGroundHandler.MoveSelectedUnitsToPoint(movementDestination, false);
            movementCommand = horse.Commands.Move as UnitMoveTo;
            observations["ordinaryMoveAdmission"] = new JObject
            {
                ["fiveFootEnabled"] = turn.EnabledFiveFootStep,
                ["currentMovementLimit"] = turn.CurrentMovementLimit.ToString(),
                ["commandPresent"] = movementCommand != null,
                ["commandOwnerId"] = movementCommand?.Executor?.UniqueId
            };
            if (turn.EnabledFiveFootStep || movementCommand == null || movementCommand.Executor != horse)
            {
                FailCurrent("mounted-ordinary-move-aao-control", "Ordinary mounted ground input did not produce the exact Horse Move-slot command outside five-foot mode.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitOrdinaryMove;
            ResetLeafClock();
        }

        private void AwaitOrdinaryMove()
        {
            if (movementCommand != null && !movementCommand.IsFinished)
            {
                return;
            }

            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            var after = combat.CaptureUnifiedTurnSnapshot();
            var distance = HorizontalDistance(movementStart, horse.Position);
            var restrictsStep = turn.ShouldRestrictFiveFootStep();
            var changeAdmitted = false;
            for (var index = 0; index < 4 && !turn.EnabledFiveFootStep; index++)
            {
                changeAdmitted |= turn.TryChangeSmartAction();
            }
            var evidence = new JObject
            {
                ["before"] = JObject.FromObject(turnSnapshotBefore, JsonSerializer.Create(JsonSettings)),
                ["after"] = JObject.FromObject(after, JsonSerializer.Create(JsonSettings)),
                ["physicalDistance"] = distance,
                ["opportunity"] = ruleProbe.CaptureOpportunityEvidence(),
                ["restrictsFiveFootStep"] = restrictsStep,
                ["changeAdmitted"] = changeAdmitted,
                ["fiveFootEnabledAfterAttempt"] = turn.EnabledFiveFootStep
            };
            AddRow(
                "mounted-ordinary-move-aao-control",
                distance > TurnController.MetersOfFiveFootStep &&
                    ruleProbe.OpportunityAttackRuleCount >= 1 && ruleProbe.OpportunityAttackRollCount >= 1 &&
                    after.Mount.Move > turnSnapshotBefore.Mount.Move + 0.001f,
                "The same adjacent hostile produced a real native disengage AoO during ordinary Horse-owned movement and charged only the Horse movement ledger.",
                evidence);
            AddRow(
                "mounted-five-foot-step-after-movement-rejected",
                restrictsStep && !turn.EnabledFiveFootStep && !changeAdmitted,
                "After ordinary movement, the exact native turn restriction rejected a second five-foot-step mode admission.",
                evidence);

            turn.ForceToEnd(false);
            step = Phase3dHorseStep.AwaitNextRiderTurnForDismount;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitNextRiderTurnForDismount()
        {
            TurnController turn;
            if (!TryReachExpectedNativeTurn(
                    rider,
                    false,
                    "combat-dismount-after-ordinary-move",
                    out turn) ||
                turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing)
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            turnSnapshotBefore = combat.CaptureUnifiedTurnSnapshot();
            var clicked = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "tb-combat-dismount");
            if (!clicked)
            {
                FailCurrent("dismount-in-combat-no-extra-turn", "Native combat Dismount click was not admitted.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitCombatDismount;
            ResetLeafClock();
        }

        private void AwaitCombatDismount()
        {
            if (relationship.State != RelationshipState.Unmounted)
            {
                return;
            }

            var controller = Game.Instance.TurnBasedCombatController;
            var after = combat.CaptureUnifiedTurnSnapshot();
            var riderMoveSpent = turnSnapshotBefore?.Rider != null && rider.CombatState != null &&
                rider.CombatState.Cooldown.MoveAction >= turnSnapshotBefore.Rider.Move + 2.9f;
            var evidence = new JObject
            {
                ["before"] = JObject.FromObject(turnSnapshotBefore, JsonSerializer.Create(JsonSettings)),
                ["after"] = JObject.FromObject(after, JsonSerializer.Create(JsonSettings)),
                ["currentTurnUnitId"] = controller?.CurrentTurn?.Unit?.UniqueId,
                ["round"] = controller?.RoundNumber,
                ["riderMoveAfter"] = rider.CombatState?.Cooldown.MoveAction
            };
            AddRow("dismount-in-combat-no-extra-turn",
                riderMoveSpent && controller?.CurrentTurn?.Unit == rider &&
                    after.PendingSplit && after.PendingSplitRound == controller.RoundNumber,
                "Native combat Dismount charged rider Move, retained the current rider turn, and deferred Horse split participation.", evidence);
            AddRow("dismount-ability-in-combat",
                riderMoveSpent && relationship.Rider == null && relationship.Mount == null,
                "Combat Dismount used native Move cost and cleared the transient pair without refreshing either actor.", evidence);

            controller.CurrentTurn.ForceToEnd(false);
            step = Phase3dHorseStep.AwaitNextRiderTurnForUnmountedStep;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitNextRiderTurnForUnmountedStep()
        {
            TurnController turn;
            if (relationship.State != RelationshipState.Unmounted ||
                !TryReachExpectedNativeTurn(
                    rider,
                    true,
                    "unmounted-step-after-combat-dismount",
                    out turn) ||
                !IsStableRiderTurn(turn))
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var changed = false;
            for (var index = 0; index < 4 && !turn.EnabledFiveFootStep; index++)
            {
                changed |= turn.TryChangeSmartAction();
            }
            stepSuppressionBefore = combat.CaptureUnifiedTurnSnapshot().StepOpportunitySuppressionCount;
            ruleProbe.Arm(target, true);
            ruleProbe.ResetOpportunityCounts();
            movementStart = rider.Position;
            movementDestination = FindWalkablePointAwayFromTarget(rider.Position, target.Position, 1.5f);
            ClickGroundHandler.MoveSelectedUnitsToPoint(movementDestination, false);
            movementCommand = rider.Commands.Move as UnitMoveTo;
            observations["unmountedFiveFootAdmission"] = new JObject
            {
                ["smartActionChanged"] = changed,
                ["enabled"] = turn.EnabledFiveFootStep,
                ["commandPresent"] = movementCommand != null,
                ["commandOwnerId"] = movementCommand?.Executor?.UniqueId
            };
            if (!turn.EnabledFiveFootStep || movementCommand == null || movementCommand.Executor != rider)
            {
                FailCurrent("unmounted-five-foot-step-control", "Ordinary unmounted five-foot input did not produce the rider's exact native Move command.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitUnmountedStep;
            ResetLeafClock();
        }

        private void AwaitUnmountedStep()
        {
            if (movementCommand != null && !movementCommand.IsFinished)
            {
                return;
            }

            var after = combat.CaptureUnifiedTurnSnapshot();
            var distance = HorizontalDistance(movementStart, rider.Position);
            var evidence = new JObject
            {
                ["physicalDistance"] = distance,
                ["nativeFiveFootMaximumMeters"] = TurnController.MetersOfFiveFootStep,
                ["opportunity"] = ruleProbe.CaptureOpportunityEvidence(),
                ["stepSuppressionBefore"] = stepSuppressionBefore,
                ["stepSuppressionAfter"] = after.StepOpportunitySuppressionCount,
                ["relationshipState"] = relationship.State.ToString()
            };
            AddRow(
                "unmounted-five-foot-step-control",
                relationship.State == RelationshipState.Unmounted && distance > 0.1f &&
                    distance <= TurnController.MetersOfFiveFootStep + 0.15f &&
                    ruleProbe.OpportunityAttackRuleCount == 0 &&
                    after.StepOpportunitySuppressionCount == stepSuppressionBefore,
                "The unmounted rider retained stock five-foot distance and no-AoO behavior without invoking KMC pair-local suppression.",
                evidence);
            BeginRiderSpentAttackTb();
        }

        private void BeginRiderSpentAttackTb()
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            ruleProbe.Arm(target, true);
            targetService.BeginExpectedAttackDispatch(target);
            new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            unmountedCommand = rider.Commands.Standard;
            if (unmountedCommand == null || unmountedCommand.GetType() != typeof(UnitAttack))
            {
                FailCurrent("mount-in-combat-rider-already-acted", "The exact unmounted rider attack prerequisite did not create a native UnitAttack.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRiderSpentAttackTb;
            ResetLeafClock();
        }

        private void AwaitRiderSpentAttackTb()
        {
            if (ruleProbe.RiderAttackRuleCount < 1 || !unmountedCommand.IsFinished || !rider.Commands.Empty)
            {
                return;
            }

            riderStandardBeforeMount = rider.CombatState.Cooldown.StandardAction;
            riderMoveBeforeMount = rider.CombatState.Cooldown.MoveAction;
            mountStandardBeforeMount = horse.CombatState.Cooldown.StandardAction;
            mountMoveBeforeMount = horse.CombatState.Cooldown.MoveAction;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            if (!TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "tb-combat-mount-rider-spent"))
            {
                FailCurrent("mount-in-combat-rider-already-acted", "Combat Mount was not admitted after the rider spent its native Standard action.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRiderSpentCombatMount;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitRiderSpentCombatMount()
        {
            if (relationship.State != RelationshipState.Mounted ||
                !relationship.Runtime.PoseFrameApplied || !rider.Commands.Empty || !horse.Commands.Empty)
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            var evidence = CaptureRawLedgerMountEvidence();
            AddRow(
                "mount-in-combat-rider-already-acted",
                riderStandardBeforeMount >= 2.9f &&
                    Math.Abs(rider.CombatState.Cooldown.StandardAction - riderStandardBeforeMount) <= 0.001f &&
                    rider.CombatState.Cooldown.MoveAction >= riderMoveBeforeMount + 2.9f &&
                    Math.Abs(horse.CombatState.Cooldown.StandardAction - mountStandardBeforeMount) <= 0.001f &&
                    Math.Abs(horse.CombatState.Cooldown.MoveAction - mountMoveBeforeMount) <= 0.001f,
                "Combat Mount preserved the rider's already-spent Standard action, charged rider Move once, and preserved the Horse ledger.",
                evidence);
            Game.Instance.TurnBasedCombatController.CurrentTurn.ForceToEnd(false);
            step = Phase3dHorseStep.AwaitNextRiderTurnForSpentControlDismount;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitNextRiderTurnForSpentControlDismount()
        {
            TurnController turn;
            if (!TryReachExpectedNativeTurn(
                    rider,
                    false,
                    "spent-rider-control-dismount",
                    out turn) ||
                !IsStableRiderTurn(turn))
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            if (!TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "tb-spent-control-dismount"))
            {
                FailCurrent("mount-in-combat-mount-already-acted", "The intermediate native Dismount was not admitted before the mount-spent control.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitSpentControlDismount;
            ResetLeafClock();
        }

        private void AwaitSpentControlDismount()
        {
            if (relationship.State != RelationshipState.Unmounted || !rider.Commands.Empty || !horse.Commands.Empty)
            {
                return;
            }
            Game.Instance.TurnBasedCombatController.CurrentTurn.ForceToEnd(false);
            step = Phase3dHorseStep.AwaitHorseTurnForSpentControl;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitHorseTurnForSpentControl()
        {
            TurnController turn;
            if (relationship.State != RelationshipState.Unmounted ||
                !TryReachExpectedNativeTurn(
                    horse,
                    true,
                    "unmounted-horse-spent-control",
                    out turn) ||
                turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing ||
                !rider.Commands.Empty || !horse.Commands.Empty)
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            SelectionManager.Instance.SelectUnit(horse.View, true, true, false);
            ruleProbe.Arm(target, true);
            targetService.BeginExpectedAttackDispatch(target);
            new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            unmountedCommand = horse.Commands.Standard;
            if (unmountedCommand == null || unmountedCommand.GetType() != typeof(UnitAttack))
            {
                FailCurrent("mount-in-combat-mount-already-acted", "The exact unmounted Horse attack prerequisite did not create a native UnitAttack.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitHorseSpentAttackTb;
            ResetLeafClock();
        }

        private void AwaitHorseSpentAttackTb()
        {
            if (ruleProbe.MountAttackRuleCount < 1 || !unmountedCommand.IsFinished || !horse.Commands.Empty)
            {
                return;
            }
            Game.Instance.TurnBasedCombatController.CurrentTurn.ForceToEnd(false);
            step = Phase3dHorseStep.AwaitRiderTurnForMountSpentControl;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitRiderTurnForMountSpentControl()
        {
            TurnController turn;
            if (relationship.State != RelationshipState.Unmounted ||
                !TryReachExpectedNativeTurn(
                    rider,
                    true,
                    "combat-mount-after-horse-spent",
                    out turn) ||
                !IsStableRiderTurn(turn))
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            riderStandardBeforeMount = rider.CombatState.Cooldown.StandardAction;
            riderMoveBeforeMount = rider.CombatState.Cooldown.MoveAction;
            mountStandardBeforeMount = horse.CombatState.Cooldown.StandardAction;
            mountMoveBeforeMount = horse.CombatState.Cooldown.MoveAction;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            if (!TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "tb-combat-mount-horse-spent"))
            {
                FailCurrent("mount-in-combat-mount-already-acted", "Combat Mount was not admitted after the Horse spent its native Standard action.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitMountSpentCombatMount;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitMountSpentCombatMount()
        {
            if (relationship.State != RelationshipState.Mounted ||
                !relationship.Runtime.PoseFrameApplied || !rider.Commands.Empty || !horse.Commands.Empty)
            {
                stableFrames = 0;
                return;
            }
            stableFrames++;
            if (stableFrames < 2)
            {
                return;
            }

            var evidence = CaptureRawLedgerMountEvidence();
            AddRow(
                "mount-in-combat-mount-already-acted",
                mountStandardBeforeMount >= 2.9f &&
                    Math.Abs(horse.CombatState.Cooldown.StandardAction - mountStandardBeforeMount) <= 0.001f &&
                    Math.Abs(horse.CombatState.Cooldown.MoveAction - mountMoveBeforeMount) <= 0.001f &&
                    Math.Abs(rider.CombatState.Cooldown.StandardAction - riderStandardBeforeMount) <= 0.001f &&
                    rider.CombatState.Cooldown.MoveAction >= riderMoveBeforeMount + 2.9f,
                "Combat Mount preserved the Horse's already-spent Standard action and charged only the rider Move ledger.",
                evidence);
            BeginCleanup();
        }

        private JObject CaptureRawLedgerMountEvidence()
        {
            return new JObject
            {
                ["before"] = new JObject
                {
                    ["riderStandard"] = riderStandardBeforeMount,
                    ["riderMove"] = riderMoveBeforeMount,
                    ["mountStandard"] = mountStandardBeforeMount,
                    ["mountMove"] = mountMoveBeforeMount
                },
                ["after"] = new JObject
                {
                    ["riderStandard"] = rider.CombatState?.Cooldown.StandardAction,
                    ["riderMove"] = rider.CombatState?.Cooldown.MoveAction,
                    ["mountStandard"] = horse.CombatState?.Cooldown.StandardAction,
                    ["mountMove"] = horse.CombatState?.Cooldown.MoveAction
                },
                ["round"] = Game.Instance.TurnBasedCombatController?.RoundNumber,
                ["currentTurnUnitId"] = Game.Instance.TurnBasedCombatController?.CurrentTurn?.Unit?.UniqueId,
                ["relationshipState"] = relationship.State.ToString()
            };
        }

        private bool IsCombatReady(bool requireMounted)
        {
            return target != null && target.IsInState && target.Descriptor.State.IsConscious &&
                rider.IsInCombat && horse.IsInCombat && target.IsInCombat &&
                rider.CombatState != null && horse.CombatState != null && target.CombatState != null &&
                rider.CombatState.Prepared && horse.CombatState.Prepared && target.CombatState.Prepared &&
                (!requireMounted || relationship.State == RelationshipState.Mounted &&
                    relationship.Rider == rider && relationship.Mount == horse);
        }

        private bool IsExactRealTimePrimaryAdmissionReady(
            NativeMountedControlKind kind,
            UnitEntityData actionActor,
            string observationName)
        {
            var game = Game.Instance;
            var handsEquipment = game?.HandsEquipmentController;
            var selection = SelectionManager.Instance?.SelectedUnits;
            var availability = nativeControls.Evaluate(kind, rider);
            var exactActionActor = actionActor != null &&
                (kind == NativeMountedControlKind.RiderPrimary && actionActor == rider ||
                 kind == NativeMountedControlKind.MountPrimary && actionActor == horse);
            var relationshipExact = relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == horse;
            var modeExact = !CombatController.IsInTurnBasedCombat();
            var gameUnpaused = game != null && !game.IsPaused;
            var selectionExact = selection != null && selection.Count == 1 && selection[0] == rider;
            var targetReady = target != null && target.IsInState && target.Descriptor.State.IsConscious &&
                actionActor != null && actionActor.IsEnemy(target) && actionActor.CanAttack(target);
            var combatMemoryReady = targetService != null && targetService.RefreshBidirectionalCombatMemoryLease();
            var riderPrepared = rider.CombatState != null && rider.CombatState.Prepared;
            var riderCanAct = rider.Descriptor.State.CanAct && riderPrepared && rider.CombatState.CanActInCombat;
            var riderInitiativeReady = riderPrepared &&
                Math.Abs(rider.CombatState.Cooldown.Initiative) <= 0.000001f;
            var horsePrepared = horse.CombatState != null && horse.CombatState.Prepared;
            var horseCanAct = horse.Descriptor.State.CanAct && horsePrepared && horse.CombatState.CanActInCombat;
            var horseInitiativeReady = horsePrepared &&
                Math.Abs(horse.CombatState.Cooldown.Initiative) <= 0.000001f;
            var riderCommandsIdle = rider.Commands != null && rider.Commands.Empty;
            var horseCommandsIdle = horse.Commands != null && horse.Commands.Empty;
            var riderHandsIdle = !rider.AreHandsBusyWithAnimation;
            var horseHandsIdle = !horse.AreHandsBusyWithAnimation;
            var equipmentControllerReady = handsEquipment != null;
            var riderEquipmentIdle = equipmentControllerReady && !handsEquipment.IsUpdateScheduledFor(rider);
            var horseEquipmentIdle = equipmentControllerReady && !handsEquipment.IsUpdateScheduledFor(horse);
            var actionStandardReady = actionActor != null && actionActor.HasStandardAction();
            var transactionIdle = !combat.HasActiveCommand && !combat.HasActiveGroundMovement &&
                !combat.HasStockAttackIntent;
            var allPassed = availability.IsEnabled && exactActionActor && relationshipExact && modeExact &&
                gameUnpaused && selectionExact && targetReady && combatMemoryReady && riderCanAct &&
                riderInitiativeReady && horseCanAct && horseInitiativeReady && riderCommandsIdle &&
                horseCommandsIdle && riderHandsIdle && horseHandsIdle && equipmentControllerReady &&
                riderEquipmentIdle && horseEquipmentIdle && actionStandardReady && transactionIdle &&
                relationship.Runtime.PoseHealthy;

            observations[observationName] = new JObject
            {
                ["allPassed"] = allPassed,
                ["kind"] = kind.ToString(),
                ["frame"] = frame,
                ["stableFrames"] = stableFrames,
                ["availabilityEnabled"] = availability.IsEnabled,
                ["availabilityReason"] = availability.Reason,
                ["exactActionActor"] = exactActionActor,
                ["actionActorId"] = actionActor?.UniqueId,
                ["relationshipExact"] = relationshipExact,
                ["modeRealTime"] = modeExact,
                ["gameUnpaused"] = gameUnpaused,
                ["selectionExact"] = selectionExact,
                ["targetReady"] = targetReady,
                ["combatMemoryReady"] = combatMemoryReady,
                ["riderPrepared"] = riderPrepared,
                ["riderCanAct"] = riderCanAct,
                ["riderCanActInCombat"] = rider.CombatState?.CanActInCombat,
                ["riderInitiative"] = rider.CombatState?.Cooldown.Initiative,
                ["riderInitiativeReady"] = riderInitiativeReady,
                ["horsePrepared"] = horsePrepared,
                ["horseCanAct"] = horseCanAct,
                ["horseCanActInCombat"] = horse.CombatState?.CanActInCombat,
                ["horseInitiative"] = horse.CombatState?.Cooldown.Initiative,
                ["horseInitiativeReady"] = horseInitiativeReady,
                ["riderCommandsIdle"] = riderCommandsIdle,
                ["horseCommandsIdle"] = horseCommandsIdle,
                ["riderFreeCommand"] = rider.Commands?.Free?.GetType().FullName,
                ["riderStandardCommand"] = rider.Commands?.Standard?.GetType().FullName,
                ["horseFreeCommand"] = horse.Commands?.Free?.GetType().FullName,
                ["horseStandardCommand"] = horse.Commands?.Standard?.GetType().FullName,
                ["riderHandsIdle"] = riderHandsIdle,
                ["horseHandsIdle"] = horseHandsIdle,
                ["equipmentControllerReady"] = equipmentControllerReady,
                ["riderEquipmentIdle"] = riderEquipmentIdle,
                ["horseEquipmentIdle"] = horseEquipmentIdle,
                ["actionStandardReady"] = actionStandardReady,
                ["transactionIdle"] = transactionIdle,
                ["poseHealthy"] = relationship.Runtime.PoseHealthy
            };
            return allPassed;
        }

        private bool IsStableRiderTurn(TurnController turn)
        {
            return turn?.Unit == rider &&
                (turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing) &&
                rider.Commands.Empty && horse.Commands.Empty;
        }

        private bool CaptureNativeTurnTraversalRoster(CombatController controller)
        {
            if (nativeTurnTraversalRosterCaptureCount != 0)
            {
                return nativeTurnTraversalRoster.Length >= 3;
            }
            if (controller == null || target == null || targetService?.NonPairPartyAiLease == null ||
                !targetService.NonPairPartyAiLease.IsActive ||
                !targetService.NonPairPartyAiLease.ValidateActive())
            {
                return false;
            }

            var roster = controller.SortedUnits.Where(unit => unit != null).ToArray();
            if (roster.Length < 3 ||
                roster.Count(unit => ReferenceEquals(unit, rider)) != 1 ||
                roster.Count(unit => ReferenceEquals(unit, horse)) != 1 ||
                roster.Count(unit => ReferenceEquals(unit, target)) != 1 ||
                roster.Select(unit => unit.UniqueId).Distinct(StringComparer.Ordinal).Count() != roster.Length)
            {
                return false;
            }

            nativeTurnTraversalRosterEvidence.Clear();
            for (var index = 0; index < roster.Length; index++)
            {
                var unit = roster[index];
                var isRider = ReferenceEquals(unit, rider);
                var isMount = ReferenceEquals(unit, horse);
                var isTarget = ReferenceEquals(unit, target);
                var samePlayerParty = unit.Group != null &&
                    ReferenceEquals(unit.Group, rider.Group) && rider.Group.IsPlayerParty;
                var leasedNonPair = !isRider && !isMount &&
                    targetService.NonPairPartyAiLease.OwnsExactMember(unit);
                if (unit.IsDirectlyControllable && samePlayerParty && !isRider && !isMount &&
                    !leasedNonPair)
                {
                    return false;
                }

                nativeTurnTraversalRosterEvidence.Add(new JObject
                {
                    ["index"] = index,
                    ["unitId"] = unit.UniqueId,
                    ["role"] = DescribeNativeTurnTraversalRole(unit),
                    ["directlyControllable"] = unit.IsDirectlyControllable,
                    ["samePlayerParty"] = samePlayerParty,
                    ["nonPairLeaseReferenceExact"] = leasedNonPair,
                    ["targetExact"] = isTarget
                });
            }

            nativeTurnTraversalRoster = roster;
            nativeTurnTraversalRosterCaptureCount++;
            logger.Info("Captured exact native TB traversal roster: count=" + roster.Length +
                "; units=" + string.Join(",", roster.Select(unit => unit.UniqueId).ToArray()) + ".");
            return true;
        }

        private bool TryReachExpectedNativeTurn(
            UnitEntityData expected,
            bool allowOtherPairActor,
            string purpose,
            out TurnController turn)
        {
            var game = Game.Instance;
            var controller = game?.TurnBasedCombatController;
            turn = controller?.CurrentTurn;
            nativeTurnTraversalPurpose = purpose;
            nativeTurnTraversalExpectedUnitId = expected?.UniqueId;

            if (cleanupStarted || expected == null ||
                !ReferenceEquals(expected, rider) && !ReferenceEquals(expected, horse) ||
                nativeTurnTraversalRosterCaptureCount != 1 ||
                nativeTurnTraversalRoster.Length < 3)
            {
                ResetNativeTurnTraversalCandidate();
                return false;
            }
            if (ReferenceEquals(turn?.Unit, expected))
            {
                ResetNativeTurnTraversalCandidate();
                return true;
            }
            if (turn?.Unit == null)
            {
                ResetNativeTurnTraversalCandidate();
                return false;
            }

            var current = turn.Unit;
            var currentIsRider = ReferenceEquals(current, rider);
            var currentIsMount = ReferenceEquals(current, horse);
            var currentIsPairActor = currentIsRider || currentIsMount;
            if (DiagnosticTurnTraversalPolicy.IsProhibitedMountedMountTurn(
                    currentIsMount,
                    ReferenceEquals(current, expected),
                    relationship.State == RelationshipState.Mounted))
            {
                nativeTurnTraversalMountedHorseTurnObservedCount++;
                FailCurrent(
                    "phase3d-horse-runtime-exception",
                    "The native controller emitted the mounted Horse as CurrentTurn.Unit while the diagnostic awaited " +
                    purpose + ".");
                BeginCleanup();
                return false;
            }

            var rosterIndex = -1;
            var rosterReferenceCount = 0;
            for (var index = 0; index < nativeTurnTraversalRoster.Length; index++)
            {
                if (ReferenceEquals(nativeTurnTraversalRoster[index], current))
                {
                    rosterReferenceCount++;
                    rosterIndex = index;
                }
            }
            var rosterReferenceExact = rosterReferenceCount == 1;
            var samePlayerParty = current.Group != null &&
                ReferenceEquals(current.Group, rider.Group) && rider.Group.IsPlayerParty;
            var directlyControllable = current.IsDirectlyControllable;
            var pairActorPassAuthorized = currentIsPairActor && allowOtherPairActor &&
                relationship.State == RelationshipState.Unmounted;
            var nonPairLeaseReferenceExact = false;
            if (!currentIsPairActor && directlyControllable && samePlayerParty)
            {
                var nonPairLease = targetService?.NonPairPartyAiLease;
                nonPairLeaseReferenceExact = nonPairLease != null && nonPairLease.IsActive &&
                    nonPairLease.OwnsExactMember(current) && nonPairLease.ValidateActive();
            }

            if (directlyControllable && samePlayerParty &&
                (!rosterReferenceExact || currentIsPairActor && !pairActorPassAuthorized ||
                 !currentIsPairActor && !nonPairLeaseReferenceExact))
            {
                nativeTurnTraversalForeignTurnRejectCount++;
                FailCurrent(
                    "phase3d-horse-runtime-exception",
                    "Diagnostic native turn traversal rejected a foreign or unauthorized player-party turn: unit=" +
                    current.UniqueId + "; purpose=" + purpose + ".");
                BeginCleanup();
                return false;
            }

            var equipment = game?.HandsEquipmentController;
            var actionableTurn = turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing;
            var commandsIdle = current.Commands != null && current.Commands.Empty;
            var handsIdle = !current.AreHandsBusyWithAnimation;
            var equipmentIdle = equipment != null && !equipment.IsUpdateScheduledFor(current);
            var pairWorkIdle = !combat.HasActiveCommand && !combat.HasStockAttackIntent &&
                !combat.HasActiveGroundMovement && !combat.HasExactMountMovement;
            var pendingNextUnitClear = GetPendingNextUnit(controller) == null;
            var waitingForUiClear = controller != null && !(bool)controller.WaitingForUI &&
                controller.WaitingForUI.GuardCount == 0;
            var defaultUnpausedTurnBasedMode = game != null && game.CurrentMode == GameModeType.Default &&
                !game.IsPaused && CombatController.IsInTurnBasedCombat();
            var observedTurn = turn;
            var alreadyEnded = nativeTurnTraversalEndedTurns.Any(item => ReferenceEquals(item, observedTurn));
            if (alreadyEnded && actionableTurn)
            {
                nativeTurnTraversalDuplicateTurnRejectCount++;
                FailCurrent(
                    "phase3d-horse-runtime-exception",
                    "Diagnostic native turn traversal encountered an already-ended turn reference as actionable: unit=" +
                    current.UniqueId + "; purpose=" + purpose + ".");
                BeginCleanup();
                return false;
            }

            var identityAndReadinessExact = DiagnosticTurnTraversalPolicy.CanForceEndExactFixtureTurn(
                true,
                false,
                currentIsPairActor,
                pairActorPassAuthorized,
                rosterReferenceExact,
                nonPairLeaseReferenceExact,
                directlyControllable,
                samePlayerParty,
                actionableTurn,
                commandsIdle,
                handsIdle,
                equipmentIdle,
                pairWorkIdle,
                pendingNextUnitClear,
                waitingForUiClear,
                defaultUnpausedTurnBasedMode,
                alreadyEnded,
                DiagnosticTurnTraversalPolicy.RequiredStableFrames);
            if (!identityAndReadinessExact)
            {
                ResetNativeTurnTraversalCandidate();
                return false;
            }

            if (!ReferenceEquals(nativeTurnTraversalCandidate, turn) ||
                !string.Equals(nativeTurnTraversalCandidatePurpose, purpose, StringComparison.Ordinal))
            {
                nativeTurnTraversalCandidate = turn;
                nativeTurnTraversalCandidatePurpose = purpose;
                nativeTurnTraversalStableFrames = 0;
            }
            nativeTurnTraversalStableFrames++;
            if (!DiagnosticTurnTraversalPolicy.CanForceEndExactFixtureTurn(
                    true,
                    false,
                    currentIsPairActor,
                    pairActorPassAuthorized,
                    rosterReferenceExact,
                    nonPairLeaseReferenceExact,
                    directlyControllable,
                    samePlayerParty,
                    actionableTurn,
                    commandsIdle,
                    handsIdle,
                    equipmentIdle,
                    pairWorkIdle,
                    pendingNextUnitClear,
                    waitingForUiClear,
                    defaultUnpausedTurnBasedMode,
                    alreadyEnded,
                    nativeTurnTraversalStableFrames))
            {
                return false;
            }

            var combatState = current.CombatState;
            if (combatState == null)
            {
                ResetNativeTurnTraversalCandidate();
                return false;
            }
            var standardBefore = combatState.Cooldown.StandardAction;
            var moveBefore = combatState.Cooldown.MoveAction;
            var initiativeBefore = combatState.Cooldown.Initiative;
            var stableFramesAtInput = nativeTurnTraversalStableFrames;
            var roundAtInput = controller.RoundNumber;
            var statusBefore = turn.Status.ToString();
            var isActingBefore = turn.IsActing;
            nativeTurnTraversalEndedTurns.Add(turn);
            turn.ForceToEnd(false);
            nativeTurnTraversalForceEndCount++;
            var standardAfter = combatState.Cooldown.StandardAction;
            var moveAfter = combatState.Cooldown.MoveAction;
            var initiativeAfter = combatState.Cooldown.Initiative;
            var resourcesUnchanged = Math.Abs(standardAfter - standardBefore) <= 0.001f &&
                Math.Abs(moveAfter - moveBefore) <= 0.001f &&
                Math.Abs(initiativeAfter - initiativeBefore) <= 0.001f;
            var currentTurnReferenceRetained = ReferenceEquals(controller.CurrentTurn, turn);
            var unitReferenceRetained = ReferenceEquals(turn.Unit, current);
            nativeTurnTraversalEntries.Add(new JObject
            {
                ["sequence"] = nativeTurnTraversalForceEndCount,
                ["purpose"] = purpose,
                ["frame"] = Time.frameCount,
                ["round"] = roundAtInput,
                ["expectedUnitId"] = expected.UniqueId,
                ["unitId"] = current.UniqueId,
                ["role"] = DescribeNativeTurnTraversalRole(current),
                ["rosterIndex"] = rosterIndex,
                ["relationshipState"] = relationship.State.ToString(),
                ["referenceExact"] = rosterReferenceExact,
                ["nonPairLeaseReferenceExact"] = nonPairLeaseReferenceExact,
                ["pairActorPassAuthorized"] = pairActorPassAuthorized,
                ["directlyControllable"] = directlyControllable,
                ["samePlayerParty"] = samePlayerParty,
                ["statusBefore"] = statusBefore,
                ["isActingBefore"] = isActingBefore,
                ["commandsIdle"] = commandsIdle,
                ["handsIdle"] = handsIdle,
                ["equipmentIdle"] = equipmentIdle,
                ["pairWorkIdle"] = pairWorkIdle,
                ["pendingNextUnitClear"] = pendingNextUnitClear,
                ["waitingForUiClear"] = waitingForUiClear,
                ["stableFrames"] = stableFramesAtInput,
                ["alreadyEnded"] = alreadyEnded,
                ["forceToEndArgument"] = false,
                ["statusAfter"] = turn.Status.ToString(),
                ["currentTurnReferenceRetained"] = currentTurnReferenceRetained,
                ["unitReferenceRetained"] = unitReferenceRetained,
                ["standardBefore"] = standardBefore,
                ["standardAfter"] = standardAfter,
                ["moveBefore"] = moveBefore,
                ["moveAfter"] = moveAfter,
                ["initiativeBefore"] = initiativeBefore,
                ["initiativeAfter"] = initiativeAfter,
                ["resourcesUnchanged"] = resourcesUnchanged
            });
            logger.Info("Diagnostic native turn traversal ended exact idle fixture turn: unitId=" +
                current.UniqueId + "; role=" + DescribeNativeTurnTraversalRole(current) +
                "; round=" + roundAtInput + "; purpose=" + purpose + ".");
            ResetNativeTurnTraversalCandidate();

            if (!resourcesUnchanged || !currentTurnReferenceRetained || !unitReferenceRetained ||
                turn.Status != TurnController.TurnStatus.Ending)
            {
                nativeTurnTraversalResourceMutationCount++;
                FailCurrent(
                    "phase3d-horse-runtime-exception",
                    "Native ForceToEnd(false) violated exact diagnostic traversal postconditions for " +
                    current.UniqueId + ".");
                BeginCleanup();
            }
            return false;
        }

        private void ResetNativeTurnTraversalCandidate()
        {
            nativeTurnTraversalCandidate = null;
            nativeTurnTraversalCandidatePurpose = null;
            nativeTurnTraversalStableFrames = 0;
        }

        private string DescribeNativeTurnTraversalRole(UnitEntityData unit)
        {
            if (ReferenceEquals(unit, rider))
            {
                return "Rider";
            }
            if (ReferenceEquals(unit, horse))
            {
                return "Mount";
            }
            if (ReferenceEquals(unit, target))
            {
                return "DiagnosticTarget";
            }
            return unit != null && ReferenceEquals(unit.Group, rider.Group) && rider.Group.IsPlayerParty &&
                unit.IsDirectlyControllable
                ? "NonPairPlayerParty"
                : "Other";
        }

        private JObject CaptureNativeTurnTraversalEvidence()
        {
            return new JObject
            {
                ["rosterCaptured"] = nativeTurnTraversalRosterCaptureCount == 1,
                ["rosterCaptureCount"] = nativeTurnTraversalRosterCaptureCount,
                ["roster"] = nativeTurnTraversalRosterEvidence.DeepClone(),
                ["forceEndCallCount"] = nativeTurnTraversalForceEndCount,
                ["duplicateTurnRejectCount"] = nativeTurnTraversalDuplicateTurnRejectCount,
                ["foreignTurnRejectCount"] = nativeTurnTraversalForeignTurnRejectCount,
                ["resourceMutationCount"] = nativeTurnTraversalResourceMutationCount,
                ["mountedHorseTurnObservedCount"] = nativeTurnTraversalMountedHorseTurnObservedCount,
                ["entries"] = nativeTurnTraversalEntries.DeepClone(),
                ["lastProgress"] = CaptureNativeTurnTraversalProgress()
            };
        }

        private JObject CaptureNativeTurnTraversalProgress()
        {
            var game = Game.Instance;
            var controller = game?.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            var current = turn?.Unit;
            var equipment = game?.HandsEquipmentController;
            var rosterReferenceCount = current == null
                ? 0
                : nativeTurnTraversalRoster.Count(unit => ReferenceEquals(unit, current));
            var alreadyEnded = turn != null &&
                nativeTurnTraversalEndedTurns.Any(item => ReferenceEquals(item, turn));
            var nonPairLeaseReferenceExact = false;
            try
            {
                nonPairLeaseReferenceExact = current != null && current != rider && current != horse &&
                    targetService?.NonPairPartyAiLease != null &&
                    targetService.NonPairPartyAiLease.IsActive &&
                    targetService.NonPairPartyAiLease.OwnsExactMember(current);
            }
            catch
            {
                nonPairLeaseReferenceExact = false;
            }
            return new JObject
            {
                ["step"] = step.ToString(),
                ["frame"] = Time.frameCount,
                ["purpose"] = nativeTurnTraversalPurpose,
                ["expectedUnitId"] = nativeTurnTraversalExpectedUnitId,
                ["relationshipState"] = relationship.State.ToString(),
                ["rosterCaptured"] = nativeTurnTraversalRosterCaptureCount == 1,
                ["rosterUnitIds"] = new JArray(nativeTurnTraversalRoster.Select(unit => unit.UniqueId)),
                ["currentTurnPresent"] = turn != null,
                ["currentTurnUnitId"] = current?.UniqueId,
                ["currentTurnStatus"] = turn?.Status.ToString(),
                ["currentTurnIsActing"] = turn != null && turn.IsActing,
                ["currentReferenceExact"] = rosterReferenceCount == 1,
                ["currentIsRider"] = ReferenceEquals(current, rider),
                ["currentIsHorse"] = ReferenceEquals(current, horse),
                ["currentIsTarget"] = ReferenceEquals(current, target),
                ["currentDirectlyControllable"] = current != null && current.IsDirectlyControllable,
                ["currentSamePlayerParty"] = current?.Group != null &&
                    ReferenceEquals(current.Group, rider.Group) &&
                    rider.Group.IsPlayerParty,
                ["currentNonPairLeaseReferenceExact"] = nonPairLeaseReferenceExact,
                ["currentCommandsIdle"] = current?.Commands != null && current.Commands.Empty,
                ["currentHandsIdle"] = current != null && !current.AreHandsBusyWithAnimation,
                ["currentEquipmentIdle"] = current != null && equipment != null &&
                    !equipment.IsUpdateScheduledFor(current),
                ["pairWorkIdle"] = !combat.HasActiveCommand && !combat.HasStockAttackIntent &&
                    !combat.HasActiveGroundMovement && !combat.HasExactMountMovement,
                ["pendingNextUnitId"] = GetPendingNextUnit(controller)?.UniqueId,
                ["pendingNextUnitClear"] = GetPendingNextUnit(controller) == null,
                ["waitingForUi"] = controller != null && (bool)controller.WaitingForUI,
                ["waitingForUiGuardCount"] = controller?.WaitingForUI?.GuardCount ?? -1,
                ["candidateStableFrames"] = nativeTurnTraversalStableFrames,
                ["alreadyEnded"] = alreadyEnded,
                ["forceEndCallCount"] = nativeTurnTraversalForceEndCount,
                ["duplicateTurnRejectCount"] = nativeTurnTraversalDuplicateTurnRejectCount,
                ["foreignTurnRejectCount"] = nativeTurnTraversalForeignTurnRejectCount,
                ["resourceMutationCount"] = nativeTurnTraversalResourceMutationCount,
                ["mountedHorseTurnObservedCount"] = nativeTurnTraversalMountedHorseTurnObservedCount
            };
        }

        private bool IsRangedOpportunityControlReady()
        {
            var game = Game.Instance;
            var selected = SelectionManager.Instance?.SelectedUnits;
            var equipment = game?.HandsEquipmentController;
            return target != null && target.CombatState != null && rider.CombatState != null &&
                target.IsInState && rider.IsInState && target.IsEnemy(rider) &&
                target.CombatState.CanActInCombat && target.CombatState.CanAttackOfOpportunity &&
                target.CombatState.AttackOfOpportunityCount > 0 && target.Descriptor.State.CanAct &&
                target.GetThreatHand() != null && target.IsEngage(rider) &&
                target.CombatState.EngagedUnits.Contains(rider) &&
                rider.CombatState.EngagedBy.Contains(target) && rider.Memory.Contains(target) &&
                !target.HasMotionThisTick && rider.HasLOS(target) &&
                relationship.State == RelationshipState.Mounted &&
                !CombatController.IsInTurnBasedCombat() && game != null && !game.IsPaused &&
                selected != null && selected.Count == 1 && selected[0] == rider &&
                !combat.HasActiveCommand && !combat.HasActiveGroundMovement &&
                !combat.HasExactMountMovement && !combat.HasStockAttackIntent &&
                rider.HasStandardAction() && rider.Commands != null && rider.Commands.Empty &&
                horse.Commands != null && horse.Commands.Empty && target.Commands != null && target.Commands.Empty &&
                !rider.AreHandsBusyWithAnimation && !horse.AreHandsBusyWithAnimation &&
                !target.AreHandsBusyWithAnimation && equipment != null &&
                !equipment.IsUpdateScheduledFor(rider) && !equipment.IsUpdateScheduledFor(horse) &&
                target.CombatState.AttackOfOpportunity(rider, true);
        }

        private void ObserveCurrentRangedRiderOutcome()
        {
            var outcome = combat.LastOutcome;
            if (outcome != null && !ReferenceEquals(outcome, rangedRiderOutcomeBaseline) &&
                outcome.Action == MountedCombatActionKind.RiderRanged &&
                string.Equals(outcome.TargetId, target?.UniqueId, StringComparison.Ordinal))
            {
                rangedRiderOutcome = outcome;
            }
        }

        private JObject CaptureStockMeleeReadiness()
        {
            var game = Game.Instance;
            var selected = SelectionManager.Instance?.SelectedUnits;
            var equipment = game?.HandsEquipmentController;
            var weapon = rider.GetFirstWeapon()?.Blueprint;
            var targetReady = target != null && target.IsInState && target.Descriptor.State.IsConscious &&
                rider.IsEnemy(target) && rider.CanAttack(target) && horse.IsEnemy(target) && horse.CanAttack(target);
            var combatMemoryReady = targetService != null &&
                targetService.RefreshBidirectionalCombatMemoryLease();
            var relationshipExact = relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == horse;
            var freshTarget = stockMeleePreviousTargetCleanupPassed &&
                !string.IsNullOrWhiteSpace(stockMeleePreviousTargetId) &&
                target != null && !string.Equals(stockMeleePreviousTargetId, target.UniqueId, StringComparison.Ordinal);
            var riderHasLineOfSight = target != null && rider.HasLOS(target);
            var horseHasLineOfSight = target != null && horse.HasLOS(target);
            var ready = IsCombatReady(true) && relationshipExact && !CombatController.IsInTurnBasedCombat() &&
                game != null && !game.IsPaused && selected != null && selected.Count == 1 && selected[0] == rider &&
                weapon != null && !weapon.IsRanged && targetReady && combatMemoryReady && freshTarget &&
                !combat.HasActiveCommand && !combat.HasActiveGroundMovement && !combat.HasExactMountMovement &&
                !combat.HasStockAttackIntent && rider.HasStandardAction() && horse.HasStandardAction() &&
                rider.Commands != null && rider.Commands.Empty && horse.Commands != null && horse.Commands.Empty &&
                target.Commands != null && target.Commands.Empty && !rider.AreHandsBusyWithAnimation &&
                !horse.AreHandsBusyWithAnimation && !target.AreHandsBusyWithAnimation && equipment != null &&
                !equipment.IsUpdateScheduledFor(rider) && !equipment.IsUpdateScheduledFor(horse) &&
                relationship.Runtime.PoseHealthy;
            return new JObject
            {
                ["ready"] = ready,
                ["relationshipMounted"] = relationship.State == RelationshipState.Mounted,
                ["relationshipExact"] = relationshipExact,
                ["modeRealTime"] = !CombatController.IsInTurnBasedCombat(),
                ["gameUnpaused"] = game != null && !game.IsPaused,
                ["riderSelectedPrincipal"] = selected != null && selected.Count == 1 && selected[0] == rider,
                ["weaponGuid"] = weapon?.AssetGuid,
                ["weaponCategory"] = weapon?.Category.ToString(),
                ["weaponMelee"] = weapon != null && !weapon.IsRanged,
                ["targetReady"] = targetReady,
                ["combatMemoryReady"] = combatMemoryReady,
                ["pairCommandIdle"] = !combat.HasActiveCommand,
                ["pairGroundMovementIdle"] = !combat.HasActiveGroundMovement,
                ["exactMountMovementIdle"] = !combat.HasExactMountMovement,
                ["stockIntentIdle"] = !combat.HasStockAttackIntent,
                ["riderStandardReady"] = rider.HasStandardAction(),
                ["horseStandardReady"] = horse.HasStandardAction(),
                ["riderCommandsIdle"] = rider.Commands != null && rider.Commands.Empty,
                ["horseCommandsIdle"] = horse.Commands != null && horse.Commands.Empty,
                ["targetCommandsIdle"] = target?.Commands != null && target.Commands.Empty,
                ["riderHandsIdle"] = !rider.AreHandsBusyWithAnimation,
                ["horseHandsIdle"] = !horse.AreHandsBusyWithAnimation,
                ["targetHandsIdle"] = target != null && !target.AreHandsBusyWithAnimation,
                ["equipmentControllerReady"] = equipment != null,
                ["riderEquipmentIdle"] = equipment != null && !equipment.IsUpdateScheduledFor(rider),
                ["horseEquipmentIdle"] = equipment != null && !equipment.IsUpdateScheduledFor(horse),
                ["poseHealthy"] = relationship.Runtime.PoseHealthy,
                ["previousTargetId"] = stockMeleePreviousTargetId,
                ["previousTargetCleanupPassed"] = stockMeleePreviousTargetCleanupPassed,
                ["isolatedTargetId"] = target?.UniqueId,
                ["freshTarget"] = freshTarget,
                ["riderDistanceToTarget"] = target == null ? (double?)null : rider.DistanceTo(target),
                ["horseDistanceToTarget"] = target == null ? (double?)null : horse.DistanceTo(target),
                ["pairMechanicsDistanceToTarget"] = target == null
                    ? (double?)null
                    : GeometryUtils.MechanicsDistance(horse.Position, target.Position),
                ["riderHasLineOfSight"] = riderHasLineOfSight,
                ["horseHasLineOfSight"] = horseHasLineOfSight
            };
        }

        private JObject CaptureStockMeleeProgress()
        {
            var lastOutcome = combat.LastOutcome;
            var movementAgent = horse.View?.AgentASP;
            var pairMechanicsDistance = target == null
                ? (float?)null
                : GeometryUtils.MechanicsDistance(horse.Position, target.Position);
            var pairRadius = lastOutcome == null ? (float?)null : lastOutcome.PairApproachRadiusAtStart;
            return new JObject
            {
                ["previousTargetId"] = stockMeleePreviousTargetId,
                ["previousTargetCleanupPassed"] = stockMeleePreviousTargetCleanupPassed,
                ["isolatedTargetId"] = target?.UniqueId,
                ["targetInState"] = target != null && target.IsInState,
                ["targetConscious"] = target != null && target.Descriptor.State.IsConscious,
                ["activePairCommand"] = combat.HasActiveCommand,
                ["stockIntentActive"] = combat.HasStockAttackIntent,
                ["nativeRequestDelta"] = combat.StockAttackNativeRequestCount - stockNativeBefore,
                ["intentStartDelta"] = combat.StockAttackIntentStartCount - stockIntentBefore,
                ["riderDispatchDelta"] = combat.StockAttackRiderDispatchCount - stockRiderBefore,
                ["mountDispatchDelta"] = combat.StockAttackMountDispatchCount - stockMountBefore,
                ["duplicateDispatchDelta"] = combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore,
                ["horseMovementDistanceAfterAdmission"] = HorizontalDistance(movementStart, horse.Position),
                ["lastOutcomeMatchesIsolatedTarget"] = lastOutcome != null && target != null &&
                    string.Equals(lastOutcome.TargetId, target.UniqueId, StringComparison.Ordinal),
                ["lastOutcome"] = lastOutcome == null
                    ? null
                    : JObject.FromObject(lastOutcome, JsonSerializer.Create(JsonSettings)),
                ["currentSpatial"] = new JObject
                {
                    ["riderPosition"] = CapturePosition(rider.Position),
                    ["horsePosition"] = CapturePosition(horse.Position),
                    ["targetPosition"] = target == null ? null : CapturePosition(target.Position),
                    ["riderHorizontalDistanceToTarget"] = target == null
                        ? (double?)null
                        : HorizontalDistance(rider.Position, target.Position),
                    ["horseHorizontalDistanceToTarget"] = target == null
                        ? (double?)null
                        : HorizontalDistance(horse.Position, target.Position),
                    ["pairMechanicsDistanceToTarget"] = pairMechanicsDistance,
                    ["pairApproachRadiusAtTerminal"] = pairRadius,
                    ["pairInsideApproachRadiusAtTerminal"] = pairMechanicsDistance.HasValue && pairRadius.HasValue &&
                        pairMechanicsDistance.Value <= pairRadius.Value + MountedCombatSpatialPolicy.RangeTolerance,
                    ["riderHasLineOfSight"] = target != null && rider.HasLOS(target),
                    ["horseHasLineOfSight"] = target != null && horse.HasLOS(target),
                    ["movementAgentPresent"] = movementAgent != null,
                    ["movementAgentEnabled"] = movementAgent != null && movementAgent.enabled,
                    ["movementAgentWantsToMove"] = movementAgent != null && movementAgent.WantsToMove,
                    ["movementAgentIsReallyMoving"] = movementAgent != null && movementAgent.IsReallyMoving,
                    ["movementAgentAvoidanceDisabled"] = movementAgent != null && movementAgent.AvoidanceDisabled
                },
                ["currentCommands"] = CapturePairCommandState(),
                ["rules"] = ruleProbe?.CapturePairEvidence(),
                ["readinessNow"] = target == null ? null : CaptureStockMeleeReadiness(),
                ["admissionReadiness"] = stockMeleeReadinessAtAdmission?.DeepClone(),
                ["input"] = observations["stockMeleeRtAdmission"]?.DeepClone()
            };
        }

        private static JObject CapturePosition(Vector3 value)
        {
            return new JObject
            {
                ["x"] = value.x,
                ["y"] = value.y,
                ["z"] = value.z
            };
        }

        private JObject CaptureLongRangeRangedReadiness(bool clickLeaseReady)
        {
            var game = Game.Instance;
            var selected = SelectionManager.Instance?.SelectedUnits;
            var uiSelection = game?.UI?.SelectionManager;
            var equipment = game?.HandsEquipmentController;
            var weapon = rider.GetFirstWeapon()?.Blueprint;
            var nearestSelected = uiSelection != null && target?.View != null
                ? uiSelection.GetNearestSelectedUnit(target.View.transform.position)
                : null;
            var targetReady = target != null && target.IsInState && target.Descriptor.State.IsConscious &&
                rider.IsEnemy(target) && rider.CanAttack(target);
            var combatMemoryReady = targetService != null &&
                targetService.RefreshBidirectionalCombatMemoryLease();
            var relationshipExact = relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == horse;
            var selectionManagerExact = uiSelection != null &&
                ReferenceEquals(SelectionManager.Instance, uiSelection);
            var riderSelectedPrincipal = selected != null && selected.Count == 1 && selected[0] == rider;
            var targetVisibleNow = target != null && target.View != null && !target.IsInFogOfWar &&
                target.View.IsVisible && target.IsVisibleForPlayer;
            var targetNotDirectlyControllable = target != null && !target.IsDirectlyControllable;
            var targetOutsideParty = game != null && game.Player != null && target != null &&
                !game.Player.Party.Contains(target);
            var targetNotLoot = target != null && !target.IsDeadAndHasLoot;
            var ready = IsCombatReady(true) && relationshipExact && !CombatController.IsInTurnBasedCombat() &&
                game != null && !game.IsPaused && selectionManagerExact && riderSelectedPrincipal &&
                nearestSelected == rider && rangedWeaponLease != null && rangedWeaponLease.IsReady &&
                weapon != null && weapon.IsRanged && weapon.Category == WeaponCategory.Shortbow &&
                clickLeaseReady && targetService != null && targetService.TargetFogOfWarCleared &&
                targetService.TargetViewVisible && targetService.TargetVisibleForPlayer && targetVisibleNow &&
                targetNotDirectlyControllable && targetOutsideParty && targetNotLoot && targetReady &&
                combatMemoryReady && !combat.HasActiveCommand && !combat.HasActiveGroundMovement &&
                !combat.HasExactMountMovement && !combat.HasStockAttackIntent &&
                rider.Commands != null && rider.Commands.Empty && horse.Commands != null && horse.Commands.Empty &&
                target.Commands != null && target.Commands.Empty && !rider.AreHandsBusyWithAnimation &&
                !horse.AreHandsBusyWithAnimation && !target.AreHandsBusyWithAnimation && equipment != null &&
                !equipment.IsUpdateScheduledFor(rider) && !equipment.IsUpdateScheduledFor(horse) &&
                relationship.Runtime.PoseHealthy;
            return new JObject
            {
                ["ready"] = ready,
                ["relationshipMounted"] = relationship.State == RelationshipState.Mounted,
                ["relationshipExact"] = relationshipExact,
                ["modeRealTime"] = !CombatController.IsInTurnBasedCombat(),
                ["gameUnpaused"] = game != null && !game.IsPaused,
                ["selectionManagerExact"] = selectionManagerExact,
                ["selectionCount"] = selected?.Count ?? 0,
                ["riderSelectedPrincipal"] = riderSelectedPrincipal,
                ["nearestSelectedUnitId"] = nearestSelected?.UniqueId,
                ["nearestSelectedRider"] = nearestSelected == rider,
                ["weaponLeaseReady"] = rangedWeaponLease != null && rangedWeaponLease.IsReady,
                ["weaponGuid"] = weapon?.AssetGuid,
                ["weaponCategory"] = weapon?.Category.ToString(),
                ["weaponRanged"] = weapon != null && weapon.IsRanged,
                ["clickLeaseReady"] = clickLeaseReady,
                ["targetFogOfWarCleared"] = targetService != null && targetService.TargetFogOfWarCleared,
                ["targetViewVisible"] = targetService != null && targetService.TargetViewVisible,
                ["targetVisibleForPlayer"] = targetService != null && targetService.TargetVisibleForPlayer,
                ["targetVisibleNow"] = targetVisibleNow,
                ["targetNotDirectlyControllable"] = targetNotDirectlyControllable,
                ["targetOutsideParty"] = targetOutsideParty,
                ["targetNotLoot"] = targetNotLoot,
                ["targetReady"] = targetReady,
                ["combatMemoryReady"] = combatMemoryReady,
                ["pairCommandIdle"] = !combat.HasActiveCommand,
                ["pairGroundMovementIdle"] = !combat.HasActiveGroundMovement,
                ["exactMountMovementIdle"] = !combat.HasExactMountMovement,
                ["stockIntentIdle"] = !combat.HasStockAttackIntent,
                ["riderStandardReady"] = rider.HasStandardAction(),
                ["horseStandardReady"] = horse.HasStandardAction(),
                ["riderCommandsIdle"] = rider.Commands != null && rider.Commands.Empty,
                ["horseCommandsIdle"] = horse.Commands != null && horse.Commands.Empty,
                ["targetCommandsIdle"] = target?.Commands != null && target.Commands.Empty,
                ["riderHandsIdle"] = !rider.AreHandsBusyWithAnimation,
                ["horseHandsIdle"] = !horse.AreHandsBusyWithAnimation,
                ["targetHandsIdle"] = target != null && !target.AreHandsBusyWithAnimation,
                ["equipmentControllerReady"] = equipment != null,
                ["riderEquipmentIdle"] = equipment != null && !equipment.IsUpdateScheduledFor(rider),
                ["horseEquipmentIdle"] = equipment != null && !equipment.IsUpdateScheduledFor(horse),
                ["poseHealthy"] = relationship.Runtime.PoseHealthy,
                ["targetId"] = target?.UniqueId,
                ["riderDistanceToTarget"] = target == null ? (double?)null : rider.DistanceTo(target)
            };
        }

        private JObject CaptureLongRangeRangedProgress()
        {
            var clickLeaseReady = targetService != null && targetService.TargetFogOfWarCleared &&
                targetService.TargetViewVisible && targetService.TargetVisibleForPlayer;
            return new JObject
            {
                ["targetId"] = target?.UniqueId,
                ["targetInState"] = target != null && target.IsInState,
                ["targetConscious"] = target != null && target.Descriptor.State.IsConscious,
                ["activePairCommand"] = combat.HasActiveCommand,
                ["stockIntentActive"] = combat.HasStockAttackIntent,
                ["nativeRequestDelta"] = combat.StockAttackNativeRequestCount - stockNativeBefore,
                ["intentStartDelta"] = combat.StockAttackIntentStartCount - stockIntentBefore,
                ["riderDispatchDelta"] = combat.StockAttackRiderDispatchCount - stockRiderBefore,
                ["mountDispatchDelta"] = combat.StockAttackMountDispatchCount - stockMountBefore,
                ["duplicateDispatchDelta"] = combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore,
                ["horseMovementDistanceAfterAdmission"] = HorizontalDistance(movementStart, horse.Position),
                ["rules"] = ruleProbe?.CapturePairEvidence(),
                ["readinessNow"] = target == null
                    ? null
                    : CaptureLongRangeRangedReadiness(clickLeaseReady),
                ["admissionReadiness"] = rangedReadinessAtAdmission?.DeepClone(),
                ["input"] = observations["rangedRtInput"]?.DeepClone()
            };
        }

        private JObject CaptureRangedOpportunityReadiness()
        {
            var targetCombat = target?.CombatState;
            var riderCombat = rider?.CombatState;
            var threat = target?.GetThreatHand()?.Weapon?.Blueprint;
            var game = Game.Instance;
            var selected = SelectionManager.Instance?.SelectedUnits;
            var equipment = game?.HandsEquipmentController;
            var nativeOpportunitySimulationReady = targetCombat != null && rider != null &&
                targetCombat.AttackOfOpportunity(rider, true);
            return new JObject
            {
                ["ready"] = IsRangedOpportunityControlReady(),
                ["targetId"] = target?.UniqueId,
                ["riderId"] = rider?.UniqueId,
                ["targetCanActInCombat"] = targetCombat?.CanActInCombat,
                ["targetCanAttackOfOpportunity"] = targetCombat?.CanAttackOfOpportunity,
                ["targetOpportunityCount"] = targetCombat?.AttackOfOpportunityCount,
                ["targetOpportunityPerRound"] = targetCombat?.AttackOfOpportunityPerRound,
                ["targetThreatWeaponGuid"] = threat?.AssetGuid,
                ["targetThreatsRider"] = target != null && target.IsEngage(rider),
                ["targetTracksRiderEngagement"] = targetCombat != null && targetCombat.EngagedUnits.Contains(rider),
                ["riderTracksTargetThreat"] = riderCombat != null && riderCombat.EngagedBy.Contains(target),
                ["riderMemoryContainsTarget"] = target != null && rider.Memory.Contains(target),
                ["targetHasMotionThisTick"] = target?.HasMotionThisTick,
                ["riderHasLineOfSight"] = target != null && rider.HasLOS(target),
                ["relationshipMounted"] = relationship.State == RelationshipState.Mounted,
                ["modeRealTime"] = !CombatController.IsInTurnBasedCombat(),
                ["gameUnpaused"] = game != null && !game.IsPaused,
                ["riderSelectedPrincipal"] = selected != null && selected.Count == 1 && selected[0] == rider,
                ["pairCommandIdle"] = !combat.HasActiveCommand,
                ["pairGroundMovementIdle"] = !combat.HasActiveGroundMovement,
                ["exactMountMovementIdle"] = !combat.HasExactMountMovement,
                ["stockIntentIdle"] = !combat.HasStockAttackIntent,
                ["riderStandardReady"] = rider.HasStandardAction(),
                ["riderCommandsIdle"] = rider.Commands != null && rider.Commands.Empty,
                ["horseCommandsIdle"] = horse.Commands != null && horse.Commands.Empty,
                ["targetCommandsIdle"] = target?.Commands != null && target.Commands.Empty,
                ["riderStandardCommand"] = rider.Commands?.Standard?.GetType().FullName,
                ["horseStandardCommand"] = horse.Commands?.Standard?.GetType().FullName,
                ["targetStandardCommand"] = target?.Commands?.Standard?.GetType().FullName,
                ["riderHandsIdle"] = !rider.AreHandsBusyWithAnimation,
                ["horseHandsIdle"] = !horse.AreHandsBusyWithAnimation,
                ["targetHandsIdle"] = target != null && !target.AreHandsBusyWithAnimation,
                ["equipmentControllerReady"] = equipment != null,
                ["riderEquipmentIdle"] = equipment != null && !equipment.IsUpdateScheduledFor(rider),
                ["horseEquipmentIdle"] = equipment != null && !equipment.IsUpdateScheduledFor(horse),
                ["nativeOpportunitySimulationReady"] = nativeOpportunitySimulationReady
            };
        }

        private JObject CaptureRangedOpportunityProgress()
        {
            var outcome = rangedRiderOutcome;
            return new JObject
            {
                ["activePairCommand"] = combat.HasActiveCommand,
                ["stockIntentActive"] = combat.HasStockAttackIntent,
                ["riderDispatchDelta"] = combat.StockAttackRiderDispatchCount - stockRiderBefore,
                ["mountDispatchDelta"] = combat.StockAttackMountDispatchCount - stockMountBefore,
                ["duplicateDispatchDelta"] = combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore,
                ["riderOutcomeObserved"] = outcome != null,
                ["riderOutcome"] = outcome == null
                    ? null
                    : JObject.FromObject(outcome, JsonSerializer.Create(JsonSettings)),
                ["rules"] = ruleProbe?.CapturePairEvidence(),
                ["opportunity"] = ruleProbe?.CaptureOpportunityEvidence(),
                ["targetOpportunityCount"] = target?.CombatState?.AttackOfOpportunityCount,
                ["readinessNow"] = CaptureRangedOpportunityReadiness()
            };
        }

        private JObject CaptureRangedVariantReadiness()
        {
            var game = Game.Instance;
            var selected = SelectionManager.Instance?.SelectedUnits;
            var equipment = game?.HandsEquipmentController;
            var weapon = rider.GetFirstWeapon()?.Blueprint;
            var targetReady = target != null && target.IsInState && target.Descriptor.State.IsConscious &&
                rider.IsEnemy(target) && rider.CanAttack(target);
            var combatMemoryReady = targetService != null &&
                targetService.RefreshBidirectionalCombatMemoryLease();
            var ready = IsCombatReady(true) && !CombatController.IsInTurnBasedCombat() &&
                game != null && !game.IsPaused && selected != null && selected.Count == 1 && selected[0] == rider &&
                rangedWeaponLease != null && rangedWeaponLease.IsReady && weapon != null && weapon.IsRanged &&
                weapon.Category == rangedVariantCategory && targetReady && combatMemoryReady &&
                !combat.HasActiveCommand && !combat.HasActiveGroundMovement && !combat.HasExactMountMovement &&
                !combat.HasStockAttackIntent && rider.HasStandardAction() &&
                rider.Commands != null && rider.Commands.Empty && horse.Commands != null && horse.Commands.Empty &&
                target.Commands != null && target.Commands.Empty && !rider.AreHandsBusyWithAnimation &&
                !horse.AreHandsBusyWithAnimation && !target.AreHandsBusyWithAnimation && equipment != null &&
                !equipment.IsUpdateScheduledFor(rider) && !equipment.IsUpdateScheduledFor(horse) &&
                relationship.Runtime.PoseHealthy;
            return new JObject
            {
                ["ready"] = ready,
                ["category"] = rangedVariantCategory.ToString(),
                ["relationshipMounted"] = relationship.State == RelationshipState.Mounted,
                ["modeRealTime"] = !CombatController.IsInTurnBasedCombat(),
                ["gameUnpaused"] = game != null && !game.IsPaused,
                ["riderSelectedPrincipal"] = selected != null && selected.Count == 1 && selected[0] == rider,
                ["weaponLeaseReady"] = rangedWeaponLease != null && rangedWeaponLease.IsReady,
                ["weaponCategory"] = weapon?.Category.ToString(),
                ["targetReady"] = targetReady,
                ["combatMemoryReady"] = combatMemoryReady,
                ["pairCommandIdle"] = !combat.HasActiveCommand,
                ["pairGroundMovementIdle"] = !combat.HasActiveGroundMovement,
                ["exactMountMovementIdle"] = !combat.HasExactMountMovement,
                ["stockIntentIdle"] = !combat.HasStockAttackIntent,
                ["riderStandardReady"] = rider.HasStandardAction(),
                ["riderCommandsIdle"] = rider.Commands != null && rider.Commands.Empty,
                ["horseCommandsIdle"] = horse.Commands != null && horse.Commands.Empty,
                ["targetCommandsIdle"] = target?.Commands != null && target.Commands.Empty,
                ["riderHandsIdle"] = !rider.AreHandsBusyWithAnimation,
                ["horseHandsIdle"] = !horse.AreHandsBusyWithAnimation,
                ["targetHandsIdle"] = target != null && !target.AreHandsBusyWithAnimation,
                ["equipmentControllerReady"] = equipment != null,
                ["riderEquipmentIdle"] = equipment != null && !equipment.IsUpdateScheduledFor(rider),
                ["horseEquipmentIdle"] = equipment != null && !equipment.IsUpdateScheduledFor(horse),
                ["previousTargetId"] = rangedVariantPreviousTargetId,
                ["previousTargetCleanupPassed"] = rangedVariantPreviousTargetCleanupPassed,
                ["isolatedTargetId"] = target?.UniqueId
            };
        }

        private JObject CaptureRangedVariantProgress()
        {
            return new JObject
            {
                ["category"] = rangedVariantCategory.ToString(),
                ["previousTargetId"] = rangedVariantPreviousTargetId,
                ["previousTargetCleanupPassed"] = rangedVariantPreviousTargetCleanupPassed,
                ["isolatedTargetId"] = target?.UniqueId,
                ["targetInState"] = target != null && target.IsInState,
                ["targetConscious"] = target != null && target.Descriptor.State.IsConscious,
                ["activePairCommand"] = combat.HasActiveCommand,
                ["stockIntentActive"] = combat.HasStockAttackIntent,
                ["riderDispatchDelta"] = combat.StockAttackRiderDispatchCount - stockRiderBefore,
                ["mountDispatchDelta"] = combat.StockAttackMountDispatchCount - stockMountBefore,
                ["duplicateDispatchDelta"] = combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore,
                ["riderOutcomeObserved"] = rangedRiderOutcome != null,
                ["rules"] = ruleProbe?.CapturePairEvidence(),
                ["readinessNow"] = target == null ? null : CaptureRangedVariantReadiness()
            };
        }

        private JToken CaptureLeafDeadlineProgress()
        {
            if (step == Phase3dHorseStep.AwaitNextRiderTurnForMountPrimaryTb ||
                step == Phase3dHorseStep.AwaitNextRiderTurnForStockMeleeTb ||
                step == Phase3dHorseStep.AwaitNextRiderTurnForRangedTb ||
                step == Phase3dHorseStep.AwaitNextRiderTurnForStep ||
                step == Phase3dHorseStep.AwaitNextRiderTurnForOrdinaryMove ||
                step == Phase3dHorseStep.AwaitNextRiderTurnForDismount ||
                step == Phase3dHorseStep.AwaitNextRiderTurnForUnmountedStep ||
                step == Phase3dHorseStep.AwaitNextRiderTurnForSpentControlDismount ||
                step == Phase3dHorseStep.AwaitHorseTurnForSpentControl ||
                step == Phase3dHorseStep.AwaitRiderTurnForMountSpentControl)
            {
                return CaptureNativeTurnTraversalProgress();
            }
            if (step == Phase3dHorseStep.AwaitCombatMountAdjacencyReadiness ||
                step == Phase3dHorseStep.AwaitCombatMountAdjacencyMove)
            {
                return CaptureCombatMountAdjacencyProgress();
            }
            if (step == Phase3dHorseStep.AwaitCombatMountHorseAiIsolation)
            {
                return new JObject
                {
                    ["horseAiIsolation"] = CaptureUnmountedHorseAiIsolation(),
                    ["riderAiIsolation"] = CaptureCombatMountRiderAiIsolation()
                };
            }
            if (step == Phase3dHorseStep.AwaitTurnBasedMode ||
                step == Phase3dHorseStep.AwaitRiderTurnForMount)
            {
                return CaptureCombatMountTurnAdmissionProgress();
            }
            if (step == Phase3dHorseStep.AwaitCombatMount)
            {
                return CaptureCombatMountNativeCommandProgress();
            }
            if (step == Phase3dHorseStep.AwaitRtCombatDismountAdmission ||
                step == Phase3dHorseStep.AwaitRtCombatDismount)
            {
                var availability = nativeControls.Evaluate(NativeMountedControlKind.Dismount, rider);
                return CaptureRtCombatDismountState(availability);
            }
            if (step == Phase3dHorseStep.AwaitUnmountedHorseAiIsolation)
            {
                return CaptureUnmountedHorseAiIsolation();
            }
            if (step == Phase3dHorseStep.AwaitUnmountedTargetCleanupRt)
            {
                return new JObject
                {
                    ["previousTargetId"] = unmountedPreviousTargetId,
                    ["previousTargetCleanupPassed"] = unmountedPreviousTargetCleanupPassed,
                    ["targetCleanupComplete"] = targetCleanupComplete,
                    ["horseAiIsolation"] = CaptureUnmountedHorseAiIsolation()
                };
            }
            if (step == Phase3dHorseStep.AwaitUnmountedMeleeRt ||
                step == Phase3dHorseStep.AwaitUnmountedMeleeCancelRt)
            {
                return new JObject
                {
                    ["command"] = CaptureCommandState(unmountedCommand),
                    ["rules"] = ruleProbe?.CapturePairEvidence(),
                    ["targetInState"] = target != null && target.IsInState,
                    ["targetConscious"] = target != null && target.Descriptor.State.IsConscious,
                    ["targetDamage"] = target?.Damage,
                    ["horseAiIsolation"] = CaptureUnmountedHorseAiIsolation()
                };
            }
            if (step == Phase3dHorseStep.AwaitUnmountedRangedTargetCleanupRt)
            {
                return new JObject
                {
                    ["previousMeleeTargetId"] = unmountedMeleeTargetId,
                    ["previousMeleeTargetCleanupPassed"] = unmountedMeleeTargetCleanupPassed,
                    ["targetCleanupComplete"] = targetCleanupComplete,
                    ["horseAiIsolation"] = CaptureUnmountedHorseAiIsolation()
                };
            }
            if (step == Phase3dHorseStep.AwaitStockMeleeTargetCleanupRt)
            {
                return new JObject
                {
                    ["previousTargetId"] = stockMeleePreviousTargetId,
                    ["previousTargetCleanupPassed"] = stockMeleePreviousTargetCleanupPassed,
                    ["targetCleanupComplete"] = targetCleanupComplete
                };
            }
            if (step == Phase3dHorseStep.AwaitStockMeleeAdmissionRt ||
                step == Phase3dHorseStep.AwaitStockMeleeRt)
            {
                return CaptureStockMeleeProgress();
            }
            if (step == Phase3dHorseStep.AwaitRangedCombat ||
                step == Phase3dHorseStep.AwaitRangedAttackRt)
            {
                return CaptureLongRangeRangedProgress();
            }
            if (step == Phase3dHorseStep.AwaitRangedAdjacentMoveRt ||
                step == Phase3dHorseStep.AwaitRangedAdjacentAttackRt ||
                step == Phase3dHorseStep.AwaitRangedAdjacentCancelRt)
            {
                return CaptureRangedOpportunityProgress();
            }
            if (step == Phase3dHorseStep.AwaitRangedVariantTargetCleanupRt)
            {
                return new JObject
                {
                    ["category"] = rangedVariantCategory.ToString(),
                    ["previousTargetId"] = rangedVariantPreviousTargetId,
                    ["previousTargetCleanupPassed"] = rangedVariantPreviousTargetCleanupPassed,
                    ["targetCleanupComplete"] = targetCleanupComplete
                };
            }
            if (step == Phase3dHorseStep.AwaitRangedVariantAdmissionRt ||
                step == Phase3dHorseStep.AwaitRangedVariantRt ||
                step == Phase3dHorseStep.AwaitRangedVariantCancelRt)
            {
                return CaptureRangedVariantProgress();
            }
            if (step == Phase3dHorseStep.AwaitUnmountedRangedAdmissionRt)
            {
                return CaptureUnmountedRangedReadiness();
            }
            if (step == Phase3dHorseStep.AwaitUnmountedRangedRt)
            {
                return CaptureUnmountedRangedProgress();
            }
            return JValue.CreateNull();
        }

        private JObject CaptureCombatMountNativeCommandProgress()
        {
            try
            {
                var game = Game.Instance;
                var controller = game?.TurnBasedCombatController;
                var turn = controller?.CurrentTurn;
                var command = lastNativeAbilityShell;
                var equipment = game?.HandsEquipmentController;
                var nextUnit = GetPendingNextUnit(controller);
                var riderInAwakeUnits = game?.State?.AwakeUnits != null &&
                    game.State.AwakeUnits.Contains(rider);
                var rigidbodyControlling = rider.View?.RigidbodyController != null &&
                    rider.View.RigidbodyController.IsControllingRigidbody;
                var riderIsGetUp = rider.View != null && rider.View.IsGetUp;
                var unitTickEligible = rider.IsAwake && riderInAwakeUnits && rider.View != null &&
                    !rigidbodyControlling && !riderIsGetUp;
                var waitingForUi = controller != null && (bool)controller.WaitingForUI;
                var waitingForUiGuardCount = controller?.WaitingForUI?.GuardCount ?? -1;
                var currentTurnRiderExact = turn?.Unit == rider;
                var currentTurnEligible = currentTurnRiderExact &&
                    (turn.IsActing || turn.IsEnding);
                var riderNauseated = rider.Descriptor.State.HasCondition(UnitCondition.Nauseated);
                var riderEquipmentIdle = equipment != null && !equipment.IsUpdateScheduledFor(rider);
                var commandPresent = command != null;
                var commandInMoveSlot = commandPresent && rider.Commands != null &&
                    ReferenceEquals(rider.Commands.GetCommand(UnitCommand.CommandType.Move), command);
                var commandQueued = commandPresent && rider.Commands != null &&
                    rider.Commands.Queue.Contains(command);
                var commandSpellAvailable = commandPresent && command.Spell != null &&
                    command.Spell.IsAvailableForCast;
                var commandHasCooldown = commandPresent && rider.CombatState != null &&
                    rider.CombatState.HasCooldownForCommand(command);
                var commandShouldStartReady = commandPresent && !command.IsStarted &&
                    command.Result == UnitCommand.ResultType.None && command.IsUnitEnoughClose &&
                    (!rider.AreHandsBusyWithAnimation || command.DontWaitForHands) &&
                    (!command.AwaitMovementFinish || rider.View?.MovementAgent == null ||
                     !rider.View.MovementAgent.IsReallyMoving) &&
                    command.CanStart && rider.Descriptor.State.CanAct && riderEquipmentIdle &&
                    (command.IsIgnoreCooldown ||
                     rider.CombatState != null && rider.CombatState.CanActInCombat && !commandHasCooldown) &&
                    !riderNauseated;
                var stockTurnGateReady = CombatController.IsInTurnBasedCombat() && !waitingForUi &&
                    waitingForUiGuardCount == 0 && currentTurnEligible;

                return new JObject
                {
                    ["step"] = step.ToString(),
                    ["frame"] = Time.frameCount,
                    ["startTurnRequestCount"] = CombatMountSyntheticStartTurnRequestCount,
                    ["admissionFrame"] = combatMountNativeCommandAdmissionFrame,
                    ["startObservedFrame"] = combatMountNativeCommandStartObservedFrame,
                    ["terminalObservedFrame"] = combatMountNativeCommandTerminalObservedFrame,
                    ["nativeTickEncounterCount"] = combatMountNativeTickEncounterCount,
                    ["nativeTickEligibleCount"] = combatMountNativeTickEligibleCount,
                    ["nativeTickRejectedCount"] = combatMountNativeTickRejectedCount,
                    ["nativeTickDuplicateFrameCount"] = combatMountNativeTickDuplicateFrameCount,
                    ["nativeTickFirstFrame"] = combatMountNativeTickFirstFrame,
                    ["nativeTickLastFrame"] = combatMountNativeTickLastFrame,
                    ["nativeTickFirstEligibleFrame"] = combatMountNativeTickFirstEligibleFrame,
                    ["nativeTickLastStockEligible"] = combatMountNativeTickLastStockEligible,
                    ["nativeTickLastWaitingForUi"] = combatMountNativeTickLastWaitingForUi,
                    ["nativeTickLastWaitingForUiGuardCount"] =
                        combatMountNativeTickLastWaitingForUiGuardCount,
                    ["nativeTickLastCurrentTurnUnitId"] = combatMountNativeTickLastCurrentTurnUnitId,
                    ["nativeTickLastCurrentTurnStatus"] = combatMountNativeTickLastCurrentTurnStatus,
                    ["gamePaused"] = game != null && game.IsPaused,
                    ["gameMode"] = game?.CurrentMode.ToString(),
                    ["gameModeDefault"] = game != null && game.CurrentMode == GameModeType.Default,
                    ["turnBased"] = CombatController.IsInTurnBasedCombat(),
                    ["waitingForUi"] = waitingForUi,
                    ["waitingForUiGuardCount"] = waitingForUiGuardCount,
                    ["currentTurnUnitId"] = turn?.Unit?.UniqueId,
                    ["currentTurnStatus"] = turn?.Status.ToString(),
                    ["currentTurnIsActing"] = turn != null && turn.IsActing,
                    ["currentTurnIsEnding"] = turn != null && turn.IsEnding,
                    ["currentTurnRiderExact"] = currentTurnRiderExact,
                    ["currentTurnEligible"] = currentTurnEligible,
                    ["nextUnitId"] = nextUnit?.UniqueId,
                    ["nextUnitClear"] = nextUnit == null,
                    ["riderIsAwake"] = rider.IsAwake,
                    ["riderInAwakeUnits"] = riderInAwakeUnits,
                    ["riderViewPresent"] = rider.View != null,
                    ["riderRigidbodyControlling"] = rigidbodyControlling,
                    ["riderIsGetUp"] = riderIsGetUp,
                    ["riderUnitTickEligible"] = unitTickEligible,
                    ["riderHandsIdle"] = !rider.AreHandsBusyWithAnimation,
                    ["riderEquipmentIdle"] = riderEquipmentIdle,
                    ["riderCanAct"] = rider.Descriptor.State.CanAct,
                    ["riderCanActInCombat"] = rider.CombatState != null &&
                        rider.CombatState.CanActInCombat,
                    ["riderNauseated"] = riderNauseated,
                    ["commandReferencePresent"] = commandPresent,
                    ["commandCreatedByPlayer"] = commandPresent && command.CreatedByPlayer,
                    ["commandAiActionPresent"] = commandPresent && command.AiAction != null,
                    ["commandExecutorRiderExact"] = commandPresent && command.Executor == rider,
                    ["commandTargetHorseExact"] = commandPresent && command.Target?.Unit == horse,
                    ["commandInMoveSlotExact"] = commandInMoveSlot,
                    ["commandQueued"] = commandQueued,
                    ["commandStarted"] = commandPresent && command.IsStarted,
                    ["commandRunning"] = commandPresent && command.IsRunning,
                    ["commandFinished"] = commandPresent && command.IsFinished,
                    ["commandActed"] = commandPresent && command.IsActed,
                    ["commandResult"] = commandPresent ? command.Result.ToString() : null,
                    ["commandCanStart"] = commandPresent && command.CanStart,
                    ["commandEnoughClose"] = commandPresent && command.IsUnitEnoughClose,
                    ["commandShouldApproach"] = commandPresent && command.ShouldUnitApproach,
                    ["commandSpellAvailable"] = commandSpellAvailable,
                    ["commandHasCooldown"] = commandHasCooldown,
                    ["commandNativeShouldStartReady"] = commandShouldStartReady,
                    ["commandStockTurnGateReady"] = stockTurnGateReady,
                    ["relationshipState"] = relationship.State.ToString(),
                    ["commands"] = CapturePairCommandState(),
                    ["nativeShell"] = CaptureNativeAbilityShell(command)
                };
            }
            catch (Exception exception)
            {
                return new JObject
                {
                    ["step"] = step.ToString(),
                    ["frame"] = Time.frameCount,
                    ["captureError"] = exception.GetType().FullName + ": " + exception.Message
                };
            }
        }

        private static UnitEntityData GetPendingNextUnit(CombatController controller)
        {
            if (controller == null || CombatNextUnitField == null ||
                CombatNextUnitField.FieldType != typeof(UnitEntityData))
            {
                return null;
            }
            return CombatNextUnitField.GetValue(controller) as UnitEntityData;
        }

        private JObject CaptureCombatMountTurnAdmissionProgress()
        {
            try
            {
                var game = Game.Instance;
                var controller = game?.TurnBasedCombatController;
                var turn = controller?.CurrentTurn;
                var roster = controller?.SortedUnits;
                var selected = SelectionManager.Instance?.SelectedUnits;
                var availability = nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider);
                var rosterIds = roster == null
                    ? new JArray()
                    : new JArray(roster.Where(item => item != null).Select(item => item.UniqueId));
                var selectedIds = selected == null
                    ? new JArray()
                    : new JArray(selected.Where(item => item != null).Select(item => item.UniqueId));
                var currentTurnRiderExact = turn?.Unit == rider;
                var currentTurnActionable = currentTurnRiderExact &&
                    (turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing);
                var nextUnit = GetPendingNextUnit(controller);
                var waitingForUi = controller != null && (bool)controller.WaitingForUI;
                var waitingForUiGuardCount = controller?.WaitingForUI?.GuardCount ?? -1;
                var riderInAwakeUnits = game?.State?.AwakeUnits != null &&
                    game.State.AwakeUnits.Contains(rider);
                var riderRigidbodyControlling = rider.View?.RigidbodyController != null &&
                    rider.View.RigidbodyController.IsControllingRigidbody;
                var riderIsGetUp = rider.View != null && rider.View.IsGetUp;
                var riderNauseated = rider.Descriptor.State.HasCondition(UnitCondition.Nauseated);

                return new JObject
                {
                    ["step"] = step.ToString(),
                    ["frame"] = Time.frameCount,
                    ["stableFrames"] = stableFrames,
                    ["startTurnRequestCount"] = CombatMountSyntheticStartTurnRequestCount,
                    ["riderTurnObservedFrames"] = combatMountRiderTurnObservedFrames,
                    ["actionableTurnObservedFrames"] = combatMountActionableTurnObservedFrames,
                    ["currentTurnMismatchFrames"] = combatMountCurrentTurnMismatchFrames,
                    ["turnStatusBlockedFrames"] = combatMountTurnStatusBlockedFrames,
                    ["riderCommandBlockedFrames"] = combatMountRiderCommandBlockedFrames,
                    ["horseCommandBlockedFrames"] = combatMountHorseCommandBlockedFrames,
                    ["riderHandsBlockedFrames"] = combatMountRiderHandsBlockedFrames,
                    ["riderEquipmentBlockedFrames"] = combatMountRiderEquipmentBlockedFrames,
                    ["waitingForUiBlockedFrames"] = combatMountWaitingForUiBlockedFrames,
                    ["pendingNextUnitBlockedFrames"] = combatMountPendingNextUnitBlockedFrames,
                    ["riderAwakeBlockedFrames"] = combatMountRiderAwakeBlockedFrames,
                    ["riderAwakeScheduleBlockedFrames"] = combatMountRiderAwakeScheduleBlockedFrames,
                    ["riderUnitTickBlockedFrames"] = combatMountRiderUnitTickBlockedFrames,
                    ["gameModeBlockedFrames"] = combatMountGameModeBlockedFrames,
                    ["selectionBlockedFrames"] = combatMountSelectionBlockedFrames,
                    ["riderNauseatedBlockedFrames"] = combatMountNauseatedBlockedFrames,
                    ["gamePresent"] = game != null,
                    ["gamePaused"] = game != null && game.IsPaused,
                    ["gameMode"] = game?.CurrentMode.ToString(),
                    ["gameModeDefault"] = game != null && game.CurrentMode == GameModeType.Default,
                    ["turnBased"] = CombatController.IsInTurnBasedCombat(),
                    ["controllerPresent"] = controller != null,
                    ["controllerInitialized"] = controller != null && controller.Initialized,
                    ["waitingForUi"] = waitingForUi,
                    ["waitingForUiGuardCount"] = waitingForUiGuardCount,
                    ["nextUnitId"] = nextUnit?.UniqueId,
                    ["nextUnitClear"] = nextUnit == null,
                    ["currentTurnPresent"] = turn != null,
                    ["currentTurnUnitId"] = turn?.Unit?.UniqueId,
                    ["currentTurnStatus"] = turn?.Status.ToString(),
                    ["currentTurnIsActing"] = turn != null && turn.IsActing,
                    ["currentTurnRiderExact"] = currentTurnRiderExact,
                    ["currentTurnActionable"] = currentTurnActionable,
                    ["rosterUnitIds"] = rosterIds,
                    ["rosterRiderCount"] = roster == null ? 0 : roster.Count(item => item == rider),
                    ["rosterHorseCount"] = roster == null ? 0 : roster.Count(item => item == horse),
                    ["rosterTargetCount"] = roster == null ? 0 : roster.Count(item => item == target),
                    ["selectedUnitIds"] = selectedIds,
                    ["selectionRiderExact"] = selected != null && selected.Count == 1 && selected[0] == rider,
                    ["riderIsAwake"] = rider.IsAwake,
                    ["riderInAwakeUnits"] = riderInAwakeUnits,
                    ["riderViewPresent"] = rider.View != null,
                    ["riderRigidbodyControlling"] = riderRigidbodyControlling,
                    ["riderIsGetUp"] = riderIsGetUp,
                    ["riderUnitTickEligible"] = rider.IsAwake && riderInAwakeUnits &&
                        rider.View != null && !riderRigidbodyControlling && !riderIsGetUp,
                    ["riderNauseated"] = riderNauseated,
                    ["relationshipState"] = relationship.State.ToString(),
                    ["relationshipExact"] = relationship.State == RelationshipState.Mounted &&
                        relationship.Rider == rider && relationship.Mount == horse,
                    ["mountAbilityVisible"] = availability.IsVisible,
                    ["mountAbilityEnabled"] = availability.IsEnabled,
                    ["mountAbilityReason"] = availability.Reason,
                    ["combatMemoryQueued"] = targetService?.CombatMemoryQueued,
                    ["playerGroupMemoryContainsTarget"] = targetService?.PlayerGroupMemoryContainsTarget,
                    ["targetGroupMemoryContainsRider"] = targetService?.TargetGroupMemoryContainsRider,
                    ["rider"] = CaptureCombatMountActorState(rider),
                    ["mount"] = CaptureCombatMountActorState(horse),
                    ["target"] = CaptureCombatMountTargetState(),
                    ["commands"] = CapturePairCommandState(),
                    ["lastNativeAbilityShell"] = CaptureNativeAbilityShell(lastNativeAbilityShell),
                    ["unified"] = JObject.FromObject(
                        combat.CaptureUnifiedTurnSnapshot(), JsonSerializer.Create(JsonSettings))
                };
            }
            catch (Exception exception)
            {
                return new JObject
                {
                    ["step"] = step.ToString(),
                    ["frame"] = Time.frameCount,
                    ["captureError"] = exception.GetType().FullName + ": " + exception.Message
                };
            }
        }

        private static JObject CaptureCombatMountActorState(UnitEntityData unit)
        {
            var game = Game.Instance;
            var equipment = game?.HandsEquipmentController;
            var combatState = unit?.CombatState;
            var view = unit?.View;
            var rigidbodyControlling = view?.RigidbodyController != null &&
                view.RigidbodyController.IsControllingRigidbody;
            return new JObject
            {
                ["present"] = unit != null,
                ["unitId"] = unit?.UniqueId,
                ["isInState"] = unit != null && unit.IsInState,
                ["isInCombat"] = unit != null && unit.IsInCombat,
                ["conscious"] = unit?.Descriptor?.State?.IsConscious,
                ["canAct"] = unit?.Descriptor?.State?.CanAct,
                ["isAwake"] = unit != null && unit.IsAwake,
                ["inAwakeUnits"] = unit != null && game?.State?.AwakeUnits != null &&
                    game.State.AwakeUnits.Contains(unit),
                ["viewPresent"] = view != null,
                ["rigidbodyControlling"] = rigidbodyControlling,
                ["isGetUp"] = view != null && view.IsGetUp,
                ["prone"] = unit?.Descriptor?.State?.Prone?.Active,
                ["nauseated"] = unit != null && unit.Descriptor.State.HasCondition(UnitCondition.Nauseated),
                ["movementAgentPresent"] = view?.MovementAgent != null,
                ["movementAgentReallyMoving"] = view?.MovementAgent != null &&
                    view.MovementAgent.IsReallyMoving,
                ["combatStatePresent"] = combatState != null,
                ["prepared"] = combatState?.Prepared,
                ["canActInCombat"] = combatState?.CanActInCombat,
                ["initiative"] = combatState?.Cooldown.Initiative,
                ["standardCooldown"] = combatState?.Cooldown.StandardAction,
                ["moveCooldown"] = combatState?.Cooldown.MoveAction,
                ["hasStandardAction"] = unit != null && combatState != null && unit.HasStandardAction(),
                ["hasMoveAction"] = unit != null && combatState != null && unit.HasMoveAction(),
                ["commandsPresent"] = unit?.Commands != null,
                ["commandsIdle"] = unit?.Commands != null && unit.Commands.Empty,
                ["handsIdle"] = unit != null && !unit.AreHandsBusyWithAnimation,
                ["equipmentControllerPresent"] = equipment != null,
                ["equipmentIdle"] = equipment != null && unit != null &&
                    !equipment.IsUpdateScheduledFor(unit)
            };
        }

        private JObject CaptureCombatMountTargetState()
        {
            return new JObject
            {
                ["present"] = target != null,
                ["unitId"] = target?.UniqueId,
                ["isInState"] = target != null && target.IsInState,
                ["isInCombat"] = target != null && target.IsInCombat,
                ["conscious"] = target?.Descriptor?.State?.IsConscious,
                ["riderEnemy"] = target != null && rider.IsEnemy(target),
                ["riderCanAttack"] = target != null && rider.CanAttack(target),
                ["commandsPresent"] = target?.Commands != null,
                ["commandsIdle"] = target?.Commands != null && target.Commands.Empty,
                ["rawCommands"] = CaptureRawCommandSlots(target),
                ["queuedCommands"] = CaptureQueuedCommands(target)
            };
        }

        private JObject CaptureUnmountedRangedReadiness()
        {
            var game = Game.Instance;
            var selected = SelectionManager.Instance?.SelectedUnits;
            var equipment = game?.HandsEquipmentController;
            var weapon = rider.GetFirstWeapon()?.Blueprint;
            var targetReady = target != null && target.IsInState &&
                target.Descriptor.State.IsConscious && rider.IsEnemy(target) && rider.CanAttack(target);
            var combatMemoryReady = targetService != null &&
                targetService.RefreshBidirectionalCombatMemoryLease();
            var horseAiIsolated = AiBackingField != null &&
                unmountedHorseAiLease != null && unmountedHorseAiLease.IsAcquired &&
                unmountedHorseAiLease.LastActiveValidationPassed && horse.Commands != null && horse.Commands.Empty &&
                !((bool)AiBackingField.GetValue(horse)) && !horse.IsAIEnabled;
            var freshTarget = unmountedMeleeTargetCleanupPassed &&
                !string.IsNullOrWhiteSpace(unmountedMeleeTargetId) && target != null &&
                !string.Equals(unmountedMeleeTargetId, target.UniqueId, StringComparison.Ordinal);
            var ready = relationship.State == RelationshipState.Unmounted &&
                !CombatController.IsInTurnBasedCombat() && game != null && !game.IsPaused &&
                selected != null && selected.Count == 1 && selected[0] == rider &&
                rangedWeaponLease != null && rangedWeaponLease.IsReady && weapon != null &&
                weapon.IsRanged && weapon.Category == WeaponCategory.Sling &&
                targetReady && combatMemoryReady && freshTarget && rider.HasStandardAction() &&
                rider.Commands != null && rider.Commands.Empty &&
                horseAiIsolated &&
                target.Commands != null && target.Commands.Empty &&
                !rider.AreHandsBusyWithAnimation && !target.AreHandsBusyWithAnimation &&
                equipment != null && !equipment.IsUpdateScheduledFor(rider);
            return new JObject
            {
                ["ready"] = ready,
                ["relationshipState"] = relationship.State.ToString(),
                ["modeRealTime"] = !CombatController.IsInTurnBasedCombat(),
                ["gameUnpaused"] = game != null && !game.IsPaused,
                ["riderSelected"] = selected != null && selected.Count == 1 && selected[0] == rider,
                ["weaponLeaseReady"] = rangedWeaponLease != null && rangedWeaponLease.IsReady,
                ["weaponCategory"] = weapon?.Category.ToString(),
                ["targetReady"] = targetReady,
                ["combatMemoryReady"] = combatMemoryReady,
                ["riderStandardReady"] = rider.HasStandardAction(),
                ["riderStandardCooldown"] = rider.CombatState?.Cooldown.StandardAction,
                ["riderCommandsIdle"] = rider.Commands != null && rider.Commands.Empty,
                ["horseAiIsolated"] = horseAiIsolated,
                ["horseCommandsIdle"] = horse.Commands != null && horse.Commands.Empty,
                ["targetCommandsIdle"] = target?.Commands != null && target.Commands.Empty,
                ["riderHandsIdle"] = !rider.AreHandsBusyWithAnimation,
                ["targetHandsIdle"] = target != null && !target.AreHandsBusyWithAnimation,
                ["equipmentControllerReady"] = equipment != null,
                ["riderEquipmentIdle"] = equipment != null && !equipment.IsUpdateScheduledFor(rider),
                ["previousTargetId"] = unmountedPreviousTargetId,
                ["previousTargetCleanupPassed"] = unmountedPreviousTargetCleanupPassed,
                ["isolatedTargetId"] = target?.UniqueId,
                ["previousMeleeTargetId"] = unmountedMeleeTargetId,
                ["previousMeleeTargetCleanupPassed"] = unmountedMeleeTargetCleanupPassed,
                ["freshTarget"] = freshTarget,
                ["targetDamage"] = target?.Damage
            };
        }

        private bool PrepareUnmountedHorseAiIsolation()
        {
            try
            {
                if (unmountedHorseAiLease == null)
                {
                    if (horse?.Commands == null || rider == null || horse.Group == null ||
                        horse.Group != rider.Group || !IsExactDiagnosticAiIsolationRelationship() ||
                        AiBackingField == null || AiBackingField.FieldType != typeof(bool))
                    {
                        throw new InvalidOperationException(
                            "The exact unmounted Horse AI-isolation contract is unavailable.");
                    }

                    if (!unmountedHorseAiSettleRequested)
                    {
                        unmountedHorseAiSettleRequested = true;
                        unmountedHorseAiSettleStartedAtSeconds = clock.Elapsed.TotalSeconds;
                    }
                    horse.Commands.RemoveFinishedAndUpdateQueue();
                    if (!horse.Commands.Empty)
                    {
                        if (clock.Elapsed.TotalSeconds - unmountedHorseAiSettleStartedAtSeconds <=
                            UnmountedHorseAiSettleTimeoutSeconds)
                        {
                            return false;
                        }
                        throw new InvalidOperationException(
                            "The exact Horse command surface did not settle within the bounded five-second post-Dismount window.");
                    }

                    unmountedHorseAiLease = new ScopedDiagnosticAiLease<UnitEntityData>(
                        unit => unit.UniqueId,
                        unit => ReferenceEquals(unit, horse) && unit.IsInState &&
                            unit.IsDirectlyControllable && unit.Group == rider.Group &&
                            IsExactDiagnosticAiIsolationRelationship(),
                        unit => unit.Commands != null && unit.Commands.Empty,
                        unit => (bool)AiBackingField.GetValue(unit),
                        unit => unit.IsAIEnabled,
                        (unit, value) => unit.IsAIEnabled = value);
                    unmountedHorseAiLeaseRestored = false;
                    unmountedHorseAiLease.Acquire(new[] { horse });
                    unmountedHorseAiStableFrames = 0;
                    unmountedHorseAiLeaseError = null;
                    return false;
                }

                unmountedHorseAiLease.ValidateActive(new[] { horse });
                unmountedHorseAiStableFrames++;
                unmountedHorseAiLeaseError = null;
                return unmountedHorseAiStableFrames >= 2;
            }
            catch (Exception exception)
            {
                unmountedHorseAiLeaseError = exception.GetType().Name + ": " + exception.Message;
                unmountedHorseAiLeaseRestored = unmountedHorseAiLease == null ||
                    unmountedHorseAiLease.LastRestoreVerified;
                FailCurrent(
                    "phase3d-horse-runtime-exception",
                    "The exact unmounted Horse could not enter a stable reversible AI-isolation lease: " +
                    unmountedHorseAiLeaseError + ".");
                BeginCleanup();
                return false;
            }
        }

        private bool ValidateUnmountedHorseAiIsolation()
        {
            try
            {
                if (unmountedHorseAiLease == null || !unmountedHorseAiLease.IsAcquired)
                {
                    throw new InvalidOperationException("The exact Horse AI-isolation lease is not active.");
                }
                unmountedHorseAiLease.ValidateActive(new[] { horse });
                unmountedHorseAiLeaseError = null;
                return true;
            }
            catch (Exception exception)
            {
                unmountedHorseAiLeaseError = exception.GetType().Name + ": " + exception.Message;
                FailCurrent(
                    "phase3d-horse-runtime-exception",
                    "The exact unmounted Horse AI-isolation lease lost its command or state invariant: " +
                    unmountedHorseAiLeaseError + ".");
                BeginCleanup();
                return false;
            }
        }

        private bool RestoreUnmountedHorseAiIsolation()
        {
            if (unmountedHorseAiLease == null)
            {
                unmountedHorseAiLeaseRestored = true;
                unmountedHorseAiLeaseError = null;
                return true;
            }
            if (!unmountedHorseAiLease.IsAcquired)
            {
                unmountedHorseAiLeaseRestored = unmountedHorseAiLease.LastRestoreVerified;
                return unmountedHorseAiLeaseRestored;
            }

            try
            {
                if (horse?.Commands == null)
                {
                    throw new InvalidOperationException(
                        "The exact Horse command surface is unavailable for AI restoration.");
                }
                horse.Commands.InterruptAll(false);
                horse.Commands.RemoveFinishedAndUpdateQueue();
                unmountedHorseAiLease.Restore(new[] { horse });
                unmountedHorseAiLeaseRestored = !unmountedHorseAiLease.IsAcquired &&
                    unmountedHorseAiLease.LastRestoreVerified;
                unmountedHorseAiLeaseError = unmountedHorseAiLeaseRestored
                    ? null
                    : "The exact Horse AI lease did not report verified restoration.";
                return unmountedHorseAiLeaseRestored;
            }
            catch (Exception exception)
            {
                unmountedHorseAiLeaseRestored = false;
                unmountedHorseAiLeaseError = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        private JObject CaptureUnmountedHorseAiIsolation()
        {
            var states = new JArray();
            if (unmountedHorseAiLease != null)
            {
                foreach (var state in unmountedHorseAiLease.States)
                {
                    states.Add(new JObject
                    {
                        ["unitId"] = state.UnitId,
                        ["commandsEmptyBefore"] = state.CommandsEmptyBefore,
                        ["rawAiBefore"] = state.RawAiBefore,
                        ["effectiveAiBefore"] = state.EffectiveAiBefore,
                        ["commandsEmptyDuring"] = state.CommandsEmptyDuring,
                        ["rawAiDuring"] = state.RawAiDuring,
                        ["effectiveAiDuring"] = state.EffectiveAiDuring,
                        ["commandsEmptyAfter"] = state.CommandsEmptyAfter,
                        ["rawAiAfter"] = state.RawAiAfter,
                        ["effectiveAiAfter"] = state.EffectiveAiAfter
                    });
                }
            }
            return new JObject
            {
                ["present"] = unmountedHorseAiLease != null,
                ["acquired"] = unmountedHorseAiLease != null && unmountedHorseAiLease.IsAcquired,
                ["activeValidationPassed"] = unmountedHorseAiLease != null &&
                    unmountedHorseAiLease.LastActiveValidationPassed,
                ["restoreVerified"] = unmountedHorseAiLease != null &&
                    unmountedHorseAiLease.LastRestoreVerified,
                ["restored"] = unmountedHorseAiLeaseRestored,
                ["stableFrames"] = unmountedHorseAiStableFrames,
                ["error"] = unmountedHorseAiLeaseError,
                ["states"] = states
            };
        }

        private bool PrepareCombatMountRiderAiIsolation()
        {
            try
            {
                if (combatMountRiderAiLease == null)
                {
                    var selected = SelectionManager.Instance?.SelectedUnits;
                    if ((!string.Equals(request.Scenario, TurnBasedScenario, StringComparison.Ordinal) && !IsOrdinaryAttackControls) ||
                        rider?.Commands == null || horse?.Commands == null || !rider.Commands.Empty ||
                        !horse.Commands.Empty || rider.Group == null || rider.Group != horse.Group ||
                        !rider.IsDirectlyControllable || !IsExactDiagnosticAiIsolationRelationship() ||
                        rider.IsInCombat || horse.IsInCombat || target != null || selected == null ||
                        selected.Count != 1 || selected[0] != rider ||
                        unmountedHorseAiLease == null || !unmountedHorseAiLease.IsAcquired ||
                        !unmountedHorseAiLease.LastActiveValidationPassed ||
                        AiBackingField == null || AiBackingField.FieldType != typeof(bool))
                    {
                        throw new InvalidOperationException(
                            "The exact pre-target rider AI-isolation contract is unavailable.");
                    }

                    combatMountRiderAiLease = new ScopedDiagnosticAiLease<UnitEntityData>(
                        unit => unit.UniqueId,
                        unit => ReferenceEquals(unit, rider) && unit.IsInState &&
                            unit.IsDirectlyControllable && unit.Group == horse.Group &&
                            IsExactDiagnosticAiIsolationRelationship(),
                        unit => unit.Commands != null && unit.Commands.Empty,
                        unit => (bool)AiBackingField.GetValue(unit),
                        unit => unit.IsAIEnabled,
                        (unit, value) => unit.IsAIEnabled = value);
                    combatMountRiderAiLeaseRestored = false;
                    combatMountRiderAiLease.Acquire(new[] { rider });
                    combatMountRiderAiStableFrames = 0;
                    combatMountRiderAiLeaseError = null;
                    return false;
                }

                combatMountRiderAiLease.ValidateActive(new[] { rider });
                combatMountRiderAiStableFrames++;
                combatMountRiderAiLeaseError = null;
                return combatMountRiderAiStableFrames >= 2;
            }
            catch (Exception exception)
            {
                combatMountRiderAiLeaseError = exception.GetType().Name + ": " + exception.Message;
                combatMountRiderAiLeaseRestored = combatMountRiderAiLease == null ||
                    combatMountRiderAiLease.LastRestoreVerified;
                FailCurrent(
                    "phase3d-horse-runtime-exception",
                    "The exact combat-Mount rider could not enter a stable reversible AI-isolation lease: " +
                    combatMountRiderAiLeaseError + ".");
                BeginCleanup();
                return false;
            }
        }

        private bool ValidateCombatMountRiderAiIsolation()
        {
            try
            {
                if (combatMountRiderAiLease == null || !combatMountRiderAiLease.IsAcquired)
                {
                    throw new InvalidOperationException(
                        "The exact combat-Mount rider AI-isolation lease is not active.");
                }
                combatMountRiderAiLease.ValidateActive(new[] { rider });
                combatMountRiderAiLeaseError = null;
                return true;
            }
            catch (Exception exception)
            {
                combatMountRiderAiLeaseError = exception.GetType().Name + ": " + exception.Message;
                FailCurrent(
                    "phase3d-horse-runtime-exception",
                    "The exact combat-Mount rider AI-isolation lease lost its command or state invariant: " +
                    combatMountRiderAiLeaseError + ".");
                BeginCleanup();
                return false;
            }
        }

        private bool IsExactDiagnosticAiIsolationRelationship()
        {
            return relationship.State == RelationshipState.Unmounted ||
                string.Equals(request.Scenario, TurnBasedScenario, StringComparison.Ordinal) &&
                relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == horse;
        }

        private bool RestoreCombatMountRiderAiIsolation()
        {
            if (combatMountRiderAiLease == null)
            {
                combatMountRiderAiLeaseRestored = true;
                combatMountRiderAiLeaseError = null;
                return true;
            }
            if (!combatMountRiderAiLease.IsAcquired)
            {
                combatMountRiderAiLeaseRestored = combatMountRiderAiLease.LastRestoreVerified;
                return combatMountRiderAiLeaseRestored;
            }

            try
            {
                if (rider?.Commands == null)
                {
                    throw new InvalidOperationException(
                        "The exact rider command surface is unavailable for AI restoration.");
                }
                rider.Commands.InterruptAll(false);
                rider.Commands.RemoveFinishedAndUpdateQueue();
                combatMountRiderAiLease.Restore(new[] { rider });
                combatMountRiderAiLeaseRestored = !combatMountRiderAiLease.IsAcquired &&
                    combatMountRiderAiLease.LastRestoreVerified;
                combatMountRiderAiLeaseError = combatMountRiderAiLeaseRestored
                    ? null
                    : "The exact rider AI lease did not report verified restoration.";
                return combatMountRiderAiLeaseRestored;
            }
            catch (Exception exception)
            {
                combatMountRiderAiLeaseRestored = false;
                combatMountRiderAiLeaseError = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        private JObject CaptureCombatMountRiderAiIsolation()
        {
            var states = new JArray();
            if (combatMountRiderAiLease != null)
            {
                foreach (var state in combatMountRiderAiLease.States)
                {
                    states.Add(new JObject
                    {
                        ["unitId"] = state.UnitId,
                        ["commandsEmptyBefore"] = state.CommandsEmptyBefore,
                        ["rawAiBefore"] = state.RawAiBefore,
                        ["effectiveAiBefore"] = state.EffectiveAiBefore,
                        ["commandsEmptyDuring"] = state.CommandsEmptyDuring,
                        ["rawAiDuring"] = state.RawAiDuring,
                        ["effectiveAiDuring"] = state.EffectiveAiDuring,
                        ["commandsEmptyAfter"] = state.CommandsEmptyAfter,
                        ["rawAiAfter"] = state.RawAiAfter,
                        ["effectiveAiAfter"] = state.EffectiveAiAfter
                    });
                }
            }
            return new JObject
            {
                ["present"] = combatMountRiderAiLease != null,
                ["acquired"] = combatMountRiderAiLease != null && combatMountRiderAiLease.IsAcquired,
                ["activeValidationPassed"] = combatMountRiderAiLease != null &&
                    combatMountRiderAiLease.LastActiveValidationPassed,
                ["restoreVerified"] = combatMountRiderAiLease != null &&
                    combatMountRiderAiLease.LastRestoreVerified,
                ["restored"] = combatMountRiderAiLeaseRestored,
                ["stableFrames"] = combatMountRiderAiStableFrames,
                ["error"] = combatMountRiderAiLeaseError,
                ["states"] = states
            };
        }

        private JObject CaptureUnmountedRangedProgress()
        {
            return new JObject
            {
                ["readinessNow"] = CaptureUnmountedRangedReadiness(),
                ["command"] = CaptureUnmountedRangedCommand(),
                ["rules"] = ruleProbe?.CapturePairEvidence(),
                ["nativeRequestDelta"] = combat.StockAttackNativeRequestCount - stockNativeBefore,
                ["intentStartDelta"] = combat.StockAttackIntentStartCount - stockIntentBefore
            };
        }

        private JObject CaptureUnmountedRangedCommand()
        {
            var command = unmountedCommand;
            var commands = rider?.Commands;
            return new JObject
            {
                ["present"] = command != null,
                ["type"] = command?.GetType().FullName,
                ["executorId"] = command?.Executor?.UniqueId,
                ["targetId"] = command?.Target?.Unit?.UniqueId,
                ["contained"] = command != null && commands != null && commands.Contains(command),
                ["inStandardSlot"] = command != null && commands != null &&
                    ReferenceEquals(commands.GetCommand(UnitCommand.CommandType.Standard), command),
                ["queued"] = command != null && commands != null && commands.Queue.Contains(command),
                ["started"] = command?.IsStarted,
                ["running"] = command?.IsRunning,
                ["finished"] = command?.IsFinished,
                ["acted"] = command?.IsActed,
                ["result"] = command?.Result.ToString(),
                ["canStart"] = command?.CanStart,
                ["unitEnoughClose"] = command?.IsUnitEnoughClose,
                ["shouldUnitApproach"] = command?.ShouldUnitApproach,
                ["needLineOfSight"] = command?.NeedLoS,
                ["approachRadius"] = command?.ApproachRadius,
                ["riderStandardCooldown"] = rider?.CombatState?.Cooldown.StandardAction
            };
        }

        private JObject CaptureRtCombatDismountState(NativeMountedControlAvailability availability)
        {
            var game = Game.Instance;
            var selected = SelectionManager.Instance?.SelectedUnits;
            return new JObject
            {
                ["availabilityVisible"] = availability?.IsVisible,
                ["availabilityEnabled"] = availability?.IsEnabled,
                ["availabilityReason"] = availability?.Reason,
                ["relationshipState"] = relationship.State.ToString(),
                ["turnBased"] = CombatController.IsInTurnBasedCombat(),
                ["riderSelectedPrincipal"] = selected != null && selected.Count == 1 && selected[0] == rider,
                ["riderInCombat"] = rider.IsInCombat,
                ["horseInCombat"] = horse.IsInCombat,
                ["partyInCombat"] = game?.Player?.IsInCombat,
                ["riderHasMoveAction"] = rider.HasMoveAction(),
                ["riderMoveCooldown"] = rider.CombatState?.Cooldown.MoveAction,
                ["riderStandardCooldown"] = rider.CombatState?.Cooldown.StandardAction,
                ["abilityActionType"] = nativeControls.DismountAbility?.ActionType.ToString(),
                ["playerActionFeedback"] = playerAction.LastFeedback,
                ["nativeControls"] = JObject.FromObject(
                    nativeControls.CaptureSnapshot(), JsonSerializer.Create(JsonSettings)),
                ["commands"] = CapturePairCommandState(),
                ["dismountActivations"] = JArray.FromObject(
                    nativeControls.SnapshotAbilityActivations()
                        .Where(item => item.Kind == NativeMountedControlKind.Dismount).ToArray(),
                    JsonSerializer.Create(JsonSettings))
            };
        }

        private bool TryNativeAbilityTargetClick(
            BlueprintAbility blueprint,
            UnitEntityData clickedTarget,
            string observationName)
        {
            nativeControls.Update();
            var fact = rider.Descriptor.Abilities.GetAbility(blueprint);
            var data = fact?.Data;
            var handler = Game.Instance.SelectedAbilityHandler;
            var targetObject = clickedTarget?.View?.gameObject;
            var position = clickedTarget?.Position ?? Vector3.zero;
            if (data == null || handler == null || targetObject == null)
            {
                observations[observationName] = new JObject
                {
                    ["abilityPresent"] = data != null,
                    ["handlerPresent"] = handler != null,
                    ["targetViewPresent"] = targetObject != null,
                    ["clicked"] = false
                };
                return false;
            }

            lastNativeAbilityShell = null;
            var before = nativeControls.CaptureSnapshot();
            handler.SetAbility(data);
            var priority = handler.GetPriority(targetObject, position);
            var resolvedTarget = handler.GetTarget(targetObject, position, data);
            var clicked = handler.OnClick(targetObject, position, 0, false, false);
            var after = nativeControls.CaptureSnapshot();
            var rawCommands = rider.Commands?.Raw;
            var shell = rawCommands == null
                ? null
                : rawCommands.OfType<UnitUseAbility>().FirstOrDefault(
                    item => ReferenceEquals(item.Spell?.Blueprint, blueprint));
            if (clicked && shell != null && ReferenceEquals(shell.Spell?.Blueprint, blueprint))
            {
                lastNativeAbilityShell = shell;
            }
            observations[observationName] = new JObject
            {
                ["abilityGuid"] = blueprint.AssetGuid,
                ["clickedTargetId"] = clickedTarget.UniqueId,
                ["resolvedTargetId"] = resolvedTarget?.Unit?.UniqueId,
                ["priority"] = priority.ToString(),
                ["clicked"] = clicked,
                ["targetSelectionStartDelta"] = after.TargetSelectionStartCount - before.TargetSelectionStartCount,
                ["targetSelectionEndDelta"] = after.TargetSelectionEndCount - before.TargetSelectionEndCount,
                ["nativeCastRequestDelta"] = after.NativeCastRequestCount - before.NativeCastRequestCount,
                ["nativeRefusalDelta"] = after.NativeRefusalCount - before.NativeRefusalCount,
                ["dispatchAcceptedDelta"] = after.DispatchAcceptedCount - before.DispatchAcceptedCount,
                ["dispatchRejectedDelta"] = after.DispatchRejectedCount - before.DispatchRejectedCount,
                ["nativePrimaryShellPrepareDelta"] = after.NativePrimaryShellPrepareCount -
                    before.NativePrimaryShellPrepareCount,
                ["nativePrimaryShellObservation"] = after.LastNativePrimaryShellObservation,
                ["nativeShell"] = CaptureNativeAbilityShell(shell)
            };
            handler.DropAbility();
            return clicked;
        }

        private JObject CaptureNativeAbilityShell(UnitUseAbility command)
        {
            if (command == null)
            {
                return new JObject { ["present"] = false };
            }

            try
            {
                var executor = command.Executor;
                var commands = executor?.Commands;
                var game = Game.Instance;
                var controller = game?.TurnBasedCombatController;
                var turn = controller?.CurrentTurn;
                var equipment = game?.HandsEquipmentController;
                var view = executor?.View;
                var rigidbodyControlling = view?.RigidbodyController != null &&
                    view.RigidbodyController.IsControllingRigidbody;
                return new JObject
                {
                    ["present"] = true,
                    ["abilityGuid"] = command.Spell?.Blueprint?.AssetGuid,
                    ["executorId"] = executor?.UniqueId,
                    ["targetId"] = command.Target?.Unit?.UniqueId,
                    ["createdByPlayer"] = command.CreatedByPlayer,
                    ["aiActionPresent"] = command.AiAction != null,
                    ["aiActionType"] = command.AiAction?.GetType().FullName,
                    ["type"] = command.Type.ToString(),
                    ["contained"] = commands != null && commands.Contains(command),
                    ["inFreeSlot"] = commands != null && ReferenceEquals(commands.Free, command),
                    ["inMoveSlot"] = commands != null && ReferenceEquals(
                        commands.GetCommand(UnitCommand.CommandType.Move), command),
                    ["queued"] = commands != null && commands.Queue.Contains(command),
                    ["started"] = command.IsStarted,
                    ["running"] = command.IsRunning,
                    ["finished"] = command.IsFinished,
                    ["acted"] = command.IsActed,
                    ["result"] = command.Result.ToString(),
                    ["canStart"] = command.CanStart,
                    ["unitEnoughClose"] = command.IsUnitEnoughClose,
                    ["shouldUnitApproach"] = command.ShouldUnitApproach,
                    ["needLineOfSight"] = command.NeedLoS,
                    ["approachRadius"] = command.ApproachRadius.ToString("R", CultureInfo.InvariantCulture),
                    ["dontWaitForHands"] = command.DontWaitForHands,
                    ["awaitMovementFinish"] = command.AwaitMovementFinish,
                    ["ignoreCooldown"] = command.IsIgnoreCooldown,
                    ["hasCooldown"] = executor?.CombatState != null &&
                        executor.CombatState.HasCooldownForCommand(command),
                    ["executorPrepared"] = executor?.CombatState?.Prepared,
                    ["executorCanAct"] = executor?.Descriptor?.State?.CanAct,
                    ["executorCanActInCombat"] = executor?.CombatState?.CanActInCombat,
                    ["executorInitiative"] = executor?.CombatState?.Cooldown.Initiative,
                    ["executorHandsBusy"] = executor?.AreHandsBusyWithAnimation,
                    ["executorEquipmentIdle"] = equipment != null && executor != null &&
                        !equipment.IsUpdateScheduledFor(executor),
                    ["executorAwake"] = executor != null && executor.IsAwake,
                    ["executorInAwakeUnits"] = executor != null && game?.State?.AwakeUnits != null &&
                        game.State.AwakeUnits.Contains(executor),
                    ["executorViewPresent"] = view != null,
                    ["executorRigidbodyControlling"] = rigidbodyControlling,
                    ["executorIsGetUp"] = view != null && view.IsGetUp,
                    ["executorNauseated"] = executor != null &&
                        executor.Descriptor.State.HasCondition(UnitCondition.Nauseated),
                    ["executorMovementAgentReallyMoving"] = view?.MovementAgent != null &&
                        view.MovementAgent.IsReallyMoving,
                    ["spellAvailableForCast"] = command.Spell != null && command.Spell.IsAvailableForCast,
                    ["shouldBeInterrupted"] = command.ShouldBeInterrupted,
                    ["waitingForUi"] = controller != null && (bool)controller.WaitingForUI,
                    ["waitingForUiGuardCount"] = controller?.WaitingForUI?.GuardCount ?? -1,
                    ["currentTurnUnitId"] = turn?.Unit?.UniqueId,
                    ["currentTurnStatus"] = turn?.Status.ToString(),
                    ["currentTurnIsActing"] = turn != null && turn.IsActing,
                    ["currentTurnIsEnding"] = turn != null && turn.IsEnding
                };
            }
            catch (Exception exception)
            {
                return new JObject
                {
                    ["present"] = true,
                    ["captureError"] = exception.GetType().FullName + ": " + exception.Message
                };
            }
        }

        private JObject CapturePairCommandState()
        {
            try
            {
                var outcome = combat.LastOutcome;
                return new JObject
                {
                    ["frame"] = Time.frameCount,
                    ["stockIntentActive"] = combat.HasStockAttackIntent,
                    ["activePairCommand"] = combat.HasActiveCommand,
                    ["riderManualTargetId"] = rider.CombatState?.ManualTarget?.UniqueId,
                    ["mountManualTargetId"] = horse.CombatState?.ManualTarget?.UniqueId,
                    ["riderRaw"] = CaptureRawCommandSlots(rider),
                    ["riderQueue"] = CaptureQueuedCommands(rider),
                    ["mountRaw"] = CaptureRawCommandSlots(horse),
                    ["mountQueue"] = CaptureQueuedCommands(horse),
                    ["lastOutcome"] = outcome == null
                        ? null
                        : new JObject
                        {
                            ["action"] = outcome.Action.ToString(),
                            ["actorId"] = outcome.ActorId,
                            ["targetId"] = outcome.TargetId,
                            ["result"] = outcome.Result,
                            ["terminalReason"] = outcome.TerminalReason,
                            ["childAttackStartCount"] = outcome.ChildAttackStartCount,
                            ["nativeAttackRuleObserved"] = outcome.NativeAttackRuleObserved
                        }
                };
            }
            catch (Exception exception)
            {
                return new JObject
                {
                    ["frame"] = Time.frameCount,
                    ["captureError"] = exception.GetType().FullName + ": " + exception.Message
                };
            }
        }

        private static JArray CaptureRawCommandSlots(UnitEntityData unit)
        {
            var result = new JArray();
            var raw = unit?.Commands?.Raw;
            if (raw == null)
            {
                return result;
            }
            for (var index = 0; index < raw.Length; index++)
            {
                var item = CaptureCommandState(raw[index]);
                item["slotIndex"] = index;
                item["slot"] = ((UnitCommand.CommandType)index).ToString();
                result.Add(item);
            }
            return result;
        }

        private static JArray CaptureQueuedCommands(UnitEntityData unit)
        {
            var result = new JArray();
            var queue = unit?.Commands?.Queue;
            if (queue == null)
            {
                return result;
            }
            var index = 0;
            foreach (var command in queue)
            {
                var item = CaptureCommandState(command);
                item["queueIndex"] = index++;
                result.Add(item);
            }
            return result;
        }

        private static JObject CaptureCommandState(UnitCommand command)
        {
            if (command == null)
            {
                return new JObject { ["present"] = false };
            }
            return new JObject
            {
                ["present"] = true,
                ["type"] = command.GetType().FullName,
                ["executorId"] = command.Executor?.UniqueId,
                ["targetId"] = command.Target?.Unit?.UniqueId,
                ["commandType"] = command.Type.ToString(),
                ["exactStockUnitAttack"] = command.GetType() == typeof(UnitAttack),
                ["attackOfOpportunity"] = command is UnitAttackOfOpportunity,
                ["mountedPairWrapper"] = command is MountedPairAttackCommand,
                ["createdByPlayer"] = command.CreatedByPlayer,
                ["aiActionPresent"] = command.AiAction != null,
                ["aiActionType"] = command.AiAction?.GetType().FullName,
                ["started"] = command.IsStarted,
                ["running"] = command.IsRunning,
                ["finished"] = command.IsFinished,
                ["acted"] = command.IsActed,
                ["result"] = command.Result.ToString(),
                ["interruptible"] = command.IsInterruptible,
                ["interruptAsSoonAsPossible"] = command.InterruptAsSoonAsPossible
            };
        }

        private JObject CaptureOutcome(
            MountedPairAttackOutcome outcome,
            IReadOnlyList<NativeMountedAbilityActivationRecord> activations)
        {
            return new JObject
            {
                ["outcome"] = outcome == null
                    ? null
                    : JObject.FromObject(outcome, JsonSerializer.Create(JsonSettings)),
                ["activations"] = JArray.FromObject(activations ?? new NativeMountedAbilityActivationRecord[0],
                    JsonSerializer.Create(JsonSettings)),
                ["relationshipState"] = relationship.State.ToString(),
                ["presentation"] = relationship.CapturePresentationObservation(),
                ["rules"] = ruleProbe.CapturePairEvidence(),
                ["nativeControls"] = JObject.FromObject(
                    nativeControls.CaptureSnapshot(), JsonSerializer.Create(JsonSettings)),
                ["unified"] = JObject.FromObject(
                    combat.CaptureUnifiedTurnSnapshot(), JsonSerializer.Create(JsonSettings)),
                ["pairedScheduler"] = JObject.FromObject(
                    combat.CapturePairedCommandSchedulerSnapshot(), JsonSerializer.Create(JsonSettings))
            };
        }

        private JObject CaptureStockEvidence()
        {
            return new JObject
            {
                ["nativeRequestDelta"] = combat.StockAttackNativeRequestCount - stockNativeBefore,
                ["intentStartDelta"] = combat.StockAttackIntentStartCount - stockIntentBefore,
                ["intentCancelDelta"] = combat.StockAttackIntentCancelCount - stockCancelBefore,
                ["riderDispatchDelta"] = combat.StockAttackRiderDispatchCount - stockRiderBefore,
                ["mountDispatchDelta"] = combat.StockAttackMountDispatchCount - stockMountBefore,
                ["duplicateDispatchDelta"] = combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore,
                ["intentActive"] = combat.HasStockAttackIntent,
                ["lastObservation"] = combat.LastStockAttackObservation,
                ["lastFeedback"] = combat.LastFeedback,
                ["rules"] = ruleProbe.CapturePairEvidence(),
                ["unified"] = JObject.FromObject(combat.CaptureUnifiedTurnSnapshot(), JsonSerializer.Create(JsonSettings)),
                ["relationshipState"] = relationship.State.ToString()
            };
        }

        private void AddRow(string name, bool passed, string detail, JToken evidence)
        {
            var rowErrors = passed ? new string[0] : new[] { detail };
            results.Add(new RuntimeSubscenarioResult
            {
                Name = name,
                Status = passed ? "PASS" : "FAIL",
                AssertionPassCount = passed ? 1 : 0,
                AssertionFailCount = passed ? 0 : 1,
                Errors = rowErrors
            });
            rows.Add(new JObject
            {
                ["name"] = name,
                ["status"] = passed ? "PASS" : "FAIL",
                ["detail"] = detail,
                ["frame"] = frame,
                ["seconds"] = clock.Elapsed.TotalSeconds,
                ["evidence"] = evidence
            });
            if (!passed)
            {
                errors.Add(name + ": " + detail);
            }
        }

        private void FailCurrent(string name, string detail)
        {
            AddRow(name, false, detail, new JObject
            {
                ["step"] = step.ToString(),
                ["relationshipState"] = relationship.State.ToString(),
                ["stockObservation"] = combat.LastStockAttackObservation,
                ["feedback"] = combat.LastFeedback,
                ["currentTurnUnitId"] = Game.Instance?.TurnBasedCombatController?.CurrentTurn?.Unit?.UniqueId,
                ["currentTurnStatus"] = Game.Instance?.TurnBasedCombatController?.CurrentTurn?.Status.ToString(),
                ["transitionRiderTurnObserved"] = transitionRiderTurnObserved,
                ["transitionRiderStartRequestCount"] = transitionRiderStartRequestCount,
                ["leafDeadlineProgress"] = CaptureLeafDeadlineProgress(),
                ["nativeControls"] = JObject.FromObject(
                    nativeControls.CaptureSnapshot(), JsonSerializer.Create(JsonSettings)),
                ["lastNativeAbilityShell"] = CaptureNativeAbilityShell(lastNativeAbilityShell)
            });
        }

        private void BeginCleanup()
        {
            if (cleanupStarted)
            {
                return;
            }
            cleanupStarted = true;
            BestEffortCleanup();
            cleanupFrame = frame;
            step = Phase3dHorseStep.AwaitCleanup;
            ResetLeafClock();
        }

        private void BestEffortCleanup()
        {
            try { combat.Cancel("Phase 3D Horse tranche cleanup"); }
            catch (Exception exception) { AddCleanupError("combat", exception); }
            try { combatMountAdjacencyCommand?.Interrupt(); }
            catch (Exception exception) { AddCleanupError("combat Mount adjacency movement", exception); }
            try { movementCommand?.Interrupt(); }
            catch (Exception exception) { AddCleanupError("movement", exception); }
            try { unmountedCommand?.Interrupt(); }
            catch (Exception exception) { AddCleanupError("unmounted command", exception); }
            try
            {
                if (relationship.State != RelationshipState.Unmounted)
                {
                    var cleanup = relationship.Dismount(CleanupTrigger.Exception);
                    if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                    {
                        cleanupError = true;
                        errors.Add("Phase 3D relationship cleanup retained mounted residue.");
                    }
                }
            }
            catch (Exception exception) { AddCleanupError("relationship", exception); }
            if (!RestoreUnmountedHorseAiIsolation())
            {
                cleanupError = true;
                var message = "unmounted Horse AI cleanup: " +
                    (unmountedHorseAiLeaseError ?? "unknown restoration failure") + ".";
                if (!errors.Contains(message))
                {
                    errors.Add(message);
                }
            }
            if (!RestoreCombatMountRiderAiIsolation())
            {
                cleanupError = true;
                var message = "combat-Mount rider AI cleanup: " +
                    (combatMountRiderAiLeaseError ?? "unknown restoration failure") + ".";
                if (!errors.Contains(message))
                {
                    errors.Add(message);
                }
            }
            TryLeaveCombat(target);
            TryLeaveCombat(horse);
            TryLeaveCombat(rider);
            try { targetCleanupComplete = targetService == null || targetService.DestroyAndVerify(); }
            catch (Exception exception) { AddCleanupError("target", exception); }
            try
            {
                if (turnBasedModeProbe != null)
                {
                    turnBasedModeProbe.Dispose();
                    turnBasedModeProbe = null;
                }
                modeRestored = true;
            }
            catch (Exception exception) { AddCleanupError("mode", exception); }
            // Native equipment removal is legal only after leaving combat and restoring
            // the mode. Never drop ownership of an item before that cleanup boundary.
            try { rangedWeaponLease?.Dispose(); rangedWeaponLease = null; }
            catch (Exception exception) { AddCleanupError("ranged weapon", exception); }
            try { ordinaryVariation?.Dispose(); ordinaryVariation = null; }
            catch (Exception exception) { AddCleanupError("Ordinary native stat fixture", exception); }
            try { RestorePhase3hRapidShot(); }
            catch (Exception exception) { AddCleanupError("Rapid Shot fixture feature", exception); }
            if (ordinaryAttackTrace != null)
            {
                observations["ordinaryAttackTrace"] = ordinaryAttackTrace.Capture();
                ordinaryAttackTrace.Dispose();
                ordinaryAttackTrace = null;
            }
            settings.EnablePairedCommandScheduler = originalPairedCommandScheduler;
            settings.EnableUnsafeMovementExperiment = originalUnsafeExperiment;
        }

        private void AwaitCleanup()
        {
            try
            {
                if (!unmountedHorseAiLeaseRestored)
                {
                    RestoreUnmountedHorseAiIsolation();
                }
                if (!combatMountRiderAiLeaseRestored)
                {
                    RestoreCombatMountRiderAiIsolation();
                }
                if (!targetCleanupComplete && targetService != null)
                {
                    targetCleanupComplete = targetService.DestroyAndVerify();
                }
                Game.Instance?.EntityDestroyer?.Tick();
            }
            catch (Exception exception)
            {
                AddCleanupError("cleanup poll", exception);
            }

            if (frame <= cleanupFrame || !targetCleanupComplete || !modeRestored ||
                !unmountedHorseAiLeaseRestored || !combatMountRiderAiLeaseRestored ||
                relationship.State != RelationshipState.Unmounted)
            {
                return;
            }

            try
            {
                targetService?.Dispose();
                targetService = null;
            }
            catch (Exception exception) { AddCleanupError("target disposal", exception); }

            Game.Instance.IsPaused = originalPause;
            if (rider.Body.CurrentHandEquipmentSetIndex != originalEquipmentSet)
            {
                rider.Body.CurrentHandEquipmentSetIndex = originalEquipmentSet;
            }
            RestoreSelection();
            var selected = SelectionManager.Instance.SelectedUnits;
            var expectedSelection = originalSelection.Where(item => item != null && item.IsInState).ToArray();
            var selectionRestored = selected.Count == expectedSelection.Length &&
                expectedSelection.All(item => selected.Contains(item));
            var cleanupPassed = selectionRestored &&
                rider.Body.CurrentHandEquipmentSetIndex == originalEquipmentSet &&
                settings.EnablePairedCommandScheduler == originalPairedCommandScheduler &&
                settings.EnableUnsafeMovementExperiment == originalUnsafeExperiment &&
                relationship.State == RelationshipState.Unmounted &&
                (targetService == null || targetCleanupComplete) && modeRestored &&
                unmountedHorseAiLeaseRestored && combatMountRiderAiLeaseRestored && !cleanupError;
            observations["cleanup"] = new JObject
            {
                ["selectionRestored"] = selectionRestored,
                ["equipmentSetRestored"] = rider.Body.CurrentHandEquipmentSetIndex == originalEquipmentSet,
                ["settingRestored"] = settings.EnableUnsafeMovementExperiment == originalUnsafeExperiment,
                ["pairedSchedulerSettingRestored"] =
                    settings.EnablePairedCommandScheduler == originalPairedCommandScheduler,
                ["relationshipState"] = relationship.State.ToString(),
                ["targetClean"] = targetCleanupComplete,
                ["modeRestored"] = modeRestored,
                ["unmountedHorseAiLeaseRestored"] = unmountedHorseAiLeaseRestored,
                ["unmountedHorseAiIsolation"] = CaptureUnmountedHorseAiIsolation(),
                ["combatMountRiderAiLeaseRestored"] = combatMountRiderAiLeaseRestored,
                ["combatMountRiderAiIsolation"] = CaptureCombatMountRiderAiIsolation()
            };
            if (!cleanupPassed)
            {
                AddRow("phase3d-horse-tranche-cleanup", false,
                    "Phase 3D Horse tranche did not restore every acquired local lease.", observations["cleanup"]);
            }

            WriteEvidence();
            completed = true;
            logger.Info("Phase 3D Horse tranche completed: scenario=" + request.Scenario +
                "; rows=" + results.Count + "; failures=" + results.Count(item => item.Status == "FAIL") + ".");
        }

        private void WriteEvidence()
        {
            if (motionEvidence != null)
            {
                motionEvidence.Dispose();
                observations["phase3fMotionFrames"] = motionEvidence.Snapshot();
            }
            if (string.Equals(request.Scenario, TurnBasedScenario, StringComparison.Ordinal))
            {
                observations["nativeTurnTraversal"] = CaptureNativeTurnTraversalEvidence();
            }
            var path = Path.Combine(request.EvidenceRoot, EvidenceFileName);
            if (File.Exists(path))
            {
                throw new InvalidOperationException("Phase 3D Horse evidence artifact already exists.");
            }
            var artifact = new JObject
            {
                ["schemaVersion"] = IsOrdinaryAttackControls ? 1 : IsPhase3hLoop ? 9 : IsPhase3gControls ? 8 : IsPhase3fNativeControlScope ? 7 : 6,
                ["evidenceKind"] = EvidenceKind,
                ["runId"] = request.RunId,
                ["scenario"] = request.Scenario,
                ["branch"] = request.Branch,
                ["commit"] = request.Commit,
                ["productVersion"] = request.ProductVersion,
                ["dllSha256"] = ComputeSha256(typeof(Main).Assembly.Location),
                ["dllMvid"] = typeof(Main).Assembly.ManifestModule.ModuleVersionId.ToString(),
                ["createdAtUtc"] = DateTimeOffset.UtcNow.ToString("o"),
                ["status"] = errors.Count == 0 &&
                    results.All(item => string.Equals(item.Status, "PASS", StringComparison.Ordinal))
                    ? "PASS"
                    : "FAIL",
                ["rows"] = rows,
                ["observations"] = observations,
                ["subscenarioPassCount"] = results.Count(item => item.Status == "PASS"),
                ["subscenarioFailCount"] = results.Count(item => item.Status == "FAIL"),
                ["errors"] = new JArray(errors)
            };
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonConvert.SerializeObject(artifact, JsonSettings), new UTF8Encoding(false));
                File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private Vector3 FindWalkablePoint(Vector3 origin, float requestedDistance, float tolerance)
        {
            if (global::AstarPath.active == null)
            {
                throw new InvalidOperationException("Active native navigation graph is unavailable.");
            }
            var baseDirection = horse.View == null ? Vector3.forward : horse.View.transform.forward;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude < 0.01f)
            {
                baseDirection = Vector3.forward;
            }
            baseDirection.Normalize();
            for (var index = 0; index < 32; index++)
            {
                var direction = Quaternion.Euler(0f, index * 11.25f, 0f) * baseDirection;
                var nearest = global::AstarPath.active.GetNearest(origin + direction * requestedDistance);
                if (nearest.node == null || !nearest.node.Walkable)
                {
                    continue;
                }
                var point = nearest.clampedPosition;
                var distance = HorizontalDistance(origin, point);
                if (distance >= 0.25f && Math.Abs(distance - requestedDistance) <= tolerance)
                {
                    return point;
                }
            }
            throw new InvalidOperationException("No bounded walkable Phase 3D point satisfied the requested distance.");
        }

        private Vector3 FindWalkablePointAwayFromTarget(Vector3 origin, Vector3 targetPosition, float distance)
        {
            var direction = origin - targetPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = horse.View?.transform.forward ?? Vector3.forward;
            }
            direction.Normalize();
            for (var index = 0; index < 16; index++)
            {
                var rotated = Quaternion.Euler(0f, index % 2 == 0 ? index * 11.25f : -index * 11.25f, 0f) * direction;
                var nearest = global::AstarPath.active.GetNearest(origin + rotated * distance);
                if (nearest.node != null && nearest.node.Walkable &&
                    HorizontalDistance(origin, nearest.clampedPosition) >= 0.5f)
                {
                    return nearest.clampedPosition;
                }
            }
            throw new InvalidOperationException("No bounded mounted five-foot destination was available.");
        }

        private Vector3 FindWalkablePointNearTarget(Vector3 targetPosition, Vector3 origin, float distance)
        {
            if (global::AstarPath.active == null)
            {
                throw new InvalidOperationException("Active native navigation graph is unavailable.");
            }
            var direction = origin - targetPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = horse.View?.transform.forward ?? Vector3.forward;
            }
            direction.Normalize();
            for (var index = 0; index < 24; index++)
            {
                var angle = index == 0 ? 0f : (index % 2 == 0 ? index : -index) * 7.5f;
                var rotated = Quaternion.Euler(0f, angle, 0f) * direction;
                var nearest = global::AstarPath.active.GetNearest(targetPosition + rotated * distance);
                if (nearest.node != null && nearest.node.Walkable &&
                    HorizontalDistance(targetPosition, nearest.clampedPosition) <= distance + 0.45f &&
                    HorizontalDistance(origin, nearest.clampedPosition) >= 0.25f)
                {
                    return nearest.clampedPosition;
                }
            }
            throw new InvalidOperationException("No bounded walkable adjacent ranged-control point was available.");
        }

        private void RestoreSelection()
        {
            SelectionManager.Instance.MultiSelect(
                originalSelection.Where(item => item != null && item.IsInState && item.View != null)
                    .Select(item => item.View),
                false);
        }

        private void AddCleanupError(string scope, Exception exception)
        {
            cleanupError = true;
            var message = scope + " cleanup: " + exception.GetType().Name + ": " + exception.Message;
            if (!errors.Contains(message))
            {
                errors.Add(message);
            }
        }

        private void ResetLeafClock()
        {
            leafClock.Restart();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Phase3dHorseScenarioTranche));
            }
        }

        private static void TryLeaveCombat(UnitEntityData unit)
        {
            if (unit != null && unit.IsInState && unit.IsInCombat)
            {
                unit.LeaveCombat();
            }
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            var dx = first.x - second.x;
            var dz = first.z - second.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static string ComputeSha256(string path)
        {
            using (var algorithm = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private enum Phase3dHorseStep
        {
            Phase3gControls,
            PresentationSettle,
            AwaitMountedCombat,
            AwaitRiderPrimaryRt,
            AwaitMountPrimaryRtAdmission,
            AwaitMountPrimaryRt,
            AwaitStockMeleeTargetCleanupRt,
            AwaitStockMeleeAdmissionRt,
            AwaitStockMeleeRt,
            AwaitStockCancelRt,
            AwaitRangedTargetCleanup,
            AwaitRangedCombat,
            AwaitRangedAttackRt,
            AwaitRangedCancelRt,
            AwaitRangedAdjacentMoveRt,
            AwaitRangedAdjacentAttackRt,
            AwaitRangedAdjacentCancelRt,
            AwaitRangedVariantTargetCleanupRt,
            AwaitRangedVariantAdmissionRt,
            AwaitRangedVariantRt,
            AwaitRangedVariantCancelRt,
            AwaitRtToTbTransition,
            AwaitRiderPrimaryAfterTransition,
            AwaitTbToRtTransition,
            AwaitRtCombatDismountAdmission,
            AwaitRtCombatDismount,
            AwaitUnmountedHorseAiIsolation,
            AwaitUnmountedTargetCleanupRt,
            AwaitUnmountedMeleeRt,
            AwaitUnmountedMeleeCancelRt,
            AwaitUnmountedRangedTargetCleanupRt,
            AwaitUnmountedRangedAdmissionRt,
            AwaitUnmountedRangedRt,
            AwaitCombatMountAdjacencyReadiness,
            AwaitCombatMountAdjacencyMove,
            AwaitCombatMountHorseAiIsolation,
            AwaitUnmountedCombat,
            AwaitTurnBasedMode,
            AwaitRiderTurnForMount,
            AwaitCombatMount,
            AwaitRiderPrimaryTb,
            AwaitNextRiderTurnForMountPrimaryTb,
            AwaitMountPrimaryTb,
            AwaitNextRiderTurnForStockMeleeTb,
            AwaitStockMeleeTb,
            AwaitNextRiderTurnForRangedTb,
            AwaitRangedTb,
            AwaitNextRiderTurnForStep,
            AwaitFiveFootStep,
            AwaitNextRiderTurnForOrdinaryMove,
            AwaitOrdinaryMove,
            AwaitNextRiderTurnForDismount,
            AwaitCombatDismount,
            AwaitNextRiderTurnForUnmountedStep,
            AwaitUnmountedStep,
            AwaitRiderSpentAttackTb,
            AwaitRiderSpentCombatMount,
            AwaitNextRiderTurnForSpentControlDismount,
            AwaitSpentControlDismount,
            AwaitHorseTurnForSpentControl,
            AwaitHorseSpentAttackTb,
            AwaitRiderTurnForMountSpentControl,
            AwaitMountSpentCombatMount,
            AwaitCleanup
        }
    }

    internal sealed class Phase3dRangedWeaponLease : IDisposable
    {
        private readonly UnitEntityData rider;
        private readonly int originalSetIndex;
        private int leasedSetIndex = -1;
        private ItemEntityWeapon createdItem;
        private HandSlot createdSlot;
        private bool disposed;

        internal Phase3dRangedWeaponLease(UnitEntityData rider)
        {
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            originalSetIndex = rider.Body.CurrentHandEquipmentSetIndex;
        }

        internal bool IsReady => leasedSetIndex >= 0 &&
            rider.Body.CurrentHandEquipmentSetIndex == leasedSetIndex &&
            rider.GetFirstWeapon()?.Blueprint?.IsRanged == true;

        internal void Acquire(WeaponCategory category)
        {
            if (disposed || leasedSetIndex >= 0)
            {
                throw new InvalidOperationException("Phase 3D ranged weapon lease is not available.");
            }

            for (var index = 0; index < rider.Body.HandsEquipmentSets.Count; index++)
            {
                var weapon = rider.Body.HandsEquipmentSets[index].PrimaryHand.MaybeWeapon?.Blueprint;
                if (weapon != null && weapon.IsRanged && weapon.Category == category)
                {
                    leasedSetIndex = index;
                    rider.Body.CurrentHandEquipmentSetIndex = index;
                    return;
                }
            }

            var emptyIndex = Enumerable.Range(0, rider.Body.HandsEquipmentSets.Count)
                .Where(index => index != originalSetIndex && rider.Body.HandsEquipmentSets[index].IsEmpty())
                .DefaultIfEmpty(-1)
                .First();
            if (emptyIndex < 0)
            {
                throw new InvalidOperationException("No empty non-current equipment set is available for a reversible ranged lease.");
            }

            var rangedBlueprint = ResourcesLibrary.LibraryObject.GetAllBlueprints()
                .OfType<BlueprintItemWeapon>()
                .Where(candidate => candidate != null && candidate.IsRanged && candidate.Category == category &&
                    !candidate.IsMagic && !candidate.IsMasterwork && !candidate.IsNatural)
                .OrderBy(candidate => candidate.Cost)
                .ThenBy(candidate => candidate.name, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.AssetGuid, StringComparer.Ordinal)
                .FirstOrDefault();
            if (rangedBlueprint == null)
            {
                throw new InvalidOperationException("No deterministic stock ranged blueprint exists for " + category + ".");
            }

            var rangedItem = rangedBlueprint.CreateEntity<ItemEntityWeapon>();
            rangedItem = rider.Inventory.Add(rangedItem, true) as ItemEntityWeapon;
            if (rangedItem == null || rangedItem.Collection != rider.Inventory)
            {
                throw new InvalidOperationException("Deterministic stock ranged item did not enter the exact rider inventory lease.");
            }
            var slot = rider.Body.HandsEquipmentSets[emptyIndex].PrimaryHand;
            if (!slot.CanInsertItem(rangedItem))
            {
                rider.Inventory.Remove(rangedItem);
                rangedItem.Dispose();
                throw new InvalidOperationException("Exact rider cannot equip the deterministic stock " + category + " lease.");
            }
            slot.InsertItem(rangedItem);
            if (!ReferenceEquals(slot.MaybeItem, rangedItem) || rangedItem.Collection != rider.Inventory)
            {
                slot.RemoveItem();
                rider.Inventory.Remove(rangedItem);
                rangedItem.Dispose();
                throw new InvalidOperationException("Deterministic stock ranged item did not enter the exact rider lease slot.");
            }
            createdItem = rangedItem;
            createdSlot = slot;
            leasedSetIndex = emptyIndex;
            rider.Body.CurrentHandEquipmentSetIndex = leasedSetIndex;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            rider.Body.CurrentHandEquipmentSetIndex = originalSetIndex;
            if (createdItem != null)
            {
                if (createdSlot != null && ReferenceEquals(createdSlot.MaybeItem, createdItem))
                {
                    if (!createdSlot.RemoveItem())
                    {
                        throw new InvalidOperationException("Phase 3D ranged item could not be removed from its exact lease slot.");
                    }
                }
                if (createdItem.Collection != null)
                {
                    createdItem.Collection.Remove(createdItem);
                }
                createdItem.Dispose();
            }
            createdItem = null;
            createdSlot = null;
            leasedSetIndex = -1;
            disposed = true;
        }
    }

    internal sealed class Phase3dCombatRuleProbe :
        IGlobalRulebookHandler<RuleAttackWithWeapon>,
        IGlobalRulebookHandler<RuleAttackWithWeaponResolve>,
        IGlobalRulebookHandler<RuleAttackRoll>,
        IGlobalRulebookHandler<RuleRollDice>,
        IGlobalRulebookHandler<RuleDealDamage>,
        IDisposable
    {
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly IDisposable subscription;
        private readonly JArray pairAttackRuleEvents = new JArray();
        private readonly JArray pairAttackRollEvents = new JArray();
        private UnitEntityData expectedTarget;
        private bool forcePairHit;
        private bool disposed;

        internal Phase3dCombatRuleProbe(UnitEntityData rider, UnitEntityData mount)
        {
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            subscription = EventBus.Subscribe(this);
        }

        internal int RiderAttackRuleCount { get; private set; }
        internal int RiderResolvedCount { get; private set; }
        internal int MountResolvedCount { get; private set; }
        internal int MountAttackRuleCount { get; private set; }
        internal int PairAttackRuleCount => RiderAttackRuleCount + MountAttackRuleCount;
        internal int RiderOpportunityAttackRuleCount { get; private set; }
        internal int MountOpportunityAttackRuleCount { get; private set; }
        internal int PairOpportunityAttackRuleCount =>
            RiderOpportunityAttackRuleCount + MountOpportunityAttackRuleCount;
        internal int PairNonOpportunityAttackRuleCount =>
            PairAttackRuleCount - PairOpportunityAttackRuleCount;
        internal int RiderNonOpportunityAttackRuleCount =>
            RiderAttackRuleCount - RiderOpportunityAttackRuleCount;
        internal int MountNonOpportunityAttackRuleCount =>
            MountAttackRuleCount - MountOpportunityAttackRuleCount;
        internal int PairAttackRollCount { get; private set; }
        internal int PairOpportunityAttackRollCount { get; private set; }
        internal int PairDamageRuleCount { get; private set; }
        internal int PairForcedD20Count { get; private set; }
        internal int PairDamage { get; private set; }
        internal int OpportunityAttackRuleCount { get; private set; }
        internal int OpportunityAttackRollCount { get; private set; }
        internal int OpportunityDamageRuleCount { get; private set; }
        internal int ExpectedTargetForcedD20Count { get; private set; }
        internal string FirstPairAttackActorId { get; private set; }
        internal string LastPairAttackActorId { get; private set; }
        internal string LastRiderAttackType { get; private set; }
        internal bool? LastRiderAttackDoNotProvoke { get; private set; }
        internal string LastOpportunityActorId { get; private set; }
        internal string LastOpportunityTargetId { get; private set; }

        internal void Arm(UnitEntityData target, bool forceHit)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Phase3dCombatRuleProbe));
            }
            expectedTarget = target ?? throw new ArgumentNullException(nameof(target));
            forcePairHit = forceHit;
            RiderAttackRuleCount = 0;
            RiderResolvedCount = 0;
            MountResolvedCount = 0;
            MountAttackRuleCount = 0;
            RiderOpportunityAttackRuleCount = 0;
            MountOpportunityAttackRuleCount = 0;
            PairAttackRollCount = 0;
            PairOpportunityAttackRollCount = 0;
            PairDamageRuleCount = 0;
            PairForcedD20Count = 0;
            PairDamage = 0;
            ExpectedTargetForcedD20Count = 0;
            FirstPairAttackActorId = null;
            LastPairAttackActorId = null;
            LastRiderAttackType = null;
            LastRiderAttackDoNotProvoke = null;
            pairAttackRuleEvents.Clear();
            pairAttackRollEvents.Clear();
            ResetOpportunityCounts();
        }

        internal void ResetOpportunityCounts()
        {
            OpportunityAttackRuleCount = 0;
            OpportunityAttackRollCount = 0;
            OpportunityDamageRuleCount = 0;
            LastOpportunityActorId = null;
            LastOpportunityTargetId = null;
        }

        internal JObject CapturePairEvidence()
        {
            return new JObject
            {
                ["riderAttackRules"] = RiderAttackRuleCount,
                ["riderResolved"] = RiderResolvedCount,
                ["mountResolved"] = MountResolvedCount,
                ["mountAttackRules"] = MountAttackRuleCount,
                ["riderNonOpportunityAttackRules"] = RiderNonOpportunityAttackRuleCount,
                ["mountNonOpportunityAttackRules"] = MountNonOpportunityAttackRuleCount,
                ["pairNonOpportunityAttackRules"] = PairNonOpportunityAttackRuleCount,
                ["riderOpportunityAttackRules"] = RiderOpportunityAttackRuleCount,
                ["mountOpportunityAttackRules"] = MountOpportunityAttackRuleCount,
                ["pairOpportunityAttackRules"] = PairOpportunityAttackRuleCount,
                ["pairAttackRolls"] = PairAttackRollCount,
                ["pairOpportunityAttackRolls"] = PairOpportunityAttackRollCount,
                ["pairDamageRules"] = PairDamageRuleCount,
                ["pairForcedD20"] = PairForcedD20Count,
                ["pairDamage"] = PairDamage,
                ["firstPairActorId"] = FirstPairAttackActorId,
                ["lastPairActorId"] = LastPairAttackActorId,
                ["lastRiderAttackType"] = LastRiderAttackType,
                ["lastRiderAttackDoNotProvoke"] = LastRiderAttackDoNotProvoke,
                ["attackRuleEvents"] = pairAttackRuleEvents.DeepClone(),
                ["attackRollEvents"] = pairAttackRollEvents.DeepClone()
            };
        }

        internal JObject CaptureOpportunityEvidence()
        {
            return new JObject
            {
                ["attackRules"] = OpportunityAttackRuleCount,
                ["attackRolls"] = OpportunityAttackRollCount,
                ["damageRules"] = OpportunityDamageRuleCount,
                ["expectedTargetForcedD20"] = ExpectedTargetForcedD20Count,
                ["lastActorId"] = LastOpportunityActorId,
                ["lastTargetId"] = LastOpportunityTargetId
            };
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (evt == null)
            {
                return;
            }
            if (evt.IsAttackOfOpportunity && (evt.Target == rider || evt.Target == mount))
            {
                OpportunityAttackRuleCount++;
                LastOpportunityActorId = evt.Initiator?.UniqueId;
                LastOpportunityTargetId = evt.Target?.UniqueId;
                return;
            }
            if (evt.Target != expectedTarget)
            {
                return;
            }
            if (evt.Initiator == rider)
            {
                RiderAttackRuleCount++;
                if (evt.IsAttackOfOpportunity)
                {
                    RiderOpportunityAttackRuleCount++;
                }
            }
            else if (evt.Initiator == mount)
            {
                MountAttackRuleCount++;
                if (evt.IsAttackOfOpportunity)
                {
                    MountOpportunityAttackRuleCount++;
                }
            }
            else
            {
                return;
            }
            if (FirstPairAttackActorId == null)
            {
                FirstPairAttackActorId = evt.Initiator.UniqueId;
            }
            LastPairAttackActorId = evt.Initiator.UniqueId;
            pairAttackRuleEvents.Add(new JObject
            {
                ["sequence"] = PairAttackRuleCount,
                ["frame"] = Time.frameCount,
                ["actorId"] = evt.Initiator.UniqueId,
                ["targetId"] = evt.Target.UniqueId,
                ["attackOfOpportunity"] = evt.IsAttackOfOpportunity,
                ["firstAttack"] = evt.IsFirstAttack,
                ["fullAttack"] = evt.IsFullAttack,
                ["charge"] = evt.IsCharge,
                ["attackNumber"] = evt.AttackNumber,
                ["attacksCount"] = evt.AttacksCount,
                ["weaponGuid"] = evt.Weapon?.Blueprint?.AssetGuid
            });
        }

        public void OnEventAboutToTrigger(RuleAttackWithWeaponResolve evt) { }

        public void OnEventDidTrigger(RuleAttackWithWeaponResolve evt)
        {
            if (evt?.Target != expectedTarget || evt.AttackWithWeapon.IsAttackOfOpportunity) { return; }
            if (evt.Initiator == rider) { RiderResolvedCount++; }
            if (evt.Initiator == mount) { MountResolvedCount++; }
        }

        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            if (evt == null)
            {
                return;
            }
            if (evt.RuleAttackWithWeapon != null && evt.RuleAttackWithWeapon.IsAttackOfOpportunity &&
                (evt.Target == rider || evt.Target == mount))
            {
                OpportunityAttackRollCount++;
            }
            else if (evt.Target == expectedTarget && (evt.Initiator == rider || evt.Initiator == mount))
            {
                PairAttackRollCount++;
                pairAttackRollEvents.Add(new JObject {
                    ["actor"] = evt.Initiator.UniqueId, ["target"] = evt.Target.UniqueId,
                    ["attackBonus"] = evt.AttackBonus, ["iterativePenalty"] = evt.AttackBonusPenalty,
                    ["statModifier"] = evt.AttackBonusRule?.AttackBonusStatModifier,
                    ["secondaryBonus"] = evt.AttackBonusRule?.SecondaryBonus,
                    ["shootIntoCombatBonus"] = evt.AttackBonusRule?.ShootIntoCombatBonus,
                    ["bonuses"] = evt.AttackBonusRule == null ? null : new JArray(evt.AttackBonusRule.BonusSources.Select(item => new JObject {
                        ["bonus"] = item.Bonus, ["source"] = item.Source?.Blueprint.AssetGuid,
                        ["sourceName"] = item.Source?.Blueprint.name }))
                });
                if (evt.RuleAttackWithWeapon != null && evt.RuleAttackWithWeapon.IsAttackOfOpportunity)
                {
                    PairOpportunityAttackRollCount++;
                }
                if (evt.Initiator == rider)
                {
                    LastRiderAttackType = evt.AttackType.ToString();
                    LastRiderAttackDoNotProvoke = evt.DoNotProvokeAttacksOfOpportunity;
                }
            }
        }

        public void OnEventAboutToTrigger(RuleRollDice evt)
        {
            if (!forcePairHit || evt == null || evt.DiceFormula.Rolls != 1 ||
                evt.DiceFormula.Dice != Kingmaker.RuleSystem.DiceType.D20)
            {
                return;
            }
            if (evt.Initiator == rider || evt.Initiator == mount)
            {
                evt.Override(20);
                PairForcedD20Count++;
            }
            else if (expectedTarget != null && evt.Initiator == expectedTarget)
            {
                evt.Override(20);
                ExpectedTargetForcedD20Count++;
            }
        }

        public void OnEventDidTrigger(RuleRollDice evt)
        {
        }

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt == null)
            {
                return;
            }
            if (evt.Target == expectedTarget && (evt.Initiator == rider || evt.Initiator == mount))
            {
                PairDamageRuleCount++;
                PairDamage += evt.Damage;
            }
            else if ((evt.Target == rider || evt.Target == mount) && evt.Initiator == expectedTarget)
            {
                OpportunityDamageRuleCount++;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            subscription.Dispose();
            disposed = true;
        }
    }
}
