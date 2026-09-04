using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class PairedCommandSchedulerTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("paired scheduler accepts one exact leased command", AcceptsExactLease);
            runner.Run("paired scheduler rejects foreign and AI commands", RejectsForeignAndAiCommands);
            runner.Run("paired scheduler excludes attacks of opportunity", ExcludesAttackOfOpportunity);
            runner.Run("paired scheduler rejects wrong rider or mount", RejectsWrongPair);
            runner.Run("paired scheduler rejects stale generation or turn", RejectsStaleGenerationAndTurn);
            runner.Run("paired scheduler rejects slot replacement", RejectsSlotReplacement);
            runner.Run("paired scheduler observes start exactly once", ObservesStartOnce);
            runner.Run("paired scheduler drives at most once per frame", DrivesAtMostOncePerFrame);
            runner.Run("paired scheduler finishes exactly once", FinishesOnce);
            runner.Run("paired scheduler interrupts exactly once", InterruptsOnce);
            runner.Run("paired scheduler faults retain one exact interrupt", FaultRetainsExactInterrupt);
            runner.Run("paired scheduler cleanup is idempotent", CleanupIsIdempotent);
            runner.Run("paired scheduler exception cleanup faults and disposes", ExceptionCleanup);
            runner.Run("paired scheduler state is not serializable", StateIsNotSerializable);
            runner.Run("paired scheduler disabled fallback is inert", DisabledFallbackIsInert);
        }

        private static void AcceptsExactLease()
        {
            var lease = CreateAdmittedLease();
            PairedCommandSchedulerRejection rejection;
            TestRunner.True(
                lease.TryAuthorizeDrive(100, ExactContext(), out rejection),
                "Exact leased command was rejected: " + rejection + ".");
            TestRunner.Equal(1, lease.DriveCount, "Exact command did not receive one drive lease.");
        }

        private static void RejectsForeignAndAiCommands()
        {
            var foreign = ExactContext();
            foreign.ExplicitKmcOrigin = false;
            TestRunner.Equal(
                PairedCommandSchedulerRejection.CommandNotKmcCreated,
                PairedCommandSchedulerPolicy.Evaluate(foreign).Rejection,
                "Foreign command was not rejected.");

            var ai = ExactContext();
            ai.IsAiCommand = true;
            TestRunner.Equal(
                PairedCommandSchedulerRejection.AiCommandExcluded,
                PairedCommandSchedulerPolicy.Evaluate(ai).Rejection,
                "AI command was not rejected.");
        }

        private static void ExcludesAttackOfOpportunity()
        {
            var context = ExactContext();
            context.IsAttackOfOpportunity = true;
            TestRunner.Equal(
                PairedCommandSchedulerRejection.AttackOfOpportunityExcluded,
                PairedCommandSchedulerPolicy.Evaluate(context).Rejection,
                "AoO command was admitted by the scheduler.");
        }

        private static void RejectsWrongPair()
        {
            var rider = ExactContext();
            rider.CurrentTurnIsRider = false;
            TestRunner.Equal(
                PairedCommandSchedulerRejection.RiderNotCurrent,
                PairedCommandSchedulerPolicy.Evaluate(rider).Rejection,
                "Wrong rider was accepted.");

            var mount = ExactContext();
            mount.ExecutorIsMount = false;
            TestRunner.Equal(
                PairedCommandSchedulerRejection.ExecutorNotMount,
                PairedCommandSchedulerPolicy.Evaluate(mount).Rejection,
                "Wrong mount was accepted.");
        }

        private static void RejectsStaleGenerationAndTurn()
        {
            var generation = ExactContext();
            generation.RelationshipGenerationMatches = false;
            TestRunner.Equal(
                PairedCommandSchedulerRejection.RelationshipGenerationChanged,
                PairedCommandSchedulerPolicy.Evaluate(generation).Rejection,
                "Stale relationship generation was accepted.");

            var turn = ExactContext();
            turn.TurnReferenceMatches = false;
            TestRunner.Equal(
                PairedCommandSchedulerRejection.TurnReferenceChanged,
                PairedCommandSchedulerPolicy.Evaluate(turn).Rejection,
                "Stale rider turn was accepted.");
        }

        private static void RejectsSlotReplacement()
        {
            var context = ExactContext();
            context.ExactExpectedSlot = false;
            TestRunner.Equal(
                PairedCommandSchedulerRejection.CommandSlotChanged,
                PairedCommandSchedulerPolicy.Evaluate(context).Rejection,
                "Replaced Standard slot was accepted.");
        }

        private static void ObservesStartOnce()
        {
            var lease = CreateAdmittedLease();
            TestRunner.True(
                lease.ObserveLifecycle(10, true, true, false, null),
                "First start observation failed.");
            TestRunner.True(
                lease.ObserveLifecycle(11, true, true, false, null),
                "Stable running observation failed.");
            TestRunner.Equal(1, lease.StartObservationCount, "Start was counted more than once.");
        }

        private static void DrivesAtMostOncePerFrame()
        {
            var lease = CreateAdmittedLease();
            PairedCommandSchedulerRejection rejection;
            TestRunner.True(lease.TryAuthorizeDrive(20, ExactContext(), out rejection), "First frame drive failed.");
            TestRunner.True(!lease.TryAuthorizeDrive(20, ExactContext(), out rejection), "Duplicate frame drive was accepted.");
            TestRunner.Equal(PairedCommandSchedulerRejection.DuplicateFrameDrive, rejection, "Wrong duplicate-drive result.");
            TestRunner.Equal(1, lease.DriveCount, "Duplicate drive changed the admitted drive count.");
            TestRunner.Equal(1, lease.DuplicateFrameDriveCount, "Duplicate drive was not recorded exactly once.");
            TestRunner.Equal(PairedCommandSchedulerState.Faulted, lease.State, "Duplicate drive did not fault closed.");
        }

        private static void FinishesOnce()
        {
            var lease = CreateAdmittedLease();
            TestRunner.True(lease.Complete("Success"), "First terminal result was rejected.");
            TestRunner.True(!lease.Complete("Success"), "Second terminal result was accepted.");
            TestRunner.Equal(1, lease.TerminalObservationCount, "Terminal result was counted more than once.");
        }

        private static void InterruptsOnce()
        {
            var lease = CreateAdmittedLease();
            TestRunner.True(lease.Interrupt("test"), "First interrupt was rejected.");
            TestRunner.True(!lease.Interrupt("duplicate"), "Second interrupt was accepted.");
            TestRunner.Equal(1, lease.InterruptCount, "Interrupt was counted more than once.");
        }

        private static void FaultRetainsExactInterrupt()
        {
            var lease = CreateAdmittedLease();
            TestRunner.True(lease.Fault("invariant"), "Lease did not fault.");
            TestRunner.True(lease.Interrupt("fault cleanup"), "Faulted lease did not record its exact interrupt.");
            TestRunner.True(!lease.Interrupt("duplicate"), "Faulted lease accepted a duplicate interrupt.");
            TestRunner.Equal(1, lease.InterruptCount, "Fault cleanup did not retain exactly one interrupt.");
            TestRunner.Equal(PairedCommandSchedulerState.Faulted, lease.State, "Fault cleanup changed the faulted state.");
        }

        private static void CleanupIsIdempotent()
        {
            var lease = CreateAdmittedLease();
            TestRunner.True(lease.Dispose("complete"), "First cleanup failed.");
            TestRunner.True(!lease.Dispose("duplicate"), "Second cleanup mutated the lease.");
            TestRunner.Equal(1, lease.CleanupCount, "Cleanup was not idempotent.");
        }

        private static void ExceptionCleanup()
        {
            var lease = CreateAdmittedLease();
            TestRunner.True(lease.Fault("InvalidOperationException"), "Exception did not fault the lease.");
            TestRunner.True(lease.Dispose("exception cleanup"), "Faulted lease did not dispose.");
            TestRunner.Equal(PairedCommandSchedulerState.Disposed, lease.State, "Exception cleanup retained live state.");
            TestRunner.Equal(1, lease.CleanupCount, "Exception cleanup did not occur exactly once.");
        }

        private static void StateIsNotSerializable()
        {
            TestRunner.True(
                !typeof(PairedCommandSchedulerLeaseStateMachine).IsSerializable,
                "Scheduler lease state became serializable.");
            TestRunner.True(
                !Attribute.IsDefined(typeof(PairedCommandSchedulerLeaseStateMachine), typeof(SerializableAttribute)),
                "Scheduler lease acquired a serialization attribute.");
        }

        private static void DisabledFallbackIsInert()
        {
            var context = ExactContext();
            context.SchedulerEnabled = false;
            var decision = PairedCommandSchedulerPolicy.Evaluate(context);
            TestRunner.True(!decision.Eligible, "Disabled scheduler admitted a command.");
            TestRunner.Equal(
                PairedCommandSchedulerRejection.SchedulerDisabled,
                decision.Rejection,
                "Disabled scheduler did not fail inertly.");
        }

        private static PairedCommandSchedulerLeaseStateMachine CreateAdmittedLease()
        {
            var lease = new PairedCommandSchedulerLeaseStateMachine();
            TestRunner.True(
                lease.Register("rider", "mount", 1, "turn", "command"),
                "Lease registration failed.");
            TestRunner.True(
                lease.ConfirmAdmission(true, true, false),
                "Lease admission confirmation failed.");
            return lease;
        }

        private static PairedCommandEligibilityContext ExactContext()
        {
            return new PairedCommandEligibilityContext
            {
                SchedulerEnabled = true,
                UnifiedTurnEnabled = true,
                TurnBased = true,
                RelationshipMounted = true,
                PairReferencesMatch = true,
                RelationshipGenerationMatches = true,
                TurnReferenceMatches = true,
                CurrentTurnIsRider = true,
                TurnPreparing = true,
                TurnActing = false,
                TurnEnding = false,
                CommandReferenceMatches = true,
                ExecutorIsMount = true,
                CommandTypeEligible = true,
                ExplicitKmcOrigin = true,
                CreatedByPlayer = true,
                ExactExpectedSlot = true,
                InQueue = false,
                IsAttackOfOpportunity = false,
                IsFreeOrOutOfTurn = false,
                IsAiCommand = false,
                WaitingForUi = false,
                MountAwake = true,
                MountInAwakeUnits = true,
                CommandStarted = false,
                CommandTerminal = false
            };
        }
    }
}
