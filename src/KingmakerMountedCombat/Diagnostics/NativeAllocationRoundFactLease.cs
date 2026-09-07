using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Native Prepare processes Unit.Logic facts. Buffs have a separate timed
    // controller, so a healing buff cannot prove preparation fact delivery.
    internal sealed class NativeAllocationRoundFactLease : IDisposable
    {
        internal static string ComponentType => typeof(AddFactContextActions).FullName;
        private readonly UnitEntityData actor;
        private readonly int originalDamage;
        private readonly BlueprintFeature blueprint;
        private readonly AddFactContextActions component;
        private readonly ContextActionHealTarget heal;
        private readonly string blueprintId;
        private readonly string templateId;
        private bool disposed;

        internal NativeAllocationRoundFactLease(UnitEntityData actor)
        {
            if (actor == null || actor.IsInCombat || !actor.Descriptor.State.IsConscious ||
                actor.Stats.HitPoints.ModifiedValue - actor.Damage <= 6)
                throw new InvalidOperationException("Native round fact requires a healthy disposable actor before combat.");
            this.actor = actor;
            originalDamage = actor.Damage;
            var template = actor.Logic.Enumerable.Select(fact => fact.Blueprint).OfType<BlueprintFeature>().FirstOrDefault();
            if (template == null) throw new InvalidOperationException("Disposable actor lacks a native feature fixture template.");
            templateId = template.AssetGuid;
            blueprint = UnityEngine.Object.Instantiate(template);
            component = ScriptableObject.CreateInstance<AddFactContextActions>();
            heal = ScriptableObject.CreateInstance<ContextActionHealTarget>();
            blueprint.name = "KMC_DiagnosticAllocationRoundFact";
            blueprintId = Guid.NewGuid().ToString("N");
            blueprint.AssetGuid = blueprintId;
            component.name = "KMC_DiagnosticNativeRoundActions";
            heal.name = "KMC_DiagnosticNativeRoundHealing";
            heal.Value = new ContextDiceValue { DiceType = DiceType.Zero, DiceCountValue = 0, BonusValue = 1 };
            component.Activated = new ActionList { Actions = new GameAction[0] };
            component.Deactivated = new ActionList { Actions = new GameAction[0] };
            component.NewRound = new ActionList { Actions = new GameAction[] { heal } };
            blueprint.ComponentsArray = new BlueprintComponent[] { component };
            try
            {
                actor.Descriptor.Damage = originalDamage + 6;
                var context = new MechanicsContext(actor, actor.Descriptor, blueprint);
                if (actor.Logic.AddFact(blueprint, context) == null || !actor.Logic.HasFact(blueprint))
                    throw new InvalidOperationException("Native preparation feature did not activate for " + actor.UniqueId);
            }
            catch { Dispose(); throw; }
        }

        internal JObject Capture() => new JObject {
            ["actor"] = actor.UniqueId, ["originalDamage"] = originalDamage,
            ["currentDamage"] = actor.Damage, ["blueprint"] = blueprintId, ["template"] = templateId,
            ["nativeComponent"] = ComponentType, ["nativeEffect"] = typeof(ContextActionHealTarget).FullName,
            ["nativeCollection"] = "Unit.Logic", ["active"] = actor.Logic.HasFact(blueprint),
            ["healPerRound"] = 1, ["restored"] = disposed && actor.Damage == originalDamage && !actor.Logic.HasFact(blueprint)
        };

        public void Dispose()
        {
            if (disposed) return;
            foreach (var owned in actor.Logic.Enumerable.Where(item => item.Blueprint == blueprint).ToArray()) actor.Logic.RemoveFact(owned);
            actor.Descriptor.Damage = originalDamage;
            UnityEngine.Object.Destroy(blueprint);
            UnityEngine.Object.Destroy(component);
            UnityEngine.Object.Destroy(heal);
            disposed = true;
        }
    }
}
