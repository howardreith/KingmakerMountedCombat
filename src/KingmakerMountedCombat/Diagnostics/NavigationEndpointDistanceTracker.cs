using System;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Keeps endpoint qualification independent from the coarser progress-clock
    /// watermark used to detect stuck movement.
    /// </summary>
    internal sealed class NavigationEndpointDistanceTracker
    {
        private readonly double progressClockHysteresis;
        private bool started;

        internal NavigationEndpointDistanceTracker(double progressClockHysteresis)
        {
            if (!IsFiniteNonnegative(progressClockHysteresis))
            {
                throw new ArgumentOutOfRangeException(nameof(progressClockHysteresis));
            }

            this.progressClockHysteresis = progressClockHysteresis;
            Reset();
        }

        internal double MinimumObservedDistance { get; private set; }

        internal double ProgressClockDistance { get; private set; }

        internal double LastProgressAtSeconds { get; private set; }

        internal void Start(double initialDistance, double observedAtSeconds)
        {
            ValidateObservation(initialDistance, observedAtSeconds);
            started = true;
            MinimumObservedDistance = initialDistance;
            ProgressClockDistance = initialDistance;
            LastProgressAtSeconds = observedAtSeconds;
        }

        internal bool Observe(double distance, double observedAtSeconds)
        {
            if (!started)
            {
                throw new InvalidOperationException("Navigation endpoint tracking has not started.");
            }

            ValidateObservation(distance, observedAtSeconds);
            MinimumObservedDistance = Math.Min(MinimumObservedDistance, distance);
            if (distance + progressClockHysteresis < ProgressClockDistance)
            {
                ProgressClockDistance = distance;
                LastProgressAtSeconds = observedAtSeconds;
                return true;
            }

            return false;
        }

        internal void Reset()
        {
            started = false;
            MinimumObservedDistance = double.MaxValue;
            ProgressClockDistance = double.MaxValue;
            LastProgressAtSeconds = 0.0d;
        }

        private static void ValidateObservation(double distance, double observedAtSeconds)
        {
            if (!IsFiniteNonnegative(distance))
            {
                throw new ArgumentOutOfRangeException(nameof(distance));
            }

            if (!IsFiniteNonnegative(observedAtSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(observedAtSeconds));
            }
        }

        private static bool IsFiniteNonnegative(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0.0d;
        }
    }
}
