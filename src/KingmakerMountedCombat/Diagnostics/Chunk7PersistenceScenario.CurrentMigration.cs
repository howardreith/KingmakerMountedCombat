using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        private bool OwnedSchema1CurrentCase => request.PersistenceCase == "owned-schema1-current-manual";
        private bool OwnedSchema2CurrentCase => request.PersistenceCase == "owned-schema2-pending-current-manual";
        private bool CurrentMigrationCase => OwnedSchema1CurrentCase || OwnedSchema2CurrentCase;
        private string currentMigrationParentPath, currentMigrationInputPath;
        private long currentMigrationParentLength;

        private void ObserveOwnedSchema1Current()
        {
            Require(Cold && OwnedSchema1CurrentCase && request.OwnedPairs?.IsOwnerSeedCheckpointLoad == true &&
                !Game.Instance.Player.IsInCombat && !persistence.CombatRestorationPending &&
                persistence.ArchiveRestorePhase == "all-pair-presentation-restored",
                "Current schema-1 continuation requires its settled native current-format load.");
            saved = persistence.LoadedArchive;
            BindOwnedFixture(false);
            ValidateOwnedSchema1Archive(saved, request.PersistenceLoad.GameId, request.PersistenceLoad.Area,
                firstRider.UniqueId, firstMount.UniqueId, true);
            var pair = saved.Pairs.Single(); expectedOwners.Add(Guid.Parse(pair.PairId), pair.Generation); slots = saved.Slots;
            AssertOwnedSchema1Owner(); AssertSlots(); AssertDebt(saved);
            CaptureOwnedSchema1Field("current-restored");
            Require(persistence.SemanticRestoreCount == 2 && persistence.PresentationRestoreCount == 1 &&
                persistence.ArchivePostLoadObservedCount == 2 && persistence.SnapshotCount == 0 && controls.NativeCastRequestCount == 0,
                "Current schema-1 continuation repeated restoration, native input or saving.");
            AssertSchema1Observer();
            evidence["loadedArchive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer());
            var input = request.PersistenceLoad;
            evidence["loadedDescriptor"] = new JObject { ["internalName"] = input.InternalName, ["fileName"] = input.FileName,
                ["sha256"] = input.Sha256, ["length"] = input.Length, ["lastWriteTimeUtcTicks"] = input.LastWriteTimeUtcTicks,
                ["gameId"] = input.GameId, ["gameName"] = input.GameName, ["area"] = input.Area };
            evidence["loadedSourceSchema"] = 3;
            ObserveCurrentMigrationParent();
            evidence["coldRestoration"] = restoration.Capture(); restoration.Dispose(); restoration = null;
            snapshots.Add(Capture("schema1-current-restored-before-continuation"));
            Pass("C7-persistence-current-migration-native-load");
        }

        // The runner requalifies the complete accepted parent receipt chain and
        // stages this exact artifact. Bind its genuine native write to the actual
        // current-format input again inside the child process before any action.
        private void ObserveCurrentMigrationParent()
        {
            Require(Cold && CurrentMigrationCase && currentMigrationParentPath == null &&
                inherited?.Bound == true && persistence.SnapshotCount == 0 && controls.NativeCastRequestCount == 0 &&
                replaySaveAuthority.MaximumNativeWriteRequests == 0 && replaySaveAuthority.AdmittedNativeWriteRequests == 0 &&
                replayAuthorization.AuthorizedWriteCount == 0, "Current-format migration child gained a write or replayed native input.");
            var path = Path.Combine(Path.GetDirectoryName(replaySaveAuthority.Root), "owner-seed-current-parent.json");
            var file = new FileInfo(path);
            Require(file.Exists && file.Length > 0 && file.Length <= 16 * 1024 * 1024 &&
                (file.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(path) == request.OwnedPairs.CheckpointSourceArtifactSha256,
                "Current-format migration parent artifact is absent or changed.");
            var parent = JObject.Parse(File.ReadAllText(path));
            var source = ValidateCurrentMigrationParent(parent, request, saved, System.Diagnostics.Process.GetCurrentProcess().Id);
            var input = Game.Instance.SaveManager.Single(s => s.FileName == request.PersistenceLoad.FileName);
            var read = NativeMountedSaveStorage.ReadArchive(input.Saver);
            Require(input.Type == SaveInfo.SaveType.Manual && input.Name == request.PersistenceLoad.InternalName &&
                input.OperationState == SaveInfo.StateType.None && input.HasFileOnDisk &&
                read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 3 && read.HistoricalData == null &&
                JToken.DeepEquals(JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()),
                    JObject.FromObject(saved, MountedSaveCodec.CreateSerializer())),
                "Current-format migration child did not load the parent's actual schema-3 native archive.");
            currentMigrationParentPath = path; currentMigrationParentLength = file.Length; currentMigrationInputPath = input.FolderName;
            VerifyCurrentMigrationInput();
            evidence["currentFormatMigration"] = new JObject { ["contract"] = "owner-seed-current-format-load-v1",
                ["parentCheckpoint"] = source["checkpoint"].DeepClone(), ["parentRunId"] = parent["runId"].DeepClone(),
                ["parentArtifactSha256"] = request.OwnedPairs.CheckpointSourceArtifactSha256,
                ["parentProcessId"] = source["processId"].DeepClone(), ["inputSchema"] = read.SourceSchema,
                ["inputSha256"] = request.PersistenceLoad.Sha256, ["inputLength"] = request.PersistenceLoad.Length,
                ["parentNativeWriteSha256"] = source["nativeWrite"]["sha256"].DeepClone(),
                ["loadedEqualsParentNativeWrite"] = true, ["restoredWorld"] = Chunk7ArchiveNativeSnapshot.Capture(world, saved) };
        }

        internal static JObject ValidateCurrentMigrationParent(JObject parent, RuntimeRequest child, MountedArchiveData loaded, int processId)
        {
            Require(parent != null && child?.OwnedPairs?.IsOwnerSeedCheckpointLoad == true &&
                Chunk7OwnedPairsSelection.CurrentMigrationCheckpoint(child.PersistenceCase) && child.Schema1DerivationSha256 == null &&
                child.OwnedPairs.ExpectedNativeLoadCount == 1 && child.OwnedPairs.ExpectedNativeWriteCount == 0 &&
                child.PersistenceLoad != null && loaded != null && processId > 0,
                "Current-format continuation requires its closed load-only declaration.");
            var schema1 = child.PersistenceCase == "owned-schema1-current-manual";
            var checkpoint = schema1 ? OwnedSchema1Cold : "legacy-mounted-pending-tb-manual";
            var name = schema1 ? Schema1OutputName : Schema2OutputName;
            var source = parent["observations"] as JObject; var write = source?["nativeWrite"] as JObject;
            var migration = source?[schema1 ? "schema1Migration" : "schema2Migration"] as JObject;
            Require((string)parent["scenario"] == Chunk7OwnedPairsSelection.Scenario && (string)parent["evidenceKind"] == EvidenceKind &&
                (string)parent["runId"] == child.OwnedPairs.CheckpointSourceRunId && (string)parent["status"] == "PASS" &&
                parent["errors"] is JArray errors && errors.Count == 0 &&
                Regex.IsMatch((string)parent["commit"] ?? "", "^[a-f0-9]{40}$") &&
                Regex.IsMatch((string)parent["dllSha256"] ?? "", "^[a-f0-9]{64}$") &&
                Guid.TryParseExact((string)parent["dllMvid"], "D", out var mvid) && mvid != Guid.Empty &&
                (string)source?["checkpoint"] == checkpoint && (bool?)source?["cold"] == true &&
                source["processId"]?.Type == JTokenType.Integer && (int)source["processId"] > 0 && (int)source["processId"] != processId &&
                (string)source["inheritedManualSha256"] == child.OwnedPairs.SourceArchiveSha256 &&
                migration != null && (int?)migration["inputSchema"] == (schema1 ? 1 : 2) && migration["resavedWorld"] is JObject &&
                source[schema1 ? "schema2Migration" : "schema1Migration"] == null && write != null,
                "Current-format continuation parent is not the exact accepted historical native migration/resave.");
            Require((string)write["nativeName"] == name && child.PersistenceLoad.InternalName == name &&
                (string)write["nativeType"] == "Manual" && (int?)write["sourceSchema"] == 3 &&
                (bool?)write["nativeCallback"] == true && (string)write["operation"] == "None" && (int?)write["snapshotCount"] == 1 &&
                (string)write["sha256"] == child.PersistenceLoad.Sha256 && (long?)write["length"] == child.PersistenceLoad.Length &&
                child.PersistenceLoad.Length > 0 && write["archive"] is JObject &&
                Path.GetFileName((string)write["path"]) == child.PersistenceLoad.FileName &&
                JToken.DeepEquals(write["archive"], JObject.FromObject(loaded, MountedSaveCodec.CreateSerializer())),
                "Current-format continuation substituted the parent's genuine current-schema native write.");
            loaded.Validate();
            Require(loaded.SchemaVersion == 3 && loaded.CampaignId == child.PersistenceLoad.GameId &&
                loaded.AreaId == child.PersistenceLoad.Area && loaded.Pairs.Length == 1 &&
                loaded.Pairs[0].Rider.Id == child.OwnedPairs.RiderAId && loaded.Pairs[0].Mount.Id == child.OwnedPairs.MountAId &&
                loaded.Pairs[0].ProfileId == SupportedMountedProfiles.Horse.Id,
                "Current-format continuation changed its exact surviving Horse owner or native campaign.");
            if (schema1)
                ValidateOwnedSchema1Archive(loaded, child.PersistenceLoad.GameId, child.PersistenceLoad.Area,
                    child.OwnedPairs.RiderAId, child.OwnedPairs.MountAId, true);
            else ValidateSchema2Continuity(loaded, loaded);
            return source;
        }

        private void VerifyCurrentMigrationInput()
        {
            Require(CurrentMigrationCase && currentMigrationParentPath != null && currentMigrationInputPath != null &&
                new FileInfo(currentMigrationParentPath).Length == currentMigrationParentLength &&
                Hash(currentMigrationParentPath) == request.OwnedPairs.CheckpointSourceArtifactSha256 &&
                new FileInfo(currentMigrationInputPath).Length == request.PersistenceLoad.Length &&
                Hash(currentMigrationInputPath) == request.PersistenceLoad.Sha256 && persistence.SnapshotCount == 0 &&
                replaySaveAuthority.AdmittedNativeWriteRequests == 0 && replayAuthorization.AuthorizedWriteCount == 0,
                "Current-format continuation changed its immutable parent/input or admitted a save.");
        }
    }
}
