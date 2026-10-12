using System;using System.Collections.Generic;using System.Linq;
using Actor=Kingmaker.EntitySystem.Entities.UnitEntityData;using UnitUseAbility=Kingmaker.UnitLogic.Commands.UnitUseAbility;
enum RelationshipState{Unmounted,Mounted}enum CleanupTrigger{Exception}
class JObject:Dictionary<string,object>{public new object this[string key]{get{object v;return TryGetValue(key,out v)?v:null;}set{base[key]=value;}}public static JObject FromObject(object x){return new JObject();}}
class JArray:List<object>{public JArray(){}public JArray(object v){}}
class Clock{public TimeSpan Elapsed;}
class Lease:IDisposable{public bool Fail;public int Releases;public Action<object> AfterNativeRound;public object Capture(){return new JObject();}public virtual void Dispose(){Releases++;if(Fail)throw new Exception("observer retained");}}
class Target:Lease{public int Destroyed;public bool DestroyAndVerify(){Destroyed++;return true;}}
class Ai{public bool LastRestoreVerified;public void ReassertAfterNativeReset(Actor[] a){}public void Restore(Actor[] a){LastRestoreVerified=true;}}
class Process{public bool IsEnded;}class Command{public bool IsFinished;public int Interruptions;public void Interrupt(){Interruptions++;IsFinished=true;}}
namespace Kingmaker.UnitLogic.Commands{class UnitUseAbility:Command{public Process ExecutionProcess=new Process();}}
namespace Kingmaker.EntitySystem.Entities{class UnitEntityData{public bool IsInState=true,IsInCombat=true;public int Leaves,Destroyed;public string UniqueId=Guid.NewGuid().ToString();public Group Group;public Cmds Commands=new Cmds();public void Destroy(){Destroyed++;IsInState=false;Game.Instance.State.Units.Remove(this);Group?.Remove(this);}public Descriptor Descriptor=new Descriptor();public Stats Stats=new Stats();public View View;public UnitEntityData(){View=new View{Actor=this};}public void LeaveCombat(){Leaves++;IsInCombat=false;}}}
class Stats{public Stat Initiative=new Stat();}class Stat{public int BaseValue;}class View{public Actor Actor;}
class Descriptor{public Actor Pet;public Progression Progression=new Progression();}class Progression{public Features Features=new Features();}
class Features{public object Failing;public List<object> Removed=new List<object>();public Action<object> OnRemove;public void RemoveFact(object x){if(ReferenceEquals(Failing,x))throw new Exception("fact retained");Removed.Add(x);OnRemove?.Invoke(x);}}
class Runtime{public bool NoPreparedPairResidue;}class Result{public bool Succeeded;}
class Relationship{public RelationshipState State=RelationshipState.Mounted;public Runtime Runtime=new Runtime();public object PairOwner=new object();public bool Reject;public event Action<CleanupTrigger> Dismounting;public Result Dismount(CleanupTrigger t){Dismounting?.Invoke(t);if(Reject)return new Result();State=RelationshipState.Unmounted;Runtime.NoPreparedPairResidue=true;PairOwner=null;return new Result{Succeeded=true};}}
class Combat{public bool HasActiveCommand,HasActiveGroundMovement,HasChargeOwnership;public int Cancels;public void Cancel(string s){Cancels++;}public bool TryConfigurePairedActivation(bool b){return true;}}
class Pair{public Relationship Relationship=new Relationship();public Combat Combat=new Combat();}class Unified{public bool HasUnsettledPreparation;}
class World{public Exception[] Visit(Action<Pair> callback){var errors=new List<Exception>();foreach(var pair in Pairs)try{callback(pair);}catch(Exception e){errors.Add(e);}return errors.ToArray();}public bool SerializationBusy;public Pair[] Pairs=new[]{new Pair(),new Pair()};public Pair Primary=>Pairs[0];public Unified UnifiedTurn=new Unified();}
class Controls{public bool HasUnsettledRelationshipTransition,Shell;public bool HasUnsettledShellFor(Relationship r){return Shell;}public object DescribeNativeShellLifecycle(){return null;}}
class Settings{public bool EnableUnsafeMovementExperiment,EnableMountedCharge;}class Request{public string Scenario="tb";}
class LoadingProcess{public static LoadingProcess Instance=new LoadingProcess();public bool IsLoadingInProcess;public string[] QueuedNames=new string[0];}static class NativePersistenceIsolation{public static bool HasPendingWrites;}
class Game{public static Game Instance=new Game();public bool IsPaused;public Destroyer EntityDestroyer=new Destroyer();public Player Player=new Player();public Controller TurnBasedCombatController=new Controller();public GroupController UnitGroupsController=new GroupController();public State State=new State();}
class Player{public bool IsInCombat;}class Controller{public bool Initialized;}class State{public List<Actor> Units=new List<Actor>();}class Destroyer{public void Tick(){}}
class SelectionManager{public static SelectionManager Instance=new SelectionManager();public void MultiSelect(IEnumerable<View> x,bool b){}}
static class RuntimeLoaderInventory{public static HashSet<string> Leaves=new HashSet<string>();public static int Writes;public static bool FailNext;public static string LastStatus;
 public static void WriteArtifact(object r,string file,string kind,string status,object evidence,object errors){if(FailNext){FailNext=false;throw new Exception("artifact IO failed");}if(!Leaves.Add(file))throw new Exception("immutable artifact already exists");Writes++;LastStatus=status;}}

