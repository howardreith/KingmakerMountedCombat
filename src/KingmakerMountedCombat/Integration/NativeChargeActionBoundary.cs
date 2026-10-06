using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Harmony12.ILCopying;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Commands;

namespace KingmakerMountedCombat.Integration
{
    // Harmony12 has no exception finalizer. Preserve the pinned native body and
    // its nested finally blocks inside one outer scope; every return still returns
    // its original value and every exception still propagates unchanged.
    internal static class NativeChargeActionBoundary
    {
        internal static IEnumerable<CodeInstruction> Wrap(IEnumerable<CodeInstruction> source, ILGenerator generator,
            MethodBase original, MethodInfo beginScope, MethodInfo triggerRule)
        {
            if (original.DeclaringType != typeof(UnitUseAbility) || original.MetadataToken != 0x06002737)
                throw new InvalidOperationException("Unpinned native charge action body.");
            var code = source.ToList();
            var scope = generator.DeclareLocal(typeof(Action<bool>));
            var returned = generator.DeclareLocal(typeof(bool));
            var result = generator.DeclareLocal(((MethodInfo)original).ReturnType);
            var done = generator.DefineLabel();
            var output = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, beginScope),
                new CodeInstruction(OpCodes.Stloc, scope),
                new CodeInstruction(OpCodes.Ldc_I4_0), new CodeInstruction(OpCodes.Stloc, returned)
            };
            var begin = new CodeInstruction(OpCodes.Nop);
            begin.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock, null)); output.Add(begin);
            var triggers = 0;
            foreach (var instruction in code)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method &&
                    method.DeclaringType == typeof(Rulebook) && method.Name == "Trigger" && method.IsGenericMethod &&
                    method.GetGenericArguments().Single() == typeof(RuleCastSpell))
                {
                    var instance = new CodeInstruction(OpCodes.Ldarg_0);
                    instance.labels.AddRange(instruction.labels); instance.blocks.AddRange(instruction.blocks);
                    output.Add(instance); output.Add(new CodeInstruction(OpCodes.Call, triggerRule)); triggers++;
                    continue;
                }
                if (instruction.opcode != OpCodes.Ret) { output.Add(instruction); continue; }
                var store = new CodeInstruction(OpCodes.Stloc, result);
                store.labels.AddRange(instruction.labels); store.blocks.AddRange(instruction.blocks);
                output.Add(store); output.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
                output.Add(new CodeInstruction(OpCodes.Stloc, returned));
                output.Add(new CodeInstruction(OpCodes.Leave, done));
            }
            if (triggers != 1) throw new InvalidOperationException("Native action rule registration call changed.");
            var finish = new CodeInstruction(OpCodes.Ldloc, scope);
            finish.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock, null));
            output.Add(finish); output.Add(new CodeInstruction(OpCodes.Ldloc, returned));
            output.Add(new CodeInstruction(OpCodes.Call, typeof(NativeChargeActionBoundary).GetMethod(nameof(Complete), BindingFlags.Static | BindingFlags.NonPublic)));
            var end = new CodeInstruction(OpCodes.Nop);
            end.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock, null)); output.Add(end);
            var load = new CodeInstruction(OpCodes.Ldloc, result); load.labels.Add(done); output.Add(load);
            output.Add(new CodeInstruction(OpCodes.Ret));
            return output;
        }

        private static void Complete(Action<bool> scope, bool returned) => scope?.Invoke(returned);
    }
}
