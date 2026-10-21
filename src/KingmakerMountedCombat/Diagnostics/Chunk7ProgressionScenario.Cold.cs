using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7ProgressionScenario
    {
        private JObject CaptureLevelNativeOutcome()
        {
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            var feature = rider.Descriptor.GetFact(horses.HorseFeature) as Feature;
            var rank = rider.Descriptor.GetFact(horses.LevelRank);
            var controlFeature = controlRider.Descriptor.GetFact(horses.HorseFeature) as Feature;
            var controlRank = controlRider.Descriptor.GetFact(horses.LevelRank);
            Require(ranger != null && feature?.Source != null && rank != null &&
                controlFeature?.Source != null && controlRank != null &&
                horses.HorseFeature.AssetGuid == HorseCompanionBlueprintService.FeatureGuid &&
                horses.LevelRank.AssetGuid == HorseCompanionBlueprintService.LevelRankGuid &&
                rider.Descriptor.Progression.CharacterLevel == 5 &&
                rider.Descriptor.Progression.GetClassLevel(ranger) == 5 &&
                ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider),
                "Small later-level native Ranger, sourced Horse fact or reciprocal pet did not survive the operation.");
            return new JObject { ["rangerLevel"] = rider.Descriptor.Progression.GetClassLevel(ranger),
                ["horseFeatureGuid"] = horses.HorseFeature.AssetGuid,
                ["horseFeatureSourceGuid"] = feature.Source.AssetGuid,
                ["horseRankGuid"] = horses.LevelRank.AssetGuid, ["horseRank"] = rank.GetRank(),
                ["petBlueprintGuid"] = mount.Blueprint.AssetGuid,
                ["controlRangerLevel"] = controlRider.Descriptor.Progression.GetClassLevel(ranger),
                ["controlHorseFeatureSourceGuid"] = controlFeature.Source.AssetGuid,
                ["controlHorseRank"] = controlRank.GetRank() };
        }
        // The level Manual retains both owners. Its cold consumer has a
        // separate contract from retrain, which retires the target owner.
        private void StartLevelCold()
        {
            Require(selection.IsLevelCold && Game.Instance.CurrentMode == GameModeType.Default &&
                !Game.Instance.Player.IsInCombat && !Game.Instance.CutsceneLock &&
                !Game.Instance.IsModeActive(GameModeType.Dialog) &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "Small level cold continuation requires settled free-control exploration.");
            sourceEvidencePath = Path.Combine(Path.GetDirectoryName(root), Chunk7ProgressionSelection.SourceEvidenceLeaf);
            inventoryPath = Path.Combine(Path.GetDirectoryName(root), Chunk7ProgressionSelection.InventoryLeaf);
            var source = ReadPinned(sourceEvidencePath, selection.SourceArtifactSha256, out sourceEvidenceLength, out sourceEvidenceTicks);
            var inventory = ReadPinned(inventoryPath, selection.InventoryArtifactSha256, out inventoryLength, out inventoryTicks);
            var observed = source["observations"];
            var write = observed?["session"]?["nativeWrite"];
            var saved = write?["archive"] as JObject;
            var terminal = (observed?["session"]?["events"] as JArray)?.Children<JObject>()
                .Where(value => (string)value["kind"] == "native-progression-builder-complete").ToArray();
            Require((string)source["runId"] == selection.SourceRunId &&
                (string)source["scenario"] == Chunk7ProgressionSelection.Scenario &&
                (string)source["evidenceKind"] == EvidenceKind && (string)source["status"] == "PASS" &&
                (string)source["commit"] == request.Commit && (string)source["dllSha256"] == request.DllSha256 &&
                (string)source["dllMvid"] == request.DllMvid &&
                (string)observed?["selection"]?["case"] == Chunk7ProgressionSelection.SmallLevelCase &&
                (string)observed["selection"]["actorId"] == selection.ActorId &&
                (string)observed["selection"]["petId"] == selection.PetId &&
                (string)observed["selection"]["controlActorId"] == selection.ControlActorId &&
                (string)observed["selection"]["controlPetId"] == selection.ControlPetId &&
                (string)observed["selection"]["inventoryRunId"] == selection.InventoryRunId &&
                (string)observed["selection"]["inventoryArtifactSha256"] == selection.InventoryArtifactSha256 &&
                (string)write?["sha256"] == selection.SourceArchiveSha256 &&
                (string)write?["leaf"] == request.PersistenceLoad.FileName &&
                (string)write?["name"] == request.PersistenceLoad.InternalName &&
                terminal != null && terminal.Length == 1 &&
                (string)terminal[0]["actorId"] == selection.ActorId &&
                (string)terminal[0]["petId"] == selection.PetId &&
                (int?)terminal[0]["level"] == 5 && (int?)terminal[0]["xp"] == 15000 &&
                (int?)terminal[0]["petLevel"] != null && (int?)terminal[0]["money"] != null &&
                saved != null && (int?)saved["SchemaVersion"] == 3 &&
                (string)inventory["runId"] == selection.InventoryRunId &&
                (string)inventory["status"] == "PASS" &&
                (string)inventory["commit"] == request.Commit &&
                (string)inventory["dllSha256"] == request.DllSha256 &&
                (string)inventory["dllMvid"] == request.DllMvid,
                "Small level cold source lacks its exact same-candidate producer, mounted inventory and native Manual.");
            var archive = persistence.LoadedArchive;
            Require(archive != null && archive.SchemaVersion == 3 && archive.CampaignId == request.PersistenceLoad.GameId &&
                archive.AreaId == request.PersistenceLoad.Area && archive.Pairs.Length == 2 &&
                archive.Participations.Length == 0 && archive.Combat == null &&
                JToken.DeepEquals(saved, JObject.FromObject(archive, MountedSaveCodec.CreateSerializer())) &&
                persistence.SemanticRestoreCount == 4 && persistence.PresentationRestoreCount == 2 &&
                persistence.ArchivePostLoadObservedCount == 4 && persistence.SnapshotCount == 0 &&
                persistence.CompletedSaveCount == 0,
                "Small level cold Manual did not restore its exact two-pair saved graph once.");
            rider = Exact(selection.ActorId); mount = Exact(selection.PetId);
            controlRider = Exact(selection.ControlActorId); controlMount = Exact(selection.ControlPetId);
            actors = new[] { rider, mount, controlRider, controlMount };
            var producerOutcome = observed?["nativeLevelOutcome"] as JObject;
            Require(producerOutcome != null && JToken.DeepEquals(producerOutcome, CaptureLevelNativeOutcome()),
                "Small level cold lost the producer's native Ranger or sourced Horse progression.");
            Require(actors.All(value => value.IsInState && value.IsInGame && value.View?.AgentASP != null) &&
                archive.Pairs.Count(value => value.Rider.Id == selection.ActorId && value.Mount.Id == selection.PetId &&
                    value.ProfileId == "small-default-halfling-horse-v1") == 1 &&
                archive.Pairs.Count(value => value.Rider.Id == selection.ControlActorId && value.Mount.Id == selection.ControlPetId &&
                    value.ProfileId == SupportedMountedProfiles.Horse.Id) == 1,
                "Small level cold Manual changed an actor or mounted profile role.");
            var sourceRows = inventory["observations"]?["inheritedFixture"]?["arrangement"]?["actors"] as JArray;
            var field = Chunk7NativeFieldRoster.Capture(Game.Instance.Player);
            Require(sourceRows != null && sourceRows.Count == 7 && field.Length == 7 &&
                field.All(actor => actor != null && actor.IsInGame && actor.IsInState && actor.View?.AgentASP != null &&
                    sourceRows.Count(row => (string)row["actorId"] == actor.UniqueId &&
                        (string)row["blueprint"] == actor.Blueprint.AssetGuid &&
                        (ReferenceEquals(actor, rider) || ReferenceEquals(actor, mount) ||
                         (int)row["characterLevel"] == actor.Descriptor.Progression.CharacterLevel &&
                         (int)row["experience"] == actor.Descriptor.Progression.Experience)) == 1),
                "Small level cold field roster differs from the seven exact qualified native actors.");
            foreach (var pair in archive.Pairs)
            {
                var actor = Exact(pair.Rider.Id); var pet = Exact(pair.Mount.Id); var owner = world.Registry.FindActive(actor);
                Require(owner != null && owner.Id == Guid.Parse(pair.PairId) && owner.Generation == pair.Generation &&
                    ReferenceEquals(owner.Mount, pet) && owner.Boundary.State == RelationshipState.Mounted &&
                    ReferenceEquals(actor.Descriptor.Pet, pet) && ReferenceEquals(pet.Descriptor.Master.Value, actor),
                    "Small level cold lost one exact reciprocal native owner.");
            }
            var ids = actors.Select(value => value.UniqueId).ToArray();
            Require(world.Registry.Owners.Length == 2 && restoration != null && restoration.Complete &&
                restoration.Count("prepare-before", ids) == 0 && restoration.Count("round-before", ids) == 0 &&
                restoration.Count("restore-debt-before", ids) == 4 && restoration.Count("restore-debt-after", ids) == 4,
                "Small level cold replayed preparation or did not restore four native debts once.");
            AssertRestoredDebt(archive);
            var slots = world.NativeControls.CapturePersistentSlots();
            Require(slots.Length == archive.Slots.Length && archive.Slots.All(row => slots.Count(value =>
                value.ActorId == row.ActorId && value.Index == row.Index && value.Kind == row.Kind) == 1) &&
                world.NativeControls.NativeCastRequestCount == 0,
                "Small level cold changed native control slots or replayed a cast.");
            evidence["source"] = new JObject { ["sourceRunId"] = selection.SourceRunId,
                ["sourceArtifactSha256"] = selection.SourceArtifactSha256,
                ["sourceArchiveSha256"] = selection.SourceArchiveSha256,
                ["inventoryRunId"] = selection.InventoryRunId,
                ["inventoryArtifactSha256"] = selection.InventoryArtifactSha256,
                ["sourceProcessId"] = (int)observed["processId"], ["sameCandidate"] = true };
            evidence["loadedDescriptor"] = Chunk7EvidenceDto.Request(request.PersistenceLoad);
            evidence["loadedArchive"] = JObject.FromObject(archive, MountedSaveCodec.CreateSerializer());
            evidence["processId"] = Process.GetCurrentProcess().Id;
            evidence["selection"] = Chunk7EvidenceDto.Request(selection);
            evidence["authorityBefore"] = AuthoritySnapshot(); evidence["beforeOwners"] = Owners();
            targetDescriptor = rider.Descriptor; targetView = rider.View;
            petDescriptor = mount.Descriptor; petView = mount.View;
            scope = new Chunk7NativeWorldScope(request, Game.Instance.Player.AllCharacters.Concat(Game.Instance.Player.ControllableCharacters)
                .Where(value => value != null && !ReferenceEquals(value, rider) && !ReferenceEquals(value, mount)).Distinct());
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray();
            pause = Game.Instance.IsPaused; captured = true;
            coldExpected = new JObject { ["targetLevel"] = terminal[0]["level"].DeepClone(),
                ["targetXp"] = terminal[0]["xp"].DeepClone(), ["petLevel"] = terminal[0]["petLevel"].DeepClone(),
                ["money"] = terminal[0]["money"].DeepClone() };
            coldInitial = CaptureLevelColdState(); coldFirstFrame = UnityEngine.Time.frameCount;
            Require(JToken.DeepEquals(coldExpected["targetLevel"], coldInitial["targetLevel"]) &&
                JToken.DeepEquals(coldExpected["targetXp"], coldInitial["targetXp"]) &&
                JToken.DeepEquals(coldExpected["petLevel"], coldInitial["petLevel"]) &&
                JToken.DeepEquals(coldExpected["money"], coldInitial["money"]),
                "Small level cold native progression differs from accepted producer output.");
            evidence["producerTerminal"] = coldExpected.DeepClone();
            evidence["producerNativeLevelOutcome"] = producerOutcome.DeepClone();
            evidence["coldInitial"] = coldInitial.DeepClone(); evidence["coldInitialFrame"] = coldFirstFrame;
            Pass("C7-progression-cold-source");
        }
        private JObject CaptureLevelColdState()
        {
            Require(ReferenceEquals(Exact(selection.ActorId), rider) && ReferenceEquals(Exact(selection.PetId), mount) &&
                ReferenceEquals(Exact(selection.ControlActorId), controlRider) &&
                ReferenceEquals(Exact(selection.ControlPetId), controlMount) &&
                Game.Instance.CurrentMode == GameModeType.Default && !Game.Instance.Player.IsInCombat &&
                actors.All(value => !value.IsInCombat && !value.CombatState.Prepared &&
                    value.Commands != null && value.Commands.Empty && value.Commands.Queue.Count == 0) &&
                restoration != null && restoration.Complete &&
                restoration.Count("prepare-before", actors.Select(value => value.UniqueId)) == 0 &&
                restoration.Count("round-before", actors.Select(value => value.UniqueId)) == 0 &&
                restoration.Count("restore-debt-before", actors.Select(value => value.UniqueId)) == 4 &&
                restoration.Count("restore-debt-after", actors.Select(value => value.UniqueId)) == 4 &&
                persistence.SnapshotCount == 0 && persistence.CompletedSaveCount == 0 &&
                world.NativeControls.NativeCastRequestCount == 0,
                "Small level cold changed actor, preparation, command or native write custody.");
            return new JObject { ["targetLevel"] = rider.Descriptor.Progression.CharacterLevel,
                ["targetXp"] = rider.Descriptor.Progression.Experience,
                ["petLevel"] = mount.Descriptor.Progression.CharacterLevel,
                ["petXp"] = mount.Descriptor.Progression.Experience,
                ["controlLevel"] = controlRider.Descriptor.Progression.CharacterLevel,
                ["controlXp"] = controlRider.Descriptor.Progression.Experience,
                ["controlPetLevel"] = controlMount.Descriptor.Progression.CharacterLevel,
                ["controlPetXp"] = controlMount.Descriptor.Progression.Experience,
                ["money"] = Game.Instance.Player.Money,
                ["nativeLevelOutcome"] = CaptureLevelNativeOutcome(),
                ["owners"] = Owners()["owners"].DeepClone(),
                ["fieldRoster"] = new JArray(Chunk7NativeFieldRoster.Capture(Game.Instance.Player)
                    .OrderBy(value => value.UniqueId, StringComparer.Ordinal)
                    .Select(value => new JObject { ["actorId"] = value.UniqueId,
                        ["blueprint"] = value.Blueprint.AssetGuid,
                        ["characterLevel"] = value.Descriptor.Progression.CharacterLevel,
                        ["experience"] = value.Descriptor.Progression.Experience })),
                ["slots"] = new JArray(world.NativeControls.CapturePersistentSlots()
                    .OrderBy(value => value.ActorId, StringComparer.Ordinal).ThenBy(value => value.Index)
                    .Select(value => new JObject { ["actorId"] = value.ActorId, ["index"] = value.Index, ["kind"] = value.Kind })),
                ["actors"] = new JArray(actors.OrderBy(value => value.UniqueId, StringComparer.Ordinal)
                    .Select(value => new JObject { ["id"] = value.UniqueId, ["inCombat"] = value.IsInCombat,
                        ["prepared"] = value.CombatState.Prepared })),
                ["snapshots"] = persistence.SnapshotCount, ["completedSaves"] = persistence.CompletedSaveCount,
                ["nativeCasts"] = world.NativeControls.NativeCastRequestCount };
        }
        private void TickLevelCold()
        {
            Require(captured && coldInitial != null, "Small level cold continuation was not admitted.");
            Require(JToken.DeepEquals(coldInitial, CaptureLevelColdState()),
                "Small level cold continuation changed its two-pair native state before input.");
            if (UnityEngine.Time.frameCount < coldFirstFrame + 5) return;
            evidence["coldContinuation"] = CaptureLevelColdState();
            evidence["coldContinuationFrame"] = UnityEngine.Time.frameCount;
            evidence["coldRestoration"] = restoration.Capture(); restoration.Dispose(); restoration = null;
            Pass("C7-progression-cold-continuation"); BeginCleanup();
        }
        // A second process consumes the genuine native retrain Manual. No native
        // choice, money, clock or save request belongs to this read-only case.
        private void StartCold()
        {
            Require(selection.IsColdOnly && Game.Instance.CurrentMode == GameModeType.Default &&
                !Game.Instance.Player.IsInCombat && !Game.Instance.CutsceneLock &&
                !Game.Instance.IsModeActive(GameModeType.Dialog) &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "Retrain cold continuation requires settled free-control exploration.");
            sourceEvidencePath = Path.Combine(Path.GetDirectoryName(root), Chunk7ProgressionSelection.SourceEvidenceLeaf);
            inventoryPath = Path.Combine(Path.GetDirectoryName(root), Chunk7ProgressionSelection.InventoryLeaf);
            var source = ReadPinned(sourceEvidencePath, selection.SourceArtifactSha256, out sourceEvidenceLength, out sourceEvidenceTicks);
            var inventory = ReadPinned(inventoryPath, selection.InventoryArtifactSha256, out inventoryLength, out inventoryTicks);
            var observed = source["observations"];
            var write = observed?["session"]?["nativeWrite"];
            var saved = write?["archive"] as JObject;
            var terminal = (observed?["session"]?["events"] as JArray)?.Children<JObject>()
                .Where(value => (string)value["kind"] == "native-progression-builder-complete").ToArray();
            Require((string)source["runId"] == selection.SourceRunId && (string)source["scenario"] == Chunk7ProgressionSelection.Scenario &&
                (string)source["evidenceKind"] == EvidenceKind && (string)source["status"] == "PASS" &&
                (string)source["commit"] == request.Commit && (string)source["dllSha256"] == request.DllSha256 &&
                (string)source["dllMvid"] == request.DllMvid &&
                (string)observed?["selection"]?["case"] == (selection.IsMediumRetrainColdSource ?
                    Chunk7ProgressionSelection.MediumRetrainMammothCase : Chunk7ProgressionSelection.SmallRetrainHorseCase) &&
                (string)observed["selection"]["actorId"] == selection.ActorId &&
                (string)observed["selection"]["petId"] == selection.FormerPetId &&
                (string)observed["selection"]["controlActorId"] == selection.ControlActorId &&
                (string)observed["selection"]["controlPetId"] == selection.ControlPetId &&
                (string)observed["selection"]["inventoryRunId"] == selection.InventoryRunId &&
                (string)observed["selection"]["inventoryArtifactSha256"] == selection.InventoryArtifactSha256 &&
                (string)write?["sha256"] == selection.SourceArchiveSha256 &&
                (string)write?["leaf"] == request.PersistenceLoad.FileName &&
                (string)write?["name"] == request.PersistenceLoad.InternalName &&
                (string)observed?["ownedRosterAfter"]?["currentPetId"] == selection.PetId &&
                (string)observed?["ownedRosterAfter"]?["originalPet"]?["id"] == selection.FormerPetId &&
                terminal != null && terminal.Length == 1 &&
                (string)terminal[0]["actorId"] == selection.ActorId &&
                (string)terminal[0]["petId"] == selection.PetId &&
                (int?)terminal[0]["level"] == (selection.IsMediumRetrainColdSource ? 10 : 4) &&
                (int?)terminal[0]["petLevel"] == (selection.IsMediumRetrainColdSource ? 6 : 2) &&
                (int?)terminal[0]["xp"] != null && (int?)terminal[0]["money"] != null &&
                saved != null && (int?)saved["SchemaVersion"] == 3 &&
                (string)inventory["runId"] == selection.InventoryRunId &&
                (string)inventory["status"] == "PASS" && (string)inventory["commit"] == request.Commit &&
                (string)inventory["dllSha256"] == request.DllSha256 && (string)inventory["dllMvid"] == request.DllMvid,
                "Retrain cold source lacks its exact same-candidate producer, mounted inventory and native Manual.");
            var archive = persistence.LoadedArchive;
            Require(archive != null && archive.SchemaVersion == 3 && archive.CampaignId == request.PersistenceLoad.GameId &&
                archive.AreaId == request.PersistenceLoad.Area && archive.Pairs.Length == 1 &&
                archive.Participations.Length == 0 && archive.Combat == null &&
                JToken.DeepEquals(saved, JObject.FromObject(archive, MountedSaveCodec.CreateSerializer())) &&
                persistence.SemanticRestoreCount == 2 && persistence.PresentationRestoreCount == 1 &&
                persistence.ArchivePostLoadObservedCount == 2 && persistence.SnapshotCount == 0 &&
                persistence.CompletedSaveCount == 0, "Cold retrain Manual did not restore its exact one-pair saved graph once.");
            rider = Exact(selection.ActorId); mount = Exact(selection.PetId);
            controlRider = Exact(selection.ControlActorId); controlMount = Exact(selection.ControlPetId);
            actors = new[] { rider, mount, controlRider, controlMount };
            Require(actors.All(value => value.IsInState && value.IsInGame && value.View?.AgentASP != null) &&
                !Game.Instance.State.Units.Concat(Game.Instance.Player.AllCharacters).Any(value => value?.UniqueId == selection.FormerPetId),
                "Cold retrain restored a missing field actor or retained the retired old horse.");
            Require(archive.Pairs[0].Rider.Id == controlRider.UniqueId && archive.Pairs[0].Mount.Id == controlMount.UniqueId &&
                archive.Pairs[0].ProfileId == SupportedMountedProfiles.Horse.Id &&
                archive.Slots.All(value => value.ActorId == controlRider.UniqueId) && archive.Slots.Length == 2,
                "Cold retrain archive lost the sole independent Medium control owner or slots.");
            var owner = world.Registry.FindActive(controlRider);
            Require(world.Registry.Owners.Length == 1 && owner != null && owner.Id == Guid.Parse(archive.Pairs[0].PairId) &&
                owner.Generation == archive.Pairs[0].Generation && ReferenceEquals(owner.Mount, controlMount) &&
                owner.Boundary.State == RelationshipState.Mounted &&
                world.Registry.FindActive(rider) == null &&
                ReferenceEquals(rider.Descriptor.Pet, mount) &&
                ReferenceEquals(mount.Descriptor.Master.Value, rider) &&
                ReferenceEquals(controlRider.Descriptor.Pet, controlMount) &&
                ReferenceEquals(controlMount.Descriptor.Master.Value, controlRider),
                "Cold retrain reassigned the target, new pet or surviving mounted control owner.");
            var ids = new[] { controlRider.UniqueId, controlMount.UniqueId };
            Require(restoration != null && restoration.Complete && restoration.Count("prepare-before", ids) == 0 &&
                restoration.Count("round-before", ids) == 0 && restoration.Count("restore-debt-before", ids) == 2 &&
                restoration.Count("restore-debt-after", ids) == 2,
                "Cold retrain replayed native preparation or omitted exact control debt restoration.");
            AssertRestoredDebt(archive);
            var slots = world.NativeControls.CapturePersistentSlots();
            Require(slots.Length == archive.Slots.Length && archive.Slots.All(row =>
                slots.Count(value => value.ActorId == row.ActorId && value.Index == row.Index && value.Kind == row.Kind) == 1) &&
                world.NativeControls.NativeCastRequestCount == 0,
                "Cold retrain changed control slots or replayed a Mount cast.");
            evidence["source"] = new JObject { ["sourceRunId"] = selection.SourceRunId,
                ["sourceArtifactSha256"] = selection.SourceArtifactSha256,
                ["sourceArchiveSha256"] = selection.SourceArchiveSha256,
                ["inventoryRunId"] = selection.InventoryRunId,
                ["inventoryArtifactSha256"] = selection.InventoryArtifactSha256,
                ["sourceProcessId"] = (int)observed["processId"], ["sameCandidate"] = true };
            evidence["loadedDescriptor"] = Chunk7EvidenceDto.Request(request.PersistenceLoad);
            evidence["loadedArchive"] = JObject.FromObject(archive, MountedSaveCodec.CreateSerializer());
            evidence["processId"] = Process.GetCurrentProcess().Id;
            evidence["selection"] = Chunk7EvidenceDto.Request(selection);
            evidence["authorityBefore"] = AuthoritySnapshot();
            evidence["beforeOwners"] = Owners();
            targetDescriptor = rider.Descriptor; targetView = rider.View;
            petDescriptor = mount.Descriptor; petView = mount.View;
            scope = new Chunk7NativeWorldScope(request, Game.Instance.Player.AllCharacters.Concat(Game.Instance.Player.ControllableCharacters)
                .Where(value => value != null && !ReferenceEquals(value, rider) && !ReferenceEquals(value, mount)).Distinct());
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray();
            pause = Game.Instance.IsPaused; captured = true;
            var priorBody = observed["nativeBodyBefore"];
            Require(priorBody is JObject && !string.IsNullOrEmpty((string)priorBody["RaceGuid"]) &&
                !string.IsNullOrEmpty((string)priorBody["PresetGuid"]) &&
                !string.IsNullOrEmpty((string)priorBody["SkeletonName"]) &&
                !string.IsNullOrEmpty((string)priorBody["Gender"]) &&
                (bool?)priorBody["LeftHanded"] != null &&
                (int?)priorBody["OriginalSizeOrdinal"] == (selection.IsMediumRetrainColdSource ? 4 : 3) &&
                (int?)priorBody["EffectiveSizeOrdinal"] == (selection.IsMediumRetrainColdSource ? 4 : 3),
                "Accepted retrain producer lacks its pinned paid native body.");
            coldExpected = new JObject { ["targetLevel"] = terminal[0]["level"].DeepClone(),
                ["targetXp"] = terminal[0]["xp"].DeepClone(), ["newPetLevel"] = terminal[0]["petLevel"].DeepClone(),
                ["nativeMoney"] = terminal[0]["money"].DeepClone(),
                ["targetBlueprint"] = (string)observed["source"]?["inventoryActor"]?["blueprint"],
                ["newPetBlueprint"] = selection.IsMediumRetrainColdSource ?
                    HorseCompanionBlueprintService.MammothUnitGuid : (string)priorBody["MountBlueprintGuid"],
                ["nativeBody"] = new JObject { ["raceGuid"] = (string)priorBody["RaceGuid"],
                    ["presetGuid"] = (string)priorBody["PresetGuid"],
                    ["skeletonName"] = (string)priorBody["SkeletonName"],
                    ["gender"] = (string)priorBody["Gender"],
                    ["leftHanded"] = (bool)priorBody["LeftHanded"],
                    ["originalSizeOrdinal"] = (int)priorBody["OriginalSizeOrdinal"],
                    ["effectiveSizeOrdinal"] = (int)priorBody["EffectiveSizeOrdinal"] } };
            coldInitial = CaptureColdState(); coldFirstFrame = UnityEngine.Time.frameCount;
            Require(coldExpected.Properties().All(value => value.Value.Type != JTokenType.Null) &&
                coldExpected.Properties().All(value => JToken.DeepEquals(value.Value, coldInitial[value.Name])),
                "Cold retrain native progression or money differs from the accepted producer output.");
            evidence["producerTerminal"] = coldExpected.DeepClone();
            evidence["coldInitial"] = coldInitial.DeepClone();
            evidence["coldInitialFrame"] = coldFirstFrame;
            Pass("C7-progression-cold-source");
        }

        private JObject CaptureColdState()
        {
            Require(ReferenceEquals(Exact(selection.ActorId), rider) && ReferenceEquals(Exact(selection.PetId), mount) &&
                ReferenceEquals(Exact(selection.ControlActorId), controlRider) &&
                ReferenceEquals(Exact(selection.ControlPetId), controlMount),
                "Cold retrain actor membership or object identity changed after restoration.");
            Require(Game.Instance.CurrentMode == GameModeType.Default && !Game.Instance.Player.IsInCombat &&
                actors.All(value => !value.IsInCombat && !value.CombatState.Prepared) && restoration != null && restoration.Complete &&
                restoration.Count("prepare-before", actors.Select(value => value.UniqueId)) == 0 &&
                restoration.Count("round-before", actors.Select(value => value.UniqueId)) == 0 &&
                restoration.Count("restore-debt-before", actors.Select(value => value.UniqueId)) == 2 &&
                restoration.Count("restore-debt-after", actors.Select(value => value.UniqueId)) == 2,
                "Cold retrain entered combat, replayed native preparation or restored debt again.");
            var owner = world.Registry.FindActive(controlRider);
            Require(owner != null && world.Registry.Owners.Length == 1 &&
                world.Registry.FindActive(rider) == null &&
                ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider) &&
                ReferenceEquals(controlRider.Descriptor.Pet, controlMount) && ReferenceEquals(controlMount.Descriptor.Master.Value, controlRider) &&
                !Game.Instance.State.Units.Concat(Game.Instance.Player.AllCharacters).Any(value => value?.UniqueId == selection.FormerPetId) &&
                actors.All(value => value.Commands != null && value.Commands.Empty && value.Commands.Queue.Count == 0) &&
                persistence.SnapshotCount == 0 && persistence.CompletedSaveCount == 0 &&
                world.NativeControls.NativeCastRequestCount == 0,
                "Cold retrain continuation changed pet, owner, commands or native write custody.");
            var body = NativeSmallRiderBody.Capture(rider, mount);
            Require(body != null && body.NativeRigAvailable && body.NativeRaceAndPresetIdentityExact &&
                body.NativeSkeletonIsExactPreset && !body.RiderPolymorphed && !body.MountPolymorphed,
                "Cold retrain native body or rig changed after restoration.");
            var mammoth = selection.IsMediumRetrainColdSource ? CaptureColdMammoth() : null;
            var state = new JObject { ["targetId"] = rider.UniqueId, ["newPetId"] = mount.UniqueId,
                ["targetLevel"] = rider.Descriptor.Progression.CharacterLevel,
                ["targetXp"] = rider.Descriptor.Progression.Experience,
                ["targetBlueprint"] = rider.Blueprint?.AssetGuid,
                ["newPetLevel"] = mount.Descriptor.Progression.CharacterLevel,
                ["newPetBlueprint"] = mount.Blueprint?.AssetGuid,
                ["nativeBody"] = new JObject { ["raceGuid"] = body.RaceGuid,
                    ["presetGuid"] = body.PresetGuid, ["skeletonName"] = body.SkeletonName,
                    ["gender"] = body.Gender, ["leftHanded"] = body.LeftHanded,
                    ["originalSizeOrdinal"] = body.OriginalSizeOrdinal,
                    ["effectiveSizeOrdinal"] = body.EffectiveSizeOrdinal },
                ["oldPetAbsent"] = true, ["targetUnpaired"] = true,
                ["controlRiderId"] = controlRider.UniqueId, ["controlMountId"] = controlMount.UniqueId,
                ["controlOwnerId"] = owner.Id.ToString("N"), ["controlGeneration"] = owner.Generation,
                ["nativeMoney"] = Game.Instance.Player.Money, ["slots"] = new JArray(world.NativeControls.CapturePersistentSlots()
                    .OrderBy(value => value.ActorId, StringComparer.Ordinal).ThenBy(value => value.Index)
                    .Select(value => new JObject { ["actorId"] = value.ActorId, ["index"] = value.Index, ["kind"] = value.Kind })),
                ["actors"] = new JArray(actors.OrderBy(value => value.UniqueId, StringComparer.Ordinal)
                    .Select(value => new JObject { ["id"] = value.UniqueId,
                        ["inCombat"] = value.IsInCombat, ["prepared"] = value.CombatState.Prepared })),
                ["playerInCombat"] = Game.Instance.Player.IsInCombat,
                ["snapshots"] = persistence.SnapshotCount, ["completedSaves"] = persistence.CompletedSaveCount,
                ["nativeCasts"] = world.NativeControls.NativeCastRequestCount };
            if (mammoth != null) state["nativeMammoth"] = mammoth;
            return state;
        }

        private JObject CaptureColdMammoth()
        {
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.MammothFeatureGuid);
            var rank = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.LevelRankGuid);
            var upgrade = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.MammothUpgradeGuid);
            var sourced = feature == null ? null : rider.Descriptor.GetFact(feature) as Feature;
            Require(ranger != null && feature != null && rank != null && upgrade != null && sourced?.Source != null &&
                rider.Descriptor.Progression.GetClassLevel(ranger) == 10 && rider.Descriptor.Progression.CharacterLevel == 10 &&
                rider.Descriptor.GetFact(rank)?.GetRank() == 7 && mount.Descriptor.GetFact(upgrade) != null &&
                rider.Descriptor.OriginalSize == Kingmaker.Enums.Size.Medium && rider.Descriptor.State.Size == Kingmaker.Enums.Size.Medium &&
                mount.Blueprint?.AssetGuid == HorseCompanionBlueprintService.MammothUnitGuid &&
                mount.Descriptor.State.Size == Kingmaker.Enums.Size.Large &&
                rider.GetActivePolymorph() == null && mount.GetActivePolymorph() == null,
                "Cold Mammoth replacement lost its native Ranger-ten sourced rank-seven Large upgrade.");
            return new JObject { ["rangerLevel"] = 10, ["companionRank"] = rider.Descriptor.GetFact(rank).GetRank(),
                ["featureGuid"] = feature.AssetGuid, ["featureSourceGuid"] = sourced.Source.AssetGuid,
                ["upgradeGuid"] = upgrade.AssetGuid, ["petGuid"] = mount.Blueprint.AssetGuid,
                ["riderSize"] = rider.Descriptor.State.Size.ToString(), ["mountSize"] = mount.Descriptor.State.Size.ToString() };
        }

        private void TickCold()
        {
            Require(captured && coldInitial != null, "Cold retrain continuation was not admitted.");
            Require(JToken.DeepEquals(coldInitial, CaptureColdState()),
                "Cold retrain continuation changed its one-pair native state before input.");
            if (UnityEngine.Time.frameCount < coldFirstFrame + 5) return;
            evidence["coldContinuation"] = CaptureColdState();
            evidence["coldContinuationFrame"] = UnityEngine.Time.frameCount;
            evidence["coldRestoration"] = restoration.Capture(); restoration.Dispose(); restoration = null;
            Pass("C7-progression-cold-continuation"); BeginCleanup();
        }
    }
}
