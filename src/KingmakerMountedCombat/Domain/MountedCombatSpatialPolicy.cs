using System;

namespace KingmakerMountedCombat.Domain
{
    public struct MountedCombatPoint
    {
        public MountedCombatPoint(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float X { get; }

        public float Z { get; }
    }

    public static class MountedCombatSpatialPolicy
    {
        public const float RangeTolerance = 0.05f;
        public const float DiagnosticRangeInset = 0.12f;
        public const float DiagnosticApproachExtension = 2.0f;
        public const float DiagnosticPlacementTolerance = 0.06f;
        public const float MinimumDiagnosticApproachDisplacement = 0.5f;
        public const float NativeAdmissionEpsilon = 0.001f;
        public const float MaximumNativeExecutorRadiusAdjustment = 0.75f;

        public static float CalculateStoppingRadius(
            float mammothCorpulence,
            float targetCorpulence,
            float weaponRange)
        {
            RequireFiniteNonNegative(mammothCorpulence, nameof(mammothCorpulence));
            RequireFiniteNonNegative(targetCorpulence, nameof(targetCorpulence));
            RequireFiniteNonNegative(weaponRange, nameof(weaponRange));
            return mammothCorpulence + targetCorpulence + weaponRange;
        }

        public static bool IsWithinRange(
            MountedCombatPoint mammothOrigin,
            MountedCombatPoint targetOrigin,
            float stoppingRadius)
        {
            RequireFiniteNonNegative(stoppingRadius, nameof(stoppingRadius));
            RequireFinite(mammothOrigin.X, nameof(mammothOrigin));
            RequireFinite(mammothOrigin.Z, nameof(mammothOrigin));
            RequireFinite(targetOrigin.X, nameof(targetOrigin));
            RequireFinite(targetOrigin.Z, nameof(targetOrigin));
            var dx = mammothOrigin.X - targetOrigin.X;
            var dz = mammothOrigin.Z - targetOrigin.Z;
            var admitted = stoppingRadius + RangeTolerance;
            return (dx * dx) + (dz * dz) <= admitted * admitted;
        }

        public static bool TryCalculateDiagnosticTargetDistance(
            float stoppingRadius,
            out float targetDistance)
        {
            RequireFiniteNonNegative(stoppingRadius, nameof(stoppingRadius));
            if (stoppingRadius <= DiagnosticRangeInset + RangeTolerance)
            {
                targetDistance = 0f;
                return false;
            }

            targetDistance = stoppingRadius - DiagnosticRangeInset;
            return true;
        }

        public static bool IsBoundedDiagnosticTargetDistance(
            float stoppingRadius,
            float actualDistance)
        {
            RequireFiniteNonNegative(actualDistance, nameof(actualDistance));
            float expectedDistance;
            return TryCalculateDiagnosticTargetDistance(stoppingRadius, out expectedDistance) &&
                actualDistance > RangeTolerance &&
                Math.Abs(actualDistance - expectedDistance) <= DiagnosticPlacementTolerance &&
                actualDistance <= stoppingRadius + RangeTolerance;
        }

        public static bool RequiresDiagnosticTargetPlacementRefresh(
            float stoppingRadius,
            float actualDistance)
        {
            return !IsBoundedDiagnosticTargetDistance(stoppingRadius, actualDistance);
        }

        public static bool TryCalculateDiagnosticApproachTargetDistance(
            float stoppingRadius,
            out float targetDistance)
        {
            RequireFiniteNonNegative(stoppingRadius, nameof(stoppingRadius));
            targetDistance = stoppingRadius + DiagnosticApproachExtension;
            return targetDistance > stoppingRadius + RangeTolerance;
        }

        public static bool IsBoundedDiagnosticApproachTargetDistance(
            float stoppingRadius,
            float actualDistance)
        {
            RequireFiniteNonNegative(actualDistance, nameof(actualDistance));
            float expectedDistance;
            return TryCalculateDiagnosticApproachTargetDistance(stoppingRadius, out expectedDistance) &&
                actualDistance > stoppingRadius + RangeTolerance &&
                Math.Abs(actualDistance - expectedDistance) <= DiagnosticPlacementTolerance;
        }

