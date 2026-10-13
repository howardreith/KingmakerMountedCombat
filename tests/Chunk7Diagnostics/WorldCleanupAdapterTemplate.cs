using System;using System.Collections.Generic;using System.Linq;using KingmakerMountedCombat.Diagnostics;
using UnitEntityData=Kingmaker.EntitySystem.Entities.UnitEntityData;using Actor=Kingmaker.EntitySystem.Entities.UnitEntityData;using MountedPairServices=Pair;
enum RelationshipState{Unmounted,Mounted}enum CleanupTrigger{Exception}
class JObject:Dictionary<string,object>{public new object this[string key]{get{object value;return TryGetValue(key,out value)?value:null;}set{base[key]=value;}}public static JObject FromObject(object x){return new JObject();}}
class JArray{public JArray(object x){}}
class Lease:IDisposable{public bool Fail;public int Releases;public void Dispose(){Releases++;if(Fail)throw new Exception("lease pending");}}
class MatchedLease:Lease{public bool Complete=true;}
class Clock{public TimeSpan Elapsed=TimeSpan.Zero;}
class Process{public bool IsEnded;}
class Command{public bool IsFinished;public int Interruptions;public void Interrupt(){Interruptions++;IsFinished=true;}}
class UnitUseAbility:Command{public Process ExecutionProcess=new Process();}
namespace Kingmaker.EntitySystem.Entities{class UnitEntityData{public Descriptor Descriptor=new Descriptor();public bool IsInState=true;public View View;public string UniqueId=Guid.NewGuid().ToString(),CharacterName="fixture";public UnitEntityData(){View=new View{Actor=this};}}}
class View{public Actor Actor;}
class Descriptor{public Actor Pet;public Reference Master=new Reference();public Progression Progression=new Progression();}
class Reference{public Actor Value;}
class Progression{public Features Features=new Features();}
class Features{public List<object> Removed=new List<object>();public object Failing;public Action<object> OnRemove;public void RemoveFact(object f){if(ReferenceEquals(Failing,f))throw new Exception("fact pending");Removed.Add(f);OnRemove?.Invoke(f);}}
class Runtime{public bool NoPreparedPairResidue;}
class Result{public bool Succeeded;}
class Relationship{public Runtime Runtime=new Runtime();public RelationshipState State=RelationshipState.Mounted;public object PairOwner=new object();public bool Reject;public int Dismounts;public Result Dismount(CleanupTrigger x){Dismounts++;if(Reject)return new Result();State=RelationshipState.Unmounted;Runtime.NoPreparedPairResidue=true;PairOwner=null;return new Result{Succeeded=true};}}
class Combat{public bool HasActiveCommand,HasActiveGroundMovement,HasChargeOwnership;public int Cancels;public void Cancel(string x){Cancels++;}public bool TryConfigurePairedActivation(bool x){return true;}}
class Pair{public Relationship Relationship=new Relationship();public Combat Combat=new Combat();}
class Unified{public bool HasUnsettledPreparation;}class Registry{public object[] Owners=new object[0];public int ActiveCount;}
class World{public Pair[] Pairs=new[]{new Pair(),new Pair()};public Pair Primary=>Pairs[0];public bool SerializationBusy;public Unified UnifiedTurn=new Unified();public Registry Registry=new Registry();}
class Controls{public bool HasUnsettledRelationshipTransition;public bool ShellPending;public bool HasUnsettledShellFor(Relationship r){return ShellPending;}public object DescribeNativeShellLifecycle(){return null;}}
class Settings{public bool EnableUnsafeMovementExperiment,EnablePairedActivation;}
class Game{public NativeArea CurrentlyLoadedArea=new NativeArea();public static Game Instance=new Game();public bool IsPaused;public Destroyer EntityDestroyer=new Destroyer();public State State=new State();public Player Player=new Player();public TimeController TimeController=new TimeController();}
class TimeController{public TimeSpan GameTime=TimeSpan.Zero;}class LoadingProcess{public static LoadingProcess Instance=new LoadingProcess();public bool IsLoadingInProcess;public string[] QueuedNames=new string[0];}static class NativePersistenceIsolation{public static bool HasPendingWrites;}static class Time{public static int frameCount;}
class Destroyer{public void Tick(){}}class State{public NativeAreaState LoadedAreaState=new NativeAreaState();public List<Actor> Units=new List<Actor>();}class Player{public string GameId="campaign";public Reference MainCharacter=new Reference{Value=new Actor()};public List<Actor> ControllableCharacters=new List<Actor>();}
class SelectionManager{public static SelectionManager Instance=new SelectionManager();public Actor[] SelectedUnits=new Actor[0];public void MultiSelect(IEnumerable<View> v,bool x){SelectedUnits=v.Select(y=>y.Actor).ToArray();}}
static class RuntimeLoaderInventory{public static int Writes;public static void WriteArtifact(object r,string f,string k,string s,object e,object x){Writes++;}}

