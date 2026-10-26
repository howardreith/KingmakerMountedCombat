using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Diagnostics
{
    // F3 r39: the pure part of the six-pair cycle-1 ground goal selection (Chunk7WorldScenarioEngine.SixPairClearance).
    // The engine reads the native navmesh, unit footprints, roaming parts and extended vision areas; this class only orders
    // the candidates, measures the native wander envelopes and applies the hostile vision clearance. Read-only.
    internal static class Chunk7SixPairGroundGoalPolicy
    {
        internal const string HostileWanderVision = "hostile-wander-vision";
        internal const string HostileExtendedVision = "hostile-extended-vision";
        internal const string PairSpacing = "pair-spacing";

        // A native Dismount stands the rider beside its mount: the F3 r39 record shows 2.08-3.55 m. A ground goal keeps the
        // vision clearance for every point the dismounted rider can take there.
        internal const float DismountReachMeters = 4f;

        // The first candidate (GroundGoalDistances x GroundGoalBearings, the declared +x 2 m route first) the refusal admits.
        // firstRefusal is called once per reason, for the first candidate refused with it.
        internal static bool Search(Func<float, float, string> refusal, Action<string, float, float> firstRefusal,
            out float distance, out float bearing, out int tried)
        {
            var reasons = new HashSet<string>(StringComparer.Ordinal); tried = 0;
            foreach (var d in Chunk7SixPairCombatEntry.GroundGoalDistances())
            {
                foreach (var b in Chunk7SixPairCombatEntry.GroundGoalBearings())
                {
                    tried++;
                    var reason = refusal(d, b);
                    if (reason == null) { distance = d; bearing = b; return true; }
                    if (reasons.Add(reason)) firstRefusal(reason, d, b);
                }
            }
            distance = float.NaN; bearing = float.NaN; return false;
        }

        // Each other pair's goal (earlier pairs) or mount (later pairs) keeps the declared spacing from the goal.
        internal static bool SpacingHolds(double goalX, double goalZ, IEnumerable<double[]> others)
        {
            foreach (var other in others)
                if (Chunk7SixPairCombatEntry.Distance2D(goalX, goalZ, other[0], other[1]) < Chunk7SixPairCombatEntry.GroundGoalSpacingMeters) return false;
            return true;
        }

        // Native vision plus margin from the nearer of each hostile's current position and wander envelope, less the dismount
        // reach; then the hostile's extended vision area. Clearance is the smallest such reach (double.MaxValue with no hostile).
        internal static string HostileRefusal(double goalX, double goalZ, IList<SixHostile> hostiles, Func<int, bool> extendedVisionCovers, out double clearance)
        {
            clearance = double.MaxValue;
            for (var i = 0; i < hostiles.Count; i++)
            {
                var hostile = hostiles[i];
                var envelope = hostile.Envelope.Distance2D(goalX, goalZ);
                var current = Chunk7SixPairCombatEntry.Distance2D(goalX, goalZ, hostile.X, hostile.Z);
                var reach = Math.Min(envelope, current) - DismountReachMeters;
                clearance = Math.Min(clearance, reach);
                if (!Chunk7SixPairCombatEntry.ClearanceHolds(reach, hostile.VisionRangeMeters, false)) return HostileWanderVision;
                if (extendedVisionCovers(i)) return HostileExtendedVision;
            }
            return null;
        }

        // Kingmaker 2.1.7b UnitRoamingController.AdvancePoint asks the reached waypoint for its next point in the current
        // direction (NextWaypoints.WeightedRandom()?.Waypoint?.WaypointData, or PrevWaypoints.Random()?.WaypointData), flips the
        // direction once when that is null and leaves NextPoint null when the other direction is null too; the next tick then
        // rolls a random point around OriginalPoint + Settings.Radius (RollNextPoint). A direction may be null when its list is
        // empty or holds an entry without a waypoint or waypoint data.
        internal static bool WaypointMayFallBackToRandom(int nextCount, bool nextHasNullEntry, int prevCount, bool prevHasNullEntry) =>
            (nextCount == 0 || nextHasNullEntry) && (prevCount == 0 || prevHasNullEntry);
    }

    internal sealed class SixHostile
    {
        internal SixHostile(SixWanderEnvelope envelope, double x, double z, float visionRangeMeters)
        { Envelope = envelope; X = x; Z = z; VisionRangeMeters = visionRangeMeters; }
        internal SixWanderEnvelope Envelope { get; }
        internal double X { get; }
        internal double Z { get; }
        internal float VisionRangeMeters { get; }
    }

    // Kingmaker 2.1.7b UnitRoamingController / UnitPartRoaming: where an idle out-of-combat unit can stand natively. A unit
    // without an enabled roaming part stands where it is; a random roamer walks to points rolled inside OriginalPoint +
    // Settings.Radius; a waypoint roamer follows its NextWaypoints/PrevWaypoints graph and, where a reached waypoint can
    // yield no point in either direction, the random disk. Each walk ends within the 1 m roaming approach of its point; the
    // leg toward the current NextPoint is included. An unreadable envelope yields distance 0 (the goal refuses).
    internal sealed class SixWanderEnvelope
    {
        internal const string Stationary = "stationary";
        internal const string RoamingDisabled = "roaming-disabled";
        internal const string Random = "random";
        internal const string Waypoints = "waypoints";
        internal const string WaypointsUnreadable = "waypoints-unreadable";

        private readonly double x, z, nextX, nextZ, originX, originZ, radius;
        private readonly bool disk;
        private readonly double[][] nodes, legs;

        private SixWanderEnvelope(string kind, double x, double z, double nextX, double nextZ, bool disk, double originX, double originZ,
            double radius, double[][] nodes, double[][] legs)
        {
            Kind = kind; this.x = x; this.z = z; this.nextX = nextX; this.nextZ = nextZ; this.disk = disk;
            this.originX = originX; this.originZ = originZ; this.radius = radius; this.nodes = nodes; this.legs = legs;
        }

        internal string Kind { get; }

        internal static SixWanderEnvelope Standing(bool roamingDisabled, double x, double z) =>
            new SixWanderEnvelope(roamingDisabled ? RoamingDisabled : Stationary, x, z, x, z, false, 0, 0, 0, null, null);

        internal static SixWanderEnvelope Unreadable(double x, double z) =>
            new SixWanderEnvelope(WaypointsUnreadable, x, z, x, z, false, 0, 0, 0, null, null);

        // nextX/nextZ: the current NextPoint, or the unit's own position without one.
        internal static SixWanderEnvelope RandomDisk(double x, double z, double nextX, double nextZ, double originX, double originZ, double radius) =>
            new SixWanderEnvelope(Random, x, z, nextX, nextZ, true, originX, originZ, radius, null, null);

        // nodes: {x, z}; legs: {ax, az, bx, bz}. The disk counts only when a reached waypoint may fall back to it.
        internal static SixWanderEnvelope WaypointGraph(double x, double z, double nextX, double nextZ, IList<double[]> nodes, IList<double[]> legs,
            bool randomFallback, double originX, double originZ, double radius)
        {
            var n = new double[nodes.Count][]; for (var i = 0; i < n.Length; i++) n[i] = nodes[i];
            var l = new double[legs.Count][]; for (var i = 0; i < l.Length; i++) l[i] = legs[i];
            return new SixWanderEnvelope(Waypoints, x, z, nextX, nextZ, randomFallback, originX, originZ, radius, n, l);
        }

        internal double Distance2D(double px, double pz)
        {
            if (Kind == Stationary || Kind == RoamingDisabled) return Chunk7SixPairCombatEntry.Distance2D(px, pz, x, z);
            if (Kind == WaypointsUnreadable) return 0d;
            var result = Chunk7SixPairCombatEntry.RoamingLegDistance2D(px, pz, x, z, nextX, nextZ);
            if (disk)
            {
                var d = Chunk7SixPairCombatEntry.RoamingDiskDistance2D(px, pz, originX, originZ, radius);
                if (double.IsNaN(d)) return 0d;
                result = Math.Min(result, d);
            }
            if (Kind == Waypoints)
            {
                var nearestNode = double.MaxValue;
                foreach (var w in nodes) nearestNode = Math.Min(nearestNode, Math.Max(0, Chunk7SixPairCombatEntry.Distance2D(px, pz, w[0], w[1]) - Chunk7SixPairCombatEntry.RoamingApproachMeters));
                var nearestLeg = double.MaxValue;
                foreach (var l in legs) nearestLeg = Math.Min(nearestLeg, Chunk7SixPairCombatEntry.RoamingLegDistance2D(px, pz, l[0], l[1], l[2], l[3]));
                result = Math.Min(result, Math.Min(nearestNode, nearestLeg));
            }
            return result;
        }
    }
}
