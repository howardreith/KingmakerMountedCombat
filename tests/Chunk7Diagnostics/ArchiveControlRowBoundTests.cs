using System;
using System.IO;
using System.Linq;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;

// The owned archive's control-row bound through the production DTO, validator and codec (MountedArchiveData.Validate,
// MountedArchiveCodec.Encode/Decode), compiled from the candidate's own sources by scripts/Test-CityArchiveRowBound.ps1.
// The rows are what NativeMountedControlService.CapturePersistentSlots writes: a live rider's slot row and placement
// record, and absent riders' arrangements from MountedControlSlotPolicy.AbsentArrangementRows. In-memory graphs only.
internal static class ArchiveControlRowBoundTests
{
    private const string Campaign = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa", Area = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static int passed, failed;

    private static string ActorId(int i) => new Guid(0x10000000 + i, 0x1111, 0x4111, 0x81, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11).ToString();

    private static MountedArchiveData Archive(SavedMountedSlot[] slots) => new MountedArchiveData
    {
        SchemaVersion = 3, CampaignId = Campaign, AreaId = Area, GameTimeTicks = 10000000,
        Policy = MountedSaveData.PairedPolicy, RulesId = MountedSaveData.Rules,
        Pairs = new SavedMountedPair[0], Participations = new SavedPairParticipation[0], Slots = slots
    };

    private static SavedMountedSlot[] AbsentRows(int actor, int index) =>
        MountedControlSlotPolicy.AbsentArrangementRows(index)
            .Select(row => new SavedMountedSlot { ActorId = ActorId(actor), Index = row.Key, Kind = row.Value }).ToArray();

    // One live rider (slot row at 9 plus its placement record) and absentRiders absent arrangements of two rows each.
    private static SavedMountedSlot[] Rows(int absentRiders)
    {
        var live = new[]
        {
            new SavedMountedSlot { ActorId = ActorId(0), Index = 9, Kind = MountedControlSlotPolicy.SavedMountState },
            new SavedMountedSlot { ActorId = ActorId(0), Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord }
        };
        return live.Concat(Enumerable.Range(1, absentRiders).SelectMany(i => AbsentRows(i, i % 120))).ToArray();
    }

    private static void True(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Check(string name, Action body)
    {
        try { body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error); }
    }

