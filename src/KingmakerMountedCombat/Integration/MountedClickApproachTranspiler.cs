using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;

namespace KingmakerMountedCombat.Integration
{
    // Known issues M04 closeout, review of b1a88b4e (R1). In native ClickUnitHandler.OnClick (060093ED) the cannot-attack
    // branch builds the method's one UnitMoveTo (new UnitMoveTo(target position, corpulence * 1.1), nothing else set),
    // stores it in the local that holds every click command, and later runs that local through one
    // UnitCommands.Run(UnitCommand). Only that call is replaced, by a hook receiving the same container and command; every
    // other instruction, and every other Run, is untouched. The local can also hold the cast or attack command; the hook
    // acts only on an exact UnitMoveTo, which in this method only that construction makes.
    internal static class MountedClickApproachTranspiler
    {
        internal static IEnumerable<CodeInstruction> CannotAttackApproachRun(IEnumerable<CodeInstruction> source, MethodInfo hook)
        {
            var code = source.ToList();
            var moveCtor = typeof(UnitMoveTo).GetConstructor(new[] { typeof(UnityEngine.Vector3), typeof(float) });
            var run = typeof(UnitCommands).GetMethod("Run", new[] { typeof(UnitCommand) });
            var constructions = Enumerable.Range(0, code.Count).Where(i => code[i].opcode == OpCodes.Newobj && Equals(code[i].operand, moveCtor)).ToArray();
            Require(constructions.Length == 1 && constructions[0] + 1 < code.Count, "one UnitMoveTo construction");
            var local = StoredLocal(code[constructions[0] + 1]);
            Require(local >= 0, "the UnitMoveTo is stored in a local");
            var sites = Enumerable.Range(1, code.Count - 1).Where(i => (code[i].opcode == OpCodes.Callvirt || code[i].opcode == OpCodes.Call) &&
                Equals(code[i].operand, run) && LoadedLocal(code[i - 1]) == local).ToArray();
            Require(sites.Length == 1 && sites[0] > constructions[0], "one UnitCommands.Run of the click command's local after the construction");
            code[sites[0]].opcode = OpCodes.Call;
            code[sites[0]].operand = hook;
            return code;
        }

        private static int StoredLocal(CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Stloc_0) return 0;
            if (instruction.opcode == OpCodes.Stloc_1) return 1;
            if (instruction.opcode == OpCodes.Stloc_2) return 2;
            if (instruction.opcode == OpCodes.Stloc_3) return 3;
            if (instruction.opcode == OpCodes.Stloc_S || instruction.opcode == OpCodes.Stloc) return Index(instruction.operand);
            return -1;
        }

        private static int LoadedLocal(CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Ldloc_0) return 0;
            if (instruction.opcode == OpCodes.Ldloc_1) return 1;
            if (instruction.opcode == OpCodes.Ldloc_2) return 2;
            if (instruction.opcode == OpCodes.Ldloc_3) return 3;
            if (instruction.opcode == OpCodes.Ldloc_S || instruction.opcode == OpCodes.Ldloc) return Index(instruction.operand);
            return -2;
        }

        private static int Index(object operand)
        {
            if (operand is LocalVariableInfo variable) return variable.LocalIndex;
            if (operand is byte b) return b;
            if (operand is short s) return s;
            if (operand is int i) return i;
            return -1;
        }

        private static void Require(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("Cannot-attack click approach structural contract mismatch: " + description);
        }
    }
}
