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
    // Separate save-only derivative. The historical read-only bridge is unchanged.
    // One exact native cold load is followed by one new native Manual save.
    internal sealed partial class Chunk7AcquiredManualScenario : IDisposable
    {
        internal const string EvidenceFileName = "chunk7-acquired-manual-evidence.json";
        internal const string EvidenceKind = "chunk7-acquired-manual-evidence";
        internal const string PendingFileName = "chunk7-acquired-manual-cleanup-pending.json";
        internal const string PendingKind = "chunk7-acquired-manual-cleanup-pending";
        private readonly RuntimeRequest request;
        private readonly Chunk7AcquiredManualSelection selection;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly PersistenceSaveAuthorization authority;
        private readonly MountedWorldService world;
        private readonly MountedPersistenceService persistence;
        private readonly NativeMountedControlService controls;
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
        internal static bool SupportsScenario(string value) => value == Chunk7AcquiredManualSelection.Scenario;

        internal Chunk7AcquiredManualScenario(RuntimeRequest request, RuntimeSaveAuthorization authorization,
            PersistenceSaveAuthorization authority, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, MountedPersistenceService persistence, HorseCompanionBlueprintService horses)
        {
            if (request?.AcquiredManual == null || request.Validate().Count != 0)
                throw new ArgumentException("An exact closed acquired Manual request is required.");
            this.request = request; selection = request.AcquiredManual;
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
            Require(authority.MatchesAcquiredManualPlan(request.PersistenceLoad, true, true), "Manual output was not initially absent/unwritten."); ReadSource();
            probe = new Chunk7BootstrapInventoryProbe(game); probe.Install();
            restoration = new NativeArchiveRestorationProbe(persistence); restoration.Install();
            evidence["processId"] = Process.GetCurrentProcess().Id;
            evidence["selection"] = Chunk7EvidenceDto.Request(selection);
            evidence["source"] = new JObject { ["runId"] = selection.SourceRunId, ["artifactSha256"] = selection.SourceArtifactSha256,
                ["archiveSha256"] = selection.SourceArchiveSha256, ["archiveLeaf"] = request.PersistenceLoad.FileName,
                ["sameCandidate"] = true, ["diagnosticArchiveContentRead"] = false };
            evidence["sourceNativeWorld"] = sourceWorld.DeepClone();
            evidence["sourceAcquisition"] = sourceObservations.DeepClone();
            evidence["reservation"] = ReservationSnapshot();
            evidence["beforeLoad"] = new JObject { ["frame"] = UnityEngine.Time.frameCount, ["game"] = probe.Identity(game),
                ["saveManager"] = probe.Identity(manager), ["menuPlayer"] = probe.Identity(menuPlayer),
                ["loadedAreaPresent"] = false, ["nativeLoading"] = false, ["authority"] = AuthoritySnapshot() };
            evidence["resourceWrites"] = 0; evidence["xpWrites"] = 0; evidence["moneyWrites"] = 0;
            evidence["profileAdmissionChanged"] = false; evidence["ordinaryAcquisitionProven"] = false;
            evidence["sixPairFixtureQualified"] = false; Pass("C7-acquired-manual-source");
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
                Require(clock.Elapsed.TotalSeconds <= 240, "Cold inventory exceeded its bounded native load/readiness window.");
                if (Busy()) return;
                if (saveRequested) { PollManualSave(); return; }
                if (admitted) { TickField(); return; }
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
                if (horses.State != HorseCompanionBlueprintState.Registered) return;
                var candidateActors = BindColdActors();
                if (candidateActors == null) return;
                RequireNoOwners(); RequireNoEffects();
                var data = persistence.LoadedArchive;
                Require(data != null && data.SchemaVersion == 3 && data.CampaignId == saved.GameId && data.AreaId == saved.Area &&
                    data.Pairs.Length == 0 && data.Participations.Length == 0 && data.Slots.Length == 0 && data.Combat == null,
                    "Cold source is not the qualified empty mounted archive.");
                var trace = restoration.Capture();
                Require(restoration.Complete && trace["events"] is JArray entries && entries.Count == 0,
                    "Cold inventory observed native Prepare/round or KMC actor/debt restoration entries.");
                nativePlayer = game.Player; state = game.State; areaState = game.State.LoadedAreaState;
                mainState = game.State.LoadedAreaState.MainState; area = game.CurrentlyLoadedArea; mainActor = game.Player.MainCharacter.Value;
                actors = candidateActors; descriptors = actors.Select(actor => (object)actor.Descriptor).ToArray(); views = actors.Select(actor => (object)actor.View).ToArray();
                admitted = true; RequireWorld(); VerifySourceBytes();
                BindAcquiredOwner(); BindPriorOwner();
                evidence["loadedWorld"] = Snapshot();
                evidence["loadedOwner"] = CaptureAcquiredOwner();
                noArrangementLoaded = CaptureNoArrangement(); evidence["loadedArrangement"] = noArrangementLoaded.DeepClone();
                evidence["archive"] = new JObject { ["schemaVersion"] = data.SchemaVersion, ["campaignId"] = data.CampaignId, ["areaId"] = data.AreaId,
                    ["pairs"] = data.Pairs.Length, ["participations"] = data.Participations.Length, ["slots"] = data.Slots.Length,
                    ["combatPresent"] = data.Combat != null, ["nativeRestorationGrantedByDiagnostic"] = false };
                Pass("C7-acquired-manual-native-load"); Pass("C7-acquired-manual-restored-owner"); StartField();
            }
            catch (Exception error) { Fail("C7-acquired-manual-runtime", error.ToString()); BeginCleanup(); }
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
            // This family never acquires slot projection. The native slider getter
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
            // These static getters do not open the selector. The case-owned selector has its separate raw receipt.
            result["staticPartyFacts"] = new JObject { ["observationScope"] = "static-getters-only", ["selectorOpened"] = false, ["liveSelectorRequiredAuthorityAvailable"] = false,
                ["membershipOrRemovalQualified"] = false, ["characters"] = new JArray(characters.Select(value => new JObject {
                    ["actorId"] = value.UniqueId, ["blueprintGuid"] = value.Blueprint.AssetGuid, ["isMain"] = ReferenceEquals(value, main),
                    ["lockedCompanionComponent"] = value.Blueprint.GetComponent<Kingmaker.Blueprints.LockedCompanionComponent>() != null })) };

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
                saves[0].Area?.AssetGuidThreadSafe == request.PersistenceLoad.Area && saves[0].Type == SaveInfo.SaveType.Auto &&
                saves[0].IsAutoLevelupSave && saves[0].HasFileOnDisk && saves[0].OperationState == SaveInfo.StateType.None,
                "Read-only isolated descriptor inventory differs from its sole qualified Auto_1 source.");
            Require(ReferenceEquals(settings, Kingmaker.UI.SettingsUI.SettingsRoot.Instance) && ReferenceEquals(onlyOne, settings.OnlyOneSave) &&
                ReferenceEquals(enabled, settings.AutosaveEnabled) && ReferenceEquals(slots, settings.AutosaveSlots) &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled && slots.OverrideDefaultValue == null,
                "Native settings identity or read-only getter admission changed during observation.");
            RequireWorld(); result["captureStatus"] = "PASS"; return result;
        }
        private void ReadSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7AcquiredManualSelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 && (source.Attributes & FileAttributes.ReparsePoint) == 0 &&
                archive.Exists && archive.Length == request.PersistenceLoad.Length && (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == selection.SourceArtifactSha256 && Hash(archivePath) == selection.SourceArchiveSha256,
                "Exact staged acquisition source/archive bytes differ.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks; archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks; sourcePinned = true;
            var artifact = JObject.Parse(File.ReadAllText(sourcePath)); sourceObservations = artifact["observations"] as JObject;
            Require((string)artifact["runId"] == selection.SourceRunId && (string)artifact["scenario"] == (TwoOwners ? Chunk7CumulativeAcquisitionSelection.Scenario : Chunk7ManualDerivedSelection.Scenario) &&
                (string)artifact["evidenceKind"] == (TwoOwners ? Chunk7ManualDerivedScenario.CumulativeEvidenceKind : Chunk7ManualDerivedScenario.EvidenceKind) && (string)artifact["status"] == "PASS" &&
                artifact["errors"] is JArray sourceErrors && sourceErrors.Count == 0 &&
                (string)artifact["commit"] == request.Commit && (string)artifact["dllSha256"] == request.DllSha256 &&
                (string)artifact["dllMvid"] == request.DllMvid && sourceObservations != null &&
                sourceObservations["selection"]?["baseGameOnly"]?.Type == JTokenType.Boolean && (bool)sourceObservations["selection"]["baseGameOnly"] &&
                (string)sourceObservations["nativeWrite"]?["sha256"] == selection.SourceArchiveSha256 &&
                (string)sourceObservations["nativeWrite"]?["gameId"] == request.PersistenceLoad.GameId &&
                (string)sourceObservations["nativeWrite"]?["gameName"] == request.PersistenceLoad.GameName &&
                (string)sourceObservations["nativeWrite"]?["area"] == request.PersistenceLoad.Area &&
                (string)sourceObservations["nativeWrite"]?["leaf"] == "Auto_1.zks" &&
                (string)sourceObservations["field"]?["actorId"] == selection.ActorId && (string)sourceObservations["field"]?["petId"] == selection.PetId &&
                sourceObservations["cleanup"]?["custodySettled"]?.Type == JTokenType.Boolean && (bool)sourceObservations["cleanup"]["custodySettled"],
                "Staged source lacks exact same-candidate successful Manual-derived acquisition provenance.");
            var sourceCase = (string)sourceObservations["selection"]?["case"];
            Require(sourceCase == Chunk7ManualDerivedSelection.ExistingCase || sourceCase == Chunk7ManualDerivedSelection.PaidSmallCase,
                "Only the two complete ordinary acquisition routes may produce this derivative.");
            sourcePaid = sourceCase == Chunk7ManualDerivedSelection.PaidSmallCase;
            Require(sourceObservations["nativeWrite"]?["saveBeforeFieldSelection"]?.Type == JTokenType.Boolean &&
                (bool)sourceObservations["nativeWrite"]["saveBeforeFieldSelection"] == sourcePaid,
                "Native acquisition save/field order differs from its closed source case.");
            sourceWorld = sourceObservations["loadedWorld"] as JObject;
            Require(sourceWorld != null && sourceWorld["party"] is JArray party && party.Count > 0 && party.Count <= 24 &&
                (string)sourceWorld["gameId"] == request.PersistenceLoad.GameId && (string)sourceWorld["area"] == request.PersistenceLoad.Area,
                "Source native world/party is missing or unbounded.");
            expectedMainId = (string)sourceWorld["mainActorId"];
            Require(Guid.TryParseExact(expectedMainId, "D", out var mainId) && mainId != Guid.Empty &&
                ((JArray)sourceWorld["party"]).Count(row => (string)row["id"] == expectedMainId) == 1,
                "Source main actor lacks its exact native party identity.");
            RequireTwoOwnerSource();
        }
        private void VerifySourceBytes()
        {
            if (!sourcePinned) return;
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            Require(source.Exists && source.Length == sourceLength && source.LastWriteTimeUtc.Ticks == sourceTicks &&
                archive.Exists && archive.Length == archiveLength && archive.LastWriteTimeUtc.Ticks == archiveTicks &&
                Hash(sourcePath) == selection.SourceArtifactSha256 && Hash(archivePath) == selection.SourceArchiveSha256,
                "Immutable acquisition source changed during native observation.");
        }
        private void RequireAuthority(bool cleanupOnly)
        {
            Require(ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.SaveManager, manager) &&
                authorization.IsActive && authorization.AuthorityGeneration == generation && authorization.IsExactPersistenceScope(authority) &&
                NativePersistenceIsolation.IsExactAuthority(authority) && authority.Root == root && manager.SavePath == root &&
                NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(root), "Areas") &&
                authority.MatchesAcquiredManualPlan(request.PersistenceLoad, true, false) &&
                authority.AdmittedNativeLoadRequests <= 1 && authority.AdmittedNativeWriteRequests <= 1 &&
                authorization.AuthorizedLoadCount <= 1 && authorization.AuthorizedWriteCount <= 1 &&
                !authorization.IsOneShotWorkingWriteSuppressionArmed,
                "Acquired Manual lost its exact one-load one-write authority.");
            if (!cleanupOnly) Require(authorization.UnauthorizedLoadCount == 0 && authorization.UnauthorizedWriteCount == 0 &&
                authorization.FatalViolationCount == 0 && authorization.SuppressedWorkingWriteCount == 0 && authorization.BaselineLoadRequestCount == 0,
                "Cold inventory encountered a refused native boundary.");
            if (admitted)
            {
                if (saveRequested && Busy()) RequireSaveWorldReferences();
                else RequireWorld();
            }
            if (!cleanupOnly && !Busy()) RequireTwoOwnerValues();
        }
        private void RequireWorld()
        {
            Require(ReferenceEquals(nativePlayer, game.Player) && ReferenceEquals(state, game.State) &&
                ReferenceEquals(areaState, game.State?.LoadedAreaState) && ReferenceEquals(mainState, game.State?.LoadedAreaState?.MainState) &&
                ReferenceEquals(area, game.CurrentlyLoadedArea) && ReferenceEquals(mainActor, game.Player?.MainCharacter.Value) &&
                game.Player.GameId == request.PersistenceLoad.GameId && game.Player.MainCharacter.Value.CharacterName == request.PersistenceLoad.GameName &&
                game.Player.MainCharacter.Value.UniqueId == expectedMainId, "Admitted cold native world or main actor was replaced.");
            var current = game.Player.Party.ToArray();
            Require(current.Length == actors.Length || fieldTransitionOpen && sourcePaid &&
                current.Length >= actors.Length && current.Length <= actors.Length + 2, "Admitted cold party count changed.");
            Require(current.All(value => actors.Any(actor => ReferenceEquals(actor, value)) || fieldTransitionOpen && sourcePaid &&
                (ReferenceEquals(value, acquiredActor) || ReferenceEquals(value, acquiredPet))), "Native field transition contains a foreign actor.");
            for (var i = 0; i < actors.Length; i++)
                Require(current.Count(actor => ReferenceEquals(actor, actors[i])) == 1 && ReferenceEquals(actors[i].Descriptor, descriptors[i]) &&
                    ReferenceEquals(actors[i].View, views[i]) && actors[i].IsInState && actors[i].IsInGame,
                    "Admitted cold actor, descriptor or view was replaced.");
            if (ownerBound) RequireAcquiredOwner();
            RequireTwoOwnerReferences();
        }
        private bool Busy() => LoadingProcess.Instance != null && (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) ||
            NativePersistenceIsolation.HasPendingWrites || persistence.AreaTransitionPending || persistence.HasActiveSaveScope ||
            persistence.HasSaveAdmissionFence || world.SerializationBusy;
        private void RequireNoOwners() => Require(world.Registry.Owners.Length == 0 && !controls.HasUnsettledRelationshipTransition &&
            !world.UnifiedTurn.HasUnsettledPreparation && world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted &&
                pair.Relationship.PairOwner == null && pair.Relationship.Runtime.NoPreparedPairResidue && !pair.Combat.HasActiveCommand &&
                !pair.Combat.HasActiveGroundMovement && !pair.Combat.HasChargeOwnership), "Cold inventory found mounted custody.");
        private void RequireNoEffects() => Require(persistence.SnapshotCount <= (saveRequested ? 1 : 0) && persistence.CompletedSaveCount <= (saveRequested ? 1 : 0) &&
            persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == 0 &&
            controls.CapturePersistentSlots().Length == 0, "Save-only derivative observed excess snapshots, writing, cast or actor/pair restoration.");
        private JObject AuthoritySnapshot() => new JObject { ["root"] = root, ["generation"] = generation,
            ["maximumNativeLoads"] = authority.MaximumNativeLoadRequests, ["maximumNativeWrites"] = authority.MaximumNativeWriteRequests,
            ["admittedNativeLoads"] = authority.AdmittedNativeLoadRequests, ["admittedNativeWrites"] = authority.AdmittedNativeWriteRequests,
            ["authorizedLoads"] = authorization.AuthorizedLoadCount, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
            ["unauthorizedLoads"] = authorization.UnauthorizedLoadCount, ["unauthorizedWrites"] = authorization.UnauthorizedWriteCount,
            ["fatalViolations"] = authorization.FatalViolationCount, ["suppressedWrites"] = authorization.SuppressedWorkingWriteCount,
            ["reservationCount"] = authority.AcquiredManualReservationCount, ["manualNumber"] = authority.AcquiredManualNumber,
            ["outputLeaf"] = authority.AcquiredManualOutputLeaf };
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
                if (Busy() || probe?.InSynchronousCallback == true || saveProbe?.InCallback == true || persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused)
                { Retain("Cold native load/callback or worker retains observer custody."); return; }
                RequireNoOwners();
                if (fieldAdmission != null && !fieldAdmission.TryDrain()) { Retain("Owned native selector retains custody."); return; }
                RestoreFieldPresentation();
                if (errors.Count == 0) Require(saveProven, "Acquired Manual cannot complete before its exact native output proof.");
                // Observation failures never authorize native cleanup mutations.
                // Once native custody is settled, release our own observers even
                // when a source/effect invariant failed; preserve the failure.
                try { RequireNoEffects(); } catch (Exception error) { Fail("C7-acquired-manual-cleanup", error.ToString()); }
                var sourceUnchanged = sourcePinned;
                try { VerifySourceBytes(); } catch (Exception error) { sourceUnchanged = false; Fail("C7-acquired-manual-cleanup", error.ToString()); }
                if (admitted && evidence["settledWorld"] == null) evidence["settledWorld"] = Snapshot();
                try { CaptureTwoOwners("settledTwoOwners"); } catch (Exception error) { Fail("C7-acquired-manual-cleanup", error.ToString()); }
                if (saveProbe != null && evidence["nativeSave"] == null) evidence["nativeSave"] = saveProbe.Capture();
                if (probe != null && evidence["nativeLoad"] == null) evidence["nativeLoad"] = probe.Capture();
                if (restoration != null && evidence["noPreparation"] == null) evidence["noPreparation"] = restoration.Capture();
                // Both exact observers are attempted independently. A failed
                // unpatch keeps its field and prevents completion/host release.
                Exception failure = null; var saveObserverReleased = saveProbe == null;
                try { saveProbe?.Dispose(); saveObserverReleased = true; } catch (Exception error) { failure = error; Fail("C7-acquired-manual-cleanup", error.ToString()); }
                try { restoration?.Dispose(); } catch (Exception error) { failure = error; Fail("C7-acquired-manual-cleanup", error.ToString()); }
                // The save observer borrows this identity registry. Its failed
                // unpatch must retain the registry owner through a settled retry.
                if (saveObserverReleased)
                    try { probe?.Dispose(); } catch (Exception error) { failure = failure ?? error; Fail("C7-acquired-manual-cleanup", error.ToString()); }
                if (failure != null) throw new InvalidOperationException("Cold observer cleanup remains retained.", failure);
                Require(probe == null || probe.IdentityReleased, "Cold observer native labels remain retained.");
                evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["nativeWorkerSettled"] = !Busy(),
                    ["selectorClosed"] = fieldAdmission == null || !fieldAdmission.Owned, ["presentationRestored"] = !presentationCaptured || presentationRestored,
                    ["slotProjectionEnabled"] = NativePersistenceIsolation.NativeSlotRotationEnabled, ["manualPartyMutations"] = 0,
                    ["observersReleased"] = true, ["worldAdmitted"] = admitted, ["sourcePinned"] = sourcePinned, ["sourceUnchanged"] = sourceUnchanged,
                    ["authority"] = AuthoritySnapshot() };
                settled = true; Pass("C7-acquired-manual-cleanup");
                RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors);
                completed = true;
            }
            catch (Exception error) { Fail("C7-acquired-manual-cleanup", error.ToString()); Retain(error.Message); }
        }
        private void Retain(string reason)
        {
            if (pending || clock.Elapsed.TotalSeconds - cleanupStart <= 45) return;
            Fail("C7-acquired-manual-cleanup", reason + " The deadline does not relinquish custody.");
            RuntimeLoaderInventory.WriteArtifact(request, PendingFileName, PendingKind, "FAIL", new JObject {
                ["cleanupPendingAtHostBoundary"] = true, ["cleanupCustodyRetained"] = true, ["reason"] = reason, ["authority"] = AuthoritySnapshot() }, errors);
            pending = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanup) { Fail("C7-acquired-manual-interrupted", "Cold inventory ended before its selected terminal proof."); BeginCleanup(); }
            if (!completed) PollCleanup(); return completed && settled;
        }
        public void Dispose() { if (disposed) return; if (!DrainForHost()) throw new InvalidOperationException("Cold inventory custody remains retained."); disposed = true; }
        private void Pass(string row) { if (results.All(value => value.Name != row)) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "PASS", AssertionPassCount = 1, Errors = Array.Empty<string>() }); }
        private void Fail(string row, string error)
        {
            if (errors.Contains(error)) return; errors.Add(error); var value = results.SingleOrDefault(result => result.Name == row);
            if (value == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { error } });
            else { value.Status = "FAIL"; value.AssertionFailCount++; value.Errors = value.Errors.Concat(new[] { error }).ToArray(); }
        }
        private static string Hash(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    }
}
