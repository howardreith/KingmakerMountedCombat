using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class DivineMountPolicyTests
    {
        internal static void CallUsesFollowTheRule()
        {
            TestRunner.Equal(0, DivineMountPolicy.CallUses(4), "Divine Mount value changed.");
            TestRunner.Equal(1, DivineMountPolicy.CallUses(5), "Divine Mount value changed.");
            TestRunner.Equal(1, DivineMountPolicy.CallUses(8), "Divine Mount value changed.");
            TestRunner.Equal(2, DivineMountPolicy.CallUses(9), "Divine Mount value changed.");
            TestRunner.Equal(3, DivineMountPolicy.CallUses(13), "Divine Mount value changed.");
            TestRunner.Equal(4, DivineMountPolicy.CallUses(17), "Divine Mount value changed.");
            TestRunner.Equal(4, DivineMountPolicy.CallUses(20), "Divine Mount value changed.");
            TestRunner.True(DivineMountPolicy.GrantsAdditionalCall(9), "Divine Mount decision changed.");
            TestRunner.True(!DivineMountPolicy.GrantsAdditionalCall(5), "Divine Mount decision changed.");
            TestRunner.True(!DivineMountPolicy.GrantsAdditionalCall(10), "Divine Mount decision changed.");
        }

        internal static void SpellResistanceStartsAtFifteenAndFollowsTheLevel()
        {
            TestRunner.Equal(0, DivineMountPolicy.SpellResistance(14), "Divine Mount value changed.");
            TestRunner.Equal(26, DivineMountPolicy.SpellResistance(15), "Divine Mount value changed.");
            TestRunner.Equal(31, DivineMountPolicy.SpellResistance(20), "Divine Mount value changed.");
        }

        // Paladin level is the effective druid level, not the Ranger's reduced rate.
        internal static void RankIsThePaladinLevel()
        {
            TestRunner.Equal(0, DivineMountPolicy.ExpectedRankFromPaladin(4), "Divine Mount value changed.");
            TestRunner.Equal(5, DivineMountPolicy.ExpectedRankFromPaladin(5), "Divine Mount value changed.");
            TestRunner.Equal(20, DivineMountPolicy.ExpectedRankFromPaladin(20), "Divine Mount value changed.");
            TestRunner.Equal(5, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(DivineMountPolicy.ExpectedRankFromPaladin(5)), "Divine Mount value changed.");
            // The table reaches 11 Hit Dice at 13th, where the celestial protections step.
            TestRunner.Equal(11, HorseCompanionProgressionPolicy.ExpectedCharacterLevel(DivineMountPolicy.GreaterCelestialLevel), "Divine Mount value changed.");
        }

        internal static void TheMountPathKeepsNoWeaponUpgrade()
        {
            TestRunner.True(!DivineMountPolicy.KeepsLevelEntryFeature(isWeaponBondUpgrade: true, hasDivineMount: true), "Divine Mount decision changed.");
            TestRunner.True(DivineMountPolicy.KeepsLevelEntryFeature(isWeaponBondUpgrade: true, hasDivineMount: false), "Divine Mount decision changed.");
            TestRunner.True(DivineMountPolicy.KeepsLevelEntryFeature(isWeaponBondUpgrade: false, hasDivineMount: true), "Divine Mount decision changed.");
        }

        internal static void OnlyACharacterOutsideThePartyTakesTheStockBond()
        {
            TestRunner.True(DivineMountPolicy.SubstitutesStockBond(isPlayerFaction: false), "Divine Mount decision changed.");
            TestRunner.True(!DivineMountPolicy.SubstitutesStockBond(isPlayerFaction: true), "Divine Mount decision changed.");
        }
    }
}
