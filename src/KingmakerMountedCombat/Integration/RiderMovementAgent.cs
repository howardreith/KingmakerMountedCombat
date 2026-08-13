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
        private double maximumResidualWorldUnits;
        private double maximumRotationResidualDegrees;
        private long sampleCount;

        public override bool WantsToMove => configured && mount?.View?.AgentASP != null && mount.View.AgentASP.WantsToMove;

        public override bool IsReallyMoving => configured && mount?.View?.AgentASP != null && mount.View.AgentASP.IsReallyMoving;

        public override Path Path { get; protected set; }

        public override bool AvoidanceDisabled
        {
            get => true;
            set { }
        }

        public double MaximumResidualWorldUnits => maximumResidualWorldUnits;

        public double MaximumRotationResidualDegrees => maximumRotationResidualDegrees;

        public long SampleCount => sampleCount;

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
            maximumResidualWorldUnits = 0.0d;
            maximumRotationResidualDegrees = 0.0d;
            sampleCount = 0;
            Synchronize();
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
            Synchronize();
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
                Synchronize();
            }
        }

        private void Synchronize()
        {
            if (Unit == null || Unit.EntityData == null || mount == null || mount.View == null || anchor == null)
            {
                return;
            }

            var expectedPosition = anchor.TransformPoint(anchorLocalOffset);
            var expectedRotation = anchor.rotation * Quaternion.Euler(localEulerRotation);
            Unit.transform.position = expectedPosition;
            Unit.transform.rotation = expectedRotation;
            Unit.EntityData.Position = expectedPosition;
            Unit.EntityData.Orientation = expectedRotation.eulerAngles.y;
            var observedPosition = Unit.transform.position;
            var observedYaw = Unit.transform.rotation.eulerAngles.y;
            var residual = MovementTelemetrySample.CalculateDistance(expectedPosition.x, expectedPosition.y, expectedPosition.z, observedPosition.x, observedPosition.y, observedPosition.z);
            var rotationResidual = MovementTelemetrySample.CalculateAngleDelta(expectedRotation.eulerAngles.y, observedYaw);
            maximumResidualWorldUnits = System.Math.Max(maximumResidualWorldUnits, residual);
            maximumRotationResidualDegrees = System.Math.Max(maximumRotationResidualDegrees, rotationResidual);
            sampleCount++;
        }

        private void ResetVelocity()
        {
            m_Velocity = Vector3.zero;
            m_Speed = 0f;
            MoveDirection = Vector2.zero;
        }
    }
}
