using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    // One declared ordinary paired completion interval: no command cost, one
    // next-round native preparation per actor, exact observed completion effects.
    // This model is observational and has no game/resource write surface.
    internal static class NativeAllocationContinuationEvidence
    {
        private static void Require(bool ok, string why) { if (!ok) throw new InvalidOperationException("Allocation continuation: " + why); }
        private static string Text(JToken x) => x?.Type == JTokenType.String ? (string)x : null;
        private static long Int(JToken x) { Require(x?.Type == JTokenType.Integer, "integer missing"); return (long)x; }
        private static bool Bool(JToken x) { Require(x?.Type == JTokenType.Boolean, "boolean missing"); return (bool)x; }
        private static double Num(JToken x) { Require(x != null && (x.Type == JTokenType.Integer || x.Type == JTokenType.Float), "number missing"); var n = (double)x; Require(!double.IsNaN(n) && !double.IsInfinity(n), "nonfinite"); return n; }
        private sealed class State
        {
            internal string Actor; internal long Object, Grants; internal int PerRound; internal bool InCombat;
            internal double Standard, Move, Swift; internal NativeReactionResources Reaction;
            internal static State Read(JToken s) => new State {
                Actor = Text(s?["actor"]), Object = Int(s?["actorObject"]), Grants = Int(s?["grantSequence"]), InCombat = Bool(s?["inCombat"]),
                PerRound = (int)Int(s?["reactionsPerRound"]), Standard = Num(s?["standard"]), Move = Num(s?["move"]), Swift = Num(s?["swift"]),
                Reaction = new NativeReactionResources((int)Int(s?["reactions"]), Num(s?["reactionCooldown"]), Num(s?["initiativeCooldown"]), (int)Int(s?["initiativeOrder"]))
            };
            internal State Copy() => (State)MemberwiseClone();
            internal bool Matches(State s) => s != null && Actor == s.Actor && Object == s.Object && Object != 0 && InCombat && s.InCombat &&
                Grants == s.Grants && PerRound == s.PerRound && Reaction.Matches(s.Reaction) && Math.Abs(Standard - s.Standard) <= 0.0001 && Math.Abs(Move - s.Move) <= 0.0001 && Math.Abs(Swift - s.Swift) <= 0.0001;
            internal State Tick(JToken e)
            {
                var delta = Num(e["gameDeltaTime"]); Require(delta >= 0 && Bool(e["nativeTurnBased"]), "tick is not native TB time");
                var passing = Bool(e["nativePassing"]); var surprised = Bool(e["nativeSurprised"]); var waiting = Bool(e["state"]?["waitingInitiative"]);
                var result = Copy(); result.Reaction = Reaction.Tick(delta, true, InCombat, passing, surprised, waiting, PerRound);
                var decay = !passing ? 0 : Reaction.InitiativeCooldown > 0 && !surprised ? Math.Max(0, delta - Reaction.InitiativeCooldown) : delta;
                if (decay > 0) { result.Standard = Math.Max(0, Standard - decay); result.Move = Math.Max(0, Move - decay); result.Swift = Math.Max(0, Swift - decay); }
                return result;
            }
            internal State Clear() { var s = Copy(); s.Standard = s.Move = s.Swift = 0; s.Reaction = Reaction.Clear(); return s; }
            internal State Prepare() { var s = Clear(); s.Reaction = Reaction.Prepare(PerRound); return s; }
        }
        private sealed class Boundary { internal string Kind; internal State Expected; }
        internal static void AssertComplete(JObject p)
        {
            Require(Text(p?["contract"]) == "ordinary-paired-end-through-one-native-next-preparation" && Bool(p["traceComplete"]), "contract or trace missing");
            var before = p["before"]; var after = p["after"];
            var rider = Text(p["riderId"]); var mount = Text(p["mountId"]);
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount, "pair missing");
            var start = Int(before?["allocationSequence"]); var end = Int(after?["allocationSequence"]); var round = Int(before?["round"]);
            Require(start >= 0 && end > start && Int(after?["round"]) == round + 1 && Int(after["frame"]) > Int(before["frame"]) && Int(after["gameTicks"]) >= Int(before["gameTicks"]), "interval did not reach exactly the next round");
            var completion = (JObject)p["completion"]; NativeTurnCompletionEvidence.AssertComplete(completion);
            Require(Text(completion["riderId"]) == rider && Text(completion["mountId"]) == mount, "completion pair differs");
            Require(p["events"] is JArray && p["observerHooks"] is JArray, "event/hook inventory absent");
            foreach (var token in new[] { "06000C3C", "0600C3BE", "0600934A", "060093A1", "060093A4", "06009120", "0600838F", "060026B2", "06000C37" }) {
                var hooks = p["observerHooks"].Where(x => Text(x["token"]) == token).ToArray();
                Require(hooks.Length == 1 && Text(hooks[0]["moduleMvid"]) == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7", "native allocation hook absent");
            }
            var completions = new Dictionary<long, JToken>();
            foreach (var c in completion["calls"]) foreach (var phase in new[] { "before", "after" }) {
                var seq = Int(c[phase]["allocationSequence"]);
                Require(seq > start && seq <= end && !completions.ContainsKey(seq), "completion boundary outside interval or duplicate");
                completions.Add(seq, c);
            }
            var current = new Dictionary<string, State>(); var stacks = new Dictionary<string, Stack<Boundary>>();
            var preparations = new Dictionary<string, int>(); var clears = new Dictionary<string, int>();
            foreach (var pair in new[] { new[] { "rider", rider }, new[] { "mount", mount } }) {
                var s = State.Read(before[pair[0]]); Require(s.Actor == pair[1] && s.Object != 0 && s.InCombat, "baseline actor absent");
                current.Add(pair[1], s); stacks.Add(pair[1], new Stack<Boundary>()); preparations.Add(pair[1], 0); clears.Add(pair[1], 0);
            }
            Require(current[rider].Object != current[mount].Object, "actors alias");
            var sequence = start; var ticks = Int(before["gameTicks"]); var frame = Int(before["frame"]); var nativeRound = round; var observedCompletion = new HashSet<long>();
            foreach (var e in p["events"]) {
                Require(Int(e["sequence"]) == ++sequence && Int(e["gameTicks"]) >= ticks && Int(e["gameTicks"]) <= Int(after["gameTicks"]), "trace gap or clock mismatch");
                Require(Int(e["frame"]) >= frame && Int(e["frame"]) <= Int(after["frame"]) && Int(e["round"]) >= nativeRound && Int(e["round"]) <= round + 1, "frame or native round mismatch");
                frame = Int(e["frame"]); nativeRound = Int(e["round"]); ticks = Int(e["gameTicks"]); var actor = Text(e["state"]?["actor"]); Require(!string.IsNullOrEmpty(actor), "event actor absent");
                if (!current.ContainsKey(actor)) continue;
                Require(Bool(e["nativeTurnBased"]), "native mode changed");
                var actual = State.Read(e["state"]); var boundary = Text(e["boundary"]); Require(boundary != null, "boundary absent");
                Require(!new[] { "cost", "admission", "opportunity", "combat-clear", "approach-movement", "native-movement-displacement", "remove-unit" }.Any(boundary.Contains), "undeclared command/cost/reaction/lifecycle event");
                var stack = stacks[actor]; var isBefore = boundary.EndsWith("-before", StringComparison.Ordinal); var isAfter = boundary.EndsWith("-after", StringComparison.Ordinal);
                var kind = isBefore ? boundary.Substring(0, boundary.Length - 7) : isAfter ? boundary.Substring(0, boundary.Length - 6) : boundary;
                var owned = kind == "cooldown-tick" || kind == "prepare" || kind == "clear" || kind == "completion-force-to-end" || kind == "completion-end";
                JToken call = null;
                if (kind.StartsWith("completion-", StringComparison.Ordinal)) {
                    Require(completions.TryGetValue(sequence, out call), "completion event lacks exact native call binding");
                    var phase = isBefore ? "before" : "after"; var s = call[phase];
                    Require(kind == "completion-" + Text(call["kind"]) && Int(s["allocationSequence"]) == sequence && Int(s["turnObject"]) == Int(e["callbackObject"]) &&
                        Int(s["frame"]) == Int(e["frame"]) && Int(s["gameTicks"]) == Int(e["gameTicks"]) && Int(s["round"]) == Int(e["round"]) &&
                        Text(s["actorId"]) == actor && State.Read(s["resources"]).Matches(actual), "completion identity or state differs");
                    observedCompletion.Add(sequence);
                }
                if (kind == "prepare" && isBefore) {
                    Require(stack.Count == 0 && Int(e["round"]) == round + 1 && Int(e["preparingTurn"]) != 0 && ++preparations[actor] == 1, "preparation is not the one next-round grant");
                    var incremented = current[actor].Copy(); incremented.Grants++; current[actor] = incremented;
                }
                if (owned && isAfter) {
                    Require(stack.Count > 0 && stack.Peek().Kind == kind && stack.Pop().Expected.Matches(actual), "native effect differs at " + boundary);
                    current[actor] = actual; continue;
                }
                Require(current[actor].Matches(actual) || stack.Count > 0 && stack.Peek().Expected.Matches(actual), "unexplained resource change at " + boundary);
                current[actor] = actual;
                if (!owned) continue;
                Require(isBefore, "native boundary suffix differs"); var expected = actual;
                if (kind == "cooldown-tick") expected = actual.Tick(e);
                else if (kind == "prepare") expected = actual.Prepare();
                else if (kind == "clear") {
                    Require(stack.Any(x => x.Kind == "prepare") && ++clears[actor] == 1, "clear lacks exact next-round preparation"); expected = actual.Clear();
                } else { Require(call != null && Int(call["before"]["round"]) == round, "completion outside original allocation"); expected = State.Read(call["after"]["resources"]); }
                stack.Push(new Boundary { Kind = kind, Expected = expected });
            }
            Require(sequence == end && observedCompletion.Count == completions.Count, "interval or native completion did not close");
            foreach (var pair in new[] { new[] { "rider", rider }, new[] { "mount", mount } }) {
                Require(stacks[pair[1]].Count == 0 && preparations[pair[1]] == 1 && clears[pair[1]] == 1 && current[pair[1]].Matches(State.Read(after[pair[0]])), "terminal resources or exactly-once grant differ");
            }
        }
    }
}
