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
   var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
   var service=candidate.GetType("KingmakerMountedCombat.Integration.NativeMountedControlService",true);
   var actorType=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
   var actor=FormatterServices.GetUninitializedObject(actorType);
   var foreignActor=FormatterServices.GetUninitializedObject(actorType);
   var instance=FormatterServices.GetUninitializedObject(service);
   var shellType=service.GetNestedType("NativeRelationshipShell",BindingFlags.NonPublic);
   var tableField=service.GetField("relationshipShells",flags);
   var table=Activator.CreateInstance(tableField.FieldType); tableField.SetValue(instance,table);
   var owned=FormatterServices.GetUninitializedObject(ability);
   command.GetField("<Type>k__BackingField",flags).SetValue(owned,kind);
   command.GetField("<Executor>k__BackingField",flags).SetValue(owned,actor);
   var ownership=FormatterServices.GetUninitializedObject(shellType);
   var kindField=shellType.GetField("Kind",flags);
   kindField.SetValue(ownership,Enum.Parse(kindField.FieldType,"MountCompanion"));
   tableField.FieldType.GetMethod("Add").Invoke(table,new[]{owned,ownership});
   var resolve=service.GetMethod("ResolveUnactedNativeMount",flags);
   Check(ReferenceEquals(resolve.Invoke(instance,new[]{actor,owned}),owned),"actual interruption resolver owns the exact registered unacted Move Mount");
   Check(resolve.Invoke(instance,new[]{foreignActor,owned})==null,"foreign actor cannot interrupt the owned Mount");
   Check(resolve.Invoke(instance,new object[]{actor,null})==null,"missing current Move command passes through");
   Check(resolve.Invoke(instance,new[]{actor,ground})==null,"ordinary native ground command passes through");
   var unrelated=FormatterServices.GetUninitializedObject(ability);
   command.GetField("<Type>k__BackingField",flags).SetValue(unrelated,kind);
   command.GetField("<Executor>k__BackingField",flags).SetValue(unrelated,actor);
   Check(resolve.Invoke(instance,new[]{actor,unrelated})==null,"unregistered ability in the same actor Move slot passes through");
   foreach(var fieldName in new[]{"IsStarted","IsActed","IsFinished"}) {
    var field=command.GetField("<"+fieldName+">k__BackingField",flags);field.SetValue(owned,true);
    Check(resolve.Invoke(instance,new[]{actor,owned})==null,"Mount with "+fieldName+" is outside unacted approach cancellation");
    field.SetValue(owned,false);
   }
   foreach(var fieldName in new[]{"ProcessBound","Consumed","Retired"}) {
    var field=shellType.GetField(fieldName,flags);field.SetValue(ownership,true);
    Check(resolve.Invoke(instance,new[]{actor,owned})==null,"shell with "+fieldName+" cannot be interrupted twice or after commitment");
    field.SetValue(ownership,false);
   }
   kindField.SetValue(ownership,Enum.Parse(kindField.FieldType,"Dismount"));
   Check(resolve.Invoke(instance,new[]{actor,owned})==null,"registered Dismount passes through approach failure handler");
   kindField.SetValue(ownership,Enum.Parse(kindField.FieldType,"MountCompanion"));
   command.GetField("<Type>k__BackingField",flags).SetValue(owned,Enum.Parse(kind.GetType(),"Standard"));
   Check(resolve.Invoke(instance,new[]{actor,owned})==null,"non-Move owned command passes through");
   command.GetField("<Type>k__BackingField",flags).SetValue(owned,kind);
   var execution=ability.GetProperty("ExecutionProcess",flags);
   var executionField=ability.GetField("<ExecutionProcess>k__BackingField",flags);
   executionField.SetValue(owned,FormatterServices.GetUninitializedObject(execution.PropertyType));
   Check(resolve.Invoke(instance,new[]{actor,owned})==null,"a native execution process excludes approach cancellation even without acted evidence");
   executionField.SetValue(owned,null);
   Check(ReferenceEquals(resolve.Invoke(instance,new[]{actor,owned}),owned),"negative-control probes left the original detached command and shell intact");
   var nativeInterruption=(MethodInfo)native.ManifestModule.ResolveMethod(0x0600184F);
   Check(Calls(nativeInterruption).Any(m=>m.MetadataToken==getter.MetadataToken),"pinned interruption handler uses the UnitMoveTo-only accessor that missed preview119 Mount");
   var captureInterruption=service.GetMethod("CaptureNativeMountApproachInterruption",flags);
   var interruptionCalls=Calls(captureInterruption).Where(m=>m.DeclaringType==commands).ToArray();
   Check(interruptionCalls.Any(m=>m.MetadataToken==read.MetadataToken)&&!interruptionCalls.Any(m=>m.MetadataToken==getter.MetadataToken),
    "compiled repair captures the actual base Move slot without the native subtype filter");
   var completion=service.GetMethod("CompleteNativeMountApproachInterruption",flags);
   Check(Calls(completion).Any(m=>m.DeclaringType==command&&m.MetadataToken==0x060027AC)&&
    !Calls(completion).Any(m=>m.Name=="ForceFinishForTurnBased"||m.Name=="UpdateCooldowns"||m.Name=="set_IsActed"),
    "compiled repair delegates terminal state to native Interrupt without cost or acted writes");
   Console.WriteLine("CHUNK6A NATIVE SLOT COMPONENT PASS="+passed+" FAIL=0; detached accessors and compiled call contract, no Unity execution");
  } finally { AppDomain.CurrentDomain.AssemblyResolve-=resolver; }
 }
}
"@
[KmcRelationshipSlotContract]::Run($kmcManaged,(Join-Path $kmcRepo "bin/$Configuration/KingmakerMountedCombat.dll"))
