using System;
using System.Linq;
using Kingmaker;
using Kingmaker.View;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        // Fixture selection is read-only. The chosen endpoint is still reached by
        // one ordinary native ground order whose Success and path are mandatory.
        private Vector3 FindChunk6aGroundDestination(float extraMeters)
        {
            if (AstarPath.active == null) throw new InvalidOperationException("Active native navigation graph is unavailable.");
            var origin = horse.Position; var center = rider.Position;
            var direction = origin - center; direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) direction = horse.View.transform.forward;
            direction.Normalize();
            var radius = rider.DistanceTo(horse) + extraMeters;
            var candidates = new JArray();
            var attempts = observations["chunk6aGroundDestinations"] as JArray;
            if (attempts == null) observations["chunk6aGroundDestinations"] = attempts = new JArray();
            attempts.Add(new JObject { ["frame"] = Time.frameCount, ["origin"] = CapturePosition(origin),
                ["rider"] = CapturePosition(center), ["requestedSeparation"] = radius, ["candidates"] = candidates });
            for (var index = 0; index < 24; index++)
            {
                var angle = index == 0 ? 0f : (index % 2 == 0 ? index : -index) * 7.5f;
                var requested = center + Quaternion.Euler(0, angle, 0) * direction * radius;
                var nearest = AstarPath.active.GetNearest(requested);
                var point = nearest.clampedPosition;
                var candidate = new JObject { ["requested"] = CapturePosition(requested), ["point"] = CapturePosition(point),
                    ["walkable"] = nearest.node != null && nearest.node.Walkable, ["eligible"] = false };
                candidates.Add(candidate);
                if (nearest.node == null || !nearest.node.Walkable) continue;
                var routeEnd = ObstacleAnalyzer.TraceAlongNavmesh(origin, point);
                var routeResidual = HorizontalDistance(routeEnd, point);
                var footprint = NativeGroundMovementObservation.CaptureFootprint(horse, point);
                var residuals = ((JArray)footprint["probes"]).Select(probe => (float)probe["residual"]).ToArray();
                var footprintResidual = residuals.Any(value => float.IsNaN(value) || float.IsInfinity(value)) ? float.NaN : residuals.Max();
                var blockers = Game.Instance.State.Units.Where(unit => unit != horse && unit.IsInState && unit.View != null &&
                    HorizontalDistance(point, unit.Position) < horse.View.Corpulence + unit.View.Corpulence + 0.05f)
                    .Select(unit => unit.UniqueId).ToArray();
                var eligible = NativeGroundFixturePolicy.IsClear(radius, HorizontalDistance(center, point),
                    HorizontalDistance(origin, point), routeResidual, footprintResidual, blockers.Length != 0);
                candidate["routeEnd"] = CapturePosition(routeEnd); candidate["routeResidual"] = routeResidual;
                candidate["footprint"] = footprint; candidate["blockers"] = new JArray(blockers); candidate["eligible"] = eligible;
                if (eligible) return point;
            }
            throw new InvalidOperationException("No bounded clear native endpoint preserves separation and the Horse footprint.");
        }
    }
}
