using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // AI-01 behavior over the production policy, gate and intent. The facts are the ones the controller reads from native
    // slots and queues at the UnitCommands.Run boundary.
    internal static class MountedAutomaticAttackPolicyTests
    {
        private sealed class Unit { }
        private sealed class Turn { }

        public static void Register(TestRunner runner)
        {
            runner.Run("an idle enabled rider's brain proposal becomes a new intent", IdleProposalAdmitted);
            runner.Run("repeated proposals on the intent's target continue it without a second intent", RepeatedProposalContinues);
            runner.Run("proposals for another target leave a live automatic intent alone", OtherTargetRefused);
            runner.Run("no click, manual, armed or mount operation is displaced by a proposal", HigherPriorityNeverDisplaced);
            runner.Run("only a real-time native rider brain proposal is admitted", OriginAndModeBoundaries);
            runner.Run("AI off, brain off and direct control refuse without touching the intent state", ActorBoundaries);
            runner.Run("hold allows only a target already in the rider's reach", HoldIsReachOnly);
            runner.Run("a queued future order refuses a proposal even when no slot is occupied", QueuedOrderCounts);
            runner.Run("the pair's own command, AI commands and reactions are not player operations", OwnedCommandsAreNotOperations);
            runner.Run("a swift or free command beside the routine does not displace it", CoexistingCommandsAreNotOperations);
            runner.Run("a live player cast retires a repeating intent of either origin in real time", NewerOperationRetires);
            runner.Run("automatic boundaries never retire a click-owned intent", ClickOwnedIntentKeepsItsBoundaries);
            runner.Run("stop holds proposals until a new order, not on the next tick", StopHoldsUntilOrder);
            runner.Run("an explicit AI off and on rearms a stopped pair", AiToggleRearmsStop);
            runner.Run("a stop only belongs to its combat, mode and relationship generation", StopClearsOnBoundaries);
            runner.Run("two pairs keep separate stop state and intents", PairsStaySeparate);
            runner.Run("a completed player move holds the pair until the next order", PlayerMoveHoldsUntilOrder);
            runner.Run("a click on the brain's target makes the intent the player's own", ClickPromotesAutomaticIntent);
            runner.Run("cancellation clears automatic origin for the next intent", CancelClearsOrigin);
            runner.Run("an automatic ranged intent never drags the mount into melee", RangedAutomaticKeepsMountOut);
            runner.Run("a new automatic target cannot create readiness a spent actor lacks", NewTargetGrantsNoReadiness);
        }

        private static MountedAutomaticProposalFacts Ready()
        {
            return new MountedAutomaticProposalFacts
            {
                AiOrigin = true,
                ExactMountedPair = true,
                OwnerIsRider = true,
                RiderDirectlyControllable = true,
                RiderAiEnabled = true,
                RiderBrainActive = true,
                TargetInRiderReach = true,
                TargetValid = true
            };
        }

        private static string Refusal(MountedAutomaticProposalFacts facts)
        {
            string refusal;
            var decision = MountedAutomaticAttackPolicy.Decide(facts, out refusal);
            TestRunner.True(decision == MountedAutomaticProposalDecision.Refuse == (refusal != null), "Decision and refusal disagree.");
            return refusal;
        }

        private static void IdleProposalAdmitted()
        {
            string refusal;
            TestRunner.True(MountedAutomaticAttackPolicy.Decide(Ready(), out refusal) == MountedAutomaticProposalDecision.Admit,
                "An idle rider with AI on did not get its next target.");
            TestRunner.True(refusal == null, "An admitted proposal carried a refusal.");
        }

        private static void RepeatedProposalContinues()
        {
            var facts = Ready();
            facts.AutomaticIntentLive = true;
            facts.AutomaticIntentOnThisTarget = true;
            string refusal;
            TestRunner.True(MountedAutomaticAttackPolicy.Decide(facts, out refusal) == MountedAutomaticProposalDecision.ContinueOwnIntent,
                "A repeated observer proposal was not the same intent.");
            // Its own running command does not refuse it, and nothing it does can start a second command.
            facts.PairCommandActive = true;
            TestRunner.True(MountedAutomaticAttackPolicy.Decide(facts, out refusal) == MountedAutomaticProposalDecision.ContinueOwnIntent,
                "The intent's running command refused the intent's own proposal.");
        }

        private static void OtherTargetRefused()
        {
            var facts = Ready();
            facts.AutomaticIntentLive = true;
            TestRunner.Equal("automatic-intent-holds-its-target", Refusal(facts), "A brain re-score displaced a live automatic target.");
        }

        private static void HigherPriorityNeverDisplaced()
        {
            var cases = new Dictionary<string, Func<MountedAutomaticProposalFacts, MountedAutomaticProposalFacts>>
            {
                { "player-intent-live", f => { f.PlayerIntentLive = true; return f; } },
                { "armed-action", f => { f.ArmedAction = true; return f; } },
                { "pair-command-active", f => { f.PairCommandActive = true; return f; } },
                { "mount-operation-live", f => { f.MountOperationLive = true; return f; } },
                { "player-operation-live", f => { f.NewerPlayerOperation = true; return f; } }
            };
            foreach (var entry in cases)
                TestRunner.Equal(entry.Key, Refusal(entry.Value(Ready())), entry.Key + " did not take precedence.");
        }

        private static void OriginAndModeBoundaries()
        {
            var facts = Ready();
            facts.AiOrigin = false;
            TestRunner.Equal("not-native-ai-origin", Refusal(facts), "A command without brain origin was admitted.");
            facts = Ready();
            facts.TurnBased = true;
            TestRunner.Equal("turn-based", Refusal(facts), "Automatic acquisition reached turn-based combat.");
            facts = Ready();
            facts.OwnerIsRider = false;
            TestRunner.Equal("not-rider-brain", Refusal(facts), "The mount's brain got its own dispatch authority.");
            facts = Ready();
            facts.ExactMountedPair = false;
            TestRunner.Equal("no-exact-mounted-pair", Refusal(facts), "A stale relationship was admitted.");
        }

        private static void ActorBoundaries()
        {
            var facts = Ready();
            facts.RiderAiEnabled = false;
            TestRunner.Equal("ai-off", Refusal(facts), "AI off did not refuse.");
            facts = Ready();
            facts.RiderBrainActive = false;
            TestRunner.Equal("brain-inactive", Refusal(facts), "An inactive brain did not refuse.");
            facts = Ready();
            facts.RiderDirectlyControllable = false;
            TestRunner.Equal("rider-not-directly-controllable", Refusal(facts), "A rider out of direct control was admitted.");
            facts = Ready();
            facts.TargetValid = false;
            TestRunner.Equal("target-invalid", Refusal(facts), "An invalid target was admitted.");
        }

        private static void HoldIsReachOnly()
        {
            var facts = Ready();
            facts.MovementForbidden = true;
            facts.TargetInRiderReach = false;
            TestRunner.Equal("hold-out-of-reach", Refusal(facts), "Hold chased a distant target.");
            facts.TargetInRiderReach = true;
            string refusal;
            TestRunner.True(MountedAutomaticAttackPolicy.Decide(facts, out refusal) == MountedAutomaticProposalDecision.Admit,
                "Hold refused a target already in reach.");
        }

        private static MountedAutomaticSlotFacts PlayerCast() => new MountedAutomaticSlotFacts();
        private static MountedAutomaticSlotFacts OwnPairCommand() => new MountedAutomaticSlotFacts { PairOwnedOrReaction = true };
        private static MountedAutomaticSlotFacts BrainCommand() => new MountedAutomaticSlotFacts { AiOrigin = true };
        private static MountedAutomaticSlotFacts FinishedCast() => new MountedAutomaticSlotFacts { Finished = true };

        private static void QueuedOrderCounts()
        {
            TestRunner.True(MountedAutomaticAttackPolicy.HasPlayerOperation(new MountedAutomaticSlotFacts[0], 1),
                "A queued order was treated as an idle rider, whose queue native Run would clear.");
        }

        private static void OwnedCommandsAreNotOperations()
        {
            TestRunner.True(!MountedAutomaticAttackPolicy.HasPlayerOperation(new[] { OwnPairCommand(), BrainCommand(), FinishedCast() }, 0),
                "A pair command, brain command or finished slot was read as a newer order.");
            TestRunner.True(MountedAutomaticAttackPolicy.HasPlayerOperation(new[] { OwnPairCommand(), PlayerCast() }, 0),
                "A live player command beside the pair's own was missed.");
        }

        private static void CoexistingCommandsAreNotOperations()
        {
            TestRunner.True(!MountedAutomaticAttackPolicy.HasPlayerOperation(new[] { OwnPairCommand(), new MountedAutomaticSlotFacts { CoexistsWithAttack = true } }, 0),
                "A swift self-buff or blade activation would have interrupted the rider's running routine.");
        }

        private static string Retire(bool automatic, bool ai = true, bool brain = true, bool control = true, bool stopped = false,
            bool forbidden = false, bool reach = true, bool operation = false, bool realTime = true)
        {
            return MountedAutomaticAttackPolicy.RetireReason(realTime, automatic, control, ai, brain, stopped, forbidden, reach, operation);
        }

        private static void NewerOperationRetires()
        {
            TestRunner.Equal("newer player operation", Retire(true, operation: true), "An automatic intent kept repeating over a cast.");
            TestRunner.Equal("newer player operation", Retire(false, operation: true), "A click-owned intent kept repeating over a newer cast.");
            TestRunner.True(Retire(true) == null, "A clean automatic intent retired.");
        }

        private static void ClickOwnedIntentKeepsItsBoundaries()
        {
            TestRunner.True(Retire(false, ai: false, stopped: true, forbidden: true, reach: false) == null,
                "AI off, stop state or hold retired an intent the player clicked.");
            TestRunner.True(Retire(false, operation: true, realTime: false) == null, "A turn-based click-owned intent changed behavior.");
            TestRunner.Equal("automatic intent left real time", Retire(true, realTime: false), "An automatic intent survived into turn-based combat.");
            TestRunner.Equal("ai off", Retire(true, ai: false), "AI off did not end an automatic intent.");
            TestRunner.Equal("stop", Retire(true, stopped: true), "Stop did not end an automatic intent.");
            TestRunner.Equal("hold out of reach", Retire(true, forbidden: true, reach: false), "Hold kept a chase alive.");
        }

        private static void StopHoldsUntilOrder()
        {
            var gate = new MountedAutomaticAttackGate();
            gate.ObserveStop(4);
            for (var tick = 0; tick < 5; tick++) gate.Tick(4, true, true, false, true);
            TestRunner.True(gate.IsSuppressed(4), "Stop allowed a next-tick restart.");
            gate.ObservePlayerOrder();
            TestRunner.True(!gate.IsSuppressed(4), "A deliberate order did not rearm the pair.");
        }

        private static void AiToggleRearmsStop()
        {
            var gate = new MountedAutomaticAttackGate();
            gate.ObserveStop(4);
            gate.Tick(4, true, true, false, true);
            TestRunner.True(gate.IsSuppressed(4), "AI staying on rearmed a stop.");
            gate.Tick(4, true, true, false, false);
            TestRunner.True(gate.IsSuppressed(4), "AI off alone rearmed a stop.");
            gate.Tick(4, true, true, false, true);
            TestRunner.True(!gate.IsSuppressed(4), "An explicit AI re-enable did not rearm a stop.");
        }

        private static void StopClearsOnBoundaries()
        {
            foreach (var boundary in new Action<MountedAutomaticAttackGate>[]
            {
                g => g.Tick(4, false, true, false, true),
                g => g.Tick(4, true, false, false, true),
                g => g.Tick(4, true, true, true, true),
                g => g.Tick(5, true, true, false, true)
            })
            {
                var gate = new MountedAutomaticAttackGate();
                gate.ObserveStop(4);
                gate.ObservePlayerMove();
                gate.ObserveStop(4);
                boundary(gate);
                TestRunner.True(!gate.IsSuppressed(4) && !gate.IsSuppressed(5), "A stop outlived its boundary and became a permanent AI switch.");
            }
            var stale = new MountedAutomaticAttackGate();
            stale.ObserveStop(4);
            TestRunner.True(!stale.IsSuppressed(5), "A stale generation inherited a stop.");
        }

        private static void PairsStaySeparate()
        {
            var first = new MountedAutomaticAttackGate();
            var second = new MountedAutomaticAttackGate();
            first.ObserveStop(1);
            TestRunner.True(first.IsSuppressed(1) && !second.IsSuppressed(1), "One pair's stop reached another pair.");
            var firstIntent = new MountedAttackIntent<Unit, Turn>();
            var secondIntent = new MountedAttackIntent<Unit, Turn>();
            var target = new Unit();
            firstIntent.Begin(target, null, false, true, automatic: true);
            TestRunner.True(firstIntent.Automatic && !secondIntent.Automatic && secondIntent.Target == null,
                "One pair's automatic target reached another pair.");
        }

        private static void PlayerMoveHoldsUntilOrder()
        {
            var gate = new MountedAutomaticAttackGate();
            gate.ObservePlayerMove();
            gate.Tick(1, true, true, false, true);
            TestRunner.True(gate.PlayerMoveHold, "A player move did not hold like the native unit's own move.");
            gate.ObservePlayerOrder();
            TestRunner.True(!gate.PlayerMoveHold, "A new order did not release the move hold.");
            gate.ObservePlayerMove();
            gate.Tick(1, true, false, false, true);
            TestRunner.True(!gate.PlayerMoveHold, "An exploration move held the pair into the next combat.");
        }

        private static void ClickPromotesAutomaticIntent()
        {
            var intent = new MountedAttackIntent<Unit, Turn>();
            var target = new Unit();
            intent.Begin(target, null, false, true, automatic: true);
            var generation = intent.Generation;
            intent.PromoteToPlayer();
            TestRunner.True(!intent.Automatic && intent.Owns(target, generation), "The click did not keep and own the same running intent.");
        }

        private static void CancelClearsOrigin()
        {
            var intent = new MountedAttackIntent<Unit, Turn>();
            intent.Begin(new Unit(), null, false, true, automatic: true);
            intent.Cancel();
            TestRunner.True(!intent.Automatic && intent.Target == null, "Cancellation left automatic origin behind.");
            intent.Begin(new Unit(), null, false, true);
            TestRunner.True(!intent.Automatic, "A click-owned intent inherited automatic origin.");
        }

        // The automatic path dispatches through the same production decision a click does.
        private static void RangedAutomaticKeepsMountOut()
        {
            var decision = UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, false, true, true, false, preferMount: true);
            TestRunner.True(decision == MountedStockAttackDecision.Wait, "A ranged or held intent dragged the mount into melee.");
            decision = UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, false, true, true, true, preferMount: true);
            TestRunner.True(decision == MountedStockAttackDecision.DispatchMount, "A mount already in reach lost its legitimate attack.");
        }

        private static void NewTargetGrantsNoReadiness()
        {
            var decision = UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, false, false, false, false, true);
            TestRunner.True(decision == MountedStockAttackDecision.Wait, "A fresh target bought a routine neither actor can afford.");
            decision = UnifiedMountedStockAttackPolicy.DecideNext(true, true, true, false, true, true, false, true);
            TestRunner.True(decision == MountedStockAttackDecision.Wait, "A second command overlapped a running one.");
        }
    }
}
