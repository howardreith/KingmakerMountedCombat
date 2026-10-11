using System;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View.MapObjects;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using Pathfinding;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private StandardDoor chunk6aDoor;
        private NavmeshCut chunk6aDoorCut;
        private bool chunk6aDoorOriginalOpen, chunk6aDoorOriginalEnabled, chunk6aDoorOriginalCut;
        private bool chunk6aDoorRestoreRequested;
        private Vector3 chunk6aDoorNear, chunk6aDoorFar;
        private int chunk6aDoorSetupStage, chunk6aDoorReadyFrame = -1;
        private UnitMoveTo chunk6aDoorGround;
        private NativeCommandPathProbe chunk6aPath;
        private JObject chunk6aDoorEvidence;
        private bool Chunk6aNeedsDoor => Chunk6aObstructionOnly;
        private static float DoorDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }

        private JObject CaptureChunk6aDoor()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var playableField = typeof(StandardDoor).GetField("m_Playable", flags);
            var graphField = typeof(StandardDoor).GetField("m_Graph", flags);
            if (playableField?.MetadataToken != 0x040012CE || graphField?.MetadataToken != 0x040012CD)
                throw new InvalidOperationException("Pinned StandardDoor playback fields changed.");
            var playable = (AnimationClipPlayable)playableField.GetValue(chunk6aDoor);
            var graph = (PlayableGraph)graphField.GetValue(chunk6aDoor);
            if (!playable.IsValid() || !graph.IsValid() || chunk6aDoor.ObstacleAnimation == null)
                throw new InvalidOperationException("Native door playback is unavailable.");
            return new JObject { ["frame"] = Time.frameCount, ["open"] = chunk6aDoor.GetState(),
                ["enabled"] = chunk6aDoor.Enabled, ["cutEnabled"] = chunk6aDoorCut.enabled,
                ["cutNeedsUpdate"] = chunk6aDoorCut.RequiresUpdate(), ["graphUpdatesQueued"] = AstarPath.active.IsAnyGraphUpdatesQueued,
                ["tileUpdateFrame"] = Pathfinding.Util.TileHandler.LastUpdateFrame,
                ["clipTime"] = playable.GetTime(), ["clipSpeed"] = playable.GetSpeed(), ["clipLength"] = chunk6aDoor.ObstacleAnimation.length,
                ["graphPlaying"] = graph.IsPlaying() };
        }
        private bool Chunk6aDoorSettled(bool open)
        {
            var state = CaptureChunk6aDoor();
            var animation = open ? (double)state["clipTime"] >= (double)state["clipLength"] : (double)state["clipTime"] <= 0;
            if ((bool)state["open"] != open || (bool)state["cutEnabled"] == open || !animation ||
                (bool)state["cutNeedsUpdate"] || (bool)state["graphUpdatesQueued"] || Time.frameCount <= (int)state["tileUpdateFrame"])
            { chunk6aDoorReadyFrame = -1; return false; }
            if (chunk6aDoorReadyFrame < 0) { chunk6aDoorReadyFrame = Time.frameCount; return false; }
            if (Time.frameCount <= chunk6aDoorReadyFrame) return false;
            state["settledFrame"] = chunk6aDoorReadyFrame;
            chunk6aDoorEvidence[open ? "openReady" : "closedReady"] = state;
            return true;
        }
        private UnitMoveTo Chunk6aDoorMove(UnitEntityData actor, Vector3 destination)
        {
            SelectionManager.Instance.SelectUnit(actor.View, true, true, false);
            var selected = SelectionManager.Instance.SelectedUnits;
            if (selected.Count != 1 || selected[0] != actor) throw new InvalidOperationException("Door setup lost exact ground-order selection.");
            chunk6aPath = new NativeCommandPathProbe(actor);
            ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false);
            var command = actor.Commands.Move;
            if (command?.Executor != actor || !command.CreatedByPlayer) throw new InvalidOperationException("Door setup admitted no exact native ground order.");
            chunk6aPath.Bind(command);
            return command;
        }
        private bool FinishChunk6aDoorMove(string name, UnitEntityData actor, Vector3 destination, bool requireCrossing)
        {
            if (!chunk6aDoorGround.IsFinished || !Chunk6aIdle) return false;
            var path = chunk6aPath.Capture(); chunk6aPath.Dispose(); chunk6aPath = null;
            var events = ((JArray)path["events"]).OfType<JObject>().ToArray();
            var crossed = events.Where(e => (string)e["boundary"] == "path-complete-after").Any(e => Chunk6aPathCrossesDoor((JArray)e["points"]));
            var evidence = new JObject { ["command"] = CaptureOrdinaryCommand(chunk6aDoorGround), ["path"] = path,
                ["crossedOpenDoor"] = crossed, ["distanceToDestination"] = DoorDistance(actor.Position, destination),
                ["destination"] = CapturePosition(destination), ["door"] = CaptureChunk6aDoor() };
            chunk6aDoorEvidence[name] = evidence;
            if (chunk6aDoorGround.Result != UnitCommand.ResultType.Success || DoorDistance(actor.Position, destination) > 1.25f ||
                !(bool)path["complete"] || !events.Any(e => (string)e["boundary"] == "command-ended") || requireCrossing && !crossed)
                throw new InvalidOperationException("Native door setup did not prove terminal arrival and its required open-door crossing: " + name);
            chunk6aDoorGround = null; return true;
        }
        private bool Chunk6aPathCrossesDoor(JArray points)
        {
            if (points == null || points.Count < 2) return false;
            var center = chunk6aDoor.transform.position;
            var normal = (chunk6aDoorFar - chunk6aDoorNear).normalized;
            var minSide = float.MaxValue; var maxSide = float.MinValue; var near = float.MaxValue;
            Vector3 previous = default(Vector3);
            for (var index = 0; index < points.Count; index++)
            {
                var p = new Vector3((float)points[index]["x"], 0, (float)points[index]["z"]);
                var c = new Vector3(center.x, 0, center.z);
                var side = Vector3.Dot(p - c, normal); minSide = Math.Min(minSide, side); maxSide = Math.Max(maxSide, side);
                if (index > 0)
                {
                    var delta = p - previous; var t = delta.sqrMagnitude == 0 ? 0 : Mathf.Clamp01(Vector3.Dot(c - previous, delta) / delta.sqrMagnitude);
                    near = Math.Min(near, Vector3.Distance(c, previous + delta * t));
                }
                previous = p;
            }
            return minSide < -0.25f && maxSide > 0.25f && near <= Math.Max(2.5f, horse.Corpulence + 1f);
        }
        private bool TickChunk6aDoorSetup()
        {
            if (chunk6aDoorSetupStage == 0)
            {
                chunk6aDoor = UnityEngine.Object.FindObjectsOfType<StandardDoor>()
                    .Where(d => d != null && d.isActiveAndEnabled && d.gameObject.activeInHierarchy && d.IsOpen &&
                        d.DisableNavmeshCutWhenOpen && DoorDistance(horse.Position, d.transform.position) <= 28f)
                    .OrderBy(d => DoorDistance(horse.Position, d.transform.position)).FirstOrDefault();
                if (chunk6aDoor == null) throw new InvalidOperationException("No bounded active open native StandardDoor fixture exists.");
                chunk6aDoorCut = chunk6aDoor.GetComponentInChildren<NavmeshCut>();
                if (chunk6aDoorCut == null) throw new InvalidOperationException("Selected native door has no navmesh cut.");
                chunk6aDoorOriginalOpen = chunk6aDoor.GetState(); chunk6aDoorOriginalEnabled = chunk6aDoor.Enabled; chunk6aDoorOriginalCut = chunk6aDoorCut.enabled;
                var center = chunk6aDoor.transform.position;
                var toward = horse.Position - center; toward.y = 0;
                var axis = new[] { chunk6aDoor.transform.forward, chunk6aDoor.transform.right }.Select(a => { a.y = 0; return a.normalized; })
                    .OrderByDescending(a => Math.Abs(Vector3.Dot(a, toward.normalized))).First();
                var clearance = Math.Max(3.5f, horse.Corpulence * 2f + 1f);
                chunk6aDoorNear = center - axis * clearance; chunk6aDoorFar = center + axis * clearance;
                if (DoorDistance(horse.Position, chunk6aDoorFar) < DoorDistance(horse.Position, chunk6aDoorNear))
                { var swap = chunk6aDoorNear; chunk6aDoorNear = chunk6aDoorFar; chunk6aDoorFar = swap; }
                chunk6aDoorEvidence = new JObject { ["contract"] = "native-open-door-crossing-then-closed-cut",
                    ["name"] = chunk6aDoor.name, ["center"] = CapturePosition(center), ["near"] = CapturePosition(chunk6aDoorNear), ["far"] = CapturePosition(chunk6aDoorFar),
                    ["originalOpen"] = chunk6aDoorOriginalOpen, ["originalEnabled"] = chunk6aDoorOriginalEnabled, ["originalCut"] = chunk6aDoorOriginalCut,
                    ["disableOnOpen"] = chunk6aDoor.DisableOnOpen, ["disableNavmeshCutWhenOpen"] = chunk6aDoor.DisableNavmeshCutWhenOpen };
                observations["chunk6aDoorFixture"] = chunk6aDoorEvidence;
                if (!chunk6aDoor.CanInteract() && MountedDistanceDoorFixturePolicy.CanTemporarilyEnable(
                    chunk6aDoor.CanInteract(), chunk6aDoor.Enabled, chunk6aDoor.DisableOnOpen, Game.Instance.Player.IsInCombat))
                { chunk6aDoor.Enabled = true; chunk6aDoorEvidence["temporaryEnableUsed"] = true; }
                if (!chunk6aDoor.CanInteract() || chunk6aDoorCut.enabled) throw new InvalidOperationException("The selected open door is not a lawful setup fixture.");
                chunk6aDoorGround = Chunk6aDoorMove(horse, chunk6aDoorFar);
                chunk6aDoorSetupStage = 1; ResetLeafClock(); return false;
            }
            if (chunk6aDoorSetupStage == 1)
            {
                if (!FinishChunk6aDoorMove("openHorseCrossing", horse, chunk6aDoorFar, true)) return false;
                chunk6aDoorGround = Chunk6aDoorMove(rider, chunk6aDoorNear);
                chunk6aDoorSetupStage = 2; ResetLeafClock(); return false;
            }
            if (chunk6aDoorSetupStage == 2)
            {
                if (!FinishChunk6aDoorMove("riderNearArrival", rider, chunk6aDoorNear, false)) return false;
                if (Chunk6aObstructionOnly) CloseChunk6aDoor();
                chunk6aDoorSetupStage = 3; ResetLeafClock(); return false;
            }
            return !Chunk6aObstructionOnly || Chunk6aDoorSettled(false);
        }
        private void CloseChunk6aDoor()
        {
            if (!chunk6aDoor.GetState() || !chunk6aDoor.CanInteract()) throw new InvalidOperationException("Prepared native door cannot admit its ordinary close interaction.");
            chunk6aDoor.Interact(rider); chunk6aDoorReadyFrame = -1;
            chunk6aDoorEvidence["closeInteraction"] = CaptureChunk6aDoor();
        }
        private bool RestoreChunk6aDoor()
        {
            if (chunk6aDoor == null || chunk6aDoorEvidence == null) return true;
            if (!chunk6aDoorRestoreRequested)
            {
                if (chunk6aDoor.GetState() != chunk6aDoorOriginalOpen) chunk6aDoor.Open();
                chunk6aDoor.Enabled = chunk6aDoorOriginalEnabled;
                chunk6aDoorRestoreRequested = true; chunk6aDoorReadyFrame = -1;
            }
            var exact = chunk6aDoor.GetState() == chunk6aDoorOriginalOpen && chunk6aDoor.Enabled == chunk6aDoorOriginalEnabled && chunk6aDoorCut.enabled == chunk6aDoorOriginalCut;
            var ready = exact && Chunk6aDoorSettled(chunk6aDoorOriginalOpen);
            chunk6aDoorEvidence["restoration"] = new JObject { ["exact"] = exact, ["ready"] = ready, ["state"] = CaptureChunk6aDoor() };
            return ready;
        }
    }
}
