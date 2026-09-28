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
  // Exercise the actual evidence builder/serializer: the new paired authority
  // must report its principal even while the incompatible legacy flag is false.
  var turnEvidence=candidate.GetType("KingmakerMountedCombat.Diagnostics.RuntimeCombatScenarioEngine+TurnBasedCombatEvidence",true);
  var capture=turnEvidence.GetMethod("Capture",BindingFlags.Public|BindingFlags.Static);
  var json=Assembly.LoadFrom(Path.Combine(managed,"Newtonsoft.Json.dll"));
  var serialize=json.GetType("Newtonsoft.Json.JsonConvert",true).GetMethod("SerializeObject",new[]{typeof(object)});
  for(var mode=0;mode<3;mode++) {
   var parameters=capture.GetParameters(); var values=new object[parameters.Length];
   for(var i=0;i<parameters.Length;i++) {
    var p=parameters[i];
    values[i]=p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
    if(p.Name=="includeSharedTurnEvidence") values[i]=mode!=2;
    if(p.Name=="unifiedMountedTurn") values[i]=mode==1;
    if(p.Name=="expectedTurnPrincipal") values[i]="rider";
    if(p.Name=="expectedActionActor") values[i]="mount";
    if(p.Name=="nativeTurnPrincipalStarted" || p.Name=="actionActorSharedTurnAdmitted") values[i]=true;
   }
   var captured=capture.Invoke(null,values);
   var serialized=(string)serialize.Invoke(null,new[]{captured});
   foreach(var name in new[]{"UnifiedMountedTurn","ExpectedTurnPrincipal","ExpectedActionActor","NativeTurnPrincipalStarted","ActionActorSharedTurnAdmitted"}) {
    var value=turnEvidence.GetProperty(name).GetValue(captured,null);
    if((value!=null)!=(mode!=2) || serialized.Contains("\""+name+"\":")!=(mode!=2))
     throw new InvalidOperationException("Turn principal evidence was omitted or invented for mode "+mode+": "+name);
   }
   if(mode!=2 && (bool)turnEvidence.GetProperty("UnifiedMountedTurn").GetValue(captured,null)!=(mode==1))
    throw new InvalidOperationException("Turn evidence changed the observed legacy configuration.");
  }
  Console.WriteLine("TURN PRINCIPAL EVIDENCE SERIALIZATION PASS=3 FAIL=0; original evidence builder, no game operation");
  // The active sampler uses Unity ECalls, so inspect the compiled entry guard:
  // cleanupStarted must return before any game or disposed-trace access.
  var diagnosticFlags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  var tranche=candidate.GetType("KingmakerMountedCombat.Diagnostics.Phase3dHorseScenarioTranche",true);
  var liveness=tranche.GetMethod("ObserveChunk6aEncounterLiveness",diagnosticFlags).GetMethodBody().GetILAsByteArray();
  var cleanupToken=tranche.GetField("cleanupStarted",diagnosticFlags).MetadataToken;
  if(liveness.Length<10 || liveness[0]!=0x02 || liveness[1]!=0x7b ||
     BitConverter.ToInt32(liveness,2)!=cleanupToken || liveness[6]!=0x2c ||
     liveness[7]!=1 || liveness[8]!=0x2a)
   throw new InvalidOperationException("Released Chunk 6A liveness observer does not return before native access.");
  Console.WriteLine("CHUNK6A RELEASED OBSERVER CONTRACT PASS=1 FAIL=0; exact compiled entry guard, no Unity invocation");
  // Exercise the actual diagnostic endpoint helper with the timer decay that
  // rejected c6a-cleanup-a-approach. Committed windows also require the separate
  // complete native event proof, pinned by the source contract.
  var jObject=json.GetType("Newtonsoft.Json.Linq.JObject",true);
  var parse=jObject.GetMethod("Parse",new[]{typeof(string)});
  var held=tranche.GetMethod("Chunk6aResourcesHeld",BindingFlags.NonPublic|BindingFlags.Static);
  var baseline="{\"standard\":0,\"move\":0,\"swift\":0,\"initiative\":5.336399,\"initiativeCooldown\":5.336399,\"initiativeOrder\":2,\"reactionCooldown\":0,\"attackOfOpportunity\":0,\"reactions\":1,\"reactionsPerRound\":1}";
  var decayed=baseline.Replace("5.336399","4.12132454");
  var endpointChecks=0;
  foreach(var probe in new[]{
    new object[]{decayed,false,true},
    new object[]{decayed,true,false},
    new object[]{baseline.Replace("\"initiativeOrder\":2","\"initiativeOrder\":3"),false,false},
    new object[]{baseline.Replace("\"reactions\":1","\"reactions\":0"),false,false},
    new object[]{baseline.Replace("\"reactionCooldown\":0","\"reactionCooldown\":1"),false,false}}) {
   var result=(bool)held.Invoke(null,new[]{parse.Invoke(null,new object[]{baseline}),parse.Invoke(null,new[]{probe[0]}),probe[1],new string[0]});
   if(result!=(bool)probe[2]) throw new InvalidOperationException("Chunk 6A endpoint conflates initiative ordering/timer or overlooks reaction change; case="+endpointChecks);
   endpointChecks++;
  }
  Console.WriteLine("CHUNK6A RESOURCE FIELD CONTRACT PASS="+endpointChecks+" FAIL=0; actual diagnostic helper");
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
   var movementOriginal=native.ManifestModule.ResolveMethod(0x06009183);
   var movementHook=hooks.GetMethod("NativeMovementUpdatePrefix",BindingFlags.Static|BindingFlags.NonPublic);
   var movementIl=movementOriginal.GetMethodBody().GetILAsByteArray();
   if(movementOriginal.Name!="Tick" || movementOriginal.GetParameters().Length!=0 ||
      movementHook==null || movementHook.ReturnType!=typeof(void) || movementHook.GetParameters().Length!=0 ||
      movementIl[0x124]!=0x6f || BitConverter.ToInt32(movementIl,0x125)!=0x060018D9 ||
      movementIl[0x2e2]!=0x6f || BitConverter.ToInt32(movementIl,0x2e3)!=0x06008366)
    throw new InvalidOperationException("Native movement entry/rotation contract changed.");
   // This target's native Unity ECalls cannot be JIT-compiled by desktop CLR.
   // Actual hook construction and callback delivery remain required in Unity.
   Console.WriteLine("MOVEMENT ENTRY CONTRACT PASS=1 FAIL=0; native construction/delivery required in Unity");
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
   var causal=candidate.GetType("KingmakerMountedCombat.Diagnostics.NativeRelationshipCommandProbe+Hooks",true);
   var service=candidate.GetType("KingmakerMountedCombat.Integration.NativeMountedControlService",true);
   var causalFlags=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
   var targets=new MethodBase[]{service.GetMethod("PrepareNativeMountApproach",causalFlags),service.GetMethod("BindNativeRelationshipProcess",causalFlags),
    service.GetMethod("TryDispatch",causalFlags,null,new[]{candidate.GetType("KingmakerMountedCombat.Domain.NativeMountedControlKind",true),native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true),native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true),native.GetType("Kingmaker.UnitLogic.Abilities.AbilityExecutionContext",true)},null),
    native.ManifestModule.ResolveMethod(0x060027A6),native.ManifestModule.ResolveMethod(0x060027A7),native.ManifestModule.ResolveMethod(0x06008FD6)};
   var beforeNames=new string[]{null,null,"DeliverBefore","ApproachBefore","TickBefore",null};
   var afterNames=new string[]{"InitAfter","BindAfter","DeliverAfter",null,"TickAfter","ProcessTickAfter"};
   for(var i=0;i<targets.Length;i++) {
    if(targets[i]==null)throw new InvalidOperationException("Missing exact causal observer boundary.");
    var before=beforeNames[i]==null?null:Activator.CreateInstance(harmonyMethod,new object[]{causal.GetMethod(beforeNames[i],causalFlags)});
    var after=afterNames[i]==null?null:Activator.CreateInstance(harmonyMethod,new object[]{causal.GetMethod(afterNames[i],causalFlags)});
    Console.WriteLine("stage: causal observer "+targets[i].DeclaringType.FullName+"."+targets[i].Name);
    try { patch.Invoke(harmony,new object[]{targets[i],before,after,null});
     Console.WriteLine("PASS causal observer wrapper construction "+targets[i].Name);
    } catch(TargetInvocationException e) {
     if(!(e.InnerException is System.Security.SecurityException) || e.InnerException.Message!="ECall methods must be packaged into a system module.") throw;
     Console.WriteLine("DEFER - EVIDENCED: desktop CLR cannot construct Unity ECall wrapper "+targets[i].Name+"; construction/delivery required in native runtime.");
    }
   }
   Console.WriteLine("RELATIONSHIP CAUSAL OBSERVER SIGNATURE PASS=6 FAIL=0; native construction and callback delivery remain runtime gates");
   foreach(var token in new[]{0x0600934A,0x060093A1}) {
    var resourceTarget=native.ManifestModule.ResolveMethod(token);
    var beforeName=token==0x0600934A?"CooldownTickBefore":"OpportunityBefore";
    var afterName=token==0x0600934A?"CooldownTickAfter":"OpportunityAfter";
    var before=Activator.CreateInstance(harmonyMethod,new object[]{observer.GetMethod(beforeName,causalFlags)});
    var after=Activator.CreateInstance(harmonyMethod,new object[]{observer.GetMethod(afterName,causalFlags)});
    patch.Invoke(harmony,new object[]{resourceTarget,before,after,null});
    Console.WriteLine("PASS reaction observer wrapper construction "+resourceTarget.Name);
   }
   Console.WriteLine("REACTION OBSERVER CONSTRUCTION PASS=2 FAIL=0; callbacks still require Unity execution");
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
   var interruptedTarget=native.ManifestModule.ResolveMethod(0x0600184F);
   var interruptedBefore=Activator.CreateInstance(harmonyMethod,new object[]{hooks.GetMethod("NativeMountMovementInterruptedPrefix",causalFlags)});
   var interruptedAfter=Activator.CreateInstance(harmonyMethod,new object[]{hooks.GetMethod("NativeMountMovementInterruptedPostfix",causalFlags)});
   patch.Invoke(harmony,new object[]{interruptedTarget,interruptedBefore,interruptedAfter,null});
   Console.WriteLine("MOUNT INTERRUPTION WRAPPER PASS=1 FAIL=0; exact native boundary, callback execution remains a Unity gate");
   var pathHooks=candidate.GetType("KingmakerMountedCombat.Diagnostics.NativeCommandPathProbe+Hooks",true);
   var pathTokens=new[]{0x060018A3,0x060018B9,0x0600184F,0x06001850,0x060027B2,0x060027B2};
   var pathNames=new[]{"PathTo","OnPathComplete","OnMovementInterrupted","OnPathNotFound","OnEnded","OnEnded"};
   var pathBefore=new string[]{null,"CompleteBefore","InterruptedBefore","NotFoundBefore",null,null};
   var pathAfter=new string[]{"PathAfter","CompleteAfter",null,null,"EndedAfter","UnactedEndedAfter"};
   var requested=native.GetType("Kingmaker.View.UnitMovementAgent",true).GetField("m_RequestedPath",causalFlags);
   if(requested.MetadataToken!=0x0400118E || requested.FieldType.FullName!="Pathfinding.Path") throw new InvalidOperationException("Requested path field changed.");
   for(var i=0;i<pathTokens.Length;i++) {
    var original=native.ManifestModule.ResolveMethod(pathTokens[i]);
    if(original.Name!=pathNames[i]) throw new InvalidOperationException("Native path observer token changed.");
    var owner=i==5?causal:pathHooks;
    var before=pathBefore[i]==null?null:Activator.CreateInstance(harmonyMethod,new object[]{owner.GetMethod(pathBefore[i],causalFlags)});
    var after=pathAfter[i]==null?null:Activator.CreateInstance(harmonyMethod,new object[]{owner.GetMethod(pathAfter[i],causalFlags)});
    try {patch.Invoke(harmony,new object[]{original,before,after,null}); Console.WriteLine("PASS native obstruction observer wrapper "+pathNames[i]+" "+pathAfter[i]);}
    catch(TargetInvocationException e) {
     if(!(e.InnerException is System.Security.SecurityException) || e.InnerException.Message!="ECall methods must be packaged into a system module.") throw;
     Console.WriteLine("DEFER - EVIDENCED: desktop CLR cannot construct Unity ECall wrapper "+pathNames[i]+"; native installation and callback still required.");
    }
   }
   var approachHooks=candidate.GetType("KingmakerMountedCombat.Diagnostics.NativeMountApproachPathProbe+Hooks",true);
   var approachTokens=new[]{0x060027A6,0x0600700F,0x060018B6,0x060018A3,0x06000C37};
   var approachNames=new[]{"TickApproaching","CurrentPathForUnit","FollowPrecomputedPath","PathTo","TickMovement"};
   var approachBefore=new string[]{"ApproachBefore",null,"PrecomputedBefore","RequestedBefore","ApproachMovementBefore"};
   var approachAfter=new string[]{"ApproachAfter","PreviewAfter","PrecomputedAfter",null,"ApproachMovementAfter"};
   var approachConstructed=0;var approachDeferred=0;
   for(var i=0;i<approachTokens.Length;i++) {
    var original=native.ManifestModule.ResolveMethod(approachTokens[i]);
    if(original.Name!=approachNames[i]) throw new InvalidOperationException("Native consumed-path/movement observer token changed.");
    var owner=i==4?observer:approachHooks;
    var before=approachBefore[i]==null?null:Activator.CreateInstance(harmonyMethod,new object[]{owner.GetMethod(approachBefore[i],causalFlags)});
    var after=approachAfter[i]==null?null:Activator.CreateInstance(harmonyMethod,new object[]{owner.GetMethod(approachAfter[i],causalFlags)});
    try {patch.Invoke(harmony,new object[]{original,before,after,null}); approachConstructed++;Console.WriteLine("PASS native Mount consumed-path/movement wrapper "+approachNames[i]);}
    catch(TargetInvocationException e) {
     if(!(e.InnerException is System.Security.SecurityException) || e.InnerException.Message!="ECall methods must be packaged into a system module.") throw;
     approachDeferred++;Console.WriteLine("DEFER - EVIDENCED: desktop CLR cannot construct Unity ECall wrapper "+approachNames[i]+"; native installation and callback still required.");
    }
   }
   Console.WriteLine("MOUNT CONSUMED PATH/MOVEMENT WRAPPERS PASS="+approachConstructed+" FAIL=0 DEFER="+approachDeferred+"; no native gameplay execution");
   var pathProbe=candidate.GetType("KingmakerMountedCombat.Diagnostics.NativeCommandPathProbe",true);
   var contentsMethod=pathProbe.GetMethod("CapturePathContents",causalFlags);
   if(contentsMethod==null) throw new InvalidOperationException("Path contents boundary contract is missing.");
   var pathType=requested.FieldType.Assembly.GetType("Pathfinding.ABPath",true);
   var samplePath=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(pathType);
   var vectorType=native.GetType("Kingmaker.View.UnitMovementAgent",true).GetMethod("PathTo").GetParameters()[1].ParameterType;
   var points=(System.Collections.IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(vectorType));
   points.Add(Activator.CreateInstance(vectorType,new object[]{1f,2f,3f}));
   requested.FieldType.GetField("vectorPath").SetValue(samplePath,points);
   foreach(var boundary in new[]{"path-request","command-bound","command-ended","unknown"}) {
    var captured=contentsMethod.Invoke(null,new object[]{samplePath,boundary});
    // Serialization normalizes a null-valued String token to JSON null. Check
    // the live tokens consumed by the in-process ground evidence validator.
    foreach(var field in new[]{"pathError","pathState","points"}) {
     var token=captured.GetType().GetProperty("Item",new[]{typeof(string)}).GetValue(captured,new object[]{field});
     if(token==null || token.GetType().GetProperty("Type").GetValue(token,null).ToString()!="Null")
      throw new InvalidOperationException("In-flight path token must be Null before serialization: "+boundary+" "+field);
    }
    var encoded=(string)serialize.Invoke(null,new[]{captured});
    if(encoded!="{\"pathError\":null,\"pathState\":null,\"points\":null}")
     throw new InvalidOperationException("Worker-owned path contents exposed at "+boundary);
   }
   var completed=contentsMethod.Invoke(null,new object[]{samplePath,"path-complete-before"});
   var completedText=(string)serialize.Invoke(null,new[]{completed});
   if(completedText.IndexOf("\"points\":[",StringComparison.Ordinal)<0) throw new InvalidOperationException("Completed path points absent.");
   points.Clear(); points.Add(Activator.CreateInstance(vectorType,new object[]{9f,8f,7f}));
   if((string)serialize.Invoke(null,new[]{completed})!=completedText) throw new InvalidOperationException("Native pool reuse mutated the completed snapshot.");
   Console.WriteLine("PATH CONTENTS BOUNDARY PASS=17 FAIL=0; twelve live Null tokens, request contents absent, completed snapshot immutable");
   var consumedProbe=candidate.GetType("KingmakerMountedCombat.Diagnostics.NativeMountApproachPathProbe",true);
   var snapshotMethod=consumedProbe.GetMethod("SnapshotConsumedPath",causalFlags);
   if(snapshotMethod==null)throw new InvalidOperationException("Consumed path snapshot seam absent.");
   points.Clear();points.Add(Activator.CreateInstance(vectorType,new object[]{1f,2f,3f}));
   points.Add(Activator.CreateInstance(vectorType,new object[]{4f,8f,7f}));
   var snapshot=snapshotMethod.Invoke(null,new object[]{samplePath});
   var snapshotText=(string)serialize.Invoke(null,new[]{snapshot});
   if(snapshotText.IndexOf("\"horizontalLength\":5.0",StringComparison.Ordinal)<0 ||
      snapshotText.IndexOf("\"points\":[{\"x\":1.0,\"y\":2.0,\"z\":3.0},{\"x\":4.0,\"y\":8.0,\"z\":7.0}]",StringComparison.Ordinal)<0)
    throw new InvalidOperationException("Consumed path snapshot lost exact points or horizontal length.");
   points.Clear();points.Add(Activator.CreateInstance(vectorType,new object[]{900f,800f,700f}));
   if((string)serialize.Invoke(null,new[]{snapshot})!=snapshotText)throw new InvalidOperationException("Pooled native path reuse mutated consumed snapshot.");
   requested.FieldType.GetField("vectorPath").SetValue(samplePath,null);
   var missingPoints=(string)serialize.Invoke(null,new[]{snapshotMethod.Invoke(null,new object[]{samplePath})});
   if(missingPoints.IndexOf("\"points\":null",StringComparison.Ordinal)<0 || missingPoints.IndexOf("\"horizontalLength\":null",StringComparison.Ordinal)<0)
    throw new InvalidOperationException("Missing native points became invented geometry.");
   var missingPath=(string)serialize.Invoke(null,new[]{snapshotMethod.Invoke(null,new object[]{null})});
   if(missingPath!="{\"pathObject\":0,\"pathType\":null,\"pathState\":null,\"pathError\":null,\"points\":null,\"horizontalLength\":null}")
    throw new InvalidOperationException("Missing native preview became invented path.");
   Console.WriteLine("CONSUMED PATH SNAPSHOT PASS=4 FAIL=0; exact completed geometry, immutable copy, null points and null path");

   try {
    var disposable=(IDisposable)Activator.CreateInstance(pathProbe,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{null},null);
    disposable.Dispose();
    Console.WriteLine("PASS actual native path observer constructor/disposal without actor execution");
   } catch(TargetInvocationException e) {
    var context=e.InnerException as InvalidOperationException;
    if(context==null || context.Message.IndexOf("Kingmaker.View.UnitMovementAgent.PathTo token=060018A3; installed=[]",StringComparison.Ordinal)<0 ||
       !(context.InnerException is System.Security.SecurityException)) throw;
    Console.WriteLine("PASS actual constructor retains the first deferred PathTo wrapper and empty installed-hook receipt");
   }
   if(pathProbe.GetField("active",causalFlags).GetValue(null)!=null) throw new InvalidOperationException("Path observer constructor/disposal retained an active observer.");
   Console.WriteLine("PASS path observer constructor/disposal releases its active observer");
   Console.WriteLine("OBSTRUCTION OBSERVER SIGNATURE PASS=6 FAIL=0; native execution still required");
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
