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
        {
            this.slots = slots.ToArray(); this.read = read;
            if (this.slots.Length == 0 || this.slots.Any(s => s == null))
                throw new InvalidOperationException("Original fixture quick slots are unavailable.");
            items = this.slots.Select(read).ToArray();
        }
        internal bool Restored => slots.Select((s, i) => ReferenceEquals(read(s), items[i])).All(b => b);
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
            if (!Restored) throw new InvalidOperationException("Original quick-slot placement was not restored; owner retained.");
        }
    }
}