class RuntimeRequest{public Fixture Fixture=new Fixture();public Working SelectedChunk7Fixture=>Fixture.Working;public object OwnedPairs;public List<string> Validate(){throw new Exception("unexpected owned DTO in external world stub");}}class Fixture{public Working Working=new Working();}class Working{public string GameId="campaign",GameName="fixture",Area="area";}
class NativeArea{public string AssetGuidThreadSafe="area";}class NativeAreaState{public object MainState=new object();}
namespace Kingmaker{class Game:global::Game{}}
class DoorProbe{public Command[] IssuedCommands=new Command[0];public bool Ready,Fail;public int Drains;public bool Drain(){Drains++;if(Fail)throw new Exception("door pending");return Ready;}}
class InheritedFixtureBoundary{public Actor[] Party;public bool Bound=true,Reject,Released;public int Captures,Releases;public void RequirePreserved(){if(Reject||Released)throw new Exception("inherited custody retained");}public object Capture(string phase){RequirePreserved();Captures++;return new JObject();}public void Release(){if(!Released){Released=true;Releases++;}}}
class WorldAdapter{
 InheritedFixtureBoundary inherited;
 // Actual ground-input settlement is exercised by WorldGroundInputAdapterTemplate.
 // This boundary lets the existing full cleanup adapter test its ownership fence.
 bool groundInputSettled=true;
 bool GroundInputCommandsSettled()=>groundInputSettled;
 MatchedLease matched=new MatchedLease();Lease matchedPins=new Lease();bool worldEvidenceWritten,matchedEvidenceWritten,failMatchedWrite;int matchedWrites;
 void WriteMatchedPoolEvidence(){if(matchedEvidenceWritten)return;if(failMatchedWrite)throw new Exception("sidecar write pending");matchedWrites++;matchedEvidenceWritten=true;}
 DoorProbe doorProbe;
 Lease quietWindow;
 Chunk7NativeWorldScope nativeScope;
 bool cleanupStarted,captured=true,completed,disposed,cleanupProven,cleanupPendingReported,originalPaired,originalMovement,originalPause;double stepStarted;Clock clock=new Clock();Lease measurement,mode=new Lease(),ummLease=new Lease(),screenshots;
 List<Command> issuedCommands=new List<Command>{new UnitUseAbility()};World world=new World();Controls controls=new Controls();Actor secondRider=new Actor(),secondMount=new Actor(),firstRider=new Actor(),firstMount=new Actor();Actor[] originalSelection=new Actor[0];object horseFeature=new object(),rankFeature=new object(),request;
 Settings settings=new Settings();JObject evidence=new JObject();List<string> errors=new List<string>();Dictionary<string,int> cleanupAttempts=new Dictionary<string,int>();const string EvidenceFileName="fixture",EvidenceKind="fixture";
 public WorldAdapter(){secondRider.Descriptor.Pet=secondMount;Game.Instance.State.Units.Add(secondMount);firstRider.Descriptor.Pet=firstMount;firstMount.Descriptor.Master.Value=firstRider;var horse=horseFeature;secondRider.Descriptor.Progression.Features.OnRemove=f=>{if(ReferenceEquals(f,horse)){secondMount.IsInState=false;Game.Instance.State.Units.Remove(secondMount);secondRider.Descriptor.Pet=null;}};foreach(var actor in new[]{firstRider,firstMount,secondRider,secondMount})if(!Game.Instance.State.Units.Contains(actor))Game.Instance.State.Units.Add(actor);nativeScope=new Chunk7NativeWorldScope(new RuntimeRequest(),new[]{firstRider,firstMount,secondRider,secondMount});}
 static int Id(object x){return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(x);}static void Require(bool c,string e){if(!c)throw new Exception(e);}void Fail(string n,string e){errors.Add(e);}void Pass(string n){}object CaptureWorld(string x){return null;}IEnumerable<Actor> PartyActors(){return new Actor[0];}object CaptureRig(Actor a){return null;}
 static void VerifyInheritedCleanup(){Game.Instance=new Game();LoadingProcess.Instance=new LoadingProcess();NativePersistenceIsolation.HasPendingWrites=false;var e=new WorldAdapter();((UnitUseAbility)e.issuedCommands[0]).ExecutionProcess.IsEnded=true;var guard=new InheritedFixtureBoundary{Party=new[]{e.firstRider,e.firstMount,e.secondRider,e.secondMount},Reject=true};e.inherited=guard;e.BeginCleanup();Check(!e.DrainForHost()&&!e.completed&&guard.Releases==0&&e.secondRider.Descriptor.Progression.Features.Removed.Count==0,"inherited refusal retains ordinary pets/facts and registry");guard.Reject=false;Check(e.DrainForHost()&&e.completed&&guard.Captures==1&&guard.Releases==1&&e.secondRider.Descriptor.Progression.Features.Removed.Count==0,"inherited retry snapshots before registry release without removing native facts");e.Dispose();Check(guard.Releases==1&&e.secondRider.Descriptor.Progression.Features.Removed.Count==0,"inherited repeated disposal never replays fact cleanup");}
__METHODS__


