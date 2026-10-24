using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
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
 internal sealed class UnitEntityData { internal string UniqueId;internal Blueprint Blueprint;internal Descriptor Descriptor=new Descriptor();internal object View=new object();internal bool IsInState=true,IsInGame=true,Polymorph;internal Commands Commands=new Commands();internal Combat CombatState=new Combat();internal object GetActivePolymorph()=>Polymorph?this:null; }
 internal sealed class Player { internal List<Ref> PartyCharacters=new List<Ref>(),RemoteCompanions=new List<Ref>();internal long Money=20000;internal int ExperienceRatePercent=100; }
 internal sealed class Game { internal Player Player=new Player(); }
 internal sealed class Horses { internal Blueprint HorseUnit=new Blueprint{AssetGuid="horse"},HorseFeature=new Blueprint{AssetGuid="feature"},LevelRank=new Blueprint{AssetGuid="rank"}; }
 internal sealed class Probe { internal bool Released;internal int Calls;private readonly Dictionary<object,int> ids=new Dictionary<object,int>();internal int Identity(object o){if(Released)throw new InvalidOperationException("released-registry");Calls++;if(!ids.ContainsKey(o))ids.Add(o,ids.Count+1);return ids[o];} }
 internal sealed class Selection { internal string ExistingActorId,PriorOwnerId,PriorPetId; }
 internal sealed class Actual {
 private Selection cumulative=new Selection(),selection=new Selection();private UnitEntityData priorOwner,priorPet,acquiredActor,acquiredPet;private UnitEntityData[] actors;
 private object priorOwnerDescriptor,priorPetDescriptor,priorOwnerView,priorPetView,priorFeature,priorFeatureSource,priorRank;
 private JObject cumulativeSourceOwner,cumulativeInitialResources,cumulativeInitialRelationships,cumulativeInitialProgression,sourceObservations,evidence=new JObject();
 private bool cumulativeBound,acquisitionProven;private Game game=new Game();private Horses horses=new Horses();private Probe probe=new Probe();
 private static void Require(bool v,string reason){if(!v)throw new InvalidOperationException(reason);}
// Source projection is a separately tested boundary; this original matrix uses its legacy saved resources.
 private JToken SelectedSourceResources => sourceObservations["resourcesAfterSave"];
@@ACTUAL_METHODS@@
 private static Actual Fresh(){var a=new Actual();var owner=a.Unit("owner");var pet=a.Unit("pet",true);var target=a.Unit("target");a.actors=new[]{owner,pet,target};owner.Descriptor.Pet=pet;pet.Descriptor.Master.Value=owner;var feature=new Feature{Source=new Blueprint{AssetGuid="source"}};owner.Descriptor.Facts[a.horses.HorseFeature]=feature;owner.Descriptor.Facts[a.horses.LevelRank]=new Feature();a.game.Player.PartyCharacters.Add(new Ref{Value=owner});a.game.Player.PartyCharacters.Add(new Ref{Value=target});a.cumulative.PriorOwnerId="owner";a.cumulative.PriorPetId="pet";a.selection.ExistingActorId="target";a.cumulativeSourceOwner=new JObject{["actorId"]="owner",["petId"]="pet",["actorBlueprint"]="actor",["petBlueprint"]="horse",["sourceGuid"]="source",["featureGuid"]="feature",["rank"]=1};a.sourceObservations=new JObject{["resourcesAfterSave"]=a.CaptureCumulativeResources(false)};return a;}
 private UnitEntityData Unit(string id,bool horse=false)=>new UnitEntityData{UniqueId=id,Blueprint=horse?horses.HorseUnit:new Blueprint{AssetGuid="actor"}};
 private static int pass,fail;private static void Check(bool v,string n){if(v){pass++;Console.WriteLine("PASS "+n);}else{fail++;Console.WriteLine("FAIL "+n);}}
 private static bool Refuses(Action a){try{a();return false;}catch(InvalidOperationException){return true;}}
 internal static int Run(){
 var x=Fresh();x.BindCumulativeOwner();x.RequireCumulativeValues();Check(x.cumulativeBound&&x.evidence["cumulative"]!=null,"actual native prior-pair bind and conserved values");
 foreach(var kind in new[]{"wrong owner","wrong pet blueprint","reciprocal","additional pet","remote","saved resources"}){x=Fresh();switch(kind){case "wrong owner":x.cumulative.PriorOwnerId="foreign";break;case "wrong pet blueprint":x.actors[1].Blueprint=new Blueprint{AssetGuid="horse"};break;case "reciprocal":x.actors[1].Descriptor.Master.Value=x.actors[2];break;case "additional pet":x.actors[2].Descriptor.Pet=x.actors[1];break;case "remote":x.game.Player.RemoteCompanions.Add(new Ref{Value=x.actors[0]});break;case "saved resources":x.sourceObservations["resourcesAfterSave"]["actors"][0]["standard"]=1;break;}Check(Refuses(x.BindCumulativeOwner),"actual bind refuses "+kind);}
 foreach(var kind in new[]{"descriptor","view","pet view","pet detached","master replaced","feature","source","rank object","rank value","polymorph","dead","command","field","move","standard","swift","prepared","reactions","allowance","initiative","xp","level"}){x=Fresh();x.BindCumulativeOwner();var p=x.priorOwner;switch(kind){case "descriptor":p.Descriptor=new Descriptor();break;case "view":p.View=new object();break;case "pet view":x.priorPet.View=new object();break;case "pet detached":p.Descriptor.Pet=null;break;case "master replaced":x.priorPet.Descriptor.Master.Value=x.actors[2];break;case "feature":p.Descriptor.Facts[x.horses.HorseFeature]=new Feature{Source=((Feature)x.priorFeature).Source};break;case "source":((Feature)x.priorFeature).Source=new Blueprint{AssetGuid="source"};break;case "rank object":p.Descriptor.Facts[x.horses.LevelRank]=new Feature();break;case "rank value":((Feature)x.priorRank).Rank++;break;case "polymorph":p.Polymorph=true;break;case "dead":p.Descriptor.State.IsDead=true;break;case "command":p.Commands.Empty=false;break;case "field":x.game.Player.PartyCharacters.RemoveAt(0);break;case "move":p.CombatState.Cooldown.MoveAction++;break;case "standard":p.CombatState.Cooldown.StandardAction++;break;case "swift":p.CombatState.Cooldown.SwiftAction++;break;case "prepared":p.CombatState.Prepared=true;break;case "reactions":p.CombatState.AttackOfOpportunityCount++;break;case "allowance":p.CombatState.AttackOfOpportunityPerRound++;break;case "initiative":p.CombatState.Initiative++;break;case "xp":p.Descriptor.Progression.Experience++;break;case "level":p.Descriptor.Progression.CharacterLevel++;break;}Check(Refuses(x.RequireCumulativeValues),"actual retained guard refuses "+kind);}
 x=Fresh();x.BindCumulativeOwner();x.acquiredActor=x.actors[2];x.acquiredPet=x.Unit("new-pet",true);x.acquiredActor.Descriptor.Pet=x.acquiredPet;x.acquiredPet.Descriptor.Master.Value=x.acquiredActor;x.acquisitionProven=true;x.CaptureCumulativeFinal();Check((int)x.evidence["cumulative"]["finalOwnerCount"]==2,"actual final receipt retains exactly two native pairs");var captured=x.evidence["cumulative"].DeepClone();var calls=x.probe.Calls;x.probe.Released=true;x.CaptureCumulativeFinal();Check(x.probe.Calls==calls&&JToken.DeepEquals(x.evidence["cumulative"],captured),"frozen final receipt survives released-registry cleanup retry");
 x=Fresh();x.BindCumulativeOwner();x.acquiredActor=x.actors[2];x.acquiredPet=x.Unit("new-pet",true);x.acquisitionProven=true;Check(Refuses(x.CaptureCumulativeFinal),"actual final refuses missing second pair");
 Console.WriteLine("TOTAL PASS="+pass+" FAIL="+fail);return fail==0?0:1;
 }
 }
 internal static class Program{private static int Main(){try{return Actual.Run();}catch(Exception error){Console.WriteLine(error);return 1;}}}
}
