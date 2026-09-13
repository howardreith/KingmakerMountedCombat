using System;
using System.Reflection;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View.MapObjects;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KingmakerMountedCombat.Diagnostics
{
    // Unregistered extension of the existing exact StandardDoor fixture lease.
    internal sealed partial class RuntimeMovementScenarioEngine
    {
        private int chunk4BlockedDoorStage;
        private UnitCommand chunk4BlockedDoorMove;
        private UnitCommand chunk4BlockedDoorReturn;
        private Vector3 chunk4BlockedDoorHome;
        private double chunk4BlockedDoorStarted;
        private double chunk4BlockedDoorSampleAt;
        private JObject chunk4BlockedDoorEvidence;
        private readonly JArray chunk4BlockedDoorSamples = new JArray();
        private JObject chunk4DoorClosingInitial;
        private double chunk4DoorClosingStarted;
        private int chunk4DoorSettledFrame = -1;
        private int chunk4DoorClosingObservations;

        private JObject CaptureChunk4DoorPlayback()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var playableField = typeof(StandardDoor).GetField("m_Playable", flags);
            var graphField = typeof(StandardDoor).GetField("m_Graph", flags);
            if (playableField == null || playableField.MetadataToken != 0x040012CE ||
                graphField == null || graphField.MetadataToken != 0x040012CD)
                throw new InvalidOperationException("Native StandardDoor playback observation contract changed.");
            var playable = (AnimationClipPlayable)playableField.GetValue(selectedDoor);
            var graph = (PlayableGraph)graphField.GetValue(selectedDoor);
            if (!playable.IsValid() || !graph.IsValid() || selectedDoor.ObstacleAnimation == null)
                throw new InvalidOperationException("The selected native door has no valid closing animation.");
            var time = playable.GetTime();
            var speed = playable.GetSpeed();
            var length = selectedDoor.ObstacleAnimation.length;
            if (double.IsNaN(time) || double.IsInfinity(time) || speed != -1d ||
                float.IsNaN(length) || float.IsInfinity(length) || length <= 0f ||
                time > 0d && !graph.IsPlaying())
                throw new InvalidOperationException("The native door is not completing its ordinary backward closing animation.");
            return new JObject { ["frame"] = Time.frameCount, ["time"] = time, ["speed"] = speed,
                ["clipLength"] = length, ["graphPlaying"] = graph.IsPlaying() };
        }

        private JObject CaptureChunk4BlockedDoor() => new JObject {
            ["frame"] = Time.frameCount, ["position"] = new JArray(mount.Position.x,mount.Position.y,mount.Position.z),
            ["riderPosition"] = new JArray(rider.Position.x,rider.Position.y,rider.Position.z),
            ["farDistance"] = PlanarDistance(mount.Position,doorFarPoint), ["homeDistance"] = PlanarDistance(mount.Position,chunk4BlockedDoorHome),
            ["doorOpen"] = selectedDoor.GetState(), ["cutEnabled"] = distanceDoorNavmeshCut?.enabled,
            ["doorAnimationTime"] = CaptureChunk4DoorPlayback()["time"],
            ["cutNeedsUpdate"] = distanceDoorNavmeshCut?.RequiresUpdate(), ["reallyMoving"] = mount.View.AgentASP.IsReallyMoving,
            ["agentEnabled"] = mount.View.AgentASP.enabled, ["avoidanceDisabled"] = mount.View.AgentASP.AvoidanceDisabled,
            ["corpulence"] = mount.View.Corpulence, ["riderMove"] = rider.CombatState.Cooldown.MoveAction,
            ["mountMove"] = mount.CombatState.Cooldown.MoveAction, ["riderStandard"] = rider.CombatState.Cooldown.StandardAction,
            ["mountStandard"] = mount.CombatState.Cooldown.StandardAction,
            ["moveStarted"] = chunk4BlockedDoorMove?.IsStarted, ["moveFinished"] = chunk4BlockedDoorMove?.IsFinished,
            ["moveResult"] = chunk4BlockedDoorMove?.Result.ToString(), ["pathError"] = mount.View.AgentASP.Path?.error,
            ["pathPoints"] = mount.View.AgentASP.Path?.vectorPath?.Count,
            ["pathState"] = mount.View.AgentASP.Path?.CompleteState.ToString()
        };

        private bool PollChunk4BlockedDoor()
        {
            if (chunk4BlockedDoorStage == 0)
            {
                if (distanceDoorNavmeshCut == null || !distanceDoorNavmeshCut.enabled || selectedDoor.GetState())
                    throw new InvalidOperationException("Blocked route requires the actual closed door and active native cut.");
                if (chunk4DoorClosingInitial == null || suiteClock.Elapsed.TotalSeconds - chunk4DoorClosingStarted > 30d)
                    throw new InvalidOperationException("Native closed-door preparation did not settle within its bounded observation.");
                var playback = CaptureChunk4DoorPlayback();
                chunk4DoorClosingObservations++;
                if ((double)playback["time"] > 0d || distanceDoorNavmeshCut.RequiresUpdate() ||
                    AstarPath.active.IsAnyGraphUpdatesQueued || Time.frameCount <= Pathfinding.Util.TileHandler.LastUpdateFrame)
                {
                    chunk4DoorSettledFrame = -1;
                    return false;
                }
                // State flips before the backward clip finishes. Observe its endpoint
                // and a subsequent native frame, including all resulting tile work.
                if (chunk4DoorSettledFrame < 0) { chunk4DoorSettledFrame = Time.frameCount; return false; }
                if (Time.frameCount <= chunk4DoorSettledFrame) return false;
                chunk4BlockedDoorHome = mount.Position;
                chunk4BlockedDoorEvidence = new JObject { ["level"] = "NATIVE INTEGRATION",
                    ["caseId"] = "C4-TRAVERSAL-closed-door-stop-return", ["rider"] = rider.UniqueId, ["mount"] = mount.UniqueId,
                    ["closing"] = new JObject { ["initial"] = chunk4DoorClosingInitial, ["ready"] = playback,
                        ["settledFrame"] = chunk4DoorSettledFrame, ["observations"] = chunk4DoorClosingObservations,
                        ["elapsed"] = suiteClock.Elapsed.TotalSeconds - chunk4DoorClosingStarted },
                    ["before"] = CaptureChunk4BlockedDoor(),
                    ["destination"] = new JArray(doorFarPoint.x,doorFarPoint.y,doorFarPoint.z) };
                ClickGroundHandler.MoveSelectedUnitsToPoint(doorFarPoint, false);
                TrackTouched(mount); chunk4BlockedDoorMove = mount.Commands.Move;
                if (chunk4BlockedDoorMove == null || chunk4BlockedDoorMove.Executor != mount || rider.Commands.Move != null)
                    throw new InvalidOperationException("Closed-door native order lost mount movement authority.");
                chunk4BlockedDoorEvidence["moveType"] = chunk4BlockedDoorMove.GetType().FullName;
                chunk4BlockedDoorEvidence["moveExecutor"] = chunk4BlockedDoorMove.Executor.UniqueId;
                chunk4BlockedDoorStarted = suiteClock.Elapsed.TotalSeconds; chunk4BlockedDoorSampleAt = double.MinValue;
                chunk4BlockedDoorStage = 1; return false;
            }
            if (chunk4BlockedDoorStage == 1)
            {
                var elapsed = suiteClock.Elapsed.TotalSeconds - chunk4BlockedDoorStarted;
                var state = CaptureChunk4BlockedDoor();
                if ((bool)state["doorOpen"] || !(bool)state["cutEnabled"] || (bool)state["cutNeedsUpdate"] ||
                    (double)state["doorAnimationTime"] > 0d || !(bool)state["agentEnabled"] || (bool)state["avoidanceDisabled"] ||
                    !JToken.DeepEquals(state["corpulence"], chunk4BlockedDoorEvidence["before"]["corpulence"]))
                    throw new InvalidOperationException("Closed-door route changed native collision or its actual door state.");
                if (suiteClock.Elapsed.TotalSeconds - chunk4BlockedDoorSampleAt >= 0.1d)
                {
                    chunk4BlockedDoorSampleAt = suiteClock.Elapsed.TotalSeconds;
                    if (chunk4BlockedDoorSamples.Count >= 512) throw new InvalidOperationException("Blocked-door observation exceeded its sample bound.");
                    chunk4BlockedDoorSamples.Add(state);
                }
                if (elapsed < 2d || !chunk4BlockedDoorMove.IsFinished && elapsed < 30d) return false;
                if ((double)state["farDistance"] <= 1.25d)
                    throw new InvalidOperationException("The real closed-door order found a traversable route; this is not blocked-route evidence.");
                chunk4BlockedDoorEvidence["beforeStop"] = state;
                chunk4BlockedDoorEvidence["elapsed"] = elapsed;
                chunk4BlockedDoorEvidence["samples"] = chunk4BlockedDoorSamples.DeepClone();
                SelectionManager.Instance.Stop();
                var after = CaptureChunk4BlockedDoor();
                foreach (var cost in new[] { "riderMove", "mountMove", "riderStandard", "mountStandard" })
                    if (!JToken.DeepEquals(state[cost], after[cost])) throw new InvalidOperationException("Blocked-route Stop changed a native cost.");
                chunk4BlockedDoorEvidence["afterStopInput"] = after; chunk4BlockedDoorStage = 2; return false;
            }
            if (chunk4BlockedDoorStage == 2)
            {
                if (!mount.Commands.Empty || !rider.Commands.Empty || mount.View.AgentASP.IsReallyMoving || combat.HasActiveGroundMovement) return false;
                chunk4BlockedDoorEvidence["afterStop"] = CaptureChunk4BlockedDoor();
                ClickGroundHandler.MoveSelectedUnitsToPoint(chunk4BlockedDoorHome, false);
                chunk4BlockedDoorReturn = mount.Commands.Move;
                if (chunk4BlockedDoorReturn == null || chunk4BlockedDoorReturn.Executor != mount)
                    throw new InvalidOperationException("Native return order was not available after blocked-route Stop.");
                chunk4BlockedDoorStage = 3; return false;
            }
            if (chunk4BlockedDoorStage == 3)
            {
                if (!chunk4BlockedDoorReturn.IsFinished || mount.View.AgentASP.IsReallyMoving || !mount.Commands.Empty || !rider.Commands.Empty) return false;
                if (chunk4BlockedDoorReturn.Result != UnitCommand.ResultType.Success || PlanarDistance(mount.Position,chunk4BlockedDoorHome) > 1.25d ||
                    PlanarDistance(mount.Position,selectedDoor.transform.position) <= selectedDoor.ProximityRadius + 0.5f)
                    throw new InvalidOperationException("Real return movement did not restore the legal distant-door control position.");
                chunk4BlockedDoorEvidence["afterReturn"] = CaptureChunk4BlockedDoor();
                chunk4BlockedDoorEvidence["returnResult"] = chunk4BlockedDoorReturn.Result.ToString();
                chunk4BlockedDoorStage = 4;
            }
            return chunk4BlockedDoorStage == 4;
        }
    }
}
