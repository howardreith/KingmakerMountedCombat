using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

class JObject : Dictionary<string,object> {}
class Player { public string GameId="campaign"; public MainRef MainCharacter=new MainRef(); }
class MainRef { public Named Value=new Named(); }
class Named { public string CharacterName="name"; }
class Area { public string AssetGuidThreadSafe="area"; }
class SaveManager { public string SavePath="root"; }
class Canvas {public Group GroupManager=new Group();} class Ui { public Canvas Canvas=new Canvas(); public CharacterBuildController CharacterBuildController=new CharacterBuildController(); }
class Game { public static Game Instance; public Player Player=new Player(); public SaveManager SaveManager=new SaveManager(); public Area CurrentlyLoadedArea=new Area();public Ui UI=new Ui(); private bool pause; public int PauseMutations;public bool IsPaused {get{return pause;}set{PauseMutations++;pause=value;}} }
class Request {public Fixture Fixture=new Fixture();}
class Fixture {public Working Working=new Working();}
class Working {public string GameId="campaign",GameName="name",Area="area";}
class Authority {}
class Authorization {public bool IsActive=true;public int AuthorityGeneration=1;public Authority Exact; public bool IsExactPersistenceScope(Authority value){return object.ReferenceEquals(value,Exact);} }
static class NativePersistenceIsolation {public static Authority Exact;public static int RotationMutations;public static bool IsExactAuthority(Authority value){return object.ReferenceEquals(value,Exact);}public static void DisableNativeSlotRotation(){RotationMutations++;}}
enum OwnedWorkerTeardownVerdict { Settled,Refused }
class Persistence {public OwnedWorkerTeardownVerdict Verdict;public OwnedWorkerTeardownVerdict DrainForTeardown(int count){return Verdict;}}
class CharacterBuildController {public object LevelUpController;public void OnHotKeyEscPressed(){Counters.UiMutations++;}}
class Driver {public bool TryGetOwnedBuildSession(out CharacterBuildController value){value=null;return false;}}
class Probe {public bool InSynchronousCallback;public JObject Capture(){return new JObject();}public void Dispose(){Counters.ObserverMutations++;}}
class DialogMessageBoxBase {public enum BoxButton {Yes}public enum BoxType {Dialog}}
class DialogMessageBox : DialogMessageBoxBase {public static DialogMessageBox Instance;public bool IsShown;}
class Component {public GameObject gameObject=new GameObject();}
class GameObject {public bool activeInHierarchy=true;public bool activeSelf;}
class View {public UnitEntityData Actor;}
class UnitEntityData {public bool IsInState=true;public View View;public string UniqueId="actor";}
class SelectionManager {public static SelectionManager Instance=new SelectionManager();public UnitEntityData[] SelectedUnits=new UnitEntityData[0];public void MultiSelect(IEnumerable<View> views,bool flag){Counters.SelectionMutations++;SelectedUnits=views.Select(x=>x.Actor).ToArray();}}
class Group {public GameObject gameObject=new GameObject();public void Close(){Counters.UiMutations++;gameObject.activeSelf=false;}}
class Registry {public object[] Owners=new object[0];}
class RuntimeState {public bool NoPreparedPairResidue=true;}
enum RelationshipState {Unmounted}
class Relationship {public RelationshipState State;public object PairOwner;public RuntimeState Runtime=new RuntimeState();}
class Pair {public Relationship Relationship=new Relationship();}
class Unified {public bool HasUnsettledPreparation;}
class World {public Registry Registry=new Registry();public Pair[] Pairs=new Pair[0];public Unified UnifiedTurn=new Unified();}
class Controls {public bool HasUnsettledRelationshipTransition;}
static class Counters {public static int UiMutations,SelectionMutations,ObserverMutations,Artifacts;public static void Reset(){UiMutations=SelectionMutations=ObserverMutations=Artifacts=0;}}
static class RuntimeLoaderInventory {public static void WriteArtifact(Request r,string name,string kind,string status,JObject e,List<string> errors){Counters.Artifacts++;}}
class Lease:IDisposable {public void Dispose(){Counters.UiMutations++;}}
class Clock {public TimeSpan Elapsed=TimeSpan.FromSeconds(60);}
class ActualCleanupAdapter {
 private bool completed,cleanupProven,captured=true,groupOwned;private bool busy;private bool originalPause=true;private int retained;
 private Request request=new Request();private Authorization authorization=new Authorization();private Authority authority=new Authority();private int authorityGeneration=1;private string expectedRoot="root";
 private Player nativeWorld;private Persistence persistence=new Persistence();private Probe probe;private Driver driver=new Driver();private Group group=new Group();
 private UnitEntityData[] originalSelection=new UnitEntityData[0],originalParty=new UnitEntityData[0];private IDisposable ummLease=new Lease();
 private string inventoryPath,inventoryHash;private World world=new World();private Controls controls=new Controls();private JObject evidence=new JObject();private List<JObject> choices=new List<JObject>();private List<string> errors=new List<string>();private Clock clock=new Clock();
 private const string EvidenceFileName="final.json",EvidenceKind="ordinary";
 ActualCleanupAdapter(){Game.Instance=new Game();nativeWorld=Game.Instance.Player;authorization.Exact=authority;NativePersistenceIsolation.Exact=authority;Counters.Reset();NativePersistenceIsolation.RotationMutations=0;SelectionManager.Instance=new SelectionManager();}
 private bool NativeBusy(){return busy;}private void Retain(string reason){retained++;}private bool GroupActive(){return group!=null&&group.gameObject.activeSelf;}private JObject PartySnapshot(){return new JObject();}private JObject AuthoritySnapshot(){return new JObject();}private UnitEntityData Exact(string id){return originalParty.Single(x=>x.UniqueId==id);}private string Hash(string path){return inventoryHash;}private void Pass(string row){}private static void Require(bool ok,string msg){if(!ok)throw new InvalidOperationException(msg);}
/*ACTUAL_POLL_CLEANUP*/
 private static int passes,failures;
 private static void Check(string name,Action<ActualCleanupAdapter> mutate,bool mustBlock){var a=new ActualCleanupAdapter();mutate(a);Exception error=null;try{a.PollCleanup();}catch(Exception e){error=e;}var mutated=Counters.UiMutations+Counters.SelectionMutations+Counters.ObserverMutations+NativePersistenceIsolation.RotationMutations+Game.Instance.PauseMutations+Counters.Artifacts;var ok=mustBlock?!a.completed&&!a.cleanupProven&&mutated==0&&(error!=null||a.retained>0):a.completed&&a.cleanupProven&&error==null&&Counters.Artifacts==1;if(ok){passes++;Console.WriteLine("PASS "+name);}else{failures++;Console.WriteLine("FAIL "+name+" completed="+a.completed+" proven="+a.cleanupProven+" mutations="+mutated+" retained="+a.retained+" error="+error);}}
 public static int Main(){Check("same captured world can settle",a=>{},false);Check("changed Player retains custody before all native mutations",a=>{Game.Instance.Player=new Player();},true);Check("same Player wrong campaign retains custody",a=>{Game.Instance.Player.GameId="other";},true);Check("same Player wrong campaign name retains custody",a=>{Game.Instance.Player.MainCharacter.Value.CharacterName="other";},true);Check("same Player changed area retains custody",a=>{Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe="other";},true);Check("busy native worker retains custody before identity read",a=>{a.busy=true;Game.Instance.Player=new Player();},true);Check("refused worker drain retains custody",a=>{a.persistence.Verdict=OwnedWorkerTeardownVerdict.Refused;},true);Check("active callback retains observer custody",a=>{a.probe=new Probe{InSynchronousCallback=true};},true);Check("no captured world does not claim UI ownership",a=>{a.captured=false;Game.Instance.Player=new Player();},false);Check("changed authority generation retains custody",a=>{a.authorization.AuthorityGeneration=2;},true);Console.WriteLine("ORDINARY ACTUAL CLEANUP WORLD CONTRACT PASS="+passes+" FAIL="+failures);return failures==0?0:1;}
}
