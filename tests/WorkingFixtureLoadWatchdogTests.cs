using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class WorkingFixtureLoadWatchdogTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("working load watchdog remains pending while pipeline is active", StillActiveRemainsPending);
            runner.Run("working load watchdog accepts callback completion", CallbackCompletionSucceeds);
            runner.Run("working load watchdog rejects inactive pipeline before callback", InactiveBeforeCallbackFails);
        }

        private static void StillActiveRemainsPending()
        {
            var watchdog = new WorkingFixtureLoadWatchdog();

            TestRunner.Equal(WorkingFixtureLoadWatchdogState.Pending, watchdog.Observe(true, false),
                "Active load pipeline did not remain pending.");
            TestRunner.True(watchdog.ObservedActivePipeline, "Active load pipeline was not observed.");
        }

        private static void CallbackCompletionSucceeds()
        {
            var watchdog = new WorkingFixtureLoadWatchdog();
            watchdog.Observe(true, false);

            TestRunner.Equal(WorkingFixtureLoadWatchdogState.CallbackCompleted, watchdog.Observe(true, true),
                "Completed SaveManager callback was not accepted.");
        }

        private static void InactiveBeforeCallbackFails()
        {
            var watchdog = new WorkingFixtureLoadWatchdog();

            TestRunner.Equal(WorkingFixtureLoadWatchdogState.Pending, watchdog.Observe(false, false),
                "Pre-bootstrap idle frame should remain pending.");
            TestRunner.Equal(WorkingFixtureLoadWatchdogState.Pending, watchdog.Observe(true, false),
                "Active pipeline should remain pending.");
            TestRunner.Equal(WorkingFixtureLoadWatchdogState.PipelineStoppedBeforeCallback, watchdog.Observe(false, false),
                "Pipeline active-to-inactive transition before callback was not rejected.");
        }
    }
}
