using System;

namespace KingmakerMountedCombat.Diagnostics
{
    internal enum ManualReviewFixtureBoundary
    {
        Exact,
        FixtureUnavailable,
        Invalid
    }

    internal enum ManualReviewBoundaryDecision
    {
        Continue,
        BeginProcessTeardown,
        ContinueProcessTeardown,
        Fail
    }

    /// <summary>
    /// Distinguishes an ordinary process-exit teardown after READY from an
    /// interactive escape to another game boundary. The grace is one-way:
    /// once fixture objects disappear, the review can only finish by process
    /// exit; returning to a loaded boundary fails closed.
    /// </summary>
    internal sealed class ManualReviewBoundaryGuard
    {
        private readonly TimeSpan processTeardownGrace;
        private DateTimeOffset? processTeardownStartedAt;

        public ManualReviewBoundaryGuard(TimeSpan processTeardownGrace)
        {
            if (processTeardownGrace <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(processTeardownGrace));
            }

            this.processTeardownGrace = processTeardownGrace;
        }

        public bool ProcessTeardownStarted => processTeardownStartedAt.HasValue;

        public ManualReviewBoundaryDecision Observe(
            bool ready,
            ManualReviewFixtureBoundary boundary,
            DateTimeOffset observedAt)
        {
            if (processTeardownStartedAt.HasValue)
            {
                if (boundary != ManualReviewFixtureBoundary.FixtureUnavailable ||
                    observedAt < processTeardownStartedAt.Value ||
                    observedAt - processTeardownStartedAt.Value > processTeardownGrace)
                {
                    return ManualReviewBoundaryDecision.Fail;
                }

                return ManualReviewBoundaryDecision.ContinueProcessTeardown;
            }

            if (boundary == ManualReviewFixtureBoundary.Exact)
            {
                return ManualReviewBoundaryDecision.Continue;
            }

            if (ready && boundary == ManualReviewFixtureBoundary.FixtureUnavailable)
            {
                processTeardownStartedAt = observedAt;
                return ManualReviewBoundaryDecision.BeginProcessTeardown;
            }

            return ManualReviewBoundaryDecision.Fail;
        }
    }
}
