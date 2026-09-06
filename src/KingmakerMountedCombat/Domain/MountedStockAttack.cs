using System;
using System.Collections.Generic;

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

    public enum MountedTargetTerminationDecision
    {
        ContinueNativeLifecycle,
        ObserveNativeTerminal,
        CancelExpectedInvalidation
    }

    public static class MountedTargetTerminationPolicy
    {
        public static MountedTargetTerminationDecision Decide(
            bool targetValid, bool targetInState, bool stillHostile,
            bool childReleasedAttack, bool childFinished)
        {
            if (childFinished) { return MountedTargetTerminationDecision.ObserveNativeTerminal; }
            if (targetValid || targetInState && stillHostile && childReleasedAttack)
            {
                return MountedTargetTerminationDecision.ContinueNativeLifecycle;
            }
            return MountedTargetTerminationDecision.CancelExpectedInvalidation;
        }
    }

    public static class UnifiedMountedStockAttackPolicy
    {
        public const int MaximumInputFrameDelta = 1;

        public static bool ContainsExactPrincipal<T>(IEnumerable<T> selection, T principal) where T : class
        {
            if (selection == null || principal == null) { return false; }
            foreach (var unit in selection)
            {
                if (ReferenceEquals(unit, principal)) { return true; }
            }
            return false;
        }

        public static bool AllowsOrdinaryInput(
            bool turnBasedCombat, bool enableUnifiedMountedTurn, bool currentTurnIsActor)
        {
            return !turnBasedCombat || enableUnifiedMountedTurn || currentTurnIsActor;
        }

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
            bool mountAlreadyInMeleeRange,
            bool riderIsLegalActor = true,
            bool mountIsLegalActor = true)
        {
            if (!exactMountedPair || !targetValid)
            {
                return MountedStockAttackDecision.CancelInvalidIntent;
            }
            if (pairCommandActive)
            {
                return MountedStockAttackDecision.Wait;
            }
            if (riderHasStandardAction && riderIsLegalActor)
            {
                return MountedStockAttackDecision.DispatchRider;
            }
            if (mountHasStandardAction && mountIsLegalActor && (!riderWeaponIsRanged || mountAlreadyInMeleeRange))
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

    /// <summary>Only admitted pointer intent owns this state. Cancellation invalidates all prior generations.</summary>
    public sealed class MountedAttackIntent<TTarget, TTurn> where TTarget : class where TTurn : class
    {
        public TTarget Target { get; private set; }
        public TTurn Turn { get; private set; }
        public bool MountActor { get; private set; }
        public long Generation { get; private set; }
        public bool HasEnteredCombat { get; private set; }
        private object riderContext;
        private object mountContext;
        private object weaponContext;
        private object mountWeaponContext;
        private int actionContext;

        public bool CanContinue(TTarget target, TTurn turn, bool mountActor,
            object rider = null, object mount = null, object weapon = null, object mountWeapon = null, int action = 0)
        {
            return Target != null && ReferenceEquals(Target, target) && ReferenceEquals(Turn, turn) &&
                MountActor == mountActor && ReferenceEquals(riderContext, rider) && ReferenceEquals(mountContext, mount) &&
                ReferenceEquals(weaponContext, weapon) && ReferenceEquals(mountWeaponContext, mountWeapon) && actionContext == action;
        }

        public void Begin(TTarget target, TTurn turn, bool mountActor, bool inCombat,
            object rider = null, object mount = null, object weapon = null, object mountWeapon = null, int action = 0)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Turn = turn;
            MountActor = mountActor;
            HasEnteredCombat = inCombat;
            riderContext = rider;
            mountContext = mount;
            weaponContext = weapon;
            mountWeaponContext = mountWeapon;
            actionContext = action;
            Generation++;
        }

        public bool Owns(TTarget target, long generation)
        {
            return Target != null && ReferenceEquals(Target, target) && Generation == generation;
        }

        public bool ObserveCombatEnded(bool inCombat)
        {
            HasEnteredCombat |= inCombat;
            return Target != null && HasEnteredCombat && !inCombat;
        }

        public void Cancel()
        {
            Target = null;
            Turn = null;
            HasEnteredCombat = false;
            MountActor = false;
            riderContext = mountContext = weaponContext = mountWeaponContext = null;
            actionContext = 0;
            Generation++;
        }
    }
}
