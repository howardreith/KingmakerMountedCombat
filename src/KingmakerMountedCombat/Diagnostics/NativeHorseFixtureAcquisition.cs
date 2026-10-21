using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Diagnostic fixture construction through the unchanged native progression pipeline.
    // Callers require the guarded disposable Working fixture. This grants no direct facts.
    internal static class NativeHorseFixtureAcquisition
    {
        private const string RangerClassGuid = "cda0615668a6df14eb36ba19ee881af6";
        private const string HuntersBondSelectionGuid = "b705c5184a96a84428eeb35ae2517a14";
        internal static bool Create(UnitEntityData owner, HorseCompanionBlueprintService service, IModLogger logger, JObject observations)
        {
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(RangerClassGuid);
            var huntersBond = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(HuntersBondSelectionGuid);
            var rangerCompanion = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>(
                HorseCompanionBlueprintService.RangerSelectionGuid);
            if (ranger == null || huntersBond == null || rangerCompanion == null ||
                !ReferenceEquals(rangerCompanion, ResourcesLibrary.LibraryObject.BlueprintsByAssetId[
                    HorseCompanionBlueprintService.RangerSelectionGuid]))
            {
                throw new InvalidOperationException("The exact Ranger class/level-4 bond/companion selection contract is unavailable.");
            }

            var originalCharacterLevel = owner.Descriptor.Progression.CharacterLevel;
            var originalRangerLevel = owner.Descriptor.Progression.GetClassLevel(ranger);
            var originalAutoLevelup = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            var commits = 0;
            var huntersBondSelectionLevel = -1;
            var rangerCompanionSelectionLevel = -1;
            if (originalRangerLevel != 0)
            {
                throw new InvalidOperationException("The disposable native-level-up owner already has Ranger levels: " + originalRangerLevel + ".");
            }
            try
            {
                // The Working fixture enables automatic plans globally. Disable
                // that setting only while constructing each native controller so
                // the diagnostic can exercise an explicit Ranger selection.
                SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
                for (var rangerLevel = 1; rangerLevel <= 4; rangerLevel++)
                {
                    // StartWithoutAssigningStaticInstance can construct a controller
                    // even when this disposable actor has no native level-up due.
                    // Fixture setup must not turn that into four free levels.
                    if (!LevelUpController.CanLevelUp(owner.Descriptor))
                    {
                        throw new InvalidOperationException("Native Ranger fixture owner has no level-up entitlement before Ranger level " +
                            rangerLevel + ".");
                    }
                    LevelUpController controller = null;
                    var committed = false;
                    try
                    {
                        controller = LevelUpController.StartWithoutAssigningStaticInstance(owner.Descriptor);
                        if (!controller.SelectClass(ranger))
                        {
                            throw new InvalidOperationException("Native Ranger class selection failed at Ranger level " + rangerLevel + ".");
                        }

                        if (rangerLevel == 4)
                        {
                            var bondState = FindExactSelection(controller, huntersBond);
                            huntersBondSelectionLevel = bondState.Level;
                            var companionItem = bondState.Selection.Items.SingleOrDefault(item =>
                                ReferenceEquals(item.Feature, rangerCompanion));
                            if (companionItem == null || !controller.SelectFeature(bondState, companionItem))
                            {
                                throw new InvalidOperationException("Native Hunter's Bond did not accept the exact Ranger companion selection.");
                            }

                            var companionState = FindExactSelection(controller, rangerCompanion);
                            rangerCompanionSelectionLevel = companionState.Level;
                            var horseItem = companionState.Selection.Items.SingleOrDefault(item =>
                                ReferenceEquals(item.Feature, service.HorseFeature));
                            if (horseItem == null || !controller.SelectFeature(companionState, horseItem))
                            {
                                throw new InvalidOperationException("Native Ranger companion selection did not accept the exact KMC Horse item.");
                            }

                            var previewFact = controller.Preview.GetFact(service.HorseFeature);
                            logger.Info("Native Ranger horse preview selected: fact=" + (previewFact != null) +
                                "; previewPet=" + (controller.Preview.Pet != null) + ".");
                        }

                        controller.Commit();
                        committed = true;
                        commits++;
                        logger.Info("Native Ranger level-up committed: rangerLevel=" + rangerLevel +
                            "; characterLevel=" + owner.Descriptor.Progression.CharacterLevel +
                            "; horseFact=" + (owner.Descriptor.GetFact(service.HorseFeature) != null) +
                            "; pet=" + (owner.Descriptor.Pet != null) + ".");
                    }
                    finally
                    {
                        if (controller != null && !committed)
                        {
                            controller.Cancel();
                        }
                    }
                }
            }
            finally
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = originalAutoLevelup;
            }

            var bondSelections = owner.Descriptor.Progression.GetSelections(huntersBond, huntersBondSelectionLevel);
            var companionSelections = owner.Descriptor.Progression.GetSelections(
                rangerCompanion, rangerCompanionSelectionLevel);
            var feature = owner.Descriptor.GetFact(service.HorseFeature) as Feature;
            var rank = owner.Descriptor.GetFact(service.LevelRank)?.GetRank() ?? 0;
            var pet = owner.Descriptor.Pet;
            observations["nativeRangerCommitCount"] = commits;
            observations["huntersBondSelectionLevel"] = huntersBondSelectionLevel;
            observations["rangerCompanionSelectionLevel"] = rangerCompanionSelectionLevel;
            observations["horseFactRankAtCommit"] = rank;
            observations["horsePresentAtNativeCommit"] = pet != null &&
                string.Equals(pet.Blueprint?.AssetGuid, HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal);
            observations["horseFeatureSourceGuid"] = feature?.Source?.AssetGuid;
            return commits == 4 &&
                owner.Descriptor.Progression.CharacterLevel == originalCharacterLevel + 4 &&
                owner.Descriptor.Progression.GetClassLevel(ranger) == originalRangerLevel + 4 &&
                huntersBondSelectionLevel == 4 && rangerCompanionSelectionLevel == 4 &&
                bondSelections.Count(item => ReferenceEquals(item, rangerCompanion)) == 1 &&
                companionSelections.Count(item => ReferenceEquals(item, service.HorseFeature)) == 1 &&
                feature != null && feature.Source != null && rank == 1 && pet != null &&
                string.Equals(pet.Blueprint?.AssetGuid, HorseCompanionBlueprintService.UnitGuid, StringComparison.Ordinal);
        }

        private static FeatureSelectionState FindExactSelection(
            LevelUpController controller,
            BlueprintFeatureSelection selection)
        {
            var matches = controller.State.Selections.Where(state =>
                ReferenceEquals(state.Selection, selection) && !state.Selected).ToArray();
            if (matches.Length != 1)
            {
                var available = string.Join(", ", controller.State.Selections.Select(state =>
                {
                    var blueprint = state.Selection as BlueprintScriptableObject;
                    return (blueprint == null ? state.Selection.GetType().FullName : blueprint.name + "/" + blueprint.AssetGuid) +
                        ":selected=" + state.Selected;
                }).ToArray());
                throw new InvalidOperationException("Expected one unselected native " + selection.name +
                    " state; observed " + matches.Length + ". Available: " + available + ".");
            }
            return matches[0];
        }

    }
}
