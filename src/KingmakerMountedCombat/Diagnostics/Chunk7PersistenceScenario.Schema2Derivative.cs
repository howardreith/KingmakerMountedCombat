using System;
using System.IO;
using System.Linq;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        private string ownedSchema2CheckpointPath, ownedSchema2ReceiptPath;
        private long ownedSchema2CheckpointLength, ownedSchema2ReceiptLength;
        private JObject ownedSchema2DerivativeEvidence;

        // This observation projection is never a writer mode or a restoration
        // permission. The real historical decoder remains the migration authority.
        internal static MountedSaveData ProjectOwnedPendingSchema2(MountedArchiveData source)
        {
            Require(source != null, "Owned pending source is absent."); source.Validate();
            Require(source.Pairs.Length == 1 && source.Participations.Length == 1 && source.Combat?.TurnBased == true,
                "Owned schema-2 projection cannot discard another pair, participation or native world.");
            var pair = source.Pairs.Single(); var participation = source.Participations.Single();
            var pending = participation.State;
            Require(participation.OwnerId == pair.PairId && participation.Generation == pair.Generation &&
                participation.MountedPairId == pair.PairId && pending != null && pending.Activation == null &&
                pending.RiderId == null && pending.MountId == null && pending.PendingSplitId == pair.Mount.Id,
                "Owned schema-2 projection requires the exact mounted owner and its pending-only survivor.");
            var serializer = MountedSaveCodec.CreateSerializer();
            var combat = JObject.FromObject(source.Combat, serializer).ToObject<SavedCombatData>(serializer);
            combat.Paired = JObject.FromObject(pending, serializer).ToObject<SavedPairedState>(serializer);
            var result = new MountedSaveData { SchemaVersion = 2, CampaignId = source.CampaignId,
                AreaId = source.AreaId, GameTimeTicks = source.GameTimeTicks, Policy = source.Policy, RulesId = source.RulesId,
                Mounted = true, ProfileId = pair.ProfileId,
                Rider = JObject.FromObject(pair.Rider, serializer).ToObject<SavedNativeActor>(serializer),
                Mount = JObject.FromObject(pair.Mount, serializer).ToObject<SavedNativeActor>(serializer),
                Slots = JArray.FromObject(source.Slots, serializer).ToObject<SavedMountedSlot[]>(serializer), Combat = combat };
            result.Validate();
            return result;
        }

        private void ReadOwnedSchema2Derivative()
        {
            Require(Schema2ResaveCase && request.OwnedPairs?.IsOwnerSeedCheckpointLoad == true &&
                schema2Input != null && saved != null && schema2Imported != null,
                "Owned schema-2 derivative observation belongs only to its real cold-loaded input.");
            var root = Path.GetDirectoryName(replaySaveAuthority.Root);
            var path = Path.Combine(root, "owned-schema2-checkpoint.json"); var file = new FileInfo(path);
            Require(file.Exists && file.Length > 0 && file.Length <= 16 * 1024 * 1024 &&
                (file.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(path) == request.OwnedPairs.CheckpointSourceArtifactSha256,
                "Owned pending producer evidence differs from its immutable request pin.");
            var checkpoint = JObject.Parse(File.ReadAllText(path)); var source = checkpoint["observations"] as JObject;
            Require((string)checkpoint["runId"] == request.OwnedPairs.CheckpointSourceRunId &&
                (string)checkpoint["scenario"] == Chunk7OwnedPairsSelection.Scenario &&
                (string)checkpoint["evidenceKind"] == EvidenceKind && (string)checkpoint["status"] == "PASS" &&
                checkpoint["errors"] is JArray errors && errors.Count == 0 && (string)checkpoint["commit"] == request.Commit &&
                (string)checkpoint["dllSha256"] == request.DllSha256 && (string)checkpoint["dllMvid"] == request.DllMvid &&
                (string)source?["checkpoint"] == request.PersistenceCase && (bool?)source?["cold"] == false &&
                (string)source?["nativeSourceContract"] == "owner-seed-current-pending-source-v1" && source?["legacyWriterScope"] == null &&
                (int?)source?["nativeWrite"]?["sourceSchema"] == 3 &&
                (string)source?["inheritedManualSha256"] == request.OwnedPairs.SourceArchiveSha256,
                "Owned pending source must be the exact same-candidate successful native current-format producer.");
            var original = source["nativeWrite"]?["archive"]?.ToObject<MountedArchiveData>(MountedSaveCodec.CreateSerializer());
            Require(original?.CampaignId == request.PersistenceLoad.GameId && original.AreaId == request.PersistenceLoad.Area &&
                original.Pairs?.SingleOrDefault()?.Rider.Id == firstRider.UniqueId && original.Pairs?.SingleOrDefault()?.Mount.Id == firstMount.UniqueId,
                "Owned pending original archive differs from the cold input's native actors or campaign.");
            var projection = ProjectOwnedPendingSchema2(original);
            var native = NativeMountedSaveStorage.ReadArchive(schema2Input.Saver);
            var projected = JObject.FromObject(projection, MountedSaveCodec.CreateSerializer());
            Require(native.Kind == MountedSaveReadKind.Current && native.SourceSchema == 2 && native.HistoricalData != null &&
                JToken.DeepEquals(JObject.Parse(native.OriginalJson), projected) &&
                JToken.DeepEquals(JObject.FromObject(native.Data, MountedSaveCodec.CreateSerializer()), JObject.FromObject(saved, MountedSaveCodec.CreateSerializer())),
                "Actual historical loader did not restore the faithful pending-only metadata projection.");
            var receiptPath = Path.Combine(root, "owned-schema2-derivation.json"); var receiptFile = new FileInfo(receiptPath);
            Require(receiptFile.Exists && receiptFile.Length > 0 && receiptFile.Length <= 256 * 1024 &&
                (receiptFile.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(receiptPath) == request.Schema2DerivationSha256,
                "Owned schema-2 staged derivation differs from its immutable request pin.");
            var receipt = JObject.Parse(File.ReadAllText(receiptPath));
            Require((string)receipt["contract"] == "owned-schema2-metadata-copy-v1" && (bool?)receipt["nativeRestorationProven"] == false &&
                (string)receipt["sourceRunId"] == request.OwnedPairs.CheckpointSourceRunId &&
                (string)receipt["sourceArtifactSha256"] == request.OwnedPairs.CheckpointSourceArtifactSha256 &&
                (string)receipt["sourceDescriptor"]?["sha256"] == (string)source["nativeWrite"]?["sha256"] &&
                (string)receipt["descriptor"]?["sha256"] == request.PersistenceLoad.Sha256 &&
                (string)receipt["sourceDescriptor"]?["sha256"] != request.PersistenceLoad.Sha256 && JToken.DeepEquals(receipt["metadata"], projected),
                "Owned schema-2 derivation substituted its native source, projection or actual historical input.");
            ownedSchema2CheckpointPath = path; ownedSchema2ReceiptPath = receiptPath;
            ownedSchema2CheckpointLength = file.Length; ownedSchema2ReceiptLength = receiptFile.Length;
            ownedSchema2DerivativeEvidence = new JObject { ["syntheticMetadata"] = true,
                ["nativeInputMetadata"] = projected, ["derivationReceiptSha256"] = request.Schema2DerivationSha256,
                ["originalNativeArchiveSha256"] = source["nativeWrite"]["sha256"].DeepClone() };
            VerifyOwnedSchema2DerivativeImmutable();
        }

        private void VerifyOwnedSchema2DerivativeImmutable()
        {
            if (ownedSchema2CheckpointPath == null) return;
            Require(new FileInfo(ownedSchema2CheckpointPath).Length == ownedSchema2CheckpointLength &&
                new FileInfo(ownedSchema2ReceiptPath).Length == ownedSchema2ReceiptLength &&
                Hash(ownedSchema2CheckpointPath) == request.OwnedPairs.CheckpointSourceArtifactSha256 &&
                Hash(ownedSchema2ReceiptPath) == request.Schema2DerivationSha256,
                "Owned pending native checkpoint or metadata-only derivative changed during cold migration.");
        }
    }
}
