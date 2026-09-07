using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool IsPairedAllocation => IsActorAllocation && AllocationMounted;
        private int pairedStage;
        private int pairedActivationNumber;
        private int pairedMoveNumber;
        private bool pairedAttackMount;
        private MountedPairAttackOutcome pairedPriorOutcome;
        private readonly JArray pairedActivations = new JArray();
        private readonly JArray pairedTurnVisits = new JArray();
        private readonly HashSet<TurnController> pairedVisitedTurns = new HashSet<TurnController>();
        private JObject pairedSample;
        private JObject pairedOperation;
        private readonly List<string> pairedGateErrors = new List<string>();

        private void TickPairedActivationGate()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            observations["pairedActivationGate"] = new JObject {
                ["stage"] = pairedStage, ["activation"] = combat.PairedActivationIdentity,
                ["measuredActivations"] = pairedActivations, ["turnVisits"] = pairedTurnVisits,
                ["errors"] = new JArray(pairedGateErrors)
            };
            if (turn != null && pairedVisitedTurns.Add(turn))
            {
                pairedTurnVisits.Add(new JObject { ["actor"] = turn.Unit.UniqueId, ["round"] = controller.RoundNumber,
                    ["friendly"] = turn.Unit.Group == rider.Group, ["principal"] = turn.Unit == rider,
                    ["mount"] = turn.Unit == horse });
                allocationTrace.Record("turn-observed", turn.Unit);
                if (turn.Unit == horse) throw new InvalidOperationException("Paired mount received an independent native turn.");
            }
            if (pairedStage == 1) { FinishPairedMove(); return; }
            if (pairedStage == 2) { FinishPairedAttack(); return; }
            if (pairedStage == 3)
            {
                if (ReferenceEquals(turn, allocationTurn)) { EndAllocationNativeTurn(turn); return; }
                pairedStage = 0;
            }
            if (turn == null || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
            if (turn.Unit != rider)
            {
                if (TickPairedReactionProbe(turn)) return;
                EndAllocationNativeTurn(turn); return;
            }
            if (!rider.Commands.Empty || !horse.Commands.Empty || combat.HasActiveCommand || combat.HasActiveGroundMovement ||
                rider.AreHandsBusyWithAnimation || horse.AreHandsBusyWithAnimation ||
                game.HandsEquipmentController.IsUpdateScheduledFor(rider) || game.HandsEquipmentController.IsUpdateScheduledFor(horse)) return;
            if (ReferenceEquals(allocationTurn, turn)) return;
            if (combat.PairedActivationSequence < 1 || combat.PairedPartnerContext == null)
                throw new InvalidOperationException("Native rider turn has no paired grant and preparation.");
            allocationTurn = turn;
            var riderBefore = allocationTrace.Snapshot(rider);
            var mountBefore = allocationTrace.Snapshot(horse);
            RequirePaired((float)riderBefore["standard"] == 0f && (float)riderBefore["move"] == 0f &&
                (float)mountBefore["standard"] == 0f && (float)mountBefore["move"] == 0f,
                "Fresh native paired boundary must renew both actors without old expenditure.");
            if (pairedActivationNumber == 1) VerifyPairedReactionRefresh(mountBefore);
            if (pairedActivationNumber == 3)
            {
                observations["pairedRefreshAfterEarlyEnd"] = new JObject {
                    ["identity"] = combat.PairedActivationIdentity, ["rider"] = riderBefore, ["mount"] = mountBefore };
                CompletePairedGate(); return;
            }
            pairedActivationNumber++;
            if (allocationFirstRound == 0) allocationFirstRound = controller.RoundNumber;
            pairedMoveNumber = 0;
            pairedSample = new JObject { ["index"] = pairedActivationNumber,
                ["identity"] = combat.PairedActivationIdentity, ["round"] = controller.RoundNumber,
                ["riderBefore"] = riderBefore, ["mountBefore"] = mountBefore, ["operations"] = new JArray() };
            pairedActivations.Add(pairedSample);
            BeginPairedMove(0.75f, "partial");
        }

        private void BeginPairedMove(float distance, string purpose)
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            allocationMoveOrigin = horse.Position;
            var direction = (target.Position - horse.Position).normalized;
            if (pairedActivationNumber == 2 && pairedMoveNumber % 2 == 0) direction = -direction;
            var destination = horse.Position + direction * distance;
            pairedOperation = new JObject { ["kind"] = "movement", ["purpose"] = purpose,
                ["requestedDistance"] = distance, ["before"] = allocationTrace.Snapshot(horse),
                ["riderBefore"] = allocationTrace.Snapshot(rider) };
            ((JArray)pairedSample["operations"]).Add(pairedOperation);
            allocationTrace.Record("paired-move-input-before", horse);
            using (var input = new NativeOrdinaryAttackInput(destination))
            {
                input.Predict();
                var cycles = 0;
                while ((allocationTurn.EnabledFiveFootStep || allocationTurn.EnabledSingleActionMove) && cycles++ < 4)
                { input.Click(button: 1); input.Predict(); }
                pairedOperation["clicked"] = input.Click();
                pairedOperation["fiveFootStep"] = allocationTurn.EnabledFiveFootStep;
                pairedOperation["singleMove"] = allocationTurn.EnabledSingleActionMove;
            }
            movementCommand = horse.Commands.Move as UnitMoveTo;
            pairedOperation["admitted"] = movementCommand != null;
            pairedOperation["feedback"] = combat.LastFeedback;
            allocationTrace.Record("paired-move-input-after", horse, movementCommand);
            allocationMoveFrame = Time.frameCount; pairedStage = 1; ResetLeafClock();
        }

        private void FinishPairedMove()
        {
            if (movementCommand != null && !movementCommand.IsFinished)
            {
                if (Time.frameCount <= allocationMoveFrame + 240) return;
                allocationTrace.Record("paired-stall-stop-input", horse, movementCommand);
                pairedOperation["stalledBeforeStop"] = allocationTrace.Snapshot(horse);
                SelectionManager.Instance.Stop();
                RequirePaired(false, "Paired movement stalled beyond the native fixture deadline.");
            }
            if (combat.HasActiveGroundMovement || combat.HasActiveCommand || !horse.Commands.Empty) return;
            var after = allocationTrace.Snapshot(horse);
            var riderAfter = allocationTrace.Snapshot(rider);
            RequirePaired((string)after["actorContextStatus"] == "Acting",
                "The native partner context did not enter Acting with the principal.");
            var distance = HorizontalDistance(allocationMoveOrigin, horse.Position);
            var before = pairedOperation["before"];
            var cost = (float)after["move"] - (float)before["move"];
            var elapsed = (float)after["measuredAllowedTime"] - (float)before["measuredAllowedTime"];
            var travelled = (float)after["measuredTravelDistance"] - (float)before["measuredTravelDistance"];
            var nativeShift = (float)after["measuredNativeShiftDistance"] - (float)before["measuredNativeShiftDistance"];
            pairedOperation["after"] = after; pairedOperation["riderAfter"] = riderAfter;
            pairedOperation["distance"] = distance; pairedOperation["nativeMoveCost"] = cost;
            pairedOperation["nativeAllowedTime"] = elapsed; pairedOperation["result"] = movementCommand?.Result.ToString();
            pairedOperation["travelledDistance"] = travelled; pairedOperation["nativeShiftDistance"] = nativeShift;
            pairedOperation["samePrincipalTurn"] = ReferenceEquals(allocationTurn, Game.Instance.TurnBasedCombatController.CurrentTurn);
            var reject = (string)pairedOperation["purpose"] == "exhausted-rejection";
            RequirePaired((bool)pairedOperation["samePrincipalTurn"], "Movement changed the activation principal.");
            RequirePaired(Math.Abs((float)riderAfter["move"] - (float)pairedOperation["riderBefore"]["move"]) < 0.0001f,
                "Transport charged rider movement.");
            if (reject)
            {
                RequirePaired(distance < 0.02f && travelled < 0.02f && nativeShift < 0.02f && Math.Abs(cost) < 0.001f && Math.Abs(elapsed) < 0.001f,
                    "Exhausted movement delivered distance or refunded resources.");
                pairedSample["exhaustedMovementRejected"] = true;
                pairedStage = 3; return;
            }
            RequirePaired(movementCommand != null && distance > 0.02f && cost > 0f && Math.Abs(cost - elapsed) < 0.02f,
                "Admitted movement lacks native cost/time/distance.");
            // Turning interpolates the native direction vector; speed times
            // elapsed time is an upper bound, not endpoint displacement. The
            // physical observer measures each actual native Move segment.
            RequirePaired(travelled >= distance - 0.02f && Math.Abs(travelled - nativeShift) < 0.35f &&
                nativeShift <= elapsed * (float)before["speedMps"] + 0.35f,
                "Actual travel differs from native displacement or exceeds allowed movement time.");
            pairedMoveNumber++;
            if (pairedActivationNumber == 1) { BeginPairedAttack(false); return; }
            if (pairedActivationNumber == 3)
            { pairedSample["earlyEnd"] = true; pairedStage = 3; return; }
            if ((float)after["move"] < 5.99f)
            {
                if (pairedMoveNumber >= 14) throw new InvalidOperationException("Conversion requests did not reach the native movement limit.");
                BeginPairedMove(pairedMoveNumber == 1 ? 0.75f : 3f, pairedMoveNumber == 1 ? "residual" : "conversion");
                return;
            }
            var previous = combat.LastOutcome;
            var accepted = TryNativeAbilityTargetClick(nativeControls.MountPrimaryAbility, target, "paired-unaffordable-mount-attack");
            pairedSample["conversionAttackRejected"] = !accepted && ReferenceEquals(previous, combat.LastOutcome) && !combat.HasActiveCommand;
            RequirePaired((bool)pairedSample["conversionAttackRejected"], "Converted Standard admitted an unaffordable mount attack.");
            pairedStage = 3;
        }

        private void BeginPairedAttack(bool mountActor)
        {
            pairedAttackMount = mountActor;
            pairedPriorOutcome = combat.LastOutcome;
            var actor = mountActor ? horse : rider;
            pairedOperation = new JObject { ["kind"] = "attack", ["actor"] = actor.UniqueId,
                ["before"] = allocationTrace.Snapshot(actor), ["riderBefore"] = allocationTrace.Snapshot(rider),
                ["mountBefore"] = allocationTrace.Snapshot(horse) };
            ((JArray)pairedSample["operations"]).Add(pairedOperation);
            pairedOperation["clicked"] = TryNativeAbilityTargetClick(mountActor ? nativeControls.MountPrimaryAbility : nativeControls.RiderPrimaryAbility,
                target, mountActor ? "paired-mount-primary" : "paired-rider-primary");
            RequirePaired((bool)pairedOperation["clicked"], "Native paired Primary input was rejected: " + combat.LastFeedback);
            allocationMoveFrame = Time.frameCount; pairedStage = 2; ResetLeafClock();
        }

        private void FinishPairedAttack()
        {
            if (ReferenceEquals(pairedPriorOutcome, combat.LastOutcome) || combat.HasActiveCommand ||
                !rider.Commands.Empty || !horse.Commands.Empty)
            {
                if (Time.frameCount > allocationMoveFrame + 900)
                    throw new InvalidOperationException("Paired native attack did not finish.");
                return;
            }
            var outcome = combat.LastOutcome;
            var actor = pairedAttackMount ? horse : rider;
            pairedOperation["after"] = allocationTrace.Snapshot(actor);
            pairedOperation["riderAfter"] = allocationTrace.Snapshot(rider);
            pairedOperation["mountAfter"] = allocationTrace.Snapshot(horse);
            pairedOperation["result"] = outcome.Result;
            pairedOperation["completedAttacks"] = outcome.NativeCompletedAttackCount;
            pairedOperation["nativeRule"] = outcome.NativeAttackRuleObserved;
            RequirePaired(outcome.ActorId == actor.UniqueId && outcome.ResourceOwnerId == actor.UniqueId &&
                outcome.Result == "Success" && outcome.NativeCompletedAttackCount == 1 && outcome.NativeAttackRuleObserved &&
                actor.CombatState.Cooldown.StandardAction > 0f,
                "Paired attack did not preserve native actor/rule/cost/single-Primary semantics.");
            RequirePaired(ReferenceEquals(allocationTurn, Game.Instance.TurnBasedCombatController.CurrentTurn),
                "Rider attack ended activation before mount continuation.");
            if (!pairedAttackMount) { BeginPairedAttack(true); return; }
            RequirePaired(horse.CombatState.Cooldown.MoveAction >= 3f && !horse.HasStandardAction(),
                "Mount partial move plus attack did not exhaust native allocation.");
            BeginPairedMove(0.75f, "exhausted-rejection");
        }

        private void RequirePaired(bool condition, string failure)
        {
            if (condition) return;
            pairedGateErrors.Add(failure);
            // Flush the current operation before the outer exception path takes
            // its artifact snapshot; the frame-start copy predates this result.
            observations["pairedActivationGate"]["measuredActivations"] = pairedActivations.DeepClone();
            observations["pairedActivationGate"]["errors"] = new JArray(pairedGateErrors);
            throw new InvalidOperationException(failure);
        }

        private void CompletePairedGate()
        {
            var visits = pairedTurnVisits.OfType<JObject>().ToArray();
            foreach (var sample in pairedActivations.OfType<JObject>())
            {
                var round = (int)sample["round"];
                RequirePaired(visits.Any(v => (int)v["round"] == round && !(bool)v["friendly"]) &&
                    visits.Any(v => (int)v["round"] == round && (bool)v["friendly"] && !(bool)v["principal"]),
                    "A measured activation lacks unrelated friendly/enemy native turns.");
            }
            allocationTrace.Record("first-gate-sealed", rider);
            var trace = allocationTrace.Capture();
            RequirePaired(pairedReactionRefreshed, "Consumed native mount reaction was not renewed at the next paired boundary.");
            RequirePaired((int)trace["dropped"] == 0 && (int)trace["observationErrors"] == 0,
                "The native paired trace contains dropped events or observation errors.");
            var callbacks = ActorAllocationCallbackEvidence.Evaluate(trace,
                allocationRoundFacts.Select(fact => (string)fact.Capture()["actor"]), allocationFirstRound);
            var evidence = new JObject { ["level"] = "NATIVE INTEGRATION", ["gameplayQualified"] = true,
                ["inputKind"] = "scripted-native-handler-integration", ["principal"] = rider.UniqueId,
                ["firstRound"] = allocationFirstRound, ["activations"] = pairedActivations.DeepClone(),
                ["traceEndSequence"] = ((JArray)trace["events"]).Count,
                ["turnVisits"] = pairedTurnVisits.DeepClone(), ["refresh"] = observations["pairedRefreshAfterEarlyEnd"].DeepClone(),
                ["errors"] = new JArray(pairedGateErrors), ["reactions"] = pairedReactionEvidence.DeepClone() };
            AddRow("P01-three-paired-activations", pairedGateErrors.Count == 0, "Native paired movement, both actor attacks, conversion, exhaustion and early End Turn across three activations.", evidence);
            AddRow("A05-native-preparation-callbacks", (bool)callbacks["passed"], "Exact-candidate native actor preparation and callback/effect counts.", callbacks);
            BeginPairedControlProbe();
        }
    }
}
