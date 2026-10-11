param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
Add-Type -TypeDefinition @'
using System;using System.IO;using System.Reflection;
public static class KmcOrderObservationMembers {
 public static void Check(string managed,string dll) {
  ResolveEventHandler resolver=(s,a)=>{var f=Path.Combine(managed,new AssemblyName(a.Name).Name+".dll");if(File.Exists(f))return Assembly.LoadFrom(f);f=Path.Combine(managed,"UnityModManager",new AssemblyName(a.Name).Name+".dll");return File.Exists(f)?Assembly.LoadFrom(f):null;};AppDomain.CurrentDomain.AssemblyResolve+=resolver;
  try {
   var assembly=Assembly.LoadFrom(dll);var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
   var c=assembly.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true);var u=assembly.GetType("KingmakerMountedCombat.Integration.UnifiedMountedTurnCoordinator",true);
   var owner=c.GetField("unifiedTurn",flags);if(owner==null||owner.FieldType!=u)throw new Exception("Observer owner field differs");
   var activation=u.GetField("activation",flags);if(activation==null||!activation.FieldType.IsGenericType||activation.FieldType.GetGenericTypeDefinition().FullName!="KingmakerMountedCombat.Domain.PairedActivation`2")throw new Exception("Observer activation field differs");
   var actor=activation.FieldType.GetMethod("State",flags);if(actor==null)throw new Exception("Actor-state observation absent");
   var capture=actor.ReturnType.GetMethod("Capture",flags);if(capture==null||capture.ReturnType.FullName!="KingmakerMountedCombat.Domain.PairedActorSnapshot")throw new Exception("Read-only actor capture differs");
   foreach(var n in new[]{"Granted","Prepared","ForfeitRecorded","ForfeitSettled"})if(capture.ReturnType.GetProperty(n).PropertyType!=typeof(bool))throw new Exception("Forfeit observation type differs");
   if(capture.ReturnType.GetProperty("ForfeitStandardAdded").PropertyType!=typeof(float))throw new Exception("Forfeit debt observation type differs");
   var condition=u.GetField("nativeConditionForfeitContext",flags);if(condition==null||condition.FieldType.FullName!="TurnBased.Controllers.TurnController")throw new Exception("Condition context observation differs");
   Console.WriteLine("ORDER OBSERVER MEMBER CONTRACT PASS=10 FAIL=0; no native method or resource write invoked");
  } finally {AppDomain.CurrentDomain.AssemblyResolve-=resolver;}
 }
}
'@
[KmcOrderObservationMembers]::Check($managed,(Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
