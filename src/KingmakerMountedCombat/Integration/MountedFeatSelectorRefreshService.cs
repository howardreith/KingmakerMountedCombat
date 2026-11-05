using System;
using System.Linq;
using Kingmaker.Assets.UI.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp;


namespace KingmakerMountedCombat.Integration
{
    // A replaced preview needs native sorting/layout and row/tooltip reference rebinding.
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

        internal static bool RebindEqualPreviewRow(HorseCompanionBlueprintService blueprints, CharBuildSelectorItem row,
            FeatureSelectionState state, Kingmaker.Blueprints.Classes.Selection.IFeatureSelectionItem feature,
            CharBSelectorLayer layer, UnityEngine.Events.UnityAction action)
        {
            if (row == null || state == null || feature == null || layer == null || row.FeatureSelection == null || blueprints == null ||
                ReferenceEquals(row.FeatureSelection, state) || !state.Equals(row.FeatureSelection) ||
                !ReferenceEquals(row.Feature, feature) ||
                !blueprints.MountedCombatSelections.Any(selection => ReferenceEquals(selection, state.Selection) &&
                    selection.AllFeatures != null && selection.AllFeatures.Contains(blueprints.MountedCombatFeature))) return false;
            // Native ReInit's value equality skips this exact case. Use the native initializer
            // with the caller's current callback so tooltip, recommendation and fact binding move together.
            row.Init(state, feature, layer, action);
            return true;
        }
    }
}
