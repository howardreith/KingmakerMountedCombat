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

        // Known issues M04 closeout. The mount's delegated approach for a mounted door click carries no destination
        // marker. A native door click is a UnitInteractWithObject, which the native visibility rule never marks; the
        // product's other delegated approaches (attack, carried interactions, recovery, regroup) already ask for none.
        // Before the closeout the door approach alone asked for one, so a mounted door click showed a green marker an
        // unmounted one never does.
        public const bool DelegatedInteractionApproachShowsMarker = false;

        // Known issues M04 closeout. Native ClickUnitHandler.OnClick (060093ED) gives each selected unit that cannot
        // attack a UnitMoveTo toward the clicked unit, its one UnitMoveTo, outside the ground-click routing. A mounted
        // rider cannot walk it (its stock agent is off), yet its own stock agent plans it, so native places and shows the
        // rider's marker for as long as the order lives, and it lives until something else replaces it. That order is
        // the pair's approach: it is given to the mount through the existing ground-order lifecycle (admission, routing,
        // the watched player move) and the rider's own copy is not run. Only inside the native unit click, only an exact
        // UnitMoveTo on the mounted pair's rider, never an AI or simulated one.
        public static bool RoutesRiderClickApproach(bool pairMounted, bool containerIsRider, bool exactUnitMoveTo, bool aiAction,
            bool insideNativeUnitClick, bool simulatingClick)
        {
            return pairMounted && containerIsRider && exactUnitMoveTo && !aiAction && insideNativeUnitClick && !simulatingClick;
        }
    }

    // Known issues M04 closeout: what one unit's native destination pointer shows, and why.
    public enum MountedMarkerClass
    {
        // No pointer object for the unit.
        NoPointer,
        // A pointer object exists and is hidden: not a visible marker, and never stale.
        HiddenPointer,
        // The physical mover's pointer, placed for the live command that shows it.
        ExpectedPhysicalMover,
        // A live command placed its pointer and the unit is not moving (blocked or stopped short): native, kept.
        ExpectedUnfinishedMove,
        // A live command shows the unit's pointer where an earlier command placed it, and its own plan is still due
        // (inside the bounded transition native always has between a new command and its completed plan).
        PlacementPending,
        // Rider and mount both show a marker for the pair.
        DuplicatePairMarkers,
        // A visible pointer whose unit holds no live command that native shows it for (native never leaves one).
        VisibleWithoutQualifyingCommand,
        // A live command shows the pointer where an earlier command placed it, past the bounded transition.
        PlacedForEarlierCommand,
        // The mounted rider holds a live command native marks while the mount is the pair's physical mover.
        RiderHeldWhileMountMoves,
        // A move no player ordered asks for a marker.
        InternalMoveRequestsMarker
    }

    // The facts read for one unit's pointer: native table state, the command the native visibility rule reads, and the
    // last placement recorded for the unit.
    public sealed class MountedMarkerFacts
    {
        public bool PointerExists, PointerVisible;
        // The native rule: a live UnitMoveTo with ShowTargetMarker (Move slot, or the first one queued) or a live
        // UnitAreaTransition.
        public bool QualifyingCommandLive;
        // That command was ordered by no player (not created by the player, no AI, no scene).
        public bool QualifyingCommandUnordered;
        // The unit's last recorded placement was made for that same command.
        public bool PlacedForCurrentCommand;
        // How long the observation has seen that command.
        public float SecondsSinceCommandSeen;
        public bool Moving;
    }

    public static class MountedMarkerOwnershipPolicy
    {
        // A new command's own plan normally completes within frames; past this, a pointer it shows at an earlier place
        // is no longer a transition.
        public const float PlacementTransitionSeconds = 3f;

        public static MountedMarkerClass ClassifyUnit(MountedMarkerFacts unit)
        {
            if (unit == null || !unit.PointerExists) return MountedMarkerClass.NoPointer;
            if (!unit.PointerVisible) return MountedMarkerClass.HiddenPointer;
            if (!unit.QualifyingCommandLive) return MountedMarkerClass.VisibleWithoutQualifyingCommand;
            if (unit.QualifyingCommandUnordered) return MountedMarkerClass.InternalMoveRequestsMarker;
            if (!unit.PlacedForCurrentCommand)
                return unit.SecondsSinceCommandSeen < PlacementTransitionSeconds ? MountedMarkerClass.PlacementPending : MountedMarkerClass.PlacedForEarlierCommand;
            return unit.Moving ? MountedMarkerClass.ExpectedPhysicalMover : MountedMarkerClass.ExpectedUnfinishedMove;
        }

        // A mounted pair: the rider never shows a marker of its own while the mount is the physical mover (the carried
        // entrance rider's placement is skipped and its earlier pointer retired). Otherwise the pair's state is the
        // mount's.
        public static MountedMarkerClass ClassifyPair(bool pairMounted, MountedMarkerFacts rider, MountedMarkerFacts mount)
        {
            var riderClass = ClassifyUnit(rider);
            var mountClass = ClassifyUnit(mount);
            // Unmounted, each unit is its own physical mover: an anomaly of either, else the one that shows a marker.
            if (!pairMounted)
            {
                if (IsAnomaly(riderClass)) return riderClass;
                if (IsAnomaly(mountClass)) return mountClass;
                return riderClass != MountedMarkerClass.NoPointer && riderClass != MountedMarkerClass.HiddenPointer ? riderClass : mountClass;
            }
            var riderShows = rider != null && rider.PointerExists && rider.PointerVisible;
            var mountShows = mount != null && mount.PointerExists && mount.PointerVisible;
            if (riderShows && rider.QualifyingCommandLive)
                return mountShows && mount.QualifyingCommandLive ? MountedMarkerClass.DuplicatePairMarkers : MountedMarkerClass.RiderHeldWhileMountMoves;
            if (IsAnomaly(riderClass)) return riderClass;
            return mountClass;
        }

        public static bool IsAnomaly(MountedMarkerClass value)
        {
            switch (value)
            {
                case MountedMarkerClass.DuplicatePairMarkers:
                case MountedMarkerClass.VisibleWithoutQualifyingCommand:
                case MountedMarkerClass.PlacedForEarlierCommand:
                case MountedMarkerClass.RiderHeldWhileMountMoves:
                case MountedMarkerClass.InternalMoveRequestsMarker:
                    return true;
                default:
                    return false;
            }
        }
    }
}