        public static bool RequiresDiagnosticApproachPlacementRefresh(
            float stoppingRadius,
            float actualDistance)
        {
            return !IsBoundedDiagnosticApproachTargetDistance(stoppingRadius, actualDistance);
        }

        public static bool TryCalculateNativeExecutorAdmissionRadius(
            float pairApproachRadius,
            float pairOriginDistance,
            float nativeExecutorDistance,
            out float nativeAdmissionRadius)
        {
            RequireFiniteNonNegative(pairApproachRadius, nameof(pairApproachRadius));
            RequireFiniteNonNegative(pairOriginDistance, nameof(pairOriginDistance));
            RequireFiniteNonNegative(nativeExecutorDistance, nameof(nativeExecutorDistance));
            nativeAdmissionRadius = pairApproachRadius;
            if (pairOriginDistance > pairApproachRadius + RangeTolerance)
            {
                return false;
            }

            if (nativeExecutorDistance > pairApproachRadius)
            {
                nativeAdmissionRadius = nativeExecutorDistance + NativeAdmissionEpsilon;
            }
            return nativeAdmissionRadius - pairApproachRadius <= MaximumNativeExecutorRadiusAdjustment;
        }

        private static void RequireFiniteNonNegative(float value, string name)
        {
            RequireFinite(value, name);
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static void RequireFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }

    public sealed class MountedCombatApproachSnapshot
    {
        public MountedCombatApproachSnapshot(
            bool approachRequiredAtStart,
            int delegatedMoveStartCount,
            int delegatedMoveTickCount,
            bool delegatedMoveExecutorIsExactMount,
            bool wrapperCommandRetained,
            bool delegatedMoveNeverQueued,
            bool delegatedMoveOwnedByMountMoveSlot,
            bool mountMoveSlotUnreplacedThroughoutApproach,
            bool mountQueueEmptyThroughoutApproach,
            bool delegatedMoveFinishedSuccessfully,
            bool mountMoveSlotRestoredAfterApproach,
            bool delegatedMoveDrivenByStockController,
            bool delegatedMoveDrivenByRiderTurnAdapter,
            bool turnBasedApproach,
            int delegatedMoveProgressObservationCount,
            bool riderStockAgentSuppressed,
            bool mountStockAgentAuthoritative,
            bool poseHealthyThroughout,
            int observationCount,
            bool selectionRetained,
            bool uiCoherentThroughout,
            float pairApproachRadius,
            float initialPairDistance,
            float pairDistanceAtAttackStart,
            float riderDisplacementAtAttackStart,
            float mountDisplacementAtAttackStart,
            float targetDisplacementAtAttackStart,
            int repathCount)
        {
            ApproachRequiredAtStart = approachRequiredAtStart;
            DelegatedMoveStartCount = delegatedMoveStartCount;
            DelegatedMoveTickCount = delegatedMoveTickCount;
            DelegatedMoveExecutorIsExactMount = delegatedMoveExecutorIsExactMount;
            WrapperCommandRetained = wrapperCommandRetained;
            DelegatedMoveNeverQueued = delegatedMoveNeverQueued;
            DelegatedMoveOwnedByMountMoveSlot = delegatedMoveOwnedByMountMoveSlot;
            MountMoveSlotUnreplacedThroughoutApproach = mountMoveSlotUnreplacedThroughoutApproach;
            MountQueueEmptyThroughoutApproach = mountQueueEmptyThroughoutApproach;
            DelegatedMoveFinishedSuccessfully = delegatedMoveFinishedSuccessfully;
            MountMoveSlotRestoredAfterApproach = mountMoveSlotRestoredAfterApproach;
            DelegatedMoveDrivenByStockController = delegatedMoveDrivenByStockController;
            DelegatedMoveDrivenByRiderTurnAdapter = delegatedMoveDrivenByRiderTurnAdapter;
            TurnBasedApproach = turnBasedApproach;
            DelegatedMoveProgressObservationCount = delegatedMoveProgressObservationCount;
            RiderStockAgentSuppressed = riderStockAgentSuppressed;
            MountStockAgentAuthoritative = mountStockAgentAuthoritative;
            PoseHealthyThroughout = poseHealthyThroughout;
            ObservationCount = observationCount;
            SelectionRetained = selectionRetained;
            UiCoherentThroughout = uiCoherentThroughout;
            PairApproachRadius = pairApproachRadius;
            InitialPairDistance = initialPairDistance;
            PairDistanceAtAttackStart = pairDistanceAtAttackStart;
            RiderDisplacementAtAttackStart = riderDisplacementAtAttackStart;
            MountDisplacementAtAttackStart = mountDisplacementAtAttackStart;
            TargetDisplacementAtAttackStart = targetDisplacementAtAttackStart;
            RepathCount = repathCount;
        }

