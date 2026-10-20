using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Domain
{
    // No admitted production declaration is provided by this staged source.
    // A reviewed native inventory and measured pose must supply one concrete
    // body/equipment combination before a caller can create a declaration.
    public sealed class SmallRiderBodyObservation
    {
        public int OriginalSizeOrdinal { get; set; }
        public int EffectiveSizeOrdinal { get; set; }
        public bool RiderPolymorphed { get; set; }
        public bool MountPolymorphed { get; set; }
        public string MountBlueprintGuid { get; set; }
        public int MountSizeOrdinal { get; set; }
        public string RaceGuid { get; set; }
        public string PresetGuid { get; set; }
        public string Gender { get; set; }
        public string SkeletonName { get; set; }
        public bool LeftHanded { get; set; }
        public bool AvatarMirrored { get; set; }
        public bool NativeRaceAndPresetIdentityExact { get; set; }
        public bool NativeSkeletonIsExactPreset { get; set; }
        public bool NativeRigAvailable { get; set; }
        public PoseVector3 AvatarLocalScale { get; set; }
        public string[] DollEquipmentEntityIds { get; set; }
        // Native UnitBody.CurrentEquipmentSlots has exactly twelve entries in
        // the installed build, including explicit empty slots in this order:
        // active primary/secondary, armor, belt, head, feet, gloves, neck,
        // ring1/ring2, wrist and shoulders. Inventory/belt consumables are not
        // visual body scope; active hands and every worn slot are exact pins.
        public string[] CurrentEquipmentBlueprints { get; set; }
        public string[] ActiveVisualEquipmentNames { get; set; }
        public int AdditionalLimbCount { get; set; }
    }

    public sealed class SmallRiderBodyMatch
    {
        internal SmallRiderBodyMatch(string profileId) { ProfileId = profileId; }
        public string ProfileId { get; }
    }

    public sealed class DeclaredSmallRiderBody
    {
        public const int NativeSmallOrdinal = 3;
        public const int NativeHorseOrdinal = 5;
        public const string HorseBlueprintGuid = "4016c7db400ab721ff125aef9e65e202";
        private readonly string[] dollEquipment, currentEquipment, visualEquipment;

        public DeclaredSmallRiderBody(string profileId, string nativeInventorySha256, string raceGuid,
            string presetGuid, string gender, string skeletonName, bool leftHanded, bool avatarMirrored,
            PoseVector3 avatarLocalScale, IEnumerable<string> dollEquipmentEntityIds,
            IEnumerable<string> currentEquipmentBlueprints, IEnumerable<string> activeVisualEquipmentNames)
        {
            Require(Regex.IsMatch(profileId ?? "", "^small-[a-z0-9]+(?:-[a-z0-9]+)*-horse-v[1-9][0-9]*$"), "Small profile needs its own versioned Horse identity.");
            Require(Hex(nativeInventorySha256, 64), "Exact reviewed native body inventory hash is required.");
            Require(Hex(raceGuid, 32) && Hex(presetGuid, 32), "Exact Small race and preset identities are required.");
            Require(gender == "Male" || gender == "Female", "Unknown native gender.");
            Require(!string.IsNullOrWhiteSpace(skeletonName) && skeletonName.Length <= 160, "Exact native skeleton name is required.");
            Require(avatarLocalScale.IsFinite && Math.Abs(avatarLocalScale.X) >= 0.01f && Math.Abs(avatarLocalScale.Y) >= 0.01f &&
                Math.Abs(avatarLocalScale.Z) >= 0.01f && Math.Abs(avatarLocalScale.X) <= 10f && Math.Abs(avatarLocalScale.Y) <= 10f &&
                Math.Abs(avatarLocalScale.Z) <= 10f, "Native avatar scale pin is invalid.");
            dollEquipment = CopyBounded(dollEquipmentEntityIds, 128, false);
            Require(dollEquipment.All(value => Hex(value, 32)) && dollEquipment.Distinct(StringComparer.Ordinal).Count() == dollEquipment.Length,
                "Native Doll equipment IDs must be exact and unambiguous.");
            currentEquipment = CopyBounded(currentEquipmentBlueprints, 12, true);
            Require(currentEquipment.Length == 12 && currentEquipment.All(value => value == null || Hex(value, 32)),
                "All twelve active native equipment slots require an explicit exact blueprint or empty value.");
            visualEquipment = CopyBounded(activeVisualEquipmentNames, 128, false);
            Require(visualEquipment.Length != 0 && visualEquipment.All(value => !string.IsNullOrWhiteSpace(value) && value.Length <= 160),
                "The exact native active visual-equipment multiset is required.");
            Array.Sort(dollEquipment, StringComparer.Ordinal); Array.Sort(visualEquipment, StringComparer.Ordinal);
            ProfileId = profileId; NativeInventorySha256 = nativeInventorySha256; RaceGuid = raceGuid; PresetGuid = presetGuid;
            Gender = gender; SkeletonName = skeletonName; LeftHanded = leftHanded; AvatarMirrored = avatarMirrored;
            AvatarLocalScale = avatarLocalScale;
        }

        public string ProfileId { get; }
        public string NativeInventorySha256 { get; }
        public string RaceGuid { get; }
        public string PresetGuid { get; }
        public string Gender { get; }
        public string SkeletonName { get; }
        public bool LeftHanded { get; }
        public bool AvatarMirrored { get; }
        public PoseVector3 AvatarLocalScale { get; }

        public SmallRiderBodyMatch Match(SmallRiderBodyObservation current, out string refusal)
        {
            refusal = Refusal(current);
            return refusal == null ? new SmallRiderBodyMatch(ProfileId) : null;
        }

        public string Refusal(SmallRiderBodyObservation current)
        {
            if (current == null) return "The exact native Small body is unavailable.";
            if (current.OriginalSizeOrdinal != NativeSmallOrdinal || current.EffectiveSizeOrdinal != NativeSmallOrdinal)
                return "The declared Small profile requires original and effective native Small size.";
            if (current.RiderPolymorphed || current.MountPolymorphed)
                return "Polymorphed rider or mount is outside the declared Small profile.";
            if (current.MountBlueprintGuid != HorseBlueprintGuid || current.MountSizeOrdinal != NativeHorseOrdinal)
                return "The declared Small profile requires the exact native Large Horse companion.";
            if (current.RaceGuid != RaceGuid || current.PresetGuid != PresetGuid || current.Gender != Gender ||
                !current.NativeRaceAndPresetIdentityExact)
                return "The native Small race, preset or sex differs from the declared body.";
            if (current.SkeletonName != SkeletonName || !current.NativeSkeletonIsExactPreset || !current.NativeRigAvailable)
                return "The live native skeleton is not the exact declared preset rig.";
            if (current.LeftHanded != LeftHanded || current.AvatarMirrored != AvatarMirrored || !SameScale(current.AvatarLocalScale, AvatarLocalScale))
                return "Native handedness, mirroring or body scale differs from the declared profile.";
            if (!ExactSet(current.DollEquipmentEntityIds, dollEquipment, false))
                return "Native Doll equipment differs from the declared Small body.";
            if (current.CurrentEquipmentBlueprints == null || !current.CurrentEquipmentBlueprints.SequenceEqual(currentEquipment, StringComparer.Ordinal))
                return "Active hand or worn equipment is outside the declared Small combination.";
            if (!ExactSet(current.ActiveVisualEquipmentNames, visualEquipment, true) || current.AdditionalLimbCount != 0)
                return "The native visual equipment or extra limbs differ from the declared Small body.";
            return null;
        }

        private static bool ExactSet(string[] actual, string[] expected, bool permitDuplicateNames)
        {
            if (actual == null || actual.Length != expected.Length || actual.Length > 128 || actual.Any(string.IsNullOrWhiteSpace)) return false;
            if (!permitDuplicateNames && actual.Distinct(StringComparer.Ordinal).Count() != actual.Length) return false;
            return actual.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(expected, StringComparer.Ordinal);
        }
        private static bool SameScale(PoseVector3 left, PoseVector3 right) => left.IsFinite &&
            Math.Abs(left.X - right.X) <= 0.0001f && Math.Abs(left.Y - right.Y) <= 0.0001f && Math.Abs(left.Z - right.Z) <= 0.0001f;
        private static string[] CopyBounded(IEnumerable<string> values, int bound, bool permitNull)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var result = values.Take(bound + 1).ToArray();
            Require(result.Length <= bound && (permitNull || result.All(value => value != null)), "Native equipment declaration exceeds its bounded shape.");
            return result;
        }
        private static bool Hex(string value, int length) => value != null && value.Length == length && value.All(character =>
            character >= '0' && character <= '9' || character >= 'a' && character <= 'f');
        private static void Require(bool condition, string message)
        { if (!condition) throw new ArgumentException(message); }
    }

    // Declaration is impossible without explicit independent pose/calibration
    // values. No Medium parameter is selected implicitly for a Small body.
    public sealed class DeclaredSmallMountedProfile
    {
        public DeclaredSmallMountedProfile(DeclaredSmallRiderBody body, MountedRiderPoseProfile pose,
            string nativeCalibrationSha256, PoseVector3 mountRootOffset, PoseVector3 animatedSeatCorrection,
            PoseVector3 riderEulerOffset)
        {
            if (body == null || pose == null) throw new ArgumentNullException(body == null ? nameof(body) : nameof(pose));
            if (body.ProfileId != pose.Id || pose.Validate() != null ||
                ReferenceEquals(pose, MountedRiderPoseProfiles.MediumHumanoidOnHorse) ||
                ReferenceEquals(pose, MountedRiderPoseProfiles.MediumHumanoidOnMammoth))
                throw new ArgumentException("Small body requires its own exact validated pose identity.");
            if (!Regex.IsMatch(nativeCalibrationSha256 ?? "", "^[a-f0-9]{64}$") || !mountRootOffset.IsFinite ||
                !animatedSeatCorrection.IsFinite || !riderEulerOffset.IsFinite || mountRootOffset.Magnitude > 2f ||
                animatedSeatCorrection.Magnitude > 2f || riderEulerOffset.Magnitude > 360f)
                throw new ArgumentException("Explicit bounded native Small calibration is required.");
            Body = body; Pose = pose; NativeCalibrationSha256 = nativeCalibrationSha256; MountRootOffset = mountRootOffset;
            AnimatedSeatCorrection = animatedSeatCorrection; RiderEulerOffset = riderEulerOffset;
        }
        public DeclaredSmallRiderBody Body { get; }
        public MountedRiderPoseProfile Pose { get; }
        public string NativeCalibrationSha256 { get; }
        public PoseVector3 MountRootOffset { get; }
        public PoseVector3 AnimatedSeatCorrection { get; }
        public PoseVector3 RiderEulerOffset { get; }
    }
}
