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
    // stores it in the local that holds every click command, and later submits that local through one
    // UnitCommands.Run(UnitCommand). The local can also hold the cast or attack command; the router acts only on an exact
    // UnitMoveTo, which in this method only that construction makes.
    //
    // Tabletop Rules Expansion compatibility (startup repair after 0.2.12). The submission is no longer replaced: a guard is
    // placed in front of it. The router receives the same container and command; when it has handled the command (routed
    // to the mount, refused by the ground-order admission, or already given to another unit) the submission is skipped,
    // otherwise the submission runs exactly as the method has it. No other instruction changes, so the transform composes,
    // in either Harmony order, with a transpiler that rewrites this method's submissions. Two submission shapes are
    // accepted, each consuming (UnitCommands, UnitCommand):
    //   native:     ldloc command; callvirt UnitCommands.Run(UnitCommand)
    //   Tabletop Rules Expansion 0.0.191 (its NativeFirearmAttackOrderPatch.DesktopBody rewrites every native Run here):
    //               ldloc command; ldloc invocation; call NativeFirearmAttackOrder.Submit(UnitCommands, UnitCommand, InputInvocation)
    // Result: dup; ldloc command; call router; brtrue handled; <submission unchanged>; br done; handled: pop; done: nop
    // That mod is never referenced: its shape is matched by exact type name only (Validate-Source.ps1 allows that one line).
    internal static class MountedClickApproachTranspiler
    {
        internal const string GunslingerOrderType = "KingmakerGunslinger.Firing.NativeFirearmAttackOrder";
        internal const string GunslingerInvocationType = GunslingerOrderType + "+InputInvocation";

        internal static IEnumerable<CodeInstruction> CannotAttackApproachRun(IEnumerable<CodeInstruction> source, ILGenerator generator, MethodInfo router)
        {
            Require(generator != null, "Harmony supplies the method's IL generator");
            Require(router != null && router.IsStatic && router.ReturnType == typeof(bool) &&
                router.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(UnitCommands), typeof(UnitCommand) }),
                "the router is static bool (UnitCommands, UnitCommand)");
            var code = source.ToList();
            var moveCtor = typeof(UnitMoveTo).GetConstructor(new[] { typeof(UnityEngine.Vector3), typeof(float) });
            var constructions = Enumerable.Range(0, code.Count).Where(i => code[i].opcode == OpCodes.Newobj && Equals(code[i].operand, moveCtor)).ToArray();
            Require(constructions.Length == 1 && constructions[0] + 1 < code.Count, "one UnitMoveTo construction");
            var local = StoredLocal(code[constructions[0] + 1]);
            Require(local >= 0, "the UnitMoveTo is stored in a local");
            var first = constructions[0] + 2;
            var sites = Enumerable.Range(first, Math.Max(0, code.Count - first))
                .Where(i => LoadedLocal(code[i]) == local && SubmissionLength(code, i + 1) > 0).ToArray();
            Require(sites.Length == 1, "one submission of the click command's local after the construction (native UnitCommands.Run, or " +
                GunslingerOrderType + ".Submit with its input invocation); found " + sites.Length + Followers(code, first, local));
            var site = sites[0];
            var length = SubmissionLength(code, site + 1);
            for (var i = site; i <= site + length; i++)
                Require(code[i].blocks.Count == 0 && (i == site || code[i].labels.Count == 0),
                    "the submission is one straight sequence (no exception block boundary in it, no branch into its middle)");

            var handled = generator.DefineLabel();
            var done = generator.DefineLabel();
            var guard = new CodeInstruction(OpCodes.Dup);
            guard.labels.AddRange(code[site].labels);
            code[site].labels.Clear();
            var skip = new CodeInstruction(OpCodes.Pop);
            skip.labels.Add(handled);
            var after = new CodeInstruction(OpCodes.Nop);
            after.labels.Add(done);
            code.InsertRange(site + 1 + length, new[] { new CodeInstruction(OpCodes.Br, done), skip, after });
            code.InsertRange(site, new[]
            {
                guard,
                new CodeInstruction(code[site].opcode, code[site].operand),
                new CodeInstruction(OpCodes.Call, router),
                new CodeInstruction(OpCodes.Brtrue, handled),
            });
            return code;
        }

        // The number of instructions after the command load that make up one recognised submission, or 0.
        private static int SubmissionLength(List<CodeInstruction> code, int start)
        {
            if (start < code.Count && IsNativeRun(code[start])) return 1;
            if (start + 1 < code.Count && IsGunslingerInvocationLoad(code[start]) && IsGunslingerSubmit(code[start + 1], (LocalBuilder)code[start].operand))
                return 2;
            return 0;
        }

        private static readonly MethodInfo NativeRun = typeof(UnitCommands).GetMethod("Run", new[] { typeof(UnitCommand) });

        private static bool IsNativeRun(CodeInstruction instruction) =>
            (instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call) && Equals(instruction.operand, NativeRun);

        private static bool IsGunslingerInvocationLoad(CodeInstruction instruction) =>
            (instruction.opcode == OpCodes.Ldloc || instruction.opcode == OpCodes.Ldloc_S) && instruction.operand is LocalBuilder invocation &&
            invocation.LocalType != null && invocation.LocalType.FullName == GunslingerInvocationType;

        private static bool IsGunslingerSubmit(CodeInstruction instruction, LocalBuilder invocation)
        {
            if (instruction.opcode != OpCodes.Call || !(instruction.operand is MethodInfo submit)) return false;
            var parameters = submit.GetParameters();
            return submit.IsStatic && submit.Name == "Submit" && submit.DeclaringType != null && submit.DeclaringType.FullName == GunslingerOrderType &&
                submit.ReturnType == typeof(void) && parameters.Length == 3 && parameters[0].ParameterType == typeof(UnitCommands) &&
                parameters[1].ParameterType == typeof(UnitCommand) && parameters[2].ParameterType == invocation.LocalType;
        }

        // Diagnostic only: what follows each load of the command local after the construction.
        private static string Followers(List<CodeInstruction> code, int first, int local)
        {
            var seen = new List<string>();
            for (var i = first; i + 1 < code.Count && seen.Count < 8; i++)
                if (LoadedLocal(code[i]) == local)
                    seen.Add(Describe(code[i + 1]) + (i + 2 < code.Count ? " then " + Describe(code[i + 2]) : string.Empty));
            return "; the command local is next used by: " + (seen.Count == 0 ? "nothing" : string.Join(" | ", seen.ToArray()));
        }

        private static string Describe(CodeInstruction instruction)
        {
            if (instruction.operand is MethodBase method) return instruction.opcode + " " + method.DeclaringType?.FullName + "." + method.Name;
            if (instruction.operand is LocalVariableInfo variable)
                return instruction.opcode + " local " + variable.LocalIndex + " (" + variable.LocalType?.FullName + ")";
            return instruction.opcode + (instruction.operand == null ? string.Empty : " " + instruction.operand);
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
