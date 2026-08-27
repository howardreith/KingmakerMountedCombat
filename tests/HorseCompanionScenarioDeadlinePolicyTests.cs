using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class HorseCompanionScenarioDeadlinePolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("horse companion lifecycle deadline is independently bounded", LifecycleDeadlineIsIndependentAndBounded);
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
