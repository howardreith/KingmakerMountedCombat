namespace KingmakerMountedCombat.Domain
{
    // How a paired activation created during a running encounter disposes of the
    // partner's participation in the transition round.
    public enum MidEncounterAdoption
    {
        // The pair's participation cannot be disposed of unambiguously, so the
        // voluntary combat transition is refused before any native commitment.
        Unavailable = 0,

        // Real time has no native turn slots. Adoption takes encounter ownership
        // only: no boundary, no grant, no preparation, and the existing
        // already-running-encounter renewal floor applies.
        RealTimeOwnership = 1,

        // The partner's own native slot in this round is still ahead of the
        // principal's running turn and is provably not skipped. The partner
        // receives its one native preparation as the paired partner and its own
        // later slot is suppressed.
        PreparePartnerThisRound = 2,

        // The partner's own native slot in this round is already behind the
        // principal's running turn. Its participation stands exactly as it is and
        // no native preparation runs.
        RetainPartnerParticipation = 3
    }

    // Kingmaker's turn-based round is a strictly positional walk over the sorted
    // roster: CombatController.ChooseNextUnit (0x06000BD2) finds the index of
    // CurrentTurn.Unit and steps forward with wraparound, calling StartRound
    // (0x06000BD3) exactly at the wrap point, and StartRound is the only in-round
    // re-sort. A roster position before the running principal has therefore
    // already taken its slot in this round, and a position after it has not.
    //
    // ChooseNextUnit skips a candidate when its TBUnitInfo is Surprised, when it
    // is ActingInSurpriseRound without an offensive command, or when it is not an
    // enemy and not visible to the player. Preparing a partner whose own slot
    // would have been skipped would add participation, so every uncertainty about
    // that predicate refuses the transition instead.
    public static class MidEncounterAdoptionPolicy
    {
        public static MidEncounterAdoption Resolve(
            bool turnBasedCombat,
            bool currentTurnIsExactRider,
            int riderRosterIndex,
            int mountRosterIndex,
            bool mountSurprised,
            bool mountActingInSurpriseRound,
            bool mountVisibleToPlayer)
        {
            if (!turnBasedCombat)
            {
                return MidEncounterAdoption.RealTimeOwnership;
            }

            if (!currentTurnIsExactRider || riderRosterIndex < 0 || mountRosterIndex < 0 ||
                mountRosterIndex == riderRosterIndex)
            {
                return MidEncounterAdoption.Unavailable;
            }

            if (mountRosterIndex < riderRosterIndex)
            {
                return MidEncounterAdoption.RetainPartnerParticipation;
            }

            if (mountSurprised || mountActingInSurpriseRound || !mountVisibleToPlayer)
            {
                return MidEncounterAdoption.Unavailable;
            }

            return MidEncounterAdoption.PreparePartnerThisRound;
        }

        public static string DescribeUnavailable(
            bool turnBasedCombat,
            bool currentTurnIsExactRider,
            int riderRosterIndex,
            int mountRosterIndex)
        {
            if (!turnBasedCombat)
            {
                return null;
            }

            if (!currentTurnIsExactRider)
            {
                return "Mount Companion during turn-based combat belongs to the rider's current turn.";
            }

            if (riderRosterIndex < 0 || mountRosterIndex < 0)
            {
                return "Rider and mount must both be in this encounter's initiative order to mount during it.";
            }

            if (mountRosterIndex == riderRosterIndex)
            {
                return "Rider and mount cannot share one initiative slot.";
            }

            return "The companion's own turn in this round cannot be resolved; mount on a later round.";
        }
    }
}
