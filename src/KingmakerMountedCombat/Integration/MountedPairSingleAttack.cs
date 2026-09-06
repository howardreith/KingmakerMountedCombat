using System;
using System.Diagnostics;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal enum MountedPairNativeAdmissionState
    {
        Unavailable,
        OutsidePairRange,
        UnsupportedExecutorOffset,
        OutsideNativeDistance,
        BlockedLineOfSight,
        Admitted
    }

    // Native attack geometry/terminal adapter. The historical filename is retained;
    // Phase 3H permits native ordinary sequences as well as explicit single attacks.
    internal class MountedPairSingleAttack : UnitAttack
    {
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly bool usesMountedRiderReach;
        private float pairApproachRadius;

        internal string TerminalLifecycle { get; private set; }

        public MountedPairSingleAttack(
            UnitEntityData target,
            UnitEntityData rider,
            UnitEntityData mount,
            bool usesMountedRiderReach,
            bool singleAttack = true)
            : base(target)
        {
            this.rider = rider;
            this.mount = mount;
            this.usesMountedRiderReach = usesMountedRiderReach;
            IsSingleAttack = singleAttack;
            CreatedByPlayer = true;
        }

        public override void Init(UnitEntityData executor)
        {
            base.Init(executor);
            float pairRadius;
            if (TryCalculatePairStoppingRadius(Target, out pairRadius))
            {
                pairApproachRadius = pairRadius;
                float nativeRadius;
                ApproachRadius = TryCalculateNativeApproachRadius(Target, out nativeRadius)
                    ? nativeRadius
                    : 0f;
            }
            else
            {
                pairApproachRadius = ApproachRadius;
            }
        }

        internal bool TryCalculateNativeApproachRadius(UnitEntityData target, out float radius)
        {
            radius = 0f;
            float stoppingRadius;
            if (!TryCalculatePairStoppingRadius(target, out stoppingRadius))
            {
                return false;
            }

            float pairDistance;
            float executorDistance;
            if (!TryObserveDistances(target, out pairDistance, out executorDistance))
            {
                return false;
            }

            float nativeRadius;
            if (MountedCombatSpatialPolicy.TryCalculateNativeExecutorAdmissionRadius(
                    stoppingRadius,
                    pairDistance,
                    executorDistance,
                    out nativeRadius))
            {
                radius = nativeRadius;
            }
            return true;
        }

        private bool TryCalculatePairStoppingRadius(UnitEntityData target, out float radius)
        {
            radius = 0f;
            if (!usesMountedRiderReach ||
                Executor != rider ||
                rider == null ||
                mount == null ||
                target == null ||
                rider.View == null ||
                mount.View == null ||
                target.View == null ||
                PlannedAttack == null)
            {
                return false;
            }

            radius = mount.View.Corpulence + target.View.Corpulence + PlannedAttack.WeaponRange;
            return radius >= 0f;
        }

        internal float PairApproachRadius => pairApproachRadius;

        internal float DelegatedMoveApproachRadius
        {
            get
            {
                UnitEntityData target = Target;
                if (mount?.View == null || target?.View == null || pairApproachRadius < 0f)
                {
                    return pairApproachRadius;
                }

                return MountedCombatSpatialPolicy.CalculateDelegatedMoveApproachRadius(
                    pairApproachRadius,
                    mount.View.Corpulence,
                    target.View.Corpulence);
            }
        }

        internal MountedPairNativeAdmissionState LastNativeAdmissionState { get; private set; }

        internal bool LastNativeDistanceSatisfied { get; private set; }

        internal bool PairRangeSatisfiedAtNativeStart { get; private set; }

        internal float PairDistanceAtNativeStart { get; private set; }

        internal float NativeExecutorDistanceAtStart { get; private set; }

        internal float NativeAdmissionRadiusAtStart { get; private set; }

        internal bool NativeAdmissionAdjustedAtStart { get; private set; }

        internal MountedPairNativeAdmissionState NativeAdmissionStateAtStart { get; private set; }

        internal bool NativeDistanceSatisfiedAtStart { get; private set; }

        internal bool IsPairEnoughClose
        {
            get
            {
                return EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted;
            }
        }

        internal MountedPairNativeAdmissionState EvaluateCurrentNativeAdmission()
        {
            LastNativeDistanceSatisfied = false;
            UnitEntityData target = Target;
            var origin = usesMountedRiderReach ? mount : Executor;
            var planned = PlannedAttack;
            if (Executor == null || target?.View == null || origin?.View == null || planned == null)
            {
                LastNativeAdmissionState = MountedPairNativeAdmissionState.Unavailable;
                return LastNativeAdmissionState;
            }

            // Native sequences can change weapons (for example, bow iteratives
            // followed by a natural bite). The next native attack owns its range;
            // retaining the first weapon's radius would grant that bite bow reach.
            pairApproachRadius = origin.View.Corpulence + target.View.Corpulence + planned.WeaponRange;

            if (!usesMountedRiderReach)
            {
                ApproachRadius = pairApproachRadius;
                return EvaluateExactNativeGate();
            }

            float pairDistance;
            float executorDistance;
            if (!TryObserveDistances(target, out pairDistance, out executorDistance))
            {
                LastNativeAdmissionState = MountedPairNativeAdmissionState.Unavailable;
                return LastNativeAdmissionState;
            }
            if (pairDistance > pairApproachRadius + MountedCombatSpatialPolicy.RangeTolerance)
            {
                LastNativeAdmissionState = MountedPairNativeAdmissionState.OutsidePairRange;
                return LastNativeAdmissionState;
            }

            float nativeAdmissionRadius;
            if (!MountedCombatSpatialPolicy.TryCalculateNativeExecutorAdmissionRadius(
                    pairApproachRadius,
                    pairDistance,
                    executorDistance,
                    out nativeAdmissionRadius))
            {
                LastNativeAdmissionState = MountedPairNativeAdmissionState.UnsupportedExecutorOffset;
                return LastNativeAdmissionState;
            }

            ApproachRadius = nativeAdmissionRadius;
            return EvaluateExactNativeGate();
        }

        internal bool TryPrepareNativeStartAdmission()
        {
            PairRangeSatisfiedAtNativeStart = false;
            PairDistanceAtNativeStart = 0f;
            NativeExecutorDistanceAtStart = 0f;
            NativeAdmissionRadiusAtStart = pairApproachRadius;
            NativeAdmissionAdjustedAtStart = false;
            NativeAdmissionStateAtStart = MountedPairNativeAdmissionState.Unavailable;
            NativeDistanceSatisfiedAtStart = false;

            if (!usesMountedRiderReach)
            {
                UnitEntityData nativeTarget = Target;
                if (Executor == null || nativeTarget == null)
                {
                    return false;
                }
                var nativeDistance = GeometryUtils.MechanicsDistance(Executor.Position, nativeTarget.Position);
                PairDistanceAtNativeStart = nativeDistance;
                NativeExecutorDistanceAtStart = nativeDistance;
                NativeAdmissionRadiusAtStart = ApproachRadius;
                NativeAdmissionStateAtStart = EvaluateCurrentNativeAdmission();
                NativeDistanceSatisfiedAtStart = LastNativeDistanceSatisfied;
                PairRangeSatisfiedAtNativeStart =
                    NativeAdmissionStateAtStart == MountedPairNativeAdmissionState.Admitted;
                return PairRangeSatisfiedAtNativeStart;
            }

            float pairDistance;
            float executorDistance;
            UnitEntityData target = Target;
            if (!TryObserveDistances(target, out pairDistance, out executorDistance))
            {
                return false;
            }

            PairDistanceAtNativeStart = pairDistance;
            NativeExecutorDistanceAtStart = executorDistance;
            float nativeAdmissionRadius;
            if (!MountedCombatSpatialPolicy.TryCalculateNativeExecutorAdmissionRadius(
                    pairApproachRadius,
                    pairDistance,
                    executorDistance,
                    out nativeAdmissionRadius))
            {
                return false;
            }

            PairRangeSatisfiedAtNativeStart = true;
            NativeAdmissionRadiusAtStart = nativeAdmissionRadius;
            NativeAdmissionAdjustedAtStart = nativeAdmissionRadius > pairApproachRadius;
            ApproachRadius = nativeAdmissionRadius;
            NativeAdmissionStateAtStart = EvaluateExactNativeGate();
            NativeDistanceSatisfiedAtStart = LastNativeDistanceSatisfied;
            return NativeAdmissionStateAtStart == MountedPairNativeAdmissionState.Admitted;
        }

        private MountedPairNativeAdmissionState EvaluateExactNativeGate()
        {
            LastNativeDistanceSatisfied =
                GeometryUtils.SqrMechanicsDistance(ApproachPoint, Executor.Position) <=
                ApproachRadius * ApproachRadius;
            if (!LastNativeDistanceSatisfied)
            {
                LastNativeAdmissionState = MountedPairNativeAdmissionState.OutsideNativeDistance;
                return LastNativeAdmissionState;
            }

            LastNativeAdmissionState = IsUnitEnoughClose
                ? MountedPairNativeAdmissionState.Admitted
                : MountedPairNativeAdmissionState.BlockedLineOfSight;
            return LastNativeAdmissionState;
        }

        protected override void OnTick()
        {
            if (NativeSingleAttackTerminalPolicy.ShouldAwaitNativeAnimation(
                    CombatController.IsInTurnBasedCombat(),
                    IsActed,
                    Result == ResultType.Success,
                    LastAttackRule != null,
                    AllAttacks.Count,
                    GetAttackIndex(),
                    PlannedAttack != null))
            {
                return;
            }

            base.OnTick();
        }

        protected override void OnEnded(bool raiseEvent = true)
        {
            TerminalLifecycle = DescribeTerminalLifecycle();
            base.OnEnded(raiseEvent);
        }

        private string DescribeTerminalLifecycle()
        {
            var frames = new StackTrace(1, false).GetFrames() ?? new StackFrame[0];
            var callerChain = string.Join(
                ">",
                frames
                .Take(12)
                .Select(frame => frame.GetMethod())
                .Where(method => method != null)
                .Select(method => (method.DeclaringType == null
                    ? "<unknown>"
                    : method.DeclaringType.FullName) + "." + method.Name)
                .ToArray());
            return "result=" + Result +
                ";acted=" + IsActed +
                ";timeSinceStart=" + TimeSinceStart.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                ";attackRuleObserved=" + (LastAttackRule != null) +
                ";animationPresent=" + (Animation != null) +
                ";animationFinished=" + (Animation != null && Animation.IsFinished) +
                ";callerChain=" + (string.IsNullOrEmpty(callerChain) ? "<unavailable>" : callerChain);
        }

        private bool TryObserveDistances(
            UnitEntityData target,
            out float pairDistance,
            out float executorDistance)
        {
            pairDistance = 0f;
            executorDistance = 0f;
            if (!usesMountedRiderReach || Executor != rider || rider == null || mount == null ||
                target == null || pairApproachRadius < 0f)
            {
                return false;
            }

            pairDistance = GeometryUtils.MechanicsDistance(mount.Position, target.Position);
            executorDistance = GeometryUtils.MechanicsDistance(rider.Position, target.Position);
            return !float.IsNaN(pairDistance) && !float.IsInfinity(pairDistance) && pairDistance >= 0f &&
                !float.IsNaN(executorDistance) && !float.IsInfinity(executorDistance) && executorDistance >= 0f;
        }
    }
}
