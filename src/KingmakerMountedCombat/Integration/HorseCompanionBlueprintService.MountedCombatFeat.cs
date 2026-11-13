using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // FEAT-01's blueprints, additive beside the stock feats. The native PrerequisiteStatValue reads a skill's BaseValue,
    // which is trained ranks alone (ModifiableValueSkill adds its class and attribute bonuses in CalculateBaseValue).
    // The reaction's state lives in ordinary rider buffs: they serialize with the unit, follow the rider and not the
    // mount, and need no archive change.
    internal sealed partial class HorseCompanionBlueprintService
    {
        internal const string MountedCombatFeatureGuid = "a7d2e94c6f1b4830b5c9e28d17f3a604";
        internal const string MountedCombatToggleGuid = "3c81f5a02be74d96a4e07d1c92b6f385";
        internal const string MountedCombatToggleBuffGuid = "e96b0d47c3a2485f8d21f07ea5c4193b";
        internal const string MountedCombatSpentBuffGuid = "58f4c1e7a0d94b26973ae6b12d08c7f1";
        internal const string MountedCombatOwedBuffGuid = "0bd7e3926c5f41a8b164d8f3e27a9c50";
        internal const string MountedCombatWindowBuffGuid = "c2a65f18d7e3490bb8f4167e0a3dc952";

        private const int BuffFlagsFieldToken = 0x04001BB6;
        private const int BuffFlagHiddenInUi = 2, BuffFlagStayOnDeath = 8;

        private BlueprintFeature mountedCombatFeature;
        private BlueprintActivatableAbility mountedCombatToggle;
        private BlueprintBuff mountedCombatToggleBuff, mountedCombatSpentBuff, mountedCombatOwedBuff, mountedCombatWindowBuff;
        private string mountedCombatFailure;
        private readonly List<KeyValuePair<BlueprintFeatureSelection, ExactAppendOnlyArrayLease<BlueprintFeature>>> mountedCombatSelections =
            new List<KeyValuePair<BlueprintFeatureSelection, ExactAppendOnlyArrayLease<BlueprintFeature>>>();
        private readonly List<BlueprintFeatureSelection> mountedCombatSelectionTargets = new List<BlueprintFeatureSelection>();

        internal BlueprintFeature MountedCombatFeature => mountedCombatFeature;
        internal BlueprintActivatableAbility MountedCombatToggle => mountedCombatToggle;
        internal BlueprintBuff MountedCombatSpentBuff => mountedCombatSpentBuff;
        internal BlueprintBuff MountedCombatOwedBuff => mountedCombatOwedBuff;
        internal BlueprintBuff MountedCombatWindowBuff => mountedCombatWindowBuff;
        internal string MountedCombatFailure => mountedCombatFailure;
        internal IEnumerable<BlueprintFeatureSelection> MountedCombatSelections => mountedCombatSelections.Select(pair => pair.Key);

        // A feat fault never costs the player the companions or the mounted controls, and leaves nothing selectable.
        private void RegisterMountedCombatFeatReported(LibraryScriptableObject library)
        {
            try { RegisterMountedCombatFeat(library); }
            catch (Exception exception)
            {
                mountedCombatFailure = exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Mounted Combat feat blueprint registration", exception);
                RemoveMountedCombatFeat(library);
            }
            // Mounted Archery's own fault removes only Mounted Archery. Mounted Combat is its prerequisite, so it is not
            // attempted when Mounted Combat did not register.
            if (mountedCombatFeature != null) RegisterMountedArcheryFeatReported(library);
            else mountedArcheryFailure = "Mounted Combat did not register.";
            if (mountedCombatFeature != null)
            {
                try { LeaseKmcCombatFeatSelections(); }
                catch (Exception exception)
                {
                    mountedCombatFailure = exception.GetType().Name + ": " + exception.Message;
                    mountedArcheryFailure = mountedArcheryFailure ?? "Mounted Combat selections could not be leased.";
                    logger.Exception("KMC combat feat selection lease", exception);
                    RemoveMountedArcheryFeat(library);
                    RemoveMountedCombatFeat(library);
                }
            }
            logger.Info("Mounted Combat feat selections: " + string.Join(",", mountedCombatSelections.Select(pair => pair.Key.name).ToArray()) +
                "; failure=" + (mountedCombatFailure ?? "none") + "; Mounted Archery " +
                (mountedArcheryFeature != null ? "offered" : "absent") + "; failure=" + (mountedArcheryFailure ?? "none") + ".");
        }

        private void RegisterMountedCombatFeat(LibraryScriptableObject library)
        {
            foreach (var guid in new[] { MountedCombatFeatureGuid, MountedCombatToggleGuid, MountedCombatToggleBuffGuid, MountedCombatSpentBuffGuid, MountedCombatOwedBuffGuid, MountedCombatWindowBuffGuid })
                AssertReservedGuidAbsent(library, blueprintList, guid);
            var powerAttack = RequireUniqueNamedBlueprint<BlueprintFeature>(library, "PowerAttackFeature");
            var dodge = RequireUniqueNamedBlueprint<BlueprintFeature>(library, "Dodge");
            var ordinary = RequireUniqueNamedBlueprint<BlueprintFeatureSelection>(library, "BasicFeatSelection");
            var fighter = RequireUniqueNamedBlueprint<BlueprintFeatureSelection>(library, "FighterFeatSelection");
            if (!powerAttack.Groups.Contains(FeatureGroup.Feat) || !powerAttack.Groups.Contains(FeatureGroup.CombatFeat))
                throw new InvalidOperationException("The stock combat-feat group contract changed.");

            // Another mod's Mounted Combat would be a second identically named choice with a different rule.
            var existing = ordinary.AllFeatures.FirstOrDefault(feature => feature != null &&
                (feature.name.IndexOf("MountedCombat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 string.Equals(feature.Name, "Mounted Combat", StringComparison.OrdinalIgnoreCase)));
            if (existing != null)
                throw new InvalidOperationException("An installed feature already offers Mounted Combat: " + existing.name + "/" + existing.AssetGuid + ".");

            var icon = CreateEmbeddedSprite("KingmakerMountedCombat.Assets.MountedCombatReaction.png", "KMC_MountedCombat_Reaction_Icon");
            var featIcon = CreateEmbeddedSprite("KingmakerMountedCombat.Assets.MountedCombatFeat.png", "KMC_MountedCombat_Feat_Icon");
            mountedCombatToggleBuff = CreateMountedCombatBuff("MountedCombatToggleBuff", MountedCombatToggleBuffGuid,
                NewLocalizedString("KMC.MountedCombat.Toggle.Name"), NewLocalizedString("KMC.MountedCombat.Toggle.Description"), BuffFlagHiddenInUi | BuffFlagStayOnDeath, icon);
            mountedCombatSpentBuff = CreateMountedCombatBuff("MountedCombatSpentBuff", MountedCombatSpentBuffGuid,
                NewLocalizedString("KMC.MountedCombat.Spent.Name"), NewLocalizedString("KMC.MountedCombat.Spent.Description"), BuffFlagStayOnDeath, icon);
            mountedCombatOwedBuff = CreateMountedCombatBuff("MountedCombatOwedBuff", MountedCombatOwedBuffGuid,
                NewLocalizedString("KMC.MountedCombat.Owed.Name"), NewLocalizedString("KMC.MountedCombat.Owed.Description"), BuffFlagStayOnDeath, icon);
            mountedCombatWindowBuff = CreateMountedCombatBuff("MountedCombatWindowBuff", MountedCombatWindowBuffGuid,
                NewLocalizedString("KMC.MountedCombat.Spent.Name"), NewLocalizedString("KMC.MountedCombat.Spent.Description"), BuffFlagStayOnDeath, icon);

            mountedCombatToggle = CreateOwned<BlueprintActivatableAbility>("MountedCombatToggle");
            mountedCombatToggle.AssetGuid = MountedCombatToggleGuid;
            mountedCombatToggle.Buff = mountedCombatToggleBuff;
            mountedCombatToggle.IsOnByDefault = true;
            mountedCombatToggle.DeactivateImmediately = true;
            mountedCombatToggle.ActivationType = AbilityActivationType.Immediately;
            mountedCombatToggle.ResourceAssetIds = new string[0];
            mountedCombatToggle.ComponentsArray = new BlueprintComponent[0];
            SetFeaturePresentation(mountedCombatToggle, NewLocalizedString("KMC.MountedCombat.Toggle.Name"), NewLocalizedString("KMC.MountedCombat.Toggle.Description"), icon);

            mountedCombatFeature = CreateOwned<BlueprintFeature>("MountedCombatFeature");
            mountedCombatFeature.AssetGuid = MountedCombatFeatureGuid;
            CopyFeatureContract(powerAttack, mountedCombatFeature);
            SetFeaturePresentation(mountedCombatFeature, NewLocalizedString("KMC.MountedCombat.Name"), NewLocalizedString("KMC.MountedCombat.Description"), featIcon);
            var trained = CreateOwned<PrerequisiteStatValue>("KMC_MountedCombat_MobilityRank");
            trained.Stat = StatType.SkillMobility;
            trained.Value = MountedCombatDefensePolicy.MinimumTrainedRanks;
            var addToggle = CreateOwned<AddFacts>("KMC_MountedCombat_AddToggle");
            addToggle.Facts = new Kingmaker.Blueprints.Facts.BlueprintUnitFact[] { mountedCombatToggle };
            mountedCombatFeature.ComponentsArray = new BlueprintComponent[] { trained, addToggle };

            AddExactBlueprintRegistration(library, mountedCombatToggleBuff);
            AddExactBlueprintRegistration(library, mountedCombatSpentBuff);
            AddExactBlueprintRegistration(library, mountedCombatOwedBuff);
            AddExactBlueprintRegistration(library, mountedCombatWindowBuff);
            AddExactBlueprintRegistration(library, mountedCombatToggle);
            AddExactBlueprintRegistration(library, mountedCombatFeature);

            // The ordinary feat list and every combat-bonus list: a selection offering both stock combat feats is one.
            mountedCombatSelectionTargets.AddRange(blueprintList.OfType<BlueprintFeatureSelection>().Where(value =>
                value.AllFeatures != null && value.AllFeatures.Contains(powerAttack) && value.AllFeatures.Contains(dodge) && !value.AllFeatures.Contains(null)));
            if (!mountedCombatSelectionTargets.Any(selection => ReferenceEquals(selection, ordinary)) || !mountedCombatSelectionTargets.Any(selection => ReferenceEquals(selection, fighter)))
                throw new InvalidOperationException("The ordinary or fighter feat selection no longer offers the stock combat feats.");
        }

        // One exact lease per shared AllFeatures array for every KMC combat feat that registered, in a fixed order (Mounted
        // Combat, then Mounted Archery). Two leases captured from the same original array would each append to and
        // restore over the other; a single lease keeps the original order, refuses duplicates and keeps compare-before-
        // write and compare-before-restore against foreign drift. Installed BlueprintFeatureSelection.Items enumerates
        // AllFeatures only (see the Ranger lease); the stock Features arrays are not an offered surface and stay unchanged.
        private void LeaseKmcCombatFeatSelections()
        {
            var items = KmcCombatFeatPublication.Items(mountedCombatFeature, mountedArcheryFeature);
            foreach (var selection in mountedCombatSelectionTargets)
                mountedCombatSelections.Add(new KeyValuePair<BlueprintFeatureSelection, ExactAppendOnlyArrayLease<BlueprintFeature>>(
                    selection, new ExactAppendOnlyArrayLease<BlueprintFeature>(selection.AllFeatures, items)));
        }

        private BlueprintBuff CreateMountedCombatBuff(string name, string guid, Kingmaker.Localization.LocalizedString displayName,
            Kingmaker.Localization.LocalizedString description, int flags, UnityEngine.Sprite icon)
        {
            var buff = CreateOwned<BlueprintBuff>(name);
            buff.AssetGuid = guid;
            buff.Stacking = StackingType.Replace;
            buff.FxOnStart = new PrefabLink();
            buff.FxOnRemove = new PrefabLink();
            buff.ResourceAssetIds = new string[0];
            buff.ComponentsArray = new BlueprintComponent[0];
            var field = typeof(BlueprintBuff).GetField("m_Flags", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.MetadataToken != BuffFlagsFieldToken)
                throw new MissingFieldException("The native buff flags field differs.");
            field.SetValue(buff, Enum.ToObject(field.FieldType, flags));
            SetFeaturePresentation(buff, displayName, description, icon);
            return buff;
        }

        private void RemoveMountedCombatFeat(LibraryScriptableObject library)
        {
            RemoveExact(library, blueprintList, MountedCombatFeatureGuid, mountedCombatFeature);
            RemoveExact(library, blueprintList, MountedCombatToggleGuid, mountedCombatToggle);
            RemoveExact(library, blueprintList, MountedCombatToggleBuffGuid, mountedCombatToggleBuff);
            RemoveExact(library, blueprintList, MountedCombatSpentBuffGuid, mountedCombatSpentBuff);
            RemoveExact(library, blueprintList, MountedCombatOwedBuffGuid, mountedCombatOwedBuff);
            RemoveExact(library, blueprintList, MountedCombatWindowBuffGuid, mountedCombatWindowBuff);
            mountedCombatFeature = null; mountedCombatToggle = null; mountedCombatToggleBuff = null;
            mountedCombatSpentBuff = null; mountedCombatOwedBuff = null; mountedCombatWindowBuff = null;
            mountedCombatSelections.Clear();
            mountedCombatSelectionTargets.Clear();
        }

        private void ApplyMountedCombatSelectionsReported(bool desired)
        {
            string error = null;
            foreach (var pair in mountedCombatSelections)
            {
                var current = pair.Key.AllFeatures;
                if (desired ? pair.Value.MatchesAppended(current) : pair.Value.MatchesOriginal(current)) continue;
                if (desired ? !pair.Value.MatchesOriginal(current) : !pair.Value.MatchesAppended(current))
                {
                    error = JoinErrors(error, "Feat selection " + pair.Key.name + " changed outside KMC; no overwrite was attempted.");
                    continue;
                }
                pair.Key.AllFeatures = desired ? pair.Value.CreateAppendedValue() : pair.Value.CreateOriginalValue();
            }
            if (error == null) return;
            mountedCombatFailure = error;
            logger.Error(error);
        }
    }
}
