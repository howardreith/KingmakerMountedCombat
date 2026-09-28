using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Brain;
using Kingmaker.Controllers.Brain.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.ActionBar;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    // Future diagnostic only. The sole fixture mutation is an explicit reversible
    // native AutoUseAbility lease. No command is admitted and no resource is written.
    internal sealed class NativeAutoUseControlProbe : IDisposable
    {
        private const string HarmonyId="KingmakerMountedCombat.Diagnostics.AutoUseControl";
        private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        private static NativeAutoUseControlProbe active;
        private readonly NativeMountedControlService controls;
        private readonly NativeActorAllocationTrace trace;
        private readonly ActionBarSlot slot;
        private readonly UnitEntityData rider,mount,enemy;
        private readonly AbilityData ability,originalAuto,ordinary;
        private readonly MechanicActionBarSlotAbility mechanic;
        private readonly Func<JObject> state;
        private readonly NativePassiveResourceProbe resources;
        private readonly HarmonyInstance harmony;
        private readonly ScriptableObject ai;
        private static readonly Type AiType=typeof(UnitEntityData).Assembly.GetType("Kingmaker.Controllers.Brain.Blueprints.BlueprintAiAttack",true);
        private static readonly MethodInfo CreateAi=AiType.GetMethods(Flags).Single(m=>m.MetadataToken==0x06009434);
        private readonly JObject registration;
        private readonly JArray events=new JArray(),hooks=new JArray(),errors=new JArray();
        private readonly List<UnitUseAbility> constructed=new List<UnitUseAbility>();
        private readonly JArray constructorCallbacks=new JArray(),constructedEvidence=new JArray();
        private string phase="construction";
        private int ordinal;
        private bool invoked,disposed,leaseOwned;
        private JObject evidence;
        internal NativeAutoUseControlProbe(NativeMountedControlService controls,NativeActorAllocationTrace trace,
            UnitEntityData rider,UnitEntityData mount,UnitEntityData enemy,ActionBarSlot slot,AbilityData ordinaryControl,Func<JObject> capture)
        {
            if(active!=null)throw new InvalidOperationException("Auto-use observer already owned.");
            this.controls=controls;this.trace=trace;this.slot=slot;this.rider=rider;this.mount=mount;this.enemy=enemy;state=capture;ordinary=ordinaryControl;
            NativeMountActionBarProbe.RequireSelection(rider);
            mechanic=slot?.MechanicSlot as MechanicActionBarSlotAbility;ability=mechanic?.Ability;originalAuto=rider.AutoUseAbility;
            if(controls==null||trace==null||capture==null||mount==null||enemy==null||enemy==rider||enemy==mount||
                !rider.IsEnemy(enemy)||!enemy.IsEnemy(rider)||!rider.IsInCombat||!mount.IsInCombat||!enemy.IsInCombat||
                !rider.Commands.Empty||!mount.Commands.Empty||CombatController.IsInTurnBasedCombat()||!Game.Instance.IsPaused||
                slot==null||!slot.gameObject.activeInHierarchy||mechanic.Unit!=rider||Game.Instance.SelectedAbilityHandler?.Ability!=null||ability?.Caster?.Unit!=rider||
                !controls.IsPlayerOnlyRelationshipControl(ability)||controls.IsPlayerOnlyRelationshipControl(originalAuto)||
                ordinary?.Caster?.Unit!=rider||controls.IsPlayerOnlyRelationshipControl(ordinary))
                throw new InvalidOperationException("Auto-use requires a paused RT pair, exact live rider control slot, hostile disposable target and no pre-existing relationship auto-use reference.");
            if(typeof(AbilityData).Assembly.ManifestModule.ModuleVersionId.ToString()!="07fa1e4d-8618-41b3-9b8d-faa17d3b26f7")throw new InvalidOperationException("Auto-use native module differs.");
            registration=(JObject)typeof(NativeMountActionBarProbe).GetMethod("RequireLiveRegistration",Flags).Invoke(null,new object[]{rider,slot});
            harmony=HarmonyInstance.Create(HarmonyId);active=this;
            try {
                resources=new NativePassiveResourceProbe(trace,rider,mount);
                ai=ScriptableObject.CreateInstance(AiType);
                Patch(typeof(AbilityData),0x06002B30,"SuitableBefore","SuitableAfter");
                Patch(typeof(MechanicActionBarSlotAbility),0x06002F5F,"ToggleBefore","ToggleAfter");
                Patch(typeof(UnitEntityData),0x06008370,"AvailableBefore","AvailableAfter");
                Patch(AiType,0x06009434,"AiBefore","AiAfter");
                foreach(var token in new[]{0x06002726,0x06002727}) {
                    var method=typeof(UnitUseAbility).GetConstructors(Flags).Single(m=>m.MetadataToken==token);
                    harmony.Patch(method,null,new HarmonyMethod(typeof(Hooks).GetMethod("Constructed",Flags)){prioritiy=Priority.Last});
                    hooks.Add(Hook(method,null,"Constructed"));
                }
            }catch{Dispose();throw;}
        }
        private static int Id(object o)=>o==null?0:RuntimeHelpers.GetHashCode(o);
        private static JObject Hook(MethodBase m,string before,string after)=>new JObject{["method"]=m.DeclaringType.FullName+"."+m.Name,["token"]=m.MetadataToken.ToString("X8"),["moduleMvid"]=m.Module.ModuleVersionId.ToString(),["prefix"]=before,["postfix"]=after};
        private void Patch(Type type,int token,string before,string after){var m=type.GetMethods(Flags).Single(x=>x.MetadataToken==token);harmony.Patch(m,new HarmonyMethod(typeof(Hooks).GetMethod(before,Flags)){prioritiy=Priority.First},new HarmonyMethod(typeof(Hooks).GetMethod(after,Flags)){prioritiy=Priority.Last});hooks.Add(Hook(m,before,after));}
        private JObject Snapshot()=>new JObject{["frame"]=Time.frameCount,["gameTicks"]=Game.Instance.TimeController.GameTime.Ticks,["allocationSequence"]=trace.EventCount,["selectedAbilityObject"]=Id(Game.Instance.SelectedAbilityHandler?.Ability),["paused"]=Game.Instance.IsPaused,["turnBased"]=CombatController.IsInTurnBasedCombat(),["autoUseObject"]=Id(rider.AutoUseAbility),["autoUseGuid"]=rider.AutoUseAbility?.Blueprint?.AssetGuid,["riderCommandsEmpty"]=rider.Commands.Empty,["mountCommandsEmpty"]=mount.Commands.Empty,["state"]=state(),["shellCount"]=controls.NativeRelationshipShellCount,["processBindings"]=controls.NativeRelationshipProcessBindingCount,["controls"]=JObject.FromObject(controls.CaptureSnapshot())};
        private JObject Begin(string kind,object receiver,AbilityData value=null){
            try{if(events.Count>=128)throw new InvalidOperationException("Auto-use observation bound exceeded.");var e=new JObject{["phase"]=phase,["kind"]=kind,["receiverObject"]=Id(receiver),["abilityObject"]=Id(value),["abilityGuid"]=value?.Blueprint?.AssetGuid,["casterId"]=value?.Caster?.Unit?.UniqueId,["enterOrdinal"]=++ordinal,["frame"]=Time.frameCount,["gameTicks"]=Game.Instance.TimeController.GameTime.Ticks,["complete"]=false};events.Add(e);return e;}catch(Exception e){errors.Add(e.ToString());return null;}
        }
        private void End(JObject e,JToken result){if(e==null)return;try{e["exitOrdinal"]=++ordinal;e["exitFrame"]=Time.frameCount;e["exitGameTicks"]=Game.Instance.TimeController.GameTime.Ticks;e["result"]=result;e["complete"]=true;}catch(Exception x){errors.Add(x.ToString());}}
        private static JObject Command(UnitCommand c)=>new JObject{["object"]=Id(c),["type"]=c?.GetType().FullName,["action"]=c?.Type.ToString(),["executorId"]=c?.Executor?.UniqueId,["targetId"]=c?.Target?.Unit?.UniqueId,["started"]=c?.IsStarted,["acted"]=c?.IsActed,["finished"]=c?.IsFinished,["abilityGuid"]=(c as UnitUseAbility)?.Spell?.Blueprint?.AssetGuid};
        internal JObject Invoke(){
            if(disposed||invoked)throw new InvalidOperationException("Auto-use probe may run once.");
            NativeMountActionBarProbe.RequireSelection(rider);
            if(!ReferenceEquals(slot.MechanicSlot,mechanic)||!ReferenceEquals(mechanic.Ability,ability)||Game.Instance.SelectedAbilityHandler?.Ability!=null)throw new InvalidOperationException("Exact auto-use slot changed before invocation.");
            var live=(JObject)typeof(NativeMountActionBarProbe).GetMethod("RequireLiveRegistration",Flags).Invoke(null,new object[]{rider,slot});
            if(!JToken.DeepEquals(live,registration))throw new InvalidOperationException("Live auto-use slot registration changed.");
            invoked=true;
            evidence=new JObject{["contract"]="native-ui-auto-use-and-ai-revalidation-of-explicit-stale-reference",["riderId"]=rider.UniqueId,["mountId"]=mount.UniqueId,["targetId"]=enemy.UniqueId,["riderObject"]=Id(rider),["mountObject"]=Id(mount),["targetObject"]=Id(enemy),["mechanicObject"]=Id(mechanic),["aiObject"]=Id(ai),["abilityObject"]=Id(ability),["abilityGuid"]=ability.Blueprint.AssetGuid,["ordinaryAbilityObject"]=Id(ordinary),["ordinaryAbilityGuid"]=ordinary.Blueprint.AssetGuid,["slotRegistration"]=registration.DeepClone(),["before"]=Snapshot(),["staleReferenceFixtureDeclared"]=true};
            try {
                var context=new DecisionContext{Unit=rider};evidence["contextObject"]=Id(context);
                phase="constructor-control";evidence["ordinarySuitable"]=ordinary.IsSuitableForAutoUse;
                if(!(bool)evidence["ordinarySuitable"])throw new InvalidOperationException("Ordinary auto-use control must be natively eligible.");
                var constructorControl=new UnitUseAbility(ordinary,new Kingmaker.Utility.TargetWrapper(enemy));
                evidence["constructorControl"]=Command(constructorControl);
                evidence["constructorObserverExecuted"]=constructed.Any(c=>ReferenceEquals(c,constructorControl));
                if(!(bool)evidence["constructorObserverExecuted"])throw new InvalidOperationException("Native UnitUseAbility constructor observer did not execute.");
                phase="baseline-ai";evidence["baselineAi"]=Command((UnitCommand)CreateAi.Invoke(ai,new object[]{context,enemy}));
                phase="ui-toggle";evidence["suitable"]=ability.IsSuitableForAutoUse;mechanic.OnAutoUseToggle();evidence["afterUi"]=Snapshot();
                if(!ReferenceEquals(rider.AutoUseAbility,originalAuto)){leaseOwned=ReferenceEquals(rider.AutoUseAbility,ability);throw new InvalidOperationException("Native UI auto-use toggle changed the saved reference.");}
                phase="stale-reference";rider.AutoUseAbility=ability;leaseOwned=true;evidence["afterLease"]=Snapshot();
                var available=rider.GetAvailableAutoUseAbility();evidence["availableObject"]=Id(available);evidence["availableGuid"]=available?.Blueprint?.AssetGuid;
                evidence["aiWithStaleReference"]=Command((UnitCommand)CreateAi.Invoke(ai,new object[]{context,enemy}));evidence["afterAi"]=Snapshot();
            } catch(Exception failure){evidence["failure"]=failure.ToString();throw;} finally {
                phase="restore";try{RestoreLease();}catch(Exception failure){errors.Add("Restore: "+failure);}
                try{evidence["afterRestoration"]=Snapshot();evidence["resources"]=resources.Finish();}catch(Exception failure){errors.Add("Final observation: "+failure);}
                evidence["observerHooks"]=hooks.DeepClone();evidence["events"]=events.DeepClone();evidence["constructedAbilityCommands"]=constructedEvidence.DeepClone();evidence["constructorCallbacks"]=constructorCallbacks.DeepClone();evidence["errors"]=errors.DeepClone();
            }
            try { NativeAutoUseControlEvidence.AssertComplete(evidence); evidence["pass"]=true; } catch(Exception failure){evidence["pass"]=false;evidence["validationFailure"]=failure.Message;}
            return (JObject)evidence.DeepClone();
        }
        private void RestoreLease(){
            if(!leaseOwned)return;
            if(!ReferenceEquals(rider.AutoUseAbility,ability))throw new InvalidOperationException("Auto-use fixture lease was changed by another writer.");
            rider.AutoUseAbility=originalAuto;leaseOwned=false;
            if(!ReferenceEquals(rider.AutoUseAbility,originalAuto))throw new InvalidOperationException("Auto-use reference restoration differs.");
        }
        internal JObject Capture()=>evidence==null?new JObject{["events"]=events.DeepClone(),["observerHooks"]=hooks.DeepClone(),["errors"]=errors.DeepClone()}:(JObject)evidence.DeepClone();
        public void Dispose(){if(disposed)return;try{RestoreLease();}finally{disposed=true;resources?.Dispose();harmony?.UnpatchAll(HarmonyId);if(ai!=null)UnityEngine.Object.Destroy(ai);if(ReferenceEquals(active,this))active=null;}}
        private static class Hooks {
            internal static void SuitableBefore(AbilityData __instance,out JObject __state){var p=active;__state=p!=null&&__instance.Caster?.Unit==p.rider?p.Begin("suitability",__instance,__instance):null;}
            internal static void SuitableAfter(JObject __state,bool __result)=>active?.End(__state,new JValue(__result));
            internal static void ToggleBefore(MechanicActionBarSlotAbility __instance,out JObject __state){var p=active;__state=p!=null&&ReferenceEquals(__instance,p.mechanic)?p.Begin("toggle",__instance,__instance.Ability):null;}
            internal static void ToggleAfter(JObject __state)=>active?.End(__state,JValue.CreateNull());
            internal static void AvailableBefore(UnitEntityData __instance,out JObject __state){var p=active;__state=p!=null&&__instance==p.rider?p.Begin("available",__instance,__instance.AutoUseAbility):null;}
            internal static void AvailableAfter(JObject __state,AbilityData __result)=>active?.End(__state,new JObject{["object"]=Id(__result),["guid"]=__result?.Blueprint?.AssetGuid});
            internal static void AiBefore(object __instance,DecisionContext context,UnitEntityData target,out JObject __state){var p=active;__state=p!=null&&ReferenceEquals(__instance,p.ai)&&context.Unit==p.rider&&target==p.enemy?p.Begin("ai-create",__instance):null;if(__state!=null){__state["contextObject"]=Id(context);__state["casterId"]=context.Unit.UniqueId;__state["targetId"]=target.UniqueId;__state["targetObject"]=Id(target);}}
            internal static void AiAfter(JObject __state,UnitCommand __result)=>active?.End(__state,Command(__result));
            internal static void Constructed(UnitUseAbility __instance,MethodBase __originalMethod){
                var p=active;if(p==null||__instance.Spell?.Caster?.Unit!=p.rider)return;
                try{
                    if(p.constructorCallbacks.Count>=32)throw new InvalidOperationException("Auto-use command construction bound exceeded.");
                    p.constructorCallbacks.Add(new JObject{["phase"]=p.phase,["ordinal"]=++p.ordinal,["frame"]=Time.frameCount,["gameTicks"]=Game.Instance.TimeController.GameTime.Ticks,["token"]=__originalMethod.MetadataToken.ToString("X8"),["command"]=Command(__instance)});
                    if(!p.constructed.Any(c=>ReferenceEquals(c,__instance))){p.constructed.Add(__instance);p.constructedEvidence.Add(new JObject{["phase"]=p.phase,["command"]=Command(__instance)});}
                }catch(Exception e){p.errors.Add(e.ToString());}
            }
        }
    }
}