using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
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
        private bool captured, originalEnabled, originalOpen, originalCut, restored;
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
            Require(door != null, "No bounded open native door outside both pair interaction radii; exact door fixture is required.");
            cut = door.GetComponentInChildren<NavmeshCut>();
            doorObject = door.gameObject; doorTransform = door.transform;
            originalEnabled = door.Enabled; originalOpen = door.GetState(); originalCut = cut.enabled; captured = true;
            Require(!originalCut, "Open door retained an active navmesh cut.");
            evidence["fixture"] = new JObject { ["name"] = door.name, ["object"] = Id(door),
                ["position"] = Point(door.transform.position), ["originalOpen"] = originalOpen,
                ["originalEnabled"] = originalEnabled, ["originalCut"] = originalCut,
                ["disableOnOpen"] = door.DisableOnOpen, ["radius"] = Radius(door) };
            firstStart = mounts[0].Position;
            CloseForInput(); Next(1);
        }

        // The exact qualifying-door query Begin uses, as a read-only bounded scan: no
        // door is acquired, closed or changed. Applicability is decided only from it.
        internal const float ScanRadius = 28f;
        internal static StandardDoor FindQualifyingDoor(UnitEntityData[] mounts, out int candidatesScanned)
        {
            var candidates = UnityEngine.Object.FindObjectsOfType<StandardDoor>() ?? new StandardDoor[0];
            candidatesScanned = candidates.Length;
            return candidates
                .Where(d => d != null && d.isActiveAndEnabled && d.gameObject.activeInHierarchy && d.GetState() &&
                    d.DisableNavmeshCutWhenOpen && d.GetComponentInChildren<NavmeshCut>() != null &&
                    mounts.All(m => Distance(m.Position, d.transform.position) > Radius(d) + 0.5f &&
                        Distance(m.Position, d.transform.position) <= ScanRadius))
                .OrderBy(d => mounts.Sum(m => Distance(m.Position, d.transform.position))).FirstOrDefault();
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
            var rows = new JArray();
            foreach (var door in present
                .OrderBy(d => mounts.Sum(m => Distance(m.Position, d.transform.position)))
                .ThenBy(d => d.name, StringComparer.Ordinal).Take(32))
            {
                var near = mounts.Select(m => Distance(m.Position, door.transform.position)).ToArray();
                var cut = door.GetComponentInChildren<NavmeshCut>();
                var active = door.isActiveAndEnabled && door.gameObject.activeInHierarchy;
                var open = door.GetState();
                var eligible = active && open && door.DisableNavmeshCutWhenOpen && cut != null &&
                    near.All(distance => distance > Radius(door) + 0.5f && distance <= ScanRadius);
                rows.Add(new JObject { ["name"] = door.name, ["position"] = Point(door.transform.position),
                    ["active"] = active, ["open"] = open, ["disableCutWhenOpen"] = door.DisableNavmeshCutWhenOpen,
                    ["cutPresent"] = cut != null, ["proximityRadius"] = Radius(door),
                    ["mountDistances"] = new JArray(near[0], near[1]), ["eligible"] = eligible });
            }
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
            if (!door.GetState()) { door.Open(); settledFrame = -1; }
            if (!Settled(originalOpen)) return false;
            door.Enabled = originalEnabled;
            Require(door.GetState() == originalOpen && door.Enabled == originalEnabled && cut.enabled == originalCut,
                "Native door lease has not restored its original open/enabled/cut state.");
            restored = true; evidence["restoration"] = new JObject { ["exact"] = true, ["commandsSettled"] = true,
                ["open"] = door.GetState(), ["enabled"] = door.Enabled, ["cutEnabled"] = cut.enabled };
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
