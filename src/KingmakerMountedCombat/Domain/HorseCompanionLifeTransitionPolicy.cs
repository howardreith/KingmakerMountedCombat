using System;

namespace KingmakerMountedCombat.Domain
{
    internal static class HorseCompanionLifeTransitionPolicy
    {
        internal static bool IsExpectedNonConsciousTransition(
            string expectedActorId,
            string observedActorId,
            string previousLifeState,
            string currentLifeState)
        {
            return !string.IsNullOrWhiteSpace(expectedActorId) &&
                   string.Equals(expectedActorId, observedActorId, StringComparison.Ordinal) &&
                   string.Equals(previousLifeState, "Conscious", StringComparison.Ordinal) &&
                   (string.Equals(currentLifeState, "Unconscious", StringComparison.Ordinal) ||
                    string.Equals(currentLifeState, "Dead", StringComparison.Ordinal));
        }
    }
}
