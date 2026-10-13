using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Diagnostics;
namespace Kingmaker { public sealed class Player { public List<UnitEntityData> Party=new List<UnitEntityData>(); public List<Ref> PartyCharacters=new List<Ref>(); public Ref MainCharacter=new Ref(); public IEnumerable<UnitEntityData> AllCharacters {get{return Party;}} } public sealed class Ref { public UnitEntityData Value; } public sealed class State {public List<UnitEntityData> Units=new List<UnitEntityData>();} public sealed class Game {public static Game Instance;public Player Player=new Player();public State State=new State();}}
namespace Kingmaker.EntitySystem.Entities { public enum Size {Small,Medium} public sealed class Blueprint {public string AssetGuid;} public sealed class NativeState {public Size Size=Size.Small;} public sealed class Feature {public Blueprint Source;} public sealed class Rank {public int Value=4;public int GetRank(){return Value;}} public sealed class Descriptor {public UnitEntityData Pet;public Ref Master=new Ref();public Size OriginalSize=Size.Small;public NativeState State=new NativeState();public Feature Feature;public Rank Rank;public object FeatureKey,RankKey;public dynamic GetFact(object key){return ReferenceEquals(key,FeatureKey)?(object)Feature:(object)Rank;}} public sealed class UnitEntityData { public string UniqueId; public Blueprint Blueprint; public Descriptor Descriptor=new Descriptor();}}
public sealed class BlueprintRoot {public static BlueprintRoot Instance=new BlueprintRoot();public Blueprint CustomCompanion=new Blueprint{AssetGuid="custom"};}
public sealed class Horses {public Blueprint HorseUnit=new Blueprint{AssetGuid="horse"};public Blueprint HorseFeature=new Blueprint{AssetGuid="feature"};public Blueprint LevelRank=new Blueprint{AssetGuid="rank"};}
public sealed class Plan {public string ActorId="owner",PetId="pet";}
public sealed class Calibration {public Plan ManualSource=new Plan();}
public sealed class ActualSmallRoster {
 Calibration calibration=new Calibration();Horses horses=new Horses();JObject manualSource,manualArrangement;UnitEntityData actor,pet,main;UnitEntityData[] manualActors,manualCharacters,originalActors;object manualFeature,manualRank,manualFeatureSource;
 static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 /*EXACT*/
 /*CAPTURE*/
 /*IDENTITY*/
 JObject CaptureManualArrangement(){return new JObject();}
 ActualSmallRoster(){Game.Instance=new Game();main=new UnitEntityData{UniqueId="main",Blueprint=new Blueprint{AssetGuid="mainbp"}};actor=new UnitEntityData{UniqueId="owner",Blueprint=BlueprintRoot.Instance.CustomCompanion};pet=new UnitEntityData{UniqueId="pet",Blueprint=horses.HorseUnit};actor.Descriptor.Pet=pet;pet.Descriptor.Master.Value=actor;Game.Instance.Player.Party.AddRange(new[]{main,actor});Game.Instance.Player.PartyCharacters.AddRange(new[]{new Ref{Value=main},new Ref{Value=actor}});Game.Instance.Player.MainCharacter.Value=main;Game.Instance.State.Units.AddRange(new[]{main,actor,pet});manualSource=new JObject{["savedWorld"]=new JObject{["mainActorId"]="main",["party"]=new JArray(new[]{main,actor,pet}.Select(x=>new JObject{["id"]=x.UniqueId,["blueprint"]=x.Blueprint.AssetGuid}))},["savedOwner"]=new JObject{["sourceGuid"]="source",["featureGuid"]="feature",["rank"]=4}};manualFeature=new Feature{Source=new Blueprint{AssetGuid="source"}};manualRank=new Rank();manualFeatureSource=((Feature)manualFeature).Source;actor.Descriptor.Feature=(Feature)manualFeature;actor.Descriptor.Rank=(Rank)manualRank;actor.Descriptor.FeatureKey=horses.HorseFeature;actor.Descriptor.RankKey=horses.LevelRank;manualActors=new[]{main,actor,pet};manualCharacters=new[]{main,actor};originalActors=new[]{main};manualArrangement=new JObject();}
 static int pass,fail;static void Check(string name,Action<ActualSmallRoster> action,bool refuse=false){var x=new ActualSmallRoster();Exception caught=null;try{action(x);}catch(Exception e){caught=e;}if((caught!=null)==refuse){pass++;Console.WriteLine("PASS "+name);}else{fail++;Console.WriteLine("FAIL "+name+" "+caught);}}
 public static int Verify(){
 Check("native character Party plus exact reciprocal pet is admitted",x=>{var a=x.CaptureManualFieldRoster();Require(a.Length==3&&a.Contains(x.actor)&&a.Contains(x.pet)&&!Game.Instance.Player.Party.Contains(x.pet),"split roster missing");});
 Check("native characters may be enumerated in another order",x=>{Game.Instance.Player.Party.Reverse();x.CaptureManualFieldRoster();});
 Check("pet cannot masquerade as a raw party character",x=>{Game.Instance.Player.Party.Add(x.pet);Game.Instance.Player.PartyCharacters.Add(new Ref{Value=x.pet});x.CaptureManualFieldRoster();},true);
 Check("PartyCharacters missing exact owner refuses",x=>{Game.Instance.Player.PartyCharacters.RemoveAt(1);x.CaptureManualFieldRoster();},true);
 Check("same ID replacement PartyCharacters refuses",x=>{Game.Instance.Player.PartyCharacters[1].Value=new UnitEntityData{UniqueId="owner",Blueprint=x.actor.Blueprint};x.CaptureManualFieldRoster();},true);
 Check("extra native character refuses saved roster",x=>{var u=new UnitEntityData{UniqueId="foreign",Blueprint=new Blueprint{AssetGuid="foreign"}};Game.Instance.Player.Party.Add(u);Game.Instance.Player.PartyCharacters.Add(new Ref{Value=u});x.CaptureManualFieldRoster();},true);
 Check("missing reciprocal master refuses",x=>{x.pet.Descriptor.Master.Value=null;x.CaptureManualFieldRoster();},true);
 Check("different master refuses",x=>{x.pet.Descriptor.Master.Value=x.main;x.CaptureManualFieldRoster();},true);
 Check("second inherited pet refuses sole owner",x=>{var u=new UnitEntityData{UniqueId="otherpet",Blueprint=x.horses.HorseUnit};u.Descriptor.Master.Value=x.main;x.main.Descriptor.Pet=u;Game.Instance.State.Units.Add(u);((JArray)x.manualSource["savedWorld"]["party"]).Add(new JObject{["id"]=u.UniqueId,["blueprint"]=u.Blueprint.AssetGuid});x.CaptureManualFieldRoster();},true);
 Check("pet aliasing a character refuses",x=>{x.actor.Descriptor.Pet=x.main;x.main.Descriptor.Master.Value=x.actor;x.CaptureManualFieldRoster();},true);
 Check("duplicate actor identity in native state refuses",x=>{Game.Instance.State.Units.Add(new UnitEntityData{UniqueId="pet",Blueprint=x.horses.HorseUnit});x.CaptureManualFieldRoster();},true);
 Check("wrong pet blueprint refuses",x=>{x.pet.Blueprint=new Blueprint{AssetGuid="horse"};x.CaptureManualFieldRoster();},true);
 Check("wrong main ID refuses",x=>{x.manualSource["savedWorld"]["mainActorId"]="owner";x.CaptureManualFieldRoster();},true);
 Check("native character and explicit pet identities remain valid",x=>x.RequireManualFieldIdentity());
 Check("changed raw character membership refuses before mutation",x=>{Game.Instance.Player.Party.Remove(x.actor);Game.Instance.Player.PartyCharacters.RemoveAt(1);x.RequireManualFieldIdentity();},true);
 Check("replacement native character object refuses",x=>{var n=new UnitEntityData{UniqueId="main",Blueprint=x.main.Blueprint};Game.Instance.Player.Party[0]=n;Game.Instance.Player.PartyCharacters[0].Value=n;x.RequireManualFieldIdentity();},true);
 Check("replacement pet reference refuses",x=>{var n=new UnitEntityData{UniqueId="pet",Blueprint=x.pet.Blueprint};n.Descriptor.Master.Value=x.actor;x.actor.Descriptor.Pet=n;x.RequireManualFieldIdentity();},true);
 Check("rebound main to another captured actor refuses",x=>{Game.Instance.Player.MainCharacter.Value=x.actor;x.RequireManualFieldIdentity();},true);
 Check("original sourced fact replacement refuses",x=>{x.actor.Descriptor.Feature=new Feature{Source=(Blueprint)x.manualFeatureSource};x.RequireManualFieldIdentity();},true);
 Console.WriteLine("ACTUAL SMALL MANUAL ROSTER PASS "+pass+" FAIL "+fail);return fail;
 }
}
