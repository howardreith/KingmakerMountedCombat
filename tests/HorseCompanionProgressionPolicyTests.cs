using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class HorseCompanionProgressionPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("horse companion rank mapping matches installed AddPet", RankMappingMatchesInstalledAddPet);
            runner.Run("horse companion progression accepts exact class or native XP settlement", NativeProgressionSettlementIsExact);
            runner.Run("horse companion deferred update is exact and bounded", DeferredUpdateIsExactAndBounded);
        }

        private static void RankMappingMatchesInstalledAddPet()
        {
            TestRunner.Equal(0, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(-1), "Negative rank was not clamped.");
            TestRunner.Equal(2, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(1), "Rank 1 mapping changed.");
            TestRunner.Equal(4, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(4), "Rank 4 mapping changed.");
            TestRunner.Equal(16, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(20), "Rank 20 mapping changed.");
            TestRunner.Equal(16, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(21), "Rank above 20 was not clamped.");
        }

        private static void NativeProgressionSettlementIsExact()
        {
            TestRunner.Equal(
                true,
                HorseCompanionProgressionPolicy.IsNativeProgressionReady(4, 4, 0, 9000),
                "Committed rank-4 class levels were rejected.");
            TestRunner.Equal(
                true,
                HorseCompanionProgressionPolicy.IsNativeManualLevelingReady(4, 1, 9000, 9000),
                "The exact native manual-leveling XP handoff was rejected.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.IsNativeManualLevelingReady(4, 4, 9000, 9000),
                "A class-level-synchronized horse was misclassified as awaiting manual leveling.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.IsNativeManualLevelingReady(4, 1, 9001, 9000),
                "An inexact native manual-leveling XP handoff was accepted.");
            TestRunner.Equal(
                true,
                HorseCompanionProgressionPolicy.RequiresSynchronization(4, 1, 8999, 9000),
                "An under-level, under-XP horse was accepted.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.RequiresSynchronization(4, 1, 9000, 9000),
                "A native manual-leveling handoff requested a duplicate update.");
        }

        private static void DeferredUpdateIsExactAndBounded()
        {
            TestRunner.Equal(
                true,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, true, false, 4, 1, 8999, 9000, 0),
                "The exact one-shot deferred native update was rejected.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(false, true, false, 4, 1, 8999, 9000, 0),
                "A non-horse target was admitted.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, false, false, 4, 1, 8999, 9000, 0),
                "A non-owned horse was admitted.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, true, true, 4, 1, 8999, 9000, 0),
                "A DefaultBuildData context was admitted.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, true, false, 4, 1, 8999, 9000, 1),
                "A second native retry was admitted.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, true, false, 4, 4, 0, 9000, 0),
                "An already synchronized horse was admitted.");
            TestRunner.Equal(
                false,
                HorseCompanionProgressionPolicy.CanInvokeDeferredNativeUpdate(true, true, false, 4, 1, 9000, 9000, 0),
                "An exact native XP handoff admitted a duplicate retry.");
        }
    }
}
