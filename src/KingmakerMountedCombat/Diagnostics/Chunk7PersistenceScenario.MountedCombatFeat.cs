using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.ActivatableAbilities;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 6: the turn-based debt checkpoint with the first rider holding a spent use and a borrowed Swift action. The source
    // adds the feat directly, as setup: the native choice is the Sprint 6 row's. The rider is off-turn at the checkpoint, so one
    // native attack roll on the mount commits a real borrow. The cold half observes the same state with nothing refunded, the
    // debt paid once at the next native preparation, and a further legal reaction.
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string FeatDebtCaseName = "mounted-combat-feat-tb-debt-manual";
        private bool FeatDebtCase => request.PersistenceCase == FeatDebtCaseName || IntegratedCase;
        private const int FeatStrikeBound = 200;
        private int featChargesBefore, featAttemptsBefore;
        private TurnController featEndedTurn;

        private static JObject FeatState(MountedCombatDefenseState s) => new JObject { ["spent"] = s.Spent, ["owed"] = s.Owed, ["window"] = s.Window };

        private JObject FeatStrike(UnitEntityData attacker, MountedCombatDefenseService service)
        {
            MountedCombatDefenseRecord record = null;
            Action<MountedCombatDefenseRecord> observe = value => record = value;
            service.Observed += observe;
            try
            {
                var weapon = attacker.GetThreatHand()?.Weapon ?? attacker.Body.EmptyHandWeapon;
                for (var index = 0; index < FeatStrikeBound && record == null; index++)
                    Rulebook.Trigger(new RuleAttackRoll(attacker, firstMount, weapon, 0));
            }
            finally { service.Observed -= observe; }
            Require(record != null, "No attack roll hit the first mount within its bound.");
            return new JObject { ["commitment"] = record.Commitment.ToString(), ["refusal"] = record.Refusal.ToString(), ["attempted"] = record.Attempted, ["negated"] = record.Negated,
                ["riderTurn"] = record.RiderTurn, ["attackResult"] = record.AttackResult, ["checkResult"] = record.CheckResult, ["checkD20"] = record.CheckD20, ["checkDc"] = record.CheckDc,
                ["forced"] = record.Forced, ["swiftBefore"] = record.SwiftBefore, ["swiftAfter"] = record.SwiftAfter, ["mountSwift"] = firstMount.CombatState.Cooldown.SwiftAction,
                ["state"] = FeatState(record.StateAfter), ["stateAfter"] = FeatState(record.StateAfter) };
        }

        private void CommitFeatDebtBeforeSave()
        {
            var service = MountedCombatDefenseService.Current;
            Require(service != null && horses.MountedCombatFeature != null, "The Mounted Combat feat is unavailable: " + horses.MountedCombatFailure);
            Require(Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit != firstRider, "The source checkpoint must leave the first rider off-turn.");
            firstRider.Descriptor.Progression.Features.AddFeature(horses.MountedCombatFeature);
            Require(Mounted(firstRider, firstMount), "The first pair left its mounted checkpoint before the commit.");
            evidence["featDebt"] = FeatStrike(checkpoint.CombatTarget, service);
            var debt = (JObject)evidence["featDebt"];
            Require(debt.Value<string>("commitment") == "BorrowNextSwift", "The source commit was not an off-turn borrow: " + debt.Value<string>("refusal"));
        }

        private void ObserveFeatDebtCold()
        {
            var service = MountedCombatDefenseService.Current;
            Require(service != null && horses.MountedCombatFeature != null, "The Mounted Combat feat is unavailable after the load.");
            var toggle = firstRider.Descriptor.ActivatableAbilities.GetFact(horses.MountedCombatToggle) as ActivatableAbility;
            evidence["featCold"] = new JObject { ["hasFeat"] = firstRider.Descriptor.Progression.Features.HasFact(horses.MountedCombatFeature),
                ["preferenceOn"] = toggle?.IsOn, ["state"] = FeatState(service.ReadState(firstRider)), ["swift"] = firstRider.CombatState.Cooldown.SwiftAction,
                ["charges"] = service.SwiftChargeCount, ["attempts"] = service.AttemptCount, ["mounted"] = Mounted(firstRider, firstMount) };
            featChargesBefore = service.SwiftChargeCount; featAttemptsBefore = service.AttemptCount;
        }

        // Stage 21: native End Turn until the first rider's own next preparation. Stage 22: past that turn, a further attack.
        private bool UpdateFeatDebtRenewal(bool afterPreparation)
        {
            var service = MountedCombatDefenseService.Current;
            var game = Game.Instance; var turn = game.TurnBasedCombatController.CurrentTurn;
            if (game.IsPaused) game.IsPaused = false;
            if (!afterPreparation && turn?.Unit == firstRider && Chunk7NativeFixture.ReadyForTurnInput(turn) && firstRider.Commands.Empty)
            {
                evidence["featRenewal"] = new JObject { ["afterPreparation"] = FeatRenewalState(service), ["chargesAtPreparation"] = service.SwiftChargeCount - featChargesBefore };
                return true;
            }
            if (afterPreparation && turn?.Unit != firstRider && turn?.Unit != firstMount && !game.TurnBasedCombatController.WaitingForUI && firstRider.CombatState.CanActInCombat)
            {
                var renewal = (JObject)evidence["featRenewal"];
                var strike = FeatStrike(checkpoint.CombatTarget, service);
                foreach (var property in strike.Properties()) renewal[property.Name] = property.Value;
                game.IsPaused = true;
                return true;
            }
            if (turn?.Unit != null && turn.Unit.IsDirectlyControllable && !ReferenceEquals(turn, featEndedTurn) && turn.Unit.Commands.Empty && !turn.Unit.AreHandsBusyWithAnimation &&
                turn.CanEndTurnAndNoActing() && !game.TurnBasedCombatController.WaitingForUI && MountedPairTurnParticipant.NativeNextCandidate(game.TurnBasedCombatController) == null)
            {
                game.PauseBind();
                featEndedTurn = turn;
            }
            return false;
        }

        private JObject FeatRenewalState(MountedCombatDefenseService service)
        {
            var state = FeatState(service.ReadState(firstRider));
            state["swift"] = firstRider.CombatState.Cooldown.SwiftAction; state["hasSwift"] = firstRider.HasSwiftAction();
            state["mountSwift"] = firstMount.CombatState.Cooldown.SwiftAction;
            return state;
        }
    }
}
