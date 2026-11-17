using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues C03: the notice that pairs still staged when an encounter began stay dismounted through it.
    //
    // Production code executed: MountedRetainedIntentPolicy.CombatHoldNotice (owed, attempted, spent) and Decide.
    // Represented: the receiver. A receiver present is "attempt, then Spend"; no receiver is "attempt, not spent", as the
    // notifier (MountedCombatHoldNotifier, Integration) does; its delivery through the installed native EventBus is
    // executed by Test-ScenePlacementAttributionContracts.ps1. The base latch is reproduced from e5b92474
    // MountedRetainedIntent.cs only to show the sequence the first C03 repair changed; it is not production code.
    internal static class CombatHoldNoticeTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("known issues C03: a staged pair's encounter is reported once per custody and encounter, never per frame", OncePerCustodyAndEncounter);
            runner.Run("known issues C03 (evaluation sequence modelled): a custody dropped during combat leaves nothing behind, so a later custody's encounter is reported; the base latch did not report it", DroppedCustodyLeavesNoLatch);
            runner.Run("C03 review R4: a notice is owed until a receiver takes it; a load, or an attempt no receiver heard, spends nothing; once spent it is not attempted again in that encounter", OwedUntilReceived);
            runner.Run("C03 review R4: scene-staging custody from a save, held in combat while the load is still in progress, keeps its notice owed until delivery is eligible, then delivers once", HeldDuringLoad);
        }

        private sealed class Custody { }

        // One evaluation with a receiver present: attempt, and the receiver takes it.
        private static bool Deliver(MountedRetainedIntentPolicy.CombatHoldNotice notice, object custody, bool held, bool loading = false)
        {
            if (!notice.ShouldAttempt(custody, held, loading)) return false;
            notice.Spend(custody);
            return true;
        }

        private static void OncePerCustodyAndEncounter()
        {
            var notice = new MountedRetainedIntentPolicy.CombatHoldNotice();
            var custody = new Custody();
            var reports = 0;
            for (var frame = 0; frame < 50; frame++) if (Deliver(notice, custody, true)) reports++;
            TestRunner.Equal(1, reports, "An encounter was reported more than once, or not at all.");
            // The encounter ends (an evaluation outside combat); a second encounter of the same custody is reported again.
            Deliver(notice, custody, false);
            for (var frame = 0; frame < 50; frame++) if (Deliver(notice, custody, true)) reports++;
            TestRunner.Equal(2, reports, "A second encounter of the same custody was not reported once.");
            TestRunner.True(!Deliver(notice, null, true), "An absent custody was reported.");
            // The decision that produces the hold: only scene staging is held through an encounter.
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, MountedRetainedIntentPolicy.Decide(MountedRetainedIntentKind.ScriptedEvent, true, true, false,
                "TacticalCombat", false, false, false, false), "Scene staging was not held through the encounter.");
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, MountedRetainedIntentPolicy.Decide(MountedRetainedIntentKind.Camp, true, true, false,
                "TacticalCombat", false, false, false, false), "Camp custody was held through an encounter.");
        }

        private static void DroppedCustodyLeavesNoLatch()
        {
            // Evaluations: custody A held through an encounter; A dropped during that encounter (a load, a release), so no
            // later evaluation of A; custody B staged by a later scene and held while the scene runs (outside combat); then
            // an encounter begins while B is still staged.
            var a = new Custody(); var b = new Custody();
            var sequence = new List<Tuple<object, bool>>();
            for (var i = 0; i < 5; i++) sequence.Add(Tuple.Create((object)a, true));
            for (var i = 0; i < 5; i++) sequence.Add(Tuple.Create((object)b, false));
            for (var i = 0; i < 5; i++) sequence.Add(Tuple.Create((object)b, true));

            var notice = new MountedRetainedIntentPolicy.CombatHoldNotice();
            var corrected = new List<object>();
            foreach (var step in sequence) if (Deliver(notice, step.Item1, step.Item2)) corrected.Add(step.Item1);
            TestRunner.True(corrected.Count == 2 && corrected[0] == a && corrected[1] == b, "Each custody's encounter was not reported exactly once.");

            // The base latch on the same sequence: every evaluation is a Hold (scene mode or combat), so nothing clears it.
            var latched = false; var baseReports = new List<object>();
            foreach (var step in sequence)
                if (step.Item2 && !latched) { latched = true; baseReports.Add(step.Item1); }
            TestRunner.True(baseReports.Count == 1 && baseReports[0] == a, "Setup: the base latch should have reported only custody A.");

            // Review R3: custody A disappears during the encounter and custody B is held in combat at once, with no
            // evaluation outside combat between them: B is still reported, once.
            var direct = new MountedRetainedIntentPolicy.CombatHoldNotice();
            var c = new Custody(); var d = new Custody();
            var directReports = new List<object>();
            for (var i = 0; i < 4; i++) if (Deliver(direct, c, true)) directReports.Add(c);
            for (var i = 0; i < 4; i++) if (Deliver(direct, d, true)) directReports.Add(d);
            TestRunner.True(directReports.Count == 2 && directReports[0] == c && directReports[1] == d,
                "A new custody held in combat right after another was not reported exactly once.");
        }

        private static void OwedUntilReceived()
        {
            var notice = new MountedRetainedIntentPolicy.CombatHoldNotice();
            var custody = new Custody();
            // No receiver: attempted, not spent, so the same custody is still owed and attempted again.
            for (var i = 0; i < 3; i++)
                TestRunner.True(notice.ShouldAttempt(custody, true, false) && notice.IsOwed(custody, true), "An attempt no receiver heard spent the notice.");
            // During a load: owed, not attempted.
            TestRunner.True(!notice.ShouldAttempt(custody, true, true) && notice.IsOwed(custody, true), "A notice was attempted, or spent, during a load.");
            // A receiver takes it: spent, never attempted again in this encounter.
            TestRunner.True(Deliver(notice, custody, true), "The owed notice was not delivered once a receiver existed.");
            for (var i = 0; i < 5; i++) TestRunner.True(!notice.ShouldAttempt(custody, true, false) && !notice.IsOwed(custody, true), "A spent notice was attempted again.");
            // A later encounter after the established reset, and a new custody held in combat at once, are owed again.
            notice.IsOwed(custody, false);
            TestRunner.True(notice.ShouldAttempt(custody, true, false), "A later encounter of the same custody was not owed again.");
            notice.Spend(custody);
            TestRunner.True(notice.ShouldAttempt(new Custody(), true, false), "A new custody held in combat was not owed.");
        }

        private static void HeldDuringLoad()
        {
            // The restore loop's own decision for scene-staging custody restored from a save, in combat, during the load.
            TestRunner.Equal(MountedRetainedIntentDecision.Hold, MountedRetainedIntentPolicy.Decide(MountedRetainedIntentKind.ScriptedEvent, true, true, false,
                "Default", false, true, false, false), "Scene staging in combat during a load was not held.");
            var notice = new MountedRetainedIntentPolicy.CombatHoldNotice();
            var custody = new Custody();
            var deliveries = 0;
            // Frames of the load: held in combat, loading; then the area's UI exists but the load is still finishing.
            for (var i = 0; i < 10; i++) if (Deliver(notice, custody, true, loading: true)) deliveries++;
            TestRunner.True(deliveries == 0 && notice.IsOwed(custody, true), "The notice was attempted or spent while the load was in progress.");
            // The load ended; the first evaluation finds no receiver yet (attempted, not spent), then one exists.
            TestRunner.True(notice.ShouldAttempt(custody, true, false), "The notice was not attempted once the load ended.");
            for (var i = 0; i < 10; i++) if (Deliver(notice, custody, true)) deliveries++;
            TestRunner.Equal(1, deliveries, "The same custody was not delivered exactly once after the load.");
        }
    }
}
