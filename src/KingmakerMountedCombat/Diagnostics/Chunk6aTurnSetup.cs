using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.TurnBasedMode;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pathfinding;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private UnitMoveTo chunk6aActingSetupCommand;
        private TurnController chunk6aActingSetupTurn;
        private JObject chunk6aActingSetup;
        private Vector3 chunk6aActingSetupOrigin, chunk6aActingSetupDestination;
        private int chunk6aActingSetupTraceStart;
        private bool chunk6aActingSetupComplete;

        // Generate points from the measured pair geometry, then query native navigation.
        // Every rejected proposal is retained even if no ground command can be admitted.
        private Vector3 FindChunk6aActingDestination()
        {
            if (AstarPath.active == null) throw new InvalidOperationException("Active native navigation graph is unavailable.");
            var origin = rider.Position; var targetPosition = horse.Position;
            var separation = HorizontalDistance(origin, targetPosition);
            var candidates = new JArray();
            observations["chunk6aActingDestinationSearch"] = new JObject
            {
                ["contract"] = "bounded-pair-relative-native-ground-search",
                ["origin"] = CapturePosition(origin), ["target"] = CapturePosition(targetPosition),
                ["originNavigation"] = CaptureChunk6aOriginNavigation(),
                ["initialSeparation"] = separation, ["requestedTravel"] = 0.6f,
                ["travelTolerance"] = 0.15f, ["maximumSeparationIncrease"] = 0.15f,
                ["routeTolerance"] = MountedCombatSpatialPolicy.DiagnosticPlacementTolerance,
                ["candidates"] = candidates
            };
            foreach (var proposal in NativeGroundFixturePolicy.ActingProposals(
                new PoseVector3(origin.x, origin.y, origin.z), new PoseVector3(targetPosition.x, targetPosition.y, targetPosition.z)))
            {
                var requested = new Vector3(proposal.X, proposal.Y, proposal.Z);
                var nearest = AstarPath.active.GetNearest(requested); var point = nearest.clampedPosition;
                var candidate = new JObject { ["requested"] = CapturePosition(requested), ["point"] = CapturePosition(point),
                    ["walkable"] = nearest.node != null && nearest.node.Walkable, ["eligible"] = false };
                candidates.Add(candidate);
                if (nearest.node == null || !nearest.node.Walkable) continue;
                var routeEnd = ObstacleAnalyzer.TraceAlongNavmesh(origin, point);
                var routeResidual = HorizontalDistance(routeEnd, point);
                var footprint = NativeGroundMovementObservation.CaptureFootprint(rider, point);
                var residuals = ((JArray)footprint["probes"]).Select(probe => (float)probe["residual"]).ToArray();
                var footprintResidual = residuals.Any(value => float.IsNaN(value) || float.IsInfinity(value)) ? float.NaN : residuals.Max();
                var blockers = Game.Instance.State.Units.Where(unit => unit != rider && unit.IsInState && unit.View != null &&
                    HorizontalDistance(point, unit.Position) < rider.View.Corpulence + unit.View.Corpulence + 0.05f)
                    .Select(unit => unit.UniqueId).ToArray();
                var travel = HorizontalDistance(origin, point); var proposedSeparation = HorizontalDistance(point, targetPosition);
                var eligible = NativeGroundFixturePolicy.IsActingStep(separation, proposedSeparation, travel,
                    routeResidual, footprintResidual, blockers.Length != 0);
                candidate["travel"] = travel; candidate["separation"] = proposedSeparation;
                candidate["routeEnd"] = CapturePosition(routeEnd); candidate["routeResidual"] = routeResidual;
                candidate["footprint"] = footprint; candidate["blockers"] = new JArray(blockers); candidate["eligible"] = eligible;
                if (eligible) return point;
            }
            throw new InvalidOperationException("No bounded pair-relative native Acting setup point satisfied unchanged travel/separation and clear footprint constraints.");
        }

        // The turn-based path preview the visualizer holds for the rider at order time, read-only: its end and the
        // end's distance to the ordered point name whether a stale preview existed (frozen 205 stage 35).
        private static JObject CaptureChunk6aVisualizerPath(Path path, Vector3 destination)
        {
            var points = path?.vectorPath;
            var present = points != null && points.Count > 0;
            return new JObject {
                ["present"] = present, ["pathObject"] = path == null ? 0 : RuntimeHelpers.GetHashCode(path),
                ["pointCount"] = points == null ? 0 : points.Count,
                ["end"] = present ? CapturePosition(points[points.Count - 1]) : null,
                ["endResidual"] = present ? (JToken)HorizontalDistance(points[points.Count - 1], destination) : JValue.CreateNull()
            };
        }

        // Native Preparing persists while an able player unit is idle. A real, short
        // ground order enters Acting; its debt is carried into the later Mount baseline.
        private bool PrepareChunk6aNativeActingTurn(TurnController turn)
        {
            if (chunk6aActingSetupComplete) return turn.IsActing;
            if (chunk6aActingSetupCommand == null)
            {
                if (turn.Status != TurnController.TurnStatus.Preparing) return turn.IsActing;
                if (!Chunk6aIdle || !EnsureChunk6aRiderSelection("phase3d-horse-runtime-exception")) return false;
                chunk6aActingSetupOrigin = rider.Position;
                chunk6aActingSetupDestination = FindChunk6aActingDestination();
                chunk6aActingSetupTurn = turn;
                chunk6aActingSetupTraceStart = allocationTrace.EventCount;
                chunk6aActingSetup = new JObject
                {
                    ["contract"] = "native-rider-ground-order-before-mount-baseline",
                    ["beforeTurnObject"] = RuntimeHelpers.GetHashCode(turn),
                    ["before"] = CaptureChunk6aState("native-acting-setup-before"),
                    ["origin"] = CapturePosition(chunk6aActingSetupOrigin),
                    ["destination"] = CapturePosition(chunk6aActingSetupDestination),
                    ["placementTolerance"] = MountedCombatSpatialPolicy.DiagnosticPlacementTolerance
                };
                observations["chunk6aNativeActingSetup"] = chunk6aActingSetup;
                UnitMoveTo ownedCommand = null; var callbackCount = 0;
                ClickGroundHandler.MoveSelectedUnitsToPoint(chunk6aActingSetupDestination,
                    ClickGroundHandler.GetDefaultDirection(chunk6aActingSetupDestination),
                    preview: false, showTargetMarker: false, formationSpaceFactor: 1f, ignoreHold: true,
                    commandRunner: (unit, point, speedLimit, orientation, delay, marker) => {
                        var selected = Game.Instance.UI.SelectionManager.SelectedUnits;
                        if (++callbackCount != 1 || unit != rider || selected.Count != 1 || selected[0] != rider ||
                            !ReferenceEquals(Game.Instance.TurnBasedCombatController.CurrentTurn, turn) ||
                            Vector3.Distance(point, chunk6aActingSetupDestination) > 0.000001f)
                            throw new InvalidOperationException("Precise Acting input changed its single rider, native turn or destination.");
                        ownedCommand = NativeActingGroundInput.Create(point, speedLimit, orientation, delay, marker);
                        // Turn-based UnitCommand.TickApproaching (060027A6) follows PathVisualizer.CurrentPathForUnit
                        // (0600700F) whenever no ForcedPath is set: frozen 205 chunk6a-mount-spent-standard-tb followed a
                        // stale visualizer preview (the real cursor's path toward the mount) 7.16 m to a Success terminal
                        // 7.30 m from the ordered point. The precise setup order carries its own route (set_ForcedPath
                        // 0600279B): the straight route whose navmesh trace and footprint the destination search already
                        // proved clear. The preview the visualizer held at order time is recorded as evidence.
                        var staleVisualizerPath = PathVisualizer.Instance?.CurrentPathForUnit(unit.View);
                        var forcedRoute = new List<Vector3> { unit.Position, point };
                        ownedCommand.ForcedPath = new ForcedPath(forcedRoute);
                        unit.Commands.Run(ownedCommand);
                        chunk6aActingSetup["nativeGroundInput"] = new JObject {
                            ["contract"] = "native-ground-input-with-one-precise-setup-command",
                            ["methodToken"] = "060093DB", ["directionToken"] = "060093D9", ["constructorToken"] = "060026FF",
                            ["callbackCount"] = callbackCount, ["actorId"] = unit.UniqueId,
                            ["selectedIds"] = new JArray(selected.Select(actor => actor.UniqueId)),
                            ["commandObject"] = RuntimeHelpers.GetHashCode(ownedCommand), ["frame"] = Time.frameCount,
                            ["targetPoint"] = CapturePosition(ownedCommand.Target), ["approachRadius"] = ownedCommand.ApproachRadius,
                            ["createdByPlayer"] = ownedCommand.CreatedByPlayer,
                            ["forcedPath"] = new JObject {
                                ["contract"] = "forced-path-along-the-verified-straight-route", ["setterToken"] = "0600279B",
                                ["approachingToken"] = "060027A6", ["visualizerToken"] = "0600700F",
                                ["points"] = new JArray(forcedRoute.Select(CapturePosition)),
                                ["endResidual"] = HorizontalDistance(forcedRoute[forcedRoute.Count - 1], point),
                                ["startResidual"] = HorizontalDistance(forcedRoute[0], unit.Position)
                            },
                            ["staleVisualizerPath"] = CaptureChunk6aVisualizerPath(staleVisualizerPath, point)
                        };
                    });
                chunk6aActingSetupCommand = rider.Commands.Move as UnitMoveTo;
                chunk6aActingSetup["admittedCommand"] = CaptureOrdinaryCommand(chunk6aActingSetupCommand);
                if (callbackCount != 1 || !ReferenceEquals(ownedCommand, chunk6aActingSetupCommand) ||
                    chunk6aActingSetupCommand == null || chunk6aActingSetupCommand.Executor != rider ||
                    !chunk6aActingSetupCommand.CreatedByPlayer)
                    throw new InvalidOperationException("Native rider turn setup admitted no exact player ground command: " +
                        chunk6aActingSetup.ToString(Formatting.None));
                return false;
            }
            if (!ReferenceEquals(turn, chunk6aActingSetupTurn))
                throw new InvalidOperationException("Native rider turn changed before its setup command settled.");
            if (!chunk6aActingSetupCommand.IsFinished || !Chunk6aIdle || rider.View.AgentASP.IsReallyMoving) return false;
            chunk6aActingSetup["afterTurnObject"] = RuntimeHelpers.GetHashCode(turn);
            chunk6aActingSetup["terminalCommand"] = CaptureOrdinaryCommand(chunk6aActingSetupCommand);
            chunk6aActingSetup["createdByPlayer"] = chunk6aActingSetupCommand.CreatedByPlayer;
            chunk6aActingSetup["after"] = CaptureChunk6aState("native-acting-setup-after");
            chunk6aActingSetup["movement"] = NativeGroundMovementObservation.Capture(rider, chunk6aActingSetupCommand);
            chunk6aActingSetup["events"] = allocationTrace.EventsSince(chunk6aActingSetupTraceStart);
            chunk6aActingSetup["traceComplete"] = allocationTrace.Complete;
            chunk6aActingSetup["displacement"] = HorizontalDistance(chunk6aActingSetupOrigin, rider.Position);
            chunk6aActingSetup["residual"] = HorizontalDistance(chunk6aActingSetupDestination, rider.Position);
            chunk6aActingSetup["sameNativeTurn"] = ReferenceEquals(turn, chunk6aActingSetupTurn);
            var preciseInput = (JObject)chunk6aActingSetup["nativeGroundInput"];
            preciseInput["terminalApproachRadius"] = chunk6aActingSetupCommand.ApproachRadius;
            preciseInput["agentApproachRadius"] = rider.View.AgentASP.ApproachRadius;
            NativeActingGroundInput.AssertComplete(chunk6aActingSetup);
            if (chunk6aActingSetupCommand.Result != UnitCommand.ResultType.Success || !turn.IsActing ||
                (float)chunk6aActingSetup["displacement"] <= 0.1f ||
                (float)chunk6aActingSetup["residual"] > MountedCombatSpatialPolicy.DiagnosticPlacementTolerance)
                throw new InvalidOperationException("Native rider ground setup did not settle on the same Acting turn: " +
                    chunk6aActingSetup.ToString(Formatting.None));
            chunk6aActingSetupComplete = true;
            return true;
        }
    }
}
