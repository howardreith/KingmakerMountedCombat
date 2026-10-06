using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // A native fact supplies Slowed so a beyond-maximum target fits the unchanged 3..20m
    // diagnostic envelope. No speed, cooldown, movement budget or preparation is written.
    internal sealed class NativeChargeSlowLease : IDisposable
    {
        private readonly UnitEntityData actor;
        private readonly BlueprintFeature blueprint;
        private readonly AddCondition component;
        private readonly float speedBefore;
        private readonly string blueprintId;
        private Action remove;
        private Func<bool> present;
        private bool restored;
        internal NativeChargeSlowLease(UnitEntityData actor)
        {
            if (actor == null || actor.IsInCombat || actor.Descriptor.State.HasCondition(UnitCondition.Slowed) ||
                actor.Descriptor.State.HasConditionImmunity(UnitCondition.Slowed))
                throw new InvalidOperationException("Charge range fixture requires an unconditioned disposable mount before combat.");
            this.actor = actor; speedBefore = actor.CombatSpeedMps;
            var template = ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Values.OfType<BlueprintFeature>()
                .Single(item => item.name == "RapidShot" || item.Name == "Rapid Shot");
            blueprint = UnityEngine.Object.Instantiate(template);
            blueprint.name = "KMC_DiagnosticChargeSlow"; blueprint.AssetGuid = Guid.NewGuid().ToString("N");
            blueprintId = blueprint.AssetGuid;
            component = ScriptableObject.CreateInstance<AddCondition>();
            component.name = "KMC_DiagnosticNativeSlow"; component.Condition = UnitCondition.Slowed;
            blueprint.ComponentsArray = new BlueprintComponent[] { component };
            present = () => actor.Logic.Enumerable.Any(item => item.Blueprint == blueprint);
            remove = () => { foreach (var item in actor.Logic.Enumerable.Where(item => item.Blueprint == blueprint).ToArray()) actor.Logic.RemoveFact(item); };
        }
        internal void Apply()
        {
            if (restored || present()) throw new InvalidOperationException("Native slow lease cannot be reapplied.");
            var fact = actor.Logic.AddFact(blueprint, new MechanicsContext(actor, actor.Descriptor, blueprint));
            present = () => fact != null && actor.Logic.Enumerable.Any(item => ReferenceEquals(item, fact));
            remove = () => { if (present()) actor.Logic.RemoveFact(fact); };
            if (!present() || !actor.Descriptor.State.HasCondition(UnitCondition.Slowed))
                throw new InvalidOperationException("Native slow fact did not apply.");
        }
        internal JObject Capture() => new JObject
        {
            ["actor"] = actor.UniqueId, ["blueprint"] = blueprintId, ["nativeComponent"] = typeof(AddCondition).FullName,
            ["condition"] = "Slowed", ["present"] = present?.Invoke() == true,
            ["slowed"] = actor.Descriptor.State.HasCondition(UnitCondition.Slowed),
            ["speedBefore"] = speedBefore, ["speedNow"] = actor.CombatSpeedMps,
            ["maximumNow"] = actor.CombatSpeedMps * 6f, ["restored"] = restored
        };
        public void Dispose()
        {
            if (restored) return;
            remove?.Invoke();
            if (present?.Invoke() == true || actor.Descriptor.State.HasCondition(UnitCondition.Slowed) ||
                Math.Abs(actor.CombatSpeedMps - speedBefore) > 0.001f)
                throw new InvalidOperationException("Owned native slow fact did not restore exactly.");
            restored = true;
            UnityEngine.Object.Destroy(blueprint);
            UnityEngine.Object.Destroy(component);
        }
    }
}
