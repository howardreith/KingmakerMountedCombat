namespace KingmakerMountedCombat.Domain
{
    // MMR-03/04/05 actor eligibility at the stock dispatcher (review R1-F4/F5/F6/F10).
    public static class MountedStrikeReadinessPolicy
    {
        // Whether an actor's own strike may be dispatched now. Turn-based: its Standard action, its own capability and its
        // own addressability on the activation (an individually ended or incapable actor is skipped, so its partner can
        // still act). Real time: additionally every native start wait that would leave a dispatched command unstarted
        // (initiative, busy hands, a scheduled equipment update); those waits permit travel, they do not permit a strike.
        public static bool CanStrikeNow(bool turnBased, bool hasStandardAction, bool capable, bool addressable,
            bool waitingInitiative, bool handsBusy, bool equipmentUpdateScheduled) =>
            hasStandardAction && capable && addressable &&
            (turnBased || !waitingInitiative && !handsBusy && !equipmentUpdateScheduled);

        // Whether the waiting pair may travel: the mount itself can act and move and no Hold / completed player move
        // forbids an automatic intent's movement.
        public static bool MayTravelWhileWaiting(bool mountCanAct, bool mountCanMove, bool movementForbidden) =>
            mountCanAct && mountCanMove && !movementForbidden;
    }
}
