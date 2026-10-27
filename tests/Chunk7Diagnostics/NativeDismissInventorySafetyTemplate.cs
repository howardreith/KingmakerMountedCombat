using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;

namespace Harmony12
{
    internal sealed class CodeInstruction
    {
        internal OpCode opcode;
        internal object operand;
        internal readonly List<Label> labels = new List<Label>();
        internal CodeInstruction(OpCode opcode, object operand = null) { this.opcode = opcode; this.operand = operand; }
    }
}
namespace KingmakerMountedCombat.Domain
{
    internal enum RelationshipState { Unmounted, Mounted }
}
namespace Kingmaker.EntitySystem.Entities
{
    internal sealed class UnitState { internal bool CanRemoveFromParty = true; }
    internal sealed class UnitDescriptor { internal UnitState State = new UnitState(); }
    internal sealed class UnitEntityData { internal UnitDescriptor Descriptor = new UnitDescriptor(); internal bool IsInGame = true; }
}
namespace Kingmaker
{
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.Items;
    internal sealed class Game { internal static Game Instance = new Game(); internal Player Player; }
    internal sealed class Ref { internal UnitEntityData Value; internal Ref(UnitEntityData value) { Value = value; } }
    internal sealed class Inventory { internal readonly List<ItemEntity> Items = new List<ItemEntity>(); }
    internal sealed class Player
    {
        internal readonly Inventory Inventory = new Inventory();
        internal readonly List<Ref> PartyCharacters = new List<Ref>();
        internal Ref MainCharacter = new Ref(null);
        internal string GameId = "campaign";
    }
}
namespace Kingmaker.Items
{
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.Items.Slots;
    internal sealed class ItemEntity
    {
        internal object Blueprint;
        internal bool IsStackable;
        internal int Count = 1;
        internal Kingmaker.Inventory Collection;
        public ItemSlot HoldingSlot { get; set; }
        public UnitDescriptor Owner => HoldingSlot?.Owner;
    }
}
namespace Kingmaker.Items.Slots
{
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.Items;
    internal sealed class ItemSlot
    {
        internal readonly UnitDescriptor Owner;
        internal ItemEntity Item;
        internal bool Fail, Throw;
        internal static Action OnRemove;
        internal ItemSlot(UnitDescriptor owner, ItemEntity item) { Owner = owner; Item = item; item.HoldingSlot = this; }
        public bool RemoveItem(bool autoMerge = true)
        {
            if (Throw) throw new InvalidOperationException("native slot throw");
            if (Fail) return false;
            var item = Item; Item = null; item.HoldingSlot = null; OnRemove?.Invoke();
            if (autoMerge && item.IsStackable)
            {
                var other = item.Collection.Items.FirstOrDefault(row => row != item &&
                    row.HoldingSlot == null && row.IsStackable && ReferenceEquals(row.Blueprint, item.Blueprint));
                if (other != null) { other.Count += item.Count; item.Collection.Items.Remove(item); }
            }
            return true;
        }
    }
}
namespace KingmakerMountedCombat.Integration
{
    using Kingmaker;
    using Kingmaker.EntitySystem.Entities;
    using Kingmaker.Items;
    using Kingmaker.Items.Slots;
    using KingmakerMountedCombat.Domain;
    internal sealed class PairOwner { internal object Session; internal string CampaignId; }
    internal sealed class GameMountedRelationshipService
    {
        internal RelationshipState State = RelationshipState.Mounted;
        internal UnitEntityData Rider, Mount;
        internal PairOwner PairOwner;
        internal bool Current = true;
        internal GameMountedRelationshipService ResolveActorOwner(UnitEntityData actor, bool activeOnly) =>
            activeOnly && (ReferenceEquals(actor, Rider) || ReferenceEquals(actor, Mount)) ? this : null;
        internal bool IsCurrentPairOwner(PairOwner owner) => Current && ReferenceEquals(owner, PairOwner);
    }
    internal static class NativeDismissInventorySafetyTests
    {
        private static int passes;
        private static GameMountedRelationshipService service;
        private static bool Capture(Player player, UnitEntityData actor) =>
            NativeMountedDismissInventorySafety.AutoMerge(service, player, actor);
        private static void Check(bool condition, string label)
        { if (!condition) throw new Exception(label); passes++; }
        private static ItemEntity Add(Player player, object blueprint, bool stackable, UnitDescriptor owner = null)
        {
            var item = new ItemEntity { Blueprint = blueprint, IsStackable = stackable, Collection = player.Inventory };
            player.Inventory.Items.Add(item);
            if (owner != null) new ItemSlot(owner, item);
            return item;
        }
        private static void NativeLoop(Player player, UnitEntityData actor, bool autoMerge)
        {
            player.PartyCharacters.RemoveAll(row => ReferenceEquals(row.Value, actor));
            actor.IsInGame = false;
            service.Current = false; // lifecycle can retire ownership before item callbacks
            foreach (var item in player.Inventory.Items)
                if (ReferenceEquals(item.Owner, actor.Descriptor) && !item.HoldingSlot.RemoveItem(autoMerge))
                    FailedRemovals++;
        }
        private static int FailedRemovals;
        private sealed class NativeClosure { internal UnitEntityData value; internal NativeClosure() { value = null; } }
        private static List<CodeInstruction> NativeShape()
        {
            return new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Newobj, typeof(NativeClosure).GetConstructor(Type.EmptyTypes)),
                new CodeInstruction(OpCodes.Stloc_0), new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Stfld, typeof(NativeClosure).GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Callvirt, typeof(ItemEntity).GetProperty(nameof(ItemEntity.HoldingSlot)).GetGetMethod()),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Callvirt, typeof(ItemSlot).GetMethod(nameof(ItemSlot.RemoveItem)))
            };
        }
        private static bool Refused(List<CodeInstruction> code, MethodInfo capture)
        {
            var dynamic = new DynamicMethod("dismiss-rewrite-test", typeof(void), Type.EmptyTypes);
            try { NativeMountedDismissInventorySafety.Rewrite(code, dynamic.GetILGenerator(), capture).ToArray(); return false; }
            catch (InvalidOperationException) { return true; }
        }
        internal static int Main()
        {
            var method = typeof(NativeDismissInventorySafetyTests).GetMethod(nameof(Capture),
                BindingFlags.Static | BindingFlags.NonPublic);
            var shape = NativeShape(); var originalLiteral = shape[7];
            var label = new DynamicMethod("label", typeof(void), Type.EmptyTypes).GetILGenerator().DefineLabel();
            originalLiteral.labels.Add(label);
            var dynamic = new DynamicMethod("dismiss-rewrite", typeof(void), Type.EmptyTypes);
            var changed = NativeMountedDismissInventorySafety.Rewrite(shape, dynamic.GetILGenerator(), method).ToArray();
            Check(changed.Length == shape.Count + 4 && ReferenceEquals(changed[11], originalLiteral) &&
                changed[11].opcode == OpCodes.Ldloc && changed[11].labels.Single().Equals(label),
                "Exact literal/labels not preserved at original native call.");
            Check(changed[5].opcode == OpCodes.Ldarg_0 && changed[6].opcode == OpCodes.Ldarg_1 &&
                changed[7].opcode == OpCodes.Call && ReferenceEquals(changed[7].operand, method) &&
                changed[8].opcode == OpCodes.Stloc, "Entry capture is not before native membership changes.");
            Check(ReferenceEquals(changed[12].operand, typeof(ItemSlot).GetMethod(nameof(ItemSlot.RemoveItem))) &&
                changed[12].opcode == OpCodes.Callvirt, "Native RemoveItem(bool) call was changed.");
            shape = NativeShape(); shape[7].opcode = OpCodes.Ldc_I4_0;
            Check(Refused(shape, method), "Changed native literal was accepted.");
            shape = NativeShape(); shape.AddRange(shape.Skip(6).ToArray());
            Check(Refused(shape, method), "Duplicate native removal call was accepted.");
            shape = NativeShape(); shape[6].operand = typeof(ItemEntity).GetProperty(nameof(ItemEntity.Owner)).GetGetMethod();
            Check(Refused(shape, method), "Changed native slot getter was accepted.");
            shape = NativeShape(); shape[4].operand = typeof(NativeClosure).GetField("value", BindingFlags.Instance | BindingFlags.Public);
            Check(Refused(shape, method), "Changed native entry was accepted.");

            var player = new Player(); Game.Instance.Player = player;
            var rider = new UnitEntityData(); var mount = new UnitEntityData();
            player.PartyCharacters.Add(new Ref(rider));
            service = new GameMountedRelationshipService { Rider = rider, Mount = mount,
                PairOwner = new PairOwner { Session = player, CampaignId = player.GameId } };
            Check(!Capture(player, rider), "Current mounted rider did not select no-merge.");
            Check(Capture(player, mount), "Mount actor changed native behavior.");
            Check(Capture(player, new UnitEntityData()), "Foreign actor changed native behavior.");
            service.State = RelationshipState.Unmounted;
            Check(Capture(player, rider), "Unmounted rider changed native behavior.");
            service.State = RelationshipState.Mounted;
            service.PairOwner.Session = new Player();
            Check(Capture(player, rider), "Stale session changed native behavior.");
            service.PairOwner.Session = player; service.PairOwner.CampaignId = "other";
            Check(Capture(player, rider), "Stale campaign changed native behavior.");
            service.PairOwner.CampaignId = player.GameId; service.Current = false;
            Check(Capture(player, rider), "Stale generation changed native behavior.");
            service.Current = true; player.PartyCharacters.Clear();
            Check(Capture(player, rider), "Absent native character changed native behavior.");
            player.PartyCharacters.Add(new Ref(rider)); rider.IsInGame = false;
            Check(Capture(player, rider), "Out-of-game actor changed native behavior.");
            rider.IsInGame = true; player.MainCharacter = new Ref(rider);
            Check(Capture(player, rider), "Main character changed native behavior.");
            player.MainCharacter = new Ref(null);
            Check(NativeMountedDismissInventorySafety.AutoMerge(null, player, rider), "Missing service changed native behavior.");
            Check(Capture(new Player(), rider), "Foreign player changed native behavior.");
            rider.Descriptor.State.CanRemoveFromParty = false;
            Check(Capture(player, rider), "Nonremovable character changed native behavior.");
            rider.Descriptor.State.CanRemoveFromParty = true;
            player.PartyCharacters.Add(new Ref(rider));
            Check(Capture(player, rider), "Duplicate native character changed native behavior.");
            player.PartyCharacters.RemoveAt(1);

            // Matched original behavior: native auto-merge invalidates its own
            // inventory enumerator after the first equipped stackable.
            var originalPlayer = new Player(); Game.Instance.Player = originalPlayer;
            var originalRider = new UnitEntityData();
            originalPlayer.PartyCharacters.Add(new Ref(originalRider));
            service.Rider = originalRider;
            service.PairOwner = new PairOwner { Session = originalPlayer, CampaignId = originalPlayer.GameId };
            service.Current = true;
            var originalPotion = new object();
            Add(originalPlayer, originalPotion, true);
            Add(originalPlayer, originalPotion, true, originalRider.Descriptor);
            bool originalThrows = false;
            try { NativeLoop(originalPlayer, originalRider, true); }
            catch (InvalidOperationException) { originalThrows = true; }
            Check(originalThrows && originalPlayer.Inventory.Items.Count == 1,
                "Original native auto-merge failure was not reproduced.");

            Game.Instance.Player = player; service.Rider = rider;
            service.PairOwner = new PairOwner { Session = player, CampaignId = player.GameId };
            service.Current = true;
            var potion = new object(); var armor = new object();
            var stash = Add(player, potion, true);
            var equipped = Add(player, potion, true, rider.Descriptor);
            var gear = Add(player, armor, false, rider.Descriptor);
            var autoMerge = Capture(player, rider);
            Check(!autoMerge, "Mounted capture refused before native transition.");
            ItemSlot.OnRemove = () => Check(player.PartyCharacters.Count == 0 && !rider.IsInGame,
                "Equipment event occurred before native party/out-of-game transition.");
            NativeLoop(player, rider, autoMerge);
            Check(player.Inventory.Items.Count == 3 && stash.Count == 1 && equipped.Count == 1 &&
                equipped.HoldingSlot == null && gear.HoldingSlot == null,
                "Native loop did not finish without merge or fabricated item count.");
            Check(FailedRemovals == 0, "Native boolean success changed.");
            Check(Capture(player, rider), "Eligibility was recomputed after native retirement.");
            ItemSlot.OnRemove = null;

            player = new Player(); Game.Instance.Player = player; rider = new UnitEntityData();
            player.PartyCharacters.Add(new Ref(rider)); service.Rider = rider;
            service.PairOwner = new PairOwner { Session = player, CampaignId = player.GameId }; service.Current = true;
            var stuck = Add(player, potion, true, rider.Descriptor); stuck.HoldingSlot.Fail = true;
            NativeLoop(player, rider, Capture(player, rider));
            Check(FailedRemovals == 1 && stuck.HoldingSlot != null, "Native false result/logging changed.");

            player = new Player(); Game.Instance.Player = player; rider = new UnitEntityData();
            player.PartyCharacters.Add(new Ref(rider)); service.Rider = rider;
            service.PairOwner = new PairOwner { Session = player, CampaignId = player.GameId }; service.Current = true;
            stuck = Add(player, potion, true, rider.Descriptor); stuck.HoldingSlot.Throw = true;
            bool threw = false;
            try { NativeLoop(player, rider, Capture(player, rider)); }
            catch (InvalidOperationException) { threw = true; }
            Check(threw && stuck.HoldingSlot != null, "Native slot exception was suppressed.");
            Console.WriteLine("NATIVE MOUNTED DISMISS INVENTORY PASS=" + passes + " FAIL=0");
            return 0;
        }
    }
}
