[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
$layout=(Get-Content -Raw (Join-Path $lab 'environment-intake.json')|ConvertFrom-Json).requestedLayout
$managed=Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data/Managed'
$cotw=Join-Path $layout.kingmakerInstallDir 'Mods/CallOfTheWild/CallOfTheWild.dll'
if((Get-FileHash -LiteralPath $cotw).Hash.ToLowerInvariant()-cne'4ebf8e1ed3e66ffed72ea33ea325595629423dacd5bffa23e3c9109144b26915'){throw 'Pinned optional COTW assembly changed'}
if((Get-FileHash -LiteralPath (Join-Path $managed 'Assembly-CSharp.dll')).Hash.ToLowerInvariant()-cne'3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb'){throw 'Pinned native assembly changed'}
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
public static class ChargeBuffSurfaceProbe {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static Assembly native,cotw,mod;
 static int passed;
 static object Blank(Type t){return FormatterServices.GetUninitializedObject(t);}
 static object N(string name){return Blank(native.GetType(name,true));}
 static FieldInfo Field(Type t,string name){for(;t!=null;t=t.BaseType){var f=t.GetField(name,F);if(f!=null)return f;}throw new MissingFieldException(name);}
 static void Set(object o,string name,object value){var f=Field(o.GetType(),name);if(value!=null&&f.FieldType.IsEnum)value=Enum.ToObject(f.FieldType,value);f.SetValue(o,value);}
 static object Get(object o,string name){return Field(o.GetType(),name).GetValue(o);}
 static void ArrayField(object o,string name,params object[] values){var f=Field(o.GetType(),name);var a=Array.CreateInstance(f.FieldType.GetElementType(),values.Length);for(var i=0;i<values.Length;i++)a.SetValue(values[i],i);f.SetValue(o,a);}
 static object Buff(string guid){var b=N("Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff");Set(b,"m_AssetGuid",guid);return b;}
 static object Blueprint(string type,string guid){var b=N(type);Set(b,"m_AssetGuid",guid);return b;}
 static object Value(int type,int number,int rank){var v=N("Kingmaker.UnitLogic.Mechanics.ContextValue");Set(v,"ValueType",type);Set(v,"Value",number);Set(v,"ValueRank",rank);return v;}
 static object List(params object[] actions){var l=N("Kingmaker.ElementsSystem.ActionList");ArrayField(l,"Actions",actions);return l;}
 static object Apply(object buff,bool permanent,int rank){var a=N("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff");Set(a,"Buff",buff);Set(a,"Permanent",permanent);Set(a,"AsChild",true);Set(a,"IsNotDispelable",true);var d=N("Kingmaker.UnitLogic.Mechanics.ContextDurationValue");Set(d,"DiceCountValue",Value(0,0,0));Set(d,"BonusValue",Value(1,0,rank));Set(a,"DurationValue",d);return a;}
 static object Conditional(object buff,params string[] facts){var c=N("Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional");var checker=N("Kingmaker.ElementsSystem.ConditionsChecker");var cs=facts.Select(g=>{var f=N("Kingmaker.UnitLogic.Mechanics.Conditions.ContextConditionHasFact");Set(f,"Fact",Blueprint("Kingmaker.Blueprints.Classes.BlueprintFeature",g));return f;}).ToArray();ArrayField(checker,"Conditions",cs);Set(c,"ConditionsChecker",checker);Set(c,"IfFalse",List());Set(c,"IfTrue",List(Apply(buff,true,0)));return c;}
 static object Rank(int source,int progression,int type,bool min,int minValue,bool max,int maxValue,int start,int step,string cls,string archetype){
  var r=N("Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig");Set(r,"m_BaseValueType",source);Set(r,"m_Progression",progression);Set(r,"m_Type",type);Set(r,"m_UseMin",min);Set(r,"m_Min",minValue);Set(r,"m_UseMax",max);Set(r,"m_Max",maxValue);Set(r,"m_StartLevel",start);Set(r,"m_StepLevel",step);
  ArrayField(r,"m_Class",Blueprint("Kingmaker.Blueprints.Classes.BlueprintCharacterClass",cls));ArrayField(r,"m_FeatureList");ArrayField(r,"m_CustomProgression");if(archetype!=null)Set(r,"Archetype",Blueprint("Kingmaker.Blueprints.Classes.BlueprintArchetype",archetype));return r;
 }
 static object Trigger(object action,bool melee){var t=N("Kingmaker.UnitLogic.Mechanics.Components.AddInitiatorAttackWithWeaponTrigger");Set(t,"OnlyHit",true);Set(t,"CheckWeaponRangeType",melee);Set(t,"Action",List(action));return t;}
 static object Root(bool augmented){
  var root=Buff("f36da144a379d534cad8e21667079066");var armor=N("Kingmaker.UnitLogic.FactLogic.AddStatBonus");Set(armor,"Stat",11);Set(armor,"Value",-2);
  var condition=N("Kingmaker.UnitLogic.FactLogic.AddCondition");Set(condition,"Condition",40);
  var attack=N("Kingmaker.Designers.Mechanics.Facts.AttackOfOpportunityAttackBonus");Set(attack,"NotAttackOfOpportunity",true);Set(attack,"AttackBonus",1);Set(attack,"Value",Value(0,2,0));
  if(!augmented){ArrayField(root,"Components",armor,condition,attack);return root;}
  var toss=Buff("6683a35444eb42ddbd21f87c3441a50a");var trip=N("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionCombatManeuver");Set(trip,"Type",1);Set(trip,"OnSuccess",List(N("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionDealDamage")));ArrayField(toss,"Components",Trigger(trip,true));
  var hellfire=Buff("b0439659723f4a8da680965c78a8fbf5");var et=cotw.ManifestModule.ResolveMethod(0x060014FD).DeclaringType;
  var enchanters=new[]{"30f90becaaac51f41bf56641966c4121","3f032a3cd54e57649a0cdad0434bf221"}.Select((g,i)=>{var e=Blank(et);var enchant=Blueprint("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintWeaponEnchantment",g);var listener=Blank(native.ManifestModule.ResolveMethod(i==0?0x0600895E:0x0600895B).DeclaringType);if(i==0){var dice=N("Kingmaker.RuleSystem.DiceFormula");Set(dice,"m_Rolls",1);Set(dice,"m_Dice",Enum.Parse(Field(dice.GetType(),"m_Dice").FieldType,"D6"));Set(listener,"EnergyDamageDice",dice);}else Set(listener,"Dice",Enum.Parse(Field(listener.GetType(),"Dice").FieldType,"D10"));ArrayField(enchant,"Components",listener);ArrayField(e,"enchantments",enchant);ArrayField(e,"allowed_types");Set(e,"value",Value(0,1,0));return e;}).ToArray();ArrayField(hellfire,"Components",enchanters);
  var frightful=Buff("61aff33f69d84391b49782fb976cf870");ArrayField(frightful,"Components",Trigger(Apply(Buff("25ec6cb6ab1845c48a95f9c20b034220"),false,2),false),Rank(1,1,2,true,1,false,0,0,0,"cf217eb4f8504d67aad37464cee966f8",null));
  var actions=N("Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions");Set(actions,"Activated",List(Conditional(toss,"4f8d33348b184125a8b81363232535c0"),Conditional(hellfire,"d26ca0ac64874157aad34ef664b116a9","30e6b21b6baa49669b33adf46806fdfa"),Conditional(frightful,"2ba88b87439e456cb382392ba07ffa96","30e6b21b6baa49669b33adf46806fdfa")));
  var removals=new[]{hellfire,frightful}.Select(b=>{var r=N("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff");Set(r,"Buff",b);return r;}).ToArray();Set(actions,"Deactivated",List(removals));Set(actions,"NewRound",List());
  var bonus=N("Kingmaker.UnitLogic.FactLogic.AddContextStatBonus");Set(bonus,"Stat",11);Set(bonus,"Descriptor",25);Set(bonus,"Multiplier",1);Set(bonus,"Value",Value(1,0,0));
  ArrayField(root,"Components",actions,armor,condition,attack,bonus,Rank(12,9,0,false,0,true,2,3,4,"48ac8db94d5de7645906c7d0ad3bcfbd","4a76470cab5144159e37f55b25b074d2"));return root;
 }
 static object Read(object root){return mod.GetType("KingmakerMountedCombat.Integration.MountedChargeBuffSurface",true).GetMethod("Read",F).Invoke(null,new[]{root});}
 static void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);passed++;Console.WriteLine("PASS "+label);}
 static void Refuses(object root,Action change,string label){change();var refused=false;try{Read(root);}catch(TargetInvocationException e){refused=e.InnerException is InvalidOperationException;}Check(refused,label);}
 static object Comp(object root,int i){return ((Array)Get(root,"Components")).GetValue(i);}
 static object Activation(object root,int i){return ((Array)Get(Get(Comp(root,0),"Activated"),"Actions")).GetValue(i);}
 static object Child(object root,int i){var action=((Array)Get(Get(Activation(root,i),"IfTrue"),"Actions")).GetValue(0);return Get(action,"Buff");}
 static Func<object> callback;
 static object rootFact;
 static T NativeCallback<T>() where T:class{return (T)callback();}
 static T RootCallback<T>() where T:class{return (T)rootFact;}
 static Delegate TypedReturn(Type t,string method){return Delegate.CreateDelegate(typeof(Func<>).MakeGenericType(t),typeof(ChargeBuffSurfaceProbe).GetMethod(method,F).MakeGenericMethod(t));}
 static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,F).Invoke(o,args);}
 static void ChildOwnership(){
  var graphType=mod.GetType("KingmakerMountedCombat.Integration.MountedChargeBuffChildren",true);
  var owners=(System.Collections.IList)graphType.GetField("owners",F).GetValue(null);
  Check(owners.Count==0,"detached child probe begins without shared graph owners");
  var factType=native.GetType("Kingmaker.Blueprints.Facts.Fact",true);var buffType=native.GetType("Kingmaker.UnitLogic.Buffs.Buff",true);
  var statsType=native.GetType("Kingmaker.EntitySystem.Stats.ModifiableValue",true);
  Func<object> context=()=>N("Kingmaker.UnitLogic.Mechanics.MechanicsContext");
  Func<object> collection=()=>{var c=N("Kingmaker.UnitLogic.Buffs.BuffCollection");var f=native.ManifestModule.ResolveField(0x04006075);f.SetValue(c,Activator.CreateInstance(f.FieldType));return c;};
  Func<object,object,object> fact=(bp,ctx)=>{var b=Blank(buffType);Set(b,"Blueprint",bp);native.ManifestModule.ResolveField(0x04001B6F).SetValue(b,ctx);return b;};
  var blueprint=Root(true);var surface=Read(blueprint);var rootContext=context();rootFact=fact(blueprint,rootContext);
  var original=collection();var foreign=collection();var rider=N("Kingmaker.EntitySystem.Entities.UnitEntityData");
  var graph=Blank(graphType);Set(graph,"surface",surface);Set(graph,"rider",rider);Set(graph,"collection",original);
  Set(graph,"root",TypedReturn(buffType,"RootCallback"));var rootAcquiring=false;Set(graph,"rootAcquiring",new Func<bool>(()=>rootAcquiring));Set(graph,"observeRootRemoval",new Action(()=>{}));
  Set(graph,"stats",Array.CreateInstance(statsType,0));Set(graph,"thread",System.Threading.Thread.CurrentThread.ManagedThreadId);
  Set(graph,"nodes",Activator.CreateInstance(Field(graphType,"nodes").FieldType));owners.Add(graph);
  Set(graph,"failures",Activator.CreateInstance(Field(graphType,"failures").FieldType));
  var nodes=(System.Collections.IList)Get(graph,"nodes");var created=graphType.GetMethod("Created",F);var add=graphType.GetMethod("AddFact",F);
  var beforeAdd=graphType.GetMethod("BeforeAddBuff",F);var beforeRemove=graphType.GetMethod("BeforeRemove",F);
  var childContext=context();native.ManifestModule.ResolveField(0x04001707).SetValue(childContext,rootContext);
  var childBlueprint=Child(blueprint,0);var child=fact(childBlueprint,childContext);var foreignChild=fact(childBlueprint,context());
  var nativeCalls=0;
  try{
   rootAcquiring=true;
   Check(!(bool)Call(graph,"get_ScopeSettled")&&!(bool)Call(graph,"TryDrain"),"reentrant cleanup cannot close root acquisition");
   rootAcquiring=false;Set(graph,"retiring",false);
   callback=()=>{
    nativeCalls++;created.Invoke(null,new[]{original,child});
    Check(nodes.Count==1&&Object.ReferenceEquals(Call(Get(nodes[0],"Owner"),"get_Fact"),child),"cloned parent context captures exact child before activation");
    Check(!(bool)Call(graph,"get_ScopeSettled"),"child acquisition itself blocks lifecycle barriers");
    var saved=callback;
    try{
     callback=()=>{nativeCalls++;created.Invoke(null,new[]{original,foreignChild});return foreignChild;};
     var result=add.Invoke(null,new[]{original,childBlueprint,context(),(object)TypedReturn(factType,"NativeCallback")});
     Check(Object.ReferenceEquals(result,foreignChild)&&nodes.Count==1,"unowned nested native add shadows outer scope and preserves foreign fact");
    }finally{callback=saved;}
    Check(Object.ReferenceEquals(graphType.GetField("acquiring",F).GetValue(null),nodes[0]),"nested add restores exact outer acquisition scope");
    return child;
   };
   Check(Object.ReferenceEquals(add.Invoke(null,new[]{original,childBlueprint,childContext,(object)TypedReturn(factType,"NativeCallback")}),child)&&nativeCalls==2,"owned and unowned acquisition each call their native boundary once");
   Check((bool)Call(graph,"get_ScopeSettled")&&graphType.GetField("acquiring",F).GetValue(null)==null,"successful acquisition returns with no resumable observer scope");
   var owner=Get(nodes[0],"Owner");
   beforeRemove.Invoke(null,new[]{foreign,child});beforeRemove.Invoke(null,new[]{original,foreignChild});
   Check(!(bool)Call(owner,"get_RemovalAttempted"),"foreign collection and same-blueprint foreign instance cannot mark owned removal");
   native.ManifestModule.ResolveField(0x04006963).SetValue(child,true);
   Check(!(bool)Call(nodes[0],"Remove")&&!(bool)Call(owner,"get_RemovalAttempted"),"child's resumable deactivation cannot trigger reentrant native removal");
   native.ManifestModule.ResolveField(0x04006963).SetValue(child,false);
   beforeRemove.Invoke(null,new[]{original,child});
   Check((bool)Call(owner,"get_RemovalAttempted")&&!(bool)Call(nodes[0],"Remove"),"automatic removal with unresolved native residue is retained without replay");
   native.ManifestModule.ResolveField(0x04006961).SetValue(child,true);
   Check((bool)Call(graph,"TryDrain")&&(bool)Call(owner,"get_Drained"),"later exact disposed/listener/modifier postconditions drain retained child");
   Call(graph,"Release");Check(owners.Count==0,"only drained graph may release its observer registration");
   // A destructive Replace is refused before any native fact mutation.
   owners.Add(graph);Set(graph,"retiring",false);
   var raw=(System.Collections.IList)native.ManifestModule.ResolveField(0x04006075).GetValue(original);raw.Add(foreignChild);
   var refused=false;try{beforeAdd.Invoke(null,new[]{original,childBlueprint,childContext});}catch(TargetInvocationException e){refused=e.InnerException is InvalidOperationException;}
   Check(refused&&raw.Count==1&&Object.ReferenceEquals(raw[0],foreignChild),"preexisting same-blueprint foreign buff refuses native Replace without modifying it");
   raw.Clear();Set(graph,"fault",null);
   // A legitimate target-side consequence uses another container and is never adopted.
   callback=()=>{created.Invoke(null,new[]{foreign,foreignChild});return foreignChild;};
   var beforeCount=nodes.Count;
   add.Invoke(null,new[]{foreign,childBlueprint,childContext,(object)TypedReturn(factType,"NativeCallback")});
   Check(nodes.Count==beforeCount,"different-owner target consequence remains outside charge lifetime ownership");
  }finally{owners.Remove(graph);callback=null;rootFact=null;}
  var modern=Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(native.Location),"UnityModManager/0Harmony.dll"));
  var read=modern.GetType("HarmonyLib.PatchProcessor",true).GetMethods(F).Single(m=>m.Name=="GetOriginalInstructions"&&m.GetParameters().Length==2&&!m.GetParameters()[1].ParameterType.IsByRef);
  var transform=graphType.GetMethod("WrapAddFact",F);var instructionType=transform.GetParameters()[0].ParameterType.GetGenericArguments()[0];var listType=typeof(System.Collections.Generic.List<>).MakeGenericType(instructionType);
  foreach(var token in new[]{0x060029F7,0x060099B8}){
   var originalMethod=native.ManifestModule.ResolveMethod(token);var input=(System.Collections.IList)Activator.CreateInstance(listType);
   foreach(var ins in (System.Collections.IEnumerable)read.Invoke(null,new object[]{originalMethod,null})){var t=ins.GetType();input.Add(Activator.CreateInstance(instructionType,new[]{t.GetField("opcode").GetValue(ins),t.GetField("operand").GetValue(ins)}));}
   var output=(System.Collections.IEnumerable)transform.Invoke(null,new object[]{input,originalMethod});var count=0;
   foreach(var ins in output){var method=instructionType.GetField("operand").GetValue(ins) as MethodInfo;if(method!=null&&method.DeclaringType==graphType)count++;}
   Check(count==1,"actual IL wraps one typed OwnedFactCollection acquisition including native gain event at "+token.ToString("X8"));
  }
 }
 static void FxHooks(){
  var fxType=mod.GetType("KingmakerMountedCombat.Integration.MountedChargeEnchantmentFx",true);
  var patchMethods=mod.GetType("KingmakerMountedCombat.Integration.MountedPatchController+PatchMethods",true);
  var legacy=fxType.GetMethod("WrapAcquisition",F).GetParameters()[0].ParameterType.GetGenericArguments()[0].Assembly;
  var instanceType=legacy.GetType("Harmony12.HarmonyInstance",true);var harmonyMethod=legacy.GetType("Harmony12.HarmonyMethod",true);
  var harmony=instanceType.GetMethod("Create",F).Invoke(null,new object[]{"KMC.detached.charge-visual-ownership"});
  var patch=instanceType.GetMethod("Patch",F);
  foreach(var token in new[]{0x060099AB,0x060099AF}){
   var transpiler=patchMethods.GetMethod(token==0x060099AB?"ChargeFxRespawnTranspiler":"ChargeFxDestructionTranspiler",F);
   var hm=Activator.CreateInstance(harmonyMethod,new object[]{transpiler});
   patch.Invoke(harmony,new object[]{native.ManifestModule.ResolveMethod(token),null,null,hm});
   Check(true,"actual Harmony12 compiles pinned native FX hook "+token.ToString("X8")+" including native exception regions");
  }
  var enchantment=N("Kingmaker.Blueprints.Items.Ecnchantments.ItemEnchantment");
  Set(enchantment,"Blueprint",Blueprint("Kingmaker.Blueprints.Items.Ecnchantments.BlueprintWeaponEnchantment","30f90becaaac51f41bf56641966c4121"));
  var owners=(System.Collections.IList)fxType.GetField("owners",F).GetValue(null);
  var owner=Blank(fxType);Set(owner,"fact",enchantment);Set(owner,"thread",System.Threading.Thread.CurrentThread.ManagedThreadId);owners.Add(owner);
  Set(owner,"failures",Activator.CreateInstance(Field(fxType,"failures").FieldType));
  Set(owner,"roots",Activator.CreateInstance(Field(fxType,"roots").FieldType));
  Set(owner,"lifetime",Activator.CreateInstance(Field(fxType,"lifetime").FieldType));
  var rootType=fxType.GetNestedType("Root",F);var fxRoot=Blank(rootType);
  var goType=((MethodInfo)native.ManifestModule.ResolveMethod(0x060099A9)).ReturnType;var go=Blank(goType);
  var controllerType=native.ManifestModule.ResolveMethod(0x0600111A).DeclaringType;var controller=Blank(native.ManifestModule.ResolveMethod(0x0600114E).DeclaringType);
  var acquisition=Call(Get(owner,"lifetime"),"Capture",go);Set(fxRoot,"Acquisition",acquisition);
  var controllers=Array.CreateInstance(controllerType,1);controllers.SetValue(controller,0);Set(fxRoot,"Controllers",controllers);
  ((System.Collections.IList)Get(owner,"roots")).Add(fxRoot);
  foreach(var boundary in new[]{"BeginController","BeginRelease"}){
   var argument=boundary=="BeginController"?controller:go;var begin=fxType.GetMethod(boundary,F);var end=fxType.GetMethod("EndRespawn",F);
   foreach(var uncertain in new[]{false,true}){
    Set(fxRoot,"CustodyUncertain",uncertain);var scope=begin.Invoke(null,new[]{argument});
    Check(Object.ReferenceEquals(Get(scope,"Owner"),uncertain?null:owner),"native "+boundary+" attaches only confirmed current custody: uncertain="+uncertain);
    fxType.GetMethod("Returned",F).Invoke(null,new[]{scope});end.Invoke(null,new[]{scope});
   }
  }
  Set(fxRoot,"CustodyUncertain",false);
  var modern=Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(native.Location),"UnityModManager/0Harmony.dll"));
  var read=modern.GetType("HarmonyLib.PatchProcessor",true).GetMethods(F).Single(m=>m.Name=="GetOriginalInstructions"&&m.GetParameters().Length==2&&!m.GetParameters()[1].ParameterType.IsByRef);
  var transform=fxType.GetMethod("WrapAcquisition",F);var instructionType=transform.GetParameters()[0].ParameterType.GetGenericArguments()[0];var listType=typeof(System.Collections.Generic.List<>).MakeGenericType(instructionType);
  foreach(var token in new[]{0x060010BD,0x06001109,0x0600111A,0x060099AF}){
   var original=native.ManifestModule.ResolveMethod(token);var input=(System.Collections.IList)Activator.CreateInstance(listType);
   foreach(var ins in (System.Collections.IEnumerable)read.Invoke(null,new object[]{original,null})){var t=ins.GetType();input.Add(Activator.CreateInstance(instructionType,new[]{t.GetField("opcode").GetValue(ins),t.GetField("operand").GetValue(ins)}));}
   var output=(System.Collections.IEnumerable)(token==0x060099AF?fxType.GetMethod("WrapDestruction",F):transform).Invoke(null,new object[]{input,original});var count=0;
   foreach(var ins in output){var method=instructionType.GetField("operand").GetValue(ins) as MethodInfo;if(method!=null&&method.DeclaringType==fxType)count++;}
   Check(count==(token==0x060010BD?2:1),"actual native IL captures each root/copy before later native setup at "+token.ToString("X8"));
  }
  // CLR cannot JIT Unity's pooled bodies outside the game (ECall security). Execute
  // the production finally transformer with a substituted external native body.
  // Actual Unity pool/instantiate behavior remains a native qualification gate.
  var dynamic=new System.Reflection.Emit.DynamicMethod("owned_respawn_scope",typeof(void),new[]{enchantment.GetType()},typeof(ChargeBuffSurfaceProbe).Module,true);
  var il=dynamic.GetILGenerator();var body=(System.Collections.IList)Activator.CreateInstance(listType);
  body.Add(Activator.CreateInstance(instructionType,new object[]{System.Reflection.Emit.OpCodes.Call,typeof(ChargeBuffSurfaceProbe).GetMethod("SimulatedNativeFxBody",F)}));
  body.Add(Activator.CreateInstance(instructionType,new object[]{System.Reflection.Emit.OpCodes.Ret,null}));
  var wrapped=(System.Collections.IEnumerable)fxType.GetMethod("WrapRespawn",F).Invoke(null,new object[]{body,il,native.ManifestModule.ResolveMethod(0x060099AB)});
  foreach(var ins in wrapped){
   var blocks=((System.Collections.IEnumerable)instructionType.GetField("blocks").GetValue(ins)).Cast<object>().ToArray();
   foreach(var block in blocks){var kind=block.GetType().GetField("blockType",F).GetValue(block).ToString();if(kind=="BeginExceptionBlock")il.BeginExceptionBlock();else if(kind=="BeginFinallyBlock")il.BeginFinallyBlock();}
   foreach(var label in (System.Collections.IEnumerable)instructionType.GetField("labels").GetValue(ins))il.MarkLabel((System.Reflection.Emit.Label)label);
   var op=(System.Reflection.Emit.OpCode)instructionType.GetField("opcode").GetValue(ins);var operand=instructionType.GetField("operand").GetValue(ins);
   if(operand==null)il.Emit(op);else if(operand is MethodInfo)il.Emit(op,(MethodInfo)operand);else if(operand is System.Reflection.Emit.LocalBuilder)il.Emit(op,(System.Reflection.Emit.LocalBuilder)operand);else if(operand is System.Reflection.Emit.Label)il.Emit(op,(System.Reflection.Emit.Label)operand);else throw new InvalidOperationException("Unexpected probe emitter operand");
   foreach(var block in blocks)if(block.GetType().GetField("blockType",F).GetValue(block).ToString()=="EndExceptionBlock")il.EndExceptionBlock();
  }
  var invoke=dynamic.CreateDelegate(typeof(Action<>).MakeGenericType(enchantment.GetType()));
  try{
   nativeFxBody=()=>Check((int)Get(owner,"scopes")==1&&(int)Get(owner,"pendingRoots")==1&&fxType.GetField("current",F).GetValue(null)!=null,"entire native respawn body retains scope and capacity during reentrant callbacks");
   invoke.DynamicInvoke(enchantment);
   Check((int)Get(owner,"scopes")==0&&(int)Get(owner,"pendingRoots")==0&&Get(owner,"fault")==null&&fxType.GetField("current",F).GetValue(null)==null,
    "executed production finally wrapper closes exact owned scope on normal native return");
   nativeFxBody=()=>{throw new InvalidOperationException("external native body failed");};var thrown=false;
   try{invoke.DynamicInvoke(enchantment);}catch(TargetInvocationException e){thrown=e.InnerException is InvalidOperationException;}
   Check(thrown&&(int)Get(owner,"scopes")==0&&(int)Get(owner,"pendingRoots")==0&&((System.Collections.IList)Get(owner,"failures")).Count==1&&fxType.GetField("current",F).GetValue(null)==null,
    "executed production finally wrapper propagates native exception and retains fault without leaking scope");
   var reserved=new System.Collections.Generic.List<object>();var refused=false;
   try{
    for(var i=0;i<15;i++)reserved.Add(fxType.GetMethod("BeginRespawn",F).Invoke(null,new[]{enchantment}));
    try{fxType.GetMethod("BeginRespawn",F).Invoke(null,new[]{enchantment});}catch(TargetInvocationException e){refused=e.InnerException is InvalidOperationException;}
    Check(refused&&(int)Get(owner,"pendingRoots")==15&&(int)Get(owner,"scopes")==15,
     "nested native respawns reserve finite capacity before any new acquisition");
   }finally{reserved.Reverse();foreach(var scope in reserved){fxType.GetMethod("Returned",F).Invoke(null,new[]{scope});fxType.GetMethod("EndRespawn",F).Invoke(null,new[]{scope});}}
   var firstFault=Get(owner,"fault");refused=false;
   try{fxType.GetMethod("BeginRespawn",F).Invoke(null,new[]{enchantment});}catch(TargetInvocationException e){refused=e.InnerException is InvalidOperationException;}
   Check(refused&&Object.Equals(firstFault,Get(owner,"fault"))&&(int)Get(owner,"pendingRoots")==0&&(int)Get(owner,"scopes")==0,
    "closed capacity scopes retain first fault and refuse further native acquisitions");
   Set(owner,"fault",null);var copies=(System.Collections.IList)Activator.CreateInstance(Field(fxType,"copies").FieldType);Set(owner,"copies",copies);
   copies.Add(acquisition);Set(owner,"pendingCopies",1);refused=false;
   // Execute the production preflight directly: Unity Instantiate's ECall body
   // cannot be JITted by the detached CLR, even when its guard would refuse.
   try{fxType.GetMethod("RequireCopyCapacity",F).Invoke(owner,new object[0]);}catch(TargetInvocationException e){refused=e.InnerException is InvalidOperationException;}
   Check(refused&&copies.Count==1&&(int)Get(owner,"pendingCopies")==1,
    "native copy admission counts pending acquisition before touching the Unity boundary");
   copies.Clear();Set(owner,"pendingCopies",0);
  }finally{owners.Remove(owner);nativeFxBody=null;}
 }
 static Action nativeFxBody;
 static void SimulatedNativeFxBody(){nativeFxBody();}
 public static void Run(string managed,string optional,string dll){
  AppDomain.CurrentDomain.AssemblyResolve+=(s,a)=>{var leaf=new AssemblyName(a.Name).Name+".dll";foreach(var dir in new[]{managed,Path.Combine(managed,"UnityModManager"),Path.GetDirectoryName(optional)}){var p=Path.Combine(dir,leaf);if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};
  native=Assembly.LoadFrom(Path.Combine(managed,"Assembly-CSharp.dll"));cotw=Assembly.LoadFrom(optional);mod=Assembly.LoadFrom(dll);
  Check(native.ManifestModule.ModuleVersionId.ToString()=="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"&&cotw.ManifestModule.ModuleVersionId.ToString()=="8caab254-aacf-4811-8093-44b9184e6e53","exact native and optional module identities");
  var root=Root(false);Check(Read(root)!=null,"base three-component native surface is accepted");
  root=Root(true);var surface=Read(root);Check(surface!=null,"complete inspected COTW six-component surface is accepted without executing or replacing its actions");
  Check(((Array)surface.GetType().GetProperty("Children",F).GetValue(surface,null)).Length==3&&((Array)surface.GetType().GetProperty("Enchantments",F).GetValue(surface,null)).Length==2,"validated lifetime inventory includes exact three children and two potential enchantments");
  root=Root(true);Refuses(root,()=>Set(Comp(root,1),"Value",-1),"changed original armor penalty is refused");
  root=Root(true);Refuses(root,()=>Set(Comp(root,2),"Condition",20),"changed native condition is refused");
  root=Root(true);Refuses(root,()=>Set(Comp(root,4),"Stat",1),"unexpected contextual stat mutation is refused");
  root=Root(true);Refuses(root,()=>Set(Comp(root,5),"m_Progression",0),"uninspected contextual rank is refused");
  root=Root(true);Refuses(root,()=>Set(Comp(Child(root,0),0),"ActionsOnInitiator",true),"target consequences cannot silently become rider lifetime mutations");
  root=Root(true);Refuses(root,()=>Set(Comp(Child(root,1),0),"lock_slot",true),"unowned equipment-slot locking is refused");
  root=Root(true);Refuses(root,()=>Set(Comp(Child(root,1),0),"in_off_hand",true),"uninspected alternate equipment ownership is refused");
  root=Root(true);Refuses(root,()=>Set(Child(root,1),"Stacking",1),"child adoption or stacking policy change is refused");
  root=Root(true);Refuses(root,()=>Set(Comp(root,0),"NewRound",List(N("Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRemoveBuff"))),"unbounded repeating native effects are refused");
  root=Root(true);Refuses(root,()=>{var enchant=((Array)Get(Comp(Child(root,1),0),"enchantments")).GetValue(0);ArrayField(enchant,"Components",N("Kingmaker.UnitLogic.FactLogic.AddCondition"));},"same-GUID enchantment with unaudited lifetime mutation is refused");
  ChildOwnership();
  FxHooks();
  Console.WriteLine("CHARGE BUFF SURFACE PASS="+passed+" FAIL=0; detached input contract, not loaded-graph or gameplay proof");
 }
}
'@
[ChargeBuffSurfaceProbe]::Run($managed,$cotw,(Join-Path $repo "bin/$Configuration/KingmakerMountedCombat.dll"))
