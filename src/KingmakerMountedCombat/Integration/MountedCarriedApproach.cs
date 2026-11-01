using System;
using System.Reflection;
using System.Reflection.Emit;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.FogOfWar;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedCarriedApproachOutcome
    {
        public string Kind { get; set; }
        public string CommandType { get; set; }
        public string RiderId { get; set; }
        public string MountId { get; set; }
        public string TargetId { get; set; }
        public float ApproachRadius { get; set; } = float.NaN;
        public int DelegatedMoveStartCount { get; set; }
        public int DelegatedMoveTickCount { get; set; }
        public int ReplanCount { get; set; }
        public int ApproachesWithoutProgress { get; set; }
        public bool Satisfied { get; set; }
        public bool NativeApproachEndSignalled { get; set; }
        public bool TrapSafePointApplied { get; set; }
        // Satisfaction geometry: the rider's mechanics distance to the command's approach point and the native
        // line-of-sight test from the rider's eye position (what UnitCommand.IsUnitEnoughClose evaluates).
        public float DistanceAtSatisfaction { get; set; } = float.NaN;
        public bool LineOfSightAtSatisfaction { get; set; }
        public float ApproachElapsedOperationalSeconds { get; set; }
        // The carrier's own delegated move is finished and released (review correction B: our work, not "no commands").
        public bool MountMoveSlotRestored { get; set; }
        // Another command owned the mount's movement at a release and was left untouched (path, target and queue).
        public bool SuccessorRetained { get; set; }
        // The approach yielded because another command owned the mount's movement when a move was due.
        public bool YieldedToMountOwner { get; set; }
        public string TerminalReason { get; set; }
        public string Result { get; set; }
    }

    // Sprint 3 (TRAP-01, SPELL-01). The mount's delegated approach for the rider's OWN unstarted native
    // command: a UnitUseAbility (spell, item, held touch delivery) or a UnitInteractWithObject on a
    // discovered trap. The native command is never replaced: it starts, acts, spends, rolls and ends through
    // its own code. What this driver replaces is the native per-tick rider approach, which paths the carried
    // rider's disabled stock agent and therefore never moves the pair. On every approach tick it first runs
    // the engine's own OnTickApproaching (the turn-based sticky-touch range rule, the trap safe-point rule),
    // then drives the mount's Move slot through the accepted Sprint 2 approach lifecycle. In range it stops
    // the mount and hands the tick back to the native code, which starts the command exactly as for an
    // unmounted unit.
    internal sealed class MountedCarriedApproach
    {
        private static readonly Action<UnitCommand> NativeOnTickApproaching = CreateNativeOnTickApproachingBridge();
        private const float MountSaddleMarginMetres = 0.5f;
        private const float MinimumDelegatedRadiusMetres = 0.3f;

        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPairOwner<UnitEntityData, GameMountedRelationshipService> pairOwner;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly IModLogger logger;
        private readonly Action<MountedCarriedApproach, MountedCarriedApproachOutcome> terminal;
        private readonly MountedDoorApproachLifecycle lifecycle = new MountedDoorApproachLifecycle();
        private readonly MountedDoorOperationalClock operationalClock = new MountedDoorOperationalClock();
        private readonly MountedCarriedApproachOutcome outcome = new MountedCarriedApproachOutcome();
        private MountedDoorApproachMove delegatedMove;
        private Vector3 plannedPoint;
        private Vector3 lastSampledMountPosition;
        private bool mountPositionSampled;
        private bool mountMoveSlotRestored = true;
        private bool released;
        private bool terminalReported;
        private bool nativeApproachEndSignalled;
        private string terminalReason;

        public MountedCarriedApproach(
            GameMountedRelationshipService relationship,
            UnitEntityData rider,
            UnitEntityData mount,
            UnitCommand command,
            MountedCarriedCommandKind kind,
            IModLogger logger,
            Action<MountedCarriedApproach, MountedCarriedApproachOutcome> terminal)
        {
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            pairOwner = relationship.PairOwner;
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Kind = kind;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            outcome.Kind = kind.ToString();
            outcome.CommandType = command.GetType().Name;
            outcome.RiderId = rider.UniqueId;
            outcome.MountId = mount.UniqueId;
            outcome.TargetId = command.TargetUnit?.UniqueId ?? (command as UnitInteractWithObject)?.Interaction?.MapObject?.Data?.UniqueId;
        }

        internal UnitCommand Command { get; }
        internal MountedCarriedCommandKind Kind { get; }
        internal UnitMoveTo DelegatedMove => delegatedMove;
        internal bool NativePartnerMovement { get; set; }
        internal Func<bool> MovementOpportunity { get; set; }
        internal bool IsLive => !released && Command != null && !Command.IsFinished;
        internal bool HasLiveDelegatedMove => delegatedMove != null && !delegatedMove.IsFinished;
        internal bool Satisfied => outcome.Satisfied;

        // Called from the TickApproaching prefix on every native approach tick of the carried command. Returns true
        // when the native approach must be skipped (the mount is approaching), false when the native code should
        // run (the command is in range, or this driver no longer owns the command).
        internal bool Tick()
        {
            if (!IsLive) return false;
            var command = Command;
            try
            {
                RequireExactPair();
                if (Kind == MountedCarriedCommandKind.TrapInteraction) ApplyMountFootprintSafePoint(command as UnitInteractWithObject);
                // The engine's own per-tick adjustment runs first, exactly as inside native TickApproaching.
                NativeOnTickApproaching(command);
                if (!command.ShouldUnitApproach)
                {
                    if (delegatedMove != null) StopDelegatedMove();
                    if (!outcome.Satisfied)
                    {
                        outcome.Satisfied = true;
                        outcome.ApproachElapsedOperationalSeconds = lifecycle.BeganAt.HasValue
                            ? (float)Math.Max(0d, operationalClock.Seconds - lifecycle.BeganAt.Value) : 0f;
                        var point = command.ApproachPoint;
                        outcome.DistanceAtSatisfaction = Kingmaker.Utility.GeometryUtils.MechanicsDistance(point, rider.Position);
                        outcome.LineOfSightAtSatisfaction = !command.NeedLoS ||
                            !LineOfSightGeometry.Instance.HasObstacle(rider.EyePosition, point, command.GetTargetLOSObjectId());
                    }
                    // Native: FinishedApproaching, the rider's (override) StopMoving, TurnToTarget, the cooldown stamp.
                    return false;
                }
                outcome.Satisfied = false;
                var turnBased = CombatController.IsInTurnBasedCombat();
                var position = mount.Position;
                var displacement = mountPositionSampled ? HorizontalDistance(position, lastSampledMountPosition) : 0f;
                if (!mountPositionSampled || displacement >= MountedDoorApproachPolicy.DisplacementEpsilonMetres) lastSampledMountPosition = position;
                mountPositionSampled = true;
                var approachPoint = command.ApproachPoint;
                // A moving recipient: the live move is replanned once the point drifted; the replan is an ordinary
                // approach end, so the bounded no-progress rule still ends a chase that never closes.
                if (delegatedMove != null && !delegatedMove.IsFinished &&
                    MountedCarriedApproachPolicy.ShouldReplan(HorizontalDistance(plannedPoint, approachPoint), MountedCarriedApproachPolicy.ReplanEpsilonMetres))
                {
                    outcome.ReplanCount++;
                    delegatedMove.Interrupt(false);
                }
                var operationalSeconds = operationalClock.Observe(Time.frameCount,
                    Game.Instance.IsPaused ? 0f : Game.Instance.TimeController.GameDeltaTime);
                if (delegatedMove != null && delegatedMove.IsFinished &&
                    MountedCarriedApproachPolicy.OnDelegatedMoveEnded(turnBased) == MountedCarriedMoveEndAction.SignalNativeApproachEnd)
                {
                    // Turn-based: the native "path ended, executor stopped" rule ends the command (force-finished by
                    // UnitActionController.TickCommandTurnBased once the rider's override agent reports no movement).
                    ClearFinishedDelegatedMove();
                    command.NextApproachTime = TimeSpan.MaxValue;
                    nativeApproachEndSignalled = true;
                    outcome.NativeApproachEndSignalled = true;
                    logger.Info("Mounted carried approach ended short in turn-based combat; native approach-end semantics apply (" +
                        outcome.CommandType + ", riderId=" + rider.UniqueId + ").");
                    return true;
                }
                if (nativeApproachEndSignalled) return true;
                var live = delegatedMove != null && !delegatedMove.IsFinished;
                var owned = live && mount.Commands != null && ReferenceEquals(mount.Commands.GetCommand(UnitCommand.CommandType.Move), delegatedMove);
                var action = lifecycle.Tick(new MountedDoorApproachObservation
                {
                    NowSeconds = operationalSeconds,
                    InRange = false,
                    Move = delegatedMove == null ? MountedDoorApproachMoveState.None : delegatedMove.IsFinished ? MountedDoorApproachMoveState.Finished : MountedDoorApproachMoveState.Live,
                    MoveResult = delegatedMove != null && delegatedMove.IsFinished ? delegatedMove.Result.ToString() : null,
                    DistanceToPoint = HorizontalDistance(position, approachPoint),
                    DisplacementSinceLastTick = displacement,
                    StallClockRuns = MountedDoorApproachPolicy.StallClockRuns(owned, Game.Instance.IsPaused, turnBased, MovementOpportunity == null || MovementOpportunity()),
                    ReapproachSample = UnityEngine.Random.value,
                    TurnToken = turnBased ? Game.Instance.TurnBasedCombatController?.CurrentTurn : null
                });
                switch (action)
                {
                    case MountedDoorApproachAction.BeginMove:
                        if (MountMovementOwnedByAnotherCommand())
                        {
                            // Review correction B: a successor owns the mount's movement (a queued or native order that
                            // arrived without displacing this approach through Run). Nothing of it is cancelled: this
                            // approach yields, and the rider's native command waits unstarted until the mount is free.
                            outcome.YieldedToMountOwner = true;
                            outcome.SuccessorRetained = true;
                            logger.Info("Mounted carried approach yielded: another command owns the mount's movement (mountId=" + mount.UniqueId + ").");
                            Release("mount movement owned by another command");
                            return false;
                        }
                        BeginDelegatedMove(command, approachPoint);
                        return true;
                    case MountedDoorApproachAction.Retry:
                        ClearFinishedDelegatedMove();
                        return true;
                    case MountedDoorApproachAction.Stalled:
                        delegatedMove.Interrupt(false);
                        ClearFinishedDelegatedMove();
                        logger.Info("Mounted carried approach " + lifecycle.LastApproachEnd + "; retrying after the native cooldown (mountId=" + mount.UniqueId + ").");
                        return true;
                    case MountedDoorApproachAction.Exhausted:
                        if (delegatedMove != null) ClearFinishedDelegatedMove();
                        // The native command is interrupted through its own API (an unreachable target ends the order with
                        // the reason instead of a permanent waiting command); the interrupt observation releases this driver.
                        terminalReason = lifecycle.ExhaustedReason;
                        command.Interrupt();
                        return true;
                    default:
                        if (live && turnBased) DriveDelegatedMoveOnRiderTurn();
                        return true;
                }
            }
            catch (Exception exception)
            {
                terminalReason = exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Mounted carried approach", exception);
                if (!command.IsFinished) command.Interrupt();
                Release("exception");
                return true;
            }
        }

        // Owner release: Stop, a replacement rider command, dismount, boundary, disposal, or the command's own end.
        internal void Release(string reason)
        {
            if (released) return;
            released = true;
            try
            {
                StopDelegatedMove();
            }
            catch (Exception exception)
            {
                terminalReason = terminalReason ?? exception.GetType().Name + ": " + exception.Message;
                logger.Exception("Mounted carried approach cleanup", exception);
            }
            finally
            {
                ReportTerminalOnce(reason);
            }
        }

        private void ApplyMountFootprintSafePoint(UnitInteractWithObject interaction)
        {
            if (interaction == null || interaction.OverrideTarget.HasValue) return;
            var trap = (interaction.Interaction as DisableTrapInteractionComponent)?.TrapObject;
            var zone = trap == null ? null : trap.ScriptZoneTrigger;
            if (zone == null) return;
            // Native UnitInteractWithObject.UpdateTarget with the pair's actual footprint: the mount's corpulence, not
            // the carried rider's, decides where the next step would enter the trap's trigger zone.
            var direction = (trap.transform.position - mount.Position).normalized;
            var corpulence = mount.View != null ? mount.View.Corpulence : 0.5f;
            var probe = mount.Position + direction * (corpulence + MountedCarriedApproachPolicy.NativeTrapProbeMarginMetres);
            if (!zone.ContainsPosition(probe)) return;
            interaction.OverrideTarget = rider.Position + direction * MountedCarriedApproachPolicy.NativeTrapStopOffsetMetres;
            outcome.TrapSafePointApplied = true;
        }

        private void BeginDelegatedMove(UnitCommand command, Vector3 approachPoint)
        {
            // A live or queued command of another owner never reaches here (the caller yields to it). A finished
            // command still sitting in a slot is native residue the native sweep removes anyway.
            if (mount.Commands == null || MountMovementOwnedByAnotherCommand())
            {
                throw new InvalidOperationException("Mount Move slot is not idle for the carried approach.");
            }
            if (!mount.Commands.Empty) mount.Commands.RemoveFinishedAndUpdateQueue();
            plannedPoint = approachPoint;
            // The native start gate measures the RIDER's position; the mount stops a saddle margin inside the
            // command's radius so a settled mount never leaves the rider a few centimetres out of range.
            var radius = Math.Max(MinimumDelegatedRadiusMetres, command.ApproachRadius - MountSaddleMarginMetres);
            outcome.ApproachRadius = command.ApproachRadius;
            delegatedMove = new MountedDoorApproachMove(approachPoint, radius, command.NeedLoS ? command.GetTargetLOSObjectId() : 0)
            {
                CreatedByPlayer = true,
                ShowTargetMarker = false
            };
            outcome.DelegatedMoveStartCount++;
            mountMoveSlotRestored = false;
            mount.Commands.Run(delegatedMove);
            if (delegatedMove.Executor != mount ||
                mount.Commands.GetCommand(UnitCommand.CommandType.Move) != delegatedMove ||
                mount.Commands.Queue.Contains(delegatedMove))
            {
                throw new InvalidOperationException("Exact mount carried approach did not acquire only its Move slot.");
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
                outcome.DelegatedMoveTickCount++;
                delegatedMove.Tick();
            }
        }

        // Review correction B: only the carrier's exact move and the agent it still owns are touched. A successor that
        // owns the slot, another slot or the queue (including one the sweep itself activates) keeps its path.
        private void ClearFinishedDelegatedMove()
        {
            var exactMove = delegatedMove;
            if (exactMove == null) return;
            delegatedMove = null;
            var release = MountedOwnMovePolicy.Release(new MountMoveSlotHost(mount), exactMove);
            mountMoveSlotRestored = release.OwnReleased;
            if (release.SuccessorRetained) outcome.SuccessorRetained = true;
        }

        // Whether a command other than this approach's own live move owns any of the mount's slots or its queue.
        internal bool MountMovementOwnedByAnotherCommand()
        {
            return MountMoveSlotHost.HasOtherLive(mount.Commands, delegatedMove);
        }

        internal sealed class MountMoveSlotHost : IMountedOwnMoveHost
        {
            private readonly UnitEntityData mount;
            internal MountMoveSlotHost(UnitEntityData mount) { this.mount = mount; }
            public object MoveSlot => mount.Commands?.GetCommand(UnitCommand.CommandType.Move);
            public bool Holds(object command) => mount.Commands != null && mount.Commands.Contains(command as UnitCommand);
            public bool HasOtherLiveCommand(object own) => HasOtherLive(mount.Commands, own as UnitCommand);
            public bool IsFinished(object command) => (command as UnitCommand)?.IsFinished != false;
            public void Interrupt(object command) => (command as UnitCommand)?.Interrupt(false);
            public void SweepFinished() => mount.Commands?.RemoveFinishedAndUpdateQueue();
            public void StopAgent() => mount.View?.StopMoving();

            internal static bool HasOtherLive(UnitCommands commands, UnitCommand own)
            {
                if (commands == null) return false;
                if (commands.Queue.Count != 0) return true;
                var raw = commands.Raw;
                for (var index = 0; index < raw.Length; index++)
                {
                    var command = raw[index];
                    if (command != null && !command.IsFinished && !ReferenceEquals(command, own)) return true;
                }
                return false;
            }
        }

        private void StopDelegatedMove()
        {
            if (delegatedMove != null) ClearFinishedDelegatedMove();
        }

        private void RequireExactPair()
        {
            if (!relationship.IsCurrentPairOwner(pairOwner) || relationship.State != RelationshipState.Mounted ||
                relationship.Rider != rider || relationship.Mount != mount ||
                rider.Descriptor?.Pet != mount || mount.Descriptor?.Master.Value != rider ||
                !relationship.Runtime.PoseHealthy)
            {
                throw new InvalidOperationException("Mounted carried approach lost the exact active pair.");
            }
        }

        private void ReportTerminalOnce(string reason)
        {
            if (terminalReported) return;
            terminalReported = true;
            outcome.ApproachesWithoutProgress = lifecycle.ApproachesWithoutProgress;
            if (outcome.ApproachElapsedOperationalSeconds <= 0f && lifecycle.BeganAt.HasValue)
            {
                outcome.ApproachElapsedOperationalSeconds = (float)Math.Max(0d, operationalClock.Seconds - lifecycle.BeganAt.Value);
            }
            outcome.MountMoveSlotRestored = mountMoveSlotRestored;
            outcome.TerminalReason = terminalReason;
            outcome.Result = Command.IsFinished ? Command.Result.ToString() : "released:" + (reason ?? "unspecified");
            terminal(this, outcome);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // The protected virtual UnitCommand.OnTickApproaching (2.1.7b 0x060027AE), invoked with ordinary virtual
        // dispatch so UnitUseAbility's turn-based sticky-touch range rule and UnitInteractWithObject's safe-point
        // update run exactly as inside the native TickApproaching this driver replaces for the carried rider.
        private static Action<UnitCommand> CreateNativeOnTickApproachingBridge()
        {
            var method = typeof(UnitCommand).GetMethod("OnTickApproaching", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null, Type.EmptyTypes, null);
            if (method == null || method.MetadataToken != 0x060027AE || !method.IsVirtual || !method.IsFamily ||
                method.ReturnType != typeof(void) || method.Module != typeof(UnitCommand).Module)
            {
                throw new InvalidOperationException("Native UnitCommand.OnTickApproaching signature differs.");
            }
            var bridge = new DynamicMethod("KmcCarriedOnTickApproaching", typeof(void), new[] { typeof(UnitCommand) },
                typeof(MountedCarriedApproach).Module, true);
            var il = bridge.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, method);
            il.Emit(OpCodes.Ret);
            return (Action<UnitCommand>)bridge.CreateDelegate(typeof(Action<UnitCommand>));
        }
    }
}
