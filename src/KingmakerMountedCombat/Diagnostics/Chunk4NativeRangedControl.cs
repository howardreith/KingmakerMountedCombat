using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Enums;
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
        private bool IsChunk4NativeRanged => request.Scenario == "chunk4-ranged-native-control-rt";
        internal static bool IsChunk4CoreScenario(string scenario) => IsChunk4NativeLifeScenario(scenario) ||
            IsChunk4IncomingScenario(scenario) || scenario == "chunk4-horse-strike-comparison-rt" || scenario == "chunk4-ranged-native-control-rt";
        private bool IsChunk4Core => IsChunk4CoreScenario(request.Scenario);
        private int chunk4NativeRangedStage;
        private bool chunk4NativeRangedDismount;
        private UnitAttack chunk4NativeRangedAttack;
        private JObject chunk4NativeRangedEvidence;

        private void BeginChunk4NativeRanged()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay)
                throw new InvalidOperationException("Native ranged control requires the tested paired configuration while actually unmounted.");
            CaptureIdleFixturePartyForCleanup();
            rangedWeaponLease = new Phase3dRangedWeaponLease(rider); rangedWeaponLease.Acquire(WeaponCategory.Longbow);
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString());
            chunk4NativeRangedEvidence = new JObject { ["level"] = "NATIVE INTEGRATION", ["mode"] = "RT",
                ["caseId"] = "C4-RANGED-native-mixed-range", ["scope"] = "one unmounted native mixed-range routine; not the three-routine sustained gate" };
            observations["chunk4NativeRanged"] = chunk4NativeRangedEvidence;
            step = Phase3dHorseStep.Phase3gControls; ResetLeafClock();
        }
        private void TickChunk4NativeRanged()
        {
            var game = Game.Instance;
            if (game.IsPaused) { game.IsPaused = false; return; }
            chunk4NativeRangedEvidence["progress"] = new JObject { ["stage"] = chunk4NativeRangedStage,
                ["live"] = CaptureOrdinaryLiveState(), ["command"] = CaptureOrdinaryCommand(chunk4NativeRangedAttack) };
            if (chunk4NativeRangedStage == 0)
            {
                if (!Chunk4PairedPlayIdle) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Unmounted)
                {
                    if (!chunk4NativeRangedDismount) chunk4NativeRangedDismount = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "chunk4-native-ranged-dismount");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                BeginTarget(6f, "C4-RANGED-native-mixed-range"); ruleProbe.Arm(target, false);
                chunk4NativeRangedStage = 1; ResetLeafClock(); return;
            }
            if (chunk4NativeRangedStage == 1)
            {
                if (!IsCombatReady(false) || CombatController.IsInTurnBasedCombat() || !Chunk4PairedPlayIdle ||
                    !rider.CombatState.CanActInCombat || rider.CombatState.Cooldown.StandardAction > .001f || rider.CombatState.Cooldown.MoveAction > .001f) return;
                var native = new UnitAttack(target); native.Init(rider);
                if (!native.IsUnitEnoughClose || rider.DistanceTo(target) <= rider.View.Corpulence + target.View.Corpulence + native.CreateFullAttack().Min(attack => attack.WeaponRange))
                    throw new InvalidOperationException("Unmounted ranged control is not inside native bow range and outside its melee tail range.");
                chunk4NativeRangedEvidence["before"] = CaptureOrdinaryLiveState();
                ordinaryAttackTrace.BeginCase("C4-RANGED-native-mixed-range");
                if (!targetService.BeginExpectedAttackDispatch(target)) throw new InvalidOperationException("Native control target invalid before input.");
                using (var input = new NativeOrdinaryAttackInput(target)) { input.Predict(); if (!input.Click()) return; }
                chunk4NativeRangedStage = 2; ResetLeafClock(); return;
            }
            if (chunk4NativeRangedStage == 2)
            {
                if (chunk4NativeRangedAttack == null) chunk4NativeRangedAttack = ordinaryAttackTrace.LastStartedRiderAttack;
                var attack = chunk4NativeRangedAttack;
                if (attack == null || !attack.IsFinished || rider.AreHandsBusyWithAnimation || ruleProbe.RiderResolvedCount < attack.GetAttackIndex()) return;
                var rejected = ordinaryAttackTrace.NativeRangeRejection(attack);
                var completed = attack.GetAttackIndex();
                chunk4NativeRangedEvidence["command"] = CaptureOrdinaryCommand(attack);
                chunk4NativeRangedEvidence["nativePlan"] = new JArray(attack.AllAttacks.Select(item => new JObject {
                    ["weapon"] = item.Weapon.Blueprint.AssetGuid, ["ranged"] = item.Weapon.Blueprint.IsRanged, ["range"] = item.WeaponRange }));
                chunk4NativeRangedEvidence["planned"] = attack.AllAttacks.Count; chunk4NativeRangedEvidence["completed"] = completed;
                chunk4NativeRangedEvidence["nativeRangeRejection"] = rejected; chunk4NativeRangedEvidence["rules"] = ruleProbe.CapturePairEvidence();
                chunk4NativeRangedEvidence["nativeTrace"] = ordinaryAttackTrace.CaptureCaseEvents("C4-RANGED-native-mixed-range");
                if (attack.GetType() != typeof(UnitAttack) || relationship.State != RelationshipState.Unmounted || !attack.IsFullAttack ||
                    attack.Result != UnitCommand.ResultType.Interrupt || completed <= 0 || completed >= attack.AllAttacks.Count ||
                    !attack.AllAttacks.Take(completed).All(item => item.Weapon.Blueprint.IsRanged) ||
                    !attack.AllAttacks.Skip(completed).All(item => !item.Weapon.Blueprint.IsRanged) || rejected == null ||
                    (float)rejected["rangeOriginDistance"] <= (float)rejected["approachRadius"] ||
                    ruleProbe.RiderResolvedCount != completed || ruleProbe.PairForcedD20Count != 0 || ruleProbe.MountNonOpportunityAttackRuleCount != 0)
                    throw new InvalidOperationException("Unmounted native control did not exhibit the observed mixed-range terminal and exact delivered prefix.");
                chunk4NativeRangedEvidence["beforeStop"] = CaptureOrdinaryLiveState(); SelectionManager.Instance.Stop();
                chunk4NativeRangedEvidence["afterStopInput"] = CaptureOrdinaryLiveState();
                foreach (var actor in new[] { "rider", "mount" }) foreach (var cost in new[] { "standard", "move" })
                    if (!JToken.DeepEquals(chunk4NativeRangedEvidence["beforeStop"][actor][cost], chunk4NativeRangedEvidence["afterStopInput"][actor][cost]))
                        throw new InvalidOperationException("Native Stop changed a real spent cost.");
                AddRow("C4-RANGED-native-mixed-range", true, "Actual unmounted native bow plan ended on its out-of-range melee tail after preserving ranged deliveries and cost.", chunk4NativeRangedEvidence);
                BeginCleanup();
            }
        }
    }
}