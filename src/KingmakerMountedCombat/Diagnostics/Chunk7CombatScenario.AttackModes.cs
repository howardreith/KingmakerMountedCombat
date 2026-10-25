using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Items;
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
    // ordinary attacks (the product routes a mounted click as an ordinary-intent RiderRanged command,
    // so native UnitAttack.InitAttacks chooses full or single from the TB cursor) on the diagnostic
    // target from where it stands. Each leg's expected mode is the native estimate at the click: a
    // one-attack routine is natively single; with Rapid Shot on it is always a full attack:
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
        private JObject attackModes, attackModeObservation, attackModeWatchStart;
        private UnitAttack attackModeCommand, attackModeWatched;
        private int attackModesStage, attackModesRound;
        private TurnController attackModesDismountTurn;
        private JObject RapidState(string label) => new JObject { ["label"] = label, ["toggle"] = attackModesRapid.Blueprint.AssetGuid,
            ["isOn"] = attackModesRapid.IsOn, ["isRunning"] = attackModesRapid.IsRunning,
            ["buff"] = ar.Buffs.Enumerable.Any(buff => buff.Blueprint.AssetGuid == RapidShotBuffGuid), ["inCombat"] = ar.IsInCombat,
            ["round"] = Game.Instance.TurnBasedCombatController.RoundNumber };
        private bool Mounted6AttackModes => AttackModesCase && request.OwnedPairs.IsMounted6;
        private JObject HandSetState() => new JObject { ["handSet"] = ar.Body.CurrentHandEquipmentSetIndex,
            ["primaryBlueprint"] = ar.Body.PrimaryHand.MaybeWeapon?.Blueprint.AssetGuid, ["firstWeapon"] = ar.GetFirstWeapon()?.Blueprint.AssetGuid,
            ["firstWeaponRanged"] = ar.GetFirstWeapon()?.Blueprint.IsRanged == true, ["inCombat"] = ar.IsInCombat };
        private ItemEntityWeapon SavedPrimaryWeapon(int index) =>
            index < ar.Body.HandsEquipmentSets.Count ? ar.Body.HandsEquipmentSets[index].PrimaryHand.MaybeItem as ItemEntityWeapon : null;
        // Every hand set's primary slot as saved: stored item (MaybeItem), whether it is a weapon and ranged, the slot's native
        // Active/Disabled state and the native MaybeWeapon answer (the empty-hand weapon for an inactive slot).
        private JObject SavedHandSets()
        {
            var sets = ar.Body.HandsEquipmentSets; var rows = new JArray();
            for (var index = 0; index < sets.Count; index++)
            {
                var slot = sets[index].PrimaryHand; var item = slot.MaybeItem; var weapon = item as ItemEntityWeapon;
                rows.Add(new JObject { ["index"] = index, ["hasItem"] = slot.HasItem, ["active"] = slot.Active, ["disabled"] = slot.Disabled,
                    ["item"] = item?.Blueprint.AssetGuid, ["weapon"] = weapon?.Blueprint.AssetGuid, ["weaponRanged"] = weapon?.Blueprint.IsRanged == true,
                    ["maybeWeapon"] = slot.MaybeWeapon?.Blueprint.AssetGuid });
            }
            return new JObject { ["count"] = sets.Count, ["current"] = ar.Body.CurrentHandEquipmentSetIndex, ["polymorphed"] = ar.Body.IsPolymorphed,
                ["emptyHandWeapon"] = ar.Body.EmptyHandWeapon?.Blueprint.AssetGuid, ["sets"] = rows };
        }
        // Mounted6 lineage (C03 BAB variation): rider A must have BAB >= 6, so with Rapid Shot off the native routine has
        // an iterative second attack. The seed saved hand set 1 (the longsword) active; out of combat, before the encounter,
        // the saved ranged hand set 0 is selected through the public native UnitBody.CurrentHandEquipmentSetIndex setter
        // (the Phase 3D ranged-set precedent), recorded before and after. No item is created, moved or equipped.
        private JObject PrepareMounted6AttackModes()
        {
            var bab = ar.Stats.BaseAttackBonus.ModifiedValue; var selection = new JObject { ["baseAttackBonus"] = bab, ["before"] = HandSetState() };
            Require(bab >= Chunk7OwnedPairsSelection.Mounted6MinimumBaseAttackBonus,
                "Rider A's native BAB is below 6; the C03 BAB variation (Rapid Shot off full attack) is uncovered on this fixture.");
            // A saved weapon in an inactive set is its slot's stored item (ItemSlot.MaybeItem); WeaponSlot.MaybeWeapon answers
            // the empty-hand weapon for an inactive slot (b207 m5's false refusal). Every clause is recorded for a refusal.
            var sets = ar.Body.HandsEquipmentSets; var current = ar.Body.CurrentHandEquipmentSetIndex;
            var saved = SavedHandSets(); selection["savedSets"] = saved;
            var rangedSaved = SavedPrimaryWeapon(Chunk7OwnedPairsSelection.Mounted6RangedHandSet);
            Require(Chunk7OwnedPairsSelection.Mounted6HandSetsAdmitted(ar.IsInCombat, ar.Body.IsPolymorphed, sets.Count, current, rangedSaved?.Blueprint.AssetGuid,
                    rangedSaved?.Blueprint.IsRanged == true, SavedPrimaryWeapon(Chunk7OwnedPairsSelection.Mounted6MeleeHandSet)?.Blueprint.AssetGuid,
                    ar.Body.PrimaryHand.MaybeWeapon?.Blueprint.AssetGuid),
                "Rider A's saved hand sets differ from the mounted6 seed's (set 0 ranged, set 1 longsword); C03 attack modes are uncovered on this fixture: " +
                new JObject { ["before"] = selection["before"], ["savedSets"] = saved }.ToString(Newtonsoft.Json.Formatting.None));
            selection["switched"] = current != Chunk7OwnedPairsSelection.Mounted6RangedHandSet;
            selection["nativeApi"] = "Kingmaker.Items.UnitBody.CurrentHandEquipmentSetIndex (public setter, out of combat)";
            if ((bool)selection["switched"]) ar.Body.CurrentHandEquipmentSetIndex = Chunk7OwnedPairsSelection.Mounted6RangedHandSet;
            selection["after"] = HandSetState();
            Require(ar.Body.CurrentHandEquipmentSetIndex == Chunk7OwnedPairsSelection.Mounted6RangedHandSet && ar.GetFirstWeapon()?.Blueprint.AssetGuid == Chunk7OwnedPairsSelection.Mounted6RangedWeapon,
                "Native hand-set selection did not make the saved ranged weapon rider A's first weapon.");
            return selection;
        }
        // Out of combat, before the encounter: rider A's own native facts are verified and Rapid Shot is
        // switched off through the native slot if the save left it on. A missing feat, toggle or ranged
        // weapon is refused before any input and records the behavior as uncovered.
        private void PrepareAttackModes()
        {
            var feat = ar.Descriptor.Progression.Features.Enumerable.Count(fact => fact.Blueprint.AssetGuid == RapidShotFeatureGuid);
            var toggles = ar.ActivatableAbilities.Enumerable.Where(item => item.Blueprint.AssetGuid == RapidShotToggleGuid).ToArray();
            // The feat and toggle are checked before the mounted6 hand-set selection, so a gap refuses before any mutation.
            Require(!Mounted6AttackModes || feat == 1 && toggles.Length == 1, "Rider A lacks its native Rapid Shot feat or toggle; C03 attack modes are uncovered on this fixture.");
            var mounted6 = Mounted6AttackModes ? PrepareMounted6AttackModes() : null;
            var weapon = ar.GetFirstWeapon();
            attackModes = new JObject { ["contract"] = "owner-seed-attack-modes-v2", ["riderId"] = ar.UniqueId, ["mountId"] = am.UniqueId,
                ["rapidShotFeature"] = feat, ["rapidShotToggles"] = toggles.Length, ["weapon"] = weapon?.Blueprint.AssetGuid,
                ["weaponRanged"] = weapon?.Blueprint.IsRanged == true, ["weaponRangeMeters"] = weapon?.Blueprint.AttackRange.Meters };
            if (mounted6 != null) attackModes["mounted6"] = mounted6;
            evidence["attackModes"] = attackModes;
            Require(feat == 1 && toggles.Length == 1 && weapon != null && weapon.Blueprint.IsRanged,
                "Rider A lacks its native Rapid Shot feat, toggle or equipped ranged weapon; C03 attack modes are uncovered on this fixture.");
            attackModesRapid = toggles[0];
            attackModes["rapidBefore"] = RapidState("save");
            if (attackModesRapid.IsOn) ToggleRapidShot(false, "pre-combat-off");
            Require(!attackModesRapid.IsOn, "Native Rapid Shot did not turn off out of combat.");
        }
        // Native OnTurnOff may keep the ability running until its next native round tick; the
        // encounter starts only once the toggle has natively stopped and its buff is gone.
        private bool AttackModesRapidSettled()
        {
            Require(!attackModesRapid.IsOn && !ar.IsInCombat, "Native Rapid Shot was switched back on or rider A entered combat before the encounter.");
            var state = RapidState("pre-combat");
            if ((bool)state["isRunning"] || (bool)state["buff"]) return false;
            // A mounted6 hand-set selection settles natively (no scheduled hands update, no hand animation) before the encounter.
            if (Mounted6AttackModes && (Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(ar) || ar.AreHandsBusyWithAnimation)) return false;
            attackModes["rapidOff"] = state; return true;
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
                ["traceSequenceBefore"] = trace.EventCount, ["pairState"] = a.Relationship.State.ToString(),
                ["stockObservationBefore"] = a.Combat.LastStockAttackObservation };
            attackModeObservation = observation; ((JArray)attackModes["measurements"]).Add(observation);
            Require(targetService.BeginExpectedAttackDispatch(target), "Exact diagnostic target attack authorization refused.");
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(ar);
            bool clicked;
            using (var input = new NativeOrdinaryAttackInput(target))
            {
                input.Predict();
                // Native TB offers its FullAttack cursor only when UnitAttack.EstimateFullAttacks > 1
                // (TurnController.UpdateSmartCursorVariants), so a one-attack routine (BAB < 6 without
                // Rapid Shot, b201 am: bab 4) is natively a single attack, mounted or not. The expected
                // shape is derived from that native estimate at the click; Rapid Shot always adds one.
                var estimate = UnitAttack.EstimateFullAttacks(ar); var current = Game.Instance.TurnBasedCombatController.CurrentTurn;
                observation["nativeEstimate"] = estimate; observation["expectedFull"] = estimate > 1;
                observation["currentTurnEnabledFullAttack"] = current?.Unit == ar ? (JToken)current.EnabledFullAttack : JValue.CreateNull();
                Require(!attackModesRapid.IsOn || estimate > 1, "Native Rapid Shot on left rider A no native full attack estimate above one.");
                clicked = input.Click();
            }
            observation["clicked"] = clicked;
            attackModeCommand = ar.Commands.Raw.OfType<UnitAttack>().FirstOrDefault(command => ReferenceEquals(command.Target, target));
            if (attackModeCommand != null) WatchAttackModeCommand(attackModeCommand, true);
            Game.Instance.IsPaused = false;
            Require(clicked, "Native ordinary attack click on the target was refused.");
        }
        private static JObject PlanEntry(AttackHandInfo info, int index) => new JObject { ["index"] = index,
            ["weapon"] = info.Weapon?.Blueprint.AssetGuid, ["ranged"] = info.Weapon?.Blueprint.IsRanged == true,
            ["natural"] = info.Weapon?.Blueprint.IsNatural == true, ["owner"] = info.Hand?.Owner?.Unit?.UniqueId,
            ["iterativePenalty"] = info.AttackBonusPenalty };
        // Contract v2: every native Interrupt(bool) of the measured command is recorded with its prior terminal
        // state and caller chain (b202 am: an unpaired rider's command is otherwise unobserved). The watch opens
        // at the click or, if the command is admitted later, the first frame it is observed; an effective native
        // interrupt always finishes the command, so an unfinished command at the watch start proves none was missed.
        private void WatchAttackModeCommand(UnitAttack command, bool atClick)
        {
            trace.WatchCommandInterrupts(command); attackModeWatched = command;
            attackModeWatchStart = new JObject { ["atClick"] = atClick, ["frame"] = UnityEngine.Time.frameCount, ["finished"] = command.IsFinished,
                ["result"] = command.Result.ToString(), ["acted"] = command.IsActed };
        }
        private bool FinishFullAttack()
        {
            if (attackModeCommand == null)
            {
                attackModeCommand = ar.Commands.Raw.OfType<UnitAttack>().FirstOrDefault(started => ReferenceEquals(started.Target, target));
                if (attackModeCommand != null) WatchAttackModeCommand(attackModeCommand, false);
            }
            if (attackModeCommand == null || !attackModeCommand.IsFinished || a.Combat.HasActiveCommand || a.Combat.HasActiveGroundMovement ||
                !ar.Commands.Empty || !am.Commands.Empty || ar.AreHandsBusyWithAnimation || am.AreHandsBusyWithAnimation) return false;
            var observation = attackModeObservation; var command = attackModeCommand; attackModeCommand = null;
            var after = WorldSnapshot((string)observation["label"] + "-after"); observation["after"] = after;
            var plan = command.AllAttacks.Select(PlanEntry).ToArray();
            var watched = ReferenceEquals(attackModeWatched, command);
            var interrupts = attackModeWatched == null ? new JArray() : trace.TakeWatchedInterrupts(attackModeWatched);
            if (!watched) interrupts = new JArray(); attackModeWatched = null; var watchStart = attackModeWatchStart; attackModeWatchStart = null;
            var targetUnit = command.TargetUnit;
            observation["command"] = new JObject { ["identity"] = trace.ObjectIdentity(command), ["executor"] = command.Executor?.UniqueId,
                ["target"] = command.Target?.UniqueId, ["fullAttack"] = command.IsFullAttack, ["singleAttack"] = command.IsSingleAttack,
                ["result"] = command.Result.ToString(), ["completedAttacks"] = command.GetAttackIndex(), ["plan"] = new JArray(plan),
                ["interruptWatch"] = new JObject { ["watched"] = watched, ["watchStart"] = watched ? watchStart : null, ["interrupts"] = interrupts,
                    ["interruptAsSoonAsPossible"] = command.InterruptAsSoonAsPossible, ["executorCanAct"] = ar.Descriptor.State.CanAct,
                    ["targetInState"] = targetUnit?.IsInState, ["targetDead"] = targetUnit?.Descriptor.State.IsDead } };
            // Native UnitAttack.OnAction returns Success with the last planned attack; UnitCommand.Interrupt(bool)
            // then overwrites it while that attack's animation still runs. A plan-complete Interrupt is admitted
            // only as exactly one such native overwrite (no KingmakerMountedCombat frame in its caller chain).
            var effective = interrupts.OfType<JObject>().Where(row => !(bool)row["finishedBefore"] && (string)row["resultBefore"] != "Interrupt").ToArray();
            var planComplete = command.GetAttackIndex() >= plan.Length;
            // Admitted means no KingmakerMountedCombat frame on the whole (untruncated) stack and no queued-command
            // overwrite (InterruptAsSoonAsPossible); it does not prove which native rule interrupted.
            Require(watched && !(bool)watchStart["finished"] && (command.Result != UnitCommand.ResultType.Interrupt || (effective.Length == 1 &&
                (!planComplete || (string)effective[0]["resultBefore"] == "Success" && (bool)effective[0]["actedBefore"] &&
                    !(bool)effective[0]["callersTruncated"] && !(bool)effective[0]["interruptAsSoonAsPossible"] &&
                    !((string)effective[0]["callers"] ?? "KingmakerMountedCombat").Contains("KingmakerMountedCombat")))) &&
                (command.Result != UnitCommand.ResultType.Success || effective.Length == 0),
                "Native attack interrupt cause differs from one native terminal overwrite: " + observation["command"]["interruptWatch"].ToString(Newtonsoft.Json.Formatting.None));
            observation["rules"] = attackModesRules.Since((int)observation["ruleCountBefore"]);
            observation["stockObservationAfter"] = a.Combat.LastStockAttackObservation;
            observation["traceSequenceAfter"] = trace.EventCount;
            observation["rapidAfter"] = RapidState((string)observation["label"] + "-after");
            var rolls = ((JArray)observation["rules"]).OfType<JObject>().Where(e => (string)e["kind"] == "roll").ToArray();
            var riderRolls = rolls.Where(e => (string)e["actor"] == ar.UniqueId && !(bool)e["opportunity"]).ToArray();
            var rangedPlan = plan.Count(e => (bool)e["ranged"] && (string)e["owner"] == ar.UniqueId);
            var before = (JObject)observation["before"];
            var expectedFull = (bool)observation["expectedFull"];
            Require(command.Executor == ar && command.IsFullAttack == expectedFull && !command.IsSingleAttack &&
                (expectedFull ? plan.Length == (int)observation["nativeEstimate"] : plan.Length == 1 && rangedPlan == 1) &&
                rangedPlan >= 1 && riderRolls.Length == rangedPlan &&
                riderRolls.All(e => (bool)e["ranged"]) && rolls.All(e => (string)e["actor"] == ar.UniqueId) &&
                (command.Result == UnitCommand.ResultType.Success || command.Result == UnitCommand.ResultType.Interrupt) &&
                plan.Skip(rangedPlan).All(e => !(bool)e["ranged"]) &&
                (float)Actor(after, ar)["standard"] > (float)Actor(before, ar)["standard"] &&
                // Mount A is exact while it carries rider A; unmounted (control) it is an independent unit whose finished
                // paired turn may still decay. Pair B never spends: a finished turn's cooldowns may only decay natively.
                ((bool)observation["mounted"] ? SameActorBudget(Actor(before, am), Actor(after, am)) : NeverSpentNatively(before, after, am)) &&
                new[] { br, bm }.All(actor => NeverSpentNatively(before, after, actor)),
                "Native " + (expectedFull ? "full" : "single") + " attack (native estimate " + (int)observation["nativeEstimate"] +
                ") escaped its exact plan, rolls or actor allocation: " + observation["command"].ToString(Newtonsoft.Json.Formatting.None));
            return true;
        }
        // A surprised rider A turn cannot carry a measured input: it is ended natively, as the full activations do,
        // and recorded in the case record. Native surprise belongs to the first round only.
        private bool EndSurprisedRiderTurn(TurnController turn, JObject record)
        {
            if (turn?.Unit != ar || !Game.Instance.TurnBasedCombatController.IsSurprised(ar)) return false;
            record["surprisedRiderTurnEnded"] = Game.Instance.TurnBasedCombatController.RoundNumber;
            EndNative(turn); return true;
        }
        private static int[] InnerBonuses(JObject observation) => ((JArray)observation["rules"]).OfType<JObject>()
            .Where(e => (string)e["kind"] == "roll" && !(bool)e["opportunity"]).Select(e => (int)e["nativeCalculation"]["innerResult"]).ToArray();
        private static int RapidBonus(JObject observation)
        {
            var values = ((JArray)observation["rules"]).OfType<JObject>().Where(e => (string)e["kind"] == "roll" && !(bool)e["opportunity"])
                .SelectMany(e => ((JArray)e["bonuses"] ?? new JArray()).OfType<JObject>())
                .Where(b => (string)b["source"] == RapidShotBuffGuid).Select(b => (int)b["bonus"]).Distinct().ToArray();
            Require(values.Length <= 1, "Rapid Shot bonus sources are not uniform within one full attack.");
            return values.Length == 0 ? 0 : values[0];
        }
        private void TickAttackModes()
        {
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            attackModes["stage"] = attackModesStage;
            if (attackModesStage == 0)
            {
                if (EndSurprisedRiderTurn(turn, attackModes)) return;
                // Rider A's own turn is never ended here; its partner context may still be binding.
                if (!ReadyPrincipal(a)) { if (turn?.Unit != target && turn?.Unit != ar) EndNative(turn); return; }
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
                // Mounted6 (BAB >= 6): with Rapid Shot off the native routine is a full attack with an iterative second ranged attack.
                if (Mounted6AttackModes)
                    Require((bool)off["expectedFull"] && (int)off["nativeEstimate"] >= 2 && ((JArray)off["command"]["plan"]).OfType<JObject>().Count(e => (bool)e["ranged"] && (string)e["owner"] == ar.UniqueId) >= 2,
                        "Mounted Rapid Shot off attack at BAB >= 6 was not a native full attack with an iterative second ranged attack.");
                Pass("C7-combat-attack-modes-mounted-off");
                // Switched on in the same activation, after the attack, so the native toggle runs into N+1.
                ToggleRapidShot(true, "round-n-on"); AttackModesStage(2); return;
            }
            if (attackModesStage == 2)
            {
                if (ReferenceEquals(turn, actingTurn)) { EndNative(turn); return; }
                Require(controller.RoundNumber <= attackModesRound + 1, "Attack modes missed the next native round.");
                var due = controller.RoundNumber == attackModesRound + 1;
                if (!due || !ReadyPrincipal(a)) { if (turn?.Unit != target && (turn?.Unit != ar || !due)) EndNative(turn); return; }
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
                var due = controller.RoundNumber == attackModesRound + 2;
                if (!due || !ReadyPrincipal(a)) { if (turn?.Unit != target && (turn?.Unit != ar || !due)) EndNative(turn); return; }
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
                var rules = attackModesRules.Capture();
                Require((int)rules["dropped"] == 0 && ((JArray)rules["errors"]).Count == 0, "Attack modes rule observation dropped or failed.");
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
