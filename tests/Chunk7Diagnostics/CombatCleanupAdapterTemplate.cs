using System;using System.Collections.Generic;using System.Linq;using KingmakerMountedCombat.Diagnostics;
using Actor=Kingmaker.EntitySystem.Entities.UnitEntityData;using UnitUseAbility=Kingmaker.UnitLogic.Commands.UnitUseAbility;
enum RelationshipState{Unmounted,Mounted}enum CleanupTrigger{Exception}
class JObject:Dictionary<string,object>{public new object this[string key]{get{object v;return TryGetValue(key,out v)?v:null;}set{base[key]=value;}}public static JObject FromObject(object x){return new JObject();}}
class JArray{public JArray(object v){}}
class Clock{public TimeSpan Elapsed;}
class Lease:IDisposable{public bool Fail;public int Releases;public Action<object> AfterNativeRound;public object Capture(){return new JObject();}public virtual void Dispose(){Releases++;if(Fail)throw new Exception("observer retained");}}
class Target:Lease{public int Destroyed;public bool DestroyAndVerify(){Destroyed++;return true;}}
class Ai{public bool LastRestoreVerified;public void ReassertAfterNativeReset(Actor[] a){}public void Restore(Actor[] a){LastRestoreVerified=true;}}
class Process{public bool IsEnded;}class Command{public bool IsFinished;public int Interruptions;public void Interrupt(){Interruptions++;IsFinished=true;}}
namespace Kingmaker.UnitLogic.Commands{class UnitUseAbility:Command{public Process ExecutionProcess=new Process();}}
namespace Kingmaker.EntitySystem.Entities{class UnitEntityData{public string UniqueId=Guid.NewGuid().ToString(),CharacterName="fixture";public bool IsInState=true,IsInCombat=true;public int Leaves;public Descriptor Descriptor=new Descriptor();public Stats Stats=new Stats();public View View;public UnitEntityData(){View=new View{Actor=this};}public void LeaveCombat(){Leaves++;IsInCombat=false;}}}
class Stats{public Stat Initiative=new Stat();}class Stat{public int BaseValue;}class View{public Actor Actor;}
class Descriptor{public Actor Pet;public Progression Progression=new Progression();}class Progression{public Features Features=new Features();}
class Features{public object Failing;public List<object> Removed=new List<object>();public Action<object> OnRemove;public void RemoveFact(object x){if(ReferenceEquals(Failing,x))throw new Exception("fact retained");Removed.Add(x);OnRemove?.Invoke(x);}}
class Runtime{public bool NoPreparedPairResidue;}class Result{public bool Succeeded;}
class Relationship{public RelationshipState State=RelationshipState.Mounted;public Runtime Runtime=new Runtime();public object PairOwner=new object();public bool Reject;public event Action<CleanupTrigger> Dismounting;public Result Dismount(CleanupTrigger t){Dismounting?.Invoke(t);if(Reject)return new Result();State=RelationshipState.Unmounted;Runtime.NoPreparedPairResidue=true;PairOwner=null;return new Result{Succeeded=true};}}
class Combat{public bool HasActiveCommand,HasActiveGroundMovement,HasChargeOwnership;public int Cancels;public void Cancel(string s){Cancels++;}public bool TryConfigurePairedActivation(bool b){return true;}}
class Pair{public Relationship Relationship=new Relationship();public Combat Combat=new Combat();}class Unified{public bool HasUnsettledPreparation;}
class World{public bool SerializationBusy;public Pair[] Pairs=new[]{new Pair(),new Pair()};public Pair Primary=>Pairs[0];public Unified UnifiedTurn=new Unified();}
class Controls{public bool HasUnsettledRelationshipTransition,Shell;public bool HasUnsettledShellFor(Relationship r){return Shell;}public object DescribeNativeShellLifecycle(){return null;}}
class Settings{public bool EnableUnsafeMovementExperiment,EnableMountedCharge;}class Request{public string Scenario="tb";}
class LoadingProcess{public static LoadingProcess Instance=new LoadingProcess();public bool IsLoadingInProcess;public string[] QueuedNames=new string[0];}static class NativePersistenceIsolation{public static bool HasPendingWrites;}
class Game{public NativeArea CurrentlyLoadedArea=new NativeArea();public static Game Instance=new Game();public bool IsPaused;public Destroyer EntityDestroyer=new Destroyer();public Player Player=new Player();public Controller TurnBasedCombatController=new Controller();public State State=new State();}
class Player{public string GameId="campaign";public MainReference MainCharacter=new MainReference{Value=new Actor()};public bool IsInCombat;}class MainReference{public Actor Value;}class Controller{public bool Initialized;}class State{public NativeAreaState LoadedAreaState=new NativeAreaState();public List<Actor> Units=new List<Actor>();}class Destroyer{public void Tick(){}}
class SelectionManager{public static SelectionManager Instance=new SelectionManager();public void MultiSelect(IEnumerable<View> x,bool b){}}
static class RuntimeLoaderInventory{public static HashSet<string> Leaves=new HashSet<string>();public static int Writes;public static bool FailNext;public static string LastStatus;
 public static void WriteArtifact(object r,string file,string kind,string status,object evidence,object errors){if(FailNext){FailNext=false;throw new Exception("artifact IO failed");}if(!Leaves.Add(file))throw new Exception("immutable artifact already exists");Writes++;LastStatus=status;}}
