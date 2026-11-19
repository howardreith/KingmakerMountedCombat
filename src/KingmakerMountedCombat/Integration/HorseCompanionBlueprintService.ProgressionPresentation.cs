using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Known issues Sprint 7 (U03): the class progression panel's rows for the character it shows. The panel builds its
    // layout from the unit's own progression data; this hands it projected copies whose level entries omit the
    // weapon-bond upgrades that character's bond never receives. The unit's progression data, its level entries, the
    // stock blueprints and every grant are untouched, and nothing is cached: each panel build reads the descriptor native
    // passes (the committed unit or the level-up/respec preview) afresh.
    internal sealed partial class HorseCompanionBlueprintService
    {
        private static readonly MethodInfo ShallowCopy =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        internal int ProgressionProjectionFaultCount { get; private set; }

        // Replaces the panel's one GetClassProgressions call: native runs exactly once, its result is projected.
        internal ProgressionData[] ProjectClassProgressions(UnitProgressionData progression, BlueprintCharacterClass characterClass)
        {
            var native = progression.GetClassProgressions(characterClass);
            try
            {
                var unit = progression.Owner;
                if (native == null || unit == null || divineMountSelection == null || weaponBondUpgrades.Count == 0) return native;
                var features = unit.Progression.Features;
                var path = DivineMountPolicy.ProgressionDisplayPath(divineBondApplied,
                    DivineMountPolicy.SubstitutesStockBond(unit.IsPlayerFaction), features.HasFact(divineMountSelection),
                    weaponBondFeature != null && features.HasFact(weaponBondFeature));
                return ProjectProgressions(native, feature => DivineMountPolicy.ShowsProgressionRow(weaponBondUpgrades.Contains(feature), path));
            }
            catch (Exception exception)
            {
                // A projection failure shows native's rows; the panel never fails because of it.
                ProgressionProjectionFaultCount++;
                try { logger.Exception("Divine Bond progression rows", exception); } catch { }
                return native;
            }
        }

        // Copies only the progression data that has a row to omit; the array, the data objects and their entries that
        // native returned are never modified.
        internal static ProgressionData[] ProjectProgressions(ProgressionData[] native, Func<BlueprintFeatureBase, bool> shows)
        {
            if (native == null || shows == null) return native;
            ProgressionData[] result = null;
            for (var i = 0; i < native.Length; i++)
            {
                var data = native[i];
                var entries = data?.LevelEntries;
                if (entries == null || !entries.Any(entry => entry?.Features != null && entry.Features.Any(feature => !shows(feature))))
                    continue;
                if (result == null) result = (ProgressionData[])native.Clone();
                var copy = (ProgressionData)ShallowCopy.Invoke(data, null);
                copy.LevelEntries = entries.Select(entry => entry?.Features == null ? entry :
                    new LevelEntry { Level = entry.Level, Features = entry.Features.Where(shows).ToList() }).ToArray();
                result[i] = copy;
            }
            return result ?? native;
        }
    }
}
