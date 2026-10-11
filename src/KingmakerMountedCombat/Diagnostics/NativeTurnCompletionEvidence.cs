using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    // Ordinary native End Turn only. Condition-forfeit normalization is a separate
    // existing contract and cannot be inferred from these observations.
    internal static class NativeTurnCompletionEvidence
    {
        private static void Require(bool ok, string why) { if (!ok) throw new InvalidOperationException("Native completion: " + why); }
        private static long Int(JToken x) { Require(x?.Type == JTokenType.Integer, "integer absent"); return (long)x; }
        private static double Num(JToken x) { Require(x != null && (x.Type == JTokenType.Integer || x.Type == JTokenType.Float), "number absent"); var n = (double)x; Require(!double.IsNaN(n) && !double.IsInfinity(n), "nonfinite"); return n; }
        private static bool Bool(JToken x) { Require(x?.Type == JTokenType.Boolean, "boolean absent"); return (bool)x; }
        private static string Text(JToken x) => x?.Type == JTokenType.String ? (string)x : null;
        private static void EqualNumber(JToken actual, double expected, string why) { Require(Math.Abs(Num(actual) - expected) <= 0.0001, why); }
        internal static void AssertCall(JObject c)
        {
            Require(Bool(c?["complete"]), "native callback incomplete");
            var kind = Text(c["kind"]); Require(kind == "end" || kind == "force-to-end", "unknown callback");
            var set = kind == "force-to-end" && Bool(c["setCooldowns"]);
            if (kind == "end") Require(c["setCooldowns"] != null && c["setCooldowns"].Type == JTokenType.Null, "End has no bool argument");
            var before = c["before"]; var after = c["after"]; var a = before?["resources"]; var b = after?["resources"];
            foreach (var f in new[] { "frame", "gameTicks", "turnObject", "round", "pairedSequence" }) Require(Int(before?[f]) == Int(after?[f]), "synchronous callback identity/clock changed");
            Require(Int(before["turnObject"]) != 0 && Int(after["allocationSequence"]) >= Int(before["allocationSequence"]), "turn/trace identity missing");
            Require(Bool(before["turnBased"]) && Bool(after["turnBased"]) && Bool(a?["inCombat"]) && Bool(b?["inCombat"]), "requires live TB allocation");
            Require(!Bool(before["conditionForfeitContext"]) && !Bool(after["conditionForfeitContext"]), "condition forfeiture is outside ordinary End contract");
            var owned = Bool(before["completionDebtOwned"]); Require(owned == Bool(after["completionDebtOwned"]), "completion ownership changed inside call");
            if (owned) {
                Require(Int(before["pairedSequence"]) > 0, "owned completion has no activation sequence");
                foreach (var endpoint in new[] { before, after }) {
                    var state = endpoint["forfeitState"];
                    Require(Bool(state?["granted"]) && Bool(state["prepared"]) && !Bool(state["forfeitRecorded"]) &&
                        !Bool(state["forfeitSettled"]) && Num(state["forfeitStandardAdded"]) == 0, "ordinary completion has prior condition-forfeit settlement");
                }
            }
            Require(!string.IsNullOrEmpty(Text(before["actorId"])) && Text(before["actorId"]) == Text(after["actorId"]) &&
                Text(a?["actor"]) == Text(before["actorId"]) && Text(b?["actor"]) == Text(before["actorId"]) &&
                Int(a?["actorObject"]) != 0 && Int(a["actorObject"]) == Int(b?["actorObject"]), "actor identity differs");
            Require(Int(c["enterOrdinal"]) > 0 && Int(c["exitOrdinal"]) > Int(c["enterOrdinal"]), "call ordering invalid");
            foreach (var f in new[] { "reactions", "reactionsPerRound", "initiativeOrder", "grantSequence" }) Require(Int(a[f]) == Int(b[f]), "discrete resource changed: " + f);
            foreach (var f in new[] { "reactionCooldown", "initiativeCooldown" }) EqualNumber(b[f], Num(a[f]), "reaction/initiative cooldown changed");
            var standard = Num(a["standard"]); var move = Num(a["move"]); var swift = Num(a["swift"]);
            var step = Num(before["stepMetres"]); var limit = Num(before["stepLimit"]);
            Require(limit > 0, "step limit absent"); EqualNumber(after["stepLimit"], limit, "step limit changed");
            if (kind == "end") {
                standard = owned ? Math.Max(standard, 6) : 6;
                move = owned ? Math.Max(move, Math.Min(6, move)) : Math.Min(6, move);
                Require(Text(after["status"]) == "Ended", "End did not end the exact context");
            } else {
                if (set) { standard = owned ? Math.Max(standard, 6) : 6; move = owned ? Math.Max(move, 3) : 3; swift = owned ? Math.Max(swift, 6) : 6; step = limit; }
                Require(Text(after["status"]) == "Ending", "ordinary ForceToEnd did not enter Ending");
            }
            EqualNumber(b["standard"], standard, "Standard differs from exact observed completion");
            EqualNumber(b["move"], move, "Move differs from exact observed completion");
            EqualNumber(b["swift"], swift, "Swift differs from exact observed completion");
            EqualNumber(after["stepMetres"], step, "step differs from native completion");
        }
        internal static void AssertComplete(JObject evidence)
        {
            Require(Text(evidence?["contract"]) == "observed-native-turn-completion-writes" && Bool(evidence["traceComplete"]), "observation contract or trace missing");
            Require(evidence["calls"] is JArray && evidence["errors"] is JArray && !evidence["errors"].Any(), "incomplete calls/errors");
            var rider = Text(evidence["riderId"]); var mount = Text(evidence["mountId"]);
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount, "pair missing");
            foreach (var token in new[] { "06000C46", "06000C47" }) {
                var hooks = evidence["observerHooks"].Where(x => Text(x["token"]) == token).ToArray();
                Require(hooks.Length == 1 && Text(hooks[0]["moduleMvid"]) == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7", "native hook absent");
            }
            var calls = ((JArray)evidence["calls"]).OfType<JObject>().ToArray(); Require(calls.Length > 0 && calls.Length <= 64, "empty or unbounded calls");
            var ordinals = new List<long>(); var starts = new List<long>();
            foreach (var c in calls) {
                AssertCall(c); Require(Text(c["before"]["actorId"]) == rider || Text(c["before"]["actorId"]) == mount, "foreign actor recorded");
                starts.Add(Int(c["enterOrdinal"])); ordinals.Add(Int(c["enterOrdinal"])); ordinals.Add(Int(c["exitOrdinal"]));
            }
            Require(starts.SequenceEqual(starts.OrderBy(x => x)) && ordinals.OrderBy(x => x).SequenceEqual(Enumerable.Range(1, calls.Length * 2).Select(x => (long)x)), "missing, duplicate or unordered boundary");
            foreach (var c in calls) foreach (var d in calls) {
                var a = Int(c["enterOrdinal"]); var b = Int(c["exitOrdinal"]); var x = Int(d["enterOrdinal"]); var y = Int(d["exitOrdinal"]);
                Require(!(a < x && x < b && b < y), "crossed nested callback lifetime");
            }
        }
    }
}
