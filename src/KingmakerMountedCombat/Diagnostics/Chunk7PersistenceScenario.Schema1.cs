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
        internal const string OwnedSchema1Source = "owned-schema1-manual";
        internal const string OwnedSchema1Cold = "owned-schema1-resave-manual";
        private bool OwnedSchema1SourceCase => request.OwnedPairs != null && !Cold && request.PersistenceCase == OwnedSchema1Source;
        private bool OwnedSchema1ColdCase => request.OwnedPairs != null && Cold && request.PersistenceCase == OwnedSchema1Cold;
        private bool Schema1ResaveCase => request.PersistenceCase == Schema1Case || OwnedSchema1ColdCase;
        private MountedArchiveData schema1Imported;
        private SaveInfo schema1Input;
        private string schema1InputHash;

        private void ObserveSchema1Cold()
        {
            Require(Cold && Schema1ResaveCase && (OwnedSchema1ColdCase || request.PersistenceLoad.FileName == "Manual_812_KMC_P06.zks" &&
                request.PersistenceLoad.InternalName == "KMC_P01") && !Game.Instance.Player.IsInCombat &&
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
            if (OwnedSchema1ColdCase)
            {
                BindOwnedFixture(false);
                ValidateOwnedSchema1Archive(saved, request.PersistenceLoad.GameId, request.PersistenceLoad.Area, firstRider.UniqueId, firstMount.UniqueId, true);
                ReadOwnedSchema1Checkpoint();
            }
            else
            {
            Require(pair.ProfileId == SupportedMountedProfiles.Mammoth.Id && pair.Generation == 1 &&
                pair.Rider.Id == Chunk7WorldScenarioEngine.MammothRiderId && pair.Mount.Id == Chunk7WorldScenarioEngine.MammothId &&
                saved.CampaignId == request.Fixture.Working.GameId && saved.AreaId == request.Fixture.Working.Area,
                "Schema-1 input differs from the qualified one-pair Mammoth fixture.");
            firstRider = Exact(pair.Rider.Id); firstMount = Exact(pair.Mount.Id); actors = new[] { firstRider, firstMount };
            TrackNativeActors();
            }
            expectedOwners.Add(Guid.Parse(pair.PairId), pair.Generation); slots = saved.Slots;
            AssertSchema1Owners(); AssertSlots(); AssertDebt(saved);
            if (OwnedSchema1ColdCase) CompleteOwnedSchema1Field("cold-restored");
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
            if (OwnedSchema1ColdCase) { RequireOwnedSaveAdmission(SaveInfo.SaveType.Manual); CompleteOwnedSchema1Field("before-resave"); }
            var descriptor = manager.CreateNewSave(Schema1OutputName);
            Require(descriptor != null && descriptor.Type == SaveInfo.SaveType.Manual && descriptor.Name == Schema1OutputName &&
                !descriptor.IsActuallySaved && manager.IsSaveAllowed(), "Schema-1 resave lacks its exact new native Manual slot.");
            if (OwnedSchema1ColdCase) ownedRequestedSave = descriptor;
            castBefore = controls.NativeCastRequestCount; callback = false;
            evidence["saveRequested"] = new JObject { ["nativeType"] = "Manual", ["nativeName"] = descriptor.Name,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["snapshotCount"] = persistence.SnapshotCount,
                ["nativeSaveAllowed"] = true, ["newSlot"] = true };
            Game.Instance.SaveGame(descriptor, () => { if (OwnedSchema1ColdCase) ownedSaveCallbacks++; callback = true; });
        }
        private void ObserveSchema1Resaved()
        {
            savedDescriptor = Game.Instance.SaveManager.Single(s => s.Type == SaveInfo.SaveType.Manual && s.Name == Schema1OutputName);
            Require(savedDescriptor.FileName == (OwnedSchema1ColdCase ? replaySaveAuthority.OwnedPairsOutputLeaf : Schema1OutputLeaf) && savedDescriptor.OperationState == SaveInfo.StateType.None &&
                savedDescriptor.HasFileOnDisk, "Schema-1 native resave did not settle its separately authorized output leaf.");
            if (OwnedSchema1ColdCase) ObserveOwnedSaveCompletion();
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
            if (OwnedSchema1ColdCase) { ValidateOwnedSchema1Archive(saved, request.PersistenceLoad.GameId, request.PersistenceLoad.Area, firstRider.UniqueId, firstMount.UniqueId, true); CompleteOwnedSchema1Field("resave-settled"); }
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
            if (OwnedSchema1ColdCase)
                Require(ownedSchema1CheckpointPath != null && ownedSchema1ReceiptPath != null &&
                    new FileInfo(ownedSchema1CheckpointPath).Length == ownedSchema1CheckpointLength &&
                    new FileInfo(ownedSchema1ReceiptPath).Length == ownedSchema1ReceiptLength &&
                    Hash(ownedSchema1CheckpointPath) == request.OwnedPairs.CheckpointSourceArtifactSha256 &&
                    Hash(ownedSchema1ReceiptPath) == request.Schema1DerivationSha256,
                    "Owned schema-1 checkpoint or derivation changed while native custody remained active.");
        }

        private JObject ownedSchema1FieldBaseline;
        private JArray ownedSchema1Fields;
        private object[][] ownedSchema1OtherSlots;
        private string ownedSchema1CheckpointPath, ownedSchema1ReceiptPath;
        private long ownedSchema1CheckpointLength, ownedSchema1ReceiptLength;

        private void AssertOwnedSchema1Owner()
        {
            inherited.RequirePreserved();
            Require(!Game.Instance.Player.IsInCombat && !world.UnifiedTurn.HasUnsettledPreparation &&
                !controls.HasUnsettledRelationshipTransition && world.Registry.ActiveCount == 1 && world.Registry.Owners.Length == 1 &&
                Mounted(firstRider, firstMount) && world.Registry.FindOwner(secondRider) == null && world.Registry.FindOwner(secondMount) == null &&
                world.Registry.Owners.All(o => expectedOwners.TryGetValue(o.Id, out var generation) && generation == o.Generation),
                "Owned schema-1 fixture must retain exactly its first settled mounted owner and no second or pending owner.");
        }
        private JObject CaptureOwnedSchema1Field(string phase)
        {
            inherited.RequirePreserved();
            var party = inherited.Party.OrderBy(a => a.UniqueId, StringComparer.Ordinal).ToArray();
            Require(party.All(a => a.IsInState && a.IsInGame && !a.IsInCombat && a.Commands != null && a.Commands.Empty &&
                a.Commands.Queue != null && a.Commands.Queue.Count == 0 && a.Commands.Raw.All(c => c == null || c.IsFinished &&
                (!(c is Kingmaker.UnitLogic.Commands.UnitUseAbility ability) || ability.ExecutionProcess == null || ability.ExecutionProcess.IsEnded))),
                "Owned schema-1 fixture requires the complete native field command lifetime settled without draining it.");
            var value = new JObject { ["phase"] = phase, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["actors"] = new JArray(party.Select(a => new JObject { ["native"] = JObject.FromObject(
                    MountedPersistenceService.CaptureActor(a), MountedSaveCodec.CreateSerializer()),
                    ["prepared"] = a.CombatState.Prepared, ["inCombat"] = a.IsInCombat,
                    ["commandsEmpty"] = a.Commands.Empty, ["queuedCommands"] = a.Commands.Queue.Count })) };
            ValidateOwnedSchema1Field(value, request.OwnedPairs.PartyIds);
            return value;
        }
        internal static void ValidateOwnedSchema1Field(JObject value, string[] actorIds)
        {
            Require(value != null && value["gameTicks"]?.Type == JTokenType.Integer && (long)value["gameTicks"] >= 0 &&
                actorIds != null && actorIds.Length >= 4 && actorIds.Length <= 12 && actorIds.Distinct(StringComparer.Ordinal).Count() == actorIds.Length,
                "Owned schema-1 full-field identity or native clock is invalid.");
            var rows = value["actors"] as JArray;
            Require(rows != null && rows.Count == actorIds.Length && rows.All(row => row is JObject && row["native"] is JObject) &&
                actorIds.All(id => rows.Count(row => (string)((JObject)row["native"])["Id"] == id) == 1),
                "Owned schema-1 field omitted, duplicated or substituted a native actor.");
            foreach (var row in rows)
            {
                Require(row["native"] is JObject && row["prepared"]?.Type == JTokenType.Boolean &&
                    row["inCombat"]?.Type == JTokenType.Boolean && !(bool)row["inCombat"] &&
                    row["commandsEmpty"]?.Type == JTokenType.Boolean && (bool)row["commandsEmpty"] &&
                    row["queuedCommands"]?.Type == JTokenType.Integer && (int)row["queuedCommands"] == 0,
                    "Owned schema-1 field has native combat or unsettled commands.");
                var native = row["native"].ToObject<SavedNativeActor>(MountedSaveCodec.CreateSerializer()); native.Validate();
                Require(native.Standard == 0 && native.Move == 0 && native.Swift == 0 && native.Initiative == 0 && native.Reaction == 0,
                    "Owned schema-1 fixture cannot discard any native action debt.");
            }
        }
        internal static void ValidateOwnedSchema1Archive(MountedArchiveData archive, string campaign, string area,
            string rider, string mount, bool migrated)
        {
            Require(archive != null, "Owned schema-1 source archive is absent."); archive.Validate();
            Require(archive.SchemaVersion == 3 && archive.CampaignId == campaign && archive.AreaId == area &&
                archive.Pairs.Length == 1 && archive.Participations.Length == 0 && archive.Combat == null && archive.Slots.Length <= 128,
                "Owned schema-1 projection cannot discard a second owner, combat, participation or slots.");
            var pair = archive.Pairs.Single();
            Require(pair.ProfileId == SupportedMountedProfiles.Horse.Id && pair.Rider.Id == rider && pair.Mount.Id == mount &&
                (!migrated || pair.Generation == 1) && archive.Slots.All(s => s.Kind >= 1 && s.Kind <= 4),
                "Owned schema-1 fixture differs from its exact Medium Horse owner or contains unrepresentable controls.");
            foreach (var actor in new[] { pair.Rider, pair.Mount })
                Require(actor.Standard == 0 && actor.Move == 0 && actor.Swift == 0 && actor.Initiative == 0 && actor.Reaction == 0,
                    "Owned schema-1 native archive retains action debt.");
        }
        internal static void ValidateOwnedSchema1PreservedField(JObject before, JObject after, string[] actorIds)
        {
            ValidateOwnedSchema1Field(before, actorIds); ValidateOwnedSchema1Field(after, actorIds);
            Require(JToken.DeepEquals(after["actors"], before["actors"]) && (long)after["gameTicks"] >= (long)before["gameTicks"],
                "Owned schema-1 save/load changed the complete field's native allocations or prepared state.");
        }
        private void PinOwnedSchema1OtherSlots()
        {
            ownedSchema1OtherSlots = inherited.Party.Where(a => !ReferenceEquals(a, firstRider)).Select(a => a.UISettings.Slots.Cast<object>().ToArray()).ToArray();
        }
        private void CompleteOwnedSchema1Field(string phase)
        {
            AssertOwnedSchema1Owner();
            var current = CaptureOwnedSchema1Field(phase);
            ValidateOwnedSchema1PreservedField(ownedSchema1FieldBaseline, current, request.OwnedPairs.PartyIds);
            var other = inherited.Party.Where(a => !ReferenceEquals(a, firstRider)).ToArray();
            Require(ownedSchema1OtherSlots != null && ownedSchema1OtherSlots.Length == other.Length &&
                other.Select((a, i) => a.UISettings.Slots.Cast<object>().SequenceEqual(ownedSchema1OtherSlots[i])).All(v => v),
                "Owned schema-1 operation changed unrelated native hotbar slots.");
            Require(ownedSchema1Fields.Count < 4, "Owned schema-1 field observation bound exceeded.");
            ownedSchema1Fields.Add(current);
        }
        private void BeginOwnedSchema1Source()
        {
            Require(OwnedSchema1SourceCase && inherited != null && NativeMountedSaveFormat.SelectedWriterSchema == 3,
                "Owned schema-1 fixture source requires its closed normal schema-3 native writer.");
            RememberOwners(); AssertOwnedSchema1Owner(); PinOwnedSchema1OtherSlots(); BindSlots();
            ownedSchema1FieldBaseline = CaptureOwnedSchema1Field("source-before-save");
            ownedSchema1Fields = new JArray(ownedSchema1FieldBaseline.DeepClone());
            evidence["ownedSchema1"] = new JObject { ["contract"] = "owned-schema1-fixture-v1", ["syntheticMetadata"] = false,
                ["sourceArchiveSchema"] = 3, ["fields"] = ownedSchema1Fields };
            Game.Instance.IsPaused = true;
            snapshots.Add(Capture("before-native-save")); RequestSave(); Next(4);
        }
        private void ReadOwnedSchema1Checkpoint()
        {
            var root = Path.GetDirectoryName(replaySaveAuthority.Root);
            var path = Path.Combine(root, "owned-schema1-checkpoint.json");
            var file = new FileInfo(path);
            Require(file.Exists && file.Length > 0 && file.Length <= 16 * 1024 * 1024 &&
                (file.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(path) == request.OwnedPairs.CheckpointSourceArtifactSha256,
                "Owned schema-1 original native checkpoint receipt is absent or changed.");
            var checkpoint = JObject.Parse(File.ReadAllText(path)); var source = checkpoint["observations"] as JObject;
            Require((string)checkpoint["runId"] == request.OwnedPairs.CheckpointSourceRunId && (string)checkpoint["scenario"] == Chunk7OwnedPairsSelection.Scenario &&
                (string)checkpoint["evidenceKind"] == EvidenceKind && (string)checkpoint["status"] == "PASS" &&
                checkpoint["errors"] is JArray errors && errors.Count == 0 && (string)checkpoint["commit"] == request.Commit &&
                (string)checkpoint["dllSha256"] == request.DllSha256 && (string)checkpoint["dllMvid"] == request.DllMvid &&
                (string)source?["checkpoint"] == OwnedSchema1Source && (bool?)source?["cold"] == false &&
                (string)source?["inheritedManualSha256"] == request.OwnedPairs.SourceArchiveSha256,
                "Owned schema-1 source does not bind its same-candidate successful native source case.");
            var original = source["nativeWrite"]?["archive"]?.ToObject<MountedArchiveData>(MountedSaveCodec.CreateSerializer());
            ValidateOwnedSchema1Archive(original, request.PersistenceLoad.GameId, request.PersistenceLoad.Area, firstRider.UniqueId, firstMount.UniqueId, false);
            var fields = source["ownedSchema1"]?["fields"] as JArray;
            Require(fields != null && fields.Count == 2 && (string)fields[1]["phase"] == "source-settled" &&
                JToken.DeepEquals(fields[0]["actors"], fields[1]["actors"]), "Owned schema-1 source omitted its full native allocation preservation.");
            ownedSchema1FieldBaseline = (JObject)fields[1].DeepClone(); ValidateOwnedSchema1Field(ownedSchema1FieldBaseline, request.OwnedPairs.PartyIds);
            var receiptPath = Path.Combine(root, "owned-schema1-derivation.json"); var receiptFile = new FileInfo(receiptPath);
            Require(receiptFile.Exists && receiptFile.Length > 0 && receiptFile.Length <= 256 * 1024 &&
                (receiptFile.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(receiptPath) == request.Schema1DerivationSha256,
                "Owned schema-1 staged derivation receipt differs from its request pin.");
            ownedSchema1CheckpointPath = path; ownedSchema1ReceiptPath = receiptPath;
            ownedSchema1CheckpointLength = file.Length; ownedSchema1ReceiptLength = receiptFile.Length;
            var receipt = JObject.Parse(File.ReadAllText(receiptPath));
            Require((string)receipt["contract"] == "owned-schema1-metadata-copy-v1" && (string)receipt["sourceRunId"] == request.OwnedPairs.CheckpointSourceRunId &&
                (string)receipt["sourceArtifactSha256"] == request.OwnedPairs.CheckpointSourceArtifactSha256 &&
                (string)receipt["sourceDescriptor"]?["sha256"] == (string)source["nativeWrite"]?["sha256"] &&
                (string)receipt["descriptor"]?["sha256"] == request.PersistenceLoad.Sha256 &&
                JToken.DeepEquals(receipt["metadata"], JObject.Parse(NativeMountedSaveStorage.ReadArchive(schema1Input.Saver).OriginalJson)) &&
                Hash(receiptPath) == request.Schema1DerivationSha256 && Hash(path) == request.OwnedPairs.CheckpointSourceArtifactSha256,
                "Owned schema-1 source/metadata projection changed during native observation.");
            ownedSchema1Fields = new JArray(); PinOwnedSchema1OtherSlots();
            evidence["ownedSchema1"] = new JObject { ["contract"] = "owned-schema1-fixture-v1", ["syntheticMetadata"] = true,
                ["sourceArchiveSchema"] = 1, ["sourceCheckpointRunId"] = request.OwnedPairs.CheckpointSourceRunId,
                ["sourceCheckpointArtifactSha256"] = request.OwnedPairs.CheckpointSourceArtifactSha256,
                ["originalNativeArchiveSha256"] = source["nativeWrite"]["sha256"].DeepClone(),
                ["sourceField"] = ownedSchema1FieldBaseline.DeepClone(), ["fields"] = ownedSchema1Fields };
        }
    }
}
