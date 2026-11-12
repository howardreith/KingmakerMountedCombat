using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace KingmakerMountedCombat.Domain
{
    public sealed class MountedRetainedIntentPairRecord
    {
        public string RiderId { get; set; }
        public string MountId { get; set; }
        public string ProfileId { get; set; }
        // The exact pair-owner identity (Guid "N" form) and generation captured when the attachment was suspended.
        public string OwnerId { get; set; }
        public long Generation { get; set; }
    }

    // Exploration repair (review finding 2): suspended mounted intent is saved as its own bounded archive member
    // ("kmc-retained-intent"), separate from the strict schema-3 kmc-mounted-state member, so existing saves and
    // older readers are unaffected. It names only stable actor ids, the profile and the owner identity; it never
    // carries a command, position, action or turn. A record is accepted only for the exact campaign, area and native
    // game time of the save it was written into, so a stale member can never restore anything.
    public sealed class MountedRetainedIntentRecord
    {
        public const string Header = "KMC-RETAINED-INTENT 1";
        public const int MaximumPairs = 6;
        public const int MaximumCharacters = 4096;

        public string CampaignId { get; set; }
        public string AreaId { get; set; }
        public long GameTimeTicks { get; set; }
        public MountedRetainedIntentKind Kind { get; set; }
        public MountedRetainedIntentPairRecord[] Pairs { get; set; } = new MountedRetainedIntentPairRecord[0];

        // The pairs a save records as retained intent: only suspended pairs, never a pair physically mounted at the
        // barrier (the kmc-mounted-state member owns it), one record per rider, at most MaximumPairs.
        public static MountedRetainedIntentPairRecord[] Recordable(IEnumerable<MountedRetainedIntentPairRecord> retained,
            ICollection<string> physicallyMountedActorIds)
        {
            return (retained ?? Enumerable.Empty<MountedRetainedIntentPairRecord>())
                .Where(p => p != null && !physicallyMountedActorIds.Contains(p.RiderId) && !physicallyMountedActorIds.Contains(p.MountId))
                .GroupBy(p => p.RiderId, StringComparer.Ordinal).Select(g => g.Last())
                .Take(MaximumPairs)
                .Select(p => new MountedRetainedIntentPairRecord { RiderId = p.RiderId, MountId = p.MountId, ProfileId = p.ProfileId,
                    OwnerId = p.OwnerId != null && p.Generation > 0 ? p.OwnerId : null,
                    Generation = p.OwnerId != null && p.Generation > 0 ? p.Generation : 0 })
                .ToArray();
        }

        // A loaded record is adopted only by the exact world it was written into.
        public bool MatchesLoadedWorld(string campaignId, string areaId, long gameTimeTicks)
        {
            return string.Equals(CampaignId, campaignId, StringComparison.Ordinal) &&
                string.Equals(AreaId, areaId, StringComparison.Ordinal) && GameTimeTicks == gameTimeTicks;
        }

        public string Encode()
        {
            Validate();
            var text = new StringBuilder();
            text.Append(Header).Append('\n');
            text.Append("campaign ").Append(CampaignId).Append('\n');
            text.Append("area ").Append(AreaId).Append('\n');
            text.Append("time ").Append(GameTimeTicks.ToString(CultureInfo.InvariantCulture)).Append('\n');
            text.Append("kind ").Append(Kind.ToString()).Append('\n');
            foreach (var pair in Pairs)
                text.Append("pair ").Append(pair.RiderId).Append(' ').Append(pair.MountId).Append(' ').Append(pair.ProfileId).Append(' ')
                    .Append(pair.OwnerId ?? "-").Append(' ').Append(pair.Generation.ToString(CultureInfo.InvariantCulture)).Append('\n');
            var result = text.ToString();
            if (result.Length > MaximumCharacters) throw new InvalidDataException("Retained mounted intent exceeds its bound.");
            return result;
        }

        public static MountedRetainedIntentRecord Decode(string text)
        {
            if (text == null || text.Length == 0 || text.Length > MaximumCharacters)
                throw new InvalidDataException("Retained mounted intent is empty or exceeds its bound.");
            var lines = text.Split('\n');
            if (lines.Length < 6 || lines[lines.Length - 1].Length != 0 || lines[0] != Header)
                throw new InvalidDataException("Retained mounted intent has an unknown header or shape.");
            var record = new MountedRetainedIntentRecord
            {
                CampaignId = Field(lines[1], "campaign"),
                AreaId = Field(lines[2], "area"),
                GameTimeTicks = ParseLong(Field(lines[3], "time")),
                Kind = ParseKind(Field(lines[4], "kind"))
            };
            var pairs = new List<MountedRetainedIntentPairRecord>();
            for (var index = 5; index < lines.Length - 1; index++)
            {
                var parts = Field(lines[index], "pair").Split(' ');
                if (parts.Length != 5) throw new InvalidDataException("Retained mounted pair has an unknown shape.");
                pairs.Add(new MountedRetainedIntentPairRecord { RiderId = parts[0], MountId = parts[1], ProfileId = parts[2],
                    OwnerId = parts[3] == "-" ? null : parts[3], Generation = ParseLong(parts[4]) });
            }
            record.Pairs = pairs.ToArray();
            record.Validate();
            return record;
        }

        public void Validate()
        {
            Require(Guid.TryParse(CampaignId, out var campaign) && campaign != Guid.Empty && CampaignId.IndexOf(' ') < 0,
                "Retained mounted intent campaign is invalid.");
            Require(AreaId != null && AreaId.Length == 32 && AreaId.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'),
                "Retained mounted intent area is invalid.");
            Require(GameTimeTicks >= 0, "Retained mounted intent time is invalid.");
            Require(Enum.IsDefined(typeof(MountedRetainedIntentKind), Kind), "Retained mounted intent kind is invalid.");
            Require(Pairs != null && Pairs.Length >= 1 && Pairs.Length <= MaximumPairs, "Retained mounted intent pair count is invalid.");
            var actors = new HashSet<Guid>();
            var owners = new HashSet<Guid>();
            foreach (var pair in Pairs)
            {
                Require(pair != null && ActorId(pair.RiderId) && ActorId(pair.MountId), "Retained mounted pair actor is invalid.");
                Require(actors.Add(Guid.Parse(pair.RiderId)) && actors.Add(Guid.Parse(pair.MountId)),
                    "Retained mounted pair names one actor twice or in two pairs.");
                Require(MountedRiderProfileCatalog.IsSupportedArchiveProfile(pair.ProfileId) && pair.ProfileId.IndexOf(' ') < 0,
                    "Retained mounted pair profile is unsupported.");
                if (pair.OwnerId == null) Require(pair.Generation == 0, "Retained mounted pair generation has no owner.");
                else Require(Guid.TryParse(pair.OwnerId, out var owner) && owner != Guid.Empty && pair.OwnerId.IndexOf(' ') < 0 &&
                    owners.Add(owner) && pair.Generation > 0, "Retained mounted pair owner identity is invalid or duplicated.");
            }
        }

        private static bool ActorId(string value) => value != null && value.Length <= 64 && value.IndexOf(' ') < 0 &&
            Guid.TryParse(value, out var parsed) && parsed != Guid.Empty;

        private static string Field(string line, string name)
        {
            if (line == null || !line.StartsWith(name + " ", StringComparison.Ordinal) || line.Length == name.Length + 1)
                throw new InvalidDataException("Retained mounted intent field " + name + " is missing.");
            return line.Substring(name.Length + 1);
        }

        private static long ParseLong(string value)
        {
            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
                throw new InvalidDataException("Retained mounted intent number is invalid.");
            return parsed;
        }

        private static MountedRetainedIntentKind ParseKind(string value)
        {
            foreach (MountedRetainedIntentKind kind in Enum.GetValues(typeof(MountedRetainedIntentKind)))
                if (string.Equals(kind.ToString(), value, StringComparison.Ordinal)) return kind;
            throw new InvalidDataException("Retained mounted intent kind is unknown.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }
    }
}
