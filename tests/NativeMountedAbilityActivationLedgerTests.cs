using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class NativeMountedAbilityActivationLedgerTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("native ability activation ledger assigns stable identities", AssignsStableIdentities);
            runner.Run("native ability activation ledger retains a bounded tail", RetainsBoundedTail);
        }

        private static void AssignsStableIdentities()
        {
            var ledger = new NativeMountedAbilityActivationLedger();
            var first = ledger.BeginActivation();
            var second = ledger.BeginActivation();
            TestRunner.Equal(1L, first, "First activation identity changed.");
            TestRunner.Equal(2L, second, "Activation identities were not monotonic.");

            ledger.Record(NewRecord(first, NativeMountedAbilityActivationPhase.DispatchStarted));
            ledger.Record(NewRecord(first, NativeMountedAbilityActivationPhase.DispatchCompleted));
            var records = ledger.Snapshot();
            TestRunner.Equal(2, records.Count, "Activation phases were not retained.");
            TestRunner.Equal(1L, records[0].Sequence, "Ledger sequence did not begin at one.");
            TestRunner.Equal(2L, records[1].Sequence, "Ledger sequence was not monotonic.");
            TestRunner.Equal(first, records[1].ActivationId, "Activation correlation identity changed between phases.");
        }

        private static void RetainsBoundedTail()
        {
            var ledger = new NativeMountedAbilityActivationLedger();
            var activation = ledger.BeginActivation();
            for (var index = 0; index < 520; index++)
            {
                ledger.Record(NewRecord(activation, NativeMountedAbilityActivationPhase.CommandTerminal));
            }

            var records = ledger.Snapshot();
            TestRunner.Equal(512, records.Count, "Activation telemetry exceeded its bounded retention.");
            TestRunner.Equal(9L, records[0].Sequence, "Activation telemetry did not retain the newest bounded tail.");
            TestRunner.Equal(520L, records[records.Count - 1].Sequence, "Activation telemetry lost its newest record.");
        }

        private static NativeMountedAbilityActivationRecord NewRecord(
            long activationId,
            NativeMountedAbilityActivationPhase phase)
        {
            return new NativeMountedAbilityActivationRecord
            {
                ActivationId = activationId,
                Phase = phase,
                Kind = NativeMountedControlKind.RiderPrimary,
                RelationshipStateAtStart = RelationshipState.Mounted,
                RelationshipStateObserved = RelationshipState.Mounted
            };
        }
    }
}
