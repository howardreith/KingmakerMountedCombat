using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool IsChunk4GroundArrival => request.Scenario == "chunk4-ground-arrival-rt";
        // Observed DU47 route in the disposable Working area. These are command
        // destinations only; neither entity nor view positions are assigned.
        private static readonly Vector3 Chunk4GroundOrigin = new Vector3(-4.185761f, 6.058932f, 60.0466156f);
        private static readonly Vector3 Chunk4GroundDestination = new Vector3(-3.23257446f, 6.52700043f, 62.35777f);
        private const string Chunk4GroundArea = "9d1278a2f599b2a4daab53abdfe88d2e";
        private int chunk4GroundStage;
        private int chunk4GroundCase;
        private bool chunk4GroundControlSent;
        private UnitMoveTo chunk4GroundMove;
        private JObject chunk4GroundEvidence;
        private JObject chunk4GroundInputEvidence;
        private Vector3 chunk4GroundStart;
        private Vector3 chunk4GroundMountedStart;
        private bool Chunk4GroundMounted => chunk4GroundCase == 0;
        private string Chunk4GroundId => Chunk4GroundMounted ? "C4-GROUND-mounted-arrival" : "C4-GROUND-unmounted-arrival";

        private void BeginChunk4GroundArrival()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent ||
                Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe != Chunk4GroundArea)
                throw new InvalidOperationException("Ground comparison requires the exact paired configuration and disposable Working area.");
            CaptureIdleFixturePartyForCleanup();
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString());
            step = Phase3dHorseStep.Phase3gControls; ResetLeafClock();
        }

        private UnitMoveTo BeginChunk4GroundInput(UnitEntityData selected, UnitEntityData executor, Vector3 destination, string traceCase)
        {
            Game.Instance.SelectedAbilityHandler.SetAbility(null);
            SelectionManager.Instance.SelectUnit(selected.View, true, true, false);
            ordinaryAttackTrace.BeginCase(traceCase);
            using (var input = new NativeOrdinaryAttackInput(destination))
            {
                var before = CaptureOrdinaryLiveState(); input.Predict(); input.Predict();
                var after = CaptureOrdinaryLiveState();
                if (!JToken.DeepEquals(before, after))
                    throw new InvalidOperationException("Ground comparison prediction changed live state.");
                if (!input.Click()) throw new InvalidOperationException("Ground comparison native point input refused.");
                chunk4GroundInputEvidence = new JObject { ["beforePrediction"] = before, ["afterPrediction"] = after,
                    ["clicked"] = true, ["selected"] = selected.UniqueId, ["destination"] = CapturePosition(destination) };
            }
            var command = executor.Commands.Move as UnitMoveTo;
            if (command == null || command.GetType() != typeof(UnitMoveTo) || command.Executor != executor || !command.CreatedByPlayer)
                throw new InvalidOperationException("Ground comparison lost its exact native command owner.");
            return command;
        }

        private void TickChunk4GroundArrival()
        {
            var game = Game.Instance;
            observations["chunk4GroundProgress"] = new JObject { ["stage"] = chunk4GroundStage, ["case"] = Chunk4GroundId,
                ["live"] = CaptureOrdinaryLiveState(), ["command"] = CaptureOrdinaryCommand(chunk4GroundMove),
                ["current"] = chunk4GroundEvidence };
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (game.CurrentlyLoadedArea?.AssetGuidThreadSafe != Chunk4GroundArea)
                throw new InvalidOperationException("Ground comparison left its exact disposable area.");
            if (chunk4GroundStage == 0)
            {
                if (!Chunk4PairedPlayIdle) return;
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (game.Player.IsInCombat || rider.IsInCombat || horse.IsInCombat)
                        throw new InvalidOperationException("Ground comparison must mount before combat.");
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    if (!chunk4GroundControlSent) chunk4GroundControlSent = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "ground-comparison-mount");
                    return;
                }
                if (!PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                var enemyPoint = FindWalkablePoint(rider.Position, 9f, 0.5f, point =>
                    HorizontalDistance(point, Chunk4GroundOrigin) > 6f && HorizontalDistance(point, Chunk4GroundDestination) > 6f);
                BeginTarget(9f, "ground-comparison", enemyPoint); ruleProbe.Arm(target, false);
                chunk4GroundStage = 1; ResetLeafClock(); return;
            }
            if (chunk4GroundStage == 1)
            {
                if (!IsCombatReady(Chunk4GroundMounted) || CombatController.IsInTurnBasedCombat() || !Chunk4PairedPlayIdle ||
                    !rider.CombatState.CanActInCombat || !horse.CombatState.CanActInCombat) return;
                chunk4GroundMove = BeginChunk4GroundInput(Chunk4GroundMounted ? rider : horse, horse, Chunk4GroundOrigin, Chunk4GroundId + "-positioning");
                chunk4GroundStage = 2; ResetLeafClock(); return;
            }
            if (chunk4GroundStage == 2)
            {
                if (!chunk4GroundMove.IsFinished || !Chunk4PairedPlayIdle || !rider.CombatState.CanActInCombat ||
                    !horse.CombatState.CanActInCombat) return;
                var setup = new JObject { ["command"] = CaptureOrdinaryCommand(chunk4GroundMove), ["actual"] = CapturePosition(horse.Position),
                    ["requested"] = CapturePosition(Chunk4GroundOrigin), ["residual"] = HorizontalDistance(horse.Position, Chunk4GroundOrigin),
                    ["trace"] = ordinaryAttackTrace.CaptureCaseEvents(Chunk4GroundId + "-positioning") };
                observations[Chunk4GroundId + "-positioning"] = setup;
                if (chunk4GroundMove.Result != UnitCommand.ResultType.Success ||
                    (float)setup["residual"] > MountedCombatSpatialPolicy.DiagnosticPlacementTolerance)
                    throw new InvalidOperationException("Ground comparison could not reach its matched origin through native movement.");
                chunk4GroundStart = horse.Position;
                if (Chunk4GroundMounted) chunk4GroundMountedStart = chunk4GroundStart;
                var match = HorizontalDistance(chunk4GroundStart, chunk4GroundMountedStart);
                if (match > MountedCombatSpatialPolicy.DiagnosticPlacementTolerance)
                    throw new InvalidOperationException("Mounted and unmounted ground origins do not match within the existing placement tolerance.");
                chunk4GroundEvidence = new JObject { ["level"] = "NATIVE INTEGRATION", ["caseId"] = Chunk4GroundId,
                    ["mode"] = "RT", ["mounted"] = Chunk4GroundMounted, ["area"] = Chunk4GroundArea,
                    ["setup"] = setup,
                    ["inputKind"] = "native-ordinary-pointer-ground", ["before"] = CaptureOrdinaryLiveState(),
                    ["origin"] = CapturePosition(chunk4GroundStart), ["destination"] = CapturePosition(Chunk4GroundDestination),
                    ["originMatchDistance"] = match, ["originTolerance"] = MountedCombatSpatialPolicy.DiagnosticPlacementTolerance,
                    ["originFootprint"] = NativeGroundMovementObservation.CaptureFootprint(horse, chunk4GroundStart),
                    ["destinationFootprint"] = NativeGroundMovementObservation.CaptureFootprint(horse, Chunk4GroundDestination),
                    ["riderCanAct"] = rider.CombatState.CanActInCombat, ["mountCanAct"] = horse.CombatState.CanActInCombat,
                    ["selected"] = (Chunk4GroundMounted ? rider : horse).UniqueId, ["samples"] = new JArray() };
                observations[Chunk4GroundId] = chunk4GroundEvidence;
                chunk4GroundMove = BeginChunk4GroundInput(Chunk4GroundMounted ? rider : horse, horse, Chunk4GroundDestination, Chunk4GroundId);
                chunk4GroundEvidence["input"] = chunk4GroundInputEvidence;
                chunk4GroundStage = 3; ResetLeafClock(); return;
            }
            if (chunk4GroundStage == 3)
            {
                var samples = (JArray)chunk4GroundEvidence["samples"];
                if (samples.Count >= 256) throw new InvalidOperationException("Ground comparison exceeded its bounded observation capacity.");
                samples.Add(new JObject { ["frame"] = Time.frameCount, ["gameSeconds"] = game.TimeController.GameTime.TotalSeconds,
                    ["movement"] = NativeGroundMovementObservation.Capture(horse, chunk4GroundMove) });
                if (!chunk4GroundMove.IsFinished || !Chunk4PairedPlayIdle) return;
                var after = CaptureOrdinaryLiveState();
                chunk4GroundEvidence["after"] = after; chunk4GroundEvidence["command"] = CaptureOrdinaryCommand(chunk4GroundMove);
                chunk4GroundEvidence["travel"] = HorizontalDistance(horse.Position, chunk4GroundStart);
                chunk4GroundEvidence["endpointDistance"] = HorizontalDistance(horse.Position, Chunk4GroundDestination);
                chunk4GroundEvidence["approachRadius"] = chunk4GroundMove.ApproachRadius;
                chunk4GroundEvidence["createdByPlayer"] = chunk4GroundMove.CreatedByPlayer;
                chunk4GroundEvidence["nativeTrace"] = ordinaryAttackTrace.CaptureCaseEvents(Chunk4GroundId);
                chunk4GroundEvidence["forcedD20"] = ruleProbe.PairForcedD20Count;
                var success = chunk4GroundMove.Result == UnitCommand.ResultType.Success && chunk4GroundMove.IsStarted && chunk4GroundMove.IsActed &&
                    (float)chunk4GroundEvidence["travel"] >= MountedCombatSpatialPolicy.MinimumDiagnosticApproachDisplacement &&
                    (float)chunk4GroundEvidence["endpointDistance"] <= chunk4GroundMove.ApproachRadius && ruleProbe.PairForcedD20Count == 0 &&
                    (!Chunk4GroundMounted || (float)after["rider"]["move"] <= (float)chunk4GroundEvidence["before"]["rider"]["move"]);
                AddRow(Chunk4GroundId, success, "Native ground result=" + chunk4GroundMove.Result + "; failed movement remains FAIL while the independent control is collected.", chunk4GroundEvidence);
                if (!Chunk4GroundMounted) { observations["ordinaryTrace"] = ordinaryAttackTrace.Capture(); BeginCleanup(); return; }
                chunk4GroundControlSent = false; chunk4GroundStage = 4; ResetLeafClock(); return;
            }
            if (chunk4GroundStage == 4)
            {
                if (!Chunk4PairedPlayIdle) return;
                if (relationship.State != RelationshipState.Unmounted)
                {
                    SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                    if (!chunk4GroundControlSent) chunk4GroundControlSent = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "ground-comparison-dismount");
                    return;
                }
                // The native rider is a real obstacle after Dismount. Move it
                // away through ordinary controls before measuring the Horse.
                var away = FindChunk4GroundRiderClearance();
                chunk4GroundMove = BeginChunk4GroundInput(rider, rider, away, "ground-comparison-rider-clearance");
                chunk4GroundStage = 5; ResetLeafClock(); return;
            }
            if (chunk4GroundStage == 5)
            {
                if (!chunk4GroundMove.IsFinished || !Chunk4PairedPlayIdle) return;
                observations["groundComparisonRiderClearance"] = new JObject { ["command"] = CaptureOrdinaryCommand(chunk4GroundMove),
                    ["originDistance"] = HorizontalDistance(rider.Position, Chunk4GroundOrigin),
                    ["destinationDistance"] = HorizontalDistance(rider.Position, Chunk4GroundDestination),
                    ["targetDistance"] = HorizontalDistance(rider.Position, target.Position) };
                if (chunk4GroundMove.Result != UnitCommand.ResultType.Success || HorizontalDistance(rider.Position, Chunk4GroundOrigin) <= 6f ||
                    HorizontalDistance(rider.Position, Chunk4GroundDestination) <= 6f || HorizontalDistance(rider.Position, target.Position) <= 3f)
                    throw new InvalidOperationException("Ground comparison did not clear the real dismounted rider through native movement.");
                chunk4GroundCase = 1; chunk4GroundEvidence = null; chunk4GroundStage = 1; ResetLeafClock();
            }
        }

        private Vector3 FindChunk4GroundRiderClearance()
        {
            if (global::AstarPath.active == null) throw new InvalidOperationException("Ground comparison requires the native navigation graph.");
            var candidates = new JArray();
            observations["groundRiderClearanceSearch"] = new JObject { ["rider"] = CapturePosition(rider.Position),
                ["target"] = CapturePosition(target.Position), ["routeOrigin"] = CapturePosition(Chunk4GroundOrigin),
                ["routeDestination"] = CapturePosition(Chunk4GroundDestination), ["candidates"] = candidates };
            var direction = horse.View.transform.forward; direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            direction.Normalize();
            // EY exhausted one 7 m ring after actual Dismount. Search three
            // bounded rings with the same native projection/distance tolerance;
            // preserve the actual 6 m clearance and full actor footprint.
            foreach (var radius in new[] { 7f, 9f, 11f })
            {
                for (var index = 0; index < 32; index++)
                {
                    var nearest = global::AstarPath.active.GetNearest(rider.Position +
                        Quaternion.Euler(0f, index * 11.25f, 0f) * direction * radius);
                    var point = nearest.clampedPosition;
                    var distance = HorizontalDistance(rider.Position, point);
                    var originDistance = HorizontalDistance(point, Chunk4GroundOrigin);
                    var destinationDistance = HorizontalDistance(point, Chunk4GroundDestination);
                    var targetDistance = HorizontalDistance(point, target.Position);
                    var walkable = nearest.node != null && nearest.node.Walkable;
                    var eligible = walkable && distance >= 0.25f && Math.Abs(distance - radius) <= 0.5f &&
                        originDistance > 6.4f && destinationDistance > 6.4f && targetDistance > 3.4f;
                    var blockers = Game.Instance.State.Units.Where(unit => unit != rider && unit.IsInState && unit.View != null &&
                        HorizontalDistance(point, unit.Position) < rider.View.Corpulence + unit.View.Corpulence + 0.05f)
                        .Select(unit => unit.UniqueId).ToArray();
                    var footprint = eligible && blockers.Length == 0 ? NativeGroundMovementObservation.CaptureFootprint(rider, point) : null;
                    eligible &= footprint != null && ((JArray)footprint["probes"]).All(probe => (float)probe["residual"] < 0.001f);
                    candidates.Add(new JObject { ["radius"] = radius, ["directionIndex"] = index, ["point"] = CapturePosition(point),
                        ["walkable"] = walkable, ["distance"] = distance, ["originDistance"] = originDistance,
                        ["destinationDistance"] = destinationDistance, ["targetDistance"] = targetDistance,
                        ["blockers"] = new JArray(blockers), ["footprint"] = footprint, ["eligible"] = eligible });
                    if (eligible) return point;
                }
            }
            throw new InvalidOperationException("No native rider-clearance endpoint exists on the three bounded comparison rings.");
        }
    }
}
