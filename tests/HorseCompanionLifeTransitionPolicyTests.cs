using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class HorseCompanionLifeTransitionPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("horse lifecycle accepts exact unconscious transition", AcceptsExactUnconsciousTransition);
            runner.Run("horse lifecycle accepts exact dead transition", AcceptsExactDeadTransition);
            runner.Run("horse lifecycle rejects recovered poll without transition", RejectsRecoveredPollWithoutTransition);
            runner.Run("horse lifecycle rejects another actor", RejectsAnotherActor);
        }

        private static void AcceptsExactUnconsciousTransition()
        {
            TestRunner.True(
                HorseCompanionLifeTransitionPolicy.IsExpectedNonConsciousTransition(
                    "horse-id", "horse-id", "Conscious", "Unconscious"),
                "The exact Horse Conscious-to-Unconscious event was rejected.");
        }

        private static void AcceptsExactDeadTransition()
        {
            TestRunner.True(
                HorseCompanionLifeTransitionPolicy.IsExpectedNonConsciousTransition(
                    "horse-id", "horse-id", "Conscious", "Dead"),
                "The exact Horse Conscious-to-Dead event was rejected.");
        }

        private static void RejectsRecoveredPollWithoutTransition()
        {
            TestRunner.Equal(
                false,
                HorseCompanionLifeTransitionPolicy.IsExpectedNonConsciousTransition(
                    "horse-id", "horse-id", "Conscious", "Conscious"),
                "A later conscious poll was treated as native incapacitation evidence.");
        }

        private static void RejectsAnotherActor()
        {
            TestRunner.Equal(
                false,
                HorseCompanionLifeTransitionPolicy.IsExpectedNonConsciousTransition(
                    "horse-id", "owner-id", "Conscious", "Dead"),
                "Another actor's life-state event was credited to the Horse.");
        }
    }
}
