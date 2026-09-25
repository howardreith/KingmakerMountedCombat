using System;
using System.Threading.Tasks;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // The unload/disposal decision about an owned archive worker. Real Tasks,
    // real settlement, no game types. The rule under test: a bounded wait that
    // expires is not evidence that cleanup is safe, and ownership that could not
    // be established is not "no worker" -- both refuse.
    internal static class OwnedWorkerTeardownPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("teardown with nothing owned is clear", NothingOwned);
            runner.Run("teardown with a verified absent worker settles without waiting", VerifiedNone);
            runner.Run("teardown with unknown ownership refuses even with no task", UnknownOwnership);
            runner.Run("teardown with unknown ownership refuses even when the task has finished", UnknownOwnershipFinishedTask);
            runner.Run("teardown with a live worker that settles in time proceeds", LiveSettles);
            runner.Run("teardown with a live worker past the bounded wait refuses", LiveTimesOut);
            runner.Run("teardown with a faulted worker settles because it is finished", Faulted);
            runner.Run("teardown with a canceled worker settles because it is finished", Canceled);
            runner.Run("teardown with an already completed worker never waits", CompletedNoWait);
            runner.Run("repeated teardown requests give the same verdict without a second wait", Repeated);
        }

        private static Func<Task, int, bool> Wait(Func<Task, int, bool> inner, Action onWait) =>
            (task, ms) => { onWait(); return inner(task, ms); };

        private static bool RealWait(Task task, int ms)
        {
            try { return task.Wait(ms); }
            catch (Exception) { return task.IsCompleted; }
        }

        private static void NothingOwned()
        {
            var waited = false;
            var verdict = OwnedWorkerTeardownPolicy.Decide(false, false, null, Wait(RealWait, () => waited = true), 100);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Clear, verdict, "No scope should be clear.");
            TestRunner.True(!waited, "Nothing to wait for.");
        }

        private static void VerifiedNone()
        {
            var waited = false;
            var verdict = OwnedWorkerTeardownPolicy.Decide(true, true, null, Wait(RealWait, () => waited = true), 100);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Settled, verdict, "A verified absent worker settles.");
            TestRunner.True(!waited, "A verified absent worker must not wait.");
        }

        private static void UnknownOwnership()
        {
            var waited = false;
            var verdict = OwnedWorkerTeardownPolicy.Decide(true, false, null, Wait(RealWait, () => waited = true), 100);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Refused, verdict, "Unknown ownership must refuse.");
            TestRunner.True(!waited, "There is nothing to wait on when ownership is unknown.");
        }

        private static void UnknownOwnershipFinishedTask()
        {
            var finished = new TaskCompletionSource<bool>(); finished.SetResult(true);
            var verdict = OwnedWorkerTeardownPolicy.Decide(true, false, finished.Task, RealWait, 100);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Refused, verdict,
                "An unestablished answer refuses even when the observed task has finished.");
        }

        private static void LiveSettles()
        {
            var live = new TaskCompletionSource<bool>();
            var waits = 0;
            Func<Task, int, bool> wait = (task, ms) => { waits++; live.SetResult(true); return RealWait(task, ms); };
            var verdict = OwnedWorkerTeardownPolicy.Decide(true, true, live.Task, wait, 500);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Settled, verdict, "A worker that settles in time permits teardown.");
            TestRunner.Equal(1, waits, "Exactly one bounded wait.");
        }

        private static void LiveTimesOut()
        {
            var live = new TaskCompletionSource<bool>();
            var verdict = OwnedWorkerTeardownPolicy.Decide(true, true, live.Task, RealWait, 50);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Refused, verdict, "An expired wait refuses; it is not evidence of safety.");
            TestRunner.True(!live.Task.IsCompleted, "The worker is still live after the refusal.");
            live.SetResult(true);
        }

        private static void Faulted()
        {
            var broken = new TaskCompletionSource<bool>();
            broken.SetException(new InvalidOperationException("serializer fault"));
            var waited = false;
            var verdict = OwnedWorkerTeardownPolicy.Decide(true, true, broken.Task, Wait(RealWait, () => waited = true), 100);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Settled, verdict, "A faulted worker is finished.");
            TestRunner.True(!waited, "A finished worker never waits.");
            GC.KeepAlive(broken.Task.Exception);
        }

        private static void Canceled()
        {
            var stopped = new TaskCompletionSource<bool>(); stopped.SetCanceled();
            var verdict = OwnedWorkerTeardownPolicy.Decide(true, true, stopped.Task, RealWait, 100);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Settled, verdict, "A canceled worker is finished.");
        }

        private static void CompletedNoWait()
        {
            var done = new TaskCompletionSource<bool>(); done.SetResult(true);
            var waited = false;
            var verdict = OwnedWorkerTeardownPolicy.Decide(true, true, done.Task, Wait(RealWait, () => waited = true), 100);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Settled, verdict, "A completed worker settles.");
            TestRunner.True(!waited, "A completed worker never waits.");
        }

        private static void Repeated()
        {
            var live = new TaskCompletionSource<bool>();
            var waits = 0;
            Func<Task, int, bool> wait = (task, ms) => { waits++; return RealWait(task, ms); };
            var first = OwnedWorkerTeardownPolicy.Decide(true, true, live.Task, wait, 20);
            var second = OwnedWorkerTeardownPolicy.Decide(true, true, live.Task, wait, 20);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Refused, first, "First request refuses while live.");
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Refused, second, "Second request refuses while still live.");
            TestRunner.Equal(2, waits, "Each request waits once, bounded.");
            live.SetResult(true);
            var third = OwnedWorkerTeardownPolicy.Decide(true, true, live.Task, wait, 20);
            TestRunner.Equal(OwnedWorkerTeardownVerdict.Settled, third, "Once settled, teardown may proceed.");
            TestRunner.Equal(2, waits, "A completed worker is not waited on again.");
        }
    }
}
