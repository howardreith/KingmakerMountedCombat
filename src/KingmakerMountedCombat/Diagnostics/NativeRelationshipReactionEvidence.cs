using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class NativeRelationshipCommandProbe
    {
        private const string ReactionContract = "native-time-and-declared-partner-preparation-only";
        private sealed class ReactionBoundary
        {
            internal string Kind;
            internal NativeReactionResources Expected;
        }
        private static NativeReactionResources ReadReaction(JToken state) => new NativeReactionResources(
            (int)state["reactions"], (double)state["reactionCooldown"],
            (double)state["initiativeCooldown"], (int)state["initiativeOrder"]);

        private JObject EvaluateReactionResources(JArray events, int partnerPreparations)
        {
            var failures = new JArray();
            try
            {
                foreach (var actor in new[] { "rider", "mount" })
                {
                    var actorId = actor == "rider" ? rider.UniqueId : mount.UniqueId;
                    var baseline = preClick["state"][actor];
                    var current = ReadReaction(baseline);
                    var initiative = current.InitiativeOrder;
                    var perRound = (int)baseline["reactionsPerRound"];
                    var stack = new Stack<ReactionBoundary>();
                    // Native event sequence is the ordering authority, including equal-clock samples.
                    var timeline = events.OfType<JObject>().Where(e => (string)e["state"]?["actor"] == actorId)
                        .Select(e => new { Sequence = (int)e["sequence"], IsEvent = true, Value = e })
                        .Concat(samples.Select(sample => new { Sequence = (int)sample.Value["allocationSequence"], IsEvent = false, Value = sample.Value }))
                        .OrderBy(item => item.Sequence).ThenBy(item => item.IsEvent ? 0 : 1);
                    foreach (var item in timeline)
                    {
                        var value = item.Value;
                        var stateValue = item.IsEvent ? value["state"] : value["state"][actor];
                        var actual = ReadReaction(stateValue);
                        if (item.Sequence < (int)preClick["allocationSequence"] || item.Sequence > traceEnd ||
                            actual.InitiativeOrder != initiative || (int)stateValue["reactionsPerRound"] != perRound)
                            throw new InvalidOperationException(actor + " reaction sequence, initiative ordering or per-round allowance changed.");
                        var boundary = item.IsEvent ? (string)value["boundary"] : "sample";
                        var kind = boundary.EndsWith("-before", StringComparison.Ordinal) ? boundary.Substring(0, boundary.Length - 7) :
                            boundary.EndsWith("-after", StringComparison.Ordinal) ? boundary.Substring(0, boundary.Length - 6) : null;
                        var nativeResource = kind == "cooldown-tick" || kind == "prepare" || kind == "clear" || kind == "opportunity";
                        if (nativeResource && boundary.EndsWith("-after", StringComparison.Ordinal))
                        {
                            if (stack.Count == 0 || stack.Peek().Kind != kind || !actual.Matches(stack.Pop().Expected))
                                throw new InvalidOperationException(actor + " reaction effect differs from the exact native " + boundary + " event.");
                            current = actual;
                            continue;
                        }
                        if (stack.Count == 0 && !actual.Matches(current))
                            throw new InvalidOperationException(actor + " reaction allowance or cooldown changed without an allowed native event at " + boundary + ".");
                        if (!nativeResource) continue;
                        var expected = actual;
                        if (kind == "cooldown-tick")
                            expected = actual.Tick((double)value["gameDeltaTime"], (bool)value["nativeTurnBased"],
                                (bool)stateValue["inCombat"], (bool)value["nativePassing"], (bool)value["nativeSurprised"],
                                (bool)stateValue["waitingInitiative"], perRound);
                        else if (kind == "prepare")
                        {
                            if (actor != "mount" || partnerPreparations != 1 || (int)value["preparingTurn"] == 0)
                                throw new InvalidOperationException("Undeclared reaction preparation.");
                            expected = actual.Prepare(perRound);
                        }
                        else if (kind == "clear")
                        {
                            if (actor != "mount" || partnerPreparations != 1 || !stack.Any(entry => entry.Kind == "prepare"))
                                throw new InvalidOperationException("Undeclared reaction cooldown clear.");
                            expected = actual.Clear();
                        }
                        // The measured Mount/Dismount contract allows no opportunity consumption.
                        // A declined/simulated native attempt may occur only with unchanged resources.
                        stack.Push(new ReactionBoundary { Kind = kind, Expected = expected });
                    }
                    if (stack.Count != 0) throw new InvalidOperationException("Missing native reaction callback end for " + actor + ".");
                }
            }
            catch (Exception exception) { failures.Add(exception.Message); }
            return new JObject { ["contract"] = ReactionContract, ["pass"] = failures.Count == 0, ["errors"] = failures };
        }
    }
}
