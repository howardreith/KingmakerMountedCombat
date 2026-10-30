using System;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 2 (MOVE-01) measurement correction. The v1 sampler accumulated the first displacement
    // into the path but started the clock at the sample AFTER it, so the numerator covered one
    // interval the denominator omitted and short legs read fast. Here the motion interval opens at
    // the last sample at which the mover was still at rest, so path and time cover the same samples.
    // The v1 figure is kept beside the aligned one for comparison with retained runs.
    internal sealed class SpeedLegSampler
    {
        internal const string Contract = "aligned-interval/v2";
        internal const double DisplacementEpsilonMetres = 0.001;

        private bool hasSample;
        private double lastX, lastZ, previousSampleSeconds;
        private double? firstMotionSeconds, firstDisplacementSampleSeconds, lastMotionSeconds;

        internal double PathLength { get; private set; }
        internal double? FirstMotionSeconds => firstMotionSeconds;
        internal double? LastMotionSeconds => lastMotionSeconds;
        internal bool Moved => firstMotionSeconds.HasValue;

        // Path and clock over the same samples: from the last at-rest sample to the last moving sample.
        internal double MotionSeconds => Moved ? lastMotionSeconds.Value - firstMotionSeconds.Value : 0d;

        // The v1 interval (first displacement sample to last moving sample), for comparison only.
        internal double UnalignedMotionSeconds => Moved ? lastMotionSeconds.Value - firstDisplacementSampleSeconds.Value : 0d;

        internal double EffectiveSpeed => MotionSeconds > 0d ? PathLength / MotionSeconds : 0d;

        internal double UnalignedEffectiveSpeed => UnalignedMotionSeconds > 0d ? PathLength / UnalignedMotionSeconds : 0d;

        internal void Observe(double seconds, double x, double z)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || double.IsNaN(x) || double.IsNaN(z))
            {
                throw new ArgumentException("Speed leg samples must be finite.");
            }
            if (!hasSample)
            {
                hasSample = true;
                lastX = x; lastZ = z;
                previousSampleSeconds = seconds;
                return;
            }
            if (seconds < previousSampleSeconds)
            {
                throw new ArgumentException("Speed leg samples must not go back in time.");
            }
            var dx = x - lastX; var dz = z - lastZ;
            var moved = Math.Sqrt(dx * dx + dz * dz);
            if (moved > DisplacementEpsilonMetres)
            {
                PathLength += moved;
                lastX = x; lastZ = z;
                if (!firstMotionSeconds.HasValue)
                {
                    firstMotionSeconds = previousSampleSeconds;
                    firstDisplacementSampleSeconds = seconds;
                }
                lastMotionSeconds = seconds;
            }
            previousSampleSeconds = seconds;
        }
    }
}
