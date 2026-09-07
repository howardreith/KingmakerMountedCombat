using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Disposable fixture only. The existing native fast-healing component
    // supplies an observable per-round effect through the real fact pipeline.
    internal sealed class NativeAllocationRoundFactLease : IDisposable, IGlobalRulebookHandler<RuleApplyBuff>
    {
        private readonly UnitEntityData actor;
        private readonly int originalDamage;
        private readonly BlueprintBuff blueprint;
        private readonly AddEffectFastHealing component;
        private readonly string blueprintId;
        private Buff buff;
        private bool disposed;
        private readonly JObject application = new JObject();

        internal NativeAllocationRoundFactLease(UnitEntityData actor)
        {
            if (actor == null || actor.IsInCombat || !actor.Descriptor.State.IsConscious ||
                actor.Stats.HitPoints.ModifiedValue - actor.Damage <= 6)
                throw new InvalidOperationException("Native round fact requires a healthy disposable actor before combat.");
            this.actor = actor;
            originalDamage = actor.Damage;
            // Clone the already qualified native fixture buff to retain its
            // required serialization defaults, then replace its behavior. A
            // bare ScriptableObject leaves native FX links null during activation.
            var template = ResourcesLibrary.LibraryObject.BlueprintsByAssetId["03464790f40c3c24aa684b57155f3280"] as BlueprintBuff;
            if (template == null || template.name != "HasteBuff")
                throw new InvalidOperationException("Qualified native buff fixture template is unavailable.");
            blueprint = UnityEngine.Object.Instantiate(template);
            component = ScriptableObject.CreateInstance<AddEffectFastHealing>();
            blueprint.name = "KMC_DiagnosticAllocationRoundFact";
            blueprintId = Guid.NewGuid().ToString("N");
            blueprint.AssetGuid = blueprintId;
            component.Heal = 1;
            component.name = "KMC_DiagnosticFastHealing";
            blueprint.ComponentsArray = new BlueprintComponent[] { component };
            blueprint.FxOnStart = new PrefabLink();
            blueprint.FxOnRemove = new PrefabLink();
            blueprint.ResourceAssetIds = new string[0];
            try
            {
                actor.Descriptor.Damage = originalDamage + 6;
                using (EventBus.Subscribe(this))
                    buff = actor.Buffs.AddBuff(blueprint, actor, TimeSpan.FromMinutes(10), null);
                if (buff == null) throw new InvalidOperationException("Native round fact did not activate; actor=" + actor.UniqueId + "; admission=" + application);
            }
            catch { Dispose(); throw; }
        }

        internal JObject Capture() => new JObject {
            ["actor"] = actor.UniqueId, ["originalDamage"] = originalDamage,
            ["currentDamage"] = actor.Damage, ["blueprint"] = blueprintId,
            ["nativeComponent"] = typeof(AddEffectFastHealing).FullName,
            ["template"] = "03464790f40c3c24aa684b57155f3280", ["admission"] = application.DeepClone(),
            ["healPerRound"] = 1, ["restored"] = disposed && actor.Damage == originalDamage
        };

        public void Dispose()
        {
            if (disposed) return;
            // A native rule can catch an activation exception after adding a
            // partial fact. Remove every exact fixture fact even in that case.
            foreach (var owned in actor.Buffs.Enumerable.Where(item => item.Blueprint == blueprint).ToArray()) owned.Remove();
            buff = null;
            actor.Descriptor.Damage = originalDamage;
            UnityEngine.Object.Destroy(blueprint);
            UnityEngine.Object.Destroy(component);
            disposed = true;
        }

        public void OnEventAboutToTrigger(RuleApplyBuff evt)
        {
            if (evt.Blueprint == blueprint) application["before"] = RuleState(evt);
        }
        public void OnEventDidTrigger(RuleApplyBuff evt)
        {
            if (evt.Blueprint == blueprint) application["after"] = RuleState(evt);
        }
        private static JObject RuleState(RuleApplyBuff evt) => new JObject {
            ["canApply"] = evt.CanApply, ["immunity"] = evt.Immunity,
            ["storyModeImmunity"] = evt.StoryModeImmunity, ["applied"] = evt.AppliedBuff != null
        };
    }
}
