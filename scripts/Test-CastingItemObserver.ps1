param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
$json=Join-Path $managed 'Newtonsoft.Json.dll'
[Reflection.Assembly]::LoadFrom($json)|Out-Null
Add-Type -ReferencedAssemblies $json -TypeDefinition @"
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
public static class CastingObserverProbe {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static int passed;
 static void Check(bool condition,string why){if(!condition)throw new InvalidOperationException(why);passed++;Console.WriteLine("PASS "+why);}
 public static void Run(string managed,string product){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
   var leaf=new AssemblyName(e.Name).Name+".dll";
   foreach(var dir in new[]{managed,Path.Combine(managed,"UnityModManager")}){var p=Path.Combine(dir,leaf);if(File.Exists(p))return Assembly.LoadFrom(p);}return null;
  };
  var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
  Check(native.ManifestModule.ModuleVersionId.ToString()=="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7","exact native assembly");
  var mod=Assembly.LoadFrom(product);var type=mod.GetType("KingmakerMountedCombat.Diagnostics.NativeCastingItemTrace",true);
  var child=mod.GetType("KingmakerMountedCombat.Diagnostics.Phase3dHorseScenarioTranche",true);
  var parent=mod.GetType("KingmakerMountedCombat.Diagnostics.Chunk6aMammothScenarioEngine",true);
  foreach(var scenario in new[]{"chunk6c-casting-rt","chunk6c-casting-tb","chunk6c-casting-unmounted-rt","chunk6c-casting-unmounted-tb"}){
   Check((bool)parent.GetMethod("SupportsScenario",F).Invoke(null,new object[]{scenario}),"native original-pair engine accepts "+scenario);
   Check((bool)child.GetMethod("IsChunk6cCastingScenario",F).Invoke(null,new object[]{scenario}) &&
    !(bool)child.GetMethod("IsChunk6aCombatMountScenario",F).Invoke(null,new object[]{scenario}),"compiled child chooses casting rather than Mount/Dismount for "+scenario);
  }
  foreach(var field in new[]{"RealTimeScenario","TurnBasedScenario"}){
   var scenario=(string)parent.GetField(field,F).GetRawConstantValue();
   Check((bool)child.GetMethod("IsChunk6aCombatMountScenario",F).Invoke(null,new object[]{scenario}),"existing native Mammoth Mount/Dismount classification retained "+scenario);
  }
  var actor=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
  var rider=FormatterServices.GetUninitializedObject(actor);var mount=FormatterServices.GetUninitializedObject(actor);
  var owner=Activator.CreateInstance(type,F,null,new[]{rider,mount},null);
  try {
   var capture=(JObject)type.GetMethod("Capture",F).Invoke(owner,null);
   Check((int)capture["faults"]==0&&(int)capture["dropped"]==0,"fresh bounded observer complete");
   Check(((JArray)capture["hooks"]).Count==6,"all six exact native observation hooks installed");
   Check(type.GetMethod("ConcentrationAfter",F).GetParameters().Length==1,"void native concentration postfix collects no invented result");
   var item=native.ManifestModule.ResolveMethod(0x06007B74);
   Check(item.GetParameters()[0].Name=="user","item spending observer binds exact native user parameter");
   var duplicateRefused=false;try{Activator.CreateInstance(type,F,null,new[]{rider,mount},null);}catch(TargetInvocationException e){duplicateRefused=e.InnerException is InvalidOperationException;}
   Check(duplicateRefused,"second observer cannot replace current owner");
   var before=(int)capture["faults"];
   foreach(var eventType in new[]{"Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell","Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage","Kingmaker.RuleSystem.Rules.Damage.RuleHealDamage","Kingmaker.RuleSystem.Rules.Abilities.RuleCheckConcentration","Kingmaker.RuleSystem.Rules.Abilities.RuleCheckCastingDefensively","Kingmaker.RuleSystem.Rules.RuleSummonUnit"}){
    var eventNative=native.GetType(eventType,true);var method=type.GetMethod("OnEventDidTrigger",F,null,new[]{eventNative},null);
    method.Invoke(owner,new object[]{null});
   }
   var faulted=(JObject)type.GetMethod("Capture",F).Invoke(owner,null);
   Check((int)faulted["faults"]==before+6,"rule observation failures remain explicit without escaping into native callbacks");
   Check(!(bool)type.GetProperty("Complete",F).GetValue(owner,null),"observation failure cannot pass as complete evidence");
  } finally {((IDisposable)owner).Dispose();}
  var closed=(JObject)type.GetMethod("Capture",F).Invoke(owner,null);
  Check((bool)closed["identityRegistry"]["released"]&&(int)closed["identityRegistry"]["retainedCount"]==0,"closed trace releases exact retained identities");
  ((IDisposable)owner).Dispose();
  Check(JToken.DeepEquals(closed,(JObject)type.GetMethod("Capture",F).Invoke(owner,null)),"repeat disposal preserves immutable terminal evidence");
  var next=Activator.CreateInstance(type,F,null,new[]{rider,mount},null);((IDisposable)next).Dispose();
  Check(type.GetField("active",F).GetValue(null)==null,"observer unpatch and retirement permit next bounded fixture");
  Console.WriteLine("TOTAL PASS="+passed+" FAIL=0");
 }
}
"@
[CastingObserverProbe]::Run($managed,(Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"))