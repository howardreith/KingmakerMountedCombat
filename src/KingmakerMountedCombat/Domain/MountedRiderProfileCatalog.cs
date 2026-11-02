using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    public static class MountedRiderBodySelectionPolicy
    {
        // A native Medium body preserves its historical path. A declared Small
        // match is an independent alternative, never permission for any Small
        // body and never a second simultaneous profile claim.
        public static bool IsSupported(bool exactlyMedium, SmallRiderBodyMatch small) => exactlyMedium != (small != null);
    }

    public static class MountedRiderProfileCatalog
    {
        public const string MediumMammothId = "medium-humanoid-mammoth-v1";
        public const string MediumHorseId = "medium-humanoid-horse-v1";

        public const string OriginalSmallHorseId = "small-default-halfling-horse-v1";
        private const string HalflingRaceGuid = "b0c3ef2729c498f47970bb50fa1acd30";
        private const string GnomeRaceGuid = "ef35a22c9a27da345a4528f0d5889157";
        // The first batch70 declaration's identity now names the whole MaleHalfling
        // rig on the Horse. Keeping the one ID makes existing archives load unchanged
        // and needs no migration. Every other Small rig or mount has its own ID.
        private static readonly DeclaredSmallMountedProfile halflingHorse = InitialHalflingCalibration();
        private static readonly ReadOnlyCollection<DeclaredSmallMountedProfile> small = Array.AsReadOnly(new[]
        {
            halflingHorse,
            HorseFamily("small-halfling-female-horse-v1", HalflingRaceGuid, "Female", "FemaleHalfling", 0.324f, HalflingSaddleHeight),
            HorseFamily("small-gnome-male-horse-v1", GnomeRaceGuid, "Male", "MaleGnome", 0.465f, GnomeSaddleHeight),
            HorseFamily("small-gnome-female-horse-v1", GnomeRaceGuid, "Female", "FemaleGnome", 0.452f, GnomeSaddleHeight),
            DogFamily("small-halfling-male-ridingdog-v1", HalflingRaceGuid, "Male", "MaleHalfling", 0.351f, HalflingDogBackHeight),
            DogFamily("small-halfling-female-ridingdog-v1", HalflingRaceGuid, "Female", "FemaleHalfling", 0.324f, HalflingDogBackHeight),
            DogFamily("small-gnome-male-ridingdog-v1", GnomeRaceGuid, "Male", "MaleGnome", 0.465f, GnomeDogBackHeight),
            DogFamily("small-gnome-female-ridingdog-v1", GnomeRaceGuid, "Female", "FemaleGnome", 0.452f, GnomeDogBackHeight)
        });
        public static IReadOnlyList<DeclaredSmallMountedProfile> Small => small;
        // Retain the exact F09 calibration lease for reproducible diagnostics;
        // it names the same declaration as ordinary admission.
        private static readonly ReadOnlyCollection<DeclaredSmallMountedProfile> privateCalibrationSmall =
            Array.AsReadOnly(new[] { halflingHorse });
        internal static IReadOnlyList<DeclaredSmallMountedProfile> PrivateCalibrationSmall => privateCalibrationSmall;
        private static DeclaredSmallMountedProfile InitialHalflingCalibration()
        {
            const string id = OriginalSmallHorseId;
            var body = new DeclaredSmallRiderBody(id,
                "dcdc6cdce0265a221b2f3d890d34229385098235d2546df4a9c6a399239e48a9",
                "b0c3ef2729c498f47970bb50fa1acd30", "639cebe8cbc098345af429d62fd79578",
                "Male", "MaleHalfling", false, false, new PoseVector3(1f, 1f, 1f),
                new[] { "bb0d12a522c0c294ba3d6558682450e6", "5ab700946d8459c42a26d193eb77624d",
                    "fe9ed93cd7abb9f4ab164f0e7e4676eb", "f283c87e2db77e648a7d4c1ae1b1f79c" },
                new string[] { "201f6150321e09048bd59e9b7f558cb0", null,
                    "afbe88d27a0eb544583e00fa78ffb2c7", null, null, null, null, null, null, null, null, null },
                new[] { "EE_Naked_M_HL", "EE_Head_Face01_M_HL", "EE_Eyebrows_Face01_M_HL",
                    "EE_Hair_HairUncombed_M_HL", "EE_FacialandHair_Empty_U_Any",
                    "EE_Armor_StuddedLeatherlUnpadded_M", "EE_Ranger_M_Cap_HL_Colorize",
                    "Ranger_M_Accessories", "EE_RangerAccesories_M_Any_Colorize",
                    "EE_Ranger_M_Any_Colorize", "EE_Ranger_M_Cape" });
            return new DeclaredSmallMountedProfile(body, SmallPose(id, 0.351f), SeatRecordSha256,
                SmallHorseRootOffset, HorseSeat(HalflingSaddleHeight), new PoseVector3(0f, 0f, 0f));
        }

        // The old seat aimed the feet at the Horse's stirrups. Small legs reach only 0.35 of
        // the 0.75 from saddle to stirrup, so the pelvis sank about 0.59 below the accepted
        // Medium seat, into the Horse (mount-local height 1.63 against 2.22). The seat now
        // comes from the measured saddle (its surface is about 1.82 high at z 0.22, with the
        // pommel over the Chest bone at z 0.47); the legs straddle instead of reaching down.
        // A Small seat is a fixed offset from the mount's anchor bone, so it cannot depend on
        // the animation pose playing at the moment of mounting.
        private const string SeatRecordSha256 = "0000000000000000000000000000000000000000000000000000000000000000";
        private const float HorseChestHeight = 1.505f, HorseChestForward = 0.468f, HorseSaddleForward = 0.22f;
        private const float HalflingSaddleHeight = 1.95f, GnomeSaddleHeight = 1.98f;
        private static readonly PoseVector3 SmallHorseRootOffset = new PoseVector3(0f, -0.16f, 0f);
        private static PoseVector3 HorseSeat(float height) => new PoseVector3(0f, height - HorseChestHeight, HorseSaddleForward - HorseChestForward);
        // Foot and knee are sized by the rig's own thigh-and-shin reach.
        private static MountedRiderPoseProfile SmallPose(string id, float reach) => new MountedRiderPoseProfile(id, "Pelvis",
            new PoseVector3(0f, 0f, 0f), new PoseVector3(0f, 0f, 0f),
            new MountedRiderLegPoseProfile("L_Up_leg", "L_leg", "L_foot",
                new PoseVector3(-0.61f * reach, -0.67f * reach, 0.13f * reach), new PoseVector3(-0.80f * reach, -0.06f * reach, 0.46f * reach), new PoseVector3(0f, 0f, 0f)),
            new MountedRiderLegPoseProfile("R_Up_leg", "R_leg", "R_foot",
                new PoseVector3(0.61f * reach, -0.67f * reach, 0.13f * reach), new PoseVector3(0.80f * reach, -0.06f * reach, 0.46f * reach), new PoseVector3(0f, 0f, 0f)));
        // The Riding Dog has no saddle: its back is about 0.89 high at z 0.12, between hips and shoulders, and
        // 0.22 wide to each side, measured on the Medium dog. The seat is mount-local, so it follows native growth.
        public const string RidingDogBlueprintGuid = "b7a14f25d3c94e0d8e6f1a2c5d90e431";
        private const string RidingDogAnchor = "UpperTorso";
        private const float DogTorsoHeight = 0.77f, DogTorsoForward = 0.08f, DogSeatForward = 0.12f;
        private const float HalflingDogBackHeight = 1.02f, GnomeDogBackHeight = 1.05f;
        private static DeclaredSmallMountedProfile DogFamily(string id, string raceGuid, string gender, string skeleton, float reach, float seatHeight) =>
            new DeclaredSmallMountedProfile(new DeclaredSmallRiderBody(id, RidingDogBlueprintGuid, raceGuid, gender, skeleton),
                SmallPose(id, reach), SeatRecordSha256, new PoseVector3(0f, -0.40f, 0f),
                new PoseVector3(0f, seatHeight - DogTorsoHeight, DogSeatForward - DogTorsoForward), new PoseVector3(0f, 0f, 0f), "Riding Dog", RidingDogAnchor);
        private static DeclaredSmallMountedProfile HorseFamily(string id, string raceGuid, string gender, string skeleton, float reach, float seatHeight) =>
            new DeclaredSmallMountedProfile(new DeclaredSmallRiderBody(id, DeclaredSmallRiderBody.HorseBlueprintGuid, raceGuid, gender, skeleton),
                SmallPose(id, reach), SeatRecordSha256, SmallHorseRootOffset, HorseSeat(seatHeight), new PoseVector3(0f, 0f, 0f));
        internal static bool IsDeclaredSmallProfile(string profileId) =>
            small.Any(profile => string.Equals(profile.Body.ProfileId, profileId, StringComparison.Ordinal));

        // Schema 3 policy only. Historical schema 1/2 validation must keep its
        // existing independent Medium whitelist in MountedSaveData unchanged.
        // A recognized ID is data eligibility, not native restore authority.
        public static bool IsSupportedArchiveProfile(string profileId) =>
            profileId == MediumMammothId || profileId == MediumHorseId ||
            IsDeclaredSmallProfile(profileId);
    }

    // The provisional pose is usable only during one owned native calibration.
    // Ordinary profile and archive admission continue to consult Small above.
    internal sealed class PrivateSmallCalibrationGate
    {
        private readonly IReadOnlyList<DeclaredSmallMountedProfile> allowed;
        private Lease active;
        internal PrivateSmallCalibrationGate(IReadOnlyList<DeclaredSmallMountedProfile> allowed)
        { this.allowed = allowed ?? throw new ArgumentNullException(nameof(allowed)); }
        internal IDisposable Acquire(DeclaredSmallMountedProfile profile, string riderId, string mountId,
            object rider, object mount, Func<bool> currentScope)
        {
            if (active != null || profile == null || !allowed.Any(value => ReferenceEquals(value, profile)) ||
                !CanonicalId(riderId) || !CanonicalId(mountId) || riderId == mountId ||
                rider == null || mount == null || ReferenceEquals(rider, mount) ||
                currentScope == null || !SafeCurrent(currentScope))
                throw new InvalidOperationException("An exact unleased private Small calibration pair is required.");
            active = new Lease(this, profile, riderId, mountId, rider, mount, currentScope);
            return active;
        }
        internal DeclaredSmallMountedProfile Resolve(string riderId, string mountId, object rider, object mount) =>
            active != null && active.RiderId == riderId && active.MountId == mountId &&
                ReferenceEquals(active.Rider, rider) && ReferenceEquals(active.Mount, mount) &&
                SafeCurrent(active.CurrentScope) ? active.Profile : null;
        internal DeclaredSmallMountedProfile ResolveProfileId(string profileId) =>
            active != null && active.Profile.Body.ProfileId == profileId &&
                SafeCurrent(active.CurrentScope) ? active.Profile : null;
        private static bool SafeCurrent(Func<bool> probe)
        { try { return probe(); } catch { return false; } }
        private static bool CanonicalId(string value) => Guid.TryParseExact(value, "D", out var id) &&
            id != Guid.Empty && id.ToString("D") == value;
        private sealed class Lease : IDisposable
        {
            private readonly PrivateSmallCalibrationGate owner;
            internal readonly DeclaredSmallMountedProfile Profile;
            internal readonly string RiderId, MountId;
            internal readonly object Rider, Mount;
            internal readonly Func<bool> CurrentScope;
            internal Lease(PrivateSmallCalibrationGate owner, DeclaredSmallMountedProfile profile, string riderId, string mountId,
                object rider, object mount, Func<bool> currentScope)
            { this.owner = owner; Profile = profile; RiderId = riderId; MountId = mountId; Rider = rider; Mount = mount; CurrentScope = currentScope; }
            public void Dispose()
            {
                if (!ReferenceEquals(owner.active, this))
                    throw new InvalidOperationException("Private Small calibration lease identity changed.");
                owner.active = null;
            }
        }
    }
}
