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
    internal sealed partial class Chunk7OwnedPairFixture
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
        private int InheritedPairCount => selection.SixPairLineage ? 6 : selection.IsFourthHorse ? 3 : 2;
        private UnitEntityData sixRemote;
        private object sixRemoteDescriptor;
        private JObject sixRemoteArrangement;
        private object[] descriptors, views, facts, ranks;
        private UnitEntityData[] partyCharacters;
        private string sourcePath;
        private long sourceLength, sourceTicks;
        private bool bound, released;
        internal UnitEntityData[] Party { get; private set; }
        internal UnitEntityData[] Actors { get; private set; }
        internal Chunk7NativeWorldScope Scope { get; private set; }
        internal bool Bound => bound;
        // Set only by a passing VerifyRestoredOwnerSeedPairs: the restored mounted start was proven.
        internal bool RestoredOwnerSeedStartVerified { get; private set; }
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
        private void RequireBindMode(bool combatCold)
        {
            Require(!bound && !released && combatCold == selection.RequiresCombatColdBind(request.PersistenceCase),
                "Inherited fixture is already bound or its cold mode differs.");
        }
        internal void Bind(bool combatCold)
        {
            RequireBindMode(combatCold);
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
            var party = Chunk7NativeFieldRoster.Capture(game.Player);
            // Row 59 loads in the capital: add back the companion pair native capital mode keeps out of the scene.
            if (selection.IsCapitalArrivalCold(request.PersistenceCase) && game.CurrentlyLoadedArea.IsCapital) party = BindCapitalOutOfScene(party);
            var rows = sourceWorld["party"] as JArray;
            Require(rows != null && rows.Count == selection.PartyIds.Length && party.Length == rows.Count &&
                party.All(actor => actor != null) && party.Select(actor => actor.UniqueId).Distinct(StringComparer.Ordinal).Count() == party.Length &&
                party.Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(selection.PartyIds.OrderBy(id => id, StringComparer.Ordinal)) &&
                party.All(actor => rows.Count(row => (string)row["id"] == actor.UniqueId && (string)row["blueprint"] == actor.Blueprint?.AssetGuid) == 1),
                "Native field roster differs from the complete qualified cumulative Manual roster.");
            Require(party.All(actor => IsCapitalOutOfScene(actor) || actor.IsInState && actor.IsInGame && actor.View?.AgentASP != null && actor.Commands != null &&
                (combatCold || !actor.IsInCombat && actor.Commands.Empty)), "An inherited field actor is not ready in its exact native world.");
            Require(game.Player.MainCharacter.Value.UniqueId == (string)sourceWorld["mainActorId"], "Inherited fixture main actor changed.");
            Party = party; Actors = (selection.IsFourthHorse
                ? selection.PairIds.Concat(new[] { Chunk7OwnedPairsSelection.FourthHorsePriorRider,
                    Chunk7OwnedPairsSelection.FourthHorsePriorMount }) : selection.PairIds)
                .Select(id => party.Single(actor => actor.UniqueId == id)).ToArray();
            partyCharacters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            Require(partyCharacters.All(actor => actor != null && partyCharacters.Count(value => ReferenceEquals(value, actor)) == 1),
                "Inherited native character roster is null or duplicated.");
            // Row 59: fix the capital's admitted native remote companions by identity before any custody check.
            if (capitalOutOfScene != null) FixCapitalNativeRemote();
            descriptors = Actors.Select(actor => (object)actor.Descriptor).ToArray(); views = Actors.Select(actor => (object)actor.View).ToArray();
            facts = new object[InheritedPairCount]; ranks = new object[InheritedPairCount];
            for (var index = 0; index < InheritedPairCount; index++)
            {
                var rider = Actors[index * 2]; var mount = Actors[index * 2 + 1]; var row = ownerRows[index];
                Require((selection.IsCapitalArrivalCold(request.PersistenceCase) && game.CurrentlyLoadedArea.IsCapital ?
                    rider.IsDirectlyControllable == ReferenceEquals(rider, game.Player.MainCharacter.Value) : rider.IsDirectlyControllable) &&
                    !rider.Descriptor.IsPet && mount.Blueprint == horses.HorseUnit &&
                    ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider) &&
                    (IsCapitalOutOfScene(rider) || game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, rider)) == 1 &&
                    !game.Player.RemoteCompanions.Any(value => ReferenceEquals(value.Value, rider))), "An inherited Horse is not the exact reciprocal field companion.");
                var fact = rider.Descriptor.GetFact(horses.HorseFeature) as Feature; var rank = rider.Descriptor.GetFact(horses.LevelRank);
                Require(fact != null && rank != null && fact.Source != null && fact.Source.AssetGuid == (string)row["sourceGuid"] &&
                    horses.HorseFeature.AssetGuid == (string)row["featureGuid"] && rank.GetRank() == (int)row["rank"],
                    "Inherited Horse feature/source/rank differs from the completed native acquisition.");
                var profile = SupportedMountedProfiles.Resolve(rider, mount);
                // An out-of-scene rider has no live rig to validate; its profile still resolves and the
                // skip is recorded in the out-of-scene custody record, never silent.
                Require(profile != null && (IsCapitalOutOfScene(rider) || MountedRiderPoseAdapter.TryValidateSupportedSurface(rider.View, profile.RiderPoseProfile, out var poseError)),
                    "Inherited rider body has no currently declared production profile or supported live rig.");
                facts[index] = fact; ranks[index] = rank;
            }
            Scope = new Chunk7NativeWorldScope(request, party, input.Area);
            // Owner-seed-derived combat starts from the restored MOUNTED graph; its scenario
            // verifies the two restored pairs (VerifyRestoredOwnerSeedPairs) before any input.
            if (!selection.Cold && !selection.IsOwnerSeedDerivedCombat && !selection.IsOwnerSeedCheckpointSave)
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
            admittedPlayer = game.Player; admittedState = game.State;
            admittedAreaState = game.State.LoadedAreaState;
            admittedMainState = game.State.LoadedAreaState.MainState;
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
            if (selection.IsFourthHorse) { ReadFourthHorseSource(artifact); return; }
            if (selection.SixPairLineage) { ReadSixPairSource(artifact); return; }
            if (selection.PaidFixtureLineage) { ReadPaidFixtureSource(artifact); return; }
            if (Chunk7OwnedPairsSelection.IsManticoreManualCold(request.PersistenceCase) || selection.IsManticoreReturnArea)
            { ReadManticoreManualSource(artifact); return; }
            if (selection.OwnerSeedDerivedLineage) { ReadOwnerSeedDerivedSource(artifact); return; }
            // Row 59 loads in the capital; its lineage Manual was written in the pinned non-capital SourceArea.
            var originalArea = selection.AcquiredLineageArea(request.PersistenceCase, request.PersistenceLoad.Area);
            Require((string)artifact["scenario"] == Chunk7AcquiredManualSelection.Scenario && (string)artifact["evidenceKind"] == Chunk7AcquiredManualScenario.EvidenceKind &&
                (string)artifact["runId"] == selection.SourceRunId && (string)artifact["status"] == "PASS" && artifact["errors"] is JArray errors && errors.Count == 0 &&
                (string)artifact["commit"] == request.Commit && (string)artifact["dllSha256"] == request.DllSha256 && (string)artifact["dllMvid"] == request.DllMvid &&
                observations != null && (string)observations["selection"]?["case"] == Chunk7OwnedPairsSelection.SourceCase &&
                observations["selection"]?["baseGameOnly"]?.Type == JTokenType.Boolean && (bool)observations["selection"]["baseGameOnly"] &&
                observations["cleanup"]?["custodySettled"]?.Type == JTokenType.Boolean && (bool)observations["cleanup"]["custodySettled"] &&
                (string)observations["nativeWrite"]?["sha256"] == selection.SourceArchiveSha256 &&
                (string)observations["nativeWrite"]?["gameId"] == request.PersistenceLoad.GameId &&
                (string)observations["nativeWrite"]?["gameName"] == request.PersistenceLoad.GameName &&
                (string)observations["nativeWrite"]?["area"] == originalArea,
                "Staged source lacks its complete successful same-candidate two-owner Manual identity.");
            Require(selection.Cold || request.PersistenceLoad.Sha256 == selection.SourceArchiveSha256,
                "Source intake did not load its exact qualified inherited Manual bytes.");
            sourceWorld = observations["fieldWorld"] as JObject;
            var owners = observations["twoOwners"] as JArray;
            Require(sourceWorld != null && owners != null && owners.Count == 2 && owners.All(row => row is JObject) &&
                (string)sourceWorld["gameId"] == request.PersistenceLoad.GameId && (string)sourceWorld["area"] == originalArea &&
                (string)owners[0]["actorId"] == selection.RiderAId && (string)owners[0]["petId"] == selection.MountAId &&
                (string)owners[1]["actorId"] == selection.RiderBId && (string)owners[1]["petId"] == selection.MountBId,
                "The completed two-owner source role order differs from the explicit intake.");
            ownerRows = owners.Cast<JObject>().ToArray();
            sourceArrangement = observations["beforeSaveArrangement"] as JObject;
            Require(sourceArrangement != null && JToken.DeepEquals(sourceArrangement, observations["afterSaveArrangement"]),
                "Source Manual lacks unchanged native class/XP/funds evidence.");
        }
        private void ReadSixPairSource(JObject artifact)
        {
            Require((string)artifact["runId"] == Chunk7OwnedPairsSelection.SixPairSourceRun && (string)artifact["status"] == "PASS" &&
                (string)artifact["scenario"] == Chunk7AssistedColdInventorySelection.Scenario &&
                (string)artifact["evidenceKind"] == "chunk7-assisted-cold-inventory-evidence" &&
                artifact["errors"] is JArray errors && errors.Count == 0,
                "Six-pair fixture requires its exact accepted read-only cold observation.");
            observations = artifact["observations"] as JObject;
            sourceWorld = observations?["settledWorld"] as JObject;
            var rows = observations?["sixthHorseFacts"] as JArray;
            Require(sourceWorld?["party"] is JArray party && party.Count == 12 && rows?.Count == 6 &&
                JToken.DeepEquals(sourceWorld["party"], observations["loadedWorld"]?["party"]) &&
                (string)observations["source"]?["runId"] == "20261022-c7b-world184-b184s1-sixthhorse-manual" &&
                (string)observations["source"]?["archiveSha256"] == selection.SourceArchiveSha256 &&
                (int?)observations["archive"]?["pairs"] == 0 && (int?)observations["archive"]?["participations"] == 0 &&
                (int?)observations["archive"]?["slots"] == 0 && (bool?)observations["archive"]?["combatPresent"] == false,
                "Six-pair checkpoint lost its twelve native actors, six sourced Horses or empty mounted metadata.");
            // The stress and the Manual producer load Manual308 itself; the cold consumer
            // loads the producer's distinct mounted Manual, pinned separately by the runner.
            Require(selection.Cold ? request.PersistenceLoad.Sha256 != selection.SourceArchiveSha256 :
                    request.PersistenceLoad.Sha256 == selection.SourceArchiveSha256,
                "Six-pair source intake did not load its exact declared native Manual.");
            ownerRows = rows.Cast<JObject>().ToArray(); var roles = selection.PairIds;
            var riders = Enumerable.Range(0, 6).Select(i => roles[i * 2]).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var sourceCharacters = sourceWorld["partyCharacterIds"] as JArray;
            var liveCharacters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            Require(sourceCharacters?.Count == 6 && sourceCharacters.Values<string>().OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(riders) &&
                JToken.DeepEquals(sourceCharacters, observations["loadedWorld"]?["partyCharacterIds"]) && liveCharacters.Length == 6 &&
                liveCharacters.All(actor => actor != null) && liveCharacters.Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(riders),
                "Six-pair native character roster differs from the exact six cold-qualified riders.");
            for (var i = 0; i < 6; i++)
                Require((string)ownerRows[i]["riderId"] == roles[i * 2] && (string)ownerRows[i]["mountId"] == roles[i * 2 + 1] &&
                    (bool?)ownerRows[i]["reciprocal"] == true && (int?)ownerRows[i]["rank"] == 1,
                    "Six-pair source role, reciprocal ownership or rank changed.");
            // Exact checkpoint progression; no XP, level, resource or roster setup.
            sourceArrangement = new JObject { ["money"] = sourceWorld["nativeMoney"].DeepClone(), ["experienceRatePercent"] = 100,
                ["actors"] = new JArray(((JArray)sourceWorld["party"]).OrderBy(row => (string)row["id"], StringComparer.Ordinal).Select(row => new JObject {
                    ["actorId"] = row["id"].DeepClone(), ["blueprint"] = row["blueprint"].DeepClone(),
                    ["experience"] = row["experience"].DeepClone(), ["characterLevel"] = row["characterLevel"].DeepClone(),
                    ["classes"] = new JArray(((JArray)row["classes"]).OrderBy(value => (string)value["guid"], StringComparer.Ordinal).Select(value =>
                        new JObject { ["classGuid"] = value["guid"].DeepClone(), ["level"] = value["level"].DeepClone() })) })) };
            var remote = game.Player.RemoteCompanions.Select(value => value.Value).ToArray();
            Require(remote.Length == 1 && remote[0]?.UniqueId == "8265f8cf-0969-4ec7-a266-2448de91c8c4",
                "Six-pair fixture requires exactly the original remote Linzi.");
            sixRemote = remote[0]; sixRemoteDescriptor = sixRemote.Descriptor;
            sixRemoteArrangement = ArrangementOf(new[] { sixRemote }); RequireSixPairRemote();
        }
        private void RequireSixPairRemote()
        {
            if (!selection.SixPairLineage) return;
            Require(sixRemote != null && game.Player.RemoteCompanions.Count == 1 &&
                ReferenceEquals(game.Player.RemoteCompanions[0].Value, sixRemote) && ReferenceEquals(sixRemote.Descriptor, sixRemoteDescriptor) &&
                sixRemote.Blueprint.AssetGuid == "77c11edb92ce0fd408ad96b40fd27121" && sixRemote.Descriptor.Pet == null &&
                sixRemote.Descriptor.Master.Value == null && sixRemote.Descriptor.Progression.CharacterLevel == 4 &&
                sixRemote.Descriptor.Progression.Experience == 9000 && sixRemote.Descriptor.Progression.Classes.Count == 1 &&
                sixRemote.Descriptor.Progression.Classes.Single().CharacterClass.AssetGuid == "772c83a25e2268e448e841dcd548235f" &&
                sixRemote.Descriptor.Progression.Classes.Single().Level == 4 &&
                JToken.DeepEquals(sixRemoteArrangement, ArrangementOf(new[] { sixRemote })),
                "Six-pair actions changed the exact remote Linzi object, progression or pet links.");
        }
        internal JObject CaptureSixPairRemote()
        {
            Require(selection.SixPairLineage, "Remote Linzi observation belongs only to the six-pair fixture.");
            RequireSixPairRemote();
            return new JObject { ["actorId"] = sixRemote.UniqueId, ["object"] = identities.Get(sixRemote),
                ["descriptor"] = identities.Get(sixRemote.Descriptor), ["petId"] = JValue.CreateNull(), ["masterId"] = JValue.CreateNull(),
                ["arrangement"] = ArrangementOf(new[] { sixRemote }) };
        }
        private void ReadFourthHorseSource(JObject artifact)
        {
            var input = request.PersistenceLoad;
            var setup = observations?["thirdHorseSetup"] as JObject;
            var manual = observations?["thirdHorseManual"] as JObject;
            var write = observations?["nativeWrite"] as JObject;
            var inheritedSource = observations?["inheritedFixture"] as JObject;
            var priorPairs = inheritedSource?["pairs"] as JArray;
            var priorHorse = setup?["nativeHorse"] as JObject;
            var saved = manual?["boundary"] as JObject;
            var savedActors = saved?["actors"] as JArray;
            var field = observations?["cleanup"]?["partyAfter"] as JArray;
            Require((string)artifact["scenario"] == Chunk7OwnedPairsSelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7WorldScenarioEngine.EvidenceKind &&
                (string)artifact["runId"] == Chunk7OwnedPairsSelection.FourthHorseSourceRun &&
                (string)artifact["status"] == "PASS" && artifact["errors"] is JArray failures && failures.Count == 0 &&
                (string)setup?["contract"] == "small-source-third-native-horse-setup-v1" &&
                (string)write?["sha256"] == input.Sha256 && (string)write?["fileName"] == input.FileName &&
                (string)write?["gameId"] == input.GameId && (string)write?["gameName"] == input.GameName &&
                (string)write?["area"] == input.Area && (int?)write?["archive"]?["SchemaVersion"] == 3 &&
                (string)manual?["contract"] == "small-third-horse-manual-producer-v1" &&
                manual?["setupCompletedBeforeSave"]?.Value<bool>() == true &&
                setup?["fixtureSetupOnly"]?.Value<bool>() == true &&
                setup?["sixPairQualified"]?.Value<bool>() == false &&
                (string)setup?["targetRiderId"] == Chunk7OwnedPairsSelection.FourthHorsePriorRider &&
                setup?["nativeCommitCount"]?.Value<int>() == 4 &&
                observations?["cleanup"]?["cleanupCustodySettled"]?.Value<bool>() == true &&
                field != null && field.Count == 8 && savedActors != null && savedActors.Count == 8 &&
                priorPairs != null && priorPairs.Count == 2 && priorHorse != null,
                "Three-Horse source lacks its exact accepted producer, saved boundary or cleanup custody.");
            var expected = selection.PartyIds.OrderBy(id => id, StringComparer.Ordinal);
            Require(field.All(row => row["id"] != null && row["blueprint"] != null) &&
                field.Select(row => (string)row["id"]).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(expected) &&
                savedActors.Select(row => (string)row["actorId"]).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(expected) &&
                (string)priorHorse["mountId"] == Chunk7OwnedPairsSelection.FourthHorsePriorMount &&
                (string)priorHorse["mountMasterId"] == Chunk7OwnedPairsSelection.FourthHorsePriorRider &&
                (string)priorHorse["riderPetId"] == Chunk7OwnedPairsSelection.FourthHorsePriorMount &&
                (string)priorHorse["blueprint"] == HorseCompanionBlueprintService.UnitGuid &&
                (string)priorHorse["featureGuid"] == HorseCompanionBlueprintService.FeatureGuid &&
                priorHorse["rank"]?.Value<int>() > 0,
                "Three-Horse source changed its eight actors or third native reciprocal Horse.");
            ownerRows = new JObject[3];
            for (var index = 0; index < 2; index++)
            {
                var row = priorPairs[index];
                Require((string)row?["riderId"] == selection.PairIds[index * 2] &&
                    (string)row?["mountId"] == selection.PairIds[index * 2 + 1] &&
                    (string)row?["sourceGuid"] == (string)priorHorse["sourceGuid"] &&
                    row?["rank"]?.Value<int>() > 0,
                    "Three-Horse source changed one of its two prior Horse roles.");
                ownerRows[index] = new JObject { ["sourceGuid"] = row["sourceGuid"].DeepClone(),
                    ["featureGuid"] = HorseCompanionBlueprintService.FeatureGuid, ["rank"] = row["rank"].DeepClone() };
            }
            ownerRows[2] = new JObject { ["sourceGuid"] = priorHorse["sourceGuid"].DeepClone(),
                ["featureGuid"] = priorHorse["featureGuid"].DeepClone(), ["rank"] = priorHorse["rank"].DeepClone() };
            sourceWorld = new JObject { ["gameId"] = input.GameId, ["area"] = input.Area,
                ["mainActorId"] = selection.RiderAId,
                ["party"] = new JArray(field.Select(row => new JObject {
                    ["id"] = row["id"].DeepClone(), ["blueprint"] = row["blueprint"].DeepClone() })) };
            sourceArrangement = saved;
            Require(JToken.DeepEquals(saved, manual["arrangementAfterSave"]),
                "Three-Horse Manual changed its native arrangement while saving.");
        }
        private void ReadPaidFixtureSource(JObject artifact)
        {
            var medium = selection.MediumFixtureLineage;
            var producerRun = medium ? Chunk7OwnedPairsSelection.MediumFixtureProducerRun : Chunk7OwnedPairsSelection.SmallFixtureProducerRun;
            var producerArchive = medium ? Chunk7OwnedPairsSelection.MediumFixtureProducerArchive : Chunk7OwnedPairsSelection.SmallFixtureProducerArchive;
            var producerCase = medium ? Chunk7SmallFixtureSelection.MediumCaseName : Chunk7SmallFixtureSelection.CaseName;
            var producerName = medium ? Chunk7SmallFixtureSelection.MediumOutputName : Chunk7SmallFixtureSelection.OutputName;
            var input = request.PersistenceLoad;
            var write = observations?["nativeWrite"] as JObject;
            var field = observations?["fieldWorld"] as JObject;
            var party = field?["party"] as JArray;
            var owners = observations?["acquiredOwners"] as JArray;
            var before = observations?["beforeSaveArrangement"] as JObject;
            var after = observations?["afterSaveArrangement"] as JObject;
            Require((string)artifact["scenario"] == Chunk7SmallFixtureSelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7SmallFixtureScenario.EvidenceKind &&
                (string)artifact["runId"] == producerRun &&
                (string)artifact["status"] == "PASS" && artifact["errors"] is JArray failures && failures.Count == 0 &&
                (string)observations?["selection"]?["case"] == producerCase &&
                (string)write?["sha256"] == producerArchive &&
                (string)write?["internalName"] == producerName &&
                (medium ? Chunk7SmallFixtureSelection.IsMediumManualLeaf((string)write?["fileName"]) :
                    Chunk7SmallFixtureSelection.IsManualLeaf((string)write?["fileName"])) &&
                (string)write?["gameId"] == Chunk7SmallFixtureSelection.CampaignId &&
                (string)write?["area"] == Chunk7SmallFixtureSelection.AreaId &&
                (string)field?["gameId"] == input.GameId && (string)field?["area"] == input.Area &&
                (string)field?["mainActorId"] == Chunk7SmallFixtureSelection.MainActorId &&
                party != null && party.Count == 7 &&
                (int?)field["owners"] == 0 && observations["savedWorld"]?["owners"]?.Value<int>() == 0 &&
                owners != null && owners.Count == 2 && before != null && after != null && JToken.DeepEquals(before, after) &&
                (selection.IsSmallFixtureSave || selection.SmallThirdHorseSetup || selection.IsMediumFixtureSave ? input.Sha256 == selection.SourceArchiveSha256 :
                    input.Sha256 != selection.SourceArchiveSha256),
                "Paid fixture source lacks its exact successful seven-actor zero-pair Manual provenance.");
            var roles = new[] { selection.RiderAId, selection.RiderBId };
            var pets = new[] { selection.MountAId, selection.MountBId };
            ownerRows = new JObject[2];
            for (var index = 0; index < InheritedPairCount; index++)
            {
                var row = owners[index] as JObject;
                Require(row != null && (string)row["riderId"] == roles[index] && (string)row["horseId"] == pets[index] &&
                    (string)row["horseBlueprint"] == HorseCompanionBlueprintService.UnitGuid &&
                    (string)row["featureGuid"] == horses.HorseFeature.AssetGuid &&
                    row["reciprocal"]?.Type == JTokenType.Boolean && (bool)row["reciprocal"] &&
                    row["featureSourceGuid"]?.Type == JTokenType.String &&
                    row["rank"]?.Type == JTokenType.Integer && (int)row["rank"] > 0,
                    "Paid fixture role, sourced Horse or reciprocal pet differs from its producer.");
                ownerRows[index] = new JObject { ["sourceGuid"] = row["featureSourceGuid"].DeepClone(),
                    ["featureGuid"] = row["featureGuid"].DeepClone(), ["rank"] = row["rank"].DeepClone() };
            }
            sourceWorld = field;
            sourceArrangement = ProjectSmallFixtureArrangement(before);
            Require(sourceArrangement != null && party.Select(row => (string)row["id"]).OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(selection.PartyIds.OrderBy(id => id, StringComparer.Ordinal)),
                "Paid fixture full native field roster differs from its closed seven-actor selection.");
        }
        private static JObject ProjectSmallFixtureArrangement(JObject source)
        {
            var rows = source?["actors"] as JArray;
            Require(rows != null && rows.Count == 7 && source["money"]?.Type == JTokenType.Integer &&
                source["experienceRatePercent"]?.Type == JTokenType.Integer &&
                rows.All(item => item is JObject row && row["id"]?.Type == JTokenType.String &&
                    row["blueprint"]?.Type == JTokenType.String && row["experience"]?.Type == JTokenType.Integer &&
                    row["level"]?.Type == JTokenType.Integer && row["classes"] is JArray classes &&
                    classes.All(value => value is JObject cls && cls["guid"]?.Type == JTokenType.String &&
                        cls["level"]?.Type == JTokenType.Integer)),
                "Small fixture arrangement cannot be projected into inherited native progression custody.");
            return new JObject { ["money"] = source["money"].DeepClone(),
                ["experienceRatePercent"] = source["experienceRatePercent"].DeepClone(),
                ["actors"] = new JArray(rows.Children<JObject>().OrderBy(row => (string)row["id"], StringComparer.Ordinal)
                    .Select(row => new JObject { ["actorId"] = row["id"].DeepClone(),
                        ["blueprint"] = row["blueprint"].DeepClone(), ["experience"] = row["experience"].DeepClone(),
                        ["characterLevel"] = row["level"].DeepClone(),
                        ["classes"] = new JArray(((JArray)row["classes"]).Children<JObject>()
                            .OrderBy(cls => (string)cls["guid"], StringComparer.Ordinal)
                            .Select(cls => new JObject { ["classGuid"] = cls["guid"].DeepClone(),
                                ["level"] = cls["level"].DeepClone() })) })) };
        }
        // The owner-seed-derived source is the producer's own successful same-candidate world
        // evidence: its labelled ancestry, the seed pins, the written Manual identity, its
        // runtime boundary and read-back graph. Decoding it grants no native restoration.
        internal JObject OwnerSeedBoundary { get; private set; }
        internal JObject OwnerSeedArchive { get; private set; }
        internal int OwnerSeedProcessId { get; private set; }
        // Combat may run a later candidate than its producer: the producer's own recorded
        // identity is kept (the runner pins both receipts by theirs) and disclosed here.
        internal JObject OwnerSeedCandidates { get; private set; }
        private void ReadManticoreManualSource(JObject artifact)
        {
            var input = request.PersistenceLoad;
            var returnAutoCold = selection.IsManticoreReturnArea && selection.IsOwnerSeedCheckpointLoad;
            var write = observations?["nativeWrite"] as JObject;
            var preparation = observations?["manticoreFixturePreparation"] as JObject;
            var after = preparation?["afterSave"] as JObject;
            var field = after?["field"] as JObject;
            var native = after?["native"] as JObject;
            var inherited = observations?["inheritedFixture"] as JObject;
            var custody = field?["custody"] as JObject;
            var archive = write?["archive"] as JObject;
            var recordedCandidate = System.Text.RegularExpressions.Regex.IsMatch((string)artifact["commit"] ?? "", "^[a-f0-9]{40}$") &&
                System.Text.RegularExpressions.Regex.IsMatch((string)artifact["dllSha256"] ?? "", "^[a-f0-9]{64}$") &&
                Guid.TryParse((string)artifact["dllMvid"], out var producerMvid) && producerMvid != Guid.Empty;
            Require((string)artifact["scenario"] == Chunk7OwnedPairsSelection.Scenario &&
                (string)artifact["evidenceKind"] == "chunk7-persistence-evidence" &&
                (string)artifact["runId"] == selection.SourceRunId && (string)artifact["status"] == "PASS" &&
                artifact["errors"] is JArray errors && errors.Count == 0 && recordedCandidate &&
                (string)observations?["checkpoint"] == Chunk7OwnedPairsSelection.ManticoreFixtureCheckpoint &&
                (string)preparation?["contract"] == "owner-seed-manticore-fixture-v1" &&
                (string)preparation?["completedArchiveSha256"] == selection.SourceArchiveSha256 &&
                (string)write["sha256"] == selection.SourceArchiveSha256 &&
                (string)write["nativeName"] == Chunk7OwnedPairsSelection.ManticoreFixtureManualName &&
                (string)write["nativeType"] == "Manual" &&
                (returnAutoCold ? input.Sha256 != selection.SourceArchiveSha256 &&
                    input.Area == Chunk7OwnerSeedManticoreEntry.SourceArea :
                    Path.GetFileName((string)write["path"]) == input.FileName &&
                    input.Sha256 == selection.SourceArchiveSha256 &&
                    input.Area == Chunk7OwnedPairsSelection.ManticoreFixtureArea) &&
                archive != null && (int?)archive["SchemaVersion"] == MountedArchiveData.Schema &&
                (string)archive["CampaignId"] == input.GameId && (string)archive["AreaId"] == Chunk7OwnedPairsSelection.ManticoreFixtureArea &&
                field != null && native != null && inherited != null && custody != null &&
                (string)field["area"] == Chunk7OwnedPairsSelection.ManticoreFixtureArea &&
                (string)after?["area"] == Chunk7OwnedPairsSelection.ManticoreFixtureArea &&
                (string)custody["sourceCase"] == Chunk7OwnedPairsSelection.OwnerSeedManualCase &&
                (string)observations["ownerSeedCheckpointSource"]?["manualSha256"] != selection.SourceArchiveSha256 &&
                observations["processId"]?.Type == JTokenType.Integer,
                "Staged Manticore Manual lacks its distinct successful producer, saved graph or native area identity.");
            var actors = field["actors"] as JArray;
            var pairRows = custody["pairs"] as JArray;
            var nativeOwners = native["owners"] as JArray;
            var archivePairs = archive["Pairs"] as JArray;
            var archiveSlots = archive["Slots"] as JArray;
            var activeSlots = after["activeSlots"] as JArray;
            var partyIds = custody["partyIds"] as JArray;
            Require(actors != null && actors.Count == selection.PartyIds.Length && partyIds != null && partyIds.Count == actors.Count &&
                selection.PartyIds.All(id => actors.Count(row => (string)row["id"] == id && row["blueprint"] != null) == 1 &&
                    partyIds.Count(row => (string)row == id) == 1) &&
                pairRows != null && pairRows.Count == 2 && nativeOwners != null && nativeOwners.Count == 2 &&
                archivePairs != null && archivePairs.Count == 2 && archiveSlots != null && activeSlots != null &&
                JToken.DeepEquals(archiveSlots, activeSlots) &&
                (long?)native["gameTicks"] == (long?)archive["GameTimeTicks"] &&
                JToken.DeepEquals(custody["arrangement"], inherited["arrangement"]),
                "Manticore saved field, native owner, slot or arrangement custody differs from its producer.");
            sourceWorld = new JObject { ["gameId"] = input.GameId, ["area"] = Chunk7OwnedPairsSelection.ManticoreFixtureArea,
                ["mainActorId"] = selection.RiderAId,
                ["party"] = new JArray(actors.Select(row => new JObject { ["id"] = row["id"], ["blueprint"] = row["blueprint"] })) };
            ownerRows = new JObject[2];
            var boundaryPairs = new JArray();
            for (var index = 0; index < 2; index++)
            {
                var rider = selection.PairIds[index * 2]; var mount = selection.PairIds[index * 2 + 1];
                var row = pairRows[index] as JObject;
                var savedPair = archivePairs.Single(pair => (string)pair["Rider"]?["Id"] == rider && (string)pair["Mount"]?["Id"] == mount);
                var nativeOwner = nativeOwners.Single(owner => (string)owner["riderId"] == rider && (string)owner["mountId"] == mount);
                Require(row != null && (string)row["riderId"] == rider && (string)row["mountId"] == mount &&
                    (string)row["profileId"] == (string)savedPair["ProfileId"] &&
                    (string)nativeOwner["ownerId"] == (string)savedPair["PairId"] &&
                    (long?)nativeOwner["generation"] == (long?)savedPair["Generation"] &&
                    (string)nativeOwner["state"] == "Active", "Manticore producer reminted or mismatched an owned pair.");
                ownerRows[index] = new JObject { ["sourceGuid"] = row["sourceGuid"],
                    ["featureGuid"] = HorseCompanionBlueprintService.FeatureGuid, ["rank"] = row["rank"] };
                boundaryPairs.Add(new JObject { ["ownerId"] = nativeOwner["ownerId"], ["generation"] = nativeOwner["generation"],
                    ["riderId"] = rider, ["mountId"] = mount, ["profileId"] = savedPair["ProfileId"] });
            }
            sourceArrangement = custody["arrangement"] as JObject;
            Require(sourceArrangement != null, "Manticore Manual lacks native progression custody.");
            OwnerSeedBoundary = new JObject { ["gameTicks"] = native["gameTicks"], ["pairs"] = boundaryPairs,
                ["slots"] = new JArray(archiveSlots.Select(slot => new JObject { ["actorId"] = slot["ActorId"],
                    ["index"] = slot["Index"], ["kind"] = slot["Kind"] })) };
            OwnerSeedArchive = archive;
            OwnerSeedProcessId = (int)observations["processId"];
            var sameCandidate = (string)artifact["commit"] == request.Commit &&
                (string)artifact["dllSha256"] == request.DllSha256 &&
                (string)artifact["dllMvid"] == request.DllMvid;
            OwnerSeedCandidates = new JObject { ["contract"] = "owner-seed-derived-candidate-identity-v1",
                ["sameCandidate"] = sameCandidate, ["crossCandidateAllowed"] = true,
                ["producer"] = new JObject { ["runId"] = selection.SourceRunId,
                    ["commit"] = artifact["commit"], ["dllSha256"] = artifact["dllSha256"], ["dllMvid"] = artifact["dllMvid"] },
                ["current"] = new JObject { ["runId"] = request.RunId,
                    ["commit"] = request.Commit, ["dllSha256"] = request.DllSha256, ["dllMvid"] = request.DllMvid },
                ["consumerRunId"] = selection.CheckpointSourceRunId,
                ["consumerArtifactSha256"] = selection.CheckpointSourceArtifactSha256,
                ["manualSchema"] = (int)archive["SchemaVersion"],
                ["currentWriterSchema"] = NativeMountedSaveFormat.SelectedWriterSchema };
        }
        private void ReadOwnerSeedDerivedSource(JObject artifact)
        {
            var input = request.PersistenceLoad; var manual = observations?["ownerSeedManual"] as JObject; var write = observations?["nativeWrite"] as JObject;
            var originalArea = selection.IsOwnerSeedCheckpointLoad && request.PersistenceCase == "multi-area-autosave" ? selection.SourceArea : input.Area;
            var crossCandidate = selection.CrossCandidateAllowedFor(request.PersistenceCase);
            var sameCandidate = (string)artifact["commit"] == request.Commit && (string)artifact["dllSha256"] == request.DllSha256 && (string)artifact["dllMvid"] == request.DllMvid;
            var recordedCandidate = System.Text.RegularExpressions.Regex.IsMatch((string)artifact["commit"] ?? "", "^[a-f0-9]{40}$") &&
                System.Text.RegularExpressions.Regex.IsMatch((string)artifact["dllSha256"] ?? "", "^[a-f0-9]{64}$") && Guid.TryParse((string)artifact["dllMvid"], out var producerMvid) && producerMvid != Guid.Empty;
            Require((string)artifact["scenario"] == Chunk7OwnedPairsSelection.Scenario && (string)artifact["evidenceKind"] == Chunk7WorldScenarioEngine.EvidenceKind &&
                (string)artifact["runId"] == selection.SourceRunId && (string)artifact["status"] == "PASS" && artifact["errors"] is JArray errors && errors.Count == 0 &&
                (crossCandidate ? recordedCandidate : sameCandidate) &&
                // The derived Manual's KMC metadata must be the current writer schema.
                write?["sourceSchema"]?.Type == JTokenType.Integer && (int)write["sourceSchema"] == NativeMountedSaveFormat.SelectedWriterSchema &&
                write["archive"]?["SchemaVersion"]?.Type == JTokenType.Integer && (int)write["archive"]["SchemaVersion"] == MountedArchiveData.Schema &&
                MountedArchiveData.Schema == NativeMountedSaveFormat.SelectedWriterSchema &&
                manual != null && write != null && (string)manual["contract"] == Chunk7WorldScenarioEngine.OwnerSeedManualReceiptContract &&
                (string)manual["ancestry"] == Chunk7WorldScenarioEngine.OwnerSeedDerivedAncestry &&
                manual["disclosedAssistance"]?.Type == JTokenType.Boolean && (bool)manual["disclosedAssistance"] &&
                manual["acquisitionProof"]?.Type == JTokenType.Boolean && !(bool)manual["acquisitionProof"] &&
                manual["levelUpSave"]?.Type == JTokenType.Boolean && !(bool)manual["levelUpSave"] &&
                (string)manual["seedId"] == selection.SeedId && (string)manual["seedSha256"] == selection.SeedSha256 &&
                manual["seedCopyUnchanged"]?.Type == JTokenType.Boolean && (bool)manual["seedCopyUnchanged"] &&
                (string)write["sha256"] == selection.SourceArchiveSha256 && (selection.IsOwnerSeedCheckpointLoad || (string)write["fileName"] == input.FileName) &&
                (string)write["nativeName"] == Chunk7OwnedPairsSelection.OwnerSeedManualName && (string)write["nativeType"] == "Manual" &&
                (string)write["gameId"] == input.GameId && (string)write["gameName"] == input.GameName && (string)write["area"] == originalArea &&
                write["archive"] is JObject && manual["boundary"] is JObject && manual["processId"]?.Type == JTokenType.Integer,
                "Staged source lacks its complete successful same-candidate owner-seed-derived Manual identity.");
            // The checkpoint consumer loads the checkpoint producer's output, not the Manual; its bytes are pinned by the runner.
            Require(selection.IsOwnerSeedCheckpointLoad ? input.Sha256 != selection.SourceArchiveSha256 && input.Sha256 != selection.SeedSha256 : input.Sha256 == selection.SourceArchiveSha256,
                "Source intake did not load its exact owner-seed-derived Manual (or, for the checkpoint consumer, a distinct checkpoint output).");
            sourceWorld = manual["fieldWorld"] as JObject;
            var owners = manual["twoOwners"] as JArray;
            Require(sourceWorld != null && owners != null && owners.Count == 2 && owners.All(row => row is JObject) &&
                (string)sourceWorld["gameId"] == input.GameId && (string)sourceWorld["area"] == originalArea &&
                (string)owners[0]["actorId"] == selection.RiderAId && (string)owners[0]["petId"] == selection.MountAId &&
                (string)owners[1]["actorId"] == selection.RiderBId && (string)owners[1]["petId"] == selection.MountBId,
                "The owner-seed-derived source role order differs from the explicit intake.");
            ownerRows = owners.Cast<JObject>().ToArray();
            sourceArrangement = manual["arrangement"] as JObject;
            Require(sourceArrangement != null && JToken.DeepEquals(sourceArrangement, manual["arrangementAfterSave"]),
                "Owner-seed-derived source lacks unchanged native class/XP/funds evidence.");
            OwnerSeedBoundary = (JObject)manual["boundary"]; OwnerSeedArchive = (JObject)write["archive"]; OwnerSeedProcessId = (int)manual["processId"];
            OwnerSeedCandidates = new JObject { ["contract"] = "owner-seed-derived-candidate-identity-v1", ["sameCandidate"] = sameCandidate,
                ["crossCandidateAllowed"] = crossCandidate,
                ["producer"] = new JObject { ["runId"] = selection.SourceRunId, ["commit"] = artifact["commit"], ["dllSha256"] = artifact["dllSha256"], ["dllMvid"] = artifact["dllMvid"] },
                ["current"] = new JObject { ["runId"] = request.RunId, ["commit"] = request.Commit, ["dllSha256"] = request.DllSha256, ["dllMvid"] = request.DllMvid },
                // Checkpoint* is the accepted consumer (combat, checkpoint producer) or the checkpoint producer (its consumer).
                ["consumerRunId"] = crossCandidate ? selection.CheckpointSourceRunId : null,
                ["consumerArtifactSha256"] = crossCandidate ? selection.CheckpointSourceArtifactSha256 : null,
                ["manualSchema"] = (int)write["archive"]["SchemaVersion"], ["currentWriterSchema"] = NativeMountedSaveFormat.SelectedWriterSchema };
        }
        // The restored two-pair check shared by the cold consumer and the mounted-start combat
        // admission: the loaded graph equals the producer's read-back (through the same JSON text
        // form), and the live registry owners, generations, actors and profiles, and the native
        // control slots equal the producer's runtime at its save boundary.
        internal JObject VerifyRestoredOwnerSeedPairs(MountedArchiveData saved, MountedWorldService liveWorld, SavedMountedSlot[] liveSlots)
        {
            Require(bound && selection.OwnerSeedDerivedInput && OwnerSeedBoundary != null && OwnerSeedArchive != null && saved != null && liveWorld != null && liveSlots != null,
                "Owner-seed-derived restoration check requires its bound producer custody, restored graph and live world.");
            var loaded = JToken.Parse(JObject.FromObject(saved, MountedSaveCodec.CreateSerializer()).ToString(Newtonsoft.Json.Formatting.None));
            Require(JToken.DeepEquals(loaded, OwnerSeedArchive), "Cold-loaded schema-3 graph differs from the producer's read-back of the Manual it wrote.");
            Require(OwnerSeedProcessId > 0 && OwnerSeedProcessId != System.Diagnostics.Process.GetCurrentProcess().Id,
                "Owner-seed-derived restoration requires a process separate from its producer.");
            var rows = OwnerSeedBoundary["pairs"] as JArray; var owners = liveWorld.Registry.Owners;
            Require(rows != null && rows.Count == 2 && saved.Pairs.Length == 2 && saved.Pairs.All(pair => rows.Count(row =>
                    (string)row["ownerId"] == Guid.Parse(pair.PairId).ToString("N") && (long)row["generation"] == pair.Generation &&
                    (string)row["riderId"] == pair.Rider.Id && (string)row["mountId"] == pair.Mount.Id && (string)row["profileId"] == pair.ProfileId) == 1) &&
                owners.Length == 2 && owners.All(owner => rows.Count(row =>
                    (string)row["ownerId"] == owner.Id.ToString("N") && (long)row["generation"] == owner.Generation &&
                    (string)row["riderId"] == owner.Rider?.UniqueId && (string)row["mountId"] == owner.Mount?.UniqueId) == 1),
                "Restored pair owners, generations, actors or profiles differ from the producer runtime at its save boundary.");
            var slotRows = OwnerSeedBoundary["slots"] as JArray;
            Require(slotRows != null && slotRows.Count == liveSlots.Length && liveSlots.All(s => slotRows.Count(row =>
                    (string)row["actorId"] == s.ActorId && (int)row["index"] == s.Index && (int)row["kind"] == s.Kind) == 1),
                "Restored control bindings differ from the producer's bound native hotbar.");
            RestoredOwnerSeedStartVerified = true;
            return new JObject { ["loadedEqualsProducerReadback"] = true, ["producerProcessId"] = OwnerSeedProcessId,
                ["owners"] = new JArray(owners.OrderBy(owner => owner.Id.ToString("N"), StringComparer.Ordinal).Select(owner => new JObject {
                    ["ownerId"] = owner.Id.ToString("N"), ["generation"] = owner.Generation, ["riderId"] = owner.Rider?.UniqueId, ["mountId"] = owner.Mount?.UniqueId })),
                ["slots"] = new JArray(liveSlots.Select(s => new JObject { ["actorId"] = s.ActorId, ["index"] = s.Index, ["kind"] = s.Kind })) };
        }
        internal void RequirePreserved()
        {
            Require(bound && !released, "Inherited fixture custody is not bound."); RequireRequest(); RequireAuthority(); Scope.RequireCurrent();
            RequireSixPairRemote();
            var currentParty = Chunk7NativeFieldRoster.Capture(game.Player);
            var currentCharacters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            if (CapitalOutOfSceneActive) RequireCapitalRoster(currentParty, currentCharacters);
            else Require(currentParty.Length == Party.Length && Party.All(actor => currentParty.Count(value => ReferenceEquals(value, actor)) == 1) &&
                currentCharacters.Length == partyCharacters.Length && partyCharacters.All(actor => currentCharacters.Count(value => ReferenceEquals(value, actor)) == 1) &&
                !game.Player.RemoteCompanions.Any(value => Party.Any(actor => ReferenceEquals(value.Value, actor))),
                "Inherited complete native field roster changed; captured actors cannot be silently removed, replaced or moved remote.");
            for (var index = 0; index < InheritedPairCount; index++)
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
        private JObject Arrangement() => ArrangementOf(Party);
        // The one class/XP/money shape: the owner-seed Manual producer records it with this same builder.
        internal static JObject ArrangementOf(System.Collections.Generic.IEnumerable<Kingmaker.EntitySystem.Entities.UnitEntityData> party) => new JObject {
            ["money"] = Game.Instance.Player.Money, ["experienceRatePercent"] = Game.Instance.Player.ExperienceRatePercent,
            ["actors"] = new JArray(party.OrderBy(actor => actor.UniqueId, StringComparer.Ordinal).Select(actor => new JObject {
                ["actorId"] = actor.UniqueId, ["blueprint"] = actor.Blueprint.AssetGuid,
                ["experience"] = actor.Descriptor.Progression.Experience, ["characterLevel"] = actor.Descriptor.Progression.CharacterLevel,
                ["classes"] = new JArray(actor.Descriptor.Progression.Classes.OrderBy(value => value.CharacterClass.AssetGuid, StringComparer.Ordinal)
                    .Select(value => new JObject { ["classGuid"] = value.CharacterClass.AssetGuid, ["level"] = value.Level })) })) };
        internal JObject Capture(string phase)
        {
            RequirePreserved();
            // Owner-seed-derived custody keeps its own labels; it is never relabelled acquired ancestry.
            var derived = selection.OwnerSeedDerivedLineage;
            var captured = new JObject { ["contract"] = selection.SixPairLineage ? "cold-qualified-six-horse-native-field-v1" :
                    selection.IsFourthHorse ? "small-fixture-three-horse-native-field-v1" :
                    selection.SmallFixtureLineage ? "owner-seed-small-fixture-two-horse-native-field-v1" :
                    selection.MediumFixtureLineage ? "owner-seed-medium-fixture-two-horse-native-field-v1" :
                    derived ? "owner-seed-derived-two-horse-native-field-v1" : "inherited-two-horse-native-field-v1", ["phase"] = phase,
                ["sourceRunId"] = selection.SourceRunId, ["sourceArtifactSha256"] = selection.SourceArtifactSha256,
                ["sourceArchiveSha256"] = selection.SourceArchiveSha256, ["sourceCase"] =
                    (Chunk7OwnedPairsSelection.IsManticoreManualCold(request.PersistenceCase) || selection.IsManticoreReturnArea) ?
                        Chunk7OwnedPairsSelection.ManticoreFixtureCheckpoint :
                    selection.SixPairLineage ? Chunk7AssistedColdInventorySelection.SixthHorseCaseName :
                    selection.IsFourthHorse ? Chunk7OwnedPairsSelection.SmallThirdHorseSaveCase :
                    selection.SmallFixtureLineage ? Chunk7SmallFixtureSelection.CaseName :
                    selection.MediumFixtureLineage ? Chunk7SmallFixtureSelection.MediumCaseName :
                    derived ? Chunk7OwnedPairsSelection.OwnerSeedManualCase : Chunk7OwnedPairsSelection.SourceCase,
                ["nativeLoadVerifiedByHost"] = true, ["decodedReceiptAuthorizesRestoration"] = false,
                ["inheritedPetsPreserved"] = true, ["ownedFactRemovalCount"] = 0,
                ["partyIds"] = new JArray(Party.Select(actor => actor.UniqueId)),
                ["currentPartyIds"] = new JArray(Chunk7NativeFieldRoster.Capture(game.Player).Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal)),
                ["currentCharacterIds"] = new JArray(game.Player.PartyCharacters.Select(value => value.Value.UniqueId).OrderBy(id => id, StringComparer.Ordinal)),
                ["remoteCapturedIds"] = new JArray(game.Player.RemoteCompanions.Where(value => Party.Any(actor => ReferenceEquals(value.Value, actor)))
                    .Select(value => value.Value.UniqueId).OrderBy(id => id, StringComparer.Ordinal)), ["arrangement"] = Arrangement(),
                ["pairs"] = new JArray(Enumerable.Range(0, InheritedPairCount).Select(index => new JObject {
                    ["riderId"] = Actors[index * 2].UniqueId, ["mountId"] = Actors[index * 2 + 1].UniqueId,
                    ["rider"] = identities.Get(Actors[index * 2]), ["mount"] = identities.Get(Actors[index * 2 + 1]),
                    ["feature"] = identities.Get(facts[index]), ["rankFact"] = identities.Get(ranks[index]),
                    ["sourceGuid"] = ((Feature)facts[index]).Source.AssetGuid, ["rank"] = Actors[index * 2].Descriptor.GetFact(horses.LevelRank).GetRank(),
                    ["profileId"] = SupportedMountedProfiles.Resolve(Actors[index * 2], Actors[index * 2 + 1])?.Id })),
                ["identities"] = new JObject { ["capacity"] = identities.Capacity, ["ids"] = new JArray(identities.Ids), ["faults"] = identities.FaultCount } };
            if (CapitalOutOfSceneActive) captured["capitalOutOfScene"] = CaptureCapitalOutOfScene();
            if (selection.SixPairLineage)
            {
                captured["remote"] = CaptureSixPairRemote();
                ((JObject)captured["identities"])["ids"] = new JArray(identities.Ids);
            }
            return captured;
        }
        internal void Release()
        {
            if (released) return;
            Require(replayExpectedLoad == 0 && replayExpectedArea == 0 && !areaAutosavePending && !manticorePending && !capitalPending, "Declared native replay still owns inherited fixture custody.");
            if (bound) RequirePreserved();
            identities.Dispose(); released = true;
        }
        // The third-Horse diagnostic deliberately changes one story actor after
        // Release. Retain only immutable source and original-pair custody; the
        // exact seven-actor arrangement is no longer a valid invariant.
        internal void RequireReleasedThirdHorseSourceCustody()
        {
            Require(bound && released && (selection.SmallThirdHorseSetup || selection.IsFourthHorse), "Horse-setup source custody is not released.");
            RequireRequest(); RequireAuthority(); Scope.RequireCurrent();
            var current = Chunk7NativeFieldRoster.Capture(game.Player);
            Require(Party.All(actor => current.Count(value => ReferenceEquals(value, actor)) == 1) &&
                partyCharacters.All(actor => game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, actor)) == 1),
                "An inherited field actor or character was replaced during third-Horse setup.");
            for (var index = 0; index < InheritedPairCount; index++)
            {
                var rider = Actors[index * 2]; var mount = Actors[index * 2 + 1]; var row = ownerRows[index];
                Require(ReferenceEquals(rider.Descriptor, descriptors[index * 2]) && ReferenceEquals(mount.Descriptor, descriptors[index * 2 + 1]) &&
                    ReferenceEquals(rider.View, views[index * 2]) && ReferenceEquals(mount.View, views[index * 2 + 1]) &&
                    ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider) &&
                    mount.Blueprint == horses.HorseUnit &&
                    ReferenceEquals(rider.Descriptor.GetFact(horses.HorseFeature), facts[index]) &&
                    ReferenceEquals(rider.Descriptor.GetFact(horses.LevelRank), ranks[index]) &&
                    ((Feature)facts[index]).Source?.AssetGuid == (string)row["sourceGuid"] &&
                    rider.Descriptor.GetFact(horses.LevelRank).GetRank() == (int)row["rank"],
                    "An inherited Horse, feature source or rank changed during third-Horse setup.");
            }
            var file = new FileInfo(sourcePath);
            Require(file.Exists && file.Length == sourceLength && file.LastWriteTimeUtc.Ticks == sourceTicks &&
                Hash(sourcePath) == selection.SourceArtifactSha256,
                "Qualified inherited source receipt changed during third-Horse setup.");
        }
        private void RequireAuthority(int? observedLoads = null)
        {
            var count = observedLoads ?? replayLoadOrdinal;
            Require(count >= 1 && count <= selection.LoadCount(request.PersistenceCase) && ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.SaveManager, manager) &&
                authorization.IsActive && authorization.AuthorityGeneration == authorityGeneration && authorization.IsExactPersistenceScope(authority) &&
                NativePersistenceIsolation.IsExactAuthority(authority) && authority.MatchesOwnedPairsPlan(true, false) &&
                authority.AdmittedNativeLoadRequests == count && authorization.AuthorizedLoadCount == count &&
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
