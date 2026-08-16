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
            runner.Run("mounted combat diagnostic placement admits the exact observed small radius", DiagnosticPlacementAdmitsObservedRadius);
            runner.Run("mounted combat diagnostic placement rejects insufficient radius and projection drift", DiagnosticPlacementRejectsUnsafeBounds);
            runner.Run("mounted combat native admission bridges only an in-range Mammoth origin", NativeAdmissionUsesMountOrigin);
            runner.Run("mounted combat native admission rejects pair range and offset escape", NativeAdmissionRejectsUnsafeBounds);
            runner.Run("mounted pair ends only the exact Mammoth turn", SuppressesOnlyMountTurn);
            runner.Run("mounted pair delegates movement only through the exact rider turn", DelegatesOnlyExactMovement);
            runner.Run("native single attack prefers an eligible primary hand", NativeSingleAttackPrefersPrimary);
            runner.Run("native single attack falls back through secondary then additional limbs", NativeSingleAttackFallbackOrder);
            runner.Run("native single attack skips hand slots when hands are disabled", NativeSingleAttackSkipsDisabledHands);
            runner.Run("native single attack rejects negative attack counts and empty weapon sets", NativeSingleAttackRejectsInvalidOrEmptyInputs);
            runner.Run("diagnostic target requires Working authorization", TargetRequiresWorkingAuthorization);
            runner.Run("diagnostic target creation and removal are exact and idempotent", TargetLifecycleIsExact);
            runner.Run("diagnostic target safety snapshot preserves every strict gate", TargetSafetySnapshotPreservesEveryGate);
            runner.Run("diagnostic target safety snapshot reports exact failed gates", TargetSafetySnapshotReportsExactFailures);
            runner.Run("diagnostic combat click safety preserves every target-only gate", CombatClickSafetyPreservesEveryGate);
            runner.Run("diagnostic combat click safety reports exact visibility and weapon failures", CombatClickSafetyReportsExactFailures);
            runner.Run("diagnostic combat dispatch requires every native real-time start gate", CombatDispatchRequiresEveryStartGate);
            runner.Run("diagnostic combat dispatch reports exact paused initiative and equipment gates", CombatDispatchReportsExactFailures);
            runner.Run("diagnostic combat entry requires native memory preparation and group combat", CombatEntryRequiresNativeMemoryAndCombat);
            runner.Run("diagnostic combat entry reports exact memory combat and time failures", CombatEntryReportsExactFailures);
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

        private static void DiagnosticPlacementAdmitsObservedRadius()
        {
            const float observedRadius = 2.37020588f;
            float requestedDistance;
            TestRunner.True(
                MountedCombatSpatialPolicy.TryCalculateDiagnosticTargetDistance(observedRadius, out requestedDistance),
                "The exact guarded Probe F radius was rejected by diagnostic placement.");
            TestRunner.True(
                Math.Abs(requestedDistance - 2.25020588f) < 0.00001f,
                "Diagnostic placement did not retain the exact fixed range inset.");
            TestRunner.True(
                MountedCombatSpatialPolicy.IsBoundedDiagnosticTargetDistance(observedRadius, requestedDistance),
                "The exact near-boundary diagnostic distance was rejected.");
        }

        private static void DiagnosticPlacementRejectsUnsafeBounds()
        {
            float requestedDistance;
            TestRunner.True(
                !MountedCombatSpatialPolicy.TryCalculateDiagnosticTargetDistance(
                    MountedCombatSpatialPolicy.DiagnosticRangeInset + MountedCombatSpatialPolicy.RangeTolerance,
                    out requestedDistance),
                "A diagnostic radius without positive separation was accepted.");
            TestRunner.True(
                !MountedCombatSpatialPolicy.IsBoundedDiagnosticTargetDistance(2.37020588f, 2.18020588f),
                "Excess navmesh projection drift was accepted.");
            TestRunner.True(
                !MountedCombatSpatialPolicy.IsBoundedDiagnosticTargetDistance(2.37020588f, 0.05f),
                "A diagnostic target without bounded positive separation was accepted.");
        }

        private static void NativeAdmissionUsesMountOrigin()
        {
            float nativeRadius;
            TestRunner.True(
                MountedCombatSpatialPolicy.TryCalculateNativeExecutorAdmissionRadius(
                    2.37020588f,
                    2.25020623f,
                    2.47820623f,
                    out nativeRadius),
                "An in-range Mammoth origin could not bridge the exact native rider executor offset.");
            TestRunner.True(
                Math.Abs(nativeRadius - 2.47920623f) < 0.00001f,
                "Native rider admission did not retain the exact bounded executor distance plus epsilon.");

            TestRunner.True(
                MountedCombatSpatialPolicy.TryCalculateNativeExecutorAdmissionRadius(3f, 2.9f, 2.8f, out nativeRadius) &&
                    Math.Abs(nativeRadius - 3f) < 0.00001f,
                "An executor already inside the pair radius received an unnecessary range expansion.");
        }

        private static void NativeAdmissionRejectsUnsafeBounds()
        {
            float nativeRadius;
            TestRunner.True(
                !MountedCombatSpatialPolicy.TryCalculateNativeExecutorAdmissionRadius(2.37f, 2.421f, 2.4f, out nativeRadius),
                "A target outside the Mammoth-origin tolerance received native attack admission.");
            TestRunner.True(
                !MountedCombatSpatialPolicy.TryCalculateNativeExecutorAdmissionRadius(2.37f, 2.3f, 3.121f, out nativeRadius),
                "An excessive rider-executor radius expansion was admitted.");
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

        private static void NativeSingleAttackPrefersPrimary()
        {
            var decision = NativeSingleAttackSlotPolicy.Select(
                true, true, 1, true, 1, new[] { true, true });
            TestRunner.Equal(NativeSingleAttackSlotKind.PrimaryHand, decision.Kind, "Eligible primary hand lost native priority.");
            TestRunner.Equal(-1, decision.AdditionalLimbIndex, "Primary-hand selection retained a limb index.");
        }

        private static void NativeSingleAttackFallbackOrder()
        {
            var secondary = NativeSingleAttackSlotPolicy.Select(
                true, true, 0, true, 1, new[] { true });
            TestRunner.Equal(NativeSingleAttackSlotKind.SecondaryHand, secondary.Kind, "Eligible secondary hand did not precede limbs.");

            var limb = NativeSingleAttackSlotPolicy.Select(
                true, false, 0, false, 0, new[] { false, true, true });
            TestRunner.Equal(NativeSingleAttackSlotKind.AdditionalLimb, limb.Kind, "Additional-limb fallback was not selected.");
            TestRunner.Equal(1, limb.AdditionalLimbIndex, "Additional-limb fallback did not choose the first weapon-bearing slot.");
        }

        private static void NativeSingleAttackSkipsDisabledHands()
        {
            var decision = NativeSingleAttackSlotPolicy.Select(
                false, true, 3, true, 2, new[] { true });
            TestRunner.Equal(NativeSingleAttackSlotKind.AdditionalLimb, decision.Kind, "Disabled hands remained eligible for native attack selection.");
            TestRunner.Equal(0, decision.AdditionalLimbIndex, "Disabled-hand fallback did not select the exact first limb.");
        }

        private static void NativeSingleAttackRejectsInvalidOrEmptyInputs()
        {
            var empty = NativeSingleAttackSlotPolicy.Select(
                true, false, 0, false, 0, new bool[0]);
            TestRunner.True(!empty.HasSelection, "Weaponless native attack input produced a selection.");

            var threw = false;
            try
            {
                NativeSingleAttackSlotPolicy.Select(true, true, -1, false, 0, null);
            }
            catch (ArgumentOutOfRangeException)
            {
                threw = true;
            }
            TestRunner.True(threw, "Negative native primary attack count was accepted.");
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
                true, true, true, true, true, true, true, false,
                true, true, true, false, false, true, true, false);
            TestRunner.True(!snapshot.AllPassed, "A transient target with failed safety gates passed.");
            TestRunner.Equal(
                "rider-treats-target-as-enemy,inventory-has-no-loot,native-primary-natural-weapon-resolved-without-provisioning,primary-natural-weapon-is-melee",
                snapshot.FailureSummary,
                "Transient target safety failures were not reported in exact gate order.");
        }

        private static DiagnosticCombatTargetSafetySnapshot TargetSafetySnapshot()
        {
            return new DiagnosticCombatTargetSafetySnapshot(
                true, true, true, true, true, true, true, true,
                true, true, true, true, true, true, true, true);
        }

        private static void CombatClickSafetyPreservesEveryGate()
        {
            var snapshot = new DiagnosticCombatClickSafetySnapshot(
                true, true, true, true, true, true, true);
            TestRunner.True(snapshot.AllPassed, "An exact prepared diagnostic click was rejected.");
            TestRunner.Equal(0, snapshot.FailedGateNames.Length, "An exact click reported failed safety gates.");
        }

        private static void CombatClickSafetyReportsExactFailures()
        {
            var snapshot = new DiagnosticCombatClickSafetySnapshot(
                true, false, true, false, true, true, false);
            TestRunner.True(!snapshot.AllPassed, "An unsafe diagnostic click passed.");
            TestRunner.Equal(
                "fog-of-war-cleared,target-visible-for-player,rider-weapon-is-supported-melee",
                snapshot.FailureSummary,
                "Diagnostic click failures were not reported in exact gate order.");
        }

        private static void CombatDispatchRequiresEveryStartGate()
        {
            var snapshot = new DiagnosticCombatDispatchReadinessSnapshot(
                true, true, true, true, true);
            TestRunner.True(snapshot.AllPassed, "An unpaused rider with every native start gate ready was rejected.");
            TestRunner.True(snapshot.GameUnpaused && snapshot.RiderCanActInCombat && !snapshot.RiderHandsBusy &&
                    snapshot.EquipmentControllerAvailable && !snapshot.EquipmentUpdateScheduled,
                "An all-pass dispatch snapshot changed its exact native gate values.");
        }

        private static void CombatDispatchReportsExactFailures()
        {
            var snapshot = new DiagnosticCombatDispatchReadinessSnapshot(
                false, false, false, true, false);
            TestRunner.True(!snapshot.AllPassed, "A paused rider waiting on initiative and equipment was dispatched.");
            TestRunner.Equal(
                "game-unpaused,rider-can-act-in-combat,rider-hands-idle,equipment-update-idle",
                snapshot.FailureSummary,
                "Diagnostic dispatch failures were not reported in exact gate order.");
            TestRunner.True(snapshot.RiderHandsBusy && snapshot.EquipmentUpdateScheduled,
                "Failed dispatch gates were not preserved as exact observed states.");
        }

        private static void CombatEntryRequiresNativeMemoryAndCombat()
        {
            var snapshot = new DiagnosticCombatEntryReadinessSnapshot(
                true, true, true, true, true, true, true, true, true, true, 0f, 0.01f);
            TestRunner.True(snapshot.AllPassed, "A native-memory-backed Default-mode combat entry was rejected.");
            TestRunner.True(snapshot.MemoryQueued && snapshot.PlayerGroupMemoryContainsTarget &&
                    snapshot.TargetGroupMemoryContainsRider && snapshot.RiderInCombat && snapshot.MountInCombat &&
                    snapshot.TargetInCombat && snapshot.PlayerInCombat && snapshot.RiderPrepared && snapshot.RiderAwake &&
                    snapshot.DefaultGameMode && snapshot.RiderInitiative == 0f && snapshot.GameDeltaTime > 0f,
                "An all-pass combat-entry snapshot changed its exact observed state.");
        }

        private static void CombatEntryReportsExactFailures()
        {
            var snapshot = new DiagnosticCombatEntryReadinessSnapshot(
                true, false, false, true, false, false, false, false, false, false, 6f, 0f);
            TestRunner.True(!snapshot.AllPassed, "A combat entry without memory, group combat, preparation, or game time passed.");
            TestRunner.Equal(
                "player-memory-contains-target,target-memory-contains-rider,mount-in-combat,target-in-combat,player-in-combat,rider-initiative-prepared,rider-awake,default-game-mode,positive-game-delta",
                snapshot.FailureSummary,
                "Combat-entry failures were not reported in exact gate order.");
            TestRunner.True(snapshot.RiderInitiative == 6f && snapshot.GameDeltaTime == 0f,
                "Failed combat-entry timing evidence was not preserved exactly.");
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
