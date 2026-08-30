using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Diagnostics
{
    public enum NativeMountedAbilityActivationPhase
    {
        TargetSelectionStarted,
        CastRequested,
        TargetSelectionEnded,
        CastRefused,
        DispatchStarted,
        DispatchCompleted,
        CommandTerminal,
        RelationshipEnded
    }

    public sealed class NativeMountedAbilityActivationRecord
    {
        public long Sequence { get; set; }

        public long ActivationId { get; set; }

        public NativeMountedAbilityActivationPhase Phase { get; set; }

        public NativeMountedControlKind Kind { get; set; }

        public string AbilityGuid { get; set; }

        public int Frame { get; set; }

        public string CasterId { get; set; }

        public string ActiveSelectedUnitIds { get; set; }

        public string TargetId { get; set; }

        public bool TargetSelectionMode { get; set; }

        public RelationshipState RelationshipStateAtStart { get; set; }

        public RelationshipState RelationshipStateObserved { get; set; }

        public string RiderIdAtStart { get; set; }

        public string MountIdAtStart { get; set; }

        public string RiderViewAtStart { get; set; }

        public string MountViewAtStart { get; set; }

        public string RiderViewObserved { get; set; }

        public string MountViewObserved { get; set; }

        public bool RiderViewChanged { get; set; }

        public bool MountViewChanged { get; set; }

        public bool InCombat { get; set; }

        public bool TurnBased { get; set; }

        public string GameMode { get; set; }

        public string CurrentTurnUnitId { get; set; }

        public long LifecycleSequenceAtStart { get; set; }

        public long LifecycleSequenceObserved { get; set; }

        public string LifecycleDeliveries { get; set; }

        public CleanupTrigger? CleanupTrigger { get; set; }

        public bool? DispatchAccepted { get; set; }

        public bool RelationshipEnded { get; set; }

        public bool RelationshipTransitionChanged { get; set; }

        public string RelationshipTransitionResult { get; set; }

        public string TerminalResult { get; set; }
    }

    public sealed class NativeMountedAbilityActivationLedger
    {
        private const int MaximumRetainedRecords = 512;
        private readonly object gate = new object();
        private readonly List<NativeMountedAbilityActivationRecord> records =
            new List<NativeMountedAbilityActivationRecord>();
        private long sequence;
        private long activationSequence;

        public long BeginActivation()
        {
            lock (gate)
            {
                activationSequence++;
                return activationSequence;
            }
        }

        public void Record(NativeMountedAbilityActivationRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }
            if (record.ActivationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(record), "An activation record requires a positive activation identity.");
            }
            if (record.Kind == NativeMountedControlKind.None)
            {
                throw new ArgumentException("An activation record requires an exact KMC ability kind.", nameof(record));
            }

            lock (gate)
            {
                sequence++;
                record.Sequence = sequence;
                records.Add(record);
                if (records.Count > MaximumRetainedRecords)
                {
                    records.RemoveAt(0);
                }
            }
        }

        public IReadOnlyList<NativeMountedAbilityActivationRecord> Snapshot()
        {
            lock (gate)
            {
                return records.ToArray();
            }
        }
    }
}
