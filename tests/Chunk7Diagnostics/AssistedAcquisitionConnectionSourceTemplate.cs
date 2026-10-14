using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Integration;
namespace KingmakerMountedCombat.Integration {internal static class HorseCompanionBlueprintService {internal const string UnitGuid="4016c7db400ab721ff125aef9e65e202", FeatureGuid="7db7c50677e39f09feef56f3831fc723",LevelRankGuid="1670990255e4fe948a863bafd5dbda5d";}}
namespace KingmakerMountedCombat.Diagnostics {
internal static class Chunk7AssistedFixtureScenario {internal const string EvidenceKind="chunk7-assisted-fixture-evidence";}
internal static class Chunk7AssistedColdInventoryScenario {internal const string EvidenceKind="chunk7-assisted-cold-inventory-evidence";}
internal sealed partial class Chunk7ManualDerivedScenario {
private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
__ACTUAL_METHODS__
}}
namespace KingmakerMountedCombat.Tests {internal static class AssistedAcquisitionConnectionSourceTests {
private static JObject producer,cold;
internal static RuntimeRequest Request(JObject p,JObject c){var r=AssistedAcquisitionConnectionProtocolTests.AssistedRequest();var d=p["observations"]["finalManual"];var s=r.CumulativeAcquisition;
r.Commit=(string)p["commit"];r.DllSha256=(string)p["dllSha256"];r.DllMvid=(string)p["dllMvid"];s.SourceRunId=(string)p["runId"];s.AssistedColdRunId=(string)c["runId"];s.SourceArtifactSha256=(string)c["observations"]["selection"]["sourceArtifactSha256"];s.AssistedColdArtifactSha256=new string('e',64);s.SourceArchiveSha256=(string)d["sha256"];
r.PersistenceLoad=new RuntimeSaveDescriptor{FileName=(string)d["fileName"],InternalName=(string)d["internalName"],Sha256=(string)d["sha256"],Length=(long)d["length"],LastWriteTimeUtcTicks=(long)d["lastWriteTimeUtcTicks"],GameId=(string)d["gameId"],GameName=(string)d["gameName"],Area=(string)d["area"]};
var w=c["observations"]["loadedWorld"];var ready=c["observations"]["fixtureReadiness"];s.PriorOwnerId=(string)w["mainActorId"];s.PriorPetId=(string)w["party"][0]["petId"];s.ExistingActorId=(string)w["party"][1]["id"];s.ExistingLevel=(int)w["party"][1]["characterLevel"];s.ExistingExperience=(int)w["party"][1]["experience"];s.ExistingRangerLevel=0;
s.Readiness.MainActorId=s.PriorOwnerId;s.Readiness.MainLevel=(int)ready["mainProgression"]["characterLevel"];s.Readiness.MainExperience=(int)ready["mainProgression"]["experience"];s.Readiness.Money=(long)w["nativeMoney"];s.Readiness.PartyCharacterIds=((JArray)w["partyCharacterIds"]).Select(v=>(string)v).OrderBy(v=>v,StringComparer.Ordinal).ToArray();return r;}
private static void Refused(Action action){bool refused=false;try{action();}catch{refused=true;}TestRunner.True(refused,"Expected source boundary refusal.");}
private static JObject Project(JObject p,JObject c)=>Chunk7ManualDerivedScenario.RequireAssistedCumulativeSource(Request(p,c),p,c);
internal static int Main(string[] args){producer=JObject.Parse(File.ReadAllText(args[0]));cold=JObject.Parse(File.ReadAllText(args[1]));var t=new TestRunner();
t.Run("generated request remains exactly closed",()=>TestRunner.Equal(0,Request(producer,cold).Validate().Count,string.Join(";",Request(producer,cold).Validate())));
t.Run("native emitter main XP omission admits and projects readiness XP",()=>{TestRunner.True(producer["observations"]["settledWorld"]["party"][0]["experience"]==null,"Fixture no longer reproduces omitted main XP.");var v=Project(producer,cold);TestRunner.Equal(1100,(int)v["afterSaveArrangement"]["actors"][0]["experience"],"Main XP projection differs.");TestRunner.Equal(100,(int)v["afterSaveArrangement"]["actors"][1]["experience"],"Unrelated XP projection differs.");TestRunner.True(JToken.DeepEquals(v["resourcesAfterSave"]["actors"][0]["move"],producer["observations"]["settledWorld"]["party"][0]["actions"]["move"]),"Native action observation changed.");File.WriteAllText(Path.Combine(args[2],"projection.json"),v.ToString());File.WriteAllText(Path.Combine(args[2],"request.json"),JsonConvert.SerializeObject(Request(producer,cold),Formatting.Indented,new JsonSerializerSettings{ContractResolver=new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()}));});
Action<string,Action<JObject,JObject>> reject=(name,mutation)=>t.Run(name,()=>{var p=(JObject)producer.DeepClone();var c=(JObject)cold.DeepClone();mutation(p,c);Refused(()=>Project(p,c));});
reject("loaded main XP drift",(p,c)=>c["observations"]["loadedWorld"]["party"][0]["experience"]=1101);
reject("settled main XP drift",(p,c)=>c["observations"]["settledWorld"]["party"][0]["experience"]=1101);
reject("loaded unrelated XP drift",(p,c)=>c["observations"]["loadedWorld"]["party"][1]["experience"]=101);
reject("producer main readiness omitted",(p,c)=>((JObject)p["observations"]["fixtureReadiness"]["mainProgression"]).Remove("experience"));
reject("producer main readiness wrong actor",(p,c)=>p["observations"]["fixtureReadiness"]["mainProgression"]["actorId"]="77777777-7777-4777-8777-777777777777");
reject("cold readiness main XP drift",(p,c)=>c["observations"]["fixtureReadiness"]["mainProgression"]["experience"]=1101);
reject("producer FAIL",(p,c)=>p["status"]="FAIL");reject("cold FAIL",(p,c)=>c["status"]="FAIL");
reject("candidate DLL drift",(p,c)=>c["dllSha256"]=new string('f',64));reject("source copied world drift",(p,c)=>c["observations"]["sourceNativeWorld"]["party"][0]["actions"]["move"]=9);
reject("manual descriptor name",(p,c)=>p["observations"]["finalManual"]["internalName"]="KMC_OTHER");
reject("cold source producer pin",(p,c)=>c["observations"]["source"]["runId"]="other-source");
reject("cold cleanup source mutated",(p,c)=>c["observations"]["cleanup"]["sourceUnchanged"]=false);
reject("cold callback missing",(p,c)=>c["observations"]["settledLoader"]["callbackObserved"]=false);
reject("loaded native busy",(p,c)=>c["observations"]["loadedWorld"]["nativeLoading"]=true);
reject("settled scheduled dialog",(p,c)=>c["observations"]["settledWorld"]["dialogScheduled"]=true);
reject("money drift",(p,c)=>c["observations"]["sourceNativeMoney"]=4001);
reject("duplicate actor",(p,c)=>{c["observations"]["settledWorld"]["party"][1]["id"]=c["observations"]["settledWorld"]["party"][0]["id"].DeepClone();});
reject("prior pet reciprocity",(p,c)=>p["observations"]["settledWorld"]["party"][2]["masterId"]=null);
reject("prior Horse feature omitted",(p,c)=>{((JArray)p["observations"]["settledWorld"]["party"][0]["features"]).RemoveAt(0);c["observations"]["sourceNativeWorld"]=p["observations"]["settledWorld"].DeepClone();});
reject("prior rank zero",(p,c)=>{p["observations"]["settledWorld"]["party"][0]["features"][1]["rank"]=0;c["observations"]["sourceNativeWorld"]=p["observations"]["settledWorld"].DeepClone();});
reject("native resource observations omitted",(p,c)=>{((JObject)p["observations"]["settledWorld"]["party"][1]).Remove("actions");c["observations"]["sourceNativeWorld"]=p["observations"]["settledWorld"].DeepClone();});
return t.Complete();}
}}
