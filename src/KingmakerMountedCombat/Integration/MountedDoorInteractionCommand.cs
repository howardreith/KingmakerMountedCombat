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
        private int approachesWithoutProgress;
        private float bestApproachDistance = float.PositiveInfinity;
        private float stalledGameSeconds;
        private Vector3 lastSampledMountPosition;
        private bool mountPositionSampled;
        private TimeSpan lastStallSampleGameTime = TimeSpan.MinValue;
        private TimeSpan nextApproachGameTime = TimeSpan.MinValue;
        private TimeSpan approachBeganGameTime = TimeSpan.MinValue;
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
        internal bool NativePartnerMovement { get; set; }

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
                if (IsMountWithinDoorRange())
                {
                    StopDelegatedMove();
                    CompleteApproachClock();
                    approachSatisfied = true;
                    return;
                }
                if (delegatedMove == null)
                {
                    if (GameTime >= nextApproachGameTime)
                    {
                        BeginDelegatedMove();
                    }
                    return;
                }
                if (delegatedMove.IsFinished)
                {
                    // The native re-approach: the move ended outside range, so try again after the native
                    // cooldown, unless it is no longer getting nearer.
                    RecordApproachEnd("ended outside range (" + delegatedMove.Result + ")");
                    return;
                }
                if (TurnBased.Controllers.CombatController.IsInTurnBasedCombat())
                {
                    DriveDelegatedMoveOnRiderTurn();
                }
                ObserveStall();
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
                var approachPoint = NativeApproachPoint;
                distanceAtInteraction = HorizontalDistance(mount.Position, approachPoint);
                lineOfSightAtInteraction = !LineOfSightGeometry.Instance.HasObstacle(mount.EyePosition, approachPoint, GetTargetLOSObjectId());
                interactionCount++;
                door.Interact(rider);
                RevalidateOccupancy();
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
            if (approachBeganGameTime == TimeSpan.MinValue)
            {
                approachBeganGameTime = GameTime;
            }
            stalledGameSeconds = 0f;
            mountPositionSampled = false;
            mount.Commands.Run(delegatedMove);
            if (delegatedMove.Executor != mount ||
                mount.Commands.GetCommand(CommandType.Move) != delegatedMove ||
                mount.Commands.Queue.Contains(delegatedMove))
            {
                throw new InvalidOperationException("Exact mount door approach did not acquire only its Move slot.");
            }
        }

        private void RecordApproachEnd(string why)
        {
            var distance = HorizontalDistance(mount.Position, NativeApproachPoint);
            if (MountedDoorApproachPolicy.ApproachProgressed(bestApproachDistance, distance, MountedDoorApproachPolicy.ProgressEpsilonMetres))
            {
                bestApproachDistance = distance;
                approachesWithoutProgress = 0;
            }
            else
            {
                approachesWithoutProgress++;
            }
            ClearFinishedDelegatedMove();
            if (MountedDoorApproachPolicy.ApproachExhausted(approachesWithoutProgress, MountedDoorApproachPolicy.MaximumApproachesWithoutProgress))
            {
                throw new InvalidOperationException("Mount door approach cannot reach the exact interaction point: " +
                    approachesWithoutProgress + " approaches " + why + " without progress; nearest " +
                    bestApproachDistance.ToString("F2") + " m against radius " + approachRadius.ToString("F2") + " m.");
            }
            nextApproachGameTime = GameTime + TimeSpan.FromSeconds(
                MountedDoorApproachPolicy.ReapproachDelaySeconds(UnityEngine.Random.value));
        }

        private void ObserveStall()
        {
            var position = mount.Position;
            var now = GameTime;
            if (!mountPositionSampled)
            {
                mountPositionSampled = true;
                lastSampledMountPosition = position;
                lastStallSampleGameTime = now;
                return;
            }
            var elapsed = (float)(now - lastStallSampleGameTime).TotalSeconds;
            lastStallSampleGameTime = now;
            if (HorizontalDistance(position, lastSampledMountPosition) >= MountedDoorApproachPolicy.DisplacementEpsilonMetres)
            {
                lastSampledMountPosition = position;
                stalledGameSeconds = 0f;
                return;
            }
            stalledGameSeconds += Math.Max(0f, elapsed);
            if (MountedDoorApproachPolicy.MovementStalled(stalledGameSeconds, MountedDoorApproachPolicy.StalledSecondsLimit,
                delegatedMove != null && delegatedMove.IsStarted && !delegatedMove.IsFinished && !NativePartnerMovement))
            {
                delegatedMove.Interrupt(false);
                RecordApproachEnd("stalled for " + stalledGameSeconds.ToString("F1") + " game seconds");
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

        private void CompleteApproachClock()
        {
            if (approachBeganGameTime != TimeSpan.MinValue)
            {
                approachElapsedGameSeconds = (float)(GameTime - approachBeganGameTime).TotalSeconds;
            }
        }

        private void RevalidateOccupancy()
        {
            var position = mount.Position;
            var inside = ObstacleAnalyzer.IsPointInsideNavMesh(position);
            occupancyInsideNavmesh = inside;
            var nearest = ObstacleAnalyzer.GetNearestNode(position);
            var walkable = nearest.node != null && nearest.node.Walkable;
            occupancyNearestNodeDistance = walkable ? HorizontalDistance(position, nearest.clampedPosition) : float.PositiveInfinity;
            var disposition = MountedDoorApproachPolicy.DecideOccupancy(inside, walkable, occupancyNearestNodeDistance,
                MountedDoorApproachPolicy.RecoveryRadiusMetres);
            switch (disposition)
            {
                case MountedDoorOccupancyDisposition.Valid:
                    occupancyRecovery = "none";
                    break;
                case MountedDoorOccupancyDisposition.RepathToNearest:
                    // One native move to the nearest walkable node: the mount's own agent walks it, nothing
                    // is transported. The mount's Move slot is idle here because the approach has ended.
                    if (mount.Commands != null && mount.Commands.Empty && mount.Commands.Queue.Count == 0)
                    {
                        mount.Commands.Run(new UnitMoveTo(nearest.clampedPosition, 0.1f) { CreatedByPlayer = false, ShowTargetMarker = false });
                        occupancyRecovery = "repath";
                        mountMoveSlotRestored = false;
                    }
                    else
                    {
                        occupancyRecovery = "repath-refused-mount-busy";
                    }
                    logger.Info("Mounted door occupancy recovery: mountId=" + mount.UniqueId + "; nearestNodeDistance=" +
                        occupancyNearestNodeDistance.ToString("F3") + "; recovery=" + occupancyRecovery);
                    break;
                default:
                    occupancyRecovery = "unrecoverable";
                    logger.Warning("Mounted door occupancy left the navmesh with no walkable node within " +
                        MountedDoorApproachPolicy.RecoveryRadiusMetres + " m: mountId=" + mount.UniqueId + ".");
                    break;
            }
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
            if (approachElapsedGameSeconds <= 0f)
            {
                CompleteApproachClock();
            }
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
                TerminalReason = terminalReason,
                ApproachesWithoutProgress = approachesWithoutProgress,
                ApproachElapsedGameSeconds = approachElapsedGameSeconds,
                DistanceAtInteraction = distanceAtInteraction,
                LineOfSightAtInteraction = lineOfSightAtInteraction,
                OccupancyInsideNavmesh = occupancyInsideNavmesh,
                OccupancyRecovery = occupancyRecovery,
                OccupancyNearestNodeDistance = occupancyNearestNodeDistance
            });
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
