using System;

namespace KingmakerMountedCombat.Domain
{
    public enum MovementSynchronizationPhase
    {
        InitialConfiguration,
        Update,
        LateUpdate
    }

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

    public sealed class MovementSynchronizationSample
    {
        public MovementSynchronizationSample(
            long sequence,
            MovementSynchronizationPhase phase,
            double preCorrectionPositionResidualWorldUnits,
            double preCorrectionRotationResidualDegrees,
            double postCorrectionPositionResidualWorldUnits,
            double postCorrectionRotationResidualDegrees)
        {
            if (sequence < 0) { throw new ArgumentOutOfRangeException(nameof(sequence)); }
            if (phase != MovementSynchronizationPhase.InitialConfiguration && phase != MovementSynchronizationPhase.Update && phase != MovementSynchronizationPhase.LateUpdate)
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }
            RequireFiniteNonNegative(preCorrectionPositionResidualWorldUnits, nameof(preCorrectionPositionResidualWorldUnits));
            RequireFiniteNonNegative(preCorrectionRotationResidualDegrees, nameof(preCorrectionRotationResidualDegrees));
            RequireFiniteNonNegative(postCorrectionPositionResidualWorldUnits, nameof(postCorrectionPositionResidualWorldUnits));
            RequireFiniteNonNegative(postCorrectionRotationResidualDegrees, nameof(postCorrectionRotationResidualDegrees));

            Sequence = sequence;
            Phase = phase;
            PreCorrectionPositionResidualWorldUnits = preCorrectionPositionResidualWorldUnits;
            PreCorrectionRotationResidualDegrees = preCorrectionRotationResidualDegrees;
            PostCorrectionPositionResidualWorldUnits = postCorrectionPositionResidualWorldUnits;
            PostCorrectionRotationResidualDegrees = postCorrectionRotationResidualDegrees;
            CorrectionRequired = preCorrectionPositionResidualWorldUnits > 0.0d || preCorrectionRotationResidualDegrees > 0.0d;
        }

        public long Sequence { get; }

        public MovementSynchronizationPhase Phase { get; }

        public double PreCorrectionPositionResidualWorldUnits { get; }

        public double PreCorrectionRotationResidualDegrees { get; }

        public double PostCorrectionPositionResidualWorldUnits { get; }

        public double PostCorrectionRotationResidualDegrees { get; }

        public bool CorrectionRequired { get; }

        private static void RequireFiniteNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0d)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    public sealed class MovementSynchronizationTelemetryAccumulator
    {
        public long SampleCount { get; private set; }

        public long CorrectionCount { get; private set; }

        public long InitialConfigurationSampleCount { get; private set; }

        public long InitialConfigurationCorrectionCount { get; private set; }

        public long UpdateSampleCount { get; private set; }

        public long LateUpdateSampleCount { get; private set; }

        public long UpdateCorrectionCount { get; private set; }

        public long LateUpdateCorrectionCount { get; private set; }

        public MovementSynchronizationPhase LatestPhase { get; private set; }

        public double LatestPreCorrectionPositionResidualWorldUnits { get; private set; }

        public double LatestPreCorrectionRotationResidualDegrees { get; private set; }

        public double LatestPostCorrectionPositionResidualWorldUnits { get; private set; }

        public double LatestPostCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumPreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumPreCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumPostCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumPostCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumUpdatePreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumUpdatePreCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumUpdatePostCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumUpdatePostCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumLateUpdatePreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumLateUpdatePreCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumLateUpdatePostCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumLateUpdatePostCorrectionRotationResidualDegrees { get; private set; }

        public void Observe(MovementSynchronizationSample sample)
        {
            if (sample == null) { throw new ArgumentNullException(nameof(sample)); }
            if (sample.Sequence != SampleCount)
            {
                throw new InvalidOperationException("Movement synchronization sample sequence is not contiguous.");
            }

            SampleCount++;
            LatestPhase = sample.Phase;
            LatestPreCorrectionPositionResidualWorldUnits = sample.PreCorrectionPositionResidualWorldUnits;
            LatestPreCorrectionRotationResidualDegrees = sample.PreCorrectionRotationResidualDegrees;
            LatestPostCorrectionPositionResidualWorldUnits = sample.PostCorrectionPositionResidualWorldUnits;
            LatestPostCorrectionRotationResidualDegrees = sample.PostCorrectionRotationResidualDegrees;
            MaximumPreCorrectionPositionResidualWorldUnits = Math.Max(MaximumPreCorrectionPositionResidualWorldUnits, sample.PreCorrectionPositionResidualWorldUnits);
            MaximumPreCorrectionRotationResidualDegrees = Math.Max(MaximumPreCorrectionRotationResidualDegrees, sample.PreCorrectionRotationResidualDegrees);
            MaximumPostCorrectionPositionResidualWorldUnits = Math.Max(MaximumPostCorrectionPositionResidualWorldUnits, sample.PostCorrectionPositionResidualWorldUnits);
            MaximumPostCorrectionRotationResidualDegrees = Math.Max(MaximumPostCorrectionRotationResidualDegrees, sample.PostCorrectionRotationResidualDegrees);

            switch (sample.Phase)
            {
                case MovementSynchronizationPhase.InitialConfiguration:
                    InitialConfigurationSampleCount++;
                    if (sample.CorrectionRequired) { InitialConfigurationCorrectionCount++; }
                    break;
                case MovementSynchronizationPhase.Update:
                    UpdateSampleCount++;
                    if (sample.CorrectionRequired) { UpdateCorrectionCount++; }
                    MaximumUpdatePreCorrectionPositionResidualWorldUnits = Math.Max(MaximumUpdatePreCorrectionPositionResidualWorldUnits, sample.PreCorrectionPositionResidualWorldUnits);
                    MaximumUpdatePreCorrectionRotationResidualDegrees = Math.Max(MaximumUpdatePreCorrectionRotationResidualDegrees, sample.PreCorrectionRotationResidualDegrees);
                    MaximumUpdatePostCorrectionPositionResidualWorldUnits = Math.Max(MaximumUpdatePostCorrectionPositionResidualWorldUnits, sample.PostCorrectionPositionResidualWorldUnits);
                    MaximumUpdatePostCorrectionRotationResidualDegrees = Math.Max(MaximumUpdatePostCorrectionRotationResidualDegrees, sample.PostCorrectionRotationResidualDegrees);
                    break;
                case MovementSynchronizationPhase.LateUpdate:
                    LateUpdateSampleCount++;
                    if (sample.CorrectionRequired) { LateUpdateCorrectionCount++; }
                    MaximumLateUpdatePreCorrectionPositionResidualWorldUnits = Math.Max(MaximumLateUpdatePreCorrectionPositionResidualWorldUnits, sample.PreCorrectionPositionResidualWorldUnits);
                    MaximumLateUpdatePreCorrectionRotationResidualDegrees = Math.Max(MaximumLateUpdatePreCorrectionRotationResidualDegrees, sample.PreCorrectionRotationResidualDegrees);
                    MaximumLateUpdatePostCorrectionPositionResidualWorldUnits = Math.Max(MaximumLateUpdatePostCorrectionPositionResidualWorldUnits, sample.PostCorrectionPositionResidualWorldUnits);
                    MaximumLateUpdatePostCorrectionRotationResidualDegrees = Math.Max(MaximumLateUpdatePostCorrectionRotationResidualDegrees, sample.PostCorrectionRotationResidualDegrees);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sample), "Unknown movement synchronization phase.");
            }

            if (sample.CorrectionRequired)
            {
                CorrectionCount++;
            }
        }
    }
}
