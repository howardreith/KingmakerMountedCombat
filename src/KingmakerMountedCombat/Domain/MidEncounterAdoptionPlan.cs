using System;

namespace KingmakerMountedCombat.Domain
{
    // An immutable, generation-bound decision about how a paired activation
    // created during a running encounter will dispose of the transition round.
    //
    // It exists because relationship attachment and encounter adoption must be
    // ONE transaction. The plan is taken from exact live state before the
    // relationship is committed, revalidated immediately before that commit, and
    // then carried into the commit itself, so a successful native Mount can never
    // leave a mounted relationship whose paired activation could not be created.
    //
    // Every field is an observation. The plan writes nothing, reserves nothing and
    // grants nothing; it only records what was true when the transition was
    // admitted so that a change between admission and delivery is detected by
    // exact comparison rather than assumed not to have happened.
    public sealed class MidEncounterAdoptionPlan
    {
        public MidEncounterAdoptionPlan(
            MidEncounterAdoption disposition,
            long relationshipGeneration,
            string encounterSessionId,
            string riderId,
            string mountId,
            bool turnBasedCombat,
            int roundNumber,
            string currentTurnUnitId,
            int riderRosterIndex,
            int mountRosterIndex,
            bool mountSurprised,
            bool mountActingInSurpriseRound,
            bool mountVisibleToPlayer,
            bool riderInCombat,
            bool mountInCombat,
            bool partyInCombat,
            bool riderAbleToAct,
            bool mountAbleToAct,
            bool riderConscious,
            bool mountConscious)
        {
            if (disposition == MidEncounterAdoption.Unavailable)
            {
                throw new ArgumentOutOfRangeException(nameof(disposition),
                    "An unavailable disposition is a refusal, never a plan.");
            }
            if (string.IsNullOrWhiteSpace(riderId) || string.IsNullOrWhiteSpace(mountId))
            {
                throw new ArgumentException("An adoption plan requires both exact actor identities.");
            }
            if (string.Equals(riderId, mountId, StringComparison.Ordinal))
            {
                throw new ArgumentException("An adoption plan requires two distinct actors.");
            }

            Disposition = disposition;
            RelationshipGeneration = relationshipGeneration;
            EncounterSessionId = encounterSessionId;
            RiderId = riderId;
            MountId = mountId;
            TurnBasedCombat = turnBasedCombat;
            RoundNumber = roundNumber;
            CurrentTurnUnitId = currentTurnUnitId;
            RiderRosterIndex = riderRosterIndex;
            MountRosterIndex = mountRosterIndex;
            MountSurprised = mountSurprised;
            MountActingInSurpriseRound = mountActingInSurpriseRound;
            MountVisibleToPlayer = mountVisibleToPlayer;
            RiderInCombat = riderInCombat;
            MountInCombat = mountInCombat;
            PartyInCombat = partyInCombat;
            RiderAbleToAct = riderAbleToAct;
            MountAbleToAct = mountAbleToAct;
            RiderConscious = riderConscious;
            MountConscious = mountConscious;
        }

        public MidEncounterAdoption Disposition { get; }

        public long RelationshipGeneration { get; }

        public string EncounterSessionId { get; }

        public string RiderId { get; }

        public string MountId { get; }

        public bool TurnBasedCombat { get; }

        public int RoundNumber { get; }

        public string CurrentTurnUnitId { get; }

        public int RiderRosterIndex { get; }

        public int MountRosterIndex { get; }

        public bool MountSurprised { get; }

        public bool MountActingInSurpriseRound { get; }

        public bool MountVisibleToPlayer { get; }

        public bool RiderInCombat { get; }

        public bool MountInCombat { get; }

        public bool PartyInCombat { get; }

        public bool RiderAbleToAct { get; }

        public bool MountAbleToAct { get; }

        public bool RiderConscious { get; }

        public bool MountConscious { get; }

        // A live encounter is a precondition of the whole operation, so a plan
        // whose encounter has ended is invalid regardless of anything else.
        public bool EncounterStillLive => RiderInCombat || MountInCombat || PartyInCombat;

        // The relationship commit advances the mounted-pair generation by exactly
        // one. Rebinding the plan to that committed generation keeps the commit's
        // own check an exact equality rather than a tolerance, and a generation
        // that moved by anything other than one is rejected here.
        public MidEncounterAdoptionPlan WithCommittedGeneration(long committedGeneration)
        {
            if (committedGeneration != RelationshipGeneration + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(committedGeneration),
                    "A committed relationship advances the mounted-pair generation by exactly one.");
            }
            return new MidEncounterAdoptionPlan(Disposition, committedGeneration, EncounterSessionId,
                RiderId, MountId, TurnBasedCombat, RoundNumber, CurrentTurnUnitId,
                RiderRosterIndex, MountRosterIndex, MountSurprised, MountActingInSurpriseRound,
                MountVisibleToPlayer, RiderInCombat, MountInCombat, PartyInCombat,
                RiderAbleToAct, MountAbleToAct, RiderConscious, MountConscious);
        }

