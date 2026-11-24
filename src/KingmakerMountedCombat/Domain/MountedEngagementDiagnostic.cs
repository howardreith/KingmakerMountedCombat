namespace KingmakerMountedCombat.Domain
{
    // MMR-04 owner-log evidence (review R1-F11): where real-time automatic engagement first stopped, read once per combat a
    // few seconds after it began. Target choice is the rider brain's own native proposal (KMC adds no target selection);
    // this names whether a proposal arrived, was refused (with the refusal), was accepted but not dispatched yet, or is
    // engaging (dispatched or travelling).
    public static class MountedEngagementDiagnostic
    {
        public const double ReportAfterSeconds = 3d;

        public static string FirstStop(long proposals, long refused, long admittedOrContinued, long dispatched, long travelStarts,
            bool liveCommand, string lastRefusal) =>
            dispatched > 0 || liveCommand ? "engaging" :
            travelStarts > 0 ? "travelling" :
            proposals == 0 ? "no-proposal" :
            admittedOrContinued > 0 ? "accepted-not-dispatched" :
            refused > 0 ? "refused:" + (lastRefusal ?? "<unknown>") : "proposal-unresolved";
    }
}
