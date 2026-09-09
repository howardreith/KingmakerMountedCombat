using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Integration;
using Kingmaker.Visual.FogOfWar;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Bounded native geometry selection and ordinary native controls.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private const string Chunk4ObstructionId = "C4-OBSTRUCTION-ranged-native-geometry";
        private bool IsChunk4Obstruction => request.Scenario == "chunk4-obstruction-ranged-rt";
        private int chunk4ObstructionStage;
        private bool chunk4ObstructionMountSent;
        private JObject chunk4ObstructionEvidence;
        private readonly JArray chunk4ObstructionSamples = new JArray();
        private double chunk4ObstructionStarted;
        private double chunk4ObstructionSampleAt;
        private int chunk4ObstructionBlockedFrames;
        private int chunk4ObstructionDrops;
        private UnitAttack chunk4ObstructionCompleted;

        // A detached native attack only exposes its native visibility query. It
        // never enters a command collection or changes native planning/mode.
        private sealed class Chunk4NativeSightProbe : UnitAttack
        {
            internal Chunk4NativeSightProbe(UnitEntityData target, UnitEntityData actor) : base(target) { Init(actor); }
            internal JObject CaptureSight() => new JObject {
                ["actor"] = Executor.UniqueId, ["target"] = Target.UniqueId,
                ["needLoS"] = NeedLoS, ["targetLosObjectId"] = GetTargetLOSObjectId(),
                ["eye"] = new JArray(Executor.EyePosition.x, Executor.EyePosition.y, Executor.EyePosition.z),
                ["point"] = new JArray(ApproachPoint.x, ApproachPoint.y, ApproachPoint.z),
                ["blocked"] = LineOfSightGeometry.Instance.HasObstacle(Executor.EyePosition, ApproachPoint, GetTargetLOSObjectId()),
                ["enoughClose"] = IsUnitEnoughClose, ["radius"] = ApproachRadius,
                ["distance"] = Executor.DistanceTo(Target), ["weapon"] = Executor.GetFirstWeapon().Blueprint.AssetGuid
            };
        }

        private void BeginChunk4Obstruction()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent || LineOfSightGeometry.Instance == null)
                throw new InvalidOperationException("Obstruction requires the sole paired authority and native geometry.");
            CaptureIdleFixturePartyForCleanup();
            rangedWeaponLease = new Phase3dRangedWeaponLease(rider); rangedWeaponLease.Acquire(Kingmaker.Enums.WeaponCategory.Longbow);
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString());
            chunk4IncomingObserver = new Chunk4IncomingRuleObserver(rider, horse);
            chunk4ObstructionEvidence = new JObject { ["caseId"] = Chunk4ObstructionId, ["level"] = "NATIVE INTEGRATION",
                ["mode"] = "RT", ["inputKind"] = "native-pointer-prediction-and-click", ["candidates"] = new JArray() };
            observations["chunk4Obstruction"] = chunk4ObstructionEvidence;
            step = Phase3dHorseStep.Phase3gControls; ResetLeafClock();
        }

        private JObject CaptureChunk4ObstructionState(UnitEntityData subject) => new JObject {
            ["live"] = CaptureOrdinaryLiveState(), ["gameSeconds"] = Game.Instance.TimeController.GameTime.TotalSeconds,
            ["frame"] = Time.frameCount, ["sight"] = subject == null ? null : new Chunk4NativeSightProbe(subject, rider).CaptureSight(),
            ["intent"] = combat.HasStockAttackIntent, ["activeCommand"] = combat.HasActiveCommand,
            ["groundMovement"] = combat.HasActiveGroundMovement, ["mountCorpulence"] = horse.View.Corpulence,
            ["mountAgentEnabled"] = horse.View.AgentASP.enabled, ["avoidanceDisabled"] = horse.View.AgentASP.AvoidanceDisabled,
            ["target"] = subject == null ? null : CaptureChunk4LifeActor(subject)
        };

        private void TickChunk4Obstruction()
        {
            var game = Game.Instance; var now = game.TimeController.GameTime.TotalSeconds;
            chunk4ObstructionEvidence["stage"] = chunk4ObstructionStage;
            chunk4InterruptOtherService?.ObserveTargetLifeState(); chunk4InterruptOtherService?.RefreshBidirectionalCombatMemoryLease();
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (chunk4ObstructionStage == 0)
            {
                if (!Chunk4PairedPlayIdle) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!chunk4ObstructionMountSent) chunk4ObstructionMountSent = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "obstruction-pre-combat-mount");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                var point = FindWalkablePoint(rider.Position, 12f, 0.5f, candidate => {
                    var blocked = LineOfSightGeometry.Instance.HasObstacle(rider.EyePosition, candidate + Vector3.up, 0);
                    ((JArray)chunk4ObstructionEvidence["candidates"]).Add(new JObject {
                        ["point"] = new JArray(candidate.x,candidate.y,candidate.z), ["blocked"] = blocked });
                    return blocked;
                });
                BeginTarget(12f, Chunk4ObstructionId, point); ruleProbe.Arm(target, false);
                chunk4ObstructionStage = 1; ResetLeafClock(); return;
            }
            if (chunk4ObstructionStage == 1)
            {
                if (!IsCombatReady(true) || CombatController.IsInTurnBasedCombat() || !Chunk4PairedPlayIdle ||
                    rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f ||
                    horse.CombatState.Cooldown.StandardAction > 0.001f || horse.CombatState.Cooldown.MoveAction > 0.001f ||
                    !rider.CombatState.CanActInCombat || !horse.CombatState.CanActInCombat ||
                    game.HandsEquipmentController.IsUpdateScheduledFor(rider) || game.HandsEquipmentController.IsUpdateScheduledFor(horse)) return;
                var before = CaptureOrdinaryLiveState();
                var initial = CaptureChunk4ObstructionState(target);
                if (!JToken.DeepEquals(before, initial["live"]) || !JToken.DeepEquals(before, CaptureOrdinaryLiveState()))
                    throw new InvalidOperationException("Detached native sight query mutated actor state.");
                if (!(bool)initial["sight"]["needLoS"] || !(bool)initial["sight"]["blocked"] || (bool)initial["sight"]["enoughClose"])
                    throw new InvalidOperationException("Selected geometry did not actually obstruct the exact native ranged command predicate.");
                chunk4ObstructionEvidence["before"] = initial;
                chunk4ObstructionEvidence["initialQueryPure"] = true;
                ordinaryAttackTrace.BeginCase(Chunk4ObstructionId + "-blocked"); chunk4IncomingObserver.BeginCase(Chunk4ObstructionId);
                if (!targetService.BeginExpectedAttackDispatch(target)) throw new InvalidOperationException("Obstruction target cannot receive native input.");
                using (var input = new NativeOrdinaryAttackInput(target)) { input.Predict(); chunk4ObstructionEvidence["inputAccepted"] = input.Click(); }
                chunk4ObstructionEvidence["afterInput"] = CaptureChunk4ObstructionState(target);
                AssertChunk4InterruptCostsEqual(initial, (JObject)chunk4ObstructionEvidence["afterInput"]);
                chunk4ObstructionStarted = now; chunk4ObstructionSampleAt = double.MinValue;
                chunk4ObstructionStage = 2; ResetLeafClock(); return;
            }
            if (chunk4ObstructionStage == 2)
            {
                var state = CaptureChunk4ObstructionState(target);
                if ((bool)state["sight"]["blocked"]) chunk4ObstructionBlockedFrames++;
                if (now - chunk4ObstructionSampleAt >= 0.1d)
                {
                    chunk4ObstructionSampleAt = now;
                    if (chunk4ObstructionSamples.Count == 512) chunk4ObstructionDrops++;
                    else chunk4ObstructionSamples.Add(state);
                }
                var ended = ordinaryAttackTrace.StartedAttacks.LastOrDefault(command => command.Executor == rider && command.IsFinished);
                // Permit the real route to go around the obstacle. If it remains
                // blocked, use ordinary Stop after the bounded observation window.
                var rejected = !(bool)chunk4ObstructionEvidence["inputAccepted"] && Chunk4PairedPlayIdle;
                if (ended == null && now - chunk4ObstructionStarted < (rejected ? 1d : 25d)) return;
                chunk4ObstructionEvidence["observationSeconds"] = now - chunk4ObstructionStarted;
                chunk4ObstructionEvidence["blockedFrames"] = chunk4ObstructionBlockedFrames;
                chunk4ObstructionEvidence["sampleDrops"] = chunk4ObstructionDrops;
                chunk4ObstructionEvidence["samples"] = chunk4ObstructionSamples.DeepClone();
                chunk4ObstructionEvidence["beforeStop"] = state;
                SelectionManager.Instance.Stop();
                var afterStop = CaptureChunk4ObstructionState(target);
                AssertChunk4InterruptCostsEqual(state, afterStop);
                chunk4ObstructionEvidence["afterStopInput"] = afterStop;
                chunk4ObstructionStage = 3; ResetLeafClock(); return;
            }
            if (chunk4ObstructionStage == 3)
            {
                if (!Chunk4PairedPlayIdle || !chunk4IncomingObserver.AllAttacksResolved) return;
                var trace = ordinaryAttackTrace.CaptureCaseEvents(Chunk4ObstructionId + "-blocked");
                chunk4ObstructionEvidence["blockedTrace"] = trace;
                chunk4ObstructionEvidence["blockedRules"] = chunk4IncomingObserver.Capture();
                if (chunk4ObstructionBlockedFrames < 1 || chunk4ObstructionDrops != 0 ||
                    trace.OfType<JObject>().Any(row => (string)row["actor"] == rider.UniqueId &&
                        (string)row["boundary"] == "delivery-before" && (bool?)row["nativeCommandLoS"] != true))
                    throw new InvalidOperationException("Obstruction lacked actual blocked frames or a native delivery crossed obstructed geometry.");
                chunk4InterruptOtherService = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
                var point = FindWalkablePoint(rider.Position, 12f, 0.5f, candidate =>
                    HorizontalDistance(candidate, target.Position) >= 3f &&
                    !LineOfSightGeometry.Instance.HasObstacle(rider.EyePosition, candidate + Vector3.up, 0));
                chunk4InterruptOtherTarget = chunk4InterruptOtherService.Spawn(rider, horse, point, request.RunId + "-obstruction-recovery", true, true);
                if (!chunk4InterruptOtherService.PrepareForPlayerClick(chunk4InterruptOtherTarget) ||
                    !chunk4InterruptOtherService.QueueBidirectionalCombatMemory(rider, chunk4InterruptOtherTarget))
                    throw new InvalidOperationException("Clear recovery target failed its native visibility/memory lease.");
                chunk4ObstructionStage = 4; ResetLeafClock(); return;
            }
            if (chunk4ObstructionStage == 4)
            {
                if (!Chunk4PairedPlayIdle || rider.CombatState.Cooldown.StandardAction > 0.001f || horse.CombatState.Cooldown.StandardAction > 0.001f) return;
                var state = CaptureChunk4ObstructionState(chunk4InterruptOtherTarget);
                if ((bool)state["sight"]["blocked"]) throw new InvalidOperationException("Recovery control is not clear to the exact native command query.");
                chunk4ObstructionEvidence["recoveryBefore"] = state;
                ordinaryAttackTrace.BeginCase(Chunk4ObstructionId + "-clear-recovery");
                if (!chunk4InterruptOtherService.BeginExpectedAttackDispatch(chunk4InterruptOtherTarget)) throw new InvalidOperationException("Recovery target cannot receive ordinary input.");
                using (var input = new NativeOrdinaryAttackInput(chunk4InterruptOtherTarget)) { input.Predict(); if (!input.Click()) return; }
                chunk4ObstructionStage = 5; ResetLeafClock(); return;
            }
            if (chunk4ObstructionStage == 5)
            {
                var final = ordinaryAttackTrace.StartedAttacks.LastOrDefault(command => command.Executor == rider && command.Target == chunk4InterruptOtherTarget);
                if (final == null || !final.IsFinished || !final.IsActed || final.GetAttackIndex() < 1) return;
                var rangeTail = final is MountedPairAttackCommand pair && pair.NativeRangedTailTermination;
                if (final.GetAttackIndex() != final.AllAttacks.Count && !rangeTail ||
                    final.Result != Kingmaker.UnitLogic.Commands.Base.UnitCommand.ResultType.Success && !rangeTail &&
                    ordinaryAttackTrace.NativeRecoveryInterrupt(final) == null)
                    throw new InvalidOperationException("Obstruction recovery did not complete its eligible native routine.");
                chunk4ObstructionCompleted = final;
                chunk4ObstructionEvidence["recoveryCommand"] = CaptureOrdinaryCommand(final);
                chunk4ObstructionEvidence["recoveryTarget"] = final.Target.UniqueId;
                chunk4ObstructionEvidence["recoveryPlan"] = final.AllAttacks.Count;
                chunk4ObstructionEvidence["recoveryCompleted"] = final.GetAttackIndex();
                chunk4ObstructionEvidence["recoveryNativeRangeTail"] = rangeTail;
                chunk4ObstructionEvidence["recoveryRangeRejection"] = ordinaryAttackTrace.NativeRangeRejection(final);
                chunk4ObstructionEvidence["recoveryNativeRecovery"] = ordinaryAttackTrace.NativeRecoveryInterrupt(final);
                var beforeStop = CaptureChunk4ObstructionState(chunk4InterruptOtherTarget);
                chunk4ObstructionEvidence["recoveryBeforeStop"] = beforeStop;
                SelectionManager.Instance.Stop();
                var afterStop = CaptureChunk4ObstructionState(chunk4InterruptOtherTarget);
                chunk4ObstructionEvidence["recoveryAfterStopInput"] = afterStop;
                AssertChunk4InterruptCostsEqual(beforeStop, afterStop);
                chunk4ObstructionStage = 6; ResetLeafClock(); return;
            }
            if (chunk4ObstructionStage == 6)
            {
                if (!Chunk4PairedPlayIdle || !chunk4IncomingObserver.AllAttacksResolved) return;
                chunk4ObstructionEvidence["after"] = CaptureChunk4ObstructionState(chunk4InterruptOtherTarget);
                chunk4ObstructionEvidence["recoveryTrace"] = ordinaryAttackTrace.CaptureCaseEvents(Chunk4ObstructionId + "-clear-recovery");
                chunk4ObstructionEvidence["rulesAfter"] = chunk4IncomingObserver.Capture();
                if (chunk4ObstructionCompleted == null || ruleProbe.PairForcedD20Count != 0 || !target.Descriptor.State.IsConscious ||
                    !chunk4InterruptOtherTarget.Descriptor.State.IsConscious || relationship.State != RelationshipState.Mounted)
                    throw new InvalidOperationException("Native obstruction/recovery changed eligibility or lost the pair.");
                AddRow(Chunk4ObstructionId, true, "Measured native obstruction, ordinary Stop and subsequent clear native attack completed.", chunk4ObstructionEvidence);
                BeginCleanup();
            }
        }
    }
}