        public bool ApproachRequiredAtStart { get; }
        public int DelegatedMoveStartCount { get; }
        public int DelegatedMoveTickCount { get; }
        public bool DelegatedMoveExecutorIsExactMount { get; }
        public bool WrapperCommandRetained { get; }
        public bool DelegatedMoveNeverQueued { get; }
        public bool DelegatedMoveOwnedByMountMoveSlot { get; }
        public bool MountMoveSlotUnreplacedThroughoutApproach { get; }
        public bool MountQueueEmptyThroughoutApproach { get; }
        public bool DelegatedMoveFinishedSuccessfully { get; }
        public bool MountMoveSlotRestoredAfterApproach { get; }
        public bool DelegatedMoveDrivenByStockController { get; }
        public bool DelegatedMoveDrivenByRiderTurnAdapter { get; }
        public bool TurnBasedApproach { get; }
        public int DelegatedMoveProgressObservationCount { get; }
        public bool RiderStockAgentSuppressed { get; }
        public bool MountStockAgentAuthoritative { get; }
        public bool PoseHealthyThroughout { get; }
        public int ObservationCount { get; }
        public bool SelectionRetained { get; }
        public bool UiCoherentThroughout { get; }
        public float PairApproachRadius { get; }
        public float InitialPairDistance { get; }
        public float PairDistanceAtAttackStart { get; }
        public float RiderDisplacementAtAttackStart { get; }
        public float MountDisplacementAtAttackStart { get; }
        public float TargetDisplacementAtAttackStart { get; }
        public int RepathCount { get; }

