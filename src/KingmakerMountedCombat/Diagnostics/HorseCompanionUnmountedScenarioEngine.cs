using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
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
        private const double LifecycleTimeoutSeconds = 60.0;
        private const double DirectDamageObservationSeconds = 5.0;
        private const int MaximumStockLifecycleAttacks = 6;
        private const double RealTimeAttackTimeoutSeconds = 20.0;
        private const double TurnBasedTurnAcquisitionTimeoutSeconds = 20.0;
        private const double TurnBasedAttackTimeoutSeconds = 20.0;
        private const double MountedAlphaAdmissionTimeoutSeconds = 20.0;
        private const float MovementDistance = 2.0f;
        private const float MovementTolerance = 0.75f;
        private const float TargetDistance = 4.0f;
        private const string RangerClassGuid = "cda0615668a6df14eb36ba19ee881af6";
        private const string HuntersBondSelectionGuid = "b705c5184a96a84428eeb35ae2517a14";

        private static readonly FieldInfo AiBackingField = typeof(UnitEntityData).GetField(
            "m_AiEnabled",
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
        private const int TurnBasedPostDispatchStartTurnRequestCount = 0;
        private int turnBasedNativeTurnStableFrames;
        private int turnBasedStableReadyFrames;
        private bool mountedAlphaStarted;
        private bool mountedAlphaDismounted;
        private double mountedAlphaAdmissionStartedAtSeconds;
        private UnitMoveTo mountedRealTimeMove;
        private Vector3 mountedRealTimeRiderStart;
        private Vector3 mountedRealTimeHorseStart;
        private Vector3 mountedRealTimeDestination;
        private int mountedRiderTurnStartRequests;
        private int mountedNativeTurnStableFrames;
        private int mountedRiderTurnStableFrames;
        private const int MountedPostMoveTurnReassertions = 0;
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
        private double lifecycleStartedAtSeconds = -1.0;
        private NativeHorseLifeStateProbe horseLifeStateProbe;
        private UnitAttack stockLifecycleAttack;
        private double stockLifecycleAttackIssuedAtSeconds;
        private int stockLifecycleAttackCount;
        private int stockLifecycleAttackRuleCount;
        private int stockLifecycleAttackRollCount;
        private int stockLifecycleDamageRuleCount;
        private int stockLifecycleForcedD20Count;
        private int stockLifecycleDamage;
        private int stockLifecycleTransitionBaseline;
        private bool ownerAiBeforeLifecycle;
        private bool horseAiBeforeLifecycle;
        private bool pairAiSuppressedForLifecycle;
        private int directDamageTransitionBaseline;
        private double directDamageStartedAtSeconds;
        private bool directDamageObservedOutsideAwakeSchedule;
        private string directDamageTimelineState;
        private readonly JArray stockLifecycleAttacks = new JArray();
        private readonly JArray directDamageTimeline = new JArray();

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
                var lifecyclePhase = step == EngineStep.AwaitDeath ||
                    step == EngineStep.AwaitRecovery ||
                    step == EngineStep.AwaitLifecycleTargetRemoval ||
                    step == EngineStep.AwaitDirectDamage ||
                    step == EngineStep.AwaitDirectRecovery ||
                    step == EngineStep.AwaitRespecRemoval;
                var expiredDeadline = HorseCompanionScenarioDeadlinePolicy.Evaluate(
                    clock.Elapsed.TotalSeconds,
                    scenarioDeadline,
                    lifecyclePhase,
                    lifecycleStartedAtSeconds,
                    LifecycleTimeoutSeconds);
                if (!cleanupStarted && expiredDeadline != HorseCompanionDeadlineKind.None)
                {
                    var lifecycleExpired = expiredDeadline == HorseCompanionDeadlineKind.Lifecycle;
                    var deadlineSeconds = lifecycleExpired ? LifecycleTimeoutSeconds : scenarioDeadline;
                    Fail(
                        lifecycleExpired ? "lifecycle-deadline" : "bounded-deadline",
                        (lifecycleExpired ? "Horse lifecycle qualification exceeded " : "Horse qualification exceeded ") +
                        deadlineSeconds.ToString("0") + " seconds at " + step + ".");
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
                    case EngineStep.AwaitMountedAlphaAdmission:
                        AwaitMountedAlphaAdmission();
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
                    case EngineStep.AwaitLifecycleTargetRemoval:
                        AwaitLifecycleTargetRemoval();
                        break;
                    case EngineStep.AwaitDirectDamage:
                        AwaitDirectDamage();
                        break;
                    case EngineStep.AwaitDirectRecovery:
                        AwaitDirectRecovery();
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
            DisposeHorseLifeStateProbe();
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
            CreateHorseThroughNativeRangerLevelUp();
            rankFact = owner.Descriptor.GetFact(service.LevelRank) as Feature;
            horseFeatureFact = owner.Descriptor.GetFact(service.HorseFeature) as Feature;
            Check(rankFact != null && rankFact.GetRank() == 1 && horseFeatureFact != null &&
                    owner.Descriptor.Pet != null &&
                    string.Equals(owner.Descriptor.Pet.Blueprint?.AssetGuid,
                        HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal),
                "feature-activation",
                "Four native Ranger LevelUpController commits reached Ranger 4, produced the exact Ranger-offset companion rank 1, activated Horse once, and created the exact pet.");
            if (failed != 0) { BeginCleanup(); return; }
            step = EngineStep.AwaitHorseSpawn;
        }

        private void CreateHorseThroughNativeRangerLevelUp()
        {
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(RangerClassGuid);
            var huntersBond = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(HuntersBondSelectionGuid);
            var rangerCompanion = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(
                HorseCompanionBlueprintService.RangerSelectionGuid);
            if (ranger == null || huntersBond == null || rangerCompanion == null ||
                !ReferenceEquals(rangerCompanion, ResourcesLibrary.LibraryObject.BlueprintsByAssetId[
                    HorseCompanionBlueprintService.RangerSelectionGuid]))
            {
                throw new InvalidOperationException("The exact Ranger class/level-4 bond/companion selection contract is unavailable.");
            }

            var originalCharacterLevel = owner.Descriptor.Progression.CharacterLevel;
            var originalRangerLevel = owner.Descriptor.Progression.GetClassLevel(ranger);
            var originalAutoLevelup = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            var commits = 0;
            var huntersBondSelectionLevel = -1;
            var rangerCompanionSelectionLevel = -1;
            if (originalRangerLevel != 0)
            {
                throw new InvalidOperationException("The disposable native-level-up owner already has Ranger levels: " + originalRangerLevel + ".");
            }
            try
            {
                // The Working fixture enables automatic plans globally. Disable
                // that setting only while constructing each native controller so
                // the diagnostic can exercise an explicit Ranger selection.
                SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
                for (var rangerLevel = 1; rangerLevel <= 4; rangerLevel++)
                {
                    LevelUpController controller = null;
                    var committed = false;
                    try
                    {
                        controller = LevelUpController.StartWithoutAssigningStaticInstance(owner.Descriptor);
                        if (!controller.SelectClass(ranger))
                        {
                            throw new InvalidOperationException("Native Ranger class selection failed at Ranger level " + rangerLevel + ".");
                        }

                        if (rangerLevel == 4)
                        {
                            var bondState = FindExactSelection(controller, huntersBond);
                            huntersBondSelectionLevel = bondState.Level;
                            var companionItem = bondState.Selection.Items.SingleOrDefault(item =>
                                ReferenceEquals(item.Feature, rangerCompanion));
                            if (companionItem == null || !controller.SelectFeature(bondState, companionItem))
                            {
                                throw new InvalidOperationException("Native Hunter's Bond did not accept the exact Ranger companion selection.");
                            }

                            var companionState = FindExactSelection(controller, rangerCompanion);
                            rangerCompanionSelectionLevel = companionState.Level;
                            var horseItem = companionState.Selection.Items.SingleOrDefault(item =>
                                ReferenceEquals(item.Feature, service.HorseFeature));
                            if (horseItem == null || !controller.SelectFeature(companionState, horseItem))
                            {
                                throw new InvalidOperationException("Native Ranger companion selection did not accept the exact KMC Horse item.");
                            }

                            var previewFact = controller.Preview.GetFact(service.HorseFeature);
                            logger.Info("Native Ranger horse preview selected: fact=" + (previewFact != null) +
                                "; previewPet=" + (controller.Preview.Pet != null) + ".");
                        }

                        controller.Commit();
                        committed = true;
                        commits++;
                        logger.Info("Native Ranger level-up committed: rangerLevel=" + rangerLevel +
                            "; characterLevel=" + owner.Descriptor.Progression.CharacterLevel +
                            "; horseFact=" + (owner.Descriptor.GetFact(service.HorseFeature) != null) +
                            "; pet=" + (owner.Descriptor.Pet != null) + ".");
                    }
                    finally
                    {
                        if (controller != null && !committed)
                        {
                            controller.Cancel();
                        }
                    }
                }
            }
            finally
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = originalAutoLevelup;
            }

            var bondSelections = owner.Descriptor.Progression.GetSelections(huntersBond, huntersBondSelectionLevel);
            var companionSelections = owner.Descriptor.Progression.GetSelections(
                rangerCompanion, rangerCompanionSelectionLevel);
            var feature = owner.Descriptor.GetFact(service.HorseFeature) as Feature;
            var rank = owner.Descriptor.GetFact(service.LevelRank)?.GetRank() ?? 0;
            var pet = owner.Descriptor.Pet;
            observations["nativeRangerCommitCount"] = commits;
            observations["huntersBondSelectionLevel"] = huntersBondSelectionLevel;
            observations["rangerCompanionSelectionLevel"] = rangerCompanionSelectionLevel;
            observations["horseFactRankAtCommit"] = rank;
            observations["horsePresentAtNativeCommit"] = pet != null &&
                string.Equals(pet.Blueprint?.AssetGuid, HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal);
            observations["horseFeatureSourceGuid"] = feature?.Source?.AssetGuid;
            Check(
                commits == 4 &&
                owner.Descriptor.Progression.CharacterLevel == originalCharacterLevel + 4 &&
                owner.Descriptor.Progression.GetClassLevel(ranger) == originalRangerLevel + 4 &&
                huntersBondSelectionLevel == 4 && rangerCompanionSelectionLevel == 4 &&
                bondSelections.Count(item => ReferenceEquals(item, rangerCompanion)) == 1 &&
                companionSelections.Count(item => ReferenceEquals(item, service.HorseFeature)) == 1 &&
                feature != null && feature.Source != null && rank == 1 && pet != null &&
                string.Equals(pet.Blueprint?.AssetGuid, HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal),
                "native-ranger-level-up-commit",
                "The exact native preview/select/commit pipeline recorded Hunter's Bond -> Ranger companion -> Horse at Ranger progression level 4, retained feature source, granted rank 1, and created the exact pet.");
        }

        private static FeatureSelectionState FindExactSelection(
            LevelUpController controller,
            BlueprintFeatureSelection selection)
        {
            var matches = controller.State.Selections.Where(state =>
                ReferenceEquals(state.Selection, selection) && !state.Selected).ToArray();
            if (matches.Length != 1)
            {
                var available = string.Join(", ", controller.State.Selections.Select(state =>
                {
                    var blueprint = state.Selection as BlueprintScriptableObject;
                    return (blueprint == null ? state.Selection.GetType().FullName : blueprint.name + "/" + blueprint.AssetGuid) +
                        ":selected=" + state.Selected;
                }).ToArray());
                throw new InvalidOperationException("Expected one unselected native " + selection.name +
                    " state; observed " + matches.Length + ". Available: " + available + ".");
            }
            return matches[0];
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
            Check(companionAddPet != null && expectedCharacterLevel == 2 && expectedExperience >= 0 &&
                    companionAddPet.NativeProgressionReady &&
                    (classProgressionSynchronized ||
                     (manualLevelingReady && experience == expectedExperience)) &&
                    rankFact.GetRank() == 1 &&
                    !companionAddPet.DeferredProgressionPending && !companionAddPet.DeferredProgressionFailed &&
                    companionAddPet.DeferredNativeAttempts <= HorseCompanionProgressionPolicy.MaximumDeferredNativeAttempts &&
                    upgrade == null,
                "rank-progression-and-upgrade",
                "Stock AddPet mapped Ranger 4's effective companion rank 1 to animal-companion level 2 or the exact native manual-leveling XP threshold, without prematurely applying the rank-4 upgrade or duplicating progression.");
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
            turnBasedTurnAcquisitionStartedAtSeconds = clock.Elapsed.TotalSeconds;
            turnBasedNativeTurnStableFrames = 0;
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
                turnBasedNativeTurnStableFrames = 0;
                return;
            }
            if (clock.Elapsed.TotalSeconds - turnBasedTurnAcquisitionStartedAtSeconds >
                TurnBasedTurnAcquisitionTimeoutSeconds)
            {
                Fail("turn-based-native-turn-deadline",
                    "The native controller did not settle its initial queued turn within the bounded 20-second leaf.");
                BeginCleanup();
                return;
            }

            var nativeTurn = controller.CurrentTurn;
            if (nativeTurn == null ||
                (nativeTurn.Status != TurnController.TurnStatus.Preparing && !nativeTurn.IsActing))
            {
                turnBasedNativeTurnStableFrames = 0;
                return;
            }
            turnBasedNativeTurnStableFrames++;
            if (turnBasedNativeTurnStableFrames < 2)
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
                TurnBasedPostDispatchStartTurnRequestCount;
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
            if (!IncludesMountedAlpha)
            {
                BeginDeathProbe();
                return;
            }
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
                BeginMountedAlphaAdmission();
                return;
            }
            Fail("unexpected-pre-lifecycle-target-removal",
                "The final Horse lifecycle target was removed before the stock-damage comparison began.");
            BeginCleanup();
        }

        private void BeginMountedAlphaAdmission()
        {
            settings.EnableUnsafeMovementExperiment = true;
            mountedAlphaAdmissionStartedAtSeconds = clock.Elapsed.TotalSeconds;
            SelectionManager.Instance.SelectUnit(owner.View, true, true, false);
            step = EngineStep.AwaitMountedAlphaAdmission;
        }

        private void AwaitMountedAlphaAdmission()
        {
            if (Game.Instance.IsPaused) { Game.Instance.IsPaused = false; }
            TryLeaveCombat(horse);
            TryLeaveCombat(owner);

            var selection = SelectionManager.Instance;
            var selected = selection?.SelectedUnits;
            if (selection != null && (selected == null || selected.Count != 1 || selected[0] != owner))
            {
                selection.SelectUnit(owner.View, true, true, false);
                return;
            }

            var availability = playerAction.GetAvailability();
            if (availability.IsVisible && availability.IsEnabled &&
                availability.Action == MountedPlayerActionKind.Mount)
            {
                BeginMountedAlpha();
                return;
            }

            if (clock.Elapsed.TotalSeconds - mountedAlphaAdmissionStartedAtSeconds <=
                MountedAlphaAdmissionTimeoutSeconds)
            {
                return;
            }

            var game = Game.Instance;
            Fail("target-selected-mount-admission-deadline",
                "Kingmaker did not restore ordinary out-of-combat Mount admission within the bounded 20-second leaf. " +
                "Latest gate: " + availability.Feedback + "; mode=" +
                (game == null ? "<none>" : game.CurrentMode.ToString()) + ".");
            BeginCleanup();
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
                runtime.PoseApplicationFrameCount < 3 || !runtime.PoseFrameApplied)
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

            var poseCalibration = CaptureHorsePoseCalibration();
            observations["horsePoseCalibration"] = poseCalibration;
            Check(poseCalibration != null &&
                    runtime.PoseFootTargetClampCount == 0 &&
                    runtime.PoseMaximumFootTargetResidualWorldUnits <= 0.01d &&
                    runtime.PoseMaximumKneeTargetResidualWorldUnits <= 0.01d &&
                    runtime.PoseMaximumSegmentLengthResidualWorldUnits <= 0.0001d &&
                    (double)poseCalibration["leftFootToAssignedStirrup"] <= 0.5d &&
                    (double)poseCalibration["rightFootToAssignedStirrup"] <= 0.5d,
                "horse-pose-calibration",
                "One Horse-only calibration lowered the dev.23 pelvis, narrowed thigh-relative foot and knee targets, retained exact segment lengths, and placed both feet within a bounded native-stirrup neighborhood; human visual acceptance remains required.");
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
            turnBasedTurnAcquisitionStartedAtSeconds = clock.Elapsed.TotalSeconds;
            mountedNativeTurnStableFrames = 0;
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
                mountedNativeTurnStableFrames = 0;
                return;
            }
            if (clock.Elapsed.TotalSeconds - turnBasedTurnAcquisitionStartedAtSeconds >
                TurnBasedTurnAcquisitionTimeoutSeconds)
            {
                Fail("mounted-native-turn-deadline",
                    "The native controller did not settle its initial queued mounted-combat turn within the bounded 20-second leaf.");
                BeginCleanup();
                return;
            }

            var nativeTurn = controller.CurrentTurn;
            if (nativeTurn == null ||
                (nativeTurn.Status != TurnController.TurnStatus.Preparing && !nativeTurn.IsActing))
            {
                mountedNativeTurnStableFrames = 0;
                return;
            }
            mountedNativeTurnStableFrames++;
            if (mountedNativeTurnStableFrames < 2)
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
            if (combat.HasActiveGroundMovement)
            {
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
            observations["mountedTurnPostDispatchReassertions"] = MountedPostMoveTurnReassertions;
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
            if (Game.Instance.IsPaused) { Game.Instance.IsPaused = false; }
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
            var expectedHorseBiteGuid = service.CaptureSnapshot().BiteGuid;
            Check(mountedHorseOutcome.Action == MountedCombatActionKind.MountPrimaryNatural &&
                    string.Equals(mountedHorseOutcome.Result, UnitCommand.ResultType.Success.ToString(), StringComparison.Ordinal) &&
                    string.Equals(mountedHorseOutcome.ActorId, horse.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(mountedHorseOutcome.CommandOwnerId, horse.UniqueId, StringComparison.Ordinal) &&
                    string.Equals(mountedHorseOutcome.ResourceOwnerId, horse.UniqueId, StringComparison.Ordinal) &&
                    mountedHorseOutcome.ChildAttackStartCount == 1 && mountedHorseOutcome.AttackWeaponIsNatural &&
                    string.Equals(mountedHorseOutcome.AttackWeaponBlueprintId, expectedHorseBiteGuid, StringComparison.Ordinal) &&
                    string.Equals(mountedHorseOutcome.AttackWeaponSlot,
                        NativeSingleAttackSlotKind.AdditionalLimb.ToString(), StringComparison.Ordinal) &&
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
            BeginDeathProbe();
        }

        private void BeginDeathProbe()
        {
            if (relationship.State != RelationshipState.Unmounted || targetService == null ||
                target == null || !target.IsInState || horse == null || !horse.IsInState ||
                owner == null || !owner.IsInState || AiBackingField == null ||
                AiBackingField.FieldType != typeof(bool))
            {
                Fail("stock-lifecycle-admission",
                    "The exact unmounted Horse, owner, hostile stock attacker, or raw AI contract was unavailable.");
                BeginCleanup();
                return;
            }

            DisposeHorseLifeStateProbe();
            horseLifeStateProbe = new NativeHorseLifeStateProbe(horse);
            stockLifecycleTransitionBaseline = horseLifeStateProbe.Snapshot().Count;
            observations["stockLifecycleBefore"] = CaptureHorseLifeState();
            lethalDamage = (int)horse.Stats.HitPoints + (int)horse.Stats.Constitution + 1;
            observations["lethalDamage"] = lethalDamage;

            owner.Commands.InterruptAll(false);
            horse.Commands.InterruptAll(false);
            target.Commands.InterruptAll(false);
            owner.Commands.RemoveFinishedAndUpdateQueue();
            horse.Commands.RemoveFinishedAndUpdateQueue();
            target.Commands.RemoveFinishedAndUpdateQueue();
            ownerAiBeforeLifecycle = (bool)AiBackingField.GetValue(owner);
            horseAiBeforeLifecycle = (bool)AiBackingField.GetValue(horse);
            owner.IsAIEnabled = false;
            horse.IsAIEnabled = false;
            pairAiSuppressedForLifecycle = !(bool)AiBackingField.GetValue(owner) &&
                !(bool)AiBackingField.GetValue(horse);
            Check(pairAiSuppressedForLifecycle && owner.Commands.Empty && horse.Commands.Empty && target.Commands.Empty,
                "stock-lifecycle-admission",
                "The exact hostile stock-attack comparison suppressed only owner/Horse autonomous commands and began from empty queues.");
            if (failed != 0) { BeginCleanup(); return; }

            stockLifecycleAttackCount = 0;
            stockLifecycleAttackRuleCount = 0;
            stockLifecycleAttackRollCount = 0;
            stockLifecycleDamageRuleCount = 0;
            stockLifecycleForcedD20Count = 0;
            stockLifecycleDamage = 0;
            stockLifecycleAttacks.Clear();
            lifecycleStartedAtSeconds = clock.Elapsed.TotalSeconds;
            step = EngineStep.AwaitDeath;
            IssueStockLifecycleAttack();
        }

        private void IssueStockLifecycleAttack()
        {
            if (stockLifecycleAttackCount >= MaximumStockLifecycleAttacks)
            {
                Fail("ordinary-stock-damage-lifecycle",
                    "Six exact forced-hit stock attacks did not produce a native non-conscious Horse transition.");
                BeginCleanup();
                return;
            }
            if (targetService == null || !targetService.RefreshBidirectionalCombatMemoryLease() ||
                target == null || horse == null || !target.IsInState || !horse.IsInState ||
                !horse.Descriptor.State.IsConscious)
            {
                return;
            }

            target.Commands.InterruptAll(false);
            target.Commands.RemoveFinishedAndUpdateQueue();
            if (ruleProbe == null)
            {
                ruleProbe = new MountedCombatRuleProbe();
            }
            ruleProbe.Arm(owner, horse, target, horse, 20);
            stockLifecycleAttack = new UnitAttack(horse) { IsSingleAttack = true };
            stockLifecycleAttack.IgnoreCooldown();
            target.Commands.Run(stockLifecycleAttack);
            stockLifecycleAttackCount++;
            stockLifecycleAttackIssuedAtSeconds = clock.Elapsed.TotalSeconds;
            if (!ReferenceEquals(target.Commands.Standard, stockLifecycleAttack) ||
                stockLifecycleAttack.Executor != target)
            {
                Fail("ordinary-stock-damage-command",
                    "The exact hostile Mammoth did not retain its stock single attack against the Horse.");
                BeginCleanup();
            }
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
                ["attackWeaponSlot"] = outcome.AttackWeaponSlot,
                ["delegatedMoveExecutorId"] = outcome.DelegatedMoveExecutorId,
                ["delegatedMoveExecutorIsExactMount"] = outcome.DelegatedMoveExecutorIsExactMount,
                ["riderStandardCharged"] = outcome.RiderStandardCharged,
                ["actionStandardCharged"] = outcome.ActionStandardCharged,
                ["terminalReason"] = outcome.TerminalReason
            };
        }

        private void AwaitDeath()
        {
            var lifeTransitions = horseLifeStateProbe?.Snapshot() ?? new NativeHorseLifeStateObservation[0];
            var expectedTransitions = lifeTransitions.Skip(stockLifecycleTransitionBaseline).Where(item =>
                HorseCompanionLifeTransitionPolicy.IsExpectedNonConsciousTransition(
                    horseId,
                    item.ActorId,
                    item.PreviousLifeState,
                    item.CurrentLifeState)).ToArray();

            if (stockLifecycleAttack != null && !stockLifecycleAttack.IsFinished)
            {
                if (clock.Elapsed.TotalSeconds - stockLifecycleAttackIssuedAtSeconds <= RealTimeAttackTimeoutSeconds)
                {
                    return;
                }
                Fail("ordinary-stock-damage-command-deadline",
                    "A hostile stock Horse lifecycle attack exceeded its bounded 20-second command leaf.");
                BeginCleanup();
                return;
            }

            if (stockLifecycleAttack != null)
            {
                stockLifecycleAttackRuleCount += ruleProbe.AttackRuleCount;
                stockLifecycleAttackRollCount += ruleProbe.AttackRollCount;
                stockLifecycleDamageRuleCount += ruleProbe.DamageRuleCount;
                stockLifecycleForcedD20Count += ruleProbe.ForcedD20Count;
                stockLifecycleDamage += ruleProbe.TotalDamage;
                stockLifecycleAttacks.Add(new JObject
                {
                    ["sequence"] = stockLifecycleAttackCount,
                    ["result"] = stockLifecycleAttack.Result.ToString(),
                    ["attackRules"] = ruleProbe.AttackRuleCount,
                    ["attackRolls"] = ruleProbe.AttackRollCount,
                    ["damageRules"] = ruleProbe.DamageRuleCount,
                    ["forcedD20Count"] = ruleProbe.ForcedD20Count,
                    ["damage"] = ruleProbe.TotalDamage,
                    ["horseDamageAfter"] = horse.Damage,
                    ["horseLifeStateAfter"] = horse.Descriptor.State.LifeState.ToString()
                });
                if (ruleProbe.AttackRuleCount != 1 || ruleProbe.AttackRollCount != 1 ||
                    ruleProbe.DamageRuleCount != 1 || ruleProbe.ForcedD20Count < 1 ||
                    ruleProbe.TotalDamage <= 0 || ruleProbe.UnexpectedPairAttackCount != 0)
                {
                    Fail("ordinary-stock-damage-rule-chain",
                        "A hostile stock lifecycle attack did not retain one exact attack/roll/damage chain.");
                    BeginCleanup();
                    return;
                }
                stockLifecycleAttack = null;
            }

            if (expectedTransitions.Length == 0)
            {
                if (!horse.Descriptor.State.IsConscious)
                {
                    Fail("ordinary-stock-damage-event",
                        "The Horse entered a non-conscious state without the exact native life-state callback.");
                    BeginCleanup();
                    return;
                }
                IssueStockLifecycleAttack();
                return;
            }

            var transition = expectedTransitions[0];
            observations["stockLifecycleAttacks"] = stockLifecycleAttacks;
            observations["stockLifecycleAttackCount"] = stockLifecycleAttackCount;
            observations["stockLifecycleAttackRules"] = stockLifecycleAttackRuleCount;
            observations["stockLifecycleAttackRolls"] = stockLifecycleAttackRollCount;
            observations["stockLifecycleDamageRules"] = stockLifecycleDamageRuleCount;
            observations["stockLifecycleForcedD20Count"] = stockLifecycleForcedD20Count;
            observations["stockLifecycleRuleDamage"] = stockLifecycleDamage;
            observations["stockLifecycleTransitionEventCount"] = expectedTransitions.Length;
            observations["stockLifecycleTransitionActorId"] = transition.ActorId;
            observations["stockLifecycleTransitionPreviousLifeState"] = transition.PreviousLifeState;
            observations["stockLifecycleTransitionCurrentLifeState"] = transition.CurrentLifeState;
            observations["stockLifecycleAfter"] = CaptureHorseLifeState();
            Check(stockLifecycleAttackCount >= 1 && stockLifecycleAttackCount <= MaximumStockLifecycleAttacks &&
                    stockLifecycleAttackRuleCount == stockLifecycleAttackCount &&
                    stockLifecycleAttackRollCount == stockLifecycleAttackCount &&
                    stockLifecycleDamageRuleCount == stockLifecycleAttackCount &&
                    stockLifecycleForcedD20Count >= stockLifecycleAttackCount &&
                    stockLifecycleDamage > 0 && expectedTransitions.Length == 1 &&
                    !horse.Descriptor.State.IsConscious,
                "ordinary-stock-damage-lifecycle",
                "Ordinary hostile stock attacks produced exact attack/roll/damage chains and one native non-conscious Horse transition.");
            Check(expectedTransitions.Length == 1 && owner.Descriptor.Pet == horse &&
                    horse.Descriptor.Master.Value == owner,
                "death-ownership",
                "The stock Horse life-state transition retained the reciprocal companion ownership relation for recovery.");
            if (failed != 0) { BeginCleanup(); return; }
            horse.Descriptor.ResurrectAndFullRestore();
            step = EngineStep.AwaitRecovery;
        }

        private void AwaitRecovery()
        {
            if (!horse.Descriptor.State.IsConscious || horse.Damage != 0) { return; }
            observations["recoveredDamage"] = horse.Damage;
            observations["stockLifecycleRecovery"] = CaptureHorseLifeState();
            Check(horse.IsInState && horse.View != null && horse.View.gameObject.activeInHierarchy &&
                    owner.Descriptor.Pet == horse && horse.Descriptor.Master.Value == owner,
                "death-and-recovery",
                "The Horse recovered visibly from ordinary stock damage with zero damage and the same reciprocal owner relation.");
            if (failed != 0) { BeginCleanup(); return; }

            TryLeaveCombat(target);
            TryLeaveCombat(horse);
            TryLeaveCombat(owner);
            if (targetService != null && !targetService.DestroyAndVerify())
            {
                step = EngineStep.AwaitLifecycleTargetRemoval;
                return;
            }
            BeginDirectDamageControl();
        }

        private void AwaitLifecycleTargetRemoval()
        {
            if (targetService != null && !targetService.DestroyAndVerify()) { return; }
            BeginDirectDamageControl();
        }

        private void BeginDirectDamageControl()
        {
            observations["targetCleanupExact"] = targetService == null ||
                (targetService.TargetEntityRemoved && targetService.RuntimeGroupRemoved &&
                 targetService.RuntimeFactionRemoved && targetService.CombatMemoryRemoved &&
                 targetService.TargetDurabilityLeaseReleased && targetService.TargetBrainLeaseReleased &&
                 targetService.TargetSleeplessLeaseReleased && targetService.NonPairPartyAiLeaseRestored);
            try { targetService?.Dispose(); }
            catch (Exception exception) { errors.Add("Lifecycle target disposal: " + exception.Message); }
            targetService = null;
            target = null;

            RestoreLifecyclePairAi();
            directDamageTransitionBaseline = horseLifeStateProbe?.Snapshot().Count ?? 0;
            directDamageObservedOutsideAwakeSchedule = false;
            directDamageTimelineState = null;
            directDamageTimeline.Clear();
            observations["directDamageBefore"] = CaptureHorseLifeState();
            horse.Damage = lethalDamage;
            observations["directDamageImmediatelyAfterMutation"] = CaptureHorseLifeState();
            directDamageStartedAtSeconds = clock.Elapsed.TotalSeconds;
            AppendDirectDamageTimeline();
            step = EngineStep.AwaitDirectDamage;
        }

        private void AwaitDirectDamage()
        {
            AppendDirectDamageTimeline();
            var transitions = (horseLifeStateProbe?.Snapshot() ?? new NativeHorseLifeStateObservation[0])
                .Skip(directDamageTransitionBaseline)
                .Where(item => HorseCompanionLifeTransitionPolicy.IsExpectedNonConsciousTransition(
                    horseId, item.ActorId, item.PreviousLifeState, item.CurrentLifeState))
                .ToArray();
            if (transitions.Length != 0)
            {
                observations["directDamageDisposition"] = "native-life-controller-observed-direct-mutation";
                observations["directDamageTransitionEventCount"] = transitions.Length;
                observations["directDamageAfterObservation"] = CaptureHorseLifeState();
                observations["directDamageTimeline"] = directDamageTimeline;
                Check(transitions.Length == 1 && !horse.Descriptor.State.IsConscious,
                    "direct-damage-control-disposition",
                    "The old direct mutation was observed separately and reached one native event in this exact runtime context; stock combat remains the qualification authority.");
                if (failed != 0) { BeginCleanup(); return; }
                horse.Descriptor.ResurrectAndFullRestore();
                step = EngineStep.AwaitDirectRecovery;
                return;
            }

            if (clock.Elapsed.TotalSeconds - directDamageStartedAtSeconds < DirectDamageObservationSeconds)
            {
                return;
            }

            observations["directDamageDisposition"] = directDamageObservedOutsideAwakeSchedule
                ? "direct-mutation-left-native-awake-schedule-without-life-event"
                : "direct-mutation-remained-awake-without-life-event";
            observations["directDamageTransitionEventCount"] = 0;
            observations["directDamageAfterObservation"] = CaptureHorseLifeState();
            observations["directDamageTimeline"] = directDamageTimeline;
            Check(directDamageObservedOutsideAwakeSchedule && horse.Descriptor.State.IsConscious &&
                    horse.Damage == lethalDamage,
                "direct-damage-control-disposition",
                "The old direct assignment changed damage but, after combat teardown, the Horse left the native AwakeUnits schedule before any life-state callback; it is not a valid stock lifecycle qualification path.");
            if (failed != 0) { BeginCleanup(); return; }
            horse.Descriptor.ResurrectAndFullRestore();
            step = EngineStep.AwaitDirectRecovery;
        }

        private void AwaitDirectRecovery()
        {
            if (!horse.Descriptor.State.IsConscious || horse.Damage != 0) { return; }
            observations["directDamageRecovery"] = CaptureHorseLifeState();
            Check(owner.Descriptor.Pet == horse && horse.Descriptor.Master.Value == owner &&
                    horse.IsInState && horse.View != null && horse.View.gameObject.activeInHierarchy,
                "direct-damage-control-recovery",
                "The diagnostic-only direct-damage control restored the same visible Horse and reciprocal ownership without synthesizing a life event.");
            if (failed != 0) { BeginCleanup(); return; }

            DisposeHorseLifeStateProbe();

            owner.Descriptor.Progression.Features.RemoveFact(horseFeatureFact);
            horseFeatureFact = null;
            Game.Instance.EntityDestroyer.Tick();
            step = EngineStep.AwaitRespecRemoval;
        }

        private JObject CaptureHorseLifeState()
        {
            var state = horse?.Descriptor?.State;
            var stats = horse?.Stats;
            var animator = horse?.View?.Animator;
            var animatorAvailable = animator != null && animator.layerCount > 0;
            var animatorState = animatorAvailable
                ? animator.GetCurrentAnimatorStateInfo(0)
                : default(AnimatorStateInfo);
            var dualCompanion = horse?.Get<UnitPartDualCompanion>();
            var master = horse?.Descriptor?.Master.Value;
            var player = Game.Instance?.Player;
            return new JObject
            {
                ["lifeState"] = state?.LifeState.ToString(),
                ["isConscious"] = state != null && state.IsConscious,
                ["isDead"] = state != null && state.IsDead,
                ["stateIsDead"] = state != null && state.IsDead,
                ["isFinallyDead"] = state != null && state.IsFinallyDead,
                ["damage"] = horse?.Damage,
                ["nonLethalDamage"] = horse?.DamageNonLethal,
                ["hitPoints"] = stats == null ? 0 : (int)stats.HitPoints,
                ["temporaryHitPoints"] = stats == null ? 0 : (int)stats.TemporaryHitPoints,
                ["constitution"] = stats == null ? 0 : (int)stats.Constitution,
                ["negativeHitPointThreshold"] = stats == null ? 0 : (int)stats.HitPoints + (int)stats.Constitution,
                ["allowDyingCondition"] = state != null && (bool)state.AllowDyingCondition,
                ["masterAllowDyingCondition"] = master?.Descriptor?.State != null &&
                    (bool)master.Descriptor.State.AllowDyingCondition,
                ["immortality"] = state != null && (bool)state.Immortality,
                ["regeneration"] = state != null && (bool)state.IsRegenerate,
                ["ferocity"] = state != null && (bool)state.Features.Ferocity,
                ["halfOrcFerocity"] = state != null && (bool)state.Features.HalfOrcFerocity,
                ["dualCompanionPartPresent"] = dualCompanion != null,
                ["dualCompanionPartDead"] = dualCompanion != null && dualCompanion.IsDead,
                ["dualCompanionPairId"] = dualCompanion?.PairCompanion.Value?.UniqueId,
                ["isInState"] = horse != null && horse.IsInState,
                ["inStateUnits"] = horse != null && Game.Instance.State.Units.Contains(horse),
                ["inAwakeUnits"] = horse != null && Game.Instance.State.AwakeUnits.Contains(horse),
                ["isAwake"] = horse != null && horse.IsAwake,
                ["isSleeping"] = horse != null && horse.IsSleeping,
                ["awakeTimer"] = horse?.AwakeTimer,
                ["sleepless"] = horse != null && horse.Sleepless,
                ["viewPresent"] = horse?.View != null,
                ["viewActive"] = horse?.View != null && horse.View.gameObject.activeInHierarchy,
                ["animatorPresent"] = animator != null,
                ["animatorLayerCount"] = animator == null ? 0 : animator.layerCount,
                ["animatorStateFullPathHash"] = animatorAvailable ? animatorState.fullPathHash : 0,
                ["animatorStateShortNameHash"] = animatorAvailable ? animatorState.shortNameHash : 0,
                ["animatorStateNormalizedTime"] = animatorAvailable ? animatorState.normalizedTime : 0f,
                ["animatorInTransition"] = animatorAvailable && animator.IsInTransition(0),
                ["ownerPetExact"] = owner != null && owner.Descriptor.Pet == horse,
                ["masterExact"] = master == owner,
                ["ownerPetId"] = owner?.Descriptor?.Pet?.UniqueId,
                ["masterId"] = master?.UniqueId,
                ["controllableRosterContainsHorse"] = player != null && horse != null &&
                    player.ControllableCharacters.Contains(horse),
                ["controllableRosterCount"] = player?.ControllableCharacters.Count ?? 0,
                ["groupIsPlayerParty"] = horse?.Group != null && horse.Group.IsPlayerParty
            };
        }

        private JObject CaptureHorsePoseCalibration()
        {
            var riderRoot = owner?.View?.CharacterAvatar?.transform;
            var mountRoot = horse?.View?.transform;
            var pelvis = FindUniqueTransform(riderRoot, "Pelvis");
            var leftFoot = FindUniqueTransform(riderRoot, "L_foot");
            var rightFoot = FindUniqueTransform(riderRoot, "R_foot");
            var chest = FindUniqueTransform(mountRoot, "Chest");
            var leftStirrup = FindUniqueTransform(mountRoot, "L_Stirrup");
            var rightStirrup = FindUniqueTransform(mountRoot, "R_Stirrup");
            if (pelvis == null || leftFoot == null || rightFoot == null || chest == null ||
                leftStirrup == null || rightStirrup == null)
            {
                return null;
            }

            var directDistance = Vector3.Distance(leftFoot.position, leftStirrup.position) +
                Vector3.Distance(rightFoot.position, rightStirrup.position);
            var crossedDistance = Vector3.Distance(leftFoot.position, rightStirrup.position) +
                Vector3.Distance(rightFoot.position, leftStirrup.position);
            var crossedAssignment = crossedDistance < directDistance;
            var assignedLeft = crossedAssignment ? rightStirrup : leftStirrup;
            var assignedRight = crossedAssignment ? leftStirrup : rightStirrup;
            var pelvisFromChest = mountRoot.InverseTransformVector(pelvis.position - chest.position);
            var leftFootFromStirrup = mountRoot.InverseTransformVector(leftFoot.position - assignedLeft.position);
            var rightFootFromStirrup = mountRoot.InverseTransformVector(rightFoot.position - assignedRight.position);
            var profile = MountedRiderPoseProfiles.MediumHumanoidOnHorse;
            var runtime = relationship.Runtime;
            return new JObject
            {
                ["candidateCount"] = 2,
                ["candidateId"] = "horse-human-review-20260828-b",
                ["dev23PelvisPositionOffset"] = PoseVector(new PoseVector3(0f, 0.02f, -0.02f)),
                ["selectedPelvisPositionOffset"] = PoseVector(profile.PelvisPositionOffset),
                ["dev23LeftFootTargetFromThigh"] = PoseVector(new PoseVector3(-0.305f, -0.46f, 0.044f)),
                ["selectedLeftFootTargetFromThigh"] = PoseVector(profile.LeftLeg.FootTargetFromThigh),
                ["dev23RightFootTargetFromThigh"] = PoseVector(new PoseVector3(0.305f, -0.46f, 0.044f)),
                ["selectedRightFootTargetFromThigh"] = PoseVector(profile.RightLeg.FootTargetFromThigh),
                ["dev23LeftKneeHintFromThigh"] = PoseVector(new PoseVector3(-0.38f, -0.12f, 0.26f)),
                ["selectedLeftKneeHintFromThigh"] = PoseVector(profile.LeftLeg.KneeHintFromThigh),
                ["dev23RightKneeHintFromThigh"] = PoseVector(new PoseVector3(0.38f, -0.12f, 0.26f)),
                ["selectedRightKneeHintFromThigh"] = PoseVector(profile.RightLeg.KneeHintFromThigh),
                ["crossedStirrupAssignment"] = crossedAssignment,
                ["pelvisFromChestMountLocal"] = UnityVector(pelvisFromChest),
                ["leftFootFromAssignedStirrupMountLocal"] = UnityVector(leftFootFromStirrup),
                ["rightFootFromAssignedStirrupMountLocal"] = UnityVector(rightFootFromStirrup),
                ["leftFootToAssignedStirrup"] = Vector3.Distance(leftFoot.position, assignedLeft.position),
                ["rightFootToAssignedStirrup"] = Vector3.Distance(rightFoot.position, assignedRight.position),
                ["poseApplicationFrameCount"] = runtime.PoseApplicationFrameCount,
                ["footTargetClampCount"] = runtime.PoseFootTargetClampCount,
                ["maximumFootTargetResidualWorldUnits"] = runtime.PoseMaximumFootTargetResidualWorldUnits,
                ["maximumKneeTargetResidualWorldUnits"] = runtime.PoseMaximumKneeTargetResidualWorldUnits,
                ["maximumSegmentLengthResidualWorldUnits"] = runtime.PoseMaximumSegmentLengthResidualWorldUnits,
                ["maximumApplyMicroseconds"] = runtime.PoseMaximumApplyMicroseconds,
                ["averageApplyMicroseconds"] = runtime.PoseAverageApplyMicroseconds
            };
        }

        private static JObject PoseVector(PoseVector3 value)
        {
            return new JObject { ["x"] = value.X, ["y"] = value.Y, ["z"] = value.Z };
        }

        private static JObject UnityVector(Vector3 value)
        {
            return new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };
        }

        private static Transform FindUniqueTransform(Transform root, string exactName)
        {
            Transform found = null;
            var count = 0;
            FindTransforms(root, exactName, ref found, ref count);
            return count == 1 ? found : null;
        }

        private static void FindTransforms(Transform current, string exactName, ref Transform found, ref int count)
        {
            if (current == null || count > 1) { return; }
            if (string.Equals(current.name, exactName, StringComparison.Ordinal))
            {
                found = current;
                count++;
            }
            for (var index = 0; index < current.childCount; index++)
            {
                FindTransforms(current.GetChild(index), exactName, ref found, ref count);
            }
        }

        private void RestoreLifecyclePairAi()
        {
            if (!pairAiSuppressedForLifecycle) { return; }
            owner.IsAIEnabled = ownerAiBeforeLifecycle;
            horse.IsAIEnabled = horseAiBeforeLifecycle;
            if ((bool)AiBackingField.GetValue(owner) != ownerAiBeforeLifecycle ||
                (bool)AiBackingField.GetValue(horse) != horseAiBeforeLifecycle)
            {
                throw new InvalidOperationException("The stock lifecycle comparison did not restore exact owner/Horse raw AI state.");
            }
            pairAiSuppressedForLifecycle = false;
        }

        private void AppendDirectDamageTimeline()
        {
            var state = horse.Descriptor.State;
            var inAwakeUnits = Game.Instance.State.AwakeUnits.Contains(horse);
            if (!inAwakeUnits) { directDamageObservedOutsideAwakeSchedule = true; }
            var key = state.LifeState + "|" + horse.Damage + "|" + inAwakeUnits + "|" + horse.IsSleeping;
            if (string.Equals(key, directDamageTimelineState, StringComparison.Ordinal)) { return; }
            directDamageTimelineState = key;
            directDamageTimeline.Add(new JObject
            {
                ["secondsSinceMutation"] = clock.Elapsed.TotalSeconds - directDamageStartedAtSeconds,
                ["lifeState"] = state.LifeState.ToString(),
                ["damage"] = horse.Damage,
                ["inAwakeUnits"] = inAwakeUnits,
                ["isAwake"] = horse.IsAwake,
                ["isSleeping"] = horse.IsSleeping,
                ["awakeTimer"] = horse.AwakeTimer
            });
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
            try { stockLifecycleAttack?.Interrupt(); } catch (Exception exception) { errors.Add("Stock lifecycle attack cleanup: " + exception.Message); }
            try { movementCommand?.Interrupt(); } catch (Exception exception) { errors.Add("Movement cleanup: " + exception.Message); }
            try { mountedRealTimeMove?.Interrupt(); } catch (Exception exception) { errors.Add("Mounted RT movement cleanup: " + exception.Message); }
            TryLeaveCombat(target);
            TryLeaveCombat(horse);
            TryLeaveCombat(owner);
            try { targetService?.DestroyAndVerify(); } catch (Exception exception) { errors.Add("Target cleanup: " + exception.Message); }
            try { ruleProbe?.Dispose(); } catch (Exception exception) { errors.Add("Rule-probe cleanup: " + exception.Message); }
            ruleProbe = null;
            DisposeHorseLifeStateProbe();
            try { RestoreLifecyclePairAi(); } catch (Exception exception) { errors.Add("Lifecycle pair AI cleanup: " + exception.Message); }

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

        private void DisposeHorseLifeStateProbe()
        {
            if (horseLifeStateProbe == null) { return; }
            try { horseLifeStateProbe.Dispose(); }
            catch (Exception exception) { errors.Add("Horse life-state probe cleanup: " + exception.Message); }
            horseLifeStateProbe = null;
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
                ["schemaVersion"] = 4,
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

        private sealed class NativeHorseLifeStateProbe : IUnitLifeStateChanged, IDisposable
        {
            private readonly UnitEntityData expectedHorse;
            private readonly object gate = new object();
            private readonly List<NativeHorseLifeStateObservation> observations =
                new List<NativeHorseLifeStateObservation>();
            private readonly IDisposable subscription;
            private bool disposed;

            internal NativeHorseLifeStateProbe(UnitEntityData expectedHorse)
            {
                this.expectedHorse = expectedHorse ?? throw new ArgumentNullException(nameof(expectedHorse));
                subscription = EventBus.Subscribe(this);
            }

            public void HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
            {
                if (!ReferenceEquals(unit, expectedHorse)) { return; }
                lock (gate)
                {
                    observations.Add(new NativeHorseLifeStateObservation
                    {
                        ActorId = unit.UniqueId,
                        PreviousLifeState = prevLifeState.ToString(),
                        CurrentLifeState = unit.Descriptor.State.LifeState.ToString()
                    });
                }
            }

            internal IReadOnlyList<NativeHorseLifeStateObservation> Snapshot()
            {
                lock (gate) { return observations.ToArray(); }
            }

            public void Dispose()
            {
                if (disposed) { return; }
                subscription.Dispose();
                disposed = true;
            }
        }

        private sealed class NativeHorseLifeStateObservation
        {
            internal string ActorId { get; set; }

            internal string PreviousLifeState { get; set; }

            internal string CurrentLifeState { get; set; }
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
            AwaitMountedAlphaAdmission,
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
            AwaitLifecycleTargetRemoval,
            AwaitDirectDamage,
            AwaitDirectRecovery,
            AwaitRespecRemoval,
            AwaitCleanup
        }
    }
}
