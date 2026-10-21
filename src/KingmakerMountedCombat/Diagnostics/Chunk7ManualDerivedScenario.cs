using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Separate no-grant native acquisition family. The historical read-only bridge is unchanged.
    // One exact native cold load is followed by a Manual checkpoint after legal choices
    // and field settlement. The still-refused cumulative variant retains its Auto contract.
    internal sealed partial class Chunk7ManualDerivedScenario : IDisposable
    {
        internal const string EvidenceFileName = "chunk7-manual-derived-evidence.json";
        internal const string EvidenceKind = "chunk7-manual-derived-evidence";
        internal const string PendingFileName = "chunk7-manual-derived-cleanup-pending.json";
        internal const string PendingKind = "chunk7-manual-derived-cleanup-pending";
        private readonly RuntimeRequest request;
        private readonly Chunk7ManualDerivedSelection selection;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly PersistenceSaveAuthorization authority;
        private readonly MountedWorldService world;
        private readonly MountedPersistenceService persistence;
        private readonly NativeMountedControlService controls;
        private readonly HorseCompanionBlueprintService horses;
        private readonly Game game;
        private readonly SaveManager manager;
        private readonly Player menuPlayer;
        private readonly int generation;
        private readonly string root;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly JObject evidence = new JObject();
        private readonly List<string> errors = new List<string>();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private NativeArchiveRestorationProbe restoration;
        private Chunk7BootstrapInventoryProbe probe;
        private string sourcePath, archivePath, expectedMainId;
        private long sourceLength, sourceTicks, archiveLength, archiveTicks;
        private JObject sourceWorld;
        private UnitEntityData[] actors;
        private object nativePlayer, state, areaState, mainState, area, mainActor;
        private object[] descriptors, views;
        private bool begun, admitted, cleanup, completed, settled, disposed, pending, sourcePinned;
        private double cleanupStart;
        internal bool IsCompleted => completed;
        internal IReadOnlyList<string> Errors => errors;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;
        internal static bool SupportsScenario(string value) => value == Chunk7ManualDerivedSelection.Scenario || value == Chunk7CumulativeAcquisitionSelection.Scenario;

        internal Chunk7ManualDerivedScenario(RuntimeRequest request, RuntimeSaveAuthorization authorization,
            PersistenceSaveAuthorization authority, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, MountedPersistenceService persistence, HorseCompanionBlueprintService horses)
        {
            if (request?.ManualDerived == null || request.Validate().Count != 0)
                throw new ArgumentException("An exact closed Manual-derived acquisition request is required.");
            this.request = request; selection = request.ManualDerived;
            this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            world = relationship?.World ?? throw new ArgumentNullException(nameof(relationship));
            this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.horses = horses ?? throw new ArgumentNullException(nameof(horses));
            game = Game.Instance ?? throw new InvalidOperationException("Pre-load Game is unavailable.");
            manager = game.SaveManager; menuPlayer = game.Player; generation = authorization.AuthorityGeneration;
            root = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(request.EvidenceRoot)), "runtime-staging",
                "persistence-" + request.RunId, "Saved Games");
        }
        internal void BeginBeforeLoad()
        {
            Require(!begun && !cleanup && !disposed, "Cold inventory observation can begin only once.");
            begun = true; RequireAuthority(false);
            Require(authority.AdmittedNativeLoadRequests == 0 && authorization.AuthorizedLoadCount == 0,
                "Cold observation must precede its sole native load admission.");
            Require(game.CurrentlyLoadedArea == null && ReferenceEquals(game.Player, menuPlayer) &&
                menuPlayer?.MainCharacter.Value == null && Game.NewGameUnit == null && !Busy() &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "Cold inventory must own the exact idle empty menu before load.");
            RequireNoOwners(); RequireNoEffects();
            Require(MatchesSelectedPlan(true), "Selected native output was not initially absent/unwritten.");
            if (cumulative == null) ReadSource(); else ReadCumulativeSource();
            probe = new Chunk7BootstrapInventoryProbe(game); probe.Install();
            restoration = new NativeArchiveRestorationProbe(persistence); restoration.Install();
            evidence["processId"] = Process.GetCurrentProcess().Id;
            evidence["selection"] = Chunk7EvidenceDto.Request(cumulative == null ? (object)selection : cumulative);
            evidence["source"] = new JObject { ["runId"] = selection.SourceRunId, ["artifactSha256"] = selection.SourceArtifactSha256,
                ["archiveSha256"] = selection.SourceArchiveSha256, ["archiveLeaf"] = request.PersistenceLoad.FileName,
                ["sameCandidate"] = true, ["diagnosticArchiveContentRead"] = false };
            evidence["sourceNativeWorld"] = sourceWorld.DeepClone();
            evidence["sourceManualObservations"] = sourceObservations.DeepClone();
            CaptureAssistedCumulativeSource();
            evidence["beforeLoad"] = new JObject { ["frame"] = UnityEngine.Time.frameCount, ["game"] = probe.Identity(game),
                ["saveManager"] = probe.Identity(manager), ["menuPlayer"] = probe.Identity(menuPlayer),
                ["loadedAreaPresent"] = false, ["nativeLoading"] = false, ["authority"] = AuthoritySnapshot() };
            evidence["resourceWrites"] = 0; evidence["xpWrites"] = 0; evidence["moneyWrites"] = 0;
            evidence["profileAdmissionChanged"] = false; evidence["ordinaryAcquisitionProven"] = false;
            evidence["sixPairFixtureQualified"] = false; Pass("C7-manual-derived-source");
        }
        internal void Update()
        {
            if (completed || disposed) return;
            try
            {
                if (cleanup) { PollCleanup(); return; }
                Require(begun, "Cold inventory observation was not installed before native load.");
                RequireAuthority(false);
                Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown,
                    "Unexpected native dialog; cold inventory issues no input.");
                Require(clock.Elapsed.TotalSeconds <= 1005, "Cold inventory exceeded its bounded native load/readiness window.");
                if (Busy()) return;
                if (!admitted) Require(clock.Elapsed.TotalSeconds <= 300, "Initial Manual cold load/readiness exceeded its bounded window.");
                if (admitted && buildStarted) { TickBuild(); return; }
                Require(authority.AdmittedNativeLoadRequests == 1 && authorization.AuthorizedLoadCount == 1 &&
                    probe != null && probe.Complete && ReferenceEquals(probe.ObservedPlayer, game.Player),
                    "Exact native cold load and Player.PostLoad did not complete.");
                var saved = request.PersistenceLoad;
                Require(game.Player != null && game.State?.LoadedAreaState?.MainState != null &&
                    game.CurrentlyLoadedArea?.AssetGuidThreadSafe == saved.Area && game.Player.GameId == saved.GameId &&
                    game.Player.MainCharacter.Value?.CharacterName == saved.GameName &&
                    game.Player.MainCharacter.Value.UniqueId == expectedMainId,
                    "Native cold world differs from its engine-minted source identity.");
                if (game.CurrentMode != GameModeType.Default || game.CutsceneLock || game.IsModeActive(GameModeType.Dialog)) return;
                Require(!game.Player.IsInCombat, "Read-only authored inventory does not admit combat.");
                var candidateActors = Chunk7NativeFieldRoster.Capture(game.Player);
                var expected = (JArray)sourceWorld["party"];
                Require(candidateActors.Length == expected.Count && candidateActors.Select(actor => actor.UniqueId).Distinct(StringComparer.Ordinal).Count() == candidateActors.Length,
                    "Cold native party differs from its exact source roster.");
                foreach (var actor in candidateActors)
                {
                    var matches = expected.Where(row => (string)row["id"] == actor.UniqueId).ToArray();
                    Require(matches.Length == 1 && actor.Blueprint?.AssetGuid == (string)matches[0]["blueprint"],
                        "Cold native actor or blueprint was substituted.");
                    if (!actor.IsInState || !actor.IsInGame || actor.View?.AgentASP == null) return;
                    Require(actor.Commands.Empty, "Read-only cold inventory found an unexpected native command.");
                }
                Require(horses.State != HorseCompanionBlueprintState.Faulted, "Horse/Ranger registration faulted: " + horses.Failure);
                if (horses.State != HorseCompanionBlueprintState.Registered) return;
                RequireNoOwners(); RequireNoEffects();
                var data = persistence.LoadedArchive;
                Require(selection.UsesOwnerSeedCold
                    ? data == null && persistence.LoadedArchiveKind == MountedSaveReadKind.Missing && !persistence.LoadedArchiveCanRestore
                    : data != null && data.SchemaVersion == 3 && data.CampaignId == saved.GameId && data.AreaId == saved.Area &&
                      data.Pairs.Length == 0 && data.Participations.Length == 0 && data.Slots.Length == 0 && data.Combat == null,
                    "Cold source differs from its qualified empty or missing mounted metadata.");
                var trace = restoration.Capture();
                Require(restoration.Complete && trace["events"] is JArray entries && entries.Count == 0,
                    "Cold inventory observed native Prepare/round or KMC actor/debt restoration entries.");
                nativePlayer = game.Player; state = game.State; areaState = game.State.LoadedAreaState;
                mainState = game.State.LoadedAreaState.MainState; area = game.CurrentlyLoadedArea; mainActor = game.Player.MainCharacter.Value;
                actors = candidateActors; descriptors = actors.Select(actor => (object)actor.Descriptor).ToArray(); views = actors.Select(actor => (object)actor.View).ToArray();
                admitted = true; RequireWorld(); VerifySourceBytes(); BindCumulativeOwner();
                evidence["loadedWorld"] = Snapshot();
                evidence["nativeProfileInventory"] = Chunk7NativeProfileInventory.Capture(actors);
                Require((string)evidence["nativeProfileInventory"]["captureStatus"] == "PASS", "Native read-only body/readiness inventory failed.");
                evidence["fixtureReadiness"] = CaptureFixtureReadiness();
                evidence["archive"] = selection.UsesOwnerSeedCold
                    ? new JObject { ["kind"] = "Missing", ["loadedArchivePresent"] = false, ["nativeRestorationGrantedByDiagnostic"] = false }
                    : new JObject { ["schemaVersion"] = data.SchemaVersion, ["campaignId"] = data.CampaignId, ["areaId"] = data.AreaId,
                        ["pairs"] = data.Pairs.Length, ["participations"] = data.Participations.Length, ["slots"] = data.Slots.Length,
                        ["combatPresent"] = data.Combat != null, ["nativeRestorationGrantedByDiagnostic"] = false };
                RequireManualReadiness();
                Pass("C7-manual-derived-native-load"); StartBuild();
            }
            catch (Exception error) { Fail("C7-manual-derived-runtime", error.ToString()); BeginCleanup(); }
        }
        private JObject CaptureFixtureReadiness()
        {
            RequireWorld();
            var settings = Kingmaker.UI.SettingsUI.SettingsRoot.Instance;
            Require(settings != null && settings.OnlyOneSave != null && settings.AutosaveEnabled != null && settings.AutosaveSlots != null,
                "Native save settings are unavailable for read-only inventory.");
            var onlyOne = settings.OnlyOneSave; var enabled = settings.AutosaveEnabled; var slots = settings.AutosaveSlots;
            var result = new JObject { ["schemaVersion"] = 1, ["contract"] = "installed-native-fixture-readiness-v1",
                ["captureStatus"] = "IN PROGRESS", ["frame"] = UnityEngine.Time.frameCount,
                ["gameplayAssemblyMvid"] = typeof(Game).Module.ModuleVersionId.ToString("D"), ["observationOnly"] = true };
            evidence["fixtureReadiness"] = result;
            var projection = NativePersistenceIsolation.NativeSlotRotationEnabled;
            var saveSettings = new JObject { ["observationKind"] = "native-effective-getters", ["projectionEnabled"] = projection,
                ["autosaveSlotsNativeType"] = "System.Single", ["autosaveCapacityInferred"] = false,
                ["sliderFallbackCallbackPresent"] = slots.OverrideDefaultValue != null, ["settingsWritten"] = false };
            result["saveSettings"] = saveSettings;
            // This pre-choice observation has not acquired slot projection. The native slider getter
            // may invoke a default-value callback; refuse that path rather than
            // executing an unknown action or reproducing its parsing semantics.
            Require(!projection && slots.OverrideDefaultValue == null,
                "Read-only inventory refuses an active slot projection or native slider fallback callback.");
            saveSettings["onlyOneSave"] = onlyOne.CurrentValue;
            saveSettings["autosaveEnabled"] = enabled.CurrentValue;
            var slotValue = slots.CurrentValue;
            Require(!float.IsNaN(slotValue) && !float.IsInfinity(slotValue), "Native autosave slot setting is non-finite.");
            saveSettings["autosaveSlots"] = slotValue; // Preserve Single; do not round or infer slot capacity.
            result["experienceRatePercent"] = game.Player.ExperienceRatePercent;

            var main = game.Player.MainCharacter.Value; var progression = main.Descriptor.Progression;
            var classes = progression.Classes.Take(21).ToArray();
            Require(classes.Length > 0 && classes.Length <= 20 && classes.All(value => value != null && value.CharacterClass != null &&
                value.Level > 0 && value.Level <= 20) && classes.Select(value => value.CharacterClass.AssetGuid).Distinct(StringComparer.Ordinal).Count() == classes.Length &&
                classes.Sum(value => value.Level) == progression.CharacterLevel, "Native main class progression is missing, duplicate or unbounded.");
            var classRows = new JArray();
            foreach (var value in classes)
            {
                var archetypes = value.Archetypes == null ? new string[0] : value.Archetypes.Take(17).Select(item => item == null ? null : item.AssetGuid).ToArray();
                Require(archetypes.Length <= 16 && archetypes.All(item => item != null) && archetypes.Distinct(StringComparer.Ordinal).Count() == archetypes.Length,
                    "Native main archetype inventory is missing, duplicate or unbounded.");
                classRows.Add(new JObject { ["classGuid"] = value.CharacterClass.AssetGuid, ["level"] = value.Level, ["archetypeGuids"] = new JArray(archetypes) });
            }
            result["mainProgression"] = new JObject { ["actorId"] = main.UniqueId, ["characterLevel"] = progression.CharacterLevel,
                ["experience"] = progression.Experience, ["classes"] = classRows };
            var characters = game.Player.PartyCharacters.Take(7).Select(value => value.Value).ToArray();
            Require(characters.Length > 0 && characters.Length <= 6 && characters.All(value => value != null && actors.Any(actor => ReferenceEquals(actor, value))) &&
                characters.Select(value => value.UniqueId).Distinct(StringComparer.Ordinal).Count() == characters.Length,
                "Native party-character inventory is foreign, duplicate or unbounded.");
            result["staticPartyFacts"] = new JObject { ["selectorOpened"] = false, ["liveSelectorRequiredAuthorityAvailable"] = false,
                ["membershipOrRemovalQualified"] = false, ["characters"] = new JArray(characters.Select(value => new JObject {
                    ["actorId"] = value.UniqueId, ["blueprintGuid"] = value.Blueprint.AssetGuid, ["isMain"] = ReferenceEquals(value, main),
                    ["lockedCompanionComponent"] = value.Blueprint.GetComponent<Kingmaker.Blueprints.LockedCompanionComponent>() != null })) };

            if (cumulative != null || selection.UsesOwnerSeedCold) result["staticPartyFacts"]["observationScope"] = "static-getters-only";

            var saves = manager.Take(33).ToArray();
            Require(saves.Length <= 32 && saves.All(value => value != null), "Isolated native descriptor inventory is null or exceeds its bound.");
            var saveRows = new JArray(saves.OrderBy(value => value.FileName, StringComparer.Ordinal).Select(value => new JObject {
                ["fileName"] = value.FileName, ["internalName"] = value.Name, ["gameId"] = value.GameId, ["gameName"] = value.GameName,
                ["area"] = value.Area == null ? null : value.Area.AssetGuidThreadSafe, ["type"] = value.Type.ToString(),
                ["isAutoLevelupSave"] = value.IsAutoLevelupSave, ["isActuallySaved"] = value.IsActuallySaved,
                ["hasFileOnDisk"] = value.HasFileOnDisk, ["operationState"] = value.OperationState.ToString(),
                ["systemSaveTimeTicks"] = value.SystemSaveTime.Ticks, ["systemSaveTimeKind"] = value.SystemSaveTime.Kind.ToString() }));
            result["isolatedSaveDescriptors"] = new JObject { ["root"] = manager.SavePath, ["metadataOnly"] = true,
                ["maximumObservedDescriptors"] = 32, ["descriptors"] = saveRows };
            Require(saves.Length == 1 && saves[0].FileName == request.PersistenceLoad.FileName && saves[0].Name == request.PersistenceLoad.InternalName &&
                saves[0].GameId == request.PersistenceLoad.GameId && saves[0].GameName == request.PersistenceLoad.GameName &&
                saves[0].Area?.AssetGuidThreadSafe == request.PersistenceLoad.Area && saves[0].Type == SaveInfo.SaveType.Manual &&
                !saves[0].IsAutoLevelupSave && saves[0].HasFileOnDisk && saves[0].OperationState == SaveInfo.StateType.None,
                "Read-only isolated descriptor inventory differs from its sole qualified Manual source.");
            Require(ReferenceEquals(settings, Kingmaker.UI.SettingsUI.SettingsRoot.Instance) && ReferenceEquals(onlyOne, settings.OnlyOneSave) &&
                ReferenceEquals(enabled, settings.AutosaveEnabled) && ReferenceEquals(slots, settings.AutosaveSlots) &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled && slots.OverrideDefaultValue == null,
                "Native settings identity or read-only getter admission changed during observation.");
            RequireWorld(); result["captureStatus"] = "PASS"; return result;
        }
        private void ReadSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7ManualDerivedSelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 && (source.Attributes & FileAttributes.ReparsePoint) == 0 &&
                archive.Exists && archive.Length == request.PersistenceLoad.Length && (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == selection.SourceArtifactSha256 && Hash(archivePath) == selection.SourceArchiveSha256,
                "Exact staged successful Manual artifact/archive bytes differ.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks; archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks; sourcePinned = true;
            var artifact = JObject.Parse(File.ReadAllText(sourcePath)); sourceObservations = artifact["observations"] as JObject;
            if (selection.UsesOwnerSeedCold) { ReadOwnerSeedColdSource(artifact); return; }
            Require((string)artifact["runId"] == selection.SourceRunId && (string)artifact["scenario"] == Chunk7ManualSeedSelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7ManualSeedScenario.EvidenceKind && (string)artifact["status"] == "PASS" &&
                artifact["errors"] is JArray sourceErrors && sourceErrors.Count == 0 &&
                (string)artifact["commit"] == request.Commit && (string)artifact["dllSha256"] == request.DllSha256 &&
                (string)artifact["dllMvid"] == request.DllMvid && sourceObservations != null &&
                sourceObservations["selection"]?["baseGameOnly"]?.Type == JTokenType.Boolean && (bool)sourceObservations["selection"]["baseGameOnly"] &&
                (string)sourceObservations["selection"]["case"] == Chunk7ManualSeedSelection.CaseName &&
                (string)sourceObservations["nativeWrite"]?["sha256"] == selection.SourceArchiveSha256 &&
                (string)sourceObservations["nativeWrite"]?["gameId"] == request.PersistenceLoad.GameId &&
                (string)sourceObservations["nativeWrite"]?["gameName"] == request.PersistenceLoad.GameName &&
                (string)sourceObservations["nativeWrite"]?["area"] == request.PersistenceLoad.Area &&
                (string)sourceObservations["nativeWrite"]?["fileName"] == request.PersistenceLoad.FileName &&
                (string)sourceObservations["nativeWrite"]?["type"] == "Manual" &&
                sourceObservations["cleanup"]?["custodySettled"]?.Type == JTokenType.Boolean && (bool)sourceObservations["cleanup"]["custodySettled"] &&
                sourceObservations["cleanup"]?["sourceUnchanged"]?.Type == JTokenType.Boolean && (bool)sourceObservations["cleanup"]["sourceUnchanged"],
                "Staged Manual source lacks exact same-candidate native output/cold-lineage/settlement provenance.");
            sourceWorld = sourceObservations["savedWorld"] as JObject;
            Require(sourceWorld != null && sourceWorld["party"] is JArray party && party.Count > 0 && party.Count <= 24 &&
                (string)sourceWorld["gameId"] == request.PersistenceLoad.GameId && (string)sourceWorld["area"] == request.PersistenceLoad.Area,
                "Saved Manual native world/party is missing or unbounded.");
            expectedMainId = (string)sourceWorld["mainActorId"];
            Require(Chunk7ManualDerivedSelection.NativeId(expectedMainId) && ((JArray)sourceWorld["party"]).Count(row => (string)row["id"] == expectedMainId) == 1,
                "Manual source main actor lacks its exact native identity.");
            Require((string)sourceObservations["fixtureReadiness"]?["captureStatus"] == "PASS" &&
                sourceObservations["afterSaveArrangement"] is JObject && sourceObservations["nativeProfileInventory"] is JObject,
                "Manual source lacks bounded native readiness observations.");
        }
        private void VerifySourceBytes()
        {
            if (!sourcePinned) return;
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            Require(source.Exists && source.Length == sourceLength && source.LastWriteTimeUtc.Ticks == sourceTicks &&
                archive.Exists && archive.Length == archiveLength && archive.LastWriteTimeUtc.Ticks == archiveTicks &&
                Hash(sourcePath) == selection.SourceArtifactSha256 && Hash(archivePath) == selection.SourceArchiveSha256,
                "Immutable bootstrap source changed during native observation.");
            VerifyAssistedColdBytes();

        }
        private void RequireAuthority(bool cleanupOnly)
        {
            Require(ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.SaveManager, manager) &&
                authorization.IsActive && authorization.AuthorityGeneration == generation && authorization.IsExactPersistenceScope(authority) &&
                NativePersistenceIsolation.IsExactAuthority(authority) && authority.Root == root && manager.SavePath == root &&
                NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(root), "Areas") &&
                MatchesSelectedPlan(false) &&
                authority.AdmittedNativeLoadRequests <= 1 && authority.AdmittedNativeWriteRequests <= 1 &&
                authorization.AuthorizedLoadCount <= 1 && authorization.AuthorizedWriteCount <= 1 &&
                !authorization.IsOneShotWorkingWriteSuppressionArmed,
                "Manual seed lost its exact one-load one-write authority.");
            if (!cleanupOnly) Require(authorization.UnauthorizedLoadCount == 0 && authorization.UnauthorizedWriteCount == 0 &&
                authorization.FatalViolationCount == 0 && authorization.SuppressedWorkingWriteCount == 0 && authorization.BaselineLoadRequestCount == 0,
                "Cold inventory encountered a refused native boundary.");
            if (admitted)
            {
                if (buildStarted && Busy()) RequireWorldReferences();
                else RequireWorld();
            }
            if (!cleanupOnly && !Busy()) RequireCumulativeValues();
        }
        private void RequireWorldReferences()
        {
            Require(ReferenceEquals(nativePlayer, game.Player) && ReferenceEquals(state, game.State) &&
                ReferenceEquals(areaState, game.State?.LoadedAreaState) && ReferenceEquals(mainState, game.State?.LoadedAreaState?.MainState) &&
                ReferenceEquals(area, game.CurrentlyLoadedArea) && ReferenceEquals(mainActor, game.Player?.MainCharacter.Value) &&
                game.Player.GameId == request.PersistenceLoad.GameId && game.Player.MainCharacter.Value.CharacterName == request.PersistenceLoad.GameName &&
                game.Player.MainCharacter.Value.UniqueId == expectedMainId, "Admitted Manual-derived native world or main actor was replaced.");
            RequireCumulativeReferences();
        }
        private void RequireWorld()
        {
            RequireWorldReferences();
            var current = Chunk7NativeFieldRoster.Capture(game.Player);
            Require(current.All(actor => actor != null) && current.Select(actor => actor.UniqueId).Distinct(StringComparer.Ordinal).Count() == current.Length,
                "Native party contains null or duplicate actor identities.");
            for (var i = 0; i < actors.Length; i++)
                Require(current.Count(actor => ReferenceEquals(actor, actors[i])) == 1 && ReferenceEquals(actors[i].Descriptor, descriptors[i]) &&
                    ReferenceEquals(actors[i].View, views[i]) && actors[i].IsInState && actors[i].IsInGame,
                    "Admitted original actor, descriptor or view was replaced.");
            RequireAcquiredActor();
            Require(current.All(actor => actors.Any(original => ReferenceEquals(actor, original)) ||
                ReferenceEquals(actor, acquiredActor) || ReferenceEquals(actor, acquiredPet)),
                "An unrelated actor entered the admitted native party.");
        }
        private bool Busy() => LoadingProcess.Instance != null && (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) ||
            NativePersistenceIsolation.HasPendingWrites || persistence.AreaTransitionPending || persistence.HasActiveSaveScope ||
            persistence.HasSaveAdmissionFence || world.SerializationBusy;
        private void RequireNoOwners() => Require(world.Registry.Owners.Length == 0 && !controls.HasUnsettledRelationshipTransition &&
            !world.UnifiedTurn.HasUnsettledPreparation && world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted &&
                pair.Relationship.PairOwner == null && pair.Relationship.Runtime.NoPreparedPairResidue && !pair.Combat.HasActiveCommand &&
                !pair.Combat.HasActiveGroundMovement && !pair.Combat.HasChargeOwnership), "Cold inventory found mounted custody.");
        private void RequireNoEffects() => Require(persistence.SnapshotCount <= (buildStarted ? 1 : 0) && persistence.CompletedSaveCount <= (buildStarted ? 1 : 0) &&
            persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == 0 &&
            controls.CapturePersistentSlots().Length == 0, "Save-only derivative observed excess snapshots, writing, cast or actor/pair restoration.");
        private JObject AuthoritySnapshot() => new JObject { ["root"] = root, ["generation"] = generation,
            ["maximumNativeLoads"] = authority.MaximumNativeLoadRequests, ["maximumNativeWrites"] = authority.MaximumNativeWriteRequests,
            ["admittedNativeLoads"] = authority.AdmittedNativeLoadRequests, ["admittedNativeWrites"] = authority.AdmittedNativeWriteRequests,
            ["authorizedLoads"] = authorization.AuthorizedLoadCount, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
            ["unauthorizedLoads"] = authorization.UnauthorizedLoadCount, ["unauthorizedWrites"] = authorization.UnauthorizedWriteCount,
            ["fatalViolations"] = authorization.FatalViolationCount, ["suppressedWrites"] = authorization.SuppressedWorkingWriteCount };
        private JObject Snapshot() => new JObject { ["frame"] = UnityEngine.Time.frameCount, ["game"] = probe.Identity(game),
            ["saveManager"] = probe.Identity(manager), ["player"] = probe.Identity(game.Player), ["state"] = probe.Identity(game.State),
            ["areaState"] = probe.Identity(game.State.LoadedAreaState), ["mainState"] = probe.Identity(game.State.LoadedAreaState.MainState),
            ["gameId"] = game.Player.GameId, ["gameName"] = game.Player.MainCharacter.Value.CharacterName,
            ["mainActorId"] = game.Player.MainCharacter.Value.UniqueId, ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
            ["mode"] = game.CurrentMode.ToString(), ["cutscene"] = game.CutsceneLock, ["inCombat"] = game.Player.IsInCombat,
            ["nativeLoading"] = Busy(), ["authority"] = AuthoritySnapshot(),
            ["party"] = new JArray(actors.Select(actor => new JObject { ["id"] = actor.UniqueId, ["blueprint"] = actor.Blueprint.AssetGuid,
                ["object"] = probe.Identity(actor), ["descriptor"] = probe.Identity(actor.Descriptor), ["view"] = probe.Identity(actor.View),
                ["isInGame"] = actor.IsInGame, ["isInState"] = actor.IsInState, ["commandsEmpty"] = actor.Commands.Empty })),
            ["owners"] = world.Registry.Owners.Length, ["slots"] = controls.CapturePersistentSlots().Length,
            ["casts"] = controls.NativeCastRequestCount, ["snapshots"] = persistence.SnapshotCount, ["completedSaves"] = persistence.CompletedSaveCount,
            ["semanticRestores"] = persistence.SemanticRestoreCount, ["presentationRestores"] = persistence.PresentationRestoreCount };
        private void BeginCleanup() { if (cleanup) return; cleanup = true; cleanupStart = clock.Elapsed.TotalSeconds; }
        private void PollCleanup()
        {
            try
            {
                if (completed) return;
                RequireAuthority(true);
                if (Busy() || probe?.InSynchronousCallback == true || ordinaryProbe?.InSynchronousCallback == true ||
                    manualSaveProbe?.InCallback == true || persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused)
                { Retain("Cold native load/callback or worker retains observer custody."); return; }
                RequireNoOwners();
                if (errors.Count == 0) Require(acquisitionProven, "Manual-derived acquisition cannot complete before native choices, save and field proof.");
                if (!DrainBuildCustody()) { Retain("Owned native builder/selector still retains custody."); return; }
                // Observation failures never authorize unrelated native cleanup mutations.
                // Once native custody is settled, release our own observers even
                // when a source/effect invariant failed; preserve the failure.
                try { RequireNoEffects(); } catch (Exception error) { Fail("C7-manual-derived-cleanup", error.ToString()); }
                var sourceUnchanged = sourcePinned;
                try { VerifySourceBytes(); } catch (Exception error) { sourceUnchanged = false; Fail("C7-manual-derived-cleanup", error.ToString()); }
                if (admitted && evidence["settledWorld"] == null) evidence["settledWorld"] = Snapshot();
                try { CaptureCumulativeFinal(); } catch (Exception error) { Fail("C7-manual-derived-cleanup", error.ToString()); }
                if (ordinaryProbe != null && evidence["nativeAcquisition"] == null) evidence["nativeAcquisition"] = ordinaryProbe.Capture();
                if (manualSaveProbe != null && evidence["nativeSave"] == null) evidence["nativeSave"] = manualSaveProbe.Capture();
                if (probe != null && evidence["nativeLoad"] == null) evidence["nativeLoad"] = probe.Capture();
                if (restoration != null && evidence["noPreparation"] == null)
                {
                    var observed = restoration.Capture(); evidence["noPreparation"] = observed;
                    if (!restoration.Complete || !(observed["events"] is JArray rows) || rows.Count != 0)
                        Fail("C7-manual-derived-cleanup", "Native preparation/round or KMC restoration was observed during acquisition.");
                }
                // Both exact observers are attempted independently. A failed
                // unpatch keeps its field and prevents completion/host release.
                Exception failure = null; var saveObserverReleased = ordinaryProbe == null && manualSaveProbe == null;
                try { ordinaryProbe?.Dispose(); saveObserverReleased = true; } catch (Exception error) { failure = error; Fail("C7-manual-derived-cleanup", error.ToString()); }
                try { manualSaveProbe?.Dispose(); saveObserverReleased = saveObserverReleased && true; }
                catch (Exception error) { saveObserverReleased = false; failure = failure ?? error; Fail("C7-manual-derived-cleanup", error.ToString()); }
                try { restoration?.Dispose(); } catch (Exception error) { failure = error; Fail("C7-manual-derived-cleanup", error.ToString()); }
                // Retain the whole load-observation graph until the acquisition observer
                // has released its native callbacks; never abandon partial installs.
                if (saveObserverReleased)
                    try { probe?.Dispose(); } catch (Exception error) { failure = failure ?? error; Fail("C7-manual-derived-cleanup", error.ToString()); }
                if (failure != null) throw new InvalidOperationException("Cold observer cleanup remains retained.", failure);
                Require(probe == null || probe.IdentityReleased, "Cold observer native labels remain retained.");
                evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["nativeWorkerSettled"] = !Busy(),
                    ["observersReleased"] = true, ["worldAdmitted"] = admitted, ["sourcePinned"] = sourcePinned, ["sourceUnchanged"] = sourceUnchanged,
                    ["authority"] = AuthoritySnapshot() };
                RestoreAcquisitionPresentation();
                evidence["acceptedChoiceCount"] = choices.Count(row => (string)row["event"] == "native-choice");
                evidence["nativeCommitCount"] = choices.Count(row => (string)row["event"] == "native-level-committed");
                evidence["elapsedSeconds"] = clock.Elapsed.TotalSeconds;
                settled = true; Pass("C7-manual-derived-cleanup");
                if (cumulative == null) RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors);
                else RuntimeLoaderInventory.WriteArtifact(request, CumulativeEvidenceFileName, CumulativeEvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors);
                completed = true;
            }
            catch (Exception error) { Fail("C7-manual-derived-cleanup", error.ToString()); Retain(error.Message); }
        }
        private void Retain(string reason)
        {
            if (pending || clock.Elapsed.TotalSeconds - cleanupStart <= 45) return;
            Fail("C7-manual-derived-cleanup", reason + " The deadline does not relinquish custody.");
            var observation = new JObject { ["cleanupPendingAtHostBoundary"] = true, ["cleanupCustodyRetained"] = true, ["reason"] = reason, ["authority"] = AuthoritySnapshot() };
            if (cumulative == null) RuntimeLoaderInventory.WriteArtifact(request, PendingFileName, PendingKind, "FAIL", observation, errors);
            else RuntimeLoaderInventory.WriteArtifact(request, CumulativePendingFileName, CumulativePendingKind, "FAIL", observation, errors);
            pending = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanup) { Fail("C7-manual-derived-interrupted", "Cold inventory ended before its selected terminal proof."); BeginCleanup(); }
            if (!completed) PollCleanup(); return completed && settled;
        }
        public void Dispose() { if (disposed) return; if (!DrainForHost()) throw new InvalidOperationException("Cold inventory custody remains retained."); disposed = true; }
        private void Pass(string row) { row = SelectedRow(row); if (results.All(value => value.Name != row)) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "PASS", AssertionPassCount = 1, Errors = Array.Empty<string>() }); }
        private void Fail(string row, string error)
        {
            row = SelectedRow(row); if (errors.Contains(error)) return; errors.Add(error); var value = results.SingleOrDefault(result => result.Name == row);
            if (value == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { error } });
            else { value.Status = "FAIL"; value.AssertionFailCount++; value.Errors = value.Errors.Concat(new[] { error }).ToArray(); }
        }
        private static string Hash(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    }
}
