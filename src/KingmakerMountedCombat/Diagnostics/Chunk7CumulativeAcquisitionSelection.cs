using System;
using System.Collections.Generic;
using System.Globalization;

namespace KingmakerMountedCombat.Diagnostics
{
    // A distinct second-owner request. NativeChoicePins projects only the shared
    // legal-choice inputs; it never changes RuntimeRequest.ManualDerived, source
    // provenance, save authority, or the first-owner family's admission.
    public sealed class Chunk7CumulativeAcquisitionSelection
    {
        public const string Scenario = "chunk7-cumulative-acquisition";
        public const string SourceLeaf = "cumulative-acquisition-source.json";
        public const string OutputLeaf = "Auto_1.zks";
        public const string AssistedSourceMode = "assisted-producer-and-cold";
        public const string AssistedColdLeaf = "cumulative-assisted-cold-source.json";
        public string SourceMode { get; set; }
        public string AssistedColdRunId { get; set; }
        public string AssistedColdArtifactSha256 { get; set; }
        public bool ShouldSerializeSourceMode() => SourceMode != null;
        public bool ShouldSerializeAssistedColdRunId() => AssistedColdRunId != null;
        public bool ShouldSerializeAssistedColdArtifactSha256() => AssistedColdArtifactSha256 != null;
        internal bool UsesAssistedCold => SourceMode == AssistedSourceMode;
        internal string SelectedManualName => UsesAssistedCold ? "KMC_C7_ASSISTED_FIXTURE" : "KMC_C7_ACQUIRED_FIELD";
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        public string NativeWriteLeaf { get; set; }
        public Chunk7ManualDerivedReadinessPins Readiness { get; set; }
        public string PriorOwnerId { get; set; }
        public string PriorPetId { get; set; }
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

        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario) errors.Add("Cumulative acquisition requires its exact separate family.");
            if (SourceMode != null || AssistedColdRunId != null || AssistedColdArtifactSha256 != null)
            {
                if (!UsesAssistedCold || !System.Text.RegularExpressions.Regex.IsMatch(AssistedColdRunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") ||
                    AssistedColdRunId == SourceRunId || AssistedColdRunId == runId ||
                    !System.Text.RegularExpressions.Regex.IsMatch(AssistedColdArtifactSha256 ?? "", "^[a-f0-9]{64}$"))
                    errors.Add("Assisted cumulative source requires its exact mode and distinct successful cold receipt pins.");
                if (Readiness?.MainActorId != PriorOwnerId)
                    errors.Add("Assisted cumulative input must retain the observed main owner and Horse.");
            }
            // These are pure field/choice rules. Actual cumulative source identity
            // and authority are checked separately and never delegated to seed intake.
            errors.AddRange(NativeChoicePins().Validate(Chunk7ManualDerivedSelection.Scenario, runId, true));
            if (!Chunk7ManualDerivedSelection.NativeId(PriorOwnerId) || !Chunk7ManualDerivedSelection.NativeId(PriorPetId) || PriorOwnerId == PriorPetId ||
                Readiness?.PartyCharacterIds == null || Array.IndexOf(Readiness.PartyCharacterIds, PriorOwnerId) < 0 ||
                Array.IndexOf(Readiness.PartyCharacterIds, PriorPetId) >= 0 || ExistingActorId == PriorOwnerId || ExistingActorId == PriorPetId)
                errors.Add("One distinct existing Horse owner/pet is required; neither may become the second acquisition target.");
            return errors;
        }
        internal Chunk7ManualDerivedSelection NativeChoicePins() => new Chunk7ManualDerivedSelection {
            Case = Case, SourceRunId = SourceRunId, SourceArtifactSha256 = SourceArtifactSha256, SourceArchiveSha256 = SourceArchiveSha256,
            BaseGameOnly = BaseGameOnly, ExpectedNativeLoadCount = ExpectedNativeLoadCount, ExpectedNativeWriteCount = ExpectedNativeWriteCount,
            NativeWriteLeaf = NativeWriteLeaf, Readiness = Readiness, ExistingActorId = ExistingActorId, ExistingLevel = ExistingLevel,
            ExistingExperience = ExistingExperience, ExistingRangerLevel = ExistingRangerLevel, SmallRaceGuid = SmallRaceGuid,
            SmallPresetGuid = SmallPresetGuid, SkeletonName = SkeletonName, Gender = Gender, LeftHanded = LeftHanded, MediumBody = MediumBody
        };
        internal bool IsSelectedManualLeaf(string value)
        {
            if (!UsesAssistedCold) return IsAcquiredManualLeaf(value);
            const string prefix = "Manual_", suffix = "_KMC_C7_ASSISTED_FIXTURE.zks";
            if (value == null || !value.StartsWith(prefix, StringComparison.Ordinal) || !value.EndsWith(suffix, StringComparison.Ordinal)) return false;
            var text = value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length);
            int number;
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number > 0 &&
                text == number.ToString(CultureInfo.InvariantCulture);
        }
        internal static bool IsAcquiredManualLeaf(string value)
        {
            const string prefix = "Manual_", suffix = "_KMC_C7_ACQUIRED_FIELD.zks";
            if (value == null || !value.StartsWith(prefix, StringComparison.Ordinal) || !value.EndsWith(suffix, StringComparison.Ordinal)) return false;
            var text = value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length);
            int number;
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number > 0 &&
                text == number.ToString(CultureInfo.InvariantCulture);
        }
    }
}