class RuntimeSubscenarioResult{public string Name,Status;public int AssertionPassCount,AssertionFailCount;public string[] Errors;}

class RuntimeRequest{public Fixture Fixture=new Fixture();public Working SelectedChunk7Fixture=>Fixture.Working;public object OwnedPairs;public List<string> Validate(){throw new Exception("unexpected owned DTO in external world stub");}}class Fixture{public Working Working=new Working();}class Working{public string GameId="campaign",GameName="fixture",Area="area";}
class NativeArea{public string AssetGuidThreadSafe="area";}class NativeAreaState{public object MainState=new object();}
namespace Kingmaker{class Game:global::Game{}}
class InheritedFixtureBoundary{public Actor[] Party;public bool Bound=true,Reject,Released;public int Captures,Releases;public void RequirePreserved(){if(Reject||Released)throw new Exception("inherited custody retained");}public object Capture(string phase){RequirePreserved();Captures++;return new JObject();}public void Release(){if(!Released){Released=true;Releases++;}}}
class CombatAdapter{
 InheritedFixtureBoundary inherited;
 Chunk7NativeWorldScope nativeScope;string SelectedScenario=>request.Scenario;
 bool cleanupStarted,captured=true,completed,disposed,cleanupServicesSettled,checkpointOnly,pauseBefore=true,pairedBefore,movementBefore,chargeBefore;bool RealtimeScenario;int step=10,actionStage=2;double stepStarted;Clock clock=new Clock();
 Lease trace=new Lease(),completion=new Lease(),rounds=new Lease(),realtimeEffects=new Lease(),lifeDelivery=new Lease(),lifeEffects=new Lease(),mode=new Lease(),umm=new Lease();Target targetService=new Target();Ai firstPairAi=new Ai();World world=new World();Controls controls=new Controls();Pair b;Settings settings=new Settings();Request request=new Request();JObject evidence=new JObject(),nativeLife;
 Action<CleanupTrigger> cleanupFault;List<string> errors=new List<string>();List<RuntimeSubscenarioResult> results=new List<RuntimeSubscenarioResult>();IEnumerable<string> rows=>results.Select(r=>r.Name);Command[] issuedCommands=new Command[]{new UnitUseAbility()};Actor ar=new Actor(),am=new Actor(),br=new Actor(),bm=new Actor();Actor[] actors,originalParty,selectionBefore=new Actor[0];object horseFeature=new object(),rankFeature=new object();Dictionary<Actor,int> initiativeBefore=new Dictionary<Actor,int>();const string NativeLifeCleanup="life",EvidenceFileName="chunk7-combat-evidence.json",EvidenceKind="chunk7-combat-evidence";
 public CombatAdapter(){b=world.Pairs[1];actors=new[]{ar,am,br,bm};br.Descriptor.Pet=bm;Game.Instance.State.Units.Add(bm);var owned=horseFeature;br.Descriptor.Progression.Features.OnRemove=f=>{if(ReferenceEquals(f,owned)){bm.IsInState=false;br.Descriptor.Pet=null;Game.Instance.State.Units.Remove(bm);}};foreach(var a in actors){initiativeBefore.Add(a,a.Stats.Initiative.BaseValue);if(!Game.Instance.State.Units.Contains(a))Game.Instance.State.Units.Add(a);}nativeScope=new Chunk7NativeWorldScope(new RuntimeRequest(),actors);}
 static void Require(bool c,string e){if(!c)throw new InvalidOperationException(e);}
 // X1 death proper: the death case captures its observed cleanup postconditions once, after settling.
 bool LifeDeath;int deathCleanupCaptures;JObject CaptureDeathCleanup(){deathCleanupCaptures++;return new JObject();}
 static void VerifyDeathCleanupCapture(){Reset();var e=new CombatAdapter{LifeDeath=true};e.nativeLife=new JObject();((UnitUseAbility)e.issuedCommands[0]).ExecutionProcess.IsEnded=true;e.BeginCleanup();e.world.UnifiedTurn.HasUnsettledPreparation=true;Check(!e.DrainForHost()&&e.deathCleanupCaptures==0&&e.nativeLife["deathCleanup"]==null,"death cleanup postconditions are not captured before cleanup settles");e.world.UnifiedTurn.HasUnsettledPreparation=false;e.DrainForHost();Check(e.deathCleanupCaptures==1&&e.nativeLife["deathCleanup"]!=null,"death cleanup postconditions are captured once after cleanup settles");
  Reset();var l=new CombatAdapter();l.nativeLife=new JObject();((UnitUseAbility)l.issuedCommands[0]).ExecutionProcess.IsEnded=true;l.BeginCleanup();l.DrainForHost();Check(l.deathCleanupCaptures==0&&l.nativeLife["deathCleanup"]==null,"the incapacitation case captures no death postconditions");}
 // No registered-disable diagnostic is attached in this fixture, so its release has nothing to undo.
 void EndRegisteredDisableObservation(){}
 static void VerifyInheritedCleanup(){Reset();var e=new CombatAdapter();((UnitUseAbility)e.issuedCommands[0]).ExecutionProcess.IsEnded=true;var guard=new InheritedFixtureBoundary{Party=e.actors,Reject=true};e.inherited=guard;e.BeginCleanup();Check(!e.DrainForHost()&&!e.completed&&guard.Releases==0&&e.br.Descriptor.Progression.Features.Removed.Count==0,"inherited refusal retains ordinary pets/facts and registry");guard.Reject=false;Check(e.DrainForHost()&&e.completed&&guard.Captures==1&&guard.Releases==1&&e.br.Descriptor.Progression.Features.Removed.Count==0,"inherited retry snapshots before registry release without removing native facts");e.Dispose();Check(guard.Releases==1&&e.br.Descriptor.Progression.Features.Removed.Count==0,"inherited repeated disposal never replays fact cleanup");}
__METHODS__

