using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    // A distinct read-only cold input. The historical Working identity is never
    // changed or reused as this engine-minted campaign.
    public sealed class Chunk7BootstrapInventorySelection
    {
        public const string Scenario = "chunk7-bootstrap-cold-inventory";
        public const string CaseName = "authored-readonly-inventory";
        public const string SourceLeaf = "bootstrap-inventory-source.json";
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }

        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != CaseName) errors.Add("Bootstrap inventory requires its exact closed read-only family.");
            if (!Regex.IsMatch(SourceRunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || SourceRunId == runId)
                errors.Add("A distinct completed native bootstrap source is required.");
            foreach (var value in new[] { SourceArtifactSha256, SourceArchiveSha256 })
                if (!Regex.IsMatch(value ?? "", "^[a-f0-9]{64}$")) errors.Add("Exact immutable native bootstrap artifact/archive hashes are required.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 0)
                errors.Add("Bootstrap inventory requires base-only staging, one native load and zero writes.");
            return errors;
        }
    }
}
