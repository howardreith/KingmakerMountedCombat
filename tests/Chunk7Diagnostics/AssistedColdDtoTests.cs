using System;using System.Linq;using Newtonsoft.Json.Linq;using KingmakerMountedCombat.Diagnostics;
namespace KingmakerMountedCombat.Tests{
internal static class AssistedColdDtoTests{
 static int passed,failed;static void Recurse(int n){if(n==0)throw new InvalidOperationException("deep observer fault");Recurse(n-1);}static void Check(string name,bool value){Console.WriteLine((value?"PASS ":"FAIL ")+name);if(value)passed++;else failed++;}
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
 // Precedence: a runner/authority refusal decides first; then a KMC stack frame; then names.
 const string kmcStack="  at KingmakerMountedCombat.Integration.MountedPersistenceService.OnLoad () [0x00012] in <0>:0\n  at Kingmaker.EntitySystem.Persistence.ThreadedGameLoader.Load () [0x00000] in <0>:0";
 const string gameStack="  at Newtonsoft.Json.Serialization.DefaultSerializationBinder.BindToType (System.String assemblyName, System.String typeName) [0x00000] in <0>:0\n  at Kingmaker.EntitySystem.Persistence.ThreadedGameLoader.Load () [0x00000] in <0>:0";
 Check("refusal plus unrelated blueprint miss is runner-or-authority",Chunk7OwnerSeedFailureClassifier.Classify(null,new[]{new string('9',32)},new string[0],true)=="runner-or-authority");
 Check("refusal plus foreign type and KMC frame is runner-or-authority",Chunk7OwnerSeedFailureClassifier.Classify(new[]{"CallOfTheWild.UnitPartFlying, CallOfTheWild"},null,new[]{"WorkingFixtureLoader state Failed."},true,new[]{kmcStack})=="runner-or-authority");
 Check("KMC stack frame plus foreign type text is kmc-or-loader",Chunk7OwnerSeedFailureClassifier.Classify(null,null,new[]{"Newtonsoft.Json.JsonSerializationException: Error resolving type specified in JSON 'CallOfTheWild.UnitPartFlying, CallOfTheWild'."},false,new[]{kmcStack})=="kmc-or-loader");
 Check("KMC stack frame plus unresolved blueprint is kmc-or-loader",Chunk7OwnerSeedFailureClassifier.Classify(null,new[]{new string('9',32)},new string[0],false,new[]{gameStack,kmcStack})=="kmc-or-loader");
 Check("foreign type with only game frames stays seed-dependency",Chunk7OwnerSeedFailureClassifier.Classify(null,null,new[]{"Newtonsoft.Json.JsonSerializationException: Error resolving type specified in JSON 'CallOfTheWild.UnitPartFlying, CallOfTheWild'."},false,new[]{gameStack})=="seed-dependency");
 Check("KMC named outside a stack frame is not a frame",!Chunk7OwnerSeedFailureClassifier.HasKmcFrame("Could not load file or assembly 'KingmakerMountedCombat'")&&Chunk7OwnerSeedFailureClassifier.HasKmcFrame("  at (wrapper dynamic-method) KingmakerMountedCombat.Diagnostics.X.Y (object)"));
 Check("stack bound is 4000 characters",Chunk7OwnerSeedFailureClassifier.BoundStack(new string('s',5000)).Length==4000&&Chunk7OwnerSeedFailureClassifier.BoundStack(null)==null);
 // Exception-safe observation hooks: a throwing body neither escapes nor changes the observed call.
 var faults=new Chunk7OwnerSeedObserverFaults();
 Func<int,int> observed=x=>{faults.Run("UberDebug.LogException",()=>{throw new InvalidOperationException("observer body failed");});return x*2;};
 var propagated=false;var result=0;try{result=observed(21);}catch{propagated=true;}
 var recorded=faults.Snapshot();
 Check("throwing observer body does not propagate and leaves the result unchanged",!propagated&&result==42);
 Check("throwing observer body is recorded with type, message and bounded stack",faults.Count==1&&recorded.Length==1&&recorded[0].Hook=="UberDebug.LogException"&&recorded[0].ExceptionType=="System.InvalidOperationException"&&recorded[0].Message=="observer body failed"&&recorded[0].Stack!=null&&recorded[0].Stack.Length<=4000);
 Check("throwing observer body fails qualification by name",faults.QualificationFailure()!=null&&faults.QualificationFailure().StartsWith(Chunk7OwnerSeedObserverFaults.Reason+": 1 observation hook fault(s): UberDebug.LogException System.InvalidOperationException: observer body failed",StringComparison.Ordinal));
 Check("observer fault stack classifies as kmc-or-loader",Chunk7OwnerSeedFailureClassifier.Classify(null,null,new string[0],false,faults.KmcFrameLines())=="kmc-or-loader");
 Check("no observer fault leaves qualification unaffected",new Chunk7OwnerSeedObserverFaults().QualificationFailure()==null);
 var quiet=new Chunk7OwnerSeedObserverFaults();var ran=false;quiet.Run("ResourcesLibrary.TryGetBlueprint",()=>{ran=true;});
 Check("non-throwing observer body runs once and records nothing",ran&&quiet.Count==0&&quiet.Snapshot().Length==0);
 // Harness assertions by stable identifier: a non-refusal assertion never reaches seed-dependency
 // through names; KMC invariants are KMC faults; seed content is the seed's only without a KMC frame.
 Func<string[],string[],string[],string,Chunk7OwnerSeedFailureClassifier.Decision> decide=(bp,binds,ids,_)=>Chunk7OwnerSeedFailureClassifier.Decide(binds,bp,new string[0],false,new string[0],ids);
 foreach(var id in new[]{"kmc-mounted-custody","kmc-native-write-cast-restoration","kmc-native-restoration-entries","kmc-metadata-not-missing","kmc-observer-fault","kmc-observer-not-installed"}){
  var foreignType=decide(null,new[]{"CallOfTheWild.UnitPartFlying, CallOfTheWild"},new[]{id},null);var blueprint=decide(new[]{new string('9',32)},null,new[]{id},null);
  Check("KMC-invariant assertion "+id+" with a coincidental foreign type is kmc-or-loader",foreignType.Label=="kmc-or-loader"&&foreignType.DecidedBy=="assertion:"+id&&blueprint.Label=="kmc-or-loader");
  Check("KMC-invariant assertion "+id+" outranks a seed-content assertion",decide(null,null,new[]{"seed-lost-content",id},null).Label=="kmc-or-loader");}
 foreach(var id in new[]{"seed-lost-content","seed-dlc-unavailable"}){
  var decided=decide(null,new[]{"CallOfTheWild.UnitPartFlying, CallOfTheWild"},new[]{id},null);
  Check("seed-content assertion "+id+" without a KMC frame is seed-dependency",decided.Label=="seed-dependency"&&decided.DecidedBy=="assertion:"+id);
  Check("seed-content assertion "+id+" with a KMC frame is kmc-or-loader",Chunk7OwnerSeedFailureClassifier.Decide(null,null,new string[0],false,new[]{kmcStack},new[]{id}).Label=="kmc-or-loader");
  Check("seed-content assertion "+id+" under a refusal is runner-or-authority",Chunk7OwnerSeedFailureClassifier.Decide(null,null,new string[0],true,null,new[]{id}).Label=="runner-or-authority");}
 foreach(var id in new[]{"seed-identity-mismatch","unidentified","native-control-state","game-logged-exception","owner-seed-qualification","no-such-assertion"}){
  var decided=decide(new[]{new string('9',32)},new[]{"CallOfTheWild.UnitPartFlying, CallOfTheWild"},new[]{id},null);
  Check("non-content assertion "+id+" with foreign evidence is unclassified",decided.Label=="unclassified"&&decided.DecidedBy=="assertion:"+id);}
 Check("mapping is explicit: message text never decides",Chunk7OwnerSeedAssertions.KindOf("Cold inventory found mounted custody.")==Chunk7OwnerSeedAssertions.Kind.Other&&Chunk7OwnerSeedAssertions.KindOf("kmc-mounted-custody")==Chunk7OwnerSeedAssertions.Kind.KmcInvariant);
 Check("native-load-incomplete outcome lets the evidence decide",decide(null,new[]{"CallOfTheWild.UnitPartFlying, CallOfTheWild"},new[]{"native-load-incomplete"},null).Label=="seed-dependency"&&decide(null,new[]{"CallOfTheWild.UnitPartFlying, CallOfTheWild"},new[]{"native-load-incomplete","native-control-state"},null).Label=="unclassified"&&decide(null,new[]{"CallOfTheWild.UnitPartFlying, CallOfTheWild"},new[]{"native-load-incomplete","kmc-mounted-custody"},null).Label=="kmc-or-loader");
var bpOnly=decide(new[]{new string('9',32)},null,new[]{"native-load-incomplete"},null); Check("native-load-incomplete with blueprint misses only is unclassified",bpOnly.Label=="unclassified"&&bpOnly.DecidedBy=="assertion:native-load-incomplete;blueprint-misses-only"); Check("native-load-incomplete with blueprint misses and an exception but no KMC frame is unclassified",Chunk7OwnerSeedFailureClassifier.Decide(null,new[]{new string('9',32)},new[]{"System.NullReferenceException: Object reference not set to an instance of an object"},false,new[]{gameStack},new[]{"native-load-incomplete"}).Label=="unclassified"); Check("native-load-incomplete with blueprint misses and a named foreign assembly is seed-dependency",Chunk7OwnerSeedFailureClassifier.Decide(null,new[]{new string('9',32)},new[]{"Newtonsoft.Json.JsonSerializationException: Could not load assembly 'KingmakerGunslinger'."},false,new[]{gameStack},new[]{"native-load-incomplete"}).Label=="seed-dependency"); Check("seed-identity-mismatch is not seed content",Chunk7OwnerSeedAssertions.KindOf("seed-identity-mismatch")==Chunk7OwnerSeedAssertions.Kind.Other&&Chunk7OwnerSeedAssertions.IsKnown("seed-identity-mismatch"));
 Check("no assertion keeps the evidence label and says so",decide(null,new[]{"CallOfTheWild.UnitPartFlying, CallOfTheWild"},new string[0],null).DecidedBy=="named-evidence");
 // A KMC frame below 4000 characters of game frames: decided on the full stack, stored bounded.
 var deep=new System.Text.StringBuilder();for(var i=0;i<60;i++)deep.Append("  at Kingmaker.EntitySystem.Persistence.Deep.Frame"+i+" (System.Object state, System.String name) [0x00000] in <00000000000000000000000000000000>:0\n");deep.Append("  at KingmakerMountedCombat.Integration.MountedPersistenceService.OnLoad () [0x00012] in <0>:0\n");
 var deepText=deep.ToString();var deepLines=Chunk7OwnerSeedFailureClassifier.KmcFrameLines(deepText);
 Check("deep KMC frame beyond 4000 characters is found on the full stack",deepText.Length>4000&&!Chunk7OwnerSeedFailureClassifier.HasKmcFrame(Chunk7OwnerSeedFailureClassifier.BoundStack(deepText))&&deepLines.Length==1&&deepLines[0].Contains("KingmakerMountedCombat.Integration.MountedPersistenceService.OnLoad"));
 Check("deep KMC frame with foreign type text is kmc-or-loader",Chunk7OwnerSeedFailureClassifier.Classify(null,null,new[]{"Error resolving type specified in JSON 'CallOfTheWild.UnitPartFlying, CallOfTheWild'."},false,deepLines)=="kmc-or-loader");
 Exception deepError=null;try{try{throw new InvalidOperationException("inner");}catch(Exception inner){throw new InvalidOperationException("outer",inner);}}catch(Exception outer){deepError=outer;}
 Check("frame lines cover inner exceptions",Chunk7OwnerSeedFailureClassifier.KmcFrameLines(deepError).Length>=2);
 var deepFaults=new Chunk7OwnerSeedObserverFaults();deepFaults.Run("UberDebug.LogException",()=>Recurse(80));
 Check("observer fault frame verdict uses the full stack",deepFaults.Snapshot()[0].Stack.Length<=4000&&deepFaults.KmcFrameLines().Length>0&&Chunk7OwnerSeedFailureClassifier.Classify(null,null,new string[0],false,deepFaults.KmcFrameLines())=="kmc-or-loader"); Console.WriteLine("TOTAL PASS="+passed+" FAIL="+failed);return failed==0?0:1;
 }
}}
