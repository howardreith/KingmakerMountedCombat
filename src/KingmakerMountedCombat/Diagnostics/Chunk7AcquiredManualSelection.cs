using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace KingmakerMountedCombat.Diagnostics
{
    // The actor and pet already exist in a completed native acquisition output.
    // This declaration grants neither acquisition nor XP/money/actor mutation.
    public sealed class Chunk7AcquiredManualSelection
    {
        public const string Scenario = "chunk7-acquired-manual-derivative";
        public const string CaseName = "preserve-acquired-field";
        public const string SourceLeaf = "acquired-manual-source.json";
        public const string OutputName = "KMC_C7_ACQUIRED_FIELD";
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string ActorId { get; set; }
        public string PetId { get; set; }
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != CaseName) errors.Add("Acquired Manual requires its exact field-preserving family.");
            if (!Regex.IsMatch(SourceRunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || SourceRunId == runId)
                errors.Add("A distinct completed Manual-derived acquisition run is required.");
            foreach (var hash in new[] { SourceArtifactSha256, SourceArchiveSha256 })
                if (!Regex.IsMatch(hash ?? "", "^[a-f0-9]{64}$")) errors.Add("Exact immutable acquisition artifact and Auto hashes are required.");
            if (!Chunk7ManualDerivedSelection.NativeId(ActorId) || !Chunk7ManualDerivedSelection.NativeId(PetId) || ActorId == PetId)
                errors.Add("The two distinct observed native owner/pet identities are required.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 1)
                errors.Add("Acquired Manual requires base-only staging and exactly one load and one fresh Manual write.");
            return errors;
        }
        internal static string NativeOutputLeaf(int number)
        {
            if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
            return "Manual_" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_" + OutputName + ".zks";
        }
    }
}
