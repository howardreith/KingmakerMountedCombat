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
[CastingObserverProbe]::Run($managed,(Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"))