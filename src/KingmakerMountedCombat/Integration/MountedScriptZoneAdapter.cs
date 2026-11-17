using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        private sealed class Checked { internal Vector3 Position; }

        // The position each seated rider's membership was last checked at, by native or here.
        private static readonly ConditionalWeakTable<UnitEntityData, Checked> checkedAt = new ConditionalWeakTable<UnitEntityData, Checked>();

        internal static long RecheckCount { get; private set; }
        internal static long MembershipChangeCount { get; private set; }

        // One rider of one relationship; the active zones are found once per tick, on first need.
        internal static void Recheck(GameMountedRelationshipService relationship, ref List<ScriptZone> zones, IModLogger logger)
        {
            var rider = relationship?.Rider;
            if (rider == null) return;
            Checked last;
            var known = checkedAt.TryGetValue(rider, out last);
            if (relationship.State != RelationshipState.Mounted)
            {
                if (known) checkedAt.Remove(rider);
                return;
            }
            var position = rider.Position;
            var nativeMotion = rider.HasMotionThisTick;
            var delta = known ? position - last.Position : Vector3.zero;
            if (!MountedScriptZonePolicy.RiderNeedsZoneRecheck(true, nativeMotion, known, delta.x, delta.y, delta.z))
            {
                if (nativeMotion) Record(rider, last, position);
                return;
            }
            zones = zones ?? ActiveZones();
            foreach (var zone in zones)
            {
                var before = zone.ContainsUnit(rider);
                zone.CheckUnit(rider);
                if (zone.ContainsUnit(rider) == before) continue;
                MembershipChangeCount++;
                logger?.Info("Mounted script-zone membership followed the carried rider: rider=" + rider.UniqueId +
                    "; zone=" + zone.name + "; inside=" + !before + "; position=" + position + ".");
            }
            RecheckCount++;
            Record(rider, last, position);
        }

        private static void Record(UnitEntityData rider, Checked last, Vector3 position)
        {
            if (last == null) checkedAt.Add(rider, new Checked { Position = position });
            else last.Position = position;
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
