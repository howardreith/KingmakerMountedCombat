[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')

# Construct and roll back each native injection in this isolated process.
# No native game methods execute, and no game installation files are written.
$kmcRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$kmcLab=[IO.Path]::GetFullPath((Join-Path $kmcRepo '../..'))
$kmcLayout=(Get-Content -Raw (Join-Path $kmcLab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$kmcManaged=Join-Path $kmcLayout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$kmcDll=Join-Path $kmcRepo "bin/$Configuration/KingmakerMountedCombat.dll"
$ErrorActionPreference='Stop'
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Reflection;
public static class KmcNativePatchProbe {
 public static void Run(string managed, string mod) {
  ResolveEventHandler resolver=(sender,args)=>{
   var leaf=new AssemblyName(args.Name).Name+".dll";
   foreach(var folder in new[]{managed,Path.Combine(managed,"UnityModManager")}){
    var path=Path.Combine(folder,leaf);
    if(File.Exists(path)) return Assembly.LoadFrom(path);
   }
   return null;
  };
  AppDomain.CurrentDomain.AssemblyResolve+=resolver;
  Console.WriteLine("stage: resolver attached");
  var harmonyAssembly=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony12.dll"));
  var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
  var candidate=Assembly.LoadFrom(mod);
  Console.WriteLine("stage: assemblies loaded");
  var hooks=candidate.GetType("KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods",true);
  var harmonyType=harmonyAssembly.GetType("Harmony12.HarmonyInstance",true);
  var harmonyMethod=harmonyAssembly.GetType("Harmony12.HarmonyMethod",true);
  var id="KingmakerMountedCombat.LocalPatchConstruction";
  var harmony=harmonyType.GetMethod("Create").Invoke(null,new object[]{id});
  var patch=harmonyType.GetMethod("Patch");
  var names=new[]{"PairedSelectorTranspiler","PairedPreparationTranspiler","PairedActivityTranspiler","PairedEligibilityTranspiler","PairedConfusionTranspiler"};
  var tokens=new[]{0x06000BD2,0x06000C3C,0x06000C34,0x0600911D,0x06009131};
  var count=0;
  try {
   for(var i=0;i<tokens.Length;i++) {
    var original=native.ManifestModule.ResolveMethod(tokens[i]);
    var hook=hooks.GetMethod(names[i],BindingFlags.Static|BindingFlags.NonPublic);
    var method=Activator.CreateInstance(harmonyMethod,new object[]{hook});
    Console.WriteLine("stage: constructing "+original.Name);
    patch.Invoke(harmony,new object[]{original,null,null,method});
    count++;Console.WriteLine("PASS native IL patch construction "+original.Name);
   }
  } finally {harmonyType.GetMethod("UnpatchAll").Invoke(harmony,new object[]{id});}
  Console.WriteLine("PATCH CONSTRUCTION PASS="+count+" FAIL=0; no game method invoked or game file written");
 }
}
'@
try {
 [KmcNativePatchProbe]::Run($kmcManaged, $kmcDll)
} catch { Write-Output $_.Exception.ToString(); exit 1 }
