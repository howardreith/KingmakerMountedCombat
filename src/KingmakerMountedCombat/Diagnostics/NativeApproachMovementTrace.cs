using System;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    // Scoped observation of the native ref-delta callback. No native state is written.
    internal sealed partial class NativeActorAllocationTrace
    {
        private sealed class ApproachMovementCall
        {
            internal NativeActorAllocationTrace Owner;
            internal TurnController Turn;
            internal UnitUseAbility Command;
            internal bool Force;
        }
        private void RecordApproachMovement(string boundary, ApproachMovementCall call, float delta)
        {
            if (!ReferenceEquals(active, this)) return;
            var offset = events.Count;
            Record(boundary, call.Turn.Unit, call.Command);
            if (events.Count != offset + 1) return;
            var value = events[offset] as JObject;
            if (value?["state"] == null) return;
            value["movementTurn"] = Id(call.Turn);
            value["slotCommand"] = Id(call.Turn.Unit.Commands.GetCommand(UnitCommand.CommandType.Move));
            value["force"] = call.Force;
            value["fiveFootStep"] = call.Turn.EnabledFiveFootStep;
            value["delta"] = delta;
        }
        private static partial class Hooks
        {
            internal static void ApproachMovementBefore(TurnController __instance, float deltaTime, bool isInForceMode, out ApproachMovementCall __state)
            {
                __state = null;
                var trace = active;
                if (trace == null || !trace.ObserveReactionResources || !trace.ObservesReactionActor(__instance.Unit)) return;
                try
                {
                    var command = __instance.Unit.Commands.GetCommand(UnitCommand.CommandType.Move) as UnitUseAbility;
                    if (command == null || command.Executor != __instance.Unit || command.IsStarted || command.IsActed || command.IsFinished ||
                        command.Spell?.Blueprint?.AssetGuid != "f053faad986631688defa003cd7bda0e") return;
                    __state = new ApproachMovementCall { Owner = trace, Turn = __instance, Command = command, Force = isInForceMode };
                    trace.RecordApproachMovement("approach-movement-before", __state, deltaTime);
                }
                catch (Exception exception) { trace.observationErrors++; trace.events.Add(new JObject { ["boundary"] = "approach-movement-before", ["observationError"] = exception.ToString() }); }
            }
            internal static void ApproachMovementAfter(TurnController __instance, float deltaTime, ApproachMovementCall __state)
            {
                if (__state == null || !ReferenceEquals(active, __state.Owner)) return;
                try
                {
                    if (!ReferenceEquals(__instance, __state.Turn)) throw new InvalidOperationException("Native movement callback changed its TurnController.");
                    __state.Owner.RecordApproachMovement("approach-movement-after", __state, deltaTime);
                }
                catch (Exception exception) { __state.Owner.observationErrors++; __state.Owner.events.Add(new JObject { ["boundary"] = "approach-movement-after", ["observationError"] = exception.ToString() }); }
            }
        }
    }
}