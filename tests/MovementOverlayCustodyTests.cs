using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class MovementOverlayCustodyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("overlay cleanup retries a failed restore", RetryAfterFailure);
            runner.Run("overlay cleanup retries after its exact UI returns", RetryAfterMissingUi);
            runner.Run("overlay repeated cleanup reports an unresolved restore", RepeatFailure);
            runner.Run("overlay cleanup retries after a partial restore exception", RetryAfterPartialRestore);
            runner.Run("overlay cleanup verifies and retries an ignored restore", RetryAfterIgnoredRestore);
            runner.Run("overlay acquisition publishes custody before hiding", PublishesBeforeMutation);
            runner.Run("overlay acquisition keeps custody after failed compensation", KeepsFailedCompensation);
            runner.Run("overlay acquisition clears custody after verified compensation", ClearsSuccessfulCompensation);
            runner.Run("overlay acquisition has no custody before a valid capture", RejectsInvalidCapture);
            runner.Run("overlay acquisition keeps missing UI compensation pending", KeepsMissingUiCompensation);
            runner.Run("overlay closed state is captured without toggling", RetainsClosedState);
            runner.Run("overlay successful disposal is idempotent", SuccessfulDisposalIsIdempotent);
        }

        private static void RetryAfterFailure()
        {
            var overlay = new Overlay { Open = true };
            var lease = MovementCaptureOverlayLease.Acquire(overlay);
            overlay.OnSet = opened => { if (opened) throw new InvalidOperationException("restore failed"); };
            Throws<InvalidOperationException>(lease.Dispose);
            overlay.OnSet = null;
            lease.Dispose();
            TestRunner.Equal(true, overlay.Open, "Pending restoration was lost after the first failure.");
            TestRunner.Equal(3, overlay.SetCount, "Retry did not perform the missing restore.");
        }

        private static void RetryAfterMissingUi()
        {
            var overlay = new Overlay { Open = true };
            var lease = MovementCaptureOverlayLease.Acquire(overlay);
            overlay.Available = false;
            Throws<InvalidOperationException>(lease.Dispose);
            overlay.Available = true;
            lease.Dispose();
            TestRunner.Equal(true, overlay.Open, "Returning UI did not receive its pending restoration.");
        }

        private static void RepeatFailure()
        {
            var overlay = new Overlay { Open = true };
            var lease = MovementCaptureOverlayLease.Acquire(overlay);
            overlay.OnSet = opened => { throw new InvalidOperationException("restore still blocked"); };
            Throws<InvalidOperationException>(lease.Dispose);
            Throws<InvalidOperationException>(lease.Dispose);
            TestRunner.Equal(false, overlay.Open, "Failure fixture changed the overlay state.");
        }

        private static void RetryAfterPartialRestore()
        {
            var overlay = new Overlay { Open = true };
            var lease = MovementCaptureOverlayLease.Acquire(overlay);
            overlay.OnSet = opened => { overlay.Open = opened; throw new InvalidOperationException("after mutation"); };
            Throws<InvalidOperationException>(lease.Dispose);
            overlay.Open = false;
            overlay.OnSet = null;
            lease.Dispose();
            TestRunner.Equal(true, overlay.Open, "Partial restore exception prematurely relinquished custody.");
        }

        private static void RetryAfterIgnoredRestore()
        {
            var overlay = new Overlay { Open = true };
            var lease = MovementCaptureOverlayLease.Acquire(overlay);
            overlay.IgnoreSet = true;
            Throws<InvalidOperationException>(lease.Dispose);
            overlay.IgnoreSet = false;
            lease.Dispose();
            TestRunner.Equal(true, overlay.Open, "Unverified restoration was treated as completed.");
        }

        private static void PublishesBeforeMutation()
        {
            var overlay = new Overlay { Open = true };
            MovementCaptureOverlayLease lease = null;
            overlay.OnSet = opened => TestRunner.True(lease != null, "Mutation began without published cleanup custody.");
            MovementCaptureOverlayLease.AcquireRetained(overlay, out lease);
            TestRunner.True(lease != null, "Successful acquisition lost custody.");
            lease.VerifyCaptureReady();
            lease.Dispose();
            TestRunner.Equal(true, overlay.Open, "Retained acquisition lost its captured state.");
        }

        private static void KeepsFailedCompensation()
        {
            var overlay = new Overlay { Open = true };
            MovementCaptureOverlayLease lease = null;
            overlay.OnSet = opened =>
            {
                if (!opened) { overlay.Open = false; throw new InvalidOperationException("hide mutated then failed"); }
                throw new ApplicationException("compensation blocked");
            };
            var failure = Throws<AggregateException>(() => MovementCaptureOverlayLease.AcquireRetained(overlay, out lease));
            TestRunner.Equal(2, failure.InnerExceptions.Count, "Both operation and compensation errors must survive.");
            TestRunner.True(failure.InnerExceptions[0] is InvalidOperationException && failure.InnerExceptions[1] is ApplicationException, "Failure order changed.");
            TestRunner.True(lease != null, "Failed compensation abandoned its owner.");
            overlay.OnSet = null;
            lease.Dispose();
            TestRunner.Equal(true, overlay.Open, "Retained failed acquisition could not restore later.");
        }

        private static void ClearsSuccessfulCompensation()
        {
            var overlay = new Overlay { Open = true };
            MovementCaptureOverlayLease lease = null;
            var acquisitionFailure = new InvalidOperationException("hide failure");
            overlay.OnSet = opened => { if (!opened) { overlay.Open = false; throw acquisitionFailure; } };
            var failure = Throws<InvalidOperationException>(() => MovementCaptureOverlayLease.AcquireRetained(overlay, out lease));
            TestRunner.True(ReferenceEquals(acquisitionFailure, failure), "Successful compensation hid the original failure.");
            TestRunner.True(lease == null, "Verified compensation retained completed custody.");
            TestRunner.Equal(true, overlay.Open, "Compensation did not restore captured state.");
        }

        private static void RejectsInvalidCapture()
        {
            MovementCaptureOverlayLease lease = null;
            Throws<ArgumentNullException>(() => MovementCaptureOverlayLease.AcquireRetained(null, out lease));
            TestRunner.True(lease == null, "Null input published custody.");
            var overlay = new Overlay { Available = false, Open = true };
            Throws<InvalidOperationException>(() => MovementCaptureOverlayLease.AcquireRetained(overlay, out lease));
            TestRunner.True(lease == null, "Missing UI published custody.");
            TestRunner.Equal(0, overlay.SetCount, "Missing UI was mutated.");
        }

        private static void KeepsMissingUiCompensation()
        {
            var overlay = new Overlay { Open = true };
            MovementCaptureOverlayLease lease = null;
            overlay.OnSet = opened => { overlay.Available = false; };
            Throws<AggregateException>(() => MovementCaptureOverlayLease.AcquireRetained(overlay, out lease));
            TestRunner.True(lease != null, "Missing UI discarded unfinished compensation.");
            overlay.OnSet = null;
            overlay.Available = true;
            lease.Dispose();
            TestRunner.Equal(true, overlay.Open, "Returning UI was not restored after acquisition failure.");
        }

        private static void RetainsClosedState()
        {
            var overlay = new Overlay { Open = false };
            MovementCaptureOverlayLease lease;
            MovementCaptureOverlayLease.AcquireRetained(overlay, out lease);
            TestRunner.Equal(0, overlay.SetCount, "Acquisition toggled a closed overlay.");
            overlay.Open = true;
            lease.Dispose();
            TestRunner.Equal(false, overlay.Open, "Captured closed state was not restored.");
        }

        private static void SuccessfulDisposalIsIdempotent()
        {
            var overlay = new Overlay { Open = true };
            MovementCaptureOverlayLease lease;
            MovementCaptureOverlayLease.AcquireRetained(overlay, out lease);
            lease.Dispose();
            overlay.Available = false;
            lease.Dispose();
            TestRunner.Equal(2, overlay.SetCount, "Completed cleanup ran again.");
            Throws<ObjectDisposedException>(lease.VerifyCaptureReady);
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T exception) { return exception; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + " was not raised.");
        }

        private sealed class Overlay : IMovementCaptureOverlay
        {
            public bool Available = true;
            public bool Open;
            public bool IgnoreSet;
            public int SetCount;
            public Action<bool> OnSet;
            public bool IsAvailable => Available;
            public bool Opened => Open;
            public void SetOpened(bool opened)
            {
                SetCount++;
                OnSet?.Invoke(opened);
                if (!IgnoreSet) Open = opened;
            }
        }
    }
}
