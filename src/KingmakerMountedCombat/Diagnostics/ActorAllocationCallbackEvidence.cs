using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class ActorAllocationCallbackEvidence
    {
        internal static JObject Evaluate(JObject trace, IEnumerable<string> actors, int firstRound)
        {
            var errors = new JArray();
            var measurements = new JArray();
            var events = ((JArray)trace["events"]).OfType<JObject>().ToArray();
            foreach (var actor in actors)
            foreach (var round in Enumerable.Range(firstRound, 3))
            {
                var rows = events.Where(item => (string)item["state"]?["actor"] == actor && (int?)item["round"] == round).ToArray();
                var counts = new JObject();
                var ordered = new[] { "prepare-before", "clear-before", "clear-after", "round-state-before", "round-state-after",
                    "ai-round-before", "ai-round-after", "fact-before", "fact-after", "prepare-after" };
                long previous = 0;
                foreach (var boundary in ordered)
                {
                    var matches = rows.Where(item => (string)item["boundary"] == boundary &&
                        (!boundary.StartsWith("fact-", StringComparison.Ordinal) ||
                         (string)item["detail"] == "Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing")).ToArray();
                    counts[boundary] = matches.Length;
                    if (matches.Length != 1 || (long?)matches[0]["sequence"] <= previous)
                        errors.Add(actor + ":" + round + ": missing, duplicated or unordered " + boundary);
                    else previous = (long)matches[0]["sequence"];
                }
                var beforeFact = rows.FirstOrDefault(item => (string)item["boundary"] == "fact-before" &&
                    (string)item["detail"] == "Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing");
                var afterFact = rows.FirstOrDefault(item => (string)item["boundary"] == "fact-after" &&
                    (string)item["detail"] == "Kingmaker.UnitLogic.Buffs.Components.AddEffectFastHealing");
                var healed = beforeFact == null || afterFact == null ? -1 :
                    (int)beforeFact["state"]["damage"] - (int)afterFact["state"]["damage"];
                if (healed != 1) errors.Add(actor + ":" + round + ": native fast healing effect was not exactly one.");
                foreach (var boundary in new[] { "round-handler", "ready-handler" })
                {
                    var before = rows.Where(item => (string)item["boundary"] == boundary + "-before").ToArray();
                    var after = rows.Where(item => (string)item["boundary"] == boundary + "-after").ToArray();
                    if (before.Length == 0 || before.Length != after.Length ||
                        !before.Select(item => (int)item["callbackObject"]).SequenceEqual(after.Select(item => (int)item["callbackObject"])) ||
                        before.Select(item => (int)item["callbackObject"]).Distinct().Count() != before.Length)
                        errors.Add(actor + ":" + round + ": callback delivery mismatch " + boundary);
                    var lower = boundary == "round-handler" ? "round-state-after" : "fact-after";
                    var upper = boundary == "round-handler" ? "ai-round-before" : "prepare-after";
                    var lowerRows = rows.Where(item => (string)item["boundary"] == lower).ToArray();
                    var upperRows = rows.Where(item => (string)item["boundary"] == upper).ToArray();
                    if (before.Length > 0 && after.Length > 0 && lowerRows.Length > 0 && upperRows.Length > 0 &&
                        ((long)before.First()["sequence"] <= lowerRows.Max(item => (long)item["sequence"]) ||
                         (long)after.Last()["sequence"] >= upperRows.Min(item => (long)item["sequence"])))
                        errors.Add(actor + ":" + round + ": callback boundary order mismatch " + boundary);
                    counts[boundary] = before.Length;
                }
                var dependent = rows.Where(item => new[] { "round-state-before", "round-handler-before", "ai-round-before",
                    "fact-before", "ready-handler-before" }.Contains((string)item["boundary"])).ToArray();
                foreach (var item in dependent)
                {
                    var retained = item["state"]?["retained"] as JObject;
                    if (retained != null && (int?)retained["Round"] == round &&
                        ((float)item["state"]["move"] + 0.00001f < (float)retained["MoveUsed"] ||
                         (bool)retained["StandardUsed"] && (float)item["state"]["standard"] < 6f))
                        errors.Add(actor + ":" + round + ": callbacks observed lost expenditure at " + item["sequence"]);
                }
                measurements.Add(new JObject { ["actor"] = actor, ["round"] = round,
                    ["counts"] = counts, ["healed"] = healed, ["dependentCallbackCount"] = dependent.Length });
            }
            return new JObject { ["level"] = "NATIVE INTEGRATION", ["passed"] = errors.Count == 0,
                ["measurements"] = measurements, ["errors"] = errors };
        }
    }
}
