using System;using System.Collections.Generic;using System.Linq;using System.Runtime.CompilerServices;
class JObject:Dictionary<string,object>{public new dynamic this[string key]{get{object value;return TryGetValue(key,out value)?value:null;}set{base[key]=value;}}}
class JArray:List<dynamic>{public JArray(){}public JArray(IEnumerable<int> x){foreach(var v in x)Add(v);}public new dynamic this[int i]{get{return base[i];}set{base[i]=value;}}}
class Game{internal static Game Instance=new Game();internal SaveManager SaveManager=new SaveManager();internal object Player=new object(),State=new object();internal BlueprintArea CurrentlyLoadedArea;internal int Loads,Areas;internal bool ThrowLoad;internal void LoadGame(SaveInfo s){Loads++;if(ThrowLoad)throw new Exception("native dispatch failed after entry");}internal void LoadArea(Entry e,AutoSaveMode m){Areas++;}}
class SaveManager{internal string SavePath="exact-root";internal List<Action> Callbacks=new List<Action>();internal void AddCallbackAfterLoad(Action a){Callbacks.Add(a);}}
class SaveInfo{internal string FileName="immutable.zks";}
class BlueprintArea{internal string AssetGuidThreadSafe;}
class Entry{internal string AssetGuidThreadSafe;internal BlueprintArea Area;}
static class ResourcesLibrary{internal static Entry Entry;internal static T TryGetBlueprint<T>(string id)where T:class{return Entry as T;}}
class BlueprintAreaEnterPoint:Entry{}
enum AutoSaveMode{None}enum RuntimeSaveOperation{Load}
class UnitEntityData{}
class Request{internal Target[] PersistenceReplayTargets;internal Descriptor PersistenceLoad=new Descriptor();}
class Descriptor{internal string Sha256="immutable-hash";}
class Target{internal string EnterPoint="entry",Area="target",AutoSaveMode="None";}
class RuntimeSaveAuthorization{internal int AuthorityGeneration=7,AuthorizedWriteCount,UnauthorizedLoadCount,UnauthorizedWriteCount,FatalViolationCount,AuthorizedLoadCount=1;internal object Scope;internal bool IsExactPersistenceScope(object s){return ReferenceEquals(Scope,s);}}
class PersistenceSaveAuthorization{internal string Root="exact-root";internal int? MaximumNativeLoadRequests=3;internal int AdmittedNativeLoadRequests=1;internal string Refusal;internal string Validate(RuntimeSaveOperation op,object target,string root){return Refusal;}}
static class NativePersistenceIsolation{internal static object Scope;internal static bool IsExactAuthority(object v){return ReferenceEquals(Scope,v);}}
static class NativeMountedSaveFormat{internal static bool ArchiveNativeSupportEnabled=true;internal static int SelectedWriterSchema=3;}
static class GameMountedRelationshipService{internal static int NativePairCapacity=2;}
class Settings{internal bool EnablePairedActivation=true,EnableUnsafeMovementExperiment;}
class NativeArchiveRestorationProbe:IDisposable{internal static bool ThrowInstall,ThrowDispose;internal bool Installed,Disposed;internal NativeArchiveRestorationProbe(object p,object w){}internal void Install(){Installed=true;if(ThrowInstall)throw new Exception("partial restoration hook");}public void Dispose(){if(ThrowDispose)throw new Exception("unpatch retained");Disposed=true;}}
class NativeModeTransitionProbe:IDisposable{internal int Dispatches;internal NativeModeTransitionProbe(bool t){}internal void DispatchTemporaryValueIfRequired(){Dispatches++;}public void Dispose(){}}
class HarmonyInstance{internal static HarmonyInstance Create(string id){return new HarmonyInstance();}internal void UnpatchAll(string id){Chunk7WorldRetentionProbe.UnpatchCalls++;if(Chunk7WorldRetentionProbe.ThrowDispose)throw new Exception("unpatch retained");Chunk7WorldRetentionProbe.LiveHooks=0;}}
class Chunk7WorldRetentionProbe:IDisposable{
 internal static bool ThrowInstall,ThrowDispose;internal static int UnpatchCalls,LiveHooks;private const string HarmonyId="probe";private static Chunk7WorldRetentionProbe active;
 private object persistence,world,controls,identities;private UnitEntityData[] actors=new UnitEntityData[0];private HarmonyInstance harmony;private bool attempted,installed,disposed;
 internal bool Installed=>installed;internal bool Disposed=>disposed;internal static bool HasActive=>active!=null;
 internal Chunk7WorldRetentionProbe(object p,object w,object c,object ids){persistence=p;world=w;controls=c;identities=ids;}
 private void Patch(string name,string before,string after){LiveHooks++;if(ThrowInstall)throw new Exception("partial retention hook");}
 internal JObject Capture(){return new JObject{["complete"]=installed};}
 __RETENTION_METHODS__
}
namespace UnityEngine{static class Time{internal static int frameCount=123;}}
namespace KingmakerMountedCombat.Diagnostics{
class ReplayAdapter{
    internal bool beforeLoad,Cold=true,CombatCase,Schema1ResaveCase,RepeatedWorldCase=true,RepeatedAreaCase,TurnBasedCase;
    internal object world=new object(),persistence=new object(),controls=new object(),beforeLoadGame;
    internal Settings settings=new Settings();internal bool originalPaired,originalMovement;
    internal RuntimeSaveAuthorization replayAuthorization=new RuntimeSaveAuthorization();internal PersistenceSaveAuthorization replaySaveAuthority=new PersistenceSaveAuthorization();
    internal int replayAuthorityGeneration;internal object replaySaveManager,replayPriorPlayer,replayPriorState;internal RetainedObjectIdentity replayIdentities;
    internal NativeArchiveRestorationProbe restoration;internal Chunk7WorldRetentionProbe retention;internal NativeModeTransitionProbe mode;
    internal bool replayBegun,replayPending,replayCallback,Idle=true,ForeignWorld,ThrowSnapshot,ThrowInput,ThrowAdoption,CleanupStarted;
    internal int replayCompletedLoads=1,replayCompletedAreas,replayOrdinal,replayReadyFrames,SnapshotCalls,NativeScopeChecks,Adoptions;
    internal UnitEntityData[] actors=new[]{new UnitEntityData(),new UnitEntityData(),new UnitEntityData(),new UnitEntityData()},replayPriorActors;
    internal BlueprintArea replayDestination;internal SaveInfo replayInput=new SaveInfo();internal Request request=new Request{PersistenceReplayTargets=new[]{new Target(),new Target(),new Target()}};
    internal JArray replayRequests=new JArray();internal JObject evidence=new JObject{["repeatedWorlds"]=new JObject()};internal List<string> failures=new List<string>();
    internal ReplayAdapter(){Game.Instance=new Game();beforeLoadGame=Game.Instance;replayAuthorization.Scope=replaySaveAuthority;NativePersistenceIsolation.Scope=replaySaveAuthority;NativeArchiveRestorationProbe.ThrowInstall=NativeArchiveRestorationProbe.ThrowDispose=false;Chunk7WorldRetentionProbe.ThrowInstall=Chunk7WorldRetentionProbe.ThrowDispose=false;}
    private static void Require(bool c,string m){if(!c)throw new InvalidOperationException(m);}
    private void RequireNativeWorld(){NativeScopeChecks++;Require(!ForeignWorld,"foreign world");}
    private void RequireReplayInput(){Require(!ThrowInput,"immutable input changed");}
    private void AssertReplayQuiet(){}private object ReplayTarget(){return new object();}
    private bool ReplayNativeBoundaryIdle(){return Idle;}
    private void BindRepeatedLoadedWorld(int n,bool replacement){Adoptions++;Require(!ThrowAdoption,"adoption refused");replayCompletedLoads=n;}
    private void AdmitRepeatedArea(){Adoptions++;Require(!ThrowAdoption,"area adoption refused");replayCompletedAreas=replayOrdinal;}
    private JObject ReplaySnapshot(){SnapshotCalls++;if(ThrowSnapshot)throw new Exception("snapshot failed before dispatch");var id=replayIdentities.Get(Game.Instance.Player);return new JObject{["active"]=0,["owners"]=new JArray(),["preparedOrViewResidue"]=0,["liveControlBoundaries"]=0,["commandOwners"]=0,["unsettledPreparation"]=false,["relationshipTransition"]=false,["serializationBusy"]=false,["pendingWrites"]=false,["saveScope"]=false,["saveFence"]=false,["object"]=id};}
    private void Fail(string row,string error){failures.Add(row+":"+error);}private void BeginCleanup(){CleanupStarted=true;}
__METHODS__
    internal void Init(){InitializeReplayAuthority();}internal void DispatchLoad(){DispatchRepeatedLoad();}internal void DispatchArea(){DispatchRepeatedArea();}internal void CleanupAuthority(){RequireRepeatedCleanupWorld();}internal void Settle(){CaptureRepeatedSettlement();}internal void End(){EndReplayObservation();}
}
static class Program{
static int passed,failed;static void Check(string name,Action body){try{body();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+" "+e);}}
static void True(bool c,string m){if(!c)throw new Exception(m);}static void Reject(Action a){bool f=false;try{a();}catch{f=true;}True(f,"expected refusal");}
static ReplayAdapter Ready(bool area=false){var a=new ReplayAdapter();a.RepeatedAreaCase=area;a.replaySaveAuthority.MaximumNativeLoadRequests=area?1:3;a.Init();a.replayBegun=true;return a;}
static int Main(){
Check("partial first observer acquisition keeps exact authority and retryable owner",()=>{var a=new ReplayAdapter();NativeArchiveRestorationProbe.ThrowInstall=true;Reject(a.BeginBeforeLoad);True(a.restoration!=null&&a.restoration.Installed,"partial restoration owner lost");a.CleanupAuthority();NativeArchiveRestorationProbe.ThrowDispose=true;Reject(a.restoration.Dispose);True(!a.restoration.Disposed,"unpatch failed but owner marked disposed");NativeArchiveRestorationProbe.ThrowDispose=false;a.restoration.Dispose();a.restoration=null;a.Settle();True(a.replayIdentities.Released,"menu partial installation leaked identity lease");});
Check("partial retention installation retains both observer owners",()=>{var a=new ReplayAdapter();Chunk7WorldRetentionProbe.ThrowInstall=true;Reject(a.BeginBeforeLoad);a.CleanupAuthority();True(a.restoration!=null&&a.retention!=null&&a.mode==null,"fallible install lost custody");a.End();a.restoration.Dispose();a.restoration=null;a.Settle();True(a.retention==null&&a.replayIdentities.Released,"partial retention cleanup did not settle");});
Check("load snapshot failure never arms native transaction",()=>{var a=Ready();a.ThrowSnapshot=true;Reject(a.DispatchLoad);True(!a.replayPending&&a.replayOrdinal==0&&Game.Instance.Loads==0&&Game.Instance.SaveManager.Callbacks.Count==0,"snapshot failure invented load custody");a.CleanupAuthority();});
Check("area snapshot failure never arms native transaction",()=>{var a=Ready(true);Game.Instance.CurrentlyLoadedArea=new BlueprintArea{AssetGuidThreadSafe="source"};ResourcesLibrary.Entry=new BlueprintAreaEnterPoint{AssetGuidThreadSafe="entry",Area=new BlueprintArea{AssetGuidThreadSafe="target"}};a.ThrowSnapshot=true;Reject(a.DispatchArea);True(!a.replayPending&&a.replayOrdinal==0&&Game.Instance.Areas==0,"snapshot failure invented area custody");a.CleanupAuthority();});
Check("native dispatch exception retains callback and exact pending epoch",()=>{var a=Ready();Game.Instance.ThrowLoad=true;Reject(a.DispatchLoad);True(a.replayPending&&a.replayOrdinal==2&&Game.Instance.Loads==1&&Game.Instance.SaveManager.Callbacks.Count==1,"uncertain native dispatch lost custody");Reject(a.CleanupAuthority);True(a.Adoptions==0,"missing callback adopted a world");});
Check("exact successful callback enables one bounded new world admission",()=>{var a=Ready();a.DispatchLoad();a.replayAuthorization.AuthorizedLoadCount=2;a.replaySaveAuthority.AdmittedNativeLoadRequests=2;Game.Instance.SaveManager.Callbacks[0]();True(a.replayCallback&&a.failures.Count==0,"exact callback refused");a.CleanupAuthority();True(!a.replayPending&&a.Adoptions==1&&a.replayCompletedLoads==2,"exact ready graph not admitted");});
Check("stale callback cannot adopt another pending epoch",()=>{var a=Ready();a.DispatchLoad();a.replayOrdinal=3;a.replayAuthorization.AuthorizedLoadCount=2;a.replaySaveAuthority.AdmittedNativeLoadRequests=2;Game.Instance.SaveManager.Callbacks[0]();True(!a.replayCallback&&a.failures.Count==1&&a.CleanupStarted,"stale callback accepted");});
Check("callback from replaced SaveManager refuses",()=>{var a=Ready();a.DispatchLoad();var cb=Game.Instance.SaveManager.Callbacks[0];Game.Instance.SaveManager=new SaveManager();a.replayAuthorization.AuthorizedLoadCount=2;a.replaySaveAuthority.AdmittedNativeLoadRequests=2;cb();True(!a.replayCallback&&a.failures.Count==1,"foreign manager callback accepted");});
Check("duplicate callback preserves first event and fails closed",()=>{var a=Ready();a.DispatchLoad();a.replayAuthorization.AuthorizedLoadCount=2;a.replaySaveAuthority.AdmittedNativeLoadRequests=2;var cb=Game.Instance.SaveManager.Callbacks[0];cb();cb();True(a.replayCallback&&a.failures.Count==1&&a.CleanupStarted,"duplicate callback promoted success");});
Check("exhausted pre-disposal authority never registers callback",()=>{var a=Ready();a.replaySaveAuthority.Refusal="exhausted";Reject(a.DispatchLoad);True(Game.Instance.Loads==0&&!a.replayPending&&Game.Instance.SaveManager.Callbacks.Count==0,"exhausted load entered native disposal");});
Check("active native pipeline cannot be adopted by cleanup",()=>{var a=Ready();a.replayPending=true;a.Idle=false;a.replayCallback=true;Reject(a.CleanupAuthority);True(a.Adoptions==0&&a.replayPending,"unfinished world was adopted");});
Check("refused exact graph keeps pending custody",()=>{var a=Ready();a.replayPending=true;a.replayCallback=true;a.ThrowAdoption=true;Reject(a.CleanupAuthority);True(a.replayPending,"failed graph validation dropped custody");});
Check("unchanged-world denied attempt does not prevent safe cleanup",()=>{var a=Ready();a.replayAuthorization.FatalViolationCount=1;a.replayAuthorization.UnauthorizedWriteCount=1;a.CleanupAuthority();Reject(a.DispatchLoad);True(Game.Instance.Loads==0,"failed evidence selected another load");});
foreach(var change in new[]{"game","manager","scope","generation","root","cap","write"}){var captured=change;Check("cleanup refuses changed exact authority "+change,()=>{var a=Ready();switch(captured){case "game":Game.Instance=new Game();break;case "manager":Game.Instance.SaveManager=new SaveManager();break;case "scope":NativePersistenceIsolation.Scope=new object();break;case "generation":a.replayAuthorization.AuthorityGeneration++;break;case "root":Game.Instance.SaveManager.SavePath="foreign";break;case "cap":a.replaySaveAuthority.MaximumNativeLoadRequests=4;break;case "write":a.replayAuthorization.AuthorizedWriteCount=1;break;}Reject(a.CleanupAuthority);True(a.Adoptions==0,"foreign cleanup admitted a world");});}
Check("settlement releases bounded labels only after full cleanup snapshot",()=>{var a=Ready();a.Settle();True(a.replayIdentities.Released&&a.replayIdentities.RetainedCount==0&&a.SnapshotCalls==1,"labels not released after final snapshot");a.ThrowSnapshot=true;a.Settle();True(a.SnapshotCalls==1,"artifact retry reread released identity registry");});
Check("settlement refuses foreign world even after registry release",()=>{var a=Ready();a.Settle();a.ForeignWorld=true;Reject(a.Settle);});
Check("live observer cannot release retained object identities",()=>{var a=Ready();a.retention=new Chunk7WorldRetentionProbe(null,null,null,null);Reject(a.Settle);True(!a.replayIdentities.Released,"live observer released native labels");});
Check("probe unpatch failure preserves owner and labels",()=>{var a=Ready();a.retention=new Chunk7WorldRetentionProbe(new object(),new object(),new object(),a.replayIdentities);a.retention.Install();Chunk7WorldRetentionProbe.ThrowDispose=true;Reject(a.End);True(a.retention!=null&&!a.replayIdentities.Released,"unpatch failure dropped observer identity custody");Chunk7WorldRetentionProbe.ThrowDispose=false;a.End();a.Settle();});
Check("actual retention Install retains partial hook ownership and refuses replay",()=>{var a=Ready();a.retention=new Chunk7WorldRetentionProbe(new object(),new object(),new object(),a.replayIdentities);Chunk7WorldRetentionProbe.ThrowInstall=true;Reject(a.retention.Install);True(Chunk7WorldRetentionProbe.HasActive&&!a.retention.Installed&&!a.retention.Disposed,"partial actual observer lost ownership");var count=Chunk7WorldRetentionProbe.LiveHooks;Reject(a.retention.Install);True(Chunk7WorldRetentionProbe.LiveHooks==count,"partial Install replayed native patch");Chunk7WorldRetentionProbe.ThrowInstall=false;a.End();True(!Chunk7WorldRetentionProbe.HasActive&&Chunk7WorldRetentionProbe.LiveHooks==0&&!a.replayIdentities.Released,"probe disposal released shared epoch labels prematurely");a.Settle();});
Check("actual retention Dispose remains retryable and idempotent",()=>{var a=Ready();var p=new Chunk7WorldRetentionProbe(new object(),new object(),new object(),a.replayIdentities);p.Install();True(p.Installed&&Chunk7WorldRetentionProbe.LiveHooks==3,"expected three exact production boundaries");Chunk7WorldRetentionProbe.ThrowDispose=true;Reject(p.Dispose);True(!p.Disposed&&Chunk7WorldRetentionProbe.HasActive,"failed Unpatch dropped exact owner");Chunk7WorldRetentionProbe.ThrowDispose=false;p.Dispose();var calls=Chunk7WorldRetentionProbe.UnpatchCalls;p.Dispose();True(p.Disposed&&!Chunk7WorldRetentionProbe.HasActive&&Chunk7WorldRetentionProbe.UnpatchCalls==calls,"settled disposal replayed native unpatch");Reject(p.Install);a.Settle();});
Console.WriteLine("REPEATED WORLD ACTUAL METHODS PASS="+passed+" FAIL="+failed);return failed==0?0:1;
}
}
}
