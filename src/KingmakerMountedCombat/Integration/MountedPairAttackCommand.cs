using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

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

        public int ChildAttackStartCount { get; set; }

        public int RepathCount { get; set; }

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

        public bool PairRangeSatisfiedAtStart { get; set; }

        public float PairDistanceAtStart { get; set; }

        public float PairApproachRadiusAtStart { get; set; }

        public float NativeExecutorDistanceAtStart { get; set; }

        public float NativeAdmissionRadiusAtStart { get; set; }

        public bool NativeAdmissionAdjusted { get; set; }

        public bool ApproachRequiredAtStart { get; set; }

        public int DelegatedMoveStartCount { get; set; }

        public int DelegatedMoveTickCount { get; set; }

        public string DelegatedMoveExecutorId { get; set; }

        public bool DelegatedMoveExecutorIsExactMount { get; set; }

        public bool WrapperCommandRetainedThroughoutApproach { get; set; }

        public bool DelegatedMoveNeverQueuedOnMount { get; set; }

        public bool DelegatedMoveOwnedByMountMoveSlot { get; set; }

        public bool MountMoveSlotUnreplacedThroughoutApproach { get; set; }

        public bool MountQueueEmptyThroughoutApproach { get; set; }

        public bool DelegatedMoveFinishedSuccessfully { get; set; }

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

    internal sealed class MountedPairAttackCommand : UnitCommand
    {
        private const float TargetRepathDistance = 0.75f;
        private const float MaximumElapsedSeconds = 8.5f;

        private readonly GameMountedRelationshipService relationship;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly UnitEntityData attackTarget;
        private readonly UnitEntityData actionActor;
        private readonly MountedCombatActionKind action;
        private readonly NativeSingleAttackWeaponSelection expectedMountPrimary;
        private readonly HorsePrimaryAttackAnimationAdapter horsePrimaryAttackAnimation;
        private readonly IModLogger logger;
        private readonly Action<MountedPairAttackCommand, MountedPairAttackOutcome> terminal;
        private readonly bool allowApproach;
        private readonly MountedCombatTransaction transaction = new MountedCombatTransaction();
        private MountedPairSingleAttack childAttack;
        private UnitMoveTo delegatedMove;
        private Vector3 targetSnapshot;
        private string retainedAttackWeaponBlueprintId;
        private bool retainedAttackWeaponIsNatural;
        private bool retainedAttackWeaponIsRanged;
        private string retainedAttackWeaponTypeBlueprintId;
        private string ammunitionStateBefore;
        private string reloadStateBefore;
        private bool terminalReported;
        private bool approachRequiredAtStart;
        private int delegatedMoveStartCount;
        private int delegatedMoveTickCount;
        private string delegatedMoveExecutorId;
        private bool delegatedMoveExecutorIsExactMount = true;
        private bool wrapperCommandRetainedThroughoutApproach = true;
        private bool delegatedMoveNeverQueuedOnMount = true;
        private bool delegatedMoveOwnedByMountMoveSlot = true;
        private bool mountMoveSlotUnreplacedThroughoutApproach = true;
        private bool mountQueueEmptyThroughoutApproach = true;
        private bool delegatedMoveFinishedSuccessfully;
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
            bool allowApproach = true)
            : base(CommandType.Standard, target)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            attackTarget = target ?? throw new ArgumentNullException(nameof(target));
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

        internal UnitMoveTo DelegatedMove => delegatedMove;

        internal UnitEntityData Rider => rider;

        internal UnitEntityData Mount => mount;

        internal UnitEntityData ActionActor => actionActor;

        internal MountedCombatActionKind Action => action;

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
            try
            {
                RequireLiveExactPair();
                CreateAndValidateChildAttack();
                riderPositionAtCommandStart = rider.Position;
                mountPositionAtCommandStart = mount.Position;
                targetPositionAtCommandStart = attackTarget.Position;
                initialPairDistance = HorizontalDistance(mountPositionAtCommandStart, targetPositionAtCommandStart);
                targetSnapshot = attackTarget.Position;
                var requiresApproach = !childAttack.IsPairEnoughClose;
                approachRequiredAtStart = requiresApproach;
                if (requiresApproach && !allowApproach)
                {
                    throw new InvalidOperationException(
                        "Mounted stock ranged intent forbids automatic mount melee approach.");
                }
                if (!transaction.AcceptTarget(attackTarget.UniqueId, requiresApproach))
                {
                    throw new InvalidOperationException("Mounted combat transaction rejected its exact target.");
                }

                if (requiresApproach)
                {
                    BeginDelegatedMove();
                }
                else
                {
                    StartChildAttack();
                }
            }
            catch (Exception exception)
            {
                logger.Exception("Mounted pair attack start", exception);
                transaction.Fault(exception.GetType().Name + ": " + exception.Message);
                Interrupt();
            }
        }

        protected override void OnTick()
        {
            try
            {
                if (TryInterruptForTargetInvalidationBeforeChildAttack())
                {
                    return;
                }
                RequireLiveExactPair();
                if (TimeSinceStart > MaximumElapsedSeconds)
                {
                    throw new InvalidOperationException("Mounted pair command exceeded its bounded execution time.");
                }

                if (childAttack == null)
                {
                    throw new InvalidOperationException("Mounted pair command lost its child attack.");
                }

                if (transaction.State == MountedCombatTransactionState.Approaching)
                {
                    TickApproach();
                }

                if (transaction.State == MountedCombatTransactionState.Attacking &&
                    transaction.ChildAttackStartCount == 0)
                {
                    StartChildAttack();
                }

                if (transaction.State == MountedCombatTransactionState.Attacking &&
                    childAttack.IsRunning &&
                    IsActed)
                {
                    childAttack.TurnToTarget();
                    childAttack.Tick();
                }

                if (childAttack.IsFinished && transaction.State == MountedCombatTransactionState.Attacking)
                {
                    if (childAttack.Result == ResultType.Success)
                    {
                        transaction.Complete(attackTarget.UniqueId);
                        ForceFinish(ResultType.Success);
                    }
                    else
                    {
                        transaction.Fault(
                            "Native child attack ended with " + childAttack.Result +
                            "; " + (childAttack.TerminalLifecycle ?? "lifecycle unavailable") + ".");
                        ForceFinish(childAttack.Result == ResultType.Interrupt ? ResultType.Interrupt : ResultType.Fail);
                    }
                }
            }
            catch (Exception exception)
            {
                logger.Exception("Mounted pair attack tick", exception);
                transaction.Fault(exception.GetType().Name + ": " + exception.Message);
                if (IsActed)
                {
                    ForceFinish(ResultType.Fail);
                }
                else
                {
                    Interrupt();
                }
            }
        }

        private bool TryInterruptForTargetInvalidationBeforeChildAttack()
        {
            if (transaction.ChildAttackStartCount != 0 || attackTarget == null)
            {
                return false;
            }

            var targetState = attackTarget.Descriptor?.State;
            var targetInvalid = !attackTarget.IsInState || targetState == null ||
                !targetState.IsConscious || targetState.IsFinallyDead ||
                actionActor != null &&
                    (!actionActor.IsEnemy(attackTarget) || !actionActor.CanAttack(attackTarget));
            if (!targetInvalid)
            {
                return false;
            }

            if (!transaction.CancelTargetInvalidationBeforeChildAttack(attackTarget.UniqueId))
            {
                throw new InvalidOperationException(
                    "Mounted pair transaction rejected exact pre-child target invalidation.");
            }

            Interrupt();
            return true;
        }

        protected override ResultType OnAction()
        {
            return ResultType.None;
        }

        protected override void OnEnded(bool raiseEvent = true)
        {
            try
            {
                StopDelegatedMove(false);
                if (childAttack != null && !childAttack.IsFinished)
                {
                    childAttack.Interrupt(false);
                }
                if (!transaction.IsTerminal)
                {
                    transaction.Cancel(Result.ToString());
                }
            }
            finally
            {
                base.OnEnded(raiseEvent);
                ReportTerminalOnce();
            }
        }

        private void TickApproach()
        {
            ObserveApproachInvariants();
            if (childAttack.IsPairEnoughClose)
            {
                if (delegatedMove != null && !delegatedMove.IsFinished &&
                    TurnBased.Controllers.CombatController.IsInTurnBasedCombat())
                {
                    DriveDelegatedMoveOnRiderTurn();
                }
                if (delegatedMove != null && !delegatedMove.IsFinished)
                {
                    return;
                }
                StopDelegatedMove(true);
                if (!transaction.Arrive(attackTarget.UniqueId))
                {
                    throw new InvalidOperationException("Mounted pair transaction could not enter attack range.");
                }
                return;
            }

            var displacement = HorizontalDistance(targetSnapshot, attackTarget.Position);
            if (displacement > TargetRepathDistance)
            {
                Repath();
            }

            if (delegatedMove == null)
            {
                BeginDelegatedMove();
            }

            if (TurnBased.Controllers.CombatController.IsInTurnBasedCombat())
            {
                DriveDelegatedMoveOnRiderTurn();
            }

            if (delegatedMove.IsFinished && !childAttack.IsPairEnoughClose)
            {
                Repath();
            }
        }

        private void DriveDelegatedMoveOnRiderTurn()
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
        }

        private void Repath()
        {
            if (!transaction.TryRepath(attackTarget.UniqueId))
            {
                throw new InvalidOperationException("Mounted pair command exhausted its bounded repath allowance.");
            }
            StopDelegatedMove(false);
            targetSnapshot = attackTarget.Position;
            BeginDelegatedMove();
        }

        private void BeginDelegatedMove()
        {
            if (childAttack == null || childAttack.PairApproachRadius < 0f)
            {
                throw new InvalidOperationException("Mounted pair attack radius is unavailable.");
            }
            if (mount.Commands == null || !mount.Commands.Empty || mount.Commands.Queue.Count != 0)
            {
                throw new InvalidOperationException("Mammoth command container was not empty before exact delegated movement ownership.");
            }
            delegatedMove = new UnitMoveTo(targetSnapshot, childAttack.PairApproachRadius)
            {
                CreatedByPlayer = true,
                ShowTargetMarker = false,
                NeedLoS = MountedCombatSpatialPolicy.DelegatedPointMoveRequiresLineOfSight
            };
            delegatedMoveStartCount++;
            delegatedMoveDrivenByStockController =
                !TurnBased.Controllers.CombatController.IsInTurnBasedCombat();
            mountMoveSlotRestoredAfterApproach = false;
            mount.Commands.Run(delegatedMove);
            delegatedMoveExecutorId = delegatedMove.Executor?.UniqueId;
            delegatedMoveExecutorIsExactMount &= delegatedMove.Executor == mount;
            var rawMoveSlot = mount.Commands.GetCommand(UnitCommand.CommandType.Move);
            delegatedMoveOwnedByMountMoveSlot &=
                rawMoveSlot == delegatedMove && mount.Commands.Contains(delegatedMove);
            delegatedMoveNeverQueuedOnMount &=
                !mount.Commands.Queue.Contains(delegatedMove);
            mountQueueEmptyThroughoutApproach &= mount.Commands.Queue.Count == 0;
            if (!delegatedMoveExecutorIsExactMount || !delegatedMoveOwnedByMountMoveSlot ||
                !delegatedMoveNeverQueuedOnMount || !mountQueueEmptyThroughoutApproach)
            {
                throw new InvalidOperationException(
                    "Exact delegated Mammoth movement did not acquire only the active Move slot.");
            }
        }

        private void StopDelegatedMove(bool requireSuccess)
        {
            if (delegatedMove == null)
            {
                return;
            }

            var exactMove = delegatedMove;
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
            if (requireSuccess && exactMove.Result != ResultType.Success)
            {
                throw new InvalidOperationException(
                    "Exact delegated Mammoth move did not finish successfully before rider attack admission.");
            }
            delegatedMoveFinishedSuccessfully |= exactMove.Result == ResultType.Success;
            if (commands != null && commands.Contains(exactMove))
            {
                commands.RemoveFinishedAndUpdateQueue();
            }
            mountMoveSlotRestoredAfterApproach = commands != null &&
                commands.GetCommand(UnitCommand.CommandType.Move) == null &&
                !commands.Contains(exactMove) && commands.Queue.Count == 0;
            mount.View?.StopMoving();
            delegatedMove = null;
        }

        private void StartChildAttack()
        {
            if (!childAttack.TryPrepareNativeStartAdmission())
            {
                throw new InvalidOperationException("Native child attack failed the bounded Mammoth-origin admission bridge.");
            }
            pairDistanceAtAttackStart = childAttack.PairDistanceAtNativeStart;
            riderDisplacementAtAttackStart = HorizontalDistance(riderPositionAtCommandStart, rider.Position);
            mountDisplacementAtAttackStart = HorizontalDistance(mountPositionAtCommandStart, mount.Position);
            targetDisplacementAtAttackStart = HorizontalDistance(targetPositionAtCommandStart, attackTarget.Position);
            StopDelegatedMove(false);
            mount.ForceLookAt(attackTarget.Position);
            childAttack.TurnToTarget();
            childAttack.OnRun();
            childAttack.Start();
            if (!childAttack.IsRunning || !transaction.TryStartSingleAttack(attackTarget.UniqueId))
            {
                throw new InvalidOperationException("Native child attack did not start exactly once.");
            }
            HasAnimation = false;
        }

        private void CreateAndValidateChildAttack()
        {
            childAttack = new MountedPairSingleAttack(
                attackTarget,
                rider,
                mount,
                action != MountedCombatActionKind.MountPrimaryNatural);
            childAttack.Init(actionActor);
            if (action == MountedCombatActionKind.MountPrimaryNatural)
            {
                horsePrimaryAttackAnimation.SupplyExact(this, childAttack.PlannedAttack, mount);
            }
            if (childAttack.IsFullAttack || childAttack.AllAttacks.Count != 1 || childAttack.PlannedAttack == null)
            {
                throw new InvalidOperationException("Native child did not resolve to exactly one attack.");
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

        private void RequireLiveExactPair()
        {
            var riderState = rider?.Descriptor?.State;
            var mountState = mount?.Descriptor?.State;
            var targetState = attackTarget?.Descriptor?.State;
            var liveness = new MountedPairLivenessSnapshot(
                relationship.State == RelationshipState.Mounted,
                relationship.Rider == rider,
                relationship.Mount == mount,
                Executor == actionActor,
                attackTarget != null && attackTarget.IsInState,
                rider != null && rider.IsInState,
                mount != null && mount.IsInState,
                riderState != null && riderState.IsConscious,
                mountState != null && mountState.IsConscious,
                MountedPairLivenessSnapshot.IsTargetConsciousnessAdmissible(
                    targetState != null && targetState.IsConscious,
                    transaction.ChildAttackStartCount),
                riderState != null && !riderState.IsFinallyDead,
                mountState != null && !mountState.IsFinallyDead,
                targetState != null && !targetState.IsFinallyDead,
                actionActor != null && attackTarget != null && actionActor.IsEnemy(attackTarget),
                actionActor != null && attackTarget != null && actionActor.CanAttack(attackTarget));
            if (!liveness.AllPassed)
            {
                throw new InvalidOperationException(
                    "Exact mounted pair or target became invalid: " + liveness.FailureSummary + ".");
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

        private void ReportTerminalOnce()
        {
            if (terminalReported)
            {
                return;
            }
            terminalReported = true;
            terminal(this, new MountedPairAttackOutcome
            {
                Action = action,
                ActorId = actionActor.UniqueId,
                CommandOwnerId = Executor?.UniqueId,
                ResourceOwnerId = actionActor.UniqueId,
                TargetId = attackTarget.UniqueId,
                Result = Result.ToString(),
                ChildAttackStartCount = transaction.ChildAttackStartCount,
                RepathCount = transaction.RepathCount,
                RiderStandardCharged = IsActed && actionActor == rider,
                ActionStandardCharged = IsActed,
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
                PairRangeSatisfiedAtStart = childAttack != null && childAttack.PairRangeSatisfiedAtNativeStart,
                PairDistanceAtStart = childAttack?.PairDistanceAtNativeStart ?? 0f,
                PairApproachRadiusAtStart = childAttack?.PairApproachRadius ?? 0f,
                NativeExecutorDistanceAtStart = childAttack?.NativeExecutorDistanceAtStart ?? 0f,
                NativeAdmissionRadiusAtStart = childAttack?.NativeAdmissionRadiusAtStart ?? 0f,
                NativeAdmissionAdjusted = childAttack != null && childAttack.NativeAdmissionAdjustedAtStart,
                ApproachRequiredAtStart = approachRequiredAtStart,
                DelegatedMoveStartCount = delegatedMoveStartCount,
                DelegatedMoveTickCount = delegatedMoveTickCount,
                DelegatedMoveExecutorId = delegatedMoveExecutorId,
                DelegatedMoveExecutorIsExactMount = delegatedMoveExecutorIsExactMount,
                WrapperCommandRetainedThroughoutApproach = wrapperCommandRetainedThroughoutApproach,
                DelegatedMoveNeverQueuedOnMount = delegatedMoveNeverQueuedOnMount,
                DelegatedMoveOwnedByMountMoveSlot = delegatedMoveOwnedByMountMoveSlot,
                MountMoveSlotUnreplacedThroughoutApproach = mountMoveSlotUnreplacedThroughoutApproach,
                MountQueueEmptyThroughoutApproach = mountQueueEmptyThroughoutApproach,
                DelegatedMoveFinishedSuccessfully = delegatedMoveFinishedSuccessfully,
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
            });
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
}