 static int scopeFailures;
 static void ScopeCheck(bool value,string name){if(value){passed++;Console.WriteLine("PASS "+name);}else{scopeFailures++;Console.WriteLine("FAIL "+name);}}
 static void ScopeRefuses(string name,Action<CombatAdapter> mutate){Game.Instance=new Game();LoadingProcess.Instance=new LoadingProcess();NativePersistenceIsolation.HasPendingWrites=false;var e=new CombatAdapter();Game.Instance.IsPaused=true;e.BeginCleanup();mutate(e);var before=e.br.Descriptor.Progression.Features.Removed.Count;bool drained=false;try{drained=e.DrainForHost();}catch{}ScopeCheck(!drained&&!e.completed&&!e.disposed&&Game.Instance.IsPaused&&e.issuedCommands.All(c=>c.Interruptions==0)&&e.world.Pairs.All(p=>p.Combat.Cancels==0)&&e.br.Descriptor.Progression.Features.Removed.Count==before,"captured scope refuses "+name+" before native mutation");}
 static void VerifyWorldScope(){
 ScopeRefuses("new Game",e=>Game.Instance=new Game{IsPaused=true});
 ScopeRefuses("new Player",e=>Game.Instance.Player=new Player());
 ScopeRefuses("changed campaign",e=>Game.Instance.Player.GameId="foreign");
 ScopeRefuses("changed main character",e=>Game.Instance.Player.MainCharacter.Value.CharacterName="foreign");
 ScopeRefuses("changed area GUID",e=>Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe="foreign");
 ScopeRefuses("same GUID new area object",e=>Game.Instance.CurrentlyLoadedArea=new NativeArea());
 ScopeRefuses("new PersistentState",e=>Game.Instance.State=new State());
 ScopeRefuses("same GUID new loaded area state",e=>Game.Instance.State.LoadedAreaState=new NativeAreaState());
 ScopeRefuses("same GUID new main scene state",e=>Game.Instance.State.LoadedAreaState.MainState=new object());
 ScopeRefuses("same actor ID replacement object",e=>{var old=e.ar;Game.Instance.State.Units.Remove(old);Game.Instance.State.Units.Add(new Actor{UniqueId=old.UniqueId});});
 ScopeRefuses("changed live view",e=>e.ar.View=new View{Actor=e.ar});
 ScopeRefuses("changed descriptor",e=>e.ar.Descriptor=new Descriptor());
 Game.Instance=new Game();var keep=new CombatAdapter();var removed=keep.ar;removed.IsInState=false;removed.View=null;Game.Instance.State.Units.Remove(removed);bool ok=true;try{keep.nativeScope.RequireCurrent();}catch{ok=false;}ScopeCheck(ok,"legitimate removed actor absence remains admissible");
 var pet=new Actor{View=null};Game.Instance.State.Units.Add(pet);keep.nativeScope.Track(pet);pet.View=new View{Actor=pet};ok=true;try{keep.nativeScope.RequireCurrent();}catch{ok=false;}ScopeCheck(ok,"newly materialized first native pet view is pinned");pet.View=new View{Actor=pet};ok=false;try{keep.nativeScope.RequireCurrent();}catch{ok=true;}ScopeCheck(ok,"subsequent new view cannot rebind exact captured actor");
 if(scopeFailures!=0)throw new Exception("WORLD SCOPE FAIL="+scopeFailures);
 }

