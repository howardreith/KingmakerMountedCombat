param([ValidateSet('Debug','Release')][string]$Configuration='Release',[string]$AssemblyPath,[string]$RepoRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepoRoot){$RepoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))}
$paths=[xml](Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
if(-not$AssemblyPath){$AssemblyPath=Join-Path $RepoRoot ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')}
Add-Type -TypeDefinition @'
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Reflection.Emit;using System.Security.Cryptography;
public static class KmcMidEncounterPreparationRegression {
 static int count;static BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);count++;}
 static List<MethodBase> Calls(MethodBase method){
  var ops=typeof(OpCodes).GetFields(BindingFlags.Static|BindingFlags.Public).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(o=>unchecked((ushort)o.Value));
  var il=method.GetMethodBody().GetILAsByteArray();var result=new List<MethodBase>();
  for(int i=0;i<il.Length;){ushort key=il[i++];if(key==254)key=(ushort)(0xfe00|il[i++]);var op=ops[key];int n;
   switch(op.OperandType){case OperandType.InlineNone:n=0;break;case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:n=1;break;case OperandType.InlineVar:n=2;break;case OperandType.InlineI8:case OperandType.InlineR:n=8;break;case OperandType.InlineSwitch:n=4+4*BitConverter.ToInt32(il,i);break;default:n=4;break;}
   if(op.OperandType==OperandType.InlineMethod)result.Add(method.Module.ResolveMethod(BitConverter.ToInt32(il,i),method.DeclaringType.GetGenericArguments(),method.IsGenericMethod?method.GetGenericArguments():null));i+=n;
  }return result;
 }
 public static void Run(string managed,string dll){
  ResolveEventHandler resolver=(s,e)=>{var name=new AssemblyName(e.Name).Name+".dll";var p=Path.Combine(managed,name);if(!File.Exists(p))p=Path.Combine(managed,"UnityModManager",name);return File.Exists(p)?Assembly.LoadFrom(p):null;};AppDomain.CurrentDomain.AssemblyResolve+=resolver;
  try{
   var assembly=Assembly.LoadFrom(dll);var u=assembly.GetType("KingmakerMountedCombat.Integration.UnifiedMountedTurnCoordinator",true);
   var calls=Calls(u.GetMethod("AdoptRunningEncounter",flags));var reservations=calls.Count(m=>m.Name=="BeginActorPreparation");
   Check(calls.Count(m=>m.Name=="Prepare"&&m.DeclaringType.FullName=="TurnBased.Controllers.TurnController")==1,"Adoption must invoke exactly one partner native Prepare");
   Check(Calls(u.GetMethod("BeginPairedPreparation",flags)).Count(m=>m.Name=="BeginActorPreparation")==2,"Both native preparation branches must reserve their own grants");
   var t=assembly.GetType("KingmakerMountedCombat.Domain.PairedActivation"+(char)96+"2",true).MakeGenericType(typeof(object),typeof(object));var disposition=assembly.GetType("KingmakerMountedCombat.Domain.MidEncounterAdoption",true);
   var rider=new object();var mount=new object();var boundary=new object();var pair=Activator.CreateInstance(t,new[]{rider,mount});
   var adopt=t.GetMethod("AdoptRunningBoundary");var begin=t.GetMethod("BeginActorPreparation");var finish=t.GetMethod("FinishActorPreparation");var state=t.GetMethod("State");
   Check((bool)adopt.Invoke(pair,new[]{boundary,Enum.Parse(disposition,"PreparePartnerThisRound")}),"Fresh boundary adoption failed");
   var riderState=state.Invoke(pair,new[]{rider});var mountState=state.Invoke(pair,new[]{mount});var actorType=riderState.GetType();
   Check((bool)actorType.GetProperty("Prepared").GetValue(riderState,null),"Rider's existing preparation was lost");
   Check(!(bool)actorType.GetProperty("Granted").GetValue(mountState,null),"Adoption prematurely granted partner");
   // Replay actual compiled caller reservations, followed by its existing native prefix.
   for(int i=0;i<reservations;i++)Check((bool)begin.Invoke(pair,new[]{mount,boundary}),"Caller reservation unexpectedly rejected");
   Check((bool)begin.Invoke(pair,new[]{mount,boundary}),"Native Prepare prefix rejected partner: adoption caller already reserved the same grant (observed caller reservations="+reservations+")");
   finish.Invoke(pair,new[]{mount});Check((bool)actorType.GetProperty("Prepared").GetValue(mountState,null),"Native completion did not prepare partner");
   Check(!(bool)begin.Invoke(pair,new[]{mount,boundary}),"Second native preparation was admitted");
   Check(!(bool)begin.Invoke(pair,new[]{rider,boundary}),"Adoption re-prepared rider");
   Check(!(bool)begin.Invoke(pair,new[]{new object(),boundary}),"Foreign actor reserved partner grant");
   Check(reservations==0,"Adoption caller must leave reservation to the existing native prefix");
   var retained=Activator.CreateInstance(t,new[]{rider,mount});Check((bool)adopt.Invoke(retained,new[]{boundary,Enum.Parse(disposition,"RetainPartnerParticipation")}),"Retained adoption failed");
   Check(!(bool)begin.Invoke(retained,new[]{mount,boundary}),"Retained spent participation was refreshed");
   var game=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));Check(game.ManifestModule.ModuleVersionId.ToString()=="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7","Pinned engine differs");
   var prepare=game.ManifestModule.ResolveMethod(0x06000C3C);var hash=BitConverter.ToString(SHA256.Create().ComputeHash(prepare.GetMethodBody().GetILAsByteArray())).Replace("-","").ToLowerInvariant();
   Check(hash=="27c769eb310f4bccfa9db38e06a921260696b4d95c382586bea04e2f091af408","Pinned native preparation body differs");
   Check(Calls(prepare).Count(m=>m.Name=="Clear"&&m.DeclaringType.FullName=="Kingmaker.Controllers.Combat.UnitCombatState+Cooldowns")==1,"Native preparation lacks its exact single cooldown Clear");
   Console.WriteLine("MID-ENCOUNTER NATIVE PREPARATION PASS="+count+" FAIL=0; compiled caller/domain/pinned native contract, not Unity qualification");
  }finally{AppDomain.CurrentDomain.AssemblyResolve-=resolver;}
 }
}
'@
[KmcMidEncounterPreparationRegression]::Run($managed,$AssemblyPath)
