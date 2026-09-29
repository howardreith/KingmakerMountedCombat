using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    // Additional evidence for unused participation relinquished through the real
    // End Turn input immediately after a settled combat Mount. Read-only.
    internal static class NativeEarlyEndMountEvidence
    {
        private static void Need(bool pass, string why) { if (!pass) throw new InvalidOperationException("Early End after Mount: " + why); }
        private static long Int(JToken t) { Need(t?.Type == JTokenType.Integer, "integer absent"); return (long)t; }
        private static bool Bool(JToken t) { Need(t?.Type == JTokenType.Boolean, "boolean absent"); return (bool)t; }
        private static string Text(JToken t) => t?.Type == JTokenType.String ? (string)t : null;
        private static readonly string[] ResourceFields = { "actor", "actorObject", "inCombat", "standard", "move", "swift", "reactions", "reactionCooldown", "initiativeCooldown", "initiativeOrder", "reactionsPerRound", "grantSequence", "hasSwift" };
        private static void SameResources(JToken a, JToken b, string why)
        {
            foreach (var f in ResourceFields) Need(a?[f] != null && a[f].Type != JTokenType.Null && b?[f] != null && JToken.DeepEquals(a[f], b[f]), why + ": " + f);
        }
        internal static void AssertComplete(JObject e)
        {
            NativeMountOrderEvidence.AssertComplete(e);
            var allowance = e["earlyEndAllowance"];
            Need(Text(allowance?["action"]) == "Swift" &&
                Text(allowance["method"]) == "Kingmaker.EntitySystem.Entities.UnitEntityData.HasSwiftAction" &&
                Text(allowance["token"]) == "06008380" &&
                Text(allowance["moduleMvid"]) == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7" &&
                Text(allowance["ilSha256"]) == "65061d8c8cccff181626338782db42cc3504357af1bc2c3121fb9dc4a49a09a8",
                "fixed native Swift allowance contract differs");
            var mounted = e["mounted"]; var before = e["beforeEndInput"]; var after = e["afterEndInput"];
            var rider = Text(e["riderId"]); var turn = Int(mounted["turnObject"]);
            foreach (var b in new[] { before, after }) {
                foreach (var k in new[] { "turnObject", "round", "pairedSequence", "gameTicks" }) Need(Int(b[k]) == Int(mounted[k]), "input left the adopted allocation: " + k);
                Need(Text(b["currentActor"]) == rider, "input no longer belongs to rider");
            }
            Need(Text(before["status"]) == "Acting" && Text(after["status"]) == "Ending", "input did not end Acting");
            Need(Int(before["frame"]) >= Int(mounted["frame"]) && Int(after["frame"]) == Int(before["frame"]), "synchronous input frame differs");
            Need(Int(before["allocationSequence"]) >= Int(mounted["allocationSequence"]), "input precedes successful Mount");
            Need(Bool(before["riderResources"]["hasSwift"]), "rider had no observed native Swift remainder to relinquish");
            foreach (var role in new[] { "rider", "mount" }) SameResources(mounted[role + "Resources"], before[role + "Resources"], "resources changed between Mount and first End input");
            var calls = e["completion"]["calls"].Where(c => Text(c["kind"]) == "force-to-end" && Text(c["before"]["actorId"]) == rider).ToArray();
            Need(calls.Length == 1 && Bool(calls[0]["setCooldowns"]), "exact ordinary rider ForceToEnd(true) missing");
            var call = calls[0];
            Need(Int(call["before"]["turnObject"]) == turn && Int(call["before"]["allocationSequence"]) > Int(before["allocationSequence"]) &&
                 Int(call["after"]["allocationSequence"]) <= Int(after["allocationSequence"]), "completion did not occur inside the one input");
            foreach (var k in new[] { "frame", "gameTicks" }) Need(Int(call["before"][k]) == Int(before[k]) && Int(call["after"][k]) == Int(after[k]), "input/callback clock differs");
            SameResources(call["before"]["resources"], before["riderResources"], "native End entry resources differ");
            SameResources(call["after"]["resources"], after["riderResources"], "native End exit resources differ");
            Need(Bool(call["before"]["resources"]["hasSwift"]), "native End did not receive the unused Swift allowance");
            Need(!Bool(call["after"]["resources"]["hasSwift"]) && !Bool(after["riderResources"]["hasSwift"]),
                "native End did not relinquish the observed Swift allowance");
            var partner = e["completion"]["calls"].Where(c => Text(c["kind"]) == "force-to-end" && Text(c["before"]["actorId"]) == Text(e["mountId"])).ToArray();
            if (Bool(e["riderFirst"])) {
                Need(partner.Length == 1 && Int(partner[0]["enterOrdinal"]) > Int(call["enterOrdinal"]) && Int(partner[0]["exitOrdinal"]) < Int(call["exitOrdinal"]), "partner completion not owned by the same End input");
                SameResources(partner[0]["before"]["resources"], before["mountResources"], "partner End entry differs");
                SameResources(partner[0]["after"]["resources"], after["mountResources"], "partner End exit differs");
            } else {
                Need(partner.Length == 0, "spent partner completed again");
                SameResources(before["mountResources"], after["mountResources"], "spent partner changed at End input");
            }
        }
    }
}