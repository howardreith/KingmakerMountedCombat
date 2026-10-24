using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedArchiveReadResult
    {
        internal MountedSaveReadKind Kind;
        internal int SourceSchema;
        internal MountedArchiveData Data;
        internal MountedSaveData HistoricalData;
        internal string OriginalJson, Feedback;
    }

    internal static class MountedArchiveCodec
    {
        // Six mounted pairs, twelve retained records, shared 64-actor/128-slot
        // bounds. The whole envelope must also fit ALL of these wire limits.
        internal const int MaximumCharacters = 131072;
        internal const int MaximumBytes = 131072;
        internal const int MaximumTokens = 32768;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        internal static string Encode(MountedArchiveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Validate();
            string json;
            using (var buffer = new StringWriter(CultureInfo.InvariantCulture))
            {
                using (var writer = new JsonTextWriter(buffer) { Formatting = Formatting.None })
                    MountedSaveCodec.CreateSerializer().Serialize(writer, data);
                json = buffer.ToString();
            }
            VerifyBounds(json);
            return json;
        }

        private static void VerifyBounds(string json)
        {
            MountedSaveCodec.VerifyBoundedPrimitiveJson(json, MaximumCharacters, MaximumTokens);
            if (Utf8.GetByteCount(json) > MaximumBytes) throw new InvalidDataException("Mounted archive exceeds its byte bound.");
        }

        internal static MountedArchiveReadResult Decode(string json)
        {
            var result = new MountedArchiveReadResult { OriginalJson = json };
            if (json == null) { result.Kind = MountedSaveReadKind.Missing; return result; }
            try
            {
                VerifyBounds(json);
                var root = JObject.Parse(json);
                var version = root["SchemaVersion"];
                if (version?.Type != JTokenType.Integer) throw new InvalidDataException("Missing mounted schema version.");
                var schema = version.Value<long>();
                if (schema > MountedArchiveData.Schema)
                {
                    result.Kind = MountedSaveReadKind.Future;
                    result.Feedback = "Mounted metadata uses an unsupported schema; original data is preserved.";
                    return result;
                }
                if (schema == 1 || schema == 2)
                {
                    // Historical size, shape, type and semantic checks run first.
                    // Do not normalize malformed old data into a valid new envelope.
                    var old = MountedSaveCodec.Decode(json);
                    result.Kind = old.Kind; result.Feedback = old.Feedback;
                    if (old.Kind != MountedSaveReadKind.Current) return result;
                    result.SourceSchema = (int)schema;
                    result.HistoricalData = old.Data;
                    result.Data = Migrate(old.Data);
                    return result;
                }
                if (schema != MountedArchiveData.Schema) throw new InvalidDataException("Unsupported historical schema.");
                MountedSaveCodec.VerifyShape(root, typeof(MountedArchiveData), new HashSet<Type> {
                    typeof(MountedArchiveData), typeof(SavedMountedPair), typeof(SavedPairParticipation) });
                using (var reader = root.CreateReader())
                    result.Data = MountedSaveCodec.CreateSerializer().Deserialize<MountedArchiveData>(reader);
                if (result.Data == null) throw new InvalidDataException("Empty mounted archive.");
                result.Data.Validate();
                result.SourceSchema = MountedArchiveData.Schema;
                result.Kind = MountedSaveReadKind.Current;
                return result;
            }
            catch (Exception error) when (error is JsonException || error is InvalidDataException ||
                error is FormatException || error is OverflowException || error is ArgumentException)
            {
                result.Kind = MountedSaveReadKind.Invalid; result.Data = null; result.HistoricalData = null;
                result.Feedback = "Mounted metadata could not be interpreted safely (" + error.GetType().Name + ").";
                return result;
            }
        }

        private static MountedArchiveData Migrate(MountedSaveData old)
        {
            // Clone through the already-validated primitive format: migration must
            // not remove Paired from the object used by the schema-2 native adapter.
            var copy = MountedSaveCodec.Decode(MountedSaveCodec.Encode(old)).Data;
            var paired = copy.Combat?.Paired;
            // Historical unmounted combat can serialize an empty coordinator DTO.
            // It carries no actor obligation; keep its raw historical data but do
            // not invent a registry owner with no native participant.
            if (paired != null && paired.RiderId == null && paired.MountId == null &&
                paired.PendingSplitId == null && paired.Activation == null) paired = null;
            if (copy.Combat != null) copy.Combat.Paired = null;
            var pairId = MigrationOwnerId(copy.CampaignId, copy.Rider?.Id, copy.Mount?.Id);
            var linked = copy.Mounted && SavedPairParticipation.BelongsToMountedPair(paired, copy.Rider.Id, copy.Mount.Id);
            var result = new MountedArchiveData { SchemaVersion = MountedArchiveData.Schema,
                CampaignId = copy.CampaignId, AreaId = copy.AreaId, GameTimeTicks = copy.GameTimeTicks,
                Policy = copy.Policy, RulesId = copy.RulesId, Slots = copy.Slots, Combat = copy.Combat,
                Pairs = copy.Mounted ? new[] { new SavedMountedPair { PairId = pairId, Generation = 1,
                    ProfileId = copy.ProfileId, Rider = copy.Rider, Mount = copy.Mount } } : new SavedMountedPair[0],
                Participations = paired == null ? new SavedPairParticipation[0] : new[] {
                    new SavedPairParticipation { OwnerId = linked ? pairId : MigrationOwnerId(copy.CampaignId,
                        paired.RiderId ?? paired.PendingSplitId, paired.MountId), Generation = 1,
                        MountedPairId = linked ? pairId : null, State = paired } }
            };
            result.Validate();
            return result;
        }

        private static string MigrationOwnerId(string campaign, string rider, string mount)
        {
            // Stable synthetic custody identity for formats which never stored one.
            // No native entity/activation identity, debt or grant is rewritten.
            using (var hash = SHA256.Create())
            {
                var bytes = hash.ComputeHash(Utf8.GetBytes("kmc-schema3|" + campaign + "|" + rider + "|" + mount));
                var id = new byte[16]; Array.Copy(bytes, id, id.Length);
                return new Guid(id).ToString("N");
            }
        }
    }
}
