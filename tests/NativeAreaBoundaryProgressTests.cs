using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class NativeAreaBoundaryProgressTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("native area progress waits for delayed unload delivery", WaitsForDelayedUnloadDelivery);
            runner.Run("native area progress orders early unload before loading", OrdersEarlyUnloadBeforeLoading);
        }

        private static void WaitsForDelayedUnloadDelivery()
        {
            var progress = new NativeAreaBoundaryProgress();
            var loadingBeforeDelivery = progress.Observe(false, true);
            TestRunner.True(!loadingBeforeDelivery.CaptureCleanupLatch, "cleanup latch before native delivery");
            TestRunner.True(!loadingBeforeDelivery.CaptureLoadingStart, "loading-start evidence before cleanup latch");
            TestRunner.True(progress.LoadingObserved, "loading observation retained while delivery is pending");

            var deliveredDuringLoading = progress.Observe(true, true);
            TestRunner.True(deliveredDuringLoading.CaptureCleanupLatch, "delayed cleanup latch capture");
            TestRunner.True(deliveredDuringLoading.CaptureLoadingStart, "loading-start follows delayed cleanup latch");

            var duplicate = progress.Observe(true, true);
            TestRunner.True(!duplicate.CaptureCleanupLatch, "duplicate cleanup latch");
            TestRunner.True(!duplicate.CaptureLoadingStart, "duplicate loading-start");
        }

        private static void OrdersEarlyUnloadBeforeLoading()
        {
            var progress = new NativeAreaBoundaryProgress();
            var deliveryBeforeLoading = progress.Observe(true, false);
            TestRunner.True(deliveryBeforeLoading.CaptureCleanupLatch, "early cleanup latch capture");
            TestRunner.True(!deliveryBeforeLoading.CaptureLoadingStart, "loading-start before loading observation");

            var loading = progress.Observe(true, true);
            TestRunner.True(!loading.CaptureCleanupLatch, "duplicate early cleanup latch");
            TestRunner.True(loading.CaptureLoadingStart, "loading-start after early cleanup latch");
        }
    }
}
