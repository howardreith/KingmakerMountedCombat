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
        // Set on the celestial link: Feature is then the lowest tier, and the pet's actual Hit Dice choose among the three.
        public BlueprintFeature StandardTier, GreaterTier;

        public override void OnFactActivate() => TryApply();

        public override void OnTurnOn()
        {
            base.OnTurnOn();
            TryApply();
        }

        public override void OnFactDeactivate()
        {
            var features = RealPetFeatures();
            if (features == null) return;
            foreach (var feature in new[] { Feature, StandardTier, GreaterTier })
                if (feature != null && features.HasFact(feature)) features.RemoveFact(feature);
        }

        internal void TryApply()
        {
            var features = RealPetFeatures();
            if (features == null) return;
            var wanted = Wanted(Owner.Pet.Descriptor.Progression.CharacterLevel);
            foreach (var feature in new[] { Feature, StandardTier, GreaterTier })
                if (feature != null && !ReferenceEquals(feature, wanted) && features.HasFact(feature)) features.RemoveFact(feature);
            if (!features.HasFact(wanted)) features.AddFeature(wanted);
        }

        private BlueprintFeature Wanted(int hitDice)
        {
            if (GreaterTier == null) return Feature;
            switch (DivineMountPolicy.CelestialTier(hitDice))
            {
                case DivineMountCelestialTier.Greater: return GreaterTier;
                case DivineMountCelestialTier.Standard: return StandardTier;
                default: return Feature;
            }
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

    // The celestial template's smite: Charisma to attack and Hit Dice to damage against this mount's own marked evil target.
    // A mark's source is its buff's native context; the current target travels with this fact.
    internal sealed class DivineMountSmiteBonus : RuleInitiatorLogicComponent<RuleAttackRoll>, IInitiatorRulebookHandler<RuleAttackWithWeapon>
    {
        public BlueprintBuff Mark;

        [JsonProperty]
        private UnitReference currentTarget;

        internal static IEnumerable<UnitEntityData> MarkSources(UnitEntityData target, BlueprintBuff mark) =>
            target.Descriptor.Buffs.Enumerable.Where(buff => ReferenceEquals(buff.Blueprint, mark)).Select(buff => buff.MaybeContext?.MaybeCaster);

        private bool Applies(UnitEntityData target) => target != null &&
            DivineMountPolicy.SmiteApplies(MarkSources(target, Mark), Owner.Unit, currentTarget.Value, target);

        internal bool MayMark(UnitEntityData target) =>
            DivineMountPolicy.MayMark(MarkSources(target, Mark), Owner.Unit, currentTarget.Value, target);

        // Only this mount's own relation ends. Another mount's mark on the same target is its own buff.
        private void Retire(UnitEntityData target)
        {
            if (target == null) return;
            foreach (var buff in target.Descriptor.Buffs.Enumerable.Where(value => ReferenceEquals(value.Blueprint, Mark) && ReferenceEquals(value.MaybeContext?.MaybeCaster, Owner.Unit)).ToArray())
                buff.Remove();
        }

        internal void BeginMark(UnitEntityData target, Kingmaker.UnitLogic.Mechanics.MechanicsContext context)
        {
            Retire(currentTarget.Value);
            Retire(target);
            currentTarget = target.Descriptor.AddBuff(Mark, context, TimeSpan.FromDays(1)) != null ? target : null;
        }

        public override void OnFactDeactivate()
        {
            base.OnFactDeactivate();
            if (Owner.Unit?.HoldingState == null) return;
            Retire(currentTarget.Value);
            currentTarget = null;
        }

        public override void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (evt.Weapon == null || !Applies(evt.Target)) return;
            evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalAttackBonus.AddModifier(Math.Max(0, Owner.Stats.Charisma.Bonus), this, ModifierDescriptor.UntypedStackable));
        }

        public override void OnEventDidTrigger(RuleAttackRoll evt) { }

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            if (evt.Weapon == null || !Applies(evt.Target)) return;
            evt.AddTemporaryModifier(evt.Initiator.Stats.AdditionalDamage.AddModifier(Owner.Progression.CharacterLevel, this, ModifierDescriptor.UntypedStackable));
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt) { }
    }

    internal sealed class DivineMountSmiteLogic : AbilityCustomLogic, IAbilityTargetChecker
    {
        private static DivineMountSmiteBonus BonusOf(UnitEntityData caster) =>
            caster?.Descriptor?.Progression?.Features.SelectFactComponents<DivineMountSmiteBonus>().FirstOrDefault();

        public bool CanTarget(UnitEntityData caster, TargetWrapper target)
        {
            var unit = target?.Unit;
            var bonus = BonusOf(caster);
            return unit != null && bonus != null && unit != caster && unit.Descriptor.State.IsConscious &&
                unit.Descriptor.Alignment.Value.HasComponent(AlignmentComponent.Evil) && bonus.MayMark(unit);
        }

        public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
        {
            var unit = target?.Unit;
            var bonus = BonusOf(context?.Caster);
            if (unit == null || bonus == null) yield break;
            bonus.BeginMark(unit, context);
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