 static void DeclaredRefuses(string name,Action<WorldAdapter> mutate){Game.Instance=new Game();var e=new WorldAdapter();var destination=new NativeArea{AssetGuidThreadSafe="target-area"};Game.Instance.CurrentlyLoadedArea=destination;Game.Instance.State.LoadedAreaState=new NativeAreaState();mutate(e);bool refused=false;try{e.nativeScope.RequireDeclaredAreaDestination("target-area",destination);}catch{refused=true;}ScopeCheck(refused,"declared destination refuses "+name);}
 static void VerifyDeclaredArea(){
 Game.Instance=new Game();var e=new WorldAdapter();var destination=new NativeArea{AssetGuidThreadSafe="target-area"};Game.Instance.CurrentlyLoadedArea=destination;Game.Instance.State.LoadedAreaState=new NativeAreaState();bool ok=true;try{e.nativeScope.RequireDeclaredAreaDestination("target-area",destination);}catch{ok=false;}ScopeCheck(ok,"exact declared destination permits retained world and actors");
 ok=false;try{e.nativeScope.RequireCurrent();}catch{ok=true;}ScopeCheck(ok,"declared destination check cannot adopt or relax original scope");
 ok=true;try{new Chunk7NativeWorldScope(new RuntimeRequest(),new[]{e.firstRider,e.firstMount,e.secondRider,e.secondMount},"target-area").RequireCurrent();}catch{ok=false;}ScopeCheck(ok,"caller may explicitly pin exact declared destination as fresh scope");
 ok=false;try{new Chunk7NativeWorldScope(new RuntimeRequest(),new[]{e.firstRider});}catch{ok=true;}ScopeCheck(ok,"default constructor still requires exact Working area");
 DeclaredRefuses("new Game",x=>Game.Instance=new Game{CurrentlyLoadedArea=Game.Instance.CurrentlyLoadedArea});
 DeclaredRefuses("new Player",x=>Game.Instance.Player=new Player());
 DeclaredRefuses("new PersistentState",x=>Game.Instance.State=new State());
 DeclaredRefuses("foreign campaign",x=>Game.Instance.Player.GameId="foreign");
 DeclaredRefuses("foreign main character",x=>Game.Instance.Player.MainCharacter.Value.CharacterName="foreign");
 DeclaredRefuses("same GUID different blueprint",x=>Game.Instance.CurrentlyLoadedArea=new NativeArea{AssetGuidThreadSafe="target-area"});
 DeclaredRefuses("changed live actor view",x=>x.firstRider.View=new View{Actor=x.firstRider});
 DeclaredRefuses("same-ID replacement actor",x=>{Game.Instance.State.Units.Remove(x.firstRider);Game.Instance.State.Units.Add(new Actor{UniqueId=x.firstRider.UniqueId});});
 DeclaredRefuses("absent destination scene",x=>Game.Instance.State.LoadedAreaState.MainState=null);
 }

