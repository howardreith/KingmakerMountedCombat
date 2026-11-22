using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // What one toolbar slot holds, as the toolbar lifecycle needs to know it.
    public enum MountedToolbarSlot
    {
        // An empty slot (native MechanicActionBarSlotEmpty, or null).
        Empty,
        // Anything that is not an exact KMC binding: an ordinary ability, an item, a spell.
        Ordinary,
        // The canonical public Mount / Dismount control (bound to any AbilityData of its blueprint, live or stale).
        PublicControl,
        // An older exact Mount or Dismount binding that F2 migrates to the public control.
        LegacyAlias,
        // Any other exact KMC binding (Rider Primary, Mount Primary, an older Mounted Charge).
        OtherManaged
    }

    // The boundary a sweep belongs to. The lifecycle decides who is swept and in which order; the host decides what a
    // sweep writes for the bindings and references that are not the public control (a save leases them back, the
    // reserve sweep migrates an older Mounted Charge, disable clears every exact KMC reference).
    public enum MountedToolbarBoundary
    {
        Update,
        ReserveSweep,
        Departure,
        AreaUnload,
        Save,
        Disable
    }

    // The narrow game adapter of the toolbar lifecycle: reads and writes native bars, facts, rosters and the player world.
    // It holds no toolbar decision. NativeMountedControlService implements it for Kingmaker; the component tests implement
    // it with thin fakes.
    public interface IMountedToolbarHost<TUnit> where TUnit : class
    {
        // The current player world (Game.Instance.Player), or null between worlds and at the main menu.
        object CurrentWorld { get; }
        // The units KMC reconciles now: party characters, their pets and every current pair member.
        IList<TUnit> CurrentRiders();
        // The current world's reserve (remote) and detached player characters that are not current riders.
        IList<TUnit> ReserveOwners();
        // The owner's exact current-world persistent pet (its master is the owner), or null.
        TUnit PersistentPet(TUnit owner);
        bool IsPet(TUnit unit);
        string ActorId(TUnit unit);
        // The persistent pet link (UnitDescriptor.PetUniqueId), or null/empty.
        string PetLink(TUnit unit);
        // This exact unit instance is one of the current world's player characters (party, reserve or detached).
        bool OnPlayerRoster(TUnit unit);
        // The one current-world player character with this actor id that an archive row may name, or null.
        TUnit ResolveCharacter(string actorId);
        // The rider gets the public control now (ShouldLease).
        bool ControlDesired(TUnit unit);
        // The public control fact is present and active.
        bool ControlLive(TUnit unit);
        // Called once per reconciled rider in each update, after its bar has been observed: the adapter's fact leasing and
        // its unchanged per-rider repairs (retired helpers, older Mounted Charge).
        void PrepareRider(TUnit unit);
        // 0 when the unit has no bar.
        int SlotCount(TUnit unit);
        MountedToolbarSlot ReadSlot(TUnit unit, int index);
        // The slot object (reference identity).
        object SlotIdentity(TUnit unit, int index);
        // The slot holds what native GetSlot / CleanupBadSlots write when they empty a slot (an empty slot bound to the
        // unit); the player's drag-off writes null instead.
        bool IsNativeEmptySlot(TUnit unit, int index);
        // A public binding (canonical or alias) holding its live fact's own data.
        bool HoldsLiveBinding(TUnit unit, int index);
        // Binds the canonical public control to its live fact's data.
        void BindControl(TUnit unit, int index);
        void ClearSlot(TUnit unit, int index);
        void BarChanged(TUnit unit);
        void SanitizeOtherBindings(TUnit unit, MountedToolbarBoundary boundary);
        void ClearManagedAutoUse(TUnit unit, MountedToolbarBoundary boundary);
        void RemoveManagedFacts(TUnit unit);
        // The native autofill setting is on and a selected load's bindings are settled.
        bool InitialPlacementAllowed(TUnit unit);
        // The first empty main-bar slot, or -1 when the main bar is full.
        int SelectInitialPlacementSlot(TUnit unit);
    }

    // One public archive row (MountedControlSlotPolicy kinds 6 and 7, or an older Mount / Dismount kind when read).
    public struct MountedToolbarRow
    {
        public string ActorId;
        public int Index;
        public int Kind;
    }

    // The one production owner of the Mount / Dismount toolbar lifecycle: every decision about the player's arrangement of
    // the public control and its sequencing and actor selection at each boundary (update, native slot invalidation, save
    // header and scope, area suspension, selected load, world disposal, companion replacement and retirement, disable). The
    // host only reads and writes native state.
    //
    // The model (MountedControlPlacementIntent): a rider's arrangement is the set of toolbar slots where the player keeps
    // the public control, plus the subset that is pending: emptied by native bad-slot cleanup or by KMC's own sweeps, not by
    // the player, and therefore owed back. Everything follows from one rule applied at every boundary before anything
    // destroys the evidence: observe the bar, then mark what the boundary empties as pending (ApplyBar holds the evidence
    // rule). Pending slots return, each once, into empty slots when the control is live again. KMC never writes the public
    // control into a slot outside the arrangement except the one automatic placement.
    //
    // Records are kept per player world (held weakly) and identify actors by id: a selected load that is abandoned keeps its
    // world and so its records unchanged; a replaced world, or one disposed without an open selected load, releases them.
    public sealed class MountedToolbarLifecycle<TUnit> where TUnit : class
    {
        private sealed class ObservedUnit
        {
            internal TUnit Unit;
            internal WeakReference World;
        }

        private sealed class NativeEntry
        {
            internal int Index;
            internal MountedToolbarSlot Content;
            internal object Identity;
        }

        private sealed class NativeOperation
        {
            internal TUnit Unit;
            internal WeakReference World;
            internal List<NativeEntry> Entries;
        }

        private readonly IMountedToolbarHost<TUnit> host;
        private MountedControlPlacementIntent records;
        private object loadOwner;
        private readonly List<ObservedUnit> observed = new List<ObservedUnit>();
        private NativeOperation nativeOperation;

        public MountedToolbarLifecycle(IMountedToolbarHost<TUnit> host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public bool Enabled { get; private set; }

        public long RestoreCount { get; private set; }
        public long NativeInvalidationCount { get; private set; }
        public long AliasMigrationCount { get; private set; }
        public long InitialPlacementCount { get; private set; }
        public long InitialPlacementFullBarCount { get; private set; }
        public long RetirementCount { get; private set; }

        // Read-only views for diagnostics and tests.
        public int ArrangementCount => records?.Count ?? 0;
        public bool HoldsWorld => records != null;
        public bool HoldsLoad => loadOwner != null;
        public bool HasOpenNativeOperation => nativeOperation != null;
        public int ObservedCount => observed.Count;
        public IList<TUnit> ObservedUnits => observed.Select(item => item.Unit).ToList();
        public bool IsPlaced(string actorId) => records != null && records.IsPlaced(actorId);
        public bool IsRetired(string actorId) => records != null && records.IsRetired(actorId);

        public bool TryGetArrangement(string actorId, out int[] slots, out int[] pending)
        {
            var arrangement = records?.Get(actorId);
            slots = arrangement?.Slots.ToArray();
            pending = arrangement?.Pending.ToArray();
            return arrangement != null;
        }

        // ---- enable, disable, disposal -------------------------------------------------------------------------------

        public void Enable()
        {
            Enabled = true;
            SyncWorld();
        }

        // Disable and disposal are cleanup, not a temporary absence: every exact KMC binding, auto-use selection and fact
        // leaves every unit the sanitation sweep covers, and nothing is remembered afterwards.
        public void Disable()
        {
            nativeOperation = null;
            var riders = host.CurrentRiders() ?? new List<TUnit>();
            foreach (var unit in SanitationUnits(riders))
            {
                ClearPublicBindings(unit, false);
                host.SanitizeOtherBindings(unit, MountedToolbarBoundary.Disable);
                host.ClearManagedAutoUse(unit, MountedToolbarBoundary.Disable);
                host.RemoveManagedFacts(unit);
            }
            observed.Clear();
            records = null;
            loadOwner = null;
            Enabled = false;
        }

        // ---- world lifetime ------------------------------------------------------------------------------------------

        // Runs at every entry point before anything reads or writes the records. A distinct player world (a selected load
        // completed, or a new game) releases the previous world's records; no player world at all releases them unless a
        // selected load is still open (it may be abandoned with the outgoing world current again).
        private void SyncWorld()
        {
            var world = host.CurrentWorld;
            if (world == null)
            {
                if (loadOwner == null) ReleaseRecords();
                return;
            }
            if (records != null && records.BelongsTo(world)) return;
            ReleaseRecords();
            loadOwner = null;
            if (Enabled) records = new MountedControlPlacementIntent(world);
        }

        private void ReleaseRecords()
        {
            records = null;
            nativeOperation = null;
        }

        // A selected native load begins (its iterator's first step). Nothing is moved or cleared: the outgoing world keeps
        // its records until a distinct world replaces it.
        public void BeginSelectedLoad(object owner)
        {
            if (!Enabled || owner == null) return;
            loadOwner = owner;
        }

        // The owning load scope ended without native completion (canceled, failed or superseded). Only that scope can end
        // it. If the outgoing world is current, its records are simply still there.
        public void AbandonSelectedLoad(object owner)
        {
            if (owner == null || !ReferenceEquals(loadOwner, owner)) return;
            loadOwner = null;
            SyncWorld();
        }

        // Diagnostics only: forget the current world's placement records, as a freshly loaded world starts.
        public void ResetPlacementRecords() => records?.Clear();

        // Every update and boundary begins here, before the host's own early returns: an aborted native operation is
        // settled and the records follow the current world.
        public void Housekeeping()
        {
            SettleNativeOperation();
            SyncWorld();
        }

        // ---- update --------------------------------------------------------------------------------------------------

        public void Reconcile()
        {
            Housekeeping();
            if (!Enabled) return;
            var riders = host.CurrentRiders() ?? new List<TUnit>();
            foreach (var rider in riders) ReconcileRider(rider);
            ProcessDepartures(riders);
            SweepReserve(riders);
            RevalidateAbsent(riders);
        }

        private void ReconcileRider(TUnit unit)
        {
            if (unit == null) return;
            Track(unit);
            // The bar as the player left it is the evidence, before the host leases, withdraws or replaces any fact.
            Observe(unit);
            host.PrepareRider(unit);
            var live = host.ControlLive(unit);
            RepairBar(unit, live);
            if (live)
            {
                RestorePending(unit);
                TryInitialPlacement(unit);
            }
            host.ClearManagedAutoUse(unit, MountedToolbarBoundary.Update);
        }

        // A stale canonical binding is rebound to the live data. An older alias inside the arrangement becomes the public
        // control in place while the control is live; otherwise it is cleared (its slot pending when it is the arrangement).
        // An alias outside the arrangement is a leftover of F2's one-control migration and is cleared.
        private void RepairBar(TUnit unit, bool live)
        {
            var count = host.SlotCount(unit);
            if (count == 0) return;
            var arrangement = Get(unit);
            var changed = false;
            for (var index = 0; index < count; index++)
            {
                var content = host.ReadSlot(unit, index);
                if (content == MountedToolbarSlot.PublicControl)
                {
                    if (!live || host.HoldsLiveBinding(unit, index)) continue;
                    host.BindControl(unit, index);
                    changed = true;
                    continue;
                }
                if (content != MountedToolbarSlot.LegacyAlias) continue;
                var inArrangement = arrangement != null && arrangement.Slots.Contains(index);
                if (live && inArrangement)
                {
                    host.BindControl(unit, index);
                    AliasMigrationCount++;
                }
                else
                {
                    host.ClearSlot(unit, index);
                    if (inArrangement) arrangement.Pending.Add(index);
                }
                changed = true;
            }
            if (changed) host.BarChanged(unit);
        }

        private void RestorePending(TUnit unit)
        {
            var arrangement = Get(unit);
            if (arrangement == null || arrangement.Pending.Count == 0) return;
            var count = host.SlotCount(unit);
            var changed = false;
            foreach (var index in arrangement.Pending.ToArray())
            {
                if (index >= count)
                {
                    arrangement.Pending.Remove(index);
                    arrangement.Slots.Remove(index);
                    continue;
                }
                if (host.ReadSlot(unit, index) != MountedToolbarSlot.Empty) continue;
                host.BindControl(unit, index);
                arrangement.Pending.Remove(index);
                RestoreCount++;
                if (arrangement.FromAliasRow.Remove(index)) AliasMigrationCount++;
                changed = true;
            }
            if (changed) host.BarChanged(unit);
        }

        // The native new-ability placement, once per actor and world, into the first empty main-bar slot; a full main bar
        // records the attempt as an off-bar arrangement.
        private void TryInitialPlacement(TUnit unit)
        {
            var id = host.ActorId(unit);
            if (id == null || records == null || host.IsPet(unit) || records.Get(id) != null || records.IsPlaced(id)) return;
            if (BarHoldsPublicBinding(unit) || !host.InitialPlacementAllowed(unit)) return;
            var arrangement = records.Create(id, host.PetLink(unit));
            arrangement.KnownAliases = new HashSet<int>();
            var index = host.SelectInitialPlacementSlot(unit);
            if (index < 0) { InitialPlacementFullBarCount++; return; }
            host.BindControl(unit, index);
            arrangement.Slots.Add(index);
            host.BarChanged(unit);
            InitialPlacementCount++;
        }

        // A previously reconciled unit that is no longer a current rider. Its bar is observed first; still on the player's
        // roster it is a temporary absence (its public slots pending), otherwise permanent loss (retired). Its KMC bindings
        // and facts go either way. An earlier world's object, or a character present now under another object, is only
        // cleaned up: it never authors anything in the current world.
        private void ProcessDepartures(IList<TUnit> riders)
        {
            foreach (var item in observed.ToArray())
            {
                if (riders.Contains(item.Unit)) continue;
                var unit = item.Unit;
                if (OwnsRecords(item, riders))
                {
                    Observe(unit);
                    if (host.OnPlayerRoster(unit)) ClearPublicBindings(unit, true);
                    else RetireAndClear(unit);
                }
                else ClearPublicBindings(unit, false);
                host.SanitizeOtherBindings(unit, MountedToolbarBoundary.Departure);
                host.RemoveManagedFacts(unit);
                observed.Remove(item);
            }
        }

        // Reserve and detached owners are observed every update and their KMC bindings cleared (their public slots pending),
        // so native serialization and a disabled KMC never meet them. Their exact persistent pets are swept too.
        private void SweepReserve(IList<TUnit> riders)
        {
            foreach (var owner in host.ReserveOwners() ?? new List<TUnit>())
            {
                if (owner == null || riders.Contains(owner)) continue;
                Observe(owner);
                ClearPublicBindings(owner, true);
                host.SanitizeOtherBindings(owner, MountedToolbarBoundary.ReserveSweep);
                var pet = host.PersistentPet(owner);
                if (pet == null || riders.Contains(pet)) continue;
                ClearPublicBindings(pet, false);
                host.SanitizeOtherBindings(pet, MountedToolbarBoundary.ReserveSweep);
            }
        }

        // An arrangement of an actor that is not a current rider is retired on positive evidence of permanent loss: the
        // character left the player's roster, or its pet link is cleared (while it does not get the control) or names
        // another companion. An actor that cannot be resolved proves nothing and keeps its arrangement.
        private void RevalidateAbsent(IList<TUnit> riders)
        {
            if (records == null || records.Count == 0) return;
            var riderIds = new HashSet<string>(riders.Where(unit => unit != null).Select(host.ActorId).Where(id => id != null), StringComparer.Ordinal);
            foreach (var id in records.ActorIds)
            {
                if (riderIds.Contains(id)) continue;
                var unit = host.ResolveCharacter(id);
                if (unit == null) continue;
                if (!host.OnPlayerRoster(unit) || CompanionLost(unit, records.Get(id))) Retire(id);
            }
        }

        // ---- native slot invalidation --------------------------------------------------------------------------------

        // Native UnitUISettings.GetSlot(index) (one slot) or CleanupBadSlots (index -1: the whole bar) begins. The public
        // bindings in its range are noted with their slot objects. A previous operation that never returned is settled
        // first.
        public void BeginNativeSlotOperation(TUnit unit, int index)
        {
            SettleNativeOperation();
            if (!Enabled || unit == null || host.IsPet(unit)) return;
            var count = host.SlotCount(unit);
            if (count == 0 || index >= count) return;
            var entries = new List<NativeEntry>();
            var last = index < 0 ? count - 1 : index;
            for (var at = index < 0 ? 0 : index; at <= last; at++)
            {
                var content = host.ReadSlot(unit, at);
                if (content == MountedToolbarSlot.PublicControl || content == MountedToolbarSlot.LegacyAlias)
                    entries.Add(new NativeEntry { Index = at, Content = content, Identity = host.SlotIdentity(unit, at) });
            }
            if (entries.Count == 0) return;
            nativeOperation = new NativeOperation { Unit = unit, World = new WeakReference(host.CurrentWorld), Entries = entries };
        }

        // The native operation returned normally: every noted slot native replaced with anything but a public binding (a
        // rebind keeps it) was emptied by native.
        public void EndNativeSlotOperation(TUnit unit)
        {
            var operation = nativeOperation;
            nativeOperation = null;
            if (operation != null) ApplyNativeOperation(operation, ReferenceEquals(operation.Unit, unit));
        }

        // An operation that never returned (it threw): only a slot holding native's own empty write counts.
        private void SettleNativeOperation()
        {
            var operation = nativeOperation;
            nativeOperation = null;
            if (operation != null) ApplyNativeOperation(operation, false);
        }

        // Native emptied these slots: the bar as it stood just before is observed (the player's latest arrangement), then
        // the emptied slots of the arrangement are pending. Only in the world that produced the operation and only for a
        // unit on its roster.
        private void ApplyNativeOperation(NativeOperation operation, bool returned)
        {
            if (!Enabled) return;
            var world = host.CurrentWorld;
            var unit = operation.Unit;
            if (world == null || !ReferenceEquals(operation.World.Target, world) || !host.OnPlayerRoster(unit)) return;
            SyncWorld();
            var count = host.SlotCount(unit);
            var before = new Dictionary<int, MountedToolbarSlot>();
            foreach (var entry in operation.Entries)
            {
                if (entry.Index >= count || ReferenceEquals(host.SlotIdentity(unit, entry.Index), entry.Identity)) continue;
                var now = host.ReadSlot(unit, entry.Index);
                if (now == MountedToolbarSlot.PublicControl || now == MountedToolbarSlot.LegacyAlias) continue;
                if (!returned && !host.IsNativeEmptySlot(unit, entry.Index)) continue;
                before[entry.Index] = entry.Content;
            }
            if (before.Count == 0) return;
            Observe(unit, before);
            var arrangement = Get(unit);
            if (arrangement == null) return;
            foreach (var index in before.Keys)
            {
                if (!arrangement.Slots.Contains(index)) continue;
                arrangement.Pending.Add(index);
                NativeInvalidationCount++;
            }
        }

        // ---- save ----------------------------------------------------------------------------------------------------

        // The archive rows of the public control. includeAbsent (the owned save archive): every bar is observed first, then
        // each arrangement of an actor that resolves as a player character writes one kind-6 row per slot (live or
        // pending), and each placed actor one placement record. Otherwise (in-world captures) only live callable public
        // bindings of current riders are written, with their placement record. A disabled service writes nothing.
        public IList<MountedToolbarRow> CaptureRows(bool includeAbsent)
        {
            var rows = new List<MountedToolbarRow>();
            Housekeeping();
            if (!Enabled || records == null) return rows;
            var riders = host.CurrentRiders() ?? new List<TUnit>();
            if (!includeAbsent)
            {
                foreach (var rider in riders) CaptureLiveRows(rider, rows);
                return rows;
            }
            foreach (var unit in PlacementUnits(riders)) Observe(unit);
            RetireDepartedFormerRiders(riders);
            RevalidateAbsent(riders);
            var ids = new SortedSet<string>(records.ActorIds.Concat(records.PlacedIds), StringComparer.Ordinal);
            foreach (var id in ids)
            {
                if (host.ResolveCharacter(id) == null) continue;
                var arrangement = records.Get(id);
                if (arrangement != null)
                    foreach (var index in arrangement.Slots)
                        if (MountedControlSlotPolicy.IsValidSavedRow(index, MountedControlSlotPolicy.SavedMountState))
                            rows.Add(new MountedToolbarRow { ActorId = id, Index = index, Kind = MountedControlSlotPolicy.SavedMountState });
                if (records.IsPlaced(id))
                    rows.Add(new MountedToolbarRow { ActorId = id, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord });
            }
            return rows;
        }

        private void CaptureLiveRows(TUnit unit, List<MountedToolbarRow> rows)
        {
            var id = unit == null ? null : host.ActorId(unit);
            if (id == null || host.IsPet(unit)) return;
            var live = host.ControlLive(unit);
            var wrote = false;
            if (live)
                foreach (var index in PublicIndices(unit))
                {
                    if (!MountedControlSlotPolicy.IsValidSavedRow(index, MountedControlSlotPolicy.SavedMountState) || !host.HoldsLiveBinding(unit, index)) continue;
                    rows.Add(new MountedToolbarRow { ActorId = id, Index = index, Kind = MountedControlSlotPolicy.SavedMountState });
                    wrote = true;
                }
            if (records.IsPlaced(id) && (live || wrote))
                rows.Add(new MountedToolbarRow { ActorId = id, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord });
        }

        // The save scope begins: every bar is observed, then every unit the sanitation sweep covers loses its KMC bindings
        // (public slots pending; the host leases the others back), its exact KMC auto-use selection and its KMC facts, so
        // native serialization holds no runtime reference. A disabled service still sanitizes, recording nothing.
        public void BeginSave()
        {
            Housekeeping();
            SweepBoundary(MountedToolbarBoundary.Save);
        }

        // ---- area ----------------------------------------------------------------------------------------------------

        public void SuspendArea()
        {
            Housekeeping();
            SweepBoundary(MountedToolbarBoundary.AreaUnload);
            observed.Clear();
        }

        private void SweepBoundary(MountedToolbarBoundary boundary)
        {
            var riders = host.CurrentRiders() ?? new List<TUnit>();
            var owners = PlacementUnits(riders);
            foreach (var unit in owners) Observe(unit);
            RetireDepartedFormerRiders(riders);
            foreach (var unit in SanitationUnits(riders))
            {
                // Only the current world's own unit objects owe their slots back; an earlier world's object is only cleaned.
                ClearPublicBindings(unit, owners.Contains(unit));
                host.SanitizeOtherBindings(unit, boundary);
                host.ClearManagedAutoUse(unit, boundary);
                host.RemoveManagedFacts(unit);
            }
        }

        // ---- load ----------------------------------------------------------------------------------------------------

        // Archive rows restored into the current world. loadedWorld: a freshly loaded save initialises each named actor's
        // arrangement (every saved slot pending until the control is live). Otherwise the rows replay a capture made
        // earlier in this world (area transfer, retained remount) and seed only an actor with no arrangement that was not
        // retired since. Several older Mount / Dismount rows collapse to one (F2's migration): the first whose slot can
        // hold the control on a fresh load.
        public void SeedFromRows(IEnumerable<MountedToolbarRow> rows, bool loadedWorld)
        {
            Housekeeping();
            if (!Enabled || records == null || rows == null) return;
            foreach (var group in rows.Where(row => row.ActorId != null).GroupBy(row => row.ActorId, StringComparer.Ordinal))
            {
                var id = group.Key;
                var placed = group.Any(row => row.Kind == MountedControlSlotPolicy.SavedPlacementRecord);
                var slots = group.Where(row => row.Kind == MountedControlSlotPolicy.SavedMountState).Select(row => row.Index).Distinct().ToList();
                var aliases = group.Where(row => row.Kind != MountedControlSlotPolicy.SavedMountState &&
                    MountedControlSlotPolicy.IsPublicBindingRow(row.Kind)).Select(row => row.Index).ToList();
                var fromAlias = slots.Count == 0 && aliases.Count > 0;
                if (fromAlias)
                {
                    var unit = host.ResolveCharacter(id);
                    slots.Add(MountedControlSlotPolicy.SelectSeededPublicRow(aliases, index => Restorable(unit, index), loadedWorld));
                }
                if (slots.Count == 0 && !placed) continue;
                if (placed) records.MarkPlaced(id);
                if (!loadedWorld && (records.Get(id) != null || records.IsRetired(id))) continue;
                var arrangement = records.Create(id, null);
                foreach (var index in slots.Where(index => index >= 0))
                {
                    arrangement.Slots.Add(index);
                    arrangement.Pending.Add(index);
                    if (fromAlias) arrangement.FromAliasRow.Add(index);
                }
            }
        }

        private bool Restorable(TUnit unit, int index)
        {
            if (unit == null || index < 0 || index >= host.SlotCount(unit)) return false;
            var content = host.ReadSlot(unit, index);
            return content == MountedToolbarSlot.Empty || content == MountedToolbarSlot.PublicControl || content == MountedToolbarSlot.LegacyAlias;
        }

        // ---- observation ---------------------------------------------------------------------------------------------

        // The rider's bar now is the evidence of the player's arrangement. nativeBefore: slots a native operation has just
        // emptied, read as they were before (the bar the player last left). The companion identity is checked first: a pet
        // link naming another companion, or cleared while the rider does not get the control, retires the arrangement and
        // clears the rider's public bindings, so a later companion never inherits them. Pets own no arrangement.
        private void Observe(TUnit unit, IDictionary<int, MountedToolbarSlot> nativeBefore = null)
        {
            if (!Enabled || records == null || unit == null || host.IsPet(unit)) return;
            var id = host.ActorId(unit);
            var count = host.SlotCount(unit);
            if (id == null || count == 0) return;
            // The common per-frame case, a unit with no arrangement and no public binding, is decided without allocating.
            if (nativeBefore == null && records.Get(id) == null && !BarHoldsPublicBinding(unit)) return;
            var canonical = new SortedSet<int>();
            var aliases = new SortedSet<int>();
            var occupied = new HashSet<int>();
            for (var index = 0; index < count; index++)
            {
                MountedToolbarSlot content;
                if (nativeBefore == null || !nativeBefore.TryGetValue(index, out content)) content = host.ReadSlot(unit, index);
                if (content == MountedToolbarSlot.PublicControl) canonical.Add(index);
                else if (content == MountedToolbarSlot.LegacyAlias) aliases.Add(index);
                else if (content != MountedToolbarSlot.Empty) occupied.Add(index);
            }
            var arrangement = records.Get(id);
            var link = host.PetLink(unit);
            if (arrangement != null ? CompanionLost(unit, arrangement) :
                canonical.Count + aliases.Count > 0 && string.IsNullOrEmpty(link) && !host.ControlDesired(unit))
            {
                RetireAndClear(unit);
                return;
            }
            if (arrangement == null)
            {
                // Older aliases stand for the control only on a bar without a canonical binding: the first one (F2).
                var evidence = canonical.Count > 0 ? canonical.ToList() : aliases.Take(1).ToList();
                if (evidence.Count == 0) return;
                arrangement = records.Create(id, link);
                arrangement.Slots.UnionWith(evidence);
                arrangement.KnownAliases = new HashSet<int>(aliases.Where(index => !arrangement.Slots.Contains(index)));
                return;
            }
            if (arrangement.PetId == null && !string.IsNullOrEmpty(link)) arrangement.PetId = link;
            MountedControlPlacementIntent.ApplyBar(arrangement, canonical, aliases, occupied, count);
            records.MarkPlaced(id);
        }

        private bool CompanionLost(TUnit unit, MountedControlPlacementIntent.Arrangement arrangement)
        {
            var link = host.PetLink(unit);
            if (string.IsNullOrEmpty(link)) return !host.ControlDesired(unit);
            return arrangement != null && arrangement.PetId != null && !string.Equals(arrangement.PetId, link, StringComparison.Ordinal);
        }

        // Permanent loss: the arrangement is retired (pets own none) and every public binding on the bar is cleared.
        private void RetireAndClear(TUnit unit)
        {
            var id = host.ActorId(unit);
            if (id != null && !host.IsPet(unit)) Retire(id);
            ClearPublicBindings(unit, false);
        }

        private void Retire(string id)
        {
            if (records == null) return;
            records.Retire(id);
            RetirementCount++;
        }

        // Every public binding (canonical or alias) on the unit's bar is cleared; recordPending: the cleared slots of its
        // arrangement are owed back (a KMC sweep, never the player's removal).
        private void ClearPublicBindings(TUnit unit, bool recordPending)
        {
            var count = unit == null ? 0 : host.SlotCount(unit);
            if (count == 0) return;
            var arrangement = recordPending ? Get(unit) : null;
            var changed = false;
            for (var index = 0; index < count; index++)
            {
                var content = host.ReadSlot(unit, index);
                if (content != MountedToolbarSlot.PublicControl && content != MountedToolbarSlot.LegacyAlias) continue;
                host.ClearSlot(unit, index);
                changed = true;
                if (arrangement != null && arrangement.Slots.Contains(index)) arrangement.Pending.Add(index);
            }
            if (changed) host.BarChanged(unit);
        }

        // ---- actor selection -----------------------------------------------------------------------------------------

        // Placement eligibility: the unit objects that can own an arrangement in the current world. Current riders,
        // observed former riders of this world (not present now under another object) and reserve or detached owners;
        // Observe itself skips pets.
        private List<TUnit> PlacementUnits(IList<TUnit> riders)
        {
            var result = new List<TUnit>();
            foreach (var unit in riders) AddUnique(result, unit);
            foreach (var item in observed)
                if (OwnsRecords(item, riders)) AddUnique(result, item.Unit);
            foreach (var unit in host.ReserveOwners() ?? new List<TUnit>()) AddUnique(result, unit);
            return result;
        }

        // Sanitation coverage: units whose managed references a cleanup boundary clears. Current riders (a rider that
        // rejoined before the next update included), every observed former rider (an earlier world's object too), reserve
        // and detached owners, and those owners' exact persistent pets.
        private List<TUnit> SanitationUnits(IList<TUnit> riders)
        {
            var result = new List<TUnit>();
            foreach (var unit in riders) AddUnique(result, unit);
            foreach (var item in observed) AddUnique(result, item.Unit);
            foreach (var owner in host.ReserveOwners() ?? new List<TUnit>())
            {
                AddUnique(result, owner);
                if (owner != null) AddUnique(result, host.PersistentPet(owner));
            }
            return result;
        }

        // A former rider of this world that is no longer on the player's roster has lost eligibility for good.
        private void RetireDepartedFormerRiders(IList<TUnit> riders)
        {
            foreach (var item in observed)
                if (!riders.Contains(item.Unit) && OwnsRecords(item, riders) && !host.OnPlayerRoster(item.Unit)) RetireAndClear(item.Unit);
        }

        // An observed unit authors records only in the world it was first reconciled in, and only while no other object of
        // the same character is a current rider.
        private bool OwnsRecords(ObservedUnit item, IList<TUnit> riders)
        {
            var world = host.CurrentWorld;
            if (world == null || !ReferenceEquals(item.World.Target, world)) return false;
            if (riders.Contains(item.Unit)) return true;
            var id = host.ActorId(item.Unit);
            return id == null || !riders.Any(other => other != null && !ReferenceEquals(other, item.Unit) &&
                string.Equals(host.ActorId(other), id, StringComparison.Ordinal));
        }

        private void Track(TUnit unit)
        {
            if (observed.Exists(item => ReferenceEquals(item.Unit, unit))) return;
            observed.Add(new ObservedUnit { Unit = unit, World = new WeakReference(host.CurrentWorld) });
        }

        private MountedControlPlacementIntent.Arrangement Get(TUnit unit) =>
            unit == null || records == null ? null : records.Get(host.ActorId(unit));

        private bool BarHoldsPublicBinding(TUnit unit)
        {
            var count = host.SlotCount(unit);
            for (var index = 0; index < count; index++)
            {
                var content = host.ReadSlot(unit, index);
                if (content == MountedToolbarSlot.PublicControl || content == MountedToolbarSlot.LegacyAlias) return true;
            }
            return false;
        }

        // The bar's public bindings as F2 reads them: every canonical binding, or the first alias when there is none.
        private IEnumerable<int> PublicIndices(TUnit unit)
        {
            var count = host.SlotCount(unit);
            var canonical = new List<int>();
            var firstAlias = -1;
            for (var index = 0; index < count; index++)
            {
                var content = host.ReadSlot(unit, index);
                if (content == MountedToolbarSlot.PublicControl) canonical.Add(index);
                else if (content == MountedToolbarSlot.LegacyAlias && firstAlias < 0) firstAlias = index;
            }
            if (canonical.Count == 0 && firstAlias >= 0) canonical.Add(firstAlias);
            return canonical;
        }

        private static void AddUnique(List<TUnit> list, TUnit unit)
        {
            if (unit != null && !list.Exists(item => ReferenceEquals(item, unit))) list.Add(unit);
        }
    }
}
