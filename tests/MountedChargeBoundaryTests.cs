using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedChargeBoundaryTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("distinct destroyed actors retain retirement after charge drain", () =>
            {
                var queue = new MountedChargeBoundaryQueue(); var first = new object(); var second = new object(); var calls = new List<int>();
                queue.Enqueue(MountedChargeBoundaryKind.DestroyedActor, first, () => true, () => calls.Add(1));
                queue.Enqueue(MountedChargeBoundaryKind.DestroyedActor, second, () => true, () => calls.Add(2));
                TestRunner.True(!queue.TryDrain(() => false) && calls.Count == 0, "Retirement crossed charge/archive ownership.");
                TestRunner.True(queue.TryDrain(() => true), "Settled retirement remained pending.");
                TestRunner.Equal("1,2", string.Join(",", calls), "One exact actor retirement was discarded.");
            });
            runner.Run("native mode request waits for charge debt and invokes once", () =>
            {
                var queue = new MountedChargeBoundaryQueue();
                var cleared = false; var transitions = 0;
                var owner = new MountedChargeOwnership(new object(), new[] {
                    new MountedChargeCompensationStep("lease", () => { }) }, new[] {
                    new MountedChargePostcondition("lease-restored", () => cleared) });
                queue.Enqueue(MountedChargeBoundaryKind.Mode, new object(), () => true, () => transitions++);
                TestRunner.True(!queue.TryDrain(() => owner.TryDrain("mode")) && queue.Pending, "Unsettled native request was lost.");
                TestRunner.Equal(0, transitions, "Native controller changed before cleanup.");
                cleared = true;
                TestRunner.True(queue.TryDrain(() => owner.TryDrain("mode")) && !queue.Pending, "Drained request did not complete.");
                queue.TryDrain(() => true);
                TestRunner.Equal(1, transitions, "Native transition replayed.");
            });
            runner.Run("superseded mode request preserves distinct boundary ordering", () =>
            {
                var queue = new MountedChargeBoundaryQueue(); var calls = new List<int>();
                var controller = new object();
                queue.Enqueue(MountedChargeBoundaryKind.Mode, controller, () => true, () => calls.Add(1));
                queue.Enqueue(MountedChargeBoundaryKind.PartyCombat, controller, () => true, () => calls.Add(2));
                queue.Enqueue(MountedChargeBoundaryKind.Mode, controller, () => true, () => calls.Add(3));
                queue.TryDrain(() => true);
                TestRunner.Equal("2,3", string.Join(",", calls), "A superseded mode was replayed or ordering changed.");
            });
            runner.Run("world or setting replacement never replays stale controller request", () =>
            {
                var queue = new MountedChargeBoundaryQueue(); var calls = 0;
                queue.Enqueue(MountedChargeBoundaryKind.Mode, new object(), () => false, () => calls++);
                TestRunner.True(queue.TryDrain(() => true) && !queue.Pending, "Stale request remained executable.");
                TestRunner.Equal(0, calls, "Old world received native transition.");
            });
            runner.Run("throwing native transition retains a nonreplayable failure", () =>
            {
                var queue = new MountedChargeBoundaryQueue(); var calls = 0;
                queue.Enqueue(MountedChargeBoundaryKind.Mode, new object(), () => true, () => { calls++; throw new InvalidOperationException("native partial mutation"); });
                TestRunner.True(!queue.TryDrain(() => true) && queue.Pending && queue.Fault != null, "Partial native mutation was forgotten.");
                TestRunner.True(!queue.TryDrain(() => true), "An ambiguous native mutation was retried.");
                TestRunner.Equal(1, calls, "Native effects were replayed after a throw.");
            });
            runner.Run("pre-invocation inspection failure remains retryable", () =>
            {
                var queue = new MountedChargeBoundaryQueue(); var inspect = false; var calls = 0;
                queue.Enqueue(MountedChargeBoundaryKind.Mode, new object(), () => inspect ? true : throw new InvalidOperationException("inspection"), () => calls++);
                TestRunner.True(!queue.TryDrain(() => true) && queue.Pending && queue.Fault == null && queue.RetryError != null, "Inspection failure became irreversible native replay debt.");
                inspect = true;
                TestRunner.True(queue.TryDrain(() => true) && calls == 1, "Safe inspection retry did not recover.");
            });
            runner.Run("reentrant native notification cannot drain the same boundary twice", () =>
            {
                var queue = new MountedChargeBoundaryQueue();
                queue.Enqueue(MountedChargeBoundaryKind.PartyCombat, new object(), () => true, () =>
                    TestRunner.True(!queue.TryDrain(() => true), "Reentrant native callback completed its own request."));
                TestRunner.True(queue.TryDrain(() => true) && queue.Completed == 1, "Outer native callback did not finish exactly once.");
            });
            runner.Run("native callback retains its successor for a bounded later update", () =>
            {
                var queue = new MountedChargeBoundaryQueue(); var calls = 0; var controller = new object();
                queue.Enqueue(MountedChargeBoundaryKind.Mode, controller, () => true, () =>
                {
                    calls++;
                    queue.Enqueue(MountedChargeBoundaryKind.Mode, controller, () => true, () => calls += 10);
                    queue.Enqueue(MountedChargeBoundaryKind.Mode, controller, () => true, () => calls += 100);
                });
                TestRunner.True(!queue.TryDrain(() => true) && queue.Pending && calls == 1, "Reentrant replacement was lost or executed inside the old callback.");
                TestRunner.True(queue.TryDrain(() => true) && calls == 101 && queue.Completed == 2, "Latest successor did not execute exactly once.");
            });
        }
    }
}
