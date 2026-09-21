using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private const string CastingHealBlueprint = "5590652e1c2225c4ca30c4a699ab3649";
        private bool RealtimeCasting => Checkpoint.EndsWith("-casting", StringComparison.Ordinal);
        private UnitEntityData castingActor;
        private SpellSlot castingSlot;
        private Chunk4IncomingRuleObserver castingEffects;
        private int castingWound;
        private int castingInputs;
        private int CastingHealCount => castingEffects == null ? 0 :
            ((JArray)castingEffects.Capture()["events"]).OfType<JObject>().Count(e =>
                (string)e["kind"] == "heal-after" && (string)e["actor"] == castingActor.UniqueId &&
                (string)e["target"] == mount.UniqueId);

        private void BindNativeCasting()
        {
            // Use the fixture's actual prepared spell and native saved slots.
            // Cold code never adds a spell, slot, actor, wound or proficiency.
            var party = Game.Instance.Player.Party.Where(u => u != null && u != mount).ToArray();
            var inventory = new JArray(party.Select(u => new JObject { ["actor"] = u.UniqueId,
                ["rider"] = u == rider, ["conscious"] = u.Descriptor.State.IsConscious,
                ["prepared"] = new JArray(u.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells())
                    .Take(64).Select(s => new JObject { ["blueprint"] = s.Spell.Blueprint.AssetGuid,
                        ["available"] = s.Available })) }));
            var candidates = party.Where(u => u.Descriptor.State.IsConscious &&
                u.Descriptor.Spellbooks.Any(b => b.GetAllMemorizedSpells().Any(s =>
                    s.Spell.Blueprint.AssetGuid == CastingHealBlueprint))).ToArray();
            Check(candidates.Length == 1, "RT-one-native-prepared-healer-without-acquisition; native inventory=" +
                inventory.ToString(Newtonsoft.Json.Formatting.None));
            castingActor = candidates[0];
            var slots = castingActor.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells())
                .Where(s => s.Spell.Blueprint.AssetGuid == CastingHealBlueprint).ToArray();
            Check(slots.Length == 1, "RT-one-exact-native-heal-slot");
            castingSlot = slots[0];
            Check(castingSlot.Available == !Cold, "RT-native-slot-availability-matches-source-or-cold");
            if (Cold)
            {
                Check(persistence.LoadedData.Combat.Actors.Any(a => a.Native.Id == castingActor.UniqueId) &&
                    !castingActor.IsAIEnabled && castingActor.Commands.Empty &&
                    !NativeSaveEffectBoundary.HasUnresolvedAbilities(),
                    "RT-cold-native-caster-and-spent-slot-without-live-command-replay");
            }
            else if (castingActor != rider && castingActor != mount)
            {
                var wasEnabled = castingActor.IsAIEnabled;
                var prior = restoreRealtimeAi;
                restoreRealtimeAi = () => { try { prior?.Invoke(); } finally { castingActor.IsAIEnabled = wasEnabled; } };
                castingActor.IsAIEnabled = false;
            }
            castingEffects = new Chunk4IncomingRuleObserver(rider, mount);
            castingEffects.BeginCase(Checkpoint);
        }

        private JObject CastingObservation()
        {
            if (!RealtimeCasting || castingActor == null) return null;
            var spells = castingActor.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells())
                .Where(s => s.Spell.Blueprint.AssetGuid == CastingHealBlueprint).ToArray();
            return new JObject {
                ["caster"] = castingActor.UniqueId, ["subject"] = mount.UniqueId,
                ["blueprint"] = CastingHealBlueprint, ["slotCount"] = spells.Length,
                ["availableSlots"] = spells.Count(s => s.Available), ["slotAvailable"] = castingSlot.Available,
                ["spellAvailable"] = castingSlot.Spell.IsAvailableForCast, ["inputs"] = castingInputs,
                ["damage"] = mount.Damage, ["wound"] = castingWound, ["heals"] = CastingHealCount,
                ["standard"] = castingActor.CombatState.Cooldown.StandardAction,
                ["move"] = castingActor.CombatState.Cooldown.MoveAction,
                ["commands"] = new JArray(castingActor.Commands.Raw.Concat(castingActor.Commands.Queue)
                    .OfType<UnitUseAbility>().Distinct().Select(c => new JObject {
                        ["blueprint"] = c.Spell.Blueprint.AssetGuid, ["started"] = c.IsStarted,
                        ["acted"] = c.IsActed, ["finished"] = c.IsFinished,
                        ["hasExecution"] = c.ExecutionProcess != null, ["executionEnded"] = c.ExecutionProcess?.IsEnded })),
                ["effects"] = castingEffects?.Capture()
            };
        }

        private void BeginNativeCastingInput()
        {
            var game = Game.Instance;
            if (!castingActor.IsInCombat || !castingActor.CombatState.CanActInCombat ||
                !castingActor.Commands.Empty || castingActor.AreHandsBusyWithAnimation ||
                castingActor.CombatState.Cooldown.StandardAction > .001f ||
                castingActor.CombatState.Cooldown.MoveAction > .001f ||
                game.HandsEquipmentController.IsUpdateScheduledFor(castingActor)) return;
            var difficulty = game.Player.Difficulty.DamageToParty;
            Check(mount.Damage == 0 && mount.Descriptor.State.IsConscious &&
                difficulty > 0 && !float.IsNaN(difficulty) && !float.IsInfinity(difficulty),
                "RT-native-heal-stimulus-intact-subject");
            var requested = (int)Math.Ceiling(3d / difficulty);
            Check(requested * difficulty < mount.Stats.HitPoints.ModifiedValue - 2,
                "RT-native-nonlethal-wound-margin");
            Rulebook.Trigger(new RuleDealDamage(combatTarget, mount,
                new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
            castingWound = mount.Damage;
            Check(castingWound > 0 && mount.Descriptor.State.IsConscious, "RT-native-wound-before-heal-input");
            SelectionManager.Instance.SelectUnit(castingActor.View, true, true, false);
            var handler = game.SelectedAbilityHandler;
            var spell = castingSlot.Spell;
            handler.SetAbility(spell);
            var selected = handler.GetTarget(mount.View.gameObject, mount.Position, spell);
            Check(selected?.Unit == mount && spell.IsAvailableForCast && spell.CanTarget(selected),
                "RT-native-spell-resolves-independent-mounted-or-unmounted-subject");
            Check(handler.OnClick(mount.View.gameObject, mount.Position, 0, false, false),
                "RT-native-selected-spell-click");
            castingInputs++;
            Write("rt-casting-input", RealtimeObservation());
            stage = 30;
        }

        private void AdvanceCastingRequest()
        {
            var running = castingActor.Commands.Raw.OfType<UnitUseAbility>()
                .Where(c => c.Spell.Blueprint.AssetGuid == CastingHealBlueprint && c.IsRunning && !c.IsActed).ToArray();
            if (running.Length == 0) return;
            Check(running.Length == 1 && CastingHealCount == 0 && castingSlot.Available && mount.Damage == castingWound,
                "RT-save-request-during-native-cast-before-effect-or-slot-spend");
            Write("rt-casting-save-request", RealtimeObservation());
            RequestRealtimeSave();
            Check(persistence.DeferredSaveCount == 1 && persistence.SnapshotCount == 0 &&
                NativeDeferredSave.Waiting(LoadingProcess.Instance) && !controls.SerializationSuspended,
                "RT-casting-native-save-defers-before-clock-pause");
        }

        private void BeginCastingContinuation()
        {
            Check(!castingSlot.Available && !castingSlot.Spell.IsAvailableForCast &&
                CastingHealCount == (Cold ? 0 : 1), "RT-consumed-native-spell-cannot-be-recast-or-replayed");
            Check(realtimeProbe.RiderResolvedCount == 0, "RT-casting-save-did-not-invent-rider-attacks");
            QueueRealtimeAttack();
            Write("rt-casting-continuation", RealtimeObservation());
            stage = 31;
        }

        private void AdvanceCastingContinuation()
        {
            Check(!castingSlot.Available && CastingHealCount == (Cold ? 0 : 1),
                "RT-spell-slot-and-healing-stay-consumed-during-normal-play");
            if (realtimeProbe.RiderResolvedCount == 0) return;
            Check(realtimeProbe.RiderResolvedCount == 1 && realtimeInputRequests == 1 &&
                realtimeProbe.PairForcedD20Count == 0, "RT-first-ordinary-post-casting-attack-once");
            Write("rt-casting-first-delivery", RealtimeObservation());
            BeginRealtimeContinuation(false);
        }
    }
}
