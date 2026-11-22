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

        private sealed class NativeOperation
        {
            internal TUnit Unit;
            internal WeakReference World;
            internal List<NativeEntry> Entries;
            internal List<NativeEntry> Context;
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
        // The one owner of the open native slot operation's unit: the adapter reads it to close the operation and keeps no
        // reference of its own, so settling or discarding the operation releases the unit.
        public TUnit NativeOperationUnit => nativeOperation?.Unit;
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

        // Native UnitUISettings.GetSlot(index) (one slot) or CleanupBadSlots (index -1: the whole bar) begins. The public
        // bindings in its range are noted with their slot objects. A previous operation that never returned is settled
        // first.
        public void BeginNativeSlotOperation(TUnit unit, int index)
        {
            SettleNativeOperation();
            if (!Enabled || unit == null || host.IsPet(unit)) return;
            var count = host.SlotCount(unit);
            if (count == 0 || index >= count) return;
            var first = index < 0 ? 0 : index;
            var last = index < 0 ? count - 1 : index;
            var content = host.ReadSlot(unit, first);
            if (index >= 0 && content != MountedToolbarSlot.PublicControl && content != MountedToolbarSlot.LegacyAlias) return;
            // Every public binding on the bar when native starts: the bar the operation's outcome is read against, so that an
            // edit the player makes before a thrown operation is settled is never mistaken for part of that bar.
            var context = new List<NativeEntry>();
            for (var at = 0; at < count; at++)
            {
                content = host.ReadSlot(unit, at);
                if (content == MountedToolbarSlot.PublicControl || content == MountedToolbarSlot.LegacyAlias)
                    context.Add(new NativeEntry { Index = at, Content = content, Identity = host.SlotIdentity(unit, at) });
            }
            var entries = context.Where(entry => entry.Index >= first && entry.Index <= last).ToList();
            if (entries.Count == 0) return;
            nativeOperation = new NativeOperation { Unit = unit, World = new WeakReference(host.CurrentWorld), Entries = entries, Context = context };
        }

        // The native operation returned normally: every noted slot native replaced with anything but a public binding (a
        // rebind keeps it) was emptied by native.
        public void EndNativeSlotOperation(TUnit unit)
        {
            var operation = nativeOperation;
            nativeOperation = null;
            if (operation != null) ApplyNativeOperation(operation, ReferenceEquals(operation.Unit, unit));
        }

        // An operation that never returned (it threw): only a slot holding native's own write counts (an empty slot bound to
        // the unit, or a live binding of the same control). While a selected load is open and no player world exists, the
        // operation is kept for its own world's outcome: settled when that world is current again, dropped on replacement.
        private void SettleNativeOperation()
        {
            if (nativeOperation != null && host.CurrentWorld == null && loadOwner != null) return;
            var operation = nativeOperation;
            nativeOperation = null;
            if (operation != null) ApplyNativeOperation(operation, false);
        }

        // Native wrote these slots. The bar as it stood just before is observed (the player's latest arrangement, every other
        // public binding a known copy), then each written slot is applied in index order: an emptied chosen slot is pending;
        // a rebound chosen slot is live (a later removal is the player's); a rebound copy stays a copy under its new slot
        // object; an emptied copy is gone, except that the last surviving copy of a pending choice whose slot the player
        // filled becomes that choice (the fallback). Only in the world that produced the operation and only for a unit on
        // its roster.
        private void ApplyNativeOperation(NativeOperation operation, bool returned)
        {
            if (!Enabled) return;
            var world = host.CurrentWorld;
            var unit = operation.Unit;
            if (world == null || !ReferenceEquals(operation.World.Target, world) || !host.OnPlayerRoster(unit)) return;
            SyncWorld();
            var count = host.SlotCount(unit);
            var written = new List<NativeEntry>();
            foreach (var entry in operation.Entries)
            {
                if (entry.Index >= count || ReferenceEquals(host.SlotIdentity(unit, entry.Index), entry.Identity)) continue;
                var now = host.ReadSlot(unit, entry.Index);
                var rebound = now == MountedToolbarSlot.PublicControl || now == MountedToolbarSlot.LegacyAlias;
                if (!returned && !rebound && !host.IsNativeEmptySlot(unit, entry.Index)) continue;
                if (!returned && rebound && !host.HoldsLiveBinding(unit, entry.Index)) continue;
                written.Add(entry);
            }
            if (written.Count == 0) return;
            Observe(unit, operation.Context.ToDictionary(entry => entry.Index, entry => new KeyValuePair<MountedToolbarSlot, object>(entry.Content, entry.Identity)));
            var arrangement = Get(unit);
            if (arrangement == null) return;
            foreach (var entry in written.OrderBy(item => item.Index))
            {
                var now = host.ReadSlot(unit, entry.Index);
                var rebound = now == MountedToolbarSlot.PublicControl || now == MountedToolbarSlot.LegacyAlias;
                if (entry.Index == arrangement.Slot)
                {
                    if (rebound) arrangement.Pending = false;
                    else if (!arrangement.Pending) { arrangement.Pending = true; NativeInvalidationCount++; }
                }
                else if (arrangement.IsCopyObject(entry.Index, entry.Identity))
                {
                    if (rebound) { arrangement.RecordCopy(entry.Index, host.SlotIdentity(unit, entry.Index)); continue; }
                    arrangement.Copies.Remove(entry.Index);
                    if (arrangement.Pending && ChoiceOccupied(unit, arrangement, count) && !CopyOnBar(unit, arrangement, count))
                    {
                        arrangement.Slot = entry.Index;
                        NativeInvalidationCount++;
                    }
                }
            }
        }

        private bool ChoiceOccupied(TUnit unit, MountedControlPlacementIntent.Arrangement arrangement, int count)
        {
            if (arrangement.Slot < 0 || arrangement.Slot >= count) return true;
            var content = host.ReadSlot(unit, arrangement.Slot);
            return content != MountedToolbarSlot.Empty && content != MountedToolbarSlot.PublicControl && content != MountedToolbarSlot.LegacyAlias;
        }

        private bool CopyOnBar(TUnit unit, MountedControlPlacementIntent.Arrangement arrangement, int count) =>
            arrangement.Copies.Any(copy => copy.Key < count && copy.Value.Target != null && ReferenceEquals(host.SlotIdentity(unit, copy.Key), copy.Value.Target));

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
                // Rider eligibility: an archive row may name only a resolvable player character that is not a pet.
                var unit = host.ResolveCharacter(id);
                if (unit == null || host.IsPet(unit)) continue;
                var arrangement = records.Get(id);
                if (arrangement != null && MountedControlSlotPolicy.IsValidSavedRow(arrangement.Slot, MountedControlSlotPolicy.SavedMountState))
                    rows.Add(new MountedToolbarRow { ActorId = id, Index = arrangement.Slot, Kind = MountedControlSlotPolicy.SavedMountState });
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
            // The arrangement's chosen slot when it holds callable live data; for a unit not yet observed, F2's choice (the
            // first canonical binding, else the first alias).
            var arrangement = records.Get(id);
            var index = arrangement != null ? arrangement.Slot : PublicIndices(unit).DefaultIfEmpty(MountedControlPlacementIntent.OffBar).First();
            if (live && MountedControlSlotPolicy.IsValidSavedRow(index, MountedControlSlotPolicy.SavedMountState) && host.HoldsLiveBinding(unit, index))
            {
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
        // arrangement (its saved slot pending until the control is live). Otherwise the rows replay a capture made earlier
        // in this world (area transfer, retained remount) and seed only an actor with no arrangement that was not retired
        // since. With several public rows (an older archive), every row that restores as the public control (kind 6 and the
        // older Mount / Dismount kinds) is considered in archive order: a fresh load takes the first whose slot can hold the
        // control, a replay the first row. Rows naming a resolvable pet own no arrangement (rider eligibility); an
        // unresolved actor keeps its rows and is purged later if it resolves as a pet.
        // A fresh load's rows never overwrite what this same incoming world has already observed for that actor (a native
        // read of its bar before the restore): that observation was made from this save's own bar, and the result is the
        // one seeding first and observing second gives.
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
                var publicRows = group.Where(row => MountedControlSlotPolicy.IsPublicBindingRow(row.Kind)).ToList();
                var slot = publicRows.Count == 0 ? MountedControlPlacementIntent.OffBar :
                    MountedControlSlotPolicy.SelectSeededPublicRow(publicRows.Select(row => row.Index).ToList(), index => Restorable(unit, index), loadedWorld);
                var fromAlias = slot >= 0 && publicRows.First(row => row.Index == slot).Kind != MountedControlSlotPolicy.SavedMountState;
                if (slot < 0 && !placed) continue;
                if (placed) records.MarkPlaced(id);
                var existing = records.Get(id);
                if (loadedWorld ? existing != null && existing.Observed || records.IsRetired(id) : existing != null || records.IsRetired(id)) continue;
                var arrangement = records.Create(id, null);
                arrangement.Slot = slot;
                arrangement.Pending = slot >= 0;
                arrangement.FromAliasRow = fromAlias;
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
        // written, read as they were before (content and slot object: the bar the player last left). The companion identity
        // is checked first: a pet link naming another companion, or cleared while the rider does not get the control,
        // retires the arrangement and clears the rider's public bindings, so a later companion never inherits them. A new
        // arrangement takes F2's choice (the first canonical binding, else the first alias), and every other public binding
        // on the bar is its known copy. Pets own no arrangement.
        private void Observe(TUnit unit, IDictionary<int, KeyValuePair<MountedToolbarSlot, object>> nativeBefore = null)
        {
            if (!Enabled || records == null || unit == null || host.IsPet(unit)) return;
            var id = host.ActorId(unit);
            if (id == null) return;
            var count = host.SlotCount(unit);
            // Companion identity does not depend on the bar: positive evidence of a replaced or lost companion retires the
            // arrangement even while the bar is unavailable (which only defers reading the slots).
            var known = records.Get(id);
            if (known != null && CompanionLost(unit, known)) { RetireAndClear(unit); return; }
            if (count == 0) return;
            // The common per-frame case, a unit with no arrangement and no public binding, is decided without allocating.
            if (nativeBefore == null && records.Get(id) == null && !BarHoldsPublicBinding(unit)) return;
            var canonical = new List<MountedControlPlacementIntent.Binding>();
            var aliases = new List<MountedControlPlacementIntent.Binding>();
            var occupied = new HashSet<int>();
            for (var index = 0; index < count; index++)
            {
                var before = default(KeyValuePair<MountedToolbarSlot, object>);
                var native = nativeBefore != null && nativeBefore.TryGetValue(index, out before);
                if (!native) before = new KeyValuePair<MountedToolbarSlot, object>(host.ReadSlot(unit, index), null);
                var content = before.Key;
                // With a native context, the public bindings are exactly those on the bar when native started; one present
                // now at another index was placed after it and is observed at the next ordinary observation.
                if (nativeBefore != null && !native && (content == MountedToolbarSlot.PublicControl || content == MountedToolbarSlot.LegacyAlias)) continue;
                if (content == MountedToolbarSlot.PublicControl || content == MountedToolbarSlot.LegacyAlias)
                {
                    var binding = new MountedControlPlacementIntent.Binding
                        { Index = index, Identity = native ? before.Value : host.SlotIdentity(unit, index), Alias = content == MountedToolbarSlot.LegacyAlias };
                    (binding.Alias ? aliases : canonical).Add(binding);
                }
                else if (content != MountedToolbarSlot.Empty) occupied.Add(index);
            }
            var bindings = canonical.Concat(aliases).ToList();
            var arrangement = records.Get(id);
            var link = host.PetLink(unit);
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
