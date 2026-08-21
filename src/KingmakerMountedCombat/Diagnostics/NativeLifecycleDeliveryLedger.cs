using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Diagnostics
{
    public enum NativeLifecycleBoundary
    {
        SaveRequest,
        LoadStart,
        AreaBeginUnload,
        AreaScenesLoaded,
        AreaDidLoad,
        AreaLoadingComplete,
        TurnBasedEnabled,
        RealtimeEnabled,
        GameModeStarted,
        GameModeStopped,
        CombatStarted,
        CombatEnded,
        ViewAttached,
        ViewDetachedOrUnitDestroyed,
        PartyRemoved,
        InGameStateChanged,
        UnitIncapacitated,
        UnitDeath,
        UnitFinallyDead,
        ModDisable
    }

    public sealed class NativeLifecycleDeliveryRecord
    {
        public long Sequence { get; set; }

        public NativeLifecycleBoundary Boundary { get; set; }

        public string Source { get; set; }

        public RelationshipState StateBefore { get; set; }

        public RelationshipState StateAfter { get; set; }

        public CleanupTrigger? CleanupTrigger { get; set; }

        public bool CleanupAttempted { get; set; }

        public bool CleanupSucceeded { get; set; }

        public IReadOnlyList<string> CleanupErrors { get; set; }
    }

    public sealed class NativeLifecycleDeliveryLedger
    {
        private const int MaximumRetainedRecords = 256;
        private readonly object gate = new object();
        private readonly List<NativeLifecycleDeliveryRecord> records = new List<NativeLifecycleDeliveryRecord>();
        private long sequence;

        public void Record(
            NativeLifecycleBoundary boundary,
            string source,
            RelationshipState stateBefore,
            RelationshipState stateAfter,
            CleanupTrigger? cleanupTrigger,
            bool cleanupAttempted,
            bool cleanupSucceeded,
            IReadOnlyList<string> cleanupErrors = null)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("A native lifecycle source is required.", nameof(source));
            }
            if (cleanupAttempted && !cleanupTrigger.HasValue)
            {
                throw new ArgumentException("A cleanup attempt requires an exact cleanup trigger.", nameof(cleanupTrigger));
            }
            if (!cleanupAttempted && cleanupTrigger.HasValue)
            {
                throw new ArgumentException("A non-cleanup observation cannot claim a cleanup trigger.", nameof(cleanupTrigger));
            }
            if (!cleanupAttempted && !cleanupSucceeded)
            {
                throw new ArgumentException("A pure observation cannot report cleanup failure.", nameof(cleanupSucceeded));
            }
            var errors = cleanupErrors == null ? new string[0] : new List<string>(cleanupErrors).ToArray();
            foreach (var error in errors)
            {
                if (string.IsNullOrWhiteSpace(error)) { throw new ArgumentException("Cleanup errors must be non-empty strings.", nameof(cleanupErrors)); }
            }

            lock (gate)
            {
                sequence++;
                records.Add(new NativeLifecycleDeliveryRecord
                {
                    Sequence = sequence,
                    Boundary = boundary,
                    Source = source,
                    StateBefore = stateBefore,
                    StateAfter = stateAfter,
                    CleanupTrigger = cleanupTrigger,
                    CleanupAttempted = cleanupAttempted,
                    CleanupSucceeded = cleanupSucceeded,
                    CleanupErrors = errors
                });
                if (records.Count > MaximumRetainedRecords)
                {
                    records.RemoveAt(0);
                }
            }
        }

        public IReadOnlyList<NativeLifecycleDeliveryRecord> Snapshot()
        {
            lock (gate)
            {
                return records.ToArray();
            }
        }
    }
}
