using System;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Domain{enum RelationshipState{Mounted,Unmounted}}
namespace KingmakerMountedCombat.Diagnostics{
class Actor{internal string UniqueId;internal bool Idle=true;internal Descriptor Descriptor=new Descriptor();}
class Descriptor{internal Abilities Abilities=new Abilities();}
class Blueprint{internal string AssetGuid;}
class Caster{internal Actor Unit;}
class AbilityData{internal Blueprint Blueprint;internal Caster Caster;}
class Fact{internal Blueprint Blueprint;internal AbilityData Data;internal bool Active=true;}
class Abilities{internal List<Fact> Enumerable=new List<Fact>();internal Fact GetAbility(Blueprint b)=>Enumerable.FirstOrDefault(f=>ReferenceEquals(f.Blueprint,b));}
static class NativeMountedControlService{
 internal const string MountAbilityGuid="f053faad986631688defa003cd7bda0e",DismountAbilityGuid="3af2b81f4d72bbb30501fa730fcdf36e",RiderPrimaryAbilityGuid="27364df661b3c121eabb97a31aa73a83",MountPrimaryAbilityGuid="f88a50d6fdbebbd709c3e323d2f52f5e",MountedChargeAbilityGuid="d79eaec224a7a832e738eb81baef9d49";
}
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
class Controls{internal bool HasUnsettledRelationshipTransition;internal int NativeCastRequestCount;
 internal Blueprint MountAbility=new Blueprint{AssetGuid=NativeMountedControlService.MountAbilityGuid},DismountAbility=new Blueprint{AssetGuid=NativeMountedControlService.DismountAbilityGuid},RiderPrimaryAbility=new Blueprint{AssetGuid=NativeMountedControlService.RiderPrimaryAbilityGuid},MountPrimaryAbility=new Blueprint{AssetGuid=NativeMountedControlService.MountPrimaryAbilityGuid},MountedChargeAbility=new Blueprint{AssetGuid=NativeMountedControlService.MountedChargeAbilityGuid};
 internal Slot[] Slots;internal Slot[] CapturePersistentSlots()=>Slots.Select(s=>new Slot{ActorId=s.ActorId,Index=s.Index,Kind=s.Kind}).ToArray();}
class Persistence{internal int SnapshotCount;internal bool HasActiveSaveScope,HasSaveAdmissionFence;}
class Inherited{internal bool Changed;internal void RequirePreserved(){if(Changed)throw new InvalidOperationException("changed inherited actor");}}
class Player{internal bool IsInCombat;}
class Game{internal static Game Instance=new Game();internal Player Player=new Player();internal bool IsPaused;}
class Request{internal string PersistenceCase="multi-nonselected-mount-unsettled-manual";}
static class NativePersistenceIsolation{internal static bool HasPendingWrites;}
static class MountedSaveCodec{internal static JsonSerializer CreateSerializer()=>new JsonSerializer();}
static class NativeMountedSaveFormat{internal static int SelectedWriterSchema=3;}
static class Chunk7NativeFixture{internal static bool Idle(Actor a)=>a.Idle;}
class Preparation{
 internal bool OwnerSeedCheckpointSave=true,PendingMountSave=true,LegacyPendingCase,OwnedSchema1SourceCase,ShellFinished,DebtChanged;
 private bool OwnerSeedSingleOwnerPreparation=>OwnerSeedCheckpointSave&&(PendingMountSave||LegacyPendingCase||OwnedSchema1SourceCase);
 internal World world=new World();internal Controls controls=new Controls();internal Persistence persistence=new Persistence();internal Inherited inherited=new Inherited();internal Request request=new Request();
 internal Actor firstRider=new Actor{UniqueId="rider-a"},firstMount=new Actor{UniqueId="mount-a"},secondRider=new Actor{UniqueId="rider-b"},secondMount=new Actor{UniqueId="mount-b"};internal Actor[] actors;
 internal Dictionary<Guid,long> expectedOwners=new Dictionary<Guid,long>();internal Slot[] slots;internal object saved=new object();internal int snapshotBefore,stage,Dispatched,Finished,DebtChecks;internal object Ability;internal Actor Caster,Target,Selected;
 internal MountedPairServices ownerSeedPreparationPair;internal Guid ownerSeedReleasedOwner;internal JObject ownerSeedPreparation,evidence=new JObject();internal JArray inputs=new JArray(),snapshots=new JArray();
 internal Owner A,B;
 internal JObject ownedSchema1FieldBaseline;internal JArray ownedSchema1Fields;internal int RememberCalls,BindCalls,OtherSlotsPinned,SaveRequests;
 internal Preparation(){Game.Instance=new Game();NativePersistenceIsolation.HasPendingWrites=false;actors=new[]{firstRider,firstMount,secondRider,secondMount};world.Pairs=new[]{new MountedPairServices{Rider=firstRider,Mount=firstMount},new MountedPairServices{Rider=secondRider,Mount=secondMount}};A=new Owner{Rider=firstRider,Mount=firstMount,Boundary=world.Pairs[0].Relationship,Generation=9};B=new Owner{Rider=secondRider,Mount=secondMount,Boundary=world.Pairs[1].Relationship,Generation=12};world.Registry.Rows.Add(A);world.Registry.Rows.Add(B);expectedOwners.Add(A.Id,A.Generation);expectedOwners.Add(B.Id,B.Generation);slots=new[]{new Slot{ActorId="rider-a",Index=2,Kind=2},new Slot{ActorId="rider-a",Index=3,Kind=3},new Slot{ActorId="rider-b",Index=4,Kind=2},new Slot{ActorId="rider-b",Index=5,Kind=3}};controls.Slots=slots;}
 static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 internal void MountedFacts(){foreach(var a in new[]{firstRider,secondRider})foreach(var b in new[]{controls.DismountAbility,controls.RiderPrimaryAbility,controls.MountPrimaryAbility})AddFact(a,b);}
 internal Fact AddFact(Actor actor,Blueprint b){var f=new Fact{Blueprint=b,Data=new AbilityData{Blueprint=b,Caster=new Caster{Unit=actor}}};actor.Descriptor.Abilities.Enumerable.Add(f);return f;}
 internal JObject Facts()=>CaptureOwnerSeedControlFacts();
 void RememberOwners(){RememberCalls++;expectedOwners.Add(A.Id,A.Generation);}
 void BindSlots(){BindCalls++;}
 void AssertOwnedSchema1Owner(){Require(world.Registry.Owners.Length==1&&world.Registry.FindActive(firstRider)==A&&world.Registry.FindOwner(secondRider)==null,"changed schema1 owner");}
 void PinOwnedSchema1OtherSlots(){OtherSlotsPinned++;}
 JObject CaptureOwnedSchema1Field(string phase)=>new JObject{["phase"]=phase,["actors"]=new JArray()};
 void RequestSave(){SaveRequests++;}
 internal void BeginSchema1()=>BeginOwnedSchema1Source();
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
 internal void Dismounted(){ShellFinished=true;world.Registry.Rows.Remove(B);B.Boundary.State=Domain.RelationshipState.Unmounted;world.Pairs[1].Relationship.Runtime.NoPreparedPairResidue=true;world.Pairs[1].Participant.CanReleaseBoundary=true;controls.Slots=slots.Take(2).Select(s=>new Slot{ActorId=s.ActorId,Index=s.Index,Kind=s.Kind}).ToArray();secondRider.Descriptor.Abilities.Enumerable.Clear();AddFact(secondRider,controls.MountAbility);}
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
 Check("native fact matrix records mounted A3 and B3 across all twenty actor kinds",()=>{var p=new Preparation();p.MountedFacts();var m=p.Facts();True(((JArray)m["rows"]).Count==20&&(int)m["totalFacts"]==6&&(int)m["activeFacts"]==6,"incomplete matrix");foreach(var row in m["rows"]){var rider=(string)row["actorId"]==p.firstRider.UniqueId||(string)row["actorId"]==p.secondRider.UniqueId;var kind=(int)row["kind"];True((int)row["totalFacts"]==(rider&&kind>=2&&kind<=4?1:0),"foreign actor/kind received a fact");foreach(var f in row["facts"])foreach(var key in new[]{"active","factBlueprintExact","dataPresent","dataBlueprintExact","casterExact","nativeLookupExact"})True((bool)f[key],"exact fact association missing");}});
 Check("native fact matrix distinguishes retired B Mount from stale hotbar templates",()=>{var p=new Preparation();p.MountedFacts();p.Begin();p.Dismounted();p.Poll();var m=p.ownerSeedPreparation["controlFactsAfter"];True((int)m["totalFacts"]==4&&(int)m["activeFacts"]==4&&p.slots.Length==2,"facts conflated with live slots");foreach(var row in m["rows"]){var id=(string)row["actorId"];var kind=(int)row["kind"];var expected=id==p.firstRider.UniqueId&&kind>=2&&kind<=4||id==p.secondRider.UniqueId&&kind==1;True((int)row["totalFacts"]==(expected?1:0),"retired B leaked a mounted fact");}});
 foreach(var flaw in new[]{"fact-alias","data-alias","caster","missing-data","inactive","duplicate","data-only"}){var fault=flaw;Check("native fact matrix exposes "+fault,()=>{var p=new Preparation();var f=p.AddFact(p.firstRider,p.controls.DismountAbility);switch(fault){case "fact-alias":f.Blueprint=new Blueprint{AssetGuid=f.Blueprint.AssetGuid};break;case "data-alias":f.Data.Blueprint=new Blueprint{AssetGuid=f.Data.Blueprint.AssetGuid};break;case "caster":f.Data.Caster.Unit=p.secondRider;break;case "missing-data":f.Data=null;break;case "inactive":f.Active=false;break;case "duplicate":p.AddFact(p.firstRider,p.controls.DismountAbility);break;case "data-only":f.Blueprint=new Blueprint{AssetGuid="foreign"};break;}var m=p.Facts();var row=m["rows"].Single(r=>(string)r["actorId"]==p.firstRider.UniqueId&&(int)r["kind"]==2);True((int)row["totalFacts"]==(fault=="duplicate"?2:1),"malformed native fact hidden");var key=fault=="fact-alias"||fault=="data-only"?"factBlueprintExact":fault=="data-alias"?"dataBlueprintExact":fault=="caster"?"casterExact":fault=="missing-data"?"dataPresent":fault=="inactive"?"active":"nativeLookupExact";True(row["facts"].Any(r=>!(bool)r[key]),"malformed association reported exact");});}
 Check("native fact matrix exposes compensated A loss and B leakage at unchanged total",()=>{var p=new Preparation();p.MountedFacts();p.Dismounted();p.firstRider.Descriptor.Abilities.Enumerable.RemoveAt(0);p.AddFact(p.secondRider,p.controls.DismountAbility);var m=p.Facts();True((int)m["totalFacts"]==4,"aggregate changed");True((int)m["rows"].Single(r=>(string)r["actorId"]==p.firstRider.UniqueId&&(int)r["kind"]==2)["totalFacts"]==0&&(int)m["rows"].Single(r=>(string)r["actorId"]==p.secondRider.UniqueId&&(int)r["kind"]==2)["totalFacts"]==1,"compensation hid wrong ownership");});
 Check("schema1 source shares exact native B retirement",()=>{var p=Ready();p.PendingMountSave=false;p.OwnedSchema1SourceCase=true;p.Poll();True(p.Dispatched==1&&p.expectedOwners.Count==1,"schema1 source did not adopt A");});
 Check("schema1 source adopts restored A once without rebinding slots or adding a duplicate save snapshot",()=>{var p=Ready();p.PendingMountSave=false;p.OwnedSchema1SourceCase=true;p.Poll();var id=p.A.Id;var slot=p.slots[0];p.BeginSchema1();True(p.RememberCalls==0&&p.BindCalls==0&&p.SaveRequests==1&&p.OtherSlotsPinned==1&&p.snapshots.Count==1&&p.expectedOwners.Count==1&&p.expectedOwners[id]==p.A.Generation&&ReferenceEquals(slot,p.slots[0]),"source rebound restored owners/slots or duplicated save boundary");});
 Check("schema1 source rejects changed live slots before pinning or writing",()=>{var p=Ready();p.PendingMountSave=false;p.OwnedSchema1SourceCase=true;p.Poll();p.controls.Slots[0].Index++;Reject(p.BeginSchema1);True(p.SaveRequests==0&&p.OtherSlotsPinned==0&&p.BindCalls==0&&p.RememberCalls==0,"failed slot adoption reached write");});
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
