using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 7: the turn-based checkpoint moved on to the first rider's next turn, a real charge delivered through the ordinary
    // ability click, then the native save. The cold half holds the spent actions and the native penalty, with nothing replayed.
    // Moving the hostile onto a clear lane is disclosed setup.
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string ChargeCommitCaseName = "mounted-charge-tb-committed-manual";
        private bool ChargeCommitCase => request.PersistenceCase == ChargeCommitCaseName;
        private TurnController chargeEndedTurn;
        private MountedPairAttackCommand chargeCommitCommand;
        private Sprint6RuleObserver chargeRules;

        private JObject ChargeCommitState()
        {
            var buff = BlueprintRoot.Instance.SystemMechanics.ChargeBuff;
            var cooldown = firstRider.CombatState.Cooldown; var mountCooldown = firstMount.CombatState.Cooldown;
            var combat = world.ForActor(firstRider).Combat;
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            return new JObject { ["riderStandard"] = cooldown.StandardAction, ["riderMove"] = cooldown.MoveAction, ["mountStandard"] = mountCooldown.StandardAction, ["mountMove"] = mountCooldown.MoveAction,
                ["buffCount"] = firstRider.Buffs.Enumerable.Count(fact => fact.Blueprint == buff), ["riderAc"] = firstRider.Stats.AC.ModifiedValue, ["riderCharging"] = firstRider.Descriptor.State.IsCharging,
                ["mountCharging"] = firstMount.View.AgentASP.IsCharging, ["chargeOwned"] = combat.HasChargeOwnership, ["activeCommand"] = combat.HasActiveCommand, ["turnUnit"] = turn?.Unit?.UniqueId, ["turnStatus"] = turn?.Status.ToString(),
                ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber, ["rule"] = chargeRules?.Attacks.Count, ["available"] = firstRider.Descriptor.Abilities.GetAbility(controls.MountedChargeAbility)?.Data.IsAvailableForCast };
        }

        // Native End Turn, one turn at a time, until the first rider's own next turn is ready for input.
        private bool ChargeAdvanceToFirstRider()
        {
            var game = Game.Instance; var turn = game.TurnBasedCombatController.CurrentTurn;
            if (game.IsPaused) game.IsPaused = false;
            if (turn?.Unit == firstRider && !ReferenceEquals(turn, chargeEndedTurn) && Chunk7NativeFixture.ReadyForTurnInput(turn) && firstRider.Commands.Empty && firstMount.Commands.Empty) return true;
            if (turn?.Unit != null && turn.Unit.IsDirectlyControllable && !ReferenceEquals(turn, chargeEndedTurn) && turn.Unit.Commands.Empty && !turn.Unit.AreHandsBusyWithAnimation &&
                turn.CanEndTurnAndNoActing() && !game.TurnBasedCombatController.WaitingForUI && MountedPairTurnParticipant.NativeNextCandidate(game.TurnBasedCombatController) == null)
            {
                game.PauseBind();
                chargeEndedTurn = turn;
            }
            return false;
        }

        // Stage 60: reach the turn, place the lane and click. Stage 61: the delivered charge, then the save.
        private bool UpdateChargeCommit(bool delivered)
        {
            var combat = world.ForActor(firstRider).Combat;
            checkpoint.RefreshBeforeCommit();
            if (!delivered)
            {
                if (!ChargeAdvanceToFirstRider()) return false;
                var target = checkpoint.CombatTarget;
                if (chargeRules == null) chargeRules = new Sprint6RuleObserver(target);
                Require(MountedChargeFixture.TryPlaceOnLane(firstMount, firstRider, target, new[] { 12f, 10f, 14f, 9f, 16f, 8f }, out var lane), "No clear charge lane exists for the committed save.");
                var before = ChargeCommitState();
                var handler = Game.Instance.SelectedAbilityHandler; bool clicked;
                try { clicked = Sprint5Fixture.Click(controls, firstRider, controls.MountedChargeAbility, target); }
                finally { handler?.DropAbility(); }
                evidence["chargeCommit"] = new JObject { ["lane"] = lane, ["before"] = before, ["clicked"] = clicked, ["refusal"] = combat.LastMountedChargeRefusal };
                Require(clicked, "The ordinary charge click was refused: " + combat.ObserveMountedChargeTarget(firstRider, target).Reason);
                return true;
            }
            chargeCommitCommand = combat.OwnedChargeCommand ?? chargeCommitCommand;
            if (chargeCommitCommand == null || !chargeCommitCommand.IsFinished || combat.HasChargeOwnership) return false;
            var commit = (JObject)evidence["chargeCommit"];
            commit["result"] = chargeCommitCommand.Result.ToString(); commit["sequenceLawful"] = chargeCommitCommand.ChargeSequenceLawful; commit["lease"] = chargeCommitCommand.CaptureChargeLeaseEvidence();
            commit["rules"] = chargeRules.Attacks.DeepClone(); commit["after"] = ChargeCommitState();
            chargeRules.Dispose(); chargeRules = null;
            return true;
        }

        private void ObserveChargeCold()
        {
            chargeRules = new Sprint6RuleObserver(checkpoint.CombatTarget);
            evidence["chargeCold"] = new JObject { ["state"] = ChargeCommitState(), ["hasShell"] = world.ForActor(firstRider).Combat.OwnedChargeShell != null, ["castRequests"] = controls.NativeCastRequestCount };
        }

        // After the restored turn's native End: the first rider's next turn renews natively, and nothing was replayed in between.
        private bool UpdateChargeRenewal()
        {
            if (!ChargeAdvanceToFirstRider()) return false;
            evidence["chargeRenewal"] = new JObject { ["state"] = ChargeCommitState(), ["castRequests"] = controls.NativeCastRequestCount, ["rulesAfterLoad"] = chargeRules.Attacks.DeepClone() };
            chargeRules.Dispose(); chargeRules = null;
            return true;
        }
    }
}
