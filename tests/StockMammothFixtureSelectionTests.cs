using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class StockMammothFixtureSelectionTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("reference-free Mammoth fixture admits the qualified seed and disclosed XP", () =>
                TestRunner.Equal(0, Valid().Validate(Chunk7StockMammothFixtureSelection.Scenario, "fixture-run").Count,
                    "Exact closed selection was rejected."));
            Refuse(runner, "Ranger nine", value => value.TargetRangerLevel = 9);
            Refuse(runner, "unreported XP assistance", value => value.NativeXpAssistance = 0);
            Refuse(runner, "an extra native write", value => value.ExpectedNativeWriteCount = 2);
            Refuse(runner, "a foreign seed", value => value.SourceSeedSha256 = new string('a', 64));
            Refuse(runner, "unreviewed inventory", value => value.SourceInventorySha256 = new string('b', 64));
            Refuse(runner, "foreign staging", value => value.BaseGameOnly = false);
            runner.Run("reference-free Mammoth fixture refuses source-run reuse", () =>
                TestRunner.True(Valid().Validate(Chunk7StockMammothFixtureSelection.Scenario,
                    Chunk7StockMammothFixtureSelection.SeedQualificationRunId).Count > 0,
                    "The producer reused its qualification run."));
            runner.Run("reference-free Mammoth Manual leaf is canonical", () =>
            {
                TestRunner.True(Chunk7StockMammothFixtureSelection.IsManualLeaf("Manual_304_KMC_C7_STOCK_MAMMOTH.zks"),
                    "Native positive Manual leaf was rejected.");
                foreach (var leaf in new[] { "Manual_KMC_C7_STOCK_MAMMOTH.zks", "Manual_0304_KMC_C7_STOCK_MAMMOTH.zks", "Manual_0_KMC_C7_STOCK_MAMMOTH.zks",
                    "Manual_2147483647_KMC_C7_STOCK_MAMMOTH.zks", "Manual_304_KMC_C7_SMALL_FIXTURE.zks", "Auto_1.zks" })
                    TestRunner.True(!Chunk7StockMammothFixtureSelection.IsManualLeaf(leaf), "Noncanonical leaf was admitted: " + leaf);
            });
        }

        private static Chunk7StockMammothFixtureSelection Valid() => new Chunk7StockMammothFixtureSelection
        {
            Case = Chunk7StockMammothFixtureSelection.CaseName,
            SourceSeedId = Chunk7StockMammothFixtureSelection.SeedId,
            SourceSeedSha256 = Chunk7StockMammothFixtureSelection.SeedSha256,
            SourceQualificationRunId = Chunk7StockMammothFixtureSelection.SeedQualificationRunId,
            SourceQualificationSha256 = Chunk7StockMammothFixtureSelection.SeedQualificationSha256,
            SourceInventoryRunId = Chunk7StockMammothFixtureSelection.NativeInventoryRunId,
            SourceInventorySha256 = Chunk7StockMammothFixtureSelection.NativeInventorySha256,
            BaseGameOnly = true, ExpectedNativeLoadCount = 1, ExpectedNativeWriteCount = 1,
            TargetRangerLevel = 10, NativeXpAssistance = Chunk7StockMammothFixtureSelection.DisclosedNativeXpAssistance
        };
        private static void Refuse(TestRunner runner, string name, Action<Chunk7StockMammothFixtureSelection> change) =>
            runner.Run("reference-free Mammoth fixture refuses " + name, () =>
            {
                var value = Valid(); change(value);
                TestRunner.True(value.Validate(Chunk7StockMammothFixtureSelection.Scenario, "fixture-run").Count > 0,
                    "Invalid selection was accepted.");
            });
    }
}
