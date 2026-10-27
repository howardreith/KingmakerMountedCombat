using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7AssistedColdInventoryScenario
    {
        private const string DepartureActorId = "8265f8cf-0969-4ec7-a266-2448de91c8c4";
        private Chunk7NativePartyFieldDeparture departure;
        private UnitEntityData departureActor;
        private object departureDescriptor;

        private void StartNativeDeparture()
        {
            Require(selection.IsFifthHorseDeparture && departure == null && actors.Length == 11 &&
                characters.Length == 6 && remote.Length == 0 && game.CurrentMode == GameModeType.Default,
                "Native departure requires the exact six-character, five-Horse source.");
            departureActor = characters.Single(value => value.UniqueId == DepartureActorId);
            Require(departureActor.Descriptor.Pet == null && departureActor.Descriptor.Master.Value == null &&
                thirdHorseFactsAtLoad.Count == 5 &&
                thirdHorseFactsAtLoad.All(value => (string)value["riderId"] != DepartureActorId && (string)value["mountId"] != DepartureActorId),
                "Only the exact petless Linzi may leave; all five native Horse owners remain.");
            departureDescriptor = departureActor.Descriptor;
            departure = new Chunk7NativePartyFieldDeparture(departureActor,
                characters.Select(value => value.UniqueId).ToArray(), RequireDepartureAuthority, RequireDepartureCleanupAuthority);
            departure.Open();
        }

        private void RequireDepartureAuthority()
        {
            RequireAuthority(false); RequireNoOwners(); RequireNoEffects(); VerifySourceBytes();
            Require(selection.IsFifthHorseDeparture && admitted && !Busy() && !game.Player.IsInCombat &&
                !game.CutsceneLock && !game.IsModeActive(GameModeType.Dialog),
                "Native departure lost its exact isolated zero-write world.");
        }

        private void RequireDepartureCleanupAuthority()
        {
            RequireAuthority(true); RequireNoOwners();
            Require(selection.IsFifthHorseDeparture && admitted && !Busy() && !game.Player.IsInCombat &&
                !game.CutsceneLock && !game.IsModeActive(GameModeType.Dialog),
                "Native departure cleanup cannot cross an unrelated world, worker or combat boundary.");
        }

        private void RequireDepartureWorld()
        {
            Require(selection.IsFifthHorseDeparture && departureActor?.UniqueId == DepartureActorId &&
                ReferenceEquals(departureActor.Descriptor, departureDescriptor),
                "The declared native departure actor or descriptor was substituted.");
            RequireLoader();
            Require(JToken.DeepEquals(loadedSemantics, CaptureSemantics()),
                "Native departure changed source money, progression or reciprocal pet semantics.");
            Require(JToken.DeepEquals(thirdHorseFactsAtLoad, CaptureThirdHorseFacts()),
                "Native departure changed a retained sourced Horse fact or rank.");
            var current = Chunk7NativeFieldRoster.Capture(game.Player);
            var retained = actors.Where(value => !ReferenceEquals(value, departureActor)).ToArray();
            if (!departure.CommitRequested)
            {
                Require(current.Length == actors.Length &&
                    game.Player.PartyCharacters.Select(value => value.Value).SequenceEqual(characters) &&
                    game.Player.RemoteCompanions.Select(value => value.Value).SequenceEqual(remote) &&
                    current.Count(value => ReferenceEquals(value, departureActor)) == 1,
                    "Native field changed before the declared selector Go.");
            }
            else
            {
                Require(current.Length == retained.Length &&
                    game.Player.PartyCharacters.Select(value => value.Value).SequenceEqual(characters.Where(value => !ReferenceEquals(value, departureActor))) &&
                    game.Player.RemoteCompanions.Count == 1 &&
                    ReferenceEquals(game.Player.RemoteCompanions.Single().Value, departureActor) &&
                    !current.Any(value => ReferenceEquals(value, departureActor)),
                    "Native departure changed more than the exact character's field/remote membership.");
            }
            foreach (var actor in retained)
            {
                var index = Array.IndexOf(actors, actor);
                Require(current.Count(value => ReferenceEquals(value, actor)) == 1 &&
                    ReferenceEquals(actor.Descriptor, descriptors[index]) && ReferenceEquals(actor.View, views[index]) &&
                    actor.IsInState && actor.IsInGame && actor.Commands.Empty,
                    "A retained native character/Horse, descriptor, view or command state changed during departure.");
            }
        }

        private void CompleteNativeDeparture()
        {
            RequireDepartureAuthority();
            Require(departure.Completed, "Native departure did not complete its one selector commit.");
            evidence["nativeDeparture"] = departure.Capture();
            evidence["departureRetention"] = new JObject {
                ["actorId"] = departureActor.UniqueId, ["blueprint"] = departureActor.Blueprint.AssetGuid,
                ["object"] = probe.Identity(departureActor), ["descriptor"] = probe.Identity(departureActor.Descriptor),
                ["semanticsBefore"] = loadedSemantics.DeepClone(), ["semanticsAfter"] = CaptureSemantics(),
                ["horseFactsAfter"] = CaptureThirdHorseFacts(), ["retainedFieldActorCount"] = 10,
                ["nativeWrites"] = 0, ["archiveUnchanged"] = true };
        }
    }
}
