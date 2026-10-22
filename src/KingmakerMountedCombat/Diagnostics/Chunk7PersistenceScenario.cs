using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Separate from historical one-pair assertions. The first connected rows
    // qualify actual native categories and all-pair cold restoration in exploration.
    // RT/TB spent-state rows require their own declared native checkpoints.
    internal sealed partial class Chunk7PersistenceScenario : IDisposable
    {
        internal const string SaveScenario = "chunk7-persistence-save", LoadScenario = "chunk7-persistence-load";
        internal const string EvidenceFileName = "chunk7-persistence-evidence.json";
        internal const string EvidenceKind = "chunk7-persistence-evidence";
        internal const string ManualName = "KMC_C7_WORLD";
        private readonly RuntimeRequest request;
        private readonly MountedWorldService world;
        private readonly NativeMountedControlService controls;
        private readonly MountedPersistenceService persistence;
        private readonly HorseCompanionBlueprintService horses;
        private readonly DiagnosticSettings settings;
        private readonly IModLogger logger;
        private readonly Stopwatch clock = new Stopwatch();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<string> errors = new List<string>();
        private readonly List<UnitCommand> issued = new List<UnitCommand>();
        private readonly JObject evidence = new JObject();
        private readonly JArray snapshots = new JArray(), inputs = new JArray(), movementRows = new JArray();
        private readonly Dictionary<Guid, long> expectedOwners = new Dictionary<Guid, long>();
        private UnitEntityData[] actors, originalSelection;
        private UnitEntityData firstRider, firstMount, secondRider, secondMount;
        private Feature acquiredHorse, acquiredRank;
        private Chunk7OwnedPairFixture inherited;
        private readonly WorkingFixtureLoader ownedFixtureLoader;
        private bool beforeLoad, started, captured, completed, disposed, cleanup, cleanupActions, callback, originalPause, pendingHostCustody;
        private bool originalPaired, originalMovement, cleanupProven;
        private int repeatedCleanupActionsFrame = -1;
        private readonly HashSet<string> cleanupFaults = new HashSet<string>(StringComparer.Ordinal);
        private double stageStarted;
        private int stage;
        private UnitUseAbility shell;
        private UnitCommand move;
        private UnitEntityData movingActor;
        private Vector3 moveStart, moveGoal;
        private JObject moveRow;
        private MountedArchiveData saved;
        private SaveInfo savedDescriptor;
        private long snapshotBefore;
        private long castBefore;
        private SavedMountedSlot[] slots;
        private NativeModeTransitionProbe mode;
        private NativeArchiveRestorationProbe restoration;
        private MovementCaptureOverlayLease ummLease;
        internal bool IsCompleted => completed;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;
        internal IReadOnlyList<string> Errors => errors;
        private bool Cold => request.SelectedChunk7Scenario == LoadScenario;
        internal static bool SupportsScenario(string value) => RuntimeRequest.IsChunk7PersistenceScenario(value);
        internal static bool SupportsRequest(RuntimeRequest request) => SupportsScenario(request?.SelectedChunk7Scenario);
        internal static bool SupportsCase(string value) => RuntimeRequest.IsChunk7PersistenceCase(value);
        internal static SaveInfo.SaveType NativeType(string value)
        {
            if (!SupportsCase(value)) throw new InvalidOperationException("Unsupported Chunk 7 native persistence checkpoint.");
            if (value == "multi-area-autosave") return SaveInfo.SaveType.Auto;
            return value.EndsWith("-quick", StringComparison.Ordinal) ? SaveInfo.SaveType.Quick :
                value.EndsWith("-auto", StringComparison.Ordinal) ? SaveInfo.SaveType.Auto : SaveInfo.SaveType.Manual;
        }
        internal static string NativeName(SaveInfo.SaveType type) => type == SaveInfo.SaveType.Manual ? ManualName : RuntimePersistenceScenario.SlotName(type);
        // The owner-seed checkpoint producer writes its Manual under its own distinct name; every other selection keeps NativeName.
        internal static string NativeName(SaveInfo.SaveType type, Chunk7OwnedPairsSelection owned) =>
            type == SaveInfo.SaveType.Manual && owned?.IsFourthHorseSave == true ? Chunk7OwnedPairsSelection.FourthHorseManualName :
            type == SaveInfo.SaveType.Manual && (owned?.IsSmallFixtureSave == true || owned?.IsSmallThirdHorseSave == true) ? Chunk7OwnedPairsSelection.SmallFixtureManualName :
            type == SaveInfo.SaveType.Manual && owned?.IsMediumFixtureSave == true ? Chunk7OwnedPairsSelection.MediumFixtureManualName :
            type == SaveInfo.SaveType.Manual && owned?.IsOwnerSeedCheckpointSave == true ? Chunk7OwnedPairsSelection.OwnerSeedCheckpointManualName : NativeName(type);
        internal static string NativeName(SaveInfo.SaveType type, Chunk7OwnedPairsSelection owned, string checkpoint) =>
            type == SaveInfo.SaveType.Manual && (owned?.IsOwnerSeedCheckpointSave == true || owned?.IsSmallFixtureSave == true || owned?.IsSmallThirdHorseSave == true || owned?.IsFourthHorseSave == true || owned?.IsMediumFixtureSave == true) ?
                owned.OwnedManualOutputName(checkpoint) : NativeName(type, owned);
        internal static string NativeLeaf(SaveInfo.SaveType type) => type == SaveInfo.SaveType.Manual ? "Manual_300_" + ManualName + ".zks" : type + "_1.zks";

        internal Chunk7PersistenceScenario(RuntimeRequest request, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, MountedPersistenceService persistence, HorseCompanionBlueprintService horses,
            DiagnosticSettings settings, IModLogger logger, RuntimeSaveAuthorization replayAuthorization = null, PersistenceSaveAuthorization replaySaveAuthority = null, WorkingFixtureLoader ownedFixtureLoader = null)
        {
            if (!SupportsRequest(request) || !SupportsCase(request.PersistenceCase))
                throw new ArgumentException("An exact Chunk 7 persistence request is required.");
            this.request = request; world = relationship.World; this.controls = controls; this.persistence = persistence;
            this.horses = horses; this.settings = settings; this.logger = logger;
            this.replayAuthorization = replayAuthorization; this.replaySaveAuthority = replaySaveAuthority; this.ownedFixtureLoader = ownedFixtureLoader;
            replayAuthorityGeneration = replayAuthorization?.AuthorityGeneration ?? 0;
        }
        // Host calls this immediately before its existing native Working loader.
        internal void BeginBeforeLoad()
        {
            if (beforeLoad || Game.Instance?.CurrentlyLoadedArea != null) throw new InvalidOperationException("Chunk 7 load observation started late.");
            Require(world != null && NativeMountedSaveFormat.ArchiveNativeSupportEnabled &&
                NativeMountedSaveFormat.SelectedWriterSchema == 3 && GameMountedRelationshipService.NativePairCapacity >= 2,
                "Native schema 3 and two-pair capacity must be explicitly selected before this scenario.");
            beforeLoadGame = Game.Instance;
            originalPaired = settings.EnablePairedActivation; originalMovement = settings.EnableUnsafeMovementExperiment;
            InitializeReplayAuthority();
            if (Cold)
            {
                restoration = new NativeArchiveRestorationProbe(persistence, CombatCase || Schema1ResaveCase || OwnedSchema1CurrentCase || RepeatedWorldCase ? world : null,
                    NativeArchiveRestorationProbe.IsDebtDiagnosticRequest(request) ? request : null);
                restoration.Install();
            }
            InstallReplayObservation();
            if (OwnerSeedLoadRefusalCase) { loadRefusalObserver = new Chunk7OwnerSeedLoadProbe(); loadRefusalObserver.Install(); }
            mode = new NativeModeTransitionProbe(TurnBasedCase); mode.DispatchTemporaryValueIfRequired();
            beforeLoad = true;
        }
        internal bool ReapplyModeAfterNativeRefresh()
        {
            RequireInitialLoadGame();
            if (RepeatedWorldCase && replayPending)
            {
                RequireReplayAuthority();
                if (RepeatedAreaCase) nativeScope.RequireTransferWorld();
                return mode?.ReapplyTemporaryCacheAfterNativeRefresh() == true;
            }
            if (nativeScope != null)
            {
                // The owned ordinary transfer is still replacing its area state.
                // It may retain its exact settings lease, but it gains no actor,
                // save or cleanup authority until explicit destination adoption.
                var declaredAreaLoading = AreaAutosaveCase && !Cold && stage == 30 && areaTransferDispatched && !areaScopeAdopted &&
                    (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) ||
                    ManticoreFixtureCase && stage == 92 && manticoreDispatched && !manticoreScopeAdopted;
                if (declaredAreaLoading) nativeScope.RequireTransferWorld();
                else RequireNativeWorld();
            }
            return mode?.ReapplyTemporaryCacheAfterNativeRefresh() == true;
        }
        internal void Update()
        {
            if (completed || disposed) return;
            try
            {
                if (!started) Start();
                if (cleanup) { PollCleanup(); return; }
                var withinDeadline = clock.Elapsed.TotalSeconds <= (RepeatedWorldCase ? 600 : CombatCase || AreaAutosaveCase || ManticoreFixtureCase ? 300 : 180) && clock.Elapsed.TotalSeconds - stageStarted <=
                    (RepeatedWorldCase && stage == 70 ? 450 : CombatCase && (stage == 5 || stage == 20) || AreaAutosaveCase && stage == 30 || ManticoreFixtureCase && stage == 92 ? 150 : 35);
                // The first ordinary Small cold load reached all four native PostLoads
                // but not presentation. On a recurrence, retain the exact read-only
                // boundary that withheld stage 0 before cleanup disposes the world.
                if (!withinDeadline && Cold && stage == 0 && request.OwnedPairs?.PaidFixtureLineage == true)
                {
                    var game = Game.Instance;
                    var feedback = persistence.Feedback;
                    evidence["smallColdReadinessAtDeadline"] = new JObject {
                        ["archivePhase"] = persistence.ArchiveRestorePhase,
                        ["feedback"] = feedback != null && feedback.Length > 512 ? feedback.Substring(0, 512) : feedback,
                        ["restorationPending"] = persistence.CombatRestorationPending,
                        ["semanticRestores"] = persistence.SemanticRestoreCount,
                        ["presentationRestores"] = persistence.PresentationRestoreCount,
                        ["nativePostLoads"] = persistence.ArchivePostLoadObservedCount,
                        ["gameMode"] = game?.CurrentMode.ToString(),
                        ["paused"] = game?.IsPaused,
                        ["nativeLoading"] = LoadingProcess.Instance.IsLoadingInProcess,
                        ["queuedNativeLoads"] = LoadingProcess.Instance.QueuedNames.Count()
                    };
                }
                Require(withinDeadline, "Bounded native persistence deadline at stage " + stage + ".");
                if (UpdateSaveBoundary()) return;
                if (UpdateRepeatedWorld()) return;
                if (UpdateAreaAutosave()) return;
                if (UpdateManticoreFixture()) return;
                if (UpdateLoadRefusal()) return;
                if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) return;
                RequireNativeWorld();
                if (!CombatCase) Require(!Game.Instance.Player.IsInCombat, "Exploration persistence fixture unexpectedly entered combat.");
                switch (stage)
                {
                    case 0:
                        if (Cold)
                        {
                            if (persistence.CombatRestorationPending) return;
                            if (RepeatedWorldCase) { BeginRepeatedWorld(); break; }
                            if (Schema1ResaveCase) { ObserveSchema1Cold(); RequestSave(); Next(40); break; }
                            if (OwnedSchema1CurrentCase) { ObserveOwnedSchema1Current(); Next(42); break; }
                            ObserveCold();
                            if (OwnerSeedLoadRefusalCase) { BeginLoadRefusal(); Next(80); break; }
                            if (OwnedSchema2CurrentCase) ObserveCurrentMigrationParent();
                            if (Schema2ResaveCase)
                            {
                                RequestSave(); Next(41); break;
                            }
                            if (CombatCase) { BeginCombatContinuation(); Next(20); } else Next(10);
                            break;
                        }
                        if (CombatController.IsInTurnBasedCombat()) return;
                        if (OwnerSeedCheckpointSave) { if (!OwnerSeedRestoredStartReady()) return; AdmitOwnerSeedRestoredStart(); break; }
                        if (inherited != null) { inherited.RequirePreserved(); Pass("C7-owned-pairs-inherited-custody"); Next(1); break; }
                        var acquisition = new JObject(); evidence["acquisition"] = acquisition;
                        bool acquired;
                        try { acquired = NativeHorseFixtureAcquisition.Create(secondRider, horses, logger, acquisition); }
                        finally
                        {
                            acquiredHorse = secondRider.Descriptor.GetFact(horses.HorseFeature) as Feature;
                            acquiredRank = secondRider.Descriptor.GetFact(horses.LevelRank) as Feature;
                            secondMount = secondRider.Descriptor.Pet;
                            TrackNativeActors();
                        }
                        Require(acquired, "Native Ranger preview/select/commit fixture acquisition failed.");
                        Next(1); break;
                    case 1:
                        if (secondMount?.View?.AgentASP == null || !secondMount.IsInState) return;
                        Require(secondMount.Blueprint?.AssetGuid == HorseCompanionBlueprintService.UnitGuid &&
                            ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider), "Acquired Horse is not the exact reciprocal pet.");
                        actors = new[] { firstRider, firstMount, secondRider, secondMount };
                        Input(firstRider, firstMount, controls.MountAbility, secondRider, "mount-mammoth"); Next(2); break;
                    case 2:
                        if (!Mounted(firstRider, firstMount) || !InputSettled()) return;
                        FinishInput();
                        if (LegacyPendingCase) { AfterNativeMounts(); break; }
                        if (OwnedSchema1SourceCase) { BeginOwnedSchema1Source(); break; }
                        if (PendingMountSave) { BeginPendingMountSave(); break; }
                        Input(secondRider, secondMount, controls.MountAbility, firstRider, "mount-horse"); Next(3); break;
                    case 3:
                        if (!Mounted(secondRider, secondMount) || !InputSettled()) return;
                        FinishInput(); AfterNativeMounts(); break;
                    case 6:
                        PollOwnerSeedCheckpointPreparation(); break;
                    case 4:
                        if (!callback || NativePersistenceIsolation.HasPendingWrites || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence) return;
                        ObserveSaved();
                        if (OwnedSchema1SourceCase) { BeginCleanup(); break; }
                        if (CombatCase) { BeginCombatContinuation(); Next(20); } else Next(10);
                        break;
                    case 41:
                        if (!callback || NativePersistenceIsolation.HasPendingWrites || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence) return;
                        ObserveSaved(); BeginCombatContinuation(); Next(20); break;
                    case 40:
                        if (!callback || NativePersistenceIsolation.HasPendingWrites || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence) return;
                        ObserveSaved(); BeginCleanup(); break;
                    case 42:
                        AssertOwnedSchema1Owner(); AssertSlots();
                        Move(firstRider, firstMount, firstMount.Position + Vector3.right * 2f, secondRider, "schema1-current-first-pair"); Next(43); break;
                    case 43:
                        AssertOwnedSchema1Owner(); AssertSlots(); if (!MovementSettled()) return;
                        VerifyCurrentMigrationInput();
                        snapshots.Add(Capture("schema1-current-continuation-complete"));
                        Pass("C7-persistence-current-migration-continuation"); BeginCleanup(); break;
                    case 5:
                        if (!checkpoint.Update()) return;
                        evidence["combatCheckpoint"] = checkpoint.Capture();
                        RequestSave(); Next(4); break;
                    case 20:
                        if (!checkpoint.UpdateContinuation()) return;
                        if (CurrentMigrationCase) VerifyCurrentMigrationInput();
                        evidence["combatContinuation"] = checkpoint.Capture();
                        Pass("C7-persistence-native-combat-continuation"); BeginCleanup(); break;
                    case 10:
                        AssertOwners();
                        if (AreaAutosaveCase || Chunk7OwnedPairsSelection.IsManticoreManualCold(request.PersistenceCase))
                            evidence["areaDoorInventory"] = Chunk7NativeDoorProbe.CaptureAreaDoorInventory(new[] { firstMount, secondMount });
                        Move(firstRider, firstMount, AreaAutosaveCase ? SelectAreaContinuationGoal(firstMount, "first-pair") : firstMount.Position + Vector3.right * 2f, secondRider, "first-pair"); Next(11); break;
                    case 11:
                        AssertOwners(); if (!MovementSettled()) return;
                        Move(secondRider, secondMount, AreaAutosaveCase ? SelectAreaContinuationGoal(secondMount, "second-pair") : secondMount.Position + Vector3.forward * 2f, firstRider, "second-pair"); Next(12); break;
                    case 12:
                        AssertOwners(); if (!MovementSettled()) return;
                        Pass("C7-persistence-native-continuation"); snapshots.Add(Capture("continuation-complete"));
                        BeginCleanup(); break;
                }
            }
            catch (Exception error) { Fail("C7-persistence-runtime", error.ToString()); BeginCleanup(); }
        }
        private void Start()
        {
            RequireInitialLoadGame();
            Require(beforeLoad, "Native load occurred without the selected observer/configuration boundary.");
            started = true; clock.Start();
            Require(request.SchemaVersion == RuntimeRequest.SaveBackedSchemaVersion && (request.OwnedPairs != null || request.Fixture.Working.Sha256 == Chunk7WorldScenarioEngine.WorkingHash),
                "Persistence source fixture identity differs.");
            Require(settings.EnablePairedActivation && settings.EnableUnsafeMovementExperiment && !settings.EnableUnifiedMountedTurn &&
                !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay, "Paired production policy differs.");
            nativeScope = new Chunk7NativeWorldScope(request, SelectionManager.Instance.SelectedUnits,
                Cold ? ExpectedArchiveArea : request.SelectedChunk7Fixture.Area);
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray(); originalPause = Game.Instance.IsPaused; captured = true;
            if (OwnedSchema1ColdCase) Game.Instance.IsPaused = true;
            MovementScreenshotCaptureCoordinator.AcquireClosedUmmLeaseRetained(out ummLease);
            evidence["checkpoint"] = request.PersistenceCase; evidence["cold"] = Cold;
            evidence["sourceWorkingSha256"] = request.OwnedPairs == null ? request.Fixture.Working.Sha256 : null;
            evidence["inheritedManualSha256"] = request.OwnedPairs?.SourceArchiveSha256;
            evidence["processId"] = Process.GetCurrentProcess().Id; evidence["snapshots"] = snapshots;
            evidence["inputs"] = inputs; evidence["movements"] = movementRows;
            evidence["nativeProgressionRequiresExternalWorkingRestoration"] = request.OwnedPairs == null;
            evidence["ordinaryRespecProven"] = false; evidence["combatDebtProof"] = CombatCase;
            evidence["nativeSchema"] = NativeMountedSaveFormat.SelectedWriterSchema;
            evidence["nativeCapacity"] = GameMountedRelationshipService.NativePairCapacity;
            if (AreaAutosaveCase) evidence["areaContinuationPlans"] = areaContinuationPlans;
            if (!Cold && OwnerSeedCheckpointSave)
            {
                // The restored mounted graph binds now and is verified at stage 0, once restoration settles.
                BindOwnedFixture(false);
                Require(!controls.HasUnsettledRelationshipTransition, "Owner-seed checkpoint producer found an unsettled relationship transition.");
                snapshotBefore = persistence.SnapshotCount;
            }
            else if (!Cold)
            {
                if (request.OwnedPairs != null)
                {
                    BindOwnedFixture(false);
                }
                else
                {
                    var party = Chunk7NativeFixture.ValidateWorking(request, horses);
                    firstRider = party.Single(x => x.UniqueId == Chunk7WorldScenarioEngine.MammothRiderId);
                    firstMount = party.Single(x => x.UniqueId == Chunk7WorldScenarioEngine.MammothId);
                    secondRider = party.Single(x => x.UniqueId == Chunk7WorldScenarioEngine.HorseRiderId);
                    TrackNativeActors();
                }
                Require(world.Registry.Owners.Length == 0 && world.Pairs.All(x => x.Relationship.State == RelationshipState.Unmounted &&
                    x.Relationship.Runtime.NoPreparedPairResidue) && !controls.HasUnsettledRelationshipTransition,
                    "Fixture already owns a relationship or unfinished command.");
                snapshotBefore = persistence.SnapshotCount;
            }
            Next(0);
        }
        private void BindOwnedFixture(bool combatCold)
        {
            Require(inherited == null, "Inherited pair fixture cannot be rebound after native admission.");
            inherited = new Chunk7OwnedPairFixture(request, horses, world, controls, persistence, replayAuthorization, replaySaveAuthority, ownedFixtureLoader); inherited.Bind(combatCold);
            firstRider = inherited.Actors[0]; firstMount = inherited.Actors[1]; secondRider = inherited.Actors[2]; secondMount = inherited.Actors[3];
            actors = inherited.Actors; nativeScope = inherited.Scope;
            evidence["inheritedFixture"] = inherited.Capture("admitted");
        }
        private void ObserveCold()
        {
            if (CombatCase) { ObserveCombatCold(); return; }
            saved = persistence.LoadedArchive;
            Require(saved != null && saved.SchemaVersion == 3 && saved.Pairs.Length == 2 && saved.Participations.Length == 0 &&
                saved.Combat == null && saved.AreaId == ExpectedArchiveArea && persistence.ArchiveRestorePhase == "all-pair-presentation-restored",
                "Entire exploration schema 3 graph was not restored from the selected native archive: " + persistence.Feedback);
            if (request.OwnedPairs != null)
            {
                BindOwnedFixture(false);
                Require(saved.Pairs.Length == 2 && saved.Pairs.All(pair =>
                    pair.Rider.Id == firstRider.UniqueId && pair.Mount.Id == firstMount.UniqueId && pair.ProfileId == SupportedMountedProfiles.Resolve(firstRider, firstMount)?.Id ||
                    pair.Rider.Id == secondRider.UniqueId && pair.Mount.Id == secondMount.UniqueId && pair.ProfileId == SupportedMountedProfiles.Resolve(secondRider, secondMount)?.Id),
                    "Cold owned archive profiles or actor roles differ from their qualified inherited native pairs.");
            }
            else
            {
                var mammoth = saved.Pairs.Single(p => p.ProfileId == SupportedMountedProfiles.Mammoth.Id);
                var horse = saved.Pairs.Single(p => p.ProfileId == SupportedMountedProfiles.Horse.Id);
                Require(mammoth.Rider.Id == Chunk7WorldScenarioEngine.MammothRiderId && mammoth.Mount.Id == Chunk7WorldScenarioEngine.MammothId &&
                    horse.Rider.Id == Chunk7WorldScenarioEngine.HorseRiderId, "Cold archive selected another fixture actor.");
                firstRider = Exact(mammoth.Rider.Id); firstMount = Exact(mammoth.Mount.Id);
                secondRider = Exact(horse.Rider.Id); secondMount = Exact(horse.Mount.Id);
                actors = new[] { firstRider, firstMount, secondRider, secondMount };
                TrackNativeActors();
                Require(ReferenceEquals(firstRider.Descriptor.Pet, firstMount) && ReferenceEquals(firstMount.Descriptor.Master.Value, firstRider) &&
                    ReferenceEquals(secondRider.Descriptor.Pet, secondMount) && ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider),
                    "Cold load lost reciprocal native companion ownership.");
            }
            foreach (var pair in saved.Pairs) expectedOwners.Add(Guid.Parse(pair.PairId), pair.Generation);
            AssertOwners(); slots = saved.Slots; AssertSlots();
            Require(persistence.SemanticRestoreCount == 4 && persistence.PresentationRestoreCount == 2 &&
                persistence.ArchivePostLoadObservedCount == 4 && controls.NativeCastRequestCount == 0 && persistence.SnapshotCount == 0,
                "Cold restore replayed attachment/native input or did not restore each actor exactly once.");
            var ids = actors.Select(x => x.UniqueId).ToArray();
            Require(restoration != null && restoration.Complete && restoration.Count("prepare-before", ids) == 0 &&
                restoration.Count("round-before", ids) == 0 && restoration.Count("restore-debt-before", ids) == 4 &&
                restoration.Count("restore-debt-after", ids) == 4, "Cold native actor restoration invented preparation or lost its complete observer window.");
            AssertDebt(saved); evidence["coldRestoration"] = restoration.Capture();
            if (!OwnerSeedLoadRefusalCase) { restoration.Dispose(); restoration = null; }
            if (Chunk7OwnedPairsSelection.IsManticoreManualCold(request.PersistenceCase)) ObserveManticoreManualCold();
            else if (request.OwnedPairs?.IsOwnerSeedDerivedLoad == true || OwnerSeedLoadRefusalCase) ObserveOwnerSeedDerivedCold();
            evidence["loadedArchive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer());
            var descriptor = request.PersistenceLoad;
            evidence["loadedDescriptor"] = new JObject { ["internalName"] = descriptor.InternalName, ["fileName"] = descriptor.FileName,
                ["sha256"] = descriptor.Sha256, ["length"] = descriptor.Length, ["lastWriteTimeUtcTicks"] = descriptor.LastWriteTimeUtcTicks,
                ["gameId"] = descriptor.GameId, ["gameName"] = descriptor.GameName, ["area"] = descriptor.Area };
            snapshots.Add(Capture("cold-restored-before-input")); Pass("C7-persistence-cold-all-pair-identity-and-no-prepare");
        }
        private UnitEntityData Exact(string id)
        {
            var matches = Game.Instance.State.Units.Concat(Game.Instance.Player.AllCharacters).Where(x => x?.UniqueId == id).Distinct().ToArray();
            Require(matches.Length == 1, "Native actor does not resolve uniquely: " + id); return matches[0];
        }
        private bool Mounted(UnitEntityData rider, UnitEntityData mount)
        {
            var owner = world.Registry.FindActive(rider);
            return owner != null && ReferenceEquals(owner.Mount, mount) && ReferenceEquals(world.Registry.FindActive(mount), owner) &&
                owner.Boundary.State == RelationshipState.Mounted;
        }
        private void RememberOwners()
        {
            foreach (var actor in new[] { firstRider, secondRider }.Where(r => world.Registry.FindActive(r) != null))
            { var owner = world.Registry.FindActive(actor); expectedOwners.Add(owner.Id, owner.Generation); }
        }
        private void AssertOwners()
        {
            if (Schema1ResaveCase) { AssertSchema1Owners(); return; }
            if (OwnedSchema1CurrentCase) { AssertOwnedSchema1Owner(); return; }
            if (OwnedSchema1SourceCase) { AssertOwnedSchema1Owner(); return; }
            if (CombatCase) { AssertCombatOwners(); return; }
            Require(Mounted(firstRider, firstMount) && Mounted(secondRider, secondMount) && world.Registry.ActiveCount == 2 &&
                world.Registry.Owners.Length == 2 && world.Registry.Owners.All(o => expectedOwners.TryGetValue(o.Id, out var generation) && generation == o.Generation),
                "All-pair owner identity, generation or exact actor routing changed.");
        }
        private void Input(UnitEntityData caster, UnitEntityData target, BlueprintAbility ability, UnitEntityData other, string label)
        {
            var row = new JObject { ["label"] = label, ["before"] = Capture("input-before") }; inputs.Add(row);
            shell = Chunk7NativeFixture.Issue(controls, caster, target, ability, other, row); issued.Add(shell);
        }
        private bool InputSettled() => shell != null && shell.IsFinished && !controls.HasUnsettledRelationshipTransition &&
            world.Pairs.All(p => !controls.HasUnsettledShellFor(p.Relationship));
        private void FinishInput() { var row = (JObject)inputs[inputs.Count - 1]; row["terminal"] = shell.IsFinished; row["after"] = Capture("input-after"); shell = null; }
        private void BindSlots()
        {
            controls.Update();
            foreach (var rider in new[] { firstRider, secondRider }.Where(r => world.Registry.FindActive(r) != null)) foreach (var blueprint in new[] { controls.DismountAbility, controls.RiderPrimaryAbility })
            {
                var fact = rider.Descriptor.Abilities.GetAbility(blueprint); var nativeSlots = rider.UISettings.Slots;
                var index = Array.FindIndex(nativeSlots, s => s == null || s is MechanicActionBarSlotEmpty);
                Require(fact != null && index >= 0 && index < 128, "Native hotbar lacks a declared empty owned slot.");
                rider.UISettings.SetSlot(new MechanicActionBarSlotAbility { Unit = rider, Ability = fact.Data }, index);
            }
            slots = controls.CapturePersistentSlots();
            if (OwnedSchema1SourceCase)
            {
                Require(slots.Length >= 2 && slots.Length <= 128 && slots.All(s => s.Kind >= 1 && s.Kind <= 4) &&
                    slots.Any(s => s.ActorId == firstRider.UniqueId && s.Kind == 2) &&
                    slots.Any(s => s.ActorId == firstRider.UniqueId && s.Kind == 3),
                    "Owned schema-1 source has an unrepresentable control; no saved slot may be discarded.");
            }
            else Require(slots.Length == (LegacyPendingCase || PendingMountSave ? 2 : 4) && slots.Count(s => s.ActorId == firstRider.UniqueId) == 2 &&
                slots.Count(s => s.ActorId == secondRider.UniqueId) == (LegacyPendingCase || PendingMountSave ? 0 : 2), "All-pair native hotbar binding differs.");
        }
        private void AssertSlots()
        {
            var actual = controls.CapturePersistentSlots();
            Require(actual.Length == slots.Length && slots.All(s => actual.Count(x => x.ActorId == s.ActorId && x.Index == s.Index && x.Kind == s.Kind) == 1),
                "Native controls did not restore every owner slot exactly once.");
        }
        private void RequestSave()
        {
            RequireNativeWorld();
            if (Schema1ResaveCase) { RequestSchema1Resave(); return; }
            if (Schema2ResaveCase) { RequestSchema2Resave(); return; }
            if (CombatCase) PrepareCombatSave();
            var type = NativeType(request.PersistenceCase); var manager = Game.Instance.SaveManager;
            if (request.OwnedPairs != null) RequireOwnedSaveAdmission(type);
            NativePersistenceIsolation.EnableNativeSlotRotation();
            var descriptor = type == SaveInfo.SaveType.Quick ? manager.GetNextQuickslot() :
                type == SaveInfo.SaveType.Auto ? manager.GetNextAutoslot() : manager.CreateNewSave(NativeName(type, request.OwnedPairs, request.PersistenceCase));
            Require(descriptor != null && descriptor.Type == type && descriptor.Name == NativeName(type, request.OwnedPairs, request.PersistenceCase) &&
                !descriptor.IsActuallySaved && manager.IsSaveAllowed(), "Actual native category admission differs.");
            if (request.OwnedPairs != null) ownedRequestedSave = descriptor;
            if (SourceSaveBoundary) { RequireSaveBoundaryAuthority(); boundaryRequestedSave = descriptor; }
            castBefore = controls.NativeCastRequestCount; callback = false;
            evidence["saveRequested"] = new JObject { ["nativeType"] = type.ToString(), ["nativeName"] = descriptor.Name,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["snapshotCount"] = persistence.SnapshotCount,
                ["nativeSaveAllowed"] = true, ["newSlot"] = true };
            Game.Instance.SaveGame(descriptor, () => { if (request.OwnedPairs != null) ownedSaveCallbacks++; if (SourceSaveBoundary && WorkerCancelSave) boundarySeedCallbacks++; callback = true; });
        }
        private void ObserveSaved()
        {
            if (Schema1ResaveCase) { ObserveSchema1Resaved(); return; }
            if (Schema2ResaveCase) { ObserveSchema2Resaved(); return; }
            var type = NativeType(request.PersistenceCase);
            savedDescriptor = Game.Instance.SaveManager.SingleOrDefault(s => s.Name == NativeName(type, request.OwnedPairs, request.PersistenceCase) && s.Type == type);
            Require(savedDescriptor != null && savedDescriptor.OperationState == SaveInfo.StateType.None && savedDescriptor.HasFileOnDisk &&
                savedDescriptor.FileName == SelectedOutputLeaf(type), "Native archive did not complete at its exact declared leaf.");
            ObserveOwnedSaveCompletion();
            var read = NativeMountedSaveStorage.ReadArchive(savedDescriptor.Saver); saved = read.Data;
            if (CombatCase) { ObserveCombatSaved(read); return; }
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 3 && saved?.Pairs.Length == (OwnedSchema1SourceCase ? 1 : 2) &&
                saved.Participations.Length == 0 && saved.Combat == null && saved.CampaignId == request.SelectedChunk7Fixture.GameId &&
                saved.AreaId == ExpectedArchiveArea, "Native archive readback did not retain the entire exploration world.");
            AssertOwners(); AssertSlots(); AssertDebt(saved);
            Require(saved.Pairs.All(p => expectedOwners.TryGetValue(Guid.Parse(p.PairId), out var generation) && generation == p.Generation &&
                world.Registry.FindActive(Exact(p.Rider.Id))?.Id == Guid.Parse(p.PairId)), "Native readback changed stable custody.");
            Require(saved.Slots.Length == slots.Length && slots.All(s => saved.Slots.Any(x => x.ActorId == s.ActorId && x.Index == s.Index && x.Kind == s.Kind)),
                "Native readback omitted another pair's hotbar bindings.");
            Require((AreaAutosaveCase ? !callback && areaHeaderStarting != null && areaHeaderStaged != null :
                    WorkerCancelSave ? !callback && boundaryDrainedCommitted : callback) &&
                persistence.SnapshotCount == snapshotBefore + (WorkerCancelSave ? 2 : 1) && controls.NativeCastRequestCount == castBefore &&
                !controls.SerializationSuspended && !persistence.SaveSuspended, "Native snapshot replayed input or retained an owned serialization lease.");
            var file = new FileInfo(savedDescriptor.FolderName);
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["sha256"] = Hash(file.FullName), ["length"] = file.Length,
                ["nativeType"] = type.ToString(), ["nativeName"] = savedDescriptor.Name, ["nativeCallback"] = callback,
                ["operation"] = savedDescriptor.OperationState.ToString(), ["snapshotCount"] = persistence.SnapshotCount,
                ["archive"] = JObject.FromObject(saved, MountedSaveCodec.CreateSerializer()) };
            snapshots.Add(Capture("native-write-complete"));
            if (OwnedSchema1SourceCase)
            {
                ValidateOwnedSchema1Archive(saved, request.PersistenceLoad.GameId, request.PersistenceLoad.Area, firstRider.UniqueId, firstMount.UniqueId, false);
                CompleteOwnedSchema1Field("source-settled"); Pass("C7-persistence-owned-schema1-source");
            }
            else Pass("C7-persistence-native-category-all-pair-archive");
        }
        private void AssertDebt(MountedArchiveData archive)
        {
            var elapsed = Math.Max(0, (Game.Instance.TimeController.GameTime.Ticks - archive.GameTimeTicks) / (double)TimeSpan.TicksPerSecond);
            foreach (var before in archive.Pairs.SelectMany(p => new[] { p.Rider, p.Mount }))
            {
                var after = MountedPersistenceService.CaptureActor(Exact(before.Id));
                var a = new[] { before.Standard, before.Move, before.Swift, before.Initiative, before.Reaction };
                var b = new[] { after.Standard, after.Move, after.Swift, after.Initiative, after.Reaction };
                var slack = 0.001 + elapsed * 0.1;
                Require(before.Id == after.Id && before.ReactionsRemaining == after.ReactionsRemaining && before.LastSurpriseTicks == after.LastSurpriseTicks &&
                    Enumerable.Range(0, a.Length).All(i => b[i] <= a[i] + 0.001 && b[i] + elapsed + slack >= a[i]),
                    "Native archive/restoration did not preserve actor debt within actual native clock progress.");
            }
        }
        private void Move(UnitEntityData selected, UnitEntityData executor, Vector3 destination, UnitEntityData other, string label)
        {
            Require(actors.All(a => a.Commands.Move == null), "Native continuation started with unfinished movement.");
            Chunk7NativeFixture.Select(selected); Game.Instance.IsPaused = true;
            var previous = actors.ToDictionary(a => a, a => a.Commands.Move); moveStart = executor.Position; moveGoal = destination;
            ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false); move = executor.Commands.Move; movingActor = executor;
            Require(move != null && !ReferenceEquals(move, previous[executor]) &&
                actors.Where(a => !ReferenceEquals(a, executor)).All(a => ReferenceEquals(a.Commands.Move, previous[a])),
                "Continuation routed ordinary movement into the wrong actor.");
            issued.Add(move); moveRow = new JObject { ["label"] = label, ["selectedId"] = selected.UniqueId,
                ["executorId"] = executor.UniqueId, ["otherCommandsUnchanged"] = true, ["start"] = Point(moveStart), ["goal"] = Point(moveGoal) };
            movementRows.Add(moveRow); Chunk7NativeFixture.Select(other); Game.Instance.IsPaused = false;
        }
        private bool MovementSettled()
        {
            if (!move.IsFinished || movingActor.View.AgentASP.IsReallyMoving) return false;
            moveRow["end"] = Point(movingActor.Position); moveRow["terminal"] = true;
            moveRow["distance"] = Vector3.Distance(moveStart, movingActor.Position); moveRow["residual"] = Vector3.Distance(moveGoal, movingActor.Position);
            Require((float)moveRow["distance"] >= 0.75f && (float)moveRow["residual"] <= 1f, "Two-metre native continuation missed its fixed endpoint.");
            return true;
        }
        private JObject Capture(string label) => new JObject { ["label"] = label, ["frame"] = Time.frameCount,
            ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["owners"] = new JArray(world.Registry.Owners.Select(o => new JObject { ["ownerId"] = o.Id.ToString("N"),
                ["generation"] = o.Generation, ["riderId"] = o.Rider?.UniqueId, ["mountId"] = o.Mount?.UniqueId, ["state"] = o.State.ToString() })),
            ["actors"] = new JArray((actors ?? new UnitEntityData[0]).Select(a => new JObject { ["native"] = JObject.FromObject(
                MountedPersistenceService.CaptureActor(a), MountedSaveCodec.CreateSerializer()), ["prepared"] = a.CombatState.Prepared, ["inCombat"] = a.IsInCombat })),
            ["controls"] = JObject.FromObject(controls.CaptureSnapshot(), MountedSaveCodec.CreateSerializer()),
            ["semanticRestores"] = persistence.SemanticRestoreCount, ["presentationRestores"] = persistence.PresentationRestoreCount,
            ["nativePostLoads"] = persistence.ArchivePostLoadObservedCount, ["snapshots"] = persistence.SnapshotCount,
            ["nativeCastRequests"] = controls.NativeCastRequestCount, ["archivePhase"] = persistence.ArchiveRestorePhase };
        private void BeginCleanup()
        {
            if (cleanup) return;
            cleanup = true;
            if (!clock.IsRunning) clock.Start();
            stageStarted = clock.Elapsed.TotalSeconds;
            TryCleanup("owned native worker hold", ReleaseSaveBoundaryHold);
            // Observers and leases remain owned until loading and native writers
            // have stopped. Beginning failure cleanup is not teardown permission.
        }
        private bool TryCleanup(string part, Action operation)
        {
            try { operation(); return true; }
            catch (Exception error)
            {
                var detail = part + ": " + error;
                if (cleanupFaults.Add(detail)) Fail("C7-persistence-cleanup", detail);
                return false;
            }
        }
        private bool cleanupPendingArtifactWritten;
        private void RetainCleanup(string message, double deadline)
        {
            if (clock.Elapsed.TotalSeconds - stageStarted < deadline) return;
            var newFailure = cleanupFaults.Add(message);
            if (newFailure) Fail("C7-persistence-cleanup", message);
            pendingHostCustody = true; evidence["cleanupPendingAtHostBoundary"] = true;
            if (!cleanupPendingArtifactWritten)
            {
                if (AreaAutosaveCase) TryCleanup("area interruption observation", CaptureAreaInterruption);
                if (checkpoint != null) evidence["checkpointCleanup"] = checkpoint.Capture();
                RuntimeLoaderInventory.WriteArtifact(request, "chunk7-persistence-cleanup-pending.json",
                    "chunk7-persistence-cleanup-pending", "FAIL", evidence, errors);
                cleanupPendingArtifactWritten = true;
            }
            completed = true;
        }
        private bool PerformCleanup()
        {
            RequireCleanupWorld();
            var settled = TryCleanup("selected native format", RestoreSelectedNativeFormat);
            settled &= TryCleanup("native slot rotation", NativePersistenceIsolation.DisableNativeSlotRotation);
            if (!captured) return settled;
            Game.Instance.IsPaused = false;
            foreach (var command in issued.Where(c => c != null && !c.IsFinished))
                settled &= TryCleanup("issued command", () => command.Interrupt());
            foreach (var pair in world.Pairs)
                settled &= TryCleanup("pair command", () => pair.Combat.Cancel("Chunk 7 native persistence fixture cleanup"));
            settled &= issued.Where(c => c != null).All(c => c.IsFinished &&
                (!(c is UnitUseAbility) || ((UnitUseAbility)c).ExecutionProcess == null || ((UnitUseAbility)c).ExecutionProcess.IsEnded)) &&
                world.Pairs.All(p => !p.Combat.HasActiveCommand && !p.Combat.HasActiveGroundMovement && !p.Combat.HasChargeOwnership &&
                    !controls.HasUnsettledShellFor(p.Relationship));
            if (!settled) return false;
            foreach (var pair in world.Pairs)
                settled &= TryCleanup("relationship", () =>
                {
                    if (pair.Relationship.State != RelationshipState.Unmounted || pair.Relationship.PairOwner != null)
                        Require(pair.Relationship.Dismount(CleanupTrigger.Exception).Succeeded, "Pair cleanup retains obligations.");
                });
            settled &= world.Pairs.All(p => p.Relationship.State == RelationshipState.Unmounted && p.Relationship.PairOwner == null &&
                p.Relationship.Runtime.NoPreparedPairResidue && !controls.HasUnsettledShellFor(p.Relationship));
            settled &= !world.UnifiedTurn.HasUnsettledPreparation && !controls.HasUnsettledRelationshipTransition &&
                !controls.SerializationSuspended;
            if (!settled) return false;
            if (inherited == null && acquiredHorse != null)
                settled &= TryCleanup("owned Horse fact", () =>
                {
                    Require(secondRider.Descriptor.Pet == null || ReferenceEquals(secondRider.Descriptor.Pet, secondMount), "Cleanup refuses a replaced companion.");
                    secondRider.Descriptor.Progression.Features.RemoveFact(acquiredHorse); acquiredHorse = null;
                });
            if (inherited == null && acquiredRank != null)
                settled &= TryCleanup("owned rank fact", () => { secondRider.Descriptor.Progression.Features.RemoveFact(acquiredRank); acquiredRank = null; });
            settled &= TryCleanup("native entity drain", () => Game.Instance.EntityDestroyer.Tick());
            if (inherited != null && inherited.Bound) return settled && TryCleanup("inherited native facts", inherited.RequirePreserved);
            return settled && acquiredHorse == null && acquiredRank == null && (Cold || secondMount == null ||
                !secondMount.IsInState && !Game.Instance.State.Units.Contains(secondMount) &&
                !Game.Instance.Player.ControllableCharacters.Contains(secondMount) && secondRider.Descriptor.Pet == null);
        }
        private void PollCleanup()
        {
            if (cleanupProven) { CompleteCleanupEvidence(); return; }
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() || persistence.AreaTransitionPending)
            {
                RetainCleanup("Native loading/area transfer still owns its declared world boundary.", 15);
                return;
            }
            if (!TryCleanup("captured native world", RequireCleanupWorld))
            {
                RetainCleanup("Captured native world identity changed; all actor/UI/mode cleanup custody is retained.", 45);
                return;
            }
            if (world.SerializationBusy ||
                NativePersistenceIsolation.HasPendingWrites || persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused ||
                persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence)
            {
                RetainCleanup("Native load/iterator/worker or failed release still owns live fixture state; no actor/control cleanup was attempted.", 15);
                return;
            }
            var observersSettled = TryCleanup("save boundary observation", EndSaveBoundaryObservation);
            observersSettled &= TryCleanup("load-refusal observation", EndLoadRefusalObservation);
            observersSettled &= TryCleanup("area observation", EndAreaObservation);
            observersSettled &= TryCleanup("repeated world observation", EndReplayObservation);
            observersSettled &= TryCleanup("cold restoration observer", () =>
            {
                if (restoration != null) evidence["coldRestoration"] = restoration.Capture();
                restoration?.Dispose(); restoration = null;
            });
            if (!observersSettled) { RetainCleanup("Native fixture observation still owns a callback or patch.", 45); return; }
            if (checkpoint != null)
            {
                var checkpointSettled = false;
                TryCleanup("checkpoint owner", () =>
                {
                    checkpoint.BeginCleanup();
                    checkpointSettled = checkpoint.UpdateCleanup();
                    if (!checkpointSettled) return;
                    Require(checkpoint.CleanupComplete, "Checkpoint returned without proving cleanup complete.");
                    evidence["checkpointCleanup"] = checkpoint.Capture();
                    checkpoint.Dispose(); checkpoint = null;
                });
                if (!checkpointSettled || checkpoint != null)
                { RetainCleanup("Checkpoint still owns native target/combat/AI cleanup; pair and acquired-fact cleanup was not attempted.", 45); return; }
            }
            if (!cleanupActions)
            {
                cleanupActions = PerformCleanup();
                if (!cleanupActions) { RetainCleanup("Native commands, pair cleanup or owned progression facts have not settled.", 45); return; }
                if (RepeatedWorldCase) repeatedCleanupActionsFrame = Time.frameCount;
            }
            // Unity destroys movement components at the end of the frame. The
            // repeated-world settlement must sample the next native frame.
            if (RepeatedWorldCase && Time.frameCount <= repeatedCleanupActionsFrame) return;
            var leasesRestored = TryCleanup("native mode", () => { mode?.Dispose(); mode = null; });
            if (captured)
            {
                leasesRestored &= TryCleanup("paired configuration", () =>
                {
                    Require(world.Primary.Combat.TryConfigurePairedActivation(originalPaired), "Original paired configuration restoration refused.");
                    settings.EnableUnsafeMovementExperiment = originalMovement;
                });
                leasesRestored &= TryCleanup("selection", () =>
                {
                    SelectionManager.Instance.MultiSelect(originalSelection.Where(a => a.IsInState && a.View != null).Select(a => a.View), false);
                    Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection), "Original selected native actors were not restored.");
                });
            }
            leasesRestored &= TryCleanup("UMM", () => { ummLease?.Dispose(); ummLease = null; });
            if (!leasesRestored) { RetainCleanup("Native fixture settings or UI leases have not restored.", 45); return; }
            if (captured) Game.Instance.IsPaused = originalPause;
            var pairsClear = world.Pairs.All(p => p.Relationship.State == RelationshipState.Unmounted && p.Relationship.PairOwner == null &&
                p.Relationship.Runtime.NoPreparedPairResidue && !controls.HasUnsettledShellFor(p.Relationship));
            var inheritedClear = inherited != null && inherited.Bound && TryCleanup("inherited native facts", inherited.RequirePreserved);
            var horseClear = inherited != null && inherited.Bound ? inheritedClear : Cold || secondMount == null || !secondMount.IsInState && !Game.Instance.State.Units.Contains(secondMount) &&
                !Game.Instance.Player.ControllableCharacters.Contains(secondMount) && secondRider.Descriptor.Pet == null;
            var cleanupReady = pairsClear && horseClear && !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence &&
                !controls.SerializationSuspended && !controls.HasUnsettledRelationshipTransition && !world.UnifiedTurn.HasUnsettledPreparation &&
                restoration == null && retention == null && saveBoundaryProbe == null && saveWorkerHold == null &&
                mode == null && ummLease == null && checkpoint == null;
            if (!cleanupReady) { RetainCleanup("Native persistence cleanup postcondition still owns an obligation.", 45); return; }
            TryCleanup("immutable source archive", () =>
            {
                if (RepeatedWorldCase && replayInput != null) RequireReplayInput();
                if (Schema1ResaveCase) VerifySchema1InputImmutable();
                if (Schema2ResaveCase) VerifySchema2InputImmutable();
                // A write refused before its record has no recorded hash to compare.
                if (savedDescriptor != null && evidence["nativeWrite"] is JObject write) Require(Hash(savedDescriptor.FolderName) == (string)write["sha256"],
                    "Source archive changed after its recorded native completion.");
            });
            if (inherited != null && inherited.Bound) evidence["inheritedCleanup"] = inherited.Capture("cleanup");
            evidence["cleanup"] = new JObject { ["pairsClear"] = pairsClear, ["sourceHorseRemoved"] = inherited == null && horseClear,
                ["inheritedPetsPreserved"] = inheritedClear,
                ["controlsSettled"] = true, ["saveCustodyClear"] = true, ["selectionRestored"] = captured,
                ["nativeProgressionRequiresExternalWorkingRestoration"] = inherited == null, ["ordinaryRespecProven"] = false,
                ["world"] = captured ? Capture("cleanup") : null };
            CaptureRepeatedSettlement();
            ReleaseOwnedAreaObservation();
            ReleaseManticoreObservation();
            if (pendingHostCustody) evidence["cleanupSettledAfterHostBoundary"] = true;
            pendingHostCustody = false;
            inherited?.Release(); cleanupProven = true;
            Pass("C7-persistence-cleanup"); CompleteCleanupEvidence();
        }
        private void CompleteCleanupEvidence()
        {
            Require(cleanupProven, "Native persistence cleanup retains its exact inherited custody.");
            evidence["elapsedSeconds"] = clock.Elapsed.TotalSeconds;
            evidence["nativeShellLifecycle"] = controls.DescribeNativeShellLifecycle();
            RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors);
            completed = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (completed && cleanupProven && !pendingHostCustody) return true;
            if (!cleanup)
            {
                Fail("C7-persistence-interrupted", "Native persistence scenario interrupted before its terminal result.");
                BeginCleanup();
            }
            completed = false;
            PollCleanup();
            return completed && cleanupProven && !pendingHostCustody;
        }
        private static JObject Point(Vector3 v) => new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };
        private static string Hash(string path) { using (var h = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(h.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private void Next(int next) { stage = next; stageStarted = clock.Elapsed.TotalSeconds; }
        private void Pass(string row)
        {
            var existing = results.SingleOrDefault(x => x.Name == row);
            if (existing == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "PASS", AssertionPassCount = 1, Errors = new string[0] });
            else existing.AssertionPassCount++;
        }
        private void Fail(string row, string error)
        {
            if (errors.Contains(error)) return;
            errors.Add(error);
            var existing = results.SingleOrDefault(x => x.Name == row);
            if (existing == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { error } });
            else { existing.Status = "FAIL"; existing.AssertionFailCount++; existing.Errors = existing.Errors.Concat(new[] { error }).ToArray(); }
        }
        public void Dispose()
        {
            if (disposed) return;
            if (!DrainForHost())
                throw new InvalidOperationException("Native persistence fixture cleanup is asynchronous and retains its exact state for retry.");
            disposed = true;
        }

    }
}
