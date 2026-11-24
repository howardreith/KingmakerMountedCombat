using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // The production hand custody the Mounted Shield gameplay driver uses (supplemental review PN6-F2), on a model of native hand
    // slots: removal refused for a non-removable occupant, insertion refused on demand, and a two-handed primary that the slot
    // drops when an off-hand item arrives. Not native evidence: the driver binds the same class to the native HandSlot calls.
    internal static class FixtureHandCustodyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("fixture hand custody: a native removal refusal changes nothing and is reported", RefusedRemoval);
            runner.Run("fixture hand custody: a failed first restoration is reported and a retry restores every original", RetriedRestoration);
            runner.Run("fixture hand custody: a two-handed primary the slot dropped returns after the created shield is released", TwoHandedPrimary);
            runner.Run("fixture hand custody: a persistent reinsertion refusal names the slot and the expected and observed items (closure review PN14-F1)", PersistentRefusalIdentified);
        }

        private sealed class Item { public string Name; public bool NonRemovable; public bool TwoHanded; public Slot Holder; public override string ToString() => Name; }
        private sealed class Slot { public string Name; public bool Primary; public Slot Pair; public Item Occupant; public bool RefuseInsert; }

        private static FixtureHandCustody<Slot, Item> Custody() => new FixtureHandCustody<Slot, Item>(
            slot => slot.Occupant,
            slot => slot.Occupant != null && !slot.Occupant.NonRemovable,
            slot => { if (slot.Occupant != null && !slot.Occupant.NonRemovable) { slot.Occupant.Holder = null; slot.Occupant = null; } },
            (slot, item) => slot.Occupant == null && !slot.RefuseInsert,
            (slot, item) =>
            {
                // A native off-hand insertion drops a two-handed primary into the inventory.
                if (!slot.Primary && slot.Pair.Occupant != null && slot.Pair.Occupant.TwoHanded) { slot.Pair.Occupant.Holder = null; slot.Pair.Occupant = null; }
                slot.Occupant = item; item.Holder = slot;
            },
            item => item.Holder,
            slot => slot.Primary);

        private static Slot[] Hands(Item primary, Item secondary)
        {
            var main = new Slot { Name = "primary", Primary = true, Occupant = primary };
            var off = new Slot { Name = "secondary", Pair = main, Occupant = secondary };
            main.Pair = off;
            if (primary != null) primary.Holder = main;
            if (secondary != null) secondary.Holder = off;
            return new[] { main, off };
        }

        private static void RefusedRemoval()
        {
            var cursed = new Item { Name = "cursed shield", NonRemovable = true };
            var hands = Hands(new Item { Name = "sword" }, cursed);
            var custody = Custody(); custody.Capture(hands);
            TestRunner.True(!custody.Vacate(hands[1]), "A non-removable occupant refuses the vacate.");
            TestRunner.True(ReferenceEquals(hands[1].Occupant, cursed) && ReferenceEquals(cursed.Holder, hands[1]), "Nothing changed: the original keeps its slot.");
            TestRunner.True(!custody.Place(hands[1], new Item { Name = "created" }), "Nothing is placed over it.");
            TestRunner.True(custody.Restore(item => false, item => false).Length == 0, "Restore finds every original in place.");
        }

        private static void RetriedRestoration()
        {
            var sword = new Item { Name = "sword" }; var buckler = new Item { Name = "original buckler" };
            var hands = Hands(sword, buckler);
            var custody = Custody(); custody.Capture(hands);
            TestRunner.True(custody.Vacate(hands[1]) && hands[1].Occupant == null && buckler.Holder == null, "The original off-hand leaves through the native removal.");
            var created = new Item { Name = "created shield" };
            TestRunner.True(custody.Place(hands[1], created), "The created shield is placed and verified.");
            var released = new HashSet<Item>();
            Func<Item, bool> release = item => { if (!custody.Vacate(item.Holder)) return false; released.Add(item); return true; };
            hands[1].RefuseInsert = true;
            var first = custody.Restore(item => item == created, release);
            TestRunner.True(first.Length == 1 && hands[1].Occupant == null && released.Contains(created), "A refused reinsertion is reported, with the created shield already released.");
            hands[1].RefuseInsert = false;
            var second = custody.Restore(item => item == created, release);
            TestRunner.True(second.Length == 0 && ReferenceEquals(hands[1].Occupant, buckler) && ReferenceEquals(hands[0].Occupant, sword), "The retry restores every original.");
        }

        private static void PersistentRefusalIdentified()
        {
            var sword = new Item { Name = "owner-1 longsword #11" };
            var hands = Hands(sword, null);
            var custody = Custody();
            custody.DescribeSlot = slot => "actor owner-1 set 0 " + slot.Name;
            custody.DescribeItem = item => item == null ? "none" : item.Name;
            custody.Capture(hands);
            TestRunner.True(custody.Vacate(hands[0]), "The original weapon leaves its slot.");
            hands[0].RefuseInsert = true;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var errors = custody.Restore(item => false, item => false);
                TestRunner.True(errors.Length == 1 && errors[0].Contains("actor owner-1 set 0 primary") && errors[0].Contains("expected owner-1 longsword #11") &&
                    errors[0].Contains("observed none"), "Every refused restoration names the actor, set, hand and the expected and observed items: " + string.Join(" | ", errors));
            }
        }

        private static void TwoHandedPrimary()
        {
            var greatsword = new Item { Name = "greatsword", TwoHanded = true };
            var hands = Hands(greatsword, null);
            var custody = Custody(); custody.Capture(hands);
            var created = new Item { Name = "created shield" };
            TestRunner.True(custody.Place(hands[1], created) && hands[0].Occupant == null, "The native slot drops the two-handed primary when the shield arrives.");
            var errors = custody.Restore(item => item == created, item => custody.Vacate(item.Holder));
            TestRunner.True(errors.Length == 0 && ReferenceEquals(hands[0].Occupant, greatsword) && hands[1].Occupant == null, "The created shield is released and the greatsword returns to its own slot.");
        }
    }
}
