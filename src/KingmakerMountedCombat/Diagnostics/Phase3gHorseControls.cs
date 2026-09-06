using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Commands;
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
        private bool IsPhase3hLoop => request.Scenario == "phase3h-combat-loop-rt" || request.Scenario == "phase3h-combat-loop-tb";
        private bool IsPhase3gControls => IsPhase3hLoop || request.Scenario == Phase3gRealTimeScenario || request.Scenario == Phase3gTurnBasedScenario;
        private bool Phase3gTurnBased => request.Scenario == Phase3gTurnBasedScenario || request.Scenario == "phase3h-combat-loop-tb";
        private Feature phase3hRapidShot;
        private ActivatableAbility phase3hRapidToggle;
        private bool phase3hRapidWasOn;
        private float phase3hStationaryRadius;
        private bool Phase3hApproachCase => IsPhase3hLoop && (phase3gCase == 2 || phase3gCase == 4 || !Phase3gTurnBased && phase3gCase == 5);
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
        private int phase3gPauseStage;
        private int phase3gPauseFrame;
        private long phase3gControlDispatches;
        private bool phase3gQueuedWithoutExecution;
        private UnitUseAbility phase3gQueuedControl;
        private int phase3gUnpauseInputs;

        private string Phase3gRow => (IsPhase3hLoop ? "3h-" : "3g-") + new[] {
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
                if (IsPhase3hLoop) AcquirePhase3hRapidShot();
            }
            if (phase3gCase == 2 && rangedWeaponLease != null)
            {
                rangedWeaponLease.Dispose();
                rangedWeaponLease = null;
                RestorePhase3hRapidShot();
            }
            if (Phase3gTurnBased && phase3gCase == 1)
            {
                // Complement case 0's mounted RT -> TB entry with an encounter
                // that starts while the native TB option is already enabled.
                turnBasedModeProbe = new NativeModeTransitionProbe(true);
                turnBasedModeProbe.DispatchTemporaryValueIfRequired();
            }
            // Preserve the target service's 3-metre minimum spawn boundary. Its native
            // Mammoth body has substantial corpulence; stationary admission below
            // still has to satisfy the exact actor's native range and LoS checks.
            BeginTarget(Phase3hApproachCase || !Phase3gTurnBased && phase3gCase == 2 ? 6f : 3.5f, Phase3gRow);
            // Natural hit/miss and projectile resolve events are authoritative.
            ruleProbe.Arm(target, false);
            phase3gStage = Phase3gTurnBased && phase3gCase >= 2 && !Phase3hApproachCase ? -1 : 0;
            phase3gClicks = 0;
            phase3gUnpauseInputs = 0;
            phase3gActorBefore = null;
            phase3gAttackTurn = null;
            outcomeBefore = combat.LastOutcome;
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private void TickPhase3gControls()
        {
            if (!Phase3gTurnBased && phase3gPauseStage != 8) { TickPhase3gPausedControls(); return; }
            observations["phase3gProgress"] = CapturePhase3gProgress();
            if (phase3gStage != 3 && Game.Instance.IsPaused)
            {
                // The attack/positioning fixture supplies explicit native unpause input
                // after native auto-pause. The paused-control cases above retain their
                // separate queue-before-unpause assertions. No production code unpauses.
                observations["phase3gLastUnpauseInput"] = CapturePhase3gProgress();
                phase3gUnpauseInputs++;
                Game.Instance.IsPaused = false;
                return;
            }
            if (phase3gStage == 3)
            {
                // Wait for released effects before retiring this isolated target.
                if (combat.HasActiveCommand || ruleProbe.RiderResolvedCount < ruleProbe.RiderNonOpportunityAttackRuleCount ||
                    ruleProbe.MountResolvedCount < ruleProbe.MountNonOpportunityAttackRuleCount) { return; }
                TryLeaveCombat(target);
                TryLeaveCombat(rider);
                TryLeaveCombat(horse);
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) { return; }
                    targetService.Dispose(); targetService = null; target = null;
                }
                if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                if (CombatController.IsInTurnBasedCombat() || Game.Instance.TurnBasedCombatController.Initialized ||
                    Game.Instance.Player.IsInCombat || !rider.Commands.Empty || !horse.Commands.Empty) { return; }
                phase3gCase++;
                BeginPhase3gCase();
                return;
            }
            var game = Game.Instance;
            if (phase3gStage == -1)
            {
                if (game.IsPaused) { game.IsPaused = false; return; }
                if (!IsCombatReady(true)) { return; }
                if (CombatController.IsInTurnBasedCombat())
                    throw new InvalidOperationException("Stationary TB setup must finish its native approach in RT first.");
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var probe = new MountedPairSingleAttack(target, rider, horse, phase3gCase < 4);
                probe.Init(Phase3gActor);
                phase3hStationaryRadius = probe.PairApproachRadius;
                movementDestination = FindWalkablePointNearTarget(target.Position, horse.Position,
                    IsPhase3hLoop ? phase3hStationaryRadius - MountedCombatSpatialPolicy.DiagnosticRangeInset : 2.5f);
                ClickGroundHandler.MoveSelectedUnitsToPoint(movementDestination, false);
                movementCommand = horse.Commands.Move as UnitMoveTo;
                if (movementCommand == null || movementCommand.Executor != horse)
                    throw new InvalidOperationException("Stationary fixture approach did not acquire the native Horse move.");
                phase3gStage = -2; ResetLeafClock(); return;
            }
            if (phase3gStage == -2)
            {
                if (!movementCommand.IsFinished || horse.View.MovementAgent.IsReallyMoving) { return; }
                if (horse.DistanceTo(target) > (IsPhase3hLoop ? phase3hStationaryRadius : 2.75f))
                    throw new InvalidOperationException("Native fixture approach did not reach adjacent stationary placement.");
                movementCommand = null;
                phase3gStage = 0; ResetLeafClock(); return;
            }
            if (phase3gStage == 0)
            {
                if (game.IsPaused) { game.IsPaused = false; return; } // Explicit fixture input, never a production unpause.
                if (!IsCombatReady(true)) { return; }
                if (Phase3gTurnBased && !CombatController.IsInTurnBasedCombat())
                {
                    if (turnBasedModeProbe == null) { turnBasedModeProbe = new NativeModeTransitionProbe(true); }
                    if (!turnBasedModeProbe.TemporaryDeliveryAttempted)
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
                if (phase3gCase >= 4 && (!horse.Commands.Empty || horse.AreHandsBusyWithAnimation ||
                    !horse.HasStandardAction())) { return; }
                SelectionManager.Instance.SelectUnit(actor.View, true, true, false);
                if (IsPhase3hLoop) Game.Instance.UI.GetCameraRig().ScrollTo(horse.Position);
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
            var expected = IsPhase3hLoop && phase3gCase == 0 ? 2 : ordinary && !Phase3gTurnBased && phase3gCase < 4 ? 2 : 1;
            if (ordinary && (!Phase3gTurnBased || IsPhase3hLoop) && clock.Elapsed.TotalSeconds - phase3gLastClick >= 0.35 && resolved < expected)
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
                    (Phase3hApproachCase ? HorizontalDistance(phase3gMovementStart, horse.Position) >= 0.5f :
                        HorizontalDistance(phase3gMovementStart, horse.Position) <= 0.1f) &&
                    (expectedActor == horse ? ruleProbe.RiderNonOpportunityAttackRuleCount : ruleProbe.MountNonOpportunityAttackRuleCount) == 0) &&
                (!ordinary || combat.StockAttackIntentStartCount - phase3gIntentStarts == 1);
            if (IsPhase3hLoop && phase3gCase == 0)
                success &= outcome.NativeFullAttack && outcome.NativePlannedAttackCount >= 2 &&
                    outcome.NativeCompletedAttackCount == outcome.NativePlannedAttackCount &&
                    (!Phase3gTurnBased || rider.CombatState.Cooldown.MoveAction >= 3f);
            if (IsPhase3hLoop && !ordinary)
                success &= outcome.SingleAttackMode && outcome.NativePlannedAttackCount == 1;
            CompletePhase3gCase(success, "Native actor command, attack rule and effect resolved; inspect exact turn/cost/cadence evidence. Visual animation remains human review.");
        }

        private void TickPhase3gPausedControls()
        {
            var game = Game.Instance;
            if (phase3gPauseStage == 0 || phase3gPauseStage == 3 || phase3gPauseStage == 6)
            {
                if (!rider.Commands.Empty || rider.AreHandsBusyWithAnimation) { return; }
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                game.IsPaused = true;
                phase3gControlDispatches = nativeControls.DispatchAcceptedCount;
                var dismount = phase3gPauseStage == 0;
                if (!TryNativeAbilityTargetClick(dismount ? nativeControls.DismountAbility : nativeControls.MountAbility,
                    dismount ? rider : horse, "phase3gPauseSubmission"))
                { FailPhase3gPause("Native paused control submission refused."); return; }
                phase3gQueuedControl = lastNativeAbilityShell;
                phase3gPauseFrame = Time.frameCount;
                phase3gPauseStage++;
                ResetLeafClock(); return;
            }
            if (phase3gPauseStage == 1 || phase3gPauseStage == 4 || phase3gPauseStage == 7)
            {
                if (Time.frameCount <= phase3gPauseFrame + 2) { return; }
                phase3gQueuedWithoutExecution = game.IsPaused && phase3gQueuedControl != null &&
                    phase3gQueuedControl.Executor == rider && !phase3gQueuedControl.IsStarted && !phase3gQueuedControl.IsActed &&
                    !phase3gQueuedControl.IsFinished && nativeControls.DispatchAcceptedCount == phase3gControlDispatches &&
                    relationship.State == (phase3gPauseStage == 1 ? RelationshipState.Mounted : RelationshipState.Unmounted);
                if (!phase3gQueuedWithoutExecution) { FailPhase3gPause("Paused native control executed early or lost its exact queue identity."); return; }
                if (phase3gPauseStage == 4) { SelectionManager.Instance.Stop(); }
                game.IsPaused = false;
                phase3gPauseStage = phase3gPauseStage == 7 ? 9 : phase3gPauseStage + 1;
                ResetLeafClock(); return;
            }
            var stopping = phase3gPauseStage == 5;
            var mounting = phase3gPauseStage == 9;
            var expectedState = mounting ? RelationshipState.Mounted : RelationshipState.Unmounted;
            if (phase3gQueuedControl == null || !phase3gQueuedControl.IsFinished || relationship.State != expectedState) { return; }
            var delta = nativeControls.DispatchAcceptedCount - phase3gControlDispatches;
            var success = phase3gQueuedWithoutExecution && delta == (stopping ? 0 : 1) &&
                phase3gQueuedControl.IsActed == !stopping &&
                phase3gQueuedControl.Result == (stopping ? UnitCommand.ResultType.Interrupt : UnitCommand.ResultType.Success) &&
                (!mounting || relationship.Rider == rider && relationship.Mount == horse && relationship.Runtime.PoseHealthy);
            var row = stopping ? "3g-paused-mount-stop" : mounting ? "3g-paused-mount-execute" : "3g-paused-dismount";
            if (IsPhase3hLoop) row = row.Replace("3g-", "3h-");
            AddRow(row, success, "Native paused queue, unpause/Stop input and exact execution outcome.", new JObject {
                ["inputKind"] = "scripted-native-handler-integration", ["queuedBeforeExecution"] = phase3gQueuedWithoutExecution,
                ["dispatchDelta"] = delta, ["finished"] = phase3gQueuedControl.IsFinished,
                ["acted"] = phase3gQueuedControl.IsActed, ["result"] = phase3gQueuedControl.Result.ToString(),
                ["relationshipState"] = relationship.State.ToString()
            });
            if (!success) { BeginCleanup(); return; }
            if (mounting) { phase3gPauseStage = 8; BeginPhase3gCase(); }
            else { phase3gPauseStage++; ResetLeafClock(); }
        }

        private void FailPhase3gPause(string detail)
        {
            AddRow((IsPhase3hLoop ? "3h-" : "3g-") + "paused-control-failure", false, detail, new JObject {
                ["stage"] = phase3gPauseStage, ["feedback"] = playerAction.LastFeedback,
                ["relationshipState"] = relationship.State.ToString(), ["paused"] = Game.Instance.IsPaused
            });
            Game.Instance.IsPaused = false;
            BeginCleanup();
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
                ["actorBefore"] = phase3gActorBefore == null ? JValue.CreateNull() : JToken.FromObject(phase3gActorBefore, JsonSerializer.Create(JsonSettings)),
                ["otherBefore"] = phase3gOtherBefore == null ? JValue.CreateNull() : JToken.FromObject(phase3gOtherBefore, JsonSerializer.Create(JsonSettings)),
                ["mountDisplacement"] = HorizontalDistance(phase3gMovementStart, horse.Position),
                ["paused"] = Game.Instance.IsPaused, ["mode"] = Game.Instance.CurrentMode.ToString(),
                ["gameDeltaTime"] = Game.Instance.TimeController.GameDeltaTime,
                ["unpauseInputs"] = phase3gUnpauseInputs,
                ["moveStarted"] = movementCommand?.IsStarted, ["moveFinished"] = movementCommand?.IsFinished,
                ["moveResult"] = movementCommand?.Result.ToString(), ["moveEnoughClose"] = movementCommand?.IsUnitEnoughClose,
                ["mountAgentMoving"] = horse.View.MovementAgent.IsReallyMoving,
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

        private void AcquirePhase3hRapidShot()
        {
            var feature = ResourcesLibrary.LibraryObject.BlueprintsByAssetId.Values.OfType<BlueprintFeature>()
                .Single(item => item.name == "RapidShot");
            if (rider.Descriptor.HasFact(feature)) throw new InvalidOperationException("Rapid Shot fixture requires an initially unowned feature.");
            phase3hRapidShot = rider.Descriptor.Progression.Features.AddFeature(feature);
            phase3hRapidToggle = rider.ActivatableAbilities.Enumerable.Single(item => item.Blueprint.name == "RapidShotToggleAbility");
            phase3hRapidWasOn = phase3hRapidToggle.IsOn;
            phase3hRapidToggle.IsOn = true;
            observations["phase3hRapidShot"] = new JObject { ["feature"] = feature.AssetGuid,
                ["toggle"] = phase3hRapidToggle.Blueprint.AssetGuid, ["nativeToggleEnabled"] = phase3hRapidToggle.IsOn };
        }

        private void RestorePhase3hRapidShot()
        {
            if (phase3hRapidToggle != null) { phase3hRapidToggle.IsOn = phase3hRapidWasOn; phase3hRapidToggle = null; }
            if (phase3hRapidShot != null) { rider.Descriptor.Progression.Features.RemoveFact(phase3hRapidShot); phase3hRapidShot = null; }
        }
    }
}
