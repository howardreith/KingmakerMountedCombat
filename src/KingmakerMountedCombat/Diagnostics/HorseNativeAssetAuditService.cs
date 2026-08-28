using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class HorseNativeAssetAuditService
    {
        internal const string ScenarioName = "horse-native-asset-audit";
        internal const string EvidenceFileName = "horse-native-asset-audit.json";
        internal const string EvidenceKind = "horse-asset-audit";
        internal const string HorseBlueprintGuid = "9e9e75c484e68734487e609714565202";
        internal const string HorsePrefabGuid = "5e0b93738ad54dd4ba101b3513ac4590";
        internal const string SummonedPonyBlueprintGuid = "3f95557fc806db741b500a5735990841";
        internal const string SummonedPonyPrefabGuid = "447d2907feec82545b3773fbb4709588";
        internal const string MammothUnitGuid = "e7aa96d15a45238438ae4cfb476f6bb9";
        internal const string MammothFeatureGuid = "6adc3aab7cde56b40aa189a797254271";
        internal const string MammothUpgradeGuid = "6a23d16a4476af644af89d91f9f96790";

        private static readonly string[] ReservedKmcGuids =
        {
            "4016c7db400ab721ff125aef9e65e202",
            "7db7c50677e39f09feef56f3831fc723",
            "98e651899e6278d938de77af1d69bd32"
        };

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        public static RuntimeSubscenarioResult Run(RuntimeRequest request, IModLogger logger)
        {
            if (request == null) { throw new ArgumentNullException(nameof(request)); }
            if (logger == null) { throw new ArgumentNullException(nameof(logger)); }
            if (!string.Equals(request.Scenario, ScenarioName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Horse native-asset audit received a different scenario.");
            }

            var errors = new List<string>();
            var assertions = new JArray();
            var passed = 0;
            var failed = 0;
            var artifact = new JObject
            {
                ["schemaVersion"] = 2,
                ["evidenceKind"] = EvidenceKind,
                ["runId"] = request.RunId,
                ["scenario"] = request.Scenario,
                ["branch"] = request.Branch,
                ["commit"] = request.Commit,
                ["productVersion"] = request.ProductVersion,
                ["createdAtUtc"] = DateTimeOffset.UtcNow.ToString("o"),
                ["loadedBlueprintCount"] = 0,
                ["resourceNameCount"] = 0,
                ["reservedGuidCollisions"] = new JArray(),
                ["exactHorse"] = JValue.CreateNull(),
                ["ponyDiscovery"] = new JObject(),
                ["portraitDiscovery"] = new JObject(),
                ["stockCompanionBaseline"] = new JObject(),
                ["companionSelections"] = new JArray(),
                ["ranger"] = JValue.CreateNull(),
                ["paladin"] = JValue.CreateNull()
            };

            try
            {
                logger.Info("Horse native-asset audit started: enumerating the initialized Kingmaker blueprint library.");
                IDictionary resourceNames;
                var blueprints = LoadBlueprintLibrary(out resourceNames);
                artifact["loadedBlueprintCount"] = blueprints.Count;
                artifact["resourceNameCount"] = resourceNames == null ? 0 : resourceNames.Count;
                AddAssertion(assertions, errors, blueprints.Count > 0,
                    "blueprint-library-initialized", "The initialized blueprint library contains " + blueprints.Count + " entries.", ref passed, ref failed);

                var reservedCollisions = new JArray();
                foreach (var guid in ReservedKmcGuids)
                {
                    var collision = blueprints.FirstOrDefault(item => string.Equals(item.AssetGuid, guid, StringComparison.Ordinal));
                    reservedCollisions.Add(new JObject
                    {
                        ["assetGuid"] = guid,
                        ["resolved"] = collision != null,
                        ["blueprint"] = collision == null ? (JToken)JValue.CreateNull() : BlueprintIdentity(collision)
                    });
                }
                artifact["reservedGuidCollisions"] = reservedCollisions;
                AddAssertion(assertions, errors, reservedCollisions.All(item => item["resolved"] != null && !item["resolved"].Value<bool>()),
                    "reserved-kmc-guids-unclaimed", "All three deterministic KMC horse blueprint GUIDs are absent from the stock library.", ref passed, ref failed);

                var horse = blueprints.SingleOrDefault(item => string.Equals(item.AssetGuid, HorseBlueprintGuid, StringComparison.Ordinal));
                var horseRecord = horse == null ? null : BuildUnitRecord(horse, resourceNames);
                artifact["exactHorse"] = horseRecord == null ? (JToken)JValue.CreateNull() : horseRecord;
                AddAssertion(assertions, errors, horse != null,
                    "exact-native-horse-blueprint", "CR1_HorseRiding resolves by exact GUID.", ref passed, ref failed);
                AddAssertion(assertions, errors, horse != null && string.Equals(horse.Name, "CR1_HorseRiding", StringComparison.Ordinal),
                    "exact-native-horse-name", "The resolved native horse internal name is CR1_HorseRiding.", ref passed, ref failed);
                AddAssertion(assertions, errors, horseRecord != null && horseRecord["sizeValue"] != null && horseRecord["sizeValue"].Value<int>() == 5,
                    "exact-native-horse-large", "The stock horse BlueprintUnit size is Large (5).", ref passed, ref failed);
                AddAssertion(assertions, errors, horseRecord != null && string.Equals((string)horseRecord["prefabAssetId"], HorsePrefabGuid, StringComparison.Ordinal),
                    "exact-native-horse-prefab", "The stock horse uses the exact HorseRiding prefab asset ID.", ref passed, ref failed);

                var horseView = horseRecord == null ? null : horseRecord["view"] as JObject;
                AddAssertion(assertions, errors, horseView != null && ContainsExact(horseView["transformNames"] as JArray, "Chest") &&
                    ContainsExact(horseView["transformNames"] as JArray, "L_Stirrup") && ContainsExact(horseView["transformNames"] as JArray, "R_Stirrup"),
                    "horse-chest-and-stirrups", "The native horse view contains Chest, L_Stirrup, and R_Stirrup transforms.", ref passed, ref failed);
                AddAssertion(assertions, errors, horseView != null && ContainsTypeSuffix(horseView["componentTypes"] as JArray, ".UnitMovementAgent"),
                    "horse-stock-movement-agent", "The native horse view contains the stock UnitMovementAgent.", ref passed, ref failed);
                AddAssertion(assertions, errors, horseView != null && (horseView["colliders"] as JArray)?.Count > 0,
                    "horse-collider-contract", "The native horse view exposes collider metadata.", ref passed, ref failed);
                AddAssertion(assertions, errors, horseView != null && (horseView["animationActions"] as JArray)?.Count > 0,
                    "horse-animation-action-contract", "The native horse view exposes a Kingmaker animation action set.", ref passed, ref failed);

                logger.Info("Horse native-asset audit: resolving every horse/pony blueprint and resource-name candidate.");
                var resourceCandidates = BuildResourceCandidates(resourceNames);
                var candidatePrefabIds = new HashSet<string>(
                    resourceCandidates.Select(item => (string)item["assetId"]).Where(value => !string.IsNullOrEmpty(value)),
                    StringComparer.Ordinal);
                var unitCandidates = blueprints.Where(item => IsUnit(item.Value) &&
                    (ContainsHorseTerm(item.Name) || candidatePrefabIds.Contains(ReadPrefabAssetId(item.Value))))
                    .OrderBy(item => item.AssetGuid, StringComparer.Ordinal)
                    .ToList();
                var candidateRecords = new JArray(unitCandidates.Select(item => BuildUnitRecord(item, resourceNames)));
                var ponyCandidates = unitCandidates.Where(item => ContainsPonyTerm(item.Name) ||
                    ContainsPonyTerm(ReadResourceName(resourceNames, ReadPrefabAssetId(item.Value))) ||
                    ContainsPonyTerm(ReadLoadedViewRootName(item.Value))).ToList();
                var ponyRecords = new JArray(ponyCandidates.Select(item => BuildUnitRecord(item, resourceNames)));
                var summonedPony = ponyCandidates.SingleOrDefault(item =>
                    string.Equals(item.AssetGuid, SummonedPonyBlueprintGuid, StringComparison.Ordinal));
                var summonedPonyRecord = ponyRecords.Children<JObject>().SingleOrDefault(item =>
                    string.Equals((string)item["assetGuid"], SummonedPonyBlueprintGuid, StringComparison.Ordinal));

                var ponyDiscovery = new JObject
                {
                    ["resourceMatches"] = resourceCandidates,
                    ["candidateUnits"] = candidateRecords,
                    ["ponyCandidateUnits"] = ponyRecords
                };
                artifact["ponyDiscovery"] = ponyDiscovery;
                AddAssertion(assertions, errors, resourceCandidates.Count > 0,
                    "native-horse-or-pony-resources-found", "The stock resource catalog contains horse/pony paths.", ref passed, ref failed);
                AddAssertion(assertions, errors, summonedPony != null &&
                    string.Equals(summonedPony.Name, "PonySummoned", StringComparison.Ordinal) &&
                    string.Equals(ReadPrefabAssetId(summonedPony.Value), SummonedPonyPrefabGuid, StringComparison.Ordinal),
                    "summoned-pony-candidate-resolved", "PonySummoned resolves by exact GUID and uses the native Pony_02 prefab.", ref passed, ref failed);
                AddAssertion(assertions, errors, summonedPonyRecord != null && summonedPonyRecord["view"] is JObject,
                    "summoned-pony-view-resolved", "The exact PonySummoned native view loaded for structural comparison.", ref passed, ref failed);

                logger.Info("Horse native-asset audit: enumerating exact Horse/Pony portrait and icon owners.");
                var portraitDiscovery = BuildPortraitDiscovery(blueprints, unitCandidates, horse);
                artifact["portraitDiscovery"] = portraitDiscovery;
                AddAssertion(assertions, errors,
                    portraitDiscovery["exactNativeHorsePortrait"] == null ||
                    portraitDiscovery["exactNativeHorsePortrait"].Type == JTokenType.Null,
                    "exact-native-horse-portrait-absent",
                    "CR1_HorseRiding has no BlueprintPortrait; the current Mammoth portrait is a KMC fallback rather than native Horse art.",
                    ref passed, ref failed);
                AddAssertion(assertions, errors,
                    portraitDiscovery["blueprintPortraitCount"] != null &&
                    portraitDiscovery["blueprintPortraitCount"].Value<int>() > 0,
                    "native-portrait-search-complete",
                    "The initialized library portrait scan completed and recorded all Horse/Pony-named portrait, unit-owner, and icon-owner candidates.",
                    ref passed, ref failed);

                logger.Info("Horse native-asset audit: scanning bounded blueprint references for candidate ownership and summon contracts.");
                var referenceScanner = new BlueprintReferenceScanner(blueprints, logger);
                var reverseReferences = referenceScanner.FindReferences(ponyCandidates);
                ponyDiscovery["reverseReferences"] = reverseReferences;
                ponyDiscovery["reverseReferenceTruncated"] = referenceScanner.Truncated;
                AddAssertion(assertions, errors, ponyCandidates.Count > 0 && !referenceScanner.Truncated,
                    "pony-reference-scan-complete", "The bounded initialized-library reference scan completed without truncation and found " +
                    reverseReferences.Count + " exact owner/reference paths; zero is an explicit negative result.", ref passed, ref failed);

                var mammothFeature = blueprints.SingleOrDefault(item => string.Equals(item.AssetGuid, MammothFeatureGuid, StringComparison.Ordinal));
                var mammothUnit = blueprints.SingleOrDefault(item => string.Equals(item.AssetGuid, MammothUnitGuid, StringComparison.Ordinal));
                var mammothUpgrade = blueprints.SingleOrDefault(item => string.Equals(item.AssetGuid, MammothUpgradeGuid, StringComparison.Ordinal));
                var mammothPet = mammothFeature == null ? null : BuildAddPetRecord(mammothFeature);
                artifact["stockCompanionBaseline"] = new JObject
                {
                    ["feature"] = mammothFeature == null ? (JToken)JValue.CreateNull() : BlueprintIdentity(mammothFeature),
                    ["unit"] = mammothUnit == null ? (JToken)JValue.CreateNull() : BlueprintIdentity(mammothUnit),
                    ["upgrade"] = mammothUpgrade == null ? (JToken)JValue.CreateNull() : BlueprintIdentity(mammothUpgrade),
                    ["addPet"] = mammothPet == null ? (JToken)JValue.CreateNull() : mammothPet
                };
                AddAssertion(assertions, errors, mammothFeature != null && mammothUnit != null && mammothUpgrade != null && mammothPet != null,
                    "stock-mammoth-companion-trio", "The exact stock Mammoth feature, unit, upgrade, and AddPet contract resolve.", ref passed, ref failed);
                AddAssertion(assertions, errors, mammothPet != null &&
                    string.Equals((string)mammothPet.SelectToken("pet.assetGuid"), MammothUnitGuid, StringComparison.Ordinal) &&
                    string.Equals((string)mammothPet.SelectToken("upgradeFeature.assetGuid"), MammothUpgradeGuid, StringComparison.Ordinal) &&
                    mammothPet["upgradeLevel"] != null && mammothPet["upgradeLevel"].Value<int>() == 7,
                    "stock-mammoth-addpet-fields", "Stock Mammoth AddPet points to the exact unit/upgrade and applies the upgrade at rank 7.", ref passed, ref failed);

                logger.Info("Horse native-asset audit: resolving Ranger selection and Paladin level-5 class contracts.");
                var companionSelections = BuildCompanionSelections(blueprints);
                artifact["companionSelections"] = companionSelections;
                var rangerClasses = blueprints.Where(item => string.Equals(item.Name, "RangerClass", StringComparison.OrdinalIgnoreCase)).ToList();
                var paladinClasses = blueprints.Where(item => string.Equals(item.Name, "PaladinClass", StringComparison.OrdinalIgnoreCase)).ToList();
                artifact["ranger"] = BuildClassRecord(rangerClasses.SingleOrDefault(), companionSelections);
                artifact["paladin"] = BuildClassRecord(paladinClasses.SingleOrDefault(), companionSelections);
                AddAssertion(assertions, errors, rangerClasses.Count == 1,
                    "exact-ranger-class", "Exactly one RangerClass resolves in the stock library.", ref passed, ref failed);
                AddAssertion(assertions, errors, paladinClasses.Count == 1,
                    "exact-paladin-class", "Exactly one PaladinClass resolves in the stock library.", ref passed, ref failed);
                AddAssertion(assertions, errors, companionSelections.Count > 0,
                    "stock-companion-selections", "At least one exact stock feature selection contains AddPet companion options.", ref passed, ref failed);

                var rangerRecord = artifact["ranger"] as JObject;
                AddAssertion(assertions, errors, rangerRecord != null && (rangerRecord["progressionLevelEntries"] as JArray)?.Count > 0,
                    "ranger-progression-levels", "The Ranger class exposes exact progression level entries for selection correlation.", ref passed, ref failed);
                var paladinRecord = artifact["paladin"] as JObject;
                AddAssertion(assertions, errors, paladinRecord != null && HasLevel(paladinRecord["progressionLevelEntries"] as JArray, 5),
                    "paladin-level-five-contract", "The Paladin progression exposes an exact level-5 entry for Divine Bond design.", ref passed, ref failed);
            }
            catch (Exception exception)
            {
                var message = "Horse native-asset audit failed: " + exception.GetType().Name + ": " + exception.Message;
                errors.Add(message);
                assertions.Add(new JObject
                {
                    ["name"] = "audit-completed-without-exception",
                    ["status"] = "FAIL",
                    ["detail"] = message
                });
                failed++;
                logger.Exception("Horse native-asset audit", exception);
            }

            artifact["assertions"] = assertions;
            artifact["assertionPassCount"] = passed;
            artifact["assertionFailCount"] = failed;
            artifact["errors"] = new JArray(errors);
            artifact["status"] = failed == 0 ? "PASS" : "FAIL";
            WriteArtifact(request.EvidenceRoot, artifact);
            logger.Info("Horse native-asset audit completed with PASS=" + passed + " FAIL=" + failed + ".");

            return new RuntimeSubscenarioResult
            {
                Name = ScenarioName,
                Status = failed == 0 ? "PASS" : "FAIL",
                AssertionPassCount = passed,
                AssertionFailCount = failed,
                Errors = errors
            };
        }

        private static List<BlueprintEntry> LoadBlueprintLibrary(out IDictionary resourceNames)
        {
            var assembly = typeof(Kingmaker.Game).Assembly;
            var resourcesType = assembly.GetType("Kingmaker.Blueprints.ResourcesLibrary", true);
            var library = ReadStaticMember(resourcesType, "LibraryObject");
            if (library == null) { throw new InvalidOperationException("ResourcesLibrary.LibraryObject is null."); }
            var dictionary = ReadMember(library, "BlueprintsByAssetId") as IDictionary;
            resourceNames = ReadMember(library, "ResourceNamesByAssetId") as IDictionary;
            if (dictionary == null) { throw new InvalidOperationException("LibraryObject.BlueprintsByAssetId is unavailable."); }

            var result = new List<BlueprintEntry>(dictionary.Count);
            foreach (DictionaryEntry pair in dictionary)
            {
                if (pair.Value == null) { continue; }
                var guid = pair.Key as string ?? ReadStringMember(pair.Value, "AssetGuid");
                if (string.IsNullOrEmpty(guid)) { continue; }
                result.Add(new BlueprintEntry(guid, ReadObjectName(pair.Value), pair.Value));
            }
            return result.OrderBy(item => item.AssetGuid, StringComparer.Ordinal).ToList();
        }

        private static JObject BuildUnitRecord(BlueprintEntry entry, IDictionary resourceNames)
        {
            var unit = entry.Value;
            var prefabAssetId = ReadPrefabAssetId(unit);
            var record = BlueprintIdentity(entry);
            var size = ReadMember(unit, "Size");
            record["size"] = size == null ? (JToken)JValue.CreateNull() : new JValue(size.ToString());
            record["sizeValue"] = size == null ? -1 : Convert.ToInt32(size, CultureInfo.InvariantCulture);
            record["prefabAssetId"] = prefabAssetId ?? string.Empty;
            record["prefabResourceName"] = ReadResourceName(resourceNames, prefabAssetId) ?? string.Empty;
            record["strength"] = ReadInt(unit, "Strength");
            record["dexterity"] = ReadInt(unit, "Dexterity");
            record["constitution"] = ReadInt(unit, "Constitution");
            record["intelligence"] = ReadInt(unit, "Intelligence");
            record["wisdom"] = ReadInt(unit, "Wisdom");
            record["charisma"] = ReadInt(unit, "Charisma");
            var speed = ReadMember(unit, "Speed");
            record["speedFeet"] = speed == null ? -1 : ReadInt(speed, "Value");
            record["componentTypes"] = BuildComponentTypes(unit);
            record["body"] = BuildBodyRecord(ReadMember(unit, "Body"));
            try
            {
                record["view"] = BuildViewRecord(unit);
            }
            catch (Exception exception)
            {
                record["view"] = JValue.CreateNull();
                record["viewError"] = exception.GetType().Name + ": " + exception.Message;
            }
            return record;
        }

        private static JObject BuildViewRecord(object unit)
        {
            var view = LoadPrefabView(unit);

            var components = view.GetComponentsInChildren<Component>(true).Where(item => item != null).ToArray();
            var transforms = view.GetComponentsInChildren<Transform>(true).Where(item => item != null).ToArray();
            var componentTypes = new JArray(components.Select(item => item.GetType().FullName).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal));
            var transformNames = new JArray(transforms.Select(item => item.name).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal));
            var boneNames = new SortedSet<string>(StringComparer.Ordinal);
            var meshNames = new SortedSet<string>(StringComparer.Ordinal);
            var materialNames = new SortedSet<string>(StringComparer.Ordinal);
            var colliders = new JArray();
            var movementAgents = new JArray();
            var animationActions = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var component in components)
            {
                var typeName = component.GetType().FullName ?? string.Empty;
                if (typeName.EndsWith("Collider", StringComparison.Ordinal))
                {
                    colliders.Add(BuildColliderRecord(component, view.transform));
                }
                if (typeName.IndexOf("UnitMovementAgent", StringComparison.Ordinal) >= 0)
                {
                    movementAgents.Add(BuildMovementAgentRecord(component));
                }
                if (typeName.IndexOf("Renderer", StringComparison.Ordinal) >= 0)
                {
                    AddUnityObjectName(meshNames, ReadMember(component, "sharedMesh"));
                    var bones = ReadMember(component, "bones") as IEnumerable;
                    if (bones != null)
                    {
                        foreach (var bone in bones) { AddUnityObjectName(boneNames, bone); }
                    }
                    var materials = ReadMember(component, "sharedMaterials") as IEnumerable;
                    if (materials != null)
                    {
                        foreach (var material in materials) { AddUnityObjectName(materialNames, material); }
                    }
                }
                if (string.Equals(typeName, "Kingmaker.Visual.Animation.Kingmaker.UnitAnimationManager", StringComparison.Ordinal))
                {
                    var actionSet = ReadMember(component, "ActionSet") as IEnumerable;
                    if (actionSet != null)
                    {
                        foreach (var action in actionSet)
                        {
                            if (action == null) { continue; }
                            var type = ReadMember(action, "Type");
                            animationActions.Add((type == null ? "Unknown" : type.ToString()) + "|" + action.GetType().FullName);
                        }
                    }
                }
            }

            var animatorControllers = new JArray();
            var animationClips = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var animator in view.GetComponentsInChildren<Animator>(true).Where(item => item != null))
            {
                var controller = animator.runtimeAnimatorController;
                animatorControllers.Add(new JObject
                {
                    ["animatorPath"] = GetTransformPath(animator.transform, view.transform),
                    ["controllerName"] = controller == null ? string.Empty : controller.name,
                    ["controllerType"] = controller == null ? string.Empty : controller.GetType().FullName
                });
                if (controller != null)
                {
                    foreach (var clip in controller.animationClips ?? new AnimationClip[0])
                    {
                        if (clip != null) { animationClips.Add(clip.name); }
                    }
                }
            }

            return new JObject
            {
                ["rootName"] = view.name,
                ["viewType"] = view.GetType().FullName,
                ["rootLocalPosition"] = VectorToken(view.transform.localPosition),
                ["rootLocalRotation"] = QuaternionToken(view.transform.localRotation),
                ["rootLocalScale"] = VectorToken(view.transform.localScale),
                ["transformCount"] = transforms.Length,
                ["transformNames"] = transformNames,
                ["importantTransforms"] = new JArray(new[] { "HorseRiding_body_RIG", "LowerTorso", "UpperTorso", "Chest", "L_Stirrup", "R_Stirrup" }
                    .Select(name => TransformToken(FindTransform(view.transform, name), view.transform, name))),
                ["boneNames"] = new JArray(boneNames),
                ["meshNames"] = new JArray(meshNames),
                ["materialNames"] = new JArray(materialNames),
                ["componentTypes"] = componentTypes,
                ["colliders"] = colliders,
                ["movementAgents"] = movementAgents,
                ["animatorControllers"] = animatorControllers,
                ["animationClips"] = new JArray(animationClips),
                ["animationActions"] = new JArray(animationActions),
                ["viewCorpulence"] = NumericToken(ReadMember(view, "Corpulence")),
                ["selectionRelatedComponents"] = new JArray(components.Select(item => item.GetType().FullName)
                    .Where(name => name != null && (name.IndexOf("Selection", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Highlight", StringComparison.OrdinalIgnoreCase) >= 0))
                    .Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal))
            };
        }

        private static JObject BuildColliderRecord(Component collider, Transform root)
        {
            return new JObject
            {
                ["type"] = collider.GetType().FullName,
                ["transformPath"] = GetTransformPath(collider.transform, root),
                ["enabled"] = BooleanToken(ReadMember(collider, "enabled")),
                ["isTrigger"] = BooleanToken(ReadMember(collider, "isTrigger")),
                ["radius"] = NumericToken(ReadMember(collider, "radius")),
                ["height"] = NumericToken(ReadMember(collider, "height")),
                ["center"] = ValueToken(ReadMember(collider, "center")),
                ["size"] = ValueToken(ReadMember(collider, "size")),
                ["sharedMesh"] = UnityObjectToken(ReadMember(collider, "sharedMesh"))
            };
        }

        private static JObject BuildMovementAgentRecord(Component agent)
        {
            return new JObject
            {
                ["type"] = agent.GetType().FullName,
                ["enabled"] = BooleanToken(ReadMember(agent, "enabled")),
                ["acceleration"] = NumericToken(ReadMember(agent, "m_Acceleration")),
                ["minimumSpeed"] = NumericToken(ReadMember(agent, "m_MinSpeed")),
                ["angularSpeed"] = NumericToken(ReadMember(agent, "m_AngularSpeed")),
                ["combatAngularSpeed"] = NumericToken(ReadMember(agent, "m_CombatAngularSpeed"))
            };
        }

        private static JObject BuildBodyRecord(object body)
        {
            if (body == null) { return new JObject(); }
            return new JObject
            {
                ["disableHands"] = BooleanToken(ReadMember(body, "DisableHands")),
                ["emptyHandWeapon"] = BlueprintToken(ReadMember(body, "EmptyHandWeapon")),
                ["primaryHand"] = BlueprintToken(ReadMember(body, "PrimaryHand")),
                ["secondaryHand"] = BlueprintToken(ReadMember(body, "SecondaryHand")),
                ["additionalLimbs"] = BlueprintArrayToken(ReadMember(body, "AdditionalLimbs") as IEnumerable),
                ["additionalSecondaryLimbs"] = BlueprintArrayToken(ReadMember(body, "AdditionalSecondaryLimbs") as IEnumerable)
            };
        }

        private static JArray BuildComponentTypes(object blueprint)
        {
            var components = ReadMember(blueprint, "ComponentsArray") as IEnumerable;
            var names = new SortedSet<string>(StringComparer.Ordinal);
            if (components != null)
            {
                foreach (var component in components)
                {
                    if (component != null) { names.Add(component.GetType().FullName); }
                }
            }
            return new JArray(names);
        }

        private static JObject BuildAddPetRecord(BlueprintEntry feature)
        {
            var components = ReadMember(feature.Value, "ComponentsArray") as IEnumerable;
            if (components == null) { return null; }
            foreach (var component in components)
            {
                if (component == null || !string.Equals(component.GetType().FullName, "Kingmaker.UnitLogic.FactLogic.AddPet", StringComparison.Ordinal)) { continue; }
                return new JObject
                {
                    ["ownerFeature"] = BlueprintIdentity(feature),
                    ["pet"] = BlueprintToken(ReadMember(component, "Pet")),
                    ["levelRank"] = BlueprintToken(ReadMember(component, "LevelRank")),
                    ["upgradeFeature"] = BlueprintToken(ReadMember(component, "UpgradeFeature")),
                    ["upgradeLevel"] = ReadInt(component, "UpgradeLevel")
                };
            }
            return null;
        }

        private static JArray BuildResourceCandidates(IDictionary resourceNames)
        {
            var result = new List<JObject>();
            if (resourceNames != null)
            {
                foreach (DictionaryEntry pair in resourceNames)
                {
                    var assetId = pair.Key as string ?? string.Empty;
                    var resourceName = pair.Value as string ?? string.Empty;
                    if (!ContainsHorseTerm(resourceName)) { continue; }
                    result.Add(new JObject
                    {
                        ["assetId"] = assetId,
                        ["resourceName"] = resourceName,
                        ["horseTerm"] = resourceName.IndexOf("horse", StringComparison.OrdinalIgnoreCase) >= 0,
                        ["ponyTerm"] = resourceName.IndexOf("pony", StringComparison.OrdinalIgnoreCase) >= 0
                    });
                }
            }
            return new JArray(result.OrderBy(item => (string)item["assetId"], StringComparer.Ordinal));
        }

        private static JArray BuildCompanionSelections(IReadOnlyList<BlueprintEntry> blueprints)
        {
            var selections = new List<JObject>();
            foreach (var entry in blueprints.Where(item => string.Equals(item.Value.GetType().FullName,
                "Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection", StringComparison.Ordinal)))
            {
                var allFeatures = ReadMember(entry.Value, "AllFeatures") as IEnumerable;
                if (allFeatures == null) { continue; }
                var features = new JArray();
                var hasPet = false;
                foreach (var feature in allFeatures)
                {
                    if (feature == null) { continue; }
                    var child = BlueprintToken(feature) as JObject;
                    var addPet = BuildAddPetRecord(new BlueprintEntry(ReadStringMember(feature, "AssetGuid"), ReadObjectName(feature), feature));
                    if (addPet != null)
                    {
                        child["addPet"] = addPet;
                        hasPet = true;
                    }
                    features.Add(child);
                }
                if (!hasPet) { continue; }
                var record = BlueprintIdentity(entry);
                record["featureCount"] = features.Count;
                record["features"] = features;
                selections.Add(record);
            }
            return new JArray(selections.OrderBy(item => (string)item["assetGuid"], StringComparer.Ordinal));
        }

        private static JObject BuildPortraitDiscovery(
            IReadOnlyList<BlueprintEntry> blueprints,
            IReadOnlyList<BlueprintEntry> unitCandidates,
            BlueprintEntry exactHorse)
        {
            var portraits = blueprints.Where(item => string.Equals(
                    item.Value.GetType().FullName,
                    "Kingmaker.Blueprints.BlueprintPortrait",
                    StringComparison.Ordinal))
                .OrderBy(item => item.AssetGuid, StringComparer.Ordinal)
                .ToList();
            var namedPortraits = portraits.Where(item => ContainsHorseTerm(item.Name))
                .Select(BuildPortraitRecord)
                .ToList();

            var unitOwners = new JArray();
            foreach (var unit in unitCandidates.OrderBy(item => item.AssetGuid, StringComparer.Ordinal))
            {
                var portrait = ReadMember(unit.Value, "m_Portrait") ?? ReadMember(unit.Value, "Portrait");
                var owner = BlueprintIdentity(unit);
                owner["portrait"] = portrait == null ? (JToken)JValue.CreateNull() :
                    BuildPortraitRecord(new BlueprintEntry(
                        ReadStringMember(portrait, "AssetGuid"),
                        ReadObjectName(portrait),
                        portrait));
                unitOwners.Add(owner);
            }

            var iconOwners = new JArray();
            foreach (var owner in blueprints.Where(item => ContainsHorseTerm(item.Name))
                .OrderBy(item => item.AssetGuid, StringComparer.Ordinal))
            {
                var icon = ReadMember(owner.Value, "m_Icon") ?? ReadMember(owner.Value, "Icon");
                if (!(icon is Sprite)) { continue; }
                var record = BlueprintIdentity(owner);
                record["icon"] = SpriteToken(icon as Sprite);
                iconOwners.Add(record);
            }

            var exactPortrait = exactHorse == null
                ? null
                : ReadMember(exactHorse.Value, "m_Portrait") ?? ReadMember(exactHorse.Value, "Portrait");
            return new JObject
            {
                ["blueprintPortraitCount"] = portraits.Count,
                ["namedHorsePonyBlueprintPortraits"] = new JArray(namedPortraits),
                ["horsePonyUnitPortraitOwners"] = unitOwners,
                ["horsePonyIconOwners"] = iconOwners,
                ["exactNativeHorsePortrait"] = exactPortrait == null ? (JToken)JValue.CreateNull() :
                    BuildPortraitRecord(new BlueprintEntry(
                        ReadStringMember(exactPortrait, "AssetGuid"),
                        ReadObjectName(exactPortrait),
                        exactPortrait))
            };
        }

        private static JObject BuildPortraitRecord(BlueprintEntry portrait)
        {
            var result = BlueprintIdentity(portrait);
            var data = ReadMember(portrait.Value, "Data");
            result["dataPresent"] = data != null;
            result["small"] = SpriteToken(ReadMember(portrait.Value, "SmallPortrait") as Sprite);
            result["medium"] = SpriteToken(ReadMember(portrait.Value, "HalfLengthPortrait") as Sprite);
            result["large"] = SpriteToken(ReadMember(portrait.Value, "FullLengthPortrait") as Sprite);
            result["backupPortrait"] = BlueprintToken(ReadMember(portrait.Value, "BackupPortrait"));
            return result;
        }

        private static JToken SpriteToken(Sprite sprite)
        {
            if (sprite == null) { return JValue.CreateNull(); }
            return new JObject
            {
                ["name"] = sprite.name ?? string.Empty,
                ["width"] = sprite.texture == null ? 0 : sprite.texture.width,
                ["height"] = sprite.texture == null ? 0 : sprite.texture.height,
                ["textureName"] = sprite.texture == null ? string.Empty : sprite.texture.name ?? string.Empty,
                ["textureType"] = sprite.texture == null ? string.Empty : sprite.texture.GetType().FullName
            };
        }

        private static JObject BuildClassRecord(BlueprintEntry characterClass, JArray companionSelections)
        {
            if (characterClass == null) { return null; }
            var progression = ReadMember(characterClass.Value, "Progression");
            var levelEntries = new JArray();
            var entries = progression == null ? null : ReadMember(progression, "LevelEntries") as IEnumerable;
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry == null) { continue; }
                    var features = new JArray();
                    var values = ReadMember(entry, "Features") as IEnumerable;
                    if (values != null)
                    {
                        foreach (var feature in values)
                        {
                            if (feature == null) { continue; }
                            var token = BlueprintToken(feature) as JObject;
                            var matchingSelection = companionSelections.FirstOrDefault(selection =>
                                string.Equals((string)selection["assetGuid"], (string)token["assetGuid"], StringComparison.Ordinal));
                            if (matchingSelection != null) { token["companionSelection"] = true; }
                            features.Add(token);
                        }
                    }
                    levelEntries.Add(new JObject
                    {
                        ["level"] = ReadInt(entry, "Level"),
                        ["features"] = features
                    });
                }
            }
            return new JObject
            {
                ["class"] = BlueprintIdentity(characterClass),
                ["progression"] = BlueprintToken(progression),
                ["progressionLevelEntries"] = levelEntries
            };
        }

        private static JObject BlueprintIdentity(BlueprintEntry entry)
        {
            return new JObject
            {
                ["name"] = entry.Name ?? string.Empty,
                ["assetGuid"] = entry.AssetGuid ?? string.Empty,
                ["type"] = entry.Value == null ? string.Empty : entry.Value.GetType().FullName
            };
        }

        private static JToken BlueprintToken(object blueprint)
        {
            if (blueprint == null) { return JValue.CreateNull(); }
            return new JObject
            {
                ["name"] = ReadObjectName(blueprint),
                ["assetGuid"] = ReadStringMember(blueprint, "AssetGuid") ?? string.Empty,
                ["type"] = blueprint.GetType().FullName
            };
        }

        private static JArray BlueprintArrayToken(IEnumerable values)
        {
            var result = new JArray();
            if (values != null)
            {
                foreach (var value in values) { result.Add(BlueprintToken(value)); }
            }
            return result;
        }

        private static string ReadPrefabAssetId(object unit)
        {
            var prefab = ReadMember(unit, "Prefab");
            return prefab == null ? null : ReadStringMember(prefab, "AssetId");
        }

        private static string ReadLoadedViewRootName(object unit)
        {
            try
            {
                var view = LoadPrefabView(unit);
                return view == null ? null : view.name;
            }
            catch { return null; }
        }

        private static Component LoadPrefabView(object unit)
        {
            var prefab = ReadMember(unit, "Prefab");
            if (prefab == null) { throw new InvalidOperationException("BlueprintUnit.Prefab is null."); }
            var load = prefab.GetType().GetMethod("Load", BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeof(bool) }, null);
            if (load == null) { throw new MissingMethodException(prefab.GetType().FullName, "Load(Boolean)"); }
            var view = load.Invoke(prefab, new object[] { false }) as Component;
            if (view == null) { throw new InvalidOperationException("UnitViewLink.Load(false) returned no Component."); }
            return view;
        }

        private static string ReadResourceName(IDictionary names, string assetId)
        {
            if (names == null || string.IsNullOrEmpty(assetId) || !names.Contains(assetId)) { return null; }
            return names[assetId] as string;
        }

        private static bool IsUnit(object value)
        {
            return value != null && string.Equals(value.GetType().FullName, "Kingmaker.Blueprints.BlueprintUnit", StringComparison.Ordinal);
        }

        private static bool ContainsHorseTerm(string value)
        {
            return !string.IsNullOrEmpty(value) && (value.IndexOf("horse", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("pony", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool ContainsPonyTerm(string value)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf("pony", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsExact(JArray values, string expected)
        {
            return values != null && values.Any(value => string.Equals((string)value, expected, StringComparison.Ordinal));
        }

        private static bool ContainsTypeSuffix(JArray values, string suffix)
        {
            return values != null && values.Any(value => ((string)value ?? string.Empty).EndsWith(suffix, StringComparison.Ordinal));
        }

        private static bool HasLevel(JArray entries, int level)
        {
            return entries != null && entries.Any(entry => entry["level"] != null && entry["level"].Value<int>() == level);
        }

        private static void AddAssertion(JArray assertions, List<string> errors, bool condition, string name, string detail,
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

        private static object ReadStaticMember(Type type, string name)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property != null) { return property.GetValue(null, null); }
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null);
        }

        private static object ReadMember(object source, string name)
        {
            if (source == null || string.IsNullOrEmpty(name)) { return null; }
            for (var type = source.GetType(); type != null; type = type.BaseType)
            {
                var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    try { return property.GetValue(source, null); }
                    catch { return null; }
                }
                var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    try { return field.GetValue(source); }
                    catch { return null; }
                }
            }
            return null;
        }

        private static string ReadStringMember(object source, string name)
        {
            return ReadMember(source, name) as string;
        }

        private static int ReadInt(object source, string name)
        {
            var value = ReadMember(source, name);
            if (value == null) { return -1; }
            try { return Convert.ToInt32(value, CultureInfo.InvariantCulture); }
            catch { return -1; }
        }

        private static string ReadObjectName(object value)
        {
            var unityObject = value as UnityEngine.Object;
            if (unityObject != null) { return unityObject.name ?? string.Empty; }
            return ReadStringMember(value, "name") ?? ReadStringMember(value, "Name") ?? string.Empty;
        }

        private static JToken NumericToken(object value)
        {
            if (value == null) { return JValue.CreateNull(); }
            try { return new JValue(Convert.ToDouble(value, CultureInfo.InvariantCulture)); }
            catch { return JValue.CreateNull(); }
        }

        private static JToken BooleanToken(object value)
        {
            if (value == null) { return JValue.CreateNull(); }
            try { return new JValue(Convert.ToBoolean(value, CultureInfo.InvariantCulture)); }
            catch { return JValue.CreateNull(); }
        }

        private static JToken ValueToken(object value)
        {
            if (value == null) { return JValue.CreateNull(); }
            if (value is Vector2) { var item = (Vector2)value; return new JObject { ["x"] = item.x, ["y"] = item.y }; }
            if (value is Vector3) { return VectorToken((Vector3)value); }
            if (value is Vector4) { var item = (Vector4)value; return new JObject { ["x"] = item.x, ["y"] = item.y, ["z"] = item.z, ["w"] = item.w }; }
            if (value is Quaternion) { return QuaternionToken((Quaternion)value); }
            return value.GetType().IsEnum ? new JValue(value.ToString()) : NumericToken(value);
        }

        private static JToken UnityObjectToken(object value)
        {
            var unityObject = value as UnityEngine.Object;
            return unityObject == null ? (JToken)JValue.CreateNull() : new JObject
            {
                ["name"] = unityObject.name ?? string.Empty,
                ["type"] = unityObject.GetType().FullName
            };
        }

        private static JObject VectorToken(Vector3 value)
        {
            return new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z };
        }

        private static JObject QuaternionToken(Quaternion value)
        {
            return new JObject { ["x"] = value.x, ["y"] = value.y, ["z"] = value.z, ["w"] = value.w };
        }

        private static JObject TransformToken(Transform transform, Transform root, string expectedName)
        {
            if (transform == null)
            {
                return new JObject { ["expectedName"] = expectedName, ["found"] = false };
            }
            return new JObject
            {
                ["expectedName"] = expectedName,
                ["found"] = true,
                ["path"] = GetTransformPath(transform, root),
                ["parentName"] = transform.parent == null ? string.Empty : transform.parent.name,
                ["localPosition"] = VectorToken(transform.localPosition),
                ["localRotation"] = QuaternionToken(transform.localRotation),
                ["localScale"] = VectorToken(transform.localScale)
            };
        }

        private static Transform FindTransform(Transform root, string exactName)
        {
            if (root == null) { return null; }
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform != null && string.Equals(transform.name, exactName, StringComparison.Ordinal)) { return transform; }
            }
            return null;
        }

        private static string GetTransformPath(Transform transform, Transform root)
        {
            if (transform == null) { return string.Empty; }
            var segments = new List<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                segments.Add(current.name);
                if (current == root) { break; }
            }
            segments.Reverse();
            return string.Join("/", segments);
        }

        private static void AddUnityObjectName(ISet<string> target, object value)
        {
            var unityObject = value as UnityEngine.Object;
            if (unityObject != null && !string.IsNullOrEmpty(unityObject.name)) { target.Add(unityObject.name); }
        }

        private static void WriteArtifact(string evidenceRoot, JObject artifact)
        {
            var root = Path.GetFullPath(evidenceRoot).TrimEnd(Path.DirectorySeparatorChar);
            var path = Path.Combine(root, EvidenceFileName);
            if (!Directory.Exists(root)) { throw new DirectoryNotFoundException("Runtime evidence root is missing."); }
            if (File.Exists(path)) { throw new InvalidOperationException("Horse native-asset audit artifact already exists."); }
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

        private sealed class BlueprintEntry
        {
            public BlueprintEntry(string assetGuid, string name, object value)
            {
                AssetGuid = assetGuid;
                Name = name;
                Value = value;
            }

            public string AssetGuid { get; }
            public string Name { get; }
            public object Value { get; }
        }

        private sealed class BlueprintReferenceScanner
        {
            private const int MaxReferences = 500;
            private const int MaxDepth = 8;
            private const int MaxEnumerableItems = 4096;
            private readonly IReadOnlyList<BlueprintEntry> blueprints;
            private readonly IModLogger logger;
            private readonly Dictionary<Type, FieldInfo[]> fields = new Dictionary<Type, FieldInfo[]>();
            private HashSet<object> targets;
            private HashSet<string> targetGuids;
            private JArray results;

            public BlueprintReferenceScanner(IReadOnlyList<BlueprintEntry> blueprints, IModLogger logger)
            {
                this.blueprints = blueprints;
                this.logger = logger;
            }

            public bool Truncated { get; private set; }

            public JArray FindReferences(IReadOnlyList<BlueprintEntry> candidateTargets)
            {
                targets = new HashSet<object>(candidateTargets.Select(item => item.Value), ReferenceComparer.Instance);
                targetGuids = new HashSet<string>(candidateTargets.Select(item => item.AssetGuid), StringComparer.Ordinal);
                results = new JArray();
                Truncated = false;
                if (targets.Count == 0) { return results; }

                for (var index = 0; index < blueprints.Count && !Truncated; index++)
                {
                    var owner = blueprints[index];
                    if (targets.Contains(owner.Value)) { continue; }
                    var visited = new HashSet<object>(ReferenceComparer.Instance);
                    Scan(owner, owner.Value, "$", 0, true, visited);
                    if ((index + 1) % 5000 == 0)
                    {
                        logger.Info("Horse native-asset audit reference scan progress: " + (index + 1) + "/" + blueprints.Count + ".");
                    }
                }
                return new JArray(results.OrderBy(item => (string)item["ownerAssetGuid"], StringComparer.Ordinal)
                    .ThenBy(item => (string)item["path"], StringComparer.Ordinal));
            }

            private void Scan(BlueprintEntry owner, object value, string path, int depth, bool isRoot, HashSet<object> visited)
            {
                if (value == null || depth > MaxDepth || Truncated) { return; }
                if (targets.Contains(value))
                {
                    AddReference(owner, value, path, "object-reference");
                    return;
                }
                var text = value as string;
                if (text != null)
                {
                    if (targetGuids.Contains(text)) { AddReference(owner, value, path, "asset-guid-string"); }
                    return;
                }

                var type = value.GetType();
                if (IsTerminal(type)) { return; }
                if (!type.IsValueType && !visited.Add(value)) { return; }
                if (!isRoot && IsBlueprint(value)) { return; }
                if (value is UnityEngine.Object && !IsBlueprint(value)) { return; }

                var enumerable = value as IEnumerable;
                if (enumerable != null)
                {
                    var itemIndex = 0;
                    foreach (var item in enumerable)
                    {
                        if (itemIndex >= MaxEnumerableItems) { break; }
                        Scan(owner, item, path + "[" + itemIndex + "]", depth + 1, false, visited);
                        itemIndex++;
                        if (Truncated) { return; }
                    }
                    return;
                }

                foreach (var field in GetFields(type))
                {
                    object child;
                    try { child = field.GetValue(value); }
                    catch { continue; }
                    Scan(owner, child, path + "." + field.Name, depth + 1, false, visited);
                    if (Truncated) { return; }
                }
            }

            private void AddReference(BlueprintEntry owner, object target, string path, string referenceKind)
            {
                var targetEntry = targets.Contains(target)
                    ? blueprints.FirstOrDefault(item => ReferenceEquals(item.Value, target))
                    : null;
                results.Add(new JObject
                {
                    ["ownerName"] = owner.Name,
                    ["ownerAssetGuid"] = owner.AssetGuid,
                    ["ownerType"] = owner.Value.GetType().FullName,
                    ["targetName"] = targetEntry == null ? string.Empty : targetEntry.Name,
                    ["targetAssetGuid"] = targetEntry == null ? target as string ?? string.Empty : targetEntry.AssetGuid,
                    ["referenceKind"] = referenceKind,
                    ["path"] = path
                });
                if (results.Count >= MaxReferences) { Truncated = true; }
            }

            private FieldInfo[] GetFields(Type type)
            {
                FieldInfo[] result;
                if (fields.TryGetValue(type, out result)) { return result; }
                var values = new List<FieldInfo>();
                for (var current = type; current != null && current != typeof(object); current = current.BaseType)
                {
                    values.AddRange(current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Where(field => !field.IsStatic));
                }
                result = values.OrderBy(field => field.Name, StringComparer.Ordinal).ToArray();
                fields[type] = result;
                return result;
            }

            private static bool IsBlueprint(object value)
            {
                for (var type = value.GetType(); type != null; type = type.BaseType)
                {
                    if (string.Equals(type.FullName, "Kingmaker.Blueprints.BlueprintScriptableObject", StringComparison.Ordinal)) { return true; }
                }
                return false;
            }

            private static bool IsTerminal(Type type)
            {
                return type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(DateTime) ||
                    type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(Type) ||
                    typeof(MemberInfo).IsAssignableFrom(type) || typeof(Delegate).IsAssignableFrom(type);
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }

            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }
    }
}
