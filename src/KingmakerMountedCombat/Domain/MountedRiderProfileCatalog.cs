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

        // Deliberately empty. Populate only with one explicitly measured,
        // reviewed native declaration; no guessed race/body/pose constants.
        private static readonly ReadOnlyCollection<DeclaredSmallMountedProfile> small =
            Array.AsReadOnly(Array.Empty<DeclaredSmallMountedProfile>());
        public static IReadOnlyList<DeclaredSmallMountedProfile> Small => small;
        // Initial default MaleHalfling/Horse pose from the pinned batch53 native
        // body and neutral geometry. Its offsets remain a private calibration
        // hypothesis, never ordinary-play or saved-profile admission.
        private static readonly ReadOnlyCollection<DeclaredSmallMountedProfile> privateCalibrationSmall =
            Array.AsReadOnly(new[] { InitialHalflingCalibration() });
        internal static IReadOnlyList<DeclaredSmallMountedProfile> PrivateCalibrationSmall => privateCalibrationSmall;
        private static DeclaredSmallMountedProfile InitialHalflingCalibration()
        {
            const string id = "small-default-halfling-horse-v1";
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
            var pose = new MountedRiderPoseProfile(id, "Pelvis", new PoseVector3(0f, 0f, 0f),
                new PoseVector3(0f, 0f, 0f),
                new MountedRiderLegPoseProfile("L_Up_leg", "L_leg", "L_foot",
                    new PoseVector3(-0.247152f, -0.228413f, 0.039166f),
                    new PoseVector3(-0.025848f, -0.14278f, 0.087729f), new PoseVector3(0f, 0f, 0f)),
                new MountedRiderLegPoseProfile("R_Up_leg", "R_leg", "R_foot",
                    new PoseVector3(0.246767f, -0.228475f, 0.048004f),
                    new PoseVector3(0.023794f, -0.167833f, 0.003672f), new PoseVector3(0f, 0f, 0f)));
            return new DeclaredSmallMountedProfile(body, pose,
                "7d2385144838cccc27082cc9792f338c716961ed25016e6fff986599196411db",
                new PoseVector3(0f, -0.16f, 0f), new PoseVector3(0f, -0.12f, 0f),
                new PoseVector3(0f, 0f, 0f));
        }
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
