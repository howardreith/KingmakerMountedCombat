using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using KingmakerMountedCombat.Diagnostics;
namespace KingmakerMountedCombat.Tests {
internal static class OwnedProgressionProtocolTests {
 private const string Campaign="11111111-1111-4111-8111-111111111111",Area="0123456789abcdef0123456789abcdef";
 private static string root;
 private static int Main(string[] args){
  // Read-only resolution against the installed managed assemblies; never copy game binaries.
  AppDomain.CurrentDomain.AssemblyResolve += (sender,eventArgs) => {
   var name=new System.Reflection.AssemblyName(eventArgs.Name).Name;
   if(name!="Newtonsoft.Json"&&name!="UnityEngine"&&name!="UnityEngine.CoreModule") return null;
   return System.Reflection.Assembly.LoadFrom(Path.Combine(@"C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data\Managed",name+".dll"));
  };
  return Run(args);
 }
 private static int Run(string[] args){root=Path.GetFullPath(args.Single());var tests=new TestRunner();
  foreach(var selected in new[]{Chunk7ProgressionSelection.LevelCase,Chunk7ProgressionSelection.DismissCase,Chunk7ProgressionSelection.RetrainHorseCase,Chunk7ProgressionSelection.RetrainMammothCase})
   tests.Run("progression complete protocol accepts "+selected,()=>TestRunner.Equal(0,Request(selected).Validate().Count,"Valid closed progression request refused."));
  foreach(var name in new[]{"missing","foreign scenario","foreign case","persistence case","foreign source","working source","wrong leaf","wrong campaign","self source","self inventory","same actor","unknown target","unobserved body","wrong respec body","missing handedness","dismissal write","second write","unbounded level","wrong autosave leaf"})
   tests.Run("progression protocol rejects "+name,()=>{var r=Request(name=="dismissal write"?Chunk7ProgressionSelection.DismissCase:name=="wrong respec body"||name=="missing handedness"?Chunk7ProgressionSelection.RetrainHorseCase:Chunk7ProgressionSelection.LevelCase);switch(name){
    case "missing":r.Progression=null;break;case "foreign scenario":r.Scenario=Chunk7OrdinaryFieldSelection.Scenario;break;case "foreign case":r.Progression.Case="horse-any";break;
    case "persistence case":r.PersistenceCase="multi-exploration-manual";break;case "foreign source":r.PersistenceLoad.Sha256=new string('f',64);break;case "working source":r.PersistenceLoad=r.Fixture.Working;break;
    case "wrong leaf":r.PersistenceLoad.FileName="Manual_320_KMC_PROGRESS.zks";break;case "wrong campaign":r.PersistenceLoad.GameId="22222222-1111-4111-8111-111111111111";break;
    case "self source":r.Progression.SourceRunId=r.RunId;break;case "self inventory":r.Progression.InventoryRunId=r.RunId;break;case "same actor":r.Progression.PetId=r.Progression.ControlPetId;break;
    case "unknown target":r.Progression.ActorId="33333333-1111-4111-8111-111111111111";break;case "unobserved body":r.Progression.RaceGuid=new string('a',32);break;
    case "wrong respec body":r.Progression.BodySize="Small";break;case "missing handedness":r.Progression.LeftHanded=null;break;case "dismissal write":r.Progression.ExpectedNativeWriteCount=1;break;
    case "second write":r.Progression.ExpectedNativeWriteCount=2;break;case "unbounded level":r.Progression.MinimumRangerLevel=21;break;case "wrong autosave leaf":r.Progression.NativeWriteLeaf="Auto_2.zks";break;}
    TestRunner.True(r.Validate().Count>0,"Invalid progression request accepted.");});
  tests.Run("progression exact rows are registered",()=>{foreach(var name in new[]{"C7-progression-cold-source","C7-progression-native-operation","C7-progression-cleanup","C7-progression-runtime","C7-progression-interrupted"})TestRunner.True(RuntimeSubscenarioResult.IsMissionScenario(name),"Unknown exact progression row.");TestRunner.True(!RuntimeSubscenarioResult.IsMissionScenario("C7-progression-any"),"Wildcard progression admitted.");});
  tests.Run("matches progression plan never spends load or write",()=>{var f=new Fixture();var a=f.Scope(true);for(var i=0;i<4;i++)TestRunner.True(a.MatchesProgressionPlan(f.Source,true,"Autosave"),"Exact plan refused.");TestRunner.Equal(0,a.AdmittedNativeLoadRequests,"Plan check spent load.");TestRunner.Equal(0,a.AdmittedNativeWriteRequests,"Plan check spent write.");});
  tests.Run("native progression load cap survives write admission",()=>{var f=new Fixture();var a=f.Scope(true);TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Load,f.Load,f.Saves)==null,"Initial load refused.");TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Load,f.Load,f.Saves)!=null,"Second load admitted.");TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Write,f.Write,f.Saves)==null,"One exact terminal save refused.");TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Write,f.Write,f.Saves)!=null,"Second native write admitted.");TestRunner.Equal(1,a.AdmittedNativeLoadRequests,"Load quota changed.");TestRunner.Equal(1,a.AdmittedNativeWriteRequests,"Write quota changed.");});
  tests.Run("dismissal scope cannot write",()=>{var f=new Fixture();var a=f.Scope(false);TestRunner.True(a.MatchesProgressionPlan(f.Source,false,"Autosave"),"Exact dismissal plan refused.");TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Write,f.Write,f.Saves)!=null,"Dismissal wrote a save.");});
  tests.Run("read-only Manual300 never authorizes overwrite",()=>{var f=new Fixture();var a=f.Scope(true);TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Write,f.Load,f.Saves)!=null,"Readonly source overwritten.");});
  tests.Run("changed Manual300 bytes cannot spend load",()=>{var f=new Fixture();var a=f.Scope(true);File.AppendAllText(f.Load.FullPath,"changed");TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Load,f.Load,f.Saves)!=null,"Changed native source loaded.");TestRunner.Equal(0,a.AdmittedNativeLoadRequests,"Refused source spent quota.");});
  foreach(var name in new[]{"extra write","writable source","wrong source type","wrong source hash","wrong source name","wrong output area","wrong output name","wrong output type","missing autosave"})
   tests.Run("progression source plan rejects "+name,()=>{var f=new Fixture();var e=f.Entries(true);switch(name){
    case "extra write":e=e.Concat(new[]{new PersistenceSaveEntry{FileName="Auto_2.zks",InternalName="Extra",SaveType="Auto",Area=Area,Writable=true}}).ToArray();break;
    case "writable source":e[0].Writable=true;break;case "wrong source type":e[0].SaveType="Auto";break;case "wrong source hash":f.Source.Sha256=new string('e',64);break;
    case "wrong source name":e[0].InternalName="Other";break;case "wrong output area":e[1].Area=new string('f',32);break;case "wrong output name":e[1].InternalName="Other";break;
    case "wrong output type":e[1].SaveType="Manual";break;case "missing autosave":e=e.Take(1).ToArray();break;}
    var a=new PersistenceSaveAuthorization(f.Run,Campaign,"Fixture",new string('a',64),e,null,null,1,1);TestRunner.True(!a.MatchesProgressionPlan(f.Source,true,"Autosave"),"Foreign progression plan admitted.");});

  foreach(var selected in new[]{Chunk7ProgressionSelection.OwnedLevelCase,Chunk7ProgressionSelection.OwnedDismissCase,Chunk7ProgressionSelection.OwnedRetrainHorseCase,Chunk7ProgressionSelection.OwnedRetrainMammothCase}){
   tests.Run("owned progression exact protocol "+selected,()=>{var r=OwnedRequest(selected);TestRunner.Equal(0,r.Validate().Count,string.Join(";",r.Validate()));TestRunner.True(ReferenceEquals(r.PersistenceLoad,r.SelectedChunk7Fixture),"Owned source did not bind selected world.");TestRunner.True(r.OwnedPairs==null,"Nested source acquired outer dispatch authority.");});
   tests.Run("owned progression actual DTO "+selected,()=>{var r=OwnedRequest(selected);var j=Chunk7EvidenceDto.Request(r.Progression);TestRunner.True(j["ownedSource"] is JObject&&j["OwnedSource"]==null,"Nested source did not use actual request-shaped serializer.");TestRunner.Equal("persistence-load",(string)j["ownedSource"]["case"],"Nested source case changed.");TestRunner.Equal(1,(int)j["ownedSource"]["expectedNativeLoadCount"],"Nested load quota changed.");TestRunner.Equal(0,(int)j["ownedSource"]["expectedNativeWriteCount"],"Nested source acquired a write.");TestRunner.Equal(0,j.ToObject<Chunk7ProgressionSelection>().Validate(r.Scenario,r.RunId).Count,"Actual serialized source did not round trip.");});
  }
  tests.Run("Small dismissal admits only its seven-actor mounted producer and separate cold consumer",()=>{
   var r=SmallDismissRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(";",r.Validate()));
   TestRunner.True(r.Progression.IsSmallDismiss&&r.Progression.IsOwned&&!r.Progression.WritesSave,"Small dismissal acquired a write or lost owned custody.");
   TestRunner.True(ReferenceEquals(r.PersistenceLoad,r.SelectedChunk7Fixture),"Small mounted Manual did not select its own source.");
  });
  tests.Run("Small Horse retrain admits its exact mounted source and native Manual",()=>{
   var r=SmallRetrainRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(";",r.Validate()));
   TestRunner.True(r.Progression.IsSmallOwned&&r.Progression.IsRespec&&r.Progression.WritesManual&&!r.Progression.WritesAutosave,
    "Small retrain lost its native respec or acquired Auto authority.");
   TestRunner.True(ReferenceEquals(r.PersistenceLoad,r.SelectedChunk7Fixture),"Small retrain did not select its mounted Manual.");
  });
  tests.Run("Small later-level admits the exact disclosed XP shortfall and one native Manual",()=>{
   var r=SmallLevelRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(";",r.Validate()));
   TestRunner.True(r.Progression.IsSmallOwned&&!r.Progression.IsRespec&&r.Progression.WritesManual&&!r.Progression.WritesAutosave,
    "Small later-level lost its owned native level operation or acquired Auto authority.");
   var dto=Chunk7EvidenceDto.Request(r.Progression);
   TestRunner.Equal(6000,(int)dto["fixtureExperienceGrant"],"Disclosed fixture XP shortfall was not serialized.");
   TestRunner.Equal(0,dto.ToObject<Chunk7ProgressionSelection>().Validate(r.Scenario,r.RunId).Count,
    "Small native-level selection did not round trip.");
  });
  tests.Run("paid Medium Horse to Mammoth retrain admits only its mounted source and exact eligibility grant",()=>{
   var r=MediumMammothRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(";",r.Validate()));
   TestRunner.True(r.Progression.IsMediumOwned&&r.Progression.IsRespec&&r.Progression.WritesManual&&!r.Progression.WritesAutosave,
    "Paid Medium replacement lost native Manual or acquired Auto authority.");
   TestRunner.True(ReferenceEquals(r.PersistenceLoad,r.SelectedChunk7Fixture),"Medium mounted Manual did not select its own source.");
   var dto=Chunk7EvidenceDto.Request(r.Progression);
   TestRunner.Equal(96000,(int)dto["fixtureExperienceGrant"],"Medium eligibility grant changed in serialized request.");
   TestRunner.Equal(0,dto.ToObject<Chunk7ProgressionSelection>().Validate(r.Scenario,r.RunId).Count,
    "Medium native replacement did not round trip.");
  });
  foreach(var label in new[]{"missing grant","different grant","higher minimum","Small source","wrong profile body","wrong target","foreign mounted name","unmounted ancestor"})
   tests.Run("paid Medium replacement refuses "+label,()=>{var r=MediumMammothRequest();var s=r.Progression;switch(label){
    case "missing grant":s.FixtureExperienceGrant=null;break;case "different grant":s.FixtureExperienceGrant=95999;break;
    case "higher minimum":s.MinimumRangerLevel=11;break;case "Small source":s.OwnedSource.Case=Chunk7OwnedPairsSelection.SmallFixtureLoadCase;break;
    case "wrong profile body":s.BodySize="Small";break;case "wrong target":s.ActorId=s.ControlActorId;break;
    case "foreign mounted name":r.PersistenceLoad.FileName="Manual_305_KMC_C7_SMALL_WORLD.zks";break;
    case "unmounted ancestor":s.SourceArchiveSha256=s.OwnedSource.SourceArchiveSha256;r.PersistenceLoad.Sha256=s.SourceArchiveSha256;break;
   }TestRunner.True(r.Validate().Count>0,"Invalid paid Medium replacement was admitted.");});
  foreach(var label in new[]{"missing grant","different grant","higher minimum","grant on dismissal","grant on retrain"})
   tests.Run("Small later-level refuses "+label,()=>{var r=label=="grant on dismissal"?SmallDismissRequest():label=="grant on retrain"?SmallRetrainRequest():SmallLevelRequest();switch(label){
    case "missing grant":r.Progression.FixtureExperienceGrant=null;break;
    case "different grant":r.Progression.FixtureExperienceGrant=6001;break;
    case "higher minimum":r.Progression.MinimumRangerLevel=5;break;
    default:r.Progression.FixtureExperienceGrant=6000;break;
   }TestRunner.True(r.Validate().Count>0,"Unexpected XP fixture authority admitted.");});
  foreach(var label in new[]{"medium body","missing rig","dismissal quota","unmounted source"})
   tests.Run("Small Horse retrain rejects "+label,()=>{var r=SmallRetrainRequest();switch(label){
    case "medium body":r.Progression.BodySize="Medium";break;
    case "missing rig":r.Progression.SkeletonName=null;break;
    case "dismissal quota":r.Progression.ExpectedNativeWriteCount=0;break;
    case "unmounted source":r.Progression.SourceArchiveSha256=r.Progression.OwnedSource.SourceArchiveSha256;break;}
    TestRunner.True(r.Validate().Count>0,"Invalid Small retrain admitted.");});
  foreach(var label in new[]{"wrong lineage","wrong target","wrong control","producer aliases cold","cold aliases fixture","foreign manual","max native number","foreign campaign","write authority","missing cold pin"})
   tests.Run("Small dismissal rejects "+label,()=>{var r=SmallDismissRequest();var s=r.Progression;switch(label){
    case "wrong lineage":s.OwnedSource.Case="persistence-load";break;
    case "wrong target":s.ActorId=s.ControlActorId;break;case "wrong control":s.ControlPetId=s.PetId;break;
    case "producer aliases cold":s.InventoryRunId=s.SourceRunId;break;
    case "cold aliases fixture":s.InventoryRunId=s.OwnedSource.SourceRunId;break;
    case "foreign manual":r.PersistenceLoad.FileName="Manual_305_KMC_C7_WORLD.zks";break;
    case "max native number":r.PersistenceLoad.FileName="Manual_2147483647_KMC_C7_SMALL_WORLD.zks";break;
    case "foreign campaign":r.PersistenceLoad.GameId="99999999-1111-4111-8111-111111111111";break;
    case "write authority":s.ExpectedNativeWriteCount=1;break;
    case "missing cold pin":s.OwnedSource.CheckpointSourceArtifactSha256=null;break;
   }TestRunner.True(r.Validate().Count>0,"Foreign Small dismissal admission accepted.");});
  tests.Run("owned Manual reservation follows one real cold load and admits one new native slot",()=>{
   var r=OwnedRequest(Chunk7ProgressionSelection.OwnedLevelCase);
   var run=Path.Combine(root,Guid.NewGuid().ToString("N"));var saves=Path.Combine(run,"Saved Games");Directory.CreateDirectory(saves);
   var source=Path.Combine(saves,r.PersistenceLoad.FileName);File.WriteAllText(source,"synthetic owned checkpoint");
   string hash;using(var sha=SHA256.Create())using(var stream=File.OpenRead(source))hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
   r.PersistenceLoad.Sha256=hash;r.PersistenceLoad.Length=new FileInfo(source).Length;r.PersistenceLoad.LastWriteTimeUtcTicks=new FileInfo(source).LastWriteTimeUtc.Ticks;
   r.Progression.SourceArchiveSha256=hash;TestRunner.Equal(0,r.Validate().Count,"Exact owned Manual test request invalid.");
   var input=new PersistenceSaveEntry{FileName=r.PersistenceLoad.FileName,InternalName=r.PersistenceLoad.InternalName,SaveType="Manual",Area=r.PersistenceLoad.Area,InitialSha256=hash,Writable=false};
   var a=new PersistenceSaveAuthorization(run,r.PersistenceLoad.GameId,r.PersistenceLoad.GameName,new string('a',64),new[]{input},null,null,1,1);
   var game=new object();var manager=new object();a.ConfigureProgressionManual(r,game,manager);
   TestRunner.True(a.MatchesProgressionManualPlan(r,game,manager),"Initial owned Manual plan refused.");
   TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Write,new RuntimeSaveTarget{FileName="Manual_1_KMC_C7_PROGRESSION.zks",FullPath=Path.Combine(saves,"Manual_1_KMC_C7_PROGRESSION.zks"),InternalName="KMC_C7_PROGRESSION",SaveType="Manual",GameId=r.PersistenceLoad.GameId,GameName=r.PersistenceLoad.GameName,Area=r.PersistenceLoad.Area},saves)!=null,"Unreserved Manual write admitted.");
   var load=new RuntimeSaveTarget{FileName=r.PersistenceLoad.FileName,FullPath=source,InternalName=r.PersistenceLoad.InternalName,SaveType="Manual",GameId=r.PersistenceLoad.GameId,GameName=r.PersistenceLoad.GameName,Area=r.PersistenceLoad.Area};
   TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Load,load,saves)==null,"Owned checkpoint cold load refused.");
   a.ReserveProgressionManual(r,game,manager,1);TestRunner.Equal("Manual_1_KMC_C7_PROGRESSION.zks",a.ProgressionManualLeaf,"Native slot reservation changed.");
   TestRunner.True(a.MatchesProgressionManualPlan(r,game,manager),"Reserved Manual plan refused.");
   TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Write,new RuntimeSaveTarget{FileName=a.ProgressionManualLeaf,FullPath=Path.Combine(saves,a.ProgressionManualLeaf),InternalName="KMC_C7_PROGRESSION",SaveType="Manual",GameId=r.PersistenceLoad.GameId,GameName=r.PersistenceLoad.GameName,Area=r.PersistenceLoad.Area},saves)==null,"One reserved Manual write refused.");
   TestRunner.Equal(1,a.AdmittedNativeWriteRequests,"Manual quota changed.");
   TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Write,new RuntimeSaveTarget{FileName=a.ProgressionManualLeaf,FullPath=Path.Combine(saves,a.ProgressionManualLeaf),InternalName="KMC_C7_PROGRESSION",SaveType="Manual",GameId=r.PersistenceLoad.GameId,GameName=r.PersistenceLoad.GameName,Area=r.PersistenceLoad.Area},saves)!=null,"Second Manual write admitted.");
  });
  foreach(var selected in new[]{Chunk7ProgressionSelection.LevelCase,Chunk7ProgressionSelection.DismissCase,Chunk7ProgressionSelection.RetrainHorseCase,Chunk7ProgressionSelection.RetrainMammothCase})
   tests.Run("legacy progression DTO omits owned source "+selected,()=>{var r=Request(selected);var j=Chunk7EvidenceDto.Request(r.Progression);TestRunner.True(j.Property("ownedSource")==null,"Historical serialized shape expanded.");TestRunner.Equal(19,j.Properties().Count(),"Historical selection property count changed.");});
  tests.Run("declared pair role reversal remains exact",()=>{var r=OwnedRequest(Chunk7ProgressionSelection.OwnedLevelCase);var s=r.Progression;var a=s.ActorId;var p=s.PetId;s.ActorId=s.ControlActorId;s.PetId=s.ControlPetId;s.ControlActorId=a;s.ControlPetId=p;TestRunner.Equal(0,r.Validate().Count,"Exact declared reverse target role refused.");});
  foreach(var name in new[]{"missing nested","outer dispatch","source save case","nested write","second nested load","source run","source artifact","inventory run","inventory artifact","undeclared target","wrong target pet","remote declaration alias","non-base source","missing checkpoint","old family nested","old family changed descriptor","not minted campaign","unmounted ancestry bytes","working bytes","baseline bytes","zero number","leading-zero number","overflow number","foreign name","foreign archive hash","new level no body choices","new dismissal write","new retrain missing body"})
   tests.Run("owned progression rejects "+name,()=>{var r=OwnedRequest(name=="new dismissal write"?Chunk7ProgressionSelection.OwnedDismissCase:name=="new retrain missing body"?Chunk7ProgressionSelection.OwnedRetrainHorseCase:Chunk7ProgressionSelection.OwnedLevelCase);var s=r.Progression;switch(name){
    case "missing nested":s.OwnedSource=null;break;case "outer dispatch":r.OwnedPairs=s.OwnedSource;break;case "source save case":s.OwnedSource.Case="persistence-save";break;
    case "nested write":s.OwnedSource.ExpectedNativeWriteCount=1;break;case "second nested load":s.OwnedSource.ExpectedNativeLoadCount=2;break;
    case "source run":s.SourceRunId="other-checkpoint";break;case "source artifact":s.SourceArtifactSha256=new string('a',64);break;case "inventory run":s.InventoryRunId="other-inventory";break;case "inventory artifact":s.InventoryArtifactSha256=new string('a',64);break;
    case "undeclared target":s.ActorId="ffffffff-1111-4111-8111-111111111111";break;case "wrong target pet":s.PetId=s.ControlPetId;break;case "remote declaration alias":s.OwnedSource.PartyIds[0]=s.OwnedSource.PartyIds[1];break;
    case "non-base source":s.OwnedSource.BaseGameOnly=false;break;case "missing checkpoint":s.OwnedSource.CheckpointSourceRunId=null;break;
    case "old family nested":s.Case=Chunk7ProgressionSelection.LevelCase;break;case "old family changed descriptor":s.Case=Chunk7ProgressionSelection.LevelCase;s.OwnedSource=null;break;
    case "not minted campaign":r.PersistenceLoad.GameId=r.Fixture.Working.GameId;break;case "unmounted ancestry bytes":s.SourceArchiveSha256=s.OwnedSource.SourceArchiveSha256;r.PersistenceLoad.Sha256=s.SourceArchiveSha256;break;
    case "working bytes":s.SourceArchiveSha256=r.Fixture.Working.Sha256;r.PersistenceLoad.Sha256=s.SourceArchiveSha256;break;case "baseline bytes":s.SourceArchiveSha256=r.Fixture.Baseline.Sha256;r.PersistenceLoad.Sha256=s.SourceArchiveSha256;break;
    case "zero number":r.PersistenceLoad.FileName="Manual_0_KMC_C7_WORLD.zks";break;case "leading-zero number":r.PersistenceLoad.FileName="Manual_0404_KMC_C7_WORLD.zks";break;case "overflow number":r.PersistenceLoad.FileName="Manual_2147483648_KMC_C7_WORLD.zks";break;
    case "foreign name":r.PersistenceLoad.InternalName="KMC_C7_ACQUIRED_FIELD";break;case "foreign archive hash":r.PersistenceLoad.Sha256=new string('f',64);break;
    case "new level no body choices":s.BodySize="Medium";break;case "new dismissal write":s.ExpectedNativeWriteCount=1;break;case "new retrain missing body":s.SkeletonName=null;break;
   }TestRunner.True(r.Validate().Count>0,"Changed owned admission accepted.");});
  return tests.Complete();
 }
 private static RuntimeRequest OwnedRequest(string selected){
  var op=selected==Chunk7ProgressionSelection.OwnedLevelCase?Chunk7ProgressionSelection.LevelCase:selected==Chunk7ProgressionSelection.OwnedDismissCase?Chunk7ProgressionSelection.DismissCase:selected==Chunk7ProgressionSelection.OwnedRetrainHorseCase?Chunk7ProgressionSelection.RetrainHorseCase:Chunk7ProgressionSelection.RetrainMammothCase;
  var r=Request(op);var s=r.Progression;s.Case=selected;s.NativeWriteLeaf=null;r.PersistenceLoad.InternalName="KMC_C7_WORLD";r.PersistenceLoad.FileName="Manual_404_KMC_C7_WORLD.zks";r.PersistenceLoad.GameId="99999999-1111-4111-8111-111111111111";r.PersistenceLoad.GameName="Observed isolated campaign";r.PersistenceLoad.Area=new string('c',32);
  s.OwnedSource=new Chunk7OwnedPairsSelection{Case="persistence-load",SourceRunId=s.InventoryRunId,SourceArtifactSha256=s.InventoryArtifactSha256,SourceArchiveSha256=new string('c',64),CheckpointSourceRunId=s.SourceRunId,CheckpointSourceArtifactSha256=s.SourceArtifactSha256,RiderAId=s.ControlActorId,MountAId=s.ControlPetId,RiderBId=s.ActorId,MountBId=s.PetId,PartyIds=new[]{s.ControlActorId,s.ControlPetId,s.ActorId,s.PetId,"88888888-1111-4111-8111-111111111111"},BaseGameOnly=true,ExpectedNativeLoadCount=1,ExpectedNativeWriteCount=0};return r;
 }
 private static RuntimeRequest SmallDismissRequest(){
  var r=OwnedRequest(Chunk7ProgressionSelection.OwnedDismissCase);var s=r.Progression;
  s.Case=Chunk7ProgressionSelection.SmallDismissCase;s.ActorId=Chunk7OwnedPairsSelection.SmallFixtureRider;
  s.PetId=Chunk7OwnedPairsSelection.SmallFixtureHorse;s.ControlActorId=Chunk7SmallFixtureSelection.MainActorId;
  s.ControlPetId=Chunk7OwnedPairsSelection.SmallFixtureMainHorse;s.SourceRunId="small-mounted-source";
  s.SourceArtifactSha256=new string('d',64);s.SourceArchiveSha256=new string('e',64);
  s.InventoryRunId="small-mounted-cold";s.InventoryArtifactSha256=new string('f',64);
  s.OwnedSource=new Chunk7OwnedPairsSelection{Case=Chunk7OwnedPairsSelection.SmallFixtureLoadCase,
   SourceRunId=Chunk7OwnedPairsSelection.SmallFixtureProducerRun,
   SourceArtifactSha256=Chunk7OwnedPairsSelection.SmallFixtureProducerArtifact,
   SourceArchiveSha256=Chunk7OwnedPairsSelection.SmallFixtureProducerArchive,
   CheckpointSourceRunId=s.SourceRunId,CheckpointSourceArtifactSha256=s.SourceArtifactSha256,
   SeedId=Chunk7SmallFixtureSelection.SeedIdentity,SeedSha256=Chunk7SmallFixtureSelection.SeedHash,
   ColdReceiptSha256=Chunk7OwnedPairsSelection.SmallFixtureColdReceipt,
   RiderAId=s.ControlActorId,MountAId=s.ControlPetId,RiderBId=s.ActorId,MountBId=s.PetId,
   PartyIds=new[]{s.ControlActorId,"5dd7bd1d-ae4c-4df7-9687-50fceb027344","8265f8cf-0969-4ec7-a266-2448de91c8c4",
    "fd213210-71cd-41b0-b35b-68e1b18e9b43",s.ActorId,s.ControlPetId,s.PetId},
   BaseGameOnly=true,ExpectedNativeLoadCount=1,ExpectedNativeWriteCount=0};
  r.PersistenceLoad.InternalName=Chunk7OwnedPairsSelection.SmallFixtureManualName;
  r.PersistenceLoad.FileName="Manual_305_KMC_C7_SMALL_WORLD.zks";r.PersistenceLoad.Sha256=s.SourceArchiveSha256;
  r.PersistenceLoad.GameId=Chunk7SmallFixtureSelection.CampaignId;
  r.PersistenceLoad.GameName=Chunk7SmallFixtureSelection.CampaignName;
  r.PersistenceLoad.Area=Chunk7SmallFixtureSelection.AreaId;
  return r;
 }
 private static RuntimeRequest SmallRetrainRequest(){
  var r=SmallDismissRequest();var s=r.Progression;
  s.Case=Chunk7ProgressionSelection.SmallRetrainHorseCase;
  s.RaceGuid=Chunk7SmallFixtureSelection.RaceGuid;s.PresetGuid=Chunk7SmallFixtureSelection.PresetGuid;
  s.SkeletonName=Chunk7SmallFixtureSelection.SkeletonName;s.Gender="Male";s.LeftHanded=false;s.BodySize="Small";
  s.MinimumRangerLevel=4;s.ExpectedNativeWriteCount=1;
  return r;
 }
 private static RuntimeRequest SmallLevelRequest(){
  var r=SmallDismissRequest();var s=r.Progression;
  s.Case=Chunk7ProgressionSelection.SmallLevelCase;
  s.MinimumRangerLevel=4;s.ExpectedNativeWriteCount=1;s.FixtureExperienceGrant=6000;
  return r;
 }
 private static RuntimeRequest MediumMammothRequest(){
  var r=SmallDismissRequest();var s=r.Progression;
  s.Case=Chunk7ProgressionSelection.MediumRetrainMammothCase;
  s.ActorId=Chunk7OwnedPairsSelection.MediumFixtureRider;s.PetId=Chunk7OwnedPairsSelection.MediumFixtureHorse;
  s.ControlPetId=Chunk7OwnedPairsSelection.MediumFixtureMainHorse;
  s.RaceGuid=Chunk7SmallFixtureSelection.MediumRaceGuid;s.PresetGuid=Chunk7SmallFixtureSelection.MediumPresetGuid;
  s.SkeletonName=Chunk7SmallFixtureSelection.MediumSkeletonName;s.Gender="Male";s.LeftHanded=false;s.BodySize="Medium";
  s.MinimumRangerLevel=10;s.ExpectedNativeWriteCount=1;s.FixtureExperienceGrant=96000;
  s.OwnedSource.Case=Chunk7OwnedPairsSelection.MediumFixtureLoadCase;
  s.OwnedSource.SourceRunId=Chunk7OwnedPairsSelection.MediumFixtureProducerRun;
  s.OwnedSource.SourceArtifactSha256=Chunk7OwnedPairsSelection.MediumFixtureProducerArtifact;
  s.OwnedSource.SourceArchiveSha256=Chunk7OwnedPairsSelection.MediumFixtureProducerArchive;
  s.OwnedSource.ColdReceiptSha256=Chunk7OwnedPairsSelection.MediumFixtureColdArtifact;
  s.OwnedSource.MountAId=s.ControlPetId;s.OwnedSource.RiderBId=s.ActorId;s.OwnedSource.MountBId=s.PetId;
  s.OwnedSource.PartyIds=new[]{s.ControlActorId,"5dd7bd1d-ae4c-4df7-9687-50fceb027344","8265f8cf-0969-4ec7-a266-2448de91c8c4",
   "fd213210-71cd-41b0-b35b-68e1b18e9b43",s.ActorId,s.ControlPetId,s.PetId};
  r.PersistenceLoad.InternalName=Chunk7OwnedPairsSelection.MediumFixtureManualName;
  r.PersistenceLoad.FileName="Manual_305_KMC_C7_MEDIUM_WORLD.zks";
  return r;
 }
 private static RuntimeRequest Request(string selected){var f=new Fixture();var respec=selected==Chunk7ProgressionSelection.RetrainHorseCase||selected==Chunk7ProgressionSelection.RetrainMammothCase;var writes=selected!=Chunk7ProgressionSelection.DismissCase;
  return new RuntimeRequest{SchemaVersion=2,RunId="progression-test",Scenario=Chunk7ProgressionSelection.Scenario,Progression=new Chunk7ProgressionSelection{Case=selected,SourceRunId="manual300-source",SourceArchiveSha256=f.Source.Sha256,SourceArtifactSha256=new string('d',64),InventoryRunId="world-inventory",InventoryArtifactSha256=new string('e',64),
   ActorId="d17c8fd0-6627-40c4-bf9a-024cf001c1f7",PetId="44444444-1111-4111-8111-111111111111",ControlActorId="b6628a77-4962-47a4-a17c-88d9836fc9d5",ControlPetId="d79a4f6c-b74e-4868-95bd-533899131acb",
   RaceGuid=respec?new string('a',32):null,PresetGuid=respec?new string('b',32):null,SkeletonName=respec?"ExactObservedMediumRig":null,Gender=respec?"Male":null,LeftHanded=respec?(bool?)false:null,BodySize=respec?"Medium":null,MinimumRangerLevel=writes?4:0,ExpectedNativeWriteCount=writes?1:0,NativeWriteLeaf=writes?"Auto_1.zks":null},
  PersistenceLoad=f.Source,Branch="codex/mounted-combat-chunk7-foundation",Commit=new string('1',40),ProductVersion=BuildIdentity.ProductVersion,DllSha256=new string('2',64),DllMvid=Campaign,TransactionToken=new string('3',64),EvidenceRoot=root,
  Fixture=new RuntimeFixtureIdentity{Baseline=Descriptor("KMC_AUTOMATION_BASELINE","Manual_298_KMC_AUTOMATION_BASELINE.zks",new string('a',64)),Working=Descriptor("KMC_AUTOMATION_WORKING","Manual_299_KMC_AUTOMATION_WORKING.zks",new string('b',64)),WriteAuthorization=new RuntimeSaveWriteAuthorization{Mode="working-only",AllowedInternalName="KMC_AUTOMATION_WORKING",AllowedFileName="Manual_299_KMC_AUTOMATION_WORKING.zks",BaselineImmutable=true}},QualificationSuite=new RuntimeQualificationSuiteIdentity{SuiteId="progression-suite",SnapshotSha256=new string('4',64)}};}
 private static RuntimeSaveDescriptor Descriptor(string name,string leaf,string hash)=>new RuntimeSaveDescriptor{InternalName=name,FileName=leaf,Sha256=hash,Length=10,LastWriteTimeUtcTicks=638907120000000000L,GameId=Campaign,GameName="Fixture",Area=Area};
 private sealed class Fixture{
  internal string Run,Saves;internal RuntimeSaveDescriptor Source;private string hash;
  internal Fixture(){Run=Path.Combine(root,Guid.NewGuid().ToString("N"));Saves=Path.Combine(Run,"Saved Games");Directory.CreateDirectory(Saves);var path=Path.Combine(Saves,Chunk7ProgressionSelection.SourceLeaf);File.WriteAllText(path,"synthetic Manual300 input");using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();Source=Descriptor(Chunk7ProgressionSelection.SourceName,Chunk7ProgressionSelection.SourceLeaf,hash);Source.Length=new FileInfo(path).Length;Source.LastWriteTimeUtcTicks=new FileInfo(path).LastWriteTimeUtc.Ticks;}
  internal PersistenceSaveEntry[] Entries(bool writes){var input=new PersistenceSaveEntry{FileName=Source.FileName,InternalName=Source.InternalName,SaveType="Manual",Area=Area,InitialSha256=hash,Writable=false};return writes?new[]{input,new PersistenceSaveEntry{FileName="Auto_1.zks",InternalName="Autosave",SaveType="Auto",Area=Area,Writable=true}}:new[]{input};}
  internal PersistenceSaveAuthorization Scope(bool writes)=>new PersistenceSaveAuthorization(Run,Campaign,"Fixture",new string('a',64),Entries(writes),null,null,writes?(int?)1:null,1);
  internal RuntimeSaveTarget Load=>new RuntimeSaveTarget{FileName=Source.FileName,FullPath=Path.Combine(Saves,Source.FileName),InternalName=Source.InternalName,SaveType="Manual",GameId=Campaign,GameName="Fixture",Area=Area};
  internal RuntimeSaveTarget Write=>new RuntimeSaveTarget{FileName="Auto_1.zks",FullPath=Path.Combine(Saves,"Auto_1.zks"),InternalName="Autosave",SaveType="Auto",GameId=Campaign,GameName="Fixture",Area=Area};
 }
}}
