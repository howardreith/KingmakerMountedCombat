using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
    // Uses real turns, native input and native budgets throughout.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool IsChunk4PairedPlay => request.Scenario == "chunk4-sustained-tb";
        private static readonly string[] Chunk4PairedPlayIds = {
            "C4-SUSTAINED-TB-rider-first", "C4-SUSTAINED-TB-mount-first",
            "C4-SUSTAINED-TB-rider-exhausted", "C4-SUSTAINED-TB-mount-exhausted",
            "C4-SUSTAINED-TB-early-end", "C4-SUSTAINED-TB-after-early-end"
        };
        private int chunk4PairedPlayStage;
        private int chunk4PairedPlayActivation;
        private int chunk4PairedPlayOperation;
        private int chunk4PairedPlaySelectionFrame;
        private bool chunk4PairedPlayMountSent;
        private TurnController chunk4PairedPlayTurn;
        private TurnController chunk4PairedPlayPartner;
        private UnitMoveTo chunk4PairedPlayMove;
        private UnitAttack chunk4PairedPlayAttack;
        private JObject chunk4PairedPlaySample;
        private JObject chunk4PairedPlayAction;
        private readonly JArray chunk4PairedPlaySamples = new JArray();
        private string Chunk4PairedPlayId => Chunk4PairedPlayIds[chunk4PairedPlayActivation - 1];
        private UnitEntityData Chunk4PairedPlayActor =>
            (chunk4PairedPlayActivation == 2 || chunk4PairedPlayActivation == 4) == (chunk4PairedPlayOperation == 0) ? horse : rider;
        private bool Chunk4PairedPlayFull => chunk4PairedPlayOperation == 0 &&
            (chunk4PairedPlayActivation == 3 || chunk4PairedPlayActivation == 4);

        private void BeginChunk4PairedPlay()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Sustained paired play requires the accepted single authority.");
            CaptureIdleFixturePartyForCleanup();
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString());
            pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
            observations["chunk4PairedPlay"] = chunk4PairedPlaySamples;
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private bool Chunk4PairedPlayIdle => !combat.HasActiveCommand && !combat.HasStockAttackIntent &&
            !combat.HasActiveGroundMovement && rider.Commands.Empty && horse.Commands.Empty &&
            !rider.AreHandsBusyWithAnimation && !horse.AreHandsBusyWithAnimation &&
            !Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(rider) &&
            !Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(horse);

        private void TickChunk4PairedPlay()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            observations["chunk4PairedPlayProgress"] = new JObject {
                ["stage"] = chunk4PairedPlayStage, ["activationNumber"] = chunk4PairedPlayActivation,
                ["operation"] = chunk4PairedPlayOperation, ["identity"] = combat.PairedActivationIdentity,
                ["turnActor"] = turn?.Unit.UniqueId, ["live"] = CaptureOrdinaryLiveState()
            };
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (turn?.Unit == horse) throw new InvalidOperationException("Sustained paired play produced an independent mount turn.");
            if (chunk4PairedPlayStage == 0)
            {
                if (!Chunk4PairedPlayIdle) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!chunk4PairedPlayMountSent)
                        chunk4PairedPlayMountSent = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk4-paired-play-mount");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                BeginTarget(6f, "chunk4-paired-play"); ruleProbe.Arm(target, false);
                chunk4PairedPlayStage = 1; ResetLeafClock(); return;
            }
            if (chunk4PairedPlayStage == 1)
            {
                if (!IsCombatReady(true) || !CombatController.IsInTurnBasedCombat()) return;
                if (turn?.Unit != rider || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing)
                { TryEndPhase3gFixtureTurn(turn); return; }
                if (!Chunk4PairedPlayIdle) return;
                chunk4PairedPlayTurn = turn;
                var point = FindPairedControlPoint(1f, "chunk4-paired-play-adjacency");
                using (var input = new NativeOrdinaryAttackInput(point))
                {
                    input.Predict(); var cycles = 0;
                    while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycles++ < 8)
                    { input.Click(button: 1); input.Predict(); }
                    if (!input.Click()) return;
                }
                chunk4PairedPlayMove = horse.Commands.Move as UnitMoveTo;
                if (chunk4PairedPlayMove?.Executor != horse) throw new InvalidOperationException("Native paired setup lost mount movement authority.");
                chunk4PairedPlayStage = 2; ResetLeafClock(); return;
            }
            if (chunk4PairedPlayStage == 2)
            {
                if (!chunk4PairedPlayMove.IsFinished || !Chunk4PairedPlayIdle) return;
                if (chunk4PairedPlayMove.Result != UnitCommand.ResultType.Success || rider.CombatState.Cooldown.MoveAction != 0f)
                    throw new InvalidOperationException("Paired native positioning failed or taxed rider Move.");
                TryEndPhase3gFixtureTurn(turn);
                if (!ReferenceEquals(phase3gEndedTurn, chunk4PairedPlayTurn)) return;
                chunk4PairedPlayStage = 3; ResetLeafClock(); return;
            }
            if (chunk4PairedPlayStage == 3)
            {
                if (turn == null || ReferenceEquals(turn, chunk4PairedPlayTurn)) return;
                if (turn.Unit != rider) { TryEndPhase3gFixtureTurn(turn); return; }
                if (!Chunk4PairedPlayIdle || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                if (combat.PairedPartnerContext?.Unit != horse || string.IsNullOrEmpty(combat.PairedActivationIdentity) ||
                    !rider.CombatState.Prepared || !horse.CombatState.Prepared || rider.CombatState.Cooldown.StandardAction != 0f ||
                    rider.CombatState.Cooldown.MoveAction != 0f || horse.CombatState.Cooldown.StandardAction != 0f || horse.CombatState.Cooldown.MoveAction != 0f)
                    throw new InvalidOperationException("Fresh paired play activation lacks its native grants or preparation.");
                chunk4PairedPlayTurn = turn; chunk4PairedPlayPartner = combat.PairedPartnerContext;
                if (chunk4PairedPlaySamples.Any(sample => (string)sample["identity"] == combat.PairedActivationIdentity))
                    throw new InvalidOperationException("A new native paired turn reused an earlier activation identity.");
                chunk4PairedPlayActivation++; chunk4PairedPlayOperation = 0;
                chunk4PairedPlaySample = new JObject {
                    ["level"] = "NATIVE INTEGRATION", ["mode"] = "TB", ["caseId"] = Chunk4PairedPlayId,
                    ["round"] = controller.RoundNumber, ["identity"] = combat.PairedActivationIdentity,
                    ["principal"] = turn.Unit.UniqueId, ["partner"] = horse.UniqueId,
                    ["riderPrepared"] = rider.CombatState.Prepared, ["mountPrepared"] = horse.CombatState.Prepared,
                    ["partnerContext"] = RuntimeHelpers.GetHashCode(chunk4PairedPlayPartner),
                    ["before"] = CaptureOrdinaryLiveState(), ["operations"] = new JArray()
                };
                chunk4PairedPlaySamples.Add(chunk4PairedPlaySample);
                if (chunk4PairedPlayActivation == 6)
                {
                    AddRow(Chunk4PairedPlayId, true, "Fresh native preparation after early End retained independent actor budgets and a new activation identity.", chunk4PairedPlaySample);
                    observations["ordinaryTrace"] = ordinaryAttackTrace.Capture(); BeginCleanup(); return;
                }
                chunk4PairedPlayStage = chunk4PairedPlayActivation == 5 ? 8 : 4; ResetLeafClock(); return;
            }
            if (chunk4PairedPlayStage >= 4 && chunk4PairedPlayStage <= 7 &&
                (!ReferenceEquals(turn, chunk4PairedPlayTurn) || !ReferenceEquals(combat.PairedPartnerContext, chunk4PairedPlayPartner) ||
                    (string)chunk4PairedPlaySample["identity"] != combat.PairedActivationIdentity))
                throw new InvalidOperationException("An ordinary operation changed paired activation or partner ownership.");
            if (chunk4PairedPlayStage == 4)
            {
                if (!Chunk4PairedPlayIdle) return;
                game.SelectedAbilityHandler.SetAbility(null);
                SelectionManager.Instance.SelectUnit(Chunk4PairedPlayFull ? Chunk4PairedPlayActor.View : rider.View, true, true, false);
                chunk4PairedPlaySelectionFrame = Time.frameCount;
                chunk4PairedPlayStage = 5; ResetLeafClock(); return;
            }
            if (chunk4PairedPlayStage == 5)
            {
                if (Time.frameCount < chunk4PairedPlaySelectionFrame + 3 || !Chunk4PairedPlayIdle) return;
                var actor = Chunk4PairedPlayActor;
                if (SelectionManager.Instance.SingleSelectedUnit != (Chunk4PairedPlayFull ? actor : rider))
                    throw new InvalidOperationException("Native actor selection was not retained for paired input.");
                var traceCase = Chunk4PairedPlayId + "-operation-" + chunk4PairedPlayOperation;
                ordinaryAttackTrace.BeginCase(traceCase);
                chunk4PairedPlayAction = new JObject {
                    ["kind"] = Chunk4PairedPlayFull ? "ordinary-Full" : "explicit-Primary", ["actor"] = actor.UniqueId,
                    ["before"] = CaptureOrdinaryLiveState(), ["rulesBefore"] = ruleProbe.CapturePairEvidence(),
                    ["beforeIdentity"] = combat.PairedActivationIdentity, ["selected"] = SelectionManager.Instance.SingleSelectedUnit.UniqueId
                };
                ((JArray)chunk4PairedPlaySample["operations"]).Add(chunk4PairedPlayAction);
                if (!targetService.BeginExpectedAttackDispatch(target)) throw new InvalidOperationException("Paired play target is not eligible for native dispatch.");
                if (Chunk4PairedPlayFull)
                {
                    var context = actor == horse ? chunk4PairedPlayPartner : turn;
                    using (var input = new NativeOrdinaryAttackInput(target))
                    {
                        var before = CaptureOrdinaryLiveState(); input.Predict(context); input.Predict(context);
                        if (!JToken.DeepEquals(before, CaptureOrdinaryLiveState())) throw new InvalidOperationException("Paired play hover spent or changed live state.");
                        var cycles = 0;
                        while (!context.EnabledFullAttack && cycles++ < 8) { input.Click(button: 1); input.Predict(context); }
                        chunk4PairedPlayAction["cursorCycles"] = cycles;
                        if (!context.EnabledFullAttack || !input.Click()) throw new InvalidOperationException("Ordinary native Full control was unavailable.");
                    }
                }
                else if (!TryNativeAbilityTargetClick(actor == horse ? nativeControls.MountPrimaryAbility : nativeControls.RiderPrimaryAbility,
                    target, traceCase)) throw new InvalidOperationException("A legal actor Primary was rejected in sustained paired play.");
                chunk4PairedPlayAttack = null; chunk4PairedPlayStage = 6; ResetLeafClock(); return;
            }
            if (chunk4PairedPlayStage == 6)
            {
                var actor = Chunk4PairedPlayActor;
                if (chunk4PairedPlayAttack == null) chunk4PairedPlayAttack = actor == horse ? ordinaryAttackTrace.LastStartedMountAttack : ordinaryAttackTrace.LastStartedRiderAttack;
                if (chunk4PairedPlayAttack == null || !chunk4PairedPlayAttack.IsFinished) return;
                var before = chunk4PairedPlayAction["before"];
                var actorKey = actor == horse ? "mount" : "rider"; var otherKey = actor == horse ? "rider" : "mount";
                var ruleKey = actor == horse ? "mountResolved" : "riderResolved";
                var rules = ruleProbe.CapturePairEvidence();
                var resolved = (int)rules[ruleKey] - (int)chunk4PairedPlayAction["rulesBefore"][ruleKey];
                if (resolved < chunk4PairedPlayAttack.GetAttackIndex() || !Chunk4PairedPlayIdle) return;
                var after = CaptureOrdinaryLiveState();
                chunk4PairedPlayAction["after"] = after; chunk4PairedPlayAction["rulesAfter"] = rules;
                chunk4PairedPlayAction["command"] = CaptureOrdinaryCommand(chunk4PairedPlayAttack);
                chunk4PairedPlayAction["planned"] = chunk4PairedPlayAttack.AllAttacks.Count;
                chunk4PairedPlayAction["completed"] = chunk4PairedPlayAttack.GetAttackIndex();
                chunk4PairedPlayAction["resolved"] = resolved; chunk4PairedPlayAction["nativeFull"] = chunk4PairedPlayAttack.IsFullAttack;
                chunk4PairedPlayAction["nativePrimary"] = chunk4PairedPlayAttack.IsSingleAttack;
                if (chunk4PairedPlayAttack.Executor != actor || chunk4PairedPlayAttack.Result != UnitCommand.ResultType.Success ||
                    chunk4PairedPlayAttack.GetAttackIndex() != chunk4PairedPlayAttack.AllAttacks.Count || resolved != chunk4PairedPlayAttack.GetAttackIndex() ||
                    resolved < 1 || chunk4PairedPlayAttack.IsFullAttack != Chunk4PairedPlayFull ||
                    chunk4PairedPlayAttack.IsSingleAttack == Chunk4PairedPlayFull || (!Chunk4PairedPlayFull && resolved != 1) ||
                    (float)after[actorKey]["standard"] != 6f || (float)after[actorKey]["move"] != (Chunk4PairedPlayFull ? 3f : 0f) ||
                    (float)after[otherKey]["standard"] != (float)before[otherKey]["standard"] ||
                    (float)after[otherKey]["move"] != (float)before[otherKey]["move"] || ruleProbe.PairForcedD20Count != 0)
                    throw new InvalidOperationException("Sustained paired attack changed native mode, sequence, costs or the other actor's budget.");
                chunk4PairedPlayOperation++;
                if (chunk4PairedPlayOperation < 2) { chunk4PairedPlayStage = 4; ResetLeafClock(); return; }
                if (chunk4PairedPlayActivation == 3)
                {
                    SelectionManager.Instance.SelectUnit(horse.View, true, true, false);
                    var direction = horse.Position - target.Position; direction.y = 0f; direction.Normalize();
                    chunk4PairedPlayAction = new JObject { ["kind"] = "partner-move-after-rider-exhaustion", ["before"] = CaptureOrdinaryLiveState() };
                    ((JArray)chunk4PairedPlaySample["operations"]).Add(chunk4PairedPlayAction);
                    using (var input = new NativeOrdinaryAttackInput(horse.Position + direction * 0.75f))
                    {
                        input.Predict(); var cycles = 0;
                        while ((chunk4PairedPlayPartner.EnabledFiveFootStep || chunk4PairedPlayPartner.EnabledSingleActionMove) && cycles++ < 8)
                        { input.Click(button: 1); input.Predict(); }
                        chunk4PairedPlayAction["movementInput"] = new JObject {
                            ["selectedActor"] = SelectionManager.Instance.SingleSelectedUnit.UniqueId,
                            ["partnerFiveFootStep"] = chunk4PairedPlayPartner.EnabledFiveFootStep,
                            ["partnerSingleMove"] = chunk4PairedPlayPartner.EnabledSingleActionMove,
                            ["riderFiveFootStep"] = turn.EnabledFiveFootStep,
                            ["partnerStepMetresBefore"] = chunk4PairedPlayPartner.MetersMovedByFiveFootStep,
                            ["observationBefore"] = combat.CaptureUnifiedTurnSnapshot().LastMovementObservation };
                        if (chunk4PairedPlayPartner.EnabledFiveFootStep || chunk4PairedPlayPartner.EnabledSingleActionMove || !input.Click())
                            throw new InvalidOperationException("Partner paid move after rider exhaustion was not selected through native controls.");
                    }
                    chunk4PairedPlayMove = horse.Commands.Move as UnitMoveTo;
                    if (chunk4PairedPlayMove?.Executor != horse) throw new InvalidOperationException("Partner residual move lost its native owner.");
                    chunk4PairedPlayStage = 7; ResetLeafClock(); return;
                }
                chunk4PairedPlayStage = 8; ResetLeafClock(); return;
            }
            if (chunk4PairedPlayStage == 7)
            {
                chunk4PairedPlayAction["movementProgress"] = new JObject {
                    ["observation"] = combat.CaptureUnifiedTurnSnapshot().LastMovementObservation,
                    ["partnerStepMetres"] = chunk4PairedPlayPartner.MetersMovedByFiveFootStep,
                    ["partnerTimeMoved"] = chunk4PairedPlayPartner.TimeMoved,
                    ["groundResult"] = combat.LastGroundMoveResult, ["slotsRestored"] = combat.LastGroundMoveSlotRestored };
                if (!chunk4PairedPlayMove.IsFinished || !Chunk4PairedPlayIdle || combat.LastGroundMoveResult == null || !combat.LastGroundMoveSlotRestored) return;
                var after = CaptureOrdinaryLiveState(); var before = chunk4PairedPlayAction["before"];
                chunk4PairedPlayAction["after"] = after; chunk4PairedPlayAction["command"] = CaptureOrdinaryCommand(chunk4PairedPlayMove);
                if (chunk4PairedPlayMove.Result != UnitCommand.ResultType.Success || (float)after["mount"]["move"] <= 0f ||
                    (float)after["mount"]["move"] > 3f || (float)after["mount"]["standard"] != 6f ||
                    (float)after["rider"]["standard"] != (float)before["rider"]["standard"] ||
                    (float)after["rider"]["move"] != (float)before["rider"]["move"])
                    throw new InvalidOperationException("Native partner movement altered the exhausted rider or exceeded its own remaining budget.");
                chunk4PairedPlayStage = 8; ResetLeafClock(); return;
            }
            if (chunk4PairedPlayStage == 8)
            {
                if (ReferenceEquals(turn, chunk4PairedPlayTurn))
                {
                    if (!Chunk4PairedPlayIdle) return;
                    chunk4PairedPlaySample["beforeEnd"] = CaptureOrdinaryLiveState();
                    chunk4PairedPlaySample["earlyEnd"] = chunk4PairedPlayActivation == 5;
                    TryEndPhase3gFixtureTurn(turn); return;
                }
                if (turn == null) return;
                if (turn.Unit == rider || turn.Unit == horse || combat.PairedPartnerContext != null)
                    throw new InvalidOperationException("Paired End did not yield cleanly to an unrelated native actor.");
                chunk4PairedPlaySample["nextUnrelatedActor"] = turn.Unit.UniqueId;
                AddRow(Chunk4PairedPlayId, true, "Ordinary native controls preserved independent costs and yielded through explicit End.", chunk4PairedPlaySample);
                chunk4PairedPlayStage = 3; ResetLeafClock();
            }
        }
    }
}
