using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeApproachMovementEvidence
    {
        // Exact TB native movement is accounted separately from the acted Move cost.
        internal static JObject Evaluate(JArray events, JObject preClick, JObject costBefore, int commandId, string riderId, string ability, bool inCombat, bool turnBased, bool required, JArray observerHooks)
        {
            var failures = new JArray();
            var movement = events.OfType<JObject>().Where(e => ((string)e["boundary"]).StartsWith("approach-movement-", StringComparison.Ordinal)).ToArray();
            var total = 0.0;
            try
            {
                var allowed = inCombat && turnBased && ability == "f053faad986631688defa003cd7bda0e";
                if (allowed && observerHooks.OfType<JObject>().Count(h => (string)h["method"] == "TurnBased.Controllers.TurnController.TickMovement" &&
                    (string)h["token"] == "06000C37" && (string)h["moduleMvid"] == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7" &&
                    (string)h["prefix"] == "ApproachMovementBefore" && (string)h["postfix"] == "ApproachMovementAfter") != 1)
                    throw new InvalidOperationException("Exact native movement observer hook installation is missing.");
                if (!allowed && movement.Length != 0) throw new InvalidOperationException("Native approach movement was not declared for this window.");
                if (movement.Length % 2 != 0 || allowed && required && movement.Length == 0)
                    throw new InvalidOperationException("Exact native TB approach movement callbacks are missing.");
                if (costBefore == null) throw new InvalidOperationException("Movement proof lacks the exact native cost boundary.");
                var expected = (double)preClick["state"]["rider"]["move"];
                var lastSequence = (int)preClick["allocationSequence"];
                double? previousTime = null;
                if (allowed)
                {
                    foreach (var field in new[] { "move", "timeMoved", "timeForced", "timeStepped" })
                    {
                        var value = preClick["state"]["rider"][field];
                        if (value == null || value.Type == JTokenType.Null || double.IsNaN((double)value) || double.IsInfinity((double)value) || (double)value < 0)
                            throw new InvalidOperationException("Pre-click native movement baseline is missing or invalid.");
                    }
                    if ((int)preClick["state"]["rider"]["nativeTurnObject"] == 0 || (int)preClick["state"]["rider"]["nativeTurnObject"] != (int)costBefore["turn"])
                        throw new InvalidOperationException("Movement baseline belongs to another native turn.");
                    previousTime = (double)preClick["state"]["rider"]["timeMoved"];
                }
                var previousTicks = (long)preClick["gameTicks"];
                for (var index = 0; index < movement.Length; index += 2)
                {
                    var before = movement[index]; var after = movement[index + 1];
                    if ((string)before["boundary"] != "approach-movement-before" || (string)after["boundary"] != "approach-movement-after" ||
                        (int)after["sequence"] != (int)before["sequence"] + 1 || (int)before["sequence"] <= lastSequence ||
                        (int)after["sequence"] >= (int)costBefore["sequence"] || (int)before["frame"] != (int)after["frame"] ||
                        (long)before["gameTicks"] != (long)after["gameTicks"] || (long)before["gameTicks"] < previousTicks ||
                        (long)after["gameTicks"] > (long)costBefore["gameTicks"])
                        throw new InvalidOperationException("Native movement callback pair or pre-acted order differs.");
                    foreach (var item in new[] { before, after })
                    {
                        if ((int)item["command"] != commandId || (int)item["slotCommand"] != commandId ||
                            (string)item["commandActor"] != riderId || (string)item["state"]["actor"] != riderId ||
                            (string)item["currentActor"] != riderId || (string)item["actionType"] != "Move" ||
                            (string)item["commandType"] != "Kingmaker.UnitLogic.Commands.UnitUseAbility" ||
                            (int)item["turn"] == 0 || (int)item["turn"] != (int)costBefore["turn"] ||
                            (int)item["movementTurn"] != (int)item["turn"] || (string)item["turnStatus"] != "Acting" ||
                            (bool?)item["nativeTurnBased"] != true || (bool?)item["nativePassing"] != false ||
                            (bool?)item["started"] != false || (bool?)item["acted"] != false || (bool?)item["finished"] != false ||
                            (bool?)item["force"] != false || (bool?)item["fiveFootStep"] != false)
                            throw new InvalidOperationException("Movement belongs to another command, turn, phase or undeclared movement mode.");
                        foreach (var value in new[] { item["delta"], item["state"]["move"], item["state"]["timeMoved"], item["state"]["timeForced"], item["state"]["timeStepped"] })
                            if (value == null || value.Type == JTokenType.Null || double.IsNaN((double)value) || double.IsInfinity((double)value) || (double)value < 0)
                                throw new InvalidOperationException("Movement measurement is not finite and nonnegative.");
                    }
                    if ((double)after["delta"] > (double)before["delta"] || Math.Abs((double)before["state"]["move"] - expected) > 0.0001 ||
                        previousTime.HasValue && Math.Abs((double)before["state"]["timeMoved"] - previousTime.Value) > 0.0001)
                        throw new InvalidOperationException("Movement has an unexplained resource/time gap or excessive accepted delta.");
                    foreach (var field in new[] { "timeForced", "timeStepped" })
                        if (Math.Abs((double)before["state"][field] - (double)preClick["state"]["rider"][field]) > 0.0001)
                            throw new InvalidOperationException("Undeclared movement mode debt differs from the pre-click baseline.");
                    var expectedMove = (float)before["state"]["move"] + (float)after["delta"];
                    var expectedTime = (float)before["state"]["timeMoved"] + (float)after["delta"];
                    if (Math.Abs((double)after["state"]["move"] - expectedMove) > 0.0001 || Math.Abs((double)after["state"]["timeMoved"] - expectedTime) > 0.0001)
                        throw new InvalidOperationException("Native accepted movement delta does not explain its exact Move and TimeMoved changes.");
                    foreach (var field in new[] { "standard", "swift", "reactions", "reactionsPerRound", "reactionCooldown", "initiativeCooldown", "initiativeOrder", "timeForced", "timeStepped" })
                        if (before["state"][field] == null || after["state"][field] == null || before["state"][field].Type == JTokenType.Null || after["state"][field].Type == JTokenType.Null ||
                            double.IsNaN((double)before["state"][field]) || double.IsInfinity((double)before["state"][field]) ||
                            !JToken.DeepEquals(before["state"][field], after["state"][field]))
                            throw new InvalidOperationException("Native movement changed an unrelated field: " + field);
                    expected = (double)after["state"]["move"]; previousTime = (double)after["state"]["timeMoved"];
                    lastSequence = (int)after["sequence"]; previousTicks = (long)after["gameTicks"]; total += (double)after["delta"];
                }
                if (allowed && (Math.Abs((double)costBefore["state"]["move"] - expected) > 0.0001 || required && total <= 0))
                    throw new InvalidOperationException("Exact native movement events do not explain the pre-acted Move endpoint.");
            }
            catch (Exception exception) { failures.Add(exception.Message); }
            return new JObject { ["contract"] = "normal-native-tb-mount-approach", ["required"] = required && inCombat && turnBased,
                ["observerHooks"] = observerHooks.DeepClone(), ["callbackPairs"] = movement.Length / 2, ["acceptedDeltaTotal"] = total, ["pass"] = failures.Count == 0, ["errors"] = failures };
        }
    }
}