    public static int Main(string[] arguments)
    {
        if (arguments.Length != 1) throw new ArgumentException("Installed Unity dependency path is required.");
        // The installed Newtonsoft's Unity converters load UnityEngine from the game's Managed directory only.
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
            new System.Reflection.AssemblyName(request.Name).Name == "UnityEngine" ? System.Reflection.Assembly.LoadFrom(arguments[0]) : null;
        Check("an exact-fit archive of 128 control rows round-trips every actor id, index and kind", () =>
        {
            var rows = Rows(63);
            True(rows.Length == MountedControlSlotPolicy.MaximumArchiveRows && rows.Length == 128, "The fixture is not an exact fit.");
            var read = MountedArchiveCodec.Decode(MountedArchiveCodec.Encode(Archive(rows)));
            True(read.Kind == MountedSaveReadKind.Current && read.Data != null, "The exact-fit archive did not decode as current data: " + read.Feedback);
            var decoded = read.Data.Slots;
            True(decoded.Length == 128, "The decoded archive lost rows: " + decoded.Length);
            for (var i = 0; i < rows.Length; i++)
                True(Guid.Parse(decoded[i].ActorId) == Guid.Parse(rows[i].ActorId) && decoded[i].Index == rows[i].Index && decoded[i].Kind == rows[i].Kind,
                    "Row " + i + " changed: " + decoded[i].ActorId + "/" + decoded[i].Index + "/" + decoded[i].Kind);
            True(decoded.Count(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord) == 64 &&
                decoded.Count(r => r.Kind == MountedControlSlotPolicy.SavedMountState) == 64, "Kinds were not preserved.");
            Console.WriteLine("INFO exact-fit decoded rows=" + decoded.Length + "; first=" + decoded[0].ActorId + "/" + decoded[0].Index + "/" + decoded[0].Kind +
                "; last=" + decoded[127].ActorId + "/" + decoded[127].Index + "/" + decoded[127].Kind);
        });
        Check("127 rows plus an absent rider's two rows are refused by the production archive validation", () =>
        {
            var rows = Rows(62).Concat(new[] { new SavedMountedSlot { ActorId = ActorId(200), Index = 3, Kind = MountedControlSlotPolicy.SavedMountState } }).ToArray();
            True(rows.Length == 127, "The fixture does not start from 127 rows.");
            rows = rows.Concat(AbsentRows(201, 40)).ToArray();
            True(rows.Length == 129, "The overflow fixture is not 129 rows.");
            string error = null;
            try { MountedArchiveCodec.Encode(Archive(rows)); }
            catch (InvalidDataException refusal) { error = refusal.Message; }
            True(error != null, "The archive writer accepted 129 control rows.");
            True(error.Contains("Owned archive control slots are not bounded."), "The refusal is not the archive's bound validation: " + error);
            Console.WriteLine("INFO overflow refused: " + error);
        });
        Check("an absent rider that left the control off the bar is a placement record alone and round-trips", () =>
        {
            var rows = Rows(0).Concat(AbsentRows(7, MountedControlPlacementIntent.OffBar)).ToArray();
            var decoded = MountedArchiveCodec.Decode(MountedArchiveCodec.Encode(Archive(rows))).Data.Slots;
            var absent = decoded.Where(r => Guid.Parse(r.ActorId) == Guid.Parse(ActorId(7))).ToArray();
            True(absent.Length == 1 && absent[0].Kind == MountedControlSlotPolicy.SavedPlacementRecord && absent[0].Index == 0,
                "An off-bar arrangement did not round-trip as one placement record.");
        });
        foreach (var alias in new[] { NativeMountedControlKind.MountCompanion, NativeMountedControlKind.Dismount })
        {
            Check("a live older alias (" + alias + ") at 9 with the canonical fact inactive and no prior record emits one binding and one record through the production codec", () =>
            {
                // CaptureLiveSlots' public rows (MountedControlSlotPolicy.LivePublicRows) for a bar in production slot
                // order: the alias at 9 is callable (its shell is live), the canonical fact is inactive, and the placement
                // was recorded only by the alias recovery. Then CaptureAbsentIntent's rows for the recovered arrangement.
                var kinds = Bar(9, alias);
                var rows = Emit(ActorId(300), kinds, i => i == 9, placementRecorded: true, publicControlActive: false, absentIndex: 9);
                var decoded = RoundTrip(rows);
                ExpectOnePublicBinding(decoded, 9);
                // Control: the duplicate-key check is intact; the same choice written twice is refused.
                ExpectDuplicateRefused(rows.Concat(new[] { new SavedMountedSlot { ActorId = ActorId(300), Index = 9, Kind = (int)alias } }).ToArray());
            });
        }
        foreach (var stale in new[] { false, true })
        {
            Check("a mixed bar (live alias at 3, canonical at 9" + (stale ? " with stale data" : "") + ") saved first restores the canonical index 9 through the production codec", () =>
            {
                // Production slot order: the alias comes first. Its shell is callable; the canonical binding is callable
                // unless its AbilityData is stale (then classification carries 9 as the remembered slot).
                var kinds = Bar(3, NativeMountedControlKind.MountCompanion, 9, NativeMountedControlKind.MountState);
                var rows = Emit(ActorId(301), kinds, i => i == 3 || (i == 9 && !stale), placementRecorded: true, publicControlActive: true,
                    absentIndex: stale ? 9 : (int?)null);
                var decoded = RoundTrip(rows);
                ExpectOnePublicBinding(decoded, 9);
                True(!decoded.Any(r => r.Index == 3 && MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)), "The alias slot 3 was written as a binding.");
            });
        }
        Check("stale canonical data at 3 before current data at 9 saves the selected index 3 once, with the placement record, through the production codec", () =>
        {
            // The selected index is the first canonical binding (3); it is not callable, so the live writer skips it and
            // classification carries 3 as the remembered slot (the same index and callability in both writers).
            var kinds = Bar(3, NativeMountedControlKind.MountState, 9, NativeMountedControlKind.MountState);
            var rows = Emit(ActorId(302), kinds, i => i == 9, placementRecorded: true, publicControlActive: true, absentIndex: 3);
            var decoded = RoundTrip(rows);
            ExpectOnePublicBinding(decoded, 3);
        });
        Check("a remembered shortcut at 9 occupied by a callable older Mounted Charge saves the Charge at 9 and the shortcut off the bar through the production codec", () =>
        {
            // Interrupted intent at 9; the live writer emits the Charge row at 9 (and no public binding: the bar holds
            // none); the remembered writer sees index 9 taken and adds the placement record only.
            var actor = ActorId(303);
            var charge = new[] { new SavedMountedSlot { ActorId = actor, Index = 9, Kind = MountedControlSlotPolicy.SavedMountedCharge } };
            var rows = Emit(actor, Bar(9, NativeMountedControlKind.MountedCharge), i => false, placementRecorded: true,
                publicControlActive: false, absentIndex: 9, otherLiveRows: charge, occupied: true);
            var decoded = RoundTrip(rows);
            True(decoded.Count(r => r.Index == 9 && r.Kind == MountedControlSlotPolicy.SavedMountedCharge) == 1 &&
                !decoded.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) &&
                decoded.Count(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord) == 1 && decoded.Length == 2,
                "The archive did not hold one Charge row at 9, no mount shortcut and one placement record.");
            // Control: without the occupancy rule the kind-6 row at 9 would collide and the archive refuses it.
            ExpectDuplicateRefused(rows.Concat(new[] { new SavedMountedSlot { ActorId = actor, Index = 9, Kind = MountedControlSlotPolicy.SavedMountState } }).ToArray());
        });
        Console.WriteLine("TOTAL PASS=" + passed + " FAIL=" + failed);
        return failed == 0 ? 0 : 1;
    }

    private static NativeMountedControlKind[] Bar(int index, NativeMountedControlKind kind, int secondIndex = -1,
        NativeMountedControlKind secondKind = NativeMountedControlKind.None)
    {
        var kinds = new NativeMountedControlKind[140];
        kinds[index] = kind;
        if (secondIndex >= 0) kinds[secondIndex] = secondKind;
        return kinds;
    }

    // NativeMountedControlService.CapturePersistentSlots for one rider: the live public rows, then (absentIndex: the
    // remembered arrangement classification recorded) the remembered rows composed after them.
    // otherLiveRows: the live writer's other KMC rows for this rider (for example an older Mounted Charge awaiting
    // migration); occupied: the remembered slot holds another binding on the bar.
    private static SavedMountedSlot[] Emit(string actor, NativeMountedControlKind[] kinds, Func<int, bool> callable, bool placementRecorded,
        bool publicControlActive, int? absentIndex, SavedMountedSlot[] otherLiveRows = null, bool occupied = false)
    {
        var rows = (otherLiveRows ?? new SavedMountedSlot[0]).ToList();
        rows.AddRange(MountedControlSlotPolicy.LivePublicRows(kinds, callable, placementRecorded, publicControlActive)
            .Select(row => new SavedMountedSlot { ActorId = actor, Index = row.Key, Kind = row.Value }));
        var writtenBinding = rows.Any(r => r.Kind == MountedControlSlotPolicy.SavedMountState);
        if (absentIndex.HasValue && !writtenBinding)
            foreach (var row in MountedControlSlotPolicy.AbsentRowsAfterLive(absentIndex.Value,
                rows.Select(r => new System.Collections.Generic.KeyValuePair<int, int>(r.Index, r.Kind)).ToArray(), occupied))
                rows.Add(new SavedMountedSlot { ActorId = actor, Index = row.Key, Kind = row.Value });
        return rows.ToArray();
    }

    private static SavedMountedSlot[] RoundTrip(SavedMountedSlot[] rows)
    {
        var read = MountedArchiveCodec.Decode(MountedArchiveCodec.Encode(Archive(rows)));
        True(read.Kind == MountedSaveReadKind.Current && read.Data != null, "The archive did not round-trip: " + read.Feedback);
        return read.Data.Slots;
    }

    // RestorePersistentSlots seeds the first public-binding row per actor: it must be the only one, at the expected index.
    private static void ExpectOnePublicBinding(SavedMountedSlot[] decoded, int index)
    {
        var bindings = decoded.Where(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).ToArray();
        True(bindings.Length == 1 && bindings[0].Index == index &&
            MountedControlSlotPolicy.RestoreSavedSlot(bindings[0].Kind) == MountedControlSlotRepair.MountState,
            "The archive does not hold exactly one restorable public binding at " + index + ".");
        True(decoded.Count(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord) == 1, "The archive does not hold exactly one placement record.");
    }

    private static void ExpectDuplicateRefused(SavedMountedSlot[] rows)
    {
        string error = null;
        try { MountedArchiveCodec.Encode(Archive(rows)); }
        catch (InvalidDataException refusal) { error = refusal.Message; }
        True(error != null && error.Contains("Invalid or duplicate owned archive control slot."), "The duplicate-key check did not refuse a repeated index: " + error);
    }
}
