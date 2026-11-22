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
        // A public binding (canonical or alias, never another KMC command) holding its live fact's own data.
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
    // the public control (one chosen slot or off the bar), whether that slot is pending (emptied by native bad-slot cleanup
    // or by a KMC sweep, not by the player, and therefore owed back), and the known copies of it on the bar (by slot
    // object), which never count as a new placement. Everything follows from one rule applied at every boundary before
    // anything destroys the evidence: observe the bar, then mark what the boundary empties as pending (ApplyBar holds the
    // evidence rule). A pending slot returns once into its empty slot when the control is live again; a live control keeps
    // exactly one shortcut. KMC never writes the public control outside the arrangement except the one automatic placement.
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

        // The bar as it stood when a native slot operation started: every public binding with its slot object, and every
        // other non-empty slot. A thrown operation's outcome is read against this, never against later edits.
        private sealed class NativeView
        {
            internal readonly Dictionary<int, NativeEntry> Public = new Dictionary<int, NativeEntry>();
            internal readonly HashSet<int> Occupied = new HashSet<int>();
        }

        private sealed class NativeOperation
        {
            internal TUnit Unit;
            internal WeakReference World;
            // GetSlot(index): one slot, and it can only empty it (installed contract). CleanupBadSlots: the whole bar,
            // emptied or rebound to the active fact of the slot's blueprint.
            internal bool SingleSlot;
            internal List<NativeEntry> Entries;
            internal NativeView View;
            // The slots CleanupBadSlots actually wrote, recorded at its own SetSlot call sites as each write completed
            // (RecordNativeSlotWrite), with the slot object it wrote.
            internal readonly Dictionary<int, object> Writes = new Dictionary<int, object>();
        }

        private readonly IMountedToolbarHost<TUnit> host;
        private MountedControlPlacementIntent records;
        private object loadOwner;
        private readonly List<ObservedUnit> observed = new List<ObservedUnit>();
        // The native slot operation whose prefix ran and whose postfix has not (each postfix completes only its own).
        private NativeOperation openOperation;
        // Operations that never returned (native threw), held for their own world's outcome while a selected load is open
        // and no player world exists.
        private readonly List<NativeOperation> thrownOperations = new List<NativeOperation>();

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
        public bool HasOpenNativeOperation => openOperation != null || thrownOperations.Count > 0;
        // The units of the open and held native slot operations are owned here alone (the adapter's Harmony state holds an
        // operation only from its prefix to its postfix), so settling or discarding an operation releases its unit.
        public TUnit NativeOperationUnit => openOperation?.Unit ?? (thrownOperations.Count > 0 ? thrownOperations[0].Unit : null);
        public int ObservedCount => observed.Count;
        public IList<TUnit> ObservedUnits => observed.Select(item => item.Unit).ToList();
        public bool IsPlaced(string actorId) => records != null && records.IsPlaced(actorId);
        public bool IsRetired(string actorId) => records != null && records.IsRetired(actorId);
        public int KnownCopyCount(string actorId) => records?.Get(actorId)?.Copies.Count ?? 0;

        public bool TryGetArrangement(string actorId, out int slot, out bool pending)
        {
            var arrangement = records?.Get(actorId);
            slot = arrangement?.Slot ?? MountedControlPlacementIntent.OffBar;
            pending = arrangement != null && arrangement.Pending;
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
            DropNativeOperations();
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
            DropNativeOperations();
        }

        private void DropNativeOperations()
        {
            openOperation = null;
            thrownOperations.Clear();
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
            // No player world while a selected load is open: the outcome (abandonment with the outgoing world current again,
            // or a new world) is not known yet, so nothing is observed, cleared or released, and the outgoing bars and
            // observed units stay exactly as they are. Without an open load the departures below release them (disposal).
            if (host.CurrentWorld == null && loadOwner != null) return;
            var riders = host.CurrentRiders() ?? new List<TUnit>();
            var reserve = host.ReserveOwners() ?? new List<TUnit>();
            foreach (var rider in riders) ReconcileRider(rider, riders, reserve);
            ProcessDepartures(riders);
            SweepReserve(riders);
            RevalidateAbsent(riders);
        }

        private void ReconcileRider(TUnit unit, IList<TUnit> riders, IList<TUnit> reserve)
        {
            if (unit == null) return;
            Track(unit);
            // A positively identified pet owns no rider state (an archive row admitted before it resolved is purged).
            if (host.IsPet(unit)) PurgePet(unit);
            // A rider object held only by pair custody while another object of the same character is on the roster (a former
            // instance) authors nothing: its public bindings are cleaned, the current instance owns the arrangement.
            if (!Authors(unit, riders, reserve))
            {
                ClearPublicBindings(unit, false);
                host.PrepareRider(unit);
                host.ClearManagedAutoUse(unit, MountedToolbarBoundary.Update);
                return;
            }
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

        // One shortcut. While the control is live: the chosen slot's stale canonical binding is rebound to the live data, an
        // older alias at the chosen slot becomes the public control in place, and every other public binding (a copy, or an
        // F2 leftover alias) is removed. While it is not live: an older alias is cleared (the chosen slot pending when it is
        // the alias), and canonical bindings stay as they are for native to judge.
        private void RepairBar(TUnit unit, bool live)
        {
            var count = host.SlotCount(unit);
            if (count == 0) return;
            var arrangement = Get(unit);
            var chosen = arrangement?.Slot ?? MountedControlPlacementIntent.OffBar;
            var changed = false;
            for (var index = 0; index < count; index++)
            {
                var content = host.ReadSlot(unit, index);
                if (content != MountedToolbarSlot.PublicControl && content != MountedToolbarSlot.LegacyAlias) continue;
                var atChoice = index == chosen;
                if (live && atChoice)
                {
                    if (content == MountedToolbarSlot.PublicControl && host.HoldsLiveBinding(unit, index)) continue;
                    host.BindControl(unit, index);
                    arrangement.RecordChoice(host.SlotIdentity(unit, index));
                    if (content == MountedToolbarSlot.LegacyAlias) AliasMigrationCount++;
                }
                else if (live || content == MountedToolbarSlot.LegacyAlias)
                {
                    host.ClearSlot(unit, index);
                    if (atChoice) arrangement.Pending = true;
                }
                else continue;
                changed = true;
            }
            if (changed) host.BarChanged(unit);
        }

        // The pending chosen slot returns once into its empty slot. An unavailable bar defers it; only a bar that cannot
        // hold the index gives it up.
        private void RestorePending(TUnit unit)
        {
            var arrangement = Get(unit);
            if (arrangement == null || !arrangement.Pending || arrangement.Slot < 0) return;
            var count = host.SlotCount(unit);
            if (count == 0) return;
            if (arrangement.Slot >= count)
            {
                arrangement.Slot = MountedControlPlacementIntent.OffBar;
                arrangement.Pending = false;
                return;
            }
            if (host.ReadSlot(unit, arrangement.Slot) != MountedToolbarSlot.Empty) return;
            host.BindControl(unit, arrangement.Slot);
            arrangement.RecordChoice(host.SlotIdentity(unit, arrangement.Slot));
            arrangement.Pending = false;
            RestoreCount++;
            if (arrangement.FromAliasRow) AliasMigrationCount++;
            arrangement.FromAliasRow = false;
            host.BarChanged(unit);
        }

        // The native new-ability placement, once per actor and world, into the first empty main-bar slot; a full main bar
        // records the attempt as an off-bar arrangement.
        private void TryInitialPlacement(TUnit unit)
        {
            var id = host.ActorId(unit);
            if (id == null || records == null || host.IsPet(unit) || records.Get(id) != null || records.IsPlaced(id)) return;
            if (BarHoldsPublicBinding(unit) || !host.InitialPlacementAllowed(unit)) return;
            var arrangement = records.Create(id, host.PetLink(unit));
            arrangement.Observed = true;
            var index = host.SelectInitialPlacementSlot(unit);
            if (index < 0) { InitialPlacementFullBarCount++; return; }
            host.BindControl(unit, index);
            arrangement.Slot = index;
            arrangement.RecordChoice(host.SlotIdentity(unit, index));
            host.BarChanged(unit);
            InitialPlacementCount++;
        }

        // A previously reconciled unit that is no longer a current rider. Its bar is observed first; still on the player's
        // roster it is a temporary absence (its public slots pending), otherwise permanent loss (retired). Its KMC bindings
        // facts and exact KMC auto-use selection go either way (this is the last sweep that reaches a unit leaving every
        // roster). An earlier world's object, or a character present now under another object, is only cleaned up: it never
        // authors anything in the current world.
        private void ProcessDepartures(IList<TUnit> riders)
        {
            var reserve = host.ReserveOwners() ?? new List<TUnit>();
            foreach (var item in observed.ToArray())
            {
                if (riders.Contains(item.Unit)) continue;
                var unit = item.Unit;
                if (OwnsRecords(item, riders, reserve))
                {
                    Observe(unit);
                    if (host.OnPlayerRoster(unit)) ClearPublicBindings(unit, true);
                    else RetireAndClear(unit);
                }
                else ClearPublicBindings(unit, false);
                host.SanitizeOtherBindings(unit, MountedToolbarBoundary.Departure);
                host.ClearManagedAutoUse(unit, MountedToolbarBoundary.Departure);
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
                PurgePet(pet);
                ClearPublicBindings(pet, false);
                host.SanitizeOtherBindings(pet, MountedToolbarBoundary.ReserveSweep);
            }
        }

        // An arrangement of an actor that is not a current rider is retired on positive evidence of permanent loss: the
        // character left the player's roster, or its pet link is cleared (while it does not get the control) or names
        // another companion. An actor that cannot be resolved proves nothing and keeps its arrangement (known copies are held
        // weakly, so no native slot object is kept alive for a unit KMC cannot reach). A positively identified pet is purged.
        private void RevalidateAbsent(IList<TUnit> riders)
        {
            if (records == null || records.Count == 0) return;
            var riderIds = new HashSet<string>(riders.Where(unit => unit != null).Select(host.ActorId).Where(id => id != null), StringComparer.Ordinal);
            foreach (var id in records.ActorIds)
            {
                if (riderIds.Contains(id)) continue;
                var unit = host.ResolveCharacter(id);
                if (unit == null) continue;
                if (host.IsPet(unit)) { records.Forget(id); continue; }
                if (!host.OnPlayerRoster(unit) || CompanionLost(unit, records.Get(id))) Retire(id);
            }
        }

        // A unit positively identified as a pet owns no arrangement or placement record (rider eligibility).
        private void PurgePet(TUnit pet)
        {
            var id = host.ActorId(pet);
            if (records != null && id != null) records.Forget(id);
        }

        // ---- native slot invalidation --------------------------------------------------------------------------------

        // Native UnitUISettings.GetSlot(index, unit) (one slot) or CleanupBadSlots (index -1: the whole bar) begins for the
        // bar's owner (the adapter resolves it from the native unit argument or the settings' own owner, never from a slot's
        // contents). The bar as it stands is noted (NativeView). Returns this invocation's operation, which only its own
        // postfix completes, or null when nothing in range is a public binding. An earlier operation whose postfix never
        // ran threw (the two native methods never nest: installed contract) and is settled first.
        public object BeginNativeSlotOperation(TUnit unit, int index)
        {
            SettleNativeOperation();
            if (!Enabled || unit == null || host.IsPet(unit) || host.CurrentWorld == null) return null;
            var count = host.SlotCount(unit);
            if (count == 0 || index >= count) return null;
            if (index >= 0 && !IsPublic(host.ReadSlot(unit, index))) return null;
            PinUnreadRetirement(unit);
            var view = new NativeView();
            for (var at = 0; at < count; at++)
            {
                var content = host.ReadSlot(unit, at);
                if (IsPublic(content))
                    view.Public[at] = new NativeEntry { Index = at, Content = content, Identity = host.SlotIdentity(unit, at) };
                else if (content != MountedToolbarSlot.Empty) view.Occupied.Add(at);
            }
            var entries = view.Public.Values.Where(entry => index < 0 || entry.Index == index).OrderBy(entry => entry.Index).ToList();
            if (entries.Count == 0) return null;
            openOperation = new NativeOperation { Unit = unit, World = new WeakReference(host.CurrentWorld), SingleSlot = index >= 0, Entries = entries, View = view };
            return openOperation;
        }

        // The native operation returned normally (its postfix, with the operation its own prefix returned): every noted
        // slot native replaced is native's write. A postfix whose prefix opened nothing completes nothing.
        public void EndNativeSlotOperation(object operation)
        {
            if (operation == null || !ReferenceEquals(operation, openOperation)) return;
            openOperation = null;
            ApplyNativeOperation((NativeOperation)operation, true);
        }

        // Native CleanupBadSlots has just written this slot of the unit's bar through one of its own SetSlot call sites (the
        // adapter redirects exactly those call sites: installed contract, three of them): the exact evidence of what a
        // cleanup that later throws actually wrote, recorded before anything else can write the bar.
        public void RecordNativeSlotWrite(TUnit unit, int index)
        {
            var operation = openOperation;
            if (operation == null || operation.SingleSlot || unit == null || !ReferenceEquals(operation.Unit, unit) || index < 0 || index >= host.SlotCount(unit)) return;
            operation.Writes[index] = host.SlotIdentity(unit, index);
        }

        // A write is about to reach this unit's bar: native UnitUISettings.SetSlot (its KMC prefix), through which the action
        // bar UI writes every player edit and CleanupBadSlots writes its own. A retirement whose bar was never read is pinned
        // now to the public bindings the bar holds before that write, so the write itself (a fresh placement) never counts
        // as predating it. Nothing else happens here: no settlement, observation or record change.
        public void BeforeSlotWrite(TUnit unit)
        {
            if (!Enabled || records == null || unit == null || !records.BelongsTo(host.CurrentWorld)) return;
            PinUnreadRetirement(unit);
        }

        // Shared by the SetSlot prefix (BeforeSlotWrite) and the native slot prefixes (BeginNativeSlotOperation). Only the
        // actor's current instance in this world supplies what predates its unread retirement (PinAuthority): a former object
        // (kept by pair custody, discarded, or an earlier world's) shares the actor id but not the bar, so cleaning it, or a
        // native read of its bar, never changes the current rider's retirement record.
        private void PinUnreadRetirement(TUnit unit)
        {
            var id = host.ActorId(unit);
            if (records == null || !records.RetiredUnreadUnknown(id) || !PinAuthority(unit)) return;
            var count = host.SlotCount(unit);
            if (count == 0) return;
            var bindings = new List<KeyValuePair<int, WeakReference>>();
            for (var index = 0; index < count; index++)
                if (IsPublic(host.ReadSlot(unit, index))) bindings.Add(new KeyValuePair<int, WeakReference>(index, new WeakReference(host.SlotIdentity(unit, index))));
            records.PinRetiredUnread(id, bindings);
        }

        // An operation whose postfix never ran threw. It is settled from what that native method can have written (see
        // ApplyNativeOperation). While a selected load is open and no player world exists, it is held for its own world's
        // outcome: settled when that world is current again, dropped when another world replaces it, at an unfenced
        // disposal and at disable.
        private void SettleNativeOperation()
        {
            if (openOperation != null) { thrownOperations.Add(openOperation); openOperation = null; }
            if (thrownOperations.Count == 0) return;
            if (host.CurrentWorld == null && loadOwner != null) return;  // held through the open load's missing player
            var settled = thrownOperations.ToArray();
            thrownOperations.Clear();
            foreach (var operation in settled) ApplyNativeOperation(operation, false);
        }

        // Native wrote these slots. The bar as it stood when native started is observed (the player's latest arrangement,
        // every other public binding a known copy, occupancy as it was), then each written slot is applied in index order:
        // an emptied chosen slot is pending; a rebound chosen slot is live (a later removal is the player's); a rebound copy
        // stays a copy under its new slot object; an emptied copy is gone, except that the last surviving copy of a pending
        // choice whose slot was filled becomes that choice (the fallback). Only in the world that produced the operation and
        // only for a unit on its roster.
        // A returned operation: every replaced slot is native's (its postfix runs right after native). A thrown one: only
        // native's actual writes. GetSlot can only write an empty slot bound to the unit (installed contract), so such a slot
        // is its write; CleanupBadSlots's writes were recorded as they completed (Writes) and count while the slot still holds
        // the object native wrote. Anything else in a noted slot is a later edit, read at the next ordinary observation, so
        // neither a player's placement (even of the very data cleanup would rebind) nor a later change of the fact reverses
        // or invents native's outcome.
        private void ApplyNativeOperation(NativeOperation operation, bool returned)
        {
            if (!Enabled) return;
            var world = host.CurrentWorld;
            var unit = operation.Unit;
            if (world == null || !ReferenceEquals(operation.World.Target, world) || !host.OnPlayerRoster(unit)) return;
            SyncWorld();
            var count = host.SlotCount(unit);
            var emptied = new HashSet<int>();
            var rebound = new Dictionary<int, object>();
            foreach (var entry in operation.Entries)
            {
                if (entry.Index >= count) continue;
                var identity = host.SlotIdentity(unit, entry.Index);
                if (ReferenceEquals(identity, entry.Identity)) continue;
                var bound = IsPublic(host.ReadSlot(unit, entry.Index));
                object written;
                if (returned) { if (bound) rebound[entry.Index] = identity; else emptied.Add(entry.Index); }
                else if (operation.SingleSlot) { if (!bound && host.IsNativeEmptySlot(unit, entry.Index)) emptied.Add(entry.Index); }
                else if (operation.Writes.TryGetValue(entry.Index, out written) && ReferenceEquals(written, identity))
                { if (bound) rebound[entry.Index] = identity; else emptied.Add(entry.Index); }
            }
            if (emptied.Count == 0 && rebound.Count == 0) return;
            // A native replacement of a binding that predates an unread retirement predates it as well.
            var actor = host.ActorId(unit);
            foreach (var entry in operation.Entries)
            {
                object replaced;
                if (rebound.TryGetValue(entry.Index, out replaced)) records.CarryRetiredUnread(actor, entry.Index, entry.Identity, replaced);
            }
            Observe(unit, operation.View);
            var arrangement = Get(unit);
            if (arrangement == null) return;
            foreach (var entry in operation.Entries)
            {
                object replacement;
                var wasRebound = rebound.TryGetValue(entry.Index, out replacement);
                if (!wasRebound && !emptied.Contains(entry.Index)) continue;
                if (entry.Index == arrangement.Slot)
                {
                    if (wasRebound) { arrangement.Pending = false; arrangement.RecordChoice(replacement); }
                    else if (!arrangement.Pending) { arrangement.Pending = true; NativeInvalidationCount++; }
                }
                else if (arrangement.IsCopyObject(entry.Index, entry.Identity))
                {
                    if (wasRebound) { arrangement.RecordCopy(entry.Index, replacement); continue; }
                    arrangement.Copies.Remove(entry.Index);
                    if (arrangement.Pending && ChoiceOccupied(unit, operation.View, arrangement, count) && !CopySurvives(operation.View, arrangement, emptied))
                    {
                        arrangement.Slot = entry.Index;
                        NativeInvalidationCount++;
                    }
                }
            }
        }

        // The pending choice could not take the control when native started (another ability or command held its slot).
        private bool ChoiceOccupied(TUnit unit, NativeView view, MountedControlPlacementIntent.Arrangement arrangement, int count)
        {
            if (arrangement.Slot < 0 || arrangement.Slot >= count) return true;
            object identity;
            var content = ViewSlot(unit, view, arrangement.Slot, out identity);
            return content != MountedToolbarSlot.Empty && !IsPublic(content);
        }

        // Another known copy was on the bar when native started and native did not empty it.
        private static bool CopySurvives(NativeView view, MountedControlPlacementIntent.Arrangement arrangement, ICollection<int> emptied) =>
            arrangement.Copies.Any(copy => !emptied.Contains(copy.Key) && view.Public.ContainsKey(copy.Key) &&
                copy.Value.Target != null && ReferenceEquals(view.Public[copy.Key].Identity, copy.Value.Target));

        // A slot of the bar as native started: a noted public binding (with its slot object), an occupied slot, or empty.
        private MountedToolbarSlot ViewSlot(TUnit unit, NativeView view, int index, out object identity)
        {
            NativeEntry entry;
            if (view.Public.TryGetValue(index, out entry)) { identity = entry.Identity; return entry.Content; }
            identity = null;
            return view.Occupied.Contains(index) ? MountedToolbarSlot.Ordinary : MountedToolbarSlot.Empty;
        }

        private static bool IsPublic(MountedToolbarSlot content) =>
            content == MountedToolbarSlot.PublicControl || content == MountedToolbarSlot.LegacyAlias;

        // ---- save ----------------------------------------------------------------------------------------------------

        // The archive rows of the public control. Every capture first prepares (every placement unit's bar observed now), so
        // that the rows describe the bars as they stand and only the current instance of each character authors them.
        // includeAbsent (the owned save archive): then each arrangement of an actor that resolves as a player character
        // writes one kind-6 row (live or pending; a fresh load's rows still awaiting a readable bar are carried as they were
        // read), and each placed actor one placement record. Otherwise (in-world captures: area transfer, retained remount)
        // only the live callable public binding of each authoring rider or reserve owner is written, with its placement
        // record. A disabled service writes nothing.
        // authors: filled, after the preparation, with the current riders that author archive rows, whose other exact KMC
        // bindings the adapter may write (CaptureAuthors). Rows of either category therefore come from the same units.
        public IList<MountedToolbarRow> CaptureRows(bool includeAbsent, ICollection<TUnit> authors = null)
        {
            var rows = new List<MountedToolbarRow>();
            Housekeeping();
            var riders = host.CurrentRiders() ?? new List<TUnit>();
            var reserve = host.ReserveOwners() ?? new List<TUnit>();
            var prepared = Enabled && records != null;
            if (prepared)
                foreach (var unit in PlacementUnits(riders)) Observe(unit);
            if (authors != null)
                foreach (var unit in CaptureAuthors(riders, reserve)) authors.Add(unit);
            if (!prepared) return rows;
            if (!includeAbsent)
            {
                var written = new HashSet<string>(StringComparer.Ordinal);
                foreach (var unit in riders.Concat(reserve))
                    if (unit != null && !host.IsPet(unit) && Authors(unit, riders, reserve)) CaptureLiveRows(unit, rows, written);
                return rows;
            }
            RetireDepartedFormerRiders(riders);
            RevalidateAbsent(riders);
            var ids = new SortedSet<string>(records.ActorIds.Concat(records.PlacedIds), StringComparer.Ordinal);
            foreach (var id in ids)
            {
                // Rider eligibility: an archive row may name only a resolvable player character that is not a pet.
                var unit = host.ResolveCharacter(id);
                if (unit == null || host.IsPet(unit)) continue;
                var arrangement = records.Get(id);
                if (arrangement?.Alternatives != null)
                {
                    foreach (var row in arrangement.Alternatives)
                        if (MountedControlSlotPolicy.IsValidSavedRow(row.Index, row.Kind)) rows.Add(row);
                }
                else if (arrangement != null && MountedControlSlotPolicy.IsValidSavedRow(arrangement.Slot, MountedControlSlotPolicy.SavedMountState))
                    rows.Add(new MountedToolbarRow { ActorId = id, Index = arrangement.Slot, Kind = MountedControlSlotPolicy.SavedMountState });
                if (records.IsPlaced(id))
                    rows.Add(new MountedToolbarRow { ActorId = id, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord });
            }
            return rows;
        }

        // An in-world row: the prepared arrangement's chosen slot when it holds the callable live public control. One row and
        // one record per actor.
        private void CaptureLiveRows(TUnit unit, List<MountedToolbarRow> rows, HashSet<string> written)
        {
            var id = host.ActorId(unit);
            if (id == null || !written.Add(id)) return;
            var live = host.ControlLive(unit);
            var wrote = false;
            var arrangement = records.Get(id);
            var index = arrangement?.Slot ?? MountedControlPlacementIntent.OffBar;
            if (live && MountedControlSlotPolicy.IsValidSavedRow(index, MountedControlSlotPolicy.SavedMountState) && host.HoldsLiveBinding(unit, index))
            {
                rows.Add(new MountedToolbarRow { ActorId = id, Index = index, Kind = MountedControlSlotPolicy.SavedMountState });
                wrote = true;
            }
            if (records.IsPlaced(id) && (live || wrote))
                rows.Add(new MountedToolbarRow { ActorId = id, Index = 0, Kind = MountedControlSlotPolicy.SavedPlacementRecord });
        }

        // The current riders whose bars author archive rows: a character's current instance (Authors), and a pet unless
        // another object of the same actor is the current persistent pet of an authoring owner (a former mount object kept
        // by pair custody is only cleaned up, like a former rider object).
        private List<TUnit> CaptureAuthors(IList<TUnit> riders, IList<TUnit> reserve)
        {
            var currentPets = new List<TUnit>();
            foreach (var owner in riders.Concat(reserve))
                if (owner != null && !host.IsPet(owner) && Authors(owner, riders, reserve)) AddUnique(currentPets, host.PersistentPet(owner));
            var result = new List<TUnit>();
            foreach (var unit in riders)
            {
                if (unit == null) continue;
                if (!host.IsPet(unit)) { if (Authors(unit, riders, reserve)) AddUnique(result, unit); continue; }
                var id = host.ActorId(unit);
                if (currentPets.Contains(unit) || !currentPets.Any(pet => string.Equals(host.ActorId(pet), id, StringComparison.Ordinal)))
                    AddUnique(result, unit);
            }
            return result;
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
        // arrangement (its saved slot pending until the control is live). Otherwise the rows replay a capture made earlier
        // in this world (area transfer, retained remount) and seed only an actor with no arrangement that was not retired
        // since. With several public rows (an older archive), every row that restores as the public control (kind 6 and the
        // older Mount / Dismount kinds) is considered in archive order: a fresh load takes the first whose slot can hold the
        // control, a replay the first row. Rows naming a resolvable pet own no arrangement (rider eligibility); an
        // unresolved actor keeps its rows and is purged later if it resolves as a pet.
        // A fresh load's rows never overwrite what this same incoming world has already observed for that actor (a native
        // read of its bar before the restore): that observation was made from this save's own bar, and the result is the
        // one seeding first and observing second gives. The one exception has the same reason: an observed choice that was
        // only an older Mount / Dismount alias (an F2 leftover; the player cannot place one) yields to the archive's own
        // record of the actor (its public row, or a marker without one: off the bar), and the alias becomes what seeding
        // first makes it, a known copy of the archive's choice.
        // A fresh load whose actor or bar cannot be read yet keeps every public row (Alternatives) and chooses once the bar's
        // occupancy can be read, so an older archive's usable later row is not lost to a toolbar that becomes readable late.
        public void SeedFromRows(IEnumerable<MountedToolbarRow> rows, bool loadedWorld)
        {
            Housekeeping();
            if (!Enabled || records == null || rows == null) return;
            foreach (var group in rows.Where(row => row.ActorId != null).GroupBy(row => row.ActorId, StringComparer.Ordinal))
            {
                var id = group.Key;
                var unit = host.ResolveCharacter(id);
                if (unit != null && host.IsPet(unit)) continue;
                var placed = group.Any(row => row.Kind == MountedControlSlotPolicy.SavedPlacementRecord);
                var publicRows = group.Where(row => MountedControlSlotPolicy.IsPublicBindingRow(row.Kind))
                    .Select(row => new MountedToolbarRow { ActorId = id, Index = row.Index, Kind = row.Kind }).ToList();
                if (publicRows.Count == 0 && !placed) continue;
                if (placed) records.MarkPlaced(id);
                if (records.IsRetired(id)) continue;
                var existing = records.Get(id);
                if (existing != null && (!loadedWorld || existing.Observed))
                {
                    if (loadedWorld && existing.ObservedBeforeRows && existing.ChoiceIsAlias)
                        AdoptArchiveChoice(unit, existing, publicRows);
                    continue;
                }
                var unreadable = loadedWorld && publicRows.Count > 1 && (unit == null || host.SlotCount(unit) == 0);
                var arrangement = records.Create(id, unit == null ? null : host.PetLink(unit));
                if (unreadable)
                {
                    arrangement.Alternatives = publicRows;
                    arrangement.Slot = publicRows[0].Index;
                    arrangement.Pending = true;
                    arrangement.FromAliasRow = publicRows[0].Kind != MountedControlSlotPolicy.SavedMountState;
                    continue;
                }
                var slot = publicRows.Count == 0 ? MountedControlPlacementIntent.OffBar : SelectRow(unit, null, publicRows, loadedWorld);
                arrangement.Slot = slot;
                arrangement.Pending = slot >= 0;
                arrangement.FromAliasRow = slot >= 0 && publicRows.First(row => row.Index == slot).Kind != MountedControlSlotPolicy.SavedMountState;
            }
            if (loadedWorld) records.RowsSeeded = true;
        }

        // The choice among public rows (MountedControlSlotPolicy.SelectSeededPublicRow): a fresh load takes the first row
        // whose slot can hold the control now, a replay the first row.
        private int SelectRow(TUnit unit, NativeView view, IList<MountedToolbarRow> publicRows, bool loadedWorld) =>
            MountedControlSlotPolicy.SelectSeededPublicRow(publicRows.Select(row => row.Index).ToList(), index => Restorable(unit, view, index), loadedWorld);

        // The deferred choice among a fresh load's rows, made at the first observation of a readable bar.
        private void ResolveAlternatives(TUnit unit, NativeView view, MountedControlPlacementIntent.Arrangement arrangement)
        {
            var rows = arrangement.Alternatives;
            arrangement.Alternatives = null;
            var slot = SelectRow(unit, view, rows, true);
            arrangement.Slot = slot;
            arrangement.FromAliasRow = rows.First(row => row.Index == slot).Kind != MountedControlSlotPolicy.SavedMountState;
        }

        // An incoming observation whose choice was a leftover alias meets the archive's record of the actor. Seeding first
        // would have made every leftover alias a known copy of the archive's choice, so:
        // - a marker alone: off the bar (no copy becomes a choice while nothing is owed);
        // - an archived slot that can take the control: that slot, pending as seeded; the alias, while still on the bar, is
        //   its known copy;
        // - an archived slot that cannot (occupied): the fallback is the first leftover in F2's order, which is exactly the
        //   alias the observation chose, so that choice stands with whatever native did to it since (emptied: still owed).
        //   Another surviving copy never displaces it.
        private void AdoptArchiveChoice(TUnit unit, MountedControlPlacementIntent.Arrangement arrangement, IList<MountedToolbarRow> publicRows)
        {
            var alias = arrangement.Slot;
            var slot = publicRows.Count == 0 ? MountedControlPlacementIntent.OffBar : SelectRow(unit, null, publicRows, true);
            arrangement.ObservedBeforeRows = false;
            arrangement.FromAliasRow = false;
            if (slot >= 0 && alias >= 0 && !Restorable(unit, null, slot)) return;
            arrangement.Slot = slot;
            arrangement.Pending = slot >= 0;
            arrangement.ChoiceIsAlias = false;
            arrangement.FromAliasRow = slot >= 0 && publicRows.First(row => row.Index == slot).Kind != MountedControlSlotPolicy.SavedMountState;
            if (alias < 0 || alias == slot || unit == null) return;
            if (alias < host.SlotCount(unit) && IsPublic(host.ReadSlot(unit, alias)))
                arrangement.RecordCopy(alias, host.SlotIdentity(unit, alias));
        }

        // The slot can take the public control (empty, or a public binding), on the bar as it stands or as native started.
        private bool Restorable(TUnit unit, NativeView view, int index)
        {
            if (unit == null || index < 0 || index >= host.SlotCount(unit)) return false;
            object identity;
            var content = view != null ? ViewSlot(unit, view, index, out identity) : host.ReadSlot(unit, index);
            return content == MountedToolbarSlot.Empty || IsPublic(content);
        }

        // ---- observation ---------------------------------------------------------------------------------------------

        // The rider's bar now is the evidence of the player's arrangement. view: the bar as a native slot operation found it
        // when it started (its public bindings with their slot objects, and its occupancy), read instead of the bar now: the
        // bar the player last left. The companion identity is checked first and recorded whether or not the bar can be read:
        // a pet link naming another companion, or cleared while the rider does not get the control, retires the arrangement
        // and clears the rider's public bindings, so a later companion never inherits them. A new arrangement takes F2's
        // choice (the first canonical binding, else the first alias), and every other public binding on the bar is its known
        // copy. Pets own no arrangement.
        private void Observe(TUnit unit, NativeView view = null)
        {
            if (!Enabled || records == null || unit == null || host.IsPet(unit)) return;
            var id = host.ActorId(unit);
            if (id == null) return;
            var count = host.SlotCount(unit);
            var link = host.PetLink(unit);
            // Companion identity does not depend on the bar: positive evidence of a replaced or lost companion retires the
            // arrangement even while the bar is unavailable (which only defers reading the slots), and the companion an
            // arrangement belongs to is recorded as soon as it is known.
            var known = records.Get(id);
            if (known != null && CompanionLost(unit, known)) { RetireAndClear(unit); return; }
            if (known != null && known.PetId == null && !string.IsNullOrEmpty(link)) known.PetId = link;
            if (count == 0) return;
            // A retirement made while this bar could not be read: the bindings that predate it are cleared and never read as
            // a placement, on the bar now or in a native operation's view of it; a binding placed since is the player's.
            List<KeyValuePair<int, WeakReference>> predating;
            var retiredUnread = records.TakeRetiredUnread(id, out predating);
            if (retiredUnread) ClearPredating(unit, predating);
            if (known?.Alternatives != null) ResolveAlternatives(unit, view, known);
            // The common per-frame case, a unit with no arrangement and no public binding, is decided without allocating.
            if (view == null && known == null && !BarHoldsPublicBinding(unit)) return;
            var canonical = new List<MountedControlPlacementIntent.Binding>();
            var aliases = new List<MountedControlPlacementIntent.Binding>();
            var occupied = new HashSet<int>();
            for (var index = 0; index < count; index++)
            {
                object identity = null;
                var content = view != null ? ViewSlot(unit, view, index, out identity) : host.ReadSlot(unit, index);
                if (IsPublic(content))
                {
                    var binding = new MountedControlPlacementIntent.Binding
                        { Index = index, Identity = view != null ? identity : host.SlotIdentity(unit, index), Alias = content == MountedToolbarSlot.LegacyAlias };
                    if (retiredUnread && MountedControlPlacementIntent.Predates(predating, index, binding.Identity)) continue;
                    (binding.Alias ? aliases : canonical).Add(binding);
                }
                else if (content != MountedToolbarSlot.Empty) occupied.Add(index);
            }
            var bindings = canonical.Concat(aliases).ToList();
            var arrangement = records.Get(id);
            if (arrangement != null ? CompanionLost(unit, arrangement) :
                bindings.Count > 0 && string.IsNullOrEmpty(link) && !host.ControlDesired(unit))
            {
                RetireAndClear(unit);
                return;
            }
            if (arrangement == null)
            {
                if (bindings.Count == 0) return;
                arrangement = records.Create(id, link);
                arrangement.Slot = bindings[0].Index;
                arrangement.Observed = true;
                arrangement.ObservedBeforeRows = !records.RowsSeeded;
            }
            if (arrangement.PetId == null && !string.IsNullOrEmpty(link)) arrangement.PetId = link;
            MountedControlPlacementIntent.ApplyBar(arrangement, bindings, occupied, count);
            records.MarkPlaced(id);
        }

        private bool CompanionLost(TUnit unit, MountedControlPlacementIntent.Arrangement arrangement)
        {
            var link = host.PetLink(unit);
            if (string.IsNullOrEmpty(link)) return !host.ControlDesired(unit);
            return arrangement != null && arrangement.PetId != null && !string.Equals(arrangement.PetId, link, StringComparison.Ordinal);
        }

        // Permanent loss: the arrangement is retired (pets own none) and every public binding on the bar is cleared. A bar
        // that cannot be read now keeps what predates the retirement: the arrangement's known bindings by slot object, or,
        // for a bar never read, everything on it (MountedControlPlacementIntent.MarkRetiredUnread).
        private void RetireAndClear(TUnit unit)
        {
            var id = host.ActorId(unit);
            if (id != null && !host.IsPet(unit))
            {
                var known = Get(unit);
                Retire(id);
                if (host.SlotCount(unit) == 0 && records != null)
                    records.MarkRetiredUnread(id, known != null && known.Observed ? known.KnownBindings() : null);
            }
            ClearPublicBindings(unit, false);
        }

        // The public bindings that predate an unread retirement are cleared from the bar now readable.
        private void ClearPredating(TUnit unit, List<KeyValuePair<int, WeakReference>> predating)
        {
            var count = host.SlotCount(unit);
            var changed = false;
            for (var index = 0; index < count; index++)
            {
                if (!IsPublic(host.ReadSlot(unit, index)) || !MountedControlPlacementIntent.Predates(predating, index, host.SlotIdentity(unit, index))) continue;
                host.ClearSlot(unit, index);
                changed = true;
            }
            if (changed) host.BarChanged(unit);
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
                if (arrangement != null && arrangement.Slot == index) arrangement.Pending = true;
                // The cleared binding's slot object is released; the chosen index and companion stay.
                arrangement?.Copies.Remove(index);
            }
            if (changed) host.BarChanged(unit);
        }

        // ---- actor selection -----------------------------------------------------------------------------------------

        // Placement eligibility: the unit objects that can own an arrangement in the current world. Current riders,
        // observed former riders of this world (not present now under another object) and reserve or detached owners;
        // Observe itself skips pets.
        private List<TUnit> PlacementUnits(IList<TUnit> riders)
        {
            var reserve = host.ReserveOwners() ?? new List<TUnit>();
            var result = new List<TUnit>();
            foreach (var unit in riders) if (unit != null && Authors(unit, riders, reserve)) AddUnique(result, unit);
            foreach (var item in observed)
                if (OwnsRecords(item, riders, reserve)) AddUnique(result, item.Unit);
            foreach (var unit in reserve) AddUnique(result, unit);
            return result;
        }

        // Sanitation coverage: units whose managed references a cleanup boundary clears. Current riders (a rider that
        // rejoined before the next update included), every observed former rider (an earlier world's object too), reserve
        // and detached owners, and the exact persistent pet of every one of them: a reserve owner that also sits in a pair's
        // custody is a current rider, and its pet is reached from it, not from the party.
        private List<TUnit> SanitationUnits(IList<TUnit> riders)
        {
            var result = new List<TUnit>();
            foreach (var unit in riders) AddUnique(result, unit);
            foreach (var item in observed) AddUnique(result, item.Unit);
            foreach (var owner in host.ReserveOwners() ?? new List<TUnit>()) AddUnique(result, owner);
            foreach (var owner in result.ToArray()) AddUnique(result, host.PersistentPet(owner));
            return result;
        }

        // A former rider of this world that is no longer on the player's roster has lost eligibility for good.
        private void RetireDepartedFormerRiders(IList<TUnit> riders)
        {
            var reserve = host.ReserveOwners() ?? new List<TUnit>();
            foreach (var item in observed)
                if (!riders.Contains(item.Unit) && OwnsRecords(item, riders, reserve) && !host.OnPlayerRoster(item.Unit)) RetireAndClear(item.Unit);
        }

        // An observed unit authors records only in the world it was first reconciled in, and only as the character's current
        // instance (Authors).
        private bool OwnsRecords(ObservedUnit item, IList<TUnit> riders, IList<TUnit> reserve)
        {
            var world = host.CurrentWorld;
            return world != null && ReferenceEquals(item.World.Target, world) && Authors(item.Unit, riders, reserve);
        }

        // Placement authority by exact roster identity: an object on the current world's player roster authors its actor's
        // arrangement. An object that is not (a former instance kept by pair custody, a departed object) authors only while
        // no other object of the same character is on the roster as a current rider or reserve or detached owner; it stays
        // in the sanitation sweep either way.
        private bool Authors(TUnit unit, IList<TUnit> riders, IList<TUnit> reserve)
        {
            if (host.OnPlayerRoster(unit)) return true;
            var id = host.ActorId(unit);
            return id == null || !riders.Concat(reserve).Any(other => other != null && !ReferenceEquals(other, unit) &&
                string.Equals(host.ActorId(other), id, StringComparison.Ordinal) && host.OnPlayerRoster(other));
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

        // The unit is its actor's current authoring instance in this world: on the current player roster, or an object of
        // this world that KMC reconciled and that authors (no other object of the character is on the roster: Authors).
        private bool PinAuthority(TUnit unit)
        {
            var world = host.CurrentWorld;
            if (world == null || records == null || !records.BelongsTo(world)) return false;
            if (!Authors(unit, host.CurrentRiders() ?? new List<TUnit>(), host.ReserveOwners() ?? new List<TUnit>())) return false;
            return host.OnPlayerRoster(unit) || observed.Exists(item => ReferenceEquals(item.Unit, unit) && ReferenceEquals(item.World.Target, world));
        }

        private static void AddUnique(List<TUnit> list, TUnit unit)
        {
            if (unit != null && !list.Exists(item => ReferenceEquals(item, unit))) list.Add(unit);
        }
    }
}
