using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class ScopedDiagnosticAiLeaseTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("diagnostic AI lease suppresses and restores only the exact empty-command set", SuppressesAndRestoresExactSet);
            runner.Run("diagnostic AI lease rejects ambiguous or active-command candidates before mutation", RejectsUnsafeCandidates);
            runner.Run("diagnostic AI lease detects membership command and AI drift", DetectsActiveDrift);
            runner.Run("diagnostic AI lease restoration verifies original raw and effective state", RestorationVerifiesOriginalState);
        }

        private static void SuppressesAndRestoresExactSet()
        {
            var first = new FakeUnit("first", true, true);
            var second = new FakeUnit("second", false, false);
            var lease = CreateLease();

            lease.Acquire(new[] { first, second });
            TestRunner.True(lease.IsAcquired && lease.LastActiveValidationPassed,
                "Exact diagnostic AI lease did not acquire.");
            TestRunner.True(!first.RawAi && !first.EffectiveAi && !second.RawAi && !second.EffectiveAi,
                "Acquisition did not suppress the exact candidate set.");
            TestRunner.True(first.SetCount == 1 && second.SetCount == 1,
                "Acquisition mutated a candidate more than once.");

            lease.ValidateActive(new[] { first, second });
            lease.Restore(new[] { first, second });
            TestRunner.True(!lease.IsAcquired && lease.LastRestoreVerified,
                "Exact diagnostic AI state was not restored and released.");
            TestRunner.True(first.RawAi && first.EffectiveAi && !second.RawAi && !second.EffectiveAi,
                "Restoration did not reproduce the mixed original raw/effective state.");
            TestRunner.True(first.CommandsEmpty && second.CommandsEmpty,
                "The diagnostic AI lease changed a command queue.");
        }

        private static void RejectsUnsafeCandidates()
        {
            var active = new FakeUnit("active", true, true) { CommandsEmpty = false };
            var lease = CreateLease();
            ExpectThrows(() => lease.Acquire(new[] { active }),
                "Diagnostic AI lease accepted a non-empty command queue.");
            TestRunner.True(active.RawAi && active.SetCount == 0,
                "Rejected active-command candidate was mutated.");

            var rollback = new FakeUnit("rollback", true, true) { IgnoreNextSet = true };
            var rollbackLease = CreateLease();
            ExpectThrows(() => rollbackLease.Acquire(new[] { rollback }),
                "Diagnostic AI lease accepted incomplete acquisition suppression.");
            TestRunner.True(!rollbackLease.IsAcquired && rollbackLease.LastRestoreVerified &&
                    rollback.RawAi && rollback.EffectiveAi && rollback.CommandsEmpty,
                "Failed diagnostic AI acquisition did not roll back to exact original state.");

            var first = new FakeUnit("duplicate", true, true);
            var second = new FakeUnit("duplicate", true, true);
            ExpectThrows(() => CreateLease().Acquire(new[] { first, second }),
                "Diagnostic AI lease accepted duplicate identity.");
            first.ContextExact = false;
            ExpectThrows(() => CreateLease().Acquire(new[] { first }),
                "Diagnostic AI lease accepted an inexact candidate context.");
        }

        private static void DetectsActiveDrift()
        {
            var first = new FakeUnit("first", true, true);
            var second = new FakeUnit("second", true, true);
            var replacement = new FakeUnit("replacement", true, true);
            var lease = CreateLease();
            lease.Acquire(new[] { first, second });

            ExpectThrows(() => lease.ValidateActive(new[] { first, replacement }),
                "Diagnostic AI lease accepted membership replacement.");
            second.CommandsEmpty = false;
            ExpectThrows(() => lease.ValidateActive(new[] { first, second }),
                "Diagnostic AI lease accepted a later command.");
            second.CommandsEmpty = true;
            second.RawAi = true;
            second.EffectiveAi = true;
            ExpectThrows(() => lease.ValidateActive(new[] { first, second }),
                "Diagnostic AI lease accepted raw/effective AI drift.");

            second.RawAi = false;
            second.EffectiveAi = false;
            lease.Restore(new[] { first, second });
        }

        private static void RestorationVerifiesOriginalState()
        {
            var unit = new FakeUnit("unit", true, true);
            var lease = CreateLease();
            lease.Acquire(new[] { unit });
            unit.IgnoreNextSet = true;
            ExpectThrows(() => lease.Restore(new[] { unit }),
                "Diagnostic AI restoration accepted a setter that retained disabled raw state.");
            TestRunner.True(lease.IsAcquired && !lease.LastRestoreVerified,
                "Failed restoration discarded its retryable lease state.");

            lease.Restore(new[] { unit });
            TestRunner.True(unit.RawAi && unit.EffectiveAi && lease.LastRestoreVerified,
                "Diagnostic AI restoration retry did not reproduce exact original state.");
        }

        private static ScopedDiagnosticAiLease<FakeUnit> CreateLease()
        {
            return new ScopedDiagnosticAiLease<FakeUnit>(
                unit => unit.Id,
                unit => unit.ContextExact,
                unit => unit.CommandsEmpty,
                unit => unit.RawAi,
                unit => unit.EffectiveAi,
                (unit, value) => unit.SetRawAi(value));
        }

        private static void ExpectThrows(Action action, string message)
        {
            var threw = false;
            try { action(); }
            catch (InvalidOperationException) { threw = true; }
            catch (AggregateException) { threw = true; }
            TestRunner.True(threw, message);
        }

        private sealed class FakeUnit
        {
            public FakeUnit(string id, bool rawAi, bool effectiveAi)
            {
                Id = id;
                RawAi = rawAi;
                EffectiveAi = effectiveAi;
            }

            public string Id { get; }

            public bool ContextExact { get; set; } = true;

            public bool CommandsEmpty { get; set; } = true;

            public bool RawAi { get; set; }

            public bool EffectiveAi { get; set; }

            public int SetCount { get; private set; }

            public bool IgnoreNextSet { get; set; }

            public void SetRawAi(bool value)
            {
                SetCount++;
                if (IgnoreNextSet)
                {
                    IgnoreNextSet = false;
                    return;
                }
                RawAi = value;
                EffectiveAi = value;
            }
        }
    }
}
