using System;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7SmallFixtureScenario
    {
        private sealed class SmallInputPin { internal string Path, Sha256; internal long Length, Ticks; }
        private long qualifiedSeedMoney;
        private JObject ReadInput(string leaf, string hash, long maximum)
        {
            var path = Path.Combine(Path.GetDirectoryName(authority.Root), leaf); var file = new FileInfo(path);
            Require(file.Exists && file.Length > 0 && file.Length <= maximum && (file.Attributes & FileAttributes.ReparsePoint) == 0,
                "F09 staged provenance file is absent, oversized or indirect: " + leaf);
            var digest = Hash(path); Require(hash == null || digest == hash, "F09 exact provenance bytes differ: " + leaf);
            var result = JObject.Parse(File.ReadAllText(path));
            inputPins.Add(new SmallInputPin { Path = path, Sha256 = digest, Length = file.Length, Ticks = file.LastWriteTimeUtc.Ticks });
            return result;
        }
        private void ReadInputs()
        {
            var source = request.PersistenceLoad; var path = Path.Combine(authority.Root, source.FileName); var file = new FileInfo(path);
            Require(file.Exists && file.Length == source.Length && file.LastWriteTimeUtc.Ticks == source.LastWriteTimeUtcTicks &&
                (file.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(path) == source.Sha256,
                "F09 source must be the unchanged ordinary run copy of the clean owner master.");
            inputPins.Add(new SmallInputPin { Path = path, Sha256 = source.Sha256, Length = file.Length, Ticks = file.LastWriteTimeUtc.Ticks });
            var record = ReadInput(Chunk7SmallFixtureSelection.SourceLeaf, null, 65536);
            var entry = record["registryEntry"];
            Require((string)record["kind"] == "kmc-owner-seed-source" && (int?)record["schemaVersion"] == 1 &&
                (string)record["seedId"] == Chunk7SmallFixtureSelection.SeedIdentity &&
                record["ownerAuthorized"]?.Type == JTokenType.Boolean && (bool)record["ownerAuthorized"] &&
                record["producerAncestry"]?.Type == JTokenType.Boolean && !(bool)record["producerAncestry"] &&
                record["nativeAuthorityGranted"]?.Type == JTokenType.Boolean && !(bool)record["nativeAuthorityGranted"] &&
                (string)entry?["sha256"] == source.Sha256 && (long?)entry?["length"] == source.Length &&
                (string)entry?["seedId"] == Chunk7SmallFixtureSelection.SeedIdentity && (string)entry?["fileName"] == source.FileName &&
                (string)entry?["internalName"] == source.InternalName && (string)entry?["gameId"] == source.GameId &&
                (string)entry?["gameName"] == source.GameName && (string)entry?["area"] == source.Area && (string)entry?["type"] == "Manual",
                "F09 staged owner record differs from its exact immutable registered seed.");
            var qualification = ReadInput(Chunk7SmallFixtureSelection.QualificationLeaf, Chunk7SmallFixtureSelection.QualificationHash, 16777216);
            var q = qualification["observations"]; var identity = q?["ownerSeedQualification"]?["identity"];
            Require((string)qualification["status"] == "PASS" && (string)qualification["runId"] == Chunk7SmallFixtureSelection.QualificationRun &&
                (string)qualification["evidenceKind"] == "chunk7-assisted-cold-inventory-evidence" &&
                qualification["errors"] is JArray qe && qe.Count == 0 &&
                q?["ownerSeedQualification"]?["qualified"]?.Type == JTokenType.Boolean && (bool)q["ownerSeedQualification"]["qualified"] &&
                (string)q["selection"]?["seedId"] == Chunk7SmallFixtureSelection.SeedIdentity &&
                (string)q["selection"]?["seedSha256"] == source.Sha256 &&
                (string)identity?["gameId"] == source.GameId && (string)identity?["gameName"] == source.GameName &&
                (string)identity?["area"] == source.Area && (string)identity?["mainActorId"] == Chunk7SmallFixtureSelection.MainActorId &&
                q?["cleanup"]?["custodySettled"]?.Type == JTokenType.Boolean && (bool)q["cleanup"]["custodySettled"] &&
                q?["nativeProfileInventory"]?["ordinaryAcquisitionPrerequisites"]?["nativeMoney"]?.Type == JTokenType.Integer,
                "F09 requires the original accepted owner-seed native cold qualification, without producer ancestry.");
            qualifiedSeedMoney = (long)q["nativeProfileInventory"]["ordinaryAcquisitionPrerequisites"]["nativeMoney"];
            var catalogue = ReadInput(Chunk7SmallFixtureSelection.InventoryLeaf, Chunk7SmallFixtureSelection.InventoryHash, 16777216);
            var inventory = catalogue["observations"]?["nativeProfileInventory"];
            Require((string)catalogue["status"] == "PASS" && (string)catalogue["runId"] == Chunk7SmallFixtureSelection.InventoryRun &&
                catalogue["errors"] is JArray ce && ce.Count == 0 && (string)inventory?["captureStatus"] == "PASS" &&
                (string)inventory?["captureKind"] == "installed-native-small-body-catalogue" &&
                (string)inventory?["gameplayAssemblyMvid"] == typeof(Kingmaker.Game).Module.ModuleVersionId.ToString("D"),
                "F09 accepted native Halfling catalogue or installed module differs.");
            evidence["source"] = new JObject { ["seedId"] = Chunk7SmallFixtureSelection.SeedIdentity, ["seedSha256"] = source.Sha256,
                ["seedQualificationRunId"] = Chunk7SmallFixtureSelection.QualificationRun, ["seedQualificationArtifactSha256"] = Chunk7SmallFixtureSelection.QualificationHash,
                ["nativeInventoryRunId"] = Chunk7SmallFixtureSelection.InventoryRun, ["nativeInventoryArtifactSha256"] = Chunk7SmallFixtureSelection.InventoryHash,
                ["registrySha256"] = record["registrySha256"].DeepClone(), ["intake"] = record["intake"].DeepClone(),
                ["sourceRecordSha256"] = inputPins.Single(value => value.Path.EndsWith(Chunk7SmallFixtureSelection.SourceLeaf, StringComparison.Ordinal)).Sha256,
                ["descriptor"] = Chunk7EvidenceDto.Request(source), ["masterLoaded"] = false, ["producerAncestry"] = false };
            VerifyInputs();
        }
        private void VerifyInputs()
        {
            Require(inputPins.Count == 4, "F09 lost one of its four immutable input pins.");
            foreach (var pin in inputPins)
            {
                var file = new FileInfo(pin.Path);
                Require(file.Exists && file.Length == pin.Length && file.LastWriteTimeUtc.Ticks == pin.Ticks &&
                    (file.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(pin.Path) == pin.Sha256,
                    "F09 source archive or provenance bytes changed: " + Path.GetFileName(pin.Path));
            }
        }
        private JObject ActorSemantic(UnitEntityData value)
        {
            var progression = value.Descriptor.Progression; var classes = progression.Classes.Take(21).ToArray();
            var facts = progression.Features.Enumerable.Take(129).ToArray();
            var equipment = value.Descriptor.Body.CurrentEquipmentSlots.Take(13).ToArray();
            Require(classes.Length <= 20 && classes.All(item => item?.CharacterClass != null) && facts.Length <= 128 &&
                facts.All(item => item?.Blueprint != null) && equipment.Length == 12 && equipment.All(item => item != null),
                "F09 actor progression/fact/equipment observation exceeds its bounded native shape.");
            return new JObject { ["id"] = value.UniqueId, ["object"] = loadProbe.Identity(value), ["descriptor"] = loadProbe.Identity(value.Descriptor),
                ["view"] = loadProbe.Identity(value.View), ["blueprint"] = value.Blueprint.AssetGuid,
                ["nativeCustomCompanion"] = ReferenceEquals(value.Blueprint, BlueprintRoot.Instance.CustomCompanion),
                ["petId"] = value.Descriptor.Pet?.UniqueId, ["masterId"] = value.Descriptor.Master.Value?.UniqueId,
                ["experience"] = progression.Experience, ["level"] = progression.CharacterLevel,
                ["nextLevelExperience"] = progression.CharacterLevel < 20 ? (JToken)new JValue(BlueprintRoot.Instance.Progression.XPTable.GetBonus(progression.CharacterLevel + 1)) : JValue.CreateNull(),
                ["nativeCanLevelUp"] = LevelUpController.CanLevelUp(value.Descriptor),
                ["classes"] = new JArray(classes.OrderBy(item => item.CharacterClass.AssetGuid, StringComparer.Ordinal).Select(item => new JObject { ["guid"] = item.CharacterClass.AssetGuid, ["level"] = item.Level })),
                ["features"] = new JArray(facts.OrderBy(item => item.Blueprint.AssetGuid, StringComparer.Ordinal).Select(item => new JObject {
                    ["guid"] = item.Blueprint.AssetGuid, ["source"] = item.Source?.AssetGuid, ["rank"] = item.GetRank() })),
                ["equipment"] = new JArray(equipment.Select(item => (JToken)new JValue(item.MaybeItem?.Blueprint?.AssetGuid))) };
        }
        private JObject CaptureNeighbours() => new JObject { ["actors"] = new JArray(originalCharacters.Where(value => !ReferenceEquals(value, main))
            .OrderBy(value => value.UniqueId, StringComparer.Ordinal).Select(ActorSemantic)) };
        private void RequireNeighbours()
        { Require(JToken.DeepEquals(neighbourBefore, CaptureNeighbours()), "F09 changed an unrelated original companion's native identity, progression, sourced facts or active equipment."); }
        private JObject Arrangement() => new JObject { ["money"] = game.Player.Money, ["experienceRatePercent"] = game.Player.ExperienceRatePercent,
            ["characters"] = new JArray(game.Player.PartyCharacters.Select(value => value.Value.UniqueId)),
            ["remote"] = new JArray(game.Player.RemoteCompanions.Select(value => value.Value.UniqueId)),
            ["actors"] = new JArray(Chunk7NativeFieldRoster.Capture(game.Player).OrderBy(value => value.UniqueId, StringComparer.Ordinal).Select(ActorSemantic)) };
        private JObject WorldSnapshot()
        {
            RequireWorld();
            return new JObject { ["frame"] = UnityEngine.Time.frameCount, ["game"] = loadProbe.Identity(game), ["saveManager"] = loadProbe.Identity(manager),
                ["player"] = loadProbe.Identity(game.Player), ["state"] = loadProbe.Identity(game.State),
                ["areaState"] = loadProbe.Identity(game.State.LoadedAreaState), ["mainState"] = loadProbe.Identity(game.State.LoadedAreaState.MainState),
                ["gameId"] = game.Player.GameId, ["gameName"] = main.CharacterName, ["mainActorId"] = main.UniqueId,
                ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe, ["mode"] = game.CurrentMode.ToString(), ["inCombat"] = game.Player.IsInCombat,
                ["owners"] = world.Registry.Owners.Length, ["slots"] = controls.CapturePersistentSlots().Length, ["casts"] = controls.NativeCastRequestCount,
                ["snapshots"] = persistence.SnapshotCount, ["completedSaves"] = persistence.CompletedSaveCount,
                ["semanticRestores"] = persistence.SemanticRestoreCount, ["presentationRestores"] = persistence.PresentationRestoreCount,
                ["authority"] = AuthoritySnapshot(), ["party"] = new JArray(Chunk7NativeFieldRoster.Capture(game.Player).Select(value => new JObject {
                    ["id"] = value.UniqueId, ["blueprint"] = value.Blueprint.AssetGuid, ["object"] = loadProbe.Identity(value),
                    ["descriptor"] = loadProbe.Identity(value.Descriptor), ["view"] = loadProbe.Identity(value.View), ["ready"] = Ready(value),
                    ["native"] = JObject.FromObject(MountedPersistenceService.CaptureActor(value), MountedSaveCodec.CreateSerializer()),
                    ["prepared"] = value.CombatState.Prepared, ["inCombat"] = value.IsInCombat })) };
        }
        private void CaptureUnmountedFixture()
        {
            RequireAuthority(false); RequireWorld(); RequireNeighbours(); RequireNoOwners(); RequireNoEffects(); VerifyInputs();
            RequireHorse(main); RequireHorse(actor);
            var characters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            var all = Chunk7NativeFieldRoster.Capture(game.Player);
            Require(characters.Length == 5 && characters.Count(value => ReferenceEquals(value, actor)) == 1 &&
                originalCharacters.All(value => characters.Count(other => ReferenceEquals(value, other)) == 1) && !game.Player.RemoteCompanions.Any() &&
                all.Length == 7 && all.All(Ready) && all.All(value => originalCharacters.Contains(value) || ReferenceEquals(value, mainHorse) || ReferenceEquals(value, actor) || ReferenceEquals(value, pet)) &&
                ReferenceEquals(actor.Blueprint, BlueprintRoot.Instance.CustomCompanion) && game.Player.Money == initialMoney - nativeHireCost &&
                main.Descriptor.Progression.Experience == 9000 && actor.Descriptor.Progression.Experience == nativeHireXp,
                "F09 final field must contain its original four characters, exact paid Halfling and two reciprocal Horses only.");
            var body = NativeSmallRiderBody.Capture(actor, pet);
            RequireSmallBody(body);
            var inventory = Chunk7NativeProfileInventory.Capture(new[] { actor, pet });
            Require((string)inventory["captureStatus"] == "PASS" && inventory["party"] is JArray party && party.Count == 2,
                "F09 unmounted live rig inventory did not complete.");
            var rider = ((JArray)inventory["party"]).Single(value => (string)value["id"] == actor.UniqueId);
            var horse = ((JArray)inventory["party"]).Single(value => (string)value["id"] == pet.UniqueId);
            Require(rider["character"]?["poseTransforms"] is JArray bones && bones.Count == 7 && bones.All(value => value["matches"] is JArray matches && matches.Count == 1) &&
                horse["horseSeatAnchors"] is JArray anchors && anchors.Count == 3 && anchors.All(value => value["matches"] is JArray matches && matches.Count == 1),
                "F09 requires seven unique live rider pose transforms and three unique live Horse anchors.");
            bodyBeforeSave = CaptureBody(body); arrangementBeforeSave = Arrangement();
            evidence["selector"] = field.Capture(); evidence["bodyObservation"] = bodyBeforeSave.DeepClone();
            evidence["unmountedProfileInventory"] = inventory; evidence["fieldWorld"] = WorldSnapshot();
            evidence["beforeSaveArrangement"] = arrangementBeforeSave.DeepClone(); evidence["neighboursAfter"] = CaptureNeighbours();
            evidence["acquiredOwners"] = new JArray(new[] { main, actor }.Select(value => {
                var fact = value.Descriptor.GetFact(horses.HorseFeature) as Feature;
                return new JObject { ["riderId"] = value.UniqueId, ["horseId"] = value.Descriptor.Pet.UniqueId,
                    ["horseBlueprint"] = value.Descriptor.Pet.Blueprint.AssetGuid, ["featureGuid"] = horses.HorseFeature.AssetGuid,
                    ["featureSourceGuid"] = fact.Source.AssetGuid, ["rank"] = value.Descriptor.GetFact(horses.LevelRank).GetRank(),
                    ["reciprocal"] = ReferenceEquals(value.Descriptor.Pet.Descriptor.Master.Value, value) }; }));
            Pass("C7-small-fixture-unmounted-field");
        }
        internal static void RequireSmallBody(SmallRiderBodyObservation body)
        {
            Require(body != null && body.OriginalSizeOrdinal == 3 && body.EffectiveSizeOrdinal == 3 && !body.RiderPolymorphed && !body.MountPolymorphed &&
                body.MountBlueprintGuid == DeclaredSmallRiderBody.HorseBlueprintGuid && body.MountSizeOrdinal > 3 &&
                body.RaceGuid == Chunk7SmallFixtureSelection.RaceGuid && body.PresetGuid == Chunk7SmallFixtureSelection.PresetGuid &&
                body.Gender == "Male" && body.SkeletonName == Chunk7SmallFixtureSelection.SkeletonName && !body.LeftHanded &&
                body.NativeRaceAndPresetIdentityExact && body.NativeSkeletonIsExactPreset && body.NativeRigAvailable && body.AvatarLocalScale.IsFinite &&
                Math.Abs(body.AvatarLocalScale.X) >= 0.01f && Math.Abs(body.AvatarLocalScale.Y) >= 0.01f && Math.Abs(body.AvatarLocalScale.Z) >= 0.01f &&
                Math.Abs(body.AvatarLocalScale.X) <= 10 && Math.Abs(body.AvatarLocalScale.Y) <= 10 && Math.Abs(body.AvatarLocalScale.Z) <= 10 &&
                body.DollEquipmentEntityIds != null && body.DollEquipmentEntityIds.Length <= 128 &&
                body.DollEquipmentEntityIds.All(value => value != null && System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-f0-9]{32}$")) &&
                body.DollEquipmentEntityIds.Distinct(StringComparer.Ordinal).Count() == body.DollEquipmentEntityIds.Length &&
                body.CurrentEquipmentBlueprints != null && body.CurrentEquipmentBlueprints.Length == 12 &&
                body.CurrentEquipmentBlueprints.All(value => value == null || System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-f0-9]{32}$")) &&
                body.ActiveVisualEquipmentNames != null && body.ActiveVisualEquipmentNames.Length > 0 && body.ActiveVisualEquipmentNames.Length <= 128 &&
                body.ActiveVisualEquipmentNames.All(value => !string.IsNullOrWhiteSpace(value) && value.Length <= 160) && body.AdditionalLimbCount == 0,
                "F09 live unmounted body/equipment differs from its exact default male right-handed Halfling and Horse catalogue pins.");
        }
        // PoseVector3 exposes derived properties including Normalized. Project
        // the three measured components explicitly; never serialize its recursive
        // derived object graph or turn an observation into a profile declaration.
        internal static JObject CaptureBody(SmallRiderBodyObservation body) => new JObject {
            ["originalSizeOrdinal"] = body.OriginalSizeOrdinal, ["effectiveSizeOrdinal"] = body.EffectiveSizeOrdinal,
            ["riderPolymorphed"] = body.RiderPolymorphed, ["mountPolymorphed"] = body.MountPolymorphed,
            ["mountBlueprintGuid"] = body.MountBlueprintGuid, ["mountSizeOrdinal"] = body.MountSizeOrdinal,
            ["raceGuid"] = body.RaceGuid, ["presetGuid"] = body.PresetGuid, ["gender"] = body.Gender, ["skeletonName"] = body.SkeletonName,
            ["leftHanded"] = body.LeftHanded, ["avatarMirrored"] = body.AvatarMirrored,
            ["nativeRaceAndPresetIdentityExact"] = body.NativeRaceAndPresetIdentityExact,
            ["nativeSkeletonIsExactPreset"] = body.NativeSkeletonIsExactPreset, ["nativeRigAvailable"] = body.NativeRigAvailable,
            ["avatarLocalScale"] = new JObject { ["x"] = body.AvatarLocalScale.X, ["y"] = body.AvatarLocalScale.Y, ["z"] = body.AvatarLocalScale.Z },
            ["dollEquipmentEntityIds"] = new JArray(body.DollEquipmentEntityIds),
            ["currentEquipmentBlueprints"] = new JArray(body.CurrentEquipmentBlueprints.Select(value => value == null ? JValue.CreateNull() : new JValue(value))),
            ["activeVisualEquipmentNames"] = new JArray(body.ActiveVisualEquipmentNames), ["additionalLimbCount"] = body.AdditionalLimbCount };
    }
}
