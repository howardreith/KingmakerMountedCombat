using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedStabilizationPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("mounted UI modes preserve the relationship without admitting combat", UiModesPreserveWithoutCombatAdmission);
            runner.Run("world-changing modes cleanly dismount", WorldChangingModesDismount);
            runner.Run("same-view attachment observes while replacement attachment cleans", ViewAttachmentClassificationIsExact);
            runner.Run("non-world UI treats stock visibility as transient while world mode remains strict", UiViewActivityIsModeScoped);
            runner.Run("native TB exit AI lease repair is exact and boundary-scoped", NativeTurnBasedExitAiLeaseRepairIsExact);
            runner.Run("native TB exit UI lease repair is exact and post-boundary", NativeTurnBasedExitUiLeaseRepairIsExact);
            runner.Run("mounted stock pair attack is pair-local and explains explicit controls", StockAttackRejectionIsPairLocal);
            runner.Run("native TB actor selection preserves separate rider and Mammoth turns", NativeTurnSelectionPreservesActorOwnership);
            runner.Run("mounted distant-door routing is exact rider-owned and turn-bounded", DistantDoorRoutingIsExact);
            runner.Run("mounted overlay world-click guard is one-shot and frame-bounded", OverlayWorldClickGuardIsBounded);
            runner.Run("cleanup feedback explains intentional transient boundaries", ExplainsIntentionalCleanupBoundaries);
        }

        private static void UiModesPreserveWithoutCombatAdmission()
        {
            foreach (var mode in new[] { "Pause", "FullScreenUi", "EscMode" })
            {
                TestRunner.Equal(MountedGameModeDisposition.PreserveNonWorldUi, MountedGameModePolicy.Classify(mode), mode + " was not classified as non-world UI.");
                TestRunner.True(MountedGameModePolicy.CanRetainMountedRelationship(mode), mode + " dismounted the pair.");
                TestRunner.True(!MountedGameModePolicy.CanAdmitMountedAction(mode), mode + " admitted a combat action.");
            }
            TestRunner.True(MountedGameModePolicy.CanAdmitMountedAction("Default"), "Default mode rejected mounted action admission.");
        }

        private static void WorldChangingModesDismount()
        {
            foreach (var mode in new[] { "None", "GlobalMap", "Dialog", "Cutscene", "Rest", "Kingdom", "GameOver", "BugReport", "KingdomSettlement", "CutsceneGlobalMap" })
            {
                TestRunner.Equal(MountedGameModeDisposition.CleanDismount, MountedGameModePolicy.Classify(mode), mode + " escaped the clean-dismount boundary.");
            }
        }

        private static void ViewAttachmentClassificationIsExact()
        {
            TestRunner.Equal(MountedViewAttachmentDisposition.ObserveExactView, MountedViewAttachmentPolicy.Classify(true, true, true, false), "An exact same-view attachment was treated as replacement.");
            TestRunner.Equal(MountedViewAttachmentDisposition.CleanReplacementFromOwnedAnchor, MountedViewAttachmentPolicy.Classify(true, true, false, true), "A replacement inherited through the owned anchor was not isolated.");
            TestRunner.Equal(MountedViewAttachmentDisposition.CleanChangedView, MountedViewAttachmentPolicy.Classify(true, true, false, false), "A changed pair view escaped fail-closed cleanup.");
            TestRunner.Equal(MountedViewAttachmentDisposition.IgnoreNonPair, MountedViewAttachmentPolicy.Classify(true, false, false, true), "A non-pair view was adopted by KMC cleanup.");
        }

        private static void UiViewActivityIsModeScoped()
        {
            TestRunner.True(MountedViewActivityPolicy.IsAdmissible(MountedGameModeDisposition.PreserveNonWorldUi, true, true, false, false), "A coherently hidden world beneath non-world UI was rejected.");
            TestRunner.True(MountedViewActivityPolicy.IsAdmissible(MountedGameModeDisposition.PreserveNonWorldUi, true, true, false, true), "Transient asymmetric hierarchy activity under stock UI was treated as relationship invalidation.");
            TestRunner.True(MountedViewActivityPolicy.IsAdmissible(MountedGameModeDisposition.PreserveNonWorldUi, false, true, false, false), "Transient stock rider visibility under non-world UI was treated as relationship invalidation.");
            TestRunner.True(MountedViewActivityPolicy.IsAdmissible(MountedGameModeDisposition.PreserveWorldInteraction, true, true, true, true), "Active world views were rejected.");
            TestRunner.True(!MountedViewActivityPolicy.IsAdmissible(MountedGameModeDisposition.PreserveWorldInteraction, false, true, false, true), "Inactive rider view was accepted after returning to the world.");
            TestRunner.True(!MountedViewActivityPolicy.IsAdmissible(MountedGameModeDisposition.CleanDismount, true, true, true, true), "A true world-changing boundary retained view activity authority.");
        }

        private static void NativeTurnBasedExitAiLeaseRepairIsExact()
        {
            TestRunner.Equal(
                NativeTurnBasedExitAiLeaseDisposition.NotPending,
                NativeTurnBasedExitAiLeasePolicy.Classify(false, true, false, false, true, true),
                "An unarmed path acquired Mammoth AI state.");
            TestRunner.Equal(
                NativeTurnBasedExitAiLeaseDisposition.AwaitNativeControllerClear,
                NativeTurnBasedExitAiLeasePolicy.Classify(true, true, false, true, true, true),
                "The policy raced the native controller cleanup.");
            TestRunner.Equal(
                NativeTurnBasedExitAiLeaseDisposition.AwaitNativeControllerClear,
                NativeTurnBasedExitAiLeasePolicy.Classify(true, true, true, false, true, true),
                "The policy changed AI while the native TB predicate remained active.");
            TestRunner.Equal(
                NativeTurnBasedExitAiLeaseDisposition.RejectInexactLease,
                NativeTurnBasedExitAiLeasePolicy.Classify(true, false, false, false, true, true),
                "An unmounted relationship acquired an AI lease.");
            TestRunner.Equal(
                NativeTurnBasedExitAiLeaseDisposition.RejectInexactLease,
                NativeTurnBasedExitAiLeasePolicy.Classify(true, true, false, false, false, true),
                "A non-owned Mammoth AI state was adopted.");
            TestRunner.Equal(
                NativeTurnBasedExitAiLeaseDisposition.AlreadyExact,
                NativeTurnBasedExitAiLeasePolicy.Classify(true, true, false, false, true, false),
                "An already exact owned lease requested a write.");
            TestRunner.Equal(
                NativeTurnBasedExitAiLeaseDisposition.ReassertExactLease,
                NativeTurnBasedExitAiLeasePolicy.Classify(true, true, false, false, true, true),
                "The exact post-controller stock AI reset was not isolated.");
        }

        private static void NativeTurnBasedExitUiLeaseRepairIsExact()
        {
            TestRunner.Equal(
                NativeTurnBasedExitUiLeaseDisposition.NotPending,
                NativeTurnBasedExitUiLeasePolicy.Classify(false, true, false, "Default", true, true, false),
                "An unarmed path acquired player UI ownership.");
            TestRunner.Equal(
                NativeTurnBasedExitUiLeaseDisposition.AwaitNativeRealtimeBoundary,
                NativeTurnBasedExitUiLeasePolicy.Classify(true, true, true, "Default", true, true, false),
                "The policy raced the native TB predicate.");
            TestRunner.Equal(
                NativeTurnBasedExitUiLeaseDisposition.AwaitNativeRealtimeBoundary,
                NativeTurnBasedExitUiLeasePolicy.Classify(true, true, false, "Pause", true, true, false),
                "The policy raced the exact native Pause boundary.");
            TestRunner.Equal(
                NativeTurnBasedExitUiLeaseDisposition.AwaitNativeRealtimeBoundary,
                NativeTurnBasedExitUiLeasePolicy.Classify(true, true, false, "Default", false, true, false),
                "The policy raced the Mammoth AI-lease boundary.");
            TestRunner.Equal(
                NativeTurnBasedExitUiLeaseDisposition.RejectInexactPair,
                NativeTurnBasedExitUiLeasePolicy.Classify(true, false, false, "Default", true, true, false),
                "An unmounted relationship acquired UI ownership.");
            TestRunner.Equal(
                NativeTurnBasedExitUiLeaseDisposition.RejectInexactPair,
                NativeTurnBasedExitUiLeasePolicy.Classify(true, true, false, "Default", true, false, false),
                "A replaced rider view was selected by the repair.");
            TestRunner.Equal(
                NativeTurnBasedExitUiLeaseDisposition.AlreadyExact,
                NativeTurnBasedExitUiLeasePolicy.Classify(true, true, false, "Default", true, true, true),
                "An exact rider selection requested a write.");
            TestRunner.Equal(
                NativeTurnBasedExitUiLeaseDisposition.RestoreExactRiderSelection,
                NativeTurnBasedExitUiLeasePolicy.Classify(true, true, false, "Default", true, true, false),
                "The exact post-native-exit rider selection loss was not isolated.");
        }

        private static void StockAttackRejectionIsPairLocal()
        {
            TestRunner.True(MountedStockAttackPolicy.ShouldReject(true, true, false, true), "Exact mounted stock rider attack escaped rejection.");
            TestRunner.True(MountedStockAttackPolicy.ShouldReject(true, false, true, true), "Exact mounted stock Mammoth attack escaped the explicit-primary boundary.");
            TestRunner.True(!MountedStockAttackPolicy.ShouldReject(false, true, false, true), "Unmounted rider attack was changed.");
            TestRunner.True(!MountedStockAttackPolicy.ShouldReject(true, false, false, true), "Non-pair attack was changed.");
            TestRunner.True(!MountedStockAttackPolicy.ShouldReject(true, true, false, false), "A non-exact UnitAttack subclass, including an opportunity or KMC child attack, was changed.");
            TestRunner.Equal("Mounted ranged attacks are not supported in this private alpha.", MountedStockAttackPolicy.RejectionFeedback(false, true), "Mounted ranged feedback changed.");
            TestRunner.Equal("Use Mammoth primary, then click one visible hostile target.", MountedStockAttackPolicy.RejectionFeedback(true, false), "Mounted Mammoth stock-attack feedback changed.");
        }

        private static void NativeTurnSelectionPreservesActorOwnership()
        {
            TestRunner.Equal(MountedSelectionDisposition.ProjectMountToRider, MountedTurnSelectionPolicy.Classify(true, true, false, false), "Real-time Mammoth selection escaped rider-principal projection.");
            TestRunner.Equal(MountedSelectionDisposition.ProjectMountToRider, MountedTurnSelectionPolicy.Classify(true, true, true, false), "A non-Mammoth TB turn exposed the Mammoth principal.");
            TestRunner.Equal(MountedSelectionDisposition.PreserveNativeMountTurn, MountedTurnSelectionPolicy.Classify(true, true, true, true), "The exact native Mammoth turn was projected onto the wrong actor.");
            TestRunner.Equal(MountedSelectionDisposition.Unchanged, MountedTurnSelectionPolicy.Classify(false, true, true, true), "Unmounted selection was changed.");
            TestRunner.True(MountedTurnSelectionPolicy.CanUseNativeMountTurnGroundCommand(true, true, true, true, true), "Exact Mammoth-turn ground input was not admitted.");
            TestRunner.True(!MountedTurnSelectionPolicy.CanUseNativeMountTurnGroundCommand(true, true, false, true, true), "Mammoth ground input was admitted on the wrong turn.");
            TestRunner.True(MountedTurnSelectionPolicy.IsExpectedActionSelection(true, true, false, true), "Mammoth primary did not require exact Mammoth TB selection.");
            TestRunner.True(!MountedTurnSelectionPolicy.IsExpectedActionSelection(true, true, true, false), "Mammoth primary accepted rider selection during the Mammoth turn.");
            TestRunner.True(MountedTurnSelectionPolicy.IsExpectedActionSelection(true, false, true, false), "Rider melee rejected exact rider selection.");
        }

        private static void DistantDoorRoutingIsExact()
        {
            TestRunner.True(MountedInteractionRoutingPolicy.ShouldRouteExactDoor(true, true, true, true), "Exact mounted rider StandardDoor interaction was not routed.");
            TestRunner.True(!MountedInteractionRoutingPolicy.ShouldRouteExactDoor(false, true, true, true), "Unmounted door interaction was changed.");
            TestRunner.True(!MountedInteractionRoutingPolicy.ShouldRouteExactDoor(true, false, true, true), "Mammoth or foreign command ownership was adopted.");
            TestRunner.True(!MountedInteractionRoutingPolicy.ShouldRouteExactDoor(true, true, false, true), "A derived or non-stock interaction command was adopted.");
            TestRunner.True(!MountedInteractionRoutingPolicy.ShouldRouteExactDoor(true, true, true, false), "A chest, switch, trap, or other non-door interaction was adopted without evidence.");
            TestRunner.True(MountedInteractionRoutingPolicy.CanAdmitInCurrentTurn(false, false, false, false), "Real-time exact door interaction was rejected.");
            TestRunner.True(MountedInteractionRoutingPolicy.CanAdmitInCurrentTurn(true, true, true, false), "Rider Preparing turn rejected exact door interaction.");
            TestRunner.True(MountedInteractionRoutingPolicy.CanAdmitInCurrentTurn(true, true, false, true), "Rider Acting turn rejected exact door interaction.");
            TestRunner.True(!MountedInteractionRoutingPolicy.CanAdmitInCurrentTurn(true, false, true, true), "Door interaction escaped exact rider-turn ownership.");
        }

        private static void OverlayWorldClickGuardIsBounded()
        {
            var guard = new MountedOverlayWorldInputGuard();
            guard.MarkActivation(100);
            TestRunner.True(guard.TryConsumePropagatedWorldClick(101), "The immediate post-overlay world click was not suppressed.");
            TestRunner.True(!guard.TryConsumePropagatedWorldClick(101), "The overlay world-click guard suppressed more than one click.");

            guard.MarkActivation(200);
            TestRunner.True(!guard.TryConsumePropagatedWorldClick(203), "A later deliberate world click remained suppressed.");
            guard.MarkActivation(300);
            guard.Clear();
            TestRunner.True(!guard.TryConsumePropagatedWorldClick(300), "A cleared overlay guard retained input ownership.");
        }

        private static void ExplainsIntentionalCleanupBoundaries()
        {
            TestRunner.True(
                MountedCleanupFeedbackPolicy.Describe(CleanupTrigger.SaveRequested).IndexOf("saving", StringComparison.OrdinalIgnoreCase) >= 0,
                "Save cleanup feedback did not identify the save boundary.");
            TestRunner.True(
                MountedCleanupFeedbackPolicy.Describe(CleanupTrigger.AreaUnloading).IndexOf("area", StringComparison.OrdinalIgnoreCase) >= 0,
                "Area cleanup feedback did not identify the area boundary.");
            TestRunner.True(
                MountedCleanupFeedbackPolicy.Describe(CleanupTrigger.ViewReplaced).IndexOf("body", StringComparison.OrdinalIgnoreCase) >= 0,
                "Body/view replacement feedback did not explain the automatic dismount.");
        }
    }
}
