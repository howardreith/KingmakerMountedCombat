using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Mounted Archery's registration contract (the 0.2.9 player report: "Mounted Archery did not retain the single-rank
    // combat-feat contract", feat absent from every list). The donor facts are the stock Kingmaker 2.1.7b blueprints as
    // serialized in the installed sharedassets1.assets (BlueprintFeature layout from the installed Assembly-CSharp:
    // Groups, Ranks, ReapplyOnLevelUp, IsClassFeature; each bool is 4-byte aligned). scripts/Test-MountedArcheryFeatContract.ps1
    // decodes them from the installed file and runs the production construction and validation on genuine blueprints.
    internal static class MountedArcheryFeatContractTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("mounted archery contract: built from stock 2.1.7b Power Attack (IsClassFeature=true) it registers; the 0.2.9 rule rejected it", StockPowerAttackDonor);
            runner.Run("mounted archery contract: every stock combat feat holds IsClassFeature=true, and the feat follows its donor's value", DonorClassFlagFollowed);
            runner.Run("mounted archery contract: each failed invariant is named with its observed and expected values", InvariantDiagnostics);
            runner.Run("mounted archery contract: several failures are all reported, with both fact sets", CombinedDiagnostics);
        }

        // PowerAttackFeature 9972f33f977fc724c838e59641b2fca5 in the installed 2.1.7b data: Groups [CombatFeat, Feat], Ranks 1,
        // ReapplyOnLevelUp false, IsClassFeature true, all Hide flags false, a stock icon.
        private static KmcFeatContractFacts StockPowerAttack() => new KmcFeatContractFacts
        {
            Groups = new[] { "CombatFeat", "Feat" }, Ranks = 1, IsClassFeature = true, HasIcon = true, IconIsKmc = false
        };

        // What production registration produces from a donor: CopyFeatureContract (groups and flags copied, one rank) and
        // SetFeaturePresentation (KMC's icon).
        private static KmcFeatContractFacts Built(KmcFeatContractFacts donor) => new KmcFeatContractFacts
        {
            Groups = (string[])donor.Groups.Clone(), Ranks = 1, IsClassFeature = donor.IsClassFeature, HasIcon = true, IconIsKmc = true,
            HideInUI = donor.HideInUI, HideInCharacterSheetAndLevelUp = donor.HideInCharacterSheetAndLevelUp, HideNotAvailibleInUI = donor.HideNotAvailibleInUI
        };

        // The released 0.2.9 condition, kept to show the reproduced failure.
        private static bool Release029Rejects(KmcFeatContractFacts feature) =>
            Array.IndexOf(feature.Groups, "Feat") < 0 || Array.IndexOf(feature.Groups, "CombatFeat") < 0 || feature.Ranks != 1 || feature.IsClassFeature || !feature.IconIsKmc;

        private static void StockPowerAttackDonor()
        {
            var donor = StockPowerAttack();
            var feature = Built(donor);
            TestRunner.True(Release029Rejects(feature), "The released rule rejects the feat built from the stock donor (the reported exception).");
            TestRunner.Equal<string>(null, MountedArcheryFeatContract.Violation(feature, donor), "The corrected contract accepts the stock-built feat.");
            TestRunner.True(feature.IsClassFeature && feature.Ranks == 1 && Array.IndexOf(feature.Groups, "Feat") >= 0 &&
                Array.IndexOf(feature.Groups, "CombatFeat") >= 0 && feature.IconIsKmc && !feature.HideInUI && !feature.HideInCharacterSheetAndLevelUp,
                "The registered feat is a visible single-rank Feat and CombatFeat with KMC's icon and the stock class flag.");
        }

        private static void DonorClassFlagFollowed()
        {
            // Decoded stock values: Dodge, Point Blank Shot, Rapid Shot and Mobility are [CombatFeat, Feat] with IsClassFeature
            // true; Toughness and Iron Will are [Feat] with IsClassFeature true.
            foreach (var groups in new[] { new[] { "CombatFeat", "Feat" }, new[] { "Feat", "CombatFeat" } })
            {
                var donor = new KmcFeatContractFacts { Groups = groups, Ranks = 1, IsClassFeature = true, HasIcon = true };
                TestRunner.Equal<string>(null, MountedArcheryFeatContract.Violation(Built(donor), donor), "A stock combat-feat donor builds a valid feat.");
            }
            // If another mod changed the donor's flag, both KMC feats follow it (Mounted Combat copies the same donor).
            var changed = StockPowerAttack();
            changed.IsClassFeature = false;
            TestRunner.Equal<string>(null, MountedArcheryFeatContract.Violation(Built(changed), changed), "The feat follows its donor's flag.");
            var diverged = Built(StockPowerAttack());
            diverged.IsClassFeature = false;
            var violation = MountedArcheryFeatContract.Violation(diverged, StockPowerAttack());
            TestRunner.True(violation != null && violation.Contains("IsClassFeature=False (expected the stock donor's True)"),
                "A feat whose class flag diverges from the stock donor is refused and both values are named.");
        }

        private static void InvariantDiagnostics()
        {
            var donor = StockPowerAttack();
            Expect(donor, feature => feature.Groups = new[] { "Feat" }, "groups lack CombatFeat");
            Expect(donor, feature => feature.Groups = new[] { "CombatFeat" }, "groups lack Feat");
            Expect(donor, feature => feature.Groups = null, "groups lack Feat; groups lack CombatFeat");
            Expect(donor, feature => feature.Ranks = 2, "ranks=2 (expected 1)");
            Expect(donor, feature => { feature.HasIcon = false; feature.IconIsKmc = false; }, "icon absent (expected KMC's Mounted Archery icon)");
            Expect(donor, feature => feature.IconIsKmc = false, "icon is not KMC's Mounted Archery icon");
            Expect(donor, feature => feature.IsClassFeature = false, "IsClassFeature=False (expected the stock donor's True)");
        }

        private static void Expect(KmcFeatContractFacts donor, Action<KmcFeatContractFacts> change, string expected)
        {
            var feature = Built(donor);
            change(feature);
            var violation = MountedArcheryFeatContract.Violation(feature, donor);
            TestRunner.True(violation != null && violation.StartsWith(MountedArcheryFeatContract.FailurePrefix + ": " + expected + ". Feature ", StringComparison.Ordinal),
                "The failure names exactly: " + expected + " (got: " + (violation ?? "null") + ").");
        }

        private static void CombinedDiagnostics()
        {
            var donor = StockPowerAttack();
            donor.HideNotAvailibleInUI = true;
            var feature = Built(donor);
            feature.Ranks = 3;
            feature.Groups = new[] { "Feat" };
            feature.IconIsKmc = false;
            var violation = MountedArcheryFeatContract.Violation(feature, donor);
            TestRunner.True(violation != null && violation.StartsWith(MountedArcheryFeatContract.FailurePrefix +
                ": groups lack CombatFeat; ranks=3 (expected 1); icon is not KMC's Mounted Archery icon. ", StringComparison.Ordinal),
                "Every failed invariant is listed, in order, and nothing that held.");
            TestRunner.True(violation.Contains("Feature " + feature.Describe() + "; donor PowerAttackFeature " + donor.Describe() + ".") &&
                feature.Describe().Contains("groups=[Feat] ranks=3 IsClassFeature=True icon=other") && feature.Describe().Contains("HideNotAvailibleInUI=True"),
                "The line carries the observed feature and donor facts, visibility flags included.");
            var threw = false;
            try { MountedArcheryFeatContract.Violation(null, donor); } catch (ArgumentNullException) { threw = true; }
            TestRunner.True(threw, "A missing feature is a programming fault, not a contract report.");
        }
    }
}
