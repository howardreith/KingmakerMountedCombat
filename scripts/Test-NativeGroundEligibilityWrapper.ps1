param([ValidateSet('Debug','Release')][string]$Configuration='Release')

$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
$dll=Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')
Add-Type -TypeDefinition @"
using System;
using System.IO;
using System.Reflection;
public static class KmcGroundEligibilityWrapperProbe {
 public static void Run(string managed,string dll,bool ignored) {
  ResolveEventHandler resolver=(sender,args)=>{
   string name=new AssemblyName(args.Name).Name+".dll";
   foreach(string folder in new[]{managed,Path.Combine(managed,"UnityModManager")}) {
    string file=Path.Combine(folder,name);if(File.Exists(file))return Assembly.LoadFrom(file);
   }
   return null;
  };
  AppDomain.CurrentDomain.AssemblyResolve+=resolver;
  var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
  if(native.ManifestModule.ModuleVersionId!=new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))throw new Exception("Pinned native MVID differs");
  var candidate=Assembly.LoadFrom(dll);
  var ha=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony12.dll"));
  var ht=ha.GetType("Harmony12.HarmonyInstance",true);var hm=ha.GetType("Harmony12.HarmonyMethod",true);
  string id="KingmakerMountedCombat.Diagnostics.DesktopWrappers.GroundEligibility";
  var h=ht.GetMethod("Create").Invoke(null,new object[]{id});
  var hookType=candidate.GetType("KingmakerMountedCombat.Diagnostics.NativeActorAllocationTrace+Hooks",true);
  int[] tokens=new[]{0x06009343};
  string[] prefixes=new string[]{"CooldownEligibilityBefore"};
  string[] postfixes=new[]{"CooldownEligibilityAfter"};
  var flags=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
  try {
   for(int i=0;i<tokens.Length;i++) {
    var original=native.ManifestModule.ResolveMethod(tokens[i]);
    var body=original.GetMethodBody().GetILAsByteArray();
    string ilHash;
    using(var sha=System.Security.Cryptography.SHA256.Create())ilHash=BitConverter.ToString(sha.ComputeHash(body)).Replace("-","").ToLowerInvariant();
    if(ilHash!="768ca06c90b9d6989de327f8f8537d30aae9e92e3fef922a78929a5407b215cc" ||
       original.DeclaringType.FullName!="Kingmaker.Controllers.Combat.BaseUnitCombatController" || original.Name!="ShouldTickOnUnit" ||
       ((MethodInfo)original).ReturnType!=typeof(bool) || original.GetParameters().Length!=1 ||
       original.GetParameters()[0].ParameterType.FullName!="Kingmaker.EntitySystem.Entities.UnitEntityData")throw new Exception("Native outside-combat eligibility contract differs");
    Console.WriteLine("PASS pinned native eligibility body and signature: "+ilHash);
    var before=prefixes[i]==null?null:Activator.CreateInstance(hm,new object[]{hookType.GetMethod(prefixes[i],flags)});
    var after=Activator.CreateInstance(hm,new object[]{hookType.GetMethod(postfixes[i],flags)});
    try {
     ht.GetMethod("Patch").Invoke(h,new object[]{original,before,after,null});
     Console.WriteLine("PASS desktop wrapper construction: "+original.DeclaringType.FullName+"."+original.Name+" token="+tokens[i].ToString("X8"));
    } catch(TargetInvocationException e) {
     if(!(e.InnerException is System.Security.SecurityException)||e.InnerException.Message!="ECall methods must be packaged into a system module.")throw;
     Console.WriteLine("DEFER - EVIDENCED: desktop Unity ECall wrapper "+original.DeclaringType.FullName+"."+original.Name+" token="+tokens[i].ToString("X8")+"; actual Unity installation/execution required");
    }
   }
  } finally {ht.GetMethod("UnpatchAll").Invoke(h,new object[]{id});AppDomain.CurrentDomain.AssemblyResolve-=resolver;}
  Console.WriteLine("Desktop wrapper construction finished; no game method invoked, no native qualification.");
 }
}
"@
[KmcGroundEligibilityWrapperProbe]::Run($managed,$dll,$false)