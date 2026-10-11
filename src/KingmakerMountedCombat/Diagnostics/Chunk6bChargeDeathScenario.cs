using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Utility;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private NativeDeathPolicyLease chargeDeathPolicy;
        private PairedConditionObserver chargeDeathObserver;
        private JObject chargeDeathFacts;
        private UnitEntityData chargeDeathSubject;
        private int chargeDeathStage;
        private bool ChargeDeathCase => Chunk6bChargeCaseId == "C6B-CHARGE-mount-dead" ||
            Chunk6bChargeCaseId == "C6B-CHARGE-rider-dead";

        private JObject CaptureChargeDeathState() => new JObject {
            ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["rider"] = CaptureChunk4LifeActor(rider), ["mount"] = CaptureChunk4LifeActor(horse),
            ["playerInCombat"] = Game.Instance.Player.IsInCombat,
            ["riderInCombat"] = rider.IsInCombat, ["mountInCombat"] = horse.IsInCombat,
            ["controllerInitialized"] = Game.Instance.TurnBasedCombatController.Initialized,
            ["relationship"] = relationship.State.ToString(),
            ["presentationResidue"] = relationship.Runtime.HasPresentationAttachmentResidue,
            ["ownership"] = combat.CaptureChargeOwnership(),
            ["lastDrained"] = combat.LastDrainedChargeOwnership?.DeepClone(),
            ["boundaries"] = combat.CaptureChargeNativeBoundaries(),
            ["actors"] = CaptureChunk6bChargeActors("native-death"),
            ["riderGrants"] = allocationTrace.GrantCount(rider), ["mountGrants"] = allocationTrace.GrantCount(horse),
            ["trackedAllocations"] = combat.TrackedActorAllocations,
            ["pairedIdentity"] = combat.PairedActivationIdentity != null,
            ["partnerContext"] = combat.PairedPartnerContext != null
        };

        private bool InterveneChargeDeath(string kind)
        {
            if (kind != "native-mount-death" && kind != "native-rider-death") return false;
            var permanent = kind == "native-rider-death";
            chargeDeathSubject = permanent ? rider : horse;
            var subject = chargeDeathSubject;
            if (!subject.Descriptor.State.IsConscious || subject.Descriptor.IsEssentialForGame ||
                subject.Descriptor.State.Immortality || subject == Game.Instance.Player.MainCharacter.Value ||
                !target.IsPlayersEnemy || target.IsPlayerFaction || !combat.HasChargeOwnership)
                throw new InvalidOperationException("Charge death fixture lacks an eligible disposable native subject and live charge.");
            chargeDeathFacts = new JObject { ["subject"] = subject.UniqueId, ["subjectKind"] = permanent ? "rider" : "mount" };
            if (allocationTrace == null) allocationTrace = new NativeActorAllocationTrace(rider, horse, combat);
            allocationTrace.BeginEncounter(Chunk6bChargeCaseId);
            chargeDeathPolicy = new NativeDeathPolicyLease(permanent);
            chargeDeathFacts["policy"] = chargeDeathPolicy.Capture();
            if (!permanent && Game.Instance.Player.Difficulty.TrueDeath)
                throw new InvalidOperationException("Fixture intake does not permit native mount recovery; no death was dispatched.");
            chargeDeathObserver = new PairedConditionObserver(rider, horse, true);
            var difficulty = Game.Instance.Player.Difficulty.DamageToParty;
            var threshold = checked(subject.Stats.HitPoints.ModifiedValue + subject.Stats.Constitution.ModifiedValue);
            var needed = checked(threshold + 1 - subject.Damage + subject.Stats.TemporaryHitPoints.ModifiedValue);
            if (difficulty <= 0 || float.IsNaN(difficulty) || float.IsInfinity(difficulty) || needed <= 0)
                throw new InvalidOperationException("Charge death fixture has no finite positive native damage window.");
            var requested = checked((int)Math.Ceiling((needed + 1d) / difficulty));
            chargeDeathFacts["before"] = CaptureChargeDeathState();
            chargeDeathFacts["source"] = target.UniqueId;
            chargeDeathFacts["requestedDamage"] = requested;
            chargeDeathFacts["damageToParty"] = difficulty;
            chargeDeathFacts["deathThreshold"] = threshold;
            chargeDeathFacts["damageDispatches"] = 1;
            var damage = Rulebook.Trigger(new RuleDealDamage(target, subject,
                new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
            chargeDeathFacts["nativeDamage"] = damage.Damage;
            chargeDeathFacts["nativeDamageBeforeDifficulty"] = damage.DamageBeforeDifficulty;
            chargeDeathFacts["afterDamage"] = CaptureChargeDeathState();
            chargeDeathStage = 1;
            return true;
        }

        private bool ObserveChargeDeathSettlement()
        {
            if (!ChargeDeathCase || chargeDeathFacts == null) return true;
            chargeDeathFacts["progress"] = CaptureChargeDeathState();
            if (chargeDeathStage == 1)
            {
                if (!chargeDeathSubject.Descriptor.State.IsDead || combat.HasChargeOwnership || combat.HasActiveCommand ||
                    !rider.Commands.Empty || !horse.Commands.Empty || relationship.State != RelationshipState.Unmounted) return false;
                chargeDeathFacts["terminated"] = CaptureChargeDeathState();
                var source = Game.Instance.Player.MainCharacter.Value;
                if (source == null || !source.Descriptor.State.IsConscious || !target.IsPlayersEnemy ||
                    target.Descriptor.IsEssentialForGame || target.Descriptor.State.Immortality || target == source)
                    throw new InvalidOperationException("Charge death completion lacks the exact disposable enemy and conscious native source.");
                var requested = checked(target.Stats.HitPoints.ModifiedValue + target.Stats.Constitution.ModifiedValue +
                    target.Stats.TemporaryHitPoints.ModifiedValue - target.Damage + 16);
                chargeDeathFacts["enemyBefore"] = CaptureChunk4LifeActor(target);
                chargeDeathFacts["enemyDamageDispatches"] = 1;
                chargeDeathFacts["enemyDamageSource"] = source.UniqueId;
                chargeDeathFacts["enemyRequestedDamage"] = requested;
                chargeDeathFacts["enemyDamageFrame"] = Time.frameCount;
                chargeDeathFacts["enemyDamageGameTicks"] = Game.Instance.TimeController.GameTime.Ticks;
                var damage = Rulebook.Trigger(new RuleDealDamage(source, target,
                    new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
                chargeDeathFacts["enemyNativeDamage"] = damage.Damage;
                chargeDeathStage = 2;
                return false;
            }
            if (chargeDeathStage == 2)
            {
                if (!target.Descriptor.State.IsDead || Game.Instance.Player.IsInCombat || rider.IsInCombat || horse.IsInCombat ||
                    Game.Instance.TurnBasedCombatController.Initialized || combat.ChargeNativeBoundaryPending ||
                    combat.TrackedActorAllocations != 0 || combat.PairedActivationIdentity != null || combat.PairedPartnerContext != null ||
                    !rider.Commands.Empty || !horse.Commands.Empty) return false;
                if (chargeDeathSubject == horse && !horse.Descriptor.State.IsConscious) return false;
                chargeDeathFacts["encounterExit"] = CaptureChargeDeathState();
                chargeDeathFacts["enemyAfter"] = CaptureChunk4LifeActor(target);
                chargeDeathFacts["enemyLifeTransitions"] = targetService.LifeTransitionCount;
                chargeDeathPolicy.Dispose();
                chargeDeathFacts["policy"] = chargeDeathPolicy.Capture();
                chargeDeathStage = 3;
            }
            if (Game.Instance.TimeController.GameTime.Ticks - (long)chargeDeathFacts["encounterExit"]["gameTicks"] < TimeSpan.TicksPerSecond / 4 ||
                !rider.Commands.Empty || !horse.Commands.Empty || horse.View.AgentASP.IsReallyMoving ||
                rider.AreHandsBusyWithAnimation || horse.AreHandsBusyWithAnimation ||
                Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(rider) || Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(horse)) return false;
            chargeDeathFacts["afterPolicyRestore"] = CaptureChargeDeathState();
            chargeDeathFacts["lifeEvents"] = chargeDeathObserver.Capture();
            chargeDeathFacts["allocationTrace"] = allocationTrace.Capture();
            return true;
        }

        // Retain the exact death witness through root cleanup and final selection restoration.
        private UnitEntityData ChargeFinalDeathSubject
        {
            get
            {
                var subject = chargeDeathSubject;
                if (!IsChunk6bCharge || subject != rider || chargeDeathFacts == null ||
                    (int?)chargeDeathFacts["damageDispatches"] != 1 || ((int?)chargeDeathFacts["nativeDamage"] ?? 0) <= 0 ||
                    !subject.IsInState || !subject.Descriptor.State.IsDead || !subject.Descriptor.State.IsFinallyDead ||
                    subject.Descriptor.State.IsConscious || subject.IsDirectlyControllable) return null;
                var events = (chargeDeathObserver?.Capture() ?? chargeDeathFacts["lifeEvents"] as JObject)?["events"] as JArray;
                return events != null && events.OfType<JObject>().Any(item => (string)item["kind"] == "native-life-state" &&
                    (string)item["actor"] == subject.UniqueId && (string)item["lifeState"] == "Dead" &&
                    item["nativeSource"] is JArray sources && sources.OfType<JObject>().Any(source => (string)source["token"] == "06009164" &&
                        (string)source["assemblyMvid"] == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7")) ? subject : null;
            }
        }

        private void CleanupChargeDeath()
        {
            try
            {
                if (chargeDeathPolicy != null)
                    try { chargeDeathPolicy.Dispose(); }
                    finally { chargeDeathFacts["policy"] = chargeDeathPolicy.Capture(); }
            }
            finally
            {
                if (chargeDeathObserver != null)
                {
                    chargeDeathFacts["lifeEvents"] = chargeDeathObserver.Capture();
                    chargeDeathObserver.Dispose(); chargeDeathObserver = null;
                }
            }
        }
    }
}
