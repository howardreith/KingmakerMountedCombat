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
    /// Tracks the same bounded Update/LateUpdate ordering contract as the yaw
    /// tracker, but for the authoritative rider anchor position.  The parented
    /// rider view must always be current.  Only the logical rider entity may
    /// retain the immediately preceding same-frame Update anchor position, and
    /// the next Update must observe it current again.
    /// </summary>
    public sealed class MovementPositionPhaseTracker
    {
        public const double StationaryAuthorityEpsilonWorldUnits = 0.000001d;
        public const double RawLagArithmeticCoherenceEpsilonWorldUnits = 0.0001d;
        private bool hasObservedAuthority;
        private double lastObservedAuthoritativeX;
        private double lastObservedAuthoritativeY;
        private double lastObservedAuthoritativeZ;
        private long authoritativePositionSequence;
        private bool hasUpdateReference;
        private long updateReferenceFrame;
        private double updateReferenceX;
        private double updateReferenceY;
        private double updateReferenceZ;
        private long updateReferenceAuthoritySequence;
        private bool recoveryPending;

        public long ObservationCount { get; private set; }

        public bool RecoveryPending => recoveryPending;

        internal bool ClosePendingRecoveryAtStationaryBoundary()
        {
            if (!recoveryPending) { return false; }
            recoveryPending = false;
            return true;
        }

        public MovementPositionPhaseObservation Observe(
            long frame,
            MovementSynchronizationPhase phase,
            double currentAuthoritativeX,
            double currentAuthoritativeY,
            double currentAuthoritativeZ,
            double riderViewX,
            double riderViewY,
            double riderViewZ,
            double riderEntityX,
            double riderEntityY,
            double riderEntityZ,
            double maximumPositionResidualWorldUnits)
        {
            if (frame < 0) { throw new ArgumentOutOfRangeException(nameof(frame)); }
            if (phase != MovementSynchronizationPhase.InitialConfiguration && phase != MovementSynchronizationPhase.Update && phase != MovementSynchronizationPhase.LateUpdate)
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }
            RequireFinite(currentAuthoritativeX, nameof(currentAuthoritativeX));
            RequireFinite(currentAuthoritativeY, nameof(currentAuthoritativeY));
            RequireFinite(currentAuthoritativeZ, nameof(currentAuthoritativeZ));
            RequireFinite(riderViewX, nameof(riderViewX));
            RequireFinite(riderViewY, nameof(riderViewY));
            RequireFinite(riderViewZ, nameof(riderViewZ));
            RequireFinite(riderEntityX, nameof(riderEntityX));
            RequireFinite(riderEntityY, nameof(riderEntityY));
            RequireFinite(riderEntityZ, nameof(riderEntityZ));
            if (double.IsNaN(maximumPositionResidualWorldUnits) || double.IsInfinity(maximumPositionResidualWorldUnits) || maximumPositionResidualWorldUnits <= 0.0d)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumPositionResidualWorldUnits));
            }

            var authoritativePositionDeltaWorldUnits = hasObservedAuthority
                ? MovementTelemetrySample.CalculateDistance(
                    lastObservedAuthoritativeX, lastObservedAuthoritativeY, lastObservedAuthoritativeZ,
                    currentAuthoritativeX, currentAuthoritativeY, currentAuthoritativeZ)
                : 0.0d;
            if (!hasObservedAuthority)
            {
                hasObservedAuthority = true;
            }
            else if (authoritativePositionDeltaWorldUnits > StationaryAuthorityEpsilonWorldUnits)
            {
                authoritativePositionSequence++;
            }

            var calibrated = phase == MovementSynchronizationPhase.Update || phase == MovementSynchronizationPhase.LateUpdate;
            var viewCurrentPositionResidualWorldUnits = MovementTelemetrySample.CalculateDistance(
                currentAuthoritativeX, currentAuthoritativeY, currentAuthoritativeZ,
                riderViewX, riderViewY, riderViewZ);
            var entityRawCurrentPositionResidualWorldUnits = MovementTelemetrySample.CalculateDistance(
                currentAuthoritativeX, currentAuthoritativeY, currentAuthoritativeZ,
                riderEntityX, riderEntityY, riderEntityZ);
            var previousReferenceAvailable = phase == MovementSynchronizationPhase.LateUpdate && hasUpdateReference;
            var previousReferenceFrame = previousReferenceAvailable ? (long?)updateReferenceFrame : null;
            var previousReferencePhase = previousReferenceAvailable ? (MovementSynchronizationPhase?)MovementSynchronizationPhase.Update : null;
            var previousReferenceAuthoritySequence = previousReferenceAvailable ? (long?)updateReferenceAuthoritySequence : null;
            var entityPreviousAuthoritativePositionResidualWorldUnits = previousReferenceAvailable
                ? (double?)MovementTelemetrySample.CalculateDistance(
                    updateReferenceX, updateReferenceY, updateReferenceZ,
                    riderEntityX, riderEntityY, riderEntityZ)
                : null;
            var authorityAgeSteps = previousReferenceAvailable
                ? authoritativePositionSequence - updateReferenceAuthoritySequence
                : 0L;
            var previousReferenceSameFrame = previousReferenceAvailable && updateReferenceFrame == frame;
            var previousReferenceEligible = previousReferenceSameFrame && authorityAgeSteps == 1L;
            var entityPositionAuthorityAgeSteps = entityRawCurrentPositionResidualWorldUnits <= maximumPositionResidualWorldUnits
                ? (long?)0L
                : entityPreviousAuthoritativePositionResidualWorldUnits.HasValue &&
                    entityPreviousAuthoritativePositionResidualWorldUnits.Value <= maximumPositionResidualWorldUnits
                    ? (long?)authorityAgeSteps
                    : null;
            var phaseLagObserved = calibrated && entityRawCurrentPositionResidualWorldUnits > maximumPositionResidualWorldUnits;
            var entityRawLagBoundWorldUnits = authoritativePositionDeltaWorldUnits;
            var entityRawLagExcessWorldUnits = Math.Max(0.0d, entityRawCurrentPositionResidualWorldUnits - entityRawLagBoundWorldUnits);
            var recoveryRequiredBeforeSample = recoveryPending;
            var recoveryUpdateObserved = phase == MovementSynchronizationPhase.Update && recoveryRequiredBeforeSample;
            var recoverySatisfied = recoveryUpdateObserved &&
                viewCurrentPositionResidualWorldUnits <= maximumPositionResidualWorldUnits &&
                entityRawCurrentPositionResidualWorldUnits <= maximumPositionResidualWorldUnits;
            var recoveryViolation = recoveryRequiredBeforeSample &&
                (phase == MovementSynchronizationPhase.Update
                    ? !recoverySatisfied
                    : phase != MovementSynchronizationPhase.LateUpdate ||
                      viewCurrentPositionResidualWorldUnits > maximumPositionResidualWorldUnits ||
                      entityRawCurrentPositionResidualWorldUnits > maximumPositionResidualWorldUnits);
            if (phase == MovementSynchronizationPhase.Update && recoveryRequiredBeforeSample)
            {
                recoveryPending = false;
            }

            var phaseLagPermitted = phaseLagObserved &&
                phase == MovementSynchronizationPhase.LateUpdate &&
                previousReferenceEligible &&
                entityPreviousAuthoritativePositionResidualWorldUnits.HasValue &&
                entityPreviousAuthoritativePositionResidualWorldUnits.Value <= maximumPositionResidualWorldUnits &&
                viewCurrentPositionResidualWorldUnits <= maximumPositionResidualWorldUnits &&
                entityRawLagExcessWorldUnits <= RawLagArithmeticCoherenceEpsilonWorldUnits &&
                !recoveryRequiredBeforeSample;
            var phaseLagViolation = phaseLagObserved && !phaseLagPermitted;
            var entityPhaseAdjustedPositionResidualWorldUnits = phaseLagPermitted
                ? Math.Min(entityRawCurrentPositionResidualWorldUnits, entityPreviousAuthoritativePositionResidualWorldUnits.Value)
                : entityRawCurrentPositionResidualWorldUnits;
            var stationaryAuthority = calibrated && authoritativePositionDeltaWorldUnits <= StationaryAuthorityEpsilonWorldUnits;
            var stationaryPositionCorrectionViolation = stationaryAuthority &&
                (viewCurrentPositionResidualWorldUnits > maximumPositionResidualWorldUnits ||
                 entityRawCurrentPositionResidualWorldUnits > maximumPositionResidualWorldUnits);

            if (phaseLagPermitted)
            {
                recoveryPending = true;
            }

            var observation = new MovementPositionPhaseObservation(
                ObservationCount++,
                frame,
                phase,
                authoritativePositionSequence,
                currentAuthoritativeX,
                currentAuthoritativeY,
                currentAuthoritativeZ,
                previousReferenceAuthoritySequence,
                previousReferenceAvailable ? (double?)updateReferenceX : null,
                previousReferenceAvailable ? (double?)updateReferenceY : null,
                previousReferenceAvailable ? (double?)updateReferenceZ : null,
                previousReferenceFrame,
                previousReferencePhase,
                previousReferenceSameFrame,
                previousReferenceEligible,
                authoritativePositionDeltaWorldUnits,
                viewCurrentPositionResidualWorldUnits,
                entityRawCurrentPositionResidualWorldUnits,
                entityPreviousAuthoritativePositionResidualWorldUnits,
                entityPhaseAdjustedPositionResidualWorldUnits,
                entityRawLagBoundWorldUnits,
                entityRawLagExcessWorldUnits,
                entityPositionAuthorityAgeSteps,
                phaseLagObserved,
                phaseLagPermitted,
                phaseLagViolation,
                recoveryRequiredBeforeSample,
                recoveryUpdateObserved,
                recoverySatisfied,
                recoveryViolation,
                recoveryPending,
                stationaryAuthority,
                stationaryPositionCorrectionViolation);

            if (phase == MovementSynchronizationPhase.Update)
            {
                hasUpdateReference = true;
                updateReferenceFrame = frame;
                updateReferenceX = currentAuthoritativeX;
                updateReferenceY = currentAuthoritativeY;
                updateReferenceZ = currentAuthoritativeZ;
                updateReferenceAuthoritySequence = authoritativePositionSequence;
            }
            lastObservedAuthoritativeX = currentAuthoritativeX;
            lastObservedAuthoritativeY = currentAuthoritativeY;
            lastObservedAuthoritativeZ = currentAuthoritativeZ;
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

    public sealed class MovementPositionPhaseObservation
    {
        internal MovementPositionPhaseObservation(
            long sequence,
            long frame,
            MovementSynchronizationPhase phase,
            long authoritativePositionSequence,
            double currentAuthoritativeX,
            double currentAuthoritativeY,
            double currentAuthoritativeZ,
            long? previousAuthoritativePositionSequence,
            double? previousAuthoritativeX,
            double? previousAuthoritativeY,
            double? previousAuthoritativeZ,
            long? previousAuthoritativeFrame,
            MovementSynchronizationPhase? previousAuthoritativePhase,
            bool previousAuthoritativeSameFrame,
            bool previousAuthoritativeReferenceEligible,
            double authoritativePositionDeltaWorldUnits,
            double viewCurrentPositionResidualWorldUnits,
            double entityRawCurrentPositionResidualWorldUnits,
            double? entityPreviousAuthoritativePositionResidualWorldUnits,
            double entityPhaseAdjustedPositionResidualWorldUnits,
            double entityRawLagBoundWorldUnits,
            double entityRawLagExcessWorldUnits,
            long? entityPositionAuthorityAgeSteps,
            bool phaseLagObserved,
            bool phaseLagPermitted,
            bool phaseLagViolation,
            bool recoveryRequiredBeforeSample,
            bool recoveryUpdateObserved,
            bool recoverySatisfied,
            bool recoveryViolation,
            bool recoveryPendingAfterSample,
            bool stationaryAuthority,
            bool stationaryPositionCorrectionViolation)
        {
            Sequence = sequence;
            Frame = frame;
            Phase = phase;
            AuthoritativePositionSequence = authoritativePositionSequence;
            CurrentAuthoritativeX = currentAuthoritativeX;
            CurrentAuthoritativeY = currentAuthoritativeY;
            CurrentAuthoritativeZ = currentAuthoritativeZ;
            PreviousAuthoritativePositionSequence = previousAuthoritativePositionSequence;
            PreviousAuthoritativeX = previousAuthoritativeX;
            PreviousAuthoritativeY = previousAuthoritativeY;
            PreviousAuthoritativeZ = previousAuthoritativeZ;
            PreviousAuthoritativeFrame = previousAuthoritativeFrame;
            PreviousAuthoritativePhase = previousAuthoritativePhase;
            PreviousAuthoritativeSameFrame = previousAuthoritativeSameFrame;
            PreviousAuthoritativeReferenceEligible = previousAuthoritativeReferenceEligible;
            AuthoritativePositionDeltaWorldUnits = authoritativePositionDeltaWorldUnits;
            ViewCurrentPositionResidualWorldUnits = viewCurrentPositionResidualWorldUnits;
            EntityRawCurrentPositionResidualWorldUnits = entityRawCurrentPositionResidualWorldUnits;
            EntityPreviousAuthoritativePositionResidualWorldUnits = entityPreviousAuthoritativePositionResidualWorldUnits;
            EntityPhaseAdjustedPositionResidualWorldUnits = entityPhaseAdjustedPositionResidualWorldUnits;
            EntityRawLagBoundWorldUnits = entityRawLagBoundWorldUnits;
            EntityRawLagExcessWorldUnits = entityRawLagExcessWorldUnits;
            EntityPositionAuthorityAgeSteps = entityPositionAuthorityAgeSteps;
            PhaseLagObserved = phaseLagObserved;
            PhaseLagPermitted = phaseLagPermitted;
            PhaseLagViolation = phaseLagViolation;
            RecoveryRequiredBeforeSample = recoveryRequiredBeforeSample;
            RecoveryUpdateObserved = recoveryUpdateObserved;
            RecoverySatisfied = recoverySatisfied;
            RecoveryViolation = recoveryViolation;
            RecoveryPendingAfterSample = recoveryPendingAfterSample;
            StationaryAuthority = stationaryAuthority;
            StationaryPositionCorrectionViolation = stationaryPositionCorrectionViolation;
        }

        internal static MovementPositionPhaseObservation CreateSynthetic(
            long sequence,
            MovementSynchronizationPhase phase,
            double positionResidualWorldUnits)
        {
            return new MovementPositionPhaseObservation(
                sequence, sequence, phase, sequence,
                0.0d, 0.0d, 0.0d,
                null, null, null, null, null, null,
                false, false, 0.0d,
                positionResidualWorldUnits,
                positionResidualWorldUnits,
                null,
                positionResidualWorldUnits,
                positionResidualWorldUnits,
                0.0d,
                positionResidualWorldUnits == 0.0d ? (long?)0L : null,
                false, false, false, false, false, false, false, false, false, false);
        }

        public long Sequence { get; }
        public long Frame { get; }
        public MovementSynchronizationPhase Phase { get; }
        public long AuthoritativePositionSequence { get; }
        public double CurrentAuthoritativeX { get; }
        public double CurrentAuthoritativeY { get; }
        public double CurrentAuthoritativeZ { get; }
        public long? PreviousAuthoritativePositionSequence { get; }
        public double? PreviousAuthoritativeX { get; }
        public double? PreviousAuthoritativeY { get; }
        public double? PreviousAuthoritativeZ { get; }
        public long? PreviousAuthoritativeFrame { get; }
        public MovementSynchronizationPhase? PreviousAuthoritativePhase { get; }
        public bool PreviousAuthoritativeSameFrame { get; }
        public bool PreviousAuthoritativeReferenceEligible { get; }
        public double AuthoritativePositionDeltaWorldUnits { get; }
        public double ViewCurrentPositionResidualWorldUnits { get; }
        public double EntityRawCurrentPositionResidualWorldUnits { get; }
        public double? EntityPreviousAuthoritativePositionResidualWorldUnits { get; }
        public double EntityPhaseAdjustedPositionResidualWorldUnits { get; }
        public double EntityRawLagBoundWorldUnits { get; }
        public double EntityRawLagExcessWorldUnits { get; }
        public long? EntityPositionAuthorityAgeSteps { get; }
        public bool PhaseLagObserved { get; }
        public bool PhaseLagPermitted { get; }
        public bool PhaseLagViolation { get; }
        public bool RecoveryRequiredBeforeSample { get; }
        public bool RecoveryUpdateObserved { get; }
        public bool RecoverySatisfied { get; }
        public bool RecoveryViolation { get; }
        public bool RecoveryPendingAfterSample { get; }
        public bool StationaryAuthority { get; }
        public bool StationaryPositionCorrectionViolation { get; }
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
        public const double StationaryAuthorityEpsilonDegrees = 0.000001d;
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

        public bool RecoveryPending => recoveryPending;

        internal bool ClosePendingRecoveryAtStationaryBoundary()
        {
            if (!recoveryPending) { return false; }
            recoveryPending = false;
            return true;
        }

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
            else if (authoritativeYawDeltaDegrees > StationaryAuthorityEpsilonDegrees)
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
                (phase == MovementSynchronizationPhase.Update
                    ? !recoverySatisfied
                    : phase != MovementSynchronizationPhase.LateUpdate ||
                      viewCurrentYawResidualDegrees > maximumYawResidualDegrees ||
                      entityRawCurrentYawResidualDegrees > maximumYawResidualDegrees);
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
            var stationaryAuthority = calibrated && authoritativeYawDeltaDegrees <= StationaryAuthorityEpsilonDegrees;
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
                MovementPositionPhaseObservation.CreateSynthetic(sequence, phase, preCorrectionPositionResidualWorldUnits),
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
            : this(
                sequence,
                phase,
                MovementPositionPhaseObservation.CreateSynthetic(sequence, phase, preCorrectionPositionResidualWorldUnits),
                yaw,
                preCorrectionFullViewCurrentRotationResidualDegrees,
                postCorrectionPositionResidualWorldUnits,
                postCorrectionViewCurrentYawResidualDegrees,
                postCorrectionEntityCurrentYawResidualDegrees)
        {
        }

        public MovementSynchronizationSample(
            long sequence,
            MovementSynchronizationPhase phase,
            MovementPositionPhaseObservation position,
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
            if (position == null) { throw new ArgumentNullException(nameof(position)); }
            if (position.Phase != phase) { throw new ArgumentException("Position observation phase differs from synchronization phase.", nameof(position)); }
            if (yaw == null) { throw new ArgumentNullException(nameof(yaw)); }
            if (yaw.Phase != phase) { throw new ArgumentException("Yaw observation phase differs from synchronization phase.", nameof(yaw)); }
            RequireFiniteNonNegative(postCorrectionPositionResidualWorldUnits, nameof(postCorrectionPositionResidualWorldUnits));
            RequireFiniteNonNegative(preCorrectionFullViewCurrentRotationResidualDegrees, nameof(preCorrectionFullViewCurrentRotationResidualDegrees));
            RequireFiniteNonNegative(postCorrectionViewCurrentYawResidualDegrees, nameof(postCorrectionViewCurrentYawResidualDegrees));
            RequireFiniteNonNegative(postCorrectionEntityCurrentYawResidualDegrees, nameof(postCorrectionEntityCurrentYawResidualDegrees));

            Sequence = sequence;
            Phase = phase;
            Position = position;
            Yaw = yaw;
            PreCorrectionViewCurrentPositionResidualWorldUnits = position.ViewCurrentPositionResidualWorldUnits;
            PreCorrectionPositionResidualWorldUnits = Math.Max(
                position.ViewCurrentPositionResidualWorldUnits,
                position.EntityPhaseAdjustedPositionResidualWorldUnits);
            PreCorrectionRawCurrentPositionResidualWorldUnits = Math.Max(
                position.ViewCurrentPositionResidualWorldUnits,
                position.EntityRawCurrentPositionResidualWorldUnits);
            PreCorrectionFullViewCurrentRotationResidualDegrees = preCorrectionFullViewCurrentRotationResidualDegrees;
            PreCorrectionRotationResidualDegrees = Math.Max(preCorrectionFullViewCurrentRotationResidualDegrees, yaw.EntityPhaseAdjustedYawResidualDegrees);
            PreCorrectionRawCurrentRotationResidualDegrees = Math.Max(preCorrectionFullViewCurrentRotationResidualDegrees, yaw.EntityRawCurrentYawResidualDegrees);
            PostCorrectionPositionResidualWorldUnits = postCorrectionPositionResidualWorldUnits;
            PostCorrectionViewCurrentYawResidualDegrees = postCorrectionViewCurrentYawResidualDegrees;
            PostCorrectionEntityCurrentYawResidualDegrees = postCorrectionEntityCurrentYawResidualDegrees;
            PostCorrectionRotationResidualDegrees = Math.Max(postCorrectionViewCurrentYawResidualDegrees, postCorrectionEntityCurrentYawResidualDegrees);
            CorrectionRequired = PreCorrectionRawCurrentPositionResidualWorldUnits > 0.0d || PreCorrectionRawCurrentRotationResidualDegrees > 0.0d;
        }

        public long Sequence { get; }

        public MovementSynchronizationPhase Phase { get; }

        public MovementPositionPhaseObservation Position { get; }

        public MovementYawPhaseObservation Yaw { get; }

        public double PreCorrectionPositionResidualWorldUnits { get; }

        public double PreCorrectionRawCurrentPositionResidualWorldUnits { get; }

        public double PreCorrectionViewCurrentPositionResidualWorldUnits { get; }

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
            : this(
                positionResidualWorldUnits,
                positionResidualWorldUnits,
                fullViewCurrentRotationResidualDegrees,
                viewCurrentYawResidualDegrees,
                entityCurrentYawResidualDegrees,
                mountEntityRootYawResidualDegrees,
                0.0d,
                0.0d,
                false,
                false,
                false)
        {
        }

        public MovementSynchronizationBoundarySnapshot(
            double viewCurrentPositionResidualWorldUnits,
            double entityCurrentPositionResidualWorldUnits,
            double fullViewCurrentRotationResidualDegrees,
            double viewCurrentYawResidualDegrees,
            double entityCurrentYawResidualDegrees,
            double mountEntityRootYawResidualDegrees,
            double authoritativePositionAdvanceWorldUnits,
            double authoritativeYawAdvanceDegrees,
            bool movementCommandAbsent,
            bool wantsToMove,
            bool isReallyMoving)
        {
            RequireFiniteNonNegative(viewCurrentPositionResidualWorldUnits, nameof(viewCurrentPositionResidualWorldUnits));
            RequireFiniteNonNegative(entityCurrentPositionResidualWorldUnits, nameof(entityCurrentPositionResidualWorldUnits));
            RequireFiniteNonNegative(fullViewCurrentRotationResidualDegrees, nameof(fullViewCurrentRotationResidualDegrees));
            RequireFiniteNonNegative(viewCurrentYawResidualDegrees, nameof(viewCurrentYawResidualDegrees));
            RequireFiniteNonNegative(entityCurrentYawResidualDegrees, nameof(entityCurrentYawResidualDegrees));
            RequireFiniteNonNegative(mountEntityRootYawResidualDegrees, nameof(mountEntityRootYawResidualDegrees));
            RequireFiniteNonNegative(authoritativePositionAdvanceWorldUnits, nameof(authoritativePositionAdvanceWorldUnits));
            RequireFiniteNonNegative(authoritativeYawAdvanceDegrees, nameof(authoritativeYawAdvanceDegrees));
            ViewCurrentPositionResidualWorldUnits = viewCurrentPositionResidualWorldUnits;
            EntityCurrentPositionResidualWorldUnits = entityCurrentPositionResidualWorldUnits;
            PositionResidualWorldUnits = Math.Max(viewCurrentPositionResidualWorldUnits, entityCurrentPositionResidualWorldUnits);
            FullViewCurrentRotationResidualDegrees = fullViewCurrentRotationResidualDegrees;
            ViewCurrentYawResidualDegrees = viewCurrentYawResidualDegrees;
            EntityCurrentYawResidualDegrees = entityCurrentYawResidualDegrees;
            MountEntityRootYawResidualDegrees = mountEntityRootYawResidualDegrees;
            AuthoritativePositionAdvanceWorldUnits = authoritativePositionAdvanceWorldUnits;
            AuthoritativeYawAdvanceDegrees = authoritativeYawAdvanceDegrees;
            MovementCommandAbsent = movementCommandAbsent;
            WantsToMove = wantsToMove;
            IsReallyMoving = isReallyMoving;
        }

        public double PositionResidualWorldUnits { get; }
        public double ViewCurrentPositionResidualWorldUnits { get; }
        public double EntityCurrentPositionResidualWorldUnits { get; }
        public double FullViewCurrentRotationResidualDegrees { get; }
        public double ViewCurrentYawResidualDegrees { get; }
        public double EntityCurrentYawResidualDegrees { get; }
        public double MountEntityRootYawResidualDegrees { get; }
        public double AuthoritativePositionAdvanceWorldUnits { get; }
        public double AuthoritativeYawAdvanceDegrees { get; }
        public bool MovementCommandAbsent { get; }
        public bool WantsToMove { get; }
        public bool IsReallyMoving { get; }

        private static void RequireFiniteNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0d)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    public sealed class MovementSynchronizationBoundaryClosure
    {
        internal MovementSynchronizationBoundaryClosure(
            bool attempted,
            bool succeeded,
            string reason,
            long yawPendingBefore,
            long positionPendingBefore,
            long yawClosedCount,
            long positionClosedCount,
            long yawPendingAfter,
            long positionPendingAfter,
            double authoritativePositionAdvanceWorldUnits,
            double authoritativeYawAdvanceDegrees)
        {
            Attempted = attempted;
            Succeeded = succeeded;
            Reason = reason ?? string.Empty;
            YawPendingBefore = yawPendingBefore;
            PositionPendingBefore = positionPendingBefore;
            YawClosedCount = yawClosedCount;
            PositionClosedCount = positionClosedCount;
            YawPendingAfter = yawPendingAfter;
            PositionPendingAfter = positionPendingAfter;
            AuthoritativePositionAdvanceWorldUnits = authoritativePositionAdvanceWorldUnits;
            AuthoritativeYawAdvanceDegrees = authoritativeYawAdvanceDegrees;
        }

        public bool Attempted { get; }
        public bool Succeeded { get; }
        public string Reason { get; }
        public long YawPendingBefore { get; }
        public long PositionPendingBefore { get; }
        public long YawClosedCount { get; }
        public long PositionClosedCount { get; }
        public long YawPendingAfter { get; }
        public long PositionPendingAfter { get; }
        public double AuthoritativePositionAdvanceWorldUnits { get; }
        public double AuthoritativeYawAdvanceDegrees { get; }
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

        public MovementPositionPhaseObservation LatestPositionObservation { get; private set; }

        public double LatestPreCorrectionPositionResidualWorldUnits { get; private set; }

        public double LatestPreCorrectionRawCurrentPositionResidualWorldUnits { get; private set; }

        public double LatestPreCorrectionViewCurrentPositionResidualWorldUnits { get; private set; }

        public double LatestPreCorrectionRotationResidualDegrees { get; private set; }

        public double LatestPreCorrectionFullViewCurrentRotationResidualDegrees { get; private set; }

        public double LatestPostCorrectionPositionResidualWorldUnits { get; private set; }

        public double LatestPostCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumPreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumPreCorrectionRawCurrentPositionResidualWorldUnits { get; private set; }

        public double MaximumPreCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumPostCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumPostCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumInitialConfigurationPreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits { get; private set; }

        public double MaximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits { get; private set; }

        public double MaximumUpdatePreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumUpdatePreCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumUpdatePostCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumUpdatePostCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumLateUpdatePreCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumLateUpdatePreCorrectionRotationResidualDegrees { get; private set; }

        public double MaximumLateUpdatePostCorrectionPositionResidualWorldUnits { get; private set; }

        public double MaximumLateUpdatePostCorrectionRotationResidualDegrees { get; private set; }

        public MovementYawPhaseObservation LatestYawObservation { get; private set; }

        public double MaximumCalibratedViewCurrentPositionResidualWorldUnits { get; private set; }

        public double MaximumCalibratedEntityRawCurrentPositionResidualWorldUnits { get; private set; }

        public double MaximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits { get; private set; }

        public double MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits { get; private set; }

        public double MaximumAuthoritativePositionDeltaWorldUnits { get; private set; }

        public double MaximumEntityRawPositionLagExcessWorldUnits { get; private set; }

        public long PositionPhaseLagObservedCount { get; private set; }

        public long PositionPhaseLagPermittedCount { get; private set; }

        public long PositionPhaseLagSameFrameUpdateReferenceCount { get; private set; }

        public long PositionPhaseLagEligibleReferenceCount { get; private set; }

        public long PositionPhaseLagViolationCount { get; private set; }

        public long PositionPhaseLagRecoveryRequiredCount { get; private set; }

        public long PositionPhaseLagRecoveryUpdateCount { get; private set; }

        public long PositionPhaseLagRecoverySatisfiedCount { get; private set; }

        public long PositionPhaseLagRecoveryViolationCount { get; private set; }

        public long StationaryPositionCorrectionViolationCount { get; private set; }

        public long OutstandingPositionPhaseLagRecoveryCount { get; private set; }

        public long MaximumConsecutiveUnrecoveredPositionPhaseLagCount { get; private set; }

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

        public long StationaryBoundaryClosureAttemptCount { get; private set; }

        public long StationaryBoundaryClosureSucceededCount { get; private set; }

        public long StationaryBoundaryClosureFailedCount { get; private set; }

        public long YawPhaseLagStationaryBoundaryClosureCount { get; private set; }

        public long PositionPhaseLagStationaryBoundaryClosureCount { get; private set; }

        public long EffectivePhaseLagRecoveryRequiredCount => PhaseLagRecoveryRequiredCount + YawPhaseLagStationaryBoundaryClosureCount;

        public long EffectivePhaseLagRecoveryUpdateOrBoundaryCount => PhaseLagRecoveryUpdateCount + YawPhaseLagStationaryBoundaryClosureCount;

        public long EffectivePhaseLagRecoverySatisfiedCount => PhaseLagRecoverySatisfiedCount + YawPhaseLagStationaryBoundaryClosureCount;

        public long EffectivePositionPhaseLagRecoveryRequiredCount => PositionPhaseLagRecoveryRequiredCount + PositionPhaseLagStationaryBoundaryClosureCount;

        public long EffectivePositionPhaseLagRecoveryUpdateOrBoundaryCount => PositionPhaseLagRecoveryUpdateCount + PositionPhaseLagStationaryBoundaryClosureCount;

        public long EffectivePositionPhaseLagRecoverySatisfiedCount => PositionPhaseLagRecoverySatisfiedCount + PositionPhaseLagStationaryBoundaryClosureCount;

        public void Observe(MovementSynchronizationSample sample)
        {
            if (sample == null) { throw new ArgumentNullException(nameof(sample)); }
            if (sample.Sequence != SampleCount)
            {
                throw new InvalidOperationException("Movement synchronization sample sequence is not contiguous.");
            }

            SampleCount++;
            LatestPhase = sample.Phase;
            LatestPositionObservation = sample.Position;
            LatestYawObservation = sample.Yaw;
            LatestPreCorrectionPositionResidualWorldUnits = sample.PreCorrectionPositionResidualWorldUnits;
            LatestPreCorrectionRawCurrentPositionResidualWorldUnits = sample.PreCorrectionRawCurrentPositionResidualWorldUnits;
            LatestPreCorrectionViewCurrentPositionResidualWorldUnits = sample.PreCorrectionViewCurrentPositionResidualWorldUnits;
            LatestPreCorrectionRotationResidualDegrees = sample.PreCorrectionRotationResidualDegrees;
            LatestPreCorrectionFullViewCurrentRotationResidualDegrees = sample.PreCorrectionFullViewCurrentRotationResidualDegrees;
            LatestPostCorrectionPositionResidualWorldUnits = sample.PostCorrectionPositionResidualWorldUnits;
            LatestPostCorrectionRotationResidualDegrees = sample.PostCorrectionRotationResidualDegrees;
            MaximumPreCorrectionPositionResidualWorldUnits = Math.Max(MaximumPreCorrectionPositionResidualWorldUnits, sample.PreCorrectionPositionResidualWorldUnits);
            MaximumPreCorrectionRawCurrentPositionResidualWorldUnits = Math.Max(
                MaximumPreCorrectionRawCurrentPositionResidualWorldUnits,
                sample.PreCorrectionRawCurrentPositionResidualWorldUnits);
            MaximumPreCorrectionRotationResidualDegrees = Math.Max(MaximumPreCorrectionRotationResidualDegrees, sample.PreCorrectionRotationResidualDegrees);
            MaximumPostCorrectionPositionResidualWorldUnits = Math.Max(MaximumPostCorrectionPositionResidualWorldUnits, sample.PostCorrectionPositionResidualWorldUnits);
            MaximumPostCorrectionRotationResidualDegrees = Math.Max(MaximumPostCorrectionRotationResidualDegrees, sample.PostCorrectionRotationResidualDegrees);

            if (sample.Phase == MovementSynchronizationPhase.Update || sample.Phase == MovementSynchronizationPhase.LateUpdate)
            {
                MaximumCalibratedViewCurrentPositionResidualWorldUnits = Math.Max(
                    MaximumCalibratedViewCurrentPositionResidualWorldUnits,
                    sample.Position.ViewCurrentPositionResidualWorldUnits);
                MaximumCalibratedEntityRawCurrentPositionResidualWorldUnits = Math.Max(
                    MaximumCalibratedEntityRawCurrentPositionResidualWorldUnits,
                    sample.Position.EntityRawCurrentPositionResidualWorldUnits);
                if (sample.Position.EntityPreviousAuthoritativePositionResidualWorldUnits.HasValue)
                {
                    MaximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits = Math.Max(
                        MaximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits,
                        sample.Position.EntityPreviousAuthoritativePositionResidualWorldUnits.Value);
                }
                MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits = Math.Max(
                    MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits,
                    sample.Position.EntityPhaseAdjustedPositionResidualWorldUnits);
                MaximumAuthoritativePositionDeltaWorldUnits = Math.Max(
                    MaximumAuthoritativePositionDeltaWorldUnits,
                    sample.Position.AuthoritativePositionDeltaWorldUnits);
                MaximumEntityRawPositionLagExcessWorldUnits = Math.Max(
                    MaximumEntityRawPositionLagExcessWorldUnits,
                    sample.Position.EntityRawLagExcessWorldUnits);
                if (sample.Position.PhaseLagObserved) { PositionPhaseLagObservedCount++; }
                if (sample.Position.PhaseLagObserved && sample.Position.PreviousAuthoritativeSameFrame &&
                    sample.Position.PreviousAuthoritativePhase == MovementSynchronizationPhase.Update)
                {
                    PositionPhaseLagSameFrameUpdateReferenceCount++;
                }
                if (sample.Position.PhaseLagObserved && sample.Position.PreviousAuthoritativeReferenceEligible)
                {
                    PositionPhaseLagEligibleReferenceCount++;
                }
                if (sample.Position.PhaseLagPermitted)
                {
                    PositionPhaseLagPermittedCount++;
                    OutstandingPositionPhaseLagRecoveryCount++;
                    MaximumConsecutiveUnrecoveredPositionPhaseLagCount = Math.Max(
                        MaximumConsecutiveUnrecoveredPositionPhaseLagCount,
                        OutstandingPositionPhaseLagRecoveryCount);
                }
                if (sample.Position.PhaseLagViolation) { PositionPhaseLagViolationCount++; }
                // A pending obligation can be carried through multiple aligned
                // non-Update observations. The raw normal-recovery counters
                // describe the one Update that discharges (or rejects) an
                // obligation, not every sample through which it remains live.
                if (sample.Position.RecoveryUpdateObserved) { PositionPhaseLagRecoveryRequiredCount++; }
                if (sample.Position.RecoveryUpdateObserved) { PositionPhaseLagRecoveryUpdateCount++; }
                if (sample.Position.RecoverySatisfied)
                {
                    PositionPhaseLagRecoverySatisfiedCount++;
                    if (OutstandingPositionPhaseLagRecoveryCount > 0L) { OutstandingPositionPhaseLagRecoveryCount--; }
                }
                if (sample.Position.RecoveryViolation) { PositionPhaseLagRecoveryViolationCount++; }
                if (sample.Position.StationaryPositionCorrectionViolation) { StationaryPositionCorrectionViolationCount++; }

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
                if (sample.Yaw.RecoveryUpdateObserved) { PhaseLagRecoveryRequiredCount++; }
                if (sample.Yaw.RecoveryUpdateObserved) { PhaseLagRecoveryUpdateCount++; }
                if (sample.Yaw.RecoverySatisfied)
                {
                    PhaseLagRecoverySatisfiedCount++;
                    if (OutstandingPhaseLagRecoveryCount > 0L) { OutstandingPhaseLagRecoveryCount--; }
                }
                if (sample.Yaw.RecoveryViolation) { PhaseLagRecoveryViolationCount++; }
                if (sample.Yaw.StationaryYawCorrectionViolation) { StationaryYawCorrectionViolationCount++; }
            }
            else if (sample.Phase == MovementSynchronizationPhase.InitialConfiguration)
            {
                // InitialConfiguration is excluded from steady-state residual
                // maxima, but it may not erase a live recovery obligation. A
                // pending wrong-phase transition remains safety-significant.
                if (sample.Position.RecoveryViolation) { PositionPhaseLagRecoveryViolationCount++; }
                if (sample.Yaw.RecoveryViolation) { PhaseLagRecoveryViolationCount++; }
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
                    MaximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits = Math.Max(
                        MaximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits,
                        sample.PreCorrectionRawCurrentPositionResidualWorldUnits);
                    MaximumUpdatePreCorrectionRotationResidualDegrees = Math.Max(MaximumUpdatePreCorrectionRotationResidualDegrees, sample.PreCorrectionRotationResidualDegrees);
                    MaximumUpdatePostCorrectionPositionResidualWorldUnits = Math.Max(MaximumUpdatePostCorrectionPositionResidualWorldUnits, sample.PostCorrectionPositionResidualWorldUnits);
                    MaximumUpdatePostCorrectionRotationResidualDegrees = Math.Max(MaximumUpdatePostCorrectionRotationResidualDegrees, sample.PostCorrectionRotationResidualDegrees);
                    break;
                case MovementSynchronizationPhase.LateUpdate:
                    LateUpdateSampleCount++;
                    if (sample.CorrectionRequired) { LateUpdateCorrectionCount++; }
                    MaximumLateUpdatePreCorrectionPositionResidualWorldUnits = Math.Max(MaximumLateUpdatePreCorrectionPositionResidualWorldUnits, sample.PreCorrectionPositionResidualWorldUnits);
                    MaximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits = Math.Max(
                        MaximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits,
                        sample.PreCorrectionRawCurrentPositionResidualWorldUnits);
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

        /// <summary>
        /// Closes at most one already-permitted lag per channel at the exact
        /// stopped cleanup boundary. This is deliberately not an Update sample
        /// and does not increment normal recovery or synchronization counters.
        /// The caller must dismount synchronously before another callback.
        /// </summary>
        public MovementSynchronizationBoundaryClosure ClosePendingRecoveryAtStationaryBoundary(
            MovementSynchronizationBoundarySnapshot boundary,
            double maximumPositionResidualWorldUnits,
            double maximumRotationResidualDegrees,
            MovementPositionPhaseTracker positionTracker,
            MovementYawPhaseTracker yawTracker)
        {
            if (boundary == null) { throw new ArgumentNullException(nameof(boundary)); }
            if (positionTracker == null) { throw new ArgumentNullException(nameof(positionTracker)); }
            if (yawTracker == null) { throw new ArgumentNullException(nameof(yawTracker)); }
            if (double.IsNaN(maximumPositionResidualWorldUnits) || double.IsInfinity(maximumPositionResidualWorldUnits) || maximumPositionResidualWorldUnits <= 0.0d)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumPositionResidualWorldUnits));
            }
            if (double.IsNaN(maximumRotationResidualDegrees) || double.IsInfinity(maximumRotationResidualDegrees) || maximumRotationResidualDegrees <= 0.0d)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumRotationResidualDegrees));
            }

            var yawPendingBefore = OutstandingPhaseLagRecoveryCount;
            var positionPendingBefore = OutstandingPositionPhaseLagRecoveryCount;
            var attempted = yawPendingBefore > 0L || positionPendingBefore > 0L;
            if (!attempted)
            {
                if (yawTracker.RecoveryPending || positionTracker.RecoveryPending)
                {
                    StationaryBoundaryClosureAttemptCount++;
                    StationaryBoundaryClosureFailedCount++;
                    return new MovementSynchronizationBoundaryClosure(
                        true, false, "tracker-pending-state-mismatch",
                        0L, 0L, 0L, 0L, 0L, 0L,
                        boundary.AuthoritativePositionAdvanceWorldUnits,
                        boundary.AuthoritativeYawAdvanceDegrees);
                }
                if (StationaryBoundaryClosureAttemptCount > 0L)
                {
                    StationaryBoundaryClosureAttemptCount++;
                    StationaryBoundaryClosureFailedCount++;
                    return new MovementSynchronizationBoundaryClosure(
                        true, false, "repeated-boundary-closure",
                        0L, 0L, 0L, 0L, 0L, 0L,
                        boundary.AuthoritativePositionAdvanceWorldUnits,
                        boundary.AuthoritativeYawAdvanceDegrees);
                }
                return new MovementSynchronizationBoundaryClosure(
                    false, true, "no-pending-recovery",
                    0L, 0L, 0L, 0L, 0L, 0L,
                    boundary.AuthoritativePositionAdvanceWorldUnits,
                    boundary.AuthoritativeYawAdvanceDegrees);
            }

            StationaryBoundaryClosureAttemptCount++;
            string failure = null;
            if (StationaryBoundaryClosureAttemptCount > 1L)
            {
                failure = "repeated-boundary-closure";
            }
            else if (yawPendingBefore > 1L || positionPendingBefore > 1L)
            {
                failure = "more-than-one-pending-lag";
            }
            else if (!boundary.MovementCommandAbsent || boundary.WantsToMove || boundary.IsReallyMoving)
            {
                failure = "movement-not-stopped";
            }
            else if (boundary.AuthoritativePositionAdvanceWorldUnits > MovementPositionPhaseTracker.StationaryAuthorityEpsilonWorldUnits ||
                boundary.AuthoritativeYawAdvanceDegrees > MovementYawPhaseTracker.StationaryAuthorityEpsilonDegrees)
            {
                failure = "authority-advanced";
            }
            else if (boundary.ViewCurrentPositionResidualWorldUnits > maximumPositionResidualWorldUnits ||
                boundary.EntityCurrentPositionResidualWorldUnits > maximumPositionResidualWorldUnits ||
                boundary.FullViewCurrentRotationResidualDegrees > maximumRotationResidualDegrees ||
                boundary.ViewCurrentYawResidualDegrees > maximumRotationResidualDegrees ||
                boundary.EntityCurrentYawResidualDegrees > maximumRotationResidualDegrees ||
                boundary.MountEntityRootYawResidualDegrees > maximumRotationResidualDegrees)
            {
                failure = "boundary-residual-exceeded";
            }
            else if (yawPendingBefore == 1L &&
                (LatestYawObservation == null ||
                 !LatestYawObservation.RecoveryPendingAfterSample ||
                 (!LatestYawObservation.PhaseLagPermitted &&
                  (LatestYawObservation.Phase != MovementSynchronizationPhase.LateUpdate ||
                   !LatestYawObservation.RecoveryRequiredBeforeSample ||
                   LatestYawObservation.RecoveryViolation)) ||
                 !yawTracker.RecoveryPending))
            {
                failure = "yaw-pending-lag-not-permitted";
            }
            else if (positionPendingBefore == 1L &&
                (LatestPositionObservation == null ||
                 !LatestPositionObservation.RecoveryPendingAfterSample ||
                 (!LatestPositionObservation.PhaseLagPermitted &&
                  (LatestPositionObservation.Phase != MovementSynchronizationPhase.LateUpdate ||
                   !LatestPositionObservation.RecoveryRequiredBeforeSample ||
                   LatestPositionObservation.RecoveryViolation)) ||
                 !positionTracker.RecoveryPending))
            {
                failure = "position-pending-lag-not-permitted";
            }
            else if (PhaseLagViolationCount != 0L || PhaseLagRecoveryViolationCount != 0L || StationaryYawCorrectionViolationCount != 0L ||
                PositionPhaseLagViolationCount != 0L || PositionPhaseLagRecoveryViolationCount != 0L || StationaryPositionCorrectionViolationCount != 0L)
            {
                failure = "prior-phase-violation";
            }
            else if (PhaseLagRecoveryRequiredCount != PhaseLagRecoveryUpdateCount ||
                PhaseLagRecoveryUpdateCount != PhaseLagRecoverySatisfiedCount ||
                PositionPhaseLagRecoveryRequiredCount != PositionPhaseLagRecoveryUpdateCount ||
                PositionPhaseLagRecoveryUpdateCount != PositionPhaseLagRecoverySatisfiedCount)
            {
                failure = "normal-recovery-count-mismatch";
            }
            else if (PhaseLagPermittedCount - PhaseLagRecoverySatisfiedCount != yawPendingBefore ||
                PositionPhaseLagPermittedCount - PositionPhaseLagRecoverySatisfiedCount != positionPendingBefore)
            {
                failure = "pending-count-mismatch";
            }
            else if (yawTracker.RecoveryPending != (yawPendingBefore == 1L) ||
                positionTracker.RecoveryPending != (positionPendingBefore == 1L))
            {
                failure = "tracker-pending-state-mismatch";
            }

            if (failure != null)
            {
                StationaryBoundaryClosureFailedCount++;
                return new MovementSynchronizationBoundaryClosure(
                    true, false, failure,
                    yawPendingBefore, positionPendingBefore,
                    0L, 0L,
                    OutstandingPhaseLagRecoveryCount,
                    OutstandingPositionPhaseLagRecoveryCount,
                    boundary.AuthoritativePositionAdvanceWorldUnits,
                    boundary.AuthoritativeYawAdvanceDegrees);
            }

            var yawClosedCount = yawPendingBefore == 1L ? 1L : 0L;
            var positionClosedCount = positionPendingBefore == 1L ? 1L : 0L;
            var yawTrackerClosed = yawClosedCount == 0L || yawTracker.ClosePendingRecoveryAtStationaryBoundary();
            var positionTrackerClosed = positionClosedCount == 0L || positionTracker.ClosePendingRecoveryAtStationaryBoundary();
            if (!yawTrackerClosed || !positionTrackerClosed)
            {
                throw new InvalidOperationException("Stationary boundary tracker closure changed after successful atomic preflight.");
            }
            YawPhaseLagStationaryBoundaryClosureCount += yawClosedCount;
            PositionPhaseLagStationaryBoundaryClosureCount += positionClosedCount;
            OutstandingPhaseLagRecoveryCount -= yawClosedCount;
            OutstandingPositionPhaseLagRecoveryCount -= positionClosedCount;
            StationaryBoundaryClosureSucceededCount++;
            return new MovementSynchronizationBoundaryClosure(
                true, true, "closed-at-stationary-boundary",
                yawPendingBefore, positionPendingBefore,
                yawClosedCount, positionClosedCount,
                OutstandingPhaseLagRecoveryCount,
                OutstandingPositionPhaseLagRecoveryCount,
                boundary.AuthoritativePositionAdvanceWorldUnits,
                boundary.AuthoritativeYawAdvanceDegrees);
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

        public bool PhaseOrderPositionPassed { get; private set; }

        public bool PhaseOrderPositionSafetyPassed { get; private set; }

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
            var phaseOrderPositionSafetyPassed = calibratedPreCorrectionMaximum <= maximumPositionResidualWorldUnits &&
                telemetry.MaximumCalibratedViewCurrentPositionResidualWorldUnits <= maximumPositionResidualWorldUnits &&
                telemetry.MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits <= maximumPositionResidualWorldUnits &&
                telemetry.MaximumEntityRawPositionLagExcessWorldUnits <= MovementPositionPhaseTracker.RawLagArithmeticCoherenceEpsilonWorldUnits &&
                telemetry.PositionPhaseLagViolationCount == 0L &&
                telemetry.PositionPhaseLagRecoveryViolationCount == 0L &&
                telemetry.StationaryPositionCorrectionViolationCount == 0L &&
                telemetry.MaximumConsecutiveUnrecoveredPositionPhaseLagCount <= 1L &&
                telemetry.OutstandingPositionPhaseLagRecoveryCount <= 1L &&
                telemetry.PositionPhaseLagObservedCount == telemetry.PositionPhaseLagPermittedCount &&
                telemetry.PositionPhaseLagPermittedCount == telemetry.PositionPhaseLagSameFrameUpdateReferenceCount &&
                telemetry.PositionPhaseLagPermittedCount == telemetry.PositionPhaseLagEligibleReferenceCount &&
                telemetry.PositionPhaseLagRecoveryRequiredCount == telemetry.PositionPhaseLagRecoveryUpdateCount &&
                telemetry.PositionPhaseLagRecoveryUpdateCount == telemetry.PositionPhaseLagRecoverySatisfiedCount &&
                telemetry.PositionPhaseLagPermittedCount == telemetry.EffectivePositionPhaseLagRecoverySatisfiedCount + telemetry.OutstandingPositionPhaseLagRecoveryCount &&
                telemetry.PositionPhaseLagStationaryBoundaryClosureCount <= 1L &&
                telemetry.StationaryBoundaryClosureFailedCount == 0L &&
                telemetry.StationaryBoundaryClosureAttemptCount == telemetry.StationaryBoundaryClosureSucceededCount + telemetry.StationaryBoundaryClosureFailedCount;
            var phaseOrderPositionPassed = phaseOrderPositionSafetyPassed &&
                telemetry.OutstandingPositionPhaseLagRecoveryCount == 0L &&
                telemetry.PositionPhaseLagPermittedCount == telemetry.EffectivePositionPhaseLagRecoveryRequiredCount &&
                telemetry.PositionPhaseLagPermittedCount == telemetry.EffectivePositionPhaseLagRecoveryUpdateOrBoundaryCount &&
                telemetry.PositionPhaseLagPermittedCount == telemetry.EffectivePositionPhaseLagRecoverySatisfiedCount;
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
                telemetry.PhaseLagPermittedCount == telemetry.PhaseLagEligibleReferenceCount &&
                telemetry.PhaseLagRecoveryRequiredCount == telemetry.PhaseLagRecoveryUpdateCount &&
                telemetry.PhaseLagRecoveryUpdateCount == telemetry.PhaseLagRecoverySatisfiedCount &&
                telemetry.PhaseLagPermittedCount == telemetry.EffectivePhaseLagRecoverySatisfiedCount + telemetry.OutstandingPhaseLagRecoveryCount &&
                telemetry.YawPhaseLagStationaryBoundaryClosureCount <= 1L &&
                telemetry.StationaryBoundaryClosureFailedCount == 0L &&
                telemetry.StationaryBoundaryClosureAttemptCount == telemetry.StationaryBoundaryClosureSucceededCount + telemetry.StationaryBoundaryClosureFailedCount;
            var phaseOrderYawPassed = phaseOrderYawSafetyPassed &&
                telemetry.OutstandingPhaseLagRecoveryCount == 0L &&
                telemetry.PhaseLagPermittedCount == telemetry.EffectivePhaseLagRecoveryRequiredCount &&
                telemetry.PhaseLagPermittedCount == telemetry.EffectivePhaseLagRecoveryUpdateOrBoundaryCount &&
                telemetry.PhaseLagPermittedCount == telemetry.EffectivePhaseLagRecoverySatisfiedCount;

            return new MovementSynchronizationQualification
            {
                MaximumCalibratedPreCorrectionPositionResidualWorldUnits = calibratedPreCorrectionMaximum,
                MaximumCalibratedPreCorrectionRotationResidualDegrees = calibratedPreCorrectionRotationMaximum,
                PreCorrectionPositionPassed = phaseOrderPositionPassed,
                PreCorrectionRotationPassed = phaseOrderYawPassed,
                PhaseOrderPositionPassed = phaseOrderPositionPassed,
                PhaseOrderPositionSafetyPassed = phaseOrderPositionSafetyPassed,
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
