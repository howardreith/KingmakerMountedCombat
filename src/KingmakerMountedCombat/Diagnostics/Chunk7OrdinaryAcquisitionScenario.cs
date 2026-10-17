using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Group;
using Kingmaker.UI.LevelUp;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Closed native ordinary-choice family. A selected isolated save authority
    // is mandatory before any UI choice; it caps actual native write admission.
    // Native paid acquisition is retained. Rollback belongs to the outer guarded
    // fixture/profile transaction, never a refund, fact deletion or entity edit.
    internal sealed class Chunk7OrdinaryAcquisitionScenario : IDisposable
    {
        internal const string EvidenceFileName = "chunk7-ordinary-acquisition-evidence.json";
        internal const string EvidenceKind = "chunk7-ordinary-acquisition-evidence";
        internal const string PendingFileName = "chunk7-ordinary-cleanup-pending.json";
        internal const string PendingKind = "chunk7-ordinary-cleanup-pending";
        private readonly RuntimeRequest request;
        private readonly Chunk7OrdinaryAcquisitionSelection selection;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly PersistenceSaveAuthorization authority;
        private readonly MountedWorldService world;
        private readonly NativeMountedControlService controls;
        private readonly MountedPersistenceService persistence;
        private readonly HorseCompanionBlueprintService horses;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly List<string> errors = new List<string>();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly JObject evidence = new JObject();
        private readonly JArray choices = new JArray();
        private readonly int authorityGeneration;
        private readonly string expectedRoot;
        private Chunk7OrdinaryBuildDriver driver;
        private NativeOrdinaryAcquisitionProbe probe;
        private Player nativeWorld;
        private UnitEntityData[] originalSelection, originalParty;
        private string[] originalPartyCharacters;
        private bool originalPause, captured, cleanup, cleanupProven, completed, disposed, pendingWritten, groupOwned;
        private double stageStart, cleanupStart;
        private int stage, snapshotBefore, completedSavesBefore;
        private long castsBefore;
        private MovementCaptureOverlayLease ummLease;
        private GroupManager group;
        private string inventoryPath, inventoryHash;
        private long inventoryLength, inventoryTicks;

        internal bool IsCompleted => completed;
        internal IReadOnlyList<string> Errors => errors;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;
        internal static bool SupportsScenario(string value) => value == Chunk7OrdinaryAcquisitionSelection.Scenario;
        internal Chunk7OrdinaryAcquisitionScenario(RuntimeRequest request, Chunk7OrdinaryAcquisitionSelection selection,
            RuntimeSaveAuthorization authorization, PersistenceSaveAuthorization authority, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, MountedPersistenceService persistence, HorseCompanionBlueprintService horses)
        {
            if (request == null || selection == null || selection.Validate(request.Scenario, request.RunId).Count != 0)
                throw new ArgumentException("An exact closed ordinary acquisition request is required.");
            this.request = request; this.selection = selection; this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            world = relationship?.World ?? throw new ArgumentNullException(nameof(relationship));
            this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.horses = horses ?? throw new ArgumentNullException(nameof(horses));
            expectedRoot = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(request.EvidenceRoot)), "runtime-staging", "persistence-" + request.RunId, "Saved Games");
            authorityGeneration = authorization.AuthorityGeneration;
        }
        internal void Update()
        {
            if (completed || disposed) return;
            try
            {
                if (cleanup) { PollCleanup(); return; }
                if (!captured) Start();
                Require(clock.Elapsed.TotalSeconds <= 720 && clock.Elapsed.TotalSeconds - stageStart <= (stage == 0 ? 600 : 60),
                    "Ordinary acquisition exceeded its bounded native stage " + stage + ".");
                AssertAuthority();
                if (NativeBusy()) return;
                Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown,
                    "Unexpected native dialog; no dialog is dismissed and no further ordinary choice is issued.");
                if (stage == 0)
                {
                    if (driver.Tick() != Chunk7OrdinaryBuildStep.Complete) return;
                    evidence["builderComplete"] = driver.CaptureState(); Pass("C7-ordinary-native-choices"); Next(1);
                }
                else if (stage == 1)
                {
                    if (!ObserveCompletedAutosave()) return;
                    Pass("C7-ordinary-native-autosave");
                    if (selection.Case == Chunk7OrdinaryAcquisitionSelection.PaidSmallCase) { OpenFieldSelection(); Next(2); }
                    else { CaptureField(); BeginCleanup(); }
                }
                else if (stage == 2)
                {
                    AssertAuthority();
                    if (!FieldSelectionReady()) return;
                    var remote = Game.Instance.Player.RemoteCompanions.SingleOrDefault(value => ReferenceEquals(value.Value, driver.Actor));
                    Require(remote != null && ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, group) && groupOwned,
                        "The exact acquired remote companion or owned native party selector was replaced.");
                    var selected = GroupSelection();
                    Require(selected.Count < 6 && !selected.Any(value => ReferenceEquals(value.Value, driver.Actor)) &&
                        originalPartyCharacters.All(id => selected.Count(value => value.Value?.UniqueId == id) == 1),
                        "Native party cap or original party membership does not admit the acquired companion.");
                    group.SwitchCharacter(remote);
                    Require(GroupSelection().Count(value => ReferenceEquals(value.Value, driver.Actor)) == 1 && GroupSelection().Count <= 6,
                        "Native party selector did not accept the exact companion.");
                    AssertAuthority(); group.Go(); Next(3);
                }
                else if (stage == 3)
                {
                    if (driver.Actor?.View?.AgentASP == null || !driver.Actor.IsInGame || driver.Actor.Descriptor.Pet?.View?.AgentASP == null) return;
                    if (NativeSmallRiderBody.Capture(driver.Actor, driver.Actor.Descriptor.Pet)?.NativeRigAvailable != true) return;
                    CompleteFieldSelection(); CaptureField(); BeginCleanup();
                }
            }
            catch (Exception error) { Fail("C7-ordinary-runtime", error.ToString()); BeginCleanup(); }
        }
        private void Start()
        {
            AssertAuthority(); var game = Game.Instance;
            Require(request.Fixture.Working.Sha256 == Chunk7WorldScenarioEngine.WorkingHash && game?.Player != null &&
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe == request.Fixture.Working.Area && game.CurrentMode == GameModeType.Default &&
                !game.Player.IsInCombat && !game.CutsceneLock && !NativeBusy(), "Ordinary acquisition needs the exact idle qualified Working world.");
            Require(horses.HorseFeature != null && horses.HorseUnit != null && horses.LevelRank != null &&
                world.Registry.Owners.Length == 0 && world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted && pair.Relationship.Runtime.NoPreparedPairResidue) &&
                !controls.HasUnsettledRelationshipTransition && !world.UnifiedTurn.HasUnsettledPreparation,
                "Ordinary fixture begins with unexpected mounted ownership or unresolved preparation.");
            Require(game.UI?.CharacterBuildController != null && game.UI.CharacterBuildController.LevelUpController == null &&
                game.UI.CharacterBuildController.Unit == null && (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "A pre-existing native builder or dialog prevents ordinary acquisition.");
            originalParty = new[] { Chunk7WorldScenarioEngine.MammothRiderId, Chunk7WorldScenarioEngine.MammothId,
                Chunk7WorldScenarioEngine.HorseRiderId }.Select(Exact).ToArray();
            Require(originalParty.All(actor => actor.IsInState && actor.IsInGame && actor.View?.AgentASP != null && actor.Commands.Empty),
                "Qualified original actors are not idle exact native field entities.");
            ValidateInventory(); nativeWorld = game.Player;
            originalPartyCharacters = game.Player.PartyCharacters.Select(value => value.Value.UniqueId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Require(originalPartyCharacters.Length < 6, "No ordinary native party slot remains.");
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray(); originalPause = game.IsPaused; captured = true;
            snapshotBefore = persistence.SnapshotCount; completedSavesBefore = persistence.CompletedSaveCount; castsBefore = controls.NativeCastRequestCount;
            MovementScreenshotCaptureCoordinator.AcquireClosedUmmLeaseRetained(out ummLease);
            Require(game.SaveManager.IsSaveAllowed() && !game.SaveManager.Any(save => save.Type == SaveInfo.SaveType.Auto),
                "Native terminal autosave is unavailable or would overwrite/rotate an existing slot.");
            NativePersistenceIsolation.EnableNativeSlotRotation();
            var paid = selection.Case == Chunk7OrdinaryAcquisitionSelection.PaidSmallCase;
            var pins = new Chunk7OrdinaryBuildPins(request.RunId, selection.InventoryArtifactSha256,
                paid ? Chunk7OrdinaryBuildMode.NewSmallCustomCompanion : Chunk7OrdinaryBuildMode.ExistingRangerLevels,
                selection.SmallRaceGuid, selection.SmallPresetGuid, selection.SkeletonName,
                selection.Gender == "Female" ? Gender.Female : Gender.Male, selection.LeftHanded == true, selection.MinimumRangerLevel);
            var existing = paid ? null : Exact(selection.ExistingActorId);
            Require(paid || ReferenceEquals(existing, originalParty[2]), "Existing acquisition is restricted to the exact observed pet-free Working rider.");
            driver = new Chunk7OrdinaryBuildDriver(pins, existing, horses.HorseFeature, horses.HorseUnit.AssetGuid, AssertChoiceAuthority, ObserveChoice);
            probe = new NativeOrdinaryAcquisitionProbe(() => driver.Actor);
            probe.Install();
            evidence["processId"] = Process.GetCurrentProcess().Id;
            evidence["case"] = selection.Case; evidence["selection"] = JObject.FromObject(selection);
            evidence["choices"] = choices; evidence["originalParty"] = new JArray(originalParty.Select(actor => actor.UniqueId));
            evidence["authority"] = AuthoritySnapshot(); evidence["nativeProgressionRequiresExternalWorkingRestoration"] = true;
            evidence["vendorDialogueProven"] = false; evidence["freshMainCampaignProven"] = false;
            evidence["smallPoseQualified"] = false; evidence["mountedCombatProven"] = false;
            evidence["before"] = PartySnapshot(); game.IsPaused = false;
            Pass("C7-ordinary-fixture"); Next(0);
        }
        private void AssertAuthority()
        {
            Require(authorization.IsActive && authorization.IsExactPersistenceScope(authority) &&
                authorization.AuthorityGeneration == authorityGeneration && NativePersistenceIsolation.IsExactAuthority(authority) &&
                authority.Root == expectedRoot && Game.Instance?.SaveManager?.SavePath == expectedRoot &&
                NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(expectedRoot), "Areas") &&
                authority.MaximumNativeWriteRequests == 1 && authority.AdmittedNativeWriteRequests <= 1 &&
                authorization.AuthorizedLoadCount == 1 && authorization.AuthorizedWriteCount <= 1 && authorization.UnauthorizedLoadCount == 0 &&
                authorization.UnauthorizedWriteCount == 0 && authorization.FatalViolationCount == 0 && authorization.SuppressedWorkingWriteCount == 0 &&
                authorization.BaselineLoadRequestCount == 0 && !authorization.IsOneShotWorkingWriteSuppressionArmed,
                "Exact ordinary isolated authorization/root/generation or bounded native write admission was lost.");
            var game = Game.Instance;
            Require(game.Player != null && game.Player.GameId == request.Fixture.Working.GameId && game.Player.MainCharacter.Value.CharacterName == request.Fixture.Working.GameName &&
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe == request.Fixture.Working.Area && !game.Player.IsInCombat && !game.CutsceneLock &&
                !game.IsModeActive(GameModeType.Dialog) && (nativeWorld == null || ReferenceEquals(nativeWorld, game.Player)),
                "Ordinary acquisition lost its captured campaign/world or entered combat/dialog/cutscene.");
            if (inventoryPath != null)
            {
                var file = new FileInfo(inventoryPath);
                Require(file.Exists && file.Length == inventoryLength && file.LastWriteTimeUtc.Ticks == inventoryTicks,
                    "The exact staged native body inventory changed while ordinary choices were pending.");
            }
        }
        private void AssertChoiceAuthority()
        {
            AssertAuthority();
            Require(!NativeBusy() && (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "Native writer/loading or an unexpected dialog blocks further ordinary choices.");
        }
        private bool NativeBusy() => LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
            NativePersistenceIsolation.HasPendingWrites || persistence.AreaTransitionPending || persistence.HasActiveSaveScope ||
            persistence.HasSaveAdmissionFence || world.SerializationBusy;
        private void ValidateInventory()
        {
            inventoryPath = Path.Combine(Path.GetDirectoryName(expectedRoot), Chunk7OrdinaryAcquisitionSelection.InventoryLeaf);
            var file = new FileInfo(inventoryPath);
            Require(file.Exists && file.Length > 0 && file.Length <= 16 * 1024 * 1024 && (file.Attributes & FileAttributes.ReparsePoint) == 0,
                "Exact staged native inventory artifact is unavailable or unsafe.");
            inventoryLength = file.Length; inventoryTicks = file.LastWriteTimeUtc.Ticks; inventoryHash = Hash(inventoryPath);
            Require(inventoryHash == selection.InventoryArtifactSha256, "Native inventory artifact hash differs.");
            var artifact = JObject.Parse(File.ReadAllText(inventoryPath));
            Require((string)artifact["runId"] == selection.InventoryRunId && (string)artifact["scenario"] == "chunk7-two-pair-exploration" &&
                (string)artifact["status"] == "PASS" && (string)artifact["evidenceKind"] == "chunk7-world-evidence" &&
                (string)artifact["commit"] == request.Commit && (string)artifact["dllSha256"] == request.DllSha256 &&
                (string)artifact["dllMvid"] == request.DllMvid && artifact["errors"] is JArray failures && failures.Count == 0 &&
                (string)artifact["observations"]?["capability"]?["workingSha256"] == request.Fixture.Working.Sha256,
                "Native body inventory is not a completed same-candidate restored Working observation.");
            var inventory = artifact["observations"]?["nativeProfileInventory"] as JObject;
            Require(inventory != null && (string)inventory["captureStatus"] == "PASS" && (int)inventory["schemaVersion"] == 1 &&
                (string)inventory["gameplayAssemblyMvid"] == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7" &&
                inventory["profileAdmissionChanged"]?.Type == JTokenType.Boolean && (bool)inventory["profileAdmissionChanged"] == false,
                "Native body catalogue lacks its exact read-only installed provenance.");
            if (selection.Case == Chunk7OrdinaryAcquisitionSelection.PaidSmallCase)
            {
                var race = inventory["smallRaces"].Children<JObject>().SingleOrDefault(row => (string)row["raceGuid"] == selection.SmallRaceGuid);
                var preset = race?["presets"].Children<JObject>().SingleOrDefault(row => (string)row["presetGuid"] == selection.SmallPresetGuid);
                var skeleton = preset?[selection.Gender == "Male" ? "maleSkeleton" : "femaleSkeleton"];
                Require((string)race?["size"] == "Small" && (string)preset?["raceId"] == (string)race?["raceId"] &&
                    preset?["present"]?.Type == JTokenType.Boolean && (bool)preset["present"] &&
                    skeleton?["present"]?.Type == JTokenType.Boolean && (bool)skeleton["present"] &&
                    (string)skeleton["name"] == selection.SkeletonName, "Requested Small body has no exact observed native catalogue member.");
            }
            else Require(inventory["party"].Children<JObject>().Count(row => (string)row["id"] == selection.ExistingActorId &&
                row["petId"]?.Type == JTokenType.Null) == 1, "Existing acquisition actor lacks exact pet-free native inventory provenance.");
            Require(Hash(inventoryPath) == inventoryHash, "Native inventory changed during bounded read.");
            evidence["inventoryProvenance"] = new JObject { ["runId"] = selection.InventoryRunId, ["sha256"] = inventoryHash,
                ["length"] = inventoryLength, ["sameCandidate"] = true, ["sameWorking"] = true };
        }
        private void ObserveChoice(JObject observation)
        {
            Require(choices.Count < 1400, "Ordinary native choice evidence exceeds its declared bound.");
            choices.Add(observation.DeepClone());
        }
        private bool ObserveCompletedAutosave()
        {
            if (NativeBusy() || probe.ObservedSave == null || probe.ObservedSave.OperationState != SaveInfo.StateType.None || !probe.ObservedSave.HasFileOnDisk) return false;
            var save = probe.ObservedSave; var completion = persistence.LastCompletedSave;
            Require(probe.Complete && authorization.AuthorizedWriteCount == 1 && authority.AdmittedNativeWriteRequests == 1 &&
                persistence.SnapshotCount == snapshotBefore + 1 && persistence.CompletedSaveCount == completedSavesBefore + 1 &&
                completion != null && ReferenceEquals(completion.Requested, probe.ObservedRequestedSave) && ReferenceEquals(completion.Written, save) &&
                completion.Sequence == completedSavesBefore + 1 &&
                completion.Path == Path.Combine(expectedRoot, Chunk7OrdinaryAcquisitionSelection.AutoLeaf) &&
                save.FileName == Chunk7OrdinaryAcquisitionSelection.AutoLeaf && save.Type == SaveInfo.SaveType.Auto && probe.ObservedRequestedSave.IsAutoLevelupSave && !save.IsAutoLevelupSave &&
                save.GameId == request.Fixture.Working.GameId && save.GameName == request.Fixture.Working.GameName &&
                save.Area.AssetGuidThreadSafe == request.Fixture.Working.Area && controls.NativeCastRequestCount == castsBefore,
                "Native terminal autosave lacks exact causal, authority, completion or descriptor proof.");
            NativePersistenceIsolation.RequireOwnedWrite(save);
            var file = new FileInfo(save.FolderName); var archive = NativeMountedSaveStorage.ReadArchive(save.Saver);
            Require(archive.Kind == MountedSaveReadKind.Current && archive.SourceSchema == 3 && archive.Data != null &&
                archive.Data.Pairs.Length == 0 && archive.Data.Participations.Length == 0,
                "Ordinary unmounted native autosave readback contains unsupported or invented mounted ownership.");
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["leaf"] = save.FileName, ["length"] = file.Length,
                ["sha256"] = Hash(file.FullName), ["name"] = save.Name, ["type"] = save.Type.ToString(), ["autoLevelup"] = save.IsAutoLevelupSave,
                ["requestedObject"] = NativeOrdinaryAcquisitionProbe.Id(completion.Requested), ["writtenObject"] = NativeOrdinaryAcquisitionProbe.Id(completion.Written),
                ["completionSequence"] = completion.Sequence, ["completedWritesBefore"] = completedSavesBefore, ["operation"] = save.OperationState.ToString(), ["gameId"] = save.GameId, ["gameName"] = save.GameName,
                ["area"] = save.Area.AssetGuidThreadSafe, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
                ["admittedWrites"] = authority.AdmittedNativeWriteRequests, ["completedWrites"] = persistence.CompletedSaveCount - completedSavesBefore,
                ["snapshotCount"] = persistence.SnapshotCount - snapshotBefore, ["nativeCallbackSupplied"] = false,
                ["saveBeforeFieldSelection"] = selection.Case == Chunk7OrdinaryAcquisitionSelection.PaidSmallCase,
                ["schema"] = archive.SourceSchema };
            return true;
        }
        private void OpenFieldSelection()
        {
            AssertChoiceAuthority();
            Require(driver.Actor != null && Game.Instance.Player.RemoteCompanions.Count(value => ReferenceEquals(value.Value, driver.Actor)) == 1 &&
                !Game.Instance.Player.PartyCharacters.Any(value => ReferenceEquals(value.Value, driver.Actor)),
                "Paid native actor is not the one exact acquired remote companion.");
            group = Game.Instance.UI.Canvas.GroupManager;
            Require(group != null && !GroupActive(), "A pre-existing native party selector cannot be borrowed.");
            groupOwned = true; new ShowPartySelection().RunAction();
            Require(ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, group) && GroupActive(), "Native party selection did not open.");
            evidence["fieldSelection"] = new JObject { ["action"] = "native-ShowPartySelection", ["nativeCap"] = 6,
                ["actorId"] = driver.Actor.UniqueId, ["manualPartyMutation"] = false, ["openActiveSelf"] = true };
        }
        private bool GroupActive() => group != null && group.gameObject.activeSelf;
        private bool FieldSelectionReady()
        {
            Require(groupOwned && ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, group) && GroupActive(),
                "Owned native party selector closed or was replaced during its opening animation.");
            var alpha = GroupAlpha();
            Require(!float.IsNaN(alpha) && !float.IsInfinity(alpha) && alpha >= 0 && alpha <= 1,
                "Native party selector has an invalid animation observation.");
            // The real UI starts at alpha zero and fades to one. Its enclosing
            // stage keeps the existing timeout; no repeat open or click occurs.
            if (!group.gameObject.activeInHierarchy || alpha < 1) return false;
            evidence["fieldSelection"]["readyAlpha"] = alpha;
            evidence["fieldSelection"]["readyActiveInHierarchy"] = true;
            return true;
        }
        private void CompleteFieldSelection()
        {
            Require(groupOwned && ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, group) && !GroupActive(),
                "Exact owned native party selector did not complete its ordinary Go path.");
            evidence["fieldSelection"]["closedActiveSelf"] = false;
            groupOwned = false;
        }
        private List<UnitReference> GroupSelection()
        {
            var field = typeof(GroupManagerBase).GetField("PartyCharacter", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(field != null && field.MetadataToken == 0x04002B1C && field.FieldType == typeof(List<UnitReference>),
                "Native party selector read-only list contract differs.");
            return (List<UnitReference>)field.GetValue(group);
        }
        private float GroupAlpha()
        {
            var getter = typeof(GroupManagerBase).GetProperty("CanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic)?.GetGetMethod(true);
            Require(group != null && getter != null && getter.MetadataToken == 0x06003F4C && getter.ReturnType.FullName == "UnityEngine.CanvasGroup",
                "Native party selector read-only presentation contract differs.");
            var canvas = getter.Invoke(group, null); var alpha = getter.ReturnType.GetProperty("alpha");
            Require(canvas != null && alpha != null && alpha.PropertyType == typeof(float), "Native party selector presentation is unavailable.");
            return (float)alpha.GetValue(canvas, null);
        }
        private void CaptureField()
        {
            AssertAuthority(); var actor = driver.Actor; var pet = actor?.Descriptor.Pet;
            Require(actor != null && pet != null && actor.IsInState && actor.IsInGame && actor.IsDirectlyControllable && actor.View?.AgentASP != null &&
                pet.IsInState && pet.IsInGame && pet.IsDirectlyControllable && pet.View?.AgentASP != null &&
                ReferenceEquals(pet.Descriptor.Master.Value, actor) && pet.Blueprint == horses.HorseUnit &&
                Game.Instance.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, actor)) == 1 &&
                originalPartyCharacters.All(id => Game.Instance.Player.PartyCharacters.Count(value => value.Value?.UniqueId == id) == 1) &&
                actor.Descriptor.GetFact(horses.HorseFeature) is Feature fact && fact.Source != null &&
                (actor.Descriptor.GetFact(horses.LevelRank)?.GetRank() ?? 0) > 0 && actor.GetActivePolymorph() == null,
                "Ordinary acquired rider/Horse lacks native field placement, source/rank or reciprocal ownership.");
            if (selection.Case == Chunk7OrdinaryAcquisitionSelection.PaidSmallCase)
                Require(actor.Descriptor.OriginalSize == Size.Small && actor.Descriptor.State.Size == Size.Small,
                    "Acquired Small native body changed original/effective size.");
            evidence["field"] = new JObject { ["actorId"] = actor.UniqueId, ["actorObject"] = Id(actor), ["petId"] = pet.UniqueId,
                ["horseBlueprint"] = pet.Blueprint.AssetGuid, ["featureBlueprint"] = horses.HorseFeature.AssetGuid,
                ["featureSourcePresent"] = true, ["featureSourceGuid"] = ((Feature)actor.Descriptor.GetFact(horses.HorseFeature)).Source.AssetGuid,
                ["rank"] = actor.Descriptor.GetFact(horses.LevelRank).GetRank(),
                ["reciprocal"] = true, ["ordinaryFieldAdmissionProven"] = true, ["parties"] = PartySnapshot(),
                ["profileInventory"] = Chunk7NativeProfileInventory.Capture(new[] { actor, pet }),
                ["bodyObservation"] = JObject.FromObject(NativeSmallRiderBody.Capture(actor, pet) ?? new SmallRiderBodyObservation()) };
            Pass("C7-ordinary-native-field-inventory");
        }
        private JObject PartySnapshot() => new JObject { ["money"] = Game.Instance.Player.Money,
            ["partyCharacters"] = new JArray(Game.Instance.Player.PartyCharacters.Select(value => value.Value.UniqueId)),
            ["remoteCompanions"] = new JArray(Game.Instance.Player.RemoteCompanions.Select(value => value.Value.UniqueId)),
            ["originalActors"] = new JArray((originalParty ?? Array.Empty<UnitEntityData>()).Select(actor => new JObject { ["id"] = actor.UniqueId,
                ["object"] = Id(actor), ["inState"] = actor.IsInState, ["inGame"] = actor.IsInGame,
                ["level"] = actor.Descriptor.Progression.CharacterLevel, ["xp"] = actor.Descriptor.Progression.Experience,
                ["petId"] = actor.Descriptor.Pet?.UniqueId })) };
        private JObject AuthoritySnapshot() => new JObject { ["root"] = authority.Root, ["generation"] = authorityGeneration,
            ["exactScope"] = authorization.IsExactPersistenceScope(authority) && NativePersistenceIsolation.IsExactAuthority(authority),
            ["maximumNativeWrites"] = authority.MaximumNativeWriteRequests, ["admittedNativeWrites"] = authority.AdmittedNativeWriteRequests,
            ["authorizedLoads"] = authorization.AuthorizedLoadCount, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
            ["unauthorizedLoads"] = authorization.UnauthorizedLoadCount, ["unauthorizedWrites"] = authorization.UnauthorizedWriteCount,
            ["fatalViolations"] = authorization.FatalViolationCount, ["suppressedWrites"] = authorization.SuppressedWorkingWriteCount };
        private UnitEntityData Exact(string id)
        {
            var matches = Game.Instance.State.Units.Concat(Game.Instance.Player.AllCharacters).Where(actor => actor?.UniqueId == id).Distinct().ToArray();
            Require(matches.Length == 1, "Exact ordinary native actor is missing or duplicated: " + id); return matches[0];
        }
        private void Next(int next) { stage = next; stageStart = clock.Elapsed.TotalSeconds; }
        private void BeginCleanup() { if (cleanup) return; cleanup = true; cleanupStart = clock.Elapsed.TotalSeconds; }
        private void PollCleanup()
        {
            if (completed) return;
            if (NativeBusy() || persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused || probe?.InSynchronousCallback == true)
            { Retain("Native load, save worker or synchronous completion callback still owns this ordinary fixture."); return; }
            Require(authorization.IsActive && authorization.AuthorityGeneration == authorityGeneration &&
                authorization.IsExactPersistenceScope(authority) && NativePersistenceIsolation.IsExactAuthority(authority) &&
                Game.Instance.SaveManager.SavePath == expectedRoot,
                "Ordinary cleanup lost its exact isolated authority; no further UI input is permitted.");
            Require(!captured || ReferenceEquals(nativeWorld, Game.Instance.Player) &&
                Game.Instance.Player.GameId == request.Fixture.Working.GameId &&
                Game.Instance.Player.MainCharacter.Value.CharacterName == request.Fixture.Working.GameName &&
                Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe == request.Fixture.Working.Area,
                "Ordinary cleanup lost its captured native world/campaign/area; UI, selection and pause custody is retained.");
            // No callback is invoked directly. The one expected cancellation
            // dialog is admitted by its actual native stored target/method.
            if (driver != null && driver.TryGetOwnedBuildSession(out var builder))
            {
                if (DialogMessageBox.Instance?.IsShown == true)
                { Retain("An unexpected dialog is open; ordinary cleanup will not dismiss it."); return; }
                builder.OnHotKeyEscPressed();
                var dialog = DialogMessageBox.Instance;
                Require(dialog != null, "Expected native owned cancellation dialog is missing.");
                var callbackField = typeof(DialogMessageBox).GetField("m_OnClose", BindingFlags.Instance | BindingFlags.NonPublic);
                var boxField = typeof(DialogMessageBox).GetField("m_BoxType", BindingFlags.Instance | BindingFlags.NonPublic);
                Require(callbackField?.MetadataToken == 0x04001E05 && boxField?.MetadataToken == 0x04001E04,
                    "Native owned cancellation dialog contract differs.");
                var callback = callbackField.GetValue(dialog) as Action<DialogMessageBoxBase.BoxButton>;
                Require(dialog != null && dialog.IsShown && (DialogMessageBoxBase.BoxType)boxField.GetValue(dialog) == DialogMessageBoxBase.BoxType.Dialog &&
                    callback != null && callback.GetInvocationList().Length == 1 && ReferenceEquals(callback.Target, builder) &&
                    callback.Method.DeclaringType == typeof(CharacterBuildController) && callback.Method.MetadataToken == 0x060039BF,
                    "Native cancellation dialog belongs to another action; it remains untouched.");
                var buttonField = typeof(DialogMessageBox).GetField("m_ButtonYes", BindingFlags.Instance | BindingFlags.NonPublic);
                Require(buttonField?.MetadataToken == 0x04001DFB && buttonField.FieldType.FullName == "UnityEngine.UI.Button",
                    "Native owned cancellation button contract differs.");
                var button = buttonField.GetValue(dialog);
                var interactable = buttonField.FieldType.GetProperty("interactable");
                var onClick = buttonField.FieldType.GetProperty("onClick");
                Require(button is Component component && component.gameObject.activeInHierarchy && interactable?.PropertyType == typeof(bool) &&
                    (bool)interactable.GetValue(button, null) && onClick != null,
                    "Native owned cancellation button is unavailable.");
                var click = onClick.GetValue(button, null); var invoke = click?.GetType().GetMethod("Invoke", Type.EmptyTypes);
                Require(invoke != null && invoke.ReturnType == typeof(void), "Native owned cancellation click event is unavailable.");
                invoke.Invoke(click, null);
                if (builder.LevelUpController != null || dialog.IsShown) { Retain("Owned native builder cancellation has not settled."); return; }
            }
            if (groupOwned)
            {
                Require(ReferenceEquals(Game.Instance.UI.Canvas.GroupManager, group), "Owned party selector was replaced during cleanup.");
                if (GroupActive()) group.Close();
                if (GroupActive()) { Retain("Owned native party selector has not closed."); return; }
                groupOwned = false;
            }
            if (probe != null) { evidence["nativeObserver"] = probe.Capture(); probe.Dispose(); probe = null; }
            if (captured)
            {
                NativePersistenceIsolation.DisableNativeSlotRotation();
                SelectionManager.Instance.MultiSelect(originalSelection.Where(actor => actor.IsInState && actor.View != null).Select(actor => actor.View), false);
                Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection), "Original native selection restoration failed.");
                ummLease?.Dispose(); ummLease = null; Game.Instance.IsPaused = originalPause;
                Require(originalParty.All(actor => ReferenceEquals(Exact(actor.UniqueId), actor)), "Original native actor identity changed during acquisition.");
                if (inventoryPath != null) Require(Hash(inventoryPath) == inventoryHash, "Source native inventory changed before terminal evidence.");
            }
            Require(world.Registry.Owners.Length == 0 && world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted &&
                pair.Relationship.PairOwner == null && pair.Relationship.Runtime.NoPreparedPairResidue) && !controls.HasUnsettledRelationshipTransition &&
                !world.UnifiedTurn.HasUnsettledPreparation, "Ordinary cleanup has unexpected mounted ownership; no acquired actor is removed.");
            cleanupProven = true; evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["acquiredNativeStateRetained"] = true,
                ["nativeBuilderClosed"] = Game.Instance.UI.CharacterBuildController.LevelUpController == null,
                ["nativeWorkerSettled"] = !NativeBusy(), ["manualRefunds"] = 0, ["manualFactRemovals"] = 0, ["manualCompanionRemovals"] = 0,
                ["authority"] = AuthoritySnapshot(), ["after"] = PartySnapshot(), ["selectionRestored"] = true, ["pauseRestored"] = captured ? Game.Instance.IsPaused == originalPause : true };
            evidence["acceptedChoiceCount"] = choices.Count(row => (string)row["event"] == "native-choice");
            evidence["nativeCommitCount"] = choices.Count(row => (string)row["event"] == "native-level-committed");
            Pass("C7-ordinary-cleanup"); evidence["elapsedSeconds"] = clock.Elapsed.TotalSeconds;
            RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors); completed = true;
        }
        private void Retain(string reason)
        {
            if (clock.Elapsed.TotalSeconds - cleanupStart <= 45 || pendingWritten) return;
            Fail("C7-ordinary-cleanup", reason + " Cleanup deadline records failure but retains native custody.");
            var pending = new JObject { ["reason"] = reason, ["cleanupCustodyRetained"] = true, ["cleanupPendingAtHostBoundary"] = true, ["nativeObserver"] = probe?.Capture(),
                ["choices"] = choices.DeepClone(), ["authority"] = AuthoritySnapshot() };
            RuntimeLoaderInventory.WriteArtifact(request, PendingFileName, PendingKind, "FAIL", pending, errors); pendingWritten = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanup) { Fail("C7-ordinary-interrupted", "Ordinary acquisition interrupted before terminal evidence."); BeginCleanup(); }
            if (!completed) PollCleanup(); return completed && cleanupProven;
        }
        public void Dispose()
        {
            if (disposed) return;
            if (!DrainForHost()) throw new InvalidOperationException("Ordinary native fixture retains asynchronous cleanup custody.");
            disposed = true;
        }
        private void Pass(string row)
        { if (results.All(result => result.Name != row)) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "PASS", AssertionPassCount = 1, Errors = Array.Empty<string>() }); }
        private void Fail(string row, string error)
        {
            if (errors.Contains(error)) return; errors.Add(error);
            var result = results.SingleOrDefault(value => value.Name == row);
            if (result == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { error } });
            else { result.Status = "FAIL"; result.AssertionFailCount++; result.Errors = result.Errors.Concat(new[] { error }).ToArray(); }
        }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static string Hash(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
