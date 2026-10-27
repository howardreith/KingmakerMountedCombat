using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        // F5 hardening (F4 r30: the horse's fixed 2 m goal lay 0.83 m from rider A and 0.91 m from the Mammoth; it arrived,
        // but a native stall there would fail the row). Each of the two-pair scenario's three ground moves (mammoth, horse,
        // unmounted-control) goes to the first GroundGoalFootprintPolicy candidate on its declared 2 m route length: the
        // declared direction first (the old fixed goal, clicked exactly), then the bearing offsets around it. A candidate is
        // admitted when its nearest native node is walkable and within 0.1 m, the navmesh trace from the mover reaches it,
        // and it keeps corpulence sum + 0.5 m from every other unit (the moving pair excluded; the remaining scenario actors
        // always considered). The search is recorded in observations.routeGoalSearches before any assertion; no admissible
        // goal refuses as uncovered before the input. The measured interaction and its reader bounds are unchanged.
        private JArray routeGoalSearches;

        private void MoveOnClearRoute(UnitEntityData selected, UnitEntityData executor, Vector3 direction, UnitEntityData switchTo, string label)
        {
            var goal = SearchRouteGoal(selected, executor, direction, label, out var found);
            Require(found, "No walkable native 2 m route goal for the " + label + " move lies clear of every other unit's native " +
                "avoidance footprint; the two-pair ground move is uncovered on this fixture. No input was issued.");
            Move(selected, executor, goal, switchTo, label);
        }

        // Read-only: every evaluated candidate is recorded with the native values that decided it; the first admitted one is
        // the goal. Nothing in the world is changed.
        private Vector3 SearchRouteGoal(UnitEntityData selected, UnitEntityData executor, Vector3 direction, string label, out bool found)
        {
            found = false;
            if (routeGoalSearches == null) { routeGoalSearches = new JArray(); evidence["routeGoalSearches"] = routeGoalSearches; }
            var start = executor.Position; direction.y = 0f; direction = direction.normalized;
            var record = new JObject { ["contract"] = GroundGoalFootprintPolicy.RouteContract, ["label"] = label,
                ["selectedId"] = selected.UniqueId, ["moverId"] = executor.UniqueId, ["start"] = Point(start),
                ["direction"] = Point(direction), ["distance"] = GroundGoalFootprintPolicy.RouteDistance,
                ["margin"] = DoorReturnGoalPolicy.ClearanceMargin, ["unitRadius"] = DoorReturnGoalPolicy.UnitConsiderationRadius };
            routeGoalSearches.Add(record);
            Require(executor.View?.AgentASP != null, "The " + label + " route mover has no native movement agent.");
            var moverCorpulence = executor.View.AgentASP.Corpulence; record["moverCorpulence"] = moverCorpulence;
            var remaining = Actors().Where(x => !ReferenceEquals(x, selected) && !ReferenceEquals(x, executor)).ToArray();
            var units = Game.Instance.State.Units.Where(u => u != null && !ReferenceEquals(u, selected) && !ReferenceEquals(u, executor) &&
                    u.IsInState && u.View != null && u.View.AgentASP != null &&
                    (remaining.Contains(u) || Vector3.Distance(Flat(u.Position), Flat(start)) <= DoorReturnGoalPolicy.UnitConsiderationRadius))
                .OrderBy(u => u.UniqueId, StringComparer.Ordinal).ToArray();
            var corpulences = units.Select(u => u.View.AgentASP.Corpulence).ToArray();
            record["units"] = new JArray(units.Select((u, n) => new JObject { ["id"] = u.UniqueId, ["position"] = Point(u.Position), ["corpulence"] = corpulences[n] }));
            var candidates = new JArray(); record["candidates"] = candidates; record["selectedIndex"] = -1;
            Require(units.Length <= DoorReturnGoalPolicy.MaximumUnits, "The " + label + " route goal unit scan exceeded its read-only bound.");
            Require(remaining.All(units.Contains), "The " + label + " route goal search lost a scenario actor outside the moving pair.");
            var offsets = GroundGoalFootprintPolicy.RouteBearingOffsets(); var goal = start;
            for (var i = 0; i < offsets.Length && !found; i++)
            {
                // Offset 0 is the old fixed goal, computed exactly as before (no rotation applied).
                var heading = offsets[i] == 0f ? direction : Quaternion.Euler(0f, offsets[i], 0f) * direction;
                var requested = start + heading * GroundGoalFootprintPolicy.RouteDistance;
                var nearest = Kingmaker.View.ObstacleAnalyzer.GetNearestNode(requested);
                var walkable = nearest.node != null && nearest.node.Walkable;
                var candidate = new JObject { ["index"] = i, ["bearingOffset"] = offsets[i], ["requested"] = Point(requested), ["walkable"] = walkable };
                var reason = "no-walkable-native-node";
                if (walkable)
                {
                    var clamped = nearest.clampedPosition;
                    var clampResidual = Vector3.Distance(Flat(requested), Flat(clamped));
                    var traceResidual = Vector3.Distance(Flat(Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(start, clamped)), Flat(clamped));
                    var slack = double.PositiveInfinity; string blocker = null;
                    for (var u = 0; u < units.Length; u++)
                    {
                        var value = GroundGoalFootprintPolicy.RouteSlack(Vector3.Distance(Flat(clamped), Flat(units[u].Position)), moverCorpulence, corpulences[u]);
                        if (value < slack) { slack = value; blocker = units[u].UniqueId; }
                    }
                    reason = GroundGoalFootprintPolicy.RouteReason(true, clampResidual, traceResidual, slack);
                    candidate["clamped"] = Point(clamped); candidate["clampResidual"] = clampResidual; candidate["traceResidual"] = traceResidual;
                    candidate["minimumSlack"] = slack; candidate["nearestBlockerId"] = blocker;
                    if (reason == "admitted") { goal = requested; found = true; }
                }
                candidate["reason"] = reason; candidates.Add(candidate);
            }
            record["selectedIndex"] = found ? candidates.Count - 1 : -1;
            return goal;
        }

        private static Vector3 Flat(Vector3 value) => new Vector3(value.x, 0f, value.z);
    }
}
