using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Mounted Shield (KMC-MOUNTED-SHIELD-2026-11-23): the production amount policy, the production contribution ledger that
    // owns the mount AC values, the production three-feat coordinated publication and the production feat contract. The
    // native reads (shield base data, effective shield, AC stacking, the skill-check Bonus) run on genuine engine objects in
    // scripts/Test-MountedShieldNativeContract.ps1; none of this is native gameplay evidence.
    internal static class MountedShieldTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("mounted shield amount: base + Shield Focus once + Greater Shield Focus once; buckler/light/heavy/tower +2/+2/+3/+5", AmountByShieldType);
            runner.Run("mounted shield amount: zero without an effective shield (focus included), the feat, the exact mounted pair or availability", AmountGates);
            runner.Run("mounted shield ledger: two pairs with different shields each get only their own amount, once", TwoPairIsolation);
            runner.Run("mounted shield ledger: repeated reconciliation of unchanged pairs writes nothing and never accumulates", IdempotentReconcile);
            runner.Run("mounted shield ledger: shield change, mount swap and dismount move or remove exactly the rider's value", ChangeSwapDismount);
            runner.Run("mounted shield ledger: with the update gates closed only stale values go; nothing new is written", RemovalOnlyPass);
            runner.Run("mounted shield ledger: a value removed by something else is restored once; a contested mount gets neither rider", SelfHealAndContest);
            runner.Run("mounted shield ledger: release paths remove only this feature's values, and a failed removal is retried", ReleasePaths);
            runner.Run("kmc publication: Mounted Combat, Mounted Archery, Mounted Shield once each, in order, after foreign entries", ThreeFeatPublication);
            runner.Run("kmc publication: losing Mounted Shield from a required list withdraws only Mounted Shield", ShieldWithdrawalIsolated);
            runner.Run("kmc publication: losing Mounted Archery from a required list withdraws only Mounted Archery", ArcheryWithdrawalIsolated);
            runner.Run("kmc publication: withdrawals keep watching the other feat, remove in-place reintroductions and restore exactly", WithdrawalLifecycle);
            runner.Run("kmc publication: a feat that did not register is never offered and the other optional feat is unaffected", PartialRegistration);
            runner.Run("mounted shield contract: the donor contract diagnostics name Mounted Shield; Mounted Archery's text is unchanged", ContractDiagnostics);
            runner.Run("mounted shield ledger: one rider is reconciled alone at the mount's AC read or its equipment change (review finding 2)", ReconcileOneRider);
            runner.Run("kmc publication: the per-frame full refresh withdraws an in-place required edit before any selection (review finding 3)", InPlaceRequiredEditWithdrawsWithinAFrame);
            runner.Run("kmc publication: disable removes only KMC's entries from a foreign-changed list; re-enable republishes them after the foreign entries (review findings PN1-F4, PN2-F5)", DisableStripsForeignChangedLists);
            runner.Run("kmc publication: a copied second reference to a KMC feat is collapsed to one; foreign entries untouched (review finding PN2-F6)", DuplicateKmcReferencesCollapse);
            runner.Run("kmc publication: a competing Mounted Shield in any publication target is found, fighter-only included (review finding 5)", CompetingFeatInAnyTarget);
            runner.Run("kmc publication: a terminal stop suspends only Mounted Shield's offers, reversibly, and never withdraws it (review finding PN4-F3)", SuspensionIsReversible);
        }

        private static MountedShieldFacts Eligible(int baseBonus, bool focus = true, bool greater = false) => new MountedShieldFacts
        {
            Available = true, MountedPair = true, OwnsFeat = true, EffectiveShield = true, ShieldBaseBonus = baseBonus, ShieldFocus = focus, GreaterShieldFocus = greater
        };

        private static void AmountByShieldType()
        {
            // Native 2.1.7b base bonuses by type: buckler 1, light 1, heavy 2, tower 4 (read from BlueprintArmorType by the
            // adapter; scripts/Test-MountedShieldNativeContract.ps1 reads them from the installed shields).
            TestRunner.Equal(2, MountedShieldPolicy.Amount(Eligible(1)), "Buckler with Shield Focus.");
            TestRunner.Equal(2, MountedShieldPolicy.Amount(Eligible(1)), "Light shield with Shield Focus.");
            TestRunner.Equal(3, MountedShieldPolicy.Amount(Eligible(2)), "Heavy shield with Shield Focus.");
            TestRunner.Equal(5, MountedShieldPolicy.Amount(Eligible(4)), "Tower shield with Shield Focus.");
            TestRunner.Equal(4, MountedShieldPolicy.Amount(Eligible(2, true, true)), "Heavy shield with both focus feats (the mission's Greater Shield Focus ruling).");
            TestRunner.Equal(2, MountedShieldPolicy.Amount(Eligible(2, false)), "Without Shield Focus only the base counts.");
            TestRunner.Equal(3, MountedShieldPolicy.Amount(Eligible(2, false, true)), "Each focus feat counts once, on its own.");
            // Enhancement is not an input: a heavy shield +5 has the same base as a mundane one, so it still supplies +3.
            TestRunner.Equal(MountedShieldPolicy.Amount(Eligible(2)), MountedShieldPolicy.Amount(Eligible(2)), "Equal base, equal amount.");
            TestRunner.Equal(0, MountedShieldPolicy.Amount(new MountedShieldFacts { Available = true, MountedPair = true, OwnsFeat = true, EffectiveShield = true, ShieldBaseBonus = -3 }),
                "A negative base never subtracts.");
            var threw = false;
            try { MountedShieldPolicy.Amount(null); } catch (ArgumentNullException) { threw = true; }
            TestRunner.True(threw, "Facts are required.");
            TestRunner.True(MountedShieldPolicy.Describe(Eligible(2, true, true)).EndsWith("amount=4", StringComparison.Ordinal), "The description names the amount.");
        }

        private static void AmountGates()
        {
            var noShield = Eligible(2, true, true); noShield.EffectiveShield = false;
            TestRunner.Equal(0, MountedShieldPolicy.Amount(noShield), "No effective shield: zero, focus feats included.");
            var noFeat = Eligible(2); noFeat.OwnsFeat = false;
            TestRunner.Equal(0, MountedShieldPolicy.Amount(noFeat), "Without the feat (or with its fact turned off): zero.");
            var notMounted = Eligible(2); notMounted.MountedPair = false;
            TestRunner.Equal(0, MountedShieldPolicy.Amount(notMounted), "Not the exact mounted pair: zero.");
            var withdrawn = Eligible(2); withdrawn.Available = false;
            TestRunner.Equal(0, MountedShieldPolicy.Amount(withdrawn), "Withdrawn or unregistered: zero.");
        }

        // A thin stand-in for a mount's native AC: the values written to it, in order. Each value is a distinct object, as
        // native ModifiableValue.Modifier instances are.
        private sealed class Value { internal object Owner; internal int Amount; }

        private sealed class Ac
        {
            internal readonly Dictionary<object, List<Value>> Values = new Dictionary<object, List<Value>>();
            internal int Applies, Removes;
            internal bool FailRemoval;
            internal List<Value> On(object mount) => Values.TryGetValue(mount, out var list) ? list : (Values[mount] = new List<Value>());
            internal int Total(object mount) => On(mount).Sum(value => value.Amount);

            internal MountedShieldContributionLedger<object, Value> Ledger() => new MountedShieldContributionLedger<object, Value>(
                (rider, mount, amount) => { Applies++; var value = new Value { Owner = rider, Amount = amount }; On(mount).Add(value); return value; },
                (mount, value) => On(mount).Contains(value),
                (mount, value) => { if (FailRemoval) throw new InvalidOperationException("native removal failed"); Removes++; On(mount).Remove(value); });
        }

        private static MountedShieldDesire<object> Want(object rider, object mount, int amount) => new MountedShieldDesire<object>(rider, mount, amount);

        private static void TwoPairIsolation()
        {
            var ac = new Ac();
            var ledger = ac.Ledger();
            object riderA = new object(), horseA = new object(), riderB = new object(), horseB = new object();
            TestRunner.Equal(2, ledger.Reconcile(new[] { Want(riderA, horseA, 3), Want(riderB, horseB, 5) }, true), "Two writes, one per pair.");
            TestRunner.Equal(3, ac.Total(horseA), "Mount A carries only rider A's heavy-shield amount.");
            TestRunner.Equal(5, ac.Total(horseB), "Mount B carries only rider B's tower-shield amount.");
            TestRunner.True(ac.On(horseA).All(value => value.Owner == riderA) && ac.On(horseB).All(value => value.Owner == riderB), "No cross-pair value.");
            // Rider B's shield changes; mount A is not written.
            var removes = ac.Removes;
            TestRunner.Equal(2, ledger.Reconcile(new[] { Want(riderA, horseA, 3), Want(riderB, horseB, 2) }, true), "One removal and one write for rider B only.");
            TestRunner.True(ac.Total(horseA) == 3 && ac.On(horseA).Count == 1 && ac.Total(horseB) == 2 && ac.On(horseB).Count == 1 && ac.Removes == removes + 1,
                "Mount A untouched; mount B holds exactly the new amount.");
        }

        private static void IdempotentReconcile()
        {
            var ac = new Ac();
            var ledger = ac.Ledger();
            object rider = new object(), horse = new object();
            ledger.Reconcile(new[] { Want(rider, horse, 3) }, true);
            for (var frame = 0; frame < 100; frame++)
                TestRunner.Equal(0, ledger.Reconcile(new[] { Want(rider, horse, 3) }, true), "An unchanged pair costs no native write (frame " + frame + ").");
            TestRunner.True(ac.Applies == 1 && ac.Removes == 0 && ac.On(horse).Count == 1 && ledger.Count == 1, "Exactly one value after repeated reconciliation.");
        }

        private static void ChangeSwapDismount()
        {
            var ac = new Ac();
            var ledger = ac.Ledger();
            object rider = new object(), first = new object(), second = new object();
            ledger.Reconcile(new[] { Want(rider, first, 3) }, true);
            // Greater Shield Focus acquired: +3 becomes +4 without remounting.
            ledger.Reconcile(new[] { Want(rider, first, 4) }, true);
            TestRunner.True(ac.On(first).Count == 1 && ac.Total(first) == 4, "A changed amount replaces the value; it never stacks.");
            // Mount swap: the old mount is clean, the new one has the current amount once.
            ledger.Reconcile(new[] { Want(rider, second, 4) }, true);
            TestRunner.True(ac.On(first).Count == 0 && ac.On(second).Count == 1 && ac.Total(second) == 4, "The old mount is clean; the new mount has one value.");
            AssertRecord(ledger, rider, second, 4);
            // Shield unequipped, or dismounted: the pair no longer wants anything.
            ledger.Reconcile(new MountedShieldDesire<object>[0], true);
            TestRunner.True(ac.On(second).Count == 0 && ledger.Count == 0, "Nothing remains after the desire ends.");
            object foundMount; int foundAmount;
            TestRunner.True(!ledger.TryGet(rider, out foundMount, out foundAmount) && foundMount == null && foundAmount == 0, "The rider has no record.");
        }

        private static void AssertRecord(MountedShieldContributionLedger<object, Value> ledger, object rider, object mount, int amount)
        {
            object foundMount; int foundAmount;
            TestRunner.True(ledger.TryGet(rider, out foundMount, out foundAmount) && ReferenceEquals(foundMount, mount) && foundAmount == amount, "The record names the exact mount and amount.");
        }

        private static void RemovalOnlyPass()
        {
            var ac = new Ac();
            var ledger = ac.Ledger();
            object rider = new object(), horse = new object(), other = new object(), otherHorse = new object();
            ledger.Reconcile(new[] { Want(rider, horse, 3) }, true);
            // A save, area transition or restoration suspends the ordinary update: the rider dismounted meanwhile, and another
            // pair became eligible.
            TestRunner.Equal(1, ledger.Reconcile(new[] { Want(other, otherHorse, 2) }, false), "Only the stale value is removed.");
            TestRunner.True(ac.On(horse).Count == 0 && ac.On(otherHorse).Count == 0 && ledger.Count == 0, "Nothing new is written while the gates are closed.");
            // A changed amount under closed gates: the old value goes and the new one waits for the ordinary pass.
            ledger.Reconcile(new[] { Want(rider, horse, 3) }, true);
            ledger.Reconcile(new[] { Want(rider, horse, 4) }, false);
            TestRunner.True(ac.On(horse).Count == 0, "A stale amount is never kept while the gates are closed.");
            ledger.Reconcile(new[] { Want(rider, horse, 4) }, true);
            TestRunner.True(ac.On(horse).Count == 1 && ac.Total(horse) == 4, "The ordinary pass writes the current amount once.");
        }

        private static void SelfHealAndContest()
        {
            var ac = new Ac();
            var ledger = ac.Ledger();
            object rider = new object(), horse = new object();
            ledger.Reconcile(new[] { Want(rider, horse, 3) }, true);
            ac.On(horse).Clear(); // for example the native load cleanup or another system removed it
            TestRunner.Equal(2, ledger.Reconcile(new[] { Want(rider, horse, 3) }, true), "The lost record is dropped and the value written again.");
            TestRunner.True(ac.On(horse).Count == 1, "Exactly one value after the repair.");

            var contested = new Ac();
            var contestedLedger = contested.Ledger();
            object riderA = new object(), riderB = new object(), shared = new object();
            contestedLedger.Reconcile(new[] { Want(riderA, shared, 3), Want(riderB, shared, 5) }, true);
            TestRunner.True(contested.On(shared).Count == 0 && contestedLedger.RefusedDesires == 2, "A mount claimed by two riders receives neither value.");
            contestedLedger.Reconcile(new[] { Want(riderA, shared, 3), Want(riderA, new object(), 3), Want(riderA, riderA, 2), Want(null, shared, 1), Want(riderB, shared, 0) }, true);
            TestRunner.True(contested.On(shared).Count == 1 && contestedLedger.RefusedDesires == 4, "One value per rider; self, null and zero desires are refused.");
        }

        private static void ReleasePaths()
        {
            var ac = new Ac();
            var ledger = ac.Ledger();
            object riderA = new object(), horseA = new object(), riderB = new object(), horseB = new object();
            var foreignShieldSpell = new Value { Owner = "Shield spell", Amount = 4 };
            ac.On(horseA).Add(foreignShieldSpell);
            ledger.Reconcile(new[] { Want(riderA, horseA, 3), Want(riderB, horseB, 5) }, true);
            TestRunner.True(ledger.Release(riderA) && !ledger.Release(riderA), "The rider's fact turned off: released once.");
            TestRunner.True(ac.On(horseA).Count == 1 && ReferenceEquals(ac.On(horseA)[0], foreignShieldSpell), "Another source's shield bonus on the mount stays.");
            TestRunner.True(ledger.ReleaseMount(horseB) && !ledger.ReleaseMount(horseB) && ac.On(horseB).Count == 0, "Dismount releases by mount when the rider is gone.");
            ledger.Reconcile(new[] { Want(riderA, horseA, 3), Want(riderB, horseB, 5) }, true);
            ac.FailRemoval = true;
            var threw = false;
            try { ledger.ReleaseAll(); } catch (InvalidOperationException) { threw = true; }
            TestRunner.True(threw && ledger.Count == 2, "A failed native removal is reported and its record kept for the retry.");
            ac.FailRemoval = false;
            TestRunner.Equal(2, ledger.ReleaseAll(), "The retry removes both.");
            TestRunner.True(ledger.Count == 0 && ac.On(horseB).Count == 0 && ac.On(horseA).Count == 1 && ReferenceEquals(ac.On(horseA)[0], foreignShieldSpell),
                "Disable leaves only the foreign value.");
        }

        private sealed class Lists
        {
            internal readonly object Power = new object(), Dodge = new object(), MountedCombat = new object(), MountedArchery = new object(), MountedShield = new object();
            internal readonly Dictionary<string, object[]> Arrays = new Dictionary<string, object[]>();
            internal readonly Dictionary<string, object[]> Originals = new Dictionary<string, object[]>();
            internal readonly KmcCombatFeatPublicationSet<string, object> Set;

            internal Lists(bool archery = true, bool shield = true)
            {
                Arrays["ordinary"] = new[] { Power, Dodge };
                Arrays["fighter"] = new[] { Power, Dodge, new object() };
                Arrays["extra"] = new[] { new object(), Power, Dodge };
                foreach (var key in Arrays.Keys) Originals[key] = Arrays[key];
                Set = new KmcCombatFeatPublicationSet<string, object>(new[] { "ordinary", "fighter", "extra" }, new[] { "ordinary", "fighter" },
                    MountedCombat, archery ? MountedArchery : null, shield ? MountedShield : null, key => Arrays[key], (key, value) => Arrays[key] = value);
            }

            internal bool Has(string key, object item) => Array.IndexOf(Arrays[key], item) >= 0;
            internal int Count(string key, object item) => Arrays[key].Count(entry => ReferenceEquals(entry, item));

            // The invariant: a list offers an optional feat only while its rule is on, and an active rule is obtainable from
            // both required lists. Checked for each feat on its own.
            internal void AssertCoherent(string when)
            {
                foreach (var feat in new[] { KmcOptionalCombatFeat.MountedArchery, KmcOptionalCombatFeat.MountedShield })
                {
                    var item = feat == KmcOptionalCombatFeat.MountedArchery ? MountedArchery : MountedShield;
                    var available = Set.Available(feat);
                    foreach (var key in Arrays.Keys)
                        if (Has(key, item)) TestRunner.True(available, when + ": " + key + " offers " + feat + " only while its rule is on.");
                    if (available) TestRunner.True(Has("ordinary", item) && Has("fighter", item), when + ": an active " + feat + " is obtainable from both required lists.");
                }
            }
        }

        private static void ThreeFeatPublication()
        {
            var lists = new Lists();
            var items = KmcCombatFeatPublication.Items(lists.MountedCombat, lists.MountedArchery, lists.MountedShield);
            TestRunner.True(items.Length == 3 && items[0] == lists.MountedCombat && items[1] == lists.MountedArchery && items[2] == lists.MountedShield, "Fixed order.");
            var withoutArchery = KmcCombatFeatPublication.Items(lists.MountedCombat, null, lists.MountedShield);
            TestRunner.True(withoutArchery.Length == 2 && withoutArchery[1] == lists.MountedShield, "Mounted Shield follows Mounted Combat when Mounted Archery is absent.");
            var threw = false;
            try { KmcCombatFeatPublication.Items(lists.MountedCombat, lists.MountedShield, lists.MountedShield); } catch (ArgumentException) { threw = true; }
            TestRunner.True(threw, "A feature is not appended twice.");

            TestRunner.True(lists.Set.Apply(true, key => key) == null, "Publication succeeds.");
            foreach (var key in new[] { "ordinary", "fighter", "extra" })
            {
                var now = lists.Arrays[key];
                var original = lists.Originals[key];
                TestRunner.True(now.Length == original.Length + 3 && now.Take(original.Length).SequenceEqual(original) &&
                    now[original.Length] == lists.MountedCombat && now[original.Length + 1] == lists.MountedArchery && now[original.Length + 2] == lists.MountedShield,
                    key + ": foreign entries keep their order; the three feats follow once each.");
                TestRunner.True(lists.Set.Offers(key, KmcOptionalCombatFeat.MountedShield) && lists.Set.Offers(key), key + " offers both optional feats.");
            }
            TestRunner.True(lists.Set.MountedShieldAvailable() && lists.Set.MountedArcheryAvailable(), "Both rules are on.");
            lists.AssertCoherent("published");
            // Another mod appends to and reorders a required list without removing either feat: nothing is withdrawn or written.
            var appended = lists.Arrays["ordinary"].Concat(new[] { new object() }).ToArray();
            lists.Arrays["ordinary"] = appended;
            Array.Reverse(lists.Arrays["fighter"]);
            TestRunner.True(!lists.Set.Refresh(false) && lists.Set.MountedShieldAvailable() && lists.Set.MountedArcheryAvailable() &&
                ReferenceEquals(lists.Arrays["ordinary"], appended), "Foreign appends and reorders keep both rules and are never rewritten.");
            lists.AssertCoherent("foreign changes");
        }

        private static void ShieldWithdrawalIsolated()
        {
            var lists = new Lists();
            lists.Set.Apply(true, key => key);
            var foreign = lists.Arrays["fighter"].Where(item => item != lists.MountedShield).Concat(new[] { new object() }).ToArray();
            lists.Arrays["fighter"] = foreign;
            TestRunner.True(lists.Set.Refresh(false), "The per-frame refresh withdraws Mounted Shield on the replaced fighter list.");
            TestRunner.True(lists.Set.MountedShieldWithdrawn && lists.Set.MountedShieldWithdrawalReason == "fighter" && !lists.Set.MountedArcheryWithdrawn && lists.Set.WithdrawalReason == null,
                "Only Mounted Shield is withdrawn, naming the list.");
            TestRunner.True(!lists.Set.MountedShieldAvailable() && lists.Set.MountedArcheryAvailable(), "Mounted Shield's rule is off; Mounted Archery's stays on.");
            TestRunner.True(lists.Arrays.Keys.All(key => !lists.Has(key, lists.MountedShield)), "No list offers Mounted Shield.");
            TestRunner.True(lists.Arrays.Keys.All(key => lists.Count(key, lists.MountedArchery) == 1 && lists.Count(key, lists.MountedCombat) == 1),
                "Mounted Combat and Mounted Archery stay offered once everywhere.");
            TestRunner.True(ReferenceEquals(lists.Arrays["fighter"], foreign), "The foreign list is never rewritten.");
            lists.AssertCoherent("shield withdrawn");
            TestRunner.True(!lists.Set.Refresh(true), "Withdrawal happens once.");
        }

        private static void ArcheryWithdrawalIsolated()
        {
            var lists = new Lists();
            lists.Set.Apply(true, key => key);
            var ordinary = lists.Arrays["ordinary"];
            ordinary[Array.IndexOf(ordinary, lists.MountedArchery)] = new object(); // in place: only the full comparison sees it
            TestRunner.True(!lists.Set.Refresh(false), "The cheap check sees the same reference.");
            TestRunner.True(!lists.Set.MountedArcheryAvailable() && lists.Set.MountedArcheryWithdrawn && lists.Set.WithdrawalReason == "ordinary", "The full comparison withdraws Mounted Archery.");
            TestRunner.True(lists.Set.MountedShieldAvailable() && !lists.Set.MountedShieldWithdrawn, "Mounted Shield's rule stays on.");
            TestRunner.True(lists.Arrays.Keys.All(key => !lists.Has(key, lists.MountedArchery) && lists.Count(key, lists.MountedShield) == 1),
                "Mounted Archery is gone everywhere; Mounted Shield stays offered once everywhere.");
            lists.AssertCoherent("archery withdrawn");
        }

        private static void WithdrawalLifecycle()
        {
            var lists = new Lists();
            lists.Set.Apply(true, key => key);
            lists.Arrays["extra"] = lists.Arrays["extra"].Where(item => item != lists.MountedShield).ToArray();
            TestRunner.True(lists.Set.MountedShieldAvailable(), "A non-required list losing Mounted Shield keeps the rule on.");
            lists.Arrays["ordinary"] = lists.Arrays["ordinary"].Where(item => item != lists.MountedShield).ToArray();
            TestRunner.True(lists.Set.Refresh(false) && lists.Set.MountedShieldWithdrawn, "The ordinary loss withdraws Mounted Shield.");
            // While Mounted Shield is withdrawn, the per-frame refresh removes it from every list on every call, and that
            // removal must not hide a replaced required list from Mounted Archery's own cheap check.
            var fighter = lists.Arrays["fighter"];
            var stale = fighter.Concat(new[] { lists.MountedShield }).ToArray();
            lists.Arrays["fighter"] = stale.Where(item => item != lists.MountedArchery).ToArray();
            TestRunner.True(lists.Set.Refresh(false), "Mounted Archery's loss on the replaced fighter list is still seen.");
            TestRunner.True(lists.Set.MountedArcheryWithdrawn && lists.Set.WithdrawalReason == "fighter" && lists.Set.MountedShieldWithdrawalReason == "ordinary",
                "Each feat records its own reason.");
            TestRunner.True(lists.Arrays.Keys.All(key => !lists.Has(key, lists.MountedShield) && !lists.Has(key, lists.MountedArchery)), "Neither withdrawn feat remains.");
            // An in-place reintroduction while withdrawn is removed by the per-frame refresh.
            var extra = lists.Arrays["extra"];
            extra[0] = lists.MountedShield;
            TestRunner.True(!lists.Set.Refresh(false) && !lists.Has("extra", lists.MountedShield), "The in-place reintroduction is removed.");
            lists.AssertCoherent("both withdrawn");

            // Disable restores the exact originals when only KMC changed a list; withdrawal is not a disable.
            var clean = new Lists();
            clean.Set.Apply(true, key => key);
            clean.Arrays["fighter"] = clean.Arrays["fighter"].Where(item => item != clean.MountedShield).ToArray();
            TestRunner.True(!clean.Set.MountedShieldAvailable(), "Withdrawn.");
            var changedFighter = clean.Arrays["fighter"];
            TestRunner.True(clean.Set.Apply(false, key => key) == null, "Every list holds a KMC value, so disable restores them all.");
            foreach (var key in new[] { "ordinary", "fighter", "extra" })
                TestRunner.True(clean.Arrays[key].SequenceEqual(clean.Originals[key]), key + " is restored to its exact original.");
            TestRunner.True(changedFighter.Length == clean.Originals["fighter"].Length + 2, "The withdrawn value was Mounted Combat plus Mounted Archery.");
            TestRunner.True(clean.Set.Apply(true, key => key) == null && clean.Arrays.Keys.All(key => clean.Has(key, clean.MountedArchery) && !clean.Has(key, clean.MountedShield)),
                "Re-enabling after the withdrawal publishes Mounted Combat and Mounted Archery only.");
            clean.AssertCoherent("re-enabled after withdrawal");
        }

        private static void SuspensionIsReversible()
        {
            var lists = new Lists();
            lists.Set.Apply(true, key => key);
            var foreign = lists.Arrays["extra"].Concat(new[] { new object() }).ToArray();
            lists.Arrays["extra"] = foreign;
            lists.Set.Suspend(KmcOptionalCombatFeat.MountedShield);
            TestRunner.True(lists.Set.Suspended(KmcOptionalCombatFeat.MountedShield) && !lists.Set.MountedShieldAvailable() && !lists.Set.MountedShieldWithdrawn,
                "Suspended: not available, not withdrawn.");
            TestRunner.True(lists.Arrays.Keys.All(key => !lists.Has(key, lists.MountedShield) && lists.Count(key, lists.MountedCombat) == 1 && lists.Count(key, lists.MountedArchery) == 1),
                "Mounted Shield leaves every list (the foreign-changed one included); Mounted Combat and Mounted Archery stay once.");
            TestRunner.True(lists.Arrays["extra"].Contains(foreign[foreign.Length - 1]), "The foreign entry stays.");
            TestRunner.True(!lists.Set.Refresh(true) && !lists.Set.MountedShieldWithdrawn && lists.Set.MountedArcheryAvailable(),
                "Its absence from the required lists does not withdraw it; Mounted Archery's rule stays on.");
            var copied = lists.Arrays["fighter"].Concat(new[] { lists.MountedShield }).ToArray();
            lists.Arrays["fighter"] = copied;
            lists.Set.Refresh(false);
            TestRunner.True(!lists.Has("fighter", lists.MountedShield), "A reference another mod copies back is removed again while suspended.");
            // A refused toggle's re-application keeps the suspension.
            TestRunner.True(lists.Set.Apply(true, key => key) != null, "The re-application reports the foreign-changed list.");
            TestRunner.True(lists.Arrays.Keys.All(key => !lists.Has(key, lists.MountedShield)), "Re-publishing while suspended does not offer Mounted Shield.");
            lists.Arrays["extra"] = lists.Arrays["extra"].Take(lists.Arrays["extra"].Length - 1).ToArray();
            var resumed = lists.Set.Resume(KmcOptionalCombatFeat.MountedShield, key => key);
            TestRunner.True(resumed == null && !lists.Set.Suspended(KmcOptionalCombatFeat.MountedShield), "An explicit enable resumes it: " + resumed);
            TestRunner.True(lists.Set.MountedShieldAvailable() && lists.Arrays.Keys.All(key => lists.Count(key, lists.MountedShield) == 1),
                "Mounted Shield is offered once in every list and available again.");
            lists.AssertCoherent("resumed");
            TestRunner.True(lists.Set.Resume(KmcOptionalCombatFeat.MountedShield, key => key) == null, "Resuming an unsuspended feat changes nothing.");
        }

        private static void PartialRegistration()
        {
            var noShield = new Lists(true, false);
            TestRunner.True(noShield.Set.Apply(true, key => key) == null && !noShield.Set.HasMountedShield, "Published without Mounted Shield.");
            TestRunner.True(noShield.Arrays.Keys.All(key => noShield.Has(key, noShield.MountedArchery) && !noShield.Has(key, noShield.MountedShield)) &&
                !noShield.Set.MountedShieldAvailable() && noShield.Set.MountedArcheryAvailable(), "A Mounted Shield fault never costs Mounted Archery.");
            noShield.AssertCoherent("no Mounted Shield");

            var noArchery = new Lists(false, true);
            TestRunner.True(noArchery.Set.Apply(true, key => key) == null, "Published without Mounted Archery.");
            TestRunner.True(noArchery.Arrays.Keys.All(key => noArchery.Has(key, noArchery.MountedShield) && !noArchery.Has(key, noArchery.MountedArchery)) &&
                noArchery.Set.MountedShieldAvailable() && !noArchery.Set.MountedArcheryAvailable(), "A Mounted Archery fault never costs Mounted Shield.");
            noArchery.AssertCoherent("no Mounted Archery");
            TestRunner.True(noArchery.Set.Apply(false, key => key) == null && noArchery.Arrays.Keys.All(key => noArchery.Arrays[key].SequenceEqual(noArchery.Originals[key])),
                "Disable restores the originals.");

            var neither = new Lists(false, false);
            TestRunner.True(neither.Set.Apply(true, key => key) == null && neither.Arrays.Keys.All(key => neither.Count(key, neither.MountedCombat) == 1) &&
                !neither.Set.MountedShieldAvailable() && !neither.Set.MountedArcheryAvailable(), "Mounted Combat alone.");
        }

        private static void ContractDiagnostics()
        {
            var donor = new KmcFeatContractFacts { Groups = new[] { "CombatFeat", "Feat" }, Ranks = 1, IsClassFeature = true, HasIcon = true };
            var built = new KmcFeatContractFacts { Groups = new[] { "CombatFeat", "Feat" }, Ranks = 1, IsClassFeature = true, HasIcon = true, IconIsKmc = true };
            TestRunner.Equal<string>(null, MountedShieldFeatContract.Violation(built, donor), "The stock-built feat holds the contract.");
            var forced = new KmcFeatContractFacts { Groups = new[] { "CombatFeat", "Feat" }, Ranks = 1, IsClassFeature = false, HasIcon = true, IconIsKmc = false };
            var violation = MountedShieldFeatContract.Violation(forced, donor);
            TestRunner.True(violation != null && violation.StartsWith(MountedShieldFeatContract.FailurePrefix +
                ": IsClassFeature=False (expected the stock donor's True); icon is not KMC's Mounted Shield icon. Feature ", StringComparison.Ordinal),
                "Forcing IsClassFeature=false (the 0.2.9 trap) and a substituted icon are both named: " + violation);
            var absent = new KmcFeatContractFacts { Groups = new[] { "Feat" }, Ranks = 2, IsClassFeature = true };
            TestRunner.True(MountedShieldFeatContract.Violation(absent, donor).StartsWith(MountedShieldFeatContract.FailurePrefix +
                ": groups lack CombatFeat; ranks=2 (expected 1); icon absent (expected KMC's Mounted Shield icon). Feature ", StringComparison.Ordinal), "Missing group, rank and icon.");
            TestRunner.True(MountedArcheryFeatContract.Violation(forced, donor).StartsWith(MountedArcheryFeatContract.FailurePrefix +
                ": IsClassFeature=False (expected the stock donor's True); icon is not KMC's Mounted Archery icon. Feature ", StringComparison.Ordinal),
                "Mounted Archery's diagnostic is unchanged.");
        }

        private static void ReconcileOneRider()
        {
            var ac = new Ac();
            var ledger = ac.Ledger();
            object rider = new object(), horse = new object(), other = new object(), otherHorse = new object();
            ledger.Reconcile(new[] { Want(other, otherHorse, 5) }, true);
            // The rider equips a heavy shield and is attacked before the next frame: the AC read reconciles it at once.
            TestRunner.Equal(1, ledger.ReconcileRider(rider, Want(rider, horse, 3), true), "The new value is written before the AC is read.");
            TestRunner.Equal(0, ledger.ReconcileRider(rider, Want(rider, horse, 3), true), "Unchanged: no write.");
            TestRunner.Equal(2, ledger.ReconcileRider(rider, Want(rider, horse, 4), true), "A changed amount is replaced, never stacked.");
            TestRunner.True(ac.On(horse).Count == 1 && ac.Total(horse) == 4, "One current value.");
            // The shield is unequipped and the mount is attacked before the next frame: the stale value is gone first.
            TestRunner.Equal(1, ledger.ReconcileRider(rider, null, true), "Removed.");
            TestRunner.True(ac.On(horse).Count == 0 && ac.On(otherHorse).Count == 1 && ac.Total(otherHorse) == 5, "The other pair is untouched.");
            // Closed gates: a stale value still goes, nothing new is written.
            ledger.ReconcileRider(rider, Want(rider, horse, 3), true);
            TestRunner.True(ledger.ReconcileRider(rider, Want(rider, horse, 2), false) == 1 && ac.On(horse).Count == 0, "Removal only while the gates are closed.");
            // A mount another rider's record holds is never written, and a malformed desire is treated as none.
            TestRunner.Equal(0, ledger.ReconcileRider(rider, Want(rider, otherHorse, 3), true), "No second value on another rider's mount.");
            TestRunner.True(ac.On(otherHorse).Count == 1 && ac.Total(otherHorse) == 5, "The other rider's value stays.");
            TestRunner.Equal(0, ledger.ReconcileRider(rider, Want(other, horse, 3), true), "A desire naming another rider is ignored.");
            TestRunner.Equal(0, ledger.ReconcileRider(null, Want(rider, horse, 3), true), "No rider, nothing.");
        }

        private static void InPlaceRequiredEditWithdrawsWithinAFrame()
        {
            var lists = new Lists();
            lists.Set.Apply(true, key => key);
            var fighter = lists.Arrays["fighter"];
            fighter[Array.IndexOf(fighter, lists.MountedShield)] = new object(); // another mod edits the array in place
            // HorseCompanionBlueprintService refreshes with the full comparison every frame; no availability call is needed.
            TestRunner.True(lists.Set.Refresh(true) && lists.Set.MountedShieldWithdrawn && lists.Set.MountedShieldWithdrawalReason == "fighter",
                "The next frame's refresh withdraws Mounted Shield.");
            TestRunner.True(lists.Arrays.Keys.All(key => !lists.Has(key, lists.MountedShield)), "No list offers it to a level-up afterwards.");
            TestRunner.True(!lists.Set.MountedArcheryWithdrawn && lists.Arrays.Keys.All(key => lists.Has(key, lists.MountedArchery)), "Mounted Archery is unaffected.");
            lists.AssertCoherent("in-place required edit");
        }

        private static void DisableStripsForeignChangedLists()
        {
            var lists = new Lists();
            lists.Set.Apply(true, key => key);
            var foreignEntry = new object();
            var appended = lists.Arrays["ordinary"].Concat(new[] { foreignEntry }).ToArray();
            lists.Arrays["ordinary"] = appended;
            TestRunner.True(lists.Set.Apply(false, key => key) != null, "Disable reports the foreign-changed list.");
            TestRunner.True(lists.Arrays["ordinary"].SequenceEqual(appended.Where(item => item != lists.MountedCombat && item != lists.MountedArchery && item != lists.MountedShield)),
                "The foreign list keeps every foreign entry in order and loses only KMC's three entries.");
            TestRunner.True(lists.Arrays["fighter"].SequenceEqual(lists.Originals["fighter"]) && lists.Arrays["extra"].SequenceEqual(lists.Originals["extra"]),
                "Unchanged lists are restored exactly.");
            TestRunner.True(lists.Arrays.Keys.All(key => !lists.Has(key, lists.MountedCombat) && !lists.Has(key, lists.MountedArchery) && !lists.Has(key, lists.MountedShield)),
                "Nothing KMC publishes stays selectable while the services are off.");
            var stripped = lists.Arrays["ordinary"];
            TestRunner.True(lists.Set.Apply(true, key => key) == null, "Re-enable publishes again: the stripped foreign value became the list's original.");
            TestRunner.True(lists.Arrays["ordinary"].SequenceEqual(stripped.Concat(new[] { lists.MountedCombat, lists.MountedArchery, lists.MountedShield })),
                "The foreign append is kept and KMC's three entries follow it once each.");
            TestRunner.True(!lists.Set.MountedShieldWithdrawn && !lists.Set.MountedArcheryWithdrawn && lists.Set.MountedShieldAvailable() && lists.Set.MountedArcheryAvailable(),
                "Neither mechanic is withdrawn by KMC's own disable and re-enable.");
            lists.AssertCoherent("re-enabled after a foreign change");
            TestRunner.True(lists.Set.Apply(false, key => key) == null && lists.Arrays["ordinary"].SequenceEqual(stripped),
                "Disabling again restores the rebased list exactly, foreign append included.");
            // A further foreign change after the strip is drift again: re-enable refuses it and writes nothing.
            var changedAgain = lists.Arrays["ordinary"].Concat(new[] { new object() }).ToArray();
            lists.Arrays["ordinary"] = changedAgain;
            TestRunner.True(lists.Set.Apply(true, key => key) != null, "A list changed again after the strip is reported.");
            lists.AssertCoherent("re-enabled after a second foreign change");
        }

        private static void DuplicateKmcReferencesCollapse()
        {
            var lists = new Lists();
            lists.Set.Apply(true, key => key);
            var foreignTwice = new object();
            var extra = lists.Arrays["extra"].Concat(new[] { foreignTwice, lists.MountedShield, foreignTwice, lists.MountedCombat }).ToArray();
            lists.Arrays["extra"] = extra;
            var ordinary = lists.Arrays["ordinary"];
            ordinary[0] = lists.MountedShield; // an in-place copy while Mounted Shield is still active
            TestRunner.True(!lists.Set.Refresh(true) && !lists.Set.MountedShieldWithdrawn, "A duplicate is not a loss: nothing is withdrawn.");
            TestRunner.True(lists.Arrays.Keys.All(key => lists.Count(key, lists.MountedShield) == 1 && lists.Count(key, lists.MountedCombat) == 1 && lists.Count(key, lists.MountedArchery) == 1),
                "Each KMC feat is offered once in every list.");
            TestRunner.True(lists.Count("extra", foreignTwice) == 2, "A foreign entry's own duplicate is not KMC's to remove.");
            var expectedExtra = new List<object>();
            foreach (var entry in extra)
            {
                var isKmc = entry == lists.MountedCombat || entry == lists.MountedArchery || entry == lists.MountedShield;
                if (isKmc && expectedExtra.Contains(entry)) continue;
                expectedExtra.Add(entry);
            }
            TestRunner.True(lists.Arrays["extra"].SequenceEqual(expectedExtra), "The first occurrence of each KMC entry and every foreign entry keep their order.");
            lists.AssertCoherent("duplicates collapsed");
        }

        private sealed class Named
        {
            internal string Asset, Display;
        }

        private static void CompetingFeatInAnyTarget()
        {
            var stock = new Named { Asset = "PowerAttackFeature", Display = "Power Attack" };
            var competitor = new Named { Asset = "OtherModMountedShieldFeat", Display = "Mounted Shield" };
            var displayOnly = new Named { Asset = "XYZ_123", Display = "mounted shield" };
            var ordinary = new[] { stock };
            var fighterOnly = new[] { stock, competitor };
            TestRunner.True(ReferenceEquals(KmcCombatFeatCollision.FindCompeting(new[] { ordinary, fighterOnly }, item => item.Asset, item => item.Display, "MountedShield", "Mounted Shield"), competitor),
                "A competitor offered only to fighters is found.");
            TestRunner.True(ReferenceEquals(KmcCombatFeatCollision.FindCompeting(new[] { ordinary, new[] { displayOnly } }, item => item.Asset, item => item.Display, "MountedShield", "Mounted Shield"), displayOnly),
                "A competitor found by its display name alone.");
            TestRunner.True(KmcCombatFeatCollision.FindCompeting(new[] { ordinary, null, new Named[] { null, stock } }, item => item.Asset, item => item.Display, "MountedShield", "Mounted Shield") == null,
                "Unrelated, empty and null entries are not competitors.");
        }
    }
}
