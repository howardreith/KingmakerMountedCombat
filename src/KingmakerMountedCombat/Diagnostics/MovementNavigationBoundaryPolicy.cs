using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Diagnostics
{
    internal enum MovementNavigationBoundaryAction
    {
        Continue,
        AbortUnexpectedRelationshipLoss,
        AbortExternalCombat
    }

    internal static class MovementNavigationBoundaryPolicy
    {
        public static MovementNavigationBoundaryAction Classify(
            bool expectsMountedRelationship,
            bool relationshipMounted,
            bool pairUnitInCombat,
            bool partyInCombat,
            bool lastTransitionSucceeded,
            CleanupTrigger? lastTransitionTrigger)
        {
            var exactCombatTransition = !relationshipMounted && lastTransitionSucceeded &&
                lastTransitionTrigger == CleanupTrigger.CombatStarted;
            if (pairUnitInCombat || partyInCombat || exactCombatTransition)
            {
                return MovementNavigationBoundaryAction.AbortExternalCombat;
            }

            if (expectsMountedRelationship && !relationshipMounted)
            {
                return MovementNavigationBoundaryAction.AbortUnexpectedRelationshipLoss;
            }

            return MovementNavigationBoundaryAction.Continue;
        }

        public static bool IsCombatControllerSelectionRewrite(
            MovementNavigationBoundaryAction boundaryAction,
            bool selectionRestoredAtCleanupBoundary,
            bool selectionMatchesAfterCleanupFrame)
        {
            return boundaryAction == MovementNavigationBoundaryAction.AbortExternalCombat &&
                selectionRestoredAtCleanupBoundary &&
                !selectionMatchesAfterCleanupFrame;
        }

        public static bool SuppressesRemainingOutOfCombatRows(MovementNavigationBoundaryAction boundaryAction)
        {
            return boundaryAction == MovementNavigationBoundaryAction.AbortExternalCombat;
        }
    }
}
