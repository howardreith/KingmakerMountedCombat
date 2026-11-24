namespace KingmakerMountedCombat.Domain
{
    // MMR-04 owner-log evidence (reviews R1-F11, R2): where real-time AUTOMATIC engagement stood, read once per combat a few
    // seconds after it began. Target choice is the rider brain's own native proposal (KMC adds no target selection). The
    // counters are automatic-intent deltas only; a live player operation is named rather than counted; motion is the mount's
    // own displacement, so a dispatched attack that never moved the pair is distinguished from one that did.
    public static class MountedEngagementDiagnostic
    {
        public const double ReportAfterSeconds = 3d;
        public const float DisplacementEpsilonMetres = 0.25f;

        public static string FirstStop(long proposals, long refused, long acceptedOrContinued, long dispatched, float mountDisplacementMetres,
            bool playerOperation, string lastRefusal) =>
            playerOperation ? "player-command" :
            dispatched > 0 && mountDisplacementMetres >= DisplacementEpsilonMetres ? "dispatched-moved" :
            dispatched > 0 ? "dispatched-not-moved" :
            proposals == 0 ? "no-proposal" :
            acceptedOrContinued > 0 ? "accepted-not-dispatched" :
            refused > 0 ? "refused:" + (lastRefusal ?? "<unknown>") : "proposal-unresolved";
    }
}
