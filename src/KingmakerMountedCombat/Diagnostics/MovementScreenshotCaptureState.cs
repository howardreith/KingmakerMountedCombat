using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Diagnostics
{
    internal interface IMovementCaptureOverlay
    {
        bool IsAvailable { get; }

        bool Opened { get; }

        void SetOpened(bool opened);
    }

    internal sealed class MovementCaptureOverlayLease : IDisposable
    {
        private readonly IMovementCaptureOverlay overlay;
        private readonly bool originallyOpened;
        private bool disposed;

        private MovementCaptureOverlayLease(IMovementCaptureOverlay overlay, bool originallyOpened)
        {
            this.overlay = overlay;
            this.originallyOpened = originallyOpened;
        }

        public static MovementCaptureOverlayLease Acquire(IMovementCaptureOverlay overlay)
        {
            if (overlay == null)
            {
                throw new ArgumentNullException(nameof(overlay));
            }
            if (!overlay.IsAvailable)
            {
                throw new InvalidOperationException("UMM overlay UI.Instance is missing before screenshot capture.");
            }

            var lease = new MovementCaptureOverlayLease(overlay, overlay.Opened);
            try
            {
                if (lease.originallyOpened)
                {
                    overlay.SetOpened(false);
                }
                lease.VerifyCaptureReady();
                return lease;
            }
            catch (Exception acquisitionException)
            {
                try
                {
                    lease.Restore();
                }
                catch (Exception restorationException)
                {
                    lease.disposed = true;
                    throw new AggregateException(
                        "UMM overlay could not be hidden and its captured state could not be restored.",
                        acquisitionException,
                        restorationException);
                }
                lease.disposed = true;
                throw;
            }
        }

        public void VerifyCaptureReady()
        {
            ThrowIfDisposed();
            if (!overlay.IsAvailable)
            {
                throw new InvalidOperationException("UMM overlay UI.Instance is missing at screenshot capture.");
            }
            if (overlay.Opened)
            {
                throw new InvalidOperationException("UMM overlay remained open at screenshot capture.");
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            try
            {
                Restore();
            }
            finally
            {
                disposed = true;
            }
        }

        private void Restore()
        {
            if (!overlay.IsAvailable)
            {
                throw new InvalidOperationException("UMM overlay UI.Instance is missing while restoring its captured state.");
            }
            if (overlay.Opened != originallyOpened)
            {
                overlay.SetOpened(originallyOpened);
            }
            if (!overlay.IsAvailable || overlay.Opened != originallyOpened)
            {
                throw new InvalidOperationException("UMM overlay did not return to its exact captured open state.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(MovementCaptureOverlayLease));
            }
        }
    }

    internal sealed class MovementScreenshotCaptureRequest
    {
        public MovementScreenshotCaptureRequest(string row, string milestone, int readyFrame)
        {
            if (string.IsNullOrWhiteSpace(row))
            {
                throw new ArgumentException("Screenshot row is required.", nameof(row));
            }
            if (string.IsNullOrWhiteSpace(milestone))
            {
                throw new ArgumentException("Screenshot milestone is required.", nameof(milestone));
            }
            if (readyFrame < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(readyFrame));
            }

            Row = row;
            Milestone = milestone;
            ReadyFrame = readyFrame;
        }

        public string Row { get; }

        public string Milestone { get; }

        public int ReadyFrame { get; }
    }

    internal sealed class MovementScreenshotCaptureQueue
    {
        private readonly int maximumPendingCount;
        private readonly Queue<MovementScreenshotCaptureRequest> waiting = new Queue<MovementScreenshotCaptureRequest>();
        private MovementScreenshotCaptureRequest active;

        public MovementScreenshotCaptureQueue(int maximumPendingCount)
        {
            if (maximumPendingCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumPendingCount));
            }
            this.maximumPendingCount = maximumPendingCount;
        }

        public int PendingCount => waiting.Count + (active == null ? 0 : 1);

        public bool HasInFlightCapture => active != null;

        public void Enqueue(MovementScreenshotCaptureRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (PendingCount >= maximumPendingCount)
            {
                throw new InvalidOperationException("Screenshot capture queue exceeded its fixed pending-request bound.");
            }
            waiting.Enqueue(request);
        }

        public bool TryBegin(int currentFrame, out MovementScreenshotCaptureRequest request)
        {
            request = null;
            if (active != null || waiting.Count == 0 || waiting.Peek().ReadyFrame > currentFrame)
            {
                return false;
            }

            active = waiting.Dequeue();
            request = active;
            return true;
        }

        public void Complete(MovementScreenshotCaptureRequest request)
        {
            if (request == null || !ReferenceEquals(request, active))
            {
                throw new InvalidOperationException("Only the exact in-flight screenshot request may be completed.");
            }
            active = null;
        }

        public IReadOnlyList<MovementScreenshotCaptureRequest> CancelAll()
        {
            var cancelled = new List<MovementScreenshotCaptureRequest>(PendingCount);
            if (active != null)
            {
                cancelled.Add(active);
                active = null;
            }
            while (waiting.Count != 0)
            {
                cancelled.Add(waiting.Dequeue());
            }
            return cancelled;
        }
    }
}
