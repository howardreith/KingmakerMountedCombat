using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerMountedCombat.Diagnostics
{
    // A captured scope fences native UI, actor and cleanup mutations by object
    // identity. A declared transfer must be proved before a new scope is captured.
    internal sealed class Chunk7NativeWorldScope
    {
        private sealed class ActorIdentity
        {
            internal UnitEntityData Actor;
            internal string Id;
            internal object Descriptor, View;
        }
        private readonly object game, player, state, areaState, mainState, area;
        private readonly string campaign, name, areaId;
        private readonly Dictionary<string, ActorIdentity> actors = new Dictionary<string, ActorIdentity>(StringComparer.Ordinal);

        internal Chunk7NativeWorldScope(RuntimeRequest request, IEnumerable<UnitEntityData> initialActors)
            : this(request, initialActors, request?.SelectedChunk7Fixture?.Area) { }

        internal Chunk7NativeWorldScope(RuntimeRequest request, IEnumerable<UnitEntityData> initialActors, string expectedArea)
        {
            var current = Game.Instance;
            var fixture = request?.SelectedChunk7Fixture;
            if (request?.OwnedPairs != null && request.Validate().Count != 0)
                throw new InvalidOperationException("Inherited native scope lacks its exact closed request.");
            if (current?.Player == null || current.State?.LoadedAreaState?.MainState == null || current.CurrentlyLoadedArea == null ||
                current.Player.MainCharacter.Value == null || fixture == null ||
                current.Player.GameId != fixture.GameId ||
                current.Player.MainCharacter.Value.CharacterName != fixture.GameName ||
                string.IsNullOrEmpty(expectedArea) || current.CurrentlyLoadedArea.AssetGuidThreadSafe != expectedArea)
                throw new InvalidOperationException("Exact native fixture world is not ready for diagnostic ownership.");
            game = current; player = current.Player; state = current.State;
            areaState = current.State.LoadedAreaState; mainState = current.State.LoadedAreaState.MainState;
            area = current.CurrentlyLoadedArea; campaign = current.Player.GameId;
            name = current.Player.MainCharacter.Value.CharacterName; areaId = current.CurrentlyLoadedArea.AssetGuidThreadSafe;
            foreach (var actor in initialActors ?? Enumerable.Empty<UnitEntityData>()) Track(actor);
        }

        internal void Track(UnitEntityData actor)
        {
            RequireCurrent();
            if (actor == null) return;
            if (string.IsNullOrEmpty(actor.UniqueId)) throw new InvalidOperationException("Owned native actor has no stable ID.");
            ActorIdentity known;
            if (actors.TryGetValue(actor.UniqueId, out known))
            {
                if (!ReferenceEquals(known.Actor, actor)) throw new InvalidOperationException("Owned native actor ID was replaced.");
                return;
            }
            actors.Add(actor.UniqueId, new ActorIdentity { Actor = actor, Id = actor.UniqueId,
                Descriptor = actor.Descriptor, View = actor.View });
        }

        internal void RequireCurrent()
        {
            RequireOriginalWorld();
            var current = Game.Instance;
            if (!ReferenceEquals(areaState, current.State.LoadedAreaState) ||
                !ReferenceEquals(mainState, current.State.LoadedAreaState?.MainState) ||
                !ReferenceEquals(area, current.CurrentlyLoadedArea) || current.CurrentlyLoadedArea.AssetGuidThreadSafe != areaId)
                throw new InvalidOperationException("Captured native area state changed; diagnostic mutation and cleanup custody remain retained.");
            RequireOriginalActors();
        }

        // Does not adopt or change this scope. Only the area scenario, after proving
        // its declared native transfer and restored owners, may create a fresh scope.
        internal void RequireDeclaredAreaDestination(string expectedArea, object expectedBlueprint)
        {
            RequireOriginalWorld();
            var current = Game.Instance;
            if (string.IsNullOrEmpty(expectedArea) || expectedBlueprint == null ||
                !ReferenceEquals(expectedBlueprint, current.CurrentlyLoadedArea) ||
                current.CurrentlyLoadedArea.AssetGuidThreadSafe != expectedArea || current.State.LoadedAreaState?.MainState == null)
                throw new InvalidOperationException("Declared native destination does not match the exact resolved area and scene state.");
            RequireOriginalActors();
        }

        // During this scenario's explicitly dispatched ordinary area load, the
        // area/scene may be absent or changing. Only the captured settings lease
        // may refresh here; actor authority still requires the full scope.
        internal void RequireTransferWorld()
        {
            RequireOriginalWorld(true);
        }

        private void RequireOriginalWorld(bool allowUnloadedArea = false)
        {
            var current = Game.Instance;
            if (!ReferenceEquals(game, current) || !ReferenceEquals(player, current?.Player) ||
                !ReferenceEquals(state, current?.State) || current.Player.GameId != campaign ||
                current.Player.MainCharacter.Value?.CharacterName != name || !allowUnloadedArea && current.CurrentlyLoadedArea == null)
                throw new InvalidOperationException("Captured native world changed; diagnostic mutation and cleanup custody remain retained.");
        }

        private void RequireOriginalActors()
        {
            var current = Game.Instance;
            var live = current.State.Units.Where(x => x != null).ToArray();
            foreach (var entry in actors.Values)
            {
                if (entry.Actor.UniqueId != entry.Id || !ReferenceEquals(entry.Descriptor, entry.Actor.Descriptor) ||
                    live.Any(x => x.UniqueId == entry.Id && !ReferenceEquals(x, entry.Actor)))
                    throw new InvalidOperationException("Captured native actor identity was replaced; diagnostic custody remains retained.");
                // Native life/entity cleanup may remove an owned actor. Its absence
                // is allowed; a same-ID replacement or a new view on a live actor is not.
                if (!entry.Actor.IsInState && !live.Any(x => ReferenceEquals(x, entry.Actor))) continue;
                // A newly acquired pet may materialize its first view after creation.
                var observedView = entry.Actor.View;
                if (entry.View == null) entry.View = observedView;
                else if (!ReferenceEquals(entry.View, observedView))
                    throw new InvalidOperationException("Captured live native view changed; diagnostic custody remains retained." +
                        " actor=" + entry.Id + "; priorViewRef=" + RuntimeHelpers.GetHashCode(entry.View).ToString("x8") +
                        "; currentViewRef=" + (ReferenceEquals(observedView, null) ? "null" : RuntimeHelpers.GetHashCode(observedView).ToString("x8")) +
                        "; actorInState=" + entry.Actor.IsInState +
                        "; exactLiveActor=" + live.Any(x => ReferenceEquals(x, entry.Actor)));
            }
        }
    }
}
