using System;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private readonly JObject chunk4ChargeTransition = new JObject();
        private int chunk4ChargeTransitionStage;
        private UnitUseAbility chunk4QueuedMount;
        private UnitUseAbility chunk4QueuedCharge;
        private bool chunk4QueuedCharging;
        private bool chunk4CombatBeforeMount;

        private bool PrepareChunk4ChargeActor()
        {
            var actor = chunk4ChargeCase == 2 ? horse : rider;
            if (chunk4ChargeCase == 3)
            {
                actor = Game.Instance.Player.PartyCharacters.Select(reference => reference.Value)
                    .FirstOrDefault(unit => unit != null && unit != rider && unit != horse &&
                        unit.IsDirectlyControllable && unit.Descriptor.State.IsConscious &&
                        unit.GetFirstWeapon()?.Blueprint.IsRanged == false &&
                        unit.Descriptor.Abilities.Enumerable.Any(fact =>
                            fact.Blueprint.AssetGuid == MountedChargeSafetyPolicy.ChargeBlueprintId));
                if (actor == null) throw new InvalidOperationException("No eligible unrelated native Charge actor in disposable fixture.");
            }
            if (actor != chunk4ChargeActor)
            {
                if (!actor.Commands.Empty || actor.AreHandsBusyWithAnimation) return false;
                observations["chargeTraceBefore-" + Chunk4ChargeId] = ordinaryAttackTrace.Capture();
                ordinaryAttackTrace.Dispose();
                ruleProbe.Dispose();
                // The probe labels its first actor 'rider'; actorId in each row
                // preserves the actual caster, while pair budgets stay separate.
                ordinaryAttackTrace = new NativeOrdinaryAttackTrace(actor == horse ? rider : actor, horse, combat,
                    () => relationship.State.ToString());
                ruleProbe = new Phase3dCombatRuleProbe(actor == horse ? rider : actor, horse);
                chunk4ChargeActor = actor;
            }
            return true;
        }

        private void TickChunk4QueuedChargeTransition()
        {
            var game = Game.Instance;
            var turn = game.TurnBasedCombatController.CurrentTurn;
            observations["queuedChargeTransition"] = chunk4ChargeTransition;
            chunk4ChargeTransition["stage"] = chunk4ChargeTransitionStage;
            chunk4ChargeTransition["current"] = CaptureOrdinaryLiveState();
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (chunk4ChargeTransitionStage == 0)
            {
                if (!PrepareChunk4ChargeActor() || !rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State == RelationshipState.Mounted)
                {
                    if (!chunk4ChargeControlSent)
                        chunk4ChargeControlSent = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider,
                            "queued-charge-dismount");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || game.Player.IsInCombat ||
                    !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (!PrepareChunk4UnmountedChargeOrigin()) return;
                chunk4ChargeAbility = rider.Descriptor.Abilities.Enumerable.Single(fact =>
                    fact.Blueprint.AssetGuid == MountedChargeSafetyPolicy.ChargeBlueprintId &&
                    fact.Blueprint.GetComponent<AbilityCustomCharge>()?.GetType() == typeof(AbilityCustomCharge)).Data;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(Chunk4ChargeTb);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }

                // Delay combat-memory admission until the real Mount has completed.
                // The native queue entry is initialized by the native queue API.
                // No relationship flag, queue node or cooldown is written here.
                game.IsPaused = true;
                targetService = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
                target = targetService.Spawn(rider, horse, FindChunk4ChargeTargetPoint(),
                    request.RunId + "-queued-charge-transition", true, true);
                if (!targetService.PrepareForPlayerClick(target))
                    throw new InvalidOperationException("Queued Charge target visibility lease failed.");
                var available = chunk4ChargeAbility.IsAvailableForCast;
                var canTarget = chunk4ChargeAbility.CanTarget(new Kingmaker.Utility.TargetWrapper(target));
                chunk4ChargeTransition["before"] = CaptureOrdinaryLiveState();
                chunk4ChargeTransition["availableWhileUnmounted"] = available;
                chunk4ChargeTransition["canTargetWhileUnmounted"] = canTarget;
                if (!available || !canTarget || rider.IsInCombat || horse.IsInCombat)
                    throw new InvalidOperationException("Queued transition must start with a legal unmounted Charge before combat.");
                if (!TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "queued-charge-mount"))
                    throw new InvalidOperationException("Queued transition native Mount input refused.");
                chunk4QueuedMount = lastNativeAbilityShell;
                if (chunk4QueuedMount == null || chunk4QueuedMount.IsActed || relationship.State != RelationshipState.Unmounted)
                    throw new InvalidOperationException("Queued transition missed the genuine pre-Mount command window.");
                var beforeQueue = CaptureOrdinaryActor(rider);
                chunk4QueuedCharge = new UnitUseAbility(chunk4ChargeAbility, new Kingmaker.Utility.TargetWrapper(target));
                var queue = typeof(UnitCommands).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(method => method.Name == "AddToQueueInternal" && method.MetadataToken == 0x060026B8);
                queue.Invoke(rider.Commands, new object[] { chunk4QueuedCharge, false });
                chunk4ChargeTransition["queuedWhileUnmounted"] = rider.Commands.Queue.Contains(chunk4QueuedCharge) &&
                    chunk4QueuedCharge.Executor == rider && !chunk4QueuedCharge.IsStarted && !chunk4QueuedCharge.IsActed;
                chunk4ChargeTransition["nativeQueueApi"] = "UnitCommands.AddToQueueInternal060026B8";
                chunk4ChargeTransition["beforeQueue"] = beforeQueue;
                chunk4ChargeTransition["afterQueue"] = CaptureOrdinaryActor(rider);
                chunk4ChargeTransition["pausedQueueCostsPure"] =
                    (float)beforeQueue["standard"] == rider.CombatState.Cooldown.StandardAction &&
                    (float)beforeQueue["move"] == rider.CombatState.Cooldown.MoveAction;
                if (!(bool)chunk4ChargeTransition["queuedWhileUnmounted"] || !(bool)chunk4ChargeTransition["pausedQueueCostsPure"])
                    throw new InvalidOperationException("Native unmounted Charge did not queue without expenditure.");
                chunk4ChargeWarningStart = chunk4ChargeWarnings.Count;
                ruleProbe.Arm(target, false);
                ordinaryAttackTrace.BeginCase(Chunk4ChargeId);
                game.IsPaused = false;
                chunk4ChargeTransitionStage = 1; ResetLeafClock(); return;
            }
            if (chunk4ChargeTransitionStage == 1)
            {
                chunk4QueuedCharging |= rider.Descriptor.State.IsCharging || rider.View.AgentASP.IsCharging;
                chunk4CombatBeforeMount |= relationship.State != RelationshipState.Mounted && (rider.IsInCombat || horse.IsInCombat);
                chunk4ChargeTransition["mount"] = CaptureNativeAbilityShell(chunk4QueuedMount);
                chunk4ChargeTransition["charge"] = CaptureNativeAbilityShell(chunk4QueuedCharge);
                chunk4ChargeTransition["admission"] = ordinaryAttackTrace.CaptureAdmission(chunk4QueuedCharge);
                chunk4ChargeTransition["approachExecution"] = ordinaryAttackTrace.CaptureChargeApproach(chunk4QueuedCharge);
                if (!chunk4QueuedCharge.IsFinished) return;
                var admission = ordinaryAttackTrace.CaptureAdmission(chunk4QueuedCharge);
                var rejected = !chunk4QueuedCharge.IsStarted && !chunk4QueuedCharge.IsActed &&
                    !rider.Commands.Queue.Contains(chunk4QueuedCharge) &&
                    !rider.Commands.Raw.Contains(chunk4QueuedCharge);
                var costsPure = admission.Count == 2 && (string)admission[0]["boundary"] == "private-run-before" &&
                    (string)admission[1]["boundary"] == "private-run-after" &&
                    (float)admission[0]["standard"] == (float)admission[1]["standard"] &&
                    (float)admission[0]["move"] == (float)admission[1]["move"] &&
                    JToken.DeepEquals(admission[0]["actorPosition"], admission[1]["actorPosition"]);
                var approach = ordinaryAttackTrace.CaptureChargeApproach(chunk4QueuedCharge);
                var rejectedAtBoundary = Chunk4ChargeRejectionPair(admission);
                for (var index = 0; index + 1 < approach.Count; index += 2)
                    rejectedAtBoundary |= Chunk4ChargeRejectionPair(new JArray(approach[index].DeepClone(), approach[index + 1].DeepClone()));
                chunk4ChargeTransition["mountSucceeded"] = chunk4QueuedMount.IsActed && chunk4QueuedMount.IsFinished &&
                    chunk4QueuedMount.Result == Kingmaker.UnitLogic.Commands.Base.UnitCommand.ResultType.Success &&
                    relationship.State == RelationshipState.Mounted;
                chunk4ChargeTransition["combatBeforeMount"] = chunk4CombatBeforeMount;
                chunk4ChargeTransition["rejectedBeforeStart"] = rejected;
                chunk4ChargeTransition["admissionCostsAndPositionPure"] = costsPure;
                chunk4ChargeTransition["executionRejectedWhileMounted"] = rejectedAtBoundary;
                chunk4ChargeTransition["chargingObserved"] = chunk4QueuedCharging;
                chunk4ChargeTransition["rulesBeforeRecovery"] = ruleProbe.CapturePairEvidence();
                chunk4ChargeTransition["warningDelta"] = chunk4ChargeWarnings.Count - chunk4ChargeWarningStart;
                if (!(bool)chunk4ChargeTransition["mountSucceeded"] || chunk4CombatBeforeMount || !rejected || !costsPure || !rejectedAtBoundary ||
                    chunk4QueuedCharging || ruleProbe.RiderNonOpportunityAttackRuleCount != 0 ||
                    ruleProbe.MountNonOpportunityAttackRuleCount != 0 || chunk4ChargeWarnings.Count != chunk4ChargeWarningStart + 1)
                    throw new InvalidOperationException("Queued Charge crossed the real Mount transition unsafely or lacked ordered evidence.");
                if (!targetService.QueueBidirectionalCombatMemory(rider, target))
                    throw new InvalidOperationException("Post-rejection native encounter admission failed.");
                chunk4ChargeTransitionStage = 2; ResetLeafClock(); return;
            }
            if (!IsCombatReady(true)) return;
            if (Chunk4ChargeTb && (turn?.Unit != rider || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing))
            { TryEndPhase3gFixtureTurn(turn); return; }
            if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation ||
                rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f) return;
            chunk4ChargeTransition["level"] = "NATIVE INTEGRATION";
            chunk4ChargeTransition["mode"] = Chunk4ChargeTb ? "TB" : "RT";
            chunk4ChargeTransition["inputKind"] = "native-mount-handler-and-native-queue-promotion";
            chunk4ChargeTransition["blueprint"] = chunk4ChargeAbility.Blueprint.AssetGuid;
            BeginChunk4ChargeRecovery(chunk4ChargeTransition);
        }

        private static bool Chunk4ChargeRejectionPair(JArray pair)
        {
            if (pair.Count != 2) return false;
            var before = pair[0]; var after = pair[1];
            return (string)before["relationship"] == "Mounted" && (string)after["relationship"] == "Mounted" &&
                (int)before["command"] == (int)after["command"] && (int)before["frame"] == (int)after["frame"] &&
                (bool?)before["started"] == false && (bool?)before["acted"] == false && (bool?)before["finished"] == false &&
                (bool?)after["started"] == false && (bool?)after["acted"] == false && (bool?)after["finished"] == true &&
                (bool?)before["charging"] == false && (bool?)after["charging"] == false &&
                new[] { "standard", "move", "mountStandard", "mountMove", "actorPosition", "mountPosition" }
                    .All(name => JToken.DeepEquals(before[name], after[name]));
        }
    }
}
