using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // This owns references to inherited ordinary pets, never their deletion. The
    // guarded loader and isolated authority must already have completed one load.
    // Decoding the source receipt cannot restore a pair or grant native resources.
    internal sealed class Chunk7OwnedPairFixture
    {
        private readonly RuntimeRequest request;
        private readonly Chunk7OwnedPairsSelection selection;
        private readonly HorseCompanionBlueprintService horses;
        private readonly MountedWorldService world;
        private readonly MountedPersistenceService persistence;
        private readonly NativeMountedControlService controls;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly PersistenceSaveAuthorization authority;
        private readonly WorkingFixtureLoader loader;
        private readonly Game game;
        private readonly SaveManager manager;
        private readonly int authorityGeneration;
        private readonly string requestKey;
        private readonly RetainedObjectIdentity identities = new RetainedObjectIdentity(128);
        private JObject observations, sourceWorld, sourceArrangement;
        private JObject[] ownerRows;
        private object[] descriptors, views, facts, ranks;
        private UnitEntityData[] partyCharacters;
        private string sourcePath;
        private long sourceLength, sourceTicks;
        private bool bound, released;
        internal UnitEntityData[] Party { get; private set; }
        internal UnitEntityData[] Actors { get; private set; }
        internal Chunk7NativeWorldScope Scope { get; private set; }
        internal bool Bound => bound;
        internal Chunk7OwnedPairFixture(RuntimeRequest request, HorseCompanionBlueprintService horses,
            MountedWorldService world, NativeMountedControlService controls, MountedPersistenceService persistence,
            RuntimeSaveAuthorization authorization, PersistenceSaveAuthorization authority, WorkingFixtureLoader loader)
        {
            if (request?.OwnedPairs == null || request.Validate().Count != 0)
                throw new ArgumentException("An exact closed inherited two-owner request is required.");
            this.request = request; selection = request.OwnedPairs;
            this.horses = horses ?? throw new ArgumentNullException(nameof(horses));
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            game = Game.Instance ?? throw new InvalidOperationException("Exact loaded Game is unavailable."); manager = game.SaveManager;
            authorityGeneration = authorization.AuthorityGeneration; requestKey = Key(request);
        }
        internal void Bind(bool combatCold)
        {
            Require(!bound && !released && combatCold == (selection.Cold && request.PersistenceCase != "multi-exploration-manual" &&
                request.PersistenceCase != "multi-exploration-quick" && request.PersistenceCase != "multi-exploration-auto"),
                "Inherited fixture is already bound or its cold mode differs.");
            RequireRequest();
            RequireAuthority();
            var input = request.PersistenceLoad;
            Require(game?.Player != null && game.SaveManager != null && input != null &&
                !LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any() &&
                !NativePersistenceIsolation.HasPendingWrites && game.Player.GameId == input.GameId &&
                game.Player.MainCharacter.Value?.CharacterName == input.GameName && game.CurrentlyLoadedArea?.AssetGuidThreadSafe == input.Area,
                "The qualified native load has not produced its exact settled world.");
            Require(horses.State == HorseCompanionBlueprintState.Registered && horses.HorseFeature != null && horses.LevelRank != null && horses.HorseUnit != null,
                "Exact registered ordinary Horse contracts are unavailable.");
            ReadSource();
            var party = game.Player.Party.ToArray();
            var rows = sourceWorld["party"] as JArray;
            Require(rows != null && rows.Count == selection.PartyIds.Length && party.Length == rows.Count &&
                party.All(actor => actor != null) && party.Select(actor => actor.UniqueId).Distinct(StringComparer.Ordinal).Count() == party.Length &&
                party.Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(selection.PartyIds.OrderBy(id => id, StringComparer.Ordinal)) &&
                party.All(actor => rows.Count(row => (string)row["id"] == actor.UniqueId && (string)row["blueprint"] == actor.Blueprint?.AssetGuid) == 1),
                "Native field roster differs from the complete qualified cumulative Manual roster.");
            Require(party.All(actor => actor.IsInState && actor.IsInGame && actor.View?.AgentASP != null && actor.Commands != null &&
                (combatCold || !actor.IsInCombat && actor.Commands.Empty)), "An inherited field actor is not ready in its exact native world.");
            Require(game.Player.MainCharacter.Value.UniqueId == (string)sourceWorld["mainActorId"], "Inherited fixture main actor changed.");
            Party = party; Actors = selection.PairIds.Select(id => party.Single(actor => actor.UniqueId == id)).ToArray();
            partyCharacters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            Require(partyCharacters.All(actor => actor != null && partyCharacters.Count(value => ReferenceEquals(value, actor)) == 1),
                "Inherited native character roster is null or duplicated.");
            descriptors = Actors.Select(actor => (object)actor.Descriptor).ToArray(); views = Actors.Select(actor => (object)actor.View).ToArray();
            facts = new object[2]; ranks = new object[2];
            for (var index = 0; index < 2; index++)
            {
                var rider = Actors[index * 2]; var mount = Actors[index * 2 + 1]; var row = ownerRows[index];
                Require(rider.IsDirectlyControllable && !rider.Descriptor.IsPet && mount.Blueprint == horses.HorseUnit &&
                    ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider) &&
                    game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, rider)) == 1 &&
                    !game.Player.RemoteCompanions.Any(value => ReferenceEquals(value.Value, rider)), "An inherited Horse is not the exact reciprocal field companion.");
                var fact = rider.Descriptor.GetFact(horses.HorseFeature) as Feature; var rank = rider.Descriptor.GetFact(horses.LevelRank);
                Require(fact != null && rank != null && fact.Source != null && fact.Source.AssetGuid == (string)row["sourceGuid"] &&
                    horses.HorseFeature.AssetGuid == (string)row["featureGuid"] && rank.GetRank() == (int)row["rank"],
                    "Inherited Horse feature/source/rank differs from the completed native acquisition.");
                var profile = SupportedMountedProfiles.Resolve(rider, mount);
                Require(profile != null && MountedRiderPoseAdapter.TryValidateSupportedSurface(rider.View, profile.RiderPoseProfile, out var poseError),
                    "Inherited rider body has no currently declared production profile or supported live rig.");
                facts[index] = fact; ranks[index] = rank;
            }
            Scope = new Chunk7NativeWorldScope(request, party, input.Area);
            if (!selection.Cold)
            {
                Require(world.Registry.Owners.Length == 0 && !world.UnifiedTurn.HasUnsettledPreparation && !controls.HasUnsettledRelationshipTransition &&
                    world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted && pair.Relationship.PairOwner == null && pair.Relationship.Runtime.NoPreparedPairResidue),
                    "Inherited source already has mounted custody or preparation obligations.");
                var archive = persistence.LoadedArchive;
                Require(archive != null && archive.SchemaVersion == 3 && archive.CampaignId == input.GameId && archive.AreaId == input.Area &&
                    archive.Pairs.Length == 0 && archive.Participations.Length == 0 && archive.Slots.Length == 0 && archive.Combat == null &&
                    persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == 0 &&
                    persistence.SnapshotCount == 0, "Inherited source is not the native-loaded empty mounted graph.");
            }
            bound = true; RequirePreserved();
        }
        private void ReadSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(request.EvidenceRoot)), "runtime-staging",
                "persistence-" + request.RunId, Chunk7OwnedPairsSelection.SourceLeaf);
            var file = new FileInfo(sourcePath);
            Require(file.Exists && file.Length > 0 && file.Length <= 8 * 1024 * 1024 && (file.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == selection.SourceArtifactSha256, "Exact staged two-owner Manual receipt is absent or changed.");
            sourceLength = file.Length; sourceTicks = file.LastWriteTimeUtc.Ticks;
            var artifact = JObject.Parse(File.ReadAllText(sourcePath)); observations = artifact["observations"] as JObject;
            Require((string)artifact["scenario"] == Chunk7AcquiredManualSelection.Scenario && (string)artifact["evidenceKind"] == Chunk7AcquiredManualScenario.EvidenceKind &&
                (string)artifact["runId"] == selection.SourceRunId && (string)artifact["status"] == "PASS" && artifact["errors"] is JArray errors && errors.Count == 0 &&
                (string)artifact["commit"] == request.Commit && (string)artifact["dllSha256"] == request.DllSha256 && (string)artifact["dllMvid"] == request.DllMvid &&
                observations != null && (string)observations["selection"]?["case"] == Chunk7OwnedPairsSelection.SourceCase &&
                observations["selection"]?["baseGameOnly"]?.Type == JTokenType.Boolean && (bool)observations["selection"]["baseGameOnly"] &&
                observations["cleanup"]?["custodySettled"]?.Type == JTokenType.Boolean && (bool)observations["cleanup"]["custodySettled"] &&
                (string)observations["nativeWrite"]?["sha256"] == selection.SourceArchiveSha256 &&
                (string)observations["nativeWrite"]?["gameId"] == request.PersistenceLoad.GameId &&
                (string)observations["nativeWrite"]?["gameName"] == request.PersistenceLoad.GameName &&
                (string)observations["nativeWrite"]?["area"] == request.PersistenceLoad.Area,
                "Staged source lacks its complete successful same-candidate two-owner Manual identity.");
            Require(selection.Cold || request.PersistenceLoad.Sha256 == selection.SourceArchiveSha256,
                "Source intake did not load its exact qualified inherited Manual bytes.");
            sourceWorld = observations["fieldWorld"] as JObject;
            var owners = observations["twoOwners"] as JArray;
            Require(sourceWorld != null && owners != null && owners.Count == 2 && owners.All(row => row is JObject) &&
                (string)sourceWorld["gameId"] == request.PersistenceLoad.GameId && (string)sourceWorld["area"] == request.PersistenceLoad.Area &&
                (string)owners[0]["actorId"] == selection.RiderAId && (string)owners[0]["petId"] == selection.MountAId &&
                (string)owners[1]["actorId"] == selection.RiderBId && (string)owners[1]["petId"] == selection.MountBId,
                "The completed two-owner source role order differs from the explicit intake.");
            ownerRows = owners.Cast<JObject>().ToArray();
            sourceArrangement = observations["beforeSaveArrangement"] as JObject;
            Require(sourceArrangement != null && JToken.DeepEquals(sourceArrangement, observations["afterSaveArrangement"]),
                "Source Manual lacks unchanged native class/XP/funds evidence.");
        }
        internal void RequirePreserved()
        {
            Require(bound && !released, "Inherited fixture custody is not bound."); RequireRequest(); RequireAuthority(); Scope.RequireCurrent();
            var currentParty = game.Player.Party.ToArray();
            var currentCharacters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            Require(currentParty.Length == Party.Length && Party.All(actor => currentParty.Count(value => ReferenceEquals(value, actor)) == 1) &&
                currentCharacters.Length == partyCharacters.Length && partyCharacters.All(actor => currentCharacters.Count(value => ReferenceEquals(value, actor)) == 1) &&
                !game.Player.RemoteCompanions.Any(value => Party.Any(actor => ReferenceEquals(value.Value, actor))),
                "Inherited complete native field roster changed; captured actors cannot be silently removed, replaced or moved remote.");
            for (var index = 0; index < 2; index++)
            {
                var rider = Actors[index * 2]; var mount = Actors[index * 2 + 1]; var row = ownerRows[index];
                Require(ReferenceEquals(rider.Descriptor, descriptors[index * 2]) && ReferenceEquals(mount.Descriptor, descriptors[index * 2 + 1]) &&
                    ReferenceEquals(rider.View, views[index * 2]) && ReferenceEquals(mount.View, views[index * 2 + 1]) &&
                    ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider) && mount.Blueprint == horses.HorseUnit &&
                    ReferenceEquals(rider.Descriptor.GetFact(horses.HorseFeature), facts[index]) && ReferenceEquals(rider.Descriptor.GetFact(horses.LevelRank), ranks[index]) &&
                    ((Feature)facts[index]).Source?.AssetGuid == (string)row["sourceGuid"] && rider.Descriptor.GetFact(horses.LevelRank).GetRank() == (int)row["rank"],
                    "Inherited native actor/pet/fact custody changed; cleanup cannot delete or replace it.");
            }
            Require(JToken.DeepEquals(sourceArrangement, Arrangement()), "Inherited native class/XP/money state changed outside this action campaign.");
            var file = new FileInfo(sourcePath);
            Require(file.Exists && file.Length == sourceLength && file.LastWriteTimeUtc.Ticks == sourceTicks && Hash(sourcePath) == selection.SourceArtifactSha256,
                "Qualified inherited source receipt changed during native use.");
        }
        private JObject Arrangement() => new JObject { ["money"] = Game.Instance.Player.Money, ["experienceRatePercent"] = Game.Instance.Player.ExperienceRatePercent,
            ["actors"] = new JArray(Party.OrderBy(actor => actor.UniqueId, StringComparer.Ordinal).Select(actor => new JObject {
                ["actorId"] = actor.UniqueId, ["blueprint"] = actor.Blueprint.AssetGuid,
                ["experience"] = actor.Descriptor.Progression.Experience, ["characterLevel"] = actor.Descriptor.Progression.CharacterLevel,
                ["classes"] = new JArray(actor.Descriptor.Progression.Classes.OrderBy(value => value.CharacterClass.AssetGuid, StringComparer.Ordinal)
                    .Select(value => new JObject { ["classGuid"] = value.CharacterClass.AssetGuid, ["level"] = value.Level })) })) };
        internal JObject Capture(string phase)
        {
            RequirePreserved();
            return new JObject { ["contract"] = "inherited-two-horse-native-field-v1", ["phase"] = phase,
                ["sourceRunId"] = selection.SourceRunId, ["sourceArtifactSha256"] = selection.SourceArtifactSha256,
                ["sourceArchiveSha256"] = selection.SourceArchiveSha256, ["sourceCase"] = Chunk7OwnedPairsSelection.SourceCase,
                ["nativeLoadVerifiedByHost"] = true, ["decodedReceiptAuthorizesRestoration"] = false,
                ["inheritedPetsPreserved"] = true, ["ownedFactRemovalCount"] = 0,
                ["partyIds"] = new JArray(Party.Select(actor => actor.UniqueId)),
                ["currentPartyIds"] = new JArray(game.Player.Party.Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal)),
                ["currentCharacterIds"] = new JArray(game.Player.PartyCharacters.Select(value => value.Value.UniqueId).OrderBy(id => id, StringComparer.Ordinal)),
                ["remoteCapturedIds"] = new JArray(game.Player.RemoteCompanions.Where(value => Party.Any(actor => ReferenceEquals(value.Value, actor)))
                    .Select(value => value.Value.UniqueId).OrderBy(id => id, StringComparer.Ordinal)), ["arrangement"] = Arrangement(),
                ["pairs"] = new JArray(Enumerable.Range(0, 2).Select(index => new JObject {
                    ["riderId"] = Actors[index * 2].UniqueId, ["mountId"] = Actors[index * 2 + 1].UniqueId,
                    ["rider"] = identities.Get(Actors[index * 2]), ["mount"] = identities.Get(Actors[index * 2 + 1]),
                    ["feature"] = identities.Get(facts[index]), ["rankFact"] = identities.Get(ranks[index]),
                    ["sourceGuid"] = ((Feature)facts[index]).Source.AssetGuid, ["rank"] = Actors[index * 2].Descriptor.GetFact(horses.LevelRank).GetRank(),
                    ["profileId"] = SupportedMountedProfiles.Resolve(Actors[index * 2], Actors[index * 2 + 1])?.Id })),
                ["identities"] = new JObject { ["capacity"] = identities.Capacity, ["ids"] = new JArray(identities.Ids), ["faults"] = identities.FaultCount } };
        }
        internal void Release()
        {
            if (released) return;
            if (bound) RequirePreserved();
            identities.Dispose(); released = true;
        }
        private void RequireAuthority()
        {
            Require(ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.SaveManager, manager) &&
                authorization.IsActive && authorization.AuthorityGeneration == authorityGeneration && authorization.IsExactPersistenceScope(authority) &&
                NativePersistenceIsolation.IsExactAuthority(authority) && authority.MatchesOwnedPairsPlan(true, false) &&
                authority.AdmittedNativeLoadRequests == 1 && authorization.AuthorizedLoadCount == 1 &&
                authority.AdmittedNativeWriteRequests <= selection.WriteCount(request.PersistenceCase) &&
                authorization.AuthorizedWriteCount <= selection.WriteCount(request.PersistenceCase) &&
                manager.SavePath == authority.Root && NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(authority.Root), "Areas") &&
                loader.State == WorkingFixtureLoadState.LoadedAndVerified && loader.WorkingPath == Path.Combine(authority.Root, request.PersistenceLoad.FileName) &&
                loader.Descriptor != null && loader.Descriptor.FileName == request.PersistenceLoad.FileName &&
                loader.Descriptor.GameId == request.PersistenceLoad.GameId && loader.Descriptor.GameName == request.PersistenceLoad.GameName &&
                loader.Descriptor.Area?.AssetGuidThreadSafe == request.PersistenceLoad.Area,
                "Inherited native actor admission lost its exact completed loader, authority generation, world or output plan.");
        }
        private void RequireRequest() => Require(Key(request) == requestKey && request.Validate().Count == 0, "Inherited intake request changed after acquisition.");
        internal static string Key(RuntimeRequest request) => JObject.FromObject(request).ToString(Newtonsoft.Json.Formatting.None);
        private static string Hash(string path) { using (var hash = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    }
}
