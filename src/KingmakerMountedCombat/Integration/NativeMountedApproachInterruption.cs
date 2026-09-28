using System;
using Kingmaker.Controllers.Clicks;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class NativeMountedControlService
    {
        // The pinned native handler reads Commands.Move, which returns only UnitMoveTo.
        // A registered Mount is a UnitUseAbility in that same slot. Capture its exact
        // identity before the native handler, then revalidate after any native callbacks.
        internal UnitUseAbility CaptureNativeMountApproachInterruption(UnitEntityView view)
        {
            if (disposed || !enabled || !registered || serializationSuspended || PointerController.SimulatingClick)
                return null;
            var actor = view?.EntityData;
            if (actor == null || !ReferenceEquals(actor.View, view)) return null;
            return ResolveUnactedNativeMount(actor, actor.Commands.GetCommand(UnitCommand.CommandType.Move));
        }

        internal UnitUseAbility ResolveUnactedNativeMount(UnitEntityData actor, UnitCommand currentMove)
        {
            var command = currentMove as UnitUseAbility;
            if (actor == null || command == null || command.Executor != actor || command.Type != UnitCommand.CommandType.Move ||
                command.IsStarted || command.IsActed || command.IsFinished || command.ExecutionProcess != null)
                return null;
            NativeRelationshipShell shell;
            if (!relationshipShells.TryGetValue(command, out shell) || shell == null ||
                shell.Kind != NativeMountedControlKind.MountCompanion || shell.ProcessBound || shell.Consumed || shell.Retired)
                return null;
            return command;
        }

        internal void CompleteNativeMountApproachInterruption(UnitEntityView view, UnitUseAbility command)
        {
            if (command == null || !ReferenceEquals(command, CaptureNativeMountApproachInterruption(view))) return;
            NativeRelationshipShell shell;
            if (!relationshipShells.TryGetValue(command, out shell)) return;
            // Retire before native OnEnded callbacks can re-enter. Interrupt owns Result,
            // IsFinished and the native end event; no acted flag, cooldown or ledger write.
            RetireShell(shell, "native-movement-interrupted");
            command.Interrupt();
            RecordShellLifecycle(NativeShellStage.ApproachInterrupted, shell.ControlIdentity,
                "native-movement-interrupted", "result=" + command.Result + ";acted=" + command.IsActed +
                ";finished=" + command.IsFinished);
        }
    }
}
