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
        foreach (var aliasKind in new[] { MountedControlSlotPolicy.SavedMountCompanion, MountedControlSlotPolicy.SavedDismount })
        {
            Check("save-first capture of a live older alias (kind " + aliasKind + ") at 9 passes the production codec with one public binding at 9", () =>
            {
                // CaptureLiveSlots: the live alias row and the placement record; then CaptureAbsentIntent's rows for the
                // same actor's recovered arrangement (interrupted at 9) come from MountedControlSlotPolicy.AbsentRowsAfterLive.
                var actor = ActorId(300);
                var rows = new[]
                {
                    new SavedMountedSlot { ActorId = actor, Index = 9, Kind = aliasKind },
                    new SavedMountedSlot { ActorId = actor, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord }
                }.ToList();
                foreach (var row in MountedControlSlotPolicy.AbsentRowsAfterLive(9, rows.Select(r => r.Kind)))
                    rows.Add(new SavedMountedSlot { ActorId = actor, Index = row.Key, Kind = row.Value });
                var read = MountedArchiveCodec.Decode(MountedArchiveCodec.Encode(Archive(rows.ToArray())));
                True(read.Kind == MountedSaveReadKind.Current && read.Data != null, "The archive did not round-trip: " + read.Feedback);
                var decoded = read.Data.Slots;
                True(decoded.Count(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) == 1 &&
                    decoded.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Index == 9 &&
                    MountedControlSlotPolicy.RestoreSavedSlot(decoded.Single(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Kind) == MountedControlSlotRepair.MountState &&
                    decoded.Count(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord) == 1 && decoded.Length == 2,
                    "The archive does not hold exactly one restorable public binding at 9 and one placement record.");
                // Control: the duplicate-key check is intact; the same choice written twice is refused.
                var twice = rows.Concat(new[] { new SavedMountedSlot { ActorId = actor, Index = 9, Kind = MountedControlSlotPolicy.SavedMountState } }).ToArray();
                string error = null;
                try { MountedArchiveCodec.Encode(Archive(twice)); }
                catch (InvalidDataException refusal) { error = refusal.Message; }
                True(error != null && error.Contains("Invalid or duplicate owned archive control slot."), "The duplicate-key check did not refuse a repeated index: " + error);
            });
        }
        Console.WriteLine("TOTAL PASS=" + passed + " FAIL=" + failed);
        return failed == 0 ? 0 : 1;
    }
}
