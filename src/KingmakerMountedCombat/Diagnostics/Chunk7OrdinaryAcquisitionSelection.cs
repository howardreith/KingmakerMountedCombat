using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    // A closed diagnostic selection, never authority derived from a KMC prefix.
    // The guarded runner separately verifies and stages the exact native source
    // inventory bytes before the runtime may spend money or select a level.
    public sealed class Chunk7OrdinaryAcquisitionSelection
    {
        public const string Scenario = "chunk7-ordinary-acquisition";
        public const string PaidSmallCase = "paid-small-ranger-horse";
        public const string ExistingCase = "existing-ranger-horse";
        public const string InventoryLeaf = "ordinary-native-inventory.json";
        public const string AutoLeaf = "Auto_1.zks";
        public string Case { get; set; }
        public string InventoryRunId { get; set; }
        public string InventoryArtifactSha256 { get; set; }
        public string SmallRaceGuid { get; set; }
        public string SmallPresetGuid { get; set; }
        public string SkeletonName { get; set; }
        public string Gender { get; set; }
        public bool? LeftHanded { get; set; }
        public string ExistingActorId { get; set; }
        public int MinimumRangerLevel { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        public string NativeWriteLeaf { get; set; }

        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario) errors.Add("Ordinary acquisition selection requires its exact closed scenario.");
            if (Case != PaidSmallCase && Case != ExistingCase) errors.Add("Unknown ordinary acquisition case.");
            if (!Regex.IsMatch(InventoryRunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || InventoryRunId == runId)
                errors.Add("A distinct exact completed native inventory run is required.");
            if (!Regex.IsMatch(InventoryArtifactSha256 ?? "", "^[a-f0-9]{64}$")) errors.Add("An immutable native inventory artifact hash is required.");
            if (MinimumRangerLevel < 4 || MinimumRangerLevel > 20) errors.Add("Ordinary Ranger acquisition must select a bounded native level.");
            if (ExpectedNativeWriteCount != 1 || NativeWriteLeaf != AutoLeaf) errors.Add("Ordinary completion authorizes exactly one Auto_1 native write.");
            if (Case == PaidSmallCase)
            {
                if (!Regex.IsMatch(SmallRaceGuid ?? "", "^[a-f0-9]{32}$") || !Regex.IsMatch(SmallPresetGuid ?? "", "^[a-f0-9]{32}$"))
                    errors.Add("Observed exact native Small race and preset IDs are required.");
                if (string.IsNullOrWhiteSpace(SkeletonName) || SkeletonName.Length > 160) errors.Add("An exact observed Small skeleton name is required.");
                if (Gender != "Male" && Gender != "Female" || !LeftHanded.HasValue) errors.Add("Explicit observed native gender and handedness are required.");
                if (ExistingActorId != null) errors.Add("Paid acquisition cannot substitute an existing actor.");
            }
            else
            {
                Guid actor;
                if (!Guid.TryParseExact(ExistingActorId, "D", out actor) || actor == Guid.Empty || actor.ToString("D") != ExistingActorId)
                    errors.Add("Existing acquisition needs an exact canonical native actor ID.");
                if (SmallRaceGuid != null || SmallPresetGuid != null || SkeletonName != null || Gender != null || LeftHanded.HasValue)
                    errors.Add("Existing acquisition cannot alter its native body.");
            }
            return errors;
        }
    }
}
