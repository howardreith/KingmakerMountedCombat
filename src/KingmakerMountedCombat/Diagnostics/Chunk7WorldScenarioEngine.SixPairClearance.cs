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

        // A native Dismount stands the rider beside its mount: the F3 r39 record shows 2.08-3.55 m. A ground goal keeps the
        // vision clearance for every point the dismounted rider can take there.
        private const float SixDismountReachMeters = 4f;
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
        // native wander envelope. A pair with no such goal refuses the stress as uncovered before any input.
        private void SelectSixGroundGoals()
        {
            Require(global::AstarPath.active != null, "Six-pair ground goals require the native navigation graph.");
            var pairActors = Actors(); var hostiles = SixClearanceUnits(pairActors);
            var envelopes = new JArray(); var envelopeByUnit = new Dictionary<UnitEntityData, Func<Vector3, double>>();
            foreach (var unit in hostiles) envelopeByUnit[unit] = SixWanderEnvelope(unit, envelopes);
            var pairs = new JArray(); var refused = new List<int>();
            for (var i = 0; i < 6; i++)
            {
                var mount = SixMount(i); var rider = SixRider(i); var start = mount.Position; var tried = 0; JObject chosen = null;
                var firstRefusal = new JObject();
                foreach (var distance in Chunk7SixPairCombatEntry.GroundGoalDistances())
                {
                    foreach (var bearing in Chunk7SixPairCombatEntry.GroundGoalBearings())
                    {
                        tried++;
                        var wanted = start + Quaternion.Euler(0f, bearing, 0f) * Vector3.right * distance;
                        var reason = SixGoalRefusal(i, mount, rider, start, wanted, hostiles, envelopeByUnit, out var goal, out var clearance);
                        if (reason != null) { if (firstRefusal[reason] == null) firstRefusal[reason] = new JArray(distance, bearing); continue; }
                        sixGroundGoals[i] = goal;
                        chosen = new JObject { ["distance"] = distance, ["bearing"] = bearing, ["goal"] = Point(goal), ["start"] = Point(start),
                            ["candidatesTried"] = tried, ["minimumEnvelopeClearanceMeters"] = clearance };
                        break;
                    }
                    if (chosen != null) break;
                }
                if (chosen == null) refused.Add(i);
                pairs.Add(new JObject { ["index"] = i, ["mountId"] = mount.UniqueId, ["riderId"] = rider.UniqueId,
                    ["chosen"] = chosen, ["firstRefusals"] = firstRefusal });
            }
            evidence["sixPairGroundGoals"] = new JObject { ["contract"] = "six-pair-wander-clear-ground-goals-v1",
                ["roamingApproachMeters"] = Chunk7SixPairCombatEntry.RoamingApproachMeters, ["marginMeters"] = Chunk7SixPairCombatEntry.ClearanceMarginMeters,
                ["dismountReachMeters"] = SixDismountReachMeters, ["spacingMeters"] = Chunk7SixPairCombatEntry.GroundGoalSpacingMeters,
                ["envelopes"] = envelopes, ["pairs"] = pairs, ["clear"] = refused.Count == 0 };
            Require(refused.Count == 0, "Six-pair stress is uncovered on this placement: no native ground goal within 10 m keeps the hostile " +
                "wander-envelope vision clearance for pair(s) " + string.Join(",", refused) + "; no input was issued.");
        }

        private string SixGoalRefusal(int index, UnitEntityData mount, UnitEntityData rider, Vector3 start, Vector3 wanted,
            UnitEntityData[] hostiles, Dictionary<UnitEntityData, Func<Vector3, double>> envelopes, out Vector3 goal, out double clearance)
        {
            goal = wanted; clearance = double.NaN;
            var nearest = global::AstarPath.active.GetNearest(wanted);
            if (nearest.node == null || !nearest.node.Walkable) return "not-walkable";
            goal = nearest.clampedPosition;
            if (Chunk7SixPairCombatEntry.Distance2D(goal.x, goal.z, wanted.x, wanted.z) > SixGoalProjectionMeters) return "navmesh-projection";
            var traced = ObstacleAnalyzer.TraceAlongNavmesh(start, goal);
            if (Chunk7SixPairCombatEntry.Distance2D(traced.x, traced.z, goal.x, goal.z) > SixGoalProjectionMeters) return "route-not-straight-on-navmesh";
            for (var j = 0; j < 6; j++)
            {
                if (j == index) continue;
                var other = j < index ? sixGroundGoals[j] : SixMount(j).Position;
                if (Chunk7SixPairCombatEntry.Distance2D(goal.x, goal.z, other.x, other.z) < Chunk7SixPairCombatEntry.GroundGoalSpacingMeters) return "pair-spacing";
            }
            var pairActors = Actors(); var corpulence = mount.View != null ? mount.View.Corpulence : 0f;
            foreach (var unit in Game.Instance.State.Units)
            {
                if (unit == null || !unit.IsInGame || unit.View == null || pairActors.Contains(unit)) continue;
                if (Chunk7SixPairCombatEntry.Distance2D(goal.x, goal.z, unit.Position.x, unit.Position.z) < corpulence + unit.View.Corpulence + 0.25f) return "unit-footprint";
            }
            clearance = double.MaxValue;
            foreach (var unit in hostiles)
            {
                var envelope = envelopes[unit](goal);
                var current = Chunk7SixPairCombatEntry.Distance2D(goal.x, goal.z, unit.Position.x, unit.Position.z);
                var reach = Math.Min(envelope, current) - SixDismountReachMeters;
                clearance = Math.Min(clearance, reach);
                if (!Chunk7SixPairCombatEntry.ClearanceHolds(reach, unit.VisionRangeMeters, false)) return "hostile-wander-vision";
                var area = unit.ExtendedVisionArea;
                if ((object)area != null && area.ContainsPosition(goal)) return "hostile-extended-vision";
            }
            return null;
        }

        // Kingmaker 2.1.7b UnitRoamingController / UnitPartRoaming, read-only. Units without an enabled roaming part stand where
        // they are; a waypoint roamer's envelope is its reachable NextWaypoints/PrevWaypoints graph; a random roamer's is its
        // OriginalPoint disk of Settings.Radius. The leg toward the current NextPoint is included for both. An unreadable
        // waypoint graph yields distance 0 (the goal refuses), never an assumed clearance.
        private Func<Vector3, double> SixWanderEnvelope(UnitEntityData unit, JArray records)
        {
            var roaming = unit.Get<UnitPartRoaming>(); var here = unit.Position;
            var row = new JObject { ["id"] = unit.UniqueId, ["groupId"] = unit.GroupId, ["position"] = Point(here) }; records.Add(row);
            if (roaming == null || roaming.Disabled) { row["kind"] = roaming == null ? "stationary" : "roaming-disabled"; return p => Chunk7SixPairCombatEntry.Distance2D(p.x, p.z, here.x, here.z); }
            var next = roaming.NextPoint; var hasNext = next != null; var nextPosition = hasNext ? next.Position : here;
            row["nextPoint"] = hasNext ? Point(nextPosition) : null;
            Func<Vector3, double> leg = p => Chunk7SixPairCombatEntry.RoamingLegDistance2D(p.x, p.z, here.x, here.z, nextPosition.x, nextPosition.z);
            if (next is RoamingWaypointData first)
            {
                var legs = new List<KeyValuePair<Vector3, Vector3>>(); var nodes = new JArray(); var unreadable = false;
                var seen = new HashSet<RoamingWaypointView>(); var queue = new Queue<RoamingWaypointView>();
                if (first.WaypointView == null) unreadable = true; else { seen.Add(first.WaypointView); queue.Enqueue(first.WaypointView); }
                while (queue.Count > 0 && !unreadable)
                {
                    var view = queue.Dequeue(); nodes.Add(Point(view.transform.position));
                    if (seen.Count > 256) { unreadable = true; break; }
                    var neighbours = view.NextWaypoints.Select(entry => entry?.Waypoint).Concat(view.PrevWaypoints);
                    foreach (var neighbour in neighbours)
                    {
                        if (neighbour == null) continue;
                        legs.Add(new KeyValuePair<Vector3, Vector3>(view.transform.position, neighbour.transform.position));
                        if (seen.Add(neighbour)) queue.Enqueue(neighbour);
                    }
                }
                row["kind"] = unreadable ? "waypoints-unreadable" : "waypoints"; row["waypoints"] = nodes;
                if (unreadable) return p => 0d;
                var points = seen.Select(view => view.transform.position).ToArray();
                return p => Math.Min(leg(p), Math.Min(
                    points.Length == 0 ? double.MaxValue : points.Min(w => Math.Max(0, Chunk7SixPairCombatEntry.Distance2D(p.x, p.z, w.x, w.z) - Chunk7SixPairCombatEntry.RoamingApproachMeters)),
                    legs.Count == 0 ? double.MaxValue : legs.Min(l => Chunk7SixPairCombatEntry.RoamingLegDistance2D(p.x, p.z, l.Key.x, l.Key.z, l.Value.x, l.Value.z))));
            }
            var origin = roaming.OriginalPoint; var radius = roaming.Settings != null ? roaming.Settings.Radius : 0f;
            row["kind"] = "random"; row["originalPoint"] = Point(origin); row["radius"] = radius;
            return p =>
            {
                var disk = Chunk7SixPairCombatEntry.RoamingDiskDistance2D(p.x, p.z, origin.x, origin.z, radius);
                return double.IsNaN(disk) ? 0d : Math.Min(disk, leg(p));
            };
        }
    }
}
