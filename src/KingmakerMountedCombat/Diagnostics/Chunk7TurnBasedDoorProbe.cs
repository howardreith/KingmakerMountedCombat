using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View.MapObjects;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using Pathfinding;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 2 (DOOR-01 / SPACE-01 in native turn-based combat): one supported door exercised through actual native
    // turn-based combat on the owner-seed exploration fixture, after the real-time door probe has released the door.
    // The pair is placed a bounded native ground move from the door, the project's diagnostic target starts a native
    // encounter in the selected turn-based mode, and on the rider's own acting turn the door is clicked through the
    // ordinary map-object handler. Observed: an off-turn click by the other pair is refused and mutates nothing (before
    // and during the first pair's live approach); the mount approaches only on the rider's turn; the door is used once
    // from inside its native range with line of sight; the rider's and the mount's native cooldowns are recorded before
    // and after (no refund, no second spend); a legal lack of movement opportunity is never a stall; the encounter is
    // ended by removing the diagnostic target; the door lease restores the door exactly. An unmounted party member's own
    // native click on its earlier turn, when the initiative order offers one, is the native cost control. Nothing here
    // writes resources, forces turn state or edits the door's flags.
    internal sealed class Chunk7TurnBasedDoorProbe
    {
        // v2 (review correction B): the movement cost is related to the installed paired contract (a rider's turn-based
        // approach is native movement charged to its own mount's Move budget, the rider keeps its own Move), through the
        // action-end boundary, the pair's own allocation observation and the settlement/recovery window.
        // v3 (review 2026-10-31, turn-based clock): the shared door selection required combat usability before any
        // preparation, and the outcome carries the supervision clock's operational seconds (retry cooldown, stall and
        // settlement bounds are measured on it; campaign GameTime is frozen inside an acting turn by native design).
        internal const string Contract = "owner-seed-turn-based-door-control-v3";
        internal const string MovementCostContract = "paired-approach-charged-to-own-mount-move/v1";
        internal const string OffTurnScope = "cross-pair-isolation: pair B's click during pair A's turn is refused and mutates nothing; not native proof that a rejected click preserves the same controller's own pending recovery (offline coverage)";
        internal const string SinglePairOffTurnScope = "cross-pair-isolation: not exercised in this run (single pair, no second pair); not native proof of any off-turn refusal here (offline coverage and the retained two-pair rows)";
        internal const string StallScope = "ApproachesWithoutProgress == 0 on a continuously acting turn; not a native exercise of an exhausted or off-turn movement wait (offline coverage)";
        internal const string Row = "C7-world-turn-based-door";
        private const float ApproachStandoffMetres = 5f;
        private const float TargetOffsetMetres = 3.5f;
        private const double StageSecondsLimit = 60, TotalSecondsLimit = 240;
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private readonly MountedWorldService world;
        private readonly Chunk7NativeWorldScope scope;
        private readonly RuntimeRequest request;
        private readonly IModLogger logger;
        private readonly Action<bool> selectMode;
        private readonly UnitEntityData[] riders, mounts, actors, originalParty;
        private readonly MountedPairOwner<UnitEntityData, GameMountedRelationshipService>[] owners;
        private readonly long[] generations;
        private readonly StandardDoor door;
        private readonly NavmeshCut cut;
        private readonly object doorObject, doorTransform;
        private readonly List<UnitCommand> commands = new List<UnitCommand>();
        private readonly JObject evidence = new JObject();
        private readonly JArray turns = new JArray(), endedTurns = new JArray();
        private readonly HashSet<TurnController> visited = new HashSet<TurnController>();
        private readonly Dictionary<UnitEntityData, int> initiativeBefore = new Dictionary<UnitEntityData, int>();
        private JObject setup, encounter, riderTurn, rejectionBefore, rejectionDuring, interaction, control, afterCombat, restoration;
        private bool originalOpen, originalEnabled, originalCut, originalAlreadyUnlocked, cutToggles, leaseCaptured, restored;
        private int originalRestrictionCount = -1, settledFrame = -1, setupInteractions;
        private int stage;
        private double stageStarted, started;
        private UnitMoveTo setupMove;
        private MountedDoorInteractionCommand command;
        private UnitCommand controlCommand;
        private UnitEntityData controlActor;
        private DiagnosticCombatTargetService targetService;
        private UnitEntityData target;
        private ScopedDiagnosticAiLease<UnitEntityData> pairAi;
        private bool modeSelected, initiativeApplied, aiAcquired, encounterEnded, servicesReleased, controlPending, controlPrepPending;
        private Vector3 interactionStart;
        private int movingPolls, offTurnMovingPolls, livePolls;
        private int settlementPolls, settlementMovingPolls, settlementOffTurnMovingPolls;
        private bool startedWhileMoving, turnChangedWhileLive, actionEndObserved, turnChangedDuringSettlement;
        private float maximumTimeMoved;
        private TurnController interactionTurn, endedRiderTurn;
        private int roundAtClick;

        internal bool Completed { get; private set; }
        internal JObject Evidence => evidence;
        internal UnitCommand[] IssuedCommands => commands.ToArray();

        internal Chunk7TurnBasedDoorProbe(MountedWorldService world, Chunk7NativeWorldScope scope, RuntimeRequest request, IModLogger logger,
            Action<bool> selectMode, StandardDoor door, UnitEntityData firstRider, UnitEntityData firstMount, UnitEntityData secondRider, UnitEntityData secondMount,
            UnitEntityData[] originalParty)
        {
            this.world = world; this.scope = scope; this.request = request; this.logger = logger; this.selectMode = selectMode;
            this.door = door; this.originalParty = originalParty;
            Require(door != null, "The turn-based door control requires the exact selected combat-usable door.");
            Require((secondRider == null) == (secondMount == null), "A second pair needs both its rider and its mount.");
            cut = door.GetComponentInChildren<NavmeshCut>(); doorObject = door.gameObject; doorTransform = door.transform;
            // Single-pair mode (finishing mission 2026-10-31): one existing mounted pair is sufficient for the positive native
            // cost/clock interaction; the cross-pair off-turn rows are recorded not applicable and stay offline/r14-era coverage.
            riders = secondRider == null ? new[] { firstRider } : new[] { firstRider, secondRider };
            mounts = secondMount == null ? new[] { firstMount } : new[] { firstMount, secondMount };
            actors = riders.SelectMany((r, i) => new[] { r, mounts[i] }).ToArray();
            owners = riders.Select(world.Registry.FindActive).ToArray();
            generations = owners.Select(x => x == null ? -1 : x.Generation).ToArray();
            evidence["contract"] = Contract; evidence["pairs"] = riders.Length; evidence["turns"] = turns; evidence["endedTurns"] = endedTurns; evidence["setupInteractions"] = 0;
            evidence["scope"] = new JObject { ["offTurnRejection"] = SinglePair ? SinglePairOffTurnScope : OffTurnScope, ["stallClassification"] = StallScope, ["movementCost"] = MovementCostContract };
        }

        private bool SinglePair => riders.Length == 1;
        // The actor selected after each pair input (the second pair's rider when there are two pairs; the declared other party member otherwise).
        internal UnitEntityData SelectionActor { get; set; }
        private void SelectAfterInput() => Chunk7NativeFixture.Select(SinglePair ? SelectionActor : riders[1]);
        private UnitEntityData[] AiActors => SinglePair ? new[] { riders[0], mounts[0] } : new[] { riders[0], mounts[0], riders[1] };
        private static JObject NotApplicableRejection(string label) => new JObject { ["label"] = label, ["applicable"] = false,
            ["reason"] = "single pair: no second pair exists in this run; the cross-pair off-turn refusal stays offline coverage and the retained two-pair rows (r14)" };

        private Vector3 ApproachPoint => door.MapObject != null ? door.MapObject.transform.position : door.transform.position;
        private float Radius => door.ProximityRadius > 0f ? door.ProximityRadius : 4f;
        private CombatController Controller => Game.Instance.TurnBasedCombatController;

        internal void Begin()
        {
            Require(stage == 0 && !leaseCaptured, "The turn-based door control may begin only once.");
            Require(!SinglePair || SelectionActor != null && !actors.Contains(SelectionActor), "Single-pair turn-based door control needs a declared other party member for the selection after inputs.");
            RequirePairs(); Require(actors.All(Chunk7NativeFixture.Idle), "Turn-based door control requires idle native actors.");
            RequireDoor();
            var facts = Chunk7NativeDoorProbe.DoorFacts.Capture(door, mounts);
            originalOpen = door.GetState(); originalEnabled = door.Enabled; originalCut = cut.enabled; cutToggles = door.DisableNavmeshCutWhenOpen;
            originalAlreadyUnlocked = facts.AlreadyUnlocked; originalRestrictionCount = facts.RestrictionCount; leaseCaptured = true;
            Require(facts.Eligible && (originalOpen || originalEnabled), "The released door no longer satisfies the qualifying-door predicate.");
            evidence["fixture"] = new JObject { ["name"] = door.name, ["object"] = Id(door), ["position"] = Point(door.transform.position),
                ["approachPoint"] = Point(ApproachPoint), ["radius"] = Radius, ["originalOpen"] = originalOpen, ["originalEnabled"] = originalEnabled,
                ["originalCut"] = originalCut, ["cutMode"] = cutToggles ? "toggles" : "static", ["notInCombat"] = door.NotInCombat,
                ["disableOnOpen"] = door.DisableOnOpen, ["restrictionCount"] = facts.RestrictionCount, ["originalAlreadyUnlocked"] = facts.AlreadyUnlocked };
            setup = new JObject { ["applicable"] = false }; evidence["setup"] = setup;
            control = new JObject { ["applicable"] = false, ["reason"] = "no unmounted party turn before the rider's turn offered a native control" }; evidence["control"] = control;
            started = Time.realtimeSinceStartup;
            // A closed door is already in its input state; only an open one is closed as native fixture preparation.
            if (originalOpen) CloseForInput();
            Next(1);
        }

        internal bool Poll()
        {
            if (Completed) return true;
            RequirePairs(); RequireDoor();
            Require(Time.realtimeSinceStartup - stageStarted <= StageSecondsLimit, "Turn-based door stage exceeded sixty seconds: " + stage + ". " + Blockers());
            Require(Time.realtimeSinceStartup - started <= TotalSecondsLimit, "Turn-based door control exceeded its total bound at stage " + stage + ".");
            if (targetService != null && targetService.State == DiagnosticCombatTargetState.Active && !encounterEnded)
                Require(targetService.RefreshBidirectionalCombatMemoryLease(), "Exact diagnostic combat memory or AI lease changed: " + (targetService.LastCombatMemoryLeaseFailure ?? "memory not mutual") + ".");
            ObserveTurn();
            switch (stage)
            {
                case 1:
                    if (!Settled(false)) return false;
                    BeginSetupMove(); break;
                case 2:
                    if (setupMove != null && (!setupMove.IsFinished || mounts[0].View.AgentASP.IsReallyMoving)) return false;
                    if (!actors.All(Chunk7NativeFixture.Idle)) return false;
                    if (setupMove != null) { setup["result"] = setupMove.Result.ToString(); setup["end"] = Point(mounts[0].Position); }
                    setup["distanceToApproachPointBeforeEncounter"] = Distance(mounts[0].Position, ApproachPoint);
                    Require(Distance(mounts[0].Position, door.transform.position) > Radius + 0.5f, "The first pair stands inside the door's immediate range; no turn-based approach geometry.");
                    BeginEncounter(); Next(4); break;
                case 4:
                    if (!CombatController.IsInTurnBasedCombat() || !Game.Instance.Player.IsInCombat || Controller.CurrentTurn == null) return false;
                    encounter["nativeInitialOrder"] = new JArray(Controller.SortedUnits.Select(u => u.UniqueId));
                    encounter["nativeInitiatives"] = new JArray(actors.Select(u => new JObject { ["actorId"] = u.UniqueId, ["value"] = u.CombatState.Initiative }));
                    encounter["roundAtStart"] = Controller.RoundNumber; encounter["turnBasedAtStart"] = true;
                    encounter["partyInCombat"] = new JArray(originalParty.Where(u => u.IsInCombat).Select(u => u.UniqueId));
                    Next(5); break;
                case 5:
                    TickUntilRiderTurn(); break;
                case 6:
                    if (!Chunk7NativeFixture.ReadyForTurnInput(Controller.CurrentTurn) || Controller.CurrentTurn.Unit != riders[0]) return false;
                    if (!riders[0].Commands.Empty || !mounts[0].Commands.Empty || riders[0].AreHandsBusyWithAnimation) return false;
                    BeginRiderTurn(); Next(7); break;
                case 7:
                    if (!PollInteraction()) return false;
                    Next(8); break;
                case 8:
                    if (!EndRiderTurnAndEncounter()) return false;
                    Next(9); break;
                case 9:
                    if (Game.Instance.Player.IsInCombat || Controller.Initialized || CombatController.IsInTurnBasedCombat()) return false;
                    if (actors.Any(a => a.IsInCombat)) return false;
                    ReleaseEncounterServices(); Next(10); break;
                case 10:
                    if (!actors.All(Chunk7NativeFixture.Idle)) return false;
                    afterCombat = new JObject { ["turnBased"] = CombatController.IsInTurnBasedCombat(), ["playerInCombat"] = Game.Instance.Player.IsInCombat,
                        ["controllerInitialized"] = Controller.Initialized, ["actorsIdle"] = true,
                        ["pairs"] = new JArray(riders.Select((r, i) => new JObject { ["riderId"] = r.UniqueId, ["mountId"] = mounts[i].UniqueId,
                            ["retainedPairCommands"] = world.ForActor(r).Combat.HasRetainedPairCommands, ["activeDoorInteraction"] = world.ForActor(r).Combat.HasActiveDoorInteraction,
                            ["pendingSettlement"] = world.ForActor(r).Combat.HasPendingDoorSettlement, ["mounted"] = Mounted(i) })),
                        ["cooldowns"] = Cooldowns() };
                    evidence["afterCombat"] = afterCombat;
                    Require(riders.All(r => !world.ForActor(r).Combat.HasRetainedPairCommands) && Enumerable.Range(0, riders.Length).All(Mounted),
                        "A pair retained a command or settlement after the native encounter ended.");
                    Next(11); break;
                case 11:
                    if (!Drain()) return false;
                    Completed = true; evidence["completed"] = true; return true;
                default: throw new InvalidOperationException("Unknown turn-based door stage " + stage + ".");
            }
            return false;
        }

        // The first pair's bounded ordinary ground move to a clear goal a few metres outside the door's radius, so the
        // turn-based approach has native geometry inside one movement allocation; recorded before any assertion.
        private void BeginSetupMove()
        {
            var distance = Distance(mounts[0].Position, ApproachPoint); var standoff = Radius + ApproachStandoffMetres;
            setup["startDistanceToApproachPoint"] = distance; setup["standoff"] = standoff;
            if (distance <= standoff + 1.5f) { setup["reason"] = "the first pair already stands within the standoff distance"; Next(2); return; }
            var search = new JObject(); setup["goalSearch"] = search;
            var toDoor = ApproachPoint - mounts[0].Position;
            var goal = Chunk7NativeDoorProbe.SearchClearGoalNear(mounts[0], riders[0], toDoor, distance - standoff, 0.5f, search, out var found,
                ApproachPoint, Radius, standoff + 1.5f);
            if (!found) { setup["reason"] = "no clear native goal toward the door at the standoff distance"; Next(2); return; }
            setup["applicable"] = true; setup["goal"] = Point(goal);
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(riders[0]);
            try { ClickGroundHandler.MoveSelectedUnitsToPoint(goal, false); }
            finally { setupMove = mounts[0].Commands.Move; if (setupMove != null && !commands.Contains(setupMove)) commands.Add(setupMove); }
            Require(setupMove != null && ReferenceEquals(setupMove.Executor, mounts[0]) && setupMove.CreatedByPlayer, "Native ground click did not bind the first pair's mount.");
            setup["command"] = Id(setupMove);
            SelectAfterInput(); Game.Instance.IsPaused = false;
            Next(2);
        }

        private void BeginEncounter()
        {
            Require(actors.All(Chunk7NativeFixture.Idle) && !Game.Instance.Player.IsInCombat && !door.GetState() && door.CanInteract(),
                "Turn-based encounter preconditions changed (idle pairs, exploration, closed interactable door).");
            // A door the game forbids in combat is a legitimate native refusal, recorded on the fixture row; it is never edited
            // and no encounter is started for it (no turn-based control is possible on such a door).
            Require(!door.NotInCombat, "The door forbids interaction in combat (NotInCombat): a legitimate native refusal; no turn-based door control is possible on this door.");
            // Fail closed before any fixture mutation: the authorization basis and the native placement are proven first.
            var basis = Chunk7CombatTargetAuthorization.Basis(request, Chunk7WorldScenarioEngine.WorkingHash, true);
            Require(basis == Chunk7CombatTargetAuthorization.OwnerSeedTurnBasedDoorBasis, "The diagnostic combat target is not authorized for the turn-based door stage.");
            var toDoor = (ApproachPoint - mounts[0].Position); toDoor.y = 0f; toDoor = toDoor.sqrMagnitude < 1e-4f ? Vector3.forward : toDoor.normalized;
            var lateral = Vector3.Cross(Vector3.up, toDoor);
            var candidates = new[] { mounts[0].Position + lateral * TargetOffsetMetres, mounts[0].Position - lateral * TargetOffsetMetres, mounts[0].Position - toDoor * TargetOffsetMetres };
            var placements = new JArray(); Vector3? position = null;
            foreach (var candidate in candidates)
            {
                var row = new JObject { ["requested"] = Point(candidate) }; placements.Add(row);
                try { DiagnosticCombatTargetService.ValidatePlacement(riders[0].Position, candidate); row["validated"] = true; position = candidate; break; }
                catch (InvalidOperationException error) { row["validated"] = false; row["refusal"] = error.Message; }
            }
            encounter = new JObject { ["contract"] = "owner-seed-turn-based-door-encounter-v1", ["basis"] = basis, ["ownerApproval"] = Chunk7CombatTargetAuthorization.OwnerApproval,
                ["seedSha256"] = request.OwnedPairs.SeedSha256, ["placements"] = placements, ["area"] = Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe };
            evidence["encounter"] = encounter;
            Require(position.HasValue, "Diagnostic target placement refused at every bounded candidate.");
            // Rider-first arrangement (the accepted combat cases' initiative inputs), restored at release.
            foreach (var actor in actors) initiativeBefore.Add(actor, actor.Stats.Initiative.BaseValue);
            riders[0].Stats.Initiative.BaseValue = 80; mounts[0].Stats.Initiative.BaseValue = -80;
            if (!SinglePair) { riders[1].Stats.Initiative.BaseValue = 40; mounts[1].Stats.Initiative.BaseValue = -40; }
            initiativeApplied = true;
            encounter["initiativeInputs"] = new JArray(actors.Select(actor => new JObject { ["actorId"] = actor.UniqueId, ["before"] = initiativeBefore[actor],
                ["input"] = actor.Stats.Initiative.BaseValue, ["outsideCombat"] = !actor.IsInCombat }));
            var ai = typeof(UnitEntityData).GetField("m_AiEnabled", Flags);
            Require(ai != null && ai.FieldType == typeof(bool), "Exact native AI observation contract differs.");
            var aiActors = AiActors;
            pairAi = new ScopedDiagnosticAiLease<UnitEntityData>(u => u.UniqueId,
                u => aiActors.Any(a => ReferenceEquals(a, u)) && u.IsInState && u.Group == riders[0].Group,
                u => u.Commands.Empty, u => (bool)ai.GetValue(u), u => u.IsAIEnabled, (u, value) => u.IsAIEnabled = value);
            pairAi.Acquire(aiActors); aiAcquired = true;
            selectMode(true); modeSelected = true;
            Require(!Game.Instance.Player.IsInCombat, "Selected turn-based mode must be set before native encounter setup.");
            targetService = new DiagnosticCombatTargetService(logger, true);
            target = targetService.Spawn(riders[0], mounts[0], position.Value, request.RunId + ":turn-based-door-target", true, true, false, SinglePair ? null : new[] { riders[1], mounts[1] });
            scope.Track(target);
            Require(targetService.PrepareForPlayerClick(target) && targetService.QueueBidirectionalCombatMemory(riders[0], target),
                "Native diagnostic target placement/visibility/combat memory was refused.");
            encounter["targetId"] = target.UniqueId; encounter["targetGroupId"] = target.Group.Id; encounter["targetPosition"] = Point(target.Position);
            encounter["cooldownsBeforeEncounter"] = Cooldowns();
        }

        // Turns before the first rider's: an unmounted controllable party member's own native door click is the native
        // cost control when its turn comes first; every other party turn is ended through the native End Turn binding;
        // the target's turn is the native AI's own.
        private void TickUntilRiderTurn()
        {
            var turn = Controller.CurrentTurn; if (turn?.Unit == null) return;
            // A pending native control (and its fixture re-close) completes before the rider's turn is admitted, whichever turn is current.
            if (controlPending) { if (!PollControl(turn)) return; }
            if (controlPrepPending) { if (!Settled(false)) return; controlPrepPending = false; }
            if (turn.Unit == riders[0]) { if (!Chunk7NativeFixture.ReadyForTurnInput(turn)) return; Next(6); return; }
            if (!(bool)control["applicable"] && controlActor == null && IsUnmountedControlCandidate(turn.Unit) && Chunk7NativeFixture.ReadyForTurnInput(turn) &&
                !door.GetState() && door.CanInteract() && Distance(turn.Unit.Position, ApproachPoint) <= Chunk7NativeDoorProbe.ScanRadius)
            { BeginControl(turn); return; }
            if (IsPartyMember(turn.Unit)) TryEndTurn(turn, "before-rider-turn");
        }

        private bool IsPartyMember(UnitEntityData unit) => unit != null && (actors.Contains(unit) || originalParty.Contains(unit)) && unit.IsDirectlyControllable;
        private bool IsUnmountedControlCandidate(UnitEntityData unit) => unit != null && originalParty.Contains(unit) && !actors.Contains(unit) && unit.IsDirectlyControllable &&
            !unit.Descriptor.IsPet && unit.View?.AgentASP != null && world.Registry.FindActive(unit) == null && unit.Commands.Empty && !unit.AreHandsBusyWithAnimation;

        private void BeginControl(TurnController turn)
        {
            controlActor = turn.Unit;
            var before = Cooldowns(controlActor);
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(controlActor);
            var start = controlActor.Position; var accepted = false;
            try { accepted = new ClickMapObjectHandler().OnClick(door.gameObject, door.transform.position, 0, false, false); }
            finally
            {
                controlCommand = controlActor.Commands.Standard;
                if (controlCommand != null && !commands.Contains(controlCommand)) commands.Add(controlCommand);
            }
            control["actorId"] = controlActor.UniqueId; control["accepted"] = accepted; control["commandType"] = controlCommand?.GetType().FullName;
            control["commandObject"] = Id(controlCommand); control["routedByKmc"] = controlCommand is MountedDoorInteractionCommand;
            control["distanceBefore"] = Distance(start, ApproachPoint); control["round"] = Controller.RoundNumber; control["turn"] = Id(turn);
            control["cooldownsBefore"] = before; control["timeMovedBefore"] = turn.TimeMoved;
            SelectAfterInput(); Game.Instance.IsPaused = false;
            if (!accepted || !(controlCommand is UnitInteractWithObject) || controlCommand is MountedDoorInteractionCommand || !ReferenceEquals(controlCommand.Executor, controlActor))
            { control["reason"] = "the native click did not admit a stock interaction command on the unmounted member in turn-based combat"; return; }
            control["applicable"] = true; control.Remove("reason"); controlPending = true;
        }

        private bool PollControl(TurnController turn)
        {
            if (turn.Unit == controlActor) control["timeMoved"] = Math.Max((double)(control["timeMoved"] ?? 0d), turn.TimeMoved);
            if (!controlCommand.IsFinished || controlActor.View.AgentASP.IsReallyMoving || !controlActor.Commands.Empty) return false;
            if (!Settled(door.GetState())) return false;
            control["result"] = controlCommand.Result.ToString(); control["doorOpenAfter"] = door.GetState(); control["cooldownsAfter"] = Cooldowns(controlActor);
            control["distanceAfter"] = Distance(controlActor.Position, ApproachPoint); control["turnStillControl"] = Controller.CurrentTurn?.Unit == controlActor;
            controlPending = false;
            RequirePairs(); Require(actors.All(a => a.Commands.Empty), "The native control's click moved a pair actor.");
            // Native fixture preparation only: the door returns to its closed input state for the rider's click.
            if (door.GetState()) { door.Open(); setupInteractions++; evidence["setupInteractions"] = setupInteractions; settledFrame = -1; controlPrepPending = true; }
            return true;
        }

        private void BeginRiderTurn()
        {
            var turn = Controller.CurrentTurn; interactionTurn = turn; roundAtClick = Controller.RoundNumber;
            Require(turn.Unit == riders[0] && !door.GetState() && door.CanInteract(), "Rider turn preconditions changed.");
            Require(Distance(mounts[0].Position, door.transform.position) > Radius + 0.5f, "Mounted door input must start outside native immediate interaction range.");
            riderTurn = new JObject { ["round"] = roundAtClick, ["turn"] = Id(turn), ["status"] = turn.Status.ToString(), ["acting"] = turn.IsActing,
                ["timeMoved"] = turn.TimeMoved, ["remainingTime"] = turn.GetRemainingTime(), ["cooldowns"] = Cooldowns(), ["mountPosition"] = Point(mounts[0].Position),
                ["distanceToApproachPoint"] = Distance(mounts[0].Position, ApproachPoint), ["surprised"] = Controller.IsSurprised(riders[0]) };
            evidence["riderTurn"] = riderTurn;
            // Off-turn input first: the second pair's rider clicks the door during the first rider's turn (two pairs only).
            rejectionBefore = SinglePair ? NotApplicableRejection("before-first-pair-click") : ClickOffTurn("before-first-pair-click"); evidence["rejectionBeforeClick"] = rejectionBefore;
            Require(Controller.CurrentTurn == turn && turn.Unit == riders[0], "The refused off-turn click changed the native turn.");
            // The first pair's own click on its acting turn.
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(riders[0]);
            interactionStart = mounts[0].Position; var accepted = false;
            try { accepted = new ClickMapObjectHandler().OnClick(door.gameObject, door.transform.position, 0, false, false); }
            finally
            {
                foreach (var native in riders[0].Commands.Raw.OfType<MountedDoorInteractionCommand>().Where(x => ReferenceEquals(x.Door, door)))
                    if (!commands.Contains(native)) commands.Add(native);
            }
            command = riders[0].Commands.Raw.OfType<MountedDoorInteractionCommand>().SingleOrDefault(x => ReferenceEquals(x.Door, door));
            var pair = world.ForActor(riders[0]).Combat;
            interaction = new JObject { ["riderId"] = riders[0].UniqueId, ["mountId"] = mounts[0].UniqueId, ["ownerId"] = owners[0].Id.ToString(), ["generation"] = generations[0],
                ["accepted"] = accepted, ["commandObject"] = Id(command), ["executorId"] = command?.Executor?.UniqueId, ["round"] = roundAtClick, ["turn"] = Id(turn),
                ["distanceBefore"] = Distance(interactionStart, door.transform.position), ["radius"] = Radius, ["cooldownsAtClick"] = Cooldowns(),
                ["timeMovedAtClick"] = turn.TimeMoved, ["selectionAfterInput"] = (SinglePair ? SelectionActor : riders[1]).UniqueId, ["feedback"] = pair.LastFeedback,
                ["rejectionCodes"] = new JArray(pair.LastRejectionCodes.Select(c => c.ToString())) };
            evidence["interaction"] = interaction;
            Require(accepted && command != null && ReferenceEquals(command.Rider, riders[0]) && ReferenceEquals(command.Mount, mounts[0]) &&
                ReferenceEquals(command.Executor, riders[0]) && pair.HasActiveDoorInteraction,
                "The rider's own-turn click did not admit exactly the pair's door command: " + pair.LastFeedback);
            movingPolls = 0; offTurnMovingPolls = 0; livePolls = 0; startedWhileMoving = false; turnChangedWhileLive = false; maximumTimeMoved = turn.TimeMoved;
            settlementPolls = 0; settlementMovingPolls = 0; settlementOffTurnMovingPolls = 0; actionEndObserved = false; turnChangedDuringSettlement = false;
            SelectAfterInput(); Game.Instance.IsPaused = false;
        }

        // The second pair's rider clicks the door while the first rider owns the turn: the ordinary handler is used, and
        // nothing may change (no command on either second-pair actor, no door interaction, same owners and generations,
        // and when the first pair's approach is live, the same command object and delegated move).
        private JObject ClickOffTurn(string label)
        {
            var pairB = world.ForActor(riders[1]).Combat; var pairA = world.ForActor(riders[0]).Combat;
            var beforeB = new[] { riders[1], mounts[1] }.Select(a => CommandIdentities(a)).ToArray();
            var liveBefore = command != null && !command.IsFinished; var moveBefore = command?.DelegatedMove;
            var retainedBefore = pairB.DoorInteractionRetainedClickCount; var turnBefore = Controller.CurrentTurn;
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(riders[1]);
            var accepted = false;
            try { accepted = new ClickMapObjectHandler().OnClick(door.gameObject, door.transform.position, 0, false, false); }
            finally
            {
                foreach (var native in riders[1].Commands.Raw.OfType<MountedDoorInteractionCommand>())
                    if (!commands.Contains(native)) commands.Add(native);
            }
            var afterB = new[] { riders[1], mounts[1] }.Select(a => CommandIdentities(a)).ToArray();
            var created = riders[1].Commands.Raw.OfType<MountedDoorInteractionCommand>().Any(x => ReferenceEquals(x.Door, door));
            var row = new JObject { ["label"] = label, ["riderId"] = riders[1].UniqueId, ["mountId"] = mounts[1].UniqueId, ["accepted"] = accepted,
                ["doorCommandCreated"] = created, ["secondPairDoorInteraction"] = pairB.HasActiveDoorInteraction,
                ["feedback"] = pairB.LastFeedback, ["rejectionCodes"] = new JArray(pairB.LastRejectionCodes.Select(c => c.ToString())),
                ["kmcWrongTurnObserved"] = pairB.LastRejectionCodes.Contains(MountedCombatRejectionCode.WrongTurn),
                ["retainedClickCountBefore"] = retainedBefore, ["retainedClickCountAfter"] = pairB.DoorInteractionRetainedClickCount,
                ["commandsBefore"] = new JArray(beforeB), ["commandsAfter"] = new JArray(afterB),
                ["turnUnchanged"] = ReferenceEquals(Controller.CurrentTurn, turnBefore) && Controller.CurrentTurn?.Unit == riders[0],
                ["firstPairLiveBefore"] = liveBefore,
                ["firstPairCommandIntact"] = !liveBefore || command != null && !command.IsFinished && pairA.HasActiveDoorInteraction &&
                    ReferenceEquals(riders[0].Commands.Raw.OfType<MountedDoorInteractionCommand>().SingleOrDefault(x => ReferenceEquals(x.Door, door)), command) &&
                    ReferenceEquals(command.DelegatedMove, moveBefore) };
            Chunk7NativeFixture.Select(riders[1]); Game.Instance.IsPaused = false;
            RequirePairs();
            Require(!created && !pairB.HasActiveDoorInteraction && JToken.DeepEquals(new JArray(beforeB), new JArray(afterB)) &&
                riders[1].Commands.Empty && mounts[1].Commands.Empty && (bool)row["turnUnchanged"] && (bool)row["firstPairCommandIntact"],
                "The refused off-turn click mutated the second pair, the turn or the first pair's live operation: " + row.ToString(Newtonsoft.Json.Formatting.None));
            return row;
        }

        private static JObject CommandIdentities(UnitEntityData actor) => new JObject { ["id"] = actor.UniqueId,
            ["raw"] = new JArray(actor.Commands.Raw.Select(c => c == null ? 0 : Id(c))), ["queue"] = actor.Commands.Queue.Count,
            ["move"] = Id(actor.Commands.Move), ["standard"] = Id(actor.Commands.Standard) };

        private bool PollInteraction()
        {
            var turn = Controller.CurrentTurn; var pair = world.ForActor(riders[0]).Combat;
            if (!command.IsFinished)
            {
                livePolls++;
                if (turn != null && turn.Unit == riders[0]) maximumTimeMoved = Math.Max(maximumTimeMoved, turn.TimeMoved);
                if (!ReferenceEquals(turn, interactionTurn)) turnChangedWhileLive = true;
                if (mounts[0].View.AgentASP.IsReallyMoving)
                {
                    movingPolls++;
                    if (command.IsStarted) startedWhileMoving = true;
                    if (turn == null || turn.Unit != riders[0]) offTurnMovingPolls++;
                }
                // The second rejection: an off-turn click while the first pair's approach is live and the mount has moved.
                if (rejectionDuring == null && movingPolls >= 2 && !command.IsStarted && turn != null && turn.Unit == riders[0])
                { rejectionDuring = SinglePair ? NotApplicableRejection("during-first-pair-approach") : ClickOffTurn("during-first-pair-approach"); evidence["rejectionDuringApproach"] = rejectionDuring; }
                return false;
            }
            // The action-end boundary: the first poll after the native action finished (the Standard was spent on the
            // interaction; the approach travel is over). Raw cooldowns, the rider turn's movement time and the pair's own
            // allocation observation are recorded here, before any settlement time passes.
            if (!actionEndObserved)
            {
                actionEndObserved = true;
                interaction["cooldownsAtActionEnd"] = Cooldowns();
                interaction["timeMovedAtActionEnd"] = turn != null && turn.Unit == riders[0] ? (JToken)turn.TimeMoved : null;
                interaction["remainingTimeAtActionEnd"] = turn != null && turn.Unit == riders[0] ? (JToken)turn.GetRemainingTime() : null;
                interaction["mountPositionAtActionEnd"] = Point(mounts[0].Position); interaction["distanceMovedAtActionEnd"] = Distance(interactionStart, mounts[0].Position);
                var observation = pair.LastPairedMovementObservation;
                interaction["movementObservationAtActionEnd"] = observation; interaction["allocation"] = ParseMovementObservation(observation);
                interaction["commandResult"] = command.Result.ToString(); interaction["commandActed"] = command.IsActed;
            }
            // The settlement/recovery window: movement legality stays observed until the owned post-action lifecycle has
            // finished and the mount is idle; no off-turn movement may hide here.
            if (pair.HasActiveDoorInteraction || pair.HasPendingDoorSettlement || !mounts[0].Commands.Empty || mounts[0].View.AgentASP.IsReallyMoving)
            {
                settlementPolls++;
                if (turn != null && turn.Unit == riders[0]) maximumTimeMoved = Math.Max(maximumTimeMoved, turn.TimeMoved);
                if (!ReferenceEquals(turn, interactionTurn)) turnChangedDuringSettlement = true;
                if (mounts[0].View.AgentASP.IsReallyMoving) { settlementMovingPolls++; if (turn == null || turn.Unit != riders[0]) settlementOffTurnMovingPolls++; }
                return false;
            }
            if (!Settled(door.GetState())) return false;
            var outcome = pair.LastDoorInteractionOutcome;
            interaction["outcome"] = Chunk7NativeDoorProbe.OutcomeRow(outcome);
            interaction["terminal"] = true; interaction["distanceMoved"] = Distance(interactionStart, mounts[0].Position);
            interaction["doorOpenAfter"] = door.GetState(); interaction["cooldownsAfter"] = Cooldowns(); interaction["timeMovedAfter"] = turn != null && turn.Unit == riders[0] ? (JToken)turn.TimeMoved : null;
            interaction["maximumTimeMovedWhileLive"] = maximumTimeMoved; interaction["livePolls"] = livePolls; interaction["movingPolls"] = movingPolls;
            interaction["offTurnMovingPolls"] = offTurnMovingPolls; interaction["startedWhileMoving"] = startedWhileMoving; interaction["unstartedWhileMoving"] = movingPolls > 0 && !startedWhileMoving;
            interaction["turnChangedWhileLive"] = turnChangedWhileLive; interaction["turnStillRiderAfter"] = turn != null && turn.Unit == riders[0];
            interaction["roundAfter"] = Controller.RoundNumber; interaction["mountPositionAfter"] = Point(mounts[0].Position);
            interaction["mountInsideNavmesh"] = Kingmaker.View.ObstacleAnalyzer.IsPointInsideNavMesh(mounts[0].Position);
            interaction["settlementPolls"] = settlementPolls; interaction["settlementMovingPolls"] = settlementMovingPolls;
            interaction["settlementOffTurnMovingPolls"] = settlementOffTurnMovingPolls; interaction["turnChangedDuringSettlement"] = turnChangedDuringSettlement;
            interaction["cooldownDeltas"] = CooldownDeltas((JObject)interaction["cooldownsAtClick"], (JObject)interaction["cooldownsAfter"]);
            var cost = MovementCost((JObject)interaction["cooldownsAtClick"], (JObject)interaction["cooldownsAtActionEnd"], (JObject)interaction["cooldownsAfter"],
                interaction["allocation"] as JObject, outcome?.RecoveryDisposition);
            interaction["movementCost"] = cost;
            Require(outcome != null && outcome.Result == "Success" && outcome.RiderId == riders[0].UniqueId && outcome.MountId == mounts[0].UniqueId &&
                outcome.InteractionCount == 1 && outcome.DelegatedMoveStartCount >= 1 && outcome.DoorStateChanged && outcome.MountMoveSlotRestored && door.GetState() &&
                (double)interaction["distanceMoved"] >= 0.5d,
                "The turn-based door input did not prove one approach, one interaction and restored mount movement: " + interaction["outcome"].ToString(Newtonsoft.Json.Formatting.None));
            Require(outcome.DistanceAtInteraction <= Radius + 0.01f && outcome.LineOfSightAtInteraction,
                "The turn-based interaction was not delivered from inside the native proximity radius with line of sight.");
            Require(outcome.ApproachesWithoutProgress == 0 && !turnChangedWhileLive && offTurnMovingPolls == 0 && (bool)interaction["unstartedWhileMoving"],
                "The mount moved off the rider's turn, the command started while moving, or a legal lack of movement was classified as a stall.");
            var deltas = (JObject)interaction["cooldownDeltas"];
            // Native spending at the action end: the rider's Standard action is spent exactly once by the interaction (+6 in
            // turn-based combat, at most one frame of decay before this boundary); the mount spends no Standard action; the
            // second pair's cooldowns are untouched; nothing of the first pair increases afterwards except an owned recovery move.
            Require((double)cost["riderStandardDelta"] >= 5.5d && (double)cost["riderStandardDelta"] <= 6.05d && Math.Abs((double)cost["mountStandardDelta"]) <= 0.01d &&
                (SinglePair || new[] { "secondRider", "secondMount" }.All(r => new[] { "standard", "move", "swift" }.All(k => Math.Abs((double)deltas[r][k]) <= 0.01d))),
                "Native action spending across the turn-based door interaction differs from exactly-once rider Standard spending: " + cost.ToString(Newtonsoft.Json.Formatting.None));
            // Movement cost (paired contract): real travel is charged once to the mount's own Move budget, never more than the
            // game time that passed or the pair's allocation time, never to the rider; nothing is refunded afterwards beyond
            // native decay, and no unowned movement follows the action.
            Require((bool)cost["lawful"], "Turn-based paired approach movement cost differs from the installed paired contract: " + cost.ToString(Newtonsoft.Json.Formatting.None));
            Require(settlementOffTurnMovingPolls == 0 && !turnChangedDuringSettlement, "The mount moved off the rider's turn, or the turn changed, during the owned settlement window.");
            if (SinglePair && rejectionDuring == null) { rejectionDuring = NotApplicableRejection("during-first-pair-approach"); evidence["rejectionDuringApproach"] = rejectionDuring; }
            Require(rejectionDuring != null, "The second pair's off-turn click during the live approach was not exercised (the approach ended before the mount was observed moving twice).");
            return true;
        }

        // The pair's own allocation observation ("mountMove=a->b;mountTime=t;mountStepMetres=s;nativePrepared=p;round=r;physicalDelta=x->y").
        private static JObject ParseMovementObservation(string observation)
        {
            if (string.IsNullOrEmpty(observation) || !observation.StartsWith("mountMove=", StringComparison.Ordinal)) return null;
            try
            {
                var fields = observation.Split(';').Select(x => x.Split(new[] { '=' }, 2)).Where(x => x.Length == 2).ToDictionary(x => x[0], x => x[1], StringComparer.Ordinal);
                var move = fields["mountMove"].Split(new[] { "->" }, StringSplitOptions.None); var physical = fields["physicalDelta"].Split(new[] { "->" }, StringSplitOptions.None);
                return new JObject { ["mountMoveBefore"] = Single(move[0]), ["mountMoveAfter"] = Single(move[1]), ["mountTime"] = Single(fields["mountTime"]),
                    ["mountStepMetres"] = Single(fields["mountStepMetres"]), ["nativePrepared"] = bool.Parse(fields["nativePrepared"]), ["round"] = int.Parse(fields["round"], System.Globalization.CultureInfo.InvariantCulture),
                    ["physicalDeltaRequested"] = Single(physical[0]), ["physicalDeltaAllowed"] = Single(physical[1]) };
            }
            catch (Exception) { return null; }
        }
        private static float Single(string value)
        {
            float parsed;
            if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed)) return parsed;
            return float.Parse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture);
        }

        // The movement-cost relation of the installed paired contract (Chunk 7 checkpoint rule, accepted natively): a rider's
        // turn-based approach is native movement charged to its own mount's Move budget (delta equal to the allocation's
        // movement time, inside the six-second budget, with real displacement); the rider keeps its own Move. Settlement may
        // only decay cooldowns (bounded by elapsed game time) unless one owned recovery move is admitted.
        private static JObject MovementCost(JObject atClick, JObject atActionEnd, JObject after, JObject allocation, string recoveryDisposition)
        {
            double Ticks(JObject row) => (double)(long)row["rider"]["gameTicks"];
            var approachSeconds = (Ticks(atActionEnd) - Ticks(atClick)) / 1e7; var settlementSeconds = (Ticks(after) - Ticks(atActionEnd)) / 1e7;
            var mountCharge = (double)atActionEnd["mount"]["move"] - (double)atClick["mount"]["move"];
            var riderMoveDelta = (double)atActionEnd["rider"]["move"] - (double)atClick["rider"]["move"];
            var riderStandardDelta = (double)atActionEnd["rider"]["standard"] - (double)atClick["rider"]["standard"];
            var mountStandardDelta = (double)atActionEnd["mount"]["standard"] - (double)atClick["mount"]["standard"];
            var mountMoveAfterSettlement = (double)after["mount"]["move"] - (double)atActionEnd["mount"]["move"];
            var riderStandardAfterSettlement = (double)after["rider"]["standard"] - (double)atActionEnd["rider"]["standard"];
            var allocationTime = allocation == null ? (double?)null : (double)allocation["mountTime"];
            var stepMetres = allocation == null ? 0d : (double)allocation["mountStepMetres"];
            var recovered = recoveryDisposition == MountedDoorRecoveryDisposition.Admitted.ToString();
            var decayBound = 1.1d * settlementSeconds + 0.01d;
            // Native turn-based GameTime does not advance inside a turn (retained TB traces: constant ticks across a whole
            // rider turn), so the game-seconds figures are recorded data, never a bound on the charge; the allocation time
            // and the six-second budget bound it.
            var lawful = mountCharge > 0.01d && (double)atActionEnd["mount"]["move"] <= 6.001d &&
                Math.Abs(riderMoveDelta) <= 0.01d && (double)atActionEnd["rider"]["move"] <= 0.01d &&
                allocationTime.HasValue && mountCharge <= allocationTime.Value + 0.05d && (stepMetres > 0d || Math.Abs(mountCharge - allocationTime.Value) <= 0.05d) &&
                Math.Abs((double)allocation["mountMoveAfter"] - (double)atActionEnd["mount"]["move"]) <= 0.05d &&
                mountMoveAfterSettlement >= -decayBound && (recovered ? (double)after["mount"]["move"] <= 6.001d : mountMoveAfterSettlement <= 0.01d) &&
                riderStandardAfterSettlement >= -decayBound && riderStandardAfterSettlement <= 0.01d;
            return new JObject { ["contract"] = MovementCostContract, ["approachGameSeconds"] = approachSeconds, ["settlementGameSeconds"] = settlementSeconds,
                ["mountMoveCharge"] = mountCharge, ["riderMoveDelta"] = riderMoveDelta, ["riderStandardDelta"] = riderStandardDelta, ["mountStandardDelta"] = mountStandardDelta,
                ["mountMoveAfterSettlement"] = mountMoveAfterSettlement, ["riderStandardAfterSettlement"] = riderStandardAfterSettlement,
                ["allocationTime"] = allocationTime, ["allocationStepMetres"] = stepMetres, ["recoveryAdmitted"] = recovered, ["decayBound"] = decayBound, ["lawful"] = lawful };
        }

        private bool EndRiderTurnAndEncounter()
        {
            var turn = Controller.CurrentTurn;
            if (!encounterEnded)
            {
                if (turn != null && turn.Unit == riders[0] && !ReferenceEquals(turn, endedRiderTurn))
                {
                    if (!TryEndTurn(turn, "rider-after-interaction")) return false;
                    endedRiderTurn = turn; return false;
                }
                // The encounter ends by removing the diagnostic target (the accepted combat cases' own cleanup order); the
                // native removal may need more than one frame, so it is retried inside the stage bound.
                var gone = targetService.DestroyAndVerify();
                foreach (var actor in originalParty.Concat(actors).Distinct()) if (actor.IsInState && actor.IsInCombat) actor.LeaveCombat();
                Game.Instance.EntityDestroyer.Tick();
                evidence["encounterEnd"] = new JObject { ["targetRemoved"] = gone, ["round"] = Controller.RoundNumber, ["frame"] = Time.frameCount };
                if (!gone) return false;
                encounterEnded = true;
            }
            return true;
        }

        private void ReleaseEncounterServices()
        {
            if (servicesReleased) return;
            targetService?.Dispose(); targetService = null; target = null;
            if (initiativeApplied) { foreach (var entry in initiativeBefore) entry.Key.Stats.Initiative.BaseValue = entry.Value; initiativeApplied = false; }
            if (aiAcquired && pairAi != null && !pairAi.LastRestoreVerified) { pairAi.ReassertAfterNativeReset(AiActors); pairAi.Restore(AiActors); }
            aiAcquired = false;
            if (modeSelected) { selectMode(false); modeSelected = false; }
            servicesReleased = true;
            evidence["servicesReleased"] = new JObject { ["initiativeRestored"] = actors.All(a => a.Stats.Initiative.BaseValue == initiativeBefore[a]),
                ["aiLeaseRestored"] = pairAi == null || pairAi.LastRestoreVerified, ["targetServiceDisposed"] = true, ["realTimeModeReselected"] = true };
        }

        private bool TryEndTurn(TurnController turn, string label)
        {
            if (turn?.Unit == null || !turn.Unit.IsDirectlyControllable || !turn.Unit.Commands.Empty || turn.Unit.AreHandsBusyWithAnimation ||
                !turn.CanEndTurnAndNoActing() || Controller.WaitingForUI || MountedPairTurnParticipant.NativeNextCandidate(Controller) != null ||
                world.Pairs.Any(p => p.Combat.HasActiveCommand || p.Combat.HasActiveGroundMovement)) return false;
            Game.Instance.PauseBind();
            endedTurns.Add(new JObject { ["label"] = label, ["actorId"] = turn.Unit.UniqueId, ["round"] = Controller.RoundNumber, ["turn"] = Id(turn), ["api"] = "Game.PauseBind (native End Turn binding)" });
            return true;
        }

        private void ObserveTurn()
        {
            var turn = Controller?.CurrentTurn;
            if (turn?.Unit == null || !visited.Add(turn)) return;
            Require(turns.Count < 64, "Native turn observation bound exceeded.");
            turns.Add(new JObject { ["actorId"] = turn.Unit.UniqueId, ["round"] = Controller.RoundNumber, ["turn"] = Id(turn), ["stage"] = stage,
                ["isPairRider"] = riders.Contains(turn.Unit), ["isMount"] = mounts.Contains(turn.Unit), ["isTarget"] = ReferenceEquals(turn.Unit, target) });
            Require(!mounts.Contains(turn.Unit), "A mounted partner received an independent native roster turn.");
        }

        private JObject Cooldowns() => new JObject { ["rider"] = Cooldowns(riders[0]), ["mount"] = Cooldowns(mounts[0]),
            ["secondRider"] = SinglePair ? null : Cooldowns(riders[1]), ["secondMount"] = SinglePair ? null : Cooldowns(mounts[1]) };
        private static JObject Cooldowns(UnitEntityData actor) => new JObject { ["id"] = actor.UniqueId, ["standard"] = actor.CombatState.Cooldown.StandardAction,
            ["move"] = actor.CombatState.Cooldown.MoveAction, ["swift"] = actor.CombatState.Cooldown.SwiftAction, ["prepared"] = actor.CombatState.Prepared,
            ["reactions"] = actor.CombatState.AttackOfOpportunityCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks };
        private static JObject CooldownDeltas(JObject before, JObject after)
        {
            var result = new JObject();
            foreach (var role in new[] { "rider", "mount", "secondRider", "secondMount" })
                result[role] = before[role] == null || before[role].Type == JTokenType.Null || after[role] == null || after[role].Type == JTokenType.Null ? null :
                    new JObject { ["standard"] = (double)after[role]["standard"] - (double)before[role]["standard"],
                    ["move"] = (double)after[role]["move"] - (double)before[role]["move"], ["swift"] = (double)after[role]["swift"] - (double)before[role]["swift"] };
            return result;
        }

        private string Blockers()
        {
            var turn = Controller?.CurrentTurn;
            return "Turn: " + (turn?.Unit?.UniqueId ?? "none") + " (" + (turn == null ? "none" : turn.Status.ToString()) + "), round " + (Controller?.RoundNumber ?? -1) +
                ", inCombat " + Game.Instance.Player.IsInCombat + ", tb " + CombatController.IsInTurnBasedCombat() + ", controlPending " + controlPending + ".";
        }

        private void CloseForInput()
        {
            RequirePairs(); RequireDoor();
            Require(actors.All(Chunk7NativeFixture.Idle) && door.GetState(), "Door fixture closing requires idle actors and an open door.");
            if (!door.CanInteract() && MountedDistanceDoorFixturePolicy.CanTemporarilyEnable(door.CanInteract(), door.Enabled, door.DisableOnOpen, Game.Instance.Player.IsInCombat)) door.Enabled = true;
            Require(door.CanInteract(), "Native door is not interactable for fixture preparation.");
            door.Interact(riders[0]); // Native fixture preparation, not a player-input assertion.
            setupInteractions++; evidence["setupInteractions"] = setupInteractions; settledFrame = -1;
        }

        // Cleanup: the encounter is ended and its services released even on a failure path, then the door lease restores.
        internal bool Drain()
        {
            scope.RequireCurrent();
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() || NativePersistenceIsolation.HasPendingWrites || world.SerializationBusy) return false;
            foreach (var owned in commands.Where(x => !x.IsFinished)) owned.Interrupt();
            if (commands.Any(x => !x.IsFinished)) return false;
            if (targetService != null)
            {
                if (!encounterEnded)
                {
                    var gone = targetService.DestroyAndVerify();
                    foreach (var actor in originalParty.Concat(actors).Distinct()) if (actor.IsInState && actor.IsInCombat) actor.LeaveCombat();
                    Game.Instance.EntityDestroyer.Tick();
                    if (!gone) return false;
                    encounterEnded = true;
                }
                if (Game.Instance.Player.IsInCombat || Controller.Initialized || CombatController.IsInTurnBasedCombat()) return false;
                ReleaseEncounterServices();
            }
            else if (!servicesReleased && (initiativeApplied || aiAcquired || modeSelected)) ReleaseEncounterServices();
            if (!leaseCaptured || restored) return true;
            RequireDoor();
            if (door.GetState() != originalOpen) { door.Open(); settledFrame = -1; }
            if (!Settled(originalOpen)) return false;
            door.Enabled = originalEnabled;
            var before = door.Data;
            Require(before != null && originalRestrictionCount == 0, "Native door unlock state is not restorable without a behavioural effect.");
            var observedBefore = before.AlreadyUnlocked;
            if (observedBefore != originalAlreadyUnlocked) before.AlreadyUnlocked = originalAlreadyUnlocked;
            var after = door.Data;
            Require(after != null && door.GetState() == originalOpen && door.Enabled == originalEnabled && cut.enabled == originalCut && after.AlreadyUnlocked == originalAlreadyUnlocked,
                "Native door lease has not restored its original open/enabled/cut/unlock state.");
            restored = true; restoration = new JObject { ["exact"] = true, ["commandsSettled"] = true, ["open"] = door.GetState(), ["enabled"] = door.Enabled,
                ["cutEnabled"] = cut.enabled, ["observedAlreadyUnlockedBeforeRestore"] = observedBefore, ["alreadyUnlocked"] = after.AlreadyUnlocked };
            evidence["restoration"] = restoration;
            return true;
        }

        private bool Mounted(int index)
        {
            var owner = world.Registry.FindActive(riders[index]);
            return owner != null && ReferenceEquals(owner.Mount, mounts[index]) && ReferenceEquals(world.Registry.FindActive(mounts[index]), owner) &&
                owner.Boundary.State == RelationshipState.Mounted;
        }
        private void RequirePairs()
        {
            scope.RequireCurrent();
            for (var i = 0; i < owners.Length; i++)
                Require(owners[i] != null && ReferenceEquals(owners[i], world.Registry.FindActive(riders[i])) && ReferenceEquals(owners[i], world.Registry.FindActive(mounts[i])) &&
                    owners[i].Generation == generations[i] && owners[i].Boundary.State == RelationshipState.Mounted, "Turn-based door input lost its exact mounted owner or generation.");
        }
        private void RequireDoor() => Require(door != null && ReferenceEquals(door.gameObject, doorObject) && ReferenceEquals(door.transform, doorTransform) &&
            ReferenceEquals(door.GetComponentInChildren<NavmeshCut>(), cut) && door.isActiveAndEnabled && door.gameObject.activeInHierarchy,
            "Captured native door/view/cut was replaced or left its area.");
        private bool Settled(bool open) => Chunk7NativeDoorProbe.DoorSettled(door, cut, open, cutToggles, originalCut, ref settledFrame);
        private void Next(int value) { stage = value; stageStarted = Time.realtimeSinceStartup; settledFrame = -1; }
        private static float Distance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static JObject Point(Vector3 value) => new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
