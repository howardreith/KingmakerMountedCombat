using System;
using System.Collections;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.Utility;
using KingmakerMountedCombat.Logging;
using UnityEngine;
using UnityModManagerNet;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed class MovementScreenshotCaptureCoordinator : IDisposable
    {
        private const int MaximumPendingCaptures = 32;
        private const int MaximumCaptureFrameDelay = 2;

        private readonly MovementScreenshotCaptureQueue queue = new MovementScreenshotCaptureQueue(MaximumPendingCaptures);
        private readonly Action<MovementScreenshotCaptureRequest, byte[]> captureCommitted;
        private readonly Action<MovementScreenshotCaptureRequest, string> captureFailed;
        private readonly IModLogger logger;

        private MovementScreenshotCaptureRequest activeRequest;
        private MovementCaptureOverlayLease activeLease;
        private UnityModManager.UI activeRunner;
        private Coroutine activeCoroutine;
        private int activeStartedFrame;
        private bool disposed;

        public MovementScreenshotCaptureCoordinator(
            Action<MovementScreenshotCaptureRequest, byte[]> captureCommitted,
            Action<MovementScreenshotCaptureRequest, string> captureFailed,
            IModLogger logger)
        {
            this.captureCommitted = captureCommitted ?? throw new ArgumentNullException(nameof(captureCommitted));
            this.captureFailed = captureFailed ?? throw new ArgumentNullException(nameof(captureFailed));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int PendingCount => queue.PendingCount;

        public bool HasInFlightCapture => queue.HasInFlightCapture;

        public void Enqueue(string row, string milestone, int readyFrame)
        {
            ThrowIfDisposed();
            queue.Enqueue(new MovementScreenshotCaptureRequest(row, milestone, readyFrame));
        }

        public void Pump(int currentFrame)
        {
            ThrowIfDisposed();
            if (activeRequest != null)
            {
                if (currentFrame > activeStartedFrame + MaximumCaptureFrameDelay)
                {
                    CancelPending("End-of-frame screenshot coroutine exceeded its fixed two-frame completion bound.");
                }
                return;
            }

            MovementScreenshotCaptureRequest request;
            if (!queue.TryBegin(currentFrame, out request))
            {
                return;
            }

            activeRequest = request;
            activeStartedFrame = currentFrame;
            try
            {
                var ui = UnityModManager.UI.Instance;
                if (ui == null)
                {
                    throw new InvalidOperationException("UMM overlay UI.Instance is missing before screenshot capture.");
                }

                activeRunner = ui;
                activeLease = MovementCaptureOverlayLease.Acquire(new UmmCaptureOverlay(ui));
                activeCoroutine = ui.StartCoroutine(CaptureAfterEndOfFrame(request));
                if (activeCoroutine == null)
                {
                    throw new InvalidOperationException("UMM overlay returned no coroutine handle for screenshot capture.");
                }
            }
            catch (Exception exception)
            {
                FailActiveRequest("Screenshot scheduling failed: " + Describe(exception));
            }
        }

        public void CancelPending(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = "Screenshot capture was cancelled.";
            }

            var active = activeRequest;
            var cancellationFailures = new List<string>();
            if (active != null)
            {
                try
                {
                    if (activeRunner != null && activeCoroutine != null)
                    {
                        activeRunner.StopCoroutine(activeCoroutine);
                    }
                }
                catch (Exception exception)
                {
                    cancellationFailures.Add("coroutine stop failed: " + Describe(exception));
                }
                try
                {
                    activeLease?.Dispose();
                }
                catch (Exception exception)
                {
                    cancellationFailures.Add("overlay restoration failed: " + Describe(exception));
                }
            }

            var cancelled = queue.CancelAll();
            ClearActive();
            for (var index = 0; index < cancelled.Count; index++)
            {
                var detail = reason;
                if (ReferenceEquals(cancelled[index], active) && cancellationFailures.Count != 0)
                {
                    detail += " " + string.Join("; ", cancellationFailures.ToArray());
                }
                SafeReportFailure(cancelled[index], detail);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            CancelPending("Screenshot capture was cancelled by movement-engine disposal.");
        }

        private IEnumerator CaptureAfterEndOfFrame(MovementScreenshotCaptureRequest request)
        {
            // Exactly one bounded render-boundary yield: no frame polling and no
            // Update-time ReadPixels call are allowed in this capture path.
            yield return new WaitForEndOfFrame();
            CompleteActiveRequestAtEndOfFrame(request);
        }

        private void CompleteActiveRequestAtEndOfFrame(MovementScreenshotCaptureRequest request)
        {
            if (disposed || !ReferenceEquals(request, activeRequest))
            {
                return;
            }

            byte[] bytes = null;
            var failures = new List<string>();
            try
            {
                activeLease.VerifyCaptureReady();
                var camera = Game.GetCamera();
                if (!camera)
                {
                    throw new InvalidOperationException("Kingmaker gameplay camera is missing at screenshot capture.");
                }
                bytes = Screenshot.CapturePNG(camera);
                if (bytes == null || bytes.Length == 0)
                {
                    throw new InvalidOperationException("Screenshot encoder returned no bytes.");
                }
            }
            catch (Exception exception)
            {
                failures.Add("end-of-frame capture failed: " + Describe(exception));
            }

            try
            {
                activeLease?.Dispose();
            }
            catch (Exception exception)
            {
                failures.Add("overlay restoration failed: " + Describe(exception));
            }

            if (failures.Count == 0)
            {
                try
                {
                    captureCommitted(request, bytes);
                }
                catch (Exception exception)
                {
                    failures.Add("screenshot evidence commit failed: " + Describe(exception));
                }
            }

            try
            {
                queue.Complete(request);
            }
            catch (Exception exception)
            {
                failures.Add("capture queue completion failed: " + Describe(exception));
            }
            ClearActive();

            if (failures.Count != 0)
            {
                SafeReportFailure(request, string.Join("; ", failures.ToArray()));
            }
        }

        private void FailActiveRequest(string reason)
        {
            var request = activeRequest;
            var failures = new List<string> { reason };
            try
            {
                activeLease?.Dispose();
            }
            catch (Exception exception)
            {
                failures.Add("overlay restoration failed: " + Describe(exception));
            }
            try
            {
                if (request != null && queue.HasInFlightCapture)
                {
                    queue.Complete(request);
                }
            }
            catch (Exception exception)
            {
                failures.Add("capture queue completion failed: " + Describe(exception));
            }
            ClearActive();
            if (request != null)
            {
                SafeReportFailure(request, string.Join("; ", failures.ToArray()));
            }
        }

        private void SafeReportFailure(MovementScreenshotCaptureRequest request, string reason)
        {
            try
            {
                captureFailed(request, reason);
            }
            catch (Exception exception)
            {
                logger.Exception("Movement screenshot failure reporting threw", exception);
            }
        }

        private void ClearActive()
        {
            activeRequest = null;
            activeLease = null;
            activeRunner = null;
            activeCoroutine = null;
            activeStartedFrame = 0;
        }

        private static string Describe(Exception exception)
        {
            return exception.GetType().Name + ": " + exception.Message;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(MovementScreenshotCaptureCoordinator));
            }
        }

        private sealed class UmmCaptureOverlay : IMovementCaptureOverlay
        {
            private readonly UnityModManager.UI instance;

            public UmmCaptureOverlay(UnityModManager.UI instance)
            {
                this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
            }

            public bool IsAvailable => instance != null && ReferenceEquals(UnityModManager.UI.Instance, instance);

            public bool Opened => instance.Opened;

            public void SetOpened(bool opened)
            {
                instance.ToggleWindow(opened);
            }
        }
    }
}