 static int scopeFailures;
 static void ScopeCheck(bool value,string name){if(value){passed++;Console.WriteLine("PASS "+name);}else{scopeFailures++;Console.WriteLine("FAIL "+name);}}
 static void ScopeRefuses(string name,Action<WorldAdapter> mutate){Game.Instance=new Game();LoadingProcess.Instance=new LoadingProcess();NativePersistenceIsolation.HasPendingWrites=false;var e=new WorldAdapter();var screenshot=new Lease();e.screenshots=screenshot;Game.Instance.IsPaused=true;mutate(e);e.BeginCleanup();var before=e.secondRider.Descriptor.Progression.Features.Removed.Count;bool drained=false;try{drained=e.DrainForHost();}catch{}ScopeCheck(!drained&&!e.completed&&!e.disposed&&Game.Instance.IsPaused&&e.issuedCommands.All(c=>c.Interruptions==0)&&e.world.Pairs.All(p=>p.Combat.Cancels==0)&&e.secondRider.Descriptor.Progression.Features.Removed.Count==before&&screenshot.Releases==0&&ReferenceEquals(e.screenshots,screenshot),"captured scope refuses "+name+" before native mutation");}
 static void VerifyWorldScope(){
 ScopeRefuses("new Game",e=>Game.Instance=new Game{IsPaused=true});
 ScopeRefuses("new Player",e=>Game.Instance.Player=new Player());
 ScopeRefuses("changed campaign",e=>Game.Instance.Player.GameId="foreign");
 ScopeRefuses("changed main character",e=>Game.Instance.Player.MainCharacter.Value.CharacterName="foreign");
 ScopeRefuses("changed area GUID",e=>Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe="foreign");
 ScopeRefuses("same GUID new area object",e=>Game.Instance.CurrentlyLoadedArea=new NativeArea());
 ScopeRefuses("new PersistentState",e=>Game.Instance.State=new State());
 ScopeRefuses("same GUID new loaded area state",e=>Game.Instance.State.LoadedAreaState=new NativeAreaState());
 ScopeRefuses("same GUID new main scene state",e=>Game.Instance.State.LoadedAreaState.MainState=new object());
 ScopeRefuses("same actor ID replacement object",e=>{var old=e.firstRider;Game.Instance.State.Units.Remove(old);Game.Instance.State.Units.Add(new Actor{UniqueId=old.UniqueId});});
 ScopeRefuses("changed live view",e=>e.firstRider.View=new View{Actor=e.firstRider});
 ScopeRefuses("changed descriptor",e=>e.firstRider.Descriptor=new Descriptor());
 Game.Instance=new Game();var keep=new WorldAdapter();var removed=keep.firstRider;removed.IsInState=false;removed.View=null;Game.Instance.State.Units.Remove(removed);bool ok=true;try{keep.nativeScope.RequireCurrent();}catch{ok=false;}ScopeCheck(ok,"legitimate removed actor absence remains admissible");
 var pet=new Actor{View=null};Game.Instance.State.Units.Add(pet);keep.nativeScope.Track(pet);pet.View=new View{Actor=pet};ok=true;try{keep.nativeScope.RequireCurrent();}catch{ok=false;}ScopeCheck(ok,"newly materialized first native pet view is pinned");pet.View=new View{Actor=pet};ok=false;try{keep.nativeScope.RequireCurrent();}catch{ok=true;}ScopeCheck(ok,"subsequent new view cannot rebind exact captured actor");
 Game.Instance=new Game();var busy=new WorldAdapter();var capture=new Lease();busy.screenshots=capture;busy.world.SerializationBusy=true;busy.BeginCleanup();busy.DrainForHost();ScopeCheck(capture.Releases==0&&ReferenceEquals(busy.screenshots,capture),"screenshot overlay lease waits for native worker custody");busy.world.SerializationBusy=false;busy.DrainForHost();ScopeCheck(capture.Releases==1&&busy.screenshots==null,"same-world settled worker permits exact screenshot lease release");
 VerifyDeclaredArea();
 if(scopeFailures!=0)throw new Exception("WORLD SCOPE FAIL="+scopeFailures);
 }