class Cmds{public void InterruptAll(){}}class Group:List<Actor>{public string Id="native-group";public bool Disposed;public bool Empty(){return Count==0;}public void Dispose(){Disposed=true;}}class GroupController{public List<Group> Groups=new List<Group>();}
class Source{public bool CheckpointCleanupComplete,Ready;public int Starts,Disposes;public void BeginCheckpointCleanup(){Starts++;}public bool UpdateCheckpointCleanup(){CheckpointCleanupComplete=Ready;return Ready;}public void Dispose(){Disposes++;}}
class CheckpointAdapter{
 World world=new World();Controls controls=new Controls();Source source;Clock clock=new Clock();bool cleanup,cleanupComplete,disposed,ownsObservers=true;double begunSeconds;Lease trace=new Lease(),completion=new Lease(),rounds=new Lease();JObject evidence=new JObject();JArray cleanupErrors=new JArray();Command[] issued=new Command[]{new UnitUseAbility()};Actor[] actors=new[]{new Actor(),new Actor(),new Actor(),new Actor()};Actor target=new Actor();Group targetGroup=new Group();string targetId,targetGroupId;
 public CheckpointAdapter(){targetGroup.Add(target);target.Group=targetGroup;targetId=target.UniqueId;targetGroupId=targetGroup.Id;Game.Instance.State.Units.Add(target);Game.Instance.UnitGroupsController.Groups.Add(targetGroup);}
__METHODS__
 static int passed;static void Check(bool c,string n){if(!c)throw new Exception(n);passed++;Console.WriteLine("PASS "+n);}static bool Refuses(Action a){try{a();return false;}catch{return true;}}
 static void Reset(){Game.Instance=new Game();LoadingProcess.Instance=new LoadingProcess();NativePersistenceIsolation.HasPendingWrites=false;}
 public static void Verify(){Reset();var e=new CheckpointAdapter();var command=(UnitUseAbility)e.issued[0];var observer=e.trace;Game.Instance.IsPaused=true;e.BeginCleanup();
 Check(Game.Instance.IsPaused&&observer.Releases==0&&e.target.Destroyed==0,"BeginCleanup retains paused native observer and actor custody");
 e.world.SerializationBusy=true;Check(!e.UpdateCleanup()&&Game.Instance.IsPaused&&observer.Releases==0,"checkpoint serialization fences cleanup");e.world.SerializationBusy=false;
 LoadingProcess.Instance.IsLoadingInProcess=true;Check(!e.UpdateCleanup()&&Game.Instance.IsPaused&&observer.Releases==0,"checkpoint active native load fences cleanup");LoadingProcess.Instance.IsLoadingInProcess=false;
 LoadingProcess.Instance.QueuedNames=new[]{"queued"};Check(!e.UpdateCleanup()&&Game.Instance.IsPaused&&observer.Releases==0,"checkpoint queued load fences cleanup");LoadingProcess.Instance.QueuedNames=new string[0];
 NativePersistenceIsolation.HasPendingWrites=true;Check(!e.UpdateCleanup()&&Game.Instance.IsPaused&&observer.Releases==0,"checkpoint native write queue fences cleanup");NativePersistenceIsolation.HasPendingWrites=false;
 var failed=e.completion;failed.Fail=true;Check(Refuses(()=>e.UpdateCleanup())&&e.completion==failed&&e.trace==null&&e.rounds==null&&e.target.Destroyed==0,"checkpoint observer failure retains exact lease while independent observers release");failed.Fail=false;
 Check(!e.UpdateCleanup()&&command.IsFinished&&!command.ExecutionProcess.IsEnded&&e.target.Destroyed==0,"checkpoint live execution process fences target cleanup");command.ExecutionProcess.IsEnded=true;
 e.world.UnifiedTurn.HasUnsettledPreparation=true;Check(!e.UpdateCleanup()&&e.target.Destroyed==0&&e.actors.All(a=>a.Leaves==0),"checkpoint preparation fences target and actor teardown");e.world.UnifiedTurn.HasUnsettledPreparation=false;
 e.controls.HasUnsettledRelationshipTransition=true;Check(!e.UpdateCleanup()&&e.target.Destroyed==0&&e.actors.All(a=>a.Leaves==0),"checkpoint relationship transition fences native actor teardown");e.controls.HasUnsettledRelationshipTransition=false;
 Check(e.UpdateCleanup()&&e.cleanupComplete&&e.target.Destroyed==1&&e.targetGroup.Disposed&&e.actors.All(a=>a.Leaves==1),"checkpoint legitimate settlement removes only captured target group and exits captured actors");e.Dispose();Check(e.disposed,"checkpoint Dispose completes only after physical settlement");
 Reset();e=new CheckpointAdapter();e.source=new Source();e.ownsObservers=false;Game.Instance.IsPaused=true;e.BeginCleanup();e.world.SerializationBusy=true;Check(!e.UpdateCleanup()&&Game.Instance.IsPaused&&e.source.Disposes==0,"delegated source cleanup remains fenced before native worker settles");e.world.SerializationBusy=false;Check(!e.UpdateCleanup()&&!e.cleanupComplete,"delegated source retains unfinished child cleanup");e.source.Ready=true;Check(e.UpdateCleanup()&&e.source.Disposes==1&&e.trace==null&&e.completion==null&&e.rounds==null,"delegated source releases borrowed aliases only after proven cleanup");
 Console.WriteLine("CHUNK7 CHECKPOINT CLEANUP ADAPTER PASS="+passed+" FAIL=0; actual methods with external boundary stubs only");}
}
static class Program{static int Main(){try{CheckpointAdapter.Verify();return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}}
