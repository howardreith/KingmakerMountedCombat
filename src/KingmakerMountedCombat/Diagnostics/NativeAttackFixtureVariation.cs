using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Original disposable-actor fixture lease. Changes native facts/stat inputs,
    // never attack plans, cooldowns, command slots or turn preparation.
    internal sealed class NativeAttackFixtureVariation : IDisposable
    {
        private readonly UnitEntityData actor;
        private readonly ActivatableAbility rapid;
        private readonly bool rapidBefore;
        private readonly int babBefore;
        private Buff haste;
        private bool ownsStaggered;
        private bool disposed;

        internal NativeAttackFixtureVariation(UnitEntityData actor, ActivatableAbility rapid,
            bool rapidOn, int? baseAttackBonus, bool hasted)
        {
            this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
            this.rapid = rapid ?? throw new ArgumentNullException(nameof(rapid));
            if (!actor.Commands.Empty || actor.IsInCombat)
                throw new InvalidOperationException("Native stat fixture requires an idle disposable actor outside combat.");
            babBefore = actor.Stats.BaseAttackBonus.BaseValue;
            rapidBefore = rapid.IsOn;
            try
            {
                rapid.IsOn = rapidOn;
                if (baseAttackBonus.HasValue) actor.Stats.BaseAttackBonus.BaseValue = baseAttackBonus.Value;
                if (hasted)
                {
                    var candidates = ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Values
                        .OfType<BlueprintBuff>().Where(item => item.name == "Haste").ToArray();
                    if (candidates.Length != 1 || actor.Descriptor.HasFact(candidates[0]))
                        throw new InvalidOperationException("Expected one initially unowned native Haste buff.");
                    haste = actor.Buffs.AddBuff(candidates[0], actor, TimeSpan.FromMinutes(10), null);
                    if (haste == null || !actor.Descriptor.HasFact(candidates[0]))
                        throw new InvalidOperationException("Native Haste fixture application failed.");
                }
            }
            catch { Dispose(); throw; }
        }

        internal void ApplyStaleRestriction()
        {
            if (ownsStaggered || actor.Descriptor.State.HasCondition(UnitCondition.Staggered) ||
                actor.Descriptor.State.HasConditionImmunity(UnitCondition.Staggered))
                throw new InvalidOperationException("Stale-condition fixture cannot overwrite an existing condition or immunity.");
            actor.Descriptor.State.AddCondition(UnitCondition.Staggered);
            ownsStaggered = true;
            if (!actor.Descriptor.State.HasCondition(UnitCondition.Staggered))
                throw new InvalidOperationException("Native Staggered application failed.");
        }

        internal JObject Capture() => new JObject {
            ["actor"] = actor.UniqueId, ["babBase"] = actor.Stats.BaseAttackBonus.BaseValue,
            ["babModified"] = actor.Stats.BaseAttackBonus.ModifiedValue,
            ["rapidShot"] = rapid.IsOn, ["haste"] = haste?.Blueprint.AssetGuid,
            ["staggered"] = actor.Descriptor.State.HasCondition(UnitCondition.Staggered),
            ["weapon"] = actor.GetFirstWeapon()?.Blueprint.AssetGuid
        };

        public void Dispose()
        {
            if (disposed) return;
            if (ownsStaggered) { actor.Descriptor.State.RemoveCondition(UnitCondition.Staggered); ownsStaggered = false; }
            if (haste != null) { haste.Remove(); haste = null; }
            actor.Stats.BaseAttackBonus.BaseValue = babBefore;
            rapid.IsOn = rapidBefore;
            disposed = true;
        }
    }
}
