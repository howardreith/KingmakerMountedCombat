using System;
using System.Linq;
using Kingmaker;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool pairedNativeConditionsStarted;
        private int pairedNativeConditionStage;
        private int pairedNativeConditionCase;
        private bool pairedNativeConditionMountInput;
        private PairedNativeConditionLease pairedNativeConditionLease;
        private UnitCommand pairedNativeConditionCommand;
        private TurnController pairedNativeConditionTurn;
        private JObject pairedNativeConditionEvidence;
        private JObject pairedNativeConditionSample;

        private void BeginPairedNativeConditions()
        {
            pairedTransitionsStarted = false;
            pairedNativeConditionsStarted = true;
            pairedNativeConditionEvidence = new JObject { ["level"] = "NATIVE INTEGRATION",
                ["cases"] = new JArray(), ["policy"] = "native condition ends its actor; forced split retains spent participation" };
            observations["pairedNativeConditions"] = pairedNativeConditionEvidence;
            ResetLeafClock();
        }

        private void CleanupPairedNativeCondition()
        {
            if (pairedNativeConditionLease == null) return;
            pairedNativeConditionLease.Dispose();
            pairedNativeConditionSample["stimulus"] = pairedNativeConditionLease.Evidence.DeepClone();
            pairedNativeConditionLease = null;
        }

        private void TickPairedNativeConditions()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            pairedNativeConditionEvidence["stage"] = pairedNativeConditionStage;
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (pairedNativeConditionStage == 0)
            {
                // Reuse the established ordinary-control encounter cleanup.
                // These actors were captured idle before the first fixture.
                if (turn != null) EndAllocationNativeTurn(turn);
                TryLeaveCombat(target); TryLeaveCombat(rider); TryLeaveCombat(horse);
                foreach (var member in allocationFixtureParty) TryLeaveCombat(member);
                if (targetService != null)
                {
                    if (!targetService.DestroyAndVerify()) return;
                    targetService.Dispose(); targetService = null; target = null;
                }
                if (pairedModeProbe != null) { pairedModeProbe.Dispose(); pairedModeProbe = null; }
                if (turnBasedModeProbe != null) { turnBasedModeProbe.Dispose(); turnBasedModeProbe = null; }
                if (CombatController.IsInTurnBasedCombat() || controller.Initialized || game.Player.IsInCombat ||
                    !PairedTransitionActorsIdle() || !rider.IsDirectlyControllable || !horse.IsDirectlyControllable) return;
                if (pairedNativeConditionCase == 2)
                {
                    pairedNativeConditionEvidence["passed"] = true;
                    AddRow("P05-paired-native-condition-commands", true,
                        "Native DoNothing/SelfHarm retain their exact grant through forced split, debit one actor and preserve principal input and subsequent participation.",
                        (JObject)pairedNativeConditionEvidence.DeepClone());
                    BeginCleanup(); return;
                }
                pairedNativeConditionMountInput = false;
                pairedNativeConditionStage = 1; ResetLeafClock(); return;
            }
            if (pairedNativeConditionStage == 1)
            {
                if (!PrepareUnmountedHorseAiIsolation() || !PrepareCombatMountRiderAiIsolation()) return;
                if (relationship.State != RelationshipState.Mounted)
                {
                    if (!pairedNativeConditionMountInput)
                    {
                        SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                        pairedNativeConditionMountInput = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "native-condition-pre-combat-mount");
                        RequirePaired(pairedNativeConditionMountInput, "Native condition fixture pre-combat Mount input refused.");
                    }
                    return;
                }
                if (!PairedTransitionActorsIdle()) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(true);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent)
                { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                var choice = pairedNativeConditionCase == 0 ? 30 : 60;
                pairedNativeConditionLease = new PairedNativeConditionLease(horse, combat, allocationTrace,
                    allocationRoundFacts.Single(fact => fact.Actor == horse), choice);
                pairedNativeConditionSample = new JObject { ["name"] = choice == 30 ? "mount-do-nothing" : "mount-self-harm",
                    ["outsideCombat"] = !rider.IsInCombat && !horse.IsInCombat && !game.Player.IsInCombat,
                    ["mountedBeforeCombat"] = relationship.State == RelationshipState.Mounted,
                    ["stimulus"] = pairedNativeConditionLease.Evidence, ["operations"] = new JArray(), ["visits"] = new JArray(),
                    ["beforeEncounter"] = RecordPairedTransition("native-condition-before-encounter") };
                ((JArray)pairedNativeConditionEvidence["cases"]).Add(pairedNativeConditionSample);
                pairedControlEvidence = pairedNativeConditionSample;
                pairedNativeConditionCommand = null; pairedNativeConditionTurn = null;
                BeginTarget(3.5f, "native-condition-" + pairedNativeConditionCase);
                ruleProbe.Arm(target, false);
                pairedNativeConditionStage = 2; ResetLeafClock(); return;
            }
            if (turn != null && pairedVisitedTurns.Add(turn))
            {
                ((JArray)pairedNativeConditionSample["visits"]).Add(new JObject {
                    ["actor"] = turn.Unit.UniqueId, ["round"] = controller.RoundNumber, ["frame"] = Time.frameCount });
                if (turn.Unit == horse && (pairedNativeConditionTurn == null ||
                    controller.RoundNumber <= (int)pairedNativeConditionSample["round"]))
                    throw new InvalidOperationException("Condition split produced a duplicate mount allocation.");
            }
            if (pairedNativeConditionStage == 2)
            {
                if (turn == null) return;
                if (turn.Unit != rider) { EndAllocationNativeTurn(turn); return; }
                if ((int)pairedNativeConditionLease.Evidence["conditionApplications"] != 1) return;
                pairedNativeConditionTurn = turn; pairedControlTurn = turn;
                pairedNativeConditionSample["round"] = controller.RoundNumber;
                pairedNativeConditionSample["activation"] = combat.PairedActivationIdentity;
                pairedNativeConditionCommand = horse.Get<UnitPartConfusion>()?.Cmd;
                if (pairedNativeConditionCommand == null) return;
                pairedNativeConditionSample["commandAtAdmission"] = CaptureOrdinaryCommand(pairedNativeConditionCommand);
                pairedNativeConditionSample["admission"] = RecordPairedTransition("native-condition-command-observed");
                RequirePaired(pairedNativeConditionCommand.Executor == horse && !pairedNativeConditionCommand.IsIgnoreCooldown,
                    "Native condition command lost its actor or bypassed native cooldowns.");
                pairedNativeConditionStage = 3; ResetLeafClock(); return;
            }
            if (pairedNativeConditionStage == 3)
            {
                if (!pairedNativeConditionCommand.IsFinished) return;
                var ended = RecordPairedTransition("native-condition-actor-ended");
                pairedNativeConditionSample["ended"] = ended;
                pairedNativeConditionSample["commandAtEnd"] = CaptureOrdinaryCommand(pairedNativeConditionCommand);
                pairedNativeConditionSample["samePrincipal"] = ReferenceEquals(turn, pairedNativeConditionTurn);
                pairedNativeConditionSample["mountEnded"] = combat.PairedActorEnded(horse);
                pairedNativeConditionSample["riderEnded"] = combat.PairedActorEnded(rider);
                RequirePaired(pairedNativeConditionCommand.Result == UnitCommand.ResultType.Success &&
                    (bool)pairedNativeConditionSample["samePrincipal"] && combat.PairedActorEnded(horse) && !combat.PairedActorEnded(rider) &&
                    (float)ended["mount"]["standard"] == 6f && (float)ended["mount"]["move"] == 3f &&
                    (float)ended["rider"]["standard"] == 0f && (float)ended["rider"]["move"] == 0f &&
                    (int)pairedNativeConditionLease.Evidence["choiceOverrides"] == 1 &&
                    (int)pairedNativeConditionLease.Evidence["nativeSelfDamageRules"] == pairedNativeConditionCase,
                    "Native condition did not end and debit exactly its actor while retaining the principal grant.");
                pairedNativeConditionStage = 4; ResetLeafClock(); return;
            }
            if (pairedNativeConditionStage == 4)
            {
                if (!PairedTransitionActorsIdle() || relationship.State != RelationshipState.Unmounted) return;
                pairedNativeConditionSample["forcedSplit"] = RecordPairedTransition("native-condition-forced-split");
                pairedNativeConditionSample["relationshipAfter"] = relationship.State.ToString();
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                BeginPairedOrdinaryAttack(rider, false);
                pairedNativeConditionStage = 5; return;
            }
            if (pairedNativeConditionStage == 5)
            {
                if (!FinishPairedOrdinaryAttack(rider, false)) return;
                pairedNativeConditionSample["beforeEndInput"] = RecordPairedTransition("native-condition-principal-end-input");
                EndAllocationNativeTurn(turn);
                pairedNativeConditionStage = 6; ResetLeafClock(); return;
            }
            if (pairedNativeConditionStage == 6)
            {
                if (turn == null || ReferenceEquals(turn, pairedNativeConditionTurn)) return;
                pairedNativeConditionSample["afterEnd"] = RecordPairedTransition("native-condition-next-actor");
                RequirePaired(turn.Unit != rider && (turn.Unit != horse || controller.RoundNumber > (int)pairedNativeConditionSample["round"]),
                    "Condition completion duplicated a native actor in the completed round.");
                pairedNativeConditionSample["nextActor"] = turn.Unit.UniqueId;
                pairedNativeConditionSample["nextRound"] = controller.RoundNumber;
                CleanupPairedNativeCondition();
                pairedNativeConditionSample["passed"] = true;
                pairedNativeConditionCase++; pairedNativeConditionStage = 0; ResetLeafClock();
            }
        }
    }
}
