// Compile the unchanged six-pair partial against native boundary facades.
// This certifies orchestration/refusal/waiting, never engine behavior or speed.
using System;using System.Collections.Generic;using System.Linq;using Newtonsoft.Json.Linq;
namespace UnityEngine { struct Vector3 { public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); } static class Time { public static int frameCount; } }
namespace Kingmaker.EntitySystem.Entities {
 class UnitEntityData { public string UniqueId=Guid.NewGuid().ToString();public object Descriptor=new object(),View=new object();public UnityEngine.Vector3 Position;public Commands Commands=new Commands();public CombatState CombatState=new CombatState(); }
 class Commands { public bool Empty=true; } class CombatState { public int AttackOfOpportunityCount=1,AttackOfOpportunityPerRound=1; }
}
namespace Kingmaker { class Game { public static Game Instance=new Game();public Player Player=new Player();public State State=new State();public bool IsPaused; } class Player { public bool IsInCombat; } class State { public Area LoadedAreaState=new Area(); } class Area { public object MainState=new object(); } }
namespace Kingmaker.EntitySystem.Persistence { class LoadingProcess { public static LoadingProcess Instance=new LoadingProcess();public bool IsLoadingInProcess;public string[] QueuedNames=new string[0]; } }
namespace TurnBased.Controllers { static class CombatController { public static bool IsInTurnBasedCombat()=>false; } }
namespace KingmakerMountedCombat.Integration { static class GameMountedRelationshipService { public static int NativePairCapacity=6; } static class NativePersistenceIsolation { public static bool HasPendingWrites; } }
namespace KingmakerMountedCombat.Diagnostics {
 using Actor=Kingmaker.EntitySystem.Entities.UnitEntityData;using UnityEngine;
 class SixPairAttachmentObservation:IDisposable { public SixPairAttachmentObservation(IEnumerable<string> ids, object logger){} public void Dispose(){} }
 class Request { public Selection OwnedPairs=new Selection(); } class Selection { public bool IsSixPairStress=true; }
 static class Chunk7OwnedPairsSelection { public const string SixPairSourceRun="qualified-six-source"; }
 class Owner { public Guid Id=Guid.NewGuid();public Actor Rider,Mount; }
 class Registry { public List<Owner> Items=new List<Owner>();public int ActiveCount=>Items.Count;public Owner[] Owners=>Items.ToArray();public Owner FindOwner(Actor a)=>Items.SingleOrDefault(x=>ReferenceEquals(x.Rider,a)||ReferenceEquals(x.Mount,a));public Owner FindActive(Actor a)=>FindOwner(a); }
 class Runtime { public bool NoPreparedPairResidue=true; } class Relationship { public Runtime Runtime=new Runtime(); } class Pair { public object Participant=new object();public Relationship Relationship=new Relationship(); }
 class Unified { public bool HasUnsettledPreparation;public object[] SnapshotParticipants;public int TrackedAllocationActorCount; }
 class World { public Registry Registry=new Registry();public Pair[] Pairs=Enumerable.Range(0,6).Select(i=>new Pair()).ToArray();public Unified UnifiedTurn=new Unified();public int BoundaryCount=>Pairs.Length;public int FailedConstructionCount;public bool SerializationBusy;public World(){UnifiedTurn.SnapshotParticipants=Pairs.Select(p=>p.Participant).ToArray();} }
 class Inherited { public Actor[] Actors=Enumerable.Range(0,12).Select(i=>new Actor()).ToArray();public bool Reject;public int Calls,DuringOpenWindow;
  public void RequirePreserved(){Calls++;var open=MountedRuntimeMeasurement.Active;if(open!=null&&!open.Ready)DuringOpenWindow++;if(Reject)throw new InvalidOperationException("inherited native custody changed");} }
 class Shell { public int Pending; } class Controls { public bool HasUnsettledRelationshipTransition;public object MountAbility=new object(),DismountAbility=new object();public int NativeCastRequestCount,NativeRefusalCount;public bool HasExactRelationshipProcessBinding(Shell s)=>s.Pending>0; }
 class Persistence { public int SnapshotCount,SemanticRestoreCount,PresentationRestoreCount; }
 class Clock { public TimeSpan Elapsed; }
 class MountedRuntimeMeasurement:IDisposable { readonly int start=Time.frameCount;public bool Ready=>Time.frameCount-start>=150;public static int Starts,Releases;public static MountedRuntimeMeasurement Active;
  public MountedRuntimeMeasurement(string label,World world){Starts++;Active=this;}public JObject Complete()=>new JObject();public void Dispose(){Releases++;if(ReferenceEquals(Active,this))Active=null;} }
 internal sealed partial class Chunk7WorldScenarioEngine {
  readonly Request request=new Request();readonly Inherited inherited=new Inherited();readonly World world=new World();readonly Controls controls=new Controls();readonly Persistence persistence=new Persistence();readonly Clock clock=new Clock();
  readonly JObject evidence=new JObject();readonly JArray inputs=new JArray(),groundInputs=new JArray(),snapshots=new JArray();readonly List<string> passed=new List<string>();
  readonly object logger=new object();
  MountedRuntimeMeasurement measurement;object quietWindow;int step;double stepStarted;Shell shell;bool observedExactProcess,cleanup;int pendingFrames,movementFrames;Actor pendingRider,pendingMount;bool pendingMounting,holdInput,holdRetirement,holdMovement;int finishedInputs,moves;
  Actor[] Actors()=>inherited.Actors;
  static void Require(bool condition,string error){if(!condition)throw new InvalidOperationException(error);}
  void Next(int value){step=value;stepStarted=clock.Elapsed.TotalSeconds;}
  static bool Idle(Actor a)=>a.Commands.Empty;static bool MatchedActorSettled(Actor a)=>Idle(a);
  bool Mounted(Actor a,Actor b){var o=world.Registry.FindOwner(a);return o!=null&&ReferenceEquals(o.Mount,b);}
  void Input(Actor a,Actor b,object ability,string label,Actor next){Require(shell==null,"native input replay before settlement");controls.NativeCastRequestCount++;inputs.Add(new JObject{{"label",label}});shell=new Shell{Pending=1};pendingRider=a;pendingMount=b;pendingMounting=ReferenceEquals(ability,controls.MountAbility);pendingFrames=3;}
  bool InputSettled()=>shell!=null&&pendingFrames<=0;
  void FinishInput(){Require(InputSettled(),"unfinished native shell");finishedInputs++;shell=null;}
  void Move(Actor a,Actor b,Vector3 goal,Actor next,string label){Require(movementFrames==0,"native movement replay");groundInputs.Add(new JObject{{"label",label}});moves++;movementFrames=3;}
  bool MovementSettled()=>movementFrames==0;
  // The read-only combat-entry capture partial needs the native census; its facade keeps the guard's throw and records the call.
  int clearanceChecks;bool clearanceBreached;void RequireSixPairHostileClearance(){clearanceChecks++;Require(!clearanceBreached,"Six-pair stress is uncovered on this placement: 1 native unit(s) within vision clearance of the pairs or their waypoints; no input was issued.");}
  int combatEntryRefusals;void RefuseSixPairCombatEntry(){combatEntryRefusals++;throw new InvalidOperationException(Chunk7SixPairCombatEntry.Message(Chunk7SixPairCombatEntry.Unclassified));}
  void BeginCleanup(){cleanup=true;ReleaseSixSamplePins();}
  void Pass(string name){passed.Add(name);}
  int siblingCaptures,siblingChecks;void CaptureSixSiblingOrder(){siblingCaptures++;}void RequireSixSiblingOrder(){Require(siblingCaptures==1,"sibling order verified before capture");siblingChecks++;}
  bool NativeCommandsSettled()=>pendingFrames<=0&&movementFrames==0;
  static bool PairCommandsSettled(Pair pair)=>true;
  JObject CaptureWorld(string label)=>new JObject{{"label",label},{"resources",new JObject{{"actors",new JArray(Actors().Select(a=>new JObject{{"id",a.UniqueId}}))}}}};
  static void CompareMatchedPool(JObject a,JObject b){}
  void TickNative(){Time.frameCount++;clock.Elapsed=TimeSpan.FromSeconds(Time.frameCount/100.0);
   if(pendingFrames>0&&!holdInput&&--pendingFrames==0){shell.Pending=0;if(pendingMounting)world.Registry.Items.Add(new Owner{Rider=pendingRider,Mount=pendingMount});else if(!holdRetirement)world.Registry.Items.RemoveAll(x=>ReferenceEquals(x.Rider,pendingRider));}
   if(movementFrames>0&&!holdMovement)movementFrames--;
  }
  static Chunk7WorldScenarioEngine Create(){GameReset();return new Chunk7WorldScenarioEngine();}
  static void GameReset(){Kingmaker.Game.Instance=new Kingmaker.Game();Time.frameCount=0;MountedRuntimeMeasurement.Starts=MountedRuntimeMeasurement.Releases=0;MountedRuntimeMeasurement.Active=null;Integration.GameMountedRelationshipService.NativePairCapacity=6;Integration.NativePersistenceIsolation.HasPendingWrites=false;}
  void Drive(int count=20000){for(int i=0;i<count&&!cleanup;i++){TickNative();UpdateSixPairStress();}}
  static int checks,failures;static void Check(bool ok,string label){checks++;Console.WriteLine((ok?"PASS ":"FAIL ")+label);if(!ok)failures++;}
  static void Refuse(string label,Action<Chunk7WorldScenarioEngine> mutate,bool afterBegin=false){var e=Create();bool refused=false;try{if(afterBegin)e.BeginSixPairStress();mutate(e);if(afterBegin){e.TickNative();e.UpdateSixPairStress();}else e.BeginSixPairStress();}catch(InvalidOperationException){refused=true;}Check(refused,label);}
  public static int Main(){
   var e=Create();e.BeginSixPairStress();e.Drive();
   Check(e.cleanup&&e.finishedInputs==36&&e.controls.NativeCastRequestCount==36,"actual partial completes 36 settled selected-ability inputs without replay");
   Check(e.moves==6&&e.groundInputs.Count==6,"actual partial routes all six native ground calls");
   Check(e.sixCycles.Count==3&&e.world.Registry.ActiveCount==0,"actual partial completes all three six-pair native lifecycle cycles");
   Check(e.sixSamples.Count==15&&MountedRuntimeMeasurement.Starts==15&&MountedRuntimeMeasurement.Releases==15,"actual partial opens/closes fifteen sampler boundaries");
   Check(e.sixPins==null&&e.sixIdentities==null&&(bool)e.sixObservation["identityScopeReleased"],"actual partial releases exact native pin scope before cleanup");
   Check(e.passed.Count==4&&e.passed.Distinct().Count()==4,"actual partial emits four distinct selected result rows once");
   Check(e.siblingCaptures==1&&e.siblingChecks==3,"actual partial captures native sibling order once and requires it after each settled cycle");
   Check(e.inherited.Calls>0&&e.inherited.DuringOpenWindow==0&&(string)e.sixObservation["contract"]=="cold-qualified-six-pair-lifecycle-v2"&&
    e.sixObservation["measuredFramesCarryFullCustody"].Value<bool>()==false,"actual partial verifies full inherited custody outside open windows only and declares it");
   var b=Create();b.BeginSixPairStress();for(int i=0;i<20000&&(MountedRuntimeMeasurement.Active==null||MountedRuntimeMeasurement.Active.Ready);i++){b.TickNative();b.UpdateSixPairStress();}
   for(int i=0;i<5;i++){b.TickNative();b.UpdateSixPairStress();}var openBefore=MountedRuntimeMeasurement.Active!=null&&!MountedRuntimeMeasurement.Active.Ready;b.inherited.Reject=true;
   bool caught=false,caughtWhileOpen=false,caughtAtReady=false;try{for(int i=0;i<1000;i++){var open=MountedRuntimeMeasurement.Active!=null&&!MountedRuntimeMeasurement.Active.Ready;b.TickNative();try{b.UpdateSixPairStress();}catch(InvalidOperationException){caught=true;caughtWhileOpen=open&&MountedRuntimeMeasurement.Active!=null&&!MountedRuntimeMeasurement.Active.Ready;caughtAtReady=MountedRuntimeMeasurement.Active!=null&&MountedRuntimeMeasurement.Active.Ready;throw;}}}catch(InvalidOperationException){}
   Check(openBefore&&caught&&!caughtWhileOpen&&caughtAtReady&&b.sixSamples.Count==0,"changed inherited custody inside an open window is refused at that window's ready boundary before its row is recorded");
   var w=Create();w.BeginSixPairStress();w.holdInput=true;w.Drive(25);Check(w.inputs.Count==1&&w.finishedInputs==0,"pending native input never replays or finishes early");w.holdInput=false;w.Drive();Check(w.cleanup&&w.finishedInputs==36,"native input continuation completes once after release");
   var r=Create();r.BeginSixPairStress();r.holdRetirement=true;for(int i=0;i<300&&!r.inputs.Any(x=>(string)x["label"]=="six-cycle-1-dismount-0");i++){r.TickNative();r.UpdateSixPairStress();}r.Drive(25);Check(r.step==1004&&r.inputs.Count==7&&r.finishedInputs==6,"native dismount shell terminal waits for exact owner retirement");r.holdRetirement=false;r.world.Registry.Items.RemoveAll(x=>ReferenceEquals(x.Rider,r.pendingRider));r.Drive();Check(r.cleanup&&r.finishedInputs==36,"owner retirement continuation completes without second dismount");
   var m=Create();m.BeginSixPairStress();m.holdMovement=true;m.Drive(100);Check(m.moves==1&&m.inputs.Count==6&&!m.cleanup,"native movement wait never replays ground input");m.holdMovement=false;m.Drive();Check(m.cleanup&&m.moves==6,"ground continuation preserves exact six input bound");
   Refuse("wrong native capacity refuses before input",x=>Integration.GameMountedRelationshipService.NativePairCapacity=2);
   Refuse("eleven field roles refuse before input",x=>x.inherited.Actors=x.inherited.Actors.Take(11).ToArray());
   Check(e.clearanceChecks==1,"actual partial checks native vision clearance once, before the first input");
   var h=Create();h.clearanceBreached=true;bool uncovered=false;try{h.BeginSixPairStress();}catch(InvalidOperationException error){uncovered=error.Message.StartsWith("Six-pair stress is uncovered");}
   Check(uncovered&&h.inputs.Count==0&&h.sixObservation==null&&h.step==0,"a native unit inside vision clearance refuses as uncovered before any input or observation");
   Refuse("native combat refuses without issuing input",x=>Kingmaker.Game.Instance.Player.IsInCombat=true,true);
   var c=Create();c.BeginSixPairStress();Kingmaker.Game.Instance.Player.IsInCombat=true;string combatMessage=null;try{c.TickNative();c.UpdateSixPairStress();}catch(InvalidOperationException error){combatMessage=error.Message;}
   Check(c.combatEntryRefusals==1&&combatMessage=="Six-pair exploration entered unexpected native combat."&&c.inputs.Count==0&&!c.cleanup,"native combat routes once through the combat-entry refusal with the unchanged guard message");
   Refuse("changed inherited custody refuses without issuing input",x=>x.inherited.Reject=true,true);
   Refuse("unrelated active ownership refuses before first input",x=>x.world.Registry.Items.Add(new Owner{Rider=new Actor(),Mount=new Actor()}),true);
   var p=Create();p.BeginSixPairStress();for(int i=0;i<1000&&p.sixPins==null;i++){p.TickNative();p.UpdateSixPairStress();}p.inherited.Actors[11].View=new object();bool replaced=false;try{p.TickNative();p.UpdateSixPairStress();}catch(InvalidOperationException){replaced=true;}Check(replaced&&!p.cleanup,"replaced sixth-Horse native view refuses retained sampling");
   Console.WriteLine("SIX-PAIR ORCHESTRATION PASS="+(checks-failures)+" FAIL="+failures);return failures==0?0:1;
  }
 }
}
