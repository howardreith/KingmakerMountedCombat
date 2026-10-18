using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7CombatScenario
    {
        private const string RealtimeHealBlueprint = "5590652e1c2225c4ca30c4a699ab3649";
        private UnitEntityData realtimeCaster;
        private SpellSlot realtimeSlot;
        private UnitUseAbility realtimeCastShell;
        private JObject realtimeCasting;
        private RealtimeAttackCheckpoint castingNeighbour;
        private void BindRealtimeCasting()
        {
            var candidates = new[] { ar, br };
            evidence["castingInventory"] = new JArray(candidates.Select(actor => new JObject { ["actorId"] = actor.UniqueId,
                ["conscious"] = actor.Descriptor.State.IsConscious,
                ["preparedSpells"] = new JArray(actor.Descriptor.Spellbooks.SelectMany(book => book.GetAllMemorizedSpells())
                    .Take(64).Select(slot => new JObject { ["blueprint"] = slot.Spell.Blueprint.AssetGuid, ["available"] = slot.Available })) }));
            var healers = candidates.Where(actor => actor.Descriptor.State.IsConscious && actor.Descriptor.Spellbooks
                .SelectMany(book => book.GetAllMemorizedSpells()).Any(slot => slot.Spell.Blueprint.AssetGuid == RealtimeHealBlueprint)).ToArray();
            Require(healers.Length == 1, "Concrete fixture prerequisite: exactly one of the two mounted riders must have the native prepared healing spell. Inventory: " + evidence["castingInventory"]);
            realtimeCaster = healers[0];
            var slots = realtimeCaster.Descriptor.Spellbooks.SelectMany(book => book.GetAllMemorizedSpells())
                .Where(slot => slot.Spell.Blueprint.AssetGuid == RealtimeHealBlueprint).ToArray();
            Require(slots.Length == 1 && slots[0].Available, "Concrete fixture prerequisite: one exact available native prepared healing slot is required; no slot will be acquired or refreshed.");
            realtimeSlot = slots[0];
        }
        private void TickRealtimeCasting()
        {
            if (realtimeStage == 0)
            {
                if (!PairIdle(a) || !PairIdle(b) || !actors.All(NativeActorReady)) return;
                BindRealtimeCasting(); BeginRealtimeNativeCast(); NextRealtime(1); return;
            }
            if (realtimeStage != 1) return;
            var neighbourDone = RealtimeAttackFinished(castingNeighbour);
            if (!neighbourDone || !realtimeCastShell.IsFinished ||
                realtimeCastShell.ExecutionProcess != null && !realtimeCastShell.ExecutionProcess.IsEnded ||
                !realtimeCaster.Commands.Empty) return;
            var effects = realtimeEffects.Capture();
            var heals = ((JArray)effects["events"]).Count(item => (string)item["kind"] == "heal-after" &&
                (string)item["actor"] == realtimeCaster.UniqueId && (string)item["target"] == am.UniqueId);
            Require(!realtimeSlot.Available && heals == 1 && am.Damage < (int)realtimeCasting["wound"] &&
                a.Relationship.State == Domain.RelationshipState.Mounted && b.Relationship.State == Domain.RelationshipState.Mounted,
                "Native mounted casting did not consume exactly one prepared slot/heal or disturbed pair ownership.");
            // The admission-time assignment stored a copy; store the settled neighbour record.
            realtimeCasting["neighbour"] = castingNeighbour.Observation.DeepClone();
            realtimeCasting["after"] = WorldSnapshot("rt-cast-after"); realtimeCasting["slotAvailableAfter"] = realtimeSlot.Available;
            realtimeCasting["damageAfter"] = am.Damage; realtimeCasting["effectsAfter"] = effects;
            realtimeCasting["shellFinished"] = realtimeCastShell.IsFinished;
            realtimeCasting["executionEnded"] = realtimeCastShell.ExecutionProcess == null || realtimeCastShell.ExecutionProcess.IsEnded;
            Pass("C7-rt-native-casting"); Pass("C7-rt-casting-neighbour");
            evidence["selectedRealtimeCaseComplete"] = true; BeginCleanup();
        }
        private void BeginRealtimeNativeCast()
        {
            Game.Instance.IsPaused = true;
            var difficulty = Game.Instance.Player.Difficulty.DamageToParty;
            Require(am.Damage == 0 && am.Descriptor.State.IsConscious && difficulty > 0 && !float.IsNaN(difficulty) && !float.IsInfinity(difficulty),
                "Native healing fixture requires an intact conscious mount and finite positive native difficulty.");
            var requested = (int)Math.Ceiling(3d / difficulty);
            Require(requested * difficulty < am.Stats.HitPoints.ModifiedValue - 2, "Native healing stimulus cannot meet its nonlethal margin.");
            realtimeCasting = new JObject { ["before"] = WorldSnapshot("rt-cast-before"), ["casterId"] = realtimeCaster.UniqueId,
                ["targetId"] = am.UniqueId, ["blueprint"] = RealtimeHealBlueprint, ["slotAvailableBefore"] = realtimeSlot.Available,
                ["damageBefore"] = am.Damage, ["requestedNativeDamage"] = requested, ["nativeDamageDifficulty"] = difficulty,
                ["effectsBefore"] = realtimeEffects.Capture() };
            evidence["casting"] = realtimeCasting;
            Rulebook.Trigger(new RuleDealDamage(target, am, new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
            realtimeCasting["wound"] = am.Damage;
            Require(am.Damage > 0 && am.Descriptor.State.IsConscious, "Native nonlethal wound did not establish a healing target.");
            Chunk7NativeFixture.Select(realtimeCaster);
            var handler = Game.Instance.SelectedAbilityHandler; var spell = realtimeSlot.Spell;
            handler.SetAbility(spell); bool clicked;
            try
            {
                var selected = handler.GetTarget(am.View.gameObject, am.Position, spell);
                Require(selected?.Unit == am && spell.IsAvailableForCast && spell.CanTarget(selected), "Exact native prepared spell target or availability differs.");
                clicked = handler.OnClick(am.View.gameObject, am.Position, 0, false, false);
            }
            finally { handler.DropAbility(); }
            realtimeCastShell = realtimeCaster.Commands.Raw.OfType<UnitUseAbility>().SingleOrDefault(command => ReferenceEquals(command.Spell, spell));
            Require(clicked && realtimeCastShell?.Executor == realtimeCaster && realtimeCastShell.Target?.Unit == am,
                "Native prepared spell click did not create the exact actor/target shell.");
            issuedCommands.Add(realtimeCastShell); realtimeCasting["clicked"] = clicked;
            realtimeCasting["command"] = trace.ObjectIdentity(realtimeCastShell); realtimeCasting["executorId"] = realtimeCastShell.Executor.UniqueId;
            actingPair = realtimeCaster == ar ? b : a; actingIndex = realtimeCaster == ar ? 1 : 0;
            BeginAttack(realtimeCaster == ar, "rt-native-casting-neighbour-primary");
            castingNeighbour = CaptureRealtimeAttack(realtimeCaster == ar ? bm : ar);
            realtimeCasting["neighbour"] = castingNeighbour.Observation;
            Chunk7NativeFixture.Select(realtimeCaster == ar ? br : ar);
            realtimeCasting["selectionAfterInput"] = SelectionManager.Instance.SelectedUnits.Single().UniqueId;
            realtimeCasting["admitted"] = WorldSnapshot("rt-cast-and-neighbour-admitted");
            realtimeCasting["admittedShells"] = new JArray(ObserveNativeShell(realtimeCastShell), ObserveNativeShell(castingNeighbour.Shell));
            Require(!realtimeCastShell.IsFinished && !castingNeighbour.Shell.IsFinished,
                "Native cast and other pair action did not overlap admitted shells.");
            Game.Instance.IsPaused = false;
        }
    }
}
