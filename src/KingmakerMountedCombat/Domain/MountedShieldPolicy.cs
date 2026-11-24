using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // What Mounted Shield needs to know about one rider and one mount at one moment, read from the native rider by the
    // integration adapter (MountedShieldService.ReadFacts). Nothing here is remembered: a snapshot taken at mount time
    // would let an equipment change or a retraining keep a stale benefit.
    public sealed class MountedShieldFacts
    {
        // KMC registered Mounted Shield and still publishes it (not withdrawn from a required feat list).
        public bool Available;
        // The authoritative pair is Mounted and is exactly this rider on exactly this mount, both alive in the world.
        public bool MountedPair;
        // The rider owns the Mounted Shield feature and its fact is active and turned on.
        public bool OwnsFeat;
        // A physical shield whose own native shield AC currently applies to the rider (an equipped shield in the active
        // off-hand of the current weapon set). A shield in the inventory or an inactive set, or a Shield spell, is not one.
        public bool EffectiveShield;
        // The shield's native base armor bonus: its armor type's own value, never the item's enhancement.
        public int ShieldBaseBonus;
        public bool ShieldFocus;
        public bool GreaterShieldFocus;
    }

    // Mounted Shield (Advanced Player's Guide), as adapted by KMC-MOUNTED-SHIELD-2026-11-23. One amount serves both
    // benefits: the shield bonus to the current mount's AC and the addition to the rider's Mounted Combat hit-negation
    // Mobility check. The amount is the shield's base bonus plus Shield Focus once plus Greater Shield Focus once (the
    // mission's explicit interpretation); enhancement never enters it. Without an effective physical shield the amount is
    // zero, focus included. The amount is what the rider supplies, not the mount's net AC change: when the mount already
    // has a stronger shield bonus, its AC is unchanged and the check still receives the full amount.
    public static class MountedShieldPolicy
    {
        public static int Amount(MountedShieldFacts facts)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (!facts.Available || !facts.MountedPair || !facts.OwnsFeat || !facts.EffectiveShield) return 0;
            var amount = Math.Max(0, facts.ShieldBaseBonus) + (facts.ShieldFocus ? 1 : 0) + (facts.GreaterShieldFocus ? 1 : 0);
            return amount;
        }

        // One line for logs and evidence: the amount and every term that produced it.
        public static string Describe(MountedShieldFacts facts) =>
            facts == null ? "none" :
            "available=" + facts.Available + " mountedPair=" + facts.MountedPair + " ownsFeat=" + facts.OwnsFeat +
            " shield=" + facts.EffectiveShield + " base=" + facts.ShieldBaseBonus + " shieldFocus=" + facts.ShieldFocus +
            " greaterShieldFocus=" + facts.GreaterShieldFocus + " amount=" + Amount(facts);
    }

    // One rider's wanted contribution: Amount on Mount's AC.
    public sealed class MountedShieldDesire<TUnit> where TUnit : class
    {
        public MountedShieldDesire(TUnit rider, TUnit mount, int amount)
        {
            Rider = rider;
            Mount = mount;
            Amount = amount;
        }

        public TUnit Rider { get; }
        public TUnit Mount { get; }
        public int Amount { get; }
    }

    // The mount-AC contributions Mounted Shield owns, one per rider, each recorded with the exact native value it wrote.
    // Reconcile is idempotent: it writes only where the wanted mount or amount changed, or where the recorded native value
    // is no longer applied (something else removed it), so repeated reconciliation never accumulates and an unchanged pair
    // costs no native write. Only this ledger's own values are ever removed; another source's shield bonus is never touched.
    // The native operations are injected so the ownership rules run, and are tested, without the engine.
    public sealed class MountedShieldContributionLedger<TUnit, TApplied> where TUnit : class where TApplied : class
    {
        private sealed class Entry
        {
            internal TUnit Rider, Mount;
            internal int Amount;
            internal TApplied Applied;
        }

        private readonly List<Entry> entries = new List<Entry>();
        private readonly Func<TUnit, TUnit, int, TApplied> apply;
        private readonly Func<TUnit, TApplied, bool> isApplied;
        private readonly Action<TUnit, TApplied> remove;

        // apply(rider, mount, amount): writes the native contribution and returns it, or null when it was refused.
        // isApplied(mount, applied): the recorded value is still on that mount. remove(mount, applied): takes it off.
        public MountedShieldContributionLedger(Func<TUnit, TUnit, int, TApplied> apply, Func<TUnit, TApplied, bool> isApplied, Action<TUnit, TApplied> remove)
        {
            this.apply = apply ?? throw new ArgumentNullException(nameof(apply));
            this.isApplied = isApplied ?? throw new ArgumentNullException(nameof(isApplied));
            this.remove = remove ?? throw new ArgumentNullException(nameof(remove));
        }

        public int Count => entries.Count;
        public IEnumerable<TUnit> Riders => entries.Select(entry => entry.Rider).ToArray();
        public int RefusedDesires { get; private set; }

        public bool TryGet(TUnit rider, out TUnit mount, out int amount)
        {
            var entry = entries.FirstOrDefault(value => ReferenceEquals(value.Rider, rider));
            mount = entry?.Mount;
            amount = entry?.Amount ?? 0;
            return entry != null;
        }

        public bool Owns(TApplied applied) => applied != null && entries.Any(entry => ReferenceEquals(entry.Applied, applied));

        // desired: one entry per rider with a positive amount on a distinct mount; anything else is ignored and counted in
        // RefusedDesires (a mount claimed by two riders gets neither). allowAdd false removes stale contributions only and
        // writes nothing new: the form used while ordinary updates are suspended. Returns the number of native writes.
        public int Reconcile(IEnumerable<MountedShieldDesire<TUnit>> desired, bool allowAdd)
        {
            var wanted = new List<MountedShieldDesire<TUnit>>();
            var contested = new List<TUnit>();
            RefusedDesires = 0;
            foreach (var desire in desired ?? Enumerable.Empty<MountedShieldDesire<TUnit>>())
            {
                if (desire == null || desire.Rider == null || desire.Mount == null || desire.Amount <= 0 || ReferenceEquals(desire.Rider, desire.Mount) ||
                    wanted.Any(value => ReferenceEquals(value.Rider, desire.Rider)))
                {
                    RefusedDesires++;
                    continue;
                }
                if (wanted.Any(value => ReferenceEquals(value.Mount, desire.Mount))) contested.Add(desire.Mount);
                wanted.Add(desire);
            }
            if (contested.Count != 0)
            {
                RefusedDesires += wanted.Count(value => contested.Any(mount => ReferenceEquals(mount, value.Mount)));
                wanted.RemoveAll(value => contested.Any(mount => ReferenceEquals(mount, value.Mount)));
            }

            var writes = 0;
            foreach (var entry in entries.ToArray())
            {
                var desire = wanted.FirstOrDefault(value => ReferenceEquals(value.Rider, entry.Rider));
                var keep = desire != null && ReferenceEquals(desire.Mount, entry.Mount) && desire.Amount == entry.Amount && isApplied(entry.Mount, entry.Applied);
                if (keep) continue;
                RemoveEntry(entry);
                writes++;
            }
            if (!allowAdd) return writes;
            foreach (var desire in wanted)
            {
                if (entries.Any(entry => ReferenceEquals(entry.Rider, desire.Rider))) continue;
                var applied = apply(desire.Rider, desire.Mount, desire.Amount);
                if (applied == null) continue;
                entries.Add(new Entry { Rider = desire.Rider, Mount = desire.Mount, Amount = desire.Amount, Applied = applied });
                writes++;
            }
            return writes;
        }

        // One rider only, as the next native AC read or an equipment change of that rider needs it now: the rider's own record
        // is kept, replaced or removed exactly as Reconcile would, and no other rider's record is touched. desire null means
        // the rider wants nothing. A mount another rider's record holds is never written. Returns the number of native writes.
        public int ReconcileRider(TUnit rider, MountedShieldDesire<TUnit> desire, bool allowAdd)
        {
            if (rider == null) return 0;
            if (desire != null && (!ReferenceEquals(desire.Rider, rider) || desire.Mount == null || desire.Amount <= 0 || ReferenceEquals(desire.Mount, rider)))
                desire = null;
            var writes = 0;
            var entry = entries.FirstOrDefault(value => ReferenceEquals(value.Rider, rider));
            if (entry != null)
            {
                if (desire != null && ReferenceEquals(desire.Mount, entry.Mount) && desire.Amount == entry.Amount && isApplied(entry.Mount, entry.Applied)) return 0;
                RemoveEntry(entry);
                writes++;
            }
            if (!allowAdd || desire == null || entries.Any(value => ReferenceEquals(value.Mount, desire.Mount))) return writes;
            var applied = apply(desire.Rider, desire.Mount, desire.Amount);
            if (applied == null) return writes;
            entries.Add(new Entry { Rider = desire.Rider, Mount = desire.Mount, Amount = desire.Amount, Applied = applied });
            return writes + 1;
        }

        // Removes the rider's contribution; true when there was one.
        public bool Release(TUnit rider)
        {
            var entry = entries.FirstOrDefault(value => ReferenceEquals(value.Rider, rider));
            if (entry == null) return false;
            RemoveEntry(entry);
            return true;
        }

        // Removes whatever contribution is on the mount; true when there was one.
        public bool ReleaseMount(TUnit mount)
        {
            var entry = entries.FirstOrDefault(value => ReferenceEquals(value.Mount, mount));
            if (entry == null) return false;
            RemoveEntry(entry);
            return true;
        }

        // Removes every contribution; returns how many there were. Every removal is attempted even if one throws.
        public int ReleaseAll()
        {
            var count = 0;
            Exception first = null;
            foreach (var entry in entries.ToArray())
            {
                try { RemoveEntry(entry); count++; }
                catch (Exception exception) { first = first ?? exception; }
            }
            if (first != null) throw new InvalidOperationException("A Mounted Shield contribution could not be removed.", first);
            return count;
        }

        // The record goes only after its native value was removed, so a failed removal is retried by the next pass.
        private void RemoveEntry(Entry entry)
        {
            if (isApplied(entry.Mount, entry.Applied)) remove(entry.Mount, entry.Applied);
            entries.Remove(entry);
        }
    }
}
