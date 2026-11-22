using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Toolbar persistence of the public Mount / Dismount control, exercised through the production lifecycle component
    // (MountedToolbarLifecycle, MountedControlPlacementIntent). Everything below the component is a thin labeled fake of
    // the game: Game implements IMountedToolbarHost (bars and slots, the public fact and its AbilityData generation, roster
    // membership of party, reserve, detached, ex-companions and pets, the player world identity), and the archive rows are
    // the component's own MountedToolbarRow output. No toolbar decision is made in this file.
    //
    // Boundary drivers call the component in the order NativeMountedControlService and MountedPersistenceService call it:
    // - Update: NativeMountedControlService.Update -> toolbar.Reconcile (Housekeeping first).
    // - Save: the owned archive header (CaptureRows(includeAbsent: true)), then the save scope (BeginSave), native
    //   serialization (a snapshot of every bar here), then EndSaveSerializationScope (Update, then the non-public leases).
    // - Selected load: BeginSelectedLoad(scope); native loading replaces the player world and every unit object (same actor
    //   ids, bars from the snapshot); RestorePersistentSlots(rows, true) -> SeedFromRows then Update.
    // - Area: SuspendArea, then Update on resume.
    // - Native GetSlot / CleanupBadSlots: BeginNativeSlotOperation, native's own write (empty bound to the unit, or a rebind
    //   to a live fact of the same blueprint), EndNativeSlotOperation (absent when native threw).
    // Not native evidence.
    internal static class MountedControlPlacementIntentTests
    {
        private const int BarSize = 140;

        internal static void Register(TestRunner runner)
        {
            runner.Run("toolbar: chosen nondefault slot survives repeated city visits, mounted and dismounted arrival and interior transitions", SurvivesTransitions);
            runner.Run("toolbar: save and load inside and outside the city keep the chosen slot (one slot row, one record)", SaveLoadInsideAndOutsideCity);
            runner.Run("toolbar: a deliberate removal survives a reserve save and load as a record only and is never re-added", RemovalSurvivesReserveSaveLoad);
            runner.Run("toolbar: D2 the same player edits give the same bar and archive whether saving or updating first", SameEditsEitherOrder);
            runner.Run("toolbar: D6 native invalidation returns the control once; a deliberate removal stays a removal", NativeInvalidationVersusRemoval);
            runner.Run("toolbar: D6 native operations that throw, query or come from another world never turn a removal into intent", NativeOperationEdges);
            runner.Run("toolbar: a save header during an interruption keeps the pending slot; an occupied slot is given up everywhere", HeaderDuringInterruption);
            runner.Run("toolbar: D1 a move or removal right before a departure, save scope or area unload is what is kept", IntentBeforeCleanup);
            runner.Run("toolbar: companion loss retires the arrangement and its slots; the same pet unresolved keeps it", CompanionLoss);
            runner.Run("toolbar: a companion replaced during a save scope gets no restored slot", CompanionChangeDuringSave);
            runner.Run("toolbar: in-world replays (area transfer, retained remount) never resurrect or move an observed arrangement", InWorldReplay);
            runner.Run("toolbar: D4/D5 an abandoned load keeps the outgoing world's state; a replaced world authors nothing", LoadLifetime);
            runner.Run("toolbar: D3 repeated scans, saves and transitions never move, duplicate or resurrect the shortcut", RepeatedBoundariesIdempotent);
            runner.Run("toolbar: known copies yield to the recoverable choice, a fresh placement wins, one shortcut is kept", KnownCopies);
            runner.Run("toolbar: a move past a surviving copy supersedes the pending slot, before a save or a rebind too", MoveSupersedesPending);
            runner.Run("toolbar: older Mount / Dismount aliases collapse to one control and never displace a canonical choice", LegacyAliases);
            runner.Run("toolbar: reserve bars of older saves are observed, cleared and carried; fresh reserve placements win", ReserveBars);
            runner.Run("toolbar: occupied slots, another rider and unrelated slots are never overwritten", OccupiedAndIndependent);
            runner.Run("toolbar: removing the shortcut right after a save scope returns it stays removed through save and load", RemovalAfterSaveRelease);
            runner.Run("toolbar: the one automatic placement runs once, never beside a manual binding, and records its companion", InitialPlacement);
            runner.Run("toolbar: disable, disabled saves and world changes sanitize without recording or retaining anything", DisableAndLifetime);
            runner.Run("toolbar: AC-R26-F4 save and disable clear a reserve owner's pet's stale Mount Primary slot and auto-use", AcR26F4ReservePetSanitized);
            runner.Run("toolbar: AC-R22-F3 the first reserve scan keeps a fresh placement made after native emptied the only shortcut", AcR22F3FreshPlacementAfterNativeEmpty);
            runner.Run("toolbar: AC-R25-F3 a scanned reserve rider establishes a fresh shortcut after its companion is replaced", AcR25F3FreshShortcutAfterReplacement);
            runner.Run("toolbar: AC-R26-F1 canceling a load keeps the next save's classification of an edited reserve bar", AcR26F1CanceledLoadKeepsEdit);
            runner.Run("toolbar: AC-R26-F2 an unfenced return to the main menu releases every record and the world", AcR26F2MainMenuReleases);
            runner.Run("toolbar: AC-R26-F3 a rider that rejoined before save or disable loses a stale KMC auto-use selection", AcR26F3RejoinedRiderAutoUse);
            runner.Run("toolbar: disable before or after partial registration clears only identifiable KMC slots; items and abilities keep their objects", UnregisteredBlueprints);
            runner.Run("toolbar: review R1-F2 a missing player during a canceled load keeps a travelling rider's live shortcut", MissingPlayerDuringCanceledLoad);
            runner.Run("toolbar: review R1-F3 a native rebind of a pending slot is live; removing it before the update leaves a record only", NativeRebindOfPendingSlot);
            runner.Run("toolbar: review R1-F5 a reserve or detached owner in pair custody still has its pet sanitized at save and disable", PairCustodyOwnerPetSanitized);
            runner.Run("toolbar: review R1-F6 an older Mounted Charge becomes stock Charge at its index whether saving or updating first", LegacyChargeEitherOrder);
            runner.Run("toolbar: review R1-F7 a departing unit loses its stale KMC auto-use before it is released", DepartureClearsAutoUse);
            runner.Run("toolbar: review R1-F9 an unavailable bar defers the pending restore instead of consuming it", UnavailableBarDefersRestore);
            runner.Run("toolbar: review R1-F10 a former instance never retires its reserve replacement's arrangement", FormerInstanceKeepsReplacement);
            runner.Run("toolbar: review R1-F11 archive rows naming a pet own no arrangement and are not saved again", PetRowsOwnNothing);
            runner.Run("toolbar: review R1-F11 a pet admitted before it resolved is purged once it is a current pet", LateResolvingPetPurged);
            runner.Run("toolbar: review R2-F1 an occupied canonical archive row yields to a usable older row", MixedArchiveRows);
            runner.Run("toolbar: review R2-F2 restored rows never overwrite the incoming world's own earlier observation", IncomingObservationBeforeRestore);
            runner.Run("toolbar: review R2-F3 a companion replaced while the bar is unavailable retires the old arrangement", CompanionReplacedWhileBarHidden);
            runner.Run("toolbar: review R2-F4 a thrown native write survives a missing-player update during an open load", FencedThrownNativeWrite);
            runner.Run("toolbar: review R2-F5 a thrown cleanup keeps a rebound copy a copy and a later placement the player's", ThrownCleanupEdges);
            runner.Run("toolbar: review R2-F6 a reserve rider's known copies are released when cleared or unreachable", ReserveCopiesReleased);
        }

        // ---- thin fakes of the game -------------------------------------------------------------------------------

        private sealed class World
        {
            internal readonly string Name;
            internal World(string name) { Name = name; }
            public override string ToString() => Name;
        }

        private abstract class Slot { }

        // What native GetSlot / CleanupBadSlots and KMC write: an empty slot bound to its unit. The player's drag-off writes
        // null instead (contract 'the action bar drag-off removal writes null').
        private sealed class EmptySlot : Slot
        {
            internal readonly Unit Owner;
            internal EmptySlot(Unit owner) { Owner = owner; }
        }

        // Kind: "Control" (the public control), "Mount" / "Dismount" (older aliases), "Charge" (an older Mounted Charge),
        // "RiderPrimary" / "MountPrimary" (retired KMC helpers), anything else an ordinary ability or item.
        // Data: the AbilityData generation of the fact it was bound to.
        private sealed class AbilitySlot : Slot
        {
            internal readonly string Kind;
            internal readonly int Data;
            // The KMC kinds share one blueprint object each; any other ability has its own.
            internal readonly object Blueprint;
            internal AbilitySlot(string kind, int data)
            {
                Kind = kind; Data = data;
                object shared;
                Blueprint = KindBlueprints.TryGetValue(kind, out shared) ? shared : new object();
            }
            public override string ToString() => Kind + "#" + Data;
        }

        // A non-ability slot (an item on the bar): no ability blueprint at all.
        private sealed class ItemSlot : Slot
        {
            internal readonly string Name;
            internal ItemSlot(string name) { Name = name; }
        }

        private static readonly Dictionary<string, object> KindBlueprints = new Dictionary<string, object>
        {
            { "Control", new object() }, { "Mount", new object() }, { "Dismount", new object() },
            { "Charge", new object() }, { "RiderPrimary", new object() }, { "MountPrimary", new object() }
        };

        private sealed class Unit
        {
            internal readonly string Id;
            internal readonly World World;
            internal Slot[] Bar = new Slot[BarSize];
            internal bool HasBar = true;
            internal bool IsPet;
            internal Unit Master, Pet;
            internal string PetLink;
            // ShouldLease(MountState) for this unit.
            internal bool Desired;
            // The leased public fact's AbilityData generation (0: none) and whether it is active.
            internal int ControlFact;
            internal bool FactActive;
            // An older Mount shell fact is live (an alias binding is callable).
            internal bool AliasLive;
            internal int OtherFacts;
            internal string AutoUse;
            internal bool HasStockCharge;
            internal Unit(string id, World world) { Id = id; World = world; }
            public override string ToString() => Id + "@" + World;
        }

        private sealed class Lease
        {
            internal Unit Unit;
            internal int Index;
            internal string Kind;
            internal Slot Placeholder;
        }

        private sealed class SaveFile
        {
            internal IList<MountedToolbarRow> Rows;
            internal readonly Dictionary<string, Slot[]> Bars = new Dictionary<string, Slot[]>();
            internal readonly Dictionary<string, string> AutoUse = new Dictionary<string, string>();
            internal List<string> Party, Remote, Detached;
            internal readonly Dictionary<string, string> PetOf = new Dictionary<string, string>();
        }

        private sealed class Game : IMountedToolbarHost<Unit>
        {
            internal World Current = new World("w1");
            internal readonly List<Unit> Party = new List<Unit>(), Remote = new List<Unit>(), Detached = new List<Unit>(),
                Ex = new List<Unit>(), PairMembers = new List<Unit>();
            internal bool Autofill = true, Settled = true;
            internal int Generation = 100, Binds, PrepareCalls;
            internal readonly List<Lease> Leases = new List<Lease>();
            internal readonly MountedToolbarLifecycle<Unit> Toolbar;

            internal Game()
            {
                Toolbar = new MountedToolbarLifecycle<Unit>(this);
                Toolbar.Enable();
            }

            public object CurrentWorld => Current;

            public IList<Unit> CurrentRiders()
            {
                var result = new List<Unit>();
                if (Current == null) return result;
                foreach (var unit in Party) { AddUnique(result, unit); if (unit.Pet != null) AddUnique(result, unit.Pet); }
                foreach (var unit in PairMembers) AddUnique(result, unit);
                return result;
            }

            public IList<Unit> ReserveOwners()
            {
                var riders = CurrentRiders();
                return Remote.Concat(Detached).Where(unit => !riders.Contains(unit) && OnPlayerRoster(unit)).Distinct().ToList();
            }

            public Unit PersistentPet(Unit owner) => owner?.Pet != null && owner.Pet.IsPet && owner.Pet.Master == owner ? owner.Pet : null;
            public bool IsPet(Unit unit) => unit.IsPet;
            public string ActorId(Unit unit) => unit.Id;
            public string PetLink(Unit unit) => unit.PetLink;

            public bool OnPlayerRoster(Unit unit) => unit != null && Current != null && unit.World == Current && !Ex.Contains(unit) &&
                (Party.Contains(unit) || Remote.Contains(unit) || Detached.Contains(unit));

            // Player.AllCharacters: the party, reserve and detached characters and their pets.
            public Unit ResolveCharacter(string actorId)
            {
                var owners = Party.Concat(Remote).Concat(Detached).ToList();
                var matches = owners.Concat(owners.Where(unit => unit.Pet != null).Select(unit => unit.Pet))
                    .Where(unit => unit.World == Current && unit.Id == actorId).Distinct().ToList();
                return matches.Count == 1 ? matches[0] : null;
            }

            public bool ControlDesired(Unit unit) => unit.Desired;
            public bool ControlLive(Unit unit) => unit.ControlFact != 0 && unit.FactActive;

            // The adapter's fact leasing: a missing or inactive desired fact is replaced (new AbilityData), an undesired one
            // removed.
            public void PrepareRider(Unit unit)
            {
                PrepareCalls++;
                if (!unit.Desired) { unit.ControlFact = 0; unit.FactActive = false; return; }
                if (unit.ControlFact != 0 && unit.FactActive) return;
                unit.ControlFact = ++Generation;
                unit.FactActive = true;
            }

            public int SlotCount(Unit unit) => unit.HasBar ? unit.Bar.Length : 0;

            // The adapter's classification: the slot's blueprint against the registered KMC blueprints through the production
            // MountedControlSlotPolicy.ResolveExactKind (an unregistered blueprint is null and identifies nothing).
            internal readonly HashSet<string> Registered = new HashSet<string>(KindBlueprints.Keys);

            private object Registration(string kind) => Registered.Contains(kind) ? KindBlueprints[kind] : null;

            public MountedToolbarSlot ReadSlot(Unit unit, int index)
            {
                var any = unit.Bar[index];
                if (any == null || any is EmptySlot) return MountedToolbarSlot.Empty;
                // A non-ability slot (an item) forwards a null blueprint, as the adapter's (slot as
                // MechanicActionBarSlotAbility)?.Ability?.Blueprint does.
                var blueprint = (any as AbilitySlot)?.Blueprint;
                var kind = MountedControlSlotPolicy.ResolveExactKind(blueprint, Registration("Mount"), Registration("Dismount"),
                    Registration("RiderPrimary"), Registration("MountPrimary"), Registration("Charge"), Registration("Control"));
                if (kind == NativeMountedControlKind.None) return MountedToolbarSlot.Ordinary;
                if (kind == NativeMountedControlKind.MountState) return MountedToolbarSlot.PublicControl;
                return MountedControlSlotPolicy.IsPublicControlKind(kind) ? MountedToolbarSlot.LegacyAlias : MountedToolbarSlot.OtherManaged;
            }

            public object SlotIdentity(Unit unit, int index) => unit.Bar[index];

            public bool IsNativeEmptySlot(Unit unit, int index)
            {
                var empty = unit.Bar[index] as EmptySlot;
                return empty != null && empty.Owner == unit;
            }

            public bool HoldsLiveBinding(Unit unit, int index)
            {
                var slot = unit.Bar[index] as AbilitySlot;
                if (slot == null) return false;
                if (slot.Kind == "Control") return ControlLive(unit) && slot.Data == unit.ControlFact;
                return (slot.Kind == "Mount" || slot.Kind == "Dismount") && unit.AliasLive;
            }

            public void BindControl(Unit unit, int index)
            {
                if (!ControlLive(unit)) throw new InvalidOperationException("The fake refuses a public binding without a live fact.");
                unit.Bar[index] = new AbilitySlot("Control", unit.ControlFact);
                Binds++;
            }

            public void ClearSlot(Unit unit, int index) => unit.Bar[index] = new EmptySlot(unit);
            public void BarChanged(Unit unit) { }

            public void SanitizeOtherBindings(Unit unit, MountedToolbarBoundary boundary)
            {
                for (var index = 0; index < unit.Bar.Length; index++)
                {
                    if (ReadSlot(unit, index) != MountedToolbarSlot.OtherManaged) continue;
                    var kind = ((AbilitySlot)unit.Bar[index]).Kind;
                    Slot replacement = new EmptySlot(unit);
                    // As the adapter: an older Mounted Charge becomes the stock Charge first at every boundary (when available
                    // and not already on the bar); a save leases the rest back, other boundaries clear them.
                    if (kind == "Charge" && unit.HasStockCharge && !unit.Bar.OfType<AbilitySlot>().Any(slot => slot.Kind == "StockCharge"))
                        replacement = new AbilitySlot("StockCharge", 1);
                    else if (boundary == MountedToolbarBoundary.Save) Leases.Add(new Lease { Unit = unit, Index = index, Kind = kind, Placeholder = replacement });
                    unit.Bar[index] = replacement;
                }
            }

            public void ClearManagedAutoUse(Unit unit, MountedToolbarBoundary boundary)
            {
                if (unit.AutoUse == null || !unit.AutoUse.StartsWith("KMC_", StringComparison.Ordinal)) return;
                if (boundary == MountedToolbarBoundary.Update && unit.AutoUse != "KMC_MountState" && unit.AutoUse != "KMC_Mount" &&
                    unit.AutoUse != "KMC_Dismount" && unit.AutoUse != "KMC_Charge") return;
                unit.AutoUse = null;
            }

            public void RemoveManagedFacts(Unit unit)
            {
                unit.ControlFact = 0; unit.FactActive = false; unit.AliasLive = false; unit.OtherFacts = 0;
            }

            public bool InitialPlacementAllowed(Unit unit) => Autofill && Settled && ControlLive(unit);

            public int SelectInitialPlacementSlot(Unit unit)
            {
                for (var index = 0; index < 14; index++)
                    if (unit.Bar[index] == null || unit.Bar[index] is EmptySlot) return index;
                return -1;
            }

            // EndSaveSerializationScope's lease restore: a placeholder still in place goes back when its fact is live.
            internal void RestoreLeases()
            {
                foreach (var lease in Leases)
                    if (ReferenceEquals(lease.Unit.Bar[lease.Index], lease.Placeholder) && lease.Unit.OtherFacts > 0)
                        lease.Unit.Bar[lease.Index] = new AbilitySlot(lease.Kind, 1);
                Leases.Clear();
            }
        }

        private static void AddUnique(List<Unit> list, Unit unit)
        {
            if (unit != null && !list.Contains(unit)) list.Add(unit);
        }

        // ---- fixtures and drivers ---------------------------------------------------------------------------------

        // A party rider with its persistent horse (IsPet, master = rider, pet link set).
        private static Unit Rider(Game game, string id, bool inParty = true, string pet = null)
        {
            var rider = new Unit(id, game.Current) { Desired = true, PetLink = pet ?? "pet-" + id };
            rider.Pet = new Unit(rider.PetLink, game.Current) { IsPet = true, Master = rider };
            (inParty ? game.Party : game.Remote).Add(rider);
            return rider;
        }

        // A rider whose control the player keeps at slot (moved there from the automatic placement).
        private static Unit Arranged(Game game, string id, int slot)
        {
            var rider = Rider(game, id);
            Update(game);
            Equal(new[] { 0 }, Controls(rider), id + " automatic placement");
            Move(rider, 0, slot);
            Update(game);
            Equal(new[] { slot }, Controls(rider), id + " arranged at " + slot);
            return rider;
        }

        private static void Update(Game game) => game.Toolbar.Reconcile();

        private static SaveFile Save(Game game)
        {
            var file = new SaveFile { Rows = game.Toolbar.CaptureRows(true) };
            game.Toolbar.BeginSave();
            foreach (var unit in game.Party.Concat(game.Remote).Concat(game.Detached).Distinct())
            {
                file.Bars[unit.Id] = (Slot[])unit.Bar.Clone();
                file.AutoUse[unit.Id] = unit.AutoUse;
                if (unit.Pet != null)
                {
                    file.Bars[unit.Pet.Id] = (Slot[])unit.Pet.Bar.Clone();
                    file.AutoUse[unit.Pet.Id] = unit.Pet.AutoUse;
                    file.PetOf[unit.Id] = unit.Pet.Id;
                }
            }
            file.Party = game.Party.Select(unit => unit.Id).ToList();
            file.Remote = game.Remote.Select(unit => unit.Id).ToList();
            file.Detached = game.Detached.Select(unit => unit.Id).ToList();
            Update(game);
            game.RestoreLeases();
            return file;
        }

        // A cold load: a new player world and new unit objects with the saved bars, then the archive rows.
        // beforeRestore: runs once the incoming world is current, before its archive rows are restored (for example a native
        // read of an incoming bar, or a bar that is not available yet).
        private static void Load(Game game, SaveFile file, Func<string, bool> desired = null, Action<Game> beforeRestore = null,
            Action<Game> afterSeed = null)
        {
            var scope = new object();
            game.Toolbar.BeginSelectedLoad(scope);
            var world = new World("w" + (game.Generation++));
            var units = new Dictionary<string, Unit>();
            Func<string, Unit> make = id =>
            {
                Unit unit;
                if (units.TryGetValue(id, out unit)) return unit;
                unit = new Unit(id, world) { Bar = ToNativeBar(file.Bars.ContainsKey(id) ? file.Bars[id] : new Slot[BarSize]) };
                string auto;
                if (file.AutoUse.TryGetValue(id, out auto)) unit.AutoUse = auto;
                units[id] = unit;
                return unit;
            };
            game.Party.Clear(); game.Remote.Clear(); game.Detached.Clear(); game.PairMembers.Clear();
            foreach (var pair in new[] { new { List = game.Party, Ids = file.Party }, new { List = game.Remote, Ids = file.Remote }, new { List = game.Detached, Ids = file.Detached } })
                foreach (var id in pair.Ids)
                {
                    var unit = make(id);
                    unit.Desired = desired == null || desired(id);
                    string petId;
                    if (file.PetOf.TryGetValue(id, out petId))
                    {
                        unit.PetLink = petId;
                        unit.Pet = make(petId);
                        unit.Pet.IsPet = true;
                        unit.Pet.Master = unit;
                    }
                    pair.List.Add(unit);
                }
            game.Current = world;
            beforeRestore?.Invoke(game);
            game.Toolbar.SeedFromRows(file.Rows, true);
            afterSeed?.Invoke(game);
            Update(game);
        }

        // Native deserialization rebinds nothing: an empty slot written into the save stays empty (unbound placeholders
        // become plain empty slots); every other slot keeps its content.
        private static Slot[] ToNativeBar(Slot[] saved) => saved.Select(slot => slot is EmptySlot ? null : slot).ToArray();

        private static void EnterCapital(Game game, Unit rider)
        {
            game.Party.Remove(rider);
            game.PairMembers.Remove(rider);
            if (!game.Remote.Contains(rider)) game.Remote.Add(rider);
        }

        private static void LeaveCapital(Game game, Unit rider, bool mounted = false)
        {
            game.Remote.Remove(rider);
            if (!game.Party.Contains(rider)) game.Party.Add(rider);
            if (mounted && !game.PairMembers.Contains(rider)) game.PairMembers.Add(rider);
            rider.Desired = true;
        }

        // Native GetSlot(index): a bad public binding (its fact gone, inactive or replaced) becomes an empty slot bound to
        // the unit. throws: native threw after its write (no postfix).
        private static void NativeGetSlot(Game game, Unit unit, int index, bool throws = false)
        {
            game.Toolbar.BeginNativeSlotOperation(unit, index);
            if (IsBad(game, unit, index)) unit.Bar[index] = new EmptySlot(unit);
            if (!throws) game.Toolbar.EndNativeSlotOperation(unit);
        }

        // Native CleanupBadSlots: every bad public binding is rebound to a live fact of its blueprint, or emptied, in index
        // order. throwAfter: native threw after that many writes.
        private static void NativeCleanup(Game game, Unit unit, int throwAfter = -1)
        {
            game.Toolbar.BeginNativeSlotOperation(unit, -1);
            var writes = 0;
            for (var index = 0; index < unit.Bar.Length; index++)
            {
                if (!IsBad(game, unit, index)) continue;
                if (throwAfter >= 0 && writes == throwAfter) return;
                var slot = (AbilitySlot)unit.Bar[index];
                unit.Bar[index] = slot.Kind == "Control" && game.ControlLive(unit)
                    ? new AbilitySlot("Control", unit.ControlFact) : (Slot)new EmptySlot(unit);
                writes++;
            }
            if (throwAfter < 0) game.Toolbar.EndNativeSlotOperation(unit);
        }

        private static bool IsBad(Game game, Unit unit, int index)
        {
            var slot = unit.Bar[index] as AbilitySlot;
            if (slot == null) return false;
            if (slot.Kind == "Control") return !game.ControlLive(unit) || slot.Data != unit.ControlFact;
            if (slot.Kind == "Mount" || slot.Kind == "Dismount") return !unit.AliasLive;
            return false;
        }

        // Player edits on the action bar.
        private static void Place(Unit unit, int index) => unit.Bar[index] = new AbilitySlot("Control", unit.ControlFact == 0 ? 1 : unit.ControlFact);
        private static void Remove(Unit unit, int index) => unit.Bar[index] = null;
        private static void Fill(Unit unit, int index, string ability = "Fireball") => unit.Bar[index] = new AbilitySlot(ability, 1);

        private static void Move(Unit unit, int from, int to)
        {
            unit.Bar[to] = unit.Bar[from];
            unit.Bar[from] = null;
        }

        private static void StaleControl(Unit unit, int index, int data = 7) => unit.Bar[index] = new AbilitySlot("Control", data);

        // ---- reading the outcome -----------------------------------------------------------------------------------

        private static int[] Controls(Unit unit) => Enumerable.Range(0, unit.Bar.Length)
            .Where(index => (unit.Bar[index] as AbilitySlot)?.Kind == "Control").ToArray();

        private static int[] Live(Game game, Unit unit) => Controls(unit).Where(index => game.HoldsLiveBinding(unit, index)).ToArray();

        private static int[] SlotRows(IEnumerable<MountedToolbarRow> rows, string id) => rows
            .Where(row => row.ActorId == id && row.Kind == MountedControlSlotPolicy.SavedMountState).Select(row => row.Index).OrderBy(i => i).ToArray();

        private static int Records(IEnumerable<MountedToolbarRow> rows, string id) =>
            rows.Count(row => row.ActorId == id && row.Kind == MountedControlSlotPolicy.SavedPlacementRecord);

        private static void ExpectRows(IEnumerable<MountedToolbarRow> rows, string id, int[] slots, int records, string message)
        {
            Equal(slots, SlotRows(rows, id), message + ": slot rows");
            TestRunner.Equal(records, Records(rows, id), message + ": placement records");
        }

        private static void Equal(int[] expected, int[] actual, string message) =>
            TestRunner.True(expected.SequenceEqual(actual), message + " Expected=[" + string.Join(",", expected) + "] Actual=[" + string.Join(",", actual) + "]");

        private static Unit Find(Game game, string id) => game.Party.Concat(game.Remote).Concat(game.Detached).Single(unit => unit.Id == id);

        private static string Describe(Unit unit) => string.Join(",", Enumerable.Range(0, unit.Bar.Length)
            .Where(index => unit.Bar[index] is AbilitySlot).Select(index => index + ":" + unit.Bar[index]));

        // ---- tests --------------------------------------------------------------------------------------------------

        // Mission §4/§7 toolbar transitions. Repeated capital visits move the rider to the reserve roster (its horse leaves
        // the game with it); return mounted or dismounted; interior transitions suspend and resume every control.
        private static void SurvivesTransitions()
        {
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            Fill(rider, 3, "Attack");
            for (var visit = 0; visit < 3; visit++)
            {
                EnterCapital(game, rider);
                Update(game);
                Equal(new int[0], Controls(rider), "visit " + visit + ": the reserve bar holds no KMC binding");
                Update(game);
                LeaveCapital(game, rider, mounted: visit % 2 == 0);
                Update(game);
                Equal(new[] { 9 }, Controls(rider), "visit " + visit + ": back at 9 exactly once");
                Equal(new[] { 9 }, Live(game, rider), "visit " + visit + ": bound to the live control");
                game.Toolbar.SuspendArea();
                Equal(new int[0], Controls(rider), "interior transition empties the KMC slots");
                Update(game);
                Equal(new[] { 9 }, Controls(rider), "visit " + visit + ": interior transition returns 9 once");
            }
            TestRunner.True((rider.Bar[3] as AbilitySlot)?.Kind == "Attack", "An unrelated slot changed.");
            TestRunner.Equal(1L, game.Toolbar.InitialPlacementCount, "The automatic placement ran again.");
        }

        private static void SaveLoadInsideAndOutsideCity()
        {
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            // Outside the city: save and cold load in the field.
            var field = Save(game);
            ExpectRows(field.Rows, "A", new[] { 9 }, 1, "field save");
            Equal(new[] { 9 }, Controls(rider), "the save scope returns the slot");
            Load(game, field);
            rider = Find(game, "A");
            Equal(new[] { 9 }, Controls(rider), "field cold load restores 9 once");
            // Inside the city: the rider is on the reserve roster when the save is made and when it is loaded.
            EnterCapital(game, rider);
            Update(game);
            var capital = Save(game);
            ExpectRows(capital.Rows, "A", new[] { 9 }, 1, "capital save of a reserve rider");
            Load(game, capital, id => false);
            rider = Find(game, "A");
            TestRunner.True(game.Remote.Contains(rider), "The capital load did not keep the rider in reserve.");
            Equal(new int[0], Controls(rider), "a reserve rider holds no binding");
            var again = Save(game);
            ExpectRows(again.Rows, "A", new[] { 9 }, 1, "a second capital save before the rider returns");
            LeaveCapital(game, rider);
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "the reserve rider returns at 9 once after the capital load");
        }

        // R1-F1.
        private static void RemovalSurvivesReserveSaveLoad()
        {
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            Remove(rider, 9);
            Update(game);
            EnterCapital(game, rider);
            Update(game);
            var file = Save(game);
            ExpectRows(file.Rows, "A", new int[0], 1, "a removal is a placement record only");
            var placements = game.Toolbar.InitialPlacementCount;
            Load(game, file, id => false);
            rider = Find(game, "A");
            LeaveCapital(game, rider);
            Update(game);
            Update(game);
            Equal(new int[0], Controls(rider), "the removed control was re-added");
            TestRunner.Equal(placements, game.Toolbar.InitialPlacementCount, "The loaded world autofilled the removed control.");
        }

        // D2 (and R1-F2, R4-F3 orders): two identical games, one updating before the save, one saving first.
        private static void SameEditsEitherOrder()
        {
            var edits = new Dictionary<string, Action<Game, Unit>>
            {
                { "move 9 to 3", (g, u) => Move(u, 9, 3) },
                { "remove 9", (g, u) => Remove(u, 9) },
                { "native empties 9 then the player places at 11", (g, u) => { u.FactActive = false; NativeGetSlot(g, u, 9); u.FactActive = true; Place(u, 11); } },
                { "native empties 9 then the player fills it", (g, u) => { u.FactActive = false; NativeGetSlot(g, u, 9); Fill(u, 9); } },
                { "native empties 9 with the fact back before the update", (g, u) => { u.FactActive = false; NativeGetSlot(g, u, 9); u.FactActive = true; } },
                { "move 9 to 3 then the rider leaves the party", (g, u) => { Move(u, 9, 3); EnterCapital(g, u); } }
            };
            foreach (var edit in edits)
            {
                var first = new Game();
                var a = Arranged(first, "A", 9);
                edit.Value(first, a);
                Update(first);
                var updateFirst = Save(first);
                var second = new Game();
                var b = Arranged(second, "A", 9);
                edit.Value(second, b);
                var saveFirst = Save(second);
                Update(second);
                Equal(SlotRows(updateFirst.Rows, "A"), SlotRows(saveFirst.Rows, "A"), edit.Key + ": archive slot rows differ by order");
                TestRunner.Equal(Records(updateFirst.Rows, "A"), Records(saveFirst.Rows, "A"), edit.Key + ": placement records differ by order");
                Equal(Controls(a), Controls(b), edit.Key + ": bars differ by order");
                Load(first, updateFirst); Load(second, saveFirst);
                Equal(Controls(Find(first, "A")), Controls(Find(second, "A")), edit.Key + ": loaded bars differ by order");
            }
        }

        // R1-F3 (with r2, r3), R4-F1 r5, R12-F4, R13-F1.
        private static void NativeInvalidationVersusRemoval()
        {
            // The fact goes inactive, GetSlot empties 9, the fact returns before the next update: 9 returns once.
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            rider.FactActive = false;
            NativeGetSlot(game, rider, 9);
            TestRunner.True(rider.Bar[9] is EmptySlot, "Native did not empty the bad slot.");
            rider.FactActive = true;
            Update(game);
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "a native emptying returns the control once (fact active again first)");
            // The fact is replaced (missing) and native cleanup empties 9 before KMC re-leases it.
            rider.ControlFact = 0;
            NativeCleanup(game, rider);
            Update(game);
            Equal(new[] { 9 }, Live(game, rider), "a native emptying before the replacement returns 9 bound to the new fact");
            // A deliberate removal of a live control stays removed, through a save scope and an area transition too.
            Remove(rider, 9);
            Update(game); Update(game);
            Equal(new int[0], Controls(rider), "a live removal was undone");
            ExpectRows(Save(game).Rows, "A", new int[0], 1, "a live removal is saved as a record only");
            game.Toolbar.SuspendArea();
            Update(game);
            Equal(new int[0], Controls(rider), "a live removal came back after a save scope and an area transition");
            // r3: the player moves the control, then native cleanup empties the new slot before the update.
            game = new Game();
            rider = Arranged(game, "A", 9);
            Move(rider, 9, 3);
            rider.FactActive = false;
            NativeCleanup(game, rider);
            rider.FactActive = true;
            Update(game);
            Equal(new[] { 3 }, Controls(rider), "native cleanup after a move returns the moved slot, not the old one");
            // R4-F1: a removal, then the rider briefly stops getting the control before the update; the same pet returns.
            game = new Game();
            rider = Arranged(game, "A", 9);
            Remove(rider, 9);
            rider.Desired = false;
            Update(game);
            rider.Desired = true;
            Update(game); Update(game);
            Equal(new int[0], Controls(rider), "a removal before a brief loss of eligibility came back");
            // R4-F1 r5: a removal, then native deactivates the fact before the update.
            game = new Game();
            rider = Arranged(game, "A", 9);
            Remove(rider, 9);
            rider.FactActive = false;
            NativeCleanup(game, rider);
            Update(game); Update(game);
            Equal(new int[0], Controls(rider), "a removal followed by native deactivation came back");
            // R12-F4: CleanupBadSlots rebinds a stale slot to the live fact; the player then removes it before the update.
            game = new Game();
            rider = Arranged(game, "A", 9);
            StaleControl(rider, 9, 3);
            NativeCleanup(game, rider);
            Equal(new[] { 9 }, Live(game, rider), "native rebound the stale slot");
            Remove(rider, 9);
            Update(game); Update(game);
            Equal(new int[0], Controls(rider), "a removal after a native rebind came back");
        }

        // R17-F2 (with r18, r19, r20), R13-F4, R14-F1, R19-F3.
        private static void NativeOperationEdges()
        {
            // A read with no native operation around it records nothing: the player's later removal stands.
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            rider.FactActive = false;
            TestRunner.True(IsBad(game, rider, 9), "The fixture is not bad.");
            Remove(rider, 9);
            rider.FactActive = true;
            Update(game);
            Equal(new int[0], Controls(rider), "a validity query turned a removal into intent");
            // r18: GetSlot throws before its write; the player then clears the slot (null) before the next update.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.FactActive = false;
            game.Toolbar.BeginNativeSlotOperation(rider, 9);
            Remove(rider, 9);
            rider.FactActive = true;
            Update(game);
            Equal(new int[0], Controls(rider), "an aborted native operation absorbed the player's removal");
            // r19: native completed its write and then threw: the write counts.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.FactActive = false;
            NativeGetSlot(game, rider, 9, throws: true);
            rider.FactActive = true;
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "a native write completed before a throw was lost");
            // r20: cleanup empties the only slot and throws; the save header is the next boundary.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.FactActive = false;
            NativeCleanup(game, rider, throwAfter: 1);
            var file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 9 }, 1, "the header after a throwing cleanup");
            // R13-F4: a native operation of world A that threw is never applied after the world is replaced.
            game = new Game();
            rider = Arranged(game, "A", 9);
            var saved = Save(game);
            rider.FactActive = false;
            game.Toolbar.BeginNativeSlotOperation(rider, 9);
            rider.Bar[9] = new EmptySlot(rider);
            Load(game, saved);
            var loaded = Find(game, "A");
            Equal(new[] { 9 }, Controls(loaded), "the loaded world was changed by the outgoing world's native write");
            TestRunner.True(!game.Toolbar.HasOpenNativeOperation, "The outgoing native operation was kept.");
            // R14-F1: native empties a first manual placement before any observation; the slot is filled before the
            // control returns: off the bar, and no autofill copy.
            game = new Game { Autofill = false };
            rider = Rider(game, "A");
            Update(game);
            Place(rider, 5);
            rider.FactActive = false;
            NativeGetSlot(game, rider, 5);
            Fill(rider, 5);
            game.Autofill = true;
            rider.FactActive = true;
            Update(game); Update(game);
            Equal(new int[0], Controls(rider), "a filled slot got an autofill copy");
            TestRunner.True(game.Toolbar.IsPlaced("A"), "The manual placement did not satisfy the one-time placement.");
            // R19-F3: native empties 3, the player places at 9, 9 is invalidated before the update: 9 returns, not 3.
            game = new Game();
            rider = Arranged(game, "A", 3);
            rider.FactActive = false;
            NativeGetSlot(game, rider, 3);
            Place(rider, 9);
            NativeGetSlot(game, rider, 9);
            rider.FactActive = true;
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "a newer invalidated placement lost to the older one");
        }

        // R2-F1 (with r3, r4), R10-F1, R13-F3, R14-F3, R22-F1.
        private static void HeaderDuringInterruption()
        {
            // Native emptied the chosen slot after the last update; the header runs first.
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            rider.FactActive = false;
            NativeGetSlot(game, rider, 9);
            var file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 9 }, 1, "the header keeps a just-invalidated slot");
            // r3: the fact was replaced (active) but the slot still holds the old data when the header runs.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.ControlFact = ++game.Generation;
            file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 9 }, 1, "the header keeps a stale binding's slot");
            Load(game, file);
            Equal(new[] { 9 }, Live(game, Find(game, "A")), "the stale slot restores bound to the new data once");
            // r4: the pet stops resolving (same link) before the update; the old fact is still active.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.Desired = false;
            file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 9 }, 1, "the header while the pet does not resolve");
            // R13-F3: the pending slot is occupied by another KMC binding (an older Mounted Charge): given up, no
            // duplicate row at that index.
            game = new Game();
            rider = Arranged(game, "A", 9);
            EnterCapital(game, rider);
            Update(game);
            rider.Bar[9] = new AbilitySlot("Charge", 1);
            file = Save(game);
            ExpectRows(file.Rows, "A", new int[0], 1, "an occupied pending slot is saved off the bar");
            // R14-F3: saving while another ability occupies the pending slot gives it up in the continuing game too.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.FactActive = false;
            NativeGetSlot(game, rider, 9);
            Fill(rider, 9);
            rider.Desired = false;
            file = Save(game);
            ExpectRows(file.Rows, "A", new int[0], 1, "an occupied slot is off the bar in the save");
            rider.Bar[9] = null;
            rider.Desired = true;
            rider.FactActive = true;
            Update(game);
            Equal(new int[0], Controls(rider), "the continuing game restored a slot the save gave up");
        }

        // R2-F2 (with r3), R4-F3, D1.
        private static void IntentBeforeCleanup()
        {
            foreach (var removal in new[] { false, true })
            {
                // An edit, then the rider leaves the party before the next update.
                var game = new Game();
                var rider = Arranged(game, "A", 9);
                if (removal) Remove(rider, 9); else Move(rider, 9, 3);
                EnterCapital(game, rider);
                Update(game);
                LeaveCapital(game, rider);
                Update(game);
                Equal(removal ? new int[0] : new[] { 3 }, Controls(rider), "departure kept the edit (removal=" + removal + ")");
                // r3: the save header runs between the roster change and the departure cleanup.
                game = new Game();
                rider = Arranged(game, "A", 9);
                if (removal) Remove(rider, 9); else Move(rider, 9, 3);
                EnterCapital(game, rider);
                var file = Save(game);
                ExpectRows(file.Rows, "A", removal ? new int[0] : new[] { 3 }, 1, "header before departure cleanup (removal=" + removal + ")");
                // R4-F3: a second rider edits and leaves; area suspension runs before the departure loop.
                game = new Game();
                var other = Arranged(game, "B", 2);
                rider = Arranged(game, "A", 9);
                if (removal) Remove(rider, 9); else Move(rider, 9, 3);
                EnterCapital(game, rider);
                game.Toolbar.SuspendArea();
                Update(game);
                LeaveCapital(game, rider);
                Update(game);
                Equal(removal ? new int[0] : new[] { 3 }, Controls(rider), "area suspension kept the edit (removal=" + removal + ")");
                Equal(new[] { 2 }, Controls(other), "the other rider changed");
            }
        }

        // R1-F5, R2-F3, R4-F2 (with r5, r6), R8-F2, R12-F3, R16-F1.
        private static void CompanionLoss()
        {
            // R1-F5: permanent pet loss while in reserve retires the arrangement; a kept pet round-trips.
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            var keeper = Arranged(game, "B", 4);
            EnterCapital(game, rider); EnterCapital(game, keeper);
            Update(game);
            // The companion is gone: no pet link, and the rider no longer gets the control.
            rider.PetLink = null; rider.Pet = null; rider.Desired = false;
            Update(game);
            var file = Save(game);
            ExpectRows(file.Rows, "A", new int[0], 1, "a lost pet leaves no restorable row");
            ExpectRows(file.Rows, "B", new[] { 4 }, 1, "a kept pet round-trips");
            rider.PetLink = "pet-new"; rider.Pet = new Unit("pet-new", game.Current) { IsPet = true, Master = rider };
            LeaveCapital(game, rider); LeaveCapital(game, keeper);
            Update(game);
            Equal(new int[0], Controls(rider), "a new pet reused the old slot");
            Equal(new[] { 4 }, Controls(keeper), "the kept pet's rider lost its slot");
            // R2-F3: the same pet link while the pet object does not resolve keeps the slot.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.Desired = false;
            Update(game); Update(game);
            rider.Desired = true;
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "an unresolved pet with the same link lost the slot");
            // R4-F2: a party rider permanently loses its pet (link cleared, no longer granted) while the bar keeps the old
            // slot object; a new pet later must not revive it.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.PetLink = null; rider.Desired = false;
            Update(game);
            Equal(new int[0], Controls(rider), "a permanent loss kept the old slot");
            rider.PetLink = "pet-B"; rider.Desired = true;
            Update(game); Update(game);
            Equal(new int[0], Controls(rider), "a new pet revived the old slot");
            // R4-F2 r5: the link changes A to B between updates: retired and the old slot cleared, never revived.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.PetLink = "pet-B";
            Update(game);
            Equal(new int[0], Controls(rider), "a direct change of pets kept the old slot");
            Update(game);
            Equal(new int[0], Controls(rider), "the old slot was revived");
            // R4-F2 r6: the link clears, native reports the slot bad, then a new pet links before the update.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.PetLink = null; rider.Desired = false; rider.FactActive = false;
            NativeGetSlot(game, rider, 9);
            rider.PetLink = "pet-B"; rider.Desired = true;
            Update(game);
            Equal(new int[0], Controls(rider), "a cleared link followed by a new pet revived the old slot");
            // R8-F2: native cleanup while a faulted relationship still grants the control with an empty link keeps the slot.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.PetLink = null;
            rider.FactActive = false;
            NativeGetSlot(game, rider, 9);
            rider.FactActive = true;
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "a faulted rider still granted the control lost its slot");
            // R17-F3: A's binding is recorded at 9 and the pet link now names B; during an open selected load native rebinds the
            // stale slot (base), or CleanupBadSlots empties stale 3 and 9 with no replacement fact (r18); the load is abandoned.
            // The retirement holds, the binding is cleared, and nothing returns (save: record only).
            foreach (var variant in new[] { "rebind", "r18 two-slot cleanup" })
            {
                game = new Game();
                rider = Arranged(game, "A", 9);
                if (variant != "rebind")
                {
                    // Both stale bindings on the bar while the control is not live (no replacement fact).
                    rider.Desired = false;
                    StaleControl(rider, 3);
                    Update(game);
                }
                var scope = new object();
                game.Toolbar.BeginSelectedLoad(scope);
                rider.PetLink = "pet-B";
                rider.Pet = new Unit("pet-B", game.Current) { IsPet = true, Master = rider };
                if (variant == "rebind") StaleControl(rider, 9, 3);
                NativeCleanup(game, rider);
                game.Toolbar.AbandonSelectedLoad(scope);
                TestRunner.True(Controls(rider).Length == 0 && game.Toolbar.IsRetired("A"), variant + ": the retirement did not hold under the load: " + Describe(rider));
                rider.Desired = true;
                Update(game);
                ExpectRows(Save(game).Rows, "A", new int[0], 1, variant + ": save after abandonment");
                Equal(new int[0], Controls(rider), variant + ": nothing returns for pet B");
            }
            // R12-F3: a direct change of pets clears the canonical binding and an older alias.
            game = new Game();
            rider = Arranged(game, "A", 9);
            rider.Bar[12] = new AbilitySlot("Mount", 1);
            rider.PetLink = "pet-B";
            Update(game);
            TestRunner.True(Controls(rider).Length == 0 && rider.Bar.OfType<AbilitySlot>().All(slot => slot.Kind != "Mount"),
                "Retirement left a public binding: " + Describe(rider));
            // R16-F1: the automatic placement records its companion; a change before the next update retires it.
            game = new Game();
            rider = Rider(game, "A");
            Update(game);
            Equal(new[] { 0 }, Controls(rider), "automatic placement");
            rider.PetLink = "pet-B";
            file = Save(game);
            ExpectRows(file.Rows, "A", new int[0], 1, "a placement for the old companion was saved");
            Equal(new int[0], Controls(rider), "the old companion's placement survived");
        }

        // R9-F2 (with r10).
        private static void CompanionChangeDuringSave()
        {
            foreach (var alias in new[] { false, true })
            {
                var game = new Game();
                var rider = Arranged(game, "A", 9);
                if (alias) { rider.Bar[9] = new AbilitySlot("Mount", 1); rider.AliasLive = true; }
                var file = game.Toolbar.CaptureRows(true);
                ExpectRows(file, "A", new[] { 9 }, 1, "header (alias=" + alias + ")");
                game.Toolbar.BeginSave();
                rider.PetLink = "pet-B";
                Update(game);
                game.RestoreLeases();
                Equal(new int[0], Controls(rider), "a slot returned for the replaced companion (alias=" + alias + ")");
                var next = Save(game);
                ExpectRows(next.Rows, "A", new int[0], 1, "the next save omits it (alias=" + alias + ")");
            }
        }

        // R1-F4, R4-F2 r7, R25-F1.
        private static void InWorldReplay()
        {
            // R1-F4: a forced dismount keeps a retained remount intent whose rows were captured with the control at 9; the
            // player removes the live control meanwhile; the delayed remount replays the old rows.
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            var captured = game.Toolbar.CaptureRows(false);
            ExpectRows(captured, "A", new[] { 9 }, 1, "in-world capture");
            Remove(rider, 9);
            Update(game);
            game.Toolbar.SeedFromRows(captured, false);
            Update(game);
            Equal(new int[0], Controls(rider), "a replay resurrected a removed control");
            // A move since the capture wins over the replay as well.
            game = new Game();
            rider = Arranged(game, "A", 9);
            captured = game.Toolbar.CaptureRows(false);
            Move(rider, 9, 2);
            Update(game);
            game.Toolbar.SuspendArea();
            game.Toolbar.SeedFromRows(captured, false);
            Update(game);
            Equal(new[] { 2 }, Controls(rider), "a replay moved the control back");
            // R4-F2 r7: the pet changes across an area transition while another pair forces suspend/restore.
            game = new Game();
            rider = Arranged(game, "A", 9);
            Arranged(game, "B", 3);
            captured = game.Toolbar.CaptureRows(false);
            game.Toolbar.SuspendArea();
            rider.PetLink = "pet-B";
            game.Toolbar.SeedFromRows(captured, false);
            Update(game);
            Equal(new int[0], Controls(rider), "the retired slot passed to the new pet through the replay");
            // ... while the same pet briefly unresolved keeps 9.
            game = new Game();
            rider = Arranged(game, "A", 9);
            captured = game.Toolbar.CaptureRows(false);
            game.Toolbar.SuspendArea();
            rider.Desired = false;
            game.Toolbar.SeedFromRows(captured, false);
            Update(game);
            rider.Desired = true;
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "the same pet unresolved across the transition lost its slot");
            // R25-F1: a never-scanned reserve rider with a pet and a surviving shortcut (an older save) stays in reserve while
            // another pair starts an area transfer (capture, then suspension); the slot stays recoverable for its return.
            game = new Game();
            Arranged(game, "B", 3);
            rider = Rider(game, "A", inParty: false);
            StaleControl(rider, 6);
            captured = game.Toolbar.CaptureRows(false);
            game.Toolbar.SuspendArea();
            TestRunner.True(!(rider.Bar[6] is AbilitySlot), "The suspension left a KMC binding on the reserve bar.");
            game.Toolbar.SeedFromRows(captured, false);
            Update(game);
            TestRunner.True(game.Remote.Contains(rider), "The fixture rider left the reserve roster.");
            ExpectRows(Save(game).Rows, "A", new[] { 6 }, 1, "R25-F1 the reserve rider's slot after the area transfer");
            LeaveCapital(game, rider);
            Update(game);
            Equal(new[] { 6 }, Live(game, rider), "R25-F1 the surviving shortcut was lost across the area transfer");
        }

        // R3-F1 (with r4), R5-F1 (with r6), R5-F2, R14-F5, R17-F1 r19, R17-F3, R24-F2, R24-F3 (with r25), D4, D5.
        private static void LoadLifetime()
        {
            // R5-F1: a selected load is disposed before the world is replaced: reserve intent and removals survive. r6: native
            // cleanup during the open load still counts. R14-F5: a native rebind during it is the live state.
            var game = new Game();
            var reserve = Arranged(game, "A", 9);
            var remover = Arranged(game, "B", 4);
            var rebound = Arranged(game, "C", 7);
            EnterCapital(game, reserve);
            Update(game);
            Remove(remover, 4);
            var scope = new object();
            game.Toolbar.BeginSelectedLoad(scope);
            Update(game);
            rebound.FactActive = false;
            NativeGetSlot(game, rebound, 7);
            rebound.FactActive = true;
            game.Toolbar.AbandonSelectedLoad(scope);
            Update(game);
            LeaveCapital(game, reserve);
            Update(game);
            Equal(new[] { 9 }, Controls(reserve), "an abandoned load lost a reserve slot");
            Equal(new int[0], Controls(remover), "an abandoned load lost a removal");
            Equal(new[] { 7 }, Controls(rebound), "an abandoned load lost a native emptying made while it was open");
            // R5-F2: a superseded scope cannot end a newer scope's load; only the owner can.
            var older = new object(); var newer = new object();
            game.Toolbar.BeginSelectedLoad(older);
            game.Toolbar.BeginSelectedLoad(newer);
            game.Toolbar.AbandonSelectedLoad(older);
            TestRunner.True(game.Toolbar.HoldsLoad, "An older scope ended the newer load.");
            game.Toolbar.AbandonSelectedLoad(newer);
            TestRunner.True(!game.Toolbar.HoldsLoad, "The owning scope did not end its load.");
            // R24-F3 (r25): the load starts, an update sees no player, the outgoing world is current again, abandoned.
            game = new Game();
            var rider = Arranged(game, "A", 9);
            EnterCapital(game, rider);
            Update(game);
            var world = game.Current;
            scope = new object();
            game.Toolbar.BeginSelectedLoad(scope);
            game.Current = null;
            Update(game);
            TestRunner.True(game.Toolbar.HoldsWorld && game.Toolbar.ArrangementCount == 1, "A missing player released an open load's records.");
            game.Current = world;
            game.Toolbar.AbandonSelectedLoad(scope);
            var file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 9 }, 1, "records after a missing-player frame and abandonment");
            // R3-F1 (r4): a load replaces the world; the outgoing riders' objects never author anything in it. Observations
            // made while the outgoing world was still current stay with it.
            game = new Game();
            rider = Arranged(game, "A", 9);
            var saved = Save(game);
            Move(rider, 9, 2);
            scope = new object();
            game.Toolbar.BeginSelectedLoad(scope);
            Update(game);
            Load(game, saved);
            var loaded = Find(game, "A");
            Equal(new[] { 9 }, Controls(loaded), "the outgoing world's edit reached the loaded world");
            Remove(rider, 2);
            Update(game);
            Equal(new[] { 9 }, Controls(loaded), "an outgoing object's later edit reached the loaded world");
            // R24-F2: the incoming world's first native report precedes its first update and its rows.
            game = new Game();
            Arranged(game, "A", 9);
            saved = Save(game);
            game.Toolbar.BeginSelectedLoad(new object());
            var incoming = new World("incoming");
            game.Current = incoming;
            var reserveB = new Unit("B", incoming) { PetLink = "pet-B" };
            reserveB.Pet = new Unit("pet-B", incoming) { IsPet = true, Master = reserveB };
            StaleControl(reserveB, 5);
            game.Party.Clear(); game.Remote.Clear(); game.Remote.Add(reserveB);
            NativeGetSlot(game, reserveB, 5);
            // The incoming world's own archive (an older save: no row for B) is restored after that report, then the world
            // updates, saves, and B joins the party.
            game.Toolbar.SeedFromRows(new MountedToolbarRow[0], true);
            Update(game);
            var rows = Save(game).Rows;
            ExpectRows(rows, "B", new[] { 5 }, 1, "R24-F2 the incoming world's first native report, after its restore and update");
            ExpectRows(rows, "A", new int[0], 0, "R24-F2 the outgoing world's rider authors nothing");
            LeaveCapital(game, reserveB);
            Update(game);
            Equal(new[] { 5 }, Controls(reserveB), "R24-F2 B returns to its recovered slot once");
        }

        // D3.
        private static void RepeatedBoundariesIdempotent()
        {
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            var remover = Arranged(game, "B", 4);
            Remove(remover, 4);
            Fill(rider, 1);
            for (var round = 0; round < 4; round++)
            {
                Update(game); Update(game);
                var file = Save(game);
                ExpectRows(file.Rows, "A", new[] { 9 }, 1, "round " + round + " A");
                ExpectRows(file.Rows, "B", new int[0], 1, "round " + round + " B");
                game.Toolbar.SuspendArea();
                Update(game);
                Equal(new[] { 9 }, Controls(rider), "round " + round + ": A moved or duplicated");
                Equal(new int[0], Controls(remover), "round " + round + ": B resurrected");
            }
            TestRunner.True((rider.Bar[1] as AbilitySlot)?.Kind == "Fireball", "An unrelated slot changed.");
            TestRunner.Equal(2L, game.Toolbar.InitialPlacementCount, "Placement ran more than once per rider.");
        }

        // A never-observed rider (an older save's bar) whose public fact is live (live) or absent.
        private static Unit Unobserved(Game game, string id, bool inParty = true, bool live = true)
        {
            var rider = Rider(game, id, inParty);
            if (live) { rider.ControlFact = ++game.Generation; rider.FactActive = true; }
            return rider;
        }

        private static void LiveControl(Unit unit, int index) => unit.Bar[index] = new AbilitySlot("Control", unit.ControlFact);

        // R12-F1, R15-F1 (with r16-r19), R17-F1 (with r18), R18-F1 (with r19-r24), R21-F1, R22-F1, R22-F2, R23-F1.
        // One shortcut: every other public binding seen on the bar is a known copy (by slot object). A copy yields to a
        // recoverable choice, becomes the fallback only when the player filled the chosen slot, and is removed while the
        // control is live; a binding that is not a known copy is the player's fresh placement and wins.
        private static void KnownCopies()
        {
            // R12-F1 / R21-F1: stale 3 and current 9 on a never-observed bar: the save carries 3; update-first keeps one
            // callable control at 3.
            var game = new Game();
            var rider = Unobserved(game, "A");
            StaleControl(rider, 3); LiveControl(rider, 9);
            var file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 3 }, 1, "R12-F1 save first");
            Equal(new[] { 3 }, Live(game, rider), "R12-F1 one control after the scope");
            Load(game, file);
            Equal(new[] { 3 }, Controls(Find(game, "A")), "R12-F1 cold load");
            game = new Game();
            rider = Unobserved(game, "A");
            StaleControl(rider, 3); LiveControl(rider, 9);
            Update(game);
            Equal(new[] { 3 }, Live(game, rider), "R21-F1 update first keeps one callable control at 3");
            // R15-F1: canonical 3 and 9 while the pet is unavailable; CleanupBadSlots empties both: 3. r16: GetSlot empties
            // only 3, then a save: 3. r17: stale 3 and current 9, GetSlot(3) right before the save: 3.
            foreach (var variant in new[] { "both emptied", "only 3 emptied", "stale 3 current 9" })
            {
                game = new Game();
                rider = Unobserved(game, "A", live: variant == "stale 3 current 9");
                if (variant == "stale 3 current 9") { StaleControl(rider, 3); LiveControl(rider, 9); }
                else { rider.Desired = false; StaleControl(rider, 3); StaleControl(rider, 9, 8); }
                if (variant == "both emptied") NativeCleanup(game, rider); else NativeGetSlot(game, rider, 3);
                file = Save(game);
                ExpectRows(file.Rows, "A", new[] { 3 }, 1, "R15-F1 " + variant);
                rider.Desired = true;
                Update(game);
                Equal(new[] { 3 }, Controls(rider), "R15-F1 " + variant + ": one control at 3");
                Load(game, file);
                Equal(new[] { 3 }, Controls(Find(game, "A")), "R15-F1 " + variant + ": cold load");
            }
            // r18: GetSlot empties 3 then 9 (pet unavailable), and the player drags the control into 9 before the update.
            game = new Game();
            rider = Unobserved(game, "A", live: false);
            rider.Desired = false;
            StaleControl(rider, 3); StaleControl(rider, 9, 8);
            NativeGetSlot(game, rider, 3); NativeGetSlot(game, rider, 9);
            Place(rider, 9);
            rider.Desired = true;
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "R15-F1 r18: the placement at 9 is the choice");
            // r19: stale 3, 7 and 9; GetSlot(3) records 7 and 9 as copies; GetSlot(9) empties 9; GetSlot(7) runs; the player
            // places at 9: 9 belongs to the new placement.
            game = new Game();
            rider = Unobserved(game, "A", live: false);
            rider.Desired = false;
            StaleControl(rider, 3); StaleControl(rider, 7, 8); StaleControl(rider, 9, 9);
            NativeGetSlot(game, rider, 3); NativeGetSlot(game, rider, 9); NativeGetSlot(game, rider, 7);
            Place(rider, 9);
            ExpectRows(Save(game).Rows, "A", new[] { 9 }, 1, "R15-F1 r19");
            // R18-F1: GetSlot empties the chosen stale 3 while live copies sit at 7 and 9: one control at 3.
            game = new Game();
            rider = Unobserved(game, "A");
            StaleControl(rider, 3); LiveControl(rider, 7); LiveControl(rider, 9);
            NativeGetSlot(game, rider, 3);
            ExpectRows(Save(game).Rows, "A", new[] { 3 }, 1, "R18-F1 save");
            Update(game);
            Equal(new[] { 3 }, Controls(rider), "R18-F1 one control at 3");
            // r19 / R22-F1: Fireball takes 3: exactly one fallback, the first copy 7, live, in the save and after a cold load.
            game = new Game();
            rider = Unobserved(game, "A");
            StaleControl(rider, 3); LiveControl(rider, 7); LiveControl(rider, 9);
            NativeGetSlot(game, rider, 3);
            Fill(rider, 3);
            file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 7 }, 1, "R22-F1 save before any update");
            Equal(new[] { 7 }, Controls(rider), "R18-F1 r19 one fallback");
            TestRunner.True((rider.Bar[3] as AbilitySlot)?.Kind == "Fireball", "The occupant of 3 was overwritten.");
            Load(game, file);
            Equal(new[] { 7 }, Controls(Find(game, "A")), "R22-F1 cold load");
            // R23-F1: GetSlot empties 3 (copies 7 and 9), Fireball takes 3, 7 is callable, native empties stale 9: 7 survives.
            game = new Game();
            rider = Unobserved(game, "A");
            StaleControl(rider, 3); LiveControl(rider, 7); StaleControl(rider, 9, 8);
            NativeGetSlot(game, rider, 3);
            Fill(rider, 3);
            NativeGetSlot(game, rider, 9);
            ExpectRows(Save(game).Rows, "A", new[] { 7 }, 1, "R23-F1");
            // R18-F1 r20 / r21: Fireball at 3, CleanupBadSlots rebinds both stale copies: one control at 7; removing it (after
            // the update, or at once) leaves the placement record only.
            foreach (var removeAtOnce in new[] { false, true })
            {
                game = new Game();
                rider = Unobserved(game, "A");
                StaleControl(rider, 3); StaleControl(rider, 7, 8); StaleControl(rider, 9, 9);
                NativeGetSlot(game, rider, 3);
                Fill(rider, 3);
                NativeCleanup(game, rider);
                if (!removeAtOnce) { Update(game); Equal(new[] { 7 }, Controls(rider), "R18-F1 r20 one control at 7"); }
                Remove(rider, 7);
                ExpectRows(Save(game).Rows, "A", new int[0], 1, "R18-F1 r20/r21 removal (at once=" + removeAtOnce + ")");
                Update(game);
                Equal(new int[0], Controls(rider), "R18-F1 r20/r21 nothing returns (at once=" + removeAtOnce + ")");
            }
            // r22-r24: stale 3, current 9 and Attack at 0; CleanupBadSlots rebinds 3 and the player removes 3 before the first
            // repair (r23: the same pet briefly unresolved first; r24: GetSlot(9) empties the stale duplicate too).
            foreach (var variant in new[] { "r22", "r23", "r24" })
            {
                game = new Game();
                rider = Unobserved(game, "A");
                Fill(rider, 0, "Attack");
                StaleControl(rider, 3); LiveControl(rider, 9);
                NativeCleanup(game, rider);
                Remove(rider, 3);
                if (variant == "r23") rider.Desired = false;
                // r24: an update while the same pet is unavailable (the fact withdrawn), then GetSlot(9) empties the stale duplicate.
                if (variant == "r24") { rider.Desired = false; Update(game); NativeGetSlot(game, rider, 9); }
                ExpectRows(Save(game).Rows, "A", new int[0], 1, "R18-F1 " + variant);
                rider.Desired = true;
                Update(game);
                Equal(new int[0], Controls(rider), "R18-F1 " + variant + ": no shortcut");
                TestRunner.True((rider.Bar[0] as AbilitySlot)?.Kind == "Attack", "The ordinary ability at 0 changed (" + variant + ").");
            }
            // R17-F1: the player fills the interrupted 4 while a secondary binding survives at 9: Fireball stays; live and cold
            // one fallback at 9. r18: the surviving copy is a stale alias that native empties before the update or header.
            foreach (var alias in new[] { false, true })
            {
                game = new Game();
                rider = Unobserved(game, "A", live: !alias);
                StaleControl(rider, 4);
                if (alias) rider.Bar[9] = new AbilitySlot("Mount", 1); else LiveControl(rider, 9);
                NativeGetSlot(game, rider, 4);
                Fill(rider, 4);
                if (alias) NativeGetSlot(game, rider, 9);
                file = Save(game);
                ExpectRows(file.Rows, "A", new[] { 9 }, 1, "R17-F1 (alias=" + alias + ")");
                Update(game);
                Equal(new[] { 9 }, Controls(rider), "R17-F1 one fallback at 9 (alias=" + alias + ")");
                TestRunner.True((rider.Bar[4] as AbilitySlot)?.Kind == "Fireball", "Fireball at 4 was overwritten.");
                Load(game, file);
                Equal(new[] { 9 }, Controls(Find(game, "A")), "R17-F1 cold load (alias=" + alias + ")");
            }
            // R22-F2: an older save has rows 3 and 9 and another ability occupies 3 at the cold restore: 9 only.
            game = new Game();
            rider = Arranged(game, "A", 9);
            file = Save(game);
            file.Rows = new[] { new MountedToolbarRow { ActorId = "A", Index = 3, Kind = MountedControlSlotPolicy.SavedMountState } }.Concat(file.Rows).ToList();
            file.Bars["A"][3] = new AbilitySlot("Fireball", 1);
            Load(game, file);
            rider = Find(game, "A");
            Equal(new[] { 9 }, Controls(rider), "R22-F2 an occupied older row was restored over the occupant");
            TestRunner.True((rider.Bar[3] as AbilitySlot)?.Kind == "Fireball", "The occupant of 3 was overwritten.");
            // A live control keeps one shortcut: a second placement beside the live choice is removed.
            game = new Game();
            rider = Arranged(game, "A", 3);
            Place(rider, 9);
            Update(game);
            Equal(new[] { 3 }, Controls(rider), "a second live binding was kept");
        }

        // R19-F2 (with r20-r22), R23-F3 (with r24, r25).
        private static void MoveSupersedesPending()
        {
            // r20 / r21: native empties 3 while copies survive at 7 and 9; the player moves 9 to 11 and CleanupBadSlots rebinds
            // 11 (r21: Fireball also takes 3 and both 7 and 11 are rebound): only 11.
            foreach (var fireball in new[] { false, true })
            {
                var game = new Game();
                var rider = Unobserved(game, "A");
                StaleControl(rider, 3); LiveControl(rider, 7); LiveControl(rider, 9);
                NativeGetSlot(game, rider, 3);
                Move(rider, 9, 11);
                StaleControl(rider, 11, 1);
                // r21: the fact is replaced, so the copy at 7 is stale under its own slot object and is rebound too.
                if (fireball) { Fill(rider, 3); rider.ControlFact = ++game.Generation; }
                NativeCleanup(game, rider);
                var file = Save(game);
                ExpectRows(file.Rows, "A", new[] { 11 }, 1, "R19-F2 a move supersedes the pending slot (fireball=" + fireball + ")");
                Update(game);
                Equal(new[] { 11 }, Controls(rider), "R19-F2 only 11 (fireball=" + fireball + ")");
                Load(game, file);
                Equal(new[] { 11 }, Controls(Find(game, "A")), "R19-F2 cold load (fireball=" + fireball + ")");
            }
            // r22: after 11 is rebound with 7 still on the bar, native empties a newly invalidated 11: 11 is interrupted, the
            // same pet returns there once, and a save carries 11.
            var g = new Game();
            var r = Unobserved(g, "A");
            StaleControl(r, 3); LiveControl(r, 7); LiveControl(r, 9);
            NativeGetSlot(g, r, 3);
            Move(r, 9, 11);
            StaleControl(r, 11, 1);
            NativeCleanup(g, r);
            r.FactActive = false;
            NativeGetSlot(g, r, 11);
            ExpectRows(Save(g).Rows, "A", new[] { 11 }, 1, "R19-F2 r22 save");
            r.FactActive = true;
            Update(g);
            Equal(new[] { 11 }, Controls(r), "R19-F2 r22 returns at 11 once");
            // R23-F3 (r24, r25): native empties canonical 4; the player drags a callable older alias from 9 to 11 (with a
            // canonical copy at 7 for r24, and while the pet is unavailable for r25): one public control at 11.
            foreach (var variant in new[] { "R23-F3", "r24", "r25" })
            {
                g = new Game();
                r = Unobserved(g, "A");
                StaleControl(r, 4);
                if (variant != "R23-F3") LiveControl(r, 7);
                r.Bar[9] = new AbilitySlot("Mount", 1);
                r.AliasLive = true;
                NativeGetSlot(g, r, 4);
                Move(r, 9, 11);
                if (variant == "r25") r.Desired = false;
                Update(g);
                r.Desired = true;
                Update(g);
                Equal(new[] { 11 }, Controls(r), "the moved alias is the player's placement (" + variant + ")");
                TestRunner.True(r.Bar.OfType<AbilitySlot>().All(slot => slot.Kind != "Mount"), "An alias survived (" + variant + ").");
            }
        }

        // R8-F1, R9-F4 (with r10), R10-F1 (with r11), R11-F1, R12-F2, R13-F1, R14-F2 (with r15-r17), R18-F2, R19-F1.
        private static void LegacyAliases()
        {
            // R10-F1 (r11): the header before repair: a live alias, no canonical binding, no record: one row and one record.
            var game = new Game();
            var rider = Rider(game, "A");
            rider.Bar[5] = new AbilitySlot("Dismount", 1);
            rider.AliasLive = true;
            var rows = game.Toolbar.CaptureRows(true);
            ExpectRows(rows, "A", new[] { 5 }, 1, "a live alias before repair");
            Update(game);
            Equal(new[] { 5 }, Live(game, rider), "the alias became the public control in place");
            TestRunner.Equal(1L, game.Toolbar.AliasMigrationCount, "alias migration count");
            // Two older aliases (Mount at 3, Dismount at 4) collapse to one control at the first.
            game = new Game();
            rider = Rider(game, "A");
            rider.Bar[3] = new AbilitySlot("Mount", 1); rider.Bar[4] = new AbilitySlot("Dismount", 1);
            Update(game);
            Equal(new[] { 3 }, Controls(rider), "two aliases did not collapse to one control");
            // R11-F1: an older bar with a callable alias at 3 beside a canonical binding at 9, saved before any update: one
            // control at 9, saved and restored.
            game = new Game();
            rider = Rider(game, "A");
            rider.Bar[3] = new AbilitySlot("Mount", 1); rider.AliasLive = true;
            StaleControl(rider, 9);
            var file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 9 }, 1, "an alias beside a canonical binding");
            Update(game);
            Equal(new[] { 9 }, Live(game, rider), "the alias beside a canonical binding was kept");
            TestRunner.True(rider.Bar.OfType<AbilitySlot>().All(slot => slot.Kind != "Mount"), "The alias survived beside the canonical binding.");
            Load(game, file);
            Equal(new[] { 9 }, Controls(Find(game, "A")), "the save restores one control at 9");
            // R9-F4 (r10): an older alias while the pet does not resolve keeps its index until the same pet returns, in
            // update-first and native-first order.
            foreach (var nativeFirst in new[] { false, true })
            {
                game = new Game();
                rider = Rider(game, "A");
                rider.Desired = false;
                rider.Bar[6] = new AbilitySlot("Mount", 1);
                if (nativeFirst) NativeGetSlot(game, rider, 6);
                Update(game); Update(game);
                rider.Desired = true;
                Update(game);
                Equal(new[] { 6 }, Controls(rider), "an alias under an unresolved pet (native first=" + nativeFirst + ")");
            }
            // R14-F2 (r15-r17), R19-F1: an older bar with a stale canonical binding at 4 and a stale alias at 9, before any
            // update; native empties 4 (in one pass with 9, or GetSlot of 4 alone, then a save, an update, or native's rebind
            // of the alias): 4 wins and the alias never becomes the control.
            foreach (var variant in new[] { "one pass", "save first", "update first", "alias rebound" })
            {
                game = new Game();
                rider = Rider(game, "A");
                StaleControl(rider, 4);
                rider.Bar[9] = new AbilitySlot("Mount", 1);
                if (variant == "one pass") NativeCleanup(game, rider);
                else NativeGetSlot(game, rider, 4);
                if (variant == "alias rebound")
                {
                    // CleanupBadSlots rebinds the alias to a live shell fact: a new slot object of the same kind.
                    game.Toolbar.BeginNativeSlotOperation(rider, -1);
                    rider.Bar[9] = new AbilitySlot("Mount", 2);
                    game.Toolbar.EndNativeSlotOperation(rider);
                }
                if (variant == "save first") ExpectRows(Save(game).Rows, "A", new[] { 4 }, 1, variant);
                Update(game);
                Equal(new[] { 4 }, Controls(rider), "canonical 4 lost to an alias (" + variant + ")");
                TestRunner.True(rider.Bar.OfType<AbilitySlot>().All(slot => slot.Kind != "Mount"), "The alias survived repair (" + variant + ").");
            }
            // R19-F1 (review R1-F4): canonical 4 and alias 9 on an older bar; native empties 4; the player fills 4 before KMC
            // updates while the same pet is unresolved: Fireball stays at 4 and the surviving alias is the fallback at 9, which
            // returns as one public control, live and after a reload.
            game = new Game();
            rider = Unobserved(game, "A", live: false);
            StaleControl(rider, 4);
            rider.Bar[9] = new AbilitySlot("Mount", 1);
            NativeGetSlot(game, rider, 4);
            Fill(rider, 4);
            rider.Desired = false;
            Update(game);
            rider.Desired = true;
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "R19-F1 the alias fallback at 9");
            TestRunner.True((rider.Bar[4] as AbilitySlot)?.Kind == "Fireball", "R19-F1 Fireball at 4 was overwritten.");
            file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 9 }, 1, "R19-F1 save");
            Load(game, file);
            Equal(new[] { 9 }, Controls(Find(game, "A")), "R19-F1 reload");
            // R18-F2: a recorded alias at 4 and a bad alias at 9; native reads only 9; the player removes 4: a removal.
            game = new Game();
            rider = Rider(game, "A");
            rider.Desired = false;
            rider.Bar[4] = new AbilitySlot("Mount", 1); rider.Bar[9] = new AbilitySlot("Dismount", 1);
            NativeGetSlot(game, rider, 9);
            Remove(rider, 4);
            rider.Desired = true;
            Update(game);
            Equal(new int[0], Controls(rider), "the emptying of 9 was credited to the removed alias at 4");
            // R12-F2 / R13-F1: an alias the save scope cleared returns as the canonical control at its index.
            game = new Game();
            rider = Rider(game, "A");
            rider.Bar[8] = new AbilitySlot("Mount", 1);
            file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 8 }, 1, "the alias index is carried");
            Equal(new[] { 8 }, Live(game, rider), "the alias returned as the live control at 8");
            // R8-F1: a canonical binding beats an alias on an older reserve bar (covered with the Mounted Charge migration in
            // ReserveBars).
        }

        // R7-F1, R8-F1, R22-F3 (with r23-r25), R24-F1, R25-F3.
        private static void ReserveBars()
        {
            // R7-F1: an older capital save leaves a native binding on a reserve rider with no row: the next save carries it,
            // native serialization never holds it, and disable clears it.
            var game = new Game();
            var rider = Rider(game, "A", inParty: false);
            StaleControl(rider, 6);
            var file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 6 }, 1, "the surviving reserve binding is carried");
            TestRunner.True(file.Bars["A"].OfType<AbilitySlot>().All(slot => slot.Kind != "Control"), "Native serialization held a KMC binding.");
            StaleControl(rider, 6);
            game.Toolbar.Disable();
            Equal(new int[0], Controls(rider), "disable left the reserve binding");
            // R8-F1: an older reserve bar: the canonical binding beats a legacy alias; a legacy Mounted Charge becomes the
            // stock Charge at its index when available and not already on the bar.
            game = new Game();
            rider = Rider(game, "A", inParty: false);
            rider.HasStockCharge = true;
            rider.Bar[2] = new AbilitySlot("Mount", 1); StaleControl(rider, 6); rider.Bar[8] = new AbilitySlot("Charge", 1);
            Update(game);
            TestRunner.True((rider.Bar[8] as AbilitySlot)?.Kind == "StockCharge", "The older Mounted Charge did not become the stock Charge.");
            LeaveCapital(game, rider);
            Update(game);
            Equal(new[] { 6 }, Controls(rider), "the alias beat the canonical binding on an older reserve bar");
            // R22-F3, an older reserve bar before its first scan: GetSlot empties 3 and records the stale copy at 9.
            // base: 3 is occupied: 9 is kept. r23: native then rebinds 9 to a live fact before the capital save: 9 and a marker.
            // r24: after the rebind the player moves 9 to 11 (11) or removes it (record only). r25: 3 free, 9 rebound and moved
            // to 11: 11; rebound with no move: 3.
            foreach (var variant in new[] { "occupied", "occupied rebound", "rebound moved", "rebound removed", "free rebound moved", "free rebound" })
            {
                game = new Game();
                rider = Rider(game, "A", inParty: false);
                StaleControl(rider, 3); StaleControl(rider, 9, 8);
                NativeGetSlot(game, rider, 3);
                if (!variant.StartsWith("free", StringComparison.Ordinal)) Fill(rider, 3);
                if (variant.Contains("rebound"))
                {
                    // A live fact of the control exists and CleanupBadSlots rebinds the stale 9 (a new slot object).
                    rider.ControlFact = ++game.Generation; rider.FactActive = true;
                    NativeCleanup(game, rider);
                    TestRunner.True(game.HoldsLiveBinding(rider, 9), variant + ": native did not rebind 9.");
                }
                var moved = variant == "rebound moved" || variant == "free rebound moved";
                var removed = variant == "rebound removed";
                if (moved) Move(rider, 9, 11);
                if (removed) Remove(rider, 9);
                file = Save(game);
                var expected = variant.StartsWith("occupied", StringComparison.Ordinal) ? new[] { 9 } : removed ? new int[0] : moved ? new[] { 11 } : new[] { 3 };
                ExpectRows(file.Rows, "A", expected, 1, "first reserve scan (" + variant + ")");
                LeaveCapital(game, rider);
                Update(game);
                Equal(expected, Controls(rider), "the returning rider (" + variant + ")");
                Load(game, file);
                var loaded = Find(game, "A");
                LeaveCapital(game, loaded);
                Update(game);
                Equal(expected, Controls(loaded), "cold restore (" + variant + ")");
            }
            // R24-F1: a rider KMC never reconciled holds a temporary KMC fact in reserve when a save or disable runs.
            game = new Game();
            rider = Rider(game, "A", inParty: false);
            rider.ControlFact = 5; rider.FactActive = true; rider.OtherFacts = 1;
            Save(game);
            TestRunner.True(rider.ControlFact == 0 && rider.OtherFacts == 0, "A reserve rider kept its temporary KMC facts through a save.");
            rider.OtherFacts = 1;
            game.Toolbar.Disable();
            TestRunner.True(rider.OtherFacts == 0, "A reserve rider kept its temporary KMC facts through disable.");
            // R25-F3: an already-scanned reserve rider gets a fresh placement and stays in reserve until a capital save.
            game = new Game();
            rider = Arranged(game, "A", 9);
            EnterCapital(game, rider);
            Update(game); Update(game);
            Place(rider, 11);
            file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 11 }, 1, "a fresh reserve placement wins");
        }

        // Mission §4: occupied slots, another rider, ordinary abilities and unrelated slots.
        private static void OccupiedAndIndependent()
        {
            var game = new Game();
            var a = Arranged(game, "A", 9);
            var b = Arranged(game, "B", 9);
            Fill(a, 5, "Attack"); Fill(b, 6, "Heal");
            EnterCapital(game, a);
            Update(game);
            Fill(a, 9, "Fireball");
            LeaveCapital(game, a);
            Update(game);
            Equal(new int[0], Controls(a), "an occupied slot was overwritten or the control duplicated elsewhere");
            TestRunner.True((a.Bar[9] as AbilitySlot)?.Kind == "Fireball", "The occupant of 9 was replaced.");
            Equal(new[] { 9 }, Controls(b), "the other rider's arrangement changed");
            TestRunner.True((a.Bar[5] as AbilitySlot)?.Kind == "Attack" && (b.Bar[6] as AbilitySlot)?.Kind == "Heal", "Unrelated slots changed.");
            a.Bar[9] = null;
            Update(game);
            Equal(new int[0], Controls(a), "a given-up slot came back when its occupant left");
            var file = Save(game);
            ExpectRows(file.Rows, "A", new int[0], 1, "A off the bar");
            ExpectRows(file.Rows, "B", new[] { 9 }, 1, "B at 9");
        }

        // R2-F4 (with r11).
        private static void RemovalAfterSaveRelease()
        {
            foreach (var alias in new[] { false, true })
            {
                var game = new Game();
                var rider = alias ? Rider(game, "A") : Arranged(game, "A", 9);
                if (alias) { rider.Bar[0] = null; rider.Bar[9] = new AbilitySlot("Mount", 1); }
                Save(game);
                Equal(new[] { 9 }, Controls(rider), "the save scope returned the control (alias=" + alias + ")");
                Remove(rider, 9);
                var file = Save(game);
                ExpectRows(file.Rows, "A", new int[0], 1, "a removal right after the release (alias=" + alias + ")");
                Load(game, file);
                Equal(new int[0], Controls(Find(game, "A")), "the removal came back after the load (alias=" + alias + ")");
            }
        }

        // CTRL-03, R13-F2, R16-F1.
        private static void InitialPlacement()
        {
            // A save before the first update sees the manual binding: no autofill copy at the release.
            var game = new Game { Autofill = false };
            var rider = Rider(game, "A");
            Update(game);
            Place(rider, 7);
            game.Autofill = true;
            Save(game);
            Update(game);
            Equal(new[] { 7 }, Controls(rider), "an autofill copy followed a manual placement");
            // A full main bar records the attempt off the bar; freeing a slot later adds nothing.
            game = new Game();
            rider = Rider(game, "A");
            for (var i = 0; i < 14; i++) Fill(rider, i);
            Update(game);
            Equal(new int[0], Controls(rider), "a full bar was overwritten");
            TestRunner.Equal(1L, game.Toolbar.InitialPlacementFullBarCount, "full-bar count");
            rider.Bar[2] = null;
            Update(game);
            Equal(new int[0], Controls(rider), "the control was auto-inserted later");
            var file = Save(game);
            ExpectRows(file.Rows, "A", new int[0], 1, "a full-bar attempt is a placement record");
            // The setting off defers the placement; the selected load's bindings must be settled.
            game = new Game { Settled = false };
            rider = Rider(game, "A");
            Update(game);
            Equal(new int[0], Controls(rider), "placement ran before the load settled");
            game.Settled = true;
            Update(game);
            Equal(new[] { 0 }, Controls(rider), "placement did not run once settled");
        }

        // R9-F3, R14-F4, R15-F2, R20-F1 (with r21, r22), R21-F2, R23-F2, R25-F2, D5.
        private static void DisableAndLifetime()
        {
            // Disable is cleanup: every KMC binding, fact and auto-use leaves current, reserve and departed units; nothing is
            // remembered; re-enabling places once again (KMC itself emptied the slots).
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            var reserve = Rider(game, "B", inParty: false);
            StaleControl(reserve, 4); reserve.AutoUse = "KMC_MountState"; reserve.Pet.AutoUse = "Bite";
            rider.AutoUse = "Fireball";
            game.Toolbar.Disable();
            Equal(new int[0], Controls(rider), "disable left the rider's binding");
            Equal(new int[0], Controls(reserve), "disable left the reserve binding");
            TestRunner.True(reserve.AutoUse == null && reserve.Pet.AutoUse == "Bite" && rider.AutoUse == "Fireball", "Disable changed an ordinary auto-use or kept a KMC one.");
            TestRunner.True(!game.Toolbar.HoldsWorld && game.Toolbar.ArrangementCount == 0 && game.Toolbar.ObservedCount == 0, "Disable retained records.");
            // R15-F2 / R14-F4 / R21-F2: a save while disabled sanitizes without recording; loading another world while
            // disabled holds nothing of the old one.
            StaleControl(rider, 6);
            var file = Save(game);
            TestRunner.Equal(0, file.Rows.Count, "A disabled save wrote rows.");
            TestRunner.True(file.Bars["A"].OfType<AbilitySlot>().All(slot => slot.Kind != "Control"), "A disabled save serialized a KMC binding.");
            Load(game, file);
            TestRunner.True(!game.Toolbar.HoldsWorld && !game.Toolbar.HoldsLoad && game.Toolbar.ArrangementCount == 0, "A disabled load retained state.");
            game.Toolbar.Enable();
            Update(game);
            Equal(new[] { 0 }, Controls(Find(game, "A")), "re-enabling did not place once");
            // R20-F1 (r21, r22) / R9-F3: a reserve rider removed from the roster before its first observation leaves nothing
            // held; observed units of a removed rider are released by its departure.
            game = new Game();
            rider = Arranged(game, "A", 9);
            game.Party.Remove(rider);
            game.Ex.Add(rider);
            Update(game);
            TestRunner.Equal(0, game.Toolbar.ObservedUnits.Count(unit => unit.Id == "A"), "A removed rider stayed observed.");
            TestRunner.True(!game.Toolbar.TryGetArrangement("A", out _, out _) && game.Toolbar.IsRetired("A"), "A removed rider kept its arrangement.");
            // R23-F2: the main menu then a new game: the previous campaign's records are released.
            game = new Game();
            Arranged(game, "A", 9);
            game.Current = null;
            Update(game);
            TestRunner.True(!game.Toolbar.HoldsWorld && game.Toolbar.ObservedCount == 0, "The main menu retained the campaign.");
            game.Current = new World("new game");
            game.Party.Clear();
            var fresh = Rider(game, "A");
            Fill(fresh, 0);
            Update(game);
            Equal(new[] { 1 }, Controls(fresh), "a new game inherited the previous campaign's records");
            // R25-F2: an older capital save's managed auto-use on a reserve rider is cleared at a save and at disable; an
            // ordinary selection is kept.
            game = new Game();
            reserve = Rider(game, "B", inParty: false);
            var ordinary = Rider(game, "C", inParty: false);
            reserve.AutoUse = "KMC_RiderPrimary"; ordinary.AutoUse = "Fireball";
            file = Save(game);
            TestRunner.True(file.AutoUse["B"] == null && file.AutoUse["C"] == "Fireball", "The save serialized a managed auto-use or lost an ordinary one.");
            reserve.AutoUse = "KMC_RiderPrimary";
            game.Toolbar.Disable();
            TestRunner.True(reserve.AutoUse == null && ordinary.AutoUse == "Fireball", "Disable kept a managed auto-use or cleared an ordinary one.");
        }

        // AC-R26-F4. The capital fixture's separate memberships: the remote owner is in RemoteCompanions; its horse is not.
        private static void AcR26F4ReservePetSanitized()
        {
            foreach (var boundary in new[] { "save", "disable" })
            {
                var game = new Game();
                var owner = Rider(game, "Owner", inParty: false);
                var horse = owner.Pet;
                TestRunner.True(!game.Remote.Contains(horse) && !game.Party.Contains(horse), "The horse is not outside the reserve roster.");
                horse.Bar[2] = new AbilitySlot("MountPrimary", 1);
                horse.Bar[4] = new AbilitySlot("Bite", 1);
                horse.AutoUse = "KMC_MountPrimary";
                var control = Rider(game, "Control", inParty: false);
                control.Pet.AutoUse = "Bite";
                // Cold load: no update, no pair and no previous observation before the boundary.
                if (boundary == "save")
                {
                    var file = Save(game);
                    TestRunner.True(!(file.Bars[horse.Id][2] is AbilitySlot), "Serialization held the horse's stale Mount Primary slot.");
                    TestRunner.True(file.AutoUse[horse.Id] == null, "Serialization held the horse's stale Mount Primary auto-use.");
                    TestRunner.True((file.Bars[horse.Id][4] as AbilitySlot)?.Kind == "Bite", "Serialization lost the horse's ordinary ability.");
                    TestRunner.True(file.AutoUse[control.Pet.Id] == "Bite", "Serialization cleared an ordinary pet auto-use.");
                }
                else game.Toolbar.Disable();
                TestRunner.True(!(horse.Bar[2] is AbilitySlot) && horse.AutoUse == null, boundary + " left the horse's managed reference.");
                TestRunner.True((horse.Bar[4] as AbilitySlot)?.Kind == "Bite" && control.Pet.AutoUse == "Bite", boundary + " changed an ordinary ability or selection.");
            }
        }

        // AC-R22-F3.
        private static void AcR22F3FreshPlacementAfterNativeEmpty()
        {
            foreach (var variant in new[] { "placed at 11", "no edit", "duplicate no edit", "duplicate placed at 11" })
            {
                var game = new Game();
                var rider = Rider(game, "A", inParty: false);
                StaleControl(rider, 3);
                if (variant.StartsWith("duplicate", StringComparison.Ordinal)) StaleControl(rider, 7, 8);
                // Never observed: GetSlot empties 3 before any reserve scan.
                NativeGetSlot(game, rider, 3);
                TestRunner.True(rider.Bar[3] is EmptySlot, "Native did not empty 3.");
                if (variant.EndsWith("placed at 11", StringComparison.Ordinal))
                {
                    // The player places the current live control: the rider's public fact is active and the binding at 11
                    // holds its own data.
                    rider.ControlFact = ++game.Generation; rider.FactActive = true;
                    LiveControl(rider, 11);
                    TestRunner.True(game.HoldsLiveBinding(rider, 11), "The placement at 11 is not the live control.");
                }
                var file = Save(game);
                var expected = variant.EndsWith("placed at 11", StringComparison.Ordinal) ? new[] { 11 } : new[] { 3 };
                ExpectRows(file.Rows, "A", expected, 1, variant + ": archive");
                // A same-world return.
                LeaveCapital(game, rider);
                Update(game);
                Equal(expected, Controls(rider), variant + ": same-world return");
                // A cold restore of that save.
                Load(game, file, id => false);
                rider = Find(game, "A");
                LeaveCapital(game, rider);
                Update(game);
                Equal(expected, Controls(rider), variant + ": cold restore");
            }
        }

        // AC-R25-F3.
        private static void AcR25F3FreshShortcutAfterReplacement()
        {
            foreach (var placed in new[] { true, false })
            {
                var game = new Game();
                var rider = Rider(game, "A", inParty: false);
                rider.ControlFact = 1; rider.FactActive = true;
                rider.Bar[9] = new AbilitySlot("Control", 1);
                Update(game);
                TestRunner.True(game.Toolbar.TryGetArrangement("A", out _, out _), "The reserve scan did not record 9.");
                // Pet A is replaced by pet B; an update with the bar empty retires A's arrangement.
                rider.PetLink = "pet-B";
                rider.Pet = new Unit("pet-B", game.Current) { IsPet = true, Master = rider };
                Update(game);
                TestRunner.True(game.Toolbar.IsRetired("A"), "A's arrangement was not retired.");
                if (placed) Place(rider, 11);
                var file = Save(game);
                ExpectRows(file.Rows, "A", placed ? new[] { 11 } : new int[0], 1, "placed=" + placed + ": archive");
                Load(game, file, id => false);
                rider = Find(game, "A");
                LeaveCapital(game, rider);
                Update(game);
                Equal(placed ? new[] { 11 } : new int[0], Controls(rider), "placed=" + placed + ": cold restore for B");
                TestRunner.True(!(rider.Bar[9] is AbilitySlot), "The retired slot 9 was restored.");
            }
        }

        // AC-R26-F1.
        private static void AcR26F1CanceledLoadKeepsEdit()
        {
            foreach (var edited in new[] { true, false })
            {
                var game = new Game();
                var rider = Arranged(game, "A", 9);
                EnterCapital(game, rider);
                Update(game);
                TestRunner.True(!(rider.Bar[9] is AbilitySlot), "The reserve scan did not clear 9.");
                if (edited)
                {
                    // The player places the live ability: the public fact is active and 11 holds its own data.
                    rider.ControlFact = ++game.Generation; rider.FactActive = true;
                    LiveControl(rider, 11);
                    TestRunner.True(game.HoldsLiveBinding(rider, 11), "The placement at 11 is not the live control.");
                }
                // A selected load starts and is abandoned before world replacement, before KMC observes the edit.
                var scope = new object();
                game.Toolbar.BeginSelectedLoad(scope);
                game.Toolbar.Housekeeping();
                game.Toolbar.AbandonSelectedLoad(scope);
                var file = Save(game);
                ExpectRows(file.Rows, "A", edited ? new[] { 11 } : new[] { 9 }, 1, "edited=" + edited + ": immediate save");
                Load(game, file, id => false);
                rider = Find(game, "A");
                LeaveCapital(game, rider);
                Update(game);
                Equal(edited ? new[] { 11 } : new[] { 9 }, Controls(rider), "edited=" + edited + ": cold restore once");
            }
        }

        // AC-R26-F2.
        private static void AcR26F2MainMenuReleases()
        {
            foreach (var variant in new[] { "main menu", "fenced abandonment", "real transition" })
            {
                var game = new Game();
                var rider = Rider(game, "A", inParty: false);
                StaleControl(rider, 3); StaleControl(rider, 7);
                NativeGetSlot(game, rider, 3);
                TestRunner.True(game.Toolbar.ArrangementCount == 1, "The duplicate on the never-observed reserve rider was not recorded.");
                var world = game.Current;
                var scope = new object();
                if (variant != "main menu") game.Toolbar.BeginSelectedLoad(scope);
                game.Current = variant == "real transition" ? new World("w2") : null;
                Update(game);
                if (variant == "main menu")
                {
                    TestRunner.True(!game.Toolbar.HoldsWorld && game.Toolbar.ArrangementCount == 0 && game.Toolbar.ObservedCount == 0 &&
                        !game.Toolbar.HasOpenNativeOperation && !game.Toolbar.HoldsLoad, "The main menu retained records or the world.");
                    continue;
                }
                if (variant == "real transition")
                {
                    TestRunner.True(game.Toolbar.HoldsWorld && game.Toolbar.ArrangementCount == 0 && !game.Toolbar.HoldsLoad,
                        "A replaced world kept the outgoing records or the load.");
                    continue;
                }
                TestRunner.True(game.Toolbar.HoldsWorld && game.Toolbar.ArrangementCount == 1, "An open load released the outgoing records at a missing player.");
                game.Current = world;
                game.Toolbar.AbandonSelectedLoad(scope);
                ExpectRows(Save(game).Rows, "A", new[] { 3 }, 1, "fenced abandonment keeps the duplicate yielding to 3");
                LeaveCapital(game, rider);
                Update(game);
                Equal(new[] { 3 }, Controls(rider), "fenced abandonment: one shortcut at 3 on return");
            }
        }

        // AC-R26-F3.
        private static void AcR26F3RejoinedRiderAutoUse()
        {
            foreach (var boundary in new[] { "save", "disable", "disabled save" })
            {
                var game = new Game();
                var rider = Rider(game, "A", inParty: false);
                rider.AutoUse = "KMC_RiderPrimary";
                var ordinary = Rider(game, "B");
                ordinary.AutoUse = "Fireball";
                Update(game);
                if (boundary == "disabled save")
                {
                    // KMC is disabled first; the stale selection is then on the rider (as an older save restores it while
                    // disabled) before the save.
                    game.Toolbar.Disable();
                    rider.AutoUse = "KMC_RiderPrimary";
                }
                TestRunner.True(rider.AutoUse == "KMC_RiderPrimary", boundary + ": the stale selection is not present before the boundary.");
                // The stale fixture moves from reserve to the travelling party immediately before the boundary.
                game.Remote.Remove(rider);
                game.Party.Add(rider);
                if (boundary == "disable") game.Toolbar.Disable();
                else
                {
                    var file = Save(game);
                    TestRunner.True(file.AutoUse["A"] == null, boundary + ": serialization held the stale Rider Primary selection.");
                    TestRunner.True(file.AutoUse["B"] == "Fireball", boundary + ": serialization lost an ordinary selection.");
                    if (boundary == "disabled save") TestRunner.True(file.Rows.Count == 0 && !game.Toolbar.HoldsWorld, "A disabled save recorded intent.");
                }
                TestRunner.True(rider.AutoUse == null && ordinary.AutoUse == "Fireball", boundary + ": managed or ordinary auto-use wrong afterwards.");
            }
        }

        // R9-F1: a save and a disable (or dispose) run with a populated reserve roster before registration succeeded, or after a
        // partial registration, with an item (a non-ability slot: its null blueprint goes through the production
        // ResolveExactKind) and an ordinary ability on the reserve bar. Inside serialization and after disable the same item and
        // ability objects stay; only an identifiable KMC binding (the public control once registered, the positive control) is
        // cleaned.
        private static void UnregisteredBlueprints()
        {
            foreach (var registration in new[] { "none", "public control only" })
                foreach (var boundary in new[] { "save", "disable" })
                {
                    var game = new Game();
                    game.Registered.Clear();
                    if (registration != "none") game.Registered.Add("Control");
                    var reserve = Rider(game, "A", inParty: false);
                    var item = new ItemSlot("Potion");
                    var ability = new AbilitySlot("Fireball", 1);
                    reserve.Bar[2] = item; reserve.Bar[3] = ability;
                    StaleControl(reserve, 5);
                    reserve.Bar[6] = new AbilitySlot("RiderPrimary", 1);
                    var label = registration + " / " + boundary;
                    Slot[] serialized;
                    if (boundary == "save") serialized = Save(game).Bars["A"];
                    else { game.Toolbar.Disable(); serialized = reserve.Bar; }
                    TestRunner.True(ReferenceEquals(serialized[2], item) && ReferenceEquals(serialized[3], ability), label + ": an ordinary slot object was replaced in the boundary's bar.");
                    TestRunner.True(ReferenceEquals(reserve.Bar[2], item) && ReferenceEquals(reserve.Bar[3], ability), label + ": an ordinary slot object was replaced afterwards.");
                    TestRunner.True((registration == "none") == (serialized[5] is AbilitySlot),
                        label + ": the public control was " + (registration == "none" ? "cleaned while unidentifiable" : "not cleaned once identifiable") + ".");
                    TestRunner.True(serialized[6] is AbilitySlot, label + ": an unregistered helper blueprint was treated as KMC.");
                }
        }

        // Review R1-F2: an observed, dismounted party rider live at 9; a selected load begins, an update runs with no player
        // world, the same world returns and the load is abandoned: 9 is on the bar, in an immediate save and after a cold
        // restore. Control: a real replacement releases the outgoing records.
        private static void MissingPlayerDuringCanceledLoad()
        {
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            var world = game.Current;
            var scope = new object();
            game.Toolbar.BeginSelectedLoad(scope);
            game.Current = null;
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "the missing-player update cleared the outgoing bar");
            TestRunner.True(game.Toolbar.ObservedCount > 0, "The missing-player update released the outgoing observations.");
            game.Current = world;
            game.Toolbar.AbandonSelectedLoad(scope);
            Update(game);
            Equal(new[] { 9 }, Controls(rider), "after abandonment");
            var file = Save(game);
            ExpectRows(file.Rows, "A", new[] { 9 }, 1, "immediate save after abandonment");
            Load(game, file);
            Equal(new[] { 9 }, Controls(Find(game, "A")), "cold restore");
            // Control: the load completes with a distinct world.
            game = new Game();
            Arranged(game, "A", 9);
            game.Toolbar.BeginSelectedLoad(new object());
            game.Current = null;
            Update(game);
            game.Current = new World("incoming");
            game.Party.Clear();
            Update(game);
            TestRunner.True(!game.Toolbar.HoldsLoad && game.Toolbar.ArrangementCount == 0 && game.Toolbar.ObservedCount == 0,
                "A completed replacement kept the outgoing records, observations or load.");
        }

        // Review R1-F3 (archived R14-F5): a pending 9 (a loaded save's row) whose older native bar still holds a stale binding
        // there; CleanupBadSlots rebinds it to the live fact during an open load; the load is abandoned and the player
        // removes the shortcut before the next update: the save holds a record only and nothing returns after a cold restore.
        // Control: native emptying instead of a rebind keeps 9 pending.
        private static void NativeRebindOfPendingSlot()
        {
            foreach (var rebind in new[] { true, false })
            {
                var game = new Game();
                Arranged(game, "A", 9);
                var file = Save(game);
                file.Bars["A"][9] = new AbilitySlot("Control", 7);
                game.Toolbar.BeginSelectedLoad(new object());
                game.Current = new World("loaded");
                var rider = new Unit("A", game.Current) { Desired = true, PetLink = "pet-A", Bar = ToNativeBar(file.Bars["A"]) };
                rider.Pet = new Unit("pet-A", game.Current) { IsPet = true, Master = rider };
                game.Party.Clear(); game.Party.Add(rider);
                game.Toolbar.SeedFromRows(file.Rows, true);
                int slot; bool pending;
                TestRunner.True(game.Toolbar.TryGetArrangement("A", out slot, out pending) && slot == 9 && pending, "The fixture is not a pending 9.");
                var scope = new object();
                game.Toolbar.BeginSelectedLoad(scope);
                if (rebind) { rider.ControlFact = ++game.Generation; rider.FactActive = true; }
                NativeCleanup(game, rider);
                TestRunner.True(rebind == (rider.Bar[9] is AbilitySlot), "Native did not " + (rebind ? "rebind" : "empty") + " 9.");
                game.Toolbar.AbandonSelectedLoad(scope);
                if (rebind) Remove(rider, 9);
                var saved = Save(game);
                ExpectRows(saved.Rows, "A", rebind ? new int[0] : new[] { 9 }, 1, "rebind=" + rebind + ": save");
                Load(game, saved);
                Equal(rebind ? new int[0] : new[] { 9 }, Controls(Find(game, "A")), "rebind=" + rebind + ": cold restore");
            }
        }

        // Review R1-F5: a remote (and separately a detached) owner that is also a retained pair member, whose current horse is
        // in neither the party nor the pair. The horse's stale Mount Primary slot and auto-use are cleared inside
        // serialization and by disable; its ordinary ability and another pet's ordinary auto-use survive.
        private static void PairCustodyOwnerPetSanitized()
        {
            foreach (var roster in new[] { "remote", "detached" })
                foreach (var boundary in new[] { "save", "disable" })
                {
                    var game = new Game();
                    var owner = Rider(game, "Owner", inParty: false);
                    if (roster == "detached") { game.Remote.Remove(owner); game.Detached.Add(owner); }
                    game.PairMembers.Add(owner);
                    var horse = owner.Pet;
                    TestRunner.True(!game.CurrentRiders().Contains(horse) && game.CurrentRiders().Contains(owner) &&
                        !game.ReserveOwners().Contains(owner), "The fixture owner is not in pair custody only.");
                    horse.Bar[2] = new AbilitySlot("MountPrimary", 1);
                    horse.Bar[4] = new AbilitySlot("Bite", 1);
                    horse.AutoUse = "KMC_MountPrimary";
                    var control = Rider(game, "Control");
                    control.Pet.AutoUse = "Bite";
                    if (boundary == "save")
                    {
                        var file = Save(game);
                        TestRunner.True(!(file.Bars[horse.Id][2] is AbilitySlot) && file.AutoUse[horse.Id] == null,
                            roster + ": serialization held the horse's managed slot or auto-use.");
                        TestRunner.True((file.Bars[horse.Id][4] as AbilitySlot)?.Kind == "Bite" && file.AutoUse[control.Pet.Id] == "Bite",
                            roster + ": serialization lost an ordinary ability or selection.");
                    }
                    else game.Toolbar.Disable();
                    TestRunner.True(!(horse.Bar[2] is AbilitySlot) && horse.AutoUse == null, roster + " " + boundary + ": the horse kept a managed reference.");
                    TestRunner.True((horse.Bar[4] as AbilitySlot)?.Kind == "Bite" && control.Pet.AutoUse == "Bite", roster + " " + boundary + ": an ordinary control changed.");
                }
        }

        // Review R1-F6 (archived R8-F1 first save): an older reserve bar with a Mounted Charge at 8 and the canonical binding at
        // 6; the rider has stock Charge available. Update-first and save-first give the stock Charge at 8 inside
        // serialization, after the release and after a cold load, beside the Mount / Dismount choice at 6.
        private static void LegacyChargeEitherOrder()
        {
            foreach (var saveFirst in new[] { true, false })
            {
                var game = new Game();
                var rider = Rider(game, "A", inParty: false);
                rider.HasStockCharge = true;
                StaleControl(rider, 6);
                rider.Bar[8] = new AbilitySlot("Charge", 1);
                if (!saveFirst) Update(game);
                var file = Save(game);
                TestRunner.True((file.Bars["A"][8] as AbilitySlot)?.Kind == "StockCharge", "saveFirst=" + saveFirst + ": serialization lost the Charge shortcut.");
                ExpectRows(file.Rows, "A", new[] { 6 }, 1, "saveFirst=" + saveFirst + ": the Mount / Dismount choice");
                TestRunner.True((rider.Bar[8] as AbilitySlot)?.Kind == "StockCharge", "saveFirst=" + saveFirst + ": the release lost the Charge shortcut.");
                Load(game, file);
                rider = Find(game, "A");
                LeaveCapital(game, rider);
                Update(game);
                TestRunner.True((rider.Bar[8] as AbilitySlot)?.Kind == "StockCharge", "saveFirst=" + saveFirst + ": the cold load lost the Charge shortcut.");
                Equal(new[] { 6 }, Controls(rider), "saveFirst=" + saveFirst + ": the Mount / Dismount choice after the cold load");
            }
        }

        // Review R1-F7: an observed rider (and its former pet) with a stale managed auto-use and no fact leaves every swept
        // roster; the departure clears both selections before releasing them; an ordinary selection survives.
        private static void DepartureClearsAutoUse()
        {
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            var stay = Arranged(game, "B", 4);
            rider.AutoUse = "KMC_RiderPrimary";
            rider.Pet.AutoUse = "KMC_MountPrimary";
            stay.AutoUse = "Fireball";
            game.Party.Remove(rider);
            game.Ex.Add(rider);
            Update(game);
            TestRunner.True(rider.AutoUse == null && rider.Pet.AutoUse == null, "The departed rider or its pet kept a managed auto-use.");
            TestRunner.True(game.Toolbar.ObservedUnits.All(unit => unit != rider && unit != rider.Pet), "The departed units are still observed.");
            TestRunner.True(stay.AutoUse == "Fireball", "An ordinary selection was cleared.");
        }

        // Review R1-F9: a loaded save seeds slot 9; an update runs with the public fact live while the bar is unavailable; the
        // bar then appears. 9 survives an intervening capture and returns exactly once.
        private static void UnavailableBarDefersRestore()
        {
            var game = new Game();
            Arranged(game, "A", 9);
            var file = Save(game);
            // The loaded rider's bar is not available when its rows are restored and its first update runs.
            Load(game, file, beforeRestore: g => Find(g, "A").HasBar = false);
            var rider = Find(game, "A");
            TestRunner.True(game.ControlLive(rider), "The fixture's fact is not live.");
            ExpectRows(game.Toolbar.CaptureRows(true), "A", new[] { 9 }, 1, "a capture while the bar is unavailable");
            rider.HasBar = true;
            Update(game); Update(game);
            Equal(new[] { 9 }, Controls(rider), "the deferred slot returned other than once");
        }

        // Review R1-F10: within one world an observed party object leaves and another object of the same actor (same pet)
        // is on the reserve roster; it places the control at 11. Save-first and update-first both keep only 11 and one record.
        private static void FormerInstanceKeepsReplacement()
        {
            foreach (var saveFirst in new[] { true, false })
            {
                var game = new Game();
                var former = Arranged(game, "A", 9);
                game.Party.Remove(former);
                var current = new Unit("A", game.Current) { Desired = true, PetLink = former.PetLink, Pet = former.Pet };
                game.Remote.Add(current);
                current.ControlFact = ++game.Generation; current.FactActive = true;
                LiveControl(current, 11);
                if (!saveFirst) Update(game);
                var file = Save(game);
                ExpectRows(file.Rows, "A", new[] { 11 }, 1, "saveFirst=" + saveFirst);
                TestRunner.True(!game.Toolbar.IsRetired("A"), "saveFirst=" + saveFirst + ": the former instance retired the actor.");
                LeaveCapital(game, current);
                Update(game);
                Equal(new[] { 11 }, Controls(current), "saveFirst=" + saveFirst + ": the current instance returns at 11");
            }
            // Round 2 (R1-F10): the former object stays in pair custody (a current rider by pair membership, off the roster).
            // A party replacement is saved at once; a reserve replacement goes through two updates first. Only 11 and one
            // marker, on save and after a cold restore; the former object's binding at 9 is cleaned.
            foreach (var replacementInParty in new[] { true, false })
            {
                var game = new Game();
                var former = Arranged(game, "A", 9);
                game.Party.Remove(former);
                game.PairMembers.Add(former);
                var current = new Unit("A", game.Current) { Desired = true, PetLink = former.PetLink };
                current.Pet = new Unit(former.PetLink, game.Current) { IsPet = true, Master = current };
                (replacementInParty ? game.Party : game.Remote).Add(current);
                current.ControlFact = ++game.Generation; current.FactActive = true;
                LiveControl(current, 11);
                TestRunner.True(game.CurrentRiders().Contains(former) && !game.OnPlayerRoster(former), "The former object is not in pair custody only.");
                if (!replacementInParty) { Update(game); Update(game); }
                var file = Save(game);
                ExpectRows(file.Rows, "A", new[] { 11 }, 1, "pair custody (replacement in party=" + replacementInParty + ")");
                TestRunner.True(!(former.Bar[9] is AbilitySlot), "The former object kept its public binding (party=" + replacementInParty + ").");
                Load(game, file);
                var loaded = Find(game, "A");
                LeaveCapital(game, loaded);
                Update(game);
                Equal(new[] { 11 }, Controls(loaded), "pair custody: cold restore (replacement in party=" + replacementInParty + ")");
            }
        }

        // Review R1-F11: a compatible archive holds kind-6 and kind-7 rows naming a current party horse beside the rider's own
        // rows. No pet arrangement or pet row survives a resave, and the rider still restores.
        private static void PetRowsOwnNothing()
        {
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            var file = Save(game);
            file.Rows = file.Rows.Concat(new[]
            {
                new MountedToolbarRow { ActorId = rider.Pet.Id, Index = 5, Kind = MountedControlSlotPolicy.SavedMountState },
                new MountedToolbarRow { ActorId = rider.Pet.Id, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord }
            }).ToList();
            Load(game, file);
            var resaved = Save(game);
            TestRunner.True(resaved.Rows.All(row => row.ActorId != rider.Pet.Id), "A pet row survived the resave.");
            TestRunner.True(!game.Toolbar.TryGetArrangement(rider.Pet.Id, out _, out _), "The pet owns an arrangement.");
            ExpectRows(resaved.Rows, "A", new[] { 9 }, 1, "the rider's rows");
            Equal(new[] { 9 }, Controls(Find(game, "A")), "the rider restores");
        }

        // Review R1-F11 (round 2): rows naming a horse are seeded while the horse is not yet among the player's characters
        // (its owner is not on the roster when the archive is restored); the owner then joins the party with it. The horse
        // owns no arrangement, marker or row afterwards; a rider that resolves late still restores.
        private static void LateResolvingPetPurged()
        {
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            var petId = rider.Pet.Id;
            var file = Save(game);
            file.Rows = file.Rows.Concat(new[]
            {
                new MountedToolbarRow { ActorId = petId, Index = 5, Kind = MountedControlSlotPolicy.SavedMountState },
                new MountedToolbarRow { ActorId = petId, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord },
                new MountedToolbarRow { ActorId = "L", Index = 4, Kind = MountedControlSlotPolicy.SavedMountState },
                new MountedToolbarRow { ActorId = "L", Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord }
            }).ToList();
            Unit owner = null;
            Load(game, file, beforeRestore: g => { owner = Find(g, "A"); g.Party.Remove(owner); });
            TestRunner.True(game.Toolbar.IsPlaced(petId), "The fixture did not admit the unresolved horse's rows.");
            game.Party.Add(owner);
            Update(game);
            TestRunner.True(!game.Toolbar.TryGetArrangement(petId, out _, out _) && !game.Toolbar.IsPlaced(petId), "The horse kept an arrangement or marker.");
            var late = Rider(game, "L");
            Update(game);
            Equal(new[] { 4 }, Controls(late), "the late-resolving rider did not restore");
            Equal(new[] { 9 }, Controls(owner), "the owner did not restore");
            var resaved = Save(game);
            TestRunner.True(resaved.Rows.All(row => row.ActorId != petId), "A pet row was saved.");
            ExpectRows(resaved.Rows, "L", new[] { 4 }, 1, "the late rider's rows");
        }

        // Review R2-F1: an older archive holds a canonical row at 3 and a legacy Dismount row at 9 for one rider; on the cold
        // load (autofill off) 3 holds Fireball and 9 is free. Fireball stays and the only shortcut is 9, after two updates,
        // a resave and a reload. Control: a marker-only archive restores nothing.
        private static void MixedArchiveRows()
        {
            var game = new Game();
            Arranged(game, "A", 9);
            var file = Save(game);
            file.Rows = new[]
            {
                new MountedToolbarRow { ActorId = "A", Index = 3, Kind = MountedControlSlotPolicy.SavedMountState },
                new MountedToolbarRow { ActorId = "A", Index = 9, Kind = MountedControlSlotPolicy.SavedDismount },
                new MountedToolbarRow { ActorId = "A", Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord }
            };
            file.Bars["A"][3] = new AbilitySlot("Fireball", 1);
            game.Autofill = false;
            Load(game, file);
            var rider = Find(game, "A");
            Update(game);
            TestRunner.True((rider.Bar[3] as AbilitySlot)?.Kind == "Fireball", "Fireball at 3 was overwritten.");
            Equal(new[] { 9 }, Controls(rider), "the usable older row at 9 did not restore as the only shortcut");
            var resaved = Save(game);
            ExpectRows(resaved.Rows, "A", new[] { 9 }, 1, "resave");
            Load(game, resaved);
            Equal(new[] { 9 }, Controls(Find(game, "A")), "reload");
            file.Rows = new[] { new MountedToolbarRow { ActorId = "A", Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord } };
            Load(game, file);
            Update(game);
            Equal(new int[0], Controls(Find(game, "A")), "a marker-only archive restored a shortcut");
        }

        // Review R2-F2: the incoming world's archive names A's slot 3 (with its marker); the incoming native bar holds stale
        // bindings at 3 and 9; GetSlot empties 3. Native read before the restore and after it give only 3 on an immediate save,
        // on the party return and after a cold restore. A marker-only incoming group beside a binding native recovered keeps
        // the recovered slot; an actual off-bar control restores nothing.
        private static void IncomingObservationBeforeRestore()
        {
            foreach (var nativeFirst in new[] { true, false })
            {
                var game = new Game();
                var rider = Arranged(game, "A", 9);
                EnterCapital(game, rider);
                Update(game);
                var file = Save(game);
                file.Rows = new[]
                {
                    new MountedToolbarRow { ActorId = "A", Index = 3, Kind = MountedControlSlotPolicy.SavedMountState },
                    new MountedToolbarRow { ActorId = "A", Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord }
                };
                file.Bars["A"][3] = new AbilitySlot("Control", 7);
                file.Bars["A"][9] = new AbilitySlot("Control", 8);
                Action<Game> read = g => NativeGetSlot(g, Find(g, "A"), 3);
                if (nativeFirst) Load(game, file, beforeRestore: read); else Load(game, file, afterSeed: read);
                var saved = Save(game);
                ExpectRows(saved.Rows, "A", new[] { 3 }, 1, "nativeFirst=" + nativeFirst + ": immediate save");
                rider = Find(game, "A");
                LeaveCapital(game, rider);
                Update(game);
                Equal(new[] { 3 }, Controls(rider), "nativeFirst=" + nativeFirst + ": party return");
                Load(game, saved);
                rider = Find(game, "A");
                LeaveCapital(game, rider);
                Update(game);
                Equal(new[] { 3 }, Controls(rider), "nativeFirst=" + nativeFirst + ": cold restore");
            }
            foreach (var recovered in new[] { true, false })
            {
                var game = new Game();
                var rider = Arranged(game, "A", 9);
                Remove(rider, 9);
                Update(game);
                EnterCapital(game, rider);
                Update(game);
                var file = Save(game);
                ExpectRows(file.Rows, "A", new int[0], 1, "the incoming group is marker-only");
                if (recovered) file.Bars["A"][5] = new AbilitySlot("Control", 7);
                Load(game, file, beforeRestore: g => { if (recovered) NativeGetSlot(g, Find(g, "A"), 5); });
                ExpectRows(Save(game).Rows, "A", recovered ? new[] { 5 } : new int[0], 1, "recovered=" + recovered);
                rider = Find(game, "A");
                LeaveCapital(game, rider);
                Update(game);
                Equal(recovered ? new[] { 5 } : new int[0], Controls(rider), "recovered=" + recovered + ": party return");
            }
        }

        // Review R2-F3: A's arrangement at 9 for pet A; the bar becomes unavailable; the link changes to pet B; a save before
        // the bar returns carries no slot, and a cold restore gives B no shortcut. With the same pet, the save carries 9 and
        // the cold restore returns it exactly once.
        private static void CompanionReplacedWhileBarHidden()
        {
            foreach (var replaced in new[] { true, false })
            {
                var game = new Game();
                var rider = Arranged(game, "A", 9);
                rider.Bar = new Slot[BarSize];
                rider.HasBar = false;
                if (replaced)
                {
                    rider.PetLink = "pet-B";
                    rider.Pet = new Unit("pet-B", game.Current) { IsPet = true, Master = rider };
                }
                var file = Save(game);
                ExpectRows(file.Rows, "A", replaced ? new int[0] : new[] { 9 }, 1, "replaced=" + replaced + ": save while the bar is unavailable");
                Load(game, file);
                Update(game);
                Equal(replaced ? new int[0] : new[] { 9 }, Controls(Find(game, "A")), "replaced=" + replaced + ": cold restore");
            }
        }

        // Review R2-F4: GetSlot writes its empty slot at 9 and throws (no postfix); an update runs with no player during a
        // selected load; the same world returns and the load is abandoned: row 9 and one restored shortcut. Controls: a true
        // replacement and an unfenced main menu drop the operation.
        private static void FencedThrownNativeWrite()
        {
            var game = new Game();
            var rider = Arranged(game, "A", 9);
            rider.FactActive = false;
            NativeGetSlot(game, rider, 9, throws: true);
            var world = game.Current;
            var scope = new object();
            game.Toolbar.BeginSelectedLoad(scope);
            game.Current = null;
            Update(game);
            TestRunner.True(game.Toolbar.HasOpenNativeOperation, "The thrown operation was dropped at the missing-player update.");
            game.Current = world;
            game.Toolbar.AbandonSelectedLoad(scope);
            rider.FactActive = true;
            ExpectRows(Save(game).Rows, "A", new[] { 9 }, 1, "the outgoing world's completed native emptying");
            Equal(new[] { 9 }, Controls(rider), "one restored shortcut at 9");
            foreach (var replacement in new[] { true, false })
            {
                game = new Game();
                rider = Arranged(game, "A", 9);
                rider.FactActive = false;
                NativeGetSlot(game, rider, 9, throws: true);
                if (replacement) game.Toolbar.BeginSelectedLoad(new object());
                game.Current = replacement ? new World("incoming") : null;
                // A replaced world has its own roster; the outgoing objects are no longer the player's characters.
                game.Party.Clear();
                Update(game);
                TestRunner.True(!game.Toolbar.HasOpenNativeOperation && game.Toolbar.ArrangementCount == 0,
                    (replacement ? "A replacement" : "The main menu") + " kept the thrown operation or the records.");
            }
        }

        // Review R2-F5: (a) 3 is pending with a known stale copy at 9; CleanupBadSlots rebinds 9 to the live fact and throws:
        // the rebound copy stays a copy, so only 3 is saved and returns. (b) GetSlot empties 3 and throws; the player places
        // the live control at 11 before the operation is settled: only 11. The throw-before-write removal control is in
        // NativeOperationEdges.
        private static void ThrownCleanupEdges()
        {
            var game = new Game();
            var rider = Unobserved(game, "A");
            StaleControl(rider, 3); StaleControl(rider, 9, 8);
            NativeGetSlot(game, rider, 3);
            game.Toolbar.BeginNativeSlotOperation(rider, -1);
            rider.Bar[9] = new AbilitySlot("Control", rider.ControlFact);
            ExpectRows(Save(game).Rows, "A", new[] { 3 }, 1, "(a) a thrown rebind of the known copy");
            Update(game);
            Equal(new[] { 3 }, Controls(rider), "(a) only 3 returns");
            game = new Game();
            rider = Arranged(game, "A", 3);
            rider.FactActive = false;
            NativeGetSlot(game, rider, 3, throws: true);
            rider.FactActive = true;
            LiveControl(rider, 11);
            ExpectRows(Save(game).Rows, "A", new[] { 11 }, 1, "(b) a placement after a thrown emptying");
            Update(game);
            Equal(new[] { 11 }, Controls(rider), "(b) only 11");
        }

        // Review R2-F6: GetSlot empties 3 on a never-scanned reserve rider and records the stale copy at 9. The reserve sweep
        // releases the copy at once. Separately, the rider leaves the roster before its first sweep with the same world
        // current: the lifecycle keeps no strong reference to the copy's slot object (it is collected once the bar drops it).
        // A legitimate return restores 3 once.
        private static void ReserveCopiesReleased()
        {
            var game = new Game();
            var rider = Rider(game, "A", inParty: false);
            StaleControl(rider, 3); StaleControl(rider, 9, 8);
            NativeGetSlot(game, rider, 3);
            TestRunner.Equal(1, game.Toolbar.KnownCopyCount("A"), "The copy at 9 was not recorded.");
            Update(game);
            TestRunner.Equal(0, game.Toolbar.KnownCopyCount("A"), "The reserve sweep kept the cleared copy.");
            // Removal before the first sweep: the rider leaves every roster; its bar later drops the slot object.
            game = new Game();
            rider = Rider(game, "A", inParty: false);
            StaleControl(rider, 3);
            var copy = PutCollectableCopy(rider, 9);
            NativeGetSlot(game, rider, 3);
            TestRunner.Equal(1, game.Toolbar.KnownCopyCount("A"), "The copy at 9 was not recorded (removal case).");
            game.Remote.Remove(rider);
            Update(game);
            rider.Bar[9] = null;
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            TestRunner.True(!copy.IsAlive, "The lifecycle kept the discarded copy's slot object alive.");
            // A legitimate return: the rider rejoins with its bar (the copy still on it) and gets 3 back once.
            game = new Game();
            rider = Rider(game, "A", inParty: false);
            StaleControl(rider, 3); StaleControl(rider, 9, 8);
            NativeGetSlot(game, rider, 3);
            game.Remote.Remove(rider);
            Update(game);
            game.Party.Add(rider);
            Update(game); Update(game);
            Equal(new[] { 3 }, Controls(rider), "the returning rider did not get 3 back once");
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static WeakReference PutCollectableCopy(Unit unit, int index)
        {
            var slot = new AbilitySlot("Control", 8);
            unit.Bar[index] = slot;
            return new WeakReference(slot);
        }
    }
}
