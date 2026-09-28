using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeRefusedMountCaseEvidence
    {
        private static void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Refused Mount case: " + why); }
        private static string Text(JToken x) => x?.Type == JTokenType.String ? (string)x : null;
        private static bool Yes(JToken x) => x?.Type == JTokenType.Boolean && (bool)x;
        private static bool No(JToken x) => x?.Type == JTokenType.Boolean && !(bool)x;
        private static long Int(JToken x) { Check(x?.Type == JTokenType.Integer, "integer missing"); return (long)x; }
        internal static void AssertComplete(JObject e)
        {
            Check(Text(e?["contract"]) == "one-native-refused-mount-in-fresh-rt-allocation", "contract differs");
            var c = Text(e["case"]); var identity = e["identity"]; var legal = e["legal"]; var negative = e["condition"];
            var rider = Text(identity?["riderId"]); var mount = Text(identity?["mountId"]); var other = Text(identity?["unrelatedId"]);
            Check(!string.IsNullOrEmpty(rider) && !string.IsNullOrEmpty(mount) && !string.IsNullOrEmpty(other) && new[] { rider, mount, other }.Distinct().Count() == 3, "distinct actors missing");
            var riderObject = Int(identity["riderObject"]); var mountObject = Int(identity["mountObject"]); var otherObject = Int(identity["unrelatedObject"]);
            Check(new[] { riderObject, mountObject, otherObject }.All(x => x != 0) && new[] { riderObject, mountObject, otherObject }.Distinct().Count() == 3, "native object identities missing");
            Check(Yes(identity["reciprocalPair"]) && Text(identity["mountProfile"]) == "Horse" && Yes(identity["unrelatedLivePartyActor"]) && No(identity["unrelatedIsPet"]) && No(identity["unrelatedSupportedMount"]), "native fixture identity differs");
            string[] selected; long[] selectedObjects; string target, reason, row;
            switch (c)
            {
                case "wrong-creature-target": selected = new[] { rider }; selectedObjects = new[] { riderObject }; target = other; row = "CM02-wrong-creature-target"; reason = "Mount target rejected: click the selected rider's exact active Horse."; break;
                case "mount-selected": selected = new[] { mount }; selectedObjects = new[] { mountObject }; target = mount; row = "CM06-mount-selected"; reason = "Select the exact prospective rider."; break;
                case "multiple-selection": selected = new[] { rider, mount }; selectedObjects = new[] { riderObject, mountObject }; target = mount; row = "CM06-multiple-selection"; reason = "Select the exact prospective rider."; break;
                case "foreign-selection": selected = new[] { other }; selectedObjects = new[] { otherObject }; target = mount; row = "CM06-foreign-selection"; reason = "Select the exact prospective rider."; break;
                default: throw new InvalidOperationException("Unknown refused Mount case.");
            }
            Check(Text(e["row"]) == row && Text(e["scenario"]) == "chunk6a-refused-" + c, "case mapping differs");
            Check(Yes(legal?["inCombat"]) && No(legal["turnBased"]) && Yes(legal["visible"]) && Yes(legal["enabled"]) && Yes(legal["canTargetOwnedMount"]) && Yes(legal["exactRiderSelected"]), "legal native baseline missing");
            Check(legal["state"]?["selectedIds"] is JArray original && original.Select(Text).SequenceEqual(new[] { rider }), "legal selection differs");
            var input = e["input"] as JObject;
            Check(input != null && Yes(negative?["selectionVerified"]) && Yes(negative["inCombat"]) && No(negative["turnBased"]) && Yes(negative["visible"]) &&
                No(negative["canTargetRequested"]), "negative fixture unavailable");
            var wrong = c == "wrong-creature-target";
            Check(wrong ? Yes(negative["enabled"]) && Yes(negative["canTargetOwnedMount"]) : No(negative["enabled"]) && No(negative["canTargetOwnedMount"]), "refusal cause differs");
            Check(negative["selectedIds"] is JArray ids && ids.Select(Text).SequenceEqual(selected) && negative["selectedObjects"] is JArray objects && objects.Select(Int).SequenceEqual(selectedObjects), "negative selection objects differ");
            Check(Text(negative["targetId"]) == target && Int(negative["targetObject"]) == (wrong ? otherObject : mountObject), "negative exact target differs");
            Check(Text(negative["expectedReason"]) == reason, "expected refusal cause differs");
            foreach (var name in new[] { "frame", "gameTicks" })
                Check(Int(legal[name]) == Int(negative[name]) && Int(legal[name]) == Int(input["before"]?[name]), "legal/negative/input clocks differ");
            NativeRefusedMountEvidence.AssertState(legal["state"], input["before"]["state"]);
            Check(JToken.DeepEquals(negative["state"], input["before"]["state"]), "negative baseline not exact click baseline");
            NativeRefusedMountEvidence.AssertInput(input, rider, mount, target, selected);
            Check(Text(input["activations"][1]["reason"]) == reason, "actual native refusal reason differs");
        }
    }
}
