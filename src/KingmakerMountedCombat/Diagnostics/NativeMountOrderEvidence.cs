using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeMountOrderEvidence
    {
        private static void Require(bool ok, string why) { if (!ok) throw new InvalidOperationException("Mount allocation order: " + why); }
        private static long Int(JToken x) { Require(x?.Type == JTokenType.Integer, "integer missing"); return (long)x; }
        private static bool Bool(JToken x) { Require(x?.Type == JTokenType.Boolean, "boolean missing"); return (bool)x; }
        private static string Text(JToken x) => x?.Type == JTokenType.String ? (string)x : null;
        internal static bool CanPrepareFixture(bool owned, bool riderCombat, bool mountCombat, bool partyCombat,
            bool unmounted, bool idle, bool targetTurnBased, bool modeLeaseCurrent)
            => !owned && !riderCombat && !mountCombat && !partyCombat && unmounted && idle && targetTurnBased && modeLeaseCurrent;
        internal static void AssertComplete(JObject e)
        {
            Require(Text(e?["contract"]) == "fresh-native-allocation-order-through-next-paired-round", "contract differs");
            var first = Bool(e["riderFirst"]); var rider = Text(e["riderId"]); var mount = Text(e["mountId"]);
            Require(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && rider != mount &&
                Text(e["scenario"]) == "chunk6a-allocation-" + (first ? "rider-first" : "mount-first") + "-tb", "case/pair differs");
            var fixture = e["fixture"]; var proof = e["positiveProof"]; var mounted = e["mounted"]; var next = e["nextRound"]; var before = e["mountBefore"];
            Require(Bool(fixture?["outsideCombat"]) && Bool(fixture["targetTurnBased"]) && Bool(fixture["modeLeaseCurrent"]) && Text(fixture["relationshipState"]) == "Unmounted" &&
                Int(fixture["riderInputBase"]) == (first ? 40 : -40) && Int(fixture["mountInputBase"]) == (first ? -40 : 40) &&
                JToken.DeepEquals(fixture["beforeResources"], fixture["afterResources"]), "native pre-encounter input changed resources");
            foreach (var actor in new[] { "rider", "mount" }) Require(Int(fixture["beforeResources"][actor]["grantSequence"]) == 0 && !Bool(fixture["beforeResources"][actor]["inCombat"]), "fixture reused an allocation");
            Require(Bool(proof?["pass"]) && Bool(proof["identityComplete"]) && Bool(proof["sameCommandAtEveryBoundary"]) && Bool(proof["exactActedObserved"]) &&
                Bool(proof["nativeTerminal"]) && Text(proof["nativeResult"]) == "Success" && Bool(proof["resourceWindow"]["pass"]) &&
                Bool(proof["resourceWindow"]["reactionResources"]["pass"]), "positive Mount proof incomplete");
            var id = proof["identity"];
            Require(Text(id["casterId"]) == rider && Text(id["targetId"]) == mount && Text(id["abilityGuid"]) == "f053faad986631688defa003cd7bda0e" &&
                Text(id["commandType"]) == "Move" && Int(proof["initCount"]) == 1, "one exact native Mount absent");
            var terminal = proof["samples"].OfType<JObject>().Single(s => Text(s["boundary"]) == "terminal");
            var bridge = (JObject)e["terminalBridge"];
            NativePassiveResourceEvidence.AssertComplete(bridge);
            Require(Text(bridge["riderId"]) == rider && Text(bridge["mountId"]) == mount && Bool(bridge["turnBased"]), "terminal bridge pair/mode differs");
            foreach (var key in new[] { "frame", "gameTicks", "allocationSequence" }) {
                Require(Int(bridge["before"][key]) == Int(terminal[key]) && Int(bridge["after"][key]) == Int(mounted[key]), "terminal bridge clock/sequence differs");
            }
            Require(JToken.DeepEquals(terminal["state"]["ledger"], mounted["state"]["ledger"]) &&
                Int(terminal["state"]["generation"]) == Int(mounted["state"]["generation"]) && Text(terminal["state"]["relationshipState"]) == "Mounted", "terminal bridge relationship differs");
            foreach (var role in new[] { "rider", "mount" }) {
                Require(JToken.DeepEquals(bridge["before"][role], terminal["nativeAllocation"][role]) &&
                    JToken.DeepEquals(bridge["after"][role], mounted[role + "Resources"]), "terminal bridge resource copies differ");
                foreach (var key in new[] { "actor", "standard", "move", "swift", "reactions", "reactionCooldown", "initiativeCooldown", "initiativeOrder", "reactionsPerRound" }) {
                    Require(terminal["state"][role][key] != null && JToken.DeepEquals(terminal["state"][role][key], terminal["nativeAllocation"][role][key]), "terminal allocation differs from exact command " + role + " " + key);
                }
                Require(Int(terminal["state"][role]["nativePrepareCount"]) == Int(terminal["nativeAllocation"][role]["grantSequence"]), "terminal preparation count differs");
            }
            var round = Int(mounted?["round"]); var turn = Int(mounted["turnObject"]); var generation = Int(id["generationAtInit"]) + 1;
            Require(turn != 0 && Int(terminal["state"]["rider"]["nativeTurnObject"]) == turn && Int(before["round"]) == round && Text(before["currentTurnActor"]) == rider && Text(mounted["currentActor"]) == rider &&
                Text(mounted["status"]) == "Acting" && Int(before["rider"]["nativeTurnObject"]) == turn, "adopted rider context differs");
            Require(Text(e["disposition"]) == (first ? "PreparePartnerThisRound" : "RetainPartnerParticipation"), "wrong adoption disposition");
            var roster = mounted["roster"].Select(x => Text(x["actor"])).ToArray();
            var ri = Array.IndexOf(roster, rider); var mi = Array.IndexOf(roster, mount);
            Require(ri >= 0 && mi >= 0 && ri != mi && (ri < mi) == first && roster.Distinct().Count() == roster.Length &&
                Int(before["riderRosterIndex"]) == ri && Int(before["mountRosterIndex"]) == mi, "observed native roster differs from declared order");
            Require(Int(before["rider"]["nativePrepareCount"]) == 1 && Int(before["mount"]["nativePrepareCount"]) == (first ? 0 : 1), "pre-Mount participation differs");
            Require((Int(mounted["partnerContextObject"]) != 0) == first && (first ? Text(mounted["partnerActor"]) == mount : mounted["partnerActor"].Type == JTokenType.Null), "retained versus prepared partner context differs");
            foreach (var b in new[] { mounted, e["beforeEndInput"], e["afterEndInput"], next }) {
                Require(Int(b["controllerObject"]) == Int(mounted["controllerObject"]) && Int(b["sessionObject"]) == Int(mounted["sessionObject"]) &&
                    Bool(b["turnBased"]) && Text(b["state"]["relationshipState"]) == "Mounted" && Int(b["state"]["generation"]) == generation &&
                    Int(b["adoptionCount"]) == 1 && !Bool(b["pairedSplit"]), "encounter, pair or adoption changed");
                Require(JToken.DeepEquals(b["state"]["ledger"], mounted["state"]["ledger"]), "another relationship transition occurred");
            }
            Require(Int(mounted["pairedSequence"]) == 1 && Int(next["pairedSequence"]) == 2 && Int(next["round"]) == round + 1 &&
                Text(next["currentActor"]) == rider && Int(next["turnObject"]) != turn && Int(next["turnObject"]) != 0 &&
                Text(next["partnerActor"]) == mount && Int(next["partnerContextObject"]) != 0 && !Bool(next["pairedFinalized"]), "exact next paired activation absent");
            foreach (var role in new[] { "riderResources", "mountResources" }) Require(Int(mounted[role]["grantSequence"]) == 1 && Int(next[role]["grantSequence"]) == 2, "exactly-once preparations differ");
            var input = e["input"];
            Require(Text(input?["method"]) == "Kingmaker.Game.PauseBind" && Text(input["token"]) == "06000CB7" &&
                Text(input["moduleMvid"]) == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7" && Int(input["count"]) == 1 &&
                Int(e["beforeEndInput"]["turnObject"]) == turn && Int(e["beforeEndInput"]["round"]) == round && Text(e["beforeEndInput"]["status"]) == "Acting", "native End Turn input differs");
            var trace = e["allocationTrace"]; Require(Int(trace?["dropped"]) == 0 && Int(trace["observationErrors"]) == 0 && trace["events"] is JArray, "native trace incomplete");
            var events = trace["events"].ToArray(); long seq = 0;
            foreach (var item in events) Require(Int(item["sequence"]) == ++seq, "native trace sequence gap");
            Require(seq == Int(next["allocationSequence"]), "trace does not end at next-round observation");
            var bridgeEvents = new JArray(events.Where(x => Int(x["sequence"]) > Int(bridge["before"]["allocationSequence"]) && Int(x["sequence"]) <= Int(bridge["after"]["allocationSequence"])));
            Require(JToken.DeepEquals(bridge["events"], bridgeEvents), "terminal bridge differs from complete trace");
            foreach (var actor in new[] { rider, mount }) foreach (var r in new[] { round, round + 1 }) foreach (var phase in new[] { "before", "after" })
                Require(events.Count(x => Text(x["boundary"]) == "prepare-" + phase && Text(x["state"]?["actor"]) == actor && Int(x["round"]) == r) == 1, "native preparation count differs by actor/round");
            var riderPrepare = events.Single(x => Text(x["boundary"]) == "prepare-before" && Text(x["state"]?["actor"]) == rider && Int(x["round"]) == round);
            var priorEnds = events.Where(x => Text(x["boundary"]) == "turn-end-after" && Text(x["state"]?["actor"]) == mount && Int(x["round"]) == round && Int(x["sequence"]) < Int(proof["preClick"]["allocationSequence"])).ToArray();
            Require(priorEnds.Length == (first ? 0 : 1) && (first || Int(priorEnds[0]["sequence"]) < Int(riderPrepare["sequence"])), "earlier mount slot was not observed ending before rider preparation");
            var turns = e["turns"].ToArray(); Require(turns.Length > 0 && turns.Length <= 128, "native turn observations missing");
            Require(!turns.Any(x => Text(x["currentActor"]) == mount && Int(x["allocationSequence"]) >= Int(mounted["allocationSequence"])), "mounted partner received another native slot");
            Require(turns.Any(x => Text(x["currentActor"]) == mount && Int(x["round"]) == round) == !first, "actual native mount slot order not observed");
            var calls = e["completion"]?["calls"]?.ToArray(); Require(calls != null, "native completion calls absent");
            foreach (var actor in new[] { rider, mount }) foreach (var kind in new[] { "force-to-end", "end" }) {
                var expected = actor == rider || first ? 1 : 0;
                Require(calls.Count(x => Text(x["kind"]) == kind && Text(x["before"]["actorId"]) == actor) == expected, "adopted context completion count differs");
                foreach (var c in calls.Where(x => Text(x["kind"]) == kind && Text(x["before"]["actorId"]) == actor))
                    Require(Int(c["before"]["turnObject"]) == (actor == rider ? turn : Int(mounted["partnerContextObject"])), "completion used another adopted context");
            }
            var continuation = (JObject)e["continuation"];
            Require(JToken.DeepEquals(e["completion"], continuation["completion"]), "completion record differs");
            NativeAllocationContinuationEvidence.AssertComplete(continuation);
            Require(Text(continuation["riderId"]) == rider && Text(continuation["mountId"]) == mount &&
                Int(continuation["before"]["allocationSequence"]) == Int(mounted["allocationSequence"]) &&
                Int(continuation["after"]["allocationSequence"]) == Int(next["allocationSequence"]), "continuation interval differs");
            foreach (var pair in new[] { new[] { "rider", "riderResources" }, new[] { "mount", "mountResources" } }) {
                Require(JToken.DeepEquals(continuation["before"][pair[0]], mounted[pair[1]]) && JToken.DeepEquals(continuation["after"][pair[0]], next[pair[1]]), "continuation actor boundaries differ");
            }
        }
        internal static void AssertRestoration(JObject e)
        {
            var r = e?["restoration"]; var f = e?["fixture"];
            Require(Bool(r?["exact"]) && Bool(r["outsideCombat"]) && Int(r["riderBase"]) == Int(f?["riderOriginalBase"]) &&
                Int(r["mountBase"]) == Int(f["mountOriginalBase"]), "pre-encounter input not restored exactly");
        }
    }
}
