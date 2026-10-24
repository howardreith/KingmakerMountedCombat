// Exact production members are inserted by the focused script. All native roster,
// fact/progression, frame/resource, logging and managed-stack reads are inert facades.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using KingmakerMountedCombat.Diagnostics;

namespace System.Diagnostics {
 internal sealed class StackTrace {
  internal static int Created; internal static bool Fail;
  public StackTrace(int skip, bool lines) { Created++; if (Fail) throw new InvalidOperationException("stack-read"); }
  public StackFrame[] GetFrames() => new[] { new StackFrame() };
 }
 internal sealed class StackFrame { public MethodBase GetMethod() => typeof(Program).GetMethod("NativeCaller"); }
}
internal static class PollTrace {
 internal static bool Enabled; internal static readonly List<string> Rows=new List<string>();
 internal static void Note(string value){if(Enabled)Rows.Add(value);}
 internal static string Capture(Action action){Rows.Clear();Enabled=true;try{action();return string.Join("|",Rows);}finally{Enabled=false;}}
}
internal sealed class CountingCulture:System.Globalization.CultureInfo {
 internal int FormatReads;
 internal CountingCulture():base("en-US"){}
 public override System.Globalization.NumberFormatInfo NumberFormat { get {FormatReads++;return base.NumberFormat;} set {base.NumberFormat=value;} }
}
internal static class Reads { internal static int Native; internal static bool Fail; internal static void Touch() { Native++; if(Fail) throw new InvalidOperationException("native-read"); } }
internal static class Time { internal static int frameCount { get { Reads.Touch(); return 41; } } }
internal sealed class Logger {
 internal readonly List<string> InfoRows=new List<string>(); internal readonly List<string> Errors=new List<string>();
 internal bool FailInfo;
 public void Info(string text) { if(FailInfo) throw new InvalidOperationException("info-read"); InfoRows.Add(text); }
 public void Error(string text) { Errors.Add(text); }
 public void Exception(string text,Exception error) { Errors.Add(text+":"+error.Message); }
}
namespace Kingmaker {
 internal sealed class Game {
  internal static Game Current; internal static Game Instance { get { Reads.Touch(); return Current; } }
  internal Player Player; internal Controller TurnBasedCombatController=new Controller();
  internal string CurrentMode="Default"; internal bool IsPaused;
 }
 internal sealed class Player { internal IEnumerable<UnitReference> PartyCharacters; }
 internal sealed class Controller { internal Turn CurrentTurn=new Turn(); }
 internal sealed class Turn { internal Unit Unit=new Unit {UniqueId="turn"}; internal string Status="Preparing"; }
}
internal sealed class UnitReference { internal Unit Value; }
internal sealed class Blueprint { private string assetGuid="horse-guid"; internal string AssetGuid {get{PollTrace.Note("Blueprint:"+assetGuid);return assetGuid;}set{assetGuid=value;}} }
internal sealed class MasterReference { private Unit value; internal Unit Value {get{PollTrace.Note("Master");return value;}set{this.value=value;}} }
internal sealed class Cooldown { internal double StandardAction=3,MoveAction=2; }
internal sealed class CombatState { internal Cooldown Cooldown=new Cooldown(); }
internal sealed class Unit {
 private string uniqueId; internal string UniqueId {get{PollTrace.Note("ID:"+uniqueId);return uniqueId;}set{uniqueId=value;}} internal Descriptor Descriptor=new Descriptor(); internal Blueprint Blueprint=new Blueprint(); internal CombatState CombatState=new CombatState();
}
internal sealed class Descriptor {
 internal Fact Fact; private Unit pet; internal Unit Pet {get{PollTrace.Note("Pet");return pet;}set{pet=value;}} internal MasterReference Master=new MasterReference();
 internal Fact GetFact(object blueprint) {PollTrace.Note("GetFact");return Fact;}
}
internal sealed class Fact {
 internal int Rank=4; internal HorseCompanionAddPet AddPet;
 internal int GetRank() {PollTrace.Note("Rank");return Rank;}
 internal T Get<T>() where T:class {PollTrace.Note("AddPet");return AddPet as T;}
}
internal sealed class HorseCompanionAddPet {
 internal bool DeferredProgressionPending,FailureReported,ThrowRetry,FailAfterRetry;
 internal int Calls,ExpectedCharacterLevel=4,DeferredCharacterLevelAfter=4;
 internal void TryDeferredProgressionSynchronization() { Calls++; DeferredProgressionPending=false; if(ThrowRetry)throw new InvalidOperationException("retry"); }
 internal bool TryMarkDeferredProgressionFailureReported() { if(!FailAfterRetry||FailureReported)return false; FailureReported=true; return true; }
}
internal enum HorseCompanionBlueprintState { Pending,Registered,Faulted,Disposed }
internal sealed class HorseCompanionBlueprintService {
 internal readonly Logger logger=new Logger();
 __OBSERVATION_VALUE__
 __CACHE_FIELDS__
 private object horseFeature=new object(),rangerSelection=new object(),selectionLease=new object();
 private HorseCompanionBlueprintState state=HorseCompanionBlueprintState.Registered;
 private bool disposed;
 internal bool RestoreSucceeds=true; internal int RestoreCalls;
 private bool TryRestoreSelection(out string error) { RestoreCalls++;error=RestoreSucceeds?null:"selection-restore";return RestoreSucceeds; }
 internal int CacheCount => observedActivationByOwner.Count;
 internal bool Has(string id) => observedActivationByOwner.ContainsKey(id);
 internal void Observe() { SynchronizeLiveHorseCompanionProgression(); }
 __SYNC__
 __DISPOSE__
}
internal sealed class MountedPairServices { internal GameMountedRelationshipService Relationship; }
internal sealed class MountedWorldService {
 private readonly List<MountedPairServices> pairs=new List<MountedPairServices>();
 internal void Add(GameMountedRelationshipService owner) { pairs.Add(new MountedPairServices{Relationship=owner});owner.World=this; }
 __WORLD_FIND__
}
internal sealed class GameMountedRelationshipService { internal MountedWorldService World; }
namespace KingmakerMountedCombat.Diagnostics {
 internal sealed class RuntimeAutomationHost {
  internal static RuntimeAutomationHost active;
  internal bool disposed,completed,completionStarted;
  internal GameMountedRelationshipService relationship;
  __HOST_GATE__
 }
}
internal class UnitCommand { internal bool IsStarted,IsFinished; }
internal sealed class UnitMoveTo:UnitCommand {}
internal sealed class MountedPairAttackCommand:UnitCommand {
 private readonly GameMountedRelationshipService relationship;
 internal readonly Logger logger=new Logger();
 internal Unit Executor=new Unit {UniqueId="actor"};
 private string preStartInterruptBoundary;
 internal MountedPairAttackCommand(GameMountedRelationshipService owner){relationship=owner;}
 internal string Observation=>preStartInterruptBoundary;
 __INTERRUPT__
}
internal sealed class MountedCombatController {
 internal MountedPairAttackCommand activeCommand;
 internal UnitCommand observedNativeMountTurnMove,projectedNativeMountTurnGroundMove;
 internal string LastNativeMountTurnMoveInterruptSource="<not-interrupted>";
 internal int ProjectedClears;
 private void ClearProjectedNativeMountTurnGroundMove(){ProjectedClears++;projectedNativeMountTurnGroundMove=null;}
 private string DescribeNativeMountTurnMoveInterruptState(UnitMoveTo command)=>"state";
 __CONTROLLER__
}
internal sealed class ThrowingRoster:IEnumerable<UnitReference> {
 private readonly Unit first; internal ThrowingRoster(Unit first){this.first=first;}
 public IEnumerator<UnitReference> GetEnumerator(){yield return new UnitReference{Value=first};throw new InvalidOperationException("partial-roster");}
 IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
}
internal static class Program {
 private static int passed,failed;
 public static void NativeCaller(){}
 private static void Check(string name,bool ok){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(ok)passed++;else failed++;}
 private static string Error(Action action){try{action();return null;}catch(Exception e){return e.Message;}}
 private static Unit Owner(string id,bool pending=false){var u=new Unit{UniqueId=id};var pet=new Unit{UniqueId=id+"-pet"};pet.Descriptor.Master.Value=u;u.Descriptor.Pet=pet;u.Descriptor.Fact=new Fact{AddPet=new HorseCompanionAddPet{DeferredProgressionPending=pending}};return u;}
 private static void Roster(params Unit[] units){Game.Current=new Game{Player=new Player{PartyCharacters=units.Select(x=>new UnitReference{Value=x}).ToArray()}};Reads.Fail=false;}
 private static HorseCompanionBlueprintService Cache(params Unit[] units){Roster(units);var service=new HorseCompanionBlueprintService();service.Observe();return service;}
 private static void ResetReads(){Reads.Native=0;Reads.Fail=false;System.Diagnostics.StackTrace.Created=0;System.Diagnostics.StackTrace.Fail=false;}
 private static MountedPairAttackCommand Command(GameMountedRelationshipService owner){return new MountedPairAttackCommand(owner);}
 private static void Off(string name,Action<RuntimeAutomationHost,GameMountedRelationshipService> setup){
  Roster();var owner=new GameMountedRelationshipService();var host=new RuntimeAutomationHost{relationship=owner};RuntimeAutomationHost.active=host;setup(host,owner);var command=Command(owner);ResetReads();Reads.Fail=true;
  Check(name+" skips native reads and stack",Error(command.ObservePreStartInterrupt)==null&&Reads.Native==0&&System.Diagnostics.StackTrace.Created==0);
  Check(name+" leaves observation/log/debt unchanged",command.Observation==null&&command.logger.InfoRows.Count==0&&command.Executor.CombatState.Cooldown.StandardAction==3&&command.Executor.CombatState.Cooldown.MoveAction==2&&!command.IsStarted&&!command.IsFinished);
 }
 private static void SignatureChecks(){
  var originalCulture=System.Threading.Thread.CurrentThread.CurrentCulture;
  var culture=new CountingCulture();
  try{
   System.Threading.Thread.CurrentThread.CurrentCulture=culture;
   var a=Owner("format");var service=Cache(a);culture.FormatReads=0;service.Observe();
   Check("unchanged positive observation performs no integer formatting",culture.FormatReads==0&&service.logger.InfoRows.Count==1);
   culture.FormatReads=0;for(var i=0;i<100;i++)service.Observe();Check("repeated unchanged polls never construct a rank signature",culture.FormatReads==0&&service.logger.InfoRows.Count==1);
   a.Descriptor.Fact.Rank=6;culture.FormatReads=0;service.Observe();Check("rank change formats exactly the prior text",culture.FormatReads>0&&service.logger.InfoRows.Last()=="Observed live KMC Horse feature: owner=format;rank=6;addPet=True;pet=format-pet;blueprint=horse-guid;master=format.");
   a.Descriptor.Fact.AddPet=null;service.Observe();Check("AddPet absence changes the same Boolean field",service.logger.InfoRows.Last().Contains(";addPet=False;"));
   a.Descriptor.Fact.AddPet=new HorseCompanionAddPet{DeferredProgressionPending=true};service.Observe();Check("AddPet return still runs the native retry",service.logger.InfoRows.Last().Contains(";addPet=True;")&&a.Descriptor.Fact.AddPet.Calls==1);
   var rows=service.logger.InfoRows.Count;a.Descriptor.Fact.AddPet=new HorseCompanionAddPet{DeferredProgressionPending=true};culture.FormatReads=0;service.Observe();Check("different present AddPet instance skips formatting but keeps its native retry",culture.FormatReads==0&&service.logger.InfoRows.Count==rows&&a.Descriptor.Fact.AddPet.Calls==1);
   a.Descriptor.Pet.UniqueId="new-pet";service.Observe();Check("pet ID change has exact original text",service.logger.InfoRows.Last().Contains(";pet=new-pet;blueprint=horse-guid;master=format."));
   a.Descriptor.Pet.Blueprint.AssetGuid="new-blueprint";service.Observe();Check("blueprint ID change has exact original text",service.logger.InfoRows.Last().Contains(";blueprint=new-blueprint;master=format."));
   a.Descriptor.Pet.Descriptor.Master.Value=new Unit{UniqueId="new-master"};service.Observe();Check("master ID change has exact original text",service.logger.InfoRows.Last().EndsWith(";master=new-master."));
   a.Descriptor.Pet.Blueprint=null;service.Observe();Check("null blueprint uses the original sentinel",service.logger.InfoRows.Last().Contains(";blueprint=<null>;"));
   rows=service.logger.InfoRows.Count;a.Descriptor.Pet.Blueprint=new Blueprint{AssetGuid=null};service.Observe();Check("null asset ID and missing blueprint retain original deduplication",service.logger.InfoRows.Count==rows);
   a.Descriptor.Pet.Blueprint.AssetGuid="<null>";service.Observe();Check("literal null sentinel retains original deduplication",service.logger.InfoRows.Count==rows);
   a.Descriptor.Pet.Blueprint.AssetGuid="";service.Observe();Check("empty blueprint remains distinct from null",service.logger.InfoRows.Count==rows+1&&service.logger.InfoRows.Last().Contains(";blueprint=;"));
   a.Descriptor.Pet.Descriptor.Master.Value=null;service.Observe();Check("null master keeps original sentinel",service.logger.InfoRows.Last().EndsWith(";master=<null>."));
   rows=service.logger.InfoRows.Count;a.Descriptor.Pet.Descriptor.Master.Value=new Unit{UniqueId=null};service.Observe();Check("null master ID keeps original deduplication",service.logger.InfoRows.Count==rows);
   a.Descriptor.Pet=null;service.Observe();Check("null pet normalizes all three original fields",service.logger.InfoRows.Last().EndsWith(";pet=<null>;blueprint=<null>;master=<null>."));
   rows=service.logger.InfoRows.Count;a.Descriptor.Pet=new Unit{UniqueId=null,Blueprint=null};service.Observe();Check("present pet with all null values preserves original deduplication",service.logger.InfoRows.Count==rows);
   a.Descriptor.Pet.UniqueId="";service.Observe();Check("empty pet ID remains distinct from null",service.logger.InfoRows.Count==rows+1&&service.logger.InfoRows.Last().Contains(";pet=;blueprint=<null>;"));
   a=Owner("collision");a.Descriptor.Pet.UniqueId="x;blueprint=y";a.Descriptor.Pet.Blueprint.AssetGuid="z";service=Cache(a);var prior=service.logger.InfoRows[0];
   a.Descriptor.Pet.UniqueId="x";a.Descriptor.Pet.Blueprint.AssetGuid="y;blueprint=z";service.Observe();Check("different field values with identical formatted text do not add a log",service.logger.InfoRows.Count==1&&service.logger.InfoRows[0]==prior);
   culture.FormatReads=0;service.Observe();Check("identical-string collision refreshes typed values without repeated formatting",culture.FormatReads==0&&service.logger.InfoRows.Count==1);
   a=Owner("negative");a.Descriptor.Fact.Rank=-7;service=Cache(a);Check("negative rank preserves default Int32 formatting",service.logger.InfoRows[0].Contains(";rank=-7;"));
   culture.NumberFormat.NegativeSign="~";service.Observe();Check("same negative rank changed sign preserves old formatted-state observation",service.logger.InfoRows.Count==2&&service.logger.InfoRows.Last().Contains(";rank=~7;"));
   a.Descriptor.Fact.Rank=int.MinValue;service.Observe();Check("Int32 minimum uses the captured default-format sign",service.logger.InfoRows.Last().Contains(";rank=~2147483648;"));
   a.Descriptor.Fact.Rank=7;service.Observe();rows=service.logger.InfoRows.Count;culture.NumberFormat.NegativeSign="!";culture.FormatReads=0;service.Observe();Check("irrelevant negative sign change cannot format positive unchanged rank",culture.FormatReads==0&&service.logger.InfoRows.Count==rows);
   a=Owner("reads");service=Cache(a);var observed=PollTrace.Capture(service.Observe);Check("unchanged observation preserves tracked native getter order",observed=="GetFact|AddPet|ID:reads|Pet|Rank|ID:reads-pet|Blueprint:horse-guid|Master|ID:reads|ID:reads");
   a.Descriptor.Fact.Rank=5;observed=PollTrace.Capture(service.Observe);Check("changed observation preserves tracked native getter order",observed=="GetFact|AddPet|ID:reads|Pet|Rank|ID:reads-pet|Blueprint:horse-guid|Master|ID:reads|ID:reads|ID:reads|ID:reads");
   a=Owner("log-retry",true);Roster(a);service=new HorseCompanionBlueprintService();service.logger.FailInfo=true;
   Check("failed changed-state log preserves prior cache-before-log and postpones native retry",Error(service.Observe)=="info-read"&&service.CacheCount==1&&a.Descriptor.Fact.AddPet.Calls==0);
   service.logger.FailInfo=false;culture.FormatReads=0;service.Observe();Check("poll after failed log keeps exact once-only native retry without reformatting",culture.FormatReads==0&&service.logger.InfoRows.Count==0&&a.Descriptor.Fact.AddPet.Calls==1);
  }finally{System.Threading.Thread.CurrentThread.CurrentCulture=originalCulture;PollTrace.Enabled=false;}
 }
 public static int Main(){
  var a=Owner("A");var b=Owner("B");var service=Cache(a,b);Check("first exact two owners observed",service.CacheCount==2&&service.logger.InfoRows.Count==2);
  service.Observe();Check("unchanged signatures do not log again",service.CacheCount==2&&service.logger.InfoRows.Count==2);
  a.Descriptor.Fact.Rank=5;service.Observe();Check("rank change preserves original signature output",service.logger.InfoRows.Count==3&&service.logger.InfoRows[2]=="Observed live KMC Horse feature: owner=A;rank=5;addPet=True;pet=A-pet;blueprint=horse-guid;master=A.");
  Roster(a);service.Observe();Check("complete roster retires absent owner cache only",service.CacheCount==1&&service.Has("A")&&!service.Has("B")&&b.Descriptor.Fact.Rank==4&&b.Descriptor.Pet.UniqueId=="B-pet");
  Roster(a,b);service.Observe();Check("returning absent owner is observed afresh",service.CacheCount==2&&service.logger.InfoRows.Count==4);
  b.Descriptor.Fact=null;service.Observe();Check("feature removal retires only its observational entry",service.CacheCount==1&&!service.Has("B")&&b.Descriptor.Pet!=null);
  b.Descriptor.Fact=new Fact{AddPet=new HorseCompanionAddPet()};service.Observe();Check("feature regain gets fresh unchanged-format log",service.Has("B")&&service.logger.InfoRows.Count==5);
  Game.Current.Player.PartyCharacters=new ThrowingRoster(a);Check("partial roster propagates failure",Error(service.Observe)=="partial-roster");Check("partial roster cannot establish absent-owner retirement",service.CacheCount==2&&service.Has("B"));
  Game.Current.Player.PartyCharacters=null;service.Observe();Check("unavailable roster does not claim disappearance",service.CacheCount==2);
  Game.Current=null;service.Observe();Check("unavailable player does not claim disappearance",service.CacheCount==2);
  Roster();service.Observe();Check("complete empty roster retires all observation entries",service.CacheCount==0);
  var originalOwner=Owner("same-id",true);service=Cache(originalOwner);var originalPlayer=Game.Current.Player;var replacementOwner=Owner("same-id",true);Roster(replacementOwner);service.Observe();
  Check("new world same ID preserves fresh native retry without retaining prior obligation",!ReferenceEquals(originalPlayer,Game.Current.Player)&&!ReferenceEquals(originalOwner,replacementOwner)&&service.CacheCount==1&&originalOwner.Descriptor.Fact.AddPet.Calls==1&&replacementOwner.Descriptor.Fact.AddPet.Calls==1&&service.logger.InfoRows.Count==1);
  replacementOwner.Descriptor.Fact.Rank=5;service.Observe();Check("new world same ID changed signature is still observed",service.CacheCount==1&&service.logger.InfoRows.Count==2&&service.logger.InfoRows[1].Contains("owner=same-id;rank=5;"));
  Roster();service.Observe();var readded=Owner("same-id",true);Roster(readded);service.Observe();Check("new world removed then readded ID gets fresh log and native retry",service.CacheCount==1&&service.logger.InfoRows.Count==3&&readded.Descriptor.Fact.AddPet.Calls==1&&originalOwner.Descriptor.Fact.AddPet.Calls==1&&replacementOwner.Descriptor.Fact.AddPet.Calls==1);
  service=new HorseCompanionBlueprintService();for(var i=0;i<1000;i++){Roster(Owner("owner-"+i));service.Observe();}
  Check("successive complete one-owner rosters retain only current entry",service.CacheCount==1&&service.Has("owner-999"));
  a=Owner("retry",true);service=Cache(a);Check("native deferred retry remains exactly once",a.Descriptor.Fact.AddPet.Calls==1&&!a.Descriptor.Fact.AddPet.DeferredProgressionPending);service.Observe();Check("settled native retry is never replayed",a.Descriptor.Fact.AddPet.Calls==1);
  a=Owner("failure",true);a.Descriptor.Fact.AddPet.ThrowRetry=true;a.Descriptor.Fact.AddPet.FailAfterRetry=true;Roster(a);service=new HorseCompanionBlueprintService();Check("native retry exception follows original once-only report",Error(service.Observe)==null&&a.Descriptor.Fact.AddPet.Calls==1&&service.logger.Errors.Count==1&&service.CacheCount==1);service.Observe();Check("native failure does not synthesize another retry",a.Descriptor.Fact.AddPet.Calls==1&&service.logger.Errors.Count==1);
  service.RestoreSucceeds=false;Check("failed exact selection restoration retains original cache/lifecycle",Error(service.Dispose)=="selection-restore"&&service.CacheCount==1&&service.RestoreCalls==1);service.RestoreSucceeds=true;service.Dispose();Check("successful service disposal releases observational strings",service.CacheCount==0&&service.RestoreCalls==2);service.Dispose();Check("repeated disposal does not restore selection again",service.RestoreCalls==2);
  Off("no host",(h,o)=>RuntimeAutomationHost.active=null);Off("disposed host",(h,o)=>h.disposed=true);Off("completed host",(h,o)=>h.completed=true);Off("foreign owner",(h,o)=>h.relationship=new GameMountedRelationshipService());
  Off("same world but absent owner",(h,o)=>{var world=new MountedWorldService();var primary=new GameMountedRelationshipService();world.Add(primary);h.relationship=primary;o.World=world;});
  Roster();var owner1=new GameMountedRelationshipService();var owner2=new GameMountedRelationshipService();var ownedWorld=new MountedWorldService();ownedWorld.Add(owner1);ownedWorld.Add(owner2);var host1=new RuntimeAutomationHost{relationship=owner1};RuntimeAutomationHost.active=host1;
  var cmd=Command(owner1);ResetReads();cmd.ObservePreStartInterrupt();var first=cmd.Observation;
  Check("live primary host preserves original forensic output",System.Diagnostics.StackTrace.Created==1&&cmd.logger.InfoRows.Count==1&&first=="frame=41;turn=turn;status=Preparing;mode=Default;paused=False;owner=actor;standard=3;move=2;source=Program.NativeCaller"&&cmd.logger.InfoRows[0]=="Mounted pre-start interruption: "+first);
  cmd.ObservePreStartInterrupt();Check("observed interruption remains exactly once",System.Diagnostics.StackTrace.Created==1&&cmd.logger.InfoRows.Count==1&&cmd.Observation==first);
  cmd=Command(owner2);ResetReads();cmd.ObservePreStartInterrupt();Check("registered second pair remains observable",System.Diagnostics.StackTrace.Created==1&&cmd.logger.InfoRows.Count==1);
  host1.completionStarted=true;cmd=Command(owner2);ResetReads();cmd.ObservePreStartInterrupt();Check("retained host cleanup preserves enabled forensic output",System.Diagnostics.StackTrace.Created==1&&cmd.logger.InfoRows.Count==1);
  foreach(var done in new[]{false,true}){cmd=Command(owner1);cmd.IsStarted=!done;cmd.IsFinished=done;ResetReads();cmd.ObservePreStartInterrupt();Check(done?"finished command stays untouched":"started command stays untouched",Reads.Native==0&&System.Diagnostics.StackTrace.Created==0&&cmd.Observation==null);}
  RuntimeAutomationHost.active=null;cmd=Command(owner1);var controller=new MountedCombatController{activeCommand=cmd,projectedNativeMountTurnGroundMove=cmd};ResetReads();Reads.Fail=true;
  Check("actual interrupt callback still clears exact projected ownership with diagnostics off",Error(()=>controller.ObserveCommandInterrupt(cmd))==null&&controller.ProjectedClears==1&&controller.projectedNativeMountTurnGroundMove==null&&Reads.Native==0&&System.Diagnostics.StackTrace.Created==0);
  Check("actual callback changes no native spending or command state",cmd.Executor.CombatState.Cooldown.StandardAction==3&&cmd.Executor.CombatState.Cooldown.MoveAction==2&&!cmd.IsStarted&&!cmd.IsFinished);
  RuntimeAutomationHost.active=host1;cmd=Command(owner1);ResetReads();System.Diagnostics.StackTrace.Fail=true;Check("enabled stack error preserves original propagation",Error(cmd.ObservePreStartInterrupt)=="stack-read"&&cmd.Observation==null&&cmd.logger.InfoRows.Count==0);
  cmd=Command(owner1);ResetReads();cmd.logger.FailInfo=true;Check("enabled log error preserves captured boundary and original propagation",Error(cmd.ObservePreStartInterrupt)=="info-read"&&cmd.Observation!=null&&System.Diagnostics.StackTrace.Created==1);
  SignatureChecks();
  Console.WriteLine("OBSERVATION LIFETIME PASS="+passed+" FAIL="+failed);return failed==0?0:1;
 }
}
