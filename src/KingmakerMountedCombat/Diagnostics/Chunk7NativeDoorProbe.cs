using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View.MapObjects;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using Pathfinding;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KingmakerMountedCombat.Diagnostics
{
    // Private two-pair interaction extension. The caller owns this object before
    // Begin can acquire a door or dispatch native work. Fixture closing is setup;
    // only ClickMapObjectHandler delivery is counted as mounted interaction proof.
    internal sealed class Chunk7NativeDoorProbe
    {
        private readonly MountedWorldService world;
        private readonly Chunk7NativeWorldScope scope;
        private readonly UnitEntityData[] riders, mounts, actors;
        private readonly MountedPairOwner<UnitEntityData, GameMountedRelationshipService>[] owners;
        private readonly long[] generations;
        private readonly List<UnitCommand> commands = new List<UnitCommand>();
        private readonly JArray interactions = new JArray();
        private readonly JArray cutObservations = new JArray();
        private StandardDoor door;
        private NavmeshCut cut;
        private object doorObject, doorTransform;
        private bool captured, originalEnabled, originalOpen, originalCut, originalAlreadyUnlocked, restored, cutToggles;
        private int originalRestrictionCount = -1;
        private int stage, pairIndex, settledFrame = -1;
        private double stageStarted;
        private Vector3 firstStart, interactionStart, returnGoal;
        private MountedDoorInteractionCommand command;
        private UnitMoveTo returnMove, setupMove, lateralMove;
        private JObject row, returnRecord, longApproach, clickWhileMoving;
        private int lateralMovingFrames;
        // v2 (F4 r30): the first pair returns to a goal clear of every other unit's native avoidance footprint
        // (DoorReturnGoalPolicy), recorded with its search and outcome before any assertion on it.
        // v3 (Sprint 2, DOOR-01/SPACE-01): the first pair's click starts from a searched long approach (a native
        // ground move away from the door first, recorded in longApproach); the second pair's click is given
        // while its mount is still carrying out an ordinary ground move (clickWhileMoving); every interaction
        // records the pair's navmesh occupancy after the door changed state; the outcome carries the command's
        // approach progress and occupancy fields; approach starts are at least one, not exactly one.
        internal const string Contract = "two-pair-native-door-click-selection-change-v3";
        private const float LongApproachDistanceMetres = 12f;
        private const float LateralMoveDistanceMetres = 6f;
        private readonly JObject evidence = new JObject();
        internal bool Completed { get; private set; }
        internal JObject Evidence => evidence;
        internal UnitCommand[] IssuedCommands => commands.ToArray();
        internal int CompletedInteractions { get; private set; }
        internal Vector3? DoorPosition => door == null ? (Vector3?)null : door.transform.position;
        internal float DoorRadius => door == null ? 0f : Radius(door);
        private Vector3 ApproachPoint => door.MapObject != null ? door.MapObject.transform.position : door.transform.position;

        internal Chunk7NativeDoorProbe(MountedWorldService world, Chunk7NativeWorldScope scope,
            UnitEntityData firstRider, UnitEntityData firstMount, UnitEntityData secondRider, UnitEntityData secondMount)
        {
            this.world = world; this.scope = scope;
            riders = new[] { firstRider, secondRider }; mounts = new[] { firstMount, secondMount };
            actors = new[] { firstRider, firstMount, secondRider, secondMount };
            owners = riders.Select(world.Registry.FindActive).ToArray();
            generations = owners.Select(x => x == null ? -1 : x.Generation).ToArray();
            evidence["contract"] = Contract;
            evidence["interactions"] = interactions;
            evidence["setupInteractions"] = 0;
            evidence["cutObservations"] = cutObservations;
        }

        internal void Begin()
        {
            Require(stage == 0 && !captured, "Door probe may begin only once.");
            RequirePairs(); Require(actors.All(Chunk7NativeFixture.Idle), "Door probe requires idle native actors.");
            door = FindQualifyingDoor(mounts, out _);
            Require(door != null, "No bounded open or player-clickable closed native door with a navmesh cut outside both pair interaction radii; exact door fixture is required.");
            cut = door.GetComponentInChildren<NavmeshCut>();
            doorObject = door.gameObject; doorTransform = door.transform;
            var facts = DoorFacts.Capture(door, mounts);
            originalEnabled = door.Enabled; originalOpen = door.GetState(); originalCut = cut.enabled;
            cutToggles = door.DisableNavmeshCutWhenOpen;
            originalAlreadyUnlocked = facts.AlreadyUnlocked; originalRestrictionCount = facts.RestrictionCount; captured = true;
            // StandardDoor writes its cut only when DisableNavmeshCutWhenOpen is set (its state application
            // and Open both guard on it): a toggling door has its cut off while open and on while closed;
            // the door never writes a static door's cut, so that cut must stay unchanged for the lease.
            if (cutToggles) Require(originalCut != originalOpen, "Door navmesh cut does not match its native open state.");
            Require(facts.Eligible, "Captured door no longer satisfies the qualifying-door predicate.");
            // CanInteract reads Enabled: a closed start is clickable only while natively enabled.
            Require(originalOpen || originalEnabled, "A closed door start must be natively enabled.");
            evidence["fixture"] = new JObject { ["name"] = door.name, ["object"] = Id(door),
                ["position"] = Point(door.transform.position), ["originalOpen"] = originalOpen,
                ["originalEnabled"] = originalEnabled, ["originalCut"] = originalCut, ["cutMode"] = cutToggles ? "toggles" : "static",
                ["disableOnOpen"] = door.DisableOnOpen, ["radius"] = Radius(door),
                ["interactable"] = facts.Interactable, ["availableInteractions"] = facts.AvailableInteractions,
                ["revealed"] = facts.Revealed, ["hasVisibleTrap"] = facts.HasVisibleTrap, ["trapActive"] = facts.TrapActive,
                ["originalAlreadyUnlocked"] = facts.AlreadyUnlocked, ["restrictionCount"] = facts.RestrictionCount,
                ["hasDialog"] = facts.HasDialog, ["dataPresent"] = facts.DataPresent,
                ["viewTriggerCount"] = facts.ViewTriggerCount, ["viewTriggerActions"] = facts.ViewTriggerActions };
            firstStart = mounts[0].Position; ObserveCut("captured");
            evidence["approach"] = new JObject { ["approachPoint"] = Point(ApproachPoint),
                ["nativeTargetNode"] = Point(Kingmaker.View.ObstacleAnalyzer.GetNearestNode(door.transform.position).clampedPosition),
                ["losObjectId"] = door.transform.GetInstanceID(), ["mapObjectIsDoorObject"] = door.MapObject == null || door.MapObject.gameObject == door.gameObject };
            longApproach = new JObject { ["applicable"] = false }; evidence["longApproach"] = longApproach;
            clickWhileMoving = new JObject { ["applicable"] = false }; evidence["clickWhileMoving"] = clickWhileMoving;
            // A closed door is already in its input state; only an open one is closed as setup.
            if (originalOpen) CloseForInput();
            Next(5);
        }

        // Sprint 2: a bounded read-only search for a native ground goal at a distance from the mover, in a preferred
        // direction and its bearings, walkable within 0.5 m of the request, reachable by the navmesh trace, outside
        // the door approach by the given margin and clear of every other unit's avoidance footprint.
        private Vector3 SearchClearGoal(UnitEntityData mover, UnitEntityData carried, Vector3 direction, float distance, float doorMargin, JObject record, out bool found)
        {
            found = false; var start = mover.Position; direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) direction = Vector3.forward; direction = direction.normalized;
            var candidates = new JArray(); record["start"] = Point(start); record["direction"] = Point(direction); record["distance"] = distance;
            record["candidates"] = candidates; record["selectedIndex"] = -1;
            var moverCorpulence = mover.View.AgentASP.Corpulence;
            var others = Game.Instance.State.Units.Where(u => u != null && u.IsInState && u.View?.AgentASP != null && !ReferenceEquals(u, mover) &&
                !ReferenceEquals(u, carried) && Distance(u.Position, start) <= 30f).Take(256).ToArray();
            var goal = start; var offsets = new[] { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 150f, -150f, 180f };
            for (var i = 0; i < offsets.Length && !found; i++)
            {
                var requested = start + Quaternion.Euler(0f, offsets[i], 0f) * direction * distance;
                var nearest = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(requested);
                var walkable = nearest.node != null && nearest.node.Walkable;
                var candidate = new JObject { ["index"] = i, ["bearingOffset"] = offsets[i], ["requested"] = Point(requested), ["walkable"] = walkable };
                var reason = "no-walkable-native-node";
                if (walkable)
                {
                    var clamped = nearest.clampedPosition;
                    var clampResidual = Distance(requested, clamped);
                    var traceResidual = Distance(Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(start, clamped), clamped);
                    var doorDistance = Distance(clamped, ApproachPoint);
                    var slack = double.PositiveInfinity;
                    foreach (var other in others) slack = Math.Min(slack, Distance(clamped, other.Position) - (moverCorpulence + other.View.AgentASP.Corpulence + 0.5f));
                    candidate["clamped"] = Point(clamped); candidate["clampResidual"] = clampResidual; candidate["traceResidual"] = traceResidual;
                    candidate["doorDistance"] = doorDistance; candidate["minimumSlack"] = slack;
                    reason = clampResidual > 0.5f ? "clamp-residual" : traceResidual > 0.5f ? "trace-unreachable" :
                        doorDistance <= Radius(door) + doorMargin ? "inside-door-approach" : doorDistance > ScanRadius ? "outside-door-scan-radius" :
                        slack < 0d ? "blocked-by-unit" : "admitted";
                    if (reason == "admitted") { goal = clamped; found = true; }
                }
                candidate["reason"] = reason; candidates.Add(candidate);
            }
            record["selectedIndex"] = found ? candidates.Count - 1 : -1;
            return goal;
        }

        private UnitMoveTo IssueGroundMove(int pair, Vector3 goal)
        {
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(riders[pair]);
            UnitMoveTo move = null;
            try { ClickGroundHandler.MoveSelectedUnitsToPoint(goal, false); }
            finally
            {
                move = mounts[pair].Commands.Move;
                if (move != null && !commands.Contains(move)) commands.Add(move);
            }
            Require(move != null && ReferenceEquals(move.Executor, mounts[pair]) && move.CreatedByPlayer, "Native ground click did not bind the pair's mount.");
            Chunk7NativeFixture.Select(riders[1 - pair]); Game.Instance.IsPaused = false;
            return move;
        }

        private JObject Occupancy(int pair)
        {
            var mount = mounts[pair]; var rider = riders[pair];
            return new JObject { ["mountPosition"] = Point(mount.Position), ["riderPosition"] = Point(rider.Position),
                ["mountInsideNavmesh"] = Kingmaker.View.ObstacleAnalyzer.IsPointInsideNavMesh(mount.Position),
                ["riderInsideNavmesh"] = Kingmaker.View.ObstacleAnalyzer.IsPointInsideNavMesh(rider.Position),
                ["mountDistanceToApproachPoint"] = Distance(mount.Position, ApproachPoint),
                ["mountDistanceToDoor"] = Distance(mount.Position, door.transform.position),
                ["doorOpen"] = door.GetState(), ["cutEnabled"] = cut.enabled,
                ["mountCanMove"] = mount.Descriptor.State.CanMove, ["mountCommandsEmpty"] = mount.Commands.Empty };
        }

        // The exact qualifying-door query Begin uses, as a read-only bounded scan: no
        // door is acquired, closed or changed. Applicability is decided only from it.
        // A door whose cut toggles is preferred, then an open door; a closed door must be one a player
        // could click. A door whose cut the door never writes (static) qualifies too. Every door
        // must be unrestricted and carry no door-view trigger actions (see DoorFacts).
        internal const float ScanRadius = 28f;
        internal static StandardDoor FindQualifyingDoor(UnitEntityData[] mounts, out int candidatesScanned)
        {
            var candidates = UnityEngine.Object.FindObjectsOfType<StandardDoor>() ?? new StandardDoor[0];
            candidatesScanned = candidates.Length;
            return candidates
                .Where(d => d != null && DoorFacts.Capture(d, mounts).Eligible)
                .OrderByDescending(d => d.DisableNavmeshCutWhenOpen).ThenByDescending(d => d.GetState())
                .ThenBy(d => mounts.Sum(m => Distance(m.Position, d.transform.position))).FirstOrDefault();
        }

        // The one door predicate shared by the probe, the inventory and the not-applicable scan.
        // Every member is the installed 2.1.7b public contract; nothing here mutates the door.
        private sealed class DoorFacts
        {
            internal bool Active, Open, DisableCutWhenOpen, CutPresent, Interactable, AvailableInteractions,
                Revealed, HasVisibleTrap, TrapActive, AlreadyUnlocked, HasDialog, DataPresent, ViewTriggerActions;
            internal int RestrictionCount, ViewTriggerCount;
            internal float Radius;
            internal float[] MountDistances;
            // A closed door must be one ordinary player input could open: clickable, revealed,
            // untrapped and not a dialog object.
            internal bool ClosedClickable => Interactable && AvailableInteractions && Revealed && !HasVisibleTrap &&
                !TrapActive && !HasDialog;
            // Every door, open or closed, must carry no InteractionRestriction (so restoring the
            // AlreadyUnlocked flag that native interaction sets has no behavioural effect) and no
            // DoorInteractionViewTrigger open/close/restricted actions (the probe must not run
            // designer actions). Blueprint-level Generic/Device interaction triggers and the
            // private after-interaction callback are not publicly readable; that residual risk
            // is recorded in the door documentation.
            internal bool Eligible => Active && DataPresent && CutPresent && RestrictionCount == 0 &&
                !ViewTriggerActions && (Open || ClosedClickable) &&
                MountDistances.All(distance => distance > Radius + 0.5f && distance <= ScanRadius);

            internal static DoorFacts Capture(StandardDoor door, UnitEntityData[] mounts)
            {
                var view = door.MapObject; var trap = door.Trap; var data = ((InteractionComponent)door).Data;
                // The door and its whole map object, so a restriction or trigger on a sibling counts.
                var roots = view != null && view.gameObject != door.gameObject ? new[] { door.gameObject, view.gameObject } : new[] { door.gameObject };
                var restrictions = roots.SelectMany(r => r.GetComponentsInChildren<InteractionRestriction>(true)).Distinct().Count();
                var triggers = roots.SelectMany(r => r.GetComponentsInChildren<DoorInteractionViewTrigger>(true)).Distinct().ToArray();
                return new DoorFacts
                {
                    Active = door.isActiveAndEnabled && door.gameObject.activeInHierarchy, Open = data != null && door.GetState(),
                    DisableCutWhenOpen = door.DisableNavmeshCutWhenOpen, CutPresent = door.GetComponentInChildren<NavmeshCut>() != null,
                    Interactable = door.CanInteract(),
                    AvailableInteractions = view != null && view.Data != null && ClickMapObjectHandler.HasAvailableInteractions(door.gameObject),
                    Revealed = view != null && view.Data != null && view.Data.IsRevealed,
                    HasVisibleTrap = door.HasVisibleTrap(), TrapActive = trap != null && trap.Data != null && trap.TrapActive,
                    DataPresent = data != null, AlreadyUnlocked = data != null && data.AlreadyUnlocked,
                    RestrictionCount = restrictions, ViewTriggerCount = triggers.Length,
                    ViewTriggerActions = triggers.Any(t => HasActions(t.OpenActions) || HasActions(t.CloseActions) || HasActions(t.RestrictedActions)),
                    HasDialog = door.Dialog != null, Radius = Chunk7NativeDoorProbe.Radius(door),
                    MountDistances = mounts.Select(m => Distance(m.Position, door.transform.position)).ToArray()
                };
            }

            private static bool HasActions(ActionsHolder holder) => holder != null && holder.Actions != null && holder.Actions.HasActions;

            internal JObject Row(StandardDoor door) => new JObject { ["name"] = door.name, ["position"] = Point(door.transform.position),
                ["active"] = Active, ["open"] = Open, ["disableCutWhenOpen"] = DisableCutWhenOpen,
                ["cutPresent"] = CutPresent, ["proximityRadius"] = Radius,
                ["mountDistances"] = new JArray(MountDistances[0], MountDistances[1]),
                ["interactable"] = Interactable, ["availableInteractions"] = AvailableInteractions, ["revealed"] = Revealed,
                ["hasVisibleTrap"] = HasVisibleTrap, ["trapActive"] = TrapActive, ["alreadyUnlocked"] = AlreadyUnlocked,
                ["restrictionCount"] = RestrictionCount, ["hasDialog"] = HasDialog, ["dataPresent"] = DataPresent,
                ["viewTriggerCount"] = ViewTriggerCount, ["viewTriggerActions"] = ViewTriggerActions, ["eligible"] = Eligible };
        }

        // Nearest-first bounded rows of every present door, by the same predicate.
        private static JArray ScanRows(UnitEntityData[] mounts, StandardDoor[] present) =>
            new JArray(present.OrderBy(d => mounts.Sum(m => Distance(m.Position, d.transform.position)))
                .ThenBy(d => d.name, StringComparer.Ordinal).Take(32).Select(d => DoorFacts.Capture(d, mounts).Row(d)));

        // The observed absence of a qualifying door, with the rows that decided it, so a door
        // too close to the pairs is distinguishable from no door at all.
        internal static JObject NotApplicable(UnitEntityData[] mounts)
        {
            var present = (UnityEngine.Object.FindObjectsOfType<StandardDoor>() ?? new StandardDoor[0]).Where(d => d != null).ToArray();
            Require(present.Length <= 100000, "Native door scan exceeded its read-only bound.");
            var rows = ScanRows(mounts, present);
            Require(rows.All(r => !(bool)r["eligible"]), "A qualifying door exists; the door step is applicable.");
            return new JObject { ["applicable"] = false,
                ["reason"] = "No open or player-clickable closed unrestricted StandardDoor with a navmesh cut lies outside both pair interaction radii within the bounded scan radius.",
                ["candidatesScanned"] = present.Length, ["radius"] = ScanRadius,
                ["mounts"] = new JArray(mounts.Select(m => new JObject { ["id"] = m.UniqueId, ["position"] = Point(m.Position) })),
                ["truncated"] = present.Length > 32, ["rows"] = rows };
        }

        // Read-only area-fixture inventory. This does not acquire, close or click a
        // door, and it cannot turn an absent fixture into door qualification.
        internal static JObject CaptureAreaDoorInventory(UnitEntityData[] mounts, string boundary = "before-first-continuation")
        {
            if (boundary != "before-first-continuation" && boundary != "after-fixture-remount")
                throw new ArgumentException("Door inventory boundary is not declared.", nameof(boundary));
            Require(mounts != null && mounts.Length == 2 && mounts.All(m => m?.View?.AgentASP != null),
                "Door inventory requires the two live mounted movement actors.");
            var doors = UnityEngine.Object.FindObjectsOfType<StandardDoor>() ?? new StandardDoor[0];
            Require(doors.Length <= 100000, "Native door inventory exceeded its read-only bound.");
            var present = doors.Where(d => d != null).ToArray();
            var rows = ScanRows(mounts, present);
            return new JObject { ["boundary"] = boundary, ["candidateCount"] = present.Length, ["rowLimit"] = 32,
                ["radius"] = ScanRadius, ["mounts"] = new JArray(mounts.Select(m => new JObject {
                    ["id"] = m.UniqueId, ["position"] = Point(m.Position) })),
                ["truncated"] = present.Length > 32, ["rows"] = rows,
                ["interactionTested"] = false };
        }

        internal bool Poll()
        {
            if (Completed) return true;
            RequirePairs(); RequireDoor();
            Require(Time.realtimeSinceStartup - stageStarted <= 30, "Native door phase exceeded thirty seconds: " + stage);
            switch (stage)
            {
                case 5:
                    // Sprint 2 long approach: the first pair first moves away from the door through an ordinary ground
                    // click, so its door click covers a meaningful native approach (accelerated-gait length when the
                    // fixture allows it). Recorded before any assertion; not applicable when no clear goal exists.
                    if (!Settled(false)) return false;
                    {
                        var search = new JObject(); longApproach["goalSearch"] = search;
                        var away = mounts[0].Position - ApproachPoint;
                        var goal = SearchClearGoal(mounts[0], riders[0], away, LongApproachDistanceMetres, 0.5f, search, out var found);
                        if (!found) { longApproach["reason"] = "no clear native goal away from the door"; Next(1); break; }
                        longApproach["applicable"] = true; longApproach["goal"] = Point(goal); longApproach["startDistanceToDoor"] = Distance(mounts[0].Position, ApproachPoint);
                        setupMove = IssueGroundMove(0, goal); longApproach["setupCommand"] = Id(setupMove);
                        Next(6);
                    }
                    break;
                case 6:
                    if (!setupMove.IsFinished || mounts[0].View.AgentASP.IsReallyMoving || !actors.All(Chunk7NativeFixture.Idle)) return false;
                    longApproach["setupResult"] = setupMove.Result.ToString(); longApproach["setupEnd"] = Point(mounts[0].Position);
                    longApproach["distanceToDoorAfterSetup"] = Distance(mounts[0].Position, ApproachPoint);
                    firstStart = mounts[0].Position; Next(1); break;
                case 7:
                    // Sprint 2 click while moving: the second pair's mount is given an ordinary ground move, and the door
                    // is clicked while that move is still running. The accepted click must replace the move.
                    if (!Settled(false)) return false;
                    {
                        var search = new JObject(); clickWhileMoving["goalSearch"] = search;
                        var lateral = Vector3.Cross(Vector3.up, (ApproachPoint - mounts[1].Position).normalized);
                        var goal = SearchClearGoal(mounts[1], riders[1], lateral, LateralMoveDistanceMetres, 0.5f, search, out var found);
                        if (!found) { clickWhileMoving["reason"] = "no clear lateral native goal"; Next(1); break; }
                        clickWhileMoving["goal"] = Point(goal); lateralMove = IssueGroundMove(1, goal); clickWhileMoving["moveCommand"] = Id(lateralMove);
                        lateralMovingFrames = 0; Next(8);
                    }
                    break;
                case 8:
                    if (lateralMove.IsFinished) { clickWhileMoving["reason"] = "the lateral move finished before the mount was observed moving"; Next(1); break; }
                    if (!mounts[1].View.AgentASP.IsReallyMoving) return false;
                    if (++lateralMovingFrames < 3) return false;
                    clickWhileMoving["applicable"] = true; clickWhileMoving["mountPositionAtClick"] = Point(mounts[1].Position);
                    clickWhileMoving["moveRunningAtClick"] = !lateralMove.IsFinished;
                    ObserveCut("input-ready-" + pairIndex); BeginInput(true); Next(2); break;
                case 1:
                    if (!Settled(false)) return false;
                    ObserveCut("input-ready-" + pairIndex); BeginInput(false); Next(2); break;
                case 2:
                    if (!FinishInput()) return false;
                    if (!Settled(true) || !actors.All(Chunk7NativeFixture.Idle)) return false;
                    ObserveCut("after-click-" + pairIndex);
                    if (pairIndex == 0) { BeginReturn(); Next(3); }
                    else { Next(4); }
                    break;
                case 3:
                    // The outcome is recorded every poll, so a timeout or refusal keeps the last native state.
                    ObserveReturn();
                    RequireOtherIdle(0);
                    if (!returnMove.IsFinished || !actors.All(Chunk7NativeFixture.Idle)) return false;
                    Require(returnMove.Result == UnitCommand.ResultType.Success && Distance(mounts[0].Position, returnGoal) <= 1.25f,
                        "First pair did not finish its native return movement to its clear return goal before the second interaction: " +
                        returnMove.Result + ", " + Distance(mounts[0].Position, returnGoal).ToString("F3") + " m from the goal.");
                    pairIndex = 1; CloseForInput(); Next(7); break;
                case 4:
                    if (!Drain()) return false;
                    Completed = true; evidence["completed"] = true; return true;
            }
            return false;
        }

        private void CloseForInput()
        {
            RequirePairs(); RequireDoor();
            Require(actors.All(Chunk7NativeFixture.Idle) && door.GetState(), "Door fixture closing requires idle actors and an open door.");
            if (!door.CanInteract() && MountedDistanceDoorFixturePolicy.CanTemporarilyEnable(
                door.CanInteract(), door.Enabled, door.DisableOnOpen, Game.Instance.Player.IsInCombat)) door.Enabled = true;
            Require(door.CanInteract(), "Native door is not interactable for fixture preparation.");
            door.Interact(riders[pairIndex]); // Native fixture preparation, not a player-input assertion.
            evidence["setupInteractions"] = (int)evidence["setupInteractions"] + 1; settledFrame = -1;
        }

        private void BeginInput(bool mountMoving)
        {
            RequirePairs(); RequireDoor();
            var rider = riders[pairIndex]; var mount = mounts[pairIndex]; var other = riders[1 - pairIndex];
            // Click while moving (Sprint 2): the active pair's mount is still carrying out its ordinary ground move;
            // every other actor is idle.
            Require(actors.Where(a => !mountMoving || !ReferenceEquals(a, mount) && !ReferenceEquals(a, rider)).All(Chunk7NativeFixture.Idle) &&
                !door.GetState() && door.CanInteract(), "Door click preconditions changed.");
            if (mountMoving) Require(lateralMove != null && !lateralMove.IsFinished && ReferenceEquals(mount.Commands.Move, lateralMove) &&
                mount.View.AgentASP.IsReallyMoving, "The click-while-moving mount is not carrying out its ordinary move.");
            Require(Distance(mount.Position, door.transform.position) > Radius(door) + 0.5f,
                "Mounted door input must start outside native immediate interaction range.");
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(rider);
            interactionStart = mount.Position;
            var accepted = false;
            try { accepted = new ClickMapObjectHandler().OnClick(door.gameObject, door.transform.position, 0, false, false); }
            finally {
                // Capture custody even if native click dispatch throws after publication.
                foreach (var native in rider.Commands.Raw.OfType<MountedDoorInteractionCommand>().Where(x => ReferenceEquals(x.Door, door)))
                    if (!commands.Contains(native)) commands.Add(native);
            }
            command = rider.Commands.Raw.OfType<MountedDoorInteractionCommand>().SingleOrDefault(x => ReferenceEquals(x.Door, door));
            Require(accepted && command != null && ReferenceEquals(command.Rider, rider) && ReferenceEquals(command.Mount, mount) &&
                ReferenceEquals(command.Executor, rider) && world.ForActor(rider).Combat.HasActiveDoorInteraction,
                "Native click did not admit exactly the captured rider/pair door command.");
            RequireOtherIdle(pairIndex);
            row = new JObject { ["pair"] = pairIndex, ["riderId"] = rider.UniqueId, ["mountId"] = mount.UniqueId,
                ["ownerId"] = owners[pairIndex].Id.ToString(), ["generation"] = generations[pairIndex],
                ["commandObject"] = Id(command), ["executorId"] = command.Executor.UniqueId,
                ["distanceBefore"] = Distance(interactionStart, door.transform.position), ["radius"] = Radius(door),
                ["selectionAfterInput"] = other.UniqueId, ["accepted"] = accepted, ["unrelatedIdleAtInput"] = true };
            row["clickWhileMoving"] = mountMoving;
            if (mountMoving)
            {
                // The accepted click replaced the mount's running move natively: that move is finished and the
                // pair's new approach owns the slot.
                clickWhileMoving["moveFinishedAtAdmission"] = lateralMove.IsFinished; clickWhileMoving["moveResult"] = lateralMove.Result.ToString();
                clickWhileMoving["replacedMoveCount"] = world.ForActor(rider).Combat.DoorInteractionReplacedMountMoveCount;
                Require(lateralMove.IsFinished, "The door click did not replace the mount's running ordinary move.");
            }
            interactions.Add(row); Chunk7NativeFixture.Select(other); Game.Instance.IsPaused = false;
        }

        private bool FinishInput()
        {
            RequireOtherIdle(pairIndex);
            var selected = SelectionManager.Instance.SelectedUnits;
            Require(selected.Count == 1 && ReferenceEquals(selected[0], riders[1 - pairIndex]), "Door execution changed the deliberately different selection.");
            var pair = world.ForActor(riders[pairIndex]);
            if (!command.IsFinished || pair.Combat.HasActiveDoorInteraction || !mounts[pairIndex].Commands.Empty) return false;
            var outcome = pair.Combat.LastDoorInteractionOutcome;
            // Explicit members: bare JObject.FromObject in the game uses the global JsonConvert.DefaultSettings
            // and serialized this outcome as an empty {"$id":"1"} shell (batch192c). Recorded before the
            // assertion, so a refused interaction keeps its native outcome.
            row["outcome"] = outcome == null ? null : new JObject { ["Result"] = outcome.Result, ["RiderId"] = outcome.RiderId, ["MountId"] = outcome.MountId,
                ["InteractionCount"] = outcome.InteractionCount, ["DelegatedMoveStartCount"] = outcome.DelegatedMoveStartCount,
                ["DelegatedMoveTickCount"] = outcome.DelegatedMoveTickCount, ["DoorStateChanged"] = outcome.DoorStateChanged,
                ["RiderPathSuppressed"] = outcome.RiderPathSuppressed, ["MountMoveSlotRestored"] = outcome.MountMoveSlotRestored,
                ["TerminalReason"] = outcome.TerminalReason,
                ["ApproachesWithoutProgress"] = outcome.ApproachesWithoutProgress, ["ApproachElapsedGameSeconds"] = outcome.ApproachElapsedGameSeconds,
                ["DistanceAtInteraction"] = outcome.DistanceAtInteraction, ["LineOfSightAtInteraction"] = outcome.LineOfSightAtInteraction,
                ["OccupancyInsideNavmesh"] = outcome.OccupancyInsideNavmesh, ["OccupancyRecovery"] = outcome.OccupancyRecovery,
                ["OccupancyNearestNodeDistance"] = outcome.OccupancyNearestNodeDistance };
            row["terminal"] = command.IsFinished;
            row["distanceMoved"] = Distance(interactionStart, mounts[pairIndex].Position);
            // Sprint 2 (SPACE-01): the pair's logical occupancy after the door changed state, recorded before the assertion.
            row["occupancy"] = Occupancy(pairIndex);
            Require(outcome != null && outcome.Result == "Success" && outcome.RiderId == riders[pairIndex].UniqueId &&
                outcome.MountId == mounts[pairIndex].UniqueId && outcome.InteractionCount == 1 && outcome.DelegatedMoveStartCount >= 1 &&
                outcome.DoorStateChanged && outcome.RiderPathSuppressed && outcome.MountMoveSlotRestored && door.GetState() &&
                Distance(interactionStart, mounts[pairIndex].Position) >= 0.5f,
                "Captured native door input did not prove one approach, one interaction and restored mount movement.");
            Require(outcome.DistanceAtInteraction <= Radius(door) + 0.01f && outcome.LineOfSightAtInteraction,
                "The interaction was not delivered from inside the native proximity radius with line of sight to the door.");
            // The mount is the pair's logical footprint; the carried rider's entity position sits at saddle height and is recorded only.
            Require(outcome.OccupancyInsideNavmesh == true && outcome.OccupancyRecovery == "none" && (bool)row["occupancy"]["mountInsideNavmesh"],
                "The pair left the navmesh at the door state change: " + row["occupancy"].ToString(Newtonsoft.Json.Formatting.None));
            row["ownerContinuity"] = true; row["unrelatedIdleAtCompletion"] = true;
            CompletedInteractions++;
            return true;
        }

        private void BeginReturn()
        {
            RequirePairs(); Require(actors.All(Chunk7NativeFixture.Idle), "Door return requires settled interaction commands.");
            var search = SearchReturnGoal(out returnGoal, out var found);
            returnRecord = new JObject { ["command"] = 0, ["executorId"] = null, ["terminal"] = false, ["result"] = null,
                ["goal"] = found ? Point(returnGoal) : null, ["commandTarget"] = null, ["position"] = null,
                ["distanceToGoal"] = null, ["distanceToStart"] = null, ["goalSearch"] = search };
            evidence["firstPairReturn"] = returnRecord;
            Require(found, "No walkable native return goal near the first pair's captured start lies clear of every other unit's " +
                "native avoidance footprint and outside the door approach; the two-pair door return is uncovered on this fixture.");
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(riders[0]);
            try { ClickGroundHandler.MoveSelectedUnitsToPoint(returnGoal, false); }
            finally {
                returnMove = mounts[0].Commands.Move;
                if (returnMove != null && !commands.Contains(returnMove)) commands.Add(returnMove);
            }
            returnRecord["command"] = Id(returnMove); returnRecord["executorId"] = returnMove?.Executor?.UniqueId;
            if (returnMove != null) returnRecord["commandTarget"] = Point(returnMove.Target);
            Require(returnMove != null && ReferenceEquals(returnMove.Executor, mounts[0]), "First-pair return did not bind the actual mount.");
            RequireOtherIdle(0); Chunk7NativeFixture.Select(riders[1]); Game.Instance.IsPaused = false;
        }

        private void ObserveReturn()
        {
            var position = mounts[0].Position;
            returnRecord["terminal"] = returnMove.IsFinished; returnRecord["result"] = returnMove.Result.ToString();
            returnRecord["position"] = Point(position);
            returnRecord["distanceToGoal"] = Distance(position, returnGoal); returnRecord["distanceToStart"] = Distance(position, firstStart);
        }

        // The bounded, read-only return goal search (DoorReturnGoalPolicy). Every evaluated candidate is recorded with
        // the native values that decided it; the first admitted one is the goal. Nothing in the world is changed.
        private JObject SearchReturnGoal(out Vector3 goal, out bool found)
        {
            goal = firstStart; found = false;
            var mover = mounts[0];
            var moverCorpulence = mover.View.AgentASP.Corpulence;
            var doorPosition = door.transform.position; var radius = Radius(door);
            var units = Game.Instance.State.Units.Where(u => u != null && !ReferenceEquals(u, mover) && !ReferenceEquals(u, riders[0]) &&
                    u.IsInState && u.View != null && u.View.AgentASP != null &&
                    (ReferenceEquals(u, riders[1]) || ReferenceEquals(u, mounts[1]) ||
                     Distance(u.Position, firstStart) <= DoorReturnGoalPolicy.UnitConsiderationRadius))
                .OrderBy(u => u.UniqueId, StringComparer.Ordinal).ToArray();
            Require(units.Length <= DoorReturnGoalPolicy.MaximumUnits, "Door return goal unit scan exceeded its read-only bound.");
            Require(units.Contains(riders[1]) && units.Contains(mounts[1]), "Door return goal search lost the other pair's native actors.");
            var corpulences = units.Select(u => u.View.AgentASP.Corpulence).ToArray();
            var candidates = new JArray();
            for (var i = 0; i < DoorReturnGoalPolicy.CandidateCount && !found; i++)
            {
                DoorReturnGoalPolicy.Offset(i, out var ring, out var bearing, out var dx, out var dz);
                var requested = new Vector3(firstStart.x + dx, firstStart.y, firstStart.z + dz);
                var nearest = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(requested);
                var walkable = nearest.node != null && nearest.node.Walkable;
                var candidate = new JObject { ["index"] = i, ["ring"] = ring, ["bearing"] = bearing, ["requested"] = Point(requested), ["walkable"] = walkable };
                var reason = "no-walkable-native-node";
                if (walkable)
                {
                    var clamped = nearest.clampedPosition;
                    var clampResidual = Distance(requested, clamped);
                    var traceResidual = Distance(Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(firstStart, clamped), clamped);
                    var doorDistance = Distance(clamped, doorPosition);
                    var slack = double.PositiveInfinity; string blocker = null;
                    for (var u = 0; u < units.Length; u++)
                    {
                        var value = DoorReturnGoalPolicy.Slack(Distance(clamped, units[u].Position), moverCorpulence, corpulences[u]);
                        if (value < slack) { slack = value; blocker = units[u].UniqueId; }
                    }
                    reason = DoorReturnGoalPolicy.Reason(true, clampResidual, traceResidual, doorDistance, radius, ScanRadius, slack);
                    candidate["clamped"] = Point(clamped); candidate["clampResidual"] = clampResidual; candidate["traceResidual"] = traceResidual;
                    candidate["doorDistance"] = doorDistance; candidate["minimumSlack"] = slack; candidate["nearestBlockerId"] = blocker;
                    if (reason == "admitted") { goal = clamped; found = true; }
                }
                candidate["reason"] = reason; candidates.Add(candidate);
            }
            return new JObject { ["contract"] = DoorReturnGoalPolicy.Contract, ["start"] = Point(firstStart),
                ["moverId"] = mover.UniqueId, ["moverCorpulence"] = moverCorpulence, ["moverPosition"] = Point(mover.Position),
                ["margin"] = DoorReturnGoalPolicy.ClearanceMargin, ["doorPosition"] = Point(doorPosition), ["doorRadius"] = radius,
                ["scanRadius"] = ScanRadius, ["unitRadius"] = DoorReturnGoalPolicy.UnitConsiderationRadius,
                ["units"] = new JArray(units.Select((u, n) => new JObject { ["id"] = u.UniqueId, ["position"] = Point(u.Position),
                    ["corpulence"] = corpulences[n] })),
                ["candidates"] = candidates, ["selectedIndex"] = found ? candidates.Count - 1 : -1 };
        }

        // Cleanup needs the original world and exact door, not continued mounted
        // admission. Native lifecycle may detach a pair while its command settles.
        internal bool Drain()
        {
            scope.RequireCurrent();
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
                NativePersistenceIsolation.HasPendingWrites || world.SerializationBusy) return false;
            foreach (var owned in commands.Where(x => !x.IsFinished)) owned.Interrupt();
            if (commands.Any(x => !x.IsFinished)) return false;
            if (!captured || restored) return true;
            RequireDoor();
            // StandardDoor.Open toggles the native State (OnInteract calls it); it needs no
            // user, restriction check or out-of-combat state, so it serves both directions.
            if (door.GetState() != originalOpen) { door.Open(); settledFrame = -1; }
            if (!Settled(originalOpen)) return false;
            door.Enabled = originalEnabled;
            // Native interaction marks an unrestricted door AlreadyUnlocked (CheckRestrictions). The
            // flag is read only behind a restriction, so it is written back only for a door captured
            // with none (any other door was never admitted), and only when it actually changed.
            var before = door.Data;
            Require(before != null && originalRestrictionCount == 0, "Native door unlock state is not restorable without a behavioural effect.");
            var observedBefore = before.AlreadyUnlocked;
            if (observedBefore != originalAlreadyUnlocked) before.AlreadyUnlocked = originalAlreadyUnlocked;
            // Re-read through the native accessor; it returns the same data object, so this confirms
            // the write landed together with the open/enabled/cut state, not an independent store.
            var after = door.Data;
            Require(after != null && door.GetState() == originalOpen && door.Enabled == originalEnabled && cut.enabled == originalCut &&
                after.AlreadyUnlocked == originalAlreadyUnlocked,
                "Native door lease has not restored its original open/enabled/cut/unlock state.");
            ObserveCut("restored");
            restored = true; evidence["restoration"] = new JObject { ["exact"] = true, ["commandsSettled"] = true,
                ["open"] = door.GetState(), ["enabled"] = door.Enabled, ["cutEnabled"] = cut.enabled,
                ["observedAlreadyUnlockedBeforeRestore"] = observedBefore, ["alreadyUnlocked"] = after.AlreadyUnlocked };
            return true;
        }

        private void RequirePairs()
        {
            scope.RequireCurrent(); Require(!Game.Instance.Player.IsInCombat, "Exploration door probe entered combat.");
            for (var i = 0; i < owners.Length; i++)
                Require(owners[i] != null && ReferenceEquals(owners[i], world.Registry.FindActive(riders[i])) &&
                    ReferenceEquals(owners[i], world.Registry.FindActive(mounts[i])) && owners[i].Generation == generations[i] &&
                    owners[i].Boundary.State == RelationshipState.Mounted && owners[i].Boundary.Runtime.PoseHealthy,
                    "Door input lost its exact mounted owner or generation.");
        }
        private void RequireOtherIdle(int active) => Require(Chunk7NativeFixture.Idle(riders[1 - active]) &&
            Chunk7NativeFixture.Idle(mounts[1 - active]) && !world.ForActor(riders[1 - active]).Combat.HasActiveDoorInteraction,
            "One pair's door work changed the other pair's native commands.");
        private void RequireDoor() => Require(door != null && ReferenceEquals(door.gameObject, doorObject) &&
            ReferenceEquals(door.transform, doorTransform) && ReferenceEquals(door.GetComponentInChildren<NavmeshCut>(), cut) &&
            door.isActiveAndEnabled && door.gameObject.activeInHierarchy, "Captured native door/view/cut was replaced or left its area.");
        private bool Settled(bool open)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var playableField = typeof(StandardDoor).GetField("m_Playable", flags);
            var graphField = typeof(StandardDoor).GetField("m_Graph", flags);
            Require(playableField?.MetadataToken == 0x040012CE && graphField?.MetadataToken == 0x040012CD,
                "Exact installed StandardDoor animation fields changed.");
            var playable = (AnimationClipPlayable)playableField.GetValue(door); var graph = (PlayableGraph)graphField.GetValue(door);
            Require(playable.IsValid() && graph.IsValid() && door.ObstacleAnimation != null && AstarPath.active != null,
                "Door animation/navmesh observation is unavailable.");
            var animationDone = open ? playable.GetTime() >= door.ObstacleAnimation.length : playable.GetTime() <= 0;
            // The door never writes a static cut, so a change during the lease is a fault, not a wait.
            if (!cutToggles) Require(cut.enabled == originalCut, "Static door navmesh cut changed during the lease.");
            var cutSettled = cutToggles ? cut.enabled != open : cut.enabled == originalCut;
            // RequiresUpdate (wasEnabled != enabled, or an enabled cut moved, or rotated when useRotation) stays meaningful for both modes.
            if (door.GetState() != open || !cutSettled || !animationDone || cut.RequiresUpdate() ||
                AstarPath.active.IsAnyGraphUpdatesQueued || Time.frameCount <= Pathfinding.Util.TileHandler.LastUpdateFrame)
            { settledFrame = -1; return false; }
            if (settledFrame < 0) { settledFrame = Time.frameCount; return false; }
            return Time.frameCount > settledFrame;
        }
        // The cut state at each settled lease boundary, in order; the reader checks it against cutMode.
        private void ObserveCut(string boundary) => cutObservations.Add(new JObject { ["boundary"] = boundary,
            ["open"] = door.GetState(), ["cutEnabled"] = cut.enabled });
        private void Next(int value) { stage = value; stageStarted = Time.realtimeSinceStartup; settledFrame = -1; }
        private static float Radius(StandardDoor value) => value.ProximityRadius > 0f ? value.ProximityRadius : 4f;
        private static float Distance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static JObject Point(Vector3 value) => new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
