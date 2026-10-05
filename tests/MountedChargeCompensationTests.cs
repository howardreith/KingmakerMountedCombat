using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Chunk 6B increment 6B.2: a charge that failed after entering the rider's queue must resolve every
    // native owner, and compensation must not itself be the thing that fails.
    //
    // The property that matters is the opposite of the application transaction's: there the first failure
    // stops the walk, here every step runs regardless, because each resolves a different owner - the
    // scheduler registration, the queued command, its slot, the charge lease - and skipping one strands it.
    internal static class MountedChargeCompensationTests
    {
        private static string Join(IEnumerable<string> values)
        {
            return string.Join("|", new List<string>(values).ToArray());
        }

        // Compensation is complete only when the native owners are observably gone, so every test that
        // asks about Complete has to confirm postconditions the way the controller does.
        private static void Confirm(MountedChargeCompensation compensation, params bool[] satisfied)
        {
            var postconditions = new List<MountedChargePostcondition>();
            for (var index = 0; index < satisfied.Length; index++)
            {
                var value = satisfied[index];
                postconditions.Add(new MountedChargePostcondition("owner-" + index, () => value));
            }

            compensation.ConfirmPostconditions(postconditions);
        }

        private static MountedChargeCompensation Build(List<string> ran, params string[] faulting)
        {
            var faults = new List<string>(faulting);
            var names = new[] { "abandon-scheduler", "interrupt-command", "dequeue-command", "restore-lease" };
            var steps = new List<MountedChargeCompensationStep>();
            foreach (var name in names)
            {
                var captured = name;
                steps.Add(new MountedChargeCompensationStep(captured, () =>
                {
                    if (faults.Contains(captured))
                    {
                        throw new InvalidOperationException("owner " + captured + " could not be resolved");
                    }

                    ran.Add(captured);
                }));
            }

            return new MountedChargeCompensation("injected failure after AddToQueueFirst", steps);
        }

        internal static void Register(TestRunner runner)
        {
            runner.Run("charge compensation resolves every owner in order", () =>
            {
                var ran = new List<string>();
                var compensation = Build(ran);
                compensation.Run();
                TestRunner.True(compensation.Ran, "Compensation did not report that it ran.");
                TestRunner.True(!compensation.Complete,
                    "Compensation was complete before its postconditions were confirmed.");
                Confirm(compensation, true, true, true);
                TestRunner.True(compensation.Complete, "Clean compensation was not reported complete.");
                TestRunner.True(Join(ran) == "abandon-scheduler|interrupt-command|dequeue-command|restore-lease",
                    "The owners were resolved in the wrong order: " + Join(ran));
                TestRunner.True(compensation.Failures.Count == 0,
                    "Clean compensation recorded failures: " + Join(compensation.Failures));
            });

            runner.Run("one owner that cannot be resolved never strands the others", () =>
            {
                // The scheduler step is first, so a failure there is the worst case for the rest.
                var ran = new List<string>();
                var compensation = Build(ran, "abandon-scheduler");
                compensation.Run();
                Confirm(compensation, true, true, true);
                TestRunner.True(Join(ran) == "interrupt-command|dequeue-command|restore-lease",
                    "A failing first step stranded the others: " + Join(ran));
                TestRunner.True(!compensation.Complete, "Compensation with a failure reported complete.");
                TestRunner.True(Join(compensation.Failures) == "abandon-scheduler:InvalidOperationException",
                    "The failure was not recorded exactly: " + Join(compensation.Failures));
            });

            runner.Run("compensation never throws, however many owners fail", () =>
            {
                var ran = new List<string>();
                var compensation = Build(ran, "abandon-scheduler", "interrupt-command", "dequeue-command", "restore-lease");
                compensation.Run();
                Confirm(compensation, true);
                TestRunner.True(ran.Count == 0, "A fully failing compensation still resolved something.");
                TestRunner.True(compensation.Failures.Count == 4,
                    "Not every failure was recorded: " + Join(compensation.Failures));
                TestRunner.True(!compensation.Complete, "A fully failing compensation reported complete.");
                TestRunner.True(compensation.Ran, "A fully failing compensation did not report that it ran.");
            });

            runner.Run("the lease is restored even when the queued command cannot be dequeued", () =>
            {
                // The lease is last precisely so that a stubborn command cannot keep the charging flag,
                // speed override and forced path alive on the mount.
                var ran = new List<string>();
                var compensation = Build(ran, "dequeue-command");
                compensation.Run();
                TestRunner.True(ran.Contains("restore-lease"),
                    "A failing dequeue stranded the charge lease: " + Join(ran));
                TestRunner.True(Join(compensation.Failures) == "dequeue-command:InvalidOperationException",
                    "The dequeue failure was not recorded exactly: " + Join(compensation.Failures));
            });

            runner.Run("charge compensation runs exactly once and names its reason", () =>
            {
                var ran = new List<string>();
                var compensation = Build(ran);
                compensation.Run();
                var threw = false;
                try
                {
                    compensation.Run();
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }

                TestRunner.True(threw, "Compensation ran twice.");
                TestRunner.True(ran.Count == 4, "A second run resolved owners again: " + Join(ran));
                TestRunner.True(compensation.Reason == "injected failure after AddToQueueFirst",
                    "The compensation reason was not carried: " + compensation.Reason);
            });

            runner.Run("an unmet native postcondition keeps compensation incomplete", () =>
            {
                // Every step can run cleanly and an owner can still be there. This is the case the
                // controller must never read as "safe to release my last reference".
                var ran = new List<string>();
                var compensation = Build(ran);
                compensation.Run();
                TestRunner.True(compensation.Failures.Count == 0, "A clean run recorded a failure.");
                Confirm(compensation, true, false, true);
                TestRunner.True(!compensation.Complete,
                    "Compensation was complete with an owner still present.");
                TestRunner.True(Join(compensation.UnmetPostconditions) == "owner-1",
                    "The unmet postcondition was not named exactly: " + Join(compensation.UnmetPostconditions));
            });

            runner.Run("a postcondition that cannot be evaluated is unmet, not met", () =>
            {
                var ran = new List<string>();
                var compensation = Build(ran);
                compensation.Run();
                compensation.ConfirmPostconditions(new[]
                {
                    new MountedChargePostcondition("queue-released", () => true),
                    new MountedChargePostcondition("container-released",
                        () => throw new InvalidOperationException("the container cannot be read"))
                });
                TestRunner.True(!compensation.Complete,
                    "An unanswerable postcondition was treated as a clean answer.");
                TestRunner.True(Join(compensation.UnmetPostconditions) == "container-released:InvalidOperationException",
                    "The unanswerable postcondition was not recorded: " + Join(compensation.UnmetPostconditions));
            });

            runner.Run("a retry can confirm the postconditions a first attempt could not", () =>
            {
                var ran = new List<string>();
                var compensation = Build(ran);
                compensation.Run();
                Confirm(compensation, false);
                TestRunner.True(!compensation.Complete, "The first confirmation reported complete.");
                Confirm(compensation, true);
                TestRunner.True(compensation.Complete,
                    "A later confirmation did not clear the unmet postcondition: " + compensation.Describe());
            });

            runner.Run("postconditions cannot be confirmed before compensation has run", () =>
            {
                var compensation = Build(new List<string>());
                var threw = false;
                try
                {
                    Confirm(compensation, true);
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }

                TestRunner.True(threw, "Postconditions were confirmed before compensation ran.");
            });

            runner.Run("charge compensation requires a reason and at least one step", () =>
            {
                var unnamed = false;
                try
                {
                    new MountedChargeCompensation(string.Empty,
                        new[] { new MountedChargeCompensationStep("x", () => { }) });
                }
                catch (ArgumentException)
                {
                    unnamed = true;
                }

                TestRunner.True(unnamed, "Compensation without a reason was accepted.");

                var empty = false;
                try
                {
                    new MountedChargeCompensation("reason", new MountedChargeCompensationStep[0]);
                }
                catch (ArgumentException)
                {
                    empty = true;
                }

                TestRunner.True(empty, "Compensation with no steps was accepted.");
            });
        }
    }
}
