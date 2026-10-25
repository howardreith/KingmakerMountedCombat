namespace KingmakerMountedCombat.Diagnostics
{
    // F2 r39: the six-pair exploration guard (no native combat) failed once without any
    // record of who entered combat. The guard is unchanged and still fails the run; this
    // pure classifier only names what the recorded combat entry shows. A hostile combatant is
    // neither a pair actor, a party member nor player faction. A native hostile
    // engagement is "uncovered" only while every pair actor is command-free and no KMC
    // input, ground move or relationship transition is in flight; anything else stays an
    // unclassified six-pair FAIL. It never suppresses AI, fences the world or removes units.
    internal static class Chunk7SixPairCombatEntry
    {
        internal const string UncoveredNativeHostileEngagement = "uncovered-native-hostile-engagement";
        internal const string Unclassified = "unclassified-six-pair-combat-entry";

        internal static bool IsHostileCombatant(bool pairActor, bool inParty, bool playerFaction) => !pairActor && !inParty && !playerFaction;

        internal static string Classify(int hostileCombatants, bool pairActorsCommandFree, bool kmcInputInFlight) =>
            hostileCombatants > 0 && pairActorsCommandFree && !kmcInputInFlight ? UncoveredNativeHostileEngagement : Unclassified;

        internal static string Message(string classification) =>
            classification == UncoveredNativeHostileEngagement
                ? "Six-pair exploration entered unexpected native combat: uncovered native hostile engagement while the pairs were idle with no KMC input in flight."
                : "Six-pair exploration entered unexpected native combat.";
    }
}
