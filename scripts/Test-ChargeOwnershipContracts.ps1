[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$dll=Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"
if((Get-FileHash (Join-Path $managed 'Assembly-CSharp.dll')).Hash.ToLowerInvariant() -cne '3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb'){throw 'Installed native identity changed.'}
# Detached production-adapter probes. Native containers and commands are real types;
# only the inaccessible engine/logging boundary is substituted. No game is launched.
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
public static class ChargeOwnershipProbe {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static int passed;
 static Action observeCreation;
 static object acquiredFact;
 static T ObserveThenReturn<T>() where T:class {observeCreation();return (T)acquiredFact;}
 static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);passed++;Console.WriteLine("PASS "+text);}
 static object Blank(Type t){return FormatterServices.GetUninitializedObject(t);}
 static FieldInfo Field(Type t,string name){for(;t!=null;t=t.BaseType){var f=t.GetField(name,F);if(f!=null)return f;}throw new MissingFieldException(name);}
 static void Set(object o,string n,object v){Field(o.GetType(),n).SetValue(o,v);}
 static object Get(object o,string n){return Field(o.GetType(),n).GetValue(o);}
 static object Call(object o,string n,params object[] args){return o.GetType().GetMethod(n,F).Invoke(o,args);}
 sealed class LoggerProxy:RealProxy {
  internal LoggerProxy(Type t):base(t){}
  public override IMessage Invoke(IMessage m){return new ReturnMessage(null,null,0,null,(IMethodCallMessage)m);}
 }
 public static void Run(string managed,string dll){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{
   var leaf=new AssemblyName(a.Name).Name+".dll";
   foreach(var root in new[]{managed,Path.Combine(managed,"UnityModManager")}){var path=Path.Combine(root,leaf);if(File.Exists(path))return Assembly.LoadFrom(path);}return null;
  };
  var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));var mod=Assembly.LoadFrom(dll);
  Check(native.ManifestModule.ModuleVersionId.ToString()=="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7","pinned native MVID");
  var commandType=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);
  var commandsType=native.GetType("Kingmaker.UnitLogic.Commands.UnitCommands",true);
  var nativeRemove=commandsType.GetMethods(F).Single(m=>m.Name=="InterruptAll"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType.IsGenericType);
  Check(nativeRemove.MetadataToken==0x060026C0,"exact native predicate removal token");
  Func<object> container=()=>{var c=Blank(commandsType);Set(c,"m_Commands",Array.CreateInstance(commandType,4));Set(c,"m_Queue",Activator.CreateInstance(Field(commandsType,"m_Queue").FieldType));return c;};
  var riderCommands=container();var mountCommands=container();var foreignCommands=container();
  var pairType=mod.GetType("KingmakerMountedCombat.Integration.MountedPairAttackCommand",true);
  var command=Blank(pairType);Set(command,"chargeMode",true);
  Set(command,"chargeCarriers",Activator.CreateInstance(Field(pairType,"chargeCarriers").FieldType));
  Set(command,"transaction",Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Domain.MountedCombatTransaction",true)));
  var result=commandType.GetProperty("Result",F).PropertyType;
  Set(command,"<Result>k__BackingField",Enum.Parse(result,"Interrupt"));
  // Reproduce native Interrupt's partial terminal trap: result already Interrupt,
  // terminal false. Do not call ForceFinish or fabricate a committed action.
  Set(command,"<IsFinished>k__BackingField",false);
  var foreign=Blank(native.GetType("Kingmaker.UnitLogic.Commands.UnitMoveTo",true));
  var raw=(Array)Get(riderCommands,"m_Commands");raw.SetValue(command,1);raw.SetValue(foreign,3);
  var queue=Get(riderCommands,"m_Queue");var add=queue.GetType().GetMethod("AddLast",new[]{commandType});
  add.Invoke(queue,new[]{command});add.Invoke(queue,new[]{foreign});
  ((Array)Get(foreignCommands,"m_Commands")).SetValue(foreign,1);
  var controller=Blank(mod.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true));
  Set(controller,"logger",new LoggerProxy(mod.GetType("KingmakerMountedCombat.Logging.IModLogger",true)).GetTransparentProxy());
  Set(controller,"pairedCommandScheduler",Blank(mod.GetType("KingmakerMountedCombat.Integration.MountedPairCommandScheduler",true)));
  var shell=Blank(native.GetType("Kingmaker.UnitLogic.Commands.UnitUseAbility",true));
  Set(shell,"<IsFinished>k__BackingField",true);
  var owner=Call(controller,"CreateChargeOwner",null,null,null,riderCommands,mountCommands,shell,17L,0);
  Set(owner,"Command",command);Set(controller,"chargeOwner",owner);Set(controller,"activeCommand",command);
  Check(!(bool)Call(controller,"AdmitChargeCommand",riderCommands,foreign)&&Object.ReferenceEquals(Get(controller,"chargeOwner"),owner),"replacement admission drains first and refuses while native terminal remains unresolved");
  Check(!(bool)Call(controller,"TryDrainChargeOwnership","interrupt callback failed"),"partial native terminal cannot drain even after exact dequeue");
  Check(Object.ReferenceEquals(Get(controller,"chargeOwner"),owner)&&Object.ReferenceEquals(Get(controller,"activeCommand"),command),"controller retains exact owner and command after failed postcondition");
  Check(raw.GetValue(1)==null&&Object.ReferenceEquals(raw.GetValue(3),foreign)&&
   (int)queue.GetType().GetProperty("Count").GetValue(queue,null)==1,"exact native removal leaves unrelated slot and queue intact");
  Check(!(bool)commandType.GetProperty("IsActed").GetValue(command,null)&&!(bool)commandType.GetProperty("IsStarted").GetValue(foreign,null),"cleanup neither commits the attack nor starts foreign queued work");
  Check(!(bool)Call(controller,"AdmitChargeCommand",riderCommands,foreign),"retained debt blocks new exact-rider commands");
  Check((bool)Call(controller,"AdmitChargeCommand",foreignCommands,foreign),"retained debt leaves unrelated actor admission alone");
  // Native terminal completion is an external observation in this detached probe.
  Set(command,"<IsFinished>k__BackingField",true);
  Check((bool)Call(controller,"TryDrainChargeOwnership","native terminal observed"),"second observed-terminal attempt drains retained original container: "+Call(controller,"get_ChargeCleanupStatus"));
  Check(Get(controller,"chargeOwner")==null&&Get(controller,"activeCommand")==null,"only a fully drained owner is released");
  Check((bool)Call(controller,"AdmitChargeCommand",riderCommands,foreign),"ordinary action admission resumes after debt drains");
  var inAction=Call(controller,"CreateChargeOwner",null,null,null,riderCommands,mountCommands,shell,17L,0);
  Set(controller,"chargeOwner",inAction);
  Call(controller,"BeginChargeNativeAction",shell);
  Check(!(bool)Call(controller,"TryDrainChargeOwnership","reentrant save before process assignment")&&Object.ReferenceEquals(Get(controller,"chargeOwner"),inAction),"native action registration window retains owner before shell process assignment");
  Call(controller,"CompleteChargeNativeAction",shell);
  Check((bool)Call(controller,"TryDrainChargeOwnership","native action returned"),"normal action return permits observed process settlement without advancing it");
  var first=new object();var second=new object();
  Check((bool)Call(controller,"AcquireChargeSaveFence",first),"settled save acquires exact operation fence");
  Call(controller,"ReleaseChargeSaveFence",second);
  Check(Object.ReferenceEquals(Get(controller,"chargeAdmissionFence"),first),"foreign completion cannot release save fence");
  var overlap=false;try{Call(controller,"AcquireChargeSaveFence",second);}catch(TargetInvocationException e){overlap=e.InnerException is InvalidOperationException;}
  Check(overlap&&Object.ReferenceEquals(Get(controller,"chargeAdmissionFence"),first),"overlapping save cannot steal operation fence");
  var persistence=Blank(mod.GetType("KingmakerMountedCombat.Integration.MountedPersistenceService",true));Set(persistence,"combat",controller);
  Check(Call(persistence,"DrainForTeardown",0).ToString()=="Refused","selected pre-enumeration save refuses teardown before activeSave exists");
  Call(controller,"ReleaseChargeSaveFence",first);
  Check(Call(persistence,"DrainForTeardown",0).ToString()=="Clear","released pre-enumeration fence permits ordinary teardown");
  var faults=mod.GetType("KingmakerMountedCombat.Integration.MountedChargeAdmissionFault",true);
  var faultHook=faults.GetField("BeforeCleanupStep",F);
  foreach(var boundary in new[]{"rider-remove","abandon-scheduler","shell-remove"}){
   raw.SetValue(command,1);add.Invoke(queue,new[]{command});
   var retained=Call(controller,"CreateChargeOwner",null,null,null,riderCommands,mountCommands,shell,17L,0);
   Set(retained,"Command",command);Set(controller,"chargeOwner",retained);Set(controller,"activeCommand",command);
   faultHook.SetValue(null,new Action<string>(step=>{if(step==boundary)throw new InvalidOperationException("injected "+step);}));
   try{
    Check(!(bool)Call(controller,"TryDrainChargeOwnership","injected "+boundary)&&Object.ReferenceEquals(Get(controller,"chargeOwner"),retained),"actual adapter retains owner when "+boundary+" throws");
    Check(!(bool)Call(controller,"AcquireChargeSaveFence",first),"actual pending cleanup defers save at "+boundary);
   }finally{faultHook.SetValue(null,null);}
   Check((bool)Call(controller,"AcquireChargeSaveFence",first)&&Get(controller,"chargeOwner")==null,"actual second attempt drains "+boundary+" before save readiness");
   Call(controller,"ReleaseChargeSaveFence",first);
   Check(Object.ReferenceEquals(raw.GetValue(3),foreign)&&!(bool)commandType.GetProperty("IsActed").GetValue(command,null),"fault retry preserves foreign command and commitment at "+boundary);
  }
  var leaseType=mod.GetType("KingmakerMountedCombat.Integration.MountedChargeLease",true);
  var restoreCounter=leaseType.GetMethod("TryRestoreChargingCounter",F);
  var agentType=native.GetType("Kingmaker.View.UnitMovementAgent",true);
  var counter=agentType.GetField("m_ChargingCounter",F);
  Check(counter.MetadataToken==0x040011B2,"exact charging counter observation token");
  var agent=Blank(agentType);
  counter.SetValue(agent,2);
  var releaseArgs=new object[]{agent,1,false};
  Check((bool)restoreCounter.Invoke(null,releaseArgs)&&(int)counter.GetValue(agent)==1&&(bool)releaseArgs[2],"restore balances KMC increment while preserving prior native charging");
  Check((bool)restoreCounter.Invoke(null,releaseArgs)&&(int)counter.GetValue(agent)==1,"charging retry does not decrement an already restored counter");
  Check(!(bool)restoreCounter.Invoke(null,new object[]{agent,1,false})&&(int)counter.GetValue(agent)==1,"external decrement before KMC release retains ambiguous debt");
  counter.SetValue(agent,3);
  Check(!(bool)restoreCounter.Invoke(null,new object[]{agent,1,false})&&(int)counter.GetValue(agent)==3,"unattributable counter change is retained without overwriting it");
  var unitType=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
  var lease=Activator.CreateInstance(leaseType,F,null,new[]{Blank(unitType),Blank(unitType),Blank(unitType),Get(controller,"logger")},null);
  counter.SetValue(agent,1);Set(lease,"appliedMountAgent",agent);Set(lease,"chargingBefore",true);Set(lease,"chargingCounterBefore",1);
  var cleanupStep=mod.GetType("KingmakerMountedCombat.Domain.MountedChargeCleanupStep",true);
  var laterOwned=true;
  var lateStep=Activator.CreateInstance(cleanupStep,new object[]{"late native callback",new Func<bool>(()=>laterOwned),new Func<bool>(()=>{
   agentType.GetProperty("MaxSpeedOverride",F).SetValue(agent,(float?)7f,null);laterOwned=false;return true;})});
  var steps=Array.CreateInstance(cleanupStep,1);steps.SetValue(lateStep,0);
  var ledger=Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Domain.MountedChargeCleanupLedger",true),new object[]{steps});
  Set(lease,"cleanup",ledger);Call(ledger,"Attempt");
  Check((bool)Call(ledger,"get_Complete")&&!(bool)Call(lease,"get_RollbackComplete")&&(bool)Call(lease,"get_HasCleanupDebt"),"later native callback invalidating an earlier restore retains lease debt despite completed undo steps: "+Call(ledger,"Describe")+";nativeSpeed="+agentType.GetProperty("MaxSpeedOverride",F).GetValue(agent,null)+";rollback="+Call(lease,"get_RollbackComplete")+";debt="+Call(lease,"get_HasCleanupDebt"));
  agentType.GetProperty("MaxSpeedOverride",F).SetValue(agent,null,null);
  Check((bool)Call(lease,"get_RollbackComplete")&&!(bool)Call(lease,"get_HasCleanupDebt"),"final observed native postconditions drain only after residue is gone");
  var buffType=native.GetType("Kingmaker.UnitLogic.Buffs.Buff",true);
  var buffCollectionType=native.GetType("Kingmaker.UnitLogic.Buffs.BuffCollection",true);
  var contextType=native.GetType("Kingmaker.UnitLogic.Mechanics.MechanicsContext",true);
  var buff=Blank(buffType);var wrongBuff=Blank(buffType);var originalBuffs=Blank(buffCollectionType);var foreignBuffs=Blank(buffCollectionType);
  var parent=Blank(contextType);var child=Blank(contextType);var otherChild=Blank(contextType);
  native.ManifestModule.ResolveField(0x04001707).SetValue(child,parent);
  native.ManifestModule.ResolveField(0x04001B6F).SetValue(buff,child);
  native.ManifestModule.ResolveField(0x04001B6F).SetValue(wrongBuff,otherChild);
  Set(lease,"appliedBuffCollection",originalBuffs);Set(lease,"buffAcquisitionContext",parent);
  var factOwner=Get(lease,"buffOwnership");var observer=leaseType.GetMethod("ObserveChargeBuffCreated",F);
  var activeAcquisition=leaseType.GetField("acquiringBuffLease",F);var previousAcquisition=activeAcquisition.GetValue(null);
  acquiredFact=buff;
  observeCreation=()=>{
   observer.Invoke(null,new[]{foreignBuffs,buff});observer.Invoke(null,new[]{originalBuffs,wrongBuff});
   Check(Call(factOwner,"get_Fact")==null,"compiled buff observer ignores foreign collection and acquisition context");
   observer.Invoke(null,new[]{originalBuffs,buff});
   Check(Object.ReferenceEquals(Call(factOwner,"get_Fact"),buff)&&!(bool)buffType.GetProperty("Active",F).GetValue(buff,null),"compiled buff observer captures exact fact before native activation");
  };
  try{
   activeAcquisition.SetValue(null,lease);
   var callbackType=typeof(Func<>).MakeGenericType(buffType);
   var callback=Delegate.CreateDelegate(callbackType,typeof(ChargeOwnershipProbe).GetMethod("ObserveThenReturn",F).MakeGenericMethod(buffType));
   Call(factOwner,"Acquire",callback);
  }finally{activeAcquisition.SetValue(null,previousAcquisition);observeCreation=null;acquiredFact=null;}
  Check((bool)Call(factOwner,"get_Acquired"),"exact observed native buff return completes acquisition");
  var nativeFacts=native.ManifestModule.ResolveField(0x04006075);nativeFacts.SetValue(originalBuffs,Activator.CreateInstance(nativeFacts.FieldType));
  native.ManifestModule.ResolveField(0x04006961).SetValue(buff,true);
  var component=Blank(native.GetType("Kingmaker.UnitLogic.FactLogic.AddStatBonus",true));
  ((System.Collections.IList)Get(lease,"buffComponents")).Add(component);
  var listening=native.ManifestModule.ResolveField(0x0400607C);listening.SetValue(component,true);
  Check(!(bool)Call(lease,"BuffCleanupPostcondition",buff),"removed disposed buff with retained native event listener remains cleanup debt");
  listening.SetValue(component,false);
  var statType=native.GetType("Kingmaker.EntitySystem.Stats.ModifiableValue",true);var stat=Blank(statType);
  var modifiersField=native.ManifestModule.ResolveField(0x0400536A);modifiersField.SetValue(stat,Activator.CreateInstance(modifiersField.FieldType));
  var stats=Array.CreateInstance(statType,1);stats.SetValue(stat,0);Set(lease,"buffStats",stats);
  var modifier=Blank(native.GetType("Kingmaker.EntitySystem.Stats.ModifiableValue+Modifier",true));
  ((System.Collections.IList)Get(lease,"buffModifiers")).Add(modifier);
  var appliedTo=native.ManifestModule.ResolveField(0x04008C66);appliedTo.SetValue(modifier,stat);
  Check(!(bool)Call(lease,"BuffCleanupPostcondition",buff),"removed disposed buff with modifier AppliedTo retained remains cleanup debt");
  appliedTo.SetValue(modifier,null);
  Check((bool)Call(lease,"BuffCleanupPostcondition",buff),"exact native listeners lists and modifiers all drained establishes buff residue postcondition");
  var boundaryType=mod.GetType("KingmakerMountedCombat.Domain.MountedChargeBoundaryQueue",true);
  var kindType=mod.GetType("KingmakerMountedCombat.Domain.MountedChargeBoundaryKind",true);
  var boundaries=Activator.CreateInstance(boundaryType,true);Set(controller,"chargeBoundaries",boundaries);
  var writing=true;var retired=0;
  controller.GetType().GetProperty("ChargeArchiveWorkerRunning",F).SetValue(controller,new Func<bool>(()=>writing),null);
  var actorType=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
  var destroyedOne=Blank(actorType);var destroyedTwo=Blank(actorType);
  Call(controller,"RetainDestroyedActorBoundary",destroyedOne,new Action(()=>retired+=1));
  Call(controller,"RetainDestroyedActorBoundary",destroyedTwo,new Action(()=>retired+=10));
  Call(controller,"RetainDestroyedActorBoundary",destroyedOne,new Action(()=>retired+=100));
  Call(controller,"ResumeChargeNativeBoundaries");
  Check(retired==0&&(bool)Call(controller,"get_ChargeNativeBoundaryPending"),"actual archive worker retains distinct actor retirement continuations");
  Check(!(bool)Call(controller,"AcquireChargeSaveFence",first),"new snapshot cannot cross retained native notifications");
  Check((bool)Call(controller,"ContinueChargeSaveFence",first),"same captured save can finish with post-save notifications queued");
  Check(!(bool)Call(controller,"ContinueChargeSaveFence",second),"foreign save cannot continue another operation's archive fence");
  Call(controller,"ReleaseChargeSaveFence",first);
  writing=false;Call(controller,"ResumeChargeNativeBoundaries");Call(controller,"ResumeChargeNativeBoundaries");
  Check(retired==110&&!(bool)Call(controller,"get_ChargeNativeBoundaryPending"),"actual settled retry retires both exact actors once and coalesces only duplicate identity");
  var movementType=mod.GetType("KingmakerMountedCombat.Integration.MountedMovementStateAdapter",true);
  var movement=Activator.CreateInstance(movementType,true);var allocationType=movementType.GetNestedType("Allocation",F);
  var allocations=(System.Collections.IDictionary)Get(movement,"allocations");
  allocations.Add(destroyedOne,Activator.CreateInstance(allocationType,true));allocations.Add(destroyedTwo,Activator.CreateInstance(allocationType,true));
  var unified=Blank(mod.GetType("KingmakerMountedCombat.Integration.UnifiedMountedTurnCoordinator",true));
  Set(unified,"settings",Activator.CreateInstance(Field(unified.GetType(),"settings").FieldType,true));Set(unified,"movementState",movement);
  writing=true;
  Call(controller,"RetainDestroyedActorBoundary",destroyedOne,new Action(()=>Call(unified,"RetireDestroyedActor",destroyedOne)));
  Check(allocations.Contains(destroyedOne)&&allocations.Contains(destroyedTwo),"archive holds actual native allocation records until retirement may execute");
  writing=false;Call(controller,"ResumeChargeNativeBoundaries");
  Check(!allocations.Contains(destroyedOne)&&allocations.Contains(destroyedTwo),"deferred production retirement removes exact stale allocation without changing foreign actor; fault="+Call(boundaries,"get_Fault"));
  var nativeController=Blank(native.GetType("TurnBased.Controllers.CombatController",true));
  var modeKind=Enum.Parse(kindType,"Mode");
  Set(controller,"chargeBoundaryPermitController",nativeController);Set(controller,"chargeBoundaryPermitKind",modeKind);
  Set(controller,"chargeBoundaryPermitValue",true);Set(controller,"chargeBoundaryPermit",true);
  Check((bool)Call(controller,"AdmitChargeNativeBoundary",nativeController,modeKind,true)&&!(bool)Get(controller,"chargeBoundaryPermit"),"actual exact native replay consumes its permit once");
  var patches=mod.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
  var bridge=patches.GetNestedType("PatchBridge",F);var methods=patches.GetNestedType("PatchMethods",F);
  var bridgeCombat=bridge.GetField("Combat",F);var bridgePersistence=bridge.GetField("Persistence",F);
  var oldCombat=bridgeCombat.GetValue(null);var oldPersistence=bridgePersistence.GetValue(null);var notifications=0;
  var modeEvent=controller.GetType().GetEvent("ChargeNativeModeResumed",F);var onMode=new Action<bool>(value=>notifications++);
  modeEvent.GetAddMethod(true).Invoke(controller,new object[]{onMode});
  try{
   bridgeCombat.SetValue(null,controller);bridgePersistence.SetValue(null,null);
   methods.GetMethod("ChargeModeBoundaryPostfix",F).Invoke(null,new object[]{true,false});
   Check(notifications==0,"skipped native mode body does not emit KMC completion");
   methods.GetMethod("ChargeModeBoundaryPostfix",F).Invoke(null,new object[]{true,true});
   Check(notifications==1,"admitted native mode body emits one KMC completion");
   Call(boundaries,"Enqueue",modeKind,nativeController,new Func<bool>(()=>true),new Action(()=>{}));
   foreach(var tickName in new[]{"ChargeTransitionTickPrefix","CombatControllerTickPrefix"})
    Check(!(bool)methods.GetMethod(tickName,F).Invoke(null,null),"actual pending native notification fences "+tickName);
   Call(controller,"ResumeChargeNativeBoundaries");
   foreach(var tickName in new[]{"ChargeTransitionTickPrefix","CombatControllerTickPrefix"})
    Check((bool)methods.GetMethod(tickName,F).Invoke(null,null),"settled native notification releases "+tickName);
  }finally{bridgeCombat.SetValue(null,oldCombat);bridgePersistence.SetValue(null,oldPersistence);modeEvent.GetRemoveMethod(true).Invoke(controller,new object[]{onMode});}
  Console.WriteLine("CHARGE OWNERSHIP ADAPTER PASS="+passed+" FAIL=0; detached, not native gameplay qualification");
 }
}
'@
try { [ChargeOwnershipProbe]::Run($managed,$dll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
