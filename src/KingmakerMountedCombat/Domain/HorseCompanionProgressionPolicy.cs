using System;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>
    /// Exact Kingmaker 2.1.7b animal-companion rank mapping used by AddPet.
    /// Keeping the decision in a pure policy lets the runtime adapter remain a
    /// narrow, bounded retry of the native operation.
    /// </summary>
    internal static class HorseCompanionProgressionPolicy
    {
        internal const int MaximumRank = 20;
        internal const int MaximumDeferredNativeAttempts = 1;
        internal const int MaximumDefaultBuildContextWaitFrames = 300;

        private static readonly int[] RankToCharacterLevel =
        {
            0, 2, 3, 3, 4, 5, 6, 6, 7, 8,
            9, 9, 10, 11, 12, 12, 13, 14, 15, 15,
            16
        };

        internal static int ExpectedCharacterLevel(int rank)
        {
            var boundedRank = Math.Max(0, Math.Min(MaximumRank, rank));
            return RankToCharacterLevel[boundedRank];
        }

        internal static bool RequiresSynchronization(int rank, int characterLevel)
        {
            return characterLevel < ExpectedCharacterLevel(rank);
        }

        internal static bool CanInvokeDeferredNativeUpdate(
            bool exactHorse,
            bool exactOwnership,
            bool defaultBuildContextPresent,
            int rank,
            int characterLevel,
            int nativeAttempts)
        {
            return exactHorse &&
                   exactOwnership &&
                   !defaultBuildContextPresent &&
                   nativeAttempts < MaximumDeferredNativeAttempts &&
                   RequiresSynchronization(rank, characterLevel);
        }
    }
}
