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
public static class KmcAutoUseWrapperProbe {
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
  string id="KingmakerMountedCombat.Diagnostics.DesktopWrappers.AutoUseControl";
  var h=ht.GetMethod("Create").Invoke(null,new object[]{id});
  var hookType=candidate.GetType("KingmakerMountedCombat.Diagnostics.NativeAutoUseControlProbe+Hooks",true);
  int[] tokens=new[]{0x06002B30,0x06002F5F,0x06008370,0x06009434,0x06002726,0x06002727};
  string[] signatures=new[]{
   "Boolean get_IsSuitableForAutoUse()","Void OnAutoUseToggle()",
   "Kingmaker.UnitLogic.Abilities.AbilityData GetAvailableAutoUseAbility()",
   "Kingmaker.UnitLogic.Commands.Base.UnitCommand CreateCommand(Kingmaker.Controllers.Brain.DecisionContext, Kingmaker.EntitySystem.Entities.UnitEntityData)",
   "Void .ctor(Kingmaker.UnitLogic.Abilities.AbilityData, Kingmaker.Utility.TargetWrapper)",
   "Void .ctor(CommandType, Kingmaker.UnitLogic.Abilities.AbilityData, Kingmaker.Utility.TargetWrapper)"};
  string[] hashes=new[]{
   "7223c69aea7ac0e8ced549bd112c94dccdfbeed535b268327487662c12fb6c27",
   "b3494e0e8e529940a5a974614ced945f86e08de94501ec56c431b4ca422aea08",
   "3c4c313aa490d44c75933c20e4497793716e203396f4f702c4bc075ec523b186",
   "664b3893e901b9dc458866835f7670fc73503b69b7e12b230de58c9c8ab45eaa",
   "7009945faa37ed958d97c02ca46609f94c3eb2470f9aaf1cffc16d919b788878",
   "4c538c0c576702e9a445f2595b5547ea529e5688e97b4010e417e13f23e24842"};
  string[] prefixes=new string[]{"SuitableBefore","ToggleBefore","AvailableBefore","AiBefore",null,null};
  string[] postfixes=new[]{"SuitableAfter","ToggleAfter","AvailableAfter","AiAfter","Constructed","Constructed"};
  var flags=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
  try {
   for(int i=0;i<tokens.Length;i++) {
    var original=native.ManifestModule.ResolveMethod(tokens[i]);
    using(var sha=System.Security.Cryptography.SHA256.Create()) {
     var hash=BitConverter.ToString(sha.ComputeHash(original.GetMethodBody().GetILAsByteArray())).Replace("-","").ToLowerInvariant();
     if(original.ToString()!=signatures[i]||hash!=hashes[i])throw new InvalidOperationException("Pinned auto-use signature/IL changed: "+tokens[i].ToString("X8"));
    }
    Console.WriteLine("PASS pinned native auto-use signature and IL "+tokens[i].ToString("X8"));
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
[KmcAutoUseWrapperProbe]::Run($managed,$dll,$false)