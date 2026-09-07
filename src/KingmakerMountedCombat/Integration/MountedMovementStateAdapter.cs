using System;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Controllers.Combat;
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
            internal string GrantIdentity;
            internal bool Prepared;
            internal float MoveUsed;
            internal bool StandardUsed;
            internal MountedMovementState Movement = new MountedMovementState();
        }
        private readonly Dictionary<UnitEntityData, Allocation> allocations = new Dictionary<UnitEntityData, Allocation>();
        private readonly ActorAllocationLifetime<UnitEntityData, Allocation> lifetime;
        private TurnController preparingTurn;

        internal MountedMovementStateAdapter()
        {
            lifetime = new ActorAllocationLifetime<UnitEntityData, Allocation>(allocations);
        }

        internal int TrackedActorCount => allocations.Count;

        internal void MaintainLifetimes()
        {
            var player = Game.Instance?.Player;
            if (player == null) return;
            if (lifetime.SynchronizeSession(player, player.GameId) != 0) preparingTurn = null;
            if (player.IsInCombat || allocations.Count == 0) return;
            var settled = new List<UnitEntityData>();
            foreach (var entry in allocations)
            {
                var actor = entry.Key;
                var cooldown = actor.CombatState?.Cooldown;
                if (cooldown == null || actor.Commands == null) continue;
                // An explicit paired grant ends with the encounter, even if
                // actor removal already disposed its activation. Native debt
                // remains on the actor and continues its normal RT recovery.
                // Dismount, selection and mode conversion are not this boundary.
                if (!actor.IsInCombat && actor.Commands.Empty && (entry.Value.GrantIdentity != null || cooldown.StandardAction <= 0f &&
                    cooldown.MoveAction <= 0f && cooldown.SwiftAction <= 0f &&
                    (entry.Value.Prepared || entry.Value.MoveUsed <= 0f)))
                    settled.Add(actor);
            }
            lifetime.RetireSettledActors(settled);
        }

        internal void RetireDestroyedActor(UnitEntityData actor)
        {
            if (actor == null) return;
            lifetime.RetireDestroyedActor(actor);
            if (preparingTurn?.Unit == actor) preparingTurn = null;
        }

        internal void RetireCompletedEncounterActor(UnitEntityData actor)
        {
            if (actor != null && !(Game.Instance?.Player?.IsInCombat ?? true) &&
                !actor.IsInCombat && actor.Commands.Empty) lifetime.RetireDestroyedActor(actor);
        }

        internal void Clear()
        {
            preparingTurn = null;
            lifetime.Clear();
        }

        internal void BeginPreparation(TurnController turn, UnitEntityData activeMount)
        {
            MaintainLifetimes();
            preparingTurn = turn != null && CombatController.IsInTurnBasedCombat() &&
                (turn.Unit == activeMount || Owns(turn.Unit)) ? turn : null;
        }

        internal void BeginGrantedPreparation(TurnController turn, string identity)
        {
            if (turn == null) return;
            MaintainLifetimes();
            var controller = Game.Instance.TurnBasedCombatController;
            allocations[turn.Unit] = new Allocation { Controller = controller, Round = controller.RoundNumber,
                RoundStart = controller.RoundStartTime.Ticks, GrantIdentity = identity };
            preparingTurn = turn;
        }

        internal void BeforeNativeRoundState(UnitCombatState state)
        {
            var turn = preparingTurn;
            if (turn == null || state?.Unit != turn.Unit ||
                Game.Instance?.TurnBasedCombatController?.CurrentTurn != turn && Get(turn.Unit).GrantIdentity == null ||
                !CombatController.IsInTurnBasedCombat()) return;
            // Exact native order: Clear -> reapply acting-command costs ->
            // reaction fields -> OnNewRound -> round/AI/fact/readiness callbacks.
            // Reconcile here, before the callbacks, without replaying any of them.
            Prepared(turn, turn.Unit);
        }

        internal void EndPreparation(TurnController turn)
        {
            if (ReferenceEquals(preparingTurn, turn)) preparingTurn = null;
        }
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly PropertyInfo[] TurnMovementProperties = {
            Property("TimeMoved"), Property("TimeMovedInForceMode"), Property("TimeMovedByFiveFootStep"),
            Property("MetersMovedByFiveFootStep"), Property("ImmuneAttackOfOpportunityOnDisengage") };
        private static readonly FieldInfo AiStepField = Field("m_AIUsedFiveFootStep");
        private static readonly FieldInfo AutoStopField = Field("m_AutoStopAfterFirstMoveAction");
        private static readonly FieldInfo ForceModeField = AgentField("m_IsInForceMode", 0x040011AE, typeof(bool));
        private static readonly FieldInfo SavedMinSpeedField = AgentField("m_SavedMinSpeed", 0x040011B6, typeof(float?));
        private static readonly FieldInfo MinSpeedField = AgentField("m_MinSpeed", 0x040011CB, typeof(float));
        private static readonly FieldInfo WarmupField = AgentField("m_WarmupTime", 0x04001191, typeof(float));
        private static readonly FieldInfo SlowdownField = AgentField("m_SlowDownTime", 0x040011AC, typeof(float));

        internal bool Owns(UnitEntityData actor) => actor != null && allocations.ContainsKey(actor);

        internal void ResetTransientStepImmunity(TurnController context)
        {
            if (context == null) return;
            Allocation allocation;
            if (allocations.TryGetValue(context.Unit, out allocation)) allocation.Movement.StepImmune = false;
            TurnMovementProperties[4].SetValue(context, false, null);
        }

        private Allocation Get(UnitEntityData mount)
        {
            MaintainLifetimes();
            var controller = Game.Instance.TurnBasedCombatController;
            Allocation allocation;
            if (!allocations.TryGetValue(mount, out allocation) || allocation.Controller != controller ||
                allocation.GrantIdentity == null &&
                (allocation.Round != controller.RoundNumber || allocation.RoundStart != controller.RoundStartTime.Ticks))
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
            if (allocation.GrantIdentity != null)
            {
                // Native Clear and acting-command reapplication have run once at
                // this explicit grant. Previous grant floors must not be restored.
                allocation.MoveUsed = mount.CombatState.Cooldown.MoveAction;
                allocation.StandardUsed = mount.CombatState.Cooldown.StandardAction > 0f;
            }
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
            var agent = mount.View.AgentASP;
            var forced = (bool)ForceModeField.GetValue(agent);
            deltaTime = allocation.Movement.Advance(deltaTime, mount.CurrentSpeedMps,
                TurnController.MetersOfFiveFootStep, used, standardUsed, mount.IsMoveActionRestricted(),
                inputTurn.EnabledFiveFootStep, inputTurn.EnabledSingleActionMove,
                forced, agent.NearTheEnd,
                SettingsRoot.Instance.AutoStopAfterFirstMoveAction.CurrentValue, out debit);
            cooldown.MoveAction = used + debit;
            allocation.MoveUsed = allocation.Prepared ? cooldown.MoveAction : allocation.MoveUsed + debit;
            // Before Prepare only the new debit belongs to this allocation; an
            // old native Standard is not converted into a guessed fresh action.
            if (allocation.Prepared) allocation.StandardUsed |= mount.CombatState.Cooldown.StandardAction > 0f;
            if (deltaTime > 0f && !forced)
            {
                // Match the native allowed-movement pacing boundary. The next
                // native TickMovement restores the saved minimum speed. Skipping
                // this adds warm-up/slowdown action time to delegated short paths.
                SavedMinSpeedField.SetValue(agent, MinSpeedField.GetValue(agent));
                MinSpeedField.SetValue(agent, 1f);
                WarmupField.SetValue(agent, 0f);
                SlowdownField.SetValue(agent, 0f);
            }
            return "mountMove=" + before.ToString("R") + "->" + cooldown.MoveAction.ToString("R") +
                ";mountTime=" + allocation.Movement.TimeMoved.ToString("R") +
                ";mountStepMetres=" + allocation.Movement.MetresStepped.ToString("R") +
                ";nativePrepared=" + allocation.Prepared + ";round=" + allocation.Round +
                ";physicalDelta=" + requested.ToString("R") + "->" + deltaTime.ToString("R");
        }

        internal void CopyGrantedMovementToContext(TurnController turn)
        {
            if (turn == null || !allocations.ContainsKey(turn.Unit)) return;
            var state = allocations[turn.Unit].Movement;
            TurnMovementProperties[0].SetValue(turn, state.TimeMoved, null);
            TurnMovementProperties[1].SetValue(turn, state.TimeForced, null);
            TurnMovementProperties[2].SetValue(turn, state.TimeStepped, null);
            TurnMovementProperties[3].SetValue(turn, state.MetresStepped, null);
            TurnMovementProperties[4].SetValue(turn, state.StepImmune, null);
            AiStepField.SetValue(turn, state.AiStep);
            AutoStopField.SetValue(turn, state.AutoStopPending);
        }

        internal bool HasGrantedMovement(UnitEntityData actor, bool step, bool singleMove)
        {
            Allocation allocation;
            if (actor == null || !allocations.TryGetValue(actor, out allocation) || !allocation.Prepared) return false;
            return allocation.Movement.Remaining(actor.CurrentSpeedMps, TurnController.MetersOfFiveFootStep,
                Math.Max(allocation.MoveUsed, actor.CombatState.Cooldown.MoveAction),
                allocation.StandardUsed || actor.UsedStandardAction(), actor.IsMoveActionRestricted(), step, singleMove) > 0f;
        }

        private static PropertyInfo Property(string name) => typeof(TurnController).GetProperty(name, Flags) ??
            throw new MissingMemberException(typeof(TurnController).FullName, name);
        private static FieldInfo Field(string name) => typeof(TurnController).GetField(name, Flags) ??
            throw new MissingFieldException(typeof(TurnController).FullName, name);
        private static FieldInfo AgentField(string name, int token, Type fieldType)
        {
            var field = typeof(UnitMovementAgent).GetField(name, Flags);
            if (field == null || field.MetadataToken != token || field.FieldType != fieldType)
                throw new MissingFieldException(typeof(UnitMovementAgent).FullName, name);
            return field;
        }
    }
}
