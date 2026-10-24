using System;
using System.Collections.Generic;
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
        private RetainedObjectIdentity areaIdentities;
        private object areaSaveOperation;
        private SaveInfo areaRequestedSave, areaPreparedSave;
        private NativeSaveCompletion areaCompletedSave;
        private bool OwnedAreaAutosave => request.OwnedPairs != null && !Cold && AreaAutosaveCase;
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
            var failed = new List<string>(); var source = descriptors.Length == 1 ? descriptors[0] : null;
            if (source == null) failed.Add("descriptorCount=" + descriptors.Length);
            else
            {
                if (!ReferenceEquals(source, replaySaveAuthority.OwnedPairsSourceDescriptor)) failed.Add("notReservedSourceDescriptor(reservedIsNull=" + (replaySaveAuthority.OwnedPairsSourceDescriptor == null) + ")");
                // WorkingFixtureLoader reads its own descriptor with SaveManager.LoadZipSave, so it is never the
                // list's object (as observed for the owner-seed Manual); both must describe the same file.
                var loaded = ownedFixtureLoader?.Descriptor;
                if (loaded == null || loaded.FileName != source.FileName || loaded.FolderName != source.FolderName || loaded.Name != source.Name || loaded.GameId != source.GameId)
                    failed.Add("notLoadedDescriptorFile(loadedIsNull=" + (loaded == null) + ")");
                if (source.FileName != request.PersistenceLoad.FileName) failed.Add("fileName=" + source.FileName);
                if (source.Name != request.PersistenceLoad.InternalName) failed.Add("name=" + source.Name);
                if (source.Type.ToString() != request.ExpectedNativeLoadType) failed.Add("type=" + source.Type + "/expected=" + request.ExpectedNativeLoadType);
                if (source.OperationState != SaveInfo.StateType.None) failed.Add("operationState=" + source.OperationState);
                if (!source.HasFileOnDisk) failed.Add("hasFileOnDisk=false");
                if (!source.IsActuallySaved) failed.Add("isActuallySaved=false");
            }
            Require(failed.Count == 0, "Inherited native source descriptor inventory changed before its selected output: " + string.Join(", ", failed.ToArray()) + ".");
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
            if (OwnedAreaAutosave) { ObserveOwnedAreaSaveCompletion(); return; }
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
                savedDescriptor.Area?.AssetGuidThreadSafe == (ManticoreFixtureCase || CapitalArrivalCase ? ExpectedArchiveArea : request.PersistenceLoad.Area) && savedDescriptor.HasFileOnDisk && savedDescriptor.IsActuallySaved &&
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
        private JObject ObserveOwnedAreaHeader(bool staged)
        {
            Require(OwnedAreaAutosave && areaScopeAdopted && inherited != null && areaIdentities != null &&
                ReferenceEquals(nativeScope, inherited.Scope) && ownedRequestedSave == null && ownedSaveCallbacks == 0 && !callback &&
                persistence.CompletedSaveCount == 0 && persistence.LastCompletedSave == null &&
                persistence.SnapshotCount == snapshotBefore + (staged ? 1 : 0) &&
                replaySaveAuthority.MatchesOwnedPairsPlan(true, false) && replaySaveAuthority.AdmittedNativeLoadRequests == 1 &&
                replaySaveAuthority.AdmittedNativeWriteRequests == 1 && replayAuthorization.AuthorizedLoadCount == 1 &&
                replayAuthorization.AuthorizedWriteCount == 1,
                "Owned area header lacks its sole authorized native request after restored destination custody.");
            inherited.RequirePreserved();
            object operation; SaveInfo requested, prepared;
            persistence.ObserveActiveSaveHeader(out operation, out requested, out prepared);
            Require(operation != null && requested != null && prepared != null && !ReferenceEquals(requested, prepared) &&
                !ReferenceEquals(requested, replaySaveAuthority.OwnedPairsSourceDescriptor) &&
                !ReferenceEquals(prepared, replaySaveAuthority.OwnedPairsSourceDescriptor) &&
                requested.Type == SaveInfo.SaveType.Auto && prepared.Type == SaveInfo.SaveType.Auto &&
                requested.Name == NativeName(SaveInfo.SaveType.Auto) && prepared.Name == requested.Name &&
                prepared.FileName == replaySaveAuthority.OwnedPairsOutputLeaf && prepared.GameId == request.PersistenceLoad.GameId &&
                prepared.GameName == request.PersistenceLoad.GameName && prepared.Area?.AssetGuidThreadSafe == ExpectedArchiveArea &&
                !prepared.IsAutoLevelupSave && prepared.FolderName == Path.Combine(replaySaveAuthority.Root, replaySaveAuthority.OwnedPairsOutputLeaf),
                "Owned area autosave changed its exact native request, prepared copy, category, destination or path.");
            if (!staged)
            {
                Require(areaSaveOperation == null && areaRequestedSave == null && areaPreparedSave == null,
                    "Owned area autosave observed a duplicate native header.");
                areaSaveOperation = operation; areaRequestedSave = requested; areaPreparedSave = prepared;
            }
            else Require(ReferenceEquals(operation, areaSaveOperation) && ReferenceEquals(requested, areaRequestedSave) &&
                ReferenceEquals(prepared, areaPreparedSave), "Owned area staging belongs to another native operation or prepared copy.");
            return new JObject { ["operation"] = areaIdentities.Get(operation), ["requested"] = areaIdentities.Get(requested),
                ["prepared"] = areaIdentities.Get(prepared), ["sourceDescriptor"] = areaIdentities.Get(replaySaveAuthority.OwnedPairsSourceDescriptor),
                ["requestedType"] = requested.Type.ToString(), ["requestedName"] = requested.Name,
                ["preparedType"] = prepared.Type.ToString(), ["preparedName"] = prepared.Name,
                ["preparedLeaf"] = prepared.FileName, ["preparedArea"] = prepared.Area.AssetGuidThreadSafe,
                ["preparedPath"] = prepared.FolderName, ["completionCount"] = persistence.CompletedSaveCount,
                ["callbackCount"] = ownedSaveCallbacks, ["snapshotCount"] = persistence.SnapshotCount };
        }
        private void ObserveOwnedAreaSaveCompletion()
        {
            var completion = persistence.LastCompletedSave;
            Require(OwnedAreaAutosave && areaScopeAdopted && areaSaveOperation != null && areaRequestedSave != null && areaPreparedSave != null &&
                areaHeaderStarting != null && areaHeaderStaged != null && areaObserverError == null && ownedRequestedSave == null &&
                ownedSaveCallbacks == 0 && !callback && completion != null && persistence.CompletedSaveCount == 1 &&
                completion.Sequence == snapshotBefore + 1 && ReferenceEquals(completion.Requested, areaRequestedSave) &&
                ReferenceEquals(completion.Written, areaPreparedSave) && ReferenceEquals(savedDescriptor, areaPreparedSave) &&
                completion.Path == Path.Combine(replaySaveAuthority.Root, replaySaveAuthority.OwnedPairsOutputLeaf) &&
                replaySaveAuthority.MatchesOwnedPairsPlan(true, false) && replaySaveAuthority.AdmittedNativeLoadRequests == 1 &&
                replaySaveAuthority.AdmittedNativeWriteRequests == 1 && replayAuthorization.AuthorizedLoadCount == 1 &&
                replayAuthorization.AuthorizedWriteCount == 1 && !NativePersistenceIsolation.HasPendingWrites &&
                !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence,
                "Owned area completion is not the exact observed native operation/copy and settled worker.");
            var descriptors = Game.Instance.SaveManager.Take(3).ToArray();
            Require(descriptors.Length == 2 && descriptors.Count(value => ReferenceEquals(value, replaySaveAuthority.OwnedPairsSourceDescriptor)) == 1 &&
                descriptors.Count(value => ReferenceEquals(value, areaPreparedSave)) == 1 && savedDescriptor.FileName == replaySaveAuthority.OwnedPairsOutputLeaf &&
                savedDescriptor.Name == replaySaveAuthority.OwnedPairsOutputName && savedDescriptor.Type == SaveInfo.SaveType.Auto &&
                savedDescriptor.GameId == request.PersistenceLoad.GameId && savedDescriptor.GameName == request.PersistenceLoad.GameName &&
                savedDescriptor.Area?.AssetGuidThreadSafe == ExpectedArchiveArea && savedDescriptor.HasFileOnDisk && savedDescriptor.IsActuallySaved &&
                !savedDescriptor.IsAutoLevelupSave && savedDescriptor.OperationState == SaveInfo.StateType.None,
                "Owned area Auto did not settle the exact immutable Manual and fresh destination descriptor.");
            Require(areaCompletedSave == null, "Owned area completion was observed twice."); areaCompletedSave = completion;
            evidence["ownedOutputCompletion"] = new JObject { ["completionSequence"] = completion.Sequence, ["callbackCount"] = ownedSaveCallbacks,
                ["nativeWorkerSettled"] = true, ["nativeInventory"] = new JArray(descriptors.OrderBy(value => value.FileName, StringComparer.Ordinal).Select(OwnedNativeDescriptor)),
                ["authority"] = OwnedAuthoritySnapshot(), ["areaSaveIdentity"] = new JObject {
                    ["operation"] = areaIdentities.Get(areaSaveOperation), ["requested"] = areaIdentities.Get(completion.Requested),
                    ["prepared"] = areaIdentities.Get(completion.Written), ["completion"] = areaIdentities.Get(completion),
                    ["path"] = completion.Path, ["sourceDescriptor"] = areaIdentities.Get(replaySaveAuthority.OwnedPairsSourceDescriptor) } };
        }
        private void ReleaseOwnedAreaObservation()
        {
            if (areaIdentities == null) return;
            Require(!areaObserverAttached && !NativePersistenceIsolation.HasPendingWrites && !persistence.HasActiveSaveScope &&
                !persistence.HasSaveAdmissionFence, "Owned area native observation still owns an active callback or worker.");
            var ids = areaIdentities.Ids; areaIdentities.Dispose();
            if (evidence["areaTransfer"] is JObject row)
                row["identityRetention"] = new JObject { ["capacity"] = areaIdentities.Capacity, ["ids"] = new JArray(ids),
                    ["faults"] = areaIdentities.FaultCount, ["released"] = areaIdentities.Released,
                    ["retainedAfterRelease"] = areaIdentities.RetainedCount };
            Require(areaIdentities.Released && areaIdentities.RetainedCount == 0 && areaIdentities.FaultCount == 0,
                "Owned area identity retention did not release exactly.");
            areaSaveOperation = null; areaRequestedSave = null; areaPreparedSave = null; areaCompletedSave = null;
        }
        // Owner-seed-derived cold proof, beyond the shared exact restoration checks: this
        // separate process restored exactly the schema-3 graph its producer read back from the
        // bytes it wrote; both pairs carry the producer's runtime owner identities, generations
        // and profiles; the control slots are the producer's; and unrelated field actors keep
        // their native expenditure within actual clock progress. Decode success alone is not
        // accepted: the restored world and live registry are compared with the producer runtime.
        private void ObserveOwnerSeedDerivedCold()
        {
            Require((request.OwnedPairs.IsOwnerSeedDerivedLoad || OwnerSeedLoadRefusalCase) && Cold && inherited != null && inherited.Bound && saved != null,
                "Owner-seed-derived cold proof requires its bound producer custody and restored graph.");
            var boundary = inherited.OwnerSeedBoundary;
            var restored = inherited.VerifyRestoredOwnerSeedPairs(saved, world, controls.CapturePersistentSlots());
            var elapsed = Math.Max(0, (Game.Instance.TimeController.GameTime.Ticks - (long)boundary["gameTicks"]) / (double)TimeSpan.TicksPerSecond);
            var unrelated = boundary["unrelated"] as JArray; var pairIds = actors.Select(actor => actor.UniqueId).ToArray();
            Require(unrelated != null && unrelated.Count == inherited.Party.Length - 4 &&
                inherited.Party.Where(actor => !pairIds.Contains(actor.UniqueId)).All(actor => unrelated.Count(row => (string)row["actorId"] == actor.UniqueId) == 1),
                "Producer unrelated-actor custody differs from the restored native field.");
            var serializer = MountedSaveCodec.CreateSerializer(); var unrelatedRows = new JArray();
            foreach (JObject row in unrelated)
            {
                var before = row["native"].ToObject<SavedNativeActor>(serializer); var after = MountedPersistenceService.CaptureActor(Exact((string)row["actorId"]));
                Require(Chunk7WorldScenarioEngine.ManualDebtWithinClock(before, after, elapsed),
                    "Unrelated native actor expenditure changed across the owner-seed-derived cold load.");
                unrelatedRows.Add(new JObject { ["actorId"] = row["actorId"], ["before"] = row["native"].DeepClone(), ["after"] = JObject.FromObject(after, serializer) });
            }
            evidence["ownerSeedDerivedCold"] = new JObject { ["contract"] = "owner-seed-derived-cold-v1", ["ancestry"] = Chunk7WorldScenarioEngine.OwnerSeedDerivedAncestry,
                ["sourceRunId"] = request.OwnedPairs.SourceRunId, ["sourceArtifactSha256"] = request.OwnedPairs.SourceArtifactSha256,
                ["sourceArchiveSha256"] = request.OwnedPairs.SourceArchiveSha256, ["seedSha256"] = request.OwnedPairs.SeedSha256,
                ["producerProcessId"] = restored["producerProcessId"], ["loadedEqualsProducerReadback"] = restored["loadedEqualsProducerReadback"], ["elapsedSeconds"] = elapsed,
                ["owners"] = restored["owners"], ["slots"] = restored["slots"], ["unrelated"] = unrelatedRows };
            Pass("C7-persistence-owner-seed-derived-cold-identity");
        }
        // Area entry reminted both owner identities. Compare against this Manual's own
        // post-entry producer boundary, never the earlier owner-seed Manual boundary.
        private void ObserveManticoreManualCold()
        {
            Require(request.OwnedPairs.IsOwnerSeedDerivedLoad && Cold && inherited != null && inherited.Bound && saved != null,
                "Manticore Manual cold proof requires the exact restored producer graph.");
            var restored = inherited.VerifyRestoredOwnerSeedPairs(saved, world, controls.CapturePersistentSlots());
            var pairIds = actors.Select(actor => actor.UniqueId).ToArray();
            var unrelatedIds = inherited.Party.Where(actor => !pairIds.Contains(actor.UniqueId))
                .Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            Require(unrelatedIds.Length == request.OwnedPairs.PartyIds.Length - 4 &&
                unrelatedIds.SequenceEqual(request.OwnedPairs.PartyIds.Where(id => !pairIds.Contains(id)).OrderBy(id => id, StringComparer.Ordinal)),
                "Manticore cold load lost an unrelated field actor.");
            // The Manticore producer recorded paired native debt, but no pre-save
            // unrelated debt. This row proves unrelated membership, not debt continuity.
            evidence["manticoreManualCold"] = new JObject { ["contract"] = "manticore-native-manual-cold-v1",
                ["ancestry"] = Chunk7WorldScenarioEngine.OwnerSeedDerivedAncestry,
                ["sourceRunId"] = request.OwnedPairs.SourceRunId,
                ["sourceArtifactSha256"] = request.OwnedPairs.SourceArtifactSha256,
                ["sourceArchiveSha256"] = request.OwnedPairs.SourceArchiveSha256,
                ["seedSha256"] = request.OwnedPairs.SeedSha256,
                ["sourceArea"] = Chunk7OwnedPairsSelection.ManticoreFixtureArea,
                ["producerProcessId"] = restored["producerProcessId"],
                ["loadedEqualsProducerReadback"] = restored["loadedEqualsProducerReadback"],
                ["owners"] = restored["owners"], ["slots"] = restored["slots"],
                ["unrelatedActorIds"] = new JArray(unrelatedIds) };
            Pass("C7-persistence-manticore-manual-cold-identity");
        }
    }
}
