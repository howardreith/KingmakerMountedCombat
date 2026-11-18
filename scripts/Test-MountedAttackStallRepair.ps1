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
 // The calls of one method body in IL order, as Type.Method.
 static List<string> CallOrder(MethodBase method){
  var il=method.GetMethodBody().GetILAsByteArray();var names=new List<string>();
  for(var i=0;i+4<il.Length;i++){
   if(il[i]!=0x28&&il[i]!=0x6F)continue;
   try{var m=method.Module.ResolveMethod(BitConverter.ToInt32(il,i+1));names.Add(m.DeclaringType.Name+"."+m.Name);i+=4;}catch(Exception){}
  }
  return names;
 }
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
  // Native's unstarted turn-based force-finish of a dazed actor. Review of fa533e97 (R18): that branch is recorded where
  // native takes it, with the block read there (ignored by a build that inferred it from the snapshot).
  internal object Unexecuted(){
   var outcome=Outcome("Success","Success","Cancelled",0,false,0,3,false,"Dazed");
   Offer(outcome,"NativeTerminalBranch","TurnBasedCannotAct");Offer(outcome,"NativeTerminalCapabilityBlock","Dazed");return outcome;
  }
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
   var branch=cause.Contains("@")?cause.Split('@')[1]:null;cause=cause.Split('@')[0];
   var outcome=f.Ended(result,reason,started,block,cause);if(recorded!=null)Offer(outcome,"EndCauseCapabilityBlock",recorded);
   if(branch!=null){Offer(outcome,"NativeTerminalBranch",branch);Offer(outcome,"NativeTerminalCapabilityBlock",block);}
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
  var banned=end("Interrupt","Interrupt",0,"CanNotAttack","Unrecorded@AttackBanned");
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

 // ---- Review of fa533e97 -----------------------------------------------------------------------------------------
 // R18: a native end is a capability end only on native's exact capability branch; a later snapshot never decides.
 static void ProbeNativeCapabilityTerminals(Assembly mod,Assembly native){
  Func<object,string> feedback=outcome=>{var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();f.Terminal(command,outcome);return f.Feedback;};
  var any=NewFixture(mod,native);
  Func<string,int,string,string,string,object> ended=(result,started,snapshot,branch,branchBlock)=>{
   var outcome=any.Ended(result,result,started,snapshot,"Unrecorded");
   Offer(outcome,"NativeTerminalBranch",branch);Offer(outcome,"NativeTerminalCapabilityBlock",branchBlock);return outcome;
  };
  Func<string,string,object> unexecuted=(branch,branchBlock)=>{
   var outcome=any.Unexecuted();Offer(outcome,"NativeTerminalBranch",branch);Offer(outcome,"NativeTerminalCapabilityBlock",branchBlock);return outcome;
  };
  const string target="Mounted rider melee ended: the exact target became invalid before completion.";
  var one=feedback(unexecuted("TurnBasedCannotAct","Dazed"));
  Check(one=="Royon cannot act right now (dazed). The attack was not performed.","1: the turn-based force-finish native took for CanAct names its block: "+one);
  var two=feedback(ended("Interrupt",1,"Stunned","RunningCannotAct","Stunned"));
  Check(two=="Royon cannot act right now (stunned). The attack was not performed.","2: the running command native interrupted for CanAct names its block: "+two);
  var three=feedback(ended("Interrupt",0,"CanNotAttack","AttackBanned","CanNotAttack"));
  Check(three=="Royon cannot attack right now. The attack was not performed.","3: native's attack-ban interruption names the ban: "+three);
  var four=feedback(ended("Interrupt",1,"Dazed","TargetUnavailable","None"));
  Check(four==target,"4: a target leaving state beside a dazed actor is the target's end, not a daze: "+four);
  var five=feedback(ended("Interrupt",0,"Stunned","TargetUnavailable","None"));
  var fiveDead=feedback(ended("Interrupt",0,"Stunned","AttackTargetDead","None"));
  Check(five==target&&fiveDead==target,"5: an untargetable, stealthed or dead target beside a stunned actor is the target's end: "+five+" / "+fiveDead);
  var six=feedback(ended("Interrupt",1,"Helpless","RunningInterruptAsSoonAsPossible","None"));
  var sixUnstarted=feedback(ended("Interrupt",0,"Dazed","InterruptAsSoonAsPossible","None"));
  Check(six.IndexOf("cannot",StringComparison.Ordinal)<0&&sixUnstarted.IndexOf("cannot",StringComparison.Ordinal)<0,
   "6: InterruptAsSoonAsPossible beside a blocked actor is not a capability end: "+six+" / "+sixUnstarted);
  var seven=feedback(ended("Interrupt",1,"Dazed","None","None"));
  var sevenUnstarted=feedback(ended("Interrupt",0,"Stunned","None","None"));
  Check(seven=="Mounted pair attack ended: Interrupt."&&sevenUnstarted=="Mounted pair attack ended: Interrupt.",
   "7: a paused previous command, InterruptMove or InterruptAll beside a blocked actor stays generic: "+seven+" / "+sevenUnstarted);
  var eight=feedback(unexecuted("None","None"));
  var eightSlot=feedback(unexecuted("TurnBasedNoCommandSlot","None"));
  Check(eight=="Royon could not perform the attack; the order was dropped."&&eightSlot==eight,
   "8: a native end whose branch was not established is never given the snapshot's condition: "+eight+" / "+eightSlot);
 }

 static List<object> Harmony12Instructions(MethodBase method,string managed,Type instruction){
  MethodInfo read=null;
  foreach(var m in Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony.dll")).GetType("HarmonyLib.PatchProcessor",true).GetMethods(BindingFlags.Public|BindingFlags.Static))
   if(m.Name=="GetOriginalInstructions"&&m.GetParameters().Length==2&&!m.GetParameters()[1].ParameterType.IsByRef)read=m;
  var list=new List<object>();
  foreach(var raw in (System.Collections.IEnumerable)read.Invoke(null,new object[]{method,null})){
   var copy=Activator.CreateInstance(instruction,new[]{raw.GetType().GetField("opcode").GetValue(raw),raw.GetType().GetField("operand").GetValue(raw)});
   var labels=(System.Collections.IList)instruction.GetField("labels").GetValue(copy);
   foreach(var label in (System.Collections.IEnumerable)raw.GetType().GetField("labels").GetValue(raw))labels.Add(label);
   list.Add(copy);
  }
  return list;
 }
 static List<object> Transpile(MethodInfo transpiler,List<object> code,Type instruction){
  var typed=(System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(instruction));
  foreach(var c in code)typed.Add(c);
  var args=transpiler.GetParameters().Length==2
   ?new object[]{typed,new DynamicMethod("kmc-probe-il",typeof(void),Type.EmptyTypes,typeof(MountedAttackStallProbe).Module,true).GetILGenerator()}
   :new object[]{typed};
  return ((System.Collections.IEnumerable)transpiler.Invoke(null,args)).Cast<object>().ToList();
 }
 // The local a frame-local capture stores into (stloc after the capture) and the local its terminal loads (ldloc before it).
 static object StoredAfter(List<object> code,Type instruction,string capture){
  for(var i=0;i+1<code.Count;i++){var m=instruction.GetField("operand").GetValue(code[i]) as MethodBase;
   if(m!=null&&m.Name==capture&&instruction.GetField("opcode").GetValue(code[i+1]).ToString().StartsWith("stloc"))return instruction.GetField("operand").GetValue(code[i+1]);}
  return null;
 }
 static object LoadedBefore(List<object> code,Type instruction,string terminal){
  for(var i=1;i<code.Count;i++){var m=instruction.GetField("operand").GetValue(code[i]) as MethodBase;
   if(m!=null&&m.Name==terminal&&instruction.GetField("opcode").GetValue(code[i-1]).ToString().StartsWith("ldloc"))return instruction.GetField("operand").GetValue(code[i-1]);}
  return null;
 }
 static List<string> Calls(List<object> code,Type instruction){
  var names=new List<string>();
  foreach(var c in code){var m=instruction.GetField("operand").GetValue(c) as MethodBase;if(m!=null)names.Add(m.Name);}
  return names;
 }

 // R18: the product transpilers, run on the installed IL, keep native's calls and add exactly the captures; a real
 // Harmony12 patch of each is then constructed in this process (composed with the existing paired eligibility rewrite).
 static object nativeBranchHarmony;
 static void ProbeNativeBranchTranspilers(Assembly mod,Assembly native,string managed){
  var harmony12=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony12.dll"));
  var instruction=harmony12.GetType("Harmony12.CodeInstruction",true);
  var methods=mod.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true).GetNestedType("PatchMethods",F);
  Func<string,MethodInfo> transpiler=name=>{var m=methods.GetMethod(name,F);Check(m!=null,"the product has "+name);return m;};
  var turnBased=native.ManifestModule.ResolveMethod(0x0600911D);var tick=native.ManifestModule.ResolveMethod(0x0600911E);
  var attackBan=native.ManifestModule.ResolveMethod(0x0600266D);
  Check(turnBased.Name=="TickCommandTurnBased"&&tick.Name=="TickCommand"&&attackBan.Name=="get_ShouldBeInterrupted"&&attackBan.DeclaringType.Name=="UnitAttack",
   "the installed exact tokens are TickCommandTurnBased, TickCommand and UnitAttack.get_ShouldBeInterrupted");
  var command=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);
  Check(mod.GetType("KingmakerMountedCombat.Integration.MountedPairAttackCommand",true).GetProperty("ShouldBeInterrupted",F).GetGetMethod(true).DeclaringType.Name=="UnitAttack",
   "the pair command does not override UnitAttack.ShouldBeInterrupted");

  var tbOriginal=Harmony12Instructions(turnBased,managed,instruction);
  var paired=Transpile(transpiler("PairedEligibilityTranspiler"),tbOriginal,instruction);
  var tb=Transpile(transpiler("NativeTurnBasedTerminalTranspiler"),paired,instruction);
  var tbCalls=Calls(tb,instruction);
  var tbLocal=StoredAfter(tb,instruction,"CaptureCanAct");
  Check(tbLocal!=null&&ReferenceEquals(tbLocal,LoadedBefore(tb,instruction,"TurnBasedForceFinish")),
   "R21 turn-based: the captured canAct is kept in a local of the native invocation and passed to its own force-finish");
  Check(tb.Count==paired.Count+4&&tbCalls.Count(n=>n=="CaptureCanAct")==1&&tbCalls.Count(n=>n=="TurnBasedForceFinish")==1&&
   tbCalls.Count(n=>n=="ForceFinishForTurnBased")==1&&tbCalls.IndexOf("TurnBasedForceFinish")<tbCalls.IndexOf("ForceFinishForTurnBased")&&
   tbCalls.IndexOf("get_CanAct")<tbCalls.IndexOf("HasCooldownForCommand")&&tbCalls.IndexOf("HasCooldownForCommand")<tbCalls.IndexOf("CaptureCanAct")&&
   tbCalls.IndexOf("CaptureCanAct")<tbCalls.IndexOf("TurnBasedForceFinish")&&
   tbCalls.Contains("PairedActorEligible"),
   "turn-based: native's canAct is captured where it branches and only the first (capability) force-finish is wrapped; the approach-time force-finish and the paired rewrite stay");
  var tickOriginal=Harmony12Instructions(tick,managed,instruction);
  var t=Transpile(transpiler("NativeTickTerminalTranspiler"),tickOriginal,instruction);
  var hooks=Calls(t,instruction).Where(n=>new[]{"BeginShouldBeInterrupted","CaptureShouldBeInterrupted","InterruptAtStateSite","InterruptAsSoonAsPossible","CaptureCanAct",
   "InterruptAtRunningSite","get_ShouldBeInterrupted","Interrupt","get_CanAct"}.Contains(n)).ToArray();
  var stateLocal=StoredAfter(t,instruction,"CaptureShouldBeInterrupted");var runningLocal=StoredAfter(t,instruction,"CaptureCanAct");
  Check(stateLocal!=null&&runningLocal!=null&&!ReferenceEquals(stateLocal,runningLocal)&&
   ReferenceEquals(stateLocal,LoadedBefore(t,instruction,"InterruptAtStateSite"))&&ReferenceEquals(runningLocal,LoadedBefore(t,instruction,"InterruptAtRunningSite")),
   "R21 tick: each captured value is kept in its own local of the native invocation and passed only to its own terminal site");
  Check(t.Count==tickOriginal.Count+9&&string.Join(",",hooks)==
   "BeginShouldBeInterrupted,get_ShouldBeInterrupted,CaptureShouldBeInterrupted,InterruptAtStateSite,InterruptAsSoonAsPossible,get_CanAct,CaptureCanAct,InterruptAtRunningSite",
   "tick: native's ShouldBeInterrupted and running CanAct calls stay and their values are captured; the three interrupt sites are wrapped in native order: "+string.Join(",",hooks));
  var banOriginal=Harmony12Instructions(attackBan,managed,instruction);
  var ban=Transpile(transpiler("NativeAttackBanTranspiler"),banOriginal,instruction);
  var banCalls=Calls(ban,instruction);
  Check(ban.Count==banOriginal.Count+1&&banCalls.IndexOf("HasCondition")>=0&&banCalls.IndexOf("HasCondition")+1==banCalls.IndexOf("AttackBanTerm"),
   "UnitAttack.ShouldBeInterrupted: native's own CanNotAttack term stays and its value is captured");

  var instance=harmony12.GetType("Harmony12.HarmonyInstance",true);var harmonyMethod=harmony12.GetType("Harmony12.HarmonyMethod",true);
  nativeBranchHarmony=instance.GetMethod("Create").Invoke(null,new object[]{"KingmakerMountedCombat.StallProbe.R18"});
  var patch=instance.GetMethod("Patch");
  foreach(var pair in new[]{Tuple.Create((MethodBase)turnBased,"PairedEligibilityTranspiler"),Tuple.Create((MethodBase)turnBased,"NativeTurnBasedTerminalTranspiler"),
    Tuple.Create((MethodBase)tick,"NativeTickTerminalTranspiler"),Tuple.Create((MethodBase)attackBan,"NativeAttackBanTranspiler")}){
   Exception failure=null;
   try{patch.Invoke(nativeBranchHarmony,new object[]{pair.Item1,null,null,Activator.CreateInstance(harmonyMethod,new object[]{transpiler(pair.Item2)})});}
   catch(TargetInvocationException e){failure=e.InnerException;}
   Check(failure==null,"a real Harmony12 patch of "+pair.Item1.Name+" with "+pair.Item2+" is constructed"+(failure==null?"":": "+failure.GetType().Name+" "+failure.Message));
  }
 }

 // A live (unfinished) pair command over the fixture's genuine rider, delivering its terminal to the compiled controller.
 static object LiveCommand(Fixture f,Assembly mod){
  var command=f.Dispatch();Set(command,"<IsFinished>k__BackingField",false);
  Set(command,"logger",f.Log.GetTransparentProxy());
  Set(command,"transaction",Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Domain.MountedCombatTransaction",true),true));
  Set(command,"terminal",Delegate.CreateDelegate(Field(f.CommandType,"terminal").FieldType,f.Controller,f.Controller.GetType().GetMethod("HandleCommandTerminal",F)));
  Set(command,"actionActor",f.Rider);Set(command,"<Executor>k__BackingField",f.Rider);
  Set(f.Controller,"<LastOutcome>k__BackingField",null);
  return command;
 }

 // R18 end to end: the compiled hooks, the patched native getter and native Interrupt/ForceFinish, the pair command's own
 // terminal and the controller's answer.
 static void ProbeNativeBranchHooks(Assembly mod,Assembly native){
  var hooks=mod.GetType("KingmakerMountedCombat.Integration.NativeCommandTerminalBranches",true);
  Func<string,object[],object> hook=(name,args)=>{
   try{return hooks.GetMethod(name,F).Invoke(null,args);}
   catch(TargetInvocationException e){if(e.InnerException is NullReferenceException||e.InnerException is MissingMethodException)return null;throw;}
  };
  var success=Enum.Parse(native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand+ResultType",true),"Success");
  Func<string,Action<Fixture,object>,Tuple<object,string>> run=(condition,act)=>{
   var f=NewFixture(mod,native);f.Order(false);var command=LiveCommand(f,mod);hook("Reset",new object[0]);
   if(condition!=null)Condition(f.Rider,condition,true);
   try{act(f,command);}catch(TargetInvocationException){}
   return Tuple.Create(Get(f.Controller,"<LastOutcome>k__BackingField"),f.Feedback);
  };
  Func<object,string,string> read=(outcome,name)=>outcome==null?"<no terminal>":outcome.GetType().GetProperty(name,F).GetValue(outcome,null).ToString();
  // Native UnitState.CanAct cannot be evaluated detached (its type initializer needs the game); the capture receives the
  // value native computed, as the transpiled TickCommand passes it.
  var running=run("Dazed",(f,c)=>{
   var observed=hook("CaptureCanAct",new object[]{false});
   hook("InterruptAtRunningSite",new[]{c,(object)true,observed});
  });
  Check(read(running.Item1,"NativeTerminalBranch")=="RunningCannotAct"&&read(running.Item1,"NativeTerminalCapabilityBlock")=="Dazed"&&
   running.Item2.Contains("cannot act right now (dazed)"),"2: native's running cannot-act interruption records its branch and block and names the daze: "+running.Item2);
  var asap=run("Sleeping",(f,c)=>hook("InterruptAtRunningSite",new[]{c,(object)true,(object)0}));
  Check(read(asap.Item1,"NativeTerminalBranch")=="RunningInterruptAsSoonAsPossible"&&asap.Item2.IndexOf("cannot",StringComparison.Ordinal)<0,
   "6: the InterruptAsSoonAsPossible short-circuit beside a helpless actor is not a capability end: "+asap.Item2);
  var state=run("Stunned",(f,c)=>hook("InterruptAtStateSite",new[]{c,(object)true,(object)0}));
  Check(read(state.Item1,"NativeTerminalBranch")=="TargetUnavailable"&&state.Item2=="Mounted rider melee ended: the exact target became invalid before completion.",
   "5: the target branch of the state site beside a stunned actor is the target's end: "+state.Item2);
  var banned=run("CanNotAttack",(f,c)=>{
   Set(c,"<IsStarted>k__BackingField",true);
   hook("BeginShouldBeInterrupted",new object[0]);
   var value=(bool)c.GetType().GetProperty("ShouldBeInterrupted",F).GetValue(c,null);
   var observed=hook("CaptureShouldBeInterrupted",new object[]{value});
   Check(value&&(int)observed==3,"the patched native UnitAttack.ShouldBeInterrupted reads true for the genuinely attack-banned executor, through its ban term");
   hook("InterruptAtStateSite",new[]{c,(object)true,observed});
  });
  Check(read(banned.Item1,"NativeTerminalBranch")=="AttackBanned"&&read(banned.Item1,"NativeTerminalCapabilityBlock")=="CanNotAttack"&&
   banned.Item2.Contains("cannot attack right now"),"3: native's attack-ban interruption, proven by its own CanNotAttack term, names the ban: "+banned.Item2);
  // Native ForceFinish begins with CombatController.IsInTurnBasedCombat, which needs the game: the hook records the branch
  // on the command before it calls native, and the command's own terminal report (Result Success, as native stamps it)
  // then carries that branch to the controller.
  Action<Fixture,object,object> finish=(f,c,observed)=>{
   try{hook("TurnBasedForceFinish",new[]{c,success,observed});}catch(TargetInvocationException){}
   if(Get(f.Controller,"<LastOutcome>k__BackingField")==null){Set(c,"<Result>k__BackingField",success);Call(c,"ReportTerminalOnce");}
  };
  var forced=run("Dazed",(f,c)=>finish(f,c,hook("CaptureCanAct",new object[]{false})));
  Check(read(forced.Item1,"NativeTerminalBranch")=="TurnBasedCannotAct"&&read(forced.Item1,"NativeTerminalCapabilityBlock")=="Dazed"&&
   forced.Item2.Contains("cannot act right now (dazed). The attack was not performed."),"1: native's turn-based cannot-act force-finish names the exact current block: "+forced.Item2);
  var slot=run("Dazed",(f,c)=>finish(f,c,hook("CaptureCanAct",new object[]{true})));
  Check(read(slot.Item1,"NativeTerminalBranch")=="TurnBasedNoCommandSlot"&&slot.Item2.IndexOf("cannot",StringComparison.Ordinal)<0,
   "the force-finish native took for a missing command slot is not a daze: "+slot.Item2);
  var stray=run("Dazed",(f,c)=>{hook("CaptureCanAct",new object[]{false});finish(f,c,0);});
  Check(read(stray.Item1,"NativeTerminalBranch")=="TurnBasedNoCommandSlot","8: a value another invocation captured is never applied to this one");
  // R21: a ban term left by an unrelated reading of the native getter (no terminal followed) never counts for a later command.
  hook("AttackBanTerm",new object[]{true});
  hook("BeginShouldBeInterrupted",new object[0]);
  Check((int)hook("CaptureShouldBeInterrupted",new object[]{true})==2,"R21: a stale ban term from an earlier healthy reading cannot make a later dead-target reading an attack ban");
  if(nativeBranchHarmony!=null)nativeBranchHarmony.GetType().GetMethod("UnpatchAll",new[]{typeof(string)}).Invoke(nativeBranchHarmony,new object[]{"KingmakerMountedCombat.StallProbe.R18"});
 }

 // ---- Review of dffd3e72 ----------------------------------------------------------------------------------------
 // R21: a static field owns a command if its type can hold one directly or through any field, element, argument, delegate
 // or interface; weak references are non-owning.
 static bool Owns(Type t,Type command,HashSet<Type> seen){
  if(t==null||t.IsPrimitive||t.IsEnum||t.IsPointer||t==typeof(string)||t==typeof(decimal))return false;
  if(typeof(WeakReference).IsAssignableFrom(t))return false;
  if(command.IsAssignableFrom(t)||t.IsAssignableFrom(command))return true;
  if(t.IsArray)return Owns(t.GetElementType(),command,seen);
  if(typeof(Delegate).IsAssignableFrom(t)||t.IsInterface||t.IsAbstract)return true;
  if(!seen.Add(t))return false;
  if(t.IsGenericType&&t.GetGenericTypeDefinition().FullName=="System.Runtime.CompilerServices.ConditionalWeakTable"+(char)96+"2")
   return Owns(t.GetGenericArguments()[1],command,seen);
  foreach(var argument in t.IsGenericType?t.GetGenericArguments():Type.EmptyTypes)if(Owns(argument,command,seen))return true;
  for(var x=t;x!=null&&x!=typeof(object);x=x.BaseType)
   foreach(var field in x.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
    if(Owns(field.FieldType,command,seen))return true;
  return false;
 }
 static readonly List<object> strongHolder=new List<object>();
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static WeakReference[] Exercise(Assembly mod,Assembly native,Func<string,object[],object> hook,int scenario){
  var f=NewFixture(mod,native);f.Order(false);var command=LiveCommand(f,mod);
  if(scenario==1)hook("CaptureCanAct",new object[]{true});
  if(scenario==2){hook("BeginShouldBeInterrupted",new object[0]);hook("CaptureShouldBeInterrupted",new object[]{false});}
  if(scenario==3)hook("CaptureCanAct",new object[]{true});
  if(scenario==4){Set(command,"<Result>k__BackingField",Enum.Parse(native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand+ResultType",true),"Success"));
   try{Call(command,"ReportTerminalOnce");}catch(Exception){}}
  if(scenario==10){hook("CaptureCanAct",new object[]{false});try{throw new InvalidOperationException("between capture and terminal");}catch(InvalidOperationException){}}
  if(scenario==11){hook("BeginShouldBeInterrupted",new object[0]);hook("CaptureShouldBeInterrupted",new object[]{true});
   try{hook("InterruptAtStateSite",new[]{command,(object)true,(object)2});}catch(Exception){}}
  if(scenario==99)strongHolder.Add(command);
  return new[]{new WeakReference(command),new WeakReference(f.Controller)};
 }
 static bool Collected(WeakReference[] refs){
  for(var i=0;i<3;i++){GC.Collect();GC.WaitForPendingFinalizers();}
  return refs.All(r=>!r.IsAlive);
 }
 static void ProbeCaptureOwnership(Assembly mod,Assembly native){
  var type=mod.GetType("KingmakerMountedCombat.Integration.NativeCommandTerminalBranches",true);
  var command=native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true);
  var statics=type.GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Where(f=>!f.IsLiteral).ToArray();
  var owning=statics.Where(f=>Owns(f.FieldType,command,new HashSet<Type>())).Select(f=>f.Name).ToArray();
  Check(owning.Length==0,"R21 structural: no static or thread-static field of the native capture strongly owns a UnitCommand ("+
   string.Join(",",statics.Select(f=>f.Name+":"+f.FieldType.Name).ToArray())+")");
  Check(Owns(typeof(List<>).MakeGenericType(command),command,new HashSet<Type>())&&Owns(typeof(object),command,new HashSet<Type>())&&
   Owns(typeof(Action),command,new HashSet<Type>())&&!Owns(typeof(WeakReference),command,new HashSet<Type>())&&!Owns(typeof(bool),command,new HashSet<Type>()),
   "R21 structural: the ownership rule recognises strong holders and treats weak references and scalars as non-owning");
  var captures=type.GetMethods(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)
   .Where(m=>m.Name.StartsWith("Capture",StringComparison.Ordinal)||m.Name.StartsWith("Begin",StringComparison.Ordinal)||m.Name=="AttackBanTerm").ToArray();
  Check(captures.Length==4&&captures.All(m=>m.GetParameters().All(a=>!Owns(a.ParameterType,command,new HashSet<Type>()))&&!Owns(m.ReturnType,command,new HashSet<Type>())),
   "R21 structural: the capture hooks never receive a command; the facts travel in the native invocation's own locals");
  Func<string,object[],object> hook=(name,args)=>{
   try{return type.GetMethod(name,F).Invoke(null,args);}
   catch(TargetInvocationException e){if(e.InnerException is NullReferenceException||e.InnerException is MissingMethodException)return null;throw;}
  };
  Check(!Collected(Exercise(mod,native,hook,99)),"R21 collection control: a command deliberately held by a static list stays alive (the check is not vacuous)");
  strongHolder.Clear();
  foreach(var scenario in new[]{1,2,3,4,10,11}){
   var label=scenario==1?"1: a healthy turn-based capture with no force-finish":scenario==2?"2: a healthy ShouldBeInterrupted == false":
    scenario==3?"3: a healthy running CanAct == true":scenario==4?"4: an ordinary completion without a wrapped terminal":
    scenario==10?"10: an exception between capture and terminal":"11: a terminal followed by no later command";
   Check(Collected(Exercise(mod,native,hook,scenario)),"R21 "+label+" leaves the command and its controller collectable");
  }
  hook("AttackBanTerm",new object[]{true});
  hook("Reset",new object[0]);
  Check(statics.All(f=>{var v=f.GetValue(null);return v==null||v.Equals(Activator.CreateInstance(f.FieldType));}),
   "R21 12: disable and disposal return every static of the native capture to its idle value");
 }

 // R22: the retired scheduler's lease is released on a deferred, owned cleanup, never kept forever. The engine boundary is
 // substituted only where a detached process cannot run it: the scheduler's own resource/frame observation is skipped or
 // made to fail on demand by a probe-scoped Harmony prefix, removed afterwards.
 // The substitute is compiled against Harmony12 (MountedAttackStallProbeEngine, below). The observation itself cannot be
 // patched detached (Harmony must compile it, and its Unity frame-clock call cannot link outside Unity), so its callers
 // Update, ObserveTerminal and ArchiveAndDispose are patched to call the substitute instead; the rest of their code is
 // the product's own.
 static Type ProbeEngine(){
  return AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("MountedAttackStallProbeEngine",false)).First(t=>t!=null);
 }
 static bool refreshThrows{set{ProbeEngine().GetField("RefreshThrows").SetValue(null,value);}}
 static void ProbeSchedulerLease(Assembly mod,Assembly native,string managed){
  var harmony12=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony12.dll"));
  var instance=harmony12.GetType("Harmony12.HarmonyInstance",true).GetMethod("Create").Invoke(null,new object[]{"KingmakerMountedCombat.StallProbe.R22"});
  var type=mod.GetType("KingmakerMountedCombat.Integration.MountedPairCommandScheduler",true);
  foreach(var caller in new[]{"Update","ObserveTerminal","ArchiveAndDispose"})
   instance.GetType().GetMethod("Patch").Invoke(instance,new object[]{type.GetMethod(caller,F),null,null,
    Activator.CreateInstance(harmony12.GetType("Harmony12.HarmonyMethod",true),new object[]{ProbeEngine().GetMethod("WithoutRefresh")})});
  try{
   var leaseType=type.GetNestedType("SchedulerLease",F);
   var machineType=mod.GetType("KingmakerMountedCombat.Domain.PairedCommandSchedulerLeaseStateMachine",true);
   var commandsType=native.GetType("Kingmaker.UnitLogic.Commands.UnitCommands",true);
   var triggerType=type.GetMethod("HandleDismounting",F).GetParameters()[0].ParameterType;
   Func<Fixture,object,Tuple<object,object,object>> make=(f,command)=>{
    var scheduler=Blank(type);Set(scheduler,"logger",f.Log.GetTransparentProxy());
    Set(scheduler,"relationship",Get(f.Controller,"relationship"));
    var machine=Activator.CreateInstance(machineType);machineType.GetMethod("Register").Invoke(machine,new object[]{"rider","mount",1L,"turn","command"});
    var lease=Activator.CreateInstance(leaseType,true);Set(lease,"StateMachine",machine);Set(lease,"Command",command);
    Set(lease,"Mount",f.Mount);Set(lease,"Rider",f.Rider);Set(scheduler,"lease",lease);
    // The mount's native slots hold the finished command until native (or the controller's sweep) removes it.
    var commands=Blank(commandsType);var slots=Array.CreateInstance(native.GetType("Kingmaker.UnitLogic.Commands.Base.UnitCommand",true),32);
    slots.SetValue(command,Convert.ToInt32(command.GetType().GetProperty("Type",F).GetValue(command,null)));
    Set(commands,"m_Commands",slots);Set(f.Mount,"<Commands>k__BackingField",commands);
    return Tuple.Create(scheduler,machine,(object)slots);
   };
   Func<object,string> state=machine=>machineType.GetProperty("State").GetValue(machine,null).ToString();
   Action<string,Fixture,Action> logged=(text,f,act)=>{Exception escaped=null;f.Log.Throws=true;try{act();}catch(Exception e){escaped=e;}f.Log.Throws=false;
    Check(escaped==null,text+": nothing escapes a logger that throws on every call"+(escaped==null?"":": "+escaped.GetType().Name+" "+escaped.Message));};
   {
    var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();var x=make(f,command);var scheduler=x.Item1;var lease=Get(scheduler,"lease");
    refreshThrows=true;
    logged("1: a terminal observation that fails",f,()=>Call(scheduler,"ObserveTerminal",command,"Interrupt","observed"));
    refreshThrows=false;
    Check(ReferenceEquals(Get(scheduler,"lease"),lease)&&state(x.Item2)=="Faulted",
     "1: a failed terminal observation faults the lease and keeps it only while its exact finished command still holds the mount's native slot");
    logged("2: an update while the slot is still held",f,()=>Call(scheduler,"Update"));
    Check(ReferenceEquals(Get(scheduler,"lease"),lease),"2: the update keeps the lease while native still holds the finished command");
    ((Array)x.Item3).SetValue(null,Convert.ToInt32(command.GetType().GetProperty("Type",F).GetValue(command,null)));
    logged("2: the update after native slot removal",f,()=>Call(scheduler,"Update"));
    Check(Get(scheduler,"lease")==null&&state(x.Item2)=="Disposed"&&!(bool)Get(scheduler,"disposed"),
     "2: the next update after native slot removal archives and releases the lease; the scheduler stays usable");
   }
   {
    var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();var x=make(f,command);var scheduler=x.Item1;
    refreshThrows=true;
    logged("1: a failed observation followed by an update that cannot observe",f,()=>{Call(scheduler,"ObserveTerminal",command,"Interrupt","observed");Call(scheduler,"Update");});
    refreshThrows=false;
    Check(Get(scheduler,"lease")==null&&state(x.Item2)=="Disposed","1: when the observation keeps failing, the next update's fault boundary archives and releases the lease");
   }
   foreach(var trigger in new[]{"LoadRequested","AreaUnloading","Manual"}){
    var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();var x=make(f,command);var scheduler=x.Item1;
    refreshThrows=true;Call(scheduler,"ObserveTerminal",command,"Interrupt","observed");refreshThrows=false;
    logged("3/8: relationship cleanup ("+trigger+")",f,()=>Call(scheduler,"HandleDismounting",Enum.Parse(triggerType,trigger)));
    Check(Get(scheduler,"lease")==null&&state(x.Item2)=="Disposed","3/8: relationship cleanup ("+trigger+") releases the retained lease");
   }
   {
    var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();var x=make(f,command);var scheduler=x.Item1;
    Set(scheduler,"relationship",Blank(Field(type,"relationship").FieldType));
    refreshThrows=true;Call(scheduler,"ObserveTerminal",command,"Interrupt","observed");refreshThrows=false;
    logged("4/8: disable or disposal",f,()=>Call(scheduler,"Dispose"));
    Check(Get(scheduler,"lease")==null&&state(x.Item2)=="Disposed"&&(bool)Get(scheduler,"disposed"),"4/8: disable or world/pair disposal releases the retained lease");
   }
   {
    var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();var x=make(f,command);var scheduler=x.Item1;var lease=Get(scheduler,"lease");
    var foreign=f.Dispatch();
    logged("6: a foreign command",f,()=>{Call(scheduler,"ObserveTerminal",foreign,"Interrupt","foreign");Call(scheduler,"AbandonRegistration",foreign,"foreign");
     Call(scheduler,"ObserveExternalInterrupt",foreign,"foreign");});
    Check(ReferenceEquals(Get(scheduler,"lease"),lease)&&state(x.Item2)=="Registered","6: no foreign command or pair can observe, abandon, interrupt or consume the lease");
   }
   // 5: the registration gate's only lease condition (IL): CanRegisterCurrentPair reads the lease field and refuses while it
   // is set; nothing else in the scheduler holds a released lease.
   var il=type.GetMethod("CanRegisterCurrentPair",F).GetMethodBody().GetILAsByteArray();var leaseField=Field(type,"lease");var reads=0;
   for(var i=0;i+4<il.Length;i++)if(il[i]==0x7B){try{if(type.Module.ResolveField(BitConverter.ToInt32(il,i+1))==leaseField)reads++;}catch(Exception){}}
   Check(reads==1,"5: the registration gate reads the lease exactly once (refusing while it is set), so a released lease never blocks a later registration");
  }
  finally{
   refreshThrows=false;
   instance.GetType().GetMethod("UnpatchAll",new[]{typeof(string)}).Invoke(instance,new object[]{"KingmakerMountedCombat.StallProbe.R22"});
  }
 }

 // R19: the innermost open Run on the ending command's container decides (production scopes, compiled).
 static void ProbeInnermostRun(Assembly mod){
  var type=mod.GetType("KingmakerMountedCombat.Domain.MountedNativeRunScopes"+(char)96+"2",true).MakeGenericType(typeof(object),typeof(object));
  var scopes=Activator.CreateInstance(type);
  Func<object,object,object> open=(c,o)=>type.GetMethod("Open").Invoke(scopes,new[]{c,o});
  Action<object> close=token=>type.GetMethod("Close").Invoke(scopes,new[]{token});
  Func<object,object,bool> replaced=(c,o)=>(bool)type.GetMethod("EndedInsideReplacingRun").Invoke(scopes,new[]{c,o});
  var rider=new object();var other=new object();var pair=new object();var a=new object();var b=new object();
  var outer=open(rider,a);
  Check(replaced(rider,pair),"1: an ordinary Run of A replaces the pair command");
  var inner=open(rider,b);
  Check(replaced(rider,pair),"2: nested A then B, the pair command ending inside B is replaced by B");
  Check(!replaced(rider,b),"3: B ending inside its own Run is not labelled replaced by the outer A");
  var foreign=open(other,new object());
  Check(!replaced(rider,b)&&replaced(rider,pair),"4: a nested Run on another pair changes nothing for this one");
  close(foreign);close(inner);
  Check(replaced(rider,b)&&!replaced(rider,a),"5: after the inner close the outer attribution remains");
  try{var thrown=open(rider,b);try{throw new InvalidOperationException();}finally{close(thrown);}}catch(InvalidOperationException){}
  Check(!replaced(rider,a),"6: an exception unwinds the inner token");
  open(rider,b);close(outer);
  Check(!replaced(rider,pair),"7: a stranded token goes with its outer close");
  open(rider,a);type.GetMethod("Reset").Invoke(scopes,null);
  Check(!replaced(rider,pair),"8: reset at disable or disposal leaves no token");
  var later=open(rider,a);
  Check(replaced(rider,pair)&&!replaced(rider,a),"9: a later replacement still works");close(later);
 }

 // R20: the retired scheduler, when explicitly exercised, records its fault, the command's typed cause, interrupts and
 // archives before any diagnostic; a logger that throws on every call stops none of it.
 static void ProbeSchedulerContainment(Assembly mod,Assembly native){
  var type=mod.GetType("KingmakerMountedCombat.Integration.MountedPairCommandScheduler",true);
  var leaseType=type.GetNestedType("SchedulerLease",F);
  var machineType=mod.GetType("KingmakerMountedCombat.Domain.PairedCommandSchedulerLeaseStateMachine",true);
  var trigger=Enum.GetValues(type.GetMethod("HandleDismounting",F).GetParameters()[0].ParameterType).GetValue(0);
  foreach(var path in new[]{"Update","ObserveTerminal","AbandonRegistration","Dispose","HandleDismounting"}){
   var f=NewFixture(mod,native);f.Order(false);var command=LiveCommand(f,mod);
   var scheduler=Blank(type);Set(scheduler,"logger",f.Log.GetTransparentProxy());Set(scheduler,"relationship",Blank(Field(type,"relationship").FieldType));
   var machine=Activator.CreateInstance(machineType);machineType.GetMethod("Register").Invoke(machine,new object[]{"rider","mount",1L,"turn","command"});
   var lease=Activator.CreateInstance(leaseType,true);Set(lease,"StateMachine",machine);Set(lease,"Command",command);Set(scheduler,"lease",lease);
   f.Log.Throws=true;Exception escaped=null;
   try{
    if(path=="Update")Call(scheduler,"Update");
    else if(path=="ObserveTerminal")Call(scheduler,"ObserveTerminal",command,"Interrupt","observed");
    else if(path=="AbandonRegistration")Call(scheduler,"AbandonRegistration",command,"abandoned");
    else if(path=="Dispose")Call(scheduler,"Dispose");
    else Call(scheduler,"HandleDismounting",trigger);
   }catch(Exception e){escaped=e;}
   f.Log.Throws=false;
   var expected=path=="Dispose"||path=="HandleDismounting"?"Cancelled":"Fault";
   Check(escaped==null,path+": nothing escapes a logger that throws on every call"+(escaped==null?"":": "+escaped.GetType().Name+" "+escaped.Message));
   Check((bool)Get(command,"<IsFinished>k__BackingField")&&Get(command,"endCause").ToString()==expected,path+": the exact command records "+expected+" and is interrupted");
   var state=machineType.GetProperty("State").GetValue(machine,null).ToString();
   if(expected=="Fault")Check(machineType.GetProperty("FaultReason").GetValue(machine,null)!=null&&(state=="Faulted"||state=="Disposed"),path+": the scheduler's fault state is recorded");
   if(path!="ObserveTerminal")Check(Get(scheduler,"lease")==null&&state=="Disposed",path+": the lease is archived and released");
  }
  // Admission and eligibility read the Unity frame clock, which a detached process cannot run; their failure routes are
  // pinned on the compiled IL: no diagnostic precedes the fault boundary, and the boundary orders its steps.
  var fail=CallOrder(type.GetMethod("FailAndInterrupt",F));
  Func<string,int> at=name=>fail.FindIndex(n=>n.EndsWith(name,StringComparison.Ordinal));
  Check(at(".Fault")>=0&&at(".Fault")<at(".InterruptExactCommand")&&at(".InterruptExactCommand")<Math.Min(at(".TryLog")<0?int.MaxValue:at(".TryLog"),at(".TryLogException"))&&
   !fail.Any(n=>n.StartsWith("IModLogger.",StringComparison.Ordinal)),"the fault boundary records fault state, then the typed interrupt, then a contained diagnostic: "+string.Join(",",fail.ToArray()));
  foreach(var name in new[]{"ConfirmAdmission","TryExtendNativeEligibility","Update","ObserveTerminal","AbandonRegistration","Dispose","HandleDismounting","InterruptExactCommand","ArchiveAndDispose"})
   Check(!CallOrder(type.GetMethod(name,F)).Any(n=>n.StartsWith("IModLogger.",StringComparison.Ordinal)),name+" calls no logger directly");
 }

 // R20: the terminal handler settles ownership, the finished-command sweep, the outcome and the order whatever fails.
 static void ProbeTerminalContainment(Assembly mod,Assembly native,Sink warnings){
  var controllerType=mod.GetType("KingmakerMountedCombat.Integration.MountedCombatController",true);
  Action<string,Fixture,object,object> settled=(text,f,command,outcome)=>{
   Exception escaped=null;f.Log.Throws=true;
   try{f.Terminal(command,outcome);}catch(Exception e){escaped=e;}
   f.Log.Throws=false;
   Check(escaped==null&&ReferenceEquals(Get(f.Controller,"<LastOutcome>k__BackingField"),outcome)&&ReferenceEquals(Get(f.Controller,"finishedCommandPendingSweep"),command)&&
    Get(f.Controller,"stockIntentCommand")==null&&!f.HasIntent,text+(escaped==null?"":": "+escaped.GetType().Name+" "+escaped.Message));
  };
  {
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();Set(command,"chargeMode",true);
   var owner=Activator.CreateInstance(controllerType.GetNestedType("ChargeOwner",F),true);
   Set(owner,"Command",command);Set(owner,"ManualTargetOwned",true);Set(owner,"Ownership",Blank(Field(owner.GetType(),"Ownership").FieldType));
   Set(f.Controller,"chargeOwner",owner);
   settled("1: a charge manual-target release that throws, with a throwing logger, still settles the terminal",f,command,f.Unexecuted());
  }
  {
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();
   var type=mod.GetType("KingmakerMountedCombat.Integration.MountedPairCommandScheduler",true);
   var scheduler=Blank(type);Set(scheduler,"logger",f.Log.GetTransparentProxy());
   var machine=Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Domain.PairedCommandSchedulerLeaseStateMachine",true));
   machine.GetType().GetMethod("Register").Invoke(machine,new object[]{"rider","mount",1L,"turn","command"});
   var lease=Activator.CreateInstance(type.GetNestedType("SchedulerLease",F),true);Set(lease,"StateMachine",machine);Set(lease,"Command",command);
   Set(scheduler,"lease",lease);Set(f.Controller,"pairedCommandScheduler",scheduler);
   settled("3: a legacy scheduler terminal observation that throws, with a throwing logger, still settles the terminal",f,command,f.Unexecuted());
  }
  var admission=CallOrder(controllerType.GetMethod("AdmitChargeCommand",F));
  Check(admission.FindIndex(n=>n.EndsWith(".Retire",StringComparison.Ordinal))>=0&&
   admission.FindIndex(n=>n.EndsWith(".Retire",StringComparison.Ordinal))<admission.FindIndex(n=>n.EndsWith(".TryLogException",StringComparison.Ordinal))&&
   !admission.Any(n=>n.StartsWith("IModLogger.",StringComparison.Ordinal)),"2: a charge admission release failure retires ownership before a contained diagnostic");
  Check(!CallOrder(controllerType.GetMethod("HandleCommandTerminal",F)).Any(n=>n.StartsWith("IModLogger.",StringComparison.Ordinal)),
   "the terminal handler calls no logger directly");
  {
   var warned=warnings.Calls.Count;
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();
   settled("4: an unexecuted player terminal with a throwing logger retires its order",f,command,f.Unexecuted());
   Check(warnings.Calls.Count==warned+1,"4: and warns once");
   var g=NewFixture(mod,native);g.Order(true);var automatic=g.Dispatch();
   settled("5: an unexecuted automatic terminal with a throwing logger retires its order",g,automatic,g.Unexecuted());
   Check(warnings.Calls.Count==warned+1,"5: and stays silent");
  }
  foreach(var tail in new[]{false,true}){
   var f=NewFixture(mod,native);f.Order(false);var command=f.Dispatch();f.Log.Throws=true;Exception escaped=null;
   try{f.Terminal(command,tail?f.Outcome("Interrupt","Interrupt","Cancelled",1,true,2,3,true,"None"):f.Completed(1));}catch(Exception e){escaped=e;}
   f.Log.Throws=false;
   Check(escaped==null&&f.HasIntent&&ReferenceEquals(Get(f.Controller,"finishedCommandPendingSweep"),command),"6: a "+(tail?"ranged-tail":"completed")+" terminal keeps its order with a throwing logger");
  }
  {
   var f=NewFixture(mod,native);f.Order(false);var command=LiveCommand(f,mod);
   var count=new int[1];
   var first=System.Linq.Expressions.Expression.Parameter(f.CommandType);var second=System.Linq.Expressions.Expression.Parameter(f.OutcomeType);
   var body=System.Linq.Expressions.Expression.Block(
    System.Linq.Expressions.Expression.PostIncrementAssign(System.Linq.Expressions.Expression.ArrayAccess(System.Linq.Expressions.Expression.Constant(count),System.Linq.Expressions.Expression.Constant(0))),
    System.Linq.Expressions.Expression.Throw(System.Linq.Expressions.Expression.New(typeof(InvalidOperationException))));
   Set(command,"terminal",System.Linq.Expressions.Expression.Lambda(Field(f.CommandType,"terminal").FieldType,body,first,second).Compile());
   Exception escaped=null;f.Log.Throws=true;
   try{Call(command,"ReportTerminalOnce");Call(command,"ReportTerminalOnce");}catch(Exception e){escaped=e;}
   f.Log.Throws=false;
   Check(escaped==null&&count[0]==1,"7: a handler that throws is contained, delivered once and never applied twice"+(escaped==null?"":": "+escaped.GetType().Name));
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
  Section("R18 native capability terminals at the controller",()=>ProbeNativeCapabilityTerminals(mod,native));
  Section("R18 native branch transpilers on the installed IL",()=>ProbeNativeBranchTranspilers(mod,native,managed));
  Section("R18 native branch hooks end to end",()=>ProbeNativeBranchHooks(mod,native));
  Section("R19 the innermost native Run token decides",()=>ProbeInnermostRun(mod));
  Section("R21 native terminal captures own no command",()=>ProbeCaptureOwnership(mod,native));
  Section("R22 the retired scheduler's deferred lease release",()=>ProbeSchedulerLease(mod,native,managed));
  Section("R20 scheduler fault containment",()=>ProbeSchedulerContainment(mod,native));
  Section("R20 terminal handler containment",()=>ProbeTerminalContainment(mod,native,warnings));
  Section("A04 automatic admission of a rider that cannot attack",()=>ProbeAutomaticAdmission(mod));
  Section("A03 native consumers of Fascinate's condition",()=>ProbeFascinateCondition(native));
  Section("Sprint 6 product wiring",()=>ProbeSprint6Wiring(mod));
  Console.WriteLine("MOUNTED ATTACK STALL REPAIR PASS="+passed+" FAIL="+sectionFailures+"; detached, not native gameplay qualification");
  if(sectionFailures>0)throw new InvalidOperationException(sectionFailures+" Sprint 6 section(s) failed");
 }
}
'@
# Review of dffd3e72 (R22): the probe-scoped substitute for the scheduler's own resource and frame observation.
Add-Type -ReferencedAssemblies (Join-Path $managed 'UnityModManager/0Harmony12.dll') -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
public static class MountedAttackStallProbeEngine {
 public static bool RefreshThrows;
 public static int Substituted;
 public static void Refresh(object scheduler){Substituted++;if(RefreshThrows)throw new InvalidOperationException("observation failure");}
 public static IEnumerable<Harmony12.CodeInstruction> WithoutRefresh(IEnumerable<Harmony12.CodeInstruction> original){
  var replaced=0;var code=new List<Harmony12.CodeInstruction>(original);
  foreach(var instruction in code){
   var method=instruction.operand as System.Reflection.MethodInfo;
   if(method!=null&&method.Name=="RefreshLifecycleAndResources"){instruction.opcode=OpCodes.Call;instruction.operand=typeof(MountedAttackStallProbeEngine).GetMethod("Refresh");replaced++;}
  }
  if(replaced!=1)throw new InvalidOperationException("expected one scheduler observation call, found "+replaced);
  return code;
 }
}
'@
try { [MountedAttackStallProbe]::Run($managed,$dll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
