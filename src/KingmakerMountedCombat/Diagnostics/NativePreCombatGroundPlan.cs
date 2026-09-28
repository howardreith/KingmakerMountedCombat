using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    // Read-only planning. At most 72 mount proposals, each with at most 72 rider
    // proposals. The ordinary rider search remains independently capped at 72.
    internal static class NativePreCombatGroundPlan
    {
        internal const int SearchLimit = 72;
        private static JObject Point(Vector3 p) => new JObject { ["x"] = p.x, ["y"] = p.y, ["z"] = p.z };
        internal static Vector3 Position(JToken p) => new Vector3((float)p["x"], (float)p["y"], (float)p["z"]);
        private static float Distance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        internal static JObject Search(UnitEntityData mover, UnitEntityData partner, Vector3 center,
            float clearance, bool joint, Vector3? partnerPosition = null)
        {
            if (AstarPath.active == null || mover?.View == null || partner?.View == null)
                throw new InvalidOperationException("Native ground planning requires the exact pair and navigation graph.");
            var origin = mover.Position; var direction = origin - center; direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) throw new InvalidOperationException("Ground planning geometry is degenerate.");
            direction.Normalize();
            var occupants = new JArray(Game.Instance.State.Units.Where(u => u.IsInState && u.View != null)
                .Select(u => new JObject { ["actorId"] = u.UniqueId, ["position"] = Point(u == partner && partnerPosition.HasValue ? partnerPosition.Value : u.Position),
                    ["corpulence"] = u.View.Corpulence }));
            var candidates = new JArray();
            var search = new JObject { ["contract"] = "bounded-native-ground-plan", ["actorId"] = mover.UniqueId,
                ["partnerId"] = partner.UniqueId, ["origin"] = Point(origin), ["center"] = Point(center),
                ["corpulence"] = mover.View.Corpulence, ["clearanceRadius"] = clearance, ["joint"] = joint,
                ["partnerPositionOverride"] = partnerPosition.HasValue ? Point(partnerPosition.Value) : null,
                ["occupants"] = occupants, ["maximumCandidates"] = SearchLimit, ["candidates"] = candidates,
                ["selectedIndex"] = -1 };
            foreach (var radius in new[] { 2.3f, 2.65f, 2f })
                for (var index = 0; index < 24; index++)
                {
                    var angle = index == 0 ? 0f : (index % 2 == 0 ? index : -index) * 15f;
                    var requested = center + Quaternion.Euler(0, angle, 0) * direction * radius;
                    var nearest = AstarPath.active.GetNearest(requested); var point = nearest.clampedPosition;
                    var c = new JObject { ["requested"] = Point(requested), ["point"] = Point(point),
                        ["walkable"] = nearest.node != null && nearest.node.Walkable,
                        ["requestedSeparation"] = radius, ["eligible"] = false };
                    candidates.Add(c);
                    if (nearest.node == null || !nearest.node.Walkable) continue;
                    var routeEnd = ObstacleAnalyzer.TraceAlongNavmesh(origin, point);
                    var footprint = NativeGroundMovementObservation.CaptureFootprint(mover, point, clearance);
                    var residuals = footprint["probes"].Select(p => (float)p["residual"]).ToArray();
                    var residual = residuals.Any(v => float.IsNaN(v) || float.IsInfinity(v)) ? float.NaN : residuals.Max();
                    var blockers = occupants.Where(u => (string)u["actorId"] != mover.UniqueId &&
                        Distance(point, Position(u["position"])) < mover.View.Corpulence + (float)u["corpulence"] + 0.05f)
                        .Select(u => (string)u["actorId"]).ToArray();
                    var travel = Distance(origin, point); var separation = Distance(center, point); var route = Distance(routeEnd, point);
                    c["travel"] = travel; c["separation"] = separation; c["routeEnd"] = Point(routeEnd);
                    c["routeResidual"] = route; c["footprint"] = footprint; c["blockers"] = new JArray(blockers);
                    var eligible = NativeGroundFixturePolicy.IsPreCombatPosition(Math.Round(radius, 2), separation, travel, route, residual, blockers.Length != 0);
                    c["eligible"] = eligible;
                    if (!eligible) continue;
                    if (joint) {
                        var riderSearch = Search(partner, mover, point, Math.Max(0.5f, partner.View.Corpulence) + 0.75f, false, point);
                        c["riderSearch"] = riderSearch;
                        if ((int)riderSearch["selectedIndex"] < 0) continue;
                    }
                    search["selectedIndex"] = candidates.Count - 1;
                    return search;
                }
            return search;
        }
    }
}
