using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.State;
using Newtonsoft.Json.Linq;

namespace Kingmaker.GameModes { enum GameModeType { Default, GlobalMap, Cutscene } }
namespace Kingmaker.Blueprints.Area {
 class BlueprintArea { public string AssetGuidThreadSafe, name; }
 class BlueprintAreaEnterPoint { public string AssetGuidThreadSafe, name; public BlueprintArea Area; }
}
namespace Kingmaker.Globalmap.Blueprints { class BlueprintLocation { public string AssetGuidThreadSafe, name; public BlueprintAreaEnterPoint AreaEntrance; } }
namespace Kingmaker.Globalmap.State {
 class LocationData { public BlueprintLocation Blueprint; public bool IsSeen=true,IsRevealed=true,IsExplored=true,IsClosed; public BlueprintAreaEnterPoint Entry; public Action OnRead; public BlueprintAreaEnterPoint AreaEntrance {get {OnRead?.Invoke();return Entry;}} }
 class GlobalMapState { public Dictionary<BlueprintLocation,LocationData> Locations=new Dictionary<BlueprintLocation,LocationData>(); }
}
namespace Kingmaker.Blueprints {
 static class ResourcesLibrary { public static Dictionary<string,object> Rows=new Dictionary<string,object>();public static T TryGetBlueprint<T>(string id) where T:class {object value;return Rows.TryGetValue(id,out value)?value as T:null;} }
}
namespace Kingmaker {
 class Player { public string GameId="campaign";public bool IsInCombat;public GlobalMapState GlobalMap=new GlobalMapState(); }
 class Game { public static Game Instance;public Player Player=new Player();public BlueprintArea CurrentlyLoadedArea;public GameModeType CurrentMode=GameModeType.Default; }
}
namespace KingmakerMountedCombat.Diagnostics {
 class Descriptor {public string Area=Chunk7OwnerSeedAreaEntry.SourceArea,GameId="campaign",GameName="Native",FileName="Manual_303_KMC_C7_SEED_WORLD.zks",Sha256=new string('5',64);}
 class Target {public string Area=Chunk7OwnerSeedAreaEntry.DestinationArea,EnterPoint=Chunk7OwnerSeedAreaEntry.Entry,AutoSaveMode="AfterEntry";}
 class Selection {public const string Scenario="chunk7-owned-two-pair-campaign",OwnerSeedManualName="KMC_C7_SEED_WORLD";public string Case="owner-seed-checkpoint-save",SourceArea=Chunk7OwnerSeedAreaEntry.SourceArea;public bool Cold;public bool IsOwnerSeedCheckpointSave=>Case=="owner-seed-checkpoint-save";public bool IsOwnerSeedCheckpointLoad=>Case=="owner-seed-checkpoint-load";public string SourceRunId="producer",SourceArchiveSha256=new string('5',64),SeedId="seed",SeedSha256=new string('9',64),CheckpointSourceRunId,CheckpointSourceArtifactSha256,RiderAId="A",MountAId="MA",RiderBId="B",MountBId="MB";public bool CrossCandidateAllowedFor(string checkpoint)=>IsOwnerSeedCheckpointSave||IsOwnerSeedCheckpointLoad||Case=="owner-seed-persistence-load"&&checkpoint=="multi-exploration-three-loads-manual";}
 class Chunk7OwnedPairsSelection:Selection { }
 class RuntimeRequest {public Selection OwnedPairs=new Selection();public string PersistenceCase="multi-area-autosave",RunId="consumer",Commit=new string('1',40),DllSha256=new string('2',64),DllMvid="33333333-3333-4333-8333-333333333333";public Target PersistenceAreaTarget=new Target();public Descriptor PersistenceLoad=new Descriptor();}
 static class Chunk7WorldScenarioEngine {public const string EvidenceKind="chunk7-world-evidence",OwnerSeedManualReceiptContract="owner-seed-derived-manual-v1",OwnerSeedDerivedAncestry="owner-seed-derived";}
 static class NativeMountedSaveFormat {public const int SelectedWriterSchema=3;}
 static class MountedArchiveData {public const int Schema=3;}
 class SourceFixture {
  internal RuntimeRequest request=new RuntimeRequest();internal Selection selection;private JObject observations,sourceWorld,sourceArrangement;private JObject[] ownerRows;
  internal JObject OwnerSeedBoundary,OwnerSeedArchive,OwnerSeedCandidates;internal int OwnerSeedProcessId;
  internal SourceFixture(){selection=request.OwnedPairs;}
  internal void Read(JObject artifact){observations=(JObject)artifact["observations"];ReadOwnerSeedDerivedSource(artifact);}
  private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
  /* ACTUAL_SOURCE_METHOD */
 }
 class Actor { }
 class Persistence {public int SemanticRestoreCount,PresentationRestoreCount,ArchivePostLoadObservedCount,AreaSuspensionCount,AreaResumeCount,FailedAreaPairCount,NativeWorldDisposalCount;}
 class Chunk7NativeWorldScope {
  public string Area;public Chunk7NativeWorldScope(RuntimeRequest r,Actor[] p,string area){Area=area;}
  public void RequireDeclaredAreaDestination(string area,object blueprint){if(Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe!=area||!ReferenceEquals(blueprint,Game.Instance.CurrentlyLoadedArea))throw new InvalidOperationException("wrong actual destination");}
 }
 class Fixture {
  public bool bound=true,released,Foreign,RequestChanged,AuthorityChanged,NotReady;
  public Selection selection;public RuntimeRequest request;public Game game;public Persistence persistence=new Persistence();public Actor[] Party={new Actor(),new Actor(),new Actor(),new Actor()};public Chunk7NativeWorldScope Scope;
  private int replayExpectedLoad,replayExpectedArea;private object admittedPlayer;
  private bool areaAutosavePending,areaAutosaveAdopted;private BlueprintArea areaAutosaveDestination;
  private int areaAutosaveSemanticBaseline,areaAutosavePresentationBaseline,areaAutosavePostLoadBaseline;
  public Fixture(bool seed){request=new RuntimeRequest();selection=request.OwnedPairs;if(!seed)selection.Case="persistence-save";game=Game.Instance=new Game{CurrentlyLoadedArea=new BlueprintArea{AssetGuidThreadSafe=selection.SourceArea}};admittedPlayer=game.Player;Scope=new Chunk7NativeWorldScope(request,Party,selection.SourceArea);if(seed){persistence.SemanticRestoreCount=4;persistence.PresentationRestoreCount=2;persistence.ArchivePostLoadObservedCount=4;}}
  private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
  private void RequireRequest(){Require(!RequestChanged,"request changed");}private void RequireAuthority(){Require(!AuthorityChanged,"authority changed");}
  private void RequirePreserved(){Require(!Foreign,"foreign inherited actors");}private void RequireReplayFieldReady(){Require(!NotReady,"unfinished native field");}
  public void Begin(BlueprintArea area){BeginAreaAutosave(area);}public void Adopt(BlueprintArea area){AdoptAreaAutosave(area);}public bool Pending=>areaAutosavePending;public bool Adopted=>areaAutosaveAdopted;
  public void Arrive(BlueprintArea area){game.CurrentlyLoadedArea=area;persistence.AreaSuspensionCount=1;persistence.AreaResumeCount=2;}
  /* ACTUAL_AREA_METHODS */
 }
 static class Program {
  static int passed,failed;
  static void Check(string n,Action a){try{a();passed++;Console.WriteLine("PASS "+n);}catch(Exception e){failed++;Console.WriteLine("FAIL "+n+": "+e.Message);}}
  static void True(bool v,string m){if(!v)throw new Exception(m);}static void Refuse(Action a){bool yes=false;try{a();}catch(InvalidOperationException){yes=true;}True(yes,"invalid boundary admitted");}
  sealed class Rig {
   public RuntimeRequest Request=new RuntimeRequest();public Game Game;public BlueprintLocation Location;public LocationData Data;public BlueprintAreaEnterPoint Entry;
   public Rig(){Game=Kingmaker.Game.Instance=new Game{CurrentlyLoadedArea=new BlueprintArea{AssetGuidThreadSafe=Chunk7OwnerSeedAreaEntry.SourceArea,name="SpiderCave"}};Entry=new BlueprintAreaEnterPoint{AssetGuidThreadSafe=Chunk7OwnerSeedAreaEntry.Entry,name="OlegEntry",Area=new BlueprintArea{AssetGuidThreadSafe=Chunk7OwnerSeedAreaEntry.DestinationArea,name="OlegsTradingPost"}};Location=new BlueprintLocation{AssetGuidThreadSafe=Chunk7OwnerSeedAreaEntry.Location,name="OlegLocation",AreaEntrance=Entry};Data=new LocationData{Blueprint=Location,Entry=Entry};Game.Player.GlobalMap.Locations.Add(Location,Data);ResourcesLibrary.Rows=new Dictionary<string,object>{{Chunk7OwnerSeedAreaEntry.Location,Location},{Chunk7OwnerSeedAreaEntry.Entry,Entry}};}
   public BlueprintAreaEnterPoint Resolve(out JObject o){return Chunk7OwnerSeedAreaEntry.Resolve(Game,Request,out o);}
  }
  static BlueprintArea Destination()=>new BlueprintArea{AssetGuidThreadSafe=Chunk7OwnerSeedAreaEntry.DestinationArea};
  static JObject Source(SourceFixture f,bool prior=false){var r=f.request;var s=f.selection;return new JObject{["scenario"]=Selection.Scenario,["evidenceKind"]=Chunk7WorldScenarioEngine.EvidenceKind,["runId"]=s.SourceRunId,["status"]="PASS",["errors"]=new JArray(),["commit"]=prior?new string('f',40):r.Commit,["dllSha256"]=prior?new string('f',64):r.DllSha256,["dllMvid"]=prior?"ffffffff-ffff-4fff-8fff-ffffffffffff":r.DllMvid,["observations"]=new JObject{["nativeWrite"]=new JObject{["sourceSchema"]=3,["archive"]=new JObject{["SchemaVersion"]=3,["AreaId"]=Chunk7OwnerSeedAreaEntry.SourceArea},["sha256"]=s.SourceArchiveSha256,["fileName"]="Manual_303_KMC_C7_SEED_WORLD.zks",["nativeName"]=Selection.OwnerSeedManualName,["nativeType"]="Manual",["gameId"]=r.PersistenceLoad.GameId,["gameName"]=r.PersistenceLoad.GameName,["area"]=Chunk7OwnerSeedAreaEntry.SourceArea},["ownerSeedManual"]=new JObject{["contract"]=Chunk7WorldScenarioEngine.OwnerSeedManualReceiptContract,["ancestry"]="owner-seed-derived",["disclosedAssistance"]=true,["acquisitionProof"]=false,["levelUpSave"]=false,["seedId"]=s.SeedId,["seedSha256"]=s.SeedSha256,["seedCopyUnchanged"]=true,["boundary"]=new JObject(),["processId"]=4242,["fieldWorld"]=new JObject{["gameId"]=r.PersistenceLoad.GameId,["area"]=Chunk7OwnerSeedAreaEntry.SourceArea},["twoOwners"]=new JArray(new JObject{["actorId"]="A",["petId"]="MA"},new JObject{["actorId"]="B",["petId"]="MB"}),["arrangement"]=new JObject{["money"]=17},["arrangementAfterSave"]=new JObject{["money"]=17}}}};}
  static int Main(){
   Check("actual visited-entry gate returns registered actual campaign entry without adding a map row",()=>{var r=new Rig();var before=r.Game.Player.GlobalMap.Locations.Count;JObject o;var e=r.Resolve(out o);True(ReferenceEquals(e,r.Entry)&&before==r.Game.Player.GlobalMap.Locations.Count&&(bool)o["navigationAssisted"]&&!(bool)o["ordinaryTravelProof"]&&!(bool)o["authoredExitProof"]&&(int)o["transferRequests"]==0&&(int)o["saveRequests"]==0,"gate mutated or overclaimed navigation");});
   foreach(var flaw in new[]{"wrong-case","wrong-family","wrong-source","wrong-target","wrong-entry","before-exit","combat","global-map","missing-location","closed","unexplored","unseen","unrevealed","foreign-data","different-entrance","different-area","map-mutation"}){var name=flaw;Check("actual visited-entry gate refuses "+name,()=>{var r=new Rig();switch(name){case "wrong-case":r.Request.PersistenceCase="multi-exploration-auto";break;case "wrong-family":r.Request.OwnedPairs.Case="persistence-save";break;case "wrong-source":r.Request.OwnedPairs.SourceArea=new string('a',32);break;case "wrong-target":r.Request.PersistenceAreaTarget.Area=new string('a',32);break;case "wrong-entry":r.Request.PersistenceAreaTarget.EnterPoint=new string('a',32);break;case "before-exit":r.Request.PersistenceAreaTarget.AutoSaveMode="BeforeExit";break;case "combat":r.Game.Player.IsInCombat=true;break;case "global-map":r.Game.CurrentMode=GameModeType.GlobalMap;break;case "missing-location":r.Game.Player.GlobalMap.Locations.Clear();break;case "closed":r.Data.IsClosed=true;break;case "unexplored":r.Data.IsExplored=false;break;case "unseen":r.Data.IsSeen=false;break;case "unrevealed":r.Data.IsRevealed=false;break;case "foreign-data":r.Data.Blueprint=new BlueprintLocation{AssetGuidThreadSafe=r.Location.AssetGuidThreadSafe};break;case "different-entrance":r.Data.Entry=new BlueprintAreaEnterPoint{AssetGuidThreadSafe=r.Entry.AssetGuidThreadSafe,Area=r.Entry.Area};break;case "different-area":r.Entry.Area.AssetGuidThreadSafe=new string('a',32);break;case "map-mutation":r.Data.OnRead=()=>r.Game.Player.GlobalMap.Locations.Add(new BlueprintLocation(),new LocationData());break;}JObject o=null;Refuse(()=>r.Resolve(out o));True(o==null,"failed gate supplied successful observation");});}
   foreach(var seed in new[]{false,true}){var selected=seed;Check("actual area fixture adopts only declared native destination with original baseline seed="+selected,()=>{var f=new Fixture(selected);var d=Destination();var prior=f.Scope;f.Begin(d);True(f.Pending&&!f.Adopted,"admitted before transfer");f.Arrive(d);f.Adopt(d);True(f.Adopted&&!f.Pending&&!ReferenceEquals(prior,f.Scope)&&f.persistence.SemanticRestoreCount==(selected?4:0)&&f.persistence.PresentationRestoreCount==(selected?2:0)&&f.persistence.ArchivePostLoadObservedCount==(selected?4:0),"native counters reset or wrong scope retained");Refuse(()=>f.Adopt(d));});}
   foreach(var bad in new[]{"semantic","presentation","postload"}){var name=bad;Check("actual restored-start admission refuses baseline drift "+name,()=>{var f=new Fixture(true);if(name=="semantic")f.persistence.SemanticRestoreCount=0;if(name=="presentation")f.persistence.PresentationRestoreCount=0;if(name=="postload")f.persistence.ArchivePostLoadObservedCount=0;Refuse(()=>f.Begin(Destination()));True(!f.Pending,"failed preparation claimed transfer");});}
   foreach(var bad in new[]{"extra-semantic","extra-presentation","extra-postload","partial-resume","no-suspension","world-disposal","foreign-world","wrong-blueprint","request","authority","field"}){var name=bad;Check("actual area adoption retains custody on "+name,()=>{var f=new Fixture(true);var d=Destination();f.Begin(d);f.Arrive(d);var prior=f.Scope;switch(name){case "extra-semantic":f.persistence.SemanticRestoreCount++;break;case "extra-presentation":f.persistence.PresentationRestoreCount++;break;case "extra-postload":f.persistence.ArchivePostLoadObservedCount++;break;case "partial-resume":f.persistence.AreaResumeCount=1;break;case "no-suspension":f.persistence.AreaSuspensionCount=0;break;case "world-disposal":f.persistence.NativeWorldDisposalCount=1;break;case "foreign-world":f.game.Player=new Player();break;case "wrong-blueprint":f.game.CurrentlyLoadedArea=Destination();break;case "request":f.RequestChanged=true;break;case "authority":f.AuthorityChanged=true;break;case "field":f.Foreign=true;break;}Refuse(()=>f.Adopt(d));True(ReferenceEquals(prior,f.Scope)&&f.Pending&&!f.Adopted,"failed transfer dropped captured custody");});}
   Check("actual source reader keeps original Manual area separate from area cold destination",()=>{var f=new SourceFixture();f.selection.Case="owner-seed-checkpoint-load";f.request.PersistenceLoad.Area=Chunk7OwnerSeedAreaEntry.DestinationArea;f.request.PersistenceLoad.FileName="Auto_1.zks";f.request.PersistenceLoad.Sha256=new string('3',64);var a=Source(f,true);f.Read(a);True((string)f.OwnerSeedArchive["AreaId"]==Chunk7OwnerSeedAreaEntry.SourceArea&&(bool)f.OwnerSeedCandidates["crossCandidateAllowed"]&&!(bool)f.OwnerSeedCandidates["sameCandidate"],"original source was rewritten or candidate relabelled");});
   Check("actual repeated source reader admits only recorded earlier candidate",()=>{var f=new SourceFixture();f.selection.Case="owner-seed-persistence-load";f.request.PersistenceCase="multi-exploration-three-loads-manual";f.Read(Source(f,true));True(!(bool)f.OwnerSeedCandidates["sameCandidate"],"earlier source relabelled");});
   Check("actual ordinary first cold source remains same candidate",()=>{var f=new SourceFixture();f.selection.Case="owner-seed-persistence-load";f.request.PersistenceCase="multi-exploration-manual";f.Read(Source(f));Refuse(()=>f.Read(Source(f,true)));});
   foreach(var fault in new[]{"source-area","input-aliases-manual","input-aliases-seed","write-area","field-area","wrong-source-run","failed-source","schema2-write","schema2-archive","missing-disclosure","acquisition-claim","modified-seed","wrong-rider","wrong-mount","arrangement-change","malformed-candidate"}){var name=fault;Check("actual area cold source reader refuses "+name,()=>{var f=new SourceFixture();f.selection.Case="owner-seed-checkpoint-load";f.request.PersistenceLoad.Area=Chunk7OwnerSeedAreaEntry.DestinationArea;f.request.PersistenceLoad.FileName="Auto_1.zks";f.request.PersistenceLoad.Sha256=new string('3',64);var a=Source(f,true);var o=(JObject)a["observations"];switch(name){case "source-area":f.selection.SourceArea=f.request.PersistenceLoad.Area;break;case "input-aliases-manual":f.request.PersistenceLoad.Sha256=f.selection.SourceArchiveSha256;break;case "input-aliases-seed":f.request.PersistenceLoad.Sha256=f.selection.SeedSha256;break;case "write-area":o["nativeWrite"]["area"]=f.request.PersistenceLoad.Area;break;case "field-area":o["ownerSeedManual"]["fieldWorld"]["area"]=f.request.PersistenceLoad.Area;break;case "wrong-source-run":a["runId"]="other";break;case "failed-source":a["status"]="FAIL";break;case "schema2-write":o["nativeWrite"]["sourceSchema"]=2;break;case "schema2-archive":o["nativeWrite"]["archive"]["SchemaVersion"]=2;break;case "missing-disclosure":o["ownerSeedManual"]["disclosedAssistance"]=false;break;case "acquisition-claim":o["ownerSeedManual"]["acquisitionProof"]=true;break;case "modified-seed":o["ownerSeedManual"]["seedCopyUnchanged"]=false;break;case "wrong-rider":o["ownerSeedManual"]["twoOwners"][0]["actorId"]="B";break;case "wrong-mount":o["ownerSeedManual"]["twoOwners"][1]["petId"]="MA";break;case "arrangement-change":o["ownerSeedManual"]["arrangementAfterSave"]["money"]=18;break;case "malformed-candidate":a["dllMvid"]="bad";break;}Refuse(()=>f.Read(a));});}
   Console.WriteLine("OWNER SEED ACTUAL AREA CONNECTION PASS="+passed+" FAIL="+failed+"; actual gate, scope and source methods; native containers and dispatch are facades.");return failed==0?0:1;
  }
 }
}
