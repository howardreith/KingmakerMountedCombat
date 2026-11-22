using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // The player's arrangements of the public Mount / Dismount control in one player world: the state that
    // MountedToolbarLifecycle owns and sequences. Actor ids, slot indices, pet ids and the identities of slot objects on
    // the current world's bars; the world is held weakly.
    //
    // An arrangement is one chosen slot (or "off the bar"), whether that slot is pending (owed back after native or KMC
    // cleanup emptied it), and the known copies: every other public binding seen on the bar, by index and slot object. A
    // copy is never the player's new placement; a binding that is not a known copy is.
    public sealed class MountedControlPlacementIntent
    {
        public const int OffBar = -1;

        public sealed class Arrangement
        {
            // The chosen slot, or OffBar.
            public int Slot = OffBar;
            // The chosen slot was emptied by native cleanup or a KMC sweep and is owed back.
            public bool Pending;
            // The companion the arrangement was observed with; null until observed (an archive row).
            public string PetId;
            // Other public bindings known on the bar: index -> slot object, held weakly (identity only; a slot object that
            // left every bar is never kept alive by this record).
            public readonly Dictionary<int, WeakReference> Copies = new Dictionary<int, WeakReference>();

            public bool IsCopyObject(int index, object identity)
            {
                WeakReference copy;
                return Copies.TryGetValue(index, out copy) && ReferenceEquals(copy.Target, identity);
            }

            public void RecordCopy(int index, object identity) => Copies[index] = new WeakReference(identity);
            // The bar has been observed since the arrangement was made (an archive row's arrangement has not).
            public bool Observed;
            // The pending slot was seeded from an older Mount / Dismount archive row (its restore is that row's migration).
            public bool FromAliasRow;
        }

        // One public binding on a bar, in F2's precedence (canonical bindings by index, then older aliases by index).
        public struct Binding
        {
            public int Index;
            public object Identity;
            public bool Alias;
        }

        private readonly WeakReference world;
        private readonly Dictionary<string, Arrangement> arrangements = new Dictionary<string, Arrangement>(StringComparer.Ordinal);
        // Retired on evidence of permanent loss in this world, with no new arrangement since: rows captured earlier in this
        // world (an area transfer, a retained remount) never recreate it.
        private readonly HashSet<string> retired = new HashSet<string>(StringComparer.Ordinal);
        // Actors that had their one automatic placement, or any arrangement, in this world (archive kind 7).
        private readonly HashSet<string> placed = new HashSet<string>(StringComparer.Ordinal);

        public MountedControlPlacementIntent(object world) { this.world = new WeakReference(world); }

        public bool BelongsTo(object currentWorld) => currentWorld != null && ReferenceEquals(world.Target, currentWorld);

        public int Count => arrangements.Count;
        public IEnumerable<string> ActorIds => arrangements.Keys.ToArray();
        public IEnumerable<string> PlacedIds => placed.ToArray();
        public bool IsPlaced(string id) => id != null && placed.Contains(id);
        public bool IsRetired(string id) => id != null && retired.Contains(id);
        public void MarkPlaced(string id) { if (id != null) placed.Add(id); }

        public Arrangement Get(string id)
        {
            Arrangement arrangement;
            return id != null && arrangements.TryGetValue(id, out arrangement) ? arrangement : null;
        }

        // A new arrangement on new evidence: the actor is placed and no longer retired.
        public Arrangement Create(string id, string petId)
        {
            var arrangement = new Arrangement { PetId = string.IsNullOrEmpty(petId) ? null : petId };
            arrangements[id] = arrangement;
            placed.Add(id);
            retired.Remove(id);
            return arrangement;
        }

        // The actor owns no rider state at all (a positively identified pet): arrangement and placement record are dropped.
        public void Forget(string id)
        {
            if (id == null) return;
            arrangements.Remove(id);
            placed.Remove(id);
        }

        public void Retire(string id)
        {
            if (id == null) return;
            arrangements.Remove(id);
            retired.Add(id);
        }

        public void Clear() { arrangements.Clear(); retired.Clear(); placed.Clear(); }

        public static bool IsCopy(Arrangement arrangement, Binding binding) =>
            binding.Index != arrangement.Slot && arrangement.IsCopyObject(binding.Index, binding.Identity);

        // The evidence rule for an existing arrangement, from the bar as it stands (bindings in precedence order; occupied:
        // indices holding anything else that is not empty; slotCount: the bar's length):
        // - copies that left the bar (removed, replaced or moved) are forgotten; an unobserved arrangement first takes the
        //   older aliases on its bar as copies (F2 leftovers, never the player's placement);
        // - the first binding that is not a known copy is the player's: it is the chosen slot from now on (a move, a fresh
        //   placement, a re-placement at a pending slot), live;
        // - with none, a live arrangement was removed by the player (off the bar; its copies yield); a pending one keeps
        //   waiting while its slot is free, and when the player filled its slot the first surviving copy becomes the
        //   choice (the fallback), or the shortcut is off the bar;
        // - every other public binding on the bar is a known copy afterwards.
        public static void ApplyBar(Arrangement arrangement, IList<Binding> bindings, ICollection<int> occupied, int slotCount)
        {
            foreach (var copy in arrangement.Copies.ToArray())
                if (!bindings.Any(binding => binding.Index == copy.Key && ReferenceEquals(binding.Identity, copy.Value.Target)))
                    arrangement.Copies.Remove(copy.Key);
            if (!arrangement.Observed)
                foreach (var binding in bindings)
                    if (binding.Alias && binding.Index != arrangement.Slot) arrangement.RecordCopy(binding.Index, binding.Identity);
            arrangement.Observed = true;
            var fresh = bindings.Where(binding => !IsCopy(arrangement, binding)).ToList();
            if (fresh.Count > 0)
            {
                arrangement.Slot = fresh[0].Index;
                arrangement.Pending = false;
                arrangement.FromAliasRow = false;
            }
            else if (!arrangement.Pending) arrangement.Slot = OffBar;
            else if (arrangement.Slot < 0 || arrangement.Slot >= slotCount || occupied.Contains(arrangement.Slot))
            {
                var fallback = bindings.Where(binding => IsCopy(arrangement, binding)).Select(binding => (int?)binding.Index).FirstOrDefault();
                arrangement.Slot = fallback ?? OffBar;
                arrangement.Pending = false;
                arrangement.FromAliasRow = false;
                if (fallback.HasValue) arrangement.Copies.Remove(fallback.Value);
            }
            foreach (var binding in bindings)
                if (binding.Index != arrangement.Slot) arrangement.RecordCopy(binding.Index, binding.Identity);
            arrangement.Copies.Remove(arrangement.Slot);
        }
    }
}
