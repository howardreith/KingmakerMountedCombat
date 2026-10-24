using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Integration;
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
        // Batch197 r58: native capital mode also moves the unpaired story companions to the remote party while
        // they stay in the game. Chunk7CapitalRemoteCompanionRule admits exactly those actors, fixed by identity
        // at adoption (row 58) or bind (row 59) and recorded. Every other captured actor keeps the exact roster contract.
        private UnitEntityData[] capitalNativeRemote;
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

        // Any KMC pair, not only pairs A/B: inherited pair custody, the loaded archive's pair records, a live
        // relationship and a registry owner.
        private bool IsKmcPairMember(UnitEntityData actor) =>
            Actors.Any(value => ReferenceEquals(value, actor)) ||
            (persistence.LoadedArchive?.Pairs ?? new SavedMountedPair[0]).Any(pair => pair == null || pair.Rider?.Id == actor.UniqueId || pair.Mount?.Id == actor.UniqueId) ||
            world.Pairs.Any(pair => ReferenceEquals(pair.Relationship.Rider, actor) || ReferenceEquals(pair.Relationship.Mount, actor)) ||
            world.Registry.FindOwner(actor) != null;

        private Chunk7CapitalRemoteCompanionRule.Member CapitalRosterMember(UnitEntityData actor, UnitEntityData[] currentParty, UnitEntityData[] currentCharacters) =>
            new Chunk7CapitalRemoteCompanionRule.Member { Id = actor.UniqueId,
                InParty = currentParty.Any(value => ReferenceEquals(value, actor)), Character = currentCharacters.Any(value => ReferenceEquals(value, actor)),
                Remote = game.Player.RemoteCompanions.Any(value => ReferenceEquals(value.Value, actor)), InGame = actor.IsInGame, InState = actor.IsInState,
                DirectlyControllable = actor.IsDirectlyControllable, ViewPresent = actor.View != null,
                MainCharacter = ReferenceEquals(actor, game.Player.MainCharacter.Value), PairMember = IsKmcPairMember(actor),
                Pet = actor.Descriptor.Master.Value != null, HasPet = actor.Descriptor.Pet != null };

        // Read-only: the exact native remote companions of the current capital roster, by identity.
        private UnitEntityData[] AdmitCapitalNativeRemote(UnitEntityData[] currentParty, UnitEntityData[] currentCharacters)
        {
            Require(capitalOutOfScene != null && Party != null && Actors != null, "Capital remote companions are judged only under out-of-scene custody.");
            var roster = Party.Where(actor => !IsCapitalOutOfScene(actor)).ToArray();
            var ids = Chunk7CapitalRemoteCompanionRule.Admit(game.CurrentlyLoadedArea != null && game.CurrentlyLoadedArea.IsCapital,
                roster.Select(actor => CapitalRosterMember(actor, currentParty, currentCharacters)).ToArray(),
                capitalOutOfScene.Select(actor => actor.UniqueId), currentParty.Select(actor => actor.UniqueId));
            return ids.Select(id => roster.Single(actor => actor.UniqueId == id)).ToArray();
        }

        private void FixCapitalNativeRemote()
        {
            var currentParty = Chunk7NativeFieldRoster.Capture(game.Player);
            capitalNativeRemote = AdmitCapitalNativeRemote(currentParty, game.Player.PartyCharacters.Select(value => value.Value).ToArray());
        }

        private void RequireCapitalRoster(UnitEntityData[] currentParty, UnitEntityData[] currentCharacters)
        {
            var rider = capitalOutOfScene[0]; var mount = capitalOutOfScene[1];
            Require(capitalOutOfScene.Length == 2 && Actors.Length >= 4 && ReferenceEquals(rider, Actors[2]) && ReferenceEquals(mount, Actors[3]) &&
                ReferenceEquals(Actors[0], game.Player.MainCharacter.Value) && !ReferenceEquals(rider, game.Player.MainCharacter.Value) &&
                !rider.IsInGame && !mount.IsInGame && !rider.IsDirectlyControllable && !mount.IsDirectlyControllable,
                "Capital out-of-scene custody is not exactly the companion pair natively out of the scene.");
            var shape = CapitalOutOfSceneShape(currentParty, currentCharacters);
            // The admitted remote companions are fixed: the same actors, no more and no fewer, on every check.
            var remoteNow = AdmitCapitalNativeRemote(currentParty, currentCharacters);
            Require(capitalNativeRemote != null && remoteNow.Length == capitalNativeRemote.Length &&
                remoteNow.All(actor => capitalNativeRemote.Count(value => ReferenceEquals(value, actor)) == 1),
                "Capital native remote companions changed from the admitted set.");
            bool Admitted(UnitEntityData actor) => capitalNativeRemote.Any(value => ReferenceEquals(value, actor));
            var expectedParty = Party.Where(actor => (shape == "field" || !IsCapitalOutOfScene(actor)) && !Admitted(actor)).ToArray();
            var expectedCharacters = partyCharacters.Where(actor => (shape == "field" || !ReferenceEquals(actor, rider)) && !Admitted(actor)).ToArray();
            var expectedRemote = capitalNativeRemote.Concat(shape == "remote" ? new[] { rider } : new UnitEntityData[0]).ToArray();
            var remoteCaptured = game.Player.RemoteCompanions.Where(value => Party.Any(actor => ReferenceEquals(value.Value, actor))).Select(value => value.Value).ToArray();
            Require(currentParty.Length == expectedParty.Length && expectedParty.All(actor => currentParty.Count(value => ReferenceEquals(value, actor)) == 1) &&
                currentCharacters.Length == expectedCharacters.Length && expectedCharacters.All(actor => currentCharacters.Count(value => ReferenceEquals(value, actor)) == 1) &&
                remoteCaptured.Length == expectedRemote.Length && expectedRemote.All(actor => remoteCaptured.Count(value => ReferenceEquals(value, actor)) == 1),
                "Inherited native field roster changed beyond the capital's out-of-scene companion pair and admitted native remote companions.");
        }

        internal JObject CaptureCapitalOutOfScene()
        {
            Require(capitalOutOfScene != null && capitalNativeRemote != null, "Capital out-of-scene custody has no fixed native remote admission to record.");
            var currentParty = Chunk7NativeFieldRoster.Capture(game.Player);
            var currentCharacters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            return new JObject { ["ids"] = new JArray(capitalOutOfScene.Select(actor => actor.UniqueId)),
                ["shape"] = CapitalOutOfSceneShape(currentParty, currentCharacters),
                ["actors"] = new JArray(capitalOutOfScene.Select(actor => new JObject { ["id"] = actor.UniqueId,
                    ["inGame"] = actor.IsInGame, ["inState"] = actor.IsInState, ["directlyControllable"] = actor.IsDirectlyControllable,
                    ["viewPresent"] = actor.View != null, ["liveRigValidated"] = false })),
                // Each admitted actor by identity with its observed flags and the field checks its admission replaces.
                [Chunk7CapitalRemoteCompanionRule.RecordField] = new JArray(capitalNativeRemote.Select(actor => {
                    var member = CapitalRosterMember(actor, currentParty, currentCharacters);
                    return new JObject { ["id"] = member.Id, ["inParty"] = member.InParty, ["character"] = member.Character,
                        ["remote"] = member.Remote, ["inGame"] = member.InGame, ["inState"] = member.InState,
                        ["directlyControllable"] = member.DirectlyControllable, ["viewPresent"] = member.ViewPresent,
                        ["mainCharacter"] = member.MainCharacter, ["pairMember"] = member.PairMember,
                        ["skippedChecks"] = new JArray(Chunk7CapitalRemoteCompanionRule.SkippedChecks) }; })) };
        }

        // Row 59 bind in the capital: the in-scene roster plus the companion pair resolved uniquely by its
        // selected ids from native state, plus each remaining roster actor found only among the native remote
        // companions. The pair must be exactly out of the scene and uncontrollable; the remote companions are
        // judged by the same exact rule as row 58 once custody is bound. The order is the producer roster's.
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
            var party = inField ? inScene : inScene.Concat(capitalOutOfScene).ToArray();
            var remote = selection.PartyIds.Where(id => party.All(actor => actor.UniqueId != id)).Select(id => {
                var matches = game.Player.RemoteCompanions.Select(value => value.Value).Where(actor => actor != null && actor.UniqueId == id).Distinct().ToArray();
                Require(matches.Length <= 1, "Capital remote companion does not resolve uniquely: " + id);
                return matches.FirstOrDefault(); }).Where(actor => actor != null).ToArray();
            party = party.Concat(remote).ToArray();
            return party.OrderBy(actor => { var index = Array.IndexOf(selection.PartyIds, actor.UniqueId); return index < 0 ? int.MaxValue : index; }).ToArray();
        }
    }
}
