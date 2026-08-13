using System;

namespace KingmakerMountedCombat.Domain
{
    public sealed class MovementTelemetrySample
    {
        public MovementTelemetrySample(
            long sequence,
            double expectedX,
            double expectedY,
            double expectedZ,
            double observedX,
            double observedY,
            double observedZ,
            double expectedYawDegrees,
            double observedYawDegrees,
            double thresholdWorldUnits)
        {
            if (sequence < 0) { throw new ArgumentOutOfRangeException(nameof(sequence)); }
            if (thresholdWorldUnits <= 0.0d) { throw new ArgumentOutOfRangeException(nameof(thresholdWorldUnits)); }
            Sequence = sequence;
            PositionResidualWorldUnits = CalculateDistance(expectedX, expectedY, expectedZ, observedX, observedY, observedZ);
            RotationResidualDegrees = CalculateAngleDelta(expectedYawDegrees, observedYawDegrees);
            WithinPositionThreshold = PositionResidualWorldUnits <= thresholdWorldUnits;
        }

        public long Sequence { get; }

        public double PositionResidualWorldUnits { get; }

        public double RotationResidualDegrees { get; }

        public bool WithinPositionThreshold { get; }

        public static double CalculateDistance(double expectedX, double expectedY, double expectedZ, double observedX, double observedY, double observedZ)
        {
            var x = observedX - expectedX;
            var y = observedY - expectedY;
            var z = observedZ - expectedZ;
            return Math.Sqrt((x * x) + (y * y) + (z * z));
        }

        public static double CalculateAngleDelta(double expectedDegrees, double observedDegrees)
        {
            var delta = (observedDegrees - expectedDegrees) % 360.0d;
            if (delta > 180.0d) { delta -= 360.0d; }
            if (delta < -180.0d) { delta += 360.0d; }
            return Math.Abs(delta);
        }
    }

    public sealed class MovementTelemetryAccumulator
    {
        public long SampleCount { get; private set; }

        public double MaximumPositionResidualWorldUnits { get; private set; }

        public double MaximumRotationResidualDegrees { get; private set; }

        public int PositionThresholdViolationCount { get; private set; }

        public void Observe(MovementTelemetrySample sample)
        {
            if (sample == null) { throw new ArgumentNullException(nameof(sample)); }
            SampleCount++;
            MaximumPositionResidualWorldUnits = Math.Max(MaximumPositionResidualWorldUnits, sample.PositionResidualWorldUnits);
            MaximumRotationResidualDegrees = Math.Max(MaximumRotationResidualDegrees, sample.RotationResidualDegrees);
            if (!sample.WithinPositionThreshold) { PositionThresholdViolationCount++; }
        }
    }
}
