using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    internal enum Chunk7NativeProgressionCase { ExistingHorseLevel, DismissCustomCompanion, RespecHorse, RespecMammoth }

    public sealed class Chunk7ProgressionSelection
    {
        public const string Scenario = "chunk7-native-progression";
        public const string LevelCase = "horse-native-level";
        public const string DismissCase = "horse-native-dismiss";
        public const string RetrainHorseCase = "horse-native-retrain-horse";
        public const string RetrainMammothCase = "horse-native-retrain-mammoth";
        public const string OwnedLevelCase = "owned-horse-native-level";
        public const string OwnedDismissCase = "owned-horse-native-dismiss";
        public const string OwnedRetrainHorseCase = "owned-horse-native-retrain-horse";
        public const string OwnedRetrainMammothCase = "owned-horse-native-retrain-mammoth";
        public const string SourceLeaf = "Manual_300_KMC_C7_WORLD.zks";
        public const string SourceName = "KMC_C7_WORLD";
        public const string SourceEvidenceLeaf = "progression-source.json";
        public const string InventoryLeaf = "progression-inventory.json";
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string InventoryRunId { get; set; }
        public string InventoryArtifactSha256 { get; set; }
        public string ActorId { get; set; }
        public string PetId { get; set; }
        public string ControlActorId { get; set; }
        public string ControlPetId { get; set; }
        public string RaceGuid { get; set; }
        public string PresetGuid { get; set; }
        public string SkeletonName { get; set; }
        public string Gender { get; set; }
        public bool? LeftHanded { get; set; }
        public string BodySize { get; set; }
        public int MinimumRangerLevel { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        public string NativeWriteLeaf { get; set; }
        public Chunk7OwnedPairsSelection OwnedSource { get; set; }
        public bool ShouldSerializeOwnedSource() => OwnedSource != null;
        internal bool IsOwned => Case == OwnedLevelCase || Case == OwnedDismissCase || Case == OwnedRetrainHorseCase || Case == OwnedRetrainMammothCase;
        internal string OperationCase
        {
            get
            {
                switch (Case)
                {
                    case LevelCase: case OwnedLevelCase: return LevelCase;
                    case DismissCase: case OwnedDismissCase: return DismissCase;
                    case RetrainHorseCase: case OwnedRetrainHorseCase: return RetrainHorseCase;
                    case RetrainMammothCase: case OwnedRetrainMammothCase: return RetrainMammothCase;
                    default: return null;
                }
            }
        }
        internal bool WritesAutosave => OperationCase == LevelCase || IsRespec;
        internal bool IsRespec => OperationCase == RetrainHorseCase || OperationCase == RetrainMammothCase;
        internal Chunk7NativeProgressionCase NativeCase
        {
            get
            {
                switch (OperationCase)
                {
                    case LevelCase: return Chunk7NativeProgressionCase.ExistingHorseLevel;
                    case DismissCase: return Chunk7NativeProgressionCase.DismissCustomCompanion;
                    case RetrainHorseCase: return Chunk7NativeProgressionCase.RespecHorse;
                    case RetrainMammothCase: return Chunk7NativeProgressionCase.RespecMammoth;
                    default: throw new InvalidOperationException("Unknown native progression operation.");
                }
            }
        }

        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario || OperationCase == null)
                errors.Add("Native progression requires its exact closed scenario/case.");
            foreach (var sourceRun in new[] { SourceRunId, InventoryRunId })
                if (!Regex.IsMatch(sourceRun ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || sourceRun == runId)
                    errors.Add("Distinct completed native source and inventory runs are required.");
            foreach (var hash in new[] { SourceArtifactSha256, SourceArchiveSha256, InventoryArtifactSha256 })
                if (!Regex.IsMatch(hash ?? "", "^[a-f0-9]{64}$")) errors.Add("Exact native source, archive and inventory hashes are required.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in new[] { ActorId, PetId, ControlActorId, ControlPetId })
                if (!Guid.TryParseExact(id, "D", out var parsed) || parsed == Guid.Empty || parsed.ToString("D") != id || !ids.Add(id))
                    errors.Add("Four distinct exact native actor IDs are required.");
            if (IsOwned)
            {
                if (OwnedSource == null) errors.Add("Owned progression requires its exact nested mounted checkpoint source.");
                else
                {
                    errors.AddRange(OwnedSource.Validate(Chunk7OwnedPairsSelection.Scenario, runId, "multi-exploration-manual"));
                    if (OwnedSource.Case != "persistence-load" || OwnedSource.ExpectedNativeLoadCount != 1 || OwnedSource.ExpectedNativeWriteCount != 0 ||
                        SourceRunId != OwnedSource.CheckpointSourceRunId || SourceArtifactSha256 != OwnedSource.CheckpointSourceArtifactSha256 ||
                        InventoryRunId != OwnedSource.SourceRunId || InventoryArtifactSha256 != OwnedSource.SourceArtifactSha256)
                        errors.Add("Owned progression requires one read-only Manual exploration checkpoint and its exact acquired inventory ancestry.");
                    if (!((ActorId == OwnedSource.RiderAId && PetId == OwnedSource.MountAId && ControlActorId == OwnedSource.RiderBId && ControlPetId == OwnedSource.MountBId) ||
                        (ActorId == OwnedSource.RiderBId && PetId == OwnedSource.MountBId && ControlActorId == OwnedSource.RiderAId && ControlPetId == OwnedSource.MountAId)))
                        errors.Add("Owned progression target/control must be the exact two declared Horse pairs.");
                }
            }
            else
            {
                if (OwnedSource != null) errors.Add("Historical progression forbids an owned source selection.");
                if (ActorId != "d17c8fd0-6627-40c4-bf9a-024cf001c1f7" || ControlActorId != "b6628a77-4962-47a4-a17c-88d9836fc9d5" ||
                    ControlPetId != "d79a4f6c-b74e-4868-95bd-533899131acb")
                    errors.Add("This progression family selects only the qualified Manual_300 Horse target and Mammoth control.");
            }
            if (ExpectedNativeWriteCount != (WritesAutosave ? 1 : 0) || NativeWriteLeaf != (WritesAutosave ? "Auto_1.zks" : null))
                errors.Add("The selected native operation has an exact terminal autosave count and leaf.");
            if (WritesAutosave ? MinimumRangerLevel < 4 || MinimumRangerLevel > 20 : MinimumRangerLevel != 0)
                errors.Add("Native progression requires its bounded real Ranger level target, or no build plan for dismissal.");
            if (IsRespec)
            {
                if (!Regex.IsMatch(RaceGuid ?? "", "^[a-f0-9]{32}$") || !Regex.IsMatch(PresetGuid ?? "", "^[a-f0-9]{32}$") ||
                    string.IsNullOrWhiteSpace(SkeletonName) || SkeletonName.Length > 160 || BodySize != "Medium" ||
                    Gender != "Male" && Gender != "Female" || !LeftHanded.HasValue)
                    errors.Add("Retraining must preserve the explicit native Medium body observed in this exact source inventory.");
            }
            else if (RaceGuid != null || PresetGuid != null || SkeletonName != null || Gender != null || LeftHanded.HasValue || BodySize != null)
                errors.Add("Leveling or dismissal cannot declare body reconstruction choices.");
            return errors;
        }
        internal IReadOnlyList<string> ValidateOwnedDescriptor(RuntimeSaveDescriptor source, RuntimeFixtureIdentity fixture)
        {
            var errors = new List<string>();
            if (!IsOwned || OwnedSource == null || source == null || fixture?.Working == null || fixture.Baseline == null)
                errors.Add("Owned progression requires its exact separately qualified Manual source and protected fixture identities.");
            else
            {
                errors.AddRange(source.Validate("persistenceLoad", "KMC_C7_WORLD", "^Manual_[1-9][0-9]*_KMC_C7_WORLD\\.zks$"));
                var numberMatch = Regex.Match(source.FileName ?? "", "^Manual_([1-9][0-9]*)_KMC_C7_WORLD\\.zks$");
                if (!numberMatch.Success || !int.TryParse(numberMatch.Groups[1].Value, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var number) || number <= 0 ||
                    source.FileName != "Manual_" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_KMC_C7_WORLD.zks" ||
                    !Guid.TryParseExact(source.GameId, "D", out var campaign) || campaign == Guid.Empty || campaign.ToString("D") != source.GameId ||
                    source.GameId == fixture.Working.GameId)
                    errors.Add("Owned progression requires the canonical engine-minted campaign and positive native Manual number.");
                if (source.Sha256 != SourceArchiveSha256 || source.Sha256 == OwnedSource.SourceArchiveSha256 ||
                    source.Sha256 == fixture.Working.Sha256 || source.Sha256 == fixture.Baseline.Sha256)
                    errors.Add("Owned progression must load its exact fresh mounted checkpoint, not the unmounted ancestry or protected Working.");
            }
            return errors;
        }
    }
}
