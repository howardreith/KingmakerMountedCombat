using System;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>
    /// PAL-01's numbers, from the Core Rulebook's Divine Bond with the adaptations in docs/SPRINT8-PALADIN-MOUNT.md.
    /// Paladin level is the effective druid level, one companion rank per level from the bond's level on.
    /// </summary>
    internal static class DivineMountPolicy
    {
        internal const int BondLevel = 5, MaximumClassLevel = 20;
        internal const int IntelligenceBonus = 4;
        internal const int CelestialLevel = 11, GreaterCelestialLevel = 13, SpellResistanceLevel = 15, SpellResistanceBonus = 11;
        internal const int CelestialReduction = 5, GreaterCelestialReduction = 10, CelestialResistance = 10, GreaterCelestialResistance = 15;
        internal const int SmiteUsesPerDay = 1;

        // Uses of the call: one at the bond's level, one more every four levels, four at 17th.
        internal static int CallUses(int paladinLevel) =>
            paladinLevel < BondLevel ? 0 : 1 + (Math.Min(paladinLevel, 17) - BondLevel) / 4;

        internal static bool GrantsAdditionalCall(int paladinLevel) =>
            paladinLevel > BondLevel && CallUses(paladinLevel) > CallUses(paladinLevel - 1);

        internal static int SpellResistance(int paladinLevel) =>
            paladinLevel < SpellResistanceLevel ? 0 : paladinLevel + SpellResistanceBonus;

        // The choice's ranks, then one from the Paladin-only progression at each later level.
        internal static int ExpectedRankFromPaladin(int paladinLevel) => paladinLevel < BondLevel ? 0 : paladinLevel;

        // The class's later weapon-bond entries reach only a character without the mount bond.
        internal static bool KeepsLevelEntryFeature(bool isWeaponBondUpgrade, bool hasDivineMount) =>
            !isWeaponBondUpgrade || !hasDivineMount;

        // A class-levelled character outside the party has no one to make the choice, and keeps the stock bond.
        internal static bool SubstitutesStockBond(bool isPlayerFaction) => !isPlayerFaction;
    }
}
