using System;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Disposable fixture only. The existing native fast-healing component
    // supplies an observable per-round effect through the real fact pipeline.
    internal sealed class NativeAllocationRoundFactLease : IDisposable
    {
        private readonly UnitEntityData actor;
        private readonly int originalDamage;
        private readonly BlueprintBuff blueprint;
        private readonly AddEffectFastHealing component;
        private readonly string blueprintId;
        private Buff buff;
        private bool disposed;

        internal NativeAllocationRoundFactLease(UnitEntityData actor)
        {
            if (actor == null || actor.IsInCombat || !actor.Descriptor.State.IsConscious ||
                actor.Stats.HitPoints.ModifiedValue - actor.Damage <= 6)
                throw new InvalidOperationException("Native round fact requires a healthy disposable actor before combat.");
            this.actor = actor;
            originalDamage = actor.Damage;
            blueprint = ScriptableObject.CreateInstance<BlueprintBuff>();
            component = ScriptableObject.CreateInstance<AddEffectFastHealing>();
            blueprint.name = "KMC_DiagnosticAllocationRoundFact";
            blueprintId = Guid.NewGuid().ToString("N");
            blueprint.AssetGuid = blueprintId;
            component.Heal = 1;
            blueprint.ComponentsArray = new BlueprintComponent[] { component };
            try
            {
                actor.Descriptor.Damage = originalDamage + 6;
                buff = actor.Buffs.AddBuff(blueprint, actor, TimeSpan.FromMinutes(10), null);
                if (buff == null) throw new InvalidOperationException("Native round fact did not activate.");
            }
            catch { Dispose(); throw; }
        }

        internal JObject Capture() => new JObject {
            ["actor"] = actor.UniqueId, ["originalDamage"] = originalDamage,
            ["currentDamage"] = actor.Damage, ["blueprint"] = blueprintId,
            ["nativeComponent"] = typeof(AddEffectFastHealing).FullName,
            ["healPerRound"] = 1, ["restored"] = disposed && actor.Damage == originalDamage
        };

        public void Dispose()
        {
            if (disposed) return;
            buff?.Remove();
            buff = null;
            actor.Descriptor.Damage = originalDamage;
            UnityEngine.Object.Destroy(blueprint);
            UnityEngine.Object.Destroy(component);
            disposed = true;
        }
    }
}
