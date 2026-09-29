using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeDismountEscapeEvidence
    {
        private const string Mount = "f053faad986631688defa003cd7bda0e", Dismount = "3af2b81f4d72bbb30501fa730fcdf36e";
        private static void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Dismount escape: " + why); }
        private static string Text(JToken x) => x?.Type == JTokenType.String ? (string)x : null;
        private static bool Yes(JToken x) => x?.Type == JTokenType.Boolean && (bool)x;
        private static bool No(JToken x) => x?.Type == JTokenType.Boolean && !(bool)x;
        private static long Int(JToken x) { Check(x?.Type == JTokenType.Integer, "integer missing"); return (long)x; }
        private static double Number(JToken x) { Check(x != null && (x.Type == JTokenType.Float || x.Type == JTokenType.Integer), "number missing"); var n = (double)x; Check(!double.IsNaN(n) && !double.IsInfinity(n), "nonfinite number"); return n; }
        private static bool SameState(JToken a, JToken b)
        {
            // CaptureChunk6aGeometry.seconds is a diagnostic wall clock read.
            // Native frame/time, geometry, resource and identity fields remain exact.
            if (!(a is JObject) || !(b is JObject)) return false;
            var x = a.DeepClone(); var y = b.DeepClone();
            (x["geometry"] as JObject)?.Remove("seconds"); (y["geometry"] as JObject)?.Remove("seconds");
            return JToken.DeepEquals(x, y);
        }
        private static void Settings(JToken s, bool movement, bool paired)
        {
            Check((movement ? Yes(s?["movement"]) : No(s?["movement"])) && (paired ? Yes(s?["paired"]) : No(s?["paired"])) &&
                No(s?["legacyUnified"]) && No(s?["legacyScheduler"]) && No(s?["overlay"]), "setting or retired authority differs");
        }
        private static void Resources(JToken a, JToken b, bool allocation)
        {
            foreach (var f in new[] { "actor", "reactions", "reactionsPerRound", "initiativeOrder" }) {
                Check(a?[f] != null && b?[f] != null && JToken.DeepEquals(a[f], b[f]), "discrete resource differs: " + f);
            }
            foreach (var f in new[] { "standard", "move", "swift", "reactionCooldown", "initiativeCooldown" })
                Check(Math.Abs(Number(a?[f]) - Number(b?[f])) <= 0.0001, "resource boundary differs: " + f);
            Check(Int(a?["grantSequence"]) == Int(b?[allocation ? "grantSequence" : "nativePrepareCount"]), "native preparation differs");
            Check(Int(a?["actorObject"]) != 0 && Yes(a?["inCombat"]), "allocation actor unavailable");
            if (allocation) Check(Int(a["actorObject"]) == Int(b?["actorObject"]) && Yes(b?["inCombat"]), "native actor object changed");
        }
        private static void Boundary(JToken b, string rider, string mount, long generation, string state)
        {
            Check(No(b?["turnBased"]) && Yes(b?["inCombat"]) && Yes(b?["pairIdle"]), "requires idle RT encounter");
            var s = b["state"]; Check(Text(s?["relationshipState"]) == state && Int(s?["generation"]) == generation, "relationship changed");
            Check(Text(s?["rider"]?["actor"]) == rider && Text(s?["mount"]?["actor"]) == mount, "pair differs");
            Check(s?["selectedIds"] is JArray && s["selectedIds"].Count() == 1 && Text(s["selectedIds"][0]) == rider, "exact rider selection missing");
            Resources(b["riderResources"], s["rider"], false); Resources(b["mountResources"], s["mount"], false);
        }
        private static void Proof(JToken p, string ability, string rider, string target, string resultState)
        {
            Check(Yes(p?["pass"]) && Yes(p?["identityComplete"]) && Yes(p?["sameCommandAtEveryBoundary"]) && Yes(p?["exactActedObserved"]) &&
                Yes(p?["nativeTerminal"]) && Text(p?["nativeResult"]) == "Success" && Yes(p?["resourceWindow"]?["pass"]) &&
                Yes(p?["resourceWindow"]?["reactionResources"]?["pass"]), "exact command/action/reaction proof missing");
            Check(Int(p?["initCount"]) == 1 && p?["errors"] is JArray && !p["errors"].Any(), "extra command or observation error");
            var id = p["identity"];
            Check(Text(id?["abilityGuid"]) == ability && Text(id?["casterId"]) == rider && Text(id?["targetId"]) == target &&
                Text(id?["commandType"]) == "Move" && !string.IsNullOrEmpty(Text(id?["controlIdentity"])), "command binding differs");
            foreach (var f in new[] { "commandObject", "processObject", "contextObject" }) Check(Int(id[f]) != 0, "command identity incomplete");
            var terminal = p["samples"].Where(s => Text(s["boundary"]) == "terminal").ToArray();
            Check(terminal.Length == 1 && Text(terminal[0]["state"]?["relationshipState"]) == resultState, "terminal relationship differs");
            foreach (var f in new[] { "admittedMount", "acceptedMount", "admittedDismount", "acceptedDismount", "refusedVoluntary", "forcedDetach", "duplicateSuppressed", "concurrentSuppressed" }) {
                var expected = ability == Mount ? f == "admittedMount" || f == "acceptedMount" : f == "admittedDismount" || f == "acceptedDismount";
                Check(Int(p["ledgerDelta"]?[f]) == (expected ? 1 : 0), "command transition delta differs");
            }
        }
        internal static void AssertStimulus(JObject e)
        {
            Check(Text(e?["contract"]) == "settled-positive-rt-mount-disabled-setting-native-dismount", "contract differs");
            var c = Text(e["case"]); Check(c == "feature" || c == "policy", "case differs");
            Check(Text(e["scenario"]) == "chunk6a-dismount-" + c + "-disabled-rt", "scenario differs");
            var mountProof = e["mountProof"]; var rider = Text(mountProof?["identity"]?["casterId"]); var mount = Text(mountProof?["mountId"]);
            Check(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount, "pair missing");
            Proof(mountProof, Mount, rider, mount, "Mounted");
            var generation = Int(mountProof["identity"]["generationAtInit"]) + 1;
            var before = e["beforeDisable"]; var disabled = e["afterDisable"]; var click = e["beforeClick"]; var wait = (JObject)e["waitResources"];
            Check(wait != null && Yes(wait["pass"]) && No(wait["turnBased"]) && Text(wait["riderId"]) == rider && Text(wait["mountId"]) == mount, "passive wait identity differs");
            NativePassiveResourceEvidence.AssertComplete(wait);
            NativeTerminalBridgeEvidence.AssertComplete((JObject)mountProof, (JObject)before, (JObject)e["mountTerminalBridge"], false, "Mounted");
            Settings(before?["settings"], true, true);
            foreach (var b in new[] { before, disabled, click }) {
                Boundary(b, rider, mount, generation, "Mounted");
                Check(Yes(b["abilityPresent"]) && Int(b["abilityObject"]) == Int(before["abilityObject"]) && Int(b["abilityObject"]) != 0 &&
                    Text(b["abilityGuid"]) == Dismount && Text(b["abilityCasterId"]) == rider && Yes(b["visible"]), "leased Dismount identity lost");
                Check(JToken.DeepEquals(b["state"]["ledger"], before["state"]["ledger"]), "transition occurred in disabled wait");
            }
            Settings(disabled["settings"], c == "policy", c == "feature"); Settings(click["settings"], c == "policy", c == "feature");
            Check(Yes(click["enabled"]) && Int(click["frame"]) >= Int(disabled["frame"]) + 10, "disabled input not observed across frames or enabled");
            Check(Int(before["frame"]) == Int(disabled["frame"]) && Int(before["gameTicks"]) == Int(disabled["gameTicks"]) &&
                Int(before["allocationSequence"]) == Int(disabled["allocationSequence"]), "synchronous disable changed native allocation");
            foreach (var pair in new[] { new[] { "rider", "riderResources" }, new[] { "mount", "mountResources" } }) {
                Resources(before[pair[1]], disabled[pair[1]], true); Resources(before[pair[1]], wait["before"][pair[0]], true);
                Resources(wait["after"][pair[0]], click[pair[1]], true);
            }
            foreach (var f in new[] { "frame", "gameTicks", "allocationSequence" }) {
                Check(Int(before[f]) == Int(wait["before"][f]), "wait baseline gap"); Check(Int(click[f]) == Int(wait["after"][f]), "wait terminal gap");
            }
        }
        internal static void AssertComplete(JObject e)
        {
            AssertStimulus(e);
            var mountProof = e["mountProof"]; var p = e["dismountProof"]; var click = e["beforeClick"];
            var rider = Text(mountProof["identity"]["casterId"]); var mount = Text(mountProof["mountId"]);
            var generation = Int(click["state"]["generation"]);
            Proof(p, Dismount, rider, rider, "Unmounted");
            Check(Yes(e["clicked"]) && Yes(e["input"]?["clicked"]) && Text(e["input"]?["abilityGuid"]) == Dismount &&
                Text(e["input"]?["clickedTargetId"]) == rider && Text(e["input"]?["resolvedTargetId"]) == rider, "native click missing");
            Check(Int(p["identity"]["generationAtInit"]) == generation, "Dismount generation differs");
            foreach (var f in new[] { "commandObject", "processObject", "contextObject", "controlIdentity" })
                Check(!JToken.DeepEquals(p["identity"][f], mountProof["identity"][f]), "commands alias");
            foreach (var f in new[] { "frame", "gameTicks", "allocationSequence" })
                Check(Int(click[f]) == Int(p["preClick"][f]), "Dismount pre-click gap");
            Check(SameState(click["state"], p["preClick"]["state"]), "Dismount baseline differs");
            var after = e["afterDismount"]; var restored = e["restored"];
            Boundary(after, rider, mount, generation, "Unmounted"); Boundary(restored, rider, mount, generation, "Unmounted");
            NativeTerminalBridgeEvidence.AssertComplete((JObject)p, (JObject)after, (JObject)e["dismountTerminalBridge"], false, "Unmounted");
            Settings(after["settings"], Text(e["case"]) == "policy", Text(e["case"]) == "feature"); Settings(restored["settings"], true, true);
            var restoredState = restored["state"].DeepClone();
            if (Text(e["case"]) == "feature")
            {
                // Re-enabling movement after an exact Dismount re-leases only the owner's Mount fact.
                // Keep the complete shell description exact except this declared false-to-true field.
                const string absent = ";mountAbilityFactPresent=False;", present = ";mountAbilityFactPresent=True;";
                var previous = Text(after["state"]?["geometry"]?["shellState"]);
                var current = Text(restoredState["geometry"]?["shellState"]);
                Check(previous != null && previous.Split(new[] { ";mountAbilityFactPresent=" }, StringSplitOptions.None).Length == 2 &&
                    previous.Contains(absent) &&
                    current == previous.Replace(absent, present), "expected exact Mount fact regrant differs");
                restoredState["geometry"]["shellState"] = previous;
            }
            Check(SameState(after["state"], restoredState), "setting restoration changed native state");
            foreach (var f in new[] { "frame", "gameTicks", "allocationSequence" }) Check(Int(after[f]) == Int(restored[f]), "restoration boundary gap");
            foreach (var f in new[] { "admittedMount", "acceptedMount", "admittedDismount", "acceptedDismount", "refusedVoluntary", "forcedDetach", "duplicateSuppressed", "concurrentSuppressed" })
                Check(Int(after["state"]["ledger"][f]) - Int(click["state"]["ledger"][f]) == (f == "admittedDismount" || f == "acceptedDismount" ? 1 : 0), "Dismount transition count differs");
        }
    }
}
