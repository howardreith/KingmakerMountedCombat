using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Mounted Archery: a passive combat feat whose only rule is MountedRangedMovementService's reduced contribution. It is
    // registered after Mounted Combat (its prerequisite) and published in the same coordinated selection leases. A fault
    // here removes only this feat; with it gone the mounted-movement penalty is not applied either (IsMountedArcheryAvailable).
    internal sealed partial class HorseCompanionBlueprintService
    {
        internal const string MountedArcheryFeatureGuid = "b81926bca1d24a3997cdc8471a84014b";

        private BlueprintFeature mountedArcheryFeature;
        private BlueprintFeature mountedRangedMovementLabel, mountedRangedMovementReducedLabel;
        private Fact mountedRangedMovementSource, mountedRangedMovementReducedSource;
        private string mountedArcheryFailure;
        private bool mountedArcheryWithdrawalReported;

        internal BlueprintFeature MountedArcheryFeature => mountedArcheryFeature;
        internal string MountedArcheryFailure => mountedArcheryFailure;
        // Combat-log sources for the one contribution. They are detached, componentless facts of unregistered label
        // blueprints: neither assumes the rider owns any feature, and neither is ever attached to a unit or saved.
        internal Fact MountedRangedMovementSource => mountedRangedMovementSource;
        internal Fact MountedRangedMovementReducedSource => mountedRangedMovementReducedSource;

        internal bool MountedArcheryWithdrawn => mountedCombatPublication?.MountedArcheryWithdrawn == true;

        // The penalty and its reduction exist together. The mechanic is available while KMC publishes the feat and both
        // required paths (ordinary and fighter feats) still hold it. If a required path loses it, the publication removes
        // KMC's own entry from every array that holds it, including arrays other mods changed, so no list offers a feat
        // whose mechanic is off; a non-required list that loses it only stops offering it there (KmcCombatFeatPublicationSet).
        internal bool IsMountedArcheryAvailable()
        {
            if (mountedArcheryFeature == null || mountedArcheryFailure != null || mountedCombatPublication == null ||
                mountedRangedMovementSource == null || mountedRangedMovementReducedSource == null) return false;
            var available = mountedCombatPublication.MountedArcheryAvailable();
            ReportMountedArcheryWithdrawal();
            return available;
        }

        // Each frame once registered: before withdrawal, one reference comparison per required array and a full check only
        // of a replaced one; once withdrawn, a membership scan of every KMC array, so an in-place reintroduction is removed.
        private void RefreshMountedArcheryPublicationReported()
        {
            if (mountedCombatPublication != null && mountedCombatPublication.Refresh(false)) ReportMountedArcheryWithdrawal();
        }

        private void ReportMountedArcheryWithdrawal()
        {
            if (mountedCombatPublication == null || !mountedCombatPublication.MountedArcheryWithdrawn || mountedArcheryWithdrawalReported) return;
            mountedArcheryWithdrawalReported = true;
            logger.Warning("Mounted Archery withdrawn for this session: a required feat selection no longer offers it (" +
                mountedCombatPublication.WithdrawalReason + "). KMC's Mounted Archery entry was removed from every KMC feat list " +
                "that still held it; Mounted Combat and other mods' entries are untouched; no mounted ranged penalty or reduction applies.");
        }

        private void RegisterMountedArcheryFeatReported(LibraryScriptableObject library)
        {
            try { RegisterMountedArcheryFeat(library); }
            catch (Exception exception)
            {
                mountedArcheryFailure = exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Mounted Archery feat blueprint registration", exception);
                RemoveMountedArcheryFeat(library);
            }
        }

        private void RegisterMountedArcheryFeat(LibraryScriptableObject library)
        {
            if (mountedCombatFeature == null)
                throw new InvalidOperationException("Mounted Archery requires the KMC Mounted Combat feature, which did not register.");
            AssertReservedGuidAbsent(library, blueprintList, MountedArcheryFeatureGuid);
            var powerAttack = RequireUniqueNamedBlueprint<BlueprintFeature>(library, "PowerAttackFeature");
            var ordinary = RequireUniqueNamedBlueprint<Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection>(library, "BasicFeatSelection");
            // Another mod's Mounted Archery would be a second identically named choice with a different rule.
            var existing = ordinary.AllFeatures.FirstOrDefault(feature => feature != null &&
                (feature.name.IndexOf("MountedArchery", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 string.Equals(feature.Name, "Mounted Archery", StringComparison.OrdinalIgnoreCase)));
            if (existing != null)
                throw new InvalidOperationException("An installed feature already offers Mounted Archery: " + existing.name + "/" + existing.AssetGuid + ".");

            var icon = CreateEmbeddedSprite("KingmakerMountedCombat.Assets.MountedArcheryFeat.png", "KMC_MountedArchery_Feat_Icon");
            mountedArcheryFeature = CreateOwned<BlueprintFeature>("MountedArcheryFeature");
            mountedArcheryFeature.AssetGuid = MountedArcheryFeatureGuid;
            BuildMountedArcheryContract(powerAttack, mountedArcheryFeature, NewLocalizedString("KMC.MountedArchery.Name"),
                NewLocalizedString("KMC.MountedArchery.Description"), icon);
            // Mobility trained ranks are the established Ride adaptation (BaseValue is ranks alone). Both prerequisites are
            // in the native ALL group and read the level-up preview unit, so a same-level Mounted Combat pick or newly
            // allocated rank counts, and Back/cancel removes it.
            var trained = CreateOwned<PrerequisiteStatValue>("KMC_MountedArchery_MobilityRank");
            trained.Stat = StatType.SkillMobility;
            trained.Value = MountedCombatDefensePolicy.MinimumTrainedRanks;
            trained.Group = Prerequisite.GroupType.All;
            var mountedCombat = CreateOwned<PrerequisiteFeature>("KMC_MountedArchery_MountedCombat");
            mountedCombat.Feature = mountedCombatFeature;
            mountedCombat.Group = Prerequisite.GroupType.All;
            mountedArcheryFeature.ComponentsArray = new BlueprintComponent[] { trained, mountedCombat };

            mountedRangedMovementLabel = CreateMountedRangedMovementLabel("KMC_MountedRangedMovement_Label",
                "KMC.MountedRangedMovement.Name", "KMC.MountedRangedMovement.Description", icon);
            mountedRangedMovementReducedLabel = CreateMountedRangedMovementLabel("KMC_MountedRangedMovement_ReducedLabel",
                "KMC.MountedRangedMovement.Reduced.Name", "KMC.MountedRangedMovement.Reduced.Description", icon);
            mountedRangedMovementSource = new Fact(mountedRangedMovementLabel);
            mountedRangedMovementReducedSource = new Fact(mountedRangedMovementReducedLabel);

            AddExactBlueprintRegistration(library, mountedArcheryFeature);
            var violation = MountedArcheryContractViolation(mountedArcheryFeature, powerAttack, icon);
            if (violation != null) throw new InvalidOperationException(violation);
            // One line per registration: the facts a feat-screen report needs, without per-frame logging.
            logger.Info("Mounted Archery feat contract: " + ReadFeatContractFacts(mountedArcheryFeature, icon).Describe() +
                "; donor PowerAttackFeature " + ReadFeatContractFacts(powerAttack, icon).Describe() + ".");
        }

        // A single visible rank with the Feat and CombatFeat groups and the stock combat feat's own flags, including
        // IsClassFeature, which every stock 2.1.7b feat holds true (MountedArcheryFeatContract). Kept separate from the
        // library so scripts/Test-MountedArcheryFeatContract.ps1 can run it on genuine blueprints outside the game.
        internal static void BuildMountedArcheryContract(BlueprintFeature donor, BlueprintFeature feature,
            Kingmaker.Localization.LocalizedString name, Kingmaker.Localization.LocalizedString description, UnityEngine.Sprite icon)
        {
            CopyFeatureContract(donor, feature);
            SetFeaturePresentation(feature, name, description, icon);
        }

        // Null when the registered feature holds the contract; otherwise each failed invariant with observed values.
        internal static string MountedArcheryContractViolation(BlueprintFeature feature, BlueprintFeature donor, UnityEngine.Sprite icon) =>
            MountedArcheryFeatContract.Violation(ReadFeatContractFacts(feature, icon), ReadFeatContractFacts(donor, icon));

        // Reference comparisons only: Unity's overloaded null test is not needed for a sprite KMC holds.
        private static KmcFeatContractFacts ReadFeatContractFacts(BlueprintFeature feature, UnityEngine.Sprite icon)
        {
            var current = feature.Icon;
            return new KmcFeatContractFacts
            {
                Groups = feature.Groups == null ? new string[0] : feature.Groups.Select(group => group.ToString()).ToArray(),
                Ranks = feature.Ranks,
                IsClassFeature = feature.IsClassFeature,
                HasIcon = !ReferenceEquals(current, null),
                IconIsKmc = !ReferenceEquals(current, null) && ReferenceEquals(current, icon),
                HideInUI = feature.HideInUI,
                HideInCharacterSheetAndLevelUp = feature.HideInCharacterSheetAndLevelUp,
                HideNotAvailibleInUI = feature.HideNotAvailibleInUI
            };
        }

        private BlueprintFeature CreateMountedRangedMovementLabel(string name, string nameKey, string descriptionKey, UnityEngine.Sprite icon)
        {
            var label = CreateOwned<BlueprintFeature>(name);
            label.Groups = new FeatureGroup[0];
            label.Ranks = 1;
            label.HideInUI = true;
            label.HideInCharacterSheetAndLevelUp = true;
            label.ComponentsArray = new BlueprintComponent[0];
            SetFeaturePresentation(label, NewLocalizedString(nameKey), NewLocalizedString(descriptionKey), icon);
            return label;
        }

        private void RemoveMountedArcheryFeat(LibraryScriptableObject library)
        {
            RemoveExact(library, blueprintList, MountedArcheryFeatureGuid, mountedArcheryFeature);
            mountedArcheryFeature = null;
            mountedRangedMovementLabel = null; mountedRangedMovementReducedLabel = null;
            mountedRangedMovementSource = null; mountedRangedMovementReducedSource = null;
        }
    }
}
