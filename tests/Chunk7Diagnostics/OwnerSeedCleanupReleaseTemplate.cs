using System;using System.Collections.Generic;
// Owner-seed cleanup release: the ACTUAL OwnerSeedCleanupReady / ReleaseSetupWriteCustody
// methods are extracted from Chunk7WorldScenarioEngine.OwnerSeed.cs into the Engine class below.
// Only the native boundaries (projection, probe, builder, loading, world) are stubs.
class JObject{readonly Dictionary<string,object> d=new Dictionary<string,object>();public object this[string k]{get{object v;return d.TryGetValue(k,out v)?v:null;}set{d[k]=value;}}}
static class NativePersistenceIsolation{static bool on;internal static bool HasPendingWrites;internal static bool NativeSlotRotationEnabled=>on;internal static void EnableNativeSlotRotation(){on=true;}internal static void DisableNativeSlotRotation(){on=false;}}
class LoadingProcess{public static LoadingProcess Instance=new LoadingProcess();public bool IsLoadingInProcess;}
class World{public bool SerializationBusy;}class Scope{public void RequireCurrent(){}}
class CharacterBuildController{}
class Driver{public bool SessionOpen;public bool TryGetOwnedBuildSession(out CharacterBuildController b){b=SessionOpen?new CharacterBuildController():null;return SessionOpen;}}
class Chunk7NativeBuilderCancellation{internal static int DrainsUntilSettled;public Chunk7NativeBuilderCancellation(CharacterBuildController b,Action a,Func<bool> busy){}public bool TryDrain(){return DrainsUntilSettled--<=0;}}
class Probe{public bool InSynchronousCallback;public bool Disposed;public object Capture(){return "probe";}public void Dispose(){Disposed=true;}}
class Ledger{public bool Completed;}class UnitEntityData{}class Player{}class Game{public static Game Instance=new Game();public Player Player=new Player();}static class Chunk7NativeFieldRoster{public static UnitEntityData[] Capture(Player p){return new UnitEntityData[0];}}
class Engine{
 internal Driver setupDriver;internal Probe setupProbe;internal bool setupSlotOwned;internal Ledger setupLedger=new Ledger();internal JObject evidence=new JObject();internal World world=new World();Scope nativeScope=new Scope();
 internal bool busy,petsPreserved=true;bool SetupBusy()=>busy||LoadingProcess.Instance.IsLoadingInProcess||NativePersistenceIsolation.HasPendingWrites||world.SerializationBusy;bool OwnerSeedPetsPreserved()=>petsPreserved;static void Require(bool c,string e){if(!c)throw new InvalidOperationException(e);}
__METHODS__
 internal bool Ready()=>OwnerSeedCleanupReady();
}
static class P{
 static int passed,failed;
 static void Check(bool c,string n){if(c){passed++;Console.WriteLine("PASS "+n);}else{failed++;Console.WriteLine("FAIL "+n);}}
 static Engine Failed(bool session,bool projection){NativePersistenceIsolation.DisableNativeSlotRotation();Chunk7NativeBuilderCancellation.DrainsUntilSettled=0;var e=new Engine();e.evidence["ownerSeedSetup"]=new JObject();if(projection){NativePersistenceIsolation.EnableNativeSlotRotation();e.setupSlotOwned=true;}e.setupProbe=new Probe();e.setupDriver=new Driver{SessionOpen=session};return e;}
 static bool Released(Engine e,Probe p)=>!e.setupSlotOwned&&!NativePersistenceIsolation.NativeSlotRotationEnabled&&p.Disposed&&e.setupProbe==null;
 static int Main(){
  foreach(var session in new[]{false,true}){
   var e=Failed(session,true);var p=e.setupProbe;var ready=e.Ready();
   Check(ready&&Released(e,p),"SETUP failure with a live driver (build session "+(session?"open":"closed")+") releases projection and probe before cleanup proves itself");
   Check(!ready||e.setupDriver==null,"released cleanup holds no build driver (session "+session+")");
  }
  {var e=Failed(true,true);var p=e.setupProbe;Chunk7NativeBuilderCancellation.DrainsUntilSettled=1;var first=e.Ready();Check(!first&&e.setupSlotOwned&&!p.Disposed,"an unsettled build-session cancellation retains projection and probe");var second=e.Ready();Check(second&&Released(e,p),"after the native cancellation settles the projection and probe are released");}
  {var e=Failed(false,true);var p=e.setupProbe;p.InSynchronousCallback=true;Check(!e.Ready()&&e.setupSlotOwned&&!p.Disposed,"a probe inside its native callback is not released and cleanup does not prove itself");p.InSynchronousCallback=false;Check(e.Ready()&&Released(e,p),"the probe is released once its callback returns");}
  {var e=Failed(false,true);var p=e.setupProbe;e.busy=true;Check(!e.Ready()&&e.setupSlotOwned&&!p.Disposed,"a busy native writer retains release");e.busy=false;Check(e.Ready()&&Released(e,p),"release follows the settled writer");}
  {var e=Failed(false,false);var p=e.setupProbe;Check(e.Ready()&&p.Disposed&&!NativePersistenceIsolation.NativeSlotRotationEnabled,"autosave-off setup failure (no projection) still disposes the probe");}
  {var e=Failed(false,true);NativePersistenceIsolation.DisableNativeSlotRotation();bool threw=false;try{e.Ready();}catch(InvalidOperationException){threw=true;}Check(threw,"an externally changed projection refuses cleanup proof");}
  {NativePersistenceIsolation.DisableNativeSlotRotation();var e=new Engine();e.setupLedger.Completed=true;e.petsPreserved=false;Check(!e.Ready(),"after SETUP-COMPLETE cleanup requires the preserved setup pets");e.petsPreserved=true;Check(e.Ready(),"completed setup with released custody and preserved pets is ready");}
  {var e=Failed(false,true);NativePersistenceIsolation.EnableNativeSlotRotation();e.setupSlotOwned=false;e.setupDriver=null;e.setupProbe=null;Check(!e.Ready(),"a projection still enabled by anyone is never reported released");NativePersistenceIsolation.DisableNativeSlotRotation();}
  Console.WriteLine("OWNER-SEED CLEANUP RELEASE PASS="+passed+" FAIL="+failed);return failed==0?0:1;}
}
