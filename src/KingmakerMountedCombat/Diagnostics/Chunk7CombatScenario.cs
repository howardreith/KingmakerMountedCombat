using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7CombatScenario : IDisposable
    {
        internal const string RiderFirst = "chunk7-two-pair-combat-rider-first";
        internal const string MountFirst = "chunk7-two-pair-combat-mount-first";
        internal const string EvidenceFileName = "chunk7-combat-evidence.json";
        internal const string RealtimeCore = "chunk7-two-pair-realtime-core";
        internal const string RealtimeCasting = "chunk7-two-pair-realtime-casting";
        internal const string RealtimeCharge = "chunk7-two-pair-realtime-charge";
        internal const string NativeLifeCleanup = "chunk7-two-pair-native-life-cleanup";
        internal const string EvidenceKind = "chunk7-combat-evidence";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly RuntimeRequest request;
        private readonly MountedWorldService world;
        private readonly NativeMountedControlService controls;
        private readonly HorseCompanionBlueprintService horses;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly MountedLifecycleSubscriber lifecycle;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly JObject evidence = new JObject();
        private readonly JArray inputs = new JArray(), activations = new JArray(), visits = new JArray(), snapshots = new JArray();
        private readonly List<UnitCommand> issuedCommands = new List<UnitCommand>();
        private readonly HashSet<TurnController> visited = new HashSet<TurnController>();
        private readonly HashSet<int> completedPairs = new HashSet<int>(), refreshedPairs = new HashSet<int>();
        private readonly long[] firstSequences = new long[2];
        private readonly int[] firstRiderPrepares = new int[2], firstMountPrepares = new int[2];
        private readonly Dictionary<UnitEntityData, int> initiativeBefore = new Dictionary<UnitEntityData, int>();
        private UnitEntityData ar, am, br, bm, target;
        private UnitEntityData[] originalParty, actors, selectionBefore;
        private Chunk7NativeWorldScope nativeScope;
        private MountedPairServices a, b, actingPair;
        private Feature horseFeature, rankFeature;
        private readonly MountedPersistenceService persistence;
        private readonly RuntimeSaveAuthorization fixtureAuthorization;
        private readonly PersistenceSaveAuthorization fixtureAuthority;
        private readonly WorkingFixtureLoader fixtureLoader;
        private Chunk7OwnedPairFixture inherited;
        private ScopedDiagnosticAiLease<UnitEntityData> firstPairAi;
        private DiagnosticCombatTargetService targetService;
        private NativeActorAllocationTrace trace;
        private NativeTurnCompletionProbe completion;
        private NativeRoundBoundaryProbe rounds;
        private NativeModeTransitionProbe mode;
        private MovementCaptureOverlayLease umm;
        private bool started, completed, disposed, captured, cleanupStarted, cleanupServicesSettled;
        private bool pauseBefore, pairedBefore, movementBefore, chargeBefore;
        private int step, actionStage, actingIndex, splitRound, preparesAtSplit;
        private double stepStarted;
        private TurnController actingTurn, endedTurn;
        private UnitUseAbility relationshipShell;
        private JObject activation, r1;
        private bool r1Injected;
        private Action<CleanupTrigger> cleanupFault;
        internal bool IsCompleted => completed;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;
        internal IReadOnlyList<string> Errors => errors;
        internal static bool SupportsScenario(string scenario) => scenario == RiderFirst || scenario == MountFirst ||
            scenario == RealtimeCore || scenario == RealtimeCasting || scenario == RealtimeCharge || scenario == NativeLifeCleanup;
        internal static bool SupportsRequest(RuntimeRequest request) => SupportsScenario(request?.SelectedChunk7Scenario);
        private string SelectedScenario => request.SelectedChunk7Scenario;
        private bool RealtimeScenario => SelectedScenario == RealtimeCore || SelectedScenario == RealtimeCasting || SelectedScenario == RealtimeCharge;
        private bool RealtimeMode => RealtimeScenario || checkpointOnly && checkpointKind == Chunk7CombatCheckpointKind.RealTimeDebts;
        internal Chunk7CombatScenario(RuntimeRequest request, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, HorseCompanionBlueprintService horses, DiagnosticSettings settings, IModLogger logger, MountedLifecycleSubscriber lifecycle = null, MountedPersistenceService persistence = null, RuntimeSaveAuthorization fixtureAuthorization = null,
            PersistenceSaveAuthorization fixtureAuthority = null, WorkingFixtureLoader fixtureLoader = null, MountedRemovalPreparation removalPreparation = null, Func<bool, bool> registeredToggle = null)
        { this.request = request; world = relationship.World; this.controls = controls; this.horses = horses; this.settings = settings; this.logger = logger; this.lifecycle = lifecycle; this.persistence = persistence; this.fixtureAuthorization = fixtureAuthorization; this.fixtureAuthority = fixtureAuthority; this.fixtureLoader = fixtureLoader; this.removalPreparation = removalPreparation; this.registeredToggle = registeredToggle; }

        internal void Start()
        {
            if (started || disposed || !SupportsRequest(request)) throw new InvalidOperationException("Chunk 7 combat admission differs.");
            started = true;
            try
            {
                Require(request.SchemaVersion == RuntimeRequest.SaveBackedSchemaVersion && world != null &&
                    NativeMountedSaveFormat.ArchiveNativeSupportEnabled && GameMountedRelationshipService.NativePairCapacity >= 2,
                    "Reviewed native two-pair adapter and exact save-backed request are required.");
                Require(!RegisteredDisableCase || removalPreparation != null && registeredToggle != null,
                    "Registered disable diagnostic requires its actual removal service and registered UMM callback before fixture admission.");
                if (request.OwnedPairs != null)
                {
                    inherited = new Chunk7OwnedPairFixture(request, horses, world, controls, persistence, fixtureAuthorization, fixtureAuthority, fixtureLoader); inherited.Bind(false);
                    originalParty = inherited.Party; ar = inherited.Actors[0]; am = inherited.Actors[1]; br = inherited.Actors[2]; bm = inherited.Actors[3];
                    evidence["inheritedFixture"] = inherited.Capture("admitted");
                }
                else
                {
                    originalParty = Chunk7NativeFixture.ValidateWorking(request, horses);
                    Require(Game.Instance.Player.GameId == request.Fixture.Working.GameId, "Loaded fixture campaign differs.");
                    ar = originalParty.Single(x => x.UniqueId == Chunk7WorldScenarioEngine.MammothRiderId);
                    am = originalParty.Single(x => x.UniqueId == Chunk7WorldScenarioEngine.MammothId);
                    br = originalParty.Single(x => x.UniqueId == Chunk7WorldScenarioEngine.HorseRiderId);
                }
                // Owner-seed-derived combat starts from the restored MOUNTED graph, verified in
                // step 2 (AdmitMountedStart); every other case keeps its unmounted start.
                if (OwnerSeedDerived) Require(!controls.HasUnsettledRelationshipTransition, "Owner-seed-derived combat found an unsettled relationship transition.");
                else Require(world.Registry.ActiveCount == 0 && world.Pairs.All(p => p.Relationship.State == RelationshipState.Unmounted &&
                    p.Relationship.Runtime.NoPreparedPairResidue) && !controls.HasUnsettledRelationshipTransition,
                    "Combat fixture requires a settled unmounted world.");
                Require(!settings.EnableUnifiedMountedTurn && !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay,
                    "Unsupported legacy or overlay authority is enabled.");
                nativeScope = inherited?.Scope ?? new Chunk7NativeWorldScope(request, originalParty);
                selectionBefore = SelectionManager.Instance.SelectedUnits.ToArray(); pauseBefore = Game.Instance.IsPaused;
                pairedBefore = settings.EnablePairedActivation; movementBefore = settings.EnableUnsafeMovementExperiment;
                chargeBefore = settings.EnableMountedCharge; captured = true;
                Require(world.Primary.Combat.TryConfigurePairedActivation(true), "Paired architecture admission refused.");
                settings.EnableUnsafeMovementExperiment = true;
                // Enable the accepted optional RT feature before combat so TB refusal
                // must come from the unsupported-mode boundary rather than default-off.
                settings.EnableMountedCharge = !RealtimeScenario || SelectedScenario == RealtimeCharge;
                MovementScreenshotCaptureCoordinator.AcquireClosedUmmLeaseRetained(out umm);
                mode = new NativeModeTransitionProbe(false); mode.DispatchTemporaryValueIfRequired();
                evidence["inputs"] = inputs; evidence["activations"] = activations; evidence["turnVisits"] = visits; evidence["snapshots"] = snapshots;
                evidence["scenario"] = request.Scenario; evidence["selectedCase"] = request.OwnedPairs?.Case;
                evidence["workingSha256"] = inherited == null ? Chunk7WorldScenarioEngine.WorkingHash : null;
                evidence["stimulusKinds"] = new JArray("native-selected-ability-handler", "native-ground-input", "Game.PauseBind",
                    "native-StartRound-postfix-repeated-cleanup", "native-LeaveCombat-RemoveUnit", "one-owned-cleanup-subscriber-fault");
                Pass("C7-combat-fixture"); Next(1);
            }
            catch (Exception error) { Fail("C7-combat-fixture", error.ToString()); BeginCleanup(); }
        }

        internal void Update()
        {
            if (!started || completed || disposed) return;
            try
            {
                if (cleanupStarted) { PollCleanup(); return; }
                nativeScope.RequireCurrent();
                if (checkpointReady) return;
                if (!(clock.Elapsed.TotalSeconds <= 420 && clock.Elapsed.TotalSeconds - stepStarted <= 60))
                    throw new InvalidOperationException("Bounded combat scenario deadline at step " + step + ", action " + actionStage + "." + PrincipalBlockers());
                if (RegisteredDisableCase && step == 75) { PollRegisteredDisabledFrame(); return; }
                if (Game.Instance.IsPaused) Game.Instance.IsPaused = false;
                if (targetService != null && targetService.State == DiagnosticCombatTargetState.Active)
                    Require(targetService.RefreshBidirectionalCombatMemoryLease(), "Exact diagnostic combat memory or AI lease changed: " + (targetService.LastCombatMemoryLeaseFailure ?? "memory not mutual") + ".");
                ObserveTurn();
                switch (step)
                {
                    case 1:
                        if (CombatController.IsInTurnBasedCombat()) return;
                        if (inherited != null) { inherited.RequirePreserved(); Pass("C7-owned-pairs-inherited-custody"); Next(2); break; }
                        var acquisition = new JObject(); evidence["acquisition"] = acquisition;
                        bool acquired;
                        try { acquired = NativeHorseFixtureAcquisition.Create(br, horses, logger, acquisition); }
                        finally { horseFeature = br.Descriptor.GetFact(horses.HorseFeature) as Feature; rankFeature = br.Descriptor.GetFact(horses.LevelRank) as Feature; bm = br.Descriptor.Pet; nativeScope.Track(bm); }
                        Require(acquired, "Exact native Ranger option acquisition failed."); Next(2); break;
                    case 2:
                        if (OwnerSeedDerived)
                        {
                            // Both pairs were restored mounted by the cold load: verify them and
                            // skip the native Mount steps for these cases only.
                            if (!MountedStartReady()) return;
                            AdmitMountedStart(); Pass("C7-combat-restored-mounts");
                            if (RegisteredDisableCase) ExercisePermanentHorseRemovalRefusal();
                            // Attack modes wait (bounded, native) for the save's Rapid Shot to stop before the encounter.
                            if (AttackModesCase) { PrepareAttackModes(); Next(79); break; }
                            if (ReactionCase) { PrepareEncounter(); Next(5); break; }
                            if (StagedCase) { PrepareStaged(); PrepareEncounter(); Next(5); break; }
                            PrepareEncounter(); Next(RealtimeScenario ? 60 : 5); break;
                        }
                        if (!Chunk7NativeFixture.Idle(bm)) return;
                        actors = new[] { ar, am, br, bm };
                        evidence["actorIds"] = new JArray(actors.Select(x => x.UniqueId));
                        BeginRelationship(ar, am, controls.MountAbility, br, "mount-a"); Next(3); break;
                    case 3:
                        if (world.Registry.FindActive(ar) == null || !RelationshipSettled()) return;
                        a = world.ForActor(ar); BeginRelationship(br, bm, controls.MountAbility, ar, "mount-b"); Next(4); break;
                    case 4:
                        if (world.Registry.FindActive(br) == null || !RelationshipSettled()) return;
                        b = world.ForActor(br);
                        Require(a != null && b != null && a != b && world.Registry.ActiveCount == 2, "Two native pair owners were not independently committed.");
                        Pass("C7-combat-native-mounts"); PrepareEncounter(); Next(RealtimeScenario ? 60 : 5); break;
                    case 5:
                        if (!CombatController.IsInTurnBasedCombat() || !Game.Instance.Player.IsInCombat || Game.Instance.TurnBasedCombatController.CurrentTurn == null) return;
                        VerifyInitiative(); Pass("C7-combat-native-orders");
                        if (SelectedScenario == NativeLifeCleanup) { BeginNativeLife(); Next(70); }
                        else if (AttackModesCase) { BeginAttackModes(); Next(80); }
                        else if (ReactionCase) { BeginReaction(); Next(85); }
                        else if (StagedCase) { BeginStaged(); Next(90); }
                        else Next(checkpointOnly && checkpointKind == Chunk7CombatCheckpointKind.MountedPendingOnly ? 40 : 10); break;
                    case 10: TickFullActivations(); break;
                    case 50: TickRealtimeCheckpoint(); break;
                    case 60: TickRealtime(); break;
                    case 70: TickNativeLife(); break;
                    case 79: if (!AttackModesRapidSettled()) return; PrepareEncounter(); Next(5); break;
                    case 80: TickAttackModes(); break;
                    case 85: TickReaction(); break;
                    case 90: TickStaged(); break;
                    case 20: TickRefresh(); break;
                    case 30:
                        if (!ReadyPrincipal(a)) { EndNative(Game.Instance.TurnBasedCombatController.CurrentTurn); return; }
                        splitRound = Game.Instance.TurnBasedCombatController.RoundNumber;
                        r1 = new JObject { ["splitRound"] = splitRound, ["beforeDismount"] = WorldSnapshot("r1-before-dismount") };
                        evidence["r1"] = r1;
                        BeginRelationship(ar, ar, controls.DismountAbility, br, "r1-native-dismount-a"); Next(31); break;
                    case 31:
                        if (a.Relationship.State != RelationshipState.Unmounted || !RelationshipSettled()) return;
                        Require(a.Relationship.PairOwner != null && SplitReleaseRound(a) == splitRound,
                            "Native Dismount did not retain the original round exclusion.");
                        r1["afterDismount"] = WorldSnapshot("r1-after-dismount"); preparesAtSplit = trace.GrantCount(am);
                        rounds.AfterNativeRound = ApplyRoundOrderedCleanup; Next(32); break;
                    case 32:
                        var independent = Game.Instance.TurnBasedCombatController.CurrentTurn;
                        if (r1Injected && independent?.Unit == am && Chunk7NativeFixture.ReadyForTurnInput(independent))
                        {
                            Require(Game.Instance.TurnBasedCombatController.RoundNumber == splitRound + 1 && trace.GrantCount(am) == preparesAtSplit + 1,
                                "Former mount did not receive exactly one independent native grant in N+1.");
                            r1["independentMountTurn"] = trace.Snapshot(am); r1["independentRound"] = Game.Instance.TurnBasedCombatController.RoundNumber;
                            Pass("C7-combat-r1-round-release");
                            // Owner-seed-derived (Directive 4 R3): the independent Horse spends its own move,
                            // then rider A mounts as the first action of its next prepared turn.
                            if (OwnerSeedDerived) { BeginHorseIndependentMove(independent); Next(33); break; }
                            Next(40);
                        }
                        EndNative(independent); break;
                    case 33:
                        if (!FinishHorseIndependentMove()) return;
                        EndNative(horseIndependentTurn); Next(35); break;
                    case 35:
                        var mountTurn = Game.Instance.TurnBasedCombatController.CurrentTurn;
                        if (!FirstActionMountReady(mountTurn)) { if (mountTurn?.Unit != ar) EndNative(mountTurn); return; }
                        BeginFirstActionMount(mountTurn); Next(36); break;
                    case 36:
                        if (!FinishFirstActionMount()) return;
                        Pass("C7-combat-first-action-mount");
                        EndNative(firstActionTurn); Next(40); break;
                    case 40:
                        var pendingPair = checkpointOnly ? a : b;
                        if (!ReadyPrincipal(pendingPair)) { EndNative(Game.Instance.TurnBasedCombatController.CurrentTurn); return; }
                        actingPair = pendingPair; actingIndex = checkpointOnly ? 0 : 1; actingTurn = Game.Instance.TurnBasedCombatController.CurrentTurn;
                        BeginAttack(true, "pending-survivor-mount-expenditure"); Next(41); break;
                    case 41:
                        if (!FinishAttack(true)) return;
                        ExerciseNativePendingSurvivor();
                        if (checkpointOnly) { MarkCheckpointReady(); break; }
                        ExerciseLocalCleanupFailure(); BeginCleanup(); break;
                }
            }
            catch (Exception error) { Fail("C7-combat-runtime", "Step " + step + ", action " + actionStage + ": " + error); BeginCleanup(); }
        }

        private void PrepareEncounter()
        {
            Require(actors.All(Chunk7NativeFixture.Idle), "Initiative fixture requires the entire captured party idle before combat.");
            var targetPosition = am.Position + Vector3.forward * (SelectedScenario == RealtimeCharge || StagedCase ? 8f : 3.5f);
            // Fail closed before any fixture mutation: the authorization basis and the native
            // placement (bounded, walkable) are proven before initiative, leases or observers.
            var targetBasis = AdmitTargetAuthorization(targetPosition);
            foreach (var actor in actors) initiativeBefore.Add(actor, actor.Stats.Initiative.BaseValue);
            if (!RealtimeMode)
            {
            var riderFirst = checkpointOnly || SelectedScenario == RiderFirst || SelectedScenario == NativeLifeCleanup;
            ar.Stats.Initiative.BaseValue = riderFirst ? 80 : -80; am.Stats.Initiative.BaseValue = riderFirst ? -80 : 80;
            br.Stats.Initiative.BaseValue = riderFirst ? 40 : -40; bm.Stats.Initiative.BaseValue = riderFirst ? -40 : 40;
            }
            evidence["initiativeInputs"] = new JArray(actors.Select(actor => new JObject { ["actorId"] = actor.UniqueId,
                ["before"] = initiativeBefore[actor], ["input"] = actor.Stats.Initiative.BaseValue, ["modified"] = actor.Stats.Initiative.ModifiedValue,
                ["outsideCombat"] = !actor.IsInCombat }));
            var ai = typeof(UnitEntityData).GetField("m_AiEnabled", Flags);
            Require(ai != null && ai.FieldType == typeof(bool), "Exact native AI observation contract differs.");
            firstPairAi = new ScopedDiagnosticAiLease<UnitEntityData>(u => u.UniqueId,
                u => (ReferenceEquals(u, ar) || ReferenceEquals(u, am) || ReferenceEquals(u, br)) && u.IsInState && u.Group == ar.Group,
                u => u.Commands.Empty, u => (bool)ai.GetValue(u), u => u.IsAIEnabled, (u, value) => u.IsAIEnabled = value);
            // Both measured pairs: pair B is excluded from the non-pair lease, so its rider's AI is
            // suppressed here (lease-scope-review01 B1). Only rider B: KMC owns the mounted Horse's AI and
            // restores it at a mid-run dismount (lease-scope-review02 B2). No per-tick empty-queue check.
            firstPairAi.Acquire(new[] { ar, am, br });
            mode?.Dispose(); mode = new NativeModeTransitionProbe(!RealtimeMode);
            mode.DispatchTemporaryValueIfRequired();
            Require(mode.TemporaryValueIsCurrent && !Game.Instance.Player.IsInCombat, "Selected mode must be set before native encounter setup.");
            trace = new NativeActorAllocationTrace(ar, am, a.Combat, deferInstallation: true) { ObserveReactionResources = false };
            trace.Install();
            completion = new NativeTurnCompletionProbe(trace, world); completion.Install();
            rounds = new NativeRoundBoundaryProbe(); rounds.Install();
            // This campaign selects exact command/Prepare/resource boundaries. Per-frame
            // reaction cooldown tracing belongs to the separate reaction campaign.
            evidence["reactionTickCampaignSelected"] = false;
            trace.BeginEncounter(request.RunId + ":two-pair-combat");
            targetService = new DiagnosticCombatTargetService(logger, true);
            target = targetService.Spawn(ar, am, targetPosition, request.RunId + ":two-pair-target", targetBasis != null, !checkpointOnly, checkpointOnly, new[] { br, bm });
            nativeScope.Track(target);
            Require(targetService.PrepareForPlayerClick(target) && targetService.QueueBidirectionalCombatMemory(ar, target),
                "Native diagnostic target placement/visibility/combat memory was refused.");
            evidence["targetId"] = target.UniqueId; evidence["targetGroupId"] = target.Group.Id;
            if (RealtimeScenario) BeginRealtimeObservations();
        }
        private void VerifyInitiative()
        {
            var order = Game.Instance.TurnBasedCombatController.SortedUnits.ToArray();
            evidence["nativeInitialOrder"] = new JArray(order.Select(u => u.UniqueId));
            evidence["nativeInitiatives"] = new JArray(actors.Select(u => new JObject { ["actorId"] = u.UniqueId, ["value"] = u.CombatState.Initiative }));
            foreach (var pair in new[] { a, b }.Where(p => p != null))
            {
                var ri = Array.IndexOf(order, pair.Relationship.Rider); var mi = Array.IndexOf(order, pair.Relationship.Mount);
                Require(ri >= 0 && mi >= 0 && ((ri < mi) == (checkpointOnly || SelectedScenario == RiderFirst || SelectedScenario == NativeLifeCleanup)), "Actual native roll order does not match the selected fixture arrangement.");
            }
            Require(order.Any(u => !actors.Contains(u)), "No unrelated native roster actor is present.");
        }
        private void TickFullActivations()
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            if (actionStage == 4)
            {
                if (ReferenceEquals(turn, actingTurn)) { EndNative(turn); return; }
                activation["afterEnd"] = WorldSnapshot("full-activation-ended");
                var calls = (JArray)completion.Capture()["calls"];
                Require(checkpointOnly && checkpointKind == Chunk7CombatCheckpointKind.ZeroMountedRetained ||
                    (actingIndex == 0 ? new[] { ar, am } : new[] { br, bm }).All(actor => calls.Any(c =>
                    (string)c["kind"] == "end" && (string)c["before"]?["actorId"] == actor.UniqueId && (bool?)c["complete"] == true)),
                    "Both exact native actor contexts must complete their full activation.");
                completedPairs.Add(actingIndex); Pass("C7-combat-pair-" + (actingIndex == 0 ? "a" : "b") + "-full-activation");
                actionStage = 0;
                if (completedPairs.Count == 2) { Require(!checkpointOnly, "Checkpoint escaped its selected native boundary."); Next(20); return; }
            }
            if (actionStage == 1)
            { if (!FinishMove()) return; BeginAttack(false, "rider-primary"); actionStage = 2; return; }
            if (actionStage == 2)
            {
                if (!FinishAttack(false)) return;
                if (checkpointOnly && checkpointKind == Chunk7CombatCheckpointKind.TurnBasedDebts && actingIndex == 1)
                { Require(completedPairs.Contains(0), "Second-pair checkpoint preceded the completed first pair."); MarkCheckpointReady(); return; }
                BeginAttack(true, "mount-primary"); actionStage = 3; return;
            }
            if (actionStage == 3)
            {
                if (!FinishAttack(true)) return;
                if (RegisteredDisableCase && actingIndex == 1)
                {
                    ExerciseNativePendingSurvivor();
                    // Native TB refuses Pause mode. The registered callback and its
                    // debt snapshots run synchronously in this quiescent TB boundary.
                    BeginRegisteredDisableFailure(); Next(75); return;
                }
                if (checkpointOnly && checkpointKind == Chunk7CombatCheckpointKind.ZeroMountedRetained)
                {
                    splitRound = Game.Instance.TurnBasedCombatController.RoundNumber;
                    BeginRelationship(actingIndex == 0 ? ar : br, actingIndex == 0 ? ar : br, controls.DismountAbility,
                        actingIndex == 0 ? br : ar, "checkpoint-native-dismount-" + actingIndex); actionStage = 6; return;
                }
                if (!checkpointOnly) ProveChargeRefusal();
                if (EndNative(turn)) actionStage = 4; return;
            }
            if (actionStage == 6)
            {
                if (actingPair.Relationship.State != RelationshipState.Unmounted || !RelationshipSettled()) return;
                Require(actingPair.Relationship.PairOwner != null && SplitReleaseRound(actingPair) == splitRound,
                    "Native Dismount did not retain the selected original round.");
                if (actingIndex == 1) { MarkCheckpointReady(); return; }
                if (EndNative(turn)) actionStage = 4; return;
            }
            var index = turn?.Unit == ar ? 0 : turn?.Unit == br ? 1 : -1;
            if (index < 0 || completedPairs.Contains(index)) { EndNative(turn); return; }
            var pair = index == 0 ? a : b;
            if (!ReadyPrincipal(pair)) return;
            if (Game.Instance.TurnBasedCombatController.IsSurprised(turn.Unit)) { EndNative(turn); return; }
            actingPair = pair; actingIndex = index; actingTurn = turn;
            Require(pair.Participant.PartnerContext != null, "Native principal has no exact partner context.");
            firstSequences[index] = pair.Combat.PairedActivationSequence;
            firstRiderPrepares[index] = trace.GrantCount(pair.Relationship.Rider); firstMountPrepares[index] = trace.GrantCount(pair.Relationship.Mount);
            activation = new JObject { ["pair"] = index, ["sequence"] = firstSequences[index], ["before"] = WorldSnapshot("activation-start") };
            activations.Add(activation); BeginMove(); actionStage = 1;
        }
        private void TickRefresh()
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            var index = turn?.Unit == ar ? 0 : turn?.Unit == br ? 1 : -1;
            if (index < 0) { EndNative(turn); return; }
            var pair = index == 0 ? a : b;
            if (!ReadyPrincipal(pair)) return;
            if (!refreshedPairs.Contains(index))
            {
                Require(pair.Combat.PairedActivationSequence == firstSequences[index] + 1 &&
                    trace.GrantCount(pair.Relationship.Rider) == firstRiderPrepares[index] + 1 &&
                    trace.GrantCount(pair.Relationship.Mount) == firstMountPrepares[index] + 1 &&
                    pair.Relationship.Rider.CombatState.Cooldown.StandardAction == 0f && pair.Relationship.Rider.CombatState.Cooldown.MoveAction == 0f &&
                    pair.Relationship.Mount.CombatState.Cooldown.StandardAction == 0f && pair.Relationship.Mount.CombatState.Cooldown.MoveAction == 0f,
                    "Next native activation did not grant each actor exactly once with native renewal.");
                snapshots.Add(WorldSnapshot("pair-" + index + "-native-refresh")); refreshedPairs.Add(index);
            }
            EndNative(turn);
            if (refreshedPairs.Count == 2) { Pass("C7-combat-refresh"); Next(30); }
        }
        private bool ReadyPrincipal(MountedPairServices pair)
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            return turn?.Unit == pair.Relationship.Rider && Chunk7NativeFixture.ReadyForTurnInput(turn) && pair.Combat.PairedPartnerContext != null &&
                pair.Relationship.Rider.Commands.Empty && pair.Relationship.Mount.Commands.Empty &&
                !pair.Combat.HasActiveCommand && !pair.Combat.HasActiveGroundMovement && !pair.Relationship.Rider.AreHandsBusyWithAnimation &&
                !pair.Relationship.Mount.AreHandsBusyWithAnimation;
        }
        // Names each ReadyPrincipal term for the pair whose rider owns the current native turn.
        private string PrincipalBlockers()
        {
            var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
            var pair = turn?.Unit == null ? null : turn.Unit == a?.Relationship.Rider ? a : turn.Unit == b?.Relationship.Rider ? b : null;
            if (pair == null) return " Current turn: " + (turn?.Unit?.UniqueId ?? "none") + " (" + (turn == null ? "none" : turn.Status.ToString()) + "), not a pair rider.";
            var rider = pair.Relationship.Rider; var mount = pair.Relationship.Mount;
            var blockers = new List<string>();
            if (!Chunk7NativeFixture.ReadyForTurnInput(turn)) blockers.Add("turn not ready for input (" + turn.Status + ")");
            if (pair.Combat.PairedPartnerContext == null) blockers.Add("no paired partner context");
            if (!rider.Commands.Empty) blockers.Add("rider commands: " + string.Join(",", rider.Commands.Raw.Where(c => c != null).Select(c => c.GetType().Name)));
            if (!mount.Commands.Empty) blockers.Add("mount commands: " + string.Join(",", mount.Commands.Raw.Where(c => c != null).Select(c => c.GetType().Name)));
            if (pair.Combat.HasActiveCommand) blockers.Add("active pair command");
            if (pair.Combat.HasActiveGroundMovement) blockers.Add("active ground movement");
            if (rider.AreHandsBusyWithAnimation) blockers.Add("rider hands busy");
            if (mount.AreHandsBusyWithAnimation) blockers.Add("mount hands busy");
            return " ReadyPrincipal(" + rider.UniqueId + ") blockers: " + (blockers.Count == 0 ? "none" : string.Join("; ", blockers)) +
                ". Activation sequence " + pair.Combat.PairedActivationSequence + ", identity " + (pair.Combat.PairedActivationIdentity ?? "none") +
                ", mount commands empty " + mount.Commands.Empty + ".";
        }
        private void ObserveTurn()
        {
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            if (trace == null || turn == null || !visited.Add(turn)) return;
            Require(visits.Count < 96, "Native turn observation bound exceeded.");
            visits.Add(new JObject { ["actorId"] = turn.Unit.UniqueId, ["round"] = controller.RoundNumber,
                ["context"] = trace.ObjectIdentity(turn), ["state"] = trace.Snapshot(turn.Unit) });
            if (world.Registry.FindActive(turn.Unit)?.Mount == turn.Unit)
                throw new InvalidOperationException("A mounted partner received an independent native roster turn.");
        }
        private bool EndNative(TurnController turn)
        {
            if (turn?.Unit == null || ReferenceEquals(turn, endedTurn) || !(actors.Contains(turn.Unit) || inherited != null && originalParty.Contains(turn.Unit)) ||
                !turn.Unit.IsDirectlyControllable || !turn.Unit.Commands.Empty || turn.Unit.AreHandsBusyWithAnimation ||
                !turn.CanEndTurnAndNoActing() || Game.Instance.TurnBasedCombatController.WaitingForUI ||
                MountedPairTurnParticipant.NativeNextCandidate(Game.Instance.TurnBasedCombatController) != null ||
                world.Pairs.Any(p => p.Combat.HasActiveCommand || p.Combat.HasActiveGroundMovement)) return false;
            trace.Record("chunk7-end-input-before", turn.Unit);
            Game.Instance.PauseBind(); trace.Record("chunk7-end-input-after", turn.Unit); endedTurn = turn;
            return true;
        }
        private void ApplyRoundOrderedCleanup(CombatController controller)
        {
            nativeScope.RequireCurrent();
            if (r1Injected || controller.RoundNumber <= splitRound) return;
            Require(controller.RoundNumber == splitRound + 1 && a.Relationship.PairOwner != null,
                "R1 callback missed the retained native N+1 pre-selection boundary.");
            var before = WorldSnapshot("r1-before-duplicate-cleanup"); var oldRound = SplitReleaseRound(a);
            var neighbourOwner = b.Relationship.PairOwner; var neighbourIdentity = b.Combat.PairedActivationIdentity;
            var result = a.Relationship.Dismount(CleanupTrigger.Incapacitated);
            var after = WorldSnapshot("r1-after-duplicate-cleanup");
            r1["stimulus"] = "diagnostic cleanup callback at actual native StartRound return; no health/round/resource mutation";
            r1["callbackRound"] = controller.RoundNumber; r1["releaseBefore"] = oldRound; r1["releaseAfter"] = SplitReleaseRound(a);
            r1["duplicateBefore"] = before; r1["duplicateAfter"] = after;
            Require(result.Succeeded && oldRound == splitRound && SplitReleaseRound(a) == splitRound &&
                ReferenceEquals(neighbourOwner, b.Relationship.PairOwner) && neighbourIdentity == b.Combat.PairedActivationIdentity &&
                SameBudgets(before, after), "Repeated detached cleanup reset a release round, altered neighbour ownership or native expenditure.");
            r1Injected = true; rounds.AfterNativeRound = null;
        }
        private static int SplitReleaseRound(MountedPairServices pair) =>
            (int)typeof(MountedPairTurnParticipant).GetField("splitReleaseRound", Flags).GetValue(pair.Participant);
        private void Next(int value) { step = value; stepStarted = clock.Elapsed.TotalSeconds; }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private void Pass(string name)
        {
            var row = results.SingleOrDefault(item => item.Name == name);
            if (row == null) { row = new RuntimeSubscenarioResult { Name = name, Status = "PASS", Errors = new string[0] }; results.Add(row); }
            row.AssertionPassCount++;
        }
        private void Fail(string name, string error)
        {
            if (!errors.Contains(error)) errors.Add(error);
            var row = results.SingleOrDefault(item => item.Name == name);
            if (row == null) { row = new RuntimeSubscenarioResult { Name = name, Status = "FAIL", Errors = new string[0] }; results.Add(row); }
            row.Status = "FAIL";
            if (!row.Errors.Contains(error)) { row.Errors = row.Errors.Concat(new[] { error }).ToArray(); row.AssertionFailCount++; }
        }
    }
}
