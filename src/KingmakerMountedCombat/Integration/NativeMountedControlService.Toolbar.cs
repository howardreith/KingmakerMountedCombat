using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities;
using KingmakerMountedCombat.Domain;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // The toolbar side of the native control service: the narrow Kingmaker adapter of MountedToolbarLifecycle. Every
    // decision about the player's Mount / Dismount arrangement and its sequencing at each boundary is the lifecycle's; this
    // file only reads and writes native bars, facts, rosters and the player world, and keeps the unchanged handling of the
    // other exact KMC bindings (save-scope leases, an older Mounted Charge, retired helpers).
    internal sealed partial class NativeMountedControlService
    {
        private MountedToolbarLifecycle<UnitEntityData> toolbar;
        private UnitEntityData nativeOperationUnit;

        private void CreateToolbarLifecycle() =>
            toolbar = new MountedToolbarLifecycle<UnitEntityData>(new ToolbarHost(this));

        internal long InitialPlacementCount => toolbar.InitialPlacementCount;

        internal long InitialPlacementFullBarCount => toolbar.InitialPlacementFullBarCount;

        internal long LegacySlotRepairCount => chargeSlotRepairCount + toolbar.AliasMigrationCount;

        private long chargeSlotRepairCount;

        // Public controls returned to the player's chosen slot after a temporary absence.
        internal long PlacementIntentRestoreCount => toolbar.RestoreCount;

        internal int PlacementIntentCount => toolbar.ArrangementCount;

        internal long NativeSlotInvalidationCount => toolbar.NativeInvalidationCount;

        internal bool IsPlacementRecorded(UnitEntityData unit) => unit?.UniqueId != null && toolbar.IsPlaced(unit.UniqueId);

        // A new native load begins within the same world (diagnostics only): the placement records are forgotten as a
        // freshly loaded world's are, so the first placement runs again.
        internal void BeginLoadedWorld()
        {
            ClearTargetingPresentation("load");
            toolbar.ResetPlacementRecords();
            initialPlacements.Clear();
        }

        // A selected native load begins (MountedPersistenceService, at its iterator's first step). The outgoing world keeps
        // its records until native loading replaces the player object; loadOwner is the only scope that can end it.
        internal void BeginSelectedLoad(object loadOwner)
        {
            ClearTargetingPresentation("load");
            initialPlacements.Clear();
            toolbar.BeginSelectedLoad(loadOwner);
        }

        // The owning load scope ended without native completion (canceled, failed or superseded).
        internal void AbandonLoadedWorld(object loadOwner) => toolbar.AbandonSelectedLoad(loadOwner);

        // Native UnitUISettings.GetSlot(index) / CleanupBadSlots begins (exact prefixes; slotIndex -1 for the whole bar).
        internal void BeginNativeBadSlotCleanup(UnitUISettings settings, int slotIndex = -1)
        {
            nativeOperationUnit = disposed || !enabled || mountStateAbility == null ? null : ResolveBarUnit(settings, slotIndex);
            toolbar.BeginNativeSlotOperation(nativeOperationUnit, slotIndex);
        }

        // The operation returned normally (exact postfixes).
        internal void EndNativeBadSlotCleanup(UnitUISettings settings)
        {
            var unit = nativeOperationUnit != null && ReferenceEquals(nativeOperationUnit.UISettings, settings) ? nativeOperationUnit : null;
            nativeOperationUnit = null;
            toolbar.EndNativeSlotOperation(unit);
        }

        // The unit whose bar a native slot operation reads: the owner of a public binding in its range (only those matter),
        // whose own UISettings is this one. Null when the range holds no public binding.
        private UnitEntityData ResolveBarUnit(UnitUISettings settings, int slotIndex)
        {
            var slots = settings?.Slots;
            if (slots == null || slotIndex >= slots.Length) return null;
            for (var index = slotIndex < 0 ? 0 : slotIndex; index < (slotIndex < 0 ? slots.Length : slotIndex + 1); index++)
            {
                var slot = slots[index] as MechanicActionBarSlotAbility;
                if (!MountedControlSlotPolicy.IsPublicControlKind(ResolveKind(slot?.Ability?.Blueprint))) continue;
                var unit = slot.Unit ?? slot.Ability?.Caster?.Unit;
                return unit != null && ReferenceEquals(unit.UISettings, settings) ? unit : null;
            }
            return null;
        }

        // Every update: the adapter's own per-rider work, called by the lifecycle after the rider's bar was observed.
        private void PrepareRider(UnitEntityData unit)
        {
            ReconcileRetiredPrimaryReferences(unit);
            foreach (var ability in EnumerateBlueprints())
            {
                var kind = ResolveKind(ability);
                var desired = ShouldLease(unit, kind);
                var facts = unit?.Descriptor?.Abilities?.Enumerable
                    .Where(item => ReferenceEquals(item.Blueprint, ability)).ToList() ?? new List<Ability>();
                if (desired && facts.Count == 1 && facts[0].Active) continue;
                foreach (var fact in facts) unit.Descriptor.Abilities.RemoveFact(fact);
                if (!desired) continue;
                var added = unit.Descriptor.Abilities.AddFact(ability, null) as Ability;
                if (added == null || !added.Active || !ReferenceEquals(added.Blueprint, ability))
                    throw new InvalidOperationException("The exact native control fact could not be leased: " + ability.name + ".");
                logger.Info("Native mounted control fact leased: ability=" + ability.name + "; unitId=" + unit.UniqueId + ".");
            }
            MigrateLegacyChargeSlots(unit, false);
        }

        // F2: an older Mounted Charge binding becomes the rider's own stock Charge in the same slot (when that Charge is
        // available and not already on the bar); otherwise it is emptied. Every other slot keeps its content and position.
        // clearOtherManaged: the reserve sweep also clears every other exact KMC binding that is not the public control.
        private void MigrateLegacyChargeSlots(UnitEntityData unit, bool clearOtherManaged)
        {
            var slots = unit?.UISettings?.Slots;
            if (slots == null || unit.Descriptor?.Abilities == null) return;
            var changed = false;
            for (var index = 0; index < slots.Length; index++)
            {
                var kind = ResolveKind((slots[index] as MechanicActionBarSlotAbility)?.Ability?.Blueprint);
                if (kind == NativeMountedControlKind.None || MountedControlSlotPolicy.IsPublicControlKind(kind)) continue;
                var charge = MountedControlSlotPolicy.RepairLiveSlot(kind) == MountedControlSlotRepair.StockCharge;
                if (!charge && !clearOtherManaged) continue;
                var replacement = charge ? ResolveStockChargeForSlot(unit, slots) : null;
                unit.UISettings.SetSlot(replacement == null
                    ? (MechanicActionBarSlot)new MechanicActionBarSlotEmpty { Unit = unit }
                    : new MechanicActionBarSlotAbility { Unit = unit, Ability = replacement }, index);
                changed = true;
                if (!charge) continue;
                chargeSlotRepairCount++;
                logger.Info("Old KMC control slot repaired: unitId=" + unit.UniqueId + "; slot=" + index + "; from=" + kind +
                    "; to=" + (replacement == null ? "empty" : replacement.Blueprint.name) + ".");
            }
            if (changed) MarkBarChanged(unit);
        }

        // legacySchema: the held schema-2 writer only knows kinds 1-4, so the public control is written as the shell it
        // runs now and F2's placement records are omitted.
        // includeAbsentIntent: the owned save archive, which carries every rider's arrangement, live or pending (for example
        // a companion on the capital's reserve roster), as kind-6 rows and one placement record (MountedToolbarLifecycle).
        internal SavedMountedSlot[] CapturePersistentSlots(bool legacySchema = false, bool includeAbsentIntent = false)
        {
            var result = new List<SavedMountedSlot>();
            if (PersistentRowsSuppressed != null && PersistentRowsSuppressed()) return result.ToArray();
            CaptureOtherLiveSlots(result, legacySchema);
            if (!legacySchema)
                foreach (var row in toolbar.CaptureRows(includeAbsentIntent))
                    result.Add(new SavedMountedSlot { ActorId = row.ActorId, Index = row.Index, Kind = row.Kind });
            return result.ToArray();
        }

        // The live callable rows of every exact KMC binding other than the public control (and, for the held legacy writer,
        // the public control as its current shell).
        private void CaptureOtherLiveSlots(List<SavedMountedSlot> result, bool legacySchema)
        {
            foreach (var unit in CollectCurrentCandidateUnits())
            {
                var slots = unit?.UISettings?.Slots;
                if (slots == null) continue;
                // Saved indices are bounded to 0-127 by the archive contract; a binding further out is not persisted.
                for (var i = 0; i < slots.Length && i <= 127; i++)
                {
                    var kind = ResolveKind((slots[i] as MechanicActionBarSlotAbility)?.Ability?.Blueprint);
                    if (!legacySchema && MountedControlSlotPolicy.IsPublicControlKind(kind)) continue;
                    if (!IsCallableLiveBinding(unit, slots[i] as MechanicActionBarSlotAbility, kind)) continue;
                    var savedKind = legacySchema && kind == NativeMountedControlKind.MountState
                        ? (int)ResolveMountStateIntent(unit) : (int)kind;
                    if (legacySchema && savedKind > MountedControlSlotPolicy.SavedMountPrimary) continue;
                    result.Add(new SavedMountedSlot { ActorId = unit.UniqueId, Index = i, Kind = savedKind });
                }
            }
        }

        // Callable live data of an exact KMC binding: the rider still gets that control, its fact is active, and the slot
        // holds that fact's own data cast by the rider. Native hotbars can retain AbilityData after its runtime fact was
        // removed by a real dismount; that is not a live binding and must not require inventing a fact after loading.
        private bool IsCallableLiveBinding(UnitEntityData unit, MechanicActionBarSlotAbility slot, NativeMountedControlKind kind)
        {
            var ability = slot?.Ability;
            if (unit == null || ability == null || kind == NativeMountedControlKind.None || !ShouldLease(unit, kind)) return false;
            var fact = unit.Descriptor?.Abilities?.GetAbility(ability.Blueprint);
            return fact != null && fact.Active && ReferenceEquals(fact.Data, ability) && ability.Caster?.Unit == unit;
        }

        // loadedWorld: the rows of a freshly loaded save, which initialise the arrangements. Otherwise the rows replay a
        // capture made earlier in this same world (area transfer, retained remount): an arrangement observed since wins.
        // The public control returns through the lifecycle's update; the other exact KMC rows are restored here as before.
        internal void RestorePersistentSlots(SavedMountedSlot[] bindings, bool loadedWorld)
        {
            bindings = bindings ?? new SavedMountedSlot[0];
            toolbar.SeedFromRows(bindings.Select(row => new MountedToolbarRow { ActorId = row.ActorId, Index = row.Index, Kind = row.Kind }), loadedWorld);
            restoringSlots = true;
            try
            {
                Update();
                var candidates = CollectCandidateUnits();
                foreach (var binding in bindings)
                {
                    var repair = MountedControlSlotPolicy.RestoreSavedSlot(binding.Kind);
                    if (binding.Kind == MountedControlSlotPolicy.SavedPlacementRecord || repair == MountedControlSlotRepair.MountState) continue;
                    var units = candidates.Where(u => u.UniqueId == binding.ActorId).ToArray();
                    if (units.Length != 1) continue;
                    var unit = units[0];
                    var slots = unit.UISettings?.Slots;
                    if (slots == null || binding.Index < 0 || binding.Index >= slots.Length) continue;
                    var current = slots[binding.Index];
                    if (ResolveKind((current as MechanicActionBarSlotAbility)?.Ability?.Blueprint) == (NativeMountedControlKind)binding.Kind) continue;
                    if (current != null && !(current is MechanicActionBarSlotEmpty)) continue;
                    AbilityData restored = null;
                    if (repair == MountedControlSlotRepair.StockCharge) restored = ResolveStockChargeForSlot(unit, slots);
                    else
                    {
                        var blueprint = EnumerateBlueprints().SingleOrDefault(b => (int)ResolveKind(b) == binding.Kind);
                        var fact = blueprint == null ? null : unit.Descriptor.Abilities.GetAbility(blueprint);
                        if (fact != null && fact.Active) restored = fact.Data;
                    }
                    if (restored == null) continue;
                    unit.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = unit, Ability = restored }, binding.Index);
                    MarkBarChanged(unit);
                    if (repair == MountedControlSlotRepair.Keep) continue;
                    chargeSlotRepairCount++;
                    logger.Info("Saved KMC control slot migrated: unitId=" + unit.UniqueId + "; slot=" + binding.Index +
                        "; savedKind=" + binding.Kind + "; to=" + restored.Blueprint.name + ".");
                }
            }
            finally
            {
                restoringSlots = false;
            }
        }

        // The save scope's leases of exact KMC bindings other than the public control (whose slots the lifecycle keeps
        // pending and returns itself). A lease goes back when its placeholder is still in the slot and its fact is active.
        private void ClearSerializationHotbarLeases(bool restore)
        {
            foreach (var lease in hotbarSerializationLeases)
            {
                var slots = lease.Unit?.UISettings?.Slots;
                if (slots == null || lease.Index < 0 || lease.Index >= slots.Length || !ReferenceEquals(slots[lease.Index], lease.Placeholder)) continue;
                var fact = lease.Unit.Descriptor?.Abilities?.GetAbility(lease.Blueprint);
                if (!restore || fact == null || !fact.Active) continue;
                lease.Unit.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = lease.Unit, Ability = fact.Data }, lease.Index);
                MarkBarChanged(lease.Unit);
            }
            hotbarSerializationLeases.Clear();
        }

        private int CountManagedHotbarSlots(UnitEntityData unit)
        {
            var slots = unit?.UISettings?.Slots;
            return slots == null ? 0 : slots.Count(slot =>
                ResolveKind((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint) != NativeMountedControlKind.None);
        }

        // This exact unit instance is one of the current world's player characters: in the party, its detached part, or
        // the reserve (remote) roster the capital moves companions to. Ex-companions, units gone from every roster and an
        // earlier world's objects (a load replaces them, keeping actor ids) are not.
        private static bool IsOnPlayerRoster(UnitEntityData unit)
        {
            var player = Game.Instance?.Player;
            if (player == null || unit == null) return false;
            bool Holds(List<UnitReference> list) => list != null && list.Any(reference => ReferenceEquals(reference.Value, unit));
            return !Holds(player.ExCompanions) &&
                (Holds(player.PartyCharacters) || Holds(player.RemoteCompanions) || Holds(player.DetachedPartyCharacters));
        }

        // The current world's player characters on the reserve (remote) roster or detached, that are not candidates now.
        private static List<UnitEntityData> ReserveRosterUnits(List<UnitEntityData> candidates)
        {
            var result = new List<UnitEntityData>();
            var player = Game.Instance?.Player;
            if (player == null) return result;
            foreach (var list in new[] { player.RemoteCompanions, player.DetachedPartyCharacters })
            {
                if (list == null) continue;
                foreach (var reference in list)
                {
                    var unit = reference.Value;
                    if (unit?.UniqueId == null || candidates.Contains(unit) || result.Contains(unit) || !IsOnPlayerRoster(unit)) continue;
                    result.Add(unit);
                }
            }
            return result;
        }

        // CTRL-03: the native new-ability placement reads the visible main bar through native GetSlot (which also runs
        // native's own bad-slot replacement) and records what it saw. Observation only.
        private int SelectInitialPlacementSlot(UnitEntityData unit)
        {
            var empty = new bool[MountedControlSlotPolicy.MainBarSlotCount];
            var before = new string[empty.Length];
            for (var index = 0; index < empty.Length; index++)
            {
                var slot = unit.UISettings.GetSlot(index, unit);
                empty[index] = slot is MechanicActionBarSlotEmpty;
                before[index] = empty[index] ? "" : (slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint?.AssetGuid ?? slot?.GetType().Name ?? "";
            }
            var chosen = MountedControlSlotPolicy.SelectInitialPlacementIndex(empty);
            if (initialPlacements.Count < 32)
                initialPlacements.Add(new InitialPlacementObservation { UnitId = unit.UniqueId, Index = chosen, Frame = Time.frameCount, MainBarBefore = before });
            if (chosen < 0) logger.Info("Mount / Dismount was not placed: the main action bar is full; unitId=" + unit.UniqueId + ".");
            else logger.Info("Mount / Dismount placed once on the main action bar: unitId=" + unit.UniqueId + "; slot=" + chosen + ".");
            return chosen;
        }

        private sealed class ToolbarHost : IMountedToolbarHost<UnitEntityData>
        {
            private readonly NativeMountedControlService service;

            internal ToolbarHost(NativeMountedControlService service) { this.service = service; }

            public object CurrentWorld => Game.Instance?.Player;

            public IList<UnitEntityData> CurrentRiders() => service.CollectCurrentCandidateUnits();

            public IList<UnitEntityData> ReserveOwners() => ReserveRosterUnits(service.CollectCurrentCandidateUnits());

            public UnitEntityData PersistentPet(UnitEntityData owner)
            {
                var pet = owner?.Descriptor?.Pet;
                return pet?.Descriptor != null && pet.Descriptor.IsPet && ReferenceEquals(pet.Descriptor.Master.Value, owner) ? pet : null;
            }

            public bool IsPet(UnitEntityData unit) => unit?.Descriptor == null || unit.Descriptor.IsPet;

            public string ActorId(UnitEntityData unit) => unit?.UniqueId;

            public string PetLink(UnitEntityData unit) => unit?.Descriptor?.PetUniqueId;

            public bool OnPlayerRoster(UnitEntityData unit) => IsOnPlayerRoster(unit);

            public UnitEntityData ResolveCharacter(string actorId)
            {
                var characters = Game.Instance?.Player?.AllCharacters;
                if (characters == null || !MountedSaveData.ActorId(actorId)) return null;
                var matches = characters.Where(unit => unit != null && unit.UniqueId == actorId).Take(2).ToArray();
                return matches.Length == 1 ? matches[0] : null;
            }

            public bool ControlDesired(UnitEntityData unit) =>
                service.mountStateAbility != null && service.ShouldLease(unit, NativeMountedControlKind.MountState);

            public bool ControlLive(UnitEntityData unit)
            {
                var fact = service.mountStateAbility == null ? null : unit?.Descriptor?.Abilities?.GetAbility(service.mountStateAbility);
                return fact != null && fact.Active;
            }

            public void PrepareRider(UnitEntityData unit) => service.PrepareRider(unit);

            public int SlotCount(UnitEntityData unit) => unit?.UISettings?.Slots?.Length ?? 0;

            private static MechanicActionBarSlot Slot(UnitEntityData unit, int index)
            {
                var slots = unit?.UISettings?.Slots;
                return slots == null || index < 0 || index >= slots.Length ? null : slots[index];
            }

            public MountedToolbarSlot ReadSlot(UnitEntityData unit, int index)
            {
                var slot = Slot(unit, index);
                if (slot == null || slot is MechanicActionBarSlotEmpty) return MountedToolbarSlot.Empty;
                var kind = service.ResolveKind((slot as MechanicActionBarSlotAbility)?.Ability?.Blueprint);
                if (kind == NativeMountedControlKind.None) return MountedToolbarSlot.Ordinary;
                if (kind == NativeMountedControlKind.MountState) return MountedToolbarSlot.PublicControl;
                return MountedControlSlotPolicy.IsPublicControlKind(kind) ? MountedToolbarSlot.LegacyAlias : MountedToolbarSlot.OtherManaged;
            }

            public object SlotIdentity(UnitEntityData unit, int index) => Slot(unit, index);

            public bool IsNativeEmptySlot(UnitEntityData unit, int index)
            {
                var empty = Slot(unit, index) as MechanicActionBarSlotEmpty;
                return empty != null && ReferenceEquals(empty.Unit, unit);
            }

            public bool HoldsLiveBinding(UnitEntityData unit, int index)
            {
                var slot = Slot(unit, index) as MechanicActionBarSlotAbility;
                return service.IsCallableLiveBinding(unit, slot, service.ResolveKind(slot?.Ability?.Blueprint));
            }

            public void BindControl(UnitEntityData unit, int index)
            {
                var fact = service.mountStateAbility == null ? null : unit?.Descriptor?.Abilities?.GetAbility(service.mountStateAbility);
                if (fact == null || !fact.Active || unit.UISettings == null) return;
                unit.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = unit, Ability = fact.Data }, index);
            }

            public void ClearSlot(UnitEntityData unit, int index)
            {
                if (unit?.UISettings != null) unit.UISettings.SetSlot(new MechanicActionBarSlotEmpty { Unit = unit }, index);
            }

            public void BarChanged(UnitEntityData unit) => MarkBarChanged(unit);

            public void SanitizeOtherBindings(UnitEntityData unit, MountedToolbarBoundary boundary)
            {
                var slots = unit?.UISettings?.Slots;
                if (slots == null) return;
                if (boundary == MountedToolbarBoundary.ReserveSweep) { service.MigrateLegacyChargeSlots(unit, true); return; }
                var changed = false;
                for (var index = 0; index < slots.Length; index++)
                {
                    var blueprint = (slots[index] as MechanicActionBarSlotAbility)?.Ability?.Blueprint;
                    var kind = service.ResolveKind(blueprint);
                    if (kind == NativeMountedControlKind.None || MountedControlSlotPolicy.IsPublicControlKind(kind)) continue;
                    var placeholder = new MechanicActionBarSlotEmpty { Unit = unit };
                    // A save scope leases the binding back afterwards; every other boundary clears it.
                    if (boundary == MountedToolbarBoundary.Save)
                        service.hotbarSerializationLeases.Add(new HotbarSerializationLease(unit, index, blueprint, placeholder));
                    unit.UISettings.SetSlot(placeholder, index);
                    changed = true;
                }
                if (changed) MarkBarChanged(unit);
            }

            public void ClearManagedAutoUse(UnitEntityData unit, MountedToolbarBoundary boundary)
            {
                if (unit == null) return;
                var kind = service.ResolveKind(unit.AutoUseAbility?.Blueprint);
                if (kind == NativeMountedControlKind.None) return;
                // An update keeps the unchanged per-rider rule (the public control, its older aliases and the older Mounted
                // Charge; retired helpers have their own rule); a sanitation boundary clears any exact KMC selection.
                if (boundary == MountedToolbarBoundary.Update && kind != NativeMountedControlKind.MountState &&
                    kind != NativeMountedControlKind.MountCompanion && kind != NativeMountedControlKind.Dismount &&
                    kind != NativeMountedControlKind.MountedCharge) return;
                unit.AutoUseAbility = null;
            }

            public void RemoveManagedFacts(UnitEntityData unit)
            {
                var abilities = unit?.Descriptor?.Abilities;
                if (abilities == null) return;
                foreach (var blueprint in service.EnumerateBlueprints())
                    foreach (var fact in abilities.Enumerable.Where(item => ReferenceEquals(item.Blueprint, blueprint)).ToList())
                        abilities.RemoveFact(fact);
            }

            public bool InitialPlacementAllowed(UnitEntityData unit)
            {
                var settled = !service.restoringSlots && (service.PlacementSettled == null || service.PlacementSettled());
                var autofill = Kingmaker.UI.SettingsUI.SettingsRoot.Instance?.AutofillActionbarSlots?.CurrentValue == true;
                return unit?.UISettings != null && MountedControlSlotPolicy.ShouldAttemptInitialPlacement(ControlLive(unit), false, settled, autofill);
            }

            public int SelectInitialPlacementSlot(UnitEntityData unit) => service.SelectInitialPlacementSlot(unit);
        }
    }
}
