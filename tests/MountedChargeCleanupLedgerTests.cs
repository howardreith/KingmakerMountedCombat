using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Chunk 6B increment 6B.2: lease cleanup must be retry-safe and postcondition-based.
    //
    // The first form of this work set Restored at the top of Restore and cleared each ownership flag whether
    // or not the native undo had worked, so a Buff.Remove that threw or a mount agent that had gone away
    // produced a lease that reported itself restored while the mutation was still live. These fault-inject
    // every failure the owner named and assert the opposite property: ownership is retained, the step stays
    // unresolved, the ledger is not complete, and a later attempt can discharge the debt.
    internal static class MountedChargeCleanupLedgerTests
    {
        // A stand-in for the lease's owned native state.
        private sealed class Mutations
        {
            internal bool Buff = true;
            internal bool Charging = true;
            internal bool Speed = true;
            internal bool RiderState = true;
            internal bool ForcedPath = true;

            internal string Faulting = string.Empty;
            internal string Unavailable = string.Empty;

            private bool Undo(string name, Action release)
            {
                if (Faulting.Contains(name))
                {
                    throw new InvalidOperationException("native " + name + " undo threw");
                }

                if (Unavailable.Contains(name))
                {
                    // The agent or descriptor is gone: ownership is retained, because reporting the mutation
                    // returned would be a guess about state we cannot see.
                    return false;
                }

                release();
                return true;
            }

            internal MountedChargeCleanupLedger Ledger()
            {
                return new MountedChargeCleanupLedger(new[]
                {
                    new MountedChargeCleanupStep("forced-path", () => ForcedPath,
                        () => Undo("forced-path", () => ForcedPath = false)),
                    new MountedChargeCleanupStep("rider-charging-state", () => RiderState,
                        () => Undo("rider-charging-state", () => RiderState = false)),
                    new MountedChargeCleanupStep("mount-speed-override", () => Speed,
                        () => Undo("mount-speed-override", () => Speed = false)),
                    new MountedChargeCleanupStep("mount-charging", () => Charging,
                        () => Undo("mount-charging", () => Charging = false)),
                    new MountedChargeCleanupStep("charge-buff", () => Buff,
                        () => Undo("charge-buff", () => Buff = false))
                });
            }
        }

        private static string Join(IEnumerable<string> values)
        {
            return string.Join("|", new List<string>(values).ToArray());
        }

        private static void RetainsOwnership(string faulting, string unavailable, string step, string what)
        {
            var mutations = new Mutations { Faulting = faulting, Unavailable = unavailable };
            var ledger = mutations.Ledger();
            ledger.Attempt();
            TestRunner.True(ledger.Attempted, "The cleanup attempt was not recorded for " + what + ".");
            TestRunner.True(!ledger.Complete, "Cleanup reported complete with " + what + ".");
            TestRunner.True(new List<string>(ledger.Unresolved).Contains(step),
                "The debt for " + what + " was not retained: " + Join(ledger.Unresolved));
            TestRunner.True(Join(ledger.Failures).Contains(step),
                "The failure for " + what + " was not recorded: " + Join(ledger.Failures));
        }

        internal static void Register(TestRunner runner)
        {
            runner.Run("cleanup that succeeds resolves every mutation and reports complete", () =>
            {
                var mutations = new Mutations();
                var ledger = mutations.Ledger();
                ledger.Attempt();
                TestRunner.True(ledger.Complete, "Clean cleanup was not complete: " + ledger.Describe());
                TestRunner.True(ledger.Unresolved.Count == 0, "Clean cleanup left debt: " + Join(ledger.Unresolved));
                TestRunner.True(Join(ledger.Resolved) ==
                    "forced-path|rider-charging-state|mount-speed-override|mount-charging|charge-buff",
                    "The cleanup order differs: " + Join(ledger.Resolved));
                TestRunner.True(!mutations.Buff && !mutations.Charging && !mutations.Speed &&
                    !mutations.RiderState && !mutations.ForcedPath,
                    "A mutation survived a complete cleanup.");
            });

            runner.Run("Buff.Remove throwing retains the buff as cleanup debt", () =>
            {
                RetainsOwnership("charge-buff", string.Empty, "charge-buff", "a throwing Buff.Remove");
            });

            runner.Run("StopMoving throwing retains the forced path as cleanup debt", () =>
            {
                RetainsOwnership("forced-path", string.Empty, "forced-path", "a throwing StopMoving");
            });

            runner.Run("an unavailable mount agent retains the charging and speed debt", () =>
            {
                RetainsOwnership(string.Empty, "mount-charging", "mount-charging", "an unavailable mount agent");
                RetainsOwnership(string.Empty, "mount-speed-override", "mount-speed-override", "an unavailable mount agent");
            });

            runner.Run("an unavailable rider descriptor retains the rider state debt", () =>
            {
                RetainsOwnership(string.Empty, "rider-charging-state", "rider-charging-state",
                    "an unavailable rider descriptor");
            });

            runner.Run("a throwing charging or speed restore retains its own debt only", () =>
            {
                RetainsOwnership("mount-charging", string.Empty, "mount-charging", "a throwing charging restore");
                RetainsOwnership("mount-speed-override", string.Empty, "mount-speed-override", "a throwing speed restore");

                // The others must still have been resolved: one fault may not strand the rest.
                var mutations = new Mutations { Faulting = "mount-charging" };
                var ledger = mutations.Ledger();
                ledger.Attempt();
                TestRunner.True(!mutations.Buff && !mutations.Speed && !mutations.RiderState && !mutations.ForcedPath,
                    "A single throwing restore stranded the other mutations.");
                TestRunner.True(mutations.Charging, "The throwing restore released its ownership anyway.");
            });

            runner.Run("a second cleanup attempt discharges the remaining debt", () =>
            {
                var mutations = new Mutations { Unavailable = "charge-buff" };
                var ledger = mutations.Ledger();
                ledger.Attempt();
                TestRunner.True(!ledger.Complete, "The first attempt reported complete with debt outstanding.");
                TestRunner.True(new List<string>(ledger.Unresolved).Contains("charge-buff"),
                    "The buff debt was not retained: " + Join(ledger.Unresolved));

                // The agent or fact comes back, and the retry discharges exactly what was left.
                mutations.Unavailable = string.Empty;
                ledger.Attempt();
                TestRunner.True(ledger.Complete, "The retry did not discharge the debt: " + ledger.Describe());
                TestRunner.True(ledger.Unresolved.Count == 0, "Debt survived the retry: " + Join(ledger.Unresolved));
                TestRunner.True(ledger.AttemptCount == 2, "The attempt count was " + ledger.AttemptCount + ".");
                TestRunner.True(!mutations.Buff, "The buff was still owned after a complete retry.");
            });

            runner.Run("an earlier failure that a retry discharged is not reported as outstanding", () =>
            {
                // Failures are from the last attempt only; otherwise debt could never be cleared.
                var mutations = new Mutations { Faulting = "mount-charging" };
                var ledger = mutations.Ledger();
                ledger.Attempt();
                TestRunner.True(ledger.Failures.Count == 1, "The first attempt did not record its failure.");
                mutations.Faulting = string.Empty;
                ledger.Attempt();
                TestRunner.True(ledger.Failures.Count == 0,
                    "A discharged failure was still reported: " + Join(ledger.Failures));
                TestRunner.True(ledger.Complete, "The retry did not complete: " + ledger.Describe());
            });

            runner.Run("an ownership probe that throws counts as outstanding debt", () =>
            {
                var ledger = new MountedChargeCleanupLedger(new[]
                {
                    new MountedChargeCleanupStep("unanswerable",
                        () => throw new InvalidOperationException("ownership cannot be read"),
                        () => true)
                });
                ledger.Attempt();
                TestRunner.True(!ledger.Complete, "An unanswerable ownership probe was treated as resolved.");
                TestRunner.True(new List<string>(ledger.Unresolved).Contains("unanswerable"),
                    "An unanswerable probe was not counted as debt: " + Join(ledger.Unresolved));
            });

            runner.Run("cleanup is not complete before it has been attempted", () =>
            {
                var mutations = new Mutations { Buff = false, Charging = false, Speed = false, RiderState = false, ForcedPath = false };
                var ledger = mutations.Ledger();
                TestRunner.True(!ledger.Complete, "Cleanup was complete before any attempt.");
                ledger.Attempt();
                TestRunner.True(ledger.Complete, "Cleanup with nothing owned was not complete after an attempt.");
            });

            runner.Run("cleanup needs at least one step and no null step", () =>
            {
                var empty = false;
                try
                {
                    new MountedChargeCleanupLedger(new MountedChargeCleanupStep[0]);
                }
                catch (ArgumentException)
                {
                    empty = true;
                }

                TestRunner.True(empty, "An empty cleanup ledger was accepted.");

                var unnamed = false;
                try
                {
                    new MountedChargeCleanupStep(string.Empty, () => false, () => true);
                }
                catch (ArgumentException)
                {
                    unnamed = true;
                }

                TestRunner.True(unnamed, "An unnamed cleanup step was accepted.");
            });
        }
    }
}
