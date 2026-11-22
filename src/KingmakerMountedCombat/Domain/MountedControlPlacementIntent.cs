using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // The player's arrangements of the public Mount / Dismount control in one player world: the state that
    // MountedToolbarLifecycle owns and sequences. Actor ids, slot indices and pet ids only; the world is held weakly, never
    // a native reference.
    //
    // An arrangement is the set of toolbar slots where the player keeps the control and the subset that is pending (owed
    // back after native or KMC cleanup emptied it). An empty set is "off the bar".
    public sealed class MountedControlPlacementIntent
    {
        public sealed class Arrangement
        {
            public readonly SortedSet<int> Slots = new SortedSet<int>();
            public readonly SortedSet<int> Pending = new SortedSet<int>();
            // The companion the arrangement was observed with; null until observed (an archive row).
            public string PetId;
            // Older aliases on the bar outside the arrangement at the last observation; null until observed.
            public HashSet<int> KnownAliases;
            // Pending slots seeded from an older Mount / Dismount archive row (their restore is that row's migration).
            public readonly HashSet<int> FromAliasRow = new HashSet<int>();
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

        public void Retire(string id)
        {
            if (id == null) return;
            arrangements.Remove(id);
            retired.Add(id);
        }

        public void Clear() { arrangements.Clear(); retired.Clear(); placed.Clear(); }

        // The evidence rule for an existing arrangement, from the bar as it stands (slotCount slots; canonical and alias
        // public bindings; occupied: anything else that is not empty):
        // - a slot of the arrangement no longer on the bar and not pending was removed by the player;
        // - a public binding at a slot outside the arrangement, back at a pending slot, or a new older alias, was placed by
        //   the player: the bar is the arrangement from now on and earlier pending slots are superseded;
        // - a pending slot the player filled with something else, or beyond the bar, is given up.
        // An alias inside the arrangement stands for the control; one outside it is a leftover unless it is new.
        public static void ApplyBar(Arrangement arrangement, ICollection<int> canonical, ICollection<int> aliases, ICollection<int> occupied, int slotCount)
        {
            var present = new SortedSet<int>(canonical);
            present.UnionWith(aliases.Where(arrangement.Slots.Contains));
            var placedNow = new SortedSet<int>(present.Where(index => !arrangement.Slots.Contains(index) || arrangement.Pending.Contains(index)));
            if (arrangement.KnownAliases != null)
                placedNow.UnionWith(aliases.Where(index => !arrangement.Slots.Contains(index) && !arrangement.KnownAliases.Contains(index)));
            var kept = arrangement.Slots.Where(index => !arrangement.Pending.Contains(index) && present.Contains(index)).ToList();
            var pending = arrangement.Pending.ToArray();
            arrangement.Slots.Clear();
            arrangement.Slots.UnionWith(kept);
            if (placedNow.Count > 0)
            {
                arrangement.Pending.Clear();
                arrangement.Slots.UnionWith(placedNow);
            }
            else arrangement.Slots.UnionWith(pending);
            foreach (var index in arrangement.Pending.ToArray())
            {
                if (index < slotCount && !occupied.Contains(index)) continue;
                arrangement.Pending.Remove(index);
                arrangement.Slots.Remove(index);
            }
            arrangement.FromAliasRow.IntersectWith(arrangement.Pending);
            arrangement.KnownAliases = new HashSet<int>(aliases.Where(index => !arrangement.Slots.Contains(index)));
        }
    }
}
