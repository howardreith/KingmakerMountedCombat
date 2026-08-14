using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedPlayerActionTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("player action is hidden without a loaded game", HiddenWithoutLoadedGame);
            runner.Run("player action reports exact selection requirement", ReportsSelectionRequirement);
            runner.Run("player action accepts a complete eligible pair", AcceptsEligiblePair);
            runner.Run("player action reports every material pair rejection", ReportsMaterialPairRejections);
            runner.Run("player action becomes dismount while mounted", BecomesDismountWhileMounted);
            runner.Run("player action permits fault cleanup when feature disabled", PermitsFaultCleanupWhenFeatureDisabled);
            runner.Run("player action rejects double activation during transition", RejectsDoubleActivationDuringTransition);
        }

        private static void HiddenWithoutLoadedGame()
        {
            var context = EligibleContext();
            context.GameAvailable = false;
            var result = MountedPlayerActionEvaluator.Evaluate(context);
            TestRunner.Equal(false, result.IsVisible, "Action was visible without a loaded game.");
            TestRunner.Equal(MountedPlayerActionKind.None, result.Action, "Hidden action retained an executable kind.");
        }

        private static void ReportsSelectionRequirement()
        {
            var context = EligibleContext();
            context.ExactlyOneRiderSelected = false;
            var result = MountedPlayerActionEvaluator.Evaluate(context);
            TestRunner.Equal(true, result.IsVisible, "Action should remain visible for eligibility feedback.");
            TestRunner.Equal(false, result.IsEnabled, "Action accepted ambiguous selection.");
            TestRunner.True(result.Feedback.Contains("Select exactly one"), "Selection rejection was not exact.");
        }

        private static void AcceptsEligiblePair()
        {
            var result = MountedPlayerActionEvaluator.Evaluate(EligibleContext());
            TestRunner.Equal(true, result.IsVisible, "Eligible action was hidden.");
            TestRunner.Equal(true, result.IsEnabled, "Eligible action was disabled.");
            TestRunner.Equal(MountedPlayerActionKind.Mount, result.Action, "Eligible action was not Mount.");
            TestRunner.Equal(0, result.UnavailableReasons.Count, "Eligible action retained rejection reasons.");
        }

        private static void ReportsMaterialPairRejections()
        {
            var context = EligibleContext();
            context.RiderIsExactlyMedium = false;
            context.RiderBodyProfileSupported = false;
            context.ExactActiveOwnedMammoth = false;
            context.RiderIsAliveAndConscious = false;
            context.MountIsAliveAndConscious = false;
            context.RiderIsDirectlyControllableAndInGame = false;
            context.MountIsDirectlyControllableAndInGame = false;
            context.ConflictingMountedRelationship = true;
            context.UnsupportedPolymorphOrSizeState = true;
            context.LoadingTransitionOrCutscene = true;
            context.InCombat = true;
            context.SafeGameMode = false;
            context.ViewsAndStockAgentsAvailable = false;
            context.StockAgentsReady = false;
            context.AgentOverridesAvailable = false;

            var result = MountedPlayerActionEvaluator.Evaluate(context);
            var feedback = string.Join(" ", result.UnavailableReasons.ToArray());
            TestRunner.Equal(false, result.IsEnabled, "Invalid pair was accepted.");
            TestRunner.True(feedback.Contains("Medium"), "Current-size reason missing.");
            TestRunner.True(feedback.Contains("body rig"), "Body-profile reason missing.");
            TestRunner.True(feedback.Contains("exact active Mammoth"), "Owned-Mammoth reason missing.");
            TestRunner.True(feedback.Contains("alive and conscious"), "Life-state reason missing.");
            TestRunner.True(feedback.Contains("directly controllable"), "Control/area reason missing.");
            TestRunner.True(feedback.Contains("conflicting"), "Relationship-conflict reason missing.");
            TestRunner.True(feedback.Contains("Polymorphed"), "Polymorph reason missing.");
            TestRunner.True(feedback.Contains("loading"), "Lifecycle-boundary reason missing.");
            TestRunner.True(feedback.Contains("outside combat"), "Combat reason missing.");
            TestRunner.True(feedback.Contains("ordinary exploration"), "Game-mode reason missing.");
            TestRunner.True(feedback.Contains("views and stock movement agents"), "View/agent reason missing.");
            TestRunner.True(feedback.Contains("enabled before mounting"), "Stock-agent readiness reason missing.");
            TestRunner.True(feedback.Contains("incompatible movement-agent override"), "Override-conflict reason missing.");
        }

        private static void BecomesDismountWhileMounted()
        {
            var context = EligibleContext();
            context.RelationshipState = RelationshipState.Mounted;
            context.ExactlyOneRiderSelected = false;
            context.LoadingTransitionOrCutscene = true;
            var result = MountedPlayerActionEvaluator.Evaluate(context);
            TestRunner.Equal(true, result.IsEnabled, "Mounted cleanup was blocked by stale eligibility.");
            TestRunner.Equal(MountedPlayerActionKind.Dismount, result.Action, "Mounted action did not become Dismount.");
            TestRunner.Equal("Dismount", result.Label, "Mounted action label is unclear.");
        }

        private static void PermitsFaultCleanupWhenFeatureDisabled()
        {
            var context = EligibleContext();
            context.RelationshipState = RelationshipState.Faulted;
            context.FeatureEnabled = false;
            var result = MountedPlayerActionEvaluator.Evaluate(context);
            TestRunner.Equal(true, result.IsEnabled, "Fault cleanup depended on feature enablement.");
            TestRunner.Equal(MountedPlayerActionKind.Dismount, result.Action, "Fault cleanup did not use the dismount path.");
        }

        private static void RejectsDoubleActivationDuringTransition()
        {
            var context = EligibleContext();
            context.RelationshipState = RelationshipState.Mounting;
            var result = MountedPlayerActionEvaluator.Evaluate(context);
            TestRunner.Equal(false, result.IsEnabled, "Action accepted a double activation during Mounting.");
            TestRunner.True(result.Feedback.Contains("already in progress"), "Transition rejection reason missing.");
        }

        private static MountedPlayerActionContext EligibleContext()
        {
            return new MountedPlayerActionContext
            {
                RelationshipState = RelationshipState.Unmounted,
                GameAvailable = true,
                FeatureEnabled = true,
                ExactlyOneRiderSelected = true,
                RiderIsExactlyMedium = true,
                RiderBodyProfileSupported = true,
                ExactActiveOwnedMammoth = true,
                MountIsStrictlyLarger = true,
                RiderIsAliveAndConscious = true,
                MountIsAliveAndConscious = true,
                RiderIsDirectlyControllableAndInGame = true,
                MountIsDirectlyControllableAndInGame = true,
                ConflictingMountedRelationship = false,
                UnsupportedPolymorphOrSizeState = false,
                LoadingTransitionOrCutscene = false,
                InCombat = false,
                SafeGameMode = true,
                ViewsAndStockAgentsAvailable = true,
                StockAgentsReady = true,
                AgentOverridesAvailable = true
            };
        }
    }
}
