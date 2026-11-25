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

        // MMR-03 (docs/missions/mounted-alpha2-melee-pose/MISSION.md). Whether the mount may take a turn-based stock intent.
        // Under paired activation one ordinary pair MELEE order (the player's rider melee click) retains its target for both
        // actors: once the rider's own attack is spent or the rider is individually unable, the horse follows through with its
        // own lawful attack in the same activation, with no redundant mount click. It joins only while it is addressable on
        // that activation (prepared, not ended, not split or suspended: PairedActivation.CanAddress) and while neither actor
        // has accepted newer competing work (review R1-F5); DecideNext still requires its own readiness and the dispatcher
        // rechecks capability and range. A rider ranged click (review R1-F4), an automatic intent and a separate-turn (paired
        // off) rider order never spend the horse; an explicit mount order stays the mount's.
        public static bool MountJoinsIntent(bool turnBased, bool explicitMountOrder, bool legacyUnifiedTurn, bool pairedActivation,
            bool automaticIntent, bool mountAddressable, bool meleeOrder = true, bool competingWork = false) =>
            !turnBased || explicitMountOrder || legacyUnifiedTurn ||
            pairedActivation && !automaticIntent && mountAddressable && meleeOrder && !competingWork;

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

        // The life-state half of target validity, with native UnitAttack's explicit-order rule (2.1.7b UnitAttack.Init
        // records m_IsUnconsciousAtStartOfCommand; UpdateTarget keeps a target that was already unconscious when ordered
        // and abandons one that falls unconscious later). A downed enemy lies prone (UnitProneController.ShouldBeProne),
        // so without this a mounted player could not attack a prone, downed enemy that an unmounted one can. A dead
        // target never qualifies; automatic proposals, routine continuation and Charge never pass the ordered flag.
        public static bool TargetLifeAdmits(bool targetConscious, bool targetDead, bool targetFinallyDead,
            bool targetUnconsciousWhenOrdered)
        {
            return !targetDead && !targetFinallyDead && (targetConscious || targetUnconsciousWhenOrdered);
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
    // Known issues Sprint 6 (A01, finding 5 of the review of 2ae4cc8b): what one exact native click on a pair actor is to
    // the retained intent.
    public enum MountedStockClick
    {
        // The same request while it has not dispatched or its dispatched command is live.
        Continue,
        // The same request after its dispatched command ended: a fresh request (the only case the A01 counter counts).
        FreshAfterTerminal,
        // Another target, turn, actor, weapon or action, or no retained request: the existing replacement path.
        NewRequest
    }

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
        // The player ordered this attack on a target that was already unconscious (native explicit-order rule).
        public bool TargetUnconsciousWhenOrdered { get; private set; }
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
            bool automatic = false, bool targetUnconsciousWhenOrdered = false)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Turn = turn;
            MountActor = mountActor;
            PreferMount = false;
            Automatic = automatic;
            // Only a player order carries the native explicit-order allowance; the party brain never targets the downed.
            TargetUnconsciousWhenOrdered = !automatic && targetUnconsciousWhenOrdered;
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

        // Known issues Sprint 6 (A01). A same-target click continues this intent only while the request has not dispatched
        // yet, or while the exact command it dispatched is still live. After that command ended, the intent is only waiting
        // for the next Update to retire or exhaust it: a deliberate click then is a fresh player request (a new
        // generation, its own answer), not continuation of work that already finished. Nothing here is timed or stored.
        public bool IsFreshRequestAfterTerminal(bool dispatchedCommandLive)
        {
            return Target != null && hasDispatched && !dispatchedCommandLive;
        }

        public MountedStockClick ClassifyClick(TTarget target, TTurn turn, bool mountActor, object rider, object mount, object weapon,
            object mountWeapon, int action, bool dispatchedCommandLive)
        {
            if (!CanContinue(target, turn, mountActor, rider, mount, weapon, mountWeapon, action)) return MountedStockClick.NewRequest;
            return IsFreshRequestAfterTerminal(dispatchedCommandLive) ? MountedStockClick.FreshAfterTerminal : MountedStockClick.Continue;
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
            TargetUnconsciousWhenOrdered = false;
            hasDispatched = false;
            riderContext = mountContext = weaponContext = mountWeaponContext = null;
            actionContext = 0;
            Generation++;
        }
    }
}
