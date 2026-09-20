using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class NativeSingleAttackWeaponSelection
    {
        public NativeSingleAttackWeaponSelection(
            NativeSingleAttackSlotKind kind,
            int additionalLimbIndex,
            int primaryMainAttacks,
            int secondaryMainAttacks,
            WeaponSlot slot,
            ItemEntityWeapon weapon)
        {
            Kind = kind;
            AdditionalLimbIndex = additionalLimbIndex;
            PrimaryMainAttacks = primaryMainAttacks;
            SecondaryMainAttacks = secondaryMainAttacks;
            Slot = slot;
            Weapon = weapon;
        }

        public NativeSingleAttackSlotKind Kind { get; }

        public int AdditionalLimbIndex { get; }

        public int PrimaryMainAttacks { get; }

        public int SecondaryMainAttacks { get; }

        public WeaponSlot Slot { get; }

        public ItemEntityWeapon Weapon { get; }
    }

    internal static class NativeSingleAttackWeaponResolver
    {
        public static NativeSingleAttackWeaponSelection Resolve(UnitEntityData unit)
        {
            var body = unit?.Body;
            if (body == null)
            {
                return null;
            }

            var primaryMainAttacks = 0;
            var secondaryMainAttacks = 0;
            if (body.HandsAreEnabled)
            {
                var attacksCount = Rulebook.Trigger(new RuleCalculateAttacksCount(unit));
                primaryMainAttacks = attacksCount.PrimaryHand.MainAttacks;
                secondaryMainAttacks = attacksCount.SecondaryHand.MainAttacks;
            }

            var additionalLimbHasWeapon = body.AdditionalLimbs
                .Select(limb => limb != null && limb.HasWeapon)
                .ToArray();
            var decision = NativeSingleAttackSlotPolicy.Select(
                body.HandsAreEnabled,
                body.PrimaryHand != null && body.PrimaryHand.HasWeapon,
                primaryMainAttacks,
                body.SecondaryHand != null && body.SecondaryHand.HasWeapon,
                secondaryMainAttacks,
                additionalLimbHasWeapon);
            if (!decision.HasSelection)
            {
                return null;
            }

            WeaponSlot slot;
            switch (decision.Kind)
            {
                case NativeSingleAttackSlotKind.PrimaryHand:
                    slot = body.PrimaryHand;
                    break;
                case NativeSingleAttackSlotKind.SecondaryHand:
                    slot = body.SecondaryHand;
                    break;
                case NativeSingleAttackSlotKind.AdditionalLimb:
                    slot = body.AdditionalLimbs[decision.AdditionalLimbIndex];
                    break;
                default:
                    return null;
            }

            var weapon = slot?.MaybeWeapon;
            if (weapon == null || !slot.HasWeapon)
            {
                return null;
            }
            return new NativeSingleAttackWeaponSelection(
                decision.Kind,
                decision.AdditionalLimbIndex,
                primaryMainAttacks,
                secondaryMainAttacks,
                slot,
                weapon);
        }
    }
}
