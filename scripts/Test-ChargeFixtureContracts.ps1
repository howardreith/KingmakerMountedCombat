param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
$json=Join-Path $managed 'Newtonsoft.Json.dll'
[Reflection.Assembly]::LoadFrom($json)|Out-Null
Add-Type -ReferencedAssemblies $json -TypeDefinition @'
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class ChargeFixtureProbe {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static int passed;
 static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);passed++;Console.WriteLine("PASS "+label);}
 static object Blank(Type t){return FormatterServices.GetUninitializedObject(t);}
 static void Set(object instance,string field,object value){var f=instance.GetType().GetField(field,F);if(f==null)throw new MissingFieldException(instance.GetType().FullName,field);f.SetValue(instance,value);}
 public static void Run(string managed,string product,string observer){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{
   string leaf=new AssemblyName(a.Name).Name+".dll";
   foreach(var root in new[]{managed,Path.Combine(managed,"UnityModManager")}){var path=Path.Combine(root,leaf);if(File.Exists(path))return Assembly.LoadFrom(path);}return null;
  };
  var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
  Check(native.ManifestModule.ModuleVersionId.ToString()=="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7","exact installed native identity");
  var mod=Assembly.LoadFrom(product);var obs=Assembly.LoadFrom(observer);
  var sourceType=obs.GetType("KmcRemovalObserver.ObserverChargeSource",true);
  var source=Activator.CreateInstance(sourceType);
  string[] names={"RunId","ObservationsSha256","RiderId","MountId","ChargeBuffGuid"};
  string[] values={"owned-source",new string('a',64),"rider","mount",new string('b',32)};
  for(int i=0;i<names.Length;i++)sourceType.GetProperty(names[i]).SetValue(source,values[i],null);
  var previous=JsonConvert.DefaultSettings;
  try{
   JsonConvert.DefaultSettings=()=>new JsonSerializerSettings{PreserveReferencesHandling=PreserveReferencesHandling.Objects};
   Check(JObject.FromObject(source).Property("$id")!=null,"native-like global settings reproduce the rejected reference metadata");
   var actual=(JObject)sourceType.GetMethod("CaptureEvidence",F).Invoke(source,null);
   var expected=new JObject{{"runId",values[0]},{"observationsSha256",values[1]},{"riderId",values[2]},{"mountId",values[3]},{"chargeBuffGuid",values[4]}};
   Check(JToken.DeepEquals(expected,actual)&&actual.Properties().Count()==5,"compiled observer emits every exact identity fact without serializer metadata");
   sourceType.GetProperty("MountId").SetValue(source,"changed-mount",null);
   Check((string)((JObject)sourceType.GetMethod("CaptureEvidence",F).Invoke(source,null))["mountId"]=="changed-mount"&&(string)actual["mountId"]=="mount","independent snapshots preserve source identity without a stale shared reference");
  }finally{JsonConvert.DefaultSettings=previous;}
  var scenarioType=mod.GetType("KingmakerMountedCombat.Diagnostics.RuntimePersistenceScenario",true);
  var scenario=Blank(scenarioType);
  var commandType=mod.GetType("KingmakerMountedCombat.Integration.MountedPairAttackCommand",true);
  var command=Blank(commandType);
  Set(command,"chargeLease",Blank(mod.GetType("KingmakerMountedCombat.Integration.MountedChargeLease",true)));
  Set(scenario,"persistenceChargeCommand",command);
  var capture=scenarioType.GetMethod("CaptureChargePersistenceLease",F);
  Check(capture.Invoke(scenario,null)==null,"disposed session does not dereference the retained command's invalid native lease");
  var actor=Blank(native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true));
  Set(scenario,"rider",actor);
  Check(capture.Invoke(scenario,null)==null,"partially absent pair cannot be described as a current native lease");
  Set(scenario,"mount",actor);
  bool propagated=false;
  try{capture.Invoke(scenario,null);}catch(TargetInvocationException){propagated=true;}
  Check(propagated,"unexpected lease observation failure in a live pair remains a failure");
  Set(scenario,"persistenceChargeCommand",null);
  Check(capture.Invoke(scenario,null)==null,"settled live pair with no charge command has no lease facts");
  // Native hook installation requires Unity. Exercise the compiled observer with
  // only that boundary replaced; identity, capture and disposal are actual code.
  var traceType=mod.GetType("KingmakerMountedCombat.Diagnostics.NativeActorAllocationTrace",true);
  var identityType=mod.GetType("KingmakerMountedCombat.Diagnostics.RetainedObjectIdentity",true);
  var identities=Activator.CreateInstance(identityType,F,null,new object[]{16000,new Func<object,int>(value=>41)},null);
  var trace=Blank(traceType);
  Set(trace,"identities",identities);Set(trace,"events",new JArray());Set(trace,"installedHooks",new JArray());
  var harmonyType=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony12.dll")).GetType("Harmony12.HarmonyInstance",true);
  Set(trace,"harmony",harmonyType.GetMethod("Create").Invoke(null,new object[]{"KingmakerMountedCombat.Diagnostics.ActorAllocation"}));
  var observe=traceType.GetMethod("ObjectIdentity",F);var internalId=traceType.GetMethod("Id",F);
  var traceCapture=traceType.GetMethod("Capture",F);var complete=traceType.GetProperty("Complete",F);
  var exact=new object();
  Check((int)observe.Invoke(trace,new[]{exact})==41 && (int)internalId.Invoke(trace,new[]{exact})==41 &&
        (int)observe.Invoke(trace,new object[]{null})==0 && (bool)complete.GetValue(trace,null),
        "compiled native trace shares exact identities between rule and command observations");
  var identityProof=(JObject)((JObject)traceCapture.Invoke(trace,null))["identityRegistry"];
  Check((string)identityProof["contract"]=="retained-native-object-identity-v1" &&
        (int)identityProof["capacity"]==16000 && (int)identityProof["retainedCount"]==1 &&
        !(bool)identityProof["released"] && (int)identityProof["faults"]==0 &&
        ((JArray)identityProof["ids"]).Count==1 && (int)identityProof["ids"][0]==41,
        "compiled capture truthfully records a live retained native identity");
  bool collision=false;
  try{observe.Invoke(trace,new[]{new object()});}catch(TargetInvocationException e){collision=e.InnerException is InvalidOperationException;}
  Check(collision && !(bool)complete.GetValue(trace,null) && (int)internalId.Invoke(trace,new[]{exact})==41,
        "compiled observer rejects a colliding command and remains incomplete without losing its first owner");
  traceType.GetMethod("Dispose",F).Invoke(trace,null);
  traceType.GetMethod("Dispose",F).Invoke(trace,null);
  identityProof=(JObject)((JObject)traceCapture.Invoke(trace,null))["identityRegistry"];
  Check((bool)identityProof["released"] && (int)identityProof["retainedCount"]==0 &&
        (int)identityProof["faults"]==1 && (int)identityProof["ids"][0]==41 && !(bool)complete.GetValue(trace,null),
        "compiled trace disposal releases references while preserving failed identity evidence");
  bool afterClose=false;
  try{observe.Invoke(trace,new[]{exact});}catch(TargetInvocationException e){afterClose=e.InnerException is ObjectDisposedException;}
  Check(afterClose,"closed compiled trace cannot silently resume object observation");
  Console.WriteLine("TOTAL PASS="+passed+" FAIL=0");
 }
}
'@
[ChargeFixtureProbe]::Run($managed,(Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"),(Join-Path $repo "bin/Observer/$Configuration/KmcRemovalObserver.dll"))
