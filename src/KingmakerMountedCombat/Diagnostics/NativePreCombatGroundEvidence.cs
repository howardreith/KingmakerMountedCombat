using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativePreCombatGroundEvidence
    {
        private static void Require(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Native ground plan: " + reason); }
        private static string Text(JToken v) => v?.Type == JTokenType.String ? (string)v : null;
        private static double Number(JToken v) {
            Require(v != null && (v.Type == JTokenType.Float || v.Type == JTokenType.Integer), "number missing");
            var n = (double)v; Require(!double.IsNaN(n) && !double.IsInfinity(n), "number nonfinite"); return n;
        }
        private static int Int(JToken v) { Require(v?.Type == JTokenType.Integer, "integer missing"); return (int)v; }
        private static bool Bool(JToken v) { Require(v?.Type == JTokenType.Boolean, "boolean missing"); return (bool)v; }
        private static double[] Point(JToken p) => p is JArray ? new[] { Number(p[0]), Number(p[1]), Number(p[2]) } : new[] { Number(p?["x"]), Number(p?["y"]), Number(p?["z"]) };
        private static double Distance(double[] a, double[] b) => Math.Sqrt((a[0]-b[0])*(a[0]-b[0])+(a[2]-b[2])*(a[2]-b[2]));
        private static void Near(double actual, double expected, string reason) => Require(Math.Abs(actual-expected) <= 0.00001, reason);
        private static void SamePoint(JToken a, JToken b) { var x=Point(a);var y=Point(b);for(var i=0;i<3;i++)Near(x[i],y[i],"position differs"); }
        internal static int AssertSearch(JObject p)
        {
            Require(Text(p?["contract"]) == "bounded-native-ground-plan" && Int(p["maximumCandidates"]) == 72, "contract/bound differs");
            var actor=Text(p["actorId"]);var partner=Text(p["partnerId"]);var joint=Bool(p["joint"]);
            Require(!string.IsNullOrEmpty(actor)&&!string.IsNullOrEmpty(partner)&&actor!=partner,"pair identity missing");
            var origin=Point(p["origin"]);var center=Point(p["center"]);var corp=Number(p["corpulence"]);var clearance=Number(p["clearanceRadius"]);
            Require(corp>=0,"negative corpulence");Near(clearance,Math.Max(0.5,corp)+(joint?0:0.75),"clearance changed");
            var length=Distance(origin,center);Require(length>=0.1,"degenerate geometry");
            var dx=(origin[0]-center[0])/length;var dz=(origin[2]-center[2])/length;
            var occupants=p["occupants"] as JArray;Require(occupants!=null,"occupants missing");
            var ids=occupants.Select(x=>Text(x["actorId"])).ToArray();
            Require(ids.All(x=>!string.IsNullOrEmpty(x))&&ids.Distinct().Count()==ids.Length&&ids.Contains(actor)&&ids.Contains(partner),"occupants identity differs");
            foreach(var u in occupants){Point(u["position"]);Require(Number(u["corpulence"])>=0,"occupant radius invalid");}
            var own=occupants.Single(x=>Text(x["actorId"])==actor);var other=occupants.Single(x=>Text(x["actorId"])==partner);
            SamePoint(own["position"],p["origin"]);SamePoint(other["position"],p["center"]);Near(Number(own["corpulence"]),corp,"actor radius differs");
            if(p["partnerPositionOverride"]?.Type!=JTokenType.Null){Require(!joint,"joint search cannot override partner");SamePoint(p["partnerPositionOverride"],p["center"]);}
            var candidates=p["candidates"] as JArray;Require(candidates!=null&&candidates.Count>=1&&candidates.Count<=72,"search size invalid");
            var selected=-1;
            for(var n=0;n<candidates.Count;n++) {
                Require(selected<0,"search continued after selection");
                var c=candidates[n];var radius=new[]{2.3,2.65,2.0}[n/24];var index=n%24;
                var angle=(index==0?0:(index%2==0?index:-index)*15)*Math.PI/180;
                var requested=Point(c["requested"]);var point=Point(c["point"]);
                Near(Number(c["requestedSeparation"]),radius,"proposal radius/order differs");
                Near(requested[0],center[0]+(Math.Cos(angle)*dx+Math.Sin(angle)*dz)*radius,"proposal x differs");
                Near(requested[1],center[1],"proposal height differs");
                Near(requested[2],center[2]+(-Math.Sin(angle)*dx+Math.Cos(angle)*dz)*radius,"proposal z differs");
                if(!Bool(c["walkable"])) {Require(!Bool(c["eligible"])&&c["riderSearch"]==null,"nonwalkable point selected");continue;}
                var travel=Distance(origin,point);var separation=Distance(center,point);var route=Distance(Point(c["routeEnd"]),point);
                Near(Number(c["travel"]),travel,"travel differs");Near(Number(c["separation"]),separation,"separation differs");Near(Number(c["routeResidual"]),route,"route residual differs");
                var f=c["footprint"];SamePoint(f?["center"],c["point"]);Near(Number(f["corpulence"]),corp,"footprint actor differs");Near(Number(f["probeRadius"]),clearance,"footprint radius differs");
                var probes=f["probes"] as JArray;Require(probes?.Count==8,"footprint probes missing");var maximum=0.0;
                for(var j=0;j<8;j++) {
                    var probe=probes[j];var to=Point(probe["requested"]);var a=j*Math.PI/4;
                    Near(to[0],point[0]+Math.Sin(a)*clearance,"footprint ray x differs");Near(to[1],point[1],"footprint height differs");Near(to[2],point[2]+Math.Cos(a)*clearance,"footprint ray z differs");
                    var residual=Distance(to,Point(probe["endpoint"]));Near(Number(probe["residual"]),residual,"footprint residual differs");maximum=Math.Max(maximum,residual);
                }
                var blockers=occupants.Where(u=>Text(u["actorId"])!=actor&&Distance(point,Point(u["position"]))<corp+Number(u["corpulence"])+0.05).Select(u=>Text(u["actorId"])).ToArray();
                Require(c["blockers"] is JArray&&blockers.SequenceEqual(c["blockers"].Select(Text)),"occupancy differs");
                var eligible=NativeGroundFixturePolicy.IsPreCombatPosition(radius,Number(c["separation"]),Number(c["travel"]),Number(c["routeResidual"]),probes.Max(x=>Number(x["residual"])),blockers.Length!=0);
                Require(Bool(c["eligible"])==eligible,"candidate eligibility differs");
                if(!eligible){Require(c["riderSearch"]==null,"rejected mount has rider search");continue;}
                if(joint) {
                    var nested=c["riderSearch"] as JObject;Require(nested!=null&&!Bool(nested["joint"])&&Text(nested["actorId"])==partner&&Text(nested["partnerId"])==actor,"joint pair differs");
                    SamePoint(nested["center"],c["point"]);SamePoint(nested["origin"],p["center"]);SamePoint(nested["partnerPositionOverride"],c["point"]);
                    Require(nested["occupants"] is JArray&&nested["occupants"].Count()==occupants.Count,"counterfactual occupancy missing");
                    for(var k=0;k<occupants.Count;k++) {
                        var actual=nested["occupants"][k];var original=occupants[k];
                        Require(Text(actual["actorId"])==Text(original["actorId"]),"counterfactual actor differs");
                        Near(Number(actual["corpulence"]),Number(original["corpulence"]),"counterfactual radius differs");
                        SamePoint(actual["position"],Text(original["actorId"])==actor?c["point"]:original["position"]);
                    }
                    if(AssertSearch(nested)<0)continue;
                }else Require(c["riderSearch"]==null,"unexpected nested search");
                selected=n;
            }
            Require(Int(p["selectedIndex"])==selected&&(selected>=0||candidates.Count==72),"selection/result cap differs");
            return selected;
        }
        internal static void AssertTransaction(JObject p, string rider, string mount)
        {
            Require(!string.IsNullOrEmpty(rider)&&!string.IsNullOrEmpty(mount)&&rider!=mount,"transaction pair missing");
            var mover=Text(p?["moverId"]);Require(mover==rider||mover==mount,"transaction mover differs");
            var plan=p["plan"] as JObject;var selected=AssertSearch(plan);Require(selected>=0&&Text(plan["actorId"])==mover&&Text(plan["partnerId"])==(mover==rider?mount:rider),"transaction plan differs");
            Require(Bool(plan["joint"])==(mover==mount),"transaction plan mode differs");
            Require(Text(p["contract"])==(mover==rider?"native-ground-positioning-before-fresh-encounter":"native-mount-ground-positioning-before-rider-staging"),"transaction contract differs");
            Require(!Bool(p["beforeInCombat"])&&!Bool(p["afterInCombat"])&&Bool(p["traceComplete"]),"transaction entered combat or lost trace");
            Near(Number(p["minimumTravel"]),0.25,"minimum travel differs");Near(Number(p["maximumTravel"]),4,"maximum travel differs");Near(Number(p["clearanceRadius"]),Number(plan["clearanceRadius"]),"transaction clearance differs");
            Require(JToken.DeepEquals(p["candidates"],plan["candidates"]),"transaction candidates differ");SamePoint(p["destination"],plan["candidates"][selected]["point"]);
            var b=p["before"];var a=p["after"];var selection=p["selection"];
            Require(Bool(selection?["exact"])&&Text(selection["actorId"])==mover&&selection["selectedIds"] is JArray&&selection["selectedIds"].Count()==1&&Text(selection["selectedIds"][0])==mover,"selection before baseline differs");
            Require(Int(selection["frame"])<=Int(p["frameBefore"])&&Number(selection["gameTicks"])<=Number(p["gameTicksBefore"]),"selection follows baseline");
            Require(Int(p["frameAfter"])>Int(p["frameBefore"])&&Number(p["gameTicksAfter"])>=Number(p["gameTicksBefore"]),"transaction clock order differs");
            foreach(var state in new[]{b,a}) {
                Require(Text(state?["rider"]?["actor"])==rider&&Text(state["mount"]?["actor"])==mount&&Text(state["relationshipState"])=="Unmounted","transaction relationship/pair differs");
                Require(JToken.DeepEquals(state["selectedIds"],selection["selectedIds"]),"transaction selection changed");
            }
            Require(b["ledger"] is JObject&&JToken.DeepEquals(b["ledger"],a["ledger"])&&b["generation"]!=null&&JToken.DeepEquals(b["generation"],a["generation"]),"transaction ledger/generation changed");
            var role=mover==rider?"riderPosition":"horsePosition";var other=mover==rider?"horsePosition":"riderPosition";
            SamePoint(b["geometry"]?[role],plan["origin"]);SamePoint(b["geometry"]?[other],plan["center"]);SamePoint(b["geometry"][other],a["geometry"]?[other]);
            var residual=Distance(Point(a["geometry"][role]),Point(p["destination"]));Near(Number(p["residual"]),residual,"arrival residual differs");Require(residual<=0.06,"arrival exceeds unchanged tolerance");
            SamePoint(p["originNavigation"]?["position"],b["geometry"][role]);SamePoint(p["terminalNavigation"]?["position"],a["geometry"][role]);
            var command=p["admittedCommand"];var terminal=p["terminalCommand"];var id=Int(command?["id"]);
            Require(id!=0&&Int(terminal?["id"])==id&&Text(command["executor"])==mover&&Text(terminal["executor"])==mover&&
                Text(command["type"])=="Kingmaker.UnitLogic.Commands.UnitMoveTo"&&Text(terminal["type"])==Text(command["type"])&&
                !Bool(command["acted"])&&!Bool(command["finished"])&&Bool(terminal["acted"])&&Bool(terminal["finished"])&&Text(terminal["result"])=="Success"&&Bool(p["createdByPlayer"]),"exact successful command differs");
            var resources=p["resourceWindow"] as JObject;NativePassiveResourceEvidence.AssertGround(resources);
            Require(Text(resources["riderId"])==rider&&Text(resources["mountId"])==mount&&Int(resources["groundCommand"]["commandObject"])==id&&Text(resources["groundCommand"]["casterId"])==mover&&
                Text(resources["groundCommand"]["type"])==Text(command["type"])&&Bool(resources["groundCommand"]["createdByPlayer"])&&Bool(resources["groundCommand"]["finished"])&&Text(resources["groundCommand"]["nativeResult"])=="Success","resource command binding differs");
            Require(JToken.DeepEquals(p["events"],resources["events"]),"resource event copies differ");
            foreach(var boundary in new[]{"before","after"}) {
                var state=p[boundary];var snapshot=resources[boundary];var suffix=boundary=="before"?"Before":"After";
                Require(Int(snapshot["frame"])==Int(p["frame"+suffix])&&Number(snapshot["gameTicks"])==Number(p["gameTicks"+suffix]),"resource clock binding differs");
                foreach(var actor in new[]{"rider","mount"}) {
                    Require(Text(state[actor]["actor"])==Text(snapshot[actor]["actor"])&&Int(state[actor]["nativePrepareCount"])==Int(snapshot[actor]["grantSequence"]),"resource actor/allocation binding differs");
                    foreach(var field in new[]{"standard","move","swift","reactions","reactionCooldown","initiativeCooldown","initiativeOrder","reactionsPerRound"})
                        Near(Number(state[actor][field]),Number(snapshot[actor][field]),"resource endpoint binding differs");
                }
            }
            AssertPath(p["path"] as JObject,id,mover,p["destination"],Number(p["gameTicksBefore"]),Number(p["gameTicksAfter"]));
        }
        private static void AssertPath(JObject p,int id,string mover,JToken destination,double before,double after)
        {
            Require(p!=null&&Bool(p["complete"])&&p["errors"] is JArray&&!p["errors"].Any()&&Int(p["commandObject"])==id&&Text(p["actorId"])==mover,"path instrumentation/identity differs");
            Require(p["observerHooks"] is JArray&&p["events"] is JArray,"path observations missing");
            foreach(var token in new[]{"060018A3","060018B9","0600184F","06001850","060027B2"}) {
                var hooks=p["observerHooks"].Where(h=>Text(h["token"])==token).ToArray();Require(hooks.Length==1&&Text(hooks[0]["moduleMvid"])=="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7","path hook differs");
            }
            var sequence=0;var time=before;var requested=0;var completed=0;var bound=0;var ended=0;var pending=false;var pathObject=0;var requestSequence=0;
            foreach(var e in p["events"]) {
                var boundary=Text(e["boundary"]);var ticks=Number(e["gameTicks"]);
                Require(Int(e["sequence"])==++sequence&&Int(e["commandObject"])==id&&Text(e["actorId"])==mover&&ticks>=time&&ticks<=after&&ended==0,"path sequence/time/identity differs");time=ticks;
                if(boundary=="command-bound"){Require(sequence==1&&++bound==1,"path command binding order differs");continue;}
                if(boundary=="path-request") {
                    Require(bound==1&&!pending&&Int(e["pathObject"])!=0&&Int(e["requestSequence"])==++requested,"path request differs");
                    pathObject=Int(e["pathObject"]);requestSequence=requested;pending=true;
                    Require(e["points"]?.Type==JTokenType.Null&&e["pathError"]?.Type==JTokenType.Null&&e["pathState"]?.Type==JTokenType.Null,"in-flight path content read");
                    SamePoint(e["destination"],destination);continue;
                }
                if(boundary=="path-complete-before"||boundary=="path-complete-after") {
                    Require(pending&&Int(e["pathObject"])==pathObject&&Int(e["requestSequence"])==requestSequence,"path callback identity differs");
                    Require(!Bool(e["pathError"])&&Text(e["pathState"])=="Complete"&&e["points"] is JArray&&e["points"].Any(),"native path failed");
                    Require(Distance(Point(e["points"].Last()),Point(destination))<=0.06,"native path endpoint differs");
                    if(boundary=="path-complete-before")Require(completed==2*(requested-1),"path completion prefix differs");
                    else {Require(completed==2*requested-1,"path completion postfix differs");pending=false;}
                    completed++;continue;
                }
                Require(boundary=="command-ended"&&!pending&&requested>0&&completed==2*requested&&Bool(e["acted"])&&Bool(e["finished"])&&Text(e["result"])=="Success","path terminal differs");ended++;
            }
            Require(bound==1&&ended==1&&requested>0,"path lifecycle incomplete");
        }

        internal static void AssertJoint(JObject p,JObject riderStep,string rider,string mount)
        {
            Require(Text(p?["contract"])=="bounded-joint-plan-before-two-separate-native-ground-inputs"&&Int(p["maximumMountCandidates"])==72&&Int(p["maximumRiderCandidatesPerMount"])==72&&Int(p["maximumJointCandidates"])==5256,"joint envelope/search bounds differ");
            var original=p["initialRiderFailure"] as JObject;Require(AssertSearch(original)==-1&&!Bool(original["joint"])&&Text(original["actorId"])==rider&&Text(original["partnerId"])==mount,"original 72 failed rider proposals missing");
            var joint=p["jointPlan"] as JObject;Require(AssertSearch(joint)>=0&&Bool(joint["joint"])&&Text(joint["actorId"])==mount&&Text(joint["partnerId"])==rider,"joint feasibility missing");
            SamePoint(original["origin"],joint["center"]);SamePoint(original["center"],joint["origin"]);
            Require(JToken.DeepEquals(original["occupants"],joint["occupants"]),"joint inventory changed before input");
            var initial=p["initialState"];Require(initial!=null&&Text(initial["relationshipState"])=="Unmounted","joint initial baseline missing");
            SamePoint(initial["geometry"]?["riderPosition"],original["origin"]);SamePoint(initial["geometry"]?["horsePosition"],original["center"]);
            var horse=p["ground"] as JObject;AssertTransaction(horse,rider,mount);AssertTransaction(riderStep,rider,mount);
            Require(Text(horse["moverId"])==mount&&Text(riderStep["moverId"])==rider&&JToken.DeepEquals(horse["plan"],joint),"joint input/plan binding differs");
            Require(Int(horse["admittedCommand"]["id"])!=Int(riderStep["admittedCommand"]["id"]),"ground commands alias");
            foreach(var field in new[]{"rider","mount","ledger","generation","relationshipState"})
                Require(JToken.DeepEquals(initial[field],horse["before"][field]),"joint baseline changed before mount input");
            foreach(var field in new[]{"riderPosition","horsePosition"}) { SamePoint(initial["geometry"][field],horse["before"]["geometry"][field]); SamePoint(horse["after"]["geometry"][field],riderStep["before"]["geometry"][field]); }
            var last=horse["resourceWindow"]["after"];var first=riderStep["resourceWindow"]["before"];
            Require(JToken.DeepEquals(last,first),"joint ground windows have an unobserved interval");
            foreach(var field in new[]{"rider","mount","ledger","generation","relationshipState"})
                Require(JToken.DeepEquals(horse["after"][field],riderStep["before"][field]),"actual rider staging baseline differs from mount terminal");
            Require(riderStep["plan"]["partnerPositionOverride"]?.Type==JTokenType.Null&&!Bool(riderStep["plan"]["joint"]),"actual rider search reused counterfactual geometry");
            SamePoint(riderStep["plan"]["origin"],horse["after"]["geometry"]["riderPosition"]);
            SamePoint(riderStep["plan"]["center"],horse["after"]["geometry"]["horsePosition"]);
        }

    }
}