        public string[] FailedGateNames
        {
            get
            {
                var failures = new System.Collections.Generic.List<string>();
                AddFailure(failures, ApproachRequiredAtStart, "approach-required-at-start");
                AddFailure(failures, DelegatedMoveStartCount == 1, "one-delegated-move");
                AddFailure(failures,
                    TurnBasedApproach ? DelegatedMoveTickCount > 0 : DelegatedMoveTickCount == 0,
                    "delegated-move-drive-mode");
                AddFailure(failures, DelegatedMoveExecutorIsExactMount, "delegated-move-executor-is-mount");
                AddFailure(failures, WrapperCommandRetained, "wrapper-command-retained");
                AddFailure(failures, DelegatedMoveNeverQueued, "delegated-move-not-queued");
                AddFailure(failures, DelegatedMoveOwnedByMountMoveSlot, "mount-move-slot-owned");
                AddFailure(failures, MountMoveSlotUnreplacedThroughoutApproach, "mount-move-slot-unreplaced");
                AddFailure(failures, MountQueueEmptyThroughoutApproach, "mount-command-queue-empty");
                AddFailure(failures, DelegatedMoveFinishedSuccessfully, "delegated-move-finished-successfully");
                AddFailure(failures, MountMoveSlotRestoredAfterApproach, "mount-move-slot-restored");
                AddFailure(failures,
                    TurnBasedApproach
                        ? DelegatedMoveDrivenByRiderTurnAdapter && !DelegatedMoveDrivenByStockController
                        : DelegatedMoveDrivenByStockController && !DelegatedMoveDrivenByRiderTurnAdapter,
                    "delegated-move-controller-exact");
                AddFailure(failures, DelegatedMoveProgressObservationCount > 0, "delegated-move-progress-observed");
                AddFailure(failures, RiderStockAgentSuppressed, "rider-stock-agent-suppressed");
                AddFailure(failures, MountStockAgentAuthoritative, "mount-stock-agent-authoritative");
                AddFailure(failures, PoseHealthyThroughout, "pose-healthy-throughout");
                AddFailure(failures, ObservationCount > 0, "runtime-approach-observed");
                AddFailure(failures, SelectionRetained, "selection-retained");
                AddFailure(failures, UiCoherentThroughout, "ui-coherent-throughout");
                AddFailure(failures,
                    InitialPairDistance > PairApproachRadius + MountedCombatSpatialPolicy.RangeTolerance,
                    "initial-pair-distance-outside-range");
                AddFailure(failures,
                    PairDistanceAtAttackStart <= PairApproachRadius + MountedCombatSpatialPolicy.RangeTolerance,
                    "attack-start-inside-range");
                AddFailure(failures,
                    RiderDisplacementAtAttackStart >= MountedCombatSpatialPolicy.MinimumDiagnosticApproachDisplacement,
                    "rider-followed-approach");
                AddFailure(failures,
                    MountDisplacementAtAttackStart >= MountedCombatSpatialPolicy.MinimumDiagnosticApproachDisplacement,
                    "mount-performed-approach");
                AddFailure(failures,
                    TargetDisplacementAtAttackStart <= MountedCombatSpatialPolicy.RangeTolerance,
                    "target-remained-stationary");
                AddFailure(failures, RepathCount == 0, "no-unexpected-repath");
                return failures.ToArray();
            }
        }

        public bool AllPassed => FailedGateNames.Length == 0;

        public string FailureSummary => string.Join(",", FailedGateNames);

        private static void AddFailure(
            System.Collections.Generic.ICollection<string> failures,
            bool passed,
            string name)
        {
            if (!passed)
            {
                failures.Add(name);
            }
        }
    }

    public static class MountedPairTurnPolicy
    {
        public static bool CanIssueRiderAction(
            bool turnBasedCombat,
            bool currentUnitIsExactRider,
            bool riderTurnIsPreparing,
            bool riderTurnIsActing)
        {
            return CanIssueAction(
                turnBasedCombat,
                currentUnitIsExactRider,
                riderTurnIsPreparing,
                riderTurnIsActing);
        }

        public static bool CanIssueAction(
            bool turnBasedCombat,
            bool currentUnitIsExactActor,
            bool actorTurnIsPreparing,
            bool actorTurnIsActing)
        {
            return !turnBasedCombat ||
                (currentUnitIsExactActor && (actorTurnIsPreparing || actorTurnIsActing));
        }

        public static bool ShouldEndMountTurn(
            bool exactMountedPair,
            bool turnBasedCombat,
            bool currentUnitIsExactMount,
            bool explicitMountActionPendingOrActive = false)
        {
            return exactMountedPair && turnBasedCombat && currentUnitIsExactMount &&
                !explicitMountActionPendingOrActive;
        }

        public static bool CanDelegateMountMovement(
            bool exactMountedPair,
            bool turnBasedCombat,
            bool currentUnitIsExactRider,
            bool riderTurnIsActing,
            bool movingAgentIsExactMount)
        {
            return exactMountedPair &&
                turnBasedCombat &&
                currentUnitIsExactRider &&
                riderTurnIsActing &&
                movingAgentIsExactMount;
        }
    }
}
