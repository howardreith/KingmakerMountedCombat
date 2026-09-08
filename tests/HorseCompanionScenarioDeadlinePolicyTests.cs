using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class HorseCompanionScenarioDeadlinePolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("horse companion lifecycle deadline is independently bounded", LifecycleDeadlineIsIndependentAndBounded);
            runner.Run("allocation host leaves cleanup time while retaining scenario and leaf bounds", AllocationHostDeadline);
        }

        private static void AllocationHostDeadline()
        {
            var normal = HorseCompanionScenarioDeadlinePolicy.HostDeadlineSeconds(false);
            var allocation = HorseCompanionScenarioDeadlinePolicy.HostDeadlineSeconds(true);
            TestRunner.Equal(HorseCompanionDeadlineKind.Scenario,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(301, normal, false, -1, 30), "ordinary host retains its bound");
            TestRunner.Equal(HorseCompanionDeadlineKind.None,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(661, allocation, false, -1, 30), "allocation host permits cleanup after engine deadline");
            TestRunner.Equal(HorseCompanionDeadlineKind.Scenario,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(661, HorseCompanionScenarioDeadlinePolicy.AllocationScenarioSeconds, false, -1, 30),
                "gameplay aggregate deadline remains bounded");
            TestRunner.Equal(HorseCompanionDeadlineKind.Scenario,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(721, allocation, false, -1, 30), "host cleanup allowance also expires");
            TestRunner.Equal(HorseCompanionDeadlineKind.Lifecycle,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(661, allocation, true, 630, 30), "longer host cannot extend a leaf deadline");
        }

        private static void LifecycleDeadlineIsIndependentAndBounded()
        {
            TestRunner.Equal(
                HorseCompanionDeadlineKind.None,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(180.0, 180.0, false, -1.0, 30.0),
                "The exact aggregate deadline boundary was rejected.");
            TestRunner.Equal(
                HorseCompanionDeadlineKind.Scenario,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(180.001, 180.0, false, -1.0, 30.0),
                "A pre-lifecycle aggregate overrun was not rejected.");
            TestRunner.Equal(
                HorseCompanionDeadlineKind.None,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(180.001, 180.0, true, 179.999, 30.0),
                "The lifecycle probe incorrectly inherited an exhausted aggregate budget.");
            TestRunner.Equal(
                HorseCompanionDeadlineKind.None,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(210.0, 180.0, true, 180.0, 30.0),
                "The exact lifecycle deadline boundary was rejected.");
            TestRunner.Equal(
                HorseCompanionDeadlineKind.Lifecycle,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(210.001, 180.0, true, 180.0, 30.0),
                "A lifecycle overrun was not rejected.");
            TestRunner.Equal(
                HorseCompanionDeadlineKind.Lifecycle,
                HorseCompanionScenarioDeadlinePolicy.Evaluate(181.0, 180.0, true, -1.0, 30.0),
                "An unarmed lifecycle phase did not fail closed.");
        }
    }
}
