using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // The selectable-feat facts of one BlueprintFeature, read once at registration. Plain values, so the contract is
    // checked and tested without native types.
    public sealed class KmcFeatContractFacts
    {
        public string[] Groups = new string[0];
        public int Ranks;
        public bool IsClassFeature;
        public bool HasIcon;
        // Reference-equal to the sprite KMC created for the feat.
        public bool IconIsKmc;
        public bool HideInUI;
        public bool HideInCharacterSheetAndLevelUp;
        public bool HideNotAvailibleInUI;

        public string Describe() =>
            "groups=[" + string.Join(",", Groups ?? new string[0]) + "] ranks=" + Ranks + " IsClassFeature=" + IsClassFeature +
            " icon=" + (!HasIcon ? "absent" : IconIsKmc ? "KMC" : "other") +
            " HideInUI=" + HideInUI + " HideInCharacterSheetAndLevelUp=" + HideInCharacterSheetAndLevelUp + " HideNotAvailibleInUI=" + HideNotAvailibleInUI;
    }

    // Mounted Archery's registration contract: a stock-shaped combat feat with the Feat and CombatFeat groups, one rank,
    // KMC's own icon, and the IsClassFeature value of the stock combat feat it is built from (Power Attack).
    //
    // Kingmaker 2.1.7b's IsClassFeature does not mean "granted by a class". Every stock feat holds true (Power Attack,
    // Dodge, Mobility, Point Blank Shot, Precise Shot, Rapid Shot, Deadly Aim, Combat Reflexes, Toughness, Iron Will),
    // and so do class features. Its only native readers are in Player.RespecCompanion, where true means "rebuilt by the
    // respec, not carried over as an extra fact". KMC's Mounted Combat already holds the copied value. Release 0.2.9
    // required false, which rejected the inherited stock value on every installation and removed Mounted Archery.
    public static class MountedArcheryFeatContract
    {
        public const string FeatGroup = "Feat";
        public const string CombatFeatGroup = "CombatFeat";
        public const string FailurePrefix = "Mounted Archery did not retain the single-rank combat-feat contract";

        // Null when the feature holds the contract. Otherwise every failed invariant with its observed and expected value,
        // then both fact sets, so one registration log line identifies the cause.
        public static string Violation(KmcFeatContractFacts feature, KmcFeatContractFacts donor)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            if (donor == null) throw new ArgumentNullException(nameof(donor));
            var failed = new List<string>();
            if (!Holds(feature.Groups, FeatGroup)) failed.Add("groups lack " + FeatGroup);
            if (!Holds(feature.Groups, CombatFeatGroup)) failed.Add("groups lack " + CombatFeatGroup);
            if (feature.Ranks != 1) failed.Add("ranks=" + feature.Ranks + " (expected 1)");
            if (feature.IsClassFeature != donor.IsClassFeature)
                failed.Add("IsClassFeature=" + feature.IsClassFeature + " (expected the stock donor's " + donor.IsClassFeature + ")");
            if (!feature.HasIcon) failed.Add("icon absent (expected KMC's Mounted Archery icon)");
            else if (!feature.IconIsKmc) failed.Add("icon is not KMC's Mounted Archery icon");
            if (failed.Count == 0) return null;
            return FailurePrefix + ": " + string.Join("; ", failed.ToArray()) + ". Feature " + feature.Describe() +
                "; donor PowerAttackFeature " + donor.Describe() + ".";
        }

        private static bool Holds(string[] groups, string group) => groups != null && groups.Contains(group, StringComparer.Ordinal);
    }
}
