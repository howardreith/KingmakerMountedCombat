using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

class Player {public string GameId="campaign";public MainRef MainCharacter=new MainRef();public List<UnitReference> RemoteCompanions=new List<UnitReference>(),PartyCharacters=new List<UnitReference>();}
class MainRef {public Named Value=new Named();}class Named {public string CharacterName="name";}class Area {public string AssetGuidThreadSafe="area";}
class SaveManager {public string SavePath="root";}class Canvas {public Group GroupManager=new Group();}class Ui {public Canvas Canvas=new Canvas();public CharacterBuildController CharacterBuildController=new CharacterBuildController();}
class Game {public static Game Instance;public Player Player=new Player();public SaveManager SaveManager=new SaveManager();public Area CurrentlyLoadedArea=new Area();public Ui UI=new Ui();public bool IsPaused;}
class Request {public Fixture Fixture=new Fixture();}class Fixture {public Working Working=new Working();}class Working {public string GameId="campaign",GameName="name",Area="area";}
class Authority {}class Authorization {public bool IsActive=true;public int AuthorityGeneration=1;public Authority Exact;public bool IsExactPersistenceScope(Authority a)=>ReferenceEquals(a,Exact);}
static class NativePersistenceIsolation {public static Authority Exact;public static bool IsExactAuthority(Authority a)=>ReferenceEquals(a,Exact);public static void DisableNativeSlotRotation(){}}
enum OwnedWorkerTeardownVerdict {Settled,Refused}class Persistence {public OwnedWorkerTeardownVerdict DrainForTeardown(int n)=>OwnedWorkerTeardownVerdict.Settled;}
class CharacterBuildController {public object LevelUpController;public void OnHotKeyEscPressed(){}}
class Driver {public UnitEntityData Actor=new UnitEntityData();public bool TryGetOwnedBuildSession(out CharacterBuildController b){b=null;return false;}}
class Probe {public bool InSynchronousCallback;public JObject Capture()=>new JObject();public void Dispose(){}}
class DialogMessageBoxBase {public enum BoxButton {Yes}public enum BoxType {Dialog}}class DialogMessageBox:DialogMessageBoxBase {public static DialogMessageBox Instance;public bool IsShown;}
class Component {public GameObject gameObject=new GameObject();}class GameObject {public bool activeSelf,activeInHierarchy=true;}
class View {public UnitEntityData Actor;}class UnitEntityData {public bool IsInState=true;public View View;public string UniqueId="actor";}class UnitReference {public UnitEntityData Value;}
class SelectionManager {public static SelectionManager Instance=new SelectionManager();public UnitEntityData[] SelectedUnits=new UnitEntityData[0];public void MultiSelect(IEnumerable<View> v,bool f){SelectedUnits=v.Select(x=>x.Actor).ToArray();}}
class Group:Component {public float Alpha;public bool RefuseClose,ThrowAfterOpen;public int CloseCalls;public void Close(){CloseCalls++;if(!RefuseClose)gameObject.activeSelf=false;}}
class ShowPartySelection {public static int OpenCalls;public void RunAction(){OpenCalls++;var g=Game.Instance.UI.Canvas.GroupManager;g.gameObject.activeSelf=true;g.Alpha=0;if(g.ThrowAfterOpen)throw new InvalidOperationException("native open failed after activation");}}
class Registry {public object[] Owners=new object[0];}class RuntimeState {public bool NoPreparedPairResidue=true;}enum RelationshipState {Unmounted}
class Relationship {public RelationshipState State;public object PairOwner;public RuntimeState Runtime=new RuntimeState();}class Pair {public Relationship Relationship=new Relationship();}
class Unified {public bool HasUnsettledPreparation;}class World {public Registry Registry=new Registry();public Pair[] Pairs=new Pair[0];public Unified UnifiedTurn=new Unified();}class Controls {public bool HasUnsettledRelationshipTransition;}
static class RuntimeLoaderInventory {public static int Writes;public static void WriteArtifact(Request r,string n,string k,string s,JObject e,List<string> errors){Writes++;}}
class Clock {public TimeSpan Elapsed=TimeSpan.FromSeconds(1);}
public class OrdinaryUiLifecycleAdapter
{
 private bool completed,cleanupProven,captured,groupOwned,busy,originalPause;private int retained;
 private Request request=new Request();private Authorization authorization=new Authorization();private Authority authority=new Authority();private int authorityGeneration=1;private string expectedRoot="root";
 private Player nativeWorld;private Persistence persistence=new Persistence();private Probe probe;private Driver driver=new Driver();private Group group;
 private UnitEntityData[] originalSelection=new UnitEntityData[0],originalParty=new UnitEntityData[0];private IDisposable ummLease;
 private string inventoryPath,inventoryHash;private World world=new World();private Controls controls=new Controls();private JObject evidence=new JObject();private List<JObject> choices=new List<JObject>();private List<string> errors=new List<string>();private Clock clock=new Clock();
 private const string EvidenceFileName="final.json",EvidenceKind="ordinary";
 private const bool HasReadiness=/*HAS_READINESS*/;
 private OrdinaryUiLifecycleAdapter(){Game.Instance=new Game();nativeWorld=Game.Instance.Player;group=Game.Instance.UI.Canvas.GroupManager;authorization.Exact=authority;NativePersistenceIsolation.Exact=authority;Game.Instance.Player.RemoteCompanions.Add(new UnitReference{Value=driver.Actor});ShowPartySelection.OpenCalls=RuntimeLoaderInventory.Writes=0;}
 private bool NativeBusy()=>busy;private void Retain(string r){retained++;}private float GroupAlpha()=>group.Alpha;private JObject PartySnapshot()=>new JObject();private JObject AuthoritySnapshot()=>new JObject();private UnitEntityData Exact(string id)=>originalParty.Single(x=>x.UniqueId==id);private string Hash(string path)=>inventoryHash;private void Pass(string row){}private void AssertChoiceAuthority(){}private static void Require(bool v,string m){if(!v)throw new InvalidOperationException(m);}
/*ACTUAL_OPEN*/
/*ACTUAL_ACTIVE*/
/*ACTUAL_READY*/
/*ACTUAL_COMPLETE*/
/*ACTUAL_CLEANUP*/
 private static int passes,failures;
 private static void Check(string name,Func<bool> test){try{if(!test())throw new Exception("predicate false");passes++;Console.WriteLine("PASS "+name);}catch(Exception e){failures++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 private static bool Refused(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
 public static int Verify()
 {
  Check("native open at alpha zero retains owned visible object",()=>{var a=new OrdinaryUiLifecycleAdapter();a.OpenFieldSelection();return a.groupOwned&&a.group.gameObject.activeSelf&&a.group.Alpha==0&&ShowPartySelection.OpenCalls==1;});
  Check("prior inactive selector with retained alpha one may open",()=>{var a=new OrdinaryUiLifecycleAdapter();a.group.Alpha=1;a.OpenFieldSelection();return a.groupOwned&&a.group.gameObject.activeSelf&&ShowPartySelection.OpenCalls==1;});
  Check("pre-existing active alpha-zero selector is never borrowed",()=>{var a=new OrdinaryUiLifecycleAdapter();a.group.gameObject.activeSelf=true;return Refused(a.OpenFieldSelection)&&!a.groupOwned&&ShowPartySelection.OpenCalls==0;});
  Check("pre-existing active alpha-one selector is never borrowed",()=>{var a=new OrdinaryUiLifecycleAdapter();a.group.gameObject.activeSelf=true;a.group.Alpha=1;return Refused(a.OpenFieldSelection)&&!a.groupOwned&&ShowPartySelection.OpenCalls==0;});
  Check("native open exception retains ownership for later cleanup",()=>{var a=new OrdinaryUiLifecycleAdapter();a.group.ThrowAfterOpen=true;return Refused(a.OpenFieldSelection)&&a.groupOwned&&a.group.gameObject.activeSelf&&ShowPartySelection.OpenCalls==1;});
  Check("native Close deactivation settles despite retained alpha one",()=>{var a=new OrdinaryUiLifecycleAdapter();a.groupOwned=true;a.group.gameObject.activeSelf=true;a.group.Alpha=1;a.PollCleanup();return a.completed&&a.cleanupProven&&!a.groupOwned&&!a.group.gameObject.activeSelf&&a.group.CloseCalls==1&&RuntimeLoaderInventory.Writes==1;});
  Check("already inactive owned selector settles without duplicate close",()=>{var a=new OrdinaryUiLifecycleAdapter();a.groupOwned=true;a.group.Alpha=1;a.PollCleanup();return a.completed&&!a.groupOwned&&a.group.CloseCalls==0;});
  Check("native refusal to close retains exact owner",()=>{var a=new OrdinaryUiLifecycleAdapter();a.groupOwned=true;a.group.gameObject.activeSelf=true;a.group.Alpha=1;a.group.RefuseClose=true;a.PollCleanup();return !a.completed&&a.groupOwned&&a.retained==1&&RuntimeLoaderInventory.Writes==0;});
  Check("replaced selector receives no cleanup input",()=>{var a=new OrdinaryUiLifecycleAdapter();a.groupOwned=true;Game.Instance.UI.Canvas.GroupManager=new Group();return Refused(a.PollCleanup)&&a.groupOwned&&a.group.CloseCalls==0&&RuntimeLoaderInventory.Writes==0;});
  Check("busy native worker receives no selector input",()=>{var a=new OrdinaryUiLifecycleAdapter();a.groupOwned=true;a.busy=true;a.PollCleanup();return a.groupOwned&&!a.completed&&a.group.CloseCalls==0&&a.retained==1;});
  if(HasReadiness)
  {
   Check("fade zero and partial frames wait without repeating open",()=>{var a=new OrdinaryUiLifecycleAdapter();a.OpenFieldSelection();var zero=!a.FieldSelectionReady();a.group.Alpha=.5f;return zero&&!a.FieldSelectionReady()&&ShowPartySelection.OpenCalls==1&&a.groupOwned;});
   Check("fully visible native selector admits readiness",()=>{var a=new OrdinaryUiLifecycleAdapter();a.OpenFieldSelection();a.group.Alpha=1;return a.FieldSelectionReady()&&(float)a.evidence["fieldSelection"]["readyAlpha"]==1&&(bool)a.evidence["fieldSelection"]["readyActiveInHierarchy"];});
   Check("inactive parent waits even with full alpha",()=>{var a=new OrdinaryUiLifecycleAdapter();a.OpenFieldSelection();a.group.Alpha=1;a.group.gameObject.activeInHierarchy=false;return !a.FieldSelectionReady()&&ShowPartySelection.OpenCalls==1;});
   Check("unexpected close during opening refuses",()=>{var a=new OrdinaryUiLifecycleAdapter();a.OpenFieldSelection();a.group.gameObject.activeSelf=false;return Refused(()=>a.FieldSelectionReady())&&a.groupOwned;});
   Check("replacement during opening refuses",()=>{var a=new OrdinaryUiLifecycleAdapter();a.OpenFieldSelection();Game.Instance.UI.Canvas.GroupManager=new Group();return Refused(()=>a.FieldSelectionReady())&&a.groupOwned;});
   Check("exact native Go closure releases selector custody",()=>{var a=new OrdinaryUiLifecycleAdapter();a.OpenFieldSelection();a.group.Alpha=1;a.group.gameObject.activeSelf=false;a.CompleteFieldSelection();return !a.groupOwned&&!(bool)a.evidence["fieldSelection"]["closedActiveSelf"];});
   Check("selector replacement after Go retains custody",()=>{var a=new OrdinaryUiLifecycleAdapter();a.OpenFieldSelection();a.group.gameObject.activeSelf=false;Game.Instance.UI.Canvas.GroupManager=new Group();return Refused(a.CompleteFieldSelection)&&a.groupOwned;});
   Check("selector remaining active after Go retains custody",()=>{var a=new OrdinaryUiLifecycleAdapter();a.OpenFieldSelection();return Refused(a.CompleteFieldSelection)&&a.groupOwned;});
   foreach(var alpha in new[]{float.NaN,float.PositiveInfinity,-.1f,1.1f}){var value=alpha;Check("invalid native alpha refuses "+value,()=>{var a=new OrdinaryUiLifecycleAdapter();a.OpenFieldSelection();a.group.Alpha=value;return Refused(()=>a.FieldSelectionReady());});}
  }
  Console.WriteLine("ORDINARY ACTUAL UI LIFECYCLE PASS="+passes+" FAIL="+failures+"; actual OpenFieldSelection/PollCleanup and readiness; native UI boundary models installed activation/fade contract");
  return failures;
 }
}
