using System;

namespace KingmakerMountedCombat.Domain
{
    public enum MountedStockAttackDecision
    {
        Wait,
        DispatchRider,
        DispatchMount,
        CompleteTurnBasedIntent,
        CancelInvalidIntent
    }

    public static class UnifiedMountedStockAttackPolicy
    {
        public const int MaximumInputFrameDelta = 1;

        public static bool IsExactObservedPlayerRequest(
            bool relationshipMounted,
            bool commandOwnerIsExactRider,
            bool exactRiderSelected,
            bool commandIsExactStockAttack,
            bool eventUnitMatchesRider,
            bool eventTargetMatchesCommand,
            int currentFrame,
            int observedFrame)
        {
            return relationshipMounted && commandOwnerIsExactRider && exactRiderSelected &&
                commandIsExactStockAttack && eventUnitMatchesRider && eventTargetMatchesCommand &&
                currentFrame >= observedFrame &&
                currentFrame - observedFrame <= MaximumInputFrameDelta;
        }

        public static MountedStockAttackDecision DecideNext(
            bool exactMountedPair,
            bool targetValid,
            bool pairCommandActive,
            bool turnBasedCombat,
            bool riderHasStandardAction,
            bool mountHasStandardAction,
            bool riderWeaponIsRanged,
            bool mountAlreadyInMeleeRange)
        {
            if (!exactMountedPair || !targetValid)
            {
                return MountedStockAttackDecision.CancelInvalidIntent;
            }
            if (pairCommandActive)
            {
                return MountedStockAttackDecision.Wait;
            }
            if (riderHasStandardAction)
            {
                return MountedStockAttackDecision.DispatchRider;
            }
            if (mountHasStandardAction && (!riderWeaponIsRanged || mountAlreadyInMeleeRange))
            {
                return MountedStockAttackDecision.DispatchMount;
            }

            return turnBasedCombat
                ? MountedStockAttackDecision.CompleteTurnBasedIntent
                : MountedStockAttackDecision.Wait;
        }

        public static bool IsValidTarget(
            bool targetExists,
            bool targetInState,
            bool targetAliveAndConscious,
            bool riderHostileToTarget,
            bool riderCanAttackTarget)
        {
            return targetExists && targetInState && targetAliveAndConscious &&
                riderHostileToTarget && riderCanAttackTarget;
        }
    }
}
