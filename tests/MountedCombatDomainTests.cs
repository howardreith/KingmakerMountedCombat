using System;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedCombatDomainTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("mounted rider melee uses rider actor and pair resource ownership", RiderMeleeOwnership);
            runner.Run("mounted Mammoth primary uses mount actor and rider resource ownership", MountAttackOwnership);
            runner.Run("mounted combat rejects ranged rider attack", RejectsRangedRider);
            runner.Run("mounted combat rejects invalid target and unavailable Standard action", RejectsInvalidContext);
            runner.Run("mounted combat transaction starts exactly one child attack", PreventsDuplicateAttack);
            runner.Run("mounted combat transaction bounds target repaths", BoundsRepaths);
            runner.Run("mounted combat transaction cancellation is idempotent", CancellationIsIdempotent);
            runner.Run("mounted combat transaction requires exact target identity", RequiresExactTarget);
            runner.Run("mounted combat range uses Mammoth origin and exact tolerance", RangeBoundary);
            runner.Run("mounted combat range rejects invalid measurements", RejectsInvalidRange);
            runner.Run("mounted pair ends only the exact Mammoth turn", SuppressesOnlyMountTurn);
            runner.Run("mounted pair delegates movement only through the exact rider turn", DelegatesOnlyExactMovement);
            runner.Run("diagnostic target requires Working authorization", TargetRequiresWorkingAuthorization);
            runner.Run("diagnostic target creation and removal are exact and idempotent", TargetLifecycleIsExact);
            runner.Run("diagnostic target safety snapshot preserves every strict gate", TargetSafetySnapshotPreservesEveryGate);
            runner.Run("diagnostic target safety snapshot reports exact failed gates", TargetSafetySnapshotReportsExactFailures);
        }

        private static void RiderMeleeOwnership()
        {
            var result = MountedCombatActionEvaluator.Evaluate(Eligible(MountedCombatActionKind.RiderMelee));
            TestRunner.True(result.IsAllowed, "Eligible rider melee was rejected.");
            TestRunner.Equal(MountedCombatActor.Rider, result.Actor, "Rider attack actor changed.");
            TestRunner.Equal(MountedCombatActor.Rider, result.ResourceOwner, "Rider did not own Standard cost.");
            TestRunner.Equal(MountedCombatActor.Mount, result.PathfindingOwner, "Mammoth did not own pathfinding.");
            TestRunner.True(result.IsSingleAttack, "Mounted rider attack admitted multiple attacks.");
        }

        private static void MountAttackOwnership()
        {
            var result = MountedCombatActionEvaluator.Evaluate(Eligible(MountedCombatActionKind.MountPrimaryNatural));
            TestRunner.True(result.IsAllowed, "Eligible Mammoth primary was rejected.");
            TestRunner.Equal(MountedCombatActor.Mount, result.Actor, "Mammoth attack actor changed.");
            TestRunner.Equal(MountedCombatActor.Rider, result.ResourceOwner, "Mammoth action did not charge rider.");
        }

        private static void RejectsRangedRider()
        {
            var context = Eligible(MountedCombatActionKind.RiderMelee);
            context.RiderWeaponIsSupportedMelee = false;
            var result = MountedCombatActionEvaluator.Evaluate(context);
            TestRunner.True(!result.IsAllowed, "Ranged mounted rider attack was accepted.");
            TestRunner.True(result.Feedback.Contains("ranged attacks"), "Ranged rejection was not explicit.");
        }

        private static void RejectsInvalidContext()
        {
            var context = Eligible(MountedCombatActionKind.MountPrimaryNatural);
            context.TargetIsVisibleEnemy = false;
            context.RiderHasStandardAction = false;
            context.RiderOwnsCurrentTurnOrRealTime = false;
            var result = MountedCombatActionEvaluator.Evaluate(context);
            var feedback = string.Join(" ", result.RejectionReasons.ToArray());
            TestRunner.True(!result.IsAllowed, "Invalid mounted attack was accepted.");
            TestRunner.True(feedback.Contains("visible enemy"), "Target rejection missing.");
            TestRunner.True(feedback.Contains("current turn"), "Turn rejection missing.");
            TestRunner.True(feedback.Contains("Standard action"), "Resource rejection missing.");
        }

        private static void PreventsDuplicateAttack()
        {
            var transaction = TargetedTransaction(false);
            TestRunner.True(transaction.TryStartSingleAttack("target-1"), "First child attack was rejected.");
            TestRunner.True(!transaction.TryStartSingleAttack("target-1"), "Duplicate child attack was admitted.");
            TestRunner.Equal(1, transaction.ChildAttackStartCount, "Child attack count was not exactly one.");
            TestRunner.True(transaction.Complete("target-1"), "Exact transaction did not complete.");
        }

        private static void BoundsRepaths()
        {
            var transaction = TargetedTransaction(true);
            for (var i = 0; i < MountedCombatTransaction.MaximumRepaths; i++)
            {
                TestRunner.True(transaction.TryRepath("target-1"), "Authorized repath was rejected.");
            }
            TestRunner.True(!transaction.TryRepath("target-1"), "Unbounded repath was admitted.");
        }

        private static void CancellationIsIdempotent()
        {
            var transaction = TargetedTransaction(true);
            TestRunner.True(transaction.Cancel("manual stop"), "Active transaction did not cancel.");
            TestRunner.True(!transaction.Cancel("second stop"), "Cancellation was not idempotent.");
            TestRunner.Equal("manual stop", transaction.TerminalReason, "First cancellation reason was overwritten.");
        }

        private static void RequiresExactTarget()
        {
            var transaction = TargetedTransaction(true);
            TestRunner.True(!transaction.Arrive("target-2"), "A substituted target was accepted.");
            TestRunner.True(transaction.Arrive("target-1"), "Exact target arrival was rejected.");
            TestRunner.True(!transaction.TryStartSingleAttack("target-2"), "Attack target substitution was accepted.");
        }

        private static void RangeBoundary()
        {
            var radius = MountedCombatSpatialPolicy.CalculateStoppingRadius(1.5f, 0.5f, 1f);
            TestRunner.Equal(3f, radius, "Stopping radius changed.");
            TestRunner.True(
                MountedCombatSpatialPolicy.IsWithinRange(
                    new MountedCombatPoint(0f, 0f),
                    new MountedCombatPoint(3.05f, 0f),
                    radius),
                "Exact tolerance boundary was rejected.");
            TestRunner.True(
                !MountedCombatSpatialPolicy.IsWithinRange(
                    new MountedCombatPoint(0f, 0f),
                    new MountedCombatPoint(3.051f, 0f),
                    radius),
                "Outside range boundary was accepted.");
        }

        private static void RejectsInvalidRange()
        {
            var threw = false;
            try
            {
                MountedCombatSpatialPolicy.CalculateStoppingRadius(1f, -1f, 1f);
            }
            catch (ArgumentOutOfRangeException)
            {
                threw = true;
            }
            TestRunner.True(threw, "Negative target corpulence was accepted.");
        }

        private static void SuppressesOnlyMountTurn()
        {
            TestRunner.True(MountedPairTurnPolicy.ShouldEndMountTurn(true, true, true), "Exact Mammoth turn was not suppressed.");
            TestRunner.True(!MountedPairTurnPolicy.ShouldEndMountTurn(true, true, false), "Rider/non-pair turn was suppressed.");
            TestRunner.True(!MountedPairTurnPolicy.ShouldEndMountTurn(false, true, true), "Unmounted Mammoth turn was suppressed.");
        }

        private static void DelegatesOnlyExactMovement()
        {
            TestRunner.True(
                MountedPairTurnPolicy.CanDelegateMountMovement(true, true, true, true, true),
                "Exact Mammoth movement was not delegated.");
            TestRunner.True(
                !MountedPairTurnPolicy.CanDelegateMountMovement(true, true, false, true, true),
                "Non-rider turn admitted Mammoth movement.");
            TestRunner.True(
                !MountedPairTurnPolicy.CanDelegateMountMovement(true, true, true, true, false),
                "Non-pair movement agent was admitted.");
        }

        private static void TargetRequiresWorkingAuthorization()
        {
            var lifecycle = new DiagnosticCombatTargetLifecycle();
            TestRunner.True(!lifecycle.BeginCreate("target-1", false), "Target creation bypassed Working authorization.");
            TestRunner.Equal(DiagnosticCombatTargetState.Absent, lifecycle.State, "Rejected target changed lifecycle state.");
        }

        private static void TargetLifecycleIsExact()
        {
            var lifecycle = new DiagnosticCombatTargetLifecycle();
            TestRunner.True(lifecycle.BeginCreate("target-1", true), "Authorized target creation was rejected.");
            TestRunner.True(!lifecycle.Activate("target-2", true), "Substituted target activated.");
            TestRunner.True(lifecycle.Activate("target-1", true), "Exact target activation was rejected.");
            TestRunner.True(lifecycle.RequestDestroy("complete"), "Active target was not queued for removal.");
            TestRunner.True(!lifecycle.RequestDestroy("again"), "Duplicate removal request was accepted.");
            TestRunner.True(!lifecycle.ConfirmRemoved("target-1", false), "Target removal accepted residue.");
            TestRunner.True(lifecycle.ConfirmRemoved("target-1", true), "Zero-residue removal was rejected.");
        }

        private static void TargetSafetySnapshotPreservesEveryGate()
        {
            var snapshot = TargetSafetySnapshot();
            TestRunner.True(snapshot.AllPassed, "An all-pass transient target safety snapshot failed.");
            TestRunner.Equal(0, snapshot.FailedGateNames.Length, "An all-pass transient target safety snapshot reported failures.");
            TestRunner.Equal(string.Empty, snapshot.FailureSummary, "An all-pass transient target safety snapshot reported a failure summary.");
        }

        private static void TargetSafetySnapshotReportsExactFailures()
        {
            var snapshot = new DiagnosticCombatTargetSafetySnapshot(
                true, true, true, true, true, true, false,
                true, true, true, false, true, false);
            TestRunner.True(!snapshot.AllPassed, "A transient target with failed safety gates passed.");
            TestRunner.Equal(
                "rider-treats-target-as-enemy,inventory-empty,primary-natural-weapon-is-melee",
                snapshot.FailureSummary,
                "Transient target safety failures were not reported in exact gate order.");
        }

        private static DiagnosticCombatTargetSafetySnapshot TargetSafetySnapshot()
        {
            return new DiagnosticCombatTargetSafetySnapshot(
                true, true, true, true, true, true, true,
                true, true, true, true, true, true);
        }

        private static MountedCombatTransaction TargetedTransaction(bool requiresApproach)
        {
            var transaction = new MountedCombatTransaction();
            TestRunner.True(transaction.Arm(MountedCombatActionKind.RiderMelee), "Transaction did not arm.");
            TestRunner.True(transaction.AcceptTarget("target-1", requiresApproach), "Transaction did not accept exact target.");
            return transaction;
        }

        private static MountedCombatActionContext Eligible(MountedCombatActionKind action)
        {
            return new MountedCombatActionContext
            {
                Action = action,
                FeatureEnabled = true,
                ExactMountedPair = true,
                SupportedMammothProfile = true,
                InCombat = true,
                RiderAliveAndConscious = true,
                MountAliveAndConscious = true,
                TargetExists = true,
                TargetAliveAndConscious = true,
                TargetIsVisibleEnemy = true,
                RiderOwnsCurrentTurnOrRealTime = true,
                RiderHasStandardAction = true,
                RiderWeaponIsSupportedMelee = true,
                MountPrimaryNaturalAttackIsExact = true,
                TransactionIdle = true,
                LoadingOrLifecycleBoundary = false
            };
        }
    }
}
