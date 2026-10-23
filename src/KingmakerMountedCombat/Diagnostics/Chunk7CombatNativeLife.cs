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
                Pass("C7-life-native-callback"); LifeStage(8); return;
            }
            if (lifeStage == 8)
            {
                Require(controller.RoundNumber <= lifeDamageRound + 1, "Healthy native successor missed the original next round.");
                if (controller.RoundNumber != lifeDamageRound + 1 || !ReadyPrincipal(a))
                { if (turn?.Unit?.Descriptor.State.IsConscious == true) EndNative(turn); return; }
                Require(trace.GrantCount(ar) == lifeGrants[0] + 1 && trace.GrantCount(am) == lifeGrants[1] + 1 &&
                    trace.GrantCount(br) == lifeGrants[2] && trace.GrantCount(bm) == lifeGrants[3],
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
                    trace.GrantCount(br) == lifeGrants[2] && bm.CombatState.Cooldown.StandardAction == 0f &&
                    bm.CombatState.Cooldown.MoveAction == 0f && world.Registry.FindActive(bm) == null && b.Participant.PartnerContext == null,
                    "Native surviving mount did not receive exactly one independent fresh turn in the original next round.");
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
