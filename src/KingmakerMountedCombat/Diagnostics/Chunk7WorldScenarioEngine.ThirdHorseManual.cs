using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        private bool ThirdHorseManualWritten => ThirdHorseManual && manualSaved != null;
        private JObject thirdManualBoundary;

        private void UpdateThirdHorseManual()
        {
            Require(ThirdHorseManual && thirdSetupCompleted && step >= 82 && step <= 83 &&
                clock.Elapsed.TotalSeconds <= 720 && clock.Elapsed.TotalSeconds - stepStarted <= 120,
                "The third-Horse Manual exceeded its bounded post-setup save window.");
            if (step == 82)
            {
                RequireThirdHorseSetupPreserved();
                if (!Idle(thirdRider) || !Idle(thirdMount) || !NativeCommandsSettled() ||
                    controls.HasUnsettledRelationshipTransition || persistence.HasActiveSaveScope ||
                    persistence.HasSaveAdmissionFence) return;
                RequestThirdHorseManual(); Next(83); return;
            }
            if (persistence.ActiveSaveWorkerRunning) manualWorkerObserved = true;
            if (manualCallbacks == 0 || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence ||
                NativePersistenceIsolation.HasPendingWrites) return;
            ObserveThirdHorseManual();
            Pass(FourthHorseSetup ? "C7-f09-fourth-horse-native-manual" :
                "C7-small-third-horse-native-manual"); BeginCleanup();
        }

        private void RequestThirdHorseManual()
        {
            var manager = Game.Instance.SaveManager;
            var input = request.PersistenceLoad;
            var name = FourthHorseSetup ? Chunk7OwnedPairsSelection.FourthHorseManualName :
                Chunk7OwnedPairsSelection.SmallFixtureManualName;
            Require(!LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any() &&
                !NativePersistenceIsolation.HasPendingWrites && !world.SerializationBusy &&
                manualRequested == null && persistence.CompletedSaveCount == 0 &&
                Game.Instance.UI?.CharacterBuildController?.LevelUpController == null &&
                !Game.Instance.Player.IsInCombat && manager.SavePath == fixtureAuthority.Root && manager.IsSaveAllowed(),
                "Third-Horse Manual found an active native load, builder, writer, combat or save refusal.");
            Require(fixtureAuthorization.IsActive && fixtureAuthorization.IsExactPersistenceScope(fixtureAuthority) &&
                NativePersistenceIsolation.IsExactAuthority(fixtureAuthority) && fixtureAuthority.MatchesOwnedPairsPlan(true, true) &&
                fixtureAuthority.OwnedPairsReservationCount == 1 && fixtureAuthority.OwnedPairsOutputType == "Manual" &&
                fixtureAuthority.OwnedPairsOutputName == name && fixtureAuthority.AdmittedNativeLoadRequests == 1 &&
                fixtureAuthority.AdmittedNativeWriteRequests == 0 && fixtureAuthorization.AuthorizedLoadCount == 1 &&
                fixtureAuthorization.AuthorizedWriteCount == 0 && fixtureAuthorization.UnauthorizedWriteCount == 0,
                "Third-Horse Manual lacks its one unused native reservation and exact run-root authority.");
            var descriptors = manager.Take(3).ToArray();
            var source = descriptors.Length == 1 ? descriptors[0] : null;
            Require(source != null && ReferenceEquals(source, fixtureAuthority.OwnedPairsSourceDescriptor) &&
                source.FileName == input.FileName && source.Name == input.InternalName &&
                source.Type.ToString() == request.ExpectedNativeLoadType && source.OperationState == SaveInfo.StateType.None &&
                source.HasFileOnDisk && source.IsActuallySaved &&
                fixtureLoader.Descriptor?.FileName == source.FileName && fixtureLoader.Descriptor?.FolderName == source.FolderName,
                "Third-Horse Manual no longer has its sole immutable native source descriptor.");
            var number = manager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual);
            var leaf = "Manual_" + number.ToString(CultureInfo.InvariantCulture) + "_" + name + ".zks";
            Require(number == fixtureAuthority.OwnedPairsOutputNumber && leaf == fixtureAuthority.OwnedPairsOutputLeaf &&
                !File.Exists(Path.Combine(fixtureAuthority.Root, leaf)) &&
                fixtureAuthority.ValidateArchiveIdentity(new RuntimeSaveTarget {
                    FullPath = Path.Combine(fixtureAuthority.Root, input.FileName), FileName = input.FileName,
                    InternalName = input.InternalName, SaveType = request.ExpectedNativeLoadType,
                    GameId = input.GameId, GameName = input.GameName, Area = input.Area }, manager.SavePath) == null,
                "Third-Horse Manual number, reserved leaf or source bytes changed.");
            RequireThirdHorseSetupPreserved();
            thirdManualBoundary = Chunk7OwnedPairFixture.ArrangementOf(Chunk7NativeFieldRoster.Capture(Game.Instance.Player));
            evidence["ownedOutputAdmission"] = new JObject { ["method"] = "FindUnusedSaveNumber",
                ["token"] = "06008024", ["moduleMvid"] = typeof(SaveManager).Module.ModuleVersionId.ToString("D"),
                ["number"] = number,
                ["leaf"] = leaf, ["nativeInventory"] = new JArray(descriptors.Select(ManualDescriptor)),
                ["authority"] = ManualAuthority() };
            var descriptor = manager.CreateNewSave(name);
            Require(descriptor != null && descriptor.Type == SaveInfo.SaveType.Manual && descriptor.Name == name &&
                !descriptor.IsActuallySaved && !descriptor.IsAutoLevelupSave && manager.IsSaveAllowed(),
                "Third-Horse native Manual descriptor differs from its selected category.");
            manualRequested = descriptor; manualSnapshotBefore = persistence.SnapshotCount;
            manualCastBefore = controls.NativeCastRequestCount;
            persistence.SaveSnapshotStarting += ObserveManualHeaderStarting;
            persistence.SaveSnapshotStaged += ObserveManualHeaderStaged;
            manualObserverAttached = true;
            evidence["saveRequested"] = new JObject { ["nativeType"] = "Manual", ["nativeName"] = name,
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["snapshotCount"] = manualSnapshotBefore, ["nativeSaveAllowed"] = true,
                ["newSlot"] = true, ["levelUpSave"] = false,
                ["api"] = "Game.SaveGame(SaveManager.CreateNewSave(name))" };
            Game.Instance.SaveGame(descriptor, () => manualCallbacks++);
        }

        private void ObserveThirdHorseManual()
        {
            Require(manualObserverError == null && manualHeaderStarts == 1 && manualHeaderStages == 1 &&
                manualHeaderStarting != null && manualHeaderStaged != null,
                "Third-Horse native requested/prepared header or KMC staging differs: " + manualObserverError);
            DetachManualObservers();
            var completion = persistence.LastCompletedSave;
            Require(manualCallbacks == 1 && completion != null && persistence.CompletedSaveCount == 1 &&
                completion.Sequence == manualSnapshotBefore + 1 && ReferenceEquals(completion.Requested, manualRequested) &&
                ReferenceEquals(completion.Written, manualHeaderPrepared) &&
                completion.Path == Path.Combine(fixtureAuthority.Root, fixtureAuthority.OwnedPairsOutputLeaf) &&
                fixtureAuthority.MatchesOwnedPairsPlan(true, false) && fixtureAuthority.AdmittedNativeLoadRequests == 1 &&
                fixtureAuthority.AdmittedNativeWriteRequests == 1 && fixtureAuthorization.AuthorizedLoadCount == 1 &&
                fixtureAuthorization.AuthorizedWriteCount == 1 && fixtureAuthorization.UnauthorizedWriteCount == 0 &&
                !NativePersistenceIsolation.HasPendingWrites && !persistence.HasActiveSaveScope &&
                !persistence.HasSaveAdmissionFence && controls.NativeCastRequestCount == manualCastBefore,
                "Third-Horse Manual lost its exact native completion, callback or single-writer custody.");
            manualSaved = completion.Written;
            var descriptors = Game.Instance.SaveManager.Take(3).ToArray();
            Require(descriptors.Length == 2 && descriptors.Count(value => ReferenceEquals(value, fixtureAuthority.OwnedPairsSourceDescriptor)) == 1 &&
                descriptors.Count(value => ReferenceEquals(value, manualSaved)) == 1 &&
                manualSaved.FileName == fixtureAuthority.OwnedPairsOutputLeaf &&
                manualSaved.Name == (FourthHorseSetup ? Chunk7OwnedPairsSelection.FourthHorseManualName :
                    Chunk7OwnedPairsSelection.SmallFixtureManualName) &&
                manualSaved.Type == SaveInfo.SaveType.Manual && manualSaved.GameId == request.PersistenceLoad.GameId &&
                manualSaved.GameName == request.PersistenceLoad.GameName &&
                manualSaved.Area?.AssetGuidThreadSafe == request.PersistenceLoad.Area &&
                manualSaved.HasFileOnDisk && manualSaved.IsActuallySaved &&
                manualSaved.OperationState == SaveInfo.StateType.None && !manualSaved.IsAutoLevelupSave,
                "Third-Horse Manual escaped its two-entry isolated native inventory.");
            var read = NativeMountedSaveStorage.ReadArchive(manualSaved.Saver);
            var saved = read.Data;
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 3 &&
                saved != null && saved.SchemaVersion == 3 && saved.CampaignId == request.PersistenceLoad.GameId &&
                saved.AreaId == request.PersistenceLoad.Area && saved.Pairs.Length == 0 &&
                saved.Participations.Length == 0 && saved.Slots.Length == 0 && saved.Combat == null &&
                world.Registry.Owners.Length == 0 && persistence.SnapshotCount == manualSnapshotBefore + 1,
                "Third-Horse native Manual did not read back as the same unmounted schema-3 world.");
            RequireThirdHorseSetupPreserved();
            var recordedHorse = (JObject)evidence["thirdHorseSetup"]["nativeHorse"];
            var feature = thirdRider.Descriptor.GetFact(horses.HorseFeature) as Feature;
            var rank = thirdRider.Descriptor.GetFact(horses.LevelRank);
            Require(recordedHorse != null && feature?.Source?.AssetGuid == (string)recordedHorse["sourceGuid"] &&
                thirdMount.UniqueId == (string)recordedHorse["mountId"] &&
                thirdMount.Blueprint.AssetGuid == (string)recordedHorse["blueprint"] &&
                ReferenceEquals(thirdRider.Descriptor.Pet, thirdMount) &&
                ReferenceEquals(thirdMount.Descriptor.Master.Value, thirdRider) &&
                rank?.GetRank() == (int)recordedHorse["rank"],
                "Third-Horse native sourced fact, rank or reciprocal pet changed across the Manual.");
            var arrangementAfter = Chunk7OwnedPairFixture.ArrangementOf(Chunk7NativeFieldRoster.Capture(Game.Instance.Player));
            Require(JToken.DeepEquals(thirdManualBoundary, arrangementAfter),
                "Third-Horse native progression, roster or money changed across the Manual.");
            var file = new FileInfo(manualSaved.FolderName);
            Require(file.Exists && file.FullName == Path.Combine(fixtureAuthority.Root, fixtureAuthority.OwnedPairsOutputLeaf) &&
                file.Length > 0, "Third-Horse native Manual escaped its reserved run-root leaf.");
            evidence["ownedOutputCompletion"] = new JObject { ["completionSequence"] = completion.Sequence,
                ["callbackCount"] = manualCallbacks, ["nativeWorkerSettled"] = true,
                ["nativeInventory"] = new JArray(descriptors.OrderBy(value => value.FileName, StringComparer.Ordinal).Select(ManualDescriptor)),
                ["authority"] = ManualAuthority() };
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["sha256"] = ManualHash(file.FullName),
                ["length"] = file.Length, ["lastWriteTimeUtcTicks"] = file.LastWriteTimeUtc.Ticks,
                ["fileName"] = manualSaved.FileName, ["nativeType"] = "Manual",
                ["nativeName"] = manualSaved.Name, ["gameId"] = manualSaved.GameId,
                ["gameName"] = manualSaved.GameName, ["area"] = manualSaved.Area.AssetGuidThreadSafe,
                ["nativeCallback"] = true, ["operation"] = manualSaved.OperationState.ToString(),
                ["snapshotCount"] = persistence.SnapshotCount, ["readKind"] = read.Kind.ToString(),
                ["sourceSchema"] = read.SourceSchema,
                ["archive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer()) };
            evidence["thirdHorseManual"] = new JObject { ["contract"] = FourthHorseSetup ?
                    "three-horse-source-fourth-native-horse-manual-v1" : "small-third-horse-manual-producer-v1",
                ["disclosedAssistance"] = true, ["acquisitionProof"] = false,
                ["setupCompletedBeforeSave"] = thirdSetupCompleted,
                ["boundary"] = thirdManualBoundary, ["arrangementAfterSave"] = arrangementAfter,
                ["headerStarting"] = manualHeaderStarting, ["headerStaged"] = manualHeaderStaged,
                ["workerObservedRunning"] = manualWorkerObserved,
                ["completionSequence"] = completion.Sequence,
                ["fieldActorIds"] = new JArray(Chunk7NativeFieldRoster.Capture(Game.Instance.Player).Select(actor => actor.UniqueId)) };
        }
    }
}
