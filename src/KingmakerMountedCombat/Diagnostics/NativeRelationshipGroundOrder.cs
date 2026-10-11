using System;
using System.Linq;
using Kingmaker;
using Kingmaker.UnitLogic.Commands;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class NativeRelationshipCommandProbe
    {
        private UnitMoveTo geometryGroundOrder;
        private JObject geometryGroundAdmission, geometryGroundAtTerminal;
        internal bool ApproachObserved => samples.Any(sample => sample.Boundary == "approach-start");

        // An explicit diagnostic declaration; the ordinary positive/compensation paths
        // never call this. The external positive validator rejects this extra shape.
        internal void DeclareGeometryGroundOrder(UnitMoveTo command)
        {
            if (geometryGroundOrder != null || traceEnd >= 0 || Command == null ||
                !ApproachObserved || Command.IsActed || Command.IsFinished ||
                CombatController.IsInTurnBasedCombat() || !rider.IsInCombat || !mount.IsInCombat ||
                command == null || command.GetType() != typeof(UnitMoveTo) || command.Executor != mount ||
                !command.CreatedByPlayer || !command.IsIgnoreCooldown || command.IsActed || command.IsFinished ||
                Id(command) == Id(Command))
                throw new InvalidOperationException("Geometry change requires one exact unacted RT Horse ground command during Mount approach.");
            geometryGroundOrder = command;
            geometryGroundAdmission = CaptureGeometryGroundOrder();
            geometryGroundAdmission["frame"] = Time.frameCount;
            geometryGroundAdmission["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks;
            geometryGroundAdmission["allocationSequence"] = trace.EventCount;
            geometryGroundAdmission["mountCommandObject"] = Id(Command);
            geometryGroundAdmission["mountActed"] = Command.IsActed;
            geometryGroundAdmission["mountFinished"] = Command.IsFinished;
        }

        private JObject CaptureGeometryGroundOrder() => geometryGroundOrder == null ? null : new JObject
        {
            ["commandObject"] = Id(geometryGroundOrder), ["actorId"] = geometryGroundOrder.Executor?.UniqueId,
            ["commandClass"] = geometryGroundOrder.GetType().FullName,
            ["commandType"] = geometryGroundOrder.Type.ToString(),
            ["createdByPlayer"] = geometryGroundOrder.CreatedByPlayer,
            ["ignoreCooldown"] = geometryGroundOrder.IsIgnoreCooldown,
            ["acted"] = geometryGroundOrder.IsActed, ["finished"] = geometryGroundOrder.IsFinished,
            ["result"] = geometryGroundOrder.Result.ToString()
        };

        private bool IsGeometryGroundCost(JObject value) => geometryGroundOrder != null &&
            (int?)value["command"] == Id(geometryGroundOrder);

        private JObject EvaluateGeometryGroundCosts(JArray events, bool inCombat, bool turnBased)
        {
            if (geometryGroundOrder == null) return null;
            var costs = events.OfType<JObject>().Where(value => IsGeometryGroundCost(value) &&
                new[] { "cost-before", "cost-after", "actor-cost-before", "actor-cost-after" }.Contains((string)value["boundary"])).ToArray();
            var before = costs.Where(value => (string)value["boundary"] == "cost-before").ToArray();
            var after = costs.Where(value => (string)value["boundary"] == "cost-after").ToArray();
            var expected = (bool?)geometryGroundAtTerminal?["acted"] == true ? 1 : 0;
            var pass = inCombat && !turnBased && geometryGroundAtTerminal != null &&
                before.Length == expected && after.Length == expected && costs.Length == expected * 2 &&
                costs.All(value => (string)value["state"]?["actor"] == mount.UniqueId &&
                    (string)value["commandActor"] == mount.UniqueId &&
                    (string)value["commandType"] == typeof(UnitMoveTo).FullName &&
                    (string)value["actionType"] == "Move" && (bool?)value["ignoreCooldown"] == true &&
                    (bool?)value["acted"] == true && (bool?)value["state"]?["inCombat"] == true);
            if (expected == 1 && before.Length == 1 && after.Length == 1)
            {
                pass &= (int)after[0]["sequence"] == (int)before[0]["sequence"] + 1;
                foreach (var field in new[] { "standard", "move", "swift", "initiativeCooldown", "initiativeOrder",
                    "reactionCooldown", "reactions", "reactionsPerRound" })
                    pass &= JToken.DeepEquals(before[0]["state"][field], after[0]["state"][field]);
            }
            return new JObject
            {
                ["contract"] = "declared-rt-unit-move-to-native-no-write",
                ["pass"] = pass, ["commandObject"] = Id(geometryGroundOrder),
                ["beforeCount"] = before.Length, ["afterCount"] = after.Length,
                ["expectedCountAtMountTerminal"] = expected
            };
        }

        private JObject DescribeGeometryGroundOrder() => new JObject
        {
            ["contract"] = "declared-rt-unit-move-to-native-no-write",
            ["admission"] = geometryGroundAdmission?.DeepClone(),
            ["atMountTerminal"] = geometryGroundAtTerminal?.DeepClone()
        };
    }
}
