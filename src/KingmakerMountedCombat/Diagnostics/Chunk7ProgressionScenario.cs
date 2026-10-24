using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // One exact native operation against a qualified disposable two-owner save.
    // The native session owns real choices, costs, rest and autosave callbacks.
    // This outer owner observes cold restoration and drains the remaining world.
    internal sealed partial class Chunk7ProgressionScenario : IDisposable
    {
        internal const string EvidenceFileName = "chunk7-progression-evidence.json";
        internal const string EvidenceKind = "chunk7-progression-evidence";
        internal const string PendingFileName = "chunk7-progression-cleanup-pending.json";
        internal const string PendingKind = "chunk7-progression-cleanup-pending";
        private readonly RuntimeRequest request;
        private readonly Chunk7ProgressionSelection selection;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly PersistenceSaveAuthorization authority;
        private readonly MountedWorldService world;
        private readonly MountedPersistenceService persistence;
        private readonly HorseCompanionBlueprintService horses;
        private readonly int generation;
        private readonly string root;
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly JObject evidence = new JObject();
        private readonly List<string> errors = new List<string>();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly HashSet<UnitCommand> commands = new HashSet<UnitCommand>();
        private NativeArchiveRestorationProbe restoration;
        private Chunk7NativeProgressionSession session;
        private Chunk7NativeWorldScope scope;
        private Game beforeLoadGame;
        private UnitEntityData rider, mount, controlRider, controlMount;
        private UnitEntityData[] actors, originalSelection;
        private object targetDescriptor, targetView, petDescriptor, petView;
        private MovementCaptureOverlayLease ummLease;
        private bool beforeLoad, captured, pause, cleanup, settled, completed, disposed, pending;
        private double cleanupStart;
        private string sourceEvidencePath, inventoryPath;
        private long sourceEvidenceLength, sourceEvidenceTicks, inventoryLength, inventoryTicks;
        private int coldFirstFrame;
        private JObject coldInitial;
        private JObject coldExpected;
        internal static bool SupportsScenario(string scenario) => scenario == Chunk7ProgressionSelection.Scenario;
        internal bool IsCompleted => completed;
        internal IReadOnlyList<string> Errors => errors;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;

        internal Chunk7ProgressionScenario(RuntimeRequest request, RuntimeSaveAuthorization authorization,
            PersistenceSaveAuthorization authority, GameMountedRelationshipService relationship,
            MountedPersistenceService persistence, HorseCompanionBlueprintService horses)
        {
            if (request?.Progression == null || request.Progression.Validate(request.Scenario, request.RunId).Count != 0 || request.PersistenceLoad == null)
                throw new ArgumentException("An exact native progression source and choice selection are required.");
            this.request = request; selection = request.Progression;
            ownedProgression = selection.IsOwned;
            if (ownedProgression) ownedSelectionKey = Chunk7EvidenceDto.Request(selection).ToString(Newtonsoft.Json.Formatting.None);
            this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            world = relationship?.World ?? throw new ArgumentNullException(nameof(relationship));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.horses = horses ?? throw new ArgumentNullException(nameof(horses));
            generation = authorization.AuthorityGeneration;
            root = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(request.EvidenceRoot)), "runtime-staging", "persistence-" + request.RunId, "Saved Games");
        }
        internal void BeginBeforeLoad()
        {
            Require(!beforeLoad && Game.Instance?.CurrentlyLoadedArea == null && NativeMountedSaveFormat.ArchiveNativeSupportEnabled &&
                NativeMountedSaveFormat.SelectedWriterSchema == 3 && GameMountedRelationshipService.NativePairCapacity == 6,
                "Progression cold observation must precede native load under explicit schema 3/six-pair capacity.");
            RequireAuthority(true); beforeLoadGame = Game.Instance; beforeLoad = true;
            restoration = new NativeArchiveRestorationProbe(persistence, world);
            restoration.Install();
        }
        internal void Update()
        {
            if (disposed || completed) return;
            try
            {
                if (cleanup) { PollCleanup(); return; }
                Require(beforeLoad && elapsed.Elapsed.TotalSeconds <= 900, "Progression exceeded its bounded native operation window.");
                if (Busy()) return;
                RequireAuthority(false);
                if (!captured)
                {
                    if (persistence.ArchiveRestorePhase != "all-pair-presentation-restored")
                    { Require(elapsed.Elapsed.TotalSeconds <= 180, "Actual all-pair native restoration did not become ready: " + persistence.Feedback); return; }
                    if (selection.IsLevelCold) StartLevelCold(); else if (selection.IsColdOnly) StartCold(); else Start();
                }
                RequireScope();
                if (selection.IsLevelCold) { TickLevelCold(); return; }
                if (selection.IsField) { TickField(); return; }
                if (selection.IsColdOnly) { TickCold(); return; }
                session.Tick();
                if (!session.IsComplete) return;
                evidence["session"] = session.Capture();
                Require(session.Succeeded, "Native progression did not complete its real choices, effects and custody proof.");
                if (selection.Case == Chunk7ProgressionSelection.SmallLevelCase)
                    evidence["nativeLevelOutcome"] = CaptureLevelNativeOutcome();
                Pass("C7-progression-native-operation"); BeginCleanup();
            }
            catch (Exception error) { CaptureOwnedRosterFailure(error); Fail("C7-progression-runtime", error.ToString()); BeginCleanup(); }
        }
        private void Start()
        {
            Require(Game.Instance.CurrentMode == GameModeType.Default && !Game.Instance.Player.IsInCombat && !Game.Instance.CutsceneLock &&
                !Game.Instance.IsModeActive(GameModeType.Dialog) && (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "Progression requires exact idle native exploration without an unexpected dialog.");
            ReadSources();
            var archive = persistence.LoadedArchive;
            Require(archive != null && archive.SchemaVersion == 3 && archive.CampaignId == request.SelectedChunk7Fixture.GameId &&
                archive.AreaId == request.SelectedChunk7Fixture.Area && archive.Pairs.Length == 2 && archive.Participations.Length == 0 && archive.Combat == null &&
                persistence.ArchiveRestorePhase == "all-pair-presentation-restored" && persistence.SemanticRestoreCount == 4 &&
                persistence.PresentationRestoreCount == 2 && persistence.ArchivePostLoadObservedCount == 4 &&
                world.NativeControls.NativeCastRequestCount == 0 && persistence.SnapshotCount == 0 && persistence.CompletedSaveCount == 0,
                "Native progression source has not actually restored both exact owners once.");
            rider = Exact(selection.ActorId); mount = Exact(selection.PetId);
            controlRider = Exact(selection.ControlActorId); controlMount = Exact(selection.ControlPetId);
            actors = new[] { rider, mount, controlRider, controlMount };
            Require(actors.All(value => value.IsInState && value.IsInGame && value.View?.AgentASP != null) &&
                world.Registry.Owners.Length == 2 && world.Registry.Owners.All(value => value.State == MountedPairOwnershipState.Active),
                "Native progression requires four real field actors and exactly two active owners.");
            foreach (var saved in archive.Pairs)
            {
                var actor = Exact(saved.Rider.Id); var pet = Exact(saved.Mount.Id); var owner = world.Registry.FindActive(actor);
                Require(owner != null && owner.Id == Guid.Parse(saved.PairId) && owner.Generation == saved.Generation &&
                    ReferenceEquals(owner.Mount, pet) && ReferenceEquals(owner.Boundary.PairOwner, owner) &&
                    owner.Boundary.State == RelationshipState.Mounted && owner.Boundary.MountedPairGeneration == saved.Generation &&
                    ReferenceEquals(actor.Descriptor.Pet, pet) && ReferenceEquals(pet.Descriptor.Master.Value, actor),
                    "Restored owner/generation, field relationship or reciprocal native pet differs from its source.");
            }
            Require(archive.Pairs.Count(value => value.ProfileId == (selection.IsSmallOwned ? "small-default-halfling-horse-v1" : SupportedMountedProfiles.Horse.Id) && value.Rider.Id == rider.UniqueId && value.Mount.Id == mount.UniqueId) == 1 &&
                archive.Pairs.Count(value => value.ProfileId == (ownedProgression ? SupportedMountedProfiles.Horse.Id : SupportedMountedProfiles.Mammoth.Id) && value.Rider.Id == controlRider.UniqueId && value.Mount.Id == controlMount.UniqueId) == 1,
                "Progression substituted a profile or target/control actor role.");
            var ids = actors.Select(value => value.UniqueId).ToArray();
            Require(restoration != null && restoration.Complete && restoration.Count("prepare-before", ids) == 0 && restoration.Count("round-before", ids) == 0 &&
                restoration.Count("restore-debt-before", ids) == 4 && restoration.Count("restore-debt-after", ids) == 4,
                "Progression cold restore omitted its before-load window or invented native preparation.");
            AssertRestoredDebt(archive);
            var nativeSlots = world.NativeControls.CapturePersistentSlots();
            Require(nativeSlots.Length == archive.Slots.Length && archive.Slots.All(saved => nativeSlots.Count(slot =>
                slot.ActorId == saved.ActorId && slot.Index == saved.Index && slot.Kind == saved.Kind) == 1), "Restored native control slots changed.");
            evidence["coldRestoration"] = restoration.Capture();
            restoration.Dispose(); restoration = null;
            evidence["loadedArchive"] = JObject.FromObject(archive, MountedSaveCodec.CreateSerializer());
            evidence["loadedDescriptor"] = Chunk7EvidenceDto.Request(request.PersistenceLoad);
            evidence["beforeOwners"] = Owners(); evidence["processId"] = Process.GetCurrentProcess().Id;
            evidence["selection"] = Chunk7EvidenceDto.Request(selection); evidence["authorityBefore"] = AuthoritySnapshot();
            targetDescriptor = rider.Descriptor; targetView = rider.View; petDescriptor = mount.Descriptor; petView = mount.View;
            scope = new Chunk7NativeWorldScope(request, Game.Instance.Player.AllCharacters.Concat(Game.Instance.Player.ControllableCharacters)
                .Where(value => value != null && !ReferenceEquals(value, rider) && !ReferenceEquals(value, mount)).Distinct());
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray(); pause = Game.Instance.IsPaused; captured = true;
            RequireBodyPins();
            if (ownedProgression) BindOwnedRoster();
            if (selection.Case == Chunk7ProgressionSelection.SmallLevelCase || selection.IsMediumOwned)
                PreparePaidExperienceFixture();
            MovementScreenshotCaptureCoordinator.AcquireClosedUmmLeaseRetained(out ummLease);
            Game.Instance.IsPaused = false;
            var pins = selection.WritesSave ? new Chunk7OrdinaryBuildPins(request.RunId, selection.InventoryArtifactSha256,
                selection.OperationCase == Chunk7ProgressionSelection.LevelCase ? Chunk7OrdinaryBuildMode.ExistingHorseLevels :
                selection.OperationCase == Chunk7ProgressionSelection.RetrainHorseCase ? Chunk7OrdinaryBuildMode.NativeRespecHorse : Chunk7OrdinaryBuildMode.NativeRespecMammoth,
                selection.RaceGuid, selection.PresetGuid, selection.SkeletonName, selection.Gender == "Female" ? Gender.Female : Gender.Male,
                selection.LeftHanded ?? false, selection.MinimumRangerLevel, selection.IsSmallOwned ? Size.Small : Size.Medium) : null;
            session = new Chunk7NativeProgressionSession(request, selection.NativeCase, pins, rider, mount, authorization, authority, world, persistence, horses);
            session.Start(); Pass("C7-progression-cold-source");
        }
        private void PreparePaidExperienceFixture()
        {
            var medium = selection.IsMediumOwned;
            // This is disclosed eligibility setup, before the ordinary native
            // level-up operation. It is never counted as a level choice or grant.
            var game = Game.Instance;
            var builder = game.UI?.CharacterBuildController;
            Require(builder != null && !builder.HideInterface && builder.LevelUpController == null && builder.Unit == null &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown) &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled &&
                authority.MatchesProgressionManualPlan(request, game, game.SaveManager) && game.SaveManager.IsSaveAllowed(),
                "Paid native eligibility setup requires idle native choices and an available exact one-Manual plan before XP changes.");
            var sourceActor = evidence["source"]?["inventoryActor"] as JObject;
            var progression = rider.Descriptor.Progression;
            var table = Kingmaker.Blueprints.Root.BlueprintRoot.Instance.Progression.XPTable;
            var threshold = table.GetBonus(medium ? 10 : progression.CharacterLevel + 1);
            var grant = selection.FixtureExperienceGrant.Value;
            var beforeXp = progression.Experience;
            var beforeLevel = progression.CharacterLevel;
            var petXp = mount.Descriptor.Progression.Experience;
            var petLevel = mount.Descriptor.Progression.CharacterLevel;
            var controlXp = controlRider.Descriptor.Progression.Experience;
            var controlPetXp = controlMount.Descriptor.Progression.Experience;
            var money = Game.Instance.Player.Money;
            var snapshots = persistence.SnapshotCount;
            var writes = persistence.CompletedSaveCount;
            var casts = world.NativeControls.NativeCastRequestCount;
            Require(sourceActor != null && beforeLevel == 4 && beforeXp == 9000 &&
                (!medium || threshold == 105000) && (int)sourceActor["characterLevel"] == beforeLevel &&
                (int)sourceActor["experience"] == beforeXp && Game.Instance.Player.ExperienceRatePercent == 100 &&
                threshold > beforeXp && threshold - beforeXp == grant && !Kingmaker.UnitLogic.Class.LevelUp.LevelUpController.CanLevelUp(rider.Descriptor) &&
                !Busy() && rider.Commands.Empty && mount.Commands.Empty && controlRider.Commands.Empty && controlMount.Commands.Empty,
                "Paid native eligibility setup lacks its exact unready level-four source and bounded XP shortfall.");
            Chunk7AssistedFixtureNativeBoundary.RequireMethod(typeof(Kingmaker.UnitLogic.UnitProgressionData),
                "GainExperience", 0x06001F80, typeof(void), new[] { typeof(int), typeof(bool) });
            progression.GainExperience(grant, false);
            Require(progression.Experience == threshold && progression.CharacterLevel == beforeLevel &&
                Kingmaker.UnitLogic.Class.LevelUp.LevelUpController.CanLevelUp(rider.Descriptor) &&
                mount.Descriptor.Progression.Experience == petXp && mount.Descriptor.Progression.CharacterLevel == petLevel &&
                controlRider.Descriptor.Progression.Experience == controlXp &&
                controlMount.Descriptor.Progression.Experience == controlPetXp &&
                Game.Instance.Player.Money == money && persistence.SnapshotCount == snapshots &&
                persistence.CompletedSaveCount == writes && world.NativeControls.NativeCastRequestCount == casts &&
                world.Registry.FindActive(rider)?.Mount == mount && world.Registry.FindActive(controlRider)?.Mount == controlMount,
                "Bounded native XP eligibility setup changed another actor, owner, resource or save boundary.");
            evidence["fixtureExperienceSetup"] = new JObject {
                ["contract"] = medium ? "medium-native-mammoth-eligibility-setup-v1" : "small-native-level-eligibility-setup-v1",
                ["methodToken"] = "06001F80",
                ["methodModuleMvid"] = "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7",
                ["sourceXp"] = beforeXp, ["sourceLevel"] = beforeLevel, ["nextThreshold"] = threshold,
                ["nativeGrant"] = grant, ["afterXp"] = progression.Experience, ["afterLevel"] = progression.CharacterLevel,
                ["petXpBefore"] = petXp, ["petXpAfter"] = mount.Descriptor.Progression.Experience,
                ["petLevelBefore"] = petLevel, ["petLevelAfter"] = mount.Descriptor.Progression.CharacterLevel,
                ["controlXpBefore"] = controlXp, ["controlXpAfter"] = controlRider.Descriptor.Progression.Experience,
                ["controlPetXpBefore"] = controlPetXp, ["controlPetXpAfter"] = controlMount.Descriptor.Progression.Experience,
                ["moneyBefore"] = money, ["moneyAfter"] = Game.Instance.Player.Money,
                ["snapshotsBefore"] = snapshots, ["snapshotsAfter"] = persistence.SnapshotCount,
                ["completedSavesBefore"] = writes, ["completedSavesAfter"] = persistence.CompletedSaveCount,
                ["castsBefore"] = casts, ["castsAfter"] = world.NativeControls.NativeCastRequestCount,
                ["sourceInitiallyUnready"] = true, ["targetReadyAfterGrant"] = true,
                ["ordinaryLevelChoicesStarted"] = false };
        }
        private void ReadSources()
        {
            sourceEvidencePath = Path.Combine(Path.GetDirectoryName(root), Chunk7ProgressionSelection.SourceEvidenceLeaf);
            inventoryPath = Path.Combine(Path.GetDirectoryName(root), Chunk7ProgressionSelection.InventoryLeaf);
            var source = ReadPinned(sourceEvidencePath, selection.SourceArtifactSha256, out sourceEvidenceLength, out sourceEvidenceTicks);
            var inventory = ReadPinned(inventoryPath, selection.InventoryArtifactSha256, out inventoryLength, out inventoryTicks);
            foreach (var item in new[] { source, inventory })
                Require((string)item["status"] == "PASS" && item["errors"] is JArray itemErrors && itemErrors.Count == 0 &&
                    (string)item["commit"] == request.Commit && (string)item["dllSha256"] == request.DllSha256 && (string)item["dllMvid"] == request.DllMvid,
                    "Staged native source/inventory is not a same-candidate immutable PASS.");
            if (selection.IsPaidOwned)
            {
                ReadPaidOwnedSources(source, inventory); return;
            }
            if (ownedProgression)
            {
                ReadOwnedSources(source, inventory); return;
            }
            Require((string)source["runId"] == selection.SourceRunId && (string)source["scenario"] == Chunk7PersistenceScenario.SaveScenario &&
                (string)source["evidenceKind"] == Chunk7PersistenceScenario.EvidenceKind && (string)source["observations"]?["checkpoint"] == "multi-exploration-manual" &&
                (string)source["observations"]?["nativeWrite"]?["sha256"] == selection.SourceArchiveSha256 &&
                (string)inventory["runId"] == selection.InventoryRunId && (string)inventory["scenario"] == Chunk7WorldScenarioEngine.ScenarioName &&
                (string)inventory["evidenceKind"] == Chunk7WorldScenarioEngine.EvidenceKind,
                "Native progression requires its exact Manual300 source and original world inventory.");
            var loaded = JObject.FromObject(persistence.LoadedArchive, MountedSaveCodec.CreateSerializer());
            Require(JToken.DeepEquals(source["observations"]["nativeWrite"]["archive"], loaded), "Production-restored archive differs from its caller-pinned native source graph.");
            var party = inventory["observations"]?["nativeProfileInventory"]?["party"] as JArray;
            Require(party != null && party.Children<JObject>().Count(value => (string)value["id"] == selection.ActorId) == 1,
                "Exact native progression target was not observed in the declared inventory.");
            evidence["source"] = new JObject { ["sourceRunId"] = selection.SourceRunId, ["sourceArtifactSha256"] = selection.SourceArtifactSha256,
                ["sourceArchiveSha256"] = selection.SourceArchiveSha256, ["inventoryRunId"] = selection.InventoryRunId,
                ["inventoryArtifactSha256"] = selection.InventoryArtifactSha256, ["sameCandidate"] = true,
                ["nativeSourceGraphExact"] = true, ["inventoryActor"] = party.Children<JObject>().Single(value => (string)value["id"] == selection.ActorId).DeepClone() };
        }
        private void RequireBodyPins()
        {
            var body = NativeSmallRiderBody.Capture(rider, mount);
            var size = selection.IsSmallOwned ? Size.Small : Size.Medium;
            Require(body != null && body.OriginalSizeOrdinal == (int)size && body.EffectiveSizeOrdinal == (int)size &&
                !body.RiderPolymorphed && !body.MountPolymorphed, "The selected progression target lacks its exact supported native body.");
            if (selection.IsRespec)
                Require(body.NativeRigAvailable && body.NativeRaceAndPresetIdentityExact && body.NativeSkeletonIsExactPreset &&
                    body.RaceGuid == selection.RaceGuid && body.PresetGuid == selection.PresetGuid && body.SkeletonName == selection.SkeletonName &&
                    body.Gender == selection.Gender && body.LeftHanded == selection.LeftHanded,
                    "Retraining body choices differ from the actual loaded native body/preset/skeleton.");
            evidence["nativeBodyBefore"] = JObject.Parse(SmallRiderBodyFingerprint.Capture(body));
        }
        private void RequireAuthority(bool loadingAdmission)
        {
            if (ownedProgression) Require(request.OwnedPairs == null && request.Scenario == Chunk7ProgressionSelection.Scenario &&
                selection.Validate(request.Scenario, request.RunId).Count == 0 && selection.ValidateOwnedDescriptor(request.PersistenceLoad, request.Fixture).Count == 0 &&
                Chunk7EvidenceDto.Request(selection).ToString(Newtonsoft.Json.Formatting.None) == ownedSelectionKey,
                "Owned progression selection/source changed after its exact admission.");
            Require(authorization.IsActive && authorization.AuthorityGeneration == generation && authorization.IsExactPersistenceScope(authority) &&
                NativePersistenceIsolation.IsExactAuthority(authority) && authority.Root == root && Game.Instance?.SaveManager?.SavePath == root &&
                NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(root), "Areas") &&
                (selection.WritesManual ? authority.MatchesProgressionManualPlan(request, Game.Instance, Game.Instance.SaveManager) :
                    authority.MatchesProgressionPlan(request.PersistenceLoad, selection.WritesAutosave, RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto))) &&
                authority.MaximumNativeLoadRequests == 1 && authority.AdmittedNativeLoadRequests == (loadingAdmission ? 0 : 1) &&
                authorization.AuthorizedLoadCount == (loadingAdmission ? 0 : 1) &&
                authorization.AuthorizedWriteCount <= selection.ExpectedNativeWriteCount && authority.AdmittedNativeWriteRequests <= selection.ExpectedNativeWriteCount &&
                authorization.UnauthorizedLoadCount == 0 && authorization.UnauthorizedWriteCount == 0 && authorization.FatalViolationCount == 0 &&
                authorization.SuppressedWorkingWriteCount == 0 && authorization.BaselineLoadRequestCount == 0 && !authorization.IsOneShotWorkingWriteSuppressionArmed,
                "Progression lost its exact isolated native source/write plan, load cap or authority generation.");
            if (!loadingAdmission)
                Require(ReferenceEquals(beforeLoadGame, Game.Instance) && Game.Instance.Player?.GameId == request.SelectedChunk7Fixture.GameId &&
                    Game.Instance.Player.MainCharacter.Value?.CharacterName == request.SelectedChunk7Fixture.GameName &&
                    Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe == request.SelectedChunk7Fixture.Area,
                    "Progression lost its originally admitted native game/campaign/area.");
        }
        private void RequireScope()
        {
            RequireAuthority(false); scope?.RequireCurrent();
            if (session != null) session.AssertOwnedScope();
            if (ownedProgression && ownedParty != null) RequireOwnedRoster(false);
            if (captured && (session == null || !session.HasStarted)) Require(ReferenceEquals(rider.Descriptor, targetDescriptor) && ReferenceEquals(rider.View, targetView) &&
                ReferenceEquals(mount.Descriptor, petDescriptor) && ReferenceEquals(mount.View, petView), "Progression target changed before native session ownership.");
            if (sourceEvidencePath != null)
            {
                var s = new FileInfo(sourceEvidencePath); var i = new FileInfo(inventoryPath);
                Require(s.Exists && s.Length == sourceEvidenceLength && s.LastWriteTimeUtc.Ticks == sourceEvidenceTicks &&
                    i.Exists && i.Length == inventoryLength && i.LastWriteTimeUtc.Ticks == inventoryTicks, "Native source evidence changed during progression.");
            }
        }
        private bool Busy() => LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() || NativePersistenceIsolation.HasPendingWrites ||
            persistence.AreaTransitionPending || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence || world.SerializationBusy || world.ArchiveWorkerRunning();
        private void AssertRestoredDebt(MountedArchiveData archive)
        {
            var seconds = Math.Max(0, (Game.Instance.TimeController.GameTime.Ticks - archive.GameTimeTicks) / (double)TimeSpan.TicksPerSecond);
            foreach (var prior in archive.Pairs.SelectMany(value => new[] { value.Rider, value.Mount }))
            {
                var after = MountedPersistenceService.CaptureActor(Exact(prior.Id));
                var a = new[] { prior.Standard, prior.Move, prior.Swift, prior.Initiative, prior.Reaction };
                var b = new[] { after.Standard, after.Move, after.Swift, after.Initiative, after.Reaction };
                Require(prior.ReactionsRemaining == after.ReactionsRemaining && prior.LastSurpriseTicks == after.LastSurpriseTicks &&
                    Enumerable.Range(0, a.Length).All(index => b[index] <= a[index] + 0.001 && b[index] + seconds * 1.1 + 0.001 >= a[index]),
                    "Restored native debt exceeds genuine elapsed-time decay or replays/refunds entitlement.");
            }
        }
        private void BeginCleanup() { if (cleanup) return; cleanup = true; cleanupStart = elapsed.Elapsed.TotalSeconds; }
        private void PollCleanup()
        {
            if (completed) return;
            try
            {
                if (Busy() || persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused) { Retain("Native load/save worker retains progression custody."); return; }
                if (captured) RequireScope();
                else Require(beforeLoadGame == null || ReferenceEquals(beforeLoadGame, Game.Instance), "Progression pre-admission game changed during observer cleanup.");
                if (restoration != null) { evidence["coldRestoration"] = restoration.Capture(); restoration.Dispose(); restoration = null; }
                if (session != null)
                {
                    if (!session.TryDrain()) { evidence["session"] = session.Capture(); Retain("Native progression session retains choices/UI/observer/target custody."); return; }
                    evidence["session"] = session.Capture(); session.Dispose();
                }
                if (captured)
                {
                    RequireScope();
                    var pairClean = true;
                    if (ownedProgression) ownedWorldCleanupStarted = true;
                    foreach (var pair in world.Pairs)
                    {
                        if (pair.Relationship.PairOwner == null && pair.Relationship.State == RelationshipState.Unmounted && pair.Relationship.Runtime.NoPreparedPairResidue) continue;
                        var owner = pair.Relationship.PairOwner;
                        // Registry keys use GUID "N" while the admitted selection
                        // uses GUID "D". Keep cleanup bound to the exact four
                        // loaded actor objects, including the surviving control.
                        Require(owner != null &&
                            (ReferenceEquals(owner.Rider, rider) && ReferenceEquals(owner.Mount, mount) ||
                             ReferenceEquals(owner.Rider, controlRider) && ReferenceEquals(owner.Mount, controlMount)),
                            "Progression cleanup refuses an undeclared owner.");
                        CaptureCommands(owner.Rider); CaptureCommands(owner.Mount);
                        if (owner.Rider?.Commands == null || owner.Mount?.Commands == null ||
                            !owner.Rider.Commands.Raw.All(CommandSettled) || !owner.Mount.Commands.Raw.All(CommandSettled))
                        { pairClean = false; continue; }
                        try
                        {
                            pair.Combat.Cancel("Selected native progression scenario cleanup");
                            if (!pair.Combat.HasActiveCommand && !pair.Combat.HasActiveGroundMovement && !pair.Combat.HasChargeOwnership &&
                                !world.NativeControls.HasUnsettledShellFor(pair.Relationship))
                                Require(pair.Relationship.Dismount(CleanupTrigger.Exception).Succeeded, "Progression relationship cleanup remains unfinished.");
                            else pairClean = false;
                        }
                        catch (Exception error) { Fail("C7-progression-cleanup", error.ToString()); pairClean = false; }
                    }
                    if (!pairClean || !CommandsSettled() || world.Registry.Owners.Length != 0 || world.NativeControls.HasUnsettledRelationshipTransition ||
                        world.UnifiedTurn.HasUnsettledPreparation || world.Pairs.Any(value => value.Relationship.State != RelationshipState.Unmounted ||
                        value.Relationship.PairOwner != null || !value.Relationship.Runtime.NoPreparedPairResidue))
                    { Retain("Remaining native mounted world still owns commands, preparation or relationship cleanup."); return; }
                    RequireScope();
                    if (ownedProgression)
                    {
                        if (!OwnedRemovedPetSettled()) { Retain("Observed native respec still owns its pending old-pet entity removal."); return; }
                        RequireOwnedRoster(true);
                        Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown,
                            "An unexpected dialog retains owned progression presentation custody.");
                        evidence["ownedRosterAfter"] = CaptureOwnedRoster("settled");
                    }
                    var remainingSelection = originalSelection.Where(value => value.IsInState && value.View != null &&
                        (value.UniqueId != selection.ActorId || selection.OperationCase != Chunk7ProgressionSelection.DismissCase)).ToArray();
                    SelectionManager.Instance.MultiSelect(remainingSelection.Select(value => value.View), false);
                    Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(remainingSelection), "Owned original surviving selection did not restore exactly.");
                    ummLease?.Dispose(); ummLease = null; Game.Instance.IsPaused = pause;
                    Require(Hash(sourceEvidencePath) == selection.SourceArtifactSha256 && Hash(inventoryPath) == selection.InventoryArtifactSha256 &&
                        Hash(Path.Combine(root, request.PersistenceLoad.FileName)) == selection.SourceArchiveSha256, "Read-only progression inputs changed.");
                }
                evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["remainingWorldCleanupProven"] = captured,
                    ["workersSettled"] = true, ["nativeProcessesSettled"] = CommandsSettled(), ["ownersRemaining"] = world.Registry.Owners.Length,
                    ["selectionRestoredToSurvivingActors"] = captured, ["pauseRestored"] = !captured || Game.Instance.IsPaused == pause,
                    ["nativeAcquiredStateRetained"] = true, ["manualMoneyWrites"] = 0, ["manualXpWrites"] = 0,
                    ["manualCompanionRemovals"] = 0, ["manualNativeRestCalls"] = 0, ["manualNativeTimeAdvances"] = 0,
                    ["sourceUnchanged"] = captured, ["authority"] = AuthoritySnapshot() };
                evidence["elapsedSeconds"] = elapsed.Elapsed.TotalSeconds;
                if (captured) Pass("C7-progression-cleanup"); settled = true;
                RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 && captured ? "PASS" : "FAIL", evidence, errors);
                completed = true;
            }
            catch (Exception error) { CaptureOwnedRosterFailure(error); Fail("C7-progression-cleanup", error.ToString()); Retain("Progression cleanup guard or release refused: " + error.Message); }
        }
        private void CaptureCommands(UnitEntityData actor) { if (actor?.Commands != null) foreach (var command in actor.Commands.Raw) if (command != null) commands.Add(command); }
        private bool CommandsSettled() => commands.All(CommandSettled);
        private static bool CommandSettled(UnitCommand command) => command == null || command.IsFinished &&
            (!(command is UnitUseAbility ability) || ability.ExecutionProcess == null || ability.ExecutionProcess.IsEnded);
        private JObject Owners() => new JObject { ["owners"] = new JArray(world.Registry.Owners.Select(value => new JObject {
            ["ownerId"] = value.Id.ToString("N"), ["generation"] = value.Generation, ["riderId"] = value.RiderId,
            ["mountId"] = value.MountId, ["state"] = value.State.ToString() })) };
        private JObject AuthoritySnapshot() => new JObject { ["root"] = root, ["generation"] = generation, ["exactScope"] = authorization.IsExactPersistenceScope(authority),
            ["maximumNativeLoads"] = authority.MaximumNativeLoadRequests, ["maximumNativeWrites"] = authority.MaximumNativeWriteRequests,
            ["admittedNativeLoads"] = authority.AdmittedNativeLoadRequests, ["admittedNativeWrites"] = authority.AdmittedNativeWriteRequests,
            ["authorizedLoads"] = authorization.AuthorizedLoadCount, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
            ["unauthorizedLoads"] = authorization.UnauthorizedLoadCount, ["unauthorizedWrites"] = authorization.UnauthorizedWriteCount,
            ["fatalViolations"] = authorization.FatalViolationCount, ["suppressedWrites"] = authorization.SuppressedWorkingWriteCount };
        private void Retain(string reason)
        {
            if (pending || elapsed.Elapsed.TotalSeconds - cleanupStart <= 45) return;
            Fail("C7-progression-cleanup", reason + " Failure never relinquishes native custody.");
            JToken sessionAtFailure = null;
            try { sessionAtFailure = session?.Capture(); }
            catch (Exception observationError)
            {
                // An optional read-only observation cannot replace the original
                // failure or release native cleanup custody.
                sessionAtFailure = new JObject { ["captureError"] = observationError.GetType().Name };
            }
            RuntimeLoaderInventory.WriteArtifact(request, PendingFileName, PendingKind, "FAIL", new JObject { ["reason"] = reason,
                ["cleanupCustodyRetained"] = true, ["cleanupPendingAtHostBoundary"] = true,
                ["sessionAtFailure"] = sessionAtFailure,
                ["firstOwnedRosterFailure"] = evidence["firstOwnedRosterFailure"]?.DeepClone() }, errors); pending = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanup) { Fail("C7-progression-interrupted", "Native progression interrupted before terminal proof."); BeginCleanup(); }
            if (!completed) PollCleanup(); return completed && settled;
        }
        public void Dispose() { if (disposed) return; if (!DrainForHost()) throw new InvalidOperationException("Native progression custody remains retained."); disposed = true; }
        private UnitEntityData Exact(string id)
        {
            var values = Game.Instance.State.Units.Concat(Game.Instance.Player.AllCharacters).Where(value => value?.UniqueId == id).Distinct().ToArray();
            Require(values.Length == 1, "Exact native progression actor is absent or duplicated: " + id); return values[0];
        }
        private static JObject ReadPinned(string path, string hash, out long length, out long ticks)
        {
            var file = new FileInfo(path); Require(file.Exists && file.Length > 0 && file.Length <= 16 * 1024 * 1024 &&
                (file.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(path) == hash, "Pinned native progression evidence is missing or changed.");
            length = file.Length; ticks = file.LastWriteTimeUtc.Ticks; return JObject.Parse(File.ReadAllText(path));
        }
        private void Pass(string row) { if (results.All(value => value.Name != row)) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "PASS", AssertionPassCount = 1, Errors = Array.Empty<string>() }); }
        private void Fail(string row, string error)
        {
            if (errors.Contains(error)) return; errors.Add(error); var item = results.SingleOrDefault(value => value.Name == row);
            if (item == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { error } });
            else { item.Status = "FAIL"; item.AssertionFailCount++; item.Errors = item.Errors.Concat(new[] { error }).ToArray(); }
        }
        private static string Hash(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
