using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.SettingsUI;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 1 (SAVE-01/03): the repaired replacement commit exercised through ordinary save
    // behaviour inside the owner-seed checkpoint producer. The first write is the generic
    // first-ever slot/Manual write (RequestSave). Every further write is an ordinary overwrite
    // of that same selected output: for the Quick case the body of the engine's QuickSave
    // hotkey handler (Game.MakeQuickSave, the exact method the "QuickSave" key binding
    // invokes; the key press itself is not synthesized), with the one projected Quick slot
    // already full so GetNextQuickslot rotates onto the existing descriptor; for the Manual
    // control Game.SaveGame on the existing Manual descriptor, as the save menu's overwrite
    // does. The engine then writes a fresh next-number staging archive and moves it onto the
    // existing leaf through the transpiled replacement site, which is the only path that
    // records a commit. Each overwrite is proven by that commit count and destination, the
    // changed bytes at the unchanged leaf, the absent staging leaf, the settled descriptor
    // inventory, the released holds and a full read-back of the mounted graph.
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string SlotRotationRow = "C7-persistence-native-slot-rotation";
        private bool SlotRotationCase => !Cold && OwnerSeedCheckpointSave && Chunk7OwnedPairsSelection.IsSlotRotationCheckpoint(request.PersistenceCase);
        private int SlotRotationWrites => Chunk7OwnedPairsSelection.SlotRotationWrites(request.PersistenceCase);
        private readonly JArray rotationWrites = new JArray();
        private JObject rotationPending;
        private SaveInfo rotationRequested;
        private string rotationPreviousHash;
        private int rotationCommitsBefore, rotationCompletionsBefore, rotationFailuresBefore, rotationReplacementFailuresBefore, rotationWriteIndex;
        private string RotationLeafPath => Path.Combine(replaySaveAuthority.Root, replaySaveAuthority.OwnedPairsOutputLeaf);
        private string RotationRoute(SaveInfo.SaveType type) => type == SaveInfo.SaveType.Quick ?
            "Game.MakeQuickSave(): the QuickSave key binding's handler body (UpdateSaveListIfNeeded + SaveGame(GetNextQuickslot())); key press not synthesized" :
            "Game.SaveGame(existing Manual descriptor): the save menu's overwrite of an existing Manual";

        // After the generic first write settled (ObserveSaved): record it as write 1 and start the overwrites.
        private void BeginSlotRotation()
        {
            var first = evidence["nativeWrite"] as JObject;
            Require(SlotRotationCase && rotationWrites.Count == 0 && savedDescriptor != null && first != null && callback &&
                ownedSaveCallbacks == 1 && persistence.CompletedSaveCount == 1 && replaySaveAuthority.AdmittedNativeWriteRequests == 1 &&
                replaySaveAuthority.MaximumNativeWriteRequests == SlotRotationWrites && SlotRotationWrites >= 2,
                "Slot rotation requires the settled generic first write of its selected output.");
            var type = NativeType(request.PersistenceCase);
            rotationWrites.Add(new JObject { ["index"] = 1, ["replacement"] = false, ["callback"] = true,
                ["route"] = type == SaveInfo.SaveType.Quick ? "slot route: GetNextQuickslot() + Game.SaveGame (first-ever Quick; fills the one projected slot)" :
                    "CreateNewSave + Game.SaveGame (new Manual control)",
                ["landedLeaf"] = savedDescriptor.FileName, ["path"] = first["path"], ["sha256"] = first["sha256"], ["length"] = first["length"],
                ["completionSequence"] = persistence.LastCompletedSave.Sequence, ["commitCount"] = NativeMountedArchiveCommit.CommitCount,
                ["commitRecorded"] = false, ["nativeWrites"] = replaySaveAuthority.AdmittedNativeWriteRequests });
            evidence["slotRotation"] = new JObject { ["contract"] = "sprint1-slot-rotation-v1", ["checkpoint"] = request.PersistenceCase,
                ["nativeType"] = type.ToString(), ["selectedWrites"] = SlotRotationWrites, ["projectedSlotCapacity"] = 1,
                ["overwriteRoute"] = RotationRoute(type), ["keyPressSynthesized"] = false, ["replacementSiteInvokedDirectly"] = false,
                ["commitCountBefore"] = NativeMountedArchiveCommit.CommitCount, ["writes"] = rotationWrites };
            BeginSlotRotationWrite();
        }

        private void BeginSlotRotationWrite()
        {
            RequireNativeWorld(); inherited.RequirePreserved(); AssertOwners(); AssertSlots();
            var type = NativeType(request.PersistenceCase); var manager = Game.Instance.SaveManager;
            Require(rotationPending == null && rotationWrites.Count >= 1 && rotationWrites.Count < SlotRotationWrites &&
                !LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any() && !NativePersistenceIsolation.HasPendingWrites &&
                !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && !persistence.SaveDraining && !persistence.WorldHoldReleasePending &&
                !world.SerializationBusy && !controls.SerializationSuspended && !persistence.SaveSuspended && actors.All(Chunk7NativeFixture.Idle) &&
                !Game.Instance.Player.IsInCombat && manager.IsSaveAllowed(),
                "Slot rotation cannot start another ordinary save while native loading, writing or a hold is pending.");
            var settings = SettingsRoot.Instance;
            var capacity = type == SaveInfo.SaveType.Quick ? settings.QuicksaveSlots.CurrentValue : type == SaveInfo.SaveType.Auto ? settings.AutosaveSlots.CurrentValue : -1f;
            Require(NativePersistenceIsolation.NativeSlotRotationEnabled && (type == SaveInfo.SaveType.Manual || capacity == 1f),
                "The disposable one-slot capacity is not in force for the selected category.");
            var existing = savedDescriptor;
            var descriptors = manager.Take(3).ToArray();
            var ofType = descriptors.Count(value => value.Type == type && value.IsActuallySaved);
            Require(descriptors.Length == 2 && descriptors.Count(value => ReferenceEquals(value, existing)) == 1 &&
                descriptors.Count(value => ReferenceEquals(value, replaySaveAuthority.OwnedPairsSourceDescriptor)) == 1 &&
                existing.FileName == replaySaveAuthority.OwnedPairsOutputLeaf && existing.Type == type && existing.IsActuallySaved && existing.HasFileOnDisk &&
                existing.OperationState == SaveInfo.StateType.None && SameArchivePath(existing.FolderName, RotationLeafPath),
                "The selected output is not the settled sole descriptor of its category before the overwrite.");
            var nextIsExisting = true;
            if (type == SaveInfo.SaveType.Quick)
            {
                // The handler's own first step, then its slot choice: with the one projected slot
                // full, the engine rotates onto the existing descriptor rather than minting a new one.
                manager.UpdateSaveListIfNeeded();
                var next = manager.GetNextQuickslot();
                nextIsExisting = ReferenceEquals(next, existing);
                Require(nextIsExisting && ofType == 1 && descriptors.All(value => manager.Contains(value)),
                    "The configured Quick capacity is not full: the handler would mint a new slot instead of overwriting the existing one, or the list was rebuilt.");
            }
            var number = manager.FindUnusedSaveNumber(type);
            replaySaveAuthority.ReserveOwnedPairsReplacement(request, Game.Instance, manager, existing, BoundaryTarget(existing), number);
            var staging = type + "_" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                (type == SaveInfo.SaveType.Manual ? "_" + replaySaveAuthority.OwnedPairsOutputName : "") + ".zks";
            Require(staging == replaySaveAuthority.OwnedPairsReplacementLeaf && staging != existing.FileName &&
                !File.Exists(Path.Combine(replaySaveAuthority.Root, staging)), "The overwrite's staging leaf is not the declared distinct next-number leaf.");
            replaySaveAuthority.AssertReadableArchive(existing.FolderName);
            var file = new FileInfo(existing.FolderName);
            rotationPreviousHash = Hash(file.FullName);
            Require(file.Length > 0 && rotationPreviousHash == (string)((JObject)rotationWrites[rotationWrites.Count - 1])["sha256"],
                "The bytes at the selected leaf are not the previous write's.");
            rotationCommitsBefore = NativeMountedArchiveCommit.CommitCount; rotationCompletionsBefore = persistence.CompletedSaveCount;
            rotationFailuresBefore = persistence.FailedSaveCount; rotationReplacementFailuresBefore = NativeMountedArchiveCommit.ReplacementFailureCount;
            rotationWriteIndex = rotationWrites.Count + 1; rotationRequested = existing; callback = false;
            rotationPending = new JObject { ["index"] = rotationWriteIndex, ["route"] = RotationRoute(type), ["replacement"] = true, ["callback"] = false,
                ["requestedIsExistingSlot"] = true, ["requestedIsListDescriptor"] = true, ["nextSlotIsExisting"] = nextIsExisting,
                ["nativeSlotCapacity"] = capacity, ["savedOfCategory"] = ofType, ["stagingLeaf"] = staging, ["landedLeaf"] = existing.FileName,
                ["previousSha256"] = rotationPreviousHash, ["previousLength"] = file.Length, ["commitCountBefore"] = rotationCommitsBefore,
                ["completionSequenceBefore"] = rotationCompletionsBefore, ["replacementFailuresBefore"] = rotationReplacementFailuresBefore,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["snapshotCount"] = persistence.SnapshotCount };
            rotationWrites.Add(rotationPending);
            if (type == SaveInfo.SaveType.Quick) Game.Instance.MakeQuickSave();
            else Game.Instance.SaveGame(existing);
        }

        // Stage 8: the overwrite's native worker settles, then its commit is verified.
        private bool PollSlotRotation()
        {
            Require(SlotRotationCase && rotationPending != null && rotationRequested != null, "Slot rotation poll outside an ordinary overwrite.");
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() || NativePersistenceIsolation.HasPendingWrites ||
                persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence || persistence.SaveDraining || persistence.WorldHoldReleasePending ||
                world.SerializationBusy || persistence.CompletedSaveCount < rotationCompletionsBefore + 1) return false;
            ObserveSlotRotationWrite();
            if (rotationWrites.Count < SlotRotationWrites) { BeginSlotRotationWrite(); return false; }
            FinishSlotRotation();
            return true;
        }

        private void ObserveSlotRotationWrite()
        {
            inherited.RequirePreserved();
            var type = NativeType(request.PersistenceCase); var manager = Game.Instance.SaveManager;
            var completion = persistence.LastCompletedSave; var leafPath = RotationLeafPath;
            Require(completion != null && completion.Sequence == rotationCompletionsBefore + 1 && persistence.CompletedSaveCount == rotationCompletionsBefore + 1 &&
                ReferenceEquals(completion.Requested, rotationRequested) && completion.Written != null && !ReferenceEquals(completion.Written, completion.Requested) &&
                SameArchivePath(completion.Path, leafPath) && persistence.FailedSaveCount == rotationFailuresBefore && !callback && ownedSaveCallbacks == 1,
                "The ordinary overwrite did not complete as one native operation requested on the existing descriptor.");
            var commits = NativeMountedArchiveCommit.CommitCount; var landed = NativeMountedArchiveCommit.LastCommittedDestination;
            Require(commits == rotationCommitsBefore + 1 && SameArchivePath(landed, leafPath) &&
                NativeMountedArchiveCommit.ReplacementFailureCount == rotationReplacementFailuresBefore,
                "The overwrite did not commit exactly one native replacement onto the selected slot (commits " + rotationCommitsBefore + " -> " + commits + ", landed " + landed + ").");
            var written = completion.Written;
            var descriptors = manager.Take(3).ToArray();
            var staging = Path.Combine(replaySaveAuthority.Root, replaySaveAuthority.OwnedPairsReplacementLeaf);
            Require(descriptors.Length == 2 && descriptors.Count(value => ReferenceEquals(value, written)) == 1 &&
                descriptors.Count(value => ReferenceEquals(value, replaySaveAuthority.OwnedPairsSourceDescriptor)) == 1 &&
                descriptors.All(value => !ReferenceEquals(value, rotationRequested)) && written.FileName == replaySaveAuthority.OwnedPairsOutputLeaf &&
                written.Name == rotationRequested.Name && written.Type == type && written.GameId == request.PersistenceLoad.GameId &&
                written.GameName == request.PersistenceLoad.GameName && written.Area?.AssetGuidThreadSafe == ExpectedArchiveArea &&
                written.IsActuallySaved && written.HasFileOnDisk && written.OperationState == SaveInfo.StateType.None && !written.IsAutoLevelupSave &&
                SameArchivePath(written.FolderName, leafPath) && !File.Exists(staging) && !Directory.Exists(staging),
                "The overwrite did not land its prepared descriptor at the existing leaf with the staging leaf gone.");
            var entries = Directory.EnumerateFileSystemEntries(replaySaveAuthority.Root).Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            Require(entries.Length == 2 && entries.Contains(request.PersistenceLoad.FileName) && entries.Contains(replaySaveAuthority.OwnedPairsOutputLeaf),
                "The isolated save root holds more than the immutable input and the overwritten output.");
            replaySaveAuthority.AssertReadableArchive(written.FolderName);
            var file = new FileInfo(written.FolderName); var hash = Hash(file.FullName);
            Require(file.Length > 0 && hash != rotationPreviousHash, "The overwrite left the previous bytes at the selected leaf.");
            Require(replaySaveAuthority.MatchesOwnedPairsPlan(true, false) && replaySaveAuthority.AdmittedNativeLoadRequests == 1 &&
                replaySaveAuthority.AdmittedNativeWriteRequests == rotationWriteIndex && replayAuthorization.AuthorizedWriteCount == rotationWriteIndex &&
                replayAuthorization.AuthorizedLoadCount == 1 && persistence.SnapshotCount == snapshotBefore + rotationWriteIndex &&
                !controls.SerializationSuspended && !persistence.SaveSuspended && !NativePersistenceIsolation.HasPendingWrites &&
                !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && !NativeSaveWorkerBoundary.WorkerHeld,
                "The overwrite did not release its serialization holds or spent other than exactly one further native write.");
            var read = NativeMountedSaveStorage.ReadArchive(written.Saver);
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 3 && read.Data?.Pairs.Length == 2 && read.Data.Participations.Length == 0 &&
                read.Data.Combat == null && read.Data.CampaignId == request.SelectedChunk7Fixture.GameId && read.Data.AreaId == ExpectedArchiveArea,
                "The overwritten archive did not retain the entire two-pair exploration world.");
            saved = read.Data; savedDescriptor = written;
            AssertOwners(); AssertSlots(); AssertDebt(saved);
            Require(saved.Pairs.All(p => expectedOwners.TryGetValue(Guid.Parse(p.PairId), out var generation) && generation == p.Generation &&
                world.Registry.FindActive(Exact(p.Rider.Id))?.Id == Guid.Parse(p.PairId)), "The overwritten archive changed stable custody.");
            rotationPending["completionSequence"] = completion.Sequence; rotationPending["commitCountAfter"] = commits; rotationPending["commitRecorded"] = true;
            rotationPending["committedDestination"] = landed; rotationPending["path"] = file.FullName; rotationPending["sha256"] = hash; rotationPending["length"] = file.Length;
            rotationPending["bytesChanged"] = true; rotationPending["stagingAbsent"] = true; rotationPending["writtenIsNewDescriptor"] = true;
            rotationPending["requestedDescriptorRetired"] = true; rotationPending["nativeWrites"] = replaySaveAuthority.AdmittedNativeWriteRequests;
            rotationPending["failedSaves"] = persistence.FailedSaveCount; rotationPending["holdsReleased"] = true;
            rotationPending["archiveGameTimeTicks"] = saved.GameTimeTicks;
            snapshots.Add(Capture("slot-rotation-write-" + rotationWriteIndex + "-complete"));
            rotationPending = null; rotationRequested = null;
        }

        // The selected output's final bytes are the last overwrite's: the generic receipt rows the
        // readers and the cold consumer compare against are re-recorded from that final archive.
        private void FinishSlotRotation()
        {
            var type = NativeType(request.PersistenceCase); var manager = Game.Instance.SaveManager;
            Require(rotationPending == null && rotationWrites.Count == SlotRotationWrites && savedDescriptor != null && saved != null &&
                replaySaveAuthority.AdmittedNativeWriteRequests == SlotRotationWrites && persistence.CompletedSaveCount == SlotRotationWrites,
                "Slot rotation did not complete exactly its selected number of writes.");
            var file = new FileInfo(savedDescriptor.FolderName);
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["sha256"] = Hash(file.FullName), ["length"] = file.Length,
                ["nativeType"] = type.ToString(), ["nativeName"] = savedDescriptor.Name, ["nativeCallback"] = false,
                ["operation"] = savedDescriptor.OperationState.ToString(), ["snapshotCount"] = persistence.SnapshotCount,
                ["archive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer()) };
            var descriptors = manager.Take(3).ToArray();
            var hashes = new JArray(rotationWrites.Select(row => (string)((JObject)row)["sha256"]));
            Require(hashes.Select(h => (string)h).Distinct(StringComparer.Ordinal).Count() == SlotRotationWrites, "Two writes of the rotation left identical bytes.");
            evidence["ownedOutputCompletion"] = new JObject { ["completionSequence"] = persistence.LastCompletedSave.Sequence, ["callbackCount"] = ownedSaveCallbacks,
                ["nativeWorkerSettled"] = true, ["nativeInventory"] = new JArray(descriptors.OrderBy(value => value.FileName, StringComparer.Ordinal).Select(OwnedNativeDescriptor)),
                ["authority"] = OwnedAuthoritySnapshot(), ["rotation"] = new JObject { ["writes"] = SlotRotationWrites, ["replacements"] = SlotRotationWrites - 1,
                    ["commitCount"] = NativeMountedArchiveCommit.CommitCount - (int)((JObject)evidence["slotRotation"])["commitCountBefore"],
                    ["replacementFailures"] = NativeMountedArchiveCommit.ReplacementFailureCount, ["failedSaves"] = persistence.FailedSaveCount,
                    ["landedLeaf"] = savedDescriptor.FileName, ["stagingLeaf"] = replaySaveAuthority.OwnedPairsReplacementLeaf,
                    ["sha256ByWrite"] = hashes, ["finalSha256"] = (string)evidence["nativeWrite"]["sha256"] } };
            Pass(SlotRotationRow);
        }
    }
}
