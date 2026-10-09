using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // The disposable fixture may auto-replace a consumed usable with an original
    // item. Retain exact original placements; never delete or spend that item.
    internal sealed class CastingFixtureSlotSnapshot<TSlot, TItem>
        where TSlot : class where TItem : class
    {
        private readonly TSlot[] slots;
        private readonly TItem[] items;
        private readonly Func<TSlot, TItem> read;
        // Occupant identity alone is not placement: the native item must also point back at its
        // slot (HoldingSlot). A foreign refill that moved an original into a disposable slot leaves
        // the original slot referencing an item whose holding slot was cleared on removal.
        private readonly Func<TSlot, TItem, bool> linked;
        internal static TSlot[] IncludeOriginalOwnerSlots(IEnumerable<TSlot> riderSlots,
            IEnumerable<TSlot> originalHeldSlots, Func<TSlot, IEnumerable<TSlot>> ownerSlots)
        {
            var result = new List<TSlot>();
            foreach (var source in riderSlots.Concat(originalHeldSlots))
            {
                var container = ownerSlots(source).ToArray();
                if (!container.Any(s => ReferenceEquals(s, source)))
                    throw new InvalidOperationException("Original item slot is outside its exact owner container.");
                foreach (var slot in container)
                    if (!result.Any(s => ReferenceEquals(s, slot))) result.Add(slot);
            }
            return result.ToArray();
        }
        internal CastingFixtureSlotSnapshot(IEnumerable<TSlot> slots, Func<TSlot, TItem> read)
            : this(slots, read, null) { }
        internal CastingFixtureSlotSnapshot(IEnumerable<TSlot> slots, Func<TSlot, TItem> read, Func<TSlot, TItem, bool> linked)
        {
            this.slots = slots.ToArray(); this.read = read; this.linked = linked ?? ((slot, item) => true);
            if (this.slots.Length == 0 || this.slots.Any(s => s == null))
                throw new InvalidOperationException("Original fixture quick slots are unavailable.");
            items = this.slots.Select(read).ToArray();
        }
        internal bool Restored => slots.Select((s, i) => ReferenceEquals(read(s), items[i]) && (items[i] == null || linked(s, items[i]))).All(b => b);
        internal bool ExactSlots(IEnumerable<TSlot> current)
        {
            var actual = current.ToArray();
            return actual.Length == slots.Length && actual.Select((s, i) => ReferenceEquals(s, slots[i])).All(b => b);
        }
        internal void Restore(Func<TSlot, bool> remove, Action<TSlot, TItem> insert)
        {
            // Validate every displaced occupant before touching any original item.
            foreach (var slot in slots)
            {
                var current = read(slot);
                if (current != null && !items.Any(i => ReferenceEquals(i, current)))
                    throw new InvalidOperationException("Unknown quick-slot occupant; fixture owner retained.");
            }
            for (var i = 0; i < slots.Length; i++)
                if (!ReferenceEquals(read(slots[i]), items[i]) && read(slots[i]) != null && !remove(slots[i]))
                    throw new InvalidOperationException("Original fixture item detach failed; owner retained.");
            for (var i = 0; i < slots.Length; i++)
                if (items[i] != null && !ReferenceEquals(read(slots[i]), items[i])) insert(slots[i], items[i]);
            // An original that sits in its slot without pointing back at it is re-linked through the
            // same native removal and insertion; its resources are never touched.
            for (var i = 0; i < slots.Length; i++)
            {
                if (items[i] == null || !ReferenceEquals(read(slots[i]), items[i]) || linked(slots[i], items[i])) continue;
                if (!remove(slots[i])) throw new InvalidOperationException("Original fixture item relink detach failed; owner retained.");
                insert(slots[i], items[i]);
            }
            if (!Restored) throw new InvalidOperationException("Original quick-slot placement was not restored; owner retained.");
        }
    }
}