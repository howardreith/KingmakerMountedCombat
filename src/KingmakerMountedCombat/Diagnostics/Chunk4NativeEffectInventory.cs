using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Bounded identity/availability inventory only; no facts, slots or effects are changed.
    internal static class Chunk4NativeEffectInventory
    {
        internal static JObject Capture(UnitEntityData rider, UnitEntityData mount)
        {
            return new JObject {
                ["assemblyMvid"] = typeof(AbilityData).Assembly.ManifestModule.ModuleVersionId.ToString(),
                ["damageToParty"] = Game.Instance.Player.Difficulty.DamageToParty,
                ["trueDeath"] = Game.Instance.Player.Difficulty.TrueDeath,
                ["deathDoorCondition"] = Game.Instance.Player.Difficulty.DeathDoorCondition,
                ["actors"] = new JArray(Game.Instance.Player.PartyCharacters.Select(reference => reference.Value)
                    .Concat(new[] { rider, mount }).Where(actor => actor != null).Distinct().Select(actor => new JObject {
                        ["id"] = actor.UniqueId, ["blueprint"] = actor.Blueprint.AssetGuid,
                        ["rider"] = actor == rider, ["mount"] = actor == mount,
                        ["essential"] = actor.Descriptor.IsEssentialForGame,
                        ["immortality"] = (bool)actor.Descriptor.State.Immortality,
                        ["allowDyingCondition"] = (bool)actor.Descriptor.State.AllowDyingCondition,
                        ["regenerate"] = (bool)actor.Descriptor.State.IsRegenerate,
                        ["lifeState"] = actor.Descriptor.State.LifeState.ToString(),
                        ["damage"] = actor.Damage, ["hp"] = actor.Stats.HitPoints.ModifiedValue,
                        ["constitution"] = actor.Stats.Constitution.ModifiedValue,
                        ["facts"] = new JArray(actor.Descriptor.Abilities.Enumerable.Select(fact => CaptureAbility(fact.Data))),
                        ["spellbooks"] = new JArray(actor.Descriptor.Spellbooks.Select(book => new JObject {
                            ["blueprint"] = book.Blueprint.AssetGuid, ["casterLevel"] = book.CasterLevel,
                            ["memorized"] = new JArray(book.GetAllMemorizedSpells().Select(slot => new JObject {
                                ["level"] = slot.SpellLevel, ["available"] = slot.Available,
                                ["spell"] = CaptureAbility(slot.Spell) })),
                            ["known"] = new JArray(book.GetAllKnownSpells().Select(CaptureAbility))
                        }))
                    }))
            };
        }

        private static JToken CaptureAbility(AbilityData ability)
        {
            if (ability == null) return JValue.CreateNull();
            var blueprint = ability.Blueprint;
            return new JObject {
                ["blueprint"] = blueprint.AssetGuid, ["assetName"] = blueprint.name,
                ["available"] = ability.IsAvailableForCast, ["reason"] = ability.GetUnavailableReason(),
                ["components"] = new JArray(blueprint.ComponentsArray.Select(component => component.GetType().FullName)),
                ["actions"] = new JArray(blueprint.GetComponents<AbilityEffectRunAction>().Select(effect => new JObject {
                    ["savingThrow"] = effect.SavingThrowType.ToString(),
                    ["types"] = new JArray(effect.Actions.Actions.Select(action => action.GetType().FullName))
                }))
            };
        }
    }
}
