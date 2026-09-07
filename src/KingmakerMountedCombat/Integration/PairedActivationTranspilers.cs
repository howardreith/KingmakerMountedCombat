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
            MethodInfo partnerContext, MethodInfo confusion)
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
            code[index].opcode = OpCodes.Call; code[index].operand = confusion;
            code.Insert(index, new CodeInstruction(OpCodes.Ldarg_0));
            return code;
        }

        internal static IEnumerable<CodeInstruction> PreparingActivity(IEnumerable<CodeInstruction> source, MethodInfo activity)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x06000C4D)).ToArray();
            Require(sites.Length == 1, "TurnController.Tick preparing activity");
            code[sites[0]].opcode = OpCodes.Call; code[sites[0]].operand = activity;
            return code;
        }
        private static bool Token(CodeInstruction instruction, int token) => instruction.operand is MemberInfo member && member.MetadataToken == token;
        private static void Require(bool condition, string boundary)
        {
            if (!condition) throw new InvalidOperationException("Paired activation structural contract mismatch: " + boundary);
        }
    }
}
