using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;

namespace KingmakerMountedCombat.Integration
{
    // These transformations retain the native loops and all native checks. Every
    // insertion requires its exact installed structural anchor; mismatch rolls
    // back the entire project Harmony installation.
    internal static class PairedActivationTranspilers
    {
        internal static IEnumerable<CodeInstruction> Selector(IEnumerable<CodeInstruction> source, MethodInfo skip)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count - 2).Where(i =>
                code[i].opcode == OpCodes.Ldloc_3 && Token(code[i + 1], 0x04007069) &&
                (code[i + 2].opcode == OpCodes.Brtrue_S || code[i + 2].opcode == OpCodes.Brtrue)).ToArray();
            Require(sites.Length == 1, "ChooseNextUnit candidate Surprised branch");
            var index = sites[0];
            // Add this participant to the native exclusion expression. The native
            // loop still owns wrap/StartRound, sorting, progression and next-unit assignment.
            var injected = new CodeInstruction(OpCodes.Ldloc_3);
            injected.labels.AddRange(code[index].labels); code[index].labels.Clear();
            code.InsertRange(index, new[] { injected, new CodeInstruction(OpCodes.Call, skip),
                new CodeInstruction(OpCodes.Brtrue, code[index + 2].operand) });
            return code;
        }

        internal static IEnumerable<CodeInstruction> ActorEligibility(IEnumerable<CodeInstruction> source, MethodInfo replacement, bool commandArgument)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x0600838E)).ToArray();
            Require(sites.Length == 1, "exact IsCurrentUnit call");
            var index = sites[0];
            if (commandArgument) { code.Insert(index, new CodeInstruction(OpCodes.Ldarg_1)); index++; }
            code[index].opcode = OpCodes.Call; code[index].operand = replacement;
            return code;
        }

        internal static IEnumerable<CodeInstruction> Preparation(IEnumerable<CodeInstruction> source, ILGenerator generator,
            MethodInfo partnerContext, MethodInfo confusion, MethodInfo resume)
        {
            var code = source.ToList();
            var tail = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x06000C29)).ToArray();
            var confusionSite = Enumerable.Range(1, code.Count - 1).Where(i =>
                Token(code[i - 1], 0x06009135) && Token(code[i], 0x0600910B)).ToArray();
            Require(tail.Length == 1 && confusionSite.Length == 1 && tail[0] > confusionSite[0], "native preparation gameplay/readiness/UI boundary");
            var next = generator.DefineLabel(); code[tail[0] + 1].labels.Add(next);
            code.InsertRange(tail[0] + 1, new[] { new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, partnerContext), new CodeInstruction(OpCodes.Brfalse, next), new CodeInstruction(OpCodes.Ret) });
            var index = confusionSite[0];
            var resumeTail = generator.DefineLabel();
            code[index + 1].labels.Add(resumeTail);
            code[index].opcode = OpCodes.Call; code[index].operand = confusion;
            code.Insert(index, new CodeInstruction(OpCodes.Ldarg_0));
            // Delay re-enters the native interaction/readiness tail. Its actor
            // grant, interruption, reactions and round/fact effects already ran.
            code.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, resume), new CodeInstruction(OpCodes.Brtrue, resumeTail) });
            return code;
        }

        internal static IEnumerable<CodeInstruction> PreparingActivity(IEnumerable<CodeInstruction> source, MethodInfo activity, MethodInfo phaseChanged)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x06000C4D)).ToArray();
            Require(sites.Length == 1, "TurnController.Tick preparing activity");
            code[sites[0]].opcode = OpCodes.Call; code[sites[0]].operand = activity;
            var phases = Enumerable.Range(2, code.Count - 2).Where(i => Token(code[i], 0x06000C0F) &&
                code[i - 1].opcode == OpCodes.Ldc_I4_3 && code[i - 2].opcode == OpCodes.Ldarg_0).ToArray();
            Require(phases.Length == 1, "TurnController.Tick native transition to Acting");
            // Observe the actual write in the caller. Hooking the tiny native
            // property setter does not intercept already-inlined callers.
            code.InsertRange(phases[0] + 1, new[] { new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, phaseChanged) });
            return code;
        }

        internal static IEnumerable<CodeInstruction> Readiness(IEnumerable<CodeInstruction> source, MethodInfo replacement)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x0600837C)).ToArray();
            Require(sites.Length == 1, "native readiness caller");
            code[sites[0]].opcode = OpCodes.Call; code[sites[0]].operand = replacement;
            return code;
        }

        internal static IEnumerable<CodeInstruction> CompletionDebt(IEnumerable<CodeInstruction> source,
            MethodInfo standard, MethodInfo move, MethodInfo swift, bool forfeit)
        {
            var code = source.ToList();
            var tokens = forfeit ? new[] { 0x0600C3B7, 0x0600C3B9, 0x0600C3BB } : new[] { 0x0600C3B7, 0x0600C3B9 };
            var replacements = new[] { standard, move, swift };
            for (var index = 0; index < tokens.Length; index++)
            {
                var sites = Enumerable.Range(0, code.Count).Where(i => Token(code[i], tokens[index])).ToArray();
                Require(sites.Length == 1, "completion native cooldown setter " + tokens[index].ToString("X8"));
                code[sites[0]].opcode = OpCodes.Call; code[sites[0]].operand = replacements[index];
            }
            Require(code.Count(i => Token(i, forfeit ? 0x06000C45 : 0x06000C62)) == 1,
                "completion state/command callback boundary");
            return code;
        }
        internal static IEnumerable<CodeInstruction> ActorConditionAction(IEnumerable<CodeInstruction> source,
            MethodInfo eligible, MethodInfo forfeit)
        {
            var code = source.ToList();
            var current = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x0600838E)).ToArray();
            var end = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x06000C47)).ToArray();
            if (current.Length != 1 || end.Length != 1 || current[0] >= end[0] ||
                current[0] == 0 || !Token(code[current[0]-1], 0x06002755))
                throw new InvalidOperationException("Native condition actor/forfeit contract changed.");
            // Preserve native argument stack, conditions, timing and action body.
            code[end[0]].opcode = OpCodes.Call; code[end[0]].operand = forfeit;
            code.Insert(end[0], new CodeInstruction(OpCodes.Ldarg_0));
            code[current[0]].opcode = OpCodes.Call; code[current[0]].operand = eligible;
            code.Insert(current[0], new CodeInstruction(OpCodes.Ldarg_0));
            return code;
        }
        internal static IEnumerable<CodeInstruction> ActorForfeitPhase(IEnumerable<CodeInstruction> source, MethodInfo complete)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x06000C45)).ToArray();
            if (sites.Length != 1) throw new InvalidOperationException("Native ForceToEnd phase contract changed.");
            code[sites[0]].opcode = OpCodes.Call; code[sites[0]].operand = complete;
            return code;
        }
        private static bool Token(CodeInstruction instruction, int token) => instruction.operand is MemberInfo member && member.MetadataToken == token;
        private static void Require(bool condition, string boundary)
        {
            if (!condition) throw new InvalidOperationException("Paired activation structural contract mismatch: " + boundary);
        }
    }
}
