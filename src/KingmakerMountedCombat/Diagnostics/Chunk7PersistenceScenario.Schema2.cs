using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string Schema2OutputName = "KMC_C7_SCHEMA2";
        internal const string Schema2OutputLeaf = "Manual_301_KMC_C7_SCHEMA2.zks";
        private bool Schema2ResaveCase => Cold && LegacyPendingCase;
        private MountedArchiveData schema2Imported;
        private SaveInfo schema2Input;
        private string schema2InputHash;
        private int schema2ResaveNativeRound;
        private object schema2ResaveNativeTurn;
        private bool schema2ResaveNativeBoundaryCaptured;

        private void RequestSchema2Resave()
        {
            var schema2Case = Schema2ResaveCase;
            var currentWriter = NativeMountedSaveFormat.SelectedWriterSchema == 3 && !legacyWriterHeld;
            var restoredSource = saved != null && (int?)evidence["loadedSourceSchema"] == 2;
            var restorationSettled = !persistence.CombatRestorationPending;
            var nativeTurnBased = CombatController.IsInTurnBasedCombat();
            var defaultMode = Game.Instance.CurrentMode == Kingmaker.GameModes.GameModeType.Default;
            var unpaused = !Game.Instance.IsPaused;
            var turnController = Game.Instance.TurnBasedCombatController;
            var nativeRound = turnController.RoundNumber;
            var nativeTurn = turnController.CurrentTurn;
            var savedCurrentActor = saved?.Combat?.Current?.ActorId;
            var nativeCurrentActor = nativeTurn?.Unit?.UniqueId;
            var roundMatches = saved?.Combat != null && nativeRound == saved.Combat.Round;
            var currentMatches = savedCurrentActor == null ? nativeTurn == null :
                nativeTurn != null && StringComparer.Ordinal.Equals(nativeCurrentActor, savedCurrentActor);
            evidence["schema2ResaveAdmission"] = new JObject {
                ["schema2Case"] = schema2Case, ["currentWriter"] = currentWriter,
                ["restoredSource"] = restoredSource, ["restorationSettled"] = restorationSettled,
                ["nativeTurnBased"] = nativeTurnBased, ["mode"] = Game.Instance.CurrentMode.ToString(),
                ["defaultMode"] = defaultMode, ["unpaused"] = unpaused,
                ["savedRound"] = saved?.Combat?.Round, ["nativeRound"] = nativeRound,
                ["savedCurrentActor"] = savedCurrentActor, ["nativeCurrentActor"] = nativeCurrentActor,
                ["roundMatches"] = roundMatches, ["currentMatches"] = currentMatches };
            Require(schema2Case && currentWriter && restoredSource && restorationSettled &&
                nativeTurnBased && defaultMode && unpaused && roundMatches && currentMatches,
                "Schema-2 resave requires the observed cold pending-only world before continuation.");
            AssertCombatOwners(); AssertSlots(); AssertCombatDebt(saved); AssertSchema2NoPreparation();
            schema2Imported = saved;
            ValidateSchema2Continuity(schema2Imported, schema2Imported);
            schema2Input = Game.Instance.SaveManager.Single(s => s.FileName == request.PersistenceLoad.FileName);
            schema2InputHash = Hash(schema2Input.FolderName);
            Require(schema2Input.Type == SaveInfo.SaveType.Manual && schema2Input.Name == request.PersistenceLoad.InternalName &&
                schema2Input.FileName == (request.OwnedPairs == null ? NativeLeaf(SaveInfo.SaveType.Manual) : request.PersistenceLoad.FileName) &&
                schema2Input.OperationState == SaveInfo.StateType.None && schema2Input.HasFileOnDisk,
                "Schema-2 resave lost its exact read-only native input descriptor.");
            VerifySchema2InputImmutable();
            if (request.OwnedPairs?.IsOwnerSeedCheckpointLoad == true) ReadOwnedSchema2Derivative();
            snapshotBefore = persistence.SnapshotCount; castBefore = controls.NativeCastRequestCount;
            var manager = Game.Instance.SaveManager;
            if (request.OwnedPairs != null) RequireOwnedSaveAdmission(SaveInfo.SaveType.Manual);
            Require(manager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual) == (request.OwnedPairs == null ? 301 : replaySaveAuthority.OwnedPairsOutputNumber) && manager.IsSaveAllowed(),
                "Schema-2 resave requires the exact next fresh native Manual slot.");
            var descriptor = manager.CreateNewSave(Schema2OutputName);
            Require(descriptor != null && descriptor.Type == SaveInfo.SaveType.Manual && descriptor.Name == Schema2OutputName &&
                !descriptor.IsActuallySaved, "Schema-2 resave substituted its native output descriptor.");
            if (request.OwnedPairs != null) ownedRequestedSave = descriptor;
            evidence["schema2Migration"] = new JObject { ["inputSchema"] = 2, ["inputSha256"] = schema2InputHash,
                ["inputLength"] = new FileInfo(schema2Input.FolderName).Length,
                ["migratedArchive"] = JObject.FromObject(schema2Imported, MountedSaveCodec.CreateSerializer()) };
            if (ownedSchema2DerivativeEvidence != null)
                foreach (var property in ownedSchema2DerivativeEvidence.Properties()) ((JObject)evidence["schema2Migration"])[property.Name] = property.Value.DeepClone();
            evidence["saveRequested"] = new JObject { ["nativeType"] = "Manual", ["nativeName"] = descriptor.Name,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["snapshotCount"] = snapshotBefore,
                ["nativeSaveAllowed"] = true, ["newSlot"] = true };
            schema2ResaveNativeRound = nativeRound;
            schema2ResaveNativeTurn = nativeTurn;
            schema2ResaveNativeBoundaryCaptured = true;
            callback = false;
            Game.Instance.SaveGame(descriptor, () => { if (request.OwnedPairs != null) ownedSaveCallbacks++; callback = true; });
        }

        private void ObserveSchema2Resaved()
        {
            Require(Schema2ResaveCase && schema2Imported != null && CombatController.IsInTurnBasedCombat() &&
                Game.Instance.CurrentMode == Kingmaker.GameModes.GameModeType.Default && !Game.Instance.IsPaused,
                "Schema-2 resave left its admitted turn-based cold world.");
            Require(schema2ResaveNativeBoundaryCaptured &&
                Game.Instance.TurnBasedCombatController.RoundNumber == schema2ResaveNativeRound &&
                ReferenceEquals(Game.Instance.TurnBasedCombatController.CurrentTurn, schema2ResaveNativeTurn),
                "Schema-2 resave crossed the retained native turn or round before continuation.");
            savedDescriptor = Game.Instance.SaveManager.Single(s => s.Type == SaveInfo.SaveType.Manual && s.Name == Schema2OutputName);
            Require(savedDescriptor.FileName == (request.OwnedPairs == null ? Schema2OutputLeaf : replaySaveAuthority.OwnedPairsOutputLeaf) && savedDescriptor.OperationState == SaveInfo.StateType.None &&
                savedDescriptor.HasFileOnDisk, "Schema-2 resave did not settle its separately authorized output leaf.");
            ObserveOwnedSaveCompletion();
            var read = NativeMountedSaveStorage.ReadArchive(savedDescriptor.Saver);
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 3 && read.Data != null &&
                NativeMountedSaveFormat.SelectedWriterSchema == 3 && !legacyWriterHeld,
                "Schema-2 migration did not deliberately write native schema 3.");
            ValidateSchema2Continuity(schema2Imported, read.Data);
            AssertCombatOwners(); AssertSlots(); AssertCombatDebt(schema2Imported); AssertCombatDebt(read.Data);
            checkpoint.ValidateArchive(read.Data); AssertSchema2NoPreparation();
            Require(callback && persistence.SnapshotCount == snapshotBefore + 1 && snapshotBefore == 0 &&
                controls.NativeCastRequestCount == castBefore && castBefore == 0 && !controls.SerializationSuspended &&
                !persistence.SaveSuspended && !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence &&
                !NativePersistenceIsolation.HasPendingWrites,
                "Schema-2 resave repeated input, omitted native completion or retained writer custody.");
            VerifySchema2InputImmutable();
            var file = new FileInfo(savedDescriptor.FolderName);
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["sha256"] = Hash(file.FullName), ["length"] = file.Length,
                ["nativeType"] = "Manual", ["nativeName"] = savedDescriptor.Name, ["nativeCallback"] = callback,
                ["sourceSchema"] = read.SourceSchema, ["operation"] = savedDescriptor.OperationState.ToString(),
                ["snapshotCount"] = persistence.SnapshotCount, ["nativeMetadata"] = JObject.Parse(read.OriginalJson),
                ["archive"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) };
            var migration = (JObject)evidence["schema2Migration"];
            migration["inputUnchanged"] = true;
            migration["resavedWorld"] = Chunk7ArchiveNativeSnapshot.Capture(world, read.Data);
            evidence["coldRestoration"] = restoration.Capture(); restoration.Dispose(); restoration = null;
            snapshots.Add(Capture("schema2-native-resave-before-continuation"));
            Pass("C7-persistence-schema2-native-resave");
            // Keep the original migrated archive as continuation's debt/round
            // baseline. The new archive has been checked against it above.
        }

        private void AssertSchema2NoPreparation()
        {
            var ids = saved.Combat.Actors.Select(a => a.Native.Id).ToArray();
            Require(restoration != null && restoration.Complete && restoration.Count("prepare-before", ids) == 0 &&
                restoration.Count("round-before", ids) == 0 &&
                restoration.Count("restore-actor-before", ids) == ids.Length &&
                restoration.Count("restore-actor-after", ids) == ids.Length &&
                restoration.Count("restore-debt-before", ids) == ids.Length &&
                restoration.Count("restore-debt-after", ids) == ids.Length,
                "Schema-2 cold restore/resave invoked preparation, advanced a round or repeated actor restoration.");
        }

        private void VerifySchema2InputImmutable()
        {
            if (schema2Input == null) return;
            Require(schema2InputHash == request.PersistenceLoad.Sha256 && Hash(schema2Input.FolderName) == schema2InputHash &&
                new FileInfo(schema2Input.FolderName).Length == request.PersistenceLoad.Length,
                "Read-only schema-2 input changed during native migration/resave.");
            VerifyOwnedSchema2DerivativeImmutable();
        }

        private static void NormalizeSchema2Collections(JObject graph)
        {
            foreach (var spec in new[] { "Pairs:PairId", "Participations:OwnerId", "Slots:ActorId,Index,Kind",
                "Combat.Actors:Native.Id", "Combat.Roster:ActorId", "Combat.Allocations:ActorId", "Combat.Engagements:From,To" })
            {
                var parts = spec.Split(':'); var rows = (JArray)graph.SelectToken(parts[0]);
                var keys = parts[1].Split(',');
                var sorted = rows.OrderBy(row => string.Join("|", keys.Select(key => (string)row.SelectToken(key))), StringComparer.Ordinal).ToArray();
                rows.RemoveAll(); foreach (var row in sorted) rows.Add(row);
            }
        }

        internal static void ValidateSchema2Continuity(MountedArchiveData before, MountedArchiveData after)
        {
            Require(before != null && after != null, "Schema-2 resave omitted a complete graph.");
            before.Validate(); after.Validate();
            Require(before.Pairs.Length == 1 && before.Participations.Length == 1 && before.Combat?.TurnBased == true,
                "Schema-2 resave requires exactly one mounted pending-only obligation.");
            var pair = before.Pairs.Single(); var state = before.Participations.Single().State;
            Require(state.RiderId == null && state.MountId == null && state.Activation == null && state.Boundary == null &&
                state.Partner == null && !state.BoundaryIsCurrent && state.PendingSplitId == pair.Mount.Id,
                "Schema-2 resave source invented an activation or lost the pending mount.");
            var elapsed = (after.GameTimeTicks - before.GameTimeTicks) / (double)TimeSpan.TicksPerSecond;
            Require(elapsed >= 0 && elapsed <= 30, "Schema-2 resave escaped its bounded native clock.");
            var original = JObject.FromObject(before, MountedSaveCodec.CreateSerializer());
            var output = JObject.FromObject(after, MountedSaveCodec.CreateSerializer());
            output["GameTimeTicks"] = original["GameTimeTicks"].DeepClone();
            var oldDebts = before.Combat.Actors.ToDictionary(a => a.Native.Id, a => a.Native, StringComparer.Ordinal);
            foreach (var debt in after.Combat?.Actors.Select(a => a.Native) ?? Enumerable.Empty<SavedNativeActor>())
            {
                Require(oldDebts.TryGetValue(debt.Id, out var old), "Schema-2 resave substituted an actor.");
                var a = new[] { old.Standard, old.Move, old.Swift, old.Initiative, old.Reaction };
                var b = new[] { debt.Standard, debt.Move, debt.Swift, debt.Initiative, debt.Reaction };
                Require(Enumerable.Range(0, a.Length).All(i => b[i] <= a[i] + 0.001 && b[i] + elapsed * 1.1 + 0.001 >= a[i]),
                    "Schema-2 resave changed spent native debt beyond elapsed native time.");
            }
            // Compare the entire graph after accounting only for the same
            // elapsed cooldown allowance used by live debt observation. Native
            // grants, contexts, order, split rounds, owners and every other
            // resource/actor field must remain identical.
            foreach (var path in new[] { "Pairs[*].Rider", "Pairs[*].Mount", "Combat.Actors[*].Native" })
                foreach (var debt in output.SelectTokens(path).Cast<JObject>())
                {
                    Require(oldDebts.TryGetValue((string)debt["Id"], out var old), "Schema-2 resave substituted a debt identity.");
                    var native = JObject.FromObject(old, MountedSaveCodec.CreateSerializer());
                    foreach (var field in new[] { "Standard", "Move", "Swift", "Initiative", "Reaction" }) debt[field] = native[field].DeepClone();
                }
            NormalizeSchema2Collections(original); NormalizeSchema2Collections(output);
            Require(JToken.DeepEquals(original, output),
                "Schema-2 resave changed ownership, pending release, native allocation or another protected graph field.");
        }
    }
}
