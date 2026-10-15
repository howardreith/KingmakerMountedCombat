using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    // A separate native campaign input. These IDs are copied only from a complete
    // same-candidate two-owner Manual receipt; this declaration creates no actors.
    public sealed class Chunk7OwnedPairsSelection
    {
        public const string Scenario = "chunk7-owned-two-pair-campaign";
        public const string SourceLeaf = "owned-two-pair-source.json";
        public const string SourceCase = "preserve-two-acquired-field";
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string CheckpointSourceRunId { get; set; }
        public string CheckpointSourceArtifactSha256 { get; set; }
        public string RiderAId { get; set; }
        public string MountAId { get; set; }
        public string RiderBId { get; set; }
        public string MountBId { get; set; }
        public string[] PartyIds { get; set; }
        public string SourceArea { get; set; }
        // Keep every historical serialized selection byte shape unchanged.
        public bool ShouldSerializeSourceArea() => SourceArea != null;
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        internal string[] PairIds => new[] { RiderAId, MountAId, RiderBId, MountBId };
        internal string RuntimeScenario
        {
            get
            {
                switch (Case)
                {
                    case "exploration": return "chunk7-two-pair-exploration";
                    case "combat-rider-first": return "chunk7-two-pair-combat-rider-first";
                    case "combat-mount-first": return "chunk7-two-pair-combat-mount-first";
                    case "realtime-core": return "chunk7-two-pair-realtime-core";
                    case "realtime-casting": return "chunk7-two-pair-realtime-casting";
                    case "realtime-charge": return "chunk7-two-pair-realtime-charge";
                    case "native-life-cleanup": return "chunk7-two-pair-native-life-cleanup";
                    case "persistence-save": return "chunk7-persistence-save";
                    case "persistence-load": return "chunk7-persistence-load";
                    default: return null;
                }
            }
        }
        internal bool Persistence => Case == "persistence-save" || Case == "persistence-load";
        internal bool Cold => Case == "persistence-load";
        internal static bool SupportedCheckpoint(string value) => Array.IndexOf(new[] {
            "multi-exploration-manual", "multi-exploration-quick", "multi-exploration-auto",
            "multi-rt-debt-manual", "multi-tb-debt-manual", "zero-mounted-tb-retained-manual",
            "legacy-mounted-pending-tb-manual", "multi-area-autosave" }, value) >= 0;
        internal static bool OwnedSchema1Source(string value) => value == "owned-schema1-manual";
        internal static bool OwnedSchema1Cold(string value) => value == "owned-schema1-resave-manual";
        internal static bool SupportedSaveBoundary(string value) => value == "multi-nonselected-mount-unsettled-manual" ||
            value == "multi-owned-worker-cancel-manual";
        internal static bool RepeatedCheckpoint(string value) => value == "multi-exploration-three-loads-manual" ||
            value == "multi-exploration-three-area-transfers-manual";
        internal int LoadCount(string checkpoint) => Cold && checkpoint == "multi-exploration-three-loads-manual" ? 3 : 1;
        internal int WriteCount(string checkpoint) => Case == "persistence-save" && checkpoint == "multi-owned-worker-cancel-manual" ? 2 : Case == "persistence-save" ||
            Cold && (checkpoint == "legacy-mounted-pending-tb-manual" || OwnedSchema1Cold(checkpoint)) ? 1 : 0;
        public IReadOnlyList<string> Validate(string scenario, string runId, string checkpoint)
        {
            var errors = new List<string>();
            if (scenario != Scenario || RuntimeScenario == null) errors.Add("Owned pairs require the separately closed native campaign family and case.");
            if (!Run(SourceRunId) || SourceRunId == runId || !Hash(SourceArtifactSha256) || !Hash(SourceArchiveSha256))
                errors.Add("The exact distinct successful two-owner acquired Manual receipt and native bytes are required.");
            if (!PairIds.All(Chunk7ManualDerivedSelection.NativeId) || PairIds.Distinct(StringComparer.Ordinal).Count() != 4)
                errors.Add("Four distinct observed native owner/pet IDs are required.");
            if (PartyIds == null || PartyIds.Length < 4 || PartyIds.Length > 12 ||
                !PartyIds.All(Chunk7ManualDerivedSelection.NativeId) || PartyIds.Distinct(StringComparer.Ordinal).Count() != PartyIds.Length ||
                PairIds.Any(id => !PartyIds.Contains(id))) errors.Add("The bounded full native field roster must contain both exact pairs once.");
            if (Persistence ? !SupportedCheckpoint(checkpoint) && !(Cold && (RepeatedCheckpoint(checkpoint) || OwnedSchema1Cold(checkpoint))) && (Cold || !SupportedSaveBoundary(checkpoint) && !OwnedSchema1Source(checkpoint)) : checkpoint != null)
                errors.Add("Owned-pair cold persistence requires a core checkpoint; the two save boundaries are source-only.");
            var area = checkpoint == "multi-area-autosave";
            if (area ? !Regex.IsMatch(SourceArea ?? "", "^[a-f0-9]{32}$") || SourceArea == new string('0', 32) : SourceArea != null)
                errors.Add("Only the owned area checkpoint requires its exact original acquired Manual area.");
            if (Cold ? !Run(CheckpointSourceRunId) || CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId ||
                !Hash(CheckpointSourceArtifactSha256) : CheckpointSourceRunId != null || CheckpointSourceArtifactSha256 != null)
                errors.Add("Only cold persistence consumes an exact separate successful owned-pair source checkpoint.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != LoadCount(checkpoint) || ExpectedNativeWriteCount != WriteCount(checkpoint))
                errors.Add("Owned pairs require base-only staging and the exact selected native load/write quotas.");
            return errors;
        }
        private static bool Run(string value) => Regex.IsMatch(value ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$");
        private static bool Hash(string value) => Regex.IsMatch(value ?? "", "^[a-f0-9]{64}$");
    }
}
