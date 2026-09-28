using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private UnitMoveTo chunk6aPositioningCommand;
        private Vector3 chunk6aPositioningDestination;
        private JObject chunk6aPositioningEvidence;
        private int chunk6aPositioningTraceStart;
        private bool chunk6aPositioningComplete;

        private JObject CaptureChunk6aOriginNavigation()
        {
            if (AstarPath.active == null) throw new InvalidOperationException("Active native navigation graph is unavailable.");
            var origin = rider.Position; var nearest = AstarPath.active.GetNearest(origin);
            return new JObject { ["position"] = CapturePosition(origin), ["nearest"] = CapturePosition(nearest.clampedPosition),
                ["projectionResidual"] = HorizontalDistance(origin, nearest.clampedPosition),
                ["walkable"] = nearest.node != null && nearest.node.Walkable,
                ["footprint"] = NativeGroundMovementObservation.CaptureFootprint(rider, origin),
                ["actingClearance"] = NativeGroundMovementObservation.CaptureFootprint(rider, origin,
                    Math.Max(0.5f, rider.View.Corpulence) + 0.75f) };
        }

        private Vector3 FindChunk6aPreCombatPosition(JArray candidates)
        {
            var origin = rider.Position; var center = horse.Position;
            var direction = origin - center; direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) throw new InvalidOperationException("Pre-combat pair geometry is degenerate.");
            direction.Normalize();
            foreach (var radius in new[] { 2.3f, 2.65f, 2f })
                for (var index = 0; index < 24; index++)
                {
                    var angle = index == 0 ? 0f : (index % 2 == 0 ? index : -index) * 15f;
                    var requested = center + Quaternion.Euler(0, angle, 0) * direction * radius;
                    var nearest = AstarPath.active.GetNearest(requested); var point = nearest.clampedPosition;
                    var candidate = new JObject { ["requested"] = CapturePosition(requested), ["point"] = CapturePosition(point),
                        ["walkable"] = nearest.node != null && nearest.node.Walkable, ["requestedSeparation"] = radius, ["eligible"] = false };
                    candidates.Add(candidate);
                    if (nearest.node == null || !nearest.node.Walkable) continue;
                    var routeEnd = ObstacleAnalyzer.TraceAlongNavmesh(origin, point);
                    var footprint = NativeGroundMovementObservation.CaptureFootprint(rider, point, (float)chunk6aPositioningEvidence["clearanceRadius"]);
                    var residuals = ((JArray)footprint["probes"]).Select(probe => (float)probe["residual"]).ToArray();
                    var footprintResidual = residuals.Any(v => float.IsNaN(v) || float.IsInfinity(v)) ? float.NaN : residuals.Max();
                    var blockers = Game.Instance.State.Units.Where(unit => unit != rider && unit.IsInState && unit.View != null &&
                        HorizontalDistance(point, unit.Position) < rider.View.Corpulence + unit.View.Corpulence + 0.05f).Select(unit => unit.UniqueId).ToArray();
                    var travel = HorizontalDistance(origin, point); var separation = HorizontalDistance(center, point);
                    var routeResidual = HorizontalDistance(routeEnd, point);
                    var eligible = NativeGroundFixturePolicy.IsPreCombatPosition(Math.Round(radius, 2), separation, travel, routeResidual, footprintResidual, blockers.Length != 0);
                    candidate["travel"] = travel; candidate["separation"] = separation; candidate["routeEnd"] = CapturePosition(routeEnd);
                    candidate["routeResidual"] = routeResidual; candidate["footprint"] = footprint;
                    candidate["blockers"] = new JArray(blockers); candidate["eligible"] = eligible;
                    if (eligible) return point;
                }
            throw new InvalidOperationException("No bounded pre-combat rider position has a clear native route and Acting clearance; origin and every proposal retained.");
        }

        // Separate native ground input before the fresh encounter. The later short
        // Acting-step contract, relationship baseline and native resources stay intact.
        private bool TickChunk6aPreCombatPositioning()
        {
            if (chunk6aPositioningComplete) return true;
            if (rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat)
                throw new InvalidOperationException("Pre-combat positioning entered an encounter.");
            if (chunk6aPositioningCommand == null)
            {
                if (!EnsureChunk6aRiderSelection("CM01-combat-mount-setup")) return false;
                var candidates = new JArray();
                chunk6aPositioningEvidence = new JObject { ["contract"] = "native-ground-positioning-before-fresh-encounter",
                    ["frameBefore"] = Time.frameCount, ["gameTicksBefore"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["beforeInCombat"] = rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat,
                    ["before"] = CaptureChunk6aCausalState(), ["originNavigation"] = CaptureChunk6aOriginNavigation(),
                    ["minimumTravel"] = 0.25f, ["maximumTravel"] = 4f,
                    ["clearanceRadius"] = Math.Max(0.5f, rider.View.Corpulence) + 0.75f, ["candidates"] = candidates };
                observations["chunk6aPreCombatPositioning"] = chunk6aPositioningEvidence;
                chunk6aPositioningDestination = FindChunk6aPreCombatPosition(candidates);
                chunk6aPositioningTraceStart = allocationTrace.EventCount;
                chunk6aPath = new NativeCommandPathProbe(rider);
                ClickGroundHandler.MoveSelectedUnitsToPoint(chunk6aPositioningDestination, false);
                chunk6aPositioningCommand = rider.Commands.Move as UnitMoveTo;
                chunk6aPositioningEvidence["admittedCommand"] = CaptureOrdinaryCommand(chunk6aPositioningCommand);
                if (chunk6aPositioningCommand == null || chunk6aPositioningCommand.Executor != rider || !chunk6aPositioningCommand.CreatedByPlayer)
                    throw new InvalidOperationException("Pre-combat positioning admitted no exact player ground command.");
                chunk6aPath.Bind(chunk6aPositioningCommand); return false;
            }
            chunk6aPositioningEvidence["path"] = chunk6aPath.Capture();
            if (!chunk6aPositioningCommand.IsFinished || !Chunk6aIdle || rider.View.AgentASP.IsReallyMoving) return false;
            var path = chunk6aPath.Capture(); chunk6aPath.Dispose(); chunk6aPath = null;
            var after = CaptureChunk6aCausalState();
            chunk6aPositioningEvidence["after"] = after;
            chunk6aPositioningEvidence["frameAfter"] = Time.frameCount;
            chunk6aPositioningEvidence["gameTicksAfter"] = Game.Instance.TimeController.GameTime.Ticks;
            chunk6aPositioningEvidence["afterInCombat"] = rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat;
            chunk6aPositioningEvidence["createdByPlayer"] = chunk6aPositioningCommand.CreatedByPlayer;
            chunk6aPositioningEvidence["terminalCommand"] = CaptureOrdinaryCommand(chunk6aPositioningCommand);
            chunk6aPositioningEvidence["destination"] = CapturePosition(chunk6aPositioningDestination);
            chunk6aPositioningEvidence["terminalNavigation"] = CaptureChunk6aOriginNavigation();
            chunk6aPositioningEvidence["events"] = allocationTrace.EventsSince(chunk6aPositioningTraceStart);
            chunk6aPositioningEvidence["traceComplete"] = allocationTrace.Complete;
            chunk6aPositioningEvidence["residual"] = HorizontalDistance(rider.Position, chunk6aPositioningDestination);
            var before = chunk6aPositioningEvidence["before"];
            var events = ((JArray)path["events"]).OfType<JObject>().ToArray();
            var pass = chunk6aPositioningCommand.Result == UnitCommand.ResultType.Success && (bool)path["complete"] &&
                events.Any(e => (string)e["boundary"] == "path-request") && events.Any(e => (string)e["boundary"] == "path-complete-after") &&
                events.Any(e => (string)e["boundary"] == "command-ended") &&
                (float)chunk6aPositioningEvidence["residual"] <= MountedCombatSpatialPolicy.DiagnosticPlacementTolerance &&
                JToken.DeepEquals(before["ledger"], after["ledger"]) && JToken.DeepEquals(before["generation"], after["generation"]) &&
                (string)before["relationshipState"] == "Unmounted" && (string)after["relationshipState"] == "Unmounted";
            chunk6aPositioningEvidence["pass"] = pass;
            if (!pass) throw new InvalidOperationException("Pre-combat positioning did not prove exact native arrival, terminal state and unchanged relationship; complete evidence retained.");
            chunk6aPositioningComplete = true; ResetLeafClock(); return true;
        }
    }
}
