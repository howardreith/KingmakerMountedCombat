using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        // F2 r39: the six-pair guard still fails the run; before throwing it records, read-only,
        // who is in native combat and the pairs' input state, so a later incident names its
        // initiator. A hostile combatant is neither a pair actor (the KMC Horses are pets, not
        // listed in Player.Party), a party member nor player faction.
        private void RefuseSixPairCombatEntry()
        {
            var classification = Chunk7SixPairCombatEntry.Unclassified;
            try
            {
                var game = Game.Instance; var pairActors = Actors();
                var party = game.Player.Party; var awake = game.State.AwakeUnits.ToArray();
                var combatants = game.State.Units.Where(unit => unit != null && unit.IsInCombat).ToArray();
                var hostile = combatants.Count(unit =>
                    Chunk7SixPairCombatEntry.IsHostileCombatant(pairActors.Contains(unit), party.Contains(unit), unit.IsPlayerFaction));
                var commandFree = pairActors.All(actor => actor != null && actor.IsInState && actor.Descriptor.State.IsConscious &&
                    actor.Commands != null && actor.Commands.Empty);
                var inFlight = shell != null || quietWindow != null || controls.HasUnsettledRelationshipTransition ||
                    world.UnifiedTurn.HasUnsettledPreparation;
                classification = Chunk7SixPairCombatEntry.Classify(hostile, commandFree, inFlight);
                var record = new JObject { ["classification"] = classification, ["frame"] = Time.frameCount,
                    ["gameTicks"] = game.TimeController.GameTime.Ticks, ["step"] = step, ["cycle"] = sixCycle, ["index"] = sixIndex,
                    ["sampleGroup"] = sixSampleGroup, ["sampleWindow"] = sixSampleWindow, ["samplingOpen"] = measurement != null,
                    ["activePairs"] = sixActive.Count, ["activeInput"] = activeInput, ["inputShellOpen"] = shell != null,
                    ["groundMoveOpen"] = quietWindow != null, ["unsettledRelationshipTransition"] = controls.HasUnsettledRelationshipTransition,
                    ["unsettledPreparation"] = world.UnifiedTurn.HasUnsettledPreparation,
                    ["pairActorsCommandFree"] = commandFree, ["kmcInputInFlight"] = inFlight, ["hostileCombatants"] = hostile,
                    ["turnBased"] = CombatController.IsInTurnBasedCombat(), ["combatants"] = new JArray(combatants.Select(unit =>
                    {
                        UnitEntityData nearest = null; var distance = float.MaxValue;
                        foreach (var actor in pairActors)
                        {
                            if (actor == null || ReferenceEquals(actor, unit)) continue;
                            var d = Vector3.Distance(actor.Position, unit.Position);
                            if (d < distance) { distance = d; nearest = actor; }
                        }
                        return new JObject { ["id"] = unit.UniqueId, ["blueprint"] = unit.Blueprint?.AssetGuid,
                            ["faction"] = unit.Faction?.AssetGuid, ["groupId"] = unit.GroupId, ["playerFaction"] = unit.IsPlayerFaction,
                            ["inParty"] = party.Contains(unit), ["pairActor"] = pairActors.Contains(unit), ["awake"] = awake.Contains(unit),
                            ["conscious"] = unit.Descriptor.State.IsConscious, ["inFog"] = unit.IsInFogOfWar, ["position"] = Point(unit.Position),
                            ["nearestPairActor"] = nearest?.UniqueId, ["nearestPairDistance"] = nearest == null ? (JToken)null : distance,
                            ["groupMemoryEnemies"] = new JArray(unit.Memory.Enemies.Where(item => item.Unit != null)
                                .Select(item => item.Unit.UniqueId).Distinct().OrderBy(id => id, StringComparer.Ordinal)) };
                    })) };
                evidence["sixPairCombatEntry"] = record;
            }
            catch (Exception error)
            {
                classification = Chunk7SixPairCombatEntry.Unclassified;
                evidence["sixPairCombatEntryError"] = error.GetType().Name + ": " + error.Message;
            }
            throw new InvalidOperationException(Chunk7SixPairCombatEntry.Message(classification));
        }
    }
}
