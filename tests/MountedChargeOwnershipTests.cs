using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedChargeOwnershipTests
    {
        // The native boundary is represented by independently owned slots, process and lease
        // mutations. Exercise the real lifetime/compensation code; resource state is read-only.
        private sealed class NativeOwners
        {
            internal readonly Dictionary<string, bool> Resident = new Dictionary<string, bool>();
            internal readonly object ExactPair = new object();
            internal object CurrentPair;
            internal string Fault;
            internal bool LeaveResidue;
            internal bool AgentAvailable = true;
            internal readonly int Standard = 6, Move = 3, Preparations = 1;
            internal MountedChargeOwnership Owner;

            internal NativeOwners()
            {
                CurrentPair = ExactPair;
                var steps = new List<MountedChargeCompensationStep>();
                var facts = new List<MountedChargePostcondition>();
                foreach (var name in new[] { "rider-interrupt", "rider-remove", "scheduler", "carrier-interrupt",
                    "mount-remove", "buff", "mount-charging", "speed", "rider-charging", "forced-path", "shell", "process" })
                {
                    var key = name;
                    Resident.Add(key, true);
                    steps.Add(new MountedChargeCompensationStep(key, () =>
                    {
                        TestRunner.True(Owner.CleanupRequested, "Late delivery was possible during interruption.");
                        if (!Resident[key]) return;
                        if (key == Fault)
                        {
                            if (LeaveResidue) return;
                            throw new InvalidOperationException(key);
                        }
                        if (!AgentAvailable && (key == "mount-charging" || key == "speed" || key == "forced-path")) return;
                        Resident[key] = false;
                    }));
                    facts.Add(new MountedChargePostcondition(key + "-released", () => !Resident[key]));
                }
                Owner = new MountedChargeOwnership(this, steps, facts);
            }

            internal bool Save() => Owner.TryDrain("save-before-enumeration");
            internal void VerifyDebt(string key)
            {
                TestRunner.True(ReferenceEquals(Owner.Identity, this), "Lost exact native owner.");
                TestRunner.True(!Owner.Drained && Owner.CleanupRequested && Resident[key], "Debt was discarded.");
                TestRunner.Equal(6, Standard, "Standard refunded.");
                TestRunner.Equal(3, Move, "Move refunded.");
                TestRunner.Equal(1, Preparations, "Preparation replayed.");
            }
        }

        internal static void Register(TestRunner runner)
        {
            foreach (var fault in new[] { "rider-interrupt", "rider-remove", "scheduler", "carrier-interrupt", "mount-remove",
                "buff", "mount-charging", "speed", "rider-charging", "forced-path", "shell", "process" })
            {
                var key = fault;
                foreach (var residue in new[] { false, true })
                {
                    var silent = residue;
                    runner.Run("charge owner retains " + key + (silent ? " residue" : " exception") + " until retry", () =>
                    {
                        var native = new NativeOwners { Fault = key, LeaveResidue = silent };
                        var captures = 0;
                        if (native.Save()) captures++;
                        native.VerifyDebt(key);
                        TestRunner.Equal(0, captures, "Half-owned snapshot captured.");
                        foreach (var item in native.Resident)
                            if (item.Key != key) TestRunner.True(!item.Value, "Independent cleanup was skipped: " + item.Key);
                        TestRunner.True(!native.Owner.TryDrain("dispose"), "Teardown discarded unresolved ownership.");
                        native.CurrentPair = new object();
                        native.Fault = null;
                        if (native.Save()) captures++;
                        TestRunner.Equal(1, captures, "A settled retry did not permit exactly one snapshot.");
                        TestRunner.True(native.Owner.Drained && ReferenceEquals(native.Owner.Identity, native), "Retry used replacement relationship.");
                        var attempts = native.Owner.AttemptCount;
                        TestRunner.True(native.Owner.TryDrain("repeat"), "Settled cleanup was not idempotent.");
                        TestRunner.Equal(attempts, native.Owner.AttemptCount, "Settled cleanup replayed native steps.");
                    });
                }
            }
            runner.Run("unavailable mount agent retains three independent mutations", () =>
            {
                var native = new NativeOwners { AgentAvailable = false };
                TestRunner.True(!native.Save(), "Unavailable native agent was treated as restored.");
                native.VerifyDebt("speed");
                TestRunner.True(!native.Resident["buff"] && !native.Resident["rider-charging"], "Independent rider cleanup skipped.");
                native.AgentAvailable = true;
                TestRunner.True(native.Save(), "Exact restored agent could not discharge debt.");
            });
            runner.Run("reentrant charge cleanup cannot release its active owner", () =>
            {
                MountedChargeOwnership owner = null;
                var finished = false;
                owner = new MountedChargeOwnership(new object(), new[] { new MountedChargeCompensationStep("native-callback", () =>
                {
                    TestRunner.True(!owner.TryDrain("reentrant"), "Reentrant cleanup reported success.");
                    finished = true;
                }) }, new[] { new MountedChargePostcondition("terminal", () => finished) });
                TestRunner.True(owner.TryDrain("cancel"), "Outer cleanup did not drain.");
                TestRunner.Equal(1, owner.AttemptCount, "Reentrant cleanup duplicated native interruption.");
            });
        }
    }
}
