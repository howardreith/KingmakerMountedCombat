using System;
using System.Linq;
using Kingmaker;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
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
        internal static bool IsChunk4SessionScenario(string scenario) => scenario == "chunk4-session-rt" || scenario == "chunk4-session-tb";
        private bool IsChunk4Session => IsChunk4SessionScenario(request.Scenario);
        private bool Chunk4SessionTb => request.Scenario == "chunk4-session-tb";
        private int chunk4SessionCycle;
        private int chunk4SessionStage;
        private bool chunk4SessionControlSent;
        private UnitMoveTo chunk4SessionSetupMove;
        private TurnController chunk4SessionSetupTurn;
        private JObject chunk4SessionEvidence;
        private JObject chunk4SessionSubscriptions;
        private string Chunk4SessionId => "C4-SESSION-" + (Chunk4SessionTb ? "TB-" : "RT-") + (chunk4SessionCycle + 1);

        private void BeginChunk4Session()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler ||
                settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Session fixture requires the accepted paired authority.");
            CaptureIdleFixturePartyForCleanup();
            ordinaryAttackTrace = new NativeOrdinaryAttackTrace(rider, horse, combat, () => relationship.State.ToString());
            chunk4IncomingObserver = new Chunk4IncomingRuleObserver(rider, horse);
            allocationTrace = new NativeActorAllocationTrace(rider, horse, combat);
            if (Chunk4SessionTb) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
            chunk4SessionSubscriptions = Chunk4SubscriptionSnapshot.Capture();
            observations["chunk4SessionSubscriptionsBefore"] = chunk4SessionSubscriptions;
            step = Phase3dHorseStep.Phase3gControls; ResetLeafClock();
        }

        private JObject CaptureChunk4SessionState() => new JObject {
            ["live"] = CaptureOrdinaryLiveState(), ["gameSeconds"] = Chunk4NativeSeconds,
            ["playerCombat"] = Game.Instance.Player.IsInCombat, ["riderCombat"] = rider.IsInCombat, ["mountCombat"] = horse.IsInCombat,
            ["tbActive"] = CombatController.IsInTurnBasedCombat(), ["tbInitialized"] = Game.Instance.TurnBasedCombatController.Initialized,
            ["currentActor"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit.UniqueId,
            ["targetLife"] = target == null ? null : CaptureChunk4LifeActor(target), ["records"] = combat.TrackedActorAllocations,
            ["privatePartner"] = combat.PairedPartnerContext?.Unit.UniqueId, ["identity"] = combat.PairedActivationIdentity,
            ["attachmentResidue"] = relationship.Runtime.HasPresentationAttachmentResidue,
            ["attachmentRestored"] = relationship.Runtime.PresentationAttachmentRestoreVerified,
            ["poseRestored"] = relationship.Runtime.PoseBaselineRestoreVerified
        };

        private void TickChunk4Session()
        {
            var game = Game.Instance; var turn = game.TurnBasedCombatController.CurrentTurn;
            observations["chunk4SessionProgress"] = new JObject { ["cycle"] = chunk4SessionCycle, ["stage"] = chunk4SessionStage,
                ["state"] = CaptureChunk4SessionState(), ["current"] = chunk4SessionEvidence };
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (chunk4SessionStage == 0)
            {
                if (chunk4SessionCycle == 3) { BeginCleanup(); return; }
                if (!Chunk4PairedPlayIdle || game.Player.IsInCombat || rider.IsInCombat || horse.IsInCombat) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!chunk4SessionControlSent) chunk4SessionControlSent = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "session-native-mount");
                    return;
                }
                if (!PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(Chunk4SessionTb);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                chunk4SessionEvidence = new JObject { ["level"] = "NATIVE INTEGRATION", ["caseId"] = Chunk4SessionId,
                    ["mode"] = Chunk4SessionTb ? "TB" : "RT", ["cycle"] = chunk4SessionCycle + 1,
                    ["inputKind"] = "native-mount-ordinary-pointer-combat-exit-dismount", ["mountedBeforeCombat"] = !game.Player.IsInCombat };
                BeginTarget(6f, Chunk4SessionId); ruleProbe.Arm(target, false);
                allocationTrace.BeginEncounter(Chunk4SessionId);
                chunk4IncomingObserver.BeginCase(Chunk4SessionId);
                chunk4SessionStage = 1; ResetLeafClock(); return;
            }
            if (chunk4SessionStage == 1)
            {
                if (!IsCombatReady(true) || CombatController.IsInTurnBasedCombat() != Chunk4SessionTb) return;
                if (Chunk4SessionTb && (turn?.Unit != rider || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing))
                { TryEndPhase3gFixtureTurn(turn); return; }
                if (!Chunk4PairedPlayIdle) return;
                var plan = new UnitAttack(target); plan.Init(rider);
                var radius = horse.View.Corpulence + target.View.Corpulence + plan.CreateFullAttack().Min(attack => attack.WeaponRange);
                var point = FindNativeAttackFixturePoint(horse, true, horse.Position, 0.25f, Math.Min(radius, 3.5f), "session-adjacency");
                chunk4SessionSetupTurn = turn;
                using (var input = new NativeOrdinaryAttackInput(point))
                {
                    input.Predict();
                    if (Chunk4SessionTb)
                    {
                        var cycles = 0;
                        while ((turn.EnabledFiveFootStep || turn.EnabledSingleActionMove) && cycles++ < 8) { input.Click(button: 1); input.Predict(); }
                    }
                    if (!input.Click()) return;
                }
                chunk4SessionSetupMove = horse.Commands.Move as UnitMoveTo;
                if (chunk4SessionSetupMove?.Executor != horse) throw new InvalidOperationException("Session setup did not retain native mount movement.");
                chunk4SessionStage = 2; ResetLeafClock(); return;
            }
            if (chunk4SessionStage == 2)
            {
                if (!chunk4SessionSetupMove.IsFinished || !Chunk4PairedPlayIdle || combat.LastGroundMoveResult == null || !combat.LastGroundMoveSlotRestored) return;
                if (chunk4SessionSetupMove.Result != UnitCommand.ResultType.Success || rider.CombatState.Cooldown.MoveAction > 0.001f)
                    throw new InvalidOperationException("Session setup movement failed or taxed the rider.");
                if (Chunk4SessionTb && ReferenceEquals(turn, chunk4SessionSetupTurn)) { TryEndPhase3gFixtureTurn(turn); return; }
                chunk4SessionStage = 3; ResetLeafClock(); return;
            }
            if (chunk4SessionStage == 3)
            {
                if (Chunk4SessionTb && (turn?.Unit != rider || ReferenceEquals(turn, chunk4SessionSetupTurn) ||
                    turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing)) { TryEndPhase3gFixtureTurn(turn); return; }
                if (!Chunk4PairedPlayIdle || !rider.CombatState.CanActInCombat || !horse.CombatState.CanActInCombat ||
                    rider.CombatState.Cooldown.StandardAction > 0.001f || rider.CombatState.Cooldown.MoveAction > 0.001f ||
                    horse.CombatState.Cooldown.StandardAction > 0.001f || horse.CombatState.Cooldown.MoveAction > 0.001f ||
                    game.HandsEquipmentController.IsUpdateScheduledFor(rider) || game.HandsEquipmentController.IsUpdateScheduledFor(horse)) return;
                ordinaryAttackTrace.BeginCase(Chunk4SessionId);
                var selected = chunk4SessionCycle == 1 ? horse : rider;
                SelectionManager.Instance.SelectUnit(selected.View, true, true, false);
                chunk4SessionEvidence["selectedFirst"] = selected.UniqueId;
                chunk4SessionEvidence["beforeOrdinary"] = CaptureChunk4SessionState();
                chunk4SessionEvidence["rulesBefore"] = chunk4IncomingObserver.Capture();
                if (!targetService.BeginExpectedAttackDispatch(target)) throw new InvalidOperationException("Session target cannot accept its ordinary native order.");
                using (var input = new NativeOrdinaryAttackInput(target))
                {
                    input.Predict(Chunk4SessionTb && selected == horse ? combat.PairedPartnerContext : turn);
                    if (!input.Click()) return;
                }
                chunk4SessionStage = 4; ResetLeafClock(); return;
            }
            if (chunk4SessionStage == 4)
            {
                if (!target.Descriptor.State.IsConscious) throw new InvalidOperationException("Session target died before the measured ordinary pair routines.");
                var riderAttack = ordinaryAttackTrace.StartedAttacks.FirstOrDefault(command => command.Executor == rider && command.IsFinished);
                var mountAttack = ordinaryAttackTrace.StartedAttacks.FirstOrDefault(command => command.Executor == horse && command.IsFinished);
                if (riderAttack == null || mountAttack == null || !chunk4IncomingObserver.AllAttacksResolved) return;
                foreach (var command in new[] { riderAttack, mountAttack })
                    if (!command.IsActed || command.GetAttackIndex() < 1 || command.GetAttackIndex() != command.AllAttacks.Count ||
                        command.Result != UnitCommand.ResultType.Success && ordinaryAttackTrace.NativeRecoveryInterrupt(command) == null)
                        throw new InvalidOperationException("Session ordinary order did not complete both real native attack routines.");
                if (ruleProbe.PairForcedD20Count != 0) throw new InvalidOperationException("Session observed a forced attack result.");
                chunk4SessionEvidence["riderRoutine"] = CaptureOrdinaryCommand(riderAttack);
                chunk4SessionEvidence["mountRoutine"] = CaptureOrdinaryCommand(mountAttack);
                chunk4SessionEvidence["riderPlan"] = riderAttack.AllAttacks.Count; chunk4SessionEvidence["riderCompleted"] = riderAttack.GetAttackIndex();
                chunk4SessionEvidence["mountPlan"] = mountAttack.AllAttacks.Count; chunk4SessionEvidence["mountCompleted"] = mountAttack.GetAttackIndex();
                chunk4SessionEvidence["beforeStop"] = CaptureChunk4SessionState();
                SelectionManager.Instance.Stop();
                chunk4SessionEvidence["afterStopInput"] = CaptureChunk4SessionState();
                foreach (var actor in new[] { "rider", "mount" }) foreach (var cost in new[] { "standard", "move", "swift" })
                    if (!JToken.DeepEquals(chunk4SessionEvidence["beforeStop"]["live"][actor][cost], chunk4SessionEvidence["afterStopInput"]["live"][actor][cost]))
                        throw new InvalidOperationException("Session Stop changed a genuine native actor cost.");
                chunk4SessionStage = 5; ResetLeafClock(); return;
            }
            if (chunk4SessionStage == 5)
            {
                if (!Chunk4PairedPlayIdle || !chunk4IncomingObserver.AllAttacksResolved) return;
                if (!target.IsPlayersEnemy || target.Descriptor.IsEssentialForGame || target.Descriptor.State.Immortality ||
                    target == game.Player.MainCharacter.Value) throw new InvalidOperationException("Session victory stimulus lacks the exact disposable enemy.");
                var requested = checked(target.Stats.HitPoints.ModifiedValue + target.Stats.Constitution.ModifiedValue +
                    target.Stats.TemporaryHitPoints.ModifiedValue - target.Damage + 16);
                chunk4SessionEvidence["beforeDeath"] = CaptureChunk4SessionState();
                var damage = Rulebook.Trigger(new RuleDealDamage(rider, target,
                    new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
                chunk4SessionEvidence["damageDispatches"] = 1; chunk4SessionEvidence["requestedDamage"] = requested;
                chunk4SessionEvidence["nativeDamage"] = damage.Damage; chunk4SessionEvidence["afterDamage"] = CaptureChunk4SessionState();
                if (target.Damage < target.Stats.HitPoints.ModifiedValue + target.Stats.Constitution.ModifiedValue)
                    throw new InvalidOperationException("Session native damage did not reach real death; no death flag is assigned.");
                chunk4SessionStage = 6; ResetLeafClock(); return;
            }
            if (chunk4SessionStage == 6)
            {
                // No LeaveCombat, EndCombat, memory removal or target destruction
                // is used to manufacture the observed native encounter exit.
                if (!target.Descriptor.State.IsDead || targetService.LifeTransitionCount < 1 || !Chunk4PairedPlayIdle ||
                    !chunk4IncomingObserver.AllAttacksResolved || game.Player.IsInCombat || rider.IsInCombat || horse.IsInCombat ||
                    CombatController.IsInTurnBasedCombat() || game.TurnBasedCombatController.Initialized) return;
                chunk4SessionEvidence["nativeEncounterExit"] = CaptureChunk4SessionState();
                chunk4SessionEvidence["nativeLifeTransitions"] = targetService.LifeTransitionCount;
                chunk4SessionControlSent = false; chunk4SessionStage = 7; ResetLeafClock(); return;
            }
            if (chunk4SessionStage == 7)
            {
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (relationship.State != RelationshipState.Unmounted)
                {
                    if (!chunk4SessionControlSent) chunk4SessionControlSent = TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "session-native-dismount");
                    return;
                }
                if (!Chunk4PairedPlayIdle || combat.TrackedActorAllocations != 0 || combat.PairedPartnerContext != null ||
                    relationship.Runtime.HasPresentationAttachmentResidue) return;
                chunk4SessionEvidence["afterDismount"] = CaptureChunk4SessionState();
                chunk4SessionEvidence["nativeTrace"] = ordinaryAttackTrace.CaptureCaseEvents(Chunk4SessionId);
                chunk4SessionEvidence["rulesAfter"] = chunk4IncomingObserver.Capture();
                if (!targetService.DestroyAndVerify()) return;
                targetService.Dispose(); targetService = null; target = null;
                if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                chunk4SessionStage = 8; ResetLeafClock(); return;
            }
            if (chunk4SessionStage == 8)
            {
                if (!Chunk4PairedPlayIdle || game.Player.IsInCombat || game.TurnBasedCombatController.Initialized || leafClock.Elapsed.TotalSeconds < 0.5d) return;
                if (!RestoreCombatMountRiderAiIsolation() || !RestoreUnmountedHorseAiIsolation())
                    throw new InvalidOperationException("Session AI leases did not restore after native victory and dismount.");
                combatMountRiderAiLease = null; unmountedHorseAiLease = null; unmountedHorseAiSettleRequested = false;
                var subscriptions = Chunk4SubscriptionSnapshot.Capture();
                Func<JObject, string[]> entries = snapshot => ((JArray)snapshot["entries"]).OfType<JObject>().Select(item =>
                    (string)item["interface"] + ":" + (string)item["type"] + ":" + (int)item["identity"]).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                if ((bool)subscriptions["executing"] || !entries(chunk4SessionSubscriptions).SequenceEqual(entries(subscriptions)) ||
                    combat.TrackedActorAllocations != 0 || combat.PairedPartnerContext != null || relationship.State != RelationshipState.Unmounted)
                    throw new InvalidOperationException("Repeated session retained actor records, private context or owned subscriptions.");
                chunk4SessionEvidence["subscriptionsAfter"] = subscriptions;
                chunk4SessionEvidence["recordsAfter"] = combat.TrackedActorAllocations;
                chunk4SessionEvidence["settledAfter"] = CaptureChunk4SessionState();
                AddRow(Chunk4SessionId, true, "Ordinary pair combat, native death/encounter exit and native dismount returned owned state to its measured baseline.", chunk4SessionEvidence);
                chunk4SessionCycle++; chunk4SessionStage = 0; chunk4SessionControlSent = false; chunk4SessionSetupMove = null;
                ResetLeafClock();
            }
        }
    }
}
