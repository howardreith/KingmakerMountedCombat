using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KingmakerMountedCombat.Domain;
namespace KingmakerMountedCombat.Diagnostics
{
    public sealed class Chunk7SmallCalibrationSelection
    {
        internal const string Scenario = "chunk7-small-profile-calibration";
        internal const string CaseName = "compiled-small-horse-rt";
        internal const string ManualCaseName = "compiled-manual-derived-small-horse-rt";
        internal const string AssistedManualCaseName = "compiled-assisted-two-owner-small-horse-rt";
        internal const string FixtureManualCaseName = "compiled-owner-seed-fixture-small-horse-rt";
        internal const string FixturePresentationCaseName = "compiled-owner-seed-fixture-small-horse-presentation-rt";
        public string Case { get; set; }
        public Chunk7OrdinaryFieldSelection Source { get; set; }
        public Chunk7SmallManualSourceSelection ManualSource { get; set; }
        public Chunk7SmallFixtureManualSourceSelection FixtureSource { get; set; }
        public bool ShouldSerializeSource() => Source != null;
        public bool ShouldSerializeManualSource() => ManualSource != null;
        public bool ShouldSerializeFixtureSource() => FixtureSource != null;
        internal bool UsesAssistedManualField => Case == AssistedManualCaseName;
        internal bool UsesManualField => Case == ManualCaseName || UsesAssistedManualField;
        internal bool UsesPresentation => Case == FixturePresentationCaseName;
        internal bool UsesFixtureManualField => Case == FixtureManualCaseName || UsesPresentation;
        internal bool UsesColdManualField => UsesManualField || UsesFixtureManualField;
        public string ProfileId { get; set; }
        public string NativeInventorySha256 { get; set; }
        public string NativeCalibrationSha256 { get; set; }
        internal IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != CaseName && Case != ManualCaseName && Case != AssistedManualCaseName && Case != FixtureManualCaseName && Case != FixturePresentationCaseName) errors.Add("The closed compiled Small calibration case differs.");
            if (UsesManualField)
            {
                if (Source != null || FixtureSource != null || ManualSource == null) errors.Add("The Small Manual case requires only its exact acquired Manual source.");
                else errors.AddRange(ManualSource.Validate(runId));
            }
            else if (UsesFixtureManualField)
            {
                if (Source != null || ManualSource != null || FixtureSource == null) errors.Add("The F09 Small case requires only its exact owner-seed fixture Manual source.");
                else errors.AddRange(FixtureSource.Validate(runId));
            }
            else
            {
                if (ManualSource != null || FixtureSource != null || Source == null) errors.Add("Exact paid remote source provenance is required.");
                else errors.AddRange(Source.Validate(Chunk7OrdinaryFieldSelection.Scenario, runId));
            }
            if (!Regex.IsMatch(ProfileId ?? "", "^small-[a-z0-9]+(?:-[a-z0-9]+)*-horse-v[1-9][0-9]*$")) errors.Add("An exact versioned Small profile ID is required.");
            if (!Regex.IsMatch(NativeInventorySha256 ?? "", "^[a-f0-9]{64}$") || !Regex.IsMatch(NativeCalibrationSha256 ?? "", "^[a-f0-9]{64}$")) errors.Add("Immutable body and declaration provenance hashes are required.");
            return errors;
        }
        // The catalogue is compiled source. Neither request nor fixture can add a
        // declaration, supply numeric pose parameters, or reinterpret Medium.
        // The calibration hash pins an initial measured declaration record; it
        // does not assert mounted PASS or owner visual acceptance in advance.
        internal DeclaredSmallMountedProfile RequireCompiledDeclaration()
        {
            var catalogue = UsesFixtureManualField ? MountedRiderProfileCatalog.PrivateCalibrationSmall : MountedRiderProfileCatalog.Small;
            var matches = catalogue.Where(value => value.Body.ProfileId == ProfileId).ToArray();
            if (matches.Length != 1 || matches[0].Body.NativeInventorySha256 != NativeInventorySha256 ||
                matches[0].NativeCalibrationSha256 != NativeCalibrationSha256)
                throw new InvalidOperationException("No unique compiled Small declaration matches the exact input provenance; native calibration admission refused.");
            return matches[0];
        }
    }

    // F09 is a distinct owner-seed-derived, cross-candidate fixture. Its source
    // is never an acquired-field Manual or a normal-player acquisition proof.
    public sealed class Chunk7SmallFixtureManualSourceSelection
    {
        internal const string SourceLeaf = "small-owner-seed-fixture-source.json";
        internal const string NativeName = "KMC_C7_SMALL_FIXTURE";
        internal static bool IsNativeManualLeaf(string leaf) =>
            Regex.IsMatch(leaf ?? "", "^Manual_[1-9][0-9]*_KMC_C7_SMALL_FIXTURE\\.zks$");
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string ColdReceiptSha256 { get; set; }
        public string ActorId { get; set; }
        public string PetId { get; set; }
        internal IReadOnlyList<string> Validate(string runId)
        {
            var errors = new List<string>();
            if (!Regex.IsMatch(SourceRunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || SourceRunId == runId)
                errors.Add("A distinct completed F09 fixture producer is required.");
            foreach (var value in new[] { SourceArtifactSha256, SourceArchiveSha256, ColdReceiptSha256 })
                if (!Regex.IsMatch(value ?? "", "^[a-f0-9]{64}$")) errors.Add("Exact immutable F09 producer and cold-receipt hashes are required.");
            foreach (var id in new[] { ActorId, PetId })
                if (!Guid.TryParseExact(id, "D", out var parsed) || parsed == Guid.Empty || parsed.ToString("D") != id)
                    errors.Add("Exact canonical native F09 Small owner and Horse IDs are required.");
            if (ActorId == PetId) errors.Add("The F09 Small owner and Horse must differ.");
            return errors;
        }
    }

    // Provenance only. This DTO never grants a profile, actor or save authority.
    public sealed class Chunk7SmallManualSourceSelection
    {
        internal const string SourceLeaf = "small-acquired-manual-source.json";
        internal static bool IsNativeManualLeaf(string leaf)
        {
            var match = Regex.Match(leaf ?? "", "^Manual_([1-9][0-9]*)_KMC_C7_ACQUIRED_FIELD\\.zks$");
            return match.Success && int.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var number) && number > 0 &&
                leaf == Chunk7AcquiredManualSelection.NativeOutputLeaf(number);
        }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string ActorId { get; set; }
        public string PetId { get; set; }
        internal IReadOnlyList<string> Validate(string runId)
        {
            var errors = new List<string>();
            if (!Regex.IsMatch(SourceRunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || SourceRunId == runId)
                errors.Add("A distinct completed first-owner acquired Manual is required.");
            foreach (var value in new[] { SourceArtifactSha256, SourceArchiveSha256 })
                if (!Regex.IsMatch(value ?? "", "^[a-f0-9]{64}$")) errors.Add("Exact immutable Small Manual source hashes are required.");
            foreach (var id in new[] { ActorId, PetId })
                if (!Guid.TryParseExact(id, "D", out var parsed) || parsed == Guid.Empty || parsed.ToString("D") != id)
                    errors.Add("Exact canonical native Small owner/pet IDs are required.");
            if (ActorId == PetId) errors.Add("The native Small owner and Horse must differ.");
            return errors;
        }
    }
}
