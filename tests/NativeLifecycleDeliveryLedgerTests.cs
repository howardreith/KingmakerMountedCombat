using System;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class NativeLifecycleDeliveryLedgerTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("native lifecycle ledger preserves delivery order and cleanup state", PreservesDeliveryOrderAndCleanupState);
            runner.Run("native lifecycle ledger rejects ambiguous cleanup claims", RejectsAmbiguousCleanupClaims);
            runner.Run("native lifecycle ledger rejects empty or unbounded observation detail", RejectsInvalidObservationDetail);
            runner.Run("native lifecycle ledger retains a bounded recent window", RetainsBoundedRecentWindow);
        }

        private static void PreservesDeliveryOrderAndCleanupState()
        {
            var ledger = new NativeLifecycleDeliveryLedger();
            ledger.Record(
                NativeLifecycleBoundary.SaveRequest,
                "SaveManager.SaveRoutine prefix",
                RelationshipState.Mounted,
                RelationshipState.Unmounted,
                CleanupTrigger.SaveRequested,
                true,
                false,
                new[] { "diagnostic cleanup failure" },
                "presentation snapshot");
            ledger.Record(
                NativeLifecycleBoundary.AreaLoadingComplete,
                "IAreaLoadingStagesHandler.OnAreaLoadingComplete",
                RelationshipState.Unmounted,
                RelationshipState.Unmounted,
                null,
                false,
                true);

            var records = ledger.Snapshot();
            TestRunner.Equal(2, records.Count, "Native delivery records were lost.");
            TestRunner.Equal(1L, records[0].Sequence, "First native delivery sequence changed.");
            TestRunner.Equal(2L, records[1].Sequence, "Second native delivery sequence changed.");
            TestRunner.Equal(CleanupTrigger.SaveRequested, records[0].CleanupTrigger.Value, "Save cleanup trigger changed.");
            TestRunner.Equal(RelationshipState.Unmounted, records[0].StateAfter, "Save did not record clean state.");
            TestRunner.Equal("diagnostic cleanup failure", records[0].CleanupErrors[0], "Cleanup diagnostics were not preserved.");
            TestRunner.Equal("presentation snapshot", records[0].Detail, "Observation detail was not preserved separately from the canonical source.");
            TestRunner.Equal("SaveManager.SaveRoutine prefix", records[0].Source, "Observation detail changed the canonical native source.");
            TestRunner.Equal(false, records[1].CleanupAttempted, "Area completion fabricated a cleanup attempt.");
        }

        private static void RejectsAmbiguousCleanupClaims()
        {
            var ledger = new NativeLifecycleDeliveryLedger();
            ExpectArgumentException(() => ledger.Record(
                NativeLifecycleBoundary.LoadStart,
                "test",
                RelationshipState.Mounted,
                RelationshipState.Mounted,
                null,
                true,
                false), "Cleanup without an exact trigger was accepted.");
            ExpectArgumentException(() => ledger.Record(
                NativeLifecycleBoundary.ViewAttached,
                "test",
                RelationshipState.Unmounted,
                RelationshipState.Unmounted,
                CleanupTrigger.ViewDetached,
                false,
                true), "Pure observation claimed a cleanup trigger.");
        }

        private static void RejectsInvalidObservationDetail()
        {
            var ledger = new NativeLifecycleDeliveryLedger();
            ExpectArgumentException(() => ledger.Record(
                NativeLifecycleBoundary.GameModeStarted,
                "test",
                RelationshipState.Mounted,
                RelationshipState.Mounted,
                null,
                false,
                true,
                null,
                " "), "Whitespace observation detail was accepted.");
            ExpectArgumentException(() => ledger.Record(
                NativeLifecycleBoundary.GameModeStarted,
                "test",
                RelationshipState.Mounted,
                RelationshipState.Mounted,
                null,
                false,
                true,
                null,
                new string('x', 8193)), "Unbounded observation detail was accepted.");
        }

        private static void ExpectArgumentException(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void RetainsBoundedRecentWindow()
        {
            var ledger = new NativeLifecycleDeliveryLedger();
            for (var index = 0; index < 300; index++)
            {
                ledger.Record(
                    NativeLifecycleBoundary.CombatEnded,
                    "test " + index,
                    RelationshipState.Unmounted,
                    RelationshipState.Unmounted,
                    null,
                    false,
                    true);
            }

            var records = ledger.Snapshot();
            TestRunner.Equal(256, records.Count, "Native lifecycle ledger retention is unbounded or incomplete.");
            TestRunner.Equal(45L, records[0].Sequence, "Ledger did not retain the newest bounded window.");
            TestRunner.Equal(300L, records[records.Count - 1].Sequence, "Ledger lost the newest native event.");
        }
    }
}
