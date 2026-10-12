using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    public sealed class Chunk7OrdinaryFieldSelection
    {
        public const string Scenario = "chunk7-ordinary-field-admission";
        public const string CaseName = "paid-small-remote-field";
        public const string SourceLeaf = "ordinary-acquisition-source.json";
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string ActorId { get; set; }
        public string PetId { get; set; }

        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != CaseName) errors.Add("Field admission requires its exact closed case.");
            if (!Regex.IsMatch(SourceRunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || SourceRunId == runId)
                errors.Add("A distinct completed paid acquisition run is required.");
            foreach (var hash in new[] { SourceArtifactSha256, SourceArchiveSha256 })
                if (!Regex.IsMatch(hash ?? "", "^[a-f0-9]{64}$")) errors.Add("Exact immutable acquisition/archive hashes are required.");
            foreach (var id in new[] { ActorId, PetId })
                if (!Guid.TryParseExact(id, "D", out var parsed) || parsed == Guid.Empty || parsed.ToString("D") != id)
                    errors.Add("Exact canonical acquired actor/pet IDs are required.");
            if (ActorId == PetId) errors.Add("The acquired rider and pet must differ.");
            return errors;
        }
    }
}
