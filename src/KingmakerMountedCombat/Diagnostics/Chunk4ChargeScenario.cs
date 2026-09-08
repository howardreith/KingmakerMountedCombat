using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
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
        internal static bool IsChunk4ChargeScenario(string scenario) =>
            scenario == "chunk4-charge-safety-rt" || scenario == "chunk4-charge-safety-tb";
        private bool IsChunk4Charge => IsChunk4ChargeScenario(request.Scenario);
        private bool Chunk4ChargeTb => request.Scenario.EndsWith("-tb", StringComparison.Ordinal);
        private bool Chunk4ChargeMounted => chunk4ChargeCase == 0;
        private string Chunk4ChargeId => Chunk4ChargeMounted ? "C4-CHARGE-mounted-rider" : "C4-CHARGE-unmounted-rider";
        private int chunk4ChargeCase;
        private int chunk4ChargeStage;
        private bool chunk4ChargeControlSent;
        private AbilityData chunk4ChargeAbility;
        private UnitUseAbility chunk4ChargeCommand;
        private JObject chunk4ChargeBefore;
        private readonly JArray chunk4ChargeSamples = new JArray();
        private double chunk4ChargeStarted;
        private double chunk4ChargeLastSample;
        private Vector3 chunk4ChargeRiderOrigin;
        private Vector3 chunk4ChargeMountOrigin;
        private float chunk4ChargeRiderDistance;
        private float chunk4ChargeMountDistance;
        private float chunk4ChargeMaxStandard;
        private float chunk4ChargeMaxMove;
        private bool chunk4ChargeObservedCharging;
        private bool chunk4ChargeClicked;
        private bool chunk4ChargeHoverPure;

        private void BeginChunk4Charge()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Chunk 4 requires the accepted paired configuration.");
            CaptureIdleFixturePartyForCleanup();
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat);
            observations["ordinaryTrace"] = new JObject();
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private void TickChunk4Charge()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            observations["chunk4ChargeProgress"] = new JObject {
                ["case"] = Chunk4ChargeId, ["stage"] = chunk4ChargeStage,
                ["rider"] = CaptureOrdinaryActor(rider), ["mount"] = CaptureOrdinaryActor(horse),
                ["turn"] = turn?.Unit?.UniqueId, ["status"] = turn?.Status.ToString(),
                ["samples"] = chunk4ChargeSamples, ["shell"] = CaptureNativeAbilityShell(chunk4ChargeCommand)
            };
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (chunk4ChargeStage == 0)
            {
                if (chunk4ChargeCase == 2) { observations["ordinaryTrace"] = ordinaryAttackTrace.Capture(); BeginCleanup(); return; }
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if ((relationship.State == RelationshipState.Mounted) != Chunk4ChargeMounted)
                {
                    if (!chunk4ChargeControlSent)
                        chunk4ChargeControlSent = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider,
                            "chunk4-charge-control-dismount");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() ||
                    !PrepareCombatMountRiderAiIsolation()) return;
                var matches = rider.Descriptor.Abilities.Enumerable.Where(fact =>
                    fact.Blueprint.GetComponent<AbilityCustomCharge>()?.GetType() == typeof(AbilityCustomCharge)).ToArray();
                observations["chargeIdentity"] = new JArray(matches.Select(fact => new JObject {
                    ["blueprint"] = fact.Blueprint.AssetGuid, ["assetName"] = fact.Blueprint.name,
                    ["logic"] = typeof(AbilityCustomCharge).FullName,
                    ["assemblyMvid"] = typeof(AbilityCustomCharge).Assembly.ManifestModule.ModuleVersionId.ToString(),
                    ["actionType"] = fact.Blueprint.ActionType.ToString()
                }));
                if (matches.Length != 1) throw new InvalidOperationException("Expected one exact native Charge logic in rider abilities.");
                chunk4ChargeAbility = matches[0].Data;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(Chunk4ChargeTb);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                BeginTarget(9f, Chunk4ChargeId);
                ruleProbe.Arm(target, false);
                ordinaryAttackTrace.BeginCase(Chunk4ChargeId);
                chunk4ChargeStage = 1;
                return;
            }
            if (chunk4ChargeStage == 1)
            {
                if (!IsCombatReady(Chunk4ChargeMounted)) return;
                if (Chunk4ChargeTb)
                {
                    if (Chunk4ChargeMounted && turn?.Unit == horse)
                        throw new InvalidOperationException("Independent mount turn in paired Charge fixture.");
                    if (turn?.Unit != rider || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing)
                    { TryEndPhase3gFixtureTurn(turn); return; }
                }
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation ||
                    !rider.CombatState.Prepared || !rider.CombatState.CanActInCombat ||
                    rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f) return;
                var nativeTarget = new TargetWrapper(target);
                chunk4ChargeBefore = new JObject {
                    ["rider"] = CaptureOrdinaryActor(rider), ["mount"] = CaptureOrdinaryActor(horse),
                    ["nativeCanTarget"] = chunk4ChargeAbility.CanTarget(nativeTarget),
                    ["nativeAvailable"] = chunk4ChargeAbility.IsAvailableForCast,
                    ["nativeReason"] = chunk4ChargeAbility.GetUnavailableReason(),
                    ["relationship"] = relationship.State.ToString(), ["turn"] = turn?.Unit?.UniqueId
                };
                observations["before-" + Chunk4ChargeId] = chunk4ChargeBefore;
                // Hover through the actual selected-ability handler while paused.
                // No command or native budget is fabricated by this fixture.
                var handler = game.SelectedAbilityHandler;
                game.IsPaused = true;
                handler.SetAbility(chunk4ChargeAbility);
                var beforeHover = CaptureOrdinaryLiveState();
                for (var index = 0; index < 3; index++)
                {
                    handler.GetPriority(target.View.gameObject, target.Position);
                    handler.GetTarget(target.View.gameObject, target.Position, chunk4ChargeAbility);
                }
                chunk4ChargeHoverPure = JToken.DeepEquals(beforeHover, CaptureOrdinaryLiveState());
                chunk4ChargeClicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false);
                chunk4ChargeCommand = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                    .FirstOrDefault(command => ReferenceEquals(command.Spell, chunk4ChargeAbility));
                observations["paused-" + Chunk4ChargeId] = new JObject {
                    ["before"] = beforeHover, ["after"] = CaptureOrdinaryLiveState(),
                    ["shell"] = CaptureNativeAbilityShell(chunk4ChargeCommand), ["clicked"] = chunk4ChargeClicked
                };
                chunk4ChargeRiderOrigin = rider.Position;
                chunk4ChargeMountOrigin = horse.Position;
                chunk4ChargeStarted = game.TimeController.GameTime.TotalSeconds;
                chunk4ChargeLastSample = -1;
                chunk4ChargeRiderDistance = chunk4ChargeMountDistance = 0;
                chunk4ChargeMaxStandard = chunk4ChargeMaxMove = 0;
                chunk4ChargeObservedCharging = false;
                chunk4ChargeSamples.Clear();
                chunk4ChargeStage = 2;
                ResetLeafClock();
                return;
            }
            if (chunk4ChargeStage == 2)
            {
                var elapsed = game.TimeController.GameTime.TotalSeconds - chunk4ChargeStarted;
                chunk4ChargeRiderDistance = Math.Max(chunk4ChargeRiderDistance, HorizontalDistance(rider.Position, chunk4ChargeRiderOrigin));
                chunk4ChargeMountDistance = Math.Max(chunk4ChargeMountDistance, HorizontalDistance(horse.Position, chunk4ChargeMountOrigin));
                chunk4ChargeMaxStandard = Math.Max(chunk4ChargeMaxStandard, rider.CombatState.Cooldown.StandardAction);
                chunk4ChargeMaxMove = Math.Max(chunk4ChargeMaxMove, rider.CombatState.Cooldown.MoveAction);
                chunk4ChargeObservedCharging |= rider.Descriptor.State.IsCharging || rider.View.AgentASP.IsCharging;
                if (elapsed - chunk4ChargeLastSample >= 0.1)
                {
                    chunk4ChargeLastSample = elapsed;
                    chunk4ChargeSamples.Add(new JObject {
                        ["nativeSeconds"] = elapsed, ["rider"] = CaptureOrdinaryActor(rider),
                        ["mount"] = CaptureOrdinaryActor(horse), ["charging"] = rider.Descriptor.State.IsCharging,
                        ["shell"] = CaptureNativeAbilityShell(chunk4ChargeCommand),
                        ["attack"] = CaptureOrdinaryCommand(ordinaryAttackTrace.LastStartedRiderAttack),
                        ["chargeAttack"] = ordinaryAttackTrace.LastStartedRiderAttack?.IsCharge
                    });
                }
                if (leafClock.Elapsed.TotalSeconds < 12 && (chunk4ChargeCommand != null && !chunk4ChargeCommand.IsFinished ||
                    !Chunk4ChargeMounted && ruleProbe.RiderResolvedCount == 0)) return;
                if (leafClock.Elapsed.TotalSeconds < 1) return;
                var rejected = chunk4ChargeCommand == null || !chunk4ChargeCommand.IsActed && chunk4ChargeCommand.IsFinished;
                var safe = rejected && !chunk4ChargeObservedCharging && chunk4ChargeRiderDistance < 0.01f &&
                    chunk4ChargeMountDistance < 0.01f && chunk4ChargeMaxStandard < 0.001f && chunk4ChargeMaxMove < 0.001f &&
                    ruleProbe.RiderNonOpportunityAttackRuleCount == 0;
                var nativeCharge = ruleProbe.RiderResolvedCount > 0 && ordinaryAttackTrace.LastStartedRiderAttack?.IsCharge == true &&
                    chunk4ChargeRiderDistance > 1f && chunk4ChargeMaxStandard > 0;
                AddRow(Chunk4ChargeId, chunk4ChargeHoverPure && (Chunk4ChargeMounted ? safe : nativeCharge),
                    Chunk4ChargeMounted ? "Mounted native Charge must reject before approach, delivery or costs." :
                        "Unmounted native Charge retains movement, charge attack and genuine cost.", new JObject {
                    ["level"] = "NATIVE INTEGRATION", ["inputKind"] = "scripted-native-handler-integration",
                    ["mode"] = Chunk4ChargeTb ? "TB" : "RT", ["mounted"] = Chunk4ChargeMounted,
                    ["identity"] = observations["chargeIdentity"].DeepClone(), ["before"] = chunk4ChargeBefore,
                    ["clicked"] = chunk4ChargeClicked, ["hoverPure"] = chunk4ChargeHoverPure,
                    ["safeRejected"] = safe, ["nativeChargeCompleted"] = nativeCharge,
                    ["riderDistance"] = chunk4ChargeRiderDistance, ["mountDistance"] = chunk4ChargeMountDistance,
                    ["maximumRiderStandard"] = chunk4ChargeMaxStandard, ["maximumRiderMove"] = chunk4ChargeMaxMove,
                    ["observedCharging"] = chunk4ChargeObservedCharging, ["samples"] = chunk4ChargeSamples.DeepClone(),
                    ["rules"] = ruleProbe.CapturePairEvidence(), ["after"] = CaptureOrdinaryLiveState()
                });
                SelectionManager.Instance.Stop();
                chunk4ChargeStage = 3;
                ResetLeafClock();
                return;
            }
            if (chunk4ChargeStage == 3)
            {
                if (combat.HasActiveCommand || ruleProbe.RiderResolvedCount < ruleProbe.RiderNonOpportunityAttackRuleCount) return;
                TryLeaveCombat(target); TryLeaveCombat(rider); TryLeaveCombat(horse);
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) return;
                    targetService.Dispose(); targetService = null; target = null;
                }
                if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                if (CombatController.IsInTurnBasedCombat() || controller.Initialized || game.Player.IsInCombat ||
                    !rider.Commands.Empty || !horse.Commands.Empty) return;
                if (!RestoreCombatMountRiderAiIsolation() || !RestoreUnmountedHorseAiIsolation())
                    throw new InvalidOperationException("Charge fixture AI restoration failed.");
                combatMountRiderAiLease = null; unmountedHorseAiLease = null; unmountedHorseAiSettleRequested = false;
                chunk4ChargeCase++; chunk4ChargeStage = 0; chunk4ChargeControlSent = false; chunk4ChargeCommand = null;
                ResetLeafClock();
            }
        }
    }
}
