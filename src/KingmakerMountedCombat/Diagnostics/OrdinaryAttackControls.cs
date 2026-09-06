using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Enums;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Parameterized controls in the existing guarded Horse fixture and result
    // protocol. Native input/planning/expenditure remains the subject of the test.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string OrdinaryAttackControlsScenario = "ordinary-attack-controls-tb";
        private bool IsOrdinaryAttackControls => request.Scenario == OrdinaryAttackControlsScenario;
        private static readonly string[] OrdinaryCaseIds = { "C01-B", "C01-C", "C01-D" };
        private int ordinaryCase;
        private int ordinaryStage;
        private bool ordinaryControlSent;
        private JObject ordinaryBefore;
        private JObject ordinaryPrediction;
        private UnitAttack ordinaryMeasured;
        private TurnController ordinaryTurn;
        private int ordinaryRepeats;
        private bool ordinaryContinuity;
        private long ordinaryIntentBefore;
        private bool OrdinaryMounted => ordinaryCase != 0;
        private bool OrdinaryPrimary => ordinaryCase == 2;

        private void BeginOrdinaryAttackControls()
        {
            if (settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Ordinary attack controls require exact separate-turn C0 settings.");
            rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
            rangedWeaponLease.Acquire(WeaponCategory.Longbow);
            AcquirePhase3hRapidShot();
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat);
            step = Phase3dHorseStep.Phase3gControls;
            BeginOrdinaryCase();
        }

        private void BeginOrdinaryCase()
        {
            if (ordinaryCase >= OrdinaryCaseIds.Length) { BeginCleanup(); return; }
            ordinaryAttackTrace.BeginCase(OrdinaryCaseIds[ordinaryCase]);
            ordinaryStage = 0;
            ordinaryControlSent = false;
            ordinaryBefore = null;
            ordinaryMeasured = null;
            ordinaryPrediction = null;
            ordinaryTurn = null;
            ordinaryRepeats = 0;
            ordinaryContinuity = true;
            ResetLeafClock();
        }

        private void TickOrdinaryAttackControls()
        {
            var game = Game.Instance;
            observations["ordinaryAttackProgress"] = new JObject {
                ["caseId"] = ordinaryCase < OrdinaryCaseIds.Length ? OrdinaryCaseIds[ordinaryCase] : "cleanup",
                ["stage"] = ordinaryStage, ["state"] = relationship.State.ToString(),
                ["turnActor"] = game.TurnBasedCombatController.CurrentTurn?.Unit?.UniqueId,
                ["before"] = ordinaryBefore, ["prediction"] = ordinaryPrediction
            };
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (ordinaryStage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if ((relationship.State == RelationshipState.Mounted) != OrdinaryMounted)
                {
                    if (!ordinaryControlSent)
                    {
                        ordinaryControlSent = TryNativeAbilityTargetClick(OrdinaryMounted ? nativeControls.MountAbility :
                            nativeControls.DismountAbility, OrdinaryMounted ? horse : rider, "ordinary-fixture-pair-control");
                        if (!ordinaryControlSent) throw new InvalidOperationException("Native fixture Mount/Dismount input refused.");
                    }
                    return;
                }
                if (!OrdinaryMounted && (!PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation())) return;
                BeginTarget(3.5f, OrdinaryCaseIds[ordinaryCase]);
                ruleProbe.Arm(target, false);
                ordinaryStage = 1;
                return;
            }
            if (ordinaryStage == 1)
            {
                if (!IsCombatReady(OrdinaryMounted)) return;
                if (!CombatController.IsInTurnBasedCombat())
                {
                    if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                    if (!turnBasedModeProbe.TemporaryDeliveryAttempted) turnBasedModeProbe.DispatchTemporaryValueIfRequired();
                    return;
                }
                var turn = game.TurnBasedCombatController.CurrentTurn;
                if (turn?.Unit != rider || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing)
                { TryEndPhase3gFixtureTurn(turn); return; }
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation ||
                    !rider.HasStandardAction() || game.HandsEquipmentController.IsUpdateScheduledFor(rider)) return;
                if (rider.IsMoveActionRestricted()) { TryEndPhase3gFixtureTurn(turn); return; }
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                ordinaryTurn = turn;
                ordinaryBefore = CaptureOrdinaryLiveState();
                ordinaryIntentBefore = combat.StockAttackIntentStartCount;
                using (var input = new NativeOrdinaryAttackInput(target))
                {
                    var beforePrediction = CaptureOrdinaryLiveState();
                    for (var index = 0; index < 3; index++) input.Predict();
                    var afterPrediction = CaptureOrdinaryLiveState();
                    ordinaryPrediction = new JObject {
                        ["count"] = 3, ["pure"] = JToken.DeepEquals(beforePrediction, afterPrediction),
                        ["before"] = beforePrediction, ["after"] = afterPrediction,
                        ["fullEnabled"] = turn.EnabledFullAttack, ["nativeEstimate"] = UnitAttack.EstimateFullAttacks(rider)
                    };
                    if (!targetService.BeginExpectedAttackDispatch(target))
                        throw new InvalidOperationException("Exact ordinary control target admission failed.");
                    var clicked = OrdinaryPrimary
                        ? TryNativeAbilityTargetClick(nativeControls.RiderPrimaryAbility, target, "ordinary-primary-input")
                        : input.Click();
                    if (!clicked) { CompleteOrdinaryCase(false, "Native input admission refused."); return; }
                }
                ordinaryStage = 2;
                ResetLeafClock();
                return;
            }
            if (ordinaryStage == 2)
            {
                if (ordinaryMeasured == null) ordinaryMeasured = ordinaryAttackTrace.LastStartedRiderAttack;
                if (ordinaryMeasured == null) return;
                if (OrdinaryMounted && !OrdinaryPrimary && !ordinaryMeasured.IsFinished && ordinaryRepeats == 0)
                {
                    var before = CaptureOrdinaryLiveState();
                    var intent = combat.StockAttackIntentStartCount;
                    using (var input = new NativeOrdinaryAttackInput(target))
                    {
                        for (var index = 0; index < 3; index++)
                        {
                            input.Predict();
                            // The native pointer admission gate may consume a repeated
                            // busy click; the observed handler additionally exercises
                            // the real mounted same-target routing service.
                            input.Click();
                            input.Click(false);
                            ordinaryRepeats++;
                        }
                    }
                    ordinaryContinuity = JToken.DeepEquals(before, CaptureOrdinaryLiveState()) &&
                        combat.StockAttackIntentStartCount == intent && rider.Commands.Contains(ordinaryMeasured);
                }
                if (!ordinaryMeasured.IsFinished || rider.AreHandsBusyWithAnimation ||
                    ruleProbe.RiderResolvedCount < ordinaryMeasured.GetAttackIndex()) return;
                var nativePlan = ordinaryMeasured.AllAttacks.Count;
                var success = ordinaryMeasured.Result == UnitCommand.ResultType.Success &&
                    ordinaryMeasured.Executor == rider && ordinaryMeasured.GetAttackIndex() == nativePlan &&
                    ruleProbe.RiderResolvedCount == nativePlan && ruleProbe.MountNonOpportunityAttackRuleCount == 0 &&
                    ruleProbe.PairForcedD20Count == 0 && ReferenceEquals(game.TurnBasedCombatController.CurrentTurn, ordinaryTurn) &&
                    (bool)ordinaryPrediction["pure"] && ordinaryContinuity &&
                    (OrdinaryPrimary ? nativePlan == 1 && ordinaryMeasured.IsSingleAttack && !ordinaryMeasured.IsFullAttack :
                        ordinaryMeasured.IsFullAttack && nativePlan >= 2) &&
                    Math.Abs(rider.CombatState.Cooldown.StandardAction - 6f) < 0.001f &&
                    Math.Abs(rider.CombatState.Cooldown.MoveAction - (OrdinaryPrimary ? 0f : 3f)) < 0.001f &&
                    Math.Abs(horse.CombatState.Cooldown.StandardAction - (float)ordinaryBefore["mount"]["standard"]) < 0.001f &&
                    Math.Abs(horse.CombatState.Cooldown.MoveAction - (float)ordinaryBefore["mount"]["move"]) < 0.001f;
                CompleteOrdinaryCase(success, "Matched native stationary plan, effects, costs and prediction continuity.");
                return;
            }
            if (ordinaryStage == 3)
            {
                if (combat.HasActiveCommand || ruleProbe.RiderResolvedCount < ruleProbe.RiderNonOpportunityAttackRuleCount) return;
                TryLeaveCombat(target); TryLeaveCombat(rider); TryLeaveCombat(horse);
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) return;
                    targetService.Dispose(); targetService = null; target = null;
                }
                if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                if (CombatController.IsInTurnBasedCombat() || game.TurnBasedCombatController.Initialized ||
                    game.Player.IsInCombat || !rider.Commands.Empty || !horse.Commands.Empty) return;
                if (!OrdinaryMounted && (!RestoreCombatMountRiderAiIsolation() || !RestoreUnmountedHorseAiIsolation()))
                    throw new InvalidOperationException("Unmounted control AI isolation did not restore exactly.");
                if (!OrdinaryMounted)
                {
                    observations["ai-" + OrdinaryCaseIds[ordinaryCase]] = new JObject {
                        ["rider"] = CaptureCombatMountRiderAiIsolation(), ["mount"] = CaptureUnmountedHorseAiIsolation()
                    };
                    combatMountRiderAiLease = null;
                    unmountedHorseAiLease = null;
                    unmountedHorseAiSettleRequested = false;
                }
                ordinaryCase++;
                BeginOrdinaryCase();
            }
        }

        private JObject CaptureOrdinaryLiveState()
        {
            return new JObject { ["rider"] = CaptureOrdinaryActor(rider), ["mount"] = CaptureOrdinaryActor(horse),
                ["relationship"] = relationship.State.ToString(), ["intentStarts"] = combat.StockAttackIntentStartCount,
                ["duplicateDispatches"] = combat.StockAttackDuplicateDispatchCount };
        }

        private static JObject CaptureOrdinaryActor(Kingmaker.EntitySystem.Entities.UnitEntityData actor)
        {
            return new JObject {
                ["id"] = actor.UniqueId, ["standard"] = actor.CombatState.Cooldown.StandardAction,
                ["move"] = actor.CombatState.Cooldown.MoveAction, ["swift"] = actor.CombatState.Cooldown.SwiftAction,
                ["position"] = new JArray(actor.Position.x, actor.Position.y, actor.Position.z),
                ["commands"] = RuntimeHelpers.GetHashCode(actor.Commands),
                ["raw"] = new JArray(actor.Commands.Raw.Select(CaptureOrdinaryCommand)),
                ["queue"] = new JArray(actor.Commands.Queue.Select(CaptureOrdinaryCommand)),
                ["previous"] = CaptureOrdinaryCommand(actor.Commands.PreviousCommand),
                ["group"] = actor.Commands.GroupCommand == null ? 0 : RuntimeHelpers.GetHashCode(actor.Commands.GroupCommand)
            };
        }

        private static JToken CaptureOrdinaryCommand(UnitCommand command)
        {
            return command == null ? JValue.CreateNull() : (JToken)new JObject {
                ["id"] = RuntimeHelpers.GetHashCode(command), ["type"] = command.GetType().FullName,
                ["started"] = command.IsStarted, ["acted"] = command.IsActed,
                ["finished"] = command.IsFinished, ["result"] = command.Result.ToString()
            };
        }

        private void CompleteOrdinaryCase(bool success, string detail)
        {
            AddRow(OrdinaryCaseIds[ordinaryCase], success, detail, new JObject {
                ["level"] = "NATIVE INTEGRATION", ["parameters"] = new JObject { ["mode"] = "TB", ["mounted"] = OrdinaryMounted,
                    ["primary"] = OrdinaryPrimary, ["weapon"] = "Longbow", ["rapidShot"] = phase3hRapidToggle.IsOn },
                ["before"] = ordinaryBefore, ["after"] = CaptureOrdinaryLiveState(), ["prediction"] = ordinaryPrediction,
                ["repeatedRequests"] = ordinaryRepeats, ["continuity"] = ordinaryContinuity,
                ["intentStarts"] = combat.StockAttackIntentStartCount - ordinaryIntentBefore,
                ["nativeCommand"] = CaptureOrdinaryCommand(ordinaryMeasured),
                ["nativeFull"] = ordinaryMeasured?.IsFullAttack, ["nativeSingle"] = ordinaryMeasured?.IsSingleAttack,
                ["planned"] = ordinaryMeasured?.AllAttacks.Count, ["completed"] = ordinaryMeasured?.GetAttackIndex(),
                ["rules"] = ruleProbe.CapturePairEvidence()
            });
            SelectionManager.Instance.Stop();
            ordinaryStage = 3;
            ResetLeafClock();
        }
    }
}
