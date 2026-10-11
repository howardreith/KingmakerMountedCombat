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
            runner.Run("native mode AI reset reasserts exact idle scope and preserves original restoration", ReassertsNativeReset);
            runner.Run("fixture AI lease acquired before Mount keeps the mount isolated across a forced lifecycle Dismount", LeaseBeforeMountSurvivesForcedDismount);
            runner.Run("fixture AI lease acquired after Mount captures the mounted-disabled state and a forced Dismount re-enables the mount", LeaseAfterMountLosesIsolationAtForcedDismount);
        }

        // Only the documented relationship contract for the mount's raw AI is modelled:
        // Mount captures the raw value and disables it; every lifecycle Dismount restores
        // the captured value (KingmakerMountedPairRuntime movement authority; the native
        // IsAIEnabled setter writes only the raw field). The real runtime needs Unity.
        private sealed class FakeMountedRelationship
        {
            private bool captured;

            public bool Mounted { get; private set; }

            public void Mount(FakeUnit mount) { captured = mount.RawAi; mount.SetRawAi(false); Mounted = true; }

            public void ForcedDismount(FakeUnit mount) { mount.SetRawAi(captured); Mounted = false; }
        }

        private static void LeaseBeforeMountSurvivesForcedDismount()
        {
            var mount = new FakeUnit("mount", true, true);
            var relationship = new FakeMountedRelationship();
            var lease = CreateLease();
            lease.Acquire(new[] { mount });
            TestRunner.True(lease.States[0].RawAiBefore && lease.States[0].EffectiveAiBefore,
                "Lease acquired before Mount must capture the mount's true enabled AI.");
            relationship.Mount(mount);
            lease.ValidateActive(new[] { mount });
            relationship.ForcedDismount(mount);
            TestRunner.True(!mount.RawAi && !mount.EffectiveAi,
                "Forced lifecycle Dismount re-enabled the isolated mount AI.");
            lease.ValidateActive(new[] { mount });
            TestRunner.True(lease.LastActiveValidationPassed, "Fixture isolation did not survive the forced Dismount.");
            lease.Restore(new[] { mount });
            TestRunner.True(mount.RawAi && mount.EffectiveAi && lease.LastRestoreVerified && !relationship.Mounted,
                "Final fixture restoration did not return the true original mount AI.");
        }

        private static void LeaseAfterMountLosesIsolationAtForcedDismount()
        {
            // The preview.199 ordering: the lease captured the already-disabled AI, so the
            // relationship's own restoration re-enabled ordinary mount behavior mid-fixture.
            var mount = new FakeUnit("mount", true, true);
            var relationship = new FakeMountedRelationship();
            relationship.Mount(mount);
            var lease = CreateLease();
            lease.Acquire(new[] { mount });
            TestRunner.True(!lease.States[0].RawAiBefore && !lease.States[0].EffectiveAiBefore,
                "Lease acquired after Mount should capture the mounted-disabled state.");
            relationship.ForcedDismount(mount);
            TestRunner.True(mount.RawAi && mount.EffectiveAi,
                "Expected the preview.199 defect: a forced Dismount re-enables the mount behind the lease.");
            var drifted = false;
            try { lease.ValidateActive(new[] { mount }); }
            catch (InvalidOperationException) { drifted = true; }
            TestRunner.True(drifted, "Lease validation must detect the re-enabled mount AI as drift.");
            mount.SetRawAi(false);
            lease.Restore(new[] { mount });
            TestRunner.True(!mount.RawAi && lease.LastRestoreVerified,
                "A post-Mount lease can only restore the mounted-disabled state, never the true original.");
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

        private static void ReassertsNativeReset()
        {
            var first = new FakeUnit("first", true, true);
            var second = new FakeUnit("second", false, false);
            var lease = CreateLease();
            lease.Acquire(new[] { first, second });
            first.SetRawAi(true); second.SetRawAi(true); // Native mode shutdown.
            lease.ReassertAfterNativeReset(new[] { first, second });
            TestRunner.True(lease.LastActiveValidationPassed && !first.EffectiveAi && !second.EffectiveAi,
                "Native shutdown lost exact fixture isolation.");
            var writes = first.SetCount + second.SetCount;
            second.CommandsEmpty = false;
            ExpectThrows(() => lease.ReassertAfterNativeReset(new[] { first, second }),
                "Reassertion accepted an active native command.");
            TestRunner.Equal(writes, first.SetCount + second.SetCount, "Unsafe reassertion partially mutated the scope.");
            second.CommandsEmpty = true;
            ExpectThrows(() => lease.ReassertAfterNativeReset(new[] { first, new FakeUnit("second", true, true) }),
                "Reassertion accepted replacement actor identity.");
            lease.Restore(new[] { first, second });
            TestRunner.True(first.RawAi && !second.RawAi && lease.LastRestoreVerified,
                "Reassertion replaced the original mixed-state restoration snapshot.");
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
