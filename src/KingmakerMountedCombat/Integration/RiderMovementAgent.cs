using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using Pathfinding;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class RiderMovementAgent : UnitMovementAgentBase
    {
        private const double MaximumPositionResidualWorldUnits = 0.10d;
        private const double MaximumYawResidualDegrees = 0.10d;
        private UnitEntityData mount;
        private Transform anchor;
        private Vector3 anchorLocalOffset;
        private Vector3 localEulerRotation;
        private bool configured;
        private MovementSynchronizationTelemetryAccumulator telemetry = new MovementSynchronizationTelemetryAccumulator();
        private MovementPositionPhaseTracker positionPhaseTracker = new MovementPositionPhaseTracker();
        private MovementYawPhaseTracker yawPhaseTracker = new MovementYawPhaseTracker();

        public override bool WantsToMove => configured && mount?.View?.AgentASP != null && mount.View.AgentASP.WantsToMove;

        public override bool IsReallyMoving => configured && mount?.View?.AgentASP != null && mount.View.AgentASP.IsReallyMoving;

        public override Path Path { get; protected set; }

        public override bool AvoidanceDisabled
        {
            get => true;
            set { }
        }

        public double MaximumResidualWorldUnits => telemetry.MaximumPreCorrectionPositionResidualWorldUnits;

        public double MaximumRotationResidualDegrees => telemetry.MaximumPreCorrectionRotationResidualDegrees;

        public long SampleCount => telemetry.SampleCount;

        public long CorrectionCount => telemetry.CorrectionCount;

        public long InitialConfigurationSampleCount => telemetry.InitialConfigurationSampleCount;

        public long InitialConfigurationCorrectionCount => telemetry.InitialConfigurationCorrectionCount;

        public long UpdateSampleCount => telemetry.UpdateSampleCount;

        public long UpdateCorrectionCount => telemetry.UpdateCorrectionCount;

        public long LateUpdateSampleCount => telemetry.LateUpdateSampleCount;

        public long LateUpdateCorrectionCount => telemetry.LateUpdateCorrectionCount;

        public MovementSynchronizationPhase LatestSynchronizationPhase => telemetry.LatestPhase;

        public double LatestPreCorrectionPositionResidualWorldUnits => telemetry.LatestPreCorrectionPositionResidualWorldUnits;

        public double LatestPreCorrectionRawCurrentPositionResidualWorldUnits => telemetry.LatestPreCorrectionRawCurrentPositionResidualWorldUnits;

        public double LatestPreCorrectionViewCurrentPositionResidualWorldUnits => telemetry.LatestPreCorrectionViewCurrentPositionResidualWorldUnits;

        public double LatestPreCorrectionRotationResidualDegrees => telemetry.LatestPreCorrectionRotationResidualDegrees;

        public double LatestPreCorrectionFullViewCurrentRotationResidualDegrees => telemetry.LatestPreCorrectionFullViewCurrentRotationResidualDegrees;

        public double LatestPostCorrectionPositionResidualWorldUnits => telemetry.LatestPostCorrectionPositionResidualWorldUnits;

        public double LatestPostCorrectionRotationResidualDegrees => telemetry.LatestPostCorrectionRotationResidualDegrees;

        public double MaximumPreCorrectionPositionResidualWorldUnits => telemetry.MaximumPreCorrectionPositionResidualWorldUnits;

        public double MaximumPreCorrectionRawCurrentPositionResidualWorldUnits => telemetry.MaximumPreCorrectionRawCurrentPositionResidualWorldUnits;

        public double MaximumPreCorrectionRotationResidualDegrees => telemetry.MaximumPreCorrectionRotationResidualDegrees;

        public double MaximumPostCorrectionPositionResidualWorldUnits => telemetry.MaximumPostCorrectionPositionResidualWorldUnits;

        public double MaximumPostCorrectionRotationResidualDegrees => telemetry.MaximumPostCorrectionRotationResidualDegrees;

        public double MaximumInitialConfigurationPreCorrectionPositionResidualWorldUnits => telemetry.MaximumInitialConfigurationPreCorrectionPositionResidualWorldUnits;

        public double MaximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits => telemetry.MaximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits;

        public double MaximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits => telemetry.MaximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits;

        public double MaximumUpdatePreCorrectionPositionResidualWorldUnits => telemetry.MaximumUpdatePreCorrectionPositionResidualWorldUnits;

        public double MaximumUpdatePreCorrectionRotationResidualDegrees => telemetry.MaximumUpdatePreCorrectionRotationResidualDegrees;

        public double MaximumUpdatePostCorrectionPositionResidualWorldUnits => telemetry.MaximumUpdatePostCorrectionPositionResidualWorldUnits;

        public double MaximumUpdatePostCorrectionRotationResidualDegrees => telemetry.MaximumUpdatePostCorrectionRotationResidualDegrees;

        public double MaximumLateUpdatePreCorrectionPositionResidualWorldUnits => telemetry.MaximumLateUpdatePreCorrectionPositionResidualWorldUnits;

        public double MaximumLateUpdatePreCorrectionRotationResidualDegrees => telemetry.MaximumLateUpdatePreCorrectionRotationResidualDegrees;

        public double MaximumLateUpdatePostCorrectionPositionResidualWorldUnits => telemetry.MaximumLateUpdatePostCorrectionPositionResidualWorldUnits;

        public double MaximumLateUpdatePostCorrectionRotationResidualDegrees => telemetry.MaximumLateUpdatePostCorrectionRotationResidualDegrees;

        public MovementYawPhaseObservation LatestYawObservation => telemetry.LatestYawObservation;

        public MovementPositionPhaseObservation LatestPositionObservation => telemetry.LatestPositionObservation;

        public double MaximumCalibratedViewCurrentPositionResidualWorldUnits => telemetry.MaximumCalibratedViewCurrentPositionResidualWorldUnits;

        public double MaximumCalibratedEntityRawCurrentPositionResidualWorldUnits => telemetry.MaximumCalibratedEntityRawCurrentPositionResidualWorldUnits;

        public double MaximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits => telemetry.MaximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits;

        public double MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits => telemetry.MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits;

        public double MaximumAuthoritativePositionDeltaWorldUnits => telemetry.MaximumAuthoritativePositionDeltaWorldUnits;

        public double MaximumEntityRawPositionLagExcessWorldUnits => telemetry.MaximumEntityRawPositionLagExcessWorldUnits;

        public long PositionPhaseLagObservedCount => telemetry.PositionPhaseLagObservedCount;

        public long PositionPhaseLagPermittedCount => telemetry.PositionPhaseLagPermittedCount;

        public long PositionPhaseLagSameFrameUpdateReferenceCount => telemetry.PositionPhaseLagSameFrameUpdateReferenceCount;

        public long PositionPhaseLagEligibleReferenceCount => telemetry.PositionPhaseLagEligibleReferenceCount;

        public long PositionPhaseLagViolationCount => telemetry.PositionPhaseLagViolationCount;

        public long PositionPhaseLagRecoveryRequiredCount => telemetry.PositionPhaseLagRecoveryRequiredCount;

        public long PositionPhaseLagRecoveryUpdateCount => telemetry.PositionPhaseLagRecoveryUpdateCount;

        public long PositionPhaseLagRecoverySatisfiedCount => telemetry.PositionPhaseLagRecoverySatisfiedCount;

        public long PositionPhaseLagRecoveryViolationCount => telemetry.PositionPhaseLagRecoveryViolationCount;

        public long StationaryPositionCorrectionViolationCount => telemetry.StationaryPositionCorrectionViolationCount;

        public long OutstandingPositionPhaseLagRecoveryCount => telemetry.OutstandingPositionPhaseLagRecoveryCount;

        public long MaximumConsecutiveUnrecoveredPositionPhaseLagCount => telemetry.MaximumConsecutiveUnrecoveredPositionPhaseLagCount;

        public double MaximumCalibratedViewCurrentYawResidualDegrees => telemetry.MaximumCalibratedViewCurrentYawResidualDegrees;

        public double MaximumCalibratedFullViewCurrentRotationResidualDegrees => telemetry.MaximumCalibratedFullViewCurrentRotationResidualDegrees;

        public double MaximumCalibratedMountEntityRootYawResidualDegrees => telemetry.MaximumCalibratedMountEntityRootYawResidualDegrees;

        public double MaximumCalibratedEntityRawCurrentYawResidualDegrees => telemetry.MaximumCalibratedEntityRawCurrentYawResidualDegrees;

        public double MaximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees => telemetry.MaximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees;

        public double MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees => telemetry.MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees;

        public double MaximumAuthoritativeYawDeltaDegrees => telemetry.MaximumAuthoritativeYawDeltaDegrees;

        public double MaximumEntityRawLagExcessDegrees => telemetry.MaximumEntityRawLagExcessDegrees;

        public long PhaseLagObservedCount => telemetry.PhaseLagObservedCount;

        public long PhaseLagPermittedCount => telemetry.PhaseLagPermittedCount;

        public long PhaseLagSameFrameUpdateReferenceCount => telemetry.PhaseLagSameFrameUpdateReferenceCount;

        public long PhaseLagEligibleReferenceCount => telemetry.PhaseLagEligibleReferenceCount;

        public long PhaseLagViolationCount => telemetry.PhaseLagViolationCount;

        public long PhaseLagRecoveryRequiredCount => telemetry.PhaseLagRecoveryRequiredCount;

        public long PhaseLagRecoveryUpdateCount => telemetry.PhaseLagRecoveryUpdateCount;

        public long PhaseLagRecoverySatisfiedCount => telemetry.PhaseLagRecoverySatisfiedCount;

        public long PhaseLagRecoveryViolationCount => telemetry.PhaseLagRecoveryViolationCount;

        public long StationaryYawCorrectionViolationCount => telemetry.StationaryYawCorrectionViolationCount;

        public long OutstandingPhaseLagRecoveryCount => telemetry.OutstandingPhaseLagRecoveryCount;

        public long MaximumConsecutiveUnrecoveredPhaseLagCount => telemetry.MaximumConsecutiveUnrecoveredPhaseLagCount;

        public long StationaryBoundaryClosureAttemptCount => telemetry.StationaryBoundaryClosureAttemptCount;

        public long StationaryBoundaryClosureSucceededCount => telemetry.StationaryBoundaryClosureSucceededCount;

        public long StationaryBoundaryClosureFailedCount => telemetry.StationaryBoundaryClosureFailedCount;

        public long YawPhaseLagStationaryBoundaryClosureCount => telemetry.YawPhaseLagStationaryBoundaryClosureCount;

        public long PositionPhaseLagStationaryBoundaryClosureCount => telemetry.PositionPhaseLagStationaryBoundaryClosureCount;

        public long EffectivePhaseLagRecoveryRequiredCount => telemetry.EffectivePhaseLagRecoveryRequiredCount;

        public long EffectivePhaseLagRecoveryUpdateOrBoundaryCount => telemetry.EffectivePhaseLagRecoveryUpdateOrBoundaryCount;

        public long EffectivePhaseLagRecoverySatisfiedCount => telemetry.EffectivePhaseLagRecoverySatisfiedCount;

        public long EffectivePositionPhaseLagRecoveryRequiredCount => telemetry.EffectivePositionPhaseLagRecoveryRequiredCount;

        public long EffectivePositionPhaseLagRecoveryUpdateOrBoundaryCount => telemetry.EffectivePositionPhaseLagRecoveryUpdateOrBoundaryCount;

        public long EffectivePositionPhaseLagRecoverySatisfiedCount => telemetry.EffectivePositionPhaseLagRecoverySatisfiedCount;

        public bool IsConfigured => configured;

        public MovementSynchronizationQualification QualifySynchronization(
            long observedUpdateFrames,
            double maximumPositionResidualWorldUnits,
            double maximumRotationResidualDegrees)
        {
            return MovementSynchronizationQualification.Evaluate(
                telemetry,
                observedUpdateFrames,
                maximumPositionResidualWorldUnits,
                maximumRotationResidualDegrees);
        }

        public string AnchorName => anchor == null ? null : anchor.name;

        public Vector3 ExpectedPosition => anchor == null ? Vector3.zero : anchor.TransformPoint(anchorLocalOffset);

        public Quaternion ExpectedRotation => anchor == null ? Quaternion.identity : anchor.rotation * Quaternion.Euler(localEulerRotation);

        public MovementSynchronizationBoundarySnapshot CaptureBoundarySnapshot()
        {
            if (!configured || Unit == null || Unit.EntityData == null || mount == null || mount.View == null || mount.View.AgentASP == null || anchor == null)
            {
                throw new System.InvalidOperationException("Mounted synchronization boundary is not fully configured.");
            }

            var expectedPosition = ExpectedPosition;
            var expectedRotation = ExpectedRotation;
            var viewPosition = Unit.transform.position;
            var entityPosition = Unit.EntityData.Position;
            var viewPositionResidual = MovementTelemetrySample.CalculateDistance(
                expectedPosition.x, expectedPosition.y, expectedPosition.z,
                viewPosition.x, viewPosition.y, viewPosition.z);
            var entityPositionResidual = MovementTelemetrySample.CalculateDistance(
                expectedPosition.x, expectedPosition.y, expectedPosition.z,
                entityPosition.x, entityPosition.y, entityPosition.z);
            var latestPosition = telemetry.LatestPositionObservation;
            var latestYaw = telemetry.LatestYawObservation;
            var authoritativePositionAdvance = latestPosition == null
                ? double.MaxValue
                : MovementTelemetrySample.CalculateDistance(
                    latestPosition.CurrentAuthoritativeX,
                    latestPosition.CurrentAuthoritativeY,
                    latestPosition.CurrentAuthoritativeZ,
                    expectedPosition.x,
                    expectedPosition.y,
                    expectedPosition.z);
            var authoritativeYawAdvance = latestYaw == null
                ? double.MaxValue
                : MovementTelemetrySample.CalculateAngleDelta(latestYaw.CurrentAuthoritativeYawDegrees, expectedRotation.eulerAngles.y);
            var mountAgent = mount.View.AgentASP;
            return new MovementSynchronizationBoundarySnapshot(
                viewPositionResidual,
                entityPositionResidual,
                Quaternion.Angle(expectedRotation, Unit.transform.rotation),
                MovementTelemetrySample.CalculateAngleDelta(expectedRotation.eulerAngles.y, Unit.transform.rotation.eulerAngles.y),
                MovementTelemetrySample.CalculateAngleDelta(expectedRotation.eulerAngles.y, Unit.EntityData.Orientation),
                MovementTelemetrySample.CalculateAngleDelta(expectedRotation.eulerAngles.y, mount.Orientation + localEulerRotation.y),
                authoritativePositionAdvance,
                authoritativeYawAdvance,
                mount.Commands != null && mount.Commands.Move == null,
                mountAgent != null && mountAgent.WantsToMove,
                mountAgent != null && mountAgent.IsReallyMoving);
        }

        public MovementSynchronizationBoundaryClosure ClosePendingRecoveryAtStationaryBoundary(
            MovementSynchronizationBoundarySnapshot boundary,
            double maximumPositionResidualWorldUnits,
            double maximumRotationResidualDegrees)
        {
            return telemetry.ClosePendingRecoveryAtStationaryBoundary(
                boundary,
                maximumPositionResidualWorldUnits,
                maximumRotationResidualDegrees,
                positionPhaseTracker,
                yawPhaseTracker);
        }

        public void Configure(UnitEntityData mountUnit, Transform mountAnchor, Vector3 offset, Vector3 eulerRotation)
        {
            mount = mountUnit;
            anchor = mountAnchor;
            anchorLocalOffset = offset;
            localEulerRotation = eulerRotation;
            configured = true;
            telemetry = new MovementSynchronizationTelemetryAccumulator();
            positionPhaseTracker = new MovementPositionPhaseTracker();
            yawPhaseTracker = new MovementYawPhaseTracker();
            Synchronize(MovementSynchronizationPhase.InitialConfiguration);
        }

        public void Deconfigure()
        {
            configured = false;
            mount = null;
            anchor = null;
            anchorLocalOffset = Vector3.zero;
            localEulerRotation = Vector3.zero;
            ResetVelocity();
        }

        public override void TickMovement(float deltaTime)
        {
            UpdateVelocity();
            Synchronize(MovementSynchronizationPhase.Update);
        }

        public override void UpdateVelocity()
        {
            var mountAgent = mount?.View?.AgentASP;
            if (!configured || mountAgent == null)
            {
                ResetVelocity();
                return;
            }

            m_Velocity = mountAgent.Velocity;
            m_Speed = mountAgent.Speed;
            MoveDirection = mountAgent.MoveDirection;
        }

        public override bool IsSoftObstacle(UnitMovementAgentBase obstacle)
        {
            return true;
        }

        public override void Stop()
        {
            Path = null;
            ResetVelocity();
        }

        private void LateUpdate()
        {
            if (configured)
            {
                Synchronize(MovementSynchronizationPhase.LateUpdate);
            }
        }

        private void Synchronize(MovementSynchronizationPhase phase)
        {
            if (Unit == null || Unit.EntityData == null || mount == null || mount.View == null || anchor == null)
            {
                return;
            }

            var expectedPosition = anchor.TransformPoint(anchorLocalOffset);
            var expectedRotation = anchor.rotation * Quaternion.Euler(localEulerRotation);
            var preCorrectionPosition = Unit.transform.position;
            var preCorrectionEntityPosition = Unit.EntityData.Position;
            var positionObservation = positionPhaseTracker.Observe(
                Time.frameCount,
                phase,
                expectedPosition.x,
                expectedPosition.y,
                expectedPosition.z,
                preCorrectionPosition.x,
                preCorrectionPosition.y,
                preCorrectionPosition.z,
                preCorrectionEntityPosition.x,
                preCorrectionEntityPosition.y,
                preCorrectionEntityPosition.z,
                MaximumPositionResidualWorldUnits);
            var preCorrectionFullViewRotationResidual = Quaternion.Angle(expectedRotation, Unit.transform.rotation);
            var yawObservation = yawPhaseTracker.Observe(
                Time.frameCount,
                phase,
                expectedRotation.eulerAngles.y,
                mount.Orientation + localEulerRotation.y,
                Unit.transform.rotation.eulerAngles.y,
                Unit.EntityData.Orientation,
                MaximumYawResidualDegrees);
            Unit.transform.position = expectedPosition;
            Unit.transform.rotation = expectedRotation;
            Unit.EntityData.Position = expectedPosition;
            Unit.EntityData.Orientation = expectedRotation.eulerAngles.y;
            var postCorrectionPosition = Unit.transform.position;
            var postCorrectionEntityPosition = Unit.EntityData.Position;
            var postCorrectionViewPositionResidual = MovementTelemetrySample.CalculateDistance(expectedPosition.x, expectedPosition.y, expectedPosition.z, postCorrectionPosition.x, postCorrectionPosition.y, postCorrectionPosition.z);
            var postCorrectionEntityPositionResidual = MovementTelemetrySample.CalculateDistance(expectedPosition.x, expectedPosition.y, expectedPosition.z, postCorrectionEntityPosition.x, postCorrectionEntityPosition.y, postCorrectionEntityPosition.z);
            var postCorrectionPositionResidual = System.Math.Max(postCorrectionViewPositionResidual, postCorrectionEntityPositionResidual);
            var postCorrectionViewRotationResidual = Quaternion.Angle(expectedRotation, Unit.transform.rotation);
            var postCorrectionEntityRotationResidual = MovementTelemetrySample.CalculateAngleDelta(expectedRotation.eulerAngles.y, Unit.EntityData.Orientation);
            telemetry.Observe(new MovementSynchronizationSample(
                telemetry.SampleCount,
                phase,
                positionObservation,
                yawObservation,
                preCorrectionFullViewRotationResidual,
                postCorrectionPositionResidual,
                postCorrectionViewRotationResidual,
                postCorrectionEntityRotationResidual));
        }

        private void ResetVelocity()
        {
            m_Velocity = Vector3.zero;
            m_Speed = 0f;
            MoveDirection = Vector2.zero;
        }
    }
}
