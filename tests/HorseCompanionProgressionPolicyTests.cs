using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class HorseCompanionProgressionPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("horse companion rank mapping matches installed AddPet", RankMappingMatchesInstalledAddPet);
            runner.Run("horse companion deferred update is exact and bounded", DeferredUpdateIsExactAndBounded);
        }

        private static void RankMappingMatchesInstalledAddPet()
        {
            TestRunner.Equal(0, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(-1), "Negative rank was not clamped.");
            TestRunner.Equal(2, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(1), "Rank 1 mapping changed.");
            TestRunner.Equal(4, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(4), "Rank 4 mapping changed.");
            TestRunner.Equal(16, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(20), "Rank 20 mapping changed.");
            TestRunner.Equal(16, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(21), "Rank above 20 was not clamped.");
            TestRunner.Equal(true, HorseCompanionProgressionPolicy.RequiresSynchronization(4, 1), "A rank-4 level-1 horse was accepted.");
            TestRunner.Equal(false, HorseCompanionProgressionPolicy.RequiresSynchronization(4, 4), "A synchronized horse requested another update.");
        }

        private static void DeferredUpdateIsExactAndBounded()
        {
            TestRunner.Equal(
                true,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, true, false, 4, 1, 0),
                "The exact one-shot deferred native update was rejected.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(false, true, false, 4, 1, 0),
                "A non-horse target was admitted.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, false, false, 4, 1, 0),
                "A non-owned horse was admitted.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, true, true, 4, 1, 0),
                "A DefaultBuildData context was admitted.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, true, false, 4, 1, 1),
                "A second native retry was admitted.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, true, false, 4, 4, 0),
                "An already synchronized horse was admitted.");
        }
    }
}
