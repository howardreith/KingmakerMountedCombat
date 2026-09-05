using System;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class SupportedMountedProfile
    {
        public SupportedMountedProfile(
            string id,
            string blueprintGuid,
            string displayName,
            string sourceAnchorName,
            PoseVector3 sourceAnchorOffset,
            PoseVector3 mountRootPositionOffset,
            PoseVector3 riderEulerOffset,
            MountedRiderPoseProfile riderPoseProfile,
            bool usesDiagnosticMammothOffsets)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            BlueprintGuid = blueprintGuid ?? throw new ArgumentNullException(nameof(blueprintGuid));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            SourceAnchorName = sourceAnchorName ?? throw new ArgumentNullException(nameof(sourceAnchorName));
            SourceAnchorOffset = sourceAnchorOffset;
            MountRootPositionOffset = mountRootPositionOffset;
            RiderEulerOffset = riderEulerOffset;
            RiderPoseProfile = riderPoseProfile ?? throw new ArgumentNullException(nameof(riderPoseProfile));
            UsesDiagnosticMammothOffsets = usesDiagnosticMammothOffsets;
        }

        public string Id { get; }

        public string BlueprintGuid { get; }

        public string DisplayName { get; }

        public string SourceAnchorName { get; }

        public PoseVector3 SourceAnchorOffset { get; }

        /// <summary>
        /// A stable mount-root-local translation applied after resolving the
        /// animated source anchor. This remains zero for the historical
        /// Mammoth profile. Horse seat-height calibration belongs here so a
        /// vertical product decision is not projected through an animated
        /// humanoid pelvis bone's local axes.
        /// </summary>
        public PoseVector3 MountRootPositionOffset { get; }

        public PoseVector3 RiderEulerOffset { get; }

        public MountedRiderPoseProfile RiderPoseProfile { get; }

        public bool UsesDiagnosticMammothOffsets { get; }

        // Horse-only visual correction from the human-reviewed forward seat. Never a mechanics offset.
        // Angular inheritance is deliberately zero: the Chest rest basis is not a rider orientation.
        public PoseVector3 AnimatedSeatCorrection => UsesDiagnosticMammothOffsets
            ? new PoseVector3(0f, 0f, 0f) : new PoseVector3(0f, 0f, -0.18f);
    }

    internal static class SupportedMountedProfiles
    {
        internal const string MammothBlueprintGuid = "e7aa96d15a45238438ae4cfb476f6bb9";
        internal const string HorseBlueprintGuid = "4016c7db400ab721ff125aef9e65e202";

        internal static readonly SupportedMountedProfile Mammoth = new SupportedMountedProfile(
            "medium-humanoid-mammoth-v1",
            MammothBlueprintGuid,
            "Mammoth",
            "Spine",
            new PoseVector3(0f, 0f, 0f),
            MountedRiderPoseProfiles.MediumHumanoidOnMammothMountRootPositionOffset,
            new PoseVector3(0f, 0f, 0f),
            MountedRiderPoseProfiles.MediumHumanoidOnMammoth,
            true);

        internal static readonly SupportedMountedProfile Horse = new SupportedMountedProfile(
            "medium-humanoid-horse-v1",
            HorseBlueprintGuid,
            "Horse",
            "Chest",
            new PoseVector3(0f, 0f, 0f),
            MountedRiderPoseProfiles.MediumHumanoidOnHorseMountRootPositionOffset,
            new PoseVector3(0f, 0f, 0f),
            MountedRiderPoseProfiles.MediumHumanoidOnHorse,
            false);

        internal static SupportedMountedProfile Resolve(UnitEntityData mount)
        {
            return Resolve(mount?.Blueprint?.AssetGuid);
        }

        internal static SupportedMountedProfile Resolve(string blueprintGuid)
        {
            if (string.Equals(blueprintGuid, MammothBlueprintGuid, StringComparison.Ordinal))
            {
                return Mammoth;
            }
            if (string.Equals(blueprintGuid, HorseBlueprintGuid, StringComparison.Ordinal))
            {
                return Horse;
            }
            return null;
        }

        internal static bool IsSupported(UnitEntityData unit)
        {
            return Resolve(unit) != null;
        }
    }
}
