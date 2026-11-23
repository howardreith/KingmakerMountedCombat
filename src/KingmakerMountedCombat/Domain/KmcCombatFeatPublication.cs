using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // The ordered set of KMC combat feats appended to each shared feat-selection array.
    public static class KmcCombatFeatPublication
    {
        // Mounted Combat is required; Mounted Archery is appended after it only when it registered.
        public static T[] Items<T>(T mountedCombat, T mountedArchery) where T : class => Items(mountedCombat, mountedArchery, null);

        // Mounted Combat first, then each optional feat that registered, in the fixed order Mounted Archery, Mounted Shield.
        public static T[] Items<T>(T mountedCombat, T mountedArchery, T mountedShield) where T : class
        {
            if (mountedCombat == null) throw new ArgumentNullException(nameof(mountedCombat));
            var items = new List<T> { mountedCombat };
            foreach (var optional in new[] { mountedArchery, mountedShield })
            {
                if (optional == null) continue;
                if (items.Any(item => ReferenceEquals(item, optional)))
                    throw new ArgumentException("Each KMC combat feat is a distinct feature.", nameof(mountedArchery));
                items.Add(optional);
            }
            return items.ToArray();
        }
    }

    // The KMC combat feats that follow Mounted Combat and can each be withdrawn on their own. The value is the feat's index
    // in KMC's fixed publication order.
    public enum KmcOptionalCombatFeat
    {
        MountedArchery = 0,
        MountedShield = 1
    }

    // One shared selection array. Every exact lease is captured from the same original value, so the array has a small set
    // of KMC-known values: the original, and original + Mounted Combat + each subset of the registered optional feats in
    // their fixed order. Whole-array writes (publish, restore) need one of those values: any other value is foreign drift
    // and is never overwritten. Whether the array offers an optional feat is a separate question, answered by membership:
    // a foreign append or reorder that keeps the entry still offers it.
    public sealed class KmcCombatFeatArrayPublication<T> where T : class
    {
        internal const int OptionalCount = 2;
        private readonly T[] optional;
        // Indexed by inclusion mask (bit i: optional feat i is offered); null where the mask names a feat that did not register.
        private readonly ExactAppendOnlyArrayLease<T>[] values;

        public KmcCombatFeatArrayPublication(T[] source, T mountedCombat, T mountedArchery) : this(source, mountedCombat, mountedArchery, null) { }

        public KmcCombatFeatArrayPublication(T[] source, T mountedCombat, T mountedArchery, T mountedShield)
        {
            optional = new[] { mountedArchery, mountedShield };
            values = new ExactAppendOnlyArrayLease<T>[1 << OptionalCount];
            for (var mask = 0; mask < values.Length; mask++)
            {
                if (!Registered(mask)) continue;
                values[mask] = new ExactAppendOnlyArrayLease<T>(source, KmcCombatFeatPublication.Items(mountedCombat,
                    Pick(KmcOptionalCombatFeat.MountedArchery, mask), Pick(KmcOptionalCombatFeat.MountedShield, mask)));
            }
        }

        // The mask that offers every registered optional feat the predicate accepts.
        internal int Mask(Func<KmcOptionalCombatFeat, bool> offered)
        {
            var mask = 0;
            for (var index = 0; index < OptionalCount; index++)
                if (optional[index] != null && offered((KmcOptionalCombatFeat)index)) mask |= 1 << index;
            return mask;
        }

        // The array holds KMC's exact optional feature, wherever it is and whatever else the array holds.
        public bool Offers(T[] current, KmcOptionalCombatFeat feat)
        {
            var item = optional[(int)feat];
            return item != null && current != null && Array.IndexOf(current, item) >= 0;
        }

        public bool OffersMountedArchery(T[] current) => Offers(current, KmcOptionalCombatFeat.MountedArchery);

        // The narrow withdrawal: the same entries in the same order without KMC's own reference to that feat, or null when
        // the array does not hold it. Foreign entries, Mounted Combat and the other optional feat stay. Applied to one of KMC's
        // exact values this yields another exact KMC value, so disable can still restore the original exactly.
        public T[] Without(T[] current, KmcOptionalCombatFeat feat)
        {
            var item = optional[(int)feat];
            return Offers(current, feat) ? current.Where(entry => !ReferenceEquals(entry, item)).ToArray() : null;
        }

        public T[] WithoutMountedArchery(T[] current) => Without(current, KmcOptionalCombatFeat.MountedArchery);

        public bool IsKmcValue(T[] current) => values[0].MatchesOriginal(current) || values.Any(value => value != null && value.MatchesAppended(current));

        // mask null: the original value; otherwise Mounted Combat plus the optional feats the mask names.
        public bool Matches(T[] current, int? mask) =>
            mask == null ? values[0].MatchesOriginal(current) : values[mask.Value] != null && values[mask.Value].MatchesAppended(current);

        // Compare before write: the value to store, or null when the array already holds the target. False on drift.
        public bool TryMove(T[] current, int? mask, out T[] write)
        {
            write = null;
            if (mask != null && values[mask.Value] == null) throw new ArgumentException("The target names a feat that did not register.", nameof(mask));
            if (Matches(current, mask)) return true;
            if (!IsKmcValue(current)) return false;
            write = mask == null ? values[0].CreateOriginalValue() : values[mask.Value].CreateAppendedValue();
            return true;
        }

        private bool Registered(int mask)
        {
            for (var index = 0; index < OptionalCount; index++)
                if ((mask & (1 << index)) != 0 && optional[index] == null) return false;
            return true;
        }

        private T Pick(KmcOptionalCombatFeat feat, int mask) => (mask & (1 << (int)feat)) != 0 ? optional[(int)feat] : null;
    }

    // Every shared selection array KMC publishes its combat feats to, with the paths every character must be able to take
    // each optional feat from. Each optional feat's mechanic and its selectable entry stay coherent on their own: the feat
    // is available while KMC publishes and every required path still holds it (membership, not exact array identity, so
    // another mod adding or reordering other entries changes nothing). If a required path loses one optional feat, that
    // feat alone is withdrawn for the session: KMC's own reference to it is removed from every array that still holds it,
    // whether or not another mod changed that array, leaving Mounted Combat, the other optional feat and every foreign
    // entry in place. While withdrawn, a reference that reappears (another mod copied an older array) is removed again. A
    // non-required array that loses a feat just stops offering it there; the rule stays on. Losing Mounted Shield never
    // withdraws Mounted Archery, and losing Mounted Archery never withdraws Mounted Shield.
    public sealed class KmcCombatFeatPublicationSet<TKey, T> where TKey : class where T : class
    {
        private readonly List<KeyValuePair<TKey, KmcCombatFeatArrayPublication<T>>> arrays = new List<KeyValuePair<TKey, KmcCombatFeatArrayPublication<T>>>();
        private readonly List<TKey> required;
        private readonly Dictionary<TKey, T[]> lastSeen = new Dictionary<TKey, T[]>();
        private readonly Func<TKey, T[]> read;
        private readonly Action<TKey, T[]> write;
        private readonly bool[] registered = new bool[KmcCombatFeatArrayPublication<T>.OptionalCount];
        private readonly bool[] withdrawn = new bool[KmcCombatFeatArrayPublication<T>.OptionalCount];
        private readonly string[] reasons = new string[KmcCombatFeatArrayPublication<T>.OptionalCount];
        private bool published;

        public KmcCombatFeatPublicationSet(IEnumerable<TKey> selections, IEnumerable<TKey> requiredSelections, T mountedCombat, T mountedArchery,
            Func<TKey, T[]> read, Action<TKey, T[]> write) : this(selections, requiredSelections, mountedCombat, mountedArchery, null, read, write) { }

        public KmcCombatFeatPublicationSet(IEnumerable<TKey> selections, IEnumerable<TKey> requiredSelections, T mountedCombat, T mountedArchery,
            T mountedShield, Func<TKey, T[]> read, Action<TKey, T[]> write)
        {
            this.read = read ?? throw new ArgumentNullException(nameof(read));
            this.write = write ?? throw new ArgumentNullException(nameof(write));
            registered[(int)KmcOptionalCombatFeat.MountedArchery] = mountedArchery != null;
            registered[(int)KmcOptionalCombatFeat.MountedShield] = mountedShield != null;
            foreach (var key in selections ?? throw new ArgumentNullException(nameof(selections)))
            {
                if (key == null || arrays.Any(pair => ReferenceEquals(pair.Key, key))) throw new ArgumentException("Each selection once.", nameof(selections));
                arrays.Add(new KeyValuePair<TKey, KmcCombatFeatArrayPublication<T>>(key,
                    new KmcCombatFeatArrayPublication<T>(read(key), mountedCombat, mountedArchery, mountedShield)));
            }
            required = (requiredSelections ?? throw new ArgumentNullException(nameof(requiredSelections))).ToList();
            if (required.Count == 0 || required.Any(key => !arrays.Any(pair => ReferenceEquals(pair.Key, key))))
                throw new ArgumentException("Every required path must be a published selection.", nameof(requiredSelections));
        }

        public bool Has(KmcOptionalCombatFeat feat) => registered[(int)feat];
        public bool Withdrawn(KmcOptionalCombatFeat feat) => withdrawn[(int)feat];
        public string Reason(KmcOptionalCombatFeat feat) => reasons[(int)feat];

        public bool HasMountedArchery => Has(KmcOptionalCombatFeat.MountedArchery);
        public bool MountedArcheryWithdrawn => Withdrawn(KmcOptionalCombatFeat.MountedArchery);
        public string WithdrawalReason => Reason(KmcOptionalCombatFeat.MountedArchery);
        public bool HasMountedShield => Has(KmcOptionalCombatFeat.MountedShield);
        public bool MountedShieldWithdrawn => Withdrawn(KmcOptionalCombatFeat.MountedShield);
        public string MountedShieldWithdrawalReason => Reason(KmcOptionalCombatFeat.MountedShield);
        public IEnumerable<TKey> Selections => arrays.Select(pair => pair.Key);

        // Publish (desired) or restore (not desired) every array; returns the drift refusals, or null.
        public string Apply(bool desired, Func<TKey, string> name)
        {
            published = desired;
            string error = null;
            foreach (var pair in arrays)
            {
                int? target = desired ? pair.Value.Mask(feat => registered[(int)feat] && !withdrawn[(int)feat]) : (int?)null;
                T[] value;
                if (!pair.Value.TryMove(read(pair.Key), target, out value))
                {
                    error = (error == null ? "" : error + " ") + "Feat selection " + name(pair.Key) + " changed outside KMC; no overwrite was attempted.";
                    continue;
                }
                if (value != null) write(pair.Key, value);
            }
            lastSeen.Clear();
            if (desired) Refresh(true);
            return error;
        }

        // A mechanic's availability. A full comparison of the required arrays; withdraws on the first required drift.
        public bool Available(KmcOptionalCombatFeat feat)
        {
            Refresh(true);
            return published && registered[(int)feat] && !withdrawn[(int)feat];
        }

        public bool MountedArcheryAvailable() => Available(KmcOptionalCombatFeat.MountedArchery);
        public bool MountedShieldAvailable() => Available(KmcOptionalCombatFeat.MountedShield);

        // Withdraws each optional feat a required path no longer holds; keeps every array free of each withdrawn feat. Before
        // a feat's withdrawal, without a full comparison only a replaced required array is examined (one reference comparison
        // each), so it is cheap enough for every frame. Once any feat is withdrawn, every array's contents are inspected on
        // every call: another mod can put a withdrawn feat back into an array in place, without replacing it, and a reference
        // check would miss that (owner re-review of 713dd621). The required arrays are examined before any removal, so the
        // reference record a removal leaves never hides a replaced array from a feat still being watched. Returns true when
        // this call withdrew a feat (each feat is withdrawn once).
        public bool Refresh(bool fullComparison)
        {
            if (!published) return false;
            List<TKey>[] lost = null;
            if (Enumerable.Range(0, registered.Length).Any(Watched))
            {
                foreach (var key in required)
                {
                    var current = read(key);
                    if (!fullComparison && Unchanged(key, current)) continue;
                    lastSeen[key] = current;
                    for (var index = 0; index < registered.Length; index++)
                    {
                        if (!Watched(index) || Publication(key).Offers(current, (KmcOptionalCombatFeat)index)) continue;
                        lost = lost ?? new List<TKey>[registered.Length];
                        (lost[index] = lost[index] ?? new List<TKey>()).Add(key);
                    }
                }
            }
            var withdrewNow = false;
            if (lost != null)
            {
                for (var index = 0; index < registered.Length; index++)
                {
                    if (lost[index] == null) continue;
                    withdrawn[index] = true;
                    reasons[index] = string.Join(",", lost[index].Select(key => key.ToString()).ToArray());
                    withdrewNow = true;
                }
            }
            if (withdrawn.Any(value => value)) RemoveWithdrawn();
            return withdrewNow;
        }

        public bool Offers(TKey key) => Offers(key, KmcOptionalCombatFeat.MountedArchery);

        public bool Offers(TKey key, KmcOptionalCombatFeat feat) => registered[(int)feat] && Publication(key).Offers(read(key), feat);

        private bool Watched(int index) => registered[index] && !withdrawn[index];

        // The narrow removal of KMC's own reference to every withdrawn feat from every array that holds it, exact or
        // foreign-changed.
        private void RemoveWithdrawn()
        {
            foreach (var pair in arrays)
            {
                var current = read(pair.Key);
                var value = current;
                for (var index = 0; index < withdrawn.Length; index++)
                {
                    if (!withdrawn[index]) continue;
                    value = pair.Value.Without(value, (KmcOptionalCombatFeat)index) ?? value;
                }
                if (!ReferenceEquals(value, current))
                {
                    write(pair.Key, value);
                    current = read(pair.Key);
                }
                lastSeen[pair.Key] = current;
            }
        }

        private bool Unchanged(TKey key, T[] current)
        {
            T[] seen;
            return lastSeen.TryGetValue(key, out seen) && ReferenceEquals(seen, current);
        }

        private KmcCombatFeatArrayPublication<T> Publication(TKey key) => arrays.First(pair => ReferenceEquals(pair.Key, key)).Value;
    }
}
