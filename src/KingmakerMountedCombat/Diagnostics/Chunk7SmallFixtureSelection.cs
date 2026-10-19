using System;
using System.Collections.Generic;
using System.Globalization;

namespace KingmakerMountedCombat.Diagnostics
{
    // F09's first measurement is deliberately unmounted. This exact case creates
    // no profile declaration, pose, mounted action or historical F10 ancestry.
    public sealed class Chunk7SmallFixtureSelection
    {
        public const string Scenario = "chunk7-small-fixture-preparation";
        public const string CaseName = "owner-seed-default-halfling-horse-manual";
        public const string OutputName = "KMC_C7_SMALL_FIXTURE";
        public const string SourceLeaf = "small-fixture-owner-seed.json";
        public const string QualificationLeaf = "small-fixture-seed-qualification.json";
        public const string InventoryLeaf = "small-fixture-native-inventory.json";
        public const string SeedIdentity = "KMC_C7_NO_MODS_OWNER_SEED";
        public const string SeedHash = "3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f";
        public const string QualificationRun = "20261017-c7b-world18-ownerseed-s4-cold";
        public const string QualificationHash = "c0a4bf3ed11f1bc8d80ffa6f999491562f1896d2f3b38f4518067ad87a9ff74a";
        public const string InventoryRun = "20261018-c7b-world26-ownerseed-b3s1d-save";
        public const string InventoryHash = "01438210fbc9cdf8f8bc228b3d973ffff7220eaa32449bf43f4462fb355723a8";
        public const string RaceGuid = "b0c3ef2729c498f47970bb50fa1acd30";
        public const string PresetGuid = "639cebe8cbc098345af429d62fd79578";
        public const string SkeletonName = "MaleHalfling";
        public const string MainActorId = "73681ef2-b281-4bc6-bc05-2de68c1616cb";
        public const string SeedFileName = "Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks";
        public const string SeedInternalName = "KMC C7 NO MODS OWNER SEED";
        public const string CampaignId = "3f7381f7-aba6-43bf-bae6-39c6a66a7180";
        public const string CampaignName = "Testy the Modless";
        public const string AreaId = "b10e86516b6437d45a629931c30936fe";

        public string Case { get; set; }
        public string SeedId { get; set; }
        public string SeedSha256 { get; set; }
        public string SeedQualificationRunId { get; set; }
        public string SeedQualificationArtifactSha256 { get; set; }
        public string NativeInventoryRunId { get; set; }
        public string NativeInventoryArtifactSha256 { get; set; }
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        public int TargetRangerLevel { get; set; }

        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != CaseName || SeedId != SeedIdentity || SeedSha256 != SeedHash)
                errors.Add("F09 unmounted fixture requires its closed qualified clean owner seed.");
            if (SeedQualificationRunId != QualificationRun || SeedQualificationArtifactSha256 != QualificationHash ||
                NativeInventoryRunId != InventoryRun || NativeInventoryArtifactSha256 != InventoryHash ||
                runId == SeedQualificationRunId || runId == NativeInventoryRunId)
                errors.Add("F09 unmounted fixture requires the accepted distinct seed cold and native Halfling catalogue artifacts.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 1 || TargetRangerLevel != 4)
                errors.Add("F09 fixture requires KMC-alone base-game staging, one native load, native Ranger 4 choices and one genuine Manual.");
            return errors;
        }

        internal static string ManualLeaf(int number)
        {
            if (number <= 0 || number == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(number));
            return "Manual_" + number.ToString(CultureInfo.InvariantCulture) + "_" + OutputName + ".zks";
        }
        internal static bool IsManualLeaf(string value)
        {
            const string prefix = "Manual_", suffix = "_" + OutputName + ".zks";
            return value != null && value.StartsWith(prefix, StringComparison.Ordinal) && value.EndsWith(suffix, StringComparison.Ordinal) &&
                value.Length > prefix.Length + suffix.Length && int.TryParse(value.Substring(prefix.Length,
                    value.Length - prefix.Length - suffix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
                number > 0 && number != int.MaxValue && value == ManualLeaf(number);
        }

        internal static bool IsSource(RuntimeSaveDescriptor source) => source != null &&
            source.FileName == SeedFileName && source.InternalName == SeedInternalName && source.Sha256 == SeedHash &&
            source.Length == 1239025 && source.LastWriteTimeUtcTicks > 0 && source.GameId == CampaignId &&
            source.GameName == CampaignName && source.Area == AreaId;
    }

    // Pure preflight, used before either native UI is opened. No grant or clamp.
    internal static class Chunk7SmallFixtureAdmission
    {
        internal static void RequireMain(int level, int rangerLevel, int experience, int readyLevel,
            bool canLevel, bool hasPet, long money, long nativeHireCost, int nativeHireReadyLevel)
        {
            Require(level == 3 && rangerLevel == 3 && experience == 9000 && readyLevel == 4 && canLevel && !hasPet,
                "Clean seed main must naturally admit exactly its native Ranger 3 to 4 level.");
            Require(money >= 0 && nativeHireCost > 0 && money >= nativeHireCost && nativeHireReadyLevel == 4,
                "The post-level native hire cost/XP table cannot produce the paid Ranger 4 fixture without assistance.");
        }
        internal static void RequireHire(int mainLevel, int mainRangerLevel, int mainExperience,
            bool mainCanLevel, long money, long nativeCost, int inheritedXp, int readyLevel)
        {
            Require(mainLevel == 4 && mainRangerLevel == 4 && mainExperience == 9000 && !mainCanLevel,
                "Main native progression must settle at Ranger 4 before paid hire.");
            Require(nativeCost > 0 && money >= nativeCost && inheritedXp > 0 && readyLevel == 4,
                "Actual native money/default hire XP cannot reach exactly Ranger 4; no shortfall is authorized.");
        }
        private static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException(reason); }
    }
}
