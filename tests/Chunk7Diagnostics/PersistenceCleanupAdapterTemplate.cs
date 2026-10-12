using System;using System.Collections.Generic;using System.Linq;using System.IO;using System.Text;using System.Security.Cryptography;using KingmakerMountedCombat.Diagnostics;
using UnitEntityData=Actor;using MountedPairServices=Pair;
enum RelationshipState{Unmounted,Mounted}enum CleanupTrigger{Exception}
class JObject:Dictionary<string,object>{public new dynamic this[string key]{get{object value;return TryGetValue(key,out value)?value:null;}set{base[key]=value;}}public static JObject FromObject(object x){return new JObject();}public string ToString(Formatting f){return "{}";}}
class JArray{public JArray(object x){}}
class Lease:IDisposable{public bool Fail;public int Releases;public void Dispose(){Releases++;if(Fail)throw new Exception("lease pending");}}
class Clock{public TimeSpan Elapsed=TimeSpan.Zero;public bool IsRunning;public void Start(){IsRunning=true;}}
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
class Controls{public bool HasUnsettledRelationshipTransition,SerializationSuspended;public bool ShellPending;public bool HasUnsettledShellFor(Relationship r){return ShellPending;}public object DescribeNativeShellLifecycle(){return null;}}
class Settings{public bool EnableUnsafeMovementExperiment,EnablePairedActivation;}
class Game{public static Game Instance=new Game();public bool IsPaused;public Destroyer EntityDestroyer=new Destroyer();public State State=new State();public Player Player=new Player();public TimeController TimeController=new TimeController();}
class TimeController{public TimeSpan GameTime=TimeSpan.Zero;}class LoadingProcess{public static LoadingProcess Instance=new LoadingProcess();public bool IsLoadingInProcess;public string[] QueuedNames=new string[0];}static class NativePersistenceIsolation{public static bool HasPendingWrites;public static void DisableNativeSlotRotation(){}}static class Time{public static int frameCount;}
class Destroyer{public void Tick(){}}class State{public List<Actor> Units=new List<Actor>();}class Player{public List<Actor> ControllableCharacters=new List<Actor>();}
class SelectionManager{public static SelectionManager Instance=new SelectionManager();public Actor[] SelectedUnits=new Actor[0];public void MultiSelect(IEnumerable<View> v,bool x){SelectedUnits=v.Select(y=>y.Actor).ToArray();}}
enum Formatting{None}class Main{}
static class RuntimeLoaderInventory{
__WRITER__
}
enum OwnedWorkerTeardownVerdict{Refused,Settled}
class Persistence{public bool AreaTransitionPending,HasActiveSaveScope,HasSaveAdmissionFence,WorkerPending;public OwnedWorkerTeardownVerdict DrainForTeardown(int wait){return WorkerPending?OwnedWorkerTeardownVerdict.Refused:OwnedWorkerTeardownVerdict.Settled;}}
class Observer:Lease{public object Capture(){return null;}}class Checkpoint:Observer{public bool CleanupComplete;public void BeginCleanup(){}public bool UpdateCleanup(){return CleanupComplete;}}class SavedDescriptor{public string FolderName;}
class PersistenceAdapter{
 bool cleanup,captured=true,completed,disposed,cleanupProven,pendingHostCustody,cleanupActions,originalPaired,originalMovement,originalPause,Cold;double stageStarted;Clock clock=new Clock();Lease mode=new Lease(),ummLease=new Lease();Observer restoration;Checkpoint checkpoint;Persistence persistence=new Persistence();
 Command[] issued=new Command[]{new UnitUseAbility()};World world=new World();Controls controls=new Controls();Actor secondRider=new Actor(),secondMount=new Actor();Actor[] originalSelection=new Actor[0];object acquiredHorse=new object(),acquiredRank=new object();RuntimeRequest request=new RuntimeRequest();SavedDescriptor savedDescriptor;Settings settings=new Settings();JObject evidence=new JObject();List<string> errors=new List<string>();List<RuntimeSubscenarioResult> results=new List<RuntimeSubscenarioResult>();HashSet<string> cleanupFaults=new HashSet<string>();const string EvidenceFileName="fixture",EvidenceKind="fixture";int observerDisposals;
 PersistenceAdapter(){request.EvidenceRoot=Path.Combine(Program.EvidenceRoot,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(request.EvidenceRoot);secondRider.Descriptor.Pet=secondMount;Game.Instance.State.Units.Add(secondMount);var horse=acquiredHorse;secondRider.Descriptor.Progression.Features.OnRemove=f=>{if(ReferenceEquals(f,horse)){secondMount.IsInState=false;Game.Instance.State.Units.Remove(secondMount);secondRider.Descriptor.Pet=null;}};}
 void RestoreSelectedNativeFormat(){}void EndAreaObservation(){observerDisposals++;}static void Require(bool c,string e){if(!c)throw new InvalidOperationException(e);}object Capture(string x){return null;}static string Hash(string p){return "hash";}
__METHODS__
 static int passed,failed;static void Check(bool c,string n){if(c)passed++;else failed++;Console.WriteLine((c?"PASS ":"FAIL ")+n);}
 static PersistenceAdapter Ready(){var e=new PersistenceAdapter();var c=(UnitUseAbility)e.issued[0];c.IsFinished=true;c.ExecutionProcess.IsEnded=true;return e;}
 internal static int Verify(){var prep=Ready();prep.world.UnifiedTurn.HasUnsettledPreparation=true;Check(!prep.PerformCleanup()&&prep.acquiredHorse!=null&&prep.acquiredRank!=null&&prep.secondRider.Descriptor.Progression.Features.Removed.Count==0,"unfinished native preparation retains exact Horse and rank facts");
 var transition=Ready();transition.controls.HasUnsettledRelationshipTransition=true;Check(!transition.PerformCleanup()&&transition.acquiredHorse!=null&&transition.acquiredRank!=null,"unfinished relationship transition retains both progression facts");
 var serializer=Ready();serializer.controls.SerializationSuspended=true;Check(!serializer.PerformCleanup()&&serializer.acquiredHorse!=null,"native control serialization fence retains acquired companion");
 var busy=Ready();busy.world.SerializationBusy=true;Game.Instance.IsPaused=true;busy.BeginCleanup();busy.PollCleanup();Check(busy.acquiredHorse!=null&&Game.Instance.IsPaused&&busy.observerDisposals==0&&!busy.completed,"world serialization fences observers, unpause, and entity cleanup");
 var worker=Ready();worker.persistence.WorkerPending=true;Game.Instance.IsPaused=true;worker.BeginCleanup();worker.PollCleanup();Check(worker.acquiredHorse!=null&&Game.Instance.IsPaused&&worker.observerDisposals==0,"native worker retains fixture before any observer cleanup");
 var live=new PersistenceAdapter();Check(!live.PerformCleanup()&&((UnitUseAbility)live.issued[0]).IsFinished&&live.acquiredHorse!=null,"finished issued shell with live native process retains facts");
 var pair=Ready();pair.world.Pairs[0].Relationship.Reject=true;Check(!pair.PerformCleanup()&&pair.world.Pairs[0].Relationship.PairOwner!=null&&pair.world.Pairs[1].Relationship.PairOwner==null&&pair.acquiredHorse!=null,"failed pair retains facts while healthy neighbour releases");
 var fact=Ready();fact.secondRider.Descriptor.Progression.Features.Failing=fact.acquiredHorse;Check(!fact.PerformCleanup()&&fact.acquiredHorse!=null&&fact.acquiredRank==null,"failed Horse removal keeps exact fact while independent rank settles");fact.secondRider.Descriptor.Progression.Features.Failing=null;Check(fact.PerformCleanup()&&fact.acquiredHorse==null&&fact.acquiredRank==null&&fact.errors.Count==1,"retry settles facts without discarding failed evidence");
 prep.world.UnifiedTurn.HasUnsettledPreparation=false;Check(prep.PerformCleanup()&&prep.acquiredHorse==null&&prep.acquiredRank==null,"settled preparation permits bounded owned fact cleanup");
 var timeout=new PersistenceAdapter();timeout.BeginCleanup();timeout.clock.Elapsed=TimeSpan.FromSeconds(46);Check(!timeout.DrainForHost()&&File.Exists(Path.Combine(timeout.request.EvidenceRoot,"chunk7-persistence-cleanup-pending.json"))&&!File.Exists(Path.Combine(timeout.request.EvidenceRoot,EvidenceFileName)),"actual immutable writer reserves final artifact while cleanup remains retained");Check(!timeout.DrainForHost()&&Directory.GetFiles(timeout.request.EvidenceRoot).Length==1,"repeated pending drain does not rewrite immutable pending artifact");
 timeout.Fail("C7-persistence-cleanup","second distinct cleanup fault");timeout.Fail("C7-persistence-cleanup","second distinct cleanup fault");((UnitUseAbility)timeout.issued[0]).ExecutionProcess.IsEnded=true;Check(timeout.DrainForHost()&&Directory.GetFiles(timeout.request.EvidenceRoot).Length==2&&File.Exists(Path.Combine(timeout.request.EvidenceRoot,EvidenceFileName)),"actual filesystem accepts one pending artifact then one final settled artifact");var row=timeout.results.Single(x=>x.Name=="C7-persistence-cleanup");Check(row.Status=="FAIL"&&row.AssertionFailCount==2&&row.AssertionPassCount==1&&row.Errors.Count==2&&RuntimeSubscenarioResult.IsMissionScenario(row.Name),"timeout and later cleanup pass remain one valid named FAIL row with distinct errors");
 var duplicateRefused=false;try{RuntimeLoaderInventory.WriteArtifact(timeout.request,EvidenceFileName,EvidenceKind,"PASS",timeout.evidence,new string[0]);}catch(InvalidOperationException){duplicateRefused=true;}Check(duplicateRefused&&Directory.GetFiles(timeout.request.EvidenceRoot).Length==2,"unchanged actual artifact writer rejects a duplicate final leaf");
 Console.WriteLine("CHUNK7 PERSISTENCE CLEANUP ADAPTER PASS="+passed+" FAIL="+failed+"; actual source method bodies with external native stubs");return failed==0?0:1;}
}
static class Program{internal static string EvidenceRoot;static int Main(string[] args){EvidenceRoot=args[0];Directory.CreateDirectory(EvidenceRoot);try{return PersistenceAdapter.Verify();}catch(Exception e){Console.Error.WriteLine(e);return 1;}}}
