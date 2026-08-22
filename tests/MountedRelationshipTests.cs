using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedRelationshipTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("relationship valid mount transition", ValidMountTransition);
            runner.Run("relationship invalid same-unit pair", InvalidSameUnitPair);
            runner.Run("relationship invalid dead rider", InvalidDeadRider);
            runner.Run("relationship invalid incapacitated mount", InvalidIncapacitatedMount);
            runner.Run("relationship invalid size relationship", InvalidSizeRelationship);
            runner.Run("relationship invalid non-companion pair", InvalidNonCompanionPair);
            runner.Run("relationship invalid non-Medium rider", InvalidNonMediumRider);
            runner.Run("relationship invalid unsafe game mode", InvalidNonDefaultMode);
            runner.Run("relationship invalid mount override", InvalidMountOverride);
            runner.Run("relationship double mount rejection", DoubleMountRejection);
            runner.Run("relationship dismount idempotence", DismountIdempotence);
            runner.Run("relationship mount failure rollback", MountFailureRollback);
            runner.Run("relationship invalidation during mounting", InvalidationDuringMounting);
            runner.Run("relationship combat-start cleanup", () => AssertCleanup(CleanupTrigger.CombatStarted));
            runner.Run("relationship view-detach cleanup", () => AssertCleanup(CleanupTrigger.ViewDetached));
            runner.Run("relationship area-unload cleanup", () => AssertCleanup(CleanupTrigger.AreaUnloading));
            runner.Run("relationship death cleanup", () => AssertCleanup(CleanupTrigger.Death));
            runner.Run("relationship mod-disable cleanup", ModDisableCleanup);
            runner.Run("relationship exception cleanup", ExceptionCleanup);
            runner.Run("relationship partial dismount continues best effort", PartialDismountContinuesBestEffort);
            runner.Run("relationship faulted cleanup can be retried idempotently", FaultedCleanupCanBeRetried);
            runner.Run("relationship cleanup diagnostics retain bounded inner cause", CleanupDiagnosticsRetainBoundedInnerCause);
            runner.Run("avoidance restoration separates KMC lease from native consciousness", AvoidanceRestorationSeparatesNativeConsciousness);
            runner.Run("command routing rewrites only active rider", CommandRoutingRewritesOnlyActiveRider);
            runner.Run("command routing suppresses only duplicate mount", CommandRoutingSuppressesOnlyDuplicateMount);
            runner.Run("cleanup trigger priority is deterministic", CleanupTriggerPriorityIsDeterministic);
            runner.Run("movement residual uses three-dimensional distance", MovementResidualUsesThreeDimensionalDistance);
            runner.Run("movement residual threshold is inclusive", MovementResidualThresholdIsInclusive);
            runner.Run("rotation residual wraps at 360 degrees", RotationResidualWrapsAt360Degrees);
            runner.Run("movement telemetry accumulator records maxima and violations", MovementTelemetryAccumulatorRecordsMaxima);
            runner.Run("movement synchronization telemetry preserves pre and post correction residuals", MovementSynchronizationTelemetryPreservesPreAndPostResiduals);
            runner.Run("movement synchronization telemetry separates update phases and corrections", MovementSynchronizationTelemetrySeparatesPhasesAndCorrections);
            runner.Run("movement synchronization telemetry rejects noncontiguous samples", MovementSynchronizationTelemetryRejectsNoncontiguousSamples);
            runner.Run("movement synchronization qualification excludes initial placement", MovementSynchronizationQualificationExcludesInitialPlacement);
            runner.Run("movement synchronization qualification gates calibrated phases", MovementSynchronizationQualificationGatesCalibratedPhases);
            runner.Run("movement synchronization qualification bounds callback cadence", MovementSynchronizationQualificationBoundsCallbackCadence);
            runner.Run("position phase tracker accepts one same-frame entity lag and next-update recovery", PositionPhaseTrackerAcceptsOneSameFrameLagAndRecovery);
            runner.Run("position phase tracker rejects stale visible stationary and excess lag", PositionPhaseTrackerRejectsUnsafeLag);
            runner.Run("position phase-aware qualification retains raw lag while gating effective state", PositionPhaseAwareQualificationRetainsRawLag);
            runner.Run("yaw phase tracker accepts one same-frame entity lag and next-update recovery", YawPhaseTrackerAcceptsOneSameFrameLagAndRecovery);
            runner.Run("yaw phase tracker rejects stale, visible, and stationary lag", YawPhaseTrackerRejectsUnsafeLag);
            runner.Run("yaw phase-aware qualification retains raw lag while gating adjusted state", YawPhaseAwareQualificationRetainsRawLag);
            runner.Run("yaw phase-aware qualification rejects mount entity-root incoherence", YawPhaseAwareQualificationRejectsMountEntityRootIncoherence);
            runner.Run("yaw phase-aware qualification rejects full view quaternion drift", YawPhaseAwareQualificationRejectsFullViewQuaternionDrift);
            runner.Run("movement synchronization boundary snapshot retains final current residuals", MovementSynchronizationBoundarySnapshotRetainsFinalResiduals);
            runner.Run("stationary boundary closure reconciles pending channels without fabricating update", StationaryBoundaryClosureReconcilesWithoutFabricatedUpdate);
            runner.Run("stationary boundary closure rejects repeated moving advanced and residual boundaries", StationaryBoundaryClosureRejectsUnsafeBoundaries);
            runner.Run("view attachment lease restores exact captured transform state", ViewAttachmentLeaseRestoresExactState);
            runner.Run("view attachment lease cleanup is idempotent", ViewAttachmentLeaseCleanupIsIdempotent);
            runner.Run("view attachment lease retains snapshot for cleanup retry", ViewAttachmentLeaseRetainsSnapshotForRetry);
            runner.Run("view attachment lease uses injected bounded restoration comparers", ViewAttachmentLeaseUsesInjectedBoundedComparers);
            runner.Run("view attachment lease releases an inherited replacement before anchor cleanup", ViewAttachmentLeaseReleasesInheritedReplacement);
        }

        private static void ValidMountTransition()
        {
            var runtime = new FakeRuntime();
            var coordinator = new MountedRelationshipCoordinator(runtime);
            var result = coordinator.Mount(ValidCandidate());
            TestRunner.True(result.Succeeded, "Valid mount failed.");
            TestRunner.Equal(RelationshipState.Mounted, coordinator.State, "State is not mounted.");
            TestRunner.Equal(1, runtime.AcquireCalls, "Movement authority was not acquired exactly once.");
            TestRunner.Equal(1, runtime.AttachCalls, "Presentation was not attached exactly once.");
        }

        private static void InvalidSameUnitPair()
        {
            var candidate = ValidCandidate();
            candidate = CopyCandidate(candidate, "same", "same");
            AssertRejected(candidate);
        }

        private static void InvalidDeadRider()
        {
            var candidate = ValidCandidate();
            candidate.RiderIsAliveAndConscious = false;
            AssertRejected(candidate);
        }

        private static void InvalidIncapacitatedMount()
        {
            var candidate = ValidCandidate();
            candidate.MountIsAliveAndConscious = false;
            AssertRejected(candidate);
        }

        private static void InvalidSizeRelationship()
        {
            var candidate = ValidCandidate();
            candidate.MountSizeOrdinal = candidate.RiderSizeOrdinal;
            AssertRejected(candidate);
        }

        private static void InvalidNonCompanionPair()
        {
            var candidate = ValidCandidate();
            candidate.ExactReciprocalCompanionRelationship = false;
            AssertRejected(candidate);
        }

        private static void InvalidNonMediumRider()
        {
            var candidate = ValidCandidate();
            candidate.RiderIsExactlyMedium = false;
            AssertRejected(candidate);
        }

        private static void InvalidNonDefaultMode()
        {
            var candidate = ValidCandidate();
            candidate.SafeMovementMode = false;
            AssertRejected(candidate);
        }

        private static void InvalidMountOverride()
        {
            var candidate = ValidCandidate();
            candidate.MountAgentOverrideAvailable = false;
            AssertRejected(candidate);
        }

        private static void DoubleMountRejection()
        {
            var runtime = new FakeRuntime();
            var coordinator = Mounted(runtime);
            var result = coordinator.Mount(ValidCandidate());
            TestRunner.True(!result.Succeeded, "Second mount was accepted.");
            TestRunner.Equal(RelationshipState.Mounted, coordinator.State, "Double mount changed active state.");
            TestRunner.Equal(1, runtime.AcquireCalls, "Double mount mutated runtime.");
        }

        private static void DismountIdempotence()
        {
            var runtime = new FakeRuntime();
            var coordinator = Mounted(runtime);
            var first = coordinator.Dismount(CleanupTrigger.Manual);
            var second = coordinator.Dismount(CleanupTrigger.Manual);
            TestRunner.True(first.Succeeded && second.Succeeded, "Idempotent dismount failed.");
            TestRunner.Equal(1, runtime.RestorePresentationCalls, "Presentation restored more than once.");
            TestRunner.Equal(1, runtime.RestoreAuthorityCalls, "Movement authority restored more than once.");
            TestRunner.Equal(null, coordinator.ActivePair, "Pair survived cleanup.");
        }

        private static void MountFailureRollback()
        {
            var runtime = new FakeRuntime { ThrowOnAttach = true };
            var coordinator = new MountedRelationshipCoordinator(runtime);
            var result = coordinator.Mount(ValidCandidate());
            TestRunner.True(!result.Succeeded, "Mount failure reported success.");
            TestRunner.Equal(RelationshipState.Unmounted, coordinator.State, "Successful rollback did not return to Unmounted.");
            TestRunner.Equal(1, runtime.RestoreAuthorityCalls, "Partial movement mutation was not rolled back.");
            TestRunner.Equal(1, runtime.RestorePresentationCalls, "Partially attempted presentation was not rolled back.");
            TestRunner.True(!result.MovementAuthorityResidual && !result.PresentationResidual, "Rollback left residue.");
        }

        private static void InvalidationDuringMounting()
        {
            var runtime = new FakeRuntime();
            var coordinator = new MountedRelationshipCoordinator(runtime);
            runtime.OnAcquire = () => coordinator.Dismount(CleanupTrigger.CombatStarted);
            var result = coordinator.Mount(ValidCandidate());
            TestRunner.True(!result.Succeeded, "Mounting continued after invalidation.");
            TestRunner.Equal(RelationshipState.Unmounted, coordinator.State, "Invalidation rollback did not unmount.");
            TestRunner.Equal(CleanupTrigger.CombatStarted, result.Trigger.Value, "Invalidation trigger was lost.");
            TestRunner.Equal(1, runtime.RestoreAuthorityCalls, "Authority was not restored after reentrant invalidation.");
        }

        private static void AssertCleanup(CleanupTrigger trigger)
        {
            var runtime = new FakeRuntime();
            var coordinator = Mounted(runtime);
            var result = coordinator.Dismount(trigger);
            TestRunner.True(result.Succeeded, trigger + " cleanup failed.");
            TestRunner.Equal(RelationshipState.Unmounted, coordinator.State, trigger + " did not unmount.");
            TestRunner.Equal(trigger, result.Trigger.Value, "Cleanup trigger mismatch.");
            TestRunner.Equal(trigger, runtime.LastRestoreTrigger.Value, "Runtime cleanup did not receive the exact trigger.");
            TestRunner.True(!result.MovementAuthorityResidual && !result.PresentationResidual, "Cleanup left residue.");
        }

        private static void ModDisableCleanup()
        {
            var runtime = new FakeRuntime();
            var coordinator = Mounted(runtime);
            coordinator.Dispose();
            coordinator.Dispose();
            TestRunner.Equal(RelationshipState.Disposed, coordinator.State, "Coordinator did not dispose.");
            TestRunner.Equal(1, runtime.RestorePresentationCalls, "Dispose restored presentation more than once.");
            TestRunner.Equal(1, runtime.RestoreAuthorityCalls, "Dispose restored authority more than once.");
        }

        private static void ExceptionCleanup()
        {
            var runtime = new FakeRuntime { ThrowOnAttach = true };
            var coordinator = new MountedRelationshipCoordinator(runtime);
            var result = coordinator.Mount(ValidCandidate());
            TestRunner.Equal(CleanupTrigger.Exception, result.Trigger.Value, "Exception cleanup trigger missing.");
            TestRunner.True(result.Errors.Count > 0, "Exception was not reported.");
            TestRunner.True(!result.MovementAuthorityResidual, "Exception cleanup left authority residue.");
        }

        private static void PartialDismountContinuesBestEffort()
        {
            var runtime = new FakeRuntime { ThrowOnRestorePresentation = true };
            var coordinator = Mounted(runtime);
            var result = coordinator.Dismount(CleanupTrigger.ViewDetached);
            TestRunner.True(!result.Succeeded, "Partial cleanup reported success.");
            TestRunner.Equal(RelationshipState.Faulted, coordinator.State, "Partial cleanup did not fault.");
            TestRunner.True(result.PresentationResidual, "Presentation residue was not reported.");
            TestRunner.True(!result.MovementAuthorityResidual, "Movement cleanup did not continue after presentation failure.");
            TestRunner.Equal(1, runtime.RestoreAuthorityCalls, "Best-effort authority cleanup was skipped.");
        }

        private static void FaultedCleanupCanBeRetried()
        {
            var runtime = new FakeRuntime { RestorePresentationFailuresRemaining = 1 };
            var coordinator = Mounted(runtime);
            var first = coordinator.Dismount(CleanupTrigger.CombatStarted);
            TestRunner.Equal(RelationshipState.Faulted, first.State, "First transient cleanup failure did not retain a retryable Faulted state.");
            TestRunner.True(first.PresentationResidual, "First transient cleanup failure lost its presentation residue.");

            var retry = coordinator.Dismount(CleanupTrigger.CombatStarted);
            TestRunner.True(retry.Succeeded, "Second bounded cleanup attempt did not recover a transient failure.");
            TestRunner.Equal(RelationshipState.Unmounted, coordinator.State, "Cleanup retry did not reach Unmounted.");
            TestRunner.True(!retry.PresentationResidual && !retry.MovementAuthorityResidual, "Cleanup retry retained residue.");
            TestRunner.Equal(2, runtime.RestorePresentationCalls, "Cleanup retry did not reattempt only the retained presentation operation.");
            TestRunner.Equal(1, runtime.RestoreAuthorityCalls, "Already-restored movement authority was repeated during cleanup retry.");
        }

        private static void CleanupDiagnosticsRetainBoundedInnerCause()
        {
            var deepest = new InvalidOperationException("exact movement sub-operation failed");
            Exception nested = deepest;
            for (var index = 0; index < 9; index++)
            {
                nested = new InvalidOperationException("wrapper " + index, nested);
            }
            var runtime = new FakeRuntime { RestoreAuthorityException = nested };
            var coordinator = Mounted(runtime);

            var result = coordinator.Dismount(CleanupTrigger.Incapacitated);

            TestRunner.Equal(RelationshipState.Faulted, result.State, "Nested cleanup failure did not retain a retryable fault.");
            TestRunner.True(result.Errors[0].StartsWith("RestoreMovementAuthority: InvalidOperationException: wrapper 8 <- InvalidOperationException: wrapper 7", StringComparison.Ordinal),
                "Cleanup diagnostics lost the ordered outer and inner exception identities.");
            TestRunner.True(result.Errors[0].EndsWith("additional inner exceptions omitted", StringComparison.Ordinal),
                "Cleanup diagnostics did not enforce the bounded inner-exception limit.");
            TestRunner.True(!result.Errors[0].Contains("exact movement sub-operation failed"),
                "Cleanup diagnostics exceeded the bounded inner-exception limit.");
        }

        private static void AvoidanceRestorationSeparatesNativeConsciousness()
        {
            TestRunner.True(AvoidanceRestorationExpectation.Matches(false, true, false),
                "Ordinary captured-enabled avoidance restoration was rejected.");
            TestRunner.True(AvoidanceRestorationExpectation.Matches(false, false, true),
                "Native unconsciousness was treated as retained KMC avoidance residue.");
            TestRunner.True(AvoidanceRestorationExpectation.Matches(true, true, true),
                "A captured foreign avoidance guard was not preserved.");
            TestRunner.True(AvoidanceRestorationExpectation.Matches(true, false, true),
                "Captured and native avoidance ownership did not compose.");
            TestRunner.True(!AvoidanceRestorationExpectation.Matches(false, true, true),
                "Unexplained avoidance drift while conscious was accepted.");
            TestRunner.True(!AvoidanceRestorationExpectation.Matches(false, false, false),
                "An impossible enabled-while-unconscious getter state was accepted.");
            TestRunner.True(!AvoidanceRestorationExpectation.Matches(true, true, false),
                "A captured foreign avoidance guard was silently lost.");
        }

        private static void CommandRoutingRewritesOnlyActiveRider()
        {
            var pair = new MountedPair("rider", "mount");
            var rider = CommandRouter.RouteGroundMove(pair, "rider");
            var unrelated = CommandRouter.RouteGroundMove(pair, "ally");
            TestRunner.Equal(CommandRoutingAction.RewriteRiderToMount, rider.Action, "Rider was not rewritten.");
            TestRunner.Equal("mount", rider.EffectiveUnitId, "Rider destination did not target mount.");
            TestRunner.Equal(CommandRoutingAction.Unchanged, unrelated.Action, "Unrelated unit was changed.");
            TestRunner.Equal("ally", unrelated.EffectiveUnitId, "Unrelated recipient changed.");
        }

        private static void CommandRoutingSuppressesOnlyDuplicateMount()
        {
            var pair = new MountedPair("rider", "mount");
            var mount = CommandRouter.RouteGroundMove(pair, "mount");
            var noPair = CommandRouter.RouteGroundMove(null, "mount");
            TestRunner.Equal(CommandRoutingAction.SuppressDuplicateMount, mount.Action, "Duplicate mount row was not suppressed.");
            TestRunner.Equal(null, mount.EffectiveUnitId, "Suppressed command retained a recipient.");
            TestRunner.Equal(CommandRoutingAction.Unchanged, noPair.Action, "No-pair path was modified.");
        }

        private static void CleanupTriggerPriorityIsDeterministic()
        {
            TestRunner.Equal(CleanupTrigger.AreaUnloading, CleanupTriggerPriority.Higher(CleanupTrigger.Manual, CleanupTrigger.AreaUnloading), "Higher cleanup trigger lost.");
            TestRunner.Equal(CleanupTrigger.ProcessTeardown, CleanupTriggerPriority.Higher(CleanupTrigger.ProcessTeardown, CleanupTrigger.Death), "Priority was order-dependent.");
        }

        private static void MovementResidualUsesThreeDimensionalDistance()
        {
            TestRunner.Equal(13.0d, MovementTelemetrySample.CalculateDistance(0, 0, 0, 3, 4, 12), "Position residual is not Euclidean.");
        }

        private static void MovementResidualThresholdIsInclusive()
        {
            var sample = new MovementTelemetrySample(0, 0, 0, 0, 0.1, 0, 0, 0, 0, 0.1);
            TestRunner.True(sample.WithinPositionThreshold, "Exact 0.10 residual failed the calibrated threshold.");
        }

        private static void RotationResidualWrapsAt360Degrees()
        {
            TestRunner.Equal(2.0d, MovementTelemetrySample.CalculateAngleDelta(359, 1), "Rotation residual did not wrap.");
        }

        private static void MovementTelemetryAccumulatorRecordsMaxima()
        {
            var accumulator = new MovementTelemetryAccumulator();
            accumulator.Observe(new MovementTelemetrySample(0, 0, 0, 0, 0.05, 0, 0, 0, 1, 0.1));
            accumulator.Observe(new MovementTelemetrySample(1, 0, 0, 0, 0.2, 0, 0, 0, 5, 0.1));
            TestRunner.Equal(2L, accumulator.SampleCount, "Sample count differs.");
            TestRunner.Equal(0.2d, accumulator.MaximumPositionResidualWorldUnits, "Maximum position residual differs.");
            TestRunner.Equal(5.0d, accumulator.MaximumRotationResidualDegrees, "Maximum rotation residual differs.");
            TestRunner.Equal(1, accumulator.PositionThresholdViolationCount, "Threshold violation count differs.");
        }

        private static void MovementSynchronizationTelemetryPreservesPreAndPostResiduals()
        {
            var accumulator = new MovementSynchronizationTelemetryAccumulator();
            accumulator.Observe(new MovementSynchronizationSample(0, MovementSynchronizationPhase.Update, 0.25d, 12.0d, 0.01d, 0.5d));
            accumulator.Observe(new MovementSynchronizationSample(1, MovementSynchronizationPhase.LateUpdate, 0.05d, 2.0d, 0.0d, 0.0d));

            TestRunner.Equal(0.05d, accumulator.LatestPreCorrectionPositionResidualWorldUnits, "Latest pre-correction position residual differs.");
            TestRunner.Equal(2.0d, accumulator.LatestPreCorrectionRotationResidualDegrees, "Latest pre-correction rotation residual differs.");
            TestRunner.Equal(0.0d, accumulator.LatestPostCorrectionPositionResidualWorldUnits, "Latest post-correction position residual differs.");
            TestRunner.Equal(0.0d, accumulator.LatestPostCorrectionRotationResidualDegrees, "Latest post-correction rotation residual differs.");
            TestRunner.Equal(0.25d, accumulator.MaximumPreCorrectionPositionResidualWorldUnits, "Pre-correction position maximum was lost.");
            TestRunner.Equal(12.0d, accumulator.MaximumPreCorrectionRotationResidualDegrees, "Pre-correction rotation maximum was lost.");
            TestRunner.Equal(0.01d, accumulator.MaximumPostCorrectionPositionResidualWorldUnits, "Post-correction position maximum differs.");
            TestRunner.Equal(0.5d, accumulator.MaximumPostCorrectionRotationResidualDegrees, "Post-correction rotation maximum differs.");
        }

        private static void MovementSynchronizationTelemetrySeparatesPhasesAndCorrections()
        {
            var accumulator = new MovementSynchronizationTelemetryAccumulator();
            accumulator.Observe(new MovementSynchronizationSample(0, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d));
            accumulator.Observe(new MovementSynchronizationSample(1, MovementSynchronizationPhase.Update, 0.2d, 0.0d, 0.0d, 0.0d));
            accumulator.Observe(new MovementSynchronizationSample(2, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d));
            accumulator.Observe(new MovementSynchronizationSample(3, MovementSynchronizationPhase.LateUpdate, 0.0d, 3.0d, 0.0d, 0.0d));

            TestRunner.Equal(4L, accumulator.SampleCount, "Synchronization sample count differs.");
            TestRunner.Equal(2L, accumulator.CorrectionCount, "Synchronization correction count differs.");
            TestRunner.Equal(1L, accumulator.InitialConfigurationSampleCount, "Initial-configuration sample count differs.");
            TestRunner.Equal(0L, accumulator.InitialConfigurationCorrectionCount, "Initial-configuration correction count differs.");
            TestRunner.Equal(2L, accumulator.UpdateSampleCount, "Update sample count differs.");
            TestRunner.Equal(1L, accumulator.UpdateCorrectionCount, "Update correction count differs.");
            TestRunner.Equal(1L, accumulator.LateUpdateSampleCount, "LateUpdate sample count differs.");
            TestRunner.Equal(1L, accumulator.LateUpdateCorrectionCount, "LateUpdate correction count differs.");
            TestRunner.Equal(MovementSynchronizationPhase.LateUpdate, accumulator.LatestPhase, "Latest synchronization phase differs.");
            TestRunner.Equal(0.2d, accumulator.MaximumUpdatePreCorrectionPositionResidualWorldUnits, "Update pre-correction position maximum differs.");
            TestRunner.Equal(0.0d, accumulator.MaximumUpdatePreCorrectionRotationResidualDegrees, "Update pre-correction rotation maximum differs.");
            TestRunner.Equal(0.0d, accumulator.MaximumUpdatePostCorrectionPositionResidualWorldUnits, "Update post-correction position maximum differs.");
            TestRunner.Equal(0.0d, accumulator.MaximumUpdatePostCorrectionRotationResidualDegrees, "Update post-correction rotation maximum differs.");
            TestRunner.Equal(0.0d, accumulator.MaximumLateUpdatePreCorrectionPositionResidualWorldUnits, "LateUpdate pre-correction position maximum differs.");
            TestRunner.Equal(3.0d, accumulator.MaximumLateUpdatePreCorrectionRotationResidualDegrees, "LateUpdate pre-correction rotation maximum differs.");
            TestRunner.Equal(0.0d, accumulator.MaximumLateUpdatePostCorrectionPositionResidualWorldUnits, "LateUpdate post-correction position maximum differs.");
            TestRunner.Equal(0.0d, accumulator.MaximumLateUpdatePostCorrectionRotationResidualDegrees, "LateUpdate post-correction rotation maximum differs.");
        }

        private static void MovementSynchronizationTelemetryRejectsNoncontiguousSamples()
        {
            var accumulator = new MovementSynchronizationTelemetryAccumulator();
            var rejected = false;
            try
            {
                accumulator.Observe(new MovementSynchronizationSample(1, MovementSynchronizationPhase.Update, 0.1d, 0.0d, 0.0d, 0.0d));
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            TestRunner.True(rejected, "Noncontiguous synchronization sample was accepted.");
            TestRunner.Equal(0L, accumulator.SampleCount, "Rejected synchronization sample mutated the accumulator.");
        }

        private static void MovementSynchronizationQualificationExcludesInitialPlacement()
        {
            var accumulator = new MovementSynchronizationTelemetryAccumulator();
            accumulator.Observe(new MovementSynchronizationSample(0, MovementSynchronizationPhase.InitialConfiguration, 15.0d, 90.0d, 0.0d, 0.0d));
            accumulator.Observe(new MovementSynchronizationSample(1, MovementSynchronizationPhase.Update, 0.10d, 0.0d, 0.0d, 0.0d));
            accumulator.Observe(new MovementSynchronizationSample(2, MovementSynchronizationPhase.LateUpdate, 0.09d, 0.0d, 0.0d, 0.0d));

            var qualification = MovementSynchronizationQualification.Evaluate(accumulator, 1L, 0.10d, 0.10d);
            TestRunner.Equal(0.10d, qualification.MaximumCalibratedPreCorrectionPositionResidualWorldUnits, "Initial placement polluted the calibrated maximum.");
            TestRunner.True(qualification.PreCorrectionPositionPassed, "Exact calibrated threshold did not pass.");
            TestRunner.True(qualification.PreCorrectionRotationPassed, "Initial-placement rotation polluted the calibrated rotation gate.");
            TestRunner.True(qualification.PostCorrectionPositionPassed, "Post-correction position unexpectedly failed.");
            TestRunner.True(qualification.PostCorrectionRotationPassed, "Post-correction rotation unexpectedly failed.");
            TestRunner.True(qualification.CorrectionCadencePassed, "Ordinary callback cadence unexpectedly failed.");
        }

        private static void MovementSynchronizationQualificationGatesCalibratedPhases()
        {
            var accumulator = new MovementSynchronizationTelemetryAccumulator();
            accumulator.Observe(new MovementSynchronizationSample(0, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d));
            accumulator.Observe(new MovementSynchronizationSample(1, MovementSynchronizationPhase.Update, 0.100001d, 160.0d, 0.0d, 0.0d));
            accumulator.Observe(new MovementSynchronizationSample(2, MovementSynchronizationPhase.LateUpdate, 0.0d, 0.0d, 0.11d, 0.11d));

            var qualification = MovementSynchronizationQualification.Evaluate(accumulator, 1L, 0.10d, 0.10d);
            TestRunner.True(!qualification.PreCorrectionPositionPassed, "Above-threshold Update residual passed.");
            TestRunner.True(!qualification.PreCorrectionRotationPassed, "Above-threshold ongoing Update rotation snap passed.");
            TestRunner.True(!qualification.PostCorrectionPositionPassed, "Above-threshold post-correction position passed.");
            TestRunner.True(!qualification.PostCorrectionRotationPassed, "Above-threshold post-correction rotation passed.");
        }

        private static void MovementSynchronizationQualificationBoundsCallbackCadence()
        {
            var accumulator = new MovementSynchronizationTelemetryAccumulator();
            for (var sequence = 0; sequence < 4; sequence++)
            {
                accumulator.Observe(new MovementSynchronizationSample(sequence, MovementSynchronizationPhase.Update, 0.01d, 0.0d, 0.0d, 0.0d));
            }

            var qualification = MovementSynchronizationQualification.Evaluate(accumulator, 1L, 0.10d, 0.10d);
            TestRunner.Equal(3L, qualification.MaximumSamplesPerPhase, "Callback cadence allowance differs.");
            TestRunner.Equal(6L, qualification.MaximumCorrectionsAcrossCalibratedPhases, "Correction-count allowance differs.");
            TestRunner.True(!qualification.CorrectionCadencePassed, "Unbounded Update callback cadence passed.");
        }

        private static void PositionPhaseTrackerAcceptsOneSameFrameLagAndRecovery()
        {
            var tracker = new MovementPositionPhaseTracker();
            tracker.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            tracker.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var late = tracker.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);

            TestRunner.True(late.PhaseLagObserved, "Same-frame logical entity position lag was not observed.");
            TestRunner.True(late.PhaseLagPermitted, "Immediately previous same-frame anchor position was not permitted.");
            TestRunner.True(!late.PhaseLagViolation, "Permitted one-phase position lag was marked invalid.");
            TestRunner.Equal(0.20d, late.EntityRawCurrentPositionResidualWorldUnits, "Raw current entity position lag was not retained.");
            TestRunner.Equal(0.0d, late.EntityPreviousAuthoritativePositionResidualWorldUnits.Value, "Previous-anchor entity residual differs.");
            TestRunner.Equal(0.0d, late.EntityPhaseAdjustedPositionResidualWorldUnits, "Phase-adjusted entity position residual differs.");
            TestRunner.Equal(1L, late.EntityPositionAuthorityAgeSteps.Value, "Entity position was not exactly one authority step old.");
            TestRunner.True(late.PreviousAuthoritativeSameFrame && late.PreviousAuthoritativeReferenceEligible,
                "Same-frame immediate Update anchor reference was not eligible.");
            TestRunner.True(late.RecoveryPendingAfterSample, "Accepted position lag did not require next-Update recovery.");

            var benign = tracker.Observe(2L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(benign.RecoveryRequiredBeforeSample && !benign.RecoveryUpdateObserved &&
                !benign.RecoverySatisfied && !benign.RecoveryViolation && benign.RecoveryPendingAfterSample,
                "An aligned non-Update position sample violated or discharged the pending recovery obligation.");
            TestRunner.True(!benign.PhaseLagObserved && !benign.PhaseLagPermitted && !benign.PhaseLagViolation,
                "An aligned intervening position sample was misclassified as another phase lag.");

            var recovered = tracker.Observe(3L, MovementSynchronizationPhase.Update, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(recovered.RecoveryRequiredBeforeSample && recovered.RecoveryUpdateObserved && recovered.RecoverySatisfied,
                "Current-again position did not satisfy the real next-Update recovery obligation.");
            TestRunner.True(!recovered.RecoveryViolation && !recovered.RecoveryPendingAfterSample,
                "Successful position recovery remained pending or was marked invalid.");

            var arithmetic = new MovementPositionPhaseTracker();
            arithmetic.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            arithmetic.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var coherentRoundoff = arithmetic.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, -0.00005d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(coherentRoundoff.EntityRawLagExcessWorldUnits > 0.0d &&
                coherentRoundoff.EntityRawLagExcessWorldUnits <= MovementPositionPhaseTracker.RawLagArithmeticCoherenceEpsilonWorldUnits,
                "Synthetic position arithmetic excess did not exercise the named coherence epsilon.");
            TestRunner.True(coherentRoundoff.PhaseLagPermitted, "Sub-epsilon position arithmetic coherence residual was rejected.");
        }

        private static void PositionPhaseTrackerRejectsUnsafeLag()
        {
            var stale = new MovementPositionPhaseTracker();
            stale.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            stale.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            stale.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var benignLate = stale.Observe(2L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(!benignLate.RecoveryViolation && benignLate.RecoveryPendingAfterSample,
                "Aligned position state did not carry recovery safely to the next Update.");
            var staleLate = stale.Observe(3L, MovementSynchronizationPhase.LateUpdate, 0.40d, 0.0d, 0.0d, 0.40d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(staleLate.PhaseLagViolation && staleLate.RecoveryViolation,
                "A second position lag without an intervening Update recovery was permitted.");
            TestRunner.True(!staleLate.PreviousAuthoritativeSameFrame && !staleLate.PreviousAuthoritativeReferenceEligible,
                "A stale prior-frame Update anchor was eligible for adjustment.");

            var unrecovered = new MovementPositionPhaseTracker();
            unrecovered.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            unrecovered.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            unrecovered.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var failedRecovery = unrecovered.Observe(2L, MovementSynchronizationPhase.Update, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(failedRecovery.RecoveryUpdateObserved && failedRecovery.RecoveryViolation && !failedRecovery.RecoverySatisfied,
                "A still-stale logical position at the next Update satisfied recovery.");

            var wrongPhase = new MovementPositionPhaseTracker();
            wrongPhase.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            wrongPhase.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            wrongPhase.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var unexpectedInitial = wrongPhase.Observe(2L, MovementSynchronizationPhase.InitialConfiguration, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(unexpectedInitial.RecoveryViolation && unexpectedInitial.RecoveryPendingAfterSample && !unexpectedInitial.PhaseLagObserved,
                "An unexpected InitialConfiguration position sample carried recovery as though it were LateUpdate.");

            var ageTwo = new MovementPositionPhaseTracker();
            ageTwo.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            ageTwo.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            ageTwo.Observe(1L, MovementSynchronizationPhase.InitialConfiguration, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            var ageTwoLate = ageTwo.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.40d, 0.0d, 0.0d, 0.40d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(ageTwoLate.PreviousAuthoritativeSameFrame && !ageTwoLate.PreviousAuthoritativeReferenceEligible && ageTwoLate.PhaseLagViolation,
                "An age-two position reference was permitted despite sharing the frame.");

            var visible = new MovementPositionPhaseTracker();
            visible.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            visible.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var visibleLate = visible.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(visibleLate.PhaseLagViolation && !visibleLate.PhaseLagPermitted,
                "Logical position lag was permitted while the rider view lagged visibly.");

            var stationary = new MovementPositionPhaseTracker();
            stationary.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            stationary.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var stationaryLate = stationary.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(stationaryLate.StationaryAuthority && stationaryLate.StationaryPositionCorrectionViolation && stationaryLate.PhaseLagViolation,
                "Stationary logical position drift was treated as a permitted phase lag.");

            var excess = new MovementPositionPhaseTracker();
            excess.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            excess.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var excessLate = excess.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, -0.0002d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(excessLate.EntityRawLagExcessWorldUnits > MovementPositionPhaseTracker.RawLagArithmeticCoherenceEpsilonWorldUnits &&
                excessLate.PhaseLagViolation && !excessLate.PhaseLagPermitted,
                "Above-epsilon raw position lag excess was permitted.");

            var previousMismatch = new MovementPositionPhaseTracker();
            previousMismatch.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            previousMismatch.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var mismatchLate = previousMismatch.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.30d, 0.0d, 0.0d, 0.30d, 0.0d, 0.0d, -0.11d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(mismatchLate.EntityPreviousAuthoritativePositionResidualWorldUnits.Value > 0.10d && mismatchLate.PhaseLagViolation,
                "Entity position that did not match the previous anchor was permitted.");
        }

        private static void PositionPhaseAwareQualificationRetainsRawLag()
        {
            var positionTracker = new MovementPositionPhaseTracker();
            var yawTracker = new MovementYawPhaseTracker();
            var accumulator = new MovementSynchronizationTelemetryAccumulator();
            var initialPosition = positionTracker.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var initialYaw = yawTracker.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(0L, MovementSynchronizationPhase.InitialConfiguration, initialPosition, initialYaw, 0.0d, 0.0d, 0.0d, 0.0d));
            var updatePosition = positionTracker.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var updateYaw = yawTracker.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(1L, MovementSynchronizationPhase.Update, updatePosition, updateYaw, 0.0d, 0.0d, 0.0d, 0.0d));
            var latePosition = positionTracker.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var lateYaw = yawTracker.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(2L, MovementSynchronizationPhase.LateUpdate, latePosition, lateYaw, 0.0d, 0.0d, 0.0d, 0.0d));

            var pending = MovementSynchronizationQualification.Evaluate(accumulator, 2L, 0.10d, 0.10d);
            TestRunner.True(pending.PhaseOrderPositionSafetyPassed && !pending.PhaseOrderPositionPassed,
                "One pending position lag did not preserve transient safety or incorrectly passed row completion.");
            TestRunner.Equal(0.20d, accumulator.MaximumCalibratedEntityRawCurrentPositionResidualWorldUnits, "Raw entity position lag was hidden.");
            TestRunner.Equal(0.0d, accumulator.MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits, "Adjusted entity position residual differs.");
            TestRunner.Equal(0.0d, accumulator.MaximumLateUpdatePreCorrectionPositionResidualWorldUnits, "Effective pre-correction position did not use the phase-adjusted entity residual.");
            TestRunner.Equal(0.20d, accumulator.MaximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits, "Raw pre-correction position maximum was not retained.");

            var benignPosition = positionTracker.Observe(2L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            var benignYaw = yawTracker.Observe(2L, MovementSynchronizationPhase.LateUpdate, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(3L, MovementSynchronizationPhase.LateUpdate, benignPosition, benignYaw, 0.0d, 0.0d, 0.0d, 0.0d));
            TestRunner.Equal(0L, accumulator.PositionPhaseLagRecoveryRequiredCount,
                "An intervening aligned position sample duplicated the normal-recovery obligation count.");

            var recoveryPosition = positionTracker.Observe(3L, MovementSynchronizationPhase.Update, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            var recoveryYaw = yawTracker.Observe(3L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(4L, MovementSynchronizationPhase.Update, recoveryPosition, recoveryYaw, 0.0d, 0.0d, 0.0d, 0.0d));
            var qualification = MovementSynchronizationQualification.Evaluate(accumulator, 2L, 0.10d, 0.10d);
            TestRunner.True(qualification.PreCorrectionPositionPassed && qualification.PhaseOrderPositionPassed,
                "Bounded entity-only position lag failed after real next-Update recovery.");
            TestRunner.Equal(1L, accumulator.PositionPhaseLagRecoveryRequiredCount, "Position recovery-required raw count differs.");
            TestRunner.Equal(1L, accumulator.PositionPhaseLagRecoveryUpdateCount, "Position recovery-Update raw count differs.");
            TestRunner.Equal(1L, accumulator.PositionPhaseLagRecoverySatisfiedCount, "Position recovery-satisfied raw count differs.");
            TestRunner.Equal(0L, accumulator.OutstandingPositionPhaseLagRecoveryCount, "Recovered position lag remained outstanding.");
        }

        private static void YawPhaseTrackerAcceptsOneSameFrameLagAndRecovery()
        {
            var tracker = new MovementYawPhaseTracker();
            tracker.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            tracker.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var late = tracker.Observe(1L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 0.0d, 0.10d);

            TestRunner.True(late.PhaseLagObserved, "Same-frame logical entity lag was not observed.");
            TestRunner.True(late.PhaseLagPermitted, "Immediately previous same-frame authority yaw was not permitted.");
            TestRunner.True(!late.PhaseLagViolation, "Permitted one-phase lag was marked invalid.");
            TestRunner.Equal(8.0d, late.EntityRawCurrentYawResidualDegrees, "Raw current entity lag was not retained.");
            TestRunner.Equal(0.0d, late.EntityPreviousAuthoritativeYawResidualDegrees.Value, "Previous-authority reference mismatch differs.");
            TestRunner.Equal(0.0d, late.EntityPhaseAdjustedYawResidualDegrees, "Phase-adjusted entity residual differs.");
            TestRunner.Equal(1L, late.EntityYawAuthorityAgeSteps.Value, "Entity yaw was not exactly one authority step old.");
            TestRunner.Equal(1L, late.PreviousAuthoritativeFrame.Value, "Previous authoritative frame differs.");
            TestRunner.Equal(MovementSynchronizationPhase.Update, late.PreviousAuthoritativePhase.Value, "Previous authoritative phase was not Update.");
            TestRunner.True(late.PreviousAuthoritativeSameFrame, "Previous Update reference was not from the same frame.");
            TestRunner.True(late.PreviousAuthoritativeReferenceEligible, "Same-frame immediate Update reference was not eligible.");
            TestRunner.True(late.RecoveryPendingAfterSample, "Accepted lag did not require next-Update recovery.");

            var benign = tracker.Observe(2L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 8.0d, 0.10d);
            TestRunner.True(benign.RecoveryRequiredBeforeSample && !benign.RecoveryUpdateObserved &&
                !benign.RecoverySatisfied && !benign.RecoveryViolation && benign.RecoveryPendingAfterSample,
                "An aligned non-Update yaw sample violated or discharged the pending recovery obligation.");
            TestRunner.True(!benign.PhaseLagObserved && !benign.PhaseLagPermitted && !benign.PhaseLagViolation,
                "An aligned intervening yaw sample was misclassified as another phase lag.");

            var recovered = tracker.Observe(3L, MovementSynchronizationPhase.Update, 8.0d, 8.0d, 8.0d, 8.0d, 0.10d);
            TestRunner.True(recovered.RecoveryRequiredBeforeSample, "Next Update did not carry the recovery obligation.");
            TestRunner.True(recovered.RecoveryUpdateObserved, "Recovery was not observed in Update.");
            TestRunner.True(recovered.RecoverySatisfied, "Current-again logical yaw did not satisfy recovery.");
            TestRunner.True(!recovered.RecoveryViolation, "Successful next-Update recovery was marked invalid.");
            TestRunner.True(!recovered.RecoveryPendingAfterSample, "Recovery remained pending after a current-again Update.");

            var arithmetic = new MovementYawPhaseTracker();
            arithmetic.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            arithmetic.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var coherentRoundoff = arithmetic.Observe(1L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, -0.00005d, 0.10d);
            TestRunner.True(coherentRoundoff.EntityRawLagExcessDegrees > 0.0d &&
                coherentRoundoff.EntityRawLagExcessDegrees <= MovementYawPhaseTracker.RawLagArithmeticCoherenceEpsilonDegrees,
                "Synthetic arithmetic lag excess did not exercise the named coherence epsilon.");
            TestRunner.True(coherentRoundoff.PhaseLagPermitted, "Sub-epsilon arithmetic coherence residual was rejected.");
        }

        private static void YawPhaseTrackerRejectsUnsafeLag()
        {
            var stale = new MovementYawPhaseTracker();
            stale.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            stale.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            stale.Observe(1L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 0.0d, 0.10d);
            var benignLate = stale.Observe(2L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 8.0d, 0.10d);
            TestRunner.True(!benignLate.RecoveryViolation && benignLate.RecoveryPendingAfterSample,
                "Aligned yaw state did not carry recovery safely to the next Update.");
            var staleLate = stale.Observe(3L, MovementSynchronizationPhase.LateUpdate, 16.0d, 16.0d, 16.0d, 8.0d, 0.10d);
            TestRunner.True(staleLate.PhaseLagViolation, "A second lag without an intervening Update was permitted.");
            TestRunner.True(staleLate.RecoveryViolation, "Missing next-Update recovery was not recorded.");
            TestRunner.Equal(1L, staleLate.PreviousAuthoritativeFrame.Value, "Stale Update reference frame was not retained for evidence.");
            TestRunner.Equal(MovementSynchronizationPhase.Update, staleLate.PreviousAuthoritativePhase.Value, "Stale reference lost its Update phase identity.");
            TestRunner.True(!staleLate.PreviousAuthoritativeSameFrame, "Prior-frame Update was misclassified as same-frame.");
            TestRunner.True(!staleLate.PreviousAuthoritativeReferenceEligible, "Prior-frame Update was eligible for phase adjustment.");

            var unrecovered = new MovementYawPhaseTracker();
            unrecovered.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            unrecovered.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            unrecovered.Observe(1L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 0.0d, 0.10d);
            var failedRecovery = unrecovered.Observe(2L, MovementSynchronizationPhase.Update, 8.0d, 8.0d, 8.0d, 0.0d, 0.10d);
            TestRunner.True(failedRecovery.RecoveryUpdateObserved, "Required recovery was not checked at the next Update.");
            TestRunner.True(failedRecovery.RecoveryViolation, "A still-stale entity at the next Update was accepted.");
            TestRunner.True(!failedRecovery.RecoverySatisfied, "A still-stale entity falsely satisfied recovery.");

            var wrongPhase = new MovementYawPhaseTracker();
            wrongPhase.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            wrongPhase.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            wrongPhase.Observe(1L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 0.0d, 0.10d);
            var unexpectedInitial = wrongPhase.Observe(2L, MovementSynchronizationPhase.InitialConfiguration, 8.0d, 8.0d, 8.0d, 8.0d, 0.10d);
            TestRunner.True(unexpectedInitial.RecoveryViolation && unexpectedInitial.RecoveryPendingAfterSample && !unexpectedInitial.PhaseLagObserved,
                "An unexpected InitialConfiguration yaw sample carried recovery as though it were LateUpdate.");

            var visible = new MovementYawPhaseTracker();
            visible.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            visible.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var visibleLate = visible.Observe(1L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 0.0d, 0.0d, 0.10d);
            TestRunner.True(visibleLate.PhaseLagViolation, "Logical lag was permitted while the rider view lagged visibly.");
            TestRunner.True(!visibleLate.PhaseLagPermitted, "Visible yaw lag was classified as a permitted entity-only lag.");

            var stationary = new MovementYawPhaseTracker();
            stationary.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            stationary.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var stationaryLate = stationary.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.0d, 0.0d, 0.0d, 2.0d, 0.10d);
            TestRunner.True(stationaryLate.StationaryAuthority, "Stationary authority was not classified as stationary.");
            TestRunner.True(stationaryLate.StationaryYawCorrectionViolation, "Stationary logical yaw correction was not rejected.");
            TestRunner.True(stationaryLate.PhaseLagViolation, "Stationary logical yaw drift was treated as a phase lag.");
        }

        private static void YawPhaseAwareQualificationRetainsRawLag()
        {
            var tracker = new MovementYawPhaseTracker();
            var accumulator = new MovementSynchronizationTelemetryAccumulator();
            var initial = tracker.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(0L, MovementSynchronizationPhase.InitialConfiguration, 1.0d, initial, 0.0d, 0.0d, 0.0d, 0.0d));
            var update = tracker.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(1L, MovementSynchronizationPhase.Update, 0.0d, update, 0.0d, 0.0d, 0.0d, 0.0d));
            var late = tracker.Observe(1L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(2L, MovementSynchronizationPhase.LateUpdate, 0.05d, late, 0.0d, 0.0d, 0.0d, 0.0d));
            var pendingQualification = MovementSynchronizationQualification.Evaluate(accumulator, 2L, 0.10d, 0.10d);
            TestRunner.True(pendingQualification.PhaseOrderYawSafetyPassed, "One outstanding same-frame recovery failed the transient live safety gate.");
            TestRunner.True(!pendingQualification.PhaseOrderYawPassed, "Row completion accepted an outstanding phase-lag recovery.");
            var benign = tracker.Observe(2L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 8.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(3L, MovementSynchronizationPhase.LateUpdate, 0.0d, benign, 0.0d, 0.0d, 0.0d, 0.0d));
            TestRunner.Equal(0L, accumulator.PhaseLagRecoveryRequiredCount,
                "An intervening aligned yaw sample duplicated the normal-recovery obligation count.");
            var recovery = tracker.Observe(3L, MovementSynchronizationPhase.Update, 8.0d, 8.0d, 8.0d, 8.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(4L, MovementSynchronizationPhase.Update, 0.0d, recovery, 0.0d, 0.0d, 0.0d, 0.0d));

            var qualification = MovementSynchronizationQualification.Evaluate(accumulator, 2L, 0.10d, 0.10d);
            TestRunner.True(qualification.PreCorrectionRotationPassed, "Bounded entity-only phase lag failed the fixed adjusted-yaw gate.");
            TestRunner.True(qualification.PhaseOrderYawPassed, "Phase-order yaw contract did not pass.");
            TestRunner.Equal(8.0d, accumulator.MaximumCalibratedEntityRawCurrentYawResidualDegrees, "Raw entity lag was hidden by phase adjustment.");
            TestRunner.Equal(0.0d, accumulator.MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees, "Adjusted entity residual differs.");
            TestRunner.Equal(1L, accumulator.PhaseLagObservedCount, "Observed phase-lag count differs.");
            TestRunner.Equal(1L, accumulator.PhaseLagPermittedCount, "Permitted phase-lag count differs.");
            TestRunner.Equal(1L, accumulator.PhaseLagSameFrameUpdateReferenceCount, "Same-frame Update-reference count differs.");
            TestRunner.Equal(1L, accumulator.PhaseLagEligibleReferenceCount, "Eligible-reference count differs.");
            TestRunner.Equal(1L, accumulator.PhaseLagRecoveryRequiredCount, "Recovery-required count differs.");
            TestRunner.Equal(1L, accumulator.PhaseLagRecoveryUpdateCount, "Recovery-Update count differs.");
            TestRunner.Equal(1L, accumulator.PhaseLagRecoverySatisfiedCount, "Recovered phase-lag count differs.");
            TestRunner.Equal(0L, accumulator.OutstandingPhaseLagRecoveryCount, "Recovered phase lag remained outstanding.");
        }

        private static void YawPhaseAwareQualificationRejectsMountEntityRootIncoherence()
        {
            var tracker = new MovementYawPhaseTracker();
            var accumulator = new MovementSynchronizationTelemetryAccumulator();
            var incoherent = tracker.Observe(
                1L,
                MovementSynchronizationPhase.Update,
                20.0d,
                20.100001d,
                20.0d,
                20.0d,
                0.10d);
            accumulator.Observe(new MovementSynchronizationSample(
                0L,
                MovementSynchronizationPhase.Update,
                0.0d,
                incoherent,
                0.0d,
                0.0d,
                0.0d,
                0.0d));

            var qualification = MovementSynchronizationQualification.Evaluate(accumulator, 1L, 0.10d, 0.10d);
            TestRunner.True(incoherent.MountEntityRootYawResidualDegrees > 0.10d, "Synthetic mount entity/root incoherence did not exceed the fixed gate.");
            TestRunner.True(!qualification.PhaseOrderYawSafetyPassed, "Mount entity/root yaw incoherence passed the live safety gate.");
            TestRunner.True(!qualification.PhaseOrderYawPassed, "Mount entity/root yaw incoherence passed row completion.");
            TestRunner.True(!qualification.PreCorrectionRotationPassed, "Mount entity/root yaw incoherence passed overall rotation qualification.");
        }

        private static void YawPhaseAwareQualificationRejectsFullViewQuaternionDrift()
        {
            var tracker = new MovementYawPhaseTracker();
            var accumulator = new MovementSynchronizationTelemetryAccumulator();
            var yawAligned = tracker.Observe(
                1L,
                MovementSynchronizationPhase.Update,
                20.0d,
                20.0d,
                20.0d,
                20.0d,
                0.10d);
            accumulator.Observe(new MovementSynchronizationSample(
                0L,
                MovementSynchronizationPhase.Update,
                0.0d,
                yawAligned,
                0.100001d,
                0.0d,
                0.0d,
                0.0d));

            var qualification = MovementSynchronizationQualification.Evaluate(accumulator, 1L, 0.10d, 0.10d);
            TestRunner.Equal(0.100001d, accumulator.MaximumCalibratedFullViewCurrentRotationResidualDegrees, "Full-view quaternion residual was not retained.");
            TestRunner.True(!qualification.PhaseOrderYawSafetyPassed, "Full-view quaternion drift passed the live safety gate.");
            TestRunner.True(!qualification.PhaseOrderYawPassed, "Full-view quaternion drift passed row completion.");
            TestRunner.True(!qualification.PreCorrectionRotationPassed, "Full-view quaternion drift passed overall rotation qualification.");
        }

        private static void MovementSynchronizationBoundarySnapshotRetainsFinalResiduals()
        {
            var snapshot = new MovementSynchronizationBoundarySnapshot(0.01d, 0.02d, 0.03d, 0.04d, 0.05d);
            TestRunner.Equal(0.01d, snapshot.PositionResidualWorldUnits, "Boundary position residual differs.");
            TestRunner.Equal(0.02d, snapshot.FullViewCurrentRotationResidualDegrees, "Boundary full-view rotation residual differs.");
            TestRunner.Equal(0.03d, snapshot.ViewCurrentYawResidualDegrees, "Boundary view-yaw residual differs.");
            TestRunner.Equal(0.04d, snapshot.EntityCurrentYawResidualDegrees, "Boundary entity-current yaw residual differs.");
            TestRunner.Equal(0.05d, snapshot.MountEntityRootYawResidualDegrees, "Boundary mount entity/root yaw residual differs.");

            var rejected = false;
            try
            {
                new MovementSynchronizationBoundarySnapshot(0.0d, -0.000001d, 0.0d, 0.0d, 0.0d);
            }
            catch (ArgumentOutOfRangeException)
            {
                rejected = true;
            }
            TestRunner.True(rejected, "Boundary snapshot accepted a negative residual.");
        }

        private static void StationaryBoundaryClosureReconcilesWithoutFabricatedUpdate()
        {
            MovementPositionPhaseTracker positionTracker;
            MovementYawPhaseTracker yawTracker;
            var accumulator = CreatePendingDualPhaseAccumulator(out positionTracker, out yawTracker);
            var benignPosition = positionTracker.Observe(2L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            var benignYaw = yawTracker.Observe(2L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 8.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(3L, MovementSynchronizationPhase.LateUpdate, benignPosition, benignYaw, 0.0d, 0.0d, 0.0d, 0.0d));
            TestRunner.True(benignPosition.RecoveryPendingAfterSample && !benignPosition.RecoveryViolation &&
                benignYaw.RecoveryPendingAfterSample && !benignYaw.RecoveryViolation,
                "Aligned non-Update observations did not preserve pending boundary-recoverable state.");
            TestRunner.Equal(0L, accumulator.PositionPhaseLagRecoveryRequiredCount,
                "A carried position obligation was counted more than once before its recovery event.");
            TestRunner.Equal(0L, accumulator.PhaseLagRecoveryRequiredCount,
                "A carried yaw obligation was counted more than once before its recovery event.");
            var pendingQualification = MovementSynchronizationQualification.Evaluate(accumulator, 2L, 0.10d, 0.10d);
            TestRunner.True(pendingQualification.PhaseOrderPositionSafetyPassed && pendingQualification.PhaseOrderYawSafetyPassed &&
                !pendingQualification.PhaseOrderPositionPassed && !pendingQualification.PhaseOrderYawPassed,
                "A benign carried obligation failed transient safety or passed before boundary reconciliation.");
            var sampleCountBefore = accumulator.SampleCount;
            var updateCountBefore = accumulator.UpdateSampleCount;
            var boundary = CreateStoppedBoundary(0.0d, 0.0d, 0.0d);

            var closure = accumulator.ClosePendingRecoveryAtStationaryBoundary(
                boundary,
                0.10d,
                0.10d,
                positionTracker,
                yawTracker);

            TestRunner.True(closure.Attempted && closure.Succeeded, "Valid stationary boundary closure did not succeed.");
            TestRunner.Equal(1L, closure.YawPendingBefore, "Yaw pending-before boundary count differs.");
            TestRunner.Equal(1L, closure.PositionPendingBefore, "Position pending-before boundary count differs.");
            TestRunner.Equal(1L, closure.YawClosedCount, "Yaw boundary closure channel count differs.");
            TestRunner.Equal(1L, closure.PositionClosedCount, "Position boundary closure channel count differs.");
            TestRunner.Equal(0L, closure.YawPendingAfter, "Yaw remained outstanding after boundary closure.");
            TestRunner.Equal(0L, closure.PositionPendingAfter, "Position remained outstanding after boundary closure.");
            TestRunner.Equal(sampleCountBefore, accumulator.SampleCount, "Boundary closure fabricated a synchronization sample.");
            TestRunner.Equal(updateCountBefore, accumulator.UpdateSampleCount, "Boundary closure fabricated a controller Update.");
            TestRunner.Equal(0L, accumulator.PhaseLagRecoveryRequiredCount, "Boundary closure fabricated a yaw recovery-required count.");
            TestRunner.Equal(0L, accumulator.PhaseLagRecoveryUpdateCount, "Boundary closure fabricated a yaw recovery-Update count.");
            TestRunner.Equal(0L, accumulator.PhaseLagRecoverySatisfiedCount, "Boundary closure fabricated a normal yaw recovery satisfaction.");
            TestRunner.Equal(0L, accumulator.PositionPhaseLagRecoveryRequiredCount, "Boundary closure fabricated a position recovery-required count.");
            TestRunner.Equal(0L, accumulator.PositionPhaseLagRecoveryUpdateCount, "Boundary closure fabricated a position recovery-Update count.");
            TestRunner.Equal(0L, accumulator.PositionPhaseLagRecoverySatisfiedCount, "Boundary closure fabricated a normal position recovery satisfaction.");
            TestRunner.Equal(1L, accumulator.EffectivePhaseLagRecoverySatisfiedCount, "Effective yaw recovery did not include the boundary channel.");
            TestRunner.Equal(1L, accumulator.EffectivePositionPhaseLagRecoverySatisfiedCount, "Effective position recovery did not include the boundary channel.");
            TestRunner.True(!yawTracker.RecoveryPending && !positionTracker.RecoveryPending,
                "Successful boundary closure left a tracker recovery bit pending.");

            var nextPosition = positionTracker.Observe(3L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            var nextYaw = yawTracker.Observe(3L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 8.0d, 0.10d);
            TestRunner.True(!nextPosition.RecoveryRequiredBeforeSample && !nextPosition.RecoveryViolation &&
                !nextYaw.RecoveryRequiredBeforeSample && !nextYaw.RecoveryViolation,
                "A callback after boundary closure resurrected the discharged recovery obligation.");

            var qualification = MovementSynchronizationQualification.Evaluate(accumulator, 2L, 0.10d, 0.10d);
            TestRunner.True(qualification.PhaseOrderPositionPassed && qualification.PhaseOrderYawPassed,
                "Separately counted boundary closure did not satisfy strict phase reconciliation.");
            TestRunner.Equal(
                accumulator.StationaryBoundaryClosureAttemptCount,
                accumulator.StationaryBoundaryClosureSucceededCount + accumulator.StationaryBoundaryClosureFailedCount,
                "Boundary closure attempt counts do not reconcile.");
        }

        private static void StationaryBoundaryClosureRejectsUnsafeBoundaries()
        {
            MovementPositionPhaseTracker repeatedPosition;
            MovementYawPhaseTracker repeatedYaw;
            var repeated = CreatePendingDualPhaseAccumulator(out repeatedPosition, out repeatedYaw);
            var stopped = CreateStoppedBoundary(0.0d, 0.0d, 0.0d);
            TestRunner.True(repeated.ClosePendingRecoveryAtStationaryBoundary(stopped, 0.10d, 0.10d, repeatedPosition, repeatedYaw).Succeeded,
                "First stationary boundary closure failed in repeated-call test.");
            var repeatedClosure = repeated.ClosePendingRecoveryAtStationaryBoundary(stopped, 0.10d, 0.10d, repeatedPosition, repeatedYaw);
            TestRunner.True(repeatedClosure.Attempted && !repeatedClosure.Succeeded && repeatedClosure.Reason == "repeated-boundary-closure",
                "Repeated stationary boundary closure was accepted.");

            MovementPositionPhaseTracker movingPosition;
            MovementYawPhaseTracker movingYaw;
            var moving = CreatePendingDualPhaseAccumulator(out movingPosition, out movingYaw);
            var movingBoundary = new MovementSynchronizationBoundarySnapshot(
                0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d,
                false, true, true);
            var movingClosure = moving.ClosePendingRecoveryAtStationaryBoundary(movingBoundary, 0.10d, 0.10d, movingPosition, movingYaw);
            TestRunner.True(!movingClosure.Succeeded && movingClosure.Reason == "movement-not-stopped",
                "Active movement boundary was accepted.");
            TestRunner.True(movingPosition.RecoveryPending && movingYaw.RecoveryPending &&
                moving.OutstandingPositionPhaseLagRecoveryCount == 1L && moving.OutstandingPhaseLagRecoveryCount == 1L,
                "Rejected moving boundary partially mutated pending recovery state.");

            MovementPositionPhaseTracker advancedPosition;
            MovementYawPhaseTracker advancedYaw;
            var advanced = CreatePendingDualPhaseAccumulator(out advancedPosition, out advancedYaw);
            var advancedClosure = advanced.ClosePendingRecoveryAtStationaryBoundary(
                CreateStoppedBoundary(0.000002d, 0.0d, 0.0d),
                0.10d,
                0.10d,
                advancedPosition,
                advancedYaw);
            TestRunner.True(!advancedClosure.Succeeded && advancedClosure.Reason == "authority-advanced",
                "Boundary closure accepted advancing authority.");

            MovementPositionPhaseTracker residualPosition;
            MovementYawPhaseTracker residualYaw;
            var residual = CreatePendingDualPhaseAccumulator(out residualPosition, out residualYaw);
            var residualClosure = residual.ClosePendingRecoveryAtStationaryBoundary(
                CreateStoppedBoundary(0.0d, 0.0d, 0.100001d),
                0.10d,
                0.10d,
                residualPosition,
                residualYaw);
            TestRunner.True(!residualClosure.Succeeded && residualClosure.Reason == "boundary-residual-exceeded",
                "Boundary closure accepted an above-threshold direct residual.");
            TestRunner.True(residual.StationaryBoundaryClosureAttemptCount ==
                residual.StationaryBoundaryClosureSucceededCount + residual.StationaryBoundaryClosureFailedCount,
                "Rejected boundary attempt counts do not reconcile.");

            MovementPositionPhaseTracker wrongPhasePosition;
            MovementYawPhaseTracker wrongPhaseYaw;
            var wrongPhase = CreatePendingDualPhaseAccumulator(out wrongPhasePosition, out wrongPhaseYaw);
            var unexpectedInitialPosition = wrongPhasePosition.Observe(2L, MovementSynchronizationPhase.InitialConfiguration, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.10d);
            var unexpectedInitialYaw = wrongPhaseYaw.Observe(2L, MovementSynchronizationPhase.InitialConfiguration, 8.0d, 8.0d, 8.0d, 8.0d, 0.10d);
            wrongPhase.Observe(new MovementSynchronizationSample(3L, MovementSynchronizationPhase.InitialConfiguration, unexpectedInitialPosition, unexpectedInitialYaw, 0.0d, 0.0d, 0.0d, 0.0d));
            TestRunner.Equal(1L, wrongPhase.PositionPhaseLagRecoveryViolationCount,
                "Accumulator discarded a pending wrong-phase position recovery violation.");
            TestRunner.Equal(1L, wrongPhase.PhaseLagRecoveryViolationCount,
                "Accumulator discarded a pending wrong-phase yaw recovery violation.");
            var wrongPhaseQualification = MovementSynchronizationQualification.Evaluate(wrongPhase, 2L, 0.10d, 0.10d);
            TestRunner.True(!wrongPhaseQualification.PhaseOrderPositionSafetyPassed && !wrongPhaseQualification.PhaseOrderYawSafetyPassed,
                "Pending wrong-phase recovery violations passed transient qualification.");
            var wrongPhaseClosure = wrongPhase.ClosePendingRecoveryAtStationaryBoundary(
                stopped,
                0.10d,
                0.10d,
                wrongPhasePosition,
                wrongPhaseYaw);
            TestRunner.True(!wrongPhaseClosure.Succeeded && wrongPhaseClosure.Reason == "yaw-pending-lag-not-permitted",
                "Stationary boundary closure erased a pending wrong-phase recovery violation.");

            var unrecordedPosition = new MovementPositionPhaseTracker();
            unrecordedPosition.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            unrecordedPosition.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            unrecordedPosition.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var unrecordedYaw = new MovementYawPhaseTracker();
            var emptyAccumulator = new MovementSynchronizationTelemetryAccumulator();
            var mismatchClosure = emptyAccumulator.ClosePendingRecoveryAtStationaryBoundary(
                stopped,
                0.10d,
                0.10d,
                unrecordedPosition,
                unrecordedYaw);
            TestRunner.True(mismatchClosure.Attempted && !mismatchClosure.Succeeded && mismatchClosure.Reason == "tracker-pending-state-mismatch",
                "No-outstanding branch hid a pending tracker recovery bit.");
            TestRunner.True(unrecordedPosition.RecoveryPending,
                "Rejected tracker/count mismatch mutated the unrecorded tracker obligation.");
        }

        private static MovementSynchronizationTelemetryAccumulator CreatePendingDualPhaseAccumulator(
            out MovementPositionPhaseTracker positionTracker,
            out MovementYawPhaseTracker yawTracker)
        {
            positionTracker = new MovementPositionPhaseTracker();
            yawTracker = new MovementYawPhaseTracker();
            var accumulator = new MovementSynchronizationTelemetryAccumulator();

            var initialPosition = positionTracker.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var initialYaw = yawTracker.Observe(0L, MovementSynchronizationPhase.InitialConfiguration, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(0L, MovementSynchronizationPhase.InitialConfiguration, initialPosition, initialYaw, 0.0d, 0.0d, 0.0d, 0.0d));

            var updatePosition = positionTracker.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var updateYaw = yawTracker.Observe(1L, MovementSynchronizationPhase.Update, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(1L, MovementSynchronizationPhase.Update, updatePosition, updateYaw, 0.0d, 0.0d, 0.0d, 0.0d));

            var latePosition = positionTracker.Observe(1L, MovementSynchronizationPhase.LateUpdate, 0.20d, 0.0d, 0.0d, 0.20d, 0.0d, 0.0d, 0.0d, 0.0d, 0.0d, 0.10d);
            var lateYaw = yawTracker.Observe(1L, MovementSynchronizationPhase.LateUpdate, 8.0d, 8.0d, 8.0d, 0.0d, 0.10d);
            accumulator.Observe(new MovementSynchronizationSample(2L, MovementSynchronizationPhase.LateUpdate, latePosition, lateYaw, 0.0d, 0.0d, 0.0d, 0.0d));
            return accumulator;
        }

        private static MovementSynchronizationBoundarySnapshot CreateStoppedBoundary(
            double authoritativePositionAdvanceWorldUnits,
            double authoritativeYawAdvanceDegrees,
            double viewPositionResidualWorldUnits)
        {
            return new MovementSynchronizationBoundarySnapshot(
                viewPositionResidualWorldUnits,
                0.0d,
                0.0d,
                0.0d,
                0.0d,
                0.0d,
                authoritativePositionAdvanceWorldUnits,
                authoritativeYawAdvanceDegrees,
                true,
                false,
                false);
        }

        private static void ViewAttachmentLeaseRestoresExactState()
        {
            var originalParent = new FakeTransformNode("original-parent");
            var attachmentParent = new FakeTransformNode("attachment-parent");
            var rider = new FakeTransformNode("rider")
            {
                Parent = originalParent,
                SiblingIndex = 3,
                WorldPosition = "position-before",
                WorldRotation = "rotation-before",
                LocalScale = "scale-before"
            };
            var lease = CreateFakeAttachmentLease();

            lease.Acquire(rider, attachmentParent);
            TestRunner.True(lease.IsAcquired, "Attachment lease was not acquired.");
            TestRunner.Equal(attachmentParent, rider.Parent, "Rider was not parented to the attachment anchor.");
            rider.SiblingIndex = 0;
            rider.WorldPosition = "position-mounted";
            rider.WorldRotation = "rotation-mounted";
            rider.LocalScale = "scale-mounted";

            lease.Restore();
            TestRunner.True(!lease.IsAcquired, "Successful restore retained the active lease.");
            TestRunner.True(lease.LastRestoreVerified, "Successful restore was not verified.");
            TestRunner.Equal(originalParent, rider.Parent, "Original parent was not restored.");
            TestRunner.Equal(3, rider.SiblingIndex, "Original sibling index was not restored.");
            TestRunner.Equal("position-before", rider.WorldPosition, "Original world position was not restored.");
            TestRunner.Equal("rotation-before", rider.WorldRotation, "Original world rotation was not restored.");
            TestRunner.Equal("scale-before", rider.LocalScale, "Original local scale was not restored.");
        }

        private static void ViewAttachmentLeaseCleanupIsIdempotent()
        {
            var rider = new FakeTransformNode("rider")
            {
                Parent = new FakeTransformNode("original-parent"),
                WorldPosition = "position",
                WorldRotation = "rotation",
                LocalScale = "scale"
            };
            var lease = CreateFakeAttachmentLease();
            lease.Acquire(rider, new FakeTransformNode("attachment-parent"));
            lease.Restore();
            lease.Restore();
            TestRunner.True(!lease.IsAcquired && lease.LastRestoreVerified, "Repeated restore changed successful cleanup state.");
        }

        private static void ViewAttachmentLeaseRetainsSnapshotForRetry()
        {
            var rider = new FakeTransformNode("rider")
            {
                Parent = new FakeTransformNode("original-parent"),
                SiblingIndex = 2,
                WorldPosition = "position-before",
                WorldRotation = "rotation-before",
                LocalScale = "scale-before",
                FailNextRotationWrite = true
            };
            var lease = CreateFakeAttachmentLease();
            lease.Acquire(rider, new FakeTransformNode("attachment-parent"));
            var failed = false;
            try
            {
                lease.Restore();
            }
            catch (AggregateException)
            {
                failed = true;
            }
            TestRunner.True(failed, "Injected transform restore failure was not reported.");
            TestRunner.True(lease.IsAcquired && !lease.LastRestoreVerified, "Failed restore discarded the retryable snapshot.");

            lease.Restore();
            TestRunner.True(!lease.IsAcquired && lease.LastRestoreVerified, "Cleanup retry did not verify exact restoration.");
            TestRunner.Equal("rotation-before", rider.WorldRotation, "Cleanup retry did not restore world rotation.");
        }

        private static void ViewAttachmentLeaseUsesInjectedBoundedComparers()
        {
            var rider = new FakeTransformNode("rider")
            {
                Parent = new FakeTransformNode("original-parent"),
                WorldPosition = "1.0000",
                WorldRotation = "20.000",
                LocalScale = "1.0000",
                PositionWriteBias = 0.00005d,
                RotationWriteBias = 0.005d,
                ScaleWriteBias = 0.00005d
            };
            var lease = CreateNumericFakeAttachmentLease();
            lease.Acquire(rider, new FakeTransformNode("attachment-parent"));
            lease.Restore();
            TestRunner.True(!lease.IsAcquired && lease.LastRestoreVerified, "Bounded float restoration drift caused a false residue failure.");

            rider.PositionWriteBias = 0.01d;
            lease.Acquire(rider, new FakeTransformNode("attachment-parent"));
            var failed = false;
            try { lease.Restore(); }
            catch (AggregateException) { failed = true; }
            TestRunner.True(failed && lease.IsAcquired, "Material transform mismatch did not retain retryable residue.");
            rider.PositionWriteBias = 0.0d;
            rider.RotationWriteBias = 0.0d;
            rider.ScaleWriteBias = 0.0d;
            lease.Restore();
            TestRunner.True(!lease.IsAcquired && lease.LastRestoreVerified, "Bounded-comparer mismatch did not support an exact cleanup retry.");
        }

        private static void ViewAttachmentLeaseReleasesInheritedReplacement()
        {
            var originalParent = new FakeTransformNode("stock-parent");
            var anchor = new FakeTransformNode("owned-anchor");
            var rider = new FakeTransformNode("old-rider")
            {
                Parent = originalParent,
                SiblingIndex = 4,
                WorldPosition = "old-position",
                WorldRotation = "old-rotation",
                LocalScale = "old-scale"
            };
            var replacement = new FakeTransformNode("stock-polymorph-replacement")
            {
                Parent = anchor,
                SiblingIndex = 0,
                WorldPosition = "replacement-position",
                WorldRotation = "replacement-rotation",
                LocalScale = "replacement-scale"
            };
            var lease = CreateFakeAttachmentLease();
            lease.Acquire(rider, anchor);

            TestRunner.True(lease.ReleaseInheritedReplacement(replacement), "Inherited replacement was not released.");
            TestRunner.Equal(originalParent, replacement.Parent, "Replacement did not return to the captured stock parent.");
            TestRunner.Equal(4, replacement.SiblingIndex, "Replacement did not inherit the captured stock sibling position.");
            TestRunner.Equal("replacement-position", replacement.WorldPosition, "Replacement world position changed.");
            TestRunner.Equal("replacement-rotation", replacement.WorldRotation, "Replacement world rotation changed.");
            TestRunner.True(lease.IsAcquired, "Releasing the replacement discarded the old rider lease.");

            var foreign = new FakeTransformNode("foreign") { Parent = originalParent };
            TestRunner.True(!lease.ReleaseInheritedReplacement(foreign), "A view outside the owned anchor was reparented.");
            lease.Restore();
        }

        private static ScopedTransformAttachmentLease<FakeTransformNode, string, string, string> CreateFakeAttachmentLease()
        {
            return new ScopedTransformAttachmentLease<FakeTransformNode, string, string, string>(
                node => node.Parent,
                node => node.SiblingIndex,
                node => node.WorldPosition,
                node => node.WorldRotation,
                node => node.LocalScale,
                (node, parent, worldPositionStays) => node.Parent = parent,
                (node, siblingIndex) => node.SiblingIndex = siblingIndex,
                (node, position) => node.WorldPosition = position,
                (node, rotation) =>
                {
                    if (node.FailNextRotationWrite)
                    {
                        node.FailNextRotationWrite = false;
                        throw new InvalidOperationException("injected rotation failure");
                    }
                    node.WorldRotation = rotation;
                },
                (node, scale) => node.LocalScale = scale);
        }

        private static ScopedTransformAttachmentLease<FakeTransformNode, double, double, double> CreateNumericFakeAttachmentLease()
        {
            return new ScopedTransformAttachmentLease<FakeTransformNode, double, double, double>(
                node => node.Parent,
                node => node.SiblingIndex,
                node => double.Parse(node.WorldPosition, System.Globalization.CultureInfo.InvariantCulture),
                node => double.Parse(node.WorldRotation, System.Globalization.CultureInfo.InvariantCulture),
                node => double.Parse(node.LocalScale, System.Globalization.CultureInfo.InvariantCulture),
                (node, parent, worldPositionStays) => node.Parent = parent,
                (node, siblingIndex) => node.SiblingIndex = siblingIndex,
                (node, position) => node.WorldPosition = (position + node.PositionWriteBias).ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                (node, rotation) => node.WorldRotation = (rotation + node.RotationWriteBias).ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                (node, scale) => node.LocalScale = (scale + node.ScaleWriteBias).ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                null,
                new BoundedDoubleComparer(0.0001d),
                new BoundedDoubleComparer(0.01d),
                new BoundedDoubleComparer(0.0001d));
        }

        private sealed class BoundedDoubleComparer : System.Collections.Generic.IEqualityComparer<double>
        {
            private readonly double tolerance;

            public BoundedDoubleComparer(double tolerance)
            {
                this.tolerance = tolerance;
            }

            public bool Equals(double first, double second)
            {
                return Math.Abs(first - second) <= tolerance;
            }

            public int GetHashCode(double value)
            {
                return 0;
            }
        }

        private sealed class FakeTransformNode
        {
            public FakeTransformNode(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public FakeTransformNode Parent { get; set; }
            public int SiblingIndex { get; set; }
            public string WorldPosition { get; set; }
            public string WorldRotation { get; set; }
            public string LocalScale { get; set; }
            public bool FailNextRotationWrite { get; set; }
            public double PositionWriteBias { get; set; }
            public double RotationWriteBias { get; set; }
            public double ScaleWriteBias { get; set; }
        }

        private static void AssertRejected(MountedPairCandidate candidate)
        {
            var runtime = new FakeRuntime();
            var coordinator = new MountedRelationshipCoordinator(runtime);
            var result = coordinator.Mount(candidate);
            TestRunner.True(!result.Succeeded, "Invalid candidate was accepted.");
            TestRunner.Equal(RelationshipState.Unmounted, coordinator.State, "Invalid candidate changed state.");
            TestRunner.Equal(0, runtime.AcquireCalls, "Invalid candidate mutated runtime.");
        }

        private static MountedRelationshipCoordinator Mounted(FakeRuntime runtime)
        {
            var coordinator = new MountedRelationshipCoordinator(runtime);
            var result = coordinator.Mount(ValidCandidate());
            TestRunner.True(result.Succeeded, "Test setup mount failed.");
            return coordinator;
        }

        private static MountedPairCandidate ValidCandidate()
        {
            return new MountedPairCandidate("rider", "mount")
            {
                RiderIsDirectlyControllable = true,
                MountIsDirectlyControllable = true,
                RiderIsAliveAndConscious = true,
                MountIsAliveAndConscious = true,
                ExactReciprocalCompanionRelationship = true,
                RiderIsInCombat = false,
                MountIsInCombat = false,
                PartyIsInCombat = false,
                RiderSizeOrdinal = 4,
                MountSizeOrdinal = 5,
                RiderViewAndStockAgentAvailable = true,
                MountViewAndStockAgentAvailable = true,
                RiderStockAgentEnabled = true,
                MountStockAgentEnabled = true,
                RiderAgentOverrideAvailable = true,
                MountAgentOverrideAvailable = true,
                RiderIsExactlyMedium = true,
                SafeMovementMode = true
            };
        }

        private static MountedPairCandidate CopyCandidate(MountedPairCandidate source, string riderId, string mountId)
        {
            return new MountedPairCandidate(riderId, mountId)
            {
                RiderIsDirectlyControllable = source.RiderIsDirectlyControllable,
                MountIsDirectlyControllable = source.MountIsDirectlyControllable,
                RiderIsAliveAndConscious = source.RiderIsAliveAndConscious,
                MountIsAliveAndConscious = source.MountIsAliveAndConscious,
                ExactReciprocalCompanionRelationship = source.ExactReciprocalCompanionRelationship,
                RiderIsInCombat = source.RiderIsInCombat,
                MountIsInCombat = source.MountIsInCombat,
                PartyIsInCombat = source.PartyIsInCombat,
                RiderSizeOrdinal = source.RiderSizeOrdinal,
                MountSizeOrdinal = source.MountSizeOrdinal,
                RiderViewAndStockAgentAvailable = source.RiderViewAndStockAgentAvailable,
                MountViewAndStockAgentAvailable = source.MountViewAndStockAgentAvailable,
                RiderStockAgentEnabled = source.RiderStockAgentEnabled,
                MountStockAgentEnabled = source.MountStockAgentEnabled,
                RiderAgentOverrideAvailable = source.RiderAgentOverrideAvailable,
                MountAgentOverrideAvailable = source.MountAgentOverrideAvailable,
                RiderIsExactlyMedium = source.RiderIsExactlyMedium,
                SafeMovementMode = source.SafeMovementMode
            };
        }

        private sealed class FakeRuntime : IMountedPairRuntime
        {
            public int AcquireCalls { get; private set; }
            public int AttachCalls { get; private set; }
            public int RestorePresentationCalls { get; private set; }
            public int RestoreAuthorityCalls { get; private set; }
            public bool ThrowOnAttach { get; set; }
            public bool ThrowOnRestorePresentation { get; set; }
            public int RestorePresentationFailuresRemaining { get; set; }
            public CleanupTrigger? LastRestoreTrigger { get; private set; }
            public Action OnAcquire { get; set; }
            public Exception RestoreAuthorityException { get; set; }

            public void AcquireMovementAuthority(MountedPair pair)
            {
                AcquireCalls++;
                OnAcquire?.Invoke();
            }

            public void AttachPresentation(MountedPair pair)
            {
                AttachCalls++;
                if (ThrowOnAttach) { throw new InvalidOperationException("attach failed"); }
            }

            public void RestorePresentation(MountedPair pair)
            {
                RestorePresentationCalls++;
                if (RestorePresentationFailuresRemaining > 0)
                {
                    RestorePresentationFailuresRemaining--;
                    throw new InvalidOperationException("restore presentation failed");
                }
                if (ThrowOnRestorePresentation) { throw new InvalidOperationException("restore presentation failed"); }
            }

            public void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger)
            {
                RestoreAuthorityCalls++;
                LastRestoreTrigger = trigger;
                if (RestoreAuthorityException != null) { throw RestoreAuthorityException; }
            }
        }
    }
}
