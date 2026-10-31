using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;
using Pathfinding;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // Sprint 2 (SPACE-01). The pair's logical footprint on the navmesh, sampled through the installed
    // ObstacleAnalyzer and A* contracts. The footprint is the mount's movement-agent corpulence around
    // its entity position; the carried rider occupies nothing of its own.
    internal static class MountedFootprintProbe
    {
        internal static MountedFootprintSample Sample(Vector3 center, float radius)
        {
            var centerInfo = ObstacleAnalyzer.GetNearestNode(center);
            var centerInside = ObstacleAnalyzer.IsPointInsideNavMesh(center);
            var origin = centerInside ? center : centerInfo.clampedPosition;
            var inside = 0; var reachable = 0;
            for (var i = 0; i < MountedDoorOccupancyPolicy.FootprintRingSamples; i++)
            {
                var angle = 360f / MountedDoorOccupancyPolicy.FootprintRingSamples * i;
                var point = center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                var pointInside = ObstacleAnalyzer.IsPointInsideNavMesh(point);
                if (!pointInside) continue;
                inside++;
                var pointInfo = ObstacleAnalyzer.GetNearestNode(point);
                var traced = ObstacleAnalyzer.TraceAlongNavmesh(origin, pointInfo.clampedPosition);
                if (Horizontal(traced, pointInfo.clampedPosition) <= MountedDoorOccupancyPolicy.ReachabilityEpsilonMetres &&
                    centerInfo.node != null && pointInfo.node != null && PathUtilities.IsPathPossible(centerInfo.node, pointInfo.node))
                {
                    reachable++;
                }
            }
            return new MountedFootprintSample(centerInside, MountedDoorOccupancyPolicy.FootprintRingSamples, inside, reachable);
        }

        // Whether the footprint circle overlaps the door's cut volume in the ground plane.
        internal static bool IntersectsCut(NavmeshCut cut, Vector3 center, float radius)
        {
            if (cut == null) return false;
            var bounds = cut.GetBounds();
            var flat = new Vector3(center.x, bounds.center.y, center.z);
            return bounds.SqrDistance(flat) <= radius * radius;
        }

        internal static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }

    // The owned post-interaction lifecycle of one mounted door click. The command's native action ends
    // the moment the door is interacted with (turn-based cost semantics stay native); the collision
    // change lands later, when the A* tile handler applies the door's cut. This object, owned and
    // pumped by the pair's controller, waits for that settled boundary (bounded), samples the pair's
    // footprint, and, when the footprint is not clear, runs exactly one owned native move to a safe,
    // reachable, body-clear destination, observing its completion, interruption or replacement and the
    // footprint afterwards. Nothing is transported, no collision is disabled, no action is spent.
    internal sealed class MountedDoorOccupancySettlement
    {
        private readonly GameMountedRelationshipService relationship;
        private readonly MountedPairOwner<UnitEntityData, GameMountedRelationshipService> pairOwner;
        private readonly UnitEntityData rider;
        private readonly UnitEntityData mount;
        private readonly StandardDoor door;
        private readonly NavmeshCut cut;
        private readonly bool expectedDoorOpen;
        private readonly IModLogger logger;
        private readonly MountedDoorInteractionOutcome outcome;
        // Turn-based combat: the recovery move is ticked natively only under paired activation (the unified turn
        // coordinator); without it nothing drives a mount move off the rider's own command, so recovery is refused.
        private readonly bool turnBasedRecoveryDrivenNatively;
        private readonly Func<bool> movementOpportunity;
        private readonly TimeSpan began;
        private readonly MountedDoorSettlementLifecycle lifecycle = new MountedDoorSettlementLifecycle(MountedDoorOccupancyPolicy.SettleOperationalSecondsLimit);
        // Review 2026-10-31: the settlement wait and the recovery stall are measured by the operational clock (the
        // engine's gameplay timestep on the frames this settlement was ticked), never by campaign GameTime, which the
        // native turn-based controller rewinds to the turn's start on every frame of an acting turn. The wait is a
        // bounded operational wait independent of any movement opportunity; the recovery stall keeps its opportunity gate.
        private readonly MountedDoorOperationalClock operationalClock = new MountedDoorOperationalClock();
        private int phase;
        private UnitMoveTo recoveryMove;
        private Vector3 lastSampledPosition;
        private bool positionSampled;
        private double lastSampleOperationalSeconds;
        private float stalledSeconds;

        internal MountedDoorOccupancySettlement(
            GameMountedRelationshipService relationship,
            UnitEntityData rider,
            UnitEntityData mount,
            StandardDoor door,
            bool expectedDoorOpen,
            IModLogger logger,
            MountedDoorInteractionOutcome outcome,
            bool turnBasedRecoveryDrivenNatively,
            Func<bool> movementOpportunity)
        {
            this.turnBasedRecoveryDrivenNatively = turnBasedRecoveryDrivenNatively;
            this.movementOpportunity = movementOpportunity;
            this.relationship = relationship ?? throw new ArgumentNullException(nameof(relationship));
            pairOwner = relationship.PairOwner;
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.mount = mount ?? throw new ArgumentNullException(nameof(mount));
            this.door = door ?? throw new ArgumentNullException(nameof(door));
            cut = door.GetComponentInChildren<NavmeshCut>();
            this.expectedDoorOpen = expectedDoorOpen;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            began = GameTime;
            outcome.OccupancySettled = false;
            outcome.RecoveryDisposition = "pending";
            outcome.RecoveryTerminal = MountedDoorRecoveryTerminal.None.ToString();
        }

        internal bool IsFinished => phase >= 2;
        internal UnitMoveTo RecoveryMove => recoveryMove != null && !recoveryMove.IsFinished ? recoveryMove : null;
        internal UnitEntityData Mount => mount;
        internal MountedDoorInteractionOutcome Outcome => outcome;

        internal void Tick()
        {
            if (IsFinished) return;
            try
            {
                operationalClock.Observe(Time.frameCount, Game.Instance.IsPaused ? 0f : Game.Instance.TimeController.GameDeltaTime);
                if (!PairIntact())
                {
                    EndRecoveryMove();
                    outcome.RecoveryTerminal = MountedDoorRecoveryTerminal.PairLost.ToString();
                    if (outcome.RecoveryDisposition == "pending") outcome.RecoveryDisposition = "pair-lost";
                    Finish();
                    return;
                }
                if (phase == 0)
                {
                    WaitForSettledNavmesh();
                }
                else if (phase == 1)
                {
                    ObserveRecovery();
                }
            }
            catch (Exception exception)
            {
                logger.Exception("Mounted door occupancy settlement", exception);
                outcome.RecoveryDisposition = "exception: " + exception.GetType().Name + ": " + exception.Message;
                EndRecoveryMove();
                Finish();
            }
        }

        internal void Cancel(string reason)
        {
            if (IsFinished) return;
            EndRecoveryMove();
            outcome.RecoveryTerminal = MountedDoorRecoveryTerminal.Cancelled.ToString();
            if (outcome.RecoveryDisposition == "pending") outcome.RecoveryDisposition = "cancelled";
            outcome.RecoveryCancelReason = reason;
            Finish();
        }

        private void WaitForSettledNavmesh()
        {
            var elapsed = operationalClock.Seconds;
            var settled = MountedDoorOccupancyPolicy.NavmeshSettled(
                door.GetState() == expectedDoorOpen,
                cut != null && cut.RequiresUpdate(),
                AstarPath.active != null && AstarPath.active.IsAnyGraphUpdatesQueued,
                Time.frameCount > Pathfinding.Util.TileHandler.LastUpdateFrame);
            var step = lifecycle.Observe(settled, Math.Max(0d, elapsed), Time.frameCount);
            if (step == MountedDoorSettlementStep.Waiting) return;
            outcome.OccupancySettled = lifecycle.Settled;
            outcome.OccupancySettleOperationalSeconds = (float)lifecycle.ElapsedSeconds;
            outcome.OccupancySettleGameSeconds = (float)Math.Max(0d, (GameTime - began).TotalSeconds);
            if (step == MountedDoorSettlementStep.Unresolved)
            {
                // Review correction A: the graph never confirmed inside the bound. Nothing is sampled or searched and no
                // move is issued on it; the door's own interaction result stands separately on the outcome.
                var unresolved = MountedDoorSettlementLifecycle.Resolve(step, SampleCurrentFootprint, () => false, false, false, false);
                outcome.RecoveryDisposition = unresolved.Disposition.ToString();
                outcome.OccupancyRecovery = MountedDoorOccupancyPolicy.LegacyRecoveryWord(unresolved.Disposition);
                outcome.FootprintClear = null;
                outcome.OccupancyInsideNavmesh = null;
                logger.Warning("Mounted door occupancy unresolved: the door's navmesh change did not settle within " +
                    MountedDoorOccupancyPolicy.SettleOperationalSecondsLimit + " operational seconds; mountId=" + mount.UniqueId +
                    "; no footprint sample, no recovery (the interaction result is recorded separately).");
                Finish();
                return;
            }
            EvaluateFootprint(step);
        }

        private MountedFootprintSample SampleCurrentFootprint()
        {
            return MountedFootprintProbe.Sample(mount.Position, Corpulence());
        }

        private void EvaluateFootprint(MountedDoorSettlementStep step)
        {
            var position = mount.Position;
            var corpulence = Corpulence();
            var nearest = ObstacleAnalyzer.GetNearestNode(position);
            Vector3 destination = position;
            var reasons = new Dictionary<string, int>(StringComparer.Ordinal);
            var evaluated = 0;
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            var turnBased = CombatController.IsInTurnBasedCombat();
            var riderTurnActing = turn != null && turn.Unit == rider && turn.IsActing && turnBasedRecoveryDrivenNatively;
            var mountIdle = mount.Commands != null && mount.Commands.Empty && mount.Commands.Queue.Count == 0;
            // The sampler and the candidate search run only here, on the confirmed graph (MountedDoorSettlementLifecycle).
            var verdict = MountedDoorSettlementLifecycle.Resolve(step,
                () => MountedFootprintProbe.Sample(position, corpulence),
                () => SearchRecoveryDestination(position, corpulence, nearest, reasons, out destination, out evaluated),
                turnBased, riderTurnActing, mountIdle);
            var sample = verdict.Sample;
            outcome.MountCorpulence = corpulence;
            outcome.FootprintClear = sample.IsClear;
            outcome.FootprintRingInside = sample.RingInside;
            outcome.FootprintRingReachable = sample.RingReachable;
            outcome.OccupancyInsideNavmesh = sample.CenterInside;
            outcome.OccupancyNearestNodeDistance = nearest.node != null && nearest.node.Walkable
                ? MountedFootprintProbe.Horizontal(position, nearest.clampedPosition) : float.PositiveInfinity;
            outcome.RecoveryCandidatesEvaluated = evaluated;
            outcome.RecoveryCandidateReasons = Summarize(reasons);
            var disposition = verdict.Disposition;
            outcome.RecoveryDisposition = disposition.ToString();
            outcome.OccupancyRecovery = MountedDoorOccupancyPolicy.LegacyRecoveryWord(disposition);
            if (!verdict.MoveAuthorized)
            {
                if (disposition != MountedDoorRecoveryDisposition.NotNeeded)
                {
                    logger.Warning("Mounted door occupancy not recovered: mountId=" + mount.UniqueId + "; disposition=" + disposition +
                        "; footprint=" + sample.RingInside + "/" + sample.RingReachable + "/" + sample.RingSamples +
                        "; candidates=" + outcome.RecoveryCandidateReasons);
                }
                Finish();
                return;
            }
            recoveryMove = new UnitMoveTo(destination, 0.1f) { CreatedByPlayer = false, ShowTargetMarker = false };
            outcome.RecoveryDestinationDistance = MountedFootprintProbe.Horizontal(position, destination);
            mount.Commands.Run(recoveryMove);
            if (recoveryMove.Executor != mount || mount.Commands.GetCommand(UnitCommand.CommandType.Move) != recoveryMove ||
                mount.Commands.Queue.Contains(recoveryMove))
            {
                EndRecoveryMove();
                throw new InvalidOperationException("Exact door occupancy recovery did not acquire only the mount's Move slot.");
            }
            outcome.MountMoveSlotRestored = false;
            positionSampled = false; stalledSeconds = 0f;
            logger.Info("Mounted door occupancy recovery: mountId=" + mount.UniqueId + "; distance=" +
                outcome.RecoveryDestinationDistance.ToString("F3") + " m; footprint=" + sample.RingInside + "/" + sample.RingReachable + "/" + sample.RingSamples);
            phase = 1;
        }

        private bool SearchRecoveryDestination(Vector3 position, float corpulence, Pathfinding.NNInfo centerInfo,
            Dictionary<string, int> reasons, out Vector3 destination, out int evaluated)
        {
            destination = position;
            evaluated = 0;
            var origin = centerInfo.clampedPosition;
            for (var i = 0; i < MountedDoorOccupancyPolicy.RecoveryCandidateCount; i++)
            {
                MountedDoorOccupancyPolicy.Offset(i, out _, out _, out var dx, out var dz);
                var requested = new Vector3(position.x + dx, position.y, position.z + dz);
                var info = ObstacleAnalyzer.GetNearestNode(requested);
                var facts = new MountedDoorRecoveryCandidateFacts { Walkable = info.node != null && info.node.Walkable };
                evaluated++;
                if (facts.Walkable)
                {
                    var clamped = info.clampedPosition;
                    facts.ClampResidualMetres = MountedFootprintProbe.Horizontal(requested, clamped);
                    facts.DistanceMetres = MountedFootprintProbe.Horizontal(position, clamped);
                    facts.SameNavmeshArea = centerInfo.node != null && PathUtilities.IsPathPossible(centerInfo.node, info.node);
                    facts.TraceResidualMetres = MountedFootprintProbe.Horizontal(ObstacleAnalyzer.TraceAlongNavmesh(origin, clamped), clamped);
                    facts.InsideDoorCut = MountedFootprintProbe.IntersectsCut(cut, clamped, corpulence);
                    // The footprint sample is the costly step; only a candidate that passed everything else pays it.
                    if (MountedDoorOccupancyPolicy.CandidateReason(new MountedDoorRecoveryCandidateFacts { Walkable = true,
                            ClampResidualMetres = facts.ClampResidualMetres, DistanceMetres = facts.DistanceMetres, SameNavmeshArea = facts.SameNavmeshArea,
                            TraceResidualMetres = facts.TraceResidualMetres, InsideDoorCut = facts.InsideDoorCut, FootprintClear = true }) == "admitted")
                    {
                        facts.FootprintClear = MountedFootprintProbe.Sample(clamped, corpulence).IsClear;
                    }
                }
                var reason = MountedDoorOccupancyPolicy.CandidateReason(facts);
                reasons[reason] = (reasons.TryGetValue(reason, out var count) ? count : 0) + 1;
                if (reason == "admitted")
                {
                    destination = info.clampedPosition;
                    return true;
                }
            }
            return false;
        }

        private void ObserveRecovery()
        {
            var move = recoveryMove;
            var owned = mount.Commands != null && mount.Commands.GetCommand(UnitCommand.CommandType.Move) == move;
            var now = operationalClock.Seconds;
            var position = mount.Position;
            if (!positionSampled)
            {
                positionSampled = true; lastSampledPosition = position; lastSampleOperationalSeconds = now;
            }
            else
            {
                var elapsed = (float)(now - lastSampleOperationalSeconds);
                lastSampleOperationalSeconds = now;
                // The same clock rule as the approach, on the operational clock: real time counts stillness; turn-based
                // combat only on frames the native movement override admitted the mount; a pause, an off-turn frame or an
                // oversized step is re-sampled, never charged.
                var clockRuns = MountedDoorApproachPolicy.StallClockRuns(owned && !move.IsFinished, Game.Instance.IsPaused,
                    CombatController.IsInTurnBasedCombat(), movementOpportunity == null || movementOpportunity());
                if (MountedFootprintProbe.Horizontal(position, lastSampledPosition) >= MountedDoorOccupancyPolicy.DisplacementEpsilonMetres)
                {
                    lastSampledPosition = position; stalledSeconds = 0f;
                }
                else if (clockRuns && elapsed <= MountedDoorApproachPolicy.MaximumTickGapSeconds)
                {
                    stalledSeconds += Math.Max(0f, elapsed);
                }
            }
            var terminal = MountedDoorOccupancyPolicy.RecoveryTerminal(true, move.IsFinished, owned,
                move.IsFinished && move.Result == UnitCommand.ResultType.Success, stalledSeconds);
            if (terminal == MountedDoorRecoveryTerminal.None) return;
            if (terminal == MountedDoorRecoveryTerminal.Stalled) move.Interrupt(false);
            EndRecoveryMove();
            outcome.RecoveryTerminal = terminal.ToString();
            var after = MountedFootprintProbe.Sample(mount.Position, Corpulence());
            outcome.FootprintClearAfterRecovery = after.IsClear;
            outcome.OccupancyInsideNavmesh = after.CenterInside;
            logger.Info("Mounted door occupancy recovery ended: mountId=" + mount.UniqueId + "; terminal=" + terminal +
                "; footprintClearAfter=" + after.IsClear + "; slotRestored=" + outcome.MountMoveSlotRestored);
            Finish();
        }

        private void EndRecoveryMove()
        {
            var move = recoveryMove;
            if (move == null) return;
            var commands = mount.Commands;
            var slot = commands?.GetCommand(UnitCommand.CommandType.Move);
            // Only the mod's own move is touched: when another command (a native order, AI, a cutscene) now holds the
            // slot, the agent is theirs and is left alone.
            var slotOursOrEmpty = slot == null || ReferenceEquals(slot, move);
            if (!move.IsFinished && ReferenceEquals(slot, move)) move.Interrupt(false);
            if (slotOursOrEmpty && commands != null && commands.Contains(move) && commands.Queue.Count == 0)
            {
                commands.RemoveFinishedAndUpdateQueue();
            }
            if (slotOursOrEmpty) mount.View?.StopMoving();
            outcome.MountMoveSlotRestored = commands != null &&
                commands.GetCommand(UnitCommand.CommandType.Move) == null &&
                !commands.Contains(move) && commands.Queue.Count == 0;
            recoveryMove = null;
        }

        private void Finish()
        {
            phase = 2;
        }

        private bool PairIntact()
        {
            return relationship.IsCurrentPairOwner(pairOwner) && relationship.State == RelationshipState.Mounted &&
                relationship.Rider == rider && relationship.Mount == mount && mount.View?.AgentASP != null;
        }

        private float Corpulence()
        {
            var agent = mount.View?.AgentASP;
            return agent != null && agent.Corpulence > 0f ? agent.Corpulence : 0.5f;
        }

        private static string Summarize(Dictionary<string, int> reasons)
        {
            if (reasons.Count == 0) return "";
            var keys = new List<string>(reasons.Keys);
            keys.Sort(StringComparer.Ordinal);
            var parts = new List<string>(keys.Count);
            foreach (var key in keys) parts.Add(key + "=" + reasons[key]);
            return string.Join(",", parts.ToArray());
        }

        private static TimeSpan GameTime => Game.Instance.TimeController.GameTime;
    }
}
