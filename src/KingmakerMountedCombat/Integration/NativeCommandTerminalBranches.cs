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
    // Every native call stays in place. Capture calls are inserted after a native value is computed and pass it through
    // unchanged, remembering it for the exact command of this tick; the terminal hooks call the same native method the
    // original did (ForceFinishForTurnBased, Interrupt), after recording on a pair command which branch ended it.
    // Recording never stands between native and its own end: it is contained.
    //
    // Installed 2.1.7b sites:
    // - TickCommandTurnBased (0600911D): the canAct value native tests before its first ForceFinishForTurnBased
    //   (CanAct && !HasCondition(Nauseated)), and that force-finish. The second force-finish (approach time) is untouched.
    // - TickCommand (0600911E): the value of get_ShouldBeInterrupted, the state-site Interrupt, the unstarted
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

        [ThreadStatic] private static UnitCommand turnBasedCommand;
        [ThreadStatic] private static bool turnBasedCanAct;
        [ThreadStatic] private static bool attackBanTerm;
        [ThreadStatic] private static UnitCommand stateCommand;
        [ThreadStatic] private static bool stateShouldBeInterrupted;
        [ThreadStatic] private static bool stateAttackBan;
        [ThreadStatic] private static UnitCommand runningCommand;
        [ThreadStatic] private static bool runningCanAct;

        internal static bool CaptureTurnBasedCanAct(bool canAct, UnitCommand command)
        {
            turnBasedCommand = command;
            turnBasedCanAct = canAct;
            return canAct;
        }

        internal static void TurnBasedForceFinish(UnitCommand command, UnitCommand.ResultType result)
        {
            var captured = command != null && ReferenceEquals(turnBasedCommand, command);
            var canAct = turnBasedCanAct;
            turnBasedCommand = null;
            Record(command, MountedNativeTerminalBranchPolicy.TurnBasedForceFinish(captured, canAct));
            command.ForceFinishForTurnBased(result);
        }

        internal static bool AttackBanTerm(bool value)
        {
            attackBanTerm = value;
            return value;
        }

        // Immediately before native's own get_ShouldBeInterrupted: a ban term from any earlier reading never counts.
        internal static void BeginShouldBeInterrupted()
        {
            attackBanTerm = false;
        }

        internal static bool CaptureShouldBeInterrupted(bool value, UnitCommand command)
        {
            stateCommand = command;
            stateShouldBeInterrupted = value;
            stateAttackBan = value && attackBanTerm;
            attackBanTerm = false;
            return value;
        }

        internal static void InterruptAtStateSite(UnitCommand command, bool raiseEvent)
        {
            var captured = command != null && ReferenceEquals(stateCommand, command);
            var branch = MountedNativeTerminalBranchPolicy.StateSite(captured, stateShouldBeInterrupted, stateAttackBan);
            stateCommand = null;
            Record(command, branch);
            command.Interrupt(raiseEvent);
        }

        internal static void InterruptAsSoonAsPossible(UnitCommand command, bool raiseEvent)
        {
            Record(command, MountedNativeTerminalBranch.InterruptAsSoonAsPossible);
            command.Interrupt(raiseEvent);
        }

        internal static bool CaptureRunningCanAct(bool value, UnitCommand command)
        {
            runningCommand = command;
            runningCanAct = value;
            return value;
        }

        internal static void InterruptAtRunningSite(UnitCommand command, bool raiseEvent)
        {
            var captured = command != null && ReferenceEquals(runningCommand, command);
            var branch = MountedNativeTerminalBranchPolicy.RunningSite(captured, runningCanAct);
            runningCommand = null;
            Record(command, branch);
            command.Interrupt(raiseEvent);
        }

        internal static void Reset()
        {
            turnBasedCommand = null;
            stateCommand = null;
            runningCommand = null;
            attackBanTerm = false;
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

        internal static IEnumerable<CodeInstruction> TurnBased(IEnumerable<CodeInstruction> source, MethodInfo capture, MethodInfo forceFinish)
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
            var insert = new[] { new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Call, capture) };
            insert[0].labels.AddRange(code[test].labels);
            code[test].labels.Clear();
            code.InsertRange(test, insert);
            Replace(code[site + insert.Length], forceFinish);
            return code;
        }

        internal static IEnumerable<CodeInstruction> Tick(IEnumerable<CodeInstruction> source, MethodInfo begin, MethodInfo captureShouldBeInterrupted,
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
            Replace(code[interrupts[0]], stateSite);
            Replace(code[interrupts[1]], asSoonAsPossible);
            Replace(code[interrupts[2]], runningSite);
            // Highest index first, so every earlier site keeps its index.
            code.InsertRange(canAct[0] + 1, new[] { new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Call, captureCanAct) });
            code.InsertRange(should[0] + 1, new[] { new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Call, captureShouldBeInterrupted) });
            code.Insert(should[0], new CodeInstruction(OpCodes.Call, begin));
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
