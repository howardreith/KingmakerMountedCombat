using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony12;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Known issues Sprint 6, review of 2ae4cc8b (finding 4). The public UnitCommands.Run(UnitCommand) (060026B2) is one call
    // of the internal Run(UnitCommand, bool, bool) (060026B3). A transpiler replaces exactly that call with ScopedRun, which
    // opens a token for this Run, calls the internal Run (with its own patches) and closes the same token in a finally.
    // Nothing re-resolves pair ownership, so a scene handoff, dismount, transfer or disposal during the Run cannot leave a
    // scope open; an exception closes it; nested Runs stack.
    internal static class MountedNativeRunScope
    {
        private static readonly MountedNativeRunScopes<UnitCommands, UnitCommand> scopes = new MountedNativeRunScopes<UnitCommands, UnitCommand>();

        private static readonly MethodInfo InternalRun = typeof(UnitCommands).GetMethod("Run", BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(UnitCommand), typeof(bool), typeof(bool) }, null);

        private static readonly Action<UnitCommands, UnitCommand, bool, bool> NativeInternalRun = InternalRun == null ? null :
            (Action<UnitCommands, UnitCommand, bool, bool>)Delegate.CreateDelegate(typeof(Action<UnitCommands, UnitCommand, bool, bool>), null, InternalRun);

        internal static int Depth => scopes.Depth;

        internal static void ScopedRun(UnitCommands commands, UnitCommand command, bool fromQueue, bool doNotClearQueue)
        {
            var token = scopes.Open(commands, command);
            try { NativeInternalRun(commands, command, fromQueue, doNotClearQueue); }
            finally { scopes.Close(token); }
        }

        internal static bool EndedInsideReplacingRun(UnitCommands container, UnitCommand ended) => scopes.EndedInsideReplacingRun(container, ended);

        internal static void Reset() => scopes.Reset();

        internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> source, MethodInfo hook)
        {
            var code = source.ToList();
            var calls = Enumerable.Range(0, code.Count).Where(i => (code[i].opcode == OpCodes.Call || code[i].opcode == OpCodes.Callvirt) &&
                Equals(code[i].operand, InternalRun)).ToArray();
            if (InternalRun == null || calls.Length != 1)
                throw new InvalidOperationException("Native Run scope structural contract mismatch: one internal Run call expected.");
            code[calls[0]].opcode = OpCodes.Call;
            code[calls[0]].operand = hook;
            return code;
        }
    }
}
