using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    // Exact commands produced by the one native preparation path retain their
    // actor's grant through condition-driven control loss and forced detachment.
    internal sealed partial class UnifiedMountedTurnCoordinator
    {
        private readonly Dictionary<UnitEntityData, UnitCommand> nativePreparationCommands =
            new Dictionary<UnitEntityData, UnitCommand>();
        private TurnController nativeConditionForfeitContext;

        internal bool AdmitNativePreparationCommand(UnitCommands commands, UnitCommand command)
        {
            if (!PairedLifecycleEnabled || activation == null || commands == null || command == null) return false;
            var actor = ReferenceEquals(commands, activation.Principal.Commands) ? activation.Principal :
                ReferenceEquals(commands, activation.Partner.Commands) ? activation.Partner : null;
            if (actor == null || !IsPreparingPairedActor(actor) ||
                command.IsIgnoreCooldown || !ReferenceEquals(actor.Get<UnitPartConfusion>()?.Cmd, command)) return false;
            nativePreparationCommands[actor] = command;
            logger.Info("Paired native preparation command: " + activation.Identity + ";actor=" + actor.UniqueId +
                ";command=" + command.GetType().FullName);
            return true;
        }

        private bool OwnsNativePreparationCommand(UnitEntityData actor, UnitCommand command, TurnController turn)
        {
            UnitCommand owned;
            return PairedLifecycleEnabled && activation != null && actor != null &&
                ReferenceEquals(turn, activation.Boundary) && !activation.Ending && !activation.Suspended &&
                activation.State(actor)?.Prepared == true && command != null && command.Executor == actor &&
                !command.IsIgnoreCooldown && !command.IsFinished &&
                nativePreparationCommands.TryGetValue(actor, out owned) && ReferenceEquals(owned, command) &&
                (ReferenceEquals(actor.Commands.GetCommand(command.Type), command) || actor.Commands.Queue.Contains(command));
        }

        private bool HasNativePreparationActivity(TurnController turn)
        {
            if (activation == null) return false;
            foreach (var item in nativePreparationCommands)
                if (OwnsNativePreparationCommand(item.Key, item.Value, turn) && item.Key.IsInState &&
                    item.Key.IsInCombat && item.Key.IsAbleToAct()) return true;
            return false;
        }

        private bool OwnsNativeConditionActorContext(UnitEntityData actor, TurnController turn)
        {
            if (!PairedLifecycleEnabled || activation == null || actor == null || !ReferenceEquals(turn, activation.Boundary)) return false;
            if (activation.IsPreparingActor(actor, turn)) return true;
            UnitCommand command;
            return nativePreparationCommands.TryGetValue(actor, out command) && OwnsNativePreparationCommand(actor, command, turn);
        }
        internal bool IsNativeConditionCommandActor(UnitEntityData actor, UnitCommand command) => actor.IsCurrentUnit() ||
            OwnsNativePreparationCommand(actor, command, Game.Instance?.TurnBasedCombatController?.CurrentTurn);

        internal void ForfeitNativeConditionActor(TurnController native, bool setCooldowns, UnitCommand command)
        {
            var actor = command?.Executor;
            if (!OwnsNativePreparationCommand(actor, command, native))
            { native.ForceToEnd(setCooldowns); return; }
            var context = actor == activation.Principal ? native : partnerContext;
            if (context == null || nativeConditionForfeitContext != null)
                throw new InvalidOperationException("Native condition actor completion has no unique granted context.");
            nativeConditionForfeitContext = context;
            try { context.ForceToEnd(setCooldowns); }
            finally { nativeConditionForfeitContext = null; }
        }

        // Called only at the unique native ForceToEnd -> ToEnd call. Returning
        // true consumes that phase transition while retaining all native costs.
        internal bool CompleteNativeConditionForfeit(TurnController context)
        {
            if (!ReferenceEquals(context, nativeConditionForfeitContext)) return false;
            ObservePairedCosts(context.Unit);
            activation.EndActor(context.Unit);
            logger.Info("Paired native condition actor ended: " + activation.Identity + ";actor=" + context.Unit.UniqueId);
            return true;
        }

        private void InterruptNativePreparationCommands()
        {
            foreach (var command in nativePreparationCommands.Values)
                if (command != null && !command.IsFinished) command.Interrupt();
        }

        internal bool TryMoveNativePreparationActor(UnitMovementAgent agent, ref float deltaTime, out bool result)
        {
            result = false;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var actor = agent?.Unit?.EntityData;
            UnitCommand command;
            if (actor == null || actor != activation?.Partner || partnerContext == null || turn == null || !turn.IsActing ||
                !nativePreparationCommands.TryGetValue(actor, out command) || !command.IsMoveUnit ||
                !OwnsNativePreparationCommand(actor, command, turn) || activation.State(actor).Ended) return false;
            if (!agent.IsReallyMoving || agent.Unit.IsCommandsPreventMovement ||
                (agent.Unit.AnimationManager != null && agent.Unit.AnimationManager.IsPreventingMovement)) return false;
            LastMovementObservation = movementState.TickDelegated(partnerContext, actor, ref deltaTime);
            movementState.CopyGrantedMovementToContext(partnerContext);
            ObservePairedCosts(actor);
            result = deltaTime > 0f;
            // Native denial/completion still owns exhaustion and forced paths.
            return result;
        }
    }
}