 static int passed;static void Check(bool value,string name){if(!value)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}static bool Refuses(Action a){try{a();return false;}catch{return true;}}
 static void Reset(){Game.Instance=new Game();LoadingProcess.Instance=new LoadingProcess();NativePersistenceIsolation.HasPendingWrites=false;RuntimeLoaderInventory.Leaves.Clear();RuntimeLoaderInventory.Writes=0;RuntimeLoaderInventory.FailNext=false;RuntimeLoaderInventory.LastStatus=null;}
 public static void Verify(){Reset();var e=new CombatAdapter();var command=(UnitUseAbility)e.issuedCommands[0];var features=e.br.Descriptor.Progression.Features;var target=e.targetService;var observer=e.trace;Game.Instance.IsPaused=true;
 e.BeginCleanup();Check(Game.Instance.IsPaused&&command.Interruptions==0&&observer.Releases==0&&target.Destroyed==0,"BeginCleanup preserves paused native/observer/actor custody");
 e.world.SerializationBusy=true;Check(!e.DrainForHost()&&Game.Instance.IsPaused&&command.Interruptions==0&&observer.Releases==0,"world serialization fences pause and observer mutation");e.world.SerializationBusy=false;
 LoadingProcess.Instance.IsLoadingInProcess=true;Check(!e.DrainForHost()&&Game.Instance.IsPaused&&target.Destroyed==0,"active native load fences cleanup");LoadingProcess.Instance.IsLoadingInProcess=false;
 LoadingProcess.Instance.QueuedNames=new[]{"queued"};Check(!e.DrainForHost()&&Game.Instance.IsPaused&&observer.Releases==0,"queued-only native load fences unpause");LoadingProcess.Instance.QueuedNames=new string[0];
 NativePersistenceIsolation.HasPendingWrites=true;Check(!e.DrainForHost()&&Game.Instance.IsPaused&&features.Removed.Count==0,"native write queue fences fixture mutation");NativePersistenceIsolation.HasPendingWrites=false;
 var retained=e.lifeDelivery;retained.Fail=true;Check(!e.DrainForHost()&&e.lifeDelivery==retained&&observer.Releases==1&&e.trace==null&&target.Destroyed==0,"observer failure retains exact failed observer while independent observers release");retained.Fail=false;
 Check(!e.DrainForHost()&&command.IsFinished&&!command.ExecutionProcess.IsEnded&&target.Destroyed==0&&features.Removed.Count==0,"finished command retains unfinished native process before target or fact teardown");
 e.clock.Elapsed=TimeSpan.FromSeconds(46);Check(!e.DrainForHost()&&!e.completed&&RuntimeLoaderInventory.Leaves.SetEquals(new[]{"chunk7-combat-cleanup-pending.json"}),"bounded pending cleanup reserves immutable final artifact");var writes=RuntimeLoaderInventory.Writes;e.DrainForHost();Check(RuntimeLoaderInventory.Writes==writes,"pending cleanup evidence writes once during retries");
 Check(Refuses(e.Dispose)&&!e.disposed&&features.Removed.Count==0,"Dispose refuses retained native process custody");
 command.ExecutionProcess.IsEnded=true;e.world.UnifiedTurn.HasUnsettledPreparation=true;Check(!e.DrainForHost()&&target.Destroyed==0&&e.actors.All(a=>a.Leaves==0)&&features.Removed.Count==0,"preparation fence precedes target actor and fact teardown");e.world.UnifiedTurn.HasUnsettledPreparation=false;
 e.controls.HasUnsettledRelationshipTransition=true;Check(!e.DrainForHost()&&target.Destroyed==0&&e.actors.All(a=>a.Leaves==0),"relationship transition fence precedes actor teardown");e.controls.HasUnsettledRelationshipTransition=false;
 e.world.Pairs[0].Relationship.Reject=true;Check(!e.DrainForHost()&&target.Destroyed==1&&e.actors.All(a=>a.Leaves==1)&&e.world.Pairs[0].Relationship.PairOwner!=null&&e.world.Pairs[1].Relationship.PairOwner==null&&features.Removed.Count==0,"failed pair retains fact custody while healthy neighbor independently releases");e.world.Pairs[0].Relationship.Reject=false;
 features.Failing=e.horseFeature;Check(!e.DrainForHost()&&e.horseFeature!=null&&e.rankFeature==null&&features.Removed.Count==1,"failed Horse fact retained while independent owned rank settles");features.Failing=null;
 RuntimeLoaderInventory.FailNext=true;Check(Refuses(()=>e.DrainForHost())&&!e.completed&&e.cleanupProven&&!RuntimeLoaderInventory.Leaves.Contains(EvidenceFileName),"artifact failure cannot mark final completion");
 Check(e.DrainForHost()&&e.completed&&RuntimeLoaderInventory.Leaves.SetEquals(new[]{"chunk7-combat-cleanup-pending.json",EvidenceFileName})&&RuntimeLoaderInventory.LastStatus=="FAIL","settled retry writes one final artifact preserving earlier failure");Check(e.results.Count(r=>r.Name=="C7-combat-cleanup")==1&&e.results.Single(r=>r.Name=="C7-combat-cleanup").Status=="FAIL"&&e.results.Single(r=>r.Name=="C7-combat-cleanup").Errors.Distinct().Count()==e.results.Single(r=>r.Name=="C7-combat-cleanup").Errors.Length,"cleanup retry merges one named row retaining distinct failures");e.Dispose();Check(e.disposed,"settled physical and evidence custody releases once");writes=RuntimeLoaderInventory.Writes;e.Dispose();Check(RuntimeLoaderInventory.Writes==writes,"repeated disposal cannot replay immutable evidence");
 Reset();e=new CombatAdapter();Check(!e.DrainForHost()&&e.rows.Contains("C7-combat-interrupted"),"host abort before own terminal step records truthful interruption");
 Reset();e=new CombatAdapter{checkpointOnly=true};Check(!e.DrainForHost()&&!e.rows.Contains("C7-combat-interrupted"),"intentional persistence checkpoint delegates cleanup without false interruption");e.clock.Elapsed=TimeSpan.FromSeconds(46);e.DrainForHost();Check(RuntimeLoaderInventory.Leaves.Count==0,"delegated checkpoint does not write foreign combat pending artifact");
 VerifyWorldScope();VerifyInheritedCleanup();VerifyDeathCleanupCapture();
 Console.WriteLine("CHUNK7 COMBAT CLEANUP ADAPTER PASS="+passed+" FAIL=0; extracted actual methods with external boundary stubs only");}
}
static class Program{static int Main(){try{CombatAdapter.Verify();return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}}
