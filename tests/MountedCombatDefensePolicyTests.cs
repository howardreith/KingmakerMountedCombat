using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedCombatDefensePolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("mounted combat check must strictly exceed the attack result", StrictComparison);
            runner.Run("mounted combat refuses what is not an eligible attempt", Refusals);
            runner.Run("mounted combat own-turn use spends that turn's Swift", OwnTurn);
            runner.Run("mounted combat off-turn use borrows even after a spent Swift", OffTurnBorrows);
            runner.Run("mounted combat second use in the round is refused until renewal", OncePerRound);
            runner.Run("mounted combat debt is charged once at the next preparation and then renews", DebtAndRenewal);
            runner.Run("mounted combat real time spends one window", RealTime);
            runner.Run("mounted combat mode changes refund nothing", ModeChanges);
        }

        private static MountedCombatDefenseFacts Eligible(bool turnBased, bool riderTurn, bool swift) => new MountedCombatDefenseFacts
        {
            WouldHit = true, HasNumericAttackResult = true, OwnsFeat = true, PreferenceOn = true, RiderCanAct = true,
            TurnBased = turnBased, RiderTurn = riderTurn, SwiftAvailable = swift
        };

        private static void StrictComparison()
        {
            TestRunner.True(MountedCombatDefensePolicy.Negates(26, 25), "A greater check negates.");
            TestRunner.True(!MountedCombatDefensePolicy.Negates(25, 25), "Equality fails.");
            TestRunner.True(!MountedCombatDefensePolicy.Negates(24, 25), "A lower check fails.");
        }

        private static void Refusals()
        {
            Refused(f => f.WouldHit = false, MountedCombatDefenseRefusal.NotAHit, "A miss is unaffected.");
            Refused(f => f.HasNumericAttackResult = false, MountedCombatDefenseRefusal.NoNumericAttackResult, "An automatic hit has no result to exceed.");
            Refused(f => f.OwnsFeat = false, MountedCombatDefenseRefusal.NoFeat, "A rider without the feat is unaffected.");
            Refused(f => f.PreferenceOn = false, MountedCombatDefenseRefusal.PreferenceOff, "The preference holds the reaction back.");
            Refused(f => f.RiderCanAct = false, MountedCombatDefenseRefusal.RiderCannotAct, "An incapacitated rider cannot react.");
            Refused(f => f.RiderFlatFooted = true, MountedCombatDefenseRefusal.RiderFlatFooted, "A flat-footed rider cannot react.");
        }

        private static void Refused(System.Action<Box> change, MountedCombatDefenseRefusal expected, string message)
        {
            var box = new Box { Value = Eligible(true, false, true) };
            change(box);
            MountedCombatDefenseCommitment commitment;
            TestRunner.True(MountedCombatDefensePolicy.Evaluate(box.Value, out commitment) == expected && commitment == MountedCombatDefenseCommitment.None, message);
        }

        private sealed class Box
        {
            internal MountedCombatDefenseFacts Value;
            internal bool WouldHit { set { Value.WouldHit = value; } }
            internal bool HasNumericAttackResult { set { Value.HasNumericAttackResult = value; } }
            internal bool OwnsFeat { set { Value.OwnsFeat = value; } }
            internal bool PreferenceOn { set { Value.PreferenceOn = value; } }
            internal bool RiderCanAct { set { Value.RiderCanAct = value; } }
            internal bool RiderFlatFooted { set { Value.RiderFlatFooted = value; } }
        }

        private static void OwnTurn()
        {
            MountedCombatDefenseCommitment commitment;
            TestRunner.True(MountedCombatDefensePolicy.Evaluate(Eligible(true, true, true), out commitment) == MountedCombatDefenseRefusal.None && commitment == MountedCombatDefenseCommitment.SpendCurrentSwift, "The own-turn use is the current Swift.");
            var after = MountedCombatDefensePolicy.Commit(default(MountedCombatDefenseState), commitment);
            TestRunner.True(after.ChargeSwift && after.State.Spent && !after.State.Owed, "It charges now and leaves no debt.");
            TestRunner.True(MountedCombatDefensePolicy.Evaluate(Eligible(true, true, false), out commitment) == MountedCombatDefenseRefusal.SwiftUnavailable, "A turn without its Swift cannot react.");
        }

        private static void OffTurnBorrows()
        {
            MountedCombatDefenseCommitment commitment;
            TestRunner.True(MountedCombatDefensePolicy.Evaluate(Eligible(true, false, false), out commitment) == MountedCombatDefenseRefusal.None && commitment == MountedCombatDefenseCommitment.BorrowNextSwift, "A Swift spent last turn is not debt against the next.");
            var after = MountedCombatDefensePolicy.Commit(default(MountedCombatDefenseState), commitment);
            TestRunner.True(!after.ChargeSwift && after.State.Spent && after.State.Owed, "Borrowing records the debt and charges nothing yet.");
        }

        private static void OncePerRound()
        {
            MountedCombatDefenseCommitment commitment;
            var facts = Eligible(true, false, true);
            facts.State = MountedCombatDefensePolicy.Commit(default(MountedCombatDefenseState), MountedCombatDefenseCommitment.BorrowNextSwift).State;
            TestRunner.True(MountedCombatDefensePolicy.Evaluate(facts, out commitment) == MountedCombatDefenseRefusal.UsageSpent, "A later attack in the round cannot reuse it.");
            facts.State = new MountedCombatDefenseState { Owed = true };
            TestRunner.True(MountedCombatDefensePolicy.Evaluate(facts, out commitment) == MountedCombatDefenseRefusal.SwiftOwed, "An unpaid debt blocks a further immediate action.");
        }

        private static void DebtAndRenewal()
        {
            var borrowed = MountedCombatDefensePolicy.Commit(default(MountedCombatDefenseState), MountedCombatDefenseCommitment.BorrowNextSwift).State;
            var prepared = MountedCombatDefensePolicy.RiderTurnPrepared(borrowed);
            TestRunner.True(prepared.ChargeSwift && !prepared.State.Any, "The next preparation pays the debt and renews the use.");
            var again = MountedCombatDefensePolicy.RiderTurnPrepared(prepared.State);
            TestRunner.True(!again.ChargeSwift && !again.State.Any, "The debt is charged exactly once.");
            MountedCombatDefenseCommitment commitment;
            var onTurn = Eligible(true, true, false); onTurn.State = prepared.State;
            TestRunner.True(MountedCombatDefensePolicy.Evaluate(onTurn, out commitment) == MountedCombatDefenseRefusal.SwiftUnavailable, "The paying turn has no Swift to react with.");
            var offTurn = Eligible(true, false, false); offTurn.State = prepared.State;
            TestRunner.True(MountedCombatDefensePolicy.Evaluate(offTurn, out commitment) == MountedCombatDefenseRefusal.None && commitment == MountedCombatDefenseCommitment.BorrowNextSwift, "After the paying turn the rider may borrow again.");
            var own = MountedCombatDefensePolicy.Commit(default(MountedCombatDefenseState), MountedCombatDefenseCommitment.SpendCurrentSwift).State;
            TestRunner.True(!MountedCombatDefensePolicy.RiderTurnPrepared(own).ChargeSwift, "An own-turn use owes nothing at the next preparation.");
        }

        private static void RealTime()
        {
            MountedCombatDefenseCommitment commitment;
            TestRunner.True(MountedCombatDefensePolicy.Evaluate(Eligible(false, false, true), out commitment) == MountedCombatDefenseRefusal.None && commitment == MountedCombatDefenseCommitment.RealTimeWindow, "Real time has no off-turn borrowing.");
            var after = MountedCombatDefensePolicy.Commit(default(MountedCombatDefenseState), commitment);
            TestRunner.True(after.ChargeSwift && after.State.Window && after.State.UsageSpent, "It charges the native cooldown and holds the use.");
            TestRunner.True(MountedCombatDefensePolicy.Evaluate(Eligible(false, false, false), out commitment) == MountedCombatDefenseRefusal.SwiftUnavailable, "A running Swift cooldown refuses.");
        }

        private static void ModeChanges()
        {
            var borrowed = MountedCombatDefensePolicy.Commit(default(MountedCombatDefenseState), MountedCombatDefenseCommitment.BorrowNextSwift).State;
            var toRealTime = MountedCombatDefensePolicy.RealTimeObserved(borrowed);
            TestRunner.True(toRealTime.ChargeSwift && toRealTime.State.Window && !toRealTime.State.Owed && !toRealTime.State.Spent, "A borrowed Swift carried into real time is charged, and the use runs a window.");
            var own = MountedCombatDefensePolicy.Commit(default(MountedCombatDefenseState), MountedCombatDefenseCommitment.SpendCurrentSwift).State;
            var ownToRealTime = MountedCombatDefensePolicy.RealTimeObserved(own);
            TestRunner.True(!ownToRealTime.ChargeSwift && ownToRealTime.State.Window, "An own-turn use keeps its use across the change and owes nothing.");
            var window = new MountedCombatDefenseState { Window = true };
            TestRunner.True(!MountedCombatDefensePolicy.RealTimeObserved(window).ChargeSwift && MountedCombatDefensePolicy.RealTimeObserved(window).State.Window, "A running window is left to its native duration.");
            TestRunner.True(MountedCombatDefensePolicy.RiderTurnPrepared(window).ChargeSwift, "A window carried into turn-based combat spends the first turn's Swift.");
        }
    }
}
