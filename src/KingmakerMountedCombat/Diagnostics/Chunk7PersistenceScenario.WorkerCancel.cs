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
        private SaveInfo boundarySeedDescriptor, boundaryReplacementDescriptor;
        private NativeSaveCompletion boundarySeedCompletion;
        private int boundarySeedCallbacks, boundaryReplacementCallbacks;
        private string boundarySeedHash, boundaryReplacementLeaf;
        private long boundarySeedLength;
        private JObject BoundaryDescriptor(SaveInfo value)
        {
            var row = OwnedNativeDescriptor(value);
            row["object"] = saveBoundaryIds.Get(value);
            return row;
        }
        private RuntimeSaveTarget BoundaryTarget(SaveInfo value) => new RuntimeSaveTarget {
            FullPath = value.FolderName, FileName = value.FileName, InternalName = value.Name,
            SaveType = value.Type.ToString(), GameId = value.GameId, GameName = value.GameName, Area = value.Area?.AssetGuidThreadSafe };
        private void RequireBoundarySourceImmutable()
        {
            var source = request.SelectedChunk7Fixture;
            Require(replaySaveAuthority.ValidateArchiveIdentity(new RuntimeSaveTarget {
                FullPath = Path.Combine(replaySaveAuthority.Root, source.FileName), FileName = source.FileName,
                InternalName = source.InternalName, SaveType = request.OwnedPairs == null ? "Manual" : request.ExpectedNativeLoadType,
                GameId = source.GameId, GameName = source.GameName, Area = source.Area }, Game.Instance.SaveManager.SavePath) == null,
                "The immutable input changed during the seed/replacement boundary.");
        }
        private void PollWorkerSeed()
        {
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
                NativePersistenceIsolation.HasPendingWrites || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence ||
                persistence.SaveDraining || persistence.WorldHoldReleasePending || world.SerializationBusy || !callback) return;
            RequireSaveBoundaryAuthority(); RequireHealthySaveOwner(); AssertOwners(); AssertSlots();
            inherited?.RequirePreserved(); RequireBoundarySourceImmutable();
            Require(!world.UnifiedTurn.HasUnsettledPreparation && !controls.HasUnsettledRelationshipTransition &&
                actors.All(a => a.Commands.Empty) && world.Pairs.All(p => !controls.HasUnsettledShellFor(p.Relationship)),
                "The completed native seed still owns actor/preparation custody.");
            var completion = persistence.LastCompletedSave;
            var manager = Game.Instance.SaveManager;
            Require(boundarySeedDescriptor == null && boundarySeedCompletion == null && boundarySeedCallbacks == 1 &&
                boundaryReplacementCallbacks == 0 && persistence.CompletedSaveCount == 1 && completion?.Sequence == 1 &&
                ReferenceEquals(completion.Requested, boundaryRequestedSave) && completion.Written != null &&
                !ReferenceEquals(completion.Written, completion.Requested) &&
                completion.Path == Path.Combine(replaySaveAuthority.Root, SelectedOutputLeaf(SaveInfo.SaveType.Manual)) &&
                saveBoundaryProbe.HeadersComplete(1) && persistence.SnapshotCount == snapshotBefore + 1 &&
                replayAuthorization.AuthorizedWriteCount == 1 && replaySaveAuthority.AdmittedNativeWriteRequests == 1 &&
                persistence.FailedSaveCount == 0 && persistence.DrainedSaveCount == 0 && persistence.DeferredSaveCancellationCount == 0 &&
                persistence.DeferredSaveCount == 0 && !NativeSaveWorkerBoundary.WorkerHeld &&
                NativeSaveWorkerBoundary.WorkerEntryCount == holdEntriesBefore + 1 && NativeSaveWorkerBoundary.WorkerHoldCount == holdCountBefore,
                "Replacement cannot start without the exact ordinary seed callback, completed worker and native header.");
            var seed = completion.Written;
            var descriptors = manager.Take(4).ToArray();
            Require(descriptors.Length == 2 && descriptors.Count(v => ReferenceEquals(v, seed)) == 1 &&
                descriptors.Count(v => v.FileName == request.SelectedChunk7Fixture.FileName) == 1 &&
                seed.FileName == SelectedOutputLeaf(SaveInfo.SaveType.Manual) && seed.Name == ManualName && seed.Type == SaveInfo.SaveType.Manual &&
                seed.GameId == request.SelectedChunk7Fixture.GameId && seed.GameName == request.SelectedChunk7Fixture.GameName &&
                seed.Area?.AssetGuidThreadSafe == ExpectedArchiveArea && seed.IsActuallySaved && seed.HasFileOnDisk &&
                seed.OperationState == SaveInfo.StateType.None && manager.IsSaveAllowed(), "The seed is not the exact completed native Manual descriptor.");
            replaySaveAuthority.AssertReadableArchive(seed.FolderName);
            var info = new FileInfo(seed.FolderName); var seedHash = Hash(info.FullName);
            Require(info.Length > 0, "The completed native seed is empty.");
            var number = manager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual);
            var leaf = "Manual_" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_" + ManualName + ".zks";
            if (request.OwnedPairs != null)
                replaySaveAuthority.ReserveOwnedPairsReplacement(request, Game.Instance, manager, seed, BoundaryTarget(seed), number);
            Require((request.OwnedPairs == null ? number == 301 && leaf == "Manual_301_KMC_C7_WORLD.zks" :
                leaf == replaySaveAuthority.OwnedPairsReplacementLeaf) && !File.Exists(Path.Combine(replaySaveAuthority.Root, leaf)),
                "Native replacement staging is not its distinct declared fresh leaf.");
            boundarySeedDescriptor = seed; boundarySeedCompletion = completion; boundarySeedHash = seedHash;
            boundarySeedLength = info.Length; boundaryReplacementLeaf = leaf;
            saveBoundaryEvidence["workerSeed"] = new JObject { ["requested"] = BoundaryDescriptor(completion.Requested),
                ["prepared"] = BoundaryDescriptor(seed), ["path"] = info.FullName, ["sha256"] = seedHash, ["length"] = info.Length,
                ["callbackCount"] = boundarySeedCallbacks, ["completionSequence"] = completion.Sequence,
                ["snapshotCount"] = persistence.SnapshotCount, ["nativeWorkerSettled"] = true };
            saveBoundaryRows.Add(CaptureSaveBoundary("worker-seed-complete"));
            // Preserve the completed seed as this next operation's requested object.
            // The native worker prepares another leaf and owns its replacement.
            boundaryRequestedSave = seed; callback = false;
            saveBoundaryEvidence["workerReplacement"] = new JObject { ["requested"] = BoundaryDescriptor(seed),
                ["stagingLeaf"] = leaf, ["landedLeaf"] = seed.FileName, ["seedSha256"] = seedHash, ["seedLength"] = info.Length,
                ["number"] = number, ["callbackCount"] = 0 };
            saveBoundaryRows.Add(CaptureSaveBoundary("worker-replacement-request"));
            saveWorkerHold = NativeSaveWorkerBoundary.ArmWorkerHold(20000);
            Game.Instance.SaveGame(seed, () => { boundaryReplacementCallbacks++; if (request.OwnedPairs != null) ownedSaveCallbacks++; callback = true; });
            Next(83);
        }
        private void ObserveWorkerReplacementPrepared()
        {
            Require(boundarySeedDescriptor != null && boundarySeedCompletion != null && boundarySeedCallbacks == 1 && boundaryReplacementCallbacks == 0 &&
                ReferenceEquals(boundaryRequestedSave, boundarySeedDescriptor) && ReferenceEquals(persistence.LastCompletedSave, boundarySeedCompletion) &&
                persistence.CompletedSaveCount == 1 && persistence.SnapshotCount == snapshotBefore + 2 && saveBoundaryProbe.Complete &&
                replaySaveAuthority.AdmittedNativeWriteRequests == 2 && replayAuthorization.AuthorizedWriteCount == 2,
                "Held replacement lost its distinct completed seed or second native operation.");
            RequireBoundarySourceImmutable();
            Require(Hash(boundarySeedDescriptor.FolderName) == boundarySeedHash && new FileInfo(boundarySeedDescriptor.FolderName).Length == boundarySeedLength,
                "The held worker changed its seed before native replacement.");
            var descriptors = Game.Instance.SaveManager.Take(4).ToArray();
            var prepared = descriptors.SingleOrDefault(v => v.FileName == boundaryReplacementLeaf);
            Require(descriptors.Length == 2 && prepared != null && !ReferenceEquals(prepared, boundarySeedDescriptor) &&
                descriptors.All(v => !ReferenceEquals(v, boundarySeedDescriptor)) &&
                descriptors.Count(v => v.FileName == request.SelectedChunk7Fixture.FileName) == 1 &&
                prepared.Name == ManualName && prepared.Type == SaveInfo.SaveType.Manual &&
                prepared.GameId == request.SelectedChunk7Fixture.GameId && prepared.GameName == request.SelectedChunk7Fixture.GameName &&
                prepared.Area?.AssetGuidThreadSafe == ExpectedArchiveArea,
                "Held native replacement did not retain its own distinct prepared descriptor and source inventory.");
            boundaryReplacementDescriptor = prepared;
            ((JObject)saveBoundaryEvidence["workerReplacement"])["prepared"] = BoundaryDescriptor(prepared);
        }
        private void ObserveWorkerReplacementSettled()
        {
            RequireBoundarySourceImmutable(); inherited?.RequirePreserved();
            Require(boundaryReplacementDescriptor != null && ReferenceEquals(persistence.LastCompletedSave, boundarySeedCompletion) &&
                boundarySeedCallbacks == 1 && boundaryReplacementCallbacks == 0 && persistence.CompletedSaveCount == 1 &&
                replayAuthorization.AuthorizedWriteCount == 2 && replaySaveAuthority.AdmittedNativeWriteRequests == 2 &&
                NativeSaveWorkerBoundary.WorkerEntryCount == holdEntriesBefore + 2 && NativeSaveWorkerBoundary.WorkerHoldCount == holdCountBefore + 1,
                "Drained replacement forged ordinary completion or changed operation counts.");
            var prepared = boundaryReplacementDescriptor;
            var descriptors = Game.Instance.SaveManager.Take(4).ToArray();
            Require(descriptors.Length == 2 && descriptors.Count(v => ReferenceEquals(v, prepared)) == 1 &&
                descriptors.Count(v => v.FileName == request.SelectedChunk7Fixture.FileName) == 1 &&
                prepared.FileName == SelectedOutputLeaf(SaveInfo.SaveType.Manual) && prepared.IsActuallySaved && prepared.HasFileOnDisk &&
                prepared.OperationState == SaveInfo.StateType.None && !File.Exists(Path.Combine(replaySaveAuthority.Root, boundaryReplacementLeaf)),
                "Native replacement did not land its own prepared descriptor at the completed seed path.");
            replaySaveAuthority.AssertReadableArchive(prepared.FolderName);
            var row = (JObject)saveBoundaryEvidence["workerReplacement"];
            row["settled"] = BoundaryDescriptor(prepared); row["callbackCount"] = boundaryReplacementCallbacks;
            row["completionCount"] = persistence.CompletedSaveCount; row["snapshotCount"] = persistence.SnapshotCount;
            row["drainCount"] = persistence.DrainedSaveCount; row["cancelCount"] = persistence.DeferredSaveCancellationCount;
            row["committed"] = persistence.LastDrainedSaveCommitted; row["outcomeEstablished"] = persistence.LastDrainedSaveOutcomeEstablished;
            row["path"] = persistence.LastDrainedSavePath;
        }
    }
}
