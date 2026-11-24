namespace KingmakerMountedCombat.Domain
{
    // MMR-03 (review R1-F5): newer player work that entered the rider's or mount's native container overrides a retained
    // turn-based ordinary order's horse follow-through for that order. The pair's own command, native reactions (attacks of
    // opportunity) and Free/Swift actions that coexist with an attack do not.
    public static class MountedFollowThroughPolicy
    {
        public static bool OverridesRetainedOrder(bool pairOwnCommand, bool reaction, bool freeOrSwift, bool playerOrder) =>
            playerOrder && !pairOwnCommand && !reaction && !freeOrSwift;
    }
}
