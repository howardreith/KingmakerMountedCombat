using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class DivineMountPolicyTests
    {
        internal static void MountedSmiteWorksFromWhereTheMountStands()
        {
            TestRunner.Equal(false, DivineMountPolicy.MountedSmiteNeedsApproach(false, 900f, 10f, true), "An unmounted Smite keeps its native approach.");
            TestRunner.Equal(true, DivineMountPolicy.MountedSmiteNeedsApproach(true, 101f, 10f, false), "A mounted Smite beyond range needs an approach.");
            TestRunner.Equal(false, DivineMountPolicy.MountedSmiteNeedsApproach(true, 100f, 10f, false), "A mounted Smite at exactly its range is in place.");
            TestRunner.Equal(true, DivineMountPolicy.MountedSmiteNeedsApproach(true, 1f, 10f, true), "A mounted Smite with no line of sight needs an approach.");
            TestRunner.Equal(false, DivineMountPolicy.MountedSmiteNeedsApproach(true, 1f, 10f, false), "A mounted Smite in reach and in sight is in place.");
        }

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
        }

        internal static void TheMountPathKeepsNoWeaponUpgrade()
        {
            TestRunner.True(!DivineMountPolicy.KeepsLevelEntryFeature(isWeaponBondUpgrade: true, hasDivineMount: true), "Divine Mount decision changed.");
            TestRunner.True(DivineMountPolicy.KeepsLevelEntryFeature(isWeaponBondUpgrade: true, hasDivineMount: false), "Divine Mount decision changed.");
            TestRunner.True(DivineMountPolicy.KeepsLevelEntryFeature(isWeaponBondUpgrade: false, hasDivineMount: true), "Divine Mount decision changed.");
        }

        // The Bestiary's table, read at its boundaries from the creature's own Hit Dice.
        internal static void CelestialTierFollowsActualHitDice()
        {
            TestRunner.Equal((int)DivineMountCelestialTier.Lesser, (int)DivineMountPolicy.CelestialTier(1), "HD 1 tier changed.");
            TestRunner.Equal((int)DivineMountCelestialTier.Lesser, (int)DivineMountPolicy.CelestialTier(4), "HD 4 tier changed.");
            TestRunner.Equal((int)DivineMountCelestialTier.Standard, (int)DivineMountPolicy.CelestialTier(5), "HD 5 tier changed.");
            TestRunner.Equal((int)DivineMountCelestialTier.Standard, (int)DivineMountPolicy.CelestialTier(10), "HD 10 tier changed.");
            TestRunner.Equal((int)DivineMountCelestialTier.Greater, (int)DivineMountPolicy.CelestialTier(11), "HD 11 tier changed.");
        }

        private sealed class Unit { }

        // The decision takes the mark's source from its buff, so it is tested with distinct sources.
        internal static void SmiteBelongsToTheMountThatPlacedIt()
        {
            Unit a = new Unit(), b = new Unit(), x = new Unit(), y = new Unit();
            var byA = new[] { a };
            var byBoth = new[] { a, b };
            var none = new Unit[0];
            TestRunner.True(DivineMountPolicy.SmiteApplies(byA, a, x, x), "A's own mark on its current target lost its bonus.");
            TestRunner.True(!DivineMountPolicy.SmiteApplies(byA, b, null, x), "B took a bonus from A's mark.");
            TestRunner.True(DivineMountPolicy.MayMark(byA, b, null, x), "A's mark blocked B.");
            TestRunner.True(!DivineMountPolicy.MayMark(byA, a, x, x), "A could mark its own current target again.");
            TestRunner.True(DivineMountPolicy.SmiteApplies(byBoth, a, x, x) && DivineMountPolicy.SmiteApplies(byBoth, b, x, x), "Two marks on one target did not stay independent.");
            TestRunner.True(!DivineMountPolicy.SmiteApplies(byBoth, a, y, x), "A kept a bonus on a former target.");
            TestRunner.True(DivineMountPolicy.SmiteApplies(byBoth, b, x, x), "A changing target took B's bonus.");
            TestRunner.True(!DivineMountPolicy.SmiteApplies(none, a, x, x), "An expired mark kept its bonus.");
            TestRunner.True(DivineMountPolicy.MayMark(none, a, x, x), "An expired mark blocked a new one.");
        }

        internal static void OnlyACharacterOutsideThePartyTakesTheStockBond()
        {
            TestRunner.True(DivineMountPolicy.SubstitutesStockBond(isPlayerFaction: false), "Divine Mount decision changed.");
            TestRunner.True(!DivineMountPolicy.SubstitutesStockBond(isPlayerFaction: true), "Divine Mount decision changed.");
        }
    }
}
