[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string]$Configuration='Release',
    # Another build of the product to judge with the same probe (the old/new comparison of Sprint 7).
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
# Known issues Sprint 7 (U01-U04): detached contracts of the mounted presentation seams. The product's compiled hooks,
# policies and services run on genuine native objects (prediction actions, action-bar slots, ability data, progression
# data and level entries, tracker view models, units); only the engine boundary is substituted (the game instance, its
# turn-based state and the facts' liveness). Real Harmony12 patches of every new seam are constructed. No game is
# launched and no screen is drawn: this is not native gameplay or visual evidence.
Add-Type -TypeDefinition @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;
public static class MountedPresentationProbe {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
 static int passed,sectionFailures;
 static void Check(bool ok,string text){if(!ok)throw new InvalidOperationException("FAIL "+text);passed++;Console.WriteLine("PASS "+text);}
 static void Section(string name,Action body){
  try{body();}
  catch(Exception error){
   var inner=error;while(inner is TargetInvocationException&&inner.InnerException!=null)inner=inner.InnerException;
   sectionFailures++;Console.WriteLine("FAIL SECTION "+name+": "+inner.Message);
  }
 }
 static object Blank(Type t){return FormatterServices.GetUninitializedObject(t);}
 static FieldInfo Field(Type t,string name){for(;t!=null;t=t.BaseType){var f=t.GetField(name,F|BindingFlags.DeclaredOnly);if(f!=null)return f;}throw new MissingFieldException(name);}
 static void Set(object o,string n,object v){Field(o.GetType(),n).SetValue(o,v);}
 static object Get(object o,string n){return Field(o.GetType(),n).GetValue(o);}
 static void SetStatic(Type t,string n,object v){Field(t,n).SetValue(null,v);}
 // A field write by emitted stfld: reflection's SetValue would run the declaring type's static constructor (the game's needs the engine).
 static void SetDirect(object o,string n,object v){
  var f=Field(o.GetType(),n);
  var dm=new DynamicMethod("kmc-probe-stfld",typeof(void),new[]{typeof(object),typeof(object)},typeof(MountedPresentationProbe).Module,true);
  var il=dm.GetILGenerator();il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Castclass,f.DeclaringType);il.Emit(OpCodes.Ldarg_1);
  il.Emit(f.FieldType.IsValueType?OpCodes.Unbox_Any:OpCodes.Castclass,f.FieldType);il.Emit(OpCodes.Stfld,f);il.Emit(OpCodes.Ret);
  ((Action<object,object>)dm.CreateDelegate(typeof(Action<object,object>)))(o,v);
 }
 static object Call(object o,string n,params object[] args){
  var m=o.GetType().GetMethods(F).FirstOrDefault(x=>x.Name==n&&x.GetParameters().Length==args.Length);
  if(m==null)throw new MissingMethodException(o.GetType().Name,n);
  try{return m.Invoke(o,args);}
  catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}
 }
 static object CallStatic(Type t,string n,params object[] args){
  var m=t.GetMethods(F).FirstOrDefault(x=>x.Name==n&&x.GetParameters().Length==args.Length);
  if(m==null)throw new MissingMethodException(t.Name,n);
  try{return m.Invoke(null,args);}
  catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}
 }
 static bool Has(Type t,string n){return t!=null&&t.GetMethods(F).Any(m=>m.Name==n);}
 sealed class Recorder:RealProxy {
  internal readonly List<string> Calls=new List<string>();
  internal Recorder(Type t):base(t){}
  public override IMessage Invoke(IMessage m){var call=(IMethodCallMessage)m;var text=call.MethodName+": "+(call.ArgCount>0?Convert.ToString(call.GetArg(0)):"");
   if(call.ArgCount>1)text+=" "+Convert.ToString(call.GetArg(1));Calls.Add(text);
   if(call.MethodName!="GetType")Console.WriteLine("product log "+text);return new ReturnMessage(null,null,0,null,call);}
 }
 static List<string> Calls(MethodBase method){
  var il=method.GetMethodBody().GetILAsByteArray();var names=new List<string>();
  for(var i=0;i+4<il.Length;i++){
   if(il[i]!=0x28&&il[i]!=0x6F&&il[i]!=0x73)continue;
   try{var m=method.Module.ResolveMethod(BitConverter.ToInt32(il,i+1));names.Add(m.DeclaringType.Name+"."+m.Name);i+=4;}catch(Exception){}
  }
  return names;
 }
 static List<int> Int32Operands(MethodBase method){
  var il=method.GetMethodBody().GetILAsByteArray();var values=new List<int>();
  for(var i=0;i+4<il.Length;i++)if(il[i]==0x20)values.Add(BitConverter.ToInt32(il,i+1));
  return values;
 }

 static Assembly native,mod;static string managed;
 static Type unitType,patchMethods,patchBridge,controller;
 static object harmony;static MethodInfo harmonyPatch;static Type harmonyMethod;
 static int ids;
 static object Unit(string label){var unit=Blank(unitType);Set(unit,"<UniqueId>k__BackingField",label+"-"+(++ids).ToString("D4"));return unit;}
 static string Id(object unit){return (string)Get(unit,"<UniqueId>k__BackingField");}

 // A probe-owned patch of the engine boundary only: whether turn-based combat is running (native reads the settings root).
 public static bool TurnBasedNow=true;
 public static bool TurnBasedPrefix(ref bool __result){__result=TurnBasedNow;return false;}
 public static bool PartyMember=true;
 public static bool PlayerFactionPrefix(ref bool __result){__result=PartyMember;return false;}
 static object game,combat;
 static void Engine(){
  var gameType=native.GetType("Kingmaker.Game",true);
  game=Blank(gameType);
  // The game instance itself: native's static constructor needs the engine, so its getter returns this blank instance.
  var instance=gameType.GetProperty("Instance").GetGetMethod();
  harmonyPatch.Invoke(harmony,new object[]{instance,Activator.CreateInstance(harmonyMethod,new object[]{ReturnPrefix(instance,"GameInstance")}),null,null});
  combat=Blank(native.GetType("TurnBased.Controllers.CombatController",true));
  SetDirect(game,"TurnBasedCombatController",combat);Set(combat,"<RoundNumber>k__BackingField",3);
  var tbc=native.GetType("TurnBased.Controllers.CombatController",true).GetMethod("IsInTurnBasedCombat",F);
  harmonyPatch.Invoke(harmony,new object[]{tbc,Activator.CreateInstance(harmonyMethod,new object[]{typeof(MountedPresentationProbe).GetMethod("TurnBasedPrefix")}),null,null});
  // Whether a descriptor is a party member (native compares its faction with the game's blueprint root).
  var faction=native.GetType("Kingmaker.UnitLogic.UnitDescriptor",true).GetProperty("IsPlayerFaction",F).GetGetMethod(true);
  harmonyPatch.Invoke(harmony,new object[]{faction,Activator.CreateInstance(harmonyMethod,new object[]{typeof(MountedPresentationProbe).GetMethod("PlayerFactionPrefix")}),null,null});
 }

 // ---- Pairs: genuine relationship services, registry owners and participants over native units ---------------------
 sealed class Pair{internal object Rider,Mount,Relationship,Participant,Owner,Activation,Partner,Services;}
 static object world,registry;static IList pairs;
 static object Settings(){var s=Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Diagnostics.DiagnosticSettings",true));
  s.GetType().GetProperty("EnablePairedActivation").SetValue(s,true,null);return s;}
 static void World(){
  var worldType=mod.GetType("KingmakerMountedCombat.Integration.MountedWorldService",true);
  world=Blank(worldType);
  var relType=mod.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
  var regType=mod.GetType("KingmakerMountedCombat.Domain.MountedPairRegistry`2",true).MakeGenericType(unitType,relType);
  var idFunc=typeof(Func<,>).MakeGenericType(unitType,typeof(string));
  registry=Activator.CreateInstance(regType,F,null,new object[]{Delegate.CreateDelegate(idFunc,typeof(MountedPresentationProbe).GetMethod("UnitId",F))},null);
  Set(world,"Registry",registry);
  pairs=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(mod.GetType("KingmakerMountedCombat.Integration.MountedPairServices",true)));
  Set(world,"pairs",pairs);
 }
 static string UnitId(object unit){return unit==null?null:Id(unit);}
 static Pair NewPair(string label,long generation,int state){
  var p=new Pair{Rider=Unit(label+"-rider"),Mount=Unit(label+"-mount")};
  var relType=mod.GetType("KingmakerMountedCombat.Integration.GameMountedRelationshipService",true);
  p.Relationship=Blank(relType);
  var runtime=Blank(mod.GetType("KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime",true));
  Set(runtime,"rider",p.Rider);Set(runtime,"mount",p.Mount);Set(p.Relationship,"runtime",runtime);
  var coordinator=Blank(mod.GetType("KingmakerMountedCombat.Domain.MountedRelationshipCoordinator",true));
  Set(p.Relationship,"coordinator",coordinator);SetState(p,state);
  Set(p.Relationship,"mountedPairGeneration",generation);
  Set(p.Relationship,"<World>k__BackingField",world);
  var ownerType=mod.GetType("KingmakerMountedCombat.Domain.MountedPairOwner`2",true).MakeGenericType(unitType,relType);
  Func<bool> yes=()=>true;
  p.Owner=Activator.CreateInstance(ownerType,F,null,new object[]{new object(),"campaign",p.Rider,p.Mount,Id(p.Rider),Id(p.Mount),generation,p.Relationship,yes,yes,yes,null},null);
  Set(p.Relationship,"<PairOwner>k__BackingField",p.Owner);
  var actors=(IDictionary)Get(registry,"actors");actors[Id(p.Rider)]=p.Owner;actors[Id(p.Mount)]=p.Owner;
  // The pair's participant: its paired activation (principal rider, partner mount) and its mount's private context.
  var participantType=mod.GetType("KingmakerMountedCombat.Integration.MountedPairTurnParticipant",true);
  p.Participant=Blank(participantType);
  Set(p.Participant,"settings",Settings());Set(p.Participant,"relationship",p.Relationship);
  Set(p.Participant,"movementState",Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Integration.MountedMovementStateAdapter",true),true));
  var activationType=mod.GetType("KingmakerMountedCombat.Domain.PairedActivation`2",true).MakeGenericType(unitType,native.GetType("TurnBased.Controllers.TurnController",true));
  p.Activation=Activator.CreateInstance(activationType,new[]{p.Rider,p.Mount});
  Set(p.Participant,"activation",p.Activation);Set(p.Participant,"splitReleaseRound",-1);
  p.Partner=Blank(native.GetType("TurnBased.Controllers.TurnController",true));
  Set(p.Partner,"Unit",p.Mount);Set(p.Partner,"<ActionsStates>k__BackingField",States());
  Set(p.Participant,"partnerContext",p.Partner);
  var services=Activator.CreateInstance(mod.GetType("KingmakerMountedCombat.Integration.MountedPairServices",true),true);
  Set(services,"Relationship",p.Relationship);Set(services,"Participant",p.Participant);
  pairs.Add(services);Set(world,"pairSnapshot",null);p.Services=services;
  return p;
 }
 static void SetState(Pair p,int state){
  var stateType=mod.GetType("KingmakerMountedCombat.Domain.RelationshipState",true);
  Set(Get(p.Relationship,"coordinator"),"<State>k__BackingField",Enum.ToObject(stateType,state));
 }
 static int Mounted(){return (int)Enum.Parse(mod.GetType("KingmakerMountedCombat.Domain.RelationshipState",true),"Mounted");}
 static int Unmounted(){return (int)Enum.Parse(mod.GetType("KingmakerMountedCombat.Domain.RelationshipState",true),"Unmounted");}

 // ---- U01: the mount's prediction actions through the native UpdateRemainingDistance -------------------------------
 static Type combatActionType;
 static object Action(){
  var activity=combatActionType.GetNestedType("ActivityState");var nullable=typeof(Nullable<>).MakeGenericType(activity);
  return Activator.CreateInstance(combatActionType,new object[]{null,null,null,(float?)50f});
 }
 static object States(){
  var statesType=native.GetType("Kingmaker.TurnBasedMode.ActionsState",true);
  var actions=Blank(statesType.GetNestedType("Actions"));
  foreach(var f in actions.GetType().GetFields(F))if(f.FieldType==combatActionType)f.SetValue(actions,Action());
  return Activator.CreateInstance(statesType,new object[]{actions,false});
 }
 static object Move(object states){return states.GetType().GetProperty("Move").GetValue(states,null);}
 static object Standard(object states){return states.GetType().GetProperty("Standard").GetValue(states,null);}
 static object PartnerStates(Pair p){return Get(p.Partner,"<ActionsStates>k__BackingField");}
 static float? Remaining(object action){return (float?)Get(action,"RemainingMoveDistance");}
 static float? Write(object action,float? feet,bool hasMove=true){
  combatActionType.GetMethod("UpdateRemainingDistance",F).Invoke(action,new object[]{feet,hasMove});return Remaining(action);
 }
 static object Allocation(Pair p,string grant,bool prepared,float forced,float moved){
  var adapter=Get(p.Participant,"movementState");
  var allocation=Activator.CreateInstance(adapter.GetType().GetNestedType("Allocation",F),true);
  Set(allocation,"GrantIdentity",grant);Set(allocation,"Prepared",prepared);
  var movement=Get(allocation,"Movement");Set(movement,"TimeForced",forced);Set(movement,"TimeMoved",moved);
  ((IDictionary)Get(adapter,"allocations"))[p.Mount]=allocation;
  return movement;
 }
 static string Identity(Pair p){return (string)p.Activation.GetType().GetProperty("Identity").GetValue(p.Activation,null);}
 static void NextActivation(Pair p){Set(p.Activation,"<Sequence>k__BackingField",(long)Get(p.Activation,"<Sequence>k__BackingField")+1);}

 static void ProbeMovementDisplay(){
  var a=NewPair("u01a",1,Mounted());var b=NewPair("u01b",1,Mounted());
  var dispatcher=Blank(mod.GetType("KingmakerMountedCombat.Integration.UnifiedMountedTurnCoordinator",true));
  var list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(a.Participant.GetType()));list.Add(a.Participant);list.Add(b.Participant);
  Set(dispatcher,"participants",list);
  SetStatic(patchBridge,"UnifiedTurn",dispatcher);
  var prefix=patchMethods.GetMethod("PairedMoveDistancePrefix",F);
  var original=combatActionType.GetMethod("UpdateRemainingDistance",F);
  Check(original.MetadataToken==0x0600704F,"native CombatAction.UpdateRemainingDistance is the pinned 0x0600704F");
  if(prefix!=null)harmonyPatch.Invoke(harmony,new object[]{original,Activator.CreateInstance(harmonyMethod,new object[]{prefix}),null,null});
  Console.WriteLine("U01 product seam "+(prefix==null?"absent: native UpdateRemainingDistance runs alone":"patched as a real Harmony12 prefix"));
  var mountMove=Move(PartnerStates(a));var mountStandard=Standard(PartnerStates(a));
  // 1. Delivered Charge: forced travel recorded on the allocation granted to this activation.
  var movement=Allocation(a,Identity(a),true,.6f,.6f);
  Check(Write(mountMove,32f)==0f,"U01 case 1: after a delivered Charge the mount's Move shows no usable distance (native wrote 32 ft)");
  Check(Write(mountStandard,32f)==0f,"U01 case 1: after a delivered Charge the mount's Standard movement shows no usable distance");
  Check((bool)Get(mountMove,"HasMovePossibility"),"U01: native's own move-possibility argument passes unchanged");
  Check((float)Get(movement,"TimeMoved")==.6f&&(float)Get(movement,"TimeForced")==.6f,"U01: TimeMoved and TimeForced are never written");
  // 2. Travelled and then cancelled: the forced travel stays (nothing is refunded).
  Allocation(a,Identity(a),true,.2f,.2f);
  Check(Write(mountMove,40f)==0f,"U01 case 2: a Charge that travelled and was cancelled shows no usable distance for the rest of the activation");
  // 3 and 4. Refused, or never obtained movement ownership: no forced travel.
  Allocation(a,Identity(a),true,0f,0f);
  Check(Write(mountMove,32f)==32f,"U01 case 3: a refused Charge keeps native's distance");
  Allocation(a,Identity(a),true,0f,.4f);
  Check(Write(mountMove,32f)==32f,"U01 case 4: a Charge that never obtained movement ownership keeps native's distance");
  // 5. The next lawful activation: an allocation of the earlier activation closes nothing.
  Allocation(a,Identity(a),true,.6f,.6f);NextActivation(a);
  Check(Write(mountMove,50f)==50f,"U01 case 5: the next activation shows native's distance again");
  Allocation(a,Identity(a),true,.6f,.6f);
  // 6-8. Rider or mount selected: both read the mount's own prediction actions; the rider's actions stay native.
  var riderStates=States();
  Check(Write(Move(riderStates),30f)==30f&&Write(Standard(riderStates),30f)==30f,"U01 cases 6-8: the rider's own actions keep native distances whichever actor is selected");
  Check(Write(mountMove,30f)==0f,"U01 cases 6-8: the mount's actions (shown for the rider-selected ground preview and the mount-selected panel) read the projection");
  // 9 and 10. Another pair and an unmounted actor.
  Allocation(b,Identity(b),true,0f,0f);
  Check(Write(Move(PartnerStates(b)),45f)==45f,"U01 case 9: another pair's mount keeps native's distance while this pair's is closed");
  Allocation(b,Identity(b),true,.5f,.5f);Allocation(a,Identity(a),true,0f,0f);
  Check(Write(mountMove,45f)==45f&&Write(Move(PartnerStates(b)),45f)==0f,"U01 case 9: a closure never crosses pairs");
  Check(Write(Action(),25f)==25f,"U01 case 10: an unmounted actor's action keeps native's distance");
  // 11. Both initiative arrangements share one activation whose principal is the rider and partner the mount.
  Check(Get(p(a),"partnerContext")==a.Partner&&a.Activation.GetType().GetProperty("Partner").GetValue(a.Activation,null)==a.Mount,
   "U01 case 11: the projected actions are the activation partner's, the mount, under either initiative arrangement");
  // 12. Load or area boundary: no live activation, or a restored allocation of the same activation.
  Allocation(a,Identity(a),true,.6f,.6f);Set(a.Participant,"activation",null);
  Check(Write(mountMove,33f)==33f,"U01 case 12: with no live activation (load, area, end) nothing stale is projected");
  Set(a.Participant,"activation",a.Activation);
  Check(Write(mountMove,33f)==0f,"U01 case 12: the restored allocation of the same activation keeps the truthful closure");
  // 13. A failing projection never reaches the native write or gameplay state.
  var adapter=Get(a.Participant,"movementState");Set(a.Participant,"movementState",null);
  Exception thrown=null;float? shown=null;try{shown=Write(mountMove,31f);}catch(Exception e){thrown=e;}
  Set(a.Participant,"movementState",adapter);
  Check(prefix==null||thrown==null&&shown==31f,"U01 case 13: a projection failure is contained and native's write stands");
  Set(a.Participant,"settings",Blank(mod.GetType("KingmakerMountedCombat.Diagnostics.DiagnosticSettings",true)));
  Check(Write(mountMove,34f)==34f,"U01: paired activation disabled projects nothing");
  Set(a.Participant,"settings",Settings());
  // Five-foot: the fill offers native's step only through the same closure.
  var fill=a.Participant.GetType().GetMethod("FillPairedMountStates",F);var fillCalls=Calls(fill);
  var promise=fillCalls.IndexOf("MountedMovementDisplayPolicy.PromisesFiveFootStep");var step=fillCalls.IndexOf("TurnController.HasFiveFootStep");
  Check(promise>=0&&step>=0&&step<promise,"U01: the rider-selected fill promises a five-foot step only through PromisesFiveFootStep over native HasFiveFootStep");
  Check(!Calls(a.Participant.GetType().GetMethod("ProjectUsableMoveDistance",F)).Any(c=>c.Contains("set_")||c.Contains("Advance")||c.Contains("Cooldown")),
   "U01: the projection calls no setter, Advance or cooldown member");
 }
 static object p(Pair pair){return pair.Participant;}
 static object Prop(object o,string n){var q=o.GetType().GetProperty(n,F);return q==null?null:q.GetValue(o,null);}

 // ---- U02: the exact public slot while its delivery owns native target selection --------------------------------
 static object controls,handler;static HashSet<object> destroyed=new HashSet<object>();
 public static bool FactLive(object unit,object data){return data!=null&&!destroyed.Contains(data);}
 public static object SelectedHandler(){return handler;}
 // A Func<T> over a probe source, typed exactly as the product's field (an emitted cast of the probe value).
 static Delegate TypedSource(Type funcType,string source){
  var result=funcType.GetGenericArguments()[0];
  var dm=new DynamicMethod("kmc-probe-source",result,Type.EmptyTypes,typeof(MountedPresentationProbe).Module,true);
  var il=dm.GetILGenerator();il.Emit(OpCodes.Call,typeof(MountedPresentationProbe).GetMethod(source,F));il.Emit(OpCodes.Castclass,result);il.Emit(OpCodes.Ret);
  return dm.CreateDelegate(funcType);
 }
 static object Ability(string guid){
  var data=Blank(native.GetType("Kingmaker.UnitLogic.Abilities.AbilityData",true));
  var blueprint=Blank(native.GetType("Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility",true));
  Set(blueprint,"m_AssetGuid",guid);Alive(blueprint);Set(data,"Blueprint",blueprint);return data;
 }
 static object NewSlot(object unit,object ability){
  var slot=Activator.CreateInstance(native.GetType("Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility",true));
  Set(slot,"Unit",unit);Set(slot,"Ability",ability);return slot;
 }
 static void Select(object ability){Set(handler,"<Ability>k__BackingField",ability);}
 static bool Glows(object slot){
  if(Has(controls.GetType(),"PresentsTargetingSelection"))return (bool)Call(controls,"PresentsTargetingSelection",slot);
  // A build without the seam draws native's own decision: the selection's ability against the slot's own.
  var selected=Get(handler,"<Ability>k__BackingField");var own=Get(slot,"Ability");
  return selected!=null&&(bool)native.GetType("Kingmaker.UnitLogic.Abilities.AbilityData",true).GetMethod("op_Equality").Invoke(null,new[]{selected,own});
 }
 // One public click as native runs it: the prefix's begin, the swap, native's selection operation, the postfix's record.
 static void Click(object slot,object delivery,bool nativeSelects=true,bool nativeThrows=false){
  var own=Get(slot,"Ability");
  if(Has(controls.GetType(),"BeginPublicSlotClick"))Call(controls,"BeginPublicSlotClick");
  Set(slot,"Ability",delivery);
  if(nativeSelects){Note("target-selection-start");Select(delivery);}
  if(nativeThrows){Set(slot,"Ability",delivery);return;}
  // The product's own click postfix: it restores the public identity and records the mapping.
  patchMethods.GetMethod("PublicSlotClickPostfix",F).Invoke(null,new[]{slot,own});
 }
 static void Note(string reason){if(Has(controls.GetType(),"NoteTargetSelectionOperation"))Call(controls,"NoteTargetSelectionOperation",reason);}
 static void ProbeTargetingSlot(){
  var serviceType=mod.GetType("KingmakerMountedCombat.Integration.NativeMountedControlService",true);
  controls=Blank(serviceType);handler=Blank(native.GetType("Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler",true));
  Set(controls,"enabled",true);
  foreach(var name in new[]{"placementRecorded","initialPlacements"})Set(controls,name,Activator.CreateInstance(Field(serviceType,name).FieldType));
  var a=NewPair("u02a",4,Mounted());var b=NewPair("u02b",9,Mounted());
  Set(controls,"relationship",a.Relationship);
  SetStatic(patchBridge,"NativeControls",controls);
  var seam=Has(serviceType,"PresentsTargetingSelection");
  if(seam){
   var serials=Field(serviceType,"presentationSerials");Set(controls,"presentationSerials",Activator.CreateInstance(serials.FieldType));
   var handlerFunc=Field(serviceType,"selectedAbilityHandler");Set(controls,"selectedAbilityHandler",TypedSource(handlerFunc.FieldType,"SelectedHandler"));
   var live=Field(serviceType,"abilityFactLive");Set(controls,"abilityFactLive",Delegate.CreateDelegate(live.FieldType,typeof(MountedPresentationProbe).GetMethod("FactLive")));
  }
  Console.WriteLine("U02 product seam "+(seam?"present":"absent: the probe draws native's own selection decision"));
  const string Public="26151090ad4744ea96dbb7e0537a4a7f",Mount="f053faad986631688defa003cd7bda0e",Charge="c78506dd0e14f7c45a599990e4e65038",Shell="d79eaec224a7a832e738eb81baef9d49";
  var control=NewSlot(a.Rider,Ability(Public));var charge=NewSlot(a.Rider,Ability(Charge));var ordinary=NewSlot(a.Rider,Ability("ordinary"));
  var otherPair=NewSlot(b.Rider,Ability(Public));
  var mountShell=Ability(Mount);var chargeShell=Ability(Shell);
  // 1. One public slot enters targeting.
  Click(control,mountShell);
  Check(Glows(control),"U02 case 1: the public Mount / Dismount slot whose Mount shell owns target selection is drawn selected");
  Check(ReferenceEquals(Get(control,"Ability"),Get(control,"Ability"))&&((string)Get(Get(Get(control,"Ability"),"Blueprint"),"m_AssetGuid"))==Public,
   "U02 case 12: the slot keeps its public canonical ability; no hidden shell is bound to it");
  // 2-4. Ordinary abilities, the other control and another pair stay native.
  Check(!Glows(ordinary),"U02 case 2: an ordinary ability slot is unchanged");
  Check(!Glows(charge),"U02 case 3: the other mounted-control slot does not glow");
  Check(!Glows(otherPair),"U02 case 4: another pair's identical public slot cannot inherit the mapping");
  var duplicate=NewSlot(a.Rider,Get(control,"Ability"));
  Check(!Glows(duplicate),"U02 case 3: a second slot of the same public ability (same owner, same ability data) does not glow");
  Check(Glows(control),"U02 case 3: drawing other slots never takes the mark from the initiating slot");
  // 5 and 6. Delivery or cancellation ends native selection (DropAbility raises the end event).
  Note("target-selection-end");Select(null);
  Check(!Glows(control)&&!(bool)(Get(controls,"targetingMapping")!=null),"U02 cases 5-6: delivery or cancellation clears the mapping");
  // 7. Replacement by another ability, then by another mounted control.
  Click(control,mountShell);Note("target-selection-start");Select(Ability("fireball"));
  Check(!Glows(control),"U02 case 7: replacement by another ability clears the old slot");
  Click(charge,chargeShell);
  Check(Glows(charge)&&!Glows(control),"U02 case 7: replacement by another mounted control moves the mapping to its own exact slot");
  // 8. The public ability or the delivery shell destroyed.
  destroyed.Add(chargeShell);
  Check(!Glows(charge),"U02 case 8: a destroyed delivery shell clears");
  destroyed.Clear();Click(control,mountShell);destroyed.Add(Get(control,"Ability"));
  Check(!Glows(control),"U02 case 8: a destroyed public ability clears");destroyed.Clear();
  // Pair generation and dismount.
  Click(control,mountShell);Set(a.Relationship,"mountedPairGeneration",5L);
  Check(!Glows(control),"U02: a pair-generation change clears");Set(a.Relationship,"mountedPairGeneration",4L);
  Click(control,mountShell);SetState(a,Unmounted());
  Check(!Glows(control),"U02: a dismount clears");SetState(a,Mounted());
  // 9. Load, world replacement, disable (an inactive service).
  Click(control,mountShell);if(seam)Call(controls,"BeginLoadedWorld");
  Check(!Glows(control),"U02 case 9: a native load clears");
  Click(control,mountShell);Set(controls,"enabled",false);
  Check(!Glows(control),"U02 case 9: a disabled service draws nothing");Set(controls,"enabled",true);
  Click(control,mountShell);Set(controls,"areaSuspended",true);
  Check(!Glows(control),"U02 case 9: an area unload draws nothing");Set(controls,"areaSuspended",false);
  // 10. The click's native body throws: the prefix cleared any earlier mapping and no postfix runs.
  Click(control,mountShell);var throwing=NewSlot(a.Rider,Ability(Charge));Click(throwing,chargeShell,true,true);
  Check(!Glows(control)&&!Glows(throwing),"U02 case 10: a throwing click leaves no mapping, old or new");
  // 11. A recycled row: the same slot object now holds another ability.
  var publicData=Get(control,"Ability");
  Click(control,mountShell);Set(control,"Ability",Ability("recycled"));
  Check(!Glows(control),"U02 case 11: a slot recycled for another ability does not glow");
  Set(control,"Ability",publicData);Click(control,mountShell);
  Check(Glows(control),"U02 case 11: the restored public slot records again");
  Set(control,"Ability",Ability(Public));
  Check(!Glows(control),"U02 case 11: a slot rebound to another ability object of the same blueprint (a rebuilt fact) does not glow");
  // Lifetime: the mapping keeps no destroyed slot alive.
  if(seam){
   var weak=RecordAndDrop(a.Rider,Public,mountShell);GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
   Check(!weak.IsAlive,"U02: the mapping holds its slot only weakly (a dropped slot is collected)");
   foreach(var f in serviceType.GetFields(F).Where(x=>x.Name.StartsWith("targeting")||x.Name.Contains("presentation")))
    Check(!f.IsStatic&&(f.FieldType==typeof(WeakReference)||f.FieldType.IsValueType||f.FieldType.Name=="MountedTargetingSlotIdentity"||f.FieldType.Name.StartsWith("ConditionalWeakTable")),
     "U02: presentation field "+f.Name+" holds no strong UI, ability, unit or pair reference");
  }
  else Check(false,"U02: the product has no exact slot mapping");
  // The wiring: begin before delivery is resolved, record after the public identity is back, and every lifecycle clears.
  var click=patchMethods.GetMethod("PublicSlotClickPrefix",F);var after=patchMethods.GetMethod("PublicSlotClickPostfix",F);
  var clickCalls=Calls(click);var afterCalls=Calls(after);
  Check(clickCalls.IndexOf("NativeMountedControlService.BeginPublicSlotClick")>=0&&clickCalls.IndexOf("NativeMountedControlService.BeginPublicSlotClick")<clickCalls.IndexOf("NativeMountedControlService.ResolvePublicSlotDelivery"),
   "U02: the click prefix clears an earlier mapping before resolving its delivery");
  Check(afterCalls.Contains("NativeMountedControlService.RecordPublicSlotTargeting"),"U02: the click postfix records after restoring the public identity");
  foreach(var name in new[]{"SetEnabled","SuspendAreaControls","BeginLoadedWorld","Dispose","RetireWorldControlBindings","HandleAbilityTargetSelectionStart","HandleAbilityTargetSelectionEnd"}){
   var calls=Calls(serviceType.GetMethods(F).First(m=>m.Name==name));
   Check(calls.Contains("NativeMountedControlService.ClearTargetingPresentation")||calls.Contains("NativeMountedControlService.NoteTargetSelectionOperation"),
    "U02: "+name+" ends the targeting presentation");
  }
  Check(!Calls(serviceType.GetMethod("PresentsTargetingSelection",F)).Any(c=>c.Contains("get_Name")||c.Contains("GetTitle")||c.Contains("GetIcon")||c.Contains("LocalizedName")),
   "U02: the decision reads no name, title or icon");
 }
 static WeakReference RecordAndDrop(object rider,string guid,object delivery){
  var slot=NewSlot(rider,Ability(guid));Click(slot,delivery);
  Check(Glows(slot),"U02: a fresh slot records its mapping");
  return new WeakReference(slot);
 }

 // ---- U03: the class panel's rows for the shown character -----------------------------------------------------------
 static object Feature(string guid){return Feature(guid,"Kingmaker.Blueprints.Classes.BlueprintFeature");}
 static object Feature(string guid,string type){var f=Blank(native.GetType(type,true));Set(f,"m_AssetGuid",guid);Alive(f);return f;}
 // A live engine object as Unity's own null test reads it (a detached blank one reads as destroyed): a nonzero native pointer.
 static void Alive(object unityObject){Set(unityObject,"m_CachedPtr",new IntPtr(1));}
 static object Entry(int level,params object[] features){
  var entry=Activator.CreateInstance(native.GetType("Kingmaker.Blueprints.Classes.LevelEntry",true));
  var list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(native.GetType("Kingmaker.Blueprints.Classes.BlueprintFeatureBase",true)));
  foreach(var f in features)list.Add(f);Set(entry,"Level",level);Set(entry,"Features",list);return entry;
 }
 static object Progression(object blueprint,params object[] entries){
  var data=Blank(native.GetType("Kingmaker.UnitLogic.ProgressionData",true));
  var array=Array.CreateInstance(native.GetType("Kingmaker.Blueprints.Classes.LevelEntry",true),entries.Length);
  for(var i=0;i<entries.Length;i++)array.SetValue(entries[i],i);
  Set(data,"Blueprint",blueprint);Set(data,"LevelEntries",array);return data;
 }
 static List<string> Rows(object progressions){
  var rows=new List<string>();
  foreach(var data in (IEnumerable)progressions)foreach(var entry in (IEnumerable)Get(data,"LevelEntries"))
   foreach(var f in (IEnumerable)Get(entry,"Features"))rows.Add(Get(entry,"Level")+":"+Get(f,"m_AssetGuid"));
  return rows;
 }
 static void ProbeClassRows(){
  var serviceType=mod.GetType("KingmakerMountedCombat.Integration.HorseCompanionBlueprintService",true);
  var service=Blank(serviceType);
  var upgrade=Feature("c8db0772");var additional=Feature("5a64de54");var bond=Feature("divine-bond");var smite=Feature("smite");
  var mountSelection=Feature("b14c9d27","Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection");var weaponBond=Feature("1c7cdc16");
  var set=Activator.CreateInstance(Field(serviceType,"weaponBondUpgrades").FieldType);
  set.GetType().GetMethod("Add").Invoke(set,new[]{upgrade});set.GetType().GetMethod("Add").Invoke(set,new[]{additional});
  Set(service,"weaponBondUpgrades",set);Set(service,"divineMountSelection",mountSelection);Set(service,"weaponBondFeature",weaponBond);
  Set(service,"divineBondApplied",true);
  var logger=new Recorder(mod.GetType("KingmakerMountedCombat.Logging.IModLogger",true));Set(service,"logger",logger.GetTransparentProxy());
  SetStatic(patchBridge,"HorseCompanion",service);
  var paladin=Blank(native.GetType("Kingmaker.Blueprints.Classes.BlueprintProgression",true));Alive(paladin);
  Func<object> table=()=>Progression(paladin,Entry(1,smite),Entry(5,bond),Entry(6,upgrade),Entry(9,additional,smite));
  // Through the production service: its own descriptor reads and the policy, over a genuine unit progression.
  var mountRows=Rows(ServiceRows(service,paladin,table(),true,false));
  Check(!mountRows.Contains("6:c8db0772")&&!mountRows.Contains("9:5a64de54")&&mountRows.Contains("9:smite")&&mountRows.Contains("5:divine-bond"),
   "U03 cases 1 and 4: a Divine Mount character's panel omits the weapon-bond upgrades and keeps every other row ("+string.Join(",",mountRows.ToArray())+")");
  var weaponRows=Rows(ServiceRows(service,paladin,table(),false,true));
  Check(weaponRows.Contains("6:c8db0772")&&weaponRows.Contains("9:5a64de54"),"U03 cases 2, 5 and 6: Weapon Bond (new, existing or an older stock save) keeps its native rows");
  var undecided=Rows(ServiceRows(service,paladin,table(),false,false));
  Check(!undecided.Contains("6:c8db0772")&&undecided.Contains("5:divine-bond"),"U03 case 3: an undecided bond promises no path's upgrades; the level-5 choice row names both");
  var toMount=Rows(ServiceRows(service,paladin,table(),true,false));var toWeapon=Rows(ServiceRows(service,paladin,table(),false,true));
  Check(!toMount.Contains("6:c8db0772")&&toWeapon.Contains("6:c8db0772"),"U03 cases 7-8: a respec preview either way follows the preview descriptor");
  // 9-12. No state: the committed unit, another character and a replaced preview read only their own facts.
  var original=table();var originalRows=Rows(new[]{original});
  var preview=Rows(ServiceRows(service,paladin,original,true,false));
  var committed=Rows(ServiceRows(service,paladin,original,false,true));
  Check(committed.SequenceEqual(originalRows),"U03 case 9: after a cancelled Divine Mount preview the committed Weapon Bond character shows its own rows");
  Check(Rows(new[]{original}).SequenceEqual(originalRows),"U03: the unit's own progression data and level entries are never modified");
  var other=Rows(ServiceRows(service,paladin,table(),false,true));
  Check(other.Contains("6:c8db0772"),"U03 cases 10-11: switching to another character never carries a preview's projection");
  Check(serviceType.GetFields(F).Where(f=>!f.IsStatic&&(f.FieldType.Name.Contains("ProgressionData")||f.FieldType.Name.Contains("LevelEntry")||f.FieldType.Name.Contains("UnitDescriptor"))).Count()==0,
   "U03 case 12: the service keeps no progression, level-entry or descriptor state to go stale");
  // The stock path and a disabled choice.
  Set(service,"divineBondApplied",false);
  Check(Rows(ServiceRows(service,paladin,table(),false,false)).Contains("6:c8db0772"),"U03: with the choice not applied a stock character keeps every native row");
  Check(!Rows(ServiceRows(service,paladin,table(),true,false)).Contains("6:c8db0772"),"U03: a mount owner's panel follows its grants even with the choice not applied");
  Set(service,"divineBondApplied",true);
  // The panel's construction seam.
  var block=native.GetType("Kingmaker.UI.ServiceWindow.CharacterScreen.CharSComponentClassBlock",true).GetMethod("GetUIProgressionLayout",F);
  Check(block.MetadataToken==0x06004A32&&Calls(block).Count(c=>c=="UnitProgressionData.GetClassProgressions")==1,"native CharSComponentClassBlock.GetUIProgressionLayout reads GetClassProgressions once");
  var transpiler=patchMethods.GetMethod("DivineBondClassRowsTranspiler",F);
  Check(transpiler!=null,"U03: the product replaces the class panel's progression read");
  var instruction=harmony.GetType().Assembly.GetType("Harmony12.CodeInstruction",true);
  var code=Harmony12Instructions(block,instruction);var patched=Transpile(transpiler,code,instruction);
  var calls=patched.Select(c=>instruction.GetField("operand").GetValue(c) as MethodBase).Where(m=>m!=null).Select(m=>m.Name).ToList();
  Check(calls.Count(n=>n=="DivineBondClassRows")==1&&!calls.Contains("GetClassProgressions")&&patched.Count==code.Count,
   "U03: the transpiled layout builder calls the projection exactly once instead of native's read, nothing else changes");
  Check(Calls(serviceType.GetMethod("ProjectClassProgressions",F)).Count(c=>c=="UnitProgressionData.GetClassProgressions")==1,"U03: the projection runs native's read exactly once");
  harmonyPatch.Invoke(harmony,new object[]{block,null,null,Activator.CreateInstance(harmonyMethod,new object[]{transpiler})});
  Check(true,"U03: a real Harmony12 patch of GetUIProgressionLayout with the product transpiler is constructed");
 }
 // The unit's progression data as native returns it to the panel, through the production service when this build has it.
 static object ServiceRows(object service,object paladin,object data,bool mount,bool weapon){
  var unit=FactsUnit(service,mount,weapon);
  var progression=Get(unit,"Progression");
  var nativeArray=Array.CreateInstance(data.GetType(),1);nativeArray.SetValue(data,0);
  SetNative(progression,nativeArray);
  if(!Has(service.GetType(),"ProjectClassProgressions"))return nativeArray;
  return Call(service,"ProjectClassProgressions",progression,Blank(native.GetType("Kingmaker.Blueprints.Classes.BlueprintCharacterClass",true)));
 }
 // A probe-owned substitute for native's GetClassProgressions read (native walks feature sources the detached unit lacks).
 static object nativeProgressions;
 static bool readPatched;
 static void SetNative(object progression,object value){
  nativeProgressions=value;
  if(readPatched)return;readPatched=true;
  var read=native.GetType("Kingmaker.UnitLogic.UnitProgressionData",true).GetMethod("GetClassProgressions",F);
  harmonyPatch.Invoke(harmony,new object[]{read,Activator.CreateInstance(harmonyMethod,new object[]{ProgressionsPrefix(read)}),null,null});
 }
 static MethodInfo ProgressionsPrefix(MethodInfo read){return ReturnPrefix(read,"NativeProgressions");}
 // A probe-owned prefix that answers a native getter with a probe value (a typed ref __result, as Harmony 1 binds it).
 static int prefixes;
 static MethodInfo ReturnPrefix(MethodInfo getter,string source){
  var module=AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("KmcPresentationProbePrefix"+(++prefixes)),AssemblyBuilderAccess.Run).DefineDynamicModule("m");
  var type=module.DefineType("ReturnPrefix",TypeAttributes.Public|TypeAttributes.Abstract|TypeAttributes.Sealed);
  var method=type.DefineMethod("Prefix",MethodAttributes.Public|MethodAttributes.Static,typeof(bool),new[]{getter.ReturnType.MakeByRefType()});
  method.DefineParameter(1,ParameterAttributes.None,"__result");
  var il=method.GetILGenerator();il.Emit(OpCodes.Ldarg_0);
  il.Emit(OpCodes.Call,typeof(MountedPresentationProbe).GetMethod(source,F));il.Emit(OpCodes.Castclass,getter.ReturnType);
  il.Emit(OpCodes.Stind_Ref);il.Emit(OpCodes.Ldc_I4_0);il.Emit(OpCodes.Ret);
  return type.CreateType().GetMethod("Prefix");
 }
 public static object GameInstance(){return game;}
 public static object NativeProgressions(){return nativeProgressions;}
 // A genuine native unit whose progression features hold exactly the bond facts of the shown descriptor.
 static object FactsUnit(object service,bool mount,bool weapon){
  var descriptorType=native.GetType("Kingmaker.UnitLogic.UnitDescriptor",true);
  var descriptor=Blank(descriptorType);
  var progression=Blank(native.GetType("Kingmaker.UnitLogic.UnitProgressionData",true));
  Set(progression,"Owner",descriptor);Set(descriptor,"Progression",progression);
  var features=Blank(native.GetType("Kingmaker.UnitLogic.FeatureCollection",true));Set(progression,"Features",features);
  var facts=(IList)Activator.CreateInstance(Field(features.GetType(),"m_Facts").FieldType);Set(features,"m_Facts",facts);
  Func<object,object> fact=bp=>{var f=Blank(native.GetType("Kingmaker.UnitLogic.Feature",true));Set(f,"Blueprint",bp);return f;};
  if(mount)facts.Add(fact(Get(service,"divineMountSelection")));
  if(weapon)facts.Add(fact(Get(service,"weaponBondFeature")));
  return descriptor;
 }

 static List<object> Harmony12Instructions(MethodBase method,Type instruction){
  MethodInfo read=null;
  foreach(var m in Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony.dll")).GetType("HarmonyLib.PatchProcessor",true).GetMethods(BindingFlags.Public|BindingFlags.Static))
   if(m.Name=="GetOriginalInstructions"&&m.GetParameters().Length==2&&!m.GetParameters()[1].ParameterType.IsByRef)read=m;
  var list=new List<object>();
  foreach(var raw in (IEnumerable)read.Invoke(null,new object[]{method,null})){
   var copy=Activator.CreateInstance(instruction,new[]{raw.GetType().GetField("opcode").GetValue(raw),raw.GetType().GetField("operand").GetValue(raw)});
   list.Add(copy);
  }
  return list;
 }
 static List<object> Transpile(MethodInfo transpiler,List<object> code,Type instruction){
  var typed=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(instruction));
  foreach(var c in code)typed.Add(c);
  return ((IEnumerable)transpiler.Invoke(null,new object[]{typed})).Cast<object>().ToList();
 }

 // ---- U04: the mounted badge and the folded mount's tracker hover ---------------------------------------------------
 static object presentation;
 static object TrackerEntry(object unit){
  var entryType=native.GetType("Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerUnitVM",true);
  var entry=Blank(entryType);Set(entry,"Unit",unit);
  Set(entry,"IsHover",Activator.CreateInstance(Field(entryType,"IsHover").FieldType,new object[]{false}));
  return entry;
 }
 static object Tracker(params object[] units){
  var trackerType=native.GetType("Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM",true);
  var tracker=Blank(trackerType);
  var list=(IList)Activator.CreateInstance(Field(trackerType,"Units").FieldType);foreach(var u in units)list.Add(TrackerEntry(u));
  Set(tracker,"Units",list);Set(tracker,"HoveredUnit",Activator.CreateInstance(Field(trackerType,"HoveredUnit").FieldType));
  return tracker;
 }
 static object Hovered(object tracker){var p=Get(tracker,"HoveredUnit");return p.GetType().GetProperty("Value").GetValue(p,null);}
 static bool IsHover(object entry){var p=Get(entry,"IsHover");return (bool)p.GetType().GetProperty("Value").GetValue(p,null);}
 static object View(object unit){
  var view=Blank(native.GetType("Kingmaker.View.UnitEntityView",true));
  var data=view.GetType().BaseType;
  for(var t=view.GetType();t!=null;t=t.BaseType){var f=t.GetFields(F|BindingFlags.DeclaredOnly).FirstOrDefault(x=>x.FieldType.Name=="EntityDataBase");if(f!=null){f.SetValue(view,unit);break;}}
  // A live view as Unity's own null test reads it: a nonzero native object pointer.
  Set(view,"m_CachedPtr",new IntPtr(1));
  return view;
 }
 // Native's hover pass found no entry of the hovered unit's own (the folded mount has none); then the product postfix.
 static void Hover(object tracker,object unit){
  var postfix=patchMethods.GetMethod("FoldedMountHoverPostfix",F);
  if(postfix!=null)postfix.Invoke(null,new object[]{tracker,View(unit),true});
 }
 static object EntryOf(object tracker,object unit){foreach(var e in (IList)Get(tracker,"Units"))if(Get(e,"Unit")==unit)return e;return null;}
 static string Order(object tracker){return string.Join(",",((IList)Get(tracker,"Units")).Cast<object>().Select(e=>Id(Get(e,"Unit"))).ToArray());}
 static void Fold(Pair p,bool split){Set(p.Activation,"<Split>k__BackingField",split);Set(p.Participant,"splitReleaseRound",split?1:-1);}
 static void ProbeTrackerAndBadge(){
  pairs.Clear();Set(world,"pairSnapshot",null);((IDictionary)Get(registry,"actors")).Clear();
  var a=NewPair("u04a",1,Mounted());var b=NewPair("u04b",6,Mounted());
  var type=mod.GetType("KingmakerMountedCombat.Integration.MountedPairPresentation");
  if(type==null){Check(false,"U04: the product has no mounted-pair presentation (no badge, no folded-mount hover)");return;}
  var logger=new Recorder(mod.GetType("KingmakerMountedCombat.Logging.IModLogger",true));
  Func<UnityEngine.Sprite> sprite=()=>null;
  presentation=Activator.CreateInstance(type,F,null,new object[]{world,sprite,logger.GetTransparentProxy()},null);
  presentation.GetType().GetProperty("Enabled",F).SetValue(presentation,true,null);
  SetStatic(patchBridge,"Presentation",presentation);
  Func<object,object> badge=unit=>Call(presentation,"PortraitBadge",unit);
  Func<object,string> mountOf=unit=>{var i=badge(unit);return i==null?null:(string)Get(i,"MountId");};
  // 1, 10. Mount: the badge appears for the exact rider, and two pairs never cross.
  Check(mountOf(a.Rider)==Id(a.Mount)&&mountOf(b.Rider)==Id(b.Mount),"U04 cases 1 and 10: each rider's badge names its own exact mount");
  Check(badge(a.Mount)==null&&badge(Unit("stranger"))==null,"U04: a mount's own portrait and an unmounted actor carry no badge");
  // 2. Dismount; 3. remount with a new generation; 4-5. death and 8. disposal leave the relationship unmounted.
  SetState(a,Unmounted());Check(badge(a.Rider)==null,"U04 cases 2, 4, 5 and 8: a dismount (death, disposal) clears the badge");
  SetState(a,Mounted());Set(a.Relationship,"mountedPairGeneration",2L);
  Check(badge(a.Rider)!=null&&(long)Get(badge(a.Rider),"Generation")==2L,"U04 case 3: a remount maps its new generation");
  // 6-7. Load and area: a world with no pairs shows nothing.
  var saved=new ArrayList(pairs);pairs.Clear();Set(world,"pairSnapshot",null);
  Check(badge(a.Rider)==null&&badge(b.Rider)==null,"U04 cases 6-7: a load or area boundary before the pairs return shows no badge");
  foreach(var s in saved)pairs.Add(s);Set(world,"pairSnapshot",null);
  // 9. Pair replacement and 11-12: the same rider with another mount, the same mount with another rider.
  pairs.Remove(a.Services);Set(world,"pairSnapshot",null);
  var c=NewPair("u04c",1,Mounted());Set(Get(c.Relationship,"runtime"),"rider",a.Rider);
  var ownerType=c.Owner.GetType();Func<bool> yes=()=>true;
  c.Owner=Activator.CreateInstance(ownerType,F,null,new object[]{new object(),"campaign",a.Rider,c.Mount,Id(a.Rider),Id(c.Mount),1L,c.Relationship,yes,yes,yes,null},null);
  Set(c.Relationship,"<PairOwner>k__BackingField",c.Owner);
  Check(mountOf(a.Rider)==Id(c.Mount),"U04 cases 9 and 11: a replaced pair or another mount moves the badge to the new mount");
  pairs.Remove(c.Services);pairs.Add(a.Services);Set(world,"pairSnapshot",null);
  // Hover. Native tracker entries: the folded mounts have none of their own.
  Fold(a,false);Fold(b,false);
  var tracker=Tracker(Unit("enemy"),b.Rider,a.Rider,Unit("ally"));var before=Order(tracker);
  var counters=new[]{"TrackerMountFilterCount","RedundantMountTurnSkipCount"}.Select(n=>Prop(a.Participant,n)).ToArray();
  Hover(tracker,a.Mount);
  Check(Hovered(tracker)==EntryOf(tracker,a.Rider)&&IsHover(EntryOf(tracker,a.Rider))&&!IsHover(EntryOf(tracker,b.Rider)),
   "U04 case 14: hovering pair A's folded mount highlights rider A's existing entry, not pair B's");
  Check(Order(tracker)==before&&((IList)Get(tracker,"Units")).Count==4,"U04: the hover adds no entry and keeps the tracker's order");
  Check(new[]{"TrackerMountFilterCount","RedundantMountTurnSkipCount"}.Select(n=>Prop(a.Participant,n)).SequenceEqual(counters),
   "U04: the hover changes no scheduler or tracker-filter state");
  var second=Tracker(Unit("enemy"),b.Rider,a.Rider);Hover(second,b.Mount);
  Check(Hovered(second)==EntryOf(second,b.Rider)&&!IsHover(EntryOf(second,a.Rider)),"U04 case 10: pair B's folded mount reaches only rider B");
  Func<Pair,bool> folded=pp=>{foreach(var f in (IList)Call(presentation,"Facts"))if((string)Get(f,"MountId")==Id(pp.Mount))return (bool)Get(f,"MountEntryFolded");throw new InvalidOperationException("no fact");};
  Check(folded(a)&&folded(b),"U04 case 14: a shared activation's mount is reported folded, as the tracker filter omits it");
  // 13. An independently acting mount keeps its own native entry and hover.
  Fold(a,true);Set(combat,"<RoundNumber>k__BackingField",5);
  Check(!folded(a)&&folded(b),"U04 case 13: an independently acting mount is not reported folded (the tracker filter keeps its entry)");
  var partial=Tracker(a.Rider,Unit("other"));Hover(partial,a.Mount);
  Check(Hovered(partial)==null,"U04 case 13: an independent mount is never redirected, even where its own entry is out of view");
  var independent=Tracker(a.Rider,a.Mount);Hover(independent,a.Mount);
  Check(Hovered(independent)==null&&!IsHover(EntryOf(independent,a.Rider)),"U04 case 13: an independently acting mount is never redirected");
  Set(combat,"<RoundNumber>k__BackingField",3);Fold(a,false);
  // 15. A rebuilt tracker: the hover reads the entries as they are now, holding none.
  var rebuilt=Tracker(a.Rider,Unit("x"));Hover(rebuilt,a.Mount);
  Check(Hovered(rebuilt)==EntryOf(rebuilt,a.Rider),"U04 case 15: a rebuilt tracker maps through its own current entries");
  Check(type.GetFields(F).All(f=>f.IsLiteral||f.IsStatic&&(typeof(MemberInfo).IsAssignableFrom(f.FieldType)||f.FieldType==typeof(Type))||!f.IsStatic&&
   (f.FieldType.IsValueType||f.FieldType==typeof(string)||f.FieldType.Name.StartsWith("List`1")&&f.FieldType.GetGenericArguments()[0]==typeof(WeakReference)||
    f.FieldType.Name.Contains("Func")||f.FieldType.Name.Contains("Logger")||f.FieldType.Name=="MountedWorldService")),
   "U04 case 15 and lifetime: the presentation keeps no tracker entry, view, unit or pair (badge objects only weakly, bounded)");
  Check((int)type.GetField("BadgeCapacity",F).GetValue(null)==32,"U04: the badge objects are bounded");
  // 16. Selection: the presentation reads no selection and selects nothing.
  var all=type.GetMethods(F|BindingFlags.DeclaredOnly).SelectMany(m=>m.GetMethodBody()==null?new List<string>():Calls(m)).ToList();
  Check(!all.Any(cl=>cl.StartsWith("SelectionManager")||cl.Contains("SelectUnit")),"U04 case 16: the presentation never reads or changes the selection");
  // 17. Disable and re-enable.
  presentation.GetType().GetProperty("Enabled",F).SetValue(presentation,false,null);
  var off=Tracker(a.Rider);Hover(off,a.Mount);
  Check(badge(a.Rider)==null&&Hovered(off)==null,"U04 case 17: disabled, no badge and no redirect");
  presentation.GetType().GetProperty("Enabled",F).SetValue(presentation,true,null);
  Check(badge(a.Rider)!=null,"U04 case 17: re-enabled, the badge is derived afresh");
  // 18. A presentation failure is contained.
  Set(world,"pairs",null);Exception thrown=null;
  try{Hover(Tracker(a.Rider),a.Mount);}catch(Exception e){thrown=e;}
  Set(world,"pairs",pairs);
  Check(thrown==null,"U04 case 18: a presentation failure never escapes the native hover");
  // 12. The same mount with another rider.
  Set(Get(b.Relationship,"runtime"),"mount",a.Mount);
  Check(mountOf(a.Rider)==Id(a.Mount)&&mountOf(b.Rider)==null,"U04 case 12: one mount claimed by two relationships shows no badge on the inconsistent pair");
  Set(Get(b.Relationship,"runtime"),"mount",b.Mount);
  // The seams.
  var trackerType=native.GetType("Kingmaker.UI._ConsoleUI.TurnBasedMode.InitiativeTrackerVM",true);
  var hover=trackerType.GetMethods(F).First(m=>m.Name=="HandleHoverChange"&&m.GetParameters().Length==2);
  var health=native.GetType("Kingmaker.UI.Group.GroupCharacterPortraitController",true).GetMethod("HealthUpdate",F);
  Check(hover.MetadataToken==0x06004F16&&health.MetadataToken==0x06003F01,"native hover and portrait refresh are the pinned seams");
  foreach(var pair in new[]{Tuple.Create((MethodBase)hover,"FoldedMountHoverPostfix"),Tuple.Create((MethodBase)health,"MountedBadgePostfix")}){
   harmonyPatch.Invoke(harmony,new object[]{pair.Item1,null,Activator.CreateInstance(harmonyMethod,new object[]{patchMethods.GetMethod(pair.Item2,F)}),null});
   Check(true,"U04: a real Harmony12 patch of "+pair.Item1.Name+" with "+pair.Item2+" is constructed");
  }
  var refresh=Calls(type.GetMethod("RefreshPortrait",F));
  Check(refresh.Contains("MountedPairPresentation.PortraitBadge")&&refresh.Contains("Transform.Find"),"U04: every portrait refresh derives the badge from the live relationship");
  Check(Calls(type.GetMethod("CreateBadge",F)).Count(cl=>cl.EndsWith("SetValue"))>=2,"U04: the badge image takes no pointer event (portrait clicks and hover stay native)");
 }

 // ---- Registration of every new seam in the product -----------------------------------------------------------------
 static void ProbeRegistration(){
  var ctor=controller.GetConstructors(F)[0];var tokens=Int32Operands(ctor);
  foreach(var pair in new[]{Tuple.Create(0x0600704F,"PairedMoveDistancePrefix"),Tuple.Create(0x06002F68,"PublicSlotSelectionPostfix"),
    Tuple.Create(0x06004A32,"DivineBondClassRowsTranspiler"),Tuple.Create(0x06004F16,"FoldedMountHoverPostfix"),Tuple.Create(0x06003F01,"MountedBadgePostfix")})
   Check(tokens.Contains(pair.Item1)&&patchMethods.GetMethod(pair.Item2,F)!=null,"the product registers "+pair.Item2+" at exact token 0x"+pair.Item1.ToString("X8"));
  // Native UpdateSlotInternal draws through Unity internal calls, so its patched replacement cannot be compiled outside the
  // engine. Its binding is checked instead: the exact method, and a postfix whose parameters native's names and types bind.
  var slot=native.GetType("Kingmaker.UI.UnitSettings.MechanicActionBarSlotAbility",true).GetMethod("UpdateSlotInternal",F);
  var selection=patchMethods.GetMethod("PublicSlotSelectionPostfix",F);var sp=selection.GetParameters();var np=slot.GetParameters();
  Check(slot.MetadataToken==0x06002F68&&sp.Length==2&&sp[0].Name=="__instance"&&sp[0].ParameterType==slot.DeclaringType&&
   sp[1].Name==np[0].Name&&sp[1].ParameterType==np[0].ParameterType&&selection.ReturnType==typeof(void),
   "U02: PublicSlotSelectionPostfix binds native UpdateSlotInternal(ActionBarSlot slot) by name and type (the patched body itself needs the engine to compile)");
  Exception failure=null;
  try{harmonyPatch.Invoke(harmony,new object[]{slot,null,Activator.CreateInstance(harmonyMethod,new object[]{selection}),null});}
  catch(TargetInvocationException e){failure=e.InnerException;}
  Console.WriteLine("U02 detached Harmony12 construction of UpdateSlotInternal: "+(failure==null?"constructed":"not constructible outside the engine ("+failure.Message+")"));
  // No new seam touches a gameplay member.
  foreach(var name in new[]{"PairedMoveDistancePrefix","PublicSlotSelectionPostfix","FoldedMountHoverPostfix","MountedBadgePostfix","DivineBondClassRows"})
   Check(!Calls(patchMethods.GetMethod(name,F)).Any(c=>c.Contains("Cooldown")||c.Contains("Spend")||c.Contains("Commands.")||c.Contains("Run(")||c.Contains("AddFact")||c.Contains("RemoveFact")),
    "presentation seam "+name+" calls no action, cost, command or fact member");
 }

 public static void Run(string managedPath,string dll){
  managed=managedPath;
  AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{
   var leaf=new AssemblyName(a.Name).Name+".dll";
   foreach(var root in new[]{managed,Path.Combine(managed,"UnityModManager")}){var path=Path.Combine(root,leaf);if(File.Exists(path))return Assembly.LoadFrom(path);}return null;
  };
  native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));mod=Assembly.LoadFrom(dll);
  Console.WriteLine("product="+dll+" mvid="+mod.ManifestModule.ModuleVersionId);
  Check(native.ManifestModule.ModuleVersionId.ToString()=="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7","pinned native MVID");
  unitType=native.GetType("Kingmaker.EntitySystem.Entities.UnitEntityData",true);
  combatActionType=native.GetType("Kingmaker.TurnBasedMode.Controllers.CombatAction",true);
  controller=mod.GetType("KingmakerMountedCombat.Integration.MountedPatchController",true);
  patchMethods=controller.GetNestedType("PatchMethods",F);patchBridge=controller.GetNestedType("PatchBridge",F);
  var h=Assembly.LoadFrom(Path.Combine(managed,"UnityModManager/0Harmony12.dll"));
  harmony=h.GetType("Harmony12.HarmonyInstance",true).GetMethod("Create").Invoke(null,new object[]{"KingmakerMountedCombat.PresentationProbe"});
  harmonyPatch=harmony.GetType().GetMethod("Patch");harmonyMethod=h.GetType("Harmony12.HarmonyMethod",true);
  try{
   Engine();World();
   SetStatic(patchBridge,"Logger",new Recorder(mod.GetType("KingmakerMountedCombat.Logging.IModLogger",true)).GetTransparentProxy());
   Section("U01 unusable movement distance after Charge",ProbeMovementDisplay);
   Section("U02 the public slot while its delivery targets",ProbeTargetingSlot);
   Section("U03 the class panel's bond rows",ProbeClassRows);
   Section("U04 mounted badge and folded mount hover",ProbeTrackerAndBadge);
   Section("Sprint 7 seam registration",ProbeRegistration);
  }
  finally{harmony.GetType().GetMethod("UnpatchAll",new[]{typeof(string)}).Invoke(harmony,new object[]{"KingmakerMountedCombat.PresentationProbe"});}
  Console.WriteLine("MOUNTED PRESENTATION CONTRACTS PASS="+passed+" FAIL="+sectionFailures+"; detached, not native gameplay or visual evidence");
  if(sectionFailures>0)throw new InvalidOperationException(sectionFailures+" Sprint 7 section(s) failed");
 }
}
'@ -ReferencedAssemblies @((Join-Path $managed 'UnityModManager/0Harmony12.dll'),(Join-Path $managed 'UnityEngine.CoreModule.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $managed 'UnityModManager/0Harmony12.dll'))
try { [MountedPresentationProbe]::Run($managed,$dll) }
catch { Write-Output $_.Exception.ToString(); exit 1 }
