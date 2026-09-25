using System;
using System.Collections.Generic;
using System.Reflection;
using Kingmaker.EntitySystem.Entities;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    // Reconstruct a native context without Start, Prepare, ForceToEnd or End.
    // The constructor owns subscriptions; every non-current context has an
    // explicit disposer in the paired coordinator.
    internal static class NativeTurnPersistence
    {
        private static FieldInfo F(string name, int token, Type type) =>
            NativeCombatActorPersistence.Field(typeof(TurnController), name, token, type);
        private static FieldInfo E(string name, int token, string enumName) =>
            F(name, token, typeof(TurnController).GetNestedType(enumName, BindingFlags.Public | BindingFlags.NonPublic));
        private static readonly FieldInfo CanGetUp = F("UnitCanGetUpOnCommand", 0x04000670,
            typeof(TurnController).GetField("UnitCanGetUpOnCommand").FieldType);
        private static readonly PropertyInfo ReactiveValue = CanGetUp.FieldType.GetProperty("Value");
        private static readonly FieldInfo Status = F("<Status>k__BackingField", 0x04000672, typeof(TurnController.TurnStatus));
        private static readonly FieldInfo AiStep = F("m_AIUsedFiveFootStep", 0x04000667, typeof(bool));
        private static readonly FieldInfo Delay = F("m_DelayTarget", 0x04000668, typeof(UnitEntityData));
        private static readonly FieldInfo Surprise = F("m_ActingInSurpriseRound", 0x0400066D, typeof(bool));
        private static readonly FieldInfo WaitedEnd = F("<TimeWaitedToEndTurn>k__BackingField", 0x04000677, typeof(float));
        private static readonly FieldInfo Moved = F("<TimeMoved>k__BackingField", 0x04000679, typeof(float));
        private static readonly FieldInfo Forced = F("<TimeMovedInForceMode>k__BackingField", 0x0400067A, typeof(float));
        private static readonly FieldInfo Stepped = F("<TimeMovedByFiveFootStep>k__BackingField", 0x0400067B, typeof(float));
        private static readonly FieldInfo Metres = F("<MetersMovedByFiveFootStep>k__BackingField", 0x0400067C, typeof(float));
        private static readonly FieldInfo Attack = E("m_AttackMode", 0x0400067D, "AttackMode");
        private static readonly FieldInfo Limit = E("m_MovementLimit", 0x0400067E, "MovementLimit");
        private static readonly FieldInfo Ground = E("m_GroundMovementLimit", 0x0400067F, "MovementLimit");
        private static readonly FieldInfo Modify = F("m_ModifyMovementCurrentValue", 0x04000680, typeof(bool));
        private static readonly FieldInfo Requested = F("m_ModifyMovementRequestValue", 0x04000681, typeof(bool));
        private static readonly FieldInfo Smart = F("m_CurrentSmartCursorIndex", 0x04000683, typeof(int));
        private static readonly FieldInfo Manual = F("m_SmartCursorChangedManually", 0x04000684, typeof(bool));
        private static readonly FieldInfo GetUp = F("m_WaitForGetUp", 0x04000685, typeof(bool));
        private static readonly FieldInfo Prone = F("m_WasProne", 0x04000686, typeof(bool));
        private static readonly FieldInfo AutoStop = F("m_AutoStopAfterFirstMoveAction", 0x04000687, typeof(bool));
        private static readonly FieldInfo StepImmune = F("<ImmuneAttackOfOpportunityOnDisengage>k__BackingField", 0x04000688, typeof(bool));

        internal static SavedTurnContext Capture(TurnController turn)
        {
            if (turn == null) return null;
            return new SavedTurnContext
            {
                ActorId = turn.Unit.UniqueId, Status = (int)turn.Status,
                Movement = new SavedMovementValues {
                    TimeMoved = turn.TimeMoved, TimeForced = turn.TimeMovedInForceMode,
                    TimeStepped = turn.TimeMovedByFiveFootStep, MetresStepped = turn.MetersMovedByFiveFootStep,
                    StepImmune = turn.ImmuneAttackOfOpportunityOnDisengage,
                    AiStep = (bool)AiStep.GetValue(turn), AutoStop = (bool)AutoStop.GetValue(turn) },
                AttackMode = Convert.ToInt32(Attack.GetValue(turn)), MovementLimit = Convert.ToInt32(Limit.GetValue(turn)),
                GroundLimit = Convert.ToInt32(Ground.GetValue(turn)), ModifyCurrent = (bool)Modify.GetValue(turn),
                ModifyRequested = (bool)Requested.GetValue(turn), SmartIndex = (int)Smart.GetValue(turn),
                SmartManual = (bool)Manual.GetValue(turn), ActingSurprise = (bool)Surprise.GetValue(turn),
                WaitingGetUp = (bool)GetUp.GetValue(turn), WasProne = (bool)Prone.GetValue(turn),
                CanGetUp = (bool)ReactiveValue.GetValue(CanGetUp.GetValue(turn), null), WaitedToEnd = turn.TimeWaitedToEndTurn,
                DelayTarget = ((UnitEntityData)Delay.GetValue(turn))?.UniqueId
            };
        }

        internal static TurnController Restore(SavedTurnContext saved, IDictionary<string, UnitEntityData> actors)
        {
            if (saved == null) return null;
            var turn = new TurnController(actors[saved.ActorId]);
            try
            {
                Status.SetValue(turn, (TurnController.TurnStatus)saved.Status);
                Moved.SetValue(turn, saved.Movement.TimeMoved); Forced.SetValue(turn, saved.Movement.TimeForced);
                Stepped.SetValue(turn, saved.Movement.TimeStepped); Metres.SetValue(turn, saved.Movement.MetresStepped);
                StepImmune.SetValue(turn, saved.Movement.StepImmune);
                AiStep.SetValue(turn, saved.Movement.AiStep); AutoStop.SetValue(turn, saved.Movement.AutoStop);
                Attack.SetValue(turn, Enum.ToObject(Attack.FieldType, saved.AttackMode));
                Limit.SetValue(turn, Enum.ToObject(Limit.FieldType, saved.MovementLimit));
                Ground.SetValue(turn, Enum.ToObject(Ground.FieldType, saved.GroundLimit));
                Modify.SetValue(turn, saved.ModifyCurrent); Requested.SetValue(turn, saved.ModifyRequested);
                Smart.SetValue(turn, saved.SmartIndex); Manual.SetValue(turn, saved.SmartManual);
                Surprise.SetValue(turn, saved.ActingSurprise); GetUp.SetValue(turn, saved.WaitingGetUp);
                Prone.SetValue(turn, saved.WasProne); WaitedEnd.SetValue(turn, saved.WaitedToEnd);
                Delay.SetValue(turn, saved.DelayTarget == null ? null : actors[saved.DelayTarget]);
                ReactiveValue.SetValue(CanGetUp.GetValue(turn), saved.CanGetUp, null);
                return turn;
            }
            catch { turn.Dispose(); throw; }
        }
    }
}
