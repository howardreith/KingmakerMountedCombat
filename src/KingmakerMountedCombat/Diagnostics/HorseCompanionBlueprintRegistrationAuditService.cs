using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.FactLogic;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class HorseCompanionBlueprintRegistrationAuditService
    {
        internal const string ScenarioName = "horse-companion-blueprint-registration";
        internal const string EvidenceFileName = "horse-companion-blueprint-registration.json";
        internal const string EvidenceKind = "horse-companion-blueprint-registration";
        private const string NativeHorsePrefabAssetId = "5e0b93738ad54dd4ba101b3513ac4590";

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        public static RuntimeSubscenarioResult Run(
            RuntimeRequest request,
            HorseCompanionBlueprintService service,
            IModLogger logger)
        {
            if (request == null) { throw new ArgumentNullException(nameof(request)); }
            if (service == null) { throw new ArgumentNullException(nameof(service)); }
            if (logger == null) { throw new ArgumentNullException(nameof(logger)); }
            if (!string.Equals(request.Scenario, ScenarioName, StringComparison.Ordinal) &&
                !string.Equals(request.Scenario, HorseCompanionUnmountedScenarioEngine.ScenarioName, StringComparison.Ordinal) &&
                !string.Equals(request.Scenario, HorseCompanionUnmountedScenarioEngine.MountedScenarioName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Horse companion registration audit received a different scenario.");
            }

            var errors = new List<string>();
            var assertions = new JArray();
            var passed = 0;
            var failed = 0;
            var selectionDisabled = false;
            var artifact = new JObject
            {
                ["schemaVersion"] = 1,
                ["evidenceKind"] = EvidenceKind,
                ["runId"] = request.RunId,
                ["scenario"] = request.Scenario,
                ["branch"] = request.Branch,
                ["commit"] = request.Commit,
                ["productVersion"] = request.ProductVersion,
                ["createdAtUtc"] = DateTimeOffset.UtcNow.ToString("o"),
                ["initial"] = JValue.CreateNull(),
                ["selectionDisabled"] = JValue.CreateNull(),
                ["selectionReenabled"] = JValue.CreateNull()
            };

            try
            {
                logger.Info("Horse companion blueprint registration audit started.");
                service.Update();
                var initial = service.CaptureSnapshot();
                artifact["initial"] = JObject.FromObject(initial, JsonSerializer.Create(JsonSettings));
                var library = ResourcesLibrary.LibraryObject;
                AddAssertion(assertions, errors,
                    initial.State == HorseCompanionBlueprintState.Registered && string.IsNullOrEmpty(initial.Failure),
                    "registration-state", "The production service reached Registered without a failure.", ref passed, ref failed);
                AddAssertion(assertions, errors,
                    library != null && library.BlueprintsByAssetId != null,
                    "initialized-blueprint-library", "The initialized blueprint dictionary is available.", ref passed, ref failed);

                BlueprintScriptableObject unitValue;
                BlueprintScriptableObject featureValue;
                BlueprintScriptableObject upgradeValue;
                var allBlueprints = library?.GetAllBlueprints();
                var exactDefinitions = library != null && library.BlueprintsByAssetId != null &&
                    library.BlueprintsByAssetId.TryGetValue(HorseCompanionBlueprintService.UnitGuid, out unitValue) &&
                    library.BlueprintsByAssetId.TryGetValue(HorseCompanionBlueprintService.FeatureGuid, out featureValue) &&
                    library.BlueprintsByAssetId.TryGetValue(HorseCompanionBlueprintService.UpgradeGuid, out upgradeValue) &&
                    ReferenceEquals(unitValue, service.HorseUnit) && ReferenceEquals(featureValue, service.HorseFeature) &&
                    ReferenceEquals(upgradeValue, service.HorseUpgrade) &&
                    CountExactBlueprint(allBlueprints, service.HorseUnit) == 1 &&
                    CountExactBlueprint(allBlueprints, service.HorseFeature) == 1 &&
                    CountExactBlueprint(allBlueprints, service.HorseUpgrade) == 1 &&
                    service.HorseFeature.DlcType == Kingmaker.Blueprints.Root.DlcType.None &&
                    service.HorseUpgrade.DlcType == Kingmaker.Blueprints.Root.DlcType.None;
                AddAssertion(assertions, errors, exactDefinitions,
                    "exact-library-identities", "All three reserved KMC GUIDs resolve once by exact reference in the dictionary and canonical list with base-game entitlement.", ref passed, ref failed);

                var horseUnit = service.HorseUnit;
                var horseFeature = service.HorseFeature;
                var horseUpgrade = service.HorseUpgrade;
                var addPet = horseFeature?.GetComponent<AddPet>();
                var classLevels = horseUnit?.GetComponent<AddClassLevels>();
                AddAssertion(assertions, errors,
                    addPet != null && ReferenceEquals(addPet.Pet, horseUnit) &&
                    string.Equals(addPet.LevelRank?.AssetGuid, HorseCompanionBlueprintService.LevelRankGuid, StringComparison.Ordinal) &&
                    ReferenceEquals(addPet.UpgradeFeature, horseUpgrade) && addPet.UpgradeLevel == 4,
                    "add-pet-contract", "AddPet owns the exact horse, rank, rank-4 upgrade, and KMC upgrade feature.", ref passed, ref failed);
                AddAssertion(assertions, errors,
                    classLevels != null && classLevels.CharacterClass != null && classLevels.Levels == 0 &&
                    initial.InitialClassLevels == 0 && initial.StockMammothInitialClassLevels == 0 &&
                    initial.StockDogInitialClassLevels == 0,
                    "companion-class-contract",
                    "The horse matches the exact stock Mammoth/Dog zero-level bootstrap; AddPet owns rank-driven runtime leveling.",
                    ref passed, ref failed);
                AddAssertion(assertions, errors,
                    horseUnit != null && horseUnit.Size == Size.Large && horseUnit.Speed.Value == 50 &&
                    string.Equals(horseUnit.Prefab?.AssetId, NativeHorsePrefabAssetId, StringComparison.Ordinal),
                    "native-view-size-speed", "The KMC horse is Large, speed 50, and uses the exact native HorseRiding prefab.", ref passed, ref failed);
                AddAssertion(assertions, errors,
                    horseUnit != null && horseUnit.Strength == 16 && horseUnit.Dexterity == 13 &&
                    horseUnit.Constitution == 15 && horseUnit.Intelligence == 2 &&
                    horseUnit.Wisdom == 12 && horseUnit.Charisma == 6,
                    "base-ability-scores", "The explicit horse base ability scores are 16/13/15/2/12/6.", ref passed, ref failed);

                var body = horseUnit?.Body;
                AddAssertion(assertions, errors,
                    body != null && body.DisableHands && body.EmptyHandWeapon == null &&
                    body.PrimaryHand == null && body.SecondaryHand == null &&
                    body.AdditionalLimbs != null && body.AdditionalLimbs.Length == 3 &&
                    body.AdditionalLimbs[0] != null &&
                    string.Equals(body.AdditionalLimbs[0].AssetGuid, initial.BiteGuid, StringComparison.Ordinal) &&
                    string.Equals(body.AdditionalLimbs[0].name, "Bite1d4", StringComparison.Ordinal) &&
                    body.AdditionalLimbs.Skip(1).All(item => item != null &&
                        string.Equals(item.AssetGuid, HorseCompanionBlueprintService.HoofGuid, StringComparison.Ordinal)) &&
                    body.AdditionalSecondaryLimbs != null && body.AdditionalSecondaryLimbs.Length == 0,
                    "natural-attack-loadout", "The horse uses the stock no-hands animal topology with one Bite1d4 followed by two exact Hoof1d4 natural limbs.", ref passed, ref failed);

                var bonuses = horseUpgrade?.ComponentsArray?.OfType<AddStatBonus>().ToArray() ?? new AddStatBonus[0];
                AddAssertion(assertions, errors,
                    bonuses.Length == 2 && HasExactBonus(bonuses, StatType.Strength, 2) &&
                    HasExactBonus(bonuses, StatType.Constitution, 2),
                    "rank-four-upgrade", "The original rank-4 upgrade contains only +2 Strength and +2 Constitution racial bonuses.", ref passed, ref failed);
                AddAssertion(assertions, errors, HasExactLocalization(),
                    "localization-contract", "All five KMC horse localization keys resolve to their exact owned text, including the distinct Animal Companion — Horse selection label.", ref passed, ref failed);

                var ranger = library.BlueprintsByAssetId[HorseCompanionBlueprintService.RangerSelectionGuid] as BlueprintFeatureSelection;
                var untouchedFeaturesReference = ranger?.Features;
                var untouchedFeaturesItems = ranger?.Features == null ? null : ranger.Features.ToArray();
                AddAssertion(assertions, errors,
                    ranger != null && FeaturesStayedUntouched(
                        ranger.Features, untouchedFeaturesReference, untouchedFeaturesItems, horseFeature) &&
                    HasExactHorseAppend(ranger.AllFeatures, horseFeature) && initial.RangerAppendOwned &&
                    ranger.Items.Count(item => ReferenceEquals(item.Feature, horseFeature)) == 1,
                    "ranger-append", "The live Features surface remains byte-for-byte untouched; Items-authoritative AllFeatures preserves seven stock options and appends one eligible Horse at index 7.", ref passed, ref failed);

                var disableSucceeded = service.SetSelectionEnabled(false);
                selectionDisabled = disableSucceeded;
                var disabled = service.CaptureSnapshot();
                artifact["selectionDisabled"] = JObject.FromObject(disabled, JsonSerializer.Create(JsonSettings));
                AddAssertion(assertions, errors,
                    disableSucceeded && FeaturesStayedUntouched(
                        ranger.Features, untouchedFeaturesReference, untouchedFeaturesItems, horseFeature) &&
                    HasExactStockRestore(ranger.AllFeatures, horseFeature) &&
                    !ranger.Items.Any(item => ReferenceEquals(item.Feature, horseFeature)),
                    "exact-disable-restore", "Disabling the AllFeatures lease restores the exact seven stock options without touching Features or leaving an eligible Horse residue.", ref passed, ref failed);

                var enableSucceeded = service.SetSelectionEnabled(true);
                selectionDisabled = !enableSucceeded;
                var reenabled = service.CaptureSnapshot();
                artifact["selectionReenabled"] = JObject.FromObject(reenabled, JsonSerializer.Create(JsonSettings));
                AddAssertion(assertions, errors,
                    enableSucceeded && FeaturesStayedUntouched(
                        ranger.Features, untouchedFeaturesReference, untouchedFeaturesItems, horseFeature) &&
                    HasExactHorseAppend(ranger.AllFeatures, horseFeature) &&
                    ranger.Items.Count(item => ReferenceEquals(item.Feature, horseFeature)) == 1 &&
                    reenabled.RangerAppendOwned,
                    "exact-reenable-append", "Re-enabling restores the reference-exact AllFeatures append and one eligible Horse item with no duplicate while Features remains untouched.", ref passed, ref failed);
            }
            catch (Exception exception)
            {
                var message = "Horse companion blueprint registration audit failed: " + exception.GetType().Name + ": " + exception.Message;
                errors.Add(message);
                assertions.Add(new JObject
                {
                    ["name"] = "audit-completed-without-exception",
                    ["status"] = "FAIL",
                    ["detail"] = message
                });
                failed++;
                logger.Exception("Horse companion blueprint registration audit", exception);
            }
            finally
            {
                if (selectionDisabled)
                {
                    var recovered = service.SetSelectionEnabled(true);
                    AddAssertion(assertions, errors, recovered,
                        "finally-selection-recovery", "The Ranger selection append was restored in the audit finally path.", ref passed, ref failed);
                }
            }

            artifact["assertions"] = assertions;
            artifact["assertionPassCount"] = passed;
            artifact["assertionFailCount"] = failed;
            artifact["errors"] = new JArray(errors);
            artifact["status"] = failed == 0 ? "PASS" : "FAIL";
            WriteArtifact(request.EvidenceRoot, artifact);
            logger.Info("Horse companion blueprint registration audit completed with PASS=" + passed + " FAIL=" + failed + ".");
            return new RuntimeSubscenarioResult
            {
                Name = ScenarioName,
                Status = failed == 0 ? "PASS" : "FAIL",
                AssertionPassCount = passed,
                AssertionFailCount = failed,
                Errors = errors
            };
        }

        private static bool HasExactBonus(IEnumerable<AddStatBonus> bonuses, StatType stat, int value)
        {
            return bonuses.Count(item => item.Stat == stat && item.Value == value &&
                item.Descriptor == ModifierDescriptor.Racial && !item.ScaleByBasicAttackBonus) == 1;
        }

        private static bool HasExactLocalization()
        {
            var strings = LocalizationManager.CurrentPack?.Strings;
            if (strings == null) { return false; }
            return HasExactString(strings, "KMC.Horse.Name", "Horse") &&
                HasExactString(strings, "KMC.Horse.Feature.Name", "Animal Companion — Horse") &&
                HasExactString(strings, "KMC.Horse.Description", "A Large native Kingmaker horse animal companion. It is fully controllable while unmounted and can be used by KMC's private-alpha mounted profile.") &&
                HasExactString(strings, "KMC.Horse.Upgrade.Name", "Horse Animal Companion Advancement") &&
                HasExactString(strings, "KMC.Horse.Upgrade.Description", "At animal-companion rank 4, the horse gains +2 Strength and +2 Constitution.");
        }

        private static bool HasExactString(IDictionary<string, string> strings, string key, string expected)
        {
            string value;
            return strings.TryGetValue(key, out value) && string.Equals(value, expected, StringComparison.Ordinal);
        }

        private static int CountExactBlueprint(
            IEnumerable<BlueprintScriptableObject> values,
            BlueprintScriptableObject expected)
        {
            if (values == null || expected == null) { return 0; }
            return values.Count(item => ReferenceEquals(item, expected) &&
                string.Equals(item.AssetGuid, expected.AssetGuid, StringComparison.Ordinal));
        }

        private static bool HasExactHorseAppend(BlueprintFeature[] values, BlueprintFeature horseFeature)
        {
            return values != null && values.Length == 8 && ReferenceEquals(values[7], horseFeature) &&
                values.Count(item => ReferenceEquals(item, horseFeature)) == 1;
        }

        private static bool HasExactStockRestore(BlueprintFeature[] values, BlueprintFeature horseFeature)
        {
            return values != null && values.Length == 7 &&
                !values.Any(item => ReferenceEquals(item, horseFeature));
        }

        private static bool FeaturesStayedUntouched(
            BlueprintFeature[] current,
            BlueprintFeature[] originalReference,
            BlueprintFeature[] originalItems,
            BlueprintFeature horseFeature)
        {
            if (!ReferenceEquals(current, originalReference)) { return false; }
            if (current == null || originalItems == null)
            {
                return current == null && originalItems == null;
            }
            if (current.Length != originalItems.Length || current.Any(item => ReferenceEquals(item, horseFeature)))
            {
                return false;
            }
            for (var index = 0; index < current.Length; index++)
            {
                if (!ReferenceEquals(current[index], originalItems[index])) { return false; }
            }
            return true;
        }

        private static void AddAssertion(JArray assertions, IList<string> errors, bool condition, string name, string detail,
            ref int passed, ref int failed)
        {
            assertions.Add(new JObject
            {
                ["name"] = name,
                ["status"] = condition ? "PASS" : "FAIL",
                ["detail"] = detail
            });
            if (condition) { passed++; }
            else { failed++; errors.Add(name + ": " + detail); }
        }

        private static void WriteArtifact(string evidenceRoot, JObject artifact)
        {
            var root = Path.GetFullPath(evidenceRoot).TrimEnd(Path.DirectorySeparatorChar);
            var path = Path.Combine(root, EvidenceFileName);
            if (!Directory.Exists(root)) { throw new DirectoryNotFoundException("Runtime evidence root is missing."); }
            if (File.Exists(path)) { throw new InvalidOperationException("Horse companion blueprint registration artifact already exists."); }
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonConvert.SerializeObject(artifact, JsonSettings), new UTF8Encoding(false));
                File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) { File.Delete(temporary); }
            }
        }
    }
}
