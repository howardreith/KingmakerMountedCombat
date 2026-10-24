using System;
using System.Linq;
using Kingmaker;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using TurnBased.Controllers;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // 7E X1 C03 attack modes (owner-seed-attack-modes, on the TB rider-first engine). Rider A's own
    // native Rapid Shot (feat and toggle granted natively in the owner's save; nothing is granted
    // here) is switched only through the native action-bar slot click. Rider A makes three native
    // ordinary full attacks on the diagnostic target from where it stands:
    //   round N    mounted, Rapid Shot off;
    //   round N+1  mounted, Rapid Shot on;
    //   round N+2  native Dismount (the pair's own first action), turn ended natively;
    //   round N+3  unmounted control, Rapid Shot on.
    // The native engine owns every plan, roll, cost and turn; the scenario only clicks and observes.
    internal sealed partial class Chunk7CombatScenario
    {
        internal const string RapidShotFeatureGuid = "9c928dc570bb9e54a9649b3ebfe47a41";
        internal const string RapidShotToggleGuid = "90a77bfe25ec2e14caf8bd5cde9febf2";
        internal const string RapidShotBuffGuid = "0f310c1e709e15e4fa693db15a4baeb4";
        private bool AttackModesCase => request.OwnedPairs?.Case == Chunk7OwnedPairsSelection.OwnerSeedAttackModesCase;
        private Chunk7AttackModesRuleObserver attackModesRules;
        private ActivatableAbility attackModesRapid;
        private JObject attackModes, attackModeObservation;
        private UnitAttack attackModeCommand;
        private int attackModesStage, attackModesRound;
        private TurnController attackModesDismountTurn;
        private JObject RapidState(string label) => new JObject { ["label"] = label, ["toggle"] = attackModesRapid.Blueprint.AssetGuid,
            ["isOn"] = attackModesRapid.IsOn, ["isRunning"] = attackModesRapid.IsRunning,
            ["buff"] = ar.Buffs.Enumerable.Any(buff => buff.Blueprint.AssetGuid == RapidShotBuffGuid), ["inCombat"] = ar.IsInCombat,
            ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber };
        // Out of combat, before the encounter: rider A's own native facts are verified and Rapid Shot is
        // switched off through the native slot if the save left it on. A missing feat, toggle or ranged
        // weapon is refused before any input and records the behavior as uncovered.
        private void PrepareAttackModes()
        {
            var feat = ar.Descriptor.Progression.Features.Enumerable.Count(fact => fact.Blueprint.AssetGuid == RapidShotFeatureGuid);
            var toggles = ar.ActivatableAbilities.Enumerable.Where(item => item.Blueprint.AssetGuid == RapidShotToggleGuid).ToArray();
            var weapon = ar.GetFirstWeapon();
            attackModes = new JObject { ["contract"] = "owner-seed-attack-modes-v1", ["riderId"] = ar.UniqueId, ["mountId"] = am.UniqueId,
                ["rapidShotFeature"] = feat, ["rapidShotToggles"] = toggles.Length, ["weapon"] = weapon?.Blueprint.AssetGuid,
                ["weaponRanged"] = weapon?.Blueprint.IsRanged == true, ["weaponRangeMeters"] = weapon?.Blueprint.AttackRange.Meters };
            evidence["attackModes"] = attackModes;
            Require(feat == 1 && toggles.Length == 1 && weapon != null && weapon.Blueprint.IsRanged,
                "Rider A lacks its native Rapid Shot feat, toggle or equipped ranged weapon; C03 attack modes are uncovered on this fixture.");
            attackModesRapid = toggles[0];
            attackModes["rapidBefore"] = RapidState("save");
            if (attackModesRapid.IsOn) ToggleRapidShot(false, "pre-combat-off");
            attackModes["rapidOff"] = RapidState("pre-combat");
            Require(!attackModesRapid.IsOn && !(bool)attackModes["rapidOff"]["buff"], "Native Rapid Shot did not turn off out of combat.");
        }
        // The native player input: MechanicActionBarSlotActivableAbility.OnClick toggles IsOn when the
        // slot is possible (native turn-based gate included). Nothing else touches the toggle.
        private void ToggleRapidShot(bool on, string label)
        {
            var slot = new MechanicActionBarSlotActivableAbility { ActivatableAbility = attackModesRapid, Unit = ar };
            var row = new JObject { ["label"] = label, ["before"] = RapidState(label + "-before"), ["possible"] = slot.IsPossibleActive };
            slot.OnClick();
            row["after"] = RapidState(label + "-after");
            var clicks = (JArray)(attackModes["toggleClicks"] ?? (attackModes["toggleClicks"] = new JArray())); clicks.Add(row);
            Require(attackModesRapid.IsOn == on, "Native Rapid Shot slot click did not switch the toggle " + (on ? "on" : "off") + ".");
        }
        private void BeginAttackModes()
        {
            Require(AttackModesCase && OwnerSeedDerived && SelectedScenario == RiderFirst && attackModesRapid != null, "Attack modes admission differs.");
            attackModesRules = new Chunk7AttackModesRuleObserver(ar, am, target);
            attackModes["measurements"] = new JArray();
            attackModesStage = 0; stepStarted = clock.Elapsed.TotalSeconds;
        }
        private void AttackModesStage(int value) { attackModesStage = value; stepStarted = clock.Elapsed.TotalSeconds; }
        private void BeginFullAttack(string label)
        {
            var mounted = a.Relationship.State == RelationshipState.Mounted;
            var observation = new JObject { ["label"] = label, ["mounted"] = mounted, ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber,
                ["rapid"] = RapidState(label), ["before"] = WorldSnapshot(label + "-before"), ["ruleCountBefore"] = attackModesRules.EventCount,
                ["traceSequenceBefore"] = trace.EventCount, ["pairState"] = a.Relationship.State.ToString() };
            attackModeObservation = observation; ((JArray)attackModes["measurements"]).Add(observation);
            Require(targetService.BeginExpectedAttackDispatch(target), "Exact diagnostic target attack authorization refused.");
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(ar);
            bool clicked;
            using (var input = new NativeOrdinaryAttackInput(target)) { input.Predict(); clicked = input.Click(); }
            observation["clicked"] = clicked;
            attackModeCommand = ar.Commands.Raw.OfType<UnitAttack>().FirstOrDefault(command => ReferenceEquals(command.Target, target));
            Game.Instance.IsPaused = false;
            Require(clicked, "Native ordinary attack click on the target was refused.");
        }
        private static JObject PlanEntry(AttackHandInfo info, int index) => new JObject { ["index"] = index,
            ["weapon"] = info.Weapon?.Blueprint.AssetGuid, ["ranged"] = info.Weapon?.Blueprint.IsRanged == true,
            ["natural"] = info.Weapon?.Blueprint.IsNatural == true, ["owner"] = info.Hand?.Owner?.Unit?.UniqueId,
            ["iterativePenalty"] = info.AttackBonusPenalty };
        private bool FinishFullAttack()
        {
            if (attackModeCommand == null)
                attackModeCommand = ar.Commands.Raw.OfType<UnitAttack>().FirstOrDefault(started => ReferenceEquals(started.Target, target));
            if (attackModeCommand == null || !attackModeCommand.IsFinished || a.Combat.HasActiveCommand || a.Combat.HasActiveGroundMovement ||
                !ar.Commands.Empty || !am.Commands.Empty || ar.AreHandsBusyWithAnimation || am.AreHandsBusyWithAnimation) return false;
            var observation = attackModeObservation; var command = attackModeCommand; attackModeCommand = null;
            var after = WorldSnapshot((string)observation["label"] + "-after"); observation["after"] = after;
            var plan = command.AllAttacks.Select(PlanEntry).ToArray();
            observation["command"] = new JObject { ["identity"] = trace.ObjectIdentity(command), ["executor"] = command.Executor?.UniqueId,
                ["target"] = command.Target?.UniqueId, ["fullAttack"] = command.IsFullAttack, ["singleAttack"] = command.IsSingleAttack,
                ["result"] = command.Result.ToString(), ["completedAttacks"] = command.GetAttackIndex(), ["plan"] = new JArray(plan) };
            observation["rules"] = attackModesRules.Since((int)observation["ruleCountBefore"]);
            observation["traceSequenceAfter"] = trace.EventCount;
            observation["rapidAfter"] = RapidState((string)observation["label"] + "-after");
            var rolls = ((JArray)observation["rules"]).OfType<JObject>().Where(e => (string)e["kind"] == "roll").ToArray();
            var riderRolls = rolls.Where(e => (string)e["actor"] == ar.UniqueId && !(bool)e["opportunity"]).ToArray();
            var rangedPlan = plan.Count(e => (bool)e["ranged"] && (string)e["owner"] == ar.UniqueId);
            var before = (JObject)observation["before"];
            Require(command.Executor == ar && command.IsFullAttack && rangedPlan >= 1 && riderRolls.Length == rangedPlan &&
                riderRolls.All(e => (bool)e["ranged"]) && rolls.All(e => (string)e["actor"] == ar.UniqueId) &&
                (command.Result == UnitCommand.ResultType.Success || command.Result == UnitCommand.ResultType.Interrupt) &&
                plan.Skip(rangedPlan).All(e => !(bool)e["ranged"]) &&
                (float)Actor(after, ar)["standard"] > (float)Actor(before, ar)["standard"] &&
                SameActorBudget(Actor(before, am), Actor(after, am)) && new[] { br, bm }.All(actor => SameActorBudget(Actor(before, actor), Actor(after, actor))),
                "Native full attack escaped its exact plan, rolls or actor allocation: " + observation["command"].ToString(Newtonsoft.Json.Formatting.None));
            return true;
        }
        private static int[] InnerBonuses(JObject observation) => ((JArray)observation["rules"]).OfType<JObject>()
            .Where(e => (string)e["kind"] == "roll" && !(bool)e["opportunity"]).Select(e => (int)e["nativeCalculation"]["innerResult"]).ToArray();
        private static int RapidBonus(JObject observation) => ((JArray)observation["rules"]).OfType<JObject>()
            .Where(e => (string)e["kind"] == "roll").SelectMany(e => ((JArray)e["bonuses"] ?? new JArray()).OfType<JObject>())
            .Where(b => (string)b["source"] == RapidShotBuffGuid).Select(b => (int)b["bonus"]).DefaultIfEmpty(0).Distinct().Single();
        private void TickAttackModes()
        {
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            attackModes["stage"] = attackModesStage;
            if (attackModesStage == 0)
            {
                if (!ReadyPrincipal(a)) { if (turn?.Unit != target) EndNative(turn); return; }
                attackModesRound = controller.RoundNumber; attackModes["firstRound"] = attackModesRound;
                actingPair = a; actingIndex = 0; actingTurn = turn;
                Require(!attackModesRapid.IsOn, "Rapid Shot must be off for the first mounted full attack.");
                BeginFullAttack("mounted-off"); AttackModesStage(1); return;
            }
            if (attackModesStage == 1)
            {
                if (!FinishFullAttack()) return;
                var off = (JObject)attackModes["measurements"][0];
                Require(RapidBonus(off) == 0, "A Rapid Shot bonus source applied while the native toggle was off.");
                Pass("C7-combat-attack-modes-mounted-off");
                // Switched on in the same activation, after the attack, so the native toggle runs into N+1.
                ToggleRapidShot(true, "round-n-on"); AttackModesStage(2); return;
            }
            if (attackModesStage == 2)
            {
                if (ReferenceEquals(turn, actingTurn)) { EndNative(turn); return; }
                Require(controller.RoundNumber <= attackModesRound + 1, "Attack modes missed the next native round.");
                if (controller.RoundNumber != attackModesRound + 1 || !ReadyPrincipal(a)) { if (turn?.Unit != target) EndNative(turn); return; }
                actingTurn = turn; BeginFullAttack("mounted-on"); AttackModesStage(3); return;
            }
            if (attackModesStage == 3)
            {
                if (!FinishFullAttack()) return;
                var measurements = (JArray)attackModes["measurements"];
                var off = InnerBonuses((JObject)measurements[0]); var on = InnerBonuses((JObject)measurements[1]); var rapid = RapidBonus((JObject)measurements[1]);
                var adjusted = on.Select(value => value - rapid).OrderBy(value => value).ToArray();
                var expected = off.Concat(new[] { off.Max() }).OrderBy(value => value).ToArray();
                attackModes["rapidComparison"] = new JObject { ["off"] = new JArray(off), ["on"] = new JArray(on), ["rapidBonus"] = rapid };
                Require(rapid < 0 && on.Length == off.Length + 1 && adjusted.SequenceEqual(expected),
                    "Native Rapid Shot did not add exactly one attack at the highest bonus with its own uniform bonus source.");
                Pass("C7-combat-attack-modes-mounted-on"); AttackModesStage(4); return;
            }
            if (attackModesStage == 4)
            {
                if (ReferenceEquals(turn, actingTurn)) { EndNative(turn); return; }
                Require(controller.RoundNumber <= attackModesRound + 2, "Attack modes missed the dismount round.");
                if (controller.RoundNumber != attackModesRound + 2 || !ReadyPrincipal(a)) { if (turn?.Unit != target) EndNative(turn); return; }
                attackModesDismountTurn = turn; attackModes["dismountBefore"] = WorldSnapshot("attack-modes-dismount-before");
                BeginRelationship(ar, ar, controls.DismountAbility, br, "attack-modes-native-dismount-a"); AttackModesStage(5); return;
            }
            if (attackModesStage == 5)
            {
                if (a.Relationship.State != RelationshipState.Unmounted || !RelationshipSettled()) return;
                attackModes["dismountAfter"] = WorldSnapshot("attack-modes-dismount-after"); AttackModesStage(6); return;
            }
            if (attackModesStage == 6)
            {
                if (ReferenceEquals(turn, attackModesDismountTurn)) { EndNative(turn); return; }
                Require(controller.RoundNumber <= attackModesRound + 3, "Attack modes missed the unmounted control round.");
                if (controller.RoundNumber != attackModesRound + 3 || turn?.Unit != ar || !Chunk7NativeFixture.ReadyForTurnInput(turn) || !ar.Commands.Empty || ar.AreHandsBusyWithAnimation)
                { if (turn?.Unit != target && turn?.Unit != ar) EndNative(turn); return; }
                Require(attackModesRapid.IsOn && world.Registry.FindActive(ar) == null, "Unmounted control lost its Rapid Shot toggle or is still paired.");
                actingTurn = turn; BeginFullAttack("unmounted-on"); AttackModesStage(7); return;
            }
            if (attackModesStage == 7)
            {
                if (!FinishFullAttack()) return;
                var measurements = (JArray)attackModes["measurements"];
                var mountedOn = (JObject)measurements[1]; var control = (JObject)measurements[2];
                var mountedPlan = ((JArray)mountedOn["command"]["plan"]).OfType<JObject>().Count(e => (bool)e["ranged"] && (string)e["owner"] == ar.UniqueId);
                var controlPlan = ((JArray)control["command"]["plan"]).OfType<JObject>().Count(e => (bool)e["ranged"] && (string)e["owner"] == ar.UniqueId);
                Require(mountedPlan == controlPlan && InnerBonuses(mountedOn).SequenceEqual(InnerBonuses(control)) && RapidBonus(mountedOn) == RapidBonus(control),
                    "Mounted native full attack differs from the unmounted control under the same Rapid Shot toggle.");
                attackModes["complete"] = true; Pass("C7-combat-attack-modes-unmounted-control");
                attackModes["rules"] = attackModesRules.Capture(); attackModesRules.Dispose(); attackModesRules = null;
                BeginCleanup();
            }
        }
    }
}
