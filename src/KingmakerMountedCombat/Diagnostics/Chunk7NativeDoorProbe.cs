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
        private StandardDoor door;
        private NavmeshCut cut;
        private object doorObject, doorTransform;
        private bool captured, originalEnabled, originalOpen, originalCut, originalAlreadyUnlocked, restored;
        private int originalRestrictionCount = -1;
        private int stage, pairIndex, settledFrame = -1;
        private double stageStarted;
        private Vector3 firstStart, interactionStart;
        private MountedDoorInteractionCommand command;
        private UnitMoveTo returnMove;
        private JObject row;
        private readonly JObject evidence = new JObject();
        internal bool Completed { get; private set; }
        internal JObject Evidence => evidence;
        internal UnitCommand[] IssuedCommands => commands.ToArray();

        internal Chunk7NativeDoorProbe(MountedWorldService world, Chunk7NativeWorldScope scope,
            UnitEntityData firstRider, UnitEntityData firstMount, UnitEntityData secondRider, UnitEntityData secondMount)
        {
            this.world = world; this.scope = scope;
            riders = new[] { firstRider, secondRider }; mounts = new[] { firstMount, secondMount };
            actors = new[] { firstRider, firstMount, secondRider, secondMount };
            owners = riders.Select(world.Registry.FindActive).ToArray();
            generations = owners.Select(x => x == null ? -1 : x.Generation).ToArray();
            evidence["contract"] = "two-pair-native-door-click-selection-change";
            evidence["interactions"] = interactions;
            evidence["setupInteractions"] = 0;
        }

        internal void Begin()
        {
            Require(stage == 0 && !captured, "Door probe may begin only once.");
            RequirePairs(); Require(actors.All(Chunk7NativeFixture.Idle), "Door probe requires idle native actors.");
            door = FindQualifyingDoor(mounts, out _);
            Require(door != null, "No bounded open or interactable closed native door outside both pair interaction radii; exact door fixture is required.");
            cut = door.GetComponentInChildren<NavmeshCut>();
            doorObject = door.gameObject; doorTransform = door.transform;
            var facts = DoorFacts.Capture(door, mounts);
            originalEnabled = door.Enabled; originalOpen = door.GetState(); originalCut = cut.enabled;
            originalAlreadyUnlocked = facts.AlreadyUnlocked; originalRestrictionCount = facts.RestrictionCount; captured = true;
            // An open door has its cut disabled; a closed door keeps it active.
            Require(originalCut != originalOpen, "Door navmesh cut does not match its native open state.");
            Require(facts.Eligible, "Captured door no longer satisfies the qualifying-door predicate.");
            // CanInteract reads Enabled: a closed start is clickable only while natively enabled.
            Require(originalOpen || originalEnabled, "A closed door start must be natively enabled.");
            evidence["fixture"] = new JObject { ["name"] = door.name, ["object"] = Id(door),
                ["position"] = Point(door.transform.position), ["originalOpen"] = originalOpen,
                ["originalEnabled"] = originalEnabled, ["originalCut"] = originalCut,
                ["disableOnOpen"] = door.DisableOnOpen, ["radius"] = Radius(door),
                ["interactable"] = facts.Interactable, ["availableInteractions"] = facts.AvailableInteractions,
                ["revealed"] = facts.Revealed, ["hasVisibleTrap"] = facts.HasVisibleTrap, ["trapActive"] = facts.TrapActive,
                ["originalAlreadyUnlocked"] = facts.AlreadyUnlocked, ["restrictionCount"] = facts.RestrictionCount,
                ["hasDialog"] = facts.HasDialog, ["dataPresent"] = facts.DataPresent,
                ["viewTriggerCount"] = facts.ViewTriggerCount, ["viewTriggerActions"] = facts.ViewTriggerActions };
            firstStart = mounts[0].Position;
            // A closed door is already in its input state; only an open one is closed as setup.
            if (originalOpen) CloseForInput();
            Next(1);
        }

        // The exact qualifying-door query Begin uses, as a read-only bounded scan: no
        // door is acquired, closed or changed. Applicability is decided only from it.
        // An open door is preferred; a closed door must be one a player could click. Every door
        // must be unrestricted and carry no door-view trigger actions (see DoorFacts).
        internal const float ScanRadius = 28f;
        internal static StandardDoor FindQualifyingDoor(UnitEntityData[] mounts, out int candidatesScanned)
        {
            var candidates = UnityEngine.Object.FindObjectsOfType<StandardDoor>() ?? new StandardDoor[0];
            candidatesScanned = candidates.Length;
            return candidates
                .Where(d => d != null && DoorFacts.Capture(d, mounts).Eligible)
                .OrderByDescending(d => d.GetState())
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
            internal bool Eligible => Active && DataPresent && DisableCutWhenOpen && CutPresent && RestrictionCount == 0 &&
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
                    Active = door.isActiveAndEnabled && door.gameObject.activeInHierarchy, Open = door.GetState(),
                    DisableCutWhenOpen = door.DisableNavmeshCutWhenOpen, CutPresent = door.GetComponentInChildren<NavmeshCut>() != null,
                    Interactable = door.CanInteract(), AvailableInteractions = ClickMapObjectHandler.HasAvailableInteractions(door.gameObject),
                    Revealed = view != null && view.Data != null && view.Data.IsRevealed,
                    HasVisibleTrap = door.HasVisibleTrap(), TrapActive = trap != null && trap.TrapActive,
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
                ["reason"] = "No open or player-clickable closed StandardDoor with an active-when-closed navmesh cut lies outside both pair interaction radii within the bounded scan radius.",
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
                case 1:
                    if (!Settled(false)) return false;
                    BeginInput(); Next(2); break;
                case 2:
                    if (!FinishInput()) return false;
                    if (!Settled(true) || !actors.All(Chunk7NativeFixture.Idle)) return false;
                    if (pairIndex == 0) { BeginReturn(); Next(3); }
                    else { Next(4); }
                    break;
                case 3:
                    RequireOtherIdle(0);
                    if (!returnMove.IsFinished || !actors.All(Chunk7NativeFixture.Idle)) return false;
                    Require(returnMove.Result == UnitCommand.ResultType.Success && Distance(mounts[0].Position, firstStart) <= 1.25f,
                        "First pair did not finish its native return movement before the second interaction.");
                    evidence["firstPairReturn"] = new JObject { ["command"] = Id(returnMove), ["executorId"] = returnMove.Executor.UniqueId,
                        ["terminal"] = true, ["result"] = returnMove.Result.ToString(), ["distanceToStart"] = Distance(mounts[0].Position, firstStart) };
                    pairIndex = 1; CloseForInput(); Next(1); break;
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

        private void BeginInput()
        {
            RequirePairs(); RequireDoor();
            Require(actors.All(Chunk7NativeFixture.Idle) && !door.GetState() && door.CanInteract(), "Door click preconditions changed.");
            var rider = riders[pairIndex]; var mount = mounts[pairIndex]; var other = riders[1 - pairIndex];
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
            Require(outcome != null && outcome.Result == "Success" && outcome.RiderId == riders[pairIndex].UniqueId &&
                outcome.MountId == mounts[pairIndex].UniqueId && outcome.InteractionCount == 1 && outcome.DelegatedMoveStartCount == 1 &&
                outcome.DoorStateChanged && outcome.RiderPathSuppressed && outcome.MountMoveSlotRestored && door.GetState() &&
                Distance(interactionStart, mounts[pairIndex].Position) >= 0.5f,
                "Captured native door input did not prove one approach, one interaction and restored mount movement.");
            row["outcome"] = JObject.FromObject(outcome); row["terminal"] = command.IsFinished;
            row["ownerContinuity"] = true; row["unrelatedIdleAtCompletion"] = true;
            row["distanceMoved"] = Distance(interactionStart, mounts[pairIndex].Position);
            return true;
        }

        private void BeginReturn()
        {
            RequirePairs(); Require(actors.All(Chunk7NativeFixture.Idle), "Door return requires settled interaction commands.");
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(riders[0]);
            try { ClickGroundHandler.MoveSelectedUnitsToPoint(firstStart, false); }
            finally {
                returnMove = mounts[0].Commands.Move;
                if (returnMove != null && !commands.Contains(returnMove)) commands.Add(returnMove);
            }
            Require(returnMove != null && ReferenceEquals(returnMove.Executor, mounts[0]), "First-pair return did not bind the actual mount.");
            RequireOtherIdle(0); Chunk7NativeFixture.Select(riders[1]); Game.Instance.IsPaused = false;
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
            // with none; any other door was never admitted.
            var data = door.Data;
            Require(data != null && originalRestrictionCount == 0, "Native door unlock state is not restorable without a behavioural effect.");
            data.AlreadyUnlocked = originalAlreadyUnlocked;
            Require(door.GetState() == originalOpen && door.Enabled == originalEnabled && cut.enabled == originalCut &&
                data.AlreadyUnlocked == originalAlreadyUnlocked,
                "Native door lease has not restored its original open/enabled/cut/unlock state.");
            restored = true; evidence["restoration"] = new JObject { ["exact"] = true, ["commandsSettled"] = true,
                ["open"] = door.GetState(), ["enabled"] = door.Enabled, ["cutEnabled"] = cut.enabled,
                ["alreadyUnlocked"] = data.AlreadyUnlocked };
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
            if (door.GetState() != open || cut.enabled == open || !animationDone || cut.RequiresUpdate() ||
                AstarPath.active.IsAnyGraphUpdatesQueued || Time.frameCount <= Pathfinding.Util.TileHandler.LastUpdateFrame)
            { settledFrame = -1; return false; }
            if (settledFrame < 0) { settledFrame = Time.frameCount; return false; }
            return Time.frameCount > settledFrame;
        }
        private void Next(int value) { stage = value; stageStarted = Time.realtimeSinceStartup; settledFrame = -1; }
        private static float Radius(StandardDoor value) => value.ProximityRadius > 0f ? value.ProximityRadius : 4f;
        private static float Distance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static JObject Point(Vector3 value) => new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
