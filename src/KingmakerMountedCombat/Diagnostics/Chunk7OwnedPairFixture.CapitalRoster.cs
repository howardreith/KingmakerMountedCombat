using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7OwnedPairFixture
    {
        // Matrix rows 58-59, batch196 r58: native capital mode takes the hired companion pair (inherited pair B)
        // out of the scene (IsInGame false) while the main pair stays. The roster rule admits exactly that pair,
        // only in a capital, in one closed membership shape that is recorded:
        //   field  - still in Party/PartyCharacters, not a remote companion;
        //   remote - out of Party/PartyCharacters and a remote companion;
        //   absent - out of Party/PartyCharacters and not a remote companion.
        // Every other captured actor keeps the exact roster contract.
        private bool CapitalOutOfSceneActive => capitalOutOfScene != null && game.CurrentlyLoadedArea != null && game.CurrentlyLoadedArea.IsCapital;
        private bool IsCapitalOutOfScene(UnitEntityData actor) => capitalOutOfScene != null && capitalOutOfScene.Any(value => ReferenceEquals(value, actor));
        internal UnitEntityData[] CapitalOutOfScene => capitalOutOfScene;

        private string CapitalOutOfSceneShape(UnitEntityData[] currentParty, UnitEntityData[] currentCharacters)
        {
            var rider = capitalOutOfScene[0]; var mount = capitalOutOfScene[1];
            var inCharacters = currentCharacters.Count(value => ReferenceEquals(value, rider));
            var riderInParty = currentParty.Count(value => ReferenceEquals(value, rider));
            var mountInParty = currentParty.Count(value => ReferenceEquals(value, mount));
            var remote = game.Player.RemoteCompanions.Count(value => ReferenceEquals(value.Value, rider));
            var mountRemote = game.Player.RemoteCompanions.Count(value => ReferenceEquals(value.Value, mount));
            Require(inCharacters == riderInParty && riderInParty == mountInParty && inCharacters <= 1 && remote <= 1 && mountRemote == 0,
                "Capital out-of-scene pair has an inconsistent native roster membership.");
            if (inCharacters == 1 && remote == 0) return "field";
            if (inCharacters == 0 && remote == 1) return "remote";
            if (inCharacters == 0 && remote == 0) return "absent";
            throw new InvalidOperationException("Capital out-of-scene pair is both a field character and a remote companion.");
        }

        private void RequireCapitalRoster(UnitEntityData[] currentParty, UnitEntityData[] currentCharacters)
        {
            var rider = capitalOutOfScene[0]; var mount = capitalOutOfScene[1];
            Require(capitalOutOfScene.Length == 2 && Actors.Length >= 4 && ReferenceEquals(rider, Actors[2]) && ReferenceEquals(mount, Actors[3]) &&
                ReferenceEquals(Actors[0], game.Player.MainCharacter.Value) && !ReferenceEquals(rider, game.Player.MainCharacter.Value) &&
                !rider.IsInGame && !mount.IsInGame && !rider.IsDirectlyControllable && !mount.IsDirectlyControllable,
                "Capital out-of-scene custody is not exactly the companion pair natively out of the scene.");
            var shape = CapitalOutOfSceneShape(currentParty, currentCharacters);
            var expectedParty = shape == "field" ? Party : Party.Where(actor => !IsCapitalOutOfScene(actor)).ToArray();
            var expectedCharacters = shape == "field" ? partyCharacters : partyCharacters.Where(actor => !ReferenceEquals(actor, rider)).ToArray();
            var remoteCaptured = game.Player.RemoteCompanions.Where(value => Party.Any(actor => ReferenceEquals(value.Value, actor))).Select(value => value.Value).ToArray();
            Require(currentParty.Length == expectedParty.Length && expectedParty.All(actor => currentParty.Count(value => ReferenceEquals(value, actor)) == 1) &&
                currentCharacters.Length == expectedCharacters.Length && expectedCharacters.All(actor => currentCharacters.Count(value => ReferenceEquals(value, actor)) == 1) &&
                remoteCaptured.Length == (shape == "remote" ? 1 : 0) && remoteCaptured.All(actor => ReferenceEquals(actor, rider)),
                "Inherited native field roster changed beyond the capital's out-of-scene companion pair.");
        }

        internal JObject CaptureCapitalOutOfScene()
        {
            var currentParty = Chunk7NativeFieldRoster.Capture(game.Player);
            var currentCharacters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            return new JObject { ["ids"] = new JArray(capitalOutOfScene.Select(actor => actor.UniqueId)),
                ["shape"] = CapitalOutOfSceneShape(currentParty, currentCharacters),
                ["actors"] = new JArray(capitalOutOfScene.Select(actor => new JObject { ["id"] = actor.UniqueId,
                    ["inGame"] = actor.IsInGame, ["inState"] = actor.IsInState, ["directlyControllable"] = actor.IsDirectlyControllable,
                    ["viewPresent"] = actor.View != null, ["liveRigValidated"] = false })) };
        }

        // Row 59 bind in the capital: the in-scene roster plus the companion pair resolved uniquely by its
        // selected ids from native state. That pair must be exactly out of the scene and uncontrollable.
        private UnitEntityData[] BindCapitalOutOfScene(UnitEntityData[] inScene)
        {
            Require(capitalOutOfScene == null && game.CurrentlyLoadedArea.IsCapital, "Capital out-of-scene custody may be bound once in a capital.");
            UnitEntityData Resolve(string id)
            {
                var matches = game.State.Units.Concat(game.Player.AllCharacters).Where(actor => actor != null && actor.UniqueId == id).Distinct().ToArray();
                Require(matches.Length == 1, "Capital out-of-scene actor does not resolve uniquely: " + id);
                return matches[0];
            }
            var rider = Resolve(selection.RiderBId); var mount = Resolve(selection.MountBId);
            Require(!rider.IsInGame && !mount.IsInGame && !rider.IsDirectlyControllable && !mount.IsDirectlyControllable &&
                ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider),
                "Capital companion pair is not exactly out of the scene, uncontrollable and reciprocal.");
            capitalOutOfScene = new[] { rider, mount };
            var inField = inScene.Any(actor => ReferenceEquals(actor, rider));
            Require(inField == inScene.Any(actor => ReferenceEquals(actor, mount)), "Capital companion pair is split across the native roster.");
            return inField ? inScene : inScene.Concat(capitalOutOfScene).ToArray();
        }
    }
}
