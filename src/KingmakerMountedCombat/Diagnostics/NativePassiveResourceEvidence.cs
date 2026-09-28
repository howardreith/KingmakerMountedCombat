using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    // Additional fixture window: no command is owned here. This cannot replace any
    // exact Mount/Dismount cost proof or authorize a gameplay resource write.
    internal static class NativePassiveResourceEvidence
    {
        private static void Require(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Passive native resources: " + reason); }
        private static string Text(JToken v) => v?.Type == JTokenType.String ? (string)v : null;
        private static long Int(JToken v) { Require(v?.Type == JTokenType.Integer, "integer field missing"); return (long)v; }
        private static bool Bool(JToken v) { Require(v?.Type == JTokenType.Boolean, "boolean field missing"); return (bool)v; }
        private static double Number(JToken v)
        { Require(v != null && (v.Type == JTokenType.Float || v.Type == JTokenType.Integer), "number missing"); var n = (double)v; Require(!double.IsNaN(n) && !double.IsInfinity(n), "nonfinite number"); return n; }
        private sealed class Resources
        {
            internal double Standard, Move, Swift;
            internal NativeReactionResources Reaction;
            internal int PerRound;
            internal long Grants, Object;
            internal string Actor;
            internal bool InCombat;
            internal static Resources Read(JToken s) => new Resources {
                Standard = Number(s?["standard"]), Move = Number(s?["move"]), Swift = Number(s?["swift"]),
                Reaction = new NativeReactionResources((int)Int(s?["reactions"]), Number(s?["reactionCooldown"]), Number(s?["initiativeCooldown"]), (int)Int(s?["initiativeOrder"])),
                PerRound = (int)Int(s?["reactionsPerRound"]), Grants = Int(s?["grantSequence"]), Object = Int(s?["actorObject"]), Actor = Text(s?["actor"]), InCombat = Bool(s?["inCombat"])
            };
            internal bool Matches(Resources x) => x != null && Actor == x.Actor && Object == x.Object && Object != 0 && InCombat == x.InCombat &&
                Grants == x.Grants && PerRound == x.PerRound && Reaction.Matches(x.Reaction) && Math.Abs(Standard - x.Standard) <= 0.0001 &&
                Math.Abs(Move - x.Move) <= 0.0001 && Math.Abs(Swift - x.Swift) <= 0.0001;
            internal Resources Tick(JToken e)
            {
                var delta = Number(e["gameDeltaTime"]); Require(delta >= 0, "negative native delta");
                var tb = Bool(e["nativeTurnBased"]); var passing = Bool(e["nativePassing"]); var surprised = Bool(e["nativeSurprised"]);
                var waiting = Bool(e["state"]?["waitingInitiative"]);
                var reaction = Reaction.Tick(delta, tb, InCombat, passing, surprised, waiting, PerRound);
                var decay = delta;
                if (tb && InCombat) {
                    if (!passing) decay = 0;
                    else if (Reaction.InitiativeCooldown > 0 && !surprised) decay = Math.Max(0, decay - Reaction.InitiativeCooldown);
                } else if ((tb && !passing) || waiting) decay = 0;
                return new Resources { Actor = Actor, Object = Object, InCombat = InCombat, Grants = Grants, PerRound = PerRound, Reaction = reaction,
                    Standard = decay > 0 ? Math.Max(0, Standard - decay) : Standard,
                    Move = decay > 0 ? Math.Max(0, Move - decay) : Move,
                    Swift = decay > 0 ? Math.Max(0, Swift - decay) : Swift };
            }
        }
        internal static void AssertComplete(JObject p) => AssertWindow(p, false);
        internal static void AssertGround(JObject p) => AssertWindow(p, true);
        private static void AssertWindow(JObject p, bool ground)
        {
            Require(Text(p?["contract"]) == (ground ? "one-native-ground-command-outside-combat-with-observed-time-only" : "same-allocation-no-command-cost-with-observed-native-time-only"), "contract differs");
            Require(Bool(p["traceComplete"]), "allocation trace incomplete");
            var tb = Bool(p["turnBased"]); var before = p["before"]; var after = p["after"];
            var rider = Text(p["riderId"]); var mount = Text(p["mountId"]);
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount, "pair identity missing");
            Require(p["events"] is JArray && p["observerHooks"] is JArray, "events/hooks missing");
            foreach (var token in new[] { "0600934A", "060093A1", "060093A4", "06000C3C", "0600C3BE", "06009120", "0600838F", "060026B2", "06000C37" }) {
                var hooks = p["observerHooks"].Where(x => Text(x["token"]) == token).ToArray();
                Require(hooks.Length == 1 && Text(hooks[0]["moduleMvid"]) == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7", "required native hook absent");
            }
            long groundCommand = 0; string groundActor = null;
            if (ground) {
                groundCommand = Int(p["groundCommand"]?["commandObject"]); groundActor = Text(p["groundCommand"]?["casterId"]);
                Require(!tb && groundCommand != 0 && (groundActor == rider || groundActor == mount), "ground input identity/mode differs");
                foreach (var role in new[] { "rider", "mount" }) foreach (var s in new[] { before[role], after[role] })
                    Require(!Bool(s["inCombat"]) && Int(s["grantSequence"]) == 0, "ground setup reused an encounter allocation");
                var controls = p["events"].Where(e => (Text(e["state"]?["actor"]) == rider || Text(e["state"]?["actor"]) == mount) &&
                    ((Text(e["boundary"]) ?? "").Contains("admission") || (Text(e["boundary"]) ?? "").Contains("cost"))).ToArray();
                var expected = new[] { "admission-before", "admission-after", "cost-before", "cost-after" };
                Require(controls.Length == expected.Length, "ground admission/cost callback count differs");
                for (var i = 0; i < controls.Length; i++) {
                    var e = controls[i];
                    Require(Text(e["boundary"]) == expected[i] && Int(e["command"]) == groundCommand && Text(e["state"]["actor"]) == groundActor &&
                        Text(e["commandType"]) == "Kingmaker.UnitLogic.Commands.UnitMoveTo" && Text(e["actionType"]) == "Move" &&
                        !Bool(e["state"]["inCombat"]) && !Bool(e["simulatingClick"]) && Bool(e["acted"]) == (i >= 2) &&
                        (i == 0 ? Text(e["commandActor"]) == null : Text(e["commandActor"]) == groundActor), "ground exact callback identity differs");
                }
            }
            var start = Int(before?["allocationSequence"]); var end = Int(after?["allocationSequence"]);
            var startTicks = Int(before?["gameTicks"]); var endTicks = Int(after?["gameTicks"]);
            Require(start >= 0 && end >= start && startTicks >= 0 && endTicks >= startTicks && Int(after["frame"]) >= Int(before["frame"]), "invalid window order");
            var current = new Dictionary<string, Resources>(); var pending = new Dictionary<string, Resources>(); var ticks = new Dictionary<string, int>();
            foreach (var pair in new[] { new[] { "rider", rider }, new[] { "mount", mount } }) {
                var state = Resources.Read(before[pair[0]]); Require(state.Actor == pair[1] && state.Object != 0, "baseline identity differs");
                current.Add(pair[1], state); ticks.Add(pair[1], 0);
            }
            Require(current[rider].Object != current[mount].Object, "pair objects alias");
            var sequence = start; var time = startTicks;
            foreach (var e in p["events"]) {
                Require(Int(e["sequence"]) == ++sequence && Int(e["gameTicks"]) >= time && Int(e["gameTicks"]) <= endTicks, "event order/gap differs");
                time = Int(e["gameTicks"]);
                var actor = Text(e["state"]?["actor"]); Require(!string.IsNullOrEmpty(actor), "event actor missing"); if (!current.ContainsKey(actor)) continue;
                Require(Bool(e["nativeTurnBased"]) == tb, "native mode changed");
                var boundary = Text(e["boundary"]); Require(boundary != null, "boundary missing");
                var groundBoundary = ground && (boundary == "admission-before" || boundary == "admission-after" || boundary == "cost-before" || boundary == "cost-after");
                Require(groundBoundary || !new[] { "cost", "prepare", "clear", "opportunity", "admission", "movement", "turn-end" }.Any(boundary.Contains), "native cost/grant/reaction/movement or allocation change occurred");
                var actual = Resources.Read(e["state"]);
                if (boundary == "cooldown-tick-after") {
                    Require(pending.ContainsKey(actor) && pending[actor].Matches(actual), "native cooldown effect differs");
                    pending.Remove(actor); current[actor] = actual; ticks[actor]++; continue;
                }
                if (pending.ContainsKey(actor)) Require(pending[actor].Matches(actual), "nested native callback changed resources");
                else Require(current[actor].Matches(actual), "resource changed without a native tick");
                if (boundary == "cooldown-tick-before") {
                    Require(!pending.ContainsKey(actor), "nested cooldown tick"); pending.Add(actor, actual.Tick(e));
                }
            }
            Require(sequence == end && pending.Count == 0, "trace did not close");
            foreach (var pair in new[] { new[] { "rider", rider }, new[] { "mount", mount } }) {
                Require(current[pair[1]].Matches(Resources.Read(after[pair[0]])), "terminal resources unexplained");
                Require(endTicks == startTicks || ticks[pair[1]] > 0, "elapsed window has no native tick observation for an actor");
            }
        }
    }
}
