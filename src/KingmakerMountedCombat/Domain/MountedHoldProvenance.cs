namespace KingmakerMountedCombat.Domain
{
    // Review R1-F8. The pair's movement owner is the mount, so an automatic attack honors the mount's effective Hold as well
    // as the rider's. Native writes UnitEntityData.HoldState (2.1.7b) in four places: the player's Hold (SelectionManagerBase
    // .Hold, selected units) and Stop (cleared), UnitCommands.Run (cleared for a non-AI command) and UnitMoveTo.OnAction (set
    // on a player-faction executor in real-time combat when its move acts). The last one also fires for the pair's own
    // delegated UnitMoveTo carrier, an artifact of carrying the attack's approach on a move command (an unmounted attacker's
    // UnitAttack approach sets no Hold); KMC restores that write. The mount's Hold therefore restricts automatic movement only
    // when the player put it there: a Hold input with the mount selected, or a completed player move (PlayerMoveHold). A Hold
    // flag that predates this observation (for example one an earlier build's own carrier left in a save) is not the player's.
    public sealed class MountedHoldProvenance
    {
        private bool playerHold;

        public bool PlayerHoldObserved => playerHold;

        public void ObservePlayerHoldInput(bool mountSelected)
        {
            if (mountSelected) playerHold = true;
        }

        public void ObservePlayerStopInput(bool mountSelected)
        {
            if (mountSelected) playerHold = false;
        }

        // Every update: once native has cleared the mount's Hold (a new order, Stop), the player's earlier Hold is gone.
        public void Observe(bool mountHoldNow)
        {
            if (!mountHoldNow) playerHold = false;
        }

        public bool MountHeldByPlayer(bool mountHoldNow) => mountHoldNow && playerHold;

        public static bool AutomaticMovementForbidden(bool riderMovementForbidden, bool completedPlayerMoveHold, bool mountCanMove,
            bool mountHeldByPlayer) =>
            riderMovementForbidden || completedPlayerMoveHold || !mountCanMove || mountHeldByPlayer;

        // UnitMoveTo.OnAction's Hold write is undone only for the pair's own exact delegated move.
        public static bool RestoreHoldAfterMoveAction(bool exactOwnDelegatedMove, bool holdBefore, bool holdAfter) =>
            exactOwnDelegatedMove && holdBefore != holdAfter;

        // UnitCommands.Run clears the owner's Hold for a non-AI command. An automatic order's exact delegated move keeps the
        // mount's Hold (it is not the player's order); a player's attack order clears it as native clears it for any new order.
        public static bool NativeRunKeepsMountHold(bool exactRetainedOwnMove, bool automaticOrder) =>
            exactRetainedOwnMove && automaticOrder;
    }
}
