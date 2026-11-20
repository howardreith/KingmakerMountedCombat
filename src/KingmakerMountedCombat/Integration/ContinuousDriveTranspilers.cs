using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;

namespace KingmakerMountedCombat.Integration
{
    // Known issues sprint 3 continuation (M05). The native reads that identify the stick-driven unit are answered with
    // the pair's physical mover; the native code around each read is untouched. A site that is not exactly as recorded
    // fails the patch installation.
    internal static class ContinuousDriveTranspilers
    {
        // InGameInputLayer.UpdateMovement (0x06005D0C):
        //     UnitEntityData unit = UnitSelectionManager.Instance.CurrentSelectUnit.Value;
        // The one store of that local receives the driven unit. Every later native step (the override reads,
        // Commands.MoveContiniously, InterruptMove, Init, Commands.Run, Accelerate, Deaccelerate) then uses that one unit.
        // The two MakeCompanionsFollow(unit) calls receive the formation anchor: the unit the player selected.
        internal static IEnumerable<CodeInstruction> DrivenUnit(IEnumerable<CodeInstruction> source, MethodInfo drivenUnit, MethodInfo formationUnit)
        {
            var code = source.ToList();
            var stores = Enumerable.Range(0, code.Count).Where(i => IsDrivenUnitStore(code[i])).ToArray();
            var follows = Enumerable.Range(0, code.Count).Where(i => IsCompanionsFollow(code[i])).ToArray();
            Require(stores.Length == 1 && stores[0] > 0 && IsCurrentSelectUnitValue(code[stores[0] - 1]) &&
                follows.Length == 2 && follows[0] > stores[0] &&
                follows.All(i => IsDrivenUnitLoad(code[i - 1])) &&
                code.Count(c => Constructs(c, typeof(Kingmaker.UnitLogic.Commands.UnitMoveContiniously))) == 1,
                "native stick-driven unit (CurrentSelectUnit.Value stored once, MakeCompanionsFollow twice, one UnitMoveContiniously)");
            foreach (var index in follows.Reverse()) code.Insert(index, new CodeInstruction(OpCodes.Call, formationUnit));
            code.Insert(stores[0], new CodeInstruction(OpCodes.Call, drivenUnit));
            return code;
        }

        // ClickGroundHandler.MoveSelectedUnitsToPoint (0x060093DB):
        //     bool flag = unitEntityData?.View?.AgentOverride != null;
        // The method's one AgentOverride read asks whether the current unit is being stick-driven.
        internal static IEnumerable<CodeInstruction> DrivenOverrideRead(IEnumerable<CodeInstruction> source, MethodInfo drivenOverride)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count).Where(i => IsAgentOverrideRead(code[i])).ToArray();
            Require(sites.Length == 1, "native stick-driven test of the current unit (one UnitEntityView.AgentOverride read)");
            code[sites[0]].opcode = OpCodes.Call;
            code[sites[0]].operand = drivenOverride;
            return code;
        }

        // UnitMoveContiniously.OnEnded (0x060026F2):
        //     if (base.Executor?.View?.AgentOverride != null) base.Executor.View.AgentOverride = null;
        // The method's one AgentOverride write is made through the guard, which also receives the ending command.
        // Everything else in the method, base.OnEnded and its end event included, is native and always runs.
        internal static IEnumerable<CodeInstruction> EndedCommandOverrideClear(IEnumerable<CodeInstruction> source, MethodInfo guardedClear)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count).Where(i => IsAgentOverrideWrite(code[i])).ToArray();
            Require(sites.Length == 1 && sites[0] > 0 && code[sites[0] - 1].opcode == OpCodes.Ldnull && code[sites[0]].labels.Count == 0,
                "native removal of the ended command's override (one UnitEntityView.AgentOverride = null)");
            code[sites[0]].opcode = OpCodes.Call;
            code[sites[0]].operand = guardedClear;
            code.Insert(sites[0], new CodeInstruction(OpCodes.Ldarg_0));
            return code;
        }

        private static bool IsAgentOverrideWrite(CodeInstruction instruction)
        {
            var method = instruction.operand as MethodInfo;
            return method != null && method.Name == "set_AgentOverride" && method.DeclaringType == typeof(UnitEntityView);
        }

        private static bool IsDrivenUnitField(object operand)
        {
            var field = operand as FieldInfo;
            return field != null && field.Name == "unit" && field.FieldType == typeof(UnitEntityData) &&
                field.DeclaringType != null && field.DeclaringType.DeclaringType == typeof(Kingmaker.UI._ConsoleUI.InputLayers.InGameLayer.InGameInputLayer);
        }

        private static bool IsDrivenUnitStore(CodeInstruction instruction) => instruction.opcode == OpCodes.Stfld && IsDrivenUnitField(instruction.operand);

        private static bool IsDrivenUnitLoad(CodeInstruction instruction) => instruction.opcode == OpCodes.Ldfld && IsDrivenUnitField(instruction.operand);

        private static bool IsCurrentSelectUnitValue(CodeInstruction instruction)
        {
            var method = instruction.operand as MethodInfo;
            var type = method != null ? method.DeclaringType : null;
            return type != null && method.Name == "get_Value" && type.IsGenericType && type.Name.StartsWith("ReactiveProperty", StringComparison.Ordinal) &&
                type.GetGenericArguments().Length == 1 && type.GetGenericArguments()[0] == typeof(UnitEntityData);
        }

        private static bool IsCompanionsFollow(CodeInstruction instruction)
        {
            var method = instruction.operand as MethodInfo;
            return method != null && method.Name == "MakeCompanionsFollow" &&
                method.DeclaringType == typeof(Kingmaker.UI._ConsoleUI.InputLayers.InGameLayer.InGameInputLayer);
        }

        private static bool Constructs(CodeInstruction instruction, Type type)
        {
            var constructor = instruction.operand as ConstructorInfo;
            return instruction.opcode == OpCodes.Newobj && constructor != null && constructor.DeclaringType == type;
        }

        private static bool IsAgentOverrideRead(CodeInstruction instruction)
        {
            var method = instruction.operand as MethodInfo;
            return method != null && method.Name == "get_AgentOverride" && method.DeclaringType == typeof(UnitEntityView);
        }

        private static void Require(bool condition, string site)
        {
            if (!condition) throw new InvalidOperationException("Continuous drive patch site is not as recorded: " + site + ".");
        }
    }
}
