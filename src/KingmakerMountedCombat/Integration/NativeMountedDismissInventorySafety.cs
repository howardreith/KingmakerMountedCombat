using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // The installed Player.DismissCompanion enumerates Inventory.Items. Its
    // RemoveItem(true) merges equipped stackables into other inventory stacks,
    // invalidating that native enumerator. Capture exact mounted eligibility
    // before native party removal; change only that call's auto-merge argument.
    internal static class NativeMountedDismissInventorySafety
    {
        internal static bool AutoMerge(GameMountedRelationshipService service, Player player, UnitEntityData actor)
        {
            if (service == null || player == null || actor?.Descriptor == null ||
                !ReferenceEquals(Game.Instance?.Player, player) ||
                !actor.Descriptor.State.CanRemoveFromParty || !actor.IsInGame ||
                ReferenceEquals(player.MainCharacter.Value, actor) ||
                player.PartyCharacters.Count(reference => ReferenceEquals(reference.Value, actor)) != 1)
                return true;
            var relationship = service.ResolveActorOwner(actor, true);
            if (relationship == null || relationship.State != RelationshipState.Mounted ||
                !ReferenceEquals(relationship.Rider, actor)) return true;
            var owner = relationship.PairOwner;
            return owner == null || !relationship.IsCurrentPairOwner(owner) ||
                !ReferenceEquals(owner.Session, player) || owner.CampaignId != player.GameId;
        }

        internal static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator, MethodInfo captureAutoMerge)
        {
            if (instructions == null || generator == null || captureAutoMerge == null ||
                captureAutoMerge.ReturnType != typeof(bool) ||
                !captureAutoMerge.IsStatic ||
                !captureAutoMerge.GetParameters().Select(parameter => parameter.ParameterType)
                    .SequenceEqual(new[] { typeof(Player), typeof(UnitEntityData) }))
                throw new InvalidOperationException("Mounted dismissal rewrite has no exact capture method.");

            var code = instructions.ToList();
            var holdingSlot = typeof(ItemEntity).GetProperty(nameof(ItemEntity.HoldingSlot))?.GetGetMethod();
            var remove = typeof(ItemSlot).GetMethod(nameof(ItemSlot.RemoveItem), new[] { typeof(bool) });
            if (holdingSlot == null || remove == null || code.Count < 8 ||
                code[0].opcode != OpCodes.Newobj || code[1].opcode != OpCodes.Stloc_0 ||
                code[2].opcode != OpCodes.Ldloc_0 || code[3].opcode != OpCodes.Ldarg_1 ||
                code[4].opcode != OpCodes.Stfld || !(code[4].operand is FieldInfo actorField) ||
                actorField.Name != "value" || actorField.FieldType != typeof(UnitEntityData))
                throw new InvalidOperationException("Mounted dismissal native entry layout changed.");

            var calls = code.Select((instruction, index) => new { instruction, index })
                .Where(row => (row.instruction.opcode == OpCodes.Callvirt || row.instruction.opcode == OpCodes.Call) &&
                    Equals(row.instruction.operand, remove)).Select(row => row.index).ToArray();
            if (calls.Length != 1 || calls[0] < 2 ||
                code[calls[0] - 1].opcode != OpCodes.Ldc_I4_1 ||
                (code[calls[0] - 2].opcode != OpCodes.Callvirt &&
                 code[calls[0] - 2].opcode != OpCodes.Call) ||
                !Equals(code[calls[0] - 2].operand, holdingSlot))
                throw new InvalidOperationException("Mounted dismissal native inventory callsite changed.");

            var autoMerge = generator.DeclareLocal(typeof(bool));
            // Mutate this instruction in place to retain Harmony's labels and
            // exception metadata at the native callsite.
            code[calls[0] - 1].opcode = OpCodes.Ldloc;
            code[calls[0] - 1].operand = autoMerge;
            code.InsertRange(5, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, captureAutoMerge),
                new CodeInstruction(OpCodes.Stloc, autoMerge)
            });
            return code;
        }
    }
}
