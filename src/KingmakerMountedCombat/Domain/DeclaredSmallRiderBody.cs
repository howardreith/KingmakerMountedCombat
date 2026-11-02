using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Domain
{
    // Sprint 5: an ordinary Small declaration names one native rig family (race,
    // sex, skeleton) on one mount rig. Armor, headgear, weapons and cosmetics do
    // not change the rig, so they do not participate. The exact-costume pins of the
    // first calibration stay available to its historical diagnostics only.
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
        private const string ProfileIdPattern = "^small-[a-z0-9]+(?:-[a-z0-9]+)*-(?:horse|ridingdog)-v[1-9][0-9]*$";
        private readonly string[] dollEquipment, currentEquipment, visualEquipment;

        // The ordinary rig-family declaration.
        public DeclaredSmallRiderBody(string profileId, string mountBlueprintGuid, string raceGuid, string gender, string skeletonName)
        {
            Require(Regex.IsMatch(profileId ?? "", ProfileIdPattern), "Small profile needs its own versioned mount identity.");
            Require(Hex(mountBlueprintGuid, 32) && Hex(raceGuid, 32), "Exact mount and Small race identities are required.");
            Require(gender == "Male" || gender == "Female", "Unknown native gender.");
            Require(!string.IsNullOrWhiteSpace(skeletonName) && skeletonName.Length <= 160, "Exact native skeleton name is required.");
            ProfileId = profileId; MountBlueprintGuid = mountBlueprintGuid; RaceGuid = raceGuid; Gender = gender; SkeletonName = skeletonName;
            AvatarLocalScale = new PoseVector3(1f, 1f, 1f);
        }

        public DeclaredSmallRiderBody(string profileId, string nativeInventorySha256, string raceGuid,
            string presetGuid, string gender, string skeletonName, bool leftHanded, bool avatarMirrored,
            PoseVector3 avatarLocalScale, IEnumerable<string> dollEquipmentEntityIds,
            IEnumerable<string> currentEquipmentBlueprints, IEnumerable<string> activeVisualEquipmentNames)
        {
            Require(Regex.IsMatch(profileId ?? "", ProfileIdPattern), "Small profile needs its own versioned mount identity.");
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
            ProfileId = profileId; MountBlueprintGuid = HorseBlueprintGuid; NativeInventorySha256 = nativeInventorySha256; RaceGuid = raceGuid; PresetGuid = presetGuid;
            Gender = gender; SkeletonName = skeletonName; LeftHanded = leftHanded; AvatarMirrored = avatarMirrored;
            AvatarLocalScale = avatarLocalScale;
        }

        public string ProfileId { get; }
        public string MountBlueprintGuid { get; }
        public bool HasExactFixturePins => dollEquipment != null;
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

        // Ordinary admission: the measured rig family on a larger declared mount. A missing
        // native view is a lifecycle wait for the caller, never a change of the saved identity.
        public string Refusal(SmallRiderBodyObservation current)
        {
            if (current == null) return "The native Small body is unavailable.";
            if (current.OriginalSizeOrdinal != NativeSmallOrdinal || current.EffectiveSizeOrdinal != NativeSmallOrdinal)
                return "A supported Small rider must currently be its native Small size.";
            if (current.RiderPolymorphed || current.MountPolymorphed)
                return "A polymorphed rider or mount has no supported seat.";
            if (current.MountBlueprintGuid != MountBlueprintGuid)
                return "This Small rig has no measured seat on this mount.";
            var size = MountedSizePolicy.Refusal(current.EffectiveSizeOrdinal, current.MountSizeOrdinal);
            if (size != null) return size;
            if (current.RaceGuid != RaceGuid || current.Gender != Gender)
                return "The rider's native race or sex has a different supported rig.";
            if (current.SkeletonName != SkeletonName || !current.NativeRigAvailable)
                return "The rider's live native skeleton is not the declared rig.";
            if (!SameScale(Unmirrored(current.AvatarLocalScale), AvatarLocalScale))
                return "The rider's native body scale differs from the measured rig.";
            return null;
        }

        // The first calibration's exact costume, for the historical F09 diagnostics. Never ordinary admission.
        public string ExactFixtureRefusal(SmallRiderBodyObservation current)
        {
            if (!HasExactFixturePins) return "This rig-family declaration has no exact fixture pins.";
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
            if (!ExactOrdered(current.CurrentEquipmentBlueprints, currentEquipment))
                return "Active hand or worn equipment is outside the declared Small combination.";
            if (!ExactSet(current.ActiveVisualEquipmentNames, visualEquipment, true) || current.AdditionalLimbCount != 0)
                return "The native visual equipment or extra limbs differ from the declared Small body.";
            return null;
        }

        private static PoseVector3 Unmirrored(PoseVector3 scale) => new PoseVector3(Math.Abs(scale.X), scale.Y, scale.Z);
        private static bool ExactSet(string[] actual, string[] expected, bool permitDuplicateNames)
        {
            if (actual == null || actual.Length != expected.Length || actual.Length > 128) return false;
            for (var index = 0; index < actual.Length; index++)
                if (string.IsNullOrWhiteSpace(actual[index])) return false;
            // The declaration stores an ordinal-sorted array and, for Doll IDs,
            // already rejects duplicates. Count each distinct expected value in
            // the bounded current array instead of sorting/copying it per frame.
            for (var index = 0; index < expected.Length;)
            {
                var value = expected[index];
                var end = index + 1;
                while (end < expected.Length && string.Equals(expected[end], value, StringComparison.Ordinal)) end++;
                if (!permitDuplicateNames && end != index + 1) return false;
                var found = 0;
                for (var current = 0; current < actual.Length; current++)
                    if (string.Equals(actual[current], value, StringComparison.Ordinal)) found++;
                if (found != end - index) return false;
                index = end;
            }
            return true;
        }
        private static bool ExactOrdered(string[] actual, string[] expected)
        {
            if (actual == null || actual.Length != expected.Length) return false;
            for (var index = 0; index < actual.Length; index++)
                if (!string.Equals(actual[index], expected[index], StringComparison.Ordinal)) return false;
            return true;
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

    // Native size categories decide who can carry whom; render scale never does.
    public static class MountedSizePolicy
    {
        public const string TooSmallRefusal = "The mount must be at least one size category larger than its rider.";
        public static string Refusal(int riderEffectiveSizeOrdinal, int mountEffectiveSizeOrdinal) =>
            mountEffectiveSizeOrdinal > riderEffectiveSizeOrdinal ? null : TooSmallRefusal;
    }

    // Declaration is impossible without explicit independent pose/calibration
    // values. No Medium parameter is selected implicitly for a Small body.
    public sealed class DeclaredSmallMountedProfile
    {
        public DeclaredSmallMountedProfile(DeclaredSmallRiderBody body, MountedRiderPoseProfile pose,
            string nativeCalibrationSha256, PoseVector3 mountRootOffset, PoseVector3 animatedSeatCorrection,
            PoseVector3 riderEulerOffset, string mountDisplayName = "Horse", string mountAnchorName = "Chest")
        {
            if (string.IsNullOrWhiteSpace(mountDisplayName) || string.IsNullOrWhiteSpace(mountAnchorName))
                throw new ArgumentException("The mount rig's name and seat anchor are required.");
            MountDisplayName = mountDisplayName; MountAnchorName = mountAnchorName;
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
        public string MountDisplayName { get; }
        public string MountAnchorName { get; }
        public MountedRiderPoseProfile Pose { get; }
        public string NativeCalibrationSha256 { get; }
        public PoseVector3 MountRootOffset { get; }
        public PoseVector3 AnimatedSeatCorrection { get; }
        public PoseVector3 RiderEulerOffset { get; }
    }
}
