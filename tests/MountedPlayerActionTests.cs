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
            runner.Run("player action reflects the domain restriction on combat Mount", RejectsUnavailableCombatMount);
            runner.Run("player action reports exact combat Mount gates", ReportsCombatMountGates);
            runner.Run("player action charges combat Dismount only on rider turn with Move", GatesCombatDismount);
            runner.Run("native Move commitment preserves Dismount but cannot authorize combat Mount", AdmitsCommittedNativeMoveShell);
            runner.Run("admitted native Move shell preserves non-resource combat gates", CommittedNativeMoveShellPreservesOtherGates);
            runner.Run("combat mount adjacency includes both native corpulence radii", UsesNativeAdjacencyEnvelope);
            runner.Run("native Mount approach stops inside its execution envelope", MountApproachMatchesExecution);
            runner.Run("player action becomes dismount while mounted", BecomesDismountWhileMounted);
            runner.Run("player action permits fault cleanup when feature disabled", PermitsFaultCleanupWhenFeatureDisabled);
            runner.Run("player action rejects double activation during transition", RejectsDoubleActivationDuringTransition);
            runner.Run("player action feedback follows an external mount transition", FeedbackFollowsExternalMountTransition);
            runner.Run("player action feedback retains an operation result while availability is stable", FeedbackRetainsStableOperationResult);
        }

        private static void MountApproachMatchesExecution()
        {
            float radius;
            TestRunner.True(CombatMountDismountPolicy.TryGetMountApproachRadius(1000000f, 0.5f, 1.1f, out radius), "Mount approach unavailable.");
            TestRunner.True(radius < 3.1f && radius > 3f &&
                CombatMountDismountPolicy.IsAdjacent(radius, 0.5f, 1.1f), "Native approach stops beyond execution reach.");
            TestRunner.True(CombatMountDismountPolicy.TryGetMountApproachRadius(2f, 0.5f, 1.1f, out radius) && radius == 2f,
                "Mount approach enlarged a tighter native radius.");
            TestRunner.True(CombatMountDismountPolicy.TryGetMountApproachRadius(float.PositiveInfinity, 0.5f, 1.1f, out radius) &&
                radius > 3f && radius < 3.1f && CombatMountDismountPolicy.IsAdjacent(radius, 0.5f, 1.1f),
                "Native AbilityRange.Unlimited must approach inside legal Mount adjacency.");
            TestRunner.True(!CombatMountDismountPolicy.TryGetMountApproachRadius(float.NegativeInfinity, 0.5f, 1.1f, out radius),
                "Negative infinite native radius admitted.");
            TestRunner.True(!CombatMountDismountPolicy.TryGetMountApproachRadius(float.NaN, 0.5f, 1.1f, out radius), "Invalid native radius admitted.");
            TestRunner.True(!CombatMountDismountPolicy.TryGetMountApproachRadius(5f, -1f, 1.1f, out radius), "Missing body radius admitted.");
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
            context.ExactActiveOwnedSupportedMount = false;
            context.RiderIsAliveAndConscious = false;
            context.MountIsAliveAndConscious = false;
            context.RiderIsDirectlyControllableAndInGame = false;
            context.MountIsDirectlyControllableAndInGame = false;
            context.ConflictingMountedRelationship = true;
            context.UnsupportedPolymorphOrSizeState = true;
            context.LoadingTransitionOrCutscene = true;
            context.InCombat = true;
            context.CombatTurnEligible = false;
            context.RiderHasMoveAction = false;
            context.PairAdjacent = false;
            context.SafeGameMode = false;
            context.ViewsAndStockAgentsAvailable = false;
            context.StockAgentsReady = false;
            context.AgentOverridesAvailable = false;

            var result = MountedPlayerActionEvaluator.Evaluate(context);
            var feedback = string.Join(" ", result.UnavailableReasons.ToArray());
            TestRunner.Equal(false, result.IsEnabled, "Invalid pair was accepted.");
            TestRunner.True(feedback.Contains("Medium"), "Current-size reason missing.");
            TestRunner.True(feedback.Contains("body rig"), "Body-profile reason missing.");
            TestRunner.True(feedback.Contains("active supported mount"), "Owned-supported-mount reason missing.");
            TestRunner.True(feedback.Contains("alive and conscious"), "Life-state reason missing.");
            TestRunner.True(feedback.Contains("directly controllable"), "Control/area reason missing.");
            TestRunner.True(feedback.Contains("conflicting"), "Relationship-conflict reason missing.");
            TestRunner.True(feedback.Contains("Polymorphed"), "Polymorph reason missing.");
            TestRunner.True(feedback.Contains("loading"), "Lifecycle-boundary reason missing.");
            TestRunner.True(feedback.Contains("adjacent") && feedback.Contains("current turn") &&
                    feedback.Contains("no Move action"),
                "Combat Mount gate reasons missing.");
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

        private static void RejectsUnavailableCombatMount()
        {
            var context = EligibleContext();
            context.InCombat = true;
            context.CombatTurnEligible = true;
            context.RiderHasMoveAction = true;
            context.PairAdjacent = true;
            var result = MountedPlayerActionEvaluator.Evaluate(context);
            TestRunner.True(!result.IsEnabled && result.Feedback.Contains("outside combat"),
                "Mount UI advertised combat admission that the relationship domain rejects.");
        }

        private static void ReportsCombatMountGates()
        {
            var context = EligibleContext();
            context.InCombat = true;
            context.CombatTurnEligible = false;
            context.RiderHasMoveAction = false;
            context.PairAdjacent = false;
            var result = MountedPlayerActionEvaluator.Evaluate(context);
            TestRunner.True(!result.IsEnabled, "Ineligible combat Mount was admitted.");
            TestRunner.True(result.Feedback.Contains("adjacent"), "Combat adjacency reason missing.");
            TestRunner.True(result.Feedback.Contains("current turn"), "Combat turn reason missing.");
            TestRunner.True(result.Feedback.Contains("no Move action"), "Combat Move-action reason missing.");
        }

        private static void GatesCombatDismount()
        {
            var context = EligibleContext();
            context.RelationshipState = RelationshipState.Mounted;
            context.InCombat = true;
            context.CombatTurnEligible = false;
            context.RiderHasMoveAction = false;
            var result = MountedPlayerActionEvaluator.Evaluate(context);
            TestRunner.True(!result.IsEnabled, "Combat Dismount bypassed rider turn/action gates.");
            TestRunner.True(result.Feedback.Contains("rider-led current turn"),
                "Combat Dismount turn feedback missing.");
            TestRunner.True(result.Feedback.Contains("no Move action"),
                "Combat Dismount Move feedback missing.");
        }

        private static void AdmitsCommittedNativeMoveShell()
        {
            var mountContext = EligibleContext();
            mountContext.InCombat = true;
            mountContext.RiderHasMoveAction = false;
            mountContext.NativeMoveActionShellAdmitted = true;
            var mount = MountedPlayerActionEvaluator.Evaluate(mountContext);
            TestRunner.True(!mount.IsEnabled && mount.Feedback.Contains("outside combat"),
                "A native Move shell bypassed the current relationship's combat Mount restriction.");

            var dismountContext = EligibleContext();
            dismountContext.RelationshipState = RelationshipState.Mounted;
            dismountContext.InCombat = true;
            dismountContext.RiderHasMoveAction = false;
            dismountContext.NativeMoveActionShellAdmitted = true;
            var dismount = MountedPlayerActionEvaluator.Evaluate(dismountContext);
            TestRunner.True(dismount.IsEnabled,
                "An exact native Dismount shell was rejected after Kingmaker admitted its Move resource.");
        }

        private static void CommittedNativeMoveShellPreservesOtherGates()
        {
            var mountContext = EligibleContext();
            mountContext.InCombat = true;
            mountContext.RiderHasMoveAction = false;
            mountContext.NativeMoveActionShellAdmitted = true;
            mountContext.PairAdjacent = false;
            mountContext.CombatTurnEligible = false;
            var mount = MountedPlayerActionEvaluator.Evaluate(mountContext);
            TestRunner.True(!mount.IsEnabled && mount.Feedback.Contains("adjacent") &&
                    mount.Feedback.Contains("current turn") && !mount.Feedback.Contains("no Move action"),
                "Native Mount delivery bypassed a non-resource combat gate.");

            var dismountContext = EligibleContext();
            dismountContext.RelationshipState = RelationshipState.Mounted;
            dismountContext.InCombat = true;
            dismountContext.RiderHasMoveAction = false;
            dismountContext.NativeMoveActionShellAdmitted = true;
            dismountContext.CombatTurnEligible = false;
            var dismount = MountedPlayerActionEvaluator.Evaluate(dismountContext);
            TestRunner.True(!dismount.IsEnabled && dismount.Feedback.Contains("rider-led current turn") &&
                    !dismount.Feedback.Contains("no Move action"),
                "Native Dismount delivery bypassed the rider-turn gate.");
        }

        private static void UsesNativeAdjacencyEnvelope()
        {
            TestRunner.True(
                CombatMountDismountPolicy.IsAdjacent(2.8f, 0.5f, 0.8f),
                "Native adjacent reach rejected touching actor radii.");
            TestRunner.True(
                !CombatMountDismountPolicy.IsAdjacent(2.81f, 0.5f, 0.8f),
                "Combat Mount adjacency leaked beyond the bounded native envelope.");
            TestRunner.True(
                !CombatMountDismountPolicy.IsAdjacent(float.NaN, 0.5f, 0.8f),
                "Nonfinite combat Mount distance was accepted.");
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

        private static void FeedbackFollowsExternalMountTransition()
        {
            var feedback = new MountedPlayerActionFeedbackState("stale initial feedback");
            var mount = MountedPlayerActionEvaluator.Evaluate(EligibleContext());
            TestRunner.Equal("Ready to mount.", feedback.ObserveAvailability(mount), "Eligible Mount feedback was not projected.");

            var mountedContext = EligibleContext();
            mountedContext.RelationshipState = RelationshipState.Mounted;
            var dismount = MountedPlayerActionEvaluator.Evaluate(mountedContext);
            TestRunner.Equal(
                "Mounted relationship is active.",
                feedback.ObserveAvailability(dismount),
                "Direct automation mount left stale Mount feedback beside Dismount.");
        }

        private static void FeedbackRetainsStableOperationResult()
        {
            var feedback = new MountedPlayerActionFeedbackState(string.Empty);
            var availability = MountedPlayerActionEvaluator.Evaluate(EligibleContext());
            feedback.ObserveAvailability(availability);
            feedback.SetOperationFeedback("Exact transition result.");
            TestRunner.Equal(
                "Exact transition result.",
                feedback.ObserveAvailability(availability),
                "A stable availability projection erased the operation result.");
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
                ExactActiveOwnedSupportedMount = true,
                MountDisplayName = "Horse",
                MountIsStrictlyLarger = true,
                RiderIsAliveAndConscious = true,
                MountIsAliveAndConscious = true,
                RiderIsDirectlyControllableAndInGame = true,
                MountIsDirectlyControllableAndInGame = true,
                ConflictingMountedRelationship = false,
                UnsupportedPolymorphOrSizeState = false,
                LoadingTransitionOrCutscene = false,
                InCombat = false,
                CombatTurnEligible = true,
                RiderHasMoveAction = true,
                PairAdjacent = true,
                SafeGameMode = true,
                ViewsAndStockAgentsAvailable = true,
                StockAgentsReady = true,
                AgentOverridesAvailable = true
            };
        }
    }
}
