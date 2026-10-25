using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        private readonly Dictionary<string, double> sixClearanceAtBegin = new Dictionary<string, double>(StringComparer.Ordinal);

        // F2 r39 (b210): a native hostile group near the cold-saved eastern placement saw a pair actor at 8.3 m (inside its
        // 8.5 m native vision) and the whole group joined combat. The placement comes from the qualified cold save and the
        // stress performs no fixture setup write, so it is admitted or refused here, before any input: every pair actor and
        // every declared ground waypoint (mount + 2 m x) must keep the native vision clearance from every conscious
        // non-player-faction unit in the area. Read-only: no unit is moved, removed, fenced or put to sleep.
        private void RequireSixPairHostileClearance()
        {
            var pairActors = Actors();
            var points = new List<KeyValuePair<string, Vector3>>();
            foreach (var actor in pairActors) points.Add(new KeyValuePair<string, Vector3>("actor:" + actor.UniqueId, actor.Position));
            for (var i = 0; i < 6; i++)
                points.Add(new KeyValuePair<string, Vector3>("waypoint:" + SixMount(i).UniqueId, SixMount(i).Position + new Vector3(2, 0, 0)));
            var rows = new JArray(); var breaches = new JArray();
            foreach (var unit in Game.Instance.State.Units.Where(unit => unit != null && unit.IsInGame && !unit.IsPlayerFaction &&
                !pairActors.Contains(unit) && unit.Descriptor.State.IsConscious))
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
    }
}
