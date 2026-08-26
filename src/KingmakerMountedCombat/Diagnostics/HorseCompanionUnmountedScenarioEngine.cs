using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
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
    /// One bounded, save-backed technical qualification for the unmounted KMC
    /// horse. It deliberately leaves actual disk save/reload to the final human
    /// checkpoint because the guarded runtime authority forbids Kingmaker's
    /// crash-unsafe temporary-save leaf.
    /// </summary>
    internal sealed class HorseCompanionUnmountedScenarioEngine : IDisposable
    {
        internal const string ScenarioName = "horse-companion-unmounted-suite";
        internal const string MountedScenarioName = "horse-mounted-alpha-suite";
        internal const string EvidenceFileName = "horse-companion-unmounted.json";
        internal const string EvidenceKind = "horse-companion-unmounted";
        internal const string MountedEvidenceFileName = "horse-mounted-alpha.json";
        internal const string MountedEvidenceKind = "horse-mounted-alpha";

        private const double ScenarioTimeoutSeconds = 180.0;
        private const double MountedScenarioTimeoutSeconds = 300.0;
        private const double RealTimeAttackTimeoutSeconds = 20.0;
        private const double TurnBasedTurnAcquisitionTimeoutSeconds = 20.0;
        private const double TurnBasedAttackTimeoutSeconds = 20.0;
        private const float MovementDistance = 2.0f;
        private const float MovementTolerance = 0.75f;
        private const float TargetDistance = 4.0f;

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
        private readonly HorseCompanionBlueprintService service;
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPlayerActionController playerAction;
        private readonly MountedCombatController combat;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly Stopwatch clock = new Stopwatch();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly JArray assertions = new JArray();
        private readonly JObject observations = new JObject();
        private readonly Dictionary<string, string> originalPartyPets = new Dictionary<string, string>(StringComparer.Ordinal);

        private EngineStep step;
        private bool started;
        private bool completed;
        private bool disposed;
        private bool cleanupStarted;
        private bool selectionRestored;
        private int frame;
        private int cleanupFrame;
        private int passed;
        private int failed;
        private bool originalPause;
        private bool originalTurnBased;
        private bool originalUnsafeExperimentSetting;
        private UnitEntityData[] originalSelection = new UnitEntityData[0];
        private UnitEntityData owner;
        private UnitEntityData horse;
        private UnitEntityData target;
        private Feature rankFact;
        private Feature horseFeatureFact;
        private UnitMoveTo movementCommand;
        private UnitAttack realTimeAttack;
        private double realTimeAttackIssuedAtSeconds;
        private UnitAttack turnBasedAttack;
        private double turnBasedAttackIssuedAtSeconds;
        private JObject turnBasedAttackAtDispatch;
        private DiagnosticCombatTargetService targetService;
        private MountedCombatRuleProbe ruleProbe;
        private NativeModeTransitionProbe realTimeModeProbe;
        private NativeModeTransitionProbe turnBasedModeProbe;
        private bool turnStarted;
        private double turnBasedTurnAcquisitionStartedAtSeconds;
        private int turnBasedStartTurnRequestCount;
        private int turnBasedPostDispatchStartTurnRequestCount;
        private int turnBasedStableReadyFrames;
        private bool mountedAlphaStarted;
        private bool mountedAlphaDismounted;
        private UnitMoveTo mountedRealTimeMove;
        private Vector3 mountedRealTimeRiderStart;
        private Vector3 mountedRealTimeHorseStart;
        private Vector3 mountedRealTimeDestination;
        private int mountedRiderTurnStartRequests;
        private int mountedRiderTurnStableFrames;
        private int mountedPostMoveTurnReassertions;
        private Vector3 mountedTurnRiderStart;
        private Vector3 mountedTurnHorseStart;
        private Vector3 mountedTurnTargetStart;
        private Vector3 mountedTurnDestination;
        private MountedPairAttackOutcome mountedRiderOutcome;
        private MountedPairAttackOutcome mountedHorseOutcome;
        private Vector3 ownerPositionBeforeMovement;
        private Vector3 horsePositionBeforeMovement;
        private Vector3 movementDestination;
        private string horseId;
        private int lethalDamage;

        public HorseCompanionUnmountedScenarioEngine(
            RuntimeRequest request,
            HorseCompanionBlueprintService service,
            GameMountedRelationshipService relationship,
            MountedPlayerActionController playerAction,
            MountedCombatController combat,
            DiagnosticSettings settings,
            IModLogger logger)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.playerAction = playerAction ?? throw new ArgumentNullException(nameof(playerAction));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsCompleted => completed;

        public IReadOnlyList<RuntimeSubscenarioResult> Results => results;

        public IReadOnlyList<string> Errors => errors;

        internal static bool SupportsScenario(string scenario)
        {
            return string.Equals(scenario, ScenarioName, StringComparison.Ordinal) ||
                string.Equals(scenario, MountedScenarioName, StringComparison.Ordinal);
        }

        private bool IncludesMountedAlpha =>
            string.Equals(request.Scenario, MountedScenarioName, StringComparison.Ordinal);

        public void Start()
        {
            ThrowIfDisposed();
            if (started) { throw new InvalidOperationException("Horse unmounted scenario engine already started."); }
            if (!SupportsScenario(request.Scenario))
            {
                throw new InvalidOperationException("Scenario is outside the horse unmounted allowlist.");
            }

            started = true;
            clock.Start();
            logger.Info("Horse companion unmounted qualification started.");
            try
            {
                results.Add(HorseCompanionBlueprintRegistrationAuditService.Run(request, service, logger));
                if (!string.Equals(results[0].Status, "PASS", StringComparison.Ordinal))
                {
                    Fail("registration-prerequisite", "The corrected blueprint registration audit did not pass.");
                    BeginCleanup();
                    return;
                }

                var game = Game.Instance;
                var selection = SelectionManager.Instance;
                if (game?.Player == null || game.State?.Units == null || selection == null)
                {
                    throw new InvalidOperationException("Loaded Working game/player/selection services are unavailable.");
                }
                originalPause = game.IsPaused;
                originalTurnBased = CombatController.IsInTurnBasedCombat();
                originalUnsafeExperimentSetting = settings.EnableUnsafeMovementExperiment;
                originalSelection = selection.SelectedUnits.Where(unit => unit != null).ToArray();
                CaptureOriginalPartyPets(game);
                observations["originalPause"] = originalPause;
                observations["originalTurnBased"] = originalTurnBased;
                observations["originalSelectionCount"] = originalSelection.Length;
                observations["saveLoadAutomationScope"] =
                    "CONTRACT-ONLY: reciprocal native AddPet/SetMaster/UnitReference state is observed; actual disk save/reload is reserved for manual review because guarded automation forbids Kingmaker's crash-unsafe temporary save leaf.";

                realTimeModeProbe = new NativeModeTransitionProbe(false);
                realTimeModeProbe.DispatchTemporaryValueIfRequired();
                step = EngineStep.AwaitRealTimeMode;
            }
            catch (Exception exception)
            {
                Fail("scenario-start", exception.GetType().Name + ": " + exception.Message);
                BeginCleanup();
            }
        }

        public void Update()
        {
            ThrowIfDisposed();
            if (!started) { throw new InvalidOperationException("Horse unmounted engine must be started before Update."); }
            if (completed) { return; }

            frame++;
            try
            {
                var scenarioDeadline = IncludesMountedAlpha
                    ? MountedScenarioTimeoutSeconds
                    : ScenarioTimeoutSeconds;
                if (!cleanupStarted && clock.Elapsed.TotalSeconds > scenarioDeadline)
                {
                    Fail("bounded-deadline", "Horse qualification exceeded " +
                        scenarioDeadline.ToString("0") + " seconds at " + step + ".");
                    BeginCleanup();
                }

                switch (step)
                {
                    case EngineStep.AwaitRealTimeMode:
                        AwaitRealTimeAndCreate();
                        break;
                    case EngineStep.AwaitHorseSpawn:
                        AwaitHorseSpawn();
                        break;
                    case EngineStep.AwaitMovement:
                        AwaitMovement();
                        break;
                    case EngineStep.AwaitCombatEntry:
                        AwaitCombatEntry();
                        break;
                    case EngineStep.AwaitRealTimeAttack:
                        AwaitRealTimeAttack();
                        break;
                    case EngineStep.AwaitTurnBasedMode:
                        AwaitTurnBasedMode();
                        break;
                    case EngineStep.AwaitTurnBasedTurn:
                        AwaitTurnBasedTurn();
                        break;
                    case EngineStep.AwaitTurnBasedAttack:
                        AwaitTurnBasedAttack();
                        break;
                    case EngineStep.AwaitRealTimeRestore:
                        AwaitRealTimeRestore();
                        break;
                    case EngineStep.AwaitTargetRemoval:
                        AwaitTargetRemoval();
                        break;
                    case EngineStep.AwaitMountedReady:
                        AwaitMountedReady();
                        break;
                    case EngineStep.AwaitMountedRealTimeMovement:
                        AwaitMountedRealTimeMovement();
                        break;
                    case EngineStep.AwaitMountedCombatEntry:
                        AwaitMountedCombatEntry();
                        break;
                    case EngineStep.AwaitMountedTurnBasedMode:
                        AwaitMountedTurnBasedMode();
                        break;
                    case EngineStep.AwaitMountedRiderTurn:
                        AwaitMountedRiderTurn();
                        break;
                    case EngineStep.AwaitMountedTurnBasedMovement:
                        AwaitMountedTurnBasedMovement();
                        break;
                    case EngineStep.AwaitMountedRealTimeRestore:
                        AwaitMountedRealTimeRestore();
                        break;
                    case EngineStep.AwaitMountedRiderAttack:
                        AwaitMountedRiderAttack();
                        break;
                    case EngineStep.AwaitMountedHorseAttack:
                        AwaitMountedHorseAttack();
                        break;
                    case EngineStep.AwaitMountedDismount:
                        AwaitMountedDismount();
                        break;
                    case EngineStep.AwaitDeath:
                        AwaitDeath();
                        break;
                    case EngineStep.AwaitRecovery:
                        AwaitRecovery();
                        break;
                    case EngineStep.AwaitRespecRemoval:
                        AwaitRespecRemoval();
                        break;
                    case EngineStep.AwaitCleanup:
                        AwaitCleanup();
                        break;
                }
            }
            catch (Exception exception)
            {
                Fail("runtime-exception", exception.GetType().Name + ": " + exception.Message);
                BeginCleanup();
            }
        }

        public void Dispose()
        {
            if (disposed) { return; }
            if (!completed)
            {
                try { BestEffortCleanup(); }
                catch (Exception exception) { errors.Add("Dispose cleanup: " + exception.Message); }
            }
            try { targetService?.Dispose(); }
            catch (Exception exception) { errors.Add("Target-service disposal: " + exception.Message); }
            targetService = null;
            disposed = true;
        }

        private void AwaitRealTimeAndCreate()
        {
            if (CombatController.IsInTurnBasedCombat()) { return; }
            owner = FindEligibleOwner();
            Check(owner != null,
                "eligible-owner",
                "A conscious, idle, directly controllable Medium party member with no pet or companion-rank fact was selected.");
            if (owner == null) { BeginCleanup(); return; }

            observations["ownerId"] = owner.UniqueId;
            observations["ownerBlueprintGuid"] = owner.Blueprint?.AssetGuid;
            rankFact = owner.Descriptor.Progression.Features.AddFeature(service.LevelRank);
            if (rankFact != null) { rankFact.SetRankForce(4); }
            horseFeatureFact = owner.Descriptor.Progression.Features.AddFeature(service.HorseFeature);
            Check(rankFact != null && rankFact.GetRank() == 4 && horseFeatureFact != null,
                "feature-activation",
                "The exact stock AnimalCompanionRank reached rank 4 and the KMC Horse feature activated once.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitHorseSpawn;
        }

        private void AwaitHorseSpawn()
        {
            horse = owner?.Descriptor?.Pet;
            if (horse == null || !horse.IsInState || horse.View == null || horse.View.AgentASP == null) { return; }

            var companionAddPet = horseFeatureFact?.Get<HorseCompanionAddPet>();
            if (companionAddPet != null && companionAddPet.DeferredProgressionPending) { return; }

            horseId = horse.UniqueId;
            var player = Game.Instance.Player;
            var upgrade = horse.Descriptor.GetFact(service.HorseUpgrade);
            var characterLevel = horse.Descriptor.Progression.CharacterLevel;
            var experience = horse.Descriptor.Progression.Experience;
            var expectedCharacterLevel = companionAddPet?.ExpectedCharacterLevel ?? -1;
            var expectedExperience = companionAddPet?.ExpectedExperience ?? -1;
            var classProgressionSynchronized = companionAddPet?.NativeClassProgressionSynchronized ?? false;
            var manualLevelingReady = companionAddPet?.NativeManualLevelingReady ?? false;
            observations["horseId"] = horseId;
            observations["horseBlueprintGuid"] = horse.Blueprint?.AssetGuid;
            observations["characterLevel"] = characterLevel;
            observations["expectedCharacterLevel"] = expectedCharacterLevel;
            observations["experience"] = experience;
            observations["expectedExperience"] = expectedExperience;
            observations["rank"] = rankFact.GetRank();
            observations["upgradeRank"] = upgrade?.GetRank() ?? 0;
            observations["activationDefaultBuildContextPresent"] = companionAddPet?.ActivationDefaultBuildContextPresent ?? false;
            observations["activationCharacterLevelAfterNativeTry"] = companionAddPet?.ActivationCharacterLevelAfterNativeTry ?? -1;
            observations["activationExperienceAfterNativeTry"] = companionAddPet?.ActivationExperienceAfterNativeTry ?? -1;
            observations["deferredNativeAttempts"] = companionAddPet?.DeferredNativeAttempts ?? -1;
            observations["defaultBuildContextWaitFrames"] = companionAddPet?.DefaultBuildContextWaitFrames ?? -1;
            observations["lastDeferredDefaultBuildContextPresent"] = companionAddPet?.LastDeferredDefaultBuildContextPresent ?? false;
            observations["deferredCharacterLevelBefore"] = companionAddPet?.DeferredCharacterLevelBefore ?? -1;
            observations["deferredCharacterLevelAfter"] = companionAddPet?.DeferredCharacterLevelAfter ?? -1;
            observations["deferredExperienceBefore"] = companionAddPet?.DeferredExperienceBefore ?? -1;
            observations["deferredExperienceAfter"] = companionAddPet?.DeferredExperienceAfter ?? -1;
            observations["nativeClassProgressionSynchronized"] = classProgressionSynchronized;
            observations["nativeManualLevelingReady"] = manualLevelingReady;
            observations["nativeProgressionDisposition"] = classProgressionSynchronized
                ? "class-level-synchronized"
                : manualLevelingReady ? "native-manual-leveling-ready" : "incomplete";
            observations["deferredProgressionSynchronized"] = companionAddPet != null &&
                                                               !companionAddPet.DeferredProgressionPending &&
                                                               !companionAddPet.DeferredProgressionFailed &&
                                                               companionAddPet.NativeProgressionReady;
            observations["runtimeSize"] = horse.Descriptor.State.Size.ToString();
            observations["speedFeet"] = horse.Blueprint.Speed.Value;
            observations["hitPoints"] = (int)horse.Stats.HitPoints;
            observations["armorClass"] = (int)horse.Stats.AC;

            Check(string.Equals(horse.Blueprint?.AssetGuid, HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal) &&
                    horse.Descriptor.IsPet && horse.Descriptor.Master.Value == owner && owner.Descriptor.Pet == horse,
                "creation-and-ownership",
                "Native AddPet spawned the exact KMC horse and established reciprocal SetMaster ownership.");
            Check(horse.IsDirectlyControllable && horse.IsInGame &&
                    player.ControllableCharacters.Contains(horse) && !player.PartyCharacters.Any(item => item.Value == horse),
                "party-control-surface",
                "The horse is directly controllable through the native pet surface without becoming a duplicate party character.");
            Check(companionAddPet != null && expectedCharacterLevel == 4 && expectedExperience >= 0 &&
                    companionAddPet.NativeProgressionReady &&
                    (classProgressionSynchronized ||
                     (manualLevelingReady && experience == expectedExperience)) &&
                    rankFact.GetRank() == 4 &&
                    !companionAddPet.DeferredProgressionPending && !companionAddPet.DeferredProgressionFailed &&
                    companionAddPet.DeferredNativeAttempts <= HorseCompanionProgressionPolicy.MaximumDeferredNativeAttempts &&
                    upgrade != null && upgrade.GetRank() == 1,
                "rank-progression-and-upgrade",
                "Stock AddPet mapped rank 4 to committed animal-companion level 4 or the exact native manual-leveling XP threshold, and applied the KMC rank-4 upgrade once without a duplicate progression update.");
            Check(horse.Descriptor.State.Size == Size.Large && horse.Blueprint.Speed.Value == 50 &&
                    string.Equals(horse.Blueprint.Prefab?.AssetId, "5e0b93738ad54dd4ba101b3513ac4590", StringComparison.Ordinal) &&
                    (int)horse.Stats.HitPoints > 0 && (int)horse.Stats.AC > 0,
                "native-view-size-statistics",
                "The live companion is Large, speed 50, uses HorseRiding, and has positive stock-derived HP/AC.");

            var selection = SelectionManager.Instance;
            selection.SelectUnit(horse.View, true, true, false);
            Check(selection.SelectedUnits.Count == 1 && selection.SelectedUnits[0] == horse,
                "horse-selection",
                "The native selection manager selected exactly the unmounted horse.");
            if (failed != 0) { BeginCleanup(); return; }

            ownerPositionBeforeMovement = owner.Position;
            horsePositionBeforeMovement = horse.Position;
            movementDestination = FindWalkablePoint(horse.Position, MovementDistance, 0.45f);
            movementCommand = new UnitMoveTo(movementDestination, 0.1f) { CreatedByPlayer = true };
            horse.Commands.Run(movementCommand);
            Check(movementCommand.Executor == horse && ReferenceEquals(horse.Commands.Move, movementCommand),
                "stock-movement-command",
                "One player-created stock UnitMoveTo is owned by the horse.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitMovement;
        }

        private void AwaitMovement()
        {
            if (movementCommand == null || !movementCommand.IsFinished) { return; }
            var displacement = HorizontalDistance(horsePositionBeforeMovement, horse.Position);
            var remaining = HorizontalDistance(horse.Position, movementDestination);
            var ownerDisplacement = HorizontalDistance(ownerPositionBeforeMovement, owner.Position);
            observations["movementDisplacement"] = displacement;
            observations["movementRemainingDistance"] = remaining;
            observations["ownerDisplacementDuringHorseMove"] = ownerDisplacement;
            Check(displacement >= 1.0f && remaining <= MovementTolerance && ownerDisplacement <= 0.2f &&
                    movementCommand.Executor == horse,
                "unmounted-party-movement",
                "The horse completed its own stock path while the owner retained position and no command ownership was duplicated.");
            if (failed != 0) { BeginCleanup(); return; }

            targetService = new DiagnosticCombatTargetService(logger);
            // DiagnosticCombatTargetService validates its placement against the
            // rider/owner authority. The horse has already completed an
            // independent movement leg, so deriving this point from the horse
            // can put it inside the service's three-unit owner-relative floor.
            var spawnPoint = FindWalkablePoint(owner.Position, TargetDistance, 0.45f);
            observations["targetOwnerDistance"] = HorizontalDistance(owner.Position, spawnPoint);
            observations["targetHorseDistance"] = HorizontalDistance(horse.Position, spawnPoint);
            target = targetService.Spawn(owner, horse, spawnPoint, request.RunId, true, true);
            Check(targetService.PrepareForPlayerClick(target) && targetService.QueueBidirectionalCombatMemory(owner, target),
                "transient-combat-target",
                "A runtime-only hostile target entered the exact visibility, memory, and cleanup lease.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitCombatEntry;
        }

        private void AwaitCombatEntry()
        {
            if (Game.Instance.IsPaused) { Game.Instance.IsPaused = false; }
            if (!targetService.RefreshBidirectionalCombatMemoryLease() ||
                !owner.IsInCombat || !horse.IsInCombat || !target.IsInCombat ||
                horse.CombatState == null || !horse.CombatState.Prepared || !horse.CombatState.CanActInCombat ||
                !horse.CanAttack(target) || !target.Descriptor.State.IsConscious)
            {
                return;
            }

            var fullAttackProbe = new UnitAttack(target) { ForceFullAttack = true, CreatedByPlayer = true };
            fullAttackProbe.IgnoreCooldown();
            fullAttackProbe.Init(horse);
            var attackGuids = fullAttackProbe.AllAttacks
                .Select(item => item.Weapon?.Blueprint?.AssetGuid)
                .ToArray();
            var snapshot = service.CaptureSnapshot();
            observations["fullAttackWeaponGuids"] = new JArray(attackGuids);
            Check(attackGuids.Length == 3 &&
                    string.Equals(attackGuids[0], snapshot.BiteGuid, StringComparison.Ordinal) &&
                    attackGuids.Skip(1).All(value => string.Equals(value, HorseCompanionBlueprintService.HoofGuid, StringComparison.Ordinal)),
                "bite-and-hoof-full-attack",
                "Stock UnitAttack enumerated one Bite primary followed by exactly two Hoof limbs.");
            if (failed != 0) { BeginCleanup(); return; }

            var preDispatchStandard = horse.Commands.Standard;
            observations["realTimePreDispatchStandardType"] = preDispatchStandard?.GetType().FullName;
            observations["realTimePreDispatchStandardRunning"] = preDispatchStandard?.IsRunning;
            observations["realTimePreDispatchStandardAiActionPresent"] = preDispatchStandard?.AiAction != null;
            observations["realTimePreDispatchStandardTargetExact"] = preDispatchStandard != null &&
                ReferenceEquals(preDispatchStandard.Target?.Unit, target);

            // Native real-time auto-combat can already own a same-target UnitAttack
            // by the time the bidirectional combat-memory lease is ready. UnitAttack
            // then merges a newly submitted same-target command into that active
            // command, intentionally leaving the submitted object outside the
            // Standard slot. This scenario needs one exact player-created single
            // Bite, so clear only this temporary horse's command surface at the
            // explicit dispatch boundary before arming the rule probe.
            horse.Commands.InterruptAll(false);
            ruleProbe = new MountedCombatRuleProbe();
            ruleProbe.Arm(owner, horse, horse, target, 20);
            var expectedDispatchStarted = targetService.BeginExpectedAttackDispatch(target);
            realTimeAttack = new UnitAttack(target) { IsSingleAttack = true, CreatedByPlayer = true };
            realTimeAttack.IgnoreCooldown();
            horse.Commands.Run(realTimeAttack);
            Check(expectedDispatchStarted && ReferenceEquals(horse.Commands.Standard, realTimeAttack),
                "expected-attack-boundary",
                "After target safety and combat entry passed, one exact player-created stock Bite owned the horse's native Standard slot.");
            if (failed != 0) { BeginCleanup(); return; }
            realTimeAttackIssuedAtSeconds = clock.Elapsed.TotalSeconds;
            observations["realTimeAttackAtDispatch"] = CaptureRealTimeAttackState(realTimeAttack);
            step = EngineStep.AwaitRealTimeAttack;
        }

        private void AwaitRealTimeAttack()
        {
            targetService.RefreshBidirectionalCombatMemoryLease();
            if (realTimeAttack == null)
            {
                Fail("real-time-attack-command", "The stock real-time Bite command reference became null.");
                BeginCleanup();
                return;
            }
            if (!realTimeAttack.IsFinished)
            {
                if (clock.Elapsed.TotalSeconds - realTimeAttackIssuedAtSeconds <= RealTimeAttackTimeoutSeconds)
                {
                    return;
                }

                var diagnostic = CaptureRealTimeAttackState(realTimeAttack);
                observations["realTimeAttackAtDeadline"] = diagnostic;
                Fail(
                    "real-time-attack-deadline",
                    "The stock real-time Bite command did not finish within its bounded 20-second leaf. State=" +
                    diagnostic.ToString(Formatting.None) + ".");
                BeginCleanup();
                return;
            }
            var weaponGuid = realTimeAttack.LastExecutedAttack?.Weapon?.Blueprint?.AssetGuid;
            observations["realTimeAttackWeaponGuid"] = weaponGuid;
            observations["realTimeAttackRules"] = ruleProbe.AttackRuleCount;
            observations["realTimeAttackRolls"] = ruleProbe.AttackRollCount;
            observations["realTimeDamageRules"] = ruleProbe.DamageRuleCount;
            observations["realTimeForcedD20Count"] = ruleProbe.ForcedD20Count;
            observations["realTimeUnexpectedPairAttackCount"] = ruleProbe.UnexpectedPairAttackCount;
            observations["realTimeDamage"] = ruleProbe.TotalDamage;
            Check(ruleProbe.AttackRuleCount == 1 && ruleProbe.AttackRollCount == 1 &&
                    ruleProbe.DamageRuleCount == 1 && ruleProbe.ForcedD20Count >= 1 &&
                    ruleProbe.UnexpectedPairAttackCount == 0 && ruleProbe.TotalDamage > 0 &&
                    string.Equals(weaponGuid, service.CaptureSnapshot().BiteGuid, StringComparison.Ordinal),
                "real-time-natural-attack",
                "One forced-hit stock real-time Bite produced one attack, roll, and damage chain with no owner duplicate.");
            if (failed != 0) { BeginCleanup(); return; }

            turnBasedModeProbe = new NativeModeTransitionProbe(true);
            turnBasedModeProbe.DispatchTemporaryValueIfRequired();
            step = EngineStep.AwaitTurnBasedMode;
        }

        private JObject CaptureRealTimeAttackState(UnitAttack command)
        {
            var game = Game.Instance;
            var planned = command?.PlannedAttack;
            var animation = command?.Animation;
            var agent = horse?.View?.AgentASP;
            var path = agent?.Path;
            var state = horse?.Descriptor?.State;
            var combatState = horse?.CombatState;
            var handsEquipment = game?.HandsEquipmentController;
            var horizontalDistance = horse == null || target == null
                ? float.MaxValue
                : HorizontalDistance(horse.Position, target.Position);
            var directDistance = horse == null || target == null
                ? float.MaxValue
                : Vector3.Distance(horse.Position, target.Position);

            return new JObject
            {
                ["scenarioSeconds"] = clock.Elapsed.TotalSeconds,
                ["secondsSinceDispatch"] = clock.Elapsed.TotalSeconds - realTimeAttackIssuedAtSeconds,
                ["gamePaused"] = game?.IsPaused,
                ["gameMode"] = game?.CurrentMode.ToString(),
                ["gameDeltaTime"] = game?.TimeController?.GameDeltaTime,
                ["commandReferenceInStandardSlot"] = horse?.Commands != null &&
                    ReferenceEquals(horse.Commands.Standard, command),
                ["commandContained"] = horse?.Commands != null && horse.Commands.Contains(command),
                ["commandResult"] = command?.Result.ToString(),
                ["commandStarted"] = command?.IsStarted,
                ["commandFinished"] = command?.IsFinished,
                ["commandRunning"] = command?.IsRunning,
                ["commandActed"] = command?.IsActed,
                ["commandCanStart"] = command?.CanStart,
                ["commandTimeSinceStart"] = command?.TimeSinceStart,
                ["commandAttackIndex"] = command?.GetAttackIndex(),
                ["commandApproachRadius"] = command?.ApproachRadius,
                ["commandMaximumApproachRadius"] = command?.MaxApproachRadius,
                ["commandEnoughClose"] = command?.IsUnitEnoughClose,
                ["commandShouldApproach"] = command?.ShouldUnitApproach,
                ["commandMovedUnit"] = command?.IsMoveUnit,
                ["commandFinishedApproaching"] = command?.FinishedApproaching,
                ["commandNeedsLineOfSight"] = command?.NeedLoS,
                ["plannedWeaponGuid"] = planned?.Weapon?.Blueprint?.AssetGuid,
                ["plannedWeaponRange"] = planned?.WeaponRange,
                ["animationPresent"] = animation != null,
                ["animationActed"] = animation?.IsActed,
                ["animationFinished"] = animation?.IsFinished,
                ["horseTargetHorizontalDistance"] = horizontalDistance,
                ["horseTargetDirectDistance"] = directDistance,
                ["distanceBeyondApproachRadius"] = command == null
                    ? float.MaxValue
                    : horizontalDistance - command.ApproachRadius,
                ["horseCorpulence"] = horse?.View?.Corpulence,
                ["targetCorpulence"] = target?.View?.Corpulence,
                ["horseCanAct"] = state?.CanAct,
                ["horseHandsBusy"] = horse?.AreHandsBusyWithAnimation,
                ["horseEquipmentUpdateScheduled"] = horse != null && handsEquipment != null &&
                    handsEquipment.IsUpdateScheduledFor(horse),
                ["horseCombatPrepared"] = combatState?.Prepared,
                ["horseCombatCanAct"] = combatState?.CanActInCombat,
                ["horseStandardCooldown"] = combatState?.Cooldown.StandardAction,
                ["agentEnabled"] = agent?.enabled,
                ["agentWantsToMove"] = agent?.WantsToMove,
                ["agentReallyMoving"] = agent?.IsReallyMoving,
                ["agentSpeed"] = agent?.Speed,
                ["agentVelocitySquared"] = agent?.Velocity.sqrMagnitude,
                ["agentPathFailed"] = agent?.PathFailed,
                ["agentPathPresent"] = path != null,
                ["agentPathPointCount"] = path?.vectorPath?.Count,
                ["moveSlotPresent"] = horse?.Commands?.Move != null,
                ["targetConscious"] = target?.Descriptor?.State.IsConscious,
                ["targetVisible"] = target?.IsVisibleForPlayer
            };
        }

        private void AwaitTurnBasedMode()
        {
            targetService.RefreshBidirectionalCombatMemoryLease();
            var controller = Game.Instance.TurnBasedCombatController;
            if (!CombatController.IsInTurnBasedCombat() || controller == null || !controller.Initialized ||
                !ContainsTurnRosterUnit(controller, horse) || !ContainsTurnRosterUnit(controller, target))
            {
                return;
            }
            Check(ContainsTurnRosterUnit(controller, owner) && relationship.State == RelationshipState.Unmounted,
                "turn-based-roster",
                "Owner, horse, and target entered the native turn roster while KMC remained unmounted.");
            if (failed != 0) { BeginCleanup(); return; }
            controller.StartTurn(horse);
            turnStarted = true;
            turnBasedTurnAcquisitionStartedAtSeconds = clock.Elapsed.TotalSeconds;
            turnBasedStartTurnRequestCount = 1;
            turnBasedStableReadyFrames = 0;
            step = EngineStep.AwaitTurnBasedTurn;
        }

        private void AwaitTurnBasedTurn()
        {
            targetService.RefreshBidirectionalCombatMemoryLease();
            var game = Game.Instance;
            var controller = Game.Instance.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            if (!turnStarted || controller == null)
            {
                return;
            }

            if (clock.Elapsed.TotalSeconds - turnBasedTurnAcquisitionStartedAtSeconds >
                TurnBasedTurnAcquisitionTimeoutSeconds)
            {
                observations["turnBasedAttackAtDispatch"] = CaptureTurnBasedAttackState(null);
                Fail("turn-based-turn-deadline",
                    "The native controller did not retain a stable actionable horse turn within its bounded 20-second leaf.");
                BeginCleanup();
                return;
            }

            if (turn?.Unit != horse)
            {
                turnBasedStableReadyFrames = 0;
                if (turnBasedStartTurnRequestCount < 2)
                {
                    // CombatController can still own a queued next-unit handoff
                    // when turn-based mode first initializes. That handoff may
                    // replace an otherwise healthy diagnostic StartTurn request
                    // on the following native tick. Reassert the exact horse turn
                    // once after observing that replacement, then require it to
                    // remain ready across two subsequent engine frames.
                    controller.StartTurn(horse);
                    turnBasedStartTurnRequestCount++;
                }
                return;
            }

            if (
                (turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) ||
                horse.CombatState == null || !horse.CombatState.Prepared || !horse.CombatState.CanActInCombat ||
                game.IsPaused || horse.Commands == null || !horse.Commands.Empty ||
                horse.AreHandsBusyWithAnimation || game.HandsEquipmentController == null ||
                game.HandsEquipmentController.IsUpdateScheduledFor(horse) || !horse.HasStandardAction() ||
                target == null || !target.IsInState || !target.Descriptor.State.IsConscious ||
                !horse.CanAttack(target))
            {
                turnBasedStableReadyFrames = 0;
                return;
            }

            turnBasedStableReadyFrames++;
            if (turnBasedStableReadyFrames < 2)
            {
                return;
            }

            var selection = SelectionManager.Instance;
            if (selection.SelectedUnits.Count != 1 || selection.SelectedUnits[0] != horse)
            {
                selection.SelectUnit(horse.View, true, true, false);
                return;
            }
            Check(selection.SelectedUnits.Count == 1 && selection.SelectedUnits[0] == horse,
                "turn-based-horse-control",
                "The horse owned its native turn, exact selection, idle hands, empty command surface, and available Standard action.");
            if (failed != 0) { BeginCleanup(); return; }

            ruleProbe.Arm(owner, horse, horse, target, 20);
            turnBasedAttack = new UnitAttack(target) { IsSingleAttack = true, CreatedByPlayer = true };
            horse.Commands.Run(turnBasedAttack);
            turnBasedAttackIssuedAtSeconds = clock.Elapsed.TotalSeconds;
            turnBasedAttackAtDispatch = CaptureTurnBasedAttackState(turnBasedAttack);
            Check(ReferenceEquals(controller.CurrentTurn, turn) &&
                    ReferenceEquals(horse.Commands.Standard, turnBasedAttack),
                "turn-based-command-admission",
                "The stable exact horse turn retained one player-created single Bite in its native Standard slot without command merging or replacement.");
            if (failed != 0)
            {
                observations["turnBasedAttackAtDispatch"] = turnBasedAttackAtDispatch;
                BeginCleanup();
                return;
            }
            step = EngineStep.AwaitTurnBasedAttack;
        }

        private void AwaitTurnBasedAttack()
        {
            targetService.RefreshBidirectionalCombatMemoryLease();
            if (turnBasedAttack == null)
            {
                Fail("turn-based-attack-command", "The stock turn-based Bite command reference became null.");
                BeginCleanup();
                return;
            }
            if (!turnBasedAttack.IsFinished)
            {
                var controller = Game.Instance?.TurnBasedCombatController;
                var exactHealthyPendingCommand = !turnBasedAttack.IsStarted &&
                    turnBasedAttack.Result == UnitCommand.ResultType.None &&
                    turnBasedAttack.CanStart &&
                    horse?.Commands != null &&
                    ReferenceEquals(horse.Commands.Standard, turnBasedAttack);
                if (controller != null && controller.CurrentTurn?.Unit != horse &&
                    exactHealthyPendingCommand && turnBasedPostDispatchStartTurnRequestCount == 0)
                {
                    // The native next-unit handoff queued during turn-mode entry can
                    // land immediately after the exact command is admitted. Preserve
                    // that command reference and reassert only its owning horse turn
                    // once; do not recreate, replace, or relax the command.
                    controller.StartTurn(horse);
                    turnBasedPostDispatchStartTurnRequestCount++;
                    observations["turnBasedPostDispatchStartTurnRequestCount"] =
                        turnBasedPostDispatchStartTurnRequestCount;
                }
                if (clock.Elapsed.TotalSeconds - turnBasedAttackIssuedAtSeconds <= TurnBasedAttackTimeoutSeconds)
                {
                    return;
                }

                observations["turnBasedAttackAtDispatch"] = turnBasedAttackAtDispatch;
                observations["turnBasedAttackAtDeadline"] = CaptureTurnBasedAttackState(turnBasedAttack);
                Fail("turn-based-attack-deadline",
                    "The stock turn-based Bite command did not finish within its bounded 20-second leaf.");
                BeginCleanup();
                return;
            }
            var terminal = CaptureTurnBasedAttackState(turnBasedAttack);
            var weaponGuid = turnBasedAttack.LastExecutedAttack?.Weapon?.Blueprint?.AssetGuid;
            observations["turnBasedAttackWeaponGuid"] = weaponGuid;
            observations["turnBasedAttackRules"] = ruleProbe.AttackRuleCount;
            observations["turnBasedAttackRolls"] = ruleProbe.AttackRollCount;
            observations["turnBasedDamageRules"] = ruleProbe.DamageRuleCount;
            observations["turnBasedForcedD20Count"] = ruleProbe.ForcedD20Count;
            observations["turnBasedUnexpectedPairAttackCount"] = ruleProbe.UnexpectedPairAttackCount;
            observations["turnBasedDamage"] = ruleProbe.TotalDamage;
            observations["turnBasedPostDispatchStartTurnRequestCount"] =
                turnBasedPostDispatchStartTurnRequestCount;
            var exactOutcome = turnBasedAttack.Result == UnitCommand.ResultType.Success &&
                turnBasedAttack.IsStarted && turnBasedAttack.IsActed &&
                ruleProbe.AttackRuleCount == 1 && ruleProbe.AttackRollCount == 1 &&
                    ruleProbe.DamageRuleCount == 1 && ruleProbe.ForcedD20Count >= 1 &&
                    ruleProbe.UnexpectedPairAttackCount == 0 && ruleProbe.TotalDamage > 0 &&
                    string.Equals(weaponGuid, service.CaptureSnapshot().BiteGuid, StringComparison.Ordinal);
            if (!exactOutcome)
            {
                observations["turnBasedAttackAtDispatch"] = turnBasedAttackAtDispatch;
                observations["turnBasedAttackAtTerminal"] = terminal;
            }
            Check(exactOutcome,
                "turn-based-natural-attack",
                "One admitted stock turn-based Bite finished Success and produced one attack, roll, and damage chain on the horse's own Standard action.");
            if (failed != 0) { BeginCleanup(); return; }

            turnBasedModeProbe.Dispose();
            turnBasedModeProbe = null;
            step = EngineStep.AwaitRealTimeRestore;
        }

        private JObject CaptureTurnBasedAttackState(UnitAttack command)
        {
            var game = Game.Instance;
            var controller = game?.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            var animation = command?.Animation;
            var combatState = horse?.CombatState;
            var selected = SelectionManager.Instance?.SelectedUnits;
            return new JObject
            {
                ["scenarioSeconds"] = clock.Elapsed.TotalSeconds,
                ["secondsSinceDispatch"] = clock.Elapsed.TotalSeconds - turnBasedAttackIssuedAtSeconds,
                ["gamePaused"] = game?.IsPaused,
                ["currentTurnUnitId"] = turn?.Unit?.UniqueId,
                ["currentTurnStatus"] = turn?.Status.ToString(),
                ["currentTurnIsActing"] = turn?.IsActing,
                ["horseSelectedExact"] = selected != null && selected.Count == 1 && selected[0] == horse,
                ["commandReferenceInStandardSlot"] = command != null && ReferenceEquals(horse?.Commands?.Standard, command),
                ["commandResult"] = command?.Result.ToString(),
                ["commandStarted"] = command?.IsStarted,
                ["commandFinished"] = command?.IsFinished,
                ["commandRunning"] = command?.IsRunning,
                ["commandActed"] = command?.IsActed,
                ["commandCanStart"] = command?.CanStart,
                ["commandTimeSinceStart"] = command?.TimeSinceStart,
                ["commandAttackIndex"] = command?.GetAttackIndex(),
                ["commandEnoughClose"] = command?.IsUnitEnoughClose,
                ["commandShouldApproach"] = command?.ShouldUnitApproach,
                ["plannedWeaponGuid"] = command?.PlannedAttack?.Weapon?.Blueprint?.AssetGuid,
                ["animationPresent"] = animation != null,
                ["animationActed"] = animation?.IsActed,
                ["animationFinished"] = animation?.IsFinished,
                ["horseCommandsEmpty"] = horse?.Commands?.Empty,
                ["horsePrepared"] = combatState?.Prepared,
                ["horseCanActInCombat"] = combatState?.CanActInCombat,
                ["horseHasStandardAction"] = horse != null && horse.HasStandardAction(),
                ["horseStandardCooldown"] = combatState?.Cooldown.StandardAction,
                ["horseHandsBusy"] = horse?.AreHandsBusyWithAnimation,
                ["horseEquipmentUpdateScheduled"] = horse != null && game?.HandsEquipmentController != null &&
                    game.HandsEquipmentController.IsUpdateScheduledFor(horse),
                ["horseTargetHorizontalDistance"] = horse == null || target == null
                    ? float.MaxValue
                    : HorizontalDistance(horse.Position, target.Position),
                ["targetInState"] = target?.IsInState,
                ["targetConscious"] = target?.Descriptor?.State.IsConscious,
                ["targetDamage"] = target?.Damage
            };
        }

        private void AwaitRealTimeRestore()
        {
            if (CombatController.IsInTurnBasedCombat()) { return; }
            TryLeaveCombat(target);
            TryLeaveCombat(horse);
            TryLeaveCombat(owner);
            if (targetService != null && !targetService.DestroyAndVerify())
            {
                step = EngineStep.AwaitTargetRemoval;
                return;
            }
            ContinueAfterTargetRemoval();
        }

        private void AwaitTargetRemoval()
        {
            if (targetService != null && !targetService.DestroyAndVerify()) { return; }
            ContinueAfterTargetRemoval();
        }

        private void ContinueAfterTargetRemoval()
        {
            var targetClean = targetService == null ||
                (targetService.TargetEntityRemoved && targetService.RuntimeGroupRemoved &&
                 targetService.RuntimeFactionRemoved && targetService.CombatMemoryRemoved &&
                 targetService.TargetDurabilityLeaseReleased && targetService.TargetBrainLeaseReleased &&
                 targetService.TargetSleeplessLeaseReleased && targetService.NonPairPartyAiLeaseRestored);
            if (IncludesMountedAlpha)
            {
                observations[mountedAlphaStarted ? "mountedTargetCleanupExact" : "unmountedTargetCleanupExact"] = targetClean;
            }
            try { targetService?.Dispose(); }
            catch (Exception exception) { errors.Add("Completed target disposal: " + exception.Message); }
            targetService = null;
            target = null;

            if (IncludesMountedAlpha && !mountedAlphaStarted)
            {
                BeginMountedAlpha();
                return;
            }
            BeginDeathProbe();
        }

        private void BeginMountedAlpha()
        {
            mountedAlphaStarted = true;
            settings.EnableUnsafeMovementExperiment = true;
            var selection = SelectionManager.Instance;
            selection.SelectUnit(owner.View, true, true, false);
            var armCountBefore = playerAction.MountTargetArmCount;
            var clickCountBefore = playerAction.MountTargetClickCount;
            var armed = playerAction.ArmMountTarget();
            var click = armed
                ? playerAction.TryHandleMountTargetClick(horse.View.gameObject, 0, false)
                : MountedCombatClickResult.NotHandled;
            observations["mountTargetArmDelta"] = playerAction.MountTargetArmCount - armCountBefore;
            observations["mountTargetClickDelta"] = playerAction.MountTargetClickCount - clickCountBefore;
            observations["mountTargetFeedback"] = playerAction.LastFeedback;
            Check(armed && click == MountedCombatClickResult.HandledAccepted &&
                    relationship.State == RelationshipState.Mounted &&
                    relationship.Rider == owner && relationship.Mount == horse,
                "target-selected-mount-action",
                "The rider armed Mount, clicked the exact KMC horse, and created one exact transient pair through the player-facing target path.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitMountedReady;
        }

        private void AwaitMountedReady()
        {
            var runtime = relationship.Runtime;
            if (relationship.State != RelationshipState.Mounted ||
                runtime.PoseApplicationFrameCount == 0 || !runtime.PoseFrameApplied)
            {
                return;
            }

            var selected = SelectionManager.Instance.SelectedUnits;
            observations["horseProfileId"] = runtime.MountProfileId;
            observations["horsePoseProfileId"] = runtime.PoseProfileId;
            observations["horseSourceAnchor"] = runtime.PresentationSourceAnchorName;
            observations["horsePresentationAtMount"] = relationship.CapturePresentationObservation();
            Check(string.Equals(runtime.MountProfileId, SupportedMountedProfiles.Horse.Id, StringComparison.Ordinal) &&
                    string.Equals(runtime.PoseProfileId, SupportedMountedProfiles.Horse.Id, StringComparison.Ordinal) &&
                    string.Equals(runtime.PresentationSourceAnchorName, "Chest", StringComparison.Ordinal) &&
                    runtime.PresentationAttachmentLeaseActive && runtime.RiderParentMatchesAttachment &&
                    runtime.PoseHealthy && runtime.PoseFrameApplied &&
                    owner.View != null && !owner.View.AgentASP.enabled &&
                    horse.View != null && horse.View.AgentASP.enabled &&
                    selected.Count == 1 && selected[0] == owner,
                "independent-horse-mounted-profile",
                "The exact horse profile used Chest, its independent rider pose, Mammoth-style sole pathfinding, and rider-owned selection without Mammoth offsets.");
            if (failed != 0) { BeginCleanup(); return; }

            mountedRealTimeRiderStart = owner.Position;
            mountedRealTimeHorseStart = horse.Position;
            mountedRealTimeDestination = FindWalkablePoint(horse.Position, MovementDistance, 0.45f);
            ClickGroundHandler.MoveSelectedUnitsToPoint(mountedRealTimeDestination, false);
            mountedRealTimeMove = horse.Commands?.Move as UnitMoveTo;
            Check(mountedRealTimeMove != null && mountedRealTimeMove.Executor == horse &&
                    mountedRealTimeMove.CreatedByPlayer && owner.Commands?.Move == null,
                "mounted-real-time-command-routing",
                "One ordinary rider-owned ground input created one exact player command in the horse Move slot and none on the rider.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitMountedRealTimeMovement;
        }

        private void AwaitMountedRealTimeMovement()
        {
            if (mountedRealTimeMove == null || !mountedRealTimeMove.IsFinished) { return; }
            var riderDistance = HorizontalDistance(mountedRealTimeRiderStart, owner.Position);
            var horseDistance = HorizontalDistance(mountedRealTimeHorseStart, horse.Position);
            var remaining = HorizontalDistance(horse.Position, mountedRealTimeDestination);
            observations["mountedRealTimeRiderDisplacement"] = riderDistance;
            observations["mountedRealTimeHorseDisplacement"] = horseDistance;
            observations["mountedRealTimeRemaining"] = remaining;
            Check(mountedRealTimeMove.Result == UnitCommand.ResultType.Success &&
                    riderDistance >= 1.0f && horseDistance >= 1.0f &&
                    remaining <= MovementTolerance &&
                    relationship.State == RelationshipState.Mounted &&
                    relationship.Runtime.PoseHealthy && relationship.Runtime.PoseFrameApplied,
                "mounted-real-time-movement",
                "The KMC horse remained sole pathfinder while the attached rider and horse reached the real-time destination with the exact pair and pose retained.");
            if (failed != 0) { BeginCleanup(); return; }

            targetService = new DiagnosticCombatTargetService(logger);
            var spawnPoint = FindWalkablePoint(owner.Position, TargetDistance, 0.45f);
            target = targetService.Spawn(owner, horse, spawnPoint, request.RunId + "-mounted", true, true);
            Check(targetService.PrepareForPlayerClick(target) &&
                    targetService.QueueBidirectionalCombatMemory(owner, target),
                "mounted-transient-combat-target",
                "A second runtime-only hostile target entered the exact visibility, memory, durability, and cleanup lease for mounted qualification.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitMountedCombatEntry;
        }

        private void AwaitMountedCombatEntry()
        {
            if (Game.Instance.IsPaused) { Game.Instance.IsPaused = false; }
            if (!targetService.RefreshBidirectionalCombatMemoryLease() ||
                !owner.IsInCombat || !horse.IsInCombat || !target.IsInCombat ||
                !owner.CombatState.Prepared || !horse.CombatState.Prepared ||
                !owner.CombatState.CanActInCombat || !horse.CombatState.CanActInCombat ||
                !target.Descriptor.State.IsConscious)
            {
                return;
            }

            turnBasedModeProbe = new NativeModeTransitionProbe(true);
            turnBasedModeProbe.DispatchTemporaryValueIfRequired();
            step = EngineStep.AwaitMountedTurnBasedMode;
        }

        private void AwaitMountedTurnBasedMode()
        {
            targetService.RefreshBidirectionalCombatMemoryLease();
            var controller = Game.Instance.TurnBasedCombatController;
            if (!CombatController.IsInTurnBasedCombat() || controller == null || !controller.Initialized ||
                !ContainsTurnRosterUnit(controller, owner) || !ContainsTurnRosterUnit(controller, horse) ||
                !ContainsTurnRosterUnit(controller, target))
            {
                return;
            }
            Check(relationship.State == RelationshipState.Mounted &&
                    relationship.Rider == owner && relationship.Mount == horse,
                "horse-pair-retained-in-turn-based-transition",
                "The exact horse pair and independent horse profile survived the native real-time-to-turn-based transition.");
            if (failed != 0) { BeginCleanup(); return; }
            controller.StartTurn(owner);
            mountedRiderTurnStartRequests = 1;
            mountedRiderTurnStableFrames = 0;
            turnBasedTurnAcquisitionStartedAtSeconds = clock.Elapsed.TotalSeconds;
            step = EngineStep.AwaitMountedRiderTurn;
        }

        private void AwaitMountedRiderTurn()
        {
            targetService.RefreshBidirectionalCombatMemoryLease();
            var controller = Game.Instance.TurnBasedCombatController;
            var turn = controller?.CurrentTurn;
            if (clock.Elapsed.TotalSeconds - turnBasedTurnAcquisitionStartedAtSeconds >
                TurnBasedTurnAcquisitionTimeoutSeconds)
            {
                Fail("mounted-rider-turn-deadline",
                    "The native controller did not retain a stable actionable rider turn within its bounded 20-second leaf.");
                BeginCleanup();
                return;
            }
            if (turn?.Unit != owner)
            {
                mountedRiderTurnStableFrames = 0;
                if (controller != null && mountedRiderTurnStartRequests < 2)
                {
                    controller.StartTurn(owner);
                    mountedRiderTurnStartRequests++;
                }
                return;
            }
            if ((turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) ||
                owner.Commands == null || !owner.Commands.Empty ||
                horse.Commands == null || !horse.Commands.Empty ||
                owner.AreHandsBusyWithAnimation || horse.AreHandsBusyWithAnimation)
            {
                mountedRiderTurnStableFrames = 0;
                return;
            }
            mountedRiderTurnStableFrames++;
            if (mountedRiderTurnStableFrames < 2) { return; }

            SelectionManager.Instance.SelectUnit(owner.View, true, true, false);
            mountedTurnRiderStart = owner.Position;
            mountedTurnHorseStart = horse.Position;
            mountedTurnTargetStart = target.Position;
            mountedTurnDestination = FindWalkablePoint(horse.Position, 1.5f, 0.45f);
            ClickGroundHandler.MoveSelectedUnitsToPoint(mountedTurnDestination, false);
            Check(combat.HasActiveGroundMovement,
                "mounted-rider-turn-ground-admission",
                "One ordinary ground click on the rider turn admitted one exact horse-owned Move command through the bounded turn adapter.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitMountedTurnBasedMovement;
        }

        private void AwaitMountedTurnBasedMovement()
        {
            targetService.RefreshBidirectionalCombatMemoryLease();
            var controller = Game.Instance.TurnBasedCombatController;
            if (combat.HasActiveGroundMovement)
            {
                if (controller?.CurrentTurn?.Unit != owner && mountedPostMoveTurnReassertions == 0)
                {
                    controller?.StartTurn(owner);
                    mountedPostMoveTurnReassertions++;
                }
                return;
            }
            if (string.IsNullOrEmpty(combat.LastGroundMoveResult)) { return; }

            var riderDistance = HorizontalDistance(mountedTurnRiderStart, owner.Position);
            var horseDistance = HorizontalDistance(mountedTurnHorseStart, horse.Position);
            var targetDistance = HorizontalDistance(mountedTurnTargetStart, target.Position);
            observations["mountedTurnRiderDisplacement"] = riderDistance;
            observations["mountedTurnHorseDisplacement"] = horseDistance;
            observations["mountedTurnTargetDisplacement"] = targetDistance;
            observations["mountedTurnDriveCount"] = combat.LastGroundMoveDriveCount;
            observations["mountedTurnPostDispatchReassertions"] = mountedPostMoveTurnReassertions;
            Check(string.Equals(combat.LastGroundMoveResult, "Success", StringComparison.Ordinal) &&
                    combat.LastGroundMoveDriveCount > 0 && combat.LastGroundMoveUsedRiderTurnAdapter &&
                    combat.LastGroundMoveSlotRestored &&
                    string.Equals(combat.LastGroundMoveExecutorId, horse.UniqueId, StringComparison.Ordinal) &&
                    combat.LastGroundMoveRiderMoveAfter > combat.LastGroundMoveRiderMoveBefore &&
                    Math.Abs(combat.LastGroundMoveMountMoveAfter - combat.LastGroundMoveMountMoveBefore) <= 0.01f &&
                    riderDistance >= 0.75f && horseDistance >= 0.75f && targetDistance <= 0.25f &&
                    relationship.State == RelationshipState.Mounted && relationship.Runtime.PoseHealthy,
                "mounted-turn-based-rider-movement",
                "The rider turn charged only the rider Move ledger while the exact horse command moved the retained pair and left the target stationary.");
            if (failed != 0) { BeginCleanup(); return; }
            turnBasedModeProbe.Dispose();
            turnBasedModeProbe = null;
            step = EngineStep.AwaitMountedRealTimeRestore;
        }

        private void AwaitMountedRealTimeRestore()
        {
            if (CombatController.IsInTurnBasedCombat()) { return; }
            targetService.RefreshBidirectionalCombatMemoryLease();
            if (relationship.State != RelationshipState.Mounted ||
                relationship.Rider != owner || relationship.Mount != horse ||
                !relationship.Runtime.PoseHealthy || !relationship.Runtime.PoseFrameApplied ||
                owner.CombatState == null || horse.CombatState == null ||
                !owner.CombatState.Prepared || !horse.CombatState.Prepared ||
                !owner.HasStandardAction() || !horse.HasStandardAction() ||
                !owner.Commands.Empty || !horse.Commands.Empty ||
                owner.AreHandsBusyWithAnimation || horse.AreHandsBusyWithAnimation)
            {
                return;
            }
            observations["horsePresentationAfterTurnBasedRestore"] = relationship.CapturePresentationObservation();
            SelectionManager.Instance.SelectUnit(owner.View, true, true, false);
            ruleProbe.Arm(owner, horse, owner, target, 20);
            var dispatchStarted = targetService.BeginExpectedAttackDispatch(target);
            var armed = combat.Arm(MountedCombatActionKind.RiderMelee);
            var clicked = armed && new ClickUnitHandler().OnClick(
                target.View.gameObject, target.Position, 0, false, false);
            Check(dispatchStarted && armed && clicked && combat.HasActiveCommand,
                "mounted-rider-primary-admission",
                "The KMC Rider melee control admitted exactly one rider-owned mounted command after the native turn-based-to-real-time transition.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitMountedRiderAttack;
        }

        private void AwaitMountedRiderAttack()
        {
            targetService.RefreshBidirectionalCombatMemoryLease();
            if (combat.HasActiveCommand || combat.LastOutcome == null) { return; }
            mountedRiderOutcome = combat.LastOutcome;
            observations["mountedRiderOutcome"] = CaptureMountedOutcome(mountedRiderOutcome);
            observations["mountedRiderAttackRules"] = ruleProbe.AttackRuleCount;
            observations["mountedRiderAttackRolls"] = ruleProbe.AttackRollCount;
            observations["mountedRiderDamageRules"] = ruleProbe.DamageRuleCount;
            Check(mountedRiderOutcome.Action == MountedCombatActionKind.RiderMelee &&
                    string.Equals(mountedRiderOutcome.Result, UnitCommand.ResultType.Success.ToString(), StringComparison.Ordinal) &&
                    string.Equals(mountedRiderOutcome.ActorId, owner.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(mountedRiderOutcome.CommandOwnerId, owner.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(mountedRiderOutcome.ResourceOwnerId, owner.UniqueId, StringComparison.Ordinal) &&
                    mountedRiderOutcome.ChildAttackStartCount == 1 &&
                    ruleProbe.AttackRuleCount == 1 && ruleProbe.AttackRollCount == 1 &&
                    ruleProbe.DamageRuleCount == 1 && ruleProbe.UnexpectedPairAttackCount == 0 &&
                    ruleProbe.TotalDamage > 0,
                "mounted-rider-primary-outcome",
                "Exactly one supported rider melee chain used rider command/resource ownership with no duplicate pair attack.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitMountedHorseAttack;
        }

        private void AwaitMountedHorseAttack()
        {
            targetService.RefreshBidirectionalCombatMemoryLease();
            if (mountedHorseOutcome == null && combat.LastOutcome == mountedRiderOutcome)
            {
                if (!horse.HasStandardAction() || !horse.Commands.Empty || horse.AreHandsBusyWithAnimation ||
                    target == null || !target.IsInState || !target.Descriptor.State.IsConscious)
                {
                    return;
                }
                SelectionManager.Instance.SelectUnit(owner.View, true, true, false);
                ruleProbe.Arm(owner, horse, horse, target, 20);
                var armed = combat.Arm(MountedCombatActionKind.MountPrimaryNatural);
                var clicked = armed && new ClickUnitHandler().OnClick(
                    target.View.gameObject, target.Position, 0, false, false);
                Check(armed && clicked && combat.HasActiveCommand,
                    "mounted-horse-primary-admission",
                    "The KMC Horse primary control admitted exactly one horse-owned natural-attack command without replacing the rider command surface.");
                if (failed != 0) { BeginCleanup(); }
                return;
            }
            if (combat.HasActiveCommand || combat.LastOutcome == null) { return; }

            mountedHorseOutcome = combat.LastOutcome;
            observations["mountedHorseOutcome"] = CaptureMountedOutcome(mountedHorseOutcome);
            observations["mountedHorseAttackRules"] = ruleProbe.AttackRuleCount;
            observations["mountedHorseAttackRolls"] = ruleProbe.AttackRollCount;
            observations["mountedHorseDamageRules"] = ruleProbe.DamageRuleCount;
            Check(mountedHorseOutcome.Action == MountedCombatActionKind.MountPrimaryNatural &&
                    string.Equals(mountedHorseOutcome.Result, UnitCommand.ResultType.Success.ToString(), StringComparison.Ordinal) &&
                    string.Equals(mountedHorseOutcome.ActorId, horse.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(mountedHorseOutcome.CommandOwnerId, horse.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(mountedHorseOutcome.ResourceOwnerId, horse.UniqueId, StringComparison.Ordinal) &&
                    mountedHorseOutcome.ChildAttackStartCount == 1 && mountedHorseOutcome.AttackWeaponIsNatural &&
                    ruleProbe.AttackRuleCount == 1 && ruleProbe.AttackRollCount == 1 &&
                    ruleProbe.DamageRuleCount == 1 && ruleProbe.UnexpectedPairAttackCount == 0 &&
                    ruleProbe.TotalDamage > 0,
                "mounted-horse-primary-outcome",
                "Exactly one Horse primary chain used horse command/resource ownership and a natural primary with no duplicate rider attack.");
            if (failed != 0) { BeginCleanup(); return; }

            var dismounted = playerAction.Activate();
            mountedAlphaDismounted = dismounted;
            Check(dismounted,
                "mounted-explicit-dismount-dispatch",
                "The player-facing Dismount action accepted after both exact mounted attacks.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitMountedDismount;
        }

        private void AwaitMountedDismount()
        {
            if (!mountedAlphaDismounted || relationship.State != RelationshipState.Unmounted) { return; }
            var selected = SelectionManager.Instance.SelectedUnits;
            Check(relationship.Rider == null && relationship.Mount == null &&
                    !relationship.Runtime.PresentationAttachmentLeaseActive &&
                    !relationship.Runtime.HasPresentationAttachmentResidue &&
                    relationship.Runtime.PoseComponentCount == 0 &&
                    relationship.Runtime.PoseBaselineRestoreVerified &&
                    owner.View.AgentASP.enabled && horse.View.AgentASP.enabled &&
                    selected.Count == 1 && selected[0] == owner,
                "mounted-explicit-dismount-restoration",
                "Explicit Dismount restored both stock agents, rider selection, pose baseline, attachment parent, and zero KMC presentation residue.");
            if (failed != 0) { BeginCleanup(); return; }
            TryLeaveCombat(target);
            TryLeaveCombat(horse);
            TryLeaveCombat(owner);
            if (targetService != null && !targetService.DestroyAndVerify())
            {
                step = EngineStep.AwaitTargetRemoval;
                return;
            }
            ContinueAfterTargetRemoval();
        }

        private void BeginDeathProbe()
        {
            observations["targetCleanupExact"] = targetService == null ||
                (targetService.TargetEntityRemoved && targetService.RuntimeGroupRemoved &&
                 targetService.RuntimeFactionRemoved && targetService.CombatMemoryRemoved);
            lethalDamage = (int)horse.Stats.HitPoints + (int)horse.Stats.Constitution + 1;
            horse.Damage = lethalDamage;
            observations["lethalDamage"] = lethalDamage;
            step = EngineStep.AwaitDeath;
        }

        private static JObject CaptureMountedOutcome(MountedPairAttackOutcome outcome)
        {
            if (outcome == null) { return null; }
            return new JObject
            {
                ["action"] = outcome.Action.ToString(),
                ["actorId"] = outcome.ActorId,
                ["commandOwnerId"] = outcome.CommandOwnerId,
                ["resourceOwnerId"] = outcome.ResourceOwnerId,
                ["targetId"] = outcome.TargetId,
                ["result"] = outcome.Result,
                ["childAttackStartCount"] = outcome.ChildAttackStartCount,
                ["repathCount"] = outcome.RepathCount,
                ["attackWeaponBlueprintId"] = outcome.AttackWeaponBlueprintId,
                ["attackWeaponIsNatural"] = outcome.AttackWeaponIsNatural,
                ["attackWeaponIsRanged"] = outcome.AttackWeaponIsRanged,
                ["delegatedMoveExecutorId"] = outcome.DelegatedMoveExecutorId,
                ["delegatedMoveExecutorIsExactMount"] = outcome.DelegatedMoveExecutorIsExactMount,
                ["riderStandardCharged"] = outcome.RiderStandardCharged,
                ["actionStandardCharged"] = outcome.ActionStandardCharged,
                ["terminalReason"] = outcome.TerminalReason
            };
        }

        private void AwaitDeath()
        {
            if (horse.Descriptor.State.IsConscious) { return; }
            Check(owner.Descriptor.Pet == horse && horse.Descriptor.Master.Value == owner,
                "death-ownership",
                "Native incapacitation retained the exact companion ownership relation for recovery.");
            horse.Descriptor.ResurrectAndFullRestore();
            step = EngineStep.AwaitRecovery;
        }

        private void AwaitRecovery()
        {
            if (!horse.Descriptor.State.IsConscious || horse.Damage != 0) { return; }
            observations["recoveredDamage"] = horse.Damage;
            Check(horse.IsInState && horse.View != null && horse.View.gameObject.activeInHierarchy &&
                    owner.Descriptor.Pet == horse && horse.Descriptor.Master.Value == owner,
                "death-and-recovery",
                "The horse recovered visibly with zero damage and the same reciprocal owner relation.");
            if (failed != 0) { BeginCleanup(); return; }

            owner.Descriptor.Progression.Features.RemoveFact(horseFeatureFact);
            horseFeatureFact = null;
            Game.Instance.EntityDestroyer.Tick();
            step = EngineStep.AwaitRespecRemoval;
        }

        private void AwaitRespecRemoval()
        {
            var stateContainsHorse = Game.Instance.State.Units.Contains(horse);
            var controllableContainsHorse = Game.Instance.Player.ControllableCharacters.Contains(horse);
            if (horse.IsInState || stateContainsHorse || controllableContainsHorse || owner.Descriptor.Pet != null) { return; }

            Check(horse.Descriptor.Master.Value == null && !horse.IsInState &&
                    !stateContainsHorse && !controllableContainsHorse && owner.Descriptor.Pet == null,
                "respec-runtime-cleanup",
                "Removing the KMC Horse feature cleared reciprocal ownership and destroyed only the exact spawned horse.");
            owner.Descriptor.Progression.Features.RemoveFact(rankFact);
            rankFact = null;
            var disabled = service.SetSelectionEnabled(false);
            var disabledSnapshot = service.CaptureSnapshot();
            var reenabled = service.SetSelectionEnabled(true);
            var reenabledSnapshot = service.CaptureSnapshot();
            Check(disabled && reenabled && disabledSnapshot.RangerCurrentOptionCount == 7 &&
                    reenabledSnapshot.RangerCurrentOptionCount == 8 && reenabledSnapshot.RangerAppendOwned,
                "respec-and-uninstall-surface",
                "After exact horse removal, the Ranger append restored to seven stock options and re-enabled once without residue.");
            BeginCleanup();
        }

        private void BeginCleanup()
        {
            if (cleanupStarted) { return; }
            cleanupStarted = true;
            BestEffortCleanup();
            cleanupFrame = frame;
            step = EngineStep.AwaitCleanup;
        }

        private void BestEffortCleanup()
        {
            try { combat.Cancel("horse qualification cleanup"); }
            catch (Exception exception) { errors.Add("Mounted combat cleanup: " + exception.Message); }
            if (relationship.State != RelationshipState.Unmounted)
            {
                try
                {
                    var cleanup = relationship.Dismount(CleanupTrigger.Exception);
                    if (!cleanup.Succeeded || cleanup.MovementAuthorityResidual || cleanup.PresentationResidual)
                    {
                        errors.Add("Mounted relationship cleanup retained runtime residue.");
                    }
                }
                catch (Exception exception) { errors.Add("Mounted relationship cleanup: " + exception.Message); }
            }
            try { realTimeAttack?.Interrupt(); } catch (Exception exception) { errors.Add("RT attack cleanup: " + exception.Message); }
            try { turnBasedAttack?.Interrupt(); } catch (Exception exception) { errors.Add("TB attack cleanup: " + exception.Message); }
            try { movementCommand?.Interrupt(); } catch (Exception exception) { errors.Add("Movement cleanup: " + exception.Message); }
            try { mountedRealTimeMove?.Interrupt(); } catch (Exception exception) { errors.Add("Mounted RT movement cleanup: " + exception.Message); }
            TryLeaveCombat(target);
            TryLeaveCombat(horse);
            TryLeaveCombat(owner);
            try { targetService?.DestroyAndVerify(); } catch (Exception exception) { errors.Add("Target cleanup: " + exception.Message); }
            try { ruleProbe?.Dispose(); } catch (Exception exception) { errors.Add("Rule-probe cleanup: " + exception.Message); }
            ruleProbe = null;

            if (owner != null && horseFeatureFact != null)
            {
                try { owner.Descriptor.Progression.Features.RemoveFact(horseFeatureFact); horseFeatureFact = null; }
                catch (Exception exception) { errors.Add("Horse feature cleanup: " + exception.Message); }
            }
            if (horse != null && horse.Descriptor?.Master.Value == null && horse.IsInState)
            {
                horse.Destroy();
            }
            if (owner != null && rankFact != null)
            {
                try { owner.Descriptor.Progression.Features.RemoveFact(rankFact); rankFact = null; }
                catch (Exception exception) { errors.Add("Rank feature cleanup: " + exception.Message); }
            }
            try { Game.Instance?.EntityDestroyer?.Tick(); } catch (Exception exception) { errors.Add("Entity cleanup tick: " + exception.Message); }

            try { turnBasedModeProbe?.Dispose(); } catch (Exception exception) { errors.Add("TB mode cleanup: " + exception.Message); }
            turnBasedModeProbe = null;
            try { realTimeModeProbe?.Dispose(); } catch (Exception exception) { errors.Add("RT mode cleanup: " + exception.Message); }
            realTimeModeProbe = null;
            settings.EnableUnsafeMovementExperiment = originalUnsafeExperimentSetting;
            try { service.SetSelectionEnabled(true); } catch (Exception exception) { errors.Add("Ranger selection cleanup: " + exception.Message); }
        }

        private void AwaitCleanup()
        {
            try { targetService?.DestroyAndVerify(); } catch (Exception exception) { AddCleanupError("target", exception); }
            try { Game.Instance?.EntityDestroyer?.Tick(); } catch (Exception exception) { AddCleanupError("entity tick", exception); }
            if (frame <= cleanupFrame) { return; }

            var targetClean = targetService == null ||
                (targetService.TargetEntityRemoved && targetService.RuntimeGroupRemoved &&
                 targetService.RuntimeFactionRemoved && targetService.CombatMemoryRemoved &&
                 targetService.TargetDurabilityLeaseReleased && targetService.TargetBrainLeaseReleased &&
                 targetService.TargetSleeplessLeaseReleased && targetService.NonPairPartyAiLeaseRestored);
            var horseClean = owner == null ||
                (owner.Descriptor.Pet == null && (horse == null ||
                    (!horse.IsInState && !Game.Instance.State.Units.Contains(horse) &&
                     !Game.Instance.Player.ControllableCharacters.Contains(horse))));
            var modeRestored = CombatController.IsInTurnBasedCombat() == originalTurnBased;
            if (!targetClean || !horseClean || !modeRestored) { return; }

            if (!selectionRestored)
            {
                RestoreSelection();
                selectionRestored = true;
                return;
            }

            Game.Instance.IsPaused = originalPause;
            var selection = SelectionManager.Instance.SelectedUnits;
            var expectedSelection = originalSelection.Where(unit => unit != null && unit.IsInState).ToArray();
            var selectionExact = selection.Count == expectedSelection.Length &&
                expectedSelection.All(unit => selection.Contains(unit));
            var unrelatedPetsExact = OriginalPartyPetsMatch();
            var selectionSnapshot = service.CaptureSnapshot();
            try
            {
                targetService?.Dispose();
                targetService = null;
            }
            catch (Exception exception)
            {
                Fail("target-subscription-cleanup", exception.GetType().Name + ": " + exception.Message);
            }
            Check(targetClean && horseClean,
                "entity-and-target-restoration",
                "Horse, hostile target, combat memory, runtime group/faction, and diagnostic leases were removed exactly.");
            Check(modeRestored && Game.Instance.IsPaused == originalPause && selectionExact,
                "mode-pause-selection-restoration",
                "Native mode, pause state, and the original exact selection were restored.");
            Check(unrelatedPetsExact && relationship.State == RelationshipState.Unmounted &&
                    selectionSnapshot.RangerCurrentOptionCount == 8 && selectionSnapshot.RangerAppendOwned,
                "non-horse-isolation",
                "All unrelated party pet identities, the unmounted Mammoth subsystem, and the exact Ranger append remained unchanged.");

            observations["finalPause"] = Game.Instance.IsPaused;
            observations["finalTurnBased"] = CombatController.IsInTurnBasedCombat();
            observations["finalSelectionCount"] = selection.Count;
            observations["unrelatedPartyPetsPreserved"] = unrelatedPetsExact;
            observations["relationshipState"] = relationship.State.ToString();
            observations["horseRemoved"] = horseClean;
            observations["targetRemoved"] = targetClean;
            Complete();
        }

        private UnitEntityData FindEligibleOwner()
        {
            var player = Game.Instance.Player;
            return player.PartyCharacters
                .Select(reference => reference.Value)
                .Where(candidate => candidate != null && candidate.IsInState && candidate.IsInGame &&
                    candidate.IsDirectlyControllable && !candidate.Descriptor.IsPet &&
                    candidate.Descriptor.Pet == null && candidate.Descriptor.State.IsConscious &&
                    candidate.Descriptor.State.Size == Size.Medium && !candidate.IsInCombat &&
                    candidate.Commands != null && candidate.Commands.Empty &&
                    candidate.Descriptor.GetFact(service.LevelRank) == null &&
                    (!IncludesMountedAlpha || IsSupportedHorseRider(candidate)))
                .OrderBy(candidate => candidate.UniqueId, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static bool IsSupportedHorseRider(UnitEntityData candidate)
        {
            if (candidate?.View == null || candidate.GetActivePolymorph() != null)
            {
                return false;
            }
            var weapon = candidate.GetFirstWeapon()?.Blueprint;
            if (weapon == null || weapon.IsRanged || weapon.IsTwoHanded || weapon.IsNatural)
            {
                return false;
            }
            string error;
            return MountedRiderPoseAdapter.TryValidateSupportedSurface(
                candidate.View,
                SupportedMountedProfiles.Horse.RiderPoseProfile,
                out error);
        }

        private void CaptureOriginalPartyPets(Game game)
        {
            originalPartyPets.Clear();
            foreach (var reference in game.Player.PartyCharacters)
            {
                var unit = reference.Value;
                if (unit != null)
                {
                    originalPartyPets[unit.UniqueId] = unit.Descriptor.Pet?.UniqueId;
                }
            }
        }

        private bool OriginalPartyPetsMatch()
        {
            foreach (var reference in Game.Instance.Player.PartyCharacters)
            {
                var unit = reference.Value;
                string expected;
                if (unit != null && originalPartyPets.TryGetValue(unit.UniqueId, out expected) && unit != owner &&
                    !string.Equals(expected, unit.Descriptor.Pet?.UniqueId, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        private void RestoreSelection()
        {
            var manager = SelectionManager.Instance;
            if (manager == null) { return; }
            manager.MultiSelect(
                originalSelection.Where(unit => unit != null && unit.IsInState && unit.View != null)
                    .Select(unit => unit.View),
                false);
        }

        private Vector3 FindWalkablePoint(Vector3 origin, float requestedDistance, float distanceTolerance)
        {
            if (global::AstarPath.active == null)
            {
                throw new InvalidOperationException("Active native navigation graph is unavailable.");
            }
            var baseDirection = horse?.View == null ? Vector3.forward : horse.View.transform.forward;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude < 0.01f) { baseDirection = Vector3.forward; }
            baseDirection.Normalize();
            for (var index = 0; index < 16; index++)
            {
                var direction = Quaternion.Euler(0f, index * 22.5f, 0f) * baseDirection;
                var nearest = global::AstarPath.active.GetNearest(origin + direction * requestedDistance);
                if (nearest.node == null || !nearest.node.Walkable) { continue; }
                var point = nearest.clampedPosition;
                var distance = HorizontalDistance(origin, point);
                if (distance > 0.25f && Math.Abs(distance - requestedDistance) <= distanceTolerance)
                {
                    return point;
                }
            }
            throw new InvalidOperationException("No bounded walkable horse scenario point satisfied the distance contract.");
        }

        private static bool ContainsTurnRosterUnit(CombatController controller, UnitEntityData expected)
        {
            return controller != null && expected != null && controller.SortedUnits.Any(unit => unit == expected);
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            var dx = first.x - second.x;
            var dz = first.z - second.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static void TryLeaveCombat(UnitEntityData unit)
        {
            if (unit != null && unit.IsInState && unit.IsInCombat) { unit.LeaveCombat(); }
        }

        private bool Check(bool condition, string name, string detail)
        {
            assertions.Add(new JObject
            {
                ["name"] = name,
                ["status"] = condition ? "PASS" : "FAIL",
                ["detail"] = detail
            });
            if (condition) { passed++; }
            else { failed++; errors.Add(name + ": " + detail); }
            return condition;
        }

        private void Fail(string name, string detail)
        {
            Check(false, name, detail);
        }

        private void AddCleanupError(string scope, Exception exception)
        {
            var message = scope + " cleanup: " + exception.GetType().Name + ": " + exception.Message;
            if (!errors.Contains(message)) { errors.Add(message); }
        }

        private void Complete()
        {
            if (completed) { return; }
            if (errors.Count > failed)
            {
                foreach (var error in errors.Skip(failed).ToArray())
                {
                    Fail("cleanup-error-" + failed, error);
                }
            }
            var status = failed == 0 ? "PASS" : "FAIL";
            var artifact = new JObject
            {
                ["schemaVersion"] = 2,
                ["evidenceKind"] = IncludesMountedAlpha ? MountedEvidenceKind : EvidenceKind,
                ["runId"] = request.RunId,
                ["scenario"] = request.Scenario,
                ["branch"] = request.Branch,
                ["commit"] = request.Commit,
                ["productVersion"] = request.ProductVersion,
                ["dllSha256"] = ComputeSha256(typeof(Main).Assembly.Location),
                ["dllMvid"] = typeof(Main).Assembly.ManifestModule.ModuleVersionId.ToString(),
                ["createdAtUtc"] = DateTimeOffset.UtcNow.ToString("o"),
                ["status"] = status,
                ["assertions"] = assertions,
                ["observations"] = observations,
                ["assertionPassCount"] = passed,
                ["assertionFailCount"] = failed,
                ["errors"] = new JArray(errors)
            };
            WriteArtifact(artifact);
            results.Add(new RuntimeSubscenarioResult
            {
                Name = request.Scenario,
                Status = status,
                AssertionPassCount = passed,
                AssertionFailCount = failed,
                Errors = errors.ToArray()
            });
            completed = true;
            logger.Info("Horse qualification completed: scenario=" + request.Scenario +
                "; PASS=" + passed + " FAIL=" + failed + ".");
        }

        private void WriteArtifact(JObject artifact)
        {
            var root = Path.GetFullPath(request.EvidenceRoot).TrimEnd(Path.DirectorySeparatorChar);
            var path = Path.Combine(root, IncludesMountedAlpha ? MountedEvidenceFileName : EvidenceFileName);
            if (!Directory.Exists(root)) { throw new DirectoryNotFoundException("Runtime evidence root is missing."); }
            if (File.Exists(path)) { throw new InvalidOperationException("Horse unmounted evidence artifact already exists."); }
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonConvert.SerializeObject(artifact, JsonSettings), new UTF8Encoding(false));
                File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) { File.Delete(temporary); }
            }
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
            if (disposed) { throw new ObjectDisposedException(nameof(HorseCompanionUnmountedScenarioEngine)); }
        }

        private enum EngineStep
        {
            AwaitRealTimeMode,
            AwaitHorseSpawn,
            AwaitMovement,
            AwaitCombatEntry,
            AwaitRealTimeAttack,
            AwaitTurnBasedMode,
            AwaitTurnBasedTurn,
            AwaitTurnBasedAttack,
            AwaitRealTimeRestore,
            AwaitTargetRemoval,
            AwaitMountedReady,
            AwaitMountedRealTimeMovement,
            AwaitMountedCombatEntry,
            AwaitMountedTurnBasedMode,
            AwaitMountedRiderTurn,
            AwaitMountedTurnBasedMovement,
            AwaitMountedRealTimeRestore,
            AwaitMountedRiderAttack,
            AwaitMountedHorseAttack,
            AwaitMountedDismount,
            AwaitDeath,
            AwaitRecovery,
            AwaitRespecRemoval,
            AwaitCleanup
        }
    }
}
