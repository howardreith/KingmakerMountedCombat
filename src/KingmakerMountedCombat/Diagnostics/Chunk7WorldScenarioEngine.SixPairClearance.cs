using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View;
using Kingmaker.View.Roaming;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        private readonly Dictionary<string, double> sixClearanceAtBegin = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly Vector3[] sixGroundGoals = new Vector3[6];

        // ObstacleAnalyzer.TraceAlongNavmesh / AstarPath projection tolerance for an admitted goal and its straight route.
        private const float SixGoalProjectionMeters = 0.1f;

        private Vector3 SixGroundGoal(int index) => sixGroundGoals[index];

        // F2 r39 (b210): a native hostile group near the cold-saved eastern placement saw a pair actor at 8.3 m (inside its
        // 8.5 m native vision) and the whole group joined combat. The placement comes from the qualified cold save and the
        // stress performs no fixture setup write, so it is admitted or refused here, before any input: every pair actor and
        // every declared ground goal must keep the native vision clearance from every conscious non-player-faction unit in
        // the area. F3 r39: the same group's random roamer passed that check at 19.35 m and then roamed natively to 8.1 m
        // of an eastern actor during cycle 3, so each cycle-1 ground goal (where the pairs spend the rest of the stress) is
        // chosen to keep the clearance from every unit's native wander envelope as well (SelectSixGroundGoals). Read-only:
        // no unit is moved, removed, fenced, paused or put to sleep, and no roaming state is written.
        private void RequireSixPairHostileClearance()
        {
            SelectSixGroundGoals();
            var pairActors = Actors();
            var points = new List<KeyValuePair<string, Vector3>>();
            foreach (var actor in pairActors) points.Add(new KeyValuePair<string, Vector3>("actor:" + actor.UniqueId, actor.Position));
            for (var i = 0; i < 6; i++)
                points.Add(new KeyValuePair<string, Vector3>("waypoint:" + SixMount(i).UniqueId, SixGroundGoal(i)));
            var rows = new JArray(); var breaches = new JArray();
            foreach (var unit in SixClearanceUnits(pairActors))
            {
                var vision = unit.VisionRangeMeters; var nearest = double.MaxValue; string nearestPoint = null;
                foreach (var point in points)
                {
                    var distance = Chunk7SixPairCombatEntry.Distance2D(unit.Position.x, unit.Position.z, point.Value.x, point.Value.z);
                    if (distance < nearest) { nearest = distance; nearestPoint = point.Key; }
                }
                var area = unit.ExtendedVisionArea;
                var extended = (object)area != null && points.Any(point => area.ContainsPosition(point.Value));
                var clear = Chunk7SixPairCombatEntry.ClearanceHolds(nearest, vision, extended);
                sixClearanceAtBegin[unit.UniqueId] = nearest;
                var row = new JObject { ["id"] = unit.UniqueId, ["blueprint"] = unit.Blueprint?.AssetGuid, ["faction"] = unit.Faction?.AssetGuid,
                    ["peacefulFaction"] = unit.Faction != null && unit.Faction.Peaceful, ["groupId"] = unit.GroupId, ["awake"] = unit.IsAwake,
                    ["inFog"] = unit.IsInFogOfWar, ["position"] = Point(unit.Position), ["visionRangeMeters"] = vision,
                    ["requiredClearanceMeters"] = Chunk7SixPairCombatEntry.RequiredClearanceMeters(vision),
                    ["nearestPoint"] = nearestPoint, ["nearestDistance2D"] = nearest, ["extendedVisionCoversPoint"] = extended, ["clear"] = clear };
                rows.Add(row);
                if (!clear) breaches.Add(unit.UniqueId);
            }
            var record = new JObject { ["contract"] = "six-pair-native-vision-clearance-v1", ["phase"] = "before-stress",
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["combatVisionMeters"] = Chunk7SixPairCombatEntry.CombatVisionMeters, ["marginMeters"] = Chunk7SixPairCombatEntry.ClearanceMarginMeters,
                ["points"] = points.Count, ["units"] = rows, ["breaches"] = breaches, ["clear"] = breaches.Count == 0 };
            evidence["sixPairHostileClearance"] = record;
            Require(breaches.Count == 0, "Six-pair stress is uncovered on this placement: " + breaches.Count +
                " native unit(s) within vision clearance of the pairs or their waypoints; no input was issued.");
        }

        private static UnitEntityData[] SixClearanceUnits(UnitEntityData[] pairActors) =>
            Game.Instance.State.Units.Where(unit => unit != null && unit.IsInGame && !unit.IsPlayerFaction &&
                !pairActors.Contains(unit) && unit.Descriptor.State.IsConscious).ToArray();

        // The cycle-1 ground goal of each pair, in input order: the first candidate (Chunk7SixPairCombatEntry.GroundGoalDistances x
        // GroundGoalBearings, the declared +x 2 m route first) that projects onto the native navmesh with a straight native
        // route from the mount, keeps the declared spacing from the goals before it, the mounts after it and every other unit,
        // and keeps native vision clearance (plus margin and the dismount reach) from every hostile's current position and
        // native wander envelope (Chunk7SixPairGroundGoalPolicy). A pair with no such goal refuses the stress as uncovered
        // before any input.
        private void SelectSixGroundGoals()
        {
            Require(global::AstarPath.active != null, "Six-pair ground goals require the native navigation graph.");
            var pairActors = Actors(); var units = SixClearanceUnits(pairActors);
            var envelopes = new JArray(); var hostiles = new List<SixHostile>();
            foreach (var unit in units)
                hostiles.Add(new SixHostile(ReadSixWanderEnvelope(unit, envelopes), unit.Position.x, unit.Position.z, unit.VisionRangeMeters));
            var pairs = new JArray(); var refused = new List<int>();
            for (var i = 0; i < 6; i++)
            {
                var mount = SixMount(i); var rider = SixRider(i); var start = mount.Position; JObject chosen = null;
                var firstRefusal = new JObject(); var goal = start; var clearance = double.NaN;
                if (Chunk7SixPairGroundGoalPolicy.Search((distance, bearing) =>
                    {
                        var wanted = start + Quaternion.Euler(0f, bearing, 0f) * Vector3.right * distance;
                        return SixGoalRefusal(i, mount, start, wanted, units, hostiles, out goal, out clearance);
                    }, (reason, distance, bearing) => firstRefusal[reason] = new JArray(distance, bearing), out var chosenDistance, out var chosenBearing, out var tried))
                {
                    sixGroundGoals[i] = goal;
                    chosen = new JObject { ["distance"] = chosenDistance, ["bearing"] = chosenBearing, ["goal"] = Point(goal), ["start"] = Point(start),
                        ["candidatesTried"] = tried, ["minimumEnvelopeClearanceMeters"] = clearance };
                }
                if (chosen == null) refused.Add(i);
                pairs.Add(new JObject { ["index"] = i, ["mountId"] = mount.UniqueId, ["riderId"] = rider.UniqueId,
                    ["chosen"] = chosen, ["firstRefusals"] = firstRefusal });
            }
            // v2: waypoint envelopes record their legs and the native random fallback disk, so a reader can recompute every goal.
            evidence["sixPairGroundGoals"] = new JObject { ["contract"] = "six-pair-wander-clear-ground-goals-v2",
                ["roamingApproachMeters"] = Chunk7SixPairCombatEntry.RoamingApproachMeters, ["marginMeters"] = Chunk7SixPairCombatEntry.ClearanceMarginMeters,
                ["dismountReachMeters"] = Chunk7SixPairGroundGoalPolicy.DismountReachMeters, ["spacingMeters"] = Chunk7SixPairCombatEntry.GroundGoalSpacingMeters,
                ["envelopes"] = envelopes, ["pairs"] = pairs, ["clear"] = refused.Count == 0 };
            Require(refused.Count == 0, "Six-pair stress is uncovered on this placement: no native ground goal within 10 m keeps the hostile " +
                "wander-envelope vision clearance for pair(s) " + string.Join(",", refused) + "; no input was issued.");
        }

        private string SixGoalRefusal(int index, UnitEntityData mount, Vector3 start, Vector3 wanted,
            UnitEntityData[] units, List<SixHostile> hostiles, out Vector3 goal, out double clearance)
        {
            goal = wanted; clearance = double.NaN;
            var nearest = global::AstarPath.active.GetNearest(wanted);
            if (nearest.node == null || !nearest.node.Walkable) return "not-walkable";
            goal = nearest.clampedPosition;
            if (Chunk7SixPairCombatEntry.Distance2D(goal.x, goal.z, wanted.x, wanted.z) > SixGoalProjectionMeters) return "navmesh-projection";
            var traced = ObstacleAnalyzer.TraceAlongNavmesh(start, goal);
            if (Chunk7SixPairCombatEntry.Distance2D(traced.x, traced.z, goal.x, goal.z) > SixGoalProjectionMeters) return "route-not-straight-on-navmesh";
            var others = new List<double[]>();
            for (var j = 0; j < 6; j++)
            {
                if (j == index) continue;
                var other = j < index ? sixGroundGoals[j] : SixMount(j).Position;
                others.Add(new double[] { other.x, other.z });
            }
            if (!Chunk7SixPairGroundGoalPolicy.SpacingHolds(goal.x, goal.z, others)) return Chunk7SixPairGroundGoalPolicy.PairSpacing;
            var pairActors = Actors(); var corpulence = mount.View != null ? mount.View.Corpulence : 0f;
            foreach (var unit in Game.Instance.State.Units)
            {
                if (unit == null || !unit.IsInGame || unit.View == null || pairActors.Contains(unit)) continue;
                if (Chunk7SixPairCombatEntry.Distance2D(goal.x, goal.z, unit.Position.x, unit.Position.z) < corpulence + unit.View.Corpulence + 0.25f) return "unit-footprint";
            }
            var at = goal;
            return Chunk7SixPairGroundGoalPolicy.HostileRefusal(goal.x, goal.z, hostiles, h =>
            {
                var area = units[h].ExtendedVisionArea;
                return (object)area != null && area.ContainsPosition(at);
            }, out clearance);
        }

        // Kingmaker 2.1.7b UnitRoamingController / UnitPartRoaming, read-only. Units without an enabled roaming part stand where
        // they are; a waypoint roamer's envelope is its reachable NextWaypoints/PrevWaypoints graph, plus its OriginalPoint disk
        // of Settings.Radius when a reached waypoint can yield no next point in either direction (AdvancePoint leaves NextPoint
        // null and RollNextPoint rolls inside the disk); a random roamer's is that disk. The leg toward the current NextPoint is
        // included for both. An unreadable waypoint graph yields distance 0 (the goal refuses), never an assumed clearance.
        private SixWanderEnvelope ReadSixWanderEnvelope(UnitEntityData unit, JArray records)
        {
            var roaming = unit.Get<UnitPartRoaming>(); var here = unit.Position;
            var row = new JObject { ["id"] = unit.UniqueId, ["groupId"] = unit.GroupId, ["position"] = Point(here) }; records.Add(row);
            if (roaming == null || roaming.Disabled) { row["kind"] = roaming == null ? "stationary" : "roaming-disabled"; return SixWanderEnvelope.Standing(roaming != null, here.x, here.z); }
            var next = roaming.NextPoint; var hasNext = next != null; var nextPosition = hasNext ? next.Position : here;
            row["nextPoint"] = hasNext ? Point(nextPosition) : null;
            var origin = roaming.OriginalPoint; var radius = roaming.Settings != null ? roaming.Settings.Radius : 0f;
            if (next is RoamingWaypointData first)
            {
                var legs = new List<double[]>(); var legRows = new JArray(); var nodes = new JArray(); var unreadable = false; var fallback = false;
                var seen = new HashSet<RoamingWaypointView>(); var queue = new Queue<RoamingWaypointView>();
                if (first.WaypointView == null) unreadable = true; else { seen.Add(first.WaypointView); queue.Enqueue(first.WaypointView); }
                while (queue.Count > 0 && !unreadable)
                {
                    var view = queue.Dequeue(); nodes.Add(Point(view.transform.position));
                    if (seen.Count > 256) { unreadable = true; break; }
                    fallback |= Chunk7SixPairGroundGoalPolicy.WaypointMayFallBackToRandom(
                        view.NextWaypoints.Count, view.NextWaypoints.Any(entry => entry == null || entry.Waypoint == null || entry.Waypoint.WaypointData == null),
                        view.PrevWaypoints.Count, view.PrevWaypoints.Any(prev => prev == null || prev.WaypointData == null));
                    var neighbours = view.NextWaypoints.Select(entry => entry?.Waypoint).Concat(view.PrevWaypoints);
                    foreach (var neighbour in neighbours)
                    {
                        if (neighbour == null) continue;
                        var a = view.transform.position; var b = neighbour.transform.position;
                        legs.Add(new double[] { a.x, a.z, b.x, b.z }); legRows.Add(new JObject { ["from"] = Point(a), ["to"] = Point(b) });
                        if (seen.Add(neighbour)) queue.Enqueue(neighbour);
                    }
                }
                row["kind"] = unreadable ? "waypoints-unreadable" : "waypoints"; row["waypoints"] = nodes;
                if (unreadable) return SixWanderEnvelope.Unreadable(here.x, here.z);
                row["legs"] = legRows; row["randomFallback"] = fallback; row["originalPoint"] = Point(origin); row["radius"] = radius;
                var points = seen.Select(view => view.transform.position).Select(w => new double[] { w.x, w.z }).ToList();
                return SixWanderEnvelope.WaypointGraph(here.x, here.z, nextPosition.x, nextPosition.z, points, legs,
                    fallback, origin.x, origin.z, radius);
            }
            row["kind"] = "random"; row["originalPoint"] = Point(origin); row["radius"] = radius;
            return SixWanderEnvelope.RandomDisk(here.x, here.z, nextPosition.x, nextPosition.z, origin.x, origin.z, radius);
        }
    }
}
