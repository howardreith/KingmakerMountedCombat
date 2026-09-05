namespace KingmakerMountedCombat.Domain
{
    /// <summary>
    /// Pure admission policy for the Horse-suite-only native end-turn input used to
    /// traverse exact fixture-controlled turns. Production turn scheduling never
    /// calls this policy.
    /// </summary>
    internal static class DiagnosticTurnTraversalPolicy
    {
        internal const int RequiredStableFrames = 2;

        internal static bool IsProhibitedMountedMountTurn(
            bool currentIsMount,
            bool currentIsExpected,
            bool relationshipMounted)
        {
            return currentIsMount && !currentIsExpected && relationshipMounted;
        }

        internal static bool CanForceEndExactFixtureTurn(
            bool currentPresent,
            bool currentIsExpected,
            bool currentIsPairActor,
            bool pairActorPassAuthorized,
            bool rosterReferenceExact,
            bool nonPairLeaseReferenceExact,
            bool directlyControllable,
            bool samePlayerParty,
            bool actionableTurn,
            bool commandsIdle,
            bool handsIdle,
            bool equipmentIdle,
            bool pairWorkIdle,
            bool pendingNextUnitClear,
            bool waitingForUiClear,
            bool defaultUnpausedTurnBasedMode,
            bool alreadyEnded,
            int stableFrames)
        {
            var ownershipExact = currentIsPairActor
                ? pairActorPassAuthorized
                : nonPairLeaseReferenceExact;
            return currentPresent && !currentIsExpected && ownershipExact &&
                rosterReferenceExact && directlyControllable && samePlayerParty &&
                actionableTurn && commandsIdle && handsIdle && equipmentIdle &&
                pairWorkIdle && pendingNextUnitClear && waitingForUiClear &&
                defaultUnpausedTurnBasedMode && !alreadyEnded &&
                stableFrames >= RequiredStableFrames;
        }
    }
}
