using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KingmakerMountedCombat.Domain
{
    // Known issues C04 (Candlemere, "wade through"). Native ScriptZoneController.Tick (060092A0) rechecks a unit's
    // script-zone membership only when the unit HasMotionThisTick (0600830F: Position != PreviousPosition), and
    // UnitMoveController.Tick (06009183) stamps PreviousPosition at the start of its tick. A seated rider's position is
    // written by the pair's synchronization outside that window (in the UnitMoveController prefix, before the stamp, and
    // in LateUpdate), so a rider carried into or out of a zone is never rechecked: its membership keeps whatever it was
    // when it mounted. Any authored condition that asks whether that unit is in a zone then answers from a stale place.
    // At Candlemere the crossing asks whether the interacting unit is in the near-side zone to choose its direction, so a
    // rider carried there is sent to the near-side enter point: a fade, and no crossing.
    public static class MountedScriptZonePolicy
    {
        // Unity's Vector3 equality: positions closer than this squared distance compare equal, as native
        // Position != PreviousPosition does.
        public const float SamePositionSquaredDistance = 9.99999944E-11f;

        public static bool PositionChanged(float dx, float dy, float dz) =>
            dx * dx + dy * dy + dz * dz >= SamePositionSquaredDistance;

        // Whether a pair's seated rider is checked against the active script zones after native ScriptZoneController.Tick,
        // with the native ScriptZone.CheckUnit and its own rules: the pair is mounted; native did not check the rider
        // itself this tick (no native motion); and its carried position moved since its membership was last checked, or
        // was never checked while mounted.
        public static bool RiderNeedsZoneRecheck(bool pairMounted, bool riderHasNativeMotionThisTick, bool checkedBefore,
            float dx, float dy, float dz) =>
            pairMounted && !riderHasNativeMotionThisTick && (!checkedBefore || PositionChanged(dx, dy, dz));
    }

    // Review R9 of e1c0c7ad: a seated rider is checked against every active zone; a membership change is reported, and a
    // report that fails (a throwing logger) never stops the remaining zones. A failing check itself is an operational
    // failure and still ends the pass (the hook's bounded fault report takes it).
    public static class MountedScriptZoneChecks
    {
        public static int CheckEach<TZone>(IList<TZone> zones, Func<TZone, bool> contains, Action<TZone> check, Action<TZone, bool> changed)
        {
            if (zones == null) return 0;
            var changes = 0;
            foreach (var zone in zones)
            {
                var before = contains(zone);
                check(zone);
                var after = contains(zone);
                if (after == before) continue;
                changes++;
                if (changed == null) continue;
                try { changed(zone, after); }
                catch (Exception) { }
            }
            return changes;
        }
    }

    // Known issues C04, review R1 of b30c24f8: where each seated rider's zone membership was last checked (by native, or
    // by the pair's recheck), scoped to the mounting instance it was checked in: the coordinator's MountedPair, which
    // every Attach creates anew. An observation of an earlier mounting (on the same relationship or another), or of a unit
    // of another world (a different object), is never the instance asked about, so it is never reused; nothing has to
    // remove it when cleanup clears the relationship's references. Within one mounting instance a stationary rider is
    // not checked again.
    public sealed class MountedScriptZoneObservations<TUnit> where TUnit : class
    {
        private sealed class Observation
        {
            internal MountedPair Instance;
            internal float X, Y, Z;
        }

        private readonly ConditionalWeakTable<TUnit, Observation> observations = new ConditionalWeakTable<TUnit, Observation>();

        // Whether the zone check must run for this mounted rider now. A tick with native motion is native's own check
        // and is recorded as such.
        public bool NeedsRecheck(TUnit rider, MountedPair instance, bool nativeMotionThisTick, float x, float y, float z)
        {
            if (rider == null || instance == null) return false;
            Observation last;
            var known = observations.TryGetValue(rider, out last) && ReferenceEquals(last.Instance, instance);
            var needs = MountedScriptZonePolicy.RiderNeedsZoneRecheck(true, nativeMotionThisTick, known,
                known ? x - last.X : 0f, known ? y - last.Y : 0f, known ? z - last.Z : 0f);
            if (!needs && nativeMotionThisTick) Record(rider, instance, x, y, z);
            return needs;
        }

        public void Record(TUnit rider, MountedPair instance, float x, float y, float z)
        {
            if (rider == null || instance == null) return;
            Observation last;
            if (!observations.TryGetValue(rider, out last))
            {
                last = new Observation();
                observations.Add(rider, last);
            }
            last.Instance = instance;
            last.X = x; last.Y = y; last.Z = z;
        }
    }
}
