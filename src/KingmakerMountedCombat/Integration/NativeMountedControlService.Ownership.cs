using System;
using System.Collections.Generic;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class NativeMountedControlService
    {
        private GameMountedRelationshipService ResolveControlBoundary(UnitEntityData actor, bool admission)
        {
            var world = relationship.World;
            if (world == null) return admission ? relationship : relationship.ResolveActorOwner(actor, true);
            return world.ForActor(actor)?.Relationship ?? (admission ? world.GetOrCreateAdmission(actor, actor?.Descriptor?.Pet) : null);
        }

        private void VisitInputOwners(Action<MountedCombatController> action)
        {
            if (relationship.World == null) { action(combat); return; }
            // Each controller checks whether its exact actor is selected. Selection
            // may cancel current intent, but never changes admitted command ownership.
            relationship.World.Visit(pair => action(pair.Combat));
        }

        private readonly List<WeakReference<UnitUseAbility>> admittedControlCommands = new List<WeakReference<UnitUseAbility>>();
        internal bool HasUnsettledShellFor(GameMountedRelationshipService boundary)
        {
            var unsettled = false;
            for (var index = admittedControlCommands.Count - 1; index >= 0; index--)
            {
                UnitUseAbility command;
                if (!admittedControlCommands[index].TryGetTarget(out command))
                { admittedControlCommands.RemoveAt(index); continue; }
                NativeRelationshipShell shell;
                NativeIntentShell intent;
                // Consuming/refusing delivery never ends the native command or
                // process. Exact actor custody lasts through their final callbacks.
                var live = !command.IsFinished || command.ExecutionProcess != null && !command.ExecutionProcess.IsEnded;
                if (relationshipShells.TryGetValue(command, out shell))
                    live = live && ReferenceEquals(shell.PairRequest?.Boundary, boundary);
                else if (intentShells.TryGetValue(command, out intent))
                    live = live && ReferenceEquals(intent.Request.Boundary, boundary);
                else live = false;
                if (live) unsettled = true;
                // A different boundary remains indexed until it settles; do not
                // erase its custody while probing a ready neighbour.
                if (command.IsFinished && (command.ExecutionProcess == null || command.ExecutionProcess.IsEnded))
                    admittedControlCommands.RemoveAt(index);
            }
            return unsettled;
        }

        internal void RetireWorldControlBindings()
        {
            foreach (var reference in admittedControlCommands)
            {
                UnitUseAbility command;
                if (!reference.TryGetTarget(out command)) continue;
                NativeRelationshipShell relationshipShell;
                if (relationshipShells.TryGetValue(command, out relationshipShell))
                { relationshipShell.Retired = true; relationshipShell.RetiredReason = "world-discarded"; }
                NativeIntentShell intent;
                if (intentShells.TryGetValue(command, out intent)) intent.Poisoned = true;
            }
            admittedControlCommands.Clear();
            awaitingActivations.Clear(); pendingActivation = null;
        }

        private sealed class NativeIntentShell
        {
            internal MountedPairRequest<UnitEntityData, GameMountedRelationshipService> Request;
            internal NativeMountedControlKind Kind;
            internal UnitUseAbility Command;
            internal bool Consumed, Poisoned;
        }
        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<UnitUseAbility, NativeIntentShell> intentShells =
            new System.Runtime.CompilerServices.ConditionalWeakTable<UnitUseAbility, NativeIntentShell>();
        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<AbilityExecutionContext, NativeIntentShell> intentContexts =
            new System.Runtime.CompilerServices.ConditionalWeakTable<AbilityExecutionContext, NativeIntentShell>();

        private bool CaptureNativeIntent(UnitUseAbility command, NativeMountedControlKind kind, GameMountedRelationshipService boundary)
        {
            if (kind != NativeMountedControlKind.RiderPrimary && kind != NativeMountedControlKind.MountPrimary &&
                kind != NativeMountedControlKind.MountedCharge) return false;
            if (command?.Executor == null || command.Spell?.Caster?.Unit != command.Executor ||
                disposed || !enabled || !registered || serializationSuspended) return false;
            NativeIntentShell existing;
            if (intentShells.TryGetValue(command, out existing)) return !existing.Consumed && !existing.Poisoned;
            if (relationship.World?.AdmissionFenced() == true || !boundary.IsExactActivePairUnit(command.Executor)) return false;
            var request = boundary.CaptureNativeRequest(boundary.Rider, boundary.Mount, true);
            if (request == null) return false;
            intentShells.Add(command, new NativeIntentShell { Request = request, Kind = kind, Command = command });
            admittedControlCommands.Add(new WeakReference<UnitUseAbility>(command));
            return true;
        }

        private void BindNativeIntentProcess(UnitUseAbility command)
        {
            NativeIntentShell shell, bound;
            var context = command?.ExecutionProcess?.Context;
            if (context == null || !intentShells.TryGetValue(command, out shell)) return;
            if (intentContexts.TryGetValue(context, out bound))
            {
                if (!ReferenceEquals(shell, bound)) { shell.Poisoned = true; bound.Poisoned = true; }
                return;
            }
            intentContexts.Add(context, shell);
        }

        private NativeIntentShell ResolveNativeIntent(NativeMountedControlKind kind, UnitEntityData caster,
            UnitEntityData target, AbilityExecutionContext context)
        {
            if (context == null || disposed || !enabled || serializationSuspended) return null;
            NativeIntentShell bound = null;
            intentContexts.TryGetValue(context, out bound);
            // During synchronous OnAction, the postfix has not run yet. Only the
            // exact command whose process created this context can supply it.
            NativeIntentShell synchronous = null;
            if (caster?.Commands != null)
                foreach (var command in caster.Commands.Raw)
                {
                    var ability = command as UnitUseAbility;
                    if (ability?.ExecutionProcess?.Context != context) continue;
                    NativeIntentShell candidate;
                    if (!intentShells.TryGetValue(ability, out candidate)) continue;
                    if (synchronous != null && !ReferenceEquals(synchronous, candidate))
                    { synchronous.Poisoned = true; candidate.Poisoned = true; return null; }
                    synchronous = candidate;
                }
            if (bound != null && synchronous != null && !ReferenceEquals(bound, synchronous))
            { bound.Poisoned = true; synchronous.Poisoned = true; return null; }
            var shell = bound ?? synchronous;
            var exact = shell?.Command;
            GameMountedRelationshipService owner;
            if (shell == null || shell.Poisoned || shell.Consumed || shell.Kind != kind ||
                exact == null || !ReferenceEquals(exact.ExecutionProcess?.Context, context) ||
                !ReferenceEquals(exact.Executor, caster) || !ReferenceEquals(exact.Spell?.Caster?.Unit, caster) ||
                !ReferenceEquals(exact.Target?.Unit, target) ||
                !shell.Request.Boundary.ResolveNativeRequest(shell.Request, out owner) || !owner.IsExactActivePairUnit(caster)) return null;
            return shell;
        }
    }
}
