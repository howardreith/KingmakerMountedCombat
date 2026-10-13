
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics {
static class Time { public static int frameCount; }
class CodeInstruction {}
class Clock { public TimeSpan Elapsed; }
enum GameModeType { None, Default, Dialog }
class Actor { public string UniqueId="actor",CharacterName="Native";public object Descriptor=new object(),View=new object(); }
class Holder { public Actor Value; }
class Player { public Holder MainCharacter=new Holder(); public string GameId; }
class Area { public string AssetGuidThreadSafe="area"; }
class BlueprintAreaPreset { public string AssetGuid="preset";public Area Area=new Area(); }
class BlueprintRoot { public static BlueprintRoot Instance=new BlueprintRoot(); }
class MainMenu { public bool AlreadyEntering;public Action Pending;public int Dispatches;public void EnterGame(Action action){Dispatches++;Pending=action;} }
class UserInterface { public MainMenu MainMenu=new MainMenu(); }
class NativeState { public AreaState LoadedAreaState=new AreaState(); }
class AreaState { public object MainState=new object(); }
class Game {
 public static Game Instance;public static object NewGameUnit;public Player Player=new Player();public SaveManager SaveManager=new SaveManager();
 public object CurrentlyLoadedArea;public NativeState State=new NativeState();public UserInterface UI=new UserInterface();public bool IsControllerMouse=true,Dialog;
 public bool IsModeActive(GameModeType mode)=>Dialog;
 public void LoadNewGame(BlueprintAreaPreset p,SaveInfo s){NativeCalls++;}
 public int NativeCalls;
}
class SaveInfo { public enum SaveType { Auto,Manual } public SaveType Type=SaveType.Auto;public bool IsAutoLevelupSave; public string FileName="Auto_1.zks",Name="Auto",FolderName="owned/Auto_1.zks",GameId,GameName="Native";public Area Area=new Area(); }
class SaveManager { public string SavePath="C:/owned/SaveGames"; }
static class SceneName { public const string MainMenu="MainMenu"; }
struct Scene { public bool isLoaded; }
static class SceneManager { public static bool Loaded=true;public static Scene GetSceneByName(string name)=>new Scene{isLoaded=Loaded}; }
class LoadingScreen { public static LoadingScreen Instance=new LoadingScreen(); }
class DialogMessageBox { public static DialogMessageBox Instance;public bool IsShown; }
class LoadingProcess { public static LoadingProcess Instance=new LoadingProcess();public bool IsLoadingInProcess;public List<string> QueuedNames=new List<string>(); }
enum OwnedWorkerTeardownVerdict { Settled,Refused }
enum RelationshipState { Unmounted,Mounted }
class Runtime { public bool NoPreparedPairResidue=true; }
class Relationship { public RelationshipState State;public object PairOwner;public Runtime Runtime=new Runtime(); }
class Combat { public bool HasActiveCommand,HasActiveGroundMovement,HasChargeOwnership; }
class Pair { public Relationship Relationship=new Relationship();public Combat Combat=new Combat(); }
class Registry { public object[] Owners=new object[0]; }
class Unified { public bool HasUnsettledPreparation; }
class MountedWorldService { public bool SerializationBusy;public Registry Registry=new Registry();public Unified UnifiedTurn=new Unified();public Pair[] Pairs=new[]{new Pair()}; }
class NativeMountedControlService { public bool HasUnsettledRelationshipTransition;public long NativeCastRequestCount; }
class MountedPersistenceService {
 public bool AreaTransitionPending,HasActiveSaveScope,HasSaveAdmissionFence;public int SnapshotCount,CompletedSaveCount,SemanticRestoreCount,PresentationRestoreCount,ArchivePostLoadObservedCount,DrainCalls;public OwnedWorkerTeardownVerdict Verdict;
 public OwnedWorkerTeardownVerdict DrainForTeardown(int timeout){DrainCalls++;return Verdict;}
}
class WorkingFixture {}
class Fixture { public WorkingFixture Working=new WorkingFixture(); }
class RuntimeRequest { public Fixture Fixture=new Fixture(); }
class RuntimeSubscenarioResult { public string Name,Status;public int AssertionPassCount,AssertionFailCount;public string[] Errors; }
class RuntimeSaveAuthorization {
 public bool IsActive=true,Exact=true;public int AuthorityGeneration=4,AuthorizedLoadCount,AuthorizedWriteCount,UnauthorizedLoadCount,UnauthorizedWriteCount,FatalViolationCount,SuppressedWorkingWriteCount,BaselineLoadRequestCount;
 public bool IsExactPersistenceScope(PersistenceSaveAuthorization scope)=>Exact;
}
class PersistenceSaveAuthorization {
 public string Root="C:/owned/SaveGames",BootstrapGameId;public int MaximumNativeLoadRequests=0,MaximumNativeWriteRequests=1,AdmittedNativeLoadRequests,AdmittedNativeWriteRequests,BootstrapFreezeCount;
 public bool BootstrapWindowOpen,Matches=true; public bool MatchesNativeBootstrapPlan(WorkingFixture f,string area)=>Matches;
 public void OpenBootstrapWindow(){BootstrapWindowOpen=true;}public void CloseUnusedBootstrapWindow(){if(BootstrapGameId==null){if(AdmittedNativeWriteRequests!=0)throw new InvalidOperationException("Unminted admitted write");BootstrapWindowOpen=false;}}
}
static class NativePersistenceIsolation {
 public static bool HasPendingWrites,NativeSlotRotationEnabled,Exact=true;public static int EnableCalls,DisableCalls;public static string ObservedStashFolder=Path.Combine("C:/owned","Areas");
 public static bool IsExactAuthority(PersistenceSaveAuthorization scope)=>Exact;
 public static void EnableNativeSlotRotation(){EnableCalls++;NativeSlotRotationEnabled=true;}
 public static void DisableNativeSlotRotation(){DisableCalls++;NativeSlotRotationEnabled=false;}
}
static class RuntimeLoaderInventory {
 public static int PendingWrites,FinalWrites,WriteFailures;public static string FinalStatus;public static HashSet<string> Leaves=new HashSet<string>();
 public static void WriteArtifact(RuntimeRequest r,string leaf,string kind,string status,JObject payload,IEnumerable<string> errors){
  if(WriteFailures>0){WriteFailures--;throw new IOException("Injected writer failure before creation");}
  if(!Leaves.Add(leaf))throw new IOException("Immutable leaf already exists");
  if(leaf.Contains("pending"))PendingWrites++;else{FinalWrites++;FinalStatus=status;}
 }
}
class HarmonyInstance {
 public static int Patches,LiveHooks,PatchFailureAt,UnpatchFailures,UnpatchCalls;
 public static HarmonyInstance Create(string id)=>new HarmonyInstance();
 public void Patch(){Patches++;LiveHooks++;if(PatchFailureAt==Patches)throw new InvalidOperationException("Native patch boundary failed after install");}
 public void UnpatchAll(string id){UnpatchCalls++;if(UnpatchFailures-->0)throw new InvalidOperationException("Native unpatch refused");LiveHooks=0;}
 public static void Reset(){Patches=LiveHooks=PatchFailureAt=UnpatchFailures=UnpatchCalls=0;}
}
static class MetadataBoundary { public static void Require(){} }
static class Chunk7NativeBootstrapSelection { public const string AutoLeaf="Auto_1.zks"; }
class Chunk7NativeBootstrapScenario {
 internal const string EvidenceFileName="chunk7-native-bootstrap-evidence.json",EvidenceKind="chunk7-native-bootstrap-evidence",PendingFileName="chunk7-native-bootstrap-cleanup-pending.json",PendingKind="chunk7-native-bootstrap-cleanup-pending";
 internal RuntimeRequest request=new RuntimeRequest();internal RuntimeSaveAuthorization authorization=new RuntimeSaveAuthorization();
 internal MountedWorldService world=new MountedWorldService();internal MountedPersistenceService persistence=new MountedPersistenceService();internal NativeMountedControlService controls=new NativeMountedControlService();
 internal Game game;internal Player player;internal SaveManager saveManager;internal Clock clock=new Clock();internal JObject evidence=new JObject();
 internal List<string> errors=new List<string>();internal List<RuntimeSubscenarioResult> results=new List<RuntimeSubscenarioResult>();
 internal PersistenceSaveAuthorization authority;internal int generation;internal BlueprintRoot blueprintRoot;internal BlueprintAreaPreset preset=new BlueprintAreaPreset();internal Chunk7NativeBootstrapProbe probe;
 internal object nativeState,nativeAreaState,nativeMainState,nativeActor,nativeDescriptor,nativeView;internal string nativeActorId,nativeName;
 internal bool preflightReady=true,dispatched,callbackEntered,callbackReturned,slotOwned,priorSlot,cleanup,settled,completed,disposed,pending;
 internal double cleanupStart;internal int snapshotsBefore,savesBefore;internal long castsBefore;
 internal Chunk7NativeBootstrapScenario(){game=Game.Instance=new Game();player=game.Player;saveManager=game.SaveManager;blueprintRoot=BlueprintRoot.Instance;}
 internal void Admit()=>RequireNativeMenuAdmission();
 internal void StartCleanup()=>BeginCleanup();
 internal void Poll()=>PollCleanup();
 internal void Bind(PersistenceSaveAuthorization scope)=>BindAndStart(scope);
 private BlueprintAreaPreset ResolveMainPreset()=>preset;
 private void RequireNativeRangerSelection(){nativeRangerChecks++;Require(nativeRangerReady,"Exact native Ranger boundary refused");}
 internal bool nativeRangerReady=true;internal int nativeRangerChecks;
 private static bool NativeMenuAlreadyEntering(MainMenu menu)=>menu.AlreadyEntering;
 private JObject Snapshot()=>new JObject();
 private JObject AuthoritySnapshot()=>new JObject();
 /*ACTUAL_SCENARIO_METHODS*/
}
static class BootstrapAdapterTests {
 static int pass,fail;
 static void Check(bool condition,string label){if(condition){pass++;Console.WriteLine("PASS "+label);}else{fail++;Console.WriteLine("FAIL "+label);}}
 static bool Throws(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
 static Chunk7NativeBootstrapScenario New(){
  HarmonyInstance.Reset();Game.NewGameUnit=null;SceneManager.Loaded=true;LoadingScreen.Instance=new LoadingScreen();LoadingProcess.Instance=new LoadingProcess();DialogMessageBox.Instance=null;
  NativePersistenceIsolation.HasPendingWrites=NativePersistenceIsolation.NativeSlotRotationEnabled=false;NativePersistenceIsolation.Exact=true;NativePersistenceIsolation.EnableCalls=NativePersistenceIsolation.DisableCalls=0;NativePersistenceIsolation.ObservedStashFolder=Path.Combine(Path.GetDirectoryName("C:/owned/SaveGames"),"Areas");
  RuntimeLoaderInventory.Leaves.Clear();RuntimeLoaderInventory.FinalWrites=RuntimeLoaderInventory.PendingWrites=RuntimeLoaderInventory.WriteFailures=0;RuntimeLoaderInventory.FinalStatus=null;Time.frameCount=10;UnityEngine.Time.frameCount=10;
  return new Chunk7NativeBootstrapScenario();
 }
 static void Reject(string name,Action<Chunk7NativeBootstrapScenario> change){var s=New();change(s);Check(Throws(s.Admit)&&s.game.UI?.MainMenu?.Dispatches!=1,name);}
 static void Invoke(string name,params object[] args){var type=typeof(Chunk7NativeBootstrapProbe).GetNestedType("Hooks",BindingFlags.NonPublic);type.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);}
 static JObject Capture(Chunk7NativeBootstrapProbe p)=>p.Capture();
 static Chunk7NativeBootstrapProbe NewProbe(out Game g,out BlueprintAreaPreset preset){New();g=Game.Instance;preset=new BlueprintAreaPreset();var p=new Chunk7NativeBootstrapProbe(g,preset);p.Install();return p;}
 static void Enter(Chunk7NativeBootstrapProbe p,Game g,BlueprintAreaPreset preset){Invoke("NewGameBefore",g,preset,null);g.Player.MainCharacter.Value=new Actor();g.Player.GameId="33333333-3333-3333-3333-333333333333";Invoke("IdentityAssigned",g);}
 public static int Main(){
  var s=New();s.Admit();Check(s.game.UI.MainMenu.Dispatches==0,"exact idle menu admission is read only");
  Reject("foreign Game",x=>Game.Instance=new Game());Reject("foreign Player",x=>x.game.Player=new Player());Reject("foreign SaveManager",x=>x.game.SaveManager=new SaveManager());
  Reject("existing main actor",x=>x.player.MainCharacter.Value=new Actor());Reject("loaded area",x=>x.game.CurrentlyLoadedArea=new object());Reject("character product",x=>Game.NewGameUnit=new object());
  Reject("non-PC controller",x=>x.game.IsControllerMouse=false);Reject("menu scene absent",x=>SceneManager.Loaded=false);Reject("menu object absent",x=>x.game.UI.MainMenu=null);Reject("loading screen absent",x=>LoadingScreen.Instance=null);
  Reject("active native loading",x=>LoadingProcess.Instance.IsLoadingInProcess=true);Reject("queued native loading",x=>LoadingProcess.Instance.QueuedNames.Add("foreign"));
  Reject("pending writer",x=>NativePersistenceIsolation.HasPendingWrites=true);Reject("area transfer custody",x=>x.persistence.AreaTransitionPending=true);Reject("active save scope",x=>x.persistence.HasActiveSaveScope=true);
  Reject("admission fence",x=>x.persistence.HasSaveAdmissionFence=true);Reject("serialization fence",x=>x.world.SerializationBusy=true);
  Reject("dialog mode",x=>x.game.Dialog=true);Reject("unexpected dialog",x=>DialogMessageBox.Instance=new DialogMessageBox{IsShown=true});Reject("native menu already entering",x=>x.game.UI.MainMenu.AlreadyEntering=true);
  Reject("registry owner",x=>x.world.Registry.Owners=new[]{new object()});Reject("retained transition",x=>x.controls.HasUnsettledRelationshipTransition=true);
  Reject("shared preparation",x=>x.world.UnifiedTurn.HasUnsettledPreparation=true);Reject("mounted boundary",x=>x.world.Pairs[0].Relationship.State=RelationshipState.Mounted);
  Reject("local owner",x=>x.world.Pairs[0].Relationship.PairOwner=new object());Reject("local preparation residue",x=>x.world.Pairs[0].Relationship.Runtime.NoPreparedPairResidue=false);
  Reject("exact command",x=>x.world.Pairs[0].Combat.HasActiveCommand=true);Reject("ground movement",x=>x.world.Pairs[0].Combat.HasActiveGroundMovement=true);Reject("charge owner",x=>x.world.Pairs[0].Combat.HasChargeOwnership=true);
  s=New();s.nativeRangerReady=false;s.Bind(new PersistenceSaveAuthorization());Check(s.cleanup&&!s.dispatched&&s.game.UI.MainMenu.Dispatches==0&&s.game.NativeCalls==0&&NativePersistenceIsolation.EnableCalls==0,"non-native Ranger surface refuses before dispatch or slot mutation");s.Poll();
  s=New();s.Bind(new PersistenceSaveAuthorization());s.nativeRangerReady=false;s.game.UI.MainMenu.Pending();Check(s.cleanup&&s.callbackReturned&&s.game.NativeCalls==0&&!s.authority.BootstrapWindowOpen&&s.nativeRangerChecks==2,"Ranger change after preflight refuses queued callback before native write window");s.Poll();
  s=New();s.Bind(new PersistenceSaveAuthorization());if(s.errors.Count!=0)Console.WriteLine("ADAPTER BIND: "+string.Join("|",s.errors));Check(s.dispatched&&!s.callbackEntered&&s.probe!=null&&NativePersistenceIsolation.NativeSlotRotationEnabled,"field and slot custody precede native callback dispatch");
  s.StartCleanup();s.Poll();Check(!s.completed&&HarmonyInstance.UnpatchCalls==0&&s.slotOwned,"pending native callback retains probe and slot");
  s.clock.Elapsed=TimeSpan.FromSeconds(46);s.Poll();s.Poll();Check(RuntimeLoaderInventory.PendingWrites==1&&RuntimeLoaderInventory.FinalWrites==0,"pending deadline writes one separate immutable failure leaf");
  s.game.UI.MainMenu.Pending();Check(s.callbackReturned&&s.game.NativeCalls==0&&!s.authority.BootstrapWindowOpen,"cleanup-requested owned callback returns without new campaign or write window");
  s.Poll();Check(s.completed&&s.settled&&!s.slotOwned&&!NativePersistenceIsolation.NativeSlotRotationEnabled&&!s.authority.BootstrapWindowOpen&&RuntimeLoaderInventory.FinalWrites==1&&RuntimeLoaderInventory.FinalStatus=="FAIL","eventual settlement preserves failure and emits final once");
  s=New();s.Bind(new PersistenceSaveAuthorization());DialogMessageBox.Instance=new DialogMessageBox{IsShown=true};LoadingProcess.Instance.IsLoadingInProcess=true;s.Update();Check(s.cleanup&&s.errors.Any(x=>x.Contains("unexpected dialog"))&&!s.completed&&s.slotOwned&&HarmonyInstance.UnpatchCalls==0,"unexpected popup fails immediately before native loading return and retains callback custody");s.game.UI.MainMenu.Pending();Check(s.callbackReturned&&s.game.NativeCalls==0&&!s.authority.BootstrapWindowOpen,"pending callback after popup refusal cannot start native campaign");DialogMessageBox.Instance=null;LoadingProcess.Instance.IsLoadingInProcess=false;s.Poll();
  s=New();s.Bind(new PersistenceSaveAuthorization());s.game.UI.MainMenu.Pending();Check(s.callbackReturned&&s.game.NativeCalls==1&&s.authority.BootstrapWindowOpen&&s.errors.Count==0,"valid admitted callback starts exactly one native campaign and write window");s.StartCleanup();s.Poll();
  s=New();s.Bind(new PersistenceSaveAuthorization());DialogMessageBox.Instance=new DialogMessageBox{IsShown=true};s.game.UI.MainMenu.Pending();Check(s.cleanup&&s.callbackReturned&&s.game.NativeCalls==0&&!s.authority.BootstrapWindowOpen,"popup arriving immediately before callback refuses native campaign without Update");DialogMessageBox.Instance=null;s.Poll();
  s=New();HarmonyInstance.PatchFailureAt=1;HarmonyInstance.UnpatchFailures=1;s.Bind(new PersistenceSaveAuthorization());
  Check(s.cleanup&&s.probe!=null&&!s.dispatched&&HarmonyInstance.LiveHooks==1,"partial installation remains in caller field before any native dispatch");
  Check(!(bool)s.probe.Capture()["complete"],"partial observer cannot claim complete");
  s.Poll();Check(!s.completed&&s.probe!=null&&HarmonyInstance.LiveHooks==1,"unpatch failure retains exact partial observer");
  s.Poll();Check(s.completed&&s.probe.IdentityReleased&&HarmonyInstance.LiveHooks==0,"partial observer disposal retry settles");
  foreach(var flag in new[]{"loading","queued","writer","worker","preparation","owner","foreign"}){
   s=New();s.probe=new Chunk7NativeBootstrapProbe(s.game,s.preset);s.probe.Install();s.StartCleanup();
   if(flag=="loading")LoadingProcess.Instance.IsLoadingInProcess=true;if(flag=="queued")LoadingProcess.Instance.QueuedNames.Add("x");
   if(flag=="writer")NativePersistenceIsolation.HasPendingWrites=true;if(flag=="worker")s.persistence.Verdict=OwnedWorkerTeardownVerdict.Refused;
   if(flag=="preparation")s.world.UnifiedTurn.HasUnsettledPreparation=true;if(flag=="owner")s.world.Registry.Owners=new[]{new object()};if(flag=="foreign")s.game.Player=new Player();
   s.Poll();Check(!s.completed&&HarmonyInstance.UnpatchCalls==0,"cleanup retains observer before "+flag+" boundary");
   LoadingProcess.Instance.IsLoadingInProcess=false;LoadingProcess.Instance.QueuedNames.Clear();NativePersistenceIsolation.HasPendingWrites=false;s.persistence.Verdict=OwnedWorkerTeardownVerdict.Settled;s.world.UnifiedTurn.HasUnsettledPreparation=false;s.world.Registry.Owners=new object[0];s.game.Player=s.player;s.Poll();Check(s.completed,"cleanup retries exact settled "+flag+" boundary");
  }
  foreach(bool prior in new[]{false,true}){s=New();NativePersistenceIsolation.NativeSlotRotationEnabled=prior;s.Bind(new PersistenceSaveAuthorization());s.callbackReturned=true;s.StartCleanup();s.Poll();Check(s.completed&&NativePersistenceIsolation.NativeSlotRotationEnabled==prior,"slot restoration preserves prior "+prior);}
  s=New();s.probe=new Chunk7NativeBootstrapProbe(s.game,s.preset);s.probe.Install();s.StartCleanup();RuntimeLoaderInventory.WriteFailures=1;s.Poll();Check(!s.completed&&s.probe.IdentityReleased&&RuntimeLoaderInventory.FinalWrites==0,"final writer failure does not mark complete");s.Poll();s.Poll();Check(s.completed&&RuntimeLoaderInventory.FinalWrites==1&&RuntimeLoaderInventory.FinalStatus=="FAIL","final writer retry uses frozen probe evidence and retains failure");
  s=New();Check(s.DrainForHost()&&s.results.Any(x=>x.Name=="C7-bootstrap-interrupted"&&x.Status=="FAIL"),"host interruption records truthful incomplete failure");
  Game g;BlueprintAreaPreset preset;var p=NewProbe(out g,out preset);Check(!p.Complete,"installed observer without native callbacks is incomplete");Enter(p,g,preset);var save=new SaveInfo{GameId=g.Player.GameId};Invoke("SaveBefore",g.SaveManager,save);Invoke("NewGameAfter",g);Invoke("PreparedAfter",g.SaveManager,save);Check(p.Complete&&((JArray)Capture(p)["events"]).Count==5,"exact native callback sequence completes observation");p.Dispose();
  p=NewProbe(out g,out preset);Enter(p,g,preset);save=new SaveInfo{GameId=g.Player.GameId};Invoke("SaveBefore",new SaveManager(),save);Check(!p.Complete&&((JArray)Capture(p)["errors"]).Count==1&&((JArray)Capture(p)["events"]).Count==2,"foreign SaveManager cannot satisfy native request observer");Invoke("NewGameAfter",g);p.Dispose();
  p=NewProbe(out g,out preset);Enter(p,g,preset);save=new SaveInfo{GameId=g.Player.GameId};Invoke("SaveBefore",g.SaveManager,save);Invoke("NewGameAfter",g);Invoke("PreparedAfter",new SaveManager(),save);Check(!p.Complete&&((JArray)Capture(p)["errors"]).Count==1&&((JArray)Capture(p)["events"]).Count==4,"foreign SaveManager cannot satisfy native prepared observer");p.Dispose();
  p=NewProbe(out g,out preset);Enter(p,g,preset);Check(Throws(p.Dispose)&&!p.IdentityReleased,"same-frame synchronous native callback retains observer");Time.frameCount++;p.Dispose();Check(p.IdentityReleased,"exception-ended prior-frame native callback can settle without forced engine exit");
  p=NewProbe(out g,out preset);int patchCount=HarmonyInstance.Patches;p.Install();Check(HarmonyInstance.Patches==patchCount,"successful probe installation is idempotent");p.Dispose();bool refused=false;try{p.Install();}catch(ObjectDisposedException){refused=true;}Check(refused,"disposed probe cannot reacquire");
  Console.WriteLine("NATIVE BOOTSTRAP ACTUAL METHODS PASS="+pass+" FAIL="+fail+"; native metadata/Harmony, filesystem writer and game boundaries are substitutes; no game or archive access.");
  return fail==0?0:1;
 }
}
}
namespace UnityEngine { static class Time { public static int frameCount; } }
