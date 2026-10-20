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
  return tests.Complete();
 }
 private static RuntimeRequest FieldRequest(){var source=Descriptor("Autosave","Auto_1.zks",new string('f',64));return new RuntimeRequest{SchemaVersion=2,RunId="field-test",Scenario=Chunk7OrdinaryFieldSelection.Scenario,
  OrdinaryField=new Chunk7OrdinaryFieldSelection{Case=Chunk7OrdinaryFieldSelection.CaseName,SourceRunId="paid-source",SourceArchiveSha256=source.Sha256,SourceArtifactSha256=new string('d',64),ActorId="33333333-1111-4111-8111-111111111111",PetId="44444444-1111-4111-8111-111111111111"},
  PersistenceLoad=source,Branch="codex/mounted-combat-chunk7-foundation",Commit=new string('1',40),ProductVersion=BuildIdentity.ProductVersion,DllSha256=new string('2',64),DllMvid=Campaign,TransactionToken=new string('3',64),EvidenceRoot=root,
  Fixture=new RuntimeFixtureIdentity{Baseline=Descriptor("KMC_AUTOMATION_BASELINE","Manual_298_KMC_AUTOMATION_BASELINE.zks",new string('a',64)),Working=Descriptor("KMC_AUTOMATION_WORKING","Manual_299_KMC_AUTOMATION_WORKING.zks",new string('b',64)),WriteAuthorization=new RuntimeSaveWriteAuthorization{Mode="working-only",AllowedInternalName="KMC_AUTOMATION_WORKING",AllowedFileName="Manual_299_KMC_AUTOMATION_WORKING.zks",BaselineImmutable=true}},
  QualificationSuite=new RuntimeQualificationSuiteIdentity{SuiteId="field-suite",SnapshotSha256=new string('4',64)}};}
 private static RuntimeSaveDescriptor Descriptor(string name,string leaf,string hash)=>new RuntimeSaveDescriptor{InternalName=name,FileName=leaf,Sha256=hash,Length=10,LastWriteTimeUtcTicks=638907120000000000L,GameId=Campaign,GameName="Fixture",Area=Area};
}}
