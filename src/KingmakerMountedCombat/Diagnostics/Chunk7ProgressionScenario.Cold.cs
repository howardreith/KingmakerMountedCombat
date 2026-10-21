using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7ProgressionScenario
    {
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
                (string)observed?["selection"]?["case"] == Chunk7ProgressionSelection.SmallRetrainHorseCase &&
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
                (int?)terminal[0]["level"] == 4 && (int?)terminal[0]["petLevel"] == 2 &&
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
                (int?)priorBody["OriginalSizeOrdinal"] == 3 &&
                (int?)priorBody["EffectiveSizeOrdinal"] == 3,
                "Accepted retrain producer lacks its pinned Small native body.");
            coldExpected = new JObject { ["targetLevel"] = terminal[0]["level"].DeepClone(),
                ["targetXp"] = terminal[0]["xp"].DeepClone(), ["newPetLevel"] = terminal[0]["petLevel"].DeepClone(),
                ["nativeMoney"] = terminal[0]["money"].DeepClone(),
                ["targetBlueprint"] = (string)observed["source"]?["inventoryActor"]?["blueprint"],
                ["newPetBlueprint"] = (string)priorBody["MountBlueprintGuid"],
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
                actors.All(value => !value.IsInCombat) && restoration != null && restoration.Complete &&
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
            return new JObject { ["targetId"] = rider.UniqueId, ["newPetId"] = mount.UniqueId,
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
