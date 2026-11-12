namespace KingmakerMountedCombat.Domain
{
    // Horse speed repair (2026-11-12). Native group pace, from the installed Kingmaker 2.1.7b contracts:
    //   - ClickGroundHandler.MoveSelectedUnitsToPoint gives every member of a multi-unit selection the same
    //     UnitMoveTo.SpeedLimit, the slowest selected ModifiedSpeedMps floored at 30 ft per round (3.048 m/s).
    //   - The only mouse-play route to the accelerated ("running") group move is UnitMovementAgent.OnPathComplete ->
    //     UnitCommand.AccelerateIfPathIsLong. The first command whose new path is at least 12.7 m accelerates itself
    //     (Charge gait, SpeedLimit x1.8) and then offers Accelerate() to every command of every selected unit.
    //     Each Accelerate() re-applies the native CanAccelerate refusals: the Accelerated Move setting, player origin,
    //     a Normal-gait order, combat, a speed-affecting condition (SpeedIsAffected) and party encumbrance.
    // Selection projects a mounted pair's mount onto its rider, and the ground click's RunCommand runs the rider's
    // order on the mount. The native offer therefore reaches the rider's empty command slots and never the mount that
    // executes the order, which kept the unaccelerated group limit while the party ran ahead. This policy decides
    // when the pair's routed ground order receives the same native offer; the offer itself stays native.
    public enum MountedGroupPaceCarry
    {
        NotApplicable,
        OfferNativeAcceleration
    }

    public static class MountedGroupPacePolicy
    {
        // AccelerateIfPathIsLong offers the group acceleration exactly when its own command became accelerated in
        // that call: CanAccelerate requires a command that was not yet accelerated, and only the long-path branch sets it.
        public static bool NativeGroupOfferRan(bool triggerAcceleratedBefore, bool triggerAcceleratedAfter)
        {
            return !triggerAcceleratedBefore && triggerAcceleratedAfter;
        }

        public static MountedGroupPaceCarry Decide(
            bool nativeGroupOfferRan,
            bool pairMounted,
            bool turnBasedCombat,
            bool riderSelected,
            bool mountSelected,
            bool mountExecutesRoutedGroundOrder,
            bool orderFinished,
            bool orderAccelerated)
        {
            // The native offer reaches selected units only; a pair whose rider was not selected received no order to carry.
            if (!nativeGroupOfferRan || !pairMounted || !riderSelected) return MountedGroupPaceCarry.NotApplicable;
            // A selected mount (the native turn-based paired-partner selection) already receives the native offer itself.
            if (mountSelected) return MountedGroupPaceCarry.NotApplicable;
            // Exploration only: turn-based movement keeps its budgets and the native in-combat refusal.
            if (turnBasedCombat) return MountedGroupPaceCarry.NotApplicable;
            // Only the exact order the ground click routed from the rider. Attack, door, trap, spell and recovery
            // approaches are separate owned moves, and an already accelerated order is left as it is.
            if (!mountExecutesRoutedGroundOrder || orderFinished || orderAccelerated) return MountedGroupPaceCarry.NotApplicable;
            return MountedGroupPaceCarry.OfferNativeAcceleration;
        }
    }
}
