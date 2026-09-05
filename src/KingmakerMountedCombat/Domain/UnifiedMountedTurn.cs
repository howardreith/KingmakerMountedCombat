using System;

namespace KingmakerMountedCombat.Domain
{
    public static class UnifiedMountedTurnPolicy
    {
        public static bool ShouldSkipTurnCandidate(
            bool unifiedTurnEnabled,
            bool turnBasedCombat,
            bool relationshipMounted,
            bool candidateIsExactMount,
            bool candidateIsPendingSplitMount,
            int currentRound,
            int splitRound)
        {
            if (!unifiedTurnEnabled || !turnBasedCombat || !candidateIsExactMount)
            {
                return false;
            }

            if (relationshipMounted)
            {
                return true;
            }

            return candidateIsPendingSplitMount && splitRound >= 0 && currentRound <= splitRound;
        }

        public static bool ShouldAdvancePastSkippedCandidate(
            bool candidateMustBeSkipped,
            bool currentTurnCleared)
        {
            return candidateMustBeSkipped && currentTurnCleared;
        }

        public static bool ShouldKeepRiderTurnOpen(
            bool unifiedTurnEnabled,
            bool relationshipMounted,
            bool currentTurnIsExactRider,
            bool turnIsActing,
            bool mountAliveAndAble,
            bool mountHasStandardAction,
            bool mountHasMovement,
            bool mountHasSwiftAction,
            bool pairCommandActive)
        {
            return unifiedTurnEnabled && relationshipMounted && currentTurnIsExactRider &&
                turnIsActing && mountAliveAndAble &&
                (mountHasStandardAction || mountHasMovement || mountHasSwiftAction || pairCommandActive);
        }

        public static bool ShouldAdmitMountCommand(
            bool unifiedTurnEnabled,
            bool relationshipMounted,
            bool turnBasedCombat,
            bool currentTurnIsExactRider,
            bool riderTurnIsActingOrEnding,
            bool commandExecutorIsExactMount,
            bool commandIsExactOwnedPairCommand)
        {
            return unifiedTurnEnabled && relationshipMounted && turnBasedCombat &&
                currentTurnIsExactRider && riderTurnIsActingOrEnding &&
                commandExecutorIsExactMount && commandIsExactOwnedPairCommand;
        }

        public static bool ShouldMirrorInitiative(
            bool unifiedTurnEnabled,
            bool relationshipMounted,
            bool riderPrepared,
            bool initiativeUnitIsExactMount)
        {
            return unifiedTurnEnabled && relationshipMounted && riderPrepared &&
                initiativeUnitIsExactMount;
        }

        public static bool ShouldPrepareMountLedger(
            bool unifiedTurnEnabled,
            bool relationshipMounted,
            bool currentTurnIsExactRider,
            bool mountAliveAndInCombat,
            bool alreadyPreparedForExactTurn)
        {
            return unifiedTurnEnabled && relationshipMounted && currentTurnIsExactRider &&
                mountAliveAndInCombat && !alreadyPreparedForExactTurn;
        }

        public static bool ShouldRestoreSplitParticipation(
            bool hasPendingSplit,
            bool turnBasedCombat,
            int currentRound,
            int splitRound)
        {
            return hasPendingSplit && (!turnBasedCombat || currentRound > splitRound);
        }
    }

    public static class MountedFiveFootStepPolicy
    {
        public static bool ShouldSuppressDisengageOpportunity(
            bool unifiedTurnEnabled,
            bool relationshipMounted,
            bool turnBasedCombat,
            bool currentTurnIsExactRider,
            bool targetIsExactMount,
            bool exactPairMoveActive,
            bool nativeFiveFootStepEnabled,
            bool ordinaryMovementAlreadyUsed,
            float metersMovedByStep,
            float nativeMaximumMeters)
        {
            return unifiedTurnEnabled && relationshipMounted && turnBasedCombat &&
                currentTurnIsExactRider && targetIsExactMount && exactPairMoveActive &&
                nativeFiveFootStepEnabled && !ordinaryMovementAlreadyUsed &&
                IsFiniteNonNegative(metersMovedByStep) &&
                IsFiniteNonNegative(nativeMaximumMeters) &&
                metersMovedByStep <= nativeMaximumMeters + 0.001f;
        }

        public static float TransferMoveCooldown(
            float riderBefore,
            float riderAfterTemporaryTick,
            float mountBefore)
        {
            if (!IsFiniteNonNegative(riderBefore) || !IsFiniteNonNegative(riderAfterTemporaryTick) ||
                !IsFiniteNonNegative(mountBefore))
            {
                throw new ArgumentOutOfRangeException("Cooldown values must be finite and nonnegative.");
            }

            var delta = Math.Max(0f, riderAfterTemporaryTick - mountBefore);
            return Math.Min(6f, mountBefore + delta);
        }

        public static float ToRiderLedgerDelta(
            float physicalDelta,
            float riderSpeedMetersPerSecond,
            float mountSpeedMetersPerSecond,
            bool fiveFootStep)
        {
            ValidateMovementTranslation(
                physicalDelta,
                riderSpeedMetersPerSecond,
                mountSpeedMetersPerSecond);
            return fiveFootStep
                ? physicalDelta * mountSpeedMetersPerSecond / riderSpeedMetersPerSecond
                : physicalDelta;
        }

        public static float ToMountPhysicalDelta(
            float riderLedgerDelta,
            float riderSpeedMetersPerSecond,
            float mountSpeedMetersPerSecond,
            bool fiveFootStep)
        {
            ValidateMovementTranslation(
                riderLedgerDelta,
                riderSpeedMetersPerSecond,
                mountSpeedMetersPerSecond);
            return fiveFootStep
                ? riderLedgerDelta * riderSpeedMetersPerSecond / mountSpeedMetersPerSecond
                : riderLedgerDelta;
        }

        private static void ValidateMovementTranslation(
            float delta,
            float riderSpeedMetersPerSecond,
            float mountSpeedMetersPerSecond)
        {
            if (!IsFiniteNonNegative(delta) ||
                !IsFinitePositive(riderSpeedMetersPerSecond) ||
                !IsFinitePositive(mountSpeedMetersPerSecond))
            {
                throw new ArgumentOutOfRangeException(
                    "Movement translation requires a finite nonnegative delta and finite positive rider and mount speeds.");
            }
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
