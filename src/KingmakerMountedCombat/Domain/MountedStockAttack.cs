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

    // One native hostile click. The native click handler raises OnAttackRequested(unit, target) and
    // then synchronously runs that unit's command in the same frame; the observation is bound to the
    // exact command object that Run carries. Only that command may claim or consume it, so a native
    // party-brain proposal can neither erase a pending click nor borrow its player provenance, and a
    // click whose own Run carried something else (auto-use, a move, an earlier refusal) leaves
    // nothing to borrow.
    public sealed class ObservedPlayerAttackRequest<TUnit, TCommand>
        where TUnit : class
        where TCommand : class
    {
        public TUnit Unit { get; private set; }
        public TUnit Target { get; private set; }
        public int Frame { get; private set; } = -1;
        public TCommand Command { get; private set; }

        public void Observe(TUnit unit, TUnit target, int frame)
        {
            Unit = unit;
            Target = target;
            Frame = frame;
            Command = null;
        }

        // Called first for every native Run on either pair actor's queue, before any refusal.
        public void Bind(TUnit runOwner, TCommand command, int frame)
        {
            if (Unit == null || Command != null)
            {
                return;
            }
            if (frame != Frame)
            {
                // The click's own Run is synchronous; an unbound observation from another frame is stale.
                Clear();
                return;
            }
            if (ReferenceEquals(runOwner, Unit))
            {
                Command = command;
            }
        }

        public bool IsPlayerCommand(TCommand command)
        {
            return command != null && ReferenceEquals(command, Command);
        }

        // Clears only for the click's own command; any other command leaves the observation intact.
        public bool Consume(TCommand command)
        {
            if (!IsPlayerCommand(command))
            {
                return false;
            }
            Clear();
            return true;
        }

        public void Clear()
        {
            Unit = null;
            Target = null;
            Frame = -1;
            Command = null;
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
            bool mountIsLegalActor = true,
            bool preferMount = false)
        {
            if (!exactMountedPair || !targetValid)
            {
                return MountedStockAttackDecision.CancelInvalidIntent;
            }
            if (pairCommandActive)
            {
                return MountedStockAttackDecision.Wait;
            }
            var mountReady = mountHasStandardAction && mountIsLegalActor && (!riderWeaponIsRanged || mountAlreadyInMeleeRange);
            // A native rider routine can outlast its cooldown. Let the waiting
            // RT partner go next instead of repeatedly selecting that rider.
            if (!turnBasedCombat && preferMount && mountReady)
            {
                return MountedStockAttackDecision.DispatchMount;
            }
            if (riderHasStandardAction && riderIsLegalActor)
            {
                return MountedStockAttackDecision.DispatchRider;
            }
            if (mountReady)
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

        // The refusal names the actor whose own native Standard is spent, and points to the
        // partner only when that partner can still attack with its own Standard.
        public static string NoStandardFeedback(string actorName, string partnerName, bool partnerHasStandardAction)
        {
            var actor = string.IsNullOrWhiteSpace(actorName) ? "This actor" : actorName;
            var feedback = actor + " has no Standard action left this turn.";
            return partnerHasStandardAction && !string.IsNullOrWhiteSpace(partnerName)
                ? feedback + " Select " + partnerName + " to attack with it."
                : feedback;
        }

        // A later dispatch of an already accepted request re-reads turn, action, selection, target, path and
        // lifecycle state that may have changed since the click: routine reassessment, not a new player error.
        // Faults and configuration (command admission, disagreeing range gates, a disabled feature, an unsupported
        // body or weapon) and an uncoded refusal are not routine, so they keep their answer.
        public static bool IsRoutineContinuationRefusal(IEnumerable<MountedCombatRejectionCode> codes)
        {
            if (codes == null) { return false; }
            var any = false;
            foreach (var code in codes)
            {
                switch (code)
                {
                    case MountedCombatRejectionCode.WrongActorOrSelection:
                    case MountedCombatRejectionCode.RelationshipInvalidated:
                    case MountedCombatRejectionCode.NotInCombat:
                    case MountedCombatRejectionCode.PairLifeStateInvalid:
                    case MountedCombatRejectionCode.TargetInvalid:
                    case MountedCombatRejectionCode.TargetNotVisible:
                    case MountedCombatRejectionCode.TargetNotHostile:
                    case MountedCombatRejectionCode.TargetNotAttackable:
                    case MountedCombatRejectionCode.WrongTurn:
                    case MountedCombatRejectionCode.WrongActionState:
                    case MountedCombatRejectionCode.AlreadyActiveCommand:
                    case MountedCombatRejectionCode.LifecycleBoundary:
                    case MountedCombatRejectionCode.NoPath:
                    case MountedCombatRejectionCode.OutsideSupportedRange:
                        any = true;
                        break;
                    default:
                        return false;
                }
            }
            return any;
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
        public bool PreferMount { get; private set; }
        // AI-01: admitted from the native party brain, not a click. Same dispatch; different boundaries.
        public bool Automatic { get; private set; }
        private bool hasDispatched;
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
            object rider = null, object mount = null, object weapon = null, object mountWeapon = null, int action = 0,
            bool automatic = false)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Turn = turn;
            MountActor = mountActor;
            PreferMount = false;
            Automatic = automatic;
            hasDispatched = false;
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

        // A click on the target the brain already chose makes the intent the player's own.
        public void PromoteToPlayer()
        {
            Automatic = false;
        }

        public void ObserveDispatch(bool mountActor)
        {
            if (Target != null)
            {
                PreferMount = !mountActor;
                hasDispatched = true;
            }
        }

        // A terminal command releases its reference before the retained intent observes spent actions.
        // That is completion of accepted work, not a fresh refused click: only a player request that
        // never dispatched is answered. Same-target continuation never re-arms feedback.
        public string GetRejectionFeedback(string feedback)
        {
            return Target != null && !Automatic && !hasDispatched ? feedback : null;
        }

        // A refused dispatch answers a player request that never dispatched. After accepted work, a later
        // refusal by the repeating intent stays quiet only when it is routine reassessment; a fault or
        // configuration refusal still answers once, since the rejection retires the intent.
        public string GetDispatchRejectionFeedback(string feedback, IEnumerable<MountedCombatRejectionCode> codes)
        {
            if (Target == null || Automatic) { return null; }
            return !hasDispatched || !UnifiedMountedStockAttackPolicy.IsRoutineContinuationRefusal(codes) ? feedback : null;
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
            PreferMount = false;
            Automatic = false;
            hasDispatched = false;
            riderContext = mountContext = weaponContext = mountWeaponContext = null;
            actionContext = 0;
            Generation++;
        }
    }
}
