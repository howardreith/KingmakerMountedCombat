using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // The planned-total custody the Mounted Shield driver uses (supplemental review PN10-F2). Not native evidence: the driver
    // binds the same class to the production defense service's scenario callback.
    internal static class PlannedAttemptTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("planned attempt: consumed once by the measured pair's attempt; a later hit during the renewal wait rolls naturally", ConsumedOnce);
            runner.Run("planned attempt: another pair's or attacker's hit neither receives nor consumes the plan", OtherPairIgnored);
            runner.Run("planned attempt: a hit delivered right after arming (before the next driver tick) receives the plan; clearing disarms", ArmedImmediately);
            runner.Run("step completion: a planned step completes on the consuming attempt, not on a follow-up or an earlier refusal (review PN11-F3)", PlannedCompletion);
        }

        private static void ConsumedOnce()
        {
            var plan = new PlannedAttempt();
            plan.Arm("equal", 0, "rider", "mount", "hostile");
            TestRunner.True(plan.Consume("rider", "mount", "hostile", 25, 101) == 25 && plan.ConsumedBy == 101 && !plan.Armed, "The first qualifying attempt gets the equal total and disarms the plan.");
            TestRunner.True(plan.Consume("rider", "mount", "hostile", 24, 102) == null && plan.ConsumedBy == 101, "A second hit while the use renews rolls naturally.");
        }

        private static void OtherPairIgnored()
        {
            var plan = new PlannedAttempt();
            plan.Arm("success", 1, "rider", "mount", "hostile");
            TestRunner.True(plan.Consume("second rider", "second mount", "hostile", 25, 201) == null && plan.Armed, "Another pair's hit rolls naturally and leaves the plan armed.");
            TestRunner.True(plan.Consume("rider", "mount", "other hostile", 25, 202) == null && plan.Armed, "Another attacker's hit rolls naturally and leaves the plan armed.");
            TestRunner.True(plan.Consume("rider", "mount", "hostile", 25, 203) == 26 && plan.ConsumedBy == 203, "The measured attempt still gets the success total.");
        }

        private static void PlannedCompletion()
        {
            var plan = new PlannedAttempt();
            plan.Arm("success", 1, "rider", "mount", "hostile");
            // Both records already present at the tick: the planned success (consumed) and a UsageSpent follow-up.
            plan.Consume("rider", "mount", "hostile", 25, 401);
            TestRunner.True(PlannedAttempt.CompletingIndex(new[] { 401, 402 }, new[] { true, true }, true, plan.ConsumedBy) == 0, "The consuming success completes the step, not the newer follow-up.");
            // A refusal (another pair's or the measured pair's) arrives before the qualifying attack: no completion yet.
            var waiting = new PlannedAttempt();
            waiting.Arm("equal", 0, "rider", "mount", "hostile");
            TestRunner.True(PlannedAttempt.CompletingIndex(new[] { 501 }, new[] { true }, true, waiting.ConsumedBy) == -1, "A refusal before the qualifying attack does not complete the step.");
            waiting.Consume("rider", "mount", "hostile", 25, 503);
            TestRunner.True(PlannedAttempt.CompletingIndex(new[] { 501, 502, 503 }, new[] { true, false, true }, true, waiting.ConsumedBy) == 2, "The step completes on the consuming attack once it arrives.");
            TestRunner.True(PlannedAttempt.CompletingIndex(new[] { 601, 602 }, new[] { false, true }, false, null) == 1, "A natural step completes on the first record of the measured pair.");
        }

        private static void ArmedImmediately()
        {
            var plan = new PlannedAttempt();
            plan.Arm("success", 1, "rider", "mount", "hostile");
            TestRunner.True(plan.Armed && plan.Consume("rider", "mount", "hostile", 20, 301) == 21, "A hit in the same frame as the arming receives the plan.");
            plan.Arm("equal", 0, "rider", "mount", "hostile");
            plan.Clear();
            TestRunner.True(!plan.Armed && plan.Consume("rider", "mount", "hostile", 20, 302) == null, "A cleared plan applies to nothing (step exit, drain).");
        }
    }
}
