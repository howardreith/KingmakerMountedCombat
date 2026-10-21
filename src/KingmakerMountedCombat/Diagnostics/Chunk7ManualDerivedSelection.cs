using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    // Caller pins are requests for verification, never native load/build authority.
    // The separate qualifier proves completed Manual lineage; the runtime then
    // observes its actual cold load and rechecks these values before any choice.
    public sealed class Chunk7ManualDerivedSelection
    {
        public const string Scenario = "chunk7-manual-derived-acquisition";
        public const string ExistingCase = "existing-ranger-horse";
        public const string PaidSmallCase = "paid-small-ranger-horse";
        public const string PaidMediumCase = "paid-medium-ranger-horse";
        public const string SourceLeaf = "manual-derived-source.json";
        public const string ManualOutputName = "KMC_C7_MANUAL_DERIVED_FIELD";
        public const string ManualOutputLeaf = "Manual_2_KMC_C7_MANUAL_DERIVED_FIELD.zks";
        public const string CumulativeLegacyOutputLeaf = "Manual_303_KMC_C7_MANUAL_DERIVED_FIELD.zks";
        public const string CumulativeAssistedOutputLeaf = "Manual_301_KMC_C7_MANUAL_DERIVED_FIELD.zks";
        // A registered owner seed is Manual_303; its first genuine new Manual is 304.
        public const string OwnerSeedOutputLeaf = "Manual_304_KMC_C7_MANUAL_DERIVED_FIELD.zks";
        public const string OwnerSeedInputLeaf = "Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks";
        public const string OwnerSeedInputName = "KMC C7 NO MODS OWNER SEED";
        // Historical level-up Auto output. Native level-up never requests this.
        public const string OutputLeaf = "Auto_1.zks";
        public const int RangerAcquisitionLevel = 4;
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        public string NativeWriteLeaf { get; set; }
        public Chunk7ManualDerivedReadinessPins Readiness { get; set; }
        public string ExistingActorId { get; set; }
        public int? ExistingLevel { get; set; }
        public int? ExistingExperience { get; set; }
        public int? ExistingRangerLevel { get; set; }
        public string SmallRaceGuid { get; set; }
        public string SmallPresetGuid { get; set; }
        public string SkeletonName { get; set; }
        public string Gender { get; set; }
        public bool? LeftHanded { get; set; }
        public Chunk7MediumHireBodyPins MediumBody { get; set; }
        public bool ShouldSerializeMediumBody() => MediumBody != null;
        internal bool PaidHire => Case == PaidSmallCase || Case == PaidMediumCase;
        internal bool UsesOwnerSeedCold => NativeWriteLeaf == OwnerSeedOutputLeaf;

        public IReadOnlyList<string> Validate(string scenario, string runId, bool cumulativeAuto = false, bool cumulativeManual = false)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != ExistingCase && Case != PaidSmallCase && Case != PaidMediumCase)
                errors.Add("Manual-derived acquisition requires its exact family and case.");
            if (!Regex.IsMatch(SourceRunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || SourceRunId == runId)
                errors.Add("A distinct completed Manual seed run is required.");
            foreach (var value in new[] { SourceArtifactSha256, SourceArchiveSha256 })
                if (!Regex.IsMatch(value ?? "", "^[a-f0-9]{64}$")) errors.Add("Exact immutable Manual source hashes are required.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 1 ||
                (cumulativeAuto ? !cumulativeManual || NativeWriteLeaf != CumulativeAssistedOutputLeaf &&
                    NativeWriteLeaf != CumulativeLegacyOutputLeaf :
                    NativeWriteLeaf != ManualOutputLeaf && NativeWriteLeaf != OwnerSeedOutputLeaf))
                errors.Add("Acquisition requires its exact base-only one-load, one-write native output.");
            if (Readiness == null) errors.Add("Observed source readiness pins are required.");
            else errors.AddRange(Readiness.Validate());
            if (Case == ExistingCase)
            {
                if (!NativeId(ExistingActorId) || Readiness?.PartyCharacterIds == null || !Readiness.PartyCharacterIds.Contains(ExistingActorId))
                    errors.Add("The exact existing owner must be an observed field party character.");
                if (!ExistingLevel.HasValue || ExistingLevel < 1 || ExistingLevel >= 20 || !ExistingExperience.HasValue || ExistingExperience < 0 ||
                    !ExistingRangerLevel.HasValue || ExistingRangerLevel < 0 || ExistingRangerLevel >= RangerAcquisitionLevel || ExistingRangerLevel > ExistingLevel)
                    errors.Add("Exact pre-acquisition native XP and class levels are required.");
                if (SmallRaceGuid != null || SmallPresetGuid != null || SkeletonName != null || Gender != null || LeftHanded.HasValue)
                    errors.Add("Existing acquisition cannot change native body identity.");
            }
            else if (Case == PaidSmallCase)
            {
                if (ExistingActorId != null || ExistingLevel.HasValue || ExistingExperience.HasValue || ExistingRangerLevel.HasValue)
                    errors.Add("A paid new companion cannot substitute existing-actor pins.");
                if (!BlueprintId(SmallRaceGuid) || !BlueprintId(SmallPresetGuid) || string.IsNullOrWhiteSpace(SkeletonName) || SkeletonName.Length > 160 ||
                    Gender != "Male" && Gender != "Female" || !LeftHanded.HasValue)
                    errors.Add("The exact observed Small race/preset/skeleton/gender/handedness are required.");
            }
            if (Case == PaidMediumCase)
            {
                if (ExistingActorId != null || ExistingLevel.HasValue || ExistingExperience.HasValue || ExistingRangerLevel.HasValue ||
                    SmallRaceGuid != null || SmallPresetGuid != null || SkeletonName != null || Gender != null || LeftHanded.HasValue)
                    errors.Add("Paid Medium hiring has separate body pins and cannot borrow an existing actor or Small choices.");
                if (MediumBody == null) errors.Add("An observed native Medium party body is required.");
                else errors.AddRange(MediumBody.Validate(Readiness?.PartyCharacterIds));
            }
            else if (MediumBody != null) errors.Add("Medium body pins belong only to the separate paid Medium case.");
            return errors;
        }
        internal static bool NativeId(string value)
        { Guid parsed; return Guid.TryParseExact(value, "D", out parsed) && parsed != Guid.Empty && parsed.ToString("D") == value; }
        internal static bool BlueprintId(string value) => Regex.IsMatch(value ?? "", "^[a-f0-9]{32}$");
    }
    // Body provenance, not an asset or character grant. The scenario rechecks the
    // exact observed actor and installed race/preset/skeleton before native hiring.
    public sealed class Chunk7MediumHireBodyPins
    {
        public string TemplateActorId { get; set; }
        public string RaceGuid { get; set; }
        public string PresetGuid { get; set; }
        public string SkeletonName { get; set; }
        public string Gender { get; set; }
        public bool? LeftHanded { get; set; }
        internal IReadOnlyList<string> Validate(string[] partyIds)
        {
            var errors = new List<string>();
            if (!Chunk7ManualDerivedSelection.NativeId(TemplateActorId) || partyIds == null || !partyIds.Contains(TemplateActorId) ||
                !Chunk7ManualDerivedSelection.BlueprintId(RaceGuid) || !Chunk7ManualDerivedSelection.BlueprintId(PresetGuid) ||
                string.IsNullOrWhiteSpace(SkeletonName) || SkeletonName.Length > 160 || SkeletonName.Any(char.IsControl) ||
                Gender != "Male" && Gender != "Female" || !LeftHanded.HasValue)
                errors.Add("Paid Medium body requires an exact observed party actor, race/preset/rig and native gender/handedness.");
            return errors;
        }
    }
    public sealed class Chunk7ManualDerivedReadinessPins
    {
        public string MainActorId { get; set; }
        public int MainLevel { get; set; }
        public int MainExperience { get; set; }
        public long Money { get; set; }
        public int ExperienceRatePercent { get; set; }
        public int NativeHireCost { get; set; }
        public int DefaultHireExperience { get; set; }
        public string[] PartyCharacterIds { get; set; }
        internal IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            if (!Chunk7ManualDerivedSelection.NativeId(MainActorId) || MainLevel < 1 || MainLevel > 20 || MainExperience < 0 || Money < 0 ||
                ExperienceRatePercent != 100 || NativeHireCost <= 0 || DefaultHireExperience < 0)
                errors.Add("Observed native readiness is malformed or uses unsupported XP scaling.");
            if (PartyCharacterIds == null || PartyCharacterIds.Length < 1 || PartyCharacterIds.Length > 6 ||
                PartyCharacterIds.Any(value => !Chunk7ManualDerivedSelection.NativeId(value)) ||
                PartyCharacterIds.Distinct(StringComparer.Ordinal).Count() != PartyCharacterIds.Length ||
                !PartyCharacterIds.SequenceEqual(PartyCharacterIds.OrderBy(value => value, StringComparer.Ordinal)) || !PartyCharacterIds.Contains(MainActorId))
                errors.Add("The source party must be a unique sorted native roster including its main character.");
            return errors;
        }
    }
}
