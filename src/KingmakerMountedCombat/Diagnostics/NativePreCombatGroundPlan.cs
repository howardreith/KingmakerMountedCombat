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
        private static JObject NavigationPoint(Vector3 point, Pathfinding.NNInfo nearest) => new JObject {
            ["requested"] = Point(point), ["clamped"] = Point(nearest.clampedPosition),
            ["nodeObject"] = nearest.node == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(nearest.node),
            ["nodeType"] = nearest.node?.GetType().FullName, ["walkable"] = nearest.node != null && nearest.node.Walkable
        };
        // Read-only origin-side measurements for a route trace that ends at its own origin: the
        // navmesh's own view of the origin, traces from nudged and deepened origins, and the
        // mover's native agent state. Recorded so that a repair, if any, follows measurements.
        internal static JObject MeasureOrigin(UnitEntityData mover, Vector3 origin, Vector3 point)
        {
            var nudged = new JArray();
            foreach (var step in new[] { 0.15f, 0.3f })
                for (var index = 0; index < 8; index++)
                {
                    var angle = index * 45f;
                    var from = origin + Quaternion.Euler(0, angle, 0) * Vector3.forward * step;
                    var end = ObstacleAnalyzer.TraceAlongNavmesh(from, point);
                    nudged.Add(new JObject { ["angle"] = angle, ["step"] = step, ["from"] = Point(from), ["fromInsideNavmesh"] = ObstacleAnalyzer.IsPointInsideNavMesh(from),
                        ["traceEnd"] = Point(end), ["residual"] = Distance(end, point), ["returnedToStart"] = Distance(end, from) <= 0.01f });
                }
            var deep = ObstacleAnalyzer.GetDeepNavmeshPoint(origin, 0.3f);
            var agent = mover.View?.AgentASP;
            return new JObject {
                ["originSelfTrace"] = Point(ObstacleAnalyzer.TraceAlongNavmesh(origin, origin)),
                ["originInsideNavmesh"] = ObstacleAnalyzer.IsPointInsideNavMesh(origin), ["originArea"] = ObstacleAnalyzer.GetArea(origin),
                ["targetInsideNavmesh"] = ObstacleAnalyzer.IsPointInsideNavMesh(point), ["targetArea"] = ObstacleAnalyzer.GetArea(point),
                ["originDeepPoint"] = Point(deep), ["originDeepOffset"] = Distance(deep, origin),
                ["fromDeepOrigin"] = Point(ObstacleAnalyzer.TraceAlongNavmesh(deep, point)),
                ["nudgedOriginTraces"] = nudged,
                ["agent"] = agent == null ? null : new JObject { ["reallyMoving"] = agent.IsReallyMoving, ["wantsToMove"] = agent.WantsToMove,
                    ["pathFailed"] = agent.PathFailed, ["repathNeeded"] = agent.RepathNeeded, ["hasPath"] = agent.Path != null,
                    ["connectedToObstacles"] = agent.ConnectedToObstacles, ["obstaclesGroup"] = agent.ObstaclesGroup == null ? -1 : agent.ObstaclesGroup.Count,
                    ["corpulence"] = mover.View.Corpulence }
            };
        }
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
            var nativeOrigin = ObstacleAnalyzer.GetNearestNode(origin);
            var astarOrigin = AstarPath.active.GetNearest(origin);
            var originInside = ObstacleAnalyzer.IsPointInsideNavMesh(origin);
            var search = new JObject { ["contract"] = "bounded-native-ground-plan-v2", ["actorId"] = mover.UniqueId,
                ["partnerId"] = partner.UniqueId, ["origin"] = Point(origin), ["center"] = Point(center),
                ["corpulence"] = mover.View.Corpulence, ["clearanceRadius"] = clearance, ["joint"] = joint,
                ["partnerPositionOverride"] = partnerPosition.HasValue ? Point(partnerPosition.Value) : null,
                ["occupants"] = occupants, ["maximumCandidates"] = SearchLimit, ["candidates"] = candidates,
                ["selectedIndex"] = -1,
                ["originInsideNavmesh"] = originInside, ["originNativeNearest"] = NavigationPoint(origin, nativeOrigin),
                ["originAstarNearest"] = NavigationPoint(origin, astarOrigin) };
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
                    var reverseEnd = ObstacleAnalyzer.TraceAlongNavmesh(point, origin);
                    var footprint = NativeGroundMovementObservation.CaptureFootprint(mover, point, clearance);
                    var residuals = footprint["probes"].Select(p => (float)p["residual"]).ToArray();
                    var residual = residuals.Any(v => float.IsNaN(v) || float.IsInfinity(v)) ? float.NaN : residuals.Max();
                    var blockers = occupants.Where(u => (string)u["actorId"] != mover.UniqueId &&
                        Distance(point, Position(u["position"])) < mover.View.Corpulence + (float)u["corpulence"] + 0.05f)
                        .Select(u => (string)u["actorId"]).ToArray();
                    var travel = Distance(origin, point); var separation = Distance(center, point); var route = Distance(routeEnd, point);
                    c["travel"] = travel; c["separation"] = separation; c["routeEnd"] = Point(routeEnd);
                    if (route > 0.01f && search["firstBlockedRouteQuery"] == null) {
                        var nativePoint = ObstacleAnalyzer.GetNearestNode(point);
                        search["firstBlockedRouteQuery"] = new JObject {
                            ["candidateIndex"] = candidates.Count - 1,
                            ["nativeTarget"] = NavigationPoint(point, nativePoint),
                            ["astarTarget"] = NavigationPoint(requested, nearest),
                            ["fromNativeClampedOrigin"] = Point(ObstacleAnalyzer.TraceAlongNavmesh(nativeOrigin.clampedPosition, point)),
                            ["fromAstarClampedOrigin"] = Point(ObstacleAnalyzer.TraceAlongNavmesh(astarOrigin.clampedPosition, point)),
                            ["toNativeClampedTarget"] = Point(ObstacleAnalyzer.TraceAlongNavmesh(origin, nativePoint.clampedPosition)),
                            ["originMeasurements"] = MeasureOrigin(mover, origin, point)
                        };
                    }
                    c["routeResidual"] = route; c["reverseRouteEnd"] = Point(reverseEnd);
                    var measuredRoute = NativeGroundFixturePolicy.PreCombatRouteResidual(originInside,
                        nativeOrigin.node != null && nativeOrigin.node.Walkable, Distance(origin, nativeOrigin.clampedPosition),
                        route, Distance(routeEnd, origin), Distance(reverseEnd, origin));
                    c["routeProof"] = route < 0.001f ? "forward" : measuredRoute < 0.001 ? "reciprocal-origin-boundary" : "rejected";
                    c["footprint"] = footprint; c["blockers"] = new JArray(blockers);
                    var eligible = NativeGroundFixturePolicy.IsPreCombatPosition(Math.Round(radius, 2), separation, travel, measuredRoute, residual, blockers.Length != 0);
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
