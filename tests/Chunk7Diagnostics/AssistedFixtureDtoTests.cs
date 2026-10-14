using System;using System.Linq;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using KingmakerMountedCombat.Diagnostics;
namespace KingmakerMountedCombat.Tests{
internal static class AssistedFixtureDtoTests{
 static int passed,failed;
 static void Check(string name,bool value){Console.WriteLine((value?"PASS ":"FAIL ")+name);if(value)passed++;else failed++;}
 static Chunk7AssistedFixtureSelection Selection()=>new Chunk7AssistedFixtureSelection{Case=Chunk7AssistedFixtureSelection.CaseName,OwnerAuthorizationSha256=Chunk7AssistedFixtureSelection.AuthorizationSha256,BaseGameOnly=true,AssistedSetupOnly=true,StoryPolicy=Chunk7AssistedFixtureSelection.StoryPolicyName,AssistancePolicy=Chunk7AssistedFixtureSelection.AssistancePolicyName,ExpectedNativeLoadCount=0,MaximumNativeWriteCount=3,TargetRangerLevel=4,MaximumTargetLevel=8,MaximumStoryChoices=16,MaximumStorySeconds=300,MaximumExperienceShortfall=200000,MaximumMoneyShortfall=100000,NativeInitialAutoLeaf=Chunk7AssistedFixtureSelection.InitialAutoLeaf,NativeManualName=Chunk7AssistedFixtureSelection.OutputName};
 static int Main(){
  foreach(var scenario in new[]{"fixture-intake","chunk7-native-main-bootstrap","chunk7-manual-derived-acquisition","chunk7-native-progression"}){var legacy=new RuntimeRequest{Scenario=scenario};Check("legacy default omits assisted "+scenario,JObject.FromObject(legacy).Property("AssistedFixture")==null);Check("legacy camel omits assisted "+scenario,Chunk7EvidenceDto.Request(legacy).Property("assistedFixture")==null);}
  var selection=Selection();var request=new RuntimeRequest{SchemaVersion=2,Scenario=Chunk7AssistedFixtureSelection.Scenario,AssistedFixture=selection};var plain=JObject.FromObject(request);var camel=Chunk7EvidenceDto.Request(request);
  Check("exact new request includes typed assisted property",plain["AssistedFixture"] is JObject&&camel["assistedFixture"] is JObject);
  var keys=new[]{"case","ownerAuthorizationSha256","baseGameOnly","assistedSetupOnly","storyPolicy","assistancePolicy","expectedNativeLoadCount","maximumNativeWriteCount","targetRangerLevel","maximumTargetLevel","maximumStoryChoices","maximumStorySeconds","maximumExperienceShortfall","maximumMoneyShortfall","nativeInitialAutoLeaf","nativeManualName"};var encoded=Chunk7EvidenceDto.Request(selection);
  Check("artifact selection has exact sixteen camel keys",encoded.Properties().Select(p=>p.Name).SequenceEqual(keys));
  Check("closed selection emits no computed key or constants",JObject.FromObject(selection).Properties().Count()==16&&encoded.Property("key")==null&&encoded.Property("scenario")==null);
  Check("actual booleans remain boolean",encoded["baseGameOnly"].Type==JTokenType.Boolean&&encoded["assistedSetupOnly"].Type==JTokenType.Boolean);
  Check("actual numeric budgets remain integers",keys.Skip(6).Take(8).All(k=>encoded[k].Type==JTokenType.Integer));
  Check("exact money ceiling and no implicit resource award",(long)encoded["maximumMoneyShortfall"]==100000&&encoded.Property("moneyGrant")==null);
  var roundtrip=camel.ToObject<RuntimeRequest>();Check("new request serializer roundtrip preserves exact selection",roundtrip.Scenario==request.Scenario&&roundtrip.AssistedFixture.Validate(roundtrip.Scenario).Count==0&&JToken.DeepEquals(Chunk7EvidenceDto.Request(roundtrip.AssistedFixture),encoded));
  Check("plain serializer selection roundtrip preserves exact values",JToken.DeepEquals(JObject.FromObject(plain.ToObject<RuntimeRequest>().AssistedFixture),JObject.FromObject(selection)));
  request.AssistedFixture=null;Check("clearing selection restores historical absent shape",Chunk7EvidenceDto.Request(request).Property("assistedFixture")==null);
  Check("canonical native Manual name is generated for positive slot",Chunk7AssistedFixtureSelection.ManualLeaf(12)=="Manual_12_KMC_C7_ASSISTED_FIXTURE.zks");
  foreach(var number in new[]{0,-1}){bool refused=false;try{Chunk7AssistedFixtureSelection.ManualLeaf(number);}catch(ArgumentOutOfRangeException){refused=true;}Check("nonpositive Manual slot refuses "+number,refused);}
  Console.WriteLine("TOTAL PASS="+passed+" FAIL="+failed);return failed==0?0:1;
 }
}}
