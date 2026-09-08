using System;
using System.Linq;
using Kingmaker;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private ChargeWarningObserver chunk4ChargeWarnings;
        private JObject chunk4ChargeSafetyEvidence;
        private JObject chunk4ChargeRecovery;
        private int chunk4RecoveryStage;
        private Vector3 chunk4RecoveryOrigin;
        private UnitMoveTo chunk4RecoveryMove;
        private UnitAttack chunk4RecoveryAttack;
        private TurnController chunk4RecoveryTurn;
        private float chunk4RecoveryMaximumStandard;

        private void BeginChunk4ChargeRecovery(JObject safety)
        {
            chunk4ChargeRecovery = new JObject();
            chunk4RecoveryStage = 0;
            chunk4RecoveryMove = null;
            chunk4RecoveryAttack = null;
            chunk4RecoveryMaximumStandard = 0;
            chunk4ChargeSafetyEvidence = safety;
            observations["chargeRecovery-" + Chunk4ChargeId] = safety;
            safety["recovery"] = chunk4ChargeRecovery;
            chunk4ChargeRecovery["inputKind"] = "scripted-native-handler-and-command-boundaries";
            chunk4ChargeRecovery["before"] = CaptureOrdinaryLiveState();
            chunk4RecoveryOrigin = horse.Position;
            chunk4RecoveryTurn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            chunk4ChargeStage = 4;
            ResetLeafClock();
        }

        private void TickChunk4ChargeRecovery()
        {
            var game = Game.Instance;
            var turn = game.TurnBasedCombatController.CurrentTurn;
            chunk4ChargeRecovery["stage"] = chunk4RecoveryStage;
            chunk4RecoveryMaximumStandard = Math.Max(chunk4RecoveryMaximumStandard, rider.CombatState.Cooldown.StandardAction);
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (chunk4RecoveryStage == 0)
            {
                game.SelectedAbilityHandler.SetAbility(null);
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var destination = FindWalkablePoint(horse.Position, 2.5f, 0.5f, point =>
                    HorizontalDistance(point, target.Position) < HorizontalDistance(horse.Position, target.Position));
                using (var input = new NativeOrdinaryAttackInput(destination))
                {
                    input.Predict();
                    if (!input.Click()) throw new InvalidOperationException("Charge recovery native ground input refused.");
                }
                chunk4RecoveryMove = horse.Commands.Move as UnitMoveTo;
                if (chunk4RecoveryMove?.Executor != horse)
                    throw new InvalidOperationException("Charge recovery lost native mount movement ownership.");
                chunk4RecoveryStage = 1; ResetLeafClock(); return;
            }
            if (chunk4RecoveryStage == 1)
            {
                if (!chunk4RecoveryMove.IsFinished || horse.View.AgentASP.IsReallyMoving || !horse.Commands.Empty) return;
                chunk4ChargeRecovery["afterMove"] = CaptureOrdinaryLiveState();
                chunk4ChargeRecovery["moveCommand"] = CaptureOrdinaryCommand(chunk4RecoveryMove);
                chunk4ChargeRecovery["moveDistance"] = HorizontalDistance(horse.Position, chunk4RecoveryOrigin);
                if (chunk4RecoveryMove.Result != UnitCommand.ResultType.Success ||
                    (float)chunk4ChargeRecovery["moveDistance"] < 0.5f || rider.CombatState.Cooldown.MoveAction > 0.001f)
                    throw new InvalidOperationException("Legal transport after rejected Charge failed or taxed rider Move.");
                if (!rider.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                if (!targetService.ExpectedAttackDispatchStarted && !targetService.BeginExpectedAttackDispatch(target))
                    throw new InvalidOperationException("Charge recovery target was invalid before ordinary attack.");
                using (var input = new NativeOrdinaryAttackInput(target))
                {
                    input.Predict();
                    if (!input.Click()) return;
                }
                chunk4RecoveryStage = 2; ResetLeafClock(); return;
            }
            if (chunk4RecoveryStage == 2)
            {
                chunk4RecoveryAttack = ordinaryAttackTrace.LastStartedRiderAttack;
                if (chunk4RecoveryAttack == null) return;
                if (chunk4RecoveryAttack.IsFinished || rider.Commands.Standard == null)
                    throw new InvalidOperationException("Charge queue control missed the live ordinary attack window.");
                game.IsPaused = true;
                var before = CaptureOrdinaryLiveState();
                var warningBefore = chunk4ChargeWarnings.Count;
                var queued = new UnitUseAbility(chunk4ChargeAbility, new TargetWrapper(target));
                rider.Commands.AddToQueue(queued);
                var queuePure = JToken.DeepEquals(before, CaptureOrdinaryLiveState());
                // A real initialized native shell also exercises execution without
                // relying on the UI/admission check. No cooldown or state is set.
                var prepared = new UnitUseAbility(chunk4ChargeAbility, new TargetWrapper(target));
                prepared.Init(rider);
                prepared.Start();
                var executionPure = JToken.DeepEquals(before, CaptureOrdinaryLiveState());
                game.SelectedAbilityHandler.SetAbility(chunk4ChargeAbility);
                var clicked = game.SelectedAbilityHandler.OnClick(target.View.gameObject, target.Position, 0, false, false);
                var clickPure = JToken.DeepEquals(before, CaptureOrdinaryLiveState());
                game.SelectedAbilityHandler.SetAbility(null);
                chunk4ChargeRecovery["liveQueue"] = new JObject {
                    ["before"] = before, ["after"] = CaptureOrdinaryLiveState(),
                    ["nativeQueueApi"] = "UnitCommands.AddToQueue -> AddToQueueInternal",
                    ["queuedRejectedBeforeInit"] = queued.Executor == null && !queued.IsStarted && !queued.IsActed,
                    ["queuePure"] = queuePure, ["executionPure"] = executionPure, ["clickPure"] = clickPure,
                    ["preparedStartRejected"] = prepared.IsFinished && !prepared.IsStarted && !prepared.IsActed,
                    ["clickRejected"] = !clicked, ["warnings"] = chunk4ChargeWarnings.Count - warningBefore,
                    ["sameLiveAttack"] = ReferenceEquals(chunk4RecoveryAttack, ordinaryAttackTrace.LastStartedRiderAttack) &&
                        !chunk4RecoveryAttack.IsFinished
                };
                if (!queuePure || !executionPure || !clickPure || queued.Executor != null || clicked ||
                    !prepared.IsFinished || prepared.IsStarted || prepared.IsActed || chunk4RecoveryAttack.IsFinished ||
                    chunk4ChargeWarnings.Count - warningBefore != 3)
                    throw new InvalidOperationException("Rejected Charge disturbed a legal live command or lacked native feedback.");
                game.IsPaused = false;
                chunk4RecoveryStage = 3; ResetLeafClock(); return;
            }
            if (chunk4RecoveryStage == 3)
            {
                if (!chunk4RecoveryAttack.IsFinished || ruleProbe.RiderResolvedCount < ruleProbe.RiderNonOpportunityAttackRuleCount ||
                    ruleProbe.MountResolvedCount < ruleProbe.MountNonOpportunityAttackRuleCount) return;
                var completed = chunk4RecoveryAttack.GetAttackIndex();
                var planned = chunk4RecoveryAttack.AllAttacks.Count;
                var complete = completed == planned && planned > 0 && ruleProbe.RiderResolvedCount > 0 &&
                    (chunk4RecoveryAttack.Result == UnitCommand.ResultType.Success ||
                        ordinaryAttackTrace.NativeRecoveryInterrupt(chunk4RecoveryAttack) != null);
                chunk4ChargeRecovery["ordinaryAttack"] = new JObject {
                    ["command"] = CaptureOrdinaryCommand(chunk4RecoveryAttack), ["planned"] = planned,
                    ["completed"] = completed, ["complete"] = complete, ["isCharge"] = chunk4RecoveryAttack.IsCharge,
                    ["maximumRiderStandard"] = chunk4RecoveryMaximumStandard, ["rules"] = ruleProbe.CapturePairEvidence()
                };
                if (!complete || chunk4RecoveryAttack.IsCharge || chunk4RecoveryMaximumStandard <= 0f)
                    throw new InvalidOperationException("Ordinary recovery after Charge rejection did not complete its native plan/cost.");
                SelectionManager.Instance.Stop();
                chunk4RecoveryStage = 4; ResetLeafClock(); return;
            }
            if (chunk4RecoveryStage == 4)
            {
                if (combat.HasActiveCommand || combat.HasStockAttackIntent || !rider.Commands.Empty || !horse.Commands.Empty ||
                    ruleProbe.RiderResolvedCount < ruleProbe.RiderNonOpportunityAttackRuleCount ||
                    ruleProbe.MountResolvedCount < ruleProbe.MountNonOpportunityAttackRuleCount) return;
                chunk4ChargeRecovery["afterStop"] = CaptureOrdinaryLiveState();
                if (Chunk4ChargeTb)
                {
                    if (!ReferenceEquals(turn, chunk4RecoveryTurn))
                        throw new InvalidOperationException("Charge recovery lost its paired activation before explicit End.");
                    TryEndPhase3gFixtureTurn(turn);
                    if (!ReferenceEquals(phase3gEndedTurn, turn)) return;
                    chunk4ChargeRecovery["nativeEndInput"] = true;
                    chunk4RecoveryStage = 5; ResetLeafClock(); return;
                }
                FinishChunk4ChargeRecovery(); return;
            }
            if (chunk4RecoveryStage == 5)
            {
                if (turn == null || ReferenceEquals(turn, chunk4RecoveryTurn)) return;
                if (turn.Unit == rider || turn.Unit == horse || combat.PairedPartnerContext != null)
                    throw new InvalidOperationException("Charge recovery End did not release the pair to an unrelated native turn.");
                chunk4ChargeRecovery["nextUnrelatedActor"] = turn.Unit.UniqueId;
                FinishChunk4ChargeRecovery();
            }
        }

        private void FinishChunk4ChargeRecovery()
        {
            chunk4ChargeSafetyEvidence["nativeWarnings"] = chunk4ChargeWarnings.Capture();
            chunk4ChargeRecovery["completed"] = true;
            AddRow(Chunk4ChargeId, true, "Exact mounted Charge rejected before costs; native move, ordinary attack, queue safety and End recovered.",
                chunk4ChargeSafetyEvidence);
            chunk4ChargeStage = 3;
            ResetLeafClock();
        }

        private sealed class ChargeWarningObserver : IWarningNotificationUIHandler, IDisposable
        {
            private readonly IDisposable subscription;
            private readonly JArray events = new JArray();
            internal int Count => events.Count;
            internal ChargeWarningObserver() { subscription = EventBus.Subscribe(this); }
            public void HandleWarning(Kingmaker.UI.WarningNotificationType type, bool addToLog) { }
            public void HandleWarning(string text, bool addToLog)
            {
                if (text == MountedChargeSafetyPolicy.Feedback && events.Count < 64)
                    events.Add(new JObject { ["text"] = text, ["frame"] = Time.frameCount, ["addToLog"] = addToLog });
            }
            internal JArray Capture() => (JArray)events.DeepClone();
            public void Dispose() { subscription.Dispose(); }
        }
    }
}
