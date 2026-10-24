using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Observation only. All state comes from the installed native world and the
    // exact production ownership boundary, never a fabricated DTO restoration.
    internal static class Chunk7ArchiveNativeSnapshot
    {
        internal static JObject Capture(MountedWorldService world, MountedArchiveData source)
        {
            if (world == null || Game.Instance?.Player == null || source == null)
                throw new InvalidOperationException("A live native archive world is required for observation.");
            var ids = (source.Combat?.Actors.Select(x => x.Native.Id) ?? Enumerable.Empty<string>())
                .Concat(source.Pairs.SelectMany(x => new[] { x.Rider.Id, x.Mount.Id })).Distinct(StringComparer.Ordinal).ToArray();
            var units = Game.Instance.State.Units.Concat(Game.Instance.Player.AllCharacters).Where(x => x != null).Distinct().ToArray();
            var actors = ids.Select(id => {
                var matches = units.Where(x => x.UniqueId == id).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("Observed saved actor is missing or ambiguous: " + id);
                return matches[0];
            }).ToArray();
            SavedCombatData combat = null;
            if (source.Combat != null)
            {
                combat = NativeCombatTurnPersistence.Capture(Game.Instance.TurnBasedCombatController, actors);
                world.UnifiedTurn.CaptureSharedPersistence(combat);
            }
            var participation = new List<SavedPairParticipation>();
            foreach (var boundary in world.Pairs)
            {
                var state = boundary.Participant.CaptureParticipation();
                if (state == null) continue;
                var owner = boundary.Relationship.PairOwner;
                if (owner == null || !world.Registry.Owns(owner))
                    throw new InvalidOperationException("Observed native participation has no exact world owner.");
                participation.Add(new SavedPairParticipation { OwnerId = owner.Id.ToString("N"), Generation = owner.Generation,
                    MountedPairId = owner.State == MountedPairOwnershipState.Active ? owner.Id.ToString("N") : null, State = state });
            }
            return new JObject {
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["turnBased"] = Kingmaker.UI.SettingsUI.SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue,
                ["playerInCombat"] = Game.Instance.Player.IsInCombat,
                ["actors"] = new JArray(actors.Select(x => JObject.FromObject(NativeCombatActorPersistence.Capture(x), MountedSaveCodec.CreateSerializer()))),
                ["combat"] = combat == null ? null : JObject.FromObject(combat, MountedSaveCodec.CreateSerializer()),
                ["participations"] = JArray.FromObject(participation, MountedSaveCodec.CreateSerializer()),
                ["owners"] = new JArray(world.Registry.Owners.Select(x => new JObject {
                    ["ownerId"] = x.Id.ToString("N"), ["generation"] = x.Generation,
                    ["riderId"] = x.Rider?.UniqueId, ["mountId"] = x.Mount?.UniqueId, ["state"] = x.State.ToString() })),
                ["nativeControls"] = JArray.FromObject(world.NativeControls.CapturePersistentSlots(), MountedSaveCodec.CreateSerializer())
            };
        }
    }
}
