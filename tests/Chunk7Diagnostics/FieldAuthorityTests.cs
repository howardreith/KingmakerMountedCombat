using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KingmakerMountedCombat.Diagnostics;
namespace KingmakerMountedCombat.Tests {
internal static class FieldAuthorityTests {
 private const string Campaign="11111111-1111-4111-8111-111111111111",Area="0123456789abcdef0123456789abcdef";
 private static string root;
 private static int Main(string[] args){root=args.Single();var tests=new TestRunner();
  tests.Run("field complete protocol accepts one remote source",()=>TestRunner.Equal(0,Request().Validate().Count,"Valid field request refused."));
  foreach(var name in new[]{"missing","foreign scenario","write case","foreign source","working source","wrong leaf","wrong campaign","self source","same actor","unknown case"})
   tests.Run("field protocol rejects "+name,()=>{var r=Request();switch(name){
    case "missing":r.OrdinaryField=null;break;case "foreign scenario":r.Scenario="chunk7-ordinary-acquisition";break;
    case "write case":r.PersistenceCase="multi-exploration-auto";break;case "foreign source":r.PersistenceLoad.Sha256=new string('f',64);break;
    case "working source":r.PersistenceLoad=r.Fixture.Working;break;case "wrong leaf":r.PersistenceLoad.FileName="Auto_2.zks";break;
    case "wrong campaign":r.PersistenceLoad.GameId="22222222-1111-4111-8111-111111111111";break;
    case "self source":r.OrdinaryField.SourceRunId=r.RunId;break;case "same actor":r.OrdinaryField.ActorId=r.OrdinaryField.PetId;break;
    case "unknown case":r.OrdinaryField.Case="field-any";break;}
    TestRunner.True(r.Validate().Count>0,"Invalid field request accepted.");});
  tests.Run("field exact result rows registered",()=>{foreach(var name in new[]{"C7-field-readonly-source","C7-field-native-party-placement","C7-field-cleanup","C7-field-runtime","C7-field-interrupted"})TestRunner.True(RuntimeSubscenarioResult.IsMissionScenario(name),"Unknown row.");TestRunner.True(!RuntimeSubscenarioResult.IsMissionScenario("C7-field-any"),"Wildcard row admitted.");});
  tests.Run("read-only plan repeated validation never spends native load",()=>{var f=new Fixture();var a=f.Scope();for(int i=0;i<5;i++)TestRunner.True(a.MatchesReadOnlyFieldPlan(f.Source)&&a.Validate(RuntimeSaveOperation.Load,f.Target,f.Saves)==null,"Readonly validation refused.");TestRunner.Equal(0,a.AdmittedNativeLoadRequests,"Probe spent native load.");});
  tests.Run("exact native load admitted once and second refused before disposal",()=>{var f=new Fixture();var a=f.Scope();TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Load,f.Target,f.Saves)==null,"First load refused.");TestRunner.True(a.Validate(RuntimeSaveOperation.Load,f.Target,f.Saves)!=null&&a.AuthorizeNativeRequest(RuntimeSaveOperation.Load,f.Target,f.Saves)!=null,"Repeat load admitted.");TestRunner.Equal(1,a.AdmittedNativeLoadRequests,"Repeat changed admission count.");TestRunner.True(a.MatchesReadOnlyFieldPlan(f.Source),"Read-only plan lost after admission.");});
  tests.Run("exact readonly archive cannot write",()=>{var f=new Fixture();var a=f.Scope();TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Write,f.Target,f.Saves)!=null,"Write admitted.");TestRunner.Equal(0,a.AdmittedNativeWriteRequests,"Rejected write counted.");});
  tests.Run("source hash mismatch cannot spend native load",()=>{var f=new Fixture();var a=f.Scope();File.AppendAllText(f.Target.FullPath,"changed");TestRunner.True(a.AuthorizeNativeRequest(RuntimeSaveOperation.Load,f.Target,f.Saves)!=null,"Changed archive loaded.");TestRunner.Equal(0,a.AdmittedNativeLoadRequests,"Rejected hash spent load.");});
  foreach(var name in new[]{"uncapped load","extra leaf","writable","write cap","foreign type","foreign hash","foreign name","foreign area","foreign campaign"})
   tests.Run("read-only field plan rejects "+name,()=>{var f=new Fixture();var entries=new[]{f.Entry()};int? load=1,write=null;switch(name){case "uncapped load":load=null;break;case "extra leaf":entries=new[]{f.Entry(),new PersistenceSaveEntry{FileName="Auto_2.zks",InternalName="Other",SaveType="Auto",Area=Area,Writable=true}};break;case "writable":entries[0].Writable=true;break;case "write cap":write=1;break;case "foreign type":entries[0].SaveType="Manual";break;case "foreign hash":f.Source.Sha256=new string('e',64);break;case "foreign name":f.Source.InternalName="Other";break;case "foreign area":f.Source.Area=new string('b',32);break;case "foreign campaign":f.Source.GameId="22222222-1111-4111-8111-111111111111";break;}
    var a=new PersistenceSaveAuthorization(f.Run,Campaign,"Fixture",new string('a',64),entries,null,null,write,load);TestRunner.True(!a.MatchesReadOnlyFieldPlan(f.Source),"Foreign plan admitted.");});
  return tests.Complete();
 }
 private static RuntimeRequest Request(){var f=new Fixture();return new RuntimeRequest{SchemaVersion=2,RunId="field-test",Scenario=Chunk7OrdinaryFieldSelection.Scenario,
  OrdinaryField=new Chunk7OrdinaryFieldSelection{Case=Chunk7OrdinaryFieldSelection.CaseName,SourceRunId="paid-source",SourceArchiveSha256=f.Source.Sha256,SourceArtifactSha256=new string('d',64),ActorId="33333333-1111-4111-8111-111111111111",PetId="44444444-1111-4111-8111-111111111111"},
  PersistenceLoad=f.Source,Branch="codex/mounted-combat-chunk7-foundation",Commit=new string('1',40),ProductVersion=BuildIdentity.ProductVersion,DllSha256=new string('2',64),DllMvid=Campaign,TransactionToken=new string('3',64),EvidenceRoot=root,
  Fixture=new RuntimeFixtureIdentity{Baseline=Descriptor("KMC_AUTOMATION_BASELINE","Manual_298_KMC_AUTOMATION_BASELINE.zks",new string('a',64)),Working=Descriptor("KMC_AUTOMATION_WORKING","Manual_299_KMC_AUTOMATION_WORKING.zks",new string('b',64)),WriteAuthorization=new RuntimeSaveWriteAuthorization{Mode="working-only",AllowedInternalName="KMC_AUTOMATION_WORKING",AllowedFileName="Manual_299_KMC_AUTOMATION_WORKING.zks",BaselineImmutable=true}},
  QualificationSuite=new RuntimeQualificationSuiteIdentity{SuiteId="field-suite",SnapshotSha256=new string('4',64)}};}
 private static RuntimeSaveDescriptor Descriptor(string name,string leaf,string hash)=>new RuntimeSaveDescriptor{InternalName=name,FileName=leaf,Sha256=hash,Length=10,LastWriteTimeUtcTicks=638907120000000000L,GameId=Campaign,GameName="Fixture",Area=Area};
 private sealed class Fixture {
  internal string Run,Saves;internal RuntimeSaveDescriptor Source;private string hash;
  internal Fixture(){Run=Path.Combine(root,Guid.NewGuid().ToString("N"));Saves=Path.Combine(Run,"Saved Games");Directory.CreateDirectory(Saves);var path=Path.Combine(Saves,"Auto_1.zks");File.WriteAllText(path,"synthetic remote source bytes");using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();Source=Descriptor("Autosave","Auto_1.zks",hash);Source.Length=new FileInfo(path).Length;Source.LastWriteTimeUtcTicks=new FileInfo(path).LastWriteTimeUtc.Ticks;}
  internal PersistenceSaveEntry Entry()=>new PersistenceSaveEntry{FileName="Auto_1.zks",InternalName="Autosave",SaveType="Auto",Area=Area,InitialSha256=hash,Writable=false};
  internal PersistenceSaveAuthorization Scope()=>new PersistenceSaveAuthorization(Run,Campaign,"Fixture",new string('a',64),new[]{Entry()},null,null,null,1);
  internal RuntimeSaveTarget Target=>new RuntimeSaveTarget{FileName="Auto_1.zks",FullPath=Path.Combine(Saves,"Auto_1.zks"),InternalName="Autosave",SaveType="Auto",GameId=Campaign,GameName="Fixture",Area=Area};
 }
}}
