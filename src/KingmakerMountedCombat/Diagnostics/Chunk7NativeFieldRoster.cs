using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerMountedCombat.Diagnostics
{
    // Player.Party is the native character list, not an actor-and-pet graph.
    // Reading it may refresh native caches; this adapter never writes membership,
    // resources, state, views, facts or native caches. Callers own readiness checks.
    internal static class Chunk7NativeFieldRoster
    {
        internal static UnitEntityData[] Capture(Player player)
        {
            if (player == null) throw new InvalidOperationException("Native player is unavailable.");
            return Capture(player.Party, player.PartyCharacters.Select(value => value.Value),
                value => value.UniqueId, value => value.Descriptor.Pet, value => value.Descriptor.Master.Value);
        }

        internal static T[] Capture<T>(IEnumerable<T> nativeParty, IEnumerable<T> partyCharacters,
            Func<T, string> identity, Func<T, T> pet, Func<T, T> master) where T : class
        {
            if (nativeParty == null || partyCharacters == null || identity == null || pet == null || master == null)
                throw new InvalidOperationException("Native field roster inputs are missing.");
            var characters = nativeParty.Take(7).ToArray();
            var declared = partyCharacters.Take(7).ToArray();
            if (characters.Length < 1 || characters.Length > 6 || declared.Length != characters.Length ||
                characters.Any(value => value == null || master(value) != null) || declared.Any(value => value == null) ||
                characters.Any(value => declared.Count(other => ReferenceEquals(value, other)) != 1) ||
                declared.Any(value => characters.Count(other => ReferenceEquals(value, other)) != 1))
                throw new InvalidOperationException("Native Party and PartyCharacters must be the exact bounded character set.");
            var actors = new List<T>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            Action<T> append = value => {
                var id = identity(value);
                if (string.IsNullOrWhiteSpace(id) || actors.Any(other => ReferenceEquals(value, other)) || !ids.Add(id))
                    throw new InvalidOperationException("Native field actor identity is missing, duplicated or conflicting.");
                actors.Add(value);
            };
            foreach (var character in characters) append(character);
            foreach (var character in characters)
            {
                var companion = pet(character);
                if (companion == null) continue;
                if (!ReferenceEquals(master(companion), character))
                    throw new InvalidOperationException("Native field companion is not reciprocally owned by its exact character.");
                append(companion);
            }
            return actors.ToArray();
        }
    }
}
