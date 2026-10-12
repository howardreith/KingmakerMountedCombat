using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KingmakerMountedCombat.Diagnostics;
namespace KingmakerMountedCombat.Tests {
internal static class ProgressionProtocolTests {
 private const string Campaign="11111111-1111-4111-8111-111111111111",Area="0123456789abcdef0123456789abcdef";
 private static string root;
 private static int Main(string[] args){root=args.Single();var tests=new TestRunner();
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
  return tests.Complete();
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
