[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$dll=Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"
if((Get-FileHash (Join-Path $managed 'Assembly-CSharp.dll')).Hash.ToLowerInvariant() -cne '3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb'){throw 'Installed native identity changed.'}
# Executes the production world factory and control-custody adapter. Native actor,
# command and logger observations are detached; no Game singleton or Unity world.
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
public static class WorldOwnershipProbe {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static int passed;
 static object Blank(Type type){return FormatterServices.GetUninitializedObject(type);}
 static FieldInfo Field(Type type,string name){for(;type!=null;type=type.BaseType){var field=type.GetField(name,F);if(field!=null)return field;}throw new MissingFieldException(name);}
 static object Get(object value,string name){return Field(value.GetType(),name).GetValue(value);}
 static void Set(object value,string name,object data){Field(value.GetType(),name).SetValue(value,data);}
 static object Call(object value,string name,params object[] args){return value.GetType().GetMethod(name,F).Invoke(value,args);}
 static void Check(bool result,string text){if(!result)throw new InvalidOperationException(text);passed++;Console.WriteLine("PASS "+text);}
 sealed class Logger:RealProxy {
  internal Logger(Type type):base(type){}
  public override IMessage Invoke(IMessage message){return new ReturnMessage(null,null,0,null,(IMethodCallMessage)message);}
 }
 public static void Run(string managed,string dll){
  AppDomain.CurrentDomain.AssemblyResolve+=(sender,args)=>{
   var leaf=new AssemblyName(args.Name).Name+".dll";
   foreach(var root in new[]{managed,Path.Combine(managed,"UnityModManager")}){
    var path=Path.Combine(root,leaf);if(File.Exists(path))return Assembly.LoadFrom(path);
   }return null;
  };
  var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));var mod=Assembly.LoadFrom(dll);
  Func<string,Type> type=name=>mod.GetType("KingmakerMountedCombat.Integration."+name,true);
  var settings=Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Diagnostics.DiagnosticSettings",true));
  var logger=new Logger(mod.GetType("KingmakerMountedCombat.Logging.IModLogger",true)).GetTransparentProxy();
  var gate=type("NativeMountedSaveFormat").GetProperty("ArchiveNativeSupportEnabled",F);
  var before=gate.GetValue(null,null);gate.SetValue(null,true,null);
  try {
   var world=Activator.CreateInstance(type("MountedWorldService"),F,null,new[]{logger,settings},null);
   var primary=Call(world,"get_Primary");var registry=Get(world,"Registry");
   var unitType=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
   var actorIds=new System.Collections.Generic.Dictionary<object,string>();
   // Supply the external stable ID reader; objects themselves are real installed types.
   var idReader=typeof(WorldOwnershipProbe).GetMethod("ReadId",F).MakeGenericMethod(unitType);
   ids=actorIds;Field(registry.GetType(),"actorId").SetValue(registry,Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(unitType,typeof(string)),idReader));
   Func<object> actor=()=>{var value=Blank(unitType);actorIds.Add(value,Guid.NewGuid().ToString("N"));return value;};
   var ar=actor();var am=actor();var br=actor();var bm=actor();var session=new object();var campaign=Guid.NewGuid().ToString("N");
   var a=Call(world,"GetOrCreateAdmissionForSession",session,ar,am);
   Check(Object.ReferenceEquals(a,Get(primary,"Relationship")),"first native admission owns the primary pair boundary");
   var controlType=type("NativeMountedControlService");
   var controls=Activator.CreateInstance(controlType,F,null,new[]{a,Get(primary,"PlayerActions"),Get(primary,"Combat"),
    Blank(type("HorseCompanionBlueprintService")),settings,Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Diagnostics.NativeLifecycleDeliveryLedger",true),true),logger},null);
   Call(world,"BindControls",controls);
   var commandType=native.GetType("Kingmaker.UnitLogic.Commands.UnitUseAbility",true);
   var command=Blank(commandType);
   var request=Call(registry,"CaptureRequest",session,campaign,ar,am,0L,a,false,6);
   var shellType=controlType.GetNestedType("NativeRelationshipShell",F);var shell=Activator.CreateInstance(shellType,F,null,new[]{command},null);
   Set(shell,"PairRequest",request);
   Call(Get(controls,"relationshipShells"),"Add",command,shell);
   var weakType=typeof(WeakReference<>).MakeGenericType(commandType);
   ((IList)Get(controls,"admittedControlCommands")).Add(Activator.CreateInstance(weakType,new[]{command}));
   Check((bool)Call(controls,"HasUnsettledShellFor",a),"registered unstarted native shell retains its boundary during another admission");
   var b=Call(world,"GetOrCreateAdmissionForSession",session,br,bm);
   Check(!Object.ReferenceEquals(a,b)&&Object.ReferenceEquals(Call(world,"GetOrCreateAdmissionForSession",session,ar,am),a),
    "two concurrent native admission shells have independent stable boundaries");
   Check((int)Call(world,"get_BoundaryCount")==2,"world factory creates only the required bounded pair services");
   var second=Call(world,"Find",b);var ua=Get(primary,"Participant");var ub=Get(second,"Participant");
   Check(Object.ReferenceEquals(Get(ua,"movementState"),Get(ub,"movementState"))&&
    Object.ReferenceEquals(Get(ua,"movementState"),Get(Get(world,"UnifiedTurn"),"movementState")),
    "both production participants share the world's sole allocation ledger");
   world.GetType().GetProperty("AdmissionFenced",F).SetValue(world,new Func<bool>(()=>true),null);
   Check(Call(world,"GetOrCreateAdmissionForSession",session,actor(),actor())==null&&
    Object.ReferenceEquals(Call(world,"GetOrCreateAdmissionForSession",session,ar,am),a),
    "world save fence refuses new admissions while retaining the exact previously admitted owner");
   world.GetType().GetProperty("AdmissionFenced",F).SetValue(world,new Func<bool>(()=>false),null);
   Set(shell,"Consumed",true);
   Check((bool)Call(controls,"HasUnsettledShellFor",a),
    "consumed delivery retains custody while its exact native command is unfinished");
   Check(((Func<bool>)Get(a,"retainedCommands"))()&&!((Func<bool>)Get(a,"pairSaveReady"))()&&!((Func<bool>)Get(a,"pairTeardownReady"))(),
    "native shell lifetime retains registry custody and refuses save and teardown readiness");
   var reservation=new object[]{session,campaign,ar,am,1L,a,Get(a,"retainedCommands"),Get(a,"pairSaveReady"),Get(a,"pairTeardownReady"),null,null,6,null};
   Check((bool)Call(registry,"TryReserve",reservation),"shell retention fixture reserves its exact actor roles");
   Call(registry,"Commit",reservation[9]);Call(registry,"Detach",reservation[9]);
   Call(world,"ReleaseSettledAdmissions");
   Check((long)Call(a,"get_MountedPairGeneration")==0L&&Object.ReferenceEquals(Call(registry,"FindOwner",ar),reservation[9])&&
    Object.ReferenceEquals(Call(registry,"FindOwner",am),reservation[9]),
    "detached consumed shell holds both actor indexes and original boundary generation until native terminal");
   var conflict=new object[]{session,campaign,ar,actor(),1L,b,new Func<bool>(()=>false),new Func<bool>(()=>true),new Func<bool>(()=>true),null,null,6,null};
   Check(!(bool)Call(registry,"TryReserve",conflict),
    "another pair cannot acquire an actor whose old consumed shell still owns native callbacks");
   Set(shell,"Retired",true);
   Check((bool)Call(controls,"HasUnsettledShellFor",a),"retired delivery remains held until the native command settles");
   Set(command,"<IsFinished>k__BackingField",true);
   Call(world,"ReleaseSettledAdmissions");
   Check((long)Call(a,"get_MountedPairGeneration")==1L,"settled boundary recycling advances generation without native preparation");
   var resolve=new object[]{request,session,campaign,1L,null};
   Check(!(bool)Call(registry,"TryResolve",resolve),"a delayed consumed shell cannot authorize a recycled boundary generation");
   var reused=Call(world,"GetOrCreateAdmissionForSession",session,ar,am);
   Check(Object.ReferenceEquals(a,reused)&&(int)Call(world,"get_BoundaryCount")==2,
    "settled remount reuses bounded pair services instead of accumulating world subscriptions");
   var oldSession=new object();var oldRequest=Call(registry,"CaptureRequest",oldSession,campaign,br,bm,1L,b,false,6);
   var stale=new object[]{oldRequest,session,campaign,1L,null};
   Check(!(bool)Call(registry,"TryResolve",stale),"old-session admission is refused even when actor objects and generation match");
   var visits=0;
   var visitor=typeof(WorldOwnershipProbe).GetMethod("VisitPair",F).MakeGenericMethod(type("MountedPairServices"));
   visit=pair=>{visits++;if(Object.ReferenceEquals(pair,primary))throw new InvalidOperationException("pair A cleanup held");};
   var action=Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(type("MountedPairServices")),visitor);
   var errors=(Array)Call(world,"Visit",action);
   Check(visits==2&&errors.Length==1,"world cleanup attempts the healthy neighbour after one owner fails");
   var q1=Get(Get(primary,"Combat"),"chargeBoundaries");var q2=Get(Get(second,"Combat"),"chargeBoundaries");
   Check(q1==null&&q2==null,"pair construction creates no competing native transition replay queues");
   var allocation=Get(Get(world,"UnifiedTurn"),"movementState");
   Set(allocation,"preparingTurn",Blank(native.GetType("TurnBased.Controllers.TurnController",true)));
   var disposalRefused=false;
   try{Call(world,"Dispose");}catch(TargetInvocationException error){disposalRefused=error.InnerException is InvalidOperationException;}
   Check(disposalRefused&&!(bool)Get(world,"disposed")&&(int)Call(world,"get_BoundaryCount")==2,
    "direct world teardown refuses while a native preparation scope still owns the shared ledger");
   Set(allocation,"preparingTurn",null);
   var turnWorld=Get(world,"UnifiedTurn");
   var actorAllocations=(IDictionary)Get(allocation,"allocations");
   var debt=Activator.CreateInstance(allocation.GetType().GetNestedType("Allocation",F),true);
   actorAllocations.Add(ar,debt);
   var constructionSteps=new System.Collections.Generic.List<string>();
   var faultStep=world.GetType().GetProperty("BeforePairConstructionStep",F);
   faultStep.SetValue(world,new Action<string>(step=>{
    constructionSteps.Add(step);
    if(step=="publish"){
     Check((int)Call(world,"get_BoundaryCount")==2&&((Array)Call(turnWorld,"get_SnapshotParticipants")).Length==2,
      "complete new pair stays unpublished until its final construction boundary");
     throw new InvalidOperationException("selected publication failure");
    }
   }),null);
   AggregateException constructionError=null;
   try{Call(world,"ConstructPair");}catch(TargetInvocationException error){constructionError=error.InnerException as AggregateException;}
   if(constructionError!=null&&constructionError.InnerExceptions.Count!=1)Console.WriteLine(constructionError.ToString());
   Check(constructionError!=null&&constructionError.InnerExceptions.Count==1&&
    constructionError.InnerExceptions[0].Message=="selected publication failure",
    "secondary construction exposes the original failure after successful independent rollback");
   Check(constructionSteps.Where(step=>step.StartsWith("rollback-")).SequenceEqual(new[]{
    "rollback-player-actions","rollback-combat","rollback-scheduler","rollback-participant","rollback-relationship"}),
    "failed construction attempts every acquired service cleanup independently");
   Check((int)Call(world,"get_BoundaryCount")==2&&((Array)Call(turnWorld,"get_SnapshotParticipants")).Length==2&&
    (int)Call(world,"get_FailedConstructionCount")==0&&Object.ReferenceEquals(actorAllocations[ar],debt),
    "failed unpublished pair preserves both active service boundaries and exact actor expenditure");
   constructionSteps.Clear();
   faultStep.SetValue(world,new Action<string>(step=>{
    constructionSteps.Add(step);
    if(step=="publish"||step=="rollback-combat")throw new InvalidOperationException("selected "+step+" failure");
   }),null);
   constructionError=null;
   try{Call(world,"ConstructPair");}catch(TargetInvocationException error){constructionError=error.InnerException as AggregateException;}
   var admissionFence=(Func<bool>)world.GetType().GetProperty("AdmissionFenced",F).GetValue(world,null);
   Check(constructionError!=null&&constructionError.InnerExceptions.Count==2&&
    (int)Call(world,"get_FailedConstructionCount")==1&&admissionFence()&&
    Call(world,"GetOrCreateAdmissionForSession",session,actor(),actor())==null,
    "unfinished construction cleanup remains owned and fences additional native admission");
   Check(constructionSteps.Contains("rollback-relationship")&&((Array)Call(turnWorld,"get_SnapshotParticipants")).Length==2&&
    Object.ReferenceEquals(actorAllocations[ar],debt),
    "one rollback failure cannot skip remaining cleanup or erase another actor's expenditure");
   faultStep.SetValue(world,null,null);
   Call(world,"RetryFailedConstructionCleanup");
   Check((int)Call(world,"get_FailedConstructionCount")==0&&!admissionFence()&&
    (int)Call(world,"get_BoundaryCount")==2&&Object.ReferenceEquals(actorAllocations[ar],debt),
    "retained construction retry releases its fence only after every local cleanup settles");
   var primarySteps=new System.Collections.Generic.List<string>();
   var failPrimary=new Action<string>(step=>{primarySteps.Add(step);if(step=="publish")throw new InvalidOperationException("selected primary failure");});
   object failedPrimary=null;AggregateException primaryError=null;
   try{failedPrimary=Activator.CreateInstance(type("MountedWorldService"),F,null,new object[]{logger,settings,failPrimary},null);}
   catch(TargetInvocationException error){primaryError=error.InnerException as AggregateException;}
   Check(failedPrimary==null&&primaryError!=null&&primaryError.InnerExceptions.Count==1&&
    primaryError.InnerExceptions[0].Message=="selected primary failure"&&
    primarySteps.Where(step=>step.StartsWith("rollback-")).SequenceEqual(new[]{
     "rollback-player-actions","rollback-combat","rollback-scheduler","rollback-participant","rollback-relationship"}),
    "primary construction failure rolls back every local service without consulting a half-published world");
   actorAllocations.Remove(ar);
   var context=Blank(native.GetType("Kingmaker.UnitLogic.Abilities.AbilityExecutionContext",true));
   var commandReference=RetainContextCommand(controls,commandType,shellType,context,request);
   CollectReleasedObjects();
   Check(commandReference.IsAlive&&(bool)Call(controls,"HasUnsettledShellFor",a),
    "exact live native context retains its admitted command and actor custody after the command slot is gone");
   Call(Get(controls,"relationshipShellContexts"),"Remove",context);
   CollectReleasedObjects();
   Check(!commandReference.IsAlive&&!(bool)Call(controls,"HasUnsettledShellFor",a),
    "released context-command cycle is collectible and leaves no false shell custody");
   GC.KeepAlive(context);
  }finally{gate.SetValue(null,before,null);}
  Console.WriteLine("WORLD OWNERSHIP ADAPTER PASS="+passed+" FAIL=0; detached external observations, not native qualification");
 }
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static WeakReference RetainContextCommand(object controls,Type commandType,Type shellType,object context,object request){
  var command=Blank(commandType);
  var shell=Activator.CreateInstance(shellType,F,null,new[]{command},null);
  Set(shell,"PairRequest",request);
  Call(Get(controls,"relationshipShells"),"Add",command,shell);
  Call(Get(controls,"relationshipShellContexts"),"Add",context,shell);
  ((IList)Get(controls,"admittedControlCommands")).Add(Activator.CreateInstance(typeof(WeakReference<>).MakeGenericType(commandType),new[]{command}));
  return new WeakReference(command);
 }
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static void CollectReleasedObjects(){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}
 static System.Collections.Generic.Dictionary<object,string> ids;
 static string ReadId<T>(T actor){return ids[actor];}
 static Action<object> visit;
 static void VisitPair<T>(T pair){visit(pair);}
}
'@
try { [WorldOwnershipProbe]::Run($managed,$dll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
