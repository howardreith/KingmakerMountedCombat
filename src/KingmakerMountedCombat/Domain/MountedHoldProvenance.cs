namespace KingmakerMountedCombat.Domain
{
    // Reviews R1-F8 (rounds 3 and 4). The pair's movement owner is the mount, so an automatic attack honors the mount's Hold
    // as well as the rider's. Native writes UnitEntityData.HoldState (2.1.7b) in four places: the player's Hold
    // (SelectionManagerBase.Hold) and Stop (cleared), UnitCommands.Run (cleared for a non-AI command) and UnitMoveTo.OnAction
    // (set on a player-faction executor in real-time combat when its move acts). The last also fires for the pair's own
    // delegated UnitMoveTo carrier, an artifact of carrying the attack's approach on a move command (an unmounted attacker's
    // UnitAttack approach sets no Hold); KMC undoes exactly that write in OnAction's postfix. Every other mount Hold is
    // honored, including one that predates the pair (set before mounting, or left in a save): the only exception is a Hold
    // for which KMC has positive provenance that its own carrier produced it and the undo did not take place.
    public sealed class MountedHoldProvenance
    {
        private bool carrierInduced;

        public bool CarrierInducedHold => carrierInduced;

        // The pair's own carrier set Hold in UnitMoveTo.OnAction and the undo could not be applied.
        public void ObserveUnrestoredCarrierHold() => carrierInduced = true;

        // Every update and at the player's Hold or Stop: once the flag is clear, or the player has expressed Hold, the
        // carrier provenance no longer applies.
        public void Observe(bool mountHoldNow)
        {
            if (!mountHoldNow) carrierInduced = false;
        }

        public void ObservePlayerHoldInput(bool mountSelected)
        {
            if (mountSelected) carrierInduced = false;
        }

        public bool MountHoldRestrictsAutomatic(bool mountHoldNow) => mountHoldNow && !carrierInduced;

        public static bool AutomaticMovementForbidden(bool riderMovementForbidden, bool completedPlayerMoveHold, bool mountCanMove,
            bool mountHoldRestricts) =>
            riderMovementForbidden || completedPlayerMoveHold || !mountCanMove || mountHoldRestricts;

        // UnitMoveTo.OnAction's Hold write is undone only for the pair's own exact delegated move.
        public static bool RestoreHoldAfterMoveAction(bool exactOwnDelegatedMove, bool holdBefore, bool holdAfter) =>
            exactOwnDelegatedMove && holdBefore != holdAfter;

        // UnitCommands.Run clears the owner's Hold for a non-AI command. An automatic order's exact delegated move keeps the
        // mount's Hold (it is not the player's order); a player's attack order clears it as native clears it for any new order.
        public static bool NativeRunKeepsMountHold(bool exactRetainedOwnMove, bool automaticOrder) =>
            exactRetainedOwnMove && automaticOrder;
    }
}
