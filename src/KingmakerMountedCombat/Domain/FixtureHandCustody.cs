using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // Custody of an actor's hand slots for a diagnostic fixture (supplemental review PN6-F2). Every slot's original occupant is
    // captured before anything changes. A slot is emptied only through the native removal and only when the native slot allows
    // it (a non-removable occupant refuses, and nothing changes); an item is placed only into an empty slot that accepts it, and
    // every step is verified by the slot's actual occupant. Restore puts each original back into its own slot (off-hands first,
    // so a two-handed primary returns to a free pair) and reports every slot still wrong; it can be called again to retry.
    public sealed class FixtureHandCustody<TSlot, TItem> where TSlot : class where TItem : class
    {
        private readonly Func<TSlot, TItem> occupant;
        private readonly Func<TSlot, bool> canRemove;
        private readonly Action<TSlot> remove;
        private readonly Func<TSlot, TItem, bool> canInsert;
        private readonly Action<TSlot, TItem> insert;
        private readonly Func<TItem, TSlot> holder;
        private readonly Func<TSlot, bool> isPrimary;
        private readonly List<KeyValuePair<TSlot, TItem>> original = new List<KeyValuePair<TSlot, TItem>>();
        // How a slot and an item are named in a restoration failure (alpha closure review, PN14-F1): the actor, set and hand, and
        // the expected and observed item identities, so the failed record identifies exactly what is outstanding.
        public Func<TSlot, string> DescribeSlot { get; set; } = slot => slot?.ToString() ?? "none";
        public Func<TItem, string> DescribeItem { get; set; } = item => item?.ToString() ?? "none";

        public FixtureHandCustody(Func<TSlot, TItem> occupant, Func<TSlot, bool> canRemove, Action<TSlot> remove, Func<TSlot, TItem, bool> canInsert,
            Action<TSlot, TItem> insert, Func<TItem, TSlot> holder, Func<TSlot, bool> isPrimary)
        {
            this.occupant = occupant ?? throw new ArgumentNullException(nameof(occupant));
            this.canRemove = canRemove ?? throw new ArgumentNullException(nameof(canRemove));
            this.remove = remove ?? throw new ArgumentNullException(nameof(remove));
            this.canInsert = canInsert ?? throw new ArgumentNullException(nameof(canInsert));
            this.insert = insert ?? throw new ArgumentNullException(nameof(insert));
            this.holder = holder ?? throw new ArgumentNullException(nameof(holder));
            this.isPrimary = isPrimary ?? throw new ArgumentNullException(nameof(isPrimary));
        }

        public int Captured => original.Count;

        public void Capture(IEnumerable<TSlot> slots)
        {
            if (original.Count != 0) throw new InvalidOperationException("The hands are already in custody.");
            foreach (var slot in slots) original.Add(new KeyValuePair<TSlot, TItem>(slot, occupant(slot)));
        }

        // Empties the slot through the native removal, verified. False, with nothing changed, when the native slot refuses.
        public bool Vacate(TSlot slot)
        {
            var item = occupant(slot);
            if (item == null) return true;
            if (!canRemove(slot)) return false;
            remove(slot);
            return occupant(slot) == null && !ReferenceEquals(holder(item), slot);
        }

        // Places the item into an empty slot that accepts it, verified. False, with nothing changed, otherwise.
        public bool Place(TSlot slot, TItem item)
        {
            if (occupant(slot) != null || !canInsert(slot, item)) return false;
            insert(slot, item);
            return ReferenceEquals(occupant(slot), item) && ReferenceEquals(holder(item), slot);
        }

        // Every original back in its own slot; returns a description of each slot still wrong (empty when all are restored).
        // A slot's foreign occupant is removed first: created items through releaseCreated (which must take them out of the
        // slot), anything else through Vacate.
        public string[] Restore(Func<TItem, bool> isCreated, Func<TItem, bool> releaseCreated)
        {
            var order = original.Where(pair => !isPrimary(pair.Key)).Concat(original.Where(pair => isPrimary(pair.Key))).ToArray();
            foreach (var pair in order)
            {
                var current = occupant(pair.Key);
                if (ReferenceEquals(current, pair.Value)) continue;
                if (current != null && !(isCreated(current) ? releaseCreated(current) && occupant(pair.Key) == null : Vacate(pair.Key))) continue;
                if (pair.Value == null) continue;
                var held = holder(pair.Value);
                if (held != null && !ReferenceEquals(held, pair.Key) && !Vacate(held)) continue;
                Place(pair.Key, pair.Value);
            }
            return original.Select((pair, index) => new { pair, index }).Where(value => !ReferenceEquals(occupant(value.pair.Key), value.pair.Value))
                .Select(value => "hand " + value.index + " not restored: " + DescribeSlot(value.pair.Key) + " expected " + DescribeItem(value.pair.Value) +
                    ", observed " + DescribeItem(occupant(value.pair.Key))).ToArray();
        }
    }
}
