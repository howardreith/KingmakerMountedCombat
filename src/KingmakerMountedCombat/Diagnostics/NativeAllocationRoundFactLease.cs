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
        private readonly AddFactContextActions activeComponent;
        private readonly ContextActionHealTarget heal;
        private readonly string blueprintId;
        private readonly string templateId;
        private bool disposed;
        internal UnitEntityData Actor => actor;
        internal void SetDiagnosticRoundAction(GameAction action)
        {
            if (disposed) throw new ObjectDisposedException(nameof(NativeAllocationRoundFactLease));
            if (activeComponent == null || !actor.Logic.Enumerable.Where(item => item.Blueprint == blueprint)
                .SelectMany(item => item.Components).Contains(activeComponent))
                throw new InvalidOperationException("Native round stimulus lost its exact active fact component.");
            // Fact activation clones its GameLogicComponent. Updating the
            // blueprint after activation does not update that actor instance.
            activeComponent.NewRound = new ActionList { Actions = action == null ? new GameAction[] { heal } : new GameAction[] { heal, action } };
        }

        internal JObject CaptureDiagnosticBinding(GameAction action) => new JObject {
            ["actor"] = actor.UniqueId, ["blueprint"] = blueprintId,
            ["activeComponent"] = activeComponent.GetInstanceID(), ["templateComponent"] = component.GetInstanceID(),
            ["activeFactCount"] = actor.Logic.Enumerable.Count(item => item.Blueprint == blueprint),
            ["actionCount"] = activeComponent.NewRound.Actions.Length,
            ["exactActionBound"] = activeComponent.NewRound.Actions.Count(item => ReferenceEquals(item, action)) == 1
        };

        internal NativeAllocationRoundFactLease(UnitEntityData actor)
        {
            if (actor == null || actor.IsInCombat || !actor.Descriptor.State.IsConscious ||
                actor.Stats.HitPoints.ModifiedValue - actor.Damage <= 6)
                throw new InvalidOperationException("Native round fact requires a healthy disposable actor before combat.");
            this.actor = actor;
            originalDamage = actor.Damage;
            // Progression features are not stored in Unit.Logic. Resolve the
            // already-qualified native template from the loaded library; only
            // our cloned, replacement component is added to the measured actor.
            var templates = ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Values.OfType<BlueprintFeature>()
                .Where(item => item.name == "RapidShot" || item.Name == "Rapid Shot").ToArray();
            if (templates.Length != 1) throw new InvalidOperationException("Expected one native round fact template; found " + templates.Length + ".");
            var template = templates[0];
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
                var fact = actor.Logic.AddFact(blueprint, context);
                if (fact == null || !actor.Logic.HasFact(blueprint))
                    throw new InvalidOperationException("Native preparation feature did not activate for " + actor.UniqueId);
                activeComponent = fact.Components.OfType<AddFactContextActions>().Single();
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
