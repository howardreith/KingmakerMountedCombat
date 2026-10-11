param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repoRoot 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
Add-Type -TypeDefinition @"
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
public static class KmcNativeInputSemantics {
 private static int passed;
 private static void Check(bool value,string detail){if(!value)throw new InvalidOperationException(detail);passed++;Console.WriteLine("PASS "+detail);}
 public static void Run(string modPath,string managed){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
  var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
  Check(native.ManifestModule.ModuleVersionId==new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"),"pinned native input module");
  var field=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true).GetField("CreatedByPlayer");
  Check(field.MetadataToken==0x04001A72 && field.FieldType==typeof(bool),"CreatedByPlayer is the exact native Boolean field");
  var ops=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(x=>unchecked((ushort)x.Value));
  var stores=new System.Collections.Generic.List<int>();var reads=new System.Collections.Generic.List<int>();
  foreach(var t in native.GetTypes())foreach(var m in t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly).Cast<MethodBase>().Concat(t.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly))){
   var body=m.GetMethodBody();if(body==null)continue;var il=body.GetILAsByteArray();
   for(int i=0;i<il.Length;){ushort key=il[i++];if(key==254)key=(ushort)(0xfe00|il[i++]);var op=ops[key];int n;
    switch(op.OperandType){case OperandType.InlineNone:n=0;break;case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:n=1;break;case OperandType.InlineVar:n=2;break;case OperandType.InlineI8:case OperandType.InlineR:n=8;break;case OperandType.InlineSwitch:n=4+4*BitConverter.ToInt32(il,i);break;default:n=4;break;}
    if(op.OperandType==OperandType.InlineField && BitConverter.ToInt32(il,i)==field.MetadataToken){if(op.Name=="stfld")stores.Add(m.MetadataToken);else reads.Add(m.MetadataToken);}
    i+=n;
   }
  }
  Check(stores.OrderBy(x=>x).SequenceEqual(new[]{0x0600378B,0x06005D0C,0x060093DC,0x060093E5,0x060093ED,0x060093ED}),"only native movement/interaction inputs write CreatedByPlayer; selected abilities leave it false");
  Check(reads.SequenceEqual(new[]{0x060027B7}),"CreatedByPlayer is read only by native movement acceleration");
  var mod=Assembly.LoadFrom(modPath);var hostType=mod.GetType("KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost",true);
  var requestType=mod.GetType("KingmakerMountedCombat.Diagnostics.RuntimeRequest",true);
  var host=FormatterServices.GetUninitializedObject(hostType);var request=Activator.CreateInstance(requestType);
  hostType.GetField("request",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(host,request);
  var scenario=requestType.GetProperty("Scenario");var policy=hostType.GetProperty("RequiresLegacyDiagnosticOverlay",BindingFlags.Instance|BindingFlags.NonPublic);
  foreach(var name in new[]{"chunk6a-mammoth-mount-rt","chunk6a-mammoth-mount-tb"}){scenario.SetValue(request,name,null);Check(!(bool)policy.GetValue(host,null),name+" cannot create a legacy overlay before its required preset guard");}
  scenario.SetValue(request,"chunk6a-mount-approach",null);Check((bool)policy.GetValue(host,null),"existing Horse outer-runner overlay policy is unchanged");
  Console.WriteLine("NATIVE INPUT SEMANTICS PASS="+passed+" FAIL=0; pinned IL and compiled policy, not Unity qualification");
 }
}
"@
[KmcNativeInputSemantics]::Run((Join-Path $repoRoot ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')),$managed)
