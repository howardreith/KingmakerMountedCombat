using System;
using System.Linq;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
namespace KingmakerMountedCombat.Tests {
internal static class SmallCalibrationProtocolTests {
 private const string Campaign="11111111-1111-4111-8111-111111111111",Area="0123456789abcdef0123456789abcdef";
 private static string root;
 private static RuntimeRequest Request(){var r=FieldRequest();r.Scenario=Chunk7SmallCalibrationSelection.Scenario;r.SmallCalibration=new Chunk7SmallCalibrationSelection{Case=Chunk7SmallCalibrationSelection.CaseName,Source=r.OrdinaryField,ProfileId="small-synthetic-horse-v1",NativeInventorySha256=new string('1',64),NativeCalibrationSha256=new string('2',64)};r.OrdinaryField=null;return r;}
 private static int Main(string[] args){root=args.Single();var tests=new TestRunner();
  tests.Run("closed Small request accepts exact read-only paid source shape",()=>TestRunner.Equal(0,Request().Validate().Count,"Valid request refused."));
  tests.Run("unchanged field neighbor accepts its existing protocol",()=>TestRunner.Equal(0,FieldRequest().Validate().Count,"Field neighbor altered."));
  foreach(var name in new[]{"missing selection","foreign family","field selection","missing source","self source","foreign archive","working archive","wrong leaf","write case","unknown profile","unknown case","missing declaration","missing body inventory","same actor"})
   tests.Run("Small protocol rejects "+name,()=>{var r=Request();switch(name){
    case "missing selection":r.SmallCalibration=null;break;case "foreign family":r.Scenario="chunk7-two-pair-exploration";break;case "field selection":r.OrdinaryField=r.SmallCalibration.Source;break;
    case "missing source":r.SmallCalibration.Source=null;break;case "self source":r.SmallCalibration.Source.SourceRunId=r.RunId;break;case "foreign archive":r.PersistenceLoad.Sha256=new string('9',64);break;
    case "working archive":r.PersistenceLoad=r.Fixture.Working;break;case "wrong leaf":r.PersistenceLoad.FileName="Auto_2.zks";break;case "write case":r.PersistenceCase="multi-exploration-manual";break;
    case "unknown profile":r.SmallCalibration.ProfileId=MountedRiderProfileCatalog.MediumHorseId;break;case "unknown case":r.SmallCalibration.Case="any";break;case "missing declaration":r.SmallCalibration.NativeCalibrationSha256=null;break;
    case "missing body inventory":r.SmallCalibration.NativeInventorySha256=null;break;case "same actor":r.SmallCalibration.Source.ActorId=r.SmallCalibration.Source.PetId;break;}
    TestRunner.True(r.Validate().Count>0,"Invalid Small request accepted.");});
  tests.Run("synthetic Small profile refuses native admission without insertion",()=>{var s=Request().SmallCalibration;var count=MountedRiderProfileCatalog.Small.Count;bool refused=false;try{s.RequireCompiledDeclaration();}catch(InvalidOperationException){refused=true;}TestRunner.True(refused,"Synthetic Small profile entered the measured compiled catalogue.");TestRunner.Equal(count,MountedRiderProfileCatalog.Small.Count,"Request altered the compiled catalogue.");});
  tests.Run("immutable Small result registration excludes unknown rows",()=>{foreach(var name in new[]{"C7-small-readonly-source","C7-small-native-party-placement","C7-small-production-mount","C7-small-native-ground","C7-small-rider-primary","C7-small-mount-primary","C7-small-native-dismount","C7-small-native-pose-observation","C7-small-cleanup","C7-small-runtime","C7-small-interrupted"})TestRunner.True(RuntimeSubscenarioResult.IsMissionScenario(name),"Unknown row: "+name);TestRunner.True(!RuntimeSubscenarioResult.IsMissionScenario("C7-small-any"),"Unknown row admitted.");});
  tests.Run("Small mounted save selection accepts only its pinned source",()=>TestRunner.Equal(0,SmallMountedSelection(false).Validate(Chunk7OwnedPairsSelection.Scenario,"small-save-run","multi-exploration-manual").Count,"Qualified save selection refused."));
  tests.Run("Small mounted cold selection accepts distinct producer",()=>TestRunner.Equal(0,SmallMountedSelection(true).Validate(Chunk7OwnedPairsSelection.Scenario,"small-cold-run","multi-exploration-manual").Count,"Qualified cold selection refused."));
  tests.Run("Small mounted cold selection refuses seed as producer",()=>{var s=SmallMountedSelection(true);s.CheckpointSourceRunId=s.SeedId;TestRunner.True(s.Validate(Chunk7OwnedPairsSelection.Scenario,"small-cold-run","multi-exploration-manual").Count>0,"Seed alias accepted as cold producer.");});
  tests.Run("Small mounted save request accepts exact F09 descriptor",()=>TestRunner.Equal(0,SmallMountedRequest(false).Validate().Count,"Qualified mounted save request refused."));
  tests.Run("Small mounted cold request accepts distinct mounted Manual",()=>TestRunner.Equal(0,SmallMountedRequest(true).Validate().Count,"Qualified mounted cold request refused."));
  tests.Run("third-Horse setup admits exact unmounted Small source with zero writes",()=>{
   var r=SmallThirdHorseRequest();TestRunner.Equal(0,r.Validate().Count,"Exact third-Horse setup request refused.");
   TestRunner.Equal("chunk7-two-pair-exploration",r.SelectedChunk7Scenario,"Third-Horse setup routed through another native host.");
  });
  tests.Run("fourth-Horse request admits only the distinct accepted eight-actor Manual",()=>{
   var r=FourthHorseRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));
   TestRunner.Equal("chunk7-two-pair-exploration",r.SelectedChunk7Scenario,"Fourth-Horse setup routed through another native host.");
   TestRunner.Equal(0,r.OwnedPairs.WriteCount(r.PersistenceCase),"Fourth-Horse setup gained a native write.");
  });
  foreach(var bad in new[]{"seven-actor roster","different cold receipt","different Manual","write","checkpoint","foreign case"})
   tests.Run("fourth-Horse request refuses "+bad,()=>{var r=FourthHorseRequest();switch(bad){
    case "seven-actor roster":r.OwnedPairs.PartyIds=r.OwnedPairs.PartyIds.Take(7).ToArray();break;
    case "different cold receipt":r.OwnedPairs.ColdReceiptSha256=new string('7',64);break;
    case "different Manual":r.PersistenceLoad.FileName="Manual_306_KMC_C7_SMALL_WORLD.zks";break;
    case "write":r.OwnedPairs.ExpectedNativeWriteCount=1;break;
    case "checkpoint":r.PersistenceCase="multi-exploration-manual";break;
    case "foreign case":r.OwnedPairs.Case="exploration";break;
   }TestRunner.True(r.Validate().Count>0,"Changed fourth-Horse custody or authority was accepted.");});
  tests.Run("third-Horse native Manual admits exact source and one reserved write",()=>{
   var r=SmallThirdHorseManualRequest();TestRunner.Equal(0,r.Validate().Count,"Exact third-Horse Manual request refused.");
   TestRunner.Equal("chunk7-two-pair-exploration",r.SelectedChunk7Scenario,"Third-Horse Manual routed through another native host.");
   TestRunner.Equal(1,r.OwnedPairs.WriteCount(r.PersistenceCase),"Third-Horse Manual lost its exact write quota.");
   TestRunner.Equal(Chunk7OwnedPairsSelection.SmallFixtureManualName,
    Chunk7PersistenceScenario.NativeName(Kingmaker.EntitySystem.Persistence.SaveInfo.SaveType.Manual,r.OwnedPairs,r.PersistenceCase),
    "Native bootstrap chose a different Manual name from the guarded output reservation.");
  });
  foreach(var bad in new[]{"missing checkpoint","zero write","foreign source","borrowed cold parent"})
   tests.Run("third-Horse native Manual refuses "+bad,()=>{var r=SmallThirdHorseManualRequest();switch(bad){
    case "missing checkpoint":r.PersistenceCase=null;break;
    case "zero write":r.OwnedPairs.ExpectedNativeWriteCount=0;break;
    case "foreign source":r.PersistenceLoad.Sha256=new string('8',64);break;
    case "borrowed cold parent":r.OwnedPairs.CheckpointSourceRunId="foreign";r.OwnedPairs.CheckpointSourceArtifactSha256=new string('8',64);break;
   }TestRunner.True(r.Validate().Count>0,"Third-Horse Manual accepted altered source or authority.");});
  foreach(var bad in new[]{"checkpoint","write","cold-source","foreign-source"})
   tests.Run("third-Horse setup refuses "+bad,()=>{var r=SmallThirdHorseRequest();switch(bad){
    case "checkpoint":r.PersistenceCase="multi-exploration-manual";break;
    case "write":r.OwnedPairs.ExpectedNativeWriteCount=1;break;
    case "cold-source":r.PersistenceLoad.InternalName=Chunk7OwnedPairsSelection.SmallFixtureManualName;r.PersistenceLoad.FileName="Manual_305_KMC_C7_SMALL_WORLD.zks";break;
    case "foreign-source":r.PersistenceLoad.Sha256=new string('8',64);break;
   }TestRunner.True(r.Validate().Count>0,"Third-Horse setup accepted altered source or authority.");});
  tests.Run("Small mounted request refuses out-of-range source Manual",()=>{var r=SmallMountedRequest(false);r.PersistenceLoad.FileName="Manual_2147483648_KMC_C7_SMALL_FIXTURE.zks";TestRunner.True(r.Validate().Count>0,"Out-of-range source Manual admitted.");});
  tests.Run("Small mounted request refuses out-of-range cold Manual",()=>{var r=SmallMountedRequest(true);r.PersistenceLoad.FileName="Manual_2147483648_KMC_C7_SMALL_WORLD.zks";TestRunner.True(r.Validate().Count>0,"Out-of-range cold Manual admitted.");});
  tests.Run("Non-Small owned request refuses F09 cold receipt",()=>{var r=SmallMountedRequest(false);r.OwnedPairs.Case="persistence-save";TestRunner.True(r.Validate().Any(e=>e=="Only paid fixture persistence or the exact fourth-Horse source carries an accepted cold receipt."),"Historical family accepted Small-only receipt.");});
  tests.Run("Medium mounted save request accepts the paid Human/Horse Manual",()=>TestRunner.Equal(0,MediumMountedRequest(false).Validate().Count,"Paid Medium source refused."));
  tests.Run("Medium mounted cold request accepts a distinct mounted Manual",()=>TestRunner.Equal(0,MediumMountedRequest(true).Validate().Count,"Paid Medium cold source refused."));
  tests.Run("Medium mounted cold refuses the unmounted source archive",()=>{var r=MediumMountedRequest(true);r.PersistenceLoad.Sha256=Chunk7OwnedPairsSelection.MediumFixtureProducerArchive;TestRunner.True(r.Validate().Count>0,"Unchanged paid source impersonated mounted checkpoint.");});
  tests.Run("Medium mounted output accepts only a canonical Manual",()=>{TestRunner.True(Chunk7OwnedPairsSelection.IsMediumFixtureManualLeaf("Manual_305_KMC_C7_MEDIUM_WORLD.zks"),"Canonical Medium Manual refused.");TestRunner.True(!Chunk7OwnedPairsSelection.IsMediumFixtureManualLeaf("Manual_0305_KMC_C7_MEDIUM_WORLD.zks"),"Noncanonical Medium Manual admitted.");});
  foreach(var leaf in new[]{"Manual_305_KMC_C7_SMALL_WORLD.zks","Manual_1_KMC_C7_SMALL_WORLD.zks"})
   tests.Run("Small mounted output accepts canonical "+leaf,()=>TestRunner.True(Chunk7OwnedPairsSelection.IsSmallFixtureManualLeaf(leaf),"Canonical native Manual refused."));
  foreach(var leaf in new[]{"Manual_2147483648_KMC_C7_SMALL_WORLD.zks","Manual_2147483647_KMC_C7_SMALL_WORLD.zks","Manual_0305_KMC_C7_SMALL_WORLD.zks","Auto_305_KMC_C7_SMALL_WORLD.zks","Manual_305_KMC_C7_SMALL_FIXTURE.zks"})
   tests.Run("Small mounted output refuses invalid "+leaf,()=>TestRunner.True(!Chunk7OwnedPairsSelection.IsSmallFixtureManualLeaf(leaf),"Invalid native Manual admitted."));
  return tests.Complete();
 }
 private static Chunk7OwnedPairsSelection SmallMountedSelection(bool cold){return new Chunk7OwnedPairsSelection{Case=cold?Chunk7OwnedPairsSelection.SmallFixtureLoadCase:Chunk7OwnedPairsSelection.SmallFixtureSaveCase,
  SourceRunId=Chunk7OwnedPairsSelection.SmallFixtureProducerRun,SourceArtifactSha256=Chunk7OwnedPairsSelection.SmallFixtureProducerArtifact,
  SourceArchiveSha256=Chunk7OwnedPairsSelection.SmallFixtureProducerArchive,ColdReceiptSha256=Chunk7OwnedPairsSelection.SmallFixtureColdReceipt,
  SeedId=Chunk7SmallFixtureSelection.SeedIdentity,SeedSha256=Chunk7SmallFixtureSelection.SeedHash,
  CheckpointSourceRunId=cold?"small-save-run":null,CheckpointSourceArtifactSha256=cold?new string('5',64):null,
  RiderAId=Chunk7SmallFixtureSelection.MainActorId,MountAId=Chunk7OwnedPairsSelection.SmallFixtureMainHorse,
  RiderBId=Chunk7OwnedPairsSelection.SmallFixtureRider,MountBId=Chunk7OwnedPairsSelection.SmallFixtureHorse,
  PartyIds=new[]{Chunk7SmallFixtureSelection.MainActorId,Chunk7OwnedPairsSelection.SmallFixtureMainHorse,
   Chunk7OwnedPairsSelection.SmallFixtureRider,Chunk7OwnedPairsSelection.SmallFixtureHorse,
   "11111111-1111-4111-8111-111111111111","22222222-2222-4222-8222-222222222222","33333333-3333-4333-8333-333333333333"},
  BaseGameOnly=true,ExpectedNativeLoadCount=1,ExpectedNativeWriteCount=cold?0:1};}
 private static RuntimeRequest SmallMountedRequest(bool cold){var r=FieldRequest();r.RunId=cold?"small-cold-run":"small-save-run";
  r.Scenario=Chunk7OwnedPairsSelection.Scenario;r.OrdinaryField=null;r.PersistenceCase="multi-exploration-manual";
  r.OwnedPairs=SmallMountedSelection(cold);
  r.PersistenceLoad=Descriptor(cold?Chunk7OwnedPairsSelection.SmallFixtureManualName:Chunk7SmallFixtureSelection.OutputName,
   cold?"Manual_305_KMC_C7_SMALL_WORLD.zks":"Manual_304_KMC_C7_SMALL_FIXTURE.zks",
   cold?new string('7',64):Chunk7OwnedPairsSelection.SmallFixtureProducerArchive);
  r.PersistenceLoad.GameId=Chunk7SmallFixtureSelection.CampaignId;r.PersistenceLoad.GameName=Chunk7SmallFixtureSelection.CampaignName;
  r.PersistenceLoad.Area=Chunk7SmallFixtureSelection.AreaId;return r;}
 private static RuntimeRequest SmallThirdHorseRequest(){var r=SmallMountedRequest(false);
  r.RunId="small-third-horse-run";r.PersistenceCase=null;
  r.OwnedPairs.Case=Chunk7OwnedPairsSelection.SmallThirdHorseCase;
  r.OwnedPairs.ExpectedNativeWriteCount=0;return r;}
 private static RuntimeRequest SmallThirdHorseManualRequest(){var r=SmallMountedRequest(false);
  r.RunId="small-third-horse-manual-run";
  r.OwnedPairs.Case=Chunk7OwnedPairsSelection.SmallThirdHorseSaveCase;return r;}
 private static RuntimeRequest FourthHorseRequest(){var r=SmallThirdHorseRequest();
  r.RunId="fourth-horse-run";
  r.OwnedPairs.Case=Chunk7OwnedPairsSelection.FourthHorseCase;
  r.OwnedPairs.SourceRunId=Chunk7OwnedPairsSelection.FourthHorseSourceRun;
  r.OwnedPairs.SourceArtifactSha256=Chunk7OwnedPairsSelection.FourthHorseSourceArtifact;
  r.OwnedPairs.SourceArchiveSha256=Chunk7OwnedPairsSelection.FourthHorseSourceArchive;
  r.OwnedPairs.ColdReceiptSha256=Chunk7OwnedPairsSelection.FourthHorseColdArtifact;
  r.OwnedPairs.PartyIds=new[]{Chunk7SmallFixtureSelection.MainActorId,Chunk7OwnedPairsSelection.SmallFixtureMainHorse,
   Chunk7OwnedPairsSelection.SmallFixtureRider,Chunk7OwnedPairsSelection.SmallFixtureHorse,
   Chunk7OwnedPairsSelection.FourthHorsePriorRider,Chunk7OwnedPairsSelection.FourthHorsePriorMount,
   Chunk7OwnedPairsSelection.FourthHorseTargetRider,"8265f8cf-0969-4ec7-a266-2448de91c8c4"};
  r.PersistenceLoad=Descriptor(Chunk7OwnedPairsSelection.SmallFixtureManualName,
   "Manual_305_KMC_C7_SMALL_WORLD.zks",Chunk7OwnedPairsSelection.FourthHorseSourceArchive);
  r.PersistenceLoad.GameId=Chunk7SmallFixtureSelection.CampaignId;
  r.PersistenceLoad.GameName=Chunk7SmallFixtureSelection.CampaignName;
  r.PersistenceLoad.Area=Chunk7SmallFixtureSelection.AreaId;return r;}
 private static RuntimeRequest MediumMountedRequest(bool cold){var r=FieldRequest();r.RunId=cold?"medium-cold-run":"medium-save-run";
  r.Scenario=Chunk7OwnedPairsSelection.Scenario;r.OrdinaryField=null;r.PersistenceCase="multi-exploration-manual";
  r.OwnedPairs=new Chunk7OwnedPairsSelection{Case=cold?Chunk7OwnedPairsSelection.MediumFixtureLoadCase:Chunk7OwnedPairsSelection.MediumFixtureSaveCase,
   SourceRunId=Chunk7OwnedPairsSelection.MediumFixtureProducerRun,SourceArtifactSha256=Chunk7OwnedPairsSelection.MediumFixtureProducerArtifact,
   SourceArchiveSha256=Chunk7OwnedPairsSelection.MediumFixtureProducerArchive,ColdReceiptSha256=Chunk7OwnedPairsSelection.MediumFixtureColdArtifact,
   SeedId=Chunk7SmallFixtureSelection.SeedIdentity,SeedSha256=Chunk7SmallFixtureSelection.SeedHash,
   CheckpointSourceRunId=cold?"medium-save-run":null,CheckpointSourceArtifactSha256=cold?new string('5',64):null,
   RiderAId=Chunk7SmallFixtureSelection.MainActorId,MountAId=Chunk7OwnedPairsSelection.MediumFixtureMainHorse,
   RiderBId=Chunk7OwnedPairsSelection.MediumFixtureRider,MountBId=Chunk7OwnedPairsSelection.MediumFixtureHorse,
   PartyIds=new[]{Chunk7SmallFixtureSelection.MainActorId,Chunk7OwnedPairsSelection.MediumFixtureMainHorse,
    Chunk7OwnedPairsSelection.MediumFixtureRider,Chunk7OwnedPairsSelection.MediumFixtureHorse,
    "11111111-1111-4111-8111-111111111111","22222222-2222-4222-8222-222222222222","33333333-3333-4333-8333-333333333333"},
   BaseGameOnly=true,ExpectedNativeLoadCount=1,ExpectedNativeWriteCount=cold?0:1};
  r.PersistenceLoad=Descriptor(cold?Chunk7OwnedPairsSelection.MediumFixtureManualName:Chunk7SmallFixtureSelection.MediumOutputName,
   cold?"Manual_305_KMC_C7_MEDIUM_WORLD.zks":"Manual_304_KMC_C7_MEDIUM_FIXTURE.zks",
   cold?new string('7',64):Chunk7OwnedPairsSelection.MediumFixtureProducerArchive);
  r.PersistenceLoad.GameId=Chunk7SmallFixtureSelection.CampaignId;r.PersistenceLoad.GameName=Chunk7SmallFixtureSelection.CampaignName;
  r.PersistenceLoad.Area=Chunk7SmallFixtureSelection.AreaId;return r;}
 private static RuntimeRequest FieldRequest(){var source=Descriptor("Autosave","Auto_1.zks",new string('f',64));return new RuntimeRequest{SchemaVersion=2,RunId="field-test",Scenario=Chunk7OrdinaryFieldSelection.Scenario,
  OrdinaryField=new Chunk7OrdinaryFieldSelection{Case=Chunk7OrdinaryFieldSelection.CaseName,SourceRunId="paid-source",SourceArchiveSha256=source.Sha256,SourceArtifactSha256=new string('d',64),ActorId="33333333-1111-4111-8111-111111111111",PetId="44444444-1111-4111-8111-111111111111"},
  PersistenceLoad=source,Branch="codex/mounted-combat-chunk7-foundation",Commit=new string('1',40),ProductVersion=BuildIdentity.ProductVersion,DllSha256=new string('2',64),DllMvid=Campaign,TransactionToken=new string('3',64),EvidenceRoot=root,
  Fixture=new RuntimeFixtureIdentity{Baseline=Descriptor("KMC_AUTOMATION_BASELINE","Manual_298_KMC_AUTOMATION_BASELINE.zks",new string('a',64)),Working=Descriptor("KMC_AUTOMATION_WORKING","Manual_299_KMC_AUTOMATION_WORKING.zks",new string('b',64)),WriteAuthorization=new RuntimeSaveWriteAuthorization{Mode="working-only",AllowedInternalName="KMC_AUTOMATION_WORKING",AllowedFileName="Manual_299_KMC_AUTOMATION_WORKING.zks",BaselineImmutable=true}},
  QualificationSuite=new RuntimeQualificationSuiteIdentity{SuiteId="field-suite",SnapshotSha256=new string('4',64)}};}
 private static RuntimeSaveDescriptor Descriptor(string name,string leaf,string hash)=>new RuntimeSaveDescriptor{InternalName=name,FileName=leaf,Sha256=hash,Length=10,LastWriteTimeUtcTicks=638907120000000000L,GameId=Campaign,GameName="Fixture",Area=Area};
}}
