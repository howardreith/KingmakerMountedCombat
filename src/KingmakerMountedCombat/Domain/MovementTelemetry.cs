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

    /// <summary>
    /// Tracks the exact Update/LateUpdate ordering contract used by the
    /// movement-only prototype.  A logical rider entity may temporarily retain
    /// the authoritative yaw sampled earlier in the same Unity frame, but only
    /// while the parented rider view already matches the current mount yaw.  The
    /// next Update must observe the entity at the current yaw before another
    /// phase lag can be accepted.
    /// </summary>
    public sealed class MovementYawPhaseTracker
    {
        private const double AuthorityChangeEpsilonDegrees = 0.000001d;
        public const double RawLagArithmeticCoherenceEpsilonDegrees = 0.0001d;
        private bool hasObservedAuthority;
        private double lastObservedAuthoritativeYawDegrees;
        private long authoritativeYawSequence;
        private bool hasUpdateReference;
        private long updateReferenceFrame;
        private double updateReferenceYawDegrees;
        private long updateReferenceAuthoritySequence;
        private bool recoveryPending;

        public long ObservationCount { get; private set; }

        public MovementYawPhaseObservation Observe(
            long frame,
            MovementSynchronizationPhase phase,
            double currentAuthoritativeYawDegrees,
            double mountEntityAuthoritativeYawDegrees,
            double riderViewYawDegrees,
            double riderEntityYawDegrees,
            double maximumYawResidualDegrees)
        {
            if (frame < 0) { throw new ArgumentOutOfRangeException(nameof(frame)); }
            if (phase != MovementSynchronizationPhase.InitialConfiguration && phase != MovementSynchronizationPhase.Update && phase != MovementSynchronizationPhase.LateUpdate)
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }
            RequireFinite(currentAuthoritativeYawDegrees, nameof(currentAuthoritativeYawDegrees));
            RequireFinite(mountEntityAuthoritativeYawDegrees, nameof(mountEntityAuthoritativeYawDegrees));
            RequireFinite(riderViewYawDegrees, nameof(riderViewYawDegrees));
            RequireFinite(riderEntityYawDegrees, nameof(riderEntityYawDegrees));
            if (double.IsNaN(maximumYawResidualDegrees) || double.IsInfinity(maximumYawResidualDegrees) || maximumYawResidualDegrees <= 0.0d)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumYawResidualDegrees));
            }

            var authoritativeYawDeltaDegrees = hasObservedAuthority
                ? MovementTelemetrySample.CalculateAngleDelta(lastObservedAuthoritativeYawDegrees, currentAuthoritativeYawDegrees)
                : 0.0d;
            if (!hasObservedAuthority)
            {
                hasObservedAuthority = true;
            }
            else if (authoritativeYawDeltaDegrees > AuthorityChangeEpsilonDegrees)
            {
                authoritativeYawSequence++;
            }

            var calibrated = phase == MovementSynchronizationPhase.Update || phase == MovementSynchronizationPhase.LateUpdate;
            var mountEntityRootYawResidualDegrees = MovementTelemetrySample.CalculateAngleDelta(
                currentAuthoritativeYawDegrees,
                mountEntityAuthoritativeYawDegrees);
            var viewCurrentYawResidualDegrees = MovementTelemetrySample.CalculateAngleDelta(currentAuthoritativeYawDegrees, riderViewYawDegrees);
            var entityRawCurrentYawResidualDegrees = MovementTelemetrySample.CalculateAngleDelta(currentAuthoritativeYawDegrees, riderEntityYawDegrees);
            var previousReferenceAvailable = phase == MovementSynchronizationPhase.LateUpdate && hasUpdateReference;
            var previousReferenceYawDegrees = previousReferenceAvailable ? (double?)updateReferenceYawDegrees : null;
            var previousReferenceFrame = previousReferenceAvailable ? (long?)updateReferenceFrame : null;
            var previousReferencePhase = previousReferenceAvailable ? (MovementSynchronizationPhase?)MovementSynchronizationPhase.Update : null;
            var previousReferenceAuthoritySequence = previousReferenceAvailable ? (long?)updateReferenceAuthoritySequence : null;
            var entityPreviousAuthoritativeYawResidualDegrees = previousReferenceAvailable
                ? (double?)MovementTelemetrySample.CalculateAngleDelta(updateReferenceYawDegrees, riderEntityYawDegrees)
                : null;
            var authorityAgeSteps = previousReferenceAvailable
                ? authoritativeYawSequence - updateReferenceAuthoritySequence
                : 0L;
            var previousReferenceSameFrame = previousReferenceAvailable && updateReferenceFrame == frame;
            var previousReferenceEligible = previousReferenceSameFrame && authorityAgeSteps == 1L;
            var entityYawAuthorityAgeSteps = entityRawCurrentYawResidualDegrees <= maximumYawResidualDegrees
                ? (long?)0L
                : entityPreviousAuthoritativeYawResidualDegrees.HasValue &&
                    entityPreviousAuthoritativeYawResidualDegrees.Value <= maximumYawResidualDegrees
                    ? (long?)authorityAgeSteps
                    : null;
            var phaseLagObserved = calibrated && entityRawCurrentYawResidualDegrees > maximumYawResidualDegrees;
            // The 0.10-degree acceptance gate applies independently to current
            // view/adjusted entity state. Raw entity lag must numerically track
            // the actual current-vs-previous authority delta; only the named
            // arithmetic coherence epsilon is allowed here.
            var entityRawLagBoundDegrees = authoritativeYawDeltaDegrees;
            var entityRawLagExcessDegrees = Math.Max(0.0d, entityRawCurrentYawResidualDegrees - entityRawLagBoundDegrees);
            var recoveryRequiredBeforeSample = recoveryPending;
            var recoveryUpdateObserved = phase == MovementSynchronizationPhase.Update && recoveryRequiredBeforeSample;
            var recoverySatisfied = recoveryUpdateObserved &&
                viewCurrentYawResidualDegrees <= maximumYawResidualDegrees &&
                entityRawCurrentYawResidualDegrees <= maximumYawResidualDegrees;
            var recoveryViolation = recoveryRequiredBeforeSample &&
                (phase != MovementSynchronizationPhase.Update || !recoverySatisfied);
            if (phase == MovementSynchronizationPhase.Update && recoveryRequiredBeforeSample)
            {
                recoveryPending = false;
            }

            var phaseLagPermitted = phaseLagObserved &&
                phase == MovementSynchronizationPhase.LateUpdate &&
                previousReferenceEligible &&
                entityPreviousAuthoritativeYawResidualDegrees.HasValue &&
                entityPreviousAuthoritativeYawResidualDegrees.Value <= maximumYawResidualDegrees &&
                viewCurrentYawResidualDegrees <= maximumYawResidualDegrees &&
                entityRawLagExcessDegrees <= RawLagArithmeticCoherenceEpsilonDegrees &&
                !recoveryRequiredBeforeSample;
            var phaseLagViolation = phaseLagObserved && !phaseLagPermitted;
            var entityPhaseAdjustedYawResidualDegrees = phaseLagPermitted
                ? Math.Min(entityRawCurrentYawResidualDegrees, entityPreviousAuthoritativeYawResidualDegrees.Value)
                : entityRawCurrentYawResidualDegrees;
            var stationaryAuthority = calibrated && authoritativeYawDeltaDegrees <= AuthorityChangeEpsilonDegrees;
            var stationaryYawCorrectionViolation = stationaryAuthority &&
                (viewCurrentYawResidualDegrees > maximumYawResidualDegrees ||
                 entityRawCurrentYawResidualDegrees > maximumYawResidualDegrees);

            if (phaseLagPermitted)
            {
                recoveryPending = true;
            }

            var observation = new MovementYawPhaseObservation(
                ObservationCount++,
                frame,
                phase,
                authoritativeYawSequence,
                currentAuthoritativeYawDegrees,
                mountEntityAuthoritativeYawDegrees,
                mountEntityRootYawResidualDegrees,
                previousReferenceAuthoritySequence,
                previousReferenceYawDegrees,
                previousReferenceFrame,
                previousReferencePhase,
                previousReferenceSameFrame,
                previousReferenceEligible,
                authoritativeYawDeltaDegrees,
                viewCurrentYawResidualDegrees,
                entityRawCurrentYawResidualDegrees,
                entityPreviousAuthoritativeYawResidualDegrees,
                entityPhaseAdjustedYawResidualDegrees,
                entityRawLagBoundDegrees,
                entityRawLagExcessDegrees,
                entityYawAuthorityAgeSteps,
                phaseLagObserved,
                phaseLagPermitted,
                phaseLagViolation,
                recoveryRequiredBeforeSample,
                recoveryUpdateObserved,
                recoverySatisfied,
                recoveryViolation,
                recoveryPending,
                stationaryAuthority,
                stationaryYawCorrectionViolation);

            if (phase == MovementSynchronizationPhase.Update)
            {
                hasUpdateReference = true;
                updateReferenceFrame = frame;
                updateReferenceYawDegrees = currentAuthoritativeYawDegrees;
                updateReferenceAuthoritySequence = authoritativeYawSequence;
            }
            lastObservedAuthoritativeYawDegrees = currentAuthoritativeYawDegrees;
            return observation;
        }

        private static void RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    public sealed class MovementYawPhaseObservation
    {
        internal MovementYawPhaseObservation(
            long sequence,
            long frame,
            MovementSynchronizationPhase phase,
            long authoritativeYawSequence,
            double currentAuthoritativeYawDegrees,
            double mountEntityAuthoritativeYawDegrees,
            double mountEntityRootYawResidualDegrees,
            long? previousAuthoritativeYawSequence,
            double? previousAuthoritativeYawDegrees,
            long? previousAuthoritativeFrame,
            MovementSynchronizationPhase? previousAuthoritativePhase,
            bool previousAuthoritativeSameFrame,
            bool previousAuthoritativeReferenceEligible,
            double authoritativeYawDeltaDegrees,
            double viewCurrentYawResidualDegrees,
            double entityRawCurrentYawResidualDegrees,
            double? entityPreviousAuthoritativeYawResidualDegrees,
            double entityPhaseAdjustedYawResidualDegrees,
            double entityRawLagBoundDegrees,
            double entityRawLagExcessDegrees,
            long? entityYawAuthorityAgeSteps,
            bool phaseLagObserved,
            bool phaseLagPermitted,
            bool phaseLagViolation,
            bool recoveryRequiredBeforeSample,
            bool recoveryUpdateObserved,
            bool recoverySatisfied,
            bool recoveryViolation,
            bool recoveryPendingAfterSample,
            bool stationaryAuthority,
            bool stationaryYawCorrectionViolation)
        {
            Sequence = sequence;
            Frame = frame;
            Phase = phase;
            AuthoritativeYawSequence = authoritativeYawSequence;
            CurrentAuthoritativeYawDegrees = currentAuthoritativeYawDegrees;
            MountEntityAuthoritativeYawDegrees = mountEntityAuthoritativeYawDegrees;
            MountEntityRootYawResidualDegrees = mountEntityRootYawResidualDegrees;
            PreviousAuthoritativeYawSequence = previousAuthoritativeYawSequence;
            PreviousAuthoritativeYawDegrees = previousAuthoritativeYawDegrees;
            PreviousAuthoritativeFrame = previousAuthoritativeFrame;
            PreviousAuthoritativePhase = previousAuthoritativePhase;
            PreviousAuthoritativeSameFrame = previousAuthoritativeSameFrame;
            PreviousAuthoritativeReferenceEligible = previousAuthoritativeReferenceEligible;
            AuthoritativeYawDeltaDegrees = authoritativeYawDeltaDegrees;
            ViewCurrentYawResidualDegrees = viewCurrentYawResidualDegrees;
            EntityRawCurrentYawResidualDegrees = entityRawCurrentYawResidualDegrees;
            EntityPreviousAuthoritativeYawResidualDegrees = entityPreviousAuthoritativeYawResidualDegrees;
            EntityPhaseAdjustedYawResidualDegrees = entityPhaseAdjustedYawResidualDegrees;
            EntityRawLagBoundDegrees = entityRawLagBoundDegrees;
            EntityRawLagExcessDegrees = entityRawLagExcessDegrees;
            EntityYawAuthorityAgeSteps = entityYawAuthorityAgeSteps;
            PhaseLagObserved = phaseLagObserved;
            PhaseLagPermitted = phaseLagPermitted;
            PhaseLagViolation = phaseLagViolation;
            RecoveryRequiredBeforeSample = recoveryRequiredBeforeSample;
            RecoveryUpdateObserved = recoveryUpdateObserved;
            RecoverySatisfied = recoverySatisfied;
            RecoveryViolation = recoveryViolation;
            RecoveryPendingAfterSample = recoveryPendingAfterSample;
            StationaryAuthority = stationaryAuthority;
            StationaryYawCorrectionViolation = stationaryYawCorrectionViolation;
        }

        public long Sequence { get; }
        public long Frame { get; }
        public MovementSynchronizationPhase Phase { get; }
        public long AuthoritativeYawSequence { get; }
        public double CurrentAuthoritativeYawDegrees { get; }
        public double MountEntityAuthoritativeYawDegrees { get; }
        public double MountEntityRootYawResidualDegrees { get; }
        public long? PreviousAuthoritativeYawSequence { get; }
        public double? PreviousAuthoritativeYawDegrees { get; }
        public long? PreviousAuthoritativeFrame { get; }
        public MovementSynchronizationPhase? PreviousAuthoritativePhase { get; }
        public bool PreviousAuthoritativeSameFrame { get; }
        public bool PreviousAuthoritativeReferenceEligible { get; }
        public double AuthoritativeYawDeltaDegrees { get; }
        public double ViewCurrentYawResidualDegrees { get; }
        public double EntityRawCurrentYawResidualDegrees { get; }
        public double? EntityPreviousAuthoritativeYawResidualDegrees { get; }
        public double EntityPhaseAdjustedYawResidualDegrees { get; }
        public double EntityRawLagBoundDegrees { get; }
        public double EntityRawLagExcessDegrees { get; }
        public long? EntityYawAuthorityAgeSteps { get; }
        public bool PhaseLagObserved { get; }
        public bool PhaseLagPermitted { get; }
        public bool PhaseLagViolation { get; }
        public bool RecoveryRequiredBeforeSample { get; }
        public bool RecoveryUpdateObserved { get; }
        public bool RecoverySatisfied { get; }
        public bool RecoveryViolation { get; }
        public bool RecoveryPendingAfterSample { get; }
        public bool StationaryAuthority { get; }
        public bool StationaryYawCorrectionViolation { get; }
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
            : this(
                sequence,
                phase,
                preCorrectionPositionResidualWorldUnits,
                new MovementYawPhaseObservation(
                    sequence,
                    sequence,
                    phase,
                    sequence,
                    0.0d,
                    0.0d,
                    0.0d,
                    null,
                    null,
                    null,
                    null,
                    false,
                    false,
                    0.0d,
                    preCorrectionRotationResidualDegrees,
                    preCorrectionRotationResidualDegrees,
                    null,
                    preCorrectionRotationResidualDegrees,
                    preCorrectionRotationResidualDegrees,
                    0.0d,
                    preCorrectionRotationResidualDegrees == 0.0d ? (long?)0L : null,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false),
                preCorrectionRotationResidualDegrees,
                postCorrectionPositionResidualWorldUnits,
                postCorrectionRotationResidualDegrees,
                postCorrectionRotationResidualDegrees)
        {
        }

        public MovementSynchronizationSample(
            long sequence,
            MovementSynchronizationPhase phase,
            double preCorrectionPositionResidualWorldUnits,
            MovementYawPhaseObservation yaw,
            double preCorrectionFullViewCurrentRotationResidualDegrees,
            double postCorrectionPositionResidualWorldUnits,
            double postCorrectionViewCurrentYawResidualDegrees,
            double postCorrectionEntityCurrentYawResidualDegrees)
        {
            if (sequence < 0) { throw new ArgumentOutOfRangeException(nameof(sequence)); }
            if (phase != MovementSynchronizationPhase.InitialConfiguration && phase != MovementSynchronizationPhase.Update && phase != MovementSynchronizationPhase.LateUpdate)
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }
            if (yaw == null) { throw new ArgumentNullException(nameof(yaw)); }
            if (yaw.Phase != phase) { throw new ArgumentException("Yaw observation phase differs from synchronization phase.", nameof(yaw)); }
            RequireFiniteNonNegative(preCorrectionPositionResidualWorldUnits, nameof(preCorrectionPositionResidualWorldUnits));
            RequireFiniteNonNegative(postCorrectionPositionResidualWorldUnits, nameof(postCorrectionPositionResidualWorldUnits));
            RequireFiniteNonNegative(preCorrectionFullViewCurrentRotationResidualDegrees, nameof(preCorrectionFullViewCurrentRotationResidualDegrees));
            RequireFiniteNonNegative(postCorrectionViewCurrentYawResidualDegrees, nameof(postCorrectionViewCurrentYawResidualDegrees));
            RequireFiniteNonNegative(postCorrectionEntityCurrentYawResidualDegrees, nameof(postCorrectionEntityCurrentYawResidualDegrees));

            Sequence = sequence;
            Phase = phase;
            Yaw = yaw;
            PreCorrectionPositionResidualWorldUnits = preCorrectionPositionResidualWorldUnits;
            PreCorrectionFullViewCurrentRotationResidualDegrees = preCorrectionFullViewCurrentRotationResidualDegrees;
            PreCorrectionRotationResidualDegrees = Math.Max(preCorrectionFullViewCurrentRotationResidualDegrees, yaw.EntityPhaseAdjustedYawResidualDegrees);
            PreCorrectionRawCurrentRotationResidualDegrees = Math.Max(preCorrectionFullViewCurrentRotationResidualDegrees, yaw.EntityRawCurrentYawResidualDegrees);
            PostCorrectionPositionResidualWorldUnits = postCorrectionPositionResidualWorldUnits;
            PostCorrectionViewCurrentYawResidualDegrees = postCorrectionViewCurrentYawResidualDegrees;
            PostCorrectionEntityCurrentYawResidualDegrees = postCorrectionEntityCurrentYawResidualDegrees;
            PostCorrectionRotationResidualDegrees = Math.Max(postCorrectionViewCurrentYawResidualDegrees, postCorrectionEntityCurrentYawResidualDegrees);
            CorrectionRequired = preCorrectionPositionResidualWorldUnits > 0.0d || PreCorrectionRawCurrentRotationResidualDegrees > 0.0d;
        }

        public long Sequence { get; }

        public MovementSynchronizationPhase Phase { get; }

        public MovementYawPhaseObservation Yaw { get; }

        public double PreCorrectionPositionResidualWorldUnits { get; }

        public double PreCorrectionRotationResidualDegrees { get; }

        public double PreCorrectionRawCurrentRotationResidualDegrees { get; }

        public double PreCorrectionFullViewCurrentRotationResidualDegrees { get; }

        public double PostCorrectionPositionResidualWorldUnits { get; }

        public double PostCorrectionRotationResidualDegrees { get; }

        public double PostCorrectionViewCurrentYawResidualDegrees { get; }

        public double PostCorrectionEntityCurrentYawResidualDegrees { get; }

        public bool CorrectionRequired { get; }

        private static void RequireFiniteNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0d)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    public sealed class MovementSynchronizationBoundarySnapshot
    {
        public MovementSynchronizationBoundarySnapshot(
            double positionResidualWorldUnits,
            double fullViewCurrentRotationResidualDegrees,
            double viewCurrentYawResidualDegrees,
            double entityCurrentYawResidualDegrees,
            double mountEntityRootYawResidualDegrees)
        {
            RequireFiniteNonNegative(positionResidualWorldUnits, nameof(positionResidualWorldUnits));
            RequireFiniteNonNegative(fullViewCurrentRotationResidualDegrees, nameof(fullViewCurrentRotationResidualDegrees));
            RequireFiniteNonNegative(viewCurrentYawResidualDegrees, nameof(viewCurrentYawResidualDegrees));
            RequireFiniteNonNegative(entityCurrentYawResidualDegrees, nameof(entityCurrentYawResidualDegrees));
            RequireFiniteNonNegative(mountEntityRootYawResidualDegrees, nameof(mountEntityRootYawResidualDegrees));
            PositionResidualWorldUnits = positionResidualWorldUnits;
            FullViewCurrentRotationResidualDegrees = fullViewCurrentRotationResidualDegrees;
            ViewCurrentYawResidualDegrees = viewCurrentYawResidualDegrees;
            EntityCurrentYawResidualDegrees = entityCurrentYawResidualDegrees;
            MountEntityRootYawResidualDegrees = mountEntityRootYawResidualDegrees;
        }

        public double PositionResidualWorldUnits { get; }
        public double FullViewCurrentRotationResidualDegrees { get; }
        public double ViewCurrentYawResidualDegrees { get; }
        public double EntityCurrentYawResidualDegrees { get; }
        public double MountEntityRootYawResidualDegrees { get; }

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

        public double LatestPreCorrectionFullViewCurrentRotationResidualDegrees { get; private set; }

        public double LatestPostCorrectionPositionResidualWorldUnits { get; private set; }

        public double LatestPostCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumPreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumPreCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumPostCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumPostCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumInitialConfigurationPreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumUpdatePreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumUpdatePreCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumUpdatePostCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumUpdatePostCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumLateUpdatePreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumLateUpdatePreCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumLateUpdatePostCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumLateUpdatePostCorrectionRotationResidualDegrees { get; private set; }

        public MovementYawPhaseObservation LatestYawObservation { get; private set; }

        public double MaximumCalibratedViewCurrentYawResidualDegrees { get; private set; }

        public double MaximumCalibratedFullViewCurrentRotationResidualDegrees { get; private set; }

        public double MaximumCalibratedMountEntityRootYawResidualDegrees { get; private set; }

        public double MaximumCalibratedEntityRawCurrentYawResidualDegrees { get; private set; }

        public double MaximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees { get; private set; }

        public double MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees { get; private set; }

        public double MaximumAuthoritativeYawDeltaDegrees { get; private set; }

        public double MaximumEntityRawLagExcessDegrees { get; private set; }

        public long PhaseLagObservedCount { get; private set; }

        public long PhaseLagPermittedCount { get; private set; }

        public long PhaseLagSameFrameUpdateReferenceCount { get; private set; }

        public long PhaseLagEligibleReferenceCount { get; private set; }

        public long PhaseLagViolationCount { get; private set; }

        public long PhaseLagRecoveryRequiredCount { get; private set; }

        public long PhaseLagRecoveryUpdateCount { get; private set; }

        public long PhaseLagRecoverySatisfiedCount { get; private set; }

        public long PhaseLagRecoveryViolationCount { get; private set; }

        public long StationaryYawCorrectionViolationCount { get; private set; }

        public long OutstandingPhaseLagRecoveryCount { get; private set; }

        public long MaximumConsecutiveUnrecoveredPhaseLagCount { get; private set; }

        public void Observe(MovementSynchronizationSample sample)
        {
            if (sample == null) { throw new ArgumentNullException(nameof(sample)); }
            if (sample.Sequence != SampleCount)
            {
                throw new InvalidOperationException("Movement synchronization sample sequence is not contiguous.");
            }

            SampleCount++;
            LatestPhase = sample.Phase;
            LatestYawObservation = sample.Yaw;
            LatestPreCorrectionPositionResidualWorldUnits = sample.PreCorrectionPositionResidualWorldUnits;
            LatestPreCorrectionRotationResidualDegrees = sample.PreCorrectionRotationResidualDegrees;
            LatestPreCorrectionFullViewCurrentRotationResidualDegrees = sample.PreCorrectionFullViewCurrentRotationResidualDegrees;
            LatestPostCorrectionPositionResidualWorldUnits = sample.PostCorrectionPositionResidualWorldUnits;
            LatestPostCorrectionRotationResidualDegrees = sample.PostCorrectionRotationResidualDegrees;
            MaximumPreCorrectionPositionResidualWorldUnits = Math.Max(MaximumPreCorrectionPositionResidualWorldUnits, sample.PreCorrectionPositionResidualWorldUnits);
            MaximumPreCorrectionRotationResidualDegrees = Math.Max(MaximumPreCorrectionRotationResidualDegrees, sample.PreCorrectionRotationResidualDegrees);
            MaximumPostCorrectionPositionResidualWorldUnits = Math.Max(MaximumPostCorrectionPositionResidualWorldUnits, sample.PostCorrectionPositionResidualWorldUnits);
            MaximumPostCorrectionRotationResidualDegrees = Math.Max(MaximumPostCorrectionRotationResidualDegrees, sample.PostCorrectionRotationResidualDegrees);

            if (sample.Phase == MovementSynchronizationPhase.Update || sample.Phase == MovementSynchronizationPhase.LateUpdate)
            {
                MaximumCalibratedViewCurrentYawResidualDegrees = Math.Max(MaximumCalibratedViewCurrentYawResidualDegrees, sample.Yaw.ViewCurrentYawResidualDegrees);
                MaximumCalibratedFullViewCurrentRotationResidualDegrees = Math.Max(
                    MaximumCalibratedFullViewCurrentRotationResidualDegrees,
                    sample.PreCorrectionFullViewCurrentRotationResidualDegrees);
                MaximumCalibratedMountEntityRootYawResidualDegrees = Math.Max(
                    MaximumCalibratedMountEntityRootYawResidualDegrees,
                    sample.Yaw.MountEntityRootYawResidualDegrees);
                MaximumCalibratedEntityRawCurrentYawResidualDegrees = Math.Max(MaximumCalibratedEntityRawCurrentYawResidualDegrees, sample.Yaw.EntityRawCurrentYawResidualDegrees);
                if (sample.Yaw.EntityPreviousAuthoritativeYawResidualDegrees.HasValue)
                {
                    MaximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees = Math.Max(
                        MaximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees,
                        sample.Yaw.EntityPreviousAuthoritativeYawResidualDegrees.Value);
                }
                MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees = Math.Max(
                    MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees,
                    sample.Yaw.EntityPhaseAdjustedYawResidualDegrees);
                MaximumAuthoritativeYawDeltaDegrees = Math.Max(MaximumAuthoritativeYawDeltaDegrees, sample.Yaw.AuthoritativeYawDeltaDegrees);
                MaximumEntityRawLagExcessDegrees = Math.Max(MaximumEntityRawLagExcessDegrees, sample.Yaw.EntityRawLagExcessDegrees);
                if (sample.Yaw.PhaseLagObserved) { PhaseLagObservedCount++; }
                if (sample.Yaw.PhaseLagObserved && sample.Yaw.PreviousAuthoritativeSameFrame &&
                    sample.Yaw.PreviousAuthoritativePhase == MovementSynchronizationPhase.Update)
                {
                    PhaseLagSameFrameUpdateReferenceCount++;
                }
                if (sample.Yaw.PhaseLagObserved && sample.Yaw.PreviousAuthoritativeReferenceEligible)
                {
                    PhaseLagEligibleReferenceCount++;
                }
                if (sample.Yaw.PhaseLagPermitted)
                {
                    PhaseLagPermittedCount++;
                    OutstandingPhaseLagRecoveryCount++;
                    MaximumConsecutiveUnrecoveredPhaseLagCount = Math.Max(MaximumConsecutiveUnrecoveredPhaseLagCount, OutstandingPhaseLagRecoveryCount);
                }
                if (sample.Yaw.PhaseLagViolation) { PhaseLagViolationCount++; }
                if (sample.Yaw.RecoveryRequiredBeforeSample) { PhaseLagRecoveryRequiredCount++; }
                if (sample.Yaw.RecoveryUpdateObserved) { PhaseLagRecoveryUpdateCount++; }
                if (sample.Yaw.RecoverySatisfied)
                {
                    PhaseLagRecoverySatisfiedCount++;
                    if (OutstandingPhaseLagRecoveryCount > 0L) { OutstandingPhaseLagRecoveryCount--; }
                }
                if (sample.Yaw.RecoveryViolation) { PhaseLagRecoveryViolationCount++; }
                if (sample.Yaw.StationaryYawCorrectionViolation) { StationaryYawCorrectionViolationCount++; }
            }

            switch (sample.Phase)
            {
                case MovementSynchronizationPhase.InitialConfiguration:
                    InitialConfigurationSampleCount++;
                    if (sample.CorrectionRequired) { InitialConfigurationCorrectionCount++; }
                    MaximumInitialConfigurationPreCorrectionPositionResidualWorldUnits = Math.Max(MaximumInitialConfigurationPreCorrectionPositionResidualWorldUnits, sample.PreCorrectionPositionResidualWorldUnits);
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

    /// <summary>
    /// Pure qualification of the synchronization loop. Initial placement is
    /// deliberately excluded from the calibrated pre-correction gate: it is a
    /// one-time mount transition, not evidence about steady-state drift.
    /// </summary>
    public sealed class MovementSynchronizationQualification
    {
        private MovementSynchronizationQualification()
        {
        }

        public double MaximumCalibratedPreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumCalibratedPreCorrectionRotationResidualDegrees { get; private set; }

        public bool PreCorrectionPositionPassed { get; private set; }

        public bool PreCorrectionRotationPassed { get; private set; }

        public bool PhaseOrderYawPassed { get; private set; }

        public bool PhaseOrderYawSafetyPassed { get; private set; }

        public bool PostCorrectionPositionPassed { get; private set; }

        public bool PostCorrectionRotationPassed { get; private set; }

        public bool CorrectionCadencePassed { get; private set; }

        public long MaximumSamplesPerPhase { get; private set; }

        public long MaximumCorrectionsAcrossCalibratedPhases { get; private set; }

        public static MovementSynchronizationQualification Evaluate(
            MovementSynchronizationTelemetryAccumulator telemetry,
            long observedUpdateFrames,
            double maximumPositionResidualWorldUnits,
            double maximumRotationResidualDegrees)
        {
            if (telemetry == null) { throw new ArgumentNullException(nameof(telemetry)); }
            if (observedUpdateFrames < 0) { throw new ArgumentOutOfRangeException(nameof(observedUpdateFrames)); }
            RequireFinitePositive(maximumPositionResidualWorldUnits, nameof(maximumPositionResidualWorldUnits));
            RequireFinitePositive(maximumRotationResidualDegrees, nameof(maximumRotationResidualDegrees));

            // Update and LateUpdate can each run once after the last engine
            // observation because of Unity callback ordering. A second frame of
            // slack covers the mount-configuration frame without making an
            // unbounded extra synchronization loop pass qualification.
            var maximumSamplesPerPhase = checked(observedUpdateFrames + 2L);
            var maximumCorrections = checked(maximumSamplesPerPhase * 2L);
            var calibratedPreCorrectionMaximum = Math.Max(
                telemetry.MaximumUpdatePreCorrectionPositionResidualWorldUnits,
                telemetry.MaximumLateUpdatePreCorrectionPositionResidualWorldUnits);
            var calibratedPreCorrectionRotationMaximum = Math.Max(
                telemetry.MaximumUpdatePreCorrectionRotationResidualDegrees,
                telemetry.MaximumLateUpdatePreCorrectionRotationResidualDegrees);
            var calibratedCorrectionCount = checked(telemetry.UpdateCorrectionCount + telemetry.LateUpdateCorrectionCount);
            var phaseOrderYawSafetyPassed = calibratedPreCorrectionRotationMaximum <= maximumRotationResidualDegrees &&
                telemetry.MaximumCalibratedViewCurrentYawResidualDegrees <= maximumRotationResidualDegrees &&
                telemetry.MaximumCalibratedFullViewCurrentRotationResidualDegrees <= maximumRotationResidualDegrees &&
                telemetry.MaximumCalibratedMountEntityRootYawResidualDegrees <= maximumRotationResidualDegrees &&
                telemetry.MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees <= maximumRotationResidualDegrees &&
                telemetry.MaximumEntityRawLagExcessDegrees <= MovementYawPhaseTracker.RawLagArithmeticCoherenceEpsilonDegrees &&
                telemetry.PhaseLagViolationCount == 0L &&
                telemetry.PhaseLagRecoveryViolationCount == 0L &&
                telemetry.StationaryYawCorrectionViolationCount == 0L &&
                telemetry.MaximumConsecutiveUnrecoveredPhaseLagCount <= 1L &&
                telemetry.OutstandingPhaseLagRecoveryCount <= 1L &&
                telemetry.PhaseLagObservedCount == telemetry.PhaseLagPermittedCount &&
                telemetry.PhaseLagPermittedCount == telemetry.PhaseLagSameFrameUpdateReferenceCount &&
                telemetry.PhaseLagPermittedCount == telemetry.PhaseLagEligibleReferenceCount;
            var phaseOrderYawPassed = phaseOrderYawSafetyPassed &&
                telemetry.OutstandingPhaseLagRecoveryCount == 0L &&
                telemetry.PhaseLagPermittedCount == telemetry.PhaseLagRecoveryRequiredCount &&
                telemetry.PhaseLagPermittedCount == telemetry.PhaseLagRecoveryUpdateCount &&
                telemetry.PhaseLagPermittedCount == telemetry.PhaseLagRecoverySatisfiedCount;

            return new MovementSynchronizationQualification
            {
                MaximumCalibratedPreCorrectionPositionResidualWorldUnits = calibratedPreCorrectionMaximum,
                MaximumCalibratedPreCorrectionRotationResidualDegrees = calibratedPreCorrectionRotationMaximum,
                PreCorrectionPositionPassed = calibratedPreCorrectionMaximum <= maximumPositionResidualWorldUnits,
                PreCorrectionRotationPassed = phaseOrderYawPassed,
                PhaseOrderYawPassed = phaseOrderYawPassed,
                PhaseOrderYawSafetyPassed = phaseOrderYawSafetyPassed,
                PostCorrectionPositionPassed = telemetry.MaximumPostCorrectionPositionResidualWorldUnits <= maximumPositionResidualWorldUnits,
                PostCorrectionRotationPassed = telemetry.MaximumPostCorrectionRotationResidualDegrees <= maximumRotationResidualDegrees,
                CorrectionCadencePassed = telemetry.UpdateSampleCount <= maximumSamplesPerPhase &&
                    telemetry.LateUpdateSampleCount <= maximumSamplesPerPhase &&
                    telemetry.UpdateCorrectionCount <= telemetry.UpdateSampleCount &&
                    telemetry.LateUpdateCorrectionCount <= telemetry.LateUpdateSampleCount &&
                    calibratedCorrectionCount <= maximumCorrections,
                MaximumSamplesPerPhase = maximumSamplesPerPhase,
                MaximumCorrectionsAcrossCalibratedPhases = maximumCorrections
            };
        }

        private static void RequireFinitePositive(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0d)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
