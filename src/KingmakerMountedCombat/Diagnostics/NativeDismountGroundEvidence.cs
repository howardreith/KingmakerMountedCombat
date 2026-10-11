using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    // One bounded setup window only. This does not qualify Mount, Dismount, or
    // after-mount-expenditure; each relationship command retains its own proof.
    internal static class NativeDismountGroundEvidence
    {
        private static void Need(bool yes, string why) { if (!yes) throw new InvalidOperationException("Dismount ground setup: " + why); }
        private static long I(JToken x) { Need(x?.Type == JTokenType.Integer, "integer missing"); return (long)x; }
        private static double N(JToken x) { Need(x != null && (x.Type == JTokenType.Float || x.Type == JTokenType.Integer), "number missing"); var n = (double)x; Need(!double.IsNaN(n) && !double.IsInfinity(n), "nonfinite"); return n; }
        internal static void AssertNextPair(JToken boundary, string rider, string mount)
        {
            Need((string)boundary["currentActor"] == rider && I(boundary["turnObject"]) != 0 &&
                I(boundary["pairedSequence"]) == 2 && I(boundary["partnerContextObject"]) != 0 &&
                (string)boundary["partnerActor"] == mount && (bool?)boundary["pairedSplit"] == false,
                "idle next rider has no exact paired allocation; no pending native event can repair it");
        }

        internal static void AssertComplete(JObject p, string rider, string mount)
        {
            Need(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount && (bool?)p?["traceComplete"] == true, "pair or trace absent");
            var before = p["before"]; var after = p["after"]; var admitted = p["admittedCommand"]; var terminal = p["terminalCommand"];
            var id = I(admitted?["id"]); var turn = I(before?["turnObject"]); var round = I(before["round"]);
            Need(id != 0 && turn != 0 && I(terminal?["id"]) == id && (string)admitted["executor"] == mount &&
                (string)terminal["executor"] == mount && (string)admitted["type"] == "Kingmaker.UnitLogic.Commands.UnitMoveTo" &&
                (string)terminal["type"] == (string)admitted["type"] && (bool?)terminal["acted"] == true &&
                (bool?)terminal["finished"] == true && (string)terminal["result"] == "Success", "exact ground command did not succeed");
            Need((string)before["status"] == "Preparing" && (string)after["status"] == "Acting" &&
                I(after["turnObject"]) == turn && I(after["round"]) == round &&
                (string)before["currentActor"] == rider && (string)after["currentActor"] == rider, "same native Preparing to Acting rider absent");
            foreach (var b in new[] { before, after }) {
                Need((bool?)b["turnBased"] == true && I(b["pairedSequence"]) == 2 && !(bool)b["pairedSplit"] &&
                    (string)b["partnerActor"] == mount && I(b["partnerContextObject"]) != 0 &&
                    (string)b["state"]["relationshipState"] == "Mounted" && b["state"]["selectedIds"] is JArray &&
                    b["state"]["selectedIds"].Count() == 1 && (string)b["state"]["selectedIds"][0] == rider, "pair, selection or allocation changed");
                foreach (var key in new[] { "generation", "ledger" }) Need(JToken.DeepEquals(before["state"][key], b["state"][key]), "relationship changed");
                foreach (var key in new[] { "controllerObject", "sessionObject", "partnerContextObject", "adoptionCount" })
                    Need(JToken.DeepEquals(before[key], b[key]), "encounter identity changed");
            }
            Need(p["events"] is JArray && p["observerHooks"] is JArray, "native evidence missing");
            foreach (var token in new[] { "060026B2", "06009120", "0600838F", "060093A1", "0600934A", "06000C3C", "0600C3BE", "060018A9", "06000C5E" })
                Need(p["observerHooks"].Count(h => (string)h["token"] == token && (string)h["moduleMvid"] == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7") == 1, "observer missing: " + token);
            long sequence = I(before["allocationSequence"]), frame = I(before["frame"]), ticks = I(before["gameTicks"]);
            var move = N(before["mountResources"]["move"]);
            var costs = new JArray(); var admissions = new JArray(); var ended = 0;
            foreach (var e in p["events"]) {
                Need(I(e["sequence"]) == ++sequence && I(e["frame"]) >= frame && I(e["frame"]) <= I(after["frame"]) &&
                    I(e["gameTicks"]) >= ticks && I(e["gameTicks"]) <= I(after["gameTicks"]), "event clock or sequence gap");
                frame = I(e["frame"]); ticks = I(e["gameTicks"]);
                var actor = (string)e["state"]?["actor"]; if (actor != rider && actor != mount) continue;
                var role = actor == rider ? "riderResources" : "mountResources"; var s = e["state"]; var baseline = before[role];
                Need((bool?)e["nativeTurnBased"] == true && (bool?)e["nativePassing"] == false &&
                    I(e["round"]) == round && I(e["turn"]) == turn && (string)e["currentActor"] == rider, "native allocation changed");
                foreach (var field in new[] { "actor", "actorObject", "inCombat", "grantSequence", "standard", "swift",
                    "reactions", "reactionsPerRound", "reactionCooldown", "initiativeCooldown", "initiativeOrder" })
                    Need(baseline[field] != null && JToken.DeepEquals(s[field], baseline[field]), "undeclared actor change: " + actor + " " + field);
                if (actor == rider) Need(N(s["move"]) == N(baseline["move"]), "carried rider was charged");
                else { Need(N(s["move"]) + 0.0001 >= move, "mount movement was refunded"); move = N(s["move"]); }
                var boundary = (string)e["boundary"] ?? "";
                Need(!new[] { "prepare", "clear", "opportunity", "round-", "turn-end", "completion-", "remove-unit" }.Any(boundary.Contains), "undeclared native event: " + boundary);
                if (boundary.Contains("cost") || boundary.Contains("admission") || boundary.StartsWith("command-end", StringComparison.Ordinal)) {
                    Need(I(e["command"]) == id && actor == mount && (string)e["commandType"] == (string)admitted["type"] &&
                        (string)e["actionType"] == "Move" && (bool?)e["simulatingClick"] == false, "another command or action");
                    if (boundary.Contains("cost")) costs.Add(e.DeepClone());
                    if (boundary.Contains("admission")) admissions.Add(e.DeepClone());
                    if (boundary == "command-end-after" && (bool?)e["finished"] == true && (string)e["result"] == "Success") ended++;
                }
            }
            Need(sequence == I(after["allocationSequence"]) && ended > 0, "terminal event absent");
            Need(admissions.Count == 2 && (string)admissions[0]["boundary"] == "admission-before" &&
                (string)admissions[1]["boundary"] == "admission-after", "one ground admission absent");
            var costBoundaries = new[] { "cost-before", "actor-cost-before", "actor-cost-after", "cost-after" };
            Need(costs.Count == costBoundaries.Length, "native TB ground cost callback count differs");
            for (var i = 0; i < costs.Count; i++) {
                Need((string)costs[i]["boundary"] == costBoundaries[i] && I(costs[i]["sequence"]) == I(costs[0]["sequence"]) + i &&
                    (bool?)costs[i]["ignoreCooldown"] == true && (bool?)costs[i]["acted"] == true, "native TB ground cost boundary differs");
                foreach (var field in new[] { "standard", "move", "swift" })
                    Need(JToken.DeepEquals(costs[0]["state"][field], costs[i]["state"][field]), "ground acted callback changed debt");
            }
            foreach (var role in new[] { "riderResources", "mountResources" }) {
                foreach (var field in new[] { "actor", "actorObject", "inCombat", "grantSequence", "standard", "swift",
                    "reactions", "reactionsPerRound", "reactionCooldown", "initiativeCooldown", "initiativeOrder" })
                    Need(JToken.DeepEquals(before[role][field], after[role][field]), "terminal unrelated resource changed");
            }
            Need(N(after["riderResources"]["move"]) == N(before["riderResources"]["move"]), "terminal rider was charged");
            var delta = N(after["mountResources"]["move"]) - N(before["mountResources"]["move"]);
            var allowed = N(after["mountResources"]["measuredAllowedTime"]) - N(before["mountResources"]["measuredAllowedTime"]);
            Need(delta > 0 && allowed > 0 && Math.Abs(delta - allowed) <= 0.0001 && Math.Abs(move - N(after["mountResources"]["move"])) <= 0.0001,
                "mount debt differs from observed native allowed movement time");
        }
    }
}
