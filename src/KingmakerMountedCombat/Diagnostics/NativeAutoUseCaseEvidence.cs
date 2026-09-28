using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeAutoUseCaseEvidence
    {
        private static void Need(bool b,string why){if(!b)throw new InvalidOperationException("Auto-use case: "+why);}
        private static long I(JToken t){Need(t?.Type==JTokenType.Integer,"integer absent");return (long)t;}
        private static bool B(JToken t){Need(t?.Type==JTokenType.Boolean,"boolean absent");return (bool)t;}
        private static string S(JToken t)=>t?.Type==JTokenType.String?(string)t:null;
        private static void Same(JToken a,JToken b,string why){Need(a!=null&&b!=null&&JToken.DeepEquals(a,b),why);}
        private static void Clock(JToken a,JToken b){foreach(var k in new[]{"frame","gameTicks","allocationSequence"})Need(I(a[k])==I(b[k]),"window clock/sequence binding differs");}
        private static JObject StableState(JToken state){var s=(JObject)state.DeepClone();var g=s["geometry"] as JObject;Need(g!=null,"geometry absent");g.Remove("seconds");return s;}
        private static void Resources(JToken native,JToken boundary){foreach(var role in new[]{"rider","mount"})Same(native[role],boundary[role+"Resources"],"resource endpoint differs");}
        private static void Slice(JObject window,JToken trace){NativePassiveResourceEvidence.AssertComplete(window);Same(window["observerHooks"],trace["observerHooks"],"window hooks differ from full trace");Same(window["events"],new JArray(trace["events"].Where(x=>I(x["sequence"])>I(window["before"]["allocationSequence"])&&I(x["sequence"])<=I(window["after"]["allocationSequence"]))),"window differs from complete native trace");}
        internal static void AssertComplete(JObject e)
        {
            var kind=S(e?["case"]);Need(S(e?["contract"])=="independent-fresh-rt-native-auto-use-control-window"&&(kind=="mount"||kind=="dismount")&&S(e["scenario"])=="chunk6a-auto-use-"+kind+"-rt","case identity differs");
            var input=(JObject)e["input"];NativeAutoUseControlEvidence.AssertComplete(input);Need(B(input["pass"]),"input producer did not PASS");
            var first=e["beforeSetup"];var before=e["beforeInput"];var after=e["afterInput"];var restored=e["afterRestoration"];
            var rider=S(input["riderId"]);var mount=S(input["mountId"]);var relation=kind=="mount"?"Unmounted":"Mounted";
            Need(S(input["abilityGuid"])==(kind=="mount"?"f053faad986631688defa003cd7bda0e":"3af2b81f4d72bbb30501fa730fcdf36e"),"case/control mismatch");
            foreach(var b in new[]{first,before,after,restored}){
                Need(!B(b["turnBased"])&&B(b["pairIdle"])&&B(b["targetInCombat"])&&B(b["hostileTarget"])&&S(b["state"]["relationshipState"])==relation,"case allocation/target not legal");
                foreach(var k in new[]{"riderObject","mountObject","targetObject"})Need(I(b[k])==I(input[k]),"case/input object differs");Need(S(b["targetId"])==S(input["targetId"]),"hostile target changed");
                foreach(var k in new[]{"ledger","generation","selectedIds"})Same(b["state"][k],input["before"]["state"][k],"case relationship/selection changed");
                foreach(var role in new[]{"rider","mount"}){Need(B(b[role+"Resources"]["inCombat"])&&S(b[role+"Resources"]["actor"])==(role=="rider"?rider:mount),"actor allocation differs");
                    foreach(var k in new[]{"actor","standard","move","swift","reactions","reactionCooldown","initiativeCooldown","initiativeOrder","reactionsPerRound"})Same(b[role+"Resources"][k],b["state"][role][k],"case resource copies differ");
                    Need(I(b[role+"Resources"]["grantSequence"])==I(b["state"][role]["nativePrepareCount"]),"case preparation copies differ");
                }
            }
            Need(B(first["paused"])==B(e["originalPause"])&&B(before["paused"])&&B(after["paused"])&&B(restored["paused"])==B(e["originalPause"])&&B(e["pauseRestored"]),"pause restoration differs");
            Clock(before,input["before"]);Clock(after,input["afterRestoration"]);Clock(after,restored);
            foreach(var pair in new[]{new[]{before,input["before"]},new[]{after,input["afterRestoration"]}}){
                Same(StableState(pair[0]["state"]),StableState(pair[1]["state"]),"case/input state differs");
            }
            // The state adapters carry monotonic wall-clock diagnostic seconds; all native
            // resource/geometry fields are bound separately by the input and these windows.
            var trace=e["allocationTrace"];Need(I(trace["dropped"])==0&&I(trace["observationErrors"])==0&&trace["events"] is JArray,"complete allocation trace absent");
            long sequence=0;foreach(var item in trace["events"])Need(I(item["sequence"])==++sequence,"complete allocation trace gap");Need(sequence==I(restored["allocationSequence"]),"trace endpoint differs");
            var setup=(JObject)e["setupResources"];Need(S(setup["riderId"])==rider&&S(setup["mountId"])==mount&&!B(setup["turnBased"]),"setup pair/mode differs");Slice(setup,trace);Clock(setup["before"],first);Clock(setup["after"],before);Resources(setup["before"],first);Resources(setup["after"],before);
            var resource=(JObject)input["resources"];Slice(resource,trace);Clock(resource["before"],before);Clock(resource["after"],after);Resources(resource["before"],before);Resources(resource["after"],after);
            foreach(var role in new[]{"rider","mount"}){Same(restored[role+"Resources"],after[role+"Resources"],"UI/pause restoration changed resources");Same(restored["state"][role],after["state"][role],"restoration changed causal resources");}
            var ui=e["ui"];Need(S(ui["contract"])=="native-action-bar-group-input-and-exact-restoration"&&S(ui["casterId"])==rider&&B(ui["restored"])&&I(ui["managerObject"])==I(input["slotRegistration"]["managerObject"]),"UI registration/restoration differs");
            Need(ui["before"] is JArray&&ui["after"] is JArray&&ui["opened"] is JArray&&ui["matchingSlotObjects"] is JArray,"UI observations absent");Same(ui["before"],ui["after"],"UI groups not restored");
            Need(ui["matchingSlotObjects"].Count()==1&&I(ui["matchingSlotObjects"][0])==I(input["slotRegistration"]["slotObject"]),"slot lookup differs");
            Need(I(input["slotRegistration"]["ownerObject"])==I(ui[S(input["slotRegistration"]["route"])=="group"?"abilityGroupObject":"mainOwnerObject"]),"live slot owner differs");
            Same(ui["current"],ui["opened"],"opened group changed before input");
            Need(ui["before"].Count()==ui["opened"].Count(),"native groups changed");
            for(int g=0;g<ui["before"].Count();g++)foreach(var k in new[]{"index","groupObject","type"})Same(ui["before"][g][k],ui["opened"][g][k],"native group registration changed");
            var groups=ui["opened"].Where(g=>I(g["groupObject"])==I(ui["abilityGroupObject"])).ToArray();Need(groups.Length==1&&B(groups[0]["toggle"])&&S(groups[0]["type"])=="ActivatableAbility","native ability group not open");
            Need(e["ordinaryCandidates"] is JArray&&e["ordinaryCandidates"].Count()>0&&e["ordinaryCandidates"].Count()<=256,"ordinary fixture inventory absent/bounded");
            var ordinary=e["ordinaryCandidates"].Where(a=>I(a["object"])==I(input["ordinaryAbilityObject"])).ToArray();Need(ordinary.Length==1&&S(ordinary[0]["guid"])==S(input["ordinaryAbilityGuid"])&&B(ordinary[0]["suitable"]),"ordinary constructor control not owned by fixture inventory");
            if(kind=="mount"){Need(e["mountProof"]==null||e["mountProof"].Type==JTokenType.Null,"Mount auto-use followed another combat Mount");}
            else{
                var proof=(JObject)e["mountProof"];Need(B(proof["pass"])&&B(proof["resourceWindow"]["pass"])&&B(proof["resourceWindow"]["reactionResources"]["pass"]),"Dismount setup lacks its own positive Mount");
                var bridge=(JObject)e["mountTerminalBridge"];NativeTerminalBridgeEvidence.AssertComplete(proof,(JObject)first,bridge,false,"Mounted");Slice(bridge,trace);
            }
        }
    }
}
