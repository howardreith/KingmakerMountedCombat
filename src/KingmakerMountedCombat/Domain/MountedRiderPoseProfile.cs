using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public sealed class MountedRiderLegPoseProfile
    {
        public MountedRiderLegPoseProfile(
            string thighBoneName,
            string lowerLegBoneName,
            string footBoneName,
            PoseVector3 footTargetFromThigh,
            PoseVector3 kneeHintFromThigh,
            PoseVector3 footEulerOffset)
        {
            ThighBoneName = thighBoneName;
            LowerLegBoneName = lowerLegBoneName;
            FootBoneName = footBoneName;
            FootTargetFromThigh = footTargetFromThigh;
            KneeHintFromThigh = kneeHintFromThigh;
            FootEulerOffset = footEulerOffset;
        }

        public string ThighBoneName { get; }

        public string LowerLegBoneName { get; }

        public string FootBoneName { get; }

        public PoseVector3 FootTargetFromThigh { get; }

        public PoseVector3 KneeHintFromThigh { get; }

        public PoseVector3 FootEulerOffset { get; }

        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(ThighBoneName) || string.IsNullOrWhiteSpace(LowerLegBoneName) || string.IsNullOrWhiteSpace(FootBoneName))
            {
                return "Every leg pose bone name is required.";
            }
            if (string.Equals(ThighBoneName, LowerLegBoneName, StringComparison.Ordinal) ||
                string.Equals(ThighBoneName, FootBoneName, StringComparison.Ordinal) ||
                string.Equals(LowerLegBoneName, FootBoneName, StringComparison.Ordinal))
            {
                return "A leg pose cannot reuse one transform for multiple joints.";
            }
            if (!FootTargetFromThigh.IsFinite || !KneeHintFromThigh.IsFinite || !FootEulerOffset.IsFinite)
            {
                return "Leg pose values must be finite.";
            }
            if (FootTargetFromThigh.Magnitude < 0.05f || FootTargetFromThigh.Magnitude > 2f ||
                KneeHintFromThigh.Magnitude > 2f || FootEulerOffset.Magnitude > 360f)
            {
                return "Leg pose targets exceed the bounded Medium-humanoid profile envelope.";
            }
            return null;
        }
    }

    public sealed class MountedRiderPoseProfile
    {
        public MountedRiderPoseProfile(
            string id,
            string pelvisBoneName,
            PoseVector3 pelvisPositionOffset,
            PoseVector3 pelvisEulerOffset,
            MountedRiderLegPoseProfile leftLeg,
            MountedRiderLegPoseProfile rightLeg)
        {
            Id = id;
            PelvisBoneName = pelvisBoneName;
            PelvisPositionOffset = pelvisPositionOffset;
            PelvisEulerOffset = pelvisEulerOffset;
            LeftLeg = leftLeg;
            RightLeg = rightLeg;
        }

        public string Id { get; }

        public string PelvisBoneName { get; }

        public PoseVector3 PelvisPositionOffset { get; }

        public PoseVector3 PelvisEulerOffset { get; }

        public MountedRiderLegPoseProfile LeftLeg { get; }

        public MountedRiderLegPoseProfile RightLeg { get; }

        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(PelvisBoneName))
            {
                return "Pose profile identity and pelvis bone are required.";
            }
            if (!PelvisPositionOffset.IsFinite || !PelvisEulerOffset.IsFinite ||
                PelvisPositionOffset.Magnitude > 0.5f || PelvisEulerOffset.Magnitude > 360f)
            {
                return "Pelvis pose values exceed the bounded Medium-humanoid profile envelope.";
            }
            if (LeftLeg == null || RightLeg == null)
            {
                return "Both typed leg profiles are required.";
            }
            var error = LeftLeg.Validate() ?? RightLeg.Validate();
            if (error != null)
            {
                return error;
            }

            var names = new HashSet<string>(StringComparer.Ordinal) { PelvisBoneName };
            foreach (var name in EnumerateLegBoneNames(LeftLeg))
            {
                if (!names.Add(name)) { return "Pose profile bone names must be unique."; }
            }
            foreach (var name in EnumerateLegBoneNames(RightLeg))
            {
                if (!names.Add(name)) { return "Pose profile bone names must be unique."; }
            }
            return null;
        }

        private static IEnumerable<string> EnumerateLegBoneNames(MountedRiderLegPoseProfile leg)
        {
            yield return leg.ThighBoneName;
            yield return leg.LowerLegBoneName;
            yield return leg.FootBoneName;
        }
    }

    public static class MountedRiderPoseProfiles
    {
        public static readonly MountedRiderPoseProfile MediumHumanoidOnMammoth = new MountedRiderPoseProfile(
            "medium-humanoid-mammoth-v1",
            "Pelvis",
            new PoseVector3(0f, 0.04f, -0.05f),
            new PoseVector3(8f, 0f, 0f),
            new MountedRiderLegPoseProfile(
                "L_Up_leg",
                "L_leg",
                "L_foot",
                new PoseVector3(-0.32f, -0.50f, 0.10f),
                new PoseVector3(-0.42f, -0.08f, 0.42f),
                new PoseVector3(0f, 0f, 0f)),
            new MountedRiderLegPoseProfile(
                "R_Up_leg",
                "R_leg",
                "R_foot",
                new PoseVector3(0.32f, -0.50f, 0.10f),
                new PoseVector3(0.42f, -0.08f, 0.42f),
                new PoseVector3(0f, 0f, 0f)));
    }
}
