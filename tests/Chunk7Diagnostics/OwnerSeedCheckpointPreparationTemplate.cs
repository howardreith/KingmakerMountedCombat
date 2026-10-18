using System;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Domain{enum RelationshipState{Mounted,Unmounted}}
namespace KingmakerMountedCombat.Diagnostics{
class Actor{internal string UniqueId;internal bool Idle=true;}
class Relationship{internal Domain.RelationshipState State=Domain.RelationshipState.Mounted;internal Runtime Runtime=new Runtime();}
class Runtime{internal bool NoPreparedPairResidue;}
class Participant{internal bool CanReleaseBoundary;}
class Combat{internal bool HasRetainedPairCommands;}
class MountedPairServices{internal Relationship Relationship=new Relationship();internal Participant Participant=new Participant();internal Combat Combat=new Combat();internal Actor Rider,Mount;}
class Owner{internal Guid Id=Guid.NewGuid();internal long Generation=4;internal Actor Rider,Mount;internal Relationship Boundary;}
class Registry{internal List<Owner> Rows=new List<Owner>();internal Owner[] Owners=>Rows.ToArray();internal int ActiveCount=>Rows.Count(o=>o.Boundary.State==Domain.RelationshipState.Mounted);internal Owner FindOwner(Actor a)=>Rows.SingleOrDefault(o=>ReferenceEquals(o.Rider,a)||ReferenceEquals(o.Mount,a));internal Owner FindActive(Actor a){var o=FindOwner(a);return o?.Boundary.State==Domain.RelationshipState.Mounted?o:null;}}
class Unified{internal bool HasUnsettledPreparation;}
class World{internal Registry Registry=new Registry();internal Unified UnifiedTurn=new Unified();internal MountedPairServices[] Pairs;internal bool SerializationBusy;internal MountedPairServices ForActor(Actor a)=>Pairs.SingleOrDefault(p=>ReferenceEquals(p.Rider,a)||ReferenceEquals(p.Mount,a));}
class Slot{public string ActorId;public int Index,Kind;}
class Controls{internal bool HasUnsettledRelationshipTransition;internal int NativeCastRequestCount;internal object DismountAbility=new object();internal Slot[] Slots;internal Slot[] CapturePersistentSlots()=>Slots;}
class Persistence{internal int SnapshotCount;internal bool HasActiveSaveScope,HasSaveAdmissionFence;}
class Inherited{internal bool Changed;internal void RequirePreserved(){if(Changed)throw new InvalidOperationException("changed inherited actor");}}
class Player{internal bool IsInCombat;}
class Game{internal static Game Instance=new Game();internal Player Player=new Player();}
class Request{internal string PersistenceCase="multi-nonselected-mount-unsettled-manual";}
static class NativePersistenceIsolation{internal static bool HasPendingWrites;}
static class MountedSaveCodec{internal static JsonSerializer CreateSerializer()=>new JsonSerializer();}
static class Chunk7NativeFixture{internal static bool Idle(Actor a)=>a.Idle;}
class Preparation{
 internal bool OwnerSeedCheckpointSave=true,PendingMountSave=true,LegacyPendingCase,ShellFinished,DebtChanged;
 private bool OwnerSeedSingleOwnerPreparation=>OwnerSeedCheckpointSave&&(PendingMountSave||LegacyPendingCase);
 internal World world=new World();internal Controls controls=new Controls();internal Persistence persistence=new Persistence();internal Inherited inherited=new Inherited();internal Request request=new Request();
 internal Actor firstRider=new Actor{UniqueId="rider-a"},firstMount=new Actor{UniqueId="mount-a"},secondRider=new Actor{UniqueId="rider-b"},secondMount=new Actor{UniqueId="mount-b"};internal Actor[] actors;
 internal Dictionary<Guid,long> expectedOwners=new Dictionary<Guid,long>();internal Slot[] slots;internal object saved=new object();internal int snapshotBefore,stage,Dispatched,Finished,DebtChecks;internal object Ability;internal Actor Caster,Target,Selected;
 internal MountedPairServices ownerSeedPreparationPair;internal Guid ownerSeedReleasedOwner;internal JObject ownerSeedPreparation,evidence=new JObject();internal JArray inputs=new JArray(),snapshots=new JArray();
 internal Owner A,B;
 internal Preparation(){Game.Instance=new Game();NativePersistenceIsolation.HasPendingWrites=false;actors=new[]{firstRider,firstMount,secondRider,secondMount};world.Pairs=new[]{new MountedPairServices{Rider=firstRider,Mount=firstMount},new MountedPairServices{Rider=secondRider,Mount=secondMount}};A=new Owner{Rider=firstRider,Mount=firstMount,Boundary=world.Pairs[0].Relationship,Generation=9};B=new Owner{Rider=secondRider,Mount=secondMount,Boundary=world.Pairs[1].Relationship,Generation=12};world.Registry.Rows.Add(A);world.Registry.Rows.Add(B);expectedOwners.Add(A.Id,A.Generation);expectedOwners.Add(B.Id,B.Generation);slots=new[]{new Slot{ActorId="rider-a",Index=2,Kind=2},new Slot{ActorId="rider-a",Index=3,Kind=3},new Slot{ActorId="rider-b",Index=4,Kind=2},new Slot{ActorId="rider-b",Index=5,Kind=3}};controls.Slots=slots;}
 static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 bool Mounted(Actor rider,Actor mount){var owner=world.Registry.FindActive(rider);return owner!=null&&ReferenceEquals(owner.Rider,rider)&&ReferenceEquals(owner.Mount,mount);}
 bool InputSettled()=>ShellFinished&&!controls.HasUnsettledRelationshipTransition;
 void Input(Actor caster,Actor target,object ability,Actor other,string label){Caster=caster;Target=target;Ability=ability;Selected=other;inputs.Add(new JObject{["label"]=label,["nativeCastDelta"]=1,["nativeRefusalDelta"]=0});controls.NativeCastRequestCount++;}
 void FinishInput(){Finished++;inputs.Last["terminal"]=true;}
 void Next(int value){stage=value;}
 void DispatchCheckpoint(){Dispatched++;}
 void AssertDebt(object archive){DebtChecks++;if(DebtChanged)throw new InvalidOperationException("changed native debt");}
 void AssertSlots(){Require(slots.Length==controls.Slots.Length&&slots.All(s=>controls.Slots.Count(x=>x.ActorId==s.ActorId&&x.Index==s.Index&&x.Kind==s.Kind)==1),"changed slots");}
 JObject Capture(string label)=>new JObject{["label"]=label,["nativeCastRequests"]=controls.NativeCastRequestCount,["snapshots"]=persistence.SnapshotCount,["owners"]=new JArray(world.Registry.Rows.Select(o=>new JObject{["ownerId"]=o.Id.ToString("N"),["generation"]=o.Generation}))};
 internal void Begin()=>BeginOwnerSeedCheckpointPreparation();internal void Poll()=>PollOwnerSeedCheckpointPreparation();internal void Slots()=>RequireOwnerSeedPreparedSlots();internal void Adopt()=>RememberOwnerSeedRemountedSecondPair();
 internal void Dismounted(){ShellFinished=true;world.Registry.Rows.Remove(B);B.Boundary.State=Domain.RelationshipState.Unmounted;world.Pairs[1].Relationship.Runtime.NoPreparedPairResidue=true;world.Pairs[1].Participant.CanReleaseBoundary=true;controls.Slots=slots.Take(2).Select(s=>new Slot{ActorId=s.ActorId,Index=s.Index,Kind=s.Kind}).ToArray();}
 internal void Remounted(){var b=new Owner{Rider=secondRider,Mount=secondMount,Boundary=world.Pairs[1].Relationship,Generation=14};b.Boundary.State=Domain.RelationshipState.Mounted;world.Registry.Rows.Add(b);}
__METHODS__
}
static class Program{
 static int passed,failed;static void Check(string name,Action test){try{test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 static void True(bool value,string message){if(!value)throw new Exception(message);}static void Reject(Action action){bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}True(rejected,"unexpected acceptance");}
 static Preparation Ready(){var p=new Preparation();p.Begin();p.Dismounted();return p;}
 static void Wait(string name,Action<Preparation> change){Check(name,()=>{var p=Ready();change(p);p.Poll();True(p.Dispatched==0&&p.expectedOwners.Count==2&&p.Finished==0,"premature ownership adoption or dispatch");});}
 static void Refuse(string name,Action<Preparation> change){Check(name,()=>{var p=Ready();change(p);Reject(p.Poll);True(p.Dispatched==0&&p.expectedOwners.Count==2,"failed preparation discarded an expected owner");});}
 static int Main(){
 Check("actual preparation uses native B self-Dismount and leaves A selected",()=>{var p=new Preparation();p.Begin();True(ReferenceEquals(p.Caster,p.secondRider)&&ReferenceEquals(p.Target,p.secondRider)&&ReferenceEquals(p.Ability,p.controls.DismountAbility)&&ReferenceEquals(p.Selected,p.firstRider)&&p.stage==6,"native input routed to another actor");True(p.Dispatched==0&&p.expectedOwners.Count==2&&p.slots.Length==4,"pre-input adoption occurred");Reject(p.Begin);});
 Check("settled native Dismount adopts only surviving A slots and original identity",()=>{var p=Ready();var id=p.A.Id;var generation=p.A.Generation;p.Poll();True(p.Dispatched==1&&p.Finished==1&&p.DebtChecks==1&&p.expectedOwners.Count==1&&p.expectedOwners[id]==generation&&p.slots.Length==2,"first owner changed or second retained");p.Slots();True((bool)p.ownerSeedPreparation["after"]["secondRiderOwnerAbsent"]&&(bool)p.ownerSeedPreparation["after"]["secondPairCanRelease"]&&(bool)p.ownerSeedPreparation["input"]["terminal"]&&p.snapshots.Count==1,"settlement evidence incomplete");Reject(p.Poll);});
 Check("legacy pending dispatch shares exact native preparation",()=>{var p=Ready();p.PendingMountSave=false;p.LegacyPendingCase=true;p.Poll();True(p.Dispatched==1&&p.expectedOwners.Count==1,"legacy did not retain one owner");});
 Wait("unfinished native shell waits",p=>p.ShellFinished=false);Wait("second owner custody waits",p=>p.world.Registry.Rows.Add(p.B));Wait("moving actor waits",p=>p.secondRider.Idle=false);Wait("second prepared residue waits",p=>p.world.Pairs[1].Relationship.Runtime.NoPreparedPairResidue=false);Wait("second participant release waits",p=>p.world.Pairs[1].Participant.CanReleaseBoundary=false);Wait("retained second command waits",p=>p.world.Pairs[1].Combat.HasRetainedPairCommands=true);
 Refuse("changed first generation refuses",p=>p.A.Generation++);Refuse("changed first identity refuses",p=>p.A.Id=Guid.NewGuid());Refuse("changed first slot refuses",p=>p.controls.Slots[0].Index++);Refuse("retained second slot refuses",p=>p.controls.Slots=p.slots);Refuse("extra native cast refuses",p=>p.controls.NativeCastRequestCount++);Refuse("intervening snapshot refuses",p=>p.persistence.SnapshotCount++);Refuse("combat entry refuses",p=>Game.Instance.Player.IsInCombat=true);Refuse("unsettled native preparation refuses",p=>p.world.UnifiedTurn.HasUnsettledPreparation=true);Refuse("busy world refuses",p=>p.world.SerializationBusy=true);Refuse("active save scope refuses",p=>p.persistence.HasActiveSaveScope=true);Refuse("pending native writer refuses",p=>NativePersistenceIsolation.HasPendingWrites=true);Refuse("changed native debt refuses",p=>p.DebtChanged=true);Refuse("changed inherited actor refuses",p=>p.inherited.Changed=true);
 Check("pending Mount records only actual new B owner",()=>{var p=Ready();p.Poll();var a=p.A.Id;p.Remounted();var b=p.world.Registry.FindActive(p.secondRider);p.Adopt();True(p.expectedOwners.Count==2&&p.expectedOwners[a]==p.A.Generation&&p.expectedOwners[b.Id]==b.Generation&&(string)p.ownerSeedPreparation["remountedSecondOwner"]["ownerId"]==b.Id.ToString("N"),"remount overwrote A or invented B");Reject(p.Adopt);});
 Check("prepared slot adoption refuses changed A",()=>{var p=Ready();p.Poll();p.A.Generation++;Reject(p.Slots);});
 Check("remount refuses reused retired B identity",()=>{var p=Ready();p.Poll();p.Remounted();p.world.Registry.FindActive(p.secondRider).Id=p.B.Id;Reject(p.Adopt);True(p.expectedOwners.Count==1,"retired B imported");});
 Check("remount refuses changed A generation",()=>{var p=Ready();p.Poll();p.Remounted();p.A.Generation++;Reject(p.Adopt);});
 Console.WriteLine("OWNER SEED ACTUAL PREPARATION METHODS PASS="+passed+" FAIL="+failed+"; native world/input/debt boundaries are facades, no native gameplay claim");return failed==0?0:1;
}
}
}
