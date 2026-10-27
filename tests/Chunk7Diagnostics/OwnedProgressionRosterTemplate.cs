using System;
using System.Collections.Generic;
using System.Linq;
namespace KingmakerMountedCombat.Tests {
#pragma warning disable 0649
internal sealed class UnitEntityData { internal string UniqueId; internal Descriptor Descriptor = new Descriptor(); internal object View=new object(); internal bool IsInState=true, IsInGame=true, Destroyed=false, ShouldBeDestroyed=false; internal UnitEntityData(string id){UniqueId=id;} }
internal sealed class Ref {internal UnitEntityData Value; internal Ref(UnitEntityData value){Value=value;} }
internal sealed class Descriptor {internal UnitEntityData Pet;internal Ref Master=new Ref(null);internal Feature Feature=new Feature(), Rank=new Feature();internal Feature GetFact(object key)=>ReferenceEquals(key,Fixtures.HorseFeature)?Feature:Rank;}
internal sealed class Feature {internal object Source=new object();internal int Rank=4;internal int GetRank()=>Rank;}
internal sealed class Player {internal List<UnitEntityData> Party=new List<UnitEntityData>();internal List<Ref> PartyCharacters=new List<Ref>(),RemoteCompanions=new List<Ref>();}
internal sealed class Ui {internal object CharacterBuildController=new object();}
internal sealed class NativeState { internal List<UnitEntityData> Units=new List<UnitEntityData>(); }
internal sealed class Game {internal static Game Instance;internal Player Player=new Player();internal Ui UI=new Ui();internal NativeState State=new NativeState();}
internal sealed class NativeUi {internal RespecProbe RespecProbe;internal bool NativeDismissalObserved;}
internal sealed class RespecProbe {internal bool Observed, BindingValid=true;internal int BindingCalls;internal bool TryRequireNativeRebind(object builder)=>Observed;internal void RequireObservedPetBinding(UnitEntityData pet,object descriptor,object view){BindingCalls++;if(!BindingValid||!ReferenceEquals(pet.Descriptor,descriptor)||(!ReferenceEquals(pet.View,view)&&!(pet.Destroyed&&pet.View==null)))throw new InvalidOperationException("Observed native pet binding changed.");}}
internal enum Chunk7NativeProgressionCase { ExistingHorseLevel, DismissCustomCompanion, RespecHorse, RespecMammoth }
internal sealed class Session {
 internal bool started=true;internal Chunk7NativeProgressionCase selectedCase;internal bool IsRespec=>selectedCase==Chunk7NativeProgressionCase.RespecHorse||selectedCase==Chunk7NativeProgressionCase.RespecMammoth;
 internal NativeUi nativeUi=new NativeUi();internal UnitEntityData rider,oldMount;internal object originalMountDescriptor,originalMountView;internal bool ScopeValid=true;internal int ScopeCalls;
 private void AssertOwnedScope(){ScopeCalls++;if(!ScopeValid)throw new InvalidOperationException("Exact session scope lost.");}
 /*ACTUAL_PROJECTION*/
}
internal sealed class Owner {}
internal sealed class Registry {internal Owner Current;internal Owner FindOwner(UnitEntityData actor)=>Current;}
internal sealed class World {internal Registry Registry=new Registry();}
internal sealed class Horses {internal object HorseFeature=Fixtures.HorseFeature, LevelRank=Fixtures.LevelRank;}
internal static class Chunk7NativeFieldRoster {
/*ACTUAL_FIELD_CAPTURE*/

}

internal sealed class Subject {
 internal UnitEntityData[] ownedParty,ownedCharacters,ownedActors;internal UnitEntityData rider,mount,controlRider,controlMount;internal Session session;internal World world=new World();internal Horses horses=new Horses();internal Owner ownedControl;
 internal object ownedControlFeature,ownedControlRank,ownedControlFeatureSource;internal int ownedControlRankValue;internal bool ownedWorldCleanupStarted;
 private static void Require(bool test,string error){if(!test)throw new InvalidOperationException(error);}
 private Session selection=>session;
 /*ACTUAL_PET_SETTLEMENT*/
 /*ACTUAL_ROSTER*/
 internal void Check(bool final=false)=>RequireOwnedRoster(final);
}
internal static class Fixtures {
 internal static readonly object HorseFeature=new object(),LevelRank=new object();
 internal static Subject Make(){
  Game.Instance=new Game();var main=new UnitEntityData("main");var rider=new UnitEntityData("target");var mount=new UnitEntityData("oldPet");var control=new UnitEntityData("control");var controlPet=new UnitEntityData("controlPet");rider.Descriptor.Pet=mount;mount.Descriptor.Master.Value=rider;control.Descriptor.Pet=controlPet;controlPet.Descriptor.Master.Value=control;
  Game.Instance.Player.Party.AddRange(new[]{main,rider,control});Game.Instance.Player.PartyCharacters.AddRange(Game.Instance.Player.Party.Select(x=>new Ref(x)));
  var s=new Subject {rider=rider,mount=mount,controlRider=control,controlMount=controlPet,ownedParty=Game.Instance.Player.Party.ToArray(),ownedCharacters=Game.Instance.Player.Party.ToArray(),ownedActors=new[]{main,rider,control,mount,controlPet},ownedControl=new Owner()};Game.Instance.State.Units.AddRange(s.ownedActors);s.world.Registry.Current=s.ownedControl;s.ownedControlFeature=control.Descriptor.Feature;s.ownedControlRank=control.Descriptor.Rank;s.ownedControlFeatureSource=control.Descriptor.Feature.Source;s.ownedControlRankValue=4;s.session=new Session{rider=rider,oldMount=mount,originalMountDescriptor=mount.Descriptor,originalMountView=mount.View};return s;
 }
 internal static void CompleteOldPetDisposal(Subject s){s.mount.Destroyed=true;s.mount.IsInState=false;s.mount.View=null;Game.Instance.State.Units.Remove(s.mount);}
 internal static UnitEntityData Rebind(Subject s){s.session.selectedCase=Chunk7NativeProgressionCase.RespecHorse;s.session.nativeUi.RespecProbe=new RespecProbe{Observed=true};var pet=new UnitEntityData("newPet");pet.Descriptor.Master.Value=s.rider;s.rider.Descriptor.Pet=pet;s.mount.Descriptor.Master.Value=null;s.mount.ShouldBeDestroyed=true;Game.Instance.State.Units.Add(pet);return pet;}
}
internal static class OwnedProgressionRosterTests {
 private static int passed,failed;
 private static void Good(string name,Action action){try{action();Console.WriteLine("PASS "+name);passed++;}catch(Exception e){Console.WriteLine("FAIL "+name+": "+e.Message);failed++;}}
 private static void Bad(string name,Action action){Good(name,()=>{try{action();}catch(InvalidOperationException){return;}throw new Exception("Invalid native roster was accepted.");});}
 private static void Equal(bool condition,string message){if(!condition)throw new Exception(message);}
 private static int Main(){
  Good("raw character Party with reciprocal pets admits unchanged roster",()=>Fixtures.Make().Check());
  Bad("unobserved target character removal refuses",()=>{var s=Fixtures.Make();Game.Instance.Player.Party.Remove(s.rider);Game.Instance.Player.PartyCharacters.RemoveAll(x=>ReferenceEquals(x.Value,s.rider));s.Check();});
  Bad("unrelated field character removal refuses",()=>{var s=Fixtures.Make();Game.Instance.Player.Party.RemoveAt(0);Game.Instance.Player.PartyCharacters.RemoveAt(0);s.Check();});
  Bad("same ID replacement refuses",()=>{var s=Fixtures.Make();var fresh=new UnitEntityData(s.rider.UniqueId);Game.Instance.Player.Party[1]=fresh;Game.Instance.Player.PartyCharacters[1]=new Ref(fresh);s.Check();});
  Bad("duplicate raw character refuses",()=>{var s=Fixtures.Make();Game.Instance.Player.Party.Add(s.rider);s.Check();});
  Bad("foreign raw character refuses",()=>{var s=Fixtures.Make();var fresh=new UnitEntityData("foreign");Game.Instance.Player.Party.Add(fresh);Game.Instance.Player.PartyCharacters.Add(new Ref(fresh));s.Check();});
  Bad("captured character moved remote refuses",()=>{var s=Fixtures.Make();Game.Instance.Player.RemoteCompanions.Add(new Ref(s.rider));s.Check();});
  Bad("unobserved target pet replacement refuses",()=>{var s=Fixtures.Make();var fresh=new UnitEntityData("foreignPet");fresh.Descriptor.Master.Value=s.rider;s.rider.Descriptor.Pet=fresh;s.Check();});
  Bad("unobserved target pet disappearance refuses",()=>{var s=Fixtures.Make();s.rider.Descriptor.Pet=null;s.Check();});
  Good("observed replacement pet stays outside raw Party",()=>{var s=Fixtures.Make();var pet=Fixtures.Rebind(s);s.Check();Equal(!Game.Instance.Player.Party.Contains(pet),"Test manufactured raw pet membership.");});
  Good("observed replacement final roster has exact new reciprocal pet",()=>{var s=Fixtures.Make();Fixtures.Rebind(s);Fixtures.CompleteOldPetDisposal(s);s.Check(true);});
  Good("observed intermediate null pet preserves raw rider membership",()=>{var s=Fixtures.Make();Fixtures.Rebind(s);s.rider.Descriptor.Pet=null;s.Check();});
  Bad("pet inserted into raw Party refuses",()=>{var s=Fixtures.Make();var pet=Fixtures.Rebind(s);Game.Instance.Player.Party.Add(pet);s.Check();});
  Bad("observed rebind with invalid original pet pins refuses",()=>{var s=Fixtures.Make();Fixtures.Rebind(s);s.session.nativeUi.RespecProbe.BindingValid=false;s.Check();});
  Good("observed dismissal removes only actual target character",()=>{var s=Fixtures.Make();s.session.selectedCase=Chunk7NativeProgressionCase.DismissCustomCompanion;s.session.nativeUi.NativeDismissalObserved=true;Game.Instance.Player.Party.Remove(s.rider);Game.Instance.Player.PartyCharacters.RemoveAll(x=>ReferenceEquals(x.Value,s.rider));s.Check(true);});
  Bad("dismissal flag cannot remove unrelated character",()=>{var s=Fixtures.Make();s.session.selectedCase=Chunk7NativeProgressionCase.DismissCustomCompanion;s.session.nativeUi.NativeDismissalObserved=true;Game.Instance.Player.Party.Remove(s.rider);Game.Instance.Player.PartyCharacters.RemoveAll(x=>ReferenceEquals(x.Value,s.rider));Game.Instance.Player.Party.Remove(s.controlRider);s.Check(true);});
  Bad("control owner retirement before cleanup refuses",()=>{var s=Fixtures.Make();s.world.Registry.Current=null;s.Check();});
  Good("declared final world cleanup may retire exact control",()=>{var s=Fixtures.Make();s.ownedWorldCleanupStarted=true;s.world.Registry.Current=null;s.Check(true);});
  Bad("cleanup cannot substitute control owner",()=>{var s=Fixtures.Make();s.ownedWorldCleanupStarted=true;s.world.Registry.Current=new Owner();s.Check(true);});
  Bad("control sourced feature replacement refuses",()=>{var s=Fixtures.Make();s.controlRider.Descriptor.Feature=new Feature();s.Check();});
  Bad("control rank mutation refuses",()=>{var s=Fixtures.Make();s.controlRider.Descriptor.Rank.Rank++;s.Check();});
  Bad("control reciprocal pet mutation refuses",()=>{var s=Fixtures.Make();s.controlMount.Descriptor.Master.Value=null;s.Check();});
  Bad("session world guard precedes lifecycle projection",()=>{var s=Fixtures.Make();s.session.ScopeValid=false;s.Check();});
  Good("unstarted session never exposes native transition",()=>{var s=Fixtures.Make();s.session.started=false;s.session.selectedCase=Chunk7NativeProgressionCase.DismissCustomCompanion;s.session.nativeUi.NativeDismissalObserved=true;bool removed;UnitEntityData pet;Equal(!s.session.TryGetObservedTargetRoster(out removed,out pet)&&!removed&&ReferenceEquals(pet,s.mount),"Unstarted session invented transition.");});
  Bad("final reciprocal pet without live view retains settlement",()=>{var s=Fixtures.Make();var pet=Fixtures.Rebind(s);pet.View=null;s.Check(true);});
  Bad("final reciprocal pet absent native state refuses",()=>{var s=Fixtures.Make();var pet=Fixtures.Rebind(s);pet.IsInState=false;s.Check(true);});
  Bad("native PartyCharacters substitution refuses",()=>{var s=Fixtures.Make();Game.Instance.Player.PartyCharacters[0]=new Ref(new UnitEntityData("main"));s.Check();});
  Bad("reciprocal companion moved remote refuses",()=>{var s=Fixtures.Make();Game.Instance.Player.RemoteCompanions.Add(new Ref(s.controlMount));s.Check();});
  Bad("pending native old pet blocks final completion",()=>{var s=Fixtures.Make();Fixtures.Rebind(s);s.Check(true);});
  Good("pending native old pet remains permitted before final settlement",()=>{var s=Fixtures.Make();Fixtures.Rebind(s);s.Check();});
  Bad("disposed flag alone cannot hide retained state entry",()=>{var s=Fixtures.Make();Fixtures.Rebind(s);s.mount.Destroyed=true;s.mount.IsInState=false;s.mount.View=null;s.Check(true);});
  Bad("disposed state entry removal cannot hide retained view",()=>{var s=Fixtures.Make();Fixtures.Rebind(s);s.mount.Destroyed=true;s.mount.IsInState=false;Game.Instance.State.Units.Remove(s.mount);s.Check(true);});
  Good("native disposed old pet may retain historical IsInGame backing flag",()=>{var s=Fixtures.Make();Fixtures.Rebind(s);Fixtures.CompleteOldPetDisposal(s);Equal(s.mount.IsInGame,"Fixture should preserve raw historical flag.");s.Check(true);});
  Console.WriteLine("Owned progression actual roster: "+passed+" PASS / "+failed+" FAIL");return failed==0?0:1;
 }
}
}
