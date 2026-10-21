using System;
using System.IO;
using System.Linq;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7ManualDerivedScenario
    {
        private string assistedColdPath;
        private long assistedColdLength, assistedColdTicks;
        private JObject assistedColdObservations, assistedSourceProjection;
        private JToken SelectedSourceReadiness => assistedSourceProjection?["fixtureReadiness"] ?? sourceObservations["fixtureReadiness"];
        private JToken SelectedSourceInventory => assistedSourceProjection?["nativeProfileInventory"] ?? sourceObservations["nativeProfileInventory"];
        private JToken SelectedSourceArrangement => assistedSourceProjection?["afterSaveArrangement"] ?? sourceObservations["afterSaveArrangement"];
        private JToken SelectedSourceResources => assistedSourceProjection?["resourcesAfterSave"] ?? sourceObservations["resourcesAfterSave"];

        private void ReadOwnerSeedColdSource(JObject artifact)
        {
            var observations = sourceObservations;
            Require((string)artifact["runId"] == selection.SourceRunId &&
                (string)artifact["scenario"] == Chunk7AssistedColdInventorySelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7AssistedColdInventoryScenario.EvidenceKind &&
                (string)artifact["status"] == "PASS" && artifact["errors"] is JArray errors && errors.Count == 0 &&
                (string)artifact["commit"] == request.Commit && (string)artifact["dllSha256"] == request.DllSha256 &&
                (string)artifact["dllMvid"] == request.DllMvid && observations != null &&
                (string)observations["selection"]?["case"] == "owner-seed-readonly-inventory" &&
                (string)observations["selection"]?["seedId"] == "KMC_C7_NO_MODS_OWNER_SEED" &&
                (string)observations["selection"]?["seedSha256"] == selection.SourceArchiveSha256 &&
                (string)observations["seedSource"]?["archiveLeaf"] == request.PersistenceLoad.FileName &&
                (string)observations["seedSource"]?["seedSha256"] == selection.SourceArchiveSha256 &&
                AssistedTrue(observations["seedSource"]?["ownerAuthorized"]) &&
                AssistedFalse(observations["seedSource"]?["producerAncestry"]) &&
                AssistedTrue(observations["ownerSeedQualification"]?["qualified"]) &&
                (string)observations["kmcMetadata"]?["kind"] == "Missing" &&
                AssistedFalse(observations["kmcMetadata"]?["loadedArchivePresent"]) &&
                AssistedTrue(observations["cleanup"]?["custodySettled"]) &&
                AssistedTrue(observations["cleanup"]?["sourceUnchanged"]) &&
                AssistedTrue(observations["cleanup"]?["observersReleased"]),
                "Registered owner-seed source lacks exact same-candidate cold qualification and missing-metadata provenance.");
            sourceWorld = observations["loadedWorld"] as JObject;
            var settled = observations["settledWorld"] as JObject;
            var party = sourceWorld?["party"] as JArray;
            Require(sourceWorld != null && settled != null && party != null && party.Count > 0 && party.Count <= 6 &&
                (string)sourceWorld["gameId"] == request.PersistenceLoad.GameId &&
                (string)sourceWorld["gameName"] == request.PersistenceLoad.GameName &&
                (string)sourceWorld["area"] == request.PersistenceLoad.Area &&
                (string)settled["gameId"] == request.PersistenceLoad.GameId &&
                (string)settled["mainActorId"] == (string)sourceWorld["mainActorId"] &&
                (string)observations["fixtureReadiness"]?["captureStatus"] == "PASS" &&
                (string)observations["nativeProfileInventory"]?["captureStatus"] == "PASS",
                "Owner-seed cold source lacks bounded native world and readiness observations.");
            expectedMainId = (string)sourceWorld["mainActorId"];
            Require(Chunk7ManualDerivedSelection.NativeId(expectedMainId) &&
                party.Count(row => (string)row["id"] == expectedMainId) == 1 &&
                party.All(row => Chunk7ManualDerivedSelection.NativeId((string)row["id"]) &&
                    Chunk7ManualDerivedSelection.BlueprintId((string)row["blueprint"]) &&
                    row["classes"] is JArray && row["experience"]?.Type == JTokenType.Integer),
                "Owner-seed cold party lacks exact actor, blueprint or progression identity.");
            assistedSourceProjection = new JObject {
                ["fixtureReadiness"] = observations["fixtureReadiness"].DeepClone(),
                ["nativeProfileInventory"] = observations["nativeProfileInventory"].DeepClone(),
                ["afterSaveArrangement"] = new JObject {
                    ["money"] = sourceWorld["nativeMoney"].DeepClone(),
                    ["experienceRatePercent"] = observations["fixtureReadiness"]["experienceRatePercent"].DeepClone(),
                    ["actors"] = new JArray(party.OrderBy(row => (string)row["id"], StringComparer.Ordinal)
                        .Select(row => new JObject {
                            ["actorId"] = row["id"].DeepClone(), ["blueprint"] = row["blueprint"].DeepClone(),
                            ["experience"] = row["experience"].DeepClone(),
                            ["characterLevel"] = row["characterLevel"].DeepClone(),
                            ["classes"] = new JArray(((JArray)row["classes"])
                                .OrderBy(value => (string)value["guid"], StringComparer.Ordinal)
                                .Select(value => new JObject { ["classGuid"] = value["guid"].DeepClone(),
                                    ["level"] = value["level"].DeepClone() })) }))
                }
            };
        }

        private void ReadAssistedCumulativeSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7CumulativeAcquisitionSelection.SourceLeaf);
            assistedColdPath = Path.Combine(Path.GetDirectoryName(root), Chunk7CumulativeAcquisitionSelection.AssistedColdLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var cold = new FileInfo(assistedColdPath); var archive = new FileInfo(archivePath);
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 && (source.Attributes & FileAttributes.ReparsePoint) == 0 &&
                cold.Exists && cold.Length > 0 && cold.Length <= 16 * 1024 * 1024 && (cold.Attributes & FileAttributes.ReparsePoint) == 0 &&
                archive.Exists && archive.Length == request.PersistenceLoad.Length && (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == cumulative.SourceArtifactSha256 && Hash(assistedColdPath) == cumulative.AssistedColdArtifactSha256 &&
                Hash(archivePath) == cumulative.SourceArchiveSha256,
                "Assisted cumulative input requires both exact source artifacts and the unchanged producer Manual.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks;
            assistedColdLength = cold.Length; assistedColdTicks = cold.LastWriteTimeUtc.Ticks;
            archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks; sourcePinned = true;
            var producer = JObject.Parse(File.ReadAllText(sourcePath)); var coldArtifact = JObject.Parse(File.ReadAllText(assistedColdPath));
            assistedSourceProjection = RequireAssistedCumulativeSource(request, producer, coldArtifact);
            sourceObservations = (JObject)producer["observations"];
            assistedColdObservations = (JObject)coldArtifact["observations"];
            sourceWorld = (JObject)sourceObservations["settledWorld"];
            cumulativeSourceOwner = (JObject)assistedSourceProjection["savedOwner"];
            expectedMainId = (string)sourceWorld["mainActorId"];
            VerifySourceBytes();
        }
        private void VerifyAssistedColdBytes()
        {
            if (cumulative?.UsesAssistedCold != true) return;
            var cold = new FileInfo(assistedColdPath);
            Require(cold.Exists && cold.Length == assistedColdLength && cold.LastWriteTimeUtc.Ticks == assistedColdTicks &&
                (cold.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(assistedColdPath) == cumulative.AssistedColdArtifactSha256,
                "Assisted cold source artifact changed during native acquisition custody.");
        }
        private void CaptureAssistedCumulativeSource()
        {
            if (cumulative?.UsesAssistedCold != true) return;
            evidence["assistedSource"] = new JObject { ["contract"] = "assisted-producer-cold-cumulative-v1",
                ["mode"] = cumulative.SourceMode, ["producerRunId"] = cumulative.SourceRunId,
                ["producerArtifactSha256"] = cumulative.SourceArtifactSha256, ["coldRunId"] = cumulative.AssistedColdRunId,
                ["coldArtifactSha256"] = cumulative.AssistedColdArtifactSha256, ["archiveSha256"] = cumulative.SourceArchiveSha256,
                ["setupWasAssisted"] = true, ["setupIsOrdinaryAcquisitionProof"] = false,
                ["coldObservations"] = assistedColdObservations.DeepClone(), ["derivedInputs"] = assistedSourceProjection.DeepClone() };
        }

        // This is a semantic projection of two different successful observations,
        // not an acquired-Manual artifact or a grant of native load authority.
        // The locked runner separately qualifies both completed/restored runs.
        internal static JObject RequireAssistedCumulativeSource(RuntimeRequest request, JObject producer, JObject coldArtifact)
        {
            var s = request?.CumulativeAcquisition; var d = request?.PersistenceLoad;
            Require(s?.UsesAssistedCold == true && request.Validate().Count == 0 && d != null,
                "A closed assisted-cold cumulative request is required.");
            RequireAssistedEnvelope(request, producer, s.SourceRunId, Chunk7AssistedFixtureSelection.Scenario, Chunk7AssistedFixtureScenario.EvidenceKind);
            RequireAssistedEnvelope(request, coldArtifact, s.AssistedColdRunId, Chunk7AssistedColdInventorySelection.Scenario, Chunk7AssistedColdInventoryScenario.EvidenceKind);
            var p = (JObject)producer["observations"]; var c = (JObject)coldArtifact["observations"];
            var write = p["finalManual"];
            Require((string)write?["fileName"] == d.FileName && (string)write?["internalName"] == Chunk7AssistedFixtureSelection.OutputName &&
                (string)write?["type"] == "Manual" && (string)write?["operation"] == "None" && AssistedFalse(write?["autoLevelup"]) &&
                (string)write?["sha256"] == s.SourceArchiveSha256 && (long?)write?["length"] == d.Length &&
                (string)write?["gameId"] == d.GameId && (string)write?["gameName"] == d.GameName && (string)write?["area"] == d.Area &&
                AssistedTrue(p["cleanup"]?["custodySettled"]) && AssistedTrue(p["cleanup"]?["nativeWorkerSettled"]) && AssistedTrue(p["cleanup"]?["observersReleased"]) &&
                (string)c["selection"]?["case"] == Chunk7AssistedColdInventorySelection.CaseName && AssistedTrue(c["selection"]?["baseGameOnly"]) &&
                (string)c["selection"]?["sourceRunId"] == s.SourceRunId && (string)c["selection"]?["sourceArtifactSha256"] == s.SourceArtifactSha256 &&
                (string)c["selection"]?["sourceArchiveSha256"] == s.SourceArchiveSha256 &&
                (int?)c["selection"]?["expectedNativeLoadCount"] == 1 && (int?)c["selection"]?["expectedNativeWriteCount"] == 0 &&
                (string)c["source"]?["runId"] == s.SourceRunId && (string)c["source"]?["artifactSha256"] == s.SourceArtifactSha256 &&
                (string)c["source"]?["archiveSha256"] == s.SourceArchiveSha256 && (string)c["source"]?["archiveLeaf"] == d.FileName &&
                AssistedTrue(c["cleanup"]?["custodySettled"]) && AssistedTrue(c["cleanup"]?["sourceUnchanged"]) &&
                AssistedTrue(c["cleanup"]?["worldAdmitted"]) && AssistedTrue(c["cleanup"]?["observersReleased"]) &&
                (string)c["loader"]?["state"] == "LoadedAndVerified" && (string)c["settledLoader"]?["state"] == "LoadedAndVerified" &&
                AssistedTrue(c["loader"]?["callbackObserved"]) && AssistedTrue(c["settledLoader"]?["callbackObserved"]) &&
                JToken.DeepEquals(c["sourceNativeWorld"], p["settledWorld"]) && JToken.DeepEquals(c["sourceFixtureReadiness"], p["fixtureReadiness"]),
                "Assisted source is not the unchanged final Manual and its exact successful cold observation.");
            var saved = p["settledWorld"] as JObject; var loaded = c["loadedWorld"] as JObject; var settled = c["settledWorld"] as JObject;
            var party = saved?["party"] as JArray; var inventory = c["nativeProfileInventory"] as JObject; var readiness = c["fixtureReadiness"] as JObject;
            Require(saved != null && loaded != null && settled != null && party != null && party.Count >= 2 && party.Count <= 12 &&
                party.All(row => row is JObject && Chunk7ManualDerivedSelection.NativeId((string)row["id"])) &&
                party.Select(row => (string)row["id"]).Distinct(StringComparer.Ordinal).Count() == party.Count &&
                (string)saved["mainActorId"] == s.PriorOwnerId && (string)saved["gameId"] == d.GameId && (string)saved["area"] == d.Area &&
                readiness != null && (string)readiness["captureStatus"] == "PASS" && inventory != null && (string)inventory["captureStatus"] == "PASS" &&
                JToken.DeepEquals(c["sourceNativeMoney"], inventory["ordinaryAcquisitionPrerequisites"]?["nativeMoney"]) &&
                JToken.DeepEquals(c["sourceNativeMoney"], p["nativeProfileInventory"]?["ordinaryAcquisitionPrerequisites"]?["nativeMoney"]),
                "Assisted source lacks bounded exact full actors, native readiness or preserved money.");
            var mainProgression = p["fixtureReadiness"]?["mainProgression"] as JObject;
            Require(mainProgression != null && (string)mainProgression["actorId"] == s.PriorOwnerId &&
                mainProgression["experience"]?.Type == JTokenType.Integer && (long)mainProgression["experience"] >= 0 &&
                JToken.DeepEquals(mainProgression, readiness["mainProgression"]),
                "Assisted main progression must retain its separately captured source readiness.");
            foreach (var world in new[] { loaded, settled })
            {
                var actual = world["party"] as JArray;
                Require((string)world["gameId"] == d.GameId && (string)world["gameName"] == d.GameName && (string)world["area"] == d.Area &&
                    (string)world["mainActorId"] == s.PriorOwnerId && (string)world["mode"] == "Default" &&
                    AssistedFalse(world["cutscene"]) && AssistedFalse(world["inCombat"]) && AssistedFalse(world["nativeLoading"]) && AssistedFalse(world["dialogScheduled"]) &&
                    JToken.DeepEquals(world["nativeMoney"], c["sourceNativeMoney"]) && actual != null && actual.Count == party.Count &&
                    actual.All(row => row is JObject) && actual.Select(row => (string)row["id"]).Distinct(StringComparer.Ordinal).Count() == actual.Count,
                    "Assisted cold source is not the same settled native campaign and roster.");
                foreach (var row in party)
                {
                    var current = actual.SingleOrDefault(value => (string)value["id"] == (string)row["id"]);
                    Require(current != null && JToken.DeepEquals(current["experience"], (string)row["id"] == s.PriorOwnerId ? mainProgression["experience"] : row["experience"]) &&
                        new[] { "blueprint", "petId", "masterId", "characterLevel", "classes" }.All(key => JToken.DeepEquals(current[key], row[key])), "Assisted cold actor semantics differ from the saved producer.");
                }
            }
            Require(party.Count(row => row["petId"]?.Type == JTokenType.String) == 1 && party.Count(row => row["masterId"]?.Type == JTokenType.String) == 1,
                "Assisted source must retain exactly one ordinary reciprocal pair.");
            var owner = party.SingleOrDefault(row => (string)row["id"] == s.PriorOwnerId);
            var pet = party.SingleOrDefault(row => (string)row["id"] == s.PriorPetId);
            var facts = owner?["features"] as JArray;
            var feature = facts?.SingleOrDefault(row => (string)row["guid"] == HorseCompanionBlueprintService.FeatureGuid);
            var rank = facts?.SingleOrDefault(row => (string)row["guid"] == HorseCompanionBlueprintService.LevelRankGuid);
            Require(owner != null && pet != null && (string)owner["petId"] == s.PriorPetId && (string)pet["masterId"] == s.PriorOwnerId &&
                (string)pet["blueprint"] == HorseCompanionBlueprintService.UnitGuid && feature != null && rank != null &&
                Chunk7ManualDerivedSelection.BlueprintId((string)feature["source"]) && rank["rank"]?.Type == JTokenType.Integer && (int)rank["rank"] > 0 && (int)rank["rank"] <= 20,
                "Assisted prior owner lacks its exact native Horse feature, source, rank or reciprocal pet.");
            var ordered = party.OrderBy(row => (string)row["id"], StringComparer.Ordinal).ToArray();
            Require(ordered.All(row => row["actions"] is JObject && row["classes"] is JArray), "Assisted source omitted saved action/class observations.");
            return new JObject { ["contract"] = "assisted-native-source-projection-v1",
                ["fixtureReadiness"] = readiness.DeepClone(), ["nativeProfileInventory"] = inventory.DeepClone(),
                ["savedOwner"] = new JObject { ["actorId"] = s.PriorOwnerId, ["petId"] = s.PriorPetId,
                    ["actorBlueprint"] = owner["blueprint"].DeepClone(), ["petBlueprint"] = pet["blueprint"].DeepClone(),
                    ["featureGuid"] = HorseCompanionBlueprintService.FeatureGuid, ["sourceGuid"] = feature["source"].DeepClone(), ["rank"] = rank["rank"].DeepClone() },
                ["afterSaveArrangement"] = new JObject { ["money"] = c["sourceNativeMoney"].DeepClone(),
                    ["experienceRatePercent"] = readiness["experienceRatePercent"].DeepClone(), ["actors"] = new JArray(ordered.Select(row => new JObject {
                        ["actorId"] = row["id"].DeepClone(), ["blueprint"] = row["blueprint"].DeepClone(), ["experience"] = ((string)row["id"] == s.PriorOwnerId ? mainProgression["experience"] : row["experience"]).DeepClone(),
                        ["characterLevel"] = row["characterLevel"].DeepClone(), ["classes"] = new JArray(((JArray)row["classes"]).OrderBy(value => (string)value["guid"], StringComparer.Ordinal)
                            .Select(value => new JObject { ["classGuid"] = value["guid"].DeepClone(), ["level"] = value["level"].DeepClone() })) })) },
                ["resourcesAfterSave"] = new JObject { ["actors"] = new JArray(ordered.Select(row => { var value = (JObject)row["actions"].DeepClone(); value.AddFirst(new JProperty("actorId", row["id"].DeepClone())); return value; })) } };
        }
        private static bool AssistedTrue(JToken value) => value?.Type == JTokenType.Boolean && (bool)value;
        private static bool AssistedFalse(JToken value) => value?.Type == JTokenType.Boolean && !(bool)value;
        private static void RequireAssistedEnvelope(RuntimeRequest request, JObject artifact, string runId, string scenario, string kind)
        {
            Require(artifact != null && (string)artifact["runId"] == runId && (string)artifact["scenario"] == scenario &&
                (string)artifact["evidenceKind"] == kind && (string)artifact["status"] == "PASS" && artifact["errors"] is JArray errors && errors.Count == 0 &&
                (string)artifact["commit"] == request.Commit && (string)artifact["dllSha256"] == request.DllSha256 &&
                (string)artifact["dllMvid"] == request.DllMvid && artifact["observations"] is JObject,
                "Assisted cumulative source requires exact successful same-candidate producer and cold artifacts.");
        }
    }
}
