using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using Pathfinding;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class RiderMovementAgent : UnitMovementAgentBase
    {
        private UnitEntityData mount;
        private Transform anchor;
        private Vector3 anchorLocalOffset;
        private Vector3 localEulerRotation;
        private bool configured;
        private MovementSynchronizationTelemetryAccumulator telemetry = new MovementSynchronizationTelemetryAccumulator();

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

        public double LatestPreCorrectionRotationResidualDegrees => telemetry.LatestPreCorrectionRotationResidualDegrees;

        public double LatestPostCorrectionPositionResidualWorldUnits => telemetry.LatestPostCorrectionPositionResidualWorldUnits;

        public double LatestPostCorrectionRotationResidualDegrees => telemetry.LatestPostCorrectionRotationResidualDegrees;

        public double MaximumPreCorrectionPositionResidualWorldUnits => telemetry.MaximumPreCorrectionPositionResidualWorldUnits;

        public double MaximumPreCorrectionRotationResidualDegrees => telemetry.MaximumPreCorrectionRotationResidualDegrees;

        public double MaximumPostCorrectionPositionResidualWorldUnits => telemetry.MaximumPostCorrectionPositionResidualWorldUnits;

        public double MaximumPostCorrectionRotationResidualDegrees => telemetry.MaximumPostCorrectionRotationResidualDegrees;

        public double MaximumUpdatePreCorrectionPositionResidualWorldUnits => telemetry.MaximumUpdatePreCorrectionPositionResidualWorldUnits;

        public double MaximumUpdatePreCorrectionRotationResidualDegrees => telemetry.MaximumUpdatePreCorrectionRotationResidualDegrees;

        public double MaximumUpdatePostCorrectionPositionResidualWorldUnits => telemetry.MaximumUpdatePostCorrectionPositionResidualWorldUnits;

        public double MaximumUpdatePostCorrectionRotationResidualDegrees => telemetry.MaximumUpdatePostCorrectionRotationResidualDegrees;

        public double MaximumLateUpdatePreCorrectionPositionResidualWorldUnits => telemetry.MaximumLateUpdatePreCorrectionPositionResidualWorldUnits;

        public double MaximumLateUpdatePreCorrectionRotationResidualDegrees => telemetry.MaximumLateUpdatePreCorrectionRotationResidualDegrees;

        public double MaximumLateUpdatePostCorrectionPositionResidualWorldUnits => telemetry.MaximumLateUpdatePostCorrectionPositionResidualWorldUnits;

        public double MaximumLateUpdatePostCorrectionRotationResidualDegrees => telemetry.MaximumLateUpdatePostCorrectionRotationResidualDegrees;

        public bool IsConfigured => configured;

        public string AnchorName => anchor == null ? null : anchor.name;

        public Vector3 ExpectedPosition => anchor == null ? Vector3.zero : anchor.TransformPoint(anchorLocalOffset);

        public Quaternion ExpectedRotation => anchor == null ? Quaternion.identity : anchor.rotation * Quaternion.Euler(localEulerRotation);

        public void Configure(UnitEntityData mountUnit, Transform mountAnchor, Vector3 offset, Vector3 eulerRotation)
        {
            mount = mountUnit;
            anchor = mountAnchor;
            anchorLocalOffset = offset;
            localEulerRotation = eulerRotation;
            configured = true;
            telemetry = new MovementSynchronizationTelemetryAccumulator();
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
            var preCorrectionPositionResidual = MovementTelemetrySample.CalculateDistance(expectedPosition.x, expectedPosition.y, expectedPosition.z, preCorrectionPosition.x, preCorrectionPosition.y, preCorrectionPosition.z);
            var preCorrectionRotationResidual = Quaternion.Angle(expectedRotation, Unit.transform.rotation);
            Unit.transform.position = expectedPosition;
            Unit.transform.rotation = expectedRotation;
            Unit.EntityData.Position = expectedPosition;
            Unit.EntityData.Orientation = expectedRotation.eulerAngles.y;
            var postCorrectionPosition = Unit.transform.position;
            var postCorrectionPositionResidual = MovementTelemetrySample.CalculateDistance(expectedPosition.x, expectedPosition.y, expectedPosition.z, postCorrectionPosition.x, postCorrectionPosition.y, postCorrectionPosition.z);
            var postCorrectionRotationResidual = Quaternion.Angle(expectedRotation, Unit.transform.rotation);
            telemetry.Observe(new MovementSynchronizationSample(
                telemetry.SampleCount,
                phase,
                preCorrectionPositionResidual,
                preCorrectionRotationResidual,
                postCorrectionPositionResidual,
                postCorrectionRotationResidual));
        }

        private void ResetVelocity()
        {
            m_Velocity = Vector3.zero;
            m_Speed = 0f;
            MoveDirection = Vector2.zero;
        }
    }
}
