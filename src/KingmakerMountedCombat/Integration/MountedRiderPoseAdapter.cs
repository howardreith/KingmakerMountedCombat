using System;
using System.Collections.Generic;
using System.Diagnostics;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    [DefaultExecutionOrder(32000)]
    internal sealed class MountedRiderPoseAdapter : MonoBehaviour
    {
        private ScopedPoseBaselineLease<Transform, Vector3, Quaternion, Vector3> baselineLease;
        private UnitEntityView riderView;
        private MountedRiderPoseProfile profile;
        private Transform pelvis;
        private LegBinding leftLeg;
        private LegBinding rightLeg;
        private bool configured;
        private bool baselineCaptured;
        private long totalApplyTicks;
        private readonly AnimatedSaddlePosition animatedSaddle = new AnimatedSaddlePosition();
        private Transform saddleMountRoot;
        private Transform saddleSource;
        private bool animatedSaddleRequired;

        public bool IsConfigured => configured;

        public bool IsHealthy => configured && string.IsNullOrEmpty(LastFailure) && baselineLease != null && baselineLease.IsAcquired &&
            (!animatedSaddleRequired || animatedSaddle.IsAcquired && saddleMountRoot != null && saddleSource != null &&
                saddleSource.IsChildOf(saddleMountRoot));

        public long AnimatedSaddleSampleCount { get; private set; }
        public double MaximumAnimatedSeatError { get; private set; }
        public Vector3 LastAnimatedSeatPosition { get; private set; }

        internal double? ObserveCurrentAnimatedSeatResidual()
        {
            if (!animatedSaddleRequired || !IsHealthy || pelvis == null) { return null; }
            var seat = animatedSaddle.Project(ToPose(saddleMountRoot.InverseTransformPoint(saddleSource.position)));
            return Vector3.Distance(pelvis.position, saddleMountRoot.TransformPoint(ToUnity(seat)));
        }

        public bool HasPoseResidue => baselineLease != null && (baselineLease.IsAcquired || baselineLease.IsFrameActive);

        public bool BaselineRestoreVerified => !baselineCaptured || (baselineLease != null && baselineLease.LastRestoreVerified);

        public bool FramePoseApplied => baselineLease != null && baselineLease.IsFrameActive;

        public int BoneCount => baselineLease == null ? 0 : baselineLease.NodeCount;

        public long PoseApplicationFrameCount { get; private set; }

        public long FootTargetClampCount { get; private set; }

        public double MaximumFootTargetResidualWorldUnits { get; private set; }

        public double MaximumKneeTargetResidualWorldUnits { get; private set; }

        public double MaximumSegmentLengthResidualWorldUnits { get; private set; }

        public double MaximumApplyMicroseconds { get; private set; }

        public double AverageApplyMicroseconds => PoseApplicationFrameCount == 0
            ? 0d
            : TicksToMicroseconds(totalApplyTicks) / PoseApplicationFrameCount;

        public string ProfileId => profile == null ? null : profile.Id;

        public string LastFailure { get; private set; }

        public string BoneInventory => pelvis == null || leftLeg == null || rightLeg == null
            ? null
            : string.Join(",", new[]
            {
                pelvis.name,
                leftLeg.Thigh.name,
                leftLeg.LowerLeg.name,
                leftLeg.Foot.name,
                rightLeg.Thigh.name,
                rightLeg.LowerLeg.name,
                rightLeg.Foot.name
            });

        public void Configure(UnitEntityView exactRiderView, MountedRiderPoseProfile exactProfile)
        {
            if (configured || (baselineLease != null && baselineLease.IsAcquired))
            {
                throw new InvalidOperationException("A mounted rider pose adapter is already configured.");
            }
            riderView = exactRiderView ?? throw new ArgumentNullException(nameof(exactRiderView));
            profile = exactProfile ?? throw new ArgumentNullException(nameof(exactProfile));
            string surfaceError;
            if (!TryResolveSupportedRig(riderView, profile, out pelvis, out leftLeg, out rightLeg, out surfaceError))
            {
                throw new InvalidOperationException(surfaceError);
            }

            baselineLease = CreateBaselineLease();
            baselineLease.Acquire(new[]
            {
                pelvis,
                leftLeg.Thigh,
                leftLeg.LowerLeg,
                leftLeg.Foot,
                rightLeg.Thigh,
                rightLeg.LowerLeg,
                rightLeg.Foot
            });
            baselineCaptured = true;
            PrimeTimedPosePath();
            LastFailure = null;
            PoseApplicationFrameCount = 0;
            FootTargetClampCount = 0;
            MaximumFootTargetResidualWorldUnits = 0d;
            MaximumKneeTargetResidualWorldUnits = 0d;
            MaximumSegmentLengthResidualWorldUnits = 0d;
            MaximumApplyMicroseconds = 0d;
            totalApplyTicks = 0;
            configured = true;
            enabled = true;
        }

        private void PrimeTimedPosePath()
        {
            // Keep one-time CLR/Unity call-site initialization outside the
            // per-frame budget while exercising the exact same pose mutation
            // and exact restoration path. Normal evidence counters start only
            // after this reversible prime succeeds.
            var started = Stopwatch.GetTimestamp();
            baselineLease.PrimeFrame(ApplyPose);
            var elapsedMicroseconds = TicksToMicroseconds(Stopwatch.GetTimestamp() - started);
            if (elapsedMicroseconds < 0d || double.IsNaN(elapsedMicroseconds) || double.IsInfinity(elapsedMicroseconds))
            {
                throw new InvalidOperationException("Mounted rider pose priming produced an invalid elapsed time.");
            }
        }

        public void ConfigureAnimatedSaddle(Transform exactMountRoot, Transform exactSource, Vector3 anatomicalCorrection)
        {
            if (!configured || exactMountRoot == null || exactSource == null ||
                !exactSource.IsChildOf(exactMountRoot))
            {
                throw new InvalidOperationException("Horse saddle requires the exact live mount rig.");
            }
            saddleMountRoot = exactMountRoot;
            saddleSource = exactSource;
            baselineLease.PrimeFrame(() =>
            {
                ApplyPose();
                animatedSaddle.Acquire(
                    ToPose(saddleMountRoot.InverseTransformPoint(saddleSource.position)),
                    ToPose(saddleMountRoot.InverseTransformPoint(pelvis.position)),
                    ToPose(anatomicalCorrection));
            });
            animatedSaddleRequired = true;
        }

        internal static bool TryValidateSupportedSurface(
            UnitEntityView exactRiderView,
            MountedRiderPoseProfile exactProfile,
            out string error)
        {
            Transform ignoredPelvis;
            LegBinding ignoredLeft;
            LegBinding ignoredRight;
            return TryResolveSupportedRig(
                exactRiderView,
                exactProfile,
                out ignoredPelvis,
                out ignoredLeft,
                out ignoredRight,
                out error);
        }

        public void Deconfigure()
        {
            configured = false;
            enabled = false;
            baselineLease?.Restore();
            animatedSaddle.Release();
            animatedSaddleRequired = false;
            saddleMountRoot = null;
            saddleSource = null;
            riderView = null;
            profile = null;
            pelvis = null;
            leftLeg = null;
            rightLeg = null;
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            try
            {
                baselineLease.RestoreFrame();
            }
            catch (Exception exception)
            {
                LatchFailure("Could not restore the previous evaluated animation baseline before the next animator pass.", exception);
            }
        }

        private void LateUpdate()
        {
            if (!configured)
            {
                return;
            }

            try
            {
                ValidateLiveRig();
                var started = Stopwatch.GetTimestamp();
                baselineLease.BeginFrame();
                ApplyPose();
                var elapsed = Stopwatch.GetTimestamp() - started;
                totalApplyTicks += elapsed;
                var microseconds = TicksToMicroseconds(elapsed);
                MaximumApplyMicroseconds = Math.Max(MaximumApplyMicroseconds, microseconds);
                PoseApplicationFrameCount++;
            }
            catch (Exception exception)
            {
                LatchFailure("Mounted rider pose application failed.", exception);
            }
        }

        private void ApplyPose()
        {
            var pelvisPosition = pelvis.localPosition;
            var pelvisRotation = pelvis.localRotation;
            pelvis.localPosition = pelvisPosition + ToUnity(profile.PelvisPositionOffset);
            pelvis.localRotation = pelvisRotation * Quaternion.Euler(ToUnity(profile.PelvisEulerOffset));

            if (animatedSaddleRequired)
            {
                // This executes after native animation and the mechanics-only RiderMovementAgent.
                // Project position through the root basis; never inherit the Chest rest quaternion.
                // The seven-bone frame lease restores this visual-only pelvis write before animation.
                if (!IsHealthy) { throw new InvalidOperationException("Owned Horse saddle frame changed."); }
                var localSeat = animatedSaddle.Project(ToPose(saddleMountRoot.InverseTransformPoint(saddleSource.position)));
                LastAnimatedSeatPosition = saddleMountRoot.TransformPoint(ToUnity(localSeat));
                pelvis.position = LastAnimatedSeatPosition;
                MaximumAnimatedSeatError = Math.Max(MaximumAnimatedSeatError,
                    Vector3.Distance(pelvis.position, LastAnimatedSeatPosition));
                AnimatedSaddleSampleCount++;
            }

            ApplyLeg(leftLeg, profile.LeftLeg);
            ApplyLeg(rightLeg, profile.RightLeg);
        }

        private void ApplyLeg(LegBinding binding, MountedRiderLegPoseProfile legProfile)
        {
            var firstLengthBefore = Vector3.Distance(binding.Thigh.position, binding.LowerLeg.position);
            var secondLengthBefore = Vector3.Distance(binding.LowerLeg.position, binding.Foot.position);
            if (firstLengthBefore <= 0.0001f || secondLengthBefore <= 0.0001f)
            {
                throw new InvalidOperationException("A supported rider leg contains a zero-length segment.");
            }

            var evaluatedFootRotation = binding.Foot.rotation;
            var root = binding.Thigh.position;
            var requestedTarget = root + riderView.transform.TransformVector(ToUnity(legProfile.FootTargetFromThigh));
            var bendHint = root + riderView.transform.TransformVector(ToUnity(legProfile.KneeHintFromThigh));
            var solution = TwoBonePoseSolver.Solve(
                ToPose(root),
                ToPose(requestedTarget),
                ToPose(bendHint),
                firstLengthBefore,
                secondLengthBefore);
            if (solution.TargetClamped)
            {
                FootTargetClampCount++;
            }

            var desiredJoint = ToUnity(solution.Joint);
            var desiredTarget = ToUnity(solution.Target);
            var currentThighDirection = binding.LowerLeg.position - binding.Thigh.position;
            var desiredThighDirection = desiredJoint - binding.Thigh.position;
            binding.Thigh.rotation = Quaternion.FromToRotation(currentThighDirection, desiredThighDirection) * binding.Thigh.rotation;

            var currentLowerDirection = binding.Foot.position - binding.LowerLeg.position;
            var desiredLowerDirection = desiredTarget - binding.LowerLeg.position;
            binding.LowerLeg.rotation = Quaternion.FromToRotation(currentLowerDirection, desiredLowerDirection) * binding.LowerLeg.rotation;
            binding.Foot.rotation = evaluatedFootRotation * Quaternion.Euler(ToUnity(legProfile.FootEulerOffset));

            var footResidual = Vector3.Distance(binding.Foot.position, desiredTarget);
            var kneeResidual = Vector3.Distance(binding.LowerLeg.position, desiredJoint);
            var firstLengthAfter = Vector3.Distance(binding.Thigh.position, binding.LowerLeg.position);
            var secondLengthAfter = Vector3.Distance(binding.LowerLeg.position, binding.Foot.position);
            var segmentResidual = Math.Max(
                Math.Abs(firstLengthAfter - firstLengthBefore),
                Math.Abs(secondLengthAfter - secondLengthBefore));
            MaximumFootTargetResidualWorldUnits = Math.Max(MaximumFootTargetResidualWorldUnits, footResidual);
            MaximumKneeTargetResidualWorldUnits = Math.Max(MaximumKneeTargetResidualWorldUnits, kneeResidual);
            MaximumSegmentLengthResidualWorldUnits = Math.Max(MaximumSegmentLengthResidualWorldUnits, segmentResidual);
        }

        private void ValidateLiveRig()
        {
            if (riderView == null || riderView.gameObject != gameObject || riderView.Animator == null || riderView.CharacterAvatar == null ||
                pelvis == null || leftLeg == null || rightLeg == null || baselineLease == null || !baselineLease.IsAcquired)
            {
                throw new InvalidOperationException("The exact rider view or pose baseline was detached/replaced.");
            }
            if (!leftLeg.IsHierarchyValid(pelvis) || !rightLeg.IsHierarchyValid(pelvis))
            {
                throw new InvalidOperationException("The supported rider pelvis/leg hierarchy changed while mounted.");
            }
        }

        private void LatchFailure(string message, Exception exception)
        {
            LastFailure = message + " " + exception.GetType().Name + ": " + exception.Message;
            enabled = false;
        }

        private void OnDisable()
        {
            BestEffortRestore();
        }

        private void OnDestroy()
        {
            configured = false;
            BestEffortRestore();
        }

        private void BestEffortRestore()
        {
            try
            {
                baselineLease?.Restore();
            }
            catch (Exception exception)
            {
                if (string.IsNullOrEmpty(LastFailure))
                {
                    LastFailure = "Pose teardown restoration failed. " + exception.GetType().Name + ": " + exception.Message;
                }
            }
        }

        private static bool TryResolveSupportedRig(
            UnitEntityView exactRiderView,
            MountedRiderPoseProfile exactProfile,
            out Transform exactPelvis,
            out LegBinding exactLeftLeg,
            out LegBinding exactRightLeg,
            out string error)
        {
            exactPelvis = null;
            exactLeftLeg = null;
            exactRightLeg = null;
            error = null;
            if (exactRiderView == null)
            {
                error = "The exact rider view is required for pose-surface validation.";
                return false;
            }
            if (exactProfile == null)
            {
                error = "The exact rider pose profile is required.";
                return false;
            }
            var profileError = exactProfile.Validate();
            if (profileError != null)
            {
                error = "Mounted rider pose profile is invalid: " + profileError;
                return false;
            }
            if (exactRiderView.Animator == null || exactRiderView.CharacterAvatar == null)
            {
                error = "The exact rider view has no active native humanoid animator/character surface.";
                return false;
            }

            var searchRoot = exactRiderView.CharacterAvatar.transform;
            exactPelvis = FindUniqueTransform(searchRoot, exactProfile.PelvisBoneName);
            exactLeftLeg = ResolveLeg(searchRoot, exactPelvis, exactProfile.LeftLeg);
            exactRightLeg = ResolveLeg(searchRoot, exactPelvis, exactProfile.RightLeg);
            if (exactPelvis == null || exactLeftLeg == null || exactRightLeg == null)
            {
                error = "The exact rider does not expose one unambiguous supported Medium humanoid pelvis/leg rig.";
                return false;
            }
            return true;
        }

        private static LegBinding ResolveLeg(Transform searchRoot, Transform exactPelvis, MountedRiderLegPoseProfile leg)
        {
            if (searchRoot == null || exactPelvis == null || leg == null)
            {
                return null;
            }
            var thigh = FindUniqueTransform(searchRoot, leg.ThighBoneName);
            var lowerLeg = FindUniqueTransform(searchRoot, leg.LowerLegBoneName);
            var foot = FindUniqueTransform(searchRoot, leg.FootBoneName);
            var binding = new LegBinding(thigh, lowerLeg, foot);
            return binding.IsHierarchyValid(exactPelvis) ? binding : null;
        }

        private static Transform FindUniqueTransform(Transform root, string exactName)
        {
            Transform found = null;
            var count = 0;
            FindTransforms(root, exactName, ref found, ref count);
            return count == 1 ? found : null;
        }

        private static void FindTransforms(Transform current, string exactName, ref Transform found, ref int count)
        {
            if (current == null || count > 1)
            {
                return;
            }
            if (string.Equals(current.name, exactName, StringComparison.Ordinal))
            {
                found = current;
                count++;
            }
            for (var index = 0; index < current.childCount; index++)
            {
                FindTransforms(current.GetChild(index), exactName, ref found, ref count);
            }
        }

        private static PoseVector3 ToPose(Vector3 value)
        {
            return new PoseVector3(value.x, value.y, value.z);
        }

        private static Vector3 ToUnity(PoseVector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static double TicksToMicroseconds(long ticks)
        {
            return ticks * (1000000d / Stopwatch.Frequency);
        }

        private static ScopedPoseBaselineLease<Transform, Vector3, Quaternion, Vector3> CreateBaselineLease()
        {
            return new ScopedPoseBaselineLease<Transform, Vector3, Quaternion, Vector3>(
                transform => transform.localPosition,
                transform => transform.localRotation,
                transform => transform.localScale,
                (transform, position) => transform.localPosition = position,
                (transform, rotation) => transform.localRotation = rotation,
                (transform, scale) => transform.localScale = scale,
                null,
                new BoundedVector3Comparer(0.0001f),
                new BoundedQuaternionComparer(0.01f),
                new BoundedVector3Comparer(0.0001f));
        }

        private sealed class LegBinding
        {
            public LegBinding(Transform thigh, Transform lowerLeg, Transform foot)
            {
                Thigh = thigh;
                LowerLeg = lowerLeg;
                Foot = foot;
            }

            public Transform Thigh { get; }

            public Transform LowerLeg { get; }

            public Transform Foot { get; }

            public bool IsHierarchyValid(Transform exactPelvis)
            {
                return exactPelvis != null && Thigh != null && LowerLeg != null && Foot != null &&
                    Thigh.IsChildOf(exactPelvis) && LowerLeg.IsChildOf(Thigh) && Foot.IsChildOf(LowerLeg);
            }
        }

        private sealed class BoundedVector3Comparer : IEqualityComparer<Vector3>
        {
            private readonly float maximumDistance;

            public BoundedVector3Comparer(float maximumDistance)
            {
                this.maximumDistance = maximumDistance;
            }

            public bool Equals(Vector3 first, Vector3 second)
            {
                return Vector3.Distance(first, second) <= maximumDistance;
            }

            public int GetHashCode(Vector3 value)
            {
                return 0;
            }
        }

        private sealed class BoundedQuaternionComparer : IEqualityComparer<Quaternion>
        {
            private readonly float maximumAngleDegrees;

            public BoundedQuaternionComparer(float maximumAngleDegrees)
            {
                this.maximumAngleDegrees = maximumAngleDegrees;
            }

            public bool Equals(Quaternion first, Quaternion second)
            {
                return Quaternion.Angle(first, second) <= maximumAngleDegrees;
            }

            public int GetHashCode(Quaternion value)
            {
                return 0;
            }
        }
    }
}