        // Exact equality over every observed fact. Nothing is treated as
        // equivalent, tolerated or rounded: any difference invalidates the plan.
        public bool Matches(MidEncounterAdoptionPlan other)
        {
            return other != null &&
                Disposition == other.Disposition &&
                RelationshipGeneration == other.RelationshipGeneration &&
                string.Equals(EncounterSessionId, other.EncounterSessionId, StringComparison.Ordinal) &&
                string.Equals(RiderId, other.RiderId, StringComparison.Ordinal) &&
                string.Equals(MountId, other.MountId, StringComparison.Ordinal) &&
                TurnBasedCombat == other.TurnBasedCombat &&
                RoundNumber == other.RoundNumber &&
                string.Equals(CurrentTurnUnitId, other.CurrentTurnUnitId, StringComparison.Ordinal) &&
                RiderRosterIndex == other.RiderRosterIndex &&
                MountRosterIndex == other.MountRosterIndex &&
                MountSurprised == other.MountSurprised &&
                MountActingInSurpriseRound == other.MountActingInSurpriseRound &&
                MountVisibleToPlayer == other.MountVisibleToPlayer &&
                RiderInCombat == other.RiderInCombat &&
                MountInCombat == other.MountInCombat &&
                PartyInCombat == other.PartyInCombat &&
                RiderAbleToAct == other.RiderAbleToAct &&
                MountAbleToAct == other.MountAbleToAct &&
                RiderConscious == other.RiderConscious &&
                MountConscious == other.MountConscious;
        }

        // The first observed difference, for a truthful refusal. Null when the
        // plans match exactly.
        public string DescribeDifference(MidEncounterAdoptionPlan other)
        {
            if (other == null) return "the encounter could not be observed again";
            if (Disposition != other.Disposition)
                return "the round's participation disposition changed from " + Disposition + " to " + other.Disposition;
            if (RelationshipGeneration != other.RelationshipGeneration)
                return "the mounted relationship generation changed";
            if (!string.Equals(EncounterSessionId, other.EncounterSessionId, StringComparison.Ordinal))
                return "the encounter session changed";
            if (!string.Equals(RiderId, other.RiderId, StringComparison.Ordinal) ||
                !string.Equals(MountId, other.MountId, StringComparison.Ordinal))
                return "the rider or companion identity changed";
            if (TurnBasedCombat != other.TurnBasedCombat)
                return "the combat mode changed";
            if (RoundNumber != other.RoundNumber)
                return "the round advanced";
            if (!string.Equals(CurrentTurnUnitId, other.CurrentTurnUnitId, StringComparison.Ordinal))
                return "the current turn moved to another actor";
            if (RiderRosterIndex != other.RiderRosterIndex || MountRosterIndex != other.MountRosterIndex)
                return "the initiative order changed";
            if (MountSurprised != other.MountSurprised ||
                MountActingInSurpriseRound != other.MountActingInSurpriseRound ||
                MountVisibleToPlayer != other.MountVisibleToPlayer)
                return "the companion's surprise or visibility state changed";
            if (RiderInCombat != other.RiderInCombat || MountInCombat != other.MountInCombat ||
                PartyInCombat != other.PartyInCombat)
                return "the encounter's liveness changed";
            if (RiderAbleToAct != other.RiderAbleToAct || MountAbleToAct != other.MountAbleToAct)
                return "an actor's ability to act changed";
            if (RiderConscious != other.RiderConscious || MountConscious != other.MountConscious)
                return "an actor's consciousness changed";
            return null;
        }

        public string Describe() =>
            "disposition=" + Disposition + ";generation=" + RelationshipGeneration +
            ";session=" + (EncounterSessionId ?? "<none>") +
            ";rider=" + RiderId + ";mount=" + MountId +
            ";turnBased=" + TurnBasedCombat + ";round=" + RoundNumber +
            ";currentTurn=" + (CurrentTurnUnitId ?? "<none>") +
            ";riderIndex=" + RiderRosterIndex + ";mountIndex=" + MountRosterIndex +
            ";mountSurprised=" + MountSurprised +
            ";mountActingInSurpriseRound=" + MountActingInSurpriseRound +
            ";mountVisible=" + MountVisibleToPlayer +
            ";riderInCombat=" + RiderInCombat + ";mountInCombat=" + MountInCombat +
            ";partyInCombat=" + PartyInCombat +
            ";riderAbleToAct=" + RiderAbleToAct + ";mountAbleToAct=" + MountAbleToAct +
            ";riderConscious=" + RiderConscious + ";mountConscious=" + MountConscious;
    }
}
