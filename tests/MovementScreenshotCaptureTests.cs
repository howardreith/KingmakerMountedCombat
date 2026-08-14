using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class MovementScreenshotCaptureTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("screenshot queue waits for declared frame and retains in-flight work", QueueWaitsAndRetainsInFlightWork);
            runner.Run("screenshot queue completes only exact active request", QueueCompletesOnlyExactActiveRequest);
            runner.Run("screenshot queue cancellation includes active and waiting work", QueueCancellationIncludesAllPendingWork);
            runner.Run("screenshot overlay lease closes and restores initially open state", LeaseRestoresInitiallyOpenState);
            runner.Run("screenshot overlay lease restores initially closed state", LeaseRestoresInitiallyClosedState);
            runner.Run("screenshot overlay lease rejects missing UI instance", LeaseRejectsMissingOverlay);
            runner.Run("screenshot overlay lease rejects missing or open UI at capture", LeaseRejectsInvalidCaptureState);
            runner.Run("screenshot overlay lease restores after acquisition failure", LeaseRestoresAfterAcquisitionFailure);
            runner.Run("screenshot overlay lease reports restoration failure", LeaseReportsRestorationFailure);
        }

        private static void QueueWaitsAndRetainsInFlightWork()
        {
            var queue = new MovementScreenshotCaptureQueue(4);
            var request = new MovementScreenshotCaptureRequest("mounted-pair-open-ground", "moving", 8);
            queue.Enqueue(request);

            MovementScreenshotCaptureRequest observed;
            TestRunner.Equal(false, queue.TryBegin(7, out observed), "Capture began before its declared frame.");
            TestRunner.Equal(1, queue.PendingCount, "Queued capture stopped counting as pending.");
            TestRunner.Equal(true, queue.TryBegin(8, out observed), "Capture did not begin at its declared frame.");
            TestRunner.True(ReferenceEquals(request, observed), "Queue returned a different request instance.");
            TestRunner.Equal(true, queue.HasInFlightCapture, "Active capture was not marked in flight.");
            TestRunner.Equal(1, queue.PendingCount, "In-flight capture stopped counting as pending.");
            TestRunner.Equal(false, queue.TryBegin(9, out observed), "Queue began another capture while one was in flight.");

            queue.Complete(request);
            TestRunner.Equal(0, queue.PendingCount, "Completed capture remained pending.");
            TestRunner.Equal(false, queue.HasInFlightCapture, "Completed capture remained in flight.");
        }

        private static void QueueCompletesOnlyExactActiveRequest()
        {
            var queue = new MovementScreenshotCaptureQueue(1);
            var request = new MovementScreenshotCaptureRequest("mounted-pair-open-ground", "stopped", 1);
            queue.Enqueue(request);
            ExpectThrows<InvalidOperationException>(
                () => queue.Enqueue(new MovementScreenshotCaptureRequest("mounted-pair-open-ground", "moving", 1)),
                "Queue accepted work beyond its fixed bound.");

            MovementScreenshotCaptureRequest observed;
            TestRunner.Equal(true, queue.TryBegin(1, out observed), "Bounded request did not begin.");
            ExpectThrows<InvalidOperationException>(
                () => queue.Complete(new MovementScreenshotCaptureRequest("mounted-pair-open-ground", "stopped", 1)),
                "Queue completed a lookalike rather than the exact active request.");
            TestRunner.Equal(1, queue.PendingCount, "Rejected completion removed active work.");
            queue.Complete(request);
        }

        private static void QueueCancellationIncludesAllPendingWork()
        {
            var queue = new MovementScreenshotCaptureQueue(3);
            var active = new MovementScreenshotCaptureRequest("mounted-pair-open-ground", "moving", 1);
            var waiting = new MovementScreenshotCaptureRequest("mounted-pair-open-ground", "stopped", 2);
            queue.Enqueue(active);
            queue.Enqueue(waiting);
            MovementScreenshotCaptureRequest observed;
            TestRunner.Equal(true, queue.TryBegin(1, out observed), "First request did not enter flight.");

            var cancelled = queue.CancelAll();
            TestRunner.Equal(2, cancelled.Count, "Cancellation did not return every pending request.");
            TestRunner.True(ReferenceEquals(active, cancelled[0]), "Cancellation did not report the active request first.");
            TestRunner.True(ReferenceEquals(waiting, cancelled[1]), "Cancellation did not preserve waiting request order.");
            TestRunner.Equal(0, queue.PendingCount, "Cancellation left pending work.");
        }

        private static void LeaseRestoresInitiallyOpenState()
        {
            var overlay = new FakeOverlay { Available = true, IsOpened = true };
            using (var lease = MovementCaptureOverlayLease.Acquire(overlay))
            {
                TestRunner.Equal(false, overlay.IsOpened, "Lease did not close an initially open overlay.");
                lease.VerifyCaptureReady();
            }
            TestRunner.Equal(true, overlay.IsOpened, "Lease did not restore the initially open overlay.");
            TestRunner.Equal(2, overlay.SetCount, "Lease did not perform one close and one restore operation.");
        }

        private static void LeaseRestoresInitiallyClosedState()
        {
            var overlay = new FakeOverlay { Available = true, IsOpened = false };
            var lease = MovementCaptureOverlayLease.Acquire(overlay);
            TestRunner.Equal(0, overlay.SetCount, "Lease toggled an already closed overlay during acquisition.");
            overlay.IsOpened = true;
            lease.Dispose();
            TestRunner.Equal(false, overlay.IsOpened, "Lease did not restore the captured closed state.");
            TestRunner.Equal(1, overlay.SetCount, "Lease did not use one exact restoration operation.");
        }

        private static void LeaseRejectsMissingOverlay()
        {
            var overlay = new FakeOverlay { Available = false, IsOpened = true };
            ExpectThrows<InvalidOperationException>(
                () => MovementCaptureOverlayLease.Acquire(overlay),
                "Lease accepted a missing overlay instance.");
        }

        private static void LeaseRejectsInvalidCaptureState()
        {
            var overlay = new FakeOverlay { Available = true, IsOpened = true };
            var lease = MovementCaptureOverlayLease.Acquire(overlay);
            overlay.IsOpened = true;
            ExpectThrows<InvalidOperationException>(lease.VerifyCaptureReady, "Lease accepted an open overlay at capture.");
            overlay.Available = false;
            ExpectThrows<InvalidOperationException>(lease.VerifyCaptureReady, "Lease accepted a missing overlay at capture.");
            overlay.Available = true;
            lease.Dispose();
            TestRunner.Equal(true, overlay.IsOpened, "Lease lost the original open state after rejected capture checks.");
        }

        private static void LeaseReportsRestorationFailure()
        {
            var overlay = new FakeOverlay { Available = true, IsOpened = true };
            var lease = MovementCaptureOverlayLease.Acquire(overlay);
            overlay.ThrowWhenOpening = true;
            ExpectThrows<InvalidOperationException>(lease.Dispose, "Lease concealed a failed exact-state restoration.");
        }

        private static void LeaseRestoresAfterAcquisitionFailure()
        {
            var overlay = new FakeOverlay
            {
                Available = true,
                IsOpened = true,
                ThrowAfterClosingOnce = true
            };
            ExpectThrows<InvalidOperationException>(
                () => MovementCaptureOverlayLease.Acquire(overlay),
                "Lease concealed an acquisition toggle failure.");
            TestRunner.Equal(true, overlay.IsOpened, "Acquisition failure did not restore the captured open state.");
            TestRunner.Equal(2, overlay.SetCount, "Acquisition rollback did not perform one failed close and one restore.");
        }

        private static void ExpectThrows<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private sealed class FakeOverlay : IMovementCaptureOverlay
        {
            public bool Available { get; set; }

            public bool IsOpened { get; set; }

            public bool ThrowWhenOpening { get; set; }

            public bool ThrowAfterClosingOnce { get; set; }

            public int SetCount { get; private set; }

            public bool IsAvailable => Available;

            public bool Opened => IsOpened;

            public void SetOpened(bool opened)
            {
                SetCount++;
                if (opened && ThrowWhenOpening)
                {
                    throw new InvalidOperationException("Synthetic overlay open failure.");
                }
                IsOpened = opened;
                if (!opened && ThrowAfterClosingOnce)
                {
                    ThrowAfterClosingOnce = false;
                    throw new InvalidOperationException("Synthetic overlay close failure after mutation.");
                }
            }
        }
    }
}
