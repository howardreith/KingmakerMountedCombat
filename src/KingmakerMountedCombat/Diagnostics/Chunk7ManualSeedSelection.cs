using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    // No arrangement authority: one proven native Auto input becomes one fresh
    // native Manual save. The engine chooses and must confirm its exact number.
    public sealed class Chunk7ManualSeedSelection
    {
        public const string Scenario = "chunk7-bootstrap-manual-seed";
        public const string CaseName = "native-save-only";
        public const string SourceLeaf = "manual-seed-bootstrap-source.json";
        public const string InventoryLeaf = "manual-seed-cold-inventory.json";
        public const string OutputName = "KMC_C7_NATIVE_SEED";
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string InventoryRunId { get; set; }
        public string InventoryArtifactSha256 { get; set; }
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        public int ExpectedManualNumber { get; set; }
        internal string OutputLeaf => "Manual_" + ExpectedManualNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_" + OutputName + ".zks";
        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != CaseName) errors.Add("Manual seed requires its exact save-only family.");
            foreach (var value in new[] { SourceRunId, InventoryRunId })
                if (!Regex.IsMatch(value ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || value == runId)
                    errors.Add("Distinct completed bootstrap and cold-inventory runs are required.");
            if (SourceRunId == InventoryRunId) errors.Add("Bootstrap and cold-inventory provenance cannot be the same run.");
            foreach (var value in new[] { SourceArtifactSha256, SourceArchiveSha256, InventoryArtifactSha256 })
                if (!Regex.IsMatch(value ?? "", "^[a-f0-9]{64}$")) errors.Add("Exact immutable source and inventory hashes are required.");
            // The qualified inventory contains only Auto_1. Installed native
            // FindUnusedSaveNumber(Manual) therefore returns one; runtime rechecks
            // the actual list and native getter before creating any descriptor.
            if (ExpectedManualNumber != 1) errors.Add("Only the observed Auto-only inventory's first native Manual number is selected.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 1)
                errors.Add("Manual seed requires base-only staging and exactly one native load and one native write.");
            return errors;
        }
    }
}
