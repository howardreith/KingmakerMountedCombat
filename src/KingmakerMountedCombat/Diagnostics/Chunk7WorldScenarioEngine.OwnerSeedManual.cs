using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Owner-seed-derived Manual producer. The disclosed SETUP and the two native Mounts are
    // the unchanged owner-seed steps. Once both pairs are mounted and idle, each rider's two
    // owned controls are bound through the native hotbar and exactly one genuine native Manual
    // is requested through SaveManager and the KMC serialization barrier into the reserved
    // run-root leaf. The receipt records the four actors, both pair owners, generations,
    // profiles and control slots at the boundary; the native request, header (prepared copy),
    // staged KMC metadata, worker and completion; the written leaf and SHA256; and the schema-3
    // metadata read back from those bytes, compared with the live runtime. Its ancestry is
    // owner-seed-derived with disclosed setup assistance: never acquired, ordinary or a level-up
    // save, and never restoration proof by itself (only a separate cold process can be).
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        internal const string OwnerSeedManualRow = "C7-world-owner-seed-manual";
        internal const string OwnerSeedManualContract = "owner-seed-derived-manual-producer-v1";
        internal const string OwnerSeedManualReceiptContract = "owner-seed-derived-manual-v1";
        internal const string OwnerSeedDerivedAncestry = "owner-seed-derived";
        internal const string OwnerSeedManualDisclosure = "Fixture setup on a disposable run copy of an owner-supplied seed: each Horse was created by the native ordinary Ranger level-up (Hunter's Bond -> Ranger companion -> KMC Horse) and rider B received only the exact native XP its Ranger 4 eligibility requires. This is not acquisition proof. Setup makes no native write; after SETUP-COMPLETE the only native write is one measured Manual into the run-owned isolated save root, labelled owner-seed-derived with disclosed assistance.";
        private bool OwnerSeedManual => OwnerSeed && request.OwnedPairs.IsOwnerSeedManual;
        private bool OwnerSeedManualWritten => OwnerSeedManual && manualSaved != null;
        private SaveInfo manualRequested, manualSaved, manualHeaderRequested, manualHeaderPrepared;
        private object manualHeaderOperation;
        private int manualCallbacks, manualSnapshotBefore, manualHeaderStarts, manualHeaderStages;
        private long manualCastBefore;
        private bool manualObserverAttached, manualWorkerObserved;
        private string manualObserverError;
        private JObject manualHeaderStarting, manualHeaderStaged, manualBoundary;
        private JArray manualHotbar;
        private SavedMountedSlot[] manualSlots;

        private void BeginOwnerSeedManualSave()
        {
            Require(OwnerSeedManual && setupLedger.Completed && setupLedger.RefusedAfterBoundary == 0 && manualRequested == null,
                "The owner-seed Manual may follow only its closed SETUP-COMPLETE boundary, once.");
            AssertBoth(); Next(60);
        }

        private void UpdateOwnerSeedManual()
        {
            Require(OwnerSeedManual, "Owner-seed Manual step outside its producer case.");
            switch (step)
            {
                case 60:
                    // Both pairs mounted and idle, every issued native command settled, no writer.
                    AssertBoth();
                    if (!Actors().All(Idle) || !NativeCommandsSettled() || controls.HasUnsettledRelationshipTransition ||
                        world.UnifiedTurn.HasUnsettledPreparation || SetupBusy() || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence) return;
                    BindOwnerSeedManualControls(); RequestOwnerSeedManual(); Next(61); break;
                case 61:
                    if (persistence.ActiveSaveWorkerRunning) manualWorkerObserved = true;
                    if (manualCallbacks == 0 || SetupBusy() || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence) return;
                    ObserveOwnerSeedManual(); Pass(OwnerSeedManualRow); BeginCleanup(); break;
                default: throw new InvalidOperationException("Unknown owner-seed Manual step " + step + ".");
            }
        }

        // The same native hotbar binding the owned persistence source uses: each rider's
        // Dismount and primary owned controls in its first empty native slot.
        private void BindOwnerSeedManualControls()
        {
            Require(manualSlots == null, "Owner-seed Manual controls may bind only once.");
            controls.Update(); manualHotbar = new JArray();
            foreach (var rider in new[] { firstRider, secondRider })
                foreach (var blueprint in new[] { controls.DismountAbility, controls.RiderPrimaryAbility })
                {
                    var fact = rider.Descriptor.Abilities.GetAbility(blueprint); var nativeSlots = rider.UISettings.Slots;
                    var index = Array.FindIndex(nativeSlots, s => s == null || s is MechanicActionBarSlotEmpty);
                    Require(fact != null && index >= 0 && index < 128, "Native hotbar lacks a declared empty owned slot.");
                    rider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = rider, Ability = fact.Data }, index);
                    manualHotbar.Add(new JObject { ["actorId"] = rider.UniqueId, ["abilityGuid"] = blueprint.AssetGuid, ["index"] = index,
                        ["nativeApi"] = "UnitUISettings.SetSlot(MechanicActionBarSlotAbility)", ["afterSetupComplete"] = true });
                }
            manualSlots = controls.CapturePersistentSlots();
            Require(manualSlots.Length == 4 && manualSlots.Count(s => s.ActorId == firstRider.UniqueId) == 2 &&
                manualSlots.Count(s => s.ActorId == secondRider.UniqueId) == 2, "All-pair native hotbar binding differs.");
        }

        private JObject ManualReservation() => new JObject { ["leaf"] = fixtureAuthority.OwnedPairsOutputLeaf,
            ["nativeName"] = fixtureAuthority.OwnedPairsOutputName, ["nativeType"] = fixtureAuthority.OwnedPairsOutputType,
            ["number"] = fixtureAuthority.OwnedPairsOutputNumber, ["reservationCount"] = fixtureAuthority.OwnedPairsReservationCount,
            ["unwritten"] = fixtureAuthority.AdmittedNativeWriteRequests == 0 };

        private JObject ManualAuthority() => new JObject {
            ["root"] = fixtureAuthority.Root, ["loadCap"] = fixtureAuthority.MaximumNativeLoadRequests,
            ["writeCap"] = fixtureAuthority.MaximumNativeWriteRequests,
            ["nativeLoads"] = fixtureAuthority.AdmittedNativeLoadRequests, ["nativeWrites"] = fixtureAuthority.AdmittedNativeWriteRequests,
            ["authorizedLoads"] = fixtureAuthorization.AuthorizedLoadCount, ["authorizedWrites"] = fixtureAuthorization.AuthorizedWriteCount,
            ["reservationCount"] = fixtureAuthority.OwnedPairsReservationCount,
            ["number"] = fixtureAuthority.OwnedPairsOutputNumber, ["leaf"] = fixtureAuthority.OwnedPairsOutputLeaf,
            ["nativeName"] = fixtureAuthority.OwnedPairsOutputName, ["nativeType"] = fixtureAuthority.OwnedPairsOutputType };

        private static JObject ManualDescriptor(SaveInfo value) => new JObject {
            ["fileName"] = value.FileName, ["nativeName"] = value.Name, ["nativeType"] = value.Type.ToString(),
            ["gameId"] = value.GameId, ["gameName"] = value.GameName, ["area"] = value.Area?.AssetGuidThreadSafe,
            ["hasFileOnDisk"] = value.HasFileOnDisk, ["isActuallySaved"] = value.IsActuallySaved,
            ["operation"] = value.OperationState.ToString(), ["autoLevelup"] = value.IsAutoLevelupSave };

        private void RequestOwnerSeedManual()
        {
            var manager = Game.Instance.SaveManager; var input = request.PersistenceLoad; var name = Chunk7OwnedPairsSelection.OwnerSeedManualName;
            Require(!LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any() && !NativePersistenceIsolation.HasPendingWrites &&
                !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && manualRequested == null && persistence.CompletedSaveCount == 0 &&
                !setupSlotOwned && !NativePersistenceIsolation.NativeSlotRotationEnabled && setupLedger.Completed && setupLedger.RefusedAfterBoundary == 0,
                "Owner-seed Manual admission found a native load/writer, a prior save, a slot projection or an open setup.");
            Require(fixtureAuthorization.IsActive && fixtureAuthorization.IsExactPersistenceScope(fixtureAuthority) && NativePersistenceIsolation.IsExactAuthority(fixtureAuthority) &&
                fixtureAuthority.MatchesOwnedPairsPlan(true, true) && fixtureAuthority.OwnedPairsReservationCount == 1 &&
                fixtureAuthority.OwnedPairsOutputType == SaveInfo.SaveType.Manual.ToString() && fixtureAuthority.OwnedPairsOutputName == name &&
                fixtureAuthority.AdmittedNativeLoadRequests == 1 && fixtureAuthorization.AuthorizedLoadCount == 1 &&
                fixtureAuthority.AdmittedNativeWriteRequests == 0 && fixtureAuthorization.AuthorizedWriteCount == 0 && fixtureAuthorization.UnauthorizedWriteCount == 0 &&
                manager.SavePath == fixtureAuthority.Root && manager.IsSaveAllowed(),
                "Owner-seed Manual lacks its exact unused reserved native write after the completed sole load.");
            var descriptors = manager.Take(3).ToArray();
            // Each condition is named so a native refusal says which one failed (the first native
            // producer run, 20261018-c7b-world23-ownerseed-b3s1-save, refused here without it).
            var seed = descriptors.Length > 0 ? descriptors[0] : null;
            var failed = new System.Collections.Generic.List<string>();
            if (descriptors.Length != 1) failed.Add("descriptorCount=" + descriptors.Length);
            if (seed != null)
            {
                if (!ReferenceEquals(seed, fixtureAuthority.OwnedPairsSourceDescriptor)) failed.Add("notReservedSourceDescriptor(reservedIsNull=" + (fixtureAuthority.OwnedPairsSourceDescriptor == null) + ")");
                // WorkingFixtureLoader reads its own descriptor with SaveManager.LoadZipSave, so it is never
                // the list's object (observed in 20261018-c7b-world24-ownerseed-b3s1b-save); both must
                // describe the same file. The list entry itself stays pinned to the reserved descriptor above.
                var loaded = fixtureLoader.Descriptor;
                if (loaded == null || loaded.FileName != seed.FileName || loaded.FolderName != seed.FolderName || loaded.Name != seed.Name || loaded.GameId != seed.GameId)
                    failed.Add("notLoadedDescriptorFile(loadedIsNull=" + (loaded == null) + ")");
                if (seed.FileName != input.FileName) failed.Add("fileName=" + seed.FileName);
                if (seed.Name != input.InternalName) failed.Add("name=" + seed.Name);
                if (seed.Type.ToString() != request.ExpectedNativeLoadType) failed.Add("type=" + seed.Type + "/expected=" + request.ExpectedNativeLoadType);
                if (seed.OperationState != SaveInfo.StateType.None) failed.Add("operationState=" + seed.OperationState);
                if (!seed.HasFileOnDisk) failed.Add("hasFileOnDisk=false");
                if (!seed.IsActuallySaved) failed.Add("isActuallySaved=false");
            }
            Require(failed.Count == 0, "Owner-seed native seed descriptor inventory changed before its sole Manual: " + string.Join(", ", failed.ToArray()) + ".");
            var number = manager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual);
            var leaf = "Manual_" + number.ToString(CultureInfo.InvariantCulture) + "_" + name + ".zks";
            Require(number == fixtureAuthority.OwnedPairsOutputNumber && leaf == fixtureAuthority.OwnedPairsOutputLeaf &&
                !File.Exists(Path.Combine(fixtureAuthority.Root, leaf)), "Owner-seed native Manual numbering/absence changed after reservation.");
            var reason = fixtureAuthority.ValidateArchiveIdentity(new RuntimeSaveTarget { FullPath = Path.Combine(fixtureAuthority.Root, input.FileName),
                FileName = input.FileName, InternalName = input.InternalName, SaveType = request.ExpectedNativeLoadType,
                GameId = input.GameId, GameName = input.GameName, Area = input.Area }, manager.SavePath);
            Require(reason == null, "Owner-seed immutable seed copy changed before its sole native Manual: " + reason);
            evidence["ownedOutputAdmission"] = new JObject { ["method"] = "FindUnusedSaveNumber", ["token"] = "06008024",
                ["moduleMvid"] = typeof(SaveManager).Module.ModuleVersionId.ToString("D"), ["number"] = number, ["leaf"] = leaf,
                ["nativeInventory"] = new JArray(descriptors.Select(ManualDescriptor)), ["authority"] = ManualAuthority() };
            // Captured with no native writer active: everything the separate cold process must restore.
            manualBoundary = CaptureManualBoundary();
            // A Manual needs no Quick/Auto slot projection: the native settings stay as the owner set them.
            var descriptor = manager.CreateNewSave(name);
            Require(descriptor != null && descriptor.Type == SaveInfo.SaveType.Manual && descriptor.Name == name && !descriptor.IsActuallySaved &&
                !descriptor.IsAutoLevelupSave && manager.IsSaveAllowed(), "Actual native Manual admission differs.");
            manualRequested = descriptor; manualSnapshotBefore = persistence.SnapshotCount; manualCastBefore = controls.NativeCastRequestCount;
            persistence.SaveSnapshotStarting += ObserveManualHeaderStarting; persistence.SaveSnapshotStaged += ObserveManualHeaderStaged;
            manualObserverAttached = true;
            evidence["saveRequested"] = new JObject { ["nativeType"] = descriptor.Type.ToString(), ["nativeName"] = descriptor.Name,
                ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["snapshotCount"] = persistence.SnapshotCount,
                ["nativeSaveAllowed"] = true, ["newSlot"] = true, ["levelUpSave"] = false, ["api"] = "Game.SaveGame(SaveManager.CreateNewSave(name))" };
            Game.Instance.SaveGame(descriptor, () => manualCallbacks++);
        }

        // SaveRoutine's game-thread header: the requested descriptor and the native PrepareSave copy.
        private void ObserveManualHeaderStarting()
        {
            try
            {
                manualHeaderStarts++;
                Require(manualHeaderStarts == 1 && manualRequested != null, "The owner-seed Manual observed a second or unrequested native header.");
                persistence.ObserveActiveSaveHeader(out var operation, out var requested, out var prepared);
                Require(ReferenceEquals(requested, manualRequested) && prepared != null && !ReferenceEquals(prepared, requested) &&
                    !ReferenceEquals(prepared, fixtureAuthority.OwnedPairsSourceDescriptor) && prepared.Type == SaveInfo.SaveType.Manual &&
                    prepared.Name == Chunk7OwnedPairsSelection.OwnerSeedManualName && prepared.FileName == fixtureAuthority.OwnedPairsOutputLeaf &&
                    !prepared.IsAutoLevelupSave && prepared.FolderName == Path.Combine(fixtureAuthority.Root, fixtureAuthority.OwnedPairsOutputLeaf),
                    "Owner-seed native Manual changed its requested descriptor, prepared copy, category, destination or path.");
                manualHeaderOperation = operation; manualHeaderRequested = requested; manualHeaderPrepared = prepared;
                manualHeaderStarting = ManualHeaderRow("native-header-starting", prepared);
            }
            catch (Exception error) { manualObserverError = manualObserverError ?? error.ToString(); }
        }

        // The KMC serialization barrier staged its metadata for this exact native operation.
        private void ObserveManualHeaderStaged()
        {
            try
            {
                manualHeaderStages++;
                Require(manualHeaderStages == 1 && manualHeaderStarting != null, "The owner-seed Manual staged twice or before its header.");
                persistence.ObserveActiveSaveHeader(out var operation, out var requested, out var prepared);
                Require(ReferenceEquals(operation, manualHeaderOperation) && ReferenceEquals(requested, manualHeaderRequested) &&
                    ReferenceEquals(prepared, manualHeaderPrepared) && persistence.SnapshotCount == manualSnapshotBefore + 1,
                    "Owner-seed KMC staging belongs to another native operation or did not stage exactly once.");
                manualHeaderStaged = ManualHeaderRow("native-header-staged", prepared);
            }
            catch (Exception error) { manualObserverError = manualObserverError ?? error.ToString(); }
        }

        private JObject ManualHeaderRow(string label, SaveInfo prepared) => new JObject { ["label"] = label, ["frame"] = Time.frameCount,
            ["requestedIsSelectedDescriptor"] = true, ["preparedIsCopy"] = true, ["preparedLeaf"] = prepared.FileName,
            ["preparedName"] = prepared.Name, ["preparedType"] = prepared.Type.ToString(), ["preparedPath"] = prepared.FolderName,
            ["snapshots"] = persistence.SnapshotCount, ["activeOwners"] = world.Registry.ActiveCount,
            ["nativeWrites"] = fixtureAuthority.AdmittedNativeWriteRequests, ["serializationBusy"] = world.SerializationBusy };

        private void DetachManualObservers()
        {
            if (!manualObserverAttached) return;
            persistence.SaveSnapshotStarting -= ObserveManualHeaderStarting; persistence.SaveSnapshotStaged -= ObserveManualHeaderStaged;
            manualObserverAttached = false;
        }

        // Cleanup admission: no Manual scope, fence or writer remains and the observers detached.
        private bool OwnerSeedManualCleanupReady()
        {
            if (persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence || NativePersistenceIsolation.HasPendingWrites || world.SerializationBusy)
                return false;
            DetachManualObservers();
            return !manualObserverAttached;
        }

        private void ObserveOwnerSeedManual()
        {
            Require(manualObserverError == null && manualHeaderStarting != null && manualHeaderStaged != null && manualHeaderStarts == 1 && manualHeaderStages == 1,
                "The native Manual header and KMC staging were not each observed exactly once: " + manualObserverError);
            DetachManualObservers();
            var completion = persistence.LastCompletedSave; var manager = Game.Instance.SaveManager; var input = request.PersistenceLoad;
            Require(manualCallbacks == 1 && completion != null && persistence.CompletedSaveCount == 1 && completion.Sequence == manualSnapshotBefore + 1 &&
                ReferenceEquals(completion.Requested, manualRequested) && ReferenceEquals(completion.Written, manualHeaderPrepared) &&
                completion.Path == Path.Combine(fixtureAuthority.Root, fixtureAuthority.OwnedPairsOutputLeaf) &&
                fixtureAuthority.MatchesOwnedPairsPlan(true, false) && fixtureAuthority.AdmittedNativeLoadRequests == 1 &&
                fixtureAuthority.AdmittedNativeWriteRequests == 1 && fixtureAuthorization.AuthorizedLoadCount == 1 &&
                fixtureAuthorization.AuthorizedWriteCount == 1 && fixtureAuthorization.UnauthorizedWriteCount == 0 &&
                !NativePersistenceIsolation.HasPendingWrites && !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence,
                "Owner-seed native Manual lost its exact requested/prepared descriptor, completed worker or single callback.");
            manualSaved = completion.Written;
            var descriptors = manager.Take(3).ToArray();
            Require(descriptors.Length == 2 && descriptors.Count(value => ReferenceEquals(value, fixtureAuthority.OwnedPairsSourceDescriptor)) == 1 &&
                descriptors.Count(value => ReferenceEquals(value, manualSaved)) == 1 && manualSaved.FileName == fixtureAuthority.OwnedPairsOutputLeaf &&
                manualSaved.Name == Chunk7OwnedPairsSelection.OwnerSeedManualName && manualSaved.Type == SaveInfo.SaveType.Manual &&
                manualSaved.GameId == input.GameId && manualSaved.GameName == input.GameName && manualSaved.Area?.AssetGuidThreadSafe == input.Area &&
                manualSaved.HasFileOnDisk && manualSaved.IsActuallySaved && manualSaved.OperationState == SaveInfo.StateType.None && !manualSaved.IsAutoLevelupSave,
                "Owner-seed native Manual did not settle the exact two-entry isolated inventory.");
            evidence["ownedOutputCompletion"] = new JObject { ["completionSequence"] = completion.Sequence, ["callbackCount"] = manualCallbacks,
                ["nativeWorkerSettled"] = true, ["nativeInventory"] = new JArray(descriptors.OrderBy(value => value.FileName, StringComparer.Ordinal).Select(ManualDescriptor)),
                ["authority"] = ManualAuthority() };
            // Read the schema-3 metadata back from the written bytes and bind it to the live runtime.
            var read = NativeMountedSaveStorage.ReadArchive(manualSaved.Saver); var saved = read.Data;
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 3 && saved != null && saved.SchemaVersion == 3 && saved.Pairs.Length == 2 &&
                saved.Participations.Length == 0 && saved.Combat == null && saved.CampaignId == input.GameId && saved.AreaId == input.Area,
                "Owner-seed native Manual readback did not retain the entire two-pair exploration world.");
            AssertBoth();
            var rows = (JArray)manualBoundary["pairs"];
            Require(saved.Pairs.All(pair => rows.Count(row => (string)row["ownerId"] == Guid.Parse(pair.PairId).ToString("N") &&
                (long)row["generation"] == pair.Generation && (string)row["riderId"] == pair.Rider.Id && (string)row["mountId"] == pair.Mount.Id &&
                (string)row["profileId"] == pair.ProfileId) == 1) && saved.Pairs.Select(pair => Guid.Parse(pair.PairId)).Distinct().Count() == 2 &&
                saved.Pairs.All(pair => world.Registry.FindActive(Exact(pair.Rider.Id))?.Id == Guid.Parse(pair.PairId)),
                "Read-back pair records differ from the live runtime owners, generations, actors or profiles at the save boundary.");
            Require(saved.Slots.Length == manualSlots.Length && manualSlots.All(s => saved.Slots.Count(x => x.ActorId == s.ActorId && x.Index == s.Index && x.Kind == s.Kind) == 1),
                "Read-back control slots differ from the bound native hotbar.");
            var boundaryActors = (JArray)manualBoundary["actors"];
            var elapsed = Math.Max(0, (saved.GameTimeTicks - (long)manualBoundary["gameTicks"]) / (double)TimeSpan.TicksPerSecond);
            Require(saved.Pairs.SelectMany(pair => new[] { pair.Rider, pair.Mount }).All(actor =>
                ManualDebtWithinClock(boundaryActors.Single(row => (string)row["Id"] == actor.Id).ToObject<SavedNativeActor>(MountedSaveCodec.CreateSerializer()), actor, elapsed)),
                "Read-back actor expenditure differs from the live runtime at the save boundary.");
            Require(persistence.SnapshotCount == manualSnapshotBefore + 1 && controls.NativeCastRequestCount == manualCastBefore &&
                !controls.SerializationSuspended && !persistence.SaveSuspended, "Native Manual replayed input or retained an owned serialization lease.");
            var path = Path.Combine(fixtureAuthority.Root, fixtureAuthority.OwnedPairsOutputLeaf); var file = new FileInfo(manualSaved.FolderName);
            Require(file.FullName == path && file.Exists && file.Length > 0, "Owner-seed native Manual escaped its reserved run-root leaf.");
            var archive = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer());
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["sha256"] = ManualHash(file.FullName), ["length"] = file.Length,
                ["lastWriteTimeUtcTicks"] = file.LastWriteTimeUtc.Ticks, ["fileName"] = manualSaved.FileName, ["nativeType"] = manualSaved.Type.ToString(),
                ["nativeName"] = manualSaved.Name, ["gameId"] = manualSaved.GameId, ["gameName"] = manualSaved.GameName, ["area"] = manualSaved.Area.AssetGuidThreadSafe,
                ["nativeCallback"] = manualCallbacks == 1, ["operation"] = manualSaved.OperationState.ToString(), ["snapshotCount"] = persistence.SnapshotCount,
                ["readKind"] = read.Kind.ToString(), ["sourceSchema"] = read.SourceSchema, ["archive"] = archive };
            var roster = Chunk7NativeFieldRoster.Capture(Game.Instance.Player);
            var seedPath = Path.Combine(fixtureAuthority.Root, input.FileName);
            evidence["ownerSeedManual"] = new JObject { ["contract"] = OwnerSeedManualReceiptContract, ["ancestry"] = OwnerSeedDerivedAncestry,
                ["disclosedAssistance"] = true, ["assistanceCount"] = setupLedger.Items.Count, ["acquisitionProof"] = false,
                ["ordinaryProducerAncestry"] = false, ["levelUpSave"] = false, ["restorationProof"] = false,
                ["setupCompletedBeforeSave"] = setupLedger.Completed, ["setupMutationsAfterBoundary"] = setupLedger.RefusedAfterBoundary,
                ["seedId"] = request.OwnedPairs.SeedId, ["seedSha256"] = request.OwnedPairs.SeedSha256,
                ["seedCopyUnchanged"] = ManualHash(seedPath) == request.OwnedPairs.SeedSha256, ["processId"] = Process.GetCurrentProcess().Id,
                ["boundary"] = manualBoundary, ["hotbar"] = manualHotbar, ["headerStarting"] = manualHeaderStarting, ["headerStaged"] = manualHeaderStaged,
                ["workerObservedRunning"] = manualWorkerObserved, ["completionSequence"] = completion.Sequence,
                ["fieldWorld"] = new JObject { ["gameId"] = Game.Instance.Player.GameId, ["gameName"] = input.GameName,
                    ["area"] = Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe, ["mainActorId"] = Game.Instance.Player.MainCharacter.Value.UniqueId,
                    ["party"] = new JArray(roster.Select(actor => new JObject { ["id"] = actor.UniqueId, ["blueprint"] = actor.Blueprint?.AssetGuid })) },
                ["twoOwners"] = new JArray(new[] { 0, 1 }.Select(ManualOwnerRow)),
                ["arrangement"] = manualBoundary["arrangement"].DeepClone(), ["arrangementAfterSave"] = Chunk7OwnedPairFixture.ArrangementOf(roster) };
            Require((bool)evidence["ownerSeedManual"]["seedCopyUnchanged"], "The run copy of the owner's immutable seed changed during the producer.");
            Require(JToken.DeepEquals(manualBoundary["arrangement"], evidence["ownerSeedManual"]["arrangementAfterSave"]),
                "Native class/XP/money state changed across the owner-seed Manual.");
            snapshots.Add(CaptureWorld("native-manual-complete"));
        }

        private JObject ManualOwnerRow(int index)
        {
            var rider = index == 0 ? firstRider : secondRider; var mount = index == 0 ? firstMount : secondMount;
            var feature = rider.Descriptor.GetFact(horses.HorseFeature) as Feature; var rank = rider.Descriptor.GetFact(horses.LevelRank);
            Require(feature?.Source != null && rank != null && ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider),
                "Owner-seed Manual pair lost its reciprocal Horse, sourced feature or rank.");
            return new JObject { ["actorId"] = rider.UniqueId, ["petId"] = mount.UniqueId, ["sourceGuid"] = feature.Source.AssetGuid,
                ["featureGuid"] = horses.HorseFeature.AssetGuid, ["rank"] = rank.GetRank(),
                ["profileId"] = SupportedMountedProfiles.Resolve(rider, mount)?.Id, ["presentation"] = "fixture-setup", ["acquisitionProof"] = false };
        }

        private JObject CaptureManualBoundary()
        {
            var serializer = MountedSaveCodec.CreateSerializer(); var roster = Chunk7NativeFieldRoster.Capture(Game.Instance.Player); var pairActors = Actors();
            Require(pairActors.Length == 4 && pairActors.All(actor => roster.Contains(actor)), "Owner-seed Manual boundary lost a pair actor from the native field.");
            return new JObject { ["label"] = "save-boundary", ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["actorIds"] = new JArray(pairActors.Select(actor => actor.UniqueId)),
                ["pairs"] = new JArray(new[] { firstRider, secondRider }.Select(rider =>
                {
                    var owner = world.Registry.FindActive(rider); var mount = ReferenceEquals(rider, firstRider) ? firstMount : secondMount;
                    return new JObject { ["ownerId"] = owner.Id.ToString("N"), ["generation"] = owner.Generation, ["riderId"] = rider.UniqueId,
                        ["mountId"] = mount.UniqueId, ["profileId"] = SupportedMountedProfiles.Resolve(rider, mount)?.Id, ["state"] = owner.State.ToString(),
                        ["relationshipState"] = owner.Boundary.State.ToString() };
                })),
                ["slots"] = new JArray(manualSlots.Select(s => new JObject { ["actorId"] = s.ActorId, ["index"] = s.Index, ["kind"] = s.Kind })),
                ["actors"] = new JArray(pairActors.Select(actor => JObject.FromObject(MountedPersistenceService.CaptureActor(actor), serializer))),
                ["unrelated"] = new JArray(roster.Where(actor => !pairActors.Contains(actor)).OrderBy(actor => actor.UniqueId, StringComparer.Ordinal)
                    .Select(actor => new JObject { ["actorId"] = actor.UniqueId, ["native"] = JObject.FromObject(MountedPersistenceService.CaptureActor(actor), serializer) })),
                ["rosterIds"] = new JArray(roster.Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal)),
                ["arrangement"] = Chunk7OwnedPairFixture.ArrangementOf(roster) };
        }

        // Native cooldown debt never increases, and decreases no faster than native clock progress.
        internal static bool ManualDebtWithinClock(SavedNativeActor before, SavedNativeActor after, double elapsed)
        {
            if (before == null || after == null || before.Id != after.Id || before.ReactionsRemaining != after.ReactionsRemaining ||
                before.LastSurpriseTicks != after.LastSurpriseTicks) return false;
            var a = new[] { before.Standard, before.Move, before.Swift, before.Initiative, before.Reaction };
            var b = new[] { after.Standard, after.Move, after.Swift, after.Initiative, after.Reaction };
            var slack = 0.001 + elapsed * 0.1;
            return Enumerable.Range(0, a.Length).All(i => b[i] <= a[i] + 0.001 && b[i] + elapsed + slack >= a[i]);
        }

        private static string ManualHash(string path)
        {
            using (var hash = SHA256.Create()) using (var stream = File.OpenRead(path))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
