using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Mounted Shield: a passive combat feat (prerequisites Mounted Combat and Shield Focus) whose two benefits are applied by
    // MountedShieldService: a shield bonus on the current mount's AC and an addition to the Mounted Combat hit-negation
    // check. It is registered after Mounted Archery and published in the same coordinated selection leases. A fault here
    // removes only this feat; with it gone or withdrawn neither benefit applies (IsMountedShieldAvailable). Mounted Combat
    // and Mounted Archery never depend on it.
    internal sealed partial class HorseCompanionBlueprintService
    {
        internal const string MountedShieldFeatureGuid = "671a301bd1774677a8bfa2873d33b191";
        internal const string MountedShieldIconResource = "KingmakerMountedCombat.Assets.MountedShieldFeat.png";
        internal const string MountedShieldIconSprite = "KMC_MountedShield_Feat_Icon";
        // Stock Kingmaker 2.1.7b feats, resolved by GUID and name together (sharedassets1.assets; see
        // scripts/Test-MountedShieldNativeContract.ps1, which reads both from the installed data).
        internal const string ShieldFocusGuid = "ac57069b6bf8c904086171683992a92a";
        internal const string ShieldFocusName = "ShieldFocus";
        internal const string GreaterShieldFocusGuid = "afd05ca5363036c44817c071189b67e1";
        internal const string GreaterShieldFocusName = "ShieldFocusGreater";

        private BlueprintFeature mountedShieldFeature, shieldFocusFeature, greaterShieldFocusFeature;
        private Func<bool> mountedShieldSelectable;
        private UnityEngine.Sprite mountedShieldIcon;
        private string mountedShieldFailure;
        private bool mountedShieldWithdrawalReported;

        internal BlueprintFeature MountedShieldFeature => mountedShieldFeature;
        internal BlueprintFeature ShieldFocusFeature => shieldFocusFeature;
        internal BlueprintFeature GreaterShieldFocusFeature => greaterShieldFocusFeature;
        internal UnityEngine.Sprite MountedShieldIcon => mountedShieldIcon;
        internal string MountedShieldFailure => mountedShieldFailure;
        internal bool MountedShieldWithdrawn => mountedCombatPublication?.MountedShieldWithdrawn == true;
        // Set by the composition root: the services are on and the Mounted Shield service is enabled, so both benefits apply.
        internal Func<bool> MountedShieldServiceActive { get; set; }

        // Both benefits exist together. They are available while KMC publishes the feat and both required paths (ordinary
        // and fighter feats) still hold it; otherwise the publication removes KMC's own Mounted Shield entry from every
        // array that holds it, so no list offers a feat whose mechanic is off. Mounted Archery's state plays no part.
        internal bool IsMountedShieldAvailable()
        {
            if (mountedShieldFeature == null || mountedShieldFailure != null || mountedCombatPublication == null ||
                shieldFocusFeature == null || greaterShieldFocusFeature == null) return false;
            var available = mountedCombatPublication.MountedShieldAvailable();
            ReportMountedShieldWithdrawal();
            return available;
        }

        // The terminal stop (CompositionRoot failure cleanup, supplemental review PN4-F3) closes Mounted Shield's offers with
        // its benefits, reversibly, so no list (one that ignores prerequisites included) keeps a feat whose mechanic is
        // stopped; an explicit enable reopens them. Mounted Combat and Mounted Archery are untouched.
        internal void SuspendMountedShieldOffers() => mountedCombatPublication?.Suspend(KmcOptionalCombatFeat.MountedShield);

        internal void ResumeMountedShieldOffers()
        {
            var error = mountedCombatPublication?.Resume(KmcOptionalCombatFeat.MountedShield, SelectionName);
            if (error == null) return;
            mountedCombatFailure = error;
            logger.Error(error);
        }

        internal bool MountedShieldOffersSuspended => mountedCombatPublication?.Suspended(KmcOptionalCombatFeat.MountedShield) == true;

        private void ReportMountedShieldWithdrawal()
        {
            if (mountedCombatPublication == null || !mountedCombatPublication.MountedShieldWithdrawn || mountedShieldWithdrawalReported) return;
            mountedShieldWithdrawalReported = true;
            logger.Warning("Mounted Shield withdrawn for this session: a required feat selection no longer offers it (" +
                mountedCombatPublication.MountedShieldWithdrawalReason + "). KMC's Mounted Shield entry was removed from every KMC feat list " +
                "that still held it; Mounted Combat, Mounted Archery and other mods' entries are untouched; no Mounted Shield benefit applies.");
        }

        private void RegisterMountedShieldFeatReported(LibraryScriptableObject library)
        {
            try { RegisterMountedShieldFeat(library); }
            catch (Exception exception)
            {
                mountedShieldFailure = exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Mounted Shield feat blueprint registration", exception);
                RemoveMountedShieldFeat(library);
            }
        }

        private void RegisterMountedShieldFeat(LibraryScriptableObject library)
        {
            if (mountedCombatFeature == null)
                throw new InvalidOperationException("Mounted Shield requires the KMC Mounted Combat feature, which did not register.");
            AssertReservedGuidAbsent(library, blueprintList, MountedShieldFeatureGuid);
            var powerAttack = RequireUniqueNamedBlueprint<BlueprintFeature>(library, "PowerAttackFeature");
            shieldFocusFeature = RequireBlueprint<BlueprintFeature>(library, ShieldFocusGuid, ShieldFocusName);
            greaterShieldFocusFeature = RequireBlueprint<BlueprintFeature>(library, GreaterShieldFocusGuid, GreaterShieldFocusName);
            // Another mod's Mounted Shield would be a second identically named choice with a different rule, in any list KMC
            // publishes to (ordinary, fighter and the other combat-bonus lists Mounted Combat already selected).
            if (mountedCombatSelectionTargets.Count == 0)
                throw new InvalidOperationException("Mounted Shield has no Mounted Combat publication targets.");
            var existing = KmcCombatFeatCollision.FindCompeting(mountedCombatSelectionTargets.Select(selection => selection.AllFeatures),
                feature => feature.name, feature => feature.Name, "MountedShield", "Mounted Shield");
            if (existing != null)
                throw new InvalidOperationException("An installed feature already offers Mounted Shield: " + existing.name + "/" + existing.AssetGuid + ".");

            // A missing or undecodable icon throws here, which withdraws this feat alone (no substitute image).
            mountedShieldIcon = CreateEmbeddedSprite(MountedShieldIconResource, MountedShieldIconSprite);
            mountedShieldFeature = CreateOwned<BlueprintFeature>("MountedShieldFeature");
            mountedShieldFeature.AssetGuid = MountedShieldFeatureGuid;
            BuildMountedShieldContract(powerAttack, mountedShieldFeature, NewLocalizedString("KMC.MountedShield.Name"),
                NewLocalizedString("KMC.MountedShield.Description"), mountedShieldIcon);
            // Exactly the two published prerequisites, in the native ALL group, read from the level-up preview unit: a
            // same-level Mounted Combat or Shield Focus pick counts, and Back/cancel removes it. No rank, BAB or
            // mount-ownership prerequisite is added.
            var mountedCombat = CreateOwned<PrerequisiteFeature>("KMC_MountedShield_MountedCombat");
            mountedCombat.Feature = mountedCombatFeature;
            mountedCombat.Group = Prerequisite.GroupType.All;
            var shieldFocus = CreateOwned<PrerequisiteFeature>("KMC_MountedShield_ShieldFocus");
            shieldFocus.Feature = shieldFocusFeature;
            shieldFocus.Group = Prerequisite.GroupType.All;
            // The fact's own logic: its turn-off (feat removal, respec, unit teardown, and the native save path's TurnOff
            // before serialization) releases the mount contribution this rider owns.
            var logic = CreateOwned<MountedShieldFeatureLogic>("KMC_MountedShield_FeatureLogic");
            // Checked by the native selection decision itself (BlueprintFeature.MeetsPrerequisites), with the publication's
            // full comparison: a choice is never committed while a required list no longer offers the feat, even between two
            // frame refreshes. Hidden from the prerequisite list; owned features are never re-checked against it (the engine
            // re-validates owned features only for stat prerequisites).
            var available = CreateOwned<MountedShieldAvailabilityPrerequisite>("KMC_MountedShield_Available");
            available.Group = Prerequisite.GroupType.All;
            available.HideInUI = true;
            mountedShieldSelectable = () => MountedShieldServiceActive?.Invoke() == true && IsMountedShieldAvailable();
            MountedShieldAvailabilityPrerequisite.Available = mountedShieldSelectable;
            mountedShieldFeature.ComponentsArray = new BlueprintComponent[] { mountedCombat, shieldFocus, available, logic };

            AddExactBlueprintRegistration(library, mountedShieldFeature);
            var violation = MountedShieldContractViolation(mountedShieldFeature, powerAttack, mountedShieldIcon);
            if (violation != null) throw new InvalidOperationException(violation);
            logger.Info("Mounted Shield feat contract: " + ReadFeatContractFacts(mountedShieldFeature, mountedShieldIcon).Describe() +
                "; donor PowerAttackFeature " + ReadFeatContractFacts(powerAttack, mountedShieldIcon).Describe() +
                "; prerequisites " + mountedCombatFeature.name + "+" + shieldFocusFeature.name + " (All); focus " + shieldFocusFeature.AssetGuid + "/" +
                greaterShieldFocusFeature.AssetGuid + "; icon " + MountedShieldIconResource + " " + mountedShieldIcon.name + " " +
                mountedShieldIcon.texture.width + "x" + mountedShieldIcon.texture.height + ".");
        }

        // The same donor-shaped contract as Mounted Archery (MountedArcheryFeatContract): one visible rank, the Feat and
        // CombatFeat groups and the stock combat feat's own flags, including IsClassFeature. Kept separate from the library so
        // scripts/Test-MountedShieldNativeContract.ps1 can run it on genuine blueprints outside the game.
        internal static void BuildMountedShieldContract(BlueprintFeature donor, BlueprintFeature feature,
            Kingmaker.Localization.LocalizedString name, Kingmaker.Localization.LocalizedString description, UnityEngine.Sprite icon)
        {
            CopyFeatureContract(donor, feature);
            SetFeaturePresentation(feature, name, description, icon);
        }

        internal static string MountedShieldContractViolation(BlueprintFeature feature, BlueprintFeature donor, UnityEngine.Sprite icon) =>
            MountedShieldFeatContract.Violation(ReadFeatContractFacts(feature, icon), ReadFeatContractFacts(donor, icon));

        // The selection check stops naming this service (it would keep a disposed service and its leases reachable); a
        // check another registration owns is left alone.
        private void ReleaseMountedShieldSelectable()
        {
            if (mountedShieldSelectable != null && ReferenceEquals(MountedShieldAvailabilityPrerequisite.Available, mountedShieldSelectable))
                MountedShieldAvailabilityPrerequisite.Available = null;
            mountedShieldSelectable = null;
        }

        private void RemoveMountedShieldFeat(LibraryScriptableObject library)
        {
            RemoveExact(library, blueprintList, MountedShieldFeatureGuid, mountedShieldFeature);
            ReleaseMountedShieldSelectable();
            mountedShieldFeature = null;
            mountedShieldIcon = null;
            shieldFocusFeature = null;
            greaterShieldFocusFeature = null;
        }
    }

    // Mounted Shield is selectable only while its rule is available (registered, published and offered by every required
    // list). Unset (null) it refuses: no registered service, no selectable feat.
    internal sealed class MountedShieldAvailabilityPrerequisite : Prerequisite
    {
        internal static Func<bool> Available;

        public override bool Check(Kingmaker.UnitLogic.Class.LevelUp.FeatureSelectionState selectionState, Kingmaker.UnitLogic.UnitDescriptor unit,
            Kingmaker.UnitLogic.Class.LevelUp.LevelUpState state) => Available?.Invoke() == true;

        public override string GetUIText() => string.Empty;
    }
}
