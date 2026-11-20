using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerMountedCombat.Integration
{
    // Known issues sprint 3. Two exact native read sites are answered with the pair's physical mover; the native
    // algorithm around each site is untouched. A site that is not exactly as recorded fails the patch installation.
    internal static class GroupPaceTranspilers
    {
        // M03. ClickGroundHandler.MoveSelectedUnitsToPoint (0x060093DB):
        //     num = selectedUnits.Select(u => u.ModifiedSpeedMps).Min();
        // The one Enumerable.Select<UnitEntityData, float> call is replaced by a hook of the same shape. The native
        // selector still reads each speed, and Min, the 30 ft floor, the Count > 1 gate and every later use of the
        // selection are native.
        internal static IEnumerable<CodeInstruction> SpeedInputs(IEnumerable<CodeInstruction> source, MethodInfo speedInputs)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count).Where(i => IsSpeedSelect(code[i])).ToArray();
            Require(sites.Length == 1 && sites[0] + 1 < code.Count && IsSingleMin(code[sites[0] + 1]),
                "native group speed-limit selection (Select of ModifiedSpeedMps, then Min)");
            code[sites[0]].opcode = OpCodes.Call;
            code[sites[0]].operand = speedInputs;
            return code;
        }

        // M02. InGameInputLayer.UpdateMovement (0x06005D0C):
        //     foreach (UnitEntityData selectedUnit in Game.Instance.UI.SelectionManager.SelectedUnits) ...Commands.Move.Accelerate()/Deaccelerate()
        // The one SelectedUnits read of the method feeds that loop; the hook answers it with the list to walk.
        internal static IEnumerable<CodeInstruction> GamepadFollowerOffer(IEnumerable<CodeInstruction> source, MethodInfo offerUnits)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count).Where(i => IsSelectedUnitsRead(code[i])).ToArray();
            Require(sites.Length == 1 && sites[0] + 1 < code.Count && IsListEnumerator(code[sites[0] + 1]) &&
                MoveCalls(code, "Accelerate") == 1 && MoveCalls(code, "Deaccelerate") == 1,
                "native gamepad follower acceleration pass (SelectedUnits, then Commands.Move Accelerate/Deaccelerate)");
            code.Insert(sites[0] + 1, new CodeInstruction(OpCodes.Call, offerUnits));
            return code;
        }

        private static bool IsSpeedSelect(CodeInstruction instruction)
        {
            var method = instruction.operand as MethodInfo;
            if (method == null || method.DeclaringType != typeof(Enumerable) || method.Name != "Select" || !method.IsGenericMethod) return false;
            var arguments = method.GetGenericArguments();
            var parameters = method.GetParameters();
            return arguments.Length == 2 && arguments[0] == typeof(UnitEntityData) && arguments[1] == typeof(float) &&
                parameters.Length == 2 && parameters[1].ParameterType == typeof(Func<UnitEntityData, float>);
        }

        private static bool IsSingleMin(CodeInstruction instruction)
        {
            var method = instruction.operand as MethodInfo;
            if (method == null || method.DeclaringType != typeof(Enumerable) || method.Name != "Min" || method.IsGenericMethod) return false;
            var parameters = method.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(IEnumerable<float>) && method.ReturnType == typeof(float);
        }

        private static bool IsSelectedUnitsRead(CodeInstruction instruction)
        {
            var method = instruction.operand as MethodInfo;
            return method != null && method.Name == "get_SelectedUnits" && method.DeclaringType == typeof(SelectionManagerBase);
        }

        private static bool IsListEnumerator(CodeInstruction instruction)
        {
            var method = instruction.operand as MethodInfo;
            return method != null && method.Name == "GetEnumerator" && method.DeclaringType == typeof(List<UnitEntityData>);
        }

        // The two calls of the follower pass are the only ones whose receiver comes from UnitCommands.Move; the
        // driver's own calls read UnitCommands.MoveContiniously.
        private static int MoveCalls(List<CodeInstruction> code, string name) =>
            Enumerable.Range(1, code.Count - 1).Count(i => Calls(code[i], name) && Calls(code[i - 1], "get_Move"));

        private static bool Calls(CodeInstruction instruction, string name)
        {
            var method = instruction.operand as MethodInfo;
            return method != null && method.Name == name;
        }

        private static void Require(bool condition, string site)
        {
            if (!condition) throw new InvalidOperationException("Group pace patch site is not as recorded: " + site + ".");
        }
    }
}
