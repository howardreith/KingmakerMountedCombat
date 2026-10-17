using System;using System.Linq;using Newtonsoft.Json.Linq;using KingmakerMountedCombat.Diagnostics;
namespace KingmakerMountedCombat.Tests{
internal static class AssistedColdDtoTests{
 static int passed,failed;static void Check(string name,bool value){Console.WriteLine((value?"PASS ":"FAIL ")+name);if(value)passed++;else failed++;}
 static int Main(){
 foreach(var scenario in new[]{"fixture-intake","chunk7-native-main-bootstrap","chunk7-assisted-fixture-preparation","chunk7-bootstrap-cold-inventory"}){var r=new RuntimeRequest{Scenario=scenario};Check("legacy plain omits cold "+scenario,JObject.FromObject(r).Property("AssistedColdInventory")==null);Check("legacy artifact omits cold "+scenario,Chunk7EvidenceDto.Request(r).Property("assistedColdInventory")==null);}
 var s=new Chunk7AssistedColdInventorySelection{Case=Chunk7AssistedColdInventorySelection.CaseName,SourceRunId="source-run",SourceArtifactSha256=new string('a',64),SourceArchiveSha256=new string('b',64),BaseGameOnly=true,ExpectedNativeLoadCount=1,ExpectedNativeWriteCount=0};
 var request=new RuntimeRequest{RunId="cold-run",Scenario=Chunk7AssistedColdInventorySelection.Scenario,AssistedColdInventory=s};var plain=JObject.FromObject(request);var encoded=Chunk7EvidenceDto.Request(request);var selection=(JObject)encoded["assistedColdInventory"];
 Check("new plain and artifact retain typed cold object",plain["AssistedColdInventory"] is JObject&&selection!=null);
 Check("closed seven scalar keys only",selection.Properties().Select(p=>p.Name).SequenceEqual(new[]{"case","sourceRunId","sourceArtifactSha256","sourceArchiveSha256","baseGameOnly","expectedNativeLoadCount","expectedNativeWriteCount"}));
 Check("native scalar JSON types preserved",selection["baseGameOnly"].Type==JTokenType.Boolean&&selection["expectedNativeLoadCount"].Type==JTokenType.Integer&&selection["expectedNativeWriteCount"].Type==JTokenType.Integer&&selection.Properties().Take(4).All(p=>p.Value.Type==JTokenType.String));
 Check("actual camel roundtrip retains exact cold selection",JToken.DeepEquals(Chunk7EvidenceDto.Request(encoded.ToObject<RuntimeRequest>().AssistedColdInventory),selection));
 Check("actual plain roundtrip retains exact cold selection",JToken.DeepEquals(JObject.FromObject(plain.ToObject<RuntimeRequest>().AssistedColdInventory),JObject.FromObject(s)));
 request.AssistedColdInventory=null;Check("clearing selection restores absent legacy shape",JObject.FromObject(request).Property("AssistedColdInventory")==null&&Chunk7EvidenceDto.Request(request).Property("assistedColdInventory")==null);
 var seed=new Chunk7AssistedColdInventorySelection{Case=Chunk7AssistedColdInventorySelection.OwnerSeedCaseName,SeedId="KMC_C7_OWNER_SEED",SeedSha256=new string('c',64),BaseGameOnly=true,ExpectedNativeLoadCount=1,ExpectedNativeWriteCount=0};
 var seedEncoded=Chunk7EvidenceDto.Request(new RuntimeRequest{RunId="cold-run",Scenario=Chunk7AssistedColdInventorySelection.Scenario,AssistedColdInventory=seed})["assistedColdInventory"] as JObject;
 Check("owner seed closed six keys, no producer pins",seedEncoded!=null&&seedEncoded.Properties().Select(p=>p.Name).SequenceEqual(new[]{"case","baseGameOnly","expectedNativeLoadCount","expectedNativeWriteCount","seedId","seedSha256"}));
 Check("owner seed camel roundtrip retains exact selection",JToken.DeepEquals(Chunk7EvidenceDto.Request(seedEncoded.ToObject<Chunk7AssistedColdInventorySelection>()),seedEncoded));
 // Classification from synthetic exact error inputs; never inferred beyond the names given.
 Func<string[],string[],string[],bool,string> c=(d,b,m,h)=>Chunk7OwnerSeedFailureClassifier.Classify(d,b,m,h);
 Check("classify foreign $type serializer error as seed-dependency",c(null,null,new[]{"Newtonsoft.Json.JsonSerializationException: Error resolving type specified in JSON 'CallOfTheWild.UnitPartFlying, CallOfTheWild'. Path 'm_Parts'."},false)=="seed-dependency");
 Check("classify foreign assembly load error as seed-dependency",c(null,null,new[]{"Newtonsoft.Json.JsonSerializationException: Could not load assembly 'KingmakerGunslinger'."},false)=="seed-dependency");
 Check("classify foreign Type dictionary key as seed-dependency",c(null,null,new[]{"Could not convert string 'CallOfTheWild.SanctuaryMechanics.UnitPartSanctuary, CallOfTheWild' to dictionary key type 'System.Type'."},false)=="seed-dependency");
 Check("classify archive-declared foreign type with loader failure as seed-dependency",c(new[]{"CallOfTheWild.UnitPartFlying, CallOfTheWild"},null,new[]{"WorkingFixtureLoader state Failed."},false)=="seed-dependency");
 Check("classify unresolved blueprint as seed-dependency",c(null,new[]{new string('9',32)},new string[0],false)=="seed-dependency");
 Check("classify KMC type error as kmc-or-loader",c(null,null,new[]{"Error resolving type specified in JSON 'KingmakerMountedCombat.Integration.Foo, KingmakerMountedCombat'."},false)=="kmc-or-loader");
 Check("classify bare loader failure as kmc-or-loader",c(null,null,new[]{"WorkingFixtureLoader state Failed."},false)=="kmc-or-loader");
 Check("classify base-game type miss as unclassified",c(null,null,new[]{"Could not find type 'Kingmaker.Removed' in assembly 'Assembly-CSharp'."},false)=="unclassified");
 Check("classify harness refusal without native error as runner-or-authority",c(null,null,new string[0],true)=="runner-or-authority");
 Check("classify foreign plus KMC names as unclassified",c(new[]{"CallOfTheWild.UnitPartFlying, CallOfTheWild"},null,new[]{"Could not load assembly 'KingmakerMountedCombat'."},false)=="unclassified");
 Check("classify no evidence as none",c(null,null,new string[0],false)=="none");
 Console.WriteLine("TOTAL PASS="+passed+" FAIL="+failed);return failed==0?0:1;
 }
}}
