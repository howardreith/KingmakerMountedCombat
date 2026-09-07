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
  // Native movement deliberately ignores the command-slot cooldown. Its real
  // debit is made by the movement controller; this is not an attack exemption.
  var move=native.GetType("Kingmaker.UnitLogic.Commands.UnitMoveTo",true);
  var command=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);
  var ignore=command.GetMethod("IgnoreCooldown",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
  var constructors=move.GetConstructors();
  if(ignore==null || constructors.Length!=2) throw new InvalidOperationException("Native movement constructor contract changed.");
  foreach(var constructor in constructors) {
   var il=constructor.GetMethodBody().GetILAsByteArray();
   var calls=0;
   for(var offset=0;offset+4<il.Length;offset++)
    if(il[offset]==0x28 && BitConverter.ToInt32(il,offset+1)==ignore.MetadataToken) calls++;
   if(calls!=1) throw new InvalidOperationException("Native movement cooldown convention changed.");
  }
  Console.WriteLine("NATIVE MOVE CONSTRUCTOR CONTRACT PASS=2 FAIL=0; no constructor invoked");
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
   var observer=candidate.GetType("KingmakerMountedCombat.Diagnostics.NativeActorAllocationTrace+Hooks",true);
   var observerNames=new[]{"PhysicalTick","PhysicalMove"};
   var nativeObserverNames=new[]{"TickMovement","Move"};
   var nativeParameterTypes=new[]{"System.Single","UnityEngine.Vector3"};
   var observerTokens=new[]{0x060018AA,0x060018DB};
   for(var i=0;i<observerTokens.Length;i++) {
    var original=native.ManifestModule.ResolveMethod(observerTokens[i]);
    var before=observer.GetMethod(observerNames[i]+"Before",BindingFlags.Static|BindingFlags.NonPublic);
    var after=observer.GetMethod(observerNames[i]+"After",BindingFlags.Static|BindingFlags.NonPublic);
    if(original.Name!=nativeObserverNames[i] || original.IsStatic || original.GetParameters().Length!=1 ||
       original.GetParameters()[0].ParameterType.FullName!=nativeParameterTypes[i] || before==null || after==null)
     throw new InvalidOperationException("Native physical movement observation contract changed.");
    Console.WriteLine("PASS native movement observation signature "+original.Name);
   }
   // These bodies call Unity ECalls which cannot be JIT-constructed in this
   // isolated CLR. Actual patch installation is required in the native scenario.
   Console.WriteLine("MOVEMENT OBSERVER SIGNATURE PASS=2 FAIL=0; runtime construction still required");
  } finally {harmonyType.GetMethod("UnpatchAll").Invoke(harmony,new object[]{id});}
  Console.WriteLine("PATCH CONSTRUCTION PASS="+count+" FAIL=0; no game method invoked or game file written");
 }
}
'@
try {
 [KmcNativePatchProbe]::Run($kmcManaged, $kmcDll)
} catch { Write-Output $_.Exception.ToString(); exit 1 }
