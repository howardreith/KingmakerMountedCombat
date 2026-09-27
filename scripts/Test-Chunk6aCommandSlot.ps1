[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$kmcRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$kmcLab=[IO.Path]::GetFullPath((Join-Path $kmcRepo '../..'))
$kmcLayout=(Get-Content -Raw (Join-Path $kmcLab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$kmcManaged=Join-Path $kmcLayout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$nativePath=Join-Path $kmcManaged 'Assembly-CSharp.dll'
if ((Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash.ToLowerInvariant() -cne '3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb') { throw 'Native slot contract requires the pinned Kingmaker assembly.' }
# Exercise only detached slot accessors; no constructors, gameplay or Unity calls.
Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
public static class KmcRelationshipSlotContract {
 static int passed;
 static void Check(bool value,string message) {
  if(!value) throw new InvalidOperationException(message);
  passed++; Console.WriteLine("PASS "+message);
 }
 static IEnumerable<MethodBase> Calls(MethodInfo method) {
  var codes=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static)
   .Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(c=>c.Value);
  var il=method.GetMethodBody().GetILAsByteArray();
  for(int i=0;i<il.Length;) {
   short code=il[i++]; if(code==0xfe) code=(short)(0xfe00|il[i++]);
   var op=codes[code]; int size;
   switch(op.OperandType) {
    case OperandType.InlineNone: size=0; break;
    case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: size=1; break;
    case OperandType.InlineVar: size=2; break;
    case OperandType.InlineI8: case OperandType.InlineR: size=8; break;
    case OperandType.InlineSwitch: size=4+4*BitConverter.ToInt32(il,i); break;
    default: size=4; break;
   }
   if(op.OperandType==OperandType.InlineMethod) yield return method.Module.ResolveMethod(BitConverter.ToInt32(il,i));
   i+=size;
  }
 }
 public static void Run(string managed,string candidatePath) {
  ResolveEventHandler resolver=(sender,args)=>{
   var leaf=new AssemblyName(args.Name).Name+".dll";
   foreach(var folder in new[]{managed,Path.Combine(managed,"UnityModManager")}) {
    var path=Path.Combine(folder,leaf); if(File.Exists(path)) return Assembly.LoadFrom(path);
   }
   return null;
  };
  AppDomain.CurrentDomain.AssemblyResolve+=resolver;
  try {
   var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
   var commands=native.GetType("Kingmaker.UnitLogic.Commands.UnitCommands",true);
   var command=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);
   var ability=native.GetType("Kingmaker.UnitLogic.Commands.UnitUseAbility",true);
   var move=native.GetType("Kingmaker.UnitLogic.Commands.UnitMoveTo",true);
   var getter=commands.GetProperty("Move").GetGetMethod();
   var read=commands.GetMethods().Single(m=>m.MetadataToken==0x060026A9);
   var storage=commands.GetField("m_Commands",BindingFlags.Instance|BindingFlags.NonPublic);
   Check(getter.MetadataToken==0x0600269F&&getter.ReturnType==move,"pinned Move getter returns UnitMoveTo only");
   Check(read.Name=="GetCommand"&&read.ReturnType==command&&storage.MetadataToken==0x04001A46,"pinned GetCommand reads the base UnitCommand slot");
   var kind=Enum.Parse(command.GetNestedType("CommandType"),"Move");
   var slots=Array.CreateInstance(command,16);
   var owner=FormatterServices.GetUninitializedObject(commands); storage.SetValue(owner,slots);
   var shell=FormatterServices.GetUninitializedObject(ability); slots.SetValue(shell,Convert.ToInt32(kind));
   Check(ReferenceEquals(read.Invoke(owner,new[]{kind}),shell),"actual GetCommand(Move) returns the exact UnitUseAbility shell");
   Check(getter.Invoke(owner,null)==null,"actual Move convenience getter cannot observe that UnitUseAbility");
   var ground=FormatterServices.GetUninitializedObject(move); slots.SetValue(ground,Convert.ToInt32(kind));
   Check(ReferenceEquals(getter.Invoke(owner,null),ground),"actual Move convenience getter observes an ordinary UnitMoveTo");
   var candidate=Assembly.LoadFrom(candidatePath);
   var tick=candidate.GetType("KingmakerMountedCombat.Diagnostics.Phase3dHorseScenarioTranche",true)
    .GetMethod("TickChunk6aGeometryChange",BindingFlags.Instance|BindingFlags.NonPublic);
   var calls=Calls(tick).Where(m=>m.DeclaringType==commands).ToArray();
   Check(calls.Any(m=>m.MetadataToken==0x060026A9)&&!calls.Any(m=>m.MetadataToken==0x0600269F),
    "compiled geometry observer must use GetCommand(Move), never the UnitMoveTo-only getter");
   Console.WriteLine("CHUNK6A NATIVE SLOT COMPONENT PASS="+passed+" FAIL=0; detached accessors and compiled call contract, no Unity execution");
  } finally { AppDomain.CurrentDomain.AssemblyResolve-=resolver; }
 }
}
"@
[KmcRelationshipSlotContract]::Run($kmcManaged,(Join-Path $kmcRepo "bin/$Configuration/KingmakerMountedCombat.dll"))
