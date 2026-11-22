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
            // The slot object of the binding at the chosen slot when it was last seen or written (held weakly), or null.
            public WeakReference ChoiceIdentity;

            public void RecordChoice(object identity) => ChoiceIdentity = identity == null ? null : new WeakReference(identity);

            // Every public binding this arrangement knows on the bar by slot object: the chosen binding and the copies.
            public List<KeyValuePair<int, WeakReference>> KnownBindings()
            {
                var known = Copies.Where(copy => copy.Value.Target != null).ToList();
                if (Slot >= 0 && ChoiceIdentity?.Target != null) known.Add(new KeyValuePair<int, WeakReference>(Slot, ChoiceIdentity));
                return known;
            }
            // The bar has been observed since the arrangement was made (an archive row's arrangement has not).
            public bool Observed;
            // The pending slot was seeded from an older Mount / Dismount archive row (its restore is that row's migration).
            public bool FromAliasRow;
            // A fresh load's public archive rows (archive order), kept while the actor or its bar was unreadable when they
            // were seeded: the choice among them is made once the bar's occupancy can be read. Null once chosen.
            public List<MountedToolbarRow> Alternatives;
            // The chosen binding was an older Mount / Dismount alias (an F2 leftover the player can no longer place), not the
            // canonical control.
            public bool ChoiceIsAlias;
            // Made from an observation of the incoming world's bar before that world's archive rows were restored.
            public bool ObservedBeforeRows;
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
        // Retired while their bar could not be read: the public bindings that predate the retirement, by index and slot
        // object (the arrangement's known bindings), or null when the bar was never read (then every binding the bar holds
        // when it is next readable predates it). They are cleared, and never read as a placement, when the bar returns; a
        // binding the player placed after that is a fresh placement.
        private readonly Dictionary<string, List<KeyValuePair<int, WeakReference>>> retiredUnread =
            new Dictionary<string, List<KeyValuePair<int, WeakReference>>>(StringComparer.Ordinal);

        public MountedControlPlacementIntent(object world) { this.world = new WeakReference(world); }

        // This world's own archive rows have been restored (a freshly loaded save's seeding ran).
        public bool RowsSeeded;

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

        // A second retirement while the bar is still unread never widens a known record to "everything".
        public void MarkRetiredUnread(string id, List<KeyValuePair<int, WeakReference>> predating)
        {
            if (id == null || predating == null && retiredUnread.ContainsKey(id)) return;
            retiredUnread[id] = predating;
        }

        public bool TakeRetiredUnread(string id, out List<KeyValuePair<int, WeakReference>> predating)
        {
            predating = null;
            return id != null && retiredUnread.TryGetValue(id, out predating) && retiredUnread.Remove(id);
        }

        // A binding (index, slot object) that predates an unread retirement: every binding when the bar was never read.
        public static bool Predates(List<KeyValuePair<int, WeakReference>> predating, int index, object identity) =>
            predating == null || predating.Any(known => known.Key == index && known.Value.Target != null && ReferenceEquals(known.Value.Target, identity));

        public void Clear() { arrangements.Clear(); retired.Clear(); placed.Clear(); retiredUnread.Clear(); RowsSeeded = false; }

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
                arrangement.ChoiceIsAlias = fresh[0].Alias;
            }
            else if (!arrangement.Pending) arrangement.Slot = OffBar;
            else if (arrangement.Slot < 0 || arrangement.Slot >= slotCount || occupied.Contains(arrangement.Slot))
            {
                var fallback = bindings.Where(binding => IsCopy(arrangement, binding)).Select(binding => (Binding?)binding).FirstOrDefault();
                arrangement.Slot = fallback?.Index ?? OffBar;
                arrangement.Pending = false;
                arrangement.FromAliasRow = false;
                arrangement.ChoiceIsAlias = fallback?.Alias ?? false;
                if (fallback.HasValue) arrangement.Copies.Remove(fallback.Value.Index);
            }
            foreach (var binding in bindings)
                if (binding.Index != arrangement.Slot) arrangement.RecordCopy(binding.Index, binding.Identity);
            arrangement.Copies.Remove(arrangement.Slot);
            arrangement.RecordChoice(bindings.Where(binding => binding.Index == arrangement.Slot).Select(binding => binding.Identity).FirstOrDefault());
        }
    }
}
