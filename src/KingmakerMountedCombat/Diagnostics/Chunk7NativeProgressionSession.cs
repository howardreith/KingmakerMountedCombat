using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.LevelUp;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{


    // The closed native progression scenario owns this session. It
    // requires a separately qualified native manual fixture and an exact already
    // active isolated plan. It never creates that fixture or grants entitlement.
    // The owning scenario assigns its field before Start and retains it
    // until TryDrain succeeds, then independently drain remaining world owners.
    internal sealed class Chunk7NativeProgressionSession : IDisposable
    {
        internal const string Scenario = "chunk7-native-progression";
        internal const string SourceLeaf = "Manual_300_KMC_C7_WORLD.zks";
        internal const string SourceName = "KMC_C7_WORLD";
        private readonly RuntimeRequest request;
        private readonly RuntimeSaveDescriptor source;
        private readonly Chunk7NativeProgressionCase selectedCase;
        private readonly Chunk7OrdinaryBuildPins buildPins;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly PersistenceSaveAuthorization authority;
        private readonly MountedWorldService world;
        private readonly MountedPersistenceService persistence;
        private readonly HorseCompanionBlueprintService horses;
        private readonly UnitEntityData rider, oldMount;
        private readonly string riderId, oldMountId, expectedRoot;
        private readonly int authorityGeneration;
        private readonly Stopwatch elapsed = Stopwatch.StartNew();
        private readonly List<string> errors = new List<string>();
        private readonly JArray events = new JArray();
        private Chunk7NativeWorldScope scope;
        private Chunk7ProgressionPairCustody pairCustody;
        private Chunk7NativeProgressionUi nativeUi;
        private Chunk7OrdinaryBuildDriver driver;
        private Chunk7NativeBuilderCancellation cancellation;
        private NativeOrdinaryAcquisitionProbe completion;
        private Chunk7ManualSeedSaveProbe manualProbe;
        private SaveInfo requestedManual;
        private bool manualCallback;
        private int manualCallbackCount;
        private MountedPairOwner<UnitEntityData, GameMountedRelationshipService> originalOwner;
        private MountedPairServices originalPair;
        private object originalDescriptor, originalView, originalMountDescriptor, originalMountView;
        private object capturedBuilder;
        private Player nativePlayer;
        private long sourceLength, sourceTicks, initialMoney, castsBefore;
        private int initialXp, initialLevel, initialPetLevel, snapshotBefore, completedBefore;
        private bool started, mutationStarted, cleanup, settled, disposed, successful, rotationOwned, nativeCleanupObserved, nativeCleanupSampled;
        private int stage;
        private double stageStart;
        private JObject nativeWrite, nativeUiEvidence, completionEvidence;
        private bool WritesAutosave => request.Progression?.WritesAutosave == true;
        private bool WritesManual => request.Progression?.WritesManual == true;
        private bool WritesSave => WritesAutosave || WritesManual;
        private bool IsRespec => selectedCase == Chunk7NativeProgressionCase.RespecHorse || selectedCase == Chunk7NativeProgressionCase.RespecMammoth;

        internal Chunk7NativeProgressionSession(RuntimeRequest request, Chunk7NativeProgressionCase selectedCase,
            Chunk7OrdinaryBuildPins buildPins, UnitEntityData rider, UnitEntityData oldMount,
            RuntimeSaveAuthorization authorization, PersistenceSaveAuthorization authority,
            MountedWorldService world, MountedPersistenceService persistence, HorseCompanionBlueprintService horses)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.selectedCase = selectedCase; this.buildPins = buildPins;
            source = request.PersistenceLoad ?? throw new ArgumentException("Progression requires an exact separately qualified native manual source.");
            var ownedSource = request.Progression?.IsOwned == true;
            Require(request.Scenario == Scenario && Enum.IsDefined(typeof(Chunk7NativeProgressionCase), selectedCase) &&
                (ownedSource ? request.OwnedPairs == null && request.Progression.Validate(request.Scenario, request.RunId).Count == 0 &&
                    request.Progression.NativeCase == selectedCase && request.Progression.ValidateOwnedDescriptor(source, request.Fixture).Count == 0 :
                source.Validate("progressionSource", SourceName, "^Manual_300_KMC_C7_WORLD\\.zks$").Count == 0 &&
                request.Fixture?.Working != null && source.GameId == request.Fixture.Working.GameId &&
                source.GameName == request.Fixture.Working.GameName && source.Area == request.Fixture.Working.Area &&
                source.Sha256 != request.Fixture.Baseline.Sha256 && source.Sha256 != request.Fixture.Working.Sha256),
                "Progression source is not its exact distinct disposable native fixture.");
            this.rider = rider ?? throw new ArgumentNullException(nameof(rider));
            this.oldMount = oldMount ?? throw new ArgumentNullException(nameof(oldMount));
            riderId = rider.UniqueId; oldMountId = oldMount.UniqueId;
            this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.horses = horses ?? throw new ArgumentNullException(nameof(horses));
            expectedRoot = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(request.EvidenceRoot)),
                "runtime-staging", "persistence-" + request.RunId, "Saved Games");
            authorityGeneration = authorization.AuthorityGeneration;
            Require(!WritesSave && buildPins == null || WritesSave && buildPins != null &&
                buildPins.RunId == request.RunId && buildPins.Mode == (selectedCase == Chunk7NativeProgressionCase.ExistingHorseLevel ?
                    Chunk7OrdinaryBuildMode.ExistingHorseLevels : selectedCase == Chunk7NativeProgressionCase.RespecHorse ?
                    Chunk7OrdinaryBuildMode.NativeRespecHorse : Chunk7OrdinaryBuildMode.NativeRespecMammoth),
                "Native progression case and actual choice plan differ.");
        }

        internal void AssertOwnedScope() { AssertWorld(); if (started) AssertTargetBinding(); }
        // Read-only projection of the existing native lifecycle proof. It creates
        // no new target authority and never forces a callback or pet rebind.
        internal bool TryGetObservedTargetRoster(out bool removed, out UnitEntityData currentPet)
        {
            AssertOwnedScope(); removed = false; currentPet = oldMount;
            if (!started) return false;
            if (IsRespec && nativeUi?.RespecProbe != null &&
                nativeUi.RespecProbe.TryRequireNativeRebind(Game.Instance.UI.CharacterBuildController))
            {
                nativeUi.RespecProbe.RequireObservedPetBinding(oldMount, originalMountDescriptor, originalMountView);
                currentPet = rider.Descriptor.Pet; return true;
            }
            if (selectedCase == Chunk7NativeProgressionCase.DismissCustomCompanion && nativeUi?.NativeDismissalObserved == true)
            { removed = true; currentPet = null; return true; }
            return false;
        }
        internal bool HasStarted => started;
        internal bool IsComplete => settled;
        internal bool Succeeded => settled && successful && errors.Count == 0;
        internal JObject Capture() => new JObject { ["case"] = selectedCase.ToString(), ["stage"] = stage,
            ["started"] = started, ["settled"] = settled, ["success"] = Succeeded,
            ["errors"] = new JArray(errors), ["events"] = events.DeepClone(), ["pairCustody"] = pairCustody?.Capture(),
            ["nativeUi"] = nativeUiEvidence?.DeepClone() ?? nativeUi?.Capture(),
            ["nativeCompletion"] = completionEvidence?.DeepClone() ?? completion?.Capture(),
            ["manualSave"] = manualProbe?.Capture(), ["nativeWrite"] = nativeWrite?.DeepClone(),
            ["nativeAutomaticTargetCleanupObserved"] = nativeCleanupObserved,
            ["manualMoneyWrites"] = 0, ["manualXpWrites"] = 0, ["manualCompanionRemovals"] = 0,
            ["manualNativeRestCalls"] = 0, ["manualNativeTimeAdvances"] = 0,
            ["remainingWorldCleanupProven"] = false, ["fixtureRollbackOwnedByOuterTransaction"] = true };

        internal void Start()
        {
            Require(!started && !disposed, "Progression session cannot be started twice.");
            AssertAuthority(); Require(!NativeBusy(), "Progression waits for the exact loaded source and native writer.");
            var game = Game.Instance;
            Require(game.CurrentMode == GameModeType.Default && game.UI?.CharacterBuildController != null &&
                game.UI.CharacterBuildController.LevelUpController == null && game.UI.CharacterBuildController.Unit == null &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown), "Pre-existing native UI blocks progression admission.");
            Require(rider.Blueprint == BlueprintRoot.Instance.CustomCompanion && !ReferenceEquals(game.Player.MainCharacter.Value, rider) &&
                rider.IsInState && rider.IsInGame && rider.View?.AgentASP != null && oldMount.IsInState && oldMount.IsInGame &&
                oldMount.View?.AgentASP != null && ReferenceEquals(rider.Descriptor.Pet, oldMount) &&
                ReferenceEquals(oldMount.Descriptor.Master.Value, rider) && rider.Descriptor.State.CanRemoveFromParty &&
                !rider.Descriptor.State.IsFinallyDead && !oldMount.Descriptor.State.IsFinallyDead &&
                game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, rider)) == 1,
                "Progression target is not its exact reciprocal disposable native field pair.");
            Require(horses.HorseFeature != null && horses.HorseUnit != null && horses.LevelRank != null,
                "Native sourced Horse content is unavailable.");
            originalOwner = world.Registry.FindActive(rider);
            Require(originalOwner != null && ReferenceEquals(originalOwner.Mount, oldMount), "Progression target has no exact mounted registration.");
            originalPair = world.Pairs.Single(value => ReferenceEquals(value.Relationship, originalOwner.Boundary));
            originalDescriptor = rider.Descriptor; originalView = rider.View;
            originalMountDescriptor = oldMount.Descriptor; originalMountView = oldMount.View;
            capturedBuilder = game.UI.CharacterBuildController;
            nativePlayer = game.Player;
            // The selected native respec/dismissal may replace/remove only these
            // two entities. Every other native actor remains fully pinned.
            scope = new Chunk7NativeWorldScope(request, game.Player.AllCharacters.Concat(game.Player.ControllableCharacters)
                .Where(value => value != null && !ReferenceEquals(value, rider) && !ReferenceEquals(value, oldMount)).Distinct());
            pairCustody = new Chunk7ProgressionPairCustody(world, rider, oldMount, AssertWorld, NativeBusy);
            initialMoney = game.Player.Money; initialXp = rider.Descriptor.Progression.Experience;
            initialLevel = rider.Descriptor.Progression.CharacterLevel; initialPetLevel = oldMount.Descriptor.Progression.CharacterLevel;
            snapshotBefore = persistence.SnapshotCount; completedBefore = persistence.CompletedSaveCount;
            castsBefore = world.NativeControls.NativeCastRequestCount;
            var file = new FileInfo(Path.Combine(expectedRoot, source.FileName));
            Require(file.Exists && file.Length == source.Length && (file.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(file.FullName) == source.Sha256,
                "Actual staged native progression source differs from its exact qualified input.");
            sourceLength = file.Length; sourceTicks = file.LastWriteTimeUtc.Ticks;
            started = true; Next(0); Record("native-progression-admitted", NativeActor());
            if (WritesAutosave)
            {
                Require(game.SaveManager.IsSaveAllowed() && !game.SaveManager.Any(save => save.Type == SaveInfo.SaveType.Auto) &&
                    !File.Exists(Path.Combine(expectedRoot, Chunk7OrdinaryAcquisitionSelection.AutoLeaf)),
                    "Terminal native autosave is unavailable or would overwrite a source/previous autosave.");
                completion = new NativeOrdinaryAcquisitionProbe(() => rider); mutationStarted = true; completion.Install();
                rotationOwned = true; NativePersistenceIsolation.EnableNativeSlotRotation();
            }
            if (WritesManual)
            {
                Require(!NativePersistenceIsolation.NativeSlotRotationEnabled &&
                    authority.MatchesProgressionManualPlan(request, game, game.SaveManager) &&
                    game.SaveManager.IsSaveAllowed(), "Owned progression Manual checkpoint is not an exact unused native one-write plan.");
                mutationStarted = true;
            }
            if (selectedCase == Chunk7NativeProgressionCase.ExistingHorseLevel)
            {
                driver = NewDriver(null); Next(1);
            }
            else
            {
                nativeUi = new Chunk7NativeProgressionUi(rider, AssertWorld, NativeBusy); mutationStarted = true;
                nativeUi.Start(IsRespec);
            }
        }

        internal void Tick()
        {
            if (disposed || settled) return;
            try
            {
                if (cleanup) { TryDrain(); return; }
                Require(started && elapsed.Elapsed.TotalSeconds <= 720, "Native progression session is not active or exceeded twelve minutes.");
                // The native writer temporarily clears/turns off actors while it serializes.
                // Keep custody, then make the full roster assertion after its worker settles.
                if (requestedManual != null && NativeBusy()) return;
                AssertWorld(); AssertTargetBinding(); if (NativeBusy()) return;
                if (stage == 0)
                {
                    if (!nativeUi.Tick()) return;
                    if (IsRespec) { driver = NewDriver(nativeUi.RespecProbe); Next(1); }
                    else { Record("native-dismissal-complete", NativeActor()); Next(2); }
                }
                else if (stage == 1)
                {
                    if (driver.Tick() != Chunk7OrdinaryBuildStep.Complete) return;
                    Require(ReferenceEquals(driver.Actor, rider) && rider.Descriptor.Progression.Experience == initialXp,
                        "Native progression completed on another actor or changed real XP.");
                    if (!IsRespec)
                        Require(rider.Descriptor.Progression.CharacterLevel > initialLevel && ReferenceEquals(rider.Descriptor.Pet, oldMount) &&
                            oldMount.Descriptor.Progression.CharacterLevel > initialPetLevel && Game.Instance.Player.Money == initialMoney,
                            "Selected real XP did not produce native rider and existing Horse advancement without money changes.");
                    else Require(nativeUi.RespecProbe.Complete, "Native retraining effects/rebind are incomplete.");
                    Record("native-progression-builder-complete", NativeActor()); Next(2);
                }
                else if (stage == 2)
                {
                    if (IsRespec || selectedCase == Chunk7NativeProgressionCase.DismissCustomCompanion)
                    {
                        if (!ObserveNativeTargetCleanup())
                        {
                            Require(elapsed.Elapsed.TotalSeconds - stageStart <= 15, "Native progression left its old mounted owner or command/preparation obligations unresolved.");
                            return;
                        }
                        nativeCleanupObserved = true;
                    }
                    // NOT READY - stale level-up autosave wait (docs/CHUNK7-STALE-LEVELUP-AUTOSAVE-FAMILIES.md).
                    // The installed game never registers LevelUpAutoSaveController (GameModesFactory builds its
                    // controllers explicitly without it), so an ordinary level-up never requests this native Auto
                    // and the wait cannot complete. scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1 refuses this
                    // family before any lock, transaction, Mods swap or launch. Kept intact until its completion
                    // contract is repaired; never manufacture a level-up callback or relabel a Manual as Auto.
                    if (WritesAutosave && !ObserveTerminalAutosave()) return;
                    if (WritesManual)
                    {
                        if (requestedManual == null) { BeginManualSave(); return; }
                        if (!ObserveManualSave()) return;
                    }
                    Require(WritesSave || authorization.AuthorizedWriteCount == 0 && authority.AdmittedNativeWriteRequests == 0 &&
                        persistence.SnapshotCount == snapshotBefore && persistence.CompletedSaveCount == completedBefore,
                        "Native dismissal unexpectedly wrote a save.");
                    if (selectedCase == Chunk7NativeProgressionCase.DismissCustomCompanion)
                        Require(nativeUi.NativeDismissalObserved && !Game.Instance.Player.AllCharacters.Any(value => ReferenceEquals(value, rider)) &&
                            !rider.IsInGame && Game.Instance.Player.Money == initialMoney,
                            "Native dismissal has not removed its exact companion or changed money unexpectedly.");
                    successful = true; cleanup = true; TryDrain();
                }
            }
            catch (Exception error) { Remember(error); cleanup = true; }
        }

        private Chunk7OrdinaryBuildDriver NewDriver(NativeProgressionRespecProbe respec) =>
            new Chunk7OrdinaryBuildDriver(buildPins, rider, horses.HorseFeature, horses.HorseUnit.AssetGuid,
                AssertChoice, row => { Require(events.Count < 1600, "Progression choices exceeded their evidence bound."); events.Add(row.DeepClone()); }, respec);

        private void AssertAuthority()
        {
            Require(authorization.IsActive && authorization.IsExactPersistenceScope(authority) &&
                authorization.AuthorityGeneration == authorityGeneration && NativePersistenceIsolation.IsExactAuthority(authority) &&
                authority.Root == expectedRoot && Game.Instance?.SaveManager?.SavePath == expectedRoot &&
                NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(expectedRoot), "Areas") &&
                (WritesManual ? authority.MatchesProgressionManualPlan(request, Game.Instance, Game.Instance.SaveManager) :
                    authority.MatchesProgressionPlan(source, WritesAutosave, RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto))) &&
                authority.MaximumNativeLoadRequests == 1 && authority.AdmittedNativeLoadRequests == 1 &&
                authorization.AuthorizedLoadCount == 1 && authorization.AuthorizedWriteCount <= (WritesSave ? 1 : 0) &&
                authorization.UnauthorizedLoadCount == 0 && authorization.UnauthorizedWriteCount == 0 && authorization.FatalViolationCount == 0 &&
                authorization.SuppressedWorkingWriteCount == 0 && authorization.BaselineLoadRequestCount == 0 &&
                !authorization.IsOneShotWorkingWriteSuppressionArmed,
                "Progression lost its exact isolated native plan, source, generation or bounded write admission.");
            var game = Game.Instance;
            Require(game.Player != null && game.Player.GameId == source.GameId && game.Player.MainCharacter.Value?.CharacterName == source.GameName &&
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe == source.Area && !game.Player.IsInCombat && !game.CutsceneLock &&
                !game.IsModeActive(GameModeType.Dialog), "Progression lost its exact idle campaign/area or entered a dialog/cutscene.");
        }
        private void AssertWorld()
        {
            AssertAuthority(); scope?.RequireCurrent();
            if (capturedBuilder != null) Require(ReferenceEquals(capturedBuilder, Game.Instance.UI.CharacterBuildController), "Captured native builder surface was replaced.");
            Require(rider.UniqueId == riderId && oldMount.UniqueId == oldMountId &&
                !Game.Instance.State.Units.Concat(Game.Instance.Player.AllCharacters).Any(value => value != null &&
                    (value.UniqueId == riderId && !ReferenceEquals(value, rider) || value.UniqueId == oldMountId && !ReferenceEquals(value, oldMount))),
                "Progression refuses same-ID native actor replacement outside its exact respec preview.");
            if (sourceLength != 0)
            {
                var file = new FileInfo(Path.Combine(expectedRoot, source.FileName));
                Require(file.Exists && file.Length == sourceLength && file.LastWriteTimeUtc.Ticks == sourceTicks,
                    "Read-only progression source metadata changed during native choices.");
            }
        }
        private void AssertTargetBinding()
        {
            if (IsRespec)
            {
                var probe = nativeUi?.RespecProbe;
                if (probe != null && probe.TryRequireNativeRebind(Game.Instance.UI.CharacterBuildController))
                {
                    probe.RequireObservedPetBinding(oldMount, originalMountDescriptor, originalMountView);
                    return;
                }
                Require(ReferenceEquals(originalDescriptor, rider.Descriptor) && ReferenceEquals(originalView, rider.View) &&
                    ReferenceEquals(originalMountDescriptor, oldMount.Descriptor) && ReferenceEquals(originalMountView, oldMount.View) &&
                    ReferenceEquals(rider.Descriptor.Pet, oldMount) && ReferenceEquals(oldMount.Descriptor.Master.Value, rider),
                    "Respec target or original pet changed before a complete observed native rebind.");
            }
            else if (selectedCase != Chunk7NativeProgressionCase.DismissCustomCompanion || nativeUi?.NativeDismissalObserved != true)
                Require(ReferenceEquals(originalDescriptor, rider.Descriptor) && ReferenceEquals(originalView, rider.View) &&
                    ReferenceEquals(originalMountDescriptor, oldMount.Descriptor) && ReferenceEquals(originalMountView, oldMount.View),
                    "Native progression replaced an undeclared target descriptor/view.");
        }
        private void AssertChoice()
        {
            AssertWorld(); AssertTargetBinding();
            Require(!NativeBusy() && (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "Native writer/loading or unexpected dialog blocks further progression choices.");
        }
        private bool NativeBusy() => LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
            NativePersistenceIsolation.HasPendingWrites || persistence.AreaTransitionPending || persistence.HasActiveSaveScope ||
            persistence.HasSaveAdmissionFence || world.SerializationBusy || world.ArchiveWorkerRunning();

        private bool ObserveNativeTargetCleanup()
        {
            if (!nativeCleanupSampled) { pairCustody.Observe("after-native-progression"); nativeCleanupSampled = true; }
            var ready = pairCustody.NativeCommandsSettled && originalPair.Relationship.State == RelationshipState.Unmounted && originalPair.Relationship.PairOwner == null &&
                originalPair.Relationship.Runtime.NoPreparedPairResidue && !originalPair.Combat.HasActiveCommand &&
                !originalPair.Combat.HasActiveGroundMovement && !originalPair.Combat.HasChargeOwnership &&
                !world.NativeControls.HasUnsettledShellFor(originalPair.Relationship) &&
                !world.UnifiedTurn.HasUnsettledPreparation && !originalOwner.RetainsObligations() && originalOwner.TeardownReady() &&
                world.Registry.FindOwner(rider) == null && world.Registry.FindOwner(oldMount) == null;
            if (ready && !nativeCleanupObserved) pairCustody.Observe("native-target-cleanup-settled");
            return ready;
        }
        // NOT READY - stale level-up autosave wait (docs/CHUNK7-STALE-LEVELUP-AUTOSAVE-FAMILIES.md).
        // The installed game never registers LevelUpAutoSaveController (GameModesFactory builds its
        // controllers explicitly without it), so an ordinary level-up never requests this native Auto
        // and the wait cannot complete. scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1 refuses this
        // family before any lock, transaction, Mods swap or launch. Kept intact until its completion
        // contract is repaired; never manufacture a level-up callback or relabel a Manual as Auto.
        private bool ObserveTerminalAutosave()
        {
            var save = completion?.ObservedSave;
            if (NativeBusy() || save == null || save.OperationState != SaveInfo.StateType.None || !save.HasFileOnDisk) return false;
            var receipt = persistence.LastCompletedSave;
            Require(completion.Complete && authorization.AuthorizedWriteCount == 1 && authority.AdmittedNativeWriteRequests == 1 &&
                persistence.SnapshotCount == snapshotBefore + 1 && persistence.CompletedSaveCount == completedBefore + 1 &&
                receipt != null && ReferenceEquals(receipt.Requested, completion.ObservedRequestedSave) && ReferenceEquals(receipt.Written, save) &&
                receipt.Sequence == completedBefore + 1 &&
                receipt.Path == Path.Combine(expectedRoot, Chunk7OrdinaryAcquisitionSelection.AutoLeaf) &&
                save.FileName == Chunk7OrdinaryAcquisitionSelection.AutoLeaf && save.Type == SaveInfo.SaveType.Auto && completion.ObservedRequestedSave.IsAutoLevelupSave && !save.IsAutoLevelupSave &&
                save.GameId == source.GameId && save.GameName == source.GameName && save.Area.AssetGuidThreadSafe == source.Area &&
                world.NativeControls.NativeCastRequestCount == castsBefore,
                "Progression autosave lacks exact actual native completion, write count, authority or descriptor proof.");
            NativePersistenceIsolation.RequireOwnedWrite(save);
            pairCustody.Observe("native-terminal-save-settled");
            var archive = NativeMountedSaveStorage.ReadArchive(save.Saver);
            var owners = world.Registry.Owners.Where(value => value.State == MountedPairOwnershipState.Active).ToArray();
            Require(world.Registry.Owners.Length == owners.Length && owners.Length == (IsRespec ? 1 : 2) &&
                (IsRespec ? world.Registry.FindOwner(rider) == null : ReferenceEquals(world.Registry.FindActive(rider), originalOwner)) &&
                archive.Kind == MountedSaveReadKind.Current && archive.SourceSchema == 3 && archive.Data != null &&
                archive.Data.Combat == null && archive.Data.Participations.Length == 0 && archive.Data.Pairs.Length == owners.Length &&
                owners.All(owner => archive.Data.Pairs.Count(pair => pair.PairId == owner.Id.ToString("N") && pair.Generation == owner.Generation &&
                    pair.Rider.Id == owner.RiderId && pair.Mount.Id == owner.MountId) == 1),
                "Progression autosave mounted ownership differs from actual settled native owners.");
            var file = new FileInfo(save.FolderName);
            nativeWrite = new JObject { ["path"] = file.FullName, ["sha256"] = Hash(file.FullName), ["length"] = file.Length,
                ["type"] = save.Type.ToString(), ["leaf"] = save.FileName, ["schema"] = archive.SourceSchema,
                ["name"] = save.Name, ["gameId"] = save.GameId, ["gameName"] = save.GameName, ["area"] = save.Area.AssetGuidThreadSafe,
                ["requestedObject"] = NativeOrdinaryAcquisitionProbe.Id(receipt.Requested), ["writtenObject"] = NativeOrdinaryAcquisitionProbe.Id(receipt.Written),
                ["completionSequence"] = receipt.Sequence, ["completedWritesBefore"] = completedBefore, ["operation"] = save.OperationState.ToString(), ["snapshotCount"] = persistence.SnapshotCount - snapshotBefore,
                ["completedWrites"] = persistence.CompletedSaveCount - completedBefore,
                ["nativeAutoLevelup"] = save.IsAutoLevelupSave, ["nativeCallbackSupplied"] = false, ["pairs"] = archive.Data.Pairs.Length,
                ["archive"] = JObject.FromObject(archive.Data, MountedSaveCodec.CreateSerializer()) };
            return true;
        }

        private void RequireManualWorldReferences()
        {
            var game = Game.Instance;
            Require(game != null && ReferenceEquals(game.Player, nativePlayer) &&
                ReferenceEquals(game.SaveManager, progressionManualManager) &&
                game.Player.GameId == source.GameId && game.Player.MainCharacter.Value?.CharacterName == source.GameName &&
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe == source.Area,
                "Owned progression Manual writer lost its exact native world references.");
        }

        private SaveManager progressionManualManager;
        private void BeginManualSave()
        {
            Require(WritesManual && requestedManual == null && driver?.Complete == true &&
                Game.Instance.UI.CharacterBuildController.LevelUpController == null &&
                !NativeBusy() && Game.Instance.CurrentMode == GameModeType.Default &&
                !Game.Instance.Player.IsInCombat && !Game.Instance.CutsceneLock &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled &&
                Game.Instance.Player.SavedInArea == Game.Instance.CurrentlyLoadedArea &&
                Game.Instance.SaveManager.IsSaveAllowed() &&
                authority.AdmittedNativeWriteRequests == 0 && authorization.AuthorizedWriteCount == 0,
                "Owned progression native choices did not settle before the separate Manual checkpoint.");
            var manager = Game.Instance.SaveManager;
            progressionManualManager = manager;
            var saves = manager.Take(3).ToArray();
            Require(saves.Length == 1 && saves[0].Type == SaveInfo.SaveType.Manual &&
                saves[0].FileName == source.FileName && saves[0].Name == source.InternalName &&
                saves[0].IsActuallySaved && saves[0].HasFileOnDisk &&
                saves[0].OperationState == SaveInfo.StateType.None,
                "Owned progression Manual source inventory changed before native numbering.");
            var number = manager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual);
            var leaf = Chunk7ProgressionSelection.ManualLeaf(number);
            Require(!File.Exists(Path.Combine(expectedRoot, leaf)), "Owned progression native Manual output already exists.");
            authority.ReserveProgressionManual(request, Game.Instance, manager, number);
            Require(authority.ProgressionManualReservations == 1 && authority.ProgressionManualNumber == number &&
                authority.ProgressionManualLeaf == leaf &&
                manager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual) == number,
                "Owned progression Manual reservation lost exact native numbering.");
            manualProbe = new Chunk7ManualSeedSaveProbe(Game.Instance, persistence,
                NativeOrdinaryAcquisitionProbe.Id, RequireManualWorldReferences, leaf,
                Chunk7ProgressionSelection.ManualOutputName);
            manualProbe.Install();
            requestedManual = manager.CreateNewSave(Chunk7ProgressionSelection.ManualOutputName);
            Require(requestedManual != null && requestedManual.Type == SaveInfo.SaveType.Manual &&
                requestedManual.Name == Chunk7ProgressionSelection.ManualOutputName &&
                !requestedManual.IsActuallySaved && !requestedManual.HasFileOnDisk &&
                !requestedManual.IsAutoLevelupSave && requestedManual.Area == Game.Instance.CurrentlyLoadedArea,
                "Owned progression Manual request is not a fresh exact native descriptor.");
            manualProbe.Expect(requestedManual);
            mutationStarted = true;
            Game.Instance.SaveGame(requestedManual, () => {
                manualCallbackCount++; manualCallback = true; manualProbe.ObserveCallback();
            });
        }

        private bool ObserveManualSave()
        {
            if (!manualCallback || NativeBusy()) return false;
            Require(manualCallbackCount == 1 && manualProbe != null && manualProbe.Complete &&
                authority.ProgressionManualReservations == 1 &&
                authority.AdmittedNativeWriteRequests == 1 && authorization.AuthorizedWriteCount == 1 &&
                persistence.SnapshotCount == snapshotBefore + 1 &&
                persistence.CompletedSaveCount == completedBefore + 1,
                "Owned progression Manual lacks exact callback, worker and one-write settlement.");
            var receipt = persistence.LastCompletedSave;
            var written = manualProbe.Written;
            var leaf = authority.ProgressionManualLeaf;
            Require(receipt != null && ReferenceEquals(receipt.Requested, requestedManual) &&
                ReferenceEquals(receipt.Written, written) && receipt.Sequence == completedBefore + 1 &&
                receipt.Path == Path.Combine(expectedRoot, leaf) && written != null &&
                written.Type == SaveInfo.SaveType.Manual && written.Name == Chunk7ProgressionSelection.ManualOutputName &&
                written.FileName == leaf && written.GameId == source.GameId &&
                written.GameName == source.GameName && written.Area?.AssetGuidThreadSafe == source.Area &&
                written.OperationState == SaveInfo.StateType.None && written.IsActuallySaved &&
                written.HasFileOnDisk && !written.IsAutoLevelupSave &&
                world.NativeControls.NativeCastRequestCount == castsBefore,
                "Owned progression Manual descriptor or native completion differs from the reserved write.");
            NativePersistenceIsolation.RequireOwnedWrite(written);
            var saves = progressionManualManager.Take(3).ToArray();
            Require(saves.Length == 2 && saves.Count(value => value.FileName == source.FileName &&
                value.Type == SaveInfo.SaveType.Manual) == 1 &&
                saves.Count(value => ReferenceEquals(value, written)) == 1,
                "Owned progression Manual settled descriptor inventory differs.");
            pairCustody.Observe("native-terminal-save-settled");
            var archive = NativeMountedSaveStorage.ReadArchive(written.Saver);
            var owners = world.Registry.Owners.Where(value => value.State == MountedPairOwnershipState.Active).ToArray();
            Require(world.Registry.Owners.Length == owners.Length && owners.Length == (IsRespec ? 1 : 2) &&
                (IsRespec ? world.Registry.FindOwner(rider) == null : ReferenceEquals(world.Registry.FindActive(rider), originalOwner)) &&
                archive.Kind == MountedSaveReadKind.Current && archive.SourceSchema == 3 && archive.Data != null &&
                archive.Data.Combat == null && archive.Data.Participations.Length == 0 && archive.Data.Pairs.Length == owners.Length &&
                owners.All(owner => archive.Data.Pairs.Count(pair => pair.PairId == owner.Id.ToString("N") &&
                    pair.Generation == owner.Generation && pair.Rider.Id == owner.RiderId && pair.Mount.Id == owner.MountId) == 1),
                "Owned progression Manual mounted metadata differs from settled native owner custody.");
            var file = new FileInfo(written.FolderName);
            nativeWrite = new JObject { ["path"] = file.FullName, ["sha256"] = Hash(file.FullName), ["length"] = file.Length,
                ["type"] = written.Type.ToString(), ["leaf"] = leaf, ["schema"] = archive.SourceSchema,
                ["name"] = written.Name, ["gameId"] = written.GameId, ["gameName"] = written.GameName,
                ["area"] = written.Area.AssetGuidThreadSafe,
                ["requestedObject"] = NativeOrdinaryAcquisitionProbe.Id(requestedManual),
                ["writtenObject"] = NativeOrdinaryAcquisitionProbe.Id(written),
                ["completionSequence"] = receipt.Sequence, ["completedWritesBefore"] = completedBefore,
                ["snapshotBefore"] = snapshotBefore,
                ["operation"] = written.OperationState.ToString(),
                ["snapshotCount"] = persistence.SnapshotCount - snapshotBefore,
                ["completedWrites"] = persistence.CompletedSaveCount - completedBefore,
                ["nativeAutoLevelup"] = written.IsAutoLevelupSave, ["nativeCallbackSupplied"] = true,
                ["manualCallbackCount"] = manualCallbackCount, ["nativeManualNumber"] = authority.ProgressionManualNumber,
                ["pairs"] = archive.Data.Pairs.Length,
                ["archive"] = JObject.FromObject(archive.Data, MountedSaveCodec.CreateSerializer()) };
            return true;
        }

        internal bool TryDrain()
        {
            if (disposed) return true;
            cleanup = true;
            if (!mutationStarted) { settled = true; return true; }
            try
            {
                if (NativeBusy() || completion?.InSynchronousCallback == true || manualProbe?.InCallback == true) return false;
                AssertWorld();
                AssertTargetBinding();
                if (cancellation == null)
                {
                    CharacterBuildController owned;
                    if (driver != null && driver.TryGetOwnedBuildSession(out owned))
                        cancellation = new Chunk7NativeBuilderCancellation(owned, AssertWorld, NativeBusy);
                    else if (IsRespec && nativeUi?.RespecProbe != null && Game.Instance.UI.CharacterBuildController.LevelUpController != null)
                    {
                        nativeUi.RespecProbe.RequireExactPreview(Game.Instance.UI.CharacterBuildController);
                        cancellation = new Chunk7NativeBuilderCancellation(Game.Instance.UI.CharacterBuildController, AssertWorld, NativeBusy);
                    }
                }
                if (cancellation != null && !cancellation.TryDrain()) return false;
                if (nativeUi != null && !nativeUi.DrainUi()) return false;
                if (pairCustody != null && !pairCustody.TryDrainOriginalPair()) return false;
                var observersReleased = true;
                try { if (nativeUi != null) { if (nativeUiEvidence == null) nativeUiEvidence = nativeUi.Capture(); nativeUi.Dispose(); } }
                catch (Exception error) { Remember(error); observersReleased = false; }
                try { if (completion != null) { if (completionEvidence == null) completionEvidence = completion.Capture(); completion.Dispose(); } }
                catch (Exception error) { Remember(error); observersReleased = false; }
                try { manualProbe?.Dispose(); }
                catch (Exception error) { Remember(error); observersReleased = false; }
                if (!observersReleased) return false;
                if (rotationOwned) { AssertWorld(); NativePersistenceIsolation.DisableNativeSlotRotation(); rotationOwned = false; }
                Require(!NativeBusy() && Game.Instance.UI.CharacterBuildController.LevelUpController == null &&
                    Hash(Path.Combine(expectedRoot, source.FileName)) == source.Sha256,
                    "Progression cleanup still owns a builder/worker or its read-only source changed.");
                settled = true; return true;
            }
            catch (Exception error) { Remember(error); return false; }
        }
        public void Dispose()
        {
            if (disposed) return;
            if (!TryDrain()) throw new InvalidOperationException("Native progression retains UI/observer/command/worker custody.");
            disposed = true;
        }
        private JObject NativeActor() => new JObject { ["actorId"] = rider.UniqueId, ["object"] = RuntimeHelpers.GetHashCode(rider),
            ["level"] = rider.Descriptor.Progression.CharacterLevel, ["xp"] = rider.Descriptor.Progression.Experience,
            ["petId"] = rider.Descriptor.Pet?.UniqueId, ["petLevel"] = rider.Descriptor.Pet?.Descriptor.Progression.CharacterLevel,
            ["money"] = Game.Instance.Player.Money, ["gameTimeTicks"] = Game.Instance.TimeController.GameTime.Ticks };
        private void Next(int next) { stage = next; stageStart = elapsed.Elapsed.TotalSeconds; }
        private void Record(string name, JObject detail) { Require(events.Count < 1600, "Progression evidence exceeded its bound."); detail["kind"] = name; events.Add(detail); }
        private void Remember(Exception error) { var text = error.ToString(); if (!errors.Contains(text)) errors.Add(text); successful = false; }
        private static string Hash(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
