using System;using System.Collections.Generic;using System.IO;using System.Linq;using KingmakerMountedCombat.Diagnostics;
namespace KingmakerMountedCombat.Tests{
internal static class AssistedColdFixture{
 internal const string Anchor="11111111-1111-4111-8111-111111111111", Minted="22222222-1111-4111-8111-111111111111", Area="0123456789abcdef0123456789abcdef";
 internal static RuntimeSaveDescriptor D(string name,string leaf,char hash,string gameId=Anchor)=>new RuntimeSaveDescriptor{InternalName=name,FileName=leaf,Sha256=new string(hash,64),Length=10,LastWriteTimeUtcTicks=638907120000000000L,GameId=gameId,GameName=gameId==Anchor?"Anchor":"Native",Area=Area};
 internal static RuntimeRequest Request()=>new RuntimeRequest{SchemaVersion=2,RunId="assisted-cold-test",Scenario=Chunk7AssistedColdInventorySelection.Scenario,
 AssistedColdInventory=new Chunk7AssistedColdInventorySelection{Case=Chunk7AssistedColdInventorySelection.CaseName,SourceRunId="assisted-source",SourceArtifactSha256=new string('d',64),SourceArchiveSha256=new string('c',64),BaseGameOnly=true,ExpectedNativeLoadCount=1,ExpectedNativeWriteCount=0},
 PersistenceLoad=D(Chunk7AssistedColdInventorySelection.ManualName,"Manual_7_KMC_C7_ASSISTED_FIXTURE.zks",'c',Minted),
 Branch="codex/mounted-combat-chunk7-foundation",Commit=new string('1',40),ProductVersion=BuildIdentity.ProductVersion,DllSha256=new string('2',64),DllMvid=Anchor,TransactionToken=new string('3',64),EvidenceRoot=@"C:\KmcBootstrapVirtual\evidence",
 Fixture=new RuntimeFixtureIdentity{Baseline=D("KMC_AUTOMATION_BASELINE","Manual_298_KMC_AUTOMATION_BASELINE.zks",'a'),Working=D("KMC_AUTOMATION_WORKING","Manual_299_KMC_AUTOMATION_WORKING.zks",'b'),WriteAuthorization=new RuntimeSaveWriteAuthorization{Mode="working-only",AllowedInternalName="KMC_AUTOMATION_WORKING",AllowedFileName="Manual_299_KMC_AUTOMATION_WORKING.zks",BaselineImmutable=true}},QualificationSuite=new RuntimeQualificationSuiteIdentity{SuiteId="assisted-suite",SnapshotSha256=new string('4',64)}};
 // Owner-seed kind: registry-supplied names, seed pins only, no producer pins.
 internal static RuntimeRequest SeedRequest(){var r=Request();r.AssistedColdInventory=new Chunk7AssistedColdInventorySelection{Case=Chunk7AssistedColdInventorySelection.OwnerSeedCaseName,SeedId="KMC_C7_OWNER_SEED",SeedSha256=new string('c',64),BaseGameOnly=true,ExpectedNativeLoadCount=1,ExpectedNativeWriteCount=0};r.PersistenceLoad=D("KMC_C7_OWNER_SEED","Manual_302_KMC_C7_OWNER_SEED.zks",'c',Minted);return r;}
 internal static RuntimeRequest SmallRequest(){var r=Request();r.AssistedColdInventory.Case=Chunk7AssistedColdInventorySelection.SmallFixtureCaseName;
  r.PersistenceLoad=D(Chunk7SmallFixtureSelection.OutputName,"Manual_304_KMC_C7_SMALL_FIXTURE.zks",'c',Chunk7SmallFixtureSelection.CampaignId);
  r.PersistenceLoad.GameName=Chunk7SmallFixtureSelection.CampaignName;r.PersistenceLoad.Area=Chunk7SmallFixtureSelection.AreaId;return r;}
 internal static RuntimeRequest MediumRequest(){var r=SmallRequest();r.AssistedColdInventory.Case=Chunk7AssistedColdInventorySelection.MediumFixtureCaseName;
  r.PersistenceLoad.InternalName=Chunk7SmallFixtureSelection.MediumOutputName;
  r.PersistenceLoad.FileName="Manual_304_KMC_C7_MEDIUM_FIXTURE.zks";return r;}
 internal static RuntimeRequest ThirdHorseRequest(){var r=SmallRequest();r.AssistedColdInventory.Case=Chunk7AssistedColdInventorySelection.ThirdHorseCaseName;
  r.PersistenceLoad.InternalName=Chunk7OwnedPairsSelection.SmallFixtureManualName;
  r.PersistenceLoad.FileName="Manual_305_KMC_C7_SMALL_WORLD.zks";return r;}
 internal static RuntimeRequest FourthHorseRequest(){var r=SmallRequest();r.AssistedColdInventory.Case=Chunk7AssistedColdInventorySelection.FourthHorseCaseName;
  r.PersistenceLoad.InternalName=Chunk7OwnedPairsSelection.FourthHorseManualName;
  r.PersistenceLoad.FileName="Manual_306_KMC_C7_FOUR_HORSE_WORLD.zks";return r;}
 internal static RuntimeRequest FourthHorseSelectorRequest(){var r=FourthHorseRequest();r.AssistedColdInventory.Case=Chunk7AssistedColdInventorySelection.FourthHorseSelectorCaseName;return r;}
 internal static RuntimeRequest FifthHorseRequest(){var r=SmallRequest();r.AssistedColdInventory.Case=Chunk7AssistedColdInventorySelection.FifthHorseCaseName;
  r.PersistenceLoad.InternalName=Chunk7ManualDerivedSelection.ManualOutputName;
  r.PersistenceLoad.FileName=Chunk7ManualDerivedSelection.F09FifthOutputLeaf;return r;}
 internal static RuntimeRequest FifthHorseDepartureRequest(){var r=FifthHorseRequest();var s=r.AssistedColdInventory;
  s.Case=Chunk7AssistedColdInventorySelection.FifthHorseDepartureCaseName;s.SourceRunId="20261022-c7b-world181-b181s1-fifthhorse-manual";
  s.SourceArtifactSha256="e05fdd3cfb16c344dcb821a57476cefdfb91978aa8a39aca9322e5b4fc24a47b";
  s.SourceArchiveSha256=r.PersistenceLoad.Sha256="afbeaa5bc749d91bd969581977f396ff4b0c2716691625a9648b2bb827c2318e";return r;}
 internal static RuntimeRequest SixthHorseRequest(){var r=SmallRequest();var s=r.AssistedColdInventory;
  s.Case=Chunk7AssistedColdInventorySelection.SixthHorseCaseName;s.SourceRunId="20261022-c7b-world184-b184s1-sixthhorse-manual";
  s.SourceArtifactSha256="0f6aabe42d0aeaf63f60c6912c1a029c0175ebe87806768f1191df23a913fa13";
  s.SourceArchiveSha256=r.PersistenceLoad.Sha256="b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291";
  r.PersistenceLoad.InternalName=Chunk7ManualDerivedSelection.F09SixthOutputName;
  r.PersistenceLoad.FileName=Chunk7ManualDerivedSelection.F09SixthOutputLeaf;return r;}
}
internal static class AssistedColdProtocolTests{
 static int Main(){var t=new TestRunner();
 t.Run("exact sixth-Horse Manual is one read-only native load",()=>{var r=AssistedColdFixture.SixthHorseRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));});
 foreach(var kind in new[]{"producer","artifact","archive","write","second load","fifth leaf","old name","foreign campaign","foreign area"})t.Run("sixth-Horse cold refuses "+kind,()=>{
  var r=AssistedColdFixture.SixthHorseRequest();switch(kind){
   case "producer":r.AssistedColdInventory.SourceRunId="foreign-producer";break;
   case "artifact":r.AssistedColdInventory.SourceArtifactSha256=new string('f',64);break;
   case "archive":r.AssistedColdInventory.SourceArchiveSha256=r.PersistenceLoad.Sha256=new string('f',64);break;
   case "write":r.AssistedColdInventory.ExpectedNativeWriteCount=1;break;
   case "second load":r.AssistedColdInventory.ExpectedNativeLoadCount=2;break;
   case "fifth leaf":r.PersistenceLoad.FileName=Chunk7ManualDerivedSelection.F09FifthOutputLeaf;break;
   case "old name":r.PersistenceLoad.InternalName=Chunk7ManualDerivedSelection.ManualOutputName;break;
   case "foreign campaign":r.PersistenceLoad.GameId=AssistedColdFixture.Minted;break;
   case "foreign area":r.PersistenceLoad.Area=AssistedColdFixture.Area;break;
  }TestRunner.True(r.Validate().Count>0,"Unqualified sixth-Horse cold input admitted.");});
 t.Run("exact fifth-Horse native departure remains one load and zero saves",()=>{var r=AssistedColdFixture.FifthHorseDepartureRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));});
 foreach(var kind in new[]{"producer","artifact","archive","write","second load","other Manual"})t.Run("native departure refuses "+kind,()=>{
  var r=AssistedColdFixture.FifthHorseDepartureRequest();switch(kind){
   case "producer":r.AssistedColdInventory.SourceRunId="foreign-producer";break;
   case "artifact":r.AssistedColdInventory.SourceArtifactSha256=new string('f',64);break;
   case "archive":r.AssistedColdInventory.SourceArchiveSha256=r.PersistenceLoad.Sha256=new string('f',64);break;
   case "write":r.AssistedColdInventory.ExpectedNativeWriteCount=1;break;
   case "second load":r.AssistedColdInventory.ExpectedNativeLoadCount=2;break;
   case "other Manual":r.PersistenceLoad.FileName="Manual_308_KMC_C7_MANUAL_DERIVED_FIELD.zks";break;
  }TestRunner.True(r.Validate().Count>0,"Unqualified native departure input admitted.");});
 t.Run("exact qualified Manual cold request validates",()=>{var r=AssistedColdFixture.Request();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));});
 t.Run("owner seed request validates without producer pins",()=>{var r=AssistedColdFixture.SeedRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));});
 t.Run("F09 native Manual has a distinct read-only cold case",()=>{var r=AssistedColdFixture.SmallRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));});
 t.Run("paid Medium Manual has its own read-only cold case",()=>{var r=AssistedColdFixture.MediumRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));});
 t.Run("third-Horse Manual has its own zero-write cold case",()=>{var r=AssistedColdFixture.ThirdHorseRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));});
 t.Run("fourth-Horse Manual has its own zero-write cold case",()=>{var r=AssistedColdFixture.FourthHorseRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));});
 t.Run("paid fifth-Horse Manual has its own zero-write cold case",()=>{var r=AssistedColdFixture.FifthHorseRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));});
 foreach(var kind in new[]{"fourth leaf","wrong number","wrong name","foreign campaign","foreign area","wrong hash","write","second load"})
  t.Run("fifth-Horse cold refuses "+kind,()=>{var r=AssistedColdFixture.FifthHorseRequest();switch(kind){
   case "fourth leaf":r.PersistenceLoad.FileName="Manual_306_KMC_C7_FOUR_HORSE_WORLD.zks";break;
   case "wrong number":r.PersistenceLoad.FileName="Manual_308_KMC_C7_MANUAL_DERIVED_FIELD.zks";break;
   case "wrong name":r.PersistenceLoad.InternalName=Chunk7OwnedPairsSelection.FourthHorseManualName;break;
   case "foreign campaign":r.PersistenceLoad.GameId=AssistedColdFixture.Minted;break;
   case "foreign area":r.PersistenceLoad.Area=AssistedColdFixture.Area;break;
   case "wrong hash":r.AssistedColdInventory.SourceArchiveSha256=new string('e',64);break;
   case "write":r.AssistedColdInventory.ExpectedNativeWriteCount=1;break;
   case "second load":r.AssistedColdInventory.ExpectedNativeLoadCount=2;break;}
   TestRunner.True(r.Validate().Count>0,"Foreign fifth-Horse cold shape admitted.");});
 t.Run("opened selector observation accepts only the exact fourth-Horse Manual under zero-write authority",()=>{
  var r=AssistedColdFixture.FourthHorseSelectorRequest();TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));
  r.PersistenceLoad.FileName="Manual_305_KMC_C7_SMALL_WORLD.zks";TestRunner.True(r.Validate().Count>0,"Selector admitted the third-Horse leaf.");
  r=AssistedColdFixture.FourthHorseSelectorRequest();r.AssistedColdInventory.ExpectedNativeWriteCount=1;
  TestRunner.True(r.Validate().Count>0,"Selector admitted a native write.");});
 foreach(var kind in new[]{"third leaf","wrong number","wrong name","foreign campaign","foreign area","wrong hash","write","second load"})
  t.Run("fourth-Horse cold refuses "+kind,()=>{var r=AssistedColdFixture.FourthHorseRequest();switch(kind){
   case "third leaf":r.PersistenceLoad.FileName="Manual_305_KMC_C7_SMALL_WORLD.zks";break;
   case "wrong number":r.PersistenceLoad.FileName="Manual_307_KMC_C7_FOUR_HORSE_WORLD.zks";break;
   case "wrong name":r.PersistenceLoad.InternalName=Chunk7OwnedPairsSelection.SmallFixtureManualName;break;
   case "foreign campaign":r.PersistenceLoad.GameId=AssistedColdFixture.Minted;break;
   case "foreign area":r.PersistenceLoad.Area=AssistedColdFixture.Area;break;
   case "wrong hash":r.AssistedColdInventory.SourceArchiveSha256=new string('e',64);break;
   case "write":r.AssistedColdInventory.ExpectedNativeWriteCount=1;break;
   case "second load":r.AssistedColdInventory.ExpectedNativeLoadCount=2;break;}
   TestRunner.True(r.Validate().Count>0,"Foreign fourth-Horse cold shape admitted.");});
 foreach(var kind in new[]{"old leaf","wrong number","wrong name","foreign campaign","foreign area","wrong hash","write","second load"})
  t.Run("third-Horse cold refuses "+kind,()=>{var r=AssistedColdFixture.ThirdHorseRequest();switch(kind){
   case "old leaf":r.PersistenceLoad.FileName="Manual_304_KMC_C7_SMALL_FIXTURE.zks";break;
   case "wrong number":r.PersistenceLoad.FileName="Manual_306_KMC_C7_SMALL_WORLD.zks";break;
   case "wrong name":r.PersistenceLoad.InternalName=Chunk7SmallFixtureSelection.OutputName;break;
   case "foreign campaign":r.PersistenceLoad.GameId=AssistedColdFixture.Minted;break;
   case "foreign area":r.PersistenceLoad.Area=AssistedColdFixture.Area;break;
   case "wrong hash":r.AssistedColdInventory.SourceArchiveSha256=new string('e',64);break;
   case "write":r.AssistedColdInventory.ExpectedNativeWriteCount=1;break;
   case "second load":r.AssistedColdInventory.ExpectedNativeLoadCount=2;break;}
   TestRunner.True(r.Validate().Count>0,"Foreign third-Horse cold shape admitted.");});
 foreach(var kind in new[]{"small-leaf","small-name","wrong-campaign","wrong-hash","second-load","write"})t.Run("Medium cold refuses "+kind,()=>{var r=AssistedColdFixture.MediumRequest();switch(kind){
  case "small-leaf":r.PersistenceLoad.FileName="Manual_304_KMC_C7_SMALL_FIXTURE.zks";break;
  case "small-name":r.PersistenceLoad.InternalName=Chunk7SmallFixtureSelection.OutputName;break;
  case "wrong-campaign":r.PersistenceLoad.GameId=AssistedColdFixture.Minted;break;
  case "wrong-hash":r.AssistedColdInventory.SourceArchiveSha256=new string('e',64);break;
  case "second-load":r.AssistedColdInventory.ExpectedNativeLoadCount=2;break;
  case "write":r.AssistedColdInventory.ExpectedNativeWriteCount=1;break;}
  TestRunner.True(r.Validate().Count>0,"Invalid Medium cold request admitted.");});
 foreach(var kind in new[]{"assisted-leaf","assisted-name","seed","wrong-campaign","wrong-area","wrong-hash","write","second-load","self-source"})t.Run("F09 cold refuses "+kind,()=>{var r=AssistedColdFixture.SmallRequest();switch(kind){
  case"assisted-leaf":r.PersistenceLoad.FileName="Manual_304_KMC_C7_ASSISTED_FIXTURE.zks";break;
  case"assisted-name":r.PersistenceLoad.InternalName=Chunk7AssistedColdInventorySelection.ManualName;break;
  case"seed":r.AssistedColdInventory.SeedId="KMC_C7_NO_MODS_OWNER_SEED";break;
  case"wrong-campaign":r.PersistenceLoad.GameId=AssistedColdFixture.Minted;break;
  case"wrong-area":r.PersistenceLoad.Area=AssistedColdFixture.Area;break;
  case"wrong-hash":r.AssistedColdInventory.SourceArchiveSha256=new string('e',64);break;
  case"write":r.AssistedColdInventory.ExpectedNativeWriteCount=1;break;
  case"second-load":r.AssistedColdInventory.ExpectedNativeLoadCount=2;break;
  case"self-source":r.AssistedColdInventory.SourceRunId=r.RunId;break;}
  TestRunner.True(r.Validate().Count>0,"Invalid F09 cold request admitted.");});
 // Owner-seed internal names: single interior spaces admitted (Kingmaker save names); the file leaf is unchanged.
 foreach(var name in new[]{"KMC C7 NO MODS OWNER SEED","KMC_C7_OWNER_SEED",new string('A',64),"A","KMC C7.v2-b_x"})t.Run("owner seed name admitted '"+name+"'",()=>{var r=AssistedColdFixture.SeedRequest();r.PersistenceLoad.InternalName=name;TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));TestRunner.True(Chunk7AssistedColdInventorySelection.IsOwnerSeedName(name),"name refused");});
 foreach(var name in new[]{" KMC C7 NO MODS OWNER SEED","KMC C7 NO MODS OWNER SEED ","KMC C7  NO MODS OWNER SEED","KMC\tC7 NO MODS OWNER SEED","KMC C7 NO MODS OWNER SEED\n","KMC\u00a0C7",new string('A',65),"",Chunk7AssistedColdInventorySelection.ManualName,"_KMC C7"})t.Run("owner seed name refused '"+name.Replace("\t","\\t").Replace("\n","\\n")+"'",()=>{var r=AssistedColdFixture.SeedRequest();r.PersistenceLoad.InternalName=name;TestRunner.True(r.Validate().Count>0,"Owner seed name admitted.");TestRunner.True(!Chunk7AssistedColdInventorySelection.IsOwnerSeedName(name),"name admitted");});
 t.Run("owner seed leaf still refuses spaces",()=>{TestRunner.True(!Chunk7AssistedColdInventorySelection.IsOwnerSeedLeaf("Manual_303_KMC C7.zks")&&Chunk7AssistedColdInventorySelection.IsOwnerSeedLeaf("Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks"),"leaf rule changed");});
 foreach(var kind in new[]{"producer-leaf","producer-name","sha","working-alias","baseline-alias","producer-pins","traversal","write","second-load","base"})t.Run("owner seed refuses "+kind,()=>{var r=AssistedColdFixture.SeedRequest();var s=r.AssistedColdInventory;switch(kind){case"producer-leaf":r.PersistenceLoad.FileName="Manual_7_KMC_C7_ASSISTED_FIXTURE.zks";break;case"producer-name":r.PersistenceLoad.InternalName=Chunk7AssistedColdInventorySelection.ManualName;break;case"sha":s.SeedSha256=new string('e',64);break;case"working-alias":r.PersistenceLoad.GameId=r.Fixture.Working.GameId;break;case"baseline-alias":r.PersistenceLoad.Sha256=s.SeedSha256=r.Fixture.Baseline.Sha256;break;case"producer-pins":s.SourceRunId="assisted-source";break;case"traversal":r.PersistenceLoad.FileName="../Manual_302_KMC_C7_OWNER_SEED.zks";break;case"write":s.ExpectedNativeWriteCount=1;break;case"second-load":s.ExpectedNativeLoadCount=2;break;case"base":s.BaseGameOnly=false;break;}TestRunner.True(r.Validate().Count>0,"Owner seed variant admitted.");});
 t.Run("producer selection refuses owner-seed pins",()=>{var r=AssistedColdFixture.Request();r.AssistedColdInventory.SeedId="KMC_C7_OWNER_SEED";TestRunner.True(r.Validate().Count>0,"Producer carried seed pins.");});
 t.Run("selected save-backed family is explicitly registered",()=>TestRunner.True(RuntimeRequest.IsSaveBackedScenario(Chunk7AssistedColdInventorySelection.Scenario),"Missing exact family allowlist."));
 t.Run("read-only cold defaults to native Manual category",()=>TestRunner.Equal("Manual",AssistedColdFixture.Request().ExpectedNativeLoadType,"Wrong native type."));
 t.Run("selected fixture is exact qualified Manual",()=>{var r=AssistedColdFixture.Request();TestRunner.True(ReferenceEquals(r.PersistenceLoad,r.SelectedChunk7Fixture),"Working alias selected.");});
 foreach(var kind in new[]{"missing","case","source-self","hash","base","loads","writes","Working","Auto","leading-zero","over-int","extra-area","extra-family","legacy","schema"})t.Run("cold protocol refuses "+kind,()=>{var r=AssistedColdFixture.Request();switch(kind){case"missing":r.AssistedColdInventory=null;break;case"case":r.AssistedColdInventory.Case="other";break;case"source-self":r.AssistedColdInventory.SourceRunId=r.RunId;break;case"hash":r.AssistedColdInventory.SourceArchiveSha256=new string('f',64);break;case"base":r.AssistedColdInventory.BaseGameOnly=false;break;case"loads":r.AssistedColdInventory.ExpectedNativeLoadCount=2;break;case"writes":r.AssistedColdInventory.ExpectedNativeWriteCount=1;break;case"Working":r.PersistenceLoad=r.Fixture.Working;break;case"Auto":r.PersistenceLoad.FileName="Auto_1.zks";break;case"leading-zero":r.PersistenceLoad.FileName="Manual_07_KMC_C7_ASSISTED_FIXTURE.zks";break;case"over-int":r.PersistenceLoad.FileName="Manual_2147483648_KMC_C7_ASSISTED_FIXTURE.zks";break;case"extra-area":r.PersistenceAreaTarget=new RuntimeAreaTransitionTarget();break;case"extra-family":r.BootstrapInventory=new Chunk7BootstrapInventorySelection();break;case"legacy":r.Scenario="fixture-intake";break;case"schema":r.SchemaVersion=1;break;}TestRunner.True(r.Validate().Count>0,"Invalid cold request admitted.");});
 foreach(var leaf in new[]{(string)null,"","Manual_KMC_C7_ASSISTED_FIXTURE.zks","Manual_0_KMC_C7_ASSISTED_FIXTURE.zks","Manual_-1_KMC_C7_ASSISTED_FIXTURE.zks","Manual_01_KMC_C7_ASSISTED_FIXTURE.zks","Manual_2147483648_KMC_C7_ASSISTED_FIXTURE.zks","Manual_1_KMC_C7_ASSISTED_FIXTURE.zks.other","../Manual_1_KMC_C7_ASSISTED_FIXTURE.zks"})t.Run("native Manual leaf refuses malformed "+leaf,()=>TestRunner.True(!Chunk7AssistedColdInventorySelection.IsManualLeaf(leaf),"Malformed leaf admitted."));
 foreach(var leaf in new[]{"Manual_1_KMC_C7_ASSISTED_FIXTURE.zks","Manual_2147483647_KMC_C7_ASSISTED_FIXTURE.zks"})t.Run("native Manual positive range "+leaf,()=>TestRunner.True(Chunk7AssistedColdInventorySelection.IsManualLeaf(leaf),"Canonical leaf refused."));
 t.Run("historical fixture remains exact without new selection",()=>{var r=AssistedColdFixture.Request();r.Scenario="fixture-intake";r.AssistedColdInventory=null;r.PersistenceLoad=null;TestRunner.Equal(0,r.Validate().Count,string.Join(" | ",r.Validate()));});
 return t.Complete();}
}
internal static class V{internal static readonly Dictionary<string,string> Files=new Dictionary<string,string>(StringComparer.Ordinal);internal static bool Throw;internal static void RequireDirectory(string p){if(!p.StartsWith(@"C:\KmcBootstrapVirtual\run",StringComparison.Ordinal))throw new IOException("foreign root");}internal static string Hash(string p){string h;if(!Files.TryGetValue(Path.GetFileName(p),out h))throw new IOException("missing virtual file");return h;}}
internal static class AssistedDirectory{internal static bool Exists(string p)=>!p.EndsWith(".zks",StringComparison.Ordinal);internal static IEnumerable<string> EnumerateFileSystemEntries(string p){if(V.Throw)throw new IOException("enumeration");return V.Files.Keys.Select(x=>Path.Combine(p,x)).ToArray();}}
internal static class AssistedFile{internal static bool Exists(string p)=>V.Files.ContainsKey(Path.GetFileName(p));internal static void Delete(string p){V.Files.Remove(Path.GetFileName(p));}}
}
