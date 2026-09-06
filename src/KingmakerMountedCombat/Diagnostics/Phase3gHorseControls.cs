using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic;
using Kingmaker.TurnBasedMode;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // A bounded continuation of the existing Horse fixture, not a second harness.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Phase3gRealTimeScenario = "phase3g-native-controls-rt";
        internal const string Phase3gTurnBasedScenario = "phase3g-native-controls-tb";
        private bool IsPhase3gControls => request.Scenario == Phase3gRealTimeScenario || request.Scenario == Phase3gTurnBasedScenario;
        private bool Phase3gTurnBased => request.Scenario == Phase3gTurnBasedScenario;
        private int phase3gCase;
        private int phase3gStage;
        private double phase3gLastClick;
        private int phase3gClicks;
        private TurnController phase3gAttackTurn;
        private TurnController phase3gEndedTurn;
        private TurnController phase3gEndCandidate;
        private int phase3gEndStableFrame;
        private MountedActionLedgerSnapshot phase3gActorBefore;
        private MountedActionLedgerSnapshot phase3gOtherBefore;
        private long phase3gIntentStarts;
        private Vector3 phase3gMovementStart;

        private string Phase3gRow => "3g-" + new[] {
            "rider-longbow-ordinary", "rider-longbow-primary", "rider-melee-ordinary",
            "rider-melee-primary", "horse-bite-ordinary", "horse-bite-primary" }[phase3gCase];

        private UnitEntityData Phase3gActor => phase3gCase >= 4 && Phase3gTurnBased ? horse : rider;

        private void BeginPhase3gCase()
        {
            if (phase3gCase >= 6) { BeginCleanup(); return; }
            if (settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Phase 3G requires exact shipped C0 configuration.");
            if (phase3gCase == 0)
            {
                rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
                rangedWeaponLease.Acquire(WeaponCategory.Longbow);
            }
            if (phase3gCase == 2 && rangedWeaponLease != null)
            {
                rangedWeaponLease.Dispose();
                rangedWeaponLease = null;
            }
            BeginTarget(!Phase3gTurnBased && phase3gCase == 2 ? 6f : 2.5f, Phase3gRow);
            // Natural hit/miss and projectile resolve events are authoritative.
            ruleProbe.Arm(target, false);
            phase3gStage = 0;
            phase3gClicks = 0;
            phase3gActorBefore = null;
            phase3gAttackTurn = null;
            outcomeBefore = combat.LastOutcome;
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private void TickPhase3gControls()
        {
            observations["phase3gProgress"] = CapturePhase3gProgress();
            if (phase3gStage == 3)
            {
                // Wait for released effects before retiring this isolated target.
                if (combat.HasActiveCommand || ruleProbe.RiderResolvedCount < ruleProbe.RiderNonOpportunityAttackRuleCount ||
                    ruleProbe.MountResolvedCount < ruleProbe.MountNonOpportunityAttackRuleCount) { return; }
                TryLeaveCombat(target);
                if (!targetService.DestroyAndVerify()) { return; }
                targetService.Dispose(); targetService = null; target = null;
                phase3gCase++;
                BeginPhase3gCase();
                return;
            }
            var game = Game.Instance;
            if (phase3gStage == 0)
            {
                if (game.IsPaused) { game.IsPaused = false; return; } // Explicit fixture input, never a production unpause.
                if (!IsCombatReady(true)) { return; }
                if (Phase3gTurnBased && !CombatController.IsInTurnBasedCombat())
                {
                    if (turnBasedModeProbe == null) { turnBasedModeProbe = new NativeModeTransitionProbe(true); }
                    turnBasedModeProbe.DispatchTemporaryValueIfRequired();
                    return;
                }
                var actor = Phase3gActor;
                var turn = game.TurnBasedCombatController?.CurrentTurn;
                if (Phase3gTurnBased && (turn?.Unit != actor ||
                    turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing))
                {
                    TryEndPhase3gFixtureTurn(turn);
                    return;
                }
                if (actor.Commands == null || !actor.Commands.Empty || actor.AreHandsBusyWithAnimation ||
                    !actor.HasStandardAction() || game.HandsEquipmentController.IsUpdateScheduledFor(actor)) { return; }
                SelectionManager.Instance.SelectUnit(actor.View, true, true, false);
                var ledger = combat.CaptureUnifiedTurnSnapshot();
                var mountAction = phase3gCase >= 4;
                phase3gActorBefore = mountAction ? ledger.Mount : ledger.Rider;
                phase3gOtherBefore = mountAction ? ledger.Rider : ledger.Mount;
                phase3gAttackTurn = turn;
                phase3gMovementStart = horse.Position;
                phase3gIntentStarts = combat.StockAttackIntentStartCount;
                if (!targetService.BeginExpectedAttackDispatch(target))
                    throw new InvalidOperationException("Exact Phase 3G fixture attack admission failed.");
                if (!ClickPhase3gAttack()) { CompletePhase3gCase(false, "Native input refused before command admission."); return; }
                phase3gStage = 1;
                phase3gLastClick = clock.Elapsed.TotalSeconds;
                ResetLeafClock();
                return;
            }
            if (phase3gStage != 1) { return; }
            var ordinary = phase3gCase % 2 == 0;
            var resolved = phase3gCase >= 4 ? ruleProbe.MountResolvedCount : ruleProbe.RiderResolvedCount;
            var expected = ordinary && !Phase3gTurnBased && phase3gCase < 4 ? 2 : 1;
            if (ordinary && !Phase3gTurnBased && clock.Elapsed.TotalSeconds - phase3gLastClick >= 0.35 && resolved < expected)
            {
                ClickPhase3gAttack();
                phase3gLastClick = clock.Elapsed.TotalSeconds;
            }
            var outcome = combat.LastOutcome;
            if (outcome != null && !ReferenceEquals(outcome, outcomeBefore) && outcome.Result != "Success" && resolved == 0)
            {
                CompletePhase3gCase(false, "Admitted command ended before a resolved native attack: " + outcome.Result);
                return;
            }
            if (resolved < expected || outcome == null || ReferenceEquals(outcome, outcomeBefore)) { return; }
            var expectedActor = phase3gCase >= 4 ? horse : rider;
            if (outcome.ActorId != expectedActor.UniqueId || outcome.Result != "Success") { return; }
            var actorAfter = expectedActor.CombatState.Cooldown.StandardAction;
            var otherAfter = (expectedActor == horse ? rider : horse).CombatState.Cooldown.StandardAction;
            var nativeTurn = game.TurnBasedCombatController?.CurrentTurn;
            var success = relationship.State == RelationshipState.Mounted && relationship.Runtime.PoseHealthy &&
                outcome.ChildAttackStartCount == 1 && ruleProbe.PairForcedD20Count == 0 &&
                (!Phase3gTurnBased || ReferenceEquals(nativeTurn, phase3gAttackTurn) && nativeTurn.Unit == expectedActor &&
                    actorAfter > phase3gActorBefore.Standard && Math.Abs(otherAfter - phase3gOtherBefore.Standard) < 0.001f &&
                    (expectedActor == horse ? ruleProbe.RiderNonOpportunityAttackRuleCount : ruleProbe.MountNonOpportunityAttackRuleCount) == 0) &&
                (!ordinary || combat.StockAttackIntentStartCount - phase3gIntentStarts == 1);
            CompletePhase3gCase(success, "Native actor command, attack rule and effect resolved; inspect exact turn/cost/cadence evidence. Visual animation remains human review.");
        }

        private bool ClickPhase3gAttack()
        {
            phase3gClicks++;
            if (phase3gCase % 2 == 0)
                return new ClickUnitHandler().OnClick(target.View.gameObject, target.Position, 0, false, false);
            var actor = Phase3gActor;
            var blueprint = phase3gCase >= 4 ? nativeControls.MountPrimaryAbility : nativeControls.RiderPrimaryAbility;
            var data = actor.Descriptor.Abilities.GetAbility(blueprint)?.Data;
            if (data == null) { return false; }
            Game.Instance.SelectedAbilityHandler.SetAbility(data);
            return Game.Instance.SelectedAbilityHandler.OnClick(target.View.gameObject, target.Position, 0, false, false);
        }

        private void TryEndPhase3gFixtureTurn(TurnController turn)
        {
            var unit = turn?.Unit;
            if (unit == null || ReferenceEquals(turn, phase3gEndedTurn) || !unit.IsDirectlyControllable ||
                turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing || !unit.Commands.Empty ||
                unit.AreHandsBusyWithAnimation || combat.HasActiveCommand || combat.HasStockAttackIntent ||
                Game.Instance.IsPaused || Game.Instance.TurnBasedCombatController.WaitingForUI ||
                GetPendingNextUnit(Game.Instance.TurnBasedCombatController) != null)
            { phase3gEndCandidate = null; return; }
            var exactPair = unit == rider || unit == horse;
            var fixtureParty = targetService?.NonPairPartyAiLease;
            if (!exactPair && !(fixtureParty != null && fixtureParty.OwnsExactMember(unit) && fixtureParty.ValidateActive()))
                throw new InvalidOperationException("Phase 3G refused to end a foreign native turn.");
            if (Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(unit)) { return; }
            if (!ReferenceEquals(phase3gEndCandidate, turn))
            { phase3gEndCandidate = turn; phase3gEndStableFrame = Time.frameCount; return; }
            if (Time.frameCount <= phase3gEndStableFrame) { return; }
            phase3gEndedTurn = turn;
            turn.ForceToEnd(false); // Native end-turn input for an exact idle disposable fixture actor.
        }

        private JObject CapturePhase3gProgress()
        {
            var turn = Game.Instance?.TurnBasedCombatController?.CurrentTurn;
            return new JObject {
                ["case"] = Phase3gRow, ["stage"] = phase3gStage, ["clicks"] = phase3gClicks,
                ["turnActor"] = turn?.Unit?.UniqueId, ["turnStatus"] = turn?.Status.ToString(),
                ["selected"] = new JArray(SelectionManager.Instance.SelectedUnits.Select(u => u.UniqueId)),
                ["ledger"] = JObject.FromObject(combat.CaptureUnifiedTurnSnapshot(), JsonSerializer.Create(JsonSettings)),
                ["rules"] = ruleProbe.CapturePairEvidence(),
                ["feedback"] = combat.LastFeedback,
                ["lastOutcome"] = combat.LastOutcome == null ? JValue.CreateNull() : JToken.FromObject(combat.LastOutcome, JsonSerializer.Create(JsonSettings)),
                ["intentStarts"] = combat.StockAttackIntentStartCount - phase3gIntentStarts,
                ["actorBefore"] = phase3gActorBefore == null ? JValue.CreateNull() : JToken.FromObject(phase3gActorBefore),
                ["otherBefore"] = phase3gOtherBefore == null ? JValue.CreateNull() : JToken.FromObject(phase3gOtherBefore),
                ["mountDisplacement"] = HorizontalDistance(phase3gMovementStart, horse.Position),
                ["inputKind"] = "scripted-native-handler-integration"
            };
        }

        private void CompletePhase3gCase(bool success, string detail)
        {
            AddRow(Phase3gRow, success, detail, CapturePhase3gProgress());
            combat.Cancel("Phase 3G native Stop after case");
            phase3gStage = 3;
            ResetLeafClock();
        }
    }
}
