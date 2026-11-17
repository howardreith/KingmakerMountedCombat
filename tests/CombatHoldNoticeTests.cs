using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues C03: the feedback that pairs still staged when an encounter began stay dismounted through it.
    //
    // Production code executed: MountedRetainedIntentPolicy.CombatHoldNotice and Decide. Modelled: the restore loop's
    // sequence of evaluations (one per frame) and the custody objects it holds. The base latch is reproduced from
    // e5b92474 MountedRetainedIntent.cs (a bool set on the first combat Hold and cleared only by an evaluation that was
    // not a Hold) solely to show the sequence the correction changes; it is not production code.
    internal static class CombatHoldNoticeTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("known issues C03: a staged pair's encounter is reported once per custody and encounter, never per frame", OncePerCustodyAndEncounter);
            runner.Run("known issues C03 (evaluation sequence modelled): a custody dropped during combat leaves nothing behind, so a later custody's encounter is reported; the base latch did not report it", DroppedCustodyLeavesNoLatch);
        }

        private sealed class Custody { }

        private static void OncePerCustodyAndEncounter()
        {
            var notice = new MountedRetainedIntentPolicy.CombatHoldNotice();
            var custody = new Custody();
            var reports = 0;
            for (var frame = 0; frame < 50; frame++) if (notice.ShouldNotify(custody, true)) reports++;
            TestRunner.Equal(1, reports, "An encounter was reported more than once, or not at all.");
            // The encounter ends (an evaluation outside combat); a second encounter of the same custody is reported again.
            notice.ShouldNotify(custody, false);
            for (var frame = 0; frame < 50; frame++) if (notice.ShouldNotify(custody, true)) reports++;
            TestRunner.Equal(2, reports, "A second encounter of the same custody was not reported once.");
            TestRunner.True(!notice.ShouldNotify(null, true), "An absent custody was reported.");
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
            foreach (var step in sequence) if (notice.ShouldNotify(step.Item1, step.Item2)) corrected.Add(step.Item1);
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
            for (var i = 0; i < 4; i++) if (direct.ShouldNotify(c, true)) directReports.Add(c);
            for (var i = 0; i < 4; i++) if (direct.ShouldNotify(d, true)) directReports.Add(d);
            TestRunner.True(directReports.Count == 2 && directReports[0] == c && directReports[1] == d,
                "A new custody held in combat right after another was not reported exactly once.");
        }
    }
}
