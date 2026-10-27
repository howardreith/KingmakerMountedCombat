using System;

namespace KingmakerMountedCombat.Diagnostics
{
    // F2 r39: the six-pair exploration guard (no native combat) failed once without any
    // record of who entered combat. The guard is unchanged and still fails the run; this
    // pure classifier only names what the recorded combat entry shows. A hostile combatant is
    // neither a pair actor, a party member nor player faction. A native hostile engagement is
    // "uncovered" only while no KMC-issued command or pair command is open and no KMC input,
    // ground move, relationship transition or preparation is in flight; anything else stays an
    // unclassified six-pair FAIL. It never suppresses AI, fences the world or removes units.
    // b210 r39: on the combat-entry frame the party's own native AI had already queued commands
    // (the sampling window open on the previous frame had proved every actor settled), so the
    // criterion is the KMC-issued custody, not every pair actor's native queue.
    internal static class Chunk7SixPairCombatEntry
    {
        internal const string UncoveredNativeHostileEngagement = "uncovered-native-hostile-engagement";
        internal const string Unclassified = "unclassified-six-pair-combat-entry";

        // Kingmaker 2.1.7b UnitEntityData.VisionRangeMeters: 11.7 m for player faction or any unit in combat, otherwise the
        // unit's override or 8.5 m; UnitEntityData.HasLOS compares the squared 2D distance. A group joins combat as a whole
        // once any member's group memory holds a qualifying enemy (UnitMemoryController/UnitCombatJoinController). The party
        // engages only an enemy that remembers it, so the clearance is the observing unit's own current vision range.
        internal const float CombatVisionMeters = 11.7f;
        // The margin covers native idle wander between frames and the declared 2 m ground route.
        internal const float ClearanceMarginMeters = 3f;

        internal static double Distance2D(double ax, double az, double bx, double bz) => Math.Sqrt((ax - bx) * (ax - bx) + (az - bz) * (az - bz));

        internal static double RequiredClearanceMeters(float visionRangeMeters) =>
            visionRangeMeters + ClearanceMarginMeters;

        internal static bool ClearanceHolds(double distance2D, float visionRangeMeters, bool extendedVisionCoversPoint) =>
            !extendedVisionCoversPoint && !double.IsNaN(distance2D) && !float.IsNaN(visionRangeMeters) && !float.IsInfinity(visionRangeMeters) && visionRangeMeters > 0f &&
            distance2D >= RequiredClearanceMeters(visionRangeMeters);

        // F3 r39: hostile 86c73c36 passed the before-stress clearance at 19.35 m, then roamed natively about 10 m and saw an
        // eastern pair actor at 8.1 m. Kingmaker 2.1.7b UnitRoamingController ticks every idle out-of-combat unit that has a
        // UnitPartRoaming (not Disabled): it walks to its NextPoint with UnitMoveTo(point, 1f); a random roamer rolls each next
        // point inside OriginalPoint + Settings.Radius (ObstacleAnalyzer.TraceAlongNavmesh can only shorten it toward the
        // origin); a waypoint roamer follows its RoamingWaypointView NextWaypoints/PrevWaypoints graph. The sixth-Horse save
        // holds that group's random roaming origin (12.80, 12.73), and three records saw its members 6.1-7.4 m from it.
        // The native wander envelope, not the current position, bounds where such a unit can stand during the stress.
        internal const float RoamingApproachMeters = 1f;

        internal static double SegmentDistance2D(double px, double pz, double ax, double az, double bx, double bz)
        {
            var dx = bx - ax; var dz = bz - az; var length = dx * dx + dz * dz;
            var t = length <= 0 ? 0 : Math.Max(0, Math.Min(1, ((px - ax) * dx + (pz - az) * dz) / length));
            return Distance2D(px, pz, ax + t * dx, az + t * dz);
        }

        // Distance from a point to a random roamer's native wander disk (its rolled points plus the 1 m roaming approach).
        internal static double RoamingDiskDistance2D(double px, double pz, double originX, double originZ, double radius) =>
            double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0 ? double.NaN :
            Math.Max(0, Distance2D(px, pz, originX, originZ) - radius - RoamingApproachMeters);

        // Distance from a point to one waypoint leg walked natively (the leg plus the 1 m roaming approach at its ends).
        internal static double RoamingLegDistance2D(double px, double pz, double ax, double az, double bx, double bz) =>
            Math.Max(0, SegmentDistance2D(px, pz, ax, az, bx, bz) - RoamingApproachMeters);

        // Cycle-1 ground goals: the declared 2 m route along +x first (every pair whose route is clear keeps it), then
        // bounded rings out to 10 m, nearest to +x first. A goal is admitted only when native vision clearance holds from
        // every hostile's current position and native wander envelope; none refuses as uncovered.
        internal const float GroundGoalSpacingMeters = 1.75f;
        internal static float[] GroundGoalDistances() => new[] { 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f };
        internal static float[] GroundGoalBearings()
        {
            var offsets = new float[16]; offsets[0] = 0f;
            for (var i = 1; i < 16; i++) offsets[i] = (i % 2 == 1 ? 1f : -1f) * 22.5f * ((i + 1) / 2);
            offsets[15] = 180f;
            return offsets;
        }

        internal static bool IsHostileCombatant(bool pairActor, bool inParty, bool playerFaction) => !pairActor && !inParty && !playerFaction;

        internal static string Classify(int hostileCombatants, bool kmcCommandsSettled, bool kmcInputInFlight) =>
            hostileCombatants > 0 && kmcCommandsSettled && !kmcInputInFlight ? UncoveredNativeHostileEngagement : Unclassified;

        internal static string Message(string classification) =>
            classification == UncoveredNativeHostileEngagement
                ? "Six-pair exploration entered unexpected native combat: uncovered native hostile engagement with no KMC-issued or pair command open and no KMC input in flight."
                : "Six-pair exploration entered unexpected native combat.";
    }
}
