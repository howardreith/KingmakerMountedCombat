using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7ProgressionScenario
    {
        private readonly bool ownedProgression;
        private readonly string ownedSelectionKey;
        private JObject ownedSourceWorld, ownedSourceInventory;
        private JObject smallColdFixture;
        private JArray ownedSourceOwners;
        private UnitEntityData[] ownedParty, ownedCharacters, ownedActors;
        private MountedPairOwner<UnitEntityData, GameMountedRelationshipService> ownedControl;
        private object ownedControlFeature, ownedControlRank, ownedControlFeatureSource;
        private int ownedControlRankValue;
        private bool ownedWorldCleanupStarted;

        private void ReadOwnedSources(JObject source, JObject inventory)
        {
            var pins = selection.OwnedSource;
            Require((string)source["runId"] == selection.SourceRunId && (string)source["scenario"] == Chunk7OwnedPairsSelection.Scenario &&
                (string)source["evidenceKind"] == Chunk7PersistenceScenario.EvidenceKind &&
                (string)source["observations"]?["checkpoint"] == "multi-exploration-manual" &&
                (string)source["observations"]?["nativeWrite"]?["sha256"] == selection.SourceArchiveSha256 &&
                (string)inventory["runId"] == selection.InventoryRunId && (string)inventory["scenario"] == Chunk7AcquiredManualSelection.Scenario &&
                (string)inventory["evidenceKind"] == Chunk7AcquiredManualScenario.EvidenceKind &&
                (string)inventory["observations"]?["selection"]?["case"] == Chunk7AcquiredManualSelection.TwoOwnerCaseName &&
                (string)inventory["observations"]?["nativeWrite"]?["sha256"] == pins.SourceArchiveSha256,
                "Owned progression requires the exact successful two-owner mounted checkpoint and acquired Manual lineage.");
            Require(JToken.DeepEquals(source["observations"]["nativeWrite"]["archive"],
                JObject.FromObject(persistence.LoadedArchive, MountedSaveCodec.CreateSerializer())),
                "Restored owned progression graph differs from the pinned native checkpoint.");
            ownedSourceWorld = inventory["observations"]?["fieldWorld"] as JObject;
            ownedSourceOwners = inventory["observations"]?["twoOwners"] as JArray;
            ownedSourceInventory = inventory["observations"]?["nativeProfileInventory"] as JObject;
            Require(ownedSourceWorld != null && ownedSourceOwners?.Count == 2 && ownedSourceInventory?["party"] is JArray,
                "Owned progression is missing its observed full field roster or two inherited owners.");
            var roles = pins.PairIds;
            for (var i = 0; i < 2; i++) Require((string)ownedSourceOwners[i]["actorId"] == roles[i * 2] &&
                (string)ownedSourceOwners[i]["petId"] == roles[i * 2 + 1], "Owned progression replaced source pair role order.");
            var party = (JArray)ownedSourceInventory["party"];
            var actor = party.OfType<JObject>().SingleOrDefault(value => (string)value["id"] == selection.ActorId);
            Require(actor != null, "Owned progression target lacks exact acquired native inventory.");
            evidence["source"] = new JObject { ["sourceRunId"] = selection.SourceRunId, ["sourceArtifactSha256"] = selection.SourceArtifactSha256,
                ["sourceArchiveSha256"] = selection.SourceArchiveSha256, ["inventoryRunId"] = selection.InventoryRunId,
                ["inventoryArtifactSha256"] = selection.InventoryArtifactSha256, ["sameCandidate"] = true,
                ["nativeSourceGraphExact"] = true, ["inventoryActor"] = actor.DeepClone(),
                ["ownedSource"] = Chunk7EvidenceDto.Request(pins), ["inventoryWorld"] = ownedSourceWorld.DeepClone() };
        }
        private void ReadSmallOwnedSources(JObject source, JObject inventory)
        {
            var pins = selection.OwnedSource;
            var written = source["observations"]?["nativeWrite"]?["archive"];
            var cold = inventory["observations"]?["inheritedFixture"] as JObject;
            Require((string)source["runId"] == selection.SourceRunId &&
                (string)source["scenario"] == Chunk7OwnedPairsSelection.Scenario &&
                (string)source["evidenceKind"] == Chunk7PersistenceScenario.EvidenceKind &&
                (string)source["observations"]?["checkpoint"] == "multi-exploration-manual" &&
                (string)source["observations"]?["nativeWrite"]?["sha256"] == selection.SourceArchiveSha256 &&
                (string)inventory["runId"] == selection.InventoryRunId &&
                (string)inventory["scenario"] == Chunk7OwnedPairsSelection.Scenario &&
                (string)inventory["evidenceKind"] == Chunk7PersistenceScenario.EvidenceKind &&
                (string)inventory["observations"]?["checkpoint"] == "multi-exploration-manual" &&
                (bool?)inventory["observations"]?["cold"] == true &&
                JToken.DeepEquals(written, inventory["observations"]?["loadedArchive"]) &&
                JToken.DeepEquals(written, JObject.FromObject(persistence.LoadedArchive, MountedSaveCodec.CreateSerializer())) &&
                (string)cold?["sourceCase"] == "owner-seed-default-halfling-horse-manual" &&
                JToken.DeepEquals(source["observations"]?["inheritedCleanup"]?["partyIds"], cold?["partyIds"]),
                "Small dismissal requires its exact same-candidate mounted producer and independent cold-consumer graph.");
            var arrangement = cold["arrangement"]?["actors"] as JArray;
            ownedSourceOwners = cold["pairs"] as JArray;
            Require(arrangement?.Count == pins.PartyIds.Length && ownedSourceOwners?.Count == 2 &&
                cold["partyIds"] is JArray partyIds && partyIds.Count == pins.PartyIds.Length &&
                pins.PartyIds.All(id => partyIds.Count(value => (string)value == id) == 1),
                "Small dismissal lost its qualified seven-actor field and pair roles.");
            for (var i = 0; i < 2; i++)
                Require((string)ownedSourceOwners[i]["riderId"] == pins.PairIds[i * 2] &&
                    (string)ownedSourceOwners[i]["mountId"] == pins.PairIds[i * 2 + 1],
                    "Small dismissal substituted its qualified Medium/Small pair order.");
            var target = arrangement.Children<JObject>().SingleOrDefault(row => (string)row["actorId"] == selection.ActorId);
            Require(target != null && (string)target["blueprint"] == "baaff53a675a84f4983f1e2113b24552",
                "Small dismissal target is not the paid custom Halfling in the cold field arrangement.");
            smallColdFixture = cold;
            ownedSourceWorld = new JObject { ["mainActorId"] = Chunk7SmallFixtureSelection.MainActorId,
                ["party"] = new JArray(arrangement.Children<JObject>().Select(row => new JObject {
                    ["id"] = (string)row["actorId"], ["blueprint"] = (string)row["blueprint"] })) };
            evidence["source"] = new JObject { ["sourceRunId"] = selection.SourceRunId, ["sourceArtifactSha256"] = selection.SourceArtifactSha256,
                ["sourceArchiveSha256"] = selection.SourceArchiveSha256, ["inventoryRunId"] = selection.InventoryRunId,
                ["inventoryArtifactSha256"] = selection.InventoryArtifactSha256, ["sameCandidate"] = true,
                ["nativeSourceGraphExact"] = true, ["inventoryActor"] = target.DeepClone(),
                ["ownedSource"] = Chunk7EvidenceDto.Request(pins), ["inventoryWorld"] = ownedSourceWorld.DeepClone(),
                ["smallColdFixture"] = cold.DeepClone(), ["smallFixtureSetupOnly"] = true };
        }
        private void BindOwnedRoster()
        {
            var game = Game.Instance; var pins = selection.OwnedSource;
            var party = Chunk7NativeFieldRoster.Capture(game.Player); var characters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            var rows = ownedSourceWorld["party"] as JArray;
            Require(rows != null && rows.Count == pins.PartyIds.Length && party.Length == rows.Count &&
                party.All(value => value != null && value.IsInGame && value.IsInState && value.View?.AgentASP != null && value.Commands?.Empty == true) &&
                party.Select(value => value.UniqueId).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(pins.PartyIds.OrderBy(value => value, StringComparer.Ordinal)) &&
                party.All(value => rows.Count(row => (string)row["id"] == value.UniqueId && (string)row["blueprint"] == value.Blueprint.AssetGuid) == 1) &&
                game.Player.MainCharacter.Value.UniqueId == (string)ownedSourceWorld["mainActorId"],
                "Owned progression full native field roster/main differs from the qualified acquired Manual.");
            Require(characters.Length >= 2 && characters.Length <= 6 && characters.All(value => value != null &&
                party.Count(member => ReferenceEquals(member, value)) == 1 && characters.Count(member => ReferenceEquals(member, value)) == 1) &&
                !game.Player.RemoteCompanions.Any(value => party.Any(member => ReferenceEquals(member, value.Value))),
                "Owned progression has an absent, duplicate or remote field character.");
            Require(horses.HorseFeature != null && horses.HorseUnit != null && horses.LevelRank != null,
                "Owned progression requires native sourced Horse facts.");
            foreach (var pair in new[] { new[] { rider, mount }, new[] { controlRider, controlMount } })
            {
                var actor = pair[0]; var pet = pair[1];
                var prior = ownedSourceOwners.Single(value => (string)value[selection.IsSmallDismiss ? "riderId" : "actorId"] == actor.UniqueId);
                var fact = actor.Descriptor.GetFact(horses.HorseFeature) as Feature; var rank = actor.Descriptor.GetFact(horses.LevelRank);
                var inventory = selection.IsSmallDismiss
                    ? ((JArray)smallColdFixture["arrangement"]["actors"]).Single(value => (string)value["actorId"] == actor.UniqueId)
                    : ((JArray)ownedSourceInventory["party"]).Single(value => (string)value["id"] == actor.UniqueId);
                var expectedSize = selection.IsSmallDismiss && ReferenceEquals(actor, rider) ? Size.Small : Size.Medium;
                Require(characters.Count(value => ReferenceEquals(value, actor)) == 1 && pet.Blueprint == horses.HorseUnit &&
                    SupportedMountedProfiles.Resolve(actor, pet)?.Id == (expectedSize == Size.Small ? "small-default-halfling-horse-v1" : SupportedMountedProfiles.Horse.Id) &&
                    actor.Descriptor.OriginalSize == expectedSize && actor.Descriptor.State.Size == expectedSize &&
                    (selection.IsSmallDismiss ? (string)inventory["blueprint"] == actor.Blueprint.AssetGuid &&
                        (int)inventory["characterLevel"] == actor.Descriptor.Progression.CharacterLevel &&
                        (int)inventory["experience"] == actor.Descriptor.Progression.Experience :
                        (string)inventory["originalSize"] == "Medium" && (string)inventory["effectiveSize"] == "Medium" &&
                        (int)inventory["nativeLevelUpReadiness"]["characterLevel"] == actor.Descriptor.Progression.CharacterLevel &&
                        (int)inventory["nativeLevelUpReadiness"]["experience"] == actor.Descriptor.Progression.Experience) &&
                    ReferenceEquals(actor.Descriptor.Pet, pet) && ReferenceEquals(pet.Descriptor.Master.Value, actor) &&
                    fact?.Source != null && rank != null && (string)prior["sourceGuid"] == fact.Source.AssetGuid &&
                    (selection.IsSmallDismiss || (string)prior["featureGuid"] == horses.HorseFeature.AssetGuid) &&
                    (int)prior["rank"] == rank.GetRank(),
                    "Owned progression requires exact native Horse bodies, real XP/levels and original sourced facts.");
            }
            ownedControl = world.Registry.FindActive(controlRider);
            Require(ownedControl != null && ReferenceEquals(ownedControl.Mount, controlMount), "Owned progression control owner is absent.");
            var controlFact = (Feature)controlRider.Descriptor.GetFact(horses.HorseFeature);
            ownedControlFeature = controlFact; ownedControlFeatureSource = controlFact.Source;
            ownedControlRank = controlRider.Descriptor.GetFact(horses.LevelRank); ownedControlRankValue = controlRider.Descriptor.GetFact(horses.LevelRank).GetRank();
            ownedActors = party; ownedParty = game.Player.Party.ToArray(); ownedCharacters = characters;
            RequireOwnedRoster(false); evidence["ownedRosterBefore"] = CaptureOwnedRoster("loaded");
        }
        private void RequireOwnedRoster(bool final)
        {
            var removed = false; UnitEntityData pet = mount;
            var observed = session != null && session.TryGetObservedTargetRoster(out removed, out pet);
            var expectedActors = ownedActors.AsEnumerable();
            var expectedParty = ownedParty.AsEnumerable(); var expectedCharacters = ownedCharacters.AsEnumerable();
            if (observed)
            {
                expectedActors = expectedActors.Where(value => !ReferenceEquals(value, mount) && (!removed || !ReferenceEquals(value, rider)));
                if (!removed && pet != null) expectedActors = expectedActors.Concat(new[] { pet });
                if (removed)
                {
                    expectedParty = expectedParty.Where(value => !ReferenceEquals(value, rider));
                    expectedCharacters = expectedCharacters.Where(value => !ReferenceEquals(value, rider));
                }
            }
            var currentActors = Chunk7NativeFieldRoster.Capture(Game.Instance.Player);
            var party = Game.Instance.Player.Party.ToArray(); var characters = Game.Instance.Player.PartyCharacters.Select(value => value.Value).ToArray();
            var expected = expectedActors.ToArray(); var expectedRawParty = expectedParty.ToArray(); var expectedChars = expectedCharacters.ToArray();
            // Pets belong to the explicit reciprocal actor scope. Native Party and
            // PartyCharacters retain only their exact character membership.
            Require(currentActors.Length == expected.Length && expected.All(actor => currentActors.Count(value => ReferenceEquals(value, actor)) == 1) &&
                party.Length == expectedRawParty.Length && expectedRawParty.All(actor => party.Count(value => ReferenceEquals(value, actor)) == 1) &&
                characters.Length == expectedChars.Length && expectedChars.All(actor => characters.Count(value => ReferenceEquals(value, actor)) == 1) &&
                !Game.Instance.Player.RemoteCompanions.Any(value => expected.Any(actor => ReferenceEquals(value.Value, actor))),
                "Owned progression field membership changed outside its observed native target lifecycle.");
            if (final) Require(OwnedRemovedPetSettled() && currentActors.All(actor => actor.IsInState && actor.IsInGame && actor.View != null),
                "Owned progression still awaits native field actor settlement.");
            var fact = controlRider.Descriptor.GetFact(horses.HorseFeature) as Feature;
            Require(ReferenceEquals(controlRider.Descriptor.Pet, controlMount) && ReferenceEquals(controlMount.Descriptor.Master.Value, controlRider) &&
                ReferenceEquals(fact, ownedControlFeature) && ReferenceEquals(fact?.Source, ownedControlFeatureSource) &&
                ReferenceEquals(controlRider.Descriptor.GetFact(horses.LevelRank), ownedControlRank) && controlRider.Descriptor.GetFact(horses.LevelRank).GetRank() == ownedControlRankValue,
                "Owned progression changed the unaffected ordinary Horse facts or reciprocal ownership.");
            var active = world.Registry.FindOwner(controlRider);
            Require(ownedWorldCleanupStarted ? active == null || ReferenceEquals(active, ownedControl) : ReferenceEquals(active, ownedControl),
                "Owned progression replaced or prematurely retired its unaffected mounted control owner.");
        }
        private bool OwnedRemovedPetSettled()
        {
            if (!selection.IsRespec || session == null) return true;
            bool removed; UnitEntityData pet;
            if (!session.TryGetObservedTargetRoster(out removed, out pet) || ReferenceEquals(pet, mount)) return true;
            // Native respec has already requested destruction. Poll its own final
            // disposal/removal; never destroy or detach the old entity here.
            return mount.Destroyed && !mount.IsInState && ReferenceEquals(mount.View, null) &&
                !Game.Instance.State.Units.Any(value => value != null && value.UniqueId == mount.UniqueId);
        }
        private void CaptureOwnedRosterFailure(Exception error)
        {
            if (!ownedProgression || !captured || error?.Message !=
                "Owned progression field membership changed outside its observed native target lifecycle." ||
                evidence["firstOwnedRosterFailure"] != null) return;
            // Capture the first mismatch, before later native cleanup can change the
            // roster again. Each projection is read-only and independently guarded;
            // diagnostic failure must never replace the original failure/custody.
            var detail = new JObject { ["reason"] = error.Message };
            evidence["firstOwnedRosterFailure"] = detail;
            try
            {
                detail["expectedFieldIds"] = new JArray((ownedActors ?? Array.Empty<UnitEntityData>()).Select(value => value?.UniqueId));
                detail["expectedPartyIds"] = new JArray((ownedParty ?? Array.Empty<UnitEntityData>()).Select(value => value?.UniqueId));
                detail["expectedCharacterIds"] = new JArray((ownedCharacters ?? Array.Empty<UnitEntityData>()).Select(value => value?.UniqueId));
            }
            catch (Exception observationError) { detail["expectedRosterCaptureError"] = observationError.ToString(); }
            try
            {
                bool removed = false; UnitEntityData pet = mount;
                detail["targetTransitionObserved"] = session != null && session.TryGetObservedTargetRoster(out removed, out pet);
                detail["targetRemoved"] = removed;
                detail["observedPetId"] = pet?.UniqueId;
            }
            catch (Exception observationError) { detail["transitionObservationError"] = observationError.ToString(); }
            try { detail["session"] = session?.Capture(); }
            catch (Exception observationError) { detail["sessionCaptureError"] = observationError.ToString(); }
            try
            {
                var player = Game.Instance.Player;
                detail["party"] = new JArray(player.Party.Take(7).Select(value => new JObject {
                    ["id"] = value?.UniqueId, ["object"] = value == null ? (JToken)JValue.CreateNull() : RuntimeHelpers.GetHashCode(value),
                    ["petId"] = value?.Descriptor?.Pet?.UniqueId }));
                detail["partyCharacters"] = new JArray(player.PartyCharacters.Take(7).Select(value => value.Value?.UniqueId));
                detail["remoteCompanions"] = new JArray(player.RemoteCompanions.Take(7).Select(value => value.Value?.UniqueId));
                detail["target"] = new JObject { ["riderId"] = rider?.UniqueId, ["riderInState"] = rider != null && rider.IsInState,
                    ["riderInGame"] = rider != null && rider.IsInGame, ["riderDestroyed"] = rider != null && rider.Destroyed,
                    ["originalPetId"] = mount?.UniqueId, ["originalPetInState"] = mount != null && mount.IsInState,
                    ["originalPetInGame"] = mount != null && mount.IsInGame, ["originalPetDestroyed"] = mount != null && mount.Destroyed,
                    ["currentPetId"] = rider?.Descriptor?.Pet?.UniqueId };
            }
            catch (Exception observationError) { detail["rawRosterCaptureError"] = observationError.ToString(); }
            try { detail["fieldIds"] = new JArray(Chunk7NativeFieldRoster.Capture(Game.Instance.Player).Select(value => value.UniqueId)); }
            catch (Exception observationError) { detail["fieldCaptureError"] = observationError.ToString(); }
        }
        private JObject CaptureOwnedRoster(string phase)
        {
            var removed = false; UnitEntityData pet = mount;
            var observed = session != null && session.TryGetObservedTargetRoster(out removed, out pet);
            return new JObject { ["contract"] = "owned-progression-roster-v1", ["phase"] = phase,
                ["nativeTargetTransitionObserved"] = observed, ["targetRemoved"] = removed, ["currentPetId"] = pet?.UniqueId,
                ["originalPartyIds"] = new JArray(ownedActors.Select(value => value.UniqueId)),
                ["originalCharacterIds"] = new JArray(ownedCharacters.Select(value => value.UniqueId)),
                ["party"] = new JArray(Chunk7NativeFieldRoster.Capture(Game.Instance.Player).Select(value => new JObject { ["id"] = value.UniqueId,
                    ["object"] = RuntimeHelpers.GetHashCode(value), ["descriptor"] = RuntimeHelpers.GetHashCode(value.Descriptor), ["view"] = value.View == null ? (JToken)JValue.CreateNull() : RuntimeHelpers.GetHashCode(value.View) })),
                ["characterIds"] = new JArray(Game.Instance.Player.PartyCharacters.Select(value => value.Value.UniqueId)),
                ["remoteCapturedIds"] = new JArray(Game.Instance.Player.RemoteCompanions.Where(value => ownedParty.Any(actor => ReferenceEquals(actor, value.Value))).Select(value => value.Value.UniqueId)),
                ["controlOwnerId"] = ownedControl.Id.ToString("N"), ["controlGeneration"] = ownedControl.Generation,
                ["controlFeature"] = RuntimeHelpers.GetHashCode(ownedControlFeature), ["controlRankFact"] = RuntimeHelpers.GetHashCode(ownedControlRank),
                ["controlSource"] = RuntimeHelpers.GetHashCode(ownedControlFeatureSource), ["controlRank"] = ownedControlRankValue,
                ["resourceEqualityAcrossNativeRestClaimed"] = false,
                ["originalPet"] = new JObject { ["id"] = mount.UniqueId, ["object"] = RuntimeHelpers.GetHashCode(mount),
                    ["descriptor"] = RuntimeHelpers.GetHashCode(mount.Descriptor), ["view"] = ReferenceEquals(mount.View, null) ? (JToken)JValue.CreateNull() : RuntimeHelpers.GetHashCode(mount.View),
                    ["isInState"] = mount.IsInState, ["isInGame"] = mount.IsInGame, ["shouldBeDestroyed"] = mount.ShouldBeDestroyed, ["destroyed"] = mount.Destroyed,
                    ["presentInNativeState"] = Game.Instance.State.Units.Any(value => value != null && ReferenceEquals(value, mount)), ["masterId"] = mount.Descriptor.Master.Value?.UniqueId } };
        }
    }
}
