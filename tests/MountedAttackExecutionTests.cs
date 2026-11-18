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
            runner.Run("sprint 6 A01 (production intent): a same-target click after the dispatched command ended is a fresh request with its own answer; a click while it runs continues it", SameTargetClickAfterTerminalIsFresh);
            runner.Run("sprint 6 A01 (production intent): the fresh boundary holds for single, full, mount and ranged-tail routines, automatic promotion, retarget and boundaries", FreshBoundaryAcrossShapes);
            runner.Run("sprint 6 A02 (production policy): a condition never explains an explicit end; explicit ends keep their own cause", TerminalCauseSeparatesCapability);
            runner.Run("sprint 6 A05 (production policy, review finding 1): only a typed, evidenced failed delegated approach records its target unreachable", ApproachFailureNeedsAFailedApproach);
            runner.Run("sprint 6 A02 (production policy, review finding 3): a recorded capability cause keeps its exact block whatever the later snapshot", RecordedCapabilityCauseIsDurable);
            runner.Run("sprint 6 A02 (production scopes, review finding 4): native Run scopes open and close exactly, nest, survive ownership changes and exceptions", NativeRunScopesAreExact);
            runner.Run("sprint 6 A01 (production intent, review finding 5): only the same request after its command ended is a fresh-after-terminal click", ClickClassificationCountsOnlyFreshness);
            runner.Run("sprint 6 A02 (production policy, review R18): native capability is causal only on the exact native branch that took it", NativeCapabilityNeedsTheExactBranch);
            runner.Run("sprint 6 A02 (production scopes, review R19): the innermost open Run on the ending command's container decides a replacement", NestedRunUsesTheInnermostToken);
            runner.Run("sprint 6 A02 (production policy, review R21): native terminal facts belong to their own native invocation and default to no capability", NativeObservationsAreFrameOwned);
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

        // The controller's click decision (ContinuesStockIntent): continue only while the request has not dispatched or its
        // dispatched command is live; otherwise begin a new generation (the existing replacement path).
        private static bool Continues(MountedAttackIntent<object, object> intent, object target, object turn, bool mountActor, bool commandLive) =>
            intent.CanContinue(target, turn, mountActor) && !intent.IsFreshRequestAfterTerminal(commandLive);

        private static void ClickAfterCompletion(bool mountActor, MountedAttackTerminalFacts completion, string shape)
        {
            var intent = new MountedAttackIntent<object, object>();
            var target = new object(); var turn = new object();
            intent.Begin(target, turn, mountActor, true);
            var generation = intent.Generation;
            intent.ObserveDispatch(mountActor);
            TestRunner.True(Continues(intent, target, turn, mountActor, true), shape + ": a click while the command runs did not continue it.");
            ObserveTerminal(intent, completion);
            TestRunner.True(intent.Owns(target, generation), shape + ": the executed routine did not keep its order.");
            // The command released its reference; before the next Update the same exact click arrives.
            TestRunner.True(intent.CanContinue(target, turn, mountActor), shape + ": setup, the retained order should still match the click.");
            TestRunner.True(!Continues(intent, target, turn, mountActor, false), shape + ": the click after the terminal continued finished work.");
            // Accepted Sprint 5 behaviour, for contrast: continuation kept hasDispatched, so the refusal was silent.
            TestRunner.Equal(null, intent.GetRejectionFeedback("No Standard action."), shape + ": setup, the retained order answers nothing.");
            intent.Begin(target, turn, mountActor, true);
            TestRunner.True(intent.Generation == generation + 1, shape + ": the fresh request is not a new generation.");
            TestRunner.Equal("No Standard action.", intent.GetRejectionFeedback("No Standard action."), shape + ": the fresh request has no answer of its own.");
        }

        private static void SameTargetClickAfterTerminalIsFresh()
        {
            ClickAfterCompletion(false, Terminal(true, true, false, true, 1), "rider single attack");
            // Spent Standard: the controller's resource step answers the fresh request once and never dispatches.
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent,
                UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, false, false, true, false,
                    riderIsLegalActor: true, mountIsLegalActor: false), "A spent rider dispatched again.");
            // Never dispatched yet (a paused click, a turn not reached): continuation stays.
            var waiting = new MountedAttackIntent<object, object>();
            var target = new object(); var turn = new object();
            waiting.Begin(target, turn, false, true);
            TestRunner.True(Continues(waiting, target, turn, false, false), "An order that never dispatched did not continue.");
        }

        private static void FreshBoundaryAcrossShapes()
        {
            ClickAfterCompletion(false, Terminal(true, true, false, true, 3), "rider full attack");
            ClickAfterCompletion(true, Terminal(true, true, false, true, 1), "mount attack");
            ClickAfterCompletion(false, Terminal(false, false, true, true, 2), "ranged tail");
            // Automatic origin promoted by a genuine click while live; once terminal, a later click is fresh.
            var auto = new MountedAttackIntent<object, object>();
            var target = new object(); var turn = new object();
            auto.Begin(target, turn, false, true, automatic: true);
            auto.ObserveDispatch(false);
            TestRunner.True(Continues(auto, target, turn, false, true), "A click on the live automatic command did not continue it.");
            auto.PromoteToPlayer();
            var generation = auto.Generation;
            TestRunner.True(!auto.Automatic && auto.Generation == generation, "Promotion changed the generation.");
            ObserveTerminal(auto, Terminal(true, true, false, true, 1));
            TestRunner.True(!Continues(auto, target, turn, false, false), "A click after the promoted command ended continued it.");
            // Retarget or another action context is a new request through the replacement path, as before.
            TestRunner.True(!auto.CanContinue(new object(), turn, false), "Another target continued the order.");
            TestRunner.True(!auto.CanContinue(target, turn, false, action: 9), "Another action continued the order.");
            // Two pairs: one pair's terminal state never changes the other's decision.
            var other = new MountedAttackIntent<object, object>();
            var otherTarget = new object();
            other.Begin(otherTarget, turn, false, true);
            other.ObserveDispatch(false);
            TestRunner.True(Continues(other, otherTarget, turn, false, true), "Another pair's live command was treated as finished.");
            // Stop, dismount, a load, an area or combat end all clear the order: nothing about freshness is stored.
            auto.Cancel();
            TestRunner.True(!auto.IsFreshRequestAfterTerminal(false) && !Continues(auto, target, turn, false, false),
                "A cleared order kept a stale fresh-request state.");
        }

        private static readonly MountedNativeTerminalBranch[] NonCapabilityBranches =
        {
            MountedNativeTerminalBranch.None, MountedNativeTerminalBranch.TurnBasedNoCommandSlot, MountedNativeTerminalBranch.AttackTargetDead,
            MountedNativeTerminalBranch.TargetUnavailable, MountedNativeTerminalBranch.InterruptAsSoonAsPossible,
            MountedNativeTerminalBranch.RunningInterruptAsSoonAsPossible
        };

        private static void TerminalCauseSeparatesCapability()
        {
            var dazed = MountedActorCapabilityBlock.Dazed;
            var none = MountedActorCapabilityBlock.None;
            // Explicit ends with a blocked actor: the condition never explains them, whatever native branch was offered.
            foreach (var cause in new[] { MountedAttackEndCause.Cancelled, MountedAttackEndCause.Replaced, MountedAttackEndCause.Fault,
                MountedAttackEndCause.TargetInvalid, MountedAttackEndCause.ApproachFailed })
                foreach (var branch in new[] { MountedNativeTerminalBranch.None, MountedNativeTerminalBranch.RunningCannotAct })
                    TestRunner.True(!MountedAttackTerminalCausePolicy.CapabilityIsCause(cause, none, branch, dazed), cause + " was explained by a daze.");
            TestRunner.Equal("Mounted pair attack cancelled.", MountedAttackTerminalCausePolicy.CancellationFeedback(MountedAttackEndCause.Cancelled), "Stop text.");
            TestRunner.Equal("Mounted pair attack replaced by a newer order.", MountedAttackTerminalCausePolicy.CancellationFeedback(MountedAttackEndCause.Replaced), "Replacement text.");
            TestRunner.True(MountedAttackTerminalCausePolicy.CancellationFeedback(MountedAttackEndCause.Unrecorded) == null &&
                MountedAttackTerminalCausePolicy.CancellationFeedback(MountedAttackEndCause.ActorCapability) == null, "A non-cancellation got cancellation text.");
            TestRunner.Equal(MountedAttackEndCause.Cancelled, MountedAttackTerminalCausePolicy.Resolve(MountedAttackEndCause.Cancelled, true), "A recorded cause was overwritten.");
            TestRunner.Equal(MountedAttackEndCause.Replaced, MountedAttackTerminalCausePolicy.Resolve(MountedAttackEndCause.Unrecorded, true), "A replacing Run was not recognised.");
            TestRunner.Equal(MountedAttackEndCause.Unrecorded, MountedAttackTerminalCausePolicy.Resolve(MountedAttackEndCause.Unrecorded, false), "A cause was invented.");
        }

        private static void RecordedCapabilityCauseIsDurable()
        {
            var dazed = MountedActorCapabilityBlock.Dazed;
            var none = MountedActorCapabilityBlock.None;
            var banned = MountedActorCapabilityBlock.CanNotAttack;
            // 1, 2, 3: the block recorded at the boundary is the cause; no native branch or later snapshot replaces it.
            foreach (var branch in new[] { MountedNativeTerminalBranch.None, MountedNativeTerminalBranch.AttackBanned })
                foreach (var other in new[] { none, banned })
                    TestRunner.Equal(dazed, MountedAttackTerminalCausePolicy.CausalCapability(MountedAttackEndCause.ActorCapability, dazed, branch, other),
                        "The recorded daze was replaced by " + branch + "/" + other + ".");
            TestRunner.Equal(banned, MountedAttackTerminalCausePolicy.CausalCapability(MountedAttackEndCause.ActorCapability, banned,
                MountedNativeTerminalBranch.None, none), "The recorded attack ban was lost.");
            TestRunner.Equal("Royon cannot attack right now.", MountedActorCapabilityPolicy.Feedback("Royon",
                MountedAttackTerminalCausePolicy.CausalCapability(MountedAttackEndCause.ActorCapability, banned, MountedNativeTerminalBranch.None, none)),
                "The feedback did not follow the recorded block.");
            // 4: a cancellation or a fault stays non-capability.
            foreach (var cause in new[] { MountedAttackEndCause.Cancelled, MountedAttackEndCause.Fault })
                TestRunner.Equal(none, MountedAttackTerminalCausePolicy.CausalCapability(cause, dazed, MountedNativeTerminalBranch.RunningCannotAct, dazed),
                    cause + " became capability.");
            // 6: no condition, no capability text.
            TestRunner.Equal(null, MountedActorCapabilityPolicy.Feedback("Royon", none), "No condition produced capability text.");
        }

        // Review of fa533e97 (R18): native capability is causal only on the exact native branch that took it.
        private static void NativeCapabilityNeedsTheExactBranch()
        {
            var none = MountedActorCapabilityBlock.None;
            var unrecorded = MountedAttackEndCause.Unrecorded;
            // 1: the unstarted turn-based force-finish because native's canAct read false: the block read at the branch.
            TestRunner.Equal(MountedNativeTerminalBranch.TurnBasedCannotAct, MountedNativeTerminalBranchPolicy.TurnBasedForceFinish(MountedNativeTerminalObservation.ObserveCanAct(false)),
                "The turn-based cannot-act branch was not identified.");
            TestRunner.Equal(MountedNativeTerminalBranch.TurnBasedNoCommandSlot, MountedNativeTerminalBranchPolicy.TurnBasedForceFinish(MountedNativeTerminalObservation.ObserveCanAct(true)),
                "A missing command slot was identified as a capability branch.");
            TestRunner.Equal(MountedNativeTerminalBranch.TurnBasedNoCommandSlot, MountedNativeTerminalBranchPolicy.TurnBasedForceFinish(MountedNativeTerminalObservation.NotObserved),
                "An uncaptured canAct was guessed to be false.");
            foreach (var block in new[] { MountedActorCapabilityBlock.Helpless, MountedActorCapabilityBlock.Stunned, MountedActorCapabilityBlock.Dazed, MountedActorCapabilityBlock.CantAct })
            {
                var read = MountedNativeTerminalBranchPolicy.Block(MountedNativeTerminalBranch.TurnBasedCannotAct, With(block));
                TestRunner.Equal(block, read, "The turn-based branch did not read the exact current block.");
                TestRunner.Equal(block, MountedAttackTerminalCausePolicy.CausalCapability(unrecorded, none, MountedNativeTerminalBranch.TurnBasedCannotAct, read),
                    "The turn-based capability branch was not causal.");
            }
            // An attack ban is not a CanAct term: a cannot-act branch beside a ban names the CanAct condition.
            var dazedAndBanned = With(MountedActorCapabilityBlock.Dazed); dazedAndBanned.CanNotAttack = true;
            TestRunner.Equal(MountedActorCapabilityBlock.Dazed, MountedNativeTerminalBranchPolicy.Block(MountedNativeTerminalBranch.RunningCannotAct, dazedAndBanned),
                "The CanAct branch named the attack ban.");
            TestRunner.Equal(none, MountedNativeTerminalBranchPolicy.Block(MountedNativeTerminalBranch.RunningCannotAct, Healthy()),
                "A CanAct term this reader does not name was guessed.");
            // 2: a running command interrupted because native's CanAct read false.
            TestRunner.Equal(MountedNativeTerminalBranch.RunningCannotAct, MountedNativeTerminalBranchPolicy.RunningSite(MountedNativeTerminalObservation.ObserveCanAct(false)), "The running cannot-act branch was missed.");
            TestRunner.Equal(MountedActorCapabilityBlock.Stunned, MountedAttackTerminalCausePolicy.CausalCapability(unrecorded, none,
                MountedNativeTerminalBranch.RunningCannotAct, MountedActorCapabilityBlock.Stunned), "The running capability branch was not causal.");
            // 3: the attack interrupted through ShouldBeInterrupted's CanNotAttack term.
            TestRunner.Equal(MountedNativeTerminalBranch.AttackBanned, MountedNativeTerminalBranchPolicy.StateSite(MountedNativeTerminalObservation.ObserveShouldBeInterrupted(true, true)), "The attack-ban branch was missed.");
            TestRunner.Equal(MountedActorCapabilityBlock.CanNotAttack, MountedNativeTerminalBranchPolicy.Block(MountedNativeTerminalBranch.AttackBanned, Healthy()),
                "The attack-ban branch did not carry its own block.");
            // 4, 5: the target left state, became untargetable or stealthed, or died before the start, beside a blocked actor.
            TestRunner.Equal(MountedNativeTerminalBranch.TargetUnavailable, MountedNativeTerminalBranchPolicy.StateSite(MountedNativeTerminalObservation.ObserveShouldBeInterrupted(false, false)), "A target branch was missed.");
            TestRunner.Equal(MountedNativeTerminalBranch.TargetUnavailable, MountedNativeTerminalBranchPolicy.StateSite(MountedNativeTerminalObservation.NotObserved),
                "An uncaptured ShouldBeInterrupted was guessed.");
            TestRunner.Equal(MountedNativeTerminalBranch.AttackTargetDead, MountedNativeTerminalBranchPolicy.StateSite(MountedNativeTerminalObservation.ObserveShouldBeInterrupted(true, false)), "A dead target was called an attack ban.");
            // 6: InterruptAsSoonAsPossible, unstarted or running, while blocked.
            TestRunner.Equal(MountedNativeTerminalBranch.RunningInterruptAsSoonAsPossible, MountedNativeTerminalBranchPolicy.RunningSite(MountedNativeTerminalObservation.NotObserved),
                "The InterruptAsSoonAsPossible short-circuit was called cannot-act.");
            TestRunner.Equal(MountedNativeTerminalBranch.RunningInterruptAsSoonAsPossible, MountedNativeTerminalBranchPolicy.RunningSite(MountedNativeTerminalObservation.ObserveCanAct(true)),
                "A CanAct that read true was called cannot-act.");
            // 4-8: every non-capability branch (and no branch at all: the paused previous command, InterruptMove,
            // InterruptAll, an unknown native end) beside every blocked block stays non-capability.
            foreach (var branch in NonCapabilityBranches)
            {
                TestRunner.True(!MountedNativeTerminalBranchPolicy.IsCapability(branch), branch + " was a capability branch.");
                foreach (var block in Blocks)
                    TestRunner.Equal(none, MountedAttackTerminalCausePolicy.CausalCapability(unrecorded, none, branch, block),
                        branch + " beside " + block + " became capability.");
            }
            TestRunner.True(MountedNativeTerminalBranchPolicy.IsTarget(MountedNativeTerminalBranch.TargetUnavailable) &&
                MountedNativeTerminalBranchPolicy.IsTarget(MountedNativeTerminalBranch.AttackTargetDead) &&
                !MountedNativeTerminalBranchPolicy.IsTarget(MountedNativeTerminalBranch.None), "The target branches were not named.");
        }

        private static void ApproachFailureNeedsAFailedApproach()
        {
            System.Func<MountedAttackEndCause, bool, int, int, bool, bool, bool> records = (cause, required, moves, started, success, tail) =>
                MountedAttackTerminalCausePolicy.RecordsAutomaticApproachFailure(true, started, tail, success, cause, required, moves);
            // 1: a genuine delegated approach failure records the target.
            TestRunner.True(records(MountedAttackEndCause.ApproachFailed, true, 1, 0, false, false), "A failed delegated approach was not recorded.");
            TestRunner.True(!MountedAttackTerminalCausePolicy.RecordsAutomaticApproachFailure(false, 0, false, false, MountedAttackEndCause.ApproachFailed, true, 1),
                "A player order recorded its target.");
            // 2-6: target death, unconsciousness, despawn, hostility change, temporary unattackability (all TargetInvalid).
            TestRunner.True(!records(MountedAttackEndCause.TargetInvalid, true, 1, 0, false, false), "Target invalidation recorded the target.");
            // 7: an in-range unrecorded interruption with no delegated move, and an unrecorded end after an approach.
            TestRunner.True(!records(MountedAttackEndCause.Unrecorded, false, 0, 0, false, false), "An in-range interruption recorded the target.");
            TestRunner.True(!records(MountedAttackEndCause.Unrecorded, true, 1, 0, false, false), "An unrecorded end was taken as a failed approach.");
            // 8-11: capability, cancellation, replacement, fault.
            foreach (var cause in new[] { MountedAttackEndCause.ActorCapability, MountedAttackEndCause.Cancelled, MountedAttackEndCause.Replaced, MountedAttackEndCause.Fault })
                TestRunner.True(!records(cause, true, 1, 0, false, false), cause + " recorded the target.");
            // 12: successful and ranged-tail work; an approach failure without its own evidence.
            TestRunner.True(!records(MountedAttackEndCause.ApproachFailed, true, 1, 1, true, false) && !records(MountedAttackEndCause.ApproachFailed, true, 1, 1, false, true),
                "A successful or ranged-tail terminal recorded the target.");
            TestRunner.True(!records(MountedAttackEndCause.ApproachFailed, false, 1, 0, false, false) && !records(MountedAttackEndCause.ApproachFailed, true, 0, 0, false, false) &&
                !records(MountedAttackEndCause.ApproachFailed, true, 1, 1, false, false), "An approach failure without its approach evidence recorded the target.");
        }

        private sealed class Container { }
        private sealed class Order { }

        private static void NativeRunScopesAreExact()
        {
            var scopes = new MountedNativeRunScopes<Container, Order>();
            var rider = new Container(); var otherPair = new Container();
            var pairCommand = new Order(); var newer = new Order();
            // 1: a normal Run opens and closes once.
            var token = scopes.Open(rider, newer);
            TestRunner.True(scopes.Depth == 1 && scopes.EndedInsideReplacingRun(rider, pairCommand) && !scopes.EndedInsideReplacingRun(rider, newer),
                "A normal Run did not scope its replacement.");
            scopes.Close(token);
            TestRunner.True(scopes.Depth == 0 && !scopes.EndedInsideReplacingRun(rider, pairCommand), "A normal Run left its scope open.");
            // 2, 3, 4: a refused command, a scene handoff or a dismount and remount change nothing here: the token, not the
            // current pair owner, closes the scope.
            token = scopes.Open(rider, newer);
            var owner = rider; owner = null;
            scopes.Close(token);
            TestRunner.True(scopes.Depth == 0 && owner == null, "An ownership change left the scope open.");
            // 5, 6: nested Runs on the same and another pair: an inner close never closes an outer scope.
            var outer = scopes.Open(rider, newer);
            var innerSame = scopes.Open(rider, new Order());
            var innerOther = scopes.Open(otherPair, new Order());
            scopes.Close(innerOther);
            TestRunner.True(scopes.Depth == 2 && scopes.EndedInsideReplacingRun(rider, pairCommand) && !scopes.EndedInsideReplacingRun(otherPair, pairCommand),
                "The inner Run on another pair closed or leaked the outer scope.");
            scopes.Close(innerSame);
            TestRunner.True(scopes.Depth == 1 && scopes.EndedInsideReplacingRun(rider, pairCommand), "The inner Run closed the outer one.");
            scopes.Close(outer);
            TestRunner.True(scopes.Depth == 0, "The outer Run left its scope open.");
            // 7: an exception inside the Run: the finally closes the token.
            try
            {
                var thrown = scopes.Open(rider, newer);
                try { throw new System.InvalidOperationException("native Run threw"); }
                finally { scopes.Close(thrown); }
            }
            catch (System.InvalidOperationException) { }
            TestRunner.True(scopes.Depth == 0, "An exception left the scope open.");
            // A token stranded above the one being closed goes with it, and an unknown token is harmless.
            var left = scopes.Open(rider, newer); scopes.Open(rider, new Order());
            scopes.Close(left); scopes.Close(null); scopes.Close(new MountedNativeRunScopes<Container, Order>.Token());
            TestRunner.True(scopes.Depth == 0, "A stranded inner token survived its outer close.");
            // 8: a later replacement is still classified after every case above.
            var later = scopes.Open(rider, newer);
            TestRunner.True(scopes.EndedInsideReplacingRun(rider, pairCommand) && !scopes.EndedInsideReplacingRun(otherPair, pairCommand),
                "A later replacement was not classified.");
            scopes.Close(later);
            scopes.Open(rider, newer); scopes.Reset();
            TestRunner.True(scopes.Depth == 0, "Reset kept a scope.");
        }

        // Review of dffd3e72 (R21): each native invocation's facts are its own values, encoded where native computed them and
        // passed to its own terminal; the zero default (not observed) is never a capability, and no fact can be shared.
        private static void NativeObservationsAreFrameOwned()
        {
            var notObserved = MountedNativeTerminalObservation.NotObserved;
            TestRunner.True(notObserved == default(int), "The not-observed fact is not the zero default of a fresh native local.");
            // 8, 9: two nested invocations, each with its own command: each terminal reads only its own frame's value.
            var outerCanAct = MountedNativeTerminalObservation.ObserveCanAct(false);
            var innerCanAct = MountedNativeTerminalObservation.ObserveCanAct(true);
            TestRunner.Equal(MountedNativeTerminalBranch.RunningInterruptAsSoonAsPossible, MountedNativeTerminalBranchPolicy.RunningSite(innerCanAct),
                "The inner command took the outer command's cannot-act fact.");
            TestRunner.Equal(MountedNativeTerminalBranch.RunningCannotAct, MountedNativeTerminalBranchPolicy.RunningSite(outerCanAct),
                "The outer command lost its own cannot-act fact to the inner observation.");
            var outerHealthy = MountedNativeTerminalObservation.ObserveCanAct(true);
            var innerBlocked = MountedNativeTerminalObservation.ObserveCanAct(false);
            TestRunner.Equal(MountedNativeTerminalBranch.TurnBasedNoCommandSlot, MountedNativeTerminalBranchPolicy.TurnBasedForceFinish(outerHealthy),
                "An inner observation made the outer command capability-caused.");
            TestRunner.Equal(MountedNativeTerminalBranch.TurnBasedCannotAct, MountedNativeTerminalBranchPolicy.TurnBasedForceFinish(innerBlocked),
                "The inner command lost its own fact.");
            // 10: a terminal whose frame never observed the value (an exception, a skipped site) is never a capability.
            TestRunner.True(!MountedNativeTerminalBranchPolicy.IsCapability(MountedNativeTerminalBranchPolicy.TurnBasedForceFinish(notObserved)) &&
                !MountedNativeTerminalBranchPolicy.IsCapability(MountedNativeTerminalBranchPolicy.StateSite(notObserved)) &&
                !MountedNativeTerminalBranchPolicy.IsCapability(MountedNativeTerminalBranchPolicy.RunningSite(notObserved)),
                "A missing observation invented a capability cause.");
            // The ban term counts only with the true value of the same reading.
            TestRunner.Equal(MountedNativeTerminalObservation.NotInterrupted, MountedNativeTerminalObservation.ObserveShouldBeInterrupted(false, true),
                "A ban term beside a false reading was counted.");
            // The three channels are independent encodings.
            TestRunner.True(MountedNativeTerminalObservation.CanActTrue != MountedNativeTerminalObservation.CanActFalse &&
                MountedNativeTerminalObservation.InterruptedByAttackBan != MountedNativeTerminalObservation.InterruptedByOtherTerm &&
                MountedNativeTerminalObservation.NotInterrupted != notObserved, "Two different facts share an encoding.");
        }

        // Review of fa533e97 (R19): only the innermost open Run on the ending command's container decides.
        private static void NestedRunUsesTheInnermostToken()
        {
            var scopes = new MountedNativeRunScopes<Container, Order>();
            var rider = new Container(); var otherPair = new Container();
            var pair = new Order(); var a = new Order(); var b = new Order();
            // 1: an ordinary Run of A replacing the pair command P.
            var outer = scopes.Open(rider, a);
            TestRunner.True(scopes.EndedInsideReplacingRun(rider, pair), "1: A did not replace P.");
            // 2: nested A -> B, P ends inside B: replaced by B.
            var inner = scopes.Open(rider, b);
            TestRunner.True(scopes.EndedInsideReplacingRun(rider, pair), "2: P was not replaced inside B.");
            // 3: B itself ends inside its own Run: not replaced by the outer A.
            TestRunner.True(!scopes.EndedInsideReplacingRun(rider, b), "3: B was labelled replaced by the outer A.");
            // 4: nested operations on another pair neither decide nor hide this one.
            var foreignCommand = new Order(); var foreignPair = new Order();
            var foreign = scopes.Open(otherPair, foreignCommand);
            TestRunner.True(!scopes.EndedInsideReplacingRun(rider, b) && scopes.EndedInsideReplacingRun(rider, pair) &&
                scopes.EndedInsideReplacingRun(otherPair, foreignPair) && !scopes.EndedInsideReplacingRun(otherPair, foreignCommand),
                "4: another pair's Run changed this pair's answer, or was not its own.");
            scopes.Close(foreign);
            // 5: the inner token closes; the outer attribution remains (now A decides: B ending is a replacement by A).
            scopes.Close(inner);
            TestRunner.True(scopes.Depth == 1 && scopes.EndedInsideReplacingRun(rider, b) && !scopes.EndedInsideReplacingRun(rider, a),
                "5: the outer attribution was lost after the inner close.");
            // 6: an exception unwinds the inner token through its finally.
            try
            {
                var thrown = scopes.Open(rider, b);
                try { throw new System.InvalidOperationException("inner Run threw"); }
                finally { scopes.Close(thrown); }
            }
            catch (System.InvalidOperationException) { }
            TestRunner.True(scopes.Depth == 1 && !scopes.EndedInsideReplacingRun(rider, a), "6: the exception left the inner token.");
            // 7: a stranded inner token goes with the outer close.
            scopes.Open(rider, b);
            scopes.Close(outer);
            TestRunner.True(scopes.Depth == 0 && !scopes.EndedInsideReplacingRun(rider, pair), "7: a stranded token survived.");
            // 8: reset at disable or disposal.
            scopes.Open(rider, a); scopes.Open(rider, b); scopes.Reset();
            TestRunner.True(scopes.Depth == 0 && !scopes.EndedInsideReplacingRun(rider, pair), "8: reset kept a token.");
            // 9: a later replacement still works.
            var later = scopes.Open(rider, a);
            TestRunner.True(scopes.EndedInsideReplacingRun(rider, pair) && !scopes.EndedInsideReplacingRun(rider, a), "9: a later replacement was not classified.");
            scopes.Close(later);
        }

        private static void ClickClassificationCountsOnlyFreshness()
        {
            var intent = new MountedAttackIntent<object, object>();
            var target = new object(); var turn = new object(); var rider = new object(); var mount = new object(); var weapon = new object();
            intent.Begin(target, turn, false, true, rider, mount, weapon, null, 2);
            intent.ObserveDispatch(false);
            TestRunner.Equal(MountedStockClick.Continue, intent.ClassifyClick(target, turn, false, rider, mount, weapon, null, 2, true), "A live command did not continue.");
            TestRunner.Equal(MountedStockClick.FreshAfterTerminal, intent.ClassifyClick(target, turn, false, rider, mount, weapon, null, 2, false),
                "The same request after its command ended was not fresh.");
            TestRunner.Equal(MountedStockClick.NewRequest, intent.ClassifyClick(new object(), turn, false, rider, mount, weapon, null, 2, false), "A retarget was counted as fresh.");
            TestRunner.Equal(MountedStockClick.NewRequest, intent.ClassifyClick(target, turn, false, rider, mount, weapon, null, 9, false), "An action change was counted as fresh.");
            TestRunner.Equal(MountedStockClick.NewRequest, intent.ClassifyClick(target, turn, false, rider, mount, new object(), null, 2, false), "A weapon change was counted as fresh.");
            TestRunner.Equal(MountedStockClick.NewRequest, intent.ClassifyClick(target, turn, true, rider, mount, weapon, null, 2, false), "An actor change was counted as fresh.");
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
