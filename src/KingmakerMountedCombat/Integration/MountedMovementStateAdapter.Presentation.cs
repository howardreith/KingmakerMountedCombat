using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedMovementStateAdapter
    {
        // Known issues Sprint 7 (U01): whether the mount's allocation for this exact activation has recorded forced
        // (charge) travel, the fact that already closes its ordinary locomotion. Read only: it never creates, replaces,
        // resets or writes an allocation, TimeMoved, a cooldown or a native counter.
        internal bool ForcedTravelClosesActivation(UnitEntityData mount, string activationIdentity)
        {
            Allocation allocation;
            if (mount == null || !allocations.TryGetValue(mount, out allocation) || allocation == null) return false;
            return MountedMovementDisplayPolicy.LocomotionClosed(activationIdentity, allocation.GrantIdentity,
                allocation.Prepared, allocation.Movement.ForcedTravelRecorded);
        }
    }
}
