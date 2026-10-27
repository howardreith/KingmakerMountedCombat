using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
enum OwnedWorkerTeardownVerdict { Settled, Refused }
enum CleanupTrigger { Exception }
class Persistence { public OwnedWorkerTeardownVerdict Verdict; public OwnedWorkerTeardownVerdict DrainForTeardown(int n) => Verdict; }
class Clock { public TimeSpan Elapsed = TimeSpan.FromSeconds(60); }
class View { public Unit Owner; }
class Unit { public bool IsInState=true, IsInCombat; public View View; public Commands Commands=new Commands(); public void LeaveCombat() { Counters.Leave++; IsInCombat=false; } }
class Commands { public bool Empty=true; }
class UnitCommand { public bool IsFinished=true, Throw; public int Interrupts; public void Interrupt() { Interrupts++; if(Throw)throw new Exception("interrupt"); } }
class UnitUseAbility:UnitCommand { public Process ExecutionProcess; }
class Process { public bool IsEnded; }
class SelectionManager { public static SelectionManager Instance; public Unit[] SelectedUnits=new Unit[0]; public void MultiSelect(IEnumerable<View> v,bool e) { Counters.Selection++; SelectedUnits=v.Select(x=>x.Owner).ToArray(); } }
class Player { public bool IsInCombat; }
class Turn { public bool Initialized; }
class Destroyer { public void Tick() { Counters.Entity++; } }
class SaveManager { public string SavePath="isolated"; }
class Game { public static Game Instance; public SaveManager SaveManager=new SaveManager(); public object CurrentlyLoadedArea; public Player Player=new Player(); public Turn TurnBasedCombatController=new Turn(); public Destroyer EntityDestroyer=new Destroyer(); private bool paused; public bool IsPaused {get=>paused;set{Counters.Pause++;paused=value;}} }
class Lease:IDisposable { public bool Throw; public int Attempts; public void Dispose(){Attempts++;Counters.Lease++;if(Throw)throw new Exception("lease");} }
class Field { public bool Owned=true,Ready=true,Throw; public bool TryDrain(){Counters.Field++;if(Throw)throw new Exception("foreign UI");if(Ready)Owned=false;return Ready;} }
class Trace:Lease { public int Captures; public JObject Capture(){Captures++;return new JObject();} }
class Target:Lease { public bool Gone=true,ThrowDestroy; public bool DestroyAndVerify(){Counters.Target++;if(ThrowDestroy)throw new Exception("target");return Gone;} }
class Ai { public bool LastRestoreVerified; public void ReassertAfterNativeReset(Unit[] v){} public void Restore(Unit[] v){Counters.Ai++;LastRestoreVerified=true;} }
class Runtime { public bool NoPreparedPairResidue=true; }
class Result { public bool Succeeded=true; }
class Relationship { public Runtime Runtime=new Runtime(); public object PairOwner; public bool DismountSucceeds=true; public Result Dismount(CleanupTrigger c){Counters.Dismount++;return new Result{Succeeded=DismountSucceeds};} }
class Combat { public bool HasActiveCommand,HasActiveGroundMovement,HasChargeOwnership,Throw; public int Cancels; public void Cancel(string why){Cancels++;if(Throw)throw new Exception("cancel");} }
class Pair { public Relationship Relationship=new Relationship();public Combat Combat=new Combat(); }
class Registry { public object[] Owners=new object[0]; }
class Coordinator { public bool HasUnsettledPreparation; }
class World { public Pair[] Pairs={new Pair(),new Pair()};public Registry Registry=new Registry();public Coordinator UnifiedTurn=new Coordinator(); }
class Controls { public bool HasUnsettledRelationshipTransition,Shell; public bool HasUnsettledShellFor(Relationship r)=>Shell; }
class Request { public object PersistenceLoad=new object(); }
// A cold Manual case observes one load; the owner-seed fixture Manual is checked against its own read-only plan.
class Calibration { public bool UsesManualField=true,UsesColdManualField=true,UsesFixtureManualField; }
class Authority { public int AdmittedNativeLoadRequests,AdmittedNativeWriteRequests; public bool Exact=true; public bool MatchesReadOnlySmallManualPlan(object o)=>Exact; public bool FixtureExact=true; public bool MatchesReadOnlySmallFixturePlan(object o)=>FixtureExact; }
class Authorization { public bool IsActive=true,Exact=true; public long AuthorityGeneration=1; public int AuthorizedLoadCount,AuthorizedWriteCount; public bool IsExactPersistenceScope(Authority a)=>Exact; }
static class NativePersistenceIsolation { public static bool Exact=true;public static bool IsExactAuthority(Authority a)=>Exact; }
namespace UnityEngine { static class Time { public static int frameCount=1; } }
class NativeArchiveRestorationProbe:Lease { public static bool ThrowInstall; public int Captures,Installed; public NativeArchiveRestorationProbe(Persistence p){} public void Install(){Installed++;if(ThrowInstall)throw new Exception("partial native hook");} public JObject Capture(){Captures++;return new JObject{["complete"]=true};} }
class Selection { public string SourceArtifactSha256="source",SourceArchiveSha256="archive"; }
static class Counters {public static int Field,Selection,Pause,Lease,Entity,Leave,Target,Ai,Dismount; public static List<string> Artifacts=new List<string>();public static bool FailWrite;public static void Reset(){Field=Selection=Pause=Lease=Entity=Leave=Target=Ai=Dismount=0;Artifacts.Clear();FailWrite=false;} }
static class RuntimeLoaderInventory { public static void WriteArtifact(Request r,string name,string kind,string status,JObject body,List<string> errors){if(Counters.FailWrite)throw new Exception("writer");if(Counters.Artifacts.Any(x=>x.StartsWith(name+":")))throw new Exception("immutable duplicate leaf");Counters.Artifacts.Add(name+":"+status);} }
public class ActualSmallCleanup {
 const string EvidenceFileName="final",EvidenceKind="small",PendingFileName="pending",PendingKind="pending";
 bool completed,settled,captured=true,pending,busy,refuseGuard,pause=true;double cleanupStart;
 Persistence persistence=new Persistence(); Clock clock=new Clock();Request request=new Request();Selection selection=new Selection();Field field=new Field();
 Unit actor=new Unit(),pet=new Unit();Unit[] originalSelection=new Unit[0];List<UnitCommand> issued=new List<UnitCommand>();World world=new World();Controls controls=new Controls();
 Lease ummLease=new Lease(),mode=new Lease();Trace trace=new Trace();Target targetService=new Target();Ai pairAi=new Ai();
 string sourcePath="source",archivePath="archive"; JObject evidence=new JObject();List<string> errors=new List<string>();
 string SourceArtifactHash=>selection.SourceArtifactSha256; string SourceArchiveHash=>selection.SourceArchiveSha256;
/*MANUAL_RELEASE*/
/*MANUAL_BEGIN*/
 // Native item presentation fixture and private Small calibration admission (released after pair cleanup).
 object presentationTrace;bool ReleasePresentationFixture()=>true;Lease privateSmallCalibrationLease;
 Calibration calibration=new Calibration(); Authority authority=new Authority(); Authorization authorization=new Authorization(); long generation=1; string root="isolated"; bool manualObservationBegun,sourceReadThrows; Game manualGame; SaveManager manualManager; NativeArchiveRestorationProbe manualRestoration;
 void ReadManualSource(){if(sourceReadThrows)throw new Exception("source");}
 Unit[] ControlledActors=>new[]{actor,pet};
 ActualSmallCleanup(){Counters.Reset();NativeArchiveRestorationProbe.ThrowInstall=false;NativePersistenceIsolation.Exact=true;Game.Instance=new Game();SelectionManager.Instance=new SelectionManager();}
 bool Busy()=>busy;void AssertAuthority(){if(refuseGuard)throw new Exception("world changed");}void AssertUnchangedProgression(){}JObject AuthoritySnapshot()=>new JObject();static string Hash(string p)=>p;
 void Pass(string row){} void Fail(string row,string error){if(!errors.Contains(error))errors.Add(error);}static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
/*SETTLED*/
/*RELEASE*/
/*END*/
/*POLL*/
/*RETAIN*/
 static int passes,failures;
 static void Check(string name,Func<ActualSmallCleanup,bool> test){var a=new ActualSmallCleanup();bool ok=false;Exception ex=null;try{ok=test(a);}catch(Exception e){ex=e;}if(ok){passes++;Console.WriteLine("PASS "+name);}else{failures++;Console.WriteLine("FAIL "+name+" "+ex);}}
 static bool NoActorMutation()=>Counters.Target==0&&Counters.Leave==0&&Counters.Entity==0&&Counters.Dismount==0&&Counters.Ai==0&&Counters.Selection==0;
 public static int Verify(){
 Check("settled native ownership completes",a=>{a.PollCleanup();return a.completed&&a.settled&&Counters.Artifacts.SequenceEqual(new[]{"final:PASS"});});
 Check("busy worker precedes pause UI and actors",a=>{a.busy=true;a.PollCleanup();return !a.completed&&Counters.Pause==0&&Counters.Field==0&&NoActorMutation();});
 Check("refused worker teardown retains custody",a=>{a.persistence.Verdict=OwnedWorkerTeardownVerdict.Refused;a.PollCleanup();return !a.completed&&Counters.Pause==0&&Counters.Field==0&&NoActorMutation();});
 Check("changed world blocks all native cleanup",a=>{a.refuseGuard=true;a.PollCleanup();return !a.completed&&Counters.Pause==0&&Counters.Field==0&&NoActorMutation();});
 Check("pending native selector blocks actor work",a=>{a.field.Ready=false;a.PollCleanup();return !a.completed&&a.field.Owned&&Counters.Pause==0&&NoActorMutation();});
 Check("replaced selector retains UI custody",a=>{a.field.Throw=true;a.PollCleanup();return !a.completed&&a.field.Owned&&NoActorMutation();});
 Check("terminal shell with live process blocks target",a=>{a.issued.Add(new UnitUseAbility{IsFinished=true,ExecutionProcess=new Process()});a.PollCleanup();return !a.completed&&NoActorMutation();});
 Check("unfinished detached issued command blocks target",a=>{a.issued.Add(new UnitCommand{IsFinished=false});a.PollCleanup();return !a.completed&&NoActorMutation();});
 Check("current native queue blocks target",a=>{a.actor.Commands.Empty=false;a.PollCleanup();return !a.completed&&NoActorMutation();});
 Check("retained native shell blocks target",a=>{a.controls.Shell=true;a.PollCleanup();return !a.completed&&NoActorMutation();});
 Check("preparation blocks target",a=>{a.world.UnifiedTurn.HasUnsettledPreparation=true;a.PollCleanup();return !a.completed&&NoActorMutation();});
 Check("relationship transition blocks target",a=>{a.controls.HasUnsettledRelationshipTransition=true;a.PollCleanup();return !a.completed&&NoActorMutation();});
 Check("one interrupt failure does not skip independent command",a=>{var x=new UnitCommand{IsFinished=false,Throw=true};var y=new UnitCommand{IsFinished=false};a.issued.Add(x);a.issued.Add(y);a.PollCleanup();return !a.completed&&x.Interrupts==1&&y.Interrupts==1&&NoActorMutation();});
 Check("one pair cancel failure does not skip next pair",a=>{a.world.Pairs[0].Combat.Throw=true;a.PollCleanup();return !a.completed&&a.world.Pairs.All(x=>x.Combat.Cancels==1)&&NoActorMutation();});
 Check("pending target retains observers and mode",a=>{a.targetService.Gone=false;a.PollCleanup();return !a.completed&&a.trace!=null&&a.mode!=null&&Counters.Selection==0;});
 Check("observer failure retains exact instance and frozen evidence",a=>{var t=a.trace;t.Throw=true;a.PollCleanup();if(a.completed||!ReferenceEquals(a.trace,t)||t.Captures!=1)return false;t.Throw=false;a.PollCleanup();return a.completed&&t.Captures==1&&t.Attempts==2&&Counters.Artifacts.SequenceEqual(new[]{"pending:FAIL","final:FAIL"});});
 Check("mode failure retains exact instance",a=>{var m=a.mode;m.Throw=true;a.PollCleanup();if(a.completed||!ReferenceEquals(a.mode,m))return false;m.Throw=false;a.PollCleanup();return a.completed&&m.Attempts==2;});
 Check("prepared view residue blocks observer release",a=>{a.world.Pairs[0].Relationship.Runtime.NoPreparedPairResidue=false;a.PollCleanup();return !a.completed&&a.trace!=null&&a.mode!=null;});
 Check("owner residue blocks observer release",a=>{a.world.Registry.Owners=new[]{new object()};a.PollCleanup();return !a.completed&&a.trace!=null&&a.mode!=null;});
 Check("timeout writes pending once then distinct immutable final FAIL",a=>{a.busy=true;a.PollCleanup();a.PollCleanup();a.busy=false;a.PollCleanup();return a.completed&&Counters.Artifacts.SequenceEqual(new[]{"pending:FAIL","final:FAIL"});});
 Check("failed final writer leaves completed false and supports retry",a=>{Counters.FailWrite=true;try{a.PollCleanup();}catch(Exception e){if(e.Message!="writer")throw;}if(a.completed)return false;Counters.FailWrite=false;a.PollCleanup();return a.completed&&Counters.Artifacts.SequenceEqual(new[]{"final:FAIL"});});
 Check("uncaptured refusal has no UI or actor mutation",a=>{a.captured=false;a.refuseGuard=true;a.PollCleanup();return a.completed&&Counters.Pause==0&&Counters.Field==0&&NoActorMutation()&&Counters.Artifacts.SequenceEqual(new[]{"final:FAIL"});});

 Check("before-load observer is owned through partial install failure",a=>{a.captured=false;NativeArchiveRestorationProbe.ThrowInstall=true;try{a.BeginBeforeLoad();}catch{}var observer=a.manualRestoration;if(observer==null||observer.Installed!=1)return false;a.PollCleanup();return a.completed&&observer.Attempts==1&&a.manualRestoration==null&&NoActorMutation()&&Counters.Pause==0;});
 Check("pre-observer source refusal has no borrowed lease",a=>{a.captured=false;a.sourceReadThrows=true;try{a.BeginBeforeLoad();}catch{}a.PollCleanup();return a.completed&&a.manualRestoration==null&&NoActorMutation()&&Counters.Lease==0;});
 Check("cold observer failed release retains field and frozen capture",a=>{a.captured=false;a.BeginBeforeLoad();var observer=a.manualRestoration;observer.Throw=true;a.PollCleanup();if(a.completed||!ReferenceEquals(observer,a.manualRestoration)||observer.Captures!=1)return false;observer.Throw=false;a.PollCleanup();return a.completed&&observer.Attempts==2&&observer.Captures==1&&a.manualRestoration==null&&NoActorMutation();});
 Check("cold worker fence precedes observer unpatch",a=>{a.captured=false;a.BeginBeforeLoad();var observer=a.manualRestoration;a.busy=true;a.PollCleanup();return !a.completed&&observer.Attempts==0&&ReferenceEquals(observer,a.manualRestoration)&&NoActorMutation();});
 Check("foreign Game retains cold observer",a=>{a.captured=false;a.BeginBeforeLoad();var observer=a.manualRestoration;Game.Instance=new Game();a.PollCleanup();return !a.completed&&observer.Attempts==0&&NoActorMutation()&&Counters.Pause==0;});
 Check("foreign manager retains cold observer",a=>{a.captured=false;a.BeginBeforeLoad();var observer=a.manualRestoration;Game.Instance.SaveManager=new SaveManager();a.PollCleanup();return !a.completed&&observer.Attempts==0&&NoActorMutation();});
 Check("changed authority generation retains cold observer",a=>{a.captured=false;a.BeginBeforeLoad();var observer=a.manualRestoration;a.authorization.AuthorityGeneration++;a.PollCleanup();return !a.completed&&observer.Attempts==0&&NoActorMutation();});
 Check("foreign root retains cold observer",a=>{a.captured=false;a.BeginBeforeLoad();var observer=a.manualRestoration;Game.Instance.SaveManager.SavePath="foreign";a.PollCleanup();return !a.completed&&observer.Attempts==0&&NoActorMutation();});
 Check("repeated before-load admission never installs twice",a=>{a.captured=false;a.BeginBeforeLoad();var observer=a.manualRestoration;bool refused=false;try{a.BeginBeforeLoad();}catch{refused=true;}return refused&&ReferenceEquals(observer,a.manualRestoration)&&observer.Installed==1;});
 Check("existing native world refuses observer acquisition",a=>{Game.Instance.CurrentlyLoadedArea=new object();bool refused=false;try{a.BeginBeforeLoad();}catch{refused=true;}return refused&&a.manualRestoration==null&&!a.manualObservationBegun;});
 Check("fixture Manual admission uses its own read-only fixture plan",a=>{a.calibration.UsesFixtureManualField=true;a.authority.Exact=false;a.BeginBeforeLoad();return a.manualRestoration!=null&&a.manualObservationBegun;});
 Check("fixture Manual admission refuses a foreign fixture plan",a=>{a.calibration.UsesFixtureManualField=true;a.authority.FixtureExact=false;bool refused=false;try{a.BeginBeforeLoad();}catch{refused=true;}return refused&&a.manualRestoration==null;});
 Check("non-cold calibration never observes a load",a=>{a.calibration.UsesColdManualField=false;bool refused=false;try{a.BeginBeforeLoad();}catch{refused=true;}return refused&&a.manualRestoration==null&&!a.manualObservationBegun;});
 Check("spent load refuses observer acquisition",a=>{a.authority.AdmittedNativeLoadRequests=1;bool refused=false;try{a.BeginBeforeLoad();}catch{refused=true;}return refused&&a.manualRestoration==null;});
 Console.WriteLine("ACTUAL SMALL CLEANUP PASS "+passes+" FAIL "+failures);return failures;
 }
}