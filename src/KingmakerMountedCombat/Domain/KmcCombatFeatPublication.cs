using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // The ordered set of KMC combat feats appended to each shared feat-selection array.
    public static class KmcCombatFeatPublication
    {
        // Mounted Combat is required; Mounted Archery is appended after it only when it registered.
        public static T[] Items<T>(T mountedCombat, T mountedArchery) where T : class
        {
            if (mountedCombat == null) throw new ArgumentNullException(nameof(mountedCombat));
            if (mountedArchery == null) return new[] { mountedCombat };
            if (ReferenceEquals(mountedCombat, mountedArchery))
                throw new ArgumentException("Mounted Archery is a distinct feature.", nameof(mountedArchery));
            return new[] { mountedCombat, mountedArchery };
        }
    }

    public enum KmcCombatFeatPublicationTarget
    {
        Original,
        MountedCombatOnly,
        BothFeats
    }

    // One shared selection array. Both exact leases are captured from the same original value, so the array has exactly
    // three KMC-known values: the original, original + Mounted Combat, and original + Mounted Combat + Mounted Archery.
    // Whole-array writes (publish, restore) need one of those values: any other value is foreign drift and is never
    // overwritten. Whether the array offers Mounted Archery is a separate question, answered by membership: a foreign
    // append or reorder that keeps the entry still offers it.
    public sealed class KmcCombatFeatArrayPublication<T> where T : class
    {
        private readonly ExactAppendOnlyArrayLease<T> both;
        private readonly ExactAppendOnlyArrayLease<T> combatOnly;
        private readonly T mountedArchery;

        public KmcCombatFeatArrayPublication(T[] source, T mountedCombat, T mountedArchery)
        {
            both = new ExactAppendOnlyArrayLease<T>(source, KmcCombatFeatPublication.Items(mountedCombat, mountedArchery));
            combatOnly = mountedArchery == null ? both : new ExactAppendOnlyArrayLease<T>(source, mountedCombat);
            this.mountedArchery = mountedArchery;
        }

        // The array holds KMC's exact Mounted Archery feature, wherever it is and whatever else the array holds.
        public bool OffersMountedArchery(T[] current) => mountedArchery != null && current != null && Array.IndexOf(current, mountedArchery) >= 0;

        // The narrow withdrawal: the same entries in the same order without KMC's own Mounted Archery reference, or null
        // when the array does not hold it. Foreign entries and Mounted Combat stay. Applied to KMC's exact published value
        // this yields KMC's exact Mounted-Combat-only value, so disable can still restore the original exactly.
        public T[] WithoutMountedArchery(T[] current) =>
            OffersMountedArchery(current) ? current.Where(item => !ReferenceEquals(item, mountedArchery)).ToArray() : null;

        public bool IsKmcValue(T[] current) => both.MatchesOriginal(current) || both.MatchesAppended(current) || combatOnly.MatchesAppended(current);

        public bool Matches(T[] current, KmcCombatFeatPublicationTarget target) =>
            target == KmcCombatFeatPublicationTarget.Original ? both.MatchesOriginal(current) :
            target == KmcCombatFeatPublicationTarget.MountedCombatOnly ? combatOnly.MatchesAppended(current) : both.MatchesAppended(current);

        // Compare before write: the value to store, or null when the array already holds the target. False on drift.
        public bool TryMove(T[] current, KmcCombatFeatPublicationTarget target, out T[] write)
        {
            write = null;
            if (Matches(current, target)) return true;
            if (!IsKmcValue(current)) return false;
            write = target == KmcCombatFeatPublicationTarget.Original ? both.CreateOriginalValue() :
                target == KmcCombatFeatPublicationTarget.MountedCombatOnly ? combatOnly.CreateAppendedValue() : both.CreateAppendedValue();
            return true;
        }
    }

    // Every shared selection array KMC publishes its combat feats to, with the paths every character must be able to
    // take Mounted Archery from. The mechanic and the selectable feat stay coherent: Mounted Archery is available while
    // KMC publishes and every required path still holds the feature (membership, not exact array identity, so another
    // mod adding or reordering other entries changes nothing). If a required path loses it, Mounted Archery is withdrawn
    // for the session: KMC's own Mounted Archery reference is removed from every array that still holds it, whether or
    // not another mod changed that array, leaving Mounted Combat and every foreign entry in place. While withdrawn, a
    // reference that reappears (another mod copied an older array) is removed again. A non-required array that loses
    // the feature just stops offering it there; the rule stays on.
    public sealed class KmcCombatFeatPublicationSet<TKey, T> where TKey : class where T : class
    {
        private readonly List<KeyValuePair<TKey, KmcCombatFeatArrayPublication<T>>> arrays = new List<KeyValuePair<TKey, KmcCombatFeatArrayPublication<T>>>();
        private readonly List<TKey> required;
        private readonly Dictionary<TKey, T[]> lastSeen = new Dictionary<TKey, T[]>();
        private readonly Func<TKey, T[]> read;
        private readonly Action<TKey, T[]> write;
        private bool published;

        public KmcCombatFeatPublicationSet(IEnumerable<TKey> selections, IEnumerable<TKey> requiredSelections, T mountedCombat, T mountedArchery,
            Func<TKey, T[]> read, Action<TKey, T[]> write)
        {
            this.read = read ?? throw new ArgumentNullException(nameof(read));
            this.write = write ?? throw new ArgumentNullException(nameof(write));
            HasMountedArchery = mountedArchery != null;
            foreach (var key in selections ?? throw new ArgumentNullException(nameof(selections)))
            {
                if (key == null || arrays.Any(pair => ReferenceEquals(pair.Key, key))) throw new ArgumentException("Each selection once.", nameof(selections));
                arrays.Add(new KeyValuePair<TKey, KmcCombatFeatArrayPublication<T>>(key,
                    new KmcCombatFeatArrayPublication<T>(read(key), mountedCombat, mountedArchery)));
            }
            required = (requiredSelections ?? throw new ArgumentNullException(nameof(requiredSelections))).ToList();
            if (required.Count == 0 || required.Any(key => !arrays.Any(pair => ReferenceEquals(pair.Key, key))))
                throw new ArgumentException("Every required path must be a published selection.", nameof(requiredSelections));
        }

        public bool HasMountedArchery { get; }
        public bool MountedArcheryWithdrawn { get; private set; }
        public string WithdrawalReason { get; private set; }
        public IEnumerable<TKey> Selections => arrays.Select(pair => pair.Key);

        // Publish (desired) or restore (not desired) every array; returns the drift refusals, or null.
        public string Apply(bool desired, Func<TKey, string> name)
        {
            published = desired;
            string error = null;
            var target = !desired ? KmcCombatFeatPublicationTarget.Original :
                MountedArcheryWithdrawn || !HasMountedArchery ? KmcCombatFeatPublicationTarget.MountedCombatOnly : KmcCombatFeatPublicationTarget.BothFeats;
            foreach (var pair in arrays)
            {
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

        // The mechanic's availability. A full comparison of the required arrays; withdraws on the first required drift.
        public bool MountedArcheryAvailable()
        {
            Refresh(true);
            return published && HasMountedArchery && !MountedArcheryWithdrawn;
        }

        // Withdraws when a required path no longer holds Mounted Archery; once withdrawn, keeps every array free of it.
        // Before withdrawal, without a full comparison only a replaced required array is examined (one reference comparison
        // each), so it is cheap enough for every frame. Once withdrawn, every array's contents are inspected on every call:
        // another mod can put Mounted Archery back into an array in place, without replacing it, and a reference check would
        // miss that (owner re-review of 713dd621). Returns true once, when it withdraws.
        public bool Refresh(bool fullComparison)
        {
            if (!published || !HasMountedArchery) return false;
            if (MountedArcheryWithdrawn)
            {
                RemoveMountedArchery(true);
                return false;
            }
            List<TKey> lost = null;
            foreach (var key in required)
            {
                var current = read(key);
                if (!fullComparison && Unchanged(key, current)) continue;
                lastSeen[key] = current;
                if (!Publication(key).OffersMountedArchery(current)) (lost = lost ?? new List<TKey>()).Add(key);
            }
            if (lost == null) return false;
            MountedArcheryWithdrawn = true;
            WithdrawalReason = string.Join(",", lost.Select(key => key.ToString()).ToArray());
            RemoveMountedArchery(true);
            return true;
        }

        public bool Offers(TKey key) => HasMountedArchery && Publication(key).OffersMountedArchery(read(key));

        // The narrow removal of KMC's own reference from every array that holds it, exact or foreign-changed.
        private void RemoveMountedArchery(bool fullComparison)
        {
            foreach (var pair in arrays)
            {
                var current = read(pair.Key);
                if (!fullComparison && Unchanged(pair.Key, current)) continue;
                var value = pair.Value.WithoutMountedArchery(current);
                if (value != null)
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
