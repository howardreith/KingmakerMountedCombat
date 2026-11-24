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

    // Another mod's version of a KMC feat would be a second, identically named choice with a different rule. It is looked for
    // in every array KMC will publish the feat to (not only the ordinary list: a fighter-only competitor is still offered
    // beside KMC's). Returns the first competing entry, or null.
    public static class KmcCombatFeatCollision
    {
        public static T FindCompeting<T>(IEnumerable<T[]> publicationTargets, Func<T, string> assetName, Func<T, string> displayName,
            string symbol, string display) where T : class
        {
            if (publicationTargets == null) throw new ArgumentNullException(nameof(publicationTargets));
            foreach (var array in publicationTargets)
            {
                if (array == null) continue;
                foreach (var item in array)
                {
                    if (item == null) continue;
                    var name = assetName(item);
                    if (name != null && name.IndexOf(symbol, StringComparison.OrdinalIgnoreCase) >= 0) return item;
                    if (string.Equals(displayName(item), display, StringComparison.OrdinalIgnoreCase)) return item;
                }
            }
            return null;
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
        private readonly T mountedCombat;
        private readonly T[] optional;
        // Indexed by inclusion mask (bit i: optional feat i is offered); null where the mask names a feat that did not register.
        // Rebased once disable has stripped KMC's entries from an array another mod changed (Rebase).
        private ExactAppendOnlyArrayLease<T>[] values;

        public KmcCombatFeatArrayPublication(T[] source, T mountedCombat, T mountedArchery) : this(source, mountedCombat, mountedArchery, null) { }

        public KmcCombatFeatArrayPublication(T[] source, T mountedCombat, T mountedArchery, T mountedShield)
        {
            this.mountedCombat = mountedCombat;
            optional = new[] { mountedArchery, mountedShield };
            values = Leases(source);
        }

        private ExactAppendOnlyArrayLease<T>[] Leases(T[] source)
        {
            var leases = new ExactAppendOnlyArrayLease<T>[1 << OptionalCount];
            for (var mask = 0; mask < leases.Length; mask++)
            {
                if (!Registered(mask)) continue;
                leases[mask] = new ExactAppendOnlyArrayLease<T>(source, KmcCombatFeatPublication.Items(mountedCombat,
                    Pick(KmcOptionalCombatFeat.MountedArchery, mask), Pick(KmcOptionalCombatFeat.MountedShield, mask)));
            }
            return leases;
        }

        // After disable stripped KMC's own entries from an array another mod changed, that stripped value becomes the
        // array's original: re-enabling appends KMC's entries after the foreign entries (only while the array still holds
        // exactly that value) and disabling again restores it exactly. False, with nothing changed, when the value cannot
        // be leased (it holds a null or a KMC entry).
        public bool Rebase(T[] stripped)
        {
            if (stripped == null) return false;
            try { values = Leases(stripped); return true; }
            catch (ArgumentException) { return false; }
        }

        // A second reference to one of KMC's own entries (another mod copied it into another slot): the same entries in the
        // same order keeping only the first occurrence of each KMC entry, or null when there is no such duplicate. Foreign
        // entries, including foreign duplicates, are untouched.
        public T[] WithoutKmcDuplicates(T[] current)
        {
            if (current == null) return null;
            var duplicated = false;
            foreach (var item in new[] { mountedCombat }.Concat(optional))
            {
                if (item == null) continue;
                var first = Array.IndexOf(current, item);
                if (first >= 0 && Array.IndexOf(current, item, first + 1) >= 0) { duplicated = true; break; }
            }
            if (!duplicated) return null;
            var kmc = new[] { mountedCombat }.Concat(optional.Where(item => item != null)).ToArray();
            var seen = new List<T>();
            var result = new List<T>(current.Length);
            foreach (var entry in current)
            {
                if (kmc.Any(item => ReferenceEquals(item, entry)))
                {
                    if (seen.Any(item => ReferenceEquals(item, entry))) continue;
                    seen.Add(entry);
                }
                result.Add(entry);
            }
            return result.ToArray();
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

        // The inverse of Without for the array as it is now (supplemental review PN6-F1): KMC's own reference to the feat goes
        // back right after the KMC entry that precedes it in KMC's order (Mounted Combat, Mounted Archery, Mounted Shield), and
        // every other entry, foreign or KMC, keeps its place, so another mod's edits made meanwhile are never undone. Null when
        // the array already offers it or holds no KMC entry to place it after. Applied to one of KMC's exact values this yields
        // the exact value with the feat.
        public T[] With(T[] current, KmcOptionalCombatFeat feat)
        {
            var item = optional[(int)feat];
            if (current == null || item == null || Offers(current, feat)) return null;
            var order = new[] { mountedCombat }.Concat(optional).ToArray();
            var position = Array.IndexOf(order, item);
            for (var anchor = position - 1; anchor >= 0; anchor--)
            {
                if (order[anchor] == null) continue;
                var at = Array.FindLastIndex(current, entry => ReferenceEquals(entry, order[anchor]));
                if (at < 0) continue;
                var value = new List<T>(current);
                value.Insert(at + 1, item);
                return value.ToArray();
            }
            return null;
        }

        // Disable of an array another mod changed: the same entries in the same order without any of KMC's own references
        // (Mounted Combat and every registered optional feat), or null when it holds none. Nothing stays selectable whose
        // mechanic the disable turned off, and every foreign entry is kept.
        public T[] WithoutKmc(T[] current)
        {
            if (current == null) return null;
            var kmc = new[] { mountedCombat }.Concat(optional.Where(item => item != null)).ToArray();
            if (!current.Any(entry => kmc.Any(item => ReferenceEquals(item, entry)))) return null;
            return current.Where(entry => !kmc.Any(item => ReferenceEquals(item, entry))).ToArray();
        }

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
        // Reversibly closed (supplemental review PN4-F3): the feat's entries leave every array, it is not available and its
        // required paths are not watched, until Resume. Unlike a withdrawal it does not last the session.
        private readonly bool[] suspended = new bool[KmcCombatFeatArrayPublication<T>.OptionalCount];
        // The arrays the suspension took the entry from (supplemental review PN5-F1, PN6-F1). Resume puts KMC's entry back into
        // each of them as it is then (With), so a list another mod changed, before or during the suspension, keeps all of its
        // current entries and order, gets the suspended entry back once, and is never read as having lost it.
        private readonly HashSet<TKey> suspensionKeys = new HashSet<TKey>();
        // The arrays KMC itself published while withholding a suspended feat (supplemental review PN7-F1): a stop before
        // registration finished, or a disable and enable while suspended. The feat's absence there is KMC's own, so Resume puts it
        // back into each of them as it is then, even after another mod appended to or reordered it.
        private readonly HashSet<TKey>[] withheldKeys = Enumerable.Range(0, KmcCombatFeatArrayPublication<T>.OptionalCount).Select(index => new HashSet<TKey>()).ToArray();
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
        public bool Suspended(KmcOptionalCombatFeat feat) => suspended[(int)feat];
        public IEnumerable<TKey> Selections => arrays.Select(pair => pair.Key);

        // Publish (desired) or restore (not desired) every array; returns the drift refusals, or null.
        public string Apply(bool desired, Func<TKey, string> name)
        {
            // A publication that is already live (an explicit enable after a terminal stop, which leaves the lists published)
            // writes nothing (supplemental review PN9-F2): every list is as KMC left it or as another mod has since edited it,
            // and an edit (a feat another mod removed) is kept. Suspended feats come back only through Resume, and only where
            // KMC's own suspension or publication made them absent.
            if (desired && published) { lastSeen.Clear(); Refresh(true); return null; }
            published = desired;
            // A restore supersedes what a suspension recorded: the arrays return to KMC's originals (or are stripped and rebased).
            // A publication while suspended keeps the records: it leaves the suspended entry out and writes no foreign array.
            if (!desired)
            {
                suspensionKeys.Clear();
                foreach (var keys in withheldKeys) keys.Clear();
            }
            string error = null;
            foreach (var pair in arrays)
            {
                int? target = desired ? pair.Value.Mask(Offered) : (int?)null;
                T[] value;
                if (!pair.Value.TryMove(read(pair.Key), target, out value))
                {
                    if (!desired)
                    {
                        // Restoring is refused for a foreign-changed array, but KMC's own entries leave it: the services are
                        // off, so none of them may stay selectable. Foreign entries and their order are kept.
                        var stripped = pair.Value.WithoutKmc(read(pair.Key));
                        if (stripped != null) write(pair.Key, stripped);
                        // The stripped value becomes the array's original, so re-enabling republishes KMC's entries after the
                        // foreign ones instead of treating KMC's own removal as a lost required offer.
                        var rebased = pair.Value.Rebase(read(pair.Key));
                        error = (error == null ? "" : error + " ") + "Feat selection " + name(pair.Key) +
                            " changed outside KMC; only KMC's own entries were removed from it" +
                            (rebased ? ", and its foreign contents are kept as its original." : ".");
                        continue;
                    }
                    error = (error == null ? "" : error + " ") + "Feat selection " + name(pair.Key) + " changed outside KMC; no overwrite was attempted.";
                    continue;
                }
                if (value == null) continue;
                write(pair.Key, value);
                // KMC itself just wrote this array without each suspended feat, in a transition from unpublished (reviews PN8-F1,
                // PN9-F2): an array that already matched needed no write and is not recorded, so another mod's own removal of
                // the feat is never undone by Resume.
                if (desired)
                    for (var index = 0; index < suspended.Length; index++)
                        if (suspended[index] && registered[index] && !withdrawn[index]) withheldKeys[index].Add(pair.Key);
            }
            lastSeen.Clear();
            if (desired) Refresh(true);
            return error;
        }

        // A mechanic's availability. A full comparison of the required arrays; withdraws on the first required drift.
        public bool Available(KmcOptionalCombatFeat feat)
        {
            Refresh(true);
            return published && Offered(feat);
        }

        private bool Offered(KmcOptionalCombatFeat feat) => registered[(int)feat] && !withdrawn[(int)feat] && !suspended[(int)feat];

        // Closes one optional feat reversibly: KMC's own entry leaves every array now (exact or foreign-changed, foreign entries
        // kept) and stays out of every later publication until Resume. A suspended feat is never withdrawn for missing from a
        // required path, which it is by design.
        public void Suspend(KmcOptionalCombatFeat feat)
        {
            if (!registered[(int)feat] || suspended[(int)feat]) return;
            suspended[(int)feat] = true;
            foreach (var pair in arrays)
            {
                var before = read(pair.Key);
                var after = pair.Value.Without(before, feat);
                if (after == null || ReferenceEquals(after, before)) continue;
                write(pair.Key, after);
                suspensionKeys.Add(pair.Key);
                lastSeen[pair.Key] = read(pair.Key);
            }
        }

        // Reopens a suspended feat. While KMC publishes, its entry returns where the suspension is the reason it is absent: to each
        // array the suspension took it from and each array KMC published without it (KMC's exact value moves to the full
        // publication; a list another mod changed since gets the entry in place, keeping every current entry and order). A list
        // KMC never wrote without it stays as another mod left it. Returns the arrays it could not be put back into. Not
        // publishing, the next publication includes it.
        public string Resume(KmcOptionalCombatFeat feat, Func<TKey, string> name)
        {
            if (!suspended[(int)feat]) return null;
            suspended[(int)feat] = false;
            if (!published) { suspensionKeys.Clear(); withheldKeys[(int)feat].Clear(); return null; }
            string error = null;
            foreach (var pair in arrays)
            {
                var current = read(pair.Key);
                if (pair.Value.Offers(current, feat)) continue;
                T[] value;
                if (!suspensionKeys.Contains(pair.Key) && !withheldKeys[(int)feat].Contains(pair.Key)) continue;
                if (!pair.Value.TryMove(current, pair.Value.Mask(Offered), out value)) value = pair.Value.With(current, feat);
                if (value != null) { write(pair.Key, value); continue; }
                if (pair.Value.Matches(current, pair.Value.Mask(Offered))) continue;
                error = (error == null ? "" : error + " ") + "Feat selection " + name(pair.Key) + " no longer holds a KMC entry to place the resumed feat after; it was not written.";
            }
            suspensionKeys.Clear();
            withheldKeys[(int)feat].Clear();
            lastSeen.Clear();
            Refresh(true);
            return error;
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
            if (withdrawn.Any(value => value) || suspended.Any(value => value)) RemoveClosed();
            // One occurrence of each KMC entry per array: a copied second reference would be offered twice.
            if (fullComparison) RemoveKmcDuplicates();
            return withdrewNow;
        }

        public bool Offers(TKey key) => Offers(key, KmcOptionalCombatFeat.MountedArchery);

        public bool Offers(TKey key, KmcOptionalCombatFeat feat) => registered[(int)feat] && Publication(key).Offers(read(key), feat);

        private bool Watched(int index) => registered[index] && !withdrawn[index] && !suspended[index];

        // The narrow removal of KMC's own reference to every withdrawn or suspended feat from every array that holds it, exact
        // or foreign-changed.
        private void RemoveClosed()
        {
            foreach (var pair in arrays)
            {
                var current = read(pair.Key);
                var value = current;
                for (var index = 0; index < withdrawn.Length; index++)
                {
                    if (!withdrawn[index] && !suspended[index]) continue;
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

        private void RemoveKmcDuplicates()
        {
            foreach (var pair in arrays)
            {
                var value = pair.Value.WithoutKmcDuplicates(read(pair.Key));
                if (value == null) continue;
                write(pair.Key, value);
                lastSeen[pair.Key] = read(pair.Key);
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
