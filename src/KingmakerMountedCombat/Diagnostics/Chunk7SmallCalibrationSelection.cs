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
        public string Case { get; set; }
        public Chunk7OrdinaryFieldSelection Source { get; set; }
        public string ProfileId { get; set; }
        public string NativeInventorySha256 { get; set; }
        public string NativeCalibrationSha256 { get; set; }
        internal IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != CaseName) errors.Add("The closed compiled Small calibration case differs.");
            if (Source == null) errors.Add("Exact paid remote source provenance is required.");
            else errors.AddRange(Source.Validate(Chunk7OrdinaryFieldSelection.Scenario, runId));
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
            var matches = MountedRiderProfileCatalog.Small.Where(value => value.Body.ProfileId == ProfileId).ToArray();
            if (matches.Length != 1 || matches[0].Body.NativeInventorySha256 != NativeInventorySha256 ||
                matches[0].NativeCalibrationSha256 != NativeCalibrationSha256)
                throw new InvalidOperationException("No unique compiled Small declaration matches the exact input provenance; native calibration admission refused.");
            return matches[0];
        }
    }
}
