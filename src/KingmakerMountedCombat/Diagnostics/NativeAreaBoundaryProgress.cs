namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Orders asynchronous native area-unload evidence ahead of loading-start
    /// evidence. Kingmaker queues ReloadArea work; OnAreaBeginUnloading is not
    /// guaranteed to run before Game.ReloadArea returns.
    /// </summary>
    internal sealed class NativeAreaBoundaryProgress
    {
        private bool cleanupLatchCaptured;
        private bool loadingObserved;
        private bool loadingStartCaptured;

        public bool CleanupLatchCaptured => cleanupLatchCaptured;

        public bool LoadingObserved => loadingObserved;

        public NativeAreaBoundaryProgressDecision Observe(
            bool cleanupDeliveryObserved,
            bool loadingInProgress)
        {
            if (loadingInProgress)
            {
                loadingObserved = true;
            }

            var captureCleanupLatch = cleanupDeliveryObserved && !cleanupLatchCaptured;
            if (captureCleanupLatch)
            {
                cleanupLatchCaptured = true;
            }

            var captureLoadingStart = cleanupLatchCaptured && loadingObserved && !loadingStartCaptured;
            if (captureLoadingStart)
            {
                loadingStartCaptured = true;
            }

            return new NativeAreaBoundaryProgressDecision(
                captureCleanupLatch,
                captureLoadingStart);
        }
    }

    internal sealed class NativeAreaBoundaryProgressDecision
    {
        public NativeAreaBoundaryProgressDecision(
            bool captureCleanupLatch,
            bool captureLoadingStart)
        {
            CaptureCleanupLatch = captureCleanupLatch;
            CaptureLoadingStart = captureLoadingStart;
        }

        public bool CaptureCleanupLatch { get; }

        public bool CaptureLoadingStart { get; }
    }
}
