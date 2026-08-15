using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class StopEarlyCaptureBoundaryTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("stop-early boundary queues missing capture before stop", QueuesMissingCaptureBeforeStop);
            runner.Run("stop-early boundary preserves existing capture before stop", PreservesExistingCaptureBeforeStop);
        }

        private static void QueuesMissingCaptureBeforeStop()
        {
            var boundary = new StopEarlyCaptureBoundary();
            TestRunner.Equal(StopEarlyCaptureDecision.None, boundary.Observe(false, false),
                "Stop boundary advanced before the movement threshold.");
            TestRunner.Equal(StopEarlyCaptureDecision.CaptureAndWait, boundary.Observe(true, false),
                "Missing moving capture was not queued at the stop threshold.");
            TestRunner.Equal(StopEarlyCaptureDecision.Stop, boundary.Observe(true, true),
                "Stop was not admitted after the capture render boundary.");
        }

        private static void PreservesExistingCaptureBeforeStop()
        {
            var boundary = new StopEarlyCaptureBoundary();
            TestRunner.Equal(StopEarlyCaptureDecision.Wait, boundary.Observe(true, true),
                "Existing moving capture did not retain one pre-stop render boundary.");
            TestRunner.Equal(StopEarlyCaptureDecision.Stop, boundary.Observe(true, true),
                "Stop was not admitted after retaining the existing capture boundary.");
            boundary.Reset();
            TestRunner.Equal(StopEarlyCaptureDecision.CaptureAndWait, boundary.Observe(true, false),
                "Reset did not restore the missing-capture decision.");
        }
    }
}
