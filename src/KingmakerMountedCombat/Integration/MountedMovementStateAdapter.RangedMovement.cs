using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedMovementStateAdapter
    {
        // The mounted ranged-movement rule's one reading of a mount's travel. Unlike the diagnostic PeekMovement copy it
        // applies the ordinary allocation lifetime first and refuses an allocation that is not current for the pair's
        // activation (the caller supplies that activation's grant). It never creates, replaces, resets or writes an
        // allocation, a native counter or an action.
        internal bool TryReadCurrentMountTravel(UnitEntityData mount, string pairedGrant, bool pairedLifecycle,
            out float normalTravelSeconds, out string allocationIdentity)
        {
            normalTravelSeconds = 0f;
            allocationIdentity = null;
            if (mount == null) return false;
            MaintainLifetimes();
            var controller = Game.Instance?.TurnBasedCombatController;
            Allocation allocation;
            if (controller == null || !allocations.TryGetValue(mount, out allocation)) return false;
            var roundIsCurrent = allocation.Round == controller.RoundNumber && allocation.RoundStart == controller.RoundStartTime.Ticks;
            if (!MountedRangedMovementPolicy.IsAllocationCurrent(ReferenceEquals(allocation.Controller, controller),
                allocation.GrantIdentity, pairedGrant, pairedLifecycle, roundIsCurrent)) return false;
            var movement = allocation.Movement;
            normalTravelSeconds = MountedRangedMovementPolicy.NormalTravelSeconds(movement.TimeMoved, movement.TimeForced, movement.TimeStepped);
            allocationIdentity = (allocation.GrantIdentity ?? "round") + "@" + allocation.Round;
            return true;
        }
    }
}
