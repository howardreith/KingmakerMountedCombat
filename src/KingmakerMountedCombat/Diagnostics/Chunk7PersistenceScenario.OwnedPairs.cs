using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        private SaveInfo ownedRequestedSave;
        private int ownedSaveCallbacks;
        private string SelectedOutputLeaf(SaveInfo.SaveType type) => request.OwnedPairs == null ? NativeLeaf(type) : replaySaveAuthority.OwnedPairsOutputLeaf;
        private JObject OwnedAuthoritySnapshot() => new JObject {
            ["root"] = replaySaveAuthority.Root, ["loadCap"] = replaySaveAuthority.MaximumNativeLoadRequests,
            ["writeCap"] = replaySaveAuthority.MaximumNativeWriteRequests,
            ["nativeLoads"] = replaySaveAuthority.AdmittedNativeLoadRequests, ["nativeWrites"] = replaySaveAuthority.AdmittedNativeWriteRequests,
            ["authorizedLoads"] = replayAuthorization.AuthorizedLoadCount, ["authorizedWrites"] = replayAuthorization.AuthorizedWriteCount,
            ["reservationCount"] = replaySaveAuthority.OwnedPairsReservationCount,
            ["number"] = replaySaveAuthority.OwnedPairsOutputNumber, ["leaf"] = replaySaveAuthority.OwnedPairsOutputLeaf,
            ["nativeName"] = replaySaveAuthority.OwnedPairsOutputName, ["nativeType"] = replaySaveAuthority.OwnedPairsOutputType };
        private static JObject OwnedNativeDescriptor(SaveInfo value) => new JObject {
            ["fileName"] = value.FileName, ["nativeName"] = value.Name, ["nativeType"] = value.Type.ToString(),
            ["gameId"] = value.GameId, ["gameName"] = value.GameName, ["area"] = value.Area?.AssetGuidThreadSafe,
            ["hasFileOnDisk"] = value.HasFileOnDisk, ["isActuallySaved"] = value.IsActuallySaved,
            ["operation"] = value.OperationState.ToString(), ["autoLevelup"] = value.IsAutoLevelupSave };
        private void RequireOwnedSaveAdmission(SaveInfo.SaveType type)
        {
            Require(inherited != null && inherited.Bound && !LoadingProcess.Instance.IsLoadingInProcess &&
                !LoadingProcess.Instance.QueuedNames.Any() && !NativePersistenceIsolation.HasPendingWrites &&
                !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && ownedRequestedSave == null,
                "Inherited source still owns native loading/writing or already requested its single output.");
            inherited.RequirePreserved();
            var manager = Game.Instance.SaveManager;
            Require(replaySaveAuthority.MatchesOwnedPairsPlan(true, true) && replaySaveAuthority.OwnedPairsOutputType == type.ToString() &&
                replaySaveAuthority.OwnedPairsReservationCount == 1 && replaySaveAuthority.AdmittedNativeLoadRequests == 1 &&
                replayAuthorization.AuthorizedLoadCount == 1 && replaySaveAuthority.AdmittedNativeWriteRequests == 0 &&
                replayAuthorization.AuthorizedWriteCount == 0 && manager.IsSaveAllowed(),
                "Inherited output lacks its exact unused reserved native write after the completed sole load.");
            var descriptors = manager.Take(3).ToArray();
            Require(descriptors.Length == 1 && ReferenceEquals(descriptors[0], replaySaveAuthority.OwnedPairsSourceDescriptor) &&
                ReferenceEquals(descriptors[0], ownedFixtureLoader.Descriptor) && descriptors[0].FileName == request.PersistenceLoad.FileName &&
                descriptors[0].Name == request.PersistenceLoad.InternalName && descriptors[0].Type.ToString() == request.ExpectedNativeLoadType &&
                descriptors[0].OperationState == SaveInfo.StateType.None && descriptors[0].HasFileOnDisk && descriptors[0].IsActuallySaved,
                "Inherited native source descriptor inventory changed before its selected output.");
            var number = manager.FindUnusedSaveNumber(type);
            var leaf = type + "_" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                (type == SaveInfo.SaveType.Manual ? "_" + replaySaveAuthority.OwnedPairsOutputName : "") + ".zks";
            Require(number == replaySaveAuthority.OwnedPairsOutputNumber && leaf == replaySaveAuthority.OwnedPairsOutputLeaf &&
                !File.Exists(Path.Combine(replaySaveAuthority.Root, leaf)), "Inherited native output numbering/absence changed after reservation.");
            var reason = replaySaveAuthority.ValidateArchiveIdentity(new RuntimeSaveTarget { FullPath = Path.Combine(replaySaveAuthority.Root, request.PersistenceLoad.FileName),
                FileName = request.PersistenceLoad.FileName, InternalName = request.PersistenceLoad.InternalName, SaveType = request.ExpectedNativeLoadType,
                GameId = request.PersistenceLoad.GameId, GameName = request.PersistenceLoad.GameName, Area = request.PersistenceLoad.Area }, manager.SavePath);
            Require(reason == null, "Inherited source archive changed before its sole native output: " + reason);
            evidence["ownedOutputAdmission"] = new JObject { ["method"] = "FindUnusedSaveNumber", ["token"] = "06008024",
                ["moduleMvid"] = typeof(SaveManager).Module.ModuleVersionId.ToString("D"), ["number"] = number, ["leaf"] = leaf,
                ["nativeInventory"] = new JArray(descriptors.Select(OwnedNativeDescriptor)), ["authority"] = OwnedAuthoritySnapshot() };
        }
        private void ObserveOwnedSaveCompletion()
        {
            if (request.OwnedPairs == null) return;
            inherited.RequirePreserved();
            var completion = persistence.LastCompletedSave;
            var cancelledReplacement = SourceSaveBoundary && WorkerCancelSave;
            Require(ownedRequestedSave != null && ownedSaveCallbacks == 1 && (cancelledReplacement ? !callback : callback) && completion != null &&
                completion.Sequence == snapshotBefore + 1 && ReferenceEquals(completion.Requested, ownedRequestedSave) &&
                (cancelledReplacement ? ReferenceEquals(completion.Written, boundarySeedDescriptor) && ReferenceEquals(savedDescriptor, boundaryReplacementDescriptor) &&
                    ReferenceEquals(completion, boundarySeedCompletion) && boundarySeedCallbacks == 1 && boundaryReplacementCallbacks == 0 && boundaryDrainedCommitted &&
                    persistence.CompletedSaveCount == 1 && persistence.DrainedSaveCount == 1 && persistence.DeferredSaveCancellationCount == 1 : ReferenceEquals(completion.Written, savedDescriptor)) && completion.Path == Path.Combine(replaySaveAuthority.Root, replaySaveAuthority.OwnedPairsOutputLeaf) &&
                replaySaveAuthority.MatchesOwnedPairsPlan(true, false) && replaySaveAuthority.AdmittedNativeLoadRequests == 1 &&
                replaySaveAuthority.AdmittedNativeWriteRequests == (cancelledReplacement ? 2 : 1) && replayAuthorization.AuthorizedLoadCount == 1 &&
                replayAuthorization.AuthorizedWriteCount == (cancelledReplacement ? 2 : 1) && !NativePersistenceIsolation.HasPendingWrites &&
                !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence,
                "Inherited native output lost its exact requested/prepared descriptor, completed worker or single callback.");
            var descriptors = Game.Instance.SaveManager.Take(3).ToArray();
            Require(descriptors.Length == 2 && descriptors.Count(value => ReferenceEquals(value, replaySaveAuthority.OwnedPairsSourceDescriptor)) == 1 &&
                descriptors.Count(value => ReferenceEquals(value, savedDescriptor)) == 1 && savedDescriptor.FileName == replaySaveAuthority.OwnedPairsOutputLeaf &&
                savedDescriptor.Name == replaySaveAuthority.OwnedPairsOutputName && savedDescriptor.Type.ToString() == replaySaveAuthority.OwnedPairsOutputType &&
                savedDescriptor.GameId == request.PersistenceLoad.GameId && savedDescriptor.GameName == request.PersistenceLoad.GameName &&
                savedDescriptor.Area?.AssetGuidThreadSafe == request.PersistenceLoad.Area && savedDescriptor.HasFileOnDisk && savedDescriptor.IsActuallySaved &&
                savedDescriptor.OperationState == SaveInfo.StateType.None, "Inherited native output did not settle the exact two-entry isolated inventory.");
            evidence["ownedOutputCompletion"] = new JObject { ["completionSequence"] = completion.Sequence, ["callbackCount"] = ownedSaveCallbacks,
                ["nativeWorkerSettled"] = true, ["nativeInventory"] = new JArray(descriptors.OrderBy(value => value.FileName, StringComparer.Ordinal).Select(OwnedNativeDescriptor)),
                ["authority"] = OwnedAuthoritySnapshot() };
            if (cancelledReplacement)
                ((JObject)evidence["ownedOutputCompletion"])["replacement"] = new JObject { ["callbackCount"] = boundaryReplacementCallbacks,
                    ["completedSaveCount"] = persistence.CompletedSaveCount, ["drainCount"] = persistence.DrainedSaveCount,
                    ["committed"] = persistence.LastDrainedSaveCommitted, ["outcomeEstablished"] = persistence.LastDrainedSaveOutcomeEstablished,
                    ["path"] = persistence.LastDrainedSavePath, ["stagingLeaf"] = boundaryReplacementLeaf };
        }
    }
}
