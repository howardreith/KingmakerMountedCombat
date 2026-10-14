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
 Console.WriteLine("TOTAL PASS="+passed+" FAIL="+failed);return failed==0?0:1;
 }
}}
