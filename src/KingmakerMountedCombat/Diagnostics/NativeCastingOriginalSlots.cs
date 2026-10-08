using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // One original placement snapshot for the disposable item cohort. Native
    // auto-fill can move an original shared-inventory item from another actor.
    // Capture that exact owner before acquisition, never guess it after removal.
    internal sealed class NativeCastingOriginalSlots
    {
        private readonly UsableSlot[] slots;
        private readonly UnitDescriptor[] owners;
        private readonly UnitBody[] bodies;
        private readonly NativeCastingItemTrace trace;
        private readonly CastingFixtureSlotSnapshot<UsableSlot, ItemEntity> state;
        internal JObject Evidence { get; } = new JObject();

        internal NativeCastingOriginalSlots(UnitEntityData rider, NativeCastingItemTrace trace)
        {
            this.trace = trace;
            slots = CastingFixtureSlotSnapshot<UsableSlot, ItemEntity>.IncludeOriginalOwnerSlots(
                rider.Body.QuickSlots, rider.Inventory.Items.Select(i => i.HoldingSlot).OfType<UsableSlot>(),
                s => s.Owner.Body.QuickSlots);
            owners = slots.Select(s => s.Owner).ToArray();
            bodies = owners.Select(o => o.Body).ToArray();
            state = new CastingFixtureSlotSnapshot<UsableSlot, ItemEntity>(slots, s => s.MaybeItem);
            Evidence["before"] = Snapshot();
        }

        private void RequireExactOwners()
        {
            for (var i = 0; i < slots.Length; i++)
                if (!ReferenceEquals(slots[i].Owner, owners[i]) || !ReferenceEquals(owners[i].Body, bodies[i]))
                    throw new InvalidOperationException("Original usable-slot owner changed; fixture debt retained.");
            var current = CastingFixtureSlotSnapshot<UsableSlot, ItemEntity>.IncludeOriginalOwnerSlots(
                slots, new UsableSlot[0], s => s.Owner.Body.QuickSlots);
            if (!state.ExactSlots(current))
                throw new InvalidOperationException("Original usable-slot container changed; fixture debt retained.");
        }

        internal bool Restored { get { RequireExactOwners(); return state.Restored; } }
        internal JArray Snapshot() => new JArray(slots.Select(s => new JObject {
            ["owner"] = s.Owner.Unit.UniqueId, ["slot"] = trace.Identity(s),
            ["item"] = trace.Identity(s.MaybeItem), ["blueprint"] = s.MaybeItem?.Blueprint.AssetGuid,
            ["holdingSlot"] = trace.Identity(s.MaybeItem?.HoldingSlot),
            ["count"] = s.MaybeItem?.Count, ["charges"] = s.MaybeItem?.Charges }));

        internal void Restore()
        {
            RequireExactOwners();
            state.Restore(NativeCastingItemLease.RemoveExactSlot, (slot, original) => {
                if (original.Collection != slot.Owner.Unit.Inventory || original.HoldingSlot != null ||
                    slot.HasItem || !slot.CanInsertItem(original))
                    throw new InvalidOperationException("Exact original usable item cannot return to its owner; fixture debt retained.");
                slot.InsertItem(original);
            });
            Evidence["after"] = Snapshot();
            Evidence["restored"] = Restored;
        }
    }
}