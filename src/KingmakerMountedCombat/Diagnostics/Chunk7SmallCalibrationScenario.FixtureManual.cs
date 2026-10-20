using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7SmallCalibrationScenario
    {
        // The F09 source is a previously completed owner-seed fixture, never an
        // acquired-field Manual. This case re-observes its own load and actions.
        private void ReadFixtureManualSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7SmallFixtureManualSourceSelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 &&
                (source.Attributes & FileAttributes.ReparsePoint) == 0 && archive.Exists &&
                archive.Length == request.PersistenceLoad.Length && (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == SourceArtifactHash && Hash(archivePath) == SourceArchiveHash,
                "The staged F09 fixture source differs from its immutable descriptor.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks;
            archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks;
            manualSource = RequireFixtureManualArtifact(request, JObject.Parse(File.ReadAllText(sourcePath)));
            var savedBody = manualSource["savedBodyObservation"];
            sourceSelection = new JObject { ["smallRaceGuid"] = (string)savedBody["raceGuid"],
                ["smallPresetGuid"] = (string)savedBody["presetGuid"], ["skeletonName"] = (string)savedBody["skeletonName"],
                ["gender"] = (string)savedBody["gender"], ["leftHanded"] = (bool)savedBody["leftHanded"] };
            evidence["source"] = new JObject { ["runId"] = calibration.FixtureSource.SourceRunId,
                ["artifactSha256"] = SourceArtifactHash, ["archiveSha256"] = SourceArchiveHash,
                ["coldReceiptSha256"] = calibration.FixtureSource.ColdReceiptSha256,
                ["archiveLeaf"] = request.PersistenceLoad.FileName, ["archiveName"] = request.PersistenceLoad.InternalName,
                ["archiveLength"] = archiveLength, ["bodySelection"] = sourceSelection.DeepClone(),
                ["producerCommit"] = "784a6296f9e0d91c44ad25dda826db57f609e929",
                ["producerDllSha256"] = "8f7f89769c82e0ac15f5d972441d29e68d6f67ff4ad942fe55f25ece12047d89",
                ["producerDllMvid"] = "bf98be50-898a-41b3-88f0-78959a0ea1a3",
                ["sameCandidate"] = false, ["unchangedArchive"] = true,
                ["fixtureSetupOnly"] = true, ["kind"] = "qualified-owner-seed-f09-small-fixture-manual",
                ["fixtureManual"] = manualSource.DeepClone() };
        }

        internal static JObject RequireFixtureManualArtifact(RuntimeRequest request, JObject artifact)
        {
            var plan = request?.SmallCalibration?.FixtureSource;
            var o = artifact?["observations"] as JObject;
            var pairs = o?["acquiredOwners"] as JArray;
            var world = o?["savedWorld"] as JObject;
            var arrangement = o?["afterSaveArrangement"] as JObject;
            var write = o?["nativeWrite"] as JObject;
            Require(request?.SmallCalibration?.UsesFixtureManualField == true && plan != null &&
                plan.Validate(request.RunId).Count == 0 && o != null &&
                (string)artifact["runId"] == plan.SourceRunId &&
                (string)artifact["scenario"] == Chunk7SmallFixtureSelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7SmallFixtureScenario.EvidenceKind &&
                (string)artifact["status"] == "PASS" && artifact["errors"] is JArray errors && errors.Count == 0 &&
                (string)artifact["commit"] == "784a6296f9e0d91c44ad25dda826db57f609e929" &&
                (string)artifact["dllSha256"] == "8f7f89769c82e0ac15f5d972441d29e68d6f67ff4ad942fe55f25ece12047d89" &&
                (string)artifact["dllMvid"] == "bf98be50-898a-41b3-88f0-78959a0ea1a3" &&
                (string)artifact["dllSha256"] != request.DllSha256 &&
                (string)o["contract"] == "owner-seed-unmounted-small-fixture-v1" &&
                o["fixtureSetupOnly"]?.Type == JTokenType.Boolean && (bool)o["fixtureSetupOnly"] &&
                JToken.DeepEquals(o["beforeSaveArrangement"], arrangement) &&
                JToken.DeepEquals(o["bodyObservation"], o["savedBodyObservation"]) &&
                world?["party"] is JArray party && party.Count == 7 &&
                arrangement?["characters"] is JArray characters && characters.Count == 5 &&
                arrangement["remote"] is JArray remote && remote.Count == 0 &&
                pairs != null && pairs.Count == 2 &&
                (string)pairs[0]["riderId"] == (string)world["mainActorId"] &&
                (string)pairs[1]["riderId"] == plan.ActorId && (string)pairs[1]["horseId"] == plan.PetId &&
                pairs.All(pair => pair["reciprocal"]?.Type == JTokenType.Boolean && (bool)pair["reciprocal"]) &&
                party.Count(row => (string)row["id"] == plan.ActorId) == 1 &&
                party.Count(row => (string)row["id"] == plan.PetId) == 1 &&
                write != null && (string)write["sha256"] == plan.SourceArchiveSha256 &&
                (string)write["fileName"] == request.PersistenceLoad?.FileName &&
                (string)write["internalName"] == Chunk7SmallFixtureManualSourceSelection.NativeName &&
                (string)write["type"] == "Manual" &&
                (string)write["gameId"] == request.PersistenceLoad.GameId &&
                (string)write["gameName"] == request.PersistenceLoad.GameName &&
                (string)write["area"] == request.PersistenceLoad.Area &&
                write["autoLevelup"]?.Type == JTokenType.Boolean && !(bool)write["autoLevelup"] &&
                o["cleanup"]?["custodySettled"]?.Type == JTokenType.Boolean && (bool)o["cleanup"]["custodySettled"] &&
                o["cleanup"]?["sourceUnchanged"]?.Type == JTokenType.Boolean && (bool)o["cleanup"]["sourceUnchanged"],
                "The F09 Small source is not its exact settled preview.71 owner-seed fixture Manual.");
            return o;
        }

        private void RequireFixtureManualBody(SmallRiderBodyObservation body)
        {
            RequireBody(body);
            var now = Chunk7SmallPoseObservation.Body(body, true);
            var saved = manualSource["savedBodyObservation"] as JObject;
            Require(saved != null && saved.Properties().Count() == now.Properties().Count() &&
                saved.Properties().All(value =>
                {
                    var matching = now.Properties().Where(property => string.Equals(property.Name, value.Name,
                        StringComparison.OrdinalIgnoreCase)).ToArray();
                    return matching.Length == 1 && JToken.DeepEquals(matching[0].Value, value.Value);
                }), "Cold F09 Small body, equipment or rig differs from the saved native observation.");
        }

        private void RequireFixtureManualArrangement()
        {
            var saved = manualSource["afterSaveArrangement"] as JObject;
            var rows = saved?["actors"] as JArray;
            Require(saved != null && rows != null && rows.Count == manualActors.Length &&
                (int)saved["money"] == Game.Instance.Player.Money &&
                (int)saved["experienceRatePercent"] == Game.Instance.Player.ExperienceRatePercent &&
                JToken.DeepEquals(saved["characters"], new JArray(Game.Instance.Player.PartyCharacters.Select(value => value.Value.UniqueId))) &&
                saved["remote"] is JArray remote && remote.Count == 0 &&
                manualActors.All(value =>
                {
                    var matches = rows.Where(candidate => (string)candidate["id"] == value.UniqueId).ToArray();
                    if (matches.Length != 1) return false;
                    var row = matches[0];
                    var classes = row["classes"] as JArray;
                    return (string)row["blueprint"] == value.Blueprint.AssetGuid &&
                        (int)row["experience"] == value.Descriptor.Progression.Experience &&
                        (int)row["level"] == value.Descriptor.Progression.CharacterLevel &&
                        classes != null && classes.Count == value.Descriptor.Progression.Classes.Count() &&
                        value.Descriptor.Progression.Classes.All(item => classes.Count(entry =>
                            (string)entry["guid"] == item.CharacterClass.AssetGuid && (int)entry["level"] == item.Level) == 1);
                }), "F09 cold roster, money, XP, classes or party custody differs from its genuine Manual.");
        }

        private void RequireFixtureManualResources()
        {
            var rows = manualSource["savedWorld"]?["party"] as JArray;
            Require(rows != null && rows.Count == manualActors.Length && manualActors.All(value =>
            {
                var matches = rows.Where(candidate => (string)candidate["id"] == value.UniqueId).ToArray();
                if (matches.Length != 1) return false;
                var row = matches[0]; var native = row["native"];
                return native != null && row["prepared"]?.Type == JTokenType.Boolean && !(bool)row["prepared"] &&
                    row["inCombat"]?.Type == JTokenType.Boolean && !(bool)row["inCombat"] &&
                    !value.CombatState.Prepared && !value.IsInCombat &&
                    (float)native["Standard"] == value.CombatState.Cooldown.StandardAction &&
                    (float)native["Move"] == value.CombatState.Cooldown.MoveAction &&
                    (float)native["Swift"] == value.CombatState.Cooldown.SwiftAction &&
                    (float)native["Reaction"] == value.CombatState.Cooldown.AttackOfOpportunity &&
                    (float)native["Initiative"] == value.CombatState.Initiative &&
                    (int)native["ReactionsRemaining"] == value.CombatState.AttackOfOpportunityCount;
            }), "F09 cold native action resources or preparation differ from the saved roster.");
        }

        private void BindFixtureManualPriorPair()
        {
            var prior = manualSource["acquiredOwners"]?[0];
            manualPriorOwner = manualActors.SingleOrDefault(value => value.UniqueId == (string)prior?["riderId"]);
            manualPriorPet = manualActors.SingleOrDefault(value => value.UniqueId == (string)prior?["horseId"]);
            Require(manualPriorOwner != null && manualPriorPet != null && manualPriorOwner != actor && manualPriorPet != pet &&
                manualPriorOwner != manualPriorPet, "F09 neighboring main/Horse pair is absent or aliases Small.");
            manualPriorOwnerDescriptor = manualPriorOwner.Descriptor; manualPriorPetDescriptor = manualPriorPet.Descriptor;
            manualPriorOwnerView = manualPriorOwner.View; manualPriorPetView = manualPriorPet.View;
            manualPriorFeature = manualPriorOwner.Descriptor.GetFact(horses.HorseFeature);
            manualPriorRank = manualPriorOwner.Descriptor.GetFact(horses.LevelRank);
            manualPriorSource = (manualPriorFeature as Feature)?.Source;
            manualPriorPairBefore = CaptureAssistedManualPriorPair();
            RequireFixtureManualPriorPair();
            evidence["inheritedFixturePairBefore"] = manualPriorPairBefore.DeepClone();
        }

        private void RequireFixtureManualPriorPair()
        {
            var saved = manualSource["acquiredOwners"][0];
            var now = CaptureAssistedManualPriorPair();
            var changed = manualPriorPairBefore.Properties().Where(value =>
                !JToken.DeepEquals(value.Value, now[value.Name])).Select(value => value.Name).ToArray();
            var exactCustody = ReferenceEquals(manualPriorOwner.Descriptor, manualPriorOwnerDescriptor) &&
                ReferenceEquals(manualPriorPet.Descriptor, manualPriorPetDescriptor) &&
                ReferenceEquals(manualPriorOwner.View, manualPriorOwnerView) &&
                ReferenceEquals(manualPriorPet.View, manualPriorPetView) &&
                ReferenceEquals(manualPriorOwner.Descriptor.GetFact(horses.HorseFeature), manualPriorFeature) &&
                ReferenceEquals(manualPriorOwner.Descriptor.GetFact(horses.LevelRank), manualPriorRank) &&
                ReferenceEquals((manualPriorFeature as Feature)?.Source, manualPriorSource) &&
                ReferenceEquals(manualPriorOwner.Descriptor.Pet, manualPriorPet) &&
                ReferenceEquals(manualPriorPet.Descriptor.Master.Value, manualPriorOwner) &&
                Game.Instance.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, manualPriorOwner)) == 1 &&
                (string)now["actorId"] == (string)saved["riderId"] &&
                (string)now["petId"] == (string)saved["horseId"] &&
                (string)now["petBlueprint"] == (string)saved["horseBlueprint"] &&
                (string)now["featureGuid"] == (string)saved["featureGuid"] &&
                (string)now["sourceGuid"] == (string)saved["featureSourceGuid"] &&
                (int)now["rank"] == (int)saved["rank"] &&
                (bool)now["reciprocal"] &&
                (string)now["actorBlueprint"] == manualPriorOwner.Blueprint.AssetGuid;
            var admitted = evidence["inheritedFixturePairNativeCombatAdmission"] as JObject;
            var nativeCombatAdmission = exactCustody && changed.Length == 1 && changed[0] == "resources" &&
                (admitted == null ? IsFixtureNativeCombatAdmission(now) :
                    JToken.DeepEquals(now, admitted["after"]) && manualPriorOwner.IsInCombat && manualPriorPet.IsInCombat);
            if (nativeCombatAdmission && evidence["inheritedFixturePairNativeCombatAdmission"] == null)
                evidence["inheritedFixturePairNativeCombatAdmission"] = new JObject {
                    ["stage"] = calibrationStage, ["frame"] = Time.frameCount,
                    ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["targetId"] = target.UniqueId, ["targetInCombat"] = target.IsInCombat,
                    ["ownerInCombat"] = manualPriorOwner.IsInCombat, ["horseInCombat"] = manualPriorPet.IsInCombat,
                    ["ownerCommandsEmpty"] = manualPriorOwner.Commands.Empty,
                    ["horseCommandsEmpty"] = manualPriorPet.Commands.Empty,
                    ["before"] = manualPriorPairBefore.DeepClone(), ["after"] = now.DeepClone() };
            if (!exactCustody || (changed.Length != 0 && !nativeCombatAdmission))
            {
                if (evidence["inheritedFixturePairDrift"] == null)
                    evidence["inheritedFixturePairDrift"] = new JObject {
                        ["stage"] = calibrationStage, ["frame"] = Time.frameCount,
                        ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                        ["changedKeys"] = new JArray(changed), ["exactCustody"] = exactCustody,
                        ["ownerInCombat"] = manualPriorOwner.IsInCombat,
                        ["horseInCombat"] = manualPriorPet.IsInCombat,
                        ["ownerPrepared"] = manualPriorOwner.CombatState.Prepared,
                        ["horsePrepared"] = manualPriorPet.CombatState.Prepared,
                        ["before"] = manualPriorPairBefore.DeepClone(), ["after"] = now.DeepClone() };
                var reason = "F09 neighboring main/Horse changed at calibration stage " + calibrationStage +
                    "; exactCustody=" + exactCustody + "; changed=" + string.Join(",", changed) + ".";
                // A failed diagnostic may settle its owned UI/command leases when
                // identity, native facts and reciprocal custody still hold. The
                // resource mismatch remains a FAIL and is never admitted as PASS.
                if (cleanup && exactCustody && changed.All(key => key == "resources"))
                    Fail("C7-small-cleanup", reason);
                else
                    Require(false, reason);
            }
            evidence["inheritedFixturePairAfter"] = now;
        }

        private bool IsFixtureNativeCombatAdmission(JObject now)
        {
            if (calibrationStage != 3 || targetService?.State != DiagnosticCombatTargetState.Active || target == null ||
                !target.IsInCombat || !manualPriorOwner.IsInCombat || !manualPriorPet.IsInCombat ||
                !manualPriorOwner.Commands.Empty || !manualPriorPet.Commands.Empty) return false;
            var before = manualPriorPairBefore["resources"] as JArray;
            var after = now["resources"] as JArray;
            if (before == null || after == null || before.Count != 2 || after.Count != 2) return false;
            return before.Children<JObject>().All(saved =>
            {
                var matches = after.Children<JObject>().Where(row =>
                    (string)row["actorId"] == (string)saved["actorId"]).ToArray();
                if (matches.Length != 1) return false;
                var current = matches[0];
                return saved["prepared"]?.Type == JTokenType.Boolean && !(bool)saved["prepared"] &&
                    (int)saved["initiative"] == 0 && current["prepared"]?.Type == JTokenType.Boolean &&
                    (bool)current["prepared"] && (int)current["initiative"] > 0 &&
                    saved.Properties().Where(value => value.Name != "prepared" && value.Name != "initiative")
                        .All(value => JToken.DeepEquals(value.Value, current[value.Name]));
            });
        }

        private void RequireFixtureManualIdentity()
        {
            var saved = manualSource["acquiredOwners"][1];
            var current = Chunk7NativeFieldRoster.Capture(Game.Instance.Player);
            var fact = actor.Descriptor.GetFact(horses.HorseFeature) as Feature;
            Require(manualActors != null && current.Length == manualActors.Length &&
                current.All(value => manualActors.Count(item => ReferenceEquals(item, value)) == 1) &&
                manualCharacters != null && Game.Instance.Player.Party.Count() == manualCharacters.Length &&
                Game.Instance.Player.Party.All(value => manualCharacters.Count(item => ReferenceEquals(item, value)) == 1) &&
                ReferenceEquals(Game.Instance.Player.MainCharacter.Value, manualPriorOwner) &&
                actor.UniqueId == calibration.FixtureSource.ActorId && pet.UniqueId == calibration.FixtureSource.PetId &&
                ReferenceEquals(actor.Descriptor.Pet, pet) && ReferenceEquals(pet.Descriptor.Master.Value, actor) &&
                fact != null && ReferenceEquals(fact, manualFeature) && ReferenceEquals(fact.Source, manualFeatureSource) &&
                fact.Source?.AssetGuid == (string)saved["featureSourceGuid"] &&
                horses.HorseFeature.AssetGuid == (string)saved["featureGuid"] &&
                ReferenceEquals(actor.Descriptor.GetFact(horses.LevelRank), manualRank) &&
                actor.Descriptor.GetFact(horses.LevelRank)?.GetRank() == (int)saved["rank"] &&
                JToken.DeepEquals(manualArrangement, CaptureManualArrangement()),
                "F09 Small owner/fact/rank, roster or progression changed; custody remains retained.");
            RequireFixtureManualPriorPair();
        }
    }
}
