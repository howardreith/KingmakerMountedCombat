using System;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.FogOfWar;
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
        // Sprint 2 additions: the approach's own progress record and the post-interaction occupancy check.
        public int ApproachesWithoutProgress { get; set; }
        public float ApproachElapsedGameSeconds { get; set; }
        public float DistanceAtInteraction { get; set; }
        public bool LineOfSightAtInteraction { get; set; }
        public bool? OccupancyInsideNavmesh { get; set; }
        public string OccupancyRecovery { get; set; }
        public float OccupancyNearestNodeDistance { get; set; }
        // Sprint 2 completion (SPACE-01): the owned post-interaction settlement. These fields are filled by
        // MountedDoorOccupancySettlement after the native action ended, on the same outcome instance.
        public bool PreventionRefused { get; set; }
        public bool OccupancySettled { get; set; }
        public float OccupancySettleGameSeconds { get; set; }
        public float MountCorpulence { get; set; }
        public bool? FootprintClear { get; set; }
        public int FootprintRingInside { get; set; }
        public int FootprintRingReachable { get; set; }
        public string RecoveryDisposition { get; set; } = "not-evaluated";
        public string RecoveryTerminal { get; set; } = "None";
        public int RecoveryCandidatesEvaluated { get; set; }
        public string RecoveryCandidateReasons { get; set; } = "";
        public float RecoveryDestinationDistance { get; set; } = float.NaN;
        public bool? FootprintClearAfterRecovery { get; set; }
        public string RecoveryCancelReason { get; set; }
        internal MountedDoorOccupancySettlement Settlement { get; set; }
    }

    // The mount's delegated approach carries the door's own transform as the native LOS object, exactly
    // as UnitInteractWithObject.GetTargetLOSObjectId does, so a door whose proximity radius ends its path
    // through the native LosPathCondition is not treated as an obstacle to itself.
    internal sealed class MountedDoorApproachMove : UnitMoveTo
    {
        private readonly int losObjectId;

        public MountedDoorApproachMove(Vector3 target, float approachRadius, int losObjectId)
            : base(target, approachRadius)
        {
            this.losObjectId = losObjectId;
        }

        public override int GetTargetLOSObjectId()
        {
            return losObjectId;
        }
    }

    // One accepted mounted door click. The command stays unstarted while the mount approaches (the native
    // action controller approaches unstarted commands and never ticks their nine-second clock), starts only
    // when the pair is within the native interaction range, and interacts through OnAction so turn-based
    // combat charges the rider exactly as a native interaction would.
    internal sealed class MountedDoorInteractionCommand : UnitCommand
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPairOwner<UnitEntityData, GameMountedRelationshipService> pairOwner;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly StandardDoor door;
        private readonly IModLogger logger;
        private readonly Action<MountedDoorInteractionCommand, MountedDoorInteractionOutcome> terminal;
        private readonly bool initialDoorState;
        private readonly float approachRadius;
        private MountedDoorApproachMove delegatedMove;
        private int interactionCount;
        private int delegatedMoveStartCount;
        private int delegatedMoveTickCount;
        private readonly MountedDoorApproachLifecycle lifecycle = new MountedDoorApproachLifecycle();
        private Vector3 lastSampledMountPosition;
        private bool mountPositionSampled;
        private float approachElapsedGameSeconds;
        private bool approachSatisfied;
        private bool terminalReported;
        private bool mountMoveSlotRestored = true;
        private string terminalReason;
        private float distanceAtInteraction = float.NaN;
        private bool lineOfSightAtInteraction;
        private bool? occupancyInsideNavmesh;
        private string occupancyRecovery = "not-evaluated";
        private float occupancyNearestNodeDistance = float.NaN;
        private bool preventionRefused;
        private readonly MountedDoorInteractionOutcome outcome = new MountedDoorInteractionOutcome();
        internal bool NativePartnerMovement { get; set; }
        // The controller's observation of the mount's legitimate movement opportunity this frame (turn-based
        // combat only): the native movement override admitted the mount with movement budget left.
        internal Func<bool> MovementOpportunity { get; set; }
        internal MountedDoorOccupancySettlement Settlement { get; private set; }

        public MountedDoorInteractionCommand(
            GameMountedRelationshipService relationship,
            UnitEntityData rider,
            UnitEntityData mount,
            StandardDoor door,
            IModLogger logger,
            Action<MountedDoorInteractionCommand, MountedDoorInteractionOutcome> terminal)
            : base(CommandType.Standard, door == null ? Vector3.zero : NativeTargetNode(door))
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            pairOwner = relationship.PairOwner;
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            this.door = door ?? throw new ArgumentNullException(nameof(door));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            initialDoorState = door.GetState();
            approachRadius = door.ProximityRadius > 0f ? door.ProximityRadius : 4f;
            // The rider never paths on its own: the native range gate is satisfied unconditionally and the
            // actual approach is the mount's, admitted through CanStart.
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
        internal float ApproachRadiusMetres => approachRadius;

        // Native UnitInteractWithObject approach point: the map object's view transform.
        internal Vector3 NativeApproachPoint => door.MapObject != null ? door.MapObject.transform.position : door.transform.position;

        public override bool CanStart => approachSatisfied;

        // The carried rider's movement agent mirrors the mount; the native start gate waits for it to stop.
        public override bool AwaitMovementFinish => true;

        public override int GetTargetLOSObjectId()
        {
            return door.transform.GetInstanceID();
        }

        protected override Vector3 GetTargetPoint()
        {
            return NativeApproachPoint;
        }

        public override void TurnToTarget()
        {
            // The rider's heading is the mount's; it never turns on its own.
        }

        // Called from the TickApproaching prefix in place of the native rider approach, on every action-controller
        // tick while the command is unstarted (turn-based: only on the rider's acting turn).
        internal void TickMountedApproach()
        {
            if (IsFinished)
            {
                return;
            }
            try
            {
                RequireExactPair();
                if (!door.CanInteract())
                {
                    throw new InvalidOperationException("Exact door became non-interactable before admission.");
                }
                if (approachSatisfied)
                {
                    return;
                }
                // The approach state machine (MountedDoorApproachLifecycle) decides from this tick's observation; the
                // command only executes the action against the mount's Move slot. CanStart stays false until Satisfied.
                var position = mount.Position;
                var displacement = mountPositionSampled ? HorizontalDistance(position, lastSampledMountPosition) : 0f;
                // The anchor moves only on a real displacement, so slow creep accumulates and is never read as stillness.
                if (!mountPositionSampled || displacement >= MountedDoorApproachPolicy.DisplacementEpsilonMetres)
                {
                    lastSampledMountPosition = position;
                }
                mountPositionSampled = true;
                var live = delegatedMove != null && !delegatedMove.IsFinished;
                var owned = live && mount.Commands != null && ReferenceEquals(mount.Commands.GetCommand(CommandType.Move), delegatedMove);
                var turnBased = TurnBased.Controllers.CombatController.IsInTurnBasedCombat();
                var action = lifecycle.Tick(new MountedDoorApproachObservation
                {
                    NowSeconds = GameTime.TotalSeconds,
                    InRange = IsMountWithinDoorRange(),
                    Move = delegatedMove == null ? MountedDoorApproachMoveState.None : delegatedMove.IsFinished ? MountedDoorApproachMoveState.Finished : MountedDoorApproachMoveState.Live,
                    MoveResult = delegatedMove != null && delegatedMove.IsFinished ? delegatedMove.Result.ToString() : null,
                    DistanceToPoint = HorizontalDistance(position, NativeApproachPoint),
                    DisplacementSinceLastTick = displacement,
                    StallClockRuns = MountedDoorApproachPolicy.StallClockRuns(owned, Game.Instance.IsPaused, turnBased, MovementOpportunity == null || MovementOpportunity()),
                    ReapproachSample = UnityEngine.Random.value
                });
                switch (action)
                {
                    case MountedDoorApproachAction.Satisfied:
                        StopDelegatedMove();
                        approachElapsedGameSeconds = lifecycle.ElapsedSeconds;
                        // Prevention before the native action starts (no cost is spent on an unstarted command): closing a
                        // door whose cut is written on state changes while the pair's footprint overlaps that cut would
                        // embed the pair when the cut lands; the click is refused with the reason instead.
                        if (ClosingWouldEmbedPair())
                        {
                            preventionRefused = true;
                            throw new InvalidOperationException("Refused before interaction: closing " + door.name +
                                " would embed the pair standing in its doorway; move the pair out of the doorway first.");
                        }
                        approachSatisfied = true;
                        return;
                    case MountedDoorApproachAction.BeginMove:
                        BeginDelegatedMove();
                        return;
                    case MountedDoorApproachAction.Retry:
                        // The native re-approach: the move ended outside range; the lifecycle scheduled the next one.
                        ClearFinishedDelegatedMove();
                        return;
                    case MountedDoorApproachAction.Stalled:
                        delegatedMove.Interrupt(false);
                        ClearFinishedDelegatedMove();
                        logger.Info("Mounted door approach " + lifecycle.LastApproachEnd + "; retrying after the native cooldown (mountId=" + mount.UniqueId + ").");
                        return;
                    case MountedDoorApproachAction.Exhausted:
                        if (delegatedMove != null) ClearFinishedDelegatedMove();
                        throw new InvalidOperationException(lifecycle.ExhaustedReason + " Radius " + approachRadius.ToString("F2") + " m.");
                    default:
                        if (live && turnBased)
                        {
                            DriveDelegatedMoveOnRiderTurn();
                        }
                        return;
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        protected override void OnStart()
        {
            try
            {
                RequireExactPair();
                if (!approachSatisfied || !IsMountWithinDoorRange())
                {
                    throw new InvalidOperationException("Mounted door interaction started outside the native interaction range.");
                }
                base.OnStart();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        protected override ResultType OnAction()
        {
            try
            {
                RequireExactPair();
                if (door.NotInCombat && rider.IsInCombat)
                {
                    terminalReason = "Door is not usable while the rider is in combat.";
                    return ResultType.Fail;
                }
                if (!door.CanInteract())
                {
                    terminalReason = "Exact door became non-interactable before interaction.";
                    return ResultType.Fail;
                }
                if (interactionCount != 0)
                {
                    throw new InvalidOperationException("Exact door interaction attempted more than once.");
                }
                if (ClosingWouldEmbedPair())
                {
                    preventionRefused = true;
                    terminalReason = "Refused at interaction: closing " + door.name + " would embed the pair standing in its doorway.";
                    return ResultType.Fail;
                }
                var approachPoint = NativeApproachPoint;
                distanceAtInteraction = HorizontalDistance(mount.Position, approachPoint);
                lineOfSightAtInteraction = !LineOfSightGeometry.Instance.HasObstacle(mount.EyePosition, approachPoint, GetTargetLOSObjectId());
                interactionCount++;
                var openBefore = door.GetState();
                door.Interact(rider);
                // The native action is complete here; nothing after the interaction may turn it into a failure.
                try
                {
                    ObserveImmediateOccupancy();
                    // The door's collision change lands on a later frame; the owned settlement waits for it, samples the
                    // pair's footprint and recovers through one owned native move.
                    Settlement = new MountedDoorOccupancySettlement(relationship, rider, mount, door,
                        door.GetState() != openBefore ? door.GetState() : !openBefore, logger, outcome, NativePartnerMovement, MovementOpportunity);
                }
                catch (Exception settlementException)
                {
                    logger.Exception("Mounted door occupancy settlement setup", settlementException);
                    occupancyRecovery = "settlement-unavailable: " + settlementException.GetType().Name;
                }
                return ResultType.Success;
            }
            catch (Exception exception)
            {
                terminalReason = exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Mounted door interaction", exception);
                return ResultType.Fail;
            }
        }

        protected override void OnEnded(bool raiseEvent = true)
        {
            try
            {
                StopDelegatedMove();
            }
            catch (Exception exception)
            {
                // The native Interrupt/Run caller must not see a cleanup fault; it is recorded on the outcome.
                terminalReason = terminalReason ?? exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Mounted door interaction cleanup", exception);
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
            delegatedMove = new MountedDoorApproachMove(NativeApproachPoint, approachRadius, GetTargetLOSObjectId())
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
                throw new InvalidOperationException("Exact mount door approach did not acquire only its Move slot.");
            }
        }

        private void DriveDelegatedMoveOnRiderTurn()
        {
            if (NativePartnerMovement) return;
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

        private void ClearFinishedDelegatedMove()
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

        private void StopDelegatedMove()
        {
            if (delegatedMove != null)
            {
                ClearFinishedDelegatedMove();
            }
        }

        // The synchronous observation right after the interaction: the center's navmesh membership before the
        // door's cut has landed. It proves nothing about the settled state; the owned settlement decides that.
        private void ObserveImmediateOccupancy()
        {
            var position = mount.Position;
            occupancyInsideNavmesh = ObstacleAnalyzer.IsPointInsideNavMesh(position);
            var nearest = ObstacleAnalyzer.GetNearestNode(position);
            var walkable = nearest.node != null && nearest.node.Walkable;
            occupancyNearestNodeDistance = walkable ? HorizontalDistance(position, nearest.clampedPosition) : float.PositiveInfinity;
            occupancyRecovery = "pending-settlement";
        }

        private bool ClosingWouldEmbedPair()
        {
            var agent = mount.View?.AgentASP;
            var corpulence = agent != null && agent.Corpulence > 0f ? agent.Corpulence : 0.5f;
            var cut = door.GetComponentInChildren<Pathfinding.NavmeshCut>();
            return MountedDoorOccupancyPolicy.ClosingWouldEmbed(door.DisableNavmeshCutWhenOpen, door.GetState(),
                MountedFootprintProbe.IntersectsCut(cut, mount.Position, corpulence));
        }

        private bool IsMountWithinDoorRange()
        {
            // Native UnitCommand.IsUnitEnoughClose with the pair's footprint as the executor: mechanics distance from the
            // mount to the map object's approach point within the proximity radius, and line of sight that ignores the door.
            return IsUnitCloseEnough(NativeApproachPoint, mount.Position, mount.EyePosition, approachRadius, true, GetTargetLOSObjectId());
        }

        private void RequireExactPair()
        {
            if (!relationship.IsCurrentPairOwner(pairOwner) || relationship.State != RelationshipState.Mounted ||
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
            if (approachElapsedGameSeconds <= 0f && lifecycle.BeganAt.HasValue)
            {
                approachElapsedGameSeconds = (float)(GameTime.TotalSeconds - lifecycle.BeganAt.Value);
            }
            outcome.Result = Result.ToString();
            outcome.RiderId = rider.UniqueId;
            outcome.MountId = mount.UniqueId;
            outcome.InteractionCount = interactionCount;
            outcome.DelegatedMoveStartCount = delegatedMoveStartCount;
            outcome.DelegatedMoveTickCount = delegatedMoveTickCount;
            outcome.DoorStateChanged = door != null && door.GetState() != initialDoorState;
            outcome.RiderPathSuppressed = rider.View?.AgentASP != null && !rider.View.AgentASP.enabled;
            outcome.MountMoveSlotRestored = mountMoveSlotRestored;
            outcome.TerminalReason = terminalReason;
            outcome.ApproachesWithoutProgress = lifecycle.ApproachesWithoutProgress;
            outcome.ApproachElapsedGameSeconds = approachElapsedGameSeconds;
            outcome.DistanceAtInteraction = distanceAtInteraction;
            outcome.LineOfSightAtInteraction = lineOfSightAtInteraction;
            outcome.OccupancyInsideNavmesh = occupancyInsideNavmesh;
            outcome.PreventionRefused = preventionRefused;
            if (Settlement == null)
            {
                // No interaction happened: nothing to settle. The legacy word keeps the v3 reader's meaning.
                outcome.OccupancyRecovery = occupancyRecovery;
                outcome.RecoveryDisposition = "not-evaluated";
            }
            outcome.OccupancyNearestNodeDistance = occupancyNearestNodeDistance;
            outcome.Settlement = Settlement;
            // The settlement mutates this same instance after the native action ended (the controller pumps it);
            // readers that wait for the controller's pending settlement to finish see the settled fields.
            terminal(this, outcome);
        }

        private static TimeSpan GameTime => Game.Instance.TimeController.GameTime;

        private static Vector3 NativeTargetNode(StandardDoor door)
        {
            // Native UnitInteractWithObject constructor target: the nearest navmesh node to the interaction transform.
            return ObstacleAnalyzer.GetNearestNode(door.transform.position).clampedPosition;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
