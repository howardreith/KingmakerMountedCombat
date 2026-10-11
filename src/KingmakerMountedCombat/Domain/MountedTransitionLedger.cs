using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public enum MountedTransitionKind
    {
        VoluntaryMount = 0,
        VoluntaryDismount = 1,
        ForcedDetach = 2
    }

    public sealed class MountedTransitionRecord
    {
        internal MountedTransitionRecord(
            MountedTransitionKind kind,
            string controlIdentity,
            string riderId,
            string mountId,
            long generationBefore)
        {
            Kind = kind;
            ControlIdentity = controlIdentity;
            RiderId = riderId;
            MountId = mountId;
            GenerationBefore = generationBefore;
        }

        public MountedTransitionKind Kind { get; }

        public string ControlIdentity { get; }

        public string RiderId { get; }

        public string MountId { get; }

        public long GenerationBefore { get; }

        public bool Settled { get; internal set; }

        public bool Accepted { get; internal set; }

        public string Trigger { get; internal set; }

        public override string ToString() =>
            Kind + ":" + (ControlIdentity ?? "<cleanup>") + ";rider=" + (RiderId ?? "<none>") +
            ";mount=" + (MountId ?? "<none>") + ";generationBefore=" + GenerationBefore +
            ";settled=" + Settled + ";accepted=" + Accepted +
            (Trigger == null ? string.Empty : ";trigger=" + Trigger);
    }

    // One ledger for every mounted relationship transition. It makes a repeated
    // delivery of the same native control idempotent, refuses a second voluntary
    // transition while one is in flight, and keeps forced cleanup permanently
    // distinguishable from a player-paid voluntary action so cleanup can never
    // book a voluntary cost.
    public sealed class MountedTransitionLedger
    {
        private const int MaxRetained = 24;

        private readonly List<MountedTransitionRecord> records = new List<MountedTransitionRecord>();
        private MountedTransitionRecord inFlight;

        public long AdmittedMountCount { get; private set; }

        public long AdmittedDismountCount { get; private set; }

        public long AcceptedMountCount { get; private set; }

        public long AcceptedDismountCount { get; private set; }

        public long RefusedVoluntaryCount { get; private set; }

        public long ForcedDetachCount { get; private set; }

        public long DuplicateControlSuppressedCount { get; private set; }

        public long ConcurrentControlSuppressedCount { get; private set; }

        public bool HasVoluntaryTransitionInFlight => inFlight != null;

        public string InFlightControlIdentity => inFlight?.ControlIdentity;

        public IReadOnlyList<MountedTransitionRecord> Records => records;

        public bool TryAdmitVoluntary(
            MountedTransitionKind kind,
            string controlIdentity,
            string riderId,
            string mountId,
            long generationBefore,
            out MountedTransitionRecord record,
            out string refusal)
        {
            record = null;
            if (kind != MountedTransitionKind.VoluntaryMount && kind != MountedTransitionKind.VoluntaryDismount)
            {
                throw new ArgumentOutOfRangeException(nameof(kind),
                    "Forced detach is recorded through RecordForcedDetach, never admitted as voluntary.");
            }

            if (string.IsNullOrWhiteSpace(controlIdentity))
            {
                refusal = "A voluntary mounted transition requires an exact native control identity.";
                RefusedVoluntaryCount++;
                return false;
            }

            if (inFlight != null)
            {
                ConcurrentControlSuppressedCount++;
                refusal = "A mounted transition is already in flight.";
                return false;
            }

            if (Find(controlIdentity) != null)
            {
                DuplicateControlSuppressedCount++;
                refusal = "This exact mounted transition has already been delivered.";
                return false;
            }

            record = new MountedTransitionRecord(kind, controlIdentity, riderId, mountId, generationBefore);
            Append(record);
            inFlight = record;
            if (kind == MountedTransitionKind.VoluntaryMount)
            {
                AdmittedMountCount++;
            }
            else
            {
                AdmittedDismountCount++;
            }
            refusal = null;
            return true;
        }

        public void Settle(MountedTransitionRecord record, bool accepted)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }
            if (!ReferenceEquals(record, inFlight))
            {
                throw new InvalidOperationException("Only the in-flight voluntary transition can be settled.");
            }

            record.Settled = true;
            record.Accepted = accepted;
            inFlight = null;
            if (!accepted)
            {
                RefusedVoluntaryCount++;
                return;
            }

            if (record.Kind == MountedTransitionKind.VoluntaryMount)
            {
                AcceptedMountCount++;
            }
            else
            {
                AcceptedDismountCount++;
            }
        }

        // Cleanup is never gated and never books a voluntary cost. Repeated
        // cleanup for the same generation is recorded once.
        public bool RecordForcedDetach(string riderId, string mountId, long generationBefore, string trigger)
        {
            var identity = "cleanup:" + (riderId ?? "<none>") + ":" + (mountId ?? "<none>") + ":" + generationBefore;
            var existing = Find(identity);
            if (existing != null)
            {
                DuplicateControlSuppressedCount++;
                return false;
            }

            var record = new MountedTransitionRecord(
                MountedTransitionKind.ForcedDetach, identity, riderId, mountId, generationBefore)
            {
                Settled = true,
                Accepted = true,
                Trigger = trigger
            };
            Append(record);
            ForcedDetachCount++;
            return true;
        }

        public MountedTransitionRecord Find(string controlIdentity)
        {
            if (string.IsNullOrWhiteSpace(controlIdentity))
            {
                return null;
            }

            for (var index = records.Count - 1; index >= 0; index--)
            {
                if (string.Equals(records[index].ControlIdentity, controlIdentity, StringComparison.Ordinal))
                {
                    return records[index];
                }
            }
            return null;
        }

        public string Describe() =>
            "admittedMount=" + AdmittedMountCount + ";acceptedMount=" + AcceptedMountCount +
            ";admittedDismount=" + AdmittedDismountCount + ";acceptedDismount=" + AcceptedDismountCount +
            ";refusedVoluntary=" + RefusedVoluntaryCount + ";forcedDetach=" + ForcedDetachCount +
            ";duplicateSuppressed=" + DuplicateControlSuppressedCount +
            ";concurrentSuppressed=" + ConcurrentControlSuppressedCount +
            ";inFlight=" + (InFlightControlIdentity ?? "<none>");

        private void Append(MountedTransitionRecord record)
        {
            records.Add(record);
            while (records.Count > MaxRetained)
            {
                // Never evict the in-flight record.
                if (ReferenceEquals(records[0], inFlight))
                {
                    break;
                }
                records.RemoveAt(0);
            }
        }
    }
}
