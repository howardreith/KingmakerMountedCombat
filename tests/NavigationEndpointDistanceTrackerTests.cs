using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class NavigationEndpointDistanceTrackerTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run(
                "endpoint minimum is independent from progress-clock hysteresis",
                EndpointMinimumIsIndependentFromProgressClockHysteresis);
            runner.Run(
                "endpoint minimum includes exact-hysteresis and final observations",
                EndpointMinimumIncludesExactHysteresisAndFinalObservations);
            runner.Run(
                "endpoint tracker reset and invalid input fail closed",
                EndpointTrackerResetAndInvalidInputFailClosed);
        }

        private static void EndpointMinimumIsIndependentFromProgressClockHysteresis()
        {
            var tracker = new NavigationEndpointDistanceTracker(0.10d);
            tracker.Start(2.0d, 0.0d);
            TestRunner.True(tracker.Observe(1.297d, 1.0d), "Large approach did not advance the progress clock.");
            TestRunner.Equal(1.297d, tracker.ProgressClockDistance, "Progress watermark did not retain its advanced distance.");
            TestRunner.Equal(1.0d, tracker.LastProgressAtSeconds, "Progress timestamp did not advance with the watermark.");

            TestRunner.Equal(false, tracker.Observe(1.207d, 2.0d), "Sub-hysteresis approach unexpectedly advanced the progress clock.");
            TestRunner.Equal(1.207d, tracker.MinimumObservedDistance, "True endpoint minimum lost the sub-hysteresis observation.");
            TestRunner.Equal(1.297d, tracker.ProgressClockDistance, "Endpoint qualification rewrote the progress watermark.");
            TestRunner.Equal(1.0d, tracker.LastProgressAtSeconds, "Sub-hysteresis approach rewrote the progress timestamp.");
            TestRunner.True(tracker.MinimumObservedDistance <= 1.25d, "The exact runtime regression did not qualify against the unchanged reach tolerance.");
            TestRunner.True(tracker.ProgressClockDistance > 1.25d, "The synthetic progress watermark no longer reproduces the runtime regression.");
        }

        private static void EndpointMinimumIncludesExactHysteresisAndFinalObservations()
        {
            var tracker = new NavigationEndpointDistanceTracker(0.10d);
            tracker.Start(1.40d, 4.0d);

            TestRunner.Equal(false, tracker.Observe(1.30d, 5.0d), "An exact-hysteresis improvement changed strict progress-clock semantics.");
            TestRunner.Equal(1.30d, tracker.MinimumObservedDistance, "Exact-hysteresis observation was omitted from the endpoint minimum.");
            TestRunner.Equal(1.40d, tracker.ProgressClockDistance, "Exact-hysteresis observation advanced a strict progress watermark.");

            tracker.Observe(1.22d, 6.0d);
            tracker.Observe(1.24d, 7.0d);
            TestRunner.Equal(1.22d, tracker.MinimumObservedDistance, "A worse final observation replaced the true endpoint minimum.");
            TestRunner.Equal(1.22d, tracker.ProgressClockDistance, "A qualifying progress observation was not retained.");
            TestRunner.Equal(6.0d, tracker.LastProgressAtSeconds, "A worse final observation changed the progress timestamp.");
        }

        private static void EndpointTrackerResetAndInvalidInputFailClosed()
        {
            var tracker = new NavigationEndpointDistanceTracker(0.10d);
            tracker.Start(3.0d, 1.0d);
            tracker.Observe(2.0d, 2.0d);
            tracker.Reset();

            TestRunner.Equal(double.MaxValue, tracker.MinimumObservedDistance, "Reset retained an endpoint minimum.");
            TestRunner.Equal(double.MaxValue, tracker.ProgressClockDistance, "Reset retained a progress watermark.");
            TestRunner.Equal(0.0d, tracker.LastProgressAtSeconds, "Reset retained a progress timestamp.");

            AssertThrows<InvalidOperationException>(
                () => tracker.Observe(1.0d, 3.0d),
                "Observation before Start was accepted.");
            AssertThrows<ArgumentOutOfRangeException>(
                () => tracker.Start(double.NaN, 0.0d),
                "NaN endpoint distance was accepted.");
            AssertThrows<ArgumentOutOfRangeException>(
                () => tracker.Start(1.0d, double.PositiveInfinity),
                "Infinite observation time was accepted.");
        }

        private static void AssertThrows<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
