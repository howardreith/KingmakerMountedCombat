using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
class UnitEntityData { public string UniqueId; }
class Outcome { public string Result="Success",ActorId="rider",ResourceOwnerId="rider",TargetId="target";public bool NativeAttackRuleObserved=true,SingleAttackMode=true;public int NativeCompletedAttackCount=1; }
class Combat { public Outcome LastOutcome=new Outcome(); }
class Pair { public Combat Combat=new Combat(); }
class Trace { public JArray Events=new JArray();public bool Complete=true;public JArray EventsSince(int offset)=>new JArray(Events.Skip(offset).Select(value=>value.DeepClone())); }
public class ActualSmallPrimary {
 private bool ready=true;
 private Pair pair=new Pair();private Outcome previousOutcome;
 private UnitEntityData target=new UnitEntityData{UniqueId="target"};private int attackOffset;
 private Trace trace=new Trace();private JObject input=new JObject();
 private bool CommandsSettled()=>ready;
 private JObject State()=>new JObject();
 private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
/*ACTUAL*/
 private static JObject Cost(string boundary,int sequence,float standard)=>new JObject{["boundary"]=boundary,["sequence"]=sequence,["command"]=42,["commandActor"]="rider",["commandType"]="KingmakerMountedCombat.Integration.MountedPairAttackCommand",["actionType"]="Standard",["acted"]=true,["nativeTurnBased"]=false,["state"]=new JObject{["actor"]="rider",["standard"]=standard,["move"]=.25f,["swift"]=0f,["reactions"]=0,["reactionCooldown"]=0f}};
 private static ActualSmallPrimary New(){var a=new ActualSmallPrimary();a.trace.Events.Add(Cost("cost-before",1,0));a.trace.Events.Add(Cost("cost-after",2,6));return a;}
 private static int passes,failures;
 private static void Check(string name,Func<bool> test){try{if(!test())throw new InvalidOperationException("expected result differs");passes++;Console.WriteLine("PASS "+name);}catch(Exception e){failures++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 private static bool Refuses(Action<ActualSmallPrimary> change){var a=New();change(a);try{return !a.FinishPrimary(new UnitEntityData{UniqueId="rider"});}catch(InvalidOperationException){return true;}}
 public static int Verify(){
  Check("actual RT cost-before/after completes one actor primary without TB actor-cost hooks",()=>New().FinishPrimary(new UnitEntityData{UniqueId="rider"}));
  Check("unfinished native command remains pending",()=>Refuses(a=>a.ready=false));
  Check("foreign native resource actor refused",()=>Refuses(a=>a.trace.Events[1]["state"]["actor"]="mount"));
  Check("missing native standard expenditure refused",()=>Refuses(a=>a.trace.Events[1]["state"]["standard"]=0f));
  Check("duplicate concrete cost refused",()=>Refuses(a=>a.trace.Events.Add(a.trace.Events[1].DeepClone())));
  Check("trace overflow refuses proof",()=>Refuses(a=>a.trace.Complete=false));
  Check("native multiattack outcome refused",()=>Refuses(a=>a.pair.Combat.LastOutcome.NativeCompletedAttackCount=2));
  Check("wrong resource owner outcome refused",()=>Refuses(a=>a.pair.Combat.LastOutcome.ResourceOwnerId="mount"));
  Check("TB-only actor-cost pair does not replace RT entry boundary",()=>Refuses(a=>{a.trace.Events[0]["boundary"]="actor-cost-before";a.trace.Events[1]["boundary"]="actor-cost-after";}));
  Console.WriteLine("ACTUAL SMALL PRIMARY PASS "+passes+" FAIL "+failures);return failures;
 }
}
