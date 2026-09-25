using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private const string RoundEffectGuid = "c51fd80d1c56ce445acb3a3c5ca99e9d";
        private Buff riderRoundEffect;
        private Buff mountRoundEffect;
        private TurnController roundEffectWaitTurn;
        private int roundEffectWaitFrame;
        private bool RoundEffectCase => request.PersistenceCase == "round-effect";

        private void InstallRoundEffects()
        {
            var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>(RoundEffectGuid);
            Check(blueprint != null && blueprint.ComponentsArray.Length == 1 &&
                blueprint.ComponentsArray[0].GetType() == typeof(AddEffectFastHealing) &&
                ((AddEffectFastHealing)blueprint.ComponentsArray[0]).Heal == 1 &&
                blueprint.TickTime == TimeSpan.FromSeconds(6),
                "P03-exact-native-food-healing-definition-and-timer");
            foreach (var actor in new[] { rider, mount })
            {
                Check(!actor.IsInCombat && actor.Damage == 0 &&
                    actor.Stats.HitPoints.ModifiedValue > 3 &&
                    !actor.Buffs.Enumerable.Any(b => b.Blueprint.AssetGuid == RoundEffectGuid),
                    "P03-owned-healthy-actor-has-no-existing-round-effect");
                actor.Descriptor.Damage = 3;
                Check(actor.Buffs.AddBuff(blueprint, actor, TimeSpan.FromSeconds(600)) != null,
                    "P03-native-buff-applied-to-its-own-caster");
            }
            BindRoundEffects();
            Write("round-effect-provisioned", RoundEffects());
        }

        private void BindRoundEffects()
        {
            riderRoundEffect = rider.Buffs.Enumerable.Single(b => b.Blueprint.AssetGuid == RoundEffectGuid);
            mountRoundEffect = mount.Buffs.Enumerable.Single(b => b.Blueprint.AssetGuid == RoundEffectGuid);
            Check(riderRoundEffect.Context.MaybeCaster == rider && mountRoundEffect.Context.MaybeCaster == mount,
                "P03-native-round-effect-owner-and-caster-identity");
        }

        private JObject RoundEffects() => new JObject
        {
            ["blueprint"] = RoundEffectGuid,
            ["rider"] = RoundEffect(rider, riderRoundEffect),
            ["mount"] = RoundEffect(mount, mountRoundEffect),
            ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["turnStartTicks"] = Game.Instance.TurnBasedCombatController.TurnStartTime.Ticks,
            ["current"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit.UniqueId
        };

        private static JObject RoundEffect(UnitEntityData actor, Buff buff) => new JObject
        {
            ["actor"] = actor.UniqueId, ["damage"] = actor.Damage, ["rounds"] = buff.RoundNumber,
            ["nextEventTicks"] = buff.NextEventTime.Ticks, ["endTicks"] = buff.EndTime.Ticks,
            ["active"] = buff.Active, ["suppressed"] = buff.IsSuppressed
        };

        private void ObserveColdRoundEffects()
        {
            // Bind only the native deserialized buffs. Never add a fact or repair health here.
            BindRoundEffects();
            CheckRoundEffects(1, "P03-no-round-heal-replay-during-cold-materialization");
            Write("round-effect-loaded", RoundEffects());
        }

        private void BeginRoundEffectFixture()
        {
            roundEffectWaitTurn = null;
            stage = 80;
        }

        private bool AdvanceRoundEffectFixture(TurnController turn)
        {
            if (stage != 80) return false;
            if (turn == null || !turn.IsActing && turn.Status != TurnController.TurnStatus.Preparing) return true;
            if (turn.Unit != rider) { EndFixtureTurn(turn); return true; }
            if (!PairIdle) return true;
            if (riderRoundEffect.RoundNumber == 0 && mountRoundEffect.RoundNumber == 0)
            {
                EndFixtureTurn(turn); return true;
            }
            if (!RoundEffectsReady(turn, 1)) return true;
            CheckRoundEffects(1, "P03-real-first-round-heals-both-actors-once");
            savedBoundary = turn;
            savedSequence = combat.PairedActivationSequence;
            savedRound = Game.Instance.TurnBasedCombatController.RoundNumber;
            Write("round-effect-applied", RoundEffects());
            stage = 3; return true;
        }

        private bool RoundEffectsReady(TurnController turn, int rounds)
        {
            if (riderRoundEffect.RoundNumber == rounds && mountRoundEffect.RoundNumber == rounds) return true;
            Check(riderRoundEffect.RoundNumber <= rounds && mountRoundEffect.RoundNumber <= rounds,
                "P03-native-round-effect-does-not-run-twice");
            if (!ReferenceEquals(roundEffectWaitTurn, turn))
            {
                roundEffectWaitTurn = turn;
                roundEffectWaitFrame = Time.frameCount;
            }
            if (Time.frameCount > roundEffectWaitFrame + 120)
            {
                Write("round-effect-missing", RoundEffects());
                throw new InvalidOperationException("A real paired activation did not deliver its due native round effect.");
            }
            return false;
        }

        private void CheckRoundEffects(int rounds, string label)
        {
            Check(riderRoundEffect.RoundNumber == rounds && mountRoundEffect.RoundNumber == rounds &&
                rider.Damage == 3 - rounds && mount.Damage == 3 - rounds &&
                riderRoundEffect.Active && mountRoundEffect.Active &&
                !riderRoundEffect.IsSuppressed && !mountRoundEffect.IsSuppressed, label);
        }

        private void ContinueRoundEffects()
        {
            CheckRoundEffects(1, "P03-saving-or-loading-does-not-repeat-or-erase-delivered-healing");
            Write("round-effect-retained", RoundEffects());
            stage = 9;
        }
    }
}
