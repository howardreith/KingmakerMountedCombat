using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedPairAttackOutcome
    {
        public MountedCombatActionKind Action { get; set; }

        public string ActorId { get; set; }

        public string CommandOwnerId { get; set; }

        public string ResourceOwnerId { get; set; }

        public string TargetId { get; set; }

        public string Result { get; set; }
        public bool NativeRangedTailTermination { get; set; }
        public string PreStartInterruptBoundary { get; set; }

        public int ChildAttackStartCount { get; set; }
        public bool SingleAttackMode { get; set; }
        public bool NativeFullAttack { get; set; }
        public int NativePlannedAttackCount { get; set; }
        public int NativeCompletedAttackCount { get; set; }

        public int RepathCount { get; set; }
        // F4: repaths that followed a target the pair was closing on (not counted against the bounded allowance).
        public int ProgressingRepathCount { get; set; }
        // F4: the routine continued its remaining planned attacks on another target after its target went down.
        public int ContinuationTargetCount { get; set; }
        public string InitialTargetId { get; set; }
        public string ContinuationTargetIds { get; set; }
        // One entry per repath: the cause that forced it (a moved target, or an unadmitted finished move with its admission state), the delegated move ticks and distance, the
        // target distance, the rider debt and the native turn movement record (preview.155 measurement).
        public string RepathObservations { get; set; }

        public bool RiderStandardCharged { get; set; }

        public bool ActionStandardCharged { get; set; }

        public bool NativeAttackRuleObserved { get; set; }

        public string AttackWeaponBlueprintId { get; set; }

        public bool AttackWeaponIsNatural { get; set; }

        public bool AttackWeaponIsRanged { get; set; }

        public string AttackWeaponSlot { get; set; }

        public string AttackWeaponTypeBlueprintId { get; set; }

        public string AmmunitionStateBefore { get; set; }

        public string AmmunitionStateAfter { get; set; }

        public string ReloadStateBefore { get; set; }

        public string ReloadStateAfter { get; set; }

        public string TerminalReason { get; set; }

        // The mounted transaction's own terminal state, reported beside the raw native Result: native stamps Success
        // on a command it force-finished before start, which is a cancelled transaction, not a completed attack.
        public string TransactionState { get; set; }
        public bool TransactionCompleted { get; set; }
        public bool NativeCommandStarted { get; set; }
        public string ActionActorName { get; set; }
        // The action actor's native conditions when the command ended: the cause of an unexecuted terminal.
        public MountedActorCapabilityBlock ActorCapabilityBlockAtTerminal { get; set; }
        public string ActorCapabilityAtTerminal { get; set; }
        // Known issues Sprint 6 (A02): why it ended, recorded where it was ended; the controller resolves a native replacement.
        public MountedAttackEndCause EndCause { get; set; }
        public string EndCauseDetail { get; set; }
        // The exact block read at the command's own capability boundary when that was the recorded cause; the terminal
        // snapshot (ActorCapabilityBlockAtTerminal) stays a separate, later diagnostic fact.
        public MountedActorCapabilityBlock EndCauseCapabilityBlock { get; set; }
        // Review of fa533e97 (R18): the exact native branch that ended it (None when no exact branch was observed), and the
        // block read at that branch. Only a capability branch can make a condition the cause of a native end.
        public MountedNativeTerminalBranch NativeTerminalBranch { get; set; }
        public MountedActorCapabilityBlock NativeTerminalCapabilityBlock { get; set; }

        public bool PairRangeSatisfiedAtStart { get; set; }

        public float PairDistanceAtStart { get; set; }

        public float PairApproachRadiusAtStart { get; set; }

        public float NativeExecutorDistanceAtStart { get; set; }

        public float NativeAdmissionRadiusAtStart { get; set; }

        public bool NativeAdmissionAdjusted { get; set; }

        public string InitialNativeAdmissionState { get; set; }

        public string NativeAdmissionStateAtStart { get; set; }

        public bool NativeDistanceSatisfiedAtStart { get; set; }

        public bool NativeLineOfSightRecoveryObserved { get; set; }

        public bool ApproachRequiredAtStart { get; set; }

        public int DelegatedMoveStartCount { get; set; }

        public float DelegatedMoveApproachRadius { get; set; }

        public int DelegatedMoveTickCount { get; set; }

        public string DelegatedMoveExecutorId { get; set; }

        public bool DelegatedMoveExecutorIsExactMount { get; set; }

        public bool WrapperCommandRetainedThroughoutApproach { get; set; }

        public bool DelegatedMoveNeverQueuedOnMount { get; set; }

        public bool DelegatedMoveOwnedByMountMoveSlot { get; set; }

        public bool MountMoveSlotUnreplacedThroughoutApproach { get; set; }

        public bool MountQueueEmptyThroughoutApproach { get; set; }

        public bool DelegatedMoveFinishedSuccessfully { get; set; }

        public bool DelegatedMoveStoppedAtLegalRange { get; set; }

        public string DelegatedMoveResultBeforeLegalRangeStop { get; set; }

        public float DelegatedMovePairDistanceAtLegalRangeStop { get; set; }

        public bool MountMoveSlotRestoredAfterApproach { get; set; }

        public bool DelegatedMoveDrivenByStockController { get; set; }

        public bool DelegatedMoveDrivenByRiderTurnAdapter { get; set; }

        public int DelegatedMoveProgressObservationCount { get; set; }

        public bool RiderStockAgentSuppressedThroughoutApproach { get; set; }

        public bool MountStockAgentAuthoritativeThroughoutApproach { get; set; }

        public bool PoseHealthyThroughoutApproach { get; set; }

        public int ApproachObservationCount { get; set; }

        public float InitialPairDistance { get; set; }

        public float PairDistanceAtAttackStart { get; set; }

        public float RiderDisplacementAtAttackStart { get; set; }

        public float MountDisplacementAtAttackStart { get; set; }

        public float TargetDisplacementAtAttackStart { get; set; }

        public bool AttackAnimationHandleCreated { get; set; }

        public string AttackAnimationHandleSource { get; set; }

        public string AttackAnimationActionName { get; set; }

        public string AttackAnimationActionType { get; set; }

        public bool AttackAnimationActed { get; set; }

        public bool AttackAnimationFinished { get; set; }

        public bool AttackAnimationInterrupted { get; set; }
    }

    internal sealed class MountedPairAttackCommand : MountedPairSingleAttack
    {
        internal bool NativePartnerMovement { get; set; }
        private const float TargetRepathDistance = 0.75f;
        private const float MaximumElapsedSeconds = 8.5f;

        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPairOwner<UnitEntityData, GameMountedRelationshipService> pairOwner;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        // F4 (AI-01): replaced only by a native-rule routine continuation (TryContinueRoutineOnNextTarget).
        private UnitEntityData attackTarget;
        private readonly string initialTargetId;
        private readonly List<string> continuationTargetIds = new List<string>();
        private float lastChaseDistance = float.NaN;
        private float approachAllowanceSeconds;
        private readonly UnitEntityData actionActor;
        private readonly MountedCombatActionKind action;
        private readonly NativeSingleAttackWeaponSelection expectedMountPrimary;
        private readonly HorsePrimaryAttackAnimationAdapter horsePrimaryAttackAnimation;
        private readonly IModLogger logger;
        private readonly Action<MountedPairAttackCommand, MountedPairAttackOutcome> terminal;
        private bool allowApproach;
        // Chunk 6B increment 6B.2: charge mode layers the stock charge mechanics on this already qualified
        // transaction through one lease. Everything else about the transaction is unchanged.
        private readonly bool chargeMode;
        private bool chargeCleanupRequested;
        private bool chargeCarrierMotionOwned;
        private Kingmaker.View.UnitEntityView chargeCarrierView;
        private Kingmaker.View.UnitMovementAgent chargeCarrierAgent;
        private readonly List<KeyValuePair<UnitCommands, UnitMoveTo>> chargeCarriers =
            new List<KeyValuePair<UnitCommands, UnitMoveTo>>();
        private MountedChargeLease chargeLease;
        private bool chargeLeaseApplicationFailed;
        private bool chargeLeaseRolledBackOnFailure;
        private bool carrierTerminatedAfterLeaseFailure;
        private string chargeLeaseApplicationFailure;
        private string chargeLeaseApplicationFailedStep;
        private int chargeRevalidationCount;
        private bool chargeRevalidationFailed;
        private string chargeRevalidationFailurePhase;
        private string chargeRevalidationFailureReason;
        private string chargeRevalidationFailureCode;
        private string chargeRevalidationPhases = string.Empty;
        private bool chargeSequenceViolated;
        private bool carrierReleaseProvenForAttack;
        private string chargeCleanupDebtAtEnd = string.Empty;
        private readonly MountedChargeTransactionSequence chargeSequence = new MountedChargeTransactionSequence();
        private readonly MountedCombatTransaction transaction = new MountedCombatTransaction();
        // Compatibility name for the existing bounded evidence schema. There is
        // now one native command/sequence, not a free child under a charging shell.
        private MountedPairSingleAttack childAttack => this;
        private UnitMoveTo delegatedMove;
        private bool admittingDelegatedMove;
        private Vector3 targetSnapshot;
        private string retainedAttackWeaponBlueprintId;
        private bool retainedAttackWeaponIsNatural;
        private bool retainedAttackWeaponIsRanged;
        private string retainedAttackWeaponTypeBlueprintId;
        private string ammunitionStateBefore;
        private string reloadStateBefore;
        private bool terminalReported;
        private MountedAttackEndCause endCause;
        private string endCauseDetail;
        private MountedActorCapabilityBlock endCauseCapabilityBlock;

        // Known issues Sprint 6 (A02): the first recorded cause of this command's end stands. Diagnostic text only follows it.
        internal void RecordEndCause(MountedAttackEndCause cause, string detail,
            MountedActorCapabilityBlock capabilityBlock = MountedActorCapabilityBlock.None)
        {
            if (IsFinished || endCause != MountedAttackEndCause.Unrecorded || cause == MountedAttackEndCause.Unrecorded) return;
            endCause = cause;
            endCauseDetail = detail;
            endCauseCapabilityBlock = cause == MountedAttackEndCause.ActorCapability ? capabilityBlock : MountedActorCapabilityBlock.None;
        }

        private MountedNativeTerminalBranch nativeTerminalBranch;
        private MountedActorCapabilityBlock nativeTerminalBlock;

        // Review of fa533e97 (R18): recorded by NativeCommandTerminalBranches where native takes the branch, just before it
        // ends this command. The first branch stands.
        internal void RecordNativeTerminal(MountedNativeTerminalBranch branch, MountedActorCapabilityBlock block)
        {
            if (IsFinished || nativeTerminalBranch != MountedNativeTerminalBranch.None || branch == MountedNativeTerminalBranch.None) return;
            nativeTerminalBranch = branch;
            nativeTerminalBlock = block;
        }

        // Review of fa533e97 (R20): diagnostics never stand before a required transition.
        private void TryLogInfo(string line)
        {
            try { logger.Info(line); }
            catch (Exception) { }
        }

        private void TryLogException(string phase, Exception exception)
        {
            try { logger.Exception(phase, exception); }
            catch (Exception) { }
        }

        // Review of 2ae4cc8b (finding 1): a failed approach, thrown at the exact movement boundary so the tick's catch can
        // record ApproachFailed rather than Fault.
        private sealed class ApproachFailure : InvalidOperationException
        {
            internal ApproachFailure(string message) : base(message) { }
        }

        // Review of 2ae4cc8b (finding 2): the cause and the transaction fault are recorded before any diagnostic, the
        // logger is contained, and the command still ends.
        private void EndAfterFault(Exception exception, string phase)
        {
            RecordEndCause(exception is ApproachFailure ? MountedAttackEndCause.ApproachFailed : MountedAttackEndCause.Fault,
                exception.GetType().Name);
            transaction.Fault(exception.GetType().Name + ": " + exception.Message);
            TryLogException(phase, exception);
            if (IsActed) ForceFinish(ResultType.Fail);
            else Interrupt();
        }
        private bool approachRequiredAtStart;
        private MountedPairNativeAdmissionState initialNativeAdmissionState;
        private bool nativeLineOfSightRecoveryObserved;
        private float delegatedMoveApproachRadius;
        private int delegatedMoveStartCount;
        private int delegatedMoveTickCount;
        private Vector3 delegatedMoveOrigin;
        private readonly List<string> repathObservations = new List<string>();
        private string delegatedMoveExecutorId;
        private bool delegatedMoveExecutorIsExactMount = true;
        private bool wrapperCommandRetainedThroughoutApproach = true;
        private bool delegatedMoveNeverQueuedOnMount = true;
        private bool delegatedMoveOwnedByMountMoveSlot = true;
        private bool mountMoveSlotUnreplacedThroughoutApproach = true;
        private bool mountQueueEmptyThroughoutApproach = true;
        private bool delegatedMoveFinishedSuccessfully;
        private bool delegatedMoveStoppedAtLegalRange;
        private string delegatedMoveResultBeforeLegalRangeStop;
        private float delegatedMovePairDistanceAtLegalRangeStop;
        private bool mountMoveSlotRestoredAfterApproach = true;
        private bool delegatedMoveDrivenByStockController;
        private bool delegatedMoveDrivenByRiderTurnAdapter;
        private int delegatedMoveProgressObservationCount;
        private bool riderStockAgentSuppressedThroughoutApproach = true;
        private bool mountStockAgentAuthoritativeThroughoutApproach = true;
        private bool poseHealthyThroughoutApproach = true;
        private int approachObservationCount;
        private Vector3 riderPositionAtCommandStart;
        private Vector3 mountPositionAtCommandStart;
        private Vector3 targetPositionAtCommandStart;
        private float initialPairDistance;
        private float pairDistanceAtAttackStart;
        private float riderDisplacementAtAttackStart;
        private float mountDisplacementAtAttackStart;
        private float targetDisplacementAtAttackStart;
        private UnitAnimationActionHandle horsePrimaryAnimationHandle;
        private UnitAnimationActionSpecialAttack horsePrimaryAnimationAction;
        private string horsePrimaryAnimationActionName;
        private string horsePrimaryAnimationActionType;
        private string horsePrimaryAnimationHandleSource;

        public MountedPairAttackCommand(
            GameMountedRelationshipService relationship,
            UnitEntityData rider,
            UnitEntityData mount,
            UnitEntityData target,
            MountedCombatActionKind action,
            NativeSingleAttackWeaponSelection expectedMountPrimary,
            HorsePrimaryAttackAnimationAdapter horsePrimaryAttackAnimation,
            IModLogger logger,
            Action<MountedPairAttackCommand, MountedPairAttackOutcome> terminal,
            bool allowApproach = true,
            bool singleAttack = true,
            bool chargeMode = false)
            : base(target, rider, mount, action != MountedCombatActionKind.MountPrimaryNatural, singleAttack)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            pairOwner = relationship.PairOwner;
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            attackTarget = target ?? throw new ArgumentNullException(nameof(target));
            initialTargetId = target.UniqueId;
            this.action = action;
            if (action != MountedCombatActionKind.RiderMelee &&
                action != MountedCombatActionKind.RiderRanged &&
                action != MountedCombatActionKind.MountPrimaryNatural)
            {
                throw new ArgumentOutOfRangeException(nameof(action));
            }
            actionActor = action == MountedCombatActionKind.MountPrimaryNatural ? mount : rider;
            this.expectedMountPrimary = expectedMountPrimary;
            this.horsePrimaryAttackAnimation = horsePrimaryAttackAnimation ??
                throw new ArgumentNullException(nameof(horsePrimaryAttackAnimation));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            this.allowApproach = allowApproach;
            this.chargeMode = chargeMode;
            if (chargeMode && action != MountedCombatActionKind.RiderMelee)
            {
                throw new ArgumentOutOfRangeException(nameof(chargeMode), "A mounted charge is delivered as the rider melee pair transaction.");
            }
            ApproachRadius = InfiniteRange;
            MaxApproachRadius = InfiniteRange;
            NeedLoS = false;
            HasAnimation = true;
            CreatedByPlayer = true;
            if (!transaction.Arm(action))
            {
                throw new InvalidOperationException("Mounted combat transaction could not arm.");
            }
        }

        internal MountedPairSingleAttack ChildAttack => childAttack;
        internal bool NativeSequenceStarted => transaction.ChildAttackStartCount != 0;

        // Native UnitAttack explicit-order rule (m_IsUnconsciousAtStartOfCommand): the player ordered this attack on an
        // enemy that was already unconscious, so that exact target stays valid while it lives. Charge never sets it.
        private UnitEntityData unconsciousOrderedTarget;

        internal void AdmitTargetUnconsciousWhenOrdered()
        {
            var state = attackTarget?.Descriptor?.State;
            if (!chargeMode && state != null && state.IsUnconscious) unconsciousOrderedTarget = attackTarget;
        }

        internal bool TargetUnconsciousWhenOrdered => unconsciousOrderedTarget != null;

        private bool TargetLifeAdmits(UnitEntityData unit)
        {
            var state = unit?.Descriptor?.State;
            return state != null && UnifiedMountedStockAttackPolicy.TargetLifeAdmits(state.IsConscious, state.IsDead,
                state.IsFinallyDead, unconsciousOrderedTarget != null && ReferenceEquals(unit, unconsciousOrderedTarget));
        }

        internal UnitMoveTo DelegatedMove => delegatedMove;
        // R13-F4 (round 14): the installed WantsToMove of the mount's agent (Path != null || RequestedPath != null), read only for a live carrier
        // that has already moved the mount (its one approach was issued and followed).
        internal int NativeApproachEndedCount { get; private set; }
        private bool MountAgentStoppedAfterCarrierMoved()
        {
            var agent = mount?.View?.AgentASP;
            return delegatedMove != null && !delegatedMove.IsFinished && agent != null && !agent.WantsToMove &&
                GeometryUtils.MechanicsDistance(delegatedMoveOrigin, mount.Position) > 0f;
        }


        // PR #70 R8-F1 (round 11): the revalidated approach the exact route-bound carrier was made from (its untruncated native route and
        // destination arc length), for that carrier's first-debit revalidation.
        private PairedAttackClickProbe.Approach carrierApproach;
        private UnitMoveTo carrierApproachMove;
        internal PairedAttackClickProbe.Approach ApproachOf(UnitMoveTo carrier) =>
            carrier != null && ReferenceEquals(carrier, carrierApproachMove) ? carrierApproach : null;

        // PR #70 R6-F7 (rounds 11-12): the exact carrier whose cleanup this command began and what of it is done (interruption, removal from the
        // mount's Move slot, motion), independently of native Result/IsFinished. Round 12 (R11-F7): the record is its own object
        // (MountedCarrierCleanup), so terminal settlement can retain the unfinished obligation without holding this wrapper.
        private MountedCarrierCleanup cleanup;
        internal MountedCarrierCleanup CarrierCleanupRecord => cleanup;
        internal UnitMoveTo CarrierCleanupMove => cleanup?.Carrier;
        internal bool CarrierCleanupPending => cleanup != null && cleanup.Pending;
        internal string CarrierCleanupState => cleanup == null ? "none" : cleanup.State;

        // PR #70 R6-F7 (round 11): the one cleanup path for an exact carrier of this command, resumed by the controller's sweep after a fault in
        // any caller. A carrier whose cleanup this command never began is begun here (interrupted only while it still holds the Move slot);
        // otherwise only the unfinished steps run. Returns true once every step is settled; a fault propagates and leaves the rest owned.
        internal bool SettleCarrierCleanup(UnitMoveTo carrier)
        {
            if (carrier == null || chargeMode) return true;
            if (cleanup == null || !ReferenceEquals(cleanup.Carrier, carrier)) cleanup = new MountedCarrierCleanup(carrier, mount);
            var settled = cleanup.Settle(this);
            if (ReferenceEquals(delegatedMove, carrier) && settled) delegatedMove = null;
            return settled;
        }


        // Review R3-F3: every exact delegated move this command submits (the first, each repath replacement and each pre-start
        // move) is announced before native Run sees it, so the controller recognizes the order's own movement by identity.
        internal Action<UnitCommand> OwnCommandIssuing;
        private readonly System.Collections.Generic.List<UnitMoveTo> issuedMoves = new System.Collections.Generic.List<UnitMoveTo>();

        internal bool IsOwnDelegatedMove(UnitCommand command) =>
            command != null && (ReferenceEquals(command, delegatedMove) || issuedMoves.Exists(move => ReferenceEquals(move, command)));

        private void AnnounceOwnMove(UnitMoveTo move)
        {
            // Review R4-F8: finished moves (their callbacks have run) are released; the live carrier is kept.
            issuedMoves.RemoveAll(issued => issued.IsFinished && !ReferenceEquals(issued, delegatedMove));
            issuedMoves.Add(move);
            OwnCommandIssuing?.Invoke(move);
        }

        // MMR-05 (review R3-F1): pre-start approach. Native UnitActionController ticks an unstarted command's approach while
        // ShouldStartCommand still refuses it (initiative wait, cooldown, busy hands); for this command that approach is the
        // mount's movement, driven here from native TickApproaching (MountedAttackApproachPrefix). Native alone starts the
        // command; the delegated move is the same single movement owner the started approach uses and adopts at start.
        internal Func<bool> PreStartMovementForbidden;
        // Review R4-F2, PR-1 round 11 (R10-F1, R10-F2): set for an automatic order only. The controller's one current admission decision
        // for this exact automatic order (MountedCombatController.RetireAutomaticOrderIfNotAdmitted: its generation, target and owned
        // command): an order whose target became invalid, left the rider group's perception or left native admission (the brain's
        // IsTargetValid restricted rule with the action's current range) is retired by the controller, which ends this command once, so
        // no destination is produced at any approach step (pre-start, native start, repath, carrier creation) or strike before delivery.
        internal Func<MountedPairAttackCommand, bool> AutomaticOrderAdmissionLost;
        // Review R4-F1: set for a turn-based order whose click KMC admitted over native's refusal. Every carrier of such an
        // order is created only on a route revalidated from the CURRENT origin and grant (RouteRevalidator: the click probe's
        // production walk); when that proof cannot be established the approach ends cleanly instead of falling back to
        // native routing.
        internal System.Collections.Generic.List<Vector3> ValidatedApproachRoute;
        // PR #70 R1-F9: the plan identity the click proved; every revalidation receives this order's own plan at dispatch.
        internal PairedAttackClickProbe.PlanIdentity ClickPlan;
        internal Func<PairedAttackClickProbe.PlanIdentity, PairedAttackClickProbe.Approach> RouteRevalidator;
        // PR #70 R1-F6 (round 7): a carrier made from a route revalidated in the first-tick state (Approach.FirstTick) is reported once it holds
        // the mount's Move slot; its first movement tick is then decided by the controller (MountedMovementStateAdapter.FirstTickGate).
        internal Action<UnitMoveTo> FirstTickCarrierIssued;
        // PR #70 R8-F3 (round 9): asked before a route-bound carrier is created; false (its first-tick request was refused) ends the order
        // without issuing one. R8-F4: told when this command released its own carrier because its strike became admissible where it stands.
        internal Func<bool> FirstTickCarrierPermitted;
        internal Action CarrierReleasedForStrike;
        // PR #70 R1-F6 (round 7): a provisional order (the click is not yet an accepted order) carries no manual-target write, and its strike starts
        // only after the controller committed it (a strike reached without a decided tick commits only in place).
        internal bool ProvisionalOrder { get; private set; }
        internal Func<bool> ProvisionalStrikeGate;
        internal void MarkProvisionalOrder() { ProvisionalOrder = true; DeferManualTargetWrite(true); }
        internal void CommitProvisionalOrder() { ProvisionalOrder = false; DeferManualTargetWrite(false); }
        // PR #70 R1-F9 (round 3): the plan native would build for this order NOW (UnitAttack.OnStart re-plans against the actual actor
        // state), from an unrun attack of the same executor, target and mode; this order's own plan was built when it was run.
        private PairedAttackClickProbe.PlanIdentity CurrentPlanIdentity()
        {
            var plan = new MountedPairSingleAttack(attackTarget, rider, mount, true, IsSingleAttack);
            plan.Init(rider);
            return PairedAttackClickProbe.PlanIdentity.Of(plan);
        }

        // PR #70 R1-F9 (round 4): before a carrier is issued or a strike starts, THREE plans must agree: the click's admitted plan, the
        // plan this live wrapper was initialized with (its own PlannedAttack, which supplies the reach approach and strike admission use)
        // and the plan native would build from the current state. Either disagreement ends the order cleanly before movement or attack.
        private string ClickPlanDisagreement()
        {
            if (!RouteBound || !ClickPlan.Valid) return null;
            if (!ClickPlan.Matches(PairedAttackClickProbe.PlanIdentity.Of(this))) return "live wrapper plan changed";
            if (!ClickPlan.Matches(CurrentPlanIdentity())) return "click plan changed";
            return null;
        }
        internal bool RouteBound => ValidatedApproachRoute != null;
        // PR #70 R7-F2 (round 8): the same three-plan comparison, asked by the first-tick decision before any commit.
        internal string LivePlanDisagreement() => ClickPlanDisagreement();
        internal int RouteRevalidationCount { get; private set; }
        // Review R6-F1: a route-bound carrier is a point command to the proof's destination (inside the admissible opening) with
        // the proof's arrival radius: every position native's proximity rule accepts as arrival admits the strike.
        internal int RouteProofExpiredCount { get; private set; }
        // Native proximity would complete the carrier at once although the strike is not admitted: never re-issued.
        internal int RouteArrivalContradictionCount { get; private set; }
        internal bool ValidatedRouteFollowed { get; private set; }
        private bool preStartApproachAllowed;
        // Review R3-F1: a horse strike waiting to start approaches with native's own UnitAttack approach (the horse executes).
        private bool nativePreStartApproaching;
        private int preStartFailedRoutes;
        private TimeSpan preStartRetryAt;

        internal int PreStartMoveIssueCount { get; private set; }
        internal int PreStartMoveStopCount { get; private set; }
        internal bool PreStartMoveAdoptedAtStart { get; private set; }
        internal int PreStartFailedRoutes => preStartFailedRoutes;

        internal void AllowPreStartApproach()
        {
            if (!chargeMode && (action == MountedCombatActionKind.RiderMelee && actionActor == rider ||
                action == MountedCombatActionKind.MountPrimaryNatural && actionActor == mount))
                preStartApproachAllowed = true;
        }

        // Review R1-F11: the exact command that owns the mount's movement for this order right now: its live delegated move
        // while it holds the mount's Move slot, or this command itself while the horse approaches natively before its start.
        internal object CurrentMovementOwner
        {
            get
            {
                var commands = mount?.Commands;
                if (delegatedMove != null && !delegatedMove.IsFinished && commands != null &&
                    ReferenceEquals(commands.GetCommand(CommandType.Move), delegatedMove)) return delegatedMove;
                return nativePreStartApproaching && !IsStarted && !IsFinished ? this : null;
            }
        }

        // Asks the controller's decision for this unreleased automatic command; returns true when the order was retired and this command
        // ended. A released strike keeps native completion; a command that is not its order's own is never inspected by it.
        private bool EndIfAutomaticOrderNotAdmitted()
        {
            if (!IsAutomaticOrigin || IsFinished || StrikeReleased || AutomaticOrderAdmissionLost == null) return false;
            AutomaticOrderAdmissionLost(this);
            return IsFinished;
        }

        // PR-1 round 11: the controller retires this unreleased automatic command's order for lost admission (perception or native
        // eligibility): its own carrier and native pre-start approach are released (a successor's movement is kept) and the shared cause
        // is recorded before the controller ends it (the first recorded cause stands).
        internal void RecordAutomaticAdmissionLoss(string reason, string transactionReason)
        {
            if (IsFinished) return;
            StopDelegatedMove(false);
            StopNativePreStartApproach();
            if (!transaction.IsTerminal) transaction.Cancel(transactionReason);
            RecordEndCause(MountedAttackEndCause.Cancelled, reason);
        }

        private void StopNativePreStartApproach()
        {
            if (!nativePreStartApproaching) return;
            nativePreStartApproaching = false;
            ApproachRadius = InfiniteRange;
            NeedLoS = false;
            // PR-1 review R2-F2: a successor already steering the horse keeps its movement.
            var commands = mount?.Commands;
            var slotMove = commands?.GetCommand(CommandType.Move);
            var successor = slotMove != null && !slotMove.IsFinished || SuccessorSteersMount(commands, null);
            if (!successor) mount?.View?.StopMoving();
            PreStartMoveStopCount++;
        }

        // Returns true when native's own approach of this command should run this tick (the horse striker's case).
        internal bool TickPreStartApproach()
        {
            if (!preStartApproachAllowed || IsStarted || IsFinished) return false;
            try
            {
                RequireLiveExactPair();
                if (TryEndExpectedTargetInvalidation()) return false;
                if (EndIfAutomaticOrderNotAdmitted()) return false;
                // PR-1 review R4-F4: once newer work was accepted, the unreleased generation issues no further destination.
                if (!chargeMode && EndIfSupersededBeforeDelivery()) return false;
                if (actionActor == mount) return TickNativePreStartApproach();
                var now = Kingmaker.Game.Instance.TimeController.GameTime;
                var admissible = PreStartStrikeAdmissible();
                if (delegatedMove != null && delegatedMove.IsFinished)
                {
                    // The route ended (arrived, failed, or replaced by a newer order): release our reference exactly. A route
                    // that ended out of reach waits before the next bounded attempt.
                    if (!admissible)
                    {
                        preStartFailedRoutes++;
                        preStartRetryAt = now + TimeSpan.FromSeconds(MountedPreStartApproachPolicy.RetrySeconds);
                        // PR-1 review R4-F2: a sight destination that ended without admission is not chosen again before start either.
                        if (currentSightGoal.HasValue) { failedSightGoals.Add(currentSightGoal.Value); currentSightGoal = null; }
                    }
                    StopDelegatedMove(false);
                }
                var commands = mount.Commands;
                var live = delegatedMove != null && !delegatedMove.IsFinished;
                var decision = MountedPreStartApproachPolicy.Decide(
                    actionActor.Commands != null && actionActor.Commands.Contains(this),
                    !TurnBased.Controllers.CombatController.IsInTurnBasedCombat(),
                    allowApproach && !chargeCleanupRequested,
                    IsStarted,
                    IsFinished,
                    true,
                    PreStartMovementForbidden != null && PreStartMovementForbidden(),
                    admissible,
                    live,
                    live && commands != null && ReferenceEquals(commands.GetCommand(CommandType.Move), delegatedMove),
                    live && HorizontalDistance(targetSnapshot, attackTarget.Position) > TargetRepathDistance,
                    MountSlotsFreeForPreStartMove(),
                    preStartFailedRoutes,
                    now >= preStartRetryAt);
                switch (decision)
                {
                    case MountedPreStartApproachAction.Stop:
                        StopDelegatedMove(false);
                        PreStartMoveStopCount++;
                        break;
                    case MountedPreStartApproachAction.Reissue:
                        StopDelegatedMove(false);
                        if (!BeginPreStartMove()) PreStartRouteRefused(now);
                        break;
                    case MountedPreStartApproachAction.Issue:
                        if (!BeginPreStartMove()) PreStartRouteRefused(now);
                        break;
                }
            }
            catch (Exception exception)
            {
                EndAfterFault(exception, "Mounted pair pre-start approach");
            }
            return false;
        }

        // The horse is this command's executor: before native start, native's own UnitAttack approach moves it, with the
        // strike's real admission radius and line of sight (EvaluateCurrentNativeAdmission sets them for a horse strike), so
        // native starts it only once it is both close and ready, as for an unmounted attacker. Leaving the pre-start phase
        // (Hold, turn-based, approach no longer permitted) restores the unbounded start radius and stops the horse.
        private bool TickNativePreStartApproach()
        {
            var applies = MountedPreStartApproachPolicy.Decide(
                actionActor.Commands != null && actionActor.Commands.Contains(this),
                !TurnBased.Controllers.CombatController.IsInTurnBasedCombat(),
                allowApproach && !chargeCleanupRequested, IsStarted, IsFinished, true,
                PreStartMovementForbidden != null && PreStartMovementForbidden(), false, false, false, false, true, 0, true)
                == MountedPreStartApproachAction.Issue;
            if (!applies)
            {
                StopNativePreStartApproach();
                return false;
            }
            // Review R3-F1: the strike's own sight gate is set BEFORE classifying, so the classification and the native approach
            // that runs this tick read the same inputs (EvaluateCurrentNativeAdmission sets the strike radius for a horse strike).
            NeedLoS = true;
            var admitted = childAttack.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted;
            if (!admitted && !nativePreStartApproaching) PreStartMoveIssueCount++;
            nativePreStartApproaching = !admitted;
            return true;
        }

        private void PreStartRouteRefused(TimeSpan now)
        {
            preStartFailedRoutes++;
            preStartRetryAt = now + TimeSpan.FromSeconds(MountedPreStartApproachPolicy.RetrySeconds);
        }

        // The strike's own admission (mount origin for the rider's melee, planned weapon range, native command LOS), evaluated
        // with this unstarted command's native start inputs restored afterwards (it starts with an unbounded radius, no LOS).
        private bool PreStartStrikeAdmissible()
        {
            var savedRadius = ApproachRadius;
            var savedLineOfSight = NeedLoS;
            try
            {
                NeedLoS = true;
                return childAttack.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted;
            }
            finally
            {
                ApproachRadius = savedRadius;
                NeedLoS = savedLineOfSight;
            }
        }

        // PR-1 review R1-F7 (round 2): when the point carrier's distance-only arrival is already satisfied while the rider's planned
        // strike is not admitted, the carrier instead targets a mount position from which the RIDER's strike is admitted: candidate
        // positions around the target (24 directions at 95/75/50 % of the pair reach) are tested with the strike's own production
        // admission at that mount position (MountedPairSingleAttack.EvaluateNativeAdmissionAtMountPosition: rider and eye offsets
        // turned to the travel heading, native distance and command sight with the target's own blocker identity), and the nearest
        // admitted one is the goal. Native pathing finds the route; the exact native strike gate at release is unchanged.
        internal const float SightGoalArrivalMetres = 0.1f;
        // PR-1 review R1-F7 (round 4): a goal the carrier would accept without moving (inside its arrival radius plus this margin)
        // cannot change the rider's heading or eye; it is never chosen. A goal whose carrier ended without strike admission is
        // excluded (with this radius), and at most MaximumFailedSightGoals alternatives are tried.
        internal const float SightGoalMinimumTravelMetres = SightGoalArrivalMetres + 0.05f;
        internal const float FailedSightGoalExclusionMetres = 0.25f;
        internal const int MaximumFailedSightGoals = 3;
        private readonly List<Vector3> failedSightGoals = new List<Vector3>();
        private Vector3? currentSightGoal;
        internal int SightGoalCount { get; private set; }
        internal int FailedSightGoalCount => failedSightGoals.Count;

        private bool TryRiderSightGoal(out Vector3 goal)
        {
            goal = default(Vector3);
            if (childAttack == null || actionActor != rider || attackTarget == null) return false;
            var reach = childAttack.PairApproachRadius;
            if (float.IsNaN(reach) || reach <= 0f || failedSightGoals.Count >= MaximumFailedSightGoals) return false;
            var found = false; var best = float.MaxValue;
            foreach (var factor in new[] { 0.95f, 0.75f, 0.5f })
                for (var k = 0; k < 24; k++)
                {
                    var angle = k * Math.PI / 12d;
                    var candidate = attackTarget.Position + new Vector3((float)Math.Cos(angle), 0f, (float)Math.Sin(angle)) * (reach * factor);
                    candidate.y = mount.Position.y;
                    if (childAttack.EvaluateNativeAdmissionAtMountPosition(candidate, candidate - mount.Position) != MountedPairNativeAdmissionState.Admitted)
                        continue;
                    var distance = GeometryUtils.MechanicsDistance(candidate, mount.Position);
                    if (distance <= SightGoalMinimumTravelMetres || NearFailedSightGoal(candidate)) continue;
                    if (distance < best) { best = distance; goal = candidate; found = true; }
                }
            if (found) SightGoalCount++;
            return found;
        }

        private bool NearFailedSightGoal(Vector3 candidate)
        {
            foreach (var failed in failedSightGoals)
                if (GeometryUtils.MechanicsDistance(candidate, failed) <= FailedSightGoalExclusionMetres) return true;
            return false;
        }

        private UnitMoveTo NewApproachMove(float radius)
        {
            Vector3 goal;
            currentSightGoal = null;
            if (!chargeMode && PointArrivalWithoutStrike(radius) && TryRiderSightGoal(out goal))
            {
                currentSightGoal = goal;
                return new UnitMoveTo(goal, SightGoalArrivalMetres) { CreatedByPlayer = true, ShowTargetMarker = false, NeedLoS = false };
            }
            return new UnitMoveTo(targetSnapshot, radius)
            {
                CreatedByPlayer = true,
                ShowTargetMarker = false,
                NeedLoS = MountedCombatSpatialPolicy.DelegatedPointMoveRequiresLineOfSight
            };
        }

        // PR-1 review R4-F3: the order's strike admission from the current position (the controller's restricted-movement retirement).
        internal void AttachActorCharges(MountedPairAttackOutcome outcome)
        {
            var charge = DescribeStandardCharge();
            outcome.ActorId = charge.ActorId;
            outcome.RiderStandardCharged = charge.RiderStandardCharged;
            outcome.ActionStandardCharged = charge.ActionStandardCharged;
        }

        internal MountedStandardCharge DescribeStandardCharge() => new MountedStandardCharge
        {
            ActorId = actionActor?.UniqueId,
            RiderStandardCharged = StrikeReleased && actionActor == rider,
            ActionStandardCharged = StrikeReleased
        };

        internal bool StrikeAdmissibleNow() =>
            !IsFinished && (IsStarted ? childAttack.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted : PreStartStrikeAdmissible());

        private bool PointArrivalWithoutStrike(float radius)
        {
            if (GeometryUtils.SqrMechanicsDistance(targetSnapshot, mount.Position) > radius * radius) return false;
            return !(IsStarted ? childAttack.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted : PreStartStrikeAdmissible());
        }

        private bool MountSlotsFreeForPreStartMove()
        {
            var commands = mount.Commands;
            if (commands == null || commands.Queue.Count != 0 || commands.GroupCommand != null || commands.PreviousCommand != null) return false;
            foreach (var command in commands.Raw)
                if (command != null && !command.IsFinished) return false;
            return true;
        }

        // Returns whether the exact move now owns the mount's Move slot. A refused or queued submission is released at once.
        private bool BeginPreStartMove()
        {
            var commands = mount.Commands;
            if (commands == null || !MountSlotsFreeForPreStartMove()) return false;
            var radius = childAttack.DelegatedMoveApproachRadius;
            if (radius < 0f) return false;
            delegatedMoveApproachRadius = radius;
            targetSnapshot = attackTarget.Position;
            var move = NewApproachMove(radius);
            delegatedMove = move;
            AnnounceOwnMove(move);
            delegatedMoveStartCount++;
            delegatedMoveOrigin = mount.Position;
            delegatedMoveDrivenByStockController = true;
            mountMoveSlotRestoredAfterApproach = false;
            commands.Run(move);
            if (IsFinished || actionActor.Commands.Standard != this)
                throw new InvalidOperationException("Attack owner was replaced during pre-start approach admission.");
            if (!ReferenceEquals(commands.GetCommand(CommandType.Move), move) || commands.Queue.Contains(move))
            {
                if (!move.IsFinished) move.Interrupt(false);
                commands.Queue.Remove(move);
                delegatedMove = null;
                return false;
            }
            PreStartMoveIssueCount++;
            return true;
        }
        // A Hold that arrived while this command waited in the native queue: it attacks from where the pair stands or not at all.
        internal void ForbidApproach() { allowApproach = false; }

        internal UnitEntityData Rider => rider;

        internal UnitEntityData Mount => mount;

        internal UnitEntityData ActionActor => actionActor;

        public override bool CanMoveAfterStart => transaction.State == MountedCombatTransactionState.Approaching;

        public override void Init(UnitEntityData executor)
        {
            base.Init(executor);
            // Start the owned approach coordinator without moving the rider. The
            // native weapon gate is restored/evaluated before its attack starts.
            ApproachRadius = InfiniteRange;
            NeedLoS = false;
        }

        internal bool OwnsApproachTick => !IsFinished && transaction.ChildAttackStartCount == 0;

        internal bool ChargeMode => chargeMode;

        // The bounded Preparing-turn experiment is deferred with immutable native evidence.
        internal bool ChargeTransactionDelegating => false;

        // Read-only lease facts for the external charge reader; null when this transaction is not a charge.
        internal Newtonsoft.Json.Linq.JObject CaptureChargeLeaseEvidence()
        {
            return chargeLease == null ? null : chargeLease.CaptureEvidence();
        }

        internal string ChargeLeaseDescription => chargeLease == null ? null : chargeLease.Describe();

        internal bool ChargeLeaseApplicationFailed => chargeLeaseApplicationFailed;

        // True when there is no lease, or the lease has returned everything it owned.
        internal bool ChargeLeaseRestored => chargeLease == null || chargeLease.Restored;

        // Compensation entry point for a charge that failed after admission. A command that never started
        // never reaches OnEnded, so the lease is released here instead; Restore is idempotent, so a later
        // OnEnded is harmless. Nothing else about the command is touched.
        // The mutable charge conditions, re-read from live state at one of the three transaction
        // boundaries. The request deliberately carries no action-cost input, so this can never refuse
        // the charge for the Standard action the enclosing shell has already paid.
        private MountedChargeRevalidationOutcome RevalidateCharge(MountedChargeRevalidationPhase phase, bool readLane)
        {
            var targetState = attackTarget?.Descriptor?.State;
            var commands = mount == null ? null : mount.Commands;
            var request = new MountedChargeRevalidationRequest
            {
                Phase = phase,
                TargetValid = attackTarget != null && attackTarget.IsInState && targetState != null &&
                    targetState.IsConscious && !targetState.IsFinallyDead,
                TargetVisible = attackTarget != null && attackTarget.IsVisibleForPlayer,
                TargetHostile = rider != null && attackTarget != null && rider.IsEnemy(attackTarget),
                TargetAttackable = rider != null && attackTarget != null && rider.CanAttack(attackTarget),
                StraightRoute = MountedChargeGeometry.StraightRoute(mount, attackTarget),
                // BeforeRepath is the one phase that precedes a forced line. Later phases have left the lane behind.
                LaneClear = phase == MountedChargeRevalidationPhase.BeforeRepath && (!readLane || MountedChargeGeometry.FootprintClear(mount, rider, attackTarget)),
                MountCanMove = mount?.Descriptor?.State != null && mount.Descriptor.State.CanMove,
                LandingBlocked = MountedChargeGeometry.LandingBlocked(mount, rider, attackTarget),
                MountAvoidanceDisabled = MountedChargeGeometry.MountAvoidanceDisabled(mount),
                Distance = mount == null || attackTarget == null
                    ? float.MaxValue
                    : (attackTarget.Position - mount.Position).magnitude,
                MaximumRange = mount == null ? 0f : MountedChargeGeometry.MaximumRange(mount),
                RelationshipMounted = relationship.State == RelationshipState.Mounted,
                ExactPair = relationship.IsCurrentPairOwner(pairOwner) && relationship.Rider == rider && relationship.Mount == mount,
                LifecycleBoundary = relationship.State == RelationshipState.Faulted,
                RiderOwnsAttackSlot = actionActor != null && actionActor.Commands.Standard == this,
                CarrierOwnsMountMoveSlot = delegatedMove != null && commands != null &&
                    commands.GetCommand(UnitCommand.CommandType.Move) == delegatedMove,
                MountQueueEmpty = commands != null && commands.Queue.Count == 0,
                // Only the attack boundary asks this, and it asks the engine rather than recomputing:
                // the native admission observer is the engine's own range and position check for this
                // exact attacker and target.
                FinalAttackAdmitted = phase != MountedChargeRevalidationPhase.BeforeAttackStart ||
                    childAttack.EvaluateCurrentNativeAdmission() == MountedPairNativeAdmissionState.Admitted
            };
            var outcome = MountedChargeRevalidation.Evaluate(request);
            chargeRevalidationCount++;
            if (chargeRevalidationPhases.Length < 512)
            {
                chargeRevalidationPhases += (chargeRevalidationPhases.Length == 0 ? string.Empty : "|") +
                    phase + "=" + (outcome.IsValid ? "valid" : "invalid");
            }

            return outcome;
        }

        // True when the charge was newly invalid and this transaction has been terminated without an
        // attack. The native shell cost, if the engine already took it, is deliberately left alone.
        private bool TerminateChargeIfRevalidationFails(MountedChargeRevalidationPhase phase, bool readLane = true)
        {
            if (!chargeMode || IsFinished || transaction.IsTerminal)
            {
                return false;
            }

            var outcome = RevalidateCharge(phase, readLane);
            if (outcome.IsValid)
            {
                return false;
            }

            chargeRevalidationFailed = true;
            chargeRevalidationFailurePhase = phase.ToString();
            chargeRevalidationFailureReason = outcome.Reason;
            chargeRevalidationFailureCode = outcome.RejectionCode?.ToString();
            RecordEndCause(MountedAttackEndCause.Fault, "charge revalidation at " + phase);
            transaction.Cancel("Charge revalidation at " + phase + ": " + outcome.Reason);
            TryLogInfo("Mounted charge revalidation failed: phase=" + phase +
                "; reason=" + outcome.Reason + "; code=" + (chargeRevalidationFailureCode ?? "<none>") + ".");
            Interrupt();
            return true;
        }

        internal int ChargeRevalidationCount => chargeRevalidationCount;

        internal bool ChargeRevalidationFailed => chargeRevalidationFailed;

        internal string ChargeRevalidationFailurePhase => chargeRevalidationFailurePhase;

        internal string ChargeRevalidationFailureReason => chargeRevalidationFailureReason;

        internal string ChargeRevalidationFailureCode => chargeRevalidationFailureCode;

        internal string ChargeRevalidationPhases => chargeRevalidationPhases;

        internal string ChargeSequence => chargeSequence.Describe();

        internal bool ChargeSequenceLawful => chargeSequence.Lawful;

        internal bool CarrierReleaseProvenForAttack => carrierReleaseProvenForAttack;

        // Non-empty when the lease could not finish returning its mutations by the end of the command.
        internal string ChargeCleanupDebt => chargeLease == null ? string.Empty : chargeLease.UnresolvedCleanup;

        internal string ChargeCleanupDebtAtEnd => chargeCleanupDebtAtEnd;

        internal bool ChargeCleanupComplete => chargeLease == null || chargeLease.RollbackComplete;

        // Retry entry point for the cleanup owner: discharges outstanding lease debt if it can.
        internal bool TryDischargeChargeCleanupDebt()
        {
            if (chargeLease == null)
            {
                return true;
            }

            var complete = chargeLease.TryRestore();
            chargeCleanupDebtAtEnd = chargeLease.UnresolvedCleanup;
            return complete;
        }

        internal void RequestChargeCleanup()
        {
            if (!chargeMode) return;
            chargeCleanupRequested = true;
            RecordEndCause(MountedAttackEndCause.Cancelled, "charge cleanup requested");
            if (!transaction.IsTerminal) transaction.Cancel("Charge ownership cleanup requested");
        }

        internal bool ChargeCarrierDrained => !chargeCarrierMotionOwned && chargeCarriers.All(pair => pair.Value.IsFinished &&
            pair.Key != null && pair.Key.GetCommand(CommandType.Move) != pair.Value &&
            !pair.Key.Contains(pair.Value) && !pair.Key.Queue.Contains(pair.Value));

        internal bool OwnsChargeCarrier(UnitCommand command) => chargeCarriers.Any(pair => ReferenceEquals(pair.Value, command));

        // An order in the mount's Move or Standard slot that is not this charge's. A Free or Swift command, such as an attack of opportunity, moves nothing.
        internal bool ForeignMountCommandActive() => chargeCarriers.Select(pair => pair.Key).Where(container => container != null).Distinct()
            .Any(container => new[] { container.GetCommand(CommandType.Move), container.GetCommand(CommandType.Standard) }
                .Any(command => command != null && !command.IsFinished && !OwnsChargeCarrier(command)));

        // Only that order's own path is not this charge's to stop. The forced line under it is always ours: stopping it leaves the order free to request its path.
        private bool ForeignPathActive() => ForeignMountCommandActive() && !(chargeLease != null && chargeLease.HoldsForcedLine);

        internal void TryDrainChargeCarrier()
        {
            var steps = new List<MountedChargeCompensationStep>();
            foreach (var pair in chargeCarriers)
            {
                var carrier = pair.Value;
                var container = pair.Key;
                steps.Add(new MountedChargeCompensationStep("interrupt-carrier", () =>
                {
                    MountedChargeAdmissionFault.FireCleanup("carrier-interrupt");
                    if (!carrier.IsFinished) carrier.Interrupt(false);
                }));
                steps.Add(new MountedChargeCompensationStep("remove-carrier", () =>
                {
                    MountedChargeAdmissionFault.FireCleanup("mount-remove");
                    container.InterruptAll(command => ReferenceEquals(command, carrier));
                }));
            }
            if (steps.Count == 0) return;
            steps.Add(new MountedChargeCompensationStep("stop-carrier-path", () =>
            {
                if (!chargeCarrierMotionOwned) return;
                if (ForeignPathActive()) { chargeCarrierMotionOwned = false; return; }
                if (chargeCarrierView == null || chargeCarrierAgent == null) return;
                chargeCarrierAgent.Stop();
                if (chargeCarrierAgent.Path == null && !chargeCarrierAgent.IsReallyMoving)
                    chargeCarrierMotionOwned = false;
            }));
            var attempt = new MountedChargeCompensation("charge carrier cleanup", steps);
            attempt.Run();
            attempt.ConfirmPostconditions(new[] { new MountedChargePostcondition("carriers-released", () => ChargeCarrierDrained) });
            if (!attempt.Complete) throw new InvalidOperationException(attempt.Describe());
            delegatedMove = null;
            mountMoveSlotRestoredAfterApproach = true;
        }

        // True when the charge has stopped for any reason, so a caller must not continue this tick.
        private bool ChargeTransactionStopped()
        {
            return chargeCleanupRequested || IsFinished || transaction.IsTerminal || chargeRevalidationFailed || chargeSequenceViolated;
        }

        // Records one step of the charge order. A step taken out of order fails the charge: the order is
        // what makes the revalidations mean what they say.
        private bool ObserveChargeStep(MountedChargeTransactionStep step)
        {
            if (!chargeMode)
            {
                return false;
            }

            if (chargeSequence.Observe(step))
            {
                return false;
            }

            chargeSequenceViolated = true;
            chargeRevalidationFailed = true;
            chargeRevalidationFailurePhase = step.ToString();
            chargeRevalidationFailureReason = "Charge transaction order violated: " + chargeSequence.Describe();
            RecordEndCause(MountedAttackEndCause.Fault, "charge transaction order violated");
            try { logger.Info("Mounted charge transaction order violated: " + chargeSequence.Describe()); }
            catch (Exception) { }
            if (!transaction.IsTerminal)
            {
                transaction.Cancel(chargeRevalidationFailureReason);
            }

            if (!IsFinished)
            {
                Interrupt();
            }

            return true;
        }

        internal void CompensateChargeLease()
        {
            if (chargeMode && chargeLease != null)
            {
                chargeLease.Restore();
            }
        }

        internal bool ChargeLeaseRolledBackOnFailure => chargeLeaseRolledBackOnFailure;

        internal bool CarrierTerminatedAfterLeaseFailure => carrierTerminatedAfterLeaseFailure;

        internal string ChargeLeaseApplicationFailure => chargeLeaseApplicationFailure;

        internal string ChargeLeaseApplicationFailedStep => chargeLeaseApplicationFailedStep;

        private bool nativeSequenceTick;
        private bool nativeMeleeTailRangeRejected;
        internal bool NativeRangedTailTermination { get; private set; }
        internal bool NativeSequenceTickActive => nativeSequenceTick;
        internal bool NativeMeleeTailRangeRejected => nativeMeleeTailRangeRejected;

        // F4: the routine has not struck yet and still approaches its target (native IsMoveUnit for an approaching attack).
        // PR-1 review R1-F8: also before native start, while this command's own pre-start movement is live (the rider's carrier or
        // the horse's native approach); a stationary windup has no movement and is not included.
        internal bool IsApproachingBeforeAttack => !IsFinished && !IsActed && (transaction.State == MountedCombatTransactionState.Approaching &&
            transaction.ChildAttackStartCount == 0 || !IsStarted && (delegatedMove != null && !delegatedMove.IsFinished || nativePreStartApproaching));

        // PR-1 reviews R1-F2, R1-F9, R2-F1, R1-F3: the strike has been DELIVERED to native: this wrapper's own OnAction ran (native
        // UnitCommand.Tick calls it at the act and sets IsActed only afterwards), an attack rule was raised, or the routine advanced.
        // IsActed alone is not delivery: UnitCommand.ForceFinish (ForceFinishForTurnBased) sets it without calling OnAction.
        private bool strikeDelivered;
        internal bool StrikeReleased => strikeDelivered || childAttack != null && (childAttack.LastAttackRule != null || GetAttackIndex() > 0);

        // PR-1 round 14 (R13-F1): the released routine has no planned attack left (native UnitAttack.PlannedAttack is null once m_AttackIndex
        // reached the plan's count); native keeps the command running until its animation finishes (UnitCommand.Tick ends it only then).
        internal bool AttackPlanExhausted => StrikeReleased && childAttack != null && childAttack.PlannedAttack == null;

        // PR-1 review R2-F1: set by the controller for an ordinary order: its retained generation was superseded by newer
        // accepted player work. Checked at start, before release and during windup, so an installed wrapper stops too.
        internal Func<bool> RetainedOrderSupersededCheck;

        private bool EndIfSupersededBeforeDelivery()
        {
            if (StrikeReleased || RetainedOrderSupersededCheck == null || !RetainedOrderSupersededCheck()) return false;
            StopDelegatedMove(false);
            StopNativePreStartApproach();
            if (!transaction.IsTerminal) transaction.Cancel("Newer accepted player work supersedes this order.");
            RecordEndCause(MountedAttackEndCause.Cancelled, "superseded before delivery");
            Interrupt();
            return true;
        }

        internal bool ValidateNativeSequenceTarget()
        {
            TryContinueRoutineOnNextTarget();
            var targetValid = Target == attackTarget && attackTarget.IsInState && TargetLifeAdmits(attackTarget) &&
                actionActor.IsEnemy(attackTarget) && actionActor.CanAttack(attackTarget);
            var admission = targetValid ? EvaluateCurrentNativeAdmission() : MountedPairNativeAdmissionState.Unavailable;
            // Observe the existing native UpdateTarget rejection. Only a rejection
            // inside this native tick can classify its synchronous terminal event;
            // a later Stop/retarget cannot reuse an earlier range observation.
            if (nativeSequenceTick && admission == MountedPairNativeAdmissionState.OutsidePairRange)
                nativeMeleeTailRangeRejected = true;
            return targetValid && admission == MountedPairNativeAdmissionState.Admitted;
        }

        // F4 (AI-01). Native UnitAttack.UpdateTarget continues a started routine on the nearest remembered enemy once its
        // target is down (a Rapid Shot archer's next arrow goes to the next goblin). The replacement must also pass the
        // pair's own admission without approach, or the routine ends exactly as before. Nothing is spent or granted:
        // the routine's remaining planned attacks are the upper bound.
        private void TryContinueRoutineOnNextTarget()
        {
            // As native UpdateTarget: a target ordered while already unconscious is down only once it dies.
            var targetDown = attackTarget == null || !attackTarget.IsInState || !TargetLifeAdmits(attackTarget);
            var planned = PlannedAttack;
            if (!MountedRoutineContinuationPolicy.MayContinue(chargeMode, transaction.State == MountedCombatTransactionState.Attacking,
                    IsActed && LastAttackRule != null, planned != null, targetDown) || Executor?.View == null || actionActor?.Group == null)
                return;
            var previous = Target;
            var candidates = actionActor.Group.Memory.Enemies.Select(info => info.Unit)
                .Where(unit => unit != null && unit != attackTarget && unit.View != null && unit.IsInState &&
                    unit.Descriptor.State.IsConscious && !unit.Descriptor.State.IsFinallyDead &&
                    actionActor.IsEnemy(unit) && actionActor.CanAttack(unit) && !CommandTargetUntargetable(Executor, unit) &&
                    MountedRoutineContinuationPolicy.WithinNativeContinuationRange(Executor.DistanceTo(unit), Executor.View.Corpulence,
                        unit.View.Corpulence, planned.WeaponRange))
                .OrderBy(unit => Executor.DistanceTo(unit)).ToList();
            foreach (var candidate in candidates)
            {
                SetNativeAttackTarget(candidate);
                if (EvaluateCurrentNativeAdmission() != MountedPairNativeAdmissionState.Admitted) continue;
                if (!transaction.TryContinueOnTarget(attackTarget?.UniqueId, candidate.UniqueId)) break;
                TryLogInfo("Mounted pair routine continued on the next target: from=" + attackTarget?.UniqueId + "; to=" + candidate.UniqueId +
                    "; attackIndex=" + GetAttackIndex() + "/" + AllAttacks.Count + "; action=" + action + ".");
                attackTarget = candidate;
                continuationTargetIds.Add(candidate.UniqueId);
                targetSnapshot = candidate.Position;
                return;
            }
            SetNativeAttackTarget(previous);
        }

        // Native UnitAttack.set_Target (private, exact 2.1.7b token): the same write native UpdateTarget makes.
        private static readonly System.Reflection.MethodInfo NativeAttackTargetSetter = typeof(UnitAttack).GetMethod("set_Target",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(UnitEntityData) }, null);

        private void SetNativeAttackTarget(UnitEntityData unit)
        {
            if (NativeAttackTargetSetter == null || NativeAttackTargetSetter.MetadataToken != 0x0600266C)
                throw new InvalidOperationException("The exact native UnitAttack.set_Target contract changed.");
            NativeAttackTargetSetter.Invoke(this, new object[] { unit });
        }

        private bool IsNativeRangedTailTermination()
        {
            if (mount?.View == null || attackTarget?.View == null || actionActor == null) return false;
            var completed = GetAttackIndex();
            var targetValid = Target == attackTarget && attackTarget.IsInState && TargetLifeAdmits(attackTarget) &&
                actionActor.IsEnemy(attackTarget) && actionActor.CanAttack(attackTarget) &&
                actionActor.Descriptor.State.IsConscious && actionActor.Descriptor.State.CanAct;
            var distance = mount.DistanceTo(attackTarget);
            var bodyRadius = mount.View.Corpulence + attackTarget.View.Corpulence;
            return MountedRangedRoutineCompletionPolicy.CanRetainIntent(
                CombatController.IsInTurnBasedCombat(), action == MountedCombatActionKind.RiderRanged &&
                    IsFullAttack && !IsSingleAttack && IsActed && LastAttackRule != null,
                nativeSequenceTick && nativeMeleeTailRangeRejected, Result == ResultType.Interrupt,
                targetValid, AllAttacks.Count, completed,
                AllAttacks.Take(completed).All(item => item.Weapon.Blueprint.IsRanged),
                AllAttacks.Skip(completed).All(item => !item.Weapon.Blueprint.IsRanged &&
                    distance > bodyRadius + item.WeaponRange + MountedCombatSpatialPolicy.RangeTolerance),
                NativeCommandLineOfSightClear && AllAttacks.Take(completed).All(item =>
                    MountedCombatSpatialPolicy.DeliveredAttackWithinReach(distance, bodyRadius, item.WeaponRange)));
        }

        internal bool PreservesApproachParent(UnitCommands commands, CommandType type, bool interruptPaired)
        {
            return admittingDelegatedMove && !interruptPaired && type == CommandType.Standard &&
                actionActor == mount && commands == mount.Commands && commands.Standard == this &&
                IsStarted && !IsFinished && transaction.State == MountedCombatTransactionState.Approaching &&
                delegatedMove != null && !delegatedMove.IsFinished && commands.Queue.Count == 0;
        }

        internal MountedCombatActionKind Action => action;

        internal NativeSingleAttackWeaponSelection ExpectedMountPrimary => expectedMountPrimary;

        internal void RecordHorsePrimaryAnimation(
            UnitAnimationActionHandle handle,
            UnitAnimationActionSpecialAttack animationAction,
            string handleSource)
        {
            if (action != MountedCombatActionKind.MountPrimaryNatural ||
                handle == null || animationAction == null ||
                (handleSource != "stock-created" && handleSource != "kmc-supplied") ||
                horsePrimaryAnimationHandle != null)
            {
                throw new InvalidOperationException("Horse primary animation telemetry rejected a nonexact or duplicate handle.");
            }
            horsePrimaryAnimationHandle = handle;
            horsePrimaryAnimationAction = animationAction;
            horsePrimaryAnimationHandleSource = handleSource;
            horsePrimaryAnimationActionName = animationAction.name ?? "<unnamed>";
            horsePrimaryAnimationActionType = animationAction.Type.ToString();
        }

        internal bool HasAnyRecordedHorsePrimaryAnimation => horsePrimaryAnimationHandle != null;

        internal bool HasRecordedHorsePrimaryAnimation(UnitAnimationActionHandle handle)
        {
            return handle != null && ReferenceEquals(horsePrimaryAnimationHandle, handle);
        }

        internal void RefreshStockCreatedHorsePrimaryAnimation(
            UnitAnimationActionHandle handle,
            UnitAnimationActionSpecialAttack animationAction)
        {
            if (action != MountedCombatActionKind.MountPrimaryNatural ||
                handle == null || animationAction == null ||
                horsePrimaryAnimationHandle == null ||
                ReferenceEquals(horsePrimaryAnimationHandle, handle) ||
                horsePrimaryAnimationAction == null ||
                !ReferenceEquals(horsePrimaryAnimationAction, animationAction) ||
                (horsePrimaryAnimationHandleSource != "stock-created" &&
                 horsePrimaryAnimationHandleSource != "kmc-supplied"))
            {
                throw new InvalidOperationException("Horse primary animation telemetry rejected a nonexact stock-handle refresh.");
            }

            horsePrimaryAnimationHandle = handle;
            horsePrimaryAnimationHandleSource = "stock-created";
            horsePrimaryAnimationActionName = animationAction.name ?? "<unnamed>";
            horsePrimaryAnimationActionType = animationAction.Type.ToString();
        }

        internal bool HasAcceptedTargetBeforeChildAttack(UnitEntityData exactTarget)
        {
            return exactTarget != null && exactTarget == attackTarget && !IsFinished &&
                !transaction.IsTerminal && transaction.ChildAttackStartCount == 0 &&
                string.Equals(transaction.TargetId, exactTarget.UniqueId, StringComparison.Ordinal) &&
                (transaction.State == MountedCombatTransactionState.Approaching ||
                 transaction.State == MountedCombatTransactionState.Attacking);
        }

        protected override void OnStart()
        {
            if (chargeCleanupRequested) { Interrupt(false); return; }
            try
            {
                RequireLiveExactPair();
                if (TryEndExpectedTargetInvalidation()) { return; }
                // Review R4-F2: an unreleased automatic strike is never started, approached or adopted for a target that left
                // the rider group's perception (in-range start included).
                if (EndIfAutomaticOrderNotAdmitted()) { return; }
                NeedLoS = true;
                CreateAndValidateChildAttack();
                riderPositionAtCommandStart = rider.Position;
                mountPositionAtCommandStart = mount.Position;
                targetPositionAtCommandStart = attackTarget.Position;
                initialPairDistance = HorizontalDistance(mountPositionAtCommandStart, targetPositionAtCommandStart);
                targetSnapshot = attackTarget.Position;
                initialNativeAdmissionState = childAttack.EvaluateCurrentNativeAdmission();
                ObserveNativeAdmission(initialNativeAdmissionState);
                var requiresApproach = initialNativeAdmissionState != MountedPairNativeAdmissionState.Admitted;
                approachRequiredAtStart = requiresApproach;
                // PR-1 review R2-F1: a wrapper installed before the controller saw newer accepted work stops at its start.
                if (EndIfSupersededBeforeDelivery()) return;
                if (requiresApproach && !allowApproach)
                {
                    // PR-1 review R1-F1: an order without approach authority (a ranged order's horse, or a turn-based horse
                    // follow-through whose target left its reach) settles here: no carrier, no strike, no fault.
                    if (!transaction.IsTerminal) transaction.Cancel("The strike is out of reach and this order has no approach authority.");
                    RecordEndCause(MountedAttackEndCause.Cancelled, "out of reach without approach authority");
                    Interrupt();
                    return;
                }
                if (!transaction.AcceptTarget(attackTarget.UniqueId, requiresApproach))
                {
                    throw new InvalidOperationException("Mounted combat transaction rejected its exact target.");
                }
                lastChaseDistance = mount.DistanceTo(attackTarget);
                approachAllowanceSeconds = MountedChaseProgressPolicy.ApproachAllowanceSeconds(chargeMode, requiresApproach,
                    lastChaseDistance - mount.View.Corpulence - attackTarget.View.Corpulence, mount.CombatSpeedMps);

                if (requiresApproach)
                {
                    StopNativePreStartApproach();
                    // PR-1 review R1-F4: the current movement permission is rechecked BEFORE a live carrier is adopted or a new
                    // one admitted; ending here releases the exact owned approach (successors keep theirs). PR-1 round 13 (R12-F3): an
                    // automatic order's own decision answers first (its movement boundary included), then the generic restriction.
                    if (!chargeMode && (EndIfAutomaticOrderNotAdmitted() || EndIfMovementForbiddenAtApproach())) return;
                    // PR-1 review R4-F4: and the generation's supersession.
                    if (!chargeMode && EndIfSupersededBeforeDelivery()) return;
                    var commands = mount.Commands;
                    if (!chargeMode && delegatedMove != null && !delegatedMove.IsFinished && commands != null &&
                        ReferenceEquals(commands.GetCommand(CommandType.Move), delegatedMove) && commands.Queue.Count == 0)
                    {
                        // MMR-05: the live pre-start move becomes this approach's carrier (one movement owner throughout).
                        PreStartMoveAdoptedAtStart = true;
                        return;
                    }
                    if (delegatedMove != null) StopDelegatedMove(false);
                    var slotMove = commands?.GetCommand(CommandType.Move);
                    if (!chargeMode && slotMove != null && !slotMove.IsFinished && !IsOwnDelegatedMove(slotMove))
                    {
                        // A newer order holds the mount: this older attack ends quietly and leaves that order untouched.
                        transaction.Cancel("A newer mount order holds the mount.");
                        RecordEndCause(MountedAttackEndCause.Cancelled, "newer mount order holds the mount");
                        Interrupt();
                        return;
                    }
                    // PR-1 review R1-F4: an older compatible action still holding a mount slot or the queue delays the carrier: the
                    // order stays pending instead of failing the click. Review R2-F3: the wait is not chase time.
                    if (!chargeMode && !DelegatedCarrierAdmissible())
                    {
                        carrierDeferred = true;
                        deferralStartedAt = TimeSinceStart;
                        CarrierDeferralCount++;
                        return;
                    }
                    // Every call of the approach helpers consumes their outcome: a charge terminated
                    // inside the helper must not be followed by anything in the same tick.
                    if (BeginDelegatedMove() || ChargeTransactionStopped() || delegatedMove == null)
                    {
                        return;
                    }
                }
                else
                {
                    nativePreStartApproaching = false;
                    StartChildAttack();
                }
            }
            catch (Exception exception)
            {
                EndAfterFault(exception, "Mounted pair attack start");
            }
        }

        protected override void OnTick()
        {
            if (chargeCleanupRequested) { Interrupt(false); return; }
            HoldClockWhileDeferred();
            try
            {
                RequireLiveExactPair();
                if (TryEndExpectedTargetInvalidation())
                {
                    return;
                }
                if (ChaseElapsed > MaximumElapsedSeconds + approachAllowanceSeconds)
                {
                    const string bounded = "Mounted pair command exceeded its bounded execution time.";
                    // Still approaching after a delegated move began: the approach failed. Any other overrun is a fault.
                    if (transaction.State == MountedCombatTransactionState.Approaching && delegatedMoveStartCount > 0 &&
                        transaction.ChildAttackStartCount == 0)
                        throw new ApproachFailure(bounded);
                    throw new InvalidOperationException(bounded);
                }

                if (childAttack == null)
                {
                    throw new InvalidOperationException("Mounted pair command lost its child attack.");
                }

                if (transaction.State == MountedCombatTransactionState.Approaching)
                {
                    TickApproach();
                }
                // PR-1 review R2-F1: during the first strike's windup (child started, nothing delivered) perception loss of an
                // automatic target and supersession still stop the strike before delivery.
                if (!IsFinished && transaction.State == MountedCombatTransactionState.Attacking && !StrikeReleased &&
                    (EndIfAutomaticOrderNotAdmitted() || EndIfSupersededBeforeDelivery())) return;

                if (transaction.State == MountedCombatTransactionState.Attacking &&
                    transaction.ChildAttackStartCount == 0)
                {
                    StartChildAttack();
                }

                if (transaction.State == MountedCombatTransactionState.Attacking && !IsFinished)
                {
                    if (attackTarget.IsInState && TargetLifeAdmits(attackTarget))
                    {
                        childAttack.TurnToTarget();
                    }
                    nativeSequenceTick = true;
                    nativeMeleeTailRangeRejected = false;
                    try { base.OnTick(); }
                    finally { nativeSequenceTick = false; }
                }
            }
            catch (Exception exception)
            {
                EndAfterFault(exception, "Mounted pair attack tick");
            }
        }

        // PR-1 review R6-F1: the bounded reason this command states for its target's loss, shared by its own observation and the
        // controller's retirement of it.
        private string TargetInvalidationReason()
        {
            var targetState = attackTarget?.Descriptor?.State;
            var inState = attackTarget != null && attackTarget.IsInState;
            var hostile = inState && actionActor.IsEnemy(attackTarget);
            return !inState ? "target despawned" : !hostile ? "target hostility changed" :
                targetState == null ? "target state unavailable" :
                targetState.IsFinallyDead ? "target died" : !targetState.IsConscious ?
                "target became unconscious" : "target no longer attackable";
        }

        // PR-1 review R6-F1: the controller observed the loss first; record exactly what this command records when it observes it.
        internal void RecordTargetInvalidation()
        {
            if (IsFinished) return;
            var reason = TargetInvalidationReason();
            transaction?.Cancel("Expected target invalidation: " + reason);   // refused once terminal
            RecordEndCause(MountedAttackEndCause.TargetInvalid, reason);
        }

        // PR-1 review R6-F1 (round 8): whether this command's target is no longer valid for it (its own invalidation test), for the controller
        // to give a coinciding perception loss the same precedence.
        internal bool TargetInvalidNow()
        {
            var inState = attackTarget != null && attackTarget.IsInState;
            var hostile = inState && actionActor.IsEnemy(attackTarget);
            return !(inState && TargetLifeAdmits(attackTarget) && hostile && actionActor.CanAttack(attackTarget));
        }

        private bool TryEndExpectedTargetInvalidation()
        {
            var targetState = attackTarget?.Descriptor?.State;
            var inState = attackTarget != null && attackTarget.IsInState;
            var hostile = inState && actionActor.IsEnemy(attackTarget);
            var targetValid = !TargetInvalidNow();
            var decision = MountedTargetTerminationPolicy.Decide(
                targetValid, inState, hostile,
                childAttack != null && childAttack.IsActed && childAttack.LastAttackRule != null,
                childAttack != null && childAttack.IsFinished);
            if (decision != MountedTargetTerminationDecision.CancelExpectedInvalidation)
            {
                // A terminal native child is evaluated below using its actual Result.
                // A released shot/strike may finish its native animation after target death.
                return false;
            }
            // PR-1 round 12 (R11-F4): an unreleased automatic command's own observation of its target's loss is answered by its order's one
            // admission decision, which retires the order (cause, reason, diagnostics) once; a command that is not its order's own falls
            // through to its own handling below.
            if (!StrikeReleased && EndIfAutomaticOrderNotAdmitted()) return true;
            // F4 review MINOR-4: a released routine whose target left the field (despawn, hostility) continues natively
            // on the next target exactly as for a target that died in place; only without one is it cancelled as before.
            var continuations = transaction.ContinuationTargetCount;
            TryContinueRoutineOnNextTarget();
            if (transaction.ContinuationTargetCount != continuations) return false;
            var reason = TargetInvalidationReason();
            transaction.Cancel("Expected target invalidation: " + reason);
            RecordEndCause(MountedAttackEndCause.TargetInvalid, reason);
            Interrupt();
            return true;
        }

        // Native UnitCommand.Tick runs OnTick (where an unreleased windup is stopped by supersession or perception loss) and returns
        // if the command finished, before it calls OnAction; reaching here is therefore the delivery itself.
        protected override ResultType OnAction()
        {
            strikeDelivered = true;
            return base.OnAction();
        }

        protected override void OnEnded(bool raiseEvent = true)
        {
            // PR-1 review R2-F2: whoever ends this wrapper (the controller included) releases its own native pre-start approach.
            try { StopNativePreStartApproach(); } catch (Exception) { }
            if (chargeMode)
            {
                // Interrupt stamps Result before invoking OnEnded and cannot be replayed after an
                // exception. Always reach native base termination, independently of cleanup debt.
                chargeCleanupRequested = true;
                try
                {
                    try { TryDrainChargeCarrier(); }
                    catch (Exception exception) { TryLogException("Charge carrier cleanup retained", exception); }
                    // A delivered charge keeps its native penalty for the native round. Anything less than delivery removes it.
                    if (LastAttackRule != null && chargeLease != null)
                    {
                        try { chargeLease.HandBuffToNativeDuration(); }
                        catch (Exception exception) { TryLogException("Charge buff hand-off refused", exception); }
                    }
                    try { TryDischargeChargeCleanupDebt(); }
                    catch (Exception exception) { TryLogException("Charge lease cleanup retained", exception); }
                    if (Result == ResultType.Success && LastAttackRule != null &&
                        GetAttackIndex() == AllAttacks.Count && AllAttacks.Count > 0)
                        transaction.Complete(attackTarget.UniqueId);
                    if (!transaction.IsTerminal) transaction.Cancel(Result.ToString());
                }
                finally
                {
                    try { base.OnEnded(raiseEvent); }
                    finally { ReportTerminalOnce(); }
                }
                return;
            }
            try
            {
                NativeRangedTailTermination = nativeSequenceTick && nativeMeleeTailRangeRejected && IsNativeRangedTailTermination();
                StopDelegatedMove(false);
                if (Result == ResultType.Success && LastAttackRule != null &&
                    GetAttackIndex() == AllAttacks.Count && AllAttacks.Count > 0)
                    transaction.Complete(attackTarget.UniqueId);
                if (!transaction.IsTerminal)
                {
                    transaction.Cancel(Result.ToString());
                }
            }
            finally
            {
                if (chargeMode && chargeLease != null)
                {
                    // Restore is an attempt. If it cannot complete, the debt is retained on the lease and
                    // recorded here, so the controller's compensation and the evidence both see it rather
                    // than a lease that merely claims to be restored.
                    if (!chargeLease.TryRestore())
                    {
                        chargeCleanupDebtAtEnd = chargeLease.UnresolvedCleanup;
                        TryLogInfo("Mounted charge lease cleanup debt at command end: unresolved=" +
                            chargeCleanupDebtAtEnd + "; failures=" + chargeLease.CleanupFailures + ".");
                    }
                }

                // Review of 2ae4cc8b (finding 2): the terminal is delivered even if the native end throws.
                try { base.OnEnded(raiseEvent); }
                finally { ReportTerminalOnce(); }
            }
        }

        // PR-1 review R1-F4: a carrier waiting behind older work.
        private bool carrierDeferred;
        private float deferralStartedAt;
        internal int CarrierDeferralCount { get; private set; }
        // PR-1 review R2-F3: native UnitCommand.Tick interrupts any started command (UnitMoveContiniously aside) once TimeSinceStart
        // exceeds 9 s, after OnTick. While the carrier waits, OnTick holds TimeSinceStart where the wait began, so neither that
        // deadline nor the KMC bound counts the wait; the attack's own clock restarts at StartChildAttack with nothing carried over.
        internal float DeferredSeconds { get; private set; }
        private float ChaseElapsed => TimeSinceStart;
        internal bool CarrierDeferred => carrierDeferred;

        private void HoldClockWhileDeferred()
        {
            if (!carrierDeferred) return;
            DeferredSeconds += Math.Max(0f, TimeSinceStart - deferralStartedAt);
            SetTimeSinceStart(deferralStartedAt);
        }

        private bool DelegatedCarrierAdmissible() => MountedCombatSpatialPolicy.CanAdmitDelegatedMove(mount.Commands?.Raw,
            (int)CommandType.Standard, (int)CommandType.Move, this, actionActor == mount,
            IsStarted && !IsFinished && transaction.State == MountedCombatTransactionState.Approaching,
            mount.Commands != null && mount.Commands.Queue.Count == 0,
            mount.Commands?.GroupCommand == null, mount.Commands?.PreviousCommand == null);

        private bool EndIfMovementForbiddenAtApproach()
        {
            if (PreStartMovementForbidden == null || !PreStartMovementForbidden()) return false;
            if (!transaction.IsTerminal) transaction.Cancel("Mount movement is forbidden for this approach.");
            RecordEndCause(MountedAttackEndCause.Cancelled, "mount movement forbidden at the approach");
            Interrupt();
            return true;
        }

        private void TickDeferredCarrier()
        {
            var admission = childAttack.EvaluateCurrentNativeAdmission();
            ObserveNativeAdmission(admission);
            if (EndIfAutomaticOrderNotAdmitted()) return;
            if (admission == MountedPairNativeAdmissionState.Admitted)
            {
                carrierDeferred = false;
                if (!transaction.Arrive(attackTarget.UniqueId))
                    throw new InvalidOperationException("Mounted pair transaction could not enter attack range.");
                StartChildAttack();
                return;
            }
            if (EndIfAutomaticOrderNotAdmitted() || EndIfMovementForbiddenAtApproach()) return;
            if (!DelegatedCarrierAdmissible()) return;
            carrierDeferred = false;
            // PR-1 review R2-F3: legal movement starts from the target's current position.
            targetSnapshot = attackTarget.Position;
            if (BeginDelegatedMove() || ChargeTransactionStopped() || delegatedMove == null) return;
        }

        private void TickApproach()
        {
            // PR-1 review R4-F4: no carrier is issued, adopted or replaced for a superseded unreleased generation.
            if (!chargeMode && EndIfSupersededBeforeDelivery()) return;
            if (carrierDeferred && delegatedMove == null)
            {
                TickDeferredCarrier();
                return;
            }
            ObserveApproachInvariants();
            var nativeAdmission = childAttack.EvaluateCurrentNativeAdmission();
            ObserveNativeAdmission(nativeAdmission);
            // PR-1 review R3-F3: revoked movement cancels only an approach that still needs movement; a strike admissible from the
            // current position releases the exact carrier below and starts (the out-of-range case still ends here).
            // PR-1 round 13 (R12-F3): an automatic order's own decision answers first, so its retirement records its reason once.
            if (nativeAdmission != MountedPairNativeAdmissionState.Admitted && !chargeMode &&
                (EndIfAutomaticOrderNotAdmitted() || EndIfMovementForbiddenAtApproach())) return;
            if (nativeAdmission == MountedPairNativeAdmissionState.Admitted)
            {
                // Review R4-F2: perception is rechecked before arrival commits the strike.
                if (EndIfAutomaticOrderNotAdmitted()) return;
                if (delegatedMove == null)
                {
                    throw new InvalidOperationException(
                        "Mounted pair command lost its delegated move at the legal attack-range boundary.");
                }

                // The transition revalidation runs WHILE the exact carrier still owns the mount Move slot,
                // because that is one of the facts it checks. Releasing the carrier first made a lawful
                // charge reject itself here.
                if (TerminateChargeIfRevalidationFails(MountedChargeRevalidationPhase.BeforeAttackTransition))
                {
                    return;
                }

                if (ObserveChargeStep(MountedChargeTransactionStep.TransitionRevalidation))
                {
                    return;
                }

                // Only now is the exact carrier stopped and removed.
                if (!delegatedMove.IsFinished)
                {
                    delegatedMoveStoppedAtLegalRange = true;
                    delegatedMoveResultBeforeLegalRangeStop = delegatedMove.Result.ToString();
                    delegatedMovePairDistanceAtLegalRangeStop =
                        GeometryUtils.MechanicsDistance(mount.Position, attackTarget.Position);
                    StopDelegatedMove(false);
                }
                else if (!chargeMode && delegatedMove.Result != ResultType.Success)
                {
                    // PR-1 review R4-F3: an ordinary strike that is admissible settles an exact carrier that already ended without success
                    // (interrupted when movement was revoked); supersession was checked at the top of this tick. Charges keep the strict check.
                    StopDelegatedMove(false);
                }
                else
                {
                    StopDelegatedMove(true);
                }

                if (ObserveChargeStep(MountedChargeTransactionStep.CarrierReleasedForAttack))
                {
                    return;
                }

                // The release is proven, not assumed: no carrier, an empty Move slot and an empty queue.
                var moveSlotAfterRelease = mount.Commands == null
                    ? null
                    : mount.Commands.GetCommand(UnitCommand.CommandType.Move);
                carrierReleaseProvenForAttack = delegatedMove == null && moveSlotAfterRelease == null &&
                    mount.Commands != null && mount.Commands.Queue.Count == 0;
                if (chargeMode && !carrierReleaseProvenForAttack)
                {
                    chargeRevalidationFailed = true;
                    chargeRevalidationFailurePhase = "CarrierRelease";
                    chargeRevalidationFailureReason =
                        "The mounted charge could not prove the mount movement slot was released.";
                    RecordEndCause(MountedAttackEndCause.Fault, "charge carrier release unproven");
                    transaction.Cancel(chargeRevalidationFailureReason);
                    TryLogInfo("Mounted charge carrier release unproven: carrier=" +
                        (delegatedMove == null ? "<released>" : "<live>") +
                        "; moveSlot=" + (moveSlotAfterRelease == null ? "<empty>" : "<occupied>") +
                        "; queue=" + (mount.Commands == null ? -1 : mount.Commands.Queue.Count) + ".");
                    Interrupt();
                    return;
                }

                if (ObserveChargeStep(MountedChargeTransactionStep.CarrierReleaseProven))
                {
                    return;
                }

                if (!transaction.Arrive(attackTarget.UniqueId))
                {
                    throw new InvalidOperationException("Mounted pair transaction could not enter attack range.");
                }

                if (ObserveChargeStep(MountedChargeTransactionStep.Arrived))
                {
                    return;
                }
                // PR #70 R8-F4 (round 9): the verified transition to a stationary strike settles this carrier's first-tick request.
                if (!chargeMode) CarrierReleasedForStrike?.Invoke();

                return;
            }

            if (EndIfAutomaticOrderNotAdmitted()) return;
            var displacement = HorizontalDistance(targetSnapshot, attackTarget.Position);
            if (displacement > TargetRepathDistance)
            {
                // A failed revalidation inside the repath must not be followed by anything this tick:
                // no second repath, no new carrier, no lease application, no IsFinished dereference.
                if (Repath("target-moved;displacement=" + displacement.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), true) ||
                    ChargeTransactionStopped() || delegatedMove == null)
                {
                    return;
                }
            }

            if (delegatedMove == null)
            {
                if (BeginDelegatedMove() || ChargeTransactionStopped() || delegatedMove == null)
                {
                    return;
                }
            }

            // As the stock turn-based routine does on every tick: the speed is re-asserted and a stopped line is forced again.
            if (chargeMode && chargeLease != null && !delegatedMove.IsFinished &&
                TurnBased.Controllers.CombatController.IsInTurnBasedCombat())
            {
                // The stock routine ends the same way, and a tripped mount must not hold the turn to the bound. A stopped line
                // is about to be forced again from where the mount now stands, so it answers to the same lane as any other.
                if ((!mount.Descriptor.State.CanMove || chargeLease.NeedsReforce(true)) &&
                    TerminateChargeIfRevalidationFails(MountedChargeRevalidationPhase.BeforeRepath))
                {
                    return;
                }

                chargeLease.Maintain(true);
            }

            if (!NativePartnerMovement && TurnBased.Controllers.CombatController.IsInTurnBasedCombat() &&
                Kingmaker.Game.Instance?.TurnBasedCombatController?.CurrentTurn?.Unit == rider)
            {
                if (DriveDelegatedMoveOnRiderTurn() || ChargeTransactionStopped() || delegatedMove == null)
                {
                    return;
                }
            }

            // PR #70 R13-F3/R13-F4 (round 14): an accepted route-bound ordinary turn-based order follows the installed disposition once its one
            // issued approach can no longer bring the strike into reach. Installed UnitCommand.TickApproaching issues ONE turn-based path
            // (NextApproachTime = MaxValue: no re-path); when movement is exhausted or denied, UnitMovementAgent.CanMoveInTurnBased (060018A9)
            // completes the movement (CompleteMovement -> Stop/InterruptMovement: Path and RequestedPath cleared), so WantsToMove (060018BE:
            // Path != null || RequestedPath != null) and UnitEntityView.IsMoving become false, and UnitActionController.TickCommandTurnBased
            // (0600911D) force-finishes the approaching command that is not close enough (ForceFinishForTurnBased, then TickCommand returns
            // before UpdateCooldowns: no strike, no Standard; the movement debited stays spent). The mounted order therefore ends here, without a
            // further carrier, when its carrier finished out of reach or the mount's agent no longer wants to move after the carrier moved it.
            // Explicit newer orders, the charge and real-time paths keep their own handling (Repath below).
            // Only an ACCEPTED order: a provisional or refused first-tick request keeps its own refusal path (no further carrier) below.
            var nativeTurnBasedDisposition = RouteBound && !chargeMode && ValidatedRouteFollowed && !ProvisionalOrder &&
                (FirstTickCarrierPermitted == null || FirstTickCarrierPermitted()) && TurnBased.Controllers.CombatController.IsInTurnBasedCombat();
            var approachEnded = delegatedMove.IsFinished || nativeTurnBasedDisposition && MountAgentStoppedAfterCarrierMoved();
            if (approachEnded)
            {
                var finishedMoveAdmission = childAttack.EvaluateCurrentNativeAdmission();
                ObserveNativeAdmission(finishedMoveAdmission);
                if (finishedMoveAdmission != MountedPairNativeAdmissionState.Admitted && nativeTurnBasedDisposition)
                {
                    NativeApproachEndedCount++;
                    var detail = "native turn-based approach ended out of reach (carrier " + (delegatedMove.IsFinished ? "finished" : "live, agent no longer wants to move") +
                        ";admission=" + finishedMoveAdmission + "): TickCommandTurnBased force-finish disposition, no re-path";
                    if (!transaction.IsTerminal) transaction.Cancel("The accepted turn-based approach ended out of reach; native issues no second path.");
                    RecordEndCause(MountedAttackEndCause.Cancelled, detail);
                    Interrupt();
                    return;
                }
                if (finishedMoveAdmission != MountedPairNativeAdmissionState.Admitted)
                {
                    // PR-1 review R1-F7: a sight goal reached without admission is not chosen again.
                    if (currentSightGoal.HasValue) { failedSightGoals.Add(currentSightGoal.Value); currentSightGoal = null; }
                    if (Repath("unadmitted-after-move;admission=" + finishedMoveAdmission) ||
                        ChargeTransactionStopped() || delegatedMove == null)
                    {
                        return;
                    }
                }
            }
        }

        // Returns true when the charge was terminated inside this call.
        private bool DriveDelegatedMoveOnRiderTurn()
        {
            delegatedMoveDrivenByRiderTurnAdapter = true;
            if (!delegatedMove.IsStarted && !delegatedMove.IsFinished)
            {
                delegatedMove.TickApproaching();
                if (delegatedMove.IsUnitEnoughClose && !mount.View.MovementAgent.IsReallyMoving)
                {
                    delegatedMove.Start();
                }
            }

            if (delegatedMove.IsRunning)
            {
                delegatedMoveTickCount++;
                delegatedMove.Tick();
            }

            if (chargeMode && chargeLease != null)
            {
                // Every tick here: the lane is read only when this tick will force a line.
                if (TerminateChargeIfRevalidationFails(MountedChargeRevalidationPhase.BeforeRepath,
                    chargeLease.NeedsReforce(delegatedMove != null && !delegatedMove.IsFinished)))
                {
                    return true;
                }

                if (ObserveChargeStep(MountedChargeTransactionStep.RepathRevalidation))
                {
                    return true;
                }

                chargeLease.Maintain(delegatedMove != null && !delegatedMove.IsFinished);
            }

            return false;
        }

        private string DescribeRepath(string cause)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var turn = Kingmaker.Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            return "repath=" + (transaction.RepathCount + 1) + ";" + cause + ";ticks=" + delegatedMoveTickCount +
                ";moved=" + GeometryUtils.MechanicsDistance(delegatedMoveOrigin, mount.Position).ToString("0.###", culture) +
                ";targetDistance=" + mount.DistanceTo(attackTarget).ToString("0.###", culture) +
                ";forceMode=" + (chargeLease?.ForceMode.ToString() ?? "<none>") +
                ";moveResult=" + (delegatedMove == null ? "<none>" : delegatedMove.Result.ToString()) +
                ";riderMove=" + rider.CombatState.Cooldown.MoveAction.ToString("0.###", culture) +
                ";riderStandard=" + rider.CombatState.Cooldown.StandardAction.ToString("0.###", culture) +
                ";mountMove=" + mount.CombatState.Cooldown.MoveAction.ToString("0.###", culture) +
                (turn == null ? ";turn=<none>" : ";turn=" + turn.Unit.UniqueId + ";turnTimeMoved=" + turn.TimeMoved.ToString("0.###", culture) +
                    ";turnStepMetres=" + turn.MetersMovedByFiveFootStep.ToString("0.###", culture));
        }

        // Returns true when the charge was terminated inside this call.
        private bool Repath(string cause, bool targetDrift = false)
        {
            // A repath re-forces the straight charge line, so the mutable conditions are re-read first.
            if (TerminateChargeIfRevalidationFails(MountedChargeRevalidationPhase.BeforeRepath))
            {
                return true;
            }

            var chaseDistance = mount.DistanceTo(attackTarget);
            var progressing = MountedChaseProgressPolicy.IsProgressingChase(chargeMode, targetDrift, lastChaseDistance, chaseDistance);
            lastChaseDistance = chaseDistance;
            repathObservations.Add(DescribeRepath(cause) + ";progressing=" + progressing);
            if (!transaction.TryRepath(attackTarget.UniqueId, progressing))
            {
                throw new ApproachFailure("Mounted pair command exhausted its bounded repath allowance.");
            }
            StopDelegatedMove(false);
            // A repath release is not the attack release: ownership ends and the approach cycle may
            // legally begin again.
            if (chargeMode)
            {
                chargeSequence.ObserveCarrierReleasedForRepath();
            }

            targetSnapshot = attackTarget.Position;
            return BeginDelegatedMove();
        }

        // Returns true when the charge was terminated inside this call.
        private bool BeginDelegatedMove()
        {
            if (childAttack == null || childAttack.DelegatedMoveApproachRadius < 0f)
            {
                throw new InvalidOperationException("Mounted pair attack radius is unavailable.");
            }
            if (!MountedCombatSpatialPolicy.CanAdmitDelegatedMove(mount.Commands?.Raw,
                (int)CommandType.Standard, (int)CommandType.Move, this, actionActor == mount,
                IsStarted && !IsFinished && transaction.State == MountedCombatTransactionState.Approaching,
                mount.Commands != null && mount.Commands.Queue.Count == 0,
                mount.Commands?.GroupCommand == null, mount.Commands?.PreviousCommand == null))
            {
                throw new InvalidOperationException("Mount approach conflicts with an existing command or queue.");
            }
            delegatedMoveApproachRadius = childAttack.DelegatedMoveApproachRadius;
            delegatedMove = NewApproachMove(delegatedMoveApproachRadius);
            if (chargeMode)
            {
                // The native turn-based approach would request its own path and so drop the forced line, the cause of the
                // preview.176 failure: the stock charge holds no approaching move at all. The lease alone drives this carrier.
                if (TurnBased.Controllers.CombatController.IsInTurnBasedCombat()) delegatedMove.NextApproachTime = TimeSpan.MaxValue;
                chargeCarriers.Add(new KeyValuePair<UnitCommands, UnitMoveTo>(mount.Commands, delegatedMove));
                chargeCarrierView = mount.View;
                chargeCarrierAgent = mount.View?.AgentASP;
                chargeCarrierMotionOwned = true;
            }
            // Review R4-F1: every carrier of a click-admitted turn-based order follows a route revalidated from the current origin
            // and grant (native TickApproaching follows a ForcedPath before any visualizer path); when no proof exists, or native
            // would complete the carrier at once without admitting the strike (review R6-F1), the approach ends cleanly instead
            // of falling back to native routing.
            var routeBoundCarrier = false;
            if (RouteBound && !chargeMode && TurnBased.Controllers.CombatController.IsInTurnBasedCombat())
            {
                var plansDisagree = ClickPlanDisagreement();
                if (plansDisagree != null)
                {
                    RouteProofExpiredCount++;
                    delegatedMove = null;
                    if (!transaction.IsTerminal) transaction.Cancel("The admitted turn-based order's attack plan no longer matches its click (" + plansDisagree + ").");
                    RecordEndCause(MountedAttackEndCause.Cancelled, plansDisagree);
                    Interrupt();
                    return false;
                }
                if (FirstTickCarrierPermitted != null && !FirstTickCarrierPermitted())
                {
                    RouteProofExpiredCount++;
                    delegatedMove = null;
                    if (!transaction.IsTerminal) transaction.Cancel("The first-tick decision for this order was refused; no further carrier is issued.");
                    RecordEndCause(MountedAttackEndCause.Cancelled, "first-tick request refused");
                    Interrupt();
                    return false;
                }
                RouteRevalidationCount++;
                var approach = RouteRevalidator?.Invoke(CurrentPlanIdentity());
                routeBoundCarrier = true;
                var route = approach?.Route;
                var arrived = route != null && route.Count >= 2 && approach.ArrivalRadius > 0f && !route[0].Equals(route[route.Count - 1]) &&
                    Kingmaker.Utility.GeometryUtils.SqrMechanicsDistance(route[route.Count - 1], mount.Position) <= approach.ArrivalRadius * approach.ArrivalRadius;
                if (arrived) RouteArrivalContradictionCount++;
                if (route == null || route.Count < 2 || !(approach.ArrivalRadius > 0f) || arrived)
                {
                    RouteProofExpiredCount++;
                    delegatedMove = null;
                    if (!transaction.IsTerminal) transaction.Cancel("The admitted turn-based approach is no longer a legal route within the mount's movement.");
                    RecordEndCause(MountedAttackEndCause.Cancelled, "validated approach expired");
                    Interrupt();
                    return false;
                }
                var endpoint = route[route.Count - 1];
                delegatedMoveApproachRadius = approach.ArrivalRadius;
                delegatedMove = new UnitMoveTo(endpoint, approach.ArrivalRadius)
                {
                    CreatedByPlayer = true,
                    ShowTargetMarker = false,
                    NeedLoS = false
                };
                delegatedMove.ForcedPath = new Pathfinding.ForcedPath(new System.Collections.Generic.List<Vector3>(route));
                ValidatedRouteFollowed = true;
                carrierApproach = approach;
                carrierApproachMove = delegatedMove;
            }
            AnnounceOwnMove(delegatedMove);
            delegatedMoveStartCount++;
            delegatedMoveOrigin = mount.Position;
            delegatedMoveDrivenByStockController =
                !TurnBased.Controllers.CombatController.IsInTurnBasedCombat() ||
                Kingmaker.Game.Instance?.TurnBasedCombatController?.CurrentTurn?.Unit == mount;
            mountMoveSlotRestoredAfterApproach = false;
            // Native Run pairs Move/Standard for interruption. During this one exact
            // admission, preserve this live approach owner while all other native
            // admission, targeting and command callbacks continue unchanged.
            admittingDelegatedMove = true;
            try { mount.Commands.Run(delegatedMove); }
            finally { admittingDelegatedMove = false; }
            // The rider wrapper must still be the exact parent of this admission.
            if (IsFinished || actionActor.Commands.Standard != this)
                throw new InvalidOperationException("Attack owner was replaced during approach admission.");
            if (ObserveChargeStep(MountedChargeTransactionStep.CarrierAdmitted))
            {
                return true;
            }

            // Exact carrier ownership is established BEFORE any lease mutation exists, because the lease
            // forces a path onto this carrier and a forced path lives only as long as the carrier does.
            delegatedMoveExecutorId = delegatedMove.Executor?.UniqueId;
            delegatedMoveExecutorIsExactMount &= delegatedMove.Executor == mount;
            var admittedMoveSlot = mount.Commands.GetCommand(UnitCommand.CommandType.Move);
            delegatedMoveOwnedByMountMoveSlot &=
                admittedMoveSlot == delegatedMove && mount.Commands.Contains(delegatedMove);
            delegatedMoveNeverQueuedOnMount &= !mount.Commands.Queue.Contains(delegatedMove);
            mountQueueEmptyThroughoutApproach &= mount.Commands.Queue.Count == 0;
            if (!delegatedMoveExecutorIsExactMount || !delegatedMoveOwnedByMountMoveSlot ||
                !delegatedMoveNeverQueuedOnMount || !mountQueueEmptyThroughoutApproach)
            {
                throw new InvalidOperationException(
                    "Exact delegated mount movement did not acquire only the active Move slot.");
            }
            // PR #70 R1-F6 (rounds 7-8): only now, with the exact carrier holding the mount's Move slot, is its first-tick decision bound to it.
            // Round 8 (R7-F1): every route-bound turn-based carrier is bound, whatever mode its revalidation saw, so the movement mode in force
            // at its first debit decides.
            if (routeBoundCarrier && !chargeMode) FirstTickCarrierIssued?.Invoke(delegatedMove);

            if (ObserveChargeStep(MountedChargeTransactionStep.CarrierOwnershipProven))
            {
                return true;
            }

            if (chargeMode)
            {
                // Preview.156/157: a forced path lives only while the mover holds a live command, so the lease
                // rides on this admitted carrier. A repath re-begins the carrier and the straight line is
                // re-forced onto it, which is what the stock charge does when its target has moved.
                if (chargeLease == null)
                {
                    // The initial in-transaction revalidation, before a single mutation exists.
                    if (TerminateChargeIfRevalidationFails(MountedChargeRevalidationPhase.BeforeRepath))
                    {
                        return true;
                    }

                    if (ObserveChargeStep(MountedChargeTransactionStep.InitialRevalidation))
                    {
                        return true;
                    }

                    var pending = new MountedChargeLease(rider, mount, attackTarget, logger) { ForeignMoverActive = ForeignPathActive };
                    try
                    {
                        pending.Apply();
                    }
                    catch (Exception)
                    {
                        // The lease has already returned every mutation it completed. The carrier it was
                        // applied on top of is this command's to terminate exactly: it is admitted and live
                        // on the mount's Move slot, and a forced path survives only as long as it does.
                        chargeLease = pending;
                        chargeLeaseApplicationFailed = true;
                        chargeLeaseApplicationFailure = pending.ApplyFailureReason ?? "<none>";
                        chargeLeaseApplicationFailedStep = pending.ApplyFailedStep;
                        chargeLeaseRolledBackOnFailure = pending.ApplyRolledBack;
                        StopDelegatedMove(false);
                        carrierTerminatedAfterLeaseFailure = delegatedMove == null;
                        TryLogInfo("Mounted charge lease application failed; carrier terminated exactly: " +
                            "step=" + (chargeLeaseApplicationFailedStep ?? "<none>") +
                            "; reason=" + chargeLeaseApplicationFailure +
                            "; rolledBack=" + chargeLeaseRolledBackOnFailure +
                            "; carrierTerminated=" + carrierTerminatedAfterLeaseFailure +
                            "; forcedPathOutstanding=" + pending.ForcedPathOutstanding + ".");
                        throw;
                    }

                    chargeLease = pending;
                    if (ObserveChargeStep(MountedChargeTransactionStep.LeaseApplied))
                    {
                        return true;
                    }
                }
                else
                {
                    // A re-force onto a newly begun carrier: the same conditions are re-read first.
                    if (TerminateChargeIfRevalidationFails(MountedChargeRevalidationPhase.BeforeRepath))
                    {
                        return true;
                    }

                    if (ObserveChargeStep(MountedChargeTransactionStep.RepathRevalidation))
                    {
                        return true;
                    }

                    chargeLease.Maintain(true);
                }
            }

            // The ownership proofs ran before the lease was applied, which is the point: nothing is
            // mutated until the exact carrier is established.
            return false;
        }

        private bool SuccessorSteersMount(UnitCommands commands, UnitMoveTo exactMove) => MountedCarrierCleanup.OtherCommandSteers(commands, exactMove, this);

        private void StopDelegatedMove(bool requireSuccess)
        {
            if (delegatedMove == null)
            {
                return;
            }

            if (chargeMode)
            {
                var result = delegatedMove.Result;
                if (requireSuccess && result != ResultType.Success)
                    throw new InvalidOperationException("Exact charge carrier did not finish successfully.");
                delegatedMoveFinishedSuccessfully |= result == ResultType.Success;
                TryDrainChargeCarrier();
                return;
            }

            var exactMove = delegatedMove;
            // PR #70 R6-F7 (round 11): the exact carrier's cleanup progress is recorded on this command, whichever caller runs it (native
            // interruption's OnEnded, the order's own release, the controller's sweep), so a fault leaves the unfinished steps owned and a resume
            // (SettleCarrierCleanup) does only those, with the same successor checks.
            if (cleanup == null || !ReferenceEquals(cleanup.Carrier, exactMove)) cleanup = new MountedCarrierCleanup(exactMove, mount);
            var commands = mount.Commands;
            var rawMoveSlot = commands?.GetCommand(UnitCommand.CommandType.Move);
            var exactSlotOrStockRemoved = commands != null &&
                MountedCombatSpatialPolicy.IsExactRawMoveSlotLifecycle(
                    rawMoveSlot == exactMove,
                    rawMoveSlot == null,
                    exactMove.IsFinished);
            mountMoveSlotUnreplacedThroughoutApproach &= exactSlotOrStockRemoved;
            mountQueueEmptyThroughoutApproach &= commands != null && commands.Queue.Count == 0;
            if (!exactMove.IsFinished)
            {
                exactMove.Interrupt(false);
            }
            cleanup.Interrupted = true;
            if (requireSuccess && exactMove.Result != ResultType.Success)
            {
                throw new InvalidOperationException(
                    "Exact delegated Mammoth move did not finish successfully before rider attack admission.");
            }
            delegatedMoveFinishedSuccessfully |= exactMove.Result == ResultType.Success;
            cleanup.SettleSlot();
            mountMoveSlotRestoredAfterApproach = commands != null &&
                commands.GetCommand(UnitCommand.CommandType.Move) == null &&
                !commands.Contains(exactMove) && commands.Queue.Count == 0;
            cleanup.SettleMotion(this);
            delegatedMove = null;
        }

        // A restriction can reach the action actor after its order was queued or while the mount approaches. Native
        // interrupts a running command of such an actor on its next tick in either combat mode; ending the transaction
        // at this boundary releases no attack first and names the cause. Returns true when it terminated here.
        private bool TerminateIfActionActorBlocked()
        {
            if (IsFinished || transaction.IsTerminal) return false;
            var block = MountedActorCapabilityPolicy.ExecutionBlock(NativeActorCapability.Read(actionActor));
            if (block == MountedActorCapabilityBlock.None) return false;
            transaction.Cancel("Action actor cannot attack: " + block);
            RecordEndCause(MountedAttackEndCause.ActorCapability, block.ToString(), block);
            Interrupt();
            return true;
        }

        private void StartChildAttack()
        {
            if (chargeCleanupRequested) { Interrupt(false); return; }
            // Review R4-F2: the last boundary before release.
            if (EndIfAutomaticOrderNotAdmitted()) return;
            if (EndIfSupersededBeforeDelivery()) return;
            if (TerminateIfActionActorBlocked()) return;
            // PR #70 R1-F9: a click-admitted order strikes only with the plan its click proved, also when it reaches the strike without a
            // carrier (in range at dispatch, or the target came into range before dispatch).
            var plansDisagree = ClickPlanDisagreement();
            if (plansDisagree != null)
            {
                RouteProofExpiredCount++;
                if (!transaction.IsTerminal) transaction.Cancel("The admitted turn-based order's attack plan changed after its click (" + plansDisagree + ").");
                RecordEndCause(MountedAttackEndCause.Cancelled, plansDisagree);
                Interrupt();
                return;
            }
            // PR #70 R1-F6 (round 7), R7-F2 (round 8): a provisional order strikes only once the controller committed it (in place, when no tick
            // decided it), and only after the three plans agreed above. R9-F2 (round 10): an accepted route-bound order's own first-tick request
            // is asked too (the current in-place admission, the rider's entitlement included); a refused one starts no child.
            if (ProvisionalOrder ? ProvisionalStrikeGate == null || !ProvisionalStrikeGate() || ProvisionalOrder : ProvisionalStrikeGate != null && !ProvisionalStrikeGate())
            {
                if (!IsFinished)
                {
                    if (!transaction.IsTerminal) transaction.Cancel("The provisional turn-based order was not accepted before its strike.");
                    RecordEndCause(MountedAttackEndCause.Cancelled, "provisional order not accepted");
                    Interrupt();
                }
                return;
            }
            if (!childAttack.TryPrepareNativeStartAdmission())
            {
                throw new InvalidOperationException("Native child attack failed the bounded Mammoth-origin admission bridge.");
            }
            ObserveNativeAdmission(childAttack.NativeAdmissionStateAtStart);
            pairDistanceAtAttackStart = childAttack.PairDistanceAtNativeStart;
            riderDisplacementAtAttackStart = HorizontalDistance(riderPositionAtCommandStart, rider.Position);
            mountDisplacementAtAttackStart = HorizontalDistance(mountPositionAtCommandStart, mount.Position);
            targetDisplacementAtAttackStart = HorizontalDistance(targetPositionAtCommandStart, attackTarget.Position);
            StopDelegatedMove(false);
            mount.ForceLookAt(attackTarget.Position);
            childAttack.TurnToTarget();
            // UnitAttack.OnStart re-evaluates the full attack plan against the
            // actual actor state after approach. UnitActionController observes
            // this command's first native Act and owns its sole cooldown charge.
            // The last boundary: the carrier has released the mount Move slot and the native attack has
            // not started. A charge that is no longer valid terminates here, without an attack.
            if (TerminateChargeIfRevalidationFails(MountedChargeRevalidationPhase.BeforeAttackStart))
            {
                return;
            }

            if (ObserveChargeStep(MountedChargeTransactionStep.AttackStartRevalidation))
            {
                return;
            }

            if (chargeMode)
            {
                IsCharge = true;
            }

            NeedLoS = true;
            // PR-1 review R2-F3: the attack phase gets its own ordinary bound: the clock restarts and no approach allowance or wait
            // is carried into it.
            SetTimeSinceStart(0f);
            approachAllowanceSeconds = 0f;
            if (ObserveChargeStep(MountedChargeTransactionStep.AttackStarted))
            {
                return;
            }

            base.OnStart();
            if (!childAttack.IsRunning || !transaction.TryStartSingleAttack(attackTarget.UniqueId))
            {
                throw new InvalidOperationException("Native child attack did not start exactly once.");
            }
        }

        private void CreateAndValidateChildAttack()
        {
            if (action == MountedCombatActionKind.MountPrimaryNatural)
            {
                horsePrimaryAttackAnimation.SupplyExact(this, childAttack.PlannedAttack, mount);
            }
            if (childAttack.PlannedAttack == null || childAttack.AllAttacks.Count == 0 ||
                IsSingleAttack && (childAttack.IsFullAttack || childAttack.AllAttacks.Count != 1))
            {
                throw new InvalidOperationException("Native attack plan was empty or violated the explicit single-attack mode.");
            }
            if (childAttack.PlannedAttack.Weapon == null)
            {
                throw new InvalidOperationException("Mounted combat rejected a missing planned weapon.");
            }
            var plannedRanged = childAttack.PlannedAttack.Weapon.Blueprint.IsRanged;
            if (action == MountedCombatActionKind.RiderRanged && !plannedRanged ||
                action != MountedCombatActionKind.RiderRanged && plannedRanged)
            {
                throw new InvalidOperationException(
                    "Mounted combat planned weapon did not match the exact melee/ranged action kind.");
            }
            retainedAttackWeaponBlueprintId = childAttack.PlannedAttack.Weapon.Blueprint.AssetGuid;
            retainedAttackWeaponIsNatural = childAttack.PlannedAttack.Weapon.Blueprint.IsNatural;
            retainedAttackWeaponIsRanged = childAttack.PlannedAttack.Weapon.Blueprint.IsRanged;
            retainedAttackWeaponTypeBlueprintId =
                childAttack.PlannedAttack.Weapon.Blueprint.Type?.AssetGuid ?? "<none>";
            ammunitionStateBefore = DescribeOptionalRangedState(
                childAttack.PlannedAttack.Weapon,
                "ammo",
                "ammunition");
            reloadStateBefore = DescribeOptionalRangedState(
                childAttack.PlannedAttack.Weapon,
                "reload");
            if (action == MountedCombatActionKind.MountPrimaryNatural)
            {
                if (expectedMountPrimary?.Weapon?.Blueprint == null ||
                    !NativePrimaryNaturalAttackPolicy.IsExact(
                        expectedMountPrimary.Kind,
                        expectedMountPrimary.AdditionalLimbIndex,
                        expectedMountPrimary.Weapon.Blueprint.IsNatural,
                        expectedMountPrimary.Weapon.Blueprint.IsRanged) ||
                    expectedMountPrimary.Slot == null ||
                    childAttack.PlannedAttack.Hand != expectedMountPrimary.Slot ||
                    childAttack.PlannedAttack.Weapon != expectedMountPrimary.Weapon)
                {
                    throw new InvalidOperationException("Native mount attack was not the exact primary natural attack selected by stock single-attack order.");
                }
            }
        }

        private string preStartInterruptBoundary;

        internal void ObservePreStartInterrupt()
        {
            if (preStartInterruptBoundary != null || IsStarted || IsFinished ||
                !KingmakerMountedCombat.Diagnostics.RuntimeAutomationHost.ShouldCaptureMountedPreStartInterruption(relationship)) { return; }
            var game = Kingmaker.Game.Instance;
            var turn = game?.TurnBasedCombatController?.CurrentTurn;
            preStartInterruptBoundary = "frame=" + Time.frameCount + ";turn=" + turn?.Unit?.UniqueId +
                ";status=" + turn?.Status + ";mode=" + game?.CurrentMode + ";paused=" + game?.IsPaused +
                ";owner=" + Executor?.UniqueId + ";standard=" + Executor?.CombatState?.Cooldown.StandardAction +
                ";move=" + Executor?.CombatState?.Cooldown.MoveAction + ";source=" +
                string.Join(" <- ", (new System.Diagnostics.StackTrace(1, false).GetFrames() ?? new System.Diagnostics.StackFrame[0])
                    .Take(10).Select(frame => frame.GetMethod()?.DeclaringType?.FullName + "." + frame.GetMethod()?.Name).ToArray());
            TryLogInfo("Mounted pre-start interruption: " + preStartInterruptBoundary);
        }

        private void RequireLiveExactPair()
        {
            var riderState = rider?.Descriptor?.State;
            var mountState = mount?.Descriptor?.State;
            var liveness = new MountedPairLivenessSnapshot(
                relationship.State == RelationshipState.Mounted && relationship.IsCurrentPairOwner(pairOwner),
                relationship.Rider == rider,
                relationship.Mount == mount,
                Executor == actionActor,
                true, // Target lifecycle is evaluated separately by TryEndExpectedTargetInvalidation.
                rider != null && rider.IsInState,
                mount != null && mount.IsInState,
                riderState != null && riderState.IsConscious,
                mountState != null && mountState.IsConscious,
                true,
                riderState != null && !riderState.IsFinallyDead,
                mountState != null && !mountState.IsFinallyDead,
                true,
                true,
                true);
            if (!liveness.AllPassed)
            {
                throw new InvalidOperationException(
                    "Exact mounted pair invariant failed: " + liveness.FailureSummary + ".");
            }
        }

        private void ObserveApproachInvariants()
        {
            approachObservationCount++;
            var actionCommands = actionActor.Commands;
            wrapperCommandRetainedThroughoutApproach &= actionCommands != null &&
                (actionCommands.Contains(this) || actionCommands.Queue.Contains(this));
            if (delegatedMove != null)
            {
                delegatedMoveExecutorIsExactMount &= delegatedMove.Executor == mount;
                var rawMoveSlot = mount.Commands.GetCommand(UnitCommand.CommandType.Move);
                var exactSlotOrStockRemoved = MountedCombatSpatialPolicy.IsExactRawMoveSlotLifecycle(
                    rawMoveSlot == delegatedMove,
                    rawMoveSlot == null,
                    delegatedMove.IsFinished);
                mountMoveSlotUnreplacedThroughoutApproach &= exactSlotOrStockRemoved;
                delegatedMoveNeverQueuedOnMount &=
                    !mount.Commands.Queue.Contains(delegatedMove);
                mountQueueEmptyThroughoutApproach &= mount.Commands.Queue.Count == 0;
                var mountAgent = mount.View?.AgentASP;
                if (mountAgent != null &&
                    (mountAgent.WantsToMove || mountAgent.IsReallyMoving ||
                     HorizontalDistance(mountPositionAtCommandStart, mount.Position) >
                        MountedCombatSpatialPolicy.RangeTolerance))
                {
                    delegatedMoveProgressObservationCount++;
                }
            }
            riderStockAgentSuppressedThroughoutApproach &= rider.View?.AgentASP != null &&
                !rider.View.AgentASP.enabled && rider.View.AgentASP.AvoidanceDisabled;
            mountStockAgentAuthoritativeThroughoutApproach &= mount.View?.AgentASP != null &&
                mount.View.AgentASP.enabled && !mount.View.AgentASP.AvoidanceDisabled;
            poseHealthyThroughoutApproach &= relationship.Runtime.PoseHealthy &&
                relationship.Runtime.PoseFrameApplied;
        }

        // Review of 2ae4cc8b (finding 2): the terminal callback is always delivered once. A capability reading that fails, a
        // logger that throws, or an outcome that cannot be fully described leaves a minimal outcome, never no terminal.
        private void ReportTerminalOnce()
        {
            if (terminalReported)
            {
                return;
            }
            var capabilityBlock = MountedActorCapabilityBlock.None;
            string actorName = null;
            try
            {
                capabilityBlock = MountedActorCapabilityPolicy.ExecutionBlock(NativeActorCapability.Read(actionActor));
                actorName = actionActor.CharacterName;
            }
            catch (Exception exception)
            {
                TryLogException("Mounted pair terminal capability reading", exception);
            }
            MountedPairAttackOutcome outcome;
            try { outcome = DescribeOutcome(capabilityBlock, actorName); }
            catch (Exception exception)
            {
                TryLogException("Mounted pair terminal description", exception);
                outcome = new MountedPairAttackOutcome
                {
                    Action = action,
                    TransactionState = transaction?.State.ToString(),
                    TransactionCompleted = transaction != null && transaction.State == MountedCombatTransactionState.Completed,
                    NativeCommandStarted = IsStarted,
                    ActionActorName = actorName,
                    ActorCapabilityBlockAtTerminal = capabilityBlock,
                    EndCause = endCause,
                    EndCauseDetail = endCauseDetail,
                    EndCauseCapabilityBlock = endCauseCapabilityBlock,
                    NativeTerminalBranch = nativeTerminalBranch,
                    NativeTerminalCapabilityBlock = nativeTerminalBlock,
                    Result = Result.ToString(),
                    ApproachRequiredAtStart = approachRequiredAtStart,
                    DelegatedMoveStartCount = delegatedMoveStartCount,
                    TerminalReason = transaction?.TerminalReason
                };
            }
            // Review of fa533e97 (R20): delivery is marked once and never retried, because a partly applied handler must not
            // run twice. The controller's handler settles ownership, retirement and the outcome before anything fallible and
            // contains every diagnostic, so nothing is expected to escape; anything unexpected is contained here, and the
            // controller's finished-command sweep still owns the finished command.
            // PR-1 review R4-F6: the actor and its Standard charges are assigned to the outcome actually delivered, on both paths.
            try { AttachActorCharges(outcome); }
            catch (Exception exception) { TryLogException("Mounted pair terminal attribution", exception); }
            terminalReported = true;
            try { terminal(this, outcome); }
            catch (Exception exception) { TryLogException("Mounted pair terminal handler", exception); }
        }

        private MountedPairAttackOutcome DescribeOutcome(MountedActorCapabilityBlock capabilityBlock, string actorName)
        {
            return new MountedPairAttackOutcome
            {
                Action = action,
                TransactionState = transaction.State.ToString(),
                TransactionCompleted = transaction.State == MountedCombatTransactionState.Completed,
                NativeCommandStarted = IsStarted,
                ActionActorName = actorName,
                ActorCapabilityBlockAtTerminal = capabilityBlock,
                EndCause = endCause,
                EndCauseDetail = endCauseDetail,
                EndCauseCapabilityBlock = endCauseCapabilityBlock,
                NativeTerminalBranch = nativeTerminalBranch,
                NativeTerminalCapabilityBlock = nativeTerminalBlock,
                // A completed attack needs no explanation; every other terminal records what native saw of its actor.
                ActorCapabilityAtTerminal = transaction.State == MountedCombatTransactionState.Completed
                    ? null
                    : NativeActorCapability.Describe(actionActor, this),
                ActorId = actionActor.UniqueId,
                CommandOwnerId = Executor?.UniqueId,
                ResourceOwnerId = actionActor.UniqueId,
                TargetId = attackTarget.UniqueId,
                Result = Result.ToString(),
                NativeRangedTailTermination = NativeRangedTailTermination,
                ChildAttackStartCount = transaction.ChildAttackStartCount,
                SingleAttackMode = IsSingleAttack,
                NativeFullAttack = IsFullAttack,
                NativePlannedAttackCount = AllAttacks.Count,
                NativeCompletedAttackCount = GetAttackIndex(),
                RepathCount = transaction.RepathCount,
                ProgressingRepathCount = transaction.ProgressingRepathCount,
                ContinuationTargetCount = transaction.ContinuationTargetCount,
                InitialTargetId = initialTargetId,
                ContinuationTargetIds = string.Join(",", continuationTargetIds.ToArray()),
                RepathObservations = string.Join(" | ", repathObservations.ToArray()),
                // PR-1 review R4-F6: native spends the Standard at the first native act (OnAction); a force-finish marks IsActed
                // without it and spends nothing (DescribeStandardCharge).

                NativeAttackRuleObserved = childAttack?.LastAttackRule != null,
                AttackWeaponBlueprintId = retainedAttackWeaponBlueprintId,
                AttackWeaponIsNatural = retainedAttackWeaponIsNatural,
                AttackWeaponIsRanged = retainedAttackWeaponIsRanged,
                AttackWeaponSlot = action == MountedCombatActionKind.MountPrimaryNatural
                    ? expectedMountPrimary?.Kind.ToString()
                    : action == MountedCombatActionKind.RiderRanged
                        ? "EquippedRanged"
                        : "EquippedMelee",
                AttackWeaponTypeBlueprintId = retainedAttackWeaponTypeBlueprintId,
                AmmunitionStateBefore = ammunitionStateBefore,
                AmmunitionStateAfter = childAttack?.PlannedAttack?.Weapon == null
                    ? "<unavailable>"
                    : DescribeOptionalRangedState(
                        childAttack.PlannedAttack.Weapon,
                        "ammo",
                        "ammunition"),
                ReloadStateBefore = reloadStateBefore,
                ReloadStateAfter = childAttack?.PlannedAttack?.Weapon == null
                    ? "<unavailable>"
                    : DescribeOptionalRangedState(
                        childAttack.PlannedAttack.Weapon,
                        "reload"),
                TerminalReason = transaction.TerminalReason,
                PreStartInterruptBoundary = preStartInterruptBoundary,
                PairRangeSatisfiedAtStart = childAttack != null && childAttack.PairRangeSatisfiedAtNativeStart,
                PairDistanceAtStart = childAttack?.PairDistanceAtNativeStart ?? 0f,
                PairApproachRadiusAtStart = childAttack?.PairApproachRadius ?? 0f,
                NativeExecutorDistanceAtStart = childAttack?.NativeExecutorDistanceAtStart ?? 0f,
                NativeAdmissionRadiusAtStart = childAttack?.NativeAdmissionRadiusAtStart ?? 0f,
                NativeAdmissionAdjusted = childAttack != null && childAttack.NativeAdmissionAdjustedAtStart,
                InitialNativeAdmissionState = initialNativeAdmissionState.ToString(),
                NativeAdmissionStateAtStart = childAttack?.NativeAdmissionStateAtStart.ToString(),
                NativeDistanceSatisfiedAtStart = childAttack != null &&
                    childAttack.NativeDistanceSatisfiedAtStart,
                NativeLineOfSightRecoveryObserved = nativeLineOfSightRecoveryObserved,
                ApproachRequiredAtStart = approachRequiredAtStart,
                DelegatedMoveStartCount = delegatedMoveStartCount,
                DelegatedMoveApproachRadius = delegatedMoveApproachRadius,
                DelegatedMoveTickCount = delegatedMoveTickCount,
                DelegatedMoveExecutorId = delegatedMoveExecutorId,
                DelegatedMoveExecutorIsExactMount = delegatedMoveExecutorIsExactMount,
                WrapperCommandRetainedThroughoutApproach = wrapperCommandRetainedThroughoutApproach,
                DelegatedMoveNeverQueuedOnMount = delegatedMoveNeverQueuedOnMount,
                DelegatedMoveOwnedByMountMoveSlot = delegatedMoveOwnedByMountMoveSlot,
                MountMoveSlotUnreplacedThroughoutApproach = mountMoveSlotUnreplacedThroughoutApproach,
                MountQueueEmptyThroughoutApproach = mountQueueEmptyThroughoutApproach,
                DelegatedMoveFinishedSuccessfully = delegatedMoveFinishedSuccessfully,
                DelegatedMoveStoppedAtLegalRange = delegatedMoveStoppedAtLegalRange,
                DelegatedMoveResultBeforeLegalRangeStop = delegatedMoveResultBeforeLegalRangeStop,
                DelegatedMovePairDistanceAtLegalRangeStop = delegatedMovePairDistanceAtLegalRangeStop,
                MountMoveSlotRestoredAfterApproach = mountMoveSlotRestoredAfterApproach,
                DelegatedMoveDrivenByStockController = delegatedMoveDrivenByStockController,
                DelegatedMoveDrivenByRiderTurnAdapter = delegatedMoveDrivenByRiderTurnAdapter,
                DelegatedMoveProgressObservationCount = delegatedMoveProgressObservationCount,
                RiderStockAgentSuppressedThroughoutApproach = riderStockAgentSuppressedThroughoutApproach,
                MountStockAgentAuthoritativeThroughoutApproach = mountStockAgentAuthoritativeThroughoutApproach,
                PoseHealthyThroughoutApproach = poseHealthyThroughoutApproach,
                ApproachObservationCount = approachObservationCount,
                InitialPairDistance = initialPairDistance,
                PairDistanceAtAttackStart = pairDistanceAtAttackStart,
                RiderDisplacementAtAttackStart = riderDisplacementAtAttackStart,
                MountDisplacementAtAttackStart = mountDisplacementAtAttackStart,
                TargetDisplacementAtAttackStart = targetDisplacementAtAttackStart,
                AttackAnimationHandleCreated = horsePrimaryAnimationHandle != null,
                AttackAnimationHandleSource = horsePrimaryAnimationHandleSource,
                AttackAnimationActionName = horsePrimaryAnimationActionName,
                AttackAnimationActionType = horsePrimaryAnimationActionType,
                AttackAnimationActed = horsePrimaryAnimationHandle != null && horsePrimaryAnimationHandle.IsActed,
                AttackAnimationFinished = horsePrimaryAnimationHandle != null && horsePrimaryAnimationHandle.IsFinished,
                AttackAnimationInterrupted = horsePrimaryAnimationHandle != null && horsePrimaryAnimationHandle.IsInterrupted
            };
        }

        private void ObserveNativeAdmission(MountedPairNativeAdmissionState state)
        {
            nativeLineOfSightRecoveryObserved |=
                state == MountedPairNativeAdmissionState.BlockedLineOfSight;
        }

        private static string DescribeOptionalRangedState(object weapon, params string[] terms)
        {
            if (weapon == null || terms == null || terms.Length == 0)
            {
                return "<unavailable>";
            }

            var observations = new List<string>();
            var candidates = new List<object> { weapon };
            var blueprint = OptionalPublicPropertyReader.Read(weapon, "Blueprint");
            if (blueprint != null)
            {
                candidates.Add(blueprint);
                var weaponType = OptionalPublicPropertyReader.Read(blueprint, "Type");
                if (weaponType != null)
                {
                    candidates.Add(weaponType);
                }
            }

            foreach (var candidate in candidates)
            {
                var type = candidate.GetType();
                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(property => property.GetIndexParameters().Length == 0 &&
                        terms.Any(term => property.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                    .Take(16))
                {
                    try
                    {
                        var value = property.GetValue(candidate, null);
                        if (value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum)
                        {
                            observations.Add(type.FullName + "." + property.Name + "=" +
                                (value == null ? "<null>" : value.ToString()));
                        }
                        else
                        {
                            observations.Add(type.FullName + "." + property.Name + "=<" +
                                value.GetType().FullName + ">");
                        }
                    }
                    catch (Exception exception)
                    {
                        observations.Add(type.FullName + "." + property.Name + "=<error:" +
                            exception.GetType().Name + ">");
                    }
                }

                var components = OptionalPublicPropertyReader.Read(candidate, "ComponentsArray") as
                    System.Collections.IEnumerable;
                if (components == null)
                {
                    continue;
                }
                foreach (var component in components)
                {
                    var componentType = component?.GetType();
                    if (componentType != null && terms.Any(term =>
                        componentType.FullName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        observations.Add("component=" + componentType.FullName);
                    }
                }
            }

            return observations.Count == 0
                ? "native-core:no-separate-" + string.Join("-or-", terms) + "-state"
                : string.Join("|", observations.Distinct().Take(32).ToArray());
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            var dx = first.x - second.x;
            var dz = first.z - second.z;
            return Mathf.Sqrt((dx * dx) + (dz * dz));
        }
    }

    // PR #70 R6-F7 / R11-F7 (round 12): one exact carrier's cleanup obligations (interruption, removal from the mount's slots, motion), shared by the
    // command that began it and, after that command's terminal, by the controller that retains the unfinished record. The successor rule is the
    // normal release's: a live Move-slot successor, or an unstarted out-of-range command in another slot (a Standard approaching its target,
    // including one the slot cleanup itself promoted from the queue), keeps its movement; otherwise the motion is stopped once.
    internal sealed class MountedCarrierCleanup
    {
        internal readonly UnitMoveTo Carrier;
        internal readonly UnitEntityData Mount;
        internal bool Interrupted, SlotRemoved, MotionSettled;
        internal MountedCarrierCleanup(UnitMoveTo carrier, UnitEntityData mount) { Carrier = carrier; Mount = mount; }
        internal bool Pending => !(Interrupted && SlotRemoved && MotionSettled);
        internal string State => "interrupted=" + Interrupted + ",slotRemoved=" + SlotRemoved + ",motionSettled=" + MotionSettled;

        internal void SettleSlot()
        {
            if (SlotRemoved) return;
            var commands = Mount?.Commands;
            if (commands != null && commands.Contains(Carrier) && Carrier.IsFinished) commands.RemoveFinishedAndUpdateQueue();
            SlotRemoved = commands == null || !commands.Contains(Carrier);
        }

        internal void SettleMotion(UnitCommand except)
        {
            if (MotionSettled || !SlotRemoved) return;
            var commands = Mount?.Commands;
            var successor = commands?.GetCommand(UnitCommand.CommandType.Move);
            var successorSteers = successor != null && !successor.IsFinished && !ReferenceEquals(successor, Carrier) || OtherCommandSteers(commands, Carrier, except);
            if (!successorSteers) Mount?.View?.StopMoving();
            MotionSettled = true;
        }

        // The unfinished steps only; interruption only while the exact carrier still holds the mount's Move slot. A fault propagates.
        internal bool Settle(UnitCommand except)
        {
            if (!Interrupted)
            {
                var commands = Mount?.Commands;
                if (!Carrier.IsFinished && commands != null && ReferenceEquals(commands.GetCommand(UnitCommand.CommandType.Move), Carrier)) Carrier.Interrupt(false);
                Interrupted = true;
            }
            SettleSlot();
            SettleMotion(except);
            return !Pending;
        }

        internal static bool OtherCommandSteers(UnitCommands commands, UnitMoveTo exactMove, UnitCommand except)
        {
            if (commands == null) return false;
            foreach (var command in commands.Raw)
            {
                if (command == null || command.IsFinished || ReferenceEquals(command, exactMove) || ReferenceEquals(command, except) ||
                    command is UnitMoveTo) continue;
                bool close;
                try { close = command.IsUnitEnoughClose; }
                catch (Exception) { close = false; }
                if (!command.IsStarted && !close) return true;
            }
            return false;
        }
    }
}
