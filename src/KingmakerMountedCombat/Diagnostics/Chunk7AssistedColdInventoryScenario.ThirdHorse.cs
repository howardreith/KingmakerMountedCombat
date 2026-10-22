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
            if (selection.IsPaidHorseWorld) { ReadFifthHorseSource(); return; }
            var fourth = selection.IsFourthHorseSource;
            var count = fourth ? 9 : 8;
            var horseCount = fourth ? 4 : 3;
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
                "Exact staged Horse-world producer receipt or Manual differs.");
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
                (string)setup?["contract"] == (fourth ? "three-horse-source-fourth-native-horse-setup-v1" : "small-source-third-native-horse-setup-v1") &&
                (string)write?["sha256"] == selection.SourceArchiveSha256 &&
                (string)write?["path"] != null && (string)write["fileName"] == saved.FileName &&
                (string)write["nativeName"] == saved.InternalName && (string)write["gameId"] == saved.GameId &&
                (string)write["gameName"] == saved.GameName && (string)write["area"] == saved.Area &&
                (long?)write["length"] == saved.Length && (long?)write["lastWriteTimeUtcTicks"] == saved.LastWriteTimeUtcTicks &&
                (string)manual?["contract"] == (fourth ? "three-horse-source-fourth-native-horse-manual-v1" : "small-third-horse-manual-producer-v1") &&
                manual["disclosedAssistance"]?.Value<bool>() == true && manual["acquisitionProof"]?.Value<bool>() == false &&
                manual["setupCompletedBeforeSave"]?.Value<bool>() == true &&
                boundary != null && field != null && field.Count == count && progress != null && progress.Count == count &&
                JToken.DeepEquals(boundary, manual["arrangementAfterSave"]) &&
                nativeHorse != null && inherited != null && inherited.Count == horseCount - 1 &&
                write["archive"]?["Pairs"] is JArray pairs && pairs.Count == 0 &&
                write["archive"]?["Participations"] is JArray participations && participations.Count == 0 &&
                write["archive"]?["Slots"] is JArray slots && slots.Count == 0 &&
                write["archive"]?["Combat"]?.Type == JTokenType.Null,
                "Staged Horse-world source lacks the completed Manual and empty mounted graph.");
            Require(field.Select(row => (string)row["id"]).Distinct(StringComparer.Ordinal).Count() == count &&
                progress.Select(row => (string)row["actorId"]).Distinct(StringComparer.Ordinal).Count() == count &&
                field.All(row => progress.Count(actor => (string)actor["actorId"] == (string)row["id"] &&
                    (string)actor["blueprint"] == (string)row["blueprint"]) == 1) &&
                manual["fieldActorIds"] is JArray fieldIds && fieldIds.Count == count &&
                fieldIds.Select(value => (string)value).OrderBy(id => id, StringComparer.Ordinal)
                    .SequenceEqual(field.Select(row => (string)row["id"]).OrderBy(id => id, StringComparer.Ordinal)),
                "Horse-world producer saved roster and native progression disagree.");
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
                (string)nativeHorse["mountId"] == (fourth ? "e486e6e9-aa36-423e-9d9e-9dfbe4a94a07" : "da28f0a7-6eb0-4392-b2e0-f401a889b3aa") &&
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
            Require(thirdHorseOwners.Count == horseCount &&
                thirdHorseOwners.Select(row => (string)row["riderId"]).Distinct(StringComparer.Ordinal).Count() == horseCount &&
                thirdHorseOwners.Select(row => (string)row["mountId"]).Distinct(StringComparer.Ordinal).Count() == horseCount &&
                thirdHorseOwners.All(row =>
                party.Count(actor => (string)actor["id"] == (string)row["riderId"] &&
                    (string)actor["petId"] == (string)row["mountId"]) == 1 &&
                party.Count(actor => (string)actor["id"] == (string)row["mountId"] &&
                    (string)actor["masterId"] == (string)row["riderId"] &&
                    (string)actor["blueprint"] == HorseCompanionBlueprintService.UnitGuid) == 1),
                "The producer Horses lack distinct reciprocal saved roles.");
            VerifySourceBytes();
        }

        private void RequireThirdHorseSourceRoster()
        {
            Require(actors.Length == selection.HorseWorldActorCount && game.Player.Money == expectedNativeMoney &&
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
            if (selection.IsSixthHorse) CaptureSixthRemote();
            CaptureThirdHorseFacts();
        }

        private void ReadFifthHorseSource()
        {
            var sixth = selection.IsSixthHorse;
            var priorCount = sixth ? 11 : 9;
            var count = selection.HorseWorldActorCount;
            var horseCount = selection.HorseWorldHorseCount;
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7AssistedColdInventorySelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath); var saved = request.PersistenceLoad;
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 &&
                (source.Attributes & FileAttributes.ReparsePoint) == 0 && archive.Exists &&
                archive.Length == saved.Length && archive.LastWriteTimeUtc.Ticks == saved.LastWriteTimeUtcTicks &&
                (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == selection.SourceArtifactSha256 && Hash(archivePath) == selection.SourceArchiveSha256,
                "Exact fifth-Horse producer receipt or Manual differs.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks;
            archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks; sourcePinned = true;
            var artifact = JObject.Parse(File.ReadAllText(sourcePath)); var observed = artifact["observations"] as JObject;
            var write = observed?["nativeWrite"] as JObject; var savedWorld = observed?["savedFieldWorld"] as JObject;
            var oldWorld = observed?["sourceNativeWorld"] as JObject;
            var oldFacts = observed?["sourceManualObservations"]?[sixth ? "fifthHorseFacts" : "fourthHorseFacts"] as JArray;
            var beforeFacts = observed?["f09PriorHorsesBefore"] as JArray;
            var afterFacts = observed?["f09PriorHorsesAfter"] as JArray;
            var field = observed?["field"] as JObject; var progress = observed?["afterSaveArrangement"] as JObject;
            var original = progress?["actors"] as JArray; var acquired = progress?["acquired"] as JArray;
            var prior = oldWorld?["party"] as JArray; var savedParty = savedWorld?["party"] as JArray;
            var newNative = field?["profileInventory"]?["party"] as JArray;
            Require((string)artifact["runId"] == selection.SourceRunId &&
                (string)artifact["scenario"] == Chunk7ManualDerivedSelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7ManualDerivedScenario.EvidenceKind &&
                (string)artifact["status"] == "PASS" && artifact["errors"] is JArray errors && errors.Count == 0 &&
                (string)observed?["selection"]?["case"] == Chunk7ManualDerivedSelection.PaidSmallCase &&
                observed["selection"]?["baseGameOnly"]?.Value<bool>() == true &&
                (string)write?["sha256"] == selection.SourceArchiveSha256 &&
                (string)write?["fileName"] == saved.FileName && (string)write?["internalName"] == saved.InternalName &&
                (string)write?["gameId"] == saved.GameId && (string)write?["gameName"] == saved.GameName &&
                (string)write?["area"] == saved.Area && (long?)write?["length"] == saved.Length &&
                (long?)write?["lastWriteTimeUtcTicks"] == saved.LastWriteTimeUtcTicks &&
                write["emptyMountedArchive"]?.Value<bool>() == true &&
                observed["archive"]?["pairs"]?.Value<int>() == 0 &&
                observed["archive"]?["participations"]?.Value<int>() == 0 &&
                observed["archive"]?["slots"]?.Value<int>() == 0 &&
                observed["archive"]?["combatPresent"]?.Value<bool>() == false &&
                prior != null && prior.Count == priorCount && savedParty != null && savedParty.Count == count &&
                original != null && original.Count == priorCount && acquired != null && acquired.Count == 2 &&
                oldFacts != null && oldFacts.Count == horseCount - 1 && beforeFacts != null && beforeFacts.Count == horseCount - 1 &&
                afterFacts != null && JToken.DeepEquals(beforeFacts, afterFacts) &&
                newNative != null && newNative.Count == 2 && field["reciprocal"]?.Value<bool>() == true &&
                field["ordinaryFieldAdmissionProven"]?.Value<bool>() == true,
                "Fifth-Horse Manual lacks exact paid-field, prior-Horse or empty-KMC provenance.");
            expectedMainId = (string)oldWorld["mainActorId"];
            Require(expectedMainId == (string)savedWorld["mainActorId"] &&
                (string)savedWorld["gameId"] == saved.GameId && (string)savedWorld["area"] == saved.Area &&
                prior.Count(row => (string)row["id"] == expectedMainId) == 1,
                "Fifth-Horse producer changed its native campaign or main actor.");
            var allProgress = original.Concat(acquired).ToArray();
            Require(allProgress.Select(row => (string)row["actorId"]).Distinct(StringComparer.Ordinal).Count() == priorCount + 2,
                "Paid Horse-world saved progression repeats an actor.");
            var party = new JArray();
            foreach (var row in savedParty)
            {
                var id = (string)row["id"];
                var matching = allProgress.Where(value => (string)value["actorId"] == id).ToArray();
                Require(matching.Length == 1 && (string)matching[0]["blueprint"] == (string)row["blueprint"] &&
                    matching[0]["classes"] is JArray classes && classes.Count > 0 && classes.Count <= 20,
                    "Fifth-Horse saved actor has no exact native progression.");
                var old = prior.SingleOrDefault(value => (string)value["id"] == id);
                var added = newNative.SingleOrDefault(value => (string)value["id"] == id);
                Require((old == null) != (added == null) &&
                    (old == null || (string)old["blueprint"] == (string)row["blueprint"]) &&
                    (added == null || (string)added["blueprintGuid"] == (string)row["blueprint"]),
                    "Fifth-Horse producer saved actor differs from its native source or paid field.");
                var link = old ?? added;
                party.Add(new JObject { ["id"] = id, ["blueprint"] = row["blueprint"].DeepClone(),
                    ["petId"] = link["petId"]?.DeepClone(), ["masterId"] = link["masterId"]?.DeepClone(),
                    ["characterLevel"] = matching[0]["characterLevel"].DeepClone(),
                    ["experience"] = matching[0]["experience"].DeepClone(),
                    ["classes"] = new JArray(((JArray)matching[0]["classes"]).Select(value => new JObject {
                        ["guid"] = value["classGuid"].DeepClone(), ["level"] = value["level"].DeepClone() })) });
            }
            Require(party.Select(row => (string)row["id"]).Distinct(StringComparer.Ordinal).Count() == count &&
                (string)field["actorId"] == (sixth ? "d8c2f840-9d59-4b5e-9250-c628a36c5589" : "82deb3d9-d734-4843-81c6-384becf082d9") &&
                (string)field["petId"] == (sixth ? "130fd1b5-c9f4-4b9f-80de-6980e0d21556" : "1ed01148-1646-469c-a3d5-153df01192b8") &&
                (string)field["horseBlueprint"] == HorseCompanionBlueprintService.UnitGuid,
                "Fifth-Horse output lost its exact paid rider or mount.");
            if (sixth) ReadSixthRemoteSource(observed, prior, allProgress, party);
            sourceWorld = new JObject { ["gameId"] = saved.GameId, ["gameName"] = saved.GameName,
                ["area"] = saved.Area, ["mainActorId"] = expectedMainId, ["party"] = party };
            sourceReadiness = new JObject { ["money"] = progress["money"].DeepClone(),
                ["experienceRatePercent"] = progress["experienceRatePercent"].DeepClone(),
                ["actors"] = new JArray(allProgress.Where(value => party.Any(row => (string)row["id"] == (string)value["actorId"]))
                    .OrderBy(value => (string)value["actorId"], StringComparer.Ordinal).Select(value => value.DeepClone())) };
            expectedNativeMoney = (long)progress["money"];
            thirdHorseOwners = new JArray(oldFacts.Select(value => new JObject {
                ["riderId"] = value["riderId"].DeepClone(), ["mountId"] = value["mountId"].DeepClone(),
                ["sourceGuid"] = value["sourceGuid"].DeepClone(), ["rank"] = value["rank"].DeepClone() }));
            thirdHorseOwners.Add(new JObject { ["riderId"] = field["actorId"].DeepClone(),
                ["mountId"] = field["petId"].DeepClone(), ["sourceGuid"] = field["featureSourceGuid"].DeepClone(),
                ["rank"] = field["rank"].DeepClone() });
            Require(thirdHorseOwners.Count == horseCount &&
                thirdHorseOwners.Select(value => (string)value["riderId"]).Distinct(StringComparer.Ordinal).Count() == horseCount &&
                thirdHorseOwners.Select(value => (string)value["mountId"]).Distinct(StringComparer.Ordinal).Count() == horseCount &&
                thirdHorseOwners.All(value => party.Count(row => (string)row["id"] == (string)value["riderId"] &&
                    (string)row["petId"] == (string)value["mountId"]) == 1 &&
                    party.Count(row => (string)row["id"] == (string)value["mountId"] &&
                    (string)row["masterId"] == (string)value["riderId"] &&
                    (string)row["blueprint"] == HorseCompanionBlueprintService.UnitGuid) == 1),
                "Fifth-Horse producer does not retain five reciprocal native pet links.");
            VerifySourceBytes();
        }

        private JObject sixthRemoteExpected, sixthRemoteAtLoad;
        private void ReadSixthRemoteSource(JObject observed, JArray prior, JToken[] progress, JArray field)
        {
            var old = prior.Single(row => (string)row["id"] == DepartureActorId);
            var p = progress.Single(row => (string)row["actorId"] == DepartureActorId);
            var roster = observed["savedSixthRoster"] as JObject;
            Require(old["petId"]?.Type == JTokenType.Null && old["masterId"]?.Type == JTokenType.Null &&
                (string)old["blueprint"] == (string)p["blueprint"] &&
                (int)old["characterLevel"] == (int)p["characterLevel"] && (int)old["experience"] == (int)p["experience"] &&
                JToken.DeepEquals(old["classes"], new JArray(((JArray)p["classes"]).Select(row => new JObject {
                    ["guid"] = row["classGuid"].DeepClone(), ["level"] = row["level"].DeepClone() }))) &&
                !field.Any(row => (string)row["id"] == DepartureActorId) &&
                roster?["remoteCompanionIds"] is JArray ids && ids.Count == 1 && (string)ids[0] == DepartureActorId &&
                roster["partyCharacterIds"] is JArray characters && characters.Count == 6 &&
                characters.Select(row => (string)row).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(
                    field.Where(row => row["masterId"]?.Type == JTokenType.Null).Select(row => (string)row["id"]).OrderBy(id => id, StringComparer.Ordinal)) &&
                roster["manualPartyMutation"]?.Value<bool>() == false,
                "Sixth-Horse source lost the genuine remote Linzi roster or original progression.");
            sixthRemoteExpected = new JObject { ["id"] = DepartureActorId, ["blueprint"] = old["blueprint"].DeepClone(),
                ["petId"] = JValue.CreateNull(), ["masterId"] = JValue.CreateNull(),
                ["characterLevel"] = p["characterLevel"].DeepClone(), ["experience"] = p["experience"].DeepClone(),
                ["classes"] = old["classes"].DeepClone() };
        }
        private JObject CaptureSixthRemote()
        {
            Require(selection.IsSixthHorse && sixthRemoteExpected != null && remote.Length == 1 &&
                remote[0].UniqueId == DepartureActorId && !actors.Contains(remote[0]) && !characters.Contains(remote[0]),
                "Sixth-Horse cold load lost its exact remote Linzi membership.");
            Require(game.Player.RemoteCompanions.Select(value => value.Value).SequenceEqual(remote),
                "Sixth-Horse cold native remote membership changed.");
            var actor = remote[0];
            var semantics = new JObject { ["id"] = actor.UniqueId, ["blueprint"] = actor.Blueprint.AssetGuid,
                ["petId"] = actor.Descriptor.Pet == null ? JValue.CreateNull() : new JValue(actor.Descriptor.Pet.UniqueId),
                ["masterId"] = actor.Descriptor.Master.Value == null ? JValue.CreateNull() : new JValue(actor.Descriptor.Master.Value.UniqueId),
                ["characterLevel"] = actor.Descriptor.Progression.CharacterLevel,
                ["experience"] = actor.Descriptor.Progression.Experience, ["classes"] = CaptureClasses(actor) };
            Require(JToken.DeepEquals(semantics, sixthRemoteExpected), "Cold remote Linzi progression or pet links differ from the saved source.");
            return new JObject { ["semantics"] = semantics, ["object"] = probe.Identity(actor), ["descriptor"] = probe.Identity(actor.Descriptor) };
        }

        private JArray CaptureThirdHorseFacts()
        {
            Require(thirdHorseOwners != null && thirdHorseOwners.Count == selection.HorseWorldHorseCount &&
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
