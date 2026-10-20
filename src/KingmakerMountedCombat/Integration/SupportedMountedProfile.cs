using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class SupportedMountedProfile
    {
        public SupportedMountedProfile(string id, string blueprintGuid, string displayName, string sourceAnchorName,
            PoseVector3 sourceAnchorOffset, PoseVector3 mountRootPositionOffset, PoseVector3 riderEulerOffset,
            MountedRiderPoseProfile riderPoseProfile, bool usesDiagnosticMammothOffsets)
            : this(id, blueprintGuid, displayName, sourceAnchorName, sourceAnchorOffset, mountRootPositionOffset,
                riderEulerOffset, riderPoseProfile, usesDiagnosticMammothOffsets,
                usesDiagnosticMammothOffsets ? new PoseVector3(0f, 0f, 0f) : MountedRiderPoseProfiles.HorseAnimatedSeatCorrection,
                !usesDiagnosticMammothOffsets, null) { }

        private SupportedMountedProfile(string id, string blueprintGuid, string displayName, string sourceAnchorName,
            PoseVector3 sourceAnchorOffset, PoseVector3 mountRootPositionOffset, PoseVector3 riderEulerOffset,
            MountedRiderPoseProfile riderPoseProfile, bool usesDiagnosticMammothOffsets,
            PoseVector3 animatedSeatCorrection, bool usesAnimatedSaddle, DeclaredSmallRiderBody smallRiderBody)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            BlueprintGuid = blueprintGuid ?? throw new ArgumentNullException(nameof(blueprintGuid));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            SourceAnchorName = sourceAnchorName ?? throw new ArgumentNullException(nameof(sourceAnchorName));
            SourceAnchorOffset = sourceAnchorOffset; MountRootPositionOffset = mountRootPositionOffset;
            RiderEulerOffset = riderEulerOffset;
            RiderPoseProfile = riderPoseProfile ?? throw new ArgumentNullException(nameof(riderPoseProfile));
            UsesDiagnosticMammothOffsets = usesDiagnosticMammothOffsets;
            AnimatedSeatCorrection = animatedSeatCorrection; UsesAnimatedSaddle = usesAnimatedSaddle;
            SmallRiderBody = smallRiderBody;
        }
        public string Id { get; }
        public string BlueprintGuid { get; }
        public string DisplayName { get; }
        public string SourceAnchorName { get; }
        public PoseVector3 SourceAnchorOffset { get; }
        public PoseVector3 MountRootPositionOffset { get; }
        public PoseVector3 RiderEulerOffset { get; }
        public MountedRiderPoseProfile RiderPoseProfile { get; }
        public bool UsesDiagnosticMammothOffsets { get; }
        public PoseVector3 AnimatedSeatCorrection { get; }
        public bool UsesAnimatedSaddle { get; }
        public DeclaredSmallRiderBody SmallRiderBody { get; }

        internal static SupportedMountedProfile FromDeclaredSmall(DeclaredSmallMountedProfile declared)
        {
            if (declared == null) throw new ArgumentNullException(nameof(declared));
            // Horse anchor identity is native mount geometry. Every rider pose,
            // mechanics-root translation and animated seat correction is an
            // explicit independent Small calibration, supplied by declaration.
            return new SupportedMountedProfile(declared.Body.ProfileId, SupportedMountedProfiles.HorseBlueprintGuid,
                "Horse", "Chest", new PoseVector3(0f, 0f, 0f), declared.MountRootOffset,
                declared.RiderEulerOffset, declared.Pose, false, declared.AnimatedSeatCorrection, true, declared.Body);
        }
    }

    internal static class SupportedMountedProfiles
    {
        internal const string MammothBlueprintGuid = "e7aa96d15a45238438ae4cfb476f6bb9";
        internal const string HorseBlueprintGuid = "4016c7db400ab721ff125aef9e65e202";
        internal static readonly SupportedMountedProfile Mammoth = new SupportedMountedProfile(
            "medium-humanoid-mammoth-v1", MammothBlueprintGuid, "Mammoth", "Spine", new PoseVector3(0f, 0f, 0f),
            MountedRiderPoseProfiles.MediumHumanoidOnMammothMountRootPositionOffset, new PoseVector3(0f, 0f, 0f),
            MountedRiderPoseProfiles.MediumHumanoidOnMammoth, true);
        internal static readonly SupportedMountedProfile Horse = new SupportedMountedProfile(
            "medium-humanoid-horse-v1", HorseBlueprintGuid, "Horse", "Chest", new PoseVector3(0f, 0f, 0f),
            MountedRiderPoseProfiles.MediumHumanoidOnHorseMountRootPositionOffset, new PoseVector3(0f, 0f, 0f),
            MountedRiderPoseProfiles.MediumHumanoidOnHorse, false);
        private static readonly SupportedMountedProfile[] small = MountedRiderProfileCatalog.Small
            .Select(SupportedMountedProfile.FromDeclaredSmall).ToArray();
        private static readonly SupportedMountedProfile[] privateCalibrationSmall =
            MountedRiderProfileCatalog.PrivateCalibrationSmall.Select(SupportedMountedProfile.FromDeclaredSmall).ToArray();
        private static readonly PrivateSmallCalibrationGate privateCalibrationGate =
            new PrivateSmallCalibrationGate(MountedRiderProfileCatalog.PrivateCalibrationSmall);
        internal static IDisposable AcquirePrivateSmallCalibration(DeclaredSmallMountedProfile declaration,
            UnitEntityData rider, UnitEntityData mount, Func<bool> currentScope)
        {
            if (rider == null || mount == null) throw new ArgumentNullException(rider == null ? nameof(rider) : nameof(mount));
            return privateCalibrationGate.Acquire(declaration, rider.UniqueId, mount.UniqueId, rider, mount, currentScope);
        }
        internal static SupportedMountedProfile ResolveProfileId(string profileId)
        {
            if (profileId == Mammoth.Id) return Mammoth;
            if (profileId == Horse.Id) return Horse;
            var qualified = small.SingleOrDefault(profile => profile.Id == profileId);
            var privateDeclaration = privateCalibrationGate.ResolveProfileId(profileId);
            return qualified ?? (privateDeclaration == null ? null :
                privateCalibrationSmall.Single(profile => profile.Id == privateDeclaration.Body.ProfileId));
        }

        // Mount-only resolution remains for names and exact supported mount
        // blueprint classification. It is not relationship/profile admission.
        internal static SupportedMountedProfile Resolve(UnitEntityData mount) => Resolve(mount?.Blueprint?.AssetGuid);
        internal static SupportedMountedProfile Resolve(string blueprintGuid)
        {
            if (blueprintGuid == MammothBlueprintGuid) return Mammoth;
            if (blueprintGuid == HorseBlueprintGuid) return Horse;
            return null;
        }

        internal static SupportedMountedProfile Resolve(UnitEntityData rider, UnitEntityData mount)
        {
            var mountProfile = Resolve(mount);
            if (rider?.Descriptor?.State == null || mount?.Descriptor?.State == null || mountProfile == null ||
                rider.GetActivePolymorph() != null || mount.GetActivePolymorph() != null ||
                rider.Descriptor.Body?.IsPolymorphed != false || mount.Descriptor.Body?.IsPolymorphed != false) return null;
            if (rider.Descriptor.OriginalSize == Size.Medium && rider.Descriptor.State.Size == Size.Medium)
                return mountProfile;
            var privateDeclaration = privateCalibrationGate.Resolve(rider.UniqueId, mount.UniqueId, rider, mount);
            if (small.Length == 0 && privateDeclaration == null || rider.Descriptor.OriginalSize != Size.Small || rider.Descriptor.State.Size != Size.Small ||
                mountProfile.BlueprintGuid != HorseBlueprintGuid) return null;
            var observed = NativeSmallRiderBody.Capture(rider, mount);
            SupportedMountedProfile result = null;
            foreach (var profile in small)
            {
                if (profile.SmallRiderBody.Refusal(observed) != null) continue;
                if (result != null) return null; // No ambiguous declared body/profile owner.
                result = profile;
            }
            if (privateDeclaration != null && privateDeclaration.Body.Refusal(observed) == null)
            {
                if (result != null) return null;
                result = privateCalibrationSmall.Single(profile => profile.Id == privateDeclaration.Body.ProfileId);
            }
            return result;
        }

        internal static SmallRiderBodyMatch MatchSmall(UnitEntityData rider, UnitEntityData mount, SupportedMountedProfile profile)
        {
            string refusal;
            return profile?.SmallRiderBody?.Match(NativeSmallRiderBody.Capture(rider, mount), out refusal);
        }
        internal static bool IsSupported(UnitEntityData unit) => Resolve(unit) != null;
        internal static bool IsSupportedPair(UnitEntityData rider, UnitEntityData mount) => Resolve(rider, mount) != null;
    }
}
