using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UI._ConsoleUI.TurnBasedMode;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
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
    internal sealed class Phase3dHorseScenarioTranche : IDisposable
    {
        internal const string RealTimeScenario = "phase3d-unified-combat-rt-suite";
        internal const string TurnBasedScenario = "phase3d-unified-combat-tb-suite";
        internal const string PresentationScenario = "phase3d-horse-presentation-suite";
        internal const string EvidenceFileName = "phase3d-horse-scenario-evidence.json";
        internal const string EvidenceKind = "phase3d-horse-scenario-evidence";

        private const double ScenarioDeadlineSeconds = 300.0d;
        private const double LeafDeadlineSeconds = 30.0d;
        private const float TargetDistance = 6.0f;
        private const float LongRangeTargetDistance = 19.0f;
        private const float MovementTolerance = 0.8f;

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
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly JArray rows = new JArray();
        private readonly JObject observations = new JObject();

        private Phase3dHorseStep step;
        private DiagnosticCombatTargetService targetService;
        private Phase3dCombatRuleProbe ruleProbe;
        private NativeModeTransitionProbe turnBasedModeProbe;
        private Phase3dRangedWeaponLease rangedWeaponLease;
        private WeaponCategory rangedVariantCategory;
        private UnitEntityData target;
        private UnitMoveTo movementCommand;
        private UnitCommand unmountedCommand;
        private Vector3 movementStart;
        private Vector3 movementDestination;
        private UnifiedMountedTurnSnapshot turnSnapshotBefore;
        private long stockNativeBefore;
        private long stockIntentBefore;
        private long stockRiderBefore;
        private long stockMountBefore;
        private long stockCancelBefore;
        private long stockDuplicateBefore;
        private MountedPairAttackOutcome outcomeBefore;
        private long activationSequenceBefore;
        private int attackRulesBeforeCancel;
        private long stepSuppressionBefore;
        private float riderStandardBeforeMount;
        private float riderMoveBeforeMount;
        private float mountStandardBeforeMount;
        private float mountMoveBeforeMount;
        private int stableFrames;
        private int originalEquipmentSet;
        private bool originalPause;
        private bool originalUnsafeExperiment;
        private UnitEntityData[] originalSelection;
        private bool started;
        private bool completed;
        private bool disposed;
        private bool cleanupStarted;
        private bool cleanupError;
        private bool transitionPrimaryDispatched;
        private bool targetCleanupComplete;
        private bool modeRestored;
        private int cleanupFrame;
        private int frame;

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
                string.Equals(scenario, TurnBasedScenario, StringComparison.Ordinal) ||
                string.Equals(scenario, PresentationScenario, StringComparison.Ordinal);
        }

        internal bool IsCompleted => completed;

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
            if (string.Equals(request.Scenario, TurnBasedScenario, StringComparison.Ordinal) && pairAlreadyMounted)
            {
                throw new InvalidOperationException("Combat-Mount qualification must begin with the exact Horse pair unmounted.");
            }
            if (!string.Equals(request.Scenario, TurnBasedScenario, StringComparison.Ordinal) && !pairAlreadyMounted)
            {
                throw new InvalidOperationException("RT/presentation qualification requires the exact Horse pair already mounted.");
            }

            originalPause = game.IsPaused;
            originalUnsafeExperiment = settings.EnableUnsafeMovementExperiment;
            originalSelection = SelectionManager.Instance.SelectedUnits.Where(item => item != null).ToArray();
            originalEquipmentSet = rider.Body.CurrentHandEquipmentSetIndex;
            settings.EnableUnsafeMovementExperiment = true;
            nativeControls.Update();
            ruleProbe = new Phase3dCombatRuleProbe(rider, horse);
            started = true;
            clock.Start();
            leafClock.Start();

            observations["riderId"] = rider.UniqueId;
            observations["horseId"] = horse.UniqueId;
            observations["initialRelationshipState"] = relationship.State.ToString();
            observations["initialTurnBased"] = CombatController.IsInTurnBasedCombat();
            observations["initialSelection"] = new JArray(originalSelection.Select(item => item.UniqueId));

            if (string.Equals(request.Scenario, PresentationScenario, StringComparison.Ordinal))
            {
                step = Phase3dHorseStep.PresentationSettle;
                return;
            }

            if (string.Equals(request.Scenario, TurnBasedScenario, StringComparison.Ordinal))
            {
                BeginTarget(TargetDistance, "tb-combat-mount");
                step = Phase3dHorseStep.AwaitUnmountedCombat;
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
                targetService?.ObserveTargetLifeState();
                targetService?.RefreshBidirectionalCombatMemoryLease();
                if (!cleanupStarted && clock.Elapsed.TotalSeconds > ScenarioDeadlineSeconds)
                {
                    FailCurrent("phase3d-horse-scenario-deadline", "Phase 3D Horse tranche exceeded 300 seconds at " + step + ".");
                    BeginCleanup();
                }
                else if (!cleanupStarted && leafClock.Elapsed.TotalSeconds > LeafDeadlineSeconds)
                {
                    FailCurrent("phase3d-horse-leaf-deadline", "Phase 3D Horse tranche leaf exceeded 30 seconds at " + step + ".");
                    BeginCleanup();
                }

                switch (step)
                {
                    case Phase3dHorseStep.PresentationSettle:
                        ObservePresentation();
                        break;
                    case Phase3dHorseStep.AwaitMountedCombat:
                        AwaitMountedCombat();
                        break;
                    case Phase3dHorseStep.AwaitRiderPrimaryRt:
                        AwaitRiderPrimaryRt();
                        break;
                    case Phase3dHorseStep.AwaitMountPrimaryRt:
                        AwaitMountPrimaryRt();
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
                    case Phase3dHorseStep.AwaitRtCombatDismount:
                        AwaitRtCombatDismount();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedMeleeRt:
                        AwaitUnmountedMeleeRt();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedMeleeCancelRt:
                        AwaitUnmountedMeleeCancelRt();
                        break;
                    case Phase3dHorseStep.AwaitUnmountedRangedRt:
                        AwaitUnmountedRangedRt();
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
            var horseIcon = horseService.HorseIcon;
            var mountAbility = nativeControls.MountAbility;
            var dismountAbility = nativeControls.DismountAbility;
            var presentation = relationship.CapturePresentationObservation();
            observations["presentation"] = JObject.FromObject(presentation, JsonSerializer.Create(JsonSettings));
            observations["smallPortrait"] = SpriteEvidence(small);
            observations["horseIdentityIcon"] = SpriteEvidence(horseIcon);
            observations["saddleIcon"] = SpriteEvidence(saddle);
            var horsePose = SupportedMountedProfiles.Horse.RiderPoseProfile;
            observations["pelvisOffset"] = JObject.FromObject(
                horsePose.PelvisPositionOffset,
                JsonSerializer.Create(JsonSettings));

            AddRow(
                "Horse-small-portrait-close-up",
                small != null && small.texture != null && small.texture.width == 185 && small.texture.height == 242 &&
                    !ReferenceEquals(small, horseIcon),
                "The exact runtime Horse small portrait is the KMC 185x242 close-up and remains distinct from the companion identity icon.",
                new JObject { ["sprite"] = SpriteEvidence(small) });
            AddRow(
                "saddle-icon",
                saddle != null && saddle.texture != null && saddle.texture.width == 128 && saddle.texture.height == 128 &&
                    !ReferenceEquals(saddle, horseIcon) && ReferenceEquals(mountAbility?.Icon, saddle) &&
                    ReferenceEquals(dismountAbility?.Icon, saddle),
                "Mount and Dismount reference the distinct original 128x128 KMC saddle icon; Horse identity retains its own art.",
                new JObject { ["sprite"] = SpriteEvidence(saddle) });
            AddRow(
                "Horse-pose-final-idle-walk-run-turn-stop",
                relationship.State == RelationshipState.Mounted && runtime.PoseHealthy && runtime.PoseFrameApplied &&
                    string.Equals(runtime.MountProfileId, SupportedMountedProfiles.Horse.Id, StringComparison.Ordinal) &&
                    Math.Abs(horsePose.PelvisPositionOffset.Y - (-0.29d)) <= 0.0001d &&
                    runtime.PoseFootTargetClampCount == 0 &&
                    runtime.PoseMaximumSegmentLengthResidualWorldUnits <= 0.0001d,
                "The final Horse-only profile applies pelvis Y=-0.29 with healthy bounded procedural pose state; visual contact remains a manual gate.",
                JObject.FromObject(presentation, JsonSerializer.Create(JsonSettings)));
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
            if (!IsCombatReady(true))
            {
                return;
            }

            RunRiderPrimaryCancelAndRejectionControls();
            outcomeBefore = combat.LastOutcome;
            activationSequenceBefore = nativeControls.SnapshotAbilityActivations()
                .Select(item => item.Sequence)
                .DefaultIfEmpty(0L)
                .Max();
            ruleProbe.Arm(target, true);
            movementStart = horse.Position;
            var clicked = TryNativeAbilityTargetClick(nativeControls.RiderPrimaryAbility, target, "rt-rider-primary");
            if (!clicked)
            {
                FailCurrent("rider-primary-does-not-dismount-rt", "Native Rider Primary target click was not admitted.");
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
            AddRow(
                "rider-primary-does-not-dismount-rt",
                retained && outcome != null && outcome.Action == MountedCombatActionKind.RiderMelee &&
                    outcome.ChildAttackStartCount == 1,
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

            BeginMountPrimaryRt();
        }

        private void BeginMountPrimaryRt()
        {
            outcomeBefore = combat.LastOutcome;
            ruleProbe.Arm(target, true);
            var clicked = TryNativeAbilityTargetClick(nativeControls.MountPrimaryAbility, target, "rt-mount-primary");
            if (!clicked)
            {
                FailCurrent("mounted-stock-click-melee-mount-only-explicit", "Native RT Mount Primary target click was not admitted.");
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
            AddRow(
                "mounted-stock-click-melee-mount-only-explicit",
                retained && outcome != null && outcome.Action == MountedCombatActionKind.MountPrimaryNatural &&
                    outcome.ResourceOwnerId == horse.UniqueId && ruleProbe.RiderAttackRuleCount == 0 &&
                    ruleProbe.MountAttackRuleCount == 1,
                "Explicit RT Mount Primary spent only the Horse attack ledger and retained the exact pair.",
                evidence);
            if (!retained)
            {
                BeginCleanup();
                return;
            }

            BeginStockMeleeRt();
        }

        private void BeginStockMeleeRt()
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            combat.Cancel("Phase 3D stock melee boundary");
            ruleProbe.Arm(target, true);
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            stockRiderBefore = combat.StockAttackRiderDispatchCount;
            stockMountBefore = combat.StockAttackMountDispatchCount;
            stockDuplicateBefore = combat.StockAttackDuplicateDispatchCount;
            targetService.BeginExpectedAttackDispatch(target);
            var clicked = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            observations["stockMeleeRtAdmission"] = new JObject
            {
                ["clicked"] = clicked,
                ["lastObservation"] = combat.LastStockAttackObservation,
                ["feedback"] = combat.LastFeedback
            };
            if (!clicked && combat.StockAttackIntentStartCount == stockIntentBefore)
            {
                FailCurrent("mounted-stock-click-melee-adjacent-rt", "Ordinary native hostile click did not create mounted stock intent.");
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
            if (riderDispatches < 2 || mountDispatches < 1 || combat.HasActiveCommand)
            {
                return;
            }

            var exactNative = combat.StockAttackNativeRequestCount - stockNativeBefore == 1 &&
                combat.StockAttackIntentStartCount - stockIntentBefore == 1;
            var zeroDuplicates = combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore == 0;
            var evidence = CaptureStockEvidence();
            AddRow("mounted-stock-click-melee-adjacent-rt",
                exactNative && riderDispatches >= 1 && ruleProbe.RiderAttackRuleCount >= 1,
                "One ordinary native hostile click admitted rider-principal mounted melee.", evidence);
            AddRow("mounted-stock-click-melee-approach-rt",
                relationship.State == RelationshipState.Mounted && relationship.Runtime.PoseHealthy &&
                    ruleProbe.PairAttackRuleCount >= 2,
                "The pair retained mount-owned physical approach and rider-owned attack semantics.", evidence);
            AddRow("mounted-stock-click-melee-auto-repeat-rt",
                riderDispatches >= 2 && mountDispatches >= 1 && combat.HasStockAttackIntent,
                "One hostile click persisted through at least two rider dispatches and one separately owned Horse primary dispatch.", evidence);
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
            stockCancelBefore = combat.StockAttackIntentCancelCount;
            var cancelPoint = FindWalkablePoint(horse.Position, 2.0f, 0.6f);
            ClickGroundHandler.MoveSelectedUnitsToPoint(cancelPoint, false);
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
            AddRow("mounted-stock-click-melee-cancel-rt",
                combat.StockAttackIntentCancelCount - stockCancelBefore == 1 &&
                    ruleProbe.PairAttackRuleCount == attackRulesBeforeCancel &&
                    combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore == 0,
                "One native ground command cancelled persistent stock intent and no late or duplicate pair attack completed.", evidence);
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
            if (!IsCombatReady(true) || !rangedWeaponLease.IsReady)
            {
                return;
            }

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            movementStart = horse.Position;
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
                FailCurrent("mounted-bow-approach-to-range-rt", "Ordinary native ranged hostile click was not admitted.");
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
                    rider.HasLOS(target),
                "The native ranged child retained and satisfied its exact line-of-sight requirement.", evidence);

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
                FailCurrent("mounted-bow-adjacent-rt", "Ordinary adjacent Shortbow hostile click did not create mounted ranged intent.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRangedAdjacentAttackRt;
            ResetLeafClock();
        }

        private void AwaitRangedAdjacentAttackRt()
        {
            if (combat.HasActiveCommand || combat.StockAttackRiderDispatchCount - stockRiderBefore < 1)
            {
                return;
            }

            var outcome = combat.LastOutcome;
            var evidence = CaptureStockEvidence();
            evidence["outcome"] = outcome == null
                ? null
                : JObject.FromObject(outcome, JsonSerializer.Create(JsonSettings));
            evidence["riderDistanceToTarget"] = rider.DistanceTo(target);
            evidence["opportunity"] = ruleProbe.CaptureOpportunityEvidence();
            AddRow(
                "mounted-bow-adjacent-rt",
                rider.DistanceTo(target) <= 2.5f && outcome != null && outcome.AttackWeaponIsRanged &&
                    outcome.NativeAttackRuleObserved && ruleProbe.RiderAttackRuleCount >= 1 &&
                    combat.StockAttackMountDispatchCount - stockMountBefore == 0,
                "An ordinary adjacent Shortbow click produced a rider-owned native ranged attack with zero automatic Horse melee.",
                evidence);
            AddRow(
                "mounted-ranged-aao-native-control",
                ruleProbe.OpportunityAttackRuleCount >= 1 && ruleProbe.OpportunityAttackRollCount >= 1 &&
                    ruleProbe.LastOpportunityTargetId == rider.UniqueId,
                "The adjacent ranged attack retained Kingmaker's native hostile AoO chain; KMC applied no ranged AoO suppression.",
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
            if (combat.HasStockAttackIntent || combat.HasActiveCommand)
            {
                return;
            }
            stableFrames++;
            if (stableFrames < 3)
            {
                return;
            }
            BeginRangedVariantRt(WeaponCategory.LightCrossbow);
        }

        private void BeginRangedVariantRt(WeaponCategory category)
        {
            rangedWeaponLease?.Dispose();
            rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
            rangedWeaponLease.Acquire(category);
            rangedVariantCategory = category;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            ruleProbe.Arm(target, true);
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            stockRiderBefore = combat.StockAttackRiderDispatchCount;
            stockMountBefore = combat.StockAttackMountDispatchCount;
            stockDuplicateBefore = combat.StockAttackDuplicateDispatchCount;
            targetService.BeginExpectedAttackDispatch(target);
            var clicked = new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            var row = category == WeaponCategory.LightCrossbow
                ? "mounted-crossbow-or-reload-control"
                : "mounted-sling-control";
            if (!clicked && combat.StockAttackIntentStartCount == stockIntentBefore)
            {
                FailCurrent(row, "Ordinary native ranged hostile click did not admit the " + category + " control.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitRangedVariantRt;
            ResetLeafClock();
        }

        private void AwaitRangedVariantRt()
        {
            if (combat.HasActiveCommand || combat.StockAttackRiderDispatchCount - stockRiderBefore < 1)
            {
                return;
            }

            var outcome = combat.LastOutcome;
            var weapon = rider.GetFirstWeapon()?.Blueprint;
            var evidence = CaptureStockEvidence();
            evidence["weaponCategory"] = weapon?.Category.ToString();
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
                    !string.IsNullOrWhiteSpace(outcome.AmmunitionStateBefore) &&
                    !string.IsNullOrWhiteSpace(outcome.AmmunitionStateAfter) &&
                    !string.IsNullOrWhiteSpace(outcome.ReloadStateBefore) &&
                    !string.IsNullOrWhiteSpace(outcome.ReloadStateAfter) &&
                    combat.StockAttackMountDispatchCount - stockMountBefore == 0 &&
                    combat.StockAttackDuplicateDispatchCount - stockDuplicateBefore == 0,
                "Ordinary mounted " + rangedVariantCategory + " fire stayed on native weapon/ammunition/reload surfaces with zero Horse melee dispatch.",
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
                BeginRangedVariantRt(WeaponCategory.Sling);
                return;
            }
            rangedWeaponLease.Dispose();
            rangedWeaponLease = null;
            BeginRtToTbTransition();
        }

        private void BeginRtToTbTransition()
        {
            combat.Cancel("Phase 3D RT-to-TB boundary");
            turnSnapshotBefore = combat.CaptureUnifiedTurnSnapshot();
            turnBasedModeProbe = new NativeModeTransitionProbe(true);
            turnBasedModeProbe.DispatchTemporaryValueIfRequired();
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

            var after = combat.CaptureUnifiedTurnSnapshot();
            InitiativeTrackerVM tracker = null;
            try
            {
                tracker = new InitiativeTrackerVM();
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

            controller.StartTurn(rider);
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
            nativeControls.Update();
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
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
            BeginUnmountedMeleeRt();
        }

        private void BeginUnmountedMeleeRt()
        {
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
            if (ruleProbe.RiderAttackRuleCount < 1)
            {
                return;
            }
            var evidence = new JObject
            {
                ["commandType"] = unmountedCommand?.GetType().FullName,
                ["nativeRequestDelta"] = combat.StockAttackNativeRequestCount - stockNativeBefore,
                ["intentStartDelta"] = combat.StockAttackIntentStartCount - stockIntentBefore,
                ["rules"] = ruleProbe.CapturePairEvidence(),
                ["relationshipState"] = relationship.State.ToString()
            };
            AddRow(
                "unmounted-stock-attack-control",
                relationship.State == RelationshipState.Unmounted && unmountedCommand?.GetType() == typeof(UnitAttack) &&
                    combat.StockAttackNativeRequestCount == stockNativeBefore &&
                    combat.StockAttackIntentStartCount == stockIntentBefore && ruleProbe.RiderAttackRuleCount >= 1,
                "Ordinary unmounted hostile click remained a stock rider UnitAttack and bypassed all mounted intent routing.",
                evidence);
            rider.Commands.InterruptAll(false);
            step = Phase3dHorseStep.AwaitUnmountedMeleeCancelRt;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitUnmountedMeleeCancelRt()
        {
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
            rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
            rangedWeaponLease.Acquire(WeaponCategory.Sling);
            ruleProbe.Arm(target, true);
            stockNativeBefore = combat.StockAttackNativeRequestCount;
            stockIntentBefore = combat.StockAttackIntentStartCount;
            targetService.BeginExpectedAttackDispatch(target);
            new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            unmountedCommand = rider.Commands.Standard;
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
            if (ruleProbe.RiderAttackRuleCount < 1)
            {
                return;
            }
            var weapon = rider.GetFirstWeapon()?.Blueprint;
            var evidence = new JObject
            {
                ["commandType"] = unmountedCommand?.GetType().FullName,
                ["weaponCategory"] = weapon?.Category.ToString(),
                ["nativeRequestDelta"] = combat.StockAttackNativeRequestCount - stockNativeBefore,
                ["intentStartDelta"] = combat.StockAttackIntentStartCount - stockIntentBefore,
                ["rules"] = ruleProbe.CapturePairEvidence(),
                ["relationshipState"] = relationship.State.ToString()
            };
            AddRow(
                "unmounted-ranged-control",
                relationship.State == RelationshipState.Unmounted && weapon != null && weapon.IsRanged &&
                    weapon.Category == WeaponCategory.Sling && unmountedCommand?.GetType() == typeof(UnitAttack) &&
                    combat.StockAttackNativeRequestCount == stockNativeBefore &&
                    combat.StockAttackIntentStartCount == stockIntentBefore && ruleProbe.RiderAttackRuleCount >= 1,
                "Ordinary unmounted Sling fire remained stock UnitAttack behavior and bypassed mounted intent routing.",
                evidence);
            rider.Commands.InterruptAll(false);
            BeginCleanup();
        }

        private void AwaitUnmountedCombat()
        {
            if (Game.Instance.IsPaused)
            {
                Game.Instance.IsPaused = false;
            }
            if (!IsCombatReady(false))
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
            if (!CombatController.IsInTurnBasedCombat() || controller == null || !controller.Initialized ||
                !controller.SortedUnits.Contains(rider) || !controller.SortedUnits.Contains(horse) ||
                !controller.SortedUnits.Contains(target))
            {
                return;
            }

            controller.StartTurn(rider);
            step = Phase3dHorseStep.AwaitRiderTurnForMount;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitRiderTurnForMount()
        {
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            if (turn?.Unit != rider ||
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

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            turnSnapshotBefore = combat.CaptureUnifiedTurnSnapshot();
            riderStandardBeforeMount = rider.CombatState.Cooldown.StandardAction;
            riderMoveBeforeMount = rider.CombatState.Cooldown.MoveAction;
            mountStandardBeforeMount = horse.CombatState.Cooldown.StandardAction;
            mountMoveBeforeMount = horse.CombatState.Cooldown.MoveAction;
            var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "tb-combat-mount");
            if (!clicked)
            {
                FailCurrent("mount-in-combat-before-either-acted", "Native Mount Companion click was not admitted during the exact rider turn.");
                BeginCleanup();
                return;
            }
            step = Phase3dHorseStep.AwaitCombatMount;
            stableFrames = 0;
            ResetLeafClock();
        }

        private void AwaitCombatMount()
        {
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
            AddRow("mount-in-combat-before-either-acted",
                turn?.Unit == rider && riderStandardBeforeMount < 0.001f && mountStandardBeforeMount < 0.001f &&
                    riderMoveSpent && mountLedgerPreserved &&
                    after.SharedInitiativeOwnerId == rider.UniqueId,
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
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
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

            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            outcomeBefore = combat.LastOutcome;
            ruleProbe.Arm(target, true);
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
            var evidence = CaptureOutcome(outcome, new NativeMountedAbilityActivationRecord[0]);
            AddRow(
                "mounted-stock-click-melee-mount-only-explicit",
                retained && outcome != null && outcome.Action == MountedCombatActionKind.MountPrimaryNatural &&
                    outcome.ResourceOwnerId == horse.UniqueId && ruleProbe.RiderAttackRuleCount == 0 &&
                    ruleProbe.MountAttackRuleCount == 1,
                "Explicit Mount Primary spent only the Horse attack ledger during the shared rider-led turn.",
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
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
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
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
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
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            if (turn?.Unit != rider ||
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
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
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
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            if (turn?.Unit != rider ||
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
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            if (relationship.State != RelationshipState.Unmounted || !IsStableRiderTurn(turn))
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
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
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
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            if (turn?.Unit != horse ||
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
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            if (relationship.State != RelationshipState.Unmounted || !IsStableRiderTurn(turn))
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
                (!requireMounted || relationship.State == RelationshipState.Mounted);
        }

        private bool IsStableRiderTurn(TurnController turn)
        {
            return turn?.Unit == rider &&
                (turn.Status == TurnController.TurnStatus.Preparing || turn.IsActing) &&
                rider.Commands.Empty && horse.Commands.Empty;
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

            var before = nativeControls.CaptureSnapshot();
            handler.SetAbility(data);
            var priority = handler.GetPriority(targetObject, position);
            var resolvedTarget = handler.GetTarget(targetObject, position, data);
            var clicked = handler.OnClick(targetObject, position, 0, false, false);
            var after = nativeControls.CaptureSnapshot();
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
                ["dispatchRejectedDelta"] = after.DispatchRejectedCount - before.DispatchRejectedCount
            };
            handler.DropAbility();
            return clicked;
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
                ["presentation"] = JObject.FromObject(
                    relationship.CapturePresentationObservation(), JsonSerializer.Create(JsonSettings)),
                ["rules"] = ruleProbe.CapturePairEvidence()
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
                ["feedback"] = combat.LastFeedback
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
            try { movementCommand?.Interrupt(); }
            catch (Exception exception) { AddCleanupError("movement", exception); }
            try { rangedWeaponLease?.Dispose(); }
            catch (Exception exception) { AddCleanupError("ranged weapon", exception); }
            rangedWeaponLease = null;
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
            settings.EnableUnsafeMovementExperiment = originalUnsafeExperiment;
        }

        private void AwaitCleanup()
        {
            try
            {
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
                settings.EnableUnsafeMovementExperiment == originalUnsafeExperiment &&
                relationship.State == RelationshipState.Unmounted &&
                (targetService == null || targetCleanupComplete) && modeRestored && !cleanupError;
            observations["cleanup"] = new JObject
            {
                ["selectionRestored"] = selectionRestored,
                ["equipmentSetRestored"] = rider.Body.CurrentHandEquipmentSetIndex == originalEquipmentSet,
                ["settingRestored"] = settings.EnableUnsafeMovementExperiment == originalUnsafeExperiment,
                ["relationshipState"] = relationship.State.ToString(),
                ["targetClean"] = targetCleanupComplete,
                ["modeRestored"] = modeRestored
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
            var path = Path.Combine(request.EvidenceRoot, EvidenceFileName);
            if (File.Exists(path))
            {
                throw new InvalidOperationException("Phase 3D Horse evidence artifact already exists.");
            }
            var artifact = new JObject
            {
                ["schemaVersion"] = 1,
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
            PresentationSettle,
            AwaitMountedCombat,
            AwaitRiderPrimaryRt,
            AwaitMountPrimaryRt,
            AwaitStockMeleeRt,
            AwaitStockCancelRt,
            AwaitRangedTargetCleanup,
            AwaitRangedCombat,
            AwaitRangedAttackRt,
            AwaitRangedCancelRt,
            AwaitRangedAdjacentMoveRt,
            AwaitRangedAdjacentAttackRt,
            AwaitRangedAdjacentCancelRt,
            AwaitRangedVariantRt,
            AwaitRangedVariantCancelRt,
            AwaitRtToTbTransition,
            AwaitRiderPrimaryAfterTransition,
            AwaitTbToRtTransition,
            AwaitRtCombatDismount,
            AwaitUnmountedMeleeRt,
            AwaitUnmountedMeleeCancelRt,
            AwaitUnmountedRangedRt,
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
        IGlobalRulebookHandler<RuleAttackRoll>,
        IGlobalRulebookHandler<RuleRollDice>,
        IGlobalRulebookHandler<RuleDealDamage>,
        IDisposable
    {
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly IDisposable subscription;
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
        internal int MountAttackRuleCount { get; private set; }
        internal int PairAttackRuleCount => RiderAttackRuleCount + MountAttackRuleCount;
        internal int PairAttackRollCount { get; private set; }
        internal int PairDamageRuleCount { get; private set; }
        internal int PairForcedD20Count { get; private set; }
        internal int PairDamage { get; private set; }
        internal int OpportunityAttackRuleCount { get; private set; }
        internal int OpportunityAttackRollCount { get; private set; }
        internal int OpportunityDamageRuleCount { get; private set; }
        internal string FirstPairAttackActorId { get; private set; }
        internal string LastPairAttackActorId { get; private set; }
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
            MountAttackRuleCount = 0;
            PairAttackRollCount = 0;
            PairDamageRuleCount = 0;
            PairForcedD20Count = 0;
            PairDamage = 0;
            FirstPairAttackActorId = null;
            LastPairAttackActorId = null;
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
                ["mountAttackRules"] = MountAttackRuleCount,
                ["pairAttackRolls"] = PairAttackRollCount,
                ["pairDamageRules"] = PairDamageRuleCount,
                ["pairForcedD20"] = PairForcedD20Count,
                ["pairDamage"] = PairDamage,
                ["firstPairActorId"] = FirstPairAttackActorId,
                ["lastPairActorId"] = LastPairAttackActorId
            };
        }

        internal JObject CaptureOpportunityEvidence()
        {
            return new JObject
            {
                ["attackRules"] = OpportunityAttackRuleCount,
                ["attackRolls"] = OpportunityAttackRollCount,
                ["damageRules"] = OpportunityDamageRuleCount,
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
            }
            else if (evt.Initiator == mount)
            {
                MountAttackRuleCount++;
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
