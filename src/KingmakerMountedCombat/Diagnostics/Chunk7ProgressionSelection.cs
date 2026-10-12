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
        internal bool WritesAutosave => Case != DismissCase;
        internal bool IsRespec => Case == RetrainHorseCase || Case == RetrainMammothCase;
        internal Chunk7NativeProgressionCase NativeCase => Case == LevelCase ? Chunk7NativeProgressionCase.ExistingHorseLevel :
            Case == DismissCase ? Chunk7NativeProgressionCase.DismissCustomCompanion : Case == RetrainHorseCase ?
            Chunk7NativeProgressionCase.RespecHorse : Chunk7NativeProgressionCase.RespecMammoth;

        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario || Case != LevelCase && Case != DismissCase && Case != RetrainHorseCase && Case != RetrainMammothCase)
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
            if (ActorId != "d17c8fd0-6627-40c4-bf9a-024cf001c1f7" || ControlActorId != "b6628a77-4962-47a4-a17c-88d9836fc9d5" ||
                ControlPetId != "d79a4f6c-b74e-4868-95bd-533899131acb")
                errors.Add("This progression family selects only the qualified Manual_300 Horse target and Mammoth control.");
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
    }
}
