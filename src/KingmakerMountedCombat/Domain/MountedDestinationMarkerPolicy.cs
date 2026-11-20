namespace KingmakerMountedCombat.Domain
{
    // Known issues sprint 3, M04. Native destination markers, from the installed Kingmaker 2.1.7b contracts
    // (ClickPointerManager, UnitEntityView.OnMovementStarted and MoveTo, UnitMovementAgent.PathTo and OnPathComplete):
    //   - One pointer object per unit, kept for the life of the manager. Only AddPointer creates or moves it, from
    //     UnitEntityView.OnMovementStarted, which the unit's own stock movement agent calls when a path it requested
    //     completes.
    //   - Every frame the pointer is shown while its unit holds a live UnitMoveTo with ShowTargetMarker (in the Move
    //     slot, or the first one queued) or a live UnitAreaTransition, and hidden otherwise. Nothing checks that the
    //     pointer was placed for that command.
    //   - A command that is not yet close enough plans its approach through UnitEntityView.MoveTo, which always asks
    //     the stock agent (AgentASP.PathTo), whatever agent override is installed. PathTo and the path callback do not
    //     test whether that agent is enabled.
    // A mounted rider's stock agent is switched off and cannot move the rider, but it still plans every command the
    // rider itself holds, and each completed plan places the rider's own pointer at the end of the rider's own path.
    public static class MountedDestinationMarkerPolicy
    {
        // The entrance carry leaves the rider holding its own live member command while the mount walks for the pair,
        // so native places two markers: the mount's and, from the rider's own plan, the rider's. The pair's marker is
        // the mount's. The rider's placement is skipped only while the mount executes the member command the carry
        // issued for that same rider command: a rider command nothing carries keeps the marker native gives it.
        public static bool CarriedRiderPlacesNoPointer(bool pairMounted, bool unitIsRider, bool mountExecutesCarrierOfRidersCommand)
        {
            return pairMounted && unitIsRider && mountExecutesCarrierOfRidersCommand;
        }

        // A pointer the rider already had, placed for some earlier walk of its own, would be shown where it was last
        // put for as long as the rider holds its member command. It is retired when the mount's member command was
        // admitted, and only then.
        public static bool RetireRiderPointer(bool carrierAdmitted, bool riderPointerExists)
        {
            return carrierAdmitted && riderPointerExists;
        }

        // A move the pair gives its mount for no player order (the scene regroup walk) carries no destination marker,
        // as the attack, carried-approach and recovery moves already do not.
        public const bool UnorderedMountMoveShowsMarker = false;
    }
}
