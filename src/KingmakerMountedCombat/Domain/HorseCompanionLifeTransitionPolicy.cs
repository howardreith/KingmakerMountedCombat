using System;

namespace KingmakerMountedCombat.Domain
{
    internal static class HorseCompanionLifeTransitionPolicy
    {
        internal static int MaximumPositiveDamageAttacksToReachHitPointBoundary(
            int currentDamage,
            int hitPoints)
        {
            if (currentDamage < 0) { throw new ArgumentOutOfRangeException(nameof(currentDamage)); }
            if (hitPoints <= 0) { throw new ArgumentOutOfRangeException(nameof(hitPoints)); }
            return Math.Max(1, hitPoints - currentDamage);
        }

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
