using System;
using System.Linq;
using Kingmaker.Assets.UI.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp;


namespace KingmakerMountedCombat.Integration
{
    // Native lightweight refresh already rebinds rows to the new preview's FeatureSelectionState.
    // It skips sorting and layout for the same blueprint/index even when that preview was replaced.
    internal static class MountedFeatSelectorRefreshService
    {
        internal static bool BeforeRefresh(HorseCompanionBlueprintService blueprints, CharBSelectorLayer layer,
            FeatureSelectionState next, ref bool refreshSort)
        {
            if (refreshSort || layer == null || !layer.isInit || next == null ||
                layer.CurrentSelectionState == null || ReferenceEquals(layer.CurrentSelectionState, next) ||
                !ReferenceEquals(layer.CurrentSelectionState.Selection, next.Selection) || blueprints == null ||
                !blueprints.MountedCombatSelections.Any(selection => ReferenceEquals(selection, next.Selection) &&
                    selection.AllFeatures != null && selection.AllFeatures.Contains(blueprints.MountedCombatFeature))) return false;
            // Keep native priority/name/eligibility comparison and native selected-feature binding.
            // No new facts, blueprint mutation, custom ordering, list rebuild, fade or subscriptions.
            refreshSort = true;
            layer.HasShowedItems = false;
            layer.HasDisableItems = false;
            return true;
        }

        internal static void AfterRefresh(CharBSelectorLayer layer, bool regrouped)
        {
            if (!regrouped || layer == null || !layer.gameObject.activeInHierarchy) return;
            layer.UpdateLayout();

        }
    }
}
