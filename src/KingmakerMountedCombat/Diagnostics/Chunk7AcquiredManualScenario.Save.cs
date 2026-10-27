using System;
using System.IO;
using System.Linq;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7AcquiredManualScenario
    {
        private Chunk7ManualSeedSaveProbe saveProbe;
        private SaveInfo requestedManual;
        private bool saveRequested, saveCallback, saveProven;
        private int callbackCount;
        private JObject noArrangementBefore;
        private string OutputLeaf => authority.AcquiredManualOutputLeaf;

        private JObject CaptureNoArrangement() => new JObject { ["money"] = game.Player.Money, ["experienceRatePercent"] = game.Player.ExperienceRatePercent,
            ["actors"] = new JArray(OwnedActors().OrderBy(actor => actor.UniqueId, StringComparer.Ordinal).Select(actor => new JObject {
                ["actorId"] = actor.UniqueId, ["blueprint"] = actor.Blueprint.AssetGuid,
                ["experience"] = actor.Descriptor.Progression.Experience, ["characterLevel"] = actor.Descriptor.Progression.CharacterLevel,
                ["classes"] = new JArray(actor.Descriptor.Progression.Classes.OrderBy(value => value.CharacterClass.AssetGuid, StringComparer.Ordinal)
                    .Select(value => new JObject { ["classGuid"] = value.CharacterClass.AssetGuid, ["level"] = value.Level })) })) };

        private void RequireSaveWorldReferences()
        {
            // The owned native writer temporarily turns actors off and clears
            // CrossSceneState. Retain world identity while the existing worker
            // barrier owns that interval; do not enumerate its transient party.
            Require(ReferenceEquals(nativePlayer, game.Player) && ReferenceEquals(state, game.State) &&
                ReferenceEquals(areaState, game.State?.LoadedAreaState) && ReferenceEquals(mainState, game.State?.LoadedAreaState?.MainState) &&
                ReferenceEquals(area, game.CurrentlyLoadedArea) && ReferenceEquals(mainActor, game.Player?.MainCharacter.Value) &&
                game.Player.GameId == request.PersistenceLoad.GameId && game.Player.MainCharacter.Value.UniqueId == expectedMainId,
                "Owned native save lost the exact admitted world while serialization was pending.");
            RequireTwoOwnerReferences();
        }

        private void BeginManualSave()
        {
            RequireAuthority(false); RequireWorld(); RequireNoOwners(); RequireNoEffects(); VerifySourceBytes();
            Require(fieldProven && !fieldTransitionOpen && (fieldAdmission == null || !fieldAdmission.Owned) && !saveRequested && !Busy() && authority.MatchesAcquiredManualPlan(request.PersistenceLoad, true, true) &&
                authority.AdmittedNativeLoadRequests == 1 && authorization.AuthorizedLoadCount == 1 &&
                game.CurrentMode == Kingmaker.GameModes.GameModeType.Default && !game.Player.IsInCombat && !game.CutsceneLock &&
                game.Player.SavedInArea == game.CurrentlyLoadedArea && actors.All(actor => actor.Commands.Empty) && manager.IsSaveAllowed() &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled, "Fresh native Acquired Manual derivative dispatch is not eligible.");
            var saves = manager.Take(3).ToArray();
            Require(saves.Length == 1 && saves[0].Type.ToString() == request.ExpectedNativeLoadType && saves[0].FileName == request.PersistenceLoad.FileName &&
                saves[0].IsActuallySaved && saves[0].IsAutoLevelupSave == (TwoOwners && !PostFieldManual) &&
                ReferenceEquals(saves[0], authority.AcquiredManualSourceDescriptor) && saves[0].OperationState == SaveInfo.StateType.None,
                "Acquired Manual derivative native source inventory changed before numbering.");
            var number = manager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual);
            var observedLeaf = "Manual_" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_" + Chunk7AcquiredManualSelection.OutputName + ".zks";
            Require(number == authority.AcquiredManualNumber && observedLeaf == OutputLeaf && !File.Exists(Path.Combine(root, observedLeaf)),
                "Native Manual numbering/leaf differs from the declared fresh output.");
            evidence["nativeNumbering"] = new JObject { ["method"] = "FindUnusedSaveNumber", ["token"] = "06008024", ["moduleMvid"] = typeof(SaveManager).Module.ModuleVersionId.ToString("D"),
                ["frame"] = UnityEngine.Time.frameCount, ["nativeSaveType"] = "Manual", ["number"] = number, ["fileName"] = observedLeaf,
                ["sourceDescriptors"] = evidence["fixtureReadiness"]["isolatedSaveDescriptors"]["descriptors"].DeepClone(), ["outputInitiallyAbsent"] = true, ["slotProjectionEnabled"] = false };
            Pass("C7-acquired-manual-numbering");
            Require(restoration.Complete && ((JArray)restoration.Capture()["events"]).Count == 0 &&
                !probe.InSynchronousCallback && (saveProbe == null || !saveProbe.InCallback), "Native preparation or observer callback prevents fresh Manual dispatch.");
            noArrangementBefore = CaptureNoArrangement(); evidence["beforeSaveArrangement"] = noArrangementBefore.DeepClone();
            saveProbe = new Chunk7ManualSeedSaveProbe(game, persistence, probe.Identity, RequireWorld, OutputLeaf, Chunk7AcquiredManualSelection.OutputName); saveProbe.Install();
            // Retain the exact descriptor before any later validation or dispatch.
            requestedManual = manager.CreateNewSave(Chunk7AcquiredManualSelection.OutputName);
            Require(requestedManual != null && requestedManual.Type == SaveInfo.SaveType.Manual && requestedManual.Name == Chunk7AcquiredManualSelection.OutputName &&
                !requestedManual.IsActuallySaved && !requestedManual.HasFileOnDisk && !requestedManual.IsAutoLevelupSave &&
                requestedManual.Area == game.CurrentlyLoadedArea && manager.IsSaveAllowed(), "Native new Manual descriptor is not the exact fresh cumulative derivative.");
            saveProbe.Expect(requestedManual);
            evidence["saveRequested"] = new JObject { ["frame"] = UnityEngine.Time.frameCount, ["descriptor"] = probe.Identity(requestedManual),
                ["name"] = requestedManual.Name, ["type"] = requestedManual.Type.ToString(), ["nativeSaveAllowed"] = true, ["newSlot"] = true,
                ["snapshotCount"] = persistence.SnapshotCount, ["authority"] = AuthoritySnapshot() };
            saveRequested = true;
            game.SaveGame(requestedManual, () => { callbackCount++; saveCallback = true; saveProbe.ObserveCallback(); });
        }
        private void PollManualSave()
        {
            RequireAuthority(false); RequireWorld(); RequireNoOwners(); RequireNoEffects();
            if (!saveCallback) return;
            Require(callbackCount == 1 && saveProbe != null && saveProbe.Complete &&
                authority.AdmittedNativeLoadRequests == 1 && authority.AdmittedNativeWriteRequests == 1 &&
                authorization.AuthorizedLoadCount == 1 && authorization.AuthorizedWriteCount == 1 &&
                persistence.SnapshotCount == 1 && persistence.CompletedSaveCount == 1 && !Busy(), "Acquired Manual derivative did not complete its exact one-load one-write native path.");
            var completion = persistence.LastCompletedSave;
            Require(completion != null && completion.Sequence == 1 && ReferenceEquals(completion.Requested, requestedManual) &&
                ReferenceEquals(completion.Written, saveProbe.Written) && completion.Path == Path.Combine(root, OutputLeaf),
                "Native Manual completion belongs to another requested/prepared descriptor.");
            var written = completion.Written; var saves = manager.Take(3).ToArray();
            Require(saves.Length == 2 && saves.Count(value => value.FileName == request.PersistenceLoad.FileName &&
                value.Type.ToString() == request.ExpectedNativeLoadType && value.IsAutoLevelupSave == (TwoOwners && !PostFieldManual)) == 1 &&
                saves.Count(value => ReferenceEquals(value, written)) == 1 && written.Type == SaveInfo.SaveType.Manual && written.FileName == OutputLeaf &&
                written.Name == Chunk7AcquiredManualSelection.OutputName && written.GameId == request.PersistenceLoad.GameId && written.GameName == request.PersistenceLoad.GameName &&
                written.Area?.AssetGuidThreadSafe == request.PersistenceLoad.Area && written.IsActuallySaved && written.HasFileOnDisk &&
                written.OperationState == SaveInfo.StateType.None && !written.IsAutoLevelupSave, "Settled native Manual descriptor inventory differs from its exact two entries.");
            evidence["settledDescriptors"] = new JArray(saves.OrderBy(value => value.FileName, StringComparer.Ordinal).Select(value => new JObject {
                ["descriptor"] = probe.Identity(value), ["fileName"] = value.FileName, ["internalName"] = value.Name, ["type"] = value.Type.ToString(),
                ["gameId"] = value.GameId, ["gameName"] = value.GameName, ["area"] = value.Area?.AssetGuidThreadSafe,
                ["isActuallySaved"] = value.IsActuallySaved, ["hasFileOnDisk"] = value.HasFileOnDisk, ["operationState"] = value.OperationState.ToString(), ["autoLevelup"] = value.IsAutoLevelupSave }));
            VerifySourceBytes(); var after = CaptureNoArrangement();
            Require(JToken.DeepEquals(noArrangementBefore, after), "Save-only derivative changed native XP, money, class levels or actor identity.");
            evidence["afterSaveArrangement"] = after;
            var file = new FileInfo(completion.Path); Require(file.Exists && file.Length > 0 && (file.Attributes & FileAttributes.ReparsePoint) == 0, "Native Manual archive did not materialize safely.");
            evidence["nativeWrite"] = new JObject { ["path"] = completion.Path, ["fileName"] = written.FileName, ["internalName"] = written.Name,
                ["type"] = written.Type.ToString(), ["sha256"] = Hash(file.FullName), ["length"] = file.Length, ["lastWriteTimeUtcTicks"] = file.LastWriteTimeUtc.Ticks,
                ["gameId"] = written.GameId, ["gameName"] = written.GameName, ["area"] = written.Area.AssetGuidThreadSafe,
                ["requestedDescriptor"] = probe.Identity(requestedManual), ["writtenDescriptor"] = probe.Identity(written), ["completionSequence"] = completion.Sequence,
                ["callbackCount"] = callbackCount, ["operationState"] = written.OperationState.ToString(), ["hasFileOnDisk"] = written.HasFileOnDisk,
                ["isActuallySaved"] = written.IsActuallySaved, ["autoLevelup"] = written.IsAutoLevelupSave, ["nativeWorkerSettled"] = true,
                ["snapshotCount"] = persistence.SnapshotCount, ["slotProjectionEnabled"] = NativePersistenceIsolation.NativeSlotRotationEnabled };
            Require(JToken.DeepEquals(resourceBefore, CaptureResources()), "Native derivative changed native action/reaction resources.");
            Require(restoration.Complete && ((JArray)restoration.Capture()["events"]).Count == 0, "Derivative observed preparation or mounted restoration.");
            evidence["resourcesAfterSave"] = CaptureResources(); evidence["savedOwner"] = CaptureAcquiredOwner();
            CaptureTwoOwners("twoOwners");
            evidence["savedWorld"] = Snapshot(); saveProven = true; Pass("C7-acquired-manual-native-save"); Pass("C7-acquired-manual-no-arrangement"); BeginCleanup();
        }
    }
}
