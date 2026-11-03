using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
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
        internal static bool Create(UnitEntityData owner, HorseCompanionBlueprintService service, IModLogger logger, JObject observations, BlueprintFeature companion = null, string companionUnitGuid = HorseCompanionBlueprintService.UnitGuid)
        {
            companion = companion ?? service.HorseFeature;
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
                                ReferenceEquals(item.Feature, companion));
                            if (horseItem == null || !controller.SelectFeature(companionState, horseItem))
                            {
                                throw new InvalidOperationException("Native Ranger companion selection did not accept the exact KMC Horse item.");
                            }

                            var previewFact = controller.Preview.GetFact(companion);
                            logger.Info("Native Ranger horse preview selected: fact=" + (previewFact != null) +
                                "; previewPet=" + (controller.Preview.Pet != null) + ".");
                        }

                        controller.Commit();
                        committed = true;
                        commits++;
                        logger.Info("Native Ranger level-up committed: rangerLevel=" + rangerLevel +
                            "; characterLevel=" + owner.Descriptor.Progression.CharacterLevel +
                            "; horseFact=" + (owner.Descriptor.GetFact(companion) != null) +
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
            var feature = owner.Descriptor.GetFact(companion) as Feature;
            var rank = owner.Descriptor.GetFact(service.LevelRank)?.GetRank() ?? 0;
            var pet = owner.Descriptor.Pet;
            observations["nativeRangerCommitCount"] = commits;
            observations["huntersBondSelectionLevel"] = huntersBondSelectionLevel;
            observations["rangerCompanionSelectionLevel"] = rangerCompanionSelectionLevel;
            observations["horseFactRankAtCommit"] = rank;
            observations["horsePresentAtNativeCommit"] = pet != null &&
                string.Equals(pet.Blueprint?.AssetGuid, companionUnitGuid, StringComparison.Ordinal);
            observations["horseFeatureSourceGuid"] = feature?.Source?.AssetGuid;
            return commits == 4 &&
                owner.Descriptor.Progression.CharacterLevel == originalCharacterLevel + 4 &&
                owner.Descriptor.Progression.GetClassLevel(ranger) == originalRangerLevel + 4 &&
                huntersBondSelectionLevel == 4 && rangerCompanionSelectionLevel == 4 &&
                bondSelections.Count(item => ReferenceEquals(item, rangerCompanion)) == 1 &&
                companionSelections.Count(item => ReferenceEquals(item, companion)) == 1 &&
                feature != null && feature.Source != null && rank == 1 && pet != null &&
                string.Equals(pet.Blueprint?.AssetGuid, companionUnitGuid, StringComparison.Ordinal);
        }

        // Further ordinary Ranger levels for an owner that already has its companion.
        internal static int AddRangerLevels(UnitEntityData owner, int levels, IModLogger logger)
        {
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(RangerClassGuid);
            var originalAutoLevelup = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            var commits = 0;
            try
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
                for (var index = 0; index < levels && LevelUpController.CanLevelUp(owner.Descriptor); index++)
                {
                    var controller = LevelUpController.StartWithoutAssigningStaticInstance(owner.Descriptor);
                    if (!controller.SelectClass(ranger)) { controller.Cancel(); break; }
                    controller.Commit();
                    commits++;
                    logger.Info("Native Ranger level committed: characterLevel=" + owner.Descriptor.Progression.CharacterLevel + ".");
                }
            }
            finally { SettingsRoot.Instance.AutoLevelup.CurrentValue = originalAutoLevelup; }
            return commits;
        }

        // One ordinary level of the pet's own class through the native controller, with its choices actually made.
        // Nothing here writes a level, experience or a stat: the commit is the operation under observation.
        internal static JObject CommitPetLevel(UnitEntityData pet, BlueprintCharacterClass petClass, BlueprintFeature upgrade, IModLogger logger)
        {
            var record = new JObject { ["canLevelUp"] = LevelUpController.CanLevelUp(pet.Descriptor), ["committed"] = false, ["before"] = PetProgress(pet, petClass, upgrade) };
            if (record.Value<bool>("canLevelUp") != true) return record;
            var originalAutoLevelup = SettingsRoot.Instance.AutoLevelup.CurrentValue;
            var choices = new JArray(); record["choices"] = choices;
            LevelUpController controller = null;
            var committed = false;
            try
            {
                SettingsRoot.Instance.AutoLevelup.CurrentValue = AutolevelupState.Off;
                controller = LevelUpController.StartWithoutAssigningStaticInstance(pet.Descriptor);
                if (!controller.SelectClass(petClass)) { record["refused"] = "The native controller refused the pet's own class."; return record; }
                record["nextLevel"] = controller.State.NextLevel;
                while (controller.State.AttributePoints > 0)
                {
                    if (!controller.SpendAttributePoint(StatType.Wisdom)) break;
                    choices.Add("attribute:Wisdom");
                }
                SpendPetSkills(controller, petClass, choices);
                for (var pass = 0; pass < 12 && MakeNextPetSelection(controller, choices); pass++) { }
                SpendPetSkills(controller, petClass, choices);
                record["complete"] = controller.State.IsComplete();
                record["skillPoints"] = new JObject { ["total"] = controller.State.TotalSkillPoints, ["spent"] = controller.State.SpentSkillPoints };
                if (controller.State.IsComplete() != true) { record["refused"] = "The native level was not complete."; return record; }
                controller.Commit();
                committed = true;
                record["committed"] = true;
                logger.Info("Native pet level committed: pet=" + pet.UniqueId + "; level=" + pet.Descriptor.Progression.CharacterLevel + ".");
            }
            finally
            {
                if (controller != null && !committed) controller.Cancel();
                SettingsRoot.Instance.AutoLevelup.CurrentValue = originalAutoLevelup;
            }
            record["after"] = PetProgress(pet, petClass, upgrade);
            return record;
        }

        internal static void SpendPetSkills(LevelUpController controller, BlueprintCharacterClass petClass, JArray choices)
        {
            foreach (var skill in petClass.ClassSkills.Concat(StatTypeHelper.Skills).Distinct().ToArray())
            {
                while (controller.State.SkillPointsRemaining > 0 && controller.SpendSkillPoint(skill)) choices.Add("skill:" + skill);
                if (controller.State.SkillPointsRemaining <= 0) return;
            }
        }

        internal static bool MakeNextPetSelection(LevelUpController controller, JArray choices)
        {
            foreach (var state in controller.State.Selections.Where(value => !value.Selected).ToArray())
            {
                var preview = controller.Preview;
                var item = state.Selection.Items.FirstOrDefault(candidate => state.Selection.CanSelect(preview, controller.State, state, candidate));
                if (item == null) continue;
                if (!controller.SelectFeature(state, item)) continue;
                choices.Add("selection:" + (state.Selection as BlueprintScriptableObject)?.name + "=" + item.Feature?.name);
                return true;
            }
            return false;
        }

        internal static JObject PetProgress(UnitEntityData pet, BlueprintCharacterClass petClass, BlueprintFeature upgrade)
        {
            var progression = pet.Descriptor.Progression;
            var classData = progression.GetClassData(petClass);
            var level = classData?.Level ?? 0;
            return new JObject {
                ["id"] = pet.UniqueId, ["master"] = pet.Descriptor.Master.Value?.UniqueId, ["size"] = pet.Descriptor.State.Size.ToString(),
                ["characterLevel"] = progression.CharacterLevel, ["classLevel"] = level, ["experience"] = progression.Experience,
                ["hitPointsBase"] = pet.Stats.HitPoints.BaseValue, ["maxHp"] = pet.MaxHP,
                ["babBase"] = pet.Stats.BaseAttackBonus.BaseValue, ["fortitudeBase"] = pet.Stats.SaveFortitude.BaseValue,
                ["reflexBase"] = pet.Stats.SaveReflex.BaseValue, ["willBase"] = pet.Stats.SaveWill.BaseValue,
                ["classBab"] = classData?.BaseAttackBonus.GetBonus(level), ["classFortitude"] = classData?.FortitudeSave.GetBonus(level),
                ["classReflex"] = classData?.ReflexSave.GetBonus(level), ["classWill"] = classData?.WillSave.GetBonus(level), ["classHitDie"] = petClass == null ? 0 : (int)petClass.HitDie,
                ["strength"] = pet.Stats.Strength.ModifiedValue, ["dexterity"] = pet.Stats.Dexterity.ModifiedValue, ["constitution"] = pet.Stats.Constitution.ModifiedValue,
                ["wisdom"] = pet.Stats.Wisdom.ModifiedValue,
                ["upgradeCount"] = progression.Features.Enumerable.Count(feature => feature.Blueprint == upgrade), ["upgradeRank"] = progression.Features.GetRank(upgrade),
                ["features"] = new JArray(progression.Features.Enumerable.Select(feature => feature.Blueprint.name + "/" + feature.Rank).OrderBy(name => name, StringComparer.Ordinal)) };
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
