using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class ManualReviewBoundaryGuardTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("manual review rejects boundary loss before READY", RejectsBoundaryLossBeforeReady);
            runner.Run("manual review admits bounded one-way process teardown after READY", AdmitsBoundedOneWayTeardown);
            runner.Run("manual review rejects invalid boundary after READY", RejectsInvalidBoundaryAfterReady);
            runner.Run("manual review rejects a returned fixture after teardown starts", RejectsReturnedFixture);
            runner.Run("manual review rejects teardown beyond its grace", RejectsExpiredTeardown);
        }

        private static void RejectsBoundaryLossBeforeReady()
        {
            var guard = NewGuard();
            TestRunner.Equal(
                ManualReviewBoundaryDecision.Fail,
                guard.Observe(false, ManualReviewFixtureBoundary.FixtureUnavailable, At(0)),
                "Pre-READY fixture disappearance was mistaken for process teardown.");
        }

        private static void AdmitsBoundedOneWayTeardown()
        {
            var guard = NewGuard();
            TestRunner.Equal(
                ManualReviewBoundaryDecision.Continue,
                guard.Observe(true, ManualReviewFixtureBoundary.Exact, At(0)),
                "Exact READY boundary was rejected.");
            TestRunner.Equal(
                ManualReviewBoundaryDecision.BeginProcessTeardown,
                guard.Observe(true, ManualReviewFixtureBoundary.FixtureUnavailable, At(1)),
                "Post-READY fixture disappearance did not begin bounded teardown.");
            TestRunner.Equal(
                ManualReviewBoundaryDecision.ContinueProcessTeardown,
                guard.Observe(true, ManualReviewFixtureBoundary.FixtureUnavailable, At(10)),
                "Bounded process teardown did not remain admitted.");
        }

        private static void RejectsInvalidBoundaryAfterReady()
        {
            var guard = NewGuard();
            TestRunner.Equal(
                ManualReviewBoundaryDecision.Fail,
                guard.Observe(true, ManualReviewFixtureBoundary.Invalid, At(0)),
                "A loaded wrong fixture/combat/mode boundary was admitted as teardown.");
        }

        private static void RejectsReturnedFixture()
        {
            var guard = NewGuard();
            guard.Observe(true, ManualReviewFixtureBoundary.FixtureUnavailable, At(0));
            TestRunner.Equal(
                ManualReviewBoundaryDecision.Fail,
                guard.Observe(true, ManualReviewFixtureBoundary.Exact, At(1)),
                "The review resumed after its one-way teardown boundary began.");
        }

        private static void RejectsExpiredTeardown()
        {
            var guard = NewGuard();
            guard.Observe(true, ManualReviewFixtureBoundary.FixtureUnavailable, At(0));
            TestRunner.Equal(
                ManualReviewBoundaryDecision.Fail,
                guard.Observe(true, ManualReviewFixtureBoundary.FixtureUnavailable, At(16)),
                "A persistent menu/unloaded boundary outlived the process-exit grace.");
        }

        private static ManualReviewBoundaryGuard NewGuard()
        {
            return new ManualReviewBoundaryGuard(TimeSpan.FromSeconds(15));
        }

        private static DateTimeOffset At(int seconds)
        {
            return new DateTimeOffset(2026, 8, 16, 0, 0, seconds, TimeSpan.Zero);
        }
    }
}
