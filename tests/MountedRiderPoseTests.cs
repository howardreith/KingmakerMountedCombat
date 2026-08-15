using System;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedRiderPoseTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("two-bone pose solver preserves reachable segment lengths", SolverPreservesReachableSegmentLengths);
            runner.Run("two-bone pose solver clamps unreachable target", SolverClampsUnreachableTarget);
            runner.Run("two-bone pose solver follows deterministic bend hint", SolverFollowsBendHint);
            runner.Run("two-bone pose solver rejects invalid lengths", SolverRejectsInvalidLengths);
            runner.Run("pose per-frame state uses allocation-free value types", PosePerFrameStateUsesValueTypes);
            runner.Run("pose frame priming restores before measured frames", PoseFramePrimingRestoresBeforeMeasuredFrames);
            runner.Run("pose frame priming restores after application failure", PoseFramePrimingRestoresAfterApplicationFailure);
            runner.Run("pose baseline lease prevents cumulative frame mutation", PoseLeasePreventsCumulativeMutation);
            runner.Run("pose baseline lease restores exact acquisition values", PoseLeaseRestoresAcquisitionValues);
            runner.Run("pose baseline lease retains failed restoration for retry", PoseLeaseRetainsFailedRestorationForRetry);
            runner.Run("pose baseline lease rejects duplicate nodes", PoseLeaseRejectsDuplicateNodes);
            runner.Run("supported Mammoth rider pose profile validates", SupportedProfileValidates);
            runner.Run("pose profile rejects duplicate bone ownership", ProfileRejectsDuplicateBones);
            runner.Run("pose cost evidence retains the latest cumulative average", PoseCostEvidenceRetainsLatestCumulativeAverage);
            runner.Run("stationary presentation phase coverage accepts LateUpdate only", StationaryPresentationPhaseCoverageAcceptsLateUpdateOnly);
            runner.Run("moving presentation phase coverage requires Update and LateUpdate", MovingPresentationPhaseCoverageRequiresBothPhases);
        }

        private static void SolverPreservesReachableSegmentLengths()
        {
            var root = new PoseVector3(0f, 0f, 0f);
            var solution = TwoBonePoseSolver.Solve(
                root,
                new PoseVector3(1f, 0f, 0f),
                new PoseVector3(0f, 1f, 0f),
                1f,
                1f);
            Near(1f, PoseVector3.Distance(root, solution.Joint), 0.0001f, "First segment length changed.");
            Near(1f, PoseVector3.Distance(solution.Joint, solution.Target), 0.0001f, "Second segment length changed.");
            TestRunner.True(!solution.TargetClamped, "Reachable target was unexpectedly clamped.");
        }

        private static void SolverClampsUnreachableTarget()
        {
            var root = new PoseVector3(0f, 0f, 0f);
            var solution = TwoBonePoseSolver.Solve(
                root,
                new PoseVector3(3f, 0f, 0f),
                new PoseVector3(0f, 1f, 0f),
                1f,
                1f);
            TestRunner.True(solution.TargetClamped, "Unreachable target was not clamped.");
            TestRunner.True(solution.SolvedDistance < 2f && solution.SolvedDistance > 1.99f, "Clamped target left the safe reachable envelope.");
            Near(1f, PoseVector3.Distance(root, solution.Joint), 0.0001f, "First segment length changed after clamping.");
            Near(1f, PoseVector3.Distance(solution.Joint, solution.Target), 0.0002f, "Second segment length changed after clamping.");
        }

        private static void SolverFollowsBendHint()
        {
            var up = TwoBonePoseSolver.Solve(
                new PoseVector3(0f, 0f, 0f),
                new PoseVector3(1f, 0f, 0f),
                new PoseVector3(0f, 1f, 0f),
                1f,
                1f);
            var down = TwoBonePoseSolver.Solve(
                new PoseVector3(0f, 0f, 0f),
                new PoseVector3(1f, 0f, 0f),
                new PoseVector3(0f, -1f, 0f),
                1f,
                1f);
            TestRunner.True(up.Joint.Y > 0f && down.Joint.Y < 0f, "Bend hint did not control the deterministic joint side.");
        }

        private static void SolverRejectsInvalidLengths()
        {
            var threw = false;
            try
            {
                TwoBonePoseSolver.Solve(
                    new PoseVector3(0f, 0f, 0f),
                    new PoseVector3(1f, 0f, 0f),
                    new PoseVector3(0f, 1f, 0f),
                    0f,
                    1f);
            }
            catch (ArgumentOutOfRangeException)
            {
                threw = true;
            }
            TestRunner.True(threw, "Invalid zero-length limb was accepted.");
        }

        private static void PosePerFrameStateUsesValueTypes()
        {
            TestRunner.True(typeof(TwoBonePoseSolution).IsValueType,
                "Two-bone solver retained a per-leg heap allocation.");
            var leaseType = typeof(ScopedPoseBaselineLease<FakeNode, double, double, double>);
            var snapshotType = leaseType.GetNestedType("Snapshot", System.Reflection.BindingFlags.NonPublic);
            TestRunner.True(snapshotType != null && snapshotType.IsValueType,
                "Pose baseline lease retained a per-node per-frame heap allocation.");
        }

        private static void PoseLeasePreventsCumulativeMutation()
        {
            var node = new FakeNode { Position = 1d, Rotation = 2d, Scale = 3d };
            var lease = CreateLease();
            lease.Acquire(new[] { node });
            lease.BeginFrame();
            node.Position += 10d;
            node.Rotation += 20d;
            node.Scale += 30d;

            lease.BeginFrame();
            TestRunner.Equal(1d, node.Position, "Next frame accumulated the prior position mutation.");
            TestRunner.Equal(2d, node.Rotation, "Next frame accumulated the prior rotation mutation.");
            TestRunner.Equal(3d, node.Scale, "Next frame accumulated the prior scale mutation.");
            node.Position = 11d;
            lease.RestoreFrame();
            TestRunner.Equal(1d, node.Position, "Frame baseline did not restore after repeated application.");
        }

        private static void PoseFramePrimingRestoresBeforeMeasuredFrames()
        {
            var node = new FakeNode { Position = 1d, Rotation = 2d, Scale = 3d };
            var lease = CreateLease();
            lease.Acquire(new[] { node });

            lease.PrimeFrame(() =>
            {
                node.Position = 11d;
                node.Rotation = 22d;
                node.Scale = 33d;
            });

            TestRunner.Equal(1d, node.Position, "Pose prime retained its temporary position mutation.");
            TestRunner.Equal(2d, node.Rotation, "Pose prime retained its temporary rotation mutation.");
            TestRunner.Equal(3d, node.Scale, "Pose prime retained its temporary scale mutation.");
            TestRunner.True(lease.IsAcquired && !lease.IsFrameActive && lease.FrameCaptureCount == 1,
                "Pose prime did not retain only the acquisition lease after exact restoration.");

            lease.BeginFrame();
            TestRunner.True(lease.FrameCaptureCount == 2,
                "First measured frame did not begin after the unmeasured reversible prime.");
            lease.Restore();
        }

        private static void PoseFramePrimingRestoresAfterApplicationFailure()
        {
            var node = new FakeNode { Position = 1d, Rotation = 2d, Scale = 3d };
            var lease = CreateLease();
            lease.Acquire(new[] { node });
            var failed = false;
            try
            {
                lease.PrimeFrame(() =>
                {
                    node.Position = 99d;
                    throw new InvalidOperationException("injected prime failure");
                });
            }
            catch (InvalidOperationException exception)
            {
                failed = exception.Message == "injected prime failure";
            }

            TestRunner.True(failed, "Pose prime swallowed or changed the application failure.");
            TestRunner.Equal(1d, node.Position, "Failed pose prime retained its temporary mutation.");
            TestRunner.True(lease.IsAcquired && !lease.IsFrameActive,
                "Failed pose prime did not restore its exact frame before reporting failure.");
            lease.Restore();
        }

        private static void PoseLeaseRestoresAcquisitionValues()
        {
            var node = new FakeNode { Position = 1d, Rotation = 2d, Scale = 3d };
            var lease = CreateLease();
            lease.Acquire(new[] { node });
            lease.BeginFrame();
            node.Position = 99d;
            lease.RestoreFrame();
            node.Position = 4d;
            node.Rotation = 5d;
            node.Scale = 6d;
            lease.BeginFrame();
            node.Position = 100d;
            lease.Restore();
            TestRunner.Equal(1d, node.Position, "Acquisition position was not restored.");
            TestRunner.Equal(2d, node.Rotation, "Acquisition rotation was not restored.");
            TestRunner.Equal(3d, node.Scale, "Acquisition scale was not restored.");
            TestRunner.True(lease.LastRestoreVerified && !lease.IsAcquired && !lease.IsFrameActive, "Successful pose cleanup retained lease state.");
        }

        private static void PoseLeaseRejectsDuplicateNodes()
        {
            var node = new FakeNode();
            var lease = CreateLease();
            var threw = false;
            try { lease.Acquire(new[] { node, node }); }
            catch (ArgumentException) { threw = true; }
            TestRunner.True(threw, "Duplicate pose node ownership was accepted.");
            TestRunner.True(!lease.IsAcquired, "Rejected pose acquisition retained ownership.");
        }

        private static void PoseLeaseRetainsFailedRestorationForRetry()
        {
            var node = new FakeNode { Position = 1d, Rotation = 2d, Scale = 3d };
            var failPositionRestore = true;
            var lease = new ScopedPoseBaselineLease<FakeNode, double, double, double>(
                value => value.Position,
                value => value.Rotation,
                value => value.Scale,
                (value, position) =>
                {
                    if (failPositionRestore)
                    {
                        failPositionRestore = false;
                        throw new InvalidOperationException("injected position restore failure");
                    }
                    value.Position = position;
                },
                (value, rotation) => value.Rotation = rotation,
                (value, scale) => value.Scale = scale);
            lease.Acquire(new[] { node });
            lease.BeginFrame();
            node.Position = 9d;
            node.Rotation = 10d;

            var failed = false;
            try { lease.Restore(); }
            catch (AggregateException) { failed = true; }
            TestRunner.True(failed, "Injected pose restoration failure was not reported.");
            TestRunner.True(lease.IsAcquired && !lease.LastRestoreVerified,
                "Failed pose restoration discarded its retryable acquisition snapshot.");

            lease.Restore();
            TestRunner.Equal(1d, node.Position, "Pose cleanup retry did not restore acquisition position.");
            TestRunner.Equal(2d, node.Rotation, "Pose cleanup retry did not restore acquisition rotation.");
            TestRunner.Equal(3d, node.Scale, "Pose cleanup retry did not restore acquisition scale.");
            TestRunner.True(!lease.IsAcquired && lease.LastRestoreVerified,
                "Successful pose cleanup retry retained lease state.");
        }

        private static void SupportedProfileValidates()
        {
            TestRunner.Equal(null, MountedRiderPoseProfiles.MediumHumanoidOnMammoth.Validate(), "Supported pose profile is invalid.");
            TestRunner.Equal("medium-humanoid-mammoth-v1", MountedRiderPoseProfiles.MediumHumanoidOnMammoth.Id, "Supported profile identity changed.");
        }

        private static void ProfileRejectsDuplicateBones()
        {
            var left = new MountedRiderLegPoseProfile(
                "shared",
                "left-knee",
                "left-foot",
                new PoseVector3(-0.2f, -0.4f, 0f),
                new PoseVector3(-0.3f, 0f, 0.3f),
                new PoseVector3(0f, 0f, 0f));
            var right = new MountedRiderLegPoseProfile(
                "shared",
                "right-knee",
                "right-foot",
                new PoseVector3(0.2f, -0.4f, 0f),
                new PoseVector3(0.3f, 0f, 0.3f),
                new PoseVector3(0f, 0f, 0f));
            var profile = new MountedRiderPoseProfile(
                "invalid",
                "pelvis",
                new PoseVector3(0f, 0f, 0f),
                new PoseVector3(0f, 0f, 0f),
                left,
                right);
            TestRunner.True(profile.Validate() != null, "Profile with duplicate bone ownership was accepted.");
        }

        private static void PoseCostEvidenceRetainsLatestCumulativeAverage()
        {
            var average = PresentationRuntimeEvidencePolicy.SelectLatestCumulativeAverage(0L, 0.0d, 1L, 1560.6d);
            average = PresentationRuntimeEvidencePolicy.SelectLatestCumulativeAverage(1L, average, 209L, 18.5d);
            TestRunner.Equal(18.5d, average, "First-frame warm-up cost contaminated the completed cumulative average.");
            TestRunner.Equal(
                18.5d,
                PresentationRuntimeEvidencePolicy.SelectLatestCumulativeAverage(209L, average, 209L, 999.0d),
                "An observation without a new pose frame changed the completed cumulative average.");
        }

        private static void StationaryPresentationPhaseCoverageAcceptsLateUpdateOnly()
        {
            foreach (var row in new[] { "pose-idle", "pose-equipment-variants", "ui-selection-portrait-actionbar" })
            {
                TestRunner.True(
                    PresentationRuntimeEvidencePolicy.HasRequiredSynchronizationPhaseCoverage(row, 0L, 10L),
                    "Stationary row rejected continuous LateUpdate synchronization: " + row + ".");
            }
            TestRunner.True(
                !PresentationRuntimeEvidencePolicy.HasRequiredSynchronizationPhaseCoverage("pose-idle", 0L, 0L),
                "Stationary row accepted no synchronization samples.");
        }

        private static void MovingPresentationPhaseCoverageRequiresBothPhases()
        {
            TestRunner.True(
                !PresentationRuntimeEvidencePolicy.HasRequiredSynchronizationPhaseCoverage("pose-walk-run", 0L, 10L),
                "Moving row accepted LateUpdate-only synchronization.");
            TestRunner.True(
                !PresentationRuntimeEvidencePolicy.HasRequiredSynchronizationPhaseCoverage("pose-walk-run", 10L, 0L),
                "Moving row accepted Update-only synchronization.");
            TestRunner.True(
                PresentationRuntimeEvidencePolicy.HasRequiredSynchronizationPhaseCoverage("pose-walk-run", 10L, 10L),
                "Moving row rejected complete synchronization phase coverage.");
        }

        private static ScopedPoseBaselineLease<FakeNode, double, double, double> CreateLease()
        {
            return new ScopedPoseBaselineLease<FakeNode, double, double, double>(
                node => node.Position,
                node => node.Rotation,
                node => node.Scale,
                (node, value) => node.Position = value,
                (node, value) => node.Rotation = value,
                (node, value) => node.Scale = value);
        }

        private static void Near(float expected, float actual, float tolerance, string message)
        {
            if (Math.Abs(expected - actual) > tolerance)
            {
                throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual);
            }
        }

        private sealed class FakeNode
        {
            public double Position { get; set; }

            public double Rotation { get; set; }

            public double Scale { get; set; }
        }
    }
}
