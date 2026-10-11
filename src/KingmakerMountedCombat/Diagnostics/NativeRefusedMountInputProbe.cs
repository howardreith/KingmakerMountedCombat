using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony12;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // A separate zero-command observation window; never weakens the positive command probe.
    internal sealed class NativeRefusedMountInputProbe : IDisposable
    {
        private const string HarmonyId="KingmakerMountedCombat.Diagnostics.RefusedMountInput";
        private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        private static NativeRefusedMountInputProbe active;
        private readonly HarmonyInstance harmony;
        private readonly NativeMountedControlService controls;
        private readonly NativeActorAllocationTrace trace;
        private readonly UnitEntityData rider,mount,target;
        private readonly AbilityData ability;
        private readonly Func<JObject> state;
        private readonly bool priorReactionObservation;
        private readonly JArray hooks=new JArray(),events=new JArray(),errors=new JArray();
        private readonly List<UnitUseAbility> created=new List<UnitUseAbility>();
        private int startSequence;
        private ClickWithSelectedAbilityHandler observedHandler;
        private bool invoked,disposed;
        private JObject before,after;
        internal NativeRefusedMountInputProbe(NativeMountedControlService controls,NativeActorAllocationTrace trace,
            UnitEntityData rider,UnitEntityData mount,UnitEntityData target,Func<JObject> state)
        {
            if(active!=null)throw new InvalidOperationException("Refused Mount input observer is already active.");
            this.controls=controls;this.trace=trace;this.rider=rider;this.mount=mount;this.target=target;this.state=state;
            ability=rider?.Descriptor.Abilities.GetAbility(controls.MountAbility)?.Data;
            if(ability==null || target?.View==null || Game.Instance.SelectedAbilityHandler.Ability!=null)
                throw new InvalidOperationException("Refused Mount requires the real leased rider ability, live target and empty native target selector.");
            if(typeof(UnitUseAbility).Assembly.ManifestModule.ModuleVersionId!=new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                throw new InvalidOperationException("Refused Mount observation requires the pinned native module.");
            harmony=HarmonyInstance.Create(HarmonyId);active=this;priorReactionObservation=trace.ObserveReactionResources;
            trace.ObserveReactionResources=true;
            try
            {
                foreach(var token in new[]{0x06002726,0x06002727})
                    Patch(typeof(UnitUseAbility).GetConstructors(Flags).Single(m=>m.MetadataToken==token),null,"Constructed");
                Patch(typeof(ClickWithSelectedAbilityHandler).GetMethods(Flags).Single(m=>m.MetadataToken==0x060093F6),"ClickBefore","ClickAfter");
            }
            catch {Dispose();throw;}
        }
        private void Patch(MethodBase method,string prefix,string postfix)
        {
            harmony.Patch(method,prefix==null?null:new HarmonyMethod(typeof(Hooks).GetMethod(prefix,Flags)){prioritiy=Priority.First},
                postfix==null?null:new HarmonyMethod(typeof(Hooks).GetMethod(postfix,Flags)){prioritiy=Priority.Last});
            hooks.Add(new JObject {["method"]=method.DeclaringType.FullName+"."+method.Name,["token"]=method.MetadataToken.ToString("X8"),
                ["moduleMvid"]=method.Module.ModuleVersionId.ToString(),["prefix"]=prefix,["postfix"]=postfix});
        }
        private JObject Snapshot()
        {
            var c=controls.CaptureSnapshot();
            return new JObject {["frame"]=Time.frameCount,["gameTicks"]=Game.Instance.TimeController.GameTime.Ticks,
                ["allocationSequence"]=trace.EventCount,["state"]=state(),["riderCommandsEmpty"]=rider.Commands.Empty,["mountCommandsEmpty"]=mount.Commands.Empty,["shellCount"]=controls.NativeRelationshipShellCount,["processBindings"]=controls.NativeRelationshipProcessBindingCount,
                ["castCount"]=c.NativeCastRequestCount,["refusalCount"]=c.NativeRefusalCount,
                ["targetStartCount"]=c.TargetSelectionStartCount,["targetEndCount"]=c.TargetSelectionEndCount,
                ["dispatchAccepted"]=c.DispatchAcceptedCount,["dispatchRejected"]=c.DispatchRejectedCount,
                ["targetSelectorEmpty"]=Game.Instance.SelectedAbilityHandler.Ability==null,
                ["activationSequence"]=controls.SnapshotAbilityActivations().LastOrDefault()?.Sequence ?? 0};
        }
        internal bool Invoke()
        {
            if(disposed || invoked)throw new InvalidOperationException("Refused Mount click may occur only once.");
            invoked=true;startSequence=trace.EventCount;before=Snapshot();
            var handler=Game.Instance.SelectedAbilityHandler;
            try {handler.SetAbility(ability);return handler.OnClick(target.View.gameObject,target.Position,0,false,false);}
            finally {handler.DropAbility();after=Snapshot();}
        }
        private void Observe(string boundary,ClickWithSelectedAbilityHandler handler,GameObject clicked,Vector3 point,int button,bool simulate,bool mute,bool? result)
        {
            if(disposed)return;
            if(!result.HasValue){if(!ReferenceEquals(handler.Ability,ability))return;observedHandler=handler;}
            else if(!ReferenceEquals(handler,observedHandler))return;
            try
            {
                if(events.Count>=8){errors.Add("Refused Mount input observation bound exceeded.");return;}
                events.Add(new JObject {["boundary"]=boundary,["frame"]=Time.frameCount,["gameTicks"]=Game.Instance.TimeController.GameTime.Ticks,
                    ["handlerObject"]=RuntimeHelpers.GetHashCode(handler),["abilityObject"]=RuntimeHelpers.GetHashCode(ability),
                    ["currentSelectorAbilityObject"]=handler.Ability==null?0:RuntimeHelpers.GetHashCode(handler.Ability),
                    ["abilityGuid"]=ability.Blueprint.AssetGuid,["casterId"]=ability.Caster.Unit.UniqueId,
                    ["targetId"]=target.UniqueId,["exactTargetObject"]=clicked==target.View.gameObject,
                    ["exactTargetPoint"]=point==target.Position,["button"]=button,["simulate"]=simulate,["muteEvents"]=mute,["result"]=result});
            }
            catch(Exception e){errors.Add(boundary+": "+e);}
        }
        internal JObject Capture()=>new JObject {["contract"]="synchronous-refused-native-mount-with-zero-command-construction",
            ["riderId"]=rider.UniqueId,["mountId"]=mount.UniqueId,["targetId"]=target.UniqueId,
            ["observerHooks"]=hooks.DeepClone(),["before"]=before?.DeepClone(),["after"]=after?.DeepClone(),
            ["events"]=events.DeepClone(),["constructedCommands"]=new JArray(created.Select(c=>new JObject{
                ["commandObject"]=RuntimeHelpers.GetHashCode(c),["casterId"]=c.Spell?.Caster?.Unit?.UniqueId,["targetId"]=c.Target?.Unit?.UniqueId})),
            ["allocationEvents"]=after==null?new JArray():new JArray(trace.EventsSince(startSequence).Take((int)after["allocationSequence"]-startSequence)),
            ["activations"]=after==null?new JArray():new JArray(controls.SnapshotAbilityActivations().Where(a=>a.Sequence>(long)before["activationSequence"] && a.Sequence<=(long)after["activationSequence"]).Select(a=>new JObject {
                ["sequence"]=a.Sequence,["activationId"]=a.ActivationId,["phase"]=a.Phase.ToString(),["casterId"]=a.CasterId,
                ["targetId"]=a.TargetId,["selectedIds"]=a.ActiveSelectedUnitIds,["abilityGuid"]=a.AbilityGuid,["frame"]=a.Frame,["reason"]=a.TerminalResult})),["traceComplete"]=trace.Complete,["errors"]=errors.DeepClone()};
        public void Dispose()
        {
            if(disposed)return;disposed=true;trace.ObserveReactionResources=priorReactionObservation;
            harmony.UnpatchAll(HarmonyId);if(ReferenceEquals(active,this))active=null;
        }
        private static class Hooks
        {
            internal static void Constructed(UnitUseAbility __instance)
            {
                var p=active;if(p==null)return;
                try {
                    if(__instance.Spell?.Caster?.Unit!=p.rider || __instance.Spell.Blueprint!=p.controls.MountAbility)return;
                    if(p.created.Count>=16){p.errors.Add("Refused Mount construction bound exceeded.");return;}
                    if(!p.created.Any(c=>ReferenceEquals(c,__instance)))p.created.Add(__instance);
                } catch(Exception e){p.errors.Add("Constructor observation: "+e);}
            }
            internal static void ClickBefore(ClickWithSelectedAbilityHandler __instance,GameObject gameObject,Vector3 worldPosition,int button,bool simulate,bool muteEvents)
                =>active?.Observe("click-before",__instance,gameObject,worldPosition,button,simulate,muteEvents,null);
            internal static void ClickAfter(ClickWithSelectedAbilityHandler __instance,GameObject gameObject,Vector3 worldPosition,int button,bool simulate,bool muteEvents,bool __result)
                =>active?.Observe("click-after",__instance,gameObject,worldPosition,button,simulate,muteEvents,__result);
        }
    }
}
