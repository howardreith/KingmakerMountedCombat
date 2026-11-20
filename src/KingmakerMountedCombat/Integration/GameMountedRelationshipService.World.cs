using System;
using KingmakerMountedCombat.Domain;
namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class GameMountedRelationshipService
    {
        internal void AdvanceIdleBoundaryGeneration()
        {
            RefreshOwnership();
            if (PairOwner != null || coordinator.State != RelationshipState.Unmounted || RetainsCleanupObligations)
                throw new InvalidOperationException("An unfinished owner cannot recycle its boundary.");
            mountedPairGeneration = checked(mountedPairGeneration + 1);
        }
    }
}
