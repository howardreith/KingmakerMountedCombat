using System;
using System.IO;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7AssistedColdInventoryScenario
    {
        private void ReadThirdHorseSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7AssistedColdInventorySelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            var saved = request.PersistenceLoad;
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 &&
                (source.Attributes & FileAttributes.ReparsePoint) == 0 &&
                archive.Exists && archive.Length == saved.Length &&
                archive.LastWriteTimeUtc.Ticks == saved.LastWriteTimeUtcTicks &&
                (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == selection.SourceArtifactSha256 && Hash(archivePath) == selection.SourceArchiveSha256,
                "Exact staged third-Horse producer receipt or Manual differs.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks;
            archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks; sourcePinned = true;
            var artifact = JObject.Parse(File.ReadAllText(sourcePath));
            var observed = artifact["observations"] as JObject;
            var write = observed?["nativeWrite"] as JObject;
            var manual = observed?["thirdHorseManual"] as JObject;
            var setup = observed?["thirdHorseSetup"] as JObject;
            var boundary = manual?["boundary"] as JObject;
            var progress = boundary?["actors"] as JArray;
            var field = observed?["cleanup"]?["partyAfter"] as JArray;
            var nativeHorse = setup?["nativeHorse"] as JObject;
            var inherited = observed?["inheritedFixture"]?["pairs"] as JArray;
            Require((string)artifact["runId"] == selection.SourceRunId &&
                (string)artifact["scenario"] == Chunk7OwnedPairsSelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7WorldScenarioEngine.EvidenceKind &&
                (string)artifact["status"] == "PASS" && artifact["errors"] is JArray errors && errors.Count == 0 &&
                (string)setup?["contract"] == "small-source-third-native-horse-setup-v1" &&
                (string)write?["sha256"] == selection.SourceArchiveSha256 &&
                (string)write?["path"] != null && (string)write["fileName"] == saved.FileName &&
                (string)write["nativeName"] == saved.InternalName && (string)write["gameId"] == saved.GameId &&
                (string)write["gameName"] == saved.GameName && (string)write["area"] == saved.Area &&
                (long?)write["length"] == saved.Length && (long?)write["lastWriteTimeUtcTicks"] == saved.LastWriteTimeUtcTicks &&
                (string)manual?["contract"] == "small-third-horse-manual-producer-v1" &&
                manual["disclosedAssistance"]?.Value<bool>() == true && manual["acquisitionProof"]?.Value<bool>() == false &&
                manual["setupCompletedBeforeSave"]?.Value<bool>() == true &&
                boundary != null && field != null && field.Count == 8 && progress != null && progress.Count == 8 &&
                JToken.DeepEquals(boundary, manual["arrangementAfterSave"]) &&
                nativeHorse != null && inherited != null && inherited.Count == 2 &&
                write["archive"]?["Pairs"] is JArray pairs && pairs.Count == 0 &&
                write["archive"]?["Participations"] is JArray participations && participations.Count == 0 &&
                write["archive"]?["Slots"] is JArray slots && slots.Count == 0 &&
                write["archive"]?["Combat"]?.Type == JTokenType.Null,
                "Staged third-Horse source lacks the completed eight-actor Manual and empty mounted graph.");
            Require(field.Select(row => (string)row["id"]).Distinct(StringComparer.Ordinal).Count() == 8 &&
                progress.Select(row => (string)row["actorId"]).Distinct(StringComparer.Ordinal).Count() == 8 &&
                field.All(row => progress.Count(actor => (string)actor["actorId"] == (string)row["id"] &&
                    (string)actor["blueprint"] == (string)row["blueprint"]) == 1) &&
                manual["fieldActorIds"] is JArray fieldIds && fieldIds.Count == 8 &&
                fieldIds.Select(value => (string)value).OrderBy(id => id, StringComparer.Ordinal)
                    .SequenceEqual(field.Select(row => (string)row["id"]).OrderBy(id => id, StringComparer.Ordinal)),
                "Third-Horse producer saved roster and native progression disagree.");
            var party = new JArray();
            foreach (var row in field)
            {
                var actor = progress.Single(value => (string)value["actorId"] == (string)row["id"]);
                var classes = actor["classes"] as JArray;
                Require(classes != null && classes.Count > 0 && classes.Count <= 20 &&
                    classes.All(value => value["classGuid"]?.Type == JTokenType.String && value["level"]?.Type == JTokenType.Integer),
                    "Third-Horse saved class ranks are missing or unbounded.");
                party.Add(new JObject { ["id"] = row["id"].DeepClone(), ["blueprint"] = row["blueprint"].DeepClone(),
                    ["petId"] = row["petId"]?.DeepClone(), ["masterId"] = row["masterId"]?.DeepClone(),
                    ["characterLevel"] = actor["characterLevel"].DeepClone(),
                    ["experience"] = actor["experience"].DeepClone(),
                    ["classes"] = new JArray(classes.Select(value => new JObject {
                        ["guid"] = value["classGuid"].DeepClone(), ["level"] = value["level"].DeepClone() })) });
            }
            expectedMainId = Chunk7SmallFixtureSelection.MainActorId;
            Require(party.Count(row => (string)row["id"] == expectedMainId) == 1 &&
                (string)nativeHorse["mountId"] == "da28f0a7-6eb0-4392-b2e0-f401a889b3aa" &&
                (string)nativeHorse["mountMasterId"] == (string)setup["targetRiderId"] &&
                (string)nativeHorse["blueprint"] == HorseCompanionBlueprintService.UnitGuid,
                "Third-Horse saved main, rider or native mount identity differs.");
            sourceWorld = new JObject { ["gameId"] = saved.GameId, ["gameName"] = saved.GameName,
                ["area"] = saved.Area, ["mainActorId"] = expectedMainId, ["party"] = party };
            sourceReadiness = boundary;
            expectedNativeMoney = (long)boundary["money"];
            thirdHorseOwners = new JArray(inherited.Select(row => new JObject {
                ["riderId"] = row["riderId"].DeepClone(), ["mountId"] = row["mountId"].DeepClone(),
                ["sourceGuid"] = row["sourceGuid"].DeepClone(), ["rank"] = row["rank"].DeepClone() }));
            thirdHorseOwners.Add(new JObject { ["riderId"] = nativeHorse["mountMasterId"].DeepClone(),
                ["mountId"] = nativeHorse["mountId"].DeepClone(), ["sourceGuid"] = nativeHorse["sourceGuid"].DeepClone(),
                ["rank"] = nativeHorse["rank"].DeepClone() });
            Require(thirdHorseOwners.Count == 3 &&
                thirdHorseOwners.Select(row => (string)row["riderId"]).Distinct(StringComparer.Ordinal).Count() == 3 &&
                thirdHorseOwners.Select(row => (string)row["mountId"]).Distinct(StringComparer.Ordinal).Count() == 3 &&
                thirdHorseOwners.All(row =>
                party.Count(actor => (string)actor["id"] == (string)row["riderId"] &&
                    (string)actor["petId"] == (string)row["mountId"]) == 1 &&
                party.Count(actor => (string)actor["id"] == (string)row["mountId"] &&
                    (string)actor["masterId"] == (string)row["riderId"] &&
                    (string)actor["blueprint"] == HorseCompanionBlueprintService.UnitGuid) == 1),
                "The three producer Horses lack distinct reciprocal saved roles.");
            VerifySourceBytes();
        }

        private void RequireThirdHorseSourceRoster()
        {
            Require(actors.Length == 8 && game.Player.Money == expectedNativeMoney &&
                JToken.DeepEquals(sourceReadiness, Chunk7OwnedPairFixture.ArrangementOf(actors)),
                "Cold third-Horse native roster, money or progression differs from the saved Manual.");
            foreach (var actor in actors)
            {
                var source = ((JArray)sourceWorld["party"]).Single(row => (string)row["id"] == actor.UniqueId);
                Require((string)source["blueprint"] == actor.Blueprint.AssetGuid &&
                    (string)source["petId"] == actor.Descriptor.Pet?.UniqueId &&
                    (string)source["masterId"] == actor.Descriptor.Master.Value?.UniqueId &&
                    (int)source["characterLevel"] == actor.Descriptor.Progression.CharacterLevel &&
                    (int)source["experience"] == actor.Descriptor.Progression.Experience &&
                    JToken.DeepEquals(source["classes"], CaptureClasses(actor)),
                    "Cold third-Horse actor, reciprocal pet or class rank differs from its native save.");
            }
            CaptureThirdHorseFacts();
        }

        private JArray CaptureThirdHorseFacts()
        {
            Require(thirdHorseOwners != null && thirdHorseOwners.Count == 3 &&
                horses.HorseFeature != null && horses.LevelRank != null && horses.HorseUnit != null,
                "Cold three-Horse feature catalogue is unavailable.");
            var result = new JArray();
            foreach (var row in thirdHorseOwners)
            {
                var rider = actors.Single(actor => actor.UniqueId == (string)row["riderId"]);
                var mount = actors.Single(actor => actor.UniqueId == (string)row["mountId"]);
                var fact = rider.Descriptor.GetFact(horses.HorseFeature) as Feature;
                var rank = rider.Descriptor.GetFact(horses.LevelRank)?.GetRank();
                Require(fact?.Source?.AssetGuid == (string)row["sourceGuid"] &&
                    rank == (int)row["rank"] && rank > 0 &&
                    ReferenceEquals(rider.Descriptor.Pet, mount) &&
                    ReferenceEquals(mount.Descriptor.Master.Value, rider) &&
                    ReferenceEquals(mount.Blueprint, horses.HorseUnit),
                    "Cold native Horse lost its saved sourced feature, rank or reciprocal pet.");
                result.Add(new JObject { ["riderId"] = rider.UniqueId, ["mountId"] = mount.UniqueId,
                    ["featureGuid"] = horses.HorseFeature.AssetGuid, ["blueprint"] = mount.Blueprint.AssetGuid,
                    ["sourceGuid"] = fact.Source.AssetGuid, ["rank"] = rank,
                    ["reciprocal"] = true });
            }
            return result;
        }
    }
}
