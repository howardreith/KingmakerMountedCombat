using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedPairAttackOutcome
    {
        public MountedCombatActionKind Action { get; set; }

        public string ActorId { get; set; }

        public string TargetId { get; set; }

        public string Result { get; set; }

        public int ChildAttackStartCount { get; set; }

        public int RepathCount { get; set; }

        public bool RiderStandardCharged { get; set; }

        public bool NativeAttackRuleObserved { get; set; }
    }

    internal sealed class MountedPairAttackCommand : UnitCommand
    {
        private const float TargetRepathDistance = 0.75f;
        private const float MaximumElapsedSeconds = 8.5f;

        private readonly GameMountedRelationshipService relationship;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly UnitEntityData attackTarget;
        private readonly MountedCombatActionKind action;
        private readonly NativeSingleAttackWeaponSelection expectedMountPrimary;
        private readonly IModLogger logger;
        private readonly Action<MountedPairAttackCommand, MountedPairAttackOutcome> terminal;
        private readonly MountedCombatTransaction transaction = new MountedCombatTransaction();
        private MountedPairSingleAttack childAttack;
        private UnitMoveTo delegatedMove;
        private Vector3 targetSnapshot;
        private bool terminalReported;

        public MountedPairAttackCommand(
            GameMountedRelationshipService relationship,
            UnitEntityData rider,
            UnitEntityData mount,
            UnitEntityData target,
            MountedCombatActionKind action,
            NativeSingleAttackWeaponSelection expectedMountPrimary,
            IModLogger logger,
            Action<MountedPairAttackCommand, MountedPairAttackOutcome> terminal)
            : base(CommandType.Standard, target)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            attackTarget = target ?? throw new ArgumentNullException(nameof(target));
            this.action = action;
            this.expectedMountPrimary = expectedMountPrimary;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
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

        internal UnitEntityData Rider => rider;

        internal UnitEntityData Mount => mount;

        protected override void OnStart()
        {
            try
            {
                RequireLiveExactPair();
                CreateAndValidateChildAttack();
                targetSnapshot = attackTarget.Position;
                var requiresApproach = !childAttack.IsUnitEnoughClose;
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
                    childAttack.IsRunning)
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
                        transaction.Fault("Native child attack ended with " + childAttack.Result + ".");
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

        protected override ResultType OnAction()
        {
            return ResultType.Fail;
        }

        protected override void OnEnded(bool raiseEvent = true)
        {
            try
            {
                if (delegatedMove != null && !delegatedMove.IsFinished)
                {
                    delegatedMove.Interrupt(false);
                }
                mount.Commands.InterruptMove();
                mount.View?.StopMoving();
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
            if (childAttack.IsUnitEnoughClose)
            {
                StopDelegatedMove();
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
                delegatedMove.Tick();
            }

            if (delegatedMove.IsFinished && !childAttack.IsUnitEnoughClose)
            {
                Repath();
            }
        }

        private void Repath()
        {
            if (!transaction.TryRepath(attackTarget.UniqueId))
            {
                throw new InvalidOperationException("Mounted pair command exhausted its bounded repath allowance.");
            }
            StopDelegatedMove();
            targetSnapshot = attackTarget.Position;
            BeginDelegatedMove();
        }

        private void BeginDelegatedMove()
        {
            if (childAttack == null || childAttack.PairApproachRadius < 0f)
            {
                throw new InvalidOperationException("Mounted pair attack radius is unavailable.");
            }
            delegatedMove = new UnitMoveTo(targetSnapshot, childAttack.PairApproachRadius)
            {
                CreatedByPlayer = true,
                ShowTargetMarker = false
            };
            delegatedMove.Init(mount);
            delegatedMove.OnRun();
        }

        private void StopDelegatedMove()
        {
            if (delegatedMove != null && !delegatedMove.IsFinished)
            {
                delegatedMove.Interrupt(false);
            }
            mount.View?.StopMoving();
            delegatedMove = null;
        }

        private void StartChildAttack()
        {
            if (!childAttack.IsUnitEnoughClose)
            {
                throw new InvalidOperationException("Native child attack remained outside the exact pair range.");
            }
            StopDelegatedMove();
            mount.ForceLookAt(attackTarget.Position);
            childAttack.TurnToTarget();
            childAttack.OnRun();
            childAttack.Start();
            if (!childAttack.IsRunning || !transaction.TryStartSingleAttack(attackTarget.UniqueId))
            {
                throw new InvalidOperationException("Native child attack did not start exactly once.");
            }
            SetIsActed(true);
        }

        private void CreateAndValidateChildAttack()
        {
            var actor = action == MountedCombatActionKind.RiderMelee ? rider : mount;
            childAttack = new MountedPairSingleAttack(
                attackTarget,
                rider,
                mount,
                action == MountedCombatActionKind.RiderMelee);
            childAttack.Init(actor);
            if (childAttack.IsFullAttack || childAttack.AllAttacks.Count != 1 || childAttack.PlannedAttack == null)
            {
                throw new InvalidOperationException("Native child did not resolve to exactly one attack.");
            }
            if (childAttack.PlannedAttack.Weapon == null || childAttack.PlannedAttack.Weapon.Blueprint.IsRanged)
            {
                throw new InvalidOperationException("Mounted combat rejected a missing or ranged planned weapon.");
            }
            if (action == MountedCombatActionKind.MountPrimaryNatural)
            {
                if (expectedMountPrimary?.Kind != NativeSingleAttackSlotKind.PrimaryHand ||
                    expectedMountPrimary.Slot == null ||
                    childAttack.PlannedAttack.Hand != expectedMountPrimary.Slot ||
                    childAttack.PlannedAttack.Weapon != expectedMountPrimary.Weapon ||
                    expectedMountPrimary.Weapon?.Blueprint == null ||
                    !expectedMountPrimary.Weapon.Blueprint.IsNatural ||
                    expectedMountPrimary.Weapon.Blueprint.IsRanged)
                {
                    throw new InvalidOperationException("Native Mammoth attack was not the exact primary-hand natural attack selected by stock single-attack order.");
                }
            }
        }

        private void RequireLiveExactPair()
        {
            if (relationship.State != RelationshipState.Mounted ||
                relationship.Rider != rider ||
                relationship.Mount != mount ||
                Executor != rider ||
                attackTarget == null ||
                !attackTarget.IsInState ||
                !rider.IsInState ||
                !mount.IsInState ||
                !rider.Descriptor.State.IsConscious ||
                !mount.Descriptor.State.IsConscious ||
                !attackTarget.Descriptor.State.IsConscious ||
                rider.Descriptor.State.IsFinallyDead ||
                mount.Descriptor.State.IsFinallyDead ||
                attackTarget.Descriptor.State.IsFinallyDead ||
                !rider.IsEnemy(attackTarget) ||
                !rider.CanAttack(attackTarget))
            {
                throw new InvalidOperationException("Exact mounted pair or target became invalid.");
            }
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
                ActorId = action == MountedCombatActionKind.RiderMelee ? rider.UniqueId : mount.UniqueId,
                TargetId = attackTarget.UniqueId,
                Result = Result.ToString(),
                ChildAttackStartCount = transaction.ChildAttackStartCount,
                RepathCount = transaction.RepathCount,
                RiderStandardCharged = IsActed,
                NativeAttackRuleObserved = childAttack?.LastAttackRule != null
            });
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            var dx = first.x - second.x;
            var dz = first.z - second.z;
            return Mathf.Sqrt((dx * dx) + (dz * dz));
        }
    }
}
