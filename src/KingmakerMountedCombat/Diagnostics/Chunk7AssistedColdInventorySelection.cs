using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    // A distinct read-only cold input. The historical Working identity is never
    // changed or reused as this separately qualified assisted campaign.
    public sealed class Chunk7AssistedColdInventorySelection
    {
        public const string Scenario = "chunk7-assisted-cold-inventory";
        public const string CaseName = "assisted-manual-readonly-inventory";
        public const string SourceLeaf = "assisted-cold-source.json";
        public const string ManualName = "KMC_C7_ASSISTED_FIXTURE";
        internal static bool IsManualLeaf(string value)
        {
            const string prefix = "Manual_", suffix = "_KMC_C7_ASSISTED_FIXTURE.zks";
            if (value == null || value.Length <= prefix.Length + suffix.Length || !value.StartsWith(prefix, StringComparison.Ordinal) || !value.EndsWith(suffix, StringComparison.Ordinal)) return false;
            var digits = value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length);
            return int.TryParse(digits, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number) && number > 0 &&
                value == prefix + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + suffix;
        }
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
            if (scenario != Scenario || Case != CaseName) errors.Add("Assisted cold inventory requires its exact closed read-only family.");
            if (!Regex.IsMatch(SourceRunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || SourceRunId == runId)
                errors.Add("A distinct completed assisted fixture source is required.");
            foreach (var value in new[] { SourceArtifactSha256, SourceArchiveSha256 })
                if (!Regex.IsMatch(value ?? "", "^[a-f0-9]{64}$")) errors.Add("Exact immutable assisted fixture artifact/archive hashes are required.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 0)
                errors.Add("Assisted cold inventory requires base-only staging, one native load and zero writes.");
            return errors;
        }
    }
}
