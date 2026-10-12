using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string Schema1Case = "legacy-schema1-resave-manual";
        internal const string Schema1OutputName = "KMC_C7_SCHEMA1";
        internal const string Schema1OutputLeaf = "Manual_813_KMC_C7_SCHEMA1.zks";
        private bool Schema1ResaveCase => request.PersistenceCase == Schema1Case;
        private MountedArchiveData schema1Imported;
        private SaveInfo schema1Input;
        private string schema1InputHash;

        private void ObserveSchema1Cold()
        {
            Require(Cold && Schema1ResaveCase && request.PersistenceLoad.FileName == "Manual_812_KMC_P06.zks" &&
                request.PersistenceLoad.InternalName == "KMC_P01" && !Game.Instance.Player.IsInCombat &&
                !persistence.CombatRestorationPending, "Schema-1 proof requires its exact guarded outside-combat derivative.");
            schema1Input = Game.Instance.SaveManager.Single(s => s.FileName == request.PersistenceLoad.FileName);
            Require(schema1Input.Type == SaveInfo.SaveType.Manual && schema1Input.Name == request.PersistenceLoad.InternalName &&
                schema1Input.OperationState == SaveInfo.StateType.None && schema1Input.HasFileOnDisk,
                "Schema-1 derivative changed its native descriptor.");
            schema1InputHash = Hash(schema1Input.FolderName);
            Require(schema1InputHash == request.PersistenceLoad.Sha256, "Schema-1 input bytes changed before observation.");
            // This is the already-qualified native SaveInfo/Saver selected by the
            // guarded loader. No raw path/member access or fixture rewriting occurs.
            var read = NativeMountedSaveStorage.ReadArchive(schema1Input.Saver);
            schema1Imported = read.Data;
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 1 && read.HistoricalData?.Mounted == true &&
                schema1Imported?.SchemaVersion == 3 && schema1Imported.Pairs.Length == 1 && schema1Imported.Combat == null &&
                schema1Imported.Participations.Length == 0 && read.HistoricalData.Combat == null,
                "The native input is not one valid mounted schema-1 record with no invented combat participation.");
            saved = persistence.LoadedArchive;
            Require(saved != null && JToken.DeepEquals(JObject.FromObject(saved, MountedSaveCodec.CreateSerializer()),
                JObject.FromObject(schema1Imported, MountedSaveCodec.CreateSerializer())) &&
                persistence.ArchiveRestorePhase == "all-pair-presentation-restored",
                "Native schema-1 restoration did not bind the complete deterministic migrated graph.");
            var pair = saved.Pairs.Single();
            Require(pair.ProfileId == SupportedMountedProfiles.Mammoth.Id && pair.Generation == 1 &&
                pair.Rider.Id == Chunk7WorldScenarioEngine.MammothRiderId && pair.Mount.Id == Chunk7WorldScenarioEngine.MammothId &&
                saved.CampaignId == request.Fixture.Working.GameId && saved.AreaId == request.Fixture.Working.Area,
                "Schema-1 input differs from the qualified one-pair Mammoth fixture.");
            firstRider = Exact(pair.Rider.Id); firstMount = Exact(pair.Mount.Id); actors = new[] { firstRider, firstMount };
            TrackNativeActors();
            expectedOwners.Add(Guid.Parse(pair.PairId), pair.Generation); slots = saved.Slots;
            AssertSchema1Owners(); AssertSlots(); AssertDebt(saved);
            Require(persistence.SemanticRestoreCount == 2 && persistence.PresentationRestoreCount == 1 &&
                persistence.ArchivePostLoadObservedCount == 2 && persistence.SnapshotCount == 0 && controls.NativeCastRequestCount == 0,
                "Schema-1 native restoration repeated an actor/attachment, preparation, input or snapshot.");
            AssertSchema1Observer();
            evidence["schema1Migration"] = new JObject {
                ["inputSchema"] = read.SourceSchema, ["derivationReceiptSha256"] = request.Schema1DerivationSha256,
                ["nativeInputMetadata"] = JObject.Parse(read.OriginalJson),
                ["migratedArchive"] = JObject.FromObject(schema1Imported, MountedSaveCodec.CreateSerializer()),
                ["restoredWorld"] = Chunk7ArchiveNativeSnapshot.Capture(world, schema1Imported),
                ["inputSha256"] = schema1InputHash, ["inputLength"] = new FileInfo(schema1Input.FolderName).Length };
            evidence["loadedArchive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer());
            var descriptor = request.PersistenceLoad;
            evidence["loadedDescriptor"] = new JObject { ["internalName"] = descriptor.InternalName, ["fileName"] = descriptor.FileName,
                ["sha256"] = descriptor.Sha256, ["length"] = descriptor.Length, ["lastWriteTimeUtcTicks"] = descriptor.LastWriteTimeUtcTicks,
                ["gameId"] = descriptor.GameId, ["gameName"] = descriptor.GameName, ["area"] = descriptor.Area };
            snapshots.Add(Capture("schema1-native-restored-before-resave"));
            snapshotBefore = persistence.SnapshotCount;
            Pass("C7-persistence-schema1-native-restoration");
        }
        private void AssertSchema1Owners()
        {
            Require(schema1Imported != null && Mounted(firstRider, firstMount) && world.Registry.ActiveCount == 1 &&
                world.Registry.Owners.Length == 1 && world.Registry.Owners.All(o =>
                    expectedOwners.TryGetValue(o.Id, out var generation) && generation == o.Generation &&
                    ReferenceEquals(o.Rider, firstRider) && ReferenceEquals(o.Mount, firstMount)),
                "Schema-1 migration replaced or duplicated its exact one-pair owner.");
            var participant = world.Find(world.Registry.FindActive(firstRider).Boundary).Participant.CaptureParticipation();
            Require(participant == null || participant.RiderId == firstRider.UniqueId && participant.MountId == firstMount.UniqueId &&
                participant.Activation == null && participant.Boundary == null && participant.Partner == null &&
                !participant.BoundaryIsCurrent && participant.PendingSplitId == null,
                "Schema-1 exploration restoration invented native activation, context or pending participation.");
        }
        private void AssertSchema1Observer()
        {
            var ids = actors.Select(a => a.UniqueId).ToArray();
            Require(restoration != null && restoration.Complete && restoration.Count("restore-actor-before", ids) == 0 &&
                restoration.Count("restore-actor-after", ids) == 0 && restoration.Count("restore-debt-before", ids) == 2 &&
                restoration.Count("restore-debt-after", ids) == 2,
                "Schema-1 native actor restoration did not enter and complete exactly once per actor.");
            var proof = restoration.Capture(); var events = (JArray)proof["events"];
            Require(!events.Any(e => (string)e["kind"] == "prepare-before" || (string)e["kind"] == "round-before") &&
                events.Count(e => (string)e["kind"] == "all-owner-presentation-restored") == 1 &&
                !events.Any(e => (string)e["kind"] == "shared-world-restored"),
                "Schema-1 native restore/resave invoked Prepare/round or invented a combat restoration boundary.");
        }
        private void RequestSchema1Resave()
        {
            AssertSchema1Owners(); AssertSlots(); AssertSchema1Observer();
            var manager = Game.Instance.SaveManager;
            var descriptor = manager.CreateNewSave(Schema1OutputName);
            Require(descriptor != null && descriptor.Type == SaveInfo.SaveType.Manual && descriptor.Name == Schema1OutputName &&
                !descriptor.IsActuallySaved && manager.IsSaveAllowed(), "Schema-1 resave lacks its exact new native Manual slot.");
            castBefore = controls.NativeCastRequestCount; callback = false;
            evidence["saveRequested"] = new JObject { ["nativeType"] = "Manual", ["nativeName"] = descriptor.Name,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["snapshotCount"] = persistence.SnapshotCount,
                ["nativeSaveAllowed"] = true, ["newSlot"] = true };
            Game.Instance.SaveGame(descriptor, () => callback = true);
        }
        private void ObserveSchema1Resaved()
        {
            savedDescriptor = Game.Instance.SaveManager.Single(s => s.Type == SaveInfo.SaveType.Manual && s.Name == Schema1OutputName);
            Require(savedDescriptor.FileName == Schema1OutputLeaf && savedDescriptor.OperationState == SaveInfo.StateType.None &&
                savedDescriptor.HasFileOnDisk, "Schema-1 native resave did not settle its separately authorized output leaf.");
            var read = NativeMountedSaveStorage.ReadArchive(savedDescriptor.Saver); saved = read.Data;
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 3 && saved?.Pairs.Length == 1 &&
                saved.Participations.Length == 0 && saved.Combat == null && saved.CampaignId == schema1Imported.CampaignId &&
                saved.AreaId == schema1Imported.AreaId && saved.Policy == schema1Imported.Policy && saved.RulesId == schema1Imported.RulesId,
                "Native resave did not deliberately rewrite the one-pair schema-1 graph as schema 3.");
            var before = schema1Imported.Pairs.Single(); var after = saved.Pairs.Single();
            Require(after.PairId == before.PairId && after.Generation == before.Generation && after.ProfileId == before.ProfileId &&
                after.Rider.Id == before.Rider.Id && after.Mount.Id == before.Mount.Id &&
                JToken.DeepEquals(JArray.FromObject(saved.Slots, MountedSaveCodec.CreateSerializer()),
                    JArray.FromObject(schema1Imported.Slots, MountedSaveCodec.CreateSerializer())),
                "Schema-1 native resave lost stable custody, actor role or exact owned hotbar bindings.");
            AssertSchema1Owners(); AssertSlots(); AssertDebt(schema1Imported); AssertDebt(saved); AssertSchema1Observer();
            Require(callback && persistence.SnapshotCount == snapshotBefore + 1 && controls.NativeCastRequestCount == castBefore &&
                !controls.SerializationSuspended && !persistence.SaveSuspended && !persistence.HasActiveSaveScope &&
                !persistence.HasSaveAdmissionFence, "Schema-1 native resave repeated input or retained a writer/fence.");
            VerifySchema1InputImmutable();
            var file = new FileInfo(savedDescriptor.FolderName);
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["sha256"] = Hash(file.FullName), ["length"] = file.Length,
                ["nativeType"] = "Manual", ["nativeName"] = savedDescriptor.Name, ["nativeCallback"] = callback, ["sourceSchema"] = read.SourceSchema,
                ["operation"] = savedDescriptor.OperationState.ToString(), ["snapshotCount"] = persistence.SnapshotCount,
                ["archive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer()) };
            ((JObject)evidence["schema1Migration"])["resavedWorld"] = Chunk7ArchiveNativeSnapshot.Capture(world, saved);
            evidence["coldRestoration"] = restoration.Capture(); restoration.Dispose(); restoration = null;
            snapshots.Add(Capture("schema1-native-resave-complete"));
            Pass("C7-persistence-schema1-native-resave");
        }
        private void VerifySchema1InputImmutable()
        {
            if (schema1Input == null) return;
            Require(Hash(schema1Input.FolderName) == schema1InputHash && schema1InputHash == request.PersistenceLoad.Sha256 &&
                new FileInfo(schema1Input.FolderName).Length == request.PersistenceLoad.Length,
                "Read-only schema-1 derivative changed during native migration/resave.");
        }
    }
}
