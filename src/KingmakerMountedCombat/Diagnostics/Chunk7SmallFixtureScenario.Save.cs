using System;
using System.IO;
using System.Linq;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7SmallFixtureScenario
    {
        private Chunk7SmallFixtureSaveProbe saveProbe;
        private SaveInfo nativeSourceDescriptor, requestedManual;
        private bool saveRequested, saveCallback, saveProven;
        private int callbackCount;
        private void BeginManual()
        {
            RequireAuthority(false); RequireWorld(); RequireNeighbours(); RequireNoOwners(); RequireNoEffects(); VerifyInputs();
            Require(!saveRequested && field?.Completed == true && !field.Owned && !Busy() &&
                game.UI.CharacterBuildController.LevelUpController == null && game.UI.CharacterBuildController.Unit == null &&
                game.CurrentMode == GameModeType.Default && !game.Player.IsInCombat && !game.CutsceneLock &&
                ReferenceEquals(game.Player.SavedInArea, game.CurrentlyLoadedArea) && manager.IsSaveAllowed() &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled && !loadProbe.InSynchronousCallback &&
                authority.AdmittedNativeLoadRequests == 1 && authority.AdmittedNativeWriteRequests == 0 && authority.SmallFixtureReservations == 0,
                "F09 completed native choices/field must settle before its separate genuine Manual request.");
            var before = manager.Take(3).ToArray();
            var source = before.Length == 1 ? before[0] : null;
            // LoadZipSave creates the loader's descriptor separately from the
            // SaveManager list. Pin that list's exact source object for the
            // post-write inventory, while proving both describe the same
            // immutable archive under the isolated authority.
            Require(source != null && loader.Descriptor != null && source.Type == SaveInfo.SaveType.Manual &&
                source.IsActuallySaved && source.HasFileOnDisk && source.OperationState == SaveInfo.StateType.None &&
                source.FileName == request.PersistenceLoad.FileName && source.FileName == loader.Descriptor.FileName &&
                source.FolderName == loader.Descriptor.FolderName && source.Name == loader.Descriptor.Name &&
                source.GameId == loader.Descriptor.GameId && source.GameName == loader.Descriptor.GameName &&
                source.Area?.AssetGuidThreadSafe == request.PersistenceLoad.Area &&
                source.Area?.AssetGuidThreadSafe == loader.Descriptor.Area?.AssetGuidThreadSafe &&
                !source.IsAutoLevelupSave && authority.ValidateArchiveIdentity(Target(source), authority.Root) == null,
                "F09 native save inventory differs from its sole immutable source (count=" + before.Length +
                ", loadedIsList=" + ReferenceEquals(source, loader.Descriptor) + ").");
            nativeSourceDescriptor = source;
            var number = manager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual);
            var leaf = Chunk7SmallFixtureSelection.ManualLeaf(number);
            Require(!File.Exists(Path.Combine(authority.Root, leaf)), "F09 native Manual output already exists.");
            authority.ReserveSmallFixtureOutput(request, game, manager, number);
            Require(authority.SmallFixtureOutputLeaf == leaf && authority.SmallFixtureReservations == 1,
                "F09 native Manual reservation did not retain the exact observed slot.");
            evidence["nativeNumbering"] = new JObject { ["method"] = "FindUnusedSaveNumber", ["token"] = "06008024",
                ["moduleMvid"] = typeof(SaveManager).Module.ModuleVersionId.ToString("D"), ["number"] = number,
                ["fileName"] = leaf, ["outputInitiallyAbsent"] = true, ["slotProjectionEnabled"] = false };
            saveProbe = new Chunk7SmallFixtureSaveProbe(game, persistence, loadProbe.Identity, RequireWorld, leaf); saveProbe.Install();
            requestedManual = manager.CreateNewSave(Chunk7SmallFixtureSelection.OutputName);
            Require(requestedManual != null && requestedManual.Type == SaveInfo.SaveType.Manual &&
                requestedManual.Name == Chunk7SmallFixtureSelection.OutputName && !requestedManual.IsActuallySaved && !requestedManual.HasFileOnDisk &&
                !requestedManual.IsAutoLevelupSave && ReferenceEquals(requestedManual.Area, game.CurrentlyLoadedArea) && manager.IsSaveAllowed(),
                "F09 native new-save descriptor is not the exact separate Manual.");
            saveProbe.Expect(requestedManual);
            saveRequested = true; stage = 6;
            evidence["saveRequested"] = new JObject { ["frame"] = UnityEngine.Time.frameCount, ["descriptor"] = loadProbe.Identity(requestedManual),
                ["name"] = requestedManual.Name, ["type"] = requestedManual.Type.ToString(), ["authority"] = AuthoritySnapshot() };
            game.SaveGame(requestedManual, () => { callbackCount++; saveCallback = true; saveProbe.ObserveCallback(); });
        }
        private void PollManual()
        {
            RequireAuthority(false); RequireWorld(); RequireNeighbours(); RequireNoOwners(); RequireNoEffects();
            if (!saveCallback) return;
            Require(callbackCount == 1 && saveProbe?.Complete == true && !Busy() &&
                authority.AdmittedNativeLoadRequests == 1 && authority.AdmittedNativeWriteRequests == 1 &&
                authorization.AuthorizedLoadCount == 1 && authorization.AuthorizedWriteCount == 1 &&
                persistence.SnapshotCount == 1 && persistence.CompletedSaveCount == 1,
                "F09 genuine Manual must finish its one request, prepared write, snapshot, worker and callback.");
            var completion = persistence.LastCompletedSave; var written = saveProbe.Written;
            Require(completion != null && completion.Sequence == 1 && ReferenceEquals(completion.Requested, requestedManual) &&
                ReferenceEquals(completion.Written, written) && completion.Path == Path.Combine(authority.Root, authority.SmallFixtureOutputLeaf) &&
                written.Type == SaveInfo.SaveType.Manual && written.Name == Chunk7SmallFixtureSelection.OutputName &&
                written.FileName == authority.SmallFixtureOutputLeaf && written.GameId == request.PersistenceLoad.GameId &&
                written.GameName == request.PersistenceLoad.GameName && written.Area?.AssetGuidThreadSafe == request.PersistenceLoad.Area &&
                written.IsActuallySaved && written.HasFileOnDisk && written.OperationState == SaveInfo.StateType.None && !written.IsAutoLevelupSave,
                "F09 Manual completion does not own its exact requested/prepared descriptors and reserved native leaf.");
            var saves = manager.Take(3).ToArray();
            Require(saves.Length == 2 && saves.Count(value => ReferenceEquals(value, nativeSourceDescriptor)) == 1 &&
                saves.Count(value => ReferenceEquals(value, written)) == 1 && saves.All(value =>
                    authority.ValidateArchiveIdentity(Target(value), authority.Root) == null),
                "F09 final isolated inventory must retain its original seed and only the genuine Manual output.");
            var read = NativeMountedSaveStorage.ReadArchive(written.Saver); var archive = read.Data;
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 3 && archive != null,
                "F09 native output must have actual current schema-3 metadata read back from its written archive.");
            RequireEmptyArchive(archive, request.PersistenceLoad.GameId, request.PersistenceLoad.Area);
            var after = Arrangement(); var body = NativeSmallRiderBody.Capture(actor, pet); RequireSmallBody(body);
            Require(JToken.DeepEquals(arrangementBeforeSave, after) && JToken.DeepEquals(bodyBeforeSave, CaptureBody(body)),
                "F09 save changed native ownership, XP, money, progression, field membership or live body/equipment.");
            Require(restoration.Complete && restoration.Capture()["events"] is JArray events && events.Count == 0 &&
                !controls.SerializationSuspended && !persistence.SaveSuspended,
                "F09 save observed preparation/restoration or retained a serialization lease.");
            VerifyInputs();
            var file = new FileInfo(completion.Path);
            Require(file.Exists && file.Length > 0 && (file.Attributes & FileAttributes.ReparsePoint) == 0 && file.FullName == written.FolderName,
                "F09 Manual did not materialize as its exact regular run-owned file.");
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["fileName"] = written.FileName,
                ["internalName"] = written.Name, ["type"] = written.Type.ToString(), ["sha256"] = Hash(file.FullName), ["length"] = file.Length,
                ["lastWriteTimeUtcTicks"] = file.LastWriteTimeUtc.Ticks, ["gameId"] = written.GameId, ["gameName"] = written.GameName,
                ["area"] = written.Area.AssetGuidThreadSafe, ["operationState"] = written.OperationState.ToString(),
                ["isActuallySaved"] = written.IsActuallySaved, ["hasFileOnDisk"] = written.HasFileOnDisk, ["autoLevelup"] = written.IsAutoLevelupSave,
                ["requestedDescriptor"] = loadProbe.Identity(requestedManual), ["writtenDescriptor"] = loadProbe.Identity(written),
                ["completionSequence"] = completion.Sequence, ["callbackCount"] = callbackCount, ["nativeWorkerSettled"] = true,
                ["snapshotCount"] = persistence.SnapshotCount, ["readKind"] = read.Kind.ToString(), ["sourceSchema"] = read.SourceSchema,
                ["archive"] = JObject.FromObject(archive, MountedSaveCodec.CreateSerializer()), ["slotProjectionEnabled"] = false };
            evidence["afterSaveArrangement"] = after; evidence["savedBodyObservation"] = CaptureBody(body);
            evidence["savedWorld"] = WorldSnapshot(); evidence["settledNativeHireCost"] = game.Player.GetCustomCompanionCost();
            evidence["settledDescriptors"] = new JArray(saves.Select(value => new JObject { ["fileName"] = value.FileName,
                ["internalName"] = value.Name, ["type"] = value.Type.ToString(), ["descriptor"] = loadProbe.Identity(value),
                ["gameId"] = value.GameId, ["gameName"] = value.GameName, ["area"] = value.Area.AssetGuidThreadSafe,
                ["isActuallySaved"] = value.IsActuallySaved, ["hasFileOnDisk"] = value.HasFileOnDisk,
                ["operationState"] = value.OperationState.ToString(), ["autoLevelup"] = value.IsAutoLevelupSave }));
            saveProven = true; Pass("C7-small-fixture-native-manual"); BeginCleanup();
        }
        internal static void RequireEmptyArchive(MountedArchiveData archive, string campaign, string area)
        {
            Require(archive != null, "F09 native schema-3 archive is absent."); archive.Validate();
            Require(archive.SchemaVersion == 3 && archive.CampaignId == campaign && archive.AreaId == area &&
                archive.Pairs.Length == 0 && archive.Participations.Length == 0 && archive.Slots.Length == 0 && archive.Combat == null,
                "F09 unmounted fixture Manual must have zero pairs, retained participation, persistent slots and combat.");
        }
    }
}
