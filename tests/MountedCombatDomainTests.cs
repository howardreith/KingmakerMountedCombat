using System;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedCombatDomainTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("mounted rider melee uses rider actor and rider resource ownership", RiderMeleeOwnership);
            runner.Run("mounted Mammoth primary uses mount actor and mount resource ownership", MountAttackOwnership);
            runner.Run("mounted combat rejects ranged rider attack", RejectsRangedRider);
            runner.Run("mounted combat rejects invalid target and unavailable Standard action", RejectsInvalidContext);
            runner.Run("mounted combat transaction starts exactly one child attack", PreventsDuplicateAttack);
            runner.Run("active mounted command suppresses only exact-pair stock opportunity attacks", SuppressesOnlyExactPairOpportunityAttacks);
            runner.Run("mounted combat transaction bounds target repaths", BoundsRepaths);
            runner.Run("mounted combat transaction cancellation is idempotent", CancellationIsIdempotent);
            runner.Run("mounted combat target invalidation cancels only the exact pre-child transaction", TargetInvalidationCancelsOnlyExactPreChildTransaction);
            runner.Run("mounted combat transaction requires exact target identity", RequiresExactTarget);
            runner.Run("mounted combat range uses Mammoth origin and exact tolerance", RangeBoundary);
            runner.Run("mounted combat range rejects invalid measurements", RejectsInvalidRange);
            runner.Run("mounted combat diagnostic placement admits the exact observed small radius", DiagnosticPlacementAdmitsObservedRadius);
            runner.Run("mounted combat diagnostic placement rejects insufficient radius and projection drift", DiagnosticPlacementRejectsUnsafeBounds);
            runner.Run("mounted combat diagnostic placement refreshes exact Mammoth actor drift", DiagnosticPlacementRefreshesObservedMammothDrift);
            runner.Run("mounted combat approach placement starts outside exact pair range", DiagnosticApproachPlacementStartsOutsideRange);
            runner.Run("mounted combat approach evidence preserves mount-only pathfinding", ApproachEvidencePreservesMountAuthority);
            runner.Run("mounted combat approach raw Move slot preserves the exact finished command boundary", ApproachRawMoveSlotPreservesFinishedBoundary);
            runner.Run("mounted combat approach rejects an empty Mammoth command controller", ApproachEvidenceRejectsEmptyMountCommandController);
            runner.Run("mounted combat approach evidence reports command movement and pose drift", ApproachEvidenceReportsExactFailures);
            runner.Run("mounted combat native admission bridges only an in-range Mammoth origin", NativeAdmissionUsesMountOrigin);
            runner.Run("mounted combat native admission rejects pair range and offset escape", NativeAdmissionRejectsUnsafeBounds);
            runner.Run("mounted pair ends only the exact Mammoth turn", SuppressesOnlyMountTurn);
            runner.Run("mounted pair preserves only an explicitly armed Mammoth action turn", PreservesExplicitMountActionTurn);
            runner.Run("mounted pair admits rider action in exact native command window", AdmitsExactRiderActionWindow);
            runner.Run("mounted pair delegates movement only through the exact rider turn", DelegatesOnlyExactMovement);
            runner.Run("native single attack prefers an eligible primary hand", NativeSingleAttackPrefersPrimary);
            runner.Run("native single attack falls back through secondary then additional limbs", NativeSingleAttackFallbackOrder);
            runner.Run("native single attack skips hand slots when hands are disabled", NativeSingleAttackSkipsDisabledHands);
            runner.Run("native single attack rejects negative attack counts and empty weapon sets", NativeSingleAttackRejectsInvalidOrEmptyInputs);
            runner.Run("native single attack preserves exact turn-based terminal success", NativeSingleAttackPreservesTurnBasedTerminalSuccess);
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
            runner.Run("diagnostic combat action actor uses its own real-time initiative", CombatActionActorUsesOwnInitiative);
            runner.Run("diagnostic combat action actor rejects identity preparation and initiative failures", CombatActionActorReportsExactFailures);
            runner.Run("diagnostic native combat join preserves every exact controller gate", NativeCombatJoinPreservesEveryGate);
            runner.Run("diagnostic native combat join reports exact controller failures", NativeCombatJoinReportsExactFailures);
            runner.Run("diagnostic turn-based dispatch requires exact native rider turn", TurnBasedDispatchRequiresExactRiderTurn);
            runner.Run("diagnostic turn-based dispatch reports exact roster and turn failures", TurnBasedDispatchReportsExactFailures);
            runner.Run("mounted pair liveness preserves every in-flight gate", PairLivenessPreservesEveryGate);
            runner.Run("mounted pair liveness reports exact changed gates", PairLivenessReportsExactFailures);
            runner.Run("mounted pair liveness admits target incapacitation only after exact child start", PairLivenessAdmitsPostAttackIncapacitation);
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
            TestRunner.Equal(MountedCombatActor.Mount, result.ResourceOwner, "Mammoth did not own its Standard cost.");
            TestRunner.Equal(MountedCombatActor.Mount, result.PathfindingOwner, "Mammoth did not retain pathfinding authority.");
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
            context.ActionActorHasStandardAction = false;
            context.ActionActorOwnsCurrentTurnOrRealTime = false;
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

        private static void SuppressesOnlyExactPairOpportunityAttacks()
        {
            TestRunner.True(
                MountedOpportunityIsolationPolicy.ShouldSuppressStockOpportunityAttack(
                    true, true, true, false, true),
                "An active exact-rider opportunity attack escaped the mounted transaction guard.");
            TestRunner.True(
                MountedOpportunityIsolationPolicy.ShouldSuppressStockOpportunityAttack(
                    true, true, false, true, true),
                "An active exact-Mammoth opportunity attack escaped the mounted transaction guard.");
            TestRunner.True(
                !MountedOpportunityIsolationPolicy.ShouldSuppressStockOpportunityAttack(
                    true, false, true, false, true),
                "Idle mounted rider opportunity behavior was changed.");
            TestRunner.True(
                !MountedOpportunityIsolationPolicy.ShouldSuppressStockOpportunityAttack(
                    false, true, true, false, true),
                "Non-mounted rider opportunity behavior was changed.");
            TestRunner.True(
                !MountedOpportunityIsolationPolicy.ShouldSuppressStockOpportunityAttack(
                    true, true, false, false, true),
                "A non-pair unit opportunity attack was suppressed.");
            TestRunner.True(
                !MountedOpportunityIsolationPolicy.ShouldSuppressStockOpportunityAttack(
                    true, true, true, false, false),
                "A null-target opportunity probe was suppressed.");
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

        private static void TargetInvalidationCancelsOnlyExactPreChildTransaction()
        {
            var exact = TargetedTransaction(true);
            TestRunner.True(
                exact.CancelTargetInvalidationBeforeChildAttack("target-1"),
                "Exact pre-child target invalidation did not cancel.");
            TestRunner.Equal(
                MountedCombatTransactionState.Cancelled,
                exact.State,
                "Exact pre-child target invalidation did not use cancellation semantics.");
            TestRunner.Equal(
                MountedCombatTransaction.TargetInvalidatedBeforeChildAttackReason,
                exact.TerminalReason,
                "Exact pre-child target invalidation did not preserve its bounded terminal reason.");
            TestRunner.True(
                !exact.CancelTargetInvalidationBeforeChildAttack("target-1"),
                "Exact pre-child target invalidation was not idempotent.");

            var substituted = TargetedTransaction(true);
            TestRunner.True(
                !substituted.CancelTargetInvalidationBeforeChildAttack("target-2"),
                "A substituted target cancelled the mounted transaction.");
            TestRunner.Equal(
                MountedCombatTransactionState.Approaching,
                substituted.State,
                "A rejected substituted target changed transaction state.");

            var started = TargetedTransaction(false);
            TestRunner.True(started.TryStartSingleAttack("target-1"), "Exact child attack did not start.");
            TestRunner.True(
                !started.CancelTargetInvalidationBeforeChildAttack("target-1"),
                "Post-child target invalidation cancelled the native attack lifecycle.");
            TestRunner.Equal(
                MountedCombatTransactionState.Attacking,
                started.State,
                "Rejected post-child invalidation changed transaction state.");
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

        private static void DiagnosticPlacementRefreshesObservedMammothDrift()
        {
            const float mammothPrimaryRadius = 3.589406f;
            const float observedDriftedDistance = 3.06238842f;
            float refreshedDistance;
            TestRunner.True(
                MountedCombatSpatialPolicy.RequiresDiagnosticTargetPlacementRefresh(
                    mammothPrimaryRadius,
                    observedDriftedDistance),
                "The exact failed Mammoth-primary pre-dispatch placement drift was not detected.");
            TestRunner.True(
                MountedCombatSpatialPolicy.TryCalculateDiagnosticTargetDistance(
                    mammothPrimaryRadius,
                    out refreshedDistance) &&
                Math.Abs(refreshedDistance - 3.469406f) < 0.00001f &&
                MountedCombatSpatialPolicy.IsBoundedDiagnosticTargetDistance(
                    mammothPrimaryRadius,
                    refreshedDistance),
                "The exact Mammoth-primary radius did not produce a bounded current-position refresh.");
        }

        private static void DiagnosticApproachPlacementStartsOutsideRange()
        {
            const float pairRadius = 2.37020588f;
            float requestedDistance;
            TestRunner.True(
                MountedCombatSpatialPolicy.TryCalculateDiagnosticApproachTargetDistance(
                    pairRadius,
                    out requestedDistance),
                "A positive pair radius did not produce an approach target.");
            TestRunner.True(
                Math.Abs(requestedDistance - 4.37020588f) < 0.00001f,
                "Approach placement did not retain the exact fixed extension.");
            TestRunner.True(
                MountedCombatSpatialPolicy.IsBoundedDiagnosticApproachTargetDistance(
                    pairRadius,
                    requestedDistance),
                "Exact approach placement was rejected.");
            TestRunner.True(
                !MountedCombatSpatialPolicy.IsBoundedDiagnosticApproachTargetDistance(
                    pairRadius,
                    pairRadius + MountedCombatSpatialPolicy.RangeTolerance),
                "An in-range placement was accepted as movement-to-attack evidence.");
        }

        private static void ApproachEvidencePreservesMountAuthority()
        {
            var snapshot = PassingApproachSnapshot();
            TestRunner.True(snapshot.AllPassed, "An exact mount-authoritative approach failed.");
            TestRunner.Equal(0, snapshot.FailedGateNames.Length, "An exact approach reported failed gates.");
            TestRunner.Equal(string.Empty, snapshot.FailureSummary, "An exact approach reported a failure summary.");
        }

        private static void ApproachEvidenceReportsExactFailures()
        {
            var snapshot = new MountedCombatApproachSnapshot(
                true, 2, 1, false, false, false,
                false, false, false, false, false, false, true, false, 0,
                false, false, false, 0, false, false,
                2.37f, 2.42f, 2.43f, 0.1f, 0.2f, 0.051f, 1);
            TestRunner.True(!snapshot.AllPassed, "An unsafe approach evidence snapshot passed.");
            TestRunner.Equal(
                "one-delegated-move,delegated-move-drive-mode,delegated-move-executor-is-mount," +
                "wrapper-command-retained,delegated-move-not-queued,mount-move-slot-owned," +
                "mount-move-slot-unreplaced,mount-command-queue-empty,delegated-move-finished-successfully," +
                "mount-move-slot-restored,delegated-move-controller-exact,delegated-move-progress-observed," +
                "rider-stock-agent-suppressed," +
                "mount-stock-agent-authoritative,pose-healthy-throughout,runtime-approach-observed," +
                "selection-retained,ui-coherent-throughout,attack-start-inside-range," +
                "rider-followed-approach,mount-performed-approach,target-remained-stationary," +
                "no-unexpected-repath",
                snapshot.FailureSummary,
                "Unsafe approach gates were not reported in exact order.");
        }

        private static void ApproachRawMoveSlotPreservesFinishedBoundary()
        {
            TestRunner.True(
                MountedCombatSpatialPolicy.IsExactRawMoveSlotLifecycle(true, false, false),
                "An active exact delegated raw Move slot was rejected.");
            TestRunner.True(
                MountedCombatSpatialPolicy.IsExactRawMoveSlotLifecycle(true, false, true),
                "The exact finished delegated command still present in the raw Move slot was rejected.");
            TestRunner.True(
                MountedCombatSpatialPolicy.IsExactRawMoveSlotLifecycle(false, true, true),
                "A finished delegated command removed by the stock sweep was rejected.");
            TestRunner.True(
                !MountedCombatSpatialPolicy.IsExactRawMoveSlotLifecycle(false, true, false),
                "An unfinished delegated command missing from the raw Move slot was accepted.");
            TestRunner.True(
                !MountedCombatSpatialPolicy.IsExactRawMoveSlotLifecycle(false, false, true),
                "A replacement raw Move-slot command was accepted.");
        }

        private static void ApproachEvidenceRejectsEmptyMountCommandController()
        {
            var snapshot = PassingApproachSnapshot();
            var missingMoveSlot = new MountedCombatApproachSnapshot(
                snapshot.ApproachRequiredAtStart,
                snapshot.DelegatedMoveStartCount,
                snapshot.DelegatedMoveTickCount,
                snapshot.DelegatedMoveExecutorIsExactMount,
                snapshot.WrapperCommandRetained,
                snapshot.DelegatedMoveNeverQueued,
                false,
                snapshot.MountMoveSlotUnreplacedThroughoutApproach,
                snapshot.MountQueueEmptyThroughoutApproach,
                snapshot.DelegatedMoveFinishedSuccessfully,
                snapshot.MountMoveSlotRestoredAfterApproach,
                snapshot.DelegatedMoveDrivenByStockController,
                snapshot.DelegatedMoveDrivenByRiderTurnAdapter,
                snapshot.TurnBasedApproach,
                snapshot.DelegatedMoveProgressObservationCount,
                snapshot.RiderStockAgentSuppressed,
                snapshot.MountStockAgentAuthoritative,
                snapshot.PoseHealthyThroughout,
                snapshot.ObservationCount,
                snapshot.SelectionRetained,
                snapshot.UiCoherentThroughout,
                snapshot.PairApproachRadius,
                snapshot.InitialPairDistance,
                snapshot.PairDistanceAtAttackStart,
                snapshot.RiderDisplacementAtAttackStart,
                snapshot.MountDisplacementAtAttackStart,
                snapshot.TargetDisplacementAtAttackStart,
                snapshot.RepathCount);
            TestRunner.True(!missingMoveSlot.AllPassed,
                "A detached delegated move passed despite stock empty-container movement cancellation.");
            TestRunner.True(Array.IndexOf(missingMoveSlot.FailedGateNames, "mount-move-slot-owned") >= 0,
                "The exact Mammoth Move-slot failure was not reported.");
        }

        private static MountedCombatApproachSnapshot PassingApproachSnapshot()
        {
            return new MountedCombatApproachSnapshot(
                true, 1, 0, true, true, true,
                true, true, true, true, true, true, false, false, 8,
                true, true, true, 10, true, true,
                2.37f, 4.37f, 2.36f, 2.0f, 2.0f, 0.0f, 0);
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

        private static void PreservesExplicitMountActionTurn()
        {
            TestRunner.True(
                !MountedPairTurnPolicy.ShouldEndMountTurn(true, true, true, true),
                "An explicitly armed Mammoth action turn was suppressed.");
            TestRunner.True(
                MountedPairTurnPolicy.ShouldEndMountTurn(true, true, true, false),
                "An unarmed Mammoth turn escaped suppression.");
            TestRunner.True(
                MountedPairTurnPolicy.CanIssueAction(true, true, true, false) &&
                    MountedPairTurnPolicy.CanIssueAction(true, true, false, true),
                "The action-actor policy rejected an exact native Preparing or Acting window.");
            TestRunner.True(
                !MountedPairTurnPolicy.CanIssueAction(true, false, true, true),
                "A different unit's turn admitted an actor-specific command.");
        }

        private static void AdmitsExactRiderActionWindow()
        {
            TestRunner.True(
                MountedPairTurnPolicy.CanIssueRiderAction(false, false, false, false),
                "Real-time rider action incorrectly required a turn controller.");
            TestRunner.True(
                MountedPairTurnPolicy.CanIssueRiderAction(true, true, true, false),
                "Exact native Preparing rider turn rejected command admission.");
            TestRunner.True(
                MountedPairTurnPolicy.CanIssueRiderAction(true, true, false, true),
                "Exact native Acting rider turn rejected command admission.");
            TestRunner.True(
                !MountedPairTurnPolicy.CanIssueRiderAction(true, false, true, false),
                "A different unit's native Preparing turn admitted a rider command.");
            TestRunner.True(
                !MountedPairTurnPolicy.CanIssueRiderAction(true, true, false, false),
                "A rider turn outside Preparing or Acting admitted a command.");
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

        private static void NativeSingleAttackPreservesTurnBasedTerminalSuccess()
        {
            TestRunner.True(
                NativeSingleAttackTerminalPolicy.ShouldAwaitNativeAnimation(
                    true, true, true, true, 1, 1, false),
                "An exact acted turn-based single attack lost its native Success before animation completion.");
            TestRunner.True(
                !NativeSingleAttackTerminalPolicy.ShouldAwaitNativeAnimation(
                    false, true, true, true, 1, 1, false),
                "Real-time attack lifecycle was intercepted.");
            TestRunner.True(
                !NativeSingleAttackTerminalPolicy.ShouldAwaitNativeAnimation(
                    true, false, true, true, 1, 1, false),
                "An unacted attack was treated as terminal success.");
            TestRunner.True(
                !NativeSingleAttackTerminalPolicy.ShouldAwaitNativeAnimation(
                    true, true, false, true, 1, 1, false),
                "A non-success result was preserved as terminal success.");
            TestRunner.True(
                !NativeSingleAttackTerminalPolicy.ShouldAwaitNativeAnimation(
                    true, true, true, false, 1, 1, false),
                "A command without an observed native attack rule was treated as terminal success.");
            TestRunner.True(
                !NativeSingleAttackTerminalPolicy.ShouldAwaitNativeAnimation(
                    true, true, true, true, 2, 1, true),
                "A multi-attack sequence with a planned attack was truncated.");
            TestRunner.True(
                !NativeSingleAttackTerminalPolicy.ShouldAwaitNativeAnimation(
                    true, true, true, true, 1, 0, false),
                "An incomplete single attack was treated as terminal success.");
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
                true, true, false, false, true, false, false, true, true, false);
            TestRunner.True(!snapshot.AllPassed, "A transient target with failed safety gates passed.");
            TestRunner.Equal(
                "rider-treats-target-as-enemy,bounded-brain-lease,bounded-sleepless-lease,inventory-has-no-loot,native-primary-natural-weapon-resolved-without-provisioning,primary-natural-weapon-is-melee",
                snapshot.FailureSummary,
                "Transient target safety failures were not reported in exact gate order.");
        }

        private static DiagnosticCombatTargetSafetySnapshot TargetSafetySnapshot()
        {
            return new DiagnosticCombatTargetSafetySnapshot(
                true, true, true, true, true, true, true, true,
                true, true, true, true, true, true, true, true, true, true);
        }

        private static void CombatClickSafetyPreservesEveryGate()
        {
            var snapshot = new DiagnosticCombatClickSafetySnapshot(
                true, true, true, true, true, true, true, true, true, true, true);
            TestRunner.True(snapshot.AllPassed, "An exact prepared diagnostic click was rejected.");
            TestRunner.Equal(0, snapshot.FailedGateNames.Length, "An exact click reported failed safety gates.");
        }

        private static void CombatClickSafetyReportsExactFailures()
        {
            var snapshot = new DiagnosticCombatClickSafetySnapshot(
                true, false, true, false, false, false, false, false, true, true, false);
            TestRunner.True(!snapshot.AllPassed, "An unsafe diagnostic click passed.");
            TestRunner.Equal(
                "fog-of-war-cleared,target-visible-for-player,target-commands-empty,target-agent-enabled,target-agent-stopped,target-brain-suppressed,action-weapon-is-supported-melee",
                snapshot.FailureSummary,
                "Diagnostic click failures were not reported in exact gate order.");
        }

        private static void CombatDispatchRequiresEveryStartGate()
        {
            var snapshot = new DiagnosticCombatDispatchReadinessSnapshot(
                true, true, true, true, true);
            TestRunner.True(snapshot.AllPassed, "An unpaused rider with every native start gate ready was rejected.");
            TestRunner.True(snapshot.GameUnpaused && snapshot.ActionActorCanActInCombat && !snapshot.ActionActorHandsBusy &&
                    snapshot.EquipmentControllerAvailable && !snapshot.EquipmentUpdateScheduled,
                "An all-pass dispatch snapshot changed its exact native gate values.");
        }

        private static void CombatDispatchReportsExactFailures()
        {
            var snapshot = new DiagnosticCombatDispatchReadinessSnapshot(
                false, false, false, true, false);
            TestRunner.True(!snapshot.AllPassed, "A paused rider waiting on initiative and equipment was dispatched.");
            TestRunner.Equal(
                "game-unpaused,action-actor-can-act-in-combat,action-actor-hands-idle,equipment-update-idle",
                snapshot.FailureSummary,
                "Diagnostic dispatch failures were not reported in exact gate order.");
            TestRunner.True(snapshot.ActionActorHandsBusy && snapshot.EquipmentUpdateScheduled,
                "Failed dispatch gates were not preserved as exact observed states.");
        }

        private static void CombatEntryRequiresNativeMemoryAndCombat()
        {
            var snapshot = new DiagnosticCombatEntryReadinessSnapshot(
                true, true, true, true, true, true, true, true, true, true, true, 0f, 0.01f);
            TestRunner.True(snapshot.AllPassed, "A native-memory-backed Default-mode combat entry was rejected.");
            TestRunner.True(snapshot.MemoryQueued && snapshot.PlayerGroupMemoryContainsTarget &&
                    snapshot.TargetGroupMemoryContainsRider && snapshot.RiderInCombat && snapshot.MountInCombat &&
                    snapshot.TargetInCombat && snapshot.PlayerInCombat && snapshot.RiderPrepared && snapshot.RiderAwake &&
                    snapshot.TargetAwake &&
                    snapshot.DefaultGameMode && snapshot.RiderInitiative == 0f && snapshot.GameDeltaTime > 0f,
                "An all-pass combat-entry snapshot changed its exact observed state.");
        }

        private static void CombatEntryReportsExactFailures()
        {
            var snapshot = new DiagnosticCombatEntryReadinessSnapshot(
                true, false, false, true, false, false, false, false, false, false, false, 6f, 0f);
            TestRunner.True(!snapshot.AllPassed, "A combat entry without memory, group combat, preparation, or game time passed.");
            TestRunner.Equal(
                "player-memory-contains-target,target-memory-contains-rider,mount-in-combat,target-in-combat,player-in-combat,rider-initiative-prepared,rider-awake,target-awake,default-game-mode,positive-game-delta",
                snapshot.FailureSummary,
                "Combat-entry failures were not reported in exact gate order.");
            TestRunner.True(snapshot.RiderInitiative == 6f && snapshot.GameDeltaTime == 0f,
                "Failed combat-entry timing evidence was not preserved exactly.");
        }

        private static void CombatActionActorUsesOwnInitiative()
        {
            var realTime = new DiagnosticCombatActionActorReadinessSnapshot(
                false, "mammoth", "mammoth", true, true, 0f);
            TestRunner.True(realTime.AllPassed,
                "A real-time Mammoth ready on its own zero initiative was rejected.");

            var turnBased = new DiagnosticCombatActionActorReadinessSnapshot(
                true, "mammoth", "mammoth", true, true, 3f);
            TestRunner.True(turnBased.AllPassed,
                "An exact native Mammoth turn with a bounded prepared initiative was rejected.");
            TestRunner.True(turnBased.TurnBased && turnBased.ActorInitiative == 3f &&
                    turnBased.ActorCanActInCombat && turnBased.ActorPrepared,
                "Action-actor readiness did not preserve its exact observed state.");
        }

        private static void CombatActionActorReportsExactFailures()
        {
            var snapshot = new DiagnosticCombatActionActorReadinessSnapshot(
                false, "mammoth", "rider", false, false, 4.99591351f);
            TestRunner.True(!snapshot.AllPassed,
                "A rider-owned or initiative-blocked Mammoth action actor passed readiness.");
            TestRunner.Equal(
                "exact-action-actor,action-actor-prepared,action-actor-can-act-in-combat,action-actor-initiative-ready",
                snapshot.FailureSummary,
                "Action-actor readiness failures were not reported in exact gate order.");

            var outsideNativeRange = new DiagnosticCombatActionActorReadinessSnapshot(
                true, "mammoth", "mammoth", true, true, 6.01f);
            TestRunner.Equal(
                "action-actor-initiative-ready",
                outsideNativeRange.FailureSummary,
                "Turn-based action-actor readiness admitted initiative outside the native preparation range.");
        }

        private static void NativeCombatJoinPreservesEveryGate()
        {
            var snapshot = new DiagnosticNativeCombatJoinReadinessSnapshot(
                true, true, true, true, true, true,
                false, false, false,
                true, true, true, true, true, true, true, true, true);
            TestRunner.True(snapshot.AllPassed, "An exact native UnitCombatJoinController-ready state was rejected.");
            TestRunner.True(snapshot.RiderInGame && snapshot.MountInGame && snapshot.TargetInGame &&
                    snapshot.RiderConscious && snapshot.MountConscious && snapshot.TargetConscious &&
                    !snapshot.RiderIgnoredByCombat && !snapshot.MountIgnoredByCombat && !snapshot.TargetIgnoredByCombat &&
                    snapshot.PlayerGroupContainsRider && snapshot.PlayerGroupContainsMount &&
                    snapshot.TargetGroupContainsTarget && snapshot.PlayerGroupEnemiesContainsTarget &&
                    snapshot.TargetGroupEnemiesContainsRider && snapshot.RiderNotInFogOfWar &&
                    snapshot.TargetNotInFogOfWar && snapshot.RiderNotInStealthAmbush &&
                    snapshot.TargetNotInStealthAmbush,
                "An all-pass native join snapshot changed its exact raw state.");
        }

        private static void NativeCombatJoinReportsExactFailures()
        {
            var snapshot = new DiagnosticNativeCombatJoinReadinessSnapshot(
                false, true, true, true, true, false,
                false, true, false,
                true, true, true, false, true, true, true, true, false);
            TestRunner.True(!snapshot.AllPassed, "An ineligible native combat join snapshot passed.");
            TestRunner.Equal(
                "rider-in-game,target-conscious,mount-not-ignored-by-combat,player-enemies-contain-target,target-not-in-stealth-ambush",
                snapshot.FailureSummary,
                "Native combat join failures were not reported in exact controller-gate order.");
        }

        private static void TurnBasedDispatchRequiresExactRiderTurn()
        {
            var snapshot = new DiagnosticTurnBasedDispatchReadinessSnapshot(
                true, true, true, true, true, true, true, true);
            TestRunner.True(snapshot.AllPassed,
                "An initialized native rider turn with the exact combat roster was rejected.");
            TestRunner.True(snapshot.ModeEnabled && snapshot.ControllerInitialized &&
                    snapshot.RosterContainsRider && snapshot.RosterContainsMount &&
                    snapshot.RosterContainsTarget && snapshot.NativeActionActorTurnStarted &&
                    snapshot.CurrentTurnActionActor && snapshot.CurrentTurnCommandReady,
                "An all-pass turn-based snapshot changed its exact native gate values.");
        }

        private static void TurnBasedDispatchReportsExactFailures()
        {
            var snapshot = new DiagnosticTurnBasedDispatchReadinessSnapshot(
                true, false, true, false, false, true, false, false);
            TestRunner.True(!snapshot.AllPassed,
                "A turn-based dispatch without an initialized exact rider turn passed.");
            TestRunner.Equal(
                "turn-based-controller-initialized,turn-roster-contains-mount,turn-roster-contains-target,current-turn-action-actor,current-turn-command-ready",
                snapshot.FailureSummary,
                "Turn-based dispatch failures were not reported in exact gate order.");
        }

        private static MountedCombatTransaction TargetedTransaction(bool requiresApproach)
        {
            var transaction = new MountedCombatTransaction();
            TestRunner.True(transaction.Arm(MountedCombatActionKind.RiderMelee), "Transaction did not arm.");
            TestRunner.True(transaction.AcceptTarget("target-1", requiresApproach), "Transaction did not accept exact target.");
            return transaction;
        }

        private static void PairLivenessPreservesEveryGate()
        {
            var snapshot = new MountedPairLivenessSnapshot(
                true, true, true, true, true, true, true, true, true, true,
                true, true, true, true, true);
            TestRunner.True(snapshot.AllPassed, "A fully live exact mounted pair failed its in-flight snapshot.");
            TestRunner.Equal(string.Empty, snapshot.FailureSummary,
                "A fully live exact mounted pair published false failure gates.");
        }

        private static void PairLivenessReportsExactFailures()
        {
            var snapshot = new MountedPairLivenessSnapshot(
                false, true, true, true, false, true, true, true, true, false,
                true, true, false, false, false);
            TestRunner.True(!snapshot.AllPassed, "An invalid in-flight mounted pair passed its liveness snapshot.");
            TestRunner.Equal(
                "relationship-mounted,target-in-state,target-conscious-or-child-started,target-not-finally-dead,action-actor-hostile-to-target,action-actor-can-attack-target",
                snapshot.FailureSummary,
                "In-flight mounted-pair failures were not reported in exact gate order.");
        }

        private static void PairLivenessAdmitsPostAttackIncapacitation()
        {
            TestRunner.True(
                !MountedPairLivenessSnapshot.IsTargetConsciousnessAdmissible(false, 0),
                "An unconscious target passed before the exact native child started.");
            TestRunner.True(
                MountedPairLivenessSnapshot.IsTargetConsciousnessAdmissible(true, 0),
                "A conscious target failed before native child start.");
            TestRunner.True(
                MountedPairLivenessSnapshot.IsTargetConsciousnessAdmissible(false, 1),
                "Native target incapacitation could not finish the exact already-started child.");
            TestRunner.True(
                !MountedPairLivenessSnapshot.IsTargetConsciousnessAdmissible(true, 2),
                "An impossible duplicate child-start count passed liveness admission.");
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
                ActionActorOwnsCurrentTurnOrRealTime = true,
                ActionActorHasStandardAction = true,
                RiderWeaponIsSupportedMelee = true,
                MountPrimaryNaturalAttackIsExact = true,
                TransactionIdle = true,
                LoadingOrLifecycleBoundary = false
            };
        }
    }
}
