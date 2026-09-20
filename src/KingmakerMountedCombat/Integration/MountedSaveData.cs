using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Newtonsoft.Json.Serialization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Integration
{
    // Primitive archive data, decoded with KMC's own bounded settings. Never use
    // the game's polymorphic object serializer for this extension.
    internal sealed class MountedSaveData
    {
        internal const int CurrentSchema = 2;
        internal const string ArchiveMember = "kmc-mounted-state";
        internal const string PairedPolicy = "rider-principal-distinct-native-v1";
        internal const string Rules = "crpg-transport-v1";
        public int SchemaVersion { get; set; }
        public string CampaignId { get; set; }
        public string AreaId { get; set; }
        public long GameTimeTicks { get; set; }
        public string Policy { get; set; }
        public string RulesId { get; set; }
        public bool Mounted { get; set; }
        public string ProfileId { get; set; }
        public SavedNativeActor Rider { get; set; }
        public SavedNativeActor Mount { get; set; }
        public SavedMountedSlot[] Slots { get; set; }
        public SavedCombatData Combat { get; set; }

        internal void Validate()
        {
            if (SchemaVersion != CurrentSchema || !Guid.TryParse(CampaignId, out var campaign) ||
                campaign == Guid.Empty || !HexId(AreaId) || GameTimeTicks < 0 ||
                Policy != PairedPolicy || RulesId != Rules)
                throw new InvalidDataException("Mounted save identity, clock or policy is invalid.");
            if (Mounted)
            {
                if (ProfileId != "medium-humanoid-mammoth-v1" && ProfileId != "medium-humanoid-horse-v1")
                    throw new InvalidDataException("Unsupported saved mounted profile.");
                if (Rider == null || Mount == null) throw new InvalidDataException("Saved pair is incomplete.");
                Rider.Validate(); Mount.Validate();
                if (Guid.Parse(Rider.Id) == Guid.Parse(Mount.Id)) throw new InvalidDataException("Saved pair identifies one actor twice.");
            }
            else if (Rider != null || Mount != null || ProfileId != null)
                throw new InvalidDataException("Unmounted relationship contains a pair.");
            if (Combat != null)
            {
                Combat.Validate(GameTimeTicks);
                if (Mounted && (!SameActorDebt(Rider, Combat.Actors.SingleOrDefault(a => a.Native.Id == Rider.Id)?.Native) ||
                    !SameActorDebt(Mount, Combat.Actors.SingleOrDefault(a => a.Native.Id == Mount.Id)?.Native)))
                    throw new InvalidDataException("Pair and combat actor snapshots disagree.");
            }
            if (Slots == null || Slots.Length > 128) throw new InvalidDataException("Owned control slots are not bounded.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in Slots)
            {
                if (slot == null || !ActorId(slot.ActorId) || slot.Index < 0 || slot.Index > 127 ||
                    slot.Kind < 1 || slot.Kind > 4 || !seen.Add(Guid.Parse(slot.ActorId).ToString("N") + ":" + slot.Index))
                    throw new InvalidDataException("Invalid or duplicate owned control slot.");
            }
        }

        private static bool SameActorDebt(SavedNativeActor first, SavedNativeActor second) => second != null &&
            first.Id == second.Id && first.Standard == second.Standard && first.Move == second.Move &&
            first.Swift == second.Swift && first.Initiative == second.Initiative && first.Reaction == second.Reaction &&
            first.ReactionsRemaining == second.ReactionsRemaining && first.LastSurpriseTicks == second.LastSurpriseTicks;

        internal static bool HexId(string value) => value != null && value.Length == 32 &&
            value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');

        internal static bool ActorId(string value) => value != null && value.Length <= 64 &&
            Guid.TryParse(value, out var parsed) && parsed != Guid.Empty;
    }

    internal sealed class SavedNativeActor
    {
        public string Id { get; set; }
        // Legitimate current cooldowns, never PairedActivation's observed maxima.
        public float Standard { get; set; }
        public float Move { get; set; }
        public float Swift { get; set; }
        public float Initiative { get; set; }
        public float Reaction { get; set; }
        public int ReactionsRemaining { get; set; }
        public long LastSurpriseTicks { get; set; }

        internal void Validate()
        {
            if (!MountedSaveData.ActorId(Id) || ReactionsRemaining < 0 || ReactionsRemaining > 1024 ||
                LastSurpriseTicks < 0 || !Debt(Standard) || !Debt(Move) || !Debt(Swift) ||
                !Debt(Initiative) || !Debt(Reaction))
                throw new InvalidDataException("Saved actor identity or current action debt is invalid.");
        }

        private static bool Debt(float value) => !float.IsNaN(value) && !float.IsInfinity(value) &&
            value >= 0 && value <= 86400;
    }

    internal sealed class SavedMountedSlot
    {
        public string ActorId { get; set; }
        public int Index { get; set; }
        public int Kind { get; set; }
    }

    internal enum MountedSaveReadKind { Missing, Current, Future, Invalid }

    internal sealed class MountedSaveReadResult
    {
        internal MountedSaveReadKind Kind { get; }
        internal MountedSaveData Data { get; }
        internal string OriginalJson { get; }
        internal string Feedback { get; }
        internal MountedSaveReadResult(MountedSaveReadKind kind, MountedSaveData data, string json, string feedback)
        { Kind = kind; Data = data; OriginalJson = json; Feedback = feedback; }
    }

    internal static class MountedSaveCodec
    {
        private const int MaximumCharacters = 32768;
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Error,
            MaxDepth = 12,
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
            ContractResolver = new DefaultContractResolver(),
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            Culture = CultureInfo.InvariantCulture
        };

        // Create (not CreateDefault/JsonConvert) bypasses the game's global
        // opt-in entity contracts, converters and reference-preservation defaults.
        internal static JsonSerializer CreateSerializer() => JsonSerializer.Create(Settings);

        internal static string Encode(MountedSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Validate();
            string json;
            using (var buffer = new StringWriter(CultureInfo.InvariantCulture))
            {
                using (var writer = new JsonTextWriter(buffer) { Formatting = Formatting.None })
                    CreateSerializer().Serialize(writer, data);
                json = buffer.ToString();
            }
            if (json.Length > MaximumCharacters) throw new InvalidDataException("Mounted save data is oversized.");
            return json;
        }

        internal static MountedSaveReadResult Decode(string json)
        {
            if (json == null) return new MountedSaveReadResult(MountedSaveReadKind.Missing, null, null,
                "This save has no mounted relationship metadata.");
            try
            {
                VerifyBoundedPrimitiveJson(json);
                var root = JObject.Parse(json);
                var version = root["SchemaVersion"];
                if (version?.Type != JTokenType.Integer) throw new InvalidDataException("Missing mounted schema version.");
                var schema = version.Value<long>();
                if (schema > MountedSaveData.CurrentSchema)
                    return new MountedSaveReadResult(MountedSaveReadKind.Future, null, json,
                        "Mounted metadata uses a newer schema; original data is preserved.");
                switch (schema)
                {
                    case 1:
                        // Schema 1's outside-combat format has no combat member.
                        // Migration is in memory only; the selected archive is not rewritten.
                        RequireFields(root, "SchemaVersion", "CampaignId", "AreaId", "GameTimeTicks", "Policy",
                            "RulesId", "Mounted", "ProfileId", "Rider", "Mount", "Slots");
                        root["SchemaVersion"] = MountedSaveData.CurrentSchema;
                        root.Add("Combat", JValue.CreateNull());
                        goto case MountedSaveData.CurrentSchema;
                    case MountedSaveData.CurrentSchema:
                        VerifyCurrentShape(root);
                        MountedSaveData data;
                        using (var reader = root.CreateReader())
                            data = CreateSerializer().Deserialize<MountedSaveData>(reader);
                        if (data == null) throw new InvalidDataException("Mounted metadata was empty.");
                        data.Validate();
                        return new MountedSaveReadResult(MountedSaveReadKind.Current, data, json,
                            schema == 1 ? "Historical mounted metadata migrated in memory; source archive unchanged." : null);
                    default:
                        throw new InvalidDataException("Unsupported historical mounted schema.");
                }
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidDataException ||
                exception is FormatException || exception is OverflowException || exception is ArgumentException)
            {
                return new MountedSaveReadResult(MountedSaveReadKind.Invalid, null, json,
                    "Mounted metadata could not be interpreted safely (" + exception.GetType().Name + ").");
            }
        }

        private static readonly HashSet<Type> ModelTypes = new HashSet<Type>
        {
            typeof(MountedSaveData), typeof(SavedNativeActor), typeof(SavedMountedSlot), typeof(SavedCombatData),
            typeof(SavedCombatActor), typeof(SavedAiAction), typeof(SavedEngagement), typeof(SavedRosterActor),
            typeof(SavedMovementValues), typeof(SavedTurnContext), typeof(SavedMovementAllocation),
            typeof(SavedPairedState), typeof(SavedActivation), typeof(SavedParticipation)
        };

        private static void VerifyCurrentShape(JObject root) => VerifyShape(root, typeof(MountedSaveData));

        private static void VerifyShape(JToken token, Type expected)
        {
            if (token == null) throw new InvalidDataException("Missing metadata value.");
            var nullable = Nullable.GetUnderlyingType(expected);
            if (token.Type == JTokenType.Null)
            {
                if (expected.IsValueType && nullable == null)
                    throw new InvalidDataException("A required primitive is null.");
                return;
            }
            if (nullable != null) expected = nullable;
            var valid = expected == typeof(string) ? token.Type == JTokenType.String :
                expected == typeof(bool) ? token.Type == JTokenType.Boolean :
                expected == typeof(int) || expected == typeof(long) ? token.Type == JTokenType.Integer :
                expected == typeof(float) ? token.Type == JTokenType.Integer || token.Type == JTokenType.Float : false;
            if (expected == typeof(string) || expected == typeof(bool) || expected == typeof(int) ||
                expected == typeof(long) || expected == typeof(float))
            {
                if (!valid) throw new InvalidDataException("Metadata has an incorrect primitive type.");
                return;
            }
            if (expected.IsArray)
            {
                if (!(token is JArray array)) throw new InvalidDataException("Metadata collection is not an array.");
                foreach (var item in array) VerifyShape(item, expected.GetElementType());
                return;
            }
            // Only this compile-time set of primitive DTOs can be constructed.
            // No type/member name supplied by save data participates in reflection.
            if (!ModelTypes.Contains(expected)) throw new InvalidDataException("Unknown metadata model.");
            var value = token as JObject;
            var properties = expected.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            RequireFields(value, properties.Select(p => p.Name).ToArray());
            foreach (var property in properties) VerifyShape(value[property.Name], property.PropertyType);
        }

        private static void RequireFields(JObject value, params string[] fields)
        {
            if (value == null || value.Properties().Count() != fields.Length ||
                fields.Any(name => value.Property(name) == null))
                throw new InvalidDataException("Mounted metadata has missing or unknown fields.");
        }

        private static void VerifyBoundedPrimitiveJson(string json)
        {
            if (json.Length == 0 || json.Length > MaximumCharacters)
                throw new InvalidDataException("Mounted metadata size is invalid.");
            var objects = new Stack<HashSet<string>>();
            using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 12,
                DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double })
            {
                var tokens = 0;
                while (reader.Read())
                {
                    if (++tokens > 4096) throw new InvalidDataException("Mounted metadata has too many fields.");
                    if (reader.TokenType == JsonToken.StartObject)
                        objects.Push(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    else if (reader.TokenType == JsonToken.EndObject) objects.Pop();
                    else if (reader.TokenType == JsonToken.PropertyName)
                    {
                        var name = (string)reader.Value;
                        if (objects.Count == 0 || string.IsNullOrEmpty(name) || name[0] == '$' ||
                            !objects.Peek().Add(name))
                            throw new InvalidDataException("Duplicate or nonprimitive metadata member.");
                    }
                    else if (reader.TokenType == JsonToken.Comment || reader.TokenType == JsonToken.StartConstructor ||
                        reader.TokenType == JsonToken.Undefined || reader.TokenType == JsonToken.Bytes)
                        throw new InvalidDataException("Metadata is not primitive JSON.");
                    else if (reader.TokenType == JsonToken.Float &&
                        (double.IsNaN(Convert.ToDouble(reader.Value)) || double.IsInfinity(Convert.ToDouble(reader.Value))))
                        throw new InvalidDataException("Nonfinite metadata number.");
                }
            }
        }
    }
}
