using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Read-only source evidence. A match is not native hire/load/save authority.
    internal static class Chunk7MediumHireBodySource
    {
        internal static bool Matches(JToken inventory, Chunk7MediumHireBodyPins pins)
        {
            var party = (inventory as JObject)?["party"] as JArray;
            if (pins == null || party == null || party.Count < 1 || party.Count > 24 || party.Any(value => !(value is JObject))) return false;
            var rows = party.OfType<JObject>().Where(actor => Text(actor["id"], pins.TemplateActorId)).ToArray();
            if (rows.Length != 1) return false;
            var row = rows[0];
            var character = row["character"] as JObject;
            var skeleton = character?["skeleton"] as JObject;
            return Text(row["originalSize"], "Medium") && Text(row["effectiveSize"], "Medium") &&
                Boolean(row["polymorphed"], false) && Boolean(row["dollPresent"], true) &&
                Text(row["raceGuid"], pins.RaceGuid) && Text(row["dollPresetGuid"], pins.PresetGuid) &&
                Text(row["dollGender"], pins.Gender) && pins.LeftHanded.HasValue && Boolean(row["dollLeftHanded"], pins.LeftHanded.Value) &&
                row["raceId"]?.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)row["raceId"]) &&
                Text(row["dollRaceId"], (string)row["raceId"]) && Boolean(character?["present"], true) &&
                Boolean(skeleton?["present"], true) && Text(skeleton?["name"], pins.SkeletonName);
        }
        private static bool Text(JToken value, string expected) => value?.Type == JTokenType.String && expected != null && (string)value == expected;
        private static bool Boolean(JToken value, bool expected) => value?.Type == JTokenType.Boolean && (bool)value == expected;
    }
}
