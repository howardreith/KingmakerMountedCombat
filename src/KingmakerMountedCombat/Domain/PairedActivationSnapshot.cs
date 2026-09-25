using System;

namespace KingmakerMountedCombat.Domain
{
    // Save data describes entitlement, not live actors or process-local contexts.
    public sealed class PairedActorSnapshot
    {
        public bool Granted { get; }
        public bool Prepared { get; }
        public bool Ended { get; }
        public float StandardObserved { get; }
        public float MoveObserved { get; }
        public float SwiftObserved { get; }
        public bool ForfeitRecorded { get; }
        public bool ForfeitSettled { get; }
        public float ForfeitStandardAdded { get; }

        public PairedActorSnapshot(bool granted, bool prepared, bool ended,
            float standardObserved, float moveObserved, float swiftObserved,
            bool forfeitRecorded, bool forfeitSettled, float forfeitStandardAdded)
        {
            if (prepared && !granted || ended && !granted ||
                !FiniteDebt(standardObserved) || !FiniteDebt(moveObserved) || !FiniteDebt(swiftObserved) ||
                !FiniteDebt(forfeitStandardAdded) || !granted &&
                (standardObserved != 0f || moveObserved != 0f || swiftObserved != 0f) ||
                forfeitRecorded && (!prepared || !ended) || forfeitSettled && !forfeitRecorded ||
                !forfeitRecorded && forfeitStandardAdded != 0f)
                throw new ArgumentException("Saved actor participation is inconsistent.");
            Granted = granted; Prepared = prepared; Ended = ended;
            StandardObserved = standardObserved; MoveObserved = moveObserved; SwiftObserved = swiftObserved;
            ForfeitRecorded = forfeitRecorded; ForfeitSettled = forfeitSettled;
            ForfeitStandardAdded = forfeitStandardAdded;
        }

        private static bool FiniteDebt(float value) => !float.IsNaN(value) && !float.IsInfinity(value) &&
            value >= 0f && value <= 86400f;
    }

    public sealed class PairedActivationSnapshot
    {
        public Guid EncounterId { get; }
        public long Sequence { get; }
        public bool Ending { get; }
        public bool Finalized { get; }
        public bool Split { get; }
        public bool Suspended { get; }
        public PairedActorSnapshot Rider { get; }
        public PairedActorSnapshot Mount { get; }

        public PairedActivationSnapshot(Guid encounterId, long sequence, bool ending,
            bool finalized, bool split, bool suspended, PairedActorSnapshot rider, PairedActorSnapshot mount)
        {
            if (encounterId == Guid.Empty || sequence < 0 || sequence == long.MaxValue ||
                sequence == 0 && (rider != null || mount != null || ending || finalized || suspended) ||
                sequence > 0 && (rider == null || mount == null) || finalized && !ending)
                throw new ArgumentException("Saved activation identity or state is inconsistent.");
            if (sequence > 0 && (finalized && (rider.Granted && !rider.Ended || mount.Granted && !mount.Ended) ||
                suspended && (ending || finalized || !rider.Prepared || !mount.Prepared ||
                rider.Ended || mount.Ended)))
                throw new ArgumentException("Saved activation completion or suspension is inconsistent.");
            EncounterId = encounterId; Sequence = sequence; Ending = ending; Finalized = finalized;
            Split = split; Suspended = suspended; Rider = rider; Mount = mount;
        }
    }
}
