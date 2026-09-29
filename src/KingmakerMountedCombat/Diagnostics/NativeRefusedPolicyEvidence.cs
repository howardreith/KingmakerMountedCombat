using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeRefusedPolicyEvidence
    {
        private static void Check(bool value,string reason) { if(!value)throw new InvalidOperationException("Refused policy: "+reason); }
        private static bool Bool(JToken value,bool expected) => value?.Type==JTokenType.Boolean && (bool)value==expected;
        internal static void AssertComplete(JObject e)
        {
            var p=e["policy"]; var input=e["input"]; var id=e["identity"];
            Check((string)p?["contract"]=="synchronous-paired-policy-disabled-refusal-restored","contract missing");
            var resources=p["resources"] as JObject;
            Check(Bool(resources?["pass"],true),"resource proof failed");
            NativePassiveResourceEvidence.AssertComplete(resources);
            Check((string)resources["riderId"]==(string)id["riderId"]&&(string)resources["mountId"]==(string)id["mountId"]&&Bool(resources["turnBased"],false),"resource pair differs");
            foreach(var name in new[]{"before","disabled","restored"})
            {
                var b=p[name]; var s=b?["settings"]; var paired=name!="disabled";
                Check(Bool(s?["movement"],true)&&Bool(s["paired"],paired)&&Bool(s["unified"],false)&&Bool(s["scheduler"],false)&&Bool(s["overlay"],false),"only paired policy may differ");
                foreach(var field in new[]{"frame","gameTicks"})
                    Check(b[field]?.Type==JTokenType.Integer&&JToken.DeepEquals(b[field],input["before"][field]),"synchronous policy clock differs");
                Check(b["state"]?["selectedIds"] is JArray selected&&selected.Count==1&&(string)selected[0]==(string)id["riderId"],"policy selection differs");
                NativeRefusedMountEvidence.AssertState(b["state"],input["before"]["state"]);
                Check(JToken.DeepEquals(b["state"],input["before"]["state"]), "policy state changed outside its exact input");
            }
            Check(JToken.DeepEquals(p["before"]["state"],e["legal"]["state"])&&
                JToken.DeepEquals(p["disabled"]["state"],input["before"]["state"]), "policy did not enclose the exact refusal");
            Check(p["disabled"]["allocationSequence"]?.Type==JTokenType.Integer && JToken.DeepEquals(p["disabled"]["allocationSequence"],input["before"]["allocationSequence"]), "disabled allocation boundary differs");
            Check((long)p["before"]["allocationSequence"] <= (long)p["disabled"]["allocationSequence"] && (long)input["after"]["allocationSequence"] <= (long)p["restored"]["allocationSequence"], "policy allocation order differs");
            foreach(var end in new[]{"before","after"})
            {
                var b=p[end=="before"?"before":"restored"];var r=resources[end];
                foreach(var field in new[]{"frame","gameTicks","allocationSequence"})
                    Check(b[field]?.Type==JTokenType.Integer&&JToken.DeepEquals(b[field],r[field]),"resource window boundary differs");
                foreach(var actor in new[]{"rider","mount"})
                {
                    Check((string)r[actor]["actor"]==(string)id[actor+"Id"],"resource actor differs");
                    foreach(var field in new[]{"standard","move","swift","reactions","reactionsPerRound","reactionCooldown","initiativeCooldown","initiativeOrder"})
                        Check(JToken.DeepEquals(r[actor][field],b["state"][actor][field]),"resource boundary not bound");
                    Check(JToken.DeepEquals(r[actor]["grantSequence"],b["state"][actor]["nativePrepareCount"]),"native preparation differs");
                }
            }
        }
    }
}
