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
        }
        private sealed class Item
        {
            internal readonly int Charges;
            internal Slot Holding;
            internal Item(int charges) { Charges = charges; }
        }
        private sealed class Slot { internal Item Item; }
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