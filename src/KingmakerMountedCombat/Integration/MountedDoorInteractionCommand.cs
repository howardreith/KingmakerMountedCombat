using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View.MapObjects;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedDoorInteractionOutcome
    {
        public string Result { get; set; }
        public string RiderId { get; set; }
        public string MountId { get; set; }
        public int InteractionCount { get; set; }
        public int DelegatedMoveStartCount { get; set; }
        public int DelegatedMoveTickCount { get; set; }
        public bool DoorStateChanged { get; set; }
        public bool RiderPathSuppressed { get; set; }
        public bool MountMoveSlotRestored { get; set; }
        public string TerminalReason { get; set; }
    }

    internal sealed class MountedDoorInteractionCommand : UnitCommand
    {
        private const float MaximumElapsedSeconds = 12f;
        private readonly GameMountedRelationshipService relationship;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly StandardDoor door;
        private readonly IModLogger logger;
        private readonly Action<MountedDoorInteractionCommand, MountedDoorInteractionOutcome> terminal;
        private readonly bool initialDoorState;
        private UnitMoveTo delegatedMove;
        private int interactionCount;
        private int delegatedMoveStartCount;
        private int delegatedMoveTickCount;
        private bool terminalReported;
        private bool mountMoveSlotRestored = true;
        private string terminalReason;

        public MountedDoorInteractionCommand(
            GameMountedRelationshipService relationship,
            UnitEntityData rider,
            UnitEntityData mount,
            StandardDoor door,
            IModLogger logger,
            Action<MountedDoorInteractionCommand, MountedDoorInteractionOutcome> terminal)
            : base(CommandType.Standard, door == null ? Vector3.zero : door.transform.position)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            this.door = door ?? throw new ArgumentNullException(nameof(door));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            initialDoorState = door.GetState();
            ApproachRadius = InfiniteRange;
            MaxApproachRadius = InfiniteRange;
            NeedLoS = false;
            HasAnimation = false;
            CreatedByPlayer = true;
        }

        internal UnitEntityData Rider => rider;
        internal UnitEntityData Mount => mount;
        internal StandardDoor Door => door;
        internal UnitMoveTo DelegatedMove => delegatedMove;

        protected override void OnStart()
        {
            try
            {
                RequireExactPair();
                if (!door.CanInteract())
                {
                    throw new InvalidOperationException("Exact door is no longer interactable.");
                }
                if (IsMountWithinDoorRange())
                {
                    InteractOnce();
                }
                else
                {
                    BeginDelegatedMove();
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        protected override void OnTick()
        {
            try
            {
                RequireExactPair();
                if (TimeSinceStart > MaximumElapsedSeconds)
                {
                    throw new InvalidOperationException("Mounted door approach exceeded its bounded execution time.");
                }
                if (!door.CanInteract())
                {
                    throw new InvalidOperationException("Exact door became non-interactable before admission.");
                }

                if (IsMountWithinDoorRange())
                {
                    StopDelegatedMove();
                    InteractOnce();
                    return;
                }

                if (delegatedMove == null || delegatedMove.IsFinished)
                {
                    throw new InvalidOperationException("Mount door approach ended outside exact interaction range.");
                }
                if (TurnBased.Controllers.CombatController.IsInTurnBasedCombat())
                {
                    DriveDelegatedMoveOnRiderTurn();
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        protected override ResultType OnAction()
        {
            return ResultType.None;
        }

        protected override void OnEnded(bool raiseEvent = true)
        {
            try
            {
                StopDelegatedMove();
            }
            finally
            {
                base.OnEnded(raiseEvent);
                ReportTerminalOnce();
            }
        }

        private void BeginDelegatedMove()
        {
            if (mount.Commands == null || !mount.Commands.Empty || mount.Commands.Queue.Count != 0)
            {
                throw new InvalidOperationException("Mount Move slot is not idle for exact door approach.");
            }
            delegatedMove = new UnitMoveTo(door.transform.position, GetDoorApproachRadius())
            {
                CreatedByPlayer = true,
                ShowTargetMarker = true
            };
            delegatedMoveStartCount++;
            mountMoveSlotRestored = false;
            mount.Commands.Run(delegatedMove);
            if (delegatedMove.Executor != mount ||
                mount.Commands.GetCommand(CommandType.Move) != delegatedMove ||
                mount.Commands.Queue.Contains(delegatedMove))
            {
                throw new InvalidOperationException("Exact Mammoth door approach did not acquire only its Move slot.");
            }
        }

        private void DriveDelegatedMoveOnRiderTurn()
        {
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

        private void StopDelegatedMove()
        {
            var exactMove = delegatedMove;
            if (exactMove == null)
            {
                return;
            }
            if (!exactMove.IsFinished)
            {
                exactMove.Interrupt(false);
            }
            if (mount.Commands != null && mount.Commands.Contains(exactMove) && mount.Commands.Queue.Count == 0)
            {
                mount.Commands.RemoveFinishedAndUpdateQueue();
            }
            mount.View?.StopMoving();
            mountMoveSlotRestored = mount.Commands != null &&
                mount.Commands.GetCommand(CommandType.Move) == null &&
                !mount.Commands.Contains(exactMove) && mount.Commands.Queue.Count == 0;
            delegatedMove = null;
        }

        private void InteractOnce()
        {
            if (interactionCount != 0)
            {
                throw new InvalidOperationException("Exact door interaction attempted more than once.");
            }
            interactionCount++;
            door.Interact(rider);
            ForceFinish(ResultType.Success);
        }

        private bool IsMountWithinDoorRange()
        {
            var delta = mount.Position - door.transform.position;
            delta.y = 0f;
            return delta.magnitude <= GetDoorApproachRadius() + 0.01f;
        }

        private float GetDoorApproachRadius()
        {
            return door.ProximityRadius > 0f ? door.ProximityRadius : 4f;
        }

        private void RequireExactPair()
        {
            if (relationship.State != RelationshipState.Mounted ||
                relationship.Rider != rider || relationship.Mount != mount ||
                rider.Descriptor?.Pet != mount || mount.Descriptor?.Master.Value != rider ||
                !relationship.Runtime.PoseHealthy)
            {
                throw new InvalidOperationException("Mounted door interaction lost the exact active pair.");
            }
        }

        private void Fail(Exception exception)
        {
            terminalReason = exception.GetType().Name + ": " + exception.Message;
            logger.Exception("Mounted door interaction", exception);
            if (IsActed)
            {
                ForceFinish(ResultType.Fail);
            }
            else
            {
                Interrupt();
            }
        }

        private void ReportTerminalOnce()
        {
            if (terminalReported)
            {
                return;
            }
            terminalReported = true;
            terminal(this, new MountedDoorInteractionOutcome
            {
                Result = Result.ToString(),
                RiderId = rider.UniqueId,
                MountId = mount.UniqueId,
                InteractionCount = interactionCount,
                DelegatedMoveStartCount = delegatedMoveStartCount,
                DelegatedMoveTickCount = delegatedMoveTickCount,
                DoorStateChanged = door != null && door.GetState() != initialDoorState,
                RiderPathSuppressed = rider.View?.AgentASP != null && !rider.View.AgentASP.enabled,
                MountMoveSlotRestored = mountMoveSlotRestored,
                TerminalReason = terminalReason
            });
        }
    }
}
