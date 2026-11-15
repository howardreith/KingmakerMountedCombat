using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Offline decision behavior of the mounted attack stall repair. These checks execute no game command; the
    // controller's own terminal handling on genuine native types is Test-MountedAttackStallRepair.ps1.
    internal static class MountedAttackExecutionTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("a turn-based attack order is refused for an actor whose own conditions forbid acting", TurnBasedAdmissionRefusesBlockedActor);
            runner.Run("a real-time attack order keeps the native pending behavior for a blocked actor", RealTimeAdmissionKeepsNativeWait);
            runner.Run("a started routine never releases an attack for a blocked actor in either mode", ExecutionBoundaryBlocksEveryMode);
            runner.Run("an unreadable actor state refuses nothing", UnknownStateRefusesNothing);
            runner.Run("a remaining Standard action does not admit an actor that cannot act", StandardActionIsNotPermissionToAct);
            runner.Run("rider and mount are judged on their own conditions only", ActorsDoNotShareCapability);
            runner.Run("a capability refusal answers the player once and never an automatic proposal", CapabilityRefusalFeedback);
            runner.Run("native Success without a delivered attack is not a completed attack", SuccessWithoutExecutionIsNotCompletion);
            runner.Run("executed attacks keep their order: full attacks, single attacks, misses and the ranged tail", ExecutedAttacksRetainIntent);
            runner.Run("failed terminals keep their existing retirement", FailedTerminalsRetire);
            runner.Run("an unexecuted terminal retires one intent generation and leaves a fresh order unrestricted", UnexecutedTerminalRetiresOneGeneration);
            runner.Run("unexecuted feedback names the actor and the cause, never a completion", UnexecutedFeedbackIsTruthful);
        }

        private static MountedActorCapabilityFacts Healthy() => new MountedActorCapabilityFacts { Known = true };

        private static readonly MountedActorCapabilityBlock[] Blocks =
        {
            MountedActorCapabilityBlock.Helpless, MountedActorCapabilityBlock.Stunned, MountedActorCapabilityBlock.Dazed,
            MountedActorCapabilityBlock.CantAct, MountedActorCapabilityBlock.CanNotAttack
        };

        private static MountedActorCapabilityFacts With(MountedActorCapabilityBlock block)
        {
            var facts = Healthy();
            facts.Helpless = block == MountedActorCapabilityBlock.Helpless;
            facts.Stunned = block == MountedActorCapabilityBlock.Stunned;
            facts.Dazed = block == MountedActorCapabilityBlock.Dazed;
            facts.CantAct = block == MountedActorCapabilityBlock.CantAct;
            facts.CanNotAttack = block == MountedActorCapabilityBlock.CanNotAttack;
            return facts;
        }

        private static void TurnBasedAdmissionRefusesBlockedActor()
        {
            TestRunner.Equal(MountedActorCapabilityBlock.None, MountedActorCapabilityPolicy.AdmissionBlock(Healthy(), true),
                "A healthy actor was refused in turn-based combat.");
            foreach (var block in Blocks)
                TestRunner.Equal(block, MountedActorCapabilityPolicy.AdmissionBlock(With(block), true),
                    "A turn-based order was admitted for a blocked actor: " + block + ".");
        }

        private static void RealTimeAdmissionKeepsNativeWait()
        {
            // Native real time leaves the unstarted order in its slot and starts it when the condition ends.
            foreach (var block in Blocks)
                TestRunner.Equal(MountedActorCapabilityBlock.None, MountedActorCapabilityPolicy.AdmissionBlock(With(block), false),
                    "A real-time order lost the native pending behavior: " + block + ".");
        }

        private static void ExecutionBoundaryBlocksEveryMode()
        {
            TestRunner.Equal(MountedActorCapabilityBlock.None, MountedActorCapabilityPolicy.ExecutionBlock(Healthy()),
                "A healthy actor's started routine was ended.");
            foreach (var block in Blocks)
                TestRunner.Equal(block, MountedActorCapabilityPolicy.ExecutionBlock(With(block)),
                    "A started routine could release an attack for a blocked actor: " + block + ".");
            // The attack ban is reported ahead of a simultaneous inability to act.
            var both = With(MountedActorCapabilityBlock.Dazed);
            both.CanNotAttack = true;
            TestRunner.Equal(MountedActorCapabilityBlock.CanNotAttack, MountedActorCapabilityPolicy.ExecutionBlock(both),
                "Two simultaneous restrictions did not report one stable cause.");
        }

        private static void UnknownStateRefusesNothing()
        {
            var unknown = With(MountedActorCapabilityBlock.Dazed);
            unknown.Known = false;
            TestRunner.Equal(MountedActorCapabilityBlock.None, MountedActorCapabilityPolicy.AdmissionBlock(unknown, true),
                "An unreadable state refused an order.");
            TestRunner.Equal(MountedActorCapabilityBlock.None, MountedActorCapabilityPolicy.ExecutionBlock(unknown),
                "An unreadable state ended a routine.");
        }

        private static void StandardActionIsNotPermissionToAct()
        {
            var context = MountedCombatDomainTests.Eligible(MountedCombatActionKind.RiderRanged);
            context.RiderWeaponIsSupportedMelee = false;
            context.RiderWeaponIsRanged = true;
            TestRunner.True(MountedCombatActionEvaluator.Evaluate(context).IsAllowed, "The healthy ranged control was refused.");
            context.ActionActorDisplayName = "Royon";
            context.ActionActorCapabilityBlock = MountedActorCapabilityBlock.Dazed;
            TestRunner.True(context.ActionActorHasStandardAction, "The fixture lost its remaining Standard action.");
            var refused = MountedCombatActionEvaluator.Evaluate(context);
            TestRunner.True(!refused.IsAllowed, "A remaining Standard action admitted an actor that cannot act.");
            TestRunner.True(refused.RejectionCodes.Count == 1 && refused.RejectionCodes[0] == MountedCombatRejectionCode.ActorCannotAct,
                "The refusal was not the single capability code: " + string.Join(",", refused.RejectionCodes.Select(code => code.ToString()).ToArray()));
            TestRunner.Equal("Royon cannot act right now (dazed).", refused.Feedback, "The refusal did not name the actor and its condition.");
        }

        private static void ActorsDoNotShareCapability()
        {
            // Rider dazed, mount healthy: the rider's attack is refused, the mount's own attack is not.
            var riderFacts = With(MountedActorCapabilityBlock.Dazed);
            var mountFacts = Healthy();
            var rider = MountedCombatDomainTests.Eligible(MountedCombatActionKind.RiderMelee);
            rider.ActionActorCapabilityBlock = MountedActorCapabilityPolicy.AdmissionBlock(riderFacts, true);
            var mount = MountedCombatDomainTests.Eligible(MountedCombatActionKind.MountPrimaryNatural);
            mount.ActionActorCapabilityBlock = MountedActorCapabilityPolicy.AdmissionBlock(mountFacts, true);
            TestRunner.True(!MountedCombatActionEvaluator.Evaluate(rider).IsAllowed, "The blocked rider attacked.");
            var mountResult = MountedCombatActionEvaluator.Evaluate(mount);
            TestRunner.True(mountResult.IsAllowed, "The healthy mount lost its own attack to the rider's condition.");
            TestRunner.Equal(MountedCombatActor.Mount, mountResult.ResourceOwner, "The mount's attack borrowed another actor's resources.");
            // The reverse: a stunned mount leaves the rider's own attack admitted.
            rider.ActionActorCapabilityBlock = MountedActorCapabilityPolicy.AdmissionBlock(mountFacts, true);
            mount.ActionActorCapabilityBlock = MountedActorCapabilityPolicy.AdmissionBlock(With(MountedActorCapabilityBlock.Stunned), true);
            var riderResult = MountedCombatActionEvaluator.Evaluate(rider);
            TestRunner.True(riderResult.IsAllowed, "The healthy rider lost its own attack to the mount's condition.");
            TestRunner.Equal(MountedCombatActor.Rider, riderResult.ResourceOwner, "The rider's attack borrowed another actor's resources.");
            TestRunner.True(!MountedCombatActionEvaluator.Evaluate(mount).IsAllowed, "The blocked mount attacked.");
        }

        private static void CapabilityRefusalFeedback()
        {
            var codes = new[] { MountedCombatRejectionCode.ActorCannotAct };
            const string refusal = "Royon cannot act right now (dazed).";
            TestRunner.True(!UnifiedMountedStockAttackPolicy.IsRoutineContinuationRefusal(codes),
                "A capability refusal was classed as routine reassessment.");
            var intent = new MountedAttackIntent<object, object>();
            intent.Begin(new object(), new object(), false, true);
            TestRunner.Equal(refusal, intent.GetDispatchRejectionFeedback(refusal, codes), "A fresh blocked order lost its answer.");
            // The restriction arrived after an earlier accepted attack of the same order.
            intent.ObserveDispatch(false);
            TestRunner.Equal(refusal, intent.GetDispatchRejectionFeedback(refusal, codes), "A later blocked dispatch stayed silent.");
            intent.Cancel();
            TestRunner.Equal(null, intent.GetDispatchRejectionFeedback(refusal, codes), "A retired order answered again.");
            intent.Begin(new object(), null, false, true, automatic: true);
            TestRunner.Equal(null, intent.GetDispatchRejectionFeedback(refusal, codes), "An automatic proposal produced a player refusal.");
        }

        private static MountedAttackTerminalFacts Terminal(bool success, bool completed, bool tail, bool rule, int delivered) =>
            new MountedAttackTerminalFacts
            {
                NativeSuccess = success,
                TransactionCompleted = completed,
                NativeRangedTailTermination = tail,
                NativeAttackRuleObserved = rule,
                CompletedAttackCount = delivered
            };

        private static void SuccessWithoutExecutionIsNotCompletion()
        {
            // The reported signature: native Success, cancelled transaction, no sequence start, no attack rule.
            var forced = MountedAttackTerminalPolicy.Classify(Terminal(true, false, false, false, 0));
            TestRunner.Equal(MountedAttackTerminalKind.EndedWithoutExecution, forced, "A force-finished command read as executed.");
            TestRunner.True(!MountedAttackTerminalPolicy.RetainsIntent(forced), "An unexecuted command kept its order alive.");
            // An attack index without an observed rule, or a rule without a delivered attack, proves nothing either.
            TestRunner.Equal(MountedAttackTerminalKind.EndedWithoutExecution,
                MountedAttackTerminalPolicy.Classify(Terminal(true, false, false, false, 1)), "A bare attack index counted as delivery.");
            TestRunner.Equal(MountedAttackTerminalKind.EndedWithoutExecution,
                MountedAttackTerminalPolicy.Classify(Terminal(true, false, false, true, 0)), "A rule without a delivered attack counted as delivery.");
        }

        private static void ExecutedAttacksRetainIntent()
        {
            // A full attack and a single attack; a miss or a hit for no damage runs the same native rule and
            // completes the same transaction, so these facts are also the miss and zero-damage cases.
            foreach (var delivered in new[] { 3, 1 })
            {
                var completed = MountedAttackTerminalPolicy.Classify(Terminal(true, true, false, true, delivered));
                TestRunner.Equal(MountedAttackTerminalKind.Completed, completed, "A delivered routine was not a completion.");
                TestRunner.True(MountedAttackTerminalPolicy.RetainsIntent(completed), "A delivered routine lost its order.");
            }
            var tail = MountedAttackTerminalPolicy.Classify(Terminal(false, false, true, true, 3));
            TestRunner.Equal(MountedAttackTerminalKind.RangedTailDelivered, tail, "The ranged tail lost its classification.");
            TestRunner.True(MountedAttackTerminalPolicy.RetainsIntent(tail), "The ranged tail lost its repeat order.");
            var delivered2 = MountedAttackTerminalPolicy.Classify(Terminal(true, false, false, true, 2));
            TestRunner.Equal(MountedAttackTerminalKind.DeliveredWithoutCompletion, delivered2, "Delivered attacks read as unexecuted.");
            TestRunner.True(MountedAttackTerminalPolicy.RetainsIntent(delivered2), "Delivered attacks lost their order.");
        }

        private static void FailedTerminalsRetire()
        {
            // Stop, retarget, target death mid-routine, a fault: every non-success terminal retires as before,
            // whether or not attacks were already delivered.
            foreach (var facts in new[] { Terminal(false, false, false, false, 0), Terminal(false, false, false, true, 1) })
            {
                var kind = MountedAttackTerminalPolicy.Classify(facts);
                TestRunner.Equal(MountedAttackTerminalKind.Failed, kind, "A non-success terminal changed class.");
                TestRunner.True(!MountedAttackTerminalPolicy.RetainsIntent(kind), "A non-success terminal kept its order.");
            }
        }

        // The controller's terminal step in policy terms: an order survives only a terminal that retains it.
        private static void ObserveTerminal(MountedAttackIntent<object, object> intent, MountedAttackTerminalFacts facts)
        {
            if (!MountedAttackTerminalPolicy.RetainsIntent(MountedAttackTerminalPolicy.Classify(facts))) intent.Cancel();
        }

        private static void UnexecutedTerminalRetiresOneGeneration()
        {
            var intent = new MountedAttackIntent<object, object>();
            var target = new object(); var turn = new object();
            intent.Begin(target, turn, false, true);
            var generation = intent.Generation;
            intent.ObserveDispatch(false);
            ObserveTerminal(intent, Terminal(true, false, false, false, 0));
            TestRunner.True(intent.Target == null && !intent.Owns(target, generation), "The unexecuted order survived its terminal.");
            // The same target, later, once the condition has ended: a new generation with its own answer and dispatch.
            intent.Begin(target, turn, false, true);
            TestRunner.True(intent.Generation > generation && intent.Owns(target, intent.Generation), "A fresh order was not admitted.");
            TestRunner.Equal("refused", intent.GetRejectionFeedback("refused"), "A fresh order inherited the retired order's history.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchRider,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, true, true, true, false,
                    riderIsLegalActor: true, mountIsLegalActor: false), "A fresh legal order did not dispatch.");
            intent.ObserveDispatch(false);
            ObserveTerminal(intent, Terminal(true, true, false, true, 3));
            TestRunner.True(intent.Owns(target, intent.Generation), "A completed attack retired its order.");
        }

        private static void UnexecutedFeedbackIsTruthful()
        {
            TestRunner.Equal("Royon cannot act right now (dazed). The attack was not performed.",
                MountedAttackTerminalPolicy.UnexecutedFeedback("Royon", MountedActorCapabilityBlock.Dazed), "The cause was not named.");
            var unexplained = MountedAttackTerminalPolicy.UnexecutedFeedback("Royon", MountedActorCapabilityBlock.None);
            TestRunner.Equal("Royon could not perform the attack; the order was dropped.", unexplained, "An unexplained terminal was misreported.");
            foreach (var feedback in new[] { unexplained, MountedAttackTerminalPolicy.UnexecutedFeedback(null, MountedActorCapabilityBlock.CanNotAttack) })
            {
                TestRunner.True(feedback.IndexOf("completed", System.StringComparison.OrdinalIgnoreCase) < 0, "An unexecuted attack was presented as completed.");
                TestRunner.True(!MountedFeedbackPresentationPolicy.IsRoutine(feedback), "An unexecuted attack was hidden as routine status.");
            }
        }
    }
}
