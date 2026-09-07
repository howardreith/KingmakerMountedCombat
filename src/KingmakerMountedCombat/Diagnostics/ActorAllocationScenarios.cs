using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsActorAllocationScenario(string scenario) =>
            scenario == "actor-allocation-rider-first-tb" || scenario == "actor-allocation-mount-first-tb" ||
            scenario == "actor-allocation-rider-first-unmounted-tb" || scenario == "actor-allocation-mount-first-unmounted-tb";
        private bool IsActorAllocation => IsActorAllocationScenario(request.Scenario);
        private bool AllocationMounted => !request.Scenario.Contains("unmounted");
        private bool AllocationRiderFirst => request.Scenario.Contains("rider-first");
        private NativeActorAllocationTrace allocationTrace;
        private int allocationStage;
        private int allocationFirstRound;
        private int allocationRiderInitiative;
        private int allocationMountInitiative;
        private bool allocationInitiativeLease;
        private bool allocationDismountRequested;
        private readonly List<UnitEntityData> allocationFixtureParty = new List<UnitEntityData>();
        private TurnController allocationTurn;
        private TurnController allocationEndedTurn;
        private UnitEntityData allocationMover;
        private JObject allocationSample;
        private readonly JArray allocationSamples = new JArray();
        private readonly HashSet<TurnController> allocationVisited = new HashSet<TurnController>();
        private readonly Dictionary<int, List<string>> allocationRoundActors = new Dictionary<int, List<string>>();
        private Vector3 allocationMoveOrigin;
        private int allocationMoveFrame;

        private void BeginActorAllocation()
        {
            if (settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Allocation qualification requires the unchanged separate-turn configuration.");
            allocationTrace = new NativeActorAllocationTrace(rider, horse, combat);
            allocationTrace.BeginEncounter(request.RunId + ":" + request.Scenario);
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private void TickActorAllocation()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            observations["actorAllocationProgress"] = new JObject {
                ["stage"] = allocationStage, ["firstRound"] = allocationFirstRound, ["round"] = controller.RoundNumber,
                ["currentActor"] = turn?.Unit?.UniqueId, ["turnStatus"] = turn?.Status.ToString(),
                ["samples"] = allocationSamples, ["rider"] = allocationTrace.Snapshot(rider), ["mount"] = allocationTrace.Snapshot(horse)
            };
            if (Time.frameCount % 300 == 0)
                logger.Info("Allocation trace progress: stage=" + allocationStage + "; round=" + controller.RoundNumber +
                    "; actor=" + turn?.Unit?.UniqueId + "; status=" + turn?.Status + "; samples=" + allocationSamples.Count +
                    "; mountS=" + horse.CombatState.Cooldown.StandardAction + "; mountM=" + horse.CombatState.Cooldown.MoveAction +
                    "; pending=" + GetPendingNextUnit(controller)?.UniqueId + "; paused=" + game.IsPaused);
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (allocationStage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (!AllocationMounted && relationship.State == RelationshipState.Mounted)
                {
                    if (!allocationDismountRequested) allocationDismountRequested = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "allocation-unmounted-control-setup");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                allocationRiderInitiative = rider.Stats.Initiative.BaseValue;
                allocationMountInitiative = horse.Stats.Initiative.BaseValue;
                allocationInitiativeLease = true;
                for (var index = 0; index < rider.Group.Count; index++)
                {
                    var member = rider.Group[index];
                    if (member.IsInCombat) throw new InvalidOperationException("Allocation fixture requires an idle party before encounter setup.");
                    if (member != rider && member != horse) allocationFixtureParty.Add(member);
                }
                // Deterministic native roll inputs on disposable actors BEFORE
                // encounter setup. Never rewrite a roll, active turn or readiness.
                rider.Stats.Initiative.BaseValue = AllocationRiderFirst ? 40 : -40;
                horse.Stats.Initiative.BaseValue = AllocationRiderFirst ? -40 : 40;
                observations["allocationFixture"] = new JObject {
                    ["mounted"] = AllocationMounted, ["order"] = AllocationRiderFirst ? "rider-first" : "mount-first",
                    ["riderInitiativeBefore"] = allocationRiderInitiative, ["mountInitiativeBefore"] = allocationMountInitiative,
                    ["riderInitiativeInput"] = rider.Stats.Initiative.ModifiedValue, ["mountInitiativeInput"] = horse.Stats.Initiative.ModifiedValue,
                    ["outsideCombat"] = !rider.IsInCombat && !horse.IsInCombat,
                    ["inputKind"] = "scripted-native-handler-integration", ["endTurnInput"] = "Game.PauseBind"
                };
                BeginTarget(6f, "allocation-trace");
                ruleProbe.Arm(target, false);
                allocationStage = 1; ResetLeafClock(); return;
            }
            if (allocationStage == 1)
            {
                if (!IsCombatReady(AllocationMounted)) return;
                if (!CombatController.IsInTurnBasedCombat())
                {
                    if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                    if (!turnBasedModeProbe.TemporaryDeliveryAttempted) turnBasedModeProbe.DispatchTemporaryValueIfRequired();
                    return;
                }
                allocationStage = 2; ResetLeafClock();
            }
            if (allocationStage == 3)
            {
                var timedOut = Time.frameCount > allocationMoveFrame + 240;
                if (movementCommand != null && !movementCommand.IsFinished && !timedOut) return;
                if (timedOut && movementCommand != null && !movementCommand.IsFinished)
                {
                    allocationTrace.Record("stop-input", allocationMover, movementCommand);
                    SelectionManager.Instance.Stop();
                    allocationSample["stopAfterStall"] = true;
                    allocationMoveFrame = Time.frameCount;
                    return;
                }
                allocationSample["after"] = allocationTrace.Snapshot(allocationMover);
                allocationSample["riderAfter"] = allocationTrace.Snapshot(rider);
                allocationSample["mountAfter"] = allocationTrace.Snapshot(horse);
                allocationSample["displacement"] = HorizontalDistance(allocationMoveOrigin, allocationMover.Position);
                allocationSample["commandResult"] = movementCommand?.Result.ToString();
                allocationSample["sameTurn"] = ReferenceEquals(turn, allocationTurn);
                allocationTrace.Record("move-complete", allocationMover, movementCommand);
                allocationStage = 4; ResetLeafClock(); return;
            }
            if (allocationStage == 4)
            {
                if (ReferenceEquals(turn, allocationTurn)) { EndAllocationNativeTurn(turn); return; }
                allocationStage = 2; ResetLeafClock();
            }
            if (turn == null || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
            if (allocationFirstRound > 0 && controller.RoundNumber >= allocationFirstRound + 3)
            {
                var order = AllocationRiderFirst ? new[] { rider.UniqueId, horse.UniqueId } : new[] { horse.UniqueId, rider.UniqueId };
                var complete = Enumerable.Range(allocationFirstRound, 3).All(round =>
                    allocationRoundActors.ContainsKey(round) && allocationRoundActors[round].SequenceEqual(order));
                var trace = allocationTrace.Capture();
                complete &= (int)trace["dropped"] == 0 && (int)trace["observationErrors"] == 0;
                AddRow("T01-native-allocation-trace", complete,
                    "Three complete native rounds with deterministic pre-encounter order. This row qualifies trace coverage, not A01-A09 gameplay.",
                    new JObject { ["level"] = "NATIVE INTEGRATION", ["gameplayQualified"] = false,
                        ["firstRound"] = allocationFirstRound, ["endRound"] = controller.RoundNumber,
                        ["order"] = new JArray(order), ["samples"] = allocationSamples,
                        ["rounds"] = new JObject(allocationRoundActors.Select(entry => new JProperty(entry.Key.ToString(), new JArray(entry.Value)))) });
                BeginCleanup(); return;
            }
            if (turn.Unit != rider && turn.Unit != horse || controller.RoundNumber == 0)
            { EndAllocationNativeTurn(turn); return; }
            if (allocationVisited.Contains(turn)) { EndAllocationNativeTurn(turn); return; }
            if (!rider.Commands.Empty || !horse.Commands.Empty || turn.Unit.AreHandsBusyWithAnimation ||
                game.HandsEquipmentController.IsUpdateScheduledFor(turn.Unit)) return;
            if (allocationFirstRound == 0) allocationFirstRound = controller.RoundNumber;
            allocationVisited.Add(turn);
            if (!allocationRoundActors.ContainsKey(controller.RoundNumber)) allocationRoundActors[controller.RoundNumber] = new List<string>();
            allocationRoundActors[controller.RoundNumber].Add(turn.Unit.UniqueId);
            allocationTurn = turn;
            allocationMover = AllocationMounted ? horse : turn.Unit;
            allocationMoveOrigin = allocationMover.Position;
            SelectionManager.Instance.SelectUnit(turn.Unit.View, true, true, false);
            allocationSample = new JObject {
                ["round"] = controller.RoundNumber, ["surface"] = turn.Unit == rider ? "rider" : "mount",
                ["before"] = allocationTrace.Snapshot(allocationMover), ["riderBefore"] = allocationTrace.Snapshot(rider),
                ["mountBefore"] = allocationTrace.Snapshot(horse), ["selected"] = new JArray(SelectionManager.Instance.SelectedUnits.Select(unit => unit.UniqueId))
            };
            allocationSamples.Add(allocationSample);
            var direction = (target.Position - allocationMover.Position).normalized * (allocationSamples.Count % 2 == 0 ? -1f : 1f);
            // Reuse the qualified native short-move endpoint. The navigation
            // helper rejects sub-0.25m searches; the native command owns pathing.
            var destination = allocationMover.Position + direction * 0.75f;
            allocationSample["destination"] = new JArray(destination.x, destination.y, destination.z);
            allocationTrace.Record("move-input-before", allocationMover);
            using (var input = new NativeOrdinaryAttackInput(destination))
            {
                input.Predict();
                var cycles = 0;
                while (turn.EnabledFiveFootStep && cycles++ < 4) { input.Click(button: 1); input.Predict(); }
                allocationSample["cursorCycles"] = cycles;
                allocationSample["clicked"] = input.Click();
            }
            movementCommand = allocationMover.Commands.Move as UnitMoveTo;
            allocationTrace.Record("move-input-after", allocationMover, movementCommand);
            allocationSample["admitted"] = movementCommand?.Executor == allocationMover;
            allocationMoveFrame = Time.frameCount;
            allocationStage = 3; ResetLeafClock();
        }

        private void EndAllocationNativeTurn(TurnController turn)
        {
            var unit = turn?.Unit;
            if (unit == null || ReferenceEquals(turn, allocationEndedTurn) || !unit.IsDirectlyControllable || !unit.Commands.Empty ||
                unit.AreHandsBusyWithAnimation || !turn.CanEndTurnAndNoActing() || Game.Instance.TurnBasedCombatController.WaitingForUI ||
                GetPendingNextUnit(Game.Instance.TurnBasedCombatController) != null || combat.HasActiveCommand || combat.HasActiveGroundMovement) return;
            if (unit != rider && unit != horse && !(targetService.NonPairPartyAiLease.OwnsExactMember(unit) && targetService.NonPairPartyAiLease.ValidateActive()))
                throw new InvalidOperationException("Allocation fixture refused End Turn on a foreign actor.");
            allocationTrace.Record("end-turn-input-before", unit);
            Game.Instance.PauseBind();
            allocationTrace.Record("end-turn-input-after", unit);
            allocationEndedTurn = turn;
            ResetLeafClock();
        }

        private void CleanupActorAllocation()
        {
            observations["actorAllocationSamples"] = allocationSamples.DeepClone();
            // Native encounter cleanup on the exact disposable party captured
            // idle before setup. This runs only AFTER measured turns, and avoids
            // restoring TB while an unrelated fixture member remains in combat.
            foreach (var member in allocationFixtureParty)
            {
                if (member.IsInState && member.Group != rider.Group)
                    throw new InvalidOperationException("Allocation cleanup party identity changed.");
                TryLeaveCombat(member);
            }
            observations["allocationPartyCombatRestored"] = allocationFixtureParty.All(member => !member.IsInState || !member.IsInCombat);
            observations["allocationPartyCleanupIds"] = new JArray(allocationFixtureParty.Select(member => member.UniqueId));
            if (allocationInitiativeLease)
            {
                rider.Stats.Initiative.BaseValue = allocationRiderInitiative;
                horse.Stats.Initiative.BaseValue = allocationMountInitiative;
                allocationInitiativeLease = false;
                observations["allocationInitiativeRestored"] = rider.Stats.Initiative.BaseValue == allocationRiderInitiative && horse.Stats.Initiative.BaseValue == allocationMountInitiative;
            }
            if (allocationTrace != null)
            {
                observations["actorAllocationTrace"] = allocationTrace.Capture();
                allocationTrace.Dispose(); allocationTrace = null;
            }
        }
    }
}
