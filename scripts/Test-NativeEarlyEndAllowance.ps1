$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw -LiteralPath (Join-Path $repo 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
Add-Type -TypeDefinition @"
using System;using System.IO;using System.Reflection;using System.Security.Cryptography;
public static class KmcEarlyEndAllowanceContract {
 public static void Run(string managed) {
  ResolveEventHandler resolver=(s,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  AppDomain.CurrentDomain.AssemblyResolve+=resolver;
  try {
   var a=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
   if(a.ManifestModule.ModuleVersionId!=new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))throw new Exception("Native allowance MVID differs");
   int[] tokens={0x06008380,0x06000C47};
   string[] owners={"Kingmaker.EntitySystem.Entities.UnitEntityData","TurnBased.Controllers.TurnController"};
   string[] names={"HasSwiftAction","ForceToEnd"};
   string[] hashes={"65061d8c8cccff181626338782db42cc3504357af1bc2c3121fb9dc4a49a09a8","47617319925972a71b3fe52ac1c4cd2e2e412641758f1ef319a5e2d2f0df6a1f"};
   for(int i=0;i<tokens.Length;i++) {
    var m=(MethodInfo)a.ManifestModule.ResolveMethod(tokens[i]);var p=m.GetParameters();
    if(m.IsStatic||!m.IsPublic||m.DeclaringType.FullName!=owners[i]||m.Name!=names[i]||
       m.ReturnType!=(i==0?typeof(bool):typeof(void))||p.Length!=i||(i==1&&p[0].ParameterType!=typeof(bool)))
       throw new Exception("Exact native allowance/completion signature differs");
    using(var sha=SHA256.Create()) {
     var hash=BitConverter.ToString(sha.ComputeHash(m.GetMethodBody().GetILAsByteArray())).Replace("-","").ToLowerInvariant();
     if(hash!=hashes[i])throw new Exception("Native allowance/completion IL differs: "+names[i]);
    }
    Console.WriteLine("PASS exact native early End contract: "+names[i]+" token="+tokens[i].ToString("X8"));
   }
   Console.WriteLine("EARLY END ASSEMBLY PASS=2 FAIL=0; read-only, no native methods invoked");
  } finally {AppDomain.CurrentDomain.AssemblyResolve-=resolver;}
 }
}
"@
[KmcEarlyEndAllowanceContract]::Run($managed)
