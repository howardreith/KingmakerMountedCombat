using System;
using System.Linq;
using Kingmaker;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool IsChunk4HorseStrike => request.Scenario == "chunk4-horse-strike-comparison-rt";
        private int chunk4HorseCase;
        private int chunk4HorseStage;
        private int chunk4HorseCompleted;
        private bool chunk4HorseControlSent;
        private UnitMoveTo chunk4HorseSetupMove;
        private UnitAttack chunk4HorseAttack;
        private JObject chunk4HorseEvidence;
        private JObject chunk4HorseRoutine;
        private bool Chunk4HorseMounted => chunk4HorseCase == 0;
        private string Chunk4HorseId => Chunk4HorseMounted ? "C4-HORSE-mounted-three-primaries" : "C4-HORSE-unmounted-strike-recovery";
        // Passed to the existing camera recorder by the parent Tick. No second capture system.
        private string Chunk4HorseCapturePhase => chunk4HorseStage != 3 ? null : Chunk4HorseId +
            (chunk4HorseAttack == null || !chunk4HorseAttack.IsStarted ? "-horse-approach" :
                chunk4HorseAttack.GetAttackIndex() == 0 ? "-horse-strike" : "-horse-recovery");

        private void BeginChunk4HorseStrike()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Horse comparison requires the accepted paired configuration.");
            CaptureIdleFixturePartyForCleanup();
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString());
            step = Phase3dHorseStep.Phase3gControls; ResetLeafClock();
        }

        private void TickChunk4HorseStrike()
        {
            var game = Game.Instance;
            if (game.IsPaused) { game.IsPaused = false; return; }
            observations["chunk4HorseProgress"] = new JObject { ["case"] = chunk4HorseCase, ["stage"] = chunk4HorseStage,
                ["routines"] = chunk4HorseCompleted, ["live"] = CaptureOrdinaryLiveState(), ["current"] = chunk4HorseEvidence };
            if (chunk4HorseStage == 0)
            {
                if (chunk4HorseCase == 2) { observations["ordinaryTrace"] = ordinaryAttackTrace.Capture(); BeginCleanup(); return; }
                if (!Chunk4PairedPlayIdle) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != (Chunk4HorseMounted ? RelationshipState.Mounted : RelationshipState.Unmounted))
                {
                    if (!chunk4HorseControlSent) chunk4HorseControlSent = TryNativeAbilityTargetClick(
                        Chunk4HorseMounted ? nativeControls.MountAbility : nativeControls.DismountAbility,
                        Chunk4HorseMounted ? horse : rider, "horse-comparison-pre-combat-transition");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                BeginTarget(6f, Chunk4HorseId); ruleProbe.Arm(target, false);
                chunk4HorseEvidence = new JObject { ["level"] = "NATIVE INTEGRATION", ["caseId"] = Chunk4HorseId,
                    ["mode"] = "RT", ["mounted"] = Chunk4HorseMounted, ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId,
                    ["weapon"] = horse.GetFirstWeapon().Blueprint.AssetGuid, ["routines"] = new JArray(),
                    ["targetProvisioning"] = observations["target-" + Chunk4HorseId].DeepClone(),
                    ["inputKind"] = Chunk4HorseMounted ? "native-mount-Primary-control" : "native-ordinary-pointer-click" };
                chunk4HorseStage = 1; ResetLeafClock(); return;
            }
            if (chunk4HorseStage == 1)
            {
                if (!IsCombatReady(Chunk4HorseMounted) || CombatController.IsInTurnBasedCombat()) return;
                if (chunk4HorseSetupMove == null)
                {
                    var plan = new UnitAttack(target); plan.Init(horse);
                    var radius = horse.View.Corpulence + target.View.Corpulence + plan.CreateFullAttack().Min(attack => attack.WeaponRange);
                    var point = FindNativeAttackFixturePoint(horse, Chunk4HorseMounted, horse.Position, 0.25f, radius, "horse-comparison-adjacency");
                    SelectionManager.Instance.SelectUnit(Chunk4HorseMounted ? rider.View : horse.View, true, true, false);
                    using (var input = new NativeOrdinaryAttackInput(point)) { input.Predict(); if (!input.Click()) return; }
                    chunk4HorseSetupMove = horse.Commands.Move as UnitMoveTo;
                    if (chunk4HorseSetupMove?.Executor != horse) throw new InvalidOperationException("Horse comparison lost its native ground mover.");
                    ResetLeafClock(); return;
                }
                if (!chunk4HorseSetupMove.IsFinished || !Chunk4PairedPlayIdle) return;
                if (chunk4HorseSetupMove.Result != UnitCommand.ResultType.Success) throw new InvalidOperationException("Horse comparison native positioning failed.");
                chunk4HorseStage = 2; ResetLeafClock();
            }
            if (chunk4HorseStage == 2)
            {
                if (!Chunk4PairedPlayIdle || horse.CombatState.Cooldown.StandardAction > 0.001f || horse.CombatState.Cooldown.MoveAction > 0.001f ||
                    !horse.CombatState.CanActInCombat || !target.Descriptor.State.IsConscious) return;
                SelectionManager.Instance.SelectUnit(Chunk4HorseMounted ? rider.View : horse.View, true, true, false);
                var traceCase = Chunk4HorseId + "-routine-" + chunk4HorseCompleted;
                ordinaryAttackTrace.BeginCase(traceCase);
                chunk4HorseRoutine = new JObject { ["traceCase"] = traceCase, ["before"] = CaptureOrdinaryLiveState(),
                    ["rulesBefore"] = ruleProbe.CapturePairEvidence() };
                ((JArray)chunk4HorseEvidence["routines"]).Add(chunk4HorseRoutine);
                if (!targetService.BeginExpectedAttackDispatch(target)) throw new InvalidOperationException("Horse comparison target is not valid for actual input.");
                if (Chunk4HorseMounted)
                {
                    if (!TryNativeAbilityTargetClick(nativeControls.MountPrimaryAbility, target, traceCase))
                        throw new InvalidOperationException("Ready Horse native Primary was rejected.");
                }
                else using (var input = new NativeOrdinaryAttackInput(target)) { input.Predict(); if (!input.Click()) return; }
                chunk4HorseAttack = null; chunk4HorseStage = 3; ResetLeafClock(); return;
            }
            if (chunk4HorseStage == 3)
            {
                if (chunk4HorseAttack == null) chunk4HorseAttack = ordinaryAttackTrace.LastStartedMountAttack;
                if (chunk4HorseAttack == null || !chunk4HorseAttack.IsFinished ||
                    ruleProbe.MountResolvedCount < ruleProbe.MountNonOpportunityAttackRuleCount) return;
                var delivered = ruleProbe.MountResolvedCount - (int)chunk4HorseRoutine["rulesBefore"]["mountResolved"];
                var nativeRecovery = ordinaryAttackTrace.NativeRecoveryInterrupt(chunk4HorseAttack);
                chunk4HorseRoutine["after"] = CaptureOrdinaryLiveState(); chunk4HorseRoutine["command"] = CaptureOrdinaryCommand(chunk4HorseAttack);
                chunk4HorseRoutine["planned"] = chunk4HorseAttack.AllAttacks.Count; chunk4HorseRoutine["completed"] = chunk4HorseAttack.GetAttackIndex();
                chunk4HorseRoutine["resolved"] = delivered; chunk4HorseRoutine["rulesAfter"] = ruleProbe.CapturePairEvidence();
                chunk4HorseRoutine["nativeRecovery"] = nativeRecovery;
                chunk4HorseRoutine["nativeTrace"] = ordinaryAttackTrace.CaptureCaseEvents((string)chunk4HorseRoutine["traceCase"]);
                if (chunk4HorseAttack.Executor != horse || chunk4HorseAttack.AllAttacks.Count < 1 ||
                    chunk4HorseAttack.GetAttackIndex() != chunk4HorseAttack.AllAttacks.Count || delivered != chunk4HorseAttack.GetAttackIndex() ||
                    chunk4HorseAttack.Result != UnitCommand.ResultType.Success && nativeRecovery == null ||
                    Chunk4HorseMounted && (delivered != 1 || rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f) ||
                    ruleProbe.PairForcedD20Count != 0 || !target.Descriptor.State.IsConscious)
                    throw new InvalidOperationException("Horse strike comparison lost native plan, delivery, life state or independent actor costs.");
                chunk4HorseCompleted++;
                chunk4HorseRoutine["beforeStop"] = CaptureOrdinaryLiveState();
                SelectionManager.Instance.Stop();
                chunk4HorseRoutine["afterStopInput"] = CaptureOrdinaryLiveState();
                foreach (var actor in new[] { "rider", "mount" }) foreach (var cost in new[] { "standard", "move" })
                    if (!JToken.DeepEquals(chunk4HorseRoutine["beforeStop"][actor][cost], chunk4HorseRoutine["afterStopInput"][actor][cost]))
                        throw new InvalidOperationException("Horse strike Stop changed a real native cost.");
                chunk4HorseStage = chunk4HorseCompleted < (Chunk4HorseMounted ? 3 : 1) ? 2 : 4;
                ResetLeafClock(); return;
            }
            if (chunk4HorseStage == 4)
            {
                if (!Chunk4PairedPlayIdle || ruleProbe.MountResolvedCount != ruleProbe.MountNonOpportunityAttackRuleCount) return;
                chunk4HorseEvidence["afterStop"] = CaptureOrdinaryLiveState();
                chunk4HorseEvidence["cameraSequence"] = motionEvidence?.Snapshot().DeepClone();
                AddRow(Chunk4HorseId, true, "Native Horse strike and recovery retained real plan, delivery and costs; camera evidence is separate from human visual acceptance.", chunk4HorseEvidence);
                chunk4HorseStage = 5; ResetLeafClock(); return;
            }
            if (chunk4HorseStage == 5)
            {
                TryLeaveCombat(target); TryLeaveCombat(rider); TryLeaveCombat(horse);
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) return;
                    targetService.Dispose(); targetService = null; target = null;
                }
                if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                if (game.Player.IsInCombat || game.TurnBasedCombatController.Initialized || CombatController.IsInTurnBasedCombat()) return;
                if (!RestoreCombatMountRiderAiIsolation() || !RestoreUnmountedHorseAiIsolation())
                    throw new InvalidOperationException("Horse comparison AI leases did not restore after native combat.");
                combatMountRiderAiLease = null; unmountedHorseAiLease = null; unmountedHorseAiSettleRequested = false;
                chunk4HorseCase++; chunk4HorseCompleted = 0; chunk4HorseStage = 0;
                chunk4HorseControlSent = false; chunk4HorseSetupMove = null; ResetLeafClock();
            }
        }
    }
}
