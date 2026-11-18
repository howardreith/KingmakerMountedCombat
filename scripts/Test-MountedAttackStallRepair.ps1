[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string]$Configuration='Release',
    # Another build of the product to judge with the same probe (the old/new comparison of the repair).
    [string]$ProductDll=''
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$dll=if([string]::IsNullOrWhiteSpace($ProductDll)){Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"}else{[IO.Path]::GetFullPath($ProductDll)}
if(-not(Test-Path -LiteralPath $dll -PathType Leaf)){throw "Product assembly is missing: $dll"}
if((Get-FileHash (Join-Path $managed 'Assembly-CSharp.dll')).Hash.ToLowerInvariant() -cne '3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb'){throw 'Installed native identity changed.'}
# Detached probe of the mounted attack stall repair. The controller, its terminal handler, the capability reader and
# the native state, command and condition types are the compiled originals; only the engine boundary (game instance,
# views, the turn controller's live state, the mod log) is substituted. No game is launched, so nothing here is
# native gameplay evidence: it shows what the product decides, not that a combat is fixed.
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;
public static class MountedAttackStallProbe {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static int passed;
 static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException("FAIL "+text);passed++;Console.WriteLine("PASS "+text);}
 static object Blank(Type t){return FormatterServices.GetUninitializedObject(t);}
 static FieldInfo Field(Type t,string name){for(;t!=null;t=t.BaseType){var f=t.GetField(name,F);if(f!=null)return f;}throw new MissingFieldException(name);}
 static void Set(object o,string n,object v){Field(o.GetType(),n).SetValue(o,v);}
 static object Get(object o,string n){return Field(o.GetType(),n).GetValue(o);}
 static object Call(object o,string n,params object[] args){
  try{return o.GetType().GetMethod(n,F).Invoke(o,args);}
  catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}
 }
 // Sets a property only where this build has it, so the same probe judges a build from before the repair.
 static void Offer(object o,string n,object v){
  var p=o.GetType().GetProperty(n,F);if(p==null)return;
  p.SetValue(o,p.PropertyType.IsEnum&&v is string?Enum.Parse(p.PropertyType,(string)v):v,null);
 }
 sealed class Recorder:RealProxy {
  internal readonly List<string> Calls=new List<string>();
  // Known issues Sprint 6 (A02): a logger that fails on every call.
  internal bool Throws;
  internal Recorder(Type t):base(t){}
  public override IMessage Invoke(IMessage m){
   var call=(IMethodCallMessage)m;
   Calls.Add(call.MethodName+": "+(call.ArgCount>0&&call.GetArg(0)!=null?call.GetArg(0).ToString():""));
   if(Throws)return new ReturnMessage(new InvalidOperationException("logger failure"),call);
   return new ReturnMessage(null,null,0,null,call);
  }
  internal int Count(string text){return Calls.Count(c=>c.IndexOf(text,StringComparison.Ordinal)>=0);}
  internal string Single(string text){return Calls.Single(c=>c.IndexOf(text,StringComparison.Ordinal)>=0);}
 }
 // The native warning bus is real and keys its subscribers by concrete type, so the recording subscriber is a real
 // type implementing the native handler interface; it stands in for the game's UI.
 public sealed class Sink {
  internal readonly List<string> Calls=new List<string>();
  internal int Count(string text){return Calls.Count(c=>c.IndexOf(text,StringComparison.Ordinal)>=0);}
 }
 static readonly Sink warningSink=new Sink();
 public static void Warned(string text){warningSink.Calls.Add(text??"");}
 static object WarningSubscriber(Type handler){
  var module=AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("KmcStallProbeWarnings"),AssemblyBuilderAccess.Run).DefineDynamicModule("m");
  var type=module.DefineType("WarningSubscriber",TypeAttributes.Public|TypeAttributes.Class,typeof(object),new[]{handler});
  foreach(var method in handler.GetMethods()){
   var parameters=method.GetParameters().Select(p=>p.ParameterType).ToArray();
   var body=type.DefineMethod(method.Name,MethodAttributes.Public|MethodAttributes.Virtual|MethodAttributes.HideBySig|MethodAttributes.NewSlot|MethodAttributes.Final,
    method.ReturnType,parameters);
   var il=body.GetILGenerator();
   if(parameters.Length>0&&parameters[0]==typeof(string)){il.Emit(OpCodes.Ldarg_1);il.Emit(OpCodes.Call,typeof(MountedAttackStallProbe).GetMethod("Warned"));}
   il.Emit(OpCodes.Ret);
   type.DefineMethodOverride(body,method);
  }
  return Activator.CreateInstance(type.CreateType());
 }
 static readonly Dictionary<object,string> actorIds=new Dictionary<object,string>();
 static string ActorId<T>(T actor){return actorIds[actor];}
 static HashSet<string> CalledMembers(MethodBase method){
  var il=method.GetMethodBody().GetILAsByteArray();var names=new HashSet<string>();
  for(var i=0;i+4<il.Length;i++){
   if(il[i]!=0x28&&il[i]!=0x6F)continue;
   try{names.Add(method.Module.ResolveMethod(BitConverter.ToInt32(il,i+1)).Name);}catch(Exception){}
  }
  return names;
 }

 // ---- Installed 2.1.7b contracts the repair rests on ------------------------------------------------------------
 static void ProbeNativeContracts(Assembly native){
  Check(native.ManifestModule.ModuleVersionId.ToString()=="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7","pinned native MVID");
  var command=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);
  var tick=native.GetType("Kingmaker.Controllers.Units.UnitActionController",true).GetMethod("TickCommandTurnBased",F);
  var tickCalls=CalledMembers(tick);
  Check(tick.MetadataToken==0x0600911D&&tickCalls.Contains("ForceFinishForTurnBased")&&tickCalls.Contains("get_CanAct")&&
   tickCalls.Contains("get_CanActInCombat")&&tickCalls.Contains("HasCooldownForCommand")&&tickCalls.Contains("get_IsStarted"),
   "native turn-based tick force-finishes an unstarted command from the executor's ability to act, initiative and cooldown");
  var forceCalls=CalledMembers(command.GetMethod("ForceFinish",F));
  Check(forceCalls.Contains("SetIsActed")&&forceCalls.Contains("OnEnded")&&forceCalls.Contains("set_Result"),
   "native ForceFinish stamps the result and IsActed and ends the command without starting it");
  Check(CalledMembers(command.GetMethod("ForceFinishForTurnBased",F)).Contains("ForceFinish"),
   "native ForceFinishForTurnBased is ForceFinish");
  var state=native.GetType("Kingmaker.UnitLogic.UnitState",true);
  var canAct=CalledMembers(state.GetProperty("CanAct",F).GetGetMethod(true));
  Check(canAct.Contains("get_IsHelpless")&&canAct.Contains("get_IsAble")&&canAct.Contains("HasCondition"),
   "native CanAct is composed of IsHelpless, IsAble and a condition");
  var interrupted=CalledMembers(native.GetType("Kingmaker.UnitLogic.Commands.UnitAttack",true).GetProperty("ShouldBeInterrupted",F).GetGetMethod(true));
  Check(interrupted.Contains("HasCondition"),"native UnitAttack.ShouldBeInterrupted reads an executor condition");
  var condition=native.GetType("Kingmaker.UnitLogic.UnitCondition",true);
  Check(new[]{"Dazed","Stunned","CantAct","CanNotAttack","Sleeping","Nauseated","Staggered","CantMove"}.All(n=>Enum.IsDefined(condition,n)),
   "native conditions read by the capability rule exist by name");
 }

 // ---- The production reader and rule over genuine native state --------------------------------------------------
 static Type unitType,stateType,descriptorType,conditionType;
 static object Actor(){
  var unit=Blank(unitType);var descriptor=Blank(descriptorType);
  Set(descriptor,"State",Activator.CreateInstance(stateType,new[]{descriptor}));
  Set(unit,"<Descriptor>k__BackingField",descriptor);
  actorIds.Add(unit,Guid.NewGuid().ToString("N"));
  return unit;
 }
 static object State(object unit){return Get(Get(unit,"<Descriptor>k__BackingField"),"State");}
 // The native condition counter itself: AddCondition also raises UI status effects the detached process lacks.
 static void Condition(object unit,string name,bool on){
  ((sbyte[])Get(State(unit),"m_Conditions"))[(int)Enum.Parse(conditionType,name)]=(sbyte)(on?1:0);
 }
 static void ProbeCapability(Assembly mod,Assembly native){
  var reader=mod.GetType("KingmakerMountedCombat.Integration.NativeActorCapability",true);
  var policy=mod.GetType("KingmakerMountedCombat.Domain.MountedActorCapabilityPolicy",true);
  Func<object,bool,string> admission=(unit,turnBased)=>policy.GetMethod("AdmissionBlock",F).Invoke(null,
   new[]{reader.GetMethod("Read",F).Invoke(null,new[]{unit}),turnBased}).ToString();
  Func<object,string> execution=unit=>policy.GetMethod("ExecutionBlock",F).Invoke(null,
   new[]{reader.GetMethod("Read",F).Invoke(null,new[]{unit})}).ToString();
  var rider=Actor();var mount=Actor();
  Check(admission(rider,true)=="None"&&admission(rider,false)=="None"&&execution(rider)=="None",
   "a healthy genuine actor is admitted in both modes and at the execution boundary");
  foreach(var name in new[]{"Dazed","Stunned","CantAct","CanNotAttack"}){
   Condition(rider,name,true);
   Check(admission(rider,true)==name&&execution(rider)==name,
    "genuine "+name+" refuses a turn-based order and ends a started routine");
   Check(admission(rider,false)=="None","genuine "+name+" leaves a real-time order to the native pending behavior");
   Check(admission(mount,true)=="None"&&execution(mount)=="None",
    "the rider's "+name+" does not reach the mount's own attack");
   Condition(rider,name,false);
  }
  Condition(mount,"Stunned",true);
  Check(admission(rider,true)=="None"&&admission(mount,true)=="Stunned","the mount's condition does not reach the rider's own attack");
  Condition(mount,"Stunned",false);
  Condition(rider,"Sleeping",true);
  Check(admission(rider,true)=="Helpless","genuine native IsHelpless (sleeping) refuses a turn-based order");
  Condition(rider,"Sleeping",false);
  stateType.GetProperty("LifeState",F).SetValue(State(rider),Enum.Parse(native.GetType("Kingmaker.UnitLogic.UnitLifeState",true),"Unconscious"),null);
  Check(admission(rider,true)=="Helpless","genuine native IsHelpless (unconscious) refuses a turn-based order");
  stateType.GetProperty("LifeState",F).SetValue(State(rider),Enum.Parse(native.GetType("Kingmaker.UnitLogic.UnitLifeState",true),"Conscious"),null);
  // The action budget and movement are other gates' questions: these conditions are not capability.
  foreach(var name in new[]{"Nauseated","Staggered","CantMove","Slowed","Entangled","Prone","Shaken","Confusion"}){
   Condition(rider,name,true);
   Check(admission(rider,true)=="None"&&execution(rider)=="None",name+" is not read as an inability to act");
   Condition(rider,name,false);
  }
  Check(admission(null,true)=="None"&&admission(Blank(unitType),true)=="None","an unreadable actor refuses nothing");
  Condition(rider,"Dazed",true);
  var described=(string)reader.GetMethod("Describe",F).Invoke(null,new[]{rider,null});
  Check(described.Contains("block=Dazed")&&described.Contains("dazed=True")&&described.Contains("canNotAttack=False")&&described.Contains("nativeCanAct="),
   "the terminal diagnostic records the condition facts and survives unavailable engine readings: "+described);
  Condition(rider,"Dazed",false);
 }

 // ---- The production controller's terminal handling -------------------------------------------------------------
 sealed class Fixture {
  internal object Controller,Intent,Rider,Mount,Target,Turn;internal Recorder Log;
  internal Type CommandType,OutcomeType,ActionKind;
  internal bool HasIntent{get{return (bool)Call(Controller,"get_HasStockAttackIntent");}}
  internal long Generation{get{return (long)Call(Intent,"get_Generation");}}
  internal string Feedback{get{return (string)Call(Controller,"get_LastFeedback");}}
  internal long Counter(string name){return (long)Call(Controller,"get_"+name);}
  // What TryRouteMountedStockAttack records for an admitted click (automatic: an admitted brain proposal).
  internal void Order(bool automatic){Order(automatic,false);}
  internal void Order(bool automatic,bool mountActor){
   Call(Intent,"Begin",Target,automatic?null:Turn,mountActor,true,Rider,Mount,null,null,2,automatic);
  }
  // Known issues Sprint 6 (A01): the controller's own same-target click decision. A build from before Sprint 6 has no
  // ContinuesStockIntent; its TryRouteMountedStockAttack asked CanContinue alone, which is what it decides here.
  internal bool ClickContinues(bool mountActor){return ClickContinues(mountActor,false);}
  // A real-time click carries no turn, like the brain's own (automatic) order.
  internal bool ClickContinues(bool mountActor,bool realTime){
   var turn=realTime?null:Turn;
   var observe=Controller.GetType().GetMethod("ObserveStockClick",F);
   if(observe!=null)return observe.Invoke(Controller,new object[]{Target,turn,mountActor,Rider,Mount,null,null,2}).ToString()=="Continue";
   var method=Controller.GetType().GetMethod("ContinuesStockIntent",F);
   if(method!=null)return (bool)method.Invoke(Controller,new object[]{Target,turn,mountActor,Rider,Mount,null,null,2});
   return (bool)Call(Intent,"CanContinue",Target,turn,mountActor,Rider,Mount,null,null,2);
  }
  // What TryDriveStockAttackIntent records for an accepted dispatch.
  internal object Dispatch(){return Dispatch(false);}
  internal object Dispatch(bool mountActor){
   var command=Blank(CommandType);
   Set(command,"action",Enum.Parse(ActionKind,mountActor?"MountPrimaryNatural":"RiderRanged"));
   Set(command,"<IsFinished>k__BackingField",true);
   Set(Controller,"stockIntentCommand",command);Set(Controller,"activeCommand",command);
   Call(Intent,"ObserveDispatch",mountActor);
   return command;
  }
  internal object Outcome(string result,string reason,string transaction,int started,bool rule,int delivered,int planned,bool tail,string block){
   var outcome=Activator.CreateInstance(OutcomeType,true);
   Offer(outcome,"Action",Enum.Parse(ActionKind,"RiderRanged"));
   Offer(outcome,"ActorId",actorIds[Rider]);Offer(outcome,"TargetId",actorIds[Target]);
   Offer(outcome,"Result",result);Offer(outcome,"TerminalReason",reason);
   Offer(outcome,"ChildAttackStartCount",started);Offer(outcome,"NativeAttackRuleObserved",rule);
   Offer(outcome,"NativeCompletedAttackCount",delivered);Offer(outcome,"NativePlannedAttackCount",planned);
   Offer(outcome,"NativeFullAttack",planned>1);Offer(outcome,"NativeRangedTailTermination",tail);
   Offer(outcome,"TransactionState",transaction);Offer(outcome,"TransactionCompleted",transaction=="Completed");
   Offer(outcome,"NativeCommandStarted",started!=0);Offer(outcome,"ActionActorName","Royon");
   Offer(outcome,"ActorCapabilityBlockAtTerminal",block);
   Offer(outcome,"ActorCapabilityAtTerminal","block="+block+";dazed="+(block=="Dazed")+";turnBased=True");
   return outcome;
  }
  // The signature in the player's log: native Success, cancelled transaction, no sequence start, no attack rule.
  internal object Unexecuted(){return Outcome("Success","Success","Cancelled",0,false,0,3,false,"Dazed");}
  internal object Completed(int attacks){return Outcome("Success","completed","Completed",1,true,attacks,attacks,false,"None");}
  // Known issues Sprint 6 (A02): an outcome whose end carries a recorded cause (ignored by a build without causes).
  internal object Ended(string result,string reason,int started,string block,string cause){
   var outcome=Outcome(result,reason,"Cancelled",started,false,0,3,false,block);Offer(outcome,"EndCause",cause);return outcome;
  }
  // Review of 2ae4cc8b: the approach's own evidence, and the block recorded at the capability boundary.
  internal object Approached(string result,string reason,string block,string cause,bool required,int moves){
   var outcome=Ended(result,reason,0,block,cause);Offer(outcome,"ApproachRequiredAtStart",required);Offer(outcome,"DelegatedMoveStartCount",moves);return outcome;
  }
  internal object Caused(string recorded,string snapshot){
   var outcome=Ended("Interrupt","Action actor cannot attack: "+recorded,0,snapshot,"ActorCapability");Offer(outcome,"EndCauseCapabilityBlock",recorded);return outcome;
  }
  internal void Terminal(object command,object outcome){Call(Controller,"HandleCommandTerminal",command,outcome);}
  // Update() ends in TryDriveStockAttackIntent, the only dispatcher of a retained order. With no order it returns at
  // its first guard; with one it goes on to read the live combat mode, which this detached process does not have.
  internal void LaterUpdates(int count){for(var i=0;i<count;i++)Call(Controller,"TryDriveStockAttackIntent");}
  internal bool DispatcherProceeds(){
   try{Call(Controller,"TryDriveStockAttackIntent");return false;}
   catch(TypeInitializationException){return true;}
  }
 }
 static Fixture NewFixture(Assembly mod,Assembly native){
  var fixture=new Fixture();
  var loggerType=mod.GetType("KingmakerMountedCombat.Logging.IModLogger",true);
  fixture.Log=new Recorder(loggerType);var logger=fixture.Log.GetTransparentProxy();
  var serviceType=mod.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
  var registryType=mod.GetType("KingmakerMountedCombat.Domain.MountedPairRegistry"+(char)96+"2",true).MakeGenericType(unitType,serviceType);
  var ids=Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(unitType,typeof(string)),
   typeof(MountedAttackStallProbe).GetMethod("ActorId",F).MakeGenericMethod(unitType));
  var registry=Activator.CreateInstance(registryType,F,null,new object[]{ids},null);
  var service=Activator.CreateInstance(serviceType,F,null,new[]{logger,
   Activator.CreateInstance(mod.GetTypes().Single(t=>t.Name=="DiagnosticSettings")),registry},null);
  fixture.Rider=Actor();fixture.Mount=Actor();fixture.Target=Actor();
  fixture.Turn=Blank(native.GetType("TurnBased.Controllers.TurnController",true));
  var runtime=Get(service,"runtime");Set(runtime,"rider",fixture.Rider);Set(runtime,"mount",fixture.Mount);
  var controllerType=mod.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true);
  var controller=Blank(controllerType);
  Set(controller,"logger",logger);Set(controller,"relationship",service);
  Set(controller,"pairedCommandScheduler",Blank(mod.GetType("KingmakerMountedCombat.Integration.MountedPairCommandScheduler",true)));
  fixture.Intent=Activator.CreateInstance(Field(controllerType,"stockIntent").FieldType);
  Set(controller,"stockIntent",fixture.Intent);
  Set(controller,"automaticRuns",Activator.CreateInstance(Field(controllerType,"automaticRuns").FieldType));
  Set(controller,"<LastRejectionCodes>k__BackingField",
   Array.CreateInstance(mod.GetType("KingmakerMountedCombat.Domain.MountedCombatRejectionCode",true),0));
  Set(controller,"<LastFeedback>k__BackingField","Mounted combat is idle.");
  fixture.Controller=controller;
  fixture.CommandType=mod.GetType("KingmakerMountedCombat.Integration.MountedPairAttackCommand",true);
  fixture.OutcomeType=mod.GetType("KingmakerMountedCombat.Integration.MountedPairAttackOutcome",true);
  fixture.ActionKind=mod.GetType("KingmakerMountedCombat.Domain.MountedCombatActionKind",true);
  return fixture;
 }
 const string Retirement="Mounted attack intent retired without execution";
 const string Completion="Mounted pair attack completed.";

 // The central regression: a retained player attack whose command native ended with Success and no executed attack.
 static void ProbeUnexecutedTerminal(Assembly mod,Assembly native,Sink warnings){
  var f=NewFixture(mod,native);
  f.Order(false);var generation=f.Generation;var command=f.Dispatch();
  Check(f.HasIntent,"the player's attack order is retained while its command runs");
  f.Terminal(command,f.Unexecuted());
  Check(!f.HasIntent,"native Success without an executed attack retires the retained player order");
  Check(Get(f.Controller,"stockIntentCommand")==null&&Get(f.Controller,"activeCommand")==null,
   "the finished command is released with its order");
  var logged=f.Log.Calls.Count;var warned=warnings.Calls.Count;
  f.LaterUpdates(500);
  Check(!f.HasIntent&&Get(f.Controller,"activeCommand")==null&&Get(f.Controller,"stockIntentCommand")==null&&
   f.Counter("StockAttackRiderDispatchCount")==0&&f.Counter("StockAttackMountDispatchCount")==0,
   "500 later updates find no order to dispatch: the dispatcher returns at its first guard");
  Check(f.Log.Calls.Count==logged&&warnings.Calls.Count==warned,"later updates add no log line and no warning");
  Check(f.Feedback!=Completion&&f.Feedback=="Royon cannot act right now (dazed). The attack was not performed.",
   "an unexecuted attack is not presented as completed: "+f.Feedback);
  Check(f.Log.Count(Retirement)==1&&f.Counter("StockAttackUnexecutedRetirementCount")==1&&f.Counter("StockAttackIntentCancelCount")==1,
   "one retirement is recorded once");
  var record=f.Log.Single(Retirement);
  Check(new[]{"origin=player","generation="+generation,"action=RiderRanged","actorId="+actorIds[f.Rider],"targetId="+actorIds[f.Target],
    "nativeResult=Success","transaction=Cancelled","started=False","sequenceStarted=False","attackRule=False","attacks=0/3",
    "capability={block=Dazed"}.All(record.Contains),"the transition record carries intent, actor and progress facts: "+record);
  Check(warnings.Count("Royon cannot act right now (dazed)")==1,"the player is told once");
  var terminal=f.Log.Single("Mounted combat command terminal");
  Check(terminal.Contains("result=Success")&&terminal.Contains("transaction=Cancelled")&&terminal.Contains("terminal=EndedWithoutExecution")&&
   !terminal.Contains(Completion),"the terminal record keeps the raw native result beside the transaction's own state");
  // The condition has ended: the same target, a new click.
  f.Order(false);
  Check(f.HasIntent&&f.Generation>generation&&(string)Call(f.Intent,"GetRejectionFeedback","refused")=="refused",
   "a fresh order is admitted as a new generation with no stale suppression or history");
  var next=f.Dispatch();f.Terminal(next,f.Completed(3));
  Check(f.HasIntent&&f.Feedback==Completion&&f.Counter("StockAttackUnexecutedRetirementCount")==1&&f.Log.Count(Retirement)==1,
   "the fresh order's executed full attack completes and keeps its order");
  // The contrast that gives the 500 quiet updates their meaning: a retained order does reach the dispatch path.
  Check(f.DispatcherProceeds(),"a retained order takes the dispatcher past its guard toward the next dispatch");
 }

 static void ProbeHealthyAndNeighbouringTerminals(Assembly mod,Assembly native,Sink warnings){
  var warned=warnings.Calls.Count;
  // A full attack, a single attack; a miss or a zero-damage hit completes the same transaction.
  foreach(var attacks in new[]{3,1}){
   var f=NewFixture(mod,native);f.Order(false);var generation=f.Generation;var command=f.Dispatch();
   f.Terminal(command,f.Completed(attacks));
   Check(f.HasIntent&&f.Generation==generation&&f.Feedback==Completion&&f.Log.Count(Retirement)==0,
    "an executed "+attacks+"-attack routine completes and keeps the same order generation");
   Check(Get(f.Controller,"stockIntentCommand")==null&&Get(f.Controller,"activeCommand")==null,
    "a completed command is released while its order waits for the next native budget");
  }
  {
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();
   f.Terminal(command,f.Outcome("Interrupt","Interrupt","Cancelled",1,true,3,4,true,"None"));
   Check(f.HasIntent&&f.Feedback=="Ranged attacks completed; remaining melee attacks are out of reach."&&f.Log.Count(Retirement)==0,
    "the ranged tail of a mixed routine keeps its repeat order");
  }
  // Stop, retarget and a target that dies mid-routine all end the command with a non-success native result.
  foreach(var delivered in new[]{0,1}){
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();
   f.Terminal(command,f.Outcome("Interrupt","Interrupt","Cancelled",delivered,delivered>0,delivered,3,false,"None"));
   Check(!f.HasIntent&&f.Log.Count(Retirement)==0&&f.Counter("StockAttackUnexecutedRetirementCount")==0&&
    ((string)Call(f.Controller,"get_LastStockAttackObservation")).StartsWith("intent-cancel-terminal;",StringComparison.Ordinal),
    "an interrupted routine ("+delivered+" delivered) keeps its existing retirement");
  }
  {
   // A terminal of a command the current order does not own (the order was replaced meanwhile).
   var f=NewFixture(mod,native);f.Order(false);var generation=f.Generation;var current=f.Dispatch();
   var stale=Blank(f.CommandType);Set(stale,"action",Enum.Parse(f.ActionKind,"RiderRanged"));
   f.Terminal(stale,f.Unexecuted());
   Check(f.HasIntent&&f.Generation==generation&&ReferenceEquals(Get(f.Controller,"stockIntentCommand"),current)&&f.Log.Count(Retirement)==0,
    "an unexecuted terminal of another command leaves the current order and its command alone");
  }
  {
   // An explicit mounted action holds no stock order at all.
   var f=NewFixture(mod,native);var explicitCommand=Blank(f.CommandType);
   Set(explicitCommand,"action",Enum.Parse(f.ActionKind,"RiderRanged"));Set(f.Controller,"activeCommand",explicitCommand);
   f.Terminal(explicitCommand,f.Unexecuted());
   Check(!f.HasIntent&&f.Feedback!=Completion&&f.Log.Count(Retirement)==0&&Get(f.Controller,"activeCommand")==null,
    "an unexecuted explicit action is not presented as completed and retires nothing");
  }
  Check(warnings.Calls.Count==warned,"healthy, interrupted, stale and explicit terminals raise no stock warning");
  {
   // Native dropped the unstarted order for a cause that is not the actor's condition (initiative, an animation).
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();
   f.Terminal(command,f.Outcome("Success","Success","Cancelled",0,false,0,1,false,"None"));
   f.LaterUpdates(100);
   Check(!f.HasIntent&&f.Log.Count(Retirement)==1&&f.Feedback=="Royon could not perform the attack; the order was dropped."&&
    f.Counter("StockAttackRiderDispatchCount")==0&&!f.DispatcherProceeds(),
    "an unexecuted order without an identified condition is retired and recorded once, never completed or repeated");
   Check(warnings.Calls.Count==warned+1&&warnings.Count("Royon could not perform the attack; the order was dropped.")==1,
    "the player is told once that the order was dropped");
   warned=warnings.Calls.Count;
  }
  {
   var f=NewFixture(mod,native);f.Order(true);var command=f.Dispatch();
   f.Terminal(command,f.Unexecuted());
   Check(!f.HasIntent&&f.Log.Single(Retirement).Contains("origin=automatic")&&warnings.Calls.Count==warned,
    "an automatic order retires the same way without a player warning");
  }
 }

 // ---- Known issues Sprint 6 -----------------------------------------------------------------------------------------
 static int sectionFailures;
 static void Section(string name,Action body){
  try{body();}
  catch(Exception error){
   var inner=error;while(inner is TargetInvocationException&&inner.InnerException!=null)inner=inner.InnerException;
   sectionFailures++;Console.WriteLine("FAIL SECTION "+name+": "+inner.Message);
  }
 }
 const string NoStandard="Royon has no Standard action left.";

 // A01. A same-target click after the dispatched command ended, before the next Update.
 static void ProbeFreshClick(Assembly mod,Assembly native){
  foreach(var shape in new[]{"rider single attack","rider full attack","mount attack","ranged tail"}){
   var mountActor=shape=="mount attack";
   var f=NewFixture(mod,native);f.Order(false,mountActor);var generation=f.Generation;var command=f.Dispatch(mountActor);
   Set(command,"<IsFinished>k__BackingField",false);
   Check(f.ClickContinues(mountActor),shape+": a click while the command runs continues it");
   Set(command,"<IsFinished>k__BackingField",true);
   f.Terminal(command,shape=="ranged tail"?f.Outcome("Interrupt","Interrupt","Cancelled",1,true,2,3,true,"None"):f.Completed(shape=="rider full attack"?3:1));
   Check(f.HasIntent&&f.Generation==generation&&Get(f.Controller,"stockIntentCommand")==null,shape+": setup, the executed routine keeps its order and releases its command");
   Check(!f.ClickContinues(mountActor),shape+": the exact same-target click after the command ended is a fresh request, not continuation");
   // The replacement path the controller then takes: Cancel (which clears the order) and Begin.
   Call(f.Controller,"ClearStockAttackIntent","new stock hostile attack",true);f.Order(false,mountActor);
   Check(f.Generation>generation&&(string)Call(f.Intent,"GetRejectionFeedback",NoStandard)==NoStandard,
    shape+": the fresh request is a new generation with its own answer for a spent actor");
  }
  {
   var f=NewFixture(mod,native);f.Order(true);var command=f.Dispatch();Set(command,"<IsFinished>k__BackingField",false);
   Check(f.ClickContinues(false,true),"a real-time click on the brain's live command continues and promotes it");
   Call(f.Intent,"PromoteToPlayer");Set(command,"<IsFinished>k__BackingField",true);f.Terminal(command,f.Completed(1));
   Check(!f.ClickContinues(false,true),"once the promoted command ended, a later click is fresh");
  }
  {
   var a=NewFixture(mod,native);var b=NewFixture(mod,native);
   a.Order(false);var ca=a.Dispatch();a.Terminal(ca,a.Completed(1));
   b.Order(false);var cb=b.Dispatch();Set(cb,"<IsFinished>k__BackingField",false);
   Check(!a.ClickContinues(false)&&b.ClickContinues(false),"one pair's terminal never changes another pair's live continuation");
  }
  {
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();f.Terminal(command,f.Completed(1));
   Call(f.Controller,"ClearStockAttackIntent","stop or hold",true);
   Check(!f.HasIntent&&!f.ClickContinues(false),"after a boundary clears the order nothing continues and nothing about freshness is stored");
  }
 }

 // A02. The capability snapshot explains a terminal only where it is the cause.
 static void ProbeTerminalCause(Assembly mod,Assembly native,Sink warnings){
  var warned=warnings.Calls.Count;
  Func<string,string,int,string,string,string> end=(result,reason,started,block,cause)=>{
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();
   var recorded=cause.Contains(":")?cause.Split(':')[1]:null;cause=cause.Split(':')[0];
   var outcome=f.Ended(result,reason,started,block,cause);if(recorded!=null)Offer(outcome,"EndCauseCapabilityBlock",recorded);
   f.Terminal(command,outcome);
   Check(!f.HasIntent&&Get(f.Controller,"activeCommand")==null,"the "+cause+" terminal retires its order");return f.Feedback;
  };
  var stop=end("Interrupt","Interrupt",1,"Dazed","Cancelled");
  Check(stop=="Mounted pair attack cancelled.","Stop while dazed is a cancellation, not a daze: "+stop);
  var replaced=end("Interrupt","Interrupt",1,"Stunned","Replaced");
  Check(replaced=="Mounted pair attack replaced by a newer order.","a replacing order while stunned is a replacement: "+replaced);
  var boundary=end("Interrupt","Interrupt",0,"Helpless","Cancelled");
  Check(boundary=="Mounted pair attack cancelled.","a dismount, load, area or combat boundary is not described as helplessness: "+boundary);
  var fault=end("Fail","Fail",1,"Stunned","Fault");
  Check(fault.IndexOf("cannot act",StringComparison.Ordinal)<0,"a fault stays the cause beside a stunned actor: "+fault);
  var target=end("Interrupt","Expected target invalidation: target died",1,"Dazed","TargetInvalid");
  Check(target=="Mounted rider melee ended: the exact target became invalid before completion.","target invalidation stays the cause: "+target);
  // The capability boundary always records the exact block it read (review of 2ae4cc8b, finding 3).
  var approach=end("Interrupt","Action actor cannot attack: Dazed",0,"Dazed","ActorCapability:Dazed");
  Check(approach=="Royon cannot act right now (dazed). The attack was not performed.","a daze during the approach keeps its condition text: "+approach);
  var banned=end("Interrupt","Interrupt",0,"CanNotAttack","Unrecorded");
  Check(banned=="Royon cannot attack right now. The attack was not performed.","native's attack-ban interruption keeps its condition text: "+banned);
  Check(warnings.Calls.Count==warned,"failed terminals raise no stock warning");
 }

 static void ProbeLoggerFailure(Assembly mod,Assembly native){
  {
   // A failing logger: the terminal still retires its order and releases its command, and nothing escapes.
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();f.Log.Throws=true;
   Exception escaped=null;
   try{f.Terminal(command,f.Unexecuted());}catch(Exception e){escaped=e;}
   f.Log.Throws=false;
   Check(escaped==null&&!f.HasIntent&&Get(f.Controller,"activeCommand")==null&&Get(f.Controller,"stockIntentCommand")==null,
    "a failing logger does not stop the terminal's retirement or ownership release"+(escaped!=null?": "+escaped.GetType().Name:""));
  }
 }

 // A04. A capability-caused automatic terminal is not a failed approach; the automatic admission reads CanNotAttack.
 // A05 (review of 2ae4cc8b, finding 1): only a typed, evidenced failed delegated approach records the target.
 static void ProbeAutomaticCapability(Assembly mod,Assembly native){
  Func<object,object> marked=outcome=>{
   var f=NewFixture(mod,native);f.Order(true);var command=f.Dispatch();f.Terminal(command,outcome);
   return Get(f.Controller,"automaticApproachFailedTarget");
  };
  var any=NewFixture(mod,native);
  foreach(var reason in new[]{"target died","target became unconscious","target despawned","target hostility changed","target no longer attackable"})
   Check(marked(any.Approached("Interrupt","Expected target invalidation: "+reason,"None","TargetInvalid",true,1))==null,"2-6: "+reason+" records no unreachable mark");
  Check(marked(any.Approached("Interrupt","Interrupt","None","Unrecorded",false,0))==null,"7: an in-range unrecorded interruption with no delegated move records nothing");
  Check(marked(any.Approached("Interrupt","Interrupt","None","Unrecorded",true,1))==null,"7: an unrecorded end after an approach is not evidence of a failed approach");
  Check(marked(any.Approached("Interrupt","Action actor cannot attack: Dazed","Dazed","ActorCapability",true,1))==null,"8: a capability end records nothing");
  Check(marked(any.Approached("Interrupt","Interrupt","None","Cancelled",true,1))==null,"9: a cancellation records nothing");
  Check(marked(any.Approached("Interrupt","Interrupt","None","Replaced",true,1))==null,"10: a replacement records nothing");
  Check(marked(any.Approached("Fail","InvalidOperationException: fault","None","Fault",true,1))==null,"11: a fault records nothing");
  Check(marked(any.Completed(1))==null&&marked(any.Outcome("Interrupt","Interrupt","Cancelled",1,true,2,3,true,"None"))==null,
   "12: a successful attack and a ranged tail record nothing");
  Check(marked(any.Approached("Interrupt","InvalidOperationException: Mounted pair command exhausted its bounded repath allowance.","None","ApproachFailed",true,2))!=null,
   "1: a genuine failed delegated approach records its target for the rider's brain");
 }

 // A02 (review of 2ae4cc8b, finding 3): the block recorded at the capability boundary stays the cause.
 static void ProbeCausalBlock(Assembly mod,Assembly native){
  Func<object,string> feedback=outcome=>{var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();f.Terminal(command,outcome);return f.Feedback;};
  var any=NewFixture(mod,native);
  var cleared=feedback(any.Caused("Dazed","None"));
  Check(cleared=="Royon cannot act right now (dazed). The attack was not performed.","1: a recorded daze stays the cause when the snapshot clears: "+cleared);
  var changed=feedback(any.Caused("Dazed","CanNotAttack"));
  Check(changed=="Royon cannot act right now (dazed). The attack was not performed.","2: a recorded daze is not replaced by a later attack ban: "+changed);
  var banned=feedback(any.Caused("CanNotAttack","None"));
  Check(banned=="Royon cannot attack right now. The attack was not performed.","3: a recorded attack ban stays the cause when the snapshot clears: "+banned);
 }

 // Review of 2ae4cc8b, finding 2: a start or tick fault records Fault before any diagnostic, a throwing logger and a
 // failing capability reading cannot stop the command's end, and the terminal is delivered once.
 static void ProbeFaultDelivery(Assembly mod,Assembly native){
  foreach(var phase in new[]{"OnStart","OnTick"}){
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();Set(command,"<IsFinished>k__BackingField",false);
   Set(command,"logger",f.Log.GetTransparentProxy());
   Set(command,"transaction",Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Domain.MountedCombatTransaction",true),true));
   var terminalField=Field(f.CommandType,"terminal");
   Set(command,"terminal",Delegate.CreateDelegate(terminalField.FieldType,f.Controller,f.Controller.GetType().GetMethod("HandleCommandTerminal",F)));
   Set(f.Controller,"<LastOutcome>k__BackingField",null);
   f.Log.Throws=true;
   try{f.CommandType.GetMethod(phase,F).Invoke(command,null);}catch(Exception){}
   f.Log.Throws=false;
   var outcome=Get(f.Controller,"<LastOutcome>k__BackingField");
   Check(outcome!=null,phase+": a fault with a throwing logger and an unreadable actor still delivers the terminal");
   var cause=outcome.GetType().GetProperty("EndCause",F);
   Check(cause!=null&&cause.GetValue(outcome,null).ToString()=="Fault",phase+": the fault is recorded as the cause before any diagnostic");
   Check(!f.HasIntent&&Get(f.Controller,"activeCommand")==null,phase+": the faulted command's order retires and its ownership is released");
  }
 }

 // Review of 2ae4cc8b, finding 5: only the same request after its command ended counts as fresh.
 static void ProbeFreshCounter(Assembly mod,Assembly native){
  var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();f.Terminal(command,f.Completed(1));
  var observe=f.Controller.GetType().GetMethod("ObserveStockClick",F);
  Check(observe!=null,"the controller classifies each same-actor click once");
  Func<object,int,object,string> click=(target,action,weapon)=>observe.Invoke(f.Controller,new object[]{target,f.Turn,false,f.Rider,f.Mount,weapon,null,action}).ToString();
  Check(click(f.Target,2,null)=="FreshAfterTerminal"&&f.Counter("StockAttackFreshAfterTerminalCount")==1,"the same request after its command ended counts once");
  Check(click(Actor(),2,null)=="NewRequest"&&click(f.Target,9,null)=="NewRequest"&&click(f.Target,2,new object())=="NewRequest"&&
   f.Counter("StockAttackFreshAfterTerminalCount")==1,"a retarget, another action or another weapon is a new request and is not counted");
 }

 // Review of 2ae4cc8b, finding 4: the product transpiler, run against the installed public UnitCommands.Run, replaces its
 // one internal Run call with the scoped hook and nothing else.
 static void ProbeNativeRunScope(Assembly mod,Assembly native,string managed){
  MethodInfo read=null;
  foreach(var m in Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony.dll")).GetType("HarmonyLib.PatchProcessor",true).GetMethods(BindingFlags.Public|BindingFlags.Static))
   if(m.Name=="GetOriginalInstructions"&&m.GetParameters().Length==2&&!m.GetParameters()[1].ParameterType.IsByRef)read=m;
  var commandsType=native.GetType("Kingmaker.UnitLogic.Commands.UnitCommands",true);
  var run=commandsType.GetMethod("Run",F,null,new[]{native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true)},null);
  var instruction=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony12.dll")).GetType("Harmony12.CodeInstruction",true);
  var before=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(instruction));
  var snapshot=new List<object[]>();
  foreach(var raw in (System.Collections.IEnumerable)read.Invoke(null,new object[]{run,null})){
   var op=raw.GetType().GetField("opcode").GetValue(raw);var operand=raw.GetType().GetField("operand").GetValue(raw);
   before.Add(Activator.CreateInstance(instruction,new[]{op,operand}));snapshot.Add(new[]{op,operand});
  }
  var methods=mod.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true).GetNestedType("PatchMethods",F);
  var transpiler=methods.GetMethod("NativeRunScopeTranspiler",F);
  Check(transpiler!=null,"the public Run has a scope transpiler");
  var after=((System.Collections.IEnumerable)transpiler.Invoke(null,new object[]{before})).Cast<object>().ToList();
  var changed=new List<int>();
  for(var i=0;i<after.Count;i++)if(!Equals(instruction.GetField("operand").GetValue(after[i]),snapshot[i][1]))changed.Add(i);
  var hook=instruction.GetField("operand").GetValue(after[changed.Single()]) as MethodInfo;
  Check(after.Count==snapshot.Count&&changed.Count==1&&hook!=null&&hook.Name=="ScopedRun"&&hook.DeclaringType.Name=="MountedNativeRunScope"&&
   ((MethodBase)snapshot[changed[0]][1]).GetParameters().Length==3,"the transpiler replaces exactly the one internal Run call with the scoped hook");
  var scoped=CalledMembers(hook);
  Check(scoped.Contains("Open")&&scoped.Contains("Close")&&hook.GetMethodBody().ExceptionHandlingClauses.Any(c=>c.Flags==ExceptionHandlingClauseOptions.Finally),
   "the hook opens its own token and closes it in a finally");
  Check(CalledMembers(methods.GetMethod("UnitCommandRunPrefix",F)).All(n=>n!="OpenNativeRun")&&CalledMembers(methods.GetMethod("UnitCommandRunPostfix",F)).All(n=>n!="CloseNativeRun"),
   "no scope is opened or closed through pair ownership in the prefix or postfix");
 }

 static void ProbeAutomaticAdmission(Assembly mod){
  var policy=mod.GetType("KingmakerMountedCombat.Domain.MountedAutomaticAttackPolicy",true);
  var factsType=mod.GetType("KingmakerMountedCombat.Domain.MountedAutomaticProposalFacts",true);
  Func<bool,string> decide=cannotAttack=>{
   var facts=Activator.CreateInstance(factsType);
   foreach(var name in new[]{"AiOrigin","ExactMountedPair","OwnerIsRider","RiderDirectlyControllable","RiderAiEnabled","RiderBrainActive","TargetInRiderReach","TargetValid"})
    factsType.GetField(name).SetValue(facts,true);
   var field=factsType.GetField("RiderCannotAttack");if(field!=null)field.SetValue(facts,cannotAttack);
   var args=new object[]{facts,null};var decision=policy.GetMethod("Decide").Invoke(null,args).ToString();
   return decision+(args[1]!=null?":"+args[1]:"");
  };
  var admitted=0;for(var proposal=0;proposal<30;proposal++)if(decide(true)=="Admit")admitted++;
  Check(admitted==0,"thirty brain proposals for a rider that cannot attack create no automatic intent (admitted "+admitted+")");
  Check(decide(false)=="Admit","the next proposal after the condition clears is admitted");
 }

 // A03. The installed consumers of the condition the decoded Fascinate buffs carry (AddCondition: Dazed): the component
 // adds its condition to the owner's UnitState, CanAct reads IsAble, and IsAble is false under Dazed. The blueprint
 // decoding itself is recorded in the Sprint 6 handoff; this probe reads only installed IL.
 static void ProbeFascinateCondition(Assembly native){
  var component=native.GetType("Kingmaker.UnitLogic.FactLogic.AddCondition",true);
  Check(component.GetField("Condition").FieldType.FullName=="Kingmaker.UnitLogic.UnitCondition"&&
   CalledMembers(component.GetMethod("OnTurnOn",F)).Contains("AddCondition"),"A03: native AddCondition adds its UnitCondition to the owner's state when the fact turns on");
  var state=native.GetType("Kingmaker.UnitLogic.UnitState",true);
  Check(CalledMembers(state.GetProperty("CanAct",F).GetGetMethod(true)).Contains("get_IsAble"),"A03: native CanAct requires IsAble");
  var able=state.GetProperty("IsAble",F).GetGetMethod(true).GetMethodBody().GetILAsByteArray();
  var dazed=(int)Enum.Parse(native.GetType("Kingmaker.UnitLogic.UnitCondition",true),"Dazed");
  var loadsDazed=false;
  for(var i=0;i+1<able.Length;i++){
   if(able[i]==0x1F&&able[i+1]==dazed)loadsDazed=true;
   if(dazed<=8&&able[i]==(byte)(0x16+dazed))loadsDazed=true;
  }
  Check(loadsDazed&&CalledMembers(state.GetProperty("IsAble",F).GetGetMethod(true)).Contains("HasCondition"),"A03: native IsAble is false while the unit is Dazed");
 }

 // Wiring the decisions above rely on, read from the compiled product.
 static void ProbeSprint6Wiring(Assembly mod){
  var controller=mod.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true);
  Func<string,HashSet<string>> calls=n=>CalledMembers(controller.GetMethod(n,F));
  Check(calls("TryRouteMountedStockAttack").Contains("ObserveStockClick")&&calls("ObserveStockClick").Contains("ClassifyClick"),
   "A01: the click route classifies the click against the live dispatched command");
  var terminal=calls("HandleCommandTerminal");
  Check(terminal.Contains("Resolve")&&terminal.Contains("CausalCapability")&&terminal.Contains("RecordsAutomaticApproachFailure")&&terminal.Contains("EndedInsideReplacingRun"),
   "A02/A04: the terminal resolves its cause before it reads the capability snapshot or records an unreachable target");
  Check(calls("Cancel").Contains("InterruptOwnedCommand")&&calls("TryRetireStockIntent").Contains("InterruptOwnedCommand")&&
   calls("SweepOrphanedAutomaticCommand").Contains("InterruptOwnedCommand")&&CalledMembers(controller.GetMethod("InterruptOwnedCommand",F)).Contains("RecordEndCause"),
   "A02: the controller's own ends record their cause before native Interrupt");
  var command=mod.GetType("KingmakerMountedCombat.Integration.MountedPairAttackCommand",true);
  Check(CalledMembers(command.GetMethod("TerminateIfActionActorBlocked",F)).Contains("RecordEndCause"),"A02: the execution boundary records the capability cause");
  Check(CalledMembers(command.GetMethod("OnStart",F)).Contains("EndAfterFault")&&CalledMembers(command.GetMethod("OnTick",F)).Contains("EndAfterFault"),
   "Finding 2: start and tick faults end through the cause-recording fault boundary");
  Check(calls("ObserveAutomaticProposal").Contains("Read"),"A04: the automatic admission reads the rider's native condition");
  // A04's refusal is automatic-only: the player's click route never reads the capability condition (real-time player
  // orders keep their native wait, and turn-based ones their existing capability admission).
  var route=controller.GetMethod("TryRouteMountedStockAttack",F);var il=route.GetMethodBody().GetILAsByteArray();var readsCapability=false;
  for(var i=0;i+4<il.Length;i++){
   if(il[i]!=0x28&&il[i]!=0x6F)continue;
   try{var m=route.Module.ResolveMethod(BitConverter.ToInt32(il,i+1));if(m.DeclaringType.Name=="NativeActorCapability")readsCapability=true;}catch(Exception){}
  }
  Check(!readsCapability,"A04: the player's click route does not read the automatic-only capability refusal");
 }

 public static void Run(string managed,string dll){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{
   var leaf=new AssemblyName(a.Name).Name+".dll";
   foreach(var root in new[]{managed,Path.Combine(managed,"UnityModManager")}){var path=Path.Combine(root,leaf);if(File.Exists(path))return Assembly.LoadFrom(path);}return null;
  };
  var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));var mod=Assembly.LoadFrom(dll);
  Console.WriteLine("product="+dll+" mvid="+mod.ManifestModule.ModuleVersionId);
  unitType=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
  stateType=native.GetType("Kingmaker.UnitLogic.UnitState",true);
  descriptorType=native.GetType("Kingmaker.UnitLogic.UnitDescriptor",true);
  conditionType=native.GetType("Kingmaker.UnitLogic.UnitCondition",true);
  var warnings=warningSink;
  native.GetType("Kingmaker.PubSubSystem.EventBus",true).GetMethod("Subscribe",F,null,new[]{typeof(object)},null)
   .Invoke(null,new[]{WarningSubscriber(native.GetType("Kingmaker.PubSubSystem.IWarningNotificationUIHandler",true))});
  ProbeNativeContracts(native);
  // The central regression runs first so that a build without the repair fails on it.
  ProbeUnexecutedTerminal(mod,native,warnings);
  ProbeHealthyAndNeighbouringTerminals(mod,native,warnings);
  ProbeCapability(mod,native);
  // Known issues Sprint 6: each section reports its own result, so an earlier build shows every one it fails.
  Section("A01 fresh same-target click after a terminal",()=>ProbeFreshClick(mod,native));
  Section("A02 terminal cause apart from the capability snapshot",()=>ProbeTerminalCause(mod,native,warnings));
  Section("A02 a failing logger at the terminal",()=>ProbeLoggerFailure(mod,native));
  Section("A05 only a failed delegated approach marks a target",()=>ProbeAutomaticCapability(mod,native));
  Section("A02 the recorded causal capability block",()=>ProbeCausalBlock(mod,native));
  Section("Fault recording and terminal delivery",()=>ProbeFaultDelivery(mod,native));
  Section("A01 fresh-after-terminal counter",()=>ProbeFreshCounter(mod,native));
  Section("Exact native Run scope",()=>ProbeNativeRunScope(mod,native,managed));
  Section("A04 automatic admission of a rider that cannot attack",()=>ProbeAutomaticAdmission(mod));
  Section("A03 native consumers of Fascinate's condition",()=>ProbeFascinateCondition(native));
  Section("Sprint 6 product wiring",()=>ProbeSprint6Wiring(mod));
  Console.WriteLine("MOUNTED ATTACK STALL REPAIR PASS="+passed+" FAIL="+sectionFailures+"; detached, not native gameplay qualification");
  if(sectionFailures>0)throw new InvalidOperationException(sectionFailures+" Sprint 6 section(s) failed");
 }
}
'@
try { [MountedAttackStallProbe]::Run($managed,$dll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
