using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Own only one newly-created native item in the guarded disposable fixture.
    // Factory initialization owns charges. Never add/refill spell slots or charges.
    internal sealed class NativeCastingItemLease : IDisposable
    {
        internal const string LesserQuickenRod = "55a059b32df920c4abe65b8ee8b56056";
        internal const string CurePotion = "d52566ae8cbe8dc4dae977ef51c27d91";
        internal const string CureScroll = "cd635d5720937b044a354dba17abad8d2";
        private readonly UnitEntityData rider;
        private readonly NativeCastingItemTrace trace;
        private readonly string blueprint;
        private UsableSlot slot;
        private bool disposed;
        private int rodBuffsBefore;
        private const string RodBuff = "db36e9189250df94a8e8474ee6c331e1";
        internal ItemEntityUsable Item { get; private set; }
        internal JObject Evidence { get; } = new JObject();
        internal NativeCastingItemLease(UnitEntityData rider, NativeCastingItemTrace trace, string blueprint)
        {
            this.rider = rider; this.trace = trace; this.blueprint = blueprint;
            if (blueprint != LesserQuickenRod && blueprint != CurePotion && blueprint != CureScroll)
                throw new InvalidOperationException("Casting fixture item is not in the exact native fixture list.");
        }
        internal void Acquire()
        {
            if (Item != null || disposed) throw new InvalidOperationException("Fixture item acquisition already attempted.");
            if (rider.IsInCombat || !rider.Commands.Empty)
                throw new InvalidOperationException("Fixture item acquisition requires settled exploration.");
            var template = ResourcesLibrary.TryGetBlueprint<BlueprintItem>(blueprint);
            if (template == null) throw new InvalidOperationException("Native casting fixture item blueprint unavailable.");
            slot = rider.Body.QuickSlots.FirstOrDefault(s => !s.HasItem);
            if (slot == null) throw new InvalidOperationException("No original empty rider quick slot is available.");
            Evidence["blueprint"] = blueprint;
            Evidence["rider"] = rider.UniqueId;
            Evidence["slot"] = trace.Identity(slot);
            Evidence["slotOriginallyEmpty"] = !slot.HasItem;
            Evidence["inventoryBefore"] = Inventory();
            rodBuffsBefore = rider.Buffs.Enumerable.Count(b => b.Blueprint.AssetGuid == RodBuff);
            Evidence["rodBuffsBefore"] = rodBuffsBefore;
            try
            {
                Item = template.CreateEntity<ItemEntityUsable>();
                if (Item == null || Item.Count != 1 || Item.Collection != null || Item.HoldingSlot != null)
                    throw new InvalidOperationException("Native item factory did not create one unowned exact usable item.");
                Evidence["created"] = Snapshot();
                Item.Identify();
                var inserted = rider.Inventory.Add(Item, true);
                if (!ReferenceEquals(inserted, Item) || Item.Collection != rider.Inventory)
                    throw new InvalidOperationException("Native item insertion changed exact fixture identity.");
                if (!slot.CanInsertItem(Item)) throw new InvalidOperationException("Native quick slot refused fixture item.");
                slot.InsertItem(Item);
                if (!ReferenceEquals(slot.MaybeItem, Item) || Item.HoldingSlot != slot)
                    throw new InvalidOperationException("Native equip did not retain the exact fixture item.");
                Evidence["equipped"] = Snapshot();
                Evidence["inventoryAfter"] = Inventory();
            }
            catch
            {
                // Keep the exact item reference if native cleanup fails; never
                // claim disposal by clearing a field or logging an exception.
                throw;
            }
        }
        internal JObject Snapshot() => new JObject {
            ["item"] = trace.Identity(Item), ["blueprint"] = Item?.Blueprint.AssetGuid,
            ["count"] = Item?.Count, ["charges"] = Item?.Charges,
            ["collection"] = trace.Identity(Item?.Collection), ["holdingSlot"] = trace.Identity(Item?.HoldingSlot),
            ["exactRiderCollection"] = Item != null && Item.Collection == rider.Inventory,
            ["exactSlot"] = Item != null && ReferenceEquals(slot?.MaybeItem, Item),
            ["ability"] = trace.Identity(Item?.Ability), ["abilityCaster"] = Item?.Ability?.Data?.Caster?.Unit?.UniqueId,
            ["activatable"] = trace.Identity(Item?.ActivatableAbility),
            ["activatableSourceItem"] = trace.Identity(Item?.ActivatableAbility?.SourceItem),
            ["activatableOn"] = Item?.ActivatableAbility?.IsOn
        };
        private JArray Inventory() => new JArray(rider.Inventory.Items.Select(i => new JObject {
            ["item"] = trace.Identity(i), ["blueprint"] = i.Blueprint.AssetGuid, ["count"] = i.Count,
            ["charges"] = i.Charges, ["holdingSlot"] = trace.Identity(i.HoldingSlot) }));
        public void Dispose()
        {
            if (disposed) return;
            Evidence["beforeCleanup"] = Snapshot();
            if (Item != null)
            {
                var activation = Item.ActivatableAbility;
                if (ReferenceEquals(slot?.MaybeItem, Item) && !slot.RemoveItem())
                    throw new InvalidOperationException("Exact casting fixture slot removal failed; item ownership retained.");
                if (Item.HoldingSlot != null) throw new InvalidOperationException("Fixture item retains another slot; owner retained.");
                if (Item.Collection != null) Item.Collection.Remove(Item);
                if (Item.Collection != null || rider.Inventory.Items.Any(i => ReferenceEquals(i, Item)) || slot.HasItem)
                    throw new InvalidOperationException("Fixture item removal postconditions failed; exact owner retained.");
                if (activation != null && (activation.Active || activation.IsOn))
                    throw new InvalidOperationException("Native item activation remains live; fixture owner retained.");
                if (blueprint == LesserQuickenRod && rider.Buffs.Enumerable.Count(b => b.Blueprint.AssetGuid == RodBuff) != rodBuffsBefore)
                    throw new InvalidOperationException("Native rod buff remains; fixture owner retained.");
                Item.Dispose();
            }
            Evidence["afterCleanup"] = Snapshot();
            Evidence["noOwnedItemResident"] = Item == null || !rider.Inventory.Items.Any(i => ReferenceEquals(i, Item));
            Evidence["slotRestored"] = slot == null || !slot.HasItem;
            Evidence["disposed"] = true;
            disposed = true;
        }
    }
}