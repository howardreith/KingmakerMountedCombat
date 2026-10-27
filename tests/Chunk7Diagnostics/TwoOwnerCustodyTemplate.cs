using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace UnityEngine { internal static class Time {internal static int frameCount=1;} }
namespace KingmakerMountedCombat.Diagnostics {
 internal sealed class Blueprint { internal string AssetGuid; }
 internal sealed class Feature { internal Blueprint Source; internal int Rank=1; internal int GetRank()=>Rank; }
 internal sealed class Ref { internal UnitEntityData Value; }
 internal sealed class State { internal bool IsConscious=true,IsDead; }
 internal sealed class ClassRank { internal Blueprint CharacterClass=new Blueprint{AssetGuid="class"};internal int Level=1; }
 internal sealed class Progression { internal int Experience,CharacterLevel=1; internal ClassRank[] Classes={new ClassRank()}; }
 internal sealed class Descriptor { internal UnitEntityData Pet;internal Ref Master=new Ref();internal State State=new State();internal Progression Progression=new Progression(); internal Dictionary<Blueprint,Feature> Facts=new Dictionary<Blueprint,Feature>();internal Feature GetFact(Blueprint b)=>Facts.ContainsKey(b)?Facts[b]:null; }
 internal sealed class Cooldown { internal float MoveAction,StandardAction,SwiftAction; }
 internal sealed class Combat { internal Cooldown Cooldown=new Cooldown();internal bool Prepared;internal int AttackOfOpportunityCount=1,AttackOfOpportunityPerRound=2;internal float Initiative=3; }
 internal sealed class Commands { internal bool Empty=true; }
 internal sealed class NativeView { internal object AgentASP=new object(); }
 internal sealed class UnitEntityData { internal string UniqueId;internal Blueprint Blueprint;internal Descriptor Descriptor=new Descriptor();internal NativeView View=new NativeView();internal bool IsInState=true,IsInGame=true,Polymorph;internal Commands Commands=new Commands();internal Combat CombatState=new Combat();internal object GetActivePolymorph()=>Polymorph?this:null; }
 internal sealed class Player { internal List<Ref> PartyCharacters=new List<Ref>(),RemoteCompanions=new List<Ref>();internal long Money=20000;internal int ExperienceRatePercent=100; }
 internal sealed class Game { internal Player Player=new Player(); }
 internal sealed class Horses { internal Blueprint HorseUnit=new Blueprint{AssetGuid="horse"},HorseFeature=new Blueprint{AssetGuid="feature"},LevelRank=new Blueprint{AssetGuid="rank"}; }
 internal sealed class Probe { internal bool Released;internal int Calls;private readonly Dictionary<object,int> ids=new Dictionary<object,int>();internal int Identity(object o){if(Released)throw new InvalidOperationException("released-registry");Calls++;if(!ids.ContainsKey(o))ids.Add(o,ids.Count+1);return ids[o];} }
 internal sealed class Selection { internal string ExistingActorId,PriorOwnerId,PriorPetId,ActorId,PetId; }
 internal sealed class Actual {
 private bool TwoOwners=true,priorBound,fieldTransitionOpen,sourcePaid;private Selection selection=new Selection();
 private UnitEntityData priorOwner,priorPet,acquiredActor,acquiredPet;private UnitEntityData[] actors;
 private object priorDescriptor,priorPetDescriptor,priorView,priorPetView,priorFeature,priorFeatureSource,priorRank,acquiredDescriptor,acquiredPetDescriptor,acquiredView,acquiredPetView,acquiredFeature,acquiredRank;
 private JObject priorSource,sourceWorld,sourceObservations,evidence=new JObject();private Game game=new Game();private Horses horses=new Horses();private Probe probe=new Probe();
 private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
@@ACTUAL_METHODS@@
 private static Actual Fresh(){var x=new Actual();var a=x.Unit("prior");var b=x.Unit("prior-pet",true);var c=x.Unit("second");var d=x.Unit("second-pet",true);var u=x.Unit("unrelated");x.actors=new[]{a,b,c,d,u};x.Pair(a,b);x.Pair(c,d);x.acquiredActor=c;x.acquiredPet=d;
 x.selection.PriorOwnerId=a.UniqueId;x.selection.PriorPetId=b.UniqueId;x.selection.ActorId=c.UniqueId;x.selection.PetId=d.UniqueId;
 x.acquiredDescriptor=c.Descriptor;x.acquiredPetDescriptor=d.Descriptor;x.acquiredView=c.View;x.acquiredPetView=d.View;x.acquiredFeature=c.Descriptor.GetFact(x.horses.HorseFeature);x.acquiredRank=c.Descriptor.GetFact(x.horses.LevelRank);
 foreach(var v in new[]{a,c,u})x.game.Player.PartyCharacters.Add(new Ref{Value=v});
 x.sourceWorld=new JObject{["party"]=new JArray(new JObject{["id"]=a.UniqueId},new JObject{["id"]=b.UniqueId},new JObject{["id"]=u.UniqueId})};
 var prior=new JObject{["actorId"]=a.UniqueId,["petId"]=b.UniqueId,["actorBlueprint"]="actor",["petBlueprint"]="horse",["sourceGuid"]="source",["featureGuid"]="feature",["rank"]=1};
 var originals=new[]{a.UniqueId,b.UniqueId,u.UniqueId};
 var resources=new JObject{["actors"]=new JArray(((JArray)x.CaptureResources()["actors"]).Where(r=>originals.Contains((string)r["actorId"])).Select(r=>r.DeepClone()))};
 var progression=new JObject{["actors"]=new JArray(((JArray)x.CaptureNoArrangement()["actors"]).Where(r=>originals.Contains((string)r["actorId"])).Select(r=>r.DeepClone()))};
 var relations=new JObject{["actors"]=new JArray(x.OwnedActors().Where(v=>originals.Contains(v.UniqueId)).Select(v=>new JObject{["actorId"]=v.UniqueId,["petId"]=v.Descriptor.Pet?.UniqueId,["masterId"]=v.Descriptor.Master.Value?.UniqueId}))};
 x.sourceObservations=new JObject{["selection"]=new JObject{["priorOwnerId"]=a.UniqueId,["priorPetId"]=b.UniqueId},["cumulative"]=new JObject{["contract"]="native-one-owner-to-two-v1",["priorOwnerCount"]=1,["maximumOwnerCount"]=2,["finalOwnerCount"]=2,["capturedBeforeObserverRelease"]=true,["settledPriorOwner"]=prior,["unrelatedResourcesAfter"]=resources,["progressionAfter"]=progression,["relationshipsAfter"]=relations},["field"]=new JObject{["featureSourceGuid"]="source",["featureBlueprint"]="feature",["rank"]=1},["builderComplete"]=new JObject{["level"]=1,["xp"]=0,["petLevel"]=1,["petExperience"]=0,["rangerLevel"]=1}};
 return x;}
 private UnitEntityData Unit(string id,bool horse=false){var v=new UnitEntityData{UniqueId=id,Blueprint=horse?horses.HorseUnit:new Blueprint{AssetGuid="actor"}};v.Descriptor.Progression.Classes[0].CharacterClass.AssetGuid="cda0615668a6df14eb36ba19ee881af6";return v;}
 private void Pair(UnitEntityData a,UnitEntityData b){a.Descriptor.Pet=b;b.Descriptor.Master.Value=a;a.Descriptor.Facts[horses.HorseFeature]=new Feature{Source=new Blueprint{AssetGuid="source"}};a.Descriptor.Facts[horses.LevelRank]=new Feature();}
 private void Bind(){RequireTwoOwnerSource();BindPriorOwner();}
 private static int pass,fail;private static void Check(bool value,string name){if(value){pass++;Console.WriteLine("PASS "+name);}else{fail++;Console.WriteLine("FAIL "+name);}}
 private static bool Refuses(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
 internal static int Run(){
 var x=Fresh();x.Bind();x.RequireTwoOwnerValues();Check(x.priorBound&&x.evidence["loadedPriorOwner"]!=null,"actual two-owner cold binding and native retention");
 foreach(var name in new[]{"source prior ID","source count","source capture","source roster","prior blueprint","reciprocal","third pair","remote","source resources"}){x=Fresh();switch(name){case "source prior ID":x.sourceObservations["selection"]["priorOwnerId"]="foreign";break;case "source count":x.sourceObservations["cumulative"]["finalOwnerCount"]=3;break;case "source capture":x.sourceObservations["cumulative"]["capturedBeforeObserverRelease"]=false;break;case "source roster":((JArray)x.sourceWorld["party"]).RemoveAt(0);break;case "prior blueprint":x.actors[1].Blueprint=new Blueprint{AssetGuid="horse"};break;case "reciprocal":x.actors[1].Descriptor.Master.Value=x.acquiredActor;break;case "third pair":x.actors[4].Descriptor.Pet=x.actors[1];break;case "remote":x.game.Player.RemoteCompanions.Add(new Ref{Value=x.actors[0]});break;case "source resources":x.sourceObservations["cumulative"]["unrelatedResourcesAfter"]["actors"][0]["standard"]=1;break;}Check(Refuses(x.Bind),"actual binding refuses "+name);}
 foreach(var name in new[]{"descriptor","view","pet view","pet detached","master","feature","source","rank object","rank value","polymorph","dead","command","field","move","standard","swift","prepared","reactions","allowance","initiative","xp","level","unrelated xp","unrelated resources","third owner"}){x=Fresh();x.Bind();var p=x.priorOwner;switch(name){case "descriptor":p.Descriptor=new Descriptor();break;case "view":p.View=new NativeView();break;case "pet view":x.priorPet.View=new NativeView();break;case "pet detached":p.Descriptor.Pet=null;break;case "master":x.priorPet.Descriptor.Master.Value=x.acquiredActor;break;case "feature":p.Descriptor.Facts[x.horses.HorseFeature]=new Feature{Source=((Feature)x.priorFeature).Source};break;case "source":((Feature)x.priorFeature).Source=new Blueprint{AssetGuid="source"};break;case "rank object":p.Descriptor.Facts[x.horses.LevelRank]=new Feature();break;case "rank value":((Feature)x.priorRank).Rank++;break;case "polymorph":p.Polymorph=true;break;case "dead":p.Descriptor.State.IsDead=true;break;case "command":p.Commands.Empty=false;break;case "field":x.game.Player.PartyCharacters.RemoveAt(0);break;case "move":p.CombatState.Cooldown.MoveAction++;break;case "standard":p.CombatState.Cooldown.StandardAction++;break;case "swift":p.CombatState.Cooldown.SwiftAction++;break;case "prepared":p.CombatState.Prepared=true;break;case "reactions":p.CombatState.AttackOfOpportunityCount++;break;case "allowance":p.CombatState.AttackOfOpportunityPerRound++;break;case "initiative":p.CombatState.Initiative++;break;case "xp":p.Descriptor.Progression.Experience++;break;case "level":p.Descriptor.Progression.CharacterLevel++;break;case "unrelated xp":x.actors[4].Descriptor.Progression.Experience++;break;case "unrelated resources":x.actors[4].CombatState.Cooldown.SwiftAction++;break;case "third owner":x.actors[4].Descriptor.Pet=x.priorPet;break;}Check(Refuses(x.RequireTwoOwnerValues),"actual retained guard refuses "+name);}
 x=Fresh();x.Bind();x.CaptureTwoOwners("twoOwners");var rows=(JArray)x.evidence["twoOwners"];Check(rows.Count==2&&(string)rows[0]["actorId"]=="prior"&&(string)rows[1]["actorId"]=="second","actual ordered prior/new native owner snapshot");var saved=rows.DeepClone();var calls=x.probe.Calls;x.probe.Released=true;x.CaptureTwoOwners("twoOwners");Check(calls==x.probe.Calls&&JToken.DeepEquals(saved,x.evidence["twoOwners"]),"actual cleanup retry reuses frozen owner receipt");
 x=Fresh();x.Bind();x.acquiredActor.Descriptor.Pet=null;Check(Refuses(()=>x.CaptureTwoOwners("twoOwners")),"actual final capture refuses missing second native pair");
 x=Fresh();x.TwoOwners=false;x.Bind();x.CaptureTwoOwners("twoOwners");Check(!x.priorBound&&x.evidence.Count==0,"historical one-owner branch has no new binding or evidence");
 Console.WriteLine("TOTAL PASS="+pass+" FAIL="+fail);return fail==0?0:1;
 }
 }
 internal static class Program{private static int Main(){try{return Actual.Run();}catch(Exception error){Console.WriteLine(error);return 1;}}}
}
