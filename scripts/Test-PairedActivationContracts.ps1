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
  var conditionTypes=new[]{"UnitDoNothing","UnitSelfHarm"};
  var conditionSlots=new[]{"Free","Standard"};
  var commandType=command.GetNestedType("CommandType",BindingFlags.Public);
  for(var i=0;i<conditionTypes.Length;i++) {
   var type=native.GetType("Kingmaker.UnitLogic.Commands."+conditionTypes[i],true);
   var il=type.GetConstructor(Type.EmptyTypes).GetMethodBody().GetILAsByteArray();
   if(il.Length!=9 || il[0]!=0x02 || il[1]!=0x16+i || il[2]!=0x14 || il[3]!=0x28 ||
      BitConverter.ToInt32(il,4)!=0x06002799 || il[8]!=0x2a || Enum.GetName(commandType,i)!=conditionSlots[i])
    throw new InvalidOperationException("Native condition command slot contract changed.");
  }
  Console.WriteLine("NATIVE CONDITION SLOT CONTRACT PASS=2 FAIL=0; no constructor invoked");
  var selfHarm=native.ManifestModule.ResolveMethod(0x0600270C).GetMethodBody().GetILAsByteArray();
  var nativeEnd=native.ManifestModule.ResolveMethod(0x06000C46).GetMethodBody().GetILAsByteArray();
  if(selfHarm.Length!=0x76 || selfHarm[0x6f]!=0x6f || BitConverter.ToInt32(selfHarm,0x70)!=0x06000C47 ||
     selfHarm[0x74]!=0x19 || nativeEnd[6]!=0x22 || BitConverter.ToSingle(nativeEnd,7)!=6f ||
     nativeEnd[0xb]!=0x6f || BitConverter.ToInt32(nativeEnd,0xc)!=0x0600C3B7)
   throw new InvalidOperationException("Native SelfHarm forfeiture and End normalization contract changed.");
  Console.WriteLine("NATIVE CONDITION END SETTLEMENT CONTRACT PASS=1 FAIL=0; no game method invoked");
  var damageDifficulty=native.ManifestModule.ResolveMethod(0x060073ff).GetMethodBody().GetILAsByteArray();
  var difficultyGetter=native.ManifestModule.ResolveMethod(0x06000cfb);
  if(difficultyGetter.DeclaringType.FullName!="Kingmaker.GameDifficulty" || difficultyGetter.Name!="get_DamageToParty" ||
     damageDifficulty.Length!=0x58 || damageDifficulty[0x37]!=0x6f || BitConverter.ToInt32(damageDifficulty,0x38)!=0x06000cfb ||
     damageDifficulty[0x4e]!=0x5a || damageDifficulty[0x4f]!=0x69)
   throw new InvalidOperationException("Native enemy damage difficulty/truncation contract changed.");
  Console.WriteLine("NATIVE DAMAGE DIFFICULTY CONTRACT PASS=1 FAIL=0; no game method invoked");
  Console.WriteLine("stage: assemblies loaded");
  // Resolve the actual service's public/private native contracts, not a parallel
  // reflection inventory. This performs no game operations or instance creation.
  var coordinator=candidate.GetType("KingmakerMountedCombat.Integration.UnifiedMountedTurnCoordinator",true);
  System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(coordinator.TypeHandle);
  Console.WriteLine("COORDINATOR STARTUP CONTRACT PASS=1 FAIL=0; no game instance created");
  // L attempted Pause during TB. Native DoStartMode returns immediately for
  // that request; asserting a paused TB game would fabricate a product contract.
  var startMode=native.ManifestModule.ResolveMethod(0x06000CBF);
  var pauseIl=startMode.GetMethodBody().GetILAsByteArray();
  if(startMode.Name!="DoStartMode" || pauseIl.Length<32 || pauseIl[0x17]!=0x28 ||
     BitConverter.ToInt32(pauseIl,0x18)!=0x06000BF6 || pauseIl[0x1c]!=0x2c ||
     pauseIl[0x1d]!=1 || pauseIl[0x1e]!=0x2a)
   throw new InvalidOperationException("Native TB Pause rejection contract changed.");
  Console.WriteLine("NATIVE TB PAUSE CONTRACT PASS=1 FAIL=0; no game method invoked");
  var hooks=candidate.GetType("KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods",true);
  var harmonyType=harmonyAssembly.GetType("Harmony12.HarmonyInstance",true);
  var harmonyMethod=harmonyAssembly.GetType("Harmony12.HarmonyMethod",true);
  var id="KingmakerMountedCombat.LocalPatchConstruction";
  var harmony=harmonyType.GetMethod("Create").Invoke(null,new object[]{id});
  var patch=harmonyType.GetMethod("Patch");
  var names=new[]{"PairedSelectorTranspiler","PairedPreparationTranspiler","PairedActivityTranspiler","PairedEligibilityTranspiler",
   "PairedReadinessTranspiler","PairedReadinessTranspiler","PairedForfeitDebtTranspiler","PairedEndDebtTranspiler","PairedProneTranspiler",
   "PairedFullAttackInputTranspiler","PairedControllerInputTranspiler","PairedControllerInputTranspiler","PairedControllerInputTranspiler","PairedControllerInputTranspiler",
   "PairedVmConstructorTranspiler","PairedVmReaderTranspiler","PairedVmReaderTranspiler","PairedVmReaderTranspiler","PairedVmReaderTranspiler","PairedVmReaderTranspiler","PairedVmReaderTranspiler","PairedVmReaderTranspiler",
   "PairedPathUnitTranspiler","PairedPathSettingsTranspiler","PairedControllerInputTranspiler","PairedControllerInputTranspiler",
   "PairedPathUnitTranspiler","PairedControllerInputTranspiler","PairedConditionActionTranspiler","PairedConditionActionTranspiler"};
  var tokens=new[]{0x06000BD2,0x06000C3C,0x06000C34,0x0600911D,0x06000BD6,0x0600A2BE,0x06000C47,0x06000C46,0x0600918C,
   0x06009391,0x06000BDF,0x06000BE0,0x06000BE1,0x06003086,0x06004F2F,0x06004F29,0x06004F2A,0x06004F2B,0x06004F2C,0x06004F2D,0x06004F31,0x06004F33,
   0x06007020,0x06007015,0x0600700F,0x06007021,0x060093D5,0x060093DB,0x060026C9,0x0600270C};
  var count=0;
  var conditionAdapter=candidate.GetType("KingmakerMountedCombat.Integration.PairedConfusionPreparation",true);
  System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(conditionAdapter.TypeHandle);
  Console.WriteLine("NATIVE CONDITION ADAPTER FACTORY CONTRACT PASS=1 FAIL=0; no actor or game method invoked");
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
   var adapterBefore=Activator.CreateInstance(harmonyMethod,new object[]{observer.GetMethod("PairedConfusionBefore",BindingFlags.Static|BindingFlags.NonPublic)});
   var adapterAfter=Activator.CreateInstance(harmonyMethod,new object[]{observer.GetMethod("PairedConfusionAfter",BindingFlags.Static|BindingFlags.NonPublic)});
   patch.Invoke(harmony,new object[]{conditionAdapter.GetMethod("Prepare",BindingFlags.Static|BindingFlags.NonPublic),adapterBefore,adapterAfter,null});
   Console.WriteLine("CONDITION ADAPTER OBSERVER CONSTRUCTION PASS=1 FAIL=0; no actor or game method invoked");
   var removal=native.ManifestModule.ResolveMethod(0x06000BE6);
   if(removal.Name!="RemoveUnit" || removal.GetParameters().Length!=1 || removal.GetParameters()[0].Name!="unit" ||
      removal.GetParameters()[0].ParameterType.FullName!="Kingmaker.EntitySystem.Entities.UnitEntityData" ||
      observer.GetMethod("RemoveUnitBefore",BindingFlags.Static|BindingFlags.NonPublic)==null ||
      observer.GetMethod("RemoveUnitAfter",BindingFlags.Static|BindingFlags.NonPublic)==null)
    throw new InvalidOperationException("Native removal observation signature changed.");
   var deathIl=native.ManifestModule.ResolveMethod(0x06000BF2).GetMethodBody().GetILAsByteArray();
   if(deathIl.Length!=16 || deathIl[10]!=0x28 || BitConverter.ToInt32(deathIl,11)!=0x06000BE6)
    throw new InvalidOperationException("Native death no longer calls RemoveUnit at the verified boundary.");
   Console.WriteLine("NATIVE DEATH REMOVAL CONTRACT PASS=1 FAIL=0; runtime observer construction still required");
   var leaveIl=native.ManifestModule.ResolveMethod(0x0600939F).GetMethodBody().GetILAsByteArray();
   var clearMethod=native.ManifestModule.ResolveMethod(0x060093A4);
   var clearIl=clearMethod.GetMethodBody().GetILAsByteArray();
   var leaveHandlerIl=native.ManifestModule.ResolveMethod(0x06000BF1).GetMethodBody().GetILAsByteArray();
   if(leaveIl[0x38]!=0x28 || BitConverter.ToInt32(leaveIl,0x39)!=0x060093A4 ||
      clearIl[6]!=0x6f || BitConverter.ToInt32(clearIl,7)!=0x0600C3BE ||
      leaveHandlerIl.Length!=16 || leaveHandlerIl[10]!=0x28 || BitConverter.ToInt32(leaveHandlerIl,11)!=0x06000BE6)
    throw new InvalidOperationException("Native death leave/clear/removal sequence changed.");
   patch.Invoke(harmony,new object[]{clearMethod,
    Activator.CreateInstance(harmonyMethod,new object[]{observer.GetMethod("CombatClearBefore",BindingFlags.Static|BindingFlags.NonPublic)}),
    Activator.CreateInstance(harmonyMethod,new object[]{observer.GetMethod("CombatClearAfter",BindingFlags.Static|BindingFlags.NonPublic)}),null});
   Console.WriteLine("NATIVE DEATH COMBAT CLEAR CONTRACT PASS=1 FAIL=0; observer constructed without gameplay execution");
   var confusionTick=native.ManifestModule.ResolveMethod(0x06009131);
   if(confusionTick.Name!="TickOnUnit" || confusionTick.GetParameters().Length!=1 ||
      confusionTick.GetParameters()[0].Name!="unit" ||
      confusionTick.GetParameters()[0].ParameterType.FullName!="Kingmaker.EntitySystem.Entities.UnitEntityData" ||
      observer.GetMethod("ConfusionBefore",BindingFlags.Static|BindingFlags.NonPublic)==null ||
      observer.GetMethod("ConfusionAfter",BindingFlags.Static|BindingFlags.NonPublic)==null)
    throw new InvalidOperationException("Native confusion observer contract changed.");
   Console.WriteLine("NATIVE CONFUSION OBSERVER CONTRACT PASS=1 FAIL=0; runtime construction still required");
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
