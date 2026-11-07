using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class MountedRiderFallTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("rider fall: death, collapse and new prone of the exact ridden mount each begin a fall", CausesBeginAFall);
            runner.Run("rider fall: only a life transition out of Conscious is a fall cause", LifeTransitions);
            runner.Run("rider fall: rider-only, voluntary and lifecycle cleanup begin nothing", Exclusions);
            runner.Run("rider fall: a dead rider gets no soft-fall operation", DeadRider);
            runner.Run("rider fall: loading or restoring a saved state replays nothing", NoReplayOnLoad);
            runner.Run("rider fall: DC 15 is met at 15 and missed at 14", Threshold);
            runner.Run("rider fall: success lands safely, failure or inability lands hard", Outcomes);
            runner.Run("rider fall: an incapacitated rider makes no check and says why", Inability);
            runner.Run("rider fall: prone goes through the native knock-off rule", Prone);
            runner.Run("rider fall: duplicate and prone-then-death callbacks resolve one fall", ExactlyOnce);
            runner.Run("rider fall: a fall resolves only once the rider is unseated", PendingUntilUnseated);
            runner.Run("rider fall: identically named pairs keep separate falls", SeparatePairs);
            runner.Run("rider fall: a remount is a new generation and may fall again", Remount);
            runner.Run("rider fall: a seat that moved on is abandoned with its reason", Abandoned);
            runner.Run("rider fall: the log names mount, rider, check and outcome", LogText);
        }

        private sealed class Pair { internal string Name; internal long Generation; internal bool Unseated; }

        private static MountedRiderFallFacts Facts(MountedRiderFallCause cause, bool exact = true, bool riderAlive = true, bool gameplay = true) =>
            new MountedRiderFallFacts { Cause = cause, ExactMountOfMountedPair = exact, RiderAlive = riderAlive, Gameplay = gameplay };

        private static MountedRiderFallRefusal Begin(MountedRiderFallLedger<Pair> ledger, Pair pair, MountedRiderFallFacts facts, out MountedRiderFallEvent<Pair> fall) =>
            ledger.Begin(pair, pair.Generation, facts, "rider-" + pair.Name, "mount-" + pair.Name, out fall);

        private static void CausesBeginAFall()
        {
            foreach (var cause in new[] { MountedRiderFallCause.MountDied, MountedRiderFallCause.MountCollapsed, MountedRiderFallCause.MountFellProne })
            {
                var ledger = new MountedRiderFallLedger<Pair>();
                MountedRiderFallEvent<Pair> fall;
                TestRunner.True(Begin(ledger, new Pair { Name = "a", Generation = 1 }, Facts(cause), out fall) == MountedRiderFallRefusal.None && fall != null &&
                    fall.Cause == cause && fall.RiderId == "rider-a" && fall.MountId == "mount-a", cause + " begins one fall for the exact rider.");
            }
        }

        private static void LifeTransitions()
        {
            TestRunner.True(MountedRiderFallPolicy.CauseOfLifeTransition("Conscious", "Dead") == MountedRiderFallCause.MountDied, "Conscious to Dead is a death.");
            TestRunner.True(MountedRiderFallPolicy.CauseOfLifeTransition("Conscious", "Unconscious") == MountedRiderFallCause.MountCollapsed, "Conscious to Unconscious is a collapse.");
            TestRunner.True(MountedRiderFallPolicy.CauseOfLifeTransition("Unconscious", "Dead") == MountedRiderFallCause.None, "An already collapsed mount dying is not a second fall.");
            TestRunner.True(MountedRiderFallPolicy.CauseOfLifeTransition("Unconscious", "Conscious") == MountedRiderFallCause.None, "Recovering is not a fall.");
        }

        private static void Exclusions()
        {
            var ledger = new MountedRiderFallLedger<Pair>();
            var pair = new Pair { Name = "a", Generation = 1 };
            MountedRiderFallEvent<Pair> fall;
            TestRunner.True(Begin(ledger, pair, Facts(MountedRiderFallCause.None), out fall) == MountedRiderFallRefusal.NotAFallCause && fall == null,
                "Voluntary dismount, party removal, area, view, disable and stale-pair cleanup carry no fall cause.");
            TestRunner.True(Begin(ledger, pair, Facts(MountedRiderFallCause.MountDied, exact: false), out fall) == MountedRiderFallRefusal.NotTheMountOfAMountedPair && fall == null,
                "The rider's own death or an unmounted pet is not the exact ridden mount.");
            TestRunner.True(ledger.BegunCount == 0 && ledger.PendingCount == 0, "Nothing was begun.");
        }

        private static void DeadRider()
        {
            MountedRiderFallEvent<Pair> fall;
            TestRunner.True(Begin(new MountedRiderFallLedger<Pair>(), new Pair { Name = "a" }, Facts(MountedRiderFallCause.MountDied, riderAlive: false), out fall) ==
                MountedRiderFallRefusal.RiderDead && fall == null, "A dead rider is not resurrected or rolled for.");
        }

        private static void NoReplayOnLoad()
        {
            var ledger = new MountedRiderFallLedger<Pair>();
            MountedRiderFallEvent<Pair> fall;
            TestRunner.True(Begin(ledger, new Pair { Name = "a" }, Facts(MountedRiderFallCause.MountFellProne, gameplay: false), out fall) == MountedRiderFallRefusal.NotGameplay,
                "A prone or dead mount seen while a save restores is not a new collapse.");
            TestRunner.True(Begin(ledger, new Pair { Name = "a" }, Facts(MountedRiderFallCause.MountDied, exact: false), out fall) == MountedRiderFallRefusal.NotTheMountOfAMountedPair,
                "A resolved fall saved unmounted has no mounted pair to fall from after load.");
        }

        private static void Threshold()
        {
            TestRunner.True(MountedRiderFallPolicy.DifficultyClass == 15, "D5 selects DC 15.");
            TestRunner.True(MountedRiderFallPolicy.Passes(15, 15) && MountedRiderFallPolicy.Passes(16, 15) && !MountedRiderFallPolicy.Passes(14, 15),
                "Meeting the DC succeeds, as the native skill check compares; this is not the reaction's strict comparison.");
            TestRunner.True(MountedRiderFallPolicy.Passes(15, 17) == false, "A native difficulty adjustment to the DC is respected.");
        }

        private static void Outcomes()
        {
            TestRunner.True(MountedRiderFallPolicy.Outcome(MountedRiderFallInability.None, true) == MountedRiderFallOutcome.SafeLanding, "A passed check lands safely.");
            TestRunner.True(MountedRiderFallPolicy.Outcome(MountedRiderFallInability.None, false) == MountedRiderFallOutcome.HardLanding, "A failed check lands hard.");
            TestRunner.True(MountedRiderFallPolicy.Outcome(MountedRiderFallInability.Stunned, true) == MountedRiderFallOutcome.HardLanding, "An unable rider cannot land safely.");
            TestRunner.True(!MountedRiderFallPolicy.DealsDamage(MountedRiderFallOutcome.SafeLanding) && MountedRiderFallPolicy.DealsDamage(MountedRiderFallOutcome.HardLanding),
                "Only a hard landing takes the 1d6 fall.");
            TestRunner.True(MountedRiderFallPolicy.DamageDice == 1 && MountedRiderFallPolicy.DamageDieSides == 6, "The fall is one d6.");
        }

        private static void Inability()
        {
            var able = new MountedRiderConditionFacts { Conscious = true };
            TestRunner.True(MountedRiderFallPolicy.Inability(able) == MountedRiderFallInability.None, "A conscious, able rider checks.");
            TestRunner.True(MountedRiderFallPolicy.Inability(new MountedRiderConditionFacts()) == MountedRiderFallInability.Unconscious, "Unconscious.");
            TestRunner.True(MountedRiderFallPolicy.Inability(new MountedRiderConditionFacts { Conscious = true, Helpless = true }) == MountedRiderFallInability.Helpless, "Helpless.");
            TestRunner.True(MountedRiderFallPolicy.Inability(new MountedRiderConditionFacts { Conscious = true, Stunned = true }) == MountedRiderFallInability.Stunned, "Stunned.");
            TestRunner.True(MountedRiderFallPolicy.Inability(new MountedRiderConditionFacts { Conscious = true, Dazed = true }) == MountedRiderFallInability.Dazed, "Dazed.");
            TestRunner.True(MountedRiderFallPolicy.Inability(new MountedRiderConditionFacts { Conscious = true, CantAct = true }) == MountedRiderFallInability.CannotAct, "Cannot act.");
            TestRunner.True(MountedRiderFallPolicy.Describe(MountedRiderFallInability.Stunned) == "stunned" && MountedRiderFallPolicy.Describe(MountedRiderFallInability.None) == null,
                "The reason is readable.");
        }

        private static void Prone()
        {
            TestRunner.True(MountedRiderFallPolicy.ImposesProne(MountedRiderFallOutcome.HardLanding, true), "A hard landing knocks an able rider prone.");
            TestRunner.True(!MountedRiderFallPolicy.ImposesProne(MountedRiderFallOutcome.HardLanding, false), "Immunity or an existing prone is respected.");
            TestRunner.True(!MountedRiderFallPolicy.ImposesProne(MountedRiderFallOutcome.SafeLanding, true), "A safe landing imposes no prone.");
        }

        private static void ExactlyOnce()
        {
            var ledger = new MountedRiderFallLedger<Pair>();
            var pair = new Pair { Name = "a", Generation = 3 };
            MountedRiderFallEvent<Pair> first, second;
            TestRunner.True(Begin(ledger, pair, Facts(MountedRiderFallCause.MountFellProne), out first) == MountedRiderFallRefusal.None, "The prone begins the fall.");
            TestRunner.True(Begin(ledger, pair, Facts(MountedRiderFallCause.MountFellProne), out second) == MountedRiderFallRefusal.AlreadyFell && second == null,
                "A duplicate prone notification replays nothing.");
            pair.Unseated = true;
            var resolved = ledger.TakeUnseated(pair, f => f.Pair.Unseated);
            TestRunner.True(resolved.Length == 1 && ReferenceEquals(resolved[0], first), "The fall resolves once.");
            TestRunner.True(ledger.TakeUnseated(pair, f => f.Pair.Unseated).Length == 0, "A later cleanup callback resolves nothing more.");
            TestRunner.True(Begin(ledger, pair, Facts(MountedRiderFallCause.MountDied), out second) == MountedRiderFallRefusal.AlreadyFell,
                "A death of the same generation after the prone (were the pair still reported mounted) is the same fall.");
            TestRunner.True(Begin(ledger, pair, Facts(MountedRiderFallCause.MountDied, exact: false), out second) == MountedRiderFallRefusal.NotTheMountOfAMountedPair,
                "Ordinarily the unseated former rider is no longer on that mount at all.");
            TestRunner.True(ledger.BegunCount == 1, "One fall in total.");
        }

        private static void PendingUntilUnseated()
        {
            var ledger = new MountedRiderFallLedger<Pair>();
            var pair = new Pair { Name = "a", Generation = 1 };
            MountedRiderFallEvent<Pair> fall;
            Begin(ledger, pair, Facts(MountedRiderFallCause.MountDied), out fall);
            TestRunner.True(ledger.TakeUnseated(pair, f => f.Pair.Unseated).Length == 0 && ledger.PendingCount == 1,
                "While cleanup still retains the seat (a charge barrier), nothing is rolled.");
            pair.Unseated = true;
            TestRunner.True(ledger.TakeUnseated(null, f => f.Pair.Unseated).Single() == fall && ledger.PendingCount == 0, "The retry that unseats the rider resolves it.");
        }

        private static void SeparatePairs()
        {
            var ledger = new MountedRiderFallLedger<Pair>();
            // Both companions carry the same display name; identity is the pair and its generation.
            var a = new Pair { Name = "Ébène l'Ancien", Generation = 1 };
            var b = new Pair { Name = "Ébène l'Ancien", Generation = 1 };
            MountedRiderFallEvent<Pair> fa = null, fb = null;
            TestRunner.True(Begin(ledger, a, Facts(MountedRiderFallCause.MountDied), out fa) == MountedRiderFallRefusal.None &&
                Begin(ledger, b, Facts(MountedRiderFallCause.MountFellProne), out fb) == MountedRiderFallRefusal.None && fa.Sequence != fb.Sequence,
                "Two pairs with identical names fall separately.");
            b.Unseated = true;
            var resolved = ledger.TakeUnseated(a, f => f.Pair.Unseated);
            TestRunner.True(resolved.Length == 0 && ledger.PendingCount == 2, "Completing one pair never resolves the other's fall.");
            TestRunner.True(ledger.TakeUnseated(b, f => f.Pair.Unseated).Single() == fb, "Each pair resolves its own.");
        }

        private static void Remount()
        {
            var ledger = new MountedRiderFallLedger<Pair>();
            var pair = new Pair { Name = "a", Generation = 1, Unseated = true };
            MountedRiderFallEvent<Pair> first, later;
            Begin(ledger, pair, Facts(MountedRiderFallCause.MountFellProne), out first);
            ledger.TakeUnseated(pair, f => true);
            pair.Generation = 2;
            TestRunner.True(Begin(ledger, pair, Facts(MountedRiderFallCause.MountDied), out later) == MountedRiderFallRefusal.None && later.Generation == 2,
                "After native recovery and a new legal mount, a later collapse is a new fall.");
        }

        private static void Abandoned()
        {
            var ledger = new MountedRiderFallLedger<Pair>();
            var pair = new Pair { Name = "a", Generation = 1 };
            MountedRiderFallEvent<Pair> fall;
            Begin(ledger, pair, Facts(MountedRiderFallCause.MountDied), out fall);
            TestRunner.True(ledger.TakeAbandoned(f => null).Length == 0 && ledger.PendingCount == 1, "A pending fall is kept while its seat is current.");
            pair.Generation = 2;
            var abandoned = ledger.TakeAbandoned(f => f.Pair.Generation != f.Generation ? "generation advanced" : null);
            TestRunner.True(abandoned.Single() == fall && fall.AbandonedReason == "generation advanced" && ledger.PendingCount == 0,
                "A fall is never resolved against a different seat.");
        }

        private static void LogText()
        {
            var safe = MountedRiderFallPolicy.LogText("Ébène l'Ancien", "Linzi", MountedRiderFallCause.MountFellProne, MountedRiderFallInability.None, 17, 15,
                MountedRiderFallOutcome.SafeLanding, null, false, null);
            TestRunner.True(safe == "Ébène l'Ancien falls prone; Linzi is thrown: Mobility 17 vs DC 15 - lands safely.", safe);
            var hard = MountedRiderFallPolicy.LogText("Mastodon", "Valerie", MountedRiderFallCause.MountDied, MountedRiderFallInability.None, 9, 15,
                MountedRiderFallOutcome.HardLanding, 4, true, null);
            TestRunner.True(hard == "Mastodon dies; Valerie is thrown: Mobility 9 vs DC 15 - falls hard, 4 damage, knocked prone.", hard);
            var unable = MountedRiderFallPolicy.LogText("Mastodon", "Valerie", MountedRiderFallCause.MountCollapsed, MountedRiderFallInability.Stunned, 0, 15,
                MountedRiderFallOutcome.HardLanding, 0, false, "immune to prone");
            TestRunner.True(unable == "Mastodon collapses; Valerie is thrown and cannot attempt a soft landing (stunned) - falls hard, 0 damage, not knocked prone (immune to prone).", unable);
        }
    }
}
