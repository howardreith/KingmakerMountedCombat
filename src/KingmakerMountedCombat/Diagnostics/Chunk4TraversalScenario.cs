using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Extends only the existing native movement harness for the Chunk 4 gates.
    internal sealed partial class RuntimeMovementScenarioEngine
    {
        private const string Chunk4SlopeRow = "mounted-pair-slope";
        private bool IsChunk4Traversal => request.Scenario == "chunk4-traversal-core" || request.Scenario == "chunk4-traversal-slope";
        private readonly JArray chunk4SlopeSamples = new JArray();
        private readonly JArray chunk4SlopeSurfaces = new JArray();
        private readonly JArray chunk4SlopeProbes = new JArray();
        private float chunk4SlopeMinimum;
        private float chunk4SlopeMaximum;
        private int chunk4SlopeDropped;
        private int chunk4SlopeLastFrame;
        private float chunk4SlopeStartY;
        private float chunk4SlopeRiderMove;

        private static IReadOnlyList<string> SelectChunk4TraversalRows(string scenario)
        {
            if (scenario == "chunk4-traversal-core") return new[] {
                "mounted-distance-door-interaction", "mounted-pair-doorway", "mounted-pair-turns-and-corners", "mounted-pair-party-formation" };
            if (scenario == "chunk4-traversal-slope") return new[] { Chunk4SlopeRow };
            return null;
        }

        private JObject CaptureChunk4TraversalConfiguration() => new JObject {
            ["enablePairedActivation"] = settings.EnablePairedActivation,
            ["enableUnifiedMountedTurn"] = settings.EnableUnifiedMountedTurn,
            ["enablePairedCommandScheduler"] = settings.EnablePairedCommandScheduler,
            ["enableDiagnosticOverlay"] = settings.EnableDiagnosticOverlay,
            ["overlayPresent"] = playerAction.OverlayPresent
        };

        private void AdvanceChunk4Slope()
        {
            if (rowPhase == 0)
            {
                chunk4SlopeSamples.Clear(); chunk4SlopeDropped = 0; chunk4SlopeLastFrame = -1;
                chunk4SlopeSurfaces.Clear(); chunk4SlopeProbes.Clear();
                chunk4SlopeMinimum = chunk4SlopeMaximum = chunk4SlopeStartY = mount.Position.y;
                chunk4SlopeRiderMove = rider.CombatState.Cooldown.MoveAction;
                ObserveChunk4SlopeSurfaces();
                BeginRadialNavigation(NavigationMode.Normal, null, "moving");
                rowPhase = 1; return;
            }
            if (rowPhase == 1 && PollNavigation())
            {
                assertions.Check(chunk4SlopeMaximum - chunk4SlopeMinimum >= 0.5f && chunk4SlopeSamples.Count >= 3 && chunk4SlopeDropped == 0,
                    "Native stock movement traversed at least half a metre of measured elevation with the real mount footprint.",
                    "No complete native slope traversal was measured; level ground is not slope evidence.");
                AssertRowMovementQuality(); BeginCleanup(CleanupTrigger.Manual);
            }
        }

        private static bool Chunk4PathContainsSlope(IList<Vector3> points) => points != null && points.Count >= 2 &&
            points.Max(point => point.y) - points.Min(point => point.y) >= 0.5f;

        // Read-only observations distinguish a flat fixture from rejected native
        // paths. These queries do not select destinations or change navigation.
        private void ObserveChunk4SlopeSurfaces()
        {
            var graph = global::AstarPath.active;
            if (graph == null) throw new InvalidOperationException("Native slope surface observation has no active graph.");
            foreach (var candidate in BuildRadialCandidates(mount.Position, mount.Orientation, false))
                foreach (var height in new[] { 0f, 4f, -4f })
                {
                    if (chunk4SlopeSurfaces.Count >= 72) throw new InvalidOperationException("Native slope surface observation exceeded its bound.");
                    var requested = candidate + Vector3.up * height;
                    var nearest = graph.GetNearest(requested);
                    chunk4SlopeSurfaces.Add(new JObject { ["frame"] = Time.frameCount,
                        ["requested"] = Chunk4SlopePoint(requested), ["nodePresent"] = nearest.node != null,
                        ["walkable"] = nearest.node == null ? (bool?)null : nearest.node.Walkable,
                        ["clamped"] = nearest.node == null ? (JToken)JValue.CreateNull() : Chunk4SlopePoint(nearest.clampedPosition) });
                }
        }

        private void ObserveChunk4SlopeProbe(Vector3 requested, Pathfinding.Path path)
        {
            if (currentRow != Chunk4SlopeRow) return;
            if (chunk4SlopeProbes.Count >= 24) throw new InvalidOperationException("Native slope path observation exceeded its bound.");
            var points = path?.vectorPath;
            var hasPoints = points != null && points.Count != 0;
            chunk4SlopeProbes.Add(new JObject { ["frame"] = Time.frameCount, ["requested"] = Chunk4SlopePoint(requested),
                ["endpoint"] = hasPoints ? (JToken)Chunk4SlopePoint(points[points.Count - 1]) : JValue.CreateNull(),
                ["points"] = points?.Count ?? 0, ["minimumY"] = hasPoints ? (double?)points.Min(point => point.y) : null,
                ["maximumY"] = hasPoints ? (double?)points.Max(point => point.y) : null,
                ["pathError"] = path == null ? (bool?)null : path.error,
                ["accepted"] = probeCallbackAccepted, ["reason"] = probeCallbackReason });
        }

        private static JArray Chunk4SlopePoint(Vector3 point) => new JArray((double)point.x, (double)point.y, (double)point.z);

        private void ObserveChunk4Slope()
        {
            if (currentRow != Chunk4SlopeRow || mount?.View?.AgentASP == null || !mount.View.AgentASP.IsReallyMoving ||
                chunk4SlopeLastFrame == Time.frameCount) return;
            chunk4SlopeLastFrame = Time.frameCount;
            var point = mount.Position;
            chunk4SlopeMinimum = Math.Min(chunk4SlopeMinimum, point.y); chunk4SlopeMaximum = Math.Max(chunk4SlopeMaximum, point.y);
            if (chunk4SlopeSamples.Count >= 512) { chunk4SlopeDropped++; return; }
            chunk4SlopeSamples.Add(new JObject { ["frame"] = Time.frameCount,
                ["position"] = new JArray(point.x, point.y, point.z), ["stockAgentEnabled"] = mount.View.AgentASP.enabled,
                ["avoidanceDisabled"] = mount.View.AgentASP.AvoidanceDisabled, ["corpulence"] = mount.View.Corpulence,
                ["riderMove"] = rider.CombatState.Cooldown.MoveAction });
        }

        private JObject CaptureChunk4Slope() => new JObject { ["startY"] = chunk4SlopeStartY,
            ["riderMoveBefore"] = chunk4SlopeRiderMove, ["minimumY"] = chunk4SlopeMinimum,
            ["maximumY"] = chunk4SlopeMaximum, ["heightChange"] = chunk4SlopeMaximum - chunk4SlopeMinimum,
            ["discovery"] = new JObject { ["surfaces"] = chunk4SlopeSurfaces.DeepClone(), ["probes"] = chunk4SlopeProbes.DeepClone() },
            ["dropped"] = chunk4SlopeDropped, ["samples"] = chunk4SlopeSamples.DeepClone() };
    }
}
