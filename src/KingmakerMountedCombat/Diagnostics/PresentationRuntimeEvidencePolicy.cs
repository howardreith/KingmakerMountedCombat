using System;

namespace KingmakerMountedCombat.Diagnostics
{
    public static class PresentationRuntimeEvidencePolicy
    {
        public static double SelectLatestCumulativeAverage(
            long previousFrameCount,
            double previousAverageMicroseconds,
            long currentFrameCount,
            double currentAverageMicroseconds)
        {
            if (previousFrameCount < 0L || currentFrameCount < previousFrameCount)
            {
                throw new ArgumentOutOfRangeException(nameof(currentFrameCount));
            }
            RequireFiniteNonNegative(previousAverageMicroseconds, nameof(previousAverageMicroseconds));
            RequireFiniteNonNegative(currentAverageMicroseconds, nameof(currentAverageMicroseconds));
            if (currentFrameCount == 0L)
            {
                return 0.0d;
            }

            // The adapter exposes an average over every applied frame so far.
            // Retaining its maximum would permanently turn first-frame warm-up
            // cost into the row average. Advance only when the cumulative frame
            // count advances and retain the latest complete cumulative value.
            return currentFrameCount == previousFrameCount
                ? previousAverageMicroseconds
                : currentAverageMicroseconds;
        }

        public static bool HasRequiredSynchronizationPhaseCoverage(
            string row,
            long updateSampleCount,
            long lateUpdateSampleCount)
        {
            if (string.IsNullOrEmpty(row)) { throw new ArgumentException("A presentation row is required.", nameof(row)); }
            if (updateSampleCount < 0L) { throw new ArgumentOutOfRangeException(nameof(updateSampleCount)); }
            if (lateUpdateSampleCount < 0L) { throw new ArgumentOutOfRangeException(nameof(lateUpdateSampleCount)); }
            if (lateUpdateSampleCount == 0L)
            {
                return false;
            }

            // RiderMovementAgent.TickMovement is driven by the stock movement
            // loop. It is intentionally absent while the authoritative mount is
            // stationary, but the view-owned LateUpdate synchronizer still runs
            // every frame. Moving rows continue to require both phases.
            return updateSampleCount > 0L || IsStationaryPresentationRow(row);
        }

        private static bool IsStationaryPresentationRow(string row)
        {
            return string.Equals(row, "pose-idle", StringComparison.Ordinal) ||
                string.Equals(row, "pose-equipment-variants", StringComparison.Ordinal) ||
                string.Equals(row, "ui-selection-portrait-actionbar", StringComparison.Ordinal);
        }

        private static void RequireFiniteNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0d)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
