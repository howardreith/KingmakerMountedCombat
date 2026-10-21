using System;using System.Collections.Generic;using System.Linq;using KingmakerMountedCombat.Diagnostics;using Newtonsoft.Json.Linq;using UnitEntityData=Actor;
class NativeProcess{public bool IsEnded=true;}
class Command{public bool IsFinished=true;}
class UnitUseAbility:Command{public NativeProcess ExecutionProcess=new NativeProcess();}
class Commands{public Command[] Raw=new Command[4];public List<Command> Queue=new List<Command>();public bool Empty=>Raw.All(c=>c==null);}
class Actor{public string UniqueId=Guid.NewGuid().ToString();public bool IsInState=true,IsInGame=true,IsInCombat;public View View=new View();public Descriptor Descriptor=new Descriptor();public Commands Commands=new Commands();}
class View{public object AgentASP=new object();}class Descriptor{public ActorState State=new ActorState();}class ActorState{public bool IsConscious=true;}
class Game{public static Game Instance=new Game();public bool IsPaused;public object Player=new object();public Persistent State=new Persistent();}class Persistent{public Area LoadedAreaState=new Area();}class Area{public object MainState=new object();}
class LoadingProcess{public static LoadingProcess Instance=new LoadingProcess();public bool IsLoadingInProcess;public string[] QueuedNames=new string[0];}static class NativePersistenceIsolation{public static bool HasPendingWrites;}
class NativeScope{public bool Refuse;public void RequireCurrent(){if(Refuse)throw new Exception("world scope changed");}}
class NativeRuntime{public bool NoPreparedPairResidue=true;}class Relationship{public NativeRuntime Runtime=new NativeRuntime();}class Pair{public Relationship Relationship=new Relationship();public object Participant=new object();public bool CommandsPending;}
class Owner{public string Id="owner";}class Registry{public object[] Owners=new object[0];public int ActiveCount;public Owner FindActive(Actor a)=>new Owner();public Owner FindOwner(Actor a)=>null;}
class Unified{public object[] SnapshotParticipants=new object[2];public bool HasUnsettledPreparation;}
class World{public Pair[] Pairs=new[]{new Pair(),new Pair()};public int BoundaryCount=2,FailedConstructionCount;public Unified UnifiedTurn=new Unified();public Registry Registry=new Registry();public bool SerializationBusy;}
class Controls{public bool HasUnsettledRelationshipTransition;}
class FakeClock{public TimeSpan Elapsed=TimeSpan.Zero;}
class Request{public object F11AllocationProbe;public string RunId="synthetic";public string EvidenceRoot="synthetic";}
class F11NativeAllocationProbe{public static int Installs;public static F11NativeAllocationProbe Attach(object selection,string runId,string evidenceRoot){Installs++;return new F11NativeAllocationProbe();}}
class Sampler:IChunk7MatchedPoolWindow{public string Label;public bool IsReady;public bool Ready=>IsReady;public int Count=>0;public JObject Complete()=>new JObject{{"label",Label}};public void Dispose(){}}
class AdmissionAdapter{
 object measurement,quietWindow;int pendingMatchedIndex=-1,cycle=1;Chunk7MatchedPoolPins matchedPins;Chunk7MatchedPoolCollector matched;Request request=new Request();F11NativeAllocationProbe allocationProbe;
 Actor firstRider=new Actor(),firstMount=new Actor(),secondRider=new Actor(),secondMount=new Actor();World world=new World();NativeScope nativeScope=new NativeScope();Controls controls=new Controls();string firstOwner="owner";
 int acquires,step=34;double stepStarted;FakeClock clock=new FakeClock();Sampler sampler;
 AdmissionAdapter(){matched=new Chunk7MatchedPoolCollector(RequireMatchedPool,_=>new JObject(),(a,b)=>{},label=>{acquires++;return sampler=new Sampler{Label=label};});}
 Actor[] Actors()=>new[]{firstRider,firstMount,secondRider,secondMount};bool NativeCommandsSettled()=>true;bool PairCommandsSettled(Pair p)=>!p.CommandsPending;bool Mounted(Actor a,Actor b)=>true;void AssertBoth(){}static bool Idle(Actor a)=>a.IsInState&&a.IsInGame&&a.View?.AgentASP!=null&&a.Descriptor.State.IsConscious&&!a.IsInCombat&&a.Commands!=null&&a.Commands.Empty;
 static void Require(bool c,string e){if(!c)throw new InvalidOperationException(e);}
__METHODS__
 bool Tick(){__DEADLINE__;return PollMatchedPoolGroup(0);}
 static int passed,failed;static void Check(bool value,string name){if(value){passed++;Console.WriteLine("PASS "+name);}else{failed++;Console.WriteLine("FAIL "+name);}}
 static void Refuse(string name,Action<AdmissionAdapter> mutation){Game.Instance=new Game();LoadingProcess.Instance=new LoadingProcess();NativePersistenceIsolation.HasPendingWrites=false;var a=new AdmissionAdapter();a.BeginMatchedPool(0);mutation(a);bool refused=false;try{a.Tick();}catch{refused=true;}Check(refused&&a.acquires==0&&a.matchedPins!=null,name);a.matched.Dispose();a.matchedPins.Dispose();}
 internal static int Verify(){
  Game.Instance=new Game();var a=new AdmissionAdapter();var shell=new UnitUseAbility();a.firstRider.Commands.Raw[0]=shell;bool accepted=true;try{a.BeginMatchedPool(0);}catch{accepted=false;}
  Check(accepted&&a.acquires==0&&a.matchedPins!=null,"terminal slot enters pending admission without acquiring observer");
  bool waiting=false;try{waiting=!a.Tick()&&!a.Tick();}catch{}Check(waiting&&a.acquires==0&&ReferenceEquals(a.firstRider.Commands.Raw[0],shell),"finished ended native slot waits across polls without mutation or repeated acquisition");
  a.firstRider.Commands.Raw[0]=null;bool started=false;try{started=!a.Tick();}catch{}Check(started&&a.acquires==1,"ordinary native slot removal allows exactly one observer acquisition");
  bool stable=false;try{stable=!a.Tick();}catch{}Check(stable&&a.acquires==1,"warmup polls cannot reacquire observer");
  a.firstRider.Commands.Queue.Add(new Command());bool refused=false;try{a.Tick();}catch{refused=true;}Check(refused&&a.acquires==1&&!a.matched.Released,"later queued native command refuses and retains sampler for cleanup");a.matched.Dispose();a.matchedPins.Dispose();
  a=new AdmissionAdapter();a.BeginMatchedPool(0);
  for(int i=0;i<Chunk7MatchedPoolCollector.WindowsPerState;i++){
   Check(!a.Tick()&&a.acquires==i+1,"same-state repeated window acquires one sampler at index "+i);
   a.sampler.IsReady=true;var complete=a.Tick();
   Check(complete==(i==Chunk7MatchedPoolCollector.WindowsPerState-1)&&a.matched.CompletedCount==i+1&&a.pendingMatchedIndex==(complete?-1:i+1),
    "same-state repeated window advances only after exact completion at index "+i);
  }
  Check(a.matched.CompletedCount==3&&!a.matched.Complete,"first resident state cannot claim the complete twelve-window sequence");
  a.matched.Dispose();a.matchedPins.Dispose();
  Refuse("changed world scope refuses before sampling",x=>x.nativeScope.Refuse=true);
  Refuse("same-area replaced Player refuses before sampling",x=>Game.Instance.Player=new object());
  Refuse("changed actor descriptor refuses before sampling",x=>x.firstRider.Descriptor=new Descriptor());
  Refuse("changed actor view refuses before sampling",x=>x.firstRider.View=new View());
  Refuse("unexpected retained pair refuses before sampling",x=>x.world.Registry.Owners=new object[1]);
  Refuse("active native command is not a terminal-slot wait",x=>x.firstRider.Commands.Raw[0]=new Command{IsFinished=false});
  Refuse("live execution process is not a terminal-slot wait",x=>x.firstRider.Commands.Raw[0]=new UnitUseAbility{ExecutionProcess=new NativeProcess{IsEnded=false}});
  Refuse("queued native action refuses even with empty slots",x=>x.firstRider.Commands.Queue.Add(new Command()));
  Refuse("native preparation refuses pending admission",x=>x.world.UnifiedTurn.HasUnsettledPreparation=true);
  Refuse("native worker refuses pending admission",x=>NativePersistenceIsolation.HasPendingWrites=true);
  Refuse("unchanged thirty-second stage deadline bounds pending admission",x=>x.clock.Elapsed=TimeSpan.FromSeconds(31));
  Refuse("unchanged total deadline bounds pending admission",x=>{x.clock.Elapsed=TimeSpan.FromSeconds(301);x.stepStarted=300;});
  Console.WriteLine("MATCHED ADMISSION PASS="+passed+" FAIL="+failed+"; actual production admission/actor/pin/owner/deadline methods, external native boundaries only");return failed==0?0:1;
 }
}
public static class MatchedPoolAdmissionTests{public static int Main(){try{return AdmissionAdapter.Verify();}catch(Exception e){Console.Error.WriteLine(e);return 1;}}}
