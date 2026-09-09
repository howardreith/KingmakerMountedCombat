namespace KingmakerMountedCombat.Domain
{
    // A native mixed weapon plan may end after its ranged attacks when the
    // remaining melee attacks are out of reach. That terminal result does not
    // remove the player's RT repeat order. Native budgets still gate the next
    // routine; no attack, recovery clock, plan or target is changed here.
    internal static class MountedRangedRoutineCompletionPolicy
    {
        internal static bool CanRetainIntent(
            bool turnBased, bool ordinaryRiderRanged, bool nativeSequenceTick,
            bool nativeInterrupt, bool exactTargetValid, int planned, int completed,
            bool deliveredPrefixAllRanged, bool remainingOnlyOutOfRangeMelee,
            bool rangedStillInRange)
        {
            return !turnBased && ordinaryRiderRanged && nativeSequenceTick &&
                nativeInterrupt && exactTargetValid && completed > 0 && completed < planned &&
                deliveredPrefixAllRanged && remainingOnlyOutOfRangeMelee && rangedStillInRange;
        }
    }
}