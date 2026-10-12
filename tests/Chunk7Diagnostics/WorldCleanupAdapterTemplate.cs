using System;using System.Collections.Generic;using System.Linq;
using UnitEntityData=Actor;using MountedPairServices=Pair;
enum RelationshipState{Unmounted,Mounted}enum CleanupTrigger{Exception}
class JObject:Dictionary<string,object>{public new object this[string key]{get{object value;return TryGetValue(key,out value)?value:null;}set{base[key]=value;}}public static JObject FromObject(object x){return new JObject();}}
class JArray{public JArray(object x){}}
class Lease:IDisposable{public bool Fail;public int Releases;public void Dispose(){Releases++;if(Fail)throw new Exception("lease pending");}}
class Clock{public TimeSpan Elapsed=TimeSpan.Zero;}
class Process{public bool IsEnded;}
class Command{public bool IsFinished;public int Interruptions;public void Interrupt(){Interruptions++;IsFinished=true;}}
class UnitUseAbility:Command{public Process ExecutionProcess=new Process();}
class Actor{public Descriptor Descriptor=new Descriptor();public bool IsInState=true;public View View;public string UniqueId="actor";public Actor(){View=new View{Actor=this};}}
class View{public Actor Actor;}
class Descriptor{public Actor Pet;public Reference Master=new Reference();public Progression Progression=new Progression();}
class Reference{public Actor Value;}
class Progression{public Features Features=new Features();}
class Features{public List<object> Removed=new List<object>();public object Failing;public Action<object> OnRemove;public void RemoveFact(object f){if(ReferenceEquals(Failing,f))throw new Exception("fact pending");Removed.Add(f);OnRemove?.Invoke(f);}}
class Runtime{public bool NoPreparedPairResidue;}
class Result{public bool Succeeded;}
class Relationship{public Runtime Runtime=new Runtime();public RelationshipState State=RelationshipState.Mounted;public object PairOwner=new object();public bool Reject;public int Dismounts;public Result Dismount(CleanupTrigger x){Dismounts++;if(Reject)return new Result();State=RelationshipState.Unmounted;Runtime.NoPreparedPairResidue=true;PairOwner=null;return new Result{Succeeded=true};}}
class Combat{public bool HasActiveCommand,HasActiveGroundMovement,HasChargeOwnership;public int Cancels;public void Cancel(string x){Cancels++;}public bool TryConfigurePairedActivation(bool x){return true;}}
class Pair{public Relationship Relationship=new Relationship();public Combat Combat=new Combat();}
class Unified{public bool HasUnsettledPreparation;}class Registry{public object[] Owners=new object[0];public int ActiveCount;}
class World{public Pair[] Pairs=new[]{new Pair(),new Pair()};public Pair Primary=>Pairs[0];public bool SerializationBusy;public Unified UnifiedTurn=new Unified();public Registry Registry=new Registry();}
class Controls{public bool HasUnsettledRelationshipTransition;public bool ShellPending;public bool HasUnsettledShellFor(Relationship r){return ShellPending;}public object DescribeNativeShellLifecycle(){return null;}}
class Settings{public bool EnableUnsafeMovementExperiment,EnablePairedActivation;}
class Game{public static Game Instance=new Game();public bool IsPaused;public Destroyer EntityDestroyer=new Destroyer();public State State=new State();public Player Player=new Player();public TimeController TimeController=new TimeController();}
class TimeController{public TimeSpan GameTime=TimeSpan.Zero;}class LoadingProcess{public static LoadingProcess Instance=new LoadingProcess();public bool IsLoadingInProcess;public string[] QueuedNames=new string[0];}static class NativePersistenceIsolation{public static bool HasPendingWrites;}static class Time{public static int frameCount;}
class Destroyer{public void Tick(){}}class State{public List<Actor> Units=new List<Actor>();}class Player{public List<Actor> ControllableCharacters=new List<Actor>();}
class SelectionManager{public static SelectionManager Instance=new SelectionManager();public Actor[] SelectedUnits=new Actor[0];public void MultiSelect(IEnumerable<View> v,bool x){SelectedUnits=v.Select(y=>y.Actor).ToArray();}}
static class RuntimeLoaderInventory{public static int Writes;public static void WriteArtifact(object r,string f,string k,string s,object e,object x){Writes++;}}
class WorldAdapter{
 bool cleanupStarted,captured=true,completed,disposed,cleanupProven,cleanupPendingReported,originalPaired,originalMovement,originalPause;double stepStarted;Clock clock=new Clock();Lease measurement,mode=new Lease(),ummLease=new Lease(),screenshots;
 Command[] issuedCommands=new Command[]{new UnitUseAbility()};World world=new World();Controls controls=new Controls();Actor secondRider=new Actor(),secondMount=new Actor(),firstRider=new Actor(),firstMount=new Actor();Actor[] originalSelection=new Actor[0];object horseFeature=new object(),rankFeature=new object(),request;
 Settings settings=new Settings();JObject evidence=new JObject();List<string> errors=new List<string>();Dictionary<string,int> cleanupAttempts=new Dictionary<string,int>();const string EvidenceFileName="fixture",EvidenceKind="fixture";
 public WorldAdapter(){secondRider.Descriptor.Pet=secondMount;Game.Instance.State.Units.Add(secondMount);firstRider.Descriptor.Pet=firstMount;firstMount.Descriptor.Master.Value=firstRider;var horse=horseFeature;secondRider.Descriptor.Progression.Features.OnRemove=f=>{if(ReferenceEquals(f,horse)){secondMount.IsInState=false;Game.Instance.State.Units.Remove(secondMount);secondRider.Descriptor.Pet=null;}};}
 static int Id(object x){return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(x);}static void Require(bool c,string e){if(!c)throw new Exception(e);}void Fail(string n,string e){errors.Add(e);}void Pass(string n){}object CaptureWorld(string x){return null;}IEnumerable<Actor> PartyActors(){return new Actor[0];}object CaptureRig(Actor a){return null;}
__METHODS__
 static int passed;static void Check(bool condition,string name){if(!condition)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
 public static void Verify(){var e=new WorldAdapter();var features=e.secondRider.Descriptor.Progression.Features;var command=(UnitUseAbility)e.issuedCommands[0];
 Game.Instance.IsPaused=true;e.BeginCleanup();Check(features.Removed.Count==0&&command.Interruptions==0&&Game.Instance.IsPaused,"BeginCleanup preserves pause, facts, and live commands");
 e.world.SerializationBusy=true;Check(!e.DrainForHost()&&command.Interruptions==0&&features.Removed.Count==0&&Game.Instance.IsPaused,"native serialization worker fences unpause and command/fact cleanup");e.world.SerializationBusy=false;
 LoadingProcess.Instance.IsLoadingInProcess=true;Check(!e.DrainForHost()&&command.Interruptions==0&&Game.Instance.IsPaused,"native load fences unpause and fixture mutation");LoadingProcess.Instance.IsLoadingInProcess=false;
 LoadingProcess.Instance.QueuedNames=new[]{"queued exact native load"};Check(!e.DrainForHost()&&command.Interruptions==0&&Game.Instance.IsPaused,"queued-only native load retains paused fixture");LoadingProcess.Instance.QueuedNames=new string[0];
 NativePersistenceIsolation.HasPendingWrites=true;Check(!e.DrainForHost()&&command.Interruptions==0&&Game.Instance.IsPaused,"native write queue retains pause and fixture ownership");NativePersistenceIsolation.HasPendingWrites=false;
 Check(!e.DrainForHost()&&command.IsFinished&&!command.ExecutionProcess.IsEnded&&features.Removed.Count==0&&!Game.Instance.IsPaused,"finished shell with live ExecutionProcess retains Horse fact");
 e.clock.Elapsed=TimeSpan.FromSeconds(16);Check(!e.DrainForHost()&&!e.completed&&!e.disposed&&e.errors.Count==1,"cleanup deadline records failure but retains engine");var writes=RuntimeLoaderInventory.Writes;e.DrainForHost();Check(RuntimeLoaderInventory.Writes==writes,"retained custody does not rewrite failure artifact each frame");
 bool refused=false;try{e.Dispose();}catch(InvalidOperationException){refused=true;}Check(refused&&!e.disposed&&features.Removed.Count==0,"actual Dispose refuses asynchronous custody");
 command.ExecutionProcess.IsEnded=true;e.world.Pairs[0].Relationship.State=RelationshipState.Mounted;e.world.Pairs[0].Relationship.PairOwner=new object();e.world.Pairs[0].Relationship.Runtime.NoPreparedPairResidue=false;e.world.Pairs[0].Relationship.Reject=true;e.world.Pairs[1].Relationship.State=RelationshipState.Mounted;e.world.Pairs[1].Relationship.PairOwner=new object();e.world.Pairs[1].Relationship.Runtime.NoPreparedPairResidue=false;
 Check(!e.DrainForHost()&&e.world.Pairs[0].Relationship.PairOwner!=null&&e.world.Pairs[1].Relationship.PairOwner==null&&features.Removed.Count==0,"failed owner retains itself while independent pair releases");
 e.world.Pairs[0].Relationship.Reject=false;features.Failing=e.horseFeature;Check(!e.DrainForHost()&&e.horseFeature!=null&&e.rankFeature==null&&features.Removed.Count==1,"failed Horse fact removal retains exact fact and releases independent rank");
 features.Failing=null;var lease=e.mode;lease.Fail=true;Check(!e.DrainForHost()&&features.Removed.Count==2&&e.mode!=null&&!e.completed,"failed native mode lease remains owned after entity cleanup");
 lease.Fail=false;Check(e.DrainForHost()&&e.completed&&e.cleanupProven&&e.mode==null&&e.ummLease==null,"retry proves physical cleanup without discarding earlier failures");
 Check(e.errors.Count>=3,"physical settlement preserves original failed evidence");e.Dispose();Check(e.disposed,"actual Dispose releases only proven completed custody");var count=features.Removed.Count;e.Dispose();Check(features.Removed.Count==count,"repeated Dispose does not replay fact removal");
 Console.WriteLine("CHUNK7 WORLD CLEANUP ADAPTER PASS="+passed+" FAIL=0; actual source methods with external native boundary stubs only");}
}
static class Program{static int Main(){try{WorldAdapter.Verify();return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}}
