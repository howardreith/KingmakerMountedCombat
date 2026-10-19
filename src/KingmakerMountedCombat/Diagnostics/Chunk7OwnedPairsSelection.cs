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
        // Owner-supplied seed kind: two eligible Medium riders whose Horses are
        // created by disclosed native fixture setup in the disposable run copy.
        // It carries seed pins and never the producer receipt or pet pins.
        public const string OwnerSeedCase = "owner-seed-exploration";
        // Owner-seed-derived persistence producer: the unchanged disclosed setup, both
        // native Mounts, then exactly one genuine native Manual into the run root. Its
        // ancestry is owner-seed-derived with disclosed assistance, never acquired or
        // ordinary, and its output name differs from every acquired-ancestry Manual.
        public const string OwnerSeedManualCase = "owner-seed-persistence-save";
        // The separate-process cold consumer of exactly that Manual, pinned by the
        // producer run, its world evidence and the written Manual bytes.
        public const string OwnerSeedLoadCase = "owner-seed-persistence-load";
        public const string OwnerSeedManualName = "KMC_C7_SEED_WORLD";
        internal const string OwnerSeedCheckpoint = "multi-exploration-manual";
        // Owner-seed-derived combat (batch 3 scenarios 3-5): a disposable copy of the
        // owner-seed-derived Manual, cold-loaded with both pairs restored mounted. The
        // producer pins are Source*, the ACCEPTED cold consumer receipt is Checkpoint*.
        public const string OwnerSeedRealtimeCase = "owner-seed-realtime-core";
        public const string OwnerSeedRiderFirstCase = "owner-seed-combat-rider-first";
        public const string OwnerSeedMountFirstCase = "owner-seed-combat-mount-first";
        public const string OwnerSeedDisableCase = "owner-seed-combat-disable";
        public const string OwnerSeedLoadRefusalCase = "owner-seed-load-refusal";
        internal const string OwnerSeedLoadRefusalLeaf = "Manual_812_KMC_C7_SEED_REFUSAL.zks";
        internal static bool LoadRefusalCheckpoint(string value) => value == "multi-malformed-refusal-manual" || value == "multi-future-refusal-manual";
        // Owner-seed checkpoint producer (Directive 4): a disposable copy of the same accepted
        // Manual, cold-loaded with both pairs restored mounted, writes one genuine native
        // checkpoint through the generic persistence machinery. Source* is the Manual producer,
        // Checkpoint* the ACCEPTED cold consumer, exactly as derived combat.
        public const string OwnerSeedCheckpointSaveCase = "owner-seed-checkpoint-save";
        // Its separate-process cold consumer: Source* keeps the owner-seed Manual ancestry,
        // Checkpoint* is the checkpoint producer run and its persistence evidence.
        public const string OwnerSeedCheckpointLoadCase = "owner-seed-checkpoint-load";
        // The checkpoints the owner-seed checkpoint pair admits; extend this one list as each
        // further family is connected.
        internal static readonly string[] OwnerSeedCheckpoints = { "multi-exploration-quick", "multi-exploration-auto",
            "multi-rt-debt-manual", "multi-tb-debt-manual", "zero-mounted-tb-retained-manual", "legacy-mounted-pending-tb-manual", "multi-area-autosave" };
        internal static bool IsOwnerSeedSourceOnlyCheckpoint(string checkpoint) => SupportedSaveBoundary(checkpoint) || OwnedSchema1Source(checkpoint);
        // The connected combat checkpoints: genuine native Manuals written mid-combat from the restored
        // two-pair start under the owner-approved diagnostic target. Their native Manual name is distinct
        // from every acquired-ancestry KMC_C7_WORLD and from the derived KMC_C7_SEED_WORLD input.
        public const string OwnerSeedCheckpointManualName = "KMC_C7_SEED_CHECKPOINT";
        internal static bool IsOwnerSeedCombatCheckpoint(string checkpoint) =>
            checkpoint == "multi-rt-debt-manual" || checkpoint == "multi-tb-debt-manual" || checkpoint == "zero-mounted-tb-retained-manual" ||
            checkpoint == "legacy-mounted-pending-tb-manual";
        // The native Manual name a writing owned selection reserves; a non-Manual selection writes its slot name.
        internal string OwnedManualOutputName(string checkpoint) => IsOwnerSeedManual ? OwnerSeedManualName :
            IsOwnerSeedCheckpointSave ? OwnerSeedCheckpointManualName : Cold ? (OwnedSchema1Cold(checkpoint) ? "KMC_C7_SCHEMA1" : "KMC_C7_SCHEMA2") : "KMC_C7_WORLD";
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string CheckpointSourceRunId { get; set; }
        public string CheckpointSourceArtifactSha256 { get; set; }
        public string SeedId { get; set; }
        public string SeedSha256 { get; set; }
        public string RiderAId { get; set; }
        public string MountAId { get; set; }
        public string RiderBId { get; set; }
        public string MountBId { get; set; }
        public string[] PartyIds { get; set; }
        public Chunk7OwnerSeedXpAssistance XpAssistance { get; set; }
        public string SourceArea { get; set; }
        // Scenario 3 only: the unrelated companion whose one ordinary native action is observed.
        public string CompanionId { get; set; }
        // Keep every historical serialized selection byte shape unchanged.
        public bool ShouldSerializeSourceArea() => SourceArea != null;
        public bool ShouldSerializeCompanionId() => CompanionId != null;
        public bool ShouldSerializeSourceRunId() => !OwnerSeedSetup || SourceRunId != null;
        public bool ShouldSerializeSourceArtifactSha256() => !OwnerSeedSetup || SourceArtifactSha256 != null;
        public bool ShouldSerializeSourceArchiveSha256() => !OwnerSeedSetup || SourceArchiveSha256 != null;
        public bool ShouldSerializeCheckpointSourceRunId() => !OwnerSeedSetup && !IsOwnerSeedDerivedLoad || CheckpointSourceRunId != null;
        public bool ShouldSerializeCheckpointSourceArtifactSha256() => !OwnerSeedSetup && !IsOwnerSeedDerivedLoad || CheckpointSourceArtifactSha256 != null;
        public bool ShouldSerializeMountAId() => !OwnerSeedSetup || MountAId != null;
        public bool ShouldSerializeMountBId() => !OwnerSeedSetup || MountBId != null;
        public bool ShouldSerializeSeedId() => SeedId != null;
        public bool ShouldSerializeSeedSha256() => SeedSha256 != null;
        // The owner-seed shape always names its assistance key (null when rider B needs none).
        public bool ShouldSerializeXpAssistance() => OwnerSeedSetup || XpAssistance != null;
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        internal bool IsOwnerSeed => Case == OwnerSeedCase;
        internal bool IsOwnerSeedManual => Case == OwnerSeedManualCase;
        internal bool IsOwnerSeedDerivedLoad => Case == OwnerSeedLoadCase;
        internal bool IsOwnerSeedLoadRefusal => Case == OwnerSeedLoadRefusalCase;
        // Both kinds that load the owner's seed and run its disclosed native setup.
        internal bool OwnerSeedSetup => IsOwnerSeed || IsOwnerSeedManual;
        internal bool IsOwnerSeedDerivedCombat => Case == OwnerSeedRealtimeCase || Case == OwnerSeedRiderFirstCase || Case == OwnerSeedMountFirstCase || Case == OwnerSeedDisableCase;
        internal bool IsOwnerSeedCheckpointSave => Case == OwnerSeedCheckpointSaveCase;
        internal bool IsOwnerSeedCheckpointLoad => Case == OwnerSeedCheckpointLoadCase;
        // Every kind whose native input is the owner-seed-derived Manual itself.
        internal bool OwnerSeedDerivedInput => IsOwnerSeedDerivedLoad || IsOwnerSeedDerivedCombat || IsOwnerSeedCheckpointSave || IsOwnerSeedLoadRefusal;
        // Every kind whose staged source receipt is the owner-seed Manual producer's world evidence.
        internal bool OwnerSeedDerivedLineage => OwnerSeedDerivedInput || IsOwnerSeedCheckpointLoad;
        // Kinds whose receipts may come from an earlier candidate, each pinned to its own recorded identity.
        internal bool CrossCandidateAllowed => IsOwnerSeedDerivedCombat || IsOwnerSeedCheckpointSave || IsOwnerSeedCheckpointLoad || IsOwnerSeedLoadRefusal;
        internal bool CrossCandidateAllowedFor(string checkpoint) => CrossCandidateAllowed || IsOwnerSeedDerivedLoad && checkpoint == "multi-exploration-three-loads-manual";
        internal const int MaximumSetupLevelupWrites = 2;
        internal string[] PairIds => new[] { RiderAId, MountAId, RiderBId, MountBId };
        internal string RuntimeScenario
        {
            get
            {
                switch (Case)
                {
                    case "exploration": return "chunk7-two-pair-exploration";
                    case OwnerSeedCase: return "chunk7-two-pair-exploration";
                    case OwnerSeedManualCase: return "chunk7-two-pair-exploration";
                    case OwnerSeedLoadCase: return "chunk7-persistence-load";
                    case OwnerSeedLoadRefusalCase: return "chunk7-persistence-load";
                    case OwnerSeedRealtimeCase: return "chunk7-two-pair-realtime-core";
                    case OwnerSeedRiderFirstCase: return "chunk7-two-pair-combat-rider-first";
                    case OwnerSeedMountFirstCase: return "chunk7-two-pair-combat-mount-first";
                    case OwnerSeedDisableCase: return "chunk7-two-pair-combat-rider-first";
                    case OwnerSeedCheckpointSaveCase: return "chunk7-persistence-save";
                    case OwnerSeedCheckpointLoadCase: return "chunk7-persistence-load";
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
        internal bool Persistence => Case == "persistence-save" || Case == "persistence-load" || IsOwnerSeedManual || IsOwnerSeedDerivedLoad ||
            IsOwnerSeedCheckpointSave || IsOwnerSeedCheckpointLoad || IsOwnerSeedLoadRefusal;
        internal bool Cold => Case == "persistence-load" || IsOwnerSeedDerivedLoad || IsOwnerSeedCheckpointLoad || IsOwnerSeedLoadRefusal;
        internal static bool SupportedCheckpoint(string value) => Array.IndexOf(new[] {
            "multi-exploration-manual", "multi-exploration-quick", "multi-exploration-auto",
            "multi-rt-debt-manual", "multi-tb-debt-manual", "zero-mounted-tb-retained-manual",
            "legacy-mounted-pending-tb-manual", "multi-area-autosave" }, value) >= 0;
        internal static bool OwnedSchema1Source(string value) => value == "owned-schema1-manual";
        internal static bool OwnedSchema1Cold(string value) => value == "owned-schema1-resave-manual";
        internal static bool CurrentMigrationCheckpoint(string value) => value == "owned-schema1-current-manual" ||
            value == "owned-schema2-pending-current-manual";
        internal static bool IsOwnerSeedLoadOnlyCheckpoint(string value) => OwnedSchema1Cold(value) || CurrentMigrationCheckpoint(value) ||
            value == "pending-schema3-control-manual";
        internal static bool SupportedSaveBoundary(string value) => value == "multi-nonselected-mount-unsettled-manual" ||
            value == "multi-owned-worker-cancel-manual";
        internal static bool RepeatedCheckpoint(string value) => value == "multi-exploration-three-loads-manual" ||
            value == "multi-exploration-three-area-transfers-manual";
        internal int LoadCount(string checkpoint) => Cold && checkpoint == "multi-exploration-three-loads-manual" ? 3 : 1;
        // Owner seed: exactly the native setup level-up Autos its selection declares (bound at admission).
        // Owner-seed Manual producer: exactly its one measured Manual (no setup write exists).
        internal int WriteCount(string checkpoint) => IsOwnerSeed ? ExpectedNativeWriteCount : IsOwnerSeedManual ? 1 :
            IsOwnerSeedCheckpointSave ? (checkpoint == "multi-owned-worker-cancel-manual" ? 2 : 1) :
            IsOwnerSeedCheckpointLoad ? (checkpoint == "legacy-mounted-pending-tb-manual" || OwnedSchema1Cold(checkpoint) ? 1 : 0) :
            Case == "persistence-save" && checkpoint == "multi-owned-worker-cancel-manual" ? 2 : Case == "persistence-save" ||
            Cold && (checkpoint == "legacy-mounted-pending-tb-manual" || OwnedSchema1Cold(checkpoint)) ? 1 : 0;
        public IReadOnlyList<string> Validate(string scenario, string runId, string checkpoint)
        {
            if (OwnerSeedSetup) return ValidateOwnerSeed(scenario, runId, checkpoint);
            if (IsOwnerSeedDerivedLoad) return ValidateOwnerSeedDerivedLoad(scenario, runId, checkpoint);
            if (IsOwnerSeedLoadRefusal)
            {
                var refusalErrors = DerivedInputErrors(scenario, runId);
                if (!LoadRefusalCheckpoint(checkpoint) || CompanionId != null || !Run(CheckpointSourceRunId) ||
                    CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId || CheckpointSourceRunId == SeedId ||
                    !Hash(CheckpointSourceArtifactSha256) || CheckpointSourceArtifactSha256 == SourceArtifactSha256 ||
                    CheckpointSourceArtifactSha256 == SourceArchiveSha256 || CheckpointSourceArtifactSha256 == SeedSha256)
                    refusalErrors.Add("Load refusal requires only its exact selected checkpoint and distinct accepted owner-seed cold-consumer pins.");
                return refusalErrors;
            }
            if (IsOwnerSeedDerivedCombat) return ValidateOwnerSeedDerivedCombat(scenario, runId, checkpoint);
            if (IsOwnerSeedCheckpointSave || IsOwnerSeedCheckpointLoad) return ValidateOwnerSeedCheckpoint(scenario, runId, checkpoint);
            var errors = new List<string>();
            if (CompanionId != null) errors.Add("Only owner-seed-derived RT combat names an unrelated companion.");
            if (SeedId != null || SeedSha256 != null || XpAssistance != null)
                errors.Add("Producer-backed owned pairs cannot carry owner-seed pins or setup assistance.");
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
        // Static owner-seed validation. Rider size, petless state, eligibility and the
        // exact minimal XP target are re-checked against the loaded native actors by
        // Chunk7OwnerSeedSetupPlan before any setup mutation.
        private IReadOnlyList<string> ValidateOwnerSeed(string scenario, string runId, string checkpoint)
        {
            var errors = new List<string>();
            if (scenario != Scenario) errors.Add("Owner-seed two-pair exploration requires its exact closed native campaign family.");
            if (SourceRunId != null || SourceArtifactSha256 != null || SourceArchiveSha256 != null || CheckpointSourceRunId != null ||
                CheckpointSourceArtifactSha256 != null || MountAId != null || MountBId != null || SourceArea != null || CompanionId != null)
                errors.Add("Owner-seed two-pair exploration has no producer, checkpoint, area, companion or pre-granted pet pins.");
            if (!Run(SeedId) || SeedId == runId) errors.Add("An exact registered owner seed identity is required.");
            if (!Hash(SeedSha256)) errors.Add("An exact owner seed SHA256 is required.");
            if (!Chunk7ManualDerivedSelection.NativeId(RiderAId) || !Chunk7ManualDerivedSelection.NativeId(RiderBId) ||
                string.Equals(RiderAId, RiderBId, StringComparison.Ordinal))
                errors.Add("Two distinct observed native rider IDs are required.");
            if (PartyIds == null || PartyIds.Length < 2 || PartyIds.Length > 6 ||
                !PartyIds.All(Chunk7ManualDerivedSelection.NativeId) || PartyIds.Distinct(StringComparer.Ordinal).Count() != PartyIds.Length ||
                !PartyIds.Contains(RiderAId) || !PartyIds.Contains(RiderBId))
                errors.Add("The bounded full native party must contain both selected riders once.");
            if (XpAssistance != null && (XpAssistance.UnitId != RiderBId || XpAssistance.TargetCharacterLevel < 2 ||
                XpAssistance.TargetCharacterLevel > Chunk7OwnerSeedSetupPlan.MaximumTargetLevel))
                errors.Add("Native XP assistance may target only rider B, within the bounded setup ceiling.");
            if (IsOwnerSeedManual)
            {
                // The producer's only write is its one measured native Manual after SETUP-COMPLETE.
                if (checkpoint != OwnerSeedCheckpoint) errors.Add("Owner-seed-derived Manual production selects exactly the multi-exploration-manual checkpoint.");
                if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 1)
                    errors.Add("Owner-seed-derived Manual production requires base-only staging, one native load and exactly one native Manual write.");
                return errors;
            }
            if (checkpoint != null) errors.Add("Owner-seed exploration cannot select a persistence checkpoint.");
            // Writes are only the native setup level-up Autos: at most one per rider build,
            // bound exactly to the native autosave observation before any mutation.
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount < 0 || ExpectedNativeWriteCount > MaximumSetupLevelupWrites)
                errors.Add("Owner-seed exploration requires base-only staging, one native load and only its bounded setup level-up Auto writes.");
            return errors;
        }
        // The cold consumer of one owner-seed-derived Manual: its producer run, world
        // evidence and written bytes are exact pins; the owner's seed identity is carried
        // only to prove the derived Manual is not the immutable master. No setup, no
        // checkpoint parent, one load and zero writes.
        private IReadOnlyList<string> ValidateOwnerSeedDerivedLoad(string scenario, string runId, string checkpoint)
        {
            var errors = DerivedInputErrors(scenario, runId, 0, checkpoint);
            if (CheckpointSourceRunId != null || CheckpointSourceArtifactSha256 != null)
                errors.Add("The owner-seed-derived consumer carries no checkpoint parent.");
            if (CompanionId != null) errors.Add("Only owner-seed-derived RT combat names an unrelated companion.");
            if (checkpoint != OwnerSeedCheckpoint && checkpoint != "multi-exploration-three-loads-manual") errors.Add("Owner-seed-derived cold load restores only its exploration Manual through the selected one-load or three-load case.");
            return errors;
        }
        // Owner-seed-derived combat: the same producer and seed pins plus the ACCEPTED
        // separate-process consumer receipt (Checkpoint*) that cold-loaded the same Manual.
        // The combat runs may use a later candidate; the runner pins both receipts by
        // their own recorded candidate identity. One load, zero writes, no checkpoint.
        private IReadOnlyList<string> ValidateOwnerSeedDerivedCombat(string scenario, string runId, string checkpoint)
        {
            var errors = DerivedInputErrors(scenario, runId, 0, checkpoint);
            if (!Run(CheckpointSourceRunId) || CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId || CheckpointSourceRunId == SeedId ||
                !Hash(CheckpointSourceArtifactSha256) || CheckpointSourceArtifactSha256 == SourceArtifactSha256 ||
                CheckpointSourceArtifactSha256 == SourceArchiveSha256 || CheckpointSourceArtifactSha256 == SeedSha256)
                errors.Add("Owner-seed-derived combat requires the exact distinct accepted cold-consumer run and its evidence SHA256.");
            if (Case == OwnerSeedRealtimeCase
                ? !Chunk7ManualDerivedSelection.NativeId(CompanionId) || PartyIds == null || !PartyIds.Contains(CompanionId) || PairIds.Contains(CompanionId)
                : CompanionId != null)
                errors.Add("Owner-seed-derived RT combat names exactly one unrelated party companion; the TB cases name none.");
            if (checkpoint != null) errors.Add("Owner-seed-derived combat selects no persistence checkpoint.");
            return errors;
        }
        // Owner-seed checkpoint pair. The producer cold-loads the accepted Manual (Source*, plus the
        // ACCEPTED consumer receipt as Checkpoint*) and writes exactly one selected native checkpoint;
        // the consumer cold-loads that checkpoint (Checkpoint* = the producer run and its evidence).
        private IReadOnlyList<string> ValidateOwnerSeedCheckpoint(string scenario, string runId, string checkpoint)
        {
            var errors = DerivedInputErrors(scenario, runId, WriteCount(checkpoint), checkpoint);
            if (!Run(CheckpointSourceRunId) || CheckpointSourceRunId == runId || CheckpointSourceRunId == SourceRunId || CheckpointSourceRunId == SeedId ||
                !Hash(CheckpointSourceArtifactSha256) || CheckpointSourceArtifactSha256 == SourceArtifactSha256 ||
                CheckpointSourceArtifactSha256 == SourceArchiveSha256 || CheckpointSourceArtifactSha256 == SeedSha256)
                errors.Add(IsOwnerSeedCheckpointSave ? "The owner-seed checkpoint producer requires the exact distinct accepted cold-consumer run and its evidence SHA256." :
                    "The owner-seed checkpoint consumer requires the exact distinct checkpoint producer run and its persistence evidence SHA256.");
            if (CompanionId != null) errors.Add("Only owner-seed-derived RT combat names an unrelated companion.");
            if (Array.IndexOf(OwnerSeedCheckpoints, checkpoint) < 0 &&
                !(IsOwnerSeedCheckpointSave && IsOwnerSeedSourceOnlyCheckpoint(checkpoint)) &&
                !(IsOwnerSeedCheckpointLoad && IsOwnerSeedLoadOnlyCheckpoint(checkpoint)))
                errors.Add("The owner-seed checkpoint pair admits only its connected checkpoints: " + string.Join(", ", OwnerSeedCheckpoints) + ".");
            return errors;
        }
        private List<string> DerivedInputErrors(string scenario, string runId, int writes = 0, string checkpoint = null)
        {
            var errors = new List<string>();
            if (scenario != Scenario) errors.Add("Owner-seed-derived cold load requires its exact closed native campaign family.");
            if (!Run(SourceRunId) || SourceRunId == runId || SourceRunId == SeedId || !Hash(SourceArtifactSha256) || !Hash(SourceArchiveSha256))
                errors.Add("The exact distinct successful owner-seed-derived Manual producer run, world evidence and written Manual bytes are required.");
            if (!Run(SeedId) || SeedId == runId || !Hash(SeedSha256) || SeedSha256 == SourceArchiveSha256 || SeedSha256 == SourceArtifactSha256)
                errors.Add("The owner's immutable seed identity is required and cannot alias the derived Manual or its evidence.");
            var area = (IsOwnerSeedCheckpointSave || IsOwnerSeedCheckpointLoad) && checkpoint == "multi-area-autosave";
            if (XpAssistance != null || (area ? SourceArea != "b10e86516b6437d45a629931c30936fe" : SourceArea != null))
                errors.Add("Only the selected owner-seed area checkpoint carries its exact original Manual area; setup assistance is forbidden.");
            if (!PairIds.All(Chunk7ManualDerivedSelection.NativeId) || PairIds.Distinct(StringComparer.Ordinal).Count() != 4)
                errors.Add("Four distinct observed native owner/pet IDs are required.");
            if (PartyIds == null || PartyIds.Length < 4 || PartyIds.Length > 12 ||
                !PartyIds.All(Chunk7ManualDerivedSelection.NativeId) || PartyIds.Distinct(StringComparer.Ordinal).Count() != PartyIds.Length ||
                PairIds.Any(id => !PartyIds.Contains(id))) errors.Add("The bounded full native field roster must contain both exact pairs once.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != LoadCount(checkpoint) || ExpectedNativeWriteCount != writes)
                errors.Add(writes == 0 ? "Owner-seed-derived cold load requires base-only staging, one native load and zero native writes." :
                    "The owner-seed checkpoint producer requires base-only staging, one native load and exactly one native write.");
            return errors;
        }
        private static bool Run(string value) => Regex.IsMatch(value ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$");
        private static bool Hash(string value) => Regex.IsMatch(value ?? "", "^[a-f0-9]{64}$");
    }

    public sealed class Chunk7OwnerSeedXpAssistance
    {
        public string UnitId { get; set; }
        public int TargetCharacterLevel { get; set; }
    }

    // Observed native facts for one selected rider, read before any setup mutation.
    internal sealed class Chunk7OwnerSeedRiderFacts
    {
        internal string Id { get; set; }
        internal string Size { get; set; }
        internal string OriginalSize { get; set; }
        internal bool InParty { get; set; }
        internal bool Controllable { get; set; }
        internal bool IsPet { get; set; }
        internal bool HasPet { get; set; }
        internal bool HasDualCompanion { get; set; }
        internal bool InCombat { get; set; }
        internal bool Idle { get; set; }
        internal bool Polymorphed { get; set; }
        internal bool HasHorseRank { get; set; }
        internal int CharacterLevel { get; set; }
        internal int RangerLevel { get; set; }
        internal int Experience { get; set; }
        internal int ReadyLevel { get; set; }
        internal int ExperienceRate { get; set; }
    }

    // Pure owner-seed setup planning under the ordinary build driver's own Ranger
    // eligibility rule. A refusal is a SETUP failure before any mutation, never a
    // behaviour result.
    internal static class Chunk7OwnerSeedSetupPlan
    {
        internal const int RequiredRangerLevel = 4, MaximumTargetLevel = 8;
        // Chunk7OrdinaryBuildDriver: rangerLevel + readyLevel(xp) - characterLevel >= 4.
        internal static bool EligibleWithoutAssistance(Chunk7OwnerSeedRiderFacts rider) =>
            rider.RangerLevel + rider.ReadyLevel - rider.CharacterLevel >= RequiredRangerLevel;
        // The ordinary build levels to the XP-ready level: Ranger must land on exactly 4.
        internal static bool ExactlyRangerFour(Chunk7OwnerSeedRiderFacts rider) =>
            rider.RangerLevel + rider.ReadyLevel - rider.CharacterLevel == RequiredRangerLevel;
        internal static int MinimalTargetLevel(Chunk7OwnerSeedRiderFacts rider) =>
            rider.CharacterLevel + RequiredRangerLevel - rider.RangerLevel;
        internal static IReadOnlyList<string> RiderErrors(Chunk7OwnerSeedRiderFacts rider, string role)
        {
            var errors = new List<string>();
            if (rider == null) { errors.Add("Rider " + role + " is absent from the loaded native party."); return errors; }
            if (!rider.InParty) errors.Add("Rider " + role + " is not in the exact loaded native party.");
            if (rider.Size != "Medium" || rider.OriginalSize != "Medium" || rider.Polymorphed)
                errors.Add("Rider " + role + " is not an unpolymorphed Medium body; only medium-humanoid-horse-v1 is supported.");
            if (!rider.Controllable || rider.IsPet || rider.InCombat || !rider.Idle)
                errors.Add("Rider " + role + " is not a controllable idle non-pet actor outside combat.");
            if (rider.HasPet || rider.HasDualCompanion || rider.HasHorseRank)
                errors.Add("Rider " + role + " already owns a pet, a dual companion or Horse progression; the fixture requires a petless actor.");
            if (rider.CharacterLevel < 1 || rider.RangerLevel < 0 || rider.RangerLevel > rider.CharacterLevel || rider.ReadyLevel < rider.CharacterLevel)
                errors.Add("Rider " + role + " native progression observation is inconsistent.");
            return errors;
        }
        // The installed game (2.1.7b) has no level-up autosave: GameModesFactory builds its
        // controllers explicitly and never constructs or registers LevelUpAutoSaveController
        // (no reference anywhere in Assembly-CSharp, no reflective registration), so nothing
        // handles ILevelUpCompleteUIHandler.HandleLevelUpComplete with a save. Observed natively
        // in run 20261017-c7b-world19-ownerseed-b2s1-twopair (completionEntries=0,
        // saveRequests=0, preparedWrites=0). Setup expects zero writes for every setting.
        internal static int ExpectedSetupWrites(bool autosaveEnabled, int riderBuilds) => 0;
        internal static IReadOnlyList<string> WriteErrors(Chunk7OwnedPairsSelection selection, bool autosaveEnabled, bool onlyOneSave, int riderBuilds)
        {
            var errors = new List<string>();
            if (onlyOneSave) errors.Add("Native OnlyOneSave/Ironman is on; its single-slot saves cannot be run-owned setup level-up Autos.");
            if (riderBuilds < 0 || riderBuilds > Chunk7OwnedPairsSelection.MaximumSetupLevelupWrites) errors.Add("Owner-seed rider build count is outside its bound.");
            var expected = ExpectedSetupWrites(autosaveEnabled, riderBuilds);
            // The Manual producer's selected write is its measured Manual, never a setup write.
            var selectedSetupWrites = selection == null ? -1 : SelectedSetupWrites(selection);
            if (selection == null || selectedSetupWrites != expected)
                errors.Add("Selected native setup write count " + (selection == null ? "null" : selectedSetupWrites.ToString()) +
                    " must be " + expected + ": the native game has no level-up autosave (LevelUpAutoSaveController is never registered), whatever the autosave setting (" +
                    (autosaveEnabled ? "on" : "off") + ", " + riderBuilds + " rider builds).");
            return errors;
        }
        internal static int SelectedSetupWrites(Chunk7OwnedPairsSelection selection) =>
            selection.IsOwnerSeedManual ? selection.ExpectedNativeWriteCount - 1 : selection.ExpectedNativeWriteCount;
        internal static IReadOnlyList<string> AssistanceErrors(Chunk7OwnedPairsSelection selection, Chunk7OwnerSeedRiderFacts riderA, Chunk7OwnerSeedRiderFacts riderB)
        {
            var errors = new List<string>();
            if (selection == null || !selection.OwnerSeedSetup || riderA == null || riderB == null) { errors.Add("Owner-seed setup plan lacks its selection or riders."); return errors; }
            if (riderA.Id != selection.RiderAId || riderB.Id != selection.RiderBId) errors.Add("Observed riders differ from the selected rider roles.");
            if (!EligibleWithoutAssistance(riderA))
                errors.Add("Rider A is not eligible for Ranger 4 / Hunter's Bond from its own native XP; no assistance is authorized for rider A.");
            else if (!ExactlyRangerFour(riderA)) errors.Add("Rider A's native XP would carry it beyond Ranger 4; extra levels are refused.");
            var assistance = selection.XpAssistance;
            if (EligibleWithoutAssistance(riderB))
            {
                if (assistance != null) errors.Add("Rider B is already eligible; XP assistance beyond what eligibility requires is refused.");
                else if (!ExactlyRangerFour(riderB)) errors.Add("Rider B's native XP would carry it beyond Ranger 4; extra levels are refused.");
                return errors;
            }
            if (riderB.ReadyLevel != riderB.CharacterLevel) errors.Add("Assisted rider B must start inside its current level's XP band (no pending level).");
            var minimal = MinimalTargetLevel(riderB);
            if (assistance == null) errors.Add("Rider B needs native XP to character level " + minimal + " but no assistance was selected.");
            else if (assistance.UnitId != riderB.Id) errors.Add("XP assistance names an actor other than rider B.");
            else if (assistance.TargetCharacterLevel != minimal)
                errors.Add("XP assistance target " + assistance.TargetCharacterLevel + " differs from the exact minimal eligible level " + minimal + ".");
            if (minimal > MaximumTargetLevel) errors.Add("Rider B would need more than the bounded setup ceiling of character level " + MaximumTargetLevel + ".");
            if (riderB.ExperienceRate != 100) errors.Add("Exact native XP assistance requires ExperienceRatePercent 100.");
            return errors;
        }
    }

    // One item observation: its blueprint, stack count and the unit whose slot holds it
    // (null for the shared, unequipped stash).
    internal sealed class Chunk7OwnerSeedItemRow
    {
        internal string Blueprint { get; set; }
        internal int Count { get; set; }
        internal string HolderId { get; set; }
    }

    // The conservative money/item digest, identical before setup and at SETUP-COMPLETE:
    // EVERY row of the shared Player.Inventory with its holder id (null = unheld stash),
    // plus the fixed pre-setup party characters' equipped slots. Any item added, removed
    // or moved between holders in the shared collection changes it. A companion pet keeps
    // its own collection, so the setup Horses' items are disclosed separately.
    internal static class Chunk7OwnerSeedEconomyDigest
    {
        internal static string[] Rows(IEnumerable<Chunk7OwnerSeedItemRow> sharedInventory, IEnumerable<Chunk7OwnerSeedItemRow> equippedSlots,
            ICollection<string> characterIds)
        {
            var rows = new List<string>();
            foreach (var item in sharedInventory ?? Enumerable.Empty<Chunk7OwnerSeedItemRow>())
                if (item != null) rows.Add("shared|" + (item.HolderId ?? "-") + "|" + item.Blueprint + "|" + item.Count);
            foreach (var item in equippedSlots ?? Enumerable.Empty<Chunk7OwnerSeedItemRow>())
                if (item != null && characterIds.Contains(item.HolderId)) rows.Add("slot|" + item.HolderId + "|" + item.Blueprint + "|" + item.Count);
            return rows.OrderBy(row => row, StringComparer.Ordinal).ToArray();
        }
    }

    // The SETUP-COMPLETE boundary. Every assistance item is recorded while it is open;
    // after Complete any further setup mutation is refused and counted.
    internal sealed class Chunk7OwnerSeedSetupLedger
    {
        internal sealed class Item
        {
            internal string UnitId, Kind, NativeApi, Detail;
            internal long Before, After, Amount;
        }
        private readonly List<Item> items = new List<Item>();
        internal bool Completed { get; private set; }
        internal int RefusedAfterBoundary { get; private set; }
        internal int CompletedFrame { get; private set; } = -1;
        internal IReadOnlyList<Item> Items => items;
        internal void Record(string unitId, string kind, string nativeApi, long before, long after, long amount, string detail)
        {
            RequireOpen(kind);
            if (string.IsNullOrEmpty(unitId) || string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(nativeApi))
                throw new InvalidOperationException("Setup assistance must name its unit, kind and native API.");
            items.Add(new Item { UnitId = unitId, Kind = kind, NativeApi = nativeApi, Before = before, After = after, Amount = amount, Detail = detail });
        }
        internal void RequireOpen(string operation)
        {
            if (!Completed) return;
            RefusedAfterBoundary++;
            throw new InvalidOperationException("Setup mutation refused after SETUP-COMPLETE: " + operation + ".");
        }
        internal void Complete(int frame)
        {
            if (Completed) throw new InvalidOperationException("SETUP-COMPLETE may be crossed only once.");
            Completed = true; CompletedFrame = frame;
        }
    }
}
