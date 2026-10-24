// Compiles the unchanged six-pair persistence partial against native boundary facades.
// It certifies stage orchestration, waiting, identity recording and refusal only; never
// native Mount, save, restoration or movement behavior.
using System;using System.Collections.Generic;using System.Linq;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
namespace UnityEngine { struct Vector3 { public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 right=>new Vector3(1,0,0);
 public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator *(Vector3 a,float s)=>new Vector3(a.x*s,a.y*s,a.z*s); } }
namespace Kingmaker.UI.UnitSettings { class MechanicActionBarSlot { } class MechanicActionBarSlotEmpty:MechanicActionBarSlot { }
 class MechanicActionBarSlotAbility:MechanicActionBarSlot { public Kingmaker.EntitySystem.Entities.UnitEntityData Unit;public object Ability; } }
namespace Kingmaker.EntitySystem.Entities {
 class UnitEntityData { public string UniqueId=Guid.NewGuid().ToString();public bool IsInState=true,IsInGame=true,IsInCombat;public View View=new View();public Descriptor Descriptor=new Descriptor();
  public Commands Commands=new Commands();public UISettings UISettings=new UISettings();public UnityEngine.Vector3 Position; }
 class View { public object AgentASP=new object(); } class Commands { public bool Empty=true; }
 class Descriptor { public UnitState State=new UnitState();public Abilities Abilities=new Abilities(); } class UnitState { public bool IsConscious=true; }
 class Fact { public object Data=new object(); } class Abilities { public Dictionary<object,Fact> Facts=new Dictionary<object,Fact>();public Fact GetAbility(object blueprint)=>blueprint!=null&&Facts.TryGetValue(blueprint,out var f)?f:null; }
 class UISettings { public Kingmaker.UI.UnitSettings.MechanicActionBarSlot[] Slots=new Kingmaker.UI.UnitSettings.MechanicActionBarSlot[128];public void SetSlot(Kingmaker.UI.UnitSettings.MechanicActionBarSlot slot,int index){Slots[index]=slot;} }
}
namespace Kingmaker { class Game { public static Game Instance;public object CurrentlyLoadedArea; } }
namespace KingmakerMountedCombat.Domain { enum NativeMountedControlKind { None, MountCompanion, Dismount, RiderPrimary, MountPrimary, MountedCharge } }
namespace KingmakerMountedCombat.Integration {
 static class GameMountedRelationshipService { public static int NativePairCapacity=6; }
 class SavedActor { public string Id; } class SavedPair { public string PairId;public long Generation;public string ProfileId;public SavedActor Rider,Mount; }
 class SavedMountedSlot { public string ActorId;public int Index,Kind; }
 class MountedArchiveData { public int SchemaVersion=3;public string CampaignId="campaign",AreaId="area";public SavedPair[] Pairs=new SavedPair[0];public object[] Participations=new object[0];public SavedMountedSlot[] Slots=new SavedMountedSlot[0];public object Combat; }
 class Profile { public string Id; }
 static class SupportedMountedProfiles { public static Func<Kingmaker.EntitySystem.Entities.UnitEntityData,Kingmaker.EntitySystem.Entities.UnitEntityData,string> Id=(r,m)=>"profile-"+r.UniqueId.Substring(0,4);
  public static Profile Resolve(Kingmaker.EntitySystem.Entities.UnitEntityData r,Kingmaker.EntitySystem.Entities.UnitEntityData m)=>new Profile{Id=Id(r,m)}; }
 static class MountedSaveCodec { public static JsonSerializer CreateSerializer()=>JsonSerializer.CreateDefault(); }
}
namespace KingmakerMountedCombat.Diagnostics {
 using Actor=Kingmaker.EntitySystem.Entities.UnitEntityData;using KingmakerMountedCombat.Integration;using UnityEngine;
 static class Chunk7OwnedPairsSelection { public const string SixPairSourceRun="qualified-six-source"; }
 class Selection { public bool SixPairPersistence=true;public string[] PairIds;public string CheckpointSourceRunId="six-producer",CheckpointSourceArtifactSha256=new string('9',64); }
 class Descriptor { public string InternalName="KMC_C7_SIX_MOUNTED_WORLD",FileName="Manual_309_KMC_C7_SIX_MOUNTED_WORLD.zks",Sha256=new string('8',64),GameId="campaign",GameName="native",Area="area";public long Length=10,LastWriteTimeUtcTicks=1; }
 class Request { public Selection OwnedPairs=new Selection();public Descriptor PersistenceLoad=new Descriptor(); }
 class Owner { public Guid Id=Guid.NewGuid();public long Generation=1;public Actor Rider,Mount; }
 class Registry { public List<Owner> Items=new List<Owner>();public int ActiveCount=>Items.Count;public Owner[] Owners=>Items.ToArray();
  public Owner FindOwner(Actor a)=>Items.SingleOrDefault(x=>ReferenceEquals(x.Rider,a)||ReferenceEquals(x.Mount,a));public Owner FindActive(Actor a)=>FindOwner(a); }
 class Unified { public bool HasUnsettledPreparation; } class World { public Registry Registry=new Registry();public Unified UnifiedTurn=new Unified(); }
 class Inherited { public Actor[] Actors=Enumerable.Range(0,12).Select(i=>new Actor()).ToArray();public bool Bound=true,Reject;public void RequirePreserved(){if(Reject)throw new InvalidOperationException("inherited native custody changed");} }
 class Controls { public object MountAbility=new object(),DismountAbility=new object(),RiderPrimaryAbility=new object();public int NativeCastRequestCount,NativeRefusalCount,Updates;public bool HasUnsettledRelationshipTransition;
  public Func<SavedMountedSlot[]> Capture;public int ManagedSlots=-1;public void Update(){Updates++;}public SavedMountedSlot[] CapturePersistentSlots()=>Capture();
  // Before binding, the facade counts managed slots from the actual hotbars unless a test pins a value.
  public Func<int> CountManaged;public ControlSnapshot CaptureSnapshot()=>new ControlSnapshot{ManagedHotbarSlotCount=ManagedSlots>=0?ManagedSlots:CountManaged()}; }
 class ControlSnapshot { public int ManagedHotbarSlotCount; }
 class Persistence { public int SnapshotCount,SemanticRestoreCount,PresentationRestoreCount,ArchivePostLoadObservedCount,FailedAreaPairCount;public MountedArchiveData LoadedArchive;public string ArchiveRestorePhase="not-selected",Feedback="feedback"; }
 class Probe { public bool Complete=true,Disposed;public Dictionary<string,int> Counts=new Dictionary<string,int>{{"restore-debt-before",12},{"restore-debt-after",12}};
  public int Count(string kind,IEnumerable<string> ids)=>Counts.TryGetValue(kind,out var n)?n:0;public JObject Capture()=>new JObject();public void Dispose(){Disposed=true;} }
 class Scope { public bool Reject;public void RequireCurrent(){if(Reject)throw new InvalidOperationException("captured native world changed");}
  public void RequireDeclaredAreaDestination(string area,object blueprint){if(Reject)throw new InvalidOperationException("declared area destination changed");} }
 class Area { public string AssetGuidThreadSafe="area"; }
 class Row { public string Status; }
 internal sealed partial class Chunk7PersistenceScenario {
  readonly Request request=new Request();bool Cold;Area replayDestination;Inherited inherited=new Inherited();Actor[] actors;readonly World world=new World();readonly Dictionary<Guid,long> expectedOwners=new Dictionary<Guid,long>();
  readonly JArray inputs=new JArray(),movementRows=new JArray(),snapshots=new JArray();readonly Controls controls=new Controls();readonly Persistence persistence=new Persistence();readonly JObject evidence=new JObject();
  int stage;SavedMountedSlot[] slots;MountedArchiveData saved;Probe restoration;string ExpectedArchiveArea="area";
  bool captured=true,cleanup,cleanupProven,pendingHostCustody,disposed,cleanupPendingReported;string RetainedFailureArtifactSha256;readonly List<string> errors=new List<string>();readonly List<Row> results=new List<Row>();Scope nativeScope=new Scope();
  readonly List<string> passed=new List<string>();int dispatches,inputPending=-1,movePending=-1,slotAsserts,debtAsserts;bool holdInput,holdMove;Actor pendingRider,pendingMount;
  static void Require(bool condition,string error){if(!condition)throw new InvalidOperationException(error);}
  void Next(int value){stage=value;} void Pass(string row){passed.Add(row);}
  void Input(Actor caster,Actor target,object ability,Actor other,string label){Require(inputPending<0&&ReferenceEquals(ability,controls.MountAbility),"native Mount replay before settlement");controls.NativeCastRequestCount++;inputs.Add(new JObject{{"label",label}});pendingRider=caster;pendingMount=target;inputPending=3;}
  bool InputSettled()=>inputPending==0;
  void FinishInput(){Require(InputSettled(),"unfinished native shell");inputPending=-1;}
  bool Mounted(Actor rider,Actor mount){var o=world.Registry.FindActive(rider);return o!=null&&ReferenceEquals(o.Mount,mount)&&ReferenceEquals(world.Registry.FindActive(mount),o);}
  void Move(Actor selected,Actor executor,Vector3 goal,Actor other,string label){Require(movePending<0,"native movement replay");movementRows.Add(new JObject{{"label",label},{"selectedId",selected.UniqueId},{"executorId",executor.UniqueId},{"goalX",goal.x}});movePending=3;}
  bool MovementSettled(){if(movePending!=0)return false;movePending=-1;return true;}
  JObject Capture(string label)=>new JObject{{"label",label}};
  void BeginCleanup(){cleanup=true;}
  void DispatchCheckpoint(){dispatches++;}
  void BindOwnedFixture(bool combatCold){Require(!combatCold,"cold six-pair consumer bound as combat");actors=inherited.Actors;}
  void AssertSlots(){slotAsserts++;} void AssertDebt(MountedArchiveData archive){Require(ReferenceEquals(archive,saved),"debt compared with another graph");debtAsserts++;}
  void TickNative(){
   if(inputPending>0&&!holdInput&&--inputPending==0)world.Registry.Items.Add(new Owner{Rider=pendingRider,Mount=pendingMount});
   if(movePending>0&&!holdMove)movePending--;
  }
  void Drive(int count=2000){for(var i=0;i<count&&!cleanup&&dispatches==0;i++){TickNative();UpdateSixPairPersistence();}}
  void DriveContinuation(int count=2000){for(var i=0;i<count&&!cleanup;i++){TickNative();UpdateSixPairPersistence();}}
  SavedMountedSlot[] BoundSlots()=>actors.Where((a,i)=>i%2==0).SelectMany(a=>a.UISettings.Slots.Select((s,i)=>new{s,i}).Where(x=>x.s is Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility)
   .Select(x=>new SavedMountedSlot{ActorId=a.UniqueId,Index=x.i,Kind=ReferenceEquals(((Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility)x.s).Ability,a.Descriptor.Abilities.GetAbility(controls.DismountAbility).Data)?2:3})).ToArray();
  MountedArchiveData ArchiveOfRegistry(){var a=new MountedArchiveData();
   a.Pairs=Enumerable.Range(0,6).Select(i=>{var o=world.Registry.FindOwner(actors[i*2]);return new SavedPair{PairId=o.Id.ToString("D"),Generation=o.Generation,ProfileId=SupportedMountedProfiles.Resolve(actors[i*2],actors[i*2+1]).Id,Rider=new SavedActor{Id=actors[i*2].UniqueId},Mount=new SavedActor{Id=actors[i*2+1].UniqueId}};}).ToArray();
   a.Slots=Enumerable.Range(0,6).SelectMany(i=>new[]{new SavedMountedSlot{ActorId=actors[i*2].UniqueId,Index=10,Kind=2},new SavedMountedSlot{ActorId=actors[i*2].UniqueId,Index=11,Kind=3}}).ToArray();return a;}
  static Chunk7PersistenceScenario Producer(){
   GameMountedRelationshipService.NativePairCapacity=6;var e=new Chunk7PersistenceScenario();e.actors=e.inherited.Actors;e.request.OwnedPairs.PairIds=e.actors.Select(a=>a.UniqueId).ToArray();
   foreach(var rider in e.actors.Where((a,i)=>i%2==0)){rider.Descriptor.Abilities.Facts[e.controls.DismountAbility]=new Kingmaker.EntitySystem.Entities.Fact();rider.Descriptor.Abilities.Facts[e.controls.RiderPrimaryAbility]=new Kingmaker.EntitySystem.Entities.Fact();
    for(var s=0;s<5;s++)rider.UISettings.Slots[s]=new Kingmaker.UI.UnitSettings.MechanicActionBarSlot();}
   e.controls.Capture=e.BoundSlots;e.controls.CountManaged=()=>e.BoundSlots().Length;return e;}
  static Chunk7PersistenceScenario ColdConsumer(){
   var e=Producer();e.Cold=true;e.actors=null;
   for(var i=0;i<6;i++)e.world.Registry.Items.Add(new Owner{Rider=e.inherited.Actors[i*2],Mount=e.inherited.Actors[i*2+1],Generation=4});
   e.actors=e.inherited.Actors;e.persistence.LoadedArchive=e.ArchiveOfRegistry();e.actors=null;e.controls.Capture=()=>e.persistence.LoadedArchive.Slots;
   e.persistence.ArchiveRestorePhase="all-pair-presentation-restored";e.persistence.SemanticRestoreCount=12;e.persistence.PresentationRestoreCount=6;e.persistence.ArchivePostLoadObservedCount=12;
   e.restoration=new Probe();return e;}
  // The host-side steps between the partial's stages: one native write, then the read-back record.
  void CompleteSave(){Require(dispatches==1&&persistence.SnapshotCount==0,"save dispatched twice or early");persistence.SnapshotCount=1;saved=ArchiveOfRegistry();RecordSixPairSaved();BeginSixPairContinuation();}
  static int checks,failures;static void Check(bool ok,string label){checks++;Console.WriteLine((ok?"PASS ":"FAIL ")+label);if(!ok)failures++;}
  static void Refuses(string label,Action action){var refused=false;try{action();}catch(InvalidOperationException){refused=true;}Check(refused,label);}
  public static int Main(string[] args){
   // The game's Unity build of Json.NET initializes Unity converters; resolve only those
   // managed assemblies from the installed game folder beside the referenced Json.NET.
   var managed=System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(args[0]));
   AppDomain.CurrentDomain.AssemblyResolve+=(sender,requested)=>{
    var name=new System.Reflection.AssemblyName(requested.Name).Name;
    if(name!="UnityEngine"&&!name.StartsWith("UnityEngine.",StringComparison.Ordinal))return null;
    var path=System.IO.Path.GetFullPath(System.IO.Path.Combine(managed,name+".dll"));
    return path.StartsWith(managed+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&System.IO.File.Exists(path)?System.Reflection.Assembly.LoadFrom(path):null;
   };
   var p=Producer();p.BeginSixPairMounts();p.Drive();
   Check(p.dispatches==1&&p.inputs.Count==6&&p.controls.NativeCastRequestCount==6&&p.inputs.Select(x=>(string)x["label"]).SequenceEqual(Enumerable.Range(0,6).Select(i=>"six-mount-"+i)),"actual partial issues one settled native Mount per sourced role in order, then dispatches one save");
   Check(p.sixMounted==6&&p.expectedOwners.Count==6&&Enumerable.Range(0,6).All(i=>p.sixOwnerIds[i]==p.world.Registry.FindOwner(p.actors[i*2]).Id),"actual partial records each minted owner identity and generation");
   Check(p.slots.Length==12&&p.actors.Where((a,i)=>i%2==0).All(r=>r.UISettings.Slots[5] is Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility&&r.UISettings.Slots[6] is Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility&&r.UISettings.Slots[7]==null)&&p.controls.Updates==1,"actual partial binds each rider's own Dismount and Rider Primary into its first empty slots");
   Check(p.snapshots.Count==1&&(string)p.snapshots[0]["label"]=="before-native-save"&&p.passed.SequenceEqual(new[]{"C7-persistence-six-native-mounts"}),"actual partial records the pre-save boundary and its mounts row once");
   p.CompleteSave();p.DriveContinuation();
   Check(p.cleanup&&p.movementRows.Count==6&&p.movementRows.Select(x=>(string)x["label"]).SequenceEqual(Enumerable.Range(0,6).Select(i=>"six-pair-"+i))&&p.movementRows.All(x=>(float)x["goalX"]==2f),"actual partial routes one fixed ground continuation through every pair before cleanup");
   Check(p.passed.SequenceEqual(new[]{"C7-persistence-six-native-mounts","C7-persistence-six-mounted-archive","C7-persistence-six-native-continuation"})&&(int)p.sixObservation["savedPairs"]==6&&(int)p.sixObservation["nativeCastRequests"]==6&&((JArray)p.sixObservation["owners"]).Count==6,"actual producer records its archive, owners and counts once");
   var h=Producer();h.BeginSixPairMounts();h.holdInput=true;h.Drive(50);Check(h.inputs.Count==1&&h.stage==301&&h.dispatches==0,"a pending native Mount never replays or advances");h.holdInput=false;h.Drive();Check(h.dispatches==1&&h.inputs.Count==6,"the held Mount continues exactly once after release");
   var b=Producer();b.BeginSixPairMounts();b.actors[7].Commands.Empty=false;b.Drive(100);Check(b.inputs.Count==0&&b.stage==300,"a busy actor blocks the next native Mount");b.actors[7].Commands.Empty=true;b.Drive();Check(b.dispatches==1,"the producer continues once every actor is idle");
   var m=Producer();m.BeginSixPairMounts();m.Drive();m.CompleteSave();m.holdMove=true;m.DriveContinuation(60);Check(m.movementRows.Count==1&&!m.cleanup,"a pending ground continuation never replays");m.holdMove=false;m.DriveContinuation();Check(m.cleanup&&m.movementRows.Count==6,"the held continuation completes exactly once");
   Refuses("wrong native capacity refuses before input",()=>{var e=Producer();GameMountedRelationshipService.NativePairCapacity=2;try{e.BeginSixPairMounts();}finally{GameMountedRelationshipService.NativePairCapacity=6;}});
   Refuses("eleven roles refuse before input",()=>{var e=Producer();e.actors=e.actors.Take(11).ToArray();e.BeginSixPairMounts();});
   Refuses("reordered roles refuse before input",()=>{var e=Producer();var a=e.actors[0];e.actors[0]=e.actors[2];e.actors[2]=a;e.BeginSixPairMounts();});
   Refuses("prior pair custody refuses before input",()=>{var e=Producer();e.world.Registry.Items.Add(new Owner{Rider=new Actor(),Mount=new Actor()});e.BeginSixPairMounts();});
   Refuses("a prior native cast refuses before input",()=>{var e=Producer();e.controls.NativeCastRequestCount=1;e.BeginSixPairMounts();});
   Refuses("a prior save refuses before input",()=>{var e=Producer();e.persistence.SnapshotCount=1;e.BeginSixPairMounts();});
   Refuses("the cold consumer cannot begin producer mounts",()=>{var e=Producer();e.Cold=true;e.BeginSixPairMounts();});
   Refuses("an unbound fixture refuses before input",()=>{var e=Producer();e.inherited.Bound=false;e.BeginSixPairMounts();});
   Refuses("changed inherited custody refuses during mounts",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive(10);e.inherited.Reject=true;e.Drive();});
   Refuses("an unrelated owner refuses during mounts",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive(10);e.world.Registry.Items.Add(new Owner{Rider=new Actor(),Mount=new Actor()});e.Drive();});
   Refuses("a reminted owner refuses during mounts",()=>{var e=Producer();e.BeginSixPairMounts();for(var i=0;i<200&&e.sixMounted<2;i++){e.TickNative();e.UpdateSixPairPersistence();}e.world.Registry.Items[0].Generation=9;e.Drive();});
   var w=Producer();w.BeginSixPairMounts();w.Drive(2);w.pendingMount=w.actors[3];w.Drive(200);
   Check(w.inputs.Count==1&&w.stage==301&&w.sixMounted==0&&w.dispatches==0,"an owner minted on another role never advances or replays (the bounded stage deadline fails it)");
   Refuses("an undeclared native cast during the mounts refuses before binding or saving",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive(10);e.controls.NativeCastRequestCount++;e.Drive();});
   Refuses("a reused owner identity refuses",()=>{var e=Producer();e.BeginSixPairMounts();for(var i=0;i<200&&e.sixMounted<1;i++){e.TickNative();e.UpdateSixPairPersistence();}
    var first=e.world.Registry.Items[0].Id;for(var i=0;i<200&&e.inputPending!=1;i++){e.TickNative();e.UpdateSixPairPersistence();}e.TickNative();e.world.Registry.Items[1].Id=first;e.UpdateSixPairPersistence();});
   Refuses("a managed control already on a hotbar refuses before binding",()=>{var e=Producer();e.controls.ManagedSlots=1;e.BeginSixPairMounts();e.Drive();});
   Refuses("a persistent control already captured refuses before binding",()=>{var e=Producer();var r=e.actors[8];r.UISettings.Slots[3]=new Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility{Unit=r,Ability=r.Descriptor.Abilities.GetAbility(e.controls.DismountAbility).Data};e.BeginSixPairMounts();e.Drive();});
   var z=Producer();z.BeginSixPairMounts();z.Drive();
   Check(z.sixObservation["managedHotbarSlotsBeforeBinding"].Value<int>()==0&&z.slots.Length==12,"the producer records zero managed hotbar controls before its declared binding");
   Refuses("a rider without an empty hotbar slot refuses",()=>{var e=Producer();for(var s=0;s<128;s++)e.actors[4].UISettings.Slots[s]=new Kingmaker.UI.UnitSettings.MechanicActionBarSlot();e.BeginSixPairMounts();e.Drive();});
   Refuses("a missing Dismount fact refuses",()=>{var e=Producer();e.actors[6].Descriptor.Abilities.Facts.Remove(e.controls.DismountAbility);e.BeginSixPairMounts();e.Drive();});
   Refuses("an eleven-slot persistent capture refuses",()=>{var e=Producer();e.controls.Capture=()=>e.BoundSlots().Skip(1).ToArray();e.BeginSixPairMounts();e.Drive();});
   Refuses("a foreign slot kind refuses",()=>{var e=Producer();e.controls.Capture=()=>{var s=e.BoundSlots();if(s.Length>0)s[0].Kind=4;return s;};e.BeginSixPairMounts();e.Drive();});
   Refuses("a slot on a mount refuses",()=>{var e=Producer();e.controls.Capture=()=>{var s=e.BoundSlots();if(s.Length>0)s[0].ActorId=e.actors[1].UniqueId;return s;};e.BeginSixPairMounts();e.Drive();});
   Refuses("an archive owner differing from the minted owner refuses",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive();e.persistence.SnapshotCount=1;e.saved=e.ArchiveOfRegistry();e.saved.Pairs[2].PairId=Guid.NewGuid().ToString("D");e.RecordSixPairSaved();});
   Refuses("an archive generation differing from the minted owner refuses",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive();e.saved=e.ArchiveOfRegistry();e.saved.Pairs[5].Generation=2;e.RecordSixPairSaved();});
   Refuses("an archive profile differing from the declared profile refuses",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive();e.saved=e.ArchiveOfRegistry();e.saved.Pairs[1].ProfileId="medium-humanoid-horse-v1";e.RecordSixPairSaved();});
   Refuses("a five-pair archive refuses",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive();e.saved=e.ArchiveOfRegistry();e.saved.Pairs=e.saved.Pairs.Take(5).ToArray();e.RecordSixPairSaved();});
   Refuses("an archive with combat refuses",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive();e.saved=e.ArchiveOfRegistry();e.saved.Combat=new object();e.RecordSixPairSaved();});
   Refuses("an archive missing a rider slot refuses",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive();e.saved=e.ArchiveOfRegistry();e.saved.Slots=e.saved.Slots.Skip(1).ToArray();e.RecordSixPairSaved();});
   Refuses("a continuation before all six pairs refuses",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive(10);e.BeginSixPairContinuation();});
   Refuses("a second save during continuation refuses",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive();e.CompleteSave();e.persistence.SnapshotCount=2;e.DriveContinuation();});
   Refuses("a native cast during continuation refuses",()=>{var e=Producer();e.BeginSixPairMounts();e.Drive();e.CompleteSave();e.controls.NativeCastRequestCount=7;e.DriveContinuation();});
   Refuses("an unknown six-pair stage refuses",()=>{var e=Producer();e.BeginSixPairMounts();e.stage=302;e.UpdateSixPairPersistence();});

   var c=ColdConsumer();c.ObserveSixPairCold();c.BeginSixPairContinuation();c.DriveContinuation();
   Check(c.sixMounted==6&&c.expectedOwners.Count==6&&Enumerable.Range(0,6).All(i=>c.sixOwnerIds[i]==c.world.Registry.FindOwner(c.actors[i*2]).Id&&c.sixGenerations[i]==4),"actual cold consumer adopts each restored owner and generation from the loaded archive");
   Check(c.restoration==null&&c.slotAsserts==1&&c.debtAsserts==1&&c.inputs.Count==0&&c.controls.NativeCastRequestCount==0,"actual cold consumer verifies slots and debt once, releases its observer and issues no native input");
   Check(c.cleanup&&c.movementRows.Count==6&&c.passed.SequenceEqual(new[]{"C7-persistence-cold-all-pair-identity-and-no-prepare","C7-persistence-six-native-continuation"})&&(string)c.sixObservation["producerRunId"]=="six-producer"&&(int)c.sixObservation["nativeSaveRequests"]==0,"actual cold consumer routes all six continuations and records its producer pins");
   var cold=new Dictionary<string,Action<Chunk7PersistenceScenario>>{
    {"a five-pair restored graph",e=>e.persistence.LoadedArchive.Pairs=e.persistence.LoadedArchive.Pairs.Take(5).ToArray()},
    {"a graph short of presentation",e=>e.persistence.ArchiveRestorePhase="all-native-actor-state-restored"},
    {"another campaign",e=>e.persistence.LoadedArchive.CampaignId="foreign"},
    {"another area",e=>e.persistence.LoadedArchive.AreaId="foreign"},
    {"restored combat",e=>e.persistence.LoadedArchive.Combat=new object()},
    {"eleven semantic restores",e=>e.persistence.SemanticRestoreCount=11},
    {"five presentation restores",e=>e.persistence.PresentationRestoreCount=5},
    {"eleven native post-loads",e=>e.persistence.ArchivePostLoadObservedCount=11},
    {"a replayed native cast",e=>e.controls.NativeCastRequestCount=1},
    {"a native refusal",e=>e.controls.NativeRefusalCount=1},
    {"a cold save",e=>e.persistence.SnapshotCount=1},
    {"native preparation",e=>e.restoration.Counts["prepare-before"]=1},
    {"a new round",e=>e.restoration.Counts["round-before"]=1},
    {"combat actor restoration",e=>e.restoration.Counts["restore-actor-before"]=1},
    {"eleven debt restores",e=>e.restoration.Counts["restore-debt-before"]=11},
    {"an incomplete observer",e=>e.restoration.Complete=false},
    {"an absent observer",e=>e.restoration=null},
    {"a live owner differing from the archive",e=>e.world.Registry.Items[3].Generation=5},
    {"an extra live owner",e=>e.world.Registry.Items.Add(new Owner{Rider=new Actor(),Mount=new Actor()})},
    {"an archive missing a slot",e=>e.persistence.LoadedArchive.Slots=e.persistence.LoadedArchive.Slots.Skip(1).ToArray()},
    {"changed inherited custody",e=>e.inherited.Reject=true},
    {"a reordered fixture",e=>{var a=e.inherited.Actors[0];e.inherited.Actors[0]=e.inherited.Actors[2];e.inherited.Actors[2]=a;}},
   };
   foreach(var fault in cold)Refuses("actual cold consumer refuses "+fault.Key,()=>{var e=ColdConsumer();fault.Value(e);e.ObserveSixPairCold();});

   Func<Chunk7PersistenceScenario> retained=()=>{var e=Producer();e.cleanup=true;e.pendingHostCustody=true;e.cleanupPendingReported=true;e.RetainedFailureArtifactSha256=new string('a',64);e.errors.Add("retained");e.results.Add(new Row{Status="FAIL"});return e;};
   var ok=retained();ok.RequireRetainedSixFailureScope();Check(true,"a committed hashed retained six-pair persistence failure admits the host exit");
   var scope=new Dictionary<string,Action<Chunk7PersistenceScenario>>{
    {"another persistence case",e=>e.request.OwnedPairs.SixPairPersistence=false},{"an uncaptured world",e=>e.captured=false},{"cleanup never begun",e=>e.cleanup=false},
    {"proven cleanup",e=>e.cleanupProven=true},{"no retained host custody",e=>e.pendingHostCustody=false},{"a disposed engine",e=>e.disposed=true},
    {"an unreported artifact",e=>e.cleanupPendingReported=false},{"an unhashed artifact",e=>e.RetainedFailureArtifactSha256=null},{"no recorded error",e=>e.errors.Clear()},
    {"no failed row",e=>e.results[0].Status="PASS"},{"a changed native world",e=>e.nativeScope.Reject=true},{"an absent native world",e=>e.nativeScope=null},
   };
   foreach(var fault in scope)Refuses("retained scope refuses "+fault.Key,()=>{var e=retained();fault.Value(e);e.RequireRetainedSixFailureScope();});
   Console.WriteLine("SIX-PAIR PERSISTENCE ORCHESTRATION PASS="+(checks-failures)+" FAIL="+failures);return failures==0?0:1;
  }
 }
}
