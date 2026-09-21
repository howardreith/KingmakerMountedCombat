using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool SuspendedCase => Checkpoint == "suspended";
        private bool HasRoundEffectFixture => RoundEffectCase || SuspendedCase;
        private NativeActorAllocationTrace delayTrace;
        private JObject delayCountsBefore;
        private int? delayOriginalInitiative;

        private void InstallDelayFixture()
        {
            Check(!Cold && !rider.IsInCombat, "P03-Delay-arrangement-is-pre-encounter-only");
            delayOriginalInitiative = rider.Stats.Initiative.BaseValue;
            rider.Stats.Initiative.BaseValue = 40;
            delayTrace = new NativeActorAllocationTrace(rider, mount, combat);
            delayTrace.BeginEncounter(request.RunId);
        }

        private void RestoreDelayInitiative()
        {
            if (!delayOriginalInitiative.HasValue) return;
            rider.Stats.Initiative.BaseValue = delayOriginalInitiative.Value;
            delayOriginalInitiative = null;
        }

        private JObject DelayCounts()
        {
            if (delayTrace == null) return null;
            var trace = delayTrace.Capture();
            Check((int)trace["dropped"] == 0 && (int)trace["observationErrors"] == 0,
                "P03-Delay-native-observation-is-complete");
            var rows = ((JArray)trace["events"]).OfType<JObject>().ToArray();
            var result = new JObject();
            foreach (var actor in new[] { rider, mount })
            {
                var counts = new JObject();
                foreach (var boundary in new[] { "clear-after", "round-state-after" })
                    counts[boundary] = rows.Count(e => (string)e["boundary"] == boundary &&
                        (string)e["state"]["actor"] == actor.UniqueId);
                result[actor == rider ? "rider" : "mount"] = counts;
            }
            return result;
        }

        private void CheckDelayRefresh(int increment, string label)
        {
            var current = DelayCounts();
            foreach (var actor in new[] { "rider", "mount" })
                foreach (var boundary in new[] { "clear-after", "round-state-after" })
                    Check((int)current[actor][boundary] == (int)delayCountsBefore[actor][boundary] + increment, label);
        }

        private void BeginDelayFixture()
        {
            RestoreDelayInitiative();
            roundEffectWaitTurn = null;
            stage = 110;
        }

        private void ContinueDelay()
        {
            CheckRoundEffects(1, "P03-suspended-save-retains-delivered-round-effect");
            if (Cold)
            {
                // Observation starts after semantic restoration. The native saved
                // buff, initiative, pending order and grant are the only inputs.
                delayTrace = new NativeActorAllocationTrace(rider, mount, combat);
                delayTrace.BeginEncounter(request.RunId);
                delayCountsBefore = DelayCounts();
            }
            Write("round-effect-retained", RoundEffects());
            Write("suspended-retained", CombatObservation());
            stage = 111;
        }

        private bool AdvanceDelayFixture(TurnController turn)
        {
            if (stage != 110 && stage != 111) return false;
            var game = Game.Instance;
            var controller = game.TurnBasedCombatController;
            if (turn == null || turn.Status != TurnController.TurnStatus.Preparing && !turn.IsActing) return true;
            if (turn.Unit == mount) throw new InvalidOperationException("Suspended partner received an independent grant.");
            if (turn.Unit != rider) { EndFixtureTurn(turn); return true; }
            if (!PairIdle) return true;
            if (stage == 110)
            {
                if (riderRoundEffect.RoundNumber == 0 && mountRoundEffect.RoundNumber == 0)
                { EndFixtureTurn(turn); return true; }
                if (!RoundEffectsReady(turn, 1)) return true;
                if (!game.SaveManager.IsSaveAllowed()) return true;
                CheckRoundEffects(1, "P03-native-round-effect-before-Delay");
                var sorted = controller.SortedUnits.ToList();
                var target = sorted.Skip(sorted.IndexOf(rider) + 1).FirstOrDefault(u =>
                    u != mount && u.IsDirectlyControllable && u.GetTimeToNextTurn() < controller.TimeToNextRound);
                Check(target != null && turn.CanDelay(), "P03-real-later-controllable-same-round-Delay-target");
                savedBoundary = turn; savedSequence = combat.PairedActivationSequence; savedRound = controller.RoundNumber;
                delayCountsBefore = DelayCounts();
                Write("round-effect-applied", RoundEffects());
                var before = CombatObservation(); before["delayTarget"] = target.UniqueId;
                before["targetWait"] = target.GetTimeToNextTurn(); before["nextRoundWait"] = controller.TimeToNextRound;
                Write("delay-request-before", before);
                turn.DelayInitiaive(target);
                Check(turn.Status == TurnController.TurnStatus.Delayed && !ReferenceEquals(turn, controller.CurrentTurn) &&
                    combat.PairedActivationSequence == savedSequence, "P03-native-Delay-suspends-existing-boundary");
                Write("delay-request-after", CombatObservation());
                RequestCombatSave();
                Check(stage == 4, "P03-real-save-request-queued-during-Delay");
                return true;
            }
            Check(!ReferenceEquals(turn, savedBoundary) && controller.RoundNumber == savedRound &&
                combat.PairedActivationSequence == savedSequence && !combat.PairedActorEnded(rider) &&
                !combat.PairedActorEnded(mount) && rider.CombatState.Cooldown.StandardAction == 0 &&
                rider.CombatState.Cooldown.MoveAction == 0 && mount.CombatState.Cooldown.StandardAction == 0 &&
                mount.CombatState.Cooldown.MoveAction == 0,
                "P03-native-same-round-resume-retains-unused-grant");
            CheckRoundEffects(1, "P03-Delay-resume-does-not-replay-native-buff");
            CheckDelayRefresh(0, "P03-Delay-resume-does-not-clear-or-refresh-native-actors");
            Write("suspended-grant-resumed", CombatObservation());
            var earlier = controller.SortedUnits.FirstOrDefault(u => u != rider && u != mount &&
                u.GetTimeToNextTurn() >= controller.TimeToNextRound);
            Check(earlier != null && turn.CanDelay(), "P03-real-cross-round-Delay-rejection-stimulus");
            turn.DelayInitiaive(earlier);
            Check(ReferenceEquals(turn, controller.CurrentTurn) && combat.PairedActivationSequence == savedSequence &&
                rider.CombatState.Cooldown.StandardAction == 0 && mount.CombatState.Cooldown.StandardAction == 0,
                "P03-cross-round-Delay-remains-rejected-after-load");
            CheckDelayRefresh(0, "P03-rejected-cross-round-Delay-does-not-refresh");
            Write("cross-round-delay-rejected", CombatObservation());
            savedBoundary = turn;
            stage = 6;
            return true;
        }

        private JObject DelayFailure()
        {
            var trace = delayTrace.Capture();
            var rows = ((JArray)trace["events"]).OfType<JObject>().ToArray();
            return new JObject { ["dropped"] = trace["dropped"], ["observationErrors"] = trace["observationErrors"],
                ["recent"] = new JArray(rows.Skip(Math.Max(0, rows.Length - 64))),
                ["roundEffects"] = riderRoundEffect == null ? null : RoundEffects() };
        }

        private void ValidateSuspendedCheckpoint(MountedSaveData data)
        {
            var saved = data.Combat;
            Check(saved.Paired.Activation.Suspended && !saved.Paired.Activation.Split &&
                !saved.Paired.Activation.Ending && !saved.Paired.Activation.Rider.Ended &&
                !saved.Paired.Activation.Mount.Ended && !saved.Paired.BoundaryIsCurrent &&
                saved.Paired.Boundary?.ActorId == data.Rider.Id && saved.Paired.Boundary.Status == 4 &&
                saved.Current?.ActorId != data.Rider.Id && saved.Current?.ActorId != data.Mount.Id &&
                data.Rider.Move == 0 && data.Mount.Standard == 0 && data.Mount.Move == 0,
                "P03-archive-retains-suspended-grant-and-native-pending-order");
        }
    }
}
