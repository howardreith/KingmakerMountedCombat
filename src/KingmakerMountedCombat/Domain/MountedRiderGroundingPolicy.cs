namespace KingmakerMountedCombat.Domain
{
    internal static class MountedRiderGroundingPolicy
    {
        public static bool ShouldSuppress<TView>(
            RelationshipState state,
            bool activePairPresent,
            TView activeRiderView,
            TView candidateView)
            where TView : class
        {
            return state == RelationshipState.Mounted &&
                activePairPresent &&
                activeRiderView != null &&
                candidateView != null &&
                ReferenceEquals(activeRiderView, candidateView);
        }
    }
}
