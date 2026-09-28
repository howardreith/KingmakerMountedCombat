using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    // Pure evidence check. A simulated command never becomes the committed identity,
    // and its events are still present in the complete action/reaction resource window.
    internal static class NativePredictionCommandEvidence
    {
        internal static void AssertComplete(JObject prediction, JObject proof)
        {
            if ((string)prediction?["contract"] != "native-speculative-init-separated-from-one-committed-request" ||
                !(prediction["commands"] is JArray commands) || commands.Count > 128)
                throw new InvalidOperationException("Missing bounded native prediction evidence.");
            var identity = proof["identity"];
            var actualInit = ((JArray)proof["samples"]).Single(s => (string)s["boundary"] == "init");
            var ids = new HashSet<int> { (int)identity["commandObject"] };
            var shells = new HashSet<string> { (string)identity["controlIdentity"] };
            foreach (var sample in (JArray)proof["samples"])
                if ((bool?)sample["simulatingClick"] != false)
                    throw new InvalidOperationException("Committed observation was native speculation.");
            var events = (JArray)proof["resourceWindow"]["events"];
            foreach (var item in commands)
            {
                var first = item["init"]; var last = item["close"]; var id = first?["identity"];
                foreach (var sample in new[] { first, last, actualInit, proof["preClick"] })
                    foreach (var field in new[] { "frame", "gameTicks", "allocationSequence" })
                        if (sample?[field]?.Type != JTokenType.Integer)
                            throw new InvalidOperationException("Prediction omitted an exact native clock or sequence.");
                if (id == null || (int?)id["commandObject"] == null || (int)id["commandObject"] == 0 ||
                    !ids.Add((int)id["commandObject"]) || string.IsNullOrEmpty((string)id["controlIdentity"]) || !shells.Add((string)id["controlIdentity"]))
                    throw new InvalidOperationException("Prediction shares or omits command/shell identity.");
                foreach (var field in new[] { "casterId", "targetId", "generationAtInit", "commandType", "abilityGuid" })
                    if (id[field] == null || !JToken.DeepEquals(id[field], identity[field]))
                        throw new InvalidOperationException("Prediction has another " + field + ".");
                if ((bool?)first["simulatingClick"] != true || (bool?)last?["simulatingClick"] != false ||
                    (long?)first["gameTicks"] < (long?)proof["preClick"]["gameTicks"] ||
                    (long?)first["gameTicks"] > (long?)actualInit["gameTicks"] ||
                    (long?)first["allocationSequence"] > (long?)actualInit["allocationSequence"] ||
                    (long?)last?["gameTicks"] < (long?)first["gameTicks"])
                    throw new InvalidOperationException("Prediction was not observed before the real Init.");
                foreach (var sample in new[] { first, last })
                {
                    foreach (var field in new[] { "commandObject", "casterId", "targetId", "commandType", "abilityGuid" })
                        if (sample?[field] == null || !JToken.DeepEquals(sample[field], id[field]))
                            throw new InvalidOperationException("Prediction live object identity differs.");
                    if ((bool?)sample["started"] != false || (bool?)sample["acted"] != false ||
                        (int?)sample["processObject"] != 0 || (int?)sample["contextObject"] != 0)
                        throw new InvalidOperationException("Prediction executed or bound a native process.");
                }
                if ((int?)id["processObject"] != 0 || (int?)id["contextObject"] != 0)
                    throw new InvalidOperationException("Prediction Init already bound a process.");
                var matching = events.Where(e => (int?)e["command"] == (int)id["commandObject"]).ToArray();
                if (matching.Count(e => (string)e["boundary"] == "admission-after") != 1)
                    throw new InvalidOperationException("Prediction lacks its exact temporary admission.");
                foreach (var e in matching)
                    if ((bool?)e["simulatingClick"] != true || (bool?)e["started"] != false || (bool?)e["acted"] != false ||
                        !new[] { "admission-before", "admission-after", "command-eligibility" }.Contains((string)e["boundary"]) ||
                        (long?)e["sequence"] > (long?)actualInit["allocationSequence"])
                        throw new InvalidOperationException("Prediction escaped temporary admission or had a native side effect.");
            }
            foreach (var e in events.Where(e => (bool?)e["simulatingClick"] == true))
                if ((string)e["state"]?["actor"] == (string)identity["casterId"] || (string)e["state"]?["actor"] == (string)proof["mountId"])
                    if (new[] { "cost-", "actor-cost-", "prepare-", "clear-", "combat-clear", "opportunity-", "approach-movement-", "native-movement-displacement" }
                        .Any(prefix => ((string)e["boundary"] ?? "").StartsWith(prefix, StringComparison.Ordinal)))
                        throw new InvalidOperationException("Native speculation added a resource or movement callback.");
        }
    }
}
