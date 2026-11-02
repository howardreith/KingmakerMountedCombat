using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerMountedCombat.Integration
{
    // UnitCommands.Run(cmd, fromQueue, doNotClearQueue) writes Hold exactly once: m_Owner.HoldState = false, reached for a
    // command without AiAction. That single write becomes a call deciding from the command's identity, so a queued
    // command's later Run is covered, no snapshot can overwrite a newer player decision, and no AiAction is forged.
    internal static class NativeRunHoldBoundary
    {
        private static readonly FieldInfo HoldState = AccessTools.Field(typeof(UnitEntityData), nameof(UnitEntityData.HoldState));

        internal static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions, MethodInfo write)
        {
            if (write == null) throw new InvalidOperationException("The native Run Hold write has no replacement.");
            var code = instructions.ToList();
            var writes = code.Select((instruction, index) => new { instruction, index })
                .Where(entry => entry.instruction.opcode == OpCodes.Stfld && Equals(entry.instruction.operand, HoldState))
                .Select(entry => entry.index).ToList();
            if (writes.Count != 1 || writes[0] == 0 || code[writes[0] - 1].opcode != OpCodes.Ldc_I4_0)
                throw new InvalidOperationException("The exact native Run Hold write was not found once.");
            var site = writes[0];
            // The owner is already on the stack; the literal false becomes the three arguments the decision reads.
            code[site - 1].opcode = OpCodes.Ldarg_1;
            code[site - 1].operand = null;
            code[site].opcode = OpCodes.Call;
            code[site].operand = write;
            code.InsertRange(site, new[] { new CodeInstruction(OpCodes.Ldarg_2), new CodeInstruction(OpCodes.Ldarg_3) });
            return code;
        }
    }
}
