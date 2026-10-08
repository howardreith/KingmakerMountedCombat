using System;
using System.Linq;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class CastingFixtureSlotSnapshotTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("consumed fixture auto-replacement restores exact original potion placement", RestoreReplacement);
            runner.Run("original scrolls retain identity and charges after quick-slot relocation", RestoreTwoOriginals);
            runner.Run("native removal refusal retains original placements for successful retry", RetryRemoval);
            runner.Run("native insertion exception retains original placements for successful retry", RetryInsertion);
            runner.Run("silent native insertion failure cannot claim slot restoration", VerifyInsertion);
            runner.Run("unknown quick-slot occupant fails before altering original equipment", RejectUnknown);
            runner.Run("replacement quick-slot container cannot inherit original snapshot authority", RejectReplacementSlots);
            runner.Run("settled quick-slot restoration is idempotent and does not replay equipment", Idempotent);
            runner.Run("shared inventory auto-fill restores the original item to another actor", RestoreOtherOwner);
            runner.Run("cohort releases owned items before restoring original placement without refunds", RestoreCohort);
            runner.Run("original holding slot outside its owner is refused before mutations", RejectForeignContainer);
        }
        private sealed class Item
        {
            internal int Charges;
            internal Slot Holding;
            internal Item(int charges) { Charges = charges; }
        }
        private sealed class Slot { internal Item Item; internal Slot[] OwnerSlots; }
        private sealed class Fixture
        {
            internal readonly Item Potion = new Item(1), Rod = new Item(2);
            internal readonly Slot[] Slots = { new Slot(), new Slot(), new Slot() };
            internal readonly CastingFixtureSlotSnapshot<Slot, Item> Snapshot;
            internal bool RefuseRemoval, ThrowInsertion, DropInsertion;
            internal int Mutations;
            internal Fixture()
            {
                Insert(Slots[0], Potion); Insert(Slots[2], Rod);
                Snapshot = new CastingFixtureSlotSnapshot<Slot, Item>(Slots, s => s.Item); Mutations = 0;
            }
            internal void AutoReplaceConsumedFixture()
            { Slots[0].Item = null; Slots[1].Item = Potion; Potion.Holding = Slots[1]; }
            internal bool Remove(Slot slot)
            {
                if (RefuseRemoval) return false;
                slot.Item.Holding = null; slot.Item = null; Mutations++; return true;
            }
            internal void Insert(Slot slot, Item item)
            {
                if (ThrowInsertion) throw new InvalidOperationException("Native insert fault");
                if (DropInsertion) return;
                if (slot.Item != null || item.Holding != null) throw new InvalidOperationException("Ambiguous native placement");
                slot.Item = item; item.Holding = slot; Mutations++;
            }
            internal void Restore() { Snapshot.Restore(Remove, Insert); }
            internal void Verify()
            {
                TestRunner.True(Snapshot.Restored && ReferenceEquals(Slots[0].Item, Potion) && Slots[1].Item == null &&
                    ReferenceEquals(Slots[2].Item, Rod), "Original exact quick slots were not restored.");
                TestRunner.Equal(1, Potion.Charges, "Original potion was consumed or refunded.");
                TestRunner.Equal(2, Rod.Charges, "Previously spent rod was refunded or spent again.");
                TestRunner.True(ReferenceEquals(Potion.Holding, Slots[0]) && ReferenceEquals(Rod.Holding, Slots[2]), "Native holding slots disagree.");
            }
        }
        private static void Throws(Action action)
        {
            var caught = false; try { action(); } catch (InvalidOperationException) { caught = true; }
            TestRunner.True(caught, "Incomplete fixture restoration was accepted.");
        }
        private static Slot[] OwnerSlots(int count)
        {
            var slots = Enumerable.Range(0, count).Select(_ => new Slot()).ToArray();
            foreach (var slot in slots) slot.OwnerSlots = slots;
            return slots;
        }
        private static void InsertExact(Slot slot, Item item)
        {
            if (slot.Item != null || item.Holding != null) throw new InvalidOperationException("Native placement is not vacant.");
            slot.Item = item; item.Holding = slot;
        }
        private static bool RemoveExact(Slot slot)
        {
            slot.Item.Holding = null; slot.Item = null; return true;
        }
        private static void RestoreOtherOwner()
        {
            var rider = OwnerSlots(3); var companion = OwnerSlots(2); var original = new Item(1);
            InsertExact(companion[1], original);
            var all = CastingFixtureSlotSnapshot<Slot, Item>.IncludeOriginalOwnerSlots(rider,
                new[] { original.Holding }, s => s.OwnerSlots);
            var snapshot = new CastingFixtureSlotSnapshot<Slot, Item>(all, s => s.Item);
            TestRunner.Equal(5, all.Length, "The shared original owner was omitted or duplicated.");
            // The native consumed-item replacement moves this exact original.
            RemoveExact(companion[1]); InsertExact(rider[1], original);
            snapshot.Restore(RemoveExact, InsertExact);
            TestRunner.True(snapshot.Restored && ReferenceEquals(companion[1].Item, original) &&
                rider.All(s => s.Item == null) && ReferenceEquals(original.Holding, companion[1]),
                "The original item did not return to its exact companion slot.");
            TestRunner.Equal(1, original.Charges, "Original item resources changed during equipment restoration.");
        }
        private static void RestoreCohort()
        {
            var rider = OwnerSlots(3); var companion = OwnerSlots(2);
            var originalPotion = new Item(1); var originalScroll = new Item(1);
            InsertExact(companion[0], originalPotion); InsertExact(companion[1], originalScroll);
            var all = CastingFixtureSlotSnapshot<Slot, Item>.IncludeOriginalOwnerSlots(rider,
                new[] { originalPotion.Holding, originalScroll.Holding }, s => s.OwnerSlots);
            var snapshot = new CastingFixtureSlotSnapshot<Slot, Item>(all, s => s.Item);
            var rod = new Item(3); var potion = new Item(1); var scroll = new Item(1);
            InsertExact(rider[0], rod); InsertExact(rider[1], potion); InsertExact(rider[2], scroll);
            rod.Charges--; RemoveExact(rider[0]); // Native quickened use then early rod release.
            Throws(() => snapshot.Restore(RemoveExact, InsertExact));
            TestRunner.True(ReferenceEquals(rider[1].Item, potion) && ReferenceEquals(rider[2].Item, scroll),
                "Early restoration disturbed still-owned cohort items.");
            potion.Charges--; RemoveExact(rider[1]); RemoveExact(companion[0]); InsertExact(rider[1], originalPotion);
            scroll.Charges--; RemoveExact(rider[2]); RemoveExact(companion[1]); InsertExact(rider[2], originalScroll);
            snapshot.Restore(RemoveExact, InsertExact);
            TestRunner.True(snapshot.Restored && ReferenceEquals(companion[0].Item, originalPotion) &&
                ReferenceEquals(companion[1].Item, originalScroll) && rider.All(s => s.Item == null),
                "The cohort's original placements were not restored after all releases.");
            TestRunner.True(rod.Holding == null && potion.Holding == null && scroll.Holding == null &&
                rod.Charges == 2 && potion.Charges == 0 && scroll.Charges == 0,
                "Consumed fixture items were re-equipped or their charges refunded.");
            TestRunner.True(originalPotion.Charges == 1 && originalScroll.Charges == 1,
                "Original consumables were spent or refunded.");
        }
        private static void RejectForeignContainer()
        {
            var rider = OwnerSlots(3); var companion = OwnerSlots(2); var stale = new Slot { OwnerSlots = companion };
            var original = new Item(1); InsertExact(stale, original);
            Throws(() => CastingFixtureSlotSnapshot<Slot, Item>.IncludeOriginalOwnerSlots(rider,
                new[] { original.Holding }, s => s.OwnerSlots));
            TestRunner.True(ReferenceEquals(stale.Item, original) && rider.All(s => s.Item == null),
                "A stale native owner caused equipment mutation during capture.");
        }
        private static void RestoreReplacement()
        { var f = new Fixture(); f.AutoReplaceConsumedFixture(); f.Restore(); f.Verify(); }
        private static void RestoreTwoOriginals()
        {
            var first = new Item(1); var second = new Item(1);
            var slots = new[] { new Slot { Item = first }, new Slot { Item = second }, new Slot() };
            first.Holding = slots[0]; second.Holding = slots[1];
            var snapshot = new CastingFixtureSlotSnapshot<Slot, Item>(slots, s => s.Item);
            slots[0].Item = second; second.Holding = slots[0]; slots[1].Item = null; slots[2].Item = first; first.Holding = slots[2];
            snapshot.Restore(s => { s.Item.Holding = null; s.Item = null; return true; }, (s, i) => { s.Item = i; i.Holding = s; });
            TestRunner.True(snapshot.Restored && ReferenceEquals(slots[0].Item, first) && ReferenceEquals(slots[1].Item, second) && slots[2].Item == null,
                "Same-blueprint original items were aliased or lost.");
            TestRunner.True(first.Charges == 1 && second.Charges == 1, "Original scroll resources changed.");
        }
        private static void RetryRemoval()
        { var f = new Fixture(); f.AutoReplaceConsumedFixture(); f.RefuseRemoval = true; Throws(f.Restore); TestRunner.True(!f.Snapshot.Restored, "Failed removal lost debt."); f.RefuseRemoval = false; f.Restore(); f.Verify(); }
        private static void RetryInsertion()
        { var f = new Fixture(); f.AutoReplaceConsumedFixture(); f.ThrowInsertion = true; Throws(f.Restore); TestRunner.True(!f.Snapshot.Restored, "Failed insertion lost debt."); f.ThrowInsertion = false; f.Restore(); f.Verify(); }
        private static void VerifyInsertion()
        { var f = new Fixture(); f.AutoReplaceConsumedFixture(); f.DropInsertion = true; Throws(f.Restore); TestRunner.True(!f.Snapshot.Restored, "Unobserved insertion was accepted."); f.DropInsertion = false; f.Restore(); f.Verify(); }
        private static void RejectUnknown()
        { var f = new Fixture(); f.Slots[1].Item = new Item(3); Throws(f.Restore); TestRunner.Equal(0, f.Mutations, "Unknown occupant caused unrelated equipment mutation."); TestRunner.True(ReferenceEquals(f.Slots[0].Item, f.Potion), "Original equipment changed before refusal."); }
        private static void RejectReplacementSlots()
        { var f = new Fixture(); TestRunner.True(!f.Snapshot.ExactSlots(f.Slots.Select(s => new Slot { Item = s.Item })), "Replacement native slots inherited old authority."); TestRunner.True(f.Snapshot.ExactSlots(f.Slots), "Exact original slots were lost."); }
        private static void Idempotent()
        { var f = new Fixture(); f.AutoReplaceConsumedFixture(); f.Restore(); var mutations = f.Mutations; f.Restore(); f.Verify(); TestRunner.Equal(mutations, f.Mutations, "Settled cleanup replayed equipment operations."); }
    }
}