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

        internal static bool IsHostileCombatant(bool pairActor, bool inParty, bool playerFaction) => !pairActor && !inParty && !playerFaction;

        internal static string Classify(int hostileCombatants, bool kmcCommandsSettled, bool kmcInputInFlight) =>
            hostileCombatants > 0 && kmcCommandsSettled && !kmcInputInFlight ? UncoveredNativeHostileEngagement : Unclassified;

        internal static string Message(string classification) =>
            classification == UncoveredNativeHostileEngagement
                ? "Six-pair exploration entered unexpected native combat: uncovered native hostile engagement with no KMC-issued or pair command open and no KMC input in flight."
                : "Six-pair exploration entered unexpected native combat.";
    }
}
