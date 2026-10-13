using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using KingmakerMountedCombat.Diagnostics;

// Actual host methods and DTO are inserted unchanged. Native engine state,
// family classification, OS/result publication and metadata providers are facades.
// No game assembly, runtime harness, archive or native API is loaded or invoked.
class Main { }
class UnityModManager { }
static class Application { internal static int QuitCount; public static void Quit(){QuitCount++;} }
namespace Kingmaker { enum Mode { Default, Dialog, Cutscene, None } sealed class Game { public static Game Instance; public object CurrentlyLoadedArea; public Mode CurrentMode; public SaveManager SaveManager=new SaveManager(); } sealed class SaveManager { public string SavePath="private-external-save-root"; } static class GameVersion { public static string GetVersion(){return "external-native-version";} } }
namespace KingmakerMountedCombat.Domain { enum CleanupTrigger { Exception } enum RelationshipState { Unmounted, Mounted } }
namespace KingmakerMountedCombat.Diagnostics
{
    sealed class RuntimeFixtureIdentity { }
    sealed class RuntimeRequest { public const int SaveBackedSchemaVersion=2; public string RunId="synthetic",Scenario="chunk7-native-main-bootstrap",Branch="private",Commit="source",ProductVersion="test",TransactionToken="owned"; public RuntimeFixtureIdentity Fixture=new RuntimeFixtureIdentity(); public static bool IsMissionScenario(string x){return false;} }
    sealed class RuntimeSubscenarioResult { public string Name,Status;public int AssertionPassCount,AssertionFailCount;public IReadOnlyList<string> Errors; }
    sealed class RuntimeSaveAuthorization { public int BaselineLoadRequestCount,AuthorizedLoadCount,AuthorizedWriteCount=1,SuppressedWorkingWriteCount,UnauthorizedLoadCount,UnauthorizedWriteCount; public IDisposable Activate(RuntimeFixtureIdentity f,string p,bool w){return new Lease();} sealed class Lease:IDisposable{public void Dispose(){}} }
    sealed class WorkingFixtureLoader { public WorkingFixtureLoader(RuntimeRequest r,Log l){} }
    sealed class PersistenceIsolationBootstrap { public const string Scenario="persistence-isolation"; public static bool Supports(string s){return s.StartsWith("chunk7-",StringComparison.Ordinal)||s==Scenario;} public PersistenceIsolationBootstrap(RuntimeRequest r){} }
    sealed class Log { public int Exceptions; public void Info(string x){} public void Exception(string x,Exception e){Exceptions++;} }
    sealed class CleanupResult { public bool Succeeded=true,MovementAuthorityResidual,PresentationResidual; }
    sealed class Relationship { public Domain.RelationshipState State=Domain.RelationshipState.Unmounted; public int Dismounts;public bool Refuse;public CleanupResult Dismount(Domain.CleanupTrigger t){Dismounts++;return new CleanupResult{Succeeded=!Refuse};} }
    sealed class Host
    {
        private RuntimeRequest request=new RuntimeRequest();private DiagnosticSettings diagnosticSettings=new DiagnosticSettings();
        private RuntimeSaveAuthorization saveAuthorization=new RuntimeSaveAuthorization();private Relationship relationship=new Relationship();
        private Log logger=new Log();private IDisposable saveAuthorizationLease;private WorkingFixtureLoader fixtureLoader;private PersistenceIsolationBootstrap persistenceBootstrap;
        private bool IsChunk7NativeFamily=true,completionStarted,completed,fixtureIdentityVerified;
        private static bool loading;private readonly Chunk7HostCompletionGate chunk7Completion=new Chunk7HostCompletionGate();
        private string chunk7CompletionStatus;private IReadOnlyList<string> chunk7CompletionErrors;private Stopwatch runtimeClock=Stopwatch.StartNew();
        private IReadOnlyList<RuntimeSubscenarioResult> subscenarioResults;
        private string loadedModId="synthetic",resultPath="never-written-result.json";private DateTimeOffset startedAt=DateTimeOffset.UtcNow;
        private int saveRequestCount=1,loadRequestCount,frameCount=1;private double elapsedSeconds=1;
        private Func<bool> movementExperimentProvider;private Func<string> relationshipStateProvider;
        private RuntimeGameResultV2 result;private int finalizations,drains,publishes,providerReads,emergencyWrites;
        private bool finalizeSawEnabled,publishSawEnabled,throwFinalization;private Action afterManifest;
        private void FinalizeActiveScenarioEngines(List<string> errors){finalizations++;finalizeSawEnabled=diagnosticSettings.EnableUnsafeMovementExperiment;if(throwFinalization)throw new InvalidOperationException("unsettled external owner");}
        private string PublishRuntimeArtifactManifest(){publishes++;publishSawEnabled=diagnosticSettings.EnableUnsafeMovementExperiment;afterManifest?.Invoke();return new string('a',64);}
        private static string ComputeSha256(string p){return new string('b',64);}
        private void WriteJsonAtomic(string p,RuntimeGameResultV2 value){result=value;}
        private void TryWriteEmergencySaveBackedFailure(Exception e){emergencyWrites++;}
        private static bool IsLoadingProcessActive(){return loading;}
        private void DrainChunk7Completion(){drains++;}
__METHODS__
__RESULT_DTO__
        private static int passed,failed;
        private static void Check(string name,bool ok){if(ok){passed++;Console.WriteLine("PASS "+name);}else{failed++;Console.WriteLine("FAIL "+name);}}
        private static Host Ready(bool native=true,Kingmaker.Mode mode=Kingmaker.Mode.Default,bool area=true)
        {
            loading=false;Application.QuitCount=0;Kingmaker.Game.Instance=new Kingmaker.Game{CurrentMode=Kingmaker.Mode.Default};
            var h=new Host{IsChunk7NativeFamily=native};h.request.Scenario=native?"chunk7-native-main-bootstrap":"legacy-save";
            h.movementExperimentProvider=()=>{h.providerReads++;return h.diagnosticSettings.EnableUnsafeMovementExperiment;};h.relationshipStateProvider=()=>h.relationship.State.ToString();
            h.subscenarioResults=new[]{new RuntimeSubscenarioResult{Name="first-native-proof",Status="PASS",AssertionPassCount=2,Errors=new string[0]},new RuntimeSubscenarioResult{Name="last-native-proof",Status="PASS",AssertionPassCount=3,Errors=new string[0]}};
            h.ActivateSaveBackedBoundary();Kingmaker.Game.Instance.CurrentMode=mode;Kingmaker.Game.Instance.CurrentlyLoadedArea=area?new object():null;
            return h;
        }
        private void Settle(){chunk7Completion.Request(0);chunk7Completion.Poll(0,false,()=>true,()=>true,()=>true,()=>true,x=>{throw new Exception(x);},()=>true);}
        internal static int Run()
        {
            var h=Ready();Check("actual activation enables diagnostic movement",h.diagnosticSettings.EnableUnsafeMovementExperiment);
            h.CompleteSaveBackedWhenIdle("PASS",new string[0]);
            Check("pending native custody keeps movement available",h.diagnosticSettings.EnableUnsafeMovementExperiment);
            Check("pending custody does not finalize or publish",h.finalizations==0&&h.result==null&&h.publishes==0);
            Check("pending custody remains incomplete without quit",!h.completed&&!h.completionStarted&&Application.QuitCount==0);
            Check("pending request reaches retained drain",h.chunk7Completion.Pending&&h.drains==1);
            h.Settle();h.CompleteSaveBackedWhenIdle("PASS",new string[0]);
            Check("finalization observes pre-shutdown setting",h.finalizeSawEnabled);
            Check("settled native completion disables diagnostic movement",!h.diagnosticSettings.EnableUnsafeMovementExperiment);
            Check("native result observes disabled diagnostic movement",h.result!=null&&!h.result.MovementExperimentEnabled);
            Check("publication occurs after diagnostic shutdown",!h.publishSawEnabled);
            Check("native success retains complete Default world",h.result.Status=="PASS"&&h.result.CurrentGameMode=="Default"&&h.result.LoadedAreaPresent);
            Check("native success preserves named counts",h.result.SubscenarioTotal==2&&h.result.SubscenarioPassCount==2&&h.result.SubscenarioFailCount==0&&h.result.AssertionPassCount==5&&h.result.AssertionFailCount==0);
            Check("native cleanup skips legacy dismount",h.relationship.Dismounts==0);
            Check("settled result and quit happen once",h.publishes==1&&h.completed&&Application.QuitCount==1);
            h.CompleteSaveBackedWhenIdle("PASS",new string[0]);Check("repeat completion cannot republish",h.publishes==1&&Application.QuitCount==1);

            foreach(var mode in new[]{Kingmaker.Mode.Dialog,Kingmaker.Mode.Cutscene,Kingmaker.Mode.None})
            {
                h=Ready(true,mode);h.Settle();h.CompleteSaveBackedWhenIdle("PASS",new string[0]);
                Check("terminal "+mode+" refuses PASS",h.result.Status=="FAIL");
                Check("terminal "+mode+" stays native and is recorded",Kingmaker.Game.Instance.CurrentMode==mode&&h.result.CurrentGameMode==mode.ToString());
                Check("terminal "+mode+" reports a concrete error",h.result.Errors.Any(x=>x.Contains("currentGameMode="+mode)));
                Check("terminal "+mode+" has failed named totals",h.result.SubscenarioFailCount==1&&h.result.AssertionFailCount>=1&&h.result.SubscenarioResults[1].Status=="PASS");
            }
            h=Ready(true,Kingmaker.Mode.Default,false);h.Settle();h.CompleteSaveBackedWhenIdle("PASS",new string[0]);
            Check("missing loaded area refuses native PASS",h.result.Status=="FAIL"&&!h.result.LoadedAreaPresent);
            Check("missing area is not manufactured",Kingmaker.Game.Instance.CurrentlyLoadedArea==null);
            h=Ready();h.Settle();Kingmaker.Game.Instance=null;h.CompleteSaveBackedWhenIdle("PASS",new string[0]);
            Check("missing game refuses native PASS",h.result.Status=="FAIL"&&!h.result.LoadedAreaPresent&&h.result.CurrentGameMode==null);
            h=Ready();h.movementExperimentProvider=()=>{h.providerReads++;return true;};h.Settle();h.CompleteSaveBackedWhenIdle("PASS",new string[0]);
            Check("provider still enabled refuses native PASS",h.result.Status=="FAIL"&&h.result.MovementExperimentEnabled);
            Check("provider observation is read once",h.providerReads==1);
            Check("owned setting still shut down on provider mismatch",!h.diagnosticSettings.EnableUnsafeMovementExperiment);
            h=Ready();h.Settle();h.afterManifest=()=>{Kingmaker.Game.Instance.CurrentMode=Kingmaker.Mode.Dialog;};h.CompleteSaveBackedWhenIdle("PASS",new string[0]);
            Check("terminal observation follows artifact publication and preserves changed mode",h.result.Status=="FAIL"&&h.result.CurrentGameMode=="Dialog"&&Kingmaker.Game.Instance.CurrentMode==Kingmaker.Mode.Dialog);

            h=Ready();loading=true;var threw=false;try{h.CompleteSaveBackedWhenIdle("PASS",new string[0]);}catch(InvalidOperationException){threw=true;}
            Check("active native loading refuses finalization",threw&&h.finalizations==0&&h.result==null);
            Check("active loading keeps diagnostic setting and process",h.diagnosticSettings.EnableUnsafeMovementExperiment&&Application.QuitCount==0);loading=false;
            h=Ready();h.throwFinalization=true;threw=false;try{h.CompleteSaveBackedCore("PASS",new string[0]);}catch(InvalidOperationException){threw=true;}
            Check("finalization refusal happens before shutdown",threw&&h.diagnosticSettings.EnableUnsafeMovementExperiment&&h.result==null&&h.publishes==0);

            h=Ready(false);h.CompleteSaveBackedWhenIdle("PASS",new string[0]);
            Check("historical completion still shuts down movement",!h.result.MovementExperimentEnabled&&!h.diagnosticSettings.EnableUnsafeMovementExperiment);
            Check("historical branch retains one legacy dismount",h.relationship.Dismounts==1&&h.result.Status=="PASS");
            h=Ready(false,Kingmaker.Mode.None,false);h.CompleteSaveBackedWhenIdle("PASS",new string[0]);
            Check("historical no-world exception is not widened or reclassified",h.result.Status=="PASS"&&h.result.CurrentGameMode=="None"&&!h.result.LoadedAreaPresent);
            h=Ready(false);h.relationship.Refuse=true;h.CompleteSaveBackedWhenIdle("PASS",new string[0]);
            Check("historical dismount failure stays FAIL",h.result.Status=="FAIL"&&h.result.SubscenarioFailCount==1);
            h=Ready(true,Kingmaker.Mode.Dialog);h.Settle();h.CompleteSaveBackedWhenIdle("FAIL",new[]{"original native failure"});
            Check("original native FAIL and cause remain",h.result.Status=="FAIL"&&h.result.Errors.Contains("original native failure"));
            Check("failure still reports actual terminal mode",h.result.CurrentGameMode=="Dialog"&&!h.result.MovementExperimentEnabled);
            h=Ready();h.Settle();h.subscenarioResults=new RuntimeSubscenarioResult[0];h.CompleteSaveBackedWhenIdle("FAIL",new[]{"no proof"});
            Check("no proof cannot become empty success",h.result.Status=="FAIL"&&h.result.SubscenarioTotal==1&&h.result.SubscenarioFailCount==1&&h.result.AssertionFailCount==1);
            Console.WriteLine("TOTAL PASS="+passed+" FAIL="+failed);return failed==0?0:1;
        }
    }
}
static class Program { public static int Main(){return Host.Run();} }
