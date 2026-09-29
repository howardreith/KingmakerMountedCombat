using System;
using Kingmaker.UnitLogic.Commands;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    // Diagnostic setup only. Native movement retains all path and resource ownership.
    internal static class NativeActingGroundInput
    {
        internal const float ApproachRadius = 0.03f;
        internal static UnitMoveTo Create(Vector3 point, float? speedLimit, float orientation, float delay, bool marker)
        {
            return new UnitMoveTo(point, ApproachRadius) {
                CreatedByPlayer = true, SpeedLimit = speedLimit, Orientation = orientation,
                MovementDelay = delay, ShowTargetMarker = marker
            };
        }
        private static void Need(bool value, string why)
        { if (!value) throw new InvalidOperationException("Precise native Acting setup: " + why); }
        private static double Number(JToken value)
        {
            Need(value != null && (value.Type == JTokenType.Float || value.Type == JTokenType.Integer), "numeric evidence absent");
            var number = (double)value;
            Need(!double.IsNaN(number) && !double.IsInfinity(number), "nonfinite evidence");
            return number;
        }
        internal static void AssertComplete(JObject setup)
        {
            var input = setup?["nativeGroundInput"];
            Need((string)input?["contract"] == "native-ground-input-with-one-precise-setup-command", "input contract absent");
            Need((string)input["methodToken"] == "060093DB" && (string)input["directionToken"] == "060093D9" &&
                (string)input["constructorToken"] == "060026FF", "pinned native input differs");
            Need(input["callbackCount"]?.Type == JTokenType.Integer && (int)input["callbackCount"] == 1, "callback count differs");
            var rider = (string)setup["before"]["currentTurnActor"];
            Need(!string.IsNullOrEmpty(rider) && (string)input["actorId"] == rider, "callback actor differs");
            var selection = input["selectedIds"] as JArray;
            Need(selection != null && selection.Count == 1 && (string)selection[0] == rider, "exact selected rider absent");
            Need(input["commandObject"]?.Type == JTokenType.Integer && (int)input["commandObject"] != 0 &&
                (int)input["commandObject"] == (int)setup["admittedCommand"]["id"] &&
                (int)input["commandObject"] == (int)setup["terminalCommand"]["id"], "command identity differs");
            Need(input["createdByPlayer"]?.Type == JTokenType.Boolean && (bool)input["createdByPlayer"], "player command absent");
            Need(input["frame"]?.Type == JTokenType.Integer && (int)input["frame"] == (int)setup["before"]["frame"], "input frame differs");
            foreach (var field in new[] { "approachRadius", "terminalApproachRadius", "agentApproachRadius" })
                Need(Math.Abs(Number(input[field]) - ApproachRadius) <= 0.000001, "arrival radius differs: " + field);
            Need(Math.Abs(Number(setup["placementTolerance"]) - 0.06) <= 0.000001, "arrival tolerance changed");
            foreach (var axis in new[] { "x", "y", "z" })
                Need(Math.Abs(Number(input["targetPoint"]?[axis]) - Number(setup["destination"]?[axis])) <= 0.000001,
                    "native command destination differs: " + axis);
        }
    }
}
