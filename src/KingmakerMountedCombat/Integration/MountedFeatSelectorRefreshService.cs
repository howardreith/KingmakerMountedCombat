using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Assets.UI.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp;
using KingmakerMountedCombat.Domain;


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

        internal static void ClearStaleRowSelection(HorseCompanionBlueprintService blueprints, CharBSelectorLayer layer)
        {
            var state = layer == null ? null : layer.CurrentSelectionState;
            if (state == null || blueprints == null || !NativeToggleGroupContract.Available) return;
            var group = NativeToggleGroupContract.Field.GetValue(layer);
            if (group == null) return;
            var offersMountedCombat = blueprints.MountedCombatSelections.Any(selection => ReferenceEquals(selection, state.Selection) &&
                selection.AllFeatures != null && selection.AllFeatures.Contains(blueprints.MountedCombatFeature));
            // The group holds only active rows; native switches a hidden row off directly.
            var anyActiveRowOn = offersMountedCombat && state.SelectedItem == null && (bool)NativeToggleGroupContract.AnyTogglesOn.Invoke(group, null);
            if (!FeatSelectorSelectionPolicy.MustClearStaleRows(offersMountedCombat, state.SelectedItem != null, anyActiveRowOn)) return;
            // The native full refresh's own clear, under its own action guard: rows repaint
            // and restyle through their listeners, and no selection callback can run.
            var ignoreAction = layer.IgnoreAction;
            layer.IgnoreAction = true;
            try { NativeToggleGroupContract.SetAllTogglesOff.Invoke(group, null); }
            finally { layer.IgnoreAction = ignoreAction; }
        }

        // The package allowlist keeps UnityEngine.UI out of the product's references, so the
        // exact installed members are reached by identity; any other build leaves the row native.
        private static class NativeToggleGroupContract
        {
            internal static readonly FieldInfo Field = typeof(CharBSelectorLayer).GetField("ToggleGroup", BindingFlags.Instance | BindingFlags.Public);
            private static readonly Type GroupType = Field != null && Field.MetadataToken == 0x04006D56 && Field.FieldType.FullName == "UnityEngine.UI.ToggleGroup" ? Field.FieldType : null;
            internal static readonly MethodInfo AnyTogglesOn = Method("AnyTogglesOn", 0x060004F8, typeof(bool));
            internal static readonly MethodInfo SetAllTogglesOff = Method("SetAllTogglesOff", 0x060004FA, typeof(void));
            internal static bool Available => AnyTogglesOn != null && SetAllTogglesOff != null;

            private static MethodInfo Method(string name, int token, Type returnType)
            {
                var method = GroupType == null ? null : GroupType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                return method != null && method.MetadataToken == token && method.ReturnType == returnType ? method : null;
            }
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
