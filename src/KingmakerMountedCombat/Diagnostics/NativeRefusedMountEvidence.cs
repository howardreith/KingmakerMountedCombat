using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Pure diagnostic decision. No native objects, resource writes or gameplay admission.
    internal static class NativeRefusedMountEvidence
    {
        private const string MountGuid = "f053faad986631688defa003cd7bda0e";
        private static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException("Refused Mount evidence: " + reason); }
        private static string Text(JToken v) => v?.Type == JTokenType.String ? (string)v : null;
        private static long Integer(JToken v)
        { Require(v?.Type == JTokenType.Integer, "expected exact integer"); return (long)v; }
        private static double Number(JToken v)
        {
            Require(v != null && (v.Type == JTokenType.Float || v.Type == JTokenType.Integer), "expected numeric resource");
            var n = (double)v; Require(!double.IsNaN(n) && !double.IsInfinity(n), "nonfinite resource"); return n;
        }
        private static bool Flag(JToken v) => v?.Type == JTokenType.Boolean && (bool)v;
        private static JArray Array(JToken v)
        { Require(v is JArray, "expected evidence array"); return (JArray)v; }
        private static void Same(JToken a, JToken b, string field)
        { Require(a != null && b != null && JToken.DeepEquals(a, b), "changed " + field); }
        private static void Selection(JToken state, string[] expected)
        { Require(Array(state["selectedIds"]).Select(Text).SequenceEqual(expected), "exact declared selection differs"); }
        internal static void AssertState(JToken a, JToken b)
        {
            Require(Text(a?["relationshipState"]) == "Unmounted" && Text(b?["relationshipState"]) == "Unmounted", "relationship not unmounted");
            Require(Integer(a["generation"]) == Integer(b["generation"]), "generation changed");
            Require(a["ledger"] is JObject && b["ledger"] is JObject, "ledger missing");
            Same(a["ledger"], b["ledger"], "ledger");
            foreach (var actor in new[] { "rider", "mount" })
                foreach (var field in new[] { "standard", "move", "swift", "reactions", "reactionCooldown", "initiativeCooldown", "initiativeOrder", "reactionsPerRound", "nativePrepareCount" })
                    Require(Number(a[actor]?[field]) == Number(b[actor]?[field]), actor + " " + field + " changed");
            foreach (var actor in new[] { "riderPosition", "horsePosition" })
                foreach (var axis in new[] { "x", "y", "z" })
                    Require(Number(a["geometry"]?[actor]?[axis]) == Number(b["geometry"]?[actor]?[axis]), "actor moved");
        }
        internal static void AssertInput(JObject p, string rider, string mount, string target, string[] selected)
        {
            Require(p != null && Text(p["contract"]) == "synchronous-refused-native-mount-with-zero-command-construction", "input contract differs");
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && !string.IsNullOrEmpty(target) && rider != mount, "actor IDs missing");
            Require(Text(p["riderId"]) == rider && Text(p["mountId"]) == mount && Text(p["targetId"]) == target, "input actors differ");
            Require(Flag(p["traceComplete"]) && Array(p["errors"]).Count == 0 && Array(p["constructedCommands"]).Count == 0, "incomplete observer or constructed command");
            var hooks = Array(p["observerHooks"]); Require(hooks.Count == 3, "hook count differs");
            foreach (var token in new[] { "06002726", "06002727", "060093F6" })
            {
                var h = hooks.Single(x => Text(x["token"]) == token);
                var click = token == "060093F6";
                Require(Text(h["moduleMvid"]) == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7" &&
                    Text(h["method"]) == (click ? "Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler.OnClick" : "Kingmaker.UnitLogic.Commands.UnitUseAbility..ctor") &&
                    Text(h["prefix"]) == (click ? "ClickBefore" : null) && Text(h["postfix"]) == (click ? "ClickAfter" : "Constructed"), "pinned wrapper differs");
            }
            var b = p["before"]; var a = p["after"];
            foreach (var s in new[] { b, a })
            {
                foreach (var field in new[] { "frame", "gameTicks", "allocationSequence", "activationSequence", "shellCount", "processBindings", "castCount", "refusalCount", "targetStartCount", "targetEndCount", "dispatchAccepted", "dispatchRejected" })
                    Require(Integer(s?[field]) >= 0, "negative counter");
                Require(Flag(s["targetSelectorEmpty"]) && Flag(s["riderCommandsEmpty"]) && Flag(s["mountCommandsEmpty"]), "command or selector residue");
                Selection(s["state"], selected);
            }
            foreach (var field in new[] { "frame", "gameTicks", "shellCount", "processBindings", "castCount", "dispatchAccepted", "dispatchRejected" })
                Require(Integer(b[field]) == Integer(a[field]), "changed " + field);
            foreach (var field in new[] { "refusalCount", "targetStartCount", "targetEndCount" })
                Require(Integer(a[field]) == Integer(b[field]) + 1, "not one " + field);
            AssertState(b["state"], a["state"]);
            var events = Array(p["events"]); Require(events.Count == 2, "click boundary count differs");
            Require(Text(events[0]["boundary"]) == "click-before" && Text(events[1]["boundary"]) == "click-after" &&
                events[0]["result"]?.Type == JTokenType.Null && events[1]["result"]?.Type == JTokenType.Boolean && !(bool)events[1]["result"], "click not refused");
            foreach (var e in events)
            {
                Require(Integer(e["frame"]) == Integer(b["frame"]) && Integer(e["gameTicks"]) == Integer(b["gameTicks"]), "click clock differs");
                Require(Text(e["casterId"]) == rider && Text(e["targetId"]) == target && Text(e["abilityGuid"]) == MountGuid, "click identity differs");
                Require(Integer(e["handlerObject"]) != 0 && Integer(e["abilityObject"]) != 0 &&
                    Integer(e["handlerObject"]) == Integer(events[0]["handlerObject"]) && Integer(e["abilityObject"]) == Integer(events[0]["abilityObject"]) &&
                    Integer(e["currentSelectorAbilityObject"]) == Integer(e["abilityObject"]), "exact handler/ability differs");
                Require(Flag(e["exactTargetObject"]) && Flag(e["exactTargetPoint"]) && Integer(e["button"]) == 0 &&
                    e["simulate"]?.Type == JTokenType.Boolean && !Flag(e["simulate"]) && e["muteEvents"]?.Type == JTokenType.Boolean && !Flag(e["muteEvents"]), "native click arguments differ");
            }
            var sequence = Integer(b["allocationSequence"]);
            foreach (var e in Array(p["allocationEvents"]))
            {
                Require(Integer(e["sequence"]) == ++sequence, "allocation sequence gap");
                if (Text(e["state"]?["actor"]) != rider && Text(e["state"]?["actor"]) != mount) continue;
                var name = Text(e["boundary"]);
                Require(name != null && !new[] { "admission", "cost", "prepare", "clear", "opportunity", "movement" }.Any(name.Contains), "native resource/command event");
            }
            Require(sequence == Integer(a["allocationSequence"]), "allocation window not closed");
            var activations = Array(p["activations"]);
            Require(activations.Count == 3 && activations.Select(x => Text(x["phase"])).SequenceEqual(new[] { "TargetSelectionStarted", "CastRefused", "TargetSelectionEnded" }), "activation phases differ");
            var refused = activations[1];
            Require(Text(refused["casterId"]) == rider && Text(refused["targetId"]) == target && Text(refused["selectedIds"]) == string.Join(",", selected) &&
                !string.IsNullOrWhiteSpace(Text(refused["reason"])) && Integer(refused["frame"]) == Integer(b["frame"]) && Text(refused["abilityGuid"]) == MountGuid, "native refusal identity/reason differs");
            sequence = Integer(b["activationSequence"]);
            foreach (var e in activations) Require(Integer(e["sequence"]) == ++sequence, "activation sequence gap");
            Require(sequence == Integer(a["activationSequence"]), "activation window not closed");
        }
    }
}