 static void VerifyDoorCustody(){
 Game.Instance=new Game();LoadingProcess.Instance=new LoadingProcess();NativePersistenceIsolation.HasPendingWrites=false;
 var e=new WorldAdapter();((UnitUseAbility)e.issuedCommands[0]).ExecutionProcess.IsEnded=true;
 var command=new Command{IsFinished=true};e.doorProbe=new DoorProbe{IssuedCommands=new[]{command}};
 e.BeginCleanup();Check(!e.DrainForHost()&&e.issuedCommands.Contains(command)&&e.world.Pairs.All(p=>p.Relationship.PairOwner==null)&&e.secondRider.Descriptor.Progression.Features.Removed.Count==0,"retained door lease allows independent pair cleanup but fences fact removal");
 e.doorProbe.Fail=true;Check(!e.DrainForHost()&&e.errors.Any(x=>x.Contains("door pending"))&&e.horseFeature!=null,"door restoration exception retains facts and original failure");
 e.doorProbe.Fail=false;e.doorProbe.Ready=true;Check(e.DrainForHost()&&e.completed&&e.horseFeature==null,"same retained door owner can settle before fact removal");
 }

 static void VerifyQuietCustody(){
 Game.Instance=new Game();LoadingProcess.Instance=new LoadingProcess();NativePersistenceIsolation.HasPendingWrites=false;
 var e=new WorldAdapter();var quiet=new Lease{Fail=true};var sample=new Lease();e.quietWindow=quiet;e.measurement=sample;Game.Instance.IsPaused=true;
 e.BeginCleanup();Check(ReferenceEquals(e.quietWindow,quiet)&&e.measurement==null&&sample.Releases==1&&Game.Instance.IsPaused,"quiet restore failure retains exact gate while independent measurement releases");
 Check(!e.DrainForHost()&&e.world.Pairs.All(p=>p.Combat.Cancels==0)&&e.issuedCommands.All(c=>c.Interruptions==0)&&e.horseFeature!=null,"unfinished quiet restoration fences native actor and progression teardown");
 quiet.Fail=false;e.world.SerializationBusy=true;Check(!e.DrainForHost()&&e.quietWindow==null&&Game.Instance.IsPaused&&e.horseFeature!=null,"quiet state restores independently while native worker still fences gameplay cleanup");
 e.world.SerializationBusy=false;((UnitUseAbility)e.issuedCommands[0]).ExecutionProcess.IsEnded=true;
 Check(e.DrainForHost()&&e.completed&&e.errors.Any(x=>x.Contains("lease pending")),"retained quiet gate retries without discarding original failure evidence");
 }
 static void VerifyMatchedCustody(){
 Game.Instance=new Game();LoadingProcess.Instance=new LoadingProcess();NativePersistenceIsolation.HasPendingWrites=false;
 var e=new WorldAdapter();var observer=e.matched;var pins=e.matchedPins;observer.Fail=true;e.measurement=new Lease();Game.Instance.IsPaused=true;
 e.BeginCleanup();Check(e.measurement==null&&e.matchedPins==pins&&pins.Releases==0&&observer.Releases==1,"matched release failure retains exact scope while independent observer releases");
 Check(!e.DrainForHost()&&Game.Instance.IsPaused&&e.world.Pairs.All(p=>p.Combat.Cancels==0)&&e.horseFeature!=null,"unfinished matched observer fences native command and fact cleanup");
 observer.Fail=false;((UnitUseAbility)e.issuedCommands[0]).ExecutionProcess.IsEnded=true;
 Check(e.DrainForHost()&&e.completed&&e.matchedPins==null&&pins.Releases==1&&e.errors.Any(x=>x.Contains("lease pending")),"matched release retries exact owner and preserves original failure");
 Game.Instance=new Game();e=new WorldAdapter();((UnitUseAbility)e.issuedCommands[0]).ExecutionProcess.IsEnded=true;e.failMatchedWrite=true;e.BeginCleanup();var priorWrites=RuntimeLoaderInventory.Writes;
 bool refused=false;try{e.DrainForHost();}catch{refused=true;}
 Check(refused&&!e.completed&&e.worldEvidenceWritten&&RuntimeLoaderInventory.Writes==priorWrites+1&&!e.matchedEvidenceWritten,"sidecar publication failure retains engine after immutable legacy artifact");
 e.Fail("C7-world-runtime","sidecar write pending");e.failMatchedWrite=false;
 Check(e.DrainForHost()&&e.completed&&RuntimeLoaderInventory.Writes==priorWrites+1&&e.matchedWrites==1&&e.errors.Contains("sidecar write pending"),"later failed sidecar settlement cannot rewrite or promote original legacy artifact");
 }
 static void VerifyGroundInputCustody(){
 Game.Instance=new Game();LoadingProcess.Instance=new LoadingProcess();NativePersistenceIsolation.HasPendingWrites=false;
 var e=new WorldAdapter();((UnitUseAbility)e.issuedCommands[0]).ExecutionProcess.IsEnded=true;e.groundInputSettled=false;
 e.BeginCleanup();Check(!e.DrainForHost()&&!e.completed&&e.world.Pairs.All(p=>p.Relationship.PairOwner==null)&&e.horseFeature!=null&&e.secondRider.Descriptor.Progression.Features.Removed.Count==0,"ground input custody permits independent pair drain but fences fact removal");
 bool refused=false;try{e.Dispose();}catch(InvalidOperationException){refused=true;}Check(refused&&!e.disposed&&e.horseFeature!=null,"ground input custody remains owned through Dispose polling");
 e.groundInputSettled=true;Check(e.DrainForHost()&&e.completed&&e.horseFeature==null,"exact native ground input settlement permits existing cleanup completion");
 }
 static int passed;static void Check(bool condition,string name){if(!condition)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
 public static void Verify(){var e=new WorldAdapter();var features=e.secondRider.Descriptor.Progression.Features;var command=(UnitUseAbility)e.issuedCommands[0];
 Game.Instance.IsPaused=true;e.BeginCleanup();Check(features.Removed.Count==0&&command.Interruptions==0&&Game.Instance.IsPaused,"BeginCleanup preserves pause, facts, and live commands");
 e.world.SerializationBusy=true;Check(!e.DrainForHost()&&command.Interruptions==0&&features.Removed.Count==0&&Game.Instance.IsPaused,"native serialization worker fences unpause and command/fact cleanup");e.world.SerializationBusy=false;
 LoadingProcess.Instance.IsLoadingInProcess=true;Check(!e.DrainForHost()&&command.Interruptions==0&&Game.Instance.IsPaused,"native load fences unpause and fixture mutation");LoadingProcess.Instance.IsLoadingInProcess=false;
 LoadingProcess.Instance.QueuedNames=new[]{"queued exact native load"};Check(!e.DrainForHost()&&command.Interruptions==0&&Game.Instance.IsPaused,"queued-only native load retains paused fixture");LoadingProcess.Instance.QueuedNames=new string[0];
 NativePersistenceIsolation.HasPendingWrites=true;Check(!e.DrainForHost()&&command.Interruptions==0&&Game.Instance.IsPaused,"native write queue retains pause and fixture ownership");NativePersistenceIsolation.HasPendingWrites=false;
 Check(!e.DrainForHost()&&command.IsFinished&&!command.ExecutionProcess.IsEnded&&features.Removed.Count==0&&!Game.Instance.IsPaused,"finished shell with live ExecutionProcess retains Horse fact");
 e.clock.Elapsed=TimeSpan.FromSeconds(16);Check(!e.DrainForHost()&&!e.completed&&!e.disposed&&e.errors.Count==1,"cleanup deadline records failure but retains engine");var writes=RuntimeLoaderInventory.Writes;e.DrainForHost();Check(RuntimeLoaderInventory.Writes==writes,"retained custody does not rewrite failure artifact each frame");
 bool refused=false;try{e.Dispose();}catch(InvalidOperationException){refused=true;}Check(refused&&!e.disposed&&features.Removed.Count==0,"actual Dispose refuses asynchronous custody");
 command.ExecutionProcess.IsEnded=true;e.world.Pairs[0].Relationship.State=RelationshipState.Mounted;e.world.Pairs[0].Relationship.PairOwner=new object();e.world.Pairs[0].Relationship.Runtime.NoPreparedPairResidue=false;e.world.Pairs[0].Relationship.Reject=true;e.world.Pairs[1].Relationship.State=RelationshipState.Mounted;e.world.Pairs[1].Relationship.PairOwner=new object();e.world.Pairs[1].Relationship.Runtime.NoPreparedPairResidue=false;
 Check(!e.DrainForHost()&&e.world.Pairs[0].Relationship.PairOwner!=null&&e.world.Pairs[1].Relationship.PairOwner==null&&features.Removed.Count==0,"failed owner retains itself while independent pair releases");
 e.world.Pairs[0].Relationship.Reject=false;features.Failing=e.horseFeature;Check(!e.DrainForHost()&&e.horseFeature!=null&&e.rankFeature==null&&features.Removed.Count==1,"failed Horse fact removal retains exact fact and releases independent rank");
 features.Failing=null;var lease=e.mode;lease.Fail=true;Check(!e.DrainForHost()&&features.Removed.Count==2&&e.mode!=null&&!e.completed,"failed native mode lease remains owned after entity cleanup");
 lease.Fail=false;Check(e.DrainForHost()&&e.completed&&e.cleanupProven&&e.mode==null&&e.ummLease==null,"retry proves physical cleanup without discarding earlier failures");
 Check(e.errors.Count>=3,"physical settlement preserves original failed evidence");e.Dispose();Check(e.disposed,"actual Dispose releases only proven completed custody");var count=features.Removed.Count;e.Dispose();Check(features.Removed.Count==count,"repeated Dispose does not replay fact removal");
 VerifyWorldScope();VerifyInheritedCleanup();
 VerifyDoorCustody();
 VerifyQuietCustody();
 VerifyMatchedCustody();
 VerifyGroundInputCustody();
 Console.WriteLine("CHUNK7 WORLD CLEANUP ADAPTER PASS="+passed+" FAIL=0; actual source methods with external native boundary stubs only");}
}
static class Program{static int Main(){try{WorldAdapter.Verify();return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}}
