using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    // Keep native input and prediction operations on their addressed actor.
    internal static class PairedActivationControlTranspilers
    {
        internal static IEnumerable<CodeInstruction> TickInput(IEnumerable<CodeInstruction> source,
            ILGenerator generator, MethodInfo preserveSelection, MethodInfo inputContext)
        {
            var code = source.ToList();
            var selection = Enumerable.Range(2, code.Count - 3).FirstOrDefault(i =>
                Token(code[i], 0x06008328) && Token(code[i - 1], 0x04000669) && code[i - 2].opcode == OpCodes.Ldarg_0);
            Require(selection > 0 && selection < 8 &&
                (code[selection + 1].opcode == OpCodes.Brfalse_S || code[selection + 1].opcode == OpCodes.Brfalse),
                "native Tick initial selection block");
            var selectionExit = (Label)code[selection + 1].operand;
            var exit = code.FindIndex(i => i.labels.Contains(selectionExit));
            Require(exit > selection && code[exit].opcode == OpCodes.Ldarg_0 && Token(code[exit + 1], 0x06000C0E),
                "native selection exit precedes status/scroll/preparation processing");
            var start = selection - 2;
            var first = new CodeInstruction(OpCodes.Ldarg_0);
            first.labels.AddRange(code[start].labels); code[start].labels.Clear();
            code.InsertRange(start, new[] { first, new CodeInstruction(OpCodes.Call, preserveSelection),
                new CodeInstruction(OpCodes.Brtrue, code[selection + 1].operand) });

            var tails = Enumerable.Range(1, code.Count - 1).Where(i =>
                code[i].opcode == OpCodes.Ldfld && Token(code[i], 0x04000691)).ToArray();
            Require(tails.Length == 1 && code[tails[0] - 1].opcode == OpCodes.Ldarg_0, "native Tick input tail");
            start = tails[0] - 1;
            var tail = code.Skip(start).ToArray();
            Require(tail.Count(i => i.opcode == OpCodes.Ldarg_0) == 11 &&
                tail.Count(i => Token(i, 0x06000C6E)) == 1 && tail.Count(i => Token(i, 0x06000C41)) == 1 &&
                tail.Count(i => Token(i, 0x06000C35)) == 1 && !tail.Any(i =>
                    Token(i, 0x06000C3C) || Token(i, 0x06000C46) || Token(i, 0x06000C34)),
                "native Tick UI-only prediction/cursor/range tail");
            var context = generator.DeclareLocal(typeof(TurnController));
            for (var i = start; i < code.Count; i++)
                if (code[i].opcode == OpCodes.Ldarg_0) { code[i].opcode = OpCodes.Ldloc; code[i].operand = context; }
            first = new CodeInstruction(OpCodes.Ldarg_0);
            first.labels.AddRange(code[start].labels); code[start].labels.Clear();
            code.InsertRange(start, new[] { first, new CodeInstruction(OpCodes.Call, inputContext),
                new CodeInstruction(OpCodes.Stloc, context) });
            return code;
        }

        internal static IEnumerable<CodeInstruction> ViewModelContext(IEnumerable<CodeInstruction> source,
            MethodInfo replacement, int expectedCalls)
        {
            var code = source.ToList();
            var sites = code.Where(i => Token(i, 0x06004F28)).ToArray();
            Require(sites.Length == expectedCalls, "native prediction VM context readers");
            foreach (var site in sites) { site.opcode = OpCodes.Call; site.operand = replacement; }
            return code;
        }

        internal static IEnumerable<CodeInstruction> ControllerInput(IEnumerable<CodeInstruction> source, MethodInfo inputContext)
        {
            var code = source.ToList();
            var sites = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x0400064A) || Token(code[i], 0x06000BBE)).ToArray();
            Require(sites.Length == 1, "native input controller context reader");
            code.Insert(sites[0] + 1, new CodeInstruction(OpCodes.Call, inputContext));
            return code;
        }

        internal static IEnumerable<CodeInstruction> PathUnitReads(IEnumerable<CodeInstruction> source,
            MethodInfo inputUnit, int expectedReads)
        {
            var code = source.ToList();
            var sites = code.Where(i => Token(i, 0x06000BFA)).ToArray();
            Require(sites.Length == expectedReads && sites.All(i => i.opcode == OpCodes.Call),
                "native path preview global actor readers");
            foreach (var site in sites) site.operand = inputUnit;
            return code;
        }

        internal static IEnumerable<CodeInstruction> FullAttackRestriction(IEnumerable<CodeInstruction> source,
            MethodInfo eligibleActor, MethodInfo actorContext)
        {
            var code = source.ToList();
            var eligible = code.Where(i => Token(i, 0x0600838E)).ToArray();
            var contexts = Enumerable.Range(0, code.Count).Where(i => Token(code[i], 0x06000BBE)).ToArray();
            Require(eligible.Length == 1 && contexts.Length == 1 &&
                code.Count(i => Token(i, 0x06008382)) == 1 && code.Count(i => Token(i, 0x06000C1D)) == 1,
                "native actor full-attack movement/Single restriction");
            eligible[0].opcode = OpCodes.Call; eligible[0].operand = eligibleActor;
            var actor = typeof(Kingmaker.Controllers.Combat.UnitCombatState).GetField("Unit");
            Require(actor != null && actor.MetadataToken == 0x04005E97, "combat state actor field");
            var context = contexts[0];
            code[context].opcode = OpCodes.Call; code[context].operand = actorContext;
            var first = new CodeInstruction(OpCodes.Ldarg_0);
            first.labels.AddRange(code[context].labels); code[context].labels.Clear();
            code.InsertRange(context, new[] { first, new CodeInstruction(OpCodes.Ldfld, actor) });
            return code;
        }

        private static bool Token(CodeInstruction instruction, int token) => instruction.operand is MemberInfo member && member.MetadataToken == token;
        private static void Require(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("Paired input structural contract mismatch: " + description);
        }
    }
}
