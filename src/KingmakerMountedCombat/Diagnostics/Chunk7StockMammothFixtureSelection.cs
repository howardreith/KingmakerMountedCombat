using System;
using System.Collections.Generic;
using System.Globalization;

namespace KingmakerMountedCombat.Diagnostics
{
    // A separate clean-seed ancestry for a stock, reference-free Mammoth.
    // It does not claim ordinary acquisition or positive removal by itself.
    public sealed class Chunk7StockMammothFixtureSelection
    {
        public const string Scenario = "chunk7-stock-mammoth-fixture-preparation";
        public const string CaseName = "owner-seed-ranger-ten-stock-mammoth-manual";
        public const string OutputName = "KMC_C7_STOCK_MAMMOTH";
        public const string SeedId = Chunk7SmallFixtureSelection.SeedIdentity;
        public const string SeedSha256 = Chunk7SmallFixtureSelection.SeedHash;
        public const string SeedQualificationRunId = Chunk7SmallFixtureSelection.QualificationRun;
        public const string SeedQualificationSha256 = Chunk7SmallFixtureSelection.QualificationHash;
        public const string NativeInventoryRunId = Chunk7SmallFixtureSelection.InventoryRun;
        public const string NativeInventorySha256 = Chunk7SmallFixtureSelection.InventoryHash;
        public const int StartingExperience = 9000;
        public const int TargetExperience = 105000;
        public const int DisclosedNativeXpAssistance = TargetExperience - StartingExperience;

        public string Case { get; set; }
        public string SourceSeedId { get; set; }
        public string SourceSeedSha256 { get; set; }
        public string SourceQualificationRunId { get; set; }
        public string SourceQualificationSha256 { get; set; }
        public string SourceInventoryRunId { get; set; }
        public string SourceInventorySha256 { get; set; }
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        public int TargetRangerLevel { get; set; }
        public int NativeXpAssistance { get; set; }

        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != CaseName || SourceSeedId != SeedId || SourceSeedSha256 != SeedSha256)
                errors.Add("Stock Mammoth fixture requires its exact qualified clean owner seed.");
            if (SourceQualificationRunId != SeedQualificationRunId || SourceQualificationSha256 != SeedQualificationSha256 ||
                SourceInventoryRunId != NativeInventoryRunId || SourceInventorySha256 != NativeInventorySha256 ||
                runId == SourceQualificationRunId || runId == SourceInventoryRunId)
                errors.Add("Stock Mammoth fixture requires distinct accepted native seed and inventory provenance.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 1 ||
                TargetRangerLevel != 10 || NativeXpAssistance != DisclosedNativeXpAssistance)
                errors.Add("Stock Mammoth fixture requires one clean-seed load, exactly 96000 disclosed native XP, Ranger ten choices and one genuine Manual.");
            return errors;
        }

        internal static bool IsSource(RuntimeSaveDescriptor source) => Chunk7SmallFixtureSelection.IsSource(source);

        internal static string ManualLeaf(int number)
        {
            if (number <= 0 || number == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(number));
            return "Manual_" + number.ToString(CultureInfo.InvariantCulture) + "_" + OutputName + ".zks";
        }
        internal static bool IsManualLeaf(string value)
        {
            const string prefix = "Manual_", suffix = "_" + OutputName + ".zks";
            if (value == null || value.Length <= prefix.Length + suffix.Length ||
                !value.StartsWith(prefix, StringComparison.Ordinal) || !value.EndsWith(suffix, StringComparison.Ordinal)) return false;
            var digits = value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length);
            return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
                number > 0 && number != int.MaxValue && value == ManualLeaf(number);
        }
    }
}
