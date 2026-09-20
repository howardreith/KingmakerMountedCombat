[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$kmcRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$kmcLab=[IO.Path]::GetFullPath((Join-Path $kmcRepo '../..'))
$kmcLayout=(Get-Content -Raw (Join-Path $kmcLab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$kmcManaged=Join-Path $kmcLayout.kingmakerInstallDir 'Kingmaker_Data/Managed'
# Exercise the actual service and exact native field declarations. The action's
# constructor requires Unity, so only that external construction boundary is
# bypassed for this metadata shell. No action, rule or game state is executed.
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
public static class KmcAreaMetadataProbe {
 public static string Capture(string managed, string mod) {
  ResolveEventHandler resolver=(sender,args)=>{
   var leaf=new AssemblyName(args.Name).Name+".dll";
   foreach(var folder in new[]{managed,Path.Combine(managed,"UnityModManager")}) {
    var path=Path.Combine(folder,leaf);
    if(File.Exists(path)) return Assembly.LoadFrom(path);
   }
   return null;
  };
  AppDomain.CurrentDomain.AssemblyResolve+=resolver;
  var stage="assembly loading";
  try {
   var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
   var candidate=Assembly.LoadFrom(mod);
   var actionType=native.GetType("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSavingThrow",true);
   stage="native action metadata shell";
   var action=FormatterServices.GetUninitializedObject(actionType);
   var saveType=actionType.GetField("Type");
   saveType.SetValue(action,Enum.Parse(saveType.FieldType,"Reflex"));
   var listType=native.GetType("Kingmaker.ElementsSystem.ActionList",true);
   stage="native action-list construction";
   var list=Activator.CreateInstance(listType);
   var field=listType.GetField("Actions");
   var actions=Array.CreateInstance(field.FieldType.GetElementType(),1);
   actions.SetValue(action,0); field.SetValue(list,actions);
   var service=candidate.GetType("KingmakerMountedCombat.Diagnostics.Chunk4NativeAreaObservation",true);
   var capture=service.GetMethod("CaptureElement",BindingFlags.Static|BindingFlags.NonPublic);
   stage="actual metadata service";
   return capture.Invoke(null,new object[]{list,0,256}).ToString();
  } catch(Exception error) { throw new InvalidOperationException(stage+": "+error.ToString(),error); }
  finally { AppDomain.CurrentDomain.AssemblyResolve-=resolver; }
 }
}
'@
$kmcJson=[KmcAreaMetadataProbe]::Capture($kmcManaged,(Join-Path $kmcRepo "bin/$Configuration/KingmakerMountedCombat.dll"))
# Windows PowerShell rejects names differing only in case. U reproduced this
# with the service's "type" discriminator and the native action's "Type" field.
$kmcObserved=$kmcJson|ConvertFrom-Json
if($kmcObserved.type -cne 'Kingmaker.ElementsSystem.ActionList' -or
    @($kmcObserved.fields.Actions).Count -ne 1 -or
    $kmcObserved.fields.Actions[0].type -cne 'Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSavingThrow' -or
    $kmcObserved.fields.Actions[0].fields.Type -cne 'Reflex') {
    throw 'Actual area metadata lost its native type or public saving-throw field.'
}
$kmcRoundTrip=$kmcObserved|ConvertTo-Json -Depth 30|ConvertFrom-Json
if($kmcRoundTrip.fields.Actions[0].fields.Type -cne 'Reflex'){throw 'PowerShell artifact round-trip changed native metadata.'}
Write-Host 'CHUNK 4 AREA METADATA COMPONENT PASS=2 FAIL=0; real service and native metadata shell, no gameplay execution'
