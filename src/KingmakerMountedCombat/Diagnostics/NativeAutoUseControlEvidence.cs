using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeAutoUseControlEvidence
    {
        private const string Mount="f053faad986631688defa003cd7bda0e",Dismount="3af2b81f4d72bbb30501fa730fcdf36e",Mvid="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7";
        private static void Need(bool b,string why){if(!b)throw new InvalidOperationException("Native auto-use: "+why);}
        private static long I(JToken v){Need(v?.Type==JTokenType.Integer,"integer absent");return (long)v;}
        private static bool B(JToken v){Need(v?.Type==JTokenType.Boolean,"boolean absent");return (bool)v;}
        private static string S(JToken v)=>v?.Type==JTokenType.String?(string)v:null;
        private static JToken[] A(JToken v){Need(v is JArray,"array absent");return v.ToArray();}
        private static void Same(JToken a,JToken b,string why){Need(a!=null&&b!=null&&JToken.DeepEquals(a,b),why);}
        private static double N(JToken v){Need(v!=null&&(v.Type==JTokenType.Integer||v.Type==JTokenType.Float),"number absent");var n=(double)v;Need(!double.IsNaN(n)&&!double.IsInfinity(n),"nonfinite number");return n;}
        private static JObject StableState(JToken state){var s=(JObject)state.DeepClone();var g=s["geometry"] as JObject;Need(g!=null,"geometry absent");g.Remove("seconds");return s;}
        // Native JObject string assignments retain String/null until serialization.
        private static bool Empty(JToken v)=>v==null||v.Type==JTokenType.Null||
            v.Type==JTokenType.String&&((JValue)v).Value==null;
        private static void Unadmitted(JToken c,string target){Need(I(c["object"])!=0&&Empty(c["executorId"])&&S(c["targetId"])==target&&!B(c["started"])&&!B(c["acted"])&&!B(c["finished"]),"AI/control command was admitted or changed target");}
        internal static void AssertComplete(JObject e)
        {
            Need(S(e["contract"])=="native-ui-auto-use-and-ai-revalidation-of-explicit-stale-reference"&&Empty(e["failure"])&&A(e["errors"]).Length==0,"contract/failure differs");
            var rider=S(e["riderId"]);var mount=S(e["mountId"]);var target=S(e["targetId"]);var guid=S(e["abilityGuid"]);
            Need(!string.IsNullOrWhiteSpace(rider)&&!string.IsNullOrWhiteSpace(mount)&&!string.IsNullOrWhiteSpace(target)&&new[]{rider,mount,target}.Distinct().Count()==3&&(guid==Mount||guid==Dismount),"pair/control differs");
            foreach(var k in new[]{"riderObject","mountObject","targetObject","mechanicObject","aiObject","abilityObject","ordinaryAbilityObject","contextObject"})Need(I(e[k])!=0,"object missing: "+k);
            Need(I(e["riderObject"])!=I(e["mountObject"])&&I(e["ordinaryAbilityObject"])!=I(e["abilityObject"]),"actor/ability aliases");
            var ordinary=S(e["ordinaryAbilityGuid"]);Need(!string.IsNullOrWhiteSpace(ordinary)&&ordinary!=Mount&&ordinary!=Dismount&&B(e["ordinarySuitable"])&&!B(e["suitable"])&&B(e["staleReferenceFixtureDeclared"])&&B(e["constructorObserverExecuted"]),"ordinary control or refusal absent");
            var reg=e["slotRegistration"];Need(S(reg["casterId"])==rider&&I(reg["occurrences"])==1&&B(reg["active"])&&(S(reg["route"])=="main"||S(reg["route"])=="group"),"live slot registration differs");
            foreach(var k in new[]{"managerObject","ownerObject","slotObject"})Need(I(reg[k])!=0,"registered object absent");
            var before=e["before"];var frame=I(before["frame"]);var ticks=I(before["gameTicks"]);var seq=I(before["allocationSequence"]);var original=I(before["autoUseObject"]);
            Need(original!=I(e["abilityObject"])&&S(before["autoUseGuid"])!=Mount&&S(before["autoUseGuid"])!=Dismount,"preexisting relationship auto-use");
            foreach(var name in new[]{"before","afterUi","afterLease","afterAi","afterRestoration"}){
                var s=e[name];Need(I(s["frame"])==frame&&I(s["gameTicks"])==ticks&&I(s["allocationSequence"])==seq&&B(s["paused"])&&!B(s["turnBased"])&&I(s["selectedAbilityObject"])==0&&B(s["riderCommandsEmpty"])&&B(s["mountCommandsEmpty"]),"synchronous paused command-free window differs: "+name);
                foreach(var k in new[]{"shellCount","processBindings","controls"})Same(s[k],before[k],"transition/selection/resource state changed: "+name+" "+k);
                Same(StableState(s["state"]),StableState(before["state"]),"causal state changed: "+name);
                Need(I(s["state"]["geometry"]["frame"])==frame&&N(s["state"]["geometry"]["seconds"])>=N(before["state"]["geometry"]["seconds"]),"geometry observation clock differs");
                var leased=name=="afterLease"||name=="afterAi";
                Need(I(s["autoUseObject"])==(leased?I(e["abilityObject"]):original),"lease/reference restoration differs");
                if(leased)Need(S(s["autoUseGuid"])==guid,"leased blueprint differs");else Same(s["autoUseGuid"],before["autoUseGuid"],"restored blueprint differs");
            }
            Need(A(before["state"]["selectedIds"]).Length==1&&S(before["state"]["selectedIds"][0])==rider&&S(before["state"]["relationshipState"])==(guid==Mount?"Unmounted":"Mounted"),"selected rider/relationship differs");
            var resources=(JObject)e["resources"];NativePassiveResourceEvidence.AssertComplete(resources);Need(!B(resources["turnBased"])&&S(resources["riderId"])==rider&&S(resources["mountId"])==mount&&A(resources["events"]).Length==0,"passive window admitted native work");
            foreach(var end in new[]{"before","after"}){Need(I(resources[end]["frame"])==frame&&I(resources[end]["gameTicks"])==ticks&&I(resources[end]["allocationSequence"])==seq,"passive clock differs");foreach(var role in new[]{"rider","mount"}){
                var r=resources[end][role];Need(B(r["inCombat"])&&I(r["actorObject"])==I(e[role+"Object"]),"passive actor differs");
                foreach(var k in new[]{"actor","standard","move","swift","reactions","reactionCooldown","initiativeCooldown","initiativeOrder","reactionsPerRound"})Same(r[k],before["state"][role][k],"passive resource copy differs: "+role+" "+k);
                Need(I(r["grantSequence"])==I(before["state"][role]["nativePrepareCount"]),"passive preparation differs");
            }}
            var specs=new[]{new[]{"06002B30","Kingmaker.UnitLogic.Abilities.AbilityData.get_IsSuitableForAutoUse","SuitableBefore","SuitableAfter"},new[]{"06002F5F","Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility.OnAutoUseToggle","ToggleBefore","ToggleAfter"},new[]{"06008370","Kingmaker.EntitySystem.Entities.UnitEntityData.GetAvailableAutoUseAbility","AvailableBefore","AvailableAfter"},new[]{"06009434","Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAttack.CreateCommand","AiBefore","AiAfter"},new[]{"06002726","Kingmaker.UnitLogic.Commands.UnitUseAbility..ctor",null,"Constructed"},new[]{"06002727","Kingmaker.UnitLogic.Commands.UnitUseAbility..ctor",null,"Constructed"}};
            var hooks=A(e["observerHooks"]);Need(hooks.Length==specs.Length,"hook count differs");foreach(var spec in specs){var h=hooks.Where(x=>S(x["token"])==spec[0]).ToArray();Need(h.Length==1&&S(h[0]["method"])==spec[1]&&S(h[0]["prefix"])==spec[2]&&S(h[0]["postfix"])==spec[3]&&S(h[0]["moduleMvid"])==Mvid,"exact hook absent");}
            var events=A(e["events"]);Need(events.Length>0&&events.Length<=128,"events absent/bounded");var ordinals=new List<long>();var phases=new[]{"constructor-control","baseline-ai","ui-toggle","stale-reference"};var lastPhase=0;long lastEnter=0;
            foreach(var c in events){var phase=Array.IndexOf(phases,S(c["phase"]));var enter=I(c["enterOrdinal"]);var exit=I(c["exitOrdinal"]);Need(phase>=lastPhase&&enter>lastEnter&&exit>enter&&B(c["complete"])&&I(c["frame"])==frame&&I(c["exitFrame"])==frame&&I(c["gameTicks"])==ticks&&I(c["exitGameTicks"])==ticks,"callback order/clock/completion differs");lastPhase=phase;lastEnter=enter;ordinals.Add(enter);ordinals.Add(exit);
                var kind=S(c["kind"]);Need(new[]{"suitability","toggle","available","ai-create"}.Contains(kind),"unknown callback");
                if(kind=="ai-create"){Need(I(c["receiverObject"])==I(e["aiObject"])&&I(c["contextObject"])==I(e["contextObject"])&&I(c["targetObject"])==I(e["targetObject"])&&S(c["targetId"])==target&&S(c["casterId"])==rider&&I(c["abilityObject"])==0,"AI callback receiver/context/target differs");}
                else {Need(S(c["casterId"])==rider||kind=="available"&&I(c["abilityObject"])==0,"callback caster differs");Need(I(c["receiverObject"])==(kind=="suitability"?I(c["abilityObject"]):kind=="toggle"?I(e["mechanicObject"]):I(e["riderObject"])),"native receiver differs");}
                foreach(var prior in events.Where(p=>I(p["enterOrdinal"])<enter&&I(p["exitOrdinal"])>enter))Need(exit<I(prior["exitOrdinal"])&&S(prior["phase"])==S(c["phase"]),"crossed callback nesting");
            }
            Func<string,string,JToken[]> calls=(phase,kind)=>events.Where(c=>S(c["phase"])==phase&&S(c["kind"])==kind).ToArray();
            Action<JToken,JToken> nested=(child,parent)=>Need(I(child["enterOrdinal"])>I(parent["enterOrdinal"])&&I(child["exitOrdinal"])<I(parent["exitOrdinal"]),"missing native nested revalidation");
            var control=calls("constructor-control","suitability");Need(control.Length==1&&events.Count(c=>S(c["phase"])=="constructor-control")==1&&I(control[0]["abilityObject"])==I(e["ordinaryAbilityObject"])&&S(control[0]["abilityGuid"])==ordinary&&B(control[0]["result"]),"ordinary native suitability control absent");
            foreach(var phase in new[]{"baseline-ai","stale-reference"}){
                var ai=calls(phase,"ai-create");Need(ai.Length==1,"native AI call missing");Same(ai[0]["result"],e[phase=="baseline-ai"?"baselineAi":"aiWithStaleReference"],"AI result differs");
                var available=calls(phase,"available");Need(available.Length==(phase=="baseline-ai"?1:2),"native availability call count differs");Need(available.Count(c=>I(c["enterOrdinal"])>I(ai[0]["enterOrdinal"])&&I(c["exitOrdinal"])<I(ai[0]["exitOrdinal"]))==1,"AI did not revalidate native availability");
                if(phase=="stale-reference")foreach(var call in available){Need(I(call["abilityObject"])==I(e["abilityObject"])&&S(call["abilityGuid"])==guid&&I(call["result"]["object"])==0&&Empty(call["result"]["guid"]),"stale reference remained available");var suitable=calls(phase,"suitability").Where(c=>I(c["enterOrdinal"])>I(call["enterOrdinal"])&&I(c["exitOrdinal"])<I(call["exitOrdinal"])).ToArray();Need(suitable.Length==1,"native availability did not query eligibility");}
            }
            var baselineAvailable=calls("baseline-ai","available")[0];var baselineSuitable=calls("baseline-ai","suitability");
            Need(I(baselineAvailable["abilityObject"])==original&&S(baselineAvailable["abilityGuid"])==S(before["autoUseGuid"]),"baseline availability input differs");
            Need(baselineSuitable.Length==(original==0?0:1)&&events.Count(c=>S(c["phase"])=="baseline-ai")==2+baselineSuitable.Length,"baseline callback chain differs");
            var availableOriginal=I(baselineAvailable["result"]["object"]);Need(availableOriginal==0||availableOriginal==original,"baseline availability result differs");
            Need(S(baselineAvailable["result"]["guid"])==(availableOriginal==0?null:S(before["autoUseGuid"])),"baseline available blueprint differs");
            if(original!=0){nested(baselineSuitable[0],baselineAvailable);Need(I(baselineSuitable[0]["abilityObject"])==original&&S(baselineSuitable[0]["abilityGuid"])==S(before["autoUseGuid"]),"baseline eligibility identity differs");if(availableOriginal!=0)Need(B(baselineSuitable[0]["result"]),"baseline ignored eligibility");else B(baselineSuitable[0]["result"]);}
            var toggle=calls("ui-toggle","toggle");var ui=calls("ui-toggle","suitability");Need(toggle.Length==1&&ui.Length==2&&events.Count(c=>S(c["phase"])=="ui-toggle")==3&&Empty(toggle[0]["result"]),"native UI toggle callbacks differ");nested(ui[1],toggle[0]);Need(I(ui[0]["exitOrdinal"])<I(toggle[0]["enterOrdinal"]),"explicit suitability query was not before UI input");
            Need(calls("stale-reference","suitability").Length==2&&events.Count(c=>S(c["phase"])=="stale-reference")==5,"stale native callback chain differs");
            foreach(var c in events.Where(c=>S(c["phase"])=="ui-toggle"||S(c["phase"])=="stale-reference")){if(S(c["kind"])=="suitability"||S(c["kind"])=="toggle")Need(I(c["abilityObject"])==I(e["abilityObject"])&&S(c["abilityGuid"])==guid&&(S(c["kind"])!="suitability"||!B(c["result"])),"relationship eligibility not refused");}
            Need(I(e["availableObject"])==0&&Empty(e["availableGuid"]),"explicit stale availability differs");
            var ctor=e["constructorControl"];Unadmitted(ctor,target);Need(S(ctor["type"])=="Kingmaker.UnitLogic.Commands.UnitUseAbility"&&S(ctor["abilityGuid"])==ordinary,"constructor control differs");
            var baseline=e["baselineAi"];var stale=e["aiWithStaleReference"];Unadmitted(baseline,target);Unadmitted(stale,target);Need(I(baseline["object"])!=I(stale["object"])&&I(ctor["object"])!=I(stale["object"])&&I(ctor["object"])!=I(baseline["object"]),"command identities alias");Need(S(stale["type"])=="Kingmaker.UnitLogic.Commands.UnitAttack"&&Empty(stale["abilityGuid"]),"AI did not build its ordinary attack fallback");
            var baselineCast=S(baseline["type"])=="Kingmaker.UnitLogic.Commands.UnitUseAbility";Need(baselineCast||S(baseline["type"])=="Kingmaker.UnitLogic.Commands.UnitAttack","baseline AI command unexpected");if(baselineCast)Need(original!=0&&S(baseline["abilityGuid"])==S(before["autoUseGuid"])&&S(baseline["abilityGuid"])!=Mount&&S(baseline["abilityGuid"])!=Dismount,"baseline AI cast differs");else Need(Empty(baseline["abilityGuid"]),"ordinary baseline acquired ability");
            var constructed=A(e["constructedAbilityCommands"]);Need(constructed.Length==(baselineCast?2:1),"unexpected ability command constructed");Same(constructed[0]["command"],ctor,"control construction differs");Need(S(constructed[0]["phase"])=="constructor-control","control phase differs");if(baselineCast){Same(constructed[1]["command"],baseline,"baseline construction differs");Need(S(constructed[1]["phase"])=="baseline-ai","baseline construction phase differs");}
            var callbacks=A(e["constructorCallbacks"]);Need(callbacks.Length==constructed.Length*2,"constructor execution control incomplete");foreach(var c in constructed){var pair=callbacks.Where(x=>I(x["command"]["object"])==I(c["command"]["object"])).ToArray();Need(pair.Length==2&&S(pair[0]["token"])=="06002727"&&S(pair[1]["token"])=="06002726"&&I(pair[0]["ordinal"])<I(pair[1]["ordinal"]),"exact constructor chain missing");foreach(var cb in pair){Same(cb["command"],c["command"],"constructor identity differs");Need(S(cb["phase"])==S(c["phase"])&&I(cb["frame"])==frame&&I(cb["gameTicks"])==ticks,"constructor phase/clock differs");ordinals.Add(I(cb["ordinal"]));}}
            var sorted=ordinals.OrderBy(x=>x).ToArray();for(int n=0;n<sorted.Length;n++)Need(sorted[n]==n+1L,"observer sequence gap/duplicate");
        }
    }
}
