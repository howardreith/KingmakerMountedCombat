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
            runner.Run("relationship invalid non-default game mode", InvalidNonDefaultMode);
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
            candidate.DefaultGameMode = false;
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
                DefaultGameMode = true
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
                DefaultGameMode = source.DefaultGameMode
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
            public Action OnAcquire { get; set; }

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
                if (ThrowOnRestorePresentation) { throw new InvalidOperationException("restore presentation failed"); }
            }

            public void RestoreMovementAuthority(MountedPair pair)
            {
                RestoreAuthorityCalls++;
            }
        }
    }
}
