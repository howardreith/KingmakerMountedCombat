using System;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UI.SettingsUI;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    // Pair-scoped actor records survive dismount/reselection. No live synthetic
    // TurnController, rider cooldown projection, selection mutation or turn reset.
    internal sealed class MountedMovementStateAdapter
    {
        private sealed class Allocation
        {
            internal object Controller;
            internal int Round;
            internal long RoundStart;
            internal bool Prepared;
            internal float MoveUsed;
            internal bool StandardUsed;
            internal MountedMovementState Movement = new MountedMovementState();
        }
        private readonly Dictionary<UnitEntityData, Allocation> allocations = new Dictionary<UnitEntityData, Allocation>();
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly PropertyInfo[] TurnMovementProperties = {
            Property("TimeMoved"), Property("TimeMovedInForceMode"), Property("TimeMovedByFiveFootStep"),
            Property("MetersMovedByFiveFootStep"), Property("ImmuneAttackOfOpportunityOnDisengage") };
        private static readonly FieldInfo AiStepField = Field("m_AIUsedFiveFootStep");
        private static readonly FieldInfo AutoStopField = Field("m_AutoStopAfterFirstMoveAction");
        private static readonly FieldInfo ForceModeField = typeof(UnitMovementAgent).GetField("m_IsInForceMode", Flags);

        internal bool Owns(UnitEntityData actor) => actor != null && allocations.ContainsKey(actor);

        private Allocation Get(UnitEntityData mount)
        {
            var controller = Game.Instance.TurnBasedCombatController;
            Allocation allocation;
            if (!allocations.TryGetValue(mount, out allocation) || allocation.Controller != controller ||
                allocation.Round != controller.RoundNumber || allocation.RoundStart != controller.RoundStartTime.Ticks)
            {
                allocation = new Allocation { Controller = controller, Round = controller.RoundNumber,
                    RoundStart = controller.RoundStartTime.Ticks };
                allocations[mount] = allocation;
            }
            return allocation;
        }

        internal void ObserveNativeMovement(TurnController turn, UnitEntityData mount)
        {
            if (turn == null || turn.Unit != mount || mount == null || !CombatController.IsInTurnBasedCombat()) return;
            var allocation = Get(mount);
            allocation.MoveUsed = Math.Max(allocation.MoveUsed, mount.CombatState.Cooldown.MoveAction);
            allocation.StandardUsed |= mount.CombatState.Cooldown.StandardAction > 0f;
            var state = allocation.Movement;
            state.TimeMoved = turn.TimeMoved;
            state.TimeForced = turn.TimeMovedInForceMode;
            state.TimeStepped = turn.TimeMovedByFiveFootStep;
            state.MetresStepped = turn.MetersMovedByFiveFootStep;
            state.StepImmune = turn.ImmuneAttackOfOpportunityOnDisengage;
            state.AiStep = (bool)AiStepField.GetValue(turn);
            state.AutoStopPending = (bool)AutoStopField.GetValue(turn);
        }

        internal void ObserveAction(UnitCommand command)
        {
            if (command?.Executor == null || !Owns(command.Executor) || !CombatController.IsInTurnBasedCombat()) return;
            var allocation = Get(command.Executor);
            allocation.StandardUsed |= command.Executor.CombatState.Cooldown.StandardAction > 0f;
            allocation.MoveUsed = Math.Max(allocation.MoveUsed, command.Executor.CombatState.Cooldown.MoveAction);
        }

        internal void Prepared(TurnController turn, UnitEntityData activeMount)
        {
            if (turn == null || !CombatController.IsInTurnBasedCombat() ||
                turn.Unit != activeMount && !Owns(turn.Unit)) return;
            var mount = turn.Unit;
            var allocation = Get(mount);
            // A native allocation remains authoritative. Only expenditure made
            // within this native epoch survives its later Prepare; no early refresh.
            mount.CombatState.Cooldown.MoveAction = Math.Max(mount.CombatState.Cooldown.MoveAction, allocation.MoveUsed);
            if (allocation.StandardUsed) mount.CombatState.Cooldown.StandardAction = Math.Max(6f, mount.CombatState.Cooldown.StandardAction);
            allocation.Prepared = true;
            allocation.MoveUsed = mount.CombatState.Cooldown.MoveAction;
            var state = allocation.Movement;
            TurnMovementProperties[0].SetValue(turn, state.TimeMoved, null);
            TurnMovementProperties[1].SetValue(turn, state.TimeForced, null);
            TurnMovementProperties[2].SetValue(turn, state.TimeStepped, null);
            TurnMovementProperties[3].SetValue(turn, state.MetresStepped, null);
            TurnMovementProperties[4].SetValue(turn, state.StepImmune, null);
            AiStepField.SetValue(turn, state.AiStep);
            AutoStopField.SetValue(turn, state.AutoStopPending);
        }

        internal string TickDelegated(TurnController inputTurn, UnitEntityData mount, ref float deltaTime)
        {
            var allocation = Get(mount);
            var cooldown = mount.CombatState.Cooldown;
            var before = cooldown.MoveAction;
            // Within a prepared allocation native time passing must not turn an
            // already-spent Standard into a second mounted movement conversion.
            var standardUsed = allocation.StandardUsed || mount.UsedStandardAction();
            var used = Math.Max(before, allocation.MoveUsed);
            float debit;
            var requested = deltaTime;
            deltaTime = allocation.Movement.Advance(deltaTime, mount.CurrentSpeedMps,
                TurnController.MetersOfFiveFootStep, used, standardUsed, mount.IsMoveActionRestricted(),
                inputTurn.EnabledFiveFootStep, inputTurn.EnabledSingleActionMove,
                (bool)ForceModeField.GetValue(mount.View.AgentASP), mount.View.AgentASP.NearTheEnd,
                SettingsRoot.Instance.AutoStopAfterFirstMoveAction.CurrentValue, out debit);
            cooldown.MoveAction = used + debit;
            allocation.MoveUsed = allocation.Prepared ? cooldown.MoveAction : allocation.MoveUsed + debit;
            // Before Prepare only the new debit belongs to this allocation; an
            // old native Standard is not converted into a guessed fresh action.
            if (allocation.Prepared) allocation.StandardUsed |= mount.CombatState.Cooldown.StandardAction > 0f;
            return "mountMove=" + before.ToString("R") + "->" + cooldown.MoveAction.ToString("R") +
                ";mountTime=" + allocation.Movement.TimeMoved.ToString("R") +
                ";mountStepMetres=" + allocation.Movement.MetresStepped.ToString("R") +
                ";nativePrepared=" + allocation.Prepared + ";round=" + allocation.Round +
                ";physicalDelta=" + requested.ToString("R") + "->" + deltaTime.ToString("R");
        }

        private static PropertyInfo Property(string name) => typeof(TurnController).GetProperty(name, Flags) ??
            throw new MissingMemberException(typeof(TurnController).FullName, name);
        private static FieldInfo Field(string name) => typeof(TurnController).GetField(name, Flags) ??
            throw new MissingFieldException(typeof(TurnController).FullName, name);
    }
}
