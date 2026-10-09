using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Own only one newly-created native item in the guarded disposable fixture.
    // Factory initialization owns charges. Never add/refill spell slots or charges.
    internal sealed class NativeCastingItemLease : IDisposable
    {
        internal const string LesserQuickenRod = "55a059b32df920c4abe65b8ee8b56056";
        internal const string CurePotion = "d52566ae8cbe8dc4dae977ef51c27d91";
        internal const string CureScroll = "cd635d5720937b044a354dba17abad8d";
        private readonly UnitEntityData rider;
        private readonly NativeCastingItemTrace trace;
        private readonly string blueprint;
        private readonly NativeCastingOriginalSlots originalQuickSlots;
        private UsableSlot slot;
        private ActivatableAbility ownedActivation;
        private bool released, disposed;
        private int rodBuffsBefore;
        private const string RodBuff = "db36e9189250df94a8e8474ee6c331e1";
        internal ItemEntityUsable Item { get; private set; }
        internal JObject Evidence { get; } = new JObject();
        internal NativeCastingItemLease(UnitEntityData rider, NativeCastingItemTrace trace, string blueprint,
            NativeCastingOriginalSlots originalQuickSlots)
        {
            this.rider = rider; this.trace = trace; this.blueprint = blueprint;
            this.originalQuickSlots = originalQuickSlots ?? throw new ArgumentNullException(nameof(originalQuickSlots));
            if (blueprint != LesserQuickenRod && blueprint != CurePotion && blueprint != CureScroll)
                throw new InvalidOperationException("Casting fixture item is not in the exact native fixture list.");
        }
        // One exact native item entity. A count above one is the native stack of that many
        // factory-created disposable items (ItemEntity.IncrementCount), released together.
        internal void Acquire(int count = 1)
        {
            if (count < 1 || count > 32) throw new ArgumentOutOfRangeException(nameof(count), "Fixture item stack count is outside the bounded disposable range.");
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
            Evidence["quickSlotsBefore"] = originalQuickSlots.Evidence["before"].DeepClone();
            rodBuffsBefore = rider.Buffs.Enumerable.Count(b => b.Blueprint.AssetGuid == RodBuff);
            Evidence["rodBuffsBefore"] = rodBuffsBefore;
            try
            {
                Item = template.CreateEntity<ItemEntityUsable>();
                if (Item == null || Item.Count != 1 || Item.Collection != null || Item.HoldingSlot != null)
                    throw new InvalidOperationException("Native item factory did not create one unowned exact usable item.");
                Evidence["created"] = Snapshot();
                Evidence["requestedCount"] = count;
                if (count > 1)
                {
                    if (!Item.IsStackable) throw new InvalidOperationException("Native fixture item does not stack; one item only.");
                    Item.IncrementCount(count - 1, true);
                    if (Item.Count != count) throw new InvalidOperationException("Native stack count did not reach the requested disposable count.");
                    Evidence["stacked"] = Snapshot();
                }
                Item.Identify();
                var inserted = rider.Inventory.Add(Item, true);
                if (!ReferenceEquals(inserted, Item) || Item.Collection != rider.Inventory)
                    throw new InvalidOperationException("Native item insertion changed exact fixture identity.");
                if (!slot.CanInsertItem(Item)) throw new InvalidOperationException("Native quick slot refused fixture item.");
                slot.InsertItem(Item);
                if (!ReferenceEquals(slot.MaybeItem, Item) || Item.HoldingSlot != slot)
                    throw new InvalidOperationException("Native equip did not retain the exact fixture item.");
                ownedActivation = Item.ActivatableAbility;
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
            ["slotOccupant"] = trace.Identity(slot?.MaybeItem),
            ["slotOccupantBlueprint"] = slot?.MaybeItem?.Blueprint.AssetGuid,
            ["exactInventoryResident"] = Item != null && rider.Inventory.Items.Any(i => ReferenceEquals(i, Item)),
            ["ability"] = trace.Identity(Item?.Ability), ["abilityCaster"] = Item?.Ability?.Data?.Caster?.Unit?.UniqueId,
            ["activatable"] = trace.Identity(Item?.ActivatableAbility),
            ["activatableSourceItem"] = trace.Identity(Item?.ActivatableAbility?.SourceItem),
            ["activatableOn"] = Item?.ActivatableAbility?.IsOn,
            ["retainedActivationActive"] = ownedActivation?.Active,
            ["retainedActivationOn"] = ownedActivation?.IsOn,
            ["retainedActivationDisposed"] = ownedActivation?.IsDisposed
        };
        private JArray Inventory() => new JArray(rider.Inventory.Items.Select(i => new JObject {
            ["item"] = trace.Identity(i), ["blueprint"] = i.Blueprint.AssetGuid, ["count"] = i.Count,
            ["charges"] = i.Charges, ["holdingSlot"] = trace.Identity(i.HoldingSlot) }));
        private static readonly MethodInfo NativeRemoveItem = typeof(ItemSlot).GetMethod("RemoveItem",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(bool), typeof(bool) }, null);
        internal static bool RemoveExactSlot(UsableSlot exactSlot)
        {
            if (NativeRemoveItem == null || NativeRemoveItem.MetadataToken != 0x06007C7E ||
                NativeRemoveItem.Module.ModuleVersionId.ToString() != "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7")
                throw new InvalidOperationException("Pinned native fixture removal overload differs; owner retained.");
            try
            {
                var result = NativeRemoveItem.Invoke(exactSlot, new object[] { true, false });
                if (!(result is bool)) throw new InvalidOperationException("Native fixture removal returned no observed result.");
                return (bool)result;
            }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException ?? exception).Throw();
                throw;
            }
        }
        internal static void TurnOffExactActivation(ActivatableAbility activation, ItemEntityUsable exactItem,
            Kingmaker.UnitLogic.UnitDescriptor exactOwner)
        {
            if (activation == null) return;
            if (!ReferenceEquals(activation.SourceItem, exactItem) || !ReferenceEquals(activation.Owner, exactOwner))
                throw new InvalidOperationException("Native fixture activation owner changed; exact lease retained.");
            if (activation.IsOn) activation.IsOn = false;
            if (activation.IsOn)
                throw new InvalidOperationException("Native fixture activation did not turn off; exact lease retained.");
        }
        internal void ReleaseOwnedItem()
        {
            if (disposed) return;
            Evidence["beforeCleanup"] = Snapshot();
            if (Item != null && !released)
            {
                if (ownedActivation == null) ownedActivation = Item.ActivatableAbility;
                // Native fact disposal leaves the IsOn flag unchanged. Use the
                // normal toggle while the exact fixture activation is still owned.
                TurnOffExactActivation(ownedActivation, Item, rider.Descriptor);
                if (ReferenceEquals(slot?.MaybeItem, Item) && !RemoveExactSlot(slot))
                    throw new InvalidOperationException("Exact casting fixture slot removal failed; item ownership retained.");
                if (Item.HoldingSlot != null) throw new InvalidOperationException("Fixture item retains another slot; owner retained.");
                if (Item.Collection != null && Item.Collection != rider.Inventory)
                    throw new InvalidOperationException("Fixture item moved to a different inventory; owner retained.");
                if (Item.Collection != null) Item.Collection.Remove(Item);
            }
            Evidence["removalPostconditions"] = Snapshot();
            if (Item != null)
            {
                if (Item.Collection != null || Item.HoldingSlot != null || rider.Inventory.Items.Any(i => ReferenceEquals(i, Item)))
                    throw new InvalidOperationException("Fixture item removal postconditions failed; exact owner retained: " + Evidence["removalPostconditions"].ToString(Newtonsoft.Json.Formatting.None));
                if (ownedActivation != null && (ownedActivation.Active || ownedActivation.IsOn))
                    throw new InvalidOperationException("Native item activation remains live; fixture owner retained.");
                if (blueprint == LesserQuickenRod && rider.Buffs.Enumerable.Count(b => b.Blueprint.AssetGuid == RodBuff) != rodBuffsBefore)
                    throw new InvalidOperationException("Native rod buff remains; fixture owner retained.");
            }
            released = true;
            Evidence["ownedItemReleased"] = true;
        }
        public void Dispose()
        {
            if (disposed) return;
            ReleaseOwnedItem();
            // All cohort items first release their exact native ownership. Keep
            // every lease until the shared original placement postcondition passes.
            if (!originalQuickSlots.Restored || slot != null && slot.HasItem)
                throw new InvalidOperationException("Original cohort slots remain unsettled; fixture owner retained.");
            Evidence["quickSlotsAfter"] = originalQuickSlots.Snapshot();
            Item?.Dispose();
            Evidence["afterCleanup"] = Snapshot();
            Evidence["noOwnedItemResident"] = Item == null || !rider.Inventory.Items.Any(i => ReferenceEquals(i, Item));
            Evidence["slotRestored"] = originalQuickSlots.Restored && (slot == null || !slot.HasItem);
            Evidence["disposed"] = true;
            disposed = true;
        }
    }
}