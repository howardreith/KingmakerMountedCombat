using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;

// The owned archive's control-row bound through the production DTO, validator and codec (MountedArchiveData.Validate,
// MountedArchiveCodec.Encode/Decode), compiled from the candidate's own sources by scripts/Test-CityArchiveRowBound.ps1.
// The public rows are written by the production toolbar lifecycle (MountedToolbarLifecycle.CaptureRows, the rows
// NativeMountedControlService.CapturePersistentSlots(includeAbsentIntent: true) archives) over a minimal labeled fake of
// reserve riders' bars. In-memory graphs only.
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

    // A reserve rider's bar (labeled fake): 140 slots, its pet link, nothing leased.
    private sealed class Rider
    {
        internal string Id;
        internal MountedToolbarSlot[] Bar = new MountedToolbarSlot[140];
        internal string PetLink = "pet";
    }

    private sealed class Host : IMountedToolbarHost<Rider>
    {
        internal readonly object World = new object();
        internal readonly List<Rider> Reserve = new List<Rider>();
        public object CurrentWorld => World;
        public IList<Rider> CurrentRiders() => new List<Rider>();
        public IList<Rider> ReserveOwners() => Reserve;
        public Rider PersistentPet(Rider owner) => null;
        public bool IsPet(Rider unit) => false;
        public string ActorId(Rider unit) => unit.Id;
        public string PetLink(Rider unit) => unit.PetLink;
        public bool OnPlayerRoster(Rider unit) => Reserve.Contains(unit);
        public Rider ResolveCharacter(string actorId) => Reserve.SingleOrDefault(unit => unit.Id == actorId);
        public bool ControlDesired(Rider unit) => false;
        public bool ControlLive(Rider unit) => false;
        public void PrepareRider(Rider unit) { }
        public int SlotCount(Rider unit) => unit.Bar.Length;
        public MountedToolbarSlot ReadSlot(Rider unit, int index) => unit.Bar[index];
        public object SlotIdentity(Rider unit, int index) => null;
        public bool IsNativeEmptySlot(Rider unit, int index) => false;
        public bool HoldsLiveBinding(Rider unit, int index) => false;
        public void BindControl(Rider unit, int index) { throw new InvalidOperationException("No control is live in this fixture."); }
        public void ClearSlot(Rider unit, int index) => unit.Bar[index] = MountedToolbarSlot.Empty;
        public void BarChanged(Rider unit) { }
        public void SanitizeOtherBindings(Rider unit, MountedToolbarBoundary boundary) { }
        public void ClearManagedAutoUse(Rider unit, MountedToolbarBoundary boundary) { }
        public void RemoveManagedFacts(Rider unit) { }
        public bool InitialPlacementAllowed(Rider unit) => false;
        public int SelectInitialPlacementSlot(Rider unit) => -1;
    }

    private static SavedMountedSlot[] Capture(Host host, MountedToolbarLifecycle<Rider> lifecycle) =>
        lifecycle.CaptureRows(true).Select(row => new SavedMountedSlot { ActorId = row.ActorId, Index = row.Index, Kind = row.Kind }).ToArray();

    // count reserve riders with one public binding each at index i % 120 (a slot row and a placement record each), plus
    // offBar riders whose loaded arrangement is off the bar (a placement record alone).
    private static SavedMountedSlot[] Rows(int count, int offBar = 0)
    {
        var host = new Host();
        var lifecycle = new MountedToolbarLifecycle<Rider>(host);
        lifecycle.Enable();
        for (var i = 1; i <= count + offBar; i++)
        {
            var rider = new Rider { Id = ActorId(i) };
            if (i <= count) rider.Bar[i % 120] = MountedToolbarSlot.PublicControl;
            host.Reserve.Add(rider);
        }
        lifecycle.SeedFromRows(Enumerable.Range(count + 1, offBar).Select(i => new MountedToolbarRow
            { ActorId = ActorId(i), Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord }), true);
        // The update observes and sweeps the reserve bars (every slot pending).
        lifecycle.Reconcile();
        return Capture(host, lifecycle);
    }

    private static SavedMountedSlot[] One(Action<Rider> bar, Action<Host, MountedToolbarLifecycle<Rider>, Rider> between = null)
    {
        var host = new Host();
        var lifecycle = new MountedToolbarLifecycle<Rider>(host);
        lifecycle.Enable();
        var rider = new Rider { Id = ActorId(300) };
        bar(rider);
        host.Reserve.Add(rider);
        between?.Invoke(host, lifecycle, rider);
        return Capture(host, lifecycle);
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
        Check("an exact-fit archive of 128 control rows written by the lifecycle round-trips every actor id, index and kind", () =>
        {
            var rows = Rows(64);
            True(rows.Length == MountedControlSlotPolicy.MaximumArchiveRows && rows.Length == 128, "The fixture is not an exact fit: " + rows.Length);
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
        Check("129 required rows from the lifecycle are written, then refused by the production archive validation", () =>
        {
            var rows = Rows(64, offBar: 1);
            True(rows.Length == 129, "The overflow fixture is not 129 rows: " + rows.Length);
            string error = null;
            try { MountedArchiveCodec.Encode(Archive(rows)); }
            catch (InvalidDataException refusal) { error = refusal.Message; }
            True(error != null, "The archive writer accepted 129 control rows.");
            True(error.Contains("Owned archive control slots are not bounded."), "The refusal is not the archive's bound validation: " + error);
            Console.WriteLine("INFO overflow refused: " + error);
        });
        Check("a rider that left the control off the bar is a placement record alone and round-trips", () =>
        {
            var rows = Rows(1, offBar: 1);
            var decoded = MountedArchiveCodec.Decode(MountedArchiveCodec.Encode(Archive(rows))).Data.Slots;
            var absent = decoded.Where(r => Guid.Parse(r.ActorId) == Guid.Parse(ActorId(2))).ToArray();
            True(absent.Length == 1 && absent[0].Kind == MountedControlSlotPolicy.SavedPlacementRecord && absent[0].Index == 0,
                "An off-bar arrangement did not round-trip as one placement record.");
        });
        Check("an older alias at 9 on a bar with no canonical binding writes one binding and one record through the production codec", () =>
        {
            var rows = One(rider => rider.Bar[9] = MountedToolbarSlot.LegacyAlias);
            var decoded = RoundTrip(rows);
            ExpectPublicBindings(decoded, 9);
            // Control: the duplicate-key check is intact; the same slot written twice is refused.
            ExpectDuplicateRefused(rows.Concat(new[] { new SavedMountedSlot { ActorId = ActorId(300), Index = 9, Kind = MountedControlSlotPolicy.SavedDismount } }).ToArray());
        });
        Check("an alias at 3 beside a canonical binding at 9 writes only the canonical index 9 through the production codec", () =>
        {
            var rows = One(rider => { rider.Bar[3] = MountedToolbarSlot.LegacyAlias; rider.Bar[9] = MountedToolbarSlot.PublicControl; });
            var decoded = RoundTrip(rows);
            ExpectPublicBindings(decoded, 9);
            True(!decoded.Any(r => r.Index == 3 && MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)), "The alias slot 3 was written as a binding.");
        });
        Check("the player's two canonical bindings (3 and 9) write two slot rows and one record with unique archive keys", () =>
        {
            var rows = One(rider => { rider.Bar[3] = MountedToolbarSlot.PublicControl; rider.Bar[9] = MountedToolbarSlot.PublicControl; });
            var decoded = RoundTrip(rows);
            ExpectPublicBindings(decoded, 3, 9);
        });
        Check("a pending shortcut at 9 occupied by an older Mounted Charge is saved off the bar beside the Charge row", () =>
        {
            var actor = ActorId(300);
            var rows = One(rider => rider.Bar[9] = MountedToolbarSlot.PublicControl, (host, lifecycle, rider) =>
            {
                lifecycle.BeginSave();
                True(rider.Bar[9] == MountedToolbarSlot.Empty, "The save scope did not clear the binding.");
                rider.Bar[9] = MountedToolbarSlot.OtherManaged;
            });
            // The adapter's own live row for the Charge at 9 (CaptureOtherLiveSlots), then the lifecycle's rows.
            var charge = new[] { new SavedMountedSlot { ActorId = actor, Index = 9, Kind = MountedControlSlotPolicy.SavedMountedCharge } };
            var all = charge.Concat(rows).ToArray();
            var decoded = RoundTrip(all);
            True(decoded.Count(r => r.Index == 9 && r.Kind == MountedControlSlotPolicy.SavedMountedCharge) == 1 &&
                !decoded.Any(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)) &&
                decoded.Count(r => r.Kind == MountedControlSlotPolicy.SavedPlacementRecord) == 1 && decoded.Length == 2,
                "The archive did not hold one Charge row at 9, no mount shortcut and one placement record.");
            // Control: without the occupancy rule a kind-6 row at 9 would collide and the archive refuses it.
            ExpectDuplicateRefused(all.Concat(new[] { new SavedMountedSlot { ActorId = actor, Index = 9, Kind = MountedControlSlotPolicy.SavedMountState } }).ToArray());
        });
        Console.WriteLine("TOTAL PASS=" + passed + " FAIL=" + failed);
        return failed == 0 ? 0 : 1;
    }

    private static SavedMountedSlot[] RoundTrip(SavedMountedSlot[] rows)
    {
        var read = MountedArchiveCodec.Decode(MountedArchiveCodec.Encode(Archive(rows)));
        True(read.Kind == MountedSaveReadKind.Current && read.Data != null, "The archive did not round-trip: " + read.Feedback);
        return read.Data.Slots;
    }

    // Exactly these public-binding rows (one per slot) and exactly one placement record.
    private static void ExpectPublicBindings(SavedMountedSlot[] decoded, params int[] indices)
    {
        var bindings = decoded.Where(r => MountedControlSlotPolicy.IsPublicBindingRow(r.Kind)).Select(r => r.Index).OrderBy(i => i).ToArray();
        True(bindings.SequenceEqual(indices), "The archive's public bindings are [" + string.Join(",", bindings) + "], expected [" + string.Join(",", indices) + "].");
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
