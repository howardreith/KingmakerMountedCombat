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
        internal static bool IsDeclaredSmallProfile(string profileId) =>
            small.Any(profile => string.Equals(profile.Body.ProfileId, profileId, StringComparison.Ordinal));

        // Schema 3 policy only. Historical schema 1/2 validation must keep its
        // existing independent Medium whitelist in MountedSaveData unchanged.
        // A recognized ID is data eligibility, not native restore authority.
        public static bool IsSupportedArchiveProfile(string profileId) =>
            profileId == MediumMammothId || profileId == MediumHorseId ||
            IsDeclaredSmallProfile(profileId);
    }
}
