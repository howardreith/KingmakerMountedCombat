using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace KingmakerMountedCombat.Domain
{
    // Known issues S02 review R1: where native arrival placed the rider and the mount of a pair whose restoration was
    // deferred at arrival only because its views were not active yet. It is comparison evidence and never a
    // destination: nothing is ever moved to it. A pair whose two units still stand there is the arrival restoration,
    // only later (MountedRetainedIntentPolicy.StandsAtArrivalPlacement); a pair that has no such record, or has moved,
    // is ordinary retained intent. It names the area it was observed in: coordinates mean nothing in another area, so
    // it is saved only into a save of that area and judged only while that area is loaded. Immutable, so a custody
    // row, a save row and a loaded row can share one instance.
    public sealed class MountedRetainedArrivalPlacement
    {
        public MountedRetainedArrivalPlacement(string areaId, float riderX, float riderY, float riderZ, float mountX, float mountY, float mountZ)
        {
            AreaId = areaId;
            RiderX = riderX; RiderY = riderY; RiderZ = riderZ; MountX = mountX; MountY = mountY; MountZ = mountZ;
        }

        public string AreaId { get; }
        public float RiderX { get; }
        public float RiderY { get; }
        public float RiderZ { get; }
        public float MountX { get; }
        public float MountY { get; }
        public float MountZ { get; }

        public bool IsFinite => Finite(RiderX) && Finite(RiderY) && Finite(RiderZ) && Finite(MountX) && Finite(MountY) && Finite(MountZ);

        public bool ObservedIn(string areaId) => !string.IsNullOrEmpty(AreaId) && string.Equals(AreaId, areaId, StringComparison.Ordinal);

        // How far each unit now stands from where arrival placed it (metres; not a number for an unknown position).
        public float RiderDisplacementMetres(float x, float y, float z) => Distance(x - RiderX, y - RiderY, z - RiderZ);
        public float MountDisplacementMetres(float x, float y, float z) => Distance(x - MountX, y - MountY, z - MountZ);

        private static float Distance(float x, float y, float z) => (float)Math.Sqrt((double)x * x + (double)y * y + (double)z * z);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public sealed class MountedRetainedIntentPairRecord
    {
        public string RiderId { get; set; }
        public string MountId { get; set; }
        public string ProfileId { get; set; }
        // The exact pair-owner identity (Guid "N" form) and generation captured when the attachment was suspended.
        public string OwnerId { get; set; }
        public long Generation { get; set; }
        // Known issues S02 review R1: the arrival placement of a pair deferred at arrival, or null for ordinary retained
        // intent (every version 1 row). Never inferred: a row has it only if the custody row or the saved row held it.
        public MountedRetainedArrivalPlacement Arrival { get; set; }
    }

    // Second owner review finding 3: a retained pair's own control-slot rows (the archive SavedMountedSlot shape:
    // actor, index, kind), so a world-map save keeps the exact Mount/Dismount hotbar placement that area suspension
    // cleared, including a kind-7 placement record without a slot row (a control the player deliberately removed).
    public sealed class MountedRetainedIntentSlotRecord
    {
        public string ActorId { get; set; }
        public int Index { get; set; }
        public int Kind { get; set; }
    }

    // Second owner review finding 1: which rows a save records. Before an area arrival is attempted, the in-flight
    // transfer's suspended rows are legitimate travelling intent. Once arrival has been attempted, only the
    // authoritative unresolved retained set remains (restored pairs are physically mounted and belong to
    // kmc-mounted-state; dropped and retired pairs are not pending at all).
    public static class MountedRetainedIntentCapture
    {
        public static IEnumerable<MountedRetainedIntentPairRecord> PendingRows(IEnumerable<MountedRetainedIntentPairRecord> retained,
            IEnumerable<MountedRetainedIntentPairRecord> transfer, bool transferArrivalAttempted)
        {
            var rows = (retained ?? Enumerable.Empty<MountedRetainedIntentPairRecord>());
            return transferArrivalAttempted || transfer == null ? rows : rows.Concat(transfer);
        }

        public static MountedRetainedIntentKind Kind(MountedRetainedIntentKind retainedKind, bool transferPending,
            bool transferToWorldMap, MountedRetainedIntentKind transferCarriedKind)
        {
            if (!transferPending) return retainedKind;
            return transferToWorldMap ? MountedRetainedIntentKind.WorldMapTravel : transferCarriedKind;
        }

        // Known issues S02 review R1: one custody row as a save row. The arrival placement travels only with a row of
        // the retained custody. A row of a transfer still in flight has not arrived anywhere; it is ordinary intent.
        public static MountedRetainedIntentPairRecord Row(string riderId, string mountId, string profileId, string ownerId,
            long generation, MountedRetainedArrivalPlacement arrival, bool retainedCustody)
        {
            return new MountedRetainedIntentPairRecord { RiderId = riderId, MountId = mountId, ProfileId = profileId,
                OwnerId = ownerId, Generation = generation, Arrival = retainedCustody ? arrival : null };
        }

        // The retained-intent member text of one save, or null when no pair is pending (the member is then not written).
        // An arrival placement observed in any other area than the one being saved is left out: that pair is saved as
        // ordinary retained intent.
        public static string SaveText(string campaignId, string areaId, long gameTimeTicks, MountedRetainedIntentKind kind,
            IEnumerable<MountedRetainedIntentPairRecord> pendingRows, ICollection<string> physicallyMountedActorIds,
            IEnumerable<MountedRetainedIntentSlotRecord> slotRows)
        {
            var records = MountedRetainedIntentRecord.Recordable(pendingRows, physicallyMountedActorIds);
            if (records.Length == 0) return null;
            foreach (var record in records)
                if (record.Arrival != null && !record.Arrival.ObservedIn(areaId)) record.Arrival = null;
            return new MountedRetainedIntentRecord { CampaignId = campaignId, AreaId = areaId, GameTimeTicks = gameTimeTicks,
                Kind = kind, Pairs = records, Slots = MountedRetainedIntentRecord.RecordableSlots(slotRows, records) }.EncodeForSave();
        }
    }

    // Known issues S02 review R1: what a load adopts from a decoded record. The integration (AdoptLoadedRetainedIntent)
    // only projects these rows into its custody; every decision is here.
    public static class MountedRetainedIntentAdoption
    {
        // Only the exact load that selected the record, still the current load, may adopt it. A replaced or cancelled
        // load adopts nothing, so nothing of its record (an arrival placement included) reaches a later world.
        public static bool SelectingLoadIsCurrent(long selectingLoadSequence, bool restoreLoadPresent, long restoreLoadSequence,
            long currentLoadSequence)
        {
            return restoreLoadPresent && restoreLoadSequence == selectingLoadSequence && currentLoadSequence == selectingLoadSequence;
        }

        // The pair rows adopted in the exact world the record was written into (campaign, area and native game time),
        // never a pair the mounted-state member of the same save already restores. Each row keeps exactly what the
        // record holds: an arrival placement is adopted only if it was saved, and a version 1 row never gains one.
        public static MountedRetainedIntentPairRecord[] Pairs(MountedRetainedIntentRecord record, string campaignId, string areaId,
            long gameTimeTicks, ICollection<string> archivedActorIds)
        {
            if (record == null || !record.MatchesLoadedWorld(campaignId, areaId, gameTimeTicks)) return new MountedRetainedIntentPairRecord[0];
            return record.Pairs
                .Where(p => archivedActorIds == null || !archivedActorIds.Contains(p.RiderId) && !archivedActorIds.Contains(p.MountId))
                .Select(p => new MountedRetainedIntentPairRecord { RiderId = p.RiderId, MountId = p.MountId, ProfileId = p.ProfileId,
                    OwnerId = p.OwnerId, Generation = p.Generation, Arrival = p.Arrival })
                .ToArray();
        }

        // Only the adopted pairs' own control rows.
        public static MountedRetainedIntentSlotRecord[] Slots(MountedRetainedIntentRecord record, IEnumerable<MountedRetainedIntentPairRecord> adopted)
        {
            var actors = new HashSet<string>((adopted ?? Enumerable.Empty<MountedRetainedIntentPairRecord>())
                .SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
            return (record?.Slots ?? new MountedRetainedIntentSlotRecord[0]).Where(s => actors.Contains(s.ActorId))
                .Select(s => new MountedRetainedIntentSlotRecord { ActorId = s.ActorId, Index = s.Index, Kind = s.Kind }).ToArray();
        }
    }

    // Exploration repair (review finding 2): suspended mounted intent is saved as its own bounded archive member
    // ("kmc-retained-intent"), separate from the strict schema-3 kmc-mounted-state member, so existing saves and
    // older readers are unaffected. It names only stable actor ids, the profile and the owner identity; it never
    // carries a command, a destination, an action or a turn. A record is accepted only for the exact campaign, area and
    // native game time of the save it was written into, so a stale member can never restore anything.
    //
    // Known issues S02 review R1, version 2: version 1 plus one "arrival" row (rider, mount and the six coordinates of
    // MountedRetainedArrivalPlacement) for each pair deferred at its arrival placement. It is written only when the
    // save holds such a pair; every other save keeps the exact version 1 text. Version 1 is read as before and its
    // rows never gain an arrival placement. A version 2 text that is malformed in any part (an arrival row for no
    // recorded pair or for one pair twice, a coordinate that is not a finite number, no arrival row at all) is refused
    // whole, like any other malformed record, and its pairs load dismounted. A reader older than version 2 refuses a
    // version 2 text by its header in the same way; it reads every version 1 text as it always did.
    public sealed class MountedRetainedIntentRecord
    {
        public const string Header = "KMC-RETAINED-INTENT 1";
        public const string ArrivalHeader = "KMC-RETAINED-INTENT 2";
        private const int MaximumCoordinateCharacters = 16;
        public const int MaximumPairs = 6;
        public const int MaximumCharacters = 4096;
        public const int MaximumSlots = 64;

        public string CampaignId { get; set; }
        public string AreaId { get; set; }
        public long GameTimeTicks { get; set; }
        public MountedRetainedIntentKind Kind { get; set; }
        public MountedRetainedIntentPairRecord[] Pairs { get; set; } = new MountedRetainedIntentPairRecord[0];
        public MountedRetainedIntentSlotRecord[] Slots { get; set; } = new MountedRetainedIntentSlotRecord[0];

        // Only the recorded pairs' own valid control rows, deduplicated, bounded.
        public static MountedRetainedIntentSlotRecord[] RecordableSlots(IEnumerable<MountedRetainedIntentSlotRecord> slots,
            IEnumerable<MountedRetainedIntentPairRecord> pairs)
        {
            var actors = new HashSet<string>((pairs ?? Enumerable.Empty<MountedRetainedIntentPairRecord>())
                .SelectMany(p => new[] { p.RiderId, p.MountId }), StringComparer.Ordinal);
            return (slots ?? Enumerable.Empty<MountedRetainedIntentSlotRecord>())
                .Where(s => s != null && actors.Contains(s.ActorId) && MountedControlSlotPolicy.IsValidSavedRow(s.Index, s.Kind))
                .GroupBy(s => s.ActorId + "|" + s.Index + "|" + s.Kind, StringComparer.Ordinal).Select(g => g.First())
                .Take(MaximumSlots)
                .Select(s => new MountedRetainedIntentSlotRecord { ActorId = s.ActorId, Index = s.Index, Kind = s.Kind })
                .ToArray();
        }

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
                    Generation = p.OwnerId != null && p.Generation > 0 ? p.Generation : 0,
                    // An arrival placement that is not a finite position is no evidence; the pair stays ordinary intent.
                    Arrival = p.Arrival != null && p.Arrival.IsFinite ? p.Arrival : null })
                .ToArray();
        }

        // Third owner review: the departure merge of live and retained control rows works per logical row
        // (MountedControlSlotPolicy.SavedRowKey: actor plus slot index, or actor plus the one placement record), never
        // per actor, so a live placement-only marker cannot hide a retained Mount/Dismount binding. Live rows come
        // first: a binding the player holds now wins its slot index, and RestorePersistentSlots places the public control
        // at the first saved index that is free. Nothing is invented: a retained placement marker without a slot row
        // (a deliberately removed control) stays a marker.
        public static MountedRetainedIntentSlotRecord[] MergeSlotRows(IEnumerable<MountedRetainedIntentSlotRecord> current,
            IEnumerable<MountedRetainedIntentSlotRecord> retained)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var merged = new List<MountedRetainedIntentSlotRecord>();
            foreach (var row in (current ?? Enumerable.Empty<MountedRetainedIntentSlotRecord>())
                .Concat(retained ?? Enumerable.Empty<MountedRetainedIntentSlotRecord>()))
            {
                if (row == null || row.ActorId == null) continue;
                var actor = Guid.TryParse(row.ActorId, out var parsed) ? parsed.ToString("N") : row.ActorId;
                if (keys.Add(MountedControlSlotPolicy.SavedRowKey(actor, row.Index, row.Kind)))
                    merged.Add(new MountedRetainedIntentSlotRecord { ActorId = row.ActorId, Index = row.Index, Kind = row.Kind });
            }
            return merged.ToArray();
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
            var result = Compose(true);
            if (result.Length > MaximumCharacters) throw new InvalidDataException("Retained mounted intent exceeds its bound.");
            return result;
        }

        // The text a save writes. The arrival rows are the one part a record can live without: if they alone would push
        // it past the member bound, the version 1 text is written (those pairs reload as ordinary retained intent)
        // rather than losing the record. Anything else over the bound is refused as before.
        public string EncodeForSave()
        {
            Validate();
            var result = Compose(true);
            if (result.Length > MaximumCharacters && Pairs.Any(p => p.Arrival != null)) result = Compose(false);
            if (result.Length > MaximumCharacters) throw new InvalidDataException("Retained mounted intent exceeds its bound.");
            return result;
        }

        private string Compose(bool withArrival)
        {
            var arrival = withArrival ? Pairs.Where(p => p.Arrival != null).ToArray() : new MountedRetainedIntentPairRecord[0];
            var text = new StringBuilder();
            text.Append(arrival.Length != 0 ? ArrivalHeader : Header).Append('\n');
            text.Append("campaign ").Append(CampaignId).Append('\n');
            text.Append("area ").Append(AreaId).Append('\n');
            text.Append("time ").Append(GameTimeTicks.ToString(CultureInfo.InvariantCulture)).Append('\n');
            text.Append("kind ").Append(Kind.ToString()).Append('\n');
            foreach (var pair in Pairs)
                text.Append("pair ").Append(pair.RiderId).Append(' ').Append(pair.MountId).Append(' ').Append(pair.ProfileId).Append(' ')
                    .Append(pair.OwnerId ?? "-").Append(' ').Append(pair.Generation.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var pair in arrival)
                text.Append("arrival ").Append(pair.RiderId).Append(' ').Append(pair.MountId)
                    .Append(' ').Append(Coordinate(pair.Arrival.RiderX)).Append(' ').Append(Coordinate(pair.Arrival.RiderY))
                    .Append(' ').Append(Coordinate(pair.Arrival.RiderZ)).Append(' ').Append(Coordinate(pair.Arrival.MountX))
                    .Append(' ').Append(Coordinate(pair.Arrival.MountY)).Append(' ').Append(Coordinate(pair.Arrival.MountZ)).Append('\n');
            foreach (var slot in Slots)
                text.Append("slot ").Append(slot.ActorId).Append(' ').Append(slot.Index.ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append(slot.Kind.ToString(CultureInfo.InvariantCulture)).Append('\n');
            return text.ToString();
        }

        public static MountedRetainedIntentRecord Decode(string text)
        {
            if (text == null || text.Length == 0 || text.Length > MaximumCharacters)
                throw new InvalidDataException("Retained mounted intent is empty or exceeds its bound.");
            var lines = text.Split('\n');
            var version2 = lines[0] == ArrivalHeader;
            if (lines.Length < 6 || lines[lines.Length - 1].Length != 0 || lines[0] != Header && !version2)
                throw new InvalidDataException("Retained mounted intent has an unknown header or shape.");
            var record = new MountedRetainedIntentRecord
            {
                CampaignId = Field(lines[1], "campaign"),
                AreaId = Field(lines[2], "area"),
                GameTimeTicks = ParseLong(Field(lines[3], "time")),
                Kind = ParseKind(Field(lines[4], "kind"))
            };
            var pairs = new List<MountedRetainedIntentPairRecord>();
            var slots = new List<MountedRetainedIntentSlotRecord>();
            var index = 5;
            for (; index < lines.Length - 1 && lines[index].StartsWith("pair ", StringComparison.Ordinal); index++)
            {
                var parts = Field(lines[index], "pair").Split(' ');
                if (parts.Length != 5) throw new InvalidDataException("Retained mounted pair has an unknown shape.");
                pairs.Add(new MountedRetainedIntentPairRecord { RiderId = parts[0], MountId = parts[1], ProfileId = parts[2],
                    OwnerId = parts[3] == "-" ? null : parts[3], Generation = ParseLong(parts[4]) });
            }
            // Version 2 only: the arrival rows follow the pair rows, each for exactly one recorded pair, at least one.
            if (version2)
            {
                var arrivals = 0;
                for (; index < lines.Length - 1 && lines[index].StartsWith("arrival ", StringComparison.Ordinal); index++, arrivals++)
                {
                    var parts = Field(lines[index], "arrival").Split(' ');
                    if (parts.Length != 8) throw new InvalidDataException("Retained mounted arrival placement has an unknown shape.");
                    var named = pairs.Where(p => string.Equals(p.RiderId, parts[0], StringComparison.Ordinal) &&
                        string.Equals(p.MountId, parts[1], StringComparison.Ordinal)).ToArray();
                    if (named.Length != 1 || named[0].Arrival != null)
                        throw new InvalidDataException("Retained mounted arrival placement names no recorded pair or one pair twice.");
                    named[0].Arrival = new MountedRetainedArrivalPlacement(record.AreaId, ParseCoordinate(parts[2]), ParseCoordinate(parts[3]),
                        ParseCoordinate(parts[4]), ParseCoordinate(parts[5]), ParseCoordinate(parts[6]), ParseCoordinate(parts[7]));
                }
                if (arrivals == 0) throw new InvalidDataException("Retained mounted intent version 2 holds no arrival placement.");
            }
            // Slot rows follow every pair row; anything else is an unknown shape.
            for (; index < lines.Length - 1; index++)
            {
                var parts = Field(lines[index], "slot").Split(' ');
                if (parts.Length != 3) throw new InvalidDataException("Retained mounted slot has an unknown shape.");
                slots.Add(new MountedRetainedIntentSlotRecord { ActorId = parts[0], Index = ParseInt(parts[1]), Kind = ParseInt(parts[2]) });
            }
            record.Pairs = pairs.ToArray();
            record.Slots = slots.ToArray();
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
                Require(pair.Arrival == null || pair.Arrival.IsFinite && pair.Arrival.ObservedIn(AreaId),
                    "Retained mounted arrival placement is not a finite position in the recorded area.");
            }
            Require(Slots != null && Slots.Length <= MaximumSlots, "Retained mounted slot count is invalid.");
            var slotKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in Slots)
                Require(slot != null && ActorId(slot.ActorId) && actors.Contains(Guid.Parse(slot.ActorId)) &&
                    MountedControlSlotPolicy.IsValidSavedRow(slot.Index, slot.Kind) &&
                    slotKeys.Add(Guid.Parse(slot.ActorId).ToString("N") + "|" + slot.Index + "|" + slot.Kind),
                    "Retained mounted slot is invalid, duplicated or belongs to no recorded pair.");
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

        private static int ParseInt(string value)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
                throw new InvalidDataException("Retained mounted intent number is invalid.");
            return parsed;
        }

        // Round-trip decimal text, culture-invariant: the exact float is read back. Reading accepts any finite decimal
        // number of bounded length, not only the text this writes (as the integer fields accept leading zeros); a
        // coordinate is only ever compared with a position, so its spelling carries no authority.
        private static string Coordinate(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static float ParseCoordinate(string value)
        {
            if (value.Length == 0 || value.Length > MaximumCoordinateCharacters ||
                !float.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture, out var parsed) || float.IsNaN(parsed) || float.IsInfinity(parsed))
                throw new InvalidDataException("Retained mounted arrival coordinate is invalid.");
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
