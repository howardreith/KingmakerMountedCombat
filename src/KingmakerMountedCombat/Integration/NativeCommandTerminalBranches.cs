using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Known issues Sprint 6, review of fa533e97 (R18). Native's own capability terminals are identified where native takes
    // them, never reconstructed from a terminal result, a started flag or a later condition snapshot.
    //
    // Review of dffd3e72 (R21): nothing here owns a command. Every native call stays in place; right after native computes
    // a value, the transpiled method duplicates it, encodes it (Observe...) and keeps it in a new local of that very native
    // invocation, and it passes that local to its own terminal call. The facts therefore live and die with the native frame
    // and belong to its own command: a healthy tick, an ordinary completion, an exception or a nested tick of another
    // command leaves nothing behind and cannot lend its facts to anyone. The capture hooks never see a command.
    //
    // Review of 95ae0b30 (R23): the ShouldBeInterrupted observation is made by ObserveShouldBeInterrupted, which replaces
    // TickCommand's one call of the getter. It opens one scalar frame, calls native's getter exactly once, lets the
    // getter's CanNotAttack term write only that innermost frame, writes the encoded observation to TickCommand's own local
    // (out) and returns native's Boolean unchanged, closing its frame in a finally. The frames are scalars only
    // (MountedNativeTermFrames), so nested or exceptional calls cannot exchange terms and nothing owns a command.
    //
    // Installed 2.1.7b sites:
    // - TickCommandTurnBased (0600911D): the canAct value native tests before its first ForceFinishForTurnBased
    //   (CanAct && !HasCondition(Nauseated)), and that force-finish. The second force-finish (approach time) is untouched.
    // - TickCommand (0600911E): its one get_ShouldBeInterrupted call (wrapped), the state-site Interrupt, the unstarted
    //   InterruptAsSoonAsPossible Interrupt, the value of the running get_CanAct and the running-site Interrupt.
    // - UnitAttack.get_ShouldBeInterrupted (0600266D): the value of its one HasCondition(CanNotAttack) term.
    internal static class NativeCommandTerminalBranches
    {
        internal const int TickCommandTurnBasedToken = 0x0600911D;
        internal const int TickCommandToken = 0x0600911E;
        internal const int AttackShouldBeInterruptedToken = 0x0600266D;
        private const int ForceFinishForTurnBasedToken = 0x060027AB;
        private const int InterruptToken = 0x060027AC;
        private const int ShouldBeInterruptedToken = 0x06002783;
        private const int InterruptAsSoonAsPossibleToken = 0x0600276E;
        private const int CanActToken = 0x06001FAF;
        private const int HasConditionToken = 0x06001FBB;

        [ThreadStatic] private static MountedNativeTermFrames attackBanFrames;

        internal static int CaptureCanAct(bool canAct) => MountedNativeTerminalObservation.ObserveCanAct(canAct);

        // The transpiled UnitAttack.get_ShouldBeInterrupted passes its CanNotAttack term here; only the innermost open frame
        // (the wrapped call this getter runs inside) takes it. Outside any wrapped call it changes nothing.
        internal static bool AttackBanTerm(bool value)
        {
            attackBanFrames?.SetInnermost(value);
            return value;
        }

        // Replaces TickCommand's command.ShouldBeInterrupted: native's getter runs exactly once and its value is returned.
        internal static bool ObserveShouldBeInterrupted(UnitCommand command, out int observation)
        {
            observation = MountedNativeTerminalObservation.NotObserved;
            var frames = attackBanFrames ?? (attackBanFrames = new MountedNativeTermFrames());
            var frame = frames.Open();
            try
            {
                var value = command.ShouldBeInterrupted;
                observation = MountedNativeTerminalObservation.ObserveShouldBeInterrupted(value, frames.Term(frame));
                return value;
            }
            finally
            {
                frames.Close(frame);
            }
        }

        internal static void TurnBasedForceFinish(UnitCommand command, UnitCommand.ResultType result, int canActObservation)
        {
            Record(command, MountedNativeTerminalBranchPolicy.TurnBasedForceFinish(canActObservation));
            command.ForceFinishForTurnBased(result);
        }

        internal static void InterruptAtStateSite(UnitCommand command, bool raiseEvent, int shouldBeInterruptedObservation)
        {
            Record(command, MountedNativeTerminalBranchPolicy.StateSite(shouldBeInterruptedObservation));
            command.Interrupt(raiseEvent);
        }

        internal static void InterruptAsSoonAsPossible(UnitCommand command, bool raiseEvent)
        {
            Record(command, MountedNativeTerminalBranch.InterruptAsSoonAsPossible);
            command.Interrupt(raiseEvent);
        }

        internal static void InterruptAtRunningSite(UnitCommand command, bool raiseEvent, int canActObservation)
        {
            Record(command, MountedNativeTerminalBranchPolicy.RunningSite(canActObservation));
            command.Interrupt(raiseEvent);
        }

        // Mod disable and disposal: no scalar frame survives.
        internal static void Reset()
        {
            attackBanFrames = null;
        }

        // Only a pair command records a branch, and only while it can still end. The block is read at the branch.
        private static void Record(UnitCommand command, MountedNativeTerminalBranch branch)
        {
            try
            {
                var pair = command as MountedPairAttackCommand;
                if (pair == null || pair.IsFinished) return;
                var facts = MountedNativeTerminalBranchPolicy.IsCapability(branch) && branch != MountedNativeTerminalBranch.AttackBanned
                    ? NativeActorCapability.Read(command.Executor)
                    : default(MountedActorCapabilityFacts);
                pair.RecordNativeTerminal(branch, MountedNativeTerminalBranchPolicy.Block(branch, facts));
            }
            catch (Exception)
            {
                // Evidence only: a failed record leaves the branch None (never a capability cause) and native ends as before.
            }
        }

        private static bool Calls(CodeInstruction instruction, int token) =>
            (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
            instruction.operand is MethodBase method && method.MetadataToken == token &&
            method.Module.Assembly == typeof(UnitCommand).Assembly;

        private static int[] Sites(List<CodeInstruction> code, int token) =>
            Enumerable.Range(0, code.Count).Where(i => Calls(code[i], token)).ToArray();

        private static void Require(bool condition, string contract)
        {
            if (!condition) throw new InvalidOperationException("Native terminal branch structural contract mismatch: " + contract + ".");
        }

        private static void Replace(CodeInstruction instruction, MethodInfo hook)
        {
            instruction.opcode = OpCodes.Call;
            instruction.operand = hook;
        }

        // dup; call capture; stloc local: the value native computed stays on the stack for native's own branch.
        private static CodeInstruction[] Keep(MethodInfo capture, LocalBuilder local) => new[]
        {
            new CodeInstruction(OpCodes.Dup), new CodeInstruction(OpCodes.Call, capture), new CodeInstruction(OpCodes.Stloc, local)
        };

        internal static IEnumerable<CodeInstruction> TurnBased(IEnumerable<CodeInstruction> source, ILGenerator generator, MethodInfo capture,
            MethodInfo forceFinish)
        {
            var code = source.ToList();
            var finishes = Sites(code, ForceFinishForTurnBasedToken);
            Require(finishes.Length == 2, "two ForceFinishForTurnBased calls");
            var site = finishes[0];
            Require(site >= 2 && code[site - 2].opcode == OpCodes.Ldarg_1 && code[site - 1].opcode == OpCodes.Ldc_I4_3,
                "the first force-finish is command.ForceFinishForTurnBased(Success)");
            var entry = code[site - 2].labels;
            var tests = Enumerable.Range(0, site).Where(i => (code[i].opcode == OpCodes.Brfalse || code[i].opcode == OpCodes.Brfalse_S) &&
                code[i].operand is Label label && entry.Contains(label)).ToArray();
            Require(tests.Length == 1, "one canAct test branching to the force-finish");
            var test = tests[0];
            Require(code[test + 1].opcode == OpCodes.Ldloc_S && (code[test + 2].opcode == OpCodes.Brtrue || code[test + 2].opcode == OpCodes.Brtrue_S) &&
                Sites(code, CanActToken).Count(i => i < test) == 1, "canAct is tested before the cooldown slot");
            var canAct = generator.DeclareLocal(typeof(int));
            Replace(code[site], forceFinish);
            code.Insert(site, new CodeInstruction(OpCodes.Ldloc, canAct));
            var keep = Keep(capture, canAct);
            keep[0].labels.AddRange(code[test].labels);
            code[test].labels.Clear();
            code.InsertRange(test, keep);
            return code;
        }

        internal static IEnumerable<CodeInstruction> Tick(IEnumerable<CodeInstruction> source, ILGenerator generator, MethodInfo observeShouldBeInterrupted,
            MethodInfo stateSite, MethodInfo asSoonAsPossible, MethodInfo captureCanAct, MethodInfo runningSite)
        {
            var code = source.ToList();
            var should = Sites(code, ShouldBeInterruptedToken);
            var interrupts = Sites(code, InterruptToken);
            var asap = Sites(code, InterruptAsSoonAsPossibleToken);
            var canAct = Sites(code, CanActToken);
            Require(should.Length == 1 && interrupts.Length == 3 && asap.Length == 2 && canAct.Length == 1, "TickCommand call counts");
            Require(should[0] < interrupts[0] && interrupts[0] < asap[0] && asap[0] < interrupts[1] && interrupts[1] < asap[1] &&
                asap[1] < canAct[0] && canAct[0] < interrupts[2], "TickCommand site order");
            Require(interrupts.All(i => code[i - 2].opcode == OpCodes.Ldarg_1 && code[i - 1].opcode == OpCodes.Ldc_I4_1), "Interrupt(true) sites");
            Require(code[should[0] - 1].opcode == OpCodes.Ldarg_1 && code[should[0]].labels.Count == 0, "command.ShouldBeInterrupted");
            Require(code[should[0] + 1].opcode == OpCodes.Brtrue_S || code[should[0] + 1].opcode == OpCodes.Brtrue, "ShouldBeInterrupted branches");
            var stateEntry = code[interrupts[0] - 2].labels;
            Require(code[should[0] + 1].operand is Label shouldTarget && stateEntry.Contains(shouldTarget), "ShouldBeInterrupted reaches the state site");
            Require((code[canAct[0] + 1].opcode == OpCodes.Brtrue_S || code[canAct[0] + 1].opcode == OpCodes.Brtrue) &&
                code[canAct[0] + 1].labels.Count == 0, "the running CanAct branches");
            var shouldObservation = generator.DeclareLocal(typeof(int));
            var canActObservation = generator.DeclareLocal(typeof(int));
            // Highest index first, so every earlier site keeps its index.
            Replace(code[interrupts[2]], runningSite);
            code.Insert(interrupts[2], new CodeInstruction(OpCodes.Ldloc, canActObservation));
            code.InsertRange(canAct[0] + 1, Keep(captureCanAct, canActObservation));
            Replace(code[interrupts[1]], asSoonAsPossible);
            Replace(code[interrupts[0]], stateSite);
            code.Insert(interrupts[0], new CodeInstruction(OpCodes.Ldloc, shouldObservation));
            // command.ShouldBeInterrupted becomes ObserveShouldBeInterrupted(command, out shouldObservation): the same
            // Boolean reaches native's branch, and the observation goes to this invocation's own local.
            Replace(code[should[0]], observeShouldBeInterrupted);
            code.Insert(should[0], new CodeInstruction(OpCodes.Ldloca, shouldObservation));
            return code;
        }

        internal static IEnumerable<CodeInstruction> AttackBan(IEnumerable<CodeInstruction> source, MethodInfo hook)
        {
            var code = source.ToList();
            var conditions = Sites(code, HasConditionToken);
            Require(conditions.Length == 1 && conditions[0] >= 1 && code[conditions[0] - 1].opcode == OpCodes.Ldc_I4_S &&
                Convert.ToInt32(code[conditions[0] - 1].operand) == (int)UnitCondition.CanNotAttack &&
                code[conditions[0] + 1].opcode == OpCodes.Ret && code[conditions[0] + 1].labels.Count == 0, "one returned HasCondition(CanNotAttack) term");
            code.Insert(conditions[0] + 1, new CodeInstruction(OpCodes.Call, hook));
            return code;
        }
    }
}
