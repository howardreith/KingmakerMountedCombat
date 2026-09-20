using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool allocationProbeStarted;
        private int allocationProbeFirstRound;
        private int allocationProbeStage;
        private int allocationProbeFrame;
        private int allocationProbeStartedFrame;
        private TurnController allocationProbeTurn;
        private UnitEntityData allocationProbeMover;
        private UnitMoveTo allocationProbeMove;
        private Vector3 allocationProbeOrigin;
        private JObject allocationProbeSample;
        private JObject allocationProbeAttempt;
        private readonly JArray allocationProbeSamples = new JArray();
        private readonly HashSet<TurnController> allocationProbeVisited = new HashSet<TurnController>();

        private void BeginAllocationConservationProbe()
        {
            allocationProbeStarted = true;
            allocationProbeFirstRound = Game.Instance.TurnBasedCombatController.RoundNumber;
            allocationProbeStartedFrame = Time.frameCount;
            observations["allocationConservationProbe"] = new JObject {
                ["id"] = "T02-native-exhaustion-refresh-trace", ["firstRound"] = allocationProbeFirstRound,
                ["level"] = "NATIVE INTEGRATION",
                ["samples"] = allocationProbeSamples, ["gameplayQualified"] = false,
                ["inputKind"] = "scripted-native-handler-integration", ["maximumRequestsPerTurn"] = 12
            };
            ResetLeafClock();
        }

        private void TickAllocationConservationProbe()
        {
            var controller = Game.Instance.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            observations["allocationConservationProbe"]["stage"] = allocationProbeStage;
            observations["allocationConservationProbe"]["round"] = controller.RoundNumber;
            if (Time.frameCount > allocationProbeStartedFrame + 12000)
                throw new InvalidOperationException("Bounded native allocation conservation trace exceeded its frame deadline.");
            if (allocationProbeStage == 1)
            {
                if (allocationProbeMove != null && !allocationProbeMove.IsFinished && ReferenceEquals(turn, allocationProbeTurn))
                {
                    if (Time.frameCount <= allocationProbeFrame + 240) return;
                    allocationTrace.Record("conservation-stop-input", allocationProbeMover, allocationProbeMove);
                    SelectionManager.Instance.Stop();
                    allocationProbeAttempt["stoppedAfterStall"] = true;
                    allocationProbeFrame = Time.frameCount;
                    return;
                }
                allocationProbeAttempt["after"] = allocationTrace.Snapshot(allocationProbeMover);
                allocationProbeAttempt["riderAfter"] = allocationTrace.Snapshot(rider);
                allocationProbeAttempt["mountAfter"] = allocationTrace.Snapshot(horse);
                allocationProbeAttempt["distance"] = HorizontalDistance(allocationProbeOrigin, allocationProbeMover.Position);
                allocationProbeAttempt["result"] = allocationProbeMove?.Result.ToString();
                allocationProbeAttempt["finished"] = allocationProbeMove?.IsFinished;
                allocationProbeAttempt["started"] = allocationProbeMove?.IsStarted;
                allocationProbeAttempt["sameNativeTurn"] = ReferenceEquals(turn, allocationProbeTurn);
                allocationTrace.Record("conservation-command-complete", allocationProbeMover, allocationProbeMove);
                var attempts = (JArray)allocationProbeSample["attempts"];
                var cannotContinue = !ReferenceEquals(turn, allocationProbeTurn) ||
                    (float)allocationProbeAttempt["distance"] < 0.02f ||
                    (bool?)allocationProbeAttempt["stoppedAfterStall"] == true;
                if (cannotContinue || attempts.Count == 12)
                {
                    allocationProbeSample["after"] = allocationTrace.Snapshot(allocationProbeMover);
                    allocationProbeSample["completedByNativeTurnChange"] = !ReferenceEquals(turn, allocationProbeTurn);
                    allocationProbeStage = 2;
                }
                else
                {
                    BeginAllocationConservationMove(turn);
                    return;
                }
            }
            if (allocationProbeStage == 2)
            {
                if (ReferenceEquals(turn, allocationProbeTurn)) { EndAllocationNativeTurn(turn); return; }
                allocationProbeStage = 0;
            }
            if (turn == null || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
            if (controller.RoundNumber >= allocationProbeFirstRound + 2)
            {
                var expected = AllocationRiderFirst ? new[] { rider.UniqueId, horse.UniqueId } : new[] { horse.UniqueId, rider.UniqueId };
                var complete = Enumerable.Range(allocationProbeFirstRound, 2).All(round =>
                    allocationProbeSamples.Cast<JObject>().Where(sample => (int)sample["round"] == round)
                        .Select(sample => (string)sample["actor"]).SequenceEqual(expected));
                observations["allocationConservationProbe"]["endRound"] = controller.RoundNumber;
                observations["allocationConservationProbe"]["coverageComplete"] = complete;
                AddRow("T02-native-exhaustion-refresh-trace", complete,
                    "Two further native rounds of repeated bounded native requests. Measures exhaustion and later readiness; does not declare an early grant legal.",
                    (JObject)observations["allocationConservationProbe"].DeepClone());
                BeginCleanup(); return;
            }
            if (turn.Unit != rider && turn.Unit != horse) { EndAllocationNativeTurn(turn); return; }
            if (allocationProbeVisited.Contains(turn)) { EndAllocationNativeTurn(turn); return; }
            if (!rider.Commands.Empty || !horse.Commands.Empty || turn.Unit.AreHandsBusyWithAnimation ||
                Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(turn.Unit)) return;
            allocationProbeVisited.Add(turn);
            allocationProbeTurn = turn;
            allocationProbeMover = AllocationMounted ? horse : turn.Unit;
            allocationProbeSample = new JObject {
                ["round"] = controller.RoundNumber, ["actor"] = turn.Unit.UniqueId,
                ["surface"] = turn.Unit == rider ? "rider" : "mount", ["mover"] = allocationProbeMover.UniqueId,
                ["before"] = allocationTrace.Snapshot(allocationProbeMover), ["attempts"] = new JArray()
            };
            allocationProbeSamples.Add(allocationProbeSample);
            BeginAllocationConservationMove(turn);
        }

        private void BeginAllocationConservationMove(TurnController turn)
        {
            SelectionManager.Instance.SelectUnit(turn.Unit.View, true, true, false);
            var attempts = (JArray)allocationProbeSample["attempts"];
            allocationProbeOrigin = allocationProbeMover.Position;
            var direction = (target.Position - allocationProbeMover.Position).normalized * (attempts.Count % 2 == 0 ? 1f : -1f);
            var destination = allocationProbeOrigin + direction * 3f;
            allocationProbeAttempt = new JObject {
                ["index"] = attempts.Count, ["before"] = allocationTrace.Snapshot(allocationProbeMover),
                ["riderBefore"] = allocationTrace.Snapshot(rider), ["mountBefore"] = allocationTrace.Snapshot(horse),
                ["destination"] = new JArray(destination.x, destination.y, destination.z)
            };
            attempts.Add(allocationProbeAttempt);
            allocationTrace.Record("conservation-input-before", allocationProbeMover);
            using (var input = new NativeOrdinaryAttackInput(destination))
            {
                input.Predict();
                var cycles = 0;
                while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycles++ < 4)
                { input.Click(button: 1); input.Predict(); }
                allocationProbeAttempt["cursorCycles"] = cycles;
                allocationProbeAttempt["fiveFootStep"] = turn.EnabledFiveFootStep;
                allocationProbeAttempt["singleActionMove"] = turn.EnabledSingleActionMove;
                allocationProbeAttempt["clicked"] = input.Click();
            }
            allocationProbeMove = allocationProbeMover.Commands.Move as UnitMoveTo;
            allocationProbeAttempt["admitted"] = allocationProbeMove?.Executor == allocationProbeMover;
            allocationProbeAttempt["feedback"] = combat.LastFeedback;
            allocationTrace.Record("conservation-input-after", allocationProbeMover, allocationProbeMove);
            allocationProbeFrame = Time.frameCount;
            allocationProbeStage = 1;
        }
    }
}
