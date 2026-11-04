using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;
using UnityEngine;
using Kingmaker.Blueprints.Classes.Selection;

namespace KingmakerMountedCombat.Integration
{
    // The stock AddFeatureToCompanion writes through a level-up preview to the real pet, and a cancelled level-up leaves
    // that write behind. A preview has no holding state (the test stock AddPet uses to not spawn from one). It also misses
    // a pet that spawns after the owner's fact (a hired owner's pet arrives when the owner enters the world), so the live
    // companion sync calls TryApply when an owner's pet changes.
    internal sealed class DivineMountPetBenefit : OwnedGameLogicComponent<UnitDescriptor>
    {
        public BlueprintFeature Feature;
        public BlueprintFeature Replaces;

        public override void OnFactActivate() => TryApply();

        public override void OnTurnOn()
        {
            base.OnTurnOn();
            TryApply();
        }

        public override void OnFactDeactivate()
        {
            var features = RealPetFeatures();
            if (features != null && features.HasFact(Feature)) features.RemoveFact(Feature);
        }

        internal void TryApply()
        {
            var features = RealPetFeatures();
            if (features == null) return;
            if (Replaces != null && features.HasFact(Replaces)) features.RemoveFact(Replaces);
            if (!features.HasFact(Feature)) features.AddFeature(Feature);
        }

        private FeatureCollection RealPetFeatures() =>
            Owner.Unit?.HoldingState == null ? null : Owner.Pet?.Descriptor?.Progression?.Features;
    }

    // Spell resistance follows the master's current paladin level, so it cannot be a fixed native value.
    internal sealed class DivineMountSpellResistance : OwnedGameLogicComponent<UnitDescriptor>, IUnitGainLevelHandler, IGlobalSubscriber
    {
        public BlueprintCharacterClass PaladinClass;
        [JsonProperty]
        private int? appliedId;

        public override void OnTurnOn()
        {
            base.OnTurnOn();
            Refresh();
        }

        public override void OnTurnOff()
        {
            base.OnTurnOff();
            Clear();
        }

        public void HandleUnitGainLevel(UnitDescriptor unit, BlueprintCharacterClass @class)
        {
            if (ReferenceEquals(unit, Owner.Master.Value?.Descriptor)) Refresh();
        }

        private void Refresh()
        {
            Clear();
            var value = DivineMountPolicy.SpellResistance(Owner.Master.Value?.Descriptor?.Progression?.GetClassLevel(PaladinClass) ?? 0);
            if (value > 0) appliedId = Owner.Ensure<UnitPartSpellResistance>().AddResistance(value, null, null);
        }

        private void Clear()
        {
            if (!appliedId.HasValue) return;
            Owner.Get<UnitPartSpellResistance>()?.Remove(appliedId.Value);
            appliedId = null;
        }
    }

    internal sealed class DivineMountRiderSizePrerequisite : Prerequisite
    {
        public override bool Check(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state) =>
            unit.OriginalSize <= Size.Small;

        public override string GetUIText() => "Small rider";
    }

    // The celestial template's smite: Charisma to attack and Hit Dice to damage against the marked evil target.
    internal sealed class DivineMountSmiteBonus : RuleInitiatorLogicComponent<RuleAttackRoll>, IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
        public BlueprintBuff Mark;

        public override void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (evt.Weapon == null || !evt.Target.Descriptor.HasFact(Mark)) return;
            evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus.AddModifier(Math.Max(0, Owner.Stats.Charisma.Bonus), this, ModifierDescriptor.UntypedStackable));
        }

        public override void OnEventDidTrigger(RuleAttackRoll evt) { }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.Weapon == null || !evt.Target.Descriptor.HasFact(Mark)) return;
            evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalDamage.AddModifier(Owner.Progression.CharacterLevel, this, ModifierDescriptor.UntypedStackable));
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt) { }
    }

    internal sealed class DivineMountSmiteLogic : AbilityCustomLogic, IAbilityTargetChecker
    {
        public BlueprintBuff Mark;

        public bool CanTarget(UnitEntityData caster, TargetWrapper target)
        {
            var unit = target?.Unit;
            return unit != null && caster != null && unit != caster && unit.Descriptor.State.IsConscious &&
                unit.Descriptor.Alignment.Value.HasComponent(AlignmentComponent.Evil) && !unit.Descriptor.HasFact(Mark);
        }

        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            var unit = target?.Unit;
            if (unit == null) yield break;
            unit.Descriptor.AddBuff(Mark, context, TimeSpan.FromDays(1));
            yield return new AbilityDeliveryTarget(target);
        }

        public override void Cleanup(AbilityExecutionContext context) { }
    }

    internal sealed class DivineMountCallLogic : AbilityCustomLogic, IAbilityAvailabilityProvider
    {
        private string lastReason = "Your Divine Mount cannot answer the call.";

        // Set by the composition root: a mounted pair's two actors move as the relationship says, never by a teleport.
        internal static Func<UnitEntityData, bool> IsMountedPairMember;

        public bool IsAvailableFor(AbilityData ability)
        {
            var caster = ability?.Caster?.Unit;
            var pet = ability?.Caster?.Pet;
            lastReason = pet == null ? "You have no mount to call." :
                !pet.IsInGame || !pet.Descriptor.State.CanAct ? "Your mount cannot answer the call." :
                IsMountedPairMember == null || IsMountedPairMember(caster) || IsMountedPairMember(pet) ? "Your mount is already with you." : null;
            return lastReason == null;
        }

        public string GetReason() => lastReason ?? string.Empty;

        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            var caster = context?.Caster;
            var pet = caster?.Descriptor.Pet;
            if (pet == null || IsMountedPairMember == null || IsMountedPairMember(caster) || IsMountedPairMember(pet)) yield break;
            FreePlaceSelector.PlaceSpawnPlaces(1, 0.5f, caster.Position);
            pet.Commands.InterruptAll();
            pet.Translocate(FreePlaceSelector.GetRelaxedPosition(0, projectOnGround: true), caster.Orientation);
            yield return new AbilityDeliveryTarget(new TargetWrapper(pet));
        }

        public override void Cleanup(AbilityExecutionContext context) { }
    }
}
