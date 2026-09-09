using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Native commands, damage, clocks and
    // released projectiles remain authoritative; targets are never replenished.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk4InterruptScenario(string scenario) =>
            scenario == "chunk4-interrupt-melee-rt" || scenario == "chunk4-interrupt-ranged-rt";
        private bool IsChunk4Interrupt => IsChunk4InterruptScenario(request.Scenario);
        private bool Chunk4InterruptRanged => request.Scenario == "chunk4-interrupt-ranged-rt";
        private string[] Chunk4InterruptCases => Chunk4InterruptRanged ?
            new[] { "pause-resume", "pause-stop-recover", "moving-target", "retarget-windup", "retarget-inflight", "target-death-windup", "target-death-inflight" } :
            new[] { "pause-resume", "pause-stop-recover", "moving-target", "retarget-windup", "target-death-windup", "target-death-midroutine" };
        private int chunk4InterruptCase;
        private int chunk4InterruptStage;
        private string Chunk4InterruptKind => Chunk4InterruptCases[chunk4InterruptCase];
        private string Chunk4InterruptId => "C4-INTERRUPT-" + (Chunk4InterruptRanged ? "ranged-" : "melee-") + Chunk4InterruptKind;
        private bool chunk4InterruptMountSent;
        private UnitEntityData chunk4InterruptOtherTarget;
        private DiagnosticCombatTargetService chunk4InterruptOtherService;
        private UnitMoveTo chunk4InterruptSetupMove;
        private UnitMoveTo chunk4InterruptTargetMove;
        private UnitAttack chunk4InterruptFirst;
        private JObject chunk4InterruptEvidence;
        private JObject chunk4InterruptPaused;
        private double chunk4InterruptPausedAt;
        private Vector3 chunk4InterruptTargetOrigin;
        private int chunk4InterruptFirstAttackCount;
        private bool Chunk4InterruptDeath => Chunk4InterruptKind.StartsWith("target-death-", StringComparison.Ordinal);
        private bool Chunk4InterruptRetarget => Chunk4InterruptKind.StartsWith("retarget-", StringComparison.Ordinal);

        private void BeginChunk4Interrupt()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Interruption fixture requires the sole accepted paired authority.");
            CaptureIdleFixturePartyForCleanup();
            observations["chunk4SubscriptionsBefore"] = Chunk4SubscriptionSnapshot.Capture();
            if (Chunk4InterruptRanged)
            {
                rangedWeaponLease = new Phase3dRangedWeaponLease(rider);
                rangedWeaponLease.Acquire(Kingmaker.Enums.WeaponCategory.Longbow);
            }
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString());
            chunk4IncomingObserver = new Chunk4IncomingRuleObserver(rider, horse);
            step = Phase3dHorseStep.Phase3gControls; ResetLeafClock();
        }

        private JObject CaptureChunk4InterruptState() => new JObject {
            ["live"] = CaptureOrdinaryLiveState(), ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["target"] = target == null ? null : CaptureChunk4LifeActor(target),
            ["otherTarget"] = chunk4InterruptOtherTarget == null ? null : CaptureChunk4LifeActor(chunk4InterruptOtherTarget),
            ["firstCommand"] = CaptureOrdinaryCommand(chunk4InterruptFirst),
            ["firstClock"] = chunk4InterruptFirst?.TimeSinceStart,
            ["firstIndex"] = chunk4InterruptFirst?.GetAttackIndex(), ["intent"] = combat.HasStockAttackIntent,
            ["nativeProjectiles"] = CaptureChunk4InterruptProjectiles()
        };

        private JArray CaptureChunk4InterruptProjectiles() => new JArray(Game.Instance.ProjectileController.Projectiles
            .Where(projectile => projectile.OnHitTrigger is Kingmaker.RuleSystem.Rules.RuleAttackWithWeaponResolve resolve &&
                (resolve.Initiator == rider || resolve.Initiator == horse)).Select(projectile => {
                    var resolve = (Kingmaker.RuleSystem.Rules.RuleAttackWithWeaponResolve)projectile.OnHitTrigger;
                    return new JObject { ["identity"] = RuntimeHelpers.GetHashCode(projectile),
                        ["attack"] = RuntimeHelpers.GetHashCode(resolve.AttackWithWeapon), ["actor"] = resolve.Initiator.UniqueId,
                        ["target"] = projectile.Target?.Unit?.UniqueId, ["hit"] = projectile.IsHit, ["hitWall"] = projectile.IsHitWall,
                        ["cleared"] = projectile.Cleared, ["destroyed"] = projectile.Destroyed,
                        ["activeView"] = projectile.View != null && projectile.View.activeInHierarchy };
                }));

        private void TickChunk4Interrupt()
        {
            var game = Game.Instance;
            chunk4InterruptOtherService?.ObserveTargetLifeState();
            chunk4InterruptOtherService?.RefreshBidirectionalCombatMemoryLease();
            observations["chunk4InterruptProgress"] = new JObject { ["case"] = chunk4InterruptCase,
                ["stage"] = chunk4InterruptStage, ["state"] = CaptureChunk4InterruptState(), ["current"] = chunk4InterruptEvidence };
            if (chunk4InterruptStage != 4 && game.IsPaused) { game.IsPaused = false; return; }
            if (chunk4InterruptStage == 0)
            {
                if (chunk4InterruptCase == Chunk4InterruptCases.Length) { BeginCleanup(); return; }
                if (!Chunk4PairedPlayIdle) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!chunk4InterruptMountSent) chunk4InterruptMountSent = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "interrupt-pre-combat-mount");
                    return;
                }
                if (rider.IsInCombat || horse.IsInCombat || !PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(false);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                BeginTarget(Chunk4InterruptRanged ? 12f : 6f, Chunk4InterruptId); ruleProbe.Arm(target, false);
                if (rider.GetFirstWeapon().Blueprint.IsRanged != Chunk4InterruptRanged)
                    throw new InvalidOperationException("Interruption fixture weapon does not match its parameter.");
                if (Chunk4InterruptDeath || Chunk4InterruptRetarget)
                {
                    chunk4InterruptOtherService = new DiagnosticCombatTargetService(logger, repeatedNativeSequences: true);
                    var point = FindWalkablePoint(rider.Position, Chunk4InterruptRanged ? 12f : 6f, 0.5f,
                        candidate => HorizontalDistance(candidate, target.Position) >= 3f &&
                            !Game.Instance.State.Units.Any(unit => unit.IsInState && unit.View != null &&
                                HorizontalDistance(candidate, unit.Position) < unit.View.Corpulence + 1f));
                    chunk4InterruptOtherTarget = chunk4InterruptOtherService.Spawn(rider, horse, point, request.RunId + "-" + Chunk4InterruptId + "-recovery", true, true);
                    if (!chunk4InterruptOtherService.PrepareForPlayerClick(chunk4InterruptOtherTarget) ||
                        !chunk4InterruptOtherService.QueueBidirectionalCombatMemory(rider, chunk4InterruptOtherTarget))
                        throw new InvalidOperationException("Interruption recovery target failed its native visibility/memory lease.");
                }
                chunk4InterruptEvidence = new JObject { ["level"] = "NATIVE INTEGRATION", ["caseId"] = Chunk4InterruptId,
                    ["kind"] = Chunk4InterruptKind, ["mode"] = "RT", ["ranged"] = Chunk4InterruptRanged,
                    ["target"] = target.UniqueId, ["otherTarget"] = chunk4InterruptOtherTarget?.UniqueId,
                    ["inputKind"] = "native-pointer-prediction-and-click", ["damageDispatches"] = 0 };
                chunk4InterruptStage = 1; ResetLeafClock(); return;
            }
            if (chunk4InterruptStage == 1)
            {
                if (!IsCombatReady(true) || CombatController.IsInTurnBasedCombat()) return;
                if (!Chunk4InterruptRanged && Chunk4InterruptKind != "moving-target" && chunk4InterruptSetupMove == null)
                {
                    var plan = new UnitAttack(target); plan.Init(rider);
                    var radius = horse.View.Corpulence + target.View.Corpulence + plan.CreateFullAttack().Min(attack => attack.WeaponRange);
                    var point = FindNativeAttackFixturePoint(horse, true, horse.Position, 0.25f, Math.Min(radius, 3.5f), "interrupt-adjacency");
                    using (var input = new NativeOrdinaryAttackInput(point)) { input.Predict(); if (!input.Click()) return; }
                    chunk4InterruptSetupMove = horse.Commands.Move as UnitMoveTo;
                    if (chunk4InterruptSetupMove?.Executor != horse) throw new InvalidOperationException("Interruption setup lost native mount movement.");
                    ResetLeafClock(); return;
                }
                if (chunk4InterruptSetupMove != null && !chunk4InterruptSetupMove.IsFinished || !Chunk4PairedPlayIdle ||
                    rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f ||
                    horse.CombatState.Cooldown.StandardAction > 0.001f || horse.CombatState.Cooldown.MoveAction > 0.001f ||
                    !rider.CombatState.CanActInCombat || !horse.CombatState.CanActInCombat ||
                    game.HandsEquipmentController.IsUpdateScheduledFor(rider) || game.HandsEquipmentController.IsUpdateScheduledFor(horse)) return;
                ordinaryAttackTrace.BeginCase(Chunk4InterruptId); chunk4IncomingObserver.BeginCase(Chunk4InterruptId);
                chunk4InterruptEvidence["before"] = CaptureChunk4InterruptState();
                chunk4InterruptEvidence["rulesBefore"] = chunk4IncomingObserver.Capture();
                chunk4InterruptFirstAttackCount = ((JArray)chunk4IncomingObserver.Capture()["attacks"]).Count;
                if (!targetService.BeginExpectedAttackDispatch(target)) throw new InvalidOperationException("Interruption target is not valid for native input.");
                using (var input = new NativeOrdinaryAttackInput(target)) { input.Predict(); if (!input.Click()) return; }
                chunk4InterruptStage = 2; ResetLeafClock(); return;
            }
            if (chunk4InterruptStage == 2)
            {
                if (chunk4InterruptFirst == null) chunk4InterruptFirst = ordinaryAttackTrace.LastStartedRiderAttack;
                var rules = chunk4IncomingObserver.Capture();
                var attacks = ((JArray)rules["attacks"]).OfType<JObject>().Skip(chunk4InterruptFirstAttackCount).ToArray();
                var inFlight = attacks.Where(item => (string)item["actor"] == rider.UniqueId &&
                    (string)item["target"] == target.UniqueId && (bool)item["projectile"] && !(bool)item["resolved"]).ToArray();
                if (Chunk4InterruptKind == "moving-target")
                {
                    if (!combat.HasActiveCommand && !combat.HasStockAttackIntent) return;
                    chunk4InterruptTargetOrigin = target.Position;
                    var point = FindWalkablePointAwayFromTarget(target.Position, horse.Position, 3f);
                    chunk4InterruptTargetMove = new UnitMoveTo(point) { CreatedByPlayer = true };
                    chunk4InterruptEvidence["beforeStimulus"] = CaptureChunk4InterruptState();
                    chunk4InterruptEvidence["targetMoveDestination"] = new JArray(point.x, point.y, point.z);
                    target.Commands.Run(chunk4InterruptTargetMove);
                    chunk4InterruptStage = 5; ResetLeafClock(); return;
                }
                if (chunk4InterruptFirst == null || !chunk4InterruptFirst.IsStarted || chunk4InterruptFirst.TimeSinceStart <= 0) return;
                var deliveryWindow = Chunk4InterruptKind.EndsWith("inflight", StringComparison.Ordinal) ? inFlight.Length > 0 :
                    Chunk4InterruptKind.EndsWith("midroutine", StringComparison.Ordinal) ?
                        chunk4InterruptFirst.GetAttackIndex() > 0 && chunk4InterruptFirst.GetAttackIndex() < chunk4InterruptFirst.AllAttacks.Count :
                        chunk4InterruptFirst.GetAttackIndex() == 0;
                if (!deliveryWindow)
                {
                    if (chunk4InterruptFirst.IsFinished || !Chunk4InterruptKind.EndsWith("inflight", StringComparison.Ordinal) &&
                        !Chunk4InterruptKind.EndsWith("midroutine", StringComparison.Ordinal))
                        throw new InvalidOperationException("Interruption fixture missed its requested native delivery window.");
                    return;
                }
                chunk4InterruptEvidence["beforeStimulus"] = CaptureChunk4InterruptState();
                chunk4InterruptEvidence["inFlightAtStimulus"] = new JArray(inFlight.Select(item => item.DeepClone()));
                chunk4InterruptEvidence["rulesAtStimulus"] = rules;
                if (Chunk4InterruptKind.StartsWith("pause-", StringComparison.Ordinal))
                {
                    game.IsPaused = true; chunk4InterruptPaused = CaptureChunk4InterruptState();
                    using (var input = new NativeOrdinaryAttackInput(target)) { input.Predict(); input.Click(); }
                    if (!JToken.DeepEquals(chunk4InterruptPaused, CaptureChunk4InterruptState()))
                        throw new InvalidOperationException("Paused same-target query/input restarted or spent a native command.");
                    chunk4InterruptPausedAt = clock.Elapsed.TotalSeconds; chunk4InterruptStage = 4; return;
                }
                if (Chunk4InterruptRetarget)
                {
                    if (!chunk4InterruptOtherService.BeginExpectedAttackDispatch(chunk4InterruptOtherTarget))
                        throw new InvalidOperationException("Retarget recovery target cannot accept native input.");
                    using (var input = new NativeOrdinaryAttackInput(chunk4InterruptOtherTarget)) { input.Predict(); if (!input.Click()) return; }
                }
                else if (Chunk4InterruptDeath) KillChunk4InterruptTarget();
                chunk4InterruptEvidence["afterStimulusInput"] = CaptureChunk4InterruptState();
                if (Chunk4InterruptDeath) AssertChunk4InterruptCostsEqual((JObject)chunk4InterruptEvidence["beforeStimulus"],
                    (JObject)chunk4InterruptEvidence["afterStimulusInput"]);
                chunk4InterruptStage = Chunk4InterruptDeath ? 6 : 5; ResetLeafClock(); return;
            }
            if (chunk4InterruptStage == 4)
            {
                if (!game.IsPaused || !JToken.DeepEquals(chunk4InterruptPaused, CaptureChunk4InterruptState()))
                    throw new InvalidOperationException("Paused native attack progressed, spent actions or moved.");
                if (clock.Elapsed.TotalSeconds - chunk4InterruptPausedAt < 0.4d) return;
                chunk4InterruptEvidence["pausedBegin"] = chunk4InterruptPaused;
                chunk4InterruptEvidence["pausedEnd"] = CaptureChunk4InterruptState();
                chunk4InterruptEvidence["pauseDuration"] = clock.Elapsed.TotalSeconds - chunk4InterruptPausedAt;
                if (Chunk4InterruptKind == "pause-stop-recover")
                {
                    SelectionManager.Instance.Stop();
                    chunk4InterruptEvidence["pausedAfterStop"] = CaptureChunk4InterruptState();
                    AssertChunk4InterruptCostsEqual(chunk4InterruptPaused, (JObject)chunk4InterruptEvidence["pausedAfterStop"]);
                }
                game.IsPaused = false;
                chunk4InterruptStage = Chunk4InterruptKind == "pause-stop-recover" ? 6 : 5;
                ResetLeafClock(); return;
            }
            if (chunk4InterruptStage == 6)
            {
                if (Chunk4InterruptDeath && !target.Descriptor.State.IsDead) return;
                if (!Chunk4PairedPlayIdle || !chunk4IncomingObserver.AllAttacksResolved) return;
                chunk4InterruptEvidence["afterInterruption"] = CaptureChunk4InterruptState();
                if (relationship.State != RelationshipState.Mounted || !rider.Descriptor.State.IsConscious || !horse.Descriptor.State.IsConscious)
                    throw new InvalidOperationException("Target interruption stranded or damaged the valid mounted relationship.");
                var recoveryTarget = chunk4InterruptOtherTarget ?? target;
                if (!(chunk4InterruptOtherService ?? targetService).BeginExpectedAttackDispatch(recoveryTarget))
                    throw new InvalidOperationException("Legal recovery target cannot accept native input after interruption.");
                using (var input = new NativeOrdinaryAttackInput(recoveryTarget)) { input.Predict(); if (!input.Click()) return; }
                chunk4InterruptEvidence["recoveryInput"] = CaptureChunk4InterruptState();
                chunk4InterruptStage = 5; ResetLeafClock(); return;
            }
            if (chunk4InterruptStage == 5)
            {
                var expectedTarget = Chunk4InterruptRetarget || Chunk4InterruptDeath ? chunk4InterruptOtherTarget : target;
                var final = ordinaryAttackTrace.StartedAttacks.LastOrDefault(command => command.Executor == rider && command.Target == expectedTarget &&
                    (Chunk4InterruptKind != "pause-stop-recover" || !ReferenceEquals(command, chunk4InterruptFirst)));
                if (final == null || !final.IsFinished || !final.IsActed || final.GetAttackIndex() < 1) return;
                var nativeRangeTail = final is KingmakerMountedCombat.Integration.MountedPairAttackCommand pairAttack && pairAttack.NativeRangedTailTermination;
                if (final.GetAttackIndex() != final.AllAttacks.Count && !nativeRangeTail ||
                    final.Result != UnitCommand.ResultType.Success && !nativeRangeTail && ordinaryAttackTrace.NativeRecoveryInterrupt(final) == null)
                    throw new InvalidOperationException("Recovery did not complete its native eligible attack routine.");
                chunk4InterruptEvidence["legalNativeRangedTail"] = nativeRangeTail;
                chunk4InterruptEvidence["legalNativeRecovery"] = ordinaryAttackTrace.NativeRecoveryInterrupt(final);
                chunk4InterruptEvidence["legalRangeRejection"] = ordinaryAttackTrace.NativeRangeRejection(final);
                if (chunk4InterruptTargetMove != null && !chunk4InterruptTargetMove.IsFinished) return;
                chunk4InterruptEvidence["legalCompletion"] = CaptureOrdinaryCommand(final);
                chunk4InterruptEvidence["legalCompletedAttacks"] = final.GetAttackIndex();
                chunk4InterruptEvidence["legalNativePlan"] = final.AllAttacks.Count;
                chunk4InterruptEvidence["targetMoved"] = chunk4InterruptTargetMove == null ? 0f : HorizontalDistance(chunk4InterruptTargetOrigin, target.Position);
                chunk4InterruptEvidence["targetMove"] = CaptureOrdinaryCommand(chunk4InterruptTargetMove);
                if (chunk4InterruptTargetMove != null && (chunk4InterruptTargetMove.Result != UnitCommand.ResultType.Success ||
                    (float)chunk4InterruptEvidence["targetMoved"] < 1f)) throw new InvalidOperationException("Native target movement did not make real progress.");
                chunk4InterruptEvidence["beforeStop"] = CaptureChunk4InterruptState();
                SelectionManager.Instance.Stop();
                chunk4InterruptEvidence["afterStopInput"] = CaptureChunk4InterruptState();
                AssertChunk4InterruptCostsEqual((JObject)chunk4InterruptEvidence["beforeStop"], (JObject)chunk4InterruptEvidence["afterStopInput"]);
                chunk4InterruptStage = 7; ResetLeafClock(); return;
            }
            if (chunk4InterruptStage == 7)
            {
                if (!Chunk4PairedPlayIdle || !chunk4IncomingObserver.AllAttacksResolved) return;
                chunk4InterruptEvidence["after"] = CaptureChunk4InterruptState();
                chunk4InterruptEvidence["nativeTrace"] = ordinaryAttackTrace.CaptureCaseEvents(Chunk4InterruptId);
                chunk4InterruptEvidence["rulesAfter"] = chunk4IncomingObserver.Capture();
                chunk4InterruptEvidence["targetLifeTransitions"] = targetService.LifeTransitionCount;
                if (ruleProbe.PairForcedD20Count != 0 || Chunk4InterruptDeath && (!target.Descriptor.State.IsDead || targetService.LifeTransitionCount < 1))
                    throw new InvalidOperationException("Interruption evidence lacks native death or contains a forced result.");
                AddRow(Chunk4InterruptId, true, "Ordinary native interruption, released delivery, real costs and subsequent legal work completed.", chunk4InterruptEvidence);
                chunk4InterruptStage = 8; ResetLeafClock(); return;
            }
            if (chunk4InterruptStage == 8)
            {
                TryLeaveCombat(chunk4InterruptOtherTarget); TryLeaveCombat(target); TryLeaveCombat(rider); TryLeaveCombat(horse);
                if (!CleanupChunk4InterruptOtherTarget()) return;
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) return;
                    targetService.Dispose(); targetService = null; target = null;
                }
                if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                if (game.Player.IsInCombat || game.TurnBasedCombatController.Initialized || CombatController.IsInTurnBasedCombat()) return;
                if (!RestoreCombatMountRiderAiIsolation() || !RestoreUnmountedHorseAiIsolation())
                    throw new InvalidOperationException("Interruption AI leases failed restoration.");
                combatMountRiderAiLease = null; unmountedHorseAiLease = null; unmountedHorseAiSettleRequested = false;
                observations["subscriptions-after-" + Chunk4InterruptId] = Chunk4SubscriptionSnapshot.Capture();
                observations["actor-records-after-" + Chunk4InterruptId] = combat.TrackedActorAllocations;
                chunk4InterruptCase++; chunk4InterruptStage = 0; chunk4InterruptSetupMove = chunk4InterruptTargetMove = null;
                chunk4InterruptFirst = null; chunk4InterruptEvidence = null; ResetLeafClock();
            }
        }

        private void KillChunk4InterruptTarget()
        {
            if (!target.IsPlayersEnemy || target.Descriptor.IsEssentialForGame || target.Descriptor.State.Immortality ||
                !target.Descriptor.State.IsConscious || target == Game.Instance.Player.MainCharacter.Value)
                throw new InvalidOperationException("Native target death stimulus requires the exact disposable hostile.");
            var amount = checked(target.Stats.HitPoints.ModifiedValue + target.Stats.Constitution.ModifiedValue +
                target.Stats.TemporaryHitPoints.ModifiedValue - target.Damage + 16);
            chunk4InterruptEvidence["damageDispatches"] = 1;
            chunk4InterruptEvidence["requestedDamage"] = amount;
            var damage = Rulebook.Trigger(new RuleDealDamage(rider, target,
                new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), amount))));
            chunk4InterruptEvidence["nativeDamage"] = damage.Damage;
            if (target.Damage < target.Stats.HitPoints.ModifiedValue + target.Stats.Constitution.ModifiedValue)
                throw new InvalidOperationException("Native damage failed to reach real target death; no life flag is assigned.");
        }

        private static void AssertChunk4InterruptCostsEqual(JObject before, JObject after)
        {
            foreach (var actor in new[] { "rider", "mount" }) foreach (var cost in new[] { "standard", "move", "swift" })
                if (!JToken.DeepEquals(before["live"][actor][cost], after["live"][actor][cost]))
                    throw new InvalidOperationException("Interruption input changed a real native cost synchronously.");
        }

        private bool CleanupChunk4InterruptOtherTarget()
        {
            if (chunk4InterruptOtherService == null) return true;
            if (!chunk4InterruptOtherService.DestroyAndVerify()) return false;
            chunk4InterruptOtherService.Dispose(); chunk4InterruptOtherService = null; chunk4InterruptOtherTarget = null;
            return true;
        }
    }
}
