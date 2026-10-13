using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.LoadingScreen;
using UnityEngine.SceneManagement;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // A selected disposable native main-campaign start. The existing Working
    // descriptor remains an immutable authorization anchor and is never loaded.
    internal sealed class Chunk7NativeBootstrapScenario : IDisposable
    {
        internal const string EvidenceFileName = "chunk7-native-bootstrap-evidence.json";
        internal const string EvidenceKind = "chunk7-native-bootstrap-evidence";
        internal const string PendingFileName = "chunk7-native-bootstrap-cleanup-pending.json";
        internal const string PendingKind = "chunk7-native-bootstrap-cleanup-pending";
        private readonly RuntimeRequest request;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly MountedWorldService world;
        private readonly MountedPersistenceService persistence;
        private readonly NativeMountedControlService controls;
        private readonly HorseCompanionBlueprintService horses;
        private readonly Game game;
        private readonly Player player;
        private readonly SaveManager saveManager;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly JObject evidence = new JObject();
        private readonly List<string> errors = new List<string>();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private PersistenceSaveAuthorization authority;
        private int generation;
        private BlueprintRoot blueprintRoot;
        private BlueprintAreaPreset preset;
        private Chunk7NativeBootstrapProbe probe;
        private object nativeState, nativeAreaState, nativeMainState, nativeActor, nativeDescriptor, nativeView;
        private string nativeActorId, nativeName;
        private bool preflightReady, dispatched, callbackEntered, callbackReturned;
        private bool slotOwned, priorSlot, cleanup, settled, completed, disposed, pending;
        private double cleanupStart;
        private int snapshotsBefore, savesBefore;
        private long castsBefore;
        internal bool PreflightReady => preflightReady;
        internal bool IsBound => authority != null;
        internal bool IsCompleted => completed;
        internal IReadOnlyList<string> Errors => errors;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;
        internal static bool SupportsScenario(string value) => value == Chunk7NativeBootstrapSelection.Scenario;

        internal Chunk7NativeBootstrapScenario(RuntimeRequest request, RuntimeSaveAuthorization authorization,
            GameMountedRelationshipService relationship, NativeMountedControlService controls,
            MountedPersistenceService persistence, HorseCompanionBlueprintService horses)
        {
            if (request?.NativeBootstrap == null || request.NativeBootstrap.Validate(request.Scenario).Count != 0 || request.Validate().Count != 0)
                throw new ArgumentException("An exact pre-fixture native bootstrap request is required.");
            this.request = request; this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            world = relationship?.World ?? throw new ArgumentNullException(nameof(relationship));
            this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.horses = horses ?? throw new ArgumentNullException(nameof(horses));
            game = Game.Instance ?? throw new InvalidOperationException("Native main-menu game is unavailable.");
            player = game.Player; saveManager = game.SaveManager;
            evidence["selection"] = Chunk7EvidenceDto.Request(request.NativeBootstrap);
            evidence["processId"] = Process.GetCurrentProcess().Id;
            evidence["workingDeserialized"] = false; evidence["mountedCombatProven"] = false;
            evidence["ordinaryAcquisitionProven"] = false; evidence["smallPoseQualified"] = false;
            evidence["diagnosticArchiveContentRead"] = false;
            evidence["nativeSourceMetadataEnumerationSelected"] = true;
        }
        internal static BlueprintAreaPreset ResolveMainPreset()
        {
            var root = BlueprintRoot.Instance; var value = root?.NewGamePreset;
            Require(value != null && value.Area != null && value.EnterPoint?.Area != null &&
                value.Area.AssetGuidThreadSafe == value.EnterPoint.Area.AssetGuidThreadSafe && value.MakeAutosave &&
                (value.PlayerCharacter ?? root.DefaultPlayerCharacter) != null,
                "The exact authored main-campaign preset, player blueprint or first autosave is unavailable.");
            return value;
        }
        internal void Preflight()
        {
            if (completed || disposed || preflightReady) return;
            try
            {
                if (cleanup) { PollCleanup(); return; }
                evidence["preflight"] = CapturePreflight();
                RequireNativeMenuAdmission();
                Require(horses.State != HorseCompanionBlueprintState.Faulted,
                    "Native bootstrap refused before new game: Horse/Ranger registration faulted. " + horses.Failure);
                Require(clock.Elapsed.TotalSeconds <= 90, "Native bootstrap Horse/blueprint readiness timed out before any new game or write.");
                if (horses.State != HorseCompanionBlueprintState.Registered) return;
                RequireNativeRangerSelection();
                preset = ResolveMainPreset(); blueprintRoot = BlueprintRoot.Instance;
                evidence["preset"] = new JObject { ["source"] = "BlueprintRoot.NewGamePreset", ["guid"] = preset.AssetGuid,
                    ["area"] = preset.Area.AssetGuidThreadSafe, ["enterPoint"] = preset.EnterPoint.AssetGuid,
                    ["enterPointArea"] = preset.EnterPoint.Area.AssetGuidThreadSafe, ["makeAutosave"] = preset.MakeAutosave,
                    ["charGen"] = preset.CharGen, ["playerBlueprint"] = (preset.PlayerCharacter ?? blueprintRoot.DefaultPlayerCharacter).AssetGuid,
                    ["usesPresetPlayer"] = preset.PlayerCharacter != null, ["companionCount"] = preset.Companions?.Count ?? 0 };
                preflightReady = true; Pass("C7-bootstrap-preflight");
            }
            catch (Exception error) { Fail("C7-bootstrap-preflight", error.ToString()); BeginCleanup(); PollCleanup(); }
        }
        private JObject CapturePreflight()
        {
            var library = ResourcesLibrary.LibraryObject; BlueprintScriptableObject value = null;
            library?.BlueprintsByAssetId.TryGetValue(HorseCompanionBlueprintService.RangerSelectionGuid, out value);
            var ranger = value as BlueprintFeatureSelection; var features = ranger?.AllFeatures;
            return new JObject { ["horseState"] = horses.State.ToString(), ["horseFailure"] = horses.Failure,
                ["libraryReady"] = library != null && library.BlueprintsByAssetId.Count > 0,
                ["rangerSelectionGuid"] = HorseCompanionBlueprintService.RangerSelectionGuid,
                ["rangerSelectionPresent"] = ranger != null, ["rangerAllFeaturesCount"] = features == null ? -1 : features.Length,
                ["rangerFirst32Guids"] = features == null ? new JArray() : new JArray(features.Take(32).Select(feature => feature?.AssetGuid)),
                ["horseFeatureGuid"] = horses.HorseFeature?.AssetGuid, ["horseUnitGuid"] = horses.HorseUnit?.AssetGuid,
                ["loadedAreaPresent"] = game.CurrentlyLoadedArea != null, ["mainActorPresent"] = player?.MainCharacter.Value != null,
                ["authorizedLoads"] = authorization.AuthorizedLoadCount, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
                ["nativeNewGameDispatched"] = dispatched };
        }
        private void RequireNativeRangerSelection()
        {
            Require(horses.State == HorseCompanionBlueprintState.Registered,
                "Native bootstrap Horse registration is no longer ready.");
            BlueprintScriptableObject value = null;
            ResourcesLibrary.LibraryObject?.BlueprintsByAssetId.TryGetValue(
                HorseCompanionBlueprintService.RangerSelectionGuid, out value);
            HorseCompanionBlueprintService.RequireNativeRangerOptions(
                (value as BlueprintFeatureSelection)?.AllFeatures, horses.HorseFeature);
        }
        internal void BindAndStart(PersistenceSaveAuthorization scope)
        {
            Require(preflightReady && authority == null && !cleanup, "Native bootstrap is not ready for its single isolated binding.");
            authority = scope ?? throw new ArgumentNullException(nameof(scope)); generation = authorization.AuthorityGeneration;
            evidence["nativeSourceMetadataEnumerationCompleted"] = true;
            try
            {
                RequireNativeMenuAdmission(); RequireNativeRangerSelection(); RequireAuthority(false);
                Require(authority.MatchesNativeBootstrapPlan(request.Fixture.Working, preset.Area.AssetGuidThreadSafe) &&
                    authority.AdmittedNativeLoadRequests == 0 && authority.AdmittedNativeWriteRequests == 0 &&
                    authority.BootstrapFreezeCount == 0 && !authority.BootstrapWindowOpen && authority.BootstrapGameId == null,
                    "The isolated scope is not the exact unused zero-load, one-autosave bootstrap plan.");
                snapshotsBefore = persistence.SnapshotCount; savesBefore = persistence.CompletedSaveCount; castsBefore = controls.NativeCastRequestCount;
                Require(snapshotsBefore == 0 && savesBefore == 0 && castsBefore == 0 && persistence.SemanticRestoreCount == 0 &&
                    persistence.PresentationRestoreCount == 0 && persistence.ArchivePostLoadObservedCount == 0, "A prior load, mounted action or save preceded native bootstrap.");
                probe = new Chunk7NativeBootstrapProbe(game, preset); probe.Install();
                evidence["before"] = Snapshot();
                priorSlot = NativePersistenceIsolation.NativeSlotRotationEnabled; slotOwned = true;
                NativePersistenceIsolation.EnableNativeSlotRotation();
                RequireNativeMenuAdmission();
                var menu = game.UI.MainMenu;
                evidence["menuAdmission"] = new JObject { ["frame"] = UnityEngine.Time.frameCount, ["menu"] = probe.Identity(menu),
                    ["pcController"] = game.IsControllerMouse, ["mainMenuSceneLoaded"] = SceneManager.GetSceneByName(SceneName.MainMenu).isLoaded,
                    ["loadingScreenPresent"] = LoadingScreen.Instance != null, ["nativeBusy"] = Busy(),
                    ["unexpectedDialog"] = game.IsModeActive(GameModeType.Dialog) || DialogMessageBox.Instance != null && DialogMessageBox.Instance.IsShown,
                    ["alreadyEntering"] = NativeMenuAlreadyEntering(menu) };
                // EnterGame owns a one-frame native coroutine. Retain custody
                // before dispatch, even if a callback or the dispatch itself fails.
                dispatched = true;
                menu.EnterGame(() =>
                {
                    callbackEntered = true;
                    try
                    {
                        Require(!cleanup && (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                            "Native bootstrap entry was cancelled or an unexpected dialog appeared before its owned callback.");
                        RequireOriginalMenu(); RequireNativeRangerSelection(); RequireAuthority(false);
                        Require(ReferenceEquals(BlueprintRoot.Instance, blueprintRoot) && ReferenceEquals(ResolveMainPreset(), preset), "Authored native preset changed before entry.");
                        authority.OpenBootstrapWindow();
                        game.LoadNewGame(preset, null);
                    }
                    catch (Exception error) { Fail("C7-bootstrap-native-start", error.ToString()); BeginCleanup(); }
                    finally { callbackReturned = true; }
                });
            }
            catch (Exception error) { Fail("C7-bootstrap-native-start", error.ToString()); BeginCleanup(); }
        }
        internal void Update()
        {
            if (completed || disposed) return;
            try
            {
                if (cleanup) { PollCleanup(); return; }
                RequireAuthority(false);
                Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown,
                    "Native authored start encountered an unexpected dialog; no input is authorized.");
                Require(clock.Elapsed.TotalSeconds <= 300, "Native authored start exceeded its bounded settlement window.");
                if (!callbackReturned || Busy()) return;
                Require(probe != null && probe.Complete && ReferenceEquals(probe.NativePlayer, player) && player.GameId == probe.NativeGameId,
                    "Exact native new-game/identity/autosave observation did not complete.");
                Require(game.CurrentlyLoadedArea == preset.Area && player.MainCharacter.Value != null &&
                    player.MainCharacter.Value.Blueprint == (preset.PlayerCharacter ?? blueprintRoot.DefaultPlayerCharacter),
                    "Native new game did not materialize its authored area and main actor.");
                // Story input is never synthesized. A noninteractive start remains
                // a recorded fixture prerequisite, not mounted/acquisition proof.
                if (game.CurrentMode != GameModeType.Default || game.CutsceneLock || game.IsModeActive(GameModeType.Dialog) ||
                    player.MainCharacter.Value.View?.AgentASP == null) return;
                RequireNoOwners();
                var save = probe.NativeSave; var completion = persistence.LastCompletedSave;
                Require(save != null && probe.NativeRequestedSave != null && !ReferenceEquals(save, probe.NativeRequestedSave) &&
                    completion != null && completion.Sequence == savesBefore + 1 &&
                    ReferenceEquals(completion.Requested, probe.NativeRequestedSave) && ReferenceEquals(completion.Written, save) &&
                    completion.Path == save.FolderName && save.HasFileOnDisk && save.OperationState == SaveInfo.StateType.None &&
                    save.FileName == Chunk7NativeBootstrapSelection.AutoLeaf && save.FolderName == Path.Combine(authority.Root, save.FileName) &&
                    save.Area == preset.Area && save.GameId == player.GameId && save.GameName == player.MainCharacter.Value.CharacterName &&
                    game.SaveManager.Count(value => ReferenceEquals(value, save)) == 1 &&
                    authority.BootstrapFreezeCount == 1 && !authority.BootstrapWindowOpen && authority.BootstrapGameId == player.GameId &&
                    authority.BootstrapGameName == save.GameName && authority.AdmittedNativeWriteRequests == 1 && authorization.AuthorizedWriteCount == 1 &&
                    persistence.SnapshotCount == snapshotsBefore + 1 && persistence.CompletedSaveCount == savesBefore + 1,
                    "The one native autosave did not finish with its exact minted campaign and storage descriptor.");
                Require(controls.NativeCastRequestCount == castsBefore && controls.CapturePersistentSlots().Length == 0 &&
                    persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 && persistence.ArchivePostLoadObservedCount == 0,
                    "New native campaign inherited mounted casts, slots, load or restoration state.");
                Require(game.State?.LoadedAreaState?.MainState != null, "Native authored area state is not ready.");
                nativeState = game.State; nativeAreaState = game.State.LoadedAreaState; nativeMainState = game.State.LoadedAreaState.MainState;
                var main = player.MainCharacter.Value;
                nativeActor = main; nativeActorId = main.UniqueId; nativeDescriptor = main.Descriptor; nativeView = main.View; nativeName = main.CharacterName;
                RequireNativeWorld();
                evidence["nativeWorld"] = Snapshot();
                var file = new FileInfo(save.FolderName);
                evidence["nativeWrite"] = new JObject { ["path"] = save.FolderName, ["fileName"] = save.FileName, ["internalName"] = save.Name,
                    ["gameId"] = save.GameId, ["gameName"] = save.GameName, ["area"] = save.Area.AssetGuidThreadSafe,
                    ["type"] = save.Type.ToString(), ["length"] = file.Length, ["sha256"] = Hash(save.FolderName),
                    ["operation"] = save.OperationState.ToString(), ["autoLevelup"] = save.IsAutoLevelupSave,
                    ["contentInspected"] = false, ["coldLoadQualified"] = false,
                    ["requestedDescriptor"] = probe.Identity(probe.NativeRequestedSave), ["preparedDescriptor"] = probe.Identity(save),
                    ["completionSequence"] = completion.Sequence };
                Pass("C7-bootstrap-native-start"); Pass("C7-bootstrap-native-world"); Pass("C7-bootstrap-autosave"); BeginCleanup();
            }
            catch (Exception error) { Fail("C7-bootstrap-runtime", error.ToString()); BeginCleanup(); }
        }
        private static bool NativeMenuAlreadyEntering(MainMenu menu)
        {
            var field = typeof(MainMenu).GetField("m_EnterGameStarted", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field != null && field.MetadataToken == 0x04000720 && field.FieldType == typeof(bool), "Native MainMenu EnterGame ownership contract changed.");
            return (bool)field.GetValue(menu);
        }
        private void RequireNativeMenuAdmission()
        {
            RequireOriginalMenu();
            Require(game.IsControllerMouse && SceneManager.GetSceneByName(SceneName.MainMenu).isLoaded &&
                game.UI?.MainMenu != null && LoadingScreen.Instance != null && !Busy() &&
                !game.IsModeActive(GameModeType.Dialog) && (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown) &&
                !NativeMenuAlreadyEntering(game.UI.MainMenu),
                "Native bootstrap requires an idle exact PC main menu, loading screen, and no queued work or unexpected dialog.");
        }
        private void RequireOriginalMenu()
        {
            Require(ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.Player, player) && ReferenceEquals(game.SaveManager, saveManager) &&
                player != null && player.MainCharacter.Value == null && game.CurrentlyLoadedArea == null && Game.NewGameUnit == null,
                "Native bootstrap requires its exact empty menu world and no generated character product.");
            RequireNoOwners();
        }
        private void RequireAuthority(bool cleanupOnly)
        {
            Require(ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.Player, player) && ReferenceEquals(game.SaveManager, saveManager),
                "Native bootstrap lost its admitted Game/Player/SaveManager.");
            if (authority == null) return;
            Require(authorization.IsActive && authorization.AuthorityGeneration == generation && authorization.IsExactPersistenceScope(authority) &&
                NativePersistenceIsolation.IsExactAuthority(authority) && game.SaveManager.SavePath == authority.Root &&
                NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(authority.Root), "Areas") &&
                authority.MaximumNativeLoadRequests == 0 && authority.MaximumNativeWriteRequests == 1 &&
                authority.AdmittedNativeLoadRequests == 0 && authorization.AuthorizedLoadCount == 0 && authority.AdmittedNativeWriteRequests <= 1,
                "Native bootstrap lost its exact zero-load isolated authority.");
            if (!cleanupOnly) Require(authorization.UnauthorizedLoadCount == 0 && authorization.UnauthorizedWriteCount == 0 &&
                authorization.FatalViolationCount == 0 && authorization.SuppressedWorkingWriteCount == 0 && authorization.BaselineLoadRequestCount == 0,
                "Native bootstrap encountered a refused native boundary.");
            if (probe?.NativeGameId != null) Require(player.GameId == probe.NativeGameId, "Native bootstrap campaign identity changed.");
            if (nativeState != null) RequireNativeWorld();
        }
        private void RequireNativeWorld()
        {
            var main = player.MainCharacter.Value;
            Require(ReferenceEquals(nativeState, game.State) && ReferenceEquals(nativeAreaState, game.State?.LoadedAreaState) &&
                ReferenceEquals(nativeMainState, game.State?.LoadedAreaState?.MainState) && ReferenceEquals(game.CurrentlyLoadedArea, preset.Area) &&
                ReferenceEquals(nativeActor, main) && main?.UniqueId == nativeActorId && main.CharacterName == nativeName &&
                ReferenceEquals(nativeDescriptor, main.Descriptor) && ReferenceEquals(nativeView, main.View),
                "Admitted native bootstrap world, main actor or live view was replaced.");
        }
        private bool Busy() => LoadingProcess.Instance != null && (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) ||
            NativePersistenceIsolation.HasPendingWrites || persistence.AreaTransitionPending || persistence.HasActiveSaveScope ||
            persistence.HasSaveAdmissionFence || world.SerializationBusy;
        private void RequireNoOwners() => Require(world.Registry.Owners.Length == 0 && !controls.HasUnsettledRelationshipTransition &&
            !world.UnifiedTurn.HasUnsettledPreparation && world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted &&
                pair.Relationship.PairOwner == null && pair.Relationship.Runtime.NoPreparedPairResidue && !pair.Combat.HasActiveCommand &&
                !pair.Combat.HasActiveGroundMovement && !pair.Combat.HasChargeOwnership), "Native bootstrap found mounted command/preparation custody.");
        private JObject Snapshot() => new JObject { ["frame"] = UnityEngine.Time.frameCount, ["game"] = probe.Identity(game), ["player"] = probe.Identity(player),
            ["saveManager"] = probe.Identity(saveManager), ["mainActor"] = probe.Identity(player.MainCharacter.Value), ["mainActorId"] = player.MainCharacter.Value?.UniqueId,
            ["mainBlueprint"] = player.MainCharacter.Value?.Blueprint.AssetGuid, ["mainView"] = probe.Identity(player.MainCharacter.Value?.View),
            ["gameId"] = player.GameId, ["gameName"] = player.MainCharacter.Value?.CharacterName, ["area"] = game.CurrentlyLoadedArea?.AssetGuidThreadSafe,
            ["mode"] = game.CurrentMode.ToString(), ["cutscene"] = game.CutsceneLock, ["nativeLoading"] = Busy(),
            ["party"] = new JArray(player.Party.Select(unit => new JObject { ["id"] = unit.UniqueId, ["blueprint"] = unit.Blueprint.AssetGuid,
                ["object"] = probe.Identity(unit), ["descriptor"] = probe.Identity(unit.Descriptor), ["view"] = probe.Identity(unit.View) })),
            ["owners"] = world.Registry.Owners.Length, ["slots"] = controls.CapturePersistentSlots().Length, ["casts"] = controls.NativeCastRequestCount,
            ["snapshots"] = persistence.SnapshotCount, ["completedSaves"] = persistence.CompletedSaveCount,
            ["semanticRestores"] = persistence.SemanticRestoreCount, ["presentationRestores"] = persistence.PresentationRestoreCount,
            ["nativePostLoads"] = persistence.ArchivePostLoadObservedCount, ["authority"] = AuthoritySnapshot() };
        private JObject AuthoritySnapshot() => new JObject { ["root"] = authority?.Root, ["generation"] = generation,
            ["maximumNativeLoads"] = authority?.MaximumNativeLoadRequests, ["maximumNativeWrites"] = authority?.MaximumNativeWriteRequests,
            ["admittedNativeLoads"] = authority?.AdmittedNativeLoadRequests, ["admittedNativeWrites"] = authority?.AdmittedNativeWriteRequests,
            ["authorizedLoads"] = authorization.AuthorizedLoadCount, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
            ["unauthorizedLoads"] = authorization.UnauthorizedLoadCount, ["unauthorizedWrites"] = authorization.UnauthorizedWriteCount,
            ["fatalViolations"] = authorization.FatalViolationCount, ["suppressedWrites"] = authorization.SuppressedWorkingWriteCount,
            ["freezeCount"] = authority?.BootstrapFreezeCount, ["windowOpen"] = authority?.BootstrapWindowOpen,
            ["frozenGameId"] = authority?.BootstrapGameId, ["frozenGameName"] = authority?.BootstrapGameName };
        private void BeginCleanup() { if (cleanup) return; cleanup = true; cleanupStart = clock.Elapsed.TotalSeconds; }
        private void PollCleanup()
        {
            try
            {
                if (completed) return;
                RequireAuthority(true);
                if (dispatched && !callbackReturned || probe?.InSynchronousCallback == true || Busy() ||
                    persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused)
                { Retain("Native menu callback, loading or save worker retains bootstrap custody."); return; }
                RequireNoOwners();
                if (probe != null)
                {
                    // Freeze raw labels before release; retries never rebuild them
                    // from a released identity registry or overwrite a final leaf.
                    if (evidence["probe"] == null) evidence["probe"] = probe.Capture();
                    probe.Dispose(); Require(probe.IdentityReleased, "Bootstrap observer identities remain retained.");
                }
                if (slotOwned)
                {
                    Require(NativePersistenceIsolation.NativeSlotRotationEnabled, "Owned native slot projection was changed externally.");
                    if (!priorSlot) NativePersistenceIsolation.DisableNativeSlotRotation();
                    slotOwned = false;
                }
                authority?.CloseUnusedBootstrapWindow();
                evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["callbackEntered"] = callbackEntered, ["callbackReturned"] = callbackReturned,
                    ["dispatched"] = dispatched, ["nativeWorkerSettled"] = !Busy(), ["observerReleased"] = probe == null || probe.IdentityReleased,
                    ["slotProjectionBefore"] = priorSlot, ["slotProjectionAfter"] = NativePersistenceIsolation.NativeSlotRotationEnabled,
                    ["slotProjectionOwned"] = slotOwned, ["identityWindowOpen"] = authority?.BootstrapWindowOpen ?? false,
                    ["authority"] = AuthoritySnapshot() };
                settled = true; Pass("C7-bootstrap-cleanup"); evidence["elapsedSeconds"] = clock.Elapsed.TotalSeconds;
                RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors); completed = true;
            }
            catch (Exception error) { Fail("C7-bootstrap-cleanup", error.ToString()); Retain(error.Message); }
        }
        private void Retain(string reason)
        {
            if (pending || clock.Elapsed.TotalSeconds - cleanupStart <= 45) return;
            Fail("C7-bootstrap-cleanup", reason + " The deadline does not relinquish native custody.");
            RuntimeLoaderInventory.WriteArtifact(request, PendingFileName, PendingKind, "FAIL", new JObject { ["reason"] = reason,
                ["cleanupCustodyRetained"] = true, ["cleanupPendingAtHostBoundary"] = true, ["authority"] = AuthoritySnapshot() }, errors); pending = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanup) { Fail("C7-bootstrap-interrupted", "Native bootstrap was interrupted before its selected terminal proof."); BeginCleanup(); }
            if (!completed) PollCleanup(); return completed && settled;
        }
        public void Dispose() { if (disposed) return; if (!DrainForHost()) throw new InvalidOperationException("Native bootstrap custody remains retained."); disposed = true; }
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
