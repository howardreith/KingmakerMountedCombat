using System;using System.Collections.Generic;using System.Linq;using KingmakerMountedCombat.Diagnostics;
namespace Kingmaker.EntitySystem.Persistence{sealed class LoadingProcess{internal static LoadingProcess Instance=new LoadingProcess();internal bool IsLoadingInProcess;internal List<string> QueuedNames=new List<string>();}}
enum OwnedWorkerTeardownVerdict{Refused,Settled}
static class NativePersistenceIsolation{internal static bool HasPendingWrites;}
sealed class Clock{internal TimeSpan Elapsed;}
sealed class Persistence{internal bool AreaTransitionPending,HasActiveSaveScope,HasSaveAdmissionFence,WorkerPending;internal OwnedWorkerTeardownVerdict DrainForTeardown(int wait){return WorkerPending?OwnedWorkerTeardownVerdict.Refused:OwnedWorkerTeardownVerdict.Settled;}}
sealed class Engine{internal bool Ready,ThrowDispose;internal int Polls,Disposals;internal IReadOnlyList<string> Results=new string[0],Errors=new string[0];internal bool DrainForHost(){Polls++;return Ready;}internal void Dispose(){Disposals++;if(ThrowDispose)throw new InvalidOperationException("retained exact engine lease");}}
sealed class HostOwnedEnginesAdapter{
 readonly Chunk7HostCompletionGate chunk7Completion=new Chunk7HostCompletionGate();readonly Clock runtimeClock=new Clock();readonly Persistence persistence=new Persistence();Engine worldPersistenceEngine,worldCombatEngine,worldEngine,ordinaryEngine;
 Engine assistedColdInventoryEngine,assistedFixtureEngine,acquiredManualEngine,manualDerivedEngine,manualSeedEngine,bootstrapInventoryEngine,nativeBootstrapEngine,smallCalibrationEngine,progressionEngine,ordinaryFieldEngine;
 static int passed;IReadOnlyList<string> subscenarioResults;int worldCleanup;List<string> faults=new List<string>();
 void RecordChunk7CleanupFailure(string e){faults.Add(e);}void CollectEngineErrors(IReadOnlyList<string> e,string p){}bool SettleChunk7World(){worldCleanup++;return true;}
__METHOD__
 internal static void Verify(){var h=new HostOwnedEnginesAdapter();var exact=new Engine();h.worldEngine=exact;h.chunk7Completion.Request(0);h.persistence.WorkerPending=true;
 Check(!h.TrySettleChunk7Custody()&&exact.Polls==0&&exact.Disposals==0&&ReferenceEquals(h.worldEngine,exact)&&h.worldCleanup==0,"actual host adapter retains world engine before worker settles");
 h.persistence.WorkerPending=false;Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.IsLoadingInProcess=true;Check(!h.TrySettleChunk7Custody()&&exact.Polls==0,"actual host adapter retains observers during native load");Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.IsLoadingInProcess=false;
 var queued=new HostOwnedEnginesAdapter();queued.chunk7Completion.Request(0);Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.QueuedNames.Add("exact queued load");Check(!queued.TrySettleChunk7Custody()&&queued.worldCleanup==0&&!queued.chunk7Completion.Settled,"queued-only native load blocks final cleanup before any engine exists");Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.QueuedNames.Clear();
 Check(!h.TrySettleChunk7Custody()&&exact.Polls==1&&exact.Disposals==0&&ReferenceEquals(h.worldEngine,exact),"actual host adapter polls unfinished world without releasing reference");
 exact.Ready=true;exact.ThrowDispose=true;Check(!h.TrySettleChunk7Custody()&&ReferenceEquals(h.worldEngine,exact)&&h.worldCleanup==0&&h.faults.Count==1,"actual host adapter retains exact world engine when final Dispose refuses");
 h.runtimeClock.Elapsed=TimeSpan.FromSeconds(46);Check(!h.TrySettleChunk7Custody()&&ReferenceEquals(h.worldEngine,exact)&&h.worldCleanup==0&&h.chunk7Completion.DeadlineExceeded,"actual host adapter deadline cannot discard failed world custody");
 exact.ThrowDispose=false;Check(h.TrySettleChunk7Custody()&&h.worldEngine==null&&h.worldCleanup==1&&h.chunk7Completion.Settled,"actual host adapter releases world reference only after proven successful Dispose");
 var p=new HostOwnedEnginesAdapter();var first=new Engine{Ready=true,ThrowDispose=true};var second=new Engine{Ready=true};p.worldPersistenceEngine=first;p.worldCombatEngine=second;p.chunk7Completion.Request(0);
 Check(!p.TrySettleChunk7Custody()&&ReferenceEquals(p.worldPersistenceEngine,first)&&p.worldCombatEngine==null&&second.Disposals==1&&p.worldCleanup==0,"actual host adapter failure retains persistence while independent combat cleanup settles");
 first.ThrowDispose=false;Check(p.TrySettleChunk7Custody()&&p.worldPersistenceEngine==null&&second.Disposals==1,"actual host adapter retry does not replay released neighbour cleanup");
 var ordinary=new HostOwnedEnginesAdapter();var retainedOrdinary=new Engine();var readyPersistence=new Engine{Ready=true};ordinary.ordinaryEngine=retainedOrdinary;ordinary.worldPersistenceEngine=readyPersistence;ordinary.chunk7Completion.Request(0);
 Check(!ordinary.TrySettleChunk7Custody()&&ReferenceEquals(ordinary.ordinaryEngine,retainedOrdinary)&&ordinary.worldPersistenceEngine==null&&readyPersistence.Disposals==1&&ordinary.worldCleanup==0,"ordinary custody blocks world cleanup while independent persistence settles");
 retainedOrdinary.Ready=true;retainedOrdinary.ThrowDispose=true;Check(!ordinary.TrySettleChunk7Custody()&&ReferenceEquals(ordinary.ordinaryEngine,retainedOrdinary)&&ordinary.worldCleanup==0,"ordinary failed disposal retains exact engine and world fence");
 retainedOrdinary.ThrowDispose=false;Check(ordinary.TrySettleChunk7Custody()&&ordinary.ordinaryEngine==null&&readyPersistence.Disposals==1&&ordinary.worldCleanup==1,"ordinary retry settles without replaying released persistence cleanup");
 VerifyAdditionalOwners();
 HostWorldGateAdapter.Verify();passed+=2;Console.WriteLine("CHUNK7 HOST OWNED ENGINES PASS="+passed+" FAIL=0; extracted production owner adapters and gate");}
 static void VerifyAdditionalOwners(){
  VerifyOwner("assisted cold inventory",(h,e)=>h.assistedColdInventoryEngine=e,h=>h.assistedColdInventoryEngine);
  VerifyOwner("assisted fixture",(h,e)=>h.assistedFixtureEngine=e,h=>h.assistedFixtureEngine);
  VerifyOwner("acquired Manual",(h,e)=>h.acquiredManualEngine=e,h=>h.acquiredManualEngine);
  VerifyOwner("Manual-derived",(h,e)=>h.manualDerivedEngine=e,h=>h.manualDerivedEngine);
  VerifyOwner("Manual seed",(h,e)=>h.manualSeedEngine=e,h=>h.manualSeedEngine);
  VerifyOwner("bootstrap inventory",(h,e)=>h.bootstrapInventoryEngine=e,h=>h.bootstrapInventoryEngine);
  VerifyOwner("native bootstrap",(h,e)=>h.nativeBootstrapEngine=e,h=>h.nativeBootstrapEngine);
  VerifyOwner("Small calibration",(h,e)=>h.smallCalibrationEngine=e,h=>h.smallCalibrationEngine);
  VerifyOwner("progression",(h,e)=>h.progressionEngine=e,h=>h.progressionEngine);
  VerifyOwner("ordinary field",(h,e)=>h.ordinaryFieldEngine=e,h=>h.ordinaryFieldEngine);
 }
 static void VerifyOwner(string name,Action<HostOwnedEnginesAdapter,Engine> bind,Func<HostOwnedEnginesAdapter,Engine> get){
  var h=new HostOwnedEnginesAdapter();var retained=new Engine();var neighbour=new Engine{Ready=true};
  bind(h,retained);h.worldPersistenceEngine=neighbour;h.chunk7Completion.Request(0);
  Check(!h.TrySettleChunk7Custody()&&ReferenceEquals(get(h),retained)&&retained.Polls==1&&retained.Disposals==0&&h.worldPersistenceEngine==null&&neighbour.Disposals==1&&h.worldCleanup==0,name+" pending custody retains exact owner while independent persistence settles");
  retained.Ready=true;retained.ThrowDispose=true;h.runtimeClock.Elapsed=TimeSpan.FromSeconds(46);
  Check(!h.TrySettleChunk7Custody()&&ReferenceEquals(get(h),retained)&&retained.Disposals==1&&h.faults.Count==2&&h.faults.Any(f=>f.Contains("bounded drain"))&&h.faults.Any(f=>f.Contains("retained exact engine lease"))&&h.worldCleanup==0&&h.chunk7Completion.DeadlineExceeded,name+" disposal refusal and deadline retain owner and world fence");
  retained.ThrowDispose=false;
  Check(h.TrySettleChunk7Custody()&&get(h)==null&&retained.Disposals==2&&neighbour.Disposals==1&&h.worldCleanup==1&&h.chunk7Completion.Settled,name+" successful retry releases owner after disposal without repeating neighbour");
  Check(h.TrySettleChunk7Custody()&&retained.Disposals==2&&neighbour.Disposals==1&&h.worldCleanup==1,name+" settled completion does not replay released cleanup");
 }
 static void Check(bool b,string message){if(!b)throw new Exception(message);passed++;Console.WriteLine("PASS "+message);}
}
enum RelationshipState{Mounted,Unmounted}enum CleanupTrigger{Exception}
class GateRuntime{internal bool NoPreparedPairResidue;}class GateRelationship{internal RelationshipState State=RelationshipState.Mounted;internal object PairOwner=new object();internal GateRuntime Runtime=new GateRuntime();internal void Dismount(CleanupTrigger reason){State=RelationshipState.Unmounted;PairOwner=null;Runtime.NoPreparedPairResidue=true;}}
class GateCombat{internal bool HasActiveCommand,HasActiveGroundMovement,HasChargeOwnership;internal int Cancels;internal void Cancel(string why){Cancels++;}}class GatePair{internal GateRelationship Relationship=new GateRelationship();internal GateCombat Combat=new GateCombat();}class GateUnified{internal bool HasUnsettledPreparation;}class GateWorld{internal bool SerializationBusy;internal GateUnified UnifiedTurn=new GateUnified();internal GatePair[] Pairs=new[]{new GatePair(),new GatePair()};internal int Visits;internal string[] Visit(Action<GatePair> action){Visits++;foreach(var pair in Pairs)action(pair);return new string[0];}}class GateHolder{internal GateWorld World=new GateWorld();}class GateControls{internal bool HasUnsettledRelationshipTransition;internal bool HasUnsettledShellFor(GateRelationship relationship){return false;}}
class HostWorldGateAdapter{GateHolder relationship=new GateHolder();GateControls nativeControls=new GateControls();void RecordChunk7CleanupFailure(string error){throw new Exception(error);}
__WORLD_METHOD__
 internal static void Verify(){var h=new HostWorldGateAdapter();h.relationship.World.SerializationBusy=true;if(h.SettleChunk7World()||h.relationship.World.Visits!=0||h.relationship.World.Pairs.Any(p=>p.Combat.Cancels!=0||p.Relationship.PairOwner==null))throw new Exception("busy world allowed actor cleanup before serialization barrier");Console.WriteLine("PASS actual host world serialization barrier precedes all actor mutation");h.relationship.World.SerializationBusy=false;if(!h.SettleChunk7World()||h.relationship.World.Visits!=1||h.relationship.World.Pairs.Any(p=>p.Combat.Cancels!=1||p.Relationship.PairOwner!=null))throw new Exception("settled host world did not release each actor exactly once");Console.WriteLine("PASS actual host world permits independent cleanup after serialization settles");}
}
static class Program{static int Main(){try{HostOwnedEnginesAdapter.Verify();return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}}
