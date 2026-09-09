using System;
using System.Collections.Generic;
using System.Linq;
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
    // A native routine means the entire command's own plan and recovery.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk4SustainedScenario(string scenario) =>
            scenario == "chunk4-sustained-melee-rt" || scenario == "chunk4-sustained-ranged-rt";
        internal static bool IsChunk4PlayScenario(string scenario) => IsChunk4SustainedScenario(scenario) || scenario == "chunk4-sustained-tb";
        private bool IsChunk4Play => IsChunk4PlayScenario(request.Scenario);
        private bool IsChunk4Sustained => IsChunk4SustainedScenario(request.Scenario);
        private bool Chunk4SustainedRanged => request.Scenario == "chunk4-sustained-ranged-rt";
        private bool Chunk4SustainedRepeat => chunk4SustainedCase % 2 == 1;
        private bool Chunk4SustainedApproach => chunk4SustainedCase >= 2;
        private string Chunk4SustainedId => "C4-SUSTAINED-" + (Chunk4SustainedRanged ? "ranged-" : "melee-") +
            (Chunk4SustainedApproach ? "approach-" : "adjacent-") + (Chunk4SustainedRepeat ? "repeat" : "held");
        private int chunk4SustainedCase;
        private int chunk4SustainedStage;
        private bool chunk4SustainedControlSent;
        private UnitMoveTo chunk4SustainedSetupMove;
        private readonly Dictionary<UnitAttack, JObject> chunk4Routines = new Dictionary<UnitAttack, JObject>();
        private JObject chunk4SustainedEvidence;
        private JArray chunk4SustainedClicks;
        private JArray chunk4SustainedSamples;
        private long chunk4SustainedIntentBefore;
        private long chunk4SustainedDuplicateBefore;
        private int chunk4SustainedCompleted;
        private double chunk4SustainedLastClick;
        private double chunk4SustainedLastSample;
        private double chunk4SustainedPreviousTick;
        private float chunk4SustainedTemporaryHp;
        private Vector3 chunk4SustainedMountOrigin;

        private static double Chunk4NativeSeconds => Game.Instance.TimeController.GameTime.TotalSeconds;

        private void BeginChunk4Sustained()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Sustained combat requires the accepted paired configuration.");
            CaptureIdleFixturePartyForCleanup();
            observations["chunk4NativeEffectInventory"] = Chunk4NativeEffectInventory.Capture(rider, horse);
            observations["chunk4SubscriptionsBefore"] = Chunk4SubscriptionSnapshot.Capture();
            if (Chunk4SustainedRanged)
            {
                rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
                rangedWeaponLease.Acquire(Kingmaker.Enums.WeaponCategory.Longbow);
            }
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString());
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }

        private void TickChunk4Sustained()
        {
            var game = Game.Instance;
            if (game.IsPaused) { game.IsPaused = false; return; }
            observations["chunk4SustainedProgress"] = new JObject {
                ["case"] = chunk4SustainedCase < 4 ? Chunk4SustainedId : "cleanup",
                ["stage"] = chunk4SustainedStage, ["state"] = CaptureOrdinaryLiveState(),
                ["routineCount"] = chunk4Routines.Count, ["completeRiderRoutines"] = chunk4SustainedCompleted,
                ["currentEvidence"] = chunk4SustainedEvidence
            };
            if (chunk4SustainedStage == 0)
            {
                if (chunk4SustainedCase >= 4) { observations["ordinaryTrace"] = ordinaryAttackTrace.Capture(); BeginCleanup(); return; }
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation || horse.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!chunk4SustainedControlSent)
                        chunk4SustainedControlSent = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk4-sustained-mount");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                var weapon = rider.GetFirstWeapon();
                if (weapon == null || weapon.Blueprint.IsRanged != Chunk4SustainedRanged)
                    throw new InvalidOperationException("Sustained fixture does not have the requested native weapon kind.");
                // Preserve the target service's 3..20 metre spawn guard. A ranged
                // approach is arranged with a native setup walk after spawning.
                var distance = Chunk4SustainedApproach ? 9f : 6f;
                BeginTarget(distance, Chunk4SustainedId);
                ruleProbe.Arm(target, false);
                chunk4SustainedEvidence = new JObject {
                    ["level"] = "NATIVE INTEGRATION", ["mode"] = "RT", ["caseId"] = Chunk4SustainedId,
                    ["weapon"] = weapon.Blueprint.AssetGuid, ["ranged"] = weapon.Blueprint.IsRanged,
                    ["repeat"] = Chunk4SustainedRepeat, ["approach"] = Chunk4SustainedApproach,
                    ["target"] = target.UniqueId, ["targetProvisioning"] = observations["target-" + Chunk4SustainedId].DeepClone(),
                    ["inputKind"] = "scripted-native-pointer-prediction-and-click", ["maximumNativeFrameStep"] = 0d
                };
                chunk4SustainedStage = 1; ResetLeafClock(); return;
            }
            if (chunk4SustainedStage == 1)
            {
                if (!IsCombatReady(true) || CombatController.IsInTurnBasedCombat()) return;
                if ((!Chunk4SustainedApproach || Chunk4SustainedRanged) && chunk4SustainedSetupMove == null)
                {
                    var plan = new UnitAttack(target); plan.Init(rider);
                    var radius = horse.View.Corpulence + target.View.Corpulence + plan.CreateFullAttack().Min(attack => attack.WeaponRange);
                    var point = Chunk4SustainedApproach ? FindWalkablePoint(target.Position,
                        horse.View.Corpulence + target.View.Corpulence + rider.GetFirstWeapon().Blueprint.AttackRange.Meters + 3f, 0.5f) :
                        FindNativeAttackFixturePoint(horse, true, horse.Position, 0.25f, Math.Min(radius, 3.5f),
                            "sustained-position-" + Chunk4SustainedId);
                    using (var input = new NativeOrdinaryAttackInput(point)) { input.Predict(); if (!input.Click()) return; }
                    chunk4SustainedSetupMove = horse.Commands.Move as UnitMoveTo;
                    if (chunk4SustainedSetupMove?.Executor != horse) throw new InvalidOperationException("Sustained setup lost native mount mover.");
                    ResetLeafClock(); return;
                }
                if (chunk4SustainedSetupMove != null && (!chunk4SustainedSetupMove.IsFinished || horse.View.AgentASP.IsReallyMoving)) return;
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation || horse.AreHandsBusyWithAnimation ||
                    !rider.CombatState.CanActInCombat || !horse.CombatState.CanActInCombat ||
                    rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f ||
                    horse.CombatState.Cooldown.StandardAction > 0.001f || horse.CombatState.Cooldown.MoveAction > 0.001f ||
                    game.HandsEquipmentController.IsUpdateScheduledFor(rider) || game.HandsEquipmentController.IsUpdateScheduledFor(horse)) return;
                var native = new UnitAttack(target); native.Init(rider);
                chunk4SustainedEvidence["before"] = CaptureOrdinaryLiveState();
                chunk4SustainedEvidence["nativeEnoughCloseBefore"] = native.IsUnitEnoughClose;
                if (native.IsUnitEnoughClose == Chunk4SustainedApproach)
                    throw new InvalidOperationException("Native range does not distinguish the requested adjacent/approach condition.");
                chunk4Routines.Clear(); chunk4SustainedCompleted = 0;
                chunk4SustainedClicks = new JArray(); chunk4SustainedSamples = new JArray();
                chunk4SustainedEvidence["clicks"] = chunk4SustainedClicks; chunk4SustainedEvidence["samples"] = chunk4SustainedSamples;
                chunk4SustainedIntentBefore = combat.StockAttackIntentStartCount;
                chunk4SustainedDuplicateBefore = combat.StockAttackDuplicateDispatchCount;
                chunk4SustainedTemporaryHp = target.Stats.TemporaryHitPoints.ModifiedValue;
                chunk4SustainedMountOrigin = horse.Position;
                ordinaryAttackTrace.BeginCase(Chunk4SustainedId);
                if (!targetService.BeginExpectedAttackDispatch(target)) throw new InvalidOperationException("Sustained target invalid before input.");
                using (var input = new NativeOrdinaryAttackInput(target)) { input.Predict(); if (!input.Click()) return; }
                chunk4SustainedEvidence["startTime"] = Chunk4NativeSeconds;
                chunk4SustainedLastClick = chunk4SustainedLastSample = chunk4SustainedPreviousTick = Chunk4NativeSeconds;
                chunk4SustainedStage = 2; ResetLeafClock(); return;
            }
            if (chunk4SustainedStage == 2 || chunk4SustainedStage == 3)
            {
                ObserveChunk4SustainedRoutines();
                if (chunk4SustainedStage == 3)
                {
                    if (combat.HasActiveCommand || combat.HasStockAttackIntent || !rider.Commands.Empty || !horse.Commands.Empty ||
                        ruleProbe.RiderResolvedCount < ruleProbe.RiderNonOpportunityAttackRuleCount ||
                        ruleProbe.MountResolvedCount < ruleProbe.MountNonOpportunityAttackRuleCount) return;
                    CompleteChunk4SustainedCase(); return;
                }
                if (target == null || !target.IsInState || !target.Descriptor.State.IsConscious)
                    throw new InvalidOperationException("Durable sustained target died before three complete routines; no replenishment is permitted.");
                if (target.Stats.TemporaryHitPoints.ModifiedValue > chunk4SustainedTemporaryHp)
                    throw new InvalidOperationException("Sustained target gained durability during measurement.");
                chunk4SustainedTemporaryHp = target.Stats.TemporaryHitPoints.ModifiedValue;
                if (chunk4SustainedCompleted >= 3)
                {
                    chunk4SustainedEvidence["beforeStop"] = CaptureOrdinaryLiveState();
                    chunk4SustainedEvidence["stopFrame"] = Time.frameCount;
                    SelectionManager.Instance.Stop();
                    chunk4SustainedEvidence["afterStopInput"] = CaptureOrdinaryLiveState();
                    foreach (var actorKey in new[] { "rider", "mount" })
                        foreach (var costKey in new[] { "standard", "move" })
                            if (!JToken.DeepEquals(chunk4SustainedEvidence["beforeStop"][actorKey][costKey],
                                chunk4SustainedEvidence["afterStopInput"][actorKey][costKey]))
                                throw new InvalidOperationException("Native Stop refunded or added a real actor cost.");
                    chunk4SustainedStage = 3; ResetLeafClock(); return;
                }
                if (Chunk4SustainedRepeat && Chunk4NativeSeconds - chunk4SustainedLastClick >= 0.25d)
                {
                    var before = CaptureOrdinaryLiveState();
                    var commands = chunk4Routines.Keys.Where(command => !command.IsFinished).ToArray();
                    var times = commands.Select(command => command.TimeSinceStart).ToArray();
                    using (var input = new NativeOrdinaryAttackInput(target)) { input.Predict(); input.Click(); }
                    var pure = JToken.DeepEquals(before, CaptureOrdinaryLiveState()) && commands.Select((command, index) =>
                        !command.IsFinished && command.TimeSinceStart == times[index]).All(value => value);
                    chunk4SustainedClicks.Add(new JObject { ["time"] = Chunk4NativeSeconds, ["frame"] = Time.frameCount,
                        ["pure"] = pure, ["before"] = before, ["after"] = CaptureOrdinaryLiveState(),
                        ["clocksBefore"] = new JArray(times), ["clocksAfter"] = new JArray(commands.Select(command => command.TimeSinceStart)),
                        ["liveCommands"] = new JArray(commands.Select(CaptureOrdinaryCommand)) });
                    if (!pure) throw new InvalidOperationException("Repeated ordinary input restarted or mutated an in-flight native routine.");
                    chunk4SustainedLastClick = Chunk4NativeSeconds;
                }
                return;
            }
            if (chunk4SustainedStage == 4)
            {
                TryLeaveCombat(target); TryLeaveCombat(rider); TryLeaveCombat(horse);
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) return;
                    targetService.Dispose(); targetService = null; target = null;
                }
                if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                if (CombatController.IsInTurnBasedCombat() || game.TurnBasedCombatController.Initialized || game.Player.IsInCombat ||
                    !rider.Commands.Empty || !horse.Commands.Empty) return;
                if (!RestoreCombatMountRiderAiIsolation() || !RestoreUnmountedHorseAiIsolation())
                    throw new InvalidOperationException("Sustained fixture AI restoration failed.");
                combatMountRiderAiLease = null; unmountedHorseAiLease = null; unmountedHorseAiSettleRequested = false;
                observations["subscriptions-after-" + Chunk4SustainedId] = Chunk4SubscriptionSnapshot.Capture();
                observations["actor-records-after-" + Chunk4SustainedId] = combat.TrackedActorAllocations;
                chunk4SustainedCase++; chunk4SustainedStage = 0; chunk4SustainedSetupMove = null;
                ResetLeafClock();
            }
        }

        private void ObserveChunk4SustainedRoutines()
        {
            var now = Chunk4NativeSeconds;
            chunk4SustainedEvidence["maximumNativeFrameStep"] = Math.Max((double)chunk4SustainedEvidence["maximumNativeFrameStep"], now - chunk4SustainedPreviousTick);
            chunk4SustainedPreviousTick = now;
            foreach (var command in ordinaryAttackTrace.StartedAttacks)
            {
                JObject evidence;
                if (!chunk4Routines.TryGetValue(command, out evidence))
                {
                    if (chunk4Routines.Keys.Any(prior => prior.Executor == command.Executor && !prior.IsFinished))
                        throw new InvalidOperationException("One actor started overlapping native attack routines.");
                    evidence = new JObject { ["firstObserved"] = now, ["lastTimeSinceStart"] = command.TimeSinceStart,
                        ["actor"] = command.Executor.UniqueId, ["completedObserved"] = false };
                    chunk4Routines.Add(command, evidence);
                }
                if (command.TimeSinceStart + 0.000001f < (float)evidence["lastTimeSinceStart"])
                    throw new InvalidOperationException("Native routine clock moved backwards during sustained input.");
                evidence["lastTimeSinceStart"] = command.TimeSinceStart;
                evidence["command"] = CaptureOrdinaryCommand(command);
                evidence["planned"] = command.AllAttacks.Count; evidence["completed"] = command.GetAttackIndex();
                if (!(bool)evidence["completedObserved"] && command.IsFinished && command.AllAttacks.Count > 0 &&
                    command.GetAttackIndex() == command.AllAttacks.Count &&
                    (command.Result == UnitCommand.ResultType.Success || ordinaryAttackTrace.NativeRecoveryInterrupt(command) != null))
                {
                    evidence["completedObserved"] = true; evidence["finishedAt"] = now;
                    if (command.Executor == rider) { chunk4SustainedCompleted++; ResetLeafClock(); }
                }
            }
            if (now - chunk4SustainedLastSample >= 0.25d)
            {
                chunk4SustainedSamples.Add(new JObject { ["time"] = now, ["frame"] = Time.frameCount,
                    ["riderStandard"] = rider.CombatState.Cooldown.StandardAction,
                    ["riderMove"] = rider.CombatState.Cooldown.MoveAction,
                    ["mountStandard"] = horse.CombatState.Cooldown.StandardAction,
                    ["mountMove"] = horse.CombatState.Cooldown.MoveAction,
                    ["targetTemporaryHp"] = target.Stats.TemporaryHitPoints.ModifiedValue,
                    ["targetDamage"] = target.Damage });
                chunk4SustainedLastSample = now;
            }
        }

        private void CompleteChunk4SustainedCase()
        {
            chunk4SustainedEvidence["routines"] = new JArray(chunk4Routines.Values.Select(value => value.DeepClone()));
            chunk4SustainedEvidence["rules"] = ruleProbe.CapturePairEvidence();
            chunk4SustainedEvidence["afterStop"] = CaptureOrdinaryLiveState();
            chunk4SustainedEvidence["intentStarts"] = combat.StockAttackIntentStartCount - chunk4SustainedIntentBefore;
            chunk4SustainedEvidence["duplicateDispatches"] = combat.StockAttackDuplicateDispatchCount - chunk4SustainedDuplicateBefore;
            chunk4SustainedEvidence["mountDistance"] = HorizontalDistance(horse.Position, chunk4SustainedMountOrigin);
            chunk4SustainedEvidence["completeRiderRoutines"] = chunk4SustainedCompleted;
            chunk4SustainedEvidence["nativeTrace"] = ordinaryAttackTrace.CaptureCaseEvents(Chunk4SustainedId);
            var plans = chunk4Routines.Keys.Where(command => command.Executor == rider).Sum(command => command.GetAttackIndex());
            var mountPlans = chunk4Routines.Keys.Where(command => command.Executor == horse).Sum(command => command.GetAttackIndex());
            var passed = chunk4SustainedCompleted >= 3 && plans == ruleProbe.RiderNonOpportunityAttackRuleCount &&
                ruleProbe.RiderResolvedCount == plans && ruleProbe.PairForcedD20Count == 0 &&
                mountPlans == ruleProbe.MountNonOpportunityAttackRuleCount && ruleProbe.MountResolvedCount == mountPlans &&
                combat.StockAttackIntentStartCount - chunk4SustainedIntentBefore == 1 &&
                combat.StockAttackDuplicateDispatchCount == chunk4SustainedDuplicateBefore &&
                (!Chunk4SustainedRepeat || chunk4SustainedClicks.Count >= 12) &&
                (!Chunk4SustainedApproach || (float)chunk4SustainedEvidence["mountDistance"] > 0.5f) &&
                chunk4SustainedSamples.All(sample => (float)sample["riderMove"] <= 0.001f);
            var starts = ((JArray)chunk4SustainedEvidence["nativeTrace"]).OfType<JObject>().Where(evt =>
                (string)evt["boundary"] == "start-after" && (string)evt["actor"] == rider.UniqueId).ToArray();
            var periods = starts.Skip(1).Select((evt, index) =>
                ((long)evt["gameTime"] - (long)starts[index]["gameTime"]) / (double)TimeSpan.TicksPerSecond).Take(2).ToArray();
            chunk4SustainedEvidence["riderStartPeriods"] = new JArray(periods);
            if (Chunk4SustainedRepeat)
            {
                var heldName = Chunk4SustainedId.Replace("-repeat", "-held");
                var held = rows.OfType<JObject>().Single(row => (string)row["name"] == heldName)["evidence"];
                var heldPeriods = ((JArray)held["riderStartPeriods"]).Select(period => (double)period).ToArray();
                var frameTolerance = 2d * Math.Max((double)held["maximumNativeFrameStep"],
                    (double)chunk4SustainedEvidence["maximumNativeFrameStep"]) + 0.02d;
                chunk4SustainedEvidence["cadenceComparison"] = new JObject {
                    ["heldCase"] = heldName, ["heldPeriods"] = new JArray(heldPeriods),
                    ["repeatPeriods"] = new JArray(periods), ["observedFrameTolerance"] = frameTolerance,
                    ["sameWeapon"] = JToken.DeepEquals(held["weapon"], chunk4SustainedEvidence["weapon"])
                };
                passed &= periods.Length == 2 && heldPeriods.Length == 2 && frameTolerance <= 0.25d &&
                    periods.Average() >= heldPeriods.Min() - frameTolerance &&
                    JToken.DeepEquals(held["weapon"], chunk4SustainedEvidence["weapon"]);
            }
            AddRow(Chunk4SustainedId, passed, "Three complete native routines with native cadence, pure repeated input, delivery accounting and Stop settlement.", chunk4SustainedEvidence);
            chunk4SustainedStage = 4; ResetLeafClock();
        }
    }
}
