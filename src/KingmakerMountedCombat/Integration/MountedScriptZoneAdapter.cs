using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View.MapObjects.SriptZones;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Known issues C04: after native ScriptZoneController.Tick, a mounted pair's seated rider whose carried position
    // moved without native motion is checked against the same zones native checks (active, in game) with the native
    // ScriptZone.CheckUnit, so its membership, and every entered/exited event native raises, follows where the rider
    // actually is. Nothing else is written; the mount and every other unit stay native.
    internal static class MountedScriptZoneAdapter
    {
        // Where each seated rider's membership was last checked, by native or here, per mounting instance (review R1 of
        // b30c24f8: a cache keyed by the rider alone outlived the dismount, whose cleanup clears the references this
        // adapter reads, and suppressed the first check after a remount at the same place).
        private static readonly MountedScriptZoneObservations<UnitEntityData> observations = new MountedScriptZoneObservations<UnitEntityData>();

        internal static long RecheckCount { get; private set; }
        internal static long MembershipChangeCount { get; private set; }

        // One rider of one relationship; the active zones are found once per tick, on first need.
        internal static void Recheck(GameMountedRelationshipService relationship, ref List<ScriptZone> zones, IModLogger logger)
        {
            var rider = relationship?.Rider;
            var instance = relationship?.MountingInstance;
            if (rider == null || instance == null || relationship.State != RelationshipState.Mounted) return;
            var position = rider.Position;
            if (!observations.NeedsRecheck(rider, instance, rider.HasMotionThisTick, position.x, position.y, position.z)) return;
            zones = zones ?? ActiveZones();
            // Review R9 of e1c0c7ad: a throwing logger never stops the remaining zones.
            MembershipChangeCount += MountedScriptZoneChecks.CheckEach(zones, zone => zone.ContainsUnit(rider), zone => zone.CheckUnit(rider),
                (zone, inside) => logger?.Info("Mounted script-zone membership followed the carried rider: rider=" + rider.UniqueId +
                    "; zone=" + zone.name + "; inside=" + inside + "; position=" + position + "."));
            RecheckCount++;
            observations.Record(rider, instance, position.x, position.y, position.z);
        }

        // Native ScriptZoneController.Tick's own selection: a ScriptZone view that is active, on a map object in game.
        private static List<ScriptZone> ActiveZones()
        {
            var zones = new List<ScriptZone>();
            var state = Game.Instance?.State;
            if (state == null) return zones;
            foreach (var mapObject in state.MapObjects)
            {
                var zone = mapObject.View as ScriptZone;
                if (zone != null && zone.IsActive && mapObject.IsInGame) zones.Add(zone);
            }
            return zones;
        }
    }
}
