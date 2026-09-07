using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private bool pairedTransitionsStarted;
        private int pairedTransitionStage;
        private int pairedTransitionFrame;
        private NativeModeTransitionProbe pairedModeProbe;
        private readonly JArray pairedTransitionEvents = new JArray();
        private readonly JArray pairedTransitionMoves = new JArray();
        private JObject pairedTransitionMove;
        private JObject pairedDelayBefore;
        private JObject pairedModeBefore;
        private JObject pairedSplitBefore;
        private UnitEntityData pairedDelayTarget;
        private TurnController pairedTransitionTurn;
        private string pairedTransitionIdentity;
        private Vector3 pairedTransitionOrigin;
        private int pairedSplitRound;

        private void BeginPairedTransitionProbe()
        {
            pairedTransitionsStarted = true;
            pairedTransitionTurn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            pairedTransitionIdentity = combat.PairedActivationIdentity;
            observations["pairedTransitions"] = new JObject {
                ["level"] = "NATIVE INTEGRATION", ["events"] = pairedTransitionEvents,
                ["movements"] = pairedTransitionMoves, ["inputKind"] = "scripted-native-control-integration",
                ["policy"] = "mode exit forfeits pair; unused same-round Delay resumes grant; split retains participation"
            };
            ResetLeafClock();
        }

        private JObject RecordPairedTransition(string kind)
        {
            allocationTrace.Record(kind, rider);
            var trace = allocationTrace.Capture();
            var sample = new JObject { ["kind"] = kind, ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
                ["currentActor"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit.UniqueId,
                ["identity"] = combat.PairedActivationIdentity, ["rider"] = allocationTrace.Snapshot(rider),
                ["mount"] = allocationTrace.Snapshot(horse), ["traceSequence"] = ((JArray)trace["events"]).Count,
                ["riderClears"] = CountPairedTransitionBoundary(trace, rider, "clear-after"),
                ["mountClears"] = CountPairedTransitionBoundary(trace, horse, "clear-after"),
                ["riderEffects"] = CountPairedTransitionBoundary(trace, rider, "round-state-after"),
                ["mountEffects"] = CountPairedTransitionBoundary(trace, horse, "round-state-after") };
            pairedTransitionEvents.Add(sample);
            return sample;
        }

        private static int CountPairedTransitionBoundary(JObject trace, UnitEntityData actor, string boundary) =>
            ((JArray)trace["events"]).OfType<JObject>().Count(e => (string)e["boundary"] == boundary && (string)e["state"]["actor"] == actor.UniqueId);

        private void RequireNoPairedRefresh(JObject before, JObject after)
        {
            foreach (var name in new[] { "riderClears", "mountClears", "riderEffects", "mountEffects" })
                RequirePaired((int)before[name] == (int)after[name], "Transition replayed native preparation/effects: " + name);
        }

        private void TickPairedTransitions()
        {
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            var turn = controller.CurrentTurn;
            observations["pairedTransitions"]["stage"] = pairedTransitionStage;
            if (turn != null && pairedVisitedTurns.Add(turn))
            {
                RecordPairedTransition("transition-native-turn-observed");
                if (turn.Unit == horse && (pairedTransitionStage < 9 || controller.RoundNumber <= pairedSplitRound))
                    throw new InvalidOperationException("Transition gave the mount duplicate native participation.");
            }
            if (pairedTransitionStage != 3 && pairedTransitionStage != 4 && game.IsPaused) { game.IsPaused = false; return; }
            if (pairedTransitionStage == 0)
            {
                var sorted = controller.SortedUnits.ToList();
                pairedDelayTarget = sorted.Skip(sorted.IndexOf(rider) + 1).FirstOrDefault(actor => actor != horse &&
                    actor.GetTimeToNextTurn() < controller.TimeToNextRound);
                pairedDelayBefore = RecordPairedTransition("native-delay-input-before");
                if (pairedDelayTarget == null)
                {
                    pairedDelayTarget = sorted.First(actor => actor != rider && actor != horse);
                    turn.DelayInitiaive(pairedDelayTarget);
                    var rejected = RecordPairedTransition("native-delay-no-forward-target-rejected");
                    RequirePaired(ReferenceEquals(turn, controller.CurrentTurn) && (string)rejected["identity"] == pairedTransitionIdentity,
                        "Delay without a later native actor changed participation.");
                    RequireNoPairedRefresh(pairedDelayBefore, rejected);
                    pairedTransitionStage = 2;
                }
                else
                {
                    RequirePaired(turn.CanDelay(), "An unused paired grant cannot use native same-round Delay.");
                    turn.DelayInitiaive(pairedDelayTarget);
                    RecordPairedTransition("native-delay-input-after");
                    pairedTransitionStage = 1;
                }
                ResetLeafClock(); return;
            }
            if (pairedTransitionStage == 1)
            {
                if (turn == null || ReferenceEquals(turn, pairedTransitionTurn)) return;
                if (turn.Unit != rider) { EndAllocationNativeTurn(turn); return; }
                if (turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                var resumed = RecordPairedTransition("native-delay-existing-grant-resumed");
                RequirePaired((string)resumed["identity"] == pairedTransitionIdentity && horse.CombatState.Cooldown.StandardAction == 0f &&
                    horse.CombatState.Cooldown.MoveAction == 0f, "Native Delay failed to resume the existing unspent partner grant.");
                RequireNoPairedRefresh(pairedDelayBefore, resumed);
                RequirePaired((int)resumed["mount"]["reactions"] == (int)pairedDelayBefore["mount"]["reactions"],
                    "Delay renewed reactions without a native grant.");
                pairedTransitionTurn = turn; pairedTransitionStage = 2; ResetLeafClock(); return;
            }
            if (pairedTransitionStage == 2)
            {
                if (!PairedTransitionActorsIdle()) return;
                BeginPairedTransitionMove(3f, false, "partial-stop");
                pairedTransitionStage = 3; return;
            }
            if (pairedTransitionStage == 3)
            {
                if ((bool?)pairedTransitionMove["stopInput"] != true)
                {
                    if (HorizontalDistance(pairedTransitionOrigin, horse.Position) < 0.4f)
                    {
                        RequirePaired(movementCommand != null && !movementCommand.IsFinished, "Stop stimulus failed before partial movement.");
                        return;
                    }
                    game.IsPaused = true;
                    pairedTransitionMove["pauseBeforeStop"] = allocationTrace.Snapshot(horse);
                    SelectionManager.Instance.Stop();
                    pairedTransitionMove["stopInput"] = true;
                    pairedTransitionFrame = Time.frameCount;
                    RecordPairedTransition("paused-native-stop-input");
                    return;
                }
                if (Time.frameCount < pairedTransitionFrame + 3) return;
                if ((bool?)pairedTransitionMove["pausedStopVerified"] != true)
                {
                    var paused = allocationTrace.Snapshot(horse);
                    RequirePaired(game.IsPaused && (float)paused["move"] == (float)pairedTransitionMove["pauseBeforeStop"]["move"],
                        "Paused Stop changed movement debt.");
                    pairedTransitionMove["pausedStopVerified"] = true;
                    game.IsPaused = false;
                }
                if (!PairedTransitionActorsIdle()) return;
                FinishPairedTransitionMove(false, false);
                RequirePaired((float)pairedTransitionMove["distance"] < 2.9f, "Stop did not interrupt the partial native path.");
                var beforeReject = RecordPairedTransition("used-pair-delay-input-before");
                RequirePaired(!turn.CanDelay(), "Mount-only movement left principal Delay enabled.");
                turn.DelayInitiaive(pairedDelayTarget);
                var rejected = RecordPairedTransition("used-pair-delay-rejected");
                RequirePaired(ReferenceEquals(turn, controller.CurrentTurn) &&
                    (float)rejected["mount"]["move"] == (float)beforeReject["mount"]["move"] &&
                    (string)rejected["identity"] == (string)beforeReject["identity"], "Rejected Delay changed mount debt or grant.");
                RequireNoPairedRefresh(beforeReject, rejected);
                pairedModeBefore = RecordPairedTransition("native-mode-exit-before");
                pairedModeProbe = new NativeModeTransitionProbe(false);
                pairedModeProbe.DispatchTemporaryValue();
                pairedTransitionStage = 4; ResetLeafClock(); return;
            }
            if (pairedTransitionStage == 4)
            {
                if (CombatController.IsInTurnBasedCombat() || controller.Initialized) return;
                var exited = RecordPairedTransition("native-mode-exit-after");
                var elapsed = ((long)exited["gameTicks"] - (long)pairedModeBefore["gameTicks"]) / (double)TimeSpan.TicksPerSecond;
                foreach (var actor in new[] { "rider", "mount" })
                    RequirePaired((float)exited[actor]["standard"] + elapsed >= 6f - 0.0001f &&
                        (float)exited[actor]["move"] + elapsed >= (float)pairedModeBefore[actor]["move"],
                        "Native mode exit refunded pair resources.");
                RequireNoPairedRefresh(pairedModeBefore, exited);
                pairedModeProbe.DispatchRestoreAndRestoreRawCache();
                observations["pairedTransitions"]["modeSettingRestored"] = pairedModeProbe.PersistedValueUnchanged;
                pairedModeProbe.Dispose(); pairedModeProbe = null;
                game.IsPaused = false;
                pairedTransitionStage = 5; ResetLeafClock(); return;
            }
            if (pairedTransitionStage == 5)
            {
                if (!controller.Initialized || turn == null) return;
                if (turn.Unit != rider) { EndAllocationNativeTurn(turn); return; }
                if (turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                var renewed = RecordPairedTransition("native-mode-next-paired-grant");
                RequirePaired((long)renewed["gameTicks"] - (long)pairedModeBefore["gameTicks"] >= TimeSpan.TicksPerSecond * 6 &&
                    (string)renewed["identity"] != pairedTransitionIdentity,
                    "Mode switching produced an early or repeated paired grant.");
                foreach (var actor in new[] { "rider", "mount" })
                    RequirePaired((float)renewed[actor]["standard"] == 0f && (float)renewed[actor]["move"] == 0f &&
                        (int)renewed[actor + "Clears"] == (int)pairedModeBefore[actor + "Clears"] + 1 &&
                        (int)renewed[actor + "Effects"] == (int)pairedModeBefore[actor + "Effects"] + 1,
                        "Mode return did not prepare each actor exactly once after real recovery.");
                pairedTransitionTurn = turn;
                pairedTransitionIdentity = combat.PairedActivationIdentity;
                pairedTransitionStage = 6; ResetLeafClock(); return;
            }
            if (pairedTransitionStage == 6)
            {
                if (!PairedTransitionActorsIdle()) return;
                BeginPairedTransitionMove(1f, true, "five-foot-step");
                pairedTransitionStage = 7; return;
            }
            if (pairedTransitionStage == 7)
            {
                if (movementCommand != null && !movementCommand.IsFinished || !PairedTransitionActorsIdle()) return;
                FinishPairedTransitionMove(true, false);
                BeginPairedTransitionMove(0.75f, false, "ordinary-after-step-rejected");
                pairedTransitionStage = 8; return;
            }
            if (pairedTransitionStage == 8)
            {
                if (Time.frameCount < pairedTransitionFrame + 3 || movementCommand != null && !movementCommand.IsFinished || !PairedTransitionActorsIdle()) return;
                FinishPairedTransitionMove(false, true);
                pairedSplitBefore = RecordPairedTransition("native-dismount-input-before");
                pairedSplitRound = controller.RoundNumber;
                RequirePaired(TryNativeAbilityTargetClick(nativeControls.DismountAbility, rider, "paired-transition-dismount"), "Native dismount input rejected.");
                pairedTransitionStage = 9; ResetLeafClock(); return;
            }
            if (pairedTransitionStage == 9)
            {
                if (relationship.State != RelationshipState.Unmounted || !PairedTransitionActorsIdle()) return;
                var split = RecordPairedTransition("native-dismount-after");
                RequireNoPairedRefresh(pairedSplitBefore, split);
                RequirePaired((float)split["mount"]["move"] >= (float)pairedSplitBefore["mount"]["move"], "Dismount erased native movement debt.");
                pairedTransitionStage = 10; ResetLeafClock(); return;
            }
            if (pairedTransitionStage == 10)
            {
                if (turn == null) return;
                if (turn.Unit != horse) { EndAllocationNativeTurn(turn); return; }
                if (turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return;
                var independent = RecordPairedTransition("native-split-next-independent-mount-grant");
                RequirePaired(controller.RoundNumber > pairedSplitRound &&
                    (int)independent["mountClears"] == (int)pairedSplitBefore["mountClears"] + 1 &&
                    (int)independent["mountEffects"] == (int)pairedSplitBefore["mountEffects"] + 1 &&
                    (float)independent["mount"]["standard"] == 0f && (float)independent["mount"]["move"] == 0f,
                    "Split participation was duplicated or native next-round preparation failed.");
                observations["pairedTransitions"]["passed"] = true;
                var trace = allocationTrace.Capture();
                RequirePaired((int)trace["dropped"] == 0 && (int)trace["observationErrors"] == 0, "Transition observers lost native evidence.");
                AddRow("P02-paired-native-transitions", true, "Native Delay conservation, paused partial Stop, TB-RT-TB recovery, step restriction and dismount participation.",
                    (JObject)observations["pairedTransitions"].DeepClone());
                BeginCleanup();
            }
        }

        private bool PairedTransitionActorsIdle() => rider.Commands.Empty && horse.Commands.Empty &&
            !combat.HasActiveCommand && !combat.HasActiveGroundMovement && !rider.AreHandsBusyWithAnimation && !horse.AreHandsBusyWithAnimation;

        private void BeginPairedTransitionMove(float distance, bool stepMove, string purpose)
        {
            pairedTransitionOrigin = horse.Position;
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var destination = horse.Position + (horse.Position - target.Position).normalized * distance;
            pairedTransitionMove = new JObject { ["kind"] = "movement", ["purpose"] = purpose,
                ["before"] = allocationTrace.Snapshot(horse), ["riderBefore"] = allocationTrace.Snapshot(rider),
                ["requestedDistance"] = distance };
            pairedTransitionMoves.Add(pairedTransitionMove);
            using (var input = new NativeOrdinaryAttackInput(destination))
            {
                input.Predict(); var cycles = 0;
                while ((pairedTransitionTurn.EnabledFiveFootStep != stepMove || !stepMove && pairedTransitionTurn.EnabledSingleActionMove) && cycles++ < 5)
                { input.Click(button: 1); input.Predict(); }
                pairedTransitionMove["fiveFootStep"] = pairedTransitionTurn.EnabledFiveFootStep;
                pairedTransitionMove["clicked"] = input.Click();
            }
            RequirePaired((bool)pairedTransitionMove["fiveFootStep"] == stepMove, "Native movement cursor did not select the requested step policy.");
            movementCommand = horse.Commands.Move as UnitMoveTo;
            pairedTransitionMove["admitted"] = movementCommand != null;
            pairedTransitionFrame = Time.frameCount;
            RecordPairedTransition("transition-move-input"); ResetLeafClock();
        }

        private void FinishPairedTransitionMove(bool stepMove, bool rejected)
        {
            var after = allocationTrace.Snapshot(horse);
            var before = pairedTransitionMove["before"];
            var cost = (float)after["move"] - (float)before["move"];
            var time = (float)after["measuredAllowedTime"] - (float)before["measuredAllowedTime"];
            var distance = HorizontalDistance(pairedTransitionOrigin, horse.Position);
            var travelled = (float)after["measuredTravelDistance"] - (float)before["measuredTravelDistance"];
            var nativeShift = (float)after["measuredNativeShiftDistance"] - (float)before["measuredNativeShiftDistance"];
            pairedTransitionMove["after"] = after; pairedTransitionMove["riderAfter"] = allocationTrace.Snapshot(rider);
            pairedTransitionMove["nativeMoveCost"] = cost; pairedTransitionMove["nativeAllowedTime"] = time;
            pairedTransitionMove["distance"] = distance; pairedTransitionMove["travelledDistance"] = travelled;
            pairedTransitionMove["nativeShiftDistance"] = nativeShift; pairedTransitionMove["result"] = movementCommand?.Result.ToString();
            RequirePaired((float)pairedTransitionMove["riderAfter"]["move"] == (float)pairedTransitionMove["riderBefore"]["move"], "Transition movement charged rider transport.");
            if (rejected)
                RequirePaired(distance < 0.02f && travelled < 0.02f && nativeShift < 0.02f && Math.Abs(cost) < 0.001f && Math.Abs(time) < 0.001f,
                    "Native step restriction allowed ordinary movement or changed debt.");
            else
            {
                RequirePaired(movementCommand != null && distance > 0.02f && time > 0f && travelled >= distance - 0.02f &&
                    Math.Abs(travelled - nativeShift) < 0.35f && nativeShift <= time * (float)before["speedMps"] + 0.35f,
                    "Transition movement lacks actual native travel/time measurements.");
                RequirePaired(stepMove ? cost == 0f && (float)after["metresStepped"] > 0f &&
                    (float)after["metresStepped"] <= TurnController.MetersOfFiveFootStep + 0.01f && (float)after["standard"] == 0f :
                    cost > 0f && Math.Abs(cost - time) < 0.02f, "Native transition movement cost/step conservation failed.");
            }
            RecordPairedTransition("transition-move-finished");
        }
    }
}
