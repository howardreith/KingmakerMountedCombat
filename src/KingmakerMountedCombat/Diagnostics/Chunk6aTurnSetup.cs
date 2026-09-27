using System;
using System.Runtime.CompilerServices;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                var separation = HorizontalDistance(rider.Position, horse.Position);
                chunk6aActingSetupDestination = FindWalkablePoint(rider.Position, 0.6f, 0.15f, point =>
                    HorizontalDistance(point, horse.Position) >= separation &&
                    HorizontalDistance(point, horse.Position) <= separation + 0.15f &&
                    HorizontalDistance(ObstacleAnalyzer.TraceAlongNavmesh(rider.Position, point), point) <=
                        MountedCombatSpatialPolicy.DiagnosticPlacementTolerance);
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
                ClickGroundHandler.MoveSelectedUnitsToPoint(chunk6aActingSetupDestination, false);
                chunk6aActingSetupCommand = rider.Commands.Move as UnitMoveTo;
                chunk6aActingSetup["admittedCommand"] = CaptureOrdinaryCommand(chunk6aActingSetupCommand);
                if (chunk6aActingSetupCommand == null || chunk6aActingSetupCommand.Executor != rider ||
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
