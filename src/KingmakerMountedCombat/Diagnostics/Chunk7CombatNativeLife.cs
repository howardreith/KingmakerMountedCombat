using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7CombatScenario
    {
        private NativeLifeDeliveryProbe lifeDelivery;
        private Chunk4IncomingRuleObserver lifeEffects;
        private JObject nativeLife;
        private int lifeStage, lifeInitialRound, lifeDamageRound;
        private long lifeLedgerOffset, lifeTransitionOffset;
        private int[] lifeGrants;
        private int lifeRiderSlotsBeforeRenewal, lifeExpectedRiderSlots;
        private UnitEntityData lifeControl;
        private void BeginNativeLife()
        {
            Require(lifecycle != null, "Native life case requires the exact existing production lifecycle subscriber.");
            lifeInitialRound = Game.Instance.TurnBasedCombatController.RoundNumber;
            nativeLife = new JObject { ["initialRound"] = lifeInitialRound, ["subjectId"] = br.UniqueId, ["survivorId"] = bm.UniqueId,
                ["healthyActorIds"] = new JArray(ar.UniqueId, am.UniqueId), ["beforeFixture"] = LifeState("life-fixture"),
                ["stimulus"] = "one labelled native RuleDealDamage; stock UnitLifeController tick delivers existing lifecycle subscriber",
                ["sameCallRoundBoundaryLifeDeliveryProven"] = false, ["nativeDamageDispatches"] = 0 };
            evidence["nativeLife"] = nativeLife;
            evidence["stimulusKinds"] = new JArray("native-selected-ability-handler", "native-ground-input", "Game.PauseBind",
                "one-labelled-RuleDealDamage", "observed-native-TickOnUnit-SetLifeState-production-subscriber");
            Require(br.Descriptor.State.IsConscious && !br.Descriptor.IsEssentialForGame &&
                br != Game.Instance.Player.MainCharacter.Value && !br.Descriptor.State.Immortality && br.Descriptor.State.AllowDyingCondition,
                "Exact Horse rider native life policy lacks a safe non-main, non-essential incapacitation fixture.");
            lifeEffects = new Chunk4IncomingRuleObserver(br, bm); lifeEffects.BeginCase(NativeLifeCleanup);
        }
        private void LifeStage(int value) { lifeStage = value; stepStarted = clock.Elapsed.TotalSeconds; }
        private void BeginLifeActivation(bool healthy, string label)
        {
            actingPair = healthy ? a : b; actingIndex = healthy ? 0 : 1;
            actingTurn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            activation = new JObject { ["before"] = LifeState(label + "-before") };
            nativeLife[label] = activation; BeginMove();
        }
        private void TickNativeLife()
        {
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn;
            nativeLife["stage"] = lifeStage;
            if (lifeStage == 0)
            {
                if (controller.RoundNumber == lifeInitialRound) { EndNative(turn); return; }
                Require(controller.RoundNumber == lifeInitialRound + 1, "Native life setup missed the next real round.");
                if (!ReadyPrincipal(a)) { EndNative(turn); return; }
                Require(!controller.IsSurprised(ar), "Native life setup reached an unexpected surprise activation.");
                lifeDamageRound = controller.RoundNumber; nativeLife["damageRound"] = lifeDamageRound;
                BeginLifeActivation(true, "healthyBeforeDamage"); LifeStage(1); return;
            }
            if (lifeStage == 1)
            { if (!FinishMove()) return; BeginAttack(false, "life-healthy-rider-before-damage"); LifeStage(2); return; }
            if (lifeStage == 2)
            { if (!FinishAttack(false)) return; nativeLife["healthyAttackBefore"] = attackObservation; LifeStage(3); }
            if (lifeStage == 3)
            {
                if (ReferenceEquals(turn, actingTurn)) { EndNative(turn); return; }
                nativeLife["healthyEndedBeforeDamage"] = LifeState("healthy-ended-before-damage"); LifeStage(4);
            }
            if (lifeStage == 4)
            {
                Require(controller.RoundNumber == lifeDamageRound, "Native life setup escaped its selected N+1 round.");
                if (!ReadyPrincipal(b)) { EndNative(turn); return; }
                BeginLifeActivation(false, "affectedBeforeDamage"); LifeStage(5); return;
            }
            if (lifeStage == 5)
            { if (!FinishMove()) return; BeginAttack(true, "life-affected-mount-before-damage"); LifeStage(6); return; }
            if (lifeStage == 6)
            {
                if (!FinishAttack(true)) return;
                nativeLife["affectedAttackBefore"] = attackObservation;
                DispatchSafeLifeDamage(); LifeStage(7); return;
            }
            if (lifeStage == 7)
            {
                if (br.Descriptor.State.IsConscious) return;
                Require(!br.Descriptor.State.IsDead, "Native incapacitation crossed the permitted living boundary.");
                var probe = lifeDelivery.Capture();
                if (!(bool)probe["completedNativeTick"]) return;
                nativeLife["deliveryProbe"] = probe;
                Require(((JArray)probe["errors"]).Count == 0 && (int)probe["tickDepth"] == 0 && (int)probe["setDepth"] == 0,
                    "Actual native lifecycle observation is incomplete.");
                var callbackBefore = ((JArray)probe["events"]).Single(e => (string)e["boundary"] == "production-callback-before");
                var callbackAfter = ((JArray)probe["events"]).Single(e => (string)e["boundary"] == "production-callback-after");
                Require((int)callbackBefore["nativeTickDepth"] == 1 && (int)callbackBefore["nativeSetDepth"] == 1 &&
                    (int)callbackAfter["nativeTickDepth"] == 1 && (int)callbackAfter["nativeSetDepth"] == 1 &&
                    (string)callbackBefore["previousLife"] == "Conscious" && (string)callbackAfter["currentLife"] == "Unconscious",
                    "Life cleanup did not arrive through the actual stock Tick/SetLifeState call chain.");
                var before = (JObject)callbackBefore["world"]; var after = (JObject)callbackAfter["world"];
                Require(HealthyOwnerUnchanged(before, after) && new[] { ar, am, bm }.All(actor => SameActorBudget(Actor(before, actor), Actor(after, actor))),
                    "Native life callback changed healthy ownership, healthy expenditure or surviving mount debt.");
                var deliveries = lifecycle.SnapshotNativeDeliveries().Where(x => x.Sequence > lifeLedgerOffset).ToArray();
                var transitions = lifecycle.SnapshotPairLifeTransitions().Where(x => x.Sequence > lifeTransitionOffset).ToArray();
                nativeLife["deliveries"] = new JArray(deliveries.Select(x => new JObject { ["sequence"] = x.Sequence,
                    ["boundary"] = x.Boundary.ToString(), ["source"] = x.Source, ["stateBefore"] = x.StateBefore.ToString(),
                    ["stateAfter"] = x.StateAfter.ToString(), ["trigger"] = x.CleanupTrigger?.ToString(),
                    ["attempted"] = x.CleanupAttempted, ["succeeded"] = x.CleanupSucceeded, ["errors"] = new JArray(x.CleanupErrors) }));
                nativeLife["lifeTransitions"] = new JArray(transitions.Select(x => new JObject { ["sequence"] = x.Sequence,
                    ["actorId"] = x.ActorId, ["previousLife"] = x.PreviousLifeState, ["currentLife"] = x.CurrentLifeState }));
                var life = deliveries.Where(x => x.Boundary == NativeLifecycleBoundary.UnitIncapacitated).ToArray();
                Require(life.Length == 1 && life[0].Source == "IUnitLifeStateChanged.HandleUnitLifeStateChanged" &&
                    life[0].StateBefore == RelationshipState.Mounted && life[0].StateAfter == RelationshipState.Unmounted &&
                    life[0].CleanupTrigger == CleanupTrigger.Incapacitated && life[0].CleanupAttempted && life[0].CleanupSucceeded && life[0].CleanupErrors.Count == 0 &&
                    transitions.Length == 1 && transitions[0].ActorId == br.UniqueId && transitions[0].PreviousLifeState == "Conscious" && transitions[0].CurrentLifeState == "Unconscious",
                    "Production native ledger lacks one successful exact actor incapacitation cleanup.");
                Require(b.Relationship.State == RelationshipState.Unmounted && b.Combat.PairedActivationSplit &&
                    SplitReleaseRound(b) == lifeDamageRound && b.Relationship.Runtime.NoPreparedPairResidue &&
                    actors.Select(trace.GrantCount).SequenceEqual(lifeGrants),
                    "Native life cleanup changed the original split round, retained presentation or granted preparation.");
                nativeLife["afterDelivery"] = LifeState("life-after-delivery");
                nativeLife["traceSequenceAfterDelivery"] = (int?)trace.Capture()["events"].Last?["sequence"] ?? 0;
                nativeLife["nativeEffects"] = lifeEffects.Capture();
                lifeDelivery.Dispose(); lifeDelivery = null;
                Pass("C7-life-native-callback"); DispatchUnconsciousControl(); LifeStage(8); return;
            }
            if (lifeStage == 8)
            {
                Require(controller.RoundNumber <= lifeDamageRound + 1, "Healthy native successor missed the original next round.");
                if (controller.RoundNumber != lifeDamageRound + 1 || !ReadyPrincipal(a))
                { if (turn?.Unit?.Descriptor.State.IsConscious == true) EndNative(turn); return; }
                Require(trace.GrantCount(ar) == lifeGrants[0] + 1 && trace.GrantCount(am) == lifeGrants[1] + 1 &&
                    trace.GrantCount(br) == lifeGrants[2] + lifeRiderSlotsBeforeRenewal && trace.GrantCount(bm) == lifeGrants[3],
                    "Healthy pair renewal changed affected native preparation or granted an extra turn.");
                nativeLife["healthyRenewed"] = LifeState("life-healthy-renewed");
                actingPair = a; actingIndex = 0; actingTurn = turn;
                BeginAttack(false, "life-healthy-rider-after-cleanup"); LifeStage(9); return;
            }
            if (lifeStage == 9)
            { if (!FinishAttack(false)) return; nativeLife["healthyAttackAfter"] = attackObservation; Pass("C7-life-healthy-neighbour"); LifeStage(10); }
            if (lifeStage == 10)
            {
                if (ReferenceEquals(turn, actingTurn)) { EndNative(turn); return; }
                nativeLife["healthyEndedAfterCleanup"] = LifeState("healthy-ended-after-cleanup"); LifeStage(11);
            }
            if (lifeStage == 11)
            {
                Require(controller.RoundNumber <= lifeDamageRound + 1 && !br.Descriptor.State.IsConscious && !br.Descriptor.State.IsDead,
                    "Native survivor continuation left its selected round or life window.");
                if (turn?.Unit != bm || !Chunk7NativeFixture.ReadyForTurnInput(turn)) { if (turn?.Unit?.Descriptor.State.IsConscious == true) EndNative(turn); return; }
                Require(controller.RoundNumber == lifeDamageRound + 1 && trace.GrantCount(bm) == lifeGrants[3] + 1 &&
                    trace.GrantCount(br) == lifeGrants[2] + lifeRiderSlotsBeforeRenewal + lifeExpectedRiderSlots &&
                    bm.CombatState.Cooldown.StandardAction == 0f &&
                    bm.CombatState.Cooldown.MoveAction == 0f && world.Registry.FindActive(bm) == null && b.Participant.PartnerContext == null,
                    "Native surviving mount did not receive exactly one independent fresh turn in the original next round.");
                RequireUnconsciousNativeTurns(controller.RoundNumber);
                nativeLife["independentSurvivor"] = LifeState("life-independent-survivor");
                nativeLife["complete"] = true; Pass("C7-life-independent-survivor"); BeginCleanup();
            }
        }
        private void DispatchSafeLifeDamage()
        {
            var difficulty = Game.Instance.Player.Difficulty.DamageToParty;
            var hp = br.Stats.HitPoints.ModifiedValue; var constitution = br.Stats.Constitution.ModifiedValue;
            var temporaryHp = br.Stats.TemporaryHitPoints.ModifiedValue; var deathThreshold = checked(hp + constitution);
            var needed = checked(hp + 1 - br.Damage + temporaryHp);
            Require(br.Descriptor.State.IsConscious && br.Descriptor.State.AllowDyingCondition && !br.Descriptor.State.Immortality &&
                !br.Descriptor.IsEssentialForGame && br != Game.Instance.Player.MainCharacter.Value && target.IsPlayersEnemy &&
                !target.IsPlayerFaction && difficulty > 0f && !float.IsNaN(difficulty) && !float.IsInfinity(difficulty) && needed > 0,
                "Exact native policy/difficulty leaves no admitted incapacitation stimulus.");
            var requested = checked((int)Math.Ceiling((needed + 1d) / difficulty));
            Require(br.Damage + requested * difficulty - temporaryHp < deathThreshold,
                "Native damage scale leaves no safe HP+1 to HP+Constitution window.");
            lifeLedgerOffset = lifecycle.SnapshotNativeDeliveries().Select(x => x.Sequence).DefaultIfEmpty().Max();
            lifeTransitionOffset = lifecycle.SnapshotPairLifeTransitions().Select(x => x.Sequence).DefaultIfEmpty().Max();
            lifeGrants = actors.Select(trace.GrantCount).ToArray();
            RecordNativeNextRoundOrder();
            nativeLife["ledgerOffset"] = lifeLedgerOffset; nativeLife["lifeTransitionOffset"] = lifeTransitionOffset;
            nativeLife["beforeDamage"] = LifeState("life-before-native-damage");
            nativeLife["traceSequenceBeforeDamage"] = (int?)trace.Capture()["events"].Last?["sequence"] ?? 0;
            nativeLife["sourceId"] = target.UniqueId; nativeLife["requestedDamage"] = requested;
            nativeLife["difficulty"] = difficulty; nativeLife["deathThreshold"] = deathThreshold;
            lifeDelivery = new NativeLifeDeliveryProbe(br, lifecycle, WorldSnapshot);
            lifeDelivery.Install();
            nativeLife["nativeDamageDispatches"] = 1;
            var damage = Rulebook.Trigger(new RuleDealDamage(target, br,
                new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
            nativeLife["nativeDamage"] = damage.Damage; nativeLife["nativeDamageBeforeDifficulty"] = damage.DamageBeforeDifficulty;
            nativeLife["afterDamage"] = LifeState("life-after-native-damage");
            Require(Game.Instance.Player.Difficulty.DamageToParty == difficulty && damage.DamageBeforeDifficulty == requested &&
                br.Damage >= hp + 1 && br.Damage < deathThreshold &&
                SameBudgets((JObject)nativeLife["beforeDamage"]["world"], (JObject)nativeLife["afterDamage"]["world"]),
                "Native damage failed its bounded living window or altered native action expenditure.");
            Pass("C7-life-safe-native-damage");
        }
        // Native 2.1.7b ChooseNextUnit/StartTurn/Prepare apply no consciousness filter: a living
        // unconscious unit receives one native Prepare and an empty turn that Tick ends natively.
        // The affected rider's N+1 slots are therefore derived from the native order at damage time.
        private void RecordNativeNextRoundOrder()
        {
            var order = Game.Instance.TurnBasedCombatController.SortedUnits.ToArray();
            int ia = Array.IndexOf(order, ar), ibr = Array.IndexOf(order, br), ibm = Array.IndexOf(order, bm);
            Require(ia >= 0 && ibr >= 0 && ibm >= 0 && ibr < ibm, "Native next-round order lacks the exact rider-first affected pair.");
            lifeRiderSlotsBeforeRenewal = ibr < ia ? 1 : 0;
            lifeExpectedRiderSlots = ia < ibr && ibr < ibm ? 1 : 0;
            nativeLife["nativeNextRoundOrder"] = new JArray(order.Select(unit => unit.UniqueId));
            nativeLife["riderSlotsBeforeRenewal"] = lifeRiderSlotsBeforeRenewal;
            nativeLife["expectedRiderSlots"] = lifeExpectedRiderSlots;
        }
        // Unmounted control: one labelled native RuleDealDamage in round N to a non-main,
        // non-essential, dying-allowed party companion outside both pairs whose N+1 slot precedes
        // the surviving mount. No eligible companion leaves the derivation-only proof, recorded so.
        private void DispatchUnconsciousControl()
        {
            var order = Game.Instance.TurnBasedCombatController.SortedUnits.ToArray();
            var survivorSlot = Array.IndexOf(order, bm); var main = Game.Instance.Player.MainCharacter.Value;
            var rows = new JArray(); UnitEntityData chosen = null;
            foreach (var unit in Game.Instance.Player.PartyCharacters.Select(reference => reference.Value)
                .Where(unit => unit != null && !actors.Contains(unit)).OrderBy(unit => Array.IndexOf(order, unit)))
            {
                var slot = Array.IndexOf(order, unit); var state = unit.Descriptor.State;
                var eligible = unit != main && !unit.Descriptor.IsPet && !unit.Descriptor.IsEssentialForGame && !state.Immortality &&
                    state.AllowDyingCondition && state.IsConscious && !state.IsDead && unit.IsInCombat &&
                    world.Registry.FindActive(unit) == null && slot >= 0 && slot < survivorSlot;
                rows.Add(new JObject { ["actorId"] = unit.UniqueId, ["slot"] = slot, ["mainCharacter"] = unit == main,
                    ["pet"] = unit.Descriptor.IsPet, ["essential"] = unit.Descriptor.IsEssentialForGame, ["immortal"] = (bool)state.Immortality,
                    ["allowDyingCondition"] = (bool)state.AllowDyingCondition, ["conscious"] = state.IsConscious, ["inCombat"] = unit.IsInCombat,
                    ["paired"] = world.Registry.FindActive(unit) != null, ["eligible"] = eligible });
                if (eligible && chosen == null) chosen = unit;
            }
            var control = new JObject { ["contract"] = "unmounted-unconscious-native-turn-control-v1", ["survivorSlot"] = survivorSlot,
                ["candidates"] = rows, ["available"] = chosen != null };
            nativeLife["unconsciousControl"] = control;
            if (chosen == null) return;
            var difficulty = Game.Instance.Player.Difficulty.DamageToParty;
            var hp = chosen.Stats.HitPoints.ModifiedValue; var deathThreshold = checked(hp + chosen.Stats.Constitution.ModifiedValue);
            var temporaryHp = chosen.Stats.TemporaryHitPoints.ModifiedValue; var needed = checked(hp + 1 - chosen.Damage + temporaryHp);
            Require(difficulty > 0f && !float.IsNaN(difficulty) && !float.IsInfinity(difficulty) && needed > 0,
                "Unmounted control has no admitted incapacitation stimulus.");
            var requested = checked((int)Math.Ceiling((needed + 1d) / difficulty));
            Require(chosen.Damage + requested * difficulty - temporaryHp < deathThreshold,
                "Unmounted control damage scale leaves no safe HP+1 to HP+Constitution window.");
            lifeControl = chosen;
            control["actorId"] = chosen.UniqueId; control["slot"] = Array.IndexOf(order, chosen);
            control["grantBefore"] = trace.GrantCount(chosen); control["traceSequenceAtDamage"] = trace.EventCount;
            control["before"] = LifeActor(chosen); control["requestedDamage"] = requested;
            var damage = Rulebook.Trigger(new RuleDealDamage(target, chosen,
                new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
            control["nativeDamage"] = damage.Damage; control["afterDamage"] = LifeActor(chosen);
            Require(damage.DamageBeforeDifficulty == requested && chosen.Damage >= hp + 1 && chosen.Damage < deathThreshold,
                "Unmounted control damage failed its bounded living window.");
        }
        // One unit's N+1 trace, defined on the trace's own hooks for every unit: the window runs
        // from native Prepare entry to the first turn-end-after (the End hook). Paired-only
        // completion probes are excluded so rider B and an unpaired control compare on identical
        // boundaries, as are UI ready handlers. ForceToEnd is the trace's separate diagnostic list.
        // The ledger identity counts after prepare-before: at its entry it still names the
        // previous round's activation (b194 a1 seq 921; native from clear-before at 922).
        private JObject UnconsciousNativeTurn(UnitEntityData unit, int round)
        {
            var capture = trace.Capture();
            var events = ((JArray)capture["events"]).OfType<JObject>()
                .Where(e => (int?)e["round"] == round && (string)e["state"]?["actor"] == unit.UniqueId).ToArray();
            var start = Array.FindIndex(events, e => (string)e["boundary"] == "prepare-before");
            var stop = start < 0 ? -1 : Array.FindIndex(events, start, e => (string)e["boundary"] == "turn-end-after");
            var window = stop < 0 ? new JObject[0] : events.Skip(start).Take(stop - start + 1).ToArray();
            var prepared = window.FirstOrDefault(e => (string)e["boundary"] == "prepare-after");
            var ended = window.LastOrDefault();
            var forbidden = new[] { "admission-", "cost-", "actor-cost-", "command-", "completion-force-to-end-", "chunk7-end-input-" };
            return new JObject { ["actorId"] = unit.UniqueId, ["round"] = round,
                ["prepareBefore"] = events.Count(e => (string)e["boundary"] == "prepare-before"),
                ["prepareAfter"] = events.Count(e => (string)e["boundary"] == "prepare-after"),
                ["ends"] = events.Count(e => (string)e["boundary"] == "turn-end-before"),
                ["forcedEnds"] = ((JArray)capture["forceToEnd"]).Count(f => (string)f["actor"] == unit.UniqueId && (int?)f["round"] == round),
                ["forbidden"] = events.Count(e => forbidden.Any(prefix => ((string)e["boundary"] ?? "").StartsWith(prefix, StringComparison.Ordinal))),
                ["sequence"] = new JArray(window.Select(e => (string)e["boundary"]).Where(b => !b.StartsWith("ready-handler-", StringComparison.Ordinal) &&
                    !b.StartsWith("completion-", StringComparison.Ordinal))),
                ["grantIdentities"] = new JArray(window.Skip(1).Select(e => e["state"]?["retained"] is JObject retained ? (string)retained["GrantIdentity"] : null).Distinct()),
                ["pairedIdentities"] = window.Count(e => e["state"]?["pairedGrantIdentity"] != null && e["state"]["pairedGrantIdentity"].Type != JTokenType.Null),
                ["prepareSequence"] = (int?)window.FirstOrDefault()?["sequence"],
                ["statusAfterPrepare"] = (string)prepared?["turnStatus"], ["standardBefore"] = (float?)prepared?["state"]?["standard"],
                ["standardAfterEnd"] = (float?)ended?["state"]?["standard"], ["conscious"] = unit.Descriptor.State.IsConscious,
                ["dead"] = unit.Descriptor.State.IsDead };
        }
        private static bool ExactUnconsciousNativeTurn(JObject turn, bool nativeLedger)
        {
            var identities = ((JArray)turn["grantIdentities"]).Select(x => (string)x).ToArray();
            return (int)turn["prepareBefore"] == 1 && (int)turn["prepareAfter"] == 1 && (int)turn["ends"] == 1 && (int)turn["forcedEnds"] == 0 &&
                (int)turn["forbidden"] == 0 && (int)turn["pairedIdentities"] == 0 && (string)turn["statusAfterPrepare"] == "Preparing" &&
                (float?)turn["standardAfterEnd"] == 6f && !(bool)turn["conscious"] && !(bool)turn["dead"] &&
                (nativeLedger ? identities.Length == 1 && identities[0] != null && identities[0].StartsWith("native:", StringComparison.Ordinal)
                    : identities.All(x => x == null || x.StartsWith("native:", StringComparison.Ordinal)));
        }
        private void RequireUnconsciousNativeTurns(int round)
        {
            var rider = UnconsciousNativeTurn(br, round); nativeLife["incapacitatedRiderTurn"] = rider;
            var slots = lifeRiderSlotsBeforeRenewal + lifeExpectedRiderSlots;
            Require(slots == 1 && ExactUnconsciousNativeTurn(rider, true) && b.Participant.PartnerContext == null,
                "Incapacitated rider's native next-round turn differs from its derived empty native slot: " + rider.ToString(Newtonsoft.Json.Formatting.None));
            var control = (JObject)nativeLife["unconsciousControl"];
            if (!(bool)control["available"]) return;
            var observed = UnconsciousNativeTurn(lifeControl, round); control["turn"] = observed;
            control["grantAfter"] = trace.GrantCount(lifeControl);
            var survivor = ((JArray)trace.Capture()["events"]).OfType<JObject>().FirstOrDefault(e => (int?)e["round"] == round &&
                (string)e["boundary"] == "prepare-before" && (string)e["state"]?["actor"] == bm.UniqueId);
            Require(ExactUnconsciousNativeTurn(observed, false) &&
                JToken.DeepEquals(observed["sequence"], rider["sequence"]) &&
                (float?)observed["standardBefore"] == (float?)rider["standardBefore"] && (float?)observed["standardAfterEnd"] == (float?)rider["standardAfterEnd"] &&
                survivor != null && (int?)observed["prepareSequence"] < (int)survivor["sequence"],
                "Unmounted unconscious control's native next-round turn differs from the incapacitated rider's: " + observed.ToString(Newtonsoft.Json.Formatting.None));
        }
        // Owner records keep the registry's "N"-form actor key; actors keep the hyphenated UniqueId.
        private bool HealthyOwnerUnchanged(JObject before, JObject after)
        {
            var left = ((JArray)before["owners"]).Single(x => OwnerActorMatch((string)x["riderId"], ar));
            var right = ((JArray)after["owners"]).Single(x => OwnerActorMatch((string)x["riderId"], ar));
            return (string)left["state"] == "Active" && (string)right["state"] == "Active" && JToken.DeepEquals(left, right);
        }
        private static bool OwnerActorMatch(string ownerValue, UnitEntityData actor) =>
            Guid.TryParse(ownerValue, out var owner) && Guid.TryParse(actor.UniqueId, out var id) ? owner == id : ownerValue == actor.UniqueId;
        private JObject LifeState(string label) => new JObject { ["world"] = WorldSnapshot(label), ["rider"] = LifeActor(br),
            ["mount"] = LifeActor(bm), ["healthyRider"] = LifeActor(ar), ["healthyMount"] = LifeActor(am),
            ["pairState"] = b.Relationship.State.ToString(), ["splitReleaseRound"] = SplitReleaseRound(b),
            ["partnerContextActor"] = b.Participant.PartnerContext?.Unit.UniqueId,
            ["activationIdentity"] = b.Combat.PairedActivationIdentity, ["activationSplit"] = b.Combat.PairedActivationSplit,
            ["presentationResidue"] = b.Relationship.Runtime.HasPresentationAttachmentResidue,
            ["grants"] = new JArray(actors.Select(actor => new JObject { ["actorId"] = actor.UniqueId, ["count"] = trace.GrantCount(actor) })) };
        private static JObject LifeActor(UnitEntityData actor) => new JObject { ["actorId"] = actor.UniqueId,
            ["life"] = actor.Descriptor.State.LifeState.ToString(), ["conscious"] = actor.Descriptor.State.IsConscious,
            ["dead"] = actor.Descriptor.State.IsDead, ["damage"] = actor.Damage, ["hp"] = actor.Stats.HitPoints.ModifiedValue,
            ["constitution"] = actor.Stats.Constitution.ModifiedValue, ["temporaryHp"] = actor.Stats.TemporaryHitPoints.ModifiedValue,
            ["mainCharacter"] = actor == Game.Instance.Player.MainCharacter.Value, ["essential"] = actor.Descriptor.IsEssentialForGame,
            ["immortal"] = (bool)actor.Descriptor.State.Immortality, ["allowDyingCondition"] = (bool)actor.Descriptor.State.AllowDyingCondition };
    }
}
