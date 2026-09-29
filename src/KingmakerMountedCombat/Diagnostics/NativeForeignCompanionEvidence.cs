using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    // Additional diagnostic contract only. The selected rider and owned mount stay unchanged.
    internal static class NativeForeignCompanionEvidence
    {
        private static void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Foreign companion evidence: " + why); }
        private static string Text(JToken v) => v?.Type == JTokenType.String ? (string)v : null;
        private static bool Yes(JToken v) => v?.Type == JTokenType.Boolean && (bool)v;
        private static long Int(JToken v) { Check(v?.Type == JTokenType.Integer, "integer missing"); return (long)v; }
        internal static void AssertComplete(JObject e)
        {
            var id = e?["identity"]; var before = e?["foreignCompanionBefore"]; var after = e?["foreignCompanionAfter"];
            Check(Text(e?["case"]) == "foreign-companion" && Yes(id?["unrelatedIsPet"]) && Yes(id["unrelatedSupportedMount"]), "declared supported foreign pet missing");
            Check(before is JObject && after is JObject && JToken.DeepEquals(before, after), "native foreign pair changed across synchronous refused input");
            var owner = Text(before["ownerId"]); var ownerObject = Int(before["ownerObject"]);
            Check(!string.IsNullOrWhiteSpace(owner) && new[] { owner, Text(id["riderId"]), Text(id["mountId"]), Text(id["unrelatedId"]) }.Distinct().Count() == 4, "distinct owner missing");
            Check(ownerObject != 0 && new[] { ownerObject, Int(id["riderObject"]), Int(id["mountObject"]), Int(id["unrelatedObject"]) }.Distinct().Count() == 4, "distinct owner object missing");
            Check(Text(before["targetId"]) == Text(id["unrelatedId"]) && Int(before["targetObject"]) == Int(id["unrelatedObject"]), "foreign target identity differs");
            Check(Text(before["masterId"]) == owner && Int(before["masterObject"]) == ownerObject &&
                Text(before["ownerPetId"]) == Text(before["targetId"]) && Int(before["ownerPetObject"]) == Int(before["targetObject"]), "native reciprocal ownership differs");
            foreach (var field in new[] { "ownerLiveParty", "targetLiveParty", "ownerCommandsEmpty", "targetCommandsEmpty" }) Check(Yes(before[field]), "native fixture " + field + " unavailable");
            Check(Text(before["targetBlueprint"]) == "e7aa96d15a45238438ae4cfb476f6bb9" && Text(before["targetProfile"]) == "Mammoth", "native foreign profile differs");
            foreach (var role in new[] { "owner", "target" })
            {
                var resources = before[role + "Resources"];
                Check(resources is JObject && Text(resources["actor"]) == Text(before[role + "Id"]), "foreign resource actor differs");
                foreach (var field in new[] { "standard", "move", "swift", "reactions", "reactionCooldown", "initiativeCooldown", "initiativeOrder", "reactionsPerRound", "nativePrepareCount" })
                {
                    var v = resources[field]; Check(v != null && (v.Type == JTokenType.Float || v.Type == JTokenType.Integer), "foreign resource absent");
                    Check(!double.IsNaN((double)v) && !double.IsInfinity((double)v), "foreign resource nonfinite");
                }
            }
        }
    }
}