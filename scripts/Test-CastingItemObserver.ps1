param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
$json=Join-Path $managed 'Newtonsoft.Json.dll'
[Reflection.Assembly]::LoadFrom($json)|Out-Null
Add-Type -ReferencedAssemblies $json -TypeDefinition @"
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
public static class CastingObserverProbe {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static int passed;
 static void Check(bool condition,string why){if(!condition)throw new InvalidOperationException(why);passed++;Console.WriteLine("PASS "+why);}
 public static void InaccessibleUnityLog(Exception error){
  throw new InvalidOperationException("Native disposal failed before inaccessible Unity logging.",error);
 }
 public static void InaccessibleComponentDispose(object component){
  throw new InvalidOperationException("A live native Unity component reached detached disposal.");
 }
 public static void InaccessibleUnityDestroy(object component){
  throw new InvalidOperationException("A nonempty Unity component reached the detached fixture probe.");
 }
 public static System.Collections.Generic.IEnumerable<T> ReplaceDetachedDestroy<T>(System.Collections.Generic.IEnumerable<T> instructions,MethodBase __originalMethod){
  if(__originalMethod.MetadataToken!=0x06009A5A)throw new InvalidOperationException("Unexpected native disposal boundary.");
  var replacement=typeof(CastingObserverProbe).GetMethod("InaccessibleUnityDestroy",F);
  var list=new System.Collections.Generic.List<T>();var count=0;var components=0;var logs=0;
  foreach(var instruction in instructions){
   var operand=typeof(T).GetField("operand").GetValue(instruction) as MethodInfo;
   if(operand!=null&&operand.DeclaringType.FullName=="UnityEngine.Object"&&operand.Name=="Destroy"&&operand.GetParameters().Length==1){
    // Only inaccessible Unity component/destruction/logging boundaries are mocked. Keep every
    // native disposal call, state mutation, finally and disposed assertion.
    // Empty component lists never reach this deliberately refusing boundary.
    typeof(T).GetField("operand").SetValue(instruction,replacement);count++;
   }
   if(operand!=null&&operand.DeclaringType.FullName=="Kingmaker.Blueprints.GameLogicComponent"&&operand.Name=="Dispose"){
    typeof(T).GetField("opcode").SetValue(instruction,System.Reflection.Emit.OpCodes.Call);
    typeof(T).GetField("operand").SetValue(instruction,typeof(CastingObserverProbe).GetMethod("InaccessibleComponentDispose",F));components++;
   }
   if(operand!=null&&operand.DeclaringType.FullName=="UberDebug"&&operand.Name=="LogException"){
    typeof(T).GetField("operand").SetValue(instruction,typeof(CastingObserverProbe).GetMethod("InaccessibleUnityLog",F));logs++;
   }
   list.Add(instruction);
  }
  if(count!=1||components!=1||logs!=1)throw new InvalidOperationException("Pinned native Unity external boundaries differ.");
  return list;
 }
 static readonly TimeSpan detachedClock=TimeSpan.FromSeconds(13);
 public static TimeSpan DetachedNativeClock(){return detachedClock;}
 static System.Reflection.Emit.DynamicMethod projectedToggle;
 public static System.Collections.Generic.IEnumerable<T> ReplaceDetachedClock<T>(System.Collections.Generic.IEnumerable<T> instructions,MethodBase __originalMethod){
  if(__originalMethod.MetadataToken!=0x06002AA0)throw new InvalidOperationException("Unexpected native toggle.");
  var input=instructions.ToList();var output=new System.Collections.Generic.List<T>();var count=0;
  for(var i=0;i<input.Count;i++){
   var called=typeof(T).GetField("operand").GetValue(input[i]) as MethodInfo;
   if(called!=null&&called.DeclaringType.FullName=="Kingmaker.Game"&&called.Name=="get_Instance"){
    if(i+2>=input.Count)throw new InvalidOperationException("Native clock read truncated.");
    var field=typeof(T).GetField("operand").GetValue(input[i+1]) as FieldInfo;
    var clock=typeof(T).GetField("operand").GetValue(input[i+2]) as MethodInfo;
    if(field==null||field.MetadataToken!=0x040006BB||clock==null||clock.MetadataToken!=0x060090CB)throw new InvalidOperationException("Pinned native clock pipeline changed.");
    foreach(var removed in new[]{input[i+1],input[i+2]})
     foreach(var name in new[]{"labels","blocks"})
      if(((System.Collections.IEnumerable)typeof(T).GetField(name).GetValue(removed)).Cast<object>().Any())throw new InvalidOperationException("Clock boundary owns a native branch/block.");
    typeof(T).GetField("operand").SetValue(input[i],typeof(CastingObserverProbe).GetMethod("DetachedNativeClock",F));
    output.Add(input[i]);i+=2;count++;
   }else output.Add(input[i]);
  }
  if(count!=1)throw new InvalidOperationException("Native toggle must have one exact external clock read.");
  return output;
 }
 public static System.Collections.Generic.IEnumerable<T> RedirectExactToggle<T>(System.Collections.Generic.IEnumerable<T> instructions){
  var output=new System.Collections.Generic.List<T>();var count=0;
  foreach(var instruction in instructions){
   var called=typeof(T).GetField("operand").GetValue(instruction) as MethodInfo;
   if(called!=null&&called.MetadataToken==0x06002AA0&&called.DeclaringType.FullName=="Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbility"){
    typeof(T).GetField("opcode").SetValue(instruction,System.Reflection.Emit.OpCodes.Call);
    typeof(T).GetField("operand").SetValue(instruction,projectedToggle);count++;
   }
   output.Add(instruction);
  }
  if(count!=1)throw new InvalidOperationException("Production fixture must use one normal native toggle.");
  return output;
 }
 static System.Reflection.Emit.DynamicMethod projectedFactDispose;
 public static System.Collections.Generic.IEnumerable<T> RedirectDetachedBaseDispose<T>(System.Collections.Generic.IEnumerable<T> instructions,MethodBase __originalMethod){
  if(__originalMethod.MetadataToken!=0x06002AC4)throw new InvalidOperationException("Unexpected activation disposal boundary.");
  var list=new System.Collections.Generic.List<T>();var count=0;
  foreach(var instruction in instructions){
   var called=typeof(T).GetField("operand").GetValue(instruction) as MethodInfo;
   if(called!=null&&called.MetadataToken==0x06009A5A){
    typeof(T).GetField("operand").SetValue(instruction,projectedFactDispose);count++;
   }
   list.Add(instruction);
  }
  if(count!=1)throw new InvalidOperationException("Pinned native base disposal call differs.");
  return list;
 }
 static System.Reflection.Emit.DynamicMethod CopyDetachedDispose(Assembly native,Type instruction,int token,string transformer){
  return CopyDetachedMethod((MethodInfo)native.ManifestModule.ResolveMethod(token),instruction,transformer);
 }
 static System.Reflection.Emit.DynamicMethod CopyDetachedMethod(MethodInfo original,Type instruction,string transformer){
  var parameters=original.GetParameters().Select(p=>p.ParameterType).ToList();
  if(!original.IsStatic)parameters.Insert(0,original.DeclaringType);
  var dynamic=new System.Reflection.Emit.DynamicMethod("detached_"+original.MetadataToken.ToString("X8"),original.ReturnType,parameters.ToArray(),typeof(CastingObserverProbe).Module,true);
  var il=dynamic.GetILGenerator();var copierType=instruction.Assembly.GetType("Harmony12.ILCopying.MethodCopier",true);
  var copier=Activator.CreateInstance(copierType,F,null,new object[]{original,il,null},null);
  copierType.GetMethod("AddTranspiler",F).Invoke(copier,new object[]{typeof(CastingObserverProbe).GetMethod(transformer,F).MakeGenericMethod(instruction)});
  var labels=new System.Collections.Generic.List<System.Reflection.Emit.Label>();
  var finish=copierType.GetMethods(F).Single(m=>m.Name=="Finalize"&&m.GetParameters().Length==2);
  var blocks=Activator.CreateInstance(finish.GetParameters()[1].ParameterType);
  finish.Invoke(copier,new[]{(object)labels,blocks});
  foreach(var label in labels)il.MarkLabel(label);
  foreach(var block in (System.Collections.IEnumerable)blocks){
   if(block.GetType().GetField("blockType",F).GetValue(block).ToString()!="EndExceptionBlock")throw new InvalidOperationException("Unexpected pending native exception block.");
   il.EndExceptionBlock();
  }
  il.Emit(System.Reflection.Emit.OpCodes.Ret);
  return dynamic;
 }
 static System.Reflection.Emit.DynamicMethod detachedDeliverTouch,detachedGroundTarget;
 public static bool DetachedNullEqual(object a,object b){
  if(a!=null||b!=null)throw new InvalidOperationException("A live Unity object reached detached comparison.");
  return true;
 }
 public static bool DetachedNullNotEqual(object a,object b){return !DetachedNullEqual(a,b);}
 public static bool DetachedNullPresent(object a){
  if(a!=null)throw new InvalidOperationException("A live Unity object reached detached presence.");
  return false;
 }
 public static System.Collections.Generic.IEnumerable<T> DetachedGroundTarget<T>(System.Collections.Generic.IEnumerable<T> instructions,MethodBase __originalMethod){
  if(__originalMethod.Name!="GetTarget"||__originalMethod.DeclaringType.FullName!="Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler")
   throw new InvalidOperationException("Unexpected native target boundary.");
  var list=new System.Collections.Generic.List<T>();var count=0;var quaternion=0;
  foreach(var instruction in instructions){
   var called=typeof(T).GetField("operand").GetValue(instruction) as MethodInfo;
   if(called!=null&&called.DeclaringType.FullName=="UnityEngine.Object"){
    var name=called.Name=="op_Equality"?"DetachedNullEqual":called.Name=="op_Inequality"?"DetachedNullNotEqual":
     called.Name=="op_Implicit"?"DetachedNullPresent":null;
    if(name==null)throw new InvalidOperationException("Unexpected Unity target operation.");
    typeof(T).GetField("operand").SetValue(instruction,typeof(CastingObserverProbe).GetMethod(name,F));count++;
   }
   if(called!=null&&called.DeclaringType.FullName=="UnityEngine.Quaternion"&&
    (called.Name=="LookRotation"||called.Name=="get_eulerAngles")){
    var parameters=called.GetParameters().Select(p=>p.ParameterType).ToList();
    if(!called.IsStatic)parameters.Insert(0,called.DeclaringType.MakeByRefType());
    var unavailable=new System.Reflection.Emit.DynamicMethod("unavailable_"+called.MetadataToken,called.ReturnType,
     parameters.ToArray(),typeof(CastingObserverProbe).Module,true);
    var il=unavailable.GetILGenerator();il.Emit(System.Reflection.Emit.OpCodes.Ldstr,"Point-only Unity rotation reached unit-ground refusal.");
    il.Emit(System.Reflection.Emit.OpCodes.Newobj,typeof(InvalidOperationException).GetConstructor(new[]{typeof(string)}));
    il.Emit(System.Reflection.Emit.OpCodes.Throw);
    typeof(T).GetField("opcode").SetValue(instruction,System.Reflection.Emit.OpCodes.Call);
    typeof(T).GetField("operand").SetValue(instruction,unavailable);quaternion++;
   }
   list.Add(instruction);
  }
  // Seven pinned Unity comparisons; accept only null inputs, never simulate a view.
  if(count!=7||quaternion!=2)throw new InvalidOperationException("Pinned native null/rotation boundary differs.");
  return list;
 }
 public static System.Collections.Generic.IEnumerable<T> DetachedGroundRefusal<T>(System.Collections.Generic.IEnumerable<T> instructions,MethodBase __originalMethod){
  if(__originalMethod.DeclaringType.FullName!="Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler"||
   __originalMethod.Name!="OnClick")throw new InvalidOperationException("Unexpected native click boundary.");
  var list=new System.Collections.Generic.List<T>();var count=0;var targets=0;
  foreach(var instruction in instructions){
   var called=typeof(T).GetField("operand").GetValue(instruction) as MethodInfo;
   if(called!=null&&called.DeclaringType==__originalMethod.DeclaringType&&called.Name=="GetTarget"){
    typeof(T).GetField("opcode").SetValue(instruction,System.Reflection.Emit.OpCodes.Call);
    typeof(T).GetField("operand").SetValue(instruction,detachedGroundTarget);targets++;
   }
   else if(called!=null&&called.DeclaringType==__originalMethod.DeclaringType&&called.Name=="get_IsTurnBasedDeliverTouch"){
    typeof(T).GetField("opcode").SetValue(instruction,System.Reflection.Emit.OpCodes.Call);
    typeof(T).GetField("operand").SetValue(instruction,detachedDeliverTouch);count++;
   }
   list.Add(instruction);
  }
  // Only the inaccessible global turn/touch input is projected. Keep native
  // target resolution, refusal, admission and spending branches unchanged.
  if(count!=1||targets!=1)throw new InvalidOperationException("Pinned native touch/target boundary differs.");
  return list;
 }
 static void ExerciseNativeMoveReadiness(Assembly native,Type child){
  var actor=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
  var mover=FormatterServices.GetUninitializedObject(actor);
  var foreign=FormatterServices.GetUninitializedObject(actor);
  var moveType=native.GetType("Kingmaker.UnitLogic.Commands.UnitMoveTo",true);
  var commandType=moveType.BaseType;
  var pointType=moveType.GetConstructors().Single(c=>c.GetParameters().Length==1).GetParameters()[0].ParameterType;
  var move=moveType.GetConstructor(new[]{pointType}).Invoke(new[]{Activator.CreateInstance(pointType)});
  var owner=commandType.GetProperty("Executor",F);var player=commandType.GetField("CreatedByPlayer",F);
  owner.SetValue(move,mover,null);player.SetValue(move,true);
  var ready=child.GetMethod("IsCastingMoveReady",F);
  Func<object,object,object,bool,bool,bool,bool> check=(c,m,slot,moving,required,paired)=>
   (bool)ready.Invoke(null,new[]{c,m,slot,(object)moving,required,paired});
  Check(!(bool)commandType.GetProperty("IsRunning",F).GetValue(move,null)&&
   !(bool)commandType.GetProperty("IsStarted",F).GetValue(move,null)&&
   !(bool)commandType.GetProperty("IsFinished",F).GetValue(move,null),
   "actual native Move carrier is live before post-approach command startup");
  Check(check(move,mover,move,true,false,false),"RT native moving carrier does not require a TB ground owner");
  Check(check(move,mover,move,true,true,true),"TB moving carrier retains its exact paired owner");
  Check(!check(move,mover,move,true,true,false),"TB movement without paired owner is not ready");
  Check(!check(move,mover,move,false,false,false),"owned idle carrier cannot stand in for observed native movement");
  Check(!check(null,mover,null,true,false,false)&&!check(move,null,move,true,false,false),
   "missing carrier or mover is not ready");
  Check(!check(move,foreign,move,true,false,false),"foreign executor is not a casting fixture move");
  Check(!check(move,mover,null,true,false,false),"released Move slot is not ready");
  player.SetValue(move,false);
  Check(!check(move,mover,move,true,false,false),"AI carrier cannot stand in for normal ground input");
  player.SetValue(move,true);
  Check(!(bool)commandType.GetProperty("IsStarted",F).GetValue(move,null)&&
   !(bool)commandType.GetProperty("IsActed",F).GetValue(move,null)&&ReferenceEquals(owner.GetValue(move,null),mover),
   "readiness observation does not start, act or replace the native command");
  // Project the native terminal input only in this detached fixture; Result alone is not IsFinished.
  commandType.GetProperty("IsFinished",F).SetValue(move,true,null);
  Check(!check(move,mover,move,true,false,false),"terminal carrier cannot satisfy movement readiness");
 }
 static void ExerciseNativeGroundRefusal(Assembly native,Type child){
  var handlerType=native.GetType("Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler",true);
  var abilityType=native.GetType("Kingmaker.UnitLogic.Abilities.AbilityData",true);
  var blueprintType=native.GetType("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility",true);
  var descriptorType=native.GetType("Kingmaker.UnitLogic.UnitDescriptor",true);
  var getTarget=handlerType.GetMethod("GetTarget",F);
  var gameObjectType=getTarget.GetParameters()[0].ParameterType;
  var view=FormatterServices.GetUninitializedObject(gameObjectType);
  var input=child.GetMethod("CastingInputObject",F);
  Check(input!=null&&input.Invoke(null,new object[]{"C6C-invalid-target",view})==null,
   "compiled refusal fixture selects empty ground instead of a targetable enemy");
  foreach(var name in (string[])child.GetField("CastingCases",F).GetValue(null)){
   if(name=="C6C-invalid-target")continue;
   Check(ReferenceEquals(input.Invoke(null,new[]{(object)name,view}),view),"ordinary native unit input retained "+name);
  }
  var blueprint=FormatterServices.GetUninitializedObject(blueprintType);
  blueprintType.GetField("CanTargetFriends",F).SetValue(blueprint,true);
  var range=blueprintType.GetField("Range",F);
  range.SetValue(blueprint,Enum.Parse(range.FieldType,"Touch"));
  var caster=FormatterServices.GetUninitializedObject(descriptorType);
  var ability=abilityType.GetConstructor(new[]{blueprintType,descriptorType}).Invoke(new[]{blueprint,caster});
  Check(abilityType.GetProperty("TargetAnchor",F).GetValue(ability,null).ToString()=="Unit",
   "actual native friendly Touch spell has unit-only target anchor");
  var handler=FormatterServices.GetUninitializedObject(handlerType);
  handlerType.GetProperty("Ability",F).SetValue(handler,ability,null);
  var point=Activator.CreateInstance(getTarget.GetParameters()[1].ParameterType);
  var instruction=child.Assembly.GetType("KingmakerMountedCombat.Integration.MountedChargeBuffChildren",true)
   .GetMethod("WrapAddFact",F).GetParameters()[0].ParameterType.GetGenericArguments()[0];
  detachedGroundTarget=CopyDetachedMethod(getTarget,instruction,"DetachedGroundTarget");
  Check(detachedGroundTarget.Invoke(null,new[]{handler,null,point,ability})==null,
   "actual native GetTarget rejects empty ground with only inaccessible null Unity comparisons projected");
  detachedDeliverTouch=new System.Reflection.Emit.DynamicMethod("detached_native_touch_context",typeof(bool),
   new[]{handlerType},typeof(CastingObserverProbe).Module,true);
  var il=detachedDeliverTouch.GetILGenerator();il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4_0);il.Emit(System.Reflection.Emit.OpCodes.Ret);
  var click=CopyDetachedMethod(handlerType.GetMethod("OnClick",F),instruction,"DetachedGroundRefusal");
  // Silence external UI events in detached CLR. If refusal reaches native
  // command admission, the uninitialized caster fails rather than fabricating it.
  Check(!(bool)click.Invoke(null,new[]{handler,null,point,(object)0,false,true}),
   "actual native OnClick refuses empty ground before command or cost admission");
  Check(ReferenceEquals(handlerType.GetProperty("Ability",F).GetValue(handler,null),ability)&&
   ReferenceEquals(abilityType.GetField("Caster",F).GetValue(ability),caster),
   "native refusal preserves selected ability and exact caster ownership");
  detachedDeliverTouch=null;detachedGroundTarget=null;
 }
 static int detachedRelationshipState;
 static object detachedRelationshipRider,detachedRelationshipMount;
 static System.Collections.Generic.Dictionary<string,System.Reflection.Emit.DynamicMethod> detachedRelationshipGetters;
 public static System.Collections.Generic.IEnumerable<T> ReplaceDetachedRelationship<T>(System.Collections.Generic.IEnumerable<T> instructions,MethodBase __originalMethod){
  if(__originalMethod.Name!="IsExactDiagnosticAiIsolationRelationship")throw new InvalidOperationException("Unexpected diagnostic predicate.");
  var counts=new System.Collections.Generic.Dictionary<string,int>();
  foreach(var instruction in instructions){
   var called=typeof(T).GetField("operand").GetValue(instruction) as MethodInfo;
   if(called!=null&&called.DeclaringType.FullName=="KingmakerMountedCombat.Integration.GameMountedRelationshipService"){
    if(!detachedRelationshipGetters.ContainsKey(called.Name))throw new InvalidOperationException("Unexpected relationship boundary.");
    typeof(T).GetField("opcode").SetValue(instruction,System.Reflection.Emit.OpCodes.Call);
    typeof(T).GetField("operand").SetValue(instruction,detachedRelationshipGetters[called.Name]);
    counts[called.Name]=counts.ContainsKey(called.Name)?counts[called.Name]+1:1;
   }
   yield return instruction;
  }
  if(counts.Count!=3||counts["get_State"]!=2||counts["get_Rider"]!=1||counts["get_Mount"]!=1)
   throw new InvalidOperationException("Exact relationship predicate boundary differs.");
 }
 public static System.Collections.Generic.IEnumerable<T> VerifyDetachedRiderAiAdmission<T>(System.Collections.Generic.IEnumerable<T> instructions,MethodBase __originalMethod){
  if(__originalMethod.Name!="PrepareCombatMountRiderAiIsolation")throw new InvalidOperationException("Unexpected full rider admission method.");
  var calls=0;
  foreach(var instruction in instructions){
   var called=typeof(T).GetField("operand").GetValue(instruction) as MethodInfo;
   if(called!=null&&called.DeclaringType==__originalMethod.DeclaringType&&called.Name=="IsDiagnosticRiderAiIsolationScenario")calls++;
   yield return instruction;
  }
  if(calls!=1)throw new InvalidOperationException("Native fixture admission must call the tested scenario gate exactly once.");
 }
 public static System.Collections.Generic.IEnumerable<T> VerifyDetachedCastingEntryOrder<T>(System.Collections.Generic.IEnumerable<T> instructions,MethodBase __originalMethod){
  if(__originalMethod.Name!="TickChunk6cCasting")throw new InvalidOperationException("Unexpected casting tick method.");
  var index=0;var mountIsolation=-1;var riderIsolation=-1;var firstClick=-1;var mayRequest=0;var ready=0;
  var list=new System.Collections.Generic.List<T>();
  foreach(var instruction in instructions){
   var called=typeof(T).GetField("operand").GetValue(instruction) as MethodInfo;
   if(called!=null&&called.DeclaringType==__originalMethod.DeclaringType){
    if(called.Name=="PrepareUnmountedHorseAiIsolation"&&mountIsolation<0)mountIsolation=index;
    if(called.Name=="PrepareCombatMountRiderAiIsolation"&&riderIsolation<0)riderIsolation=index;
    if(called.Name=="TryNativeAbilityTargetClick"&&firstClick<0)firstClick=index;
    if(called.Name=="CastingEntryMayRequestMount")mayRequest++;
    if(called.Name=="CastingEntryReady")ready++;
   }
   list.Add(instruction);index++;
  }
  // Compiled fixture entry: mount lease, then rider lease, then the tested gates, and only
  // then the first native Mount click (preview.199 clicked Mount before any lease).
  if(mountIsolation<0||riderIsolation<0||firstClick<0||mayRequest!=1||ready!=1)throw new InvalidOperationException("Casting entry must call both AI leases, both tested gates exactly once and the native Mount click.");
  if(!(mountIsolation<riderIsolation&&riderIsolation<firstClick))throw new InvalidOperationException("Casting entry must own mount then rider AI isolation before its first native Mount click.");
  return list;
 }
 static void ExerciseCastingEntryOrdering(Type child,Type instruction){
  var mayMount=child.GetMethod("CastingEntryMayRequestMount",F);var ready=child.GetMethod("CastingEntryReady",F);
  Check(mayMount!=null&&mayMount.IsStatic&&ready!=null&&ready.IsStatic,"casting entry gates are pure static decisions");
  Func<bool,bool,bool,bool,bool> request=(mounted,paired,isolated,requested)=>(bool)mayMount.Invoke(null,new object[]{mounted,paired,isolated,requested});
  Func<bool,bool,bool,bool> proceed=(mounted,paired,isolated)=>(bool)ready.Invoke(null,new object[]{mounted,paired,isolated});
  Check(!request(true,false,false,false),"exploration Mount is never requested before fixture AI isolation owns both actors");
  Check(request(true,false,true,false),"isolated unmounted casting fixture requests exactly its exploration Mount");
  Check(!request(true,false,true,true),"a requested native Mount is not repeated");
  Check(!request(true,true,true,false),"a mounted pair requests no second Mount");
  Check(!request(false,false,true,false)&&!request(false,false,false,false),"unmounted casting baselines never request a Mount");
  Check(!proceed(true,false,true),"mounted baseline waits for the native Mount to settle");
  Check(proceed(true,true,true)&&proceed(false,false,true),"isolated settled pair proceeds to the native encounter");
  Check(!proceed(true,true,false)&&!proceed(false,false,false)&&!proceed(true,false,false),"no baseline proceeds without fixture AI isolation");
  CopyDetachedMethod(child.GetMethod("TickChunk6cCasting",F),instruction,"VerifyDetachedCastingEntryOrder");
  Check(true,"compiled casting entry owns both AI leases and consults the tested gates before its first native Mount click");
  Check(child.GetMethod("CaptureChunk6cCastingDeadlineProgress",F)!=null,"6C leaf deadline records bounded raw case/boundary/AI/command facts");
  // Instruments after the preview.200 native fact: no unmemorized orison, a bounded native scroll stack.
  var cases=(string[])child.GetField("CastingCases",F).GetValue(null);
  Check(cases.Length==15&&cases[4]=="C6C-scroll-interrupt-after"&&!cases.Contains("C6C-prepared-interrupt-after")&&cases.Distinct().Count()==15,"compiled 6C rows name the scroll-sourced post-commit interruption exactly once");
  Check(!child.GetFields(F).Any(f=>f.IsLiteral&&f.FieldType==typeof(string)&&(string)f.GetRawConstantValue()=="c3a8f31778c3980498d8f00c980be5f5"),"the unavailable Guidance orison is no longer a compiled fixture instrument");
  var stack=(int)child.GetField("ScrollStackCount",F).GetRawConstantValue();
  Check(stack>=8&&stack<=32,"scroll stack covers every scroll-sourced row within the bounded disposable range");
  var acquire=child.Assembly.GetType("KingmakerMountedCombat.Diagnostics.NativeCastingItemLease",true).GetMethod("Acquire",F);
  Check(acquire.GetParameters().Length==1&&acquire.GetParameters()[0].ParameterType==typeof(int),"native item lease acquires an exact bounded stack count");
  // Disposable stacks after the preview.201 native fact: the exact unit is equipped first (native
  // InsertItem splits stacks), the stack is built on that entity, and every item-sourced row
  // demands at least two units so the native spend decrements in place (no foreign refill).
  var potion=(int)child.GetField("PotionStackCount",F).GetRawConstantValue();
  Check(potion>=2&&potion<=32,"potion stack keeps the exact potion entity equipped after its one measured drink");
  CopyDetachedMethod(acquire,instruction,"VerifyDetachedAcquireOrder");
  Check(true,"compiled lease equips the exact single unit before building the disposable stack on that entity");
  var require=acquire.DeclaringType.GetMethod("RequireExactlyEquipped",F);
  Check(child.GetMethod("ExactCastingItemAbility",F)!=null&&require!=null&&require.GetParameters().Length==2&&require.GetParameters()[1].ParameterType==typeof(int),"item-sourced rows demand the exact equipped stack with a minimum unit count");
  CopyDetachedMethod(child.GetMethod("BeginCastingCase",F),instruction,"VerifyDetachedItemRowGuard");
  Check(true,"compiled case entry resolves item-sourced abilities only through the exact equipped-stack guard");
 }
 public static System.Collections.Generic.IEnumerable<T> VerifyDetachedAcquireOrder<T>(System.Collections.Generic.IEnumerable<T> instructions,MethodBase __originalMethod){
  if(__originalMethod.Name!="Acquire")throw new InvalidOperationException("Unexpected lease method.");
  var list=new System.Collections.Generic.List<T>();var index=0;var add=-1;var insert=-1;var increment=-1;var increments=0;
  foreach(var instruction in instructions){
   var called=typeof(T).GetField("operand").GetValue(instruction) as MethodInfo;
   if(called!=null){
    if(called.DeclaringType.FullName=="Kingmaker.Items.ItemsCollection"&&called.Name=="Add"&&add<0)add=index;
    if(called.DeclaringType.FullName=="Kingmaker.Items.Slots.ItemSlot"&&called.Name=="InsertItem"&&insert<0)insert=index;
    if(called.DeclaringType.FullName=="Kingmaker.Items.ItemEntity"&&called.Name=="IncrementCount"){if(increment<0)increment=index;increments++;}
   }
   list.Add(instruction);index++;
  }
  if(add<0||insert<0||increment<0||increments!=1)throw new InvalidOperationException("Lease acquisition must add, equip and stack exactly once.");
  if(!(add<insert&&insert<increment))throw new InvalidOperationException("Lease acquisition must equip the exact unit before building its stack.");
  return list;
 }
 public static System.Collections.Generic.IEnumerable<T> VerifyDetachedItemRowGuard<T>(System.Collections.Generic.IEnumerable<T> instructions,MethodBase __originalMethod){
  if(__originalMethod.Name!="BeginCastingCase")throw new InvalidOperationException("Unexpected case entry.");
  var list=new System.Collections.Generic.List<T>();var guarded=0;var raw=0;
  foreach(var instruction in instructions){
   var called=typeof(T).GetField("operand").GetValue(instruction) as MethodInfo;
   if(called!=null&&called.Name=="ExactCastingItemAbility")guarded++;
   if(called!=null&&called.Name=="get_Ability"&&called.DeclaringType.Namespace=="Kingmaker.Items")raw++;
   list.Add(instruction);
  }
  if(guarded!=1||raw!=0)throw new InvalidOperationException("Case entry must resolve item abilities only through the exact guard.");
  return list;
 }
 static void ExerciseCastingFixtureAdmission(Assembly native,Type child){
  var mod=child.Assembly;var service=mod.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
  var actor=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
  var exactRider=FormatterServices.GetUninitializedObject(actor);var exactMount=FormatterServices.GetUninitializedObject(actor);
  var foreign=FormatterServices.GetUninitializedObject(actor);var instance=FormatterServices.GetUninitializedObject(child);
  child.GetField("relationship",F).SetValue(instance,FormatterServices.GetUninitializedObject(service));
  child.GetField("rider",F).SetValue(instance,exactRider);child.GetField("horse",F).SetValue(instance,exactMount);
  detachedRelationshipGetters=new System.Collections.Generic.Dictionary<string,System.Reflection.Emit.DynamicMethod>();
  foreach(var name in new[]{"State","Rider","Mount"}){
   var getter=service.GetProperty(name,F).GetGetMethod(true);
   var method=new System.Reflection.Emit.DynamicMethod("observed_"+name,getter.ReturnType,new[]{service},typeof(CastingObserverProbe).Module,true);
   var il=method.GetILGenerator();il.Emit(System.Reflection.Emit.OpCodes.Ldsfld,typeof(CastingObserverProbe).GetField("detachedRelationship"+name,F));
   if(name!="State")il.Emit(System.Reflection.Emit.OpCodes.Castclass,actor);il.Emit(System.Reflection.Emit.OpCodes.Ret);
   detachedRelationshipGetters.Add(getter.Name,method);
  }
  var graph=mod.GetType("KingmakerMountedCombat.Integration.MountedChargeBuffChildren",true);
  var instruction=graph.GetMethod("WrapAddFact",F).GetParameters()[0].ParameterType.GetGenericArguments()[0];
  // Mock only observed relationship inputs. Keep the exact compiled scenario,
  // state and pair-identity decisions; no native state or installed code changes.
  var predicate=CopyDetachedMethod(child.GetMethod("IsExactDiagnosticAiIsolationRelationship",F),instruction,"ReplaceDetachedRelationship");
  var scenarioGate=child.GetMethod("IsDiagnosticRiderAiIsolationScenario",F);
  Check(scenarioGate!=null,"native rider AI admission declares one tested scenario gate");
  // Verify the full compiled native-fixture guard calls this tested gate.
  // Do not invoke its Unity selection/AI mutation boundaries in detached CLR.
  CopyDetachedMethod(child.GetMethod("PrepareCombatMountRiderAiIsolation",F),instruction,"VerifyDetachedRiderAiAdmission");
  var stateType=service.GetProperty("State",F).PropertyType;
  foreach(var scenario in new[]{"chunk6c-casting-rt","chunk6c-casting-tb","chunk6c-casting-unmounted-rt","chunk6c-casting-unmounted-tb"}){
   var request=Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Diagnostics.RuntimeRequest",true));
   request.GetType().GetProperty("Scenario",F).SetValue(request,scenario,null);child.GetField("request",F).SetValue(instance,request);
   Check((bool)scenarioGate.Invoke(instance,null),"full pre-target rider scenario admission "+scenario);
   detachedRelationshipState=(int)Enum.Parse(stateType,"Mounted");detachedRelationshipRider=exactRider;detachedRelationshipMount=exactMount;
   Check((bool)predicate.Invoke(null,new[]{instance}),"exact mounted casting AI lease admission "+scenario);
   detachedRelationshipRider=foreign;Check(!(bool)predicate.Invoke(null,new[]{instance}),"foreign rider refused "+scenario);
   detachedRelationshipRider=exactRider;detachedRelationshipMount=foreign;
   Check(!(bool)predicate.Invoke(null,new[]{instance}),"foreign mount refused "+scenario);detachedRelationshipMount=exactMount;
   detachedRelationshipState=(int)Enum.Parse(stateType,"Faulted");Check(!(bool)predicate.Invoke(null,new[]{instance}),"faulted relationship refused "+scenario);
   detachedRelationshipState=(int)Enum.Parse(stateType,"Unmounted");Check((bool)predicate.Invoke(null,new[]{instance}),"existing unmounted isolation preserved "+scenario);
  }
  var legacyRequest=Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Diagnostics.RuntimeRequest",true));
  legacyRequest.GetType().GetProperty("Scenario",F).SetValue(legacyRequest,child.GetField("TurnBasedScenario",F).GetRawConstantValue(),null);
  child.GetField("request",F).SetValue(instance,legacyRequest);
  Check((bool)scenarioGate.Invoke(instance,null),"legacy native paired fixture scenario remains admitted");
  legacyRequest.GetType().GetProperty("Scenario",F).SetValue(legacyRequest,"unregistered-casting-scenario",null);
  Check(!(bool)scenarioGate.Invoke(instance,null),"unregistered scenario cannot acquire rider AI ownership");
  detachedRelationshipState=(int)Enum.Parse(stateType,"Mounted");
  Check(!(bool)predicate.Invoke(null,new[]{instance}),"unregistered scenario cannot isolate a mounted pair");
  detachedRelationshipGetters=null;detachedRelationshipRider=null;detachedRelationshipMount=null;
  var wait=child.GetMethod("WaitForNativeCastingPrincipal",F);var advances=0;Action advance=()=>advances++;
  Check((bool)wait.Invoke(null,new object[]{true,false,false,advance})&&advances==1,
   "other native turn advances even while rider cannot act; rider still waits");
  advances=0;Check((bool)wait.Invoke(null,new object[]{true,true,false,advance})&&advances==0,
   "unavailable principal waits without an End Turn request");
  Check(!(bool)wait.Invoke(null,new object[]{true,true,true,advance})&&advances==0,
   "ready native principal proceeds without an extra turn request");
  Check(!(bool)wait.Invoke(null,new object[]{false,false,true,advance})&&advances==0,
   "RT readiness does not request a TB turn");
 }
 static void ExerciseNativeActivationCleanup(Assembly native,Type itemLease){
  var graph=itemLease.Assembly.GetType("KingmakerMountedCombat.Integration.MountedChargeBuffChildren",true);
  var instruction=graph.GetMethod("WrapAddFact",F).GetParameters()[0].ParameterType.GetGenericArguments()[0];
  // Preserve both exact native Dispose bodies and every disposed assertion.
  // Only inaccessible Unity boundaries are mocked; no installed assembly,
  // native game or product code is patched by this standalone CLR projection.
  projectedFactDispose=CopyDetachedDispose(native,instruction,0x06009A5A,"ReplaceDetachedDestroy");
  var activationDispose=CopyDetachedDispose(native,instruction,0x06002AC4,"RedirectDetachedBaseDispose");
  projectedToggle=CopyDetachedDispose(native,instruction,0x06002AA0,"ReplaceDetachedClock");
  var turnOff=CopyDetachedMethod(itemLease.GetMethod("TurnOffExactActivation",F),instruction,"RedirectExactToggle");
  try {ExerciseNativeActivationCleanupBody(native,itemLease,activationDispose,turnOff);}
  finally {projectedFactDispose=null;projectedToggle=null;}
 }
 static void ExerciseNativeActivationCleanupBody(Assembly native,Type itemLease,System.Reflection.Emit.DynamicMethod activationDispose,System.Reflection.Emit.DynamicMethod turnOff){
  var activationType=native.GetType("Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbility",true);
  var factType=native.GetType("Kingmaker.Blueprints.Facts.Fact",true);
  var descriptor=native.GetType("Kingmaker.UnitLogic.UnitDescriptor",true);
  var itemType=native.GetType("Kingmaker.Items.ItemEntityUsable",true);
  var components=factType.GetField("m_Components",F);
  var toggle=activationType.GetField("m_IsOn",F);
  var source=activationType.GetProperty("SourceItem",F);
  var owner=activationType.BaseType.GetField("<Owner>k__BackingField",F);
  var isOn=activationType.GetProperty("IsOn",F);var active=activationType.GetProperty("Active",F);
  var disposed=activationType.GetProperty("IsDisposed",F);
  Check(isOn.SetMethod.MetadataToken==0x06002AA0&&activationType.GetMethod("Dispose",F).MetadataToken==0x06002AC4,
   "fixture pins the normal native activation toggle and terminal disposal");
  var oldActivation=FormatterServices.GetUninitializedObject(activationType);
  components.SetValue(oldActivation,Activator.CreateInstance(components.FieldType));toggle.SetValue(oldActivation,true);
  activationDispose.Invoke(null,new[]{oldActivation});
  Check((bool)disposed.GetValue(oldActivation,null)&&!(bool)active.GetValue(oldActivation,null)&&(bool)isOn.GetValue(oldActivation,null),
   "exact native disposal IL with Unity destruction boundary mocked retains IsOn, reproducing preview193 refusal");

  // The complete native setter uses only a mocked external clock read.
   var exactOwner=FormatterServices.GetUninitializedObject(descriptor);
   var exactItem=FormatterServices.GetUninitializedObject(itemType);
   var activation=FormatterServices.GetUninitializedObject(activationType);
   components.SetValue(activation,Activator.CreateInstance(components.FieldType));
   owner.SetValue(activation,exactOwner);source.SetValue(activation,exactItem,null);toggle.SetValue(activation,true);
   var ready=activationType.GetProperty("ReadyToStart",F);ready.SetValue(activation,true,null);
   var charges=itemType.GetProperty("Charges",F);var initialCharges=charges.GetValue(exactItem,null);
   var refused=false;
   try {turnOff.Invoke(null,new[]{activation,FormatterServices.GetUninitializedObject(itemType),exactOwner});}
   catch(TargetInvocationException e){refused=e.InnerException is InvalidOperationException;}
   Check(refused&&(bool)isOn.GetValue(activation,null)&&ReferenceEquals(source.GetValue(activation,null),exactItem),
    "foreign activation source refuses before changing the exact native toggle");
   refused=false;
   try {turnOff.Invoke(null,new[]{activation,exactItem,FormatterServices.GetUninitializedObject(descriptor)});}
   catch(TargetInvocationException e){refused=e.InnerException is InvalidOperationException;}
   Check(refused&&(bool)isOn.GetValue(activation,null),"changed native activation owner refuses without mutation");
   turnOff.Invoke(null,new[]{activation,exactItem,exactOwner});
   Check(!(bool)isOn.GetValue(activation,null)&&!(bool)ready.GetValue(activation,null),
    "production helper with full native toggle IL stops readiness before exact item disposal");
   activationDispose.Invoke(null,new[]{activation});
   Check((bool)disposed.GetValue(activation,null)&&!(bool)active.GetValue(activation,null)&&!(bool)isOn.GetValue(activation,null),
    "normal native toggle and exact disposal IL meet unchanged activation postconditions");
   turnOff.Invoke(null,new[]{activation,exactItem,exactOwner});
   Check(Equals(initialCharges,charges.GetValue(exactItem,null))&&detachedClock==TimeSpan.FromSeconds(13),
    "repeat fixture release preserves item charges without creating a game or mutating its singleton");

 }
 static void ExerciseCastingLifeSafety(Assembly native,Type child){
  var plan=child.GetMethod("PlanCastingMainCharacterDamageCap",F);
  Func<int,int,int,int,float,int?> cap=(hp,con,damage,temp,factor)=>{
   var result=plan.Invoke(null,new object[]{hp,con,damage,temp,factor});
   return result==null?(int?)null:(int)result;
  };
  Check(cap(45,11,1,0,1f)==-1,"original Druid window has a native increment cap");
  foreach(var factor in new[]{.5f,.7f,1f,1.5f,2f}){
   var value=cap(45,11,1,0,factor);
   Check(value.HasValue,"finite native difficulty has a safe bounded fixture window");
   var requested=45-value.Value;
   var nativeMaximum=Math.Max(1,(int)(requested*factor));
   Check(1+nativeMaximum>=45&&1+nativeMaximum<56,"native float/difficulty maximum stays unconscious and strictly below death");
  }
  foreach(var facts in new[]{new[]{0,11,0,0},new[]{45,1,0,0},new[]{45,11,-1,0},
    new[]{45,11,45,0},new[]{45,11,0,1},new[]{int.MaxValue,11,0,0}})
   Check(!cap(facts[0],facts[1],facts[2],facts[3],1f).HasValue,"invalid life/temporary-HP/overflow window remains refused");
  foreach(var factor in new[]{0f,-1f,float.NaN,float.PositiveInfinity,1000f,float.Epsilon})
   Check(!cap(45,11,1,0,factor).HasValue,"unsafe native difficulty does not create a main-character stimulus");
  var actor=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
  var source=FormatterServices.GetUninitializedObject(actor);
  var subject=FormatterServices.GetUninitializedObject(actor);
  var create=child.GetMethod("CreateCastingIncapacityRule",F);
  var refused=false;
  try{create.Invoke(null,new object[]{source,subject,46,true,null});}
  catch(TargetInvocationException e){refused=e.InnerException is InvalidOperationException;}
  Check(refused,"unclamped main-character damage is still refused before a native rule is created");
  var rule=create.Invoke(null,new object[]{source,subject,46,true,cap(45,11,1,0,1f)});
  var type=rule.GetType();
  Check(type.FullName=="Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage","fixture builds the actual pinned native damage rule");
  Check(ReferenceEquals(type.GetField("Initiator",F).GetValue(rule),source)&&
   ReferenceEquals(type.GetField("Target",F).GetValue(rule),subject),"native damage retains exact source and subject");
  Check((int)type.GetProperty("MinHPAfterDamage",F).GetValue(rule,null)==-1&&
   !(bool)type.GetProperty("IsFake",F).GetValue(rule,null)&&
   (int)type.GetProperty("Damage",F).GetValue(rule,null)==0,"native cap is installed before dispatch; no damage or fake result is fabricated");
  var ordinary=create.Invoke(null,new object[]{source,subject,46,false,null});
  Check(type.GetProperty("MinHPAfterDamage",F).GetValue(ordinary,null)==null,
   "existing non-main native stimulus retains its original uncapped rule");
  var health=child.GetMethod("CastingHealthBoundarySettled",F);
  Check((bool)health.Invoke(null,new object[]{true,true,true,false,false}),"health restoration admits only the settled native command/process boundary");
  foreach(var facts in new[]{new[]{false,true,true,false,false},new[]{true,false,true,false,false},
    new[]{true,true,false,false,false},new[]{true,true,true,true,false},new[]{true,true,true,false,true}})
   Check(!(bool)health.Invoke(null,facts.Cast<object>().ToArray()),"live native command/process/effect retains fixture health cleanup debt");
  var encounter=child.GetMethod("FixtureNativeEncounterPending",F);
  foreach(var facts in new[]{new[]{true,false,true,false,false},new[]{true,false,false,true,false},
    new[]{true,false,false,false,true},new[]{false,true,true,false,false}})
   Check((bool)encounter.Invoke(null,facts.Cast<object>().ToArray()),"casting/obstruction cleanup waits for its real native encounter boundary");
  Check(!(bool)encounter.Invoke(null,new object[]{true,false,false,false,false}),
   "fully settled casting encounter allows outer configuration restoration");
  Check(!(bool)encounter.Invoke(null,new object[]{false,false,true,true,true}),
   "unrelated historical fixture completion policy remains unchanged");
 }
 public static void Run(string managed,string product){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
   var leaf=new AssemblyName(e.Name).Name+".dll";
   foreach(var dir in new[]{managed,Path.Combine(managed,"UnityModManager")}){var p=Path.Combine(dir,leaf);if(File.Exists(p))return Assembly.LoadFrom(p);}return null;
  };
  var native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));
  Check(native.ManifestModule.ModuleVersionId.ToString()=="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7","exact native assembly");
  var mod=Assembly.LoadFrom(product);var type=mod.GetType("KingmakerMountedCombat.Diagnostics.NativeCastingItemTrace",true);
  var child=mod.GetType("KingmakerMountedCombat.Diagnostics.Phase3dHorseScenarioTranche",true);
  var parent=mod.GetType("KingmakerMountedCombat.Diagnostics.Chunk6aMammothScenarioEngine",true);
  ExerciseCastingFixtureAdmission(native,child);
  ExerciseCastingEntryOrdering(child,mod.GetType("KingmakerMountedCombat.Integration.MountedChargeBuffChildren",true).GetMethod("WrapAddFact",F).GetParameters()[0].ParameterType.GetGenericArguments()[0]);
  ExerciseNativeGroundRefusal(native,child);
  ExerciseNativeMoveReadiness(native,child);
  ExerciseCastingLifeSafety(native,child);
  foreach(var scenario in new[]{"chunk6c-casting-rt","chunk6c-casting-tb","chunk6c-casting-unmounted-rt","chunk6c-casting-unmounted-tb"}){
   Check((bool)parent.GetMethod("SupportsScenario",F).Invoke(null,new object[]{scenario}),"native original-pair engine accepts "+scenario);
   Check((bool)child.GetMethod("IsChunk6cCastingScenario",F).Invoke(null,new object[]{scenario}) &&
    !(bool)child.GetMethod("IsChunk6aCombatMountScenario",F).Invoke(null,new object[]{scenario}),"compiled child chooses casting rather than Mount/Dismount for "+scenario);
  }
  foreach(var field in new[]{"RealTimeScenario","TurnBasedScenario"}){
   var scenario=(string)parent.GetField(field,F).GetRawConstantValue();
   Check((bool)child.GetMethod("IsChunk6aCombatMountScenario",F).Invoke(null,new object[]{scenario}),"existing native Mammoth Mount/Dismount classification retained "+scenario);
  }
  var slotOwner=native.GetType("Kingmaker.Items.Slots.ItemSlot",true).GetField("Owner",F);
  var descriptor=native.GetType("Kingmaker.UnitLogic.UnitDescriptor",true);
  Check(slotOwner.MetadataToken==0x0400502C&&slotOwner.FieldType==descriptor,
   "original usable slots retain the exact native UnitDescriptor owner");
  var body=descriptor.GetProperty("Body",F);
  Check(body.MetadataToken==0x17000642&&body.PropertyType.FullName=="Kingmaker.Items.UnitBody",
   "original owner body identity is pinned");
  var quickSlots=body.PropertyType.GetProperty("QuickSlots",F);
  Check(quickSlots.MetadataToken==0x17001555&&quickSlots.PropertyType.GetGenericArguments()[0].FullName=="Kingmaker.Items.Slots.UsableSlot",
   "original owner quick-slot container is pinned");
  var itemLease=mod.GetType("KingmakerMountedCombat.Diagnostics.NativeCastingItemLease",true);
  try {ExerciseNativeActivationCleanup(native,itemLease);} catch(Exception e) {Console.WriteLine(e.ToString());throw;}
  var remove=(MethodInfo)itemLease.GetField("NativeRemoveItem",F).GetValue(null);
  Check(remove.MetadataToken==0x06007C7E&&remove.GetParameters().Length==2&&remove.GetParameters()[0].Name=="raiseEvent"&&remove.GetParameters()[1].Name=="autoMerge",
   "fixture cleanup pins the native two-argument equipment-event overload");
  var emptySlot=FormatterServices.GetUninitializedObject(native.GetType("Kingmaker.Items.Slots.UsableSlot",true));
  Check(!(bool)itemLease.GetMethod("RemoveExactSlot",F).Invoke(null,new[]{emptySlot}),
   "actual native empty-slot refusal is observed rather than converted to cleanup success");
  var actor=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
  var rider=FormatterServices.GetUninitializedObject(actor);var mount=FormatterServices.GetUninitializedObject(actor);
  var owner=Activator.CreateInstance(type,F,null,new[]{rider,mount},null);
  try {
   var capture=(JObject)type.GetMethod("Capture",F).Invoke(owner,null);
   Check((int)capture["faults"]==0&&(int)capture["dropped"]==0,"fresh bounded observer complete");
   Check(((JArray)capture["hooks"]).Count==6,"all six exact native observation hooks installed");
   Check(type.GetMethod("ConcentrationAfter",F).GetParameters().Length==1,"void native concentration postfix collects no invented result");
   var item=native.ManifestModule.ResolveMethod(0x06007B74);
   Check(item.GetParameters()[0].Name=="user","item spending observer binds exact native user parameter");
   var duplicateRefused=false;try{Activator.CreateInstance(type,F,null,new[]{rider,mount},null);}catch(TargetInvocationException e){duplicateRefused=e.InnerException is InvalidOperationException;}
   Check(duplicateRefused,"second observer cannot replace current owner");
   var before=(int)capture["faults"];
   foreach(var eventType in new[]{"Kingmaker.RuleSystem.Rules.Abilities.RuleCastSpell","Kingmaker.RuleSystem.Rules.Damage.RuleDealDamage","Kingmaker.RuleSystem.Rules.Damage.RuleHealDamage","Kingmaker.RuleSystem.Rules.Abilities.RuleCheckConcentration","Kingmaker.RuleSystem.Rules.Abilities.RuleCheckCastingDefensively","Kingmaker.RuleSystem.Rules.RuleSummonUnit"}){
    var eventNative=native.GetType(eventType,true);var method=type.GetMethod("OnEventDidTrigger",F,null,new[]{eventNative},null);
    method.Invoke(owner,new object[]{null});
   }
   var faulted=(JObject)type.GetMethod("Capture",F).Invoke(owner,null);
   Check((int)faulted["faults"]==before+6,"rule observation failures remain explicit without escaping into native callbacks");
   Check(!(bool)type.GetProperty("Complete",F).GetValue(owner,null),"observation failure cannot pass as complete evidence");
  } finally {((IDisposable)owner).Dispose();}
  var closed=(JObject)type.GetMethod("Capture",F).Invoke(owner,null);
  Check((bool)closed["identityRegistry"]["released"]&&(int)closed["identityRegistry"]["retainedCount"]==0,"closed trace releases exact retained identities");
  ((IDisposable)owner).Dispose();
  Check(JToken.DeepEquals(closed,(JObject)type.GetMethod("Capture",F).Invoke(owner,null)),"repeat disposal preserves immutable terminal evidence");
  var next=Activator.CreateInstance(type,F,null,new[]{rider,mount},null);((IDisposable)next).Dispose();
  Check(type.GetField("active",F).GetValue(null)==null,"observer unpatch and retirement permit next bounded fixture");
  Console.WriteLine("TOTAL PASS="+passed+" FAIL=0");
 }
}
"@
try { [CastingObserverProbe]::Run($managed,(Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll")) } catch { $cause=$_.Exception; while($null-ne$cause){[Console]::WriteLine($cause.ToString());$cause=$cause.InnerException};throw }