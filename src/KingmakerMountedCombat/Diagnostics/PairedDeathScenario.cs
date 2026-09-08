using System;
using System.Linq;
using Kingmaker;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool pairedDeathStarted;
        private bool pairedDeathMountInput;
        private int pairedDeathStage;
        private TurnController pairedDeathTurn;
        private MountedPairAttackOutcome pairedDeathPriorOutcome;
        private JObject pairedDeathEvidence;
        private PairedConditionObserver pairedDeathObserver;

        private void BeginPairedDeathProbe()
        {
            // P05 has completed native shutdown and restored its conditions.
            pairedNativeConditionsStarted = false;
            pairedDeathStarted = true;
            pairedDeathEvidence = new JObject { ["level"] = "NATIVE INTEGRATION",
                ["inputKind"] = "labelled-native-damage-effect-stimulus", ["damageDispatches"] = 0 };
            observations["pairedDeath"] = pairedDeathEvidence;
            if (CombatController.IsInTurnBasedCombat() || Game.Instance.TurnBasedCombatController.Initialized ||
                Game.Instance.Player.IsInCombat || !PairedTransitionActorsIdle())
                throw new InvalidOperationException("Death fixture requires completed native encounter shutdown.");
            unmountedHorseAiLease.ReassertAfterNativeReset(new[] { horse });
            combatMountRiderAiLease.ReassertAfterNativeReset(new[] { rider });
            pairedDeathObserver = new PairedConditionObserver(rider, horse);
            ResetLeafClock();
        }

        private void CleanupPairedDeathProbe()
        {
            if (pairedDeathObserver == null) return;
            pairedDeathEvidence["nativeLifeEvents"] = pairedDeathObserver.Capture();
            pairedDeathObserver.Dispose(); pairedDeathObserver = null;
        }

        private void TickPairedDeathProbe()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            pairedDeathEvidence["stage"] = pairedDeathStage;
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (pairedDeathStage == 0)
            {
                if (!PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!pairedDeathMountInput)
                    {
                        SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                        pairedDeathMountInput = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "death-pre-combat-mount");
                        RequirePaired(pairedDeathMountInput, "Death fixture pre-combat Mount input refused.");
                    }
                    return;
                }
                if (!PairedTransitionActorsIdle()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent)
                { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                pairedDeathEvidence["beforeEncounter"] = RecordPairedTransition("death-before-encounter");
                pairedDeathEvidence["mountedBeforeCombat"] = !rider.IsInCombat && !horse.IsInCombat && !game.Player.IsInCombat;
                BeginTarget(3.5f, "paired-mount-death");
                pairedDeathStage = 1; ResetLeafClock(); return;
            }
            if (pairedDeathStage == 1)
            {
                if (turn == null) return;
                if (turn.Unit != rider) { EndAllocationNativeTurn(turn); return; }
                if (turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing || !PairedTransitionActorsIdle()) return;
                RequirePaired(combat.PairedPartnerContext != null && combat.PairedActivationSequence == 1,
                    "Death fixture did not reach its fresh authoritative paired preparation.");
                pairedDeathTurn = turn; pairedTransitionTurn = turn;
                pairedDeathEvidence["beforeMovement"] = RecordPairedTransition("death-before-movement");
                BeginPairedTransitionMove(0.75f, false, "death-partial-movement", towardTarget: true);
                pairedDeathStage = 2; return;
            }
            if (pairedDeathStage == 2)
            {
                if (!PairedTransitionActorsIdle()) return;
                FinishPairedTransitionMove(false, false);
                pairedDeathEvidence["movement"] = pairedTransitionMove.DeepClone();
                pairedTransitionMoves.Remove(pairedTransitionMove);
                pairedDeathEvidence["beforeAttack"] = RecordPairedTransition("death-before-mount-primary");
                pairedDeathPriorOutcome = combat.LastOutcome;
                ruleProbe.Arm(target, false);
                RequirePaired(targetService.BeginExpectedAttackDispatch(target), "Death fixture native attack dispatch refused.");
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                RequirePaired(TryNativeAbilityTargetClick(nativeControls.MountPrimaryAbility, target, "death-mount-primary"),
                    "Death fixture mount Primary input refused.");
                pairedDeathStage = 3; ResetLeafClock(); return;
            }
            if (pairedDeathStage == 3)
            {
                var outcome = combat.LastOutcome;
                if (ReferenceEquals(outcome, pairedDeathPriorOutcome) || combat.HasActiveCommand || !PairedTransitionActorsIdle()) return;
                var before = RecordPairedTransition("death-before-native-damage");
                pairedDeathEvidence["beforeDamage"] = before;
                var unrelatedOrder = controller.SortedUnits.Where(unit => unit != rider && unit != horse &&
                    unit.IsInState && unit.IsInCombat && !unit.Descriptor.State.IsDead).Select(unit => unit.UniqueId).ToArray();
                RequirePaired(unrelatedOrder.Length > 0, "Death fixture has no unrelated native successor.");
                pairedDeathEvidence["nativeUnrelatedOrderBefore"] = new JArray(unrelatedOrder);
                pairedDeathEvidence["expectedNextActor"] = unrelatedOrder[0];
                pairedDeathEvidence["attack"] = new JObject { ["actor"] = outcome.ActorId,
                    ["resourceOwner"] = outcome.ResourceOwnerId, ["result"] = outcome.Result,
                    ["nativeRule"] = outcome.NativeAttackRuleObserved, ["completedAttacks"] = outcome.NativeCompletedAttackCount };
                RequirePaired(ReferenceEquals(turn, pairedDeathTurn) && horse.Descriptor.State.IsConscious &&
                    outcome.ActorId == horse.UniqueId && outcome.ResourceOwnerId == horse.UniqueId &&
                    outcome.Result == "Success" && outcome.NativeAttackRuleObserved && outcome.NativeCompletedAttackCount == 1 &&
                    horse.CombatState.Cooldown.StandardAction == 6f && rider.CombatState.Cooldown.StandardAction == 0f &&
                    rider.CombatState.Cooldown.MoveAction == 0f && ruleProbe.MountNonOpportunityAttackRuleCount == 1,
                    "Death stimulus must follow real mount expenditure in the same paired grant.");
                var lethal = horse.Stats.HitPoints.ModifiedValue + horse.Stats.Constitution.ModifiedValue +
                    horse.Stats.TemporaryHitPoints.ModifiedValue + 1;
                pairedDeathEvidence["requestedDamage"] = lethal;
                pairedDeathEvidence["sourceActor"] = target.UniqueId;
                pairedDeathEvidence["targetActor"] = horse.UniqueId;
                pairedDeathEvidence["riderDamageBefore"] = rider.Damage;
                pairedDeathEvidence["damageDispatches"] = 1;
                allocationTrace.Record("death-native-damage-dispatch", horse);
                var damage = Rulebook.Trigger(new RuleDealDamage(target, horse,
                    new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), lethal))));
                pairedDeathEvidence["nativeDamage"] = damage.Damage;
                pairedDeathEvidence["afterDamageDispatch"] = RecordPairedTransition("death-native-damage-returned");
                pairedDeathStage = 4; ResetLeafClock(); return;
            }
            if (pairedDeathStage == 4)
            {
                if (!horse.Descriptor.State.IsDead || relationship.State != RelationshipState.Unmounted ||
                    turn == null || ReferenceEquals(turn, pairedDeathTurn)) return;
                pairedDeathEvidence["afterRemoval"] = RecordPairedTransition("death-next-unrelated-actor");
                pairedDeathEvidence["nativeLifeEvents"] = pairedDeathObserver.Capture();
                pairedDeathEvidence["riderDamageAfter"] = rider.Damage;
                RequirePaired(turn.Unit.UniqueId == (string)pairedDeathEvidence["expectedNextActor"] &&
                    turn.Unit != horse && turn.Unit != rider && combat.PairedActivationIdentity == null &&
                    !combat.HasActiveCommand && !combat.HasActiveGroundMovement,
                    "Native mount removal did not retire the pair and advance to an unrelated actor.");
                RequirePaired(rider.Damage == (int)pairedDeathEvidence["riderDamageBefore"],
                    "Native mount death changed the rider's independent health.");
                RequireNoPairedRefresh((JObject)pairedDeathEvidence["beforeDamage"], (JObject)pairedDeathEvidence["afterRemoval"]);
                pairedDeathEvidence["mountDead"] = horse.Descriptor.State.IsDead;
                pairedDeathEvidence["relationshipAfter"] = relationship.State.ToString();
                CleanupPairedDeathProbe();
                pairedDeathStage = 5;
                pairedDeathEvidence["passed"] = true;
                AddRow("P06-paired-native-mount-death", true,
                    "Native lethal damage after mount expenditure finalizes both grants before native removal and unrelated selection.",
                    (JObject)pairedDeathEvidence.DeepClone());
                BeginCleanup();
            }
        }
    }
